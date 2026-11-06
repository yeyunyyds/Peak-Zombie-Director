using System.Reflection;
namespace PeakZombieDirector.Core;
public static class StatusApiCompatibility
{
    public static bool HasSixParameterAddStatus(Type type)=>type.GetMethods(BindingFlags.Instance|BindingFlags.Public)
        .Any(m=>m.Name=="AddStatus" && m.ReturnType==typeof(bool) && Matches(m.GetParameters()));
    static bool Matches(ParameterInfo[] p)=>p.Length==6 && p[0].ParameterType.IsEnum && p[1].ParameterType==typeof(float) && p.Skip(2).All(x=>x.ParameterType==typeof(bool));
}
