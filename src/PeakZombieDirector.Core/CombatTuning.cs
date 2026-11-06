namespace PeakZombieDirector.Core;
// Values consumed by the existing bite collider, not an alternative damage loop.
public static class CombatTuning
{
    public static bool CanCommitLunge(DirectorConfig c,Position zombie,Position target)
    {
        double distance=zombie.DistanceTo(target);
        return !double.IsNaN(distance) && !double.IsInfinity(distance) && distance<=Math.Min(c.AttackRange,c.LungeContactRange) && Math.Abs(zombie.Y-target.Y)<=c.LungeHeightLimit;
    }
    public static double Injury(DirectorConfig c)=>Math.Min(1,c.BiteInjury/100*c.Damage);
    public static double Spores(DirectorConfig c)=>Math.Min(1,c.BiteSpores/100*c.Damage);
    public static double Recovery(DirectorConfig c,double prefabRecovery)=>prefabRecovery*c.AttackInterval;
}
