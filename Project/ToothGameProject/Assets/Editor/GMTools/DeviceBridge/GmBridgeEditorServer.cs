#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace GameDll
{
    // 编辑器侧 GM 桥服务端：GMTools 打开开关就监听，不依赖 Play 模式，也不和骨骼接收库抢端口。
    // 只接受一台真机，新连接顶掉旧连接；网络线程只搬字节，JSON 解析统一放到 EditorApplication.update。
    [InitializeOnLoad]
    public static class GmBridgeEditorServer
    {
        private sealed class PendingCommand
        {
            public string m_Name;
            public double m_SendTime;
        }

        // 后台线程也会取时间戳，统一用 Stopwatch，避免跨线程读 Unity 的编辑器时钟。
        private static readonly System.Diagnostics.Stopwatch m_Clock = System.Diagnostics.Stopwatch.StartNew();
        private static readonly ConcurrentQueue<string> m_ReceiveQueue = new ConcurrentQueue<string>();
        private static readonly Queue<byte[]> m_SendQueue = new Queue<byte[]>();
        private static readonly Dictionary<int, PendingCommand> m_PendingCommands = new Dictionary<int, PendingCommand>();
        private static readonly List<string> m_Logs = new List<string>();
        private static readonly object m_SendLock = new object();
        private static readonly object m_ConnectionLock = new object();
        private static readonly object m_LogLock = new object();
        private static readonly AutoResetEvent m_SendSignal = new AutoResetEvent(false);

        private static TcpListener m_Listener;
        private static TcpClient m_Client;
        private static Thread m_ListenThread;
        private static Thread m_ReceiveThread;
        private static Thread m_SendThread;
        private static volatile bool m_IsListening;
        private static volatile bool m_IsClientConnected;

        private static string m_LastError = string.Empty;
        private static string m_DeviceName = string.Empty;
        private static string m_AppVersion = string.Empty;
        private static string m_UnityVersion = string.Empty;
        private static string m_DeviceScene = string.Empty;
        private static string m_StateJson = string.Empty;
        private static double m_LastReceiveTime;
        private static int m_NextCommandId;

        static GmBridgeEditorServer()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.quitting += OnEditorQuitting;

            if (GmBridgeEditorSettings.ReadIsEnabled())
            {
                EditorApplication.delayCall -= StartFromPersistence;
                EditorApplication.delayCall += StartFromPersistence;
            }
        }

        public static bool Start()
        {
            if (m_IsListening)
            {
                return true;
            }

            try
            {
                int port = GmBridgeEditorSettings.ReadPort();
                m_Listener = new TcpListener(IPAddress.Any, port);
                m_Listener.Start();
                m_IsListening = true;
                m_LastError = string.Empty;
                m_ListenThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "GmBridgeListenThread",
                };
                m_ListenThread.Start();
                AppendLog(string.Format(
                    "[GM桥] 开始监听 0.0.0.0:{0}，真机应连 {1}:{0}",
                    port,
                    ReadLocalIpv4OrPlaceholder()));
                return true;
            }
            catch (Exception exception)
            {
                m_LastError = "监听失败：" + exception.Message + "（端口可能被占用，可改端口后重试）";
                AppendLog("[GM桥] " + m_LastError);
                Stop();
                return false;
            }
        }

        public static void Stop()
        {
            m_IsListening = false;

            TcpListener listener;
            TcpClient client;
            lock (m_ConnectionLock)
            {
                listener = m_Listener;
                client = m_Client;
                m_Listener = null;
                m_Client = null;
                m_IsClientConnected = false;
            }

            try
            {
                listener?.Stop();
            }
            catch (Exception)
            {
            }

            SafeClose(client);
            m_SendSignal.Set();
            JoinThread(m_ListenThread);
            JoinThread(m_ReceiveThread);
            JoinThread(m_SendThread);
            m_ListenThread = null;
            m_ReceiveThread = null;
            m_SendThread = null;

            lock (m_SendLock)
            {
                m_SendQueue.Clear();
            }

            m_PendingCommands.Clear();
            ClearDeviceInfo();
            AppendLog("[GM桥] 已停止监听");
        }

        public static bool TrySendCommand(string name, string argsJson)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (!m_IsClientConnected)
            {
                AppendLog("[GM桥] 未连接真机，命令未发送：" + name);
                return false;
            }

            int commandId = ++m_NextCommandId;
            var message = new GmBridgeMessage
            {
                m_Type = GmBridgeProtocol.m_TypeCmd,
                m_Id = commandId,
                m_Name = name,
                m_ArgsJson = argsJson ?? string.Empty,
                m_Ts = GmBridgeProtocol.ReadTimestampMs(),
            };

            if (!SendJson(JsonUtility.ToJson(message)))
            {
                AppendLog("[GM桥] 命令发送失败：" + name);
                return false;
            }

            m_PendingCommands[commandId] = new PendingCommand
            {
                m_Name = name,
                m_SendTime = ReadNow(),
            };
            AppendLog("[GM桥] 已发送 " + name);
            return true;
        }

        public static bool ReadIsListening()
        {
            return m_IsListening;
        }

        public static bool ReadIsClientConnected()
        {
            return m_IsClientConnected;
        }

        public static string ReadLastError()
        {
            return m_LastError;
        }

        public static string ReadDeviceSummary()
        {
            if (!m_IsClientConnected)
            {
                return "未连接";
            }

            return string.Format(
                "{0} | 包版本 {1} | Unity {2} | 场景 {3}",
                string.IsNullOrEmpty(m_DeviceName) ? "未知设备" : m_DeviceName,
                string.IsNullOrEmpty(m_AppVersion) ? "未知" : m_AppVersion,
                string.IsNullOrEmpty(m_UnityVersion) ? "未知" : m_UnityVersion,
                string.IsNullOrEmpty(m_DeviceScene) ? "未知" : m_DeviceScene);
        }

        public static string ReadStateJson()
        {
            return m_StateJson;
        }

        public static GmBridgeStateDto ReadDeviceState()
        {
            if (string.IsNullOrEmpty(m_StateJson))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<GmBridgeStateDto>(m_StateJson);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static double ReadSecondsSinceLastReceive()
        {
            if (m_LastReceiveTime <= 0d)
            {
                return -1d;
            }

            return ReadNow() - m_LastReceiveTime;
        }

        public static string[] ReadLogsCopy()
        {
            lock (m_LogLock)
            {
                return m_Logs.ToArray();
            }
        }

        public static void ClearLogs()
        {
            lock (m_LogLock)
            {
                m_Logs.Clear();
            }
        }

        private static void StartFromPersistence()
        {
            if (GmBridgeEditorSettings.ReadIsEnabled() && !m_IsListening)
            {
                Start();
            }
        }

        private static void OnEditorUpdate()
        {
            if (m_IsListening)
            {
                DrainReceiveQueue();
                CheckReceiveTimeout();
                CheckPendingCommandTimeouts();
            }
        }

        private static void ListenLoop()
        {
            while (m_IsListening)
            {
                TcpClient client;
                try
                {
                    TcpListener listener = m_Listener;
                    if (listener == null)
                    {
                        break;
                    }

                    client = listener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    if (!m_IsListening)
                    {
                        break;
                    }

                    continue;
                }

                AttachClient(client);
            }
        }

        private static void AttachClient(TcpClient client)
        {
            TcpClient oldClient;
            Thread oldReceiveThread;
            Thread oldSendThread;

            lock (m_ConnectionLock)
            {
                oldClient = m_Client;
                oldReceiveThread = m_ReceiveThread;
                oldSendThread = m_SendThread;

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

                m_IsClientConnected = true;
                m_LastError = string.Empty;
                m_LastReceiveTime = ReadNow();
                m_NextCommandId = 0;
                m_PendingCommands.Clear();
                m_StateJson = string.Empty;
                ClearDeviceInfo();

                lock (m_SendLock)
                {
                    m_SendQueue.Clear();
                }

                m_ReceiveThread.Start();
                m_SendThread.Start();
            }

            SafeClose(oldClient);
            JoinThread(oldReceiveThread);
            JoinThread(oldSendThread);
            AppendLog("[GM桥] 真机已接入：" + ReadEndpoint(client));
        }

        private static void ReceiveLoop(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                while (m_IsListening)
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
                DetachClient(client, "真机连接已断开");
            }
        }

        private static void SendLoop(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                while (m_IsListening)
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
                DetachClient(client, "真机发送通道已断开");
            }
        }

        private static void DetachClient(TcpClient client, string reason)
        {
            lock (m_ConnectionLock)
            {
                if (!ReferenceEquals(m_Client, client))
                {
                    return;
                }

                m_Client = null;
                m_IsClientConnected = false;
            }

            SafeClose(client);
            ClearDeviceInfo();
            AppendLog("[GM桥] " + reason);
        }

        private static bool SendJson(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return false;
            }

            byte[] frame;
            try
            {
                frame = GmBridgeFrameCodec.EncodeFrame(payload);
            }
            catch (Exception exception)
            {
                AppendLog("[GM桥] 报文编码失败：" + exception.Message);
                return false;
            }

            lock (m_SendLock)
            {
                m_SendQueue.Enqueue(frame);
            }

            m_SendSignal.Set();
            return true;
        }

        private static void DrainReceiveQueue()
        {
            while (m_ReceiveQueue.TryDequeue(out string payload))
            {
                m_LastReceiveTime = ReadNow();
                GmBridgeMessage message;
                try
                {
                    message = JsonUtility.FromJson<GmBridgeMessage>(payload);
                }
                catch (Exception)
                {
                    continue;
                }

                if (message == null || string.IsNullOrEmpty(message.m_Type))
                {
                    continue;
                }

                switch (message.m_Type)
                {
                    case GmBridgeProtocol.m_TypeHello:
                        m_DeviceName = message.m_Device;
                        m_AppVersion = message.m_App;
                        m_UnityVersion = message.m_Unity;
                        m_DeviceScene = message.m_Scene;
                        SendJson(JsonUtility.ToJson(new GmBridgeMessage
                        {
                            m_Type = GmBridgeProtocol.m_TypeHelloAck,
                            m_Session = Guid.NewGuid().ToString("N").Substring(0, 8),
                            m_ServerPort = GmBridgeEditorSettings.ReadPort(),
                            m_Ts = GmBridgeProtocol.ReadTimestampMs(),
                        }));
                        AppendLog(string.Format(
                            "[GM桥] 握手完成：{0}（Development={1}，Unity {2}）",
                            string.IsNullOrEmpty(m_DeviceName) ? "未知设备" : m_DeviceName,
                            message.m_IsDevelopment,
                            string.IsNullOrEmpty(m_UnityVersion) ? "未知" : m_UnityVersion));
                        break;
                    case GmBridgeProtocol.m_TypeAck:
                        m_PendingCommands.Remove(message.m_Id);
                        // 部分命令会带回最新状态，先刷新回显，不用等下一次 1 秒推送。
                        TryApplyStateJson(message.m_DataJson);
                        // log.tail 的结果直接倒进面板日志区。
                        TryApplyLogTail(message.m_DataJson);
                        AppendLog(string.Format(
                            "[GM桥] 回执 #{0} {1}{2}",
                            message.m_Id,
                            message.m_Ok ? "成功" : "失败",
                            string.IsNullOrEmpty(message.m_Message) ? string.Empty : "：" + message.m_Message));
                        break;
                    case GmBridgeProtocol.m_TypeState:
                        m_StateJson = message.m_DataJson;
                        break;
                    case GmBridgeProtocol.m_TypePing:
                        // 保活应答：真机侧按“最近收到报文时间”判超时（8 秒），而编辑器平时
                        // 只在用户点按钮时才发报文。不回 ping 的话，真机连上后 8 秒内必然
                        // 自己判超时断开、1 秒后重连，面板就表现成“一会断开一会连上”。
                        // 回一条 ping 而不是新增 pong 类型，是为了兼容已装在真机上的旧包
                        // （旧包收到任何报文都会先刷新 m_LastReceiveRealTime）。
                        if (m_IsClientConnected)
                        {
                            SendJson(JsonUtility.ToJson(new GmBridgeMessage
                            {
                                m_Type = GmBridgeProtocol.m_TypePing,
                                m_Ts = GmBridgeProtocol.ReadTimestampMs(),
                            }));
                        }
                        break;
                }
            }
        }

        private static void TryApplyLogTail(string dataJson)
        {
            if (string.IsNullOrEmpty(dataJson))
            {
                return;
            }

            try
            {
                GmBridgeLogTailDto tail = JsonUtility.FromJson<GmBridgeLogTailDto>(dataJson);
                if (tail == null || string.IsNullOrEmpty(tail.m_LogText))
                {
                    return;
                }

                string[] lines = tail.m_LogText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int start = Mathf.Max(0, lines.Length - 40);
                for (int i = start; i < lines.Length; i++)
                {
                    AppendLog("[真机] " + lines[i].TrimEnd('\r'));
                }
            }
            catch (Exception)
            {
            }
        }

        // 真机每 2 秒一次心跳，超过 8 秒没任何报文就当作掉线，避免面板停在“已连接”的假状态。
        private static void CheckReceiveTimeout()
        {
            if (!m_IsClientConnected)
            {
                return;
            }

            if (ReadSecondsSinceLastReceive() <= GmBridgeProtocol.m_ReceiveTimeoutSecond)
            {
                return;
            }

            // 一次性把引用取到本地：避免传参和判断之间被其他线程置空。
            TcpClient timeoutClient = m_Client;
            if (timeoutClient == null)
            {
                m_IsClientConnected = false;
                return;
            }

            DetachClient(timeoutClient, "真机心跳超时，已断开");
        }

        private static void TryApplyStateJson(string dataJson)
        {
            if (string.IsNullOrEmpty(dataJson))
            {
                return;
            }

            try
            {
                GmBridgeStateDto state = JsonUtility.FromJson<GmBridgeStateDto>(dataJson);
                if (state != null && !string.IsNullOrEmpty(state.m_Scene))
                {
                    m_StateJson = dataJson;
                }
            }
            catch (Exception)
            {
            }
        }

        private static void CheckPendingCommandTimeouts()
        {
            if (m_PendingCommands.Count <= 0)
            {
                return;
            }

            double now = ReadNow();
            List<int> timeoutIds = null;
            foreach (var pair in m_PendingCommands)
            {
                if (now - pair.Value.m_SendTime <= GmBridgeProtocol.m_CommandTimeoutSecond)
                {
                    continue;
                }

                if (timeoutIds == null)
                {
                    timeoutIds = new List<int>();
                }

                timeoutIds.Add(pair.Key);
            }

            if (timeoutIds == null)
            {
                return;
            }

            for (int i = 0; i < timeoutIds.Count; i++)
            {
                PendingCommand pendingCommand = m_PendingCommands[timeoutIds[i]];
                m_PendingCommands.Remove(timeoutIds[i]);
                AppendLog("[GM桥] 命令超时：" + pendingCommand.m_Name);
            }
        }

        private static void ClearDeviceInfo()
        {
            m_DeviceName = string.Empty;
            m_AppVersion = string.Empty;
            m_UnityVersion = string.Empty;
            m_DeviceScene = string.Empty;
            m_StateJson = string.Empty;
        }

        private static double ReadNow()
        {
            return m_Clock.Elapsed.TotalSeconds;
        }

        // 后台线程也会写日志，统一用日志锁保护；Debug.Log 本身允许跨线程调用。
        private static void AppendLog(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            string line = string.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss"), message);
            lock (m_LogLock)
            {
                m_Logs.Add(line);
                while (m_Logs.Count > GmBridgeProtocol.m_MaxLogEntryCount)
                {
                    m_Logs.RemoveAt(0);
                }
            }

            Debug.Log(line);
        }

        private static string ReadLocalIpv4OrPlaceholder()
        {
            string ip = GmBridgeEditorSettings.ReadLocalIpv4();
            return string.IsNullOrEmpty(ip) ? "<本机局域网IP>" : ip;
        }

        private static string ReadEndpoint(TcpClient client)
        {
            try
            {
                if (client != null && client.Client != null && client.Client.RemoteEndPoint != null)
                {
                    return client.Client.RemoteEndPoint.ToString();
                }
            }
            catch (Exception)
            {
            }

            return "未知地址";
        }

        private static void JoinThread(Thread thread)
        {
            if (thread == null || !thread.IsAlive || Thread.CurrentThread == thread)
            {
                return;
            }

            try
            {
                thread.Join(300);
            }
            catch (Exception)
            {
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
                    catch (Exception)
                    {
                    }
                }

                client.Close();
            }
            catch (Exception)
            {
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            Stop();
        }

        private static void OnEditorQuitting()
        {
            Stop();
        }
    }
}
#endif
