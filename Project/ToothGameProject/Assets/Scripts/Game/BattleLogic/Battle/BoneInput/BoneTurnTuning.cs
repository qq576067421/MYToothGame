using UnityEngine;

namespace GameDll
{
    // 运行时可由 GMTools 调整的骨骼转向参数。
    public static class BoneTurnTuning
    {
        public const float m_DefaultMaxAngle = 35.0f;
        public const bool m_DefaultInvertDirection = false;
        public const float m_DefaultRotationAmplifyFactor = 1.0f;

        public static float m_MaxAngle = m_DefaultMaxAngle;
        public static bool m_InvertDirection = m_DefaultInvertDirection;
        public static float m_RotationAmplifyFactor = m_DefaultRotationAmplifyFactor;

        // 把运行时调参恢复到默认值。
        public static void ResetDefaults()
        {
            m_MaxAngle = m_DefaultMaxAngle;
            m_InvertDirection = m_DefaultInvertDirection;
            m_RotationAmplifyFactor = m_DefaultRotationAmplifyFactor;
        }

        // 统一约束运行时可调参数的有效范围，避免异常值直接把转向输出放大失控。
        public static void ClampValues()
        {
            m_MaxAngle = Mathf.Clamp(m_MaxAngle, 0f, 45f);
            m_RotationAmplifyFactor = Mathf.Clamp(m_RotationAmplifyFactor, 0f, 5f);
        }

        // 对外统一读取本轮允许的最大转角。
        public static float ReadClampedMaxAngle()
        {
            return Mathf.Clamp(m_MaxAngle, 0f, 45f);
        }

        // 对外统一读取双肩鼻子旋转法的角度放大量，1 表示保持原始输出。
        public static float ReadClampedRotationAmplifyFactor()
        {
            return Mathf.Clamp(m_RotationAmplifyFactor, 0f, 5f);
        }
    }
}
