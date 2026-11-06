using System.IO;
using BepInEx;
using HarmonyLib;
using PeakZombieDirector.Core;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombies_Anywhere;
namespace PeakZombieDirector;
[BepInPlugin(Guid,"PEAK Zombie Director",Version)]
[BepInDependency("tony4twenty.PEAK_Zombies","2.0.0")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Guid="yeyunyyds.PEAKZombieDirector",Version="0.1.1";
    internal static Plugin? Instance;
    internal readonly ZombieDirector Director=new();
    internal ConfigStore Store=null!;
    internal ZombieApiAdapter World=new();
    internal SettingsUI Panel=null!;
    internal DirectorConfig? Remote;
    internal Difficulty RemoteDifficulty;
    internal string Status="Initializing",Error="";
    internal int RemoteAlive;
    int[] remoteOwned=Array.Empty<int>();
    double lastRemoteTime;
    internal double RemoteElapsed,RemoteNext;
    internal bool MigrationStopped;
    internal bool CanEdit=>!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    internal DirectorConfig Display=>CanEdit?Store.State.Active:Remote??new DirectorConfig {Enabled=false};
    Harmony? harmony;
    double nextTick,nextSync;
    Room? room;
    int host;
    long reportedReplacements;
    const byte SyncEvent=184;
    void Awake()
    {
        Instance=this;
        Store=new ConfigStore(Path.Combine(Paths.ConfigPath,"PEAKZombieDirector.cfg"),key=>Logger.LogWarning("Corrected invalid setting: "+key));
        World.ReportUnexpectedRemoval=message=>Logger.LogWarning(message);
        Panel=new SettingsUI(this);
        harmony=new Harmony(Guid); harmony.PatchAll(typeof(Plugin).Assembly);
        PhotonNetwork.NetworkingClient.EventReceived+=OnPhotonEvent;
        SceneManager.activeSceneChanged+=OnSceneChanged;
        Logger.LogInfo("PEAK Zombie Director loaded. Ctrl+F8 opens settings. Requires API 2.0.0.");
    }
    void Update()
    {
        if(Store.Hotkey.Value.IsDown()) Panel.Toggle();
        if(Panel.Open) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        double now=Time.realtimeSinceStartup;
        if(now<nextTick) return; nextTick=now+Store.State.Active.TickInterval;
        try
        {
            var current=PhotonNetwork.CurrentRoom; var actor=PhotonNetwork.MasterClient?.ActorNumber??0;
            if(current!=room) { Stop(false); room=current; host=actor; MigrationStopped=false; Remote=null; }
            else if(host!=actor) { Stop(false); host=actor; MigrationStopped=true; Remote=null; }
            if(!CanEdit)
            {
                if(Remote!=null && now-lastRemoteTime>6) {Remote=null;Status="Host settings are stale";}
                else Status=Remote==null?"Waiting for host settings":"Client / read only";
                if(Remote!=null && World.Ready) World.ApplyReplicas(Remote,remoteOwned);
                return;
            }
            if(MigrationStopped) { if(World.Ready && World.IsHost) World.Acquire(Store.State.Active); Status="Host changed: stopped until next scene / new round"; Broadcast(now); return; }
            if(!World.Ready) { Stop(true); Status="PEAK Zombies API missing or incompatible (requires 2.0.0)"; return; }
            var c=Store.State.Active;
            World.Refresh(c);
            if(!c.Enabled || !World.IsPlayable) { Stop(true); Status=c.Enabled?"Lobby / waiting for climb":"Disabled"; Broadcast(now); return; }
            World.Acquire(c);
            Director.Tick(now,c,World); Status=Director.Diagnostics.Status;
            if(Director.Diagnostics.Replaced!=reportedReplacements) { reportedReplacements=Director.Diagnostics.Replaced; Logger.LogInfo($"Director replacements: total={reportedReplacements}, far={Director.Diagnostics.FarReplacements}, stuck={Director.Diagnostics.StuckReplacements}"); }
            if(c.Verbose) Logger.LogDebug($"alive={World.TotalAlive}, pending={Director.Pending}, elapsed={Director.Elapsed:F1}");
            Broadcast(now);
        }
        catch(Exception e) { Error=e.GetType().Name+": "+e.Message; Logger.LogError(e); try { Stop(true); } catch(Exception cleanup) { Logger.LogError(cleanup); } Status="Stopped after error"; MigrationStopped=true; }
    }
    void Stop(bool cleanup)
    {
        World.Release(cleanup);
        Director.Reset();
    }
    void OnSceneChanged(Scene oldScene,Scene newScene)
    {
        try { Stop(true); MigrationStopped=false; Remote=null; nextTick=0; }
        catch(Exception e) { Error=e.Message; Logger.LogError(e); MigrationStopped=true; }
    }
    void Broadcast(double now)
    {
        if(!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || now<nextSync) return;
        nextSync=now+2;
        object[] data={Guid,Version,ConfigCodec.Encode(Store.State.Active),(int)Store.State.Selected,World.TotalAlive,Director.Elapsed,Director.NextWaveIn,Status,World.OwnedZombies.Select(z=>z.Id).ToArray()};
        PhotonNetwork.RaiseEvent(SyncEvent,data,new RaiseEventOptions {Receivers=ReceiverGroup.Others},SendOptions.SendReliable);
    }
    void OnPhotonEvent(EventData e)
    {
        if(e.Code!=SyncEvent) return;
        if(!HostSnapshot.TryRead(e.Sender,PhotonNetwork.MasterClient?.ActorNumber??0,PhotonNetwork.IsMasterClient,e.CustomData,Guid,Version,out var snapshot,out var error))
        { if(error.Length>0) {Error=error;Remote=null;} return; }
        lastRemoteTime=Time.realtimeSinceStartup; remoteOwned=snapshot!.Owned;
        Remote=snapshot.Config; RemoteDifficulty=snapshot.Difficulty; RemoteAlive=snapshot.Alive;
        RemoteElapsed=snapshot.Elapsed; RemoteNext=snapshot.NextWave; Status=snapshot.Status;
    }

    internal void Changed()
    {
        Store.Save(); nextSync=0;
        // Apply on next director tick; nothing reads UI widget state.
    }
    void OnGUI()=>Panel?.Draw();
    void OnDestroy()
    {
        PhotonNetwork.NetworkingClient.EventReceived-=OnPhotonEvent;
        SceneManager.activeSceneChanged-=OnSceneChanged;
        try { Stop(true); } catch(Exception e) { Logger.LogError(e); }
        Panel?.Close(); harmony?.UnpatchSelf(); Instance=null;
    }
}
[HarmonyPatch(typeof(Character),"CanDoInput")]
internal static class BlockGameInput
{
    static void Postfix(Character __instance,ref bool __result) { if(Plugin.Instance?.Panel?.Open==true && __instance.IsLocal && !__instance.isBot && !__instance.isZombie) __result=false; }
}
[HarmonyPatch(typeof(CursorHandler),"Update")]
internal static class PanelCursor
{
    static void Postfix() { if(Plugin.Instance?.Panel?.Open==true) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; } }
}

// The game exposes these as getters with private setters. Getter patches avoid
// private-field writes and leave the game's window bookkeeping intact.
[HarmonyPatch(typeof(GUIManager),nameof(GUIManager.windowBlockingInput),MethodType.Getter)]
internal static class PanelBlocking
{
    static void Postfix(ref bool __result) { if(Plugin.Instance?.Panel?.Open==true) __result=true; }
}
[HarmonyPatch(typeof(GUIManager),nameof(GUIManager.windowShowingCursor),MethodType.Getter)]
internal static class PanelShowingCursor
{
    static void Postfix(ref bool __result) { if(Plugin.Instance?.Panel?.Open==true) __result=true; }
}
