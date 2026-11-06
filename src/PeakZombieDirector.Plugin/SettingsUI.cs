using System.Globalization;
using PeakZombieDirector.Core;
using UnityEngine;
using BepInEx.Configuration;
namespace PeakZombieDirector;
// IMGUI is deliberately independent of ModConfig's internal registration helpers.
// All widgets enumerate the same schema used by persistence and runtime logic.
internal sealed class SettingsUI
{
    readonly Plugin plugin;
    readonly Dictionary<string,string> input=new();
    readonly HashSet<string> dropdowns=new();
    Vector2 scroll;
    int tab;
    Rect window;
    bool previousVisible;
    CursorLockMode previousLock;
    string feedback="",shortcut="";
    readonly string[] groups={"Overview","Difficulty","Spawning","Attributes","Pursuit","Performance"};
    readonly string[] chinese={"总览","难度","生成管理","僵尸属性","追击和替换","性能和诊断"};
    public bool Open {get;private set;}
    bool Zh=>plugin.Store.Chinese.Value;
    string L(string zh,string en)=>Zh?zh:en;
    public SettingsUI(Plugin plugin) {this.plugin=plugin;}
    public void Toggle()
    {
        if(Open) { Close(); return; }
        previousLock=Cursor.lockState; previousVisible=Cursor.visible; Open=true;
        shortcut=plugin.Store.Hotkey.Value.ToString(); input.Clear();
    }
    public void Close() { if(!Open) return; Open=false; Cursor.lockState=previousLock; Cursor.visible=previousVisible; }
    public void Draw()
    {
        if(!Open) return;
        float scale=Math.Min(Screen.width/1060f,Screen.height/760f);
        if(scale<=0) return;
        var matrix=GUI.matrix; GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        window=new Rect((Screen.width/scale-1020)/2,(Screen.height/scale-720)/2,1020,720);
        GUI.depth=-10000;
        GUI.Window(784311,window,DrawWindow,"PEAK Zombie Director  "+Plugin.Version);
        GUI.matrix=matrix;
    }
    void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        if(GUILayout.Button(Zh?"English":"中文",GUILayout.Width(100))) {plugin.Store.Chinese.Value=!Zh; plugin.Store.Save();}
        GUILayout.Label(plugin.Status);
        if(GUILayout.Button("×",GUILayout.Width(40))) Close();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUI.skin.box,GUILayout.Width(170));
        for(int i=0;i<groups.Length;i++) if(GUILayout.Toggle(tab==i,Zh?chinese[i]:groups[i],GUI.skin.button,GUILayout.Height(44)) && tab!=i) {tab=i;scroll=Vector2.zero;}
        GUILayout.FlexibleSpace();
        GUILayout.Label(plugin.CanEdit?L("房主 / 可编辑","Host / editable"):L("客户端 / 只读","Client / read only"));
        GUILayout.EndVertical();
        GUILayout.BeginVertical(); scroll=GUILayout.BeginScrollView(scroll,GUILayout.Height(590));
        if(tab==0) Overview();
        if(tab==1) DifficultyPage();
        if(tab==5) Diagnostics();
        if(tab!=1) foreach(var d in Settings.All.Where(d=>d.Meta.Group==groups[tab])) Row(d);
        GUILayout.EndScrollView();
        GUILayout.Label(feedback);
        if(!string.IsNullOrEmpty(plugin.Error)) GUILayout.Label(plugin.Error);
        GUILayout.EndVertical(); GUILayout.EndHorizontal();
    }
    void Overview()
    {
        var d=plugin.Director; var host=plugin.CanEdit;
        GUILayout.Label(L("难度：","Difficulty: ")+(host?plugin.Store.State.Selected:plugin.RemoteDifficulty)+(plugin.Store.State.Dirty && host?L("（未保存自定义）"," (unsaved Custom)"):""));
        GUILayout.Label($"{L("存活","Alive")}: {(host?plugin.World.TotalAlive:plugin.RemoteAlive)}   {L("每波","Wave")}: {DifficultyScaler.Wave(plugin.Display,host?d.Elapsed:plugin.RemoteElapsed)}");
        GUILayout.Label($"{L("阶段时间","Stage time")}: {(host?d.Elapsed:plugin.RemoteElapsed):F1}s   {L("下一波","Next wave")}: {(host?d.NextWaveIn:plugin.RemoteNext):F1}s");
        GUILayout.Label(L("导演独占生成；原版和依赖 Mod 的地图/定时生成暂停。","Director owns spawning; vanilla and dependency map/timer spawns pause."));
        GUILayout.Label(L("覆盖全部已加载生态区。地面和碰撞验证始终开启。","All loaded biomes; ground and collision validation is always enabled."));
        GUILayout.Label(L("设置在下一次更新生效。寿命从原始生成时间计算。","Changes apply on next tick; lifetime is measured from original spawn time."));
        GUILayout.BeginHorizontal(); GUILayout.Label(L("快捷键","Hotkey"),GUILayout.Width(100)); shortcut=GUILayout.TextField(shortcut);
        if(GUILayout.Button(L("保存","Save"),GUILayout.Width(70)))
        {
            try { plugin.Store.Hotkey.Value=KeyboardShortcut.Deserialize(shortcut); plugin.Store.Save(); feedback=L("已保存快捷键","Hotkey saved"); }
            catch(Exception) { feedback=L("快捷键无效，例如 F8 + LeftControl","Invalid shortcut; example: F8 + LeftControl"); }
        }
        GUILayout.EndHorizontal();
    }
    void DifficultyPage()
    {
        bool enabled=GUI.enabled; GUI.enabled=plugin.CanEdit;
        foreach(Difficulty difficulty in Enum.GetValues(typeof(Difficulty)))
        {
            var c=DifficultyPresets.Get(difficulty);
            if(GUILayout.Button(difficulty==Difficulty.Custom?"Custom / 自定义":$"{difficulty} — {c.InitialCount} initial, {c.WaveCount} / {c.WaveInterval}s, cap {c.MaxAlive}",GUILayout.Height(40)))
            {plugin.Store.State.Select(difficulty);Save();}
        }
        GUILayout.Space(15);
        if(GUILayout.Button(L("恢复当前预设默认值 / Custom 恢复 Normal","Restore preset defaults / reset Custom to Normal"))) {plugin.Store.State.Restore();Save();}
        if(GUILayout.Button(L("保存当前配置为 Custom","Save current configuration as Custom"))) {plugin.Store.State.SaveCustom();Save();}
        GUILayout.Label(L("所有难度均可编辑全部参数。切换预设不会覆盖已保存的 Custom。","Every parameter remains editable; selecting a preset preserves saved Custom."));
        GUI.enabled=enabled;
    }
    void Diagnostics()
    {
        var d=plugin.Director.Diagnostics;
        GUILayout.Label($"New: {d.Spawned}  Replaced: {d.Replaced}  Spawn failures: {d.SpawnFailures}");
        GUILayout.Label($"Search failures: {d.SearchFailures}  Delete failures: {d.DeleteFailures}  Tick: {d.LastMilliseconds:F2} ms");
        GUILayout.Label(L("以上计数为本机房主统计，客户端只接收总览。","Counters are local host statistics; clients receive overview only."));
        if(plugin.Display.MaxAlive==0 || plugin.World.TotalAlive>=plugin.Display.WarningCount) GUILayout.Label(L("高数量可能降低物理、CPU 和网络性能。","High counts may reduce physics, CPU and network performance."));
    }
    void Save() {input.Clear();plugin.Changed(); feedback=L("已自动保存，下一次更新生效","Saved automatically; applies on next tick");}
    void Row(SettingDefinition d)
    {
        var c=plugin.Display; object value=d.Property.GetValue(c)!;
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label((Zh?d.Meta.Zh:d.Key)+(d.Property.PropertyType==typeof(double)||d.Property.PropertyType==typeof(int)?$" [{d.Meta.Min}–{d.Meta.Max} {d.Meta.Unit}]":""));
        bool enabled=GUI.enabled; GUI.enabled=plugin.CanEdit;
        object? changed=null;
        if(value is bool b) {bool v=GUILayout.Toggle(b,L("开启","Enabled"));if(v!=b) changed=v;}
        else if(d.Property.PropertyType.IsEnum)
        {
            string[] names=Enum.GetNames(d.Property.PropertyType); int index=Array.IndexOf(names,value.ToString());
            if(GUILayout.Button(names[index]+"  ▾")) { if(!dropdowns.Add(d.Key)) dropdowns.Remove(d.Key); }
            if(dropdowns.Contains(d.Key)) foreach(var name in names) if(GUILayout.Button(name)) { changed=Enum.Parse(d.Property.PropertyType,name); dropdowns.Remove(d.Key); }
        }
        else
        {
            double current=Convert.ToDouble(value,CultureInfo.InvariantCulture);
            GUILayout.BeginHorizontal();
            float slid=GUILayout.HorizontalSlider((float)current,(float)d.Meta.Min,(float)d.Meta.Max,GUILayout.Width(390));
            if(Math.Abs(slid-current)>Math.Max(.0001,Math.Abs(current)*.000001)) changed=d.Property.PropertyType==typeof(int)?(object)(int)Math.Round(slid):(double)Math.Round(slid,2);
            if(!input.ContainsKey(d.Key)) input[d.Key]=current.ToString(CultureInfo.InvariantCulture);
            input[d.Key]=GUILayout.TextField(input[d.Key],GUILayout.Width(120));
            if(GUILayout.Button(L("应用","Apply"),GUILayout.Width(65)))
            {
                if(double.TryParse(input[d.Key],NumberStyles.Float,CultureInfo.InvariantCulture,out var n) && !double.IsInfinity(n) && !double.IsNaN(n)) changed=d.Property.PropertyType==typeof(int)?(object)(int)Math.Round(Math.Max(d.Meta.Min,Math.Min(d.Meta.Max,n))):Math.Max(d.Meta.Min,Math.Min(d.Meta.Max,n));
                else feedback=L("请输入合法数字","Enter a valid number");
            }
            GUILayout.EndHorizontal();
        }
        GUI.enabled=enabled; GUILayout.EndVertical();
        if(changed!=null && plugin.CanEdit) { plugin.Store.State.Edit(config=>d.Property.SetValue(config,changed));Save(); }
    }
}
