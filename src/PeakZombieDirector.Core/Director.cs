namespace PeakZombieDirector.Core;

public readonly struct Position
{
    public readonly double X,Y,Z;
    public Position(double x,double y,double z) { X=x; Y=y; Z=z; }
    public double DistanceTo(Position b, DistanceMode mode=DistanceMode.Spatial) => Math.Sqrt((X-b.X)*(X-b.X)+(mode==DistanceMode.Horizontal?0:(Y-b.Y)*(Y-b.Y))+(Z-b.Z)*(Z-b.Z));
}
public sealed class PlayerSnapshot
{
    public int Id { get; }
    public Position Position { get; }
    public PlayerSnapshot(int id,Position position) { Id=id; Position=position; }
}
public sealed class ZombieSnapshot
{
    public int Id { get; }
    public Position Position { get; }
    public ZombieSnapshot(int id,Position position) { Id=id; Position=position; }
}
public interface IZombieWorld
{
    bool IsHost { get; }
    bool IsPlayable { get; }
    IReadOnlyList<PlayerSnapshot> Players { get; }
    IReadOnlyList<ZombieSnapshot> OwnedZombies { get; }
    int TotalAlive { get; }
    bool TryFindSpawn(PlayerSnapshot player, double min, double max, DirectorConfig config, out Position position);
    bool TrySpawn(Position position, out int id);
    bool TryDestroy(int id);
    void Chase(int id,int playerId,double seconds,bool snap);
}
public sealed class MapSpawnPoint
{
    public int Id {get;}
    public Position Position {get;}
    public MapSpawnPoint(int id,Position position) {Id=id;Position=position;}
}
public interface IMapSpawnWorld
{
    IReadOnlyList<MapSpawnPoint> ReadyMapPoints {get;}
    bool TryValidateMapPoint(Position requested,out Position safe);
}
public static class DifficultyScaler
{
    public static int Wave(DirectorConfig c,double time) => (int)Math.Min(c.WaveBudget, Math.Min(c.WaveGrowth?c.GrowthLimit:int.MaxValue, c.WaveCount+(c.WaveGrowth?Math.Floor(Math.Max(0,time)/c.GrowthPeriod)*c.GrowthAmount:0)));
    public static int Cap(DirectorConfig c,double time) => c.MaxAlive==0?0:(int)Math.Min(10000,c.MaxAlive+(c.CapGrowth?Math.Floor(Math.Max(0,time)/c.CapGrowthPeriod)*c.CapGrowthAmount:0));
}
public sealed class DirectorDiagnostics
{
    public long Spawned, Replaced, SpawnFailures, SearchFailures, DeleteFailures;
    public double LastMilliseconds;
    public string Status = "Waiting";
}
public sealed class ZombieDirector
{
    private sealed class Tracked
    {
        public Position ProgressPosition;
        public double ProgressTime, FarSince=-1, NextRecycle, NextTarget;
        public int Target;
    }
    private readonly Dictionary<int,Tracked> tracked = new();
    private readonly HashSet<int> rollbackPending = new();
    private readonly HashSet<int> consumedMapPoints = new();
    private readonly Queue<double> spawnTimes = new();
    private readonly Random random;
    private bool running, initialized;
    private double started, nextWave, nextCheck, lastTime;
    private int pending;
    public DirectorDiagnostics Diagnostics { get; } = new();
    public double Elapsed { get; private set; }
    public double NextWaveIn { get; private set; }
    public int Pending => pending;
    public ZombieDirector(int seed=0) { random=new Random(seed); }
    public void Reset()
    { tracked.Clear(); rollbackPending.Clear(); consumedMapPoints.Clear(); spawnTimes.Clear(); running=false; initialized=false; pending=0; Elapsed=0; NextWaveIn=0; Diagnostics.Status="Waiting"; }
    private bool Budget(double now,DirectorConfig c) { while(spawnTimes.Count>0 && now-spawnTimes.Peek()>=1) spawnTimes.Dequeue(); return spawnTimes.Count<c.NetworkBudget; }
    private bool Spawn(IZombieWorld w,Position p,double now,DirectorConfig c,out int id)
    { id=0; if(!Budget(now,c)) return false; spawnTimes.Enqueue(now); if(w.TrySpawn(p,out id)) return true; Diagnostics.SpawnFailures++; return false; }
    public void Tick(double now,DirectorConfig c,IZombieWorld w)
    {
        var timer=System.Diagnostics.Stopwatch.StartNew();
        try { Step(now,c,w); }
        finally { Diagnostics.LastMilliseconds=timer.Elapsed.TotalMilliseconds; }
    }
    private void Step(double now,DirectorConfig c,IZombieWorld w)
    {
        if(!c.Enabled || !w.IsHost || !w.IsPlayable) { Reset(); return; }
        if(running && now<lastTime) Reset();
        lastTime=now;
        if(!running) { running=true; started=now; nextWave=now+c.FirstDelay; nextCheck=now; }
        Elapsed=Math.Max(0,now-started); NextWaveIn=Math.Max(0,nextWave-now);
        var zombies=w.OwnedZombies.ToArray();
        var ids=new HashSet<int>(zombies.Select(z=>z.Id));
        foreach(var id in tracked.Keys.Where(id=>!ids.Contains(id)).ToArray()) tracked.Remove(id);
        // Compensating deletion must finish before issuing any new network spawns.
        foreach(var id in rollbackPending.ToArray()) if(!ids.Contains(id) || w.TryDestroy(id)) rollbackPending.Remove(id);
        if(rollbackPending.Count>0) { Diagnostics.Status="Waiting for replacement rollback"; return; }
        var players=w.Players;
        if(players.Count==0) { pending=0; Diagnostics.Status="No living players"; nextWave=now+c.WaveInterval; return; }
        Diagnostics.Status=w.TotalAlive>=c.WarningCount?"High zombie count / FPS risk":"Running";
        foreach(var z in zombies)
        {
            if(!tracked.TryGetValue(z.Id,out var t)) tracked[z.Id]=t=new Tracked {ProgressPosition=z.Position,ProgressTime=now,NextRecycle=now+c.RecycleCooldown};
            var existing=players.FirstOrDefault(p=>p.Id==t.Target);
            if(existing==null || now>=t.NextTarget)
            {
                var target=existing;
                if(target==null || (c.Retarget && c.Target!=TargetMode.PreferOriginal)) target=Select(players,z.Position,c);
                t.Target=target!.Id;
                t.NextTarget=now+(existing==null?c.SearchInterval:c.TargetInterval);
                if(c.Persistent) w.Chase(z.Id,t.Target,Math.Max(c.TargetInterval,c.SearchInterval)+1,true);
            }
        }
        if(now>=nextCheck)
        {
            nextCheck=now+c.CheckInterval; int replaced=0;
            foreach(var z in zombies)
            {
                var t=tracked[z.Id]; var target=players.FirstOrDefault(p=>p.Id==t.Target); if(target==null) continue;
                var distance=z.Position.DistanceTo(target.Position,c.Distance);
                if(distance>c.RecycleDistance) { if(t.FarSince<0) t.FarSince=now; } else t.FarSince=-1;
                if(z.Position.DistanceTo(t.ProgressPosition)>=c.MinProgress) { t.ProgressPosition=z.Position; t.ProgressTime=now; }
                var far=c.Recycle && t.FarSince>=0 && now-t.FarSince>=c.FarDuration;
                var stuck=c.StuckRecovery && now-t.ProgressTime>=c.StuckTime && distance>c.RecycleMin;
                if((!far && !stuck) || now<t.NextRecycle || replaced>=c.RecycleBudget || !Budget(now,c)) continue;
                // Attempt is cooled down even on position/spawn failure.
                t.NextRecycle=now+c.RecycleCooldown;
                replaced++;
                if(!w.TryFindSpawn(target,c.RecycleMin,c.RecycleMax,c,out var position)) { Diagnostics.SearchFailures++; continue; }
                if(!Spawn(w,position,now,c,out var replacement)) continue;
                if(!w.TryDestroy(z.Id))
                {
                    Diagnostics.DeleteFailures++;
                    if(!w.TryDestroy(replacement)) { rollbackPending.Add(replacement); Diagnostics.Status="Waiting for replacement rollback"; return; }
                    continue;
                }
                tracked.Remove(z.Id);
                tracked[replacement]=new Tracked {Target=target.Id,ProgressPosition=position,ProgressTime=now,NextRecycle=now+c.RecycleCooldown,NextTarget=now+c.TargetInterval};
                if(c.Persistent) w.Chase(replacement,target.Id,c.TargetInterval+1,true);
                Diagnostics.Replaced++;
            }
        }
        if(c.MapSpawns && w is IMapSpawnWorld maps)
        {
            int mapAttempts=0;
            foreach(var map in maps.ReadyMapPoints)
            {
                if(mapAttempts>=c.NetworkBudget || !Budget(now,c)) break;
                int currentCap=DifficultyScaler.Cap(c,Elapsed);
                if(currentCap>0 && w.TotalAlive>=currentCap) break;
                if(consumedMapPoints.Contains(map.Id)) continue;
                mapAttempts++;
                if(!maps.TryValidateMapPoint(map.Position,out var safe)) {Diagnostics.SearchFailures++;continue;}
                if(Spawn(w,safe,now,c,out int id)) {consumedMapPoints.Add(map.Id);Diagnostics.Spawned++;var target=Select(players,safe,c);if(c.Persistent) w.Chase(id,target.Id,c.TargetInterval+1,true);}
            }
        }
        if(!c.Waves) { pending=0; nextWave=now+c.WaveInterval; return; }
        if(now>=nextWave && pending==0)
        {
            pending=initialized?DifficultyScaler.Wave(c,Elapsed):Math.Min(c.InitialCount,c.WaveBudget);
            initialized=true;
            if(c.RandomVariation>0) pending=Math.Max(0,Math.Min(c.WaveBudget,(int)Math.Round(pending*(1+(random.NextDouble()*2-1)*c.RandomVariation))));
            nextWave=now+c.WaveInterval*(1+(random.NextDouble()*2-1)*c.RandomVariation);
        }
        int cap=DifficultyScaler.Cap(c,Elapsed);
        // No accumulated burst when a full wave is blocked by cap.
        if(cap>0 && w.TotalAlive>=cap) { pending=0; return; }
        int attempts=0;
        while(pending>0 && Budget(now,c) && attempts++<c.NetworkBudget)
        {
            if(cap>0 && w.TotalAlive>=cap) { pending=0; break; }
            var target=players[random.Next(players.Count)]; pending--;
            if(!w.TryFindSpawn(target,c.SpawnMin,c.SpawnMax,c,out var position)) { Diagnostics.SearchFailures++; continue; }
            if(Spawn(w,position,now,c,out int id)) { Diagnostics.Spawned++; if(c.Persistent) w.Chase(id,target.Id,c.TargetInterval+1,true); }
        }
    }
    private PlayerSnapshot Select(IReadOnlyList<PlayerSnapshot> players,Position position,DirectorConfig c) => c.Target==TargetMode.Random?players[random.Next(players.Count)]:players.OrderBy(p=>p.Position.DistanceTo(position,c.Distance)).First();
}
