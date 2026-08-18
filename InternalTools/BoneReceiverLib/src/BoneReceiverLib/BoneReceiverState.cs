namespace CompanyInternalTools.BoneReceiverLib
{
    public sealed class BoneReceiverState
    {
        public string m_Status = "stopped";
        public bool m_IsListening;
        public bool m_IsClientConnected;
        public int m_ClientCount;
        public long m_LastReceiveUtcTicks;
        public long m_LastHeartbeatUtcTicks;
        public int m_HeartbeatCount;
        public string m_LastHeartbeatSessionId = string.Empty;
        public string m_LastClientEndpoint = string.Empty;
        public string m_LastError = string.Empty;

        public BoneReceiverState Clone()
        {
            return new BoneReceiverState
            {
                m_Status = m_Status,
                m_IsListening = m_IsListening,
                m_IsClientConnected = m_IsClientConnected,
                m_ClientCount = m_ClientCount,
                m_LastReceiveUtcTicks = m_LastReceiveUtcTicks,
                m_LastHeartbeatUtcTicks = m_LastHeartbeatUtcTicks,
                m_HeartbeatCount = m_HeartbeatCount,
                m_LastHeartbeatSessionId = m_LastHeartbeatSessionId,
                m_LastClientEndpoint = m_LastClientEndpoint,
                m_LastError = m_LastError,
            };
        }
    }
}
