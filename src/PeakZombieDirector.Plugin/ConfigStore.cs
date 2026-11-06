using BepInEx.Configuration;
using PeakZombieDirector.Core;
using System.Globalization;
using UnityEngine;
namespace PeakZombieDirector;
internal sealed class ConfigStore
{
    readonly ConfigFile file;
    readonly Dictionary<string,ConfigEntry<string>> active=new(), custom=new();
    readonly ConfigEntry<Difficulty> difficulty;
    readonly ConfigEntry<bool> dirty;
    public readonly ConfigEntry<bool> Chinese;
    public readonly ConfigEntry<KeyboardShortcut> Hotkey;
    public ConfigurationState State { get; private set; }
    public ConfigStore(string path,Action<string> warn)
    {
        file=new ConfigFile(path,true); file.SaveOnConfigSet=false;
        difficulty=file.Bind("Difficulty","Selected",Difficulty.Normal,"Preset name. Manual edits select Custom.");
        dirty=file.Bind("Difficulty","UnsavedCustom",false,"Current edits differ from saved Custom.");
        Chinese=file.Bind("Interface","Chinese",true,"中文 / English (local preference)");
        Hotkey=file.Bind("Interface","Hotkey",new KeyboardShortcut(KeyCode.F8,KeyCode.LeftControl),"Toggle panel (local preference)");
        var defaults=new DirectorConfig();
        foreach(var d in Settings.All)
        {
            var value=Convert.ToString(d.Property.GetValue(defaults),CultureInfo.InvariantCulture);
            var description=$"{d.Meta.Zh} / {d.Key}; range {d.Meta.Min}..{d.Meta.Max} {d.Meta.Unit}. Live; map spawning is exclusively controlled by Director. 0 MaxAlive = unlimited.";
            active[d.Key]=file.Bind(d.Meta.Group,d.Key,value,description);
            custom[d.Key]=file.Bind("Saved Custom",d.Key,value,description);
        }
        var a=ConfigCodec.Decode(string.Join("\n",active.Select(p=>p.Key+"="+p.Value.Value)),warn);
        var c=ConfigCodec.Decode(string.Join("\n",custom.Select(p=>p.Key+"="+p.Value.Value)),warn);
        State=new ConfigurationState(difficulty.Value,a,c);
        if(dirty.Value) State.Edit(_=>{});
        Save();
    }
    public void Save()
    {
        difficulty.Value=State.Selected; dirty.Value=State.Dirty;
        foreach(var d in Settings.All) { active[d.Key].Value=Convert.ToString(d.Property.GetValue(State.Active),CultureInfo.InvariantCulture); custom[d.Key].Value=Convert.ToString(d.Property.GetValue(State.Custom),CultureInfo.InvariantCulture); }
        file.Save();
    }
}
