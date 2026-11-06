using System.Globalization;
namespace PeakZombieDirector.Core;
public static class ConfigCodec
{
    public static string Encode(DirectorConfig config) => string.Join("\n",Settings.All.Select(d=>d.Key+"="+Convert.ToString(d.Property.GetValue(config),CultureInfo.InvariantCulture)));
    public static DirectorConfig Decode(string text,Action<string>? warning=null)
    {
        var config=new DirectorConfig();
        foreach(var line in text.Split('\n'))
        {
            int sep=line.IndexOf('='); if(sep<0) continue;
            var d=Settings.All.FirstOrDefault(x=>x.Key==line.Substring(0,sep)); if(d==null) continue;
            try { Set(config,d,line.Substring(sep+1)); } catch(ArgumentException) { warning?.Invoke(d.Key); } catch(FormatException) { warning?.Invoke(d.Key); } catch(OverflowException) { warning?.Invoke(d.Key); }
        }
        config.Validate(warning); return config;
    }
    public static void Set(DirectorConfig config,SettingDefinition d,string text)
    {
        object value=d.Property.PropertyType.IsEnum?Enum.Parse(d.Property.PropertyType,text):Convert.ChangeType(text,d.Property.PropertyType,CultureInfo.InvariantCulture);
        d.Property.SetValue(config,value);
    }
}
