# PEAK Zombie Director

PEAK / BepInEx 的房主导演附加 Mod，依赖 **tony4twenty PEAK Zombies 2.0.0**。这是尚待实机验收的 **0.1.0 实验版本**；已针对实际程序集构建，不包含伪造的游戏 API。构建和模拟测试不代表游戏、多人与 UI 验收通过。

## 安装和使用

1. 通过 Thunderstore 安装 `BepInExPack_PEAK 5.4.75301`、`tony4twenty-PEAK_Zombies-2.0.0`。为确保配置和伤害一致，所有玩家安装相同版本的依赖与本插件。
2. 将发布压缩包中的 `BepInEx` 目录合并到游戏根目录，确保同一插件目录下同时有 `PEAKZombieDirector.dll` 和 `PeakZombieDirector.Core.dll`。
3. 启动游戏，按 **Ctrl+F8** 打开设置；可在总览页修改快捷键。大厅与攀登过程均可打开面板。
4. 选择 Easy / Normal / Advanced / Expert / Custom，编辑任何玩法参数会转为 Custom。每次编辑自动保存活动配置；“保存当前配置为 Custom”更新单独的 Custom 存档。切换其他预设不会覆盖已保存的 Custom。
5. 房主可编辑玩法，客户端显示房主快照，只能编辑自己的语言与快捷键。快照每两秒同步；中途加入者等待下一次快照。

配置文件：`BepInEx/config/PEAKZombieDirector.cfg`。设置修改在下一次导演更新生效；生命周期变更沿用僵尸原始出生时间，因此缩短寿命可能让现有僵尸立即过期。配置文件的非法范围、NaN 与枚举在启动时修正并记录警告；外部修改文件后重启游戏。

## 已实现的功能

- 初始数量、定时波次、时间增长、数量上限增长、随机数量/间隔波动、0 上限表示无限。
- 每波预算、每秒网络生成预算、位置搜索预算和每轮替换预算；大波次分散到后续更新，不在一帧无限生成。
- 复用原版僵尸的网络生成与 AI；持续锁定有效玩家、最近/随机/优先原目标，目标死亡或离开后重选。
- 三维/水平距离、持续超距检测、卡住检测、冷却替换；先验证位置和生成，再删除旧僵尸。删除失败回滚新僵尸，回滚失败时暂停新增并重试清理。
- 优先遮挡与身后位置、避免正前方、可选视线内回退。地面、坡度、碰撞、玩家间距、有限坐标和场景加载检查始终执行；找不到位置则保留旧僵尸。
- 行走/奔跑/攀爬/跳跃/扑击速度、咬击初始伤害、扑击恢复间隔、感知距离、扑击触发范围、寿命与远距离清理配置。
- 可选择使用原版地图生成点，由导演通过公开 `SpawnAt` 接管，遵守同一数量与网络预算，每个点每阶段最多使用一次。依赖 Mod 的地图散布器与定时波次始终暂停，避免双重控制。
- 默认在全部已加载生态区工作；可切换为仅 Roots。
- 独立游戏内 IMGUI 面板：分类导航、滚动、数字滑块和输入框、枚举下拉、开关、中英切换、快捷键、状态和诊断。阻止玩家输入并管理光标。
- 只有 Photon 房主执行生成、删除、波次和目标操作。版本不符、异常和房主变更会停止调度。房主变更后等待场景变化/新局初始化，不冒险迁移旧导演。

## 构建

安装 .NET SDK 8.0.427。运行目标为 Unity Mono 的 `netstandard2.1`，测试目标为 `net8.0`。

```bash
export PEAK_MANAGED_DIR='/path/to/PEAK/PEAK_Data/Managed'
export PEAK_MOD_REFERENCE_DIR='/path/to/reference-mods'
# reference-mods 包含 BepInEx.dll、0Harmony.dll、Zombies Anywhere.dll
bash scripts/build.sh
bash scripts/test.sh
python3 scripts/package.py
```

输出：`src/PeakZombieDirector.Plugin/bin/Release/netstandard2.1/PEAKZombieDirector.dll`，另需同目录的核心 DLL。安装压缩包：`artifacts/PEAK_Zombie_Director-0.1.0.zip`。

云环境可运行 `bash scripts/setup-cloud.sh`。脚本只在仓库外准备工具和参考 DLL，以固定版本、TLS 和校验和下载公开资源，不会把游戏 DLL 或依赖 Mod DLL 打进发布包。云环境参考 DLL 来自 ShootZombies 固定提交，属于编译兼容性证据；最终发布前须用自己当前安装的合法游戏程序集重新构建并实测。

## 当前限制与验收要求

- 没有启动真实 PEAK/Unity 环境；实际地形合法性、追击效果、中文字体、常见分辨率、光标恢复和多人行为均**未实机验证**。详见 [实机验收](docs/VALIDATION.md)。
- 使用独立 IMGUI 面板，没有集成 PEAKLib 原生控件或 ModConfig 原生设置页，也未验证原生视觉一致性。
- “伤害倍率”仅作用于已经核实的初始咬击 injury/spores；“攻击间隔倍率”是扑击恢复时间；“攻击范围”是扑击触发范围。没有把未经核实的完整攻击系统包装成假选项。
- 寿命禁用只禁用老死；**没有独立的“忽略所有自然死亡”功能**。危险物与战斗仍可杀死僵尸。没有禁用地面/碰撞安全验证的开关。
- 地图生成选项复用原始地图点，不重新启用依赖 Mod 的生态区散布器。每个地图点在本阶段只消费一次，切场景后重置。
- 移动倍率等依赖配置是 PEAK Zombies 的全局 NPC 设置，会影响房间其他来源的 NPC。导演仅替换/删除自己生成的对象；总存活上限包含其他来源僵尸。接管时暂存依赖配置，正常退出时恢复；依赖菜单主动保存或崩溃后的持久化行为仍需实测。
- 替换时可能临时多一只僵尸。若网络删除与回滚都失败，暂停新增直到清理成功，不能保证网络故障期间数量瞬时严格相等。
- 房主迁移不转移所有对象/计时状态；安全停止并暂停依赖生成，等待场景变化或新局。没有在本环境验证离线游玩、原版客户端或其他僵尸 Mod 兼容性。

API 证据、依赖来源、版本与许可证说明见 [依赖研究](docs/DEPENDENCIES.md)。参数完整清单见 [配置参考](docs/CONFIGURATION.md)。本仓库代码使用 MIT 许可证，不授予第三方游戏与 Mod 程序集的再分发权。
