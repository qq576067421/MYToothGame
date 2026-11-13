using UnityEngine;
using UnityEngine.UI;
using YoudooGame.CameraImage;
using YoudooGame.Core;
using YoudooGame.Framework;
using YoudooGame.MatchFlow.Runtime;
using YoudooGame.SDK.Adapter;

public sealed class GameFrameworkBootstrap : MonoBehaviour
{
    [SerializeField, Range(1, 4)] private int m_PlayerCount = 4;

    public static FrameworkCompositionRoot CompositionRoot { get; private set; }

#if UNITY_EDITOR
    private static GameEditorSyntheticMatchSessionRuntime m_EditorSyntheticRuntime;
#endif

    // 界面选择的人数是会话匹配人数的唯一来源：会话按 N 人匹配时，视觉槽位才与座位一一对应。
    public static bool TrySetSessionPlayerCount(int playerCount)
    {
        if (CompositionRoot == null)
        {
            Debug.LogWarning("[新SDK整合] session playerCount skipped, composition root missing, playerCount=" +
                             playerCount);
            return false;
        }

#if UNITY_EDITOR
        m_EditorSyntheticRuntime?.SetPlayerCount(playerCount);
#endif
        bool applied = CompositionRoot.SetPlayerCount(playerCount);
        int appliedCount = CompositionRoot.SessionHost != null && CompositionRoot.SessionHost.IsConfigured
            ? CompositionRoot.SessionHost.Config.PlayerCount
            : 0;
        GameSdkLog.Info("[新SDK整合] session playerCount request=" + playerCount + " applied=" + applied +
                  " effective=" + appliedCount +
                  (appliedCount == playerCount
                      ? string.Empty
                      : " mismatch error=" + CompositionRoot.LastError));
        return appliedCount == playerCount;
    }

    private void Awake()
    {
        if (FindFirstObjectByType<FrameworkCompositionRoot>() != null)
        {
            return;
        }

        GameSdkLog.Info("[新SDK整合] bootstrap start playerCount=" + m_PlayerCount +
                  " platform=" + Application.platform +
                  " newHostInitialized=" + YouDooSDK.Native.AndroidServerInfo.IsInitialized +
                  " adapterPresent=" + (GameProductUiAdapter.Instance != null));

        // 采集帧率必须在 CameraImage.Start 之前下发：Start 时才会把该值转给平台 provider。
        // 调低会减少摄像头解码、纹理上传和骨骼识别的每秒次数，代价是采样间隔变大。
        YoudooGame.CameraImage.CameraImage.SetTargetFps(GameSdkPerfOptions.CameraTargetFps);

        // 采集档位不在这里下发：启动档已经交给 SDK 的模型选择入口（GameSdkModelSelectionBootstrap），
        // 它会在帧服务连接后、相机第一次启动前把配置发下去，那时 catalog 已可读，也能校验档位。
        // 这里再下发一次等于让原生重新初始化一遍 ImageReader（白黑一下），
        // 而且两个入口写同一份配置，真机排查时也分不清是谁改的档。
        CameraImageStatus prepareStatus = YoudooGame.CameraImage.CameraImage.Status;
        Debug.Log("[新SDK整合] camera capture stage=bootstrap-owned state=" + prepareStatus.State +
                  " requested=" + GameSdkPerfOptions.BootstrapCaptureWidth + "x" +
                  GameSdkPerfOptions.BootstrapCaptureHeight);

        var root = new GameObject("FrameworkRoot");
        DontDestroyOnLoad(root);

        var sessionHost = root.AddComponent<MatchSessionHost>();

        var splitLayoutGo = new GameObject("CameraImageSplitLayout", typeof(RectTransform));
        splitLayoutGo.transform.SetParent(root.transform, false);
        var splitViews = new CameraImageView[4];
        for (int i = 0; i < splitViews.Length; i++)
        {
            var go = new GameObject(
                "CameraImageView" + i,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            go.SetActive(false);
            go.transform.SetParent(splitLayoutGo.transform, false);
            splitViews[i] = go.AddComponent<CameraImageView>();
            splitViews[i].StartOnEnable = false;
            go.SetActive(true);
        }

        var splitLayout = splitLayoutGo.AddComponent<CameraImageSplitLayout>();
        splitLayout.SetViews(splitViews);
        GameProductUiAdapter.RegisterFrameworkCameraViews(splitViews, splitLayout);
        GameSdkLog.Info("[新SDK整合] camera views registered count=" + splitViews.Length +
                  " cameraProvider=" + (YoudooGame.Core.PlatformServices.Get<YoudooGame.Core.ICameraImageProvider>() != null));

        var templateHost = root.AddComponent<FrameworkTemplateHost>();
        templateHost.SessionHost = sessionHost;
        templateHost.SetSceneBindings(sessionHost, null, splitViews, splitLayout);

        root.AddComponent<GameProductUiAdapter>();
        root.AddComponent<GameControllerInputBridge>();
        // 诊断组件每秒做一次反射探测并取帧，只在联调时挂，正式包由 GameSdkPerfOptions 关掉。
        if (GameSdkPerfOptions.EnableIntegrationDiagnostics)
        {
            root.AddComponent<GameSdkIntegrationDiagnostics>();
        }
        // 先创建组件（Awake 里登记 Instance），等框架/provider 就绪后再拉取存档。
        var saveService = root.AddComponent<GameSaveService>();

        // 模板必须先于组合根设置：组合根 Awake 会立即初始化并应用模板。
        if (!FrameworkTemplateResolver.TryLoad<FrameworkTemplateAsset>(
                FrameworkTemplateResourcePaths.SplitFixedTemplate,
                out var template,
                out string loadError))
        {
            Debug.LogError("[GameFrameworkBootstrap] template load failed: " + loadError);
        }
        else if (!templateHost.SetRuntimeTemplate(template))
        {
            Debug.LogError("[GameFrameworkBootstrap] SetRuntimeTemplate failed: " + templateHost.LastError);
        }
        else
        {
            GameSdkLog.Info("[新SDK整合] runtime template applied, templateId=" + template.TemplateId);
        }

#if UNITY_EDITOR
        IPlayerTrackingProvider editorTrackingProvider = PlatformServices.Get<IPlayerTrackingProvider>();
        bool useEditorSyntheticRuntime = editorTrackingProvider != null &&
            string.Equals(
                editorTrackingProvider.GetType().FullName,
                "YoudooGame.SDK.Adapter.SyntheticPlayerTrackingProvider",
                System.StringComparison.Ordinal);
        if (useEditorSyntheticRuntime)
        {
            m_EditorSyntheticRuntime = new GameEditorSyntheticMatchSessionRuntime(m_PlayerCount);
            if (!templateHost.SetRuntime(m_EditorSyntheticRuntime))
            {
                Debug.LogError("[GameFrameworkBootstrap] Editor synthetic runtime failed: " + templateHost.LastError);
            }
            else
            {
                GameSdkLog.Info("[新SDK整合] editor manual-prepare runtime installed playerCount=" + m_PlayerCount);
            }
        }
#endif

        var compositionRoot = root.AddComponent<FrameworkCompositionRoot>();
        CompositionRoot = compositionRoot;
        if (!compositionRoot.IsInitialized && !compositionRoot.Initialize())
        {
            Debug.LogError("[GameFrameworkBootstrap] " + compositionRoot.LastError);
            return;
        }

        // 组合根初始化默认选择 1 人，这里恢复项目设定的匹配人数。
        if (!compositionRoot.SetPlayerCount(m_PlayerCount))
        {
            Debug.LogError("[GameFrameworkBootstrap] SetPlayerCount failed: " + compositionRoot.LastError);
            return;
        }

        // 模板默认的骨骼点/包围盒引导是联调可视化，产品界面只要真人画面，运行期整体关掉。
        templateHost.SetCameraGuideVisibilityOverride(true, showBoundSkeletonPoints: false, showBoundPlayerBounds: false);
        GameSdkLog.Info("[新SDK整合] camera guide override applied, skeleton=false bounds=false");

        // 框架与平台 provider 就绪后再拉取 SDK 存档，避免首次请求 NotInitialized。
        saveService.Initialize();

        GameSdkLog.Info("[新SDK整合] framework initialized, playerCount=" + m_PlayerCount +
                  " adapterInstance=" + (GameProductUiAdapter.Instance != null) +
                  " liveSession=" + GameProductUiAdapter.HasLiveSession +
                  " newHost=" + (FindFirstObjectByType<YouDooSDK.Native.AndroidServerInfo>() != null));
    }
}

// 新 SDK 的 CameraImage 没有分辨率接口，采集分辨率只能随“模型选择配置”一起下发给原生层。
// 必须在 SDK 宿主完成模型选择之前配置，因此挂在 AfterAssembliesLoaded：
// 它晚于 YouDooBridgeBootstrap 在 SubsystemRegistration 的静态重置，早于 AfterSceneLoad 创建宿主，
// 也早于任何场景 Awake（挂 bootstrap 的场景不一定是首个加载场景，放 Awake 会赶不上模型选择）。
//
// 这里设的是“启动档”，SDK 会在帧服务连接后、相机第一次启动前把它下发给原生层。
// 不要再指望运行期改档：换档只会改原生采集尺寸，不会重建 Unity 侧的 RenderTexture。
// 真机日志里 requested=640x480/prepare 与 frame=4048x3040 并存，就是这件事的直接后果——
// 原生往 4048x3040 的纹理里写 640x480 的画面，准备界面四个座位框各裁 1/4，全落在空白区上。
internal static class GameSdkModelSelectionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void ApplyCameraCaptureResolution()
    {
        try
        {
            // 启动即定档：确定宽高交给 SDK 的选择器做精确匹配，没命中会按
            // InvalidSelectionPolicy 回退到设备默认档，所以不会因为某台设备
            // 没有这一档就起不来。
            YouDooAdapterRegistration.SetModelSelectionOptions(new YouDooModelSelectionOptions
            {
                ResolutionWidth = GameSdkPerfOptions.BootstrapCaptureWidth,
                ResolutionHeight = GameSdkPerfOptions.BootstrapCaptureHeight,
                // 只有走 legacy 选择器（本入口）才在档位缺失时回退到设备默认档位；profile 路径是硬失败。
                InvalidSelectionPolicy = YouDooInvalidSelectionPolicy.FallbackWithWarning,
                // 必须声明 pose 模型为必需：legacy 路径用它决定上报给帧桥的 capabilities，
                // 若填 false 会上报 HasPoseModel=false，帧桥会按“纯手部配置”处理，不再下发 SetPlayerID 人员白名单。
                RequirePoseModel = true,
                RequireHandModel = true,
                DisableFiltering = false,
            });
            Debug.Log("[新SDK整合] camera capture resolution requested=" +
                      GameSdkPerfOptions.BootstrapCaptureWidth + "x" +
                      GameSdkPerfOptions.BootstrapCaptureHeight +
                      " runtime-stage-switch=" +
                      (GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch ? "1" : "0") +
                      " frame-gate=" + (GameSdkPerfOptions.EnableCameraFrameGate ? "1" : "0"));
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[新SDK整合] camera capture resolution request failed: " + exception.Message);
        }
    }
}
