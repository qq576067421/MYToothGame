using System;
using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class BonePoseGestureState : IBoneGestureRuntimeState
    {
        public BonePoseHintFlags m_LastPoseHints = BonePoseHintFlags.无;
        public BonePoseHintFlags m_RawPoseHints = BonePoseHintFlags.无;
        public int m_RawPoseStableFrameCount;

        public void Reset()
        {
            m_LastPoseHints = BonePoseHintFlags.无;
            m_RawPoseHints = BonePoseHintFlags.无;
            m_RawPoseStableFrameCount = 0;
        }
    }

    internal interface IBonePoseRule
    {
        BonePoseHintFlags PoseFlag { get; }

        BoneGestureType GestureType { get; }

        bool ReadIsActive(BoneGestureRuntimeContext context);
    }

    internal sealed class BonePoseGestureRecognizer : IBoneGestureRecognizer
    {
        private const string m_StateKey = "Pose";
        private static readonly BoneGesturePhaseMask m_PoseSupportedPhases =
            BoneGesturePhaseMask.开始 | BoneGesturePhaseMask.持续 | BoneGesturePhaseMask.结束;

        private readonly List<IBonePoseRule> m_Rules = new List<IBonePoseRule>
        {
            new LeftHandRaisePoseRule(),
            new RightHandRaisePoseRule(),
            new CrossChestPoseRule(),
            new HandsOnHipPoseRule(),
            new CrouchPoseRule(),
        };

        public void CollectDefinitions(List<BoneGestureDefinition> definitions)
        {
            for (int i = 0; i < m_Rules.Count; i++)
            {
                definitions.Add(new BoneGestureDefinition(
                    m_Rules[i].GestureType,
                    BoneGestureCategory.姿势,
                    m_PoseSupportedPhases,
                    BoneGesturePhaseMask.无));
            }

            definitions.Add(new BoneGestureDefinition(
                BoneGestureType.举双手_姿势,
                BoneGestureCategory.姿势,
                m_PoseSupportedPhases,
                BoneGesturePhaseMask.无));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<BonePoseGestureState>(m_StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            BonePoseGestureState state = context.m_SeatState.ReadGestureState<BonePoseGestureState>(m_StateKey);
            BonePoseHintFlags previousFlags = state.m_LastPoseHints;
            BonePoseHintFlags rawFlags = CalculateRawPoseHints(context);
            BonePoseHintFlags currentFlags = ReadStablePoseHints(context, state, rawFlags);
            context.m_Result.m_ActivePoseHints = currentFlags;

            for (int i = 0; i < m_Rules.Count; i++)
            {
                IBonePoseRule rule = m_Rules[i];
                bool previousActive = (previousFlags & rule.PoseFlag) == rule.PoseFlag;
                bool currentActive = (currentFlags & rule.PoseFlag) == rule.PoseFlag;
                context.AddBooleanGestureEvent(previousActive, currentActive, rule.GestureType);
            }

            bool previousBothHands = (previousFlags & BonePoseHintFlags.举左手) == BonePoseHintFlags.举左手 &&
                (previousFlags & BonePoseHintFlags.举右手) == BonePoseHintFlags.举右手;
            bool currentBothHands = (currentFlags & BonePoseHintFlags.举左手) == BonePoseHintFlags.举左手 &&
                (currentFlags & BonePoseHintFlags.举右手) == BonePoseHintFlags.举右手;
            context.AddBooleanGestureEvent(previousBothHands, currentBothHands, BoneGestureType.举双手_姿势);

            state.m_LastPoseHints = currentFlags;
        }

        public bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult)
        {
            return false;
        }

        private BonePoseHintFlags CalculateRawPoseHints(BoneGestureRuntimeContext context)
        {
            BonePoseHintFlags flags = BonePoseHintFlags.无;
            if (context.m_Person == null)
            {
                return flags;
            }

            for (int i = 0; i < m_Rules.Count; i++)
            {
                IBonePoseRule rule = m_Rules[i];
                if (rule.ReadIsActive(context))
                {
                    flags |= rule.PoseFlag;
                }
            }

            return flags;
        }

        private static BonePoseHintFlags ReadStablePoseHints(
            BoneGestureRuntimeContext context,
            BonePoseGestureState state,
            BonePoseHintFlags rawFlags)
        {
            if (state == null)
            {
                return rawFlags;
            }

            if (state.m_RawPoseHints == rawFlags)
            {
                state.m_RawPoseStableFrameCount++;
            }
            else
            {
                state.m_RawPoseHints = rawFlags;
                state.m_RawPoseStableFrameCount = 1;
            }

            int stableFrames = Math.Max(1, context.m_Config.m_PoseStableFrames);
            return state.m_RawPoseStableFrameCount >= stableFrames
                ? rawFlags
                : state.m_LastPoseHints;
        }
    }
}
