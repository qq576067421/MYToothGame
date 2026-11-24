#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace GameDll
{
    // 编辑器 GM 桥与真机的通信约定：帧 = 4 字节小端长度 + UTF-8 JSON（与骨骼接收库的帧格式一致）。
    // 整条链路只在 Development 构建里编入，正式包不存在这些类型。
    public static class GmBridgeProtocol
    {
        // 骨骼接收库占用 17361，GM 桥用 17362，两条链路可以同时开。
        public const int m_DefaultPort = 17362;
        public const int m_MaxFrameBytes = 256 * 1024;
        public const float m_ConnectTimeoutSecond = 3f;
        public const float m_CommandTimeoutSecond = 3f;
        public const float m_PingIntervalSecond = 2f;
        public const float m_StatePushIntervalSecond = 1f;
        public const float m_ReceiveTimeoutSecond = 8f;
        public const int m_MaxLogEntryCount = 60;

        public const string m_TypeHello = "hello";
        public const string m_TypeHelloAck = "hello.ack";
        public const string m_TypeCmd = "cmd";
        public const string m_TypeAck = "ack";
        public const string m_TypeState = "state";
        public const string m_TypePing = "ping";

        public static long ReadTimestampMs()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }

    // 一条报文。字段按类型取用：hello/hello.ack/cmd/ack/state/ping 共用这一个结构，
    // 用扁平字段而不是嵌套对象，是为了绕开 JsonUtility 不支持字典与多态的限制。
    [Serializable]
    public sealed class GmBridgeMessage
    {
        public string m_Type;
        public int m_Id;
        public string m_Name;
        public string m_ArgsJson;
        public bool m_Ok;
        public string m_Message;
        public string m_DataJson;
        public long m_Ts;

        public string m_Device;
        public string m_App;
        public string m_Unity;
        public bool m_IsDevelopment;
        public string m_Scene;

        public string m_Session;
        public int m_ServerPort;
    }

    // 真机状态快照：编辑器面板只读回显，也作为 state.query 的返回数据。
    [Serializable]
    public sealed class GmBridgeStateDto
    {
        public string m_Scene;
        public string m_Battle;
        public string m_GameMode;
        public int m_Wave;
        public int m_TeamExpPercent;
        public int m_ControlledSeatId;
        public int m_FrozenSeatCount;
        public bool m_IsBattleRunning;
        public bool m_IsPreparePhase;
        public bool m_Paused;
        public bool m_Invincible;
        public bool m_MonsterMoveEnabled;
        public bool m_AutoSkillEnabled;
        public int m_TargetFrameRate;
        public int m_QualityLevel;
        public int m_Fps;
    }

    // log.tail 的返回数据：真机最近日志拼成一段文本。
    [Serializable]
    public sealed class GmBridgeLogTailDto
    {
        public string m_LogText;
    }

    public sealed class GmBridgeCommandResult
    {
        public readonly bool m_Ok;
        public readonly string m_Message;
        public readonly string m_DataJson;

        private GmBridgeCommandResult(bool ok, string message, string dataJson)
        {
            m_Ok = ok;
            m_Message = message ?? string.Empty;
            m_DataJson = dataJson ?? string.Empty;
        }

        public static GmBridgeCommandResult Ok(string message)
        {
            return new GmBridgeCommandResult(true, message, string.Empty);
        }

        public static GmBridgeCommandResult Ok(string message, string dataJson)
        {
            return new GmBridgeCommandResult(true, message, dataJson);
        }

        public static GmBridgeCommandResult Fail(string message)
        {
            return new GmBridgeCommandResult(false, message, string.Empty);
        }
    }
}
#endif
