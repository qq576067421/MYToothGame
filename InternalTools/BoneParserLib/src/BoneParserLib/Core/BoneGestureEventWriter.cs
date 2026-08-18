namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class BoneGestureEventWriter
    {
        private int m_NextActionEventId = 1;

        public void Reset()
        {
            m_NextActionEventId = 1;
        }

        public BoneActionEvent AddGestureEvent(
            BoneParserSeatDefinition seatDefinition,
            BoneParserPlayerResult result,
            BoneGestureType gestureType,
            BoneGesturePhase phase,
            int personId,
            int frameSerial)
        {
            if (result == null)
            {
                return null;
            }

            result.m_GestureEvents.Add(new BoneGestureEvent
            {
                m_GestureType = gestureType,
                m_Phase = phase,
                m_SlotIndex = result.m_SlotIndex,
                m_BindingId = result.m_BindingId,
                m_PersonId = personId,
                m_FrameSerial = frameSerial,
            });

            return AddActionEvent(seatDefinition, result, gestureType, phase, personId, frameSerial);
        }

        public void AddBooleanGestureEvent(
            bool previousActive,
            bool currentActive,
            BoneGestureType gestureType,
            BoneParserSeatDefinition seatDefinition,
            BoneParserPlayerResult result,
            int personId,
            int frameSerial)
        {
            if (!previousActive && currentActive)
            {
                AddGestureEvent(seatDefinition, result, gestureType, BoneGesturePhase.开始, personId, frameSerial);
                return;
            }

            if (previousActive && currentActive)
            {
                AddGestureEvent(seatDefinition, result, gestureType, BoneGesturePhase.持续, personId, frameSerial);
                return;
            }

            if (previousActive && !currentActive)
            {
                AddGestureEvent(seatDefinition, result, gestureType, BoneGesturePhase.结束, personId, frameSerial);
            }
        }

        public bool HasRecognizableActionBinding(BoneParserSeatDefinition seatDefinition, BoneGestureType gestureType)
        {
            if (seatDefinition == null || seatDefinition.m_ActionBindings == null)
            {
                return false;
            }

            int bindingCount = seatDefinition.m_ActionBindings.Count;
            for (int i = 0; i < bindingCount; i++)
            {
                BoneActionBinding actionBinding = seatDefinition.m_ActionBindings[i];
                if (actionBinding != null &&
                    actionBinding.m_GestureType == gestureType &&
                    (actionBinding.m_RuntimeFlags & BoneActionRuntimeFlags.可识别) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private BoneActionEvent AddActionEvent(
            BoneParserSeatDefinition seatDefinition,
            BoneParserPlayerResult result,
            BoneGestureType gestureType,
            BoneGesturePhase phase,
            int personId,
            int frameSerial)
        {
            BoneActionBinding actionBinding = FindFirstActionBinding(seatDefinition, gestureType, phase);
            if (actionBinding == null)
            {
                return null;
            }

            BoneActionEvent actionEvent = new BoneActionEvent
            {
                m_ActionEventId = m_NextActionEventId++,
                m_ActionId = actionBinding.m_ActionId,
                m_GestureType = gestureType,
                m_Phase = phase,
                m_SlotIndex = result.m_SlotIndex,
                m_BindingId = result.m_BindingId,
                m_PersonId = personId,
                m_FrameSerial = frameSerial,
                m_ConsumerType = actionBinding.m_ConsumerType,
                m_ConsumerValue = actionBinding.m_ConsumerValue,
                m_RuntimeFlags = actionBinding.m_RuntimeFlags,
                m_RequiresConsumeResult = actionBinding.m_RequiresConsumeResult,
                m_FaceForward = result.m_FaceForward,
                m_MoveDirection = result.m_FaceForward,
            };
            result.m_ActionEvents.Add(actionEvent);
            return actionEvent;
        }

        private static BoneActionBinding FindFirstActionBinding(
            BoneParserSeatDefinition seatDefinition,
            BoneGestureType gestureType,
            BoneGesturePhase phase)
        {
            if (seatDefinition == null || seatDefinition.m_ActionBindings == null)
            {
                return null;
            }

            BoneGesturePhaseMask phaseMask = ConvertPhaseMask(phase);
            int bindingCount = seatDefinition.m_ActionBindings.Count;
            for (int i = 0; i < bindingCount; i++)
            {
                BoneActionBinding actionBinding = seatDefinition.m_ActionBindings[i];
                if (actionBinding == null ||
                    actionBinding.m_GestureType != gestureType ||
                    (actionBinding.m_PhaseMask & phaseMask) == 0 ||
                    (actionBinding.m_RuntimeFlags & BoneActionRuntimeFlags.可识别) == 0)
                {
                    continue;
                }

                return actionBinding;
            }

            return null;
        }

        private static BoneGesturePhaseMask ConvertPhaseMask(BoneGesturePhase phase)
        {
            switch (phase)
            {
                case BoneGesturePhase.开始:
                    return BoneGesturePhaseMask.开始;
                case BoneGesturePhase.持续:
                    return BoneGesturePhaseMask.持续;
                case BoneGesturePhase.结束:
                    return BoneGesturePhaseMask.结束;
                case BoneGesturePhase.触发:
                    return BoneGesturePhaseMask.触发;
                default:
                    return BoneGesturePhaseMask.无;
            }
        }
    }
}
