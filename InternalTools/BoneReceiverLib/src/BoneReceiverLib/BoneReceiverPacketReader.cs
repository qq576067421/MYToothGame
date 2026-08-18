using System;
using System.IO;
using System.Text;

namespace CompanyInternalTools.BoneReceiverLib
{
    public static class BoneReceiverPacketReader
    {
        private const string m_HeartbeatPrefix = "BONE_HEARTBEAT|";
        private const string m_LogPrefix = "BONE_LOG|";
        private const int m_MaxFrameLength = 2 * 1024 * 1024;

        public static string ReadOneFrame(Stream stream)
        {
            byte[] header = ReadExact(stream, 4);
            int length = BitConverter.ToInt32(header, 0);
            if (length <= 0 || length > m_MaxFrameLength)
            {
                throw new InvalidDataException("invalid frame length");
            }

            byte[] payload = ReadExact(stream, length);
            return Encoding.UTF8.GetString(payload);
        }

        public static bool TryParseHeartbeat(
            string payload,
            out string sessionId,
            out long senderUtcTicks,
            out int senderFrameSerial)
        {
            sessionId = string.Empty;
            senderUtcTicks = 0L;
            senderFrameSerial = 0;

            if (string.IsNullOrEmpty(payload) || !payload.StartsWith(m_HeartbeatPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = payload.Split('|');
            if (parts.Length != 4)
            {
                return false;
            }

            if (!long.TryParse(parts[2], out senderUtcTicks))
            {
                return false;
            }

            if (!int.TryParse(parts[3], out senderFrameSerial))
            {
                return false;
            }

            sessionId = parts[1] ?? string.Empty;
            return true;
        }

        public static bool TryParseLog(
            string payload,
            out string level,
            out string sessionId,
            out long senderUtcMilliseconds,
            out string message)
        {
            level = string.Empty;
            sessionId = string.Empty;
            senderUtcMilliseconds = 0L;
            message = string.Empty;

            if (string.IsNullOrEmpty(payload) || !payload.StartsWith(m_LogPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = payload.Split(new[] { '|' }, 5);
            if (parts.Length != 5)
            {
                return false;
            }

            if (!long.TryParse(parts[3], out senderUtcMilliseconds))
            {
                return false;
            }

            try
            {
                byte[] decodedBytes = Convert.FromBase64String(parts[4]);
                message = Encoding.UTF8.GetString(decodedBytes);
            }
            catch (FormatException)
            {
                return false;
            }

            level = parts[1] ?? string.Empty;
            sessionId = parts[2] ?? string.Empty;
            return true;
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    throw new EndOfStreamException();
                }

                offset += read;
            }

            return buffer;
        }
    }
}
