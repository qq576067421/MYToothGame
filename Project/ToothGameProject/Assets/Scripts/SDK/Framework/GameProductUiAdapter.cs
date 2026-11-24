using System;
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
        Instance != null && Instance.IsAttached && Instance.UiSnapshot != null &&
        Instance.SessionSnapshot?.LifecycleState == MatchSessionLifecycleState.Running;

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

    private enum CameraUse
    {
        Idle,
        Prepare,
        Battle,
        LoginHandoff
    }

    private enum CameraPhase
    {
        Idle,
        Suspending,
        Starting,
        WaitingForFirstFrame,
        Active,
        Quiescing,
        Releasing,
        Failed
    }

    private const int m_MaxCameraCommandAttempts = 3;
    private const int m_MaxBattleCameraRecoveries = 2;
    private const float m_CameraCommandRetrySeconds = 0.5f;
    private const float m_PrepareCameraFirstFrameTimeoutSeconds = 3f;
    private const float m_BattleCameraFirstFrameTimeoutSeconds = 7f;
    private const float m_CameraStreamCheckIntervalSeconds = 0.5f;
    private const float m_CameraStreamStallSeconds = 2f;
    private const float m_CameraStreamStableSeconds = 5f;
    private static readonly float[] m_CameraRebindDelays = { 0.2f, 0.8f, 1.8f, 3.2f };
    private static float s_BaselineAspect;
    private static bool s_PrepareCaptureUnavailable;
    private CameraUse m_CameraUse;
    private CameraPhase m_CameraPhase;
    private string m_CameraReason;
    private Action m_CameraHandoffCompleted;
    private bool m_IsCameraRecovery;
    private bool m_ApplyCameraCaptureStage;
    private string m_RequestedCameraCaptureStage;
    private int m_CameraEarliestFrame;
    private float m_CameraEarliestTime;
    private int m_CameraStartAttempts;
    private int m_CameraStopAttempts;
    private int m_BattleCameraRecoverCount;
    private bool m_CameraPauseAccepted;
    private float m_CameraFirstFrameDeadline;
    private CameraImageFrame m_LastCameraStreamFrame;
    private float m_LastCameraStreamAdvanceAt;
    private float m_CameraStreamHealthySince;
    private float m_NextCameraStreamCheckAt;
    private int m_CameraReleaseFrame;
    private float m_CameraReleaseTime;
    private int m_PrepareSessionResumeFrame = -1;
    private int m_CameraRebindStep = -1;
    private float m_NextCameraRebindAt;
    private int m_CameraProbeGeneration;
    private bool m_CameraProbePending;
    private bool m_CameraWatchdogConfirmed;
    private int m_CameraWatchdogRecoverCount;
    private float m_NextCameraHealthProbeAt;
    private bool IsPrepareCameraInUse => m_CameraUse == CameraUse.Prepare;

    private bool m_ApplicationPaused;
    private bool m_ApplicationFocused = true;
    private const float m_PrepareTrackingMaxAgeSeconds = 0.5f;
    private MatchTrackingFrameSnapshot m_LastPrepareTrackingFrame;
    private float m_PrepareTrackingObservedAt = float.NegativeInfinity;
    private bool m_PrepareCameraRecheckRequested;
    private float m_BackgroundEnteredAt = -1f;
    private float m_BackgroundElapsed;

    public static float ForegroundRealtime
    {
        get
        {
            float now = Time.realtimeSinceStartup;
            var adapter = Instance;
            if (adapter == null)
            {
                return now;
            }

            float currentBackground = adapter.m_BackgroundEnteredAt >= 0f ? now - adapter.m_BackgroundEnteredAt : 0f;
            return now - adapter.m_BackgroundElapsed - currentBackground;
        }
    }

    public bool TryResumePrepareSession()
    {
        if (!IsPrepareCameraInUse || !IsApplicationForeground ||
            !YoudooAPI.TryGetSessionSnapshot(out var session) || session.Phase != YoudooGame.MatchFlow.GameSessionPhase.Paused)
        {
            return false;
        }

        bool resumed = YoudooAPI.ResumeGameplay();
        Debug.Log("[准备流程] 回前台恢复 SDK 会话。session=" + session.SessionToken +
                  " resumed=" + resumed + " error=" + YoudooAPI.SessionLastError);
        return resumed;
    }

    public bool IsApplicationForeground => !m_ApplicationPaused && m_ApplicationFocused;
    public int ApplicationStateRevision { get; private set; }

    // ---- 帧内容门控：相机没出画面前，不要把黑块/白块直接给玩家看 ----
    //
    // SDK 的 CameraImageView.Update 只在 GetFrame() 完全失败时把 RawImage 置透明；
    // 一旦 GetFrame() 成功但纹理内容是黑的（切档后 ImageReader 刚重建、
    // 或某档位在原生侧根本起不来），它照样 color=white 贴上去，玩家看到就是纯黑块。
    //
    // 所以在外层用 CanvasGroup 兜一层：内容没验证通过就整块透明，
    // 露出下面已经设计好的底图；验证通过再淡入。全程不改 SDK 组件、不动材质。
    private static bool s_FrameContentVisible = !GameSdkPerfOptions.EnableCameraFrameGate;
    private static float s_NextFrameProbeTime;
    private static float s_FrameGateGraceUntil;
    private static int s_FrameGateBlackStrikes;
    private const int FrameGateBlackStrikesBeforeRecover = 6;

    // 供战斗 HUD 判断“这张纹理现在能不能裁到头像上”。
    public static bool CameraFrameContentVisible => s_FrameContentVisible;

    protected override void OnEnable()
    {
        base.OnEnable();
        Instance = this;
        if (Application.platform == RuntimePlatform.Android)
        {
            UpdateApplicationState(m_ApplicationPaused, Application.isFocused);
        }
        Debug.Log("[相机生命周期] adapter enabled attached=" + IsAttached);
        if (GameSdkPerfOptions.EnableCameraFrameGate)
        {
            MarkCaptureSwitching("adapter-enable");
        }
    }

    protected override void OnDisable()
    {
        var completeHandoff = m_CameraHandoffCompleted;
        ResetCameraOperation(CameraUse.Idle, "adapter-disable");
        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDisable();
        completeHandoff?.Invoke();
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

    private void OnApplicationPause(bool paused)
    {
        UpdateApplicationState(paused, m_ApplicationFocused);
    }

    private void OnApplicationFocus(bool focused)
    {
        UpdateApplicationState(m_ApplicationPaused, focused);
    }

    private void UpdateApplicationState(bool paused, bool focused)
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        bool wasForeground = IsApplicationForeground;
        m_ApplicationPaused = paused;
        m_ApplicationFocused = focused;
        if (wasForeground == IsApplicationForeground)
        {
            return;
        }

        ApplicationStateRevision++;
        m_PrepareTrackingObservedAt = float.NegativeInfinity;
        float now = Time.realtimeSinceStartup;
        if (!IsApplicationForeground)
        {
            m_BackgroundEnteredAt = now;
        }
        else if (m_BackgroundEnteredAt >= 0f)
        {
            m_BackgroundElapsed += now - m_BackgroundEnteredAt;
            m_BackgroundEnteredAt = -1f;
        }

        InvalidateCameraProbe();
        m_PrepareSessionResumeFrame = -1;
        if (IsApplicationForeground)
        {
            if (m_CameraUse == CameraUse.Prepare)
            {
                m_PrepareSessionResumeFrame = Time.frameCount + 1;
                m_PrepareCameraRecheckRequested = true;
                m_CameraWatchdogConfirmed = false;
                m_CameraWatchdogRecoverCount = 0;
                m_NextCameraHealthProbeAt = Time.unscaledTime + 3f;
            }
            if (m_CameraUse == CameraUse.Idle)
            {
                QueueCameraSuspension();
            }
            else if ((m_CameraUse == CameraUse.Prepare || m_CameraUse == CameraUse.Battle) &&
                     (m_CameraPhase == CameraPhase.Active || m_CameraPhase == CameraPhase.Failed))
            {
                m_BattleCameraRecoverCount = 0;
                QueueCameraStart(1);
            }
        }

        UiSnapshotChanged?.Invoke();
    }

    public bool IsPrepareTrackingReady(MatchSessionSnapshot snapshot)
    {
        if (!IsAttached || !IsApplicationForeground || snapshot == null ||
            snapshot.LifecycleState != MatchSessionLifecycleState.Running)
        {
            return false;
        }

        if (Application.isEditor)
        {
            return true;
        }

        var frame = snapshot.CurrentTrackingFrame;
        return frame != null && m_LastPrepareTrackingFrame != null &&
            frame.SessionToken == m_LastPrepareTrackingFrame.SessionToken &&
            frame.RoundId == m_LastPrepareTrackingFrame.RoundId &&
            frame.Sequence == m_LastPrepareTrackingFrame.Sequence &&
            Time.realtimeSinceStartup - m_PrepareTrackingObservedAt < m_PrepareTrackingMaxAgeSeconds &&
            (!m_PrepareCameraRecheckRequested || YoudooAPI.CameraImage.Status.State == CameraImageState.Running);
    }

    private void ObservePrepareTrackingFrame(MatchSessionSnapshot snapshot)
    {
        var frame = snapshot?.CurrentTrackingFrame;
        if (frame == null || frame.SessionToken != snapshot.SessionToken || frame.RoundId != snapshot.RoundSnapshot?.RoundId)
        {
            m_PrepareTrackingObservedAt = float.NegativeInfinity;
            return;
        }

        var previous = m_LastPrepareTrackingFrame;
        if (previous == null || previous.SessionToken != frame.SessionToken || previous.RoundId != frame.RoundId)
        {
            m_LastPrepareTrackingFrame = frame;
            m_PrepareTrackingObservedAt = float.NegativeInfinity;
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (frame.Sequence > previous.Sequence)
        {
            m_LastPrepareTrackingFrame = frame;
            m_PrepareTrackingObservedAt = now;
        }
        else if (!float.IsNegativeInfinity(m_PrepareTrackingObservedAt) &&
                 now - m_PrepareTrackingObservedAt >= m_PrepareTrackingMaxAgeSeconds)
        {
            m_PrepareTrackingObservedAt = float.NegativeInfinity;
            Debug.LogWarning("[准备确认] SDK 骨骼帧停止更新。session=" + frame.SessionToken +
                             " round=" + frame.RoundId + " sequence=" + frame.Sequence);
        }
    }

    private void Update()
    {
        if (!IsApplicationForeground)
        {
            return;
        }

        if (m_PrepareSessionResumeFrame >= 0 && Time.frameCount >= m_PrepareSessionResumeFrame)
        {
            m_PrepareSessionResumeFrame = -1;
            TryResumePrepareSession();
        }

        switch (m_CameraPhase)
        {
            case CameraPhase.Suspending:
                if (IsCameraDelayComplete())
                {
                    TrySuspendIdleCamera();
                }
                break;
            case CameraPhase.Starting:
                if (IsCameraDelayComplete() && IsCameraReleaseComplete())
                {
                    TryStartCamera();
                }
                break;
            case CameraPhase.WaitingForFirstFrame:
                if (!TryActivateCameraFromFrame())
                {
                    if (ForegroundRealtime >= m_CameraFirstFrameDeadline)
                    {
                        BeginCameraRecovery("first-frame-timeout");
                    }
                    else
                    {
                        UpdateCameraRebind();
                    }
                }
                break;
            case CameraPhase.Quiescing:
                if (IsCameraDelayComplete())
                {
                    TryFinishCameraStop();
                }
                break;
            case CameraPhase.Releasing:
                if (IsCameraReleaseComplete())
                {
                    if (m_CameraUse == CameraUse.LoginHandoff)
                    {
                        var completed = m_CameraHandoffCompleted;
                        ResetCameraOperation(CameraUse.Idle, m_CameraReason);
                        completed?.Invoke();
                        break;
                    }

                    if (IsPrepareCameraInUse && m_IsCameraRecovery)
                    {
                        ApplyCameraRecoveryCaptureStage();
                    }
                    QueueCameraStart(1);
                }
                break;
            case CameraPhase.Active:
                UpdateCameraPresentation();
                break;
            case CameraPhase.Failed:
                TryActivateCameraFromFrame();
                break;
        }
    }

    private void TryStartCamera()
    {
        if (m_ApplyCameraCaptureStage)
        {
            ApplyCaptureStage(m_RequestedCameraCaptureStage, m_CameraReason);
            m_ApplyCameraCaptureStage = false;
        }

        if (!ResumeCameraImage(m_CameraReason))
        {
            if (++m_CameraStartAttempts < m_MaxCameraCommandAttempts)
            {
                m_CameraEarliestTime = Time.unscaledTime + m_CameraCommandRetrySeconds;
                return;
            }

            m_CameraStartAttempts = 0;
            BeginCameraRecovery("start-command-failed");
            return;
        }

        m_CameraStartAttempts = 0;
        m_CameraPhase = CameraPhase.WaitingForFirstFrame;
        if (IsPrepareCameraInUse)
        {
            EnableSplitCameraLayout();
        }

        if (TryActivateCameraFromFrame())
        {
            return;
        }

        m_CameraFirstFrameDeadline = ForegroundRealtime + (IsPrepareCameraInUse
            ? m_PrepareCameraFirstFrameTimeoutSeconds
            : m_BattleCameraFirstFrameTimeoutSeconds);
        m_CameraRebindStep = 0;
        m_NextCameraRebindAt = Time.unscaledTime + m_CameraRebindDelays[0];
    }

    private bool TryActivateCameraFromFrame()
    {
        if (!TryReadAdvancingCameraFrame())
        {
            return false;
        }

        float now = ForegroundRealtime;
        m_LastCameraStreamAdvanceAt = now;
        m_CameraStreamHealthySince = now;
        m_NextCameraStreamCheckAt = now + m_CameraStreamCheckIntervalSeconds;
        m_CameraPhase = CameraPhase.Active;
        m_CameraRebindStep = -1;
        m_IsCameraRecovery = false;
        m_NextCameraHealthProbeAt = Time.unscaledTime + 3f;
        Debug.Log("[相机生命周期] 首帧已就绪。use=" + m_CameraUse + " reason=" + m_CameraReason +
                  " frame=" + m_LastCameraStreamFrame.Width + "x" + m_LastCameraStreamFrame.Height);
        return true;
    }

    private bool TryReadAdvancingCameraFrame()
    {
        if (YoudooAPI.CameraImage.Status.State != CameraImageState.Running)
        {
            return false;
        }

        var result = YoudooAPI.CameraImage.GetFrame();
        if (!result.Success || result.Value.Texture == null)
        {
            return false;
        }

        var frame = result.Value;
        var previous = m_LastCameraStreamFrame;
        if (previous.Texture == frame.Texture && previous.FrameId == frame.FrameId &&
            previous.TimestampSec == frame.TimestampSec && previous.SourceId == frame.SourceId &&
            previous.Provenance.SourceSessionToken == frame.Provenance.SourceSessionToken &&
            previous.Provenance.SourceConnectionToken == frame.Provenance.SourceConnectionToken)
        {
            return false;
        }

        m_LastCameraStreamFrame = frame;
        return true;
    }

    private void UpdateActiveCameraStream()
    {
        float now = ForegroundRealtime;
        if (now < m_NextCameraStreamCheckAt)
        {
            return;
        }
        m_NextCameraStreamCheckAt = now + m_CameraStreamCheckIntervalSeconds;

        if (TryReadAdvancingCameraFrame())
        {
            m_LastCameraStreamAdvanceAt = now;
            if (m_CameraStreamHealthySince < 0f)
            {
                m_CameraStreamHealthySince = now;
            }
            else if (now - m_CameraStreamHealthySince >= m_CameraStreamStableSeconds)
            {
                m_BattleCameraRecoverCount = 0;
                m_CameraWatchdogRecoverCount = 0;
            }
            return;
        }

        m_CameraStreamHealthySince = -1f;
        if (now - m_LastCameraStreamAdvanceAt >= m_CameraStreamStallSeconds)
        {
            BeginCameraRecovery("active-stream-stalled", streamUnavailable: true);
        }
    }

    private void QueueCameraSuspension()
    {
        m_CameraPhase = CameraPhase.Suspending;
        m_CameraStopAttempts = 0;
        m_CameraEarliestFrame = Time.frameCount;
        m_CameraEarliestTime = Time.unscaledTime;
    }

    private void TrySuspendIdleCamera()
    {
        if (PauseCameraImage(m_CameraReason ?? "idle"))
        {
            m_CameraPhase = CameraPhase.Idle;
            return;
        }

        if (++m_CameraStopAttempts < m_MaxCameraCommandAttempts)
        {
            m_CameraEarliestTime = Time.unscaledTime + m_CameraCommandRetrySeconds;
            return;
        }

        m_CameraPhase = CameraPhase.Idle;
        Debug.LogWarning("[相机生命周期] 停用相机失败。reason=" + m_CameraReason +
                         " state=" + YoudooAPI.CameraImage.Status.State + " error=" + YoudooAPI.CameraImage.Status.Error);
    }

    private void ResetCameraOperation(CameraUse use, string reason)
    {
        InvalidateCameraProbe();
        m_CameraUse = use;
        m_CameraPhase = CameraPhase.Idle;
        m_CameraReason = reason;
        m_CameraHandoffCompleted = null;
        m_PrepareSessionResumeFrame = -1;
        m_CameraRebindStep = -1;
        m_CameraStartAttempts = 0;
        m_CameraStopAttempts = 0;
        m_BattleCameraRecoverCount = 0;
        m_CameraPauseAccepted = false;
        m_LastCameraStreamFrame = default;
        m_IsCameraRecovery = false;
        m_ApplyCameraCaptureStage = false;
        m_RequestedCameraCaptureStage = null;
        Debug.Log("[相机生命周期] use=" + use + " reason=" + reason + " sdk=" + YoudooAPI.CameraImage.Status.State);
    }

    private void QueueCameraStart(int frames = 0)
    {
        m_CameraPhase = CameraPhase.Starting;
        m_CameraEarliestFrame = Time.frameCount + frames;
        m_CameraEarliestTime = Time.unscaledTime;
    }

    private void BeginCameraStop(float minimumSeconds)
    {
        InvalidateCameraProbe();
        m_CameraRebindStep = -1;
        m_CameraStopAttempts = 0;
        if (YoudooAPI.CameraImage.Status.State == CameraImageState.Stopped)
        {
            m_CameraPhase = CameraPhase.Releasing;
            return;
        }

        m_CameraPauseAccepted = PauseCameraImage(m_CameraReason + "+quiesce");
        m_CameraEarliestFrame = Time.frameCount + 2;
        m_CameraEarliestTime = Time.unscaledTime + minimumSeconds;
        m_CameraPhase = CameraPhase.Quiescing;
    }

    private void TryFinishCameraStop()
    {
        var state = YoudooAPI.CameraImage.Status.State;
        if (state == CameraImageState.Stopped ||
            (m_CameraPauseAccepted && state == CameraImageState.Paused && StopCameraImage(m_CameraReason)))
        {
            m_CameraReleaseFrame = Time.frameCount + 3;
            m_CameraReleaseTime = Time.unscaledTime + 0.05f;
            m_CameraPhase = CameraPhase.Releasing;
            return;
        }

        if (++m_CameraStopAttempts < m_MaxCameraCommandAttempts)
        {
            m_CameraPauseAccepted = PauseCameraImage(m_CameraReason + "+retry-stop");
            m_CameraEarliestFrame = Time.frameCount + 2;
            m_CameraEarliestTime = Time.unscaledTime + GameSdkPerfOptions.CameraStopQuiesceSeconds;
            return;
        }

        Debug.LogWarning("[相机生命周期] 无法确认停流。reason=" + m_CameraReason + " state=" + state);
        if (m_CameraUse == CameraUse.LoginHandoff)
        {
            var completed = m_CameraHandoffCompleted;
            ResetCameraOperation(CameraUse.Idle, m_CameraReason);
            QueueCameraSuspension();
            completed?.Invoke();
            return;
        }

        m_IsCameraRecovery = false;
        m_ApplyCameraCaptureStage = false;
        QueueCameraStart();
    }

    private bool IsCameraDelayComplete() =>
        Time.frameCount >= m_CameraEarliestFrame && Time.unscaledTime >= m_CameraEarliestTime;

    private bool IsCameraReleaseComplete() =>
        Time.frameCount >= m_CameraReleaseFrame && Time.unscaledTime >= m_CameraReleaseTime;

    private void InvalidateCameraProbe()
    {
        m_CameraProbeGeneration++;
        m_CameraProbePending = false;
    }

    private void UpdateCameraRebind()
    {
        float now = Time.unscaledTime;
        if (m_CameraRebindStep < 0 || now < m_NextCameraRebindAt)
        {
            return;
        }

        RebindCameraTextureToNative(m_CameraReason + "+rebind");
        if (++m_CameraRebindStep >= m_CameraRebindDelays.Length)
        {
            m_CameraRebindStep = -1;
        }
        else
        {
            m_NextCameraRebindAt = now + m_CameraRebindDelays[m_CameraRebindStep];
        }
    }

    private void UpdateCameraPresentation()
    {
        UpdateActiveCameraStream();
        if (m_CameraPhase != CameraPhase.Active)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (GameSdkPerfOptions.EnableCameraHealthWatchdog && IsPrepareCameraInUse &&
            m_SplitCameraViews != null && !m_CameraWatchdogConfirmed && !m_CameraProbePending &&
            now >= m_NextCameraHealthProbeAt)
        {
            m_NextCameraHealthProbeAt = now + 1f;
            ProbeCameraFrame(watchdog: true);
        }

        if (m_CameraPhase == CameraPhase.Active && GameSdkPerfOptions.EnableCameraFrameGate)
        {
            if (YoudooAPI.CameraImage.Status.State == CameraImageState.Stopped)
            {
                s_FrameContentVisible = false;
            }
            else if (!m_CameraProbePending && now >= s_FrameGateGraceUntil && now >= s_NextFrameProbeTime)
            {
                s_NextFrameProbeTime = now + (s_FrameContentVisible ? 1.5f : 0.4f);
                ProbeCameraFrame(watchdog: false);
            }

            ApplySplitViewGate(s_FrameContentVisible ? 1f : 0f, false);
        }
    }

    private void ProbeCameraFrame(bool watchdog)
    {
        var result = YoudooAPI.CameraImage.GetFrame();
        if (!result.Success || result.Value.Texture == null)
        {
            if (watchdog)
            {
                Debug.LogWarning("[相机生命周期] 准备页无相机画面。state=" + YoudooAPI.CameraImage.Status.State +
                                 " error=" + result.Error);
                BeginCameraRecovery("prepare-no-frame");
            }
            else
            {
                SetFrameContentVisible(false, "no-frame", -1);
            }
            return;
        }

        var frame = result.Value;
        var texture = frame.Texture as RenderTexture;
        if (texture == null || texture.width <= 0 || texture.height <= 0 || !SystemInfo.supportsAsyncGPUReadback)
        {
            CompleteCameraProbe(watchdog, frame.Width, frame.Height, -1);
            return;
        }

        int generation = m_CameraProbeGeneration;
        int width = frame.Width;
        int height = frame.Height;
        var probe = RenderTexture.GetTemporary(16, 16, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        m_CameraProbePending = true;
        try
        {
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(texture, probe);
            }
            finally
            {
                RenderTexture.active = previous;
            }
            AsyncGPUReadback.Request(probe, 0, TextureFormat.RGBA32, request =>
            {
                int maxRgb = -1;
                try
                {
                    if (generation == m_CameraProbeGeneration && !request.hasError)
                    {
                        var data = request.GetData<byte>();
                        maxRgb = 0;
                        for (int index = 0; index + 3 < data.Length; index += 4)
                        {
                            maxRgb = Mathf.Max(maxRgb, Mathf.Max(data[index], Mathf.Max(data[index + 1], data[index + 2])));
                        }
                    }
                }
                catch (Exception exception)
                {
                    maxRgb = -1;
                    Debug.LogWarning("[相机生命周期] 相机画面回读失败。error=" + exception.Message);
                }
                finally
                {
                    RenderTexture.ReleaseTemporary(probe);
                }

                if (generation == m_CameraProbeGeneration)
                {
                    m_CameraProbePending = false;
                    CompleteCameraProbe(watchdog, width, height, maxRgb);
                }
            });
        }
        catch (Exception exception)
        {
            RenderTexture.ReleaseTemporary(probe);
            m_CameraProbePending = false;
            Debug.LogWarning("[相机生命周期] 相机画面探测失败。error=" + exception.Message);
            CompleteCameraProbe(watchdog, width, height, -1);
        }
    }

    private void CompleteCameraProbe(bool watchdog, int width, int height, int maxRgb)
    {
        if (m_CameraPhase != CameraPhase.Active || !IsApplicationForeground)
        {
            return;
        }

        if (!watchdog)
        {
            SetFrameContentVisible(maxRgb < 0 || maxRgb > 6, "readback", maxRgb);
            return;
        }

        bool aspectAdjusted = IsAspectAdjusted(width, height);
        Debug.Log("[相机生命周期] 准备页画面探测。frame=" + width + "x" + height +
                  " aspectChanged=" + aspectAdjusted + " maxRgb=" + maxRgb);
        if (maxRgb >= 0 && maxRgb <= 4)
        {
            BeginCameraRecovery("prepare-black-frame");
            return;
        }

        if (aspectAdjusted)
        {
            s_BaselineAspect = 0f;
            if (GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch)
            {
                MarkCurrentCaptureUnavailable();
                m_ApplyCameraCaptureStage = true;
                m_RequestedCameraCaptureStage = null;
                m_CameraReason = "prepare-aspect-changed";
                BeginCameraStop(GameSdkPerfOptions.CameraStopQuiesceSeconds);
                return;
            }
        }

        m_CameraWatchdogConfirmed = true;
        m_PrepareCameraRecheckRequested = false;
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
        }
    }

    private void BeginCameraRecovery(string reason, bool streamUnavailable = false)
    {
        if (m_CameraUse != CameraUse.Prepare && m_CameraUse != CameraUse.Battle)
        {
            return;
        }

        int limit = IsPrepareCameraInUse ? GameSdkPerfOptions.MaxCaptureStageAttempts + 1 : m_MaxBattleCameraRecoveries;
        int attempts = IsPrepareCameraInUse ? m_CameraWatchdogRecoverCount : m_BattleCameraRecoverCount;
        if (attempts >= limit)
        {
            m_CameraWatchdogConfirmed = true;
            m_CameraRebindStep = -1;
            if (streamUnavailable || m_CameraPhase != CameraPhase.Active)
            {
                m_CameraPhase = CameraPhase.Failed;
            }
            Debug.LogWarning("[相机生命周期] 相机恢复已达上限。use=" + m_CameraUse + " reason=" + reason +
                             " attempts=" + attempts + " sdk=" + YoudooAPI.CameraImage.Status.State);
            return;
        }

        if (IsPrepareCameraInUse)
        {
            m_CameraWatchdogConfirmed = false;
            m_CameraWatchdogRecoverCount++;
        }
        else
        {
            m_BattleCameraRecoverCount++;
        }

        m_CameraReason = reason;
        m_IsCameraRecovery = true;
        Debug.LogWarning("[相机生命周期] 恢复相机。use=" + m_CameraUse + " reason=" + reason + " attempt=" + (attempts + 1));
        BeginCameraStop(GameSdkPerfOptions.CameraStopQuiesceSeconds);
    }

    private void ApplyCameraRecoveryCaptureStage()
    {
        if (!GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch)
        {
            return;
        }

        string stage = GameSdkCameraCaptureFallback.CurrentStage;
        int oldWidth = GameSdkCameraCaptureFallback.CurrentWidth;
        int oldHeight = GameSdkCameraCaptureFallback.CurrentHeight;
        GameSdkCameraCaptureFallback.MarkKnownBad(oldWidth, oldHeight);
        if (!string.IsNullOrEmpty(stage) &&
            m_CameraWatchdogRecoverCount < GameSdkPerfOptions.MaxCaptureStageAttempts &&
            GameSdkCameraCaptureFallback.TryApplyStage(stage, m_CameraReason + "+near") &&
            (GameSdkCameraCaptureFallback.CurrentWidth != oldWidth || GameSdkCameraCaptureFallback.CurrentHeight != oldHeight))
        {
            return;
        }

        MarkCurrentCaptureUnavailable();
        GameSdkCameraCaptureFallback.TryRestoreDeviceDefault(m_CameraReason + "+default");
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
            Instance?.IsPrepareCameraInUse == true)
        {
            s_FrameGateBlackStrikes = 0;
            Instance.BeginCameraRecovery("frame-gate-black");
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

    // SDK 暂停保留 RenderTexture，并清理其原生绑定。仅在需要停用相机时调用。
    private static bool PauseCameraImage(string reason)
    {
        var status = YoudooAPI.CameraImage.Status;
        if (status.State == CameraImageState.Stopped)
        {
            // 已经是停止状态：再 Pause 会把状态改成 Paused，但内部桥已经没了，
            // 之后 Resume 只会得到一个没有桥的“假暂停”状态。
            Debug.Log("[新SDK整合] camera pause skipped reason=" + reason + ": already stopped.");
            return true;
        }

        // SDK 的 Paused 也可能来自系统失焦。Pause 幂等，必须由项目明确锁定停用。
        var result = YoudooAPI.CameraImage.Pause();
        bool paused = result.Success && result.Value;
        Debug.Log("[新SDK整合] camera pause reason=" + reason +
                  " paused=" + (paused ? "1" : "0") +
                  " state=" + status.State + "/" + status.GraphicsApi);
        return paused;
    }

    private static bool ResumeCameraImage(string reason)
    {
        var previousStatus = YoudooAPI.CameraImage.Status;
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

        // SDK 通过 Resume 恢复暂停的相机，通过 Start 启动已停止的相机。
        var result = previousStatus.State == CameraImageState.Paused
            ? YoudooAPI.CameraImage.Resume()
            : YoudooAPI.CameraImage.Start();
        bool resumed = result.Success && result.Value;
        var currentStatus = YoudooAPI.CameraImage.Status;
        Debug.Log("[新SDK整合] camera resume reason=" + reason +
                  " resumed=" + (resumed ? "1" : "0") +
                  " state=" + previousStatus.State + "->" + currentStatus.State + "/" + currentStatus.GraphicsApi +
                  " error=" + currentStatus.Error + " msg=" + currentStatus.Message);
        return resumed;
    }

    private static bool StopCameraImage(string reason)
    {
        var status = YoudooAPI.CameraImage.Status;
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
        var result = YoudooAPI.CameraImage.Stop();
        bool stopped = result.Success && result.Value;
        Debug.Log("[新SDK整合] camera stop end reason=" + reason +
                  " stopped=" + (stopped ? "1" : "0") +
                  " previous=" + status.State + "/" + status.GraphicsApi);
        return stopped;
    }

    public static void EnterBattleCamera(string reason)
    {
        var adapter = Instance;
        if (adapter != null &&
            (adapter.m_CameraUse != CameraUse.Battle || adapter.m_CameraPhase == CameraPhase.Failed))
        {
            adapter.ResetCameraOperation(CameraUse.Battle, reason);
            ApplyCameraTargetFps(GameSdkPerfOptions.BattleCameraTargetFps, reason);
            adapter.QueueCameraStart();
        }
    }

    public static void ExitBattleCamera(string reason)
    {
        if (Instance != null)
        {
            Debug.Log("[新SDK整合] 战斗 HUD 解绑相机画面 reason=" + reason +
                      " state=" + YoudooAPI.CameraImage.Status.State);
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
        var adapter = Instance;
        if (adapter == null)
        {
            return;
        }

        adapter.ResetCameraOperation(CameraUse.Prepare, reason);
        adapter.ResetPrepareTracking();
        adapter.m_CameraWatchdogConfirmed = false;
        adapter.m_CameraWatchdogRecoverCount = 0;
        ApplyCameraTargetFps(GameSdkPerfOptions.CameraTargetFps, reason);
        EnsureFrameworkCameraViews();

        bool stopped = YoudooAPI.CameraImage.Status.State == CameraImageState.Stopped;
        if (GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch)
        {
            adapter.m_ApplyCameraCaptureStage = stopped
                ? GameSdkCameraCaptureFallback.NeedsSwitch(GameSdkCameraCaptureFallback.StagePrepare)
                : s_PrepareCaptureUnavailable
                    ? GameSdkCameraCaptureFallback.NeedsSwitch(null)
                    : NeedsPrepareCaptureSwitch();
            adapter.m_RequestedCameraCaptureStage = s_PrepareCaptureUnavailable
                ? null : GameSdkCameraCaptureFallback.StagePrepare;
        }

        if (adapter.m_ApplyCameraCaptureStage && !stopped)
        {
            adapter.BeginCameraStop(GameSdkPerfOptions.CameraStopQuiesceSeconds);
        }
        else
        {
            adapter.QueueCameraStart(stopped ? 2 : 0);
        }
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
        var result = YoudooAPI.CameraImage.SetTargetFps(fps);
        if (result.Success && result.Value)
        {
            s_AppliedCameraTargetFps = fps;
        }
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
    }

    // 准备界面关闭 / 切场景时立刻隐藏框架相机视图：
    // 相机暂停后帧会被清空，直接留着会看到一块空白（白板）；
    // 而且 RawImage 没有纹理时默认渲染成白色块，隐藏时必须把颜色清成透明。
    private static void HideFrameworkCameraViews()
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

        Instance?.ResetPrepareTracking();
    }

    private void ResetPrepareTracking()
    {
        m_PrepareCameraRecheckRequested = false;
        m_LastPrepareTrackingFrame = null;
        m_PrepareTrackingObservedAt = float.NegativeInfinity;
    }

    public static void SuspendCamera(string reason)
    {
        var adapter = Instance;
        adapter?.ResetCameraOperation(CameraUse.Idle, reason);
        HideFrameworkCameraViews();
        RestoreCameraViewsToLayout();
        if (adapter == null)
        {
            PauseCameraImage(reason);
            return;
        }

        adapter.QueueCameraSuspension();
        adapter.TrySuspendIdleCamera();
    }

    public static void ClosePrepareCameraWindow(string reason, GameObject windowRoot, Action closeWindowAction)
    {
        SuspendCamera(reason);
        Debug.Log("[相机生命周期] 准备页相机已交还。window=" + (windowRoot != null ? windowRoot.name : "null"));
        closeWindowAction?.Invoke();
    }

    public static void HandoffCameraBeforeScene(string reason, Action onCompleted, bool stopCameraAfterPause = true)
    {
        HideFrameworkCameraViews();
        RestoreCameraViewsToLayout();
        var adapter = Instance;
        if (!stopCameraAfterPause)
        {
            EnterBattleCamera(reason);
            onCompleted?.Invoke();
            return;
        }

        ApplyCameraTargetFps(GameSdkPerfOptions.CameraTargetFps, reason);
        if (adapter == null || !adapter.isActiveAndEnabled)
        {
            PauseCameraImage(reason);
            onCompleted?.Invoke();
            return;
        }

        adapter.ResetCameraOperation(CameraUse.LoginHandoff, reason);
        adapter.m_CameraHandoffCompleted = onCompleted;
        adapter.BeginCameraStop(0.3f);
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
    }

    public override void RenderSession(MatchSessionSnapshot snapshot)
    {
        SessionSnapshot = snapshot;
        UiSnapshot = snapshot?.UiSnapshot;
        if (IsPrepareCameraInUse && !Application.isEditor && IsApplicationForeground)
        {
            ObservePrepareTrackingFrame(snapshot);
        }
        UiSnapshotChanged?.Invoke();
    }
}
