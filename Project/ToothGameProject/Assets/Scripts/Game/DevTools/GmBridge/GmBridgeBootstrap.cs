#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace GameDll
{
    // 真机 GM 桥入口：只有 Development 构建（或编辑器自测开关打开时）才创建客户端。
    // 正式包里 GmBridgeSettings.ReadIsEnabled 永远返回 false，且整个文件被编译剔除。
    public static class GmBridgeBootstrap
    {
        private const string m_ClientObjectName = "GmBridgeClient";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (!GmBridgeSettings.ReadIsEnabled(out string host, out int port))
            {
                return;
            }

            // 先挂日志回调，才能把启动阶段的报错也通过 log.tail 拉回来。
            GmBridgeLogBuffer.EnsureHooked();

            var clientObject = new GameObject(m_ClientObjectName);
            Object.DontDestroyOnLoad(clientObject);
            var client = clientObject.AddComponent<GmBridgeClient>();
            client.Init(host, port);
            Debug.Log(string.Format("[GM桥] 客户端已启动，目标 {0}:{1}", host, port));
        }
    }
}
#endif
