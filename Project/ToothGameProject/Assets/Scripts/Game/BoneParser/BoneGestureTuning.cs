using System.Collections.Generic;
using MonoBean;
using UnityEngine;

namespace GameDll
{
    // Unity 工程内统一维护骨骼识别参数。实际运行值来自全局表，每个姿势或流程使用独立参数组。
    public static class BoneGestureTuning
    {
        // 每个编号单独调参，修改 2001 不会改变 2011 或 2016。
        public static readonly UpDownSwing2001Tuning m_UpDownSwing2001 = new UpDownSwing2001Tuning();
        public static readonly HandsAboveNose2004Tuning m_HandsAboveNose2004 = new HandsAboveNose2004Tuning();
        public static readonly LeftLean2012Tuning m_LeftLean2012 = new LeftLean2012Tuning();
        public static readonly RightLean2013Tuning m_RightLean2013 = new RightLean2013Tuning();
        public static readonly Jump2014Tuning m_Jump2014 = new Jump2014Tuning();
        public static readonly CrouchDown2015Tuning m_CrouchDown2015 = new CrouchDown2015Tuning();
        public static readonly SingleHandSideSweep2016Tuning m_SingleHandSideSweep2016 =
            new SingleHandSideSweep2016Tuning();

        internal static void ApplyTo(BoneParserConfig config)
        {
            m_UpDownSwing2001.ApplyTo(config);
            m_HandsAboveNose2004.ApplyTo(config);
            m_LeftLean2012.ApplyTo(config);
            m_RightLean2013.ApplyTo(config);
            m_Jump2014.ApplyTo(config);
            m_CrouchDown2015.ApplyTo(config);
            m_SingleHandSideSweep2016.ApplyTo(config);
        }
    }

    internal static class BoneGestureGlobalConfigReader
    {
        private const float m_ScaledValueDivisor = 1000f;
        private static readonly HashSet<int> m_ReportedMissingConfigIds = new HashSet<int>();

        internal static bool TryReadScaledValue(int configId, string parameterName, ref float value)
        {
            var globalConfig = t_globalBean.GetConfig(configId, false);
            if (globalConfig == null)
            {
                LogMissingConfig(configId, parameterName);
                return false;
            }

            value = globalConfig.t_int / m_ScaledValueDivisor;
            return true;
        }

        internal static bool TryReadIntValue(int configId, string parameterName, ref int value)
        {
            var globalConfig = t_globalBean.GetConfig(configId, false);
            if (globalConfig == null)
            {
                LogMissingConfig(configId, parameterName);
                return false;
            }

            value = globalConfig.t_int;
            return true;
        }

        private static void LogMissingConfig(int configId, string parameterName)
        {
            if (!m_ReportedMissingConfigIds.Add(configId))
            {
                return;
            }

            Debug.LogError("骨骼识别全局表缺少配置：" + configId + "，参数：" + parameterName + "。");
        }
    }

    public sealed class UpDownSwing2001Tuning
    {
        private const int m_MinUpwardSpeedGlobalConfigId = 100323;
        private const int m_StartMinHeightGlobalConfigId = 100324;
        private const int m_StartMaxHeightGlobalConfigId = 100325;
        private const int m_MinStrokeDistanceGlobalConfigId = 100326;
        private const int m_MaxLateralGlobalConfigId = 100327;
        private const int m_BothHandsPairingWindowGlobalConfigId = 100328;

        // 双手上下挥动_流程（ID 2001）：双腕都必须参与上下运动，任意一腕完成有效上挥行程时产生攻击。
        // 双腕可同时或交替运动；向下只负责重新准备，不产生攻击。以下高度以同侧髋关节为 0、同侧肩膀为 1。
        // 数值沿同侧髋关节指向同侧肩膀的人体轴计算，不是屏幕 Y 坐标。人体比例不同，位置仅为大致参考。
        //
        //                 同侧肩膀  1.00  ───────────── 默认最高起点
        //                            0.75  胸部附近
        //                 动态触发线 = 本次最低起点 + 最小上挥行程
        //                            0.25  腰部附近
        //                 同侧髋关节 0.00  ───────────── 默认最低起点
        //
        // 起点可以位于髋关节到肩膀之间，不再要求先回到固定腰部区域。
        // 每次触发后，手腕需要向下回落至少一个最小上挥行程，才会准备下一次上挥。

        // 手腕到达胸部时要求的最低上挥速度，单位为每秒移动多少个髋肩长度。
        // 0 表示不限制速度；0.30～0.50 较容易；0.60～0.90 需要明确上挥；1.00 以上要求快速上挥。
        // 调大可过滤缓慢抬手，调小更容易触发。
        public float m_MinUpwardSpeedRatioPerSecond;

        // 允许记录起点的最低高度。0 接近髋关节；小于 0 会允许从大腿方向开始。
        public float m_StartMinHeightRatio;

        // 允许记录起点的最高高度。1 接近肩膀；调小会排除从上胸部附近开始的小幅动作。
        public float m_StartMaxHeightRatio;

        // 从本次最低起点向上移动多少个髋肩长度后触发。
        // 0.10 较灵敏；0.15 为当前表值；0.25 以上要求更明显的上下挥动。
        public float m_MinStrokeDistanceRatio;

        // 手腕离同侧肩髋连线的最大横向距离，单位同样是髋肩长度。
        // 0.25 较严格；0.50 允许正常外摆；0.75 较宽松；1.00 以上会允许很大的侧向偏移。
        // 调大允许手臂张得更开，调小可减少身体侧方无关动作参与识别。
        public float m_MaxLateralRatio;

        // 左右手都必须各自完成上面的完整行程。本值是两次完成允许相差的最长时间，单位为秒。
        // 0.40～0.80 适合连续交替；1.00～2.00 更宽松，允许动作间存在自然停顿；过大会把相隔较远的动作错误配成一次。
        public float m_BothHandsPairingWindowSeconds;

        internal void ApplyTo(BoneParserConfig config)
        {
            bool hasAllConfig = true;
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinUpwardSpeedGlobalConfigId, "ID 2001 最低上挥速度", ref m_MinUpwardSpeedRatioPerSecond);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_StartMinHeightGlobalConfigId, "ID 2001 起点最低高度", ref m_StartMinHeightRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_StartMaxHeightGlobalConfigId, "ID 2001 起点最高高度", ref m_StartMaxHeightRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinStrokeDistanceGlobalConfigId, "ID 2001 最低上挥行程", ref m_MinStrokeDistanceRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MaxLateralGlobalConfigId, "ID 2001 最大横向偏移", ref m_MaxLateralRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_BothHandsPairingWindowGlobalConfigId, "ID 2001 双手完成配对窗口", ref m_BothHandsPairingWindowSeconds);
            if (!hasAllConfig)
            {
                return;
            }

            config.m_UpDownSwingMinUpwardSpeedRatioPerSecond = Mathf.Max(0f, m_MinUpwardSpeedRatioPerSecond);
            config.m_UpDownSwingStartMinHeightRatio = Mathf.Min(m_StartMinHeightRatio, m_StartMaxHeightRatio);
            config.m_UpDownSwingStartMaxHeightRatio = Mathf.Max(m_StartMinHeightRatio, m_StartMaxHeightRatio);
            config.m_UpDownSwingMinStrokeDistanceRatio = Mathf.Max(0.0001f, m_MinStrokeDistanceRatio);
            config.m_UpDownSwingMaxLateralRatio = Mathf.Max(0f, m_MaxLateralRatio);
            config.m_UpDownSwingPairingWindowSeconds = Mathf.Max(0f, m_BothHandsPairingWindowSeconds);
        }
    }

    public sealed class HandsAboveNose2004Tuning
    {
        private const int m_EnterMarginGlobalConfigId = 100329;
        private const int m_ExitMarginGlobalConfigId = 100330;

        // 双手举过鼻尖释放_流程（ID 2004）：双腕同时超过鼻尖便触发，不需要蓄力或下压。
        // 比鼻尖额外高出多少个肩宽。0 表示刚超过鼻尖即可；调大可过滤鼻尖附近的抖动。
        public float m_EnterMarginRatio;

        // 两只手都低于鼻尖多少个肩宽后，才允许下一次举手。用于防止临界位置反复触发。
        public float m_ExitMarginRatio;

        // 每次举手只发送一次；是否满足能量、角色状态等条件由游戏判断，不保留拒绝后的补发。
        internal void ApplyTo(BoneParserConfig config)
        {
            bool hasAllConfig = true;
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_EnterMarginGlobalConfigId, "ID 2004 进入高度余量", ref m_EnterMarginRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_ExitMarginGlobalConfigId, "ID 2004 退出高度余量", ref m_ExitMarginRatio);
            if (!hasAllConfig)
            {
                return;
            }

            config.m_HandsAboveNoseEnterMarginRatio = Mathf.Max(0f, m_EnterMarginRatio);
            config.m_HandsAboveNoseExitMarginRatio = Mathf.Max(0f, m_ExitMarginRatio);
        }
    }

    public sealed class LeftLean2012Tuning
    {
        private const int m_ReadyFramesGlobalConfigId = 100331;
        private const int m_TriggerOffsetGlobalConfigId = 100332;
        private const int m_ReturnToNeutralGlobalConfigId = 100333;
        private const int m_MinSpeedGlobalConfigId = 100334;
        private const int m_CooldownGlobalConfigId = 100335;

        // 连续处于直立区域多少帧后允许识别。调大可过滤进入画面时的骨骼抖动。
        public int m_ReadyFrames;

        // 肩部中心相对髋部中心向玩家左侧偏移多少个肩宽时触发。调小更容易换道。
        public float m_TriggerOffsetRatio;

        // 偏移小于多少个肩宽视为恢复直立。触发后必须回到这个区域才能再次触发。
        public float m_ReturnToNeutralRatio;

        // 向左弯腰的最低横向速度，单位为每秒移动的肩宽倍数。
        public float m_MinSpeedRatioPerSecond;

        // 触发后的最短间隔，单位为秒。
        public float m_CooldownSeconds;

        internal void ApplyTo(BoneParserConfig config)
        {
            bool hasAllConfig = true;
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadIntValue(
                m_ReadyFramesGlobalConfigId, "ID 2012 直立准备帧数", ref m_ReadyFrames);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_TriggerOffsetGlobalConfigId, "ID 2012 触发横向偏移", ref m_TriggerOffsetRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_ReturnToNeutralGlobalConfigId, "ID 2012 恢复直立偏移", ref m_ReturnToNeutralRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinSpeedGlobalConfigId, "ID 2012 最低横向速度", ref m_MinSpeedRatioPerSecond);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_CooldownGlobalConfigId, "ID 2012 触发间隔", ref m_CooldownSeconds);
            if (!hasAllConfig)
            {
                return;
            }

            config.m_LeftLeanReadyFrames = Mathf.Max(1, m_ReadyFrames);
            config.m_LeftLeanTriggerOffsetRatio = Mathf.Max(0f, m_TriggerOffsetRatio);
            config.m_LeftLeanReturnToNeutralRatio = Mathf.Clamp(m_ReturnToNeutralRatio, 0f, config.m_LeftLeanTriggerOffsetRatio);
            config.m_LeftLeanMinSpeedRatioPerSecond = Mathf.Max(0f, m_MinSpeedRatioPerSecond);
            config.m_LeftLeanCooldownSeconds = Mathf.Max(0f, m_CooldownSeconds);
        }
    }

    public sealed class RightLean2013Tuning
    {
        private const int m_ReadyFramesGlobalConfigId = 100336;
        private const int m_TriggerOffsetGlobalConfigId = 100337;
        private const int m_ReturnToNeutralGlobalConfigId = 100338;
        private const int m_MinSpeedGlobalConfigId = 100339;
        private const int m_CooldownGlobalConfigId = 100340;

        // 连续处于直立区域多少帧后允许识别。调大可过滤进入画面时的骨骼抖动。
        public int m_ReadyFrames;

        // 肩部中心相对髋部中心向玩家右侧偏移多少个肩宽时触发。调小更容易换道。
        public float m_TriggerOffsetRatio;

        // 偏移小于多少个肩宽视为恢复直立。触发后必须回到这个区域才能再次触发。
        public float m_ReturnToNeutralRatio;

        // 向右弯腰的最低横向速度，单位为每秒移动的肩宽倍数。
        public float m_MinSpeedRatioPerSecond;

        // 触发后的最短间隔，单位为秒。
        public float m_CooldownSeconds;

        internal void ApplyTo(BoneParserConfig config)
        {
            bool hasAllConfig = true;
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadIntValue(
                m_ReadyFramesGlobalConfigId, "ID 2013 直立准备帧数", ref m_ReadyFrames);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_TriggerOffsetGlobalConfigId, "ID 2013 触发横向偏移", ref m_TriggerOffsetRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_ReturnToNeutralGlobalConfigId, "ID 2013 恢复直立偏移", ref m_ReturnToNeutralRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinSpeedGlobalConfigId, "ID 2013 最低横向速度", ref m_MinSpeedRatioPerSecond);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_CooldownGlobalConfigId, "ID 2013 触发间隔", ref m_CooldownSeconds);
            if (!hasAllConfig)
            {
                return;
            }

            config.m_RightLeanReadyFrames = Mathf.Max(1, m_ReadyFrames);
            config.m_RightLeanTriggerOffsetRatio = Mathf.Max(0f, m_TriggerOffsetRatio);
            config.m_RightLeanReturnToNeutralRatio = Mathf.Clamp(m_ReturnToNeutralRatio, 0f, config.m_RightLeanTriggerOffsetRatio);
            config.m_RightLeanMinSpeedRatioPerSecond = Mathf.Max(0f, m_MinSpeedRatioPerSecond);
            config.m_RightLeanCooldownSeconds = Mathf.Max(0f, m_CooldownSeconds);
        }
    }

    public sealed class Jump2014Tuning
    {
        private const int m_ReadyFramesGlobalConfigId = 100341;
        private const int m_ReadyMaxVerticalSpeedGlobalConfigId = 100342;
        private const int m_MinShoulderRiseGlobalConfigId = 100343;
        private const int m_MinHipRiseGlobalConfigId = 100344;
        private const int m_MinAnkleRiseGlobalConfigId = 100345;
        private const int m_MinUpwardSpeedGlobalConfigId = 100346;
        private const int m_CooldownGlobalConfigId = 100347;

        // 身体稳定多少帧后记录起跳基准高度。
        public int m_ReadyFrames;

        // 记录基准高度时允许的最大竖直速度，单位为每秒移动的肩宽倍数。
        public float m_ReadyMaxVerticalSpeedRatioPerSecond;

        // 肩部中心和髋部中心相对基准高度的最低上升距离，单位为肩宽倍数，两处必须同时达标。
        // 脚踝最低上升距离：当前算法已不参与触发判定（起跳瞬间脚踝预测不可靠），仅为兼容全局表保留。
        public float m_MinShoulderRiseRatio;
        public float m_MinHipRiseRatio;
        public float m_MinAnkleRiseRatio;

        // 肩部或髋部任一达到的最低上升速度，单位为每秒移动的肩宽倍数。不再要求脚踝同时达标。
        public float m_MinUpwardSpeedRatioPerSecond;

        // 触发后的最短间隔，单位为秒。
        public float m_CooldownSeconds;

        internal void ApplyTo(BoneParserConfig config)
        {
            bool hasAllConfig = true;
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadIntValue(
                m_ReadyFramesGlobalConfigId, "ID 2014 稳定准备帧数", ref m_ReadyFrames);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_ReadyMaxVerticalSpeedGlobalConfigId, "ID 2014 准备阶段最大竖直速度", ref m_ReadyMaxVerticalSpeedRatioPerSecond);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinShoulderRiseGlobalConfigId, "ID 2014 肩部最低上升距离", ref m_MinShoulderRiseRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinHipRiseGlobalConfigId, "ID 2014 髋部最低上升距离", ref m_MinHipRiseRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinAnkleRiseGlobalConfigId, "ID 2014 脚踝最低上升距离", ref m_MinAnkleRiseRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinUpwardSpeedGlobalConfigId, "ID 2014 最低上升速度", ref m_MinUpwardSpeedRatioPerSecond);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_CooldownGlobalConfigId, "ID 2014 触发间隔", ref m_CooldownSeconds);
            if (!hasAllConfig)
            {
                return;
            }

            config.m_JumpReadyFrames = Mathf.Max(1, m_ReadyFrames);
            config.m_JumpReadyMaxVerticalSpeedRatioPerSecond = Mathf.Max(0f, m_ReadyMaxVerticalSpeedRatioPerSecond);
            config.m_JumpMinShoulderRiseRatio = Mathf.Max(0f, m_MinShoulderRiseRatio);
            config.m_JumpMinHipRiseRatio = Mathf.Max(0f, m_MinHipRiseRatio);
            config.m_JumpMinAnkleRiseRatio = Mathf.Max(0f, m_MinAnkleRiseRatio);
            config.m_JumpMinUpwardSpeedRatioPerSecond = Mathf.Max(0f, m_MinUpwardSpeedRatioPerSecond);
            config.m_JumpCooldownSeconds = Mathf.Max(0f, m_CooldownSeconds);
        }
    }

    public sealed class CrouchDown2015Tuning
    {
        private const int m_ReadyFramesGlobalConfigId = 100348;
        private const int m_ReadyMaxVerticalSpeedGlobalConfigId = 100349;
        private const int m_MinShoulderDropGlobalConfigId = 100350;
        private const int m_MinHipDropGlobalConfigId = 100351;
        private const int m_MinLegCompressionGlobalConfigId = 100352;
        private const int m_MinDownwardSpeedGlobalConfigId = 100353;
        private const int m_CooldownGlobalConfigId = 100354;
        private const int m_ConfirmHoldFramesGlobalConfigId = 100363;

        // 站立稳定多少帧后记录下蹲基准高度。
        public int m_ReadyFrames;

        // 记录基准高度时允许的最大竖直速度，单位为每秒移动的肩宽倍数。
        public float m_ReadyMaxVerticalSpeedRatioPerSecond;

        // 肩部中心和髋部中心相对基准高度的最低下降距离，单位为肩宽倍数。
        public float m_MinShoulderDropRatio;
        public float m_MinHipDropRatio;

        // 髋部到脚踝的竖直距离至少缩短多少个肩宽，用来确认腿部确实发生弯曲。
        // 仅在基准帧和当前帧都有脚踝数据时作为附加确认；脚踝不可跟踪时依靠肩髋同步下降判定。
        public float m_MinLegCompressionRatio;

        // 肩部或髋部任一达到的最低下降速度，单位为每秒移动的肩宽倍数。
        public float m_MinDownwardSpeedRatioPerSecond;

        // 触发后的最短间隔，单位为秒。
        public float m_CooldownSeconds;

        // 下降条件首次满足后，身体需保持在髋部下降阈值以下至少多少帧才真正触发。
        // 用于区分真下蹲和起跳前的反向下压：反向下压会很快反弹，凑不满保持帧。
        // 1 表示满足条件立即触发（旧行为）；调大可减少起跳前误触发蹲，调小蹲更跟手但更容易误触发。
        // 骨骼约 30 帧每秒，8 帧约 0.27 秒。
        public int m_ConfirmHoldFrames;

        internal void ApplyTo(BoneParserConfig config)
        {
            bool hasAllConfig = true;
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadIntValue(
                m_ReadyFramesGlobalConfigId, "ID 2015 稳定准备帧数", ref m_ReadyFrames);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_ReadyMaxVerticalSpeedGlobalConfigId, "ID 2015 准备阶段最大竖直速度", ref m_ReadyMaxVerticalSpeedRatioPerSecond);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinShoulderDropGlobalConfigId, "ID 2015 肩部最低下降距离", ref m_MinShoulderDropRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinHipDropGlobalConfigId, "ID 2015 髋部最低下降距离", ref m_MinHipDropRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinLegCompressionGlobalConfigId, "ID 2015 腿部最低压缩距离", ref m_MinLegCompressionRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinDownwardSpeedGlobalConfigId, "ID 2015 最低下降速度", ref m_MinDownwardSpeedRatioPerSecond);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_CooldownGlobalConfigId, "ID 2015 触发间隔", ref m_CooldownSeconds);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadIntValue(
                m_ConfirmHoldFramesGlobalConfigId, "ID 2015 确认保持帧数", ref m_ConfirmHoldFrames);
            if (!hasAllConfig)
            {
                return;
            }

            config.m_CrouchDownReadyFrames = Mathf.Max(1, m_ReadyFrames);
            config.m_CrouchDownReadyMaxVerticalSpeedRatioPerSecond = Mathf.Max(0f, m_ReadyMaxVerticalSpeedRatioPerSecond);
            config.m_CrouchDownMinShoulderDropRatio = Mathf.Max(0f, m_MinShoulderDropRatio);
            config.m_CrouchDownMinHipDropRatio = Mathf.Max(0f, m_MinHipDropRatio);
            config.m_CrouchDownMinLegCompressionRatio = Mathf.Max(0f, m_MinLegCompressionRatio);
            config.m_CrouchDownMinDownwardSpeedRatioPerSecond = Mathf.Max(0f, m_MinDownwardSpeedRatioPerSecond);
            config.m_CrouchDownCooldownSeconds = Mathf.Max(0f, m_CooldownSeconds);
            config.m_CrouchDownConfirmHoldFrames = Mathf.Max(1, m_ConfirmHoldFrames);
        }
    }

    public sealed class SingleHandSideSweep2016Tuning
    {
        private const int m_StartAboveHipToleranceGlobalConfigId = 100355;
        private const int m_StartBelowHipToleranceGlobalConfigId = 100356;
        private const int m_StartMovementDistanceGlobalConfigId = 100357;
        private const int m_ElbowHeightToleranceGlobalConfigId = 100358;
        private const int m_WristMissingToleranceFramesGlobalConfigId = 100359;
        private const int m_MaxDurationGlobalConfigId = 100360;
        private const int m_ElbowMissingToleranceFramesGlobalConfigId = 100361;
        private const int m_MinTriggerIntervalGlobalConfigId = 100362;

        // 动作起点时，手腕允许高于同侧髋部多少个肩宽。调大可在手自然放于髋部上方时更早记录起点。
        public float m_StartAboveHipToleranceRatio;

        // 动作起点时，手腕允许低于同侧髋部多少个肩宽。调大可接受更低的自然垂手位置。
        public float m_StartBelowHipToleranceRatio;

        // 手腕沿人体向上方向离开髋部起点多少个肩宽后开始判定动作，用于过滤自然站立时的轻微抖动。
        public float m_StartMovementDistanceRatio;

        // 按人体向上方向判断手腕是否到达同侧肘部高度，允许低于肘部多少个肩宽。调大可更早触发。
        public float m_ElbowHeightToleranceRatio;

        // 手腕快速运动时允许连续丢失多少帧，期间保留当前手的起点和移动状态。调大可容忍更多识别抖动。
        public int m_WristMissingToleranceFrames;

        // 记录自然下垂起点后允许完成动作的最长时间，单位为秒，仅用于防止很久以前的起点影响后续动作。
        public float m_MaxDurationSeconds;

        // 动作过程中同侧肘部允许连续丢失多少帧，期间使用最后一次有效的肘部高度。
        public int m_ElbowMissingToleranceFrames;

        // 左右手共用的最短事件间隔，单位为秒。填 0 表示不限制，同一帧左右手均完成时可各产生一条事件。
        public float m_MinTriggerIntervalSeconds;

        internal void ApplyTo(BoneParserConfig config)
        {
            bool hasAllConfig = true;
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_StartAboveHipToleranceGlobalConfigId, "ID 2016 起点高于髋部容差", ref m_StartAboveHipToleranceRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_StartBelowHipToleranceGlobalConfigId, "ID 2016 起点低于髋部容差", ref m_StartBelowHipToleranceRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_StartMovementDistanceGlobalConfigId, "ID 2016 起步移动距离", ref m_StartMovementDistanceRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_ElbowHeightToleranceGlobalConfigId, "ID 2016 肘部高度容差", ref m_ElbowHeightToleranceRatio);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadIntValue(
                m_WristMissingToleranceFramesGlobalConfigId, "ID 2016 手腕允许丢失帧数", ref m_WristMissingToleranceFrames);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MaxDurationGlobalConfigId, "ID 2016 最长完成时间", ref m_MaxDurationSeconds);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadIntValue(
                m_ElbowMissingToleranceFramesGlobalConfigId, "ID 2016 肘部允许丢失帧数", ref m_ElbowMissingToleranceFrames);
            hasAllConfig &= BoneGestureGlobalConfigReader.TryReadScaledValue(
                m_MinTriggerIntervalGlobalConfigId, "ID 2016 最短触发间隔", ref m_MinTriggerIntervalSeconds);
            if (!hasAllConfig)
            {
                return;
            }

            config.m_SingleHandSideSweepStartAboveHipToleranceRatio = Mathf.Max(0f, m_StartAboveHipToleranceRatio);
            config.m_SingleHandSideSweepStartBelowHipToleranceRatio = Mathf.Max(0f, m_StartBelowHipToleranceRatio);
            config.m_SingleHandSideSweepStartMovementDistanceRatio = Mathf.Max(0f, m_StartMovementDistanceRatio);
            config.m_SingleHandSideSweepElbowHeightToleranceRatio = Mathf.Max(0f, m_ElbowHeightToleranceRatio);
            config.m_SingleHandSideSweepWristMissingToleranceFrames = Mathf.Max(0, m_WristMissingToleranceFrames);
            config.m_SingleHandSideSweepMaxDurationSeconds = Mathf.Max(0f, m_MaxDurationSeconds);
            config.m_SingleHandSideSweepElbowMissingToleranceFrames = Mathf.Max(0, m_ElbowMissingToleranceFrames);
            config.m_SingleHandSideSweepMinTriggerIntervalSeconds = Mathf.Max(0f, m_MinTriggerIntervalSeconds);
        }
    }
}
