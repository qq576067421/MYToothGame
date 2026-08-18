using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BoneSender
{
    public static class BoneSenderAppLogger
    {
        private const string m_LogPayloadPrefix = "BONE_LOG|";
        private const string m_LogPrefix = "[骨骼采集设备日志] ";
        private const int m_MaxPendingPayloadCount = 256;

        private static readonly object m_Lock = new object();
        private static readonly Queue<string> m_PendingPayloads = new Queue<string>(64);

        private static Func<string> m_SessionIdReader;

        public static void BindSessionIdReader(Func<string> sessionIdReader)
        {
            lock (m_Lock)
            {
                m_SessionIdReader = sessionIdReader;
            }
        }

        public static void ClearPendingPayloads()
        {
            lock (m_Lock)
            {
                m_PendingPayloads.Clear();
            }
        }

        public static void Log(string message)
        {
            Write("信息", message, Debug.Log);
        }

        public static void LogWarning(string message)
        {
            Write("警告", message, Debug.LogWarning);
        }

        public static void LogError(string message)
        {
            Write("错误", message, Debug.LogError);
        }

        public static bool TryDequeuePayload(out string payload)
        {
            lock (m_Lock)
            {
                if (m_PendingPayloads.Count <= 0)
                {
                    payload = null;
                    return false;
                }

                payload = m_PendingPayloads.Dequeue();
                return true;
            }
        }

        private static void Write(string level, string message, Action<object> unityWriter)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string formattedMessage = m_LogPrefix + "[" + level + "] " + message.Trim();
            unityWriter?.Invoke(formattedMessage);

            lock (m_Lock)
            {
                if (m_PendingPayloads.Count >= m_MaxPendingPayloadCount)
                {
                    m_PendingPayloads.Dequeue();
                }

                m_PendingPayloads.Enqueue(BuildPayload(level, formattedMessage));
            }
        }

        private static string BuildPayload(string level, string formattedMessage)
        {
            string sessionId = "unknown";
            if (m_SessionIdReader != null)
            {
                try
                {
                    string resolvedSessionId = m_SessionIdReader();
                    if (!string.IsNullOrWhiteSpace(resolvedSessionId))
                    {
                        sessionId = resolvedSessionId.Trim();
                    }
                }
                catch
                {
                }
            }

            string encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(formattedMessage));
            return string.Format(
                "{0}{1}|{2}|{3}|{4}",
                m_LogPayloadPrefix,
                string.IsNullOrWhiteSpace(level) ? "信息" : level,
                sessionId,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                encodedMessage);
        }
    }
}
