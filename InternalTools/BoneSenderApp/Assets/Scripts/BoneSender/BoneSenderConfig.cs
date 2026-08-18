using System;
using UnityEngine;

namespace BoneSender
{
    [Serializable]
    public sealed class BoneSenderConfig
    {
        public string m_TargetHost = "192.168.1.29";
        public string[] m_TargetHosts = new[] { "192.168.1.29" };
        public int m_TargetPort = 17361;
        public int m_SendFps = 20;
        public float m_ReconnectDelaySeconds = 2f;
        public int m_ConnectTimeoutMilliseconds = 800;
        public bool m_AutoStartOnEnable = true;
    }
}
