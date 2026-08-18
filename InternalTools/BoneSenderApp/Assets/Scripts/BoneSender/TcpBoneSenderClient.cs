using System;
using System.Net.Sockets;
using System.Text;

namespace BoneSender
{
    public sealed class TcpBoneSenderClient
    {
        private TcpClient m_Client;
        private NetworkStream m_Stream;

        public bool Connect(string host, int port, int timeoutMilliseconds)
        {
            Disconnect();

            try
            {
                m_Client = new TcpClient();
                m_Client.NoDelay = true;
                int resolvedTimeoutMilliseconds = timeoutMilliseconds > 0 ? timeoutMilliseconds : 800;
                m_Client.SendTimeout = resolvedTimeoutMilliseconds;
                m_Client.ReceiveTimeout = resolvedTimeoutMilliseconds;
                IAsyncResult asyncResult = m_Client.BeginConnect(host, port, null, null);
                try
                {
                    if (!asyncResult.AsyncWaitHandle.WaitOne(resolvedTimeoutMilliseconds))
                    {
                        throw new TimeoutException("connect timeout");
                    }

                    m_Client.EndConnect(asyncResult);
                }
                finally
                {
                    asyncResult.AsyncWaitHandle.Close();
                }
                m_Stream = m_Client.GetStream();
                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            Socket socket = TryReadSocket();
            if (socket != null)
            {
                try
                {
                    socket.LingerState = new LingerOption(true, 0);
                }
                catch
                {
                }

                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
            }

            if (m_Stream != null)
            {
                try
                {
                    m_Stream.Close();
                }
                catch
                {
                }

                m_Stream = null;
            }

            if (m_Client != null)
            {
                try
                {
                    m_Client.Close();
                }
                catch
                {
                }

                m_Client = null;
            }
        }

        public bool ReadIsConnected()
        {
            if (m_Client == null || m_Stream == null)
            {
                return false;
            }

            Socket socket = TryReadSocket();
            if (socket == null)
            {
                return false;
            }

            try
            {
                if (!socket.Connected)
                {
                    return false;
                }

                return !(socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        public void SendFrameJson(string json)
        {
            SendPayload(json);
        }

        public void SendPayload(string payloadText)
        {
            if (string.IsNullOrEmpty(payloadText))
            {
                return;
            }

            if (m_Stream == null || !ReadIsConnected())
            {
                throw new InvalidOperationException("sender stream is not connected");
            }

            byte[] payload = Encoding.UTF8.GetBytes(payloadText);
            byte[] length = BitConverter.GetBytes(payload.Length);
            m_Stream.Write(length, 0, length.Length);
            m_Stream.Write(payload, 0, payload.Length);
            m_Stream.Flush();
        }

        private Socket TryReadSocket()
        {
            if (m_Client == null)
            {
                return null;
            }

            try
            {
                return m_Client.Client;
            }
            catch
            {
                return null;
            }
        }
    }
}
