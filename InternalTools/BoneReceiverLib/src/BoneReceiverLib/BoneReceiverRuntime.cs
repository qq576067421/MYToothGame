using System;
using System.Text;

namespace CompanyInternalTools.BoneReceiverLib
{
    public static class BoneReceiverRuntime
    {
        private static readonly BoneReceiverServer m_Server = new BoneReceiverServer();
        private static Action<string> m_LogHandler;

        public static void StartListener(string host, int port)
        {
            m_Server.Start(host, port);
        }

        public static void StopListener()
        {
            m_Server.Stop();
        }

        public static string ReadStatusJson()
        {
            return BuildStatusJson(m_Server.ReadState());
        }

        public static string TryReadLatestFrameJson()
        {
            return m_Server.TryReadLatestFrameJson();
        }

        public static void RegisterLogger(Action<string> logHandler)
        {
            m_LogHandler = logHandler;
        }

        internal static void WriteLog(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Action<string> logHandler = m_LogHandler;
            if (logHandler != null)
            {
                try
                {
                    logHandler(message);
                    return;
                }
                catch
                {
                }
            }

            Console.WriteLine(message);
        }

        private static string BuildStatusJson(BoneReceiverState state)
        {
            if (state == null)
            {
                return "{}";
            }

            var builder = new StringBuilder(160);
            builder.Append('{');
            AppendString(builder, "m_Status", state.m_Status);
            builder.Append(',');
            AppendBool(builder, "m_IsListening", state.m_IsListening);
            builder.Append(',');
            AppendBool(builder, "m_IsClientConnected", state.m_IsClientConnected);
            builder.Append(',');
            AppendInt(builder, "m_ClientCount", state.m_ClientCount);
            builder.Append(',');
            AppendLong(builder, "m_LastReceiveUtcTicks", state.m_LastReceiveUtcTicks);
            builder.Append(',');
            AppendLong(builder, "m_LastHeartbeatUtcTicks", state.m_LastHeartbeatUtcTicks);
            builder.Append(',');
            AppendInt(builder, "m_HeartbeatCount", state.m_HeartbeatCount);
            builder.Append(',');
            AppendString(builder, "m_LastHeartbeatSessionId", state.m_LastHeartbeatSessionId);
            builder.Append(',');
            AppendString(builder, "m_LastClientEndpoint", state.m_LastClientEndpoint);
            builder.Append(',');
            AppendString(builder, "m_LastError", state.m_LastError);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendString(StringBuilder builder, string key, string value)
        {
            builder.Append('"').Append(key).Append("\":");
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char one = value[i];
                switch (one)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(one);
                        break;
                }
            }

            builder.Append('"');
        }

        private static void AppendBool(StringBuilder builder, string key, bool value)
        {
            builder.Append('"').Append(key).Append("\":").Append(value ? "true" : "false");
        }

        private static void AppendInt(StringBuilder builder, string key, int value)
        {
            builder.Append('"').Append(key).Append("\":").Append(value);
        }

        private static void AppendLong(StringBuilder builder, string key, long value)
        {
            builder.Append('"').Append(key).Append("\":").Append(value);
        }
    }
}
