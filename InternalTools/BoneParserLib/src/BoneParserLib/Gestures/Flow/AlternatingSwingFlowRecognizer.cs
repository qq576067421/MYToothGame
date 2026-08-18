using System;
using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class AlternatingSwingHandState
    {
        public bool m_HasLastRelativeWristY;
        public float m_LastRelativeWristY;
        public float m_StrokeStartRelativeWristY;
        public float m_StrokeDurationSeconds;
        public int m_StrokeDirection;
        public int m_DirectionalFrameCount;
        public int m_IdleFrameCount;

        public void Reset()
        {
            m_HasLastRelativeWristY = false;
            m_LastRelativeWristY = 0f;
            ResetStroke(0f);
        }

        public void ResetStroke(float relativeWristY)
        {
            m_StrokeStartRelativeWristY = relativeWristY;
            m_StrokeDurationSeconds = 0f;
            m_StrokeDirection = 0;
            m_DirectionalFrameCount = 0;
            m_IdleFrameCount = 0;
        }
    }

    internal sealed class AlternatingSwingFlowState : IBoneGestureRuntimeState
    {
        public readonly AlternatingSwingHandState m_LeftHand = new AlternatingSwingHandState();
        public readonly AlternatingSwingHandState m_RightHand = new AlternatingSwingHandState();
        public int m_LastAlternatingSideMarker;
        public int m_LastAlternatingFrameSerial;
        public float m_LastLeftAlternatingTimeSeconds;
        public float m_LastRightAlternatingTimeSeconds;
        public bool m_IsPending;
        public int m_ActionEventId;
        public bool m_LeftSwingDetected;
        public bool m_RightSwingDetected;
        public float m_FrameTimeSeconds;
        public int m_FrameSerial;

        public void Reset()
        {
            m_LeftHand.Reset();
            m_RightHand.Reset();
            m_LastAlternatingSideMarker = 0;
            m_LastAlternatingFrameSerial = 0;
            m_LastLeftAlternatingTimeSeconds = 0f;
            m_LastRightAlternatingTimeSeconds = 0f;
            ResetPending();
        }

        public void ResetPending()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_LeftSwingDetected = false;
            m_RightSwingDetected = false;
            m_FrameTimeSeconds = 0f;
            m_FrameSerial = 0;
        }
    }

    internal sealed class AlternatingSwingFlowRecognizer : IBoneGestureRecognizer
    {
        private const string m_StateKey = "Flow.AlternatingSwing";
        private const int m_LeftSideMarker = 1;
        private const int m_RightSideMarker = 2;

        public void CollectDefinitions(List<BoneGestureDefinition> definitions)
        {
            definitions.Add(new BoneGestureDefinition(
                BoneGestureType.左右交替挥击_流程,
                BoneGestureCategory.流程,
                BoneGesturePhaseMask.触发,
                BoneGesturePhaseMask.触发));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<AlternatingSwingFlowState>(m_StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            AlternatingSwingFlowState state = context.m_SeatState.ReadGestureState<AlternatingSwingFlowState>(m_StateKey);
            if (!TryReadFlowInput(
                    context,
                    out float shoulderWidth,
                    out bool hasLeftWrist,
                    out float leftRelativeWristY,
                    out bool hasRightWrist,
                    out float rightRelativeWristY))
            {
                state.Reset();
                return;
            }

            bool leftSwingDetected = false;
            bool rightSwingDetected = false;
            bool shouldEmitTrigger = false;
            if (!state.m_IsPending)
            {
                shouldEmitTrigger = EvaluateSwing(
                    context,
                    state,
                    shoulderWidth,
                    hasLeftWrist,
                    leftRelativeWristY,
                    hasRightWrist,
                    rightRelativeWristY,
                    out leftSwingDetected,
                    out rightSwingDetected);
            }
            else
            {
                SynchronizeHandSample(state.m_LeftHand, hasLeftWrist, leftRelativeWristY);
                SynchronizeHandSample(state.m_RightHand, hasRightWrist, rightRelativeWristY);
            }

            if (shouldEmitTrigger)
            {
                BoneActionEvent actionEvent = context.AddGestureEvent(
                    BoneGestureType.左右交替挥击_流程,
                    BoneGesturePhase.触发);
                if (actionEvent != null && actionEvent.m_RequiresConsumeResult)
                {
                    PendingAction(
                        state,
                        actionEvent.m_ActionEventId,
                        leftSwingDetected,
                        rightSwingDetected,
                        context.m_FrameTimeSeconds,
                        context.ReadFrameSerial());
                }
                else if (actionEvent != null)
                {
                    SubmitSwing(
                        state,
                        leftSwingDetected,
                        rightSwingDetected,
                        context.m_FrameTimeSeconds,
                        context.ReadFrameSerial());
                }
            }

            UpdateHandTracking(state.m_LeftHand, hasLeftWrist);
            UpdateHandTracking(state.m_RightHand, hasRightWrist);
        }

        public bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult)
        {
            AlternatingSwingFlowState state = context.m_SeatState.ReadGestureState<AlternatingSwingFlowState>(m_StateKey);
            if (state == null || !state.m_IsPending || state.m_ActionEventId != consumeResult.m_ActionEventId)
            {
                return false;
            }

            if (consumeResult.m_ResultType == BoneActionConsumeResultType.接受)
            {
                SubmitSwing(
                    state,
                    state.m_LeftSwingDetected,
                    state.m_RightSwingDetected,
                    state.m_FrameTimeSeconds,
                    state.m_FrameSerial);
            }

            state.ResetPending();
            return true;
        }

        private static bool TryReadFlowInput(
            BoneGestureRuntimeContext context,
            out float shoulderWidth,
            out bool hasLeftWrist,
            out float leftRelativeWristY,
            out bool hasRightWrist,
            out float rightRelativeWristY)
        {
            shoulderWidth = 0f;
            hasLeftWrist = false;
            leftRelativeWristY = 0f;
            hasRightWrist = false;
            rightRelativeWristY = 0f;

            if (context.m_Person == null ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左肩, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 leftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右肩, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 rightShoulder))
            {
                return false;
            }

            shoulderWidth = BoneMath.Abs(rightShoulder.m_X - leftShoulder.m_X) + context.m_Config.m_ShoulderWidthEpsilon;
            hasLeftWrist = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.左手腕,
                context.m_Config.m_GestureKeypointMinConfidence,
                out BoneVector2 leftWrist);
            hasRightWrist = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.右手腕,
                context.m_Config.m_GestureKeypointMinConfidence,
                out BoneVector2 rightWrist);
            if (hasLeftWrist)
            {
                leftRelativeWristY = leftWrist.m_Y - leftShoulder.m_Y;
            }

            if (hasRightWrist)
            {
                rightRelativeWristY = rightWrist.m_Y - rightShoulder.m_Y;
            }

            return true;
        }

        private static bool EvaluateSwing(
            BoneGestureRuntimeContext context,
            AlternatingSwingFlowState state,
            float shoulderWidth,
            bool hasLeftWrist,
            float leftRelativeWristY,
            bool hasRightWrist,
            float rightRelativeWristY,
            out bool leftSwingDetected,
            out bool rightSwingDetected)
        {
            leftSwingDetected = false;
            rightSwingDetected = false;
            if (state == null || context.m_DeltaTimeSeconds <= 0f)
            {
                return false;
            }

            if (hasLeftWrist)
            {
                bool leftMotionCompleted = EvaluateHandSwing(
                    state.m_LeftHand,
                    leftRelativeWristY,
                    shoulderWidth,
                    context.m_DeltaTimeSeconds,
                    context.m_Config);
                leftSwingDetected = leftMotionCompleted &&
                    context.m_FrameTimeSeconds - state.m_LastLeftAlternatingTimeSeconds >= context.m_Config.m_AlternatingSwingCooldownSeconds;
            }

            if (hasRightWrist)
            {
                bool rightMotionCompleted = EvaluateHandSwing(
                    state.m_RightHand,
                    rightRelativeWristY,
                    shoulderWidth,
                    context.m_DeltaTimeSeconds,
                    context.m_Config);
                rightSwingDetected = rightMotionCompleted &&
                    context.m_FrameTimeSeconds - state.m_LastRightAlternatingTimeSeconds >= context.m_Config.m_AlternatingSwingCooldownSeconds;
            }

            if (leftSwingDetected == rightSwingDetected)
            {
                return false;
            }

            int currentSideMarker = leftSwingDetected ? m_LeftSideMarker : m_RightSideMarker;
            bool hasPreviousSide = state.m_LastAlternatingSideMarker != 0;
            bool isOppositeSide = hasPreviousSide && state.m_LastAlternatingSideMarker != currentSideMarker;
            int maxWindowFrames = Math.Max(1, context.m_Config.m_AlternatingSwingWindowFrames);
            bool isInsideWindow =
                context.ReadFrameSerial() <= 0 ||
                state.m_LastAlternatingFrameSerial <= 0 ||
                context.ReadFrameSerial() - state.m_LastAlternatingFrameSerial <= maxWindowFrames;
            if (!isOppositeSide || !isInsideWindow)
            {
                SubmitSwing(state, leftSwingDetected, rightSwingDetected, context.m_FrameTimeSeconds, context.ReadFrameSerial());
                return false;
            }

            return true;
        }

        private static bool EvaluateHandSwing(
            AlternatingSwingHandState handState,
            float relativeWristY,
            float shoulderWidth,
            float deltaTimeSeconds,
            BoneParserConfig config)
        {
            if (!handState.m_HasLastRelativeWristY)
            {
                handState.m_HasLastRelativeWristY = true;
                handState.m_LastRelativeWristY = relativeWristY;
                handState.ResetStroke(relativeWristY);
                return false;
            }

            float previousRelativeWristY = handState.m_LastRelativeWristY;
            float frameDelta = relativeWristY - previousRelativeWristY;
            handState.m_LastRelativeWristY = relativeWristY;

            float directionNoise = shoulderWidth * BoneMath.Max(0f, config.m_AlternatingSwingDirectionNoiseRatio);
            int minDirectionalFrames = Math.Max(2, config.m_AlternatingSwingMinDirectionalFrames);
            if (BoneMath.Abs(frameDelta) < directionNoise)
            {
                if (handState.m_StrokeDirection != 0)
                {
                    handState.m_IdleFrameCount++;
                    handState.m_StrokeDurationSeconds += deltaTimeSeconds;
                    if (handState.m_DirectionalFrameCount < minDirectionalFrames || handState.m_IdleFrameCount > 1)
                    {
                        handState.ResetStroke(relativeWristY);
                    }
                }

                return false;
            }

            int currentDirection = frameDelta > 0f ? 1 : -1;
            if (handState.m_StrokeDirection != currentDirection)
            {
                handState.m_StrokeStartRelativeWristY = previousRelativeWristY;
                handState.m_StrokeDurationSeconds = deltaTimeSeconds;
                handState.m_StrokeDirection = currentDirection;
                handState.m_DirectionalFrameCount = 1;
                handState.m_IdleFrameCount = 0;
            }
            else
            {
                handState.m_StrokeDurationSeconds += deltaTimeSeconds;
                handState.m_DirectionalFrameCount++;
                handState.m_IdleFrameCount = 0;
            }

            float verticalDistance = BoneMath.Abs(relativeWristY - handState.m_StrokeStartRelativeWristY);
            float verticalSpeed = verticalDistance / BoneMath.Max(handState.m_StrokeDurationSeconds, 1f / 120f);
            bool isCompleted =
                handState.m_DirectionalFrameCount >= minDirectionalFrames &&
                verticalDistance >= shoulderWidth * config.m_AlternatingSwingMinVerticalDistanceRatio &&
                verticalSpeed >= shoulderWidth * config.m_AlternatingSwingSpeedRatioPerSecond;
            if (isCompleted)
            {
                handState.ResetStroke(relativeWristY);
            }

            return isCompleted;
        }

        private static void PendingAction(
            AlternatingSwingFlowState state,
            int actionEventId,
            bool leftSwingDetected,
            bool rightSwingDetected,
            float frameTimeSeconds,
            int frameSerial)
        {
            state.m_IsPending = true;
            state.m_ActionEventId = actionEventId;
            state.m_LeftSwingDetected = leftSwingDetected;
            state.m_RightSwingDetected = rightSwingDetected;
            state.m_FrameTimeSeconds = frameTimeSeconds;
            state.m_FrameSerial = frameSerial;
        }

        private static void SubmitSwing(
            AlternatingSwingFlowState state,
            bool leftSwingDetected,
            bool rightSwingDetected,
            float frameTimeSeconds,
            int frameSerial)
        {
            if (leftSwingDetected)
            {
                state.m_LastLeftAlternatingTimeSeconds = frameTimeSeconds;
                RecordSwing(state, m_LeftSideMarker, frameSerial);
            }

            if (rightSwingDetected)
            {
                state.m_LastRightAlternatingTimeSeconds = frameTimeSeconds;
                RecordSwing(state, m_RightSideMarker, frameSerial);
            }
        }

        private static void RecordSwing(AlternatingSwingFlowState state, int punchHand, int frameSerial)
        {
            state.m_LastAlternatingSideMarker = punchHand;
            state.m_LastAlternatingFrameSerial = frameSerial;
        }

        private static void UpdateHandTracking(AlternatingSwingHandState handState, bool hasWrist)
        {
            if (!hasWrist)
            {
                handState.Reset();
            }
        }

        private static void SynchronizeHandSample(
            AlternatingSwingHandState handState,
            bool hasWrist,
            float relativeWristY)
        {
            if (!hasWrist)
            {
                handState.Reset();
                return;
            }

            handState.m_HasLastRelativeWristY = true;
            handState.m_LastRelativeWristY = relativeWristY;
            handState.ResetStroke(relativeWristY);
        }
    }
}
