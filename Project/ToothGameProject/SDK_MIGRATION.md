# 新 SDK 迁移记录

## 配置确认

- Package：`com.youdoogame.framework` `v1.0.7`，Git revision `ec5b0e35572bedcdd4d83f0ee5945001bccf7d28`。
- Template：`1001` / `SPLIT_FIXED`，固定分屏。
- Gameplay rebind：`SlotScoped`。
- Lifecycle：`FrameworkManaged`。匹配与游戏阶段由 Package 的 canonical Host 管理；项目代码保留 UI、场景和玩法适配。
- 匹配界面：保留项目准备界面，由 `GameProductUiAdapter` 消费 Package 快照；头像按会话槽位读取，保留项目占位图和本地化提示。
- 当前会话资产仍配置 `GameplayEndMode=Timed`、`gameDurationSeconds=60`；本次未改计时策略。

## 清理和迁移

- 删除 `Assets/Scripts/ThirdParty/YouDooUnity`、旧 SDK 的 `GameDemo`、`UI`、`GameManager` 和 `SdkManager`。
- 删除旧 GUID 修复脚本、旧 SDK 测试脚本、未引用的 `playertextuershow.prefab`，以及 `test2.unity` 中旧 SDK 的 `curmodepanel` 测试页和其无引用预制体。
- 清理启动场景中的旧 `AndroidServerInfo`、`InputManager` 和 SDK 示例 UI，保留 `GameFrameworkBootstrap`。
- 清理准备界面/测试资源中的旧 Demo 组件与旧骨骼显示组件。
- `BattleBoneParseData` 改为从 `YoudooAPI.TryGetSessionSnapshot` 的正式槽位数据读取骨骼，并转换为项目自己的 `BoneFrameData`；游戏逻辑不再依赖旧 `AndroidParseData`、`AndroidParseDataDemo` 或 `YouDooSDKConstants`。
- 准备界面头像改用 `YoudooAPI.GetSessionPlayerAvatarSlot`；开局、重置、结算、重开、返回和暂停/恢复均连接到唯一 Framework Host。
- Editor 默认检测到 Package Synthetic Tracking Provider 时，通过 `FrameworkTemplateHost.SetRuntime` 注入项目自有的多槽位合成会话，恢复手动选座/确认开战；非 Editor 与 Editor Bridge/真实 Provider 不替换运行时。
- 保留 `Assets/Scripts/SDK/Framework`、项目骨骼解析/战斗逻辑、`com.youdoogame.framework` Package，以及 `libBoneParserCLib.so`。Package 内的 Native/Bridge 实现由 Package 管理，不再在 `Assets/Scripts/ThirdParty/YouDooUnity` 保留副本。

### 会话生命周期映射

| 阶段 | Package Host 边界 | 项目入口 |
| --- | --- | --- |
| Matching | `ResetSession` + `SetPlayerCount` | `lobby_main_wnd.OpenPrepareWindow` |
| Gameplay | `StartGameplay` | 准备界面全员就绪后确认 |
| Paused / resumed | `PauseGameplay` / `ResumeGameplay` | 塔防暂停请求；同时冻结/恢复项目战斗逻辑与音频 |
| Settlement | `CompleteGameplay` | 塔防战斗结果处理 |
| Return / retry | `ReturnToMatching` / `RestartGameplay` | 返回大厅 / 暂停菜单重开；结算后新一轮由 `OpenPrepareWindow` 重置 Host |


## 验证状态

- `GameDll.sln`：本轮再次构建成功，0 错误、8 个项目既有警告。
- Tuanjie Editor：新增 Editor 合成运行时代码已导入；当前 Console 未见编译错误（按当前 Editor 状态确认）。Unity MCP 当前连接的是另一个项目目录，未对其执行操作。
- 静态引用扫描：游戏代码和 `Assets` 场景/预制体中不再引用旧 Demo、`PlayerRoleManager`、旧输入管理器或旧骨骼类型。Framework 适配层对 `YouDooSDK.Native.AndroidServerInfo` / 相机桥的引用指向 Package 中的新 SDK 实现。
- 尚未运行 Play Mode、Editor 手动选座/确认开战、Android 构建或真机回归；准备界面、相机方向/分屏映射、手柄输入、骨骼动作、暂停恢复、结算及返回/重开需在目标设备验证后才能视为运行时验收通过。
