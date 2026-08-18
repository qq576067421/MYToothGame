#if BoneReceiverLib
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GameDll
{
    public sealed class RemoteBoneFrameSourceProxy
    {
        private const float m_SimulatedFrameLogIntervalSeconds = 5f;

        [Serializable]
        private sealed class StatusDto
        {
            public string m_Status;
            public bool m_IsListening;
            public bool m_IsClientConnected;
            public int m_ClientCount;
            public long m_LastReceiveUtcTicks;
            public long m_LastHeartbeatUtcTicks;
            public int m_HeartbeatCount;
            public string m_LastHeartbeatSessionId;
            public string m_LastClientEndpoint;
            public string m_LastError;
        }

        private const string m_AssemblyName = "BoneReceiverLib";
        private const string m_TypeName = "CompanyInternalTools.BoneReceiverLib.BoneReceiverRuntime";
        private const string m_ListenHost = "0.0.0.0";
        private static readonly long m_FreshFrameWindowTicks = TimeSpan.FromSeconds(1.5d).Ticks;

        private readonly MethodInfo m_StartListenerMethod;
        private readonly MethodInfo m_StopListenerMethod;
        private readonly MethodInfo m_RegisterLoggerMethod;
        private readonly MethodInfo m_ReadStatusJsonMethod;
        private readonly MethodInfo m_TryReadLatestFrameJsonMethod;
        private readonly BoneFrameJsonConverter m_Converter = new BoneFrameJsonConverter();

        private BoneFrameData m_CachedFrameData;
        private bool m_HasFreshFrame;
        private string m_LastFrameJson;
        private bool m_HasStartedListener;
        private bool m_LastLoggedClientConnected;
        private string m_LastLoggedClientEndpoint;
        private int m_LastLoggedHeartbeatCount;
        private bool m_HasRegisteredLogger;
        private float m_NextSimulatedFrameLogTime;

        public static bool ReadIsFeatureEnabled()
        {
#if UNITY_EDITOR
            return BoneRemoteDebugEditorConfig.ReadIsRemoteEnabled();
#else
            return false;
#endif
        }

        public static RemoteBoneFrameSourceProxy TryCreate()
        {
#if UNITY_EDITOR
            if (!ReadIsFeatureEnabled())
            {
                return null;
            }

            if (!TryResolveRuntimeType(out Type runtimeType, true))
            {
                return null;
            }

            try
            {
                return new RemoteBoneFrameSourceProxy(runtimeType);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[骨骼远程调试] 初始化远程骨骼来源失败: " + exception.Message);
                return null;
            }
#else
            return null;
#endif
        }

        public static void TryForceStopListener(string reason)
        {
#if UNITY_EDITOR
            if (!TryResolveRuntimeType(out Type runtimeType, false))
            {
                return;
            }

            MethodInfo stopListenerMethod = runtimeType.GetMethod("StopListener", BindingFlags.Public | BindingFlags.Static);
            MethodInfo registerLoggerMethod = runtimeType.GetMethod("RegisterLogger", BindingFlags.Public | BindingFlags.Static);
            TryStopListener(stopListenerMethod, registerLoggerMethod, reason, true);
#endif
        }

        private RemoteBoneFrameSourceProxy(Type runtimeType)
        {
            m_StartListenerMethod = runtimeType.GetMethod("StartListener", BindingFlags.Public | BindingFlags.Static);
            m_StopListenerMethod = runtimeType.GetMethod("StopListener", BindingFlags.Public | BindingFlags.Static);
            m_RegisterLoggerMethod = runtimeType.GetMethod("RegisterLogger", BindingFlags.Public | BindingFlags.Static);
            m_ReadStatusJsonMethod = runtimeType.GetMethod("ReadStatusJson", BindingFlags.Public | BindingFlags.Static);
            m_TryReadLatestFrameJsonMethod = runtimeType.GetMethod("TryReadLatestFrameJson", BindingFlags.Public | BindingFlags.Static);
            ValidateRequiredMethods();
            StartListener();
        }

        public void Tick()
        {
            if (!m_HasStartedListener)
            {
                return;
            }

            StatusDto status;
            try
            {
                status = ReadStatus();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[骨骼远程调试] 读取接收状态失败: " + exception.Message);
                m_HasFreshFrame = false;
                m_NextSimulatedFrameLogTime = 0f;
                return;
            }

            if (!m_HasRegisteredLogger)
            {
                ReportStatusEvents(status);
            }

            if (!ReadIsStatusFresh(status))
            {
                m_HasFreshFrame = false;
                m_NextSimulatedFrameLogTime = 0f;
                return;
            }

            string frameJson;
            try
            {
                frameJson = InvokeStringMethod(m_TryReadLatestFrameJsonMethod);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[骨骼远程调试] 读取远程骨骼帧失败: " + exception.Message);
                m_HasFreshFrame = false;
                m_NextSimulatedFrameLogTime = 0f;
                return;
            }

            if (string.IsNullOrEmpty(frameJson))
            {
                m_HasFreshFrame = false;
                m_NextSimulatedFrameLogTime = 0f;
                return;
            }

            if (frameJson != m_LastFrameJson)
            {
                m_LastFrameJson = frameJson;
                m_CachedFrameData = m_Converter.Convert(frameJson);
            }

            m_HasFreshFrame = m_CachedFrameData != null && m_CachedFrameData.m_HasFrameData;
            TryLogSimulatedFrame();
        }

        public bool ReadIsActive()
        {
            return m_HasFreshFrame;
        }

        public bool TryReadLatestFrame(out BoneFrameData frameData)
        {
            if (m_HasFreshFrame && m_CachedFrameData != null)
            {
                frameData = m_CachedFrameData;
                return true;
            }

            frameData = null;
            return false;
        }

        public void Shutdown()
        {
            if (!m_HasStartedListener)
            {
                return;
            }

            try
            {
                TryStopListener(m_StopListenerMethod, m_RegisterLoggerMethod, null, false);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[骨骼远程调试] 停止接收监听失败: " + exception.Message);
            }
            finally
            {
                m_HasStartedListener = false;
                m_HasFreshFrame = false;
                m_CachedFrameData = null;
                m_LastFrameJson = null;
                m_LastLoggedClientConnected = false;
                m_LastLoggedClientEndpoint = null;
                m_LastLoggedHeartbeatCount = 0;
                m_NextSimulatedFrameLogTime = 0f;
            }
        }

        private void ReportStatusEvents(StatusDto status)
        {
            if (status == null)
            {
                return;
            }

            if (!status.m_IsClientConnected)
            {
                m_LastLoggedClientConnected = false;
                m_LastLoggedClientEndpoint = null;
                return;
            }

            if (status.m_IsClientConnected &&
                !string.IsNullOrEmpty(status.m_LastClientEndpoint) &&
                (!m_LastLoggedClientConnected ||
                 !string.Equals(status.m_LastClientEndpoint, m_LastLoggedClientEndpoint, StringComparison.Ordinal)))
            {
                m_LastLoggedClientConnected = true;
                m_LastLoggedClientEndpoint = status.m_LastClientEndpoint;
                Debug.Log("[骨骼远程调试] 已接入发送端: " + status.m_LastClientEndpoint);
            }

            if (status.m_HeartbeatCount > m_LastLoggedHeartbeatCount)
            {
                m_LastLoggedHeartbeatCount = status.m_HeartbeatCount;
                Debug.Log(string.Format(
                    "[骨骼远程调试] 收到心跳: 客户端={0}, 会话={1}, 次数={2}",
                    string.IsNullOrEmpty(status.m_LastClientEndpoint) ? "未知" : status.m_LastClientEndpoint,
                    string.IsNullOrEmpty(status.m_LastHeartbeatSessionId) ? "未知" : status.m_LastHeartbeatSessionId,
                    status.m_HeartbeatCount));
            }
        }

        private void ValidateRequiredMethods()
        {
            if (m_StartListenerMethod == null ||
                m_StopListenerMethod == null ||
                m_ReadStatusJsonMethod == null ||
                m_TryReadLatestFrameJsonMethod == null)
            {
                throw new MissingMethodException(m_TypeName, "BoneReceiverRuntime 接口不完整");
            }
        }

        private void StartListener()
        {
            RegisterLogger(OnReceiverLogMessage);
            m_StartListenerMethod.Invoke(null, new object[] { m_ListenHost, BoneRemoteDebugEditorConfig.ReadPort() });
            m_HasStartedListener = true;
        }

        private static bool TryResolveRuntimeType(out Type runtimeType, bool writeWarning)
        {
            runtimeType = null;

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(one => one.GetName().Name == m_AssemblyName);
            if (assembly == null)
            {
                try
                {
                    assembly = Assembly.Load(m_AssemblyName);
                }
                catch (Exception exception)
                {
                    if (writeWarning)
                    {
                        Debug.LogWarning("[骨骼远程调试] 载入接收库失败: " + exception.Message);
                    }

                    return false;
                }
            }

            runtimeType = assembly.GetType(m_TypeName, false);
            if (runtimeType != null)
            {
                return true;
            }

            if (writeWarning)
            {
                Debug.LogWarning("[骨骼远程调试] 接收库类型不存在: " + m_TypeName);
            }

            return false;
        }

        private void TryLogSimulatedFrame()
        {
            if (!m_HasFreshFrame || m_CachedFrameData == null || !m_CachedFrameData.m_IsSimulated)
            {
                m_NextSimulatedFrameLogTime = 0f;
                return;
            }

            float now = Time.unscaledTime;
            if (m_NextSimulatedFrameLogTime > 0f && now < m_NextSimulatedFrameLogTime)
            {
                return;
            }

            m_NextSimulatedFrameLogTime = now + m_SimulatedFrameLogIntervalSeconds;
            Debug.Log("[骨骼远程调试] 收到模拟骨骼数据: " + BuildSimulatedFrameSummary(m_CachedFrameData));
        }

        private static string BuildSimulatedFrameSummary(BoneFrameData frameData)
        {
            if (frameData == null)
            {
                return "无帧数据";
            }

            int personCount = CountTrackedPersons(frameData);
            if (personCount <= 0)
            {
                return string.Format(
                    "帧={0}, 有效人数=0, 时间戳={1}",
                    frameData.m_FrameSerial,
                    frameData.m_FrameTimeMs);
            }

            TryReadFirstTrackedPerson(frameData, out int firstTrackedSlotIndex, out BonePersonData firstPerson);
            string rightWristText = ReadJointText(firstPerson != null ? firstPerson.m_Body : null, (int)YouDooSDKConstants.KeyPointIndex.Rightwrist);
            string leftWristText = ReadJointText(firstPerson != null ? firstPerson.m_Body : null, (int)YouDooSDKConstants.KeyPointIndex.Leftwrist);
            return string.Format(
                "帧={0}, 有效人数={1}, 首个有效槽位={2}, 人物Id={3}, 左手腕={4}, 右手腕={5}, 时间戳={6}",
                frameData.m_FrameSerial,
                personCount,
                firstTrackedSlotIndex >= 0 ? firstTrackedSlotIndex + 1 : 0,
                firstPerson != null ? firstPerson.m_PersonId : YouDooSDKConstants.PersonIdNull,
                leftWristText,
                rightWristText,
                frameData.m_FrameTimeMs);
        }

        private static int CountTrackedPersons(BoneFrameData frameData)
        {
            if (frameData == null || frameData.m_Persons == null)
            {
                return 0;
            }

            int trackedCount = 0;
            for (int i = 0; i < frameData.m_Persons.Count; i++)
            {
                if (IsTrackedPerson(frameData.m_Persons[i]))
                {
                    trackedCount++;
                }
            }

            return trackedCount;
        }

        private static bool TryReadFirstTrackedPerson(BoneFrameData frameData, out int slotIndex, out BonePersonData person)
        {
            if (frameData != null && frameData.m_Persons != null)
            {
                for (int i = 0; i < frameData.m_Persons.Count; i++)
                {
                    if (!IsTrackedPerson(frameData.m_Persons[i]))
                    {
                        continue;
                    }

                    slotIndex = i;
                    person = frameData.m_Persons[i];
                    return true;
                }
            }

            slotIndex = -1;
            person = null;
            return false;
        }

        private static bool IsTrackedPerson(BonePersonData person)
        {
            return person != null && person.m_PersonId != YouDooSDKConstants.PersonIdNull;
        }

        private static string ReadJointText(BoneDetectPartData part, int jointIndex)
        {
            if (part == null || part.m_Joints == null || jointIndex < 0 || jointIndex >= part.m_Joints.Length)
            {
                return "缺失";
            }

            BoneJointData joint = part.m_Joints[jointIndex];
            if (joint == null || !joint.m_IsTracked)
            {
                return "缺失";
            }

            return string.Format("({0:F3},{1:F3},{2:F3})", joint.m_X, joint.m_Y, joint.m_Z);
        }

        private void RegisterLogger(Action<string> logHandler)
        {
            if (m_RegisterLoggerMethod == null)
            {
                m_HasRegisteredLogger = false;
                return;
            }

            try
            {
                m_RegisterLoggerMethod.Invoke(null, new object[] { logHandler });
                m_HasRegisteredLogger = logHandler != null;
            }
            catch (Exception exception)
            {
                m_HasRegisteredLogger = false;
                Debug.LogWarning("[骨骼远程调试] 注册接收日志回调失败: " + exception.Message);
            }
        }

        private static void TryStopListener(
            MethodInfo stopListenerMethod,
            MethodInfo registerLoggerMethod,
            string reason,
            bool writeStopLog)
        {
            if (stopListenerMethod == null)
            {
                return;
            }

            try
            {
                registerLoggerMethod?.Invoke(null, new object[] { null });
                stopListenerMethod.Invoke(null, null);
                if (writeStopLog)
                {
                    Debug.Log("[骨骼远程调试] 已停止远程监听（原因：" + (string.IsNullOrEmpty(reason) ? "未提供" : reason) + "）");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[骨骼远程调试] 停止接收监听失败: " + exception.Message);
            }
        }

        private void OnReceiverLogMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Debug.Log(message);
        }

        private StatusDto ReadStatus()
        {
            string statusJson = InvokeStringMethod(m_ReadStatusJsonMethod);
            if (string.IsNullOrEmpty(statusJson))
            {
                return null;
            }

            return JsonUtility.FromJson<StatusDto>(statusJson);
        }

        private static bool ReadIsStatusFresh(StatusDto status)
        {
            if (status == null || !status.m_IsListening || !status.m_IsClientConnected || status.m_LastReceiveUtcTicks <= 0)
            {
                return false;
            }

            long ageTicks = DateTime.UtcNow.Ticks - status.m_LastReceiveUtcTicks;
            return ageTicks >= 0 && ageTicks <= m_FreshFrameWindowTicks;
        }

        private static string InvokeStringMethod(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return string.Empty;
            }

            return methodInfo.Invoke(null, null) as string;
        }
    }
}
#endif
