#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{
    // 真机侧最近日志环形缓冲：给 GM 桥的 log.tail 用，避免每次定位问题都要连 profiler。
    // 只在 Development 构建里编译，正式包不会挂日志回调。
    public static class GmBridgeLogBuffer
    {
        private const int m_Capacity = 200;

        private static readonly List<string> m_Entries = new List<string>(m_Capacity);
        private static readonly object m_Lock = new object();
        private static bool m_HasHooked;

        public static void EnsureHooked()
        {
            if (m_HasHooked)
            {
                return;
            }

            m_HasHooked = true;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        public static string ReadTailText(int count)
        {
            if (count <= 0)
            {
                count = 30;
            }

            lock (m_Lock)
            {
                int start = Mathf.Max(0, m_Entries.Count - count);
                var builder = new StringBuilder(256);
                for (int i = start; i < m_Entries.Count; i++)
                {
                    builder.AppendLine(m_Entries[i]);
                }

                return builder.ToString();
            }
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return;
            }

            // 回调可能在任意线程触发，只做加锁入队，不碰 Unity 对象。
            string line = type == LogType.Log ? condition : "[" + type + "] " + condition;
            lock (m_Lock)
            {
                m_Entries.Add(line);
                while (m_Entries.Count > m_Capacity)
                {
                    m_Entries.RemoveAt(0);
                }
            }
        }
    }
}
#endif
