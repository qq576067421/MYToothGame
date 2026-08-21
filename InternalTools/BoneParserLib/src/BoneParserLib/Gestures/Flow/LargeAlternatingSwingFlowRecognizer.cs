using System;
using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class LargeAlternatingSwingHandState
    {
        public bool m_HasLastWristHeight;
        public float m_LastWristHeight;
        public float m_StrokeStartWristHeight;
        public float m_StrokeDurationSeconds;
        public int m_UpwardFrameCount;
        public int m_IdleFrameCount;

        public void ResetStroke(float wristHeight)
        {
            m_StrokeStartWristHeight = wristHeight;
            m_StrokeDurationSeconds = 0f;
            m_UpwardFrameCount = 0;
            m_IdleFrameCount = 0;
        }

        public void Reset()
        {
            m_HasLastWristHeight = false;
            m_LastWristHeight = 0f;
            ResetStroke(0f);
        }
    }

    internal sealed class LargeAlternatingSwingFlowState : IBoneGestureRuntimeState
    {
        public readonly LargeAlternatingSwingHandState m_LeftHand = new LargeAlternatingSwingHandState();
        public readonly LargeAlternatingSwingHandState m_RightHand = new LargeAlternatingSwingHandState();
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

        public void ResetPending()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_LeftSwingDetected = false;
            m_RightSwingDetected = false;
            m_FrameTimeSeconds = 0f;
            m_FrameSerial = 0;
        }

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
    }

    internal sealed class LargeAlternatingSwingFlowRecognizer : IBoneGestureRecognizer
    {
        private const string m_StateKey = "large-alternating-swing";
        private const int m_LeftSideMarker = 1;
        private const int m_RightSideMarker = 2;

        public void CollectDefinitions(List<BoneGestureDefinition> definitions)
        {
            definitions.Add(new BoneGestureDefinition(
                BoneGestureType.左右交替大幅挥击_流程,
                BoneGestureCategory.流程,
                BoneGesturePhaseMask.触发,
                BoneGesturePhaseMask.触发));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<LargeAlternatingSwingFlowState>(m_StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            LargeAlternatingSwingFlowState state =
                context.m_SeatState.ReadGestureState<LargeAlternatingSwingFlowState>(m_StateKey);
            if (!TryReadFlowInput(
                    context,
                    out float shoulderWidth,
                    out float leftTorsoHeight,
                    out float leftWristHeight,
                    out float rightTorsoHeight,
                    out float rightWristHeight))
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
                    leftTorsoHeight,
                    leftWristHeight,
                    rightTorsoHeight,
                    rightWristHeight,
                    out leftSwingDetected,
                    out rightSwingDetected);
            }
            else
            {
                SynchronizeHandSample(state.m_LeftHand, leftWristHeight);
                SynchronizeHandSample(state.m_RightHand, rightWristHeight);
            }

            if (!shouldEmitTrigger)
            {
                return;
            }

            BoneActionEvent actionEvent = context.AddGestureEvent(
                BoneGestureType.左右交替大幅挥击_流程,
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

        public bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult)
        {
            LargeAlternatingSwingFlowState state =
                context.m_SeatState.ReadGestureState<LargeAlternatingSwingFlowState>(m_StateKey);
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
            out float leftTorsoHeight,
            out float leftWristHeight,
            out float rightTorsoHeight,
            out float rightWristHeight)
        {
            shoulderWidth = 0f;
            leftTorsoHeight = 0f;
            leftWristHeight = 0f;
            rightTorsoHeight = 0f;
            rightWristHeight = 0f;

            if (context.m_Person == null ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左肩, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 leftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右肩, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 rightShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左髋, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 leftHip) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右髋, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 rightHip) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左手腕, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 leftWrist) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右手腕, context.m_Config.m_GestureKeypointMinConfidence, out BoneVector2 rightWrist))
            {
                return false;
            }

            shoulderWidth = BoneMath.Abs(rightShoulder.m_X - leftShoulder.m_X) + context.m_Config.m_ShoulderWidthEpsilon;
            leftTorsoHeight = BoneMath.Abs(leftHip.m_Y - leftShoulder.m_Y);
            rightTorsoHeight = BoneMath.Abs(rightHip.m_Y - rightShoulder.m_Y);
            if (leftTorsoHeight <= context.m_Config.m_ShoulderWidthEpsilon ||
                rightTorsoHeight <= context.m_Config.m_ShoulderWidthEpsilon)
            {
                return false;
            }

            leftWristHeight = leftHip.m_Y - leftWrist.m_Y;
            rightWristHeight = rightHip.m_Y - rightWrist.m_Y;
            return true;
        }

        private static bool EvaluateSwing(
            BoneGestureRuntimeContext context,
            LargeAlternatingSwingFlowState state,
            float shoulderWidth,
            float leftTorsoHeight,
            float leftWristHeight,
            float rightTorsoHeight,
            float rightWristHeight,
            out bool leftSwingDetected,
            out bool rightSwingDetected)
        {
            leftSwingDetected = false;
            rightSwingDetected = false;
            if (state == null || context.m_DeltaTimeSeconds <= 0f)
            {
                return false;
            }

            bool leftMotionCompleted = EvaluateHandSwing(
                state.m_LeftHand,
                leftWristHeight,
                leftTorsoHeight,
                shoulderWidth,
                context.m_DeltaTimeSeconds,
                context.m_Config);
                leftSwingDetected = leftMotionCompleted &&
                    context.m_FrameTimeSeconds - state.m_LastLeftAlternatingTimeSeconds >= context.m_Config.m_LargeAlternatingSwingCooldownSeconds;

            bool rightMotionCompleted = EvaluateHandSwing(
                state.m_RightHand,
                rightWristHeight,
                rightTorsoHeight,
                shoulderWidth,
                context.m_DeltaTimeSeconds,
                context.m_Config);
                rightSwingDetected = rightMotionCompleted &&
                    context.m_FrameTimeSeconds - state.m_LastRightAlternatingTimeSeconds >= context.m_Config.m_LargeAlternatingSwingCooldownSeconds;

            if (leftSwingDetected == rightSwingDetected)
            {
                return false;
            }

            int currentSideMarker = leftSwingDetected ? m_LeftSideMarker : m_RightSideMarker;
            bool hasPreviousSide = state.m_LastAlternatingSideMarker != 0;
            bool isOppositeSide = hasPreviousSide && state.m_LastAlternatingSideMarker != currentSideMarker;
            int maxWindowFrames = Math.Max(1, context.m_Config.m_LargeAlternatingSwingWindowFrames);
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
            LargeAlternatingSwingHandState handState,
            float wristHeight,
            float torsoHeight,
            float shoulderWidth,
            float deltaTimeSeconds,
            BoneParserConfig config)
        {
            if (!handState.m_HasLastWristHeight)
            {
                SynchronizeHandSample(handState, wristHeight);
                return false;
            }

            float previousWristHeight = handState.m_LastWristHeight;
            float frameDelta = wristHeight - previousWristHeight;
            handState.m_LastWristHeight = wristHeight;

            float directionNoise = shoulderWidth * BoneMath.Max(0f, config.m_LargeAlternatingSwingDirectionNoiseRatio);
            int minDirectionalFrames = Math.Max(2, config.m_LargeAlternatingSwingMinDirectionalFrames);
            if (frameDelta <= directionNoise)
            {
                if (frameDelta < -directionNoise)
                {
                    handState.ResetStroke(wristHeight);
                    return false;
                }

                if (handState.m_UpwardFrameCount > 0)
                {
                    handState.m_IdleFrameCount++;
                    handState.m_StrokeDurationSeconds += deltaTimeSeconds;
                    if (handState.m_UpwardFrameCount < minDirectionalFrames || handState.m_IdleFrameCount > 1)
                    {
                        handState.ResetStroke(wristHeight);
                    }
                }
                else
                {
                    handState.ResetStroke(wristHeight);
                }

                return false;
            }

            if (handState.m_UpwardFrameCount == 0)
            {
                handState.m_StrokeStartWristHeight = previousWristHeight;
                handState.m_StrokeDurationSeconds = deltaTimeSeconds;
                handState.m_UpwardFrameCount = 1;
                handState.m_IdleFrameCount = 0;
            }
            else
            {
                handState.m_StrokeDurationSeconds += deltaTimeSeconds;
                handState.m_UpwardFrameCount++;
                handState.m_IdleFrameCount = 0;
            }

            float verticalDistance = wristHeight - handState.m_StrokeStartWristHeight;
            float verticalSpeed = verticalDistance / BoneMath.Max(handState.m_StrokeDurationSeconds, 1f / 120f);
            bool isCompleted =
                handState.m_UpwardFrameCount >= minDirectionalFrames &&
                verticalDistance >= torsoHeight * BoneMath.Max(0f, config.m_LargeAlternatingSwingMinTorsoDistanceRatio) &&
                verticalSpeed >= shoulderWidth * config.m_LargeAlternatingSwingSpeedRatioPerSecond;
            if (isCompleted)
            {
                handState.ResetStroke(wristHeight);
            }

            return isCompleted;
        }

        private static void PendingAction(
            LargeAlternatingSwingFlowState state,
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
            LargeAlternatingSwingFlowState state,
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

        private static void RecordSwing(LargeAlternatingSwingFlowState state, int sideMarker, int frameSerial)
        {
            state.m_LastAlternatingSideMarker = sideMarker;
            state.m_LastAlternatingFrameSerial = frameSerial;
        }

        private static void SynchronizeHandSample(LargeAlternatingSwingHandState handState, float wristHeight)
        {
            handState.m_HasLastWristHeight = true;
            handState.m_LastWristHeight = wristHeight;
            handState.ResetStroke(wristHeight);
        }
    }
}
