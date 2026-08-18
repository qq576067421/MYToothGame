namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class BoneGestureDefinition
    {
        public readonly BoneGestureType m_GestureType;
        public readonly BoneGestureCategory m_Category;
        public readonly BoneGesturePhaseMask m_SupportedPhases;
        public readonly BoneGesturePhaseMask m_RequiresConsumeResultPhases;

        public BoneGestureDefinition(
            BoneGestureType gestureType,
            BoneGestureCategory category,
            BoneGesturePhaseMask supportedPhases,
            BoneGesturePhaseMask requiresConsumeResultPhases)
        {
            m_GestureType = gestureType;
            m_Category = category;
            m_SupportedPhases = supportedPhases;
            m_RequiresConsumeResultPhases = requiresConsumeResultPhases;
        }

        public bool RequiresConsumeResult(BoneGesturePhaseMask phaseMask)
        {
            return (m_RequiresConsumeResultPhases & phaseMask) != 0;
        }
    }
}
