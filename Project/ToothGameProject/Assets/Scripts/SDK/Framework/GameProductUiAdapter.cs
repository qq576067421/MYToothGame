using System;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using YoudooGame.CameraImage;
using YoudooGame.Core;
using YoudooGame.Framework;
using YoudooGame.MatchFlow.Runtime;

public sealed class GameProductUiAdapter : FrameworkProductUiAdapterBase
{
    public static GameProductUiAdapter Instance { get; private set; }

    public MatchSessionUiSnapshot UiSnapshot { get; private set; }

    public MatchSessionSnapshot SessionSnapshot { get; private set; }

    public static bool HasLiveSession =>
        Instance != null && Instance.IsAttached && Instance.UiSnapshot != null;

    public static CameraImageView[] SplitCameraViews => m_SplitCameraViews;

    public static CameraImageSplitLayout SplitCameraLayout => m_SplitCameraLayout;

    public event Action UiSnapshotChanged;

    public event Action<MatchAvatarSelectionSnapshot> AvatarSelectionChanged;

    public MatchAvatarSelectionSnapshot AvatarSelectionSnapshot =>
        YoudooAPI.TryGetAvatarSelectionSnapshot(out var snapshot) ? snapshot : null;

    // 头像槽位选择统一用 MatchFlow 的状态机：进入需要指定槽位，所以直接取宿主内的 flow；
    // 其余命令走宿主包装，保证平台选角期间的匹配保持逻辑一并生效。
    public Result<bool> BeginAvatarSlotSelection(int preferredSlotIndex)
    {
        MatchAvatarSelectionFlow flow = SessionHost != null ? SessionHost.AvatarSelectionFlow : null;
        return flow == null
            ? Result<bool>.Fail(ErrorCode.NotInitialized, "MatchSessionHost avatar selection flow is unavailable.")
            : flow.BeginSelection(preferredSlotIndex);
    }

    public Result<bool> HandleAvatarSelectionCommand(MatchAvatarSelectionCommand command)
    {
        return YoudooAPI.HandleAvatarSelectionCommand(command);
    }

    public Result<bool> HandleAvatarSelectionNavigation(Vector2 direction)
    {
        return YoudooAPI.HandleAvatarSelectionNavigation(direction);
    }

    private static CameraImageView[] m_SplitCameraViews;
    private static CameraImageSplitLayout m_SplitCameraLayout;

    // 准备界面看护状态：视图可见且持续 3 秒仍拿不到画面时，回退采集档位并重启相机。
    // 标记而不是 Coroutine 引用：协程随组件禁用自动终止，跨实例 StopCoroutine 会失效。
    private static bool s_CameraWatchdogRunning;
    private static bool s_CameraWatchdogConfirmed;
    private static bool s_CameraWatchdogRecovering;
    private static bool s_CameraWatchdogProbing;
    private static float s_SplitViewVisibleSince = -1f;
    private static int s_CameraWatchdogRecoverCount;
    // 准备界面首次拿到帧的宽高比基线：之后只要对不上，说明采集档位被改过，
    // 画面会被压扁，必须恢复设备默认档。
    private static float s_BaselineAspect;
    // 本次运行期记住“这台机器上该档位不可用”（黑屏或比例异常过），
    // 避免每次进界面都停一次相机、黑一下再自愈。
    private static bool s_PrepareCaptureUnavailable;
    private static bool s_BattleCaptureUnavailable;
    // 准备界面是否正在使用相机。
    //
    // 不能只靠“视图 GameObject 是否 activeInHierarchy”判断：视图的激活由准备窗逻辑控制，
    // 一旦它被提前关掉（切场景前收回、或沿用了上一次的隐藏状态），看护协程就会永久沉默。
    // 真机上出现过四个座位框全黑、而 watchdog 一次探测都没发起的情况，就是被这个判断挡住的。
    private static bool s_PrepareCameraInUse;

    // ---- 帧内容门控：相机没出画面前，不要把黑块/白块直接给玩家看 ----
    //
    // SDK 的 CameraImageView.Update 只在 GetFrame() 完全失败时把 RawImage 置透明；
    // 一旦 GetFrame() 成功但纹理内容是黑的（切档后 ImageReader 刚重建、
    // 或某档位在原生侧根本起不来），它照样 color=white 贴上去，玩家看到就是纯黑块。
    //
    // 所以在外层用 CanvasGroup 兜一层：内容没验证通过就整块透明，
    // 露出下面已经设计好的底图；验证通过再淡入。全程不改 SDK 组件、不动材质。
    private static bool s_FrameGateRunning;
    // 默认 true：门控关掉时（GameSdkPerfOptions.EnableCameraFrameGate=false）
    // 这个值不会再被任何逻辑改回 true，所以初值必须是“可用”，否则战斗头像会被永久判为不可用。
    private static bool s_FrameContentVisible = true;
    private static bool s_FrameContentProbing;
    // 每次停流都自增。正在进行的探测拿着旧代号，回来时对不上就直接丢弃结果，
    // 否则切档期间读完的那一批“黑帧”会在重开流之后把门控重新关掉。
    private static int s_FrameGateGeneration;
    private static float s_NextFrameProbeTime;
    private static float s_FrameGateGraceUntil;
    // 连续判黑次数。门控确认“有纹理但内容全黑”累计到阈值，说明这一档在本机就是出不了画面，
    // 必须交给看护流程换档。只隐身不换档的话，玩家看到的是四个空框，等于功能不可用。
    private static int s_FrameGateBlackStrikes;
    private const int FrameGateBlackStrikesBeforeRecover = 6;

    // 供战斗 HUD 判断“这张纹理现在能不能裁到头像上”。
    public static bool CameraFrameContentVisible => s_FrameContentVisible;

    protected override void OnEnable()
    {
        base.OnEnable();
        Instance = this;
        Debug.Log("[新SDK整合] adapter enable bound=" + IsBound + " attached=" + IsAttached);
        StartCameraWatchdog();
        StartFrameContentGate();
    }

    protected override void OnDisable()
    {
        // 协程会随组件禁用自动停止，这里只复位标记，下次 OnEnable 才能重新启动。
        s_CameraWatchdogRunning = false;
        s_CameraWatchdogRecovering = false;
        s_CameraWatchdogProbing = false;
        // 恢复配额也跟着重新启用重置，否则反复进出界面几次后就不再自愈。
        s_CameraWatchdogRecoverCount = 0;

        // 门控协程同样随组件禁用停止；visible 不在这里复位，
        // 以免门控关掉时把它留在 false、让战斗头像再也拿不到画面。
        s_FrameGateRunning = false;
        s_FrameContentProbing = false;
        s_FrameGateGeneration++;

        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDisable();
    }

    protected override void OnBound()
    {
        if (SessionHost != null)
        {
            SessionHost.AvatarSelectionChanged -= HandleAvatarSelectionChanged;
            SessionHost.AvatarSelectionChanged += HandleAvatarSelectionChanged;
        }
        Debug.Log("[新SDK整合] adapter bound, sessionHost=" + (SessionHost != null) + " attached=" + IsAttached);
    }

    protected override void OnUnbound()
    {
        if (SessionHost != null)
        {
            SessionHost.AvatarSelectionChanged -= HandleAvatarSelectionChanged;
        }
        Debug.Log("[新SDK整合] adapter unbound, sessionHost=" + (SessionHost != null));
    }

    private void HandleAvatarSelectionChanged(MatchAvatarSelectionSnapshot snapshot)
    {
        AvatarSelectionChanged?.Invoke(snapshot);
    }

    public static void RegisterFrameworkCameraViews(
        CameraImageView[] splitViews,
        CameraImageSplitLayout splitLayout)
    {
        m_SplitCameraViews = splitViews;
        m_SplitCameraLayout = splitLayout;
    }

    public static bool EnsureFrameworkCameraViews()
    {
        if (m_SplitCameraLayout == null)
        {
            var layouts = Resources.FindObjectsOfTypeAll<CameraImageSplitLayout>();
            for (int i = 0; i < layouts.Length; i++)
            {
                if (layouts[i] != null && layouts[i].gameObject.scene.IsValid())
                {
                    m_SplitCameraLayout = layouts[i];
                    break;
                }
            }
        }

        if (m_SplitCameraLayout == null)
        {
            Debug.LogError("[新SDK整合] camera view ensure failed: CameraImageSplitLayout is missing.");
            return false;
        }

        var views = m_SplitCameraViews;
        if (views == null || views.Length != 4)
        {
            var resizedViews = new CameraImageView[4];
            if (views != null)
            {
                Array.Copy(views, resizedViews, Mathf.Min(views.Length, resizedViews.Length));
            }

            views = resizedViews;
        }

        bool changed = false;
        bool reactivated = false;
        for (int i = 0; i < views.Length; i++)
        {
            if (views[i] == null)
            {
                var orphan = m_SplitCameraLayout.transform.Find("CameraImageView" + i);
                if (orphan != null)
                {
                    UnityEngine.Object.Destroy(orphan.gameObject);
                }

                var viewObject = new GameObject(
                    "CameraImageView" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage));
                viewObject.SetActive(false);
                viewObject.transform.SetParent(m_SplitCameraLayout.transform, false);
                views[i] = viewObject.AddComponent<CameraImageView>();
                views[i].StartOnEnable = false;
                // 新建的 RawImage 还没有纹理时会渲染成白块。门控开着时先挂上并保持透明，
                // 等验证出画面后再淡入；门控关着时这里直接给 1，不影响显示。
                ResolveSplitViewGroup(views[i]);
                viewObject.SetActive(true);
                changed = true;
            }
            else if (views[i].GetComponent<RawImage>() == null)
            {
                views[i].gameObject.AddComponent<RawImage>();
                changed = true;
            }

            // 视图会被 HideFrameworkCameraViews / RestoreCameraViewsToLayout 关掉（进战斗前、切场景时）。
            // 这里如果不重新激活，第二次进准备界面时 CameraImageView.Update 根本不会执行：
            // RawImage 不画 -> 露出座位框自己的黑底 -> 玩家看到四个纯黑框。
            if (views[i] != null && !views[i].gameObject.activeSelf)
            {
                views[i].gameObject.SetActive(true);
                reactivated = true;
                changed = true;
            }

            if (views[i] != null && views[i].StartOnEnable)
            {
                views[i].StartOnEnable = false;
                changed = true;
            }
        }

        m_SplitCameraViews = views;
        if (changed)
        {
            m_SplitCameraLayout.SetViews(views);
        }

        if (reactivated && CameraImage.Status.State != CameraImageState.Running)
        {
            // 刚重新激活的视图还贴着上一轮的残留（纹理已被 HideFrameworkCameraViews 清空，
            // 这时 RawImage 没有纹理就会画成白块）。相机没在出帧就先关掉门控，
            // 等原生写出本轮的帧、探测确认有内容后再淡入。
            // 相机还在 Running 时不用关：第一帧 Update 取到的就是有效帧。
            MarkCaptureSwitching("views-reactivated");
        }

        Debug.Log("[新SDK整合] camera views ensured changed=" + (changed ? "1" : "0") +
                  " count=" + views.Length + " layout=" + m_SplitCameraLayout.gameObject.name +
                  " layoutEnabled=" + m_SplitCameraLayout.enabled + " summary=" + BuildCameraViewSummary(views));
        return true;
    }

    private void StartCameraWatchdog()
    {
        if (!GameSdkPerfOptions.EnableCameraHealthWatchdog)
        {
            return;
        }

        if (s_CameraWatchdogRunning)
        {
            return;
        }

        s_CameraWatchdogRunning = true;
        StartCoroutine(CameraWatchdogLoop());
    }

    // 只盯准备界面：战斗 HUD 用自建视图，这里不介入。
    //
    // 判据不能只看视图 GameObject 的激活状态——见 s_PrepareCameraInUse 的说明。
    // 两个条件取或：准备相机标志为真（逻辑层已声明在用相机），或视图自己还是激活的。
    private static bool HasVisibleSplitView()
    {
        if (s_PrepareCameraInUse)
        {
            return m_SplitCameraViews != null;
        }

        var views = m_SplitCameraViews;
        if (views == null)
        {
            return false;
        }

        for (int i = 0; i < views.Length; i++)
        {
            if (views[i] != null && views[i].gameObject.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator CameraWatchdogLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);

            if (s_CameraWatchdogConfirmed || s_CameraWatchdogRecovering || s_CameraWatchdogProbing)
            {
                continue;
            }

            if (!HasVisibleSplitView())
            {
                // 离开准备界面后重新计时，回大厅再进要重新走一轮启动宽限。
                s_CameraWatchdogConfirmed = false;
                s_SplitViewVisibleSince = -1f;
                continue;
            }

            if (s_SplitViewVisibleSince < 0f)
            {
                s_SplitViewVisibleSince = Time.unscaledTime;
                continue;
            }

            // 相机启动本身就是两三帧的异步流程，先给 3 秒宽限再判定异常。
            if (Time.unscaledTime - s_SplitViewVisibleSince < 3f)
            {
                continue;
            }

            yield return ProbeSplitCameraHealth("prepare-watchdog");
        }
    }

    private static IEnumerator ProbeSplitCameraHealth(string reason)
    {
        s_CameraWatchdogProbing = true;

        var status = CameraImage.Status;
        var frameResult = CameraImage.GetFrame();
        string requested = GameSdkCameraCaptureFallback.CurrentResolutionText;
        if (!frameResult.Success || frameResult.Value.Texture == null)
        {
            Debug.LogWarning("[新SDK整合] camera watchdog reason=" + reason +
                             " no frame, requested=" + requested +
                             " state=" + status.State + " error=" + status.Error +
                             " msg=" + status.Message + " views=" + BuildCameraViewSummary(m_SplitCameraViews));
            s_CameraWatchdogProbing = false;
            yield return RecoverSplitCamera(reason + "+no-frame");
            yield break;
        }

        Texture texture = frameResult.Value.Texture;
        var renderTexture = texture as RenderTexture;
        int maxRgb = -1;
        if (renderTexture != null && renderTexture.width > 0 && renderTexture.height > 0 &&
            SystemInfo.supportsAsyncGPUReadback)
        {
            yield return new WaitForEndOfFrame();
            RenderTexture probe = RenderTexture.GetTemporary(
                16, 16, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(renderTexture, probe);
            RenderTexture.active = previous;

            var request = AsyncGPUReadback.Request(probe, 0, TextureFormat.RGBA32);
            while (!request.done)
            {
                yield return null;
            }

            if (!request.hasError)
            {
                var data = request.GetData<byte>();
                maxRgb = 0;
                for (int i = 0; i + 3 < data.Length; i += 4)
                {
                    int value = Mathf.Max(data[i], Mathf.Max(data[i + 1], data[i + 2]));
                    if (value > maxRgb)
                    {
                        maxRgb = value;
                    }
                }
            }

            RenderTexture.ReleaseTemporary(probe);
        }

        int frameWidth = frameResult.Value.Width;
        int frameHeight = frameResult.Value.Height;
        bool black = maxRgb >= 0 && maxRgb <= 4;
        // 准备界面一律用设备默认档：只要相机出的帧宽高比和默认不一致，
        // 说明有东西把采集档位改了，那就会让人物被压扁，必须立即改回默认。
        bool aspectAdjusted = IsAspectAdjusted(frameWidth, frameHeight);

        Debug.Log("[新SDK整合] camera watchdog reason=" + reason +
                  " state=" + status.State + "/" + status.GraphicsApi +
                  " requested=" + requested +
                  " frame=" + frameWidth + "x" + frameHeight +
                  " aspect=" + (aspectAdjusted ? "1" : "0") +
                  " maxRgb=" + maxRgb +
                  " views=" + BuildCameraViewSummary(m_SplitCameraViews));

        s_CameraWatchdogProbing = false;
        if (black)
        {
            // 整幅纯黑：原生相机没往纹理里写，按设备默认档重启一次。
            yield return RecoverSplitCamera(reason + "+black");
            yield break;
        }

        if (aspectAdjusted)
        {
            // 宽高比变了：要么采集档位被人改过，要么纹理尺寸与采集尺寸不一致（画面会错位）。
            // 运行期不换档时不去动档位——那只会让它更错，只把这件事完整记下来。
            Debug.LogWarning("[新SDK整合] camera frame aspect changed, restoring device default. " +
                             "frame=" + frameWidth + "x" + frameHeight +
                             " baseline=" + s_BaselineAspect.ToString("0.0000") +
                             " current=" + ((float)frameWidth / frameHeight).ToString("0.0000"));
            if (GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch)
            {
                // 下发过的档让原生把输出比例改了：后续选档跳过它，别在同一个坑里反复退。
                GameSdkCameraCaptureFallback.MarkKnownBad(
                    GameSdkCameraCaptureFallback.CurrentWidth, GameSdkCameraCaptureFallback.CurrentHeight);
                MarkCurrentCaptureUnavailable();
                GameSdkCameraCaptureFallback.TryRestoreDeviceDefault(reason + "+aspect");
            }

            s_BaselineAspect = 0f;
            s_CameraWatchdogConfirmed = true;
            yield break;
        }

        s_CameraWatchdogConfirmed = true;
        s_CameraWatchdogRecoverCount = 0;
    }

    // 用首次拿到的帧宽高比作为基线；之后的帧只要比例不同就认为采集档位被改过。
    private static bool IsAspectAdjusted(int frameWidth, int frameHeight)
    {
        if (frameWidth <= 0 || frameHeight <= 0)
        {
            return false;
        }

        if (s_BaselineAspect <= 0f)
        {
            s_BaselineAspect = (float)frameWidth / frameHeight;
            return false;
        }

        float aspect = (float)frameWidth / frameHeight;
        return Mathf.Abs(aspect - s_BaselineAspect) > s_BaselineAspect * 0.02f;
    }

    // 当前阶段在这台机器上已经没有可用档（候选表全被证伪，或比例怎么都对不上）：
    // 本次运行不再尝试该阶段，直接用设备默认档，避免每次进界面都黑一下、或者让人物被压扁。
    // 注意：单个档跑不起来不会直接走到这里，而是先剔除它并重新选“最近的一档”。
    private static void MarkCurrentCaptureUnavailable()
    {
        if (GameSdkCameraCaptureFallback.IsStage(GameSdkCameraCaptureFallback.StagePrepare))
        {
            s_PrepareCaptureUnavailable = true;
            return;
        }

        if (GameSdkCameraCaptureFallback.IsStage(GameSdkCameraCaptureFallback.StageBattle))
        {
            s_BattleCaptureUnavailable = true;
        }
    }

    // 恢复：先在同一个阶段里换到下一个候选档，仍然不行才回设备默认档。
    //
    // 不能一步跳回默认档：默认档可能是 4048x3040，准备界面会卡、战斗里直接把帧率打回去；
    // 也不能停在黑屏上：那等于功能不可用。所以按“本阶段再选一档 -> 默认档”两级退让，
    // 试到第几个档由 GameSdkPerfOptions.MaxCaptureStageAttempts 控制，不会把界面刷成重启循环。
    private static IEnumerator RecoverSplitCamera(string reason)
    {
        if (s_CameraWatchdogRecovering)
        {
            yield break;
        }

        if (s_CameraWatchdogRecoverCount >= GameSdkPerfOptions.MaxCaptureStageAttempts + 1)
        {
            // 候选与默认档都试过仍无画面：停在日志上，不再继续重建相机。
            s_CameraWatchdogConfirmed = true;
            Debug.LogWarning("[新SDK整合] camera watchdog gives up reason=" + reason +
                             " attempts=" + s_CameraWatchdogRecoverCount);
            yield break;
        }

        s_CameraWatchdogRecovering = true;
        s_CameraWatchdogRecoverCount++;

        // 运行期不换档（见 GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch）：
        // 换档只会改原生采集尺寸，不会重建 Unity 侧 RenderTexture，画面必然错位。
        // 所以这里只做“同一档重启”，把卡死的采集流拉回来。
        string stage = GameSdkCameraCaptureFallback.CurrentStage;
        yield return QuiesceCaptureBeforeStop(reason);
        StopCameraImage(reason);
        yield return WaitNativeCaptureRelease(reason);

        bool restored = true;
        if (GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch)
        {
            // 这个档位在本机跑不出来：记下来，后续选档直接跳过它。
            // 这样第二次进准备界面直接就落在可用档上，不再重复黑一下再自愈。
            GameSdkCameraCaptureFallback.MarkKnownBad(
                GameSdkCameraCaptureFallback.CurrentWidth, GameSdkCameraCaptureFallback.CurrentHeight);

            int beforeWidth = GameSdkCameraCaptureFallback.CurrentWidth;
            int beforeHeight = GameSdkCameraCaptureFallback.CurrentHeight;

            // 第一轮只在同一阶段内重新选档：刚被标黑的那个已在候选表里剔除，
            // 重新算出来的首选就是“离它最近、且仍满足降载目标”的一档。
            restored = false;
            if (!string.IsNullOrEmpty(stage) &&
                s_CameraWatchdogRecoverCount < GameSdkPerfOptions.MaxCaptureStageAttempts)
            {
                restored = GameSdkCameraCaptureFallback.TryApplyStage(stage, reason + "+near");
                if (restored && GameSdkCameraCaptureFallback.CurrentWidth == beforeWidth &&
                    GameSdkCameraCaptureFallback.CurrentHeight == beforeHeight)
                {
                    // 候选表没变（当前已是该阶段唯一可用档，例如本来就落在默认档上的降载），
                    // 再重启同一个档没有意义，往下走默认档。
                    restored = false;
                }

                if (!restored)
                {
                    // 该阶段候选全部被证伪：本次运行不再尝试这个阶段。
                    MarkCurrentCaptureUnavailable();
                }
            }

            if (!restored)
            {
                // 最后兑底：回到设备默认档。画面比例正确、体感输入可用，代价是性能。
                restored = GameSdkCameraCaptureFallback.TryRestoreDeviceDefault(reason + "+default");
            }
        }

        Debug.Log("[新SDK整合] camera watchdog recover reason=" + reason +
                  " from=" + (stage ?? "default") +
                  " to=" + (restored ? GameSdkCameraCaptureFallback.CurrentResolutionText : "none") +
                  " attempt=" + s_CameraWatchdogRecoverCount);
        // 重建相机要重新走一轮启动，给下一次探测重新计满宽限。
        s_SplitViewVisibleSince = Time.unscaledTime;

        yield return null;
        ResumeCameraImage(reason + (restored ? "+fallback" : string.Empty));
        EnableSplitCameraLayout();
        StartCameraRebindRoutine(reason + "+recover");

        s_CameraWatchdogRecovering = false;
    }

    private static string BuildCameraViewSummary(CameraImageView[] views)
    {
        if (views == null)
        {
            return "null";
        }

        var builder = new StringBuilder(160);
        for (int i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view == null)
            {
                builder.Append(i).Append(":null ");
                continue;
            }

            var rawImage = view.GetComponent<RawImage>();
            builder.Append(i).Append(':')
                .Append(view.gameObject.activeInHierarchy ? "on" : "off")
                .Append(" autoStart=").Append(view.StartOnEnable ? "on" : "off")
                .Append("#").Append(view.GetInstanceID())
                .Append('/').Append(rawImage != null && rawImage.texture != null ? "tex" : "none")
                .Append('@').Append(view.transform.parent != null ? view.transform.parent.name : "-")
                .Append(' ');
        }

        return builder.ToString();
    }

    // ---- 帧内容门控实现 ----

    private void StartFrameContentGate()
    {
        if (!GameSdkPerfOptions.EnableCameraFrameGate)
        {
            return;
        }

        if (s_FrameGateRunning)
        {
            return;
        }

        // 组件重新启用相当于一次冷启动：先当不可见处理，
        // 让之后新建的视图在创建时就带上正确 alpha，避免先闪一帧全白。
        s_FrameContentVisible = false;
        ApplySplitViewGate(0f, true);
        s_FrameGateRunning = true;
        StartCoroutine(CameraFrameGateLoop());
    }

    // 持续维护一个“当前相机画面能不能给玩家看”的判断：
    // 相机不在跑 -> 关门；刚停过/刚重建 -> 宽限期内不探；
    // 探到有内容 -> 淡入；探到黑帧或无帧 -> 保持透明。
    //
    // 这里不看准备界面视图是否可见：战斗 HUD 的实时头像用的是自建视图，
    // 它同样需要 CameraFrameContentVisible 来判断这张纹理能不能裁出来给玩家看。
    private IEnumerator CameraFrameGateLoop()
    {
        while (true)
        {
            yield return null;

            // 只有彻底停流才一定没有可用像素。
            // Starting / Paused 不能直接判死：准备界面返回选人数时相机就是 Paused，
            // 那时纹理里仍然是最后一帧有效画面；Starting 期间交给探测去试（探测失败会自己关门）。
            // 之前这里写的是“不等于 Running 就关闭”，结果准备界面只要相机没进入 Running
            // （档位起不来、或 resume 还在异步启动），四个座位框就永久透明，看起来就是全黑。
            if (CameraImage.Status.State == CameraImageState.Stopped)
            {
                s_FrameContentVisible = false;
                ApplySplitViewGate(0f, true);
                continue;
            }

            if (Time.unscaledTime < s_FrameGateGraceUntil)
            {
                continue;
            }

            if (!s_FrameContentProbing && Time.unscaledTime >= s_NextFrameProbeTime)
            {
                yield return ProbeFrameContent();
            }

            ApplySplitViewGate(s_FrameContentVisible ? 1f : 0f, false);
        }
    }

    // 抽样判内容：16x16 缩略图 + AsyncGPUReadback，开销和 watchdog 的一次探测同量级。
    // 可见时 1.5s 一次，不可见时 0.4s 一次——正常播放几乎不占 GPU，异常时能快速恢复。
    private static IEnumerator ProbeFrameContent()
    {
        s_FrameContentProbing = true;
        int generation = s_FrameGateGeneration;
        s_NextFrameProbeTime = Time.unscaledTime + (s_FrameContentVisible ? 1.5f : 0.4f);

        var frameResult = CameraImage.GetFrame();
        if (!frameResult.Success || frameResult.Value.Texture == null)
        {
            SetFrameContentVisible(false, "no-frame", -1);
            s_FrameContentProbing = false;
            yield break;
        }

        var renderTexture = frameResult.Value.Texture as RenderTexture;
        if (renderTexture == null || renderTexture.width <= 0 || renderTexture.height <= 0 ||
            !SystemInfo.supportsAsyncGPUReadback)
        {
            // 非 RenderTexture 或设备不支持回读：没有可靠的判据。
            // 这里按“可用”处理，否则会卡住本来就正常的画面。
            SetFrameContentVisible(true, "unverified", -1);
            s_FrameContentProbing = false;
            yield break;
        }

        yield return new WaitForEndOfFrame();
        RenderTexture probe = RenderTexture.GetTemporary(
            16, 16, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(renderTexture, probe);
        RenderTexture.active = previous;

        var request = AsyncGPUReadback.Request(probe, 0, TextureFormat.RGBA32);
        while (!request.done)
        {
            yield return null;
        }

        int maxRgb = -1;
        if (!request.hasError)
        {
            var data = request.GetData<byte>();
            maxRgb = 0;
            for (int i = 0; i + 3 < data.Length; i += 4)
            {
                int value = Mathf.Max(data[i], Mathf.Max(data[i + 1], data[i + 2]));
                if (value > maxRgb)
                {
                    maxRgb = value;
                }
            }
        }

        RenderTexture.ReleaseTemporary(probe);

        if (generation != s_FrameGateGeneration)
        {
            // 探测过程中相机又停了一次：这批读数属于上一轮流，直接丢掉。
            s_FrameContentProbing = false;
            yield break;
        }

        // 回读失败（maxRgb < 0）不判黑：宁可继续显示，
        // 也不要因为一次回读异常把正常画面挡掉。
        SetFrameContentVisible(maxRgb < 0 || maxRgb > 6, "readback", maxRgb);
        s_FrameContentProbing = false;
    }

    private static void SetFrameContentVisible(bool visible, string source, int maxRgb)
    {
        if (visible)
        {
            s_FrameGateBlackStrikes = 0;
        }
        else if (source == "readback" || source == "no-frame")
        {
            s_FrameGateBlackStrikes++;
        }

        if (s_FrameContentVisible != visible)
        {
            s_FrameContentVisible = visible;
            // 日志用 Debug.Log 而不是 GameSdkLog：后者在正式包里是关的（Enabled 默认 false），
            // 而这些状态变化每次切档才一条，正是真机排查最需要的东西。
            Debug.Log("[新SDK整合] frame gate " + (visible ? "open" : "closed") +
                      " source=" + source +
                      " maxRgb=" + maxRgb +
                      " state=" + CameraImage.Status.State +
                      " res=" + GameSdkCameraCaptureFallback.CurrentResolutionText);
        }

        // 连续多轮都拿不到画面：这一档在本机就是起不来，交给看护流程换档。
        // 只隐身不自愈的话，玩家看到的是四个空框——比黑块好一点，但功能仍然不可用。
        if (!visible && s_FrameGateBlackStrikes >= FrameGateBlackStrikesBeforeRecover &&
            s_PrepareCameraInUse && !s_CameraWatchdogRecovering && Instance != null)
        {
            s_FrameGateBlackStrikes = 0;
            Instance.StartCoroutine(RecoverSplitCamera("frame-gate-black"));
        }
    }

    // 相机要停流或重建：立刻关掉门控。
    // 不这么做的话，从 Stop 到原生写出第一帧之间 RawImage 会一直贴着上一次的帧，
    // 一旦纹理被重建，贴上去的就是全黑或全白。
    private static void MarkCaptureSwitching(string reason)
    {
        if (!GameSdkPerfOptions.EnableCameraFrameGate)
        {
            return;
        }

        s_FrameGateGeneration++;
        s_FrameContentVisible = false;
        s_FrameGateBlackStrikes = 0;
        s_NextFrameProbeTime = 0f;
        s_FrameGateGraceUntil = Time.unscaledTime + GameSdkPerfOptions.CameraFrameGateGraceSeconds;
        ApplySplitViewGate(0f, true);
        Debug.Log("[新SDK整合] frame gate closed reason=" + reason);
    }

    // 门控只动 CanvasGroup.alpha：不改 SDK 组件、不动材质、不动 RawImage 的 texture。
    // 目标是 0 时立即生效——不能淡出，淡出过程中玩家还是看得到黑块。
    private static void ApplySplitViewGate(float target, bool immediate)
    {
        var views = m_SplitCameraViews;
        if (views == null)
        {
            return;
        }

        for (int i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view == null)
            {
                continue;
            }

            CanvasGroup group = ResolveSplitViewGroup(view);
            if (group == null)
            {
                continue;
            }

            float current = group.alpha;
            float next = immediate || target <= 0f
                ? target
                : Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * 4f);
            if (!Mathf.Approximately(current, next))
            {
                group.alpha = next;
            }
        }
    }

    // 懒挂 CanvasGroup：四个视图是运行时创建的，预制体上没有这个组件。
    // 新建时按当前门控状态初始化 alpha，避免先闪一帧全白再变透明。
    private static CanvasGroup ResolveSplitViewGroup(CameraImageView view)
    {
        var group = view.GetComponent<CanvasGroup>();
        if (group != null)
        {
            return group;
        }

        group = view.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = s_FrameContentVisible ? 1f : 0f;
        return group;
    }

    // 框架相机视图会被准备窗临时挂到座位节点下；切场景前必须还给框架根节点，
    // 否则会跟着大厅场景一起销毁，返回大厅后无视图可用。
    public static void RestoreCameraViewsToLayout()
    {
        var views = m_SplitCameraViews;
        var layout = m_SplitCameraLayout;
        if (views == null || layout == null)
        {
            return;
        }

        int restored = 0;
        for (int i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view == null)
            {
                continue;
            }

            if (view.transform.parent != layout.transform)
            {
                view.transform.SetParent(layout.transform, false);
                restored++;
            }

            view.gameObject.SetActive(false);
        }

        // 不在场景交接中重新启用布局，避免 CameraImage 在窗口销毁前重新启动。
        Debug.Log("[新SDK整合] camera views restored to framework root, restored=" + restored);
    }

    public bool StartGameplay()
    {
        bool started = YoudooAPI.StartGameplay();
        Debug.Log("[新SDK整合] adapter StartGameplay bound=" + IsBound + " result=" + started);
        return started;
    }

    // 暂停时保留 CameraImage 的 RenderTexture 和原生绑定；准备界面与战斗 HUD 共用该纹理生产者。
    // 场景交接过程中不销毁 RT 或派发原生 cleanup，避免原生仍持有纹理指针时释放资源。
    public static bool PauseCameraImage(string reason)
    {
        var status = CameraImage.Status;
        if (status.State == CameraImageState.Paused)
        {
            Debug.Log("[新SDK整合] camera pause skipped reason=" + reason + ": already paused.");
            return true;
        }

        if (status.State == CameraImageState.Stopped)
        {
            // 已经是停止状态：再 Pause 会把状态改成 Paused，但内部桥已经没了，
            // 之后 Resume 只会得到一个没有桥的“假暂停”状态。
            Debug.Log("[新SDK整合] camera pause skipped reason=" + reason + ": already stopped.");
            return true;
        }

        bool paused = CameraImage.Pause().Success;
        Debug.Log("[新SDK整合] camera pause reason=" + reason +
                  " paused=" + (paused ? "1" : "0") +
                  " state=" + status.State + "/" + status.GraphicsApi);
        return paused;
    }

    public static bool ResumeCameraImage(string reason)
    {
        var previousStatus = CameraImage.Status;
        if (previousStatus.State == CameraImageState.Running)
        {
            return true;
        }

        if (previousStatus.State == CameraImageState.Starting)
        {
            Debug.Log("[新SDK整合] camera resume skipped reason=" + reason +
                      ": startup is already pending.");
            return true;
        }

        // 真正要重新开流了：从这一刻到原生写出第一帧之间，纹理内容完全不可信。
        MarkCaptureSwitching(reason);

        // 返回选人数只暂停相机，必须用 Resume 保留 Android provider 的原生纹理绑定；
        // 只有已彻底停止（例如切场景交接）才用 Start 重建 provider。
        bool resumed = previousStatus.State == CameraImageState.Paused
            ? CameraImage.Resume().Success
            : CameraImage.Start().Success;
        var currentStatus = CameraImage.Status;
        Debug.Log("[新SDK整合] camera resume reason=" + reason +
                  " resumed=" + (resumed ? "1" : "0") +
                  " state=" + previousStatus.State + "->" + currentStatus.State + "/" + currentStatus.GraphicsApi +
                  " error=" + currentStatus.Error + " msg=" + currentStatus.Message);
        return resumed;
    }

    public static bool StopCameraImage(string reason)
    {
        var status = CameraImage.Status;
        if (status.State == CameraImageState.Stopped)
        {
            Debug.Log("[新SDK整合] camera stop skipped reason=" + reason + ": already stopped.");
            return true;
        }

        Debug.Log("[新SDK整合] camera stop begin reason=" + reason +
                  " state=" + status.State + "/" + status.GraphicsApi);
        // 停流后视图像素就是上一次的残留：先在表现层关掉，
        // 不让玩家盯着最后一帧，也不让重建后的首帧黑块露出来。
        MarkCaptureSwitching(reason);
        bool stopped = CameraImage.Stop().Success;
        Debug.Log("[新SDK整合] camera stop end reason=" + reason +
                  " stopped=" + (stopped ? "1" : "0") +
                  " previous=" + status.State + "/" + status.GraphicsApi);
        return stopped;
    }

    // 停流前先让原生采集安静下来。
    //
    // 真机日志已证实：准备界面一打开就直接 Stop（当时相机正在 4048x3040/30fps 全速出帧），
    // Vulkan 立刻打 QueueSignalReleaseImageANDROID failed: -4 和 freeAllBuffers while being dequeued，
    // 设备 DEVICE_LOST 后渲染再也回不来——玩家看到的就是“直接卡死”。
    //
    // Pause 是公开接口，只停派发、不拆桥不解绑纹理；派发停后原生在途 buffer 会自己归还，
    // 等完再 Stop 就不会和 dequeue 抢同一个 buffer。这是纯粹把时序拉开，不碰底层。
    private static IEnumerator QuiesceCaptureBeforeStop(string reason)
    {
        PauseCameraImage(reason + "+quiesce");
        // 一帧让最后一次 IssuePluginEvent 走完，第二帧让它的渲染线程任务落地，
        // 再补一个实时时间下限盖住低帧率设备。
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(GameSdkPerfOptions.CameraStopQuiesceSeconds);
    }

    // 等原生采集输出彻底排空，再下发新配置。
    //
    // SDK 的 Stop 是非阻塞的：桥接组件被 Destroy 后要到帧末才真正销毁，
    // 它触发的 GL.IssuePluginEvent(func, 2)（解绑纹理 + 回收 VkImage）又在下一帧
    // 的渲染线程才被执行。渲染线程手上那一帧要等 GPU 队列走完才 release，
    // 只等一帧就改档位等于在它完成前把 ImageReader/Surface 整个换掉，Vulkan 上就会打：
    //   QueueSignalReleaseImageANDROID failed: -4  (VK_ERROR_DEVICE_LOST)
    //   Surface freeAllBuffers: 1 buffers were freed while being dequeued!
    // 真机实测这段噪音会持续 60~80ms，所以除了两帧再补一个实时时间下限。
    private static IEnumerator WaitNativeCaptureRelease(string reason)
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.05f);
        Debug.Log("[新SDK整合] native capture released reason=" + reason +
                  " from=" + GameSdkCameraCaptureFallback.CurrentResolutionText);
    }

    public static void EnterBattleCamera(string reason)
    {
        if (Instance != null)
        {
            ResumeCameraImage(reason);
            StartCameraRebindRoutine(reason);
        }
    }

    public static void ExitBattleCamera(string reason)
    {
        if (Instance != null)
        {
            PauseCameraImage(reason);
        }
    }

    public static bool TryGetCameraImageTexture(out Texture texture)
    {
        texture = null;
        if (Instance == null)
        {
            return false;
        }

        var frameResult = CameraImage.GetFrame();
        if (!frameResult.Success || frameResult.Value.Texture == null)
        {
            return false;
        }

        texture = frameResult.Value.Texture;
        return true;
    }

    // ---- 准备界面相机生命周期：由大厅逻辑层调用，不放在窗口 OnClose/表现层 ----

    // 准备界面逻辑打开：恢复相机并安排一次性的原生纹理绑定补偿。
    public static void EnterPrepareCamera(string reason)
    {
        // 从这一刻起准备界面在消费实时画面：看护协程按这个标志工作，
        // 不再依赖视图 GameObject 的激活状态（那个状态可能因为上一次切场景被关掉）。
        s_PrepareCameraInUse = true;

        // 每一轮进准备界面都必须重新确认画面。
        // confirmed 是“本轮流已确认可用”的一次性标记：上一轮置过 true 后如果没有被复位，
        // 看护协程会一直 skip 探测——真机上就出现过四个座位框全黑、而 watchdog 60 秒一条日志都没有。
        s_CameraWatchdogConfirmed = false;
        s_SplitViewVisibleSince = -1f;
        s_CameraWatchdogRecoverCount = 0;

        // 准备界面要真人画面，采集派发帧率不降。
        ApplyCameraTargetFps(GameSdkPerfOptions.CameraTargetFps, reason);

        if (!EnsureFrameworkCameraViews())
        {
            return;
        }

        if (CameraImage.Status.State == CameraImageState.Stopped)
        {
            // 相机是停止状态：先等上一次销毁的桥组件真正消失（Destroy 在帧末生效），
            // 先启动 provider，再激活 CameraImageView，避免 OnEnable 抢先调用 Start。
            StartCameraRestartRoutine(reason, GameSdkCameraCaptureFallback.StagePrepare);
            return;
        }

        if (s_PrepareCaptureUnavailable && GameSdkCameraCaptureFallback.NeedsSwitch(null))
        {
            // 准备档在这台机器上不可用：至少要从战斗档退回设备默认档，
            // 不能拿战斗档的小画面（可能还被压扁）来显示准备界面。
            StartCameraRestartRoutine(reason + "+restore-default", null);
            return;
        }

        if (NeedsPrepareCaptureSwitch())
        {
            // 还停在战斗档或设备默认档：重启相机流切到准备档。
            StartCameraRestartRoutine(reason + "+prepare-capture", GameSdkCameraCaptureFallback.StagePrepare);
            return;
        }

        // 框架视图关闭 StartOnEnable，由此处独占相机生命周期；先恢复 provider 再恢复布局。
        ResumeCameraImage(reason);
        EnableSplitCameraLayout();
        StartCameraRebindRoutine(reason);
    }

    // 准备界面要不要为换档重建相机流。
    //
    // 运行期不换档（GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch）时永远不需要：
    // 启动档已由 SDK 在相机第一次启动前定下来，准备与战斗共用同一档，全程不停流。
    private static bool NeedsPrepareCaptureSwitch()
    {
        if (!GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch)
        {
            return false;
        }

        if (s_PrepareCaptureUnavailable)
        {
            return false;
        }

        if (!GameSdkCameraCaptureFallback.NeedsSwitch(GameSdkCameraCaptureFallback.StagePrepare))
        {
            return false;
        }

        // 先只算不切：准备档与当前档本来就是同一分辨率时，只更新阶段标记、不动相机流。
        // 降档后准备与战斗通常是同一档，从战斗回来就不需要停流。
        // 停一次不仅会黑 0.3 秒，DEVICE_LOST（QueueSignalReleaseImageANDROID failed: -4）
        // 恰恰就发生在停流那一刻，能不停就不停。
        bool resolved = GameSdkCameraCaptureFallback.ResolveStageResolution(
            GameSdkCameraCaptureFallback.StagePrepare, out int prepareWidth, out int prepareHeight);
        if (resolved &&
            prepareWidth == GameSdkCameraCaptureFallback.CurrentWidth &&
            prepareHeight == GameSdkCameraCaptureFallback.CurrentHeight)
        {
            GameSdkCameraCaptureFallback.AdoptStage(GameSdkCameraCaptureFallback.StagePrepare);
            Debug.Log("[新SDK整合] prepare capture reused res=" + prepareWidth + "x" + prepareHeight);
            return false;
        }

        return true;
    }

    // 采集派发帧率只在变化时下发，避免重复重建 provider 状态。
    private static int s_AppliedCameraTargetFps = -1;

    private static void ApplyCameraTargetFps(int fps, string reason)
    {
        if (s_AppliedCameraTargetFps == fps)
        {
            return;
        }

        // SetTargetFps 只改派发间隔，不会重启相机；provider 未就绪时 service 会先记住，attach 时补发。
        var result = CameraImage.SetTargetFps(fps);
        s_AppliedCameraTargetFps = fps;
        Debug.Log("[新SDK整合] camera target fps=" + fps + " reason=" + reason +
                  " ok=" + (result.Success ? "1" : "0"));
    }

    private static void EnableSplitCameraLayout()
    {
        if (m_SplitCameraLayout != null && !m_SplitCameraLayout.enabled)
        {
            m_SplitCameraLayout.enabled = true;
        }
    }

    private static Coroutine s_CameraRestartCoroutine;

    private static void StartCameraRestartRoutine(string reason, string stage)
    {
        StopCameraRebindRoutine();
        var host = Instance;
        if (host == null || !host.isActiveAndEnabled)
        {
            ApplyCaptureStage(stage, reason);
            ResumeCameraImage(reason);
            EnableSplitCameraLayout();
            StartCameraRebindRoutine(reason);
            return;
        }

        if (s_CameraRestartCoroutine != null)
        {
            host.StopCoroutine(s_CameraRestartCoroutine);
            s_CameraRestartCoroutine = null;
        }

        s_CameraRestartCoroutine = host.StartCoroutine(CameraRestartRoutine(reason, stage));
    }

    private static IEnumerator CameraRestartRoutine(string reason, string stage)
    {
        yield return null;
        yield return null;

        if (GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch)
        {
            // 换档必须重建原生相机流：先确保已停止，再下发新档位，最后重新启动。
            // 从战斗回准备界面也走这条路径（战斗档 -> 准备档）。
            if (GameSdkCameraCaptureFallback.NeedsSwitch(stage))
            {
                // 停流前先静默：相机正在全速出帧时直接 Stop 会把 Vulkan 打成 DEVICE_LOST。
                yield return QuiesceCaptureBeforeStop(reason);
                StopCameraImage(reason);
                yield return WaitNativeCaptureRelease(reason);
                ApplyCaptureStage(stage, reason);
                yield return null;
            }
        }
        else if (CameraImage.Status.State == CameraImageState.Stopped)
        {
            // 运行期不换档：这条路径只用来把已经停掉的采集流拉起来，
            // 不做任何停流/排空/下发配置的动作（那只会让画面在错误的档位上重建）。
            Debug.Log("[新SDK整合] camera restart without stage switch reason=" + reason +
                      " state=" + CameraImage.Status.State);
        }

        s_CameraRestartCoroutine = null;
        ResumeCameraImage(reason);
        EnableSplitCameraLayout();
        StartCameraRebindRoutine(reason);
    }

    // stage 为空表示恢复设备默认档；否则按 GameSdkPerfOptions 的面积比例挑一档。
    //
    // 只在运行期换档开关打开时才会真的动档位：关掉后任何走到这里的调用都只打一条日志，
    // 不碰配置（换档不重建纹理，动了就是黑屏）。
    private static void ApplyCaptureStage(string stage, string reason)
    {
        if (!GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch)
        {
            Debug.Log("[新SDK整合] apply capture stage skipped reason=" + reason +
                      " stage=" + (stage ?? "default") + ": runtime stage switch is off.");
            return;
        }

        if (string.IsNullOrEmpty(stage))
        {
            if (!GameSdkCameraCaptureFallback.IsDeviceDefault)
            {
                GameSdkCameraCaptureFallback.TryRestoreDeviceDefault(reason);
            }

            return;
        }

        if (GameSdkCameraCaptureFallback.TryApplyStage(stage, reason))
        {
            return;
        }

        // 这台机器拿不到更低档的同比例档位：本次运行不再尝试该阶段。
        if (stage == GameSdkCameraCaptureFallback.StagePrepare)
        {
            s_PrepareCaptureUnavailable = true;
        }
        else
        {
            s_BattleCaptureUnavailable = true;
        }
    }

    // 准备界面关闭 / 切场景时立刻隐藏框架相机视图：
    // 相机暂停后帧会被清空，直接留着会看到一块空白（白板）；
    // 而且 RawImage 没有纹理时默认渲染成白色块，隐藏时必须把颜色清成透明。
    public static void HideFrameworkCameraViews()
    {
        var views = m_SplitCameraViews;
        if (views != null)
        {
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null)
                {
                    continue;
                }

                var raw = views[i].GetComponent<RawImage>();
                if (raw != null)
                {
                    raw.texture = null;
                    raw.color = Color.clear;
                }

                views[i].gameObject.SetActive(false);
            }
        }

        // 视图关掉了就不再是“准备界面在用相机”，看护协程随即停止探测。
        s_PrepareCameraInUse = false;
    }

    // 关闭准备窗回选人数：暂停框架相机、收回视图，再关闭窗口；保留 provider 供再次进入时 Resume，
    // 避免销毁底层纹理绑定后，Android 再次进入准备界面时丢失图像。
    public static void ClosePrepareCameraWindow(
        string reason,
        GameObject windowRoot,
        Action closeWindowAction)
    {
        StopCameraRebindRoutine();
        HideFrameworkCameraViews();

        var host = Instance;
        if (host == null || !host.isActiveAndEnabled)
        {
            PauseCameraImage(reason);
            RestoreCameraViewsToLayout();
            Debug.Log("[新SDK整合] prepare camera returned paused reason=" + reason +
                      " state=" + CameraImage.Status.State +
                      " window=" + (windowRoot != null ? windowRoot.name : "null"));
            closeWindowAction?.Invoke();
            return;
        }

        host.StartCoroutine(ClosePrepareWindowRoutine(reason, windowRoot, closeWindowAction));
    }

    private static IEnumerator ClosePrepareWindowRoutine(
        string reason,
        GameObject windowRoot,
        Action closeWindowAction)
    {
        PauseCameraImage(reason);
        yield return new WaitForSecondsRealtime(0.3f);

        RestoreCameraViewsToLayout();
        Debug.Log("[新SDK整合] prepare camera returned paused reason=" + reason +
                  " state=" + CameraImage.Status.State +
                  " window=" + (windowRoot != null ? windowRoot.name : "null"));
        closeWindowAction?.Invoke();
    }

    // 切场景前先暂停并收回视图；只有退出会话时才停止 CameraImage，进战斗保留纹理生产者供 HUD 复用。
    public static void HandoffCameraBeforeScene(string reason, Action onCompleted, bool stopCameraAfterPause = true)
    {
        // 进战斗降采集派发帧率；退出会话保持默认。
        ApplyCameraTargetFps(
            stopCameraAfterPause ? GameSdkPerfOptions.CameraTargetFps : GameSdkPerfOptions.BattleCameraTargetFps,
            reason);

        StopCameraRebindRoutine();
        HideFrameworkCameraViews();

        var host = Instance;
        if (host == null || !host.isActiveAndEnabled)
        {
            PauseCameraImage(reason);
            if (stopCameraAfterPause)
            {
                StopCameraImage(reason);
            }
            RestoreCameraViewsToLayout();
            onCompleted?.Invoke();
            return;
        }

        host.StartCoroutine(HandoffCameraBeforeSceneRoutine(reason, onCompleted, stopCameraAfterPause));
    }

    private static IEnumerator HandoffCameraBeforeSceneRoutine(string reason, Action onCompleted, bool stopCameraAfterPause)
    {
        PauseCameraImage(reason);
        yield return new WaitForSecondsRealtime(0.3f);

        if (stopCameraAfterPause)
        {
            StopCameraImage(reason);
            yield return new WaitForEndOfFrame();
        }
        else if (GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch &&
                 GameSdkPerfOptions.EnableBattleCaptureDowngrade && !s_BattleCaptureUnavailable)
        {
            // 进战斗：HUD 只显示一个很小的真人头像，比准备界面再降一档。
            // 先只算不切：若这台设备上战斗档与准备档本来就是同一档（低端机常见），
            // 就不重建相机流——停一次会黑 0.3 秒，还会打驱动侧的 buffer 归还日志。
            bool resolved = GameSdkCameraCaptureFallback.ResolveStageResolution(
                GameSdkCameraCaptureFallback.StageBattle,
                out int battleWidth,
                out int battleHeight);
            bool needRebuild = resolved &&
                               (battleWidth != GameSdkCameraCaptureFallback.CurrentWidth ||
                                battleHeight != GameSdkCameraCaptureFallback.CurrentHeight);

            if (!resolved)
            {
                // 这台机器没有更低档的同比例档位：保持准备档，本次运行不再重试。
                s_BattleCaptureUnavailable = true;
                yield return new WaitForEndOfFrame();
            }
            else if (!needRebuild)
            {
                // 战斗档与准备档是同一档：只改阶段标记，相机流不动。
                GameSdkCameraCaptureFallback.AdoptStage(GameSdkCameraCaptureFallback.StageBattle);
                yield return new WaitForEndOfFrame();
            }
            else
            {
                // 换档必须重建原生相机流，借这 0.3s 暂停窗口先停、再按新档位启动。
                StopCameraImage(reason + "+battle-capture");
                yield return WaitNativeCaptureRelease(reason + "+battle-capture");
                bool downgraded = GameSdkCameraCaptureFallback.TryApplyStage(
                    GameSdkCameraCaptureFallback.StageBattle,
                    reason);
                if (!downgraded)
                {
                    s_BattleCaptureUnavailable = true;
                }

                yield return null;
                ResumeCameraImage(reason + (downgraded ? "+battle-capture" : string.Empty));
                yield return null;
                yield return null;

                if (downgraded)
                {
                    StartBattleCameraWatchdog(reason);
                }
            }
        }
        else
        {
            yield return new WaitForEndOfFrame();
        }

        RestoreCameraViewsToLayout();
        onCompleted?.Invoke();
    }

    // 战斗内降档后如果 2 秒内拿不到新的骨骼帧，或者实际输出的帧宽高与降档不一致，
    // 说明这个档位在原生侧没跑起来 / 改了画面比例，恢复设备默认档位再重启一次，
    // 避免整场战斗失去体感输入或人物被压扁。
    private static void StartBattleCameraWatchdog(string reason)
    {
        var host = Instance;
        if (host == null || !host.isActiveAndEnabled)
        {
            return;
        }

        host.StartCoroutine(BattleCameraWatchdog(reason));
    }

    private static IEnumerator BattleCameraWatchdog(string reason)
    {
        var provider = PlatformServices.Get<IPlayerTrackingProvider>();
        long baseline = GetLatestFrameId(provider);

        yield return new WaitForSecondsRealtime(2f);

        // 这 2 秒内可能已经切回准备档或设备默认档，以实际阶段为准。
        if (!GameSdkCameraCaptureFallback.IsStage(GameSdkCameraCaptureFallback.StageBattle))
        {
            yield break;
        }

        // 降档后实际下发的档位就是这里的基准：原生侧如果没接受或换了输出比例，就会对不上。
        int expectedWidth = GameSdkCameraCaptureFallback.CurrentWidth;
        int expectedHeight = GameSdkCameraCaptureFallback.CurrentHeight;
        float expectedAspect = expectedWidth > 0 && expectedHeight > 0
            ? (float)expectedWidth / expectedHeight
            : 0f;

        long current = GetLatestFrameId(provider);
        float actualAspect = 0f;
        var frameResult = CameraImage.GetFrame();
        if (frameResult.Success && frameResult.Value.Width > 0 && frameResult.Value.Height > 0)
        {
            actualAspect = (float)frameResult.Value.Width / frameResult.Value.Height;
        }

        bool framesAlive = current > baseline;
        // 拿不到 CameraImage 帧或拿不到下发档尺寸时不做比例判定，只靠骨骼帧是否推进来定。
        bool aspectOk = actualAspect <= 0f || expectedAspect <= 0f ||
                        Mathf.Abs(actualAspect - expectedAspect) <= expectedAspect * 0.02f;
        if (framesAlive && aspectOk)
        {
            yield break;
        }

        Debug.LogWarning("[新SDK整合] battle capture rollback reason=" + reason +
                         " frames=" + baseline + "->" + current +
                         " aspect=" + actualAspect.ToString("0.0000") +
                         " expected=" + expectedAspect.ToString("0.0000") +
                         " res=" + GameSdkCameraCaptureFallback.CurrentResolutionText);
        // 这个战斗档在本机跑不起来（2 秒内没有新骨骼帧 / 输出比例对不上）：记下来，
        // 后续选档直接跳过，避免每次进战斗都在同一个档上白停一次相机。
        int beforeWidth = GameSdkCameraCaptureFallback.CurrentWidth;
        int beforeHeight = GameSdkCameraCaptureFallback.CurrentHeight;
        GameSdkCameraCaptureFallback.MarkKnownBad(beforeWidth, beforeHeight);
        yield return QuiesceCaptureBeforeStop(reason + "+battle-rollback");
        StopCameraImage(reason + "+battle-rollback");
        yield return WaitNativeCaptureRelease(reason + "+battle-rollback");

        // 退让顺序：同阶段重新选档 -> 准备档（本机已跑出过画面）-> 设备默认档。
        // 不能一步跳默认档：默认档可能是 4048x3040，会把刚降下来的帧率又打回去；
        // 也不能停在坏档上：拿不到骨骼帧等于体感冒险不可用。
        bool restored = GameSdkCameraCaptureFallback.TryApplyStage(
            GameSdkCameraCaptureFallback.StageBattle, reason + "+battle-near");
        if (restored && GameSdkCameraCaptureFallback.CurrentWidth == beforeWidth &&
            GameSdkCameraCaptureFallback.CurrentHeight == beforeHeight)
        {
            // 候选表没变：这个阶段只剩这一档，换档救不回来，直接往下走准备档。
            restored = false;
        }

        if (!restored && !s_PrepareCaptureUnavailable)
        {
            restored = GameSdkCameraCaptureFallback.TryApplyStage(
                GameSdkCameraCaptureFallback.StagePrepare, reason + "+battle-rollback");
        }

        if (!restored)
        {
            restored = GameSdkCameraCaptureFallback.TryRestoreDeviceDefault(reason + "+battle-rollback");
        }

        // 只有退让链全部失败才算“战斗档不可用”；否则只是换了一档继续打。
        if (!restored)
        {
            s_BattleCaptureUnavailable = true;
        }

        // 档位已变：准备界面的比例基线重新采样，避免拿降档帧当基线。
        s_BaselineAspect = 0f;
        yield return null;
        ResumeCameraImage(reason + "+battle-restore");
    }

    private static long GetLatestFrameId(IPlayerTrackingProvider provider)
    {
        if (provider == null)
        {
            return 0;
        }

        var frame = provider.GetLatestPlayerFrame();
        return frame.Success ? frame.Value.FrameId : 0;
    }

    private static Coroutine s_CameraRebindCoroutine;

    private static void StartCameraRebindRoutine(string reason)
    {
        StopCameraRebindRoutine();
        var host = Instance;
        if (host == null || !host.isActiveAndEnabled)
        {
            return;
        }

        s_CameraRebindCoroutine = host.StartCoroutine(CameraRebindRoutine(reason));
    }

    private static void StopCameraRebindRoutine()
    {
        if (s_CameraRebindCoroutine != null && Instance != null)
        {
            Instance.StopCoroutine(s_CameraRebindCoroutine);
        }

        s_CameraRebindCoroutine = null;
    }

    private static IEnumerator CameraRebindRoutine(string reason)
    {
        float[] delays = { 0.2f, 0.8f, 1.8f, 3.2f };
        for (int i = 0; i < delays.Length; i++)
        {
            yield return new WaitForSecondsRealtime(delays[i]);
            RebindCameraTextureToNative(reason + "+" + delays[i].ToString("F1"));
        }

        s_CameraRebindCoroutine = null;
    }

    // SDK 的 Vulkan texture pointer 是全局写入目标；相机状态恢复后重新绑定当前 CameraImage 纹理。
    public static bool RebindCameraTextureToNative(string reason)
    {
        Type bridgeType = ResolveTypeByName(kCameraBridgeTypeName);
        if (bridgeType == null)
        {
            Debug.LogWarning("[新SDK整合] camera rebind skipped reason=" + reason + ": bridge type missing.");
            return false;
        }

        Texture liveTexture = null;
        var frameResult = CameraImage.GetFrame();
        if (frameResult.Success)
        {
            liveTexture = frameResult.Value.Texture;
            RestoreFrameworkCameraViewTextures(liveTexture, reason);
        }

        var objects = Resources.FindObjectsOfTypeAll(bridgeType);
        Component target = null;
        Texture targetTexture = null;
        var summary = new StringBuilder();
        for (int i = 0; i < objects.Length; i++)
        {
            var component = objects[i] as Component;
            if (component == null || !component.gameObject.scene.IsValid())
            {
                continue;
            }

            var behaviour = component as Behaviour;
            bool usable = behaviour != null && behaviour.enabled && component.gameObject.activeInHierarchy;
            Texture texture = ReadCurrentTexture(component);
            summary.Append('[').Append(component.gameObject.name)
                .Append(usable ? ":on/" : ":off/")
                .Append(texture != null ? "tex" : "none")
                .Append(']');

            if (!usable || texture == null)
            {
                continue;
            }

            if (liveTexture != null && ReferenceEquals(texture, liveTexture))
            {
                target = component;
                targetTexture = texture;
                break;
            }

            if (target == null)
            {
                target = component;
                targetTexture = texture;
            }
        }

        if (target == null)
        {
            Debug.LogWarning("[新SDK整合] camera rebind skipped reason=" + reason +
                             " bridges=" + summary + " liveTex=" + (liveTexture != null ? "yes" : "no"));
            return false;
        }

        bool dispatchReady = ReadRenderEventFunc(target) != IntPtr.Zero && !ReadPaused(target);
        if (!dispatchReady)
        {
            bool restarted = TryRestartBridge(target);
            Debug.Log("[新SDK整合] camera rebind reason=" + reason +
                      " target=" + target.gameObject.name + " restart=" + (restarted ? "1" : "0") +
                      " bridges=" + summary);
            return restarted;
        }

        bool bound = TryBindRenderTexture(target, targetTexture);
        Debug.Log("[新SDK整合] camera rebind reason=" + reason +
                  " target=" + target.gameObject.name +
                  " tex=" + targetTexture.GetInstanceID() +
                  " live=" + (liveTexture != null && ReferenceEquals(targetTexture, liveTexture) ? "1" : "0") +
                  " bound=" + (bound ? "1" : "0") +
                  " bridges=" + summary);
        return bound;
    }

    private static void RestoreFrameworkCameraViewTextures(Texture texture, string reason)
    {
        if (texture == null || m_SplitCameraViews == null)
        {
            return;
        }

        int restored = 0;
        for (int i = 0; i < m_SplitCameraViews.Length; i++)
        {
            var view = m_SplitCameraViews[i];
            if (view == null)
            {
                continue;
            }

            var rawImage = view.GetComponent<RawImage>();
            if (rawImage == null)
            {
                continue;
            }

            rawImage.texture = texture;
            rawImage.color = Color.white;
            restored++;
        }

        Debug.Log("[新SDK整合] camera view textures restored reason=" + reason +
                  " count=" + restored + " texture=" + texture.GetInstanceID());
    }

    private const string kCameraBridgeTypeName = "YouDooSDK.Native.AndroidTextureBridgeBase";

    private static Texture ReadCurrentTexture(Component bridge)
    {
        var property = FindMember(bridge.GetType(), "CurrentTexture",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) as PropertyInfo;
        return property != null ? property.GetValue(bridge) as Texture : null;
    }

    private static IntPtr ReadRenderEventFunc(Component bridge)
    {
        var field = FindMember(bridge.GetType(), "renderEventFunc",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) as FieldInfo;
        if (field == null || field.FieldType != typeof(IntPtr))
        {
            return IntPtr.Zero;
        }

        return (IntPtr)field.GetValue(bridge);
    }

    private static bool ReadPaused(Component bridge)
    {
        var field = FindMember(bridge.GetType(), "_isPaused",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) as FieldInfo;
        return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(bridge);
    }

    private static bool TryBindRenderTexture(Component bridge, Texture texture)
    {
        if (texture == null)
        {
            return false;
        }

        try
        {
            var method = FindMember(bridge.GetType(), "BindRenderTextureToNative",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) as MethodInfo;
            if (method == null)
            {
                return false;
            }

            method.Invoke(bridge, new object[] { texture });
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[新SDK整合] camera rebind failed: " + exception.Message);
            return false;
        }
    }

    private static bool TryRestartBridge(Component bridge)
    {
        try
        {
            var paused = FindMember(bridge.GetType(), "_isPaused",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) as FieldInfo;
            paused?.SetValue(bridge, false);

            var method = FindMember(bridge.GetType(), "InitializeRuntime",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) as MethodInfo;
            if (method == null)
            {
                return false;
            }

            method.Invoke(bridge, null);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[新SDK整合] camera bridge restart failed: " + exception.Message);
            return false;
        }
    }

    private static MemberInfo FindMember(Type type, string name, BindingFlags flags)
    {
        while (type != null && type != typeof(object))
        {
            var property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
            if (property != null)
            {
                return property;
            }

            var field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
            if (field != null)
            {
                return field;
            }

            var method = type.GetMethod(name, flags | BindingFlags.DeclaredOnly);
            if (method != null)
            {
                return method;
            }

            type = type.BaseType;
        }

        return null;
    }

    private static Type ResolveTypeByName(string fullName)
    {
        var type = Type.GetType(fullName);
        if (type != null)
        {
            return type;
        }

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(fullName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    public override void Render(MatchSessionUiSnapshot snapshot)
    {
        UiSnapshot = snapshot;
        // 每帧快照日志量太大，联调结束先关闭；需要时再打开。
        //if (snapshot != null)
        //{
        //    int readyCount = 0;
        //    for (int i = 0; i < snapshot.Slots.Count; i++)
        //    {
        //        if (snapshot.Slots[i].IsReady)
        //        {
        //            readyCount++;
        //        }
        //    }
        //
        //    GameSdkLog.Info("[新SDK整合] adapter snapshot phase=" + snapshot.SessionPhase +
        //              " slots=" + snapshot.Slots.Count + " ready=" + readyCount);
        //}

        UiSnapshotChanged?.Invoke();
    }

    public override void RenderSession(MatchSessionSnapshot snapshot)
    {
        SessionSnapshot = snapshot;
        // 每帧会话日志量太大，联调结束先关闭；需要时再打开。
        //GameSdkLog.Info("[新SDK整合] adapter session snapshot lifecycle=" +
        //          (snapshot != null ? snapshot.LifecycleState.ToString() : "null") +
        //          " sessionPhase=" + (snapshot != null && snapshot.UiSnapshot != null
        //              ? snapshot.UiSnapshot.SessionPhase.ToString()
        //              : "none"));
        UiSnapshotChanged?.Invoke();
    }
}
