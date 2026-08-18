#if UNITY_EDITOR && BoneReceiverLib
using UnityEditor;

namespace GameDll
{
    [InitializeOnLoad]
    public static class BoneRemoteDebugLifecycleBridge
    {
        static BoneRemoteDebugLifecycleBridge()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += OnEditorQuitting;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                RemoteBoneFrameSourceProxy.TryForceStopListener("退出播放模式");
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            RemoteBoneFrameSourceProxy.TryForceStopListener("脚本域重载");
        }

        private static void OnEditorQuitting()
        {
            RemoteBoneFrameSourceProxy.TryForceStopListener("关闭编辑器");
        }
    }
}
#endif
