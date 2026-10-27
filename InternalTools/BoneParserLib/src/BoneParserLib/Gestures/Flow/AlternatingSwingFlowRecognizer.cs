using System;
using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class UpDownSwingHandState
    {
        public bool m_IsArmed;
        public bool m_IsWaitingForDownstroke;
        public float m_ArmedStartHeight;
        public float m_PeakHeight;
        public float m_ArmedElapsedSeconds;
        public bool m_HasCompletedStroke;
        public float m_CompletedStrokeTimeSeconds = float.NegativeInfinity;

        public void ResetStroke()
        {
            m_IsArmed = false;
            m_IsWaitingForDownstroke = false;
            m_ArmedStartHeight = 0f;
            m_PeakHeight = 0f;
            m_ArmedElapsedSeconds = 0f;
        }

        public void 开始记录行程(float height)
        {
            m_IsArmed = true;
            m_IsWaitingForDownstroke = false;
            m_ArmedStartHeight = height;
            m_PeakHeight = height;
            m_ArmedElapsedSeconds = 0f;
        }

        public void ResetTracking()
        {
            ResetStroke();
            ClearCompletedStroke();
        }

        public void MarkCompletedStroke(float currentTimeSeconds)
        {
            m_HasCompletedStroke = true;
            m_CompletedStrokeTimeSeconds = currentTimeSeconds;
        }

        public void ExpireCompletedStroke(float currentTimeSeconds, float pairingWindowSeconds)
        {
            if (m_HasCompletedStroke &&
                currentTimeSeconds - m_CompletedStrokeTimeSeconds > pairingWindowSeconds)
            {
                ClearCompletedStroke();
            }
        }

        public void ClearCompletedStroke()
        {
            m_HasCompletedStroke = false;
            m_CompletedStrokeTimeSeconds = float.NegativeInfinity;
        }

        public void Reset()
        {
            ResetTracking();
        }
    }

    internal sealed class UpDownSwingFlowState : IBoneGestureRuntimeState
    {
        public readonly UpDownSwingHandState m_LeftHand = new UpDownSwingHandState();
        public readonly UpDownSwingHandState m_RightHand = new UpDownSwingHandState();
        public int m_PendingEventId;
        public float m_ElapsedTimeSeconds;

        public void Reset()
        {
            m_LeftHand.Reset();
            m_RightHand.Reset();
            m_PendingEventId = 0;
            m_ElapsedTimeSeconds = 0f;
        }
    }

    internal sealed class UpDownSwingFlowRecognizer : IBoneGestureRecognizer
    {
        private const string m_StateKey = "Flow.UpDownSwing";

        public void CollectDefinitions(List<BoneGestureDefinition> definitions)
        {
            definitions.Add(new BoneGestureDefinition(
                BoneGestureType.双手上下挥动_流程,
                BoneGestureCategory.流程,
                BoneGesturePhaseMask.触发,
                BoneGesturePhaseMask.触发));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<UpDownSwingFlowState>(m_StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            UpDownSwingFlowState state = context.m_SeatState.ReadGestureState<UpDownSwingFlowState>(m_StateKey);
            if (!context.HasRecognizableActionBinding(BoneGestureType.双手上下挥动_流程))
            {
                state.Reset();
                return;
            }

            state.m_ElapsedTimeSeconds += BoneMath.Max(context.m_DeltaTimeSeconds, 0f);
            float currentTimeSeconds = state.m_ElapsedTimeSeconds;
            float pairingWindowSeconds = BoneMath.Max(
                0f, context.m_Config.m_UpDownSwingPairingWindowSeconds);
            state.m_LeftHand.ExpireCompletedStroke(currentTimeSeconds, pairingWindowSeconds);
            state.m_RightHand.ExpireCompletedStroke(currentTimeSeconds, pairingWindowSeconds);

            if (读取单侧身体坐标(context, BoneBodyJointType.左肩, BoneBodyJointType.左髋,
                    out BoneVector2 leftShoulder, out BoneVector2 leftHip))
            {
                if (识别单手(context, state.m_LeftHand, BoneBodyJointType.左手腕,
                        leftShoulder, leftHip))
                {
                    state.m_LeftHand.MarkCompletedStroke(currentTimeSeconds);
                }
            }
            else
            {
                state.m_LeftHand.ResetStroke();
            }

            if (读取单侧身体坐标(context, BoneBodyJointType.右肩, BoneBodyJointType.右髋,
                    out BoneVector2 rightShoulder, out BoneVector2 rightHip))
            {
                if (识别单手(context, state.m_RightHand, BoneBodyJointType.右手腕,
                        rightShoulder, rightHip))
                {
                    state.m_RightHand.MarkCompletedStroke(currentTimeSeconds);
                }
            }
            else
            {
                state.m_RightHand.ResetStroke();
            }

            if (!state.m_LeftHand.m_HasCompletedStroke || !state.m_RightHand.m_HasCompletedStroke)
            {
                return;
            }

            if (BoneMath.Abs(state.m_LeftHand.m_CompletedStrokeTimeSeconds -
                    state.m_RightHand.m_CompletedStrokeTimeSeconds) > pairingWindowSeconds)
            {
                return;
            }

            BoneActionEvent actionEvent = context.AddGestureEvent(
                BoneGestureType.双手上下挥动_流程,
                BoneGesturePhase.触发,
                BoneGestureTriggerHand.双手);
            state.m_LeftHand.ClearCompletedStroke();
            state.m_RightHand.ClearCompletedStroke();
            state.m_PendingEventId = actionEvent != null && actionEvent.m_RequiresConsumeResult
                ? actionEvent.m_ActionEventId : 0;
        }

        public bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult)
        {
            UpDownSwingFlowState state = context.m_SeatState.ReadGestureState<UpDownSwingFlowState>(m_StateKey);
            if (consumeResult.m_ActionEventId <= 0)
            {
                return false;
            }

            if (state.m_PendingEventId != consumeResult.m_ActionEventId)
            {
                return false;
            }

            // 是否被游戏接受不改变物理行程：同一次挥动不会在冷却结束后补发。
            state.m_PendingEventId = 0;
            return true;
        }

        private static bool 读取单侧身体坐标(BoneGestureRuntimeContext context,
            BoneBodyJointType shoulderJoint, BoneBodyJointType hipJoint,
            out BoneVector2 shoulder, out BoneVector2 hip)
        {
            shoulder = BoneVector2.m_Zero;
            hip = BoneVector2.m_Zero;
            float score = context.m_Config.m_GestureKeypointMinConfidence;
            return BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, shoulderJoint, score, out shoulder) &&
                BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, hipJoint, score, out hip);
        }

        private static bool 识别单手(BoneGestureRuntimeContext context, UpDownSwingHandState state,
            BoneBodyJointType wristJoint,
            BoneVector2 sameSideShoulder, BoneVector2 sameSideHip)
        {
            BoneParserConfig config = context.m_Config;
            if (!BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, wristJoint,
                    config.m_GestureKeypointMinConfidence, out BoneVector2 wrist))
            {
                // 当前半截行程不能跨越丢点拼接，但另一只手已经完成的结果仍在配对窗口内有效。
                state.ResetStroke();
                return false;
            }

            float torsoX = sameSideShoulder.m_X - sameSideHip.m_X;
            float torsoY = sameSideShoulder.m_Y - sameSideHip.m_Y;
            float torsoLength = (float)Math.Sqrt(torsoX * torsoX + torsoY * torsoY);
            if (torsoLength <= config.m_ShoulderWidthEpsilon)
            {
                state.ResetStroke();
                return false;
            }

            BoneVector2 bodyUp = new BoneVector2(torsoX / torsoLength, torsoY / torsoLength);
            float x = wrist.m_X - sameSideHip.m_X;
            float y = wrist.m_Y - sameSideHip.m_Y;
            float height = (x * bodyUp.m_X + y * bodyUp.m_Y) / torsoLength;
            float lateral = (x * -bodyUp.m_Y + y * bodyUp.m_X) / torsoLength;

            float startMin = BoneMath.Min(
                config.m_UpDownSwingStartMinHeightRatio,
                config.m_UpDownSwingStartMaxHeightRatio);
            float startMax = BoneMath.Max(
                config.m_UpDownSwingStartMinHeightRatio,
                config.m_UpDownSwingStartMaxHeightRatio);
            float minStrokeDistance = BoneMath.Max(
                0.0001f, config.m_UpDownSwingMinStrokeDistanceRatio);
            float maxLateral = BoneMath.Max(0f, config.m_UpDownSwingMaxLateralRatio);
            bool isWithinLateral = BoneMath.Abs(lateral) <= maxLateral;
            if (!isWithinLateral)
            {
                state.ResetStroke();
                return false;
            }

            bool isWithinStartRange = height >= startMin && height <= startMax;
            if (state.m_IsWaitingForDownstroke)
            {
                state.m_PeakHeight = BoneMath.Max(state.m_PeakHeight, height);
                if (isWithinStartRange && state.m_PeakHeight - height >= minStrokeDistance)
                {
                    state.开始记录行程(height);
                }
                return false;
            }

            if (!state.m_IsArmed)
            {
                if (!isWithinStartRange)
                {
                    return false;
                }

                state.开始记录行程(height);
                return false;
            }

            if (isWithinStartRange && height < state.m_ArmedStartHeight)
            {
                // 连续记录当前行程的最低点，缓慢放手也能为下一次上挥建立起点。
                state.开始记录行程(height);
                return false;
            }

            if (height > state.m_PeakHeight)
            {
                state.m_PeakHeight = height;
                state.m_ArmedElapsedSeconds += BoneMath.Max(context.m_DeltaTimeSeconds, 0f);
            }

            float strokeDistance = state.m_PeakHeight - state.m_ArmedStartHeight;
            if (strokeDistance < minStrokeDistance)
            {
                return false;
            }

            float elapsedSeconds = BoneMath.Max(state.m_ArmedElapsedSeconds, 1f / 120f);
            float upwardSpeed = strokeDistance / elapsedSeconds;
            if (upwardSpeed < BoneMath.Max(0f, config.m_UpDownSwingMinUpwardSpeedRatioPerSecond))
            {
                // 速度不足只淘汰本次上挥；随后下放时会立即形成新的起点，不进入锁定等待。
                state.开始记录行程(height);
                return false;
            }

            // 达到有效上挥幅度后消耗本次行程，必须出现新的下挥才能再次触发。
            state.m_IsArmed = false;
            state.m_IsWaitingForDownstroke = true;
            state.m_PeakHeight = height;
            return true;
        }
    }
}
