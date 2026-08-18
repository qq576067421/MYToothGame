using System;
using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal abstract class TriggerOnlyFlowState : IBoneGestureRuntimeState
    {
        public bool m_IsPending;
        public int m_ActionEventId;
        public float m_FrameTimeSeconds;
        public float m_CooldownUntilSeconds;

        public void Reset()
        {
            ResetFlowState();
            ResetPending();
            m_CooldownUntilSeconds = 0f;
        }

        public abstract void ResetFlowState();

        public void ResetPending()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_FrameTimeSeconds = 0f;
        }
    }

    internal abstract class TriggerOnlyFlowRecognizer<TState> : IBoneGestureRecognizer
        where TState : TriggerOnlyFlowState, new()
    {
        protected abstract string StateKey { get; }

        protected abstract BoneGestureType GestureType { get; }

        public void CollectDefinitions(List<BoneGestureDefinition> definitions)
        {
            definitions.Add(new BoneGestureDefinition(
                GestureType,
                BoneGestureCategory.流程,
                BoneGesturePhaseMask.触发,
                BoneGesturePhaseMask.触发));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<TState>(StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            TState state = context.m_SeatState.ReadGestureState<TState>(StateKey);
            if (state == null || state.m_IsPending)
            {
                return;
            }

            if (!context.HasRecognizableActionBinding(GestureType))
            {
                state.ResetFlowState();
                return;
            }

            if (context.m_FrameTimeSeconds > 0f && context.m_FrameTimeSeconds < state.m_CooldownUntilSeconds)
            {
                return;
            }

            if (!EvaluateFlow(context, state))
            {
                return;
            }

            BoneActionEvent actionEvent = context.AddGestureEvent(GestureType, BoneGesturePhase.触发);
            if (actionEvent == null)
            {
                OnBlocked(context, state);
                return;
            }

            if (actionEvent.m_RequiresConsumeResult)
            {
                state.m_IsPending = true;
                state.m_ActionEventId = actionEvent.m_ActionEventId;
                state.m_FrameTimeSeconds = context.m_FrameTimeSeconds;
                return;
            }

            OnAccepted(context, state, context.m_FrameTimeSeconds);
        }

        public bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult)
        {
            TState state = context.m_SeatState.ReadGestureState<TState>(StateKey);
            if (state == null || !state.m_IsPending || state.m_ActionEventId != consumeResult.m_ActionEventId)
            {
                return false;
            }

            switch (consumeResult.m_ResultType)
            {
                case BoneActionConsumeResultType.接受:
                    OnAccepted(context, state, state.m_FrameTimeSeconds);
                    break;
                case BoneActionConsumeResultType.拒绝可重试:
                    OnRetry(context, state);
                    break;
                case BoneActionConsumeResultType.拒绝阻断:
                case BoneActionConsumeResultType.忽略:
                case BoneActionConsumeResultType.未知:
                default:
                    OnBlocked(context, state);
                    break;
            }

            state.ResetPending();
            return true;
        }

        protected abstract bool EvaluateFlow(BoneGestureRuntimeContext context, TState state);

        protected abstract float ReadCooldownSeconds(BoneParserConfig config);

        protected virtual void OnAccepted(BoneGestureRuntimeContext context, TState state, float frameTimeSeconds)
        {
            state.ResetFlowState();
            state.m_CooldownUntilSeconds = frameTimeSeconds + ReadCooldownSeconds(context.m_Config);
        }

        protected virtual void OnRetry(BoneGestureRuntimeContext context, TState state)
        {
            state.ResetFlowState();
        }

        protected virtual void OnBlocked(BoneGestureRuntimeContext context, TState state)
        {
            state.ResetFlowState();
        }
    }

    internal struct UpperBodyFlowInput
    {
        public float m_ShoulderWidth;
        public float m_HeadTopY;
        public bool m_HasHeadTop;
        public BoneVector2 m_LeftShoulder;
        public BoneVector2 m_RightShoulder;
        public BoneVector2 m_ShoulderCenter;
        public BoneVector2 m_UpperBodyCenter;
        public BoneVector2 m_LeftWrist;
        public BoneVector2 m_RightWrist;
    }

    internal static class AdditionalSkillFlowUtility
    {
        public const int m_LeftSide = 1;
        public const int m_RightSide = 2;

        public static bool TryReadUpperBodyInput(BoneGestureRuntimeContext context, out UpperBodyFlowInput input)
        {
            input = new UpperBodyFlowInput();
            BoneTrackedPerson person = context.m_Person;
            if (person == null ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左肩, context.m_Config.m_GestureKeypointMinConfidence, out input.m_LeftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右肩, context.m_Config.m_GestureKeypointMinConfidence, out input.m_RightShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左手腕, context.m_Config.m_GestureKeypointMinConfidence, out input.m_LeftWrist) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右手腕, context.m_Config.m_GestureKeypointMinConfidence, out input.m_RightWrist) ||
                !context.m_Skeleton.TryReadUpperBodyCenter(person, out input.m_UpperBodyCenter))
            {
                return false;
            }

            input.m_ShoulderWidth = BoneMath.Abs(input.m_RightShoulder.m_X - input.m_LeftShoulder.m_X) + context.m_Config.m_ShoulderWidthEpsilon;
            if (input.m_ShoulderWidth <= context.m_Config.m_ShoulderWidthEpsilon)
            {
                return false;
            }

            input.m_ShoulderCenter = (input.m_LeftShoulder + input.m_RightShoulder) * 0.5f;
            input.m_HasHeadTop = context.m_Skeleton.TryReadHeadTop(person, out input.m_HeadTopY);
            return true;
        }

        public static int ReadSingleOverheadSide(BoneGestureRuntimeContext context, UpperBodyFlowInput input)
        {
            if (!input.m_HasHeadTop)
            {
                return 0;
            }

            float threshold = input.m_HeadTopY - input.m_ShoulderWidth * context.m_Config.m_PoseRaiseMarginRatio;
            bool leftOverhead = input.m_LeftWrist.m_Y <= threshold;
            bool rightOverhead = input.m_RightWrist.m_Y <= threshold;
            if (leftOverhead == rightOverhead)
            {
                return 0;
            }

            return leftOverhead ? m_LeftSide : m_RightSide;
        }

        public static BoneVector2 ReadWristBySide(UpperBodyFlowInput input, int side)
        {
            return side == m_LeftSide ? input.m_LeftWrist : input.m_RightWrist;
        }

        public static BoneVector2 ReadShoulderBySide(UpperBodyFlowInput input, int side)
        {
            return side == m_LeftSide ? input.m_LeftShoulder : input.m_RightShoulder;
        }

        public static bool ReadBothHandsAboveShoulder(UpperBodyFlowInput input, float aboveShoulderRatio)
        {
            float threshold = input.m_ShoulderCenter.m_Y - input.m_ShoulderWidth * aboveShoulderRatio;
            return input.m_LeftWrist.m_Y <= threshold && input.m_RightWrist.m_Y <= threshold;
        }

        public static bool ReadAnyHandAboveShoulder(UpperBodyFlowInput input, float aboveShoulderRatio)
        {
            float threshold = input.m_ShoulderCenter.m_Y - input.m_ShoulderWidth * aboveShoulderRatio;
            return input.m_LeftWrist.m_Y <= threshold || input.m_RightWrist.m_Y <= threshold;
        }

        public static bool ReadChestClose(UpperBodyFlowInput input, float closeDistanceRatio, float verticalRatio)
        {
            float wristDistance = BoneMath.Distance(input.m_LeftWrist, input.m_RightWrist);
            bool isClose = wristDistance <= input.m_ShoulderWidth * closeDistanceRatio;
            bool handsSwapped = input.m_LeftWrist.m_X > input.m_ShoulderCenter.m_X && input.m_RightWrist.m_X < input.m_ShoulderCenter.m_X;
            bool leftNearChest = BoneMath.Abs(input.m_LeftWrist.m_Y - input.m_UpperBodyCenter.m_Y) <= input.m_ShoulderWidth * verticalRatio;
            bool rightNearChest = BoneMath.Abs(input.m_RightWrist.m_Y - input.m_UpperBodyCenter.m_Y) <= input.m_ShoulderWidth * verticalRatio;
            return isClose && !handsSwapped && leftNearChest && rightNearChest;
        }

        public static bool ReadHandsExpanded(UpperBodyFlowInput input, float beyondShoulderRatio, float verticalToleranceRatio)
        {
            bool leftExpanded = input.m_LeftWrist.m_X <= input.m_LeftShoulder.m_X - input.m_ShoulderWidth * beyondShoulderRatio;
            bool rightExpanded = input.m_RightWrist.m_X >= input.m_RightShoulder.m_X + input.m_ShoulderWidth * beyondShoulderRatio;
            bool leftHeightValid = BoneMath.Abs(input.m_LeftWrist.m_Y - input.m_UpperBodyCenter.m_Y) <= input.m_ShoulderWidth * verticalToleranceRatio;
            bool rightHeightValid = BoneMath.Abs(input.m_RightWrist.m_Y - input.m_UpperBodyCenter.m_Y) <= input.m_ShoulderWidth * verticalToleranceRatio;
            return leftExpanded && rightExpanded && leftHeightValid && rightHeightValid;
        }

        public static bool ReadIsTimeout(int frameCount, int maxFrameCount)
        {
            return frameCount > Math.Max(1, maxFrameCount);
        }
    }

    internal sealed class SingleHandPullDownFlowState : TriggerOnlyFlowState
    {
        public int m_CandidateSide;
        public int m_CandidateFrameCount;
        public int m_ReadySide;
        public BoneVector2 m_ReadyWrist;
        public BoneVector2 m_LastWrist;
        public bool m_HasLastWrist;
        public int m_ReleaseFrameCount;

        public override void ResetFlowState()
        {
            m_CandidateSide = 0;
            m_CandidateFrameCount = 0;
            m_ReadySide = 0;
            m_ReadyWrist = BoneVector2.m_Zero;
            m_LastWrist = BoneVector2.m_Zero;
            m_HasLastWrist = false;
            m_ReleaseFrameCount = 0;
        }
    }

    internal sealed class SingleHandPullDownFlowRecognizer : TriggerOnlyFlowRecognizer<SingleHandPullDownFlowState>
    {
        protected override string StateKey => "Flow.SingleHandPullDown";

        protected override BoneGestureType GestureType => BoneGestureType.单手举过头下拉_流程;

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, SingleHandPullDownFlowState state)
        {
            if (!AdditionalSkillFlowUtility.TryReadUpperBodyInput(context, out UpperBodyFlowInput input))
            {
                state.ResetFlowState();
                return false;
            }

            int overheadSide = AdditionalSkillFlowUtility.ReadSingleOverheadSide(context, input);
            if (state.m_ReadySide == 0)
            {
                UpdateReadyState(context, state, input, overheadSide);
                return false;
            }

            if (overheadSide == state.m_ReadySide)
            {
                state.m_ReleaseFrameCount = 0;
                state.m_LastWrist = AdditionalSkillFlowUtility.ReadWristBySide(input, state.m_ReadySide);
                state.m_HasLastWrist = true;
                return false;
            }

            if (overheadSide != 0 && overheadSide != state.m_ReadySide)
            {
                state.ResetFlowState();
                return false;
            }

            state.m_ReleaseFrameCount++;
            if (AdditionalSkillFlowUtility.ReadIsTimeout(state.m_ReleaseFrameCount, context.m_Config.m_SingleHandPullDownReleaseWindowFrames))
            {
                state.ResetFlowState();
                return false;
            }

            BoneVector2 currentWrist = AdditionalSkillFlowUtility.ReadWristBySide(input, state.m_ReadySide);
            if (!state.m_HasLastWrist || context.m_DeltaTimeSeconds <= 0f)
            {
                state.m_LastWrist = currentWrist;
                state.m_HasLastWrist = true;
                return false;
            }

            BoneVector2 shoulder = AdditionalSkillFlowUtility.ReadShoulderBySide(input, state.m_ReadySide);
            float downDistance = currentWrist.m_Y - state.m_ReadyWrist.m_Y;
            float downSpeed = (currentWrist.m_Y - state.m_LastWrist.m_Y) / context.m_DeltaTimeSeconds;
            state.m_LastWrist = currentWrist;
            return downDistance >= input.m_ShoulderWidth * context.m_Config.m_SingleHandPullDownMinDistanceRatio &&
                downSpeed >= input.m_ShoulderWidth * context.m_Config.m_SingleHandPullDownReleaseSpeedRatioPerSecond &&
                currentWrist.m_Y >= shoulder.m_Y + input.m_ShoulderWidth * context.m_Config.m_SingleHandPullDownEndBelowShoulderRatio;
        }

        protected override float ReadCooldownSeconds(BoneParserConfig config)
        {
            return config.m_SingleHandPullDownCooldownSeconds;
        }

        private static void UpdateReadyState(
            BoneGestureRuntimeContext context,
            SingleHandPullDownFlowState state,
            UpperBodyFlowInput input,
            int overheadSide)
        {
            if (overheadSide == 0)
            {
                state.m_CandidateSide = 0;
                state.m_CandidateFrameCount = 0;
                return;
            }

            if (state.m_CandidateSide == overheadSide)
            {
                state.m_CandidateFrameCount++;
            }
            else
            {
                state.m_CandidateSide = overheadSide;
                state.m_CandidateFrameCount = 1;
            }

            if (state.m_CandidateFrameCount < Math.Max(1, context.m_Config.m_SingleHandPullDownReadyFrames))
            {
                return;
            }

            state.m_ReadySide = overheadSide;
            state.m_ReadyWrist = AdditionalSkillFlowUtility.ReadWristBySide(input, overheadSide);
            state.m_LastWrist = state.m_ReadyWrist;
            state.m_HasLastWrist = true;
            state.m_ReleaseFrameCount = 0;
        }
    }

    internal sealed class HandsOnHipRaiseFlowState : TriggerOnlyFlowState
    {
        public int m_ReadyFrameCount;
        public bool m_IsReady;
        public BoneVector2 m_ReadyLeftWrist;
        public BoneVector2 m_ReadyRightWrist;
        public int m_ReleaseFrameCount;

        public override void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReadyLeftWrist = BoneVector2.m_Zero;
            m_ReadyRightWrist = BoneVector2.m_Zero;
            m_ReleaseFrameCount = 0;
        }
    }

    internal sealed class HandsOnHipRaiseFlowRecognizer : TriggerOnlyFlowRecognizer<HandsOnHipRaiseFlowState>
    {
        protected override string StateKey => "Flow.HandsOnHipRaise";

        protected override BoneGestureType GestureType => BoneGestureType.双手叉腰后上举_流程;

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, HandsOnHipRaiseFlowState state)
        {
            if (!AdditionalSkillFlowUtility.TryReadUpperBodyInput(context, out UpperBodyFlowInput input))
            {
                state.ResetFlowState();
                return false;
            }

            bool handsOnHip = BonePoseDetection.ReadIsHandsOnHip(context);
            if (!state.m_IsReady)
            {
                if (!handsOnHip)
                {
                    state.m_ReadyFrameCount = 0;
                    return false;
                }

                state.m_ReadyFrameCount++;
                if (state.m_ReadyFrameCount >= Math.Max(1, context.m_Config.m_HandsOnHipRaiseReadyFrames))
                {
                    state.m_IsReady = true;
                    state.m_ReadyLeftWrist = input.m_LeftWrist;
                    state.m_ReadyRightWrist = input.m_RightWrist;
                }

                return false;
            }

            if (handsOnHip)
            {
                state.m_ReleaseFrameCount = 0;
                return false;
            }

            state.m_ReleaseFrameCount++;
            if (AdditionalSkillFlowUtility.ReadIsTimeout(state.m_ReleaseFrameCount, context.m_Config.m_HandsOnHipRaiseReleaseWindowFrames))
            {
                state.ResetFlowState();
                return false;
            }

            float leftRaiseDistance = state.m_ReadyLeftWrist.m_Y - input.m_LeftWrist.m_Y;
            float rightRaiseDistance = state.m_ReadyRightWrist.m_Y - input.m_RightWrist.m_Y;
            return AdditionalSkillFlowUtility.ReadBothHandsAboveShoulder(input, context.m_Config.m_HandsOnHipRaiseEndAboveShoulderRatio) &&
                leftRaiseDistance >= input.m_ShoulderWidth * context.m_Config.m_HandsOnHipRaiseMinDistanceRatio &&
                rightRaiseDistance >= input.m_ShoulderWidth * context.m_Config.m_HandsOnHipRaiseMinDistanceRatio;
        }

        protected override float ReadCooldownSeconds(BoneParserConfig config)
        {
            return config.m_HandsOnHipRaiseCooldownSeconds;
        }
    }

    internal sealed class CrouchStandRaiseFlowState : TriggerOnlyFlowState
    {
        public int m_ReadyFrameCount;
        public bool m_IsReady;
        public int m_ReleaseFrameCount;

        public override void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReleaseFrameCount = 0;
        }
    }

    internal sealed class CrouchStandRaiseFlowRecognizer : TriggerOnlyFlowRecognizer<CrouchStandRaiseFlowState>
    {
        protected override string StateKey => "Flow.CrouchStandRaise";

        protected override BoneGestureType GestureType => BoneGestureType.蹲下起身举手_流程;

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, CrouchStandRaiseFlowState state)
        {
            if (!AdditionalSkillFlowUtility.TryReadUpperBodyInput(context, out UpperBodyFlowInput input))
            {
                state.ResetFlowState();
                return false;
            }

            bool crouching = BonePoseDetection.ReadIsCrouching(context);
            if (!state.m_IsReady)
            {
                if (!crouching)
                {
                    state.m_ReadyFrameCount = 0;
                    return false;
                }

                state.m_ReadyFrameCount++;
                if (state.m_ReadyFrameCount >= Math.Max(1, context.m_Config.m_CrouchStandRaiseReadyFrames))
                {
                    state.m_IsReady = true;
                }

                return false;
            }

            if (crouching)
            {
                state.m_ReleaseFrameCount = 0;
                return false;
            }

            state.m_ReleaseFrameCount++;
            if (AdditionalSkillFlowUtility.ReadIsTimeout(state.m_ReleaseFrameCount, context.m_Config.m_CrouchStandRaiseReleaseWindowFrames))
            {
                state.ResetFlowState();
                return false;
            }

            return AdditionalSkillFlowUtility.ReadAnyHandAboveShoulder(input, context.m_Config.m_CrouchStandRaiseHandAboveShoulderRatio);
        }

        protected override float ReadCooldownSeconds(BoneParserConfig config)
        {
            return config.m_CrouchStandRaiseCooldownSeconds;
        }
    }

    internal sealed class ChestClosePushFlowState : TriggerOnlyFlowState
    {
        public int m_ReadyFrameCount;
        public bool m_IsReady;
        public BoneVector2 m_ReadyLeftWrist;
        public BoneVector2 m_ReadyRightWrist;
        public BoneVector2 m_LastLeftWrist;
        public BoneVector2 m_LastRightWrist;
        public bool m_HasLastWrist;
        public int m_ReleaseFrameCount;

        public override void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReadyLeftWrist = BoneVector2.m_Zero;
            m_ReadyRightWrist = BoneVector2.m_Zero;
            m_LastLeftWrist = BoneVector2.m_Zero;
            m_LastRightWrist = BoneVector2.m_Zero;
            m_HasLastWrist = false;
            m_ReleaseFrameCount = 0;
        }
    }

    internal sealed class ChestClosePushFlowRecognizer : TriggerOnlyFlowRecognizer<ChestClosePushFlowState>
    {
        protected override string StateKey => "Flow.ChestClosePush";

        protected override BoneGestureType GestureType => BoneGestureType.双手胸前合拢后上推_流程;

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, ChestClosePushFlowState state)
        {
            if (!AdditionalSkillFlowUtility.TryReadUpperBodyInput(context, out UpperBodyFlowInput input))
            {
                state.ResetFlowState();
                return false;
            }

            bool chestClose = AdditionalSkillFlowUtility.ReadChestClose(
                input,
                context.m_Config.m_ChestClosePushCloseDistanceRatio,
                context.m_Config.m_ChestClosePushVerticalRatio);
            if (!state.m_IsReady)
            {
                if (!chestClose)
                {
                    state.m_ReadyFrameCount = 0;
                    return false;
                }

                state.m_ReadyFrameCount++;
                if (state.m_ReadyFrameCount >= Math.Max(1, context.m_Config.m_ChestClosePushReadyFrames))
                {
                    state.m_IsReady = true;
                    state.m_ReadyLeftWrist = input.m_LeftWrist;
                    state.m_ReadyRightWrist = input.m_RightWrist;
                    state.m_LastLeftWrist = input.m_LeftWrist;
                    state.m_LastRightWrist = input.m_RightWrist;
                    state.m_HasLastWrist = true;
                }

                return false;
            }

            if (chestClose)
            {
                state.m_ReleaseFrameCount = 0;
                state.m_LastLeftWrist = input.m_LeftWrist;
                state.m_LastRightWrist = input.m_RightWrist;
                state.m_HasLastWrist = true;
                return false;
            }

            state.m_ReleaseFrameCount++;
            if (AdditionalSkillFlowUtility.ReadIsTimeout(state.m_ReleaseFrameCount, context.m_Config.m_ChestClosePushReleaseWindowFrames))
            {
                state.ResetFlowState();
                return false;
            }

            if (!state.m_HasLastWrist || context.m_DeltaTimeSeconds <= 0f)
            {
                state.m_LastLeftWrist = input.m_LeftWrist;
                state.m_LastRightWrist = input.m_RightWrist;
                state.m_HasLastWrist = true;
                return false;
            }

            float leftPushDistance = state.m_ReadyLeftWrist.m_Y - input.m_LeftWrist.m_Y;
            float rightPushDistance = state.m_ReadyRightWrist.m_Y - input.m_RightWrist.m_Y;
            float leftPushSpeed = (state.m_LastLeftWrist.m_Y - input.m_LeftWrist.m_Y) / context.m_DeltaTimeSeconds;
            float rightPushSpeed = (state.m_LastRightWrist.m_Y - input.m_RightWrist.m_Y) / context.m_DeltaTimeSeconds;
            state.m_LastLeftWrist = input.m_LeftWrist;
            state.m_LastRightWrist = input.m_RightWrist;
            return AdditionalSkillFlowUtility.ReadBothHandsAboveShoulder(input, context.m_Config.m_ChestClosePushEndAboveShoulderRatio) &&
                leftPushDistance >= input.m_ShoulderWidth * context.m_Config.m_ChestClosePushMinDistanceRatio &&
                rightPushDistance >= input.m_ShoulderWidth * context.m_Config.m_ChestClosePushMinDistanceRatio &&
                leftPushSpeed >= input.m_ShoulderWidth * context.m_Config.m_ChestClosePushSpeedRatioPerSecond &&
                rightPushSpeed >= input.m_ShoulderWidth * context.m_Config.m_ChestClosePushSpeedRatioPerSecond;
        }

        protected override float ReadCooldownSeconds(BoneParserConfig config)
        {
            return config.m_ChestClosePushCooldownSeconds;
        }
    }

    internal sealed class HandsExpandHoldFlowState : TriggerOnlyFlowState
    {
        public int m_ReadyFrameCount;
        public bool m_IsReady;
        public int m_ReleaseFrameCount;
        public int m_HoldFrameCount;

        public override void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReleaseFrameCount = 0;
            m_HoldFrameCount = 0;
        }
    }

    internal sealed class HandsExpandHoldFlowRecognizer : TriggerOnlyFlowRecognizer<HandsExpandHoldFlowState>
    {
        protected override string StateKey => "Flow.HandsExpandHold";

        protected override BoneGestureType GestureType => BoneGestureType.双手左右展开保持_流程;

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, HandsExpandHoldFlowState state)
        {
            if (!AdditionalSkillFlowUtility.TryReadUpperBodyInput(context, out UpperBodyFlowInput input))
            {
                state.ResetFlowState();
                return false;
            }

            bool chestClose = AdditionalSkillFlowUtility.ReadChestClose(
                input,
                context.m_Config.m_HandsExpandCloseDistanceRatio,
                context.m_Config.m_HandsExpandCloseVerticalRatio);
            if (!state.m_IsReady)
            {
                if (!chestClose)
                {
                    state.m_ReadyFrameCount = 0;
                    return false;
                }

                state.m_ReadyFrameCount++;
                if (state.m_ReadyFrameCount >= Math.Max(1, context.m_Config.m_HandsExpandReadyFrames))
                {
                    state.m_IsReady = true;
                }

                return false;
            }

            if (chestClose)
            {
                state.m_ReleaseFrameCount = 0;
                state.m_HoldFrameCount = 0;
                return false;
            }

            state.m_ReleaseFrameCount++;
            if (AdditionalSkillFlowUtility.ReadIsTimeout(state.m_ReleaseFrameCount, context.m_Config.m_HandsExpandReleaseWindowFrames))
            {
                state.ResetFlowState();
                return false;
            }

            if (AdditionalSkillFlowUtility.ReadHandsExpanded(
                    input,
                    context.m_Config.m_HandsExpandBeyondShoulderRatio,
                    context.m_Config.m_HandsExpandVerticalToleranceRatio))
            {
                state.m_HoldFrameCount++;
            }
            else
            {
                state.m_HoldFrameCount = 0;
            }

            return state.m_HoldFrameCount >= Math.Max(1, context.m_Config.m_HandsExpandHoldFrames);
        }

        protected override float ReadCooldownSeconds(BoneParserConfig config)
        {
            return config.m_HandsExpandCooldownSeconds;
        }
    }
}
