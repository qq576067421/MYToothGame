using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CompanyInternalTools.BoneReceiverLib
{
    public sealed class BoneReceiverServer
    {
        private static readonly long m_FrameReceiveLogIntervalTicks = TimeSpan.FromSeconds(5d).Ticks;

        private readonly object m_Lock = new object();

        private TcpListener m_Listener;
        private Thread m_ListenThread;
        private TcpClient m_CurrentClient;
        private Thread m_ClientThread;
        private string m_LatestFrameJson;
        private BoneReceiverState m_State = new BoneReceiverState();
        private volatile bool m_IsRunning;
        private int m_ReceivedFrameCount;
        private long m_NextFrameReceiveLogUtcTicks;

        public void Start(string host, int port)
        {
            lock (m_Lock)
            {
                if (m_IsRunning)
                {
                    return;
                }

                IPAddress address = ResolveListenAddress(host);
                m_Listener = new TcpListener(address, port);
                m_Listener.Start();
                m_IsRunning = true;
                m_State.m_Status = "listening";
                m_State.m_IsListening = true;
                m_State.m_IsClientConnected = false;
                m_State.m_ClientCount = 0;
                m_State.m_LastReceiveUtcTicks = 0L;
                m_State.m_LastHeartbeatUtcTicks = 0L;
                m_State.m_HeartbeatCount = 0;
                m_State.m_LastHeartbeatSessionId = string.Empty;
                m_State.m_LastClientEndpoint = string.Empty;
                m_State.m_LastError = string.Empty;
                m_ReceivedFrameCount = 0;
                m_NextFrameReceiveLogUtcTicks = 0L;
                m_ListenThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "BoneReceiverListenThread",
                };
                m_ListenThread.Start();
            }
        }

        public void Stop()
        {
            Thread listenThread = null;
            Thread clientThread = null;
            TcpClient client = null;

            lock (m_Lock)
            {
                if (!m_IsRunning)
                {
                    m_State.m_Status = "stopped";
                    m_State.m_IsListening = false;
                    m_State.m_IsClientConnected = false;
                    m_State.m_ClientCount = 0;
                    m_State.m_LastReceiveUtcTicks = 0L;
                    m_State.m_LastHeartbeatUtcTicks = 0L;
                    m_State.m_HeartbeatCount = 0;
                    m_State.m_LastHeartbeatSessionId = string.Empty;
                    m_State.m_LastClientEndpoint = string.Empty;
                    m_LatestFrameJson = null;
                    m_ReceivedFrameCount = 0;
                    m_NextFrameReceiveLogUtcTicks = 0L;
                    return;
                }

                m_IsRunning = false;
                listenThread = m_ListenThread;
                clientThread = m_ClientThread;
                client = m_CurrentClient;
                m_ListenThread = null;
                m_ClientThread = null;
                m_CurrentClient = null;

                if (m_Listener != null)
                {
                    try
                    {
                        m_Listener.Stop();
                    }
                    catch
                    {
                    }

                    m_Listener = null;
                }

                m_State.m_Status = "stopped";
                m_State.m_IsListening = false;
                m_State.m_IsClientConnected = false;
                m_State.m_ClientCount = 0;
                m_State.m_LastReceiveUtcTicks = 0L;
                m_State.m_LastHeartbeatUtcTicks = 0L;
                m_State.m_HeartbeatCount = 0;
                m_State.m_LastHeartbeatSessionId = string.Empty;
                m_State.m_LastClientEndpoint = string.Empty;
                m_LatestFrameJson = null;
                m_ReceivedFrameCount = 0;
                m_NextFrameReceiveLogUtcTicks = 0L;
            }

            SafeCloseClient(client);
            JoinThread(listenThread);
            JoinThread(clientThread);
            BoneReceiverRuntime.WriteLog("[骨骼接收库] 已停止监听");
        }

        public BoneReceiverState ReadState()
        {
            lock (m_Lock)
            {
                return m_State.Clone();
            }
        }

        public string TryReadLatestFrameJson()
        {
            lock (m_Lock)
            {
                return m_LatestFrameJson;
            }
        }

        private void ListenLoop()
        {
            while (m_IsRunning)
            {
                TcpClient client;
                try
                {
                    client = m_Listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    if (!m_IsRunning)
                    {
                        break;
                    }

                    SetError("accept_failed");
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    SetError("accept_failed:" + exception.Message);
                    continue;
                }

                AttachClient(client);
            }
        }

        private void AttachClient(TcpClient client)
        {
            Thread oldClientThread;
            TcpClient oldClient;
            string clientEndpoint = ReadClientEndpoint(client);
            string oldClientEndpoint = string.Empty;
            bool hadOldClient;

            lock (m_Lock)
            {
                oldClientThread = m_ClientThread;
                oldClient = m_CurrentClient;
                hadOldClient = oldClient != null;
                if (hadOldClient)
                {
                    oldClientEndpoint = ReadClientEndpoint(oldClient);
                }

                m_CurrentClient = client;
                m_ClientThread = new Thread(() => ClientLoop(client, clientEndpoint))
                {
                    IsBackground = true,
                    Name = "BoneReceiverClientThread",
                };
                m_State.m_Status = "connected";
                m_State.m_IsClientConnected = true;
                m_State.m_ClientCount = 1;
                m_State.m_LastClientEndpoint = clientEndpoint;
                m_State.m_LastError = string.Empty;
                m_ReceivedFrameCount = 0;
                m_NextFrameReceiveLogUtcTicks = 0L;
                m_ClientThread.Start();
                BoneReceiverRuntime.WriteLog("[骨骼接收库] 客户端已连接: " + clientEndpoint);
            }

            if (hadOldClient)
            {
                BoneReceiverRuntime.WriteLog(
                    "[骨骼接收库] 检测到新的发送端连接，已替换旧连接: 旧客户端=" +
                    oldClientEndpoint +
                    "，新客户端=" +
                    clientEndpoint);
            }

            SafeCloseClient(oldClient);
            JoinThread(oldClientThread);
        }

        private void ClientLoop(TcpClient client, string clientEndpoint)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    while (m_IsRunning)
                    {
                        string payload = BoneReceiverPacketReader.ReadOneFrame(stream);
                        if (BoneReceiverPacketReader.TryParseHeartbeat(
                                payload,
                                out string heartbeatSessionId,
                                out long heartbeatSenderUtcTicks,
                                out int heartbeatFrameSerial))
                        {
                            OnHeartbeatReceived(client, clientEndpoint, heartbeatSessionId, heartbeatSenderUtcTicks, heartbeatFrameSerial);
                            continue;
                        }

                        if (BoneReceiverPacketReader.TryParseLog(
                                payload,
                                out string logLevel,
                                out string logSessionId,
                                out long logSenderUtcMilliseconds,
                                out string logMessage))
                        {
                            OnSenderLogReceived(client, clientEndpoint, logLevel, logSessionId, logSenderUtcMilliseconds, logMessage);
                            continue;
                        }

                        long receiveUtcTicks = DateTime.UtcNow.Ticks;
                        lock (m_Lock)
                        {
                            if (!ReferenceEquals(m_CurrentClient, client))
                            {
                                return;
                            }

                            m_LatestFrameJson = payload;
                            m_State.m_Status = "connected";
                            m_State.m_IsListening = true;
                            m_State.m_IsClientConnected = true;
                            m_State.m_ClientCount = 1;
                            m_State.m_LastReceiveUtcTicks = receiveUtcTicks;
                            m_State.m_LastError = string.Empty;
                        }

                        TryLogReceivedFrame(client, clientEndpoint, payload, receiveUtcTicks);
                    }
                }
            }
            catch (EndOfStreamException)
            {
            }
            catch (IOException)
            {
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                SetError("client_loop_failed:" + exception.Message);
            }
            finally
            {
                bool shouldClearCurrentClient;
                lock (m_Lock)
                {
                    shouldClearCurrentClient = ReferenceEquals(m_CurrentClient, client);
                    if (shouldClearCurrentClient)
                    {
                        m_CurrentClient = null;
                        m_ClientThread = null;
                        m_State.m_Status = m_IsRunning ? "listening" : "stopped";
                        m_State.m_IsClientConnected = false;
                        m_State.m_ClientCount = 0;
                        m_State.m_LastClientEndpoint = string.Empty;
                    }
                }

                if (shouldClearCurrentClient)
                {
                    BoneReceiverRuntime.WriteLog("[骨骼接收库] 客户端已断开: " + clientEndpoint);
                }
            }
        }

        private void OnHeartbeatReceived(
            TcpClient client,
            string clientEndpoint,
            string heartbeatSessionId,
            long heartbeatSenderUtcTicks,
            int heartbeatFrameSerial)
        {
            lock (m_Lock)
            {
                if (!ReferenceEquals(m_CurrentClient, client))
                {
                    return;
                }

                m_State.m_Status = "connected";
                m_State.m_IsListening = true;
                m_State.m_IsClientConnected = true;
                m_State.m_ClientCount = 1;
                m_State.m_LastHeartbeatUtcTicks = DateTime.UtcNow.Ticks;
                m_State.m_HeartbeatCount++;
                m_State.m_LastHeartbeatSessionId = heartbeatSessionId ?? string.Empty;
                m_State.m_LastClientEndpoint = clientEndpoint;
                m_State.m_LastError = string.Empty;
            }

            BoneReceiverRuntime.WriteLog(string.Format(
                "[骨骼接收库] 收到心跳: 客户端={0}, 会话={1}, 发送时间戳={2}, 帧序号={3}",
                clientEndpoint,
                heartbeatSessionId ?? string.Empty,
                heartbeatSenderUtcTicks,
                heartbeatFrameSerial));
        }

        private void OnSenderLogReceived(
            TcpClient client,
            string clientEndpoint,
            string logLevel,
            string logSessionId,
            long logSenderUtcMilliseconds,
            string logMessage)
        {
            lock (m_Lock)
            {
                if (!ReferenceEquals(m_CurrentClient, client))
                {
                    return;
                }

                m_State.m_Status = "connected";
                m_State.m_IsListening = true;
                m_State.m_IsClientConnected = true;
                m_State.m_ClientCount = 1;
                m_State.m_LastClientEndpoint = clientEndpoint;
                m_State.m_LastError = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(logMessage))
            {
                BoneReceiverRuntime.WriteLog(logMessage);
                return;
            }

            BoneReceiverRuntime.WriteLog(string.Format(
                "[骨骼接收库] 收到设备日志: 客户端={0}, 会话={1}, 级别={2}, 时间戳={3}",
                clientEndpoint,
                logSessionId ?? string.Empty,
                logLevel ?? string.Empty,
                logSenderUtcMilliseconds));
        }

        private void TryLogReceivedFrame(TcpClient client, string clientEndpoint, string payload, long receiveUtcTicks)
        {
            int receivedFrameCount;
            lock (m_Lock)
            {
                if (!ReferenceEquals(m_CurrentClient, client))
                {
                    return;
                }

                m_ReceivedFrameCount++;
                receivedFrameCount = m_ReceivedFrameCount;
                if (m_NextFrameReceiveLogUtcTicks > 0L && receiveUtcTicks < m_NextFrameReceiveLogUtcTicks)
                {
                    return;
                }

                m_NextFrameReceiveLogUtcTicks = receiveUtcTicks + m_FrameReceiveLogIntervalTicks;
            }

            int payloadByteCount = string.IsNullOrEmpty(payload) ? 0 : Encoding.UTF8.GetByteCount(payload);
            int frameSerial = TryReadFirstJsonInt(payload, "\"m_FrameSerial\":");
            int personCount = CountJsonFieldOccurrences(payload, "\"m_PersonId\":");
            int firstPersonId = TryReadFirstJsonInt(payload, "\"m_PersonId\":");
            BoneReceiverRuntime.WriteLog(string.Format(
                "[骨骼接收库] 收到骨骼帧: 客户端={0}, 累计帧数={1}, 帧={2}, 人数={3}, 首个人物标识={4}, 载荷字节={5}",
                clientEndpoint,
                receivedFrameCount,
                frameSerial == int.MinValue ? "未知" : frameSerial.ToString(),
                personCount,
                firstPersonId == int.MinValue ? "未知" : firstPersonId.ToString(),
                payloadByteCount));
        }

        private void SetError(string error)
        {
            lock (m_Lock)
            {
                m_State.m_LastError = error ?? string.Empty;
                if (m_IsRunning)
                {
                    m_State.m_Status = "listening";
                    m_State.m_IsListening = true;
                }
            }
        }

        private static IPAddress ResolveListenAddress(string host)
        {
            if (string.IsNullOrWhiteSpace(host) || host == "0.0.0.0")
            {
                return IPAddress.Any;
            }

            if (host == "::" || host == "[::]")
            {
                return IPAddress.IPv6Any;
            }

            return IPAddress.Parse(host);
        }

        private static int TryReadFirstJsonInt(string payload, string fieldName)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(fieldName))
            {
                return int.MinValue;
            }

            int startIndex = payload.IndexOf(fieldName, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                return int.MinValue;
            }

            startIndex += fieldName.Length;
            while (startIndex < payload.Length && char.IsWhiteSpace(payload[startIndex]))
            {
                startIndex++;
            }

            int valueStart = startIndex;
            if (startIndex < payload.Length && payload[startIndex] == '-')
            {
                startIndex++;
            }

            while (startIndex < payload.Length && char.IsDigit(payload[startIndex]))
            {
                startIndex++;
            }

            if (startIndex <= valueStart || (startIndex == valueStart + 1 && payload[valueStart] == '-'))
            {
                return int.MinValue;
            }

            int value;
            return int.TryParse(payload.Substring(valueStart, startIndex - valueStart), out value)
                ? value
                : int.MinValue;
        }

        private static int CountJsonFieldOccurrences(string payload, string fieldName)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(fieldName))
            {
                return 0;
            }

            int count = 0;
            int searchIndex = 0;
            while (searchIndex < payload.Length)
            {
                int foundIndex = payload.IndexOf(fieldName, searchIndex, StringComparison.Ordinal);
                if (foundIndex < 0)
                {
                    break;
                }

                count++;
                searchIndex = foundIndex + fieldName.Length;
            }

            return count;
        }

        private static string ReadClientEndpoint(TcpClient client)
        {
            if (client == null)
            {
                return "未知";
            }

            try
            {
                Socket socket = client.Client;
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return "未知";
                }

                return socket.RemoteEndPoint.ToString();
            }
            catch (ObjectDisposedException)
            {
                return "未知";
            }
            catch (SocketException)
            {
                return "未知";
            }
        }

        private static void SafeCloseClient(TcpClient client)
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

                    try
                    {
                        client.Client.Shutdown(SocketShutdown.Both);
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

        private static void JoinThread(Thread thread)
        {
            if (thread == null || !thread.IsAlive || Thread.CurrentThread == thread)
            {
                return;
            }

            try
            {
                thread.Join(500);
            }
            catch
            {
            }
        }
    }
}
