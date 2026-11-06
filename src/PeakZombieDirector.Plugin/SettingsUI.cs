using System.Globalization;
using PeakZombieDirector.Core;
using UnityEngine;
using BepInEx.Configuration;
namespace PeakZombieDirector;

internal sealed class SettingsUI
{
    readonly Plugin plugin;
    readonly Dictionary<string,string> input=new();
    readonly HashSet<string> dropdowns=new();
    readonly List<Texture2D> textures=new();
    Vector2 scroll;
    int tab;
    bool previousVisible;
    CursorLockMode previousLock;
    string feedback="",shortcut="",search="";
    float feedbackUntil;
    GUISkin? skin;
    GUIStyle title=null!,subtitle=null!,card=null!,nav=null!,navActive=null!,accent=null!,muted=null!,valueStyle=null!,section=null!,pill=null!,warning=null!,toggleOn=null!;
    readonly string[] groups={"Overview","Difficulty","Spawning","Attributes","Pursuit","Performance"};
    readonly string[] chinese={"总览","难度预设","生成管理","僵尸属性","追击与替换","性能与诊断"};
    readonly string[] english={"Overview","Difficulty","Spawning","Zombie attributes","Pursuit & recycling","Performance"};
    static readonly Color Ink=new(.91f,.94f,.96f), Dim=new(.55f,.63f,.69f), Teal=new(.39f,.86f,.72f);
    public bool Open {get;private set;}
    bool Zh=>plugin.Store.Chinese.Value;
    string L(string zh,string en)=>Zh?zh:en;
    public SettingsUI(Plugin plugin) {this.plugin=plugin;}
    public void Toggle()
    {
        if(Open) {Close();return;}
        previousLock=Cursor.lockState;previousVisible=Cursor.visible;Open=true;
        shortcut=plugin.Store.Hotkey.Value.ToString();input.Clear();
    }
    public void Close() {if(!Open)return;Open=false;Cursor.lockState=previousLock;Cursor.visible=previousVisible;}
    public void Dispose()
    {
        Close();foreach(var texture in textures) UnityEngine.Object.Destroy(texture);
        textures.Clear();if(skin!=null) UnityEngine.Object.Destroy(skin);skin=null;
    }
    Texture2D Surface(Color color,int radius=6)
    {
        const int size=20;var pixels=new Color[size*size];
        for(int y=0;y<size;y++) for(int x=0;x<size;x++)
        {
            float dx=Math.Max(radius-x,Math.Max(0,x-(size-1-radius)));
            float dy=Math.Max(radius-y,Math.Max(0,y-(size-1-radius)));
            pixels[y*size+x]=dx*dx+dy*dy>radius*radius?Color.clear:color;
        }
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();texture.hideFlags=HideFlags.HideAndDontSave;textures.Add(texture);return texture;
    }
    GUIStyle Text(int size,Color color,bool bold=false)
    {
        var style=new GUIStyle(skin!.label){fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,wordWrap=true};style.normal.textColor=color;return style;
    }
    GUIStyle Box(Color color,int padding)
    {
        var style=new GUIStyle(skin!.box){border=new RectOffset(7,7,7,7),padding=new RectOffset(padding,padding,padding,padding),margin=new RectOffset(0,0,0,10)};
        style.normal.background=Surface(color);return style;
    }
    void BuildTheme()
    {
        if(skin!=null)return;
        skin=UnityEngine.Object.Instantiate(GUI.skin);skin.hideFlags=HideFlags.HideAndDontSave;
        skin.label.fontSize=15;skin.label.normal.textColor=Ink;skin.label.wordWrap=true;
        skin.label.padding=new RectOffset(0,0,3,3);
        var button=skin.button;button.fontSize=14;button.fontStyle=FontStyle.Normal;
        button.border=new RectOffset(7,7,7,7);button.padding=new RectOffset(14,14,9,9);button.margin=new RectOffset(0,0,0,0);
        button.normal.background=Surface(new Color(.16f,.21f,.25f));button.normal.textColor=Ink;
        button.hover.background=Surface(new Color(.21f,.29f,.33f));button.hover.textColor=Color.white;
        button.active.background=Surface(new Color(.24f,.36f,.38f));button.active.textColor=Teal;
        button.focused.background=button.hover.background;button.focused.textColor=Ink;
        var field=skin.textField;field.fontSize=14;field.alignment=TextAnchor.MiddleLeft;field.padding=new RectOffset(10,10,8,8);field.border=new RectOffset(7,7,7,7);
        field.normal.background=Surface(new Color(.065f,.09f,.115f));field.normal.textColor=Ink;
        field.focused.background=Surface(new Color(.11f,.18f,.21f));field.focused.textColor=Teal;
        field.hover.background=field.normal.background;field.hover.textColor=Ink;
        skin.settings.cursorColor=Teal;skin.settings.selectionColor=new Color(.15f,.4f,.36f);
        skin.horizontalSlider.normal.background=Surface(new Color(.08f,.12f,.15f),3);
        skin.horizontalSlider.border=new RectOffset(4,4,4,4);skin.horizontalSlider.fixedHeight=6;skin.horizontalSlider.margin=new RectOffset(0,0,14,10);
        skin.horizontalSliderThumb.normal.background=Surface(Teal,8);skin.horizontalSliderThumb.hover.background=Surface(new Color(.62f,1,.85f),8);
        skin.horizontalSliderThumb.active.background=skin.horizontalSliderThumb.hover.background;
        skin.horizontalSliderThumb.fixedWidth=16;skin.horizontalSliderThumb.fixedHeight=16;skin.horizontalSliderThumb.margin=new RectOffset(0,0,-5,0);
        skin.verticalScrollbar.fixedWidth=8;skin.verticalScrollbar.normal.background=Surface(new Color(.065f,.09f,.11f),3);
        skin.verticalScrollbarThumb.normal.background=Surface(new Color(.26f,.34f,.38f),3);skin.verticalScrollbarThumb.border=new RectOffset(4,4,4,4);
        skin.verticalScrollbarUpButton.fixedHeight=0;skin.verticalScrollbarDownButton.fixedHeight=0;
        skin.window=Box(new Color(.075f,.10f,.125f),22);skin.window.padding=new RectOffset(22,22,20,16);
        title=Text(25,Ink,true);subtitle=Text(12,Dim);muted=Text(12,Dim);section=Text(19,Ink,true);valueStyle=Text(27,Teal,true);
        card=Box(new Color(.11f,.145f,.175f),16);
        nav=new GUIStyle(button){alignment=TextAnchor.MiddleLeft,fontSize=14,padding=new RectOffset(14,12,13,13)};
        nav.normal.background=null;nav.hover.background=Surface(new Color(.12f,.18f,.21f));
        navActive=new GUIStyle(nav);navActive.normal.background=Surface(new Color(.13f,.26f,.25f));navActive.normal.textColor=Teal;
        accent=new GUIStyle(button);accent.normal.background=Surface(new Color(.19f,.43f,.36f));accent.normal.textColor=new Color(.81f,1,.92f);
        accent.hover.background=Surface(new Color(.23f,.53f,.44f));accent.hover.textColor=Color.white;
        toggleOn=new GUIStyle(accent){fixedWidth=76};
        pill=Text(11,Teal,true);pill.alignment=TextAnchor.MiddleCenter;pill.normal.background=Surface(new Color(.12f,.25f,.23f));pill.border=new RectOffset(7,7,7,7);pill.padding=new RectOffset(10,10,6,6);
        warning=Text(12,new Color(1,.74f,.44f));
    }
    public void Draw()
    {
        if(!Open)return;
        BuildTheme();
        float scale=Math.Min(1.25f,Math.Min(Screen.width/1080f,Screen.height/780f));if(scale<=0)return;
        var matrix=GUI.matrix;var oldSkin=GUI.skin;var oldColor=GUI.color;int oldDepth=GUI.depth;
        try
        {
            GUI.depth=-10000;GUI.color=new Color(0,0,0,.68f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.skin=skin;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            var rect=new Rect((Screen.width/scale-1040)/2,(Screen.height/scale-740)/2,1040,740);
            GUI.Window(784311,rect,DrawWindow,GUIContent.none);
        }
        finally {GUI.matrix=matrix;GUI.skin=oldSkin;GUI.color=oldColor;GUI.depth=oldDepth;}
    }
    void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();GUILayout.Label("PEAK / ZOMBIE DIRECTOR",subtitle);GUILayout.Label(L("掌控每一轮追击","Direct the pursuit"),title);GUILayout.EndVertical();
        GUILayout.FlexibleSpace();GUILayout.BeginVertical(GUILayout.Width(215));GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        if(GUILayout.Button(Zh?"EN / English":"中文 / ZH",GUILayout.Width(145),GUILayout.Height(34))){plugin.Store.Chinese.Value=!Zh;plugin.Store.Save();}
        GUILayout.Space(8);if(GUILayout.Button("×",GUILayout.Width(40),GUILayout.Height(34)))Close();
        GUILayout.EndHorizontal();GUILayout.EndVertical();GUILayout.EndHorizontal();
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(184),GUILayout.Height(548));
        GUILayout.Label(L("导演控制台","DIRECTOR CONSOLE"),subtitle);GUILayout.Space(12);
        for(int i=0;i<groups.Length;i++)
        {
            if(GUILayout.Button($"{i+1:00}   "+(Zh?chinese[i]:english[i]),tab==i?navActive:nav,GUILayout.Height(46)))
            {if(tab!=i){tab=i;scroll=Vector2.zero;search="";dropdowns.Clear();GUI.FocusControl(null);}}
            GUILayout.Space(4);
        }
        GUILayout.FlexibleSpace();GUILayout.Label(plugin.CanEdit?L("房主 · 可编辑","HOST · EDITABLE"):L("客户端 · 只读","CLIENT · READ ONLY"),pill);
        GUILayout.Space(8);GUILayout.Label("v"+Plugin.Version+"  /  "+plugin.Store.Hotkey.Value,subtitle);
        GUILayout.EndVertical();GUILayout.Space(24);
        GUILayout.BeginVertical(GUILayout.Width(786));
        GUILayout.BeginHorizontal();GUILayout.Label(Zh?chinese[tab]:english[tab],section);GUILayout.FlexibleSpace();
        if(tab!=1){GUILayout.Label(L("搜索","Filter"),muted,GUILayout.Width(48));search=GUILayout.TextField(search,GUILayout.Width(200),GUILayout.Height(30));}
        GUILayout.EndHorizontal();
        GUILayout.Label(PageDescription(),subtitle);GUILayout.Space(12);
        scroll=GUILayout.BeginScrollView(scroll,false,false,GUILayout.Height(471));
        if(tab==0)Overview();if(tab==1)DifficultyPage();if(tab==5)Diagnostics();
        if(tab!=1)
        {
            var definitions=Settings.All.Where(d=>d.Meta.Group==groups[tab] && (search.Length==0 || (d.Meta.Zh+" "+d.Key).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            foreach(var d in definitions)Row(d);
            if(definitions.Length==0 && search.Length>0)GUILayout.Label(L("没有匹配的设置","No matching settings"),muted);
        }
        GUILayout.EndScrollView();GUILayout.EndVertical();GUILayout.EndHorizontal();
        GUILayout.Space(12);GUILayout.BeginHorizontal();
        GUILayout.Label("●  "+plugin.Status,subtitle,GUILayout.Width(430));GUILayout.FlexibleSpace();
        GUILayout.Label(Time.realtimeSinceStartup<feedbackUntil?feedback:L("修改自动保存 · 下一次更新生效","Auto-saved · Applies on next tick"),Time.realtimeSinceStartup<feedbackUntil?pill:subtitle);
        GUILayout.EndHorizontal();
        if(!string.IsNullOrEmpty(plugin.Error))GUILayout.Label(plugin.Error,warning);
    }
    string PageDescription()=>tab switch
    {
        0=>L("房间状态、追击压力与本机偏好","Room status, pursuit pressure and local preferences"),
        1=>L("预设是起点，每一个参数都可以自由调整","Start with a preset, then make every parameter your own"),
        2=>L("控制波次、生成距离与随时间增长的压力","Control waves, spawn distance and pressure over time"),
        3=>L("调整移动、扑击、伤害与生命周期","Tune movement, lunges, bite damage and lifetime"),
        4=>L("保持有效目标，在必要时安全替换僵尸","Keep valid targets and safely recycle distant or stuck zombies"),
        _=>L("限制处理预算，查看本机导演诊断","Limit work budgets and inspect local diagnostics")
    };
    void Stat(string label,string value)
    {GUILayout.BeginVertical(card,GUILayout.Width(246));GUILayout.Label(label,subtitle);GUILayout.Label(value,valueStyle);GUILayout.EndVertical();}
    void Overview()
    {
        bool host=plugin.CanEdit;double elapsed=host?plugin.Director.Elapsed:plugin.RemoteElapsed;
        GUILayout.BeginHorizontal();Stat(L("当前存活","LIVE ZOMBIES"),(host?plugin.World.TotalAlive:plugin.RemoteAlive).ToString());GUILayout.Space(10);
        Stat(L("每波数量","PER WAVE"),DifficultyScaler.Wave(plugin.Display,elapsed).ToString());GUILayout.Space(10);
        Stat(L("下一波","NEXT WAVE"),(host?plugin.Director.NextWaveIn:plugin.RemoteNext).ToString("F1")+" s");GUILayout.EndHorizontal();
        GUILayout.BeginVertical(card);
        GUILayout.Label(L("当前难度","ACTIVE DIFFICULTY"),subtitle);
        GUILayout.Label(DifficultyName(host?plugin.Store.State.Selected:plugin.RemoteDifficulty),section);
        if(host && plugin.Store.State.Dirty)GUILayout.Label(L("当前编辑尚未存入 Custom 存档","Current edits have not been stored in the Custom slot"),warning);
        GUILayout.Label(L("阶段时间：","Stage time: ")+TimeSpan.FromSeconds(Math.Max(0,elapsed)).ToString(@"hh\:mm\:ss"),muted);
        GUILayout.EndVertical();
        GUILayout.BeginVertical(card);GUILayout.Label(L("本机快捷键","LOCAL SHORTCUT"),subtitle);GUILayout.Space(6);
        GUILayout.BeginHorizontal();shortcut=GUILayout.TextField(shortcut,GUILayout.Height(34));GUILayout.Space(10);
        if(GUILayout.Button(L("保存快捷键","Save shortcut"),GUILayout.Width(145),GUILayout.Height(34)))
        {
            try{plugin.Store.Hotkey.Value=KeyboardShortcut.Deserialize(shortcut);plugin.Store.Save();Notice(L("快捷键已保存","Shortcut saved"));}
            catch(Exception){Notice(L("格式示例：F8 + LeftControl","Example: F8 + LeftControl"));}
        }
        GUILayout.EndHorizontal();GUILayout.EndVertical();
    }
    string DifficultyName(Difficulty d)=>d switch
    {
        Difficulty.Easy=>L("简单","Easy"),Difficulty.Normal=>L("正常","Normal"),Difficulty.Advanced=>L("高级","Advanced"),Difficulty.Expert=>L("专家","Expert"),_=>L("自定义","Custom")
    };
    void DifficultyPage()
    {
        bool enabled=GUI.enabled;GUI.enabled=plugin.CanEdit;
        foreach(Difficulty difficulty in Enum.GetValues(typeof(Difficulty)))
        {
            var c=difficulty==Difficulty.Custom?plugin.Store.State.Custom:DifficultyPresets.Get(difficulty);
            GUILayout.BeginVertical(card);GUILayout.BeginHorizontal();GUILayout.BeginVertical();
            GUILayout.Label(DifficultyName(difficulty),section);
            GUILayout.Label($"{L("初始","Initial")} {c.InitialCount}    /    {L("每波","Wave")} {c.WaveCount}    /    {c.WaveInterval}s    /    {L("上限","Cap")} {(c.MaxAlive==0?"∞":c.MaxAlive.ToString())}",muted);
            GUILayout.EndVertical();GUILayout.FlexibleSpace();
            bool selected=plugin.Store.State.Selected==difficulty;
            if(GUILayout.Button(selected?L("已选择","Selected"):L("载入预设","Load"),selected?accent:skin!.button,GUILayout.Width(115),GUILayout.Height(38)))
            {plugin.Store.State.Select(difficulty);input.Clear();Save();}
            GUILayout.EndHorizontal();GUILayout.EndVertical();
        }
        GUILayout.BeginHorizontal();
        if(GUILayout.Button(L("恢复当前默认值","Restore defaults"),GUILayout.Height(38))){plugin.Store.State.Restore();input.Clear();Save();}
        GUILayout.Space(10);
        if(GUILayout.Button(L("保存到 Custom","Store as Custom"),accent,GUILayout.Height(38))){plugin.Store.State.SaveCustom();Save();}
        GUILayout.EndHorizontal();GUILayout.Space(10);
        GUILayout.Label(L("载入预设不会覆盖已保存的 Custom。Custom 的恢复默认值以 Normal 为基础。","Loading presets preserves saved Custom. Restoring Custom uses Normal defaults."),muted);
        GUI.enabled=enabled;
    }
    void Diagnostics()
    {
        var d=plugin.Director.Diagnostics;
        GUILayout.BeginHorizontal();Stat(L("累计新增","SPAWNED"),d.Spawned.ToString());GUILayout.Space(10);Stat(L("累计替换","RECYCLED"),d.Replaced.ToString());GUILayout.Space(10);Stat(L("上轮耗时","LAST TICK"),d.LastMilliseconds.ToString("F2")+" ms");GUILayout.EndHorizontal();
        GUILayout.BeginVertical(card);
        GUILayout.Label($"{L("位置搜索失败","Position search failures")}: {d.SearchFailures}    /    {L("生成失败","Spawn failures")}: {d.SpawnFailures}    /    {L("删除失败","Delete failures")}: {d.DeleteFailures}",muted);
        GUILayout.Label(L("以上是本机房主统计，客户端只接收总览。","These are local host counters; clients receive the overview only."),muted);
        if(plugin.Display.MaxAlive==0 || plugin.World.TotalAlive>=plugin.Display.WarningCount)GUILayout.Label(L("高数量会增加物理、CPU 和网络负担。","High counts increase physics, CPU and network workload."),warning);
        GUILayout.EndVertical();
    }
    void Notice(string message){feedback=message;feedbackUntil=Time.realtimeSinceStartup+4;}
    void Save(){plugin.Changed();Notice(L("已保存","Saved"));}
    void Row(SettingDefinition d)
    {
        object value=d.Property.GetValue(plugin.Display)!;object? changed=null;
        GUILayout.BeginVertical(card);GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(430));GUILayout.Label(Zh?d.Meta.Zh:d.Key);
        bool number=value is int || value is double;
        GUILayout.Label(number?$"{d.Meta.Min} – {d.Meta.Max} {d.Meta.Unit}":d.Property.PropertyType.IsEnum?L("选择一种行为","Choose a behavior"):L("实时应用","Applies live"),muted);GUILayout.EndVertical();GUILayout.FlexibleSpace();
        bool enabled=GUI.enabled;GUI.enabled=plugin.CanEdit;
        if(value is bool b)
        {
            if(GUILayout.Button(b?L("开启","ON"):L("关闭","OFF"),b?toggleOn:skin!.button,GUILayout.Width(76),GUILayout.Height(32)))changed=!b;
        }
        else if(d.Property.PropertyType.IsEnum)
        {
            if(GUILayout.Button(EnumLabel(value),GUILayout.Width(235),GUILayout.Height(34))){if(!dropdowns.Add(d.Key))dropdowns.Remove(d.Key);}
        }
        else
        {
            double current=Convert.ToDouble(value,CultureInfo.InvariantCulture);
            if(!input.ContainsKey(d.Key))input[d.Key]=current.ToString("0.###",CultureInfo.InvariantCulture);
            input[d.Key]=GUILayout.TextField(input[d.Key],GUILayout.Width(116),GUILayout.Height(32));GUILayout.Space(8);
            if(GUILayout.Button(L("应用","Apply"),GUILayout.Width(68),GUILayout.Height(32)))
            {
                if(double.TryParse(input[d.Key],NumberStyles.Float,CultureInfo.InvariantCulture,out var n) && !double.IsNaN(n) && !double.IsInfinity(n))
                {n=Math.Max(d.Meta.Min,Math.Min(d.Meta.Max,n));changed=d.Property.PropertyType==typeof(int)?(object)(int)Math.Round(n):n;}
                else Notice(L("请输入有效数字","Enter a valid number"));
            }
        }
        GUILayout.EndHorizontal();
        if(number)
        {
            double current=Convert.ToDouble(value,CultureInfo.InvariantCulture);
            float slid=GUILayout.HorizontalSlider((float)current,(float)d.Meta.Min,(float)d.Meta.Max);
            if(Math.Abs(slid-current)>Math.Max(.0001,Math.Abs(current)*.000001))changed=d.Property.PropertyType==typeof(int)?(object)(int)Math.Round(slid):(double)Math.Round(slid,2);
        }
        if(d.Property.PropertyType.IsEnum && dropdowns.Contains(d.Key))
        {
            GUILayout.Space(8);
            foreach(var option in Enum.GetValues(d.Property.PropertyType))
                if(GUILayout.Button(EnumLabel(option),Equals(value,option)?accent:skin!.button,GUILayout.Height(32))){changed=option;dropdowns.Remove(d.Key);}
        }
        GUI.enabled=enabled;GUILayout.EndVertical();
        if(changed!=null && plugin.CanEdit){plugin.Store.State.Edit(config=>d.Property.SetValue(config,changed));input.Remove(d.Key);Save();}
    }
    string EnumLabel(object value)=>value switch
    {
        TargetMode.Nearest=>L("最近存活玩家","Nearest living player"),TargetMode.Random=>L("随机存活玩家","Random living player"),TargetMode.PreferOriginal=>L("优先原目标","Prefer original target"),
        DistanceMode.Spatial=>L("三维直线距离","3D distance"),DistanceMode.Horizontal=>L("水平距离","Horizontal distance"),_=>value.ToString()
    };
}
