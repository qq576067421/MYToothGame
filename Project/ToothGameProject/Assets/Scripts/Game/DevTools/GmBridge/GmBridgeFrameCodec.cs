#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Text;

namespace GameDll
{
    // GM 桥报文编解码：4 字节小端长度前缀 + UTF-8 JSON，收发两端共用。
    public static class GmBridgeFrameCodec
    {
        private const int m_HeaderByteCount = 4;

        public static byte[] EncodeFrame(string payload)
        {
            byte[] body = Encoding.UTF8.GetBytes(payload ?? string.Empty);
            if (body.Length <= 0 || body.Length > GmBridgeProtocol.m_MaxFrameBytes)
            {
                throw new InvalidDataException("GM 桥报文长度非法：" + body.Length);
            }

            byte[] header = BitConverter.GetBytes(body.Length);
            byte[] frame = new byte[header.Length + body.Length];
            Buffer.BlockCopy(header, 0, frame, 0, header.Length);
            Buffer.BlockCopy(body, 0, frame, header.Length, body.Length);
            return frame;
        }

        public static void WriteFrame(Stream stream, string payload)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            byte[] frame = EncodeFrame(payload);
            stream.Write(frame, 0, frame.Length);
            stream.Flush();
        }

        public static string ReadFrame(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            byte[] header = ReadExact(stream, m_HeaderByteCount);
            int length = BitConverter.ToInt32(header, 0);
            if (length <= 0 || length > GmBridgeProtocol.m_MaxFrameBytes)
            {
                throw new InvalidDataException("GM 桥报文长度非法：" + length);
            }

            return Encoding.UTF8.GetString(ReadExact(stream, length));
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
#endif
