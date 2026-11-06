# API 与依赖研究

研究依据为下载到的实际文件与仓库固定提交，不根据网页描述猜测方法。

| 资源 | 固定版本/提交 | 结果 |
|---|---|---|
| [PEAK Zombies](https://thunderstore.io/c/peak/p/tony4twenty/PEAK_Zombies/v/2.0.0/) | 2.0.0 | ZIP 内 `Zombies Anywhere.dll`；用 ILSpy 9.1.0.7988 检查导出和相关实现 |
| [ShootZombies](https://github.com/xiaofe12/ShootZombies) | `2f2f844408c25588c22a95c0383d91336e7d04e8` | MIT；研究房主控制、僵尸类型、输入与 Photon 接口，并使用其公开 libs 作编译参考；未复制实现 |
| [PEAKLib](https://github.com/PEAKModding/PEAKLib) | `0f112221a591a5c756d8ecb4c0405829f43935ff` | MIT；查看 UI 与 ModConfig 源码。当前未引入运行时依赖 |
| [ModConfig](https://thunderstore.io/c/peak/p/PEAKModding/ModConfig/) | 对应上述 PEAKLib 源码 | 自动注册 BepInEx 配置入口；具体 `SettingsHandlerUtility.Add*ToTab` 为 internal。复杂预设存档、实时诊断和客户端只读约束采用独立面板，未调用其内部 API |

## 实际调用的 PEAK Zombies 公共 API

类型为 `Zombies_Anywhere.ZombiesApi`；版本字符串为 `2.0.0`，插件 GUID 为 `tony4twenty.PEAK_Zombies`。

- `bool IsReady`, `string Version`, `bool InAirport`, `bool DirectorActive`, `int AliveCount`。
- `void BeginDirector()` / `void EndDirector()`：引用计数式接管，暂停依赖自动生成，阻止原版/Mod 地图生成器的 `ReadyToSpawn`；不是清空所有僵尸。插件严格配对释放，遇到其他导演已接管则停止。
- `MushroomZombie SpawnAt(Vector3, Quaternion, bool ignoreCap=false)`：依赖内部走 Photon 网络生成。导演统一管理 cap，显式 `ignoreCap=true`；替换可在 cap 处先生成再删除。
- `bool Destroy(MushroomZombie)`：只允许房主且对象的 PhotonView 属于本机，内部调用 Photon 销毁。
- `List<MushroomZombie> GetAlive()`。
- `void SetTarget(MushroomZombie, Character, float forceSeconds=3)`：更新目标、强制时间与目标 RPC。
- `void SnapChase(MushroomZombie)`：唤醒并沿用游戏追击 AI；不在扑击/恢复中反复强制状态。

未直接调用 dependency 的私有方法，未改写其他 Mod DLL。`FindGround` 虽有导出，但缺少失败信号；本插件使用 Unity Physics 查询并显式验证地面、坡度、碰撞、视线与加载场景。

## 配置桥接与 Harmony 的必要性

公共 API 没有行走/奔跑/攀爬/跳跃/扑击倍率 setter。通过 BepInEx `Chainloader.PluginInfos[guid].Instance.Config` 查找**已有**配置项，缺项即抛出明确错误，而不是创建同名假配置：

- `Around Players/Enabled`、`Vanilla Style/Enabled` 禁止双重生成。
- `General/DisableZombies`、`General/ForceZombies` 允许导演生成。
- `Culling/NeverDespawnFarZombies`。
- `Zombie Behavior/RemoveZombieLifetime`、`ZombieLifetimeOverride`、`AttackUnconsciousPlayers`。
- `Zombie Stats/WalkSpeedMultiplier`、`SprintSpeedMultiplier`、`ClimbSpeedMultiplier`、`JumpImpulseMultiplier`、`LungeSpeedMultiplier`。

正常接管期间暂时关闭依赖配置的自动写盘，退出时恢复原值与原自动保存状态。其他 Mod 或依赖菜单仍可能主动保存，这是需要实机验证的边界。相关移动倍率是依赖的全局 NPC 设置，不能宣称只影响本插件的僵尸。

已从实际 `Assembly-CSharp.dll` 核实公共字段：`biteInitialInjury`, `biteInitialSpores`, `lungeRecoveryTime`, `distanceBeforeChase`, `zombieLungeDistance`, `lifetime`。倍率始终依据每只对象的原始值计算，不重复相乘。房主广播配置与自己创建的 Photon IDs；客户端只将伤害等实例字段同步到对应对象，不执行生成和 AI。

Harmony 仅用于面板输入隔离：`Character.CanDoInput`、`CursorHandler.Update`、`GUIManager.windowBlockingInput` / `windowShowingCursor` 的 getter。GUIManager 两项的 setter 是 private，因此用 getter postfix 合并本面板状态，不通过猜测字段名称写入。没有修改寻路或重写移动 AI。

原版地图点通过 `ZombieManager.Instance.spawners` 读取，导演自行消费点并调用 `SpawnAt`，不会临时释放导演锁来调用可能重复生成的系统。固定点同样经过地面/碰撞检查并共享预算。

## 校验与许可证边界

- PEAK Zombies ZIP SHA-256: `905a369dad057f5f6f7ecf246ca092793a986e7b4ff0844c64b94d4af7a75e3d`。
- 其中 DLL SHA-256: `77ade23775a1971e7cbcafb4b0ecd913feb34f8748c6ee35f723b8b97b17e26d`。
- 本次编译参考 `Assembly-CSharp.dll` SHA-256: `d47a746afabffa48ca2a98ffec01fe755465510fbf3648ca6a7c5d00c3cb6a06`。
- ShootZombies 固定提交 tar SHA-256: `bd6b2c008cd68798f498cad15eb85549eb49891d6ec887c74c6329c30b0d8417`。

PEAK Zombies 2.0.0 压缩包没有附许可证，manifest 的 website_url 为空。仅检查其公开接口并声明安装依赖；不复制实现、不分发其 DLL。游戏与 Photon/Unity DLL 也不放进本仓库或发布 ZIP。ShootZombies 与 PEAKLib 的 MIT 许可证不能自动授予游戏 DLL 的再分发权。云环境的参考文件只作开发研究；发行者须用自己合法游戏安装的程序集复核兼容性。

BepInEx/Harmony 为外部运行依赖；包不重新分发这两者。依赖版本严格锁定到 2.0.0；新版本须重新检查 API、配置项及生命周期行为后再放开版本支持。

### 0.1.2 战斗适配

核实依赖 `DoLunging` postfix 在进入 LungeRecovery 时设置 `character.data.fallSeconds=LungeStumbleSeconds`；`OnBitCharacter` postfix 设置 `BiteSelfStumbleSeconds`。原版 `DoLungeRecovery` 仅在倒地/昏迷结束后累加恢复时间，故倒地与恢复叠加。现在桥接 `Combat/LungeStumbleSeconds`、`BiteSelfStumbleSeconds`、`BiteTriggerCooldown`。

依赖 `MushroomZombieBiteCollider.OnTriggerEnter` 要求碰撞对象属于本地玩家，在冷却后施加 `biteInitialInjury`、`biteInitialSpores`、后续咬击 Affliction，再调用 `OnBitCharacter`。0.1.2 对初始伤害使用明确的百分比设置，不依赖原型伤害缓存；客户端也接收实例字段与咬击冷却。新增的 `OnBitCharacter` postfix 只写真实接触诊断日志，不增加额外伤害，也不替换碰撞判定。

### 0.1.4 Harmony 与回调补充

从本次实际编译引用的 Assembly-CSharp.dll 重新检查 TryLunge、StartLunging、MushroomZombieBiteCollider.OnTriggerEnter。游戏公共 API 不提供起跳前的外部距离约束，故在前两项增加仅限本导演对象、房主的准入 prefix，保留原生移动/视线/地面判断。

原生咬击只有 OnTriggerEnter，没有公开的接触处理入口。持续重叠转发组件在实际 OnTriggerStay 条件下通过缓存 MethodInfo 调用同一私有 OnTriggerEnter，走现有冷却和 PEAK Zombies 补丁；不反编译复制伤害实现、不额外 AddStatus。用于处理已重叠但未产生新 Enter 的接触时机，不能修复根本没有物理接触或其他 Mod 撤销伤害的问题。

### 0.1.5 已确认的伤害 ABI 不兼容

用户实机日志出现 `MissingMethodException: CharacterAfflictions.AddStatus(CharacterAfflictions/STATUSTYPE,single,bool,bool,bool,bool)`，伤害与孢子非零，原生回调被依赖 prefix 阻断。当前实际参考 DLL 确认 AddStatus 是 `(STATUSTYPE,float,bool,bool,bool)` 五参数，原生 OnTriggerEnter 调用匹配本游戏自己的方法。

兼容层不调用有缺失引用的依赖咬击 prefix；只在六参数 API 缺失且通过 Harmony 元数据唯一找到该 owner 与该嵌套类型的 Prefix 时移除这一项，恢复游戏本身实现。其他前后缀/转译器不变。为保留自定义冷却，对原生回调唯一的 5f 常量做精确替换，其余伤害代码完整保留。关闭插件恢复前述单项 prefix 的原 owner、priority、before、after。未验证其他依赖 API 在所有游戏版本中的兼容性。
