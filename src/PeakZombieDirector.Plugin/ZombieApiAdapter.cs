using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using PeakZombieDirector.Core;
using Photon.Pun;
using UnityEngine;
using Zombies_Anywhere;
namespace PeakZombieDirector;
internal sealed class ZombieApiAdapter : IZombieWorld, IMapSpawnWorld
{
    readonly Dictionary<int,MushroomZombie> owned=new();
    readonly Dictionary<ConfigEntryBase,object> prior=new();
    readonly Dictionary<int,float[]> originals=new();
    readonly Dictionary<int,Character> players=new();
    DirectorConfig settings=new();
    public Action<string>? ReportUnexpectedRemoval;
    readonly Dictionary<int,float> born=new();
    bool acquired;
    bool priorAutoSave;
    bool configBorrowed;
    public bool IsHost => PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;
    public bool IsPlayable => IsHost && ZombiesApi.IsReady && !ZombiesApi.InAirport && MapHandler.Instance!=null && (settings.AllBiomes || MapHandler.GetBiomeForSegment((int)MapHandler.CurrentSegmentNumber)==Biome.BiomeType.Roots);
    public bool Ready => ZombiesApi.IsReady && ZombiesApi.Version=="2.0.0";
    public int TotalAlive => ZombiesApi.AliveCount;
    public IReadOnlyList<PlayerSnapshot> Players => players.Select(p=>new PlayerSnapshot(p.Key,ToPosition(p.Value.Center))).ToArray();
    public IReadOnlyList<ZombieSnapshot> OwnedZombies => owned.Where(p=>p.Value!=null && p.Value.currentState!=MushroomZombie.State.Dead).Select(p=>new ZombieSnapshot(p.Key,ToPosition(ZombieCenter(p.Value)))).ToArray();
    static Vector3 ZombieCenter(MushroomZombie z) => z.GetComponent<Character>().Center;
    public static Position ToPosition(Vector3 v)=>new(v.x,v.y,v.z);
    public static Vector3 ToVector(Position p)=>new((float)p.X,(float)p.Y,(float)p.Z);
    public void Refresh(DirectorConfig c)
    {
        settings=c;
        foreach(var id in owned.Where(p=>p.Value==null || p.Value.currentState==MushroomZombie.State.Dead).Select(p=>p.Key).ToArray()) { var z=owned[id]; var age=born.TryGetValue(id,out var birth)?Time.time-birth:-1; ReportUnexpectedRemoval?.Invoke($"Owned zombie {id} left tracking: {(z==null?"object destroyed externally":"AI entered Dead state")}; age={age:F1}s. Director recycling did not remove this object."); owned.Remove(id); originals.Remove(id); born.Remove(id); }
        players.Clear();
        foreach(var ch in Character.AllCharacters)
            if(ch!=null && !ch.isBot && !ch.isZombie && ch.data!=null && !ch.data.dead && !ch.warping && (c.AttackDowned || !ch.data.fullyPassedOut) && ch.photonView!=null && ch.photonView.Owner!=null)
                players[ch.photonView.ViewID]=ch;
        if(acquired) foreach(var z in owned.Values) ApplyZombie(z);
    }
    // Uses the dependency's actual ConfigFile entries; no private field reflection.
    void Override(string group,string key,object value)
    {
        var file=Chainloader.PluginInfos[ZombiesApi.PluginGuid].Instance.Config;
        var definition=new ConfigDefinition(group,key);
        if(!file.ContainsKey(definition)) throw new InvalidOperationException("PEAK Zombies config missing: "+group+"/"+key);
        var entry=file[definition];
        if(!prior.ContainsKey(entry)) prior[entry]=entry.BoxedValue;
        if(!Equals(entry.BoxedValue,value)) entry.BoxedValue=value;
    }
    void BorrowConfig()
    {
        if(configBorrowed) return;
        var file=Chainloader.PluginInfos[ZombiesApi.PluginGuid].Instance.Config;
        priorAutoSave=file.SaveOnConfigSet; file.SaveOnConfigSet=false; configBorrowed=true;
    }
    public void Acquire(DirectorConfig c)
    {
        if(!acquired && ZombiesApi.DirectorActive) throw new InvalidOperationException("Another zombie director is active");
        if(!Ready) throw new InvalidOperationException("Requires PEAK Zombies API 2.0.0");
        BorrowConfig();
        if(!acquired) { ZombiesApi.BeginDirector(); acquired=true; }
        Override("Around Players","Enabled",false);
        Override("Vanilla Style","Enabled",false);
        Override("General","DisableZombies",false);
        Override("General","ForceZombies",true);
        Override("Culling","NeverDespawnFarZombies",c.NeverCull);
        Override("Zombie Behavior","RemoveZombieLifetime",c.RemoveLifetime);
        Override("Zombie Behavior","ZombieLifetimeOverride",(float)c.Lifetime);
        Override("Zombie Behavior","AttackUnconsciousPlayers",c.AttackDowned);
        Override("Combat","LungeStumbleSeconds",(float)c.LungeStumble);
        Override("Combat","BiteSelfStumbleSeconds",(float)c.BiteSelfStumble);
        Override("Combat","BiteTriggerCooldown",(float)c.BiteCooldown);
        string[] names={"WalkSpeedMultiplier","SprintSpeedMultiplier","ClimbSpeedMultiplier","JumpImpulseMultiplier","LungeSpeedMultiplier"};
        double[] values={c.Walk,c.Sprint,c.Climb,c.Jump,c.Lunge};
        for(int i=0;i<names.Length;i++) Override("Zombie Stats",names[i],(float)values[i]);
        settings=c;
    }
    public void Release(bool cleanup)
    {
        try { if(cleanup && IsHost) foreach(var z in owned.Values.ToArray()) if(z!=null && !ZombiesApi.Destroy(z)) throw new InvalidOperationException("Failed to clean up director zombie"); }
        finally
        {
            if(configBorrowed) { foreach(var p in prior) p.Key.BoxedValue=p.Value; prior.Clear(); Chainloader.PluginInfos[ZombiesApi.PluginGuid].Instance.Config.SaveOnConfigSet=priorAutoSave; configBorrowed=false; }
            if(acquired) { ZombiesApi.EndDirector(); acquired=false; }
        }
        owned.Clear(); originals.Clear(); players.Clear(); born.Clear();
    }
    public void ApplyReplicas(DirectorConfig config,int[] ids)
    {
        if(IsHost) return;
        settings=config;
        // The bite trigger runs on the victim's client. Its cooldown must agree
        // with the host while spawning and AI remain exclusively host-owned.
        BorrowConfig(); Override("Combat","BiteTriggerCooldown",(float)config.BiteCooldown);
        var desired=new HashSet<int>(ids);
        foreach(var zombie in ZombiesApi.GetAlive()) if(zombie!=null && desired.Contains(zombie.photonView.ViewID)) ApplyZombie(zombie);
        foreach(var id in originals.Keys.Where(id=>!desired.Contains(id)).ToArray()) originals.Remove(id);
    }
    public bool TrySpawn(Position position,out int id)
    {
        id=0; if(!IsPlayable) return false;
        var z=ZombiesApi.SpawnAt(ToVector(position),Quaternion.identity,true);
        if(z==null) return false;
        id=z.photonView.ViewID; owned[id]=z; born[id]=Time.time; ApplyZombie(z); return true;
    }
    void ApplyZombie(MushroomZombie z)
    {
        if(z==null) return;
        int id=z.photonView.ViewID;
        if(!originals.TryGetValue(id,out var a)) originals[id]=a=new[]{z.lungeRecoveryTime};
        z.biteInitialInjury=(float)CombatTuning.Injury(settings); z.biteInitialSpores=(float)CombatTuning.Spores(settings);
        z.lungeRecoveryTime=(float)CombatTuning.Recovery(settings,a[0]);
        z.distanceBeforeChase=(float)settings.Awareness; z.zombieLungeDistance=(float)settings.AttackRange;
        if(z.biteColliderObject!=null)
        {
            foreach(var bite in z.biteColliderObject.GetComponentsInChildren<MushroomZombieBiteCollider>(true))
            {
                var relay=bite.GetComponent<BiteContactRelay>();
                if(relay==null) relay=bite.gameObject.AddComponent<BiteContactRelay>();
                relay.Bite=bite; relay.Cooldown=(float)settings.BiteCooldown;
            }
        }
        z.lifetime=settings.RemoveLifetime?3.1536E9f:(float)settings.Lifetime;
    }
    public bool CanCommitLunge(MushroomZombie zombie)
    {
        if(!IsHost || !IsManaged(zombie)) return true;
        var target=zombie.currentTarget;
        if(target==null || target.data==null || target.data.dead || target.isBot || target.isZombie || (!settings.AttackDowned && target.data.fullyPassedOut)) return false;
        var center=ZombieCenter(zombie);
        zombie.distanceToTarget=Vector3.Distance(center,target.Center);
        return CombatTuning.CanCommitLunge(settings,ToPosition(center),ToPosition(target.Center));
    }
    public bool IsManaged(MushroomZombie zombie)=>zombie!=null && originals.ContainsKey(zombie.photonView.ViewID);
    public bool TryDestroy(int id)
    {
        if(!IsHost || !owned.TryGetValue(id,out var z)) return false;
        if(z!=null && !ZombiesApi.Destroy(z)) return false;
        owned.Remove(id); originals.Remove(id); born.Remove(id); return true;
    }
    public void Chase(int id,int playerId,double seconds,bool snap)
    {
        if(!IsHost || !owned.TryGetValue(id,out var z) || z==null || !players.TryGetValue(playerId,out var p)) return;
        if(snap && z.currentState!=MushroomZombie.State.Chasing && z.currentState!=MushroomZombie.State.Lunging && z.currentState!=MushroomZombie.State.LungeRecovery) ZombiesApi.SnapChase(z);
        // SnapChase can assign its own nearest target with a one-second lock.
        // Apply the director selection last, including its full renewal period.
        ZombiesApi.SetTarget(z,p,(float)seconds);
    }
    public IReadOnlyList<MapSpawnPoint> ReadyMapPoints => ZombieManager.Instance==null?Array.Empty<MapSpawnPoint>():ZombieManager.Instance.spawners
        .Where(s=>s!=null && s.isActiveAndEnabled && s.spawnedZombie==null && s.mushroomZombiePrefab!=null && players.Values.Any(p=>Vector3.Distance(p.Center,s.transform.position)<=s.mushroomZombiePrefab.distanceToEnable))
        .Select(s=>new MapSpawnPoint(s.GetInstanceID(),ToPosition(s.transform.position))).ToArray();
    public bool TryValidateMapPoint(Position requested,out Position safe)
    {
        safe=default;var p=ToVector(requested);
        if(!Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,5,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) return false;
        p=hit.point+Vector3.up*.15f;
        if(!Finite(p) || !hit.collider.gameObject.scene.IsValid() || !hit.collider.gameObject.scene.isLoaded || Vector3.Angle(hit.normal,Vector3.up)>45 || players.Values.Any(ch=>Vector3.Distance(ch.Center,p)<3)) return false;
        if(Physics.CheckCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) return false;
        safe=ToPosition(p);return true;
    }
    public bool TryFindSpawn(PlayerSnapshot player,double min,double max,DirectorConfig c,out Position position)
    {
        position=default; if(!IsPlayable || !players.TryGetValue(player.Id,out var target)) return false;
        Vector3? fallback=null;
        for(int i=0;i<c.SearchAttempts;i++)
        {
            float angle=UnityEngine.Random.Range(-Mathf.PI,Mathf.PI);
            var direction=c.PreferBehind && i<c.SearchAttempts/2?-target.transform.forward:Vector3.forward;
            direction=Quaternion.AngleAxis(angle*Mathf.Rad2Deg,Vector3.up)*direction;
            if(c.PreferBehind && i<c.SearchAttempts/2 && Vector3.Dot(direction,target.transform.forward)>0) direction=-direction;
            var sample=target.Center+direction*UnityEngine.Random.Range((float)min,(float)max);
            if(!Physics.Raycast(sample+Vector3.up*20,Vector3.down,out var hit,50,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) continue;
            var p=hit.point+Vector3.up*.15f;
            if(!hit.collider.gameObject.scene.IsValid() || !hit.collider.gameObject.scene.isLoaded || Vector3.Angle(hit.normal,Vector3.up)>45 || !Finite(p)) continue;
            float horizontal=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(target.Center.x,target.Center.z));
            if(horizontal<min || horizontal>max || Math.Abs(p.y-target.Center.y)>25) continue;
            if(Physics.CheckCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) continue;
            if(players.Values.Any(ch=>Vector3.Distance(ch.Center,p)<3)) continue;
            bool visible=false,front=false;
            foreach(var ch in players.Values)
            {
                var eye=ch.Head;
                bool line=!Physics.Linecast(eye,p+Vector3.up,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                bool facing=Vector3.Dot(ch.transform.forward,(p-eye).normalized)>.5f;
                visible|=line && facing; front|=line && Vector3.Dot(ch.transform.forward,(p-eye).normalized)>.85f;
            }
            if(c.AvoidFront && front) continue;
            if(c.PreferHidden && visible) { if(c.VisibleFallback) fallback=p; continue; }
            position=ToPosition(p); return true;
        }
        if(fallback.HasValue) { position=ToPosition(fallback.Value); return true; }
        return false;
    }
    static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
}
