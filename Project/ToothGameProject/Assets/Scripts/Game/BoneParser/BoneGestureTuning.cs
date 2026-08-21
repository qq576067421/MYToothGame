using UnityEngine;

namespace GameDll
{
    // Unity 工程内统一维护骨骼识别参数。每个姿势或流程必须使用独立参数组，禁止相互借用。
    public static class BoneGestureTuning
    {
        // 左右交替挥击_流程（ID 2001）的运行时参数。
        public static readonly AlternatingSwing2001Tuning m_AlternatingSwing2001 =
            new AlternatingSwing2001Tuning();

        internal static void ApplyTo(BoneParserConfig config)
        {
            m_AlternatingSwing2001.ApplyTo(config);
        }
    }

    public sealed class AlternatingSwing2001Tuning
    {
        // 单次挥击的最低速度，单位为每秒移动的肩宽倍数。调小更容易触发，调大可过滤缓慢摆动。
        public float m_SpeedRatioPerSecond = 1.20f;

        // 单次挥击的最低垂直位移，单位为肩宽倍数。调小可降低动作幅度要求，调大会要求更完整的挥击。
        public float m_MinVerticalDistanceRatio = 0.12f;

        // 每帧方向变化的忽略范围，单位为肩宽倍数。调小会更灵敏，调大可过滤手腕轻微抖动。
        public float m_DirectionNoiseRatio = 0.015f;

        // 单次挥击必须连续保持同一方向的帧数，最小有效值为 2。调小更容易触发，调大可过滤瞬间抖动。
        public int m_MinDirectionalFrames = 2;

        // 同一只手两次有效挥击之间的最短间隔，单位为秒。调小允许更快连续挥击，调大可限制重复计数。
        public float m_CooldownSeconds = 0.45f;

        // 左右手两次相反方向挥击允许间隔的最大帧数。调大可放宽完成时间，调小会要求更快交替。
        public int m_WindowFrames = 24;

        internal void ApplyTo(BoneParserConfig config)
        {
            config.m_AlternatingSwingSpeedRatioPerSecond = Mathf.Max(0f, m_SpeedRatioPerSecond);
            config.m_AlternatingSwingMinVerticalDistanceRatio = Mathf.Max(0f, m_MinVerticalDistanceRatio);
            config.m_AlternatingSwingDirectionNoiseRatio = Mathf.Max(0f, m_DirectionNoiseRatio);
            config.m_AlternatingSwingMinDirectionalFrames = Mathf.Max(2, m_MinDirectionalFrames);
            config.m_AlternatingSwingCooldownSeconds = Mathf.Max(0f, m_CooldownSeconds);
            config.m_AlternatingSwingWindowFrames = Mathf.Max(1, m_WindowFrames);
        }
    }
}
