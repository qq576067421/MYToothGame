// [新SDK整合] SDK 性能旋钮。
// 换新 SDK 后战斗内固定开销来自摄像头采集、纹理更新与骨骼识别，全部跟随采集档位和派发帧率缩放。
// 这里集中成开关，真机排查时只改这一处即可做 A/B，避免散落在各适配层。
// 开关统一用 static readonly 而不是 const：const 会被编译器常量折叠，
// if (const false) / if (!const true) 这类写法会把分支后的语句判成不可达（CS0162），
// 而这些开关要保持真机可改。
public static class GameSdkPerfOptions
{
    // 准备界面 / 默认采集派发帧率。准备界面要显示真人画面，这里不降。
    // SDK 允许 1~30；这个值控制 GL.IssuePluginEvent 的调用次数，
    // 也就是每帧 native -> Unity 的整幅纹理搬运次数。
    public const int CameraTargetFps = 30;

    // 战斗内采集派发帧率。战斗 HUD 本身按 12fps 取帧，降到 12 不会让 HUD 看起来更卡，
    // 但能把整幅纹理搬运从每秒 30 次砍到 12 次。
    public const int BattleCameraTargetFps = 12;

    // 是否挂载 SDK 整合诊断组件：它每秒做一次反射探测并取一帧摄像头画面。
    // 只在联调阶段需要，正式包关闭可省掉这份常驻开销。
    public static readonly bool EnableIntegrationDiagnostics = false;

    // ---- 采集档位：启动时定死一档，运行期不再切换 ----
    //
    // 2026-11-13 真机实测（团结引擎 + Vulkan）推翻了这一段原来的前提。
    // 原先按“长边上限”在运行期换档，真机日志是：
    //   capture prepare ... res=640x480
    //   frame gate open ... res=640x480/prepare
    //   camera watchdog ... requested=640x480/prepare frame=4048x3040
    // 采集档位确实换了，但 Unity 侧 RenderTexture 还是 4048x3040：
    // SDK 的 GetTextureWidthVulkan() 只在 CameraTextureImage 桥初始化时读一次，
    // 运行期改 resolutions 不会让它回到“尺寸未解析”状态，纹理也就不会重建。
    // 结果是原生往 4048x3040 的纹理里写 640x480 的画面，四个座位框各裁 1/4，
    // 落在没有被写入的区域上——玩家看到的就是四个纯黑框。
    //
    // 所以改成“启动即定档”：在帧服务连接后、相机第一次启动前下发一次，
    // 之后准备界面与战斗共用同一档，全程不停流。这样纹理与采集输出必然一致，
    // 也顺带消掉了停流那一刻的 DEVICE_LOST（QueueSignalReleaseImageANDROID failed: -4）。
    //
    // 这里给确定的宽高而不是长边：SDK 的选择器只做精确匹配，没命中就按
    // InvalidSelectionPolicy 回退设备默认档，所以不会因为某台设备没有这一档而起不来。
    public const int BootstrapCaptureWidth = 640;
    public const int BootstrapCaptureHeight = 480;

    // 运行期切档开关：默认关闭，原因见上。
    // 关掉后准备/战斗共用启动档：进准备界面只恢复相机、不停流换档，
    // 看护协程也只做“同一档重启”，不再尝试别的档位。
    // 打开它之前必须确认 SDK 已经做到“换档即重建输出纹理”，否则必定画面错位。
    public static readonly bool EnableRuntimeCaptureStageSwitch = false;

    // 下面两个上限只在 EnableRuntimeCaptureStageSwitch 打开时才起作用，
    // 保留是为了让 GameSdkCameraCaptureFallback 的候选表逻辑保持完整。
    public const int PrepareCaptureLongEdge = 640;
    public const int BattleCaptureLongEdge = 640;

    // 战斗内选档策略：
    // true  = 在同比例可行档里取面积最小的一档（只受 MinBattleCapture* 下限约束）。
    //         实测本机 4:3 体系会落到 320x240，16:9 体系落到 480x270。
    // false = 取不超过 BattleCaptureLongEdge 的最大一档（保守）。
    // 战斗 HUD 只显示一个很小的真人头像，所以默认走最小档；
    // 若真机上骨架精度不够，把它改回 false 即退回 640 档。
    public static readonly bool BattleUseSmallestResolution = false;

    // 采集档位的绝对下限：低于这些尺寸后骨架识别精度会明显下降，宁可保持更大的档位。
    // 准备界面要看清真人，不能让小默认档（如 640x480）再往下裁；
    // 战斗内只显示小头像，可以再低一档。
    public const int MinPrepareCaptureWidth = 640;
    public const int MinPrepareCaptureHeight = 360;
    public const int MinBattleCaptureWidth = 320;
    public const int MinBattleCaptureHeight = 240;

    // 准备界面相机看护：视图可见但取不到画面（无帧或整幅纯黑）或帧比例被改动时，
    // 先在同一个阶段里退到“离当前档最近”的候选档，候选都试过才回设备默认档。
    // 没有异常时不会触发任何恢复动作。
    public static readonly bool EnableCameraHealthWatchdog = true;

    // 单个阶段在一台设备上最多尝试几个采集档（含首选）。
    // catalog 声明支持、原生侧实际起不来的档确实存在（实测 960x720 就是），
    // 这时不能一步跳回设备默认档（默认档可能是 4048x3040，战斗里直接卡死），
    // 而是沿“离它最近”的候选退一档；但也不能一直试——每试一次都要停相机、排空、重启，
    // 超过两次就是肉眼可见的黑屏空窗。所以上限设为 2：首选 + 最近邻。
    // 仍拿不到画面就退回设备默认档，保证画面与体感输入可用。
    public const int MaxCaptureStageAttempts = 2;

    // 停流前的静默等待：先 Pause 停派发，等原生在途 buffer 自己归还，再 Stop。
    //
    // 直接 Stop 一个正在 30fps 出帧的采集流（尤其 4048x3040 这种超大默认档）时，
    // native ImageReader 手上还有 buffer 正在 dequeue，桥组件销毁会去回收同一个 VkImage：
    //   QueueSignalReleaseImageANDROID failed: -4
    //   Surface freeAllBuffers: 1 buffers were freed while being dequeued!
    // 设备一旦 DEVICE_LOST，Vulkan 渲染就再也恢复不了，表现是整个游戏卡死。
    // 真机实测这段噪音从 Stop 开始持续 60~80ms，所以静默窗口给到 200ms 才稳。
    public const float CameraStopQuiesceSeconds = 0.2f;

    // 帧内容门控：相机没出画面前把四宫格整块透明，露出下面已经画好的底图。
    //
    // 默认关闭。它的判据是 16x16 缩略图 + AsyncGPUReadback 的最大 RGB，
    // 但真机实测这个读数恒为 255：native 用自己的命令流写这张 VkImage，
    // Unity 不知道内容已经更新，采样拿到的是未定义内容，
    // 判“黑”永远不成立——这层门控只剩开销，没有任何作用。
    // 画面正确性改为由“启动即定档”保证（见 BootstrapCaptureWidth 那段说明）。
    // 哪天 SDK 提供了可靠的内容查询，再把这里打开。
    public static readonly bool EnableCameraFrameGate = false;

    // 停流/重建后多久内不探测帧内容。这段时间读到的一定是黑帧或旧帧，
    // 探测既浪费一次 GPU 回读，又会让门控提前打开、把黑块放出去。
    public const float CameraFrameGateGraceSeconds = 0.5f;
}
