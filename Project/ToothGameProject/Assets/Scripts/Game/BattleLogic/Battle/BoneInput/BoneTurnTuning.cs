using MonoBean;
using UnityEngine;

namespace GameDll
{
    // 转向只使用双肩连线与屏幕水平线的夹角，所有运行参数均来自全局表。
    public static class BoneTurnTuning
    {
        private const int m_ShoulderAngleFullInputGlobalConfigId = 100314;
        private const int m_ShoulderAngleMaxOutputGlobalConfigId = 100315;
        private const int m_ShoulderAngleInvertDirectionGlobalConfigId = 100316;
        private const int m_ShoulderAngleSmoothTimeGlobalConfigId = 100317;
        private const int m_ShoulderAngleInputJitterGlobalConfigId = 100318;
        private const int m_ShoulderAngleMaxSpeedGlobalConfigId = 100319;
        private const int m_ShoulderAngleMinSpanGlobalConfigId = 100322;
        private const float m_GlobalRatioScale = 1000f;

        public const bool m_DefaultBlockNormalAttackWhileTurning = false;
        public static bool m_BlockNormalAttackWhileTurning = m_DefaultBlockNormalAttackWhileTurning;
        public static readonly ShoulderHorizontalAngleTurnTuning m_ShoulderAngleTurn =
            new ShoulderHorizontalAngleTurnTuning();

        public static void ResetDefaults()
        {
            m_BlockNormalAttackWhileTurning = m_DefaultBlockNormalAttackWhileTurning;
            m_ShoulderAngleTurn.ResetDefaults();
        }

        public static void ClampValues()
        {
            m_ShoulderAngleTurn.ClampValues();
        }

        public static void ApplyTo(BoneParserConfig config)
        {
            if (config == null)
            {
                return;
            }

            var fullInputConfig = t_globalBean.GetConfig(m_ShoulderAngleFullInputGlobalConfigId);
            var maxOutputConfig = t_globalBean.GetConfig(m_ShoulderAngleMaxOutputGlobalConfigId);
            var invertDirectionConfig = t_globalBean.GetConfig(m_ShoulderAngleInvertDirectionGlobalConfigId);
            var smoothTimeConfig = t_globalBean.GetConfig(m_ShoulderAngleSmoothTimeGlobalConfigId);
            var inputJitterConfig = t_globalBean.GetConfig(m_ShoulderAngleInputJitterGlobalConfigId);
            var maxSpeedConfig = t_globalBean.GetConfig(m_ShoulderAngleMaxSpeedGlobalConfigId);
            var minSpanConfig = t_globalBean.GetConfig(m_ShoulderAngleMinSpanGlobalConfigId);
            if (fullInputConfig == null || maxOutputConfig == null ||
                invertDirectionConfig == null || smoothTimeConfig == null || inputJitterConfig == null ||
                maxSpeedConfig == null || minSpanConfig == null)
            {
                Debug.LogError("侧身转向全局表配置不完整，请检查 100314 至 100319 以及 100322。");
                return;
            }

            m_ShoulderAngleTurn.m_FullInputDegrees = fullInputConfig.t_int / m_GlobalRatioScale;
            m_ShoulderAngleTurn.m_MaxOutputAngleDegrees = maxOutputConfig.t_int;
            m_ShoulderAngleTurn.m_InvertDirection = invertDirectionConfig.t_int != 0;
            m_ShoulderAngleTurn.m_SmoothTimeSeconds = smoothTimeConfig.t_int / m_GlobalRatioScale;
            m_ShoulderAngleTurn.m_InputJitterDegrees = inputJitterConfig.t_int / m_GlobalRatioScale;
            m_ShoulderAngleTurn.m_MaxSpeedDegreesPerSecond = maxSpeedConfig.t_int;
            m_ShoulderAngleTurn.m_MinShoulderSpanToTorsoRatio = minSpanConfig.t_int / m_GlobalRatioScale;
            m_ShoulderAngleTurn.ClampValues();

            config.m_ShoulderAngleTurnMaxOutputAngleDegrees = m_ShoulderAngleTurn.m_MaxOutputAngleDegrees;
            config.m_ShoulderAngleTurnInvertDirection = m_ShoulderAngleTurn.m_InvertDirection;
            config.m_ShoulderAngleTurnSmoothTimeSeconds = m_ShoulderAngleTurn.m_SmoothTimeSeconds;
            config.m_ShoulderAngleTurnFullInputDegrees = m_ShoulderAngleTurn.m_FullInputDegrees;
            config.m_ShoulderAngleTurnInputJitterDegrees = m_ShoulderAngleTurn.m_InputJitterDegrees;
            config.m_ShoulderAngleTurnMaxSpeedDegreesPerSecond = m_ShoulderAngleTurn.m_MaxSpeedDegreesPerSecond;
            config.m_ShoulderAngleTurnMinShoulderSpanToTorsoRatio =
                m_ShoulderAngleTurn.m_MinShoulderSpanToTorsoRatio;
            config.m_TurnUpdateDeltaTimeSeconds = Time.deltaTime > 0f
                ? Mathf.Clamp(Time.deltaTime, 1f / 240f, 0.1f)
                : 0f;
        }

        public static bool ReadBlockNormalAttackWhileTurning()
        {
            return m_BlockNormalAttackWhileTurning;
        }

        public sealed class ShoulderHorizontalAngleTurnTuning
        {
            //                 ●右肩
            //               /  双肩连线与水平线夹角
            //     左肩●----/---------------- 水平线
            //
            // 输入角度从 0 线性映射到满转输入角度，输出范围为 0 到最大输出角度。
            // 中心附近不做停留或回正确认，输入防抖与输出平滑负责抑制小幅波动。
            // 髋部只用于判断双肩跨度是否可信，不参与方向或角度计算。

            // 最大输出角度，单位为度。45 表示角色最多向单侧旋转 45 度。
            public float m_MaxOutputAngleDegrees;
            // 左右方向与画面相反时设为 true。
            public bool m_InvertDirection;
            // 输出平滑时间，单位为秒。越小越及时，越大越平稳；建议范围 0.08 至 0.25。
            public float m_SmoothTimeSeconds;
            // 达到最大输出转角所需的肩线角度，单位为度。调大可降低角度放大比例；建议范围 18 至 30。
            public float m_FullInputDegrees;
            // 原始肩线角度防抖范围，单位为度。只忽略小幅输入变化；建议范围 0.5 至 1.5。
            public float m_InputJitterDegrees;
            // 输出最大角速度，单位为度每秒。限制瞬时跳变，不会中断持续同向跟随。
            public float m_MaxSpeedDegreesPerSecond;
            // 双肩跨度除以同侧肩髋平均长度的最小值，用于排除肩点重合；建议范围 0.15 至 0.40。
            public float m_MinShoulderSpanToTorsoRatio;

            public ShoulderHorizontalAngleTurnTuning()
            {
                ResetDefaults();
            }

            public void ResetDefaults()
            {
                m_MaxOutputAngleDegrees = 45f;
                m_InvertDirection = false;
                m_SmoothTimeSeconds = 0.12f;
                m_FullInputDegrees = 20f;
                m_InputJitterDegrees = 0.75f;
                m_MaxSpeedDegreesPerSecond = 180f;
                m_MinShoulderSpanToTorsoRatio = 0.25f;
            }

            public void ClampValues()
            {
                m_MaxOutputAngleDegrees = Mathf.Clamp(m_MaxOutputAngleDegrees, 0f, 45f);
                m_SmoothTimeSeconds = Mathf.Clamp(m_SmoothTimeSeconds, 0.01f, 2f);
                m_FullInputDegrees = Mathf.Clamp(m_FullInputDegrees, 0.0001f, 90f);
                m_InputJitterDegrees = Mathf.Clamp(m_InputJitterDegrees, 0f, 15f);
                m_MaxSpeedDegreesPerSecond = Mathf.Clamp(m_MaxSpeedDegreesPerSecond, 0f, 720f);
                m_MinShoulderSpanToTorsoRatio = Mathf.Clamp(m_MinShoulderSpanToTorsoRatio, 0f, 2f);
            }
        }
    }
}
