using System;
using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class CrossChestExpandFlowState : IBoneGestureRuntimeState
    {
        public int m_ReadyFrameCount;
        public bool m_IsReady;
        public float m_ReadyWristDistance;
        public bool m_IsReleaseCollecting;
        public int m_ReleaseFrameCount;
        public bool m_HasLastWristDistance;
        public float m_LastWristDistance;
        public float m_CooldownUntilSeconds;
        public bool m_IsPending;
        public int m_ActionEventId;
        public float m_FrameTimeSeconds;

        public void Reset()
        {
            ResetReadyState();
            m_CooldownUntilSeconds = 0f;
            ResetPending();
        }

        public void ResetReadyState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReadyWristDistance = 0f;
            m_HasLastWristDistance = false;
            m_LastWristDistance = 0f;
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
        }

        public void ResetReleaseWindow()
        {
            m_IsReleaseCollecting = false;
            m_ReleaseFrameCount = 0;
        }

        public void ResetPending()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_FrameTimeSeconds = 0f;
        }
    }

    internal sealed class CrossChestExpandFlowRecognizer : IBoneGestureRecognizer
    {
        private const string m_StateKey = "Flow.CrossChestExpand";

        public void CollectDefinitions(List<BoneGestureDefinition> definitions)
        {
            definitions.Add(new BoneGestureDefinition(
                BoneGestureType.双手交叉抱胸快速展开_流程,
                BoneGestureCategory.流程,
                BoneGesturePhaseMask.触发,
                BoneGesturePhaseMask.触发));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<CrossChestExpandFlowState>(m_StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            CrossChestExpandFlowState state = context.m_SeatState.ReadGestureState<CrossChestExpandFlowState>(m_StateKey);
            if (state.m_IsPending)
            {
                return;
            }

            if (!TryReadFlowInput(context, out float shoulderWidth, out BoneVector2 leftWrist, out BoneVector2 rightWrist))
            {
                state.Reset();
                return;
            }

            bool shouldEmitTrigger = EvaluateFlow(context, state, shoulderWidth, leftWrist, rightWrist);
            if (!shouldEmitTrigger)
            {
                return;
            }

            BoneActionEvent actionEvent = context.AddGestureEvent(
                BoneGestureType.双手交叉抱胸快速展开_流程,
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

        public bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult)
        {
            CrossChestExpandFlowState state = context.m_SeatState.ReadGestureState<CrossChestExpandFlowState>(m_StateKey);
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
            out BoneVector2 leftWrist,
            out BoneVector2 rightWrist)
        {
            shoulderWidth = 0f;
            leftWrist = BoneVector2.m_Zero;
            rightWrist = BoneVector2.m_Zero;

            if (context.m_Person == null ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左肩, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 leftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右肩, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 rightShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左手腕, context.m_Config.m_GestureKeypointMinConfidence, out leftWrist) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右手腕, context.m_Config.m_GestureKeypointMinConfidence, out rightWrist))
            {
                return false;
            }

            shoulderWidth = BoneMath.Abs(rightShoulder.m_X - leftShoulder.m_X) + context.m_Config.m_ShoulderWidthEpsilon;
            return true;
        }

        private static bool EvaluateFlow(
            BoneGestureRuntimeContext context,
            CrossChestExpandFlowState state,
            float shoulderWidth,
            BoneVector2 leftWrist,
            BoneVector2 rightWrist)
        {
            bool hasProcessGestureBinding =
                context.HasRecognizableActionBinding(BoneGestureType.双手交叉抱胸快速展开_流程);
            if (!hasProcessGestureBinding)
            {
                state.ResetReadyState();
                return false;
            }

            if (context.m_FrameTimeSeconds > 0f && context.m_FrameTimeSeconds < state.m_CooldownUntilSeconds)
            {
                return false;
            }

            bool isCrossChest = BonePoseDetection.ReadIsCrossChest(context);
            float wristDistance = ReadHorizontalWristDistance(leftWrist, rightWrist);
            if (!state.m_IsReady)
            {
                if (isCrossChest)
                {
                    state.m_ReadyFrameCount++;
                    if (state.m_ReadyFrameCount >= Math.Max(1, context.m_Config.m_CrossChestExpandReadyFrames))
                    {
                        state.m_IsReady = true;
                        state.m_ReadyWristDistance = wristDistance;
                        state.m_LastWristDistance = wristDistance;
                        state.m_HasLastWristDistance = true;
                        state.ResetReleaseWindow();
                    }
                }
                else
                {
                    state.m_ReadyFrameCount = 0;
                }

                return false;
            }

            if (isCrossChest)
            {
                state.ResetReleaseWindow();
                state.m_LastWristDistance = wristDistance;
                state.m_HasLastWristDistance = true;
                return false;
            }

            state.StartReleaseWindow();
            state.m_ReleaseFrameCount++;

            if (!state.m_HasLastWristDistance || context.m_DeltaTimeSeconds <= 0f)
            {
                state.m_LastWristDistance = wristDistance;
                state.m_HasLastWristDistance = true;
                if (ReadIsReleaseWindowTimeout(context, state))
                {
                    state.ResetReadyState();
                }

                return false;
            }

            float expandSpeedThreshold = shoulderWidth * context.m_Config.m_CrossChestExpandSpeedRatioPerSecond;
            float expandDistanceThreshold = shoulderWidth * context.m_Config.m_CrossChestExpandMinDistanceRatio;
            float expandSpeed = BoneMath.Max(0f, (wristDistance - state.m_LastWristDistance) / context.m_DeltaTimeSeconds);
            float expandDistance = BoneMath.Max(0f, wristDistance - state.m_ReadyWristDistance);
            state.m_LastWristDistance = wristDistance;

            if (expandSpeed >= expandSpeedThreshold && expandDistance >= expandDistanceThreshold)
            {
                return true;
            }

            if (ReadIsReleaseWindowTimeout(context, state))
            {
                state.ResetReadyState();
            }

            return false;
        }

        private static float ReadHorizontalWristDistance(BoneVector2 leftWrist, BoneVector2 rightWrist)
        {
            return BoneMath.Abs(leftWrist.m_X - rightWrist.m_X);
        }

        private static bool ReadIsReleaseWindowTimeout(BoneGestureRuntimeContext context, CrossChestExpandFlowState state)
        {
            return state.m_IsReleaseCollecting &&
                state.m_ReleaseFrameCount > Math.Max(1, context.m_Config.m_CrossChestExpandReleaseWindowFrames);
        }

        private static void PendingAction(CrossChestExpandFlowState state, int actionEventId, float frameTimeSeconds)
        {
            state.m_IsPending = true;
            state.m_ActionEventId = actionEventId;
            state.m_FrameTimeSeconds = frameTimeSeconds;
        }

        private static void SubmitAccepted(
            BoneGestureRuntimeContext context,
            CrossChestExpandFlowState state,
            float frameTimeSeconds)
        {
            state.ResetReadyState();
            state.m_CooldownUntilSeconds = frameTimeSeconds + context.m_Config.m_CrossChestExpandCooldownSeconds;
        }

        private static void SubmitRetry(CrossChestExpandFlowState state)
        {
            state.m_IsReady = true;
            state.ResetReleaseWindow();
        }

        private static void SubmitBlocked(CrossChestExpandFlowState state)
        {
            state.ResetReadyState();
        }
    }
}
