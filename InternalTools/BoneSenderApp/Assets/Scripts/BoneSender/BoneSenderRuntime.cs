using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BoneSender
{
    [DefaultExecutionOrder(-32000)]
    public class BoneSenderRuntime : MonoBehaviour
    {
        private const float m_HeartbeatIntervalSeconds = 30f;
        private const float m_RealFrameLogIntervalSeconds = 5f;
        private const string m_HeartbeatPrefix = "BONE_HEARTBEAT|";

        public SenderBoneParseData m_ParseData;
        public BoneSenderConfig m_Config = new BoneSenderConfig();

        private TcpBoneSenderClient m_Client;
        private bool m_IsSending;
        private string m_StatusText = "stopped";
        private float m_NextSendTime;
        private float m_NextReconnectTime;
        private float m_NextHeartbeatTime;
        private int m_NextTargetHostIndex;
        private string m_CurrentTargetHost = string.Empty;
        private float m_LastSendTime = -1f;
        private int m_LastSentFrameSerial;
        private int m_LastSentPersonCount;
        private bool m_LastSentFrameIsSimulated;
        private long m_LastSentCaptureTimeMs;
        private BoneProtocolFrame m_LastSentFrameSnapshot;
        private float m_SendStatWindowStartTime = -1f;
        private int m_SendStatWindowFrameCount;
        private float m_MeasuredSendFramesPerSecond;
        private bool m_HasTriggeredSdkInitialization;
        private float m_NextRealFrameLogTime;
        private float m_NextWaitingFrameLogTime;

        private void Awake()
        {
            PrepareRuntimeEnvironment();
            m_Client = new TcpBoneSenderClient();
        }

        private void OnEnable()
        {
            PrepareRuntimeEnvironment();
            if (m_Config != null && m_Config.m_AutoStartOnEnable)
            {
                StartSending();
            }
        }

        private void OnDisable()
        {
            StopSending();
        }

        private void Update()
        {
            if (!m_IsSending)
            {
                return;
            }

            if (m_ParseData == null || m_Config == null)
            {
                m_StatusText = "missing_config";
                return;
            }

            if (!EnsureConnected())
            {
                return;
            }

            TryFlushPendingLogPayloads();
            TryBeginSdkInitializationAfterConnect();
            TryFlushPendingLogPayloads();

            if (Time.unscaledTime < m_NextSendTime)
            {
                TrySendHeartbeatIfDue();
                return;
            }

            var frame = m_ParseData.ReadLatestFrame();
            if (frame == null)
            {
                TryLogWaitingForFrame();
                TrySendHeartbeatIfDue();
                m_StatusText = "waiting_frame";
                return;
            }

            m_NextWaitingFrameLogTime = 0f;
            try
            {
                string frameJson = JsonUtility.ToJson(frame, false);
                m_Client.SendFrameJson(frameJson);
                RecordSentFrame(frame);
                m_NextSendTime = Time.unscaledTime + ReadSendIntervalSeconds();
                TrySendHeartbeatIfDue();
                m_StatusText = "sending frame=" + frame.m_FrameSerial;
                TryLogSentRealFrame(frame, frameJson);
            }
            catch (Exception exception)
            {
                m_StatusText = "send_failed: " + exception.Message;
                BoneSenderAppLogger.LogError("发送骨骼帧失败: " + exception.Message);
                m_Client.Disconnect();
                m_NextReconnectTime = Time.unscaledTime + ReadReconnectDelaySeconds();
            }
        }

        public void StartSending()
        {
            m_IsSending = true;
            m_StatusText = "starting";
            m_NextSendTime = 0f;
            m_NextReconnectTime = 0f;
            m_NextHeartbeatTime = Time.unscaledTime + m_HeartbeatIntervalSeconds;
            m_NextTargetHostIndex = 0;
            m_CurrentTargetHost = string.Empty;
            m_LastSendTime = -1f;
            m_LastSentFrameSerial = 0;
            m_LastSentPersonCount = 0;
            m_LastSentFrameIsSimulated = false;
            m_LastSentCaptureTimeMs = 0L;
            m_LastSentFrameSnapshot = null;
            m_SendStatWindowStartTime = -1f;
            m_SendStatWindowFrameCount = 0;
            m_MeasuredSendFramesPerSecond = 0f;
            m_NextRealFrameLogTime = 0f;
            m_NextWaitingFrameLogTime = 0f;
            BoneSenderAppLogger.ClearPendingPayloads();
            BoneSenderAppLogger.Log("骨骼发送流程已启动，等待连接主工程接收端");
        }

        public void StopSending()
        {
            m_IsSending = false;
            m_StatusText = "stopped";
            m_CurrentTargetHost = string.Empty;
            m_LastSentFrameSnapshot = null;
            m_NextRealFrameLogTime = 0f;
            m_NextWaitingFrameLogTime = 0f;
            BoneSenderAppLogger.Log("骨骼发送流程已停止");
            m_Client?.Disconnect();
        }

        public bool ReadIsSending()
        {
            return m_IsSending;
        }

        public string ReadStatusText()
        {
            return m_StatusText;
        }

        public string ReadInputSourceDisplayName()
        {
            return ReadUsesSdkRuntime()
                ? "真机SDK输入（当前设备直接读取骨骼SDK数据）"
                : "PC模拟输入（当前设备本地生成模拟骨骼数据）";
        }

        public string ReadPreviewLayoutDisplayName()
        {
            return "PartitionView（水平4槽，槽位从左到右依次为1到4）";
        }

        public string ReadUiSlotBindingSummary()
        {
            return m_ParseData != null ? m_ParseData.ReadUiSlotBindingSummary() : "未找到骨骼解析对象";
        }

        public int ReadBoundSdkSlotDisplayIndex(int uiSlotIndex)
        {
            if (m_ParseData == null)
            {
                return 0;
            }

            int sdkSlotIndex = m_ParseData.ReadBoundSdkSlotIndex(uiSlotIndex);
            return sdkSlotIndex >= 0 ? sdkSlotIndex + 1 : 0;
        }

        public string ReadNetworkStateDisplayName()
        {
            if (!m_IsSending)
            {
                return "未发送（当前发送功能已停止）";
            }

            if (m_Client != null && m_Client.ReadIsConnected())
            {
                return "已连接（当前正在向目标地址发送骨骼数据）";
            }

            if (Time.unscaledTime < m_NextReconnectTime)
            {
                return string.Format(
                    "等待重连（{0:F1} 秒后重试）",
                    Mathf.Max(0f, m_NextReconnectTime - Time.unscaledTime));
            }

            return "连接中（正在尝试连接目标地址）";
        }

        public int ReadConfiguredSendFramesPerSecond()
        {
            if (m_Config == null || m_Config.m_SendFps <= 0)
            {
                return 20;
            }

            return Mathf.Max(1, m_Config.m_SendFps);
        }

        public float ReadMeasuredSendFramesPerSecond()
        {
            RefreshSendRateWindow();
            return m_MeasuredSendFramesPerSecond;
        }

        public float ReadHeartbeatIntervalSeconds()
        {
            return m_HeartbeatIntervalSeconds;
        }

        public float ReadSecondsUntilNextHeartbeat()
        {
            if (!m_IsSending || m_Client == null || !m_Client.ReadIsConnected())
            {
                return 0f;
            }

            return Mathf.Max(0f, m_NextHeartbeatTime - Time.unscaledTime);
        }

        public int ReadTargetPort()
        {
            return m_Config != null ? m_Config.m_TargetPort : 0;
        }

        public string ReadCurrentTargetHost()
        {
            return string.IsNullOrEmpty(m_CurrentTargetHost) ? "none" : m_CurrentTargetHost;
        }

        public string[] ReadTargetHosts()
        {
            return ReadConfiguredTargetHosts();
        }

        public int ReadLastSentFrameSerial()
        {
            return m_LastSentFrameSerial;
        }

        public int ReadLastSentPersonCount()
        {
            return m_LastSentPersonCount;
        }

        public string ReadLastSentDataTypeDisplayName()
        {
            if (m_LastSentFrameSerial <= 0)
            {
                return "暂无发送数据";
            }

            return m_LastSentFrameIsSimulated
                ? "模拟骨骼数据（PC本地生成）"
                : "真实骨骼数据（真机SDK采集）";
        }

        public long ReadLastSentDataAgeMilliseconds()
        {
            if (m_LastSentCaptureTimeMs <= 0L)
            {
                return -1L;
            }

            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return Math.Max(0L, nowMs - m_LastSentCaptureTimeMs);
        }

        public long ReadLastSendAgeMilliseconds()
        {
            if (m_LastSendTime < 0f)
            {
                return -1L;
            }

            return (long)Mathf.Max(0f, (Time.unscaledTime - m_LastSendTime) * 1000f);
        }

        public BoneProtocolFrame ReadLastSentFrameSnapshot()
        {
            return m_LastSentFrameSnapshot;
        }

        private bool EnsureConnected()
        {
            if (m_Client != null && m_Client.ReadIsConnected())
            {
                return true;
            }

            if (Time.unscaledTime < m_NextReconnectTime)
            {
                return false;
            }

            if (m_Config == null)
            {
                return false;
            }

            bool connected = TryConnectConfiguredTargets();
            if (!connected)
            {
                m_NextReconnectTime = Time.unscaledTime + ReadReconnectDelaySeconds();
                return false;
            }

            m_NextHeartbeatTime = Time.unscaledTime + m_HeartbeatIntervalSeconds;
            m_StatusText = "connected " + m_CurrentTargetHost + ":" + ReadTargetPort();
            BoneSenderAppLogger.Log("已连接到主工程接收端: " + m_CurrentTargetHost + ":" + ReadTargetPort());
            return true;
        }

        private bool TryConnectConfiguredTargets()
        {
            if (m_Client == null || m_Config == null)
            {
                return false;
            }

            string[] targetHosts = ReadConfiguredTargetHosts();
            if (targetHosts.Length <= 0)
            {
                m_CurrentTargetHost = string.Empty;
                m_StatusText = "missing_target_hosts";
                return false;
            }

            int startIndex = Mathf.Clamp(m_NextTargetHostIndex, 0, targetHosts.Length - 1);
            int targetPort = ReadTargetPort();
            int connectTimeoutMilliseconds = ReadConnectTimeoutMilliseconds();
            for (int offset = 0; offset < targetHosts.Length; offset++)
            {
                int hostIndex = (startIndex + offset) % targetHosts.Length;
                string targetHost = targetHosts[hostIndex];
                if (!m_Client.Connect(targetHost, targetPort, connectTimeoutMilliseconds))
                {
                    continue;
                }

                m_CurrentTargetHost = targetHost;
                m_NextTargetHostIndex = hostIndex;
                return true;
            }

            m_CurrentTargetHost = targetHosts[startIndex];
            m_NextTargetHostIndex = (startIndex + 1) % targetHosts.Length;
            m_StatusText = "connect_failed_all_targets";
            return false;
        }

        private string[] ReadConfiguredTargetHosts()
        {
            if (m_Config == null)
            {
                return Array.Empty<string>();
            }

            var resolvedHosts = new List<string>();
            AppendResolvedHost(resolvedHosts, m_Config.m_TargetHost);
            if (m_Config.m_TargetHosts != null)
            {
                for (int i = 0; i < m_Config.m_TargetHosts.Length; i++)
                {
                    AppendResolvedHost(resolvedHosts, m_Config.m_TargetHosts[i]);
                }
            }

            return resolvedHosts.ToArray();
        }

        private static void AppendResolvedHost(List<string> resolvedHosts, string host)
        {
            if (resolvedHosts == null || string.IsNullOrWhiteSpace(host))
            {
                return;
            }

            string trimmedHost = host.Trim();
            for (int i = 0; i < resolvedHosts.Count; i++)
            {
                if (string.Equals(resolvedHosts[i], trimmedHost, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            resolvedHosts.Add(trimmedHost);
        }

        private float ReadSendIntervalSeconds()
        {
            if (m_Config == null || m_Config.m_SendFps <= 0)
            {
                return 0.05f;
            }

            return 1f / Mathf.Max(1, m_Config.m_SendFps);
        }

        private float ReadReconnectDelaySeconds()
        {
            return m_Config != null ? Mathf.Max(0.2f, m_Config.m_ReconnectDelaySeconds) : 2f;
        }

        private int ReadConnectTimeoutMilliseconds()
        {
            return m_Config != null ? Mathf.Max(100, m_Config.m_ConnectTimeoutMilliseconds) : 800;
        }

        private void TrySendHeartbeatIfDue()
        {
            if (m_Client == null || !m_Client.ReadIsConnected() || Time.unscaledTime < m_NextHeartbeatTime)
            {
                return;
            }

            try
            {
                m_Client.SendPayload(BuildHeartbeatPayload());
                m_NextHeartbeatTime = Time.unscaledTime + m_HeartbeatIntervalSeconds;
            }
            catch (Exception exception)
            {
                m_StatusText = "heartbeat_failed: " + exception.Message;
                BoneSenderAppLogger.LogWarning("发送心跳失败: " + exception.Message);
                m_Client.Disconnect();
                m_NextReconnectTime = Time.unscaledTime + ReadReconnectDelaySeconds();
            }
        }

        private void RecordSentFrame(BoneProtocolFrame frame)
        {
            m_LastSendTime = Time.unscaledTime;
            m_LastSentFrameSerial = frame != null ? frame.m_FrameSerial : 0;
            m_LastSentPersonCount = CountTrackedPersons(frame);
            m_LastSentFrameIsSimulated = frame != null && frame.m_IsSimulated;
            m_LastSentCaptureTimeMs = frame != null ? frame.m_CaptureTimeMs : 0L;
            m_LastSentFrameSnapshot = CloneFrame(frame);

            if (m_SendStatWindowStartTime < 0f)
            {
                m_SendStatWindowStartTime = m_LastSendTime;
                m_SendStatWindowFrameCount = 0;
            }

            m_SendStatWindowFrameCount++;
            RefreshSendRateWindow();
        }

        private void RefreshSendRateWindow()
        {
            if (m_SendStatWindowStartTime < 0f)
            {
                return;
            }

            float now = Time.unscaledTime;
            float elapsedSeconds = now - m_SendStatWindowStartTime;
            if (elapsedSeconds < 1f)
            {
                return;
            }

            m_MeasuredSendFramesPerSecond = elapsedSeconds > 0f
                ? m_SendStatWindowFrameCount / elapsedSeconds
                : 0f;
            m_SendStatWindowStartTime = now;
            m_SendStatWindowFrameCount = 0;
        }

        private static BoneProtocolFrame CloneFrame(BoneProtocolFrame source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new BoneProtocolFrame();
            clone.m_ProtocolVersion = source.m_ProtocolVersion;
            clone.m_SessionId = source.m_SessionId ?? string.Empty;
            clone.m_FrameSerial = source.m_FrameSerial;
            clone.m_IsSimulated = source.m_IsSimulated;
            clone.m_CaptureTimeMs = source.m_CaptureTimeMs;
            clone.m_ImageWidth = source.m_ImageWidth;
            clone.m_ImageHeight = source.m_ImageHeight;

            if (source.m_Persons == null || source.m_Persons.Length <= 0)
            {
                clone.m_Persons = Array.Empty<BoneProtocolPerson>();
                return clone;
            }

            clone.m_Persons = new BoneProtocolPerson[source.m_Persons.Length];
            for (int i = 0; i < source.m_Persons.Length; i++)
            {
                clone.m_Persons[i] = ClonePerson(source.m_Persons[i]);
            }

            return clone;
        }

        private static BoneProtocolPerson ClonePerson(BoneProtocolPerson source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new BoneProtocolPerson();
            clone.m_PersonId = source.m_PersonId;
            clone.m_Body = ClonePart(source.m_Body);
            clone.m_LeftHand = ClonePart(source.m_LeftHand);
            clone.m_RightHand = ClonePart(source.m_RightHand);
            clone.m_Face = ClonePart(source.m_Face);
            return clone;
        }

        private static BoneProtocolPart ClonePart(BoneProtocolPart source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new BoneProtocolPart();
            clone.m_Score = source.m_Score;
            clone.m_Type = source.m_Type;
            clone.m_Rect = CloneRect(source.m_Rect);

            if (source.m_Joints == null || source.m_Joints.Length <= 0)
            {
                clone.m_Joints = Array.Empty<BoneProtocolJoint>();
                return clone;
            }

            clone.m_Joints = new BoneProtocolJoint[source.m_Joints.Length];
            for (int i = 0; i < source.m_Joints.Length; i++)
            {
                clone.m_Joints[i] = CloneJoint(source.m_Joints[i]);
            }

            return clone;
        }

        private static BoneProtocolRect CloneRect(BoneProtocolRect source)
        {
            if (source == null)
            {
                return new BoneProtocolRect();
            }

            return new BoneProtocolRect
            {
                m_IsTracked = source.m_IsTracked,
                m_Left = source.m_Left,
                m_Top = source.m_Top,
                m_Right = source.m_Right,
                m_Bottom = source.m_Bottom,
            };
        }

        private static BoneProtocolJoint CloneJoint(BoneProtocolJoint source)
        {
            if (source == null)
            {
                return null;
            }

            return new BoneProtocolJoint
            {
                m_IsTracked = source.m_IsTracked,
                m_X = source.m_X,
                m_Y = source.m_Y,
                m_Z = source.m_Z,
                m_Score = source.m_Score,
            };
        }

        private string BuildHeartbeatPayload()
        {
            string sessionId = m_ParseData != null ? m_ParseData.ReadSessionId() : string.Empty;
            int frameSerial = m_ParseData != null ? m_ParseData.ReadLatestFrameSerial() : 0;
            return string.Format(
                "{0}{1}|{2}|{3}",
                m_HeartbeatPrefix,
                string.IsNullOrEmpty(sessionId) ? "unknown" : sessionId,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                frameSerial);
        }

        private void TryLogWaitingForFrame()
        {
            if (!ReadUsesSdkRuntime())
            {
                return;
            }

            float now = Time.unscaledTime;
            if (m_NextWaitingFrameLogTime > 0f && now < m_NextWaitingFrameLogTime)
            {
                return;
            }

            m_NextWaitingFrameLogTime = now + m_RealFrameLogIntervalSeconds;
            int latestFrameSerial = m_ParseData != null ? m_ParseData.ReadLatestFrameSerial() : 0;
            BoneSenderAppLogger.LogWarning("已连接主工程，但暂未取到可发送的骨骼帧，当前最新帧序号=" + latestFrameSerial);
        }

        private void TryLogSentRealFrame(BoneProtocolFrame frame, string frameJson)
        {
            if (frame == null || frame.m_IsSimulated)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (m_NextRealFrameLogTime > 0f && now < m_NextRealFrameLogTime)
            {
                return;
            }

            m_NextRealFrameLogTime = now + m_RealFrameLogIntervalSeconds;
            int personCount = CountTrackedPersons(frame);
            TryReadFirstTrackedPerson(frame, out int firstTrackedSlotIndex, out BoneProtocolPerson firstPerson);
            BoneSenderAppLogger.Log(string.Format(
                "已发送真机骨骼帧: 帧={0}, 有效人数={1}, 首个有效槽位={2}, 人物标识={3}, 左手腕={4}, 右手腕={5}, 载荷字节={6}, 采集时间戳={7}",
                frame.m_FrameSerial,
                personCount,
                firstTrackedSlotIndex >= 0 ? firstTrackedSlotIndex + 1 : 0,
                firstPerson != null ? firstPerson.m_PersonId : YouDooSDKConstants.PersonIdNull,
                ReadBodyJointText(firstPerson != null ? firstPerson.m_Body : null, (int)YouDooSDKConstants.KeyPointIndex.Leftwrist),
                ReadBodyJointText(firstPerson != null ? firstPerson.m_Body : null, (int)YouDooSDKConstants.KeyPointIndex.Rightwrist),
                string.IsNullOrEmpty(frameJson) ? 0 : Encoding.UTF8.GetByteCount(frameJson),
                frame.m_CaptureTimeMs));
        }

        private static int CountTrackedPersons(BoneProtocolFrame frame)
        {
            if (frame == null || frame.m_Persons == null)
            {
                return 0;
            }

            int trackedCount = 0;
            for (int i = 0; i < frame.m_Persons.Length; i++)
            {
                if (IsTrackedPerson(frame.m_Persons[i]))
                {
                    trackedCount++;
                }
            }

            return trackedCount;
        }

        private static bool TryReadFirstTrackedPerson(BoneProtocolFrame frame, out int slotIndex, out BoneProtocolPerson person)
        {
            if (frame != null && frame.m_Persons != null)
            {
                for (int i = 0; i < frame.m_Persons.Length; i++)
                {
                    if (!IsTrackedPerson(frame.m_Persons[i]))
                    {
                        continue;
                    }

                    slotIndex = i;
                    person = frame.m_Persons[i];
                    return true;
                }
            }

            slotIndex = -1;
            person = null;
            return false;
        }

        private static bool IsTrackedPerson(BoneProtocolPerson person)
        {
            return person != null && person.m_PersonId != YouDooSDKConstants.PersonIdNull;
        }

        private static string ReadBodyJointText(BoneProtocolPart part, int jointIndex)
        {
            if (part == null || part.m_Joints == null || jointIndex < 0 || jointIndex >= part.m_Joints.Length)
            {
                return "缺失";
            }

            BoneProtocolJoint joint = part.m_Joints[jointIndex];
            if (joint == null || !joint.m_IsTracked)
            {
                return "未跟踪";
            }

            return string.Format("({0:F1}, {1:F1}, {2:F1})", joint.m_X, joint.m_Y, joint.m_Z);
        }

        private static void PrepareRuntimeEnvironment()
        {
            if (ReadUsesSdkRuntime())
            {
                return;
            }

            var serverInfo = GameObject.FindFirstObjectByType<AndroidServerInfo>();
            if (serverInfo != null && serverInfo.gameObject.activeSelf)
            {
                serverInfo.gameObject.SetActive(false);
            }
        }

        private void TryBeginSdkInitializationAfterConnect()
        {
            if (!ReadUsesSdkRuntime())
            {
                return;
            }

            if (m_HasTriggeredSdkInitialization)
            {
                return;
            }

            BoneSenderAndroidServerInfo serverInfo = BoneSenderAndroidServerInfo.Current;
            if (serverInfo == null)
            {
                serverInfo = GameObject.FindFirstObjectByType<BoneSenderAndroidServerInfo>();
            }

            if (serverInfo == null)
            {
                BoneSenderAppLogger.LogError("场景中未找到 BoneSenderAndroidServerInfo，无法启动真机骨骼采集");
                return;
            }

            if (serverInfo.BeginSdkInitializationOnce())
            {
                m_HasTriggeredSdkInitialization = true;
            }
        }

        private void TryFlushPendingLogPayloads()
        {
            if (m_Client == null || !m_Client.ReadIsConnected())
            {
                return;
            }

            while (BoneSenderAppLogger.TryDequeuePayload(out string payload))
            {
                try
                {
                    m_Client.SendPayload(payload);
                }
                catch (Exception exception)
                {
                    BoneSenderAppLogger.LogWarning("发送设备日志失败: " + exception.Message);
                    m_StatusText = "send_log_failed: " + exception.Message;
                    m_Client.Disconnect();
                    m_NextReconnectTime = Time.unscaledTime + ReadReconnectDelaySeconds();
                    break;
                }
            }
        }

        private static bool ReadUsesSdkRuntime()
        {
            return !Application.isEditor && Application.platform == RuntimePlatform.Android;
        }
    }
}
