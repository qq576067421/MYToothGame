using System;
using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class OverheadPressReleaseFlowState : IBoneGestureRuntimeState
    {
        public bool m_HasLastLeftWrist;
        public BoneVector2 m_LastLeftWrist;
        public bool m_HasLastRightWrist;
        public BoneVector2 m_LastRightWrist;
        public int m_ReadyFrameCount;
        public bool m_IsReady;
        public BoneVector2 m_ReadyLeftWrist;
        public BoneVector2 m_ReadyRightWrist;
        public bool m_IsReleaseCollecting;
        public int m_ReleaseFrameCount;
        public bool m_HasLeftRelease;
        public bool m_HasRightRelease;
        public float m_CooldownUntilSeconds;
        public bool m_LastActive;
        public bool m_IsPending;
        public int m_ActionEventId;
        public float m_FrameTimeSeconds;

        public void Reset()
        {
            m_HasLastLeftWrist = false;
            m_LastLeftWrist = BoneVector2.m_Zero;
            m_HasLastRightWrist = false;
            m_LastRightWrist = BoneVector2.m_Zero;
            ResetReadyState();
            m_CooldownUntilSeconds = 0f;
            m_LastActive = false;
            ResetPending();
        }

        public void ResetReadyState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReadyLeftWrist = BoneVector2.m_Zero;
            m_ReadyRightWrist = BoneVector2.m_Zero;
            ResetReleaseWindow();
        }

        public void StartReleaseWindow()
        {
            if (m_IsReleaseCollecting)
            {
                return;
            }

            m_IsReleaseCollecting = true;
            m_ReleaseFrameCount = 0;
            m_HasLeftRelease = false;
            m_HasRightRelease = false;
        }

        public void ResetReleaseWindow()
        {
            m_IsReleaseCollecting = false;
            m_ReleaseFrameCount = 0;
            m_HasLeftRelease = false;
            m_HasRightRelease = false;
        }

        public void ResetPending()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_FrameTimeSeconds = 0f;
        }
    }

    internal sealed class OverheadPressReleaseFlowRecognizer : IBoneGestureRecognizer
    {
        private const string m_StateKey = "Flow.OverheadPressRelease";

        public void CollectDefinitions(List<BoneGestureDefinition> definitions)
        {
            definitions.Add(new BoneGestureDefinition(
                BoneGestureType.双手过头下压释放_流程,
                BoneGestureCategory.流程,
                BoneGesturePhaseMask.开始 | BoneGesturePhaseMask.持续 | BoneGesturePhaseMask.结束 | BoneGesturePhaseMask.触发,
                BoneGesturePhaseMask.触发));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<OverheadPressReleaseFlowState>(m_StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            OverheadPressReleaseFlowState state = context.m_SeatState.ReadGestureState<OverheadPressReleaseFlowState>(m_StateKey);
            bool previousActive = state.m_LastActive;
            if (!TryReadFlowInput(context, out float shoulderWidth, out bool hasLeftWrist, out BoneVector2 leftWrist, out bool hasRightWrist, out BoneVector2 rightWrist))
            {
                EndIfNeeded(context, state, previousActive);
                state.Reset();
                return;
            }

            bool currentActive;
            bool shouldEmitTrigger = false;
            if (!state.m_IsPending)
            {
                EvaluateFlow(
                    context,
                    state,
                    shoulderWidth,
                    hasLeftWrist,
                    leftWrist,
                    hasRightWrist,
                    rightWrist,
                    out currentActive,
                    out shouldEmitTrigger);
            }
            else
            {
                currentActive = state.m_LastActive;
            }

            context.AddBooleanGestureEvent(
                previousActive,
                currentActive,
                BoneGestureType.双手过头下压释放_流程);
            state.m_LastActive = currentActive;

            if (shouldEmitTrigger)
            {
                BoneActionEvent actionEvent = context.AddGestureEvent(
                    BoneGestureType.双手过头下压释放_流程,
                    BoneGesturePhase.触发);
                if (actionEvent == null)
                {
                    SubmitBlocked(state);
                }
                else if (actionEvent.m_RequiresConsumeResult)
                {
                    PendingAction(state, actionEvent.m_ActionEventId, context.m_FrameTimeSeconds);
                }
                else
                {
                    SubmitAccepted(context, state, context.m_FrameTimeSeconds);
                }
            }

            UpdateLastWrist(state, hasLeftWrist, leftWrist, hasRightWrist, rightWrist);
        }

        public bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult)
        {
            OverheadPressReleaseFlowState state = context.m_SeatState.ReadGestureState<OverheadPressReleaseFlowState>(m_StateKey);
            if (state == null || !state.m_IsPending || state.m_ActionEventId != consumeResult.m_ActionEventId)
            {
                return false;
            }

            switch (consumeResult.m_ResultType)
            {
                case BoneActionConsumeResultType.接受:
                    SubmitAccepted(context, state, state.m_FrameTimeSeconds);
                    break;
                case BoneActionConsumeResultType.拒绝可重试:
                    SubmitRetry(state);
                    break;
                case BoneActionConsumeResultType.拒绝阻断:
                case BoneActionConsumeResultType.忽略:
                case BoneActionConsumeResultType.未知:
                default:
                    SubmitBlocked(state);
                    break;
            }

            state.ResetPending();
            return true;
        }

        private static bool TryReadFlowInput(
            BoneGestureRuntimeContext context,
            out float shoulderWidth,
            out bool hasLeftWrist,
            out BoneVector2 leftWrist,
            out bool hasRightWrist,
            out BoneVector2 rightWrist)
        {
            shoulderWidth = 0f;
            hasLeftWrist = false;
            leftWrist = BoneVector2.m_Zero;
            hasRightWrist = false;
            rightWrist = BoneVector2.m_Zero;

            if (context.m_Person == null ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左肩, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 leftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右肩, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 rightShoulder))
            {
                return false;
            }

            shoulderWidth = BoneMath.Abs(rightShoulder.m_X - leftShoulder.m_X) + context.m_Config.m_ShoulderWidthEpsilon;
            hasLeftWrist = BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左手腕, context.m_Config.m_GestureKeypointMinConfidence, out leftWrist);
            hasRightWrist = BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右手腕, context.m_Config.m_GestureKeypointMinConfidence, out rightWrist);
            return true;
        }

        private static void EvaluateFlow(
            BoneGestureRuntimeContext context,
            OverheadPressReleaseFlowState state,
            float shoulderWidth,
            bool hasLeftWrist,
            BoneVector2 leftWrist,
            bool hasRightWrist,
            BoneVector2 rightWrist,
            out bool isActive,
            out bool shouldEmitTrigger)
        {
            isActive = false;
            shouldEmitTrigger = false;
            bool hasProcessGestureBinding =
                context.HasRecognizableActionBinding(BoneGestureType.双手过头下压释放_流程);
            if ((context.m_SeatDefinition == null || !context.m_SeatDefinition.m_IsProcessGestureEnabled) && !hasProcessGestureBinding)
            {
                state.ResetReadyState();
                return;
            }

            if (context.m_FrameTimeSeconds > 0f && context.m_FrameTimeSeconds < state.m_CooldownUntilSeconds)
            {
                return;
            }

            if (!context.m_Skeleton.TryReadHeadTop(context.m_Person, out float headTopY))
            {
                state.ResetReadyState();
                return;
            }

            bool hasBothWrist = hasLeftWrist && hasRightWrist;
            if (!hasBothWrist)
            {
                if (!state.m_IsReady)
                {
                    state.ResetReadyState();
                    return;
                }

                state.StartReleaseWindow();
                state.m_ReleaseFrameCount++;
                if (ReadIsReleaseWindowTimeout(context, state))
                {
                    state.ResetReadyState();
                }
                else
                {
                    isActive = true;
                }

                return;
            }

            float overheadThreshold = headTopY - shoulderWidth * context.m_Config.m_OverheadPressHeadMarginRatio;
            bool bothOverhead = leftWrist.m_Y <= overheadThreshold && rightWrist.m_Y <= overheadThreshold;
            isActive = bothOverhead || state.m_IsReady;
            if (!state.m_IsReady)
            {
                if (bothOverhead)
                {
                    state.m_ReadyFrameCount++;
                    if (state.m_ReadyFrameCount >= context.m_Config.m_OverheadPressReadyFrames)
                    {
                        state.m_IsReady = true;
                        state.m_ReadyLeftWrist = leftWrist;
                        state.m_ReadyRightWrist = rightWrist;
                        isActive = true;
                    }
                }
                else
                {
                    state.m_ReadyFrameCount = 0;
                }

                return;
            }

            if (bothOverhead)
            {
                state.m_ReadyFrameCount++;
                state.ResetReleaseWindow();
                isActive = true;
                return;
            }

            if (context.m_DeltaTimeSeconds <= 0f)
            {
                state.StartReleaseWindow();
                state.m_ReleaseFrameCount++;
                if (ReadIsReleaseWindowTimeout(context, state))
                {
                    state.ResetReadyState();
                    isActive = false;
                }
                else
                {
                    isActive = true;
                }

                return;
            }

            state.StartReleaseWindow();
            state.m_ReleaseFrameCount++;

            float releaseThreshold = shoulderWidth * context.m_Config.m_OverheadPressReleaseSpeedRatio;
            float releaseDistanceThreshold = shoulderWidth * context.m_Config.m_OverheadPressMinReleaseDistanceRatio;
            float releaseEndY = headTopY + shoulderWidth * context.m_Config.m_OverheadPressReleaseEndBelowHeadRatio;
            if (!state.m_HasLeftRelease &&
                ReadSingleHandReleaseCompleted(
                    state.m_HasLastLeftWrist,
                    state.m_LastLeftWrist,
                    leftWrist,
                    state.m_ReadyLeftWrist,
                    context.m_DeltaTimeSeconds,
                    releaseThreshold,
                    releaseDistanceThreshold,
                    releaseEndY))
            {
                state.m_HasLeftRelease = true;
            }

            if (!state.m_HasRightRelease &&
                ReadSingleHandReleaseCompleted(
                    state.m_HasLastRightWrist,
                    state.m_LastRightWrist,
                    rightWrist,
                    state.m_ReadyRightWrist,
                    context.m_DeltaTimeSeconds,
                    releaseThreshold,
                    releaseDistanceThreshold,
                    releaseEndY))
            {
                state.m_HasRightRelease = true;
            }

            if (state.m_HasLeftRelease && state.m_HasRightRelease)
            {
                shouldEmitTrigger = true;
                isActive = false;
            }
            else if (ReadIsReleaseWindowTimeout(context, state))
            {
                state.ResetReadyState();
                isActive = false;
            }
            else
            {
                isActive = true;
            }
        }

        private static bool ReadSingleHandReleaseCompleted(
            bool hasLastWrist,
            BoneVector2 lastWrist,
            BoneVector2 wrist,
            BoneVector2 readyWrist,
            float deltaTimeSeconds,
            float releaseThreshold,
            float releaseDistanceThreshold,
            float releaseEndY)
        {
            if (!hasLastWrist || deltaTimeSeconds <= 0f)
            {
                return false;
            }

            float downSpeed = BoneMath.Max(0f, (wrist.m_Y - lastWrist.m_Y) / deltaTimeSeconds);
            float downDistance = BoneMath.Max(0f, wrist.m_Y - readyWrist.m_Y);
            return downSpeed >= releaseThreshold &&
                downDistance >= releaseDistanceThreshold &&
                wrist.m_Y >= releaseEndY;
        }

        private static bool ReadIsReleaseWindowTimeout(BoneGestureRuntimeContext context, OverheadPressReleaseFlowState state)
        {
            return state.m_IsReleaseCollecting &&
                state.m_ReleaseFrameCount > Math.Max(1, context.m_Config.m_OverheadPressReleaseWindowFrames);
        }

        private static void PendingAction(OverheadPressReleaseFlowState state, int actionEventId, float frameTimeSeconds)
        {
            state.m_IsPending = true;
            state.m_ActionEventId = actionEventId;
            state.m_FrameTimeSeconds = frameTimeSeconds;
        }

        private static void SubmitAccepted(
            BoneGestureRuntimeContext context,
            OverheadPressReleaseFlowState state,
            float frameTimeSeconds)
        {
            state.ResetReadyState();
            state.m_CooldownUntilSeconds = frameTimeSeconds + context.m_Config.m_OverheadPressCooldownSeconds;
            state.m_LastActive = false;
        }

        private static void SubmitRetry(OverheadPressReleaseFlowState state)
        {
            state.m_IsReady = true;
            state.ResetReleaseWindow();
            state.m_LastActive = false;
        }

        private static void SubmitBlocked(OverheadPressReleaseFlowState state)
        {
            state.ResetReadyState();
            state.m_LastActive = false;
        }

        private static void EndIfNeeded(
            BoneGestureRuntimeContext context,
            OverheadPressReleaseFlowState state,
            bool previousActive)
        {
            if (!previousActive)
            {
                return;
            }

            context.AddGestureEvent(BoneGestureType.双手过头下压释放_流程, BoneGesturePhase.结束);
            state.m_LastActive = false;
        }

        private static void UpdateLastWrist(
            OverheadPressReleaseFlowState state,
            bool hasLeftWrist,
            BoneVector2 leftWrist,
            bool hasRightWrist,
            BoneVector2 rightWrist)
        {
            if (hasLeftWrist)
            {
                state.m_LastLeftWrist = leftWrist;
                state.m_HasLastLeftWrist = true;
            }
            else
            {
                state.m_HasLastLeftWrist = false;
            }

            if (hasRightWrist)
            {
                state.m_LastRightWrist = rightWrist;
                state.m_HasLastRightWrist = true;
            }
            else
            {
                state.m_HasLastRightWrist = false;
            }
        }
    }
}
