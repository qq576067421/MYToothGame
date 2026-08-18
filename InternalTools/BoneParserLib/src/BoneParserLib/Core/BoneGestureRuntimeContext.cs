namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class BoneGestureRuntimeContext
    {
        public readonly BoneParserConfig m_Config;
        public readonly BoneSkeletonQuery m_Skeleton;
        public readonly BoneGestureEventWriter m_EventWriter;
        public readonly BoneTrackedPerson m_Person;
        public readonly BoneParserSeatState m_SeatState;
        public readonly BoneParserSeatDefinition m_SeatDefinition;
        public readonly BoneParserPlayerResult m_Result;
        public readonly BoneTrackedFrame m_FrameData;
        public readonly float m_DeltaTimeSeconds;
        public readonly float m_FrameTimeSeconds;

        public BoneGestureRuntimeContext(
            BoneParserConfig config,
            BoneSkeletonQuery skeleton,
            BoneGestureEventWriter eventWriter,
            BoneTrackedPerson person,
            BoneParserSeatState seatState,
            BoneParserSeatDefinition seatDefinition,
            BoneParserPlayerResult result,
            BoneTrackedFrame frameData,
            float deltaTimeSeconds,
            float frameTimeSeconds)
        {
            m_Config = config;
            m_Skeleton = skeleton;
            m_EventWriter = eventWriter;
            m_Person = person;
            m_SeatState = seatState;
            m_SeatDefinition = seatDefinition;
            m_Result = result;
            m_FrameData = frameData;
            m_DeltaTimeSeconds = deltaTimeSeconds;
            m_FrameTimeSeconds = frameTimeSeconds;
        }

        public int ReadPersonId()
        {
            return m_Person != null ? m_Person.m_PersonId : BoneTrackedLayout.m_InvalidPersonId;
        }

        public int ReadFrameSerial()
        {
            if (m_FrameData != null)
            {
                return m_FrameData.m_FrameSerial;
            }

            return m_Result != null ? m_Result.m_SlotIndex : 0;
        }

        public bool HasRecognizableActionBinding(BoneGestureType gestureType)
        {
            return m_EventWriter.HasRecognizableActionBinding(m_SeatDefinition, gestureType);
        }

        public BoneActionEvent AddGestureEvent(BoneGestureType gestureType, BoneGesturePhase phase)
        {
            return m_EventWriter.AddGestureEvent(
                m_SeatDefinition,
                m_Result,
                gestureType,
                phase,
                ReadPersonId(),
                ReadFrameSerial());
        }

        public void AddBooleanGestureEvent(
            bool previousActive,
            bool currentActive,
            BoneGestureType gestureType)
        {
            m_EventWriter.AddBooleanGestureEvent(
                previousActive,
                currentActive,
                gestureType,
                m_SeatDefinition,
                m_Result,
                ReadPersonId(),
                ReadFrameSerial());
        }
    }
}
