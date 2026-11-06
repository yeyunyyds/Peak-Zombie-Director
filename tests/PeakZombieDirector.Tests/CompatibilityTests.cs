using PeakZombieDirector.Core;
using Xunit;
namespace PeakZombieDirector.Tests;
public sealed class CompatibilityTests
{
    public enum Status { Injury, Spores }
    public class CurrentGame
    { public bool AddStatus(Status s,float amount,bool rpc=false,bool effects=true,bool notify=true)=>true; }
    public class LegacyGame
    { public bool AddStatus(Status s,float amount,bool rpc=false,bool effects=true,bool notify=true,bool extra=false)=>true; }
    public class WrongOverload
    { public bool AddStatus(int s,float amount,bool a,bool b,bool c,bool d)=>true; }
    public class WrongReturn
    { public void AddStatus(Status s,float amount,bool a,bool b,bool c,bool d){} }
    [Fact] public void FiveParameterGameRequiresLegacyBitePatchWorkaround()
    {Assert.False(StatusApiCompatibility.HasSixParameterAddStatus(typeof(CurrentGame)));}
    [Fact] public void MatchingLegacyGameKeepsDependencyBitePatch()
    {Assert.True(StatusApiCompatibility.HasSixParameterAddStatus(typeof(LegacyGame)));}
    [Fact] public void SimilarButIncompatibleSignaturesAreNotAccepted()
    {Assert.False(StatusApiCompatibility.HasSixParameterAddStatus(typeof(WrongOverload)));Assert.False(StatusApiCompatibility.HasSixParameterAddStatus(typeof(WrongReturn)));}
}
