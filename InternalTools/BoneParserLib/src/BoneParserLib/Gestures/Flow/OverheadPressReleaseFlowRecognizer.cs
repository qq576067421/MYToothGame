using System;
using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class HandsAboveNoseFlowState : IBoneGestureRuntimeState
    {
        public bool m_IsLatched;
        public bool m_LastActive;
        public int m_PendingEventId;

        public void Reset()
        {
            m_IsLatched = false;
            m_LastActive = false;
            m_PendingEventId = 0;
        }
    }

    internal sealed class HandsAboveNoseFlowRecognizer : IBoneGestureRecognizer
    {
        private const string m_StateKey = "Flow.HandsAboveNose";

        public void CollectDefinitions(List<BoneGestureDefinition> definitions)
        {
            definitions.Add(new BoneGestureDefinition(
                BoneGestureType.双手举过鼻尖释放_流程, BoneGestureCategory.流程,
                BoneGesturePhaseMask.开始 | BoneGesturePhaseMask.持续 | BoneGesturePhaseMask.结束 | BoneGesturePhaseMask.触发,
                BoneGesturePhaseMask.触发));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<HandsAboveNoseFlowState>(m_StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            HandsAboveNoseFlowState state = context.m_SeatState.ReadGestureState<HandsAboveNoseFlowState>(m_StateKey);
            float score = context.m_Config.m_GestureKeypointMinConfidence;
            if (!BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左肩, score, out BoneVector2 leftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右肩, score, out BoneVector2 rightShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.鼻尖, score, out BoneVector2 nose) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.左手腕, score, out BoneVector2 leftWrist) ||
                !BoneSkeletonQuery.TryReadBodyJoint(context.m_Person, BoneBodyJointType.右手腕, score, out BoneVector2 rightWrist))
            {
                // 短暂遮挡不能当作放下双手，否则重获同一姿势会重复触发。
                context.AddBooleanGestureEvent(state.m_LastActive, false, BoneGestureType.双手举过鼻尖释放_流程);
                state.m_LastActive = false;
                return;
            }

            float x = rightShoulder.m_X - leftShoulder.m_X;
            float y = rightShoulder.m_Y - leftShoulder.m_Y;
            float width = (float)Math.Sqrt(x * x + y * y);
            if (width <= context.m_Config.m_ShoulderWidthEpsilon)
            {
                return;
            }

            float enterY = nose.m_Y - width * BoneMath.Max(0f, context.m_Config.m_HandsAboveNoseEnterMarginRatio);
            float exitY = nose.m_Y + width * BoneMath.Max(0f, context.m_Config.m_HandsAboveNoseExitMarginRatio);
            bool bothAbove = leftWrist.m_Y < enterY && rightWrist.m_Y < enterY;
            bool bothLowered = leftWrist.m_Y > exitY && rightWrist.m_Y > exitY;
            if (bothLowered)
            {
                state.m_IsLatched = false;
            }

            bool currentActive = bothAbove || (state.m_LastActive && !bothLowered);
            context.AddBooleanGestureEvent(state.m_LastActive, currentActive, BoneGestureType.双手举过鼻尖释放_流程);
            state.m_LastActive = currentActive;
            if (!bothAbove || state.m_IsLatched)
            {
                return;
            }

            // 先锁存再发送。能量不足或游戏拒绝都不会在保持举手时补发。
            state.m_IsLatched = true;
            BoneActionEvent actionEvent = context.AddGestureEvent(
                BoneGestureType.双手举过鼻尖释放_流程, BoneGesturePhase.触发, BoneGestureTriggerHand.双手);
            state.m_PendingEventId = actionEvent != null && actionEvent.m_RequiresConsumeResult
                ? actionEvent.m_ActionEventId : 0;
        }

        public bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult)
        {
            HandsAboveNoseFlowState state = context.m_SeatState.ReadGestureState<HandsAboveNoseFlowState>(m_StateKey);
            if (consumeResult.m_ActionEventId <= 0 || state.m_PendingEventId != consumeResult.m_ActionEventId)
            {
                return false;
            }

            state.m_PendingEventId = 0;
            return true;
        }
    }
}
