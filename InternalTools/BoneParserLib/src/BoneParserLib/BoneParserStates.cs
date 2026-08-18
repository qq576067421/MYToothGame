using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal interface IBoneGestureRuntimeState
    {
        void Reset();
    }

    internal sealed class BoneParserSeatState
    {
        private readonly Dictionary<string, IBoneGestureRuntimeState> m_GestureStates =
            new Dictionary<string, IBoneGestureRuntimeState>();

        public int m_CurrentPersonId = BoneTrackedLayout.m_InvalidPersonId;
        public int m_MissingFrameCount;
        public long m_LastFrameTimeMs;
        public float m_CachedRotationOffset;
        public float m_MaxObservedShoulderWidth;
        public BoneAimTrackingState m_AimTrackingState = BoneAimTrackingState.未跟踪;
        public bool m_HasAimOutput;
        public float m_LastAimAngleDegrees;
        public float m_LastAimAngularSpeedDegrees;
        public int m_AimMissingFrameCount;
        public int m_AimCandidatePersonId = BoneTrackedLayout.m_InvalidPersonId;
        public int m_AimCandidateStableFrameCount;
        public bool m_IsAimOutsideCenterDeadZone;

        public T ReadGestureState<T>(string stateKey) where T : class, IBoneGestureRuntimeState, new()
        {
            if (string.IsNullOrEmpty(stateKey))
            {
                return null;
            }

            if (m_GestureStates.TryGetValue(stateKey, out IBoneGestureRuntimeState state))
            {
                return state as T;
            }

            T newState = new T();
            m_GestureStates.Add(stateKey, newState);
            return newState;
        }

        public void ResetForNewPerson(int personId)
        {
            m_CurrentPersonId = personId;
            m_MissingFrameCount = 0;
            m_LastFrameTimeMs = 0L;
            m_CachedRotationOffset = 0f;
            m_MaxObservedShoulderWidth = 0f;
            ResetAimState();
            m_GestureStates.Clear();
        }

        public void ResetForRelease()
        {
            m_CurrentPersonId = BoneTrackedLayout.m_InvalidPersonId;
            m_MissingFrameCount = 0;
            m_LastFrameTimeMs = 0L;
            m_CachedRotationOffset = 0f;
            m_MaxObservedShoulderWidth = 0f;
            ResetAimState();
            m_GestureStates.Clear();
        }

        public void ResetAimState()
        {
            m_AimTrackingState = BoneAimTrackingState.未跟踪;
            m_HasAimOutput = false;
            m_LastAimAngleDegrees = 0f;
            m_LastAimAngularSpeedDegrees = 0f;
            m_AimMissingFrameCount = 0;
            m_AimCandidatePersonId = BoneTrackedLayout.m_InvalidPersonId;
            m_AimCandidateStableFrameCount = 0;
            m_IsAimOutsideCenterDeadZone = false;
        }
    }
}
