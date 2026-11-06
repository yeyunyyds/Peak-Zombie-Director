using System.Globalization;
using System.Reflection;

namespace PeakZombieDirector.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingAttribute : Attribute
{
    public string Group { get; }
    public string Zh { get; }
    public string Unit { get; }
    public double Min { get; }
    public double Max { get; }
    public SettingAttribute(string group, string zh, double min = 0, double max = 1, string unit = "")
    { Group = group; Zh = zh; Min = min; Max = max; Unit = unit; }
}
public enum Difficulty { Easy, Normal, Advanced, Expert, Custom }
public enum TargetMode { Nearest, Random, PreferOriginal }
public enum DistanceMode { Spatial, Horizontal }
public sealed class DirectorConfig
{
    [Setting("Overview", "启用导演")] public bool Enabled { get; set; } = true;
    [Setting("Spawning", "使用原版地图生成点（导演接管）")] public bool MapSpawns { get; set; }
    [Setting("Spawning", "所有生态区（关闭时仅 Roots）")] public bool AllBiomes { get; set; } = true;
    [Setting("Spawning", "玩家周围定时生成")] public bool Waves { get; set; } = true;
    [Setting("Spawning", "初始数量", 0, 10000)] public int InitialCount { get; set; } = 5;
    [Setting("Spawning", "每波数量", 0, 10000)] public int WaveCount { get; set; } = 2;
    [Setting("Spawning", "生成间隔", 1, 3600, "s")] public double WaveInterval { get; set; } = 22;
    [Setting("Spawning", "首次延迟", 0, 600, "s")] public double FirstDelay { get; set; } = 15;
    [Setting("Spawning", "最小生成距离", 3, 300, "m")] public double SpawnMin { get; set; } = 15;
    [Setting("Spawning", "最大生成距离", 3, 300, "m")] public double SpawnMax { get; set; } = 30;
    [Setting("Spawning", "最大存活数（0 无限）", 0, 10000)] public int MaxAlive { get; set; } = 18;
    [Setting("Spawning", "每波数量增长")] public bool WaveGrowth { get; set; } = true;
    [Setting("Spawning", "数量增长周期", 1, 7200, "s")] public double GrowthPeriod { get; set; } = 180;
    [Setting("Spawning", "每次增长数量", 0, 1000)] public int GrowthAmount { get; set; } = 1;
    [Setting("Spawning", "每波增长上限", 1, 10000)] public int GrowthLimit { get; set; } = 20;
    [Setting("Spawning", "存活上限增长")] public bool CapGrowth { get; set; }
    [Setting("Spawning", "上限增长周期", 1, 7200, "s")] public double CapGrowthPeriod { get; set; } = 180;
    [Setting("Spawning", "上限增长幅度", 0, 1000)] public int CapGrowthAmount { get; set; } = 2;
    [Setting("Spawning", "随机波动比例", 0, .9)] public double RandomVariation { get; set; }
    [Setting("Attributes", "行走倍率", 0, 10, "×")] public double Walk { get; set; } = 1;
    [Setting("Attributes", "奔跑倍率", 0, 10, "×")] public double Sprint { get; set; } = 1.2;
    [Setting("Attributes", "攀爬倍率", 0, 10, "×")] public double Climb { get; set; } = 1;
    [Setting("Attributes", "跳跃倍率", 0, 10, "×")] public double Jump { get; set; } = 1;
    [Setting("Attributes", "扑击速度倍率", 0, 10, "×")] public double Lunge { get; set; } = 1;
    [Setting("Attributes", "咬击伤害倍率", 0, 10, "×")] public double Damage { get; set; } = 1;
    [Setting("Attributes", "扑击恢复间隔倍率", .1, 10, "×")] public double AttackInterval { get; set; } = 1;
    [Setting("Attributes", "追击感知距离", 1, 500, "m")] public double Awareness { get; set; } = 100;
    [Setting("Attributes", "扑击触发范围", 1, 30, "m")] public double AttackRange { get; set; } = 10;
    [Setting("Attributes", "禁用寿命限制")] public bool RemoveLifetime { get; set; } = true;
    [Setting("Attributes", "生命持续时间", 1, 86400, "s")] public double Lifetime { get; set; } = 120;
    [Setting("Attributes", "忽略远距离清理")] public bool NeverCull { get; set; } = true;
    [Setting("Attributes", "追击倒地玩家")] public bool AttackDowned { get; set; }
    [Setting("Pursuit", "持续追击")] public bool Persistent { get; set; } = true;
    [Setting("Pursuit", "允许重选目标")] public bool Retarget { get; set; } = true;
    [Setting("Pursuit", "目标选择")] public TargetMode Target { get; set; } = TargetMode.Nearest;
    [Setting("Pursuit", "目标评估间隔", .2, 60, "s")] public double TargetInterval { get; set; } = 2;
    [Setting("Pursuit", "目标丢失搜索间隔", .2, 60, "s")] public double SearchInterval { get; set; } = 1;
    [Setting("Pursuit", "启用超距替换")] public bool Recycle { get; set; } = true;
    [Setting("Pursuit", "距离检测模式")] public DistanceMode Distance { get; set; }
    [Setting("Pursuit", "检查间隔", .2, 60, "s")] public double CheckInterval { get; set; } = 2;
    [Setting("Pursuit", "触发距离", 10, 1000, "m")] public double RecycleDistance { get; set; } = 85;
    [Setting("Pursuit", "超距持续时间", 0, 120, "s")] public double FarDuration { get; set; } = 3;
    [Setting("Pursuit", "替换最小距离", 3, 300, "m")] public double RecycleMin { get; set; } = 15;
    [Setting("Pursuit", "替换最大距离", 3, 300, "m")] public double RecycleMax { get; set; } = 30;
    [Setting("Pursuit", "替换冷却", 1, 600, "s")] public double RecycleCooldown { get; set; } = 10;
    [Setting("Pursuit", "优先视线外生成")] public bool PreferHidden { get; set; } = true;
    [Setting("Pursuit", "优先玩家身后")] public bool PreferBehind { get; set; } = true;
    [Setting("Pursuit", "避免视野正中央")] public bool AvoidFront { get; set; } = true;
    [Setting("Pursuit", "允许视线内回退")] public bool VisibleFallback { get; set; }
    [Setting("Pursuit", "卡住后替换")] public bool StuckRecovery { get; set; } = true;
    [Setting("Pursuit", "卡住时间", 2, 600, "s")] public double StuckTime { get; set; } = 20;
    [Setting("Pursuit", "最小进展", .1, 20, "m")] public double MinProgress { get; set; } = 1;
    [Setting("Performance", "每波最大数量", 1, 1000)] public int WaveBudget { get; set; } = 40;
    [Setting("Performance", "每轮最大替换数", 1, 100)] public int RecycleBudget { get; set; } = 5;
    [Setting("Performance", "每秒网络生成预算", 1, 100)] public int NetworkBudget { get; set; } = 5;
    [Setting("Performance", "每次位置搜索次数", 1, 100)] public int SearchAttempts { get; set; } = 20;
    [Setting("Performance", "更新间隔", .05, 5, "s")] public double TickInterval { get; set; } = .2;
    [Setting("Performance", "数量警告阈值", 1, 10000)] public int WarningCount { get; set; } = 60;
    [Setting("Performance", "详细日志")] public bool Verbose { get; set; }
    public DirectorConfig Copy() => (DirectorConfig)MemberwiseClone();
    public void Validate(Action<string>? warning = null)
    {
        foreach (var d in Settings.All)
        {
            var value = d.Property.GetValue(this);
            if (d.Property.PropertyType.IsEnum)
            { if (!Enum.IsDefined(d.Property.PropertyType, value!)) { d.Property.SetValue(this, d.Property.GetValue(new DirectorConfig())); warning?.Invoke(d.Key); } }
            else if (value is double || value is int)
            {
                double n = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                double fixedValue = double.IsNaN(n) || double.IsInfinity(n) ? Convert.ToDouble(d.Property.GetValue(new DirectorConfig())) : Math.Max(d.Meta.Min, Math.Min(d.Meta.Max, n));
                if (n != fixedValue) { d.SetNumber(this, fixedValue); warning?.Invoke(d.Key); }
            }
        }
        if (SpawnMax < SpawnMin) { SpawnMax = SpawnMin; warning?.Invoke(nameof(SpawnMax)); }
        if (RecycleMax < RecycleMin) { RecycleMax = RecycleMin; warning?.Invoke(nameof(RecycleMax)); }
    }
}
public sealed class SettingDefinition
{
    public PropertyInfo Property { get; }
    public SettingAttribute Meta { get; }
    public string Key => Property.Name;
    public SettingDefinition(PropertyInfo property, SettingAttribute meta) { Property = property; Meta = meta; }
    public void SetNumber(DirectorConfig config, double value)
    { if (Property.PropertyType == typeof(int)) Property.SetValue(config, (int)Math.Round(value)); else Property.SetValue(config, value); }
}
public static class Settings
{
    public static readonly SettingDefinition[] All = typeof(DirectorConfig).GetProperties().Select(p => new { P = p, A = p.GetCustomAttribute<SettingAttribute>() }).Where(x => x.A != null).Select(x => new SettingDefinition(x.P, x.A!)).ToArray();
}
public static class DifficultyPresets
{
    public static DirectorConfig Get(Difficulty difficulty)
    {
        var c = new DirectorConfig();
        switch (difficulty)
        {
            case Difficulty.Easy: c.InitialCount=3; c.WaveCount=1; c.WaveInterval=30; c.MaxAlive=10; c.Sprint=1; c.RecycleDistance=100; c.GrowthPeriod=240; break;
            case Difficulty.Advanced: c.InitialCount=8; c.WaveCount=3; c.WaveInterval=15; c.MaxAlive=28; c.Sprint=1.5; c.RecycleDistance=75; c.GrowthPeriod=120; break;
            case Difficulty.Expert: c.InitialCount=12; c.WaveCount=4; c.WaveInterval=10; c.MaxAlive=40; c.Sprint=1.8; c.RecycleDistance=65; c.GrowthPeriod=90; c.GrowthAmount=2; break;
        }
        return c;
    }
}
public sealed class ConfigurationState
{
    public Difficulty Selected { get; private set; }
    public DirectorConfig Active { get; private set; }
    public DirectorConfig Custom { get; private set; }
    public bool Dirty { get; private set; }
    public ConfigurationState(Difficulty selected, DirectorConfig active, DirectorConfig custom) { Selected=selected; Active=active.Copy(); Custom=custom.Copy(); Active.Validate(); Custom.Validate(); }
    public void Select(Difficulty d) { Selected=d; Active=d==Difficulty.Custom?Custom.Copy():DifficultyPresets.Get(d); Dirty=false; }
    public void Edit(Action<DirectorConfig> edit) { edit(Active); Active.Validate(); Selected=Difficulty.Custom; Dirty=true; }
    public void SaveCustom() { Custom=Active.Copy(); Dirty=false; }
    public void Restore() { Active=DifficultyPresets.Get(Selected==Difficulty.Custom?Difficulty.Normal:Selected); Dirty=Selected==Difficulty.Custom; }
}
