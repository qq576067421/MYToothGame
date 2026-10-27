using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using YoudooGame.CameraImage;
using YoudooGame.Core;
using YoudooGame.MatchFlow.Runtime;
using YoudooGame.SDK.Adapter;

public sealed class GameSdkIntegrationDiagnostics : MonoBehaviour
{
    private const string kTag = "[新SDK整合]";
    private const float kEvaluateInterval = 1f;
    private const float kHeartbeatInterval = 5f;
    private const float kWarningInterval = 10f;

    private static readonly Type s_AdapterModuleType = ResolveType(
        "YoudooGame.SDK.Adapter.YouDooAdapterModule",
        "YoudooGame.SDK.Adapter.YouDoo");

    private YouDooSDK.Native.AndroidServerInfo m_NewHost;
    private float m_NextEvaluateTime;
    private float m_NextHeartbeatTime;
    private float m_NextWarningTime;
    private string m_LastStatusKey = string.Empty;
    private string m_LastOwnerKey = string.Empty;
    private bool m_LastSkeletonReady;

    private void Awake()
    {
        ResolveHosts();
        GameSdkLog.Info(kTag + " diagnostics start platform=" + Application.platform +
                  " graphics=" + SystemInfo.graphicsDeviceType +
                   " adapterModuleType=" + (s_AdapterModuleType != null) +
                   " cameraProvider=" + (PlatformServices.Get<ICameraImageProvider>() != null) +
                  " newHost=" + Describe(m_NewHost));
    }

    private void Update()
    {
        if (Time.unscaledTime < m_NextEvaluateTime)
        {
            return;
        }

        m_NextEvaluateTime = Time.unscaledTime + kEvaluateInterval;
        Evaluate();
    }

    private void Evaluate()
    {
        ResolveHosts();

        bool newHostPresent = m_NewHost != null;
        bool newPlugin = ReadMember(m_NewHost, "_pluginInstance") != null;
        bool newConnected = ReadBool(m_NewHost, "isFrameInfoServerIsConnet");
        bool callbackRegistered = ReadBool(m_NewHost, "FrameCallbackRegistered");
        bool modelSelected = ReadBool(m_NewHost, "ModelSelected");
        bool frameServiceReady = ReadStaticBool(s_AdapterModuleType, "IsFrameServiceReady") ?? false;
        bool skeletonReady = YouDooSkeletonSink.IsReady;

        CameraImageStatus cameraStatus = CameraImage.Status;
        var frameResult = CameraImage.GetFrame();
        bool hasCameraFrame = frameResult.Success && frameResult.Value.Texture != null;

        string status = null;
        if (GameSdkLog.Enabled)
        {
            status = "host=" + (newHostPresent ? "yes" : "no") +
                     " newPlugin=" + Bit(newPlugin) +
                     " newConnected=" + Bit(newConnected) +
                      " callback=" + Bit(callbackRegistered) +
                      " model=" + Bit(modelSelected) +
                      " frameServiceReady=" + Bit(frameServiceReady) +
                     " skeletonReady=" + Bit(skeletonReady) +
                     " camera=" + cameraStatus.State + "/" + cameraStatus.GraphicsApi +
                     "(" + cameraStatus.Error + ":" + cameraStatus.Message + ")" +
                      " cameraFrame=" + Bit(hasCameraFrame) +
                      " liveSession=" + Bit(GameProductUiAdapter.HasLiveSession) +
                      " bridges=" + BuildCameraBridgeSummary() +
                      " views=" + BuildViewSummary();
        }

        LogOwnership(newPlugin);

        if (skeletonReady != m_LastSkeletonReady)
        {
            m_LastSkeletonReady = skeletonReady;
            GameSdkLog.Info(kTag + " skeleton sink ready=" + Bit(skeletonReady));
        }

        if (GameSdkLog.Enabled)
        {
            var adapter = GameProductUiAdapter.Instance;
            var snapshot = adapter != null ? adapter.UiSnapshot : null;
            string slotKey = BuildSlotStateKey(snapshot);
            string statusKey = status + " | " + slotKey;
            if (statusKey != m_LastStatusKey || Time.unscaledTime >= m_NextHeartbeatTime)
            {
                m_LastStatusKey = statusKey;
                m_NextHeartbeatTime = Time.unscaledTime + kHeartbeatInterval;
                GameSdkLog.Info(kTag + " status " + status + " | slots=" + BuildSlotDetail(snapshot));
            }
        }

        if (Time.unscaledTime >= m_NextWarningTime)
        {
            m_NextWarningTime = Time.unscaledTime + kWarningInterval;

            if (cameraStatus.State != CameraImageState.Running)
            {
                Debug.LogWarning(kTag + " camera image 未就绪: state=" + cameraStatus.State +
                                 " error=" + cameraStatus.Error + " message=" + cameraStatus.Message);
            }

            if (newConnected && !frameServiceReady)
            {
                Debug.LogWarning(kTag + " 帧服务已连接但未就绪: callback=" + Bit(callbackRegistered) +
                                 " model=" + Bit(modelSelected) + "，检查模型选择是否成功。");
            }

            if (!newHostPresent)
            {
                Debug.LogWarning(kTag + " 未找到新 SDK Host(YouDooSdkHost)，原生输入/帧服务没有接收方。");
            }

            // YMS 连接成功但原生帧回调没注册上，说明设备端服务与主 AAR 的 AIDL 接口
            // 版本不配套（主 AAR 用 IGameServiceV1_0_3，低版本 SDT/YMS 只声明 V1_0_2，
            // binder 调用会被拒为 "Binder invocation to an incorrect interface"），
            // 此时画面与举手准备一定同时失效。
            if (newConnected && !callbackRegistered)
            {
                Debug.LogWarning(kTag + " 原生帧回调注册失败(YMS 已连接但 callback=0)：设备服务与主 AAR 的 AIDL 接口版本不匹配，需核对 com.sdt.game.service/YMS 版本。");
            }
        }
    }

    private void LogOwnership(bool newPlugin)
    {
        string ownerKey = Bit(newPlugin);
        if (ownerKey == m_LastOwnerKey)
        {
            return;
        }

        m_LastOwnerKey = ownerKey;
        if (newPlugin)
        {
            GameSdkLog.Info(kTag + " package SDK host owns UnityDataTransfer.");
        }
    }

    private void ResolveHosts()
    {
        if (m_NewHost == null)
        {
            // SDK 自动创建的 YouDooBridge 宿主带 HideFlags.DontSave，
            // FindFirstObjectByType 看不到它，只能用全量查找或单例静态引用取回。
            var hosts = Resources.FindObjectsOfTypeAll<YouDooSDK.Native.AndroidServerInfo>();
            for (int i = 0; i < hosts.Length; i++)
            {
                if (hosts[i] != null)
                {
                    m_NewHost = hosts[i];
                    break;
                }
            }

            if (m_NewHost == null && YouDooSDK.Native.AndroidServerInfo.IsInitialized)
            {
                m_NewHost = YouDooSDK.Native.AndroidServerInfo.Instance;
            }
        }

    }

    // SDK camera bridge 使用全局原生纹理指针；输出纹理 ID 和派发状态可区分显示端与原生写入端问题。
    private static string BuildCameraBridgeSummary()
    {
        Type bridgeType = ResolveType(
            "YouDooSDK.Native.AndroidTextureBridgeBase",
            "YoudooGame.SDK.Adapter.YouDoo");
        if (bridgeType == null)
        {
            return "type-missing";
        }

        var objects = Resources.FindObjectsOfTypeAll(bridgeType);
        var builder = new StringBuilder(160);
        for (int i = 0; i < objects.Length; i++)
        {
            var component = objects[i] as Component;
            if (component == null || !component.gameObject.scene.IsValid())
            {
                continue;
            }

            var behaviour = component as Behaviour;
            var texture = ReadMember(component, "CurrentTexture") as Texture;
            bool dispatch = ReadMember(component, "renderEventFunc") is IntPtr pointer &&
                            pointer != IntPtr.Zero;
            bool paused = ReadMember(component, "_isPaused") is bool pausedValue && pausedValue;
            builder.Append(component.gameObject.name).Append(':')
                .Append(behaviour != null && behaviour.enabled ? "on" : "off").Append('/')
                .Append(texture != null ? "tex#" + texture.GetInstanceID() : "none")
                .Append("/ev").Append(Bit(dispatch))
                .Append("/pause").Append(Bit(paused))
                .Append(' ');
        }

        return builder.Length > 0 ? builder.ToString() : "none";
    }

    private static string BuildViewSummary()
    {
        var views = GameProductUiAdapter.SplitCameraViews;
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
            builder.Append(i)
                .Append(':').Append(view.gameObject.activeInHierarchy ? "on" : "off")
                .Append('/').Append(rawImage != null && rawImage.texture != null
                    ? "tex#" + rawImage.texture.GetInstanceID()
                    : "none")
                .Append('@').Append(view.transform.parent != null ? view.transform.parent.name : "-")
                .Append(' ');
        }

        return builder.ToString();
    }

    private static string BuildSlotStateKey(MatchSessionUiSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return "none";
        }

        var builder = new StringBuilder(128);
        builder.Append(snapshot.SessionPhase);
        var slots = snapshot.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            builder.Append('[').Append(slot.VisualSlotIndex)
                .Append(':').Append(slot.PersonId)
                .Append(':').Append(slot.State)
                .Append(':').Append(Bit(slot.HasObservation))
                .Append(Bit(slot.IsReady))
                .Append(':').Append(slot.AvatarSyncState)
                .Append(']');
        }

        return builder.ToString();
    }

    private static string BuildSlotDetail(MatchSessionUiSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return "none";
        }

        var builder = new StringBuilder(320);
        var slots = snapshot.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            builder.Append("[v").Append(slot.VisualSlotIndex)
                .Append(" s").Append(slot.SlotIndex)
                .Append(" p").Append(slot.PersonId)
                .Append(' ').Append(slot.State)
                .Append(" obs=").Append(Bit(slot.HasObservation))
                .Append(" ready=").Append(Bit(slot.IsReady))
                .Append(" avatar=").Append(slot.AvatarSyncState)
                .Append(" prog=").Append(slot.Progress01.ToString("F2"))
                .Append(" stable=").Append(slot.StableFrameCount).Append('/').Append(slot.StableFramesRequired)
                .Append(" readyF=").Append(slot.ReadyFrameCount).Append('/').Append(slot.ReadyFramesRequired)
                .Append(" missing=").Append(slot.MissingFrameCount)
                .Append(" uv=").Append(slot.Bounds.Center.x.ToString("F2"))
                .Append(',').Append(slot.Bounds.Center.y.ToString("F2"))
                .Append("] ");
        }

        return builder.ToString();
    }

    private static string Describe(Component component)
    {
        return component != null ? component.gameObject.name : "none";
    }

    private static string Bit(bool value)
    {
        return value ? "1" : "0";
    }

    private static bool ReadBool(object target, string memberName)
    {
        return ReadMember(target, memberName) is bool value && value;
    }

    private static object ReadMember(object target, string memberName)
    {
        if (target == null)
        {
            return null;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Type type = target.GetType();
        while (type != null && type != typeof(object))
        {
            var property = type.GetProperty(memberName, flags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(target);
            }

            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                return field.GetValue(target);
            }

            type = type.BaseType;
        }

        return null;
    }

    private static bool? ReadStaticBool(Type type, string memberName)
    {
        if (type == null)
        {
            return null;
        }

        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public |
                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var property = type.GetProperty(memberName, flags);
        if (property != null && property.PropertyType == typeof(bool))
        {
            return (bool)property.GetValue(null);
        }

        var field = type.GetField(memberName, flags);
        if (field != null && field.FieldType == typeof(bool))
        {
            return (bool)field.GetValue(null);
        }

        return null;
    }

    private static Type ResolveType(string fullName, string assemblyName)
    {
        var type = Type.GetType(fullName + ", " + assemblyName);
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
}
