# 牙齿 GameSdk 整合方案文档

> 目标项目：`D:\tooth_git\Project\ToothGameProject`（《武装到牙齿》）
> SDK 包：`com.youdoogame.framework` v1.0.4（`youdoogame-unity-sdk`）
> 文档状态：**实施中**（已开始阶段一：安装 + 物化资源 + 组合根脚本）
> 目的：战斗以外的匹配/结算/账号等全部用新 SDK（以新 SDK 建议为准）；只有战斗的实时人像与战斗输入先用旧 SDK。

---

## 0. 已确认决策（结论）

| 范围 | 决策 |
| --- | --- |
| **匹配 / 准备界面** | 用新 SDK `MatchSessionHost` + 项目自有 UI（`FrameworkProductUiAdapterBase`） |
| **结算** | 用新 SDK（`MatchSessionHost` 的 Settlement 阶段 + 比分 + MET/TV/TI） |
| **账号 / 头像** | 用新 SDK（`PlayerAvatar`，账号头像 + 人脸照片 + 游客/默认 + 手动换头像） |
| **相机表现（匹配）** | 用新 SDK（`CameraImageSplitLayout`，SPLIT 固定槽位） |
| **控制器（UI 导航）** | 用新 SDK（`ControllerInput` + 键盘手柄 fallback） |
| **战斗实时人像** | **先用旧 SDK**（`AndroidTextureBridge*` + `CameraTextureView` 相机裁剪跟随，不动） |
| **战斗输入（手势）** | **先用旧 SDK**（`BoneParser` + `BattleBoneParseData` 原生骨骼，不动） |
| **非战斗的必选配置** | 以新 SDK 建议为准（见 §6） |

一句话：**战斗以外全部迁新 SDK（匹配/结算/账号/相机/控制器），战斗的实时人像 + 输入保留旧 SDK。**

---

## 1. 背景与目标

牙齿项目当前用一套**旧版 YouDoo SDK**（`youdoo_plugin` 原生 + `AndroidServerInfo`/`AndroidParseData` 等 C# 封装 + 从 SDK 拷入的 Demo 代码）驱动匹配、相机、头像、骨骼与遥控器。新 SDK（`com.youdoogame.framework`）是官方唯一 UPM 入口，提供统一的匹配会话、相机表现、头像、骨骼/手势/动作、控制器与数据源。

目标（已确认）：在不改动牙齿战斗玩法（`BoneParser` + `BattleLogic` + `GameDll` 战斗逻辑）与战斗实时人像/输入的前提下，把**匹配、结算、账号、相机、控制器**等战斗以外、与新 SDK 能力重叠的部分切换到新 SDK，并保留项目自己的产品 UI 与文案。战斗以外的实现细节以新 SDK 的默认/推荐做法为准。

---

## 2. 现状盘点（旧 SDK 使用地图）

### 2.1 代码位置

| 位置 | 内容 |
| --- | --- |
| `Assets/Scripts/ThirdParty/YouDooUnity/` | 旧 SDK 运行时（`AndroidServerInfo`、`AndroidParseData`、`GameInput`、`SDKUtil`） |
| `Assets/Scripts/SDK/` | 从旧 SDK 拷入的 Demo/业务侧代码（`GameDemo`、`UI`、`GameManager`、`SdkManager`） |
| `Assets/Scripts/Game/BoneParser/` | 牙齿战斗手势识别（`BoneParserLib.dll` 原生 + `BoneParserRuntime*` + `BoneGestureTuning`） |
| `Assets/Scripts/Game/BattleLogic/` | 战斗逻辑（`Battle`、`BoneInput`、`TowerDefend`、`ObjectSystem`） |
| `GameDll/Src/GameLogic/Lobby/` | 编译进 `GameDll.dll.bytes` 的 UI（`tower_defend_prepare_wnd` 等），走 Huatuo 热更 |

### 2.2 旧 SDK 能力 → 项目消费者

| 能力 | 旧 SDK 实现 | 项目消费者 |
| --- | --- | --- |
| 原生数据源 | `AndroidParseData`（`DllImport youdoo_plugin`：`GetLatestFrameInfoPtr`、`GetDetectBoxInfoByType`、`GetKeyPointByType` 等） | `AndroidParseDataDemo`、`BattleBoneParseData` |
| 匹配/准备状态机 | `AndroidParseDataDemo`（`PrepareMatchStep`：Empty→WaitCenter→FaceRecognizing→WaitRaiseHand→Ready；`PrepareMatchSeatState`；事件 `onPlayerIsReady`/`onCanGameStart`/`onPlayerFaceRecognized` 等） | `tower_defend_prepare_wnd` |
| 准备界面 UI | 项目自有 `tower_defend_prepare_wnd` + `tower_defend_prepare_face_controller` | `GameDll` |
| 头像显示 | `HeadImage`（RawImage 加载 http/content/file）+ `HeadImageController` + `PlayerRoleManager`（账号/角色：`facePhotoPath`/`avatarUri`） | 准备界面 |
| 相机/骨骼渲染 | `PlayerMatchView`（FullView/PartitionView/HalfView）+ `PlayerTextuerShow` + `CameraImagView` | 准备界面 |
| 战斗实时人像（相机跟随） | `AndroidTextureBridgeBase`（Vulkan/OpenGLES，`GL.IssuePluginEvent` 出相机 `RenderTexture`）+ `CameraTextureView`（RawImage 按 `uvRect` 裁切）+ `CameraTextureViewManager`（选人/算包围盒）+ `BattleCameraTextureViewSmoothCalculation`（平滑跟随） | 战斗画面 |
| 遥控器输入 | `RCUPlayerInputManager` + `RemoteControlUnitInputSystemManager` + `GyroAirMouse` | 目前仅在 Demo/UI，战斗逻辑未直接消费 |
| 人脸识别/选角 | `AndroidServerInfoDemo`（`StartFaceRecognitionCurrentFrameInfo`、`SelectRole`、`SetPlayerID`） | `AndroidParseDataDemo`、`tower_defend_prepare_face_controller` |
| 战斗输入 | `BattleBoneParseData`（继承 `AndroidParseData`，实现 `IBoneFrameSource`）→ `BoneFrameSourceResolver` → `BoneParser` | `BoneParser` |

### 2.3 关键结论

1. 匹配/准备/账号头像/相机/人脸识别/选角都集中在**准备界面**这条链上，且与新 SDK 能力一一对应，可整体迁移。
2. 战斗有两条**独立且耦合**的旧 SDK 原生数据通路：
   - **实时人像（相机画面）**：`AndroidTextureBridgeBase` 出相机纹理 → `CameraTextureView` 按玩家包围盒裁切 + 平滑跟随；
   - **输入（骨骼）**：`BattleBoneParseData` 直接读旧 SDK 原生骨骼关键点 → 喂给 `BoneParser`。
   两者都来自同一个 `youdoo_plugin` 原生，因此"是否迁到新 SDK"应作为一个决策一起处理（§5.3）。
3. 账号头像（匹配界面的 `facePhotoPath`/`avatarUri`）**只**在准备界面出现，战斗逻辑与 HUD 未消费账号头像。

---

## 3. 新 SDK 能力地图（与本项目相关）

| 能力 | 新 SDK 入口 | 是否覆盖本项目需求 |
| --- | --- | --- |
| 匹配会话 | `MatchSessionHost`（候选稳定、正式绑定、人脸确认、准备、丢失/重获、局内换绑） | ✅ 覆盖 `AndroidParseDataDemo` 的匹配状态机 |
| 匹配 UI 数据 | `MatchSessionUiSnapshot`（`State`/`Intent`/`Progress01`/`VisualSlotIndex`）、`UiStateChanged`、`UiTransitioned` | ✅ 覆盖准备界面的语义状态 |
| 产品 UI 适配 | `FrameworkProductUiAdapterBase`、`IMatchSessionUiRenderer` | ✅ 承载项目自有准备界面 |
| 相机表现 | `CameraImageView` / `CameraImageSplitLayout`、FULL/SPLIT、FOLLOW/FIXED（包围盒跟随/固定槽位） | ✅ 覆盖 `PlayerMatchView`/`PlayerTextuerShow`/`CameraImagView`（分区=SPLIT）；FOLLOW 亦覆盖战斗的实时人像跟随 |
| 实时人像/抠像 | `YouDooCutout`（`SetTargets`/`GetLatestFrame`，原生分割人像带 Alpha；ARM64/API30，真机验收待完成） | ⚠️ 覆盖"抠出来的人像"；当前战斗是相机裁剪跟随而非抠像，见 §4.3 |
| 头像与人脸 | `PlayerAvatar`（账号头像/人脸照片/游客/默认、异步加载）、`GetSessionPlayerAvatarSlot`、手动换头像 `HandleAvatarSelectionCommand/Navigation`、`AvatarSelectionChanged` | ✅ 覆盖 `HeadImage`+`PlayerRoleManager`+手动选角 |
| 头像视图 | `FrameworkProductUiAvatarView`、`PlayerAvatarImage` | ✅ 承载项目头像卡片 |
| 骨骼/手部/动作 | `Skeleton`（`RequestSkeletonHandle`/`GetPlayerFrame`）、`HandGesture`（`Hand.GetPose/GetState`）、`Action`（`ActionEvaluationEngine`、`SkeletonActionRules`、`MatchActionProfileAsset`） | ⚠️ 只提供**通用**骨骼/手部/动作，**不含**牙齿专属手势 |
| 正式玩家 | `TryGetOperablePlayer`/`TryGetBoundPlayer`/`CanOperate` | ✅ 覆盖正式槽位/可操作边界 |
| 控制器 | `ControllerInput`、`Controllers`（遥控器/陀螺仪/按键/震动 + 键盘手柄 fallback） | ✅ 覆盖 `RCUPlayerInputManager`/`GyroAirMouse` |
| 数据源 | Synthetic（编辑器）/ YouDoo Adapter（Android）/ Editor Bridge（真机调试） | ✅ 覆盖旧 `youdoo_plugin` 数据接入 |
| 统一入口 | `YoudooAPI` | ✅ 推荐业务调用入口 |
| 结算/运动统计/埋点 | `TryAddScore`、`SubmitMet`、`SubmitActivityDurations`、`GameplayDataReported`、`DataChain` | ✅ 随结算迁移（§5.4） |
| 启动展示/日志/头套 | `startup-presentation`、`Youdoo.Logging`、`headwear` | 日志接入；启动展示/头套可选 |

---

## 4. 关键判断：牙齿战斗的"输入"与"实时人像"

### 4.1 "头像"要区分两个概念

用户原话"牙齿战斗里的头像"实际指**战斗时实时获取的人像**（实时相机画面），与匹配界面的**账号头像**是两回事。两者判断不同：

| 概念 | 当前实现 | 新 SDK 支持？ | 结论 |
| --- | --- | --- | --- |
| **匹配界面账号头像**（人脸照片/账号头像） | `HeadImage` + `PlayerRoleManager`（`facePhotoPath`/`avatarUri`） | ✅ 完整支持（`PlayerAvatar`） | **随匹配界面迁移** |
| **战斗实时人像**（实时相机画面跟随玩家） | `AndroidTextureBridge*` + `CameraTextureView` + `BattleCameraTextureViewSmoothCalculation` | ✅ 支持（`CameraImage` FOLLOW 模式 / `YouDooCutout` 抠像） | **与战斗输入数据源一起决策**（§4.3） |

#### 4.1.1 匹配界面账号头像——新 SDK 支持，随匹配界面迁移

判断依据（SDK 文档 `sdk-framework-usage.zh-CN.md` §9.2、`maintenance-handoff` 头像章节）：

- 新 SDK 明确支持：**账号头像 + 人脸照片 + 游客/默认头像 + 异步加载 + 正式身份绑定 + 手动换头像**，正是当前 `HeadImage` + `PlayerRoleManager` + `tower_defend_prepare_face_controller` 覆盖的能力。
- 手动换头像的设备无关命令（`Up` 进入槽位选择、`Left/Right` 切换、`Confirm` 打开平台选角、`Down/Back` 取消）与当前遥控器/点击选角流程一致。
- 账号头像**只**在准备界面出现，战斗逻辑/HUD 未消费账号头像。

**结论：账号头像迁移到新 SDK（`PlayerAvatar` + `FrameworkProductUiAvatarView`），不保留旧 `HeadImage`/`PlayerRoleManager`。** 项目自定义素材（默认头像/未识别占位图）作为 fallback 保留。

### 4.2 输入（Battle 手势识别）——新 SDK 不支持牙齿专属手势，**保留 BoneParser**

判断依据：

- 牙齿战斗的手势是**项目专属**的：交叉扩胸、单手下拉、叉腰抬臂、蹲起、扩胸推、双手展开、左倾、右倾、跳跃、下蹲、出拳、技能抛投等，且带全局表调参（配置 ID 100331/100336/100341/100348/2012~2015 等），由原生库 `BoneParserLib.dll` 识别。
- 新 SDK 的 `Action` 模块只内置**通用**动作（举手 `CreateHandRaised`、交叉手臂 `CreateArmsCrossed`、握拳 `Fist` 等），`HandGesture` 只给 `Fist/Open` 手部类型，`Skeleton` 给原始骨骼。**没有任何牙齿专属手势规则**，也**没有等价的原生识别器**。
- 用新 SDK 的 `Action` 自定义源重写全部牙齿手势 = 重写一个 C++ 手势识别器，工作量与风险极大，且需重新调参验证，不划算。

**结论：保留 `BoneParser` 手势识别逻辑与调参。** 剩下的问题是它的**数据源**（见 §5.3）。

### 4.3 战斗实时人像——新 SDK 支持，但迁不迁与输入数据源是同一个决策

**当前实现是什么**：战斗画面里的"人像"不是账号头像，也不是抠像，而是**实时相机画面按玩家包围盒裁剪 + 平滑跟随**：

- `AndroidTextureBridgeBase`（Vulkan/OpenGLES）用 `GL.IssuePluginEvent` 从旧 SDK 原生 `youdoo_plugin` 拿到相机 `RenderTexture`；
- `CameraTextureViewManager` 选出最优玩家并算包围盒；
- `CameraTextureView`（RawImage）按 `uvRect` 裁切显示；`BattleCameraTextureViewSmoothCalculation` 做位置/尺寸平滑。

**新 SDK 是否支持**：

| 需求 | 新 SDK 能力 | 说明 |
| --- | --- | --- |
| 实时相机跟随玩家（当前行为） | `CameraImage` 的 `FOLLOW` 模式（`2001 FULL+包围盒跟随`、`1002 SPLIT+分屏追随`） | 与当前"包围盒裁切 + 平滑跟随"等价；匹配/战斗共用同一套相机表现 |
| 真正"抠出来的人像"（带 Alpha 的分割人像） | `YouDooCutout`（`SetTargets`/`GetLatestFrame`） | v1.0.4 新增，ARM64/API30，真机验收仍待完成（见 `cutout.zh-CN.md`）；比当前相机裁切更"人像" |

**是否需要用新 SDK 的**：

- 实时人像（相机纹理）与战斗输入（骨骼）**都来自同一个旧 SDK 原生 `youdoo_plugin`**，两者是耦合的。因此"实时人像用不用新 SDK"和"输入数据源用不用新 SDK"是**同一个决策**（§5.3 方案 A / B）。
- **方案 A（短中期，推荐）**：匹配迁新 SDK；战斗的实时人像 + 输入继续走旧 SDK 原生（保留 `AndroidTextureBridge*` + `BattleBoneParseData`）。改动最小，BoneParser 零风险。
- **方案 B（长期最优）**：战斗也切到新 SDK 单一数据源——实时人像用 `CameraImage` FOLLOW（或 `YouDooCutout`），输入用 `RequestSkeletonHandle()`/`TryGetOperablePlayer()` 转喂 BoneParser。好处是只保留一套原生通路、正式槽位/`CanOperate` 边界统一；代价是需适配 + 真机逐项重验。

**结论（已确认）：本次按方案 A，战斗实时人像保留旧实现（`AndroidTextureBridge*` + `CameraTextureView` 相机裁剪跟随），不迁新 SDK。** 后续如需"抠像人像"再评估 `YouDooCutout`；是否整体切方案 B（单一新 SDK 数据源）作为远期可选项，不在本次范围。

---

## 5. 整合策略（分阶段）

### 5.1 阶段一：安装与组合根（不动业务）

1. `Packages/manifest.json` 增加唯一入口：
   ```json
   "com.youdoogame.framework": "https://gitlab.youdoogo.com/shared/youdoogame-unity-sdk.git#v1.0.4"
   ```
   （或本地开发源 `file:E:/Youdoo/youdoogame-unity-sdk`，正式发布用固定 tag）
2. 确认项目 manifest 无旧拆分包（`com.youdoogame.*`）条目；`com.unity.inputsystem` 版本兼容（SDK 要求 1.14.1，项目现为 `1.14.4-t1`，需核对）。
3. 搭建规范场景骨架：
   ```
   FrameworkRoot
   |-- FrameworkCompositionRoot
   |-- FrameworkTemplateHost (ConfigurationAsset)
   |-- MatchSessionHost（唯一 canonical）
   |-- CameraImageView / CameraImageSplitLayout（SPLIT）
   `-- CpProductUiAdapter（项目自有，见 §6）
   ```
4. 决定数据源：Editor 用 Synthetic 先跑通，Android 走 YouDoo Adapter，真机调试用 Editor Bridge。

### 5.2 阶段二：匹配/准备界面迁移（重点）

目标：把 `tower_defend_prepare_wnd` 的数据源从 `AndroidParseDataDemo` 换成新 SDK 的 `MatchSessionUiSnapshot`，UI 外观与文案保留。

1. **UI ownership = ProjectAdapter**：项目保留 `tower_defend_prepare_wnd`，新建 `CpProductUiAdapter : FrameworkProductUiAdapterBase`，把 `UiSnapshot.State/Intent/Progress01` 映射到现有座位节点（文案走现有 `RenderAPI.GetTextByLanId` 本地化表）。
2. **相机**：用新 SDK 的 `CameraImageSplitLayout`（SPLIT，对应当前 `PartitionView`）替换 `PlayerMatchView`/`PlayerTextuerShow`；相机纹理由 Provider 持有，项目只读不销毁。
3. **头像**：用 `PlayerAvatar` + `FrameworkProductUiAvatarView` 替换 `HeadImage`/`PlayerRoleManager`；`Intent`/`AvatarSelectionChanged` 驱动头像刷新；手动换头像走 `HandleAvatarSelectionCommand/Navigation`。
4. **准备/确认**：举手动作用 SDK 的 `MatchActionProfileAsset`（`CreateHandRaised`→Ready）表达；`onCanGameStart` 由 `UiSnapshot` 的 all-ready 语义替代；确认进战斗仍由 `tower_defend_prepare_wnd` 调用 `StartGameplay()`。
5. **座位→SDK 槽位绑定**：用 `MatchSessionUiSlotSnapshot.PersonId`（临时）+ `MatchPlayerId`/`UserId`（稳定）替代 `AndroidParseDataDemo` 的 `personPlayerIds/personPlayerLockIng`；禁止用 `PersonId` 作业务主键。

### 5.3 阶段三：战斗"输入 + 实时人像"——**保留旧 SDK（已确认）**

战斗的**输入**（`BattleBoneParseData` 读骨骼关键点）和**实时人像**（`AndroidTextureBridge*` + `CameraTextureView` 读相机纹理）都来自同一个旧 SDK 原生 `youdoo_plugin`，**本次全部保留旧实现，不迁新 SDK**：

- `BattleBoneParseData`、`BoneFrameSourceResolver`、`BoneParser`（手势识别 + 调参）保持现状；
- `AndroidTextureBridgeBase`（Vulkan/OpenGLES）、`CameraTextureView`、`CameraTextureViewManager`、`BattleCameraTextureViewSmoothCalculation`（实时人像相机裁剪跟随）保持现状。

**唯一要注意的点**：战斗保留旧 SDK 原生通路，与匹配链迁到的新 SDK（YouDoo Adapter）并存。落地时需确认两者不重复初始化同一原生 SDK、不冲突（§9）。这是技术验证项，不是产品决策。

> 远期可选项（不在本次范围）：若想彻底单一数据源，可把战斗输入改接新 SDK `RequestSkeletonHandle()` 转喂 BoneParser、实时人像改接 `CameraImage` FOLLOW 或 `YouDooCutout`，但需关键点格式适配 + 真机逐项重验。

### 5.4 阶段四：结算迁移（用新 SDK，已确认）

战斗结束后的结算数据与流程迁到新 SDK，项目自有结算 UI 消费新 SDK 快照：

1. **游戏结束模式**：项目战斗由玩法决定结束 → 在项目自有 `MatchSessionConfigAsset` 选 `GameplayEndMode = ProjectControlled`，战斗结束由项目调用 `CompleteGameplay()` 进入 Settlement（`FrameworkLifecycleMode` 为 `FrameworkManaged` 时 SDK 仍管理阶段状态，项目通过 Host API 驱动）。
2. **比分/成绩**：通过 `MatchSessionHost.TryAddScore(slotIndex, score)`（或 `YoudooAPI` 对应入口）上报；结算 UI 读 `Snapshot`（`Phase == Settlement`）与正式槽位。
3. **运动统计**：项目已有自身 TV/TI 逻辑时，开局前 `SetActivityMetricsTimeMode(ProjectProvided)`；开局保存 `RoundId`，结算先 `SubmitActivityDurations(roundId, slotIndex, tv, ti)` 再 `SubmitMet(slotIndex, met)`，避免重复上报。
4. **重开/返回匹配**：结算后 `ReturnToMatching()`（清分/新 Session）或 `RestartGameplay()`（保留绑定新回合），由项目大厅分支调用。
5. **结算 UI**：项目保留 `tower_defend_chapter_result_wnd`/`tower_defend_endless_result_wnd`，把数据源从旧逻辑换成新 SDK 的 `Snapshot`/`ActivityMetrics`。

### 5.5 阶段五：控制器 / 启动展示 / 日志（以新 SDK 建议为准）

- **遥控器（UI 导航）**：迁到新 SDK `ControllerInput`/`Controllers`，保留键盘手柄 fallback；旧 `RCUPlayerInputManager`/`RemoteControlUnitInputSystemManager` 停用（战斗未消费遥控器，不受影响）。
- **运动统计/埋点**：已并入 §5.4（结算迁移）。
- **日志**：接入 `Youdoo.Logging`（项目副本 `YoudooLogSettings.asset`），便于后续诊断。
- **启动展示（健康提示/Logo）**：可选，按产品需要再接入，不作为本次必做项。

### 5.6 阶段六：验证（见 §8）

---

## 6. 必选配置项（已按"以新 SDK 建议为准"确定）

| 配置项 | 确定值 | 依据/说明 |
| --- | --- | --- |
| **Template** | `1001 SPLIT+固定槽位` | 当前匹配是 `PartitionView` 固定 4 分区，对应 SPLIT+固定槽位；若产品后续要分屏追随再改 `1002`（注：SDK 包默认 2001 是 FULL，不适用于牙齿 4 人分区） |
| **GameplayRebindMode** | `SlotScoped` | 新 SDK 默认值 |
| **FrameworkLifecycleMode** | `FrameworkManaged` | 新 SDK 正常模式：`MatchSessionHost` 管理 Matching/Countdown/Playing/Settlement/Reset，项目通过 Host API（`StartGameplay`/`CompleteGameplay`）驱动并消费快照 |
| **UI ownership** | `ProjectAdapter` | 保留 `tower_defend_prepare_wnd`，实现 `FrameworkProductUiAdapterBase`（"SDK + 我们自己的 UI"） |
| **avatar policy** | 新 SDK 默认：账号头像 + 人脸照片；识别失败/超时允许游客；异步加载显示 fallback；短暂丢失保留已确认头像；手动换头像后刷新正式槽位 | 对齐当前 `tower_defend_prepare_face_controller` 行为，新 SDK 原生支持 |
| **prompt/localization policy** | 项目本地化表（`RenderAPI.GetTextByLanId`）维护，`State/Intent/Progress01` 映射为语言包 key；`UiTransitioned` 触发一次性动画/音效 | SDK 状态与项目文案冲突时由项目 Adapter 拥有最终显示权 |

**附加确定项**：

| 项 | 确定值 | 说明 |
| --- | --- | --- |
| **GameplayEndMode** | `ProjectControlled` | 战斗由项目玩法决定结束，项目调用 `CompleteGameplay()` |
| **SDK 安装源** | Git 固定 tag `#v1.0.4` | 新 SDK 建议正式项目用固定 tag |
| **数据源** | Editor=Synthetic，Android=YouDoo Adapter，真机调试=Editor Bridge | 新 SDK 默认 |

> 以上取值以新 SDK 建议/默认为准，实现时可直接按此落地；若实测与产品预期不符（尤其 Template 布局），再单独回调。

---

## 7. 落地细节与规则（实施时遵守）

1. **单一 canonical Host**：全项目只允许一个 `MatchSessionHost`/`MatchSessionContext`；场景交接复用，禁止第二个匹配协调器。
2. **不重复初始化 Provider**：匹配链迁到新 SDK 后，旧 `AndroidServerInfoDemo` 的人脸/选角/匹配逻辑要停用，避免新旧两条匹配状态机竞争（SDK 规则："do not run competing providers/state machines"）。
3. **身份主键**：`PersonId` 是临时追踪身份，禁止作为头像/业务主键；UI 用 `MatchPlayerId`+`UserId`+`AvatarSyncState`。
4. **相机纹理**：Provider 持有，项目只读，禁止 `Destroy`/`Release`。
5. **配置资产**：需要改模板/Profile 时，复制到项目 `Assets/` 编辑副本，不直接改包内默认资产；可运行 `materialize-framework-resources.ps1` 复制最小配置闭包到 `Assets/Resources/YoudooGame/Framework/`。
6. **旧路径可回退**：迁移期间保留旧实现可回退，直到新路径真机验证通过再删（SDK："Keep the old path recoverable until replacement verification"）。
7. **编码规范**：新增 `.cs` 用 UTF-8 带 BOM，字段 `m_XxxYyy`，不加无意义注释，命名/风格对齐现有代码。

---

## 8. 验证清单

### 8.1 静态/编译
- [ ] 根包已解析，`Packages/com.youdoogame.framework/package.json` 为 `1.0.4`。
- [ ] 无旧拆分包条目，无重复 Provider。
- [ ] `FrameworkConfigurationAsset.SelectedTemplate` 非空且通过 `TryValidate`。
- [ ] `FrameworkCompositionRoot.Initialize()` 成功。
- [ ] C#/asmdef 编译无错。

### 8.2 Synthetic 流程
- [ ] Matching → Playing → Paused → Settlement → Reset 全流程通过。
- [ ] 1~4 人切换、SPLIT 布局、UI 首次 Render、SessionToken 隔离通过。

### 8.3 匹配界面（迁移后）
- [ ] 座位/头像/提示按 `UiSnapshot` 正确渲染，文案走项目本地化。
- [ ] 人脸识别成功/失败/超时、游客、手动换头像、丢失重获通过。
- [ ] 确认进战斗 `StartGameplay()` 后，`BattleBoneParseData` 能正常接手骨骼。

### 8.4 结算（迁移后）
- [ ] `ProjectControlled` 下，项目调用 `CompleteGameplay()` 进入 Settlement，`Snapshot.Phase` 正确。
- [ ] 比分 `TryAddScore`、MET/TV/TI 上报（`SubmitActivityDurations` + `SubmitMet`）通过；项目自身 TV/TI 模式不重复上报。
- [ ] 结算 UI 读 `Snapshot`/`ActivityMetrics` 正确；`ReturnToMatching()`/`RestartGameplay()` 行为符合产品。

### 8.5 战斗输入 + 实时人像（保留项）
- [ ] BoneParser 全部手势（扩胸/下拉/叉腰/蹲起/推/展开/倾/跳/下蹲/出拳/技能）真机识别不回归。
- [ ] 战斗实时人像（相机裁剪 + 平滑跟随）真机表现不回归。
- [ ] 确认新 SDK（匹配链）与旧 SDK（战斗链）原生通路并存不冲突。

### 8.6 真机/Bridge
- [ ] Editor Bridge 预检、CameraImage 帧、镜像、方向、前后台恢复。
- [ ] 遥控器导航/确认/返回/暂停 + 键盘手柄 fallback。
- [ ] Android Manifest/Gradle/Proguard/数据上报证据。

---

## 9. 风险与依赖

| 风险 | 说明 | 应对 |
| --- | --- | --- |
| 新旧 SDK 原生通路并存冲突 | 匹配链迁新 SDK（YouDoo Adapter）后，战斗仍走旧 `youdoo_plugin` 原生，可能重复初始化同一原生 SDK | 阶段三前向 SDK 维护者确认并存可行性；明确"匹配=新 SDK、战斗=旧 SDK"的 Provider 所有权边界，不重叠初始化 |
| `com.unity.inputsystem` 版本 | SDK 依赖 1.14.1，项目 `1.14.4-t1` | 核对兼容性，必要时对齐 |
| Template 布局（SPLIT 固定 vs 追随） | 确定用 1001 固定槽位；若实测与产品预期不符 | 回调改 1002，或复制项目配置资产调整 |
| 热更（Huatuo）与 SDK 程序集 | SDK 为 asmdef 程序集，与 `GameDll` 热更程序集边界需理清 | 阶段一验证编译与热更加载 |

---

## 10. 剩余待办 / 验证项（非产品决策，实现时处理）

决策已全部确认，剩余是技术验证与实施项：

1. **新旧原生通路并存**：向 SDK 维护者确认新 SDK（YouDoo Adapter）与旧 `youdoo_plugin` 是否可并存、如何隔离 Provider 所有权（阶段三前必须确认）。
2. **`com.unity.inputsystem` 版本兼容**：核对 SDK 依赖 1.14.1 与项目 `1.14.4-t1`。
3. **热更（Huatuo）与 SDK 程序集边界**：阶段一验证编译与热更加载。
4. **Template 布局实测**：1001 SPLIT 固定槽位落地后，若与产品预期不符，回调改 1002。
5. **远期可选项（不在本次范围）**：战斗输入改接新 SDK 骨骼句柄、实时人像改接 `CameraImage`/`YouDooCutout`，待需要时再评估。

---

## 11. 进度记录（实施）

### 已完成

1. **manifest.json**：加入 `"com.youdoogame.framework": "https://gitlab.youdoogo.com/shared/youdoogame-unity-sdk.git#v1.0.4"`，**已在团结引擎中解析成功**（packages-lock.json 已写入）。本地仓库 `D:\ToGit\youdoogame-unity-sdk` 与已装版本一致（v1.0.4 @ ae30c28）。
2. **GUID 冲突已解除（编译归零）**：实测团结引擎对缺失 .meta 按资产路径确定性还原 GUID（删 .meta 等不到新 GUID），且编辑器脚本菜单在编译错误期无法注册（死循环）。最终方案：**给 57 个冲突 vendor .meta 手写随机 32-hex GUID**（工程内 2744 个 Unity 时代 hex GUID 资产一直正常，证明引擎接受；对已存在的 .meta 引擎按内容认 GUID 不重写）。已验证全工程零文件按 GUID 引用 vendor 资产，无需重映射。`YouDooSdkGuidRepair.cs` 已修复只收集 .cs 的覆盖 bug（留存备用，本次未用它）。
3. **物化模板 1001 + 内部引用重映射**：8 个物化资产 .meta 换 hex 后重跑 `materialize-framework-resources.ps1 -TemplateId 1001`，`SPLIT_FIXED` 模板 6 处内部引用已改指项目副本（可直接编辑副本调参）；布局 Profile 的包内源为 Base64 GUID 且零引用，警告可忽略。
4. **组合根挂载**：`GameFrameworkBootstrap`（原 Tooth 前缀已按移植性要求改名，GameProductUiAdapter 同）挂到 `startapp_dll.unity` 根级对象（场景 YAML 直写，两个脚本 .meta 均换 hex GUID，组件按 GUID 绑定）。运行时自建 FrameworkRoot 全套并 DontDestroyOnLoad；4×CameraImageView 归位窗口 Canvas 属运行时逻辑，见阶段二。
5. **阶段二（代码完成，待真机验证）**：`tower_defend_prepare_wnd` 新增框架会话模式——`GameProductUiAdapter.HasLiveSession`（编辑器 Synthetic / 真机 Adapter 均生效）时座位状态/就绪/槽位绑定/头像全部来自 `MatchSessionUiSnapshot` + `PlayerAvatar`（快照推送驱动刷新，状态映射：Searching=等待进入、Focused/Confirming/Reacquiring=识别中、WaitingForReady/Bound=加载头像）；确认链改为"全部槽位 Bound → `TrySyncPrepareRequestFromFramework`（CGamePro_Lobby 新增，按 VisualSlotIndex 回写）→ `StartBattleFromRequest` → 适配器 `StartGameplay()`"；`BindFrameworkCameraViews` 运行时把 4×`CameraImageView` 归位到窗口座位节点下、停用旧 CameraTextureView 与模板布局组件；头像手动选角走 `PlayerAvatar.EnterManualSelection/Move/Confirm`。旧 Demo 链路完整保留可回退（编辑器无会话时走原手动模式；OnClose 不再重置 Demo 状态机）。
6. **阶段四（代码完成）**：`CGamePro_Scene.OnFinishBattle` → 先按就绪槽位 `YoudooAPI.TryAddScore`（章节=星级 / 无尽=最佳波次；本项目无按人比分，取共享成绩）→ `YoudooAPI.CompleteGameplay()` 进结算；结算"返回"→`ReturnToMatching()`，"再来一局"→`RestartGameplay()`。项目无自有 TV/TI，走 SDK 默认时长统计（未启用 ProjectProvided）。
7. **阶段五（基础设施完成）**：新增 `GameControllerInputBridge`（`ControllerInput.CreateContext`，统一遥控器+键盘手柄轮询，挂 FrameworkRoot）；准备界面框架模式下方向键仅用于头像手动选角 `MoveManualSelection`，其余导航仍走现有链路避免双输入。旧 `RCUPlayerInputManager`/`RemoteControlUnitInputSystemManager` 未停用（战斗与现有 UI 导航仍在消费，待真机验证后整体切换）。
8. **GameDll 构建线**：`GameDll.csproj` 新增 `YoudooGame.Core/MatchFlow/Framework/PlayerAvatar/CameraImage` 引用；`dotnet build` 主工程与 GameDll 均零错误通过（GameDll 待编辑器重编译 ScriptAssemblies 后出正式 .bytes）。

### 待办（下一步）

1. **编辑器重编译**：切焦点让主工程 + SDK 重编译（`GameControllerInputBridge`、适配器新成员、场景挂载生效；临时加进 Assembly-CSharp.csproj 的 Compile 条目会被编辑器重新生成覆盖，属预期）→ Console 确认零错误。
2. **重编 GameDll 产出热更包**：主工程编译后执行 `dotnet build GameDll/GameDll.csproj`，`GameDll/bin/Debug/GameDll.dll` 拷为 `Assets/GameDll/GameDll.dll.bytes`（pdb2mdb 生成 .mdb.bytes）。
3. **Synthetic 验证**（§8.1/8.2）：startapp 场景 Play，确认 FrameworkRoot 初始化、准备界面走快照渲染、确认开战链、编辑器下 Synthetic 人数切换。
4. **阶段六真机验证**：匹配/头像/结算/遥控器导航逐项过 §8 清单；重点核对新旧原生 AAR 并存（两套原生同时用相机是最大风险项）与 Huatuo 热更加载 SDK 类型的边界。
5. 远期：战斗输入/实时人像切方案 B（单一新 SDK 数据源）。

---

## 12. 存档接入（SDK GameSave）

### 12.1 架构

- 顶级存档 `GameSaveRoot`（`Assets/Scripts/SDK/Framework/GameSaveService.cs`），JSON 经 `YoudooAPI.GetPlatformGameSave()` / `SetPlatformGameSave()` 读写**平台存档（按当前设备账号）**，不再写 `PlayerPrefs` / 本地存档文件。
- 模块数据段：`m_LobbyJson`（大厅/进度，GameDll `LobbyPlayerSaveData`）、`m_GuideJson`（新手引导，GameDll `GuideSaveData`）、音频/画质为主工程平铺字段。
- 保存采用"下一帧合并 flush"：同帧多次修改只写一次 SDK。
- 启动门禁：`login_wnd` 等 `GameSaveService.SaveDataReady`（含平台确认无档）才开放开始按钮；存档未就绪时禁止把内存默认档推上云端。

### 12.2 可存档数据审计（已全部纳入）

| 数据 | 存档字段 | 状态 |
| --- | --- | --- |
| 角色名 | `LobbyPlayerSaveData.m_RoleName` | ✅ |
| 金币 / 钻石 | `m_Coin` / `m_Gem` | ✅ |
| 章节解锁 / 任务进度 | `m_UnlockedStage` / `m_MissionStage` | ✅ |
| 无尽最佳波次 | `m_BestEndlessWave` | ✅ |
| **天赋（主线解锁数 + 分支）** | `m_GiftednessUnlockCount` / `m_GiftednessUnlockBranches` | ✅ 已存 |
| 选中模式 / 关卡 | `m_SelectedGameMode` / `m_SelectedStageId` | ✅ |
| 关卡星级 | `m_StageStarList` | ✅ |
| 语言 | `m_languageType` | ✅ |
| 首次启动 | `m_FirstStart` | ✅ |
| 音频音量 / 静音 | `GameSaveRoot.m_AudioBusVolumes` / `m_AudioMuted` | ✅ |
| 画质（高帧率 / 后处理） | `m_GraphicsAsync` / `m_GraphicsAfter` | ✅ |
| **新手引导进度** | `GameSaveRoot.m_GuideJson`（`GuideSaveData`） | ✅ 本次接入 |

### 12.3 本次修复 / 补全

1. **修复编译错误（上一轮中断的直接原因）**：`GameSaveService.ParseRoot`（静态）误调实例方法 `ParseData<T>(string)` → 改为静态重载 `ParseData<T>(string, bool)`。此前 Assembly-CSharp 一直编译失败，DLL 停留在旧版本。
2. **补全引导存档**：`GuideManager` 接入 `GameSaveService.Root.m_GuideJson`：
   - 存档就绪后加载 `m_CurGroup` / `m_LastGuideId`；
   - `SaveGroup` 经 `m_GuideSaveCall` 写入 SDK；
   - `m_ParseGuideDoCall` 查询已完成分组；
   - `UnInit` 释放 `SaveDataReady` 订阅。
   - 说明：引导系统当前处于休眠（未添加任何步骤），接线后未来启用即自动持久化。
3. **重新编译 GameDll**：`Assets/GameDll/GameDll.dll.bytes` / `GameDll.mdb.bytes` 已更新（含 `GuideSaveData` / `SaveGuideToSdk`）。

### 12.4 明确不纳入存档（非玩家存档数据）

- `PlayerInfo.level/exp/vip/guid/cfg_id/last_get_offline_award_time/login_time/online`：当前版本未使用（`account_level_up_wnd` 未被打开）。
- `PlayerPrefs "LanVer"`：只读遗留（恒为 0，实际语言走 `LobbyPlayer.GetLanguageType()`）。
- AB manifest 版本（`__abm_manifest_version__`）：资源热更缓存，非玩家存档。
- 每局人数 / 选角：会话内选择，不跨局持久化。
- `DataSaver`：未被调用的旧二进制存档基础设施。

### 12.5 验证状态

- Assembly-CSharp 编译通过（无 `error CS`）。
- GameDll 编译通过（仅历史 `QualityLevel` 过时警告），产物已拷贝到 `Assets/GameDll/`。
- 待真机验证：平台存档读写（`GameSaveLoaded`/`GameSaveStored` 通知）、换账号隔离、无档首启。

### 12.6 时序 / 异常修复（本轮）

| 问题 | 现象 | 修复 |
| --- | --- | --- |
| 存档拉取早于框架/provider 初始化 | Android 首次 `GetPlatformGameSave` 返回 `NotInitialized`，白等 8 秒才重试 | `GameSaveService` 组件提前创建，但 `Initialize()` 移到 `FrameworkCompositionRoot.Initialize()` + `SetPlayerCount()` 之后；`NotInitialized` 改为 0.5 秒快速重试 |
| `AudioManager` 错过 `SaveDataReady` | 存档在 `AudioManager` 创建前就绪（尤其编辑器同步就绪）时，音量/静音永远不应用 | 构造时先订阅，再立即调用一次 `ApplySavedVolumeSettings()`（已就绪即应用） |
| 登录 loading 未关闭 | 文件预加载先完成、存档后到时，`OnSaveDataReadyForStart` 只开放按钮，启动 loading 不关，卡在遮挡界面 | `OnSaveDataReadyForStart` 增加 `DoCloseLoading()` |
| 存档写入失败无重试 | `SetPlatformGameSave` 失败仅打日志，数据丢失 | 失败后 1 秒重试；编辑器 `NotInitialized` 只提示一次、不重试避免刷屏 |
| 存档拉取超时重试 | 超时后直接递归 `Initialize()` | 改为设置 `m_NextFetchRetryTime`，由 `Update` 统一调度，避免递归/重复请求 |
| 引导进度未持久化 | `m_GuideJson` 只声明未接线 | `GuideManager` 接线（见 12.3），并处理存档晚到（订阅 `SaveDataReady`） |

补充说明：`ThirdParty/YouDooUnity` 与 SDK 包 `YoudooGame.SDK.Adapter.YouDoo` 仍存在 `MonoSingleton<T>` / `Singleton<T>` 的 `CS0436` 重复类型**警告**（各程序集使用各自版本，编译通过）；属于"新旧原生代码并存"的已知遗留，不影响本次存档/启动链路。

### 12.7 平台存档不可用（appId 未映射）

真机日志：

```text
[IYMS] HandleIYMSgameServerCallback: {"notifyIYMSgameServerType":180007,
  "message":{"code":-3,"message":"应用 appId 映射缺失，请稍后重试: com.youdoo.yd1050"}}
[IYMS] 获取存档信息失败
```

- **根因在平台侧**：IYMS 平台没有为 `com.youdoo.yd1050` 配置 appId 映射，`GetGameSave` 必然失败。需要在平台后台注册该应用（客户端代码无法修复）。
- **按产品要求不做客户端降级**：存档未拿到平台成功结果前保持未就绪，登录页不会放开开始按钮；平台配置修复后即可正常进入。
- 客户端侧只保留快速/按时重试（`NotInitialized` 0.5 秒、其他错误 8 秒、失败通知 1 秒），不自行判定"无档"、不写默认档覆盖云端。

### 12.8 准备界面真机问题（相机冻结 / 挥手换角色）

| 问题 | 现象 | 根因 | 修复 |
| --- | --- | --- | --- |
| 框架相机画面只出第一帧 | `camera=Running`、`cameraFrame=1`、`views=0:on/tex@...`，但画面不刷新；举手准备、跟踪正常 | 新旧相机桥都通过 `youdoo_plugin` 的全局 `SetUnityTexturePtrVulkan` 指定原生写入目标。准备窗里的旧 `AndroidTextureBridgeVulkan` 晚于框架桥启动，把目标改成自己的 RenderTexture；我们只 `enabled=false`，既没解绑也没让框架桥重绑，原生仍在写旧 RT | `GameProductUiAdapter.RebindCameraTextureToNative()`：反射找到框架桥并重新 `BindRenderTextureToNative(CurrentTexture)`；`renderEventFunc=0` 或 `_isPaused` 时改走 `InitializeRuntime()` 重建。准备窗在 `BindFrameworkCameraViews` 停用旧桥后立即重绑，并在 `open/bind` 后按 0.2/0.8/1.8/3.2 秒补绑（覆盖纹理晚就绪/旧桥晚启动） |
| 左右挥手不能换角色 | 旧 SDK 支持，框架模式无响应 | 挥手识别写在旧 `AndroidParseDataDemo.EvaluatePrepareRoleSelection`（原生 KeyPoint 手腕横移速度），框架模式不订阅旧事件，新 SDK 也没有等价手势 | 新增 `PrepareRoleWaveDetector`（Assembly-CSharp）：**以人体中线（Chest / 双肩中点 / Nose 的 X）为界，手腕从一侧快速越到另一侧判定一次挥手**，方向取越过后的那一侧。越线速度用“该侧最外侧点 → 越线点”的平均速度计算，要求 ≥ 0.8 肩宽/秒且耗时 ≤ 0.6s，**慢慢把手移回去不会触发**。手腕需在头部以下、髋部以上；举手姿势期间不判定，放手后再锁 0.45s；判定后冷却 0.45s。命中后回调准备窗 `ChangeSelectedRole(seat, ±1)` |

诊断增强：`[新SDK整合] status` 增加 `bridges=名字:on/off/tex#实例ID/ev0|1/pause0|1`，`views` 的纹理改为 `tex#实例ID`。若画面仍冻结，可直接对比“框架桥纹理 ID / RawImage 纹理 ID / 原生是否在写”三者。

### 12.9 进入战斗卡死（loading 阶段）

- **根因判断**：战斗实时人像按约定仍由旧 SDK 提供，但框架 `CameraImage` 管线在进入战斗后仍在运行；新旧相机桥共用 `youdoo_plugin` 的全局纹理指针，切场景（大厅场景单场景卸载 → 战斗场景加载）时两套桥同时存在，存在原生渲染/纹理切换卡死风险。
- **修复（按真机日志四次调整）**：
  - 点击确认开战：**不再暂停相机**（`CameraImage.Pause` 会清空当前帧，准备界面画面会先消失），保持实时画面直到 loading 完全覆盖。
  - loading 覆盖后切场景前（`CGamePro_Lobby.BeginGoSceneAfterLoading`）：`Pause` → 等 0.3s → `StopCameraImage()`（解绑并释放框架 RT）→ 等 0.5s → `RestoreCameraViewsToLayout()` → `ExitLobby`。三段延时用于避开原生在途拷贝与旧场景卸载撞车。
  - `GameProductUiAdapter.RestoreCameraViewsToLayout()`：切场景前把框架相机视图还回 `CameraImageSplitLayout` 并隐藏，避免跟着大厅场景销毁。
  - `GameProductUiAdapter.ResumeCameraImage()`：回大厅重新打开准备窗时恢复相机并重新绑定。
  - 真机日志显示卡点在 `SceneManager.LoadSceneAsync`（`[入战加载] 调用 LoadSceneAsync` 后两帧内静默）。
- **排查日志**：为定位卡死点补充了 `camera pause/stop begin/end`、`[塔防入战] 开始加载关卡场景 / 空场景加载完成 / 关卡场景加载完成 / OnLoadMap begin/end`，以及 `[入战加载] 协程开始 / 请求 bundle / bundle 结果 / 调用 LoadSceneAsync / 等待场景激活(每60帧) / LoadSceneAsync 完成 / 场景切换回调完成`。

### 12.10 整合日志开关
- 新增 `GameSdkLog`（`Assets/Scripts/SDK/Framework/GameSdkLog.cs`），所有 `[新SDK整合]` 普通日志统一走 `GameSdkLog.Info`，`Enabled` 默认 `false`。
- 联调需要时把 `GameSdkLog.Enabled` 改为 `true` 即可恢复，不用逐个改回 `Debug.Log`；`Debug.LogWarning/LogError` 未接入开关，异常仍会输出。
- 每帧刷屏的 `adapter snapshot phase=...` / `adapter session snapshot lifecycle=...` 已直接注释。
- `[入战加载]` 场景加载日志暂时保留（只在大厅→战斗切场景时输出几次），用于继续定位进战斗卡死。

### 12.11 准备界面返回卡死（逻辑层收口）

- **现象**：准备界面按返回（Esc/遥控返回），第一次不一定卡，第二次进入准备界面再返回容易卡死。
- **根因**：相机生命周期原本放在窗口 `OnClose`（表现/关闭动画链路）以及窗口自己的重绑协程里。窗口存在“逻辑关闭”和“表现关闭”、关闭动画期间被复开、缓存/销毁等多种时序，SDK 相机的暂停/停止/重绑会随这些时序重复或错位，最终和旧纹理桥的原生清理撞在一起。
- **真机日志定位**：`[返回] prepare logical close begin -> close window` → `prepare OnClose begin/end` → **`PlayerTextuerShow 销毁` 之后卡死**。说明冻结点不在窗口逻辑，而在窗口销毁时 `PlayerTextuerShow` 上的纹理桥做原生清理。
- **深层原因（根因）**：准备窗里除框架相机桥外，还挂着 `PlayerTextuerShow` 上的纹理桥；受新旧 SDK 脚本 GUID 冲突影响，它实际解析成**新 SDK 的 `AndroidTextureBridgeVulkan`**。两个桥共用 `youdoo_plugin` 的全局 Vulkan 相机纹理：
  - 谁后启动谁抢写入目标（框架画面偶发停在别的纹理上）；
  - 谁先销毁谁做原生清理（`SetUnityTexturePtrVulkan(0)` + `GL.IssuePluginEvent(2)`），**共享纹理被提前释放**，另一套还在用它 → 下一帧卡死。
  - 之前只停用了“旧 vendor 全局类型”的桥，GUID 冲突后那个组件其实解析成新 SDK 类型，所以一直没被停用，重复生产者长期存在。
- **修复（把 SDK 逻辑从表现层移到大厅逻辑层）**：
  - `GameProductUiAdapter.EnterPrepareCamera()`：大厅**逻辑打开**准备界面前调用，恢复相机并启动统一的重绑补偿协程（协程归属 adapter，不再归窗口）。
  - `GameProductUiAdapter.DisableCompetingCameraBridges(windowRoot)`：准备界面**只保留框架相机桥**。在窗口 `OnInitComponent`（Start 之前）和 `BindFrameworkCameraViews` 两处调用，同时停用**新旧两种类型**的纹理桥，避免第二个生产者与框架桥抢同一个原生 Vulkan 纹理。
  - `GameProductUiAdapter.DisableLegacyCameraPresenter(windowRoot)`：框架模式下把准备窗里的旧链路相机表现层 `PlayerTextuerShow` **整个停用**（它就是重复生产者的宿主）。
  - `AndroidParseDataDemo.Start()`：框架会话下**跳过旧链路相机表现初始化**（`EnsureDefaultInitGameInfo` → `PlayerTextuerShow.InitGameInfo` / `PlayerMatchView` 分区 / 区域分割线），旧链路只保留战斗侧仍在用的部分。
  - `GameProductUiAdapter.ClosePrepareCameraWindow()`：大厅**逻辑关闭**准备界面时调用，顺序固定为
    `暂停 → 停止（销毁框架桥与 RT）→ 收回视图 → 关闭窗口`。
    关键点：必须**先停掉框架相机再关窗**——窗口销毁时 `PlayerTextuerShow` 的桥会做原生清理，
    框架桥还活着就会提前释放共享的 Vulkan 纹理导致卡死。
  - `GameProductUiAdapter.HandoffCameraBeforeScene()`：进战斗/返回登录前调用，固定顺序 `暂停 → 停止 → 收回视图 → 回调切场景`。
  - `tower_defend_prepare_wnd.OnClose` 不再处理任何 SDK/相机逻辑，只做窗口自身业务清理。
  - 调用点全部在逻辑层：`lobby_main_wnd.OpenPrepareWindow`（进入）、`CGamePro_Lobby.TryCloseCurrentLobbyActiveWindow`（返回大厅）、`CGamePro_Lobby.BackLogin`（返回登录）、`CGamePro_Lobby.BeginGoSceneAfterLoading`（进战斗）。
  - `[返回]` 日志保留：`lobby escape -> close active window / back login`、`prepare logical close begin -> close window`、`prepare OnClose begin/end`、`EnterBackLogin`。

### 12.12 选人界面人数错位 / 再次进入准备界面无图像 / 过渡白板

- **人数表现与数据错位**：选人界面的动画原来由**遥控光标列**（`RenderAPI.GetCurrentCol()`）决定，而数据由点击的按钮决定，两者可能不一致（界面两人、数据一人）。
  - **选人数界面每次进入都先回到单人模式**：`ResetSelectionToSinglePlayer()` 在 `SetRequest` 与 `OnOpen` 各执行一次，把请求人数置 1、遥控列置 0、画面切到单人；**不继承上一次选择，也不读取会话/请求里的其它人数数据**。
  - 点击人数按钮后，画面与遥控列同步到实际生效的人数。
  - `CGamePro_Lobby.TrySetPreparePlayerCount` 增加**会话人数生效校验**：没生效就回滚请求人数并提示“切换人数失败”，杜绝界面与数据分叉。
  - `lobby_main_wnd.OpenPrepareWindow` 打开准备界面前把**会话人数对齐到请求人数**，选 2 人时准备界面确实是 2 个槽位。
  - 准备界面打开时打印 `requestPlayerCount` 与 `sessionSlots` 自检行。
- **再次进入准备界面没有图像**：上一次关闭时窗口里 `PlayerTextuerShow` 的旧纹理桥是**延迟销毁**的，它的 `OnDestroy` 原生清理会在**新会话相机已启动之后**执行，`SetUnityTexturePtrVulkan(0)` 解绑新相机目标并释放共享 Vulkan 纹理。
  - 关闭流程改为：停掉框架相机 → **显式销毁**窗口里的旧纹理桥（`DestroyCompetingCameraBridges`）→ 再关窗；清理在框架相机不在场时执行，且不会拖到下一次进入准备界面。
  - 相机处于 `Stopped` 时的重启改为**等 2 帧再 Start**，避免旧桥组件（Destroy 帧末生效）与新桥有一帧共存。
- **过渡时不要看到白板**：新增 `HideFrameworkCameraViews()`，关闭准备窗/切场景一开始就把框架相机视图隐藏，相机暂停清帧后不会露出空白面板。
- **相机恢复的“假状态”问题（静态复查发现）**：
  - `CameraImage.Pause()` 作用在已经 `Stopped` 的相机上，会把状态改成 `Paused`，但内部桥此时已经不存在；后续 `Resume` 只会得到“有状态、没桥”的假恢复，表现为**再次进入准备界面没有图像**。修复：`PauseCameraImage` 遇到 `Stopped` 直接跳过。
  - 恢复统一走 `CameraImage.Start()`（而不是 `Resume`）：`Start` 既能续用已存在的桥，也能在桥被销毁时重建一个。
  - `BindFrameworkCameraViews` 每次 `RefreshView` 都会执行，所以里面**不能**调用 `EnterPrepareCamera`，否则重绑补偿协程会被不断重启、永远不触发；现在协程只在逻辑打开时启动一次。
- **选人数界面规则（最终）**：从大厅进入时固定单人模式（`SetRequest` + `OnOpen` 各复位一次）；选了人数进准备界面再返回时**不复位**（选人数界面是常驻窗口，`OnOpen`/`SetRequest` 不会再次触发）。
  - RawImage 没有纹理时默认渲染成**白色块**，所以隐藏时除了置空纹理还要把 `color` 清成透明；同时把准备窗里旧链路 `CameraTextureView` 的 RawImage 一起清掉（`RegisterPrepareWindowRoot` 登记窗口根节点）。
  - `RestoreCameraViewsToLayout()` **不再重新启用 split layout**：layout 的 `Apply` 会按 `manageViewActivation` 重新激活视图，视图一激活就回调 `CameraImage.Start()`，会在窗口还没销毁时重启相机（新旧纹理桥同帧共存）。
  - 关闭顺序固定为：**先隐藏 → 再清理（停相机 + 销毁旧桥）→ 再关窗**。
- **第一次进入准备界面人数不一致**：请求里的人数初值是 `TowerDefendSeatLayout.DefaultPlayerCount`（2），而**会话实际人数**是 1；正常大厅流程没有走 `TryQueuePrepareRequest`，没人把会话人数对齐到请求人数 → 选人界面按请求显示 2，准备界面按会话只有 1 个槽位。
  - `GameFrameworkBootstrap.ReadSessionPlayerCount()`：读会话实际生效人数。
  - `CGamePro_Lobby` 创建请求壳时用**会话实际人数**作为请求人数初值（关卡/无尽都改）。
  - `lobby_main_wnd.OpenPrepareWindow` 打开准备界面前把**会话人数对齐到请求人数**（`TrySetSessionPlayerCount`），选 2 人时准备界面确实是 2 个槽位。

---

## 附录 A：现状关键文件索引

- 匹配数据：`Assets/Scripts/SDK/GameDemo/AndroidParseDataDemo.cs`
- 匹配 UI：`GameDll/Src/GameLogic/Lobby/tower_defend_prepare_wnd.cs`（含 `tower_defend_prepare_face_controller`）
- 头像：`Assets/Scripts/SDK/GameDemo/HeadImageController.cs`、`Assets/Scripts/ThirdParty/YouDooUnity/AndroidServerInfo/HeadImage.cs`、`PlayerRoleManager.cs`
- 相机/骨骼渲染：`Assets/Scripts/ThirdParty/YouDooUnity/AndroidServerInfo/Rendering/`（`PlayerMatchView.cs`、`PlayerTextuerShow.cs`、`CameraImagView.cs`）
- 战斗实时人像：`Assets/Scripts/ThirdParty/YouDooUnity/AndroidServerInfo/CameraView/`（`AndroidTextureBridgeBase.cs`、`CameraTextureView.cs`、`CameraTextureViewManager.cs`、`BattleCameraTextureViewSmoothCalculation.cs`）
- 原生数据：`Assets/Scripts/ThirdParty/YouDooUnity/AndroidServerInfo/AndroidParseData.cs`（`DllImport youdoo_plugin`）
- 战斗输入：`Assets/Scripts/Game/BattleLogic/Battle/BoneInput/`（`BattleBoneParseData.cs`、`BoneFrameSourceResolver.cs`、`IBoneFrameSource.cs`）
- 手势识别：`Assets/Scripts/Game/BoneParser/`（`BoneParserRuntime*.cs`、`BoneGestureTuning.cs`、`BoneParserLib.dll`）
- 入口：`Assets/Scripts/Local/Classes/Main.cs`、`Assets/Scripts/SDK/GameDemo/AndroidServerInfoDemo.cs`

## 附录 B：SDK 文档入口

- 总览：`youdoogame-unity-sdk/Documentation~/sdk-framework-overview.zh-CN.md`
- AI 接入契约：`sdk-framework-usage.ai.md`
- 人工使用：`sdk-framework-usage.zh-CN.md`
- 完整接入指南：`sdk-framework-integration.zh-CN.md`
- 维护交接手册（头像/匹配 UI）：`sdk-framework-maintenance-handoff.zh-CN.md`
- 统一入口：`youdoo-api.zh-CN.md`
- 动作/手势模块：`Modules/tracking/Action/README.md`、`Modules/tracking/HandGesture/README.md`
