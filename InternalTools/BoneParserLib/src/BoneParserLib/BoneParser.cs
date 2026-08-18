using System.Collections.Generic;
using System;

namespace CompanyInternalTools.BoneParserLib
{
    public sealed class BoneParser
    {
        private readonly BoneParserConfig m_Config;
        private readonly List<BoneParserSeatState> m_SeatStates = new List<BoneParserSeatState>();
        private readonly BoneParserFrameResult m_FrameResult = new BoneParserFrameResult();
        private readonly BoneSkeletonQuery m_Skeleton;
        private readonly BoneTurnEstimator m_TurnEstimator;
        private readonly BoneGestureEventWriter m_EventWriter = new BoneGestureEventWriter();
        private readonly BoneGestureRecognizerRegistry m_GestureRecognizers;

        public BoneParser(BoneParserConfig config = null)
        {
            m_Config = config ?? new BoneParserConfig();
            m_Skeleton = new BoneSkeletonQuery(m_Config);
            m_TurnEstimator = new BoneTurnEstimator(m_Config);
            m_GestureRecognizers = BoneGestureRecognizerRegistry.CreateDefault();
        }

        public void Reset()
        {
            m_SeatStates.Clear();
            m_FrameResult.m_FrameSerial = 0;
            m_FrameResult.m_PlayerResults.Clear();
            m_EventWriter.Reset();
        }

        public BoneParserFrameResult Update(BoneTrackedFrame frameData, IList<BoneParserSeatDefinition> seatDefinitions)
        {
            int slotCount = seatDefinitions != null ? seatDefinitions.Count : 0;
            EnsureSeatRuntime(slotCount);
            PrepareFrameResult(frameData, seatDefinitions);
            if (slotCount <= 0)
            {
                return m_FrameResult;
            }

            if (frameData == null || !frameData.m_HasFrameData)
            {
                HandleAllSeatsMissing(frameData);
                return m_FrameResult;
            }

            UpdateFrameSeats(frameData, seatDefinitions);
            return m_FrameResult;
        }

        public bool ApplyActionConsumeResult(BoneActionConsumeResult consumeResult)
        {
            if (consumeResult == null || consumeResult.m_ActionEventId <= 0)
            {
                return false;
            }

            for (int i = 0; i < m_SeatStates.Count; i++)
            {
                BoneParserSeatState seatState = m_SeatStates[i];
                if (seatState == null)
                {
                    continue;
                }

                BoneGestureRuntimeContext context = new BoneGestureRuntimeContext(
                    m_Config,
                    m_Skeleton,
                    m_EventWriter,
                    null,
                    seatState,
                    null,
                    null,
                    null,
                    0f,
                    0f);
                if (m_GestureRecognizers.TryApplyConsumeResult(context, consumeResult))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureSeatRuntime(int slotCount)
        {
            while (m_SeatStates.Count < slotCount)
            {
                m_SeatStates.Add(new BoneParserSeatState());
            }

            while (m_SeatStates.Count > slotCount)
            {
                m_SeatStates.RemoveAt(m_SeatStates.Count - 1);
            }

            while (m_FrameResult.m_PlayerResults.Count < slotCount)
            {
                m_FrameResult.m_PlayerResults.Add(new BoneParserPlayerResult());
            }

            while (m_FrameResult.m_PlayerResults.Count > slotCount)
            {
                m_FrameResult.m_PlayerResults.RemoveAt(m_FrameResult.m_PlayerResults.Count - 1);
            }
        }

        private void HandleAllSeatsMissing(BoneTrackedFrame frameData)
        {
            for (int i = 0; i < m_SeatStates.Count; i++)
            {
                HandleSeatMissing(
                    m_SeatStates[i],
                    i >= 0 && i < m_FrameResult.m_PlayerResults.Count ? m_FrameResult.m_PlayerResults[i] : null,
                    frameData);
            }
        }

        private void HandleSeatMissing(
            BoneParserSeatState seatState,
            BoneParserPlayerResult result,
            BoneTrackedFrame frameData,
            bool preservePersonCandidate = false)
        {
            if (seatState == null)
            {
                return;
            }

            if (!preservePersonCandidate)
            {
                seatState.m_AimCandidatePersonId = BoneTrackedLayout.m_InvalidPersonId;
                seatState.m_AimCandidateStableFrameCount = 0;
            }

            seatState.m_MissingFrameCount++;
            float deltaTimeSeconds = ReadDeltaTimeSeconds(frameData, seatState);
            m_TurnEstimator.UpdateMissing(seatState, result, deltaTimeSeconds);
            if (seatState.m_MissingFrameCount > Math.Max(m_Config.m_MaxMissingFrameCount, m_Config.m_AimHoldMissingFrames) &&
                !seatState.m_HasAimOutput)
            {
                seatState.ResetForRelease();
            }
        }

        private void PrepareFrameResult(BoneTrackedFrame frameData, IList<BoneParserSeatDefinition> seatDefinitions)
        {
            int slotCount = seatDefinitions != null ? seatDefinitions.Count : 0;
            m_FrameResult.m_FrameSerial = frameData != null ? frameData.m_FrameSerial : m_FrameResult.m_FrameSerial + 1;
            for (int i = 0; i < slotCount; i++)
            {
                BoneParserSeatDefinition seatDefinition = ReadSeatDefinition(seatDefinitions, i);
                int slotIndex = ReadFrameSlotIndex(seatDefinitions, i);
                int bindingId = seatDefinition != null ? seatDefinition.m_BindingId : i;
                m_FrameResult.m_PlayerResults[i].Reset(slotIndex, bindingId);
            }
        }

        private static BoneParserSeatDefinition ReadSeatDefinition(
            IList<BoneParserSeatDefinition> seatDefinitions,
            int definitionIndex)
        {
            if (seatDefinitions == null || definitionIndex < 0 || definitionIndex >= seatDefinitions.Count)
            {
                return null;
            }

            return seatDefinitions[definitionIndex];
        }

        private static int ReadFrameSlotIndex(IList<BoneParserSeatDefinition> seatDefinitions, int definitionIndex)
        {
            if (seatDefinitions == null || definitionIndex < 0 || definitionIndex >= seatDefinitions.Count)
            {
                return definitionIndex;
            }

            return seatDefinitions[definitionIndex] != null ? seatDefinitions[definitionIndex].m_SlotIndex : definitionIndex;
        }

        private static float ReadFrameTimeSeconds(BoneTrackedFrame frameData)
        {
            if (frameData == null || frameData.m_FrameTimeMs <= 0L)
            {
                return 0f;
            }

            return frameData.m_FrameTimeMs / 1000f;
        }

        private float ReadDeltaTimeSeconds(BoneTrackedFrame frameData, BoneParserSeatState seatState)
        {
            const float defaultDeltaTimeSeconds = 1f / 30f;
            if (seatState == null)
            {
                return defaultDeltaTimeSeconds;
            }

            long currentFrameTimeMs = frameData != null ? frameData.m_FrameTimeMs : 0L;
            float deltaTimeSeconds = defaultDeltaTimeSeconds;
            if (currentFrameTimeMs > 0L &&
                seatState.m_LastFrameTimeMs > 0L &&
                currentFrameTimeMs > seatState.m_LastFrameTimeMs)
            {
                deltaTimeSeconds = BoneMath.Clamp((currentFrameTimeMs - seatState.m_LastFrameTimeMs) / 1000f, 1f / 120f, 0.20f);
            }

            seatState.m_LastFrameTimeMs = currentFrameTimeMs;
            return deltaTimeSeconds;
        }

        private static bool TryReadSlotPerson(BoneTrackedFrame frameData, int slotIndex, out BoneTrackedPerson person)
        {
            if (frameData == null || frameData.m_Persons == null || slotIndex < 0 || slotIndex >= frameData.m_Persons.Count)
            {
                person = null;
                return false;
            }

            person = frameData.m_Persons[slotIndex];
            return person != null;
        }

        private void UpdateFrameSeats(BoneTrackedFrame frameData, IList<BoneParserSeatDefinition> seatDefinitions)
        {
            float frameTimeSeconds = ReadFrameTimeSeconds(frameData);
            for (int definitionIndex = 0; definitionIndex < m_SeatStates.Count; definitionIndex++)
            {
                BoneParserSeatState seatState = m_SeatStates[definitionIndex];
                BoneParserPlayerResult result = m_FrameResult.m_PlayerResults[definitionIndex];
                BoneParserSeatDefinition seatDefinition = ReadSeatDefinition(seatDefinitions, definitionIndex);
                int frameSlotIndex = ReadFrameSlotIndex(seatDefinitions, definitionIndex);
                if (!TryReadSlotPerson(frameData, frameSlotIndex, out BoneTrackedPerson person) ||
                    !m_Skeleton.ReadIsBindablePerson(person))
                {
                    HandleSeatMissing(seatState, result, frameData);
                    continue;
                }

                m_TurnEstimator.NotifyPersonCandidate(seatState, person.m_PersonId);
                if (!m_TurnEstimator.CanAcceptPerson(seatState, person.m_PersonId))
                {
                    HandleSeatMissing(seatState, result, frameData, true);
                    continue;
                }

                if (seatState.m_CurrentPersonId != person.m_PersonId)
                {
                    seatState.ResetForNewPerson(person.m_PersonId);
                }
                else
                {
                    seatState.m_MissingFrameCount = 0;
                }

                result.m_IsTracked = true;
                result.m_PersonId = person.m_PersonId;
                float deltaTimeSeconds = ReadDeltaTimeSeconds(frameData, seatState);
                m_TurnEstimator.UpdateTracked(person, seatState, result, deltaTimeSeconds);

                BoneGestureRuntimeContext context = new BoneGestureRuntimeContext(
                    m_Config,
                    m_Skeleton,
                    m_EventWriter,
                    person,
                    seatState,
                    seatDefinition,
                    result,
                    frameData,
                    deltaTimeSeconds,
                    frameTimeSeconds);
                m_GestureRecognizers.Update(context);
            }
        }
    }
}
