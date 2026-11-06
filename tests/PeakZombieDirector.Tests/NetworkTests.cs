using PeakZombieDirector.Core;
using Xunit;
namespace PeakZombieDirector.Tests;
public sealed class NetworkTests
{
    static object[] Payload()=>new object[]{"director","1",ConfigCodec.Encode(new DirectorConfig{Sprint=2.8}),(int)Difficulty.Custom,7,30d,12d,"Running",new[]{11,12}};
    [Fact] public void OnlyCurrentMasterSnapshotIsAccepted()
    {Assert.False(HostSnapshot.TryRead(3,2,false,Payload(),"director","1",out _,out _));Assert.True(HostSnapshot.TryRead(2,2,false,Payload(),"director","1",out var s,out _));Assert.Equal(2.8,s!.Config.Sprint);Assert.Equal(7,s.Alive);}
    [Fact] public void ClientCannotOverwriteHostSettings()
    {Assert.False(HostSnapshot.TryRead(3,2,true,Payload(),"director","1",out _,out _));Assert.False(HostSnapshot.TryRead(2,2,true,Payload(),"director","1",out _,out _));}
    [Fact] public void IncompatibleVersionIsExplicitlyRejected()
    {Assert.False(HostSnapshot.TryRead(2,2,false,Payload(),"director","2",out _,out var error));Assert.Contains("version mismatch",error);}
    [Fact] public void LateJoinReceivesFullConfigWithoutPresetReconstruction()
    {var p=Payload();Assert.True(HostSnapshot.TryRead(2,2,false,p,"director","1",out var s,out _));Assert.Equal(Difficulty.Custom,s!.Difficulty);Assert.Equal(2.8,s.Config.Sprint);Assert.Equal(new[]{11,12},s.Owned);}
    [Fact] public void MalformedPayloadDoesNotChangeState()
    {var p=Payload();p[5]=double.NaN;Assert.False(HostSnapshot.TryRead(2,2,false,p,"director","1",out _,out _));Assert.False(HostSnapshot.TryRead(2,2,false,new object[0],"director","1",out _,out _));}
}
