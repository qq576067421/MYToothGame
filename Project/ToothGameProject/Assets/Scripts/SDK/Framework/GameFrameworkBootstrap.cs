using UnityEngine;
using UnityEngine.UI;
using YoudooGame.CameraImage;
using YoudooGame.Core;
using YoudooGame.Framework;
using YoudooGame.MatchFlow.Runtime;

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
        root.AddComponent<GameSdkIntegrationDiagnostics>();
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
