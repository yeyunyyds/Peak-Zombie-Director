using PeakZombieDirector.Core;
using Xunit;
namespace PeakZombieDirector.Tests;
public sealed class FakeWorld : IZombieWorld, IMapSpawnWorld
{
    public List<MapSpawnPoint> MapPoints=new();
    public IReadOnlyList<MapSpawnPoint> ReadyMapPoints=>MapPoints;
    public bool TryValidateMapPoint(Position requested,out Position safe) {safe=requested;return !FailSearch;}
    public bool IsHost {get;set;}=true;
    public bool IsPlayable {get;set;}=true;
    public List<PlayerSnapshot> Party=new(){new(1,new Position(0,0,0))};
    public Dictionary<int,Position> Zombies=new();
    public IReadOnlyList<PlayerSnapshot> Players=>Party;
    public IReadOnlyList<ZombieSnapshot> OwnedZombies=>Zombies.Select(z=>new ZombieSnapshot(z.Key,z.Value)).ToArray();
    public int TotalAlive=>Zombies.Count+ExternalCount;
    public int ExternalCount, SpawnCalls, DestroyCalls, TargetCalls;
    public int LastTarget;
    public bool FailSpawn,FailSearch,FailDelete,FailRollback;
    int id=100;
    public bool TryFindSpawn(PlayerSnapshot p,double min,double max,DirectorConfig c,out Position pos) {pos=new Position(p.Position.X+min,p.Position.Y,p.Position.Z);return !FailSearch;}
    public bool TrySpawn(Position p,out int spawned) {SpawnCalls++;spawned=++id;if(FailSpawn)return false; Zombies[spawned]=p;return true;}
    public bool TryDestroy(int id) {DestroyCalls++;if((FailDelete&&id<100)||(FailRollback&&id>=100))return false;return Zombies.Remove(id);}
    public void Chase(int id,int p,double seconds,bool snap) {TargetCalls++;LastTarget=p;}
}
public sealed class DirectorTests
{
    static DirectorConfig Config()=>new(){FirstDelay=0,InitialCount=0,WaveInterval=10,WaveGrowth=false,StuckRecovery=false,RecycleCooldown=1,CheckInterval=.2,FarDuration=0,NetworkBudget=10};
    [Theory]
    [InlineData(Difficulty.Easy,3,1,30,10)] [InlineData(Difficulty.Normal,5,2,22,18)]
    [InlineData(Difficulty.Advanced,8,3,15,28)] [InlineData(Difficulty.Expert,12,4,10,40)]
    public void Presets(Difficulty d,int initial,int wave,int interval,int cap)
    {var c=DifficultyPresets.Get(d);Assert.Equal(initial,c.InitialCount);Assert.Equal(wave,c.WaveCount);Assert.Equal(interval,c.WaveInterval);Assert.Equal(cap,c.MaxAlive);c.Validate();Assert.Equal(Settings.All.Length,ConfigCodec.Encode(c).Split('\n').Length);}
    [Fact] public void ManualEditSelectsCustomAndSavedCustomSurvivesPresets()
    {var s=new ConfigurationState(Difficulty.Normal,new(),new());s.Edit(c=>c.Sprint=2.7);Assert.Equal(Difficulty.Custom,s.Selected);Assert.True(s.Dirty);s.SaveCustom();s.Select(Difficulty.Expert);s.Select(Difficulty.Custom);Assert.Equal(2.7,s.Active.Sprint);Assert.False(s.Dirty);}
    [Fact] public void CustomPersistenceRoundtrip()
    {var s=new ConfigurationState(Difficulty.Normal,new(),new());s.Edit(c=>{c.SpawnMin=24;c.SpawnMax=37;c.Target=TargetMode.PreferOriginal;});s.SaveCustom();var restored=new ConfigurationState(Difficulty.Custom,ConfigCodec.Decode(ConfigCodec.Encode(s.Active)),ConfigCodec.Decode(ConfigCodec.Encode(s.Custom)));Assert.Equal(24,restored.Active.SpawnMin);Assert.Equal(TargetMode.PreferOriginal,restored.Custom.Target);restored.Select(Difficulty.Easy);restored.Select(Difficulty.Custom);Assert.Equal(37,restored.Active.SpawnMax);}
    [Fact] public void FutureKeysAndBadValuesAreHandled()
    {int warnings=0;var c=ConfigCodec.Decode("WaveInterval=NaN\nInitialCount=-20\nSpawnMin=50\nSpawnMax=10\nTarget=999\nNewFutureKey=1",_=>warnings++);Assert.Equal(22,c.WaveInterval);Assert.Equal(0,c.InitialCount);Assert.Equal(50,c.SpawnMax);Assert.Equal(TargetMode.Nearest,c.Target);Assert.Equal(4,warnings);}
    [Fact] public void WavesExecuteAtInterval()
    {var c=Config();c.InitialCount=3;var w=new FakeWorld();var d=new ZombieDirector();d.Tick(0,c,w);Assert.Equal(3,w.TotalAlive);d.Tick(9,c,w);Assert.Equal(3,w.TotalAlive);d.Tick(10,c,w);Assert.Equal(5,w.TotalAlive);}
    [Fact] public void GrowthAndCaps()
    {var c=Config();c.WaveGrowth=true;c.GrowthPeriod=120;c.GrowthAmount=1;c.GrowthLimit=5;Assert.Equal(2,DifficultyScaler.Wave(c,0));Assert.Equal(3,DifficultyScaler.Wave(c,120));Assert.Equal(5,DifficultyScaler.Wave(c,1000));c.CapGrowth=true;c.CapGrowthPeriod=60;c.CapGrowthAmount=3;Assert.Equal(24,DifficultyScaler.Cap(c,120));c.MaxAlive=0;Assert.Equal(0,DifficultyScaler.Cap(c,1000));}
    [Fact] public void CapIncludesExternalZombiesAndDropsBacklog()
    {var c=Config();c.InitialCount=5;c.MaxAlive=3;var w=new FakeWorld{ExternalCount=2};var d=new ZombieDirector();d.Tick(0,c,w);Assert.Equal(3,w.TotalAlive);Assert.Equal(0,d.Pending);}
    [Fact] public void RecycleAtCapKeepsCountAndSeparatesCounters()
    {var c=Config();c.MaxAlive=1;var w=new FakeWorld();w.Zombies[1]=new Position(200,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(1,c,w);Assert.Single(w.Zombies);Assert.False(w.Zombies.ContainsKey(1));Assert.Equal(1,d.Diagnostics.Replaced);Assert.Equal(0,d.Diagnostics.Spawned);}
    [Fact] public void SpawnFailureRetainsOldAndCoolsRetry()
    {var c=Config();c.Waves=false;c.RecycleCooldown=10;var w=new FakeWorld{FailSpawn=true};w.Zombies[1]=new Position(200,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(10,c,w);d.Tick(11,c,w);Assert.True(w.Zombies.ContainsKey(1));Assert.Equal(1,w.SpawnCalls);Assert.Equal(0,w.DestroyCalls);}
    [Fact] public void InvalidPositionNeverTriggersSpawnOrDeletion()
    {var c=Config();var w=new FakeWorld{FailSearch=true};w.Zombies[1]=new Position(200,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(1,c,w);Assert.Equal(0,w.SpawnCalls);Assert.Equal(0,w.DestroyCalls);}
    [Fact] public void FailedOldDeleteRollsBackReplacement()
    {var c=Config();var w=new FakeWorld{FailDelete=true};w.Zombies[1]=new Position(200,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(1,c,w);Assert.Single(w.Zombies);Assert.True(w.Zombies.ContainsKey(1));Assert.Equal(0,d.Diagnostics.Replaced);Assert.Equal(1,d.Diagnostics.DeleteFailures);}
    [Fact] public void FailedRollbackBlocksFurtherSpawnsUntilRecovered()
    {var c=Config();c.Waves=false;var w=new FakeWorld{FailDelete=true,FailRollback=true};w.Zombies[1]=new Position(200,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(1,c,w);Assert.Equal(2,w.TotalAlive);d.Tick(10,c,w);Assert.Equal(1,w.SpawnCalls);w.FailRollback=false;d.Tick(10.3,c,w);Assert.Single(w.Zombies);}
    [Fact] public void ReplacementCannotRecurseWithinTickAndHasCooldown()
    {var c=Config();c.RecycleDistance=10;c.RecycleMin=20;c.RecycleMax=20;c.RecycleCooldown=10;var w=new FakeWorld();w.Zombies[1]=new Position(200,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(10,c,w);Assert.Equal(1,d.Diagnostics.Replaced);d.Tick(11,c,w);Assert.Equal(1,d.Diagnostics.Replaced);}
    [Fact] public void DeadTargetIsReplacedEvenWithRetargetOff()
    {var c=Config();c.Retarget=false;var w=new FakeWorld();w.Zombies[1]=new Position(5,0,0);var d=new ZombieDirector();d.Tick(0,c,w);Assert.Equal(1,w.LastTarget);w.Party=new(){new(2,new Position(20,0,0))};d.Tick(.5,c,w);Assert.Equal(2,w.LastTarget);}
    [Fact] public void NoPlayersDoesNoWork()
    {var c=Config();c.InitialCount=5;var w=new FakeWorld();w.Party.Clear();w.Zombies[1]=new Position(200,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(100,c,w);Assert.Equal(0,w.SpawnCalls);Assert.Equal(0,w.TargetCalls);}
    [Fact] public void ClientDoesNotMutateWorld()
    {var c=Config();c.InitialCount=5;var w=new FakeWorld{IsHost=false};w.Zombies[1]=new Position(200,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(100,c,w);Assert.Equal(0,w.SpawnCalls);Assert.Equal(0,w.DestroyCalls);Assert.Equal(0,w.TargetCalls);}
    [Fact] public void ResetDiscardsTimersAndObjectReferences()
    {var c=Config();c.InitialCount=2;var w=new FakeWorld();var d=new ZombieDirector();d.Tick(0,c,w);d.Reset();w.Zombies.Clear();d.Tick(100,c,w);Assert.Equal(2,w.TotalAlive);Assert.Equal(0,d.Elapsed);}
    [Fact] public void NetworkBudgetSpreadsLargeWaveAcrossTime()
    {var c=Config();c.InitialCount=10;c.NetworkBudget=2;var w=new FakeWorld();var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(.5,c,w);Assert.Equal(2,w.SpawnCalls);d.Tick(1,c,w);Assert.Equal(4,w.SpawnCalls);Assert.Equal(6,d.Pending);}
    [Fact] public void FarDurationMustBeContinuous()
    {var c=Config();c.FarDuration=3;var w=new FakeWorld();w.Zombies[1]=new Position(200,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(2,c,w);w.Zombies[1]=new Position(1,0,0);d.Tick(3,c,w);w.Zombies[1]=new Position(200,0,0);d.Tick(4,c,w);d.Tick(6,c,w);Assert.Equal(0,d.Diagnostics.Replaced);d.Tick(7,c,w);Assert.Equal(1,d.Diagnostics.Replaced);}
    [Fact] public void HorizontalDistanceIgnoresCliffHeight()
    {var c=Config();c.Distance=DistanceMode.Horizontal;var w=new FakeWorld();w.Zombies[1]=new Position(0,200,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(1,c,w);Assert.Equal(0,d.Diagnostics.Replaced);c.Distance=DistanceMode.Spatial;d.Tick(2,c,w);Assert.Equal(1,d.Diagnostics.Replaced);}
    [Fact] public void StuckRecoveryIsIndependentOfFarDistance()
    {var c=Config();c.StuckRecovery=true;c.StuckTime=20;var w=new FakeWorld();w.Zombies[1]=new Position(50,0,0);var d=new ZombieDirector();d.Tick(0,c,w);d.Tick(19,c,w);Assert.Equal(0,d.Diagnostics.Replaced);d.Tick(20,c,w);Assert.Equal(1,d.Diagnostics.Replaced);}
    [Fact] public void SettingsSchemaCoversEveryRuntimeProperty()
    {Assert.Equal(typeof(DirectorConfig).GetProperties().Length,Settings.All.Length);Assert.All(Settings.All,d=>Assert.False(string.IsNullOrWhiteSpace(d.Meta.Zh)));}
    [Fact] public void MapPointsShareBudgetAndAreConsumedOnce()
    {var c=Config();c.Waves=false;c.MapSpawns=true;c.NetworkBudget=1;var w=new FakeWorld();w.MapPoints.Add(new(1,new Position(20,0,0)));w.MapPoints.Add(new(2,new Position(25,0,0)));var d=new ZombieDirector();d.Tick(0,c,w);Assert.Equal(1,w.TotalAlive);d.Tick(.5,c,w);Assert.Equal(1,w.TotalAlive);d.Tick(1,c,w);Assert.Equal(2,w.TotalAlive);d.Tick(2,c,w);Assert.Equal(2,w.TotalAlive);}
    [Fact] public void MapSpawnsDoNotBypassCapOrPositionValidation()
    {var c=Config();c.Waves=false;c.MapSpawns=true;c.MaxAlive=1;var w=new FakeWorld{FailSearch=true};w.MapPoints.Add(new(1,new Position(20,0,0)));var d=new ZombieDirector();d.Tick(0,c,w);Assert.Equal(0,w.SpawnCalls);w.FailSearch=false;d.Tick(1,c,w);Assert.Single(w.Zombies);w.MapPoints.Add(new(2,new Position(25,0,0)));d.Tick(2,c,w);Assert.Single(w.Zombies);}
}
