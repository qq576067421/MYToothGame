using System;

namespace CompanyInternalTools.BoneParserLib
{
    internal struct TorsoMovementInput
    {
        public float m_ShoulderWidth;
        public BoneVector2 m_LeftShoulder;
        public BoneVector2 m_RightShoulder;
        public BoneVector2 m_ShoulderCenter;
        public BoneVector2 m_LeftHip;
        public BoneVector2 m_RightHip;
        public BoneVector2 m_HipCenter;
    }

    internal struct FullBodyMovementInput
    {
        public TorsoMovementInput m_Torso;
        public BoneVector2 m_AnkleCenter;
        // 双脚踝是否至少有一侧达到置信度。脚踝是全身最不稳定的关键点，
        // 跳/蹲判定以肩髋为主，脚踝数据可用时才参与确认，不可用时不再整帧丢弃。
        public bool m_HasAnkleTracking;
    }

    internal static class RunnerMovementFlowUtility
    {
        // 跳起来/蹲下去准备阶段允许的连续竖直抖动帧数：脚踝等关键点偶发单帧尖峰
        // 不再直接重置基准收集，只有连续超过该帧数才视为真的开始运动。
        internal const int m_BodyMotionReadySpikeToleranceFrames = 2;

        // 跳已武装时，单纯下蹲蓄力（无论多深）不再重置基准，站立基准保持不变。
        // 只有在低于容忍线的位置稳定停留约这么久（落地缓冲远短于该时长），
        // 才把该姿势承认为新的站立基准。
        internal const int m_JumpSettleRebaselineFrames = 15;

        public static bool TryReadTorsoInput(BoneGestureRuntimeContext context, out TorsoMovementInput input)
        {
            input = new TorsoMovementInput();
            BoneTrackedPerson person = context.m_Person;
            float minScore = context.m_Config.m_GestureKeypointMinConfidence;
            if (person == null ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左肩, minScore, out input.m_LeftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右肩, minScore, out input.m_RightShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左髋, minScore, out input.m_LeftHip) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右髋, minScore, out input.m_RightHip))
            {
                return false;
            }

            input.m_ShoulderWidth = BoneMath.Abs(input.m_RightShoulder.m_X - input.m_LeftShoulder.m_X) +
                context.m_Config.m_ShoulderWidthEpsilon;
            if (input.m_ShoulderWidth <= context.m_Config.m_ShoulderWidthEpsilon)
            {
                return false;
            }

            input.m_ShoulderCenter = (input.m_LeftShoulder + input.m_RightShoulder) * 0.5f;
            input.m_HipCenter = (input.m_LeftHip + input.m_RightHip) * 0.5f;
            return true;
        }

        public static bool TryReadFullBodyInput(BoneGestureRuntimeContext context, out FullBodyMovementInput input)
        {
            input = new FullBodyMovementInput();
            float minScore = context.m_Config.m_GestureKeypointMinConfidence;
            if (!TryReadTorsoInput(context, out input.m_Torso))
            {
                return false;
            }

            bool hasLeftAnkle = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person, BoneBodyJointType.左脚踝, minScore, out BoneVector2 leftAnkle);
            bool hasRightAnkle = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person, BoneBodyJointType.右脚踝, minScore, out BoneVector2 rightAnkle);
            if (hasLeftAnkle && hasRightAnkle)
            {
                input.m_AnkleCenter = (leftAnkle + rightAnkle) * 0.5f;
                input.m_HasAnkleTracking = true;
            }
            else if (hasLeftAnkle)
            {
                input.m_AnkleCenter = leftAnkle;
                input.m_HasAnkleTracking = true;
            }
            else if (hasRightAnkle)
            {
                input.m_AnkleCenter = rightAnkle;
                input.m_HasAnkleTracking = true;
            }

            return true;
        }

        public static float ReadPlayerLeftAxisSign(TorsoMovementInput input)
        {
            return BoneMath.Sign(input.m_LeftShoulder.m_X - input.m_RightShoulder.m_X);
        }

        // 稳定性检查只看肩和髋：脚踝关键点即使人站着不动也会有大幅抖动，
        // 把脚踝速度算进稳定性会让准备阶段几乎永远无法完成。
        public static float ReadMaxTorsoVerticalSpeed(
            FullBodyMovementInput input,
            BoneVector2 lastShoulderCenter,
            BoneVector2 lastHipCenter,
            float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f)
            {
                return float.MaxValue;
            }

            float shoulderSpeed = BoneMath.Abs(input.m_Torso.m_ShoulderCenter.m_Y - lastShoulderCenter.m_Y) / deltaTimeSeconds;
            float hipSpeed = BoneMath.Abs(input.m_Torso.m_HipCenter.m_Y - lastHipCenter.m_Y) / deltaTimeSeconds;
            return BoneMath.Max(shoulderSpeed, hipSpeed);
        }
    }

    internal sealed class SideLeanFlowState : TriggerOnlyFlowState
    {
        public int m_NeutralFrameCount;
        public bool m_IsArmed;
        public bool m_HasLastLeanRatio;
        public float m_LastLeanRatio;

        public override void ResetFlowState()
        {
            m_NeutralFrameCount = 0;
            m_IsArmed = false;
            m_HasLastLeanRatio = false;
            m_LastLeanRatio = 0f;
        }
    }

    internal abstract class SideLeanFlowRecognizer : TriggerOnlyFlowRecognizer<SideLeanFlowState>
    {
        protected abstract float DirectionMultiplier { get; }
        protected abstract int ReadReadyFrames(BoneParserConfig config);
        protected abstract float ReadTriggerOffsetRatio(BoneParserConfig config);
        protected abstract float ReadReturnToNeutralRatio(BoneParserConfig config);
        protected abstract float ReadMinSpeedRatioPerSecond(BoneParserConfig config);

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, SideLeanFlowState state)
        {
            if (!RunnerMovementFlowUtility.TryReadTorsoInput(context, out TorsoMovementInput input))
            {
                state.ResetFlowState();
                return false;
            }

            float playerLeftAxisSign = RunnerMovementFlowUtility.ReadPlayerLeftAxisSign(input);
            if (playerLeftAxisSign == 0f)
            {
                state.ResetFlowState();
                return false;
            }

            float playerLeftLeanRatio = (input.m_ShoulderCenter.m_X - input.m_HipCenter.m_X) *
                playerLeftAxisSign / input.m_ShoulderWidth;
            float directionalLeanRatio = playerLeftLeanRatio * DirectionMultiplier;
            if (!state.m_IsArmed)
            {
                state.m_NeutralFrameCount = BoneMath.Abs(playerLeftLeanRatio) <= ReadReturnToNeutralRatio(context.m_Config)
                    ? state.m_NeutralFrameCount + 1
                    : 0;
                state.m_IsArmed = state.m_NeutralFrameCount >= Math.Max(1, ReadReadyFrames(context.m_Config));
                state.m_LastLeanRatio = directionalLeanRatio;
                state.m_HasLastLeanRatio = true;
                return false;
            }

            float leanSpeed = 0f;
            if (state.m_HasLastLeanRatio && context.m_DeltaTimeSeconds > 0f)
            {
                leanSpeed = (directionalLeanRatio - state.m_LastLeanRatio) / context.m_DeltaTimeSeconds;
            }

            state.m_LastLeanRatio = directionalLeanRatio;
            state.m_HasLastLeanRatio = true;
            return directionalLeanRatio >= ReadTriggerOffsetRatio(context.m_Config) &&
                leanSpeed >= ReadMinSpeedRatioPerSecond(context.m_Config);
        }
    }

    internal sealed class LeftLeanFlowRecognizer : SideLeanFlowRecognizer
    {
        protected override string StateKey => "Flow.LeftLean";
        protected override BoneGestureType GestureType => BoneGestureType.向左弯腰_流程;
        protected override float DirectionMultiplier => 1f;
        protected override int ReadReadyFrames(BoneParserConfig config) => config.m_LeftLeanReadyFrames;
        protected override float ReadTriggerOffsetRatio(BoneParserConfig config) => config.m_LeftLeanTriggerOffsetRatio;
        protected override float ReadReturnToNeutralRatio(BoneParserConfig config) => config.m_LeftLeanReturnToNeutralRatio;
        protected override float ReadMinSpeedRatioPerSecond(BoneParserConfig config) => config.m_LeftLeanMinSpeedRatioPerSecond;
        protected override float ReadCooldownSeconds(BoneParserConfig config) => config.m_LeftLeanCooldownSeconds;
    }

    internal sealed class RightLeanFlowRecognizer : SideLeanFlowRecognizer
    {
        protected override string StateKey => "Flow.RightLean";
        protected override BoneGestureType GestureType => BoneGestureType.向右弯腰_流程;
        protected override float DirectionMultiplier => -1f;
        protected override int ReadReadyFrames(BoneParserConfig config) => config.m_RightLeanReadyFrames;
        protected override float ReadTriggerOffsetRatio(BoneParserConfig config) => config.m_RightLeanTriggerOffsetRatio;
        protected override float ReadReturnToNeutralRatio(BoneParserConfig config) => config.m_RightLeanReturnToNeutralRatio;
        protected override float ReadMinSpeedRatioPerSecond(BoneParserConfig config) => config.m_RightLeanMinSpeedRatioPerSecond;
        protected override float ReadCooldownSeconds(BoneParserConfig config) => config.m_RightLeanCooldownSeconds;
    }

    internal abstract class StableBodyMotionFlowState : TriggerOnlyFlowState
    {
        public int m_ReadyFrameCount;
        public bool m_IsReady;
        public bool m_HasLastInput;
        public bool m_BaselineHasAnkleTracking;
        public int m_UnstableFrameCount;
        public int m_PendingRebaselineFrames;
        public int m_CrouchDownConfirmFrames;
        public BoneVector2 m_BaselineShoulderCenter;
        public BoneVector2 m_BaselineHipCenter;
        public BoneVector2 m_BaselineAnkleCenter;
        public BoneVector2 m_LastShoulderCenter;
        public BoneVector2 m_LastHipCenter;
        public BoneVector2 m_LastAnkleCenter;

        public override void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_HasLastInput = false;
            m_BaselineHasAnkleTracking = false;
            m_UnstableFrameCount = 0;
            m_PendingRebaselineFrames = 0;
            m_CrouchDownConfirmFrames = 0;
            m_BaselineShoulderCenter = BoneVector2.m_Zero;
            m_BaselineHipCenter = BoneVector2.m_Zero;
            m_BaselineAnkleCenter = BoneVector2.m_Zero;
            m_LastShoulderCenter = BoneVector2.m_Zero;
            m_LastHipCenter = BoneVector2.m_Zero;
            m_LastAnkleCenter = BoneVector2.m_Zero;
        }

        public void StoreLastInput(FullBodyMovementInput input)
        {
            m_LastShoulderCenter = input.m_Torso.m_ShoulderCenter;
            m_LastHipCenter = input.m_Torso.m_HipCenter;
            m_LastAnkleCenter = input.m_AnkleCenter;
            m_HasLastInput = true;
        }

        public void RestartReadyCollection(FullBodyMovementInput input)
        {
            m_ReadyFrameCount = 1;
            m_IsReady = false;
            m_UnstableFrameCount = 0;
            m_PendingRebaselineFrames = 0;
            m_CrouchDownConfirmFrames = 0;
            m_BaselineHasAnkleTracking = input.m_HasAnkleTracking;
            m_BaselineShoulderCenter = input.m_Torso.m_ShoulderCenter;
            m_BaselineHipCenter = input.m_Torso.m_HipCenter;
            m_BaselineAnkleCenter = input.m_AnkleCenter;
            StoreLastInput(input);
        }

        public void AccumulateBaseline(FullBodyMovementInput input)
        {
            float blend = 1f / Math.Max(1, m_ReadyFrameCount);
            m_BaselineShoulderCenter = Lerp(m_BaselineShoulderCenter, input.m_Torso.m_ShoulderCenter, blend);
            m_BaselineHipCenter = Lerp(m_BaselineHipCenter, input.m_Torso.m_HipCenter, blend);
            m_BaselineAnkleCenter = Lerp(m_BaselineAnkleCenter, input.m_AnkleCenter, blend);
        }

        private static BoneVector2 Lerp(BoneVector2 start, BoneVector2 end, float blend)
        {
            return new BoneVector2(
                BoneMath.Lerp(start.m_X, end.m_X, blend),
                BoneMath.Lerp(start.m_Y, end.m_Y, blend));
        }
    }

    internal sealed class JumpFlowState : StableBodyMotionFlowState
    {
    }

    internal sealed class JumpFlowRecognizer : TriggerOnlyFlowRecognizer<JumpFlowState>
    {
        protected override string StateKey => "Flow.Jump";
        protected override BoneGestureType GestureType => BoneGestureType.跳起来_流程;

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, JumpFlowState state)
        {
            if (!RunnerMovementFlowUtility.TryReadFullBodyInput(context, out FullBodyMovementInput input))
            {
                state.ResetFlowState();
                return false;
            }

            if (!state.m_HasLastInput)
            {
                state.RestartReadyCollection(input);
                return false;
            }

            float maxStableSpeed = input.m_Torso.m_ShoulderWidth *
                context.m_Config.m_JumpReadyMaxVerticalSpeedRatioPerSecond;
            float measuredSpeed = RunnerMovementFlowUtility.ReadMaxTorsoVerticalSpeed(
                input, state.m_LastShoulderCenter, state.m_LastHipCenter, context.m_DeltaTimeSeconds);
            if (!state.m_IsReady)
            {
                // 起跳只能从站姿开始；已处于蹲姿时不武装，蹲下再站起不会触发跳。
                if (BonePoseDetection.ReadIsCrouching(context))
                {
                    state.m_ReadyFrameCount = 0;
                    state.m_UnstableFrameCount = 0;
                    state.StoreLastInput(input);
                    return false;
                }

                if (measuredSpeed > maxStableSpeed)
                {
                    if (++state.m_UnstableFrameCount > RunnerMovementFlowUtility.m_BodyMotionReadySpikeToleranceFrames)
                    {
                        state.RestartReadyCollection(input);
                    }
                    else
                    {
                        state.StoreLastInput(input);
                    }

                    return false;
                }

                state.m_UnstableFrameCount = 0;
                state.m_ReadyFrameCount++;
                state.AccumulateBaseline(input);
                state.StoreLastInput(input);
                state.m_IsReady = state.m_ReadyFrameCount >= Math.Max(1, context.m_Config.m_JumpReadyFrames);
                return false;
            }

            float shoulderRise = state.m_BaselineShoulderCenter.m_Y - input.m_Torso.m_ShoulderCenter.m_Y;
            float hipRise = state.m_BaselineHipCenter.m_Y - input.m_Torso.m_HipCenter.m_Y;
            // 自然起跳都会先有下蹲蓄力，且深度不定：单纯下沉不再重置基准，站立基准
            // 保持不变，随后的上升仍相对站姿判定。进入蹲姿立即解除武装；只有在低于
            // 容忍线的位置稳定停留足够久（落地缓冲远短于该时长）才承认为新站姿基准。
            if (BonePoseDetection.ReadIsCrouching(context))
            {
                state.RestartReadyCollection(input);
                return false;
            }

            if (hipRise < -input.m_Torso.m_ShoulderWidth * context.m_Config.m_JumpMinHipRiseRatio)
            {
                if (measuredSpeed <= maxStableSpeed &&
                    ++state.m_PendingRebaselineFrames >= RunnerMovementFlowUtility.m_JumpSettleRebaselineFrames)
                {
                    state.RestartReadyCollection(input);
                    return false;
                }
            }
            else
            {
                state.m_PendingRebaselineFrames = 0;
            }

            if (context.m_DeltaTimeSeconds <= 0f)
            {
                state.StoreLastInput(input);
                return false;
            }

            float shoulderUpSpeed = (state.m_LastShoulderCenter.m_Y - input.m_Torso.m_ShoulderCenter.m_Y) / context.m_DeltaTimeSeconds;
            float hipUpSpeed = (state.m_LastHipCenter.m_Y - input.m_Torso.m_HipCenter.m_Y) / context.m_DeltaTimeSeconds;
            state.StoreLastInput(input);
            float minUpSpeed = input.m_Torso.m_ShoulderWidth * context.m_Config.m_JumpMinUpwardSpeedRatioPerSecond;
            // 触发条件完全排除脚踝：起跳瞬间脚踝常被预测在原地附近，曾直接否决掉
            // 完全有效的起跳。肩髋同步上升并带真实上升速度已经是跳跃专属信号。
            return shoulderRise >= input.m_Torso.m_ShoulderWidth * context.m_Config.m_JumpMinShoulderRiseRatio &&
                hipRise >= input.m_Torso.m_ShoulderWidth * context.m_Config.m_JumpMinHipRiseRatio &&
                (shoulderUpSpeed >= minUpSpeed || hipUpSpeed >= minUpSpeed);
        }

        protected override float ReadCooldownSeconds(BoneParserConfig config) => config.m_JumpCooldownSeconds;
    }

    internal sealed class CrouchDownFlowState : StableBodyMotionFlowState
    {
    }

    internal sealed class CrouchDownFlowRecognizer : TriggerOnlyFlowRecognizer<CrouchDownFlowState>
    {
        protected override string StateKey => "Flow.CrouchDown";
        protected override BoneGestureType GestureType => BoneGestureType.蹲下去_流程;

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, CrouchDownFlowState state)
        {
            if (!RunnerMovementFlowUtility.TryReadFullBodyInput(context, out FullBodyMovementInput input))
            {
                state.ResetFlowState();
                return false;
            }

            if (!state.m_HasLastInput)
            {
                state.RestartReadyCollection(input);
                return false;
            }

            float maxStableSpeed = input.m_Torso.m_ShoulderWidth *
                context.m_Config.m_CrouchDownReadyMaxVerticalSpeedRatioPerSecond;
            float measuredSpeed = RunnerMovementFlowUtility.ReadMaxTorsoVerticalSpeed(
                input, state.m_LastShoulderCenter, state.m_LastHipCenter, context.m_DeltaTimeSeconds);
            if (!state.m_IsReady)
            {
                if (BonePoseDetection.ReadIsCrouching(context))
                {
                    state.m_ReadyFrameCount = 0;
                    state.m_UnstableFrameCount = 0;
                    state.StoreLastInput(input);
                    return false;
                }

                if (measuredSpeed > maxStableSpeed)
                {
                    if (++state.m_UnstableFrameCount > RunnerMovementFlowUtility.m_BodyMotionReadySpikeToleranceFrames)
                    {
                        state.m_ReadyFrameCount = 0;
                        state.m_UnstableFrameCount = 0;
                    }

                    state.StoreLastInput(input);
                    return false;
                }

                state.m_UnstableFrameCount = 0;
                if (state.m_ReadyFrameCount <= 0)
                {
                    state.RestartReadyCollection(input);
                    return false;
                }

                state.m_ReadyFrameCount++;
                state.AccumulateBaseline(input);
                state.StoreLastInput(input);
                state.m_IsReady = state.m_ReadyFrameCount >= Math.Max(1, context.m_Config.m_CrouchDownReadyFrames);
                return false;
            }

            float shoulderDrop = input.m_Torso.m_ShoulderCenter.m_Y - state.m_BaselineShoulderCenter.m_Y;
            float hipDrop = input.m_Torso.m_HipCenter.m_Y - state.m_BaselineHipCenter.m_Y;
            float baselineLegHeight = state.m_BaselineAnkleCenter.m_Y - state.m_BaselineHipCenter.m_Y;
            float currentLegHeight = input.m_AnkleCenter.m_Y - input.m_Torso.m_HipCenter.m_Y;
            float legCompression = baselineLegHeight - currentLegHeight;
            if (hipDrop < -input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinHipDropRatio * 0.5f)
            {
                state.RestartReadyCollection(input);
                return false;
            }

            if (context.m_DeltaTimeSeconds <= 0f)
            {
                state.StoreLastInput(input);
                return false;
            }

            float shoulderDownSpeed = (input.m_Torso.m_ShoulderCenter.m_Y - state.m_LastShoulderCenter.m_Y) / context.m_DeltaTimeSeconds;
            float hipDownSpeed = (input.m_Torso.m_HipCenter.m_Y - state.m_LastHipCenter.m_Y) / context.m_DeltaTimeSeconds;
            state.StoreLastInput(input);
            float minDownSpeed = input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinDownwardSpeedRatioPerSecond;
            // 腿部压缩只有在基准帧和当前帧都有脚踝数据时才作为附加确认；
            // 脚踝不可跟踪时依靠肩髋同步下降区分下蹲与弯腰等动作。
            bool legConfirmed = !(input.m_HasAnkleTracking && state.m_BaselineHasAnkleTracking) ||
                legCompression >= input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinLegCompressionRatio;
            float hipDropThreshold = input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinHipDropRatio;
            // 起跳前的反向下压同样会让下降+速度条件短暂满足。蹲下必须保持
            // 在髋部下降阈值以下满配置的帧数才真正触发；反向下压会很快反弹
            // 回到阈值之上，从而取消待触发的蹲事件。
            int confirmHoldFrames = context.m_Config.m_CrouchDownConfirmHoldFrames;
            if (state.m_CrouchDownConfirmFrames > 0)
            {
                if (hipDrop < hipDropThreshold)
                {
                    state.m_CrouchDownConfirmFrames = 0;
                    return false;
                }

                state.m_CrouchDownConfirmFrames++;
                if (state.m_CrouchDownConfirmFrames < confirmHoldFrames)
                {
                    return false;
                }

                state.m_CrouchDownConfirmFrames = 0;
                return true;
            }

            bool fullTrigger = shoulderDrop >= input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinShoulderDropRatio &&
                hipDrop >= hipDropThreshold &&
                legConfirmed &&
                (shoulderDownSpeed >= minDownSpeed || hipDownSpeed >= minDownSpeed);
            if (fullTrigger)
            {
                if (confirmHoldFrames <= 1)
                {
                    return true;
                }

                state.m_CrouchDownConfirmFrames = 1;
            }

            return false;
        }

        protected override float ReadCooldownSeconds(BoneParserConfig config) => config.m_CrouchDownCooldownSeconds;
    }

    internal sealed class SingleHandSideSweepHandState
    {
        public bool m_IsReady;
        public bool m_IsMoving;
        public float m_ReadyWristHipOffsetRatio;
        public bool m_HasLastWrist;
        public float m_LastWristHipOffsetRatio;
        public int m_WristMissingFrameCount;
        public bool m_HasLastElbow;
        public float m_LastElbowHipOffsetRatio;
        public int m_ElbowMissingFrameCount;
        public float m_MotionSeconds;

        public void Reset()
        {
            m_IsReady = false;
            m_IsMoving = false;
            m_ReadyWristHipOffsetRatio = 0f;
            m_HasLastWrist = false;
            m_LastWristHipOffsetRatio = 0f;
            m_WristMissingFrameCount = 0;
            m_HasLastElbow = false;
            m_LastElbowHipOffsetRatio = 0f;
            m_ElbowMissingFrameCount = 0;
            m_MotionSeconds = 0f;
        }
    }

    internal sealed class SingleHandSideSweepFlowState : IBoneGestureRuntimeState
    {
        public readonly SingleHandSideSweepHandState m_LeftHand = new SingleHandSideSweepHandState();
        public readonly SingleHandSideSweepHandState m_RightHand = new SingleHandSideSweepHandState();
        public int m_FirstPendingActionEventId;
        public int m_SecondPendingActionEventId;
        public int m_PendingActionEventCount;
        public bool m_HasLastTriggerTime;
        public float m_LastTriggerTimeSeconds;

        public void Reset()
        {
            ResetFlowState();
            m_FirstPendingActionEventId = 0;
            m_SecondPendingActionEventId = 0;
            m_PendingActionEventCount = 0;
            m_HasLastTriggerTime = false;
            m_LastTriggerTimeSeconds = 0f;
        }

        public void ResetFlowState()
        {
            m_LeftHand.Reset();
            m_RightHand.Reset();
        }

        public void AddPendingActionEvent(int actionEventId)
        {
            if (actionEventId <= 0 || m_PendingActionEventCount >= 2)
            {
                return;
            }

            if (m_PendingActionEventCount == 0)
            {
                m_FirstPendingActionEventId = actionEventId;
            }
            else
            {
                m_SecondPendingActionEventId = actionEventId;
            }

            m_PendingActionEventCount++;
        }

        public bool RemovePendingActionEvent(int actionEventId)
        {
            if (m_PendingActionEventCount <= 0 || actionEventId <= 0)
            {
                return false;
            }

            if (m_FirstPendingActionEventId == actionEventId)
            {
                m_FirstPendingActionEventId = m_SecondPendingActionEventId;
                m_SecondPendingActionEventId = 0;
                m_PendingActionEventCount--;
                return true;
            }

            if (m_PendingActionEventCount > 1 && m_SecondPendingActionEventId == actionEventId)
            {
                m_SecondPendingActionEventId = 0;
                m_PendingActionEventCount--;
                return true;
            }

            return false;
        }
    }

    internal sealed class SingleHandSideSweepFlowRecognizer : IBoneGestureRecognizer
    {
        private const string m_StateKey = "Flow.SingleHandSideSweep";

        public void CollectDefinitions(System.Collections.Generic.List<BoneGestureDefinition> definitions)
        {
            definitions.Add(new BoneGestureDefinition(
                BoneGestureType.单手外侧下方向中间上方挥手_流程,
                BoneGestureCategory.流程,
                BoneGesturePhaseMask.触发,
                BoneGesturePhaseMask.触发));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<SingleHandSideSweepFlowState>(m_StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            SingleHandSideSweepFlowState state =
                context.m_SeatState.ReadGestureState<SingleHandSideSweepFlowState>(m_StateKey);
            if (state == null || state.m_PendingActionEventCount > 0)
            {
                return;
            }

            if (!context.HasRecognizableActionBinding(BoneGestureType.单手外侧下方向中间上方挥手_流程))
            {
                state.ResetFlowState();
                return;
            }

            if (!EvaluateFlow(context, state, out bool leftTriggered, out bool rightTriggered))
            {
                return;
            }

            if (leftTriggered)
            {
                EmitTrigger(context, state, BoneGestureTriggerHand.左手);
            }

            if (rightTriggered)
            {
                EmitTrigger(context, state, BoneGestureTriggerHand.右手);
            }
        }

        public bool TryApplyConsumeResult(
            BoneGestureRuntimeContext context,
            BoneActionConsumeResult consumeResult)
        {
            SingleHandSideSweepFlowState state =
                context.m_SeatState.ReadGestureState<SingleHandSideSweepFlowState>(m_StateKey);
            return state != null &&
                consumeResult != null &&
                state.RemovePendingActionEvent(consumeResult.m_ActionEventId);
        }

        private static bool EvaluateFlow(
            BoneGestureRuntimeContext context,
            SingleHandSideSweepFlowState state,
            out bool leftTriggered,
            out bool rightTriggered)
        {
            leftTriggered = false;
            rightTriggered = false;
            if (!RunnerMovementFlowUtility.TryReadTorsoInput(context, out TorsoMovementInput input))
            {
                state.ResetFlowState();
                return false;
            }

            if (!TryReadBodyUpAxis(input, out BoneVector2 bodyUpAxis, out float shoulderSpan))
            {
                state.ResetFlowState();
                return false;
            }

            float minScore = context.m_Config.m_GestureKeypointMinConfidence;
            bool hasLeftWrist = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.左手腕,
                minScore,
                out BoneVector2 leftWrist);
            bool hasRightWrist = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.右手腕,
                minScore,
                out BoneVector2 rightWrist);
            bool hasLeftElbow = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.左肘,
                minScore,
                out BoneVector2 leftElbow);
            bool hasRightElbow = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.右肘,
                minScore,
                out BoneVector2 rightElbow);
            leftTriggered = EvaluateHand(
                context,
                state.m_LeftHand,
                input,
                AdditionalSkillFlowUtility.m_LeftSide,
                hasLeftWrist,
                leftWrist,
                hasLeftElbow,
                leftElbow,
                bodyUpAxis,
                shoulderSpan);
            rightTriggered = EvaluateHand(
                context,
                state.m_RightHand,
                input,
                AdditionalSkillFlowUtility.m_RightSide,
                hasRightWrist,
                rightWrist,
                hasRightElbow,
                rightElbow,
                bodyUpAxis,
                shoulderSpan);
            return leftTriggered || rightTriggered;
        }

        private static void EmitTrigger(
            BoneGestureRuntimeContext context,
            SingleHandSideSweepFlowState state,
            BoneGestureTriggerHand triggerHand)
        {
            float minIntervalSeconds = BoneMath.Max(
                0f,
                context.m_Config.m_SingleHandSideSweepMinTriggerIntervalSeconds);
            if (minIntervalSeconds > 0f &&
                state.m_HasLastTriggerTime &&
                context.m_FrameTimeSeconds - state.m_LastTriggerTimeSeconds < minIntervalSeconds)
            {
                return;
            }

            BoneActionEvent actionEvent = context.AddGestureEvent(
                BoneGestureType.单手外侧下方向中间上方挥手_流程,
                BoneGesturePhase.触发,
                triggerHand);
            if (actionEvent == null)
            {
                return;
            }

            state.m_HasLastTriggerTime = true;
            state.m_LastTriggerTimeSeconds = context.m_FrameTimeSeconds;
            if (actionEvent.m_RequiresConsumeResult)
            {
                state.AddPendingActionEvent(actionEvent.m_ActionEventId);
            }
        }

        private static bool EvaluateHand(
            BoneGestureRuntimeContext context,
            SingleHandSideSweepHandState state,
            TorsoMovementInput input,
            int side,
            bool hasWrist,
            BoneVector2 wrist,
            bool hasElbow,
            BoneVector2 elbow,
            BoneVector2 bodyUpAxis,
            float shoulderSpan)
        {
            if (!hasWrist)
            {
                int missingToleranceFrames = Math.Max(
                    0,
                    context.m_Config.m_SingleHandSideSweepWristMissingToleranceFrames);
                if (state.m_HasLastWrist &&
                    state.m_WristMissingFrameCount < missingToleranceFrames)
                {
                    state.m_WristMissingFrameCount++;
                    if (state.m_IsReady)
                    {
                        state.m_MotionSeconds += BoneMath.Max(0f, context.m_DeltaTimeSeconds);
                        if (state.m_MotionSeconds > context.m_Config.m_SingleHandSideSweepMaxDurationSeconds)
                        {
                            state.Reset();
                        }
                    }

                    return false;
                }

                state.Reset();
                return false;
            }

            state.m_WristMissingFrameCount = 0;
            if (hasElbow)
            {
                state.m_HasLastElbow = true;
                state.m_LastElbowHipOffsetRatio = ReadHipOffsetRatio(input, side, elbow, bodyUpAxis, shoulderSpan);
                state.m_ElbowMissingFrameCount = 0;
            }
            else
            {
                int missingToleranceFrames = Math.Max(
                    0,
                    context.m_Config.m_SingleHandSideSweepElbowMissingToleranceFrames);
                if (state.m_HasLastElbow &&
                    state.m_ElbowMissingFrameCount < missingToleranceFrames)
                {
                    state.m_ElbowMissingFrameCount++;
                }
                else if (state.m_IsMoving)
                {
                    state.Reset();
                    return false;
                }
                else
                {
                    state.m_HasLastElbow = false;
                    state.m_ElbowMissingFrameCount = 0;
                }
            }

            float wristHipOffsetRatio = ReadHipOffsetRatio(input, side, wrist, bodyUpAxis, shoulderSpan);
            bool hadLastWrist = state.m_HasLastWrist;
            float lastWristHipOffsetRatio = state.m_LastWristHipOffsetRatio;
            state.m_HasLastWrist = true;
            state.m_LastWristHipOffsetRatio = wristHipOffsetRatio;
            if (!state.m_IsReady)
            {
                if (ReadIsStartPose(context.m_Config, wristHipOffsetRatio))
                {
                    ArmAtHip(state, wristHipOffsetRatio);
                    return false;
                }

                float startTopRatio = -context.m_Config.m_SingleHandSideSweepStartAboveHipToleranceRatio;
                bool crossedStartUpward = hadLastWrist &&
                    lastWristHipOffsetRatio >= startTopRatio &&
                    wristHipOffsetRatio < startTopRatio;
                if (!crossedStartUpward)
                {
                    return false;
                }

                ArmAtHip(state, startTopRatio);
            }

            state.m_MotionSeconds += BoneMath.Max(0f, context.m_DeltaTimeSeconds);
            if (state.m_MotionSeconds > context.m_Config.m_SingleHandSideSweepMaxDurationSeconds)
            {
                state.Reset();
                return false;
            }

            float upwardDistanceRatio = state.m_ReadyWristHipOffsetRatio - wristHipOffsetRatio;
            if (!state.m_IsMoving)
            {
                if (upwardDistanceRatio < context.m_Config.m_SingleHandSideSweepStartMovementDistanceRatio)
                {
                    return false;
                }

                state.m_IsMoving = true;
            }

            bool hasReachedElbowHeight = state.m_HasLastElbow &&
                wristHipOffsetRatio <= state.m_LastElbowHipOffsetRatio +
                context.m_Config.m_SingleHandSideSweepElbowHeightToleranceRatio;
            bool triggered = hasReachedElbowHeight;
            if (triggered)
            {
                state.Reset();
            }

            return triggered;
        }

        private static float ReadHipOffsetRatio(
            TorsoMovementInput input,
            int side,
            BoneVector2 point,
            BoneVector2 bodyUpAxis,
            float shoulderSpan)
        {
            BoneVector2 sameSideHip = side == AdditionalSkillFlowUtility.m_LeftSide
                ? input.m_LeftHip
                : input.m_RightHip;
            BoneVector2 hipToPoint = point - sameSideHip;
            float heightAlongBodyUp = hipToPoint.m_X * bodyUpAxis.m_X +
                hipToPoint.m_Y * bodyUpAxis.m_Y;
            return -heightAlongBodyUp / shoulderSpan;
        }

        private static bool TryReadBodyUpAxis(
            TorsoMovementInput input,
            out BoneVector2 bodyUpAxis,
            out float shoulderSpan)
        {
            shoulderSpan = BoneMath.Distance(input.m_LeftShoulder, input.m_RightShoulder);
            if (shoulderSpan <= 0.0001f)
            {
                bodyUpAxis = BoneVector2.m_Zero;
                return false;
            }

            BoneVector2 torsoUp = input.m_ShoulderCenter - input.m_HipCenter;
            float lengthSquared = torsoUp.m_X * torsoUp.m_X + torsoUp.m_Y * torsoUp.m_Y;
            if (lengthSquared <= 0.0001f)
            {
                bodyUpAxis = BoneVector2.m_Zero;
                return false;
            }

            float inverseLength = 1f / (float)Math.Sqrt(lengthSquared);
            bodyUpAxis = torsoUp * inverseLength;
            return true;
        }

        private static bool ReadIsStartPose(
            BoneParserConfig config,
            float hipOffsetRatio)
        {
            return hipOffsetRatio >= -config.m_SingleHandSideSweepStartAboveHipToleranceRatio &&
                hipOffsetRatio <= config.m_SingleHandSideSweepStartBelowHipToleranceRatio;
        }

        private static void ArmAtHip(
            SingleHandSideSweepHandState state,
            float wristHipOffsetRatio)
        {
            state.m_IsReady = true;
            state.m_IsMoving = false;
            state.m_ReadyWristHipOffsetRatio = wristHipOffsetRatio;
            state.m_MotionSeconds = 0f;
        }
    }
}
