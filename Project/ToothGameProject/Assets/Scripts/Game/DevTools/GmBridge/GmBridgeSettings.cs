#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace GameDll
{
    // 真机侧连接配置。故意不做自动发现：默认直连开发机的局域网 IP，
    // 换电脑或换 IP 时只改 m_DefaultHost；真机上可用覆盖文件临时切换，不用重新打包。
    public static class GmBridgeSettings
    {
        // 开发机局域网 IP。换机器只改这一行，也可以在 GMTools「战斗参数」页签里直接回填。
        public const string m_DefaultHost = "192.168.1.29";
        public const int m_DefaultPort = GmBridgeProtocol.m_DefaultPort;

        // 覆盖文件放 Application.persistentDataPath，第一行 IP，第二行可写端口。
        // 示例：adb push GmBridgeHost.txt /sdcard/Android/data/<包名>/files/
        private const string m_OverrideFileName = "GmBridgeHost.txt";

#if UNITY_EDITOR
        // 没有真机时用它验证链路：勾选后编辑器 Play 模式下的客户端连 127.0.0.1。
        public const string m_EditorSelfTestKey = "GmBridge.SelfTest";
#endif

        public static bool ReadIsEnabled(out string host, out int port)
        {
            host = m_DefaultHost;
            port = ClampPort(m_DefaultPort);

#if UNITY_EDITOR
            // 编辑器只在显式打开自测开关时扮演“真机”，避免平时 Play 模式自己连自己。
            if (!UnityEditor.EditorPrefs.GetBool(m_EditorSelfTestKey, false))
            {
                return false;
            }

            host = "127.0.0.1";
            return true;
#else
            if (!Debug.isDebugBuild)
            {
                return false;
            }

            TryApplyOverrideFile(ref host, ref port);
            return !string.IsNullOrEmpty(host) && port > 0;
#endif
        }

        private static void TryApplyOverrideFile(ref string host, ref int port)
        {
            string filePath;
            try
            {
                filePath = Path.Combine(Application.persistentDataPath, m_OverrideFileName);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                if (lines.Length <= 0 || string.IsNullOrWhiteSpace(lines[0]))
                {
                    return;
                }

                host = lines[0].Trim();
                if (lines.Length > 1 && int.TryParse(lines[1].Trim(), out int overridePort))
                {
                    port = ClampPort(overridePort);
                }
            }
            catch (Exception)
            {
                // 覆盖文件读失败就继续用默认值，不能因为调试配置让游戏起不来。
            }
        }

        private static int ClampPort(int port)
        {
            if (port <= 0 || port > 65535)
            {
                return GmBridgeProtocol.m_DefaultPort;
            }

            return port;
        }
    }
}
#endif
