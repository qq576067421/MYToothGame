#if UNITY_EDITOR
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEditor;

namespace GameDll
{
    // GMTools 的 GM 桥配置：这里只存编辑器侧的开关与端口。
    // 真机连的 IP 写在 GmBridgeSettings.m_DefaultHost（故意不做自动发现，换 IP 手动改）。
    public static class GmBridgeEditorSettings
    {
        private const string m_EnableKey = "GmBridge.Enable";
        private const string m_PortKey = "GmBridge.Port";

        public static bool ReadIsEnabled()
        {
            return EditorPrefs.GetBool(m_EnableKey, false);
        }

        public static void SetIsEnabled(bool isEnabled)
        {
            EditorPrefs.SetBool(m_EnableKey, isEnabled);
        }

        public static int ReadPort()
        {
            int port = EditorPrefs.GetInt(m_PortKey, GmBridgeProtocol.m_DefaultPort);
            if (port <= 0 || port > 65535)
            {
                return GmBridgeProtocol.m_DefaultPort;
            }

            return port;
        }

        public static void SetPort(int port)
        {
            if (port <= 0 || port > 65535)
            {
                port = GmBridgeProtocol.m_DefaultPort;
            }

            EditorPrefs.SetInt(m_PortKey, port);
        }

        // 面板展示用：优先取带默认网关的网卡地址，避免把虚拟机/回环网卡当成真机要连的 IP。
        public static string ReadLocalIpv4()
        {
            try
            {
                string fallback = string.Empty;
                foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        networkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    IPInterfaceProperties properties = networkInterface.GetIPProperties();
                    bool hasGateway = properties.GatewayAddresses != null && properties.GatewayAddresses.Count > 0;
                    foreach (UnicastIPAddressInformation addressInfo in properties.UnicastAddresses)
                    {
                        if (addressInfo.Address.AddressFamily != AddressFamily.InterNetwork)
                        {
                            continue;
                        }

                        string ip = addressInfo.Address.ToString();
                        if (ip.StartsWith("127.") || ip.StartsWith("169.254."))
                        {
                            continue;
                        }

                        if (hasGateway)
                        {
                            return ip;
                        }

                        if (string.IsNullOrEmpty(fallback))
                        {
                            fallback = ip;
                        }
                    }
                }

                return fallback;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
#endif
