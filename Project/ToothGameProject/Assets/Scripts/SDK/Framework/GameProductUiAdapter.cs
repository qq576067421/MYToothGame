using System;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;
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

    protected override void OnEnable()
    {
        base.OnEnable();
        Instance = this;
        GameSdkLog.Info("[新SDK整合] adapter enable bound=" + IsBound + " attached=" + IsAttached);
    }

    protected override void OnDisable()
    {
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
        GameSdkLog.Info("[新SDK整合] adapter bound, sessionHost=" + (SessionHost != null) + " attached=" + IsAttached);
    }

    protected override void OnUnbound()
    {
        if (SessionHost != null)
        {
            SessionHost.AvatarSelectionChanged -= HandleAvatarSelectionChanged;
        }
        GameSdkLog.Info("[新SDK整合] adapter unbound, sessionHost=" + (SessionHost != null));
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
                viewObject.SetActive(true);
                changed = true;
            }
            else if (views[i].GetComponent<RawImage>() == null)
            {
                views[i].gameObject.AddComponent<RawImage>();
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

        GameSdkLog.Info("[新SDK整合] camera views ensured changed=" + (changed ? "1" : "0") +
                  " count=" + views.Length + " layout=" + m_SplitCameraLayout.gameObject.name +
                  " layoutEnabled=" + m_SplitCameraLayout.enabled + " summary=" + BuildCameraViewSummary(views));
        return true;
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
        GameSdkLog.Info("[新SDK整合] camera views restored to framework root, restored=" + restored);
    }

    public bool StartGameplay()
    {
        bool started = YoudooAPI.StartGameplay();
        GameSdkLog.Info("[新SDK整合] adapter StartGameplay bound=" + IsBound + " result=" + started);
        return started;
    }

    // 暂停时保留 CameraImage 的 RenderTexture 和原生绑定；准备界面与战斗 HUD 共用该纹理生产者。
    // 场景交接过程中不销毁 RT 或派发原生 cleanup，避免原生仍持有纹理指针时释放资源。
    public static bool PauseCameraImage(string reason)
    {
        var status = CameraImage.Status;
        if (status.State == CameraImageState.Paused)
        {
            GameSdkLog.Info("[新SDK整合] camera pause skipped reason=" + reason + ": already paused.");
            return true;
        }

        if (status.State == CameraImageState.Stopped)
        {
            // 已经是停止状态：再 Pause 会把状态改成 Paused，但内部桥已经没了，
            // 之后 Resume 只会得到一个没有桥的“假暂停”状态。
            GameSdkLog.Info("[新SDK整合] camera pause skipped reason=" + reason + ": already stopped.");
            return true;
        }

        bool paused = CameraImage.Pause().Success;
        GameSdkLog.Info("[新SDK整合] camera pause reason=" + reason +
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
            GameSdkLog.Info("[新SDK整合] camera resume skipped reason=" + reason +
                      ": startup is already pending.");
            return true;
        }

        // 返回选人数只暂停相机，必须用 Resume 保留 Android provider 的原生纹理绑定；
        // 只有已彻底停止（例如切场景交接）才用 Start 重建 provider。
        bool resumed = previousStatus.State == CameraImageState.Paused
            ? CameraImage.Resume().Success
            : CameraImage.Start().Success;
        var currentStatus = CameraImage.Status;
        GameSdkLog.Info("[新SDK整合] camera resume reason=" + reason +
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
            GameSdkLog.Info("[新SDK整合] camera stop skipped reason=" + reason + ": already stopped.");
            return true;
        }

        GameSdkLog.Info("[新SDK整合] camera stop begin reason=" + reason +
                  " state=" + status.State + "/" + status.GraphicsApi);
        bool stopped = CameraImage.Stop().Success;
        GameSdkLog.Info("[新SDK整合] camera stop end reason=" + reason +
                  " stopped=" + (stopped ? "1" : "0") +
                  " previous=" + status.State + "/" + status.GraphicsApi);
        return stopped;
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
        if (!EnsureFrameworkCameraViews())
        {
            return;
        }

        if (CameraImage.Status.State == CameraImageState.Stopped)
        {
            // 相机是停止状态：先等上一次销毁的桥组件真正消失（Destroy 在帧末生效），
            // 先启动 provider，再激活 CameraImageView，避免 OnEnable 抢先调用 Start。
            StartCameraRestartRoutine(reason);
            return;
        }

        // 框架视图关闭 StartOnEnable，由此处独占相机生命周期；先恢复 provider 再恢复布局。
        ResumeCameraImage(reason);
        EnableSplitCameraLayout();
        StartCameraRebindRoutine(reason);
    }

    private static void EnableSplitCameraLayout()
    {
        if (m_SplitCameraLayout != null && !m_SplitCameraLayout.enabled)
        {
            m_SplitCameraLayout.enabled = true;
        }
    }

    private static Coroutine s_CameraRestartCoroutine;

    private static void StartCameraRestartRoutine(string reason)
    {
        StopCameraRebindRoutine();
        var host = Instance;
        if (host == null || !host.isActiveAndEnabled)
        {
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

        s_CameraRestartCoroutine = host.StartCoroutine(CameraRestartRoutine(reason));
    }

    private static IEnumerator CameraRestartRoutine(string reason)
    {
        yield return null;
        yield return null;
        s_CameraRestartCoroutine = null;
        ResumeCameraImage(reason);
        EnableSplitCameraLayout();
        StartCameraRebindRoutine(reason);
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
            GameSdkLog.Info("[新SDK整合] prepare camera returned paused reason=" + reason +
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
        GameSdkLog.Info("[新SDK整合] prepare camera returned paused reason=" + reason +
                  " state=" + CameraImage.Status.State +
                  " window=" + (windowRoot != null ? windowRoot.name : "null"));
        closeWindowAction?.Invoke();
    }

    // 切场景前先暂停并收回视图；只有退出会话时才停止 CameraImage，进战斗保留纹理生产者供 HUD 复用。
    public static void HandoffCameraBeforeScene(string reason, Action onCompleted, bool stopCameraAfterPause = true)
    {
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
        else
        {
            yield return new WaitForEndOfFrame();
        }

        RestoreCameraViewsToLayout();
        onCompleted?.Invoke();
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
            GameSdkLog.Info("[新SDK整合] camera rebind reason=" + reason +
                      " target=" + target.gameObject.name + " restart=" + (restarted ? "1" : "0") +
                      " bridges=" + summary);
            return restarted;
        }

        bool bound = TryBindRenderTexture(target, targetTexture);
        GameSdkLog.Info("[新SDK整合] camera rebind reason=" + reason +
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

        GameSdkLog.Info("[新SDK整合] camera view textures restored reason=" + reason +
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
