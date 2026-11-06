# 配置参考

下面由统一配置模型生成；UI、运行时与配置持久化使用相同属性。数值范围见最后一列；枚举使用已定义值。Normal 是基础默认值，其他预设只覆盖表后列出的差异。所有设置在下一次导演更新应用。

| 分组 | 键 | 中文 | 类型 | Normal 默认 | 范围/单位 |
|---|---|---|---|---|---|
| Overview | Enabled | 启用导演 | bool | true |  |
| Spawning | MapSpawns | 使用原版地图生成点（导演接管） | bool | false |  |
| Spawning | AllBiomes | 所有生态区（关闭时仅 Roots） | bool | true |  |
| Spawning | Waves | 玩家周围定时生成 | bool | true |  |
| Spawning | InitialCount | 初始数量 | int | 5 | 0, 10000 |
| Spawning | WaveCount | 每波数量 | int | 2 | 0, 10000 |
| Spawning | WaveInterval | 生成间隔 | double | 22 | 1, 3600, s |
| Spawning | FirstDelay | 首次延迟 | double | 15 | 0, 600, s |
| Spawning | SpawnMin | 最小生成距离 | double | 15 | 3, 300, m |
| Spawning | SpawnMax | 最大生成距离 | double | 30 | 3, 300, m |
| Spawning | MaxAlive | 最大存活数（0 无限） | int | 18 | 0, 10000 |
| Spawning | WaveGrowth | 每波数量增长 | bool | true |  |
| Spawning | GrowthPeriod | 数量增长周期 | double | 180 | 1, 7200, s |
| Spawning | GrowthAmount | 每次增长数量 | int | 1 | 0, 1000 |
| Spawning | GrowthLimit | 每波增长上限 | int | 20 | 1, 10000 |
| Spawning | CapGrowth | 存活上限增长 | bool | false |  |
| Spawning | CapGrowthPeriod | 上限增长周期 | double | 180 | 1, 7200, s |
| Spawning | CapGrowthAmount | 上限增长幅度 | int | 2 | 0, 1000 |
| Spawning | RandomVariation | 随机波动比例 | double | 0 | 0, .9 |
| Attributes | Walk | 行走倍率 | double | 1 | 0, 10, × |
| Attributes | Sprint | 奔跑倍率 | double | 1.2 | 0, 10, × |
| Attributes | Climb | 攀爬倍率 | double | 1 | 0, 10, × |
| Attributes | Jump | 跳跃倍率 | double | 1 | 0, 10, × |
| Attributes | Lunge | 扑击速度倍率 | double | 1 | 0, 10, × |
| Attributes | Damage | 咬击伤害倍率 | double | 1 | 0, 10, × |
| Attributes | AttackInterval | 扑击恢复间隔倍率 | double | .15 | .1, 10, × |
| Attributes | Awareness | 追击感知距离 | double | 100 | 1, 500, m |
| Attributes | AttackRange | 扑击触发范围 | double | 3 | 1, 30, m |
| Attributes | RemoveLifetime | 禁用寿命限制 | bool | true |  |
| Attributes | Lifetime | 生命持续时间 | double | 120 | 1, 86400, s |
| Attributes | NeverCull | 忽略远距离清理 | bool | true |  |
| Attributes | AttackDowned | 追击倒地玩家 | bool | false |  |
| Pursuit | Persistent | 持续追击 | bool | true |  |
| Pursuit | Retarget | 允许重选目标 | bool | true |  |
| Pursuit | Target | 目标选择 | TargetMode | TargetMode.Nearest |  |
| Pursuit | TargetInterval | 目标评估间隔 | double | 2 | .2, 60, s |
| Pursuit | SearchInterval | 目标丢失搜索间隔 | double | 1 | .2, 60, s |
| Pursuit | Recycle | 启用超距替换 | bool | true |  |
| Pursuit | Distance | 距离检测模式 | DistanceMode | 0 |  |
| Pursuit | CheckInterval | 检查间隔 | double | 2 | .2, 60, s |
| Pursuit | RecycleDistance | 触发距离 | double | 85 | 10, 1000, m |
| Pursuit | FarDuration | 超距持续时间 | double | 3 | 0, 120, s |
| Pursuit | RecycleMin | 替换最小距离 | double | 15 | 3, 300, m |
| Pursuit | RecycleMax | 替换最大距离 | double | 30 | 3, 300, m |
| Pursuit | RecycleCooldown | 替换冷却 | double | 10 | 1, 600, s |
| Pursuit | PreferHidden | 优先视线外生成 | bool | true |  |
| Pursuit | PreferBehind | 优先玩家身后 | bool | true |  |
| Pursuit | AvoidFront | 避免视野正中央 | bool | true |  |
| Pursuit | VisibleFallback | 允许视线内回退 | bool | false |  |
| Pursuit | StuckRecovery | 卡住后替换 | bool | true |  |
| Pursuit | StuckTime | 卡住时间 | double | 20 | 2, 600, s |
| Pursuit | MinProgress | 最小进展 | double | 1 | .1, 20, m |
| Performance | WaveBudget | 每波最大数量 | int | 40 | 1, 1000 |
| Performance | RecycleBudget | 每轮最大替换数 | int | 5 | 1, 100 |
| Performance | NetworkBudget | 每秒网络生成预算 | int | 5 | 1, 100 |
| Performance | SearchAttempts | 每次位置搜索次数 | int | 20 | 1, 100 |
| Performance | TickInterval | 更新间隔 | double | .2 | .05, 5, s |
| Performance | WarningCount | 数量警告阈值 | int | 60 | 1, 10000 |
| Performance | Verbose | 详细日志 | bool | false |  |

`Target`: Nearest / Random / PreferOriginal。`Distance`: Spatial / Horizontal。

| 预设 | 初始 | 每波 | 间隔 s | cap | 奔跑 | 替换距离 m | 增长周期 s | 增长数 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 3 | 1 | 30 | 10 | 1 | 100 | 240 | 1 |
| Normal | 5 | 2 | 22 | 18 | 1.2 | 85 | 180 | 1 |
| Advanced | 8 | 3 | 15 | 28 | 1.5 | 75 | 120 | 1 |
| Expert | 12 | 4 | 10 | 40 | 1.8 | 65 | 90 | 2 |

其余参数均从完整 Normal 默认配置复制，任何预设不隐藏参数。`Saved Custom` 区域是单独存档，活动编辑自动保存但只有“保存 Custom”覆盖它。`Interface` 保存本机 Chinese 与 Hotkey，`Difficulty` 保存当前档位和是否有未保存的 Custom 编辑。

0.1.2 新增 Attributes 参数（自动持久化并显示在 UI）：

| 键 | 含义 | 默认 | 范围 |
|---|---|---:|---|
| LungeStumble | 扑击落地倒地时间 | 0.75s | 0–10s |
| BiteSelfStumble | 命中后僵尸倒地时间 | 0.75s | 0–10s |
| BiteCooldown | 同一僵尸咬击冷却 | 1.5s | 0.1–30s |
| BiteInjury | 咬击初始受伤 | 10% | 0–100% |
| BiteSpores | 咬击初始孢子 | 5% | 0–100% |

旧配置的 AttackRange=10 和 AttackInterval=1 不会被强行覆盖。请按需改为 3 和 0.15，或选择预设加载新默认值。
