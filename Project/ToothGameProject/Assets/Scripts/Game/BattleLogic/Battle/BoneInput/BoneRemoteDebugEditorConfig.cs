using UnityEngine;

namespace GameDll
{
    public static class BoneRemoteDebugEditorConfig
    {
        public const string m_EnableKey = "BoneRemoteDebug.Enable";
        public const string m_PortKey = "BoneRemoteDebug.Port";
        public const string m_KeyboardControlEnableKey = "BoneRemoteDebug.KeyboardControl.Enable";
        public const string m_BoneControlEnableKey = "BoneRemoteDebug.BoneControl.Enable";
        public const string m_BoneDebugSkeletonOverlayEnableKey = "BoneRemoteDebug.BoneDebugSkeletonOverlay.Enable";
        public const int m_DefaultPort = 17361;

        // 战斗进行期间统一屏蔽骨骼可视化，避免真机相机层和场景调试覆盖层分别失控。
        private static bool m_IsBattleSkeletonDisplaySuppressed;

        public static bool ReadIsRemoteEnabled()
        {
#if UNITY_EDITOR && BoneReceiverLib
            return UnityEditor.EditorPrefs.GetBool(m_EnableKey, false);
#else
            return false;
#endif
        }

        public static int ReadPort()
        {
#if UNITY_EDITOR
            return ClampPort(UnityEditor.EditorPrefs.GetInt(m_PortKey, m_DefaultPort));
#else
            return m_DefaultPort;
#endif
        }

        public static bool ReadIsKeyboardControlEnabled()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(m_KeyboardControlEnableKey, true);
#else
            return true;
#endif
        }

        public static bool ReadIsBoneControlEnabled()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(m_BoneControlEnableKey, true);
#else
            return true;
#endif
        }

        public static bool ReadIsBoneDebugSkeletonOverlayEnabled()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(m_BoneDebugSkeletonOverlayEnableKey, false);
#else
            return Debug.isDebugBuild;
#endif
        }

        public static bool ReadShouldDrawBattleSkeleton()
        {
            return !m_IsBattleSkeletonDisplaySuppressed;
        }

        public static bool ReadShouldDrawBattleSkeletonOverlay()
        {
            return ReadShouldDrawBattleSkeleton() && ReadIsBoneDebugSkeletonOverlayEnabled();
        }

        public static void SetRemoteEnabled(bool isEnabled)
        {
#if UNITY_EDITOR && BoneReceiverLib
            UnityEditor.EditorPrefs.SetBool(m_EnableKey, isEnabled);
            if (!isEnabled)
            {
                RemoteBoneFrameSourceProxy.TryForceStopListener("关闭远程调试开关");
            }
#endif
        }

        public static void SetKeyboardControlEnabled(bool isEnabled)
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(m_KeyboardControlEnableKey, isEnabled);
#endif
        }

        public static void SetBoneControlEnabled(bool isEnabled)
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(m_BoneControlEnableKey, isEnabled);
#endif
        }

        public static void SetBoneDebugSkeletonOverlayEnabled(bool isEnabled)
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(m_BoneDebugSkeletonOverlayEnableKey, isEnabled);
#endif
        }

        public static void SetBattleSkeletonDisplaySuppressed(bool isSuppressed)
        {
            m_IsBattleSkeletonDisplaySuppressed = isSuppressed;
        }

        public static void SetPort(int port)
        {
#if UNITY_EDITOR
            int oldPort = ReadPort();
            int resolvedPort = ClampPort(port);
            UnityEditor.EditorPrefs.SetInt(m_PortKey, resolvedPort);
#if BoneReceiverLib
            if (resolvedPort != oldPort)
            {
                RemoteBoneFrameSourceProxy.TryForceStopListener("更新远程调试端口");
            }
#endif
#endif
        }

        private static int ClampPort(int port)
        {
            if (port < 1)
            {
                return m_DefaultPort;
            }

            return port > 65535 ? 65535 : port;
        }
    }
}
