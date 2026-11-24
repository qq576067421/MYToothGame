#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDll
{
    // 真机侧 GM 桥客户端：连到编辑器监听端口，收命令、回执、按秒推送状态。
    // 只在 Development 构建里由 GmBridgeBootstrap 创建；正式包不含该类型。
    public sealed class GmBridgeClient : MonoBehaviour
    {
        private const float m_RetryDelayFirstSecond = 1f;
        private const float m_RetryDelayMaxSecond = 8f;
        private const int m_MaxPendingSendCount = 200;

        private readonly ConcurrentQueue<string> m_ReceiveQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> m_ThreadLogQueue = new ConcurrentQueue<string>();
        private readonly Queue<byte[]> m_SendQueue = new Queue<byte[]>();
        private readonly AutoResetEvent m_SendSignal = new AutoResetEvent(false);
        private readonly object m_SendLock = new object();
        private readonly object m_ConnectionLock = new object();

        private TcpClient m_Client;
        private Thread m_ConnectThread;
        private Thread m_ReceiveThread;
        private Thread m_SendThread;
        private volatile bool m_IsConnected;
        private volatile bool m_IsConnecting;
        private volatile bool m_IsShutdown;
        private volatile bool m_NeedSendHello;

        private string m_Host;
        private int m_Port;
        private string m_Session = string.Empty;
        private float m_NextConnectTime;
        private float m_RetryDelaySecond = m_RetryDelayFirstSecond;
        private float m_LastReceiveRealTime;
        private float m_NextPingTime;
        private float m_NextStateTime;
        private int m_NextCommandId;
        private bool m_HasLoggedSelfTestHint;

        public void Init(string host, int port)
        {
            m_Host = host;
            m_Port = port;
            m_NextConnectTime = 0f;
            m_RetryDelaySecond = m_RetryDelayFirstSecond;
        }

        private void Update()
        {
            if (m_IsShutdown)
            {
                return;
            }

            FlushThreadLogs();
            HandleConnected();
            DrainReceiveQueue();
            UpdateHeartbeat();
            UpdateReconnect();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void HandleConnected()
        {
            if (!m_IsConnected || !m_NeedSendHello)
            {
                return;
            }

            m_NeedSendHello = false;
            m_RetryDelaySecond = m_RetryDelayFirstSecond;
            m_LastReceiveRealTime = Time.realtimeSinceStartup;
            m_NextPingTime = m_LastReceiveRealTime + GmBridgeProtocol.m_PingIntervalSecond;
            m_NextStateTime = m_LastReceiveRealTime + GmBridgeProtocol.m_StatePushIntervalSecond;

            var hello = new GmBridgeMessage
            {
                m_Type = GmBridgeProtocol.m_TypeHello,
                m_Device = SystemInfo.deviceModel,
                m_App = Application.version,
                m_Unity = Application.unityVersion,
                m_IsDevelopment = Debug.isDebugBuild,
                m_Scene = SceneManager.GetActiveScene().name,
                m_Ts = GmBridgeProtocol.ReadTimestampMs(),
            };
            Send(JsonUtility.ToJson(hello));
        }

        private void DrainReceiveQueue()
        {
            while (m_ReceiveQueue.TryDequeue(out string payload))
            {
                m_LastReceiveRealTime = Time.realtimeSinceStartup;
                GmBridgeMessage message = TryParseMessage(payload);
                if (message == null || string.IsNullOrEmpty(message.m_Type))
                {
                    continue;
                }

                switch (message.m_Type)
                {
                    case GmBridgeProtocol.m_TypeCmd:
                        HandleCommand(message);
                        break;
                    case GmBridgeProtocol.m_TypeHelloAck:
                        m_Session = message.m_Session ?? string.Empty;
                        Debug.Log("[GM桥] 编辑器已确认会话：" + m_Session);
                        break;
                    case GmBridgeProtocol.m_TypePing:
                        break;
                }
            }
        }

        private void HandleCommand(GmBridgeMessage message)
        {
            GmBridgeCommandResult result = GmBridgeCommandDispatcher.Execute(message.m_Name, message.m_ArgsJson);
            var ack = new GmBridgeMessage
            {
                m_Type = GmBridgeProtocol.m_TypeAck,
                m_Id = message.m_Id,
                m_Ok = result.m_Ok,
                m_Message = result.m_Message,
                m_DataJson = result.m_DataJson,
                m_Ts = GmBridgeProtocol.ReadTimestampMs(),
            };
            Send(JsonUtility.ToJson(ack));

            Debug.Log(string.Format(
                "[GM桥] 命令 {0} {1}{2}",
                message.m_Name,
                result.m_Ok ? "成功" : "失败",
                string.IsNullOrEmpty(result.m_Message) ? string.Empty : "：" + result.m_Message));
        }

        private void UpdateHeartbeat()
        {
            if (!m_IsConnected)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (now - m_LastReceiveRealTime > GmBridgeProtocol.m_ReceiveTimeoutSecond)
            {
                HandleDisconnected(m_Client, "心跳超时，等待重连");
                return;
            }

            if (now >= m_NextPingTime)
            {
                m_NextPingTime = now + GmBridgeProtocol.m_PingIntervalSecond;
                Send(JsonUtility.ToJson(new GmBridgeMessage
                {
                    m_Type = GmBridgeProtocol.m_TypePing,
                    m_Ts = GmBridgeProtocol.ReadTimestampMs(),
                }));
            }

            if (now >= m_NextStateTime)
            {
                m_NextStateTime = now + GmBridgeProtocol.m_StatePushIntervalSecond;
                Send(JsonUtility.ToJson(new GmBridgeMessage
                {
                    m_Type = GmBridgeProtocol.m_TypeState,
                    m_DataJson = GmBridgeCommandDispatcher.BuildStateJson(),
                    m_Ts = GmBridgeProtocol.ReadTimestampMs(),
                }));
            }
        }

        private void UpdateReconnect()
        {
            if (m_IsConnected || m_IsConnecting)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (now < m_NextConnectTime)
            {
                return;
            }

            BeginConnect();
        }

        private void BeginConnect()
        {
            m_IsConnecting = true;
            m_NextConnectTime = Time.realtimeSinceStartup + m_RetryDelaySecond;
            m_RetryDelaySecond = Mathf.Min(m_RetryDelaySecond * 2f, m_RetryDelayMaxSecond);
            m_ConnectThread = new Thread(ConnectLoop)
            {
                IsBackground = true,
                Name = "GmBridgeConnectThread",
            };
            m_ConnectThread.Start();
        }

        private void ConnectLoop()
        {
            TcpClient client = null;
            try
            {
                client = new TcpClient { NoDelay = true };
                IAsyncResult result = client.BeginConnect(m_Host, m_Port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(GmBridgeProtocol.m_ConnectTimeoutSecond)))
                {
                    SafeClose(client);
                    EnqueueThreadLog(string.Format("[GM桥] 连接超时 {0}:{1}，稍后重试", m_Host, m_Port));
                    return;
                }

                client.EndConnect(result);

                lock (m_ConnectionLock)
                {
                    if (m_IsShutdown)
                    {
                        SafeClose(client);
                        return;
                    }

                    m_Client = client;
                    m_ReceiveThread = new Thread(() => ReceiveLoop(client))
                    {
                        IsBackground = true,
                        Name = "GmBridgeReceiveThread",
                    };
                    m_SendThread = new Thread(() => SendLoop(client))
                    {
                        IsBackground = true,
                        Name = "GmBridgeSendThread",
                    };
                    // 必须先置位再启动线程：发送线程的循环条件就是 m_IsConnected。
                    m_IsConnected = true;
                    m_NeedSendHello = true;
                    m_ReceiveThread.Start();
                    m_SendThread.Start();
                }

                EnqueueThreadLog(string.Format("[GM桥] 已连接编辑器 {0}:{1}", m_Host, m_Port));
            }
            catch (Exception exception)
            {
                SafeClose(client);
                EnqueueThreadLog("[GM桥] 连接失败：" + exception.Message);
            }
            finally
            {
                m_IsConnecting = false;
            }
        }

        private void ReceiveLoop(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                while (!m_IsShutdown)
                {
                    string payload = GmBridgeFrameCodec.ReadFrame(stream);
                    if (!string.IsNullOrEmpty(payload))
                    {
                        m_ReceiveQueue.Enqueue(payload);
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                HandleDisconnected(client, "与编辑器的连接已断开");
            }
        }

        private void SendLoop(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                while (!m_IsShutdown && m_IsConnected)
                {
                    if (!m_SendSignal.WaitOne(100))
                    {
                        continue;
                    }

                    while (true)
                    {
                        byte[] frame;
                        lock (m_SendLock)
                        {
                            if (m_SendQueue.Count <= 0)
                            {
                                break;
                            }

                            frame = m_SendQueue.Dequeue();
                        }

                        stream.Write(frame, 0, frame.Length);
                        stream.Flush();
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                HandleDisconnected(client, "发送通道已断开");
            }
        }

        private void Send(string payload)
        {
            if (string.IsNullOrEmpty(payload) || !m_IsConnected)
            {
                return;
            }

            byte[] frame;
            try
            {
                frame = GmBridgeFrameCodec.EncodeFrame(payload);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[GM桥] 报文编码失败：" + exception.Message);
                return;
            }

            lock (m_SendLock)
            {
                // 慢网络下积压过多就丢最旧的，避免真机内存被调试报文吃掉。
                if (m_SendQueue.Count >= m_MaxPendingSendCount)
                {
                    m_SendQueue.Dequeue();
                }

                m_SendQueue.Enqueue(frame);
            }

            m_SendSignal.Set();
        }

        private void HandleDisconnected(TcpClient client, string reason)
        {
            bool shouldLog;
            lock (m_ConnectionLock)
            {
                shouldLog = client != null && ReferenceEquals(m_Client, client);
                if (shouldLog)
                {
                    m_Client = null;
                    m_IsConnected = false;
                }
            }

            if (!shouldLog)
            {
                return;
            }

            SafeClose(client);
            EnqueueThreadLog("[GM桥] " + reason);
        }

        public void Shutdown()
        {
            if (m_IsShutdown)
            {
                return;
            }

            m_IsShutdown = true;
            TcpClient client;
            lock (m_ConnectionLock)
            {
                client = m_Client;
                m_Client = null;
                m_IsConnected = false;
            }

            SafeClose(client);
            m_SendSignal.Set();
        }

        private void FlushThreadLogs()
        {
            while (m_ThreadLogQueue.TryDequeue(out string message))
            {
                Debug.Log(message);
            }

            if (!m_HasLoggedSelfTestHint && Application.isEditor)
            {
                m_HasLoggedSelfTestHint = true;
                Debug.Log("[GM桥] 当前处于编辑器自测模式，连接 127.0.0.1:17362");
            }
        }

        private void EnqueueThreadLog(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                m_ThreadLogQueue.Enqueue(message);
            }
        }

        private static GmBridgeMessage TryParseMessage(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<GmBridgeMessage>(payload);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void SafeClose(TcpClient client)
        {
            if (client == null)
            {
                return;
            }

            try
            {
                if (client.Client != null)
                {
                    try
                    {
                        client.Client.LingerState = new LingerOption(true, 0);
                    }
                    catch
                    {
                    }
                }

                client.Close();
            }
            catch
            {
            }
        }
    }
}
#endif
