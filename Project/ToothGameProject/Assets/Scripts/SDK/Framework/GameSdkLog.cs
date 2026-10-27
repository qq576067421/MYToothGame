// [新SDK整合] 日志统一入口。
// 默认关闭；联调时把 Enabled 改成 true 即可恢复，无需逐个改回 Debug.Log。
public static class GameSdkLog
{
    public static bool Enabled;

    public static void Info(string message)
    {
        if (!Enabled)
        {
            return;
        }

        UnityEngine.Debug.Log(message);
    }

    public static void Info(string message, UnityEngine.Object context)
    {
        if (!Enabled)
        {
            return;
        }

        UnityEngine.Debug.Log(message, context);
    }
}
