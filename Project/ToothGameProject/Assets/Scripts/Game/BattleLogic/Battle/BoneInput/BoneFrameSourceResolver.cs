namespace GameDll
{
    public sealed class BoneFrameSourceResolver : IBoneFrameSource
    {
        private readonly BattleBoneParseData m_LocalSource;
#if BoneReceiverLib
        private RemoteBoneFrameSourceProxy m_RemoteSource;
        private bool m_LastRemoteEnabled;
        private int m_LastRemotePort;
        private bool m_HasAttemptedRemoteCreate;
#endif

        public BoneFrameSourceResolver(BattleBoneParseData localSource)
        {
            m_LocalSource = localSource;
#if BoneReceiverLib
            m_LastRemoteEnabled = RemoteBoneFrameSourceProxy.ReadIsFeatureEnabled();
            m_LastRemotePort = BoneRemoteDebugEditorConfig.ReadPort();
            if (m_LastRemoteEnabled)
            {
                TryCreateRemoteSource();
            }
#endif
        }

        public string ReadSourceName()
        {
#if BoneReceiverLib
            if (m_RemoteSource != null && m_RemoteSource.ReadIsActive())
            {
                return "remote_debug";
            }
#endif

            return m_LocalSource != null ? m_LocalSource.ReadSourceName() : "none";
        }

        public void Tick()
        {
            m_LocalSource?.Tick();
#if BoneReceiverLib
            RefreshRemoteSourceState();
            m_RemoteSource?.Tick();
#endif
        }

        public BoneFrameData ReadLatestFrameData()
        {
#if BoneReceiverLib
            if (m_RemoteSource != null && m_RemoteSource.TryReadLatestFrame(out var remoteFrame))
            {
                return remoteFrame;
            }
#endif

            return m_LocalSource != null ? m_LocalSource.ReadLatestFrameData() : null;
        }

        public void Shutdown()
        {
#if BoneReceiverLib
            m_RemoteSource?.Shutdown();
            m_RemoteSource = null;
#endif
            m_LocalSource?.Shutdown();
        }

#if BoneReceiverLib
        private void RefreshRemoteSourceState()
        {
            bool isRemoteEnabled = RemoteBoneFrameSourceProxy.ReadIsFeatureEnabled();
            int remotePort = BoneRemoteDebugEditorConfig.ReadPort();
            if (isRemoteEnabled != m_LastRemoteEnabled || remotePort != m_LastRemotePort)
            {
                m_LastRemoteEnabled = isRemoteEnabled;
                m_LastRemotePort = remotePort;
                m_HasAttemptedRemoteCreate = false;
                if (m_RemoteSource != null)
                {
                    m_RemoteSource.Shutdown();
                    m_RemoteSource = null;
                }
            }

            if (!isRemoteEnabled || m_RemoteSource != null || m_HasAttemptedRemoteCreate)
            {
                return;
            }

            TryCreateRemoteSource();
        }

        private void TryCreateRemoteSource()
        {
            m_HasAttemptedRemoteCreate = true;
            m_RemoteSource = RemoteBoneFrameSourceProxy.TryCreate();
        }
#endif
    }
}
