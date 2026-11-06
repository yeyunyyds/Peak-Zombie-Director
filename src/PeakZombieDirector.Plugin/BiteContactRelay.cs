using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace PeakZombieDirector;

// Only physical trigger overlaps qualify. No radius scan or separate damage path.
internal sealed class BiteContactRelay : MonoBehaviour
{
    internal MushroomZombieBiteCollider? Bite;
    internal float Cooldown;
    static readonly MethodInfo Enter=AccessTools.Method(typeof(MushroomZombieBiteCollider),"OnTriggerEnter",new[]{typeof(Collider)});
    void OnTriggerStay(Collider other)
    {
        var plugin=Plugin.Instance;var bite=Bite;
        if(plugin==null || bite==null || !bite.enabled || !bite.gameObject.activeInHierarchy || !plugin.World.IsManaged(bite.parentZombie) || bite.parentZombie.currentState!=MushroomZombie.State.Lunging) return;
        if(Time.time-bite.lastBitLocalCharacter<Cooldown) return;
        if(!CharacterRagdoll.TryGetCharacterFromCollider(other,out var victim) || victim==null || !victim.IsLocal || victim.data.dead || victim.isZombie || victim.isBot) return;
        // Reuse the patched, original local-victim callback. Its timestamp is the
        // sole hit cooldown, shared with OnTriggerEnter and all body colliders.
        try { Enter.Invoke(bite,new object[]{other}); }
        catch(Exception error) { enabled=false;plugin.ReportContactFailure(error); }
    }
}
[HarmonyPatch(typeof(MushroomZombie),"TryLunge")]
internal static class CloseLungeGuard
{
    static bool Prefix(MushroomZombie __instance)=>Plugin.Instance?.World.CanCommitLunge(__instance)??true;
}
[HarmonyPatch(typeof(MushroomZombie),"StartLunging")]
internal static class LungeStartGuard
{
    static bool Prefix(MushroomZombie __instance,out bool __state)
    { __state=__instance.currentState!=MushroomZombie.State.Lunging;return Plugin.Instance?.World.CanCommitLunge(__instance)??true; }
    static void Postfix(MushroomZombie __instance,bool __state)
    { if(__state && __instance.currentState==MushroomZombie.State.Lunging) Plugin.Instance?.ReportLunge(__instance); }
}
