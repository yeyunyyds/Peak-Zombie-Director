using PeakZombieDirector.Core;
using Xunit;
namespace PeakZombieDirector.Tests;
public sealed class CombatTests
{
    [Fact] public void DefaultsUseCloseLungeAndShortSeparateRecoveries()
    {var c=DifficultyPresets.Get(Difficulty.Normal);Assert.Equal(3,c.AttackRange);Assert.Equal(.75,c.LungeStumble);Assert.Equal(.75,CombatTuning.Recovery(c,5));Assert.Equal(.75,c.BiteSelfStumble);}
    [Fact] public void DamageIsExplicitAndDoesNotDependOnZeroPrefabValues()
    {var c=new DirectorConfig();Assert.Equal(.10,CombatTuning.Injury(c),6);Assert.Equal(.05,CombatTuning.Spores(c),6);c.Damage=2;Assert.Equal(.20,CombatTuning.Injury(c),6);}
    [Fact] public void ZeroDamageIsRespectedAndRepeatedApplicationDoesNotCompound()
    {var c=new DirectorConfig{Damage=0};Assert.Equal(0,CombatTuning.Injury(c));c.Damage=2;double first=CombatTuning.Injury(c);Assert.Equal(first,CombatTuning.Injury(c));c.BiteInjury=0;Assert.Equal(0,CombatTuning.Injury(c));}
    [Fact] public void OldConfigsKeepUserValuesAndReceiveNewCombatFields()
    {var c=ConfigCodec.Decode("AttackRange=10\nAttackInterval=1\nDamage=0");Assert.Equal(10,c.AttackRange);Assert.Equal(1,c.AttackInterval);Assert.Equal(0,CombatTuning.Injury(c));Assert.Equal(.75,c.LungeStumble);Assert.Equal(1.5,c.BiteCooldown);}
    [Theory] [InlineData(Difficulty.Easy)] [InlineData(Difficulty.Normal)] [InlineData(Difficulty.Advanced)] [InlineData(Difficulty.Expert)]
    public void PresetsShareCombatTuningAndRoundtrip(Difficulty d)
    {var c=ConfigCodec.Decode(ConfigCodec.Encode(DifficultyPresets.Get(d)));Assert.Equal(3,c.AttackRange);Assert.Equal(.1,CombatTuning.Injury(c),6);Assert.Equal(.15,c.AttackInterval);}
}
