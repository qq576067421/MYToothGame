using UnityEngine;

namespace GameDll
{
    // 运行时可由 GMTools 调整的骨骼转向参数。
    public static class BoneTurnTuning
    {
        public const float m_DefaultMaxAngle = 35.0f;
        public const bool m_DefaultInvertDirection = false;
        public const float m_DefaultRotationAmplifyFactor = 1.0f;
        public const float m_DefaultShoulderTurnJitterDeadZone = 0.0f;

        public static float m_MaxAngle = m_DefaultMaxAngle;
        public static bool m_InvertDirection = m_DefaultInvertDirection;
        public static float m_RotationAmplifyFactor = m_DefaultRotationAmplifyFactor;
        public static float m_ShoulderTurnJitterDeadZone = m_DefaultShoulderTurnJitterDeadZone;

        // 把运行时调参恢复到默认值。
        public static void ResetDefaults()
        {
            m_MaxAngle = m_DefaultMaxAngle;
            m_InvertDirection = m_DefaultInvertDirection;
            m_RotationAmplifyFactor = m_DefaultRotationAmplifyFactor;
            m_ShoulderTurnJitterDeadZone = m_DefaultShoulderTurnJitterDeadZone;
        }

        // 统一约束运行时可调参数的有效范围，避免异常值直接把转向输出放大失控。
        public static void ClampValues()
        {
            m_MaxAngle = Mathf.Clamp(m_MaxAngle, 0f, 45f);
            m_RotationAmplifyFactor = Mathf.Clamp(m_RotationAmplifyFactor, 0f, 5f);
            m_ShoulderTurnJitterDeadZone = Mathf.Clamp01(m_ShoulderTurnJitterDeadZone);
        }

        // 对外统一读取允许的最大转角。
        public static float ReadClampedMaxAngle()
        {
            return Mathf.Clamp(m_MaxAngle, 0f, 45f);
        }

        // 对外统一读取方向是否反转。
        public static bool ReadClampedInvertDirection()
        {
            return m_InvertDirection;
        }

        // 对外统一读取转向放大量。
        public static float ReadClampedRotationAmplifyFactor()
        {
            return Mathf.Clamp(m_RotationAmplifyFactor, 0f, 5f);
        }

        public static float ReadClampedShoulderTurnJitterDeadZone()
        {
            return Mathf.Clamp01(m_ShoulderTurnJitterDeadZone);
        }
    }
}
