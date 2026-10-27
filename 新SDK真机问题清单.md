# 新 SDK 整合真机问题清单（交接文档）

> 用途：把本轮真机联调中发现的问题、根因与当前修法整理成一份可独立交接的文档，交给其他 AI 继续推进。
> 项目：牙齿（塔防），Unity + 团结引擎 1.8.4 / Unity 2022.3.62t6，Android 真机（Amlogic_HP52S0）。
> 新 SDK：`com.youdoogame.framework` v1.0.4（源码 `D:\ToGit\youdoogame-unity-sdk`，工程内安装副本 `Library/PackageCache/com.youdoogame.framework@ae30c28ee3`）。
> 相关大文档：`牙齿GameSdk整合方案文档.md`（§12 记录了逐条修复过程）。

---

## 0. 必须遵守的范围与设计约定

1. **战斗以外**（匹配、准备、结算、账号头像、相机、控制器）走**新 SDK**。
2. **战斗的实时人像**（相机裁剪跟随）与**战斗输入**（BoneParser 手势）继续用**旧 SDK**。
3. 存档用 SDK 平台存档（`YoudooAPI.GetPlatformGameSave/SetPlatformGameSave`），**不用 PlayerPrefs**、**不做降级**（未拿到平台成功结果前不放开始按钮）。
4. **不用陀螺仪**，相关功能必须关闭。
5. **相机/会话生命周期必须放在大厅逻辑层**（`CGamePro_Lobby` / `lobby_main_wnd`），**不能放在窗口 `OnClose`/关闭动画等表现层**。原因见 §3 机制A/B：表现层的关闭时序不确定，会和原生纹理清理撞车。
6. **选人数界面**：从大厅进入时固定单人模式；选了人数进准备界面再返回时**不复位**（该界面是常驻窗口，从未被关闭）。
7. **挥手换角色**：以**人体中线**为界，且必须是**快速摆动**（慢速把手移回去不算）。
8. **过渡不要出现白板**：顺序固定为 **先隐藏 → 再清理 → 再关闭**。

---

## 1. 工程信息

| 项 | 值 |
| --- | --- |
| 工程根 | `D:\tooth_git\Project\ToothGameProject` |
| GameDll 源码 | `D:\tooth_git\Project\ToothGameProject\GameDll\Src` |
| GameDll 编译 | `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" GameDll.csproj /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal /nologo`（在 `GameDll` 目录；会自动把 dll 拷到 `Assets/GameDll/GameDll.dll.bytes`） |
| Assembly-CSharp 编译 | 需团结引擎获得焦点才会重编译（脚本：最小化→还原窗口即可触发） |
| 编码 | 所有 `.cs` 必须 **UTF-8 带 BOM**；字段命名 `m_XxxYyy` |

### 关键文件

| 作用 | 路径 |
| --- | --- |
| 框架组合根 / 会话启动 | `Assets/Scripts/SDK/Framework/GameFrameworkBootstrap.cs` |
| 相机生命周期 + 视图挂载 + 绑定补偿 | `Assets/Scripts/SDK/Framework/GameProductUiAdapter.cs` |
| 准备界面（框架分支、旧链路分支、相机视图绑定） | `GameDll/Src/GameLogic/Lobby/tower_defend_prepare_wnd.cs` |
| 大厅流程（打开准备、返回、切战斗、返回登录） | `GameDll/Src/GameLogic/Lobby/CGamePro_Lobby.cs` |
| 大厅主界面（打开选关/选人数/准备界面） | `GameDll/Src/GameLogic/Lobby/lobby_main_wnd.cs` |
| 选人数界面 | `GameDll/Src/GameLogic/Lobby/tower_defend_player_count_wnd.cs` |
| 挥手换角色检测 | `Assets/Scripts/SDK/Framework/PrepareRoleWaveDetector.cs` |
| 旧链路数据/表现（战斗仍在用） | `Assets/Scripts/SDK/GameDemo/AndroidParseDataDemo.cs` |
| 旧链路相机表现层（重复生产者宿主） | `Assets/Scripts/ThirdParty/YouDooUnity/AndroidServerInfo/Rendering/PlayerTextuerShow.cs` |
| 真机诊断日志 | `Assets/Scripts/SDK/Framework/GameSdkIntegrationDiagnostics.cs` |
| 日志开关 | `Assets/Scripts/SDK/Framework/GameSdkLog.cs` |

---

## 2. 已发现的问题清单

### P1 准备界面实时画面只显示第一帧，之后不刷新

- **现象**：`camera=Running`、`cameraFrame=1`、`views=0:on/tex@...`，但画面不刷新；举手准备、骨骼跟踪正常。
- **根因（机制A+B）**：新旧两套纹理桥共用 `youdoo_plugin` 的**全局** Vulkan 相机纹理（`SetUnityTexturePtrVulkan`）。准备窗里的旧桥比框架桥晚启动，把原生写入目标改成了自己的 RT；只 `enabled=false` 不会解绑，框架视图就一直停在旧纹理。
- **修法**：`GameProductUiAdapter.RebindCameraTextureToNative()` 用反射找到框架桥并重新 `BindRenderTextureToNative(CurrentTexture)`；`renderEventFunc=0` 或 `_isPaused` 时改走 `InitializeRuntime()` 重建。绑定补偿协程只在**逻辑打开**时启动一次（见机制F）。

### P2 左右挥手不能换角色（旧 SDK 支持）

- **根因**：挥手识别写在旧 `AndroidParseDataDemo.EvaluatePrepareRoleSelection`（原生 KeyPoint 手腕横移速度），框架模式不订阅旧事件，新 SDK 也没有等价手势。
- **修法**：新增 `PrepareRoleWaveDetector`，用新 SDK `Player.Get(personId)` 的 18 关节按旧规则重算，命中后回调准备窗 `ChangeSelectedRole(seat, ±1)`。

### P3 抬手也会触发换角色

- **根因**：只看手腕横向位移，抬手时手臂划弧也产生横向分量。
- **修法（已被 P4 的方案取代）**：加“横向为主”约束。

### P4 慢慢把手移回来也会触发

- **现象**：挥过去触发一次，把手慢慢移回来又触发一次。
- **根因**：只判断“是否越过中线”，慢速回手同样越线。
- **修法（最终方案）**：
  - 以**人体中线**为界（中线 X 取 `Chest` → 双肩中点 → `Nose`），手腕从一侧越到另一侧才算一次挥手，方向取越过后的那一侧；抬手是纵向运动，不会越线；
  - 越线速度用「该侧最外侧点 → 越线点」的平均速度判定：`≥ 0.8 × 肩宽/秒` 且耗时 `≤ 0.6s`，慢速回手直接被过滤；
  - 手腕必须在头部以下、髋部以上；举手姿势期间不判定，且放手后再锁 0.45s；
  - 判定后冷却 0.45s。

### P5 开始游戏时准备界面的实时图像先消失

- **根因**：`CameraImage.Pause()` 会清空 provider 的当前帧，`CameraImageView` 下一帧就把纹理置空 → 画面变透明。
- **修法**：**点击确认开战后不暂停相机**；等 loading 完全覆盖后，在切场景前再走交还流程。

### P6 准备界面点击返回卡死（第二次进入准备界面再返回更易复现）

- **真机日志定位**：`[返回] prepare logical close begin -> close window` → `prepare OnClose begin/end` → **`PlayerTextuerShow 销毁` 之后卡死**。
- **根因（机制A+B+D）**：
  1. 准备窗里除框架相机桥外，还挂着 `PlayerTextuerShow` 上的纹理桥；受**新旧 SDK 脚本 GUID 冲突**影响，它实际解析成**新 SDK 的 `AndroidTextureBridgeVulkan`**（诊断日志里 `bridges=PlayerTextuerShow:on/tex#-6632` 就是它）。之前只停用了“旧 vendor 全局类型”的桥，**这个桥一直没被停用**，长期与框架桥抢同一个原生纹理。
  2. 两个桥共用同一份 Vulkan 纹理：谁后启动谁抢写入目标；**谁先销毁谁做原生清理**（`SetUnityTexturePtrVulkan(0)` + `GL.IssuePluginEvent(2)`），共享纹理被提前释放 → 另一套还在用 → 下一帧卡死。
  3. `UnityEngine.Object.Destroy` 是**帧末生效**，所以“停相机”和“销毁旧桥”如果同帧发生，新旧桥会共存一帧。
- **修法**：
  - `GameProductUiAdapter.DisableCompetingCameraBridges(windowRoot)`：**新旧两种类型**的纹理桥都在 `Start` 之前停用（调用点：准备窗 `OnInitComponent` 与 `BindFrameworkCameraViews`）；
  - `GameProductUiAdapter.DisableLegacyCameraPresenter(windowRoot)`：框架模式下把 `PlayerTextuerShow` 整个停用；
  - `AndroidParseDataDemo.Start()`：框架会话下跳过旧链路相机表现初始化（`EnsureDefaultInitGameInfo`）；
  - `GameProductUiAdapter.DestroyCompetingCameraBridges(windowRoot)`：**框架相机停掉之后**、关窗之前显式销毁窗口里的旧桥，让它的原生清理在框架相机不在场时执行，且不拖到下一次进入准备界面才做；
  - 关闭顺序固定：`Pause → Stop → 收回视图 → 关窗`（`ClosePrepareCameraWindow`），切场景顺序：`Pause → Stop → 收回视图 → 切场景`（`HandoffCameraBeforeScene`）。

### P7 再次进入准备界面没有图像

- **根因（机制B+C+D）**：
  1. 上一次关闭时旧桥是**延迟销毁**的，它的 `OnDestroy` 原生清理在**新会话相机已启动之后**才执行 → `SetUnityTexturePtrVulkan(0)` 把新相机的目标解绑；
  2. `CameraImage.Pause()` 作用在已 `Stopped` 的相机上 → 状态变成 `Paused` 但内部桥已没了 → 之后 `Resume` 得到“有状态、没桥”的假恢复（永远没帧）；
  3. `Destroy` 帧末生效 → 旧桥组件与新桥同帧共存。
- **修法**：
  - `PauseCameraImage` 遇到 `Stopped` **直接跳过**；
  - 恢复统一用 `CameraImage.Start()`（既能续用已有桥，也能在桥被销毁时重建），不用 `Resume()`；
  - 相机处于 `Stopped` 时的重启**等 2 帧再 Start**，确保上一次销毁的桥已真正消失；
  - 旧桥在关窗前被显式销毁（见 P6）。

### P8 选人数界面画面与数据不一致（显示两人、实际一人）

- **根因（机制F）**：选人数界面的动画由**遥控光标列**（`RenderAPI.GetCurrentCol()`）决定，而数据由**点击的按钮**决定，两者可能不同步。
- **修法**：画面与光标列跟随**实际生效的人数**；点击人数按钮后画面与光标列同步到生效值。

### P9 第一次进入准备界面：默认显示双人模式，实际只有一人

- **根因**：请求里的人数初值是 `TowerDefendSeatLayout.DefaultPlayerCount`（2），而**会话实际人数是 1**；正常大厅流程没有走 `TryQueuePrepareRequest`，没人把会话人数对齐到请求人数 → 选人数界面按请求显示 2，准备界面按会话只有 1 个槽位。
- **修法**：
  - 选人数界面每次进入固定**单人模式**（`ResetSelectionToSinglePlayer()`：`SetRequest` + `OnOpen` 各执行一次；不继承上次选择、不读其它数据）；
  - `lobby_main_wnd.OpenPrepareWindow` 打开准备界面前把**会话人数对齐到请求人数**（`TrySetSessionPlayerCount`）；
  - `CGamePro_Lobby.TrySetPreparePlayerCount` 增加**会话人数生效校验**：没生效就回滚人数并提示“切换人数失败：会话人数未生效，请重试。”；
  - 准备界面打开时打印 `requestPlayerCount` 与 `sessionSlots` 自检行。

### P10 返回 / 进战斗过渡时先看到白板

- **根因**：`RawImage` **没有纹理时默认渲染成白色块**；隐藏框架相机视图后，露出来的是旧链路 `CameraTextureView` 的 RawImage（无纹理 → 白块）。
- **修法**：
  - `GameProductUiAdapter.HideFrameworkCameraViews()`：隐藏时把 RawImage 的 `texture` 置空、**`color` 清成透明**，并把准备窗里旧链路 `CameraTextureView` 的 RawImage 一起清掉（`RegisterPrepareWindowRoot` 登记窗口根节点）；
  - 顺序：**先隐藏 → 再清理 → 再关闭**。

---

## 3. 根因机制汇总（接手时必须理解）

| 机制 | 说明 | 涉及修法 |
| --- | --- | --- |
| **A. 脚本 GUID 冲突** | 新旧 SDK 的同名脚本 GUID 冲突，prefab 上“旧”的纹理桥实际解析成**新 SDK** 的 `AndroidTextureBridgeVulkan`。按旧类型名去停用会漏掉它。 | `DisableCompetingCameraBridges` / `DestroyCompetingCameraBridges` 同时处理**新旧两种类型**（新的用反射取 `YouDooSDK.Native.AndroidTextureBridgeBase`） |
| **B. 全局 Vulkan 纹理共享** | `youdoo_plugin` 的 `SetUnityTexturePtrVulkan` 是**进程级全局**。两套桥同时活着：后启动的抢写入目标；先销毁的做原生清理（`SetUnityTexturePtrVulkan(0)` + `GL.IssuePluginEvent(2)`）→ 共享纹理被提前释放 → 另一套卡死或无图像。 | 准备界面只保留框架相机桥；相机交还必须“先停相机再关窗/切场景” |
| **C. CameraImage 状态机假状态** | `Pause` 会清帧并把状态置 `Paused`（桥已销毁时是假 `Paused`）；`Resume` 在桥不存在时只会得到“有状态、没桥”。 | `Pause` 遇 `Stopped` 跳过；恢复统一用 `Start()` |
| **D. Destroy 帧末生效** | `Object.Destroy` 的 `OnDestroy` 在帧末才跑；同帧新建的组件会与旧的共存一帧。 | 相机重启等 2 帧；旧桥在关窗前显式销毁 |
| **E. split layout 会重新激活视图** | `CameraImageSplitLayout.Apply` 按 `manageViewActivation` 重新 `SetActive` 视图 → 视图 `OnEnable` → `CameraImage.Start()` → **在窗口还没销毁时重启相机**。 | `RestoreCameraViewsToLayout` 不再 `layout.enabled = true`；视图激活只发生在准备界面绑定时 |
| **F. 刷新路径会重启协程** | `BindFrameworkCameraViews` 每次 `RefreshView`（快照变化，高频）都执行；在里面调 `EnterPrepareCamera` 会不断重启绑定补偿协程 → 一次都跑不到。 | 绑定补偿协程只在**逻辑打开**时启动一次 |

---

## 4. 当前实现状态

### 4.1 相机生命周期 API（`GameProductUiAdapter`，全部由大厅逻辑层调用）

| API | 作用 | 调用点 |
| --- | --- | --- |
| `EnterPrepareCamera(reason)` | 逻辑打开：恢复/重建相机 + 启动一次绑定补偿协程 | `lobby_main_wnd.OpenPrepareWindow`、准备窗 `OnOpen` |
| `ClosePrepareCameraWindow(reason, windowRoot, action)` | 逻辑关闭回大厅：`隐藏 → 暂停 → 停止 → 销毁旧桥 → 收回视图 → 关窗` | `CGamePro_Lobby.TryCloseCurrentLobbyActiveWindow`（返回准备界面时） |
| `HandoffCameraBeforeScene(reason, action)` | 切场景：`隐藏 → 暂停 → 停止 → 收回视图 → 回调切场景` | `CGamePro_Lobby.BeginGoSceneAfterLoading`（进战斗）、`CGamePro_Lobby.BackLogin`（返回登录） |
| `DisableCompetingCameraBridges(root)` | 停用新旧两种纹理桥 | 准备窗 `OnInitComponent` / `BindFrameworkCameraViews` |
| `DisableLegacyCameraPresenter(root)` | 停用旧链路 `PlayerTextuerShow` | 同上 |
| `DestroyCompetingCameraBridges(root)` | 框架相机停掉后销毁旧桥 | `ClosePrepareCameraWindow` 内部 |
| `HideFrameworkCameraViews()` | 隐藏视图并清成透明（消除白板） | 关闭/切场景流程开头 |
| `RebindCameraTextureToNative(reason)` | 重新绑定原生写入目标 | 绑定补偿协程（0.2/0.8/1.8/3.2s） |
| `RegisterPrepareWindowRoot(root)` | 登记准备窗根节点（用于隐藏旧链路显示控件） | 准备窗 `OnInitComponent` |

### 4.2 挥手判定（`PrepareRoleWaveDetector`）

- 中线 X：`Chest` → 双肩中点 → `Nose`；越线才算一次挥手，方向取越过后的那一侧；
- 速度：`(最外侧点→越线点) 距离 / 耗时 ≥ 0.8×肩宽/秒` 且 `耗时 ≤ 0.6s`；
- 手腕必须在头部以下、髋部以上；举手姿势不判定，放手后锁 0.45s；判定后冷却 0.45s；
- 触发后回调准备窗 `ChangeSelectedRole(seat, ±1)`。

### 4.3 人数规则

- 选人数界面：`SetRequest` + `OnOpen` 各复位成**单人**（不读会话/请求其它数据）；
- 选人数界面**常驻**（进准备界面时只是被盖住，没有关闭）→ 从准备界面返回时**保持上次选择，不复位**；
- 点人数按钮 → `TrySetPreparePlayerCount`（校验会话人数生效，失败回滚并提示）；
- 打开准备界面前 → `TrySetSessionPlayerCount(request 人数)`，保证准备界面槽位数 = 选定人数。

### 4.4 日志约定

- `[新SDK整合]` 普通日志统一走 `GameSdkLog.Info`，`GameSdkLog.Enabled` **默认 false**（联调时置 true 打开）；
- `Debug.LogWarning/Error` 未接入开关，异常仍会输出；
- 关键自检日志（常开，频率低）：
  - `[人数] 设置准备人数 request=N previous=M`
  - `[新SDK整合] prepare open ... requestPlayerCount=N sessionSlots=N`
  - `[返回] prepare logical close begin / -> close window`、`[返回] lobby escape -> ...`
- 诊断组件 `GameSdkIntegrationDiagnostics` 输出 `status ... bridges=... oldBridges=... views=...`，其中 `bridges` 会同时列出新旧两类桥，可用来确认“是否只有一个生产者”。

---

## 5. 待验证 / 风险点

1. **P1~P10 需真机回归**（最后一次编译：Assembly-CSharp + GameDll 均通过，无 `error CS`）。建议顺序：
   - 进选人数界面 → 应为单人；选 2 人 → 进准备界面应为 2 个座位；
   - 准备界面：实时画面持续刷新；抬手准备；左右挥手换角色；抬手/慢速回手不换角色；
   - 返回大厅：不能卡死、不能有白板；
   - 再次进入准备界面：实时画面要恢复；
   - 进入战斗：能进、过渡无白板。
2. **旧 SDK 与新 SDK 脚本 GUID 冲突未根治**：目前是运行时按类型停用/销毁来规避，prefab 上那个组件到底解析成哪个类型取决于 GUID 归属。建议后续用 `Tools/YouDoo/Re-GUID Vendored SDK` 重生成旧 vendor 的 GUID，彻底分开两套脚本。
3. **平台侧阻塞项**：`com.youdoo.yd1050` 缺 appId 映射 → `GetGameSave` 必然失败 → 登录页一直等存档就绪。这条只能平台侧修。
4. **战斗侧仍走旧 SDK**：战斗实时人像（`AndroidTextureBridge*` + `CameraTextureView`）与战斗输入（`BattleBoneParseData` / BoneParser）没有迁到新 SDK；进入战斗时框架相机会先停掉，避免与旧桥抢纹理。
5. `AndroidParseDataDemo` 在框架模式下仍会 `Awake/OnEnable`（只是不再做相机表现初始化与逐帧拉帧），如需彻底停用可再加一个总开关。

---

## 6. 交接给下一个 AI 的建议做法

1. 改动前先把「准备界面 → 返回 → 再次进入」和「准备界面 → 进战斗」两条路径按状态列成表（相机状态、视图父节点、桥的存活情况、谁在派发），再动代码；
2. 任何涉及原生纹理/桥的改动，都要确认**同一时刻只有一个生产者**，且**销毁顺序**是「先停相机 → 再销毁旧桥 → 再关窗/切场景」；
3. 相机相关逻辑一律放在 `CGamePro_Lobby` / `lobby_main_wnd` 等逻辑层，不要放回窗口 `OnClose`；
4. 改完用 `git diff --stat` 自查编码（所有 `.cs` 需 UTF-8 带 BOM），并确认 GameDll 与 Assembly-CSharp 都编译通过。
