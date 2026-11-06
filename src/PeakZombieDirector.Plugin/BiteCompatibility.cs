using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using PeakZombieDirector.Core;
using UnityEngine;
namespace PeakZombieDirector;

// Work around the exact incompatible dependency patch, never the whole mod.
internal sealed class BiteCompatibility
{
    readonly MethodInfo target=AccessTools.Method(typeof(MushroomZombieBiteCollider),"OnTriggerEnter",new[]{typeof(Collider)});
    Patch? removed;
    public bool Enabled=>removed!=null;
    public void Install(Harmony harmony,Action<string> report)
    {
        if(StatusApiCompatibility.HasSixParameterAddStatus(typeof(CharacterAfflictions))) return;
        var broken=Harmony.GetPatchInfo(target)?.Prefixes.Where(p=>p.owner=="tony4twenty.ZombiesAnywhere.Patches" && p.PatchMethod.DeclaringType?.FullName=="Zombies_Anywhere.ZombiesAnywhereMod+BiteColliderPatch" && p.PatchMethod.Name=="Prefix").ToArray();
        if(broken==null || broken.Length!=1) {report("Bite compatibility: six-parameter AddStatus is unavailable, but the exact PEAK Zombies bite prefix was not found. No unrelated patches were removed.");return;}
        removed=broken[0];
        harmony.Unpatch(target,removed.PatchMethod);
        report("Bite compatibility enabled: removed only PEAK Zombies 2.0 BiteColliderPatch.Prefix, which references unavailable six-parameter AddStatus. Native game bite callback restored; other dependency patches retained.");
    }
    public void Restore()
    {
        if(removed==null)return;
        if(Harmony.GetPatchInfo(target)?.Prefixes.Any(p=>p.PatchMethod==removed.PatchMethod)!=true)
            new Harmony(removed.owner).Patch(target,prefix:new HarmonyMethod(removed.PatchMethod){priority=removed.priority,before=removed.before,after=removed.after});
        removed=null;
    }
}
[HarmonyPatch(typeof(MushroomZombieBiteCollider),"OnTriggerEnter")]
internal static class NativeBiteCooldown
{
    static bool Prepare()=>Plugin.Instance?.Compatibility.Enabled==true;
    static float Seconds(MushroomZombieBiteCollider bite)=>Plugin.Instance?.World.BiteCooldownFor(bite.parentZombie)??5f;
    // The verified native handler uses one 5-second constant. Keep all of its
    // damage/status logic, substituting only the configurable cooldown source.
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code=instructions.ToList();
        var matches=code.Where(i=>i.opcode==OpCodes.Ldc_R4 && i.operand is float n && n==5f).ToArray();
        if(matches.Length!=1) throw new InvalidOperationException("Native bite cooldown IL changed; expected exactly one 5-second constant.");
        var constant=matches[0];
        foreach(var instruction in code)
        {
            if(!ReferenceEquals(instruction,constant)){yield return instruction;continue;}
            instruction.opcode=OpCodes.Ldarg_0;instruction.operand=null;
            yield return instruction; // Preserve original branch labels and exception blocks.
            yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(NativeBiteCooldown),nameof(Seconds)));
        }
    }
}
