#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GameDll
{
    public enum GmBridgeControlTarget
    {
        Local = 0,
        Device = 1,
        Both = 2,
    }

    // GM 命令分发入口：决定走本机 Play 模式、真机，还是两边都发。
    public static class GmBridgeEditorRouter
    {
        private const string m_TargetKey = "GmBridge.Target";

        public static GmBridgeControlTarget ReadTarget()
        {
            int target = EditorPrefs.GetInt(m_TargetKey, (int)GmBridgeControlTarget.Device);
            if (target < (int)GmBridgeControlTarget.Local || target > (int)GmBridgeControlTarget.Both)
            {
                return GmBridgeControlTarget.Device;
            }

            return (GmBridgeControlTarget)target;
        }

        public static void SetTarget(GmBridgeControlTarget target)
        {
            EditorPrefs.SetInt(m_TargetKey, (int)target);
        }

        public static void Invoke(string name, string argsJson)
        {
            GmBridgeControlTarget target = ReadTarget();
            if (target == GmBridgeControlTarget.Local || target == GmBridgeControlTarget.Both)
            {
                InvokeLocal(name, argsJson);
            }

            if (target == GmBridgeControlTarget.Device || target == GmBridgeControlTarget.Both)
            {
                if (GmBridgeEditorServer.TrySendCommand(name, argsJson))
                {
                    return;
                }

                if (target == GmBridgeControlTarget.Device)
                {
                    // 没连真机时退回本机，避免本地 Play 联调时按钮看起来没反应。
                    Debug.LogWarning("[GM桥] 未连接真机，命令改在本机执行：" + name);
                    InvokeLocal(name, argsJson);
                }
            }
        }

        private static void InvokeLocal(string name, string argsJson)
        {
            GmBridgeCommandResult result = GmBridgeCommandDispatcher.Execute(name, argsJson);
            if (result.m_Ok)
            {
                Debug.Log("[GM桥] 本机执行 " + name + (string.IsNullOrEmpty(result.m_Message) ? string.Empty : "：" + result.m_Message));
                return;
            }

            Debug.LogWarning("[GM桥] 本机执行 " + name + " 失败：" + result.m_Message);
        }
    }
}
#endif
