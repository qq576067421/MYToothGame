using UnityEngine;

namespace BoneSender.TestInput
{
    public enum BoneSenderTestInputHandSide
    {
        None = 0,
        Left = 1,
        Right = 2,
    }

    enum BoneSenderTestInputGesturePhase
    {
        None = 0,
        OutwardExtended = 1,
        FoldRetracted = 2,
    }

    public enum BoneSenderTestTurnState
    {
        Neutral = 0,
        TurningRight = 1,
        StableRight = 2,
        ReturningFromRight = 3,
        TurningLeft = 4,
        StableLeft = 5,
        ReturningFromLeft = 6,
    }

    public sealed class BoneSenderTestInputConfig
    {
        public float m_MinJointScore = 0.20f;
        public int m_HeadRaiseHoldFrameCount = 2;
        public float m_HeadRaiseMarginRatio = 0.05f;
        public int m_SkillHoldFrameCount = 6;
        public int m_SkillCooldownFrameCount = 30;
        public int m_SkillRecoverFrameCount = 5;
        public float m_SkillRaiseMarginRatio = 0.12f;
        public float m_SkillHeadAllowanceRatio = 0.45f;
        public float m_ShoulderCenterStableThreshold = 0.012f;
        public float m_HipCenterStableThreshold = 0.012f;
        public float m_ShoulderDeltaIgnoreThreshold = 0.006f;
        public float m_ShoulderReturnThreshold = 0.008f;
        public float m_ShoulderYNeutralThreshold = 0.012f;
        public float m_ShoulderSideThreshold = 0.010f;
        public int m_TurnStateConfirmFrameCount = 2;
        public float m_TurnSpeedScale = 1.0f;
        public float m_ReturnSpeedScale = 1.0f;
        public float m_StableDamping = 0.40f;
        public float m_StrengthAngleScale = 1.8f;
        public float m_MaxTurnAngle = 45f;
        public bool m_InvertTurnDirection = false;
        public int m_StableEnterFrameCount = 3;
        public float m_StableZoneRadiusRatio = 0.25f;
        public float m_StableEnterSpeedRatioPerSecond = 0.90f;
        public float m_StableExitHysteresisRatio = 1.35f;
        public float m_OutwardSwingDistanceRatio = 0.50f;
        public float m_OutwardSwingSpeedRatioPerSecond = 1.60f;
        public float m_ReturnTriggerRadiusRatio = 0.18f;
        public float m_ReturnTriggerSpeedRatioPerSecond = 1.10f;
        public int m_OutwardSwingMaxFrameCount = 10;
        public int m_ReturnTriggerMaxFrameCount = 10;
        public float m_FoldBaselineFloorRatio = 0.08f;
        public float m_FoldRetractMinDeltaRatio = 0.12f;
        public float m_FoldRetractDeltaScaleRatio = 0.55f;
        public float m_FoldPunchReleaseToleranceRatio = 0.03f;
        public float m_FoldPunchSpeedRatioPerSecond = 1.25f;
        public int m_FoldPunchMaxFrameCount = 12;
        public float m_RearmFoldDeltaRatio = 0.10f;
        public float m_RearmOutwardDistanceRatio = 0.30f;
        public float m_AttackFreezeReleaseSpeedRatioPerSecond = 0.55f;
        public int m_AttackFreezeReleaseFrameCount = 2;
    }

    sealed class BoneSenderTestInputHandState
    {
        public readonly BoneSenderTestInputHandSide m_HandSide;
        public bool m_HasLastWristPosition;
        public Vector2 m_LastWristPosition;
        public bool m_HasLastSideValue;
        public float m_LastSideValue;
        public bool m_HasLastProjectionValue;
        public float m_LastProjectionValue;
        public float m_LastSpeedPerSecond;
        public bool m_HasStableReference;
        public bool m_IsStable;
        public int m_StableCandidateFrameCount;
        public Vector2 m_StableShoulderToWrist;
        public Vector2 m_StableWristWorld;
        public float m_StableFoldN;
        public int m_HeadRaiseHoldFrameCount;
        public BoneSenderTestInputGesturePhase m_GesturePhase;
        public int m_GestureFrameCount;
        public bool m_IsGestureConsumed;

        public BoneSenderTestInputHandState(BoneSenderTestInputHandSide handSide)
        {
            m_HandSide = handSide;
            Reset();
        }

        public void Reset()
        {
            m_HasLastWristPosition = false;
            m_LastWristPosition = Vector2.zero;
            m_HasLastSideValue = false;
            m_LastSideValue = 0f;
            m_HasLastProjectionValue = false;
            m_LastProjectionValue = 0f;
            m_LastSpeedPerSecond = 0f;
            m_HasStableReference = false;
            m_IsStable = false;
            m_StableCandidateFrameCount = 0;
            m_StableShoulderToWrist = Vector2.zero;
            m_StableWristWorld = Vector2.zero;
            m_StableFoldN = 0f;
            m_HeadRaiseHoldFrameCount = 0;
            m_GesturePhase = BoneSenderTestInputGesturePhase.None;
            m_GestureFrameCount = 0;
            m_IsGestureConsumed = false;
        }

        public void ClearTransientState()
        {
            m_HasLastWristPosition = false;
            m_LastWristPosition = Vector2.zero;
            m_HasLastSideValue = false;
            m_LastSideValue = 0f;
            m_HasLastProjectionValue = false;
            m_LastProjectionValue = 0f;
            m_LastSpeedPerSecond = 0f;
            m_IsStable = false;
            m_StableCandidateFrameCount = 0;
            m_HeadRaiseHoldFrameCount = 0;
            ClearGestureRuntime();
        }

        public void ClearGestureProgress()
        {
            m_GesturePhase = BoneSenderTestInputGesturePhase.None;
            m_GestureFrameCount = 0;
        }

        public void ClearGestureRuntime()
        {
            ClearGestureProgress();
            m_IsGestureConsumed = false;
        }
    }

    struct BoneSenderTestInputHandPoseData
    {
        public bool m_IsTracked;
        public bool m_HasElbow;
        public Vector2 m_ShoulderPosition;
        public Vector2 m_ElbowPosition;
        public Vector2 m_WristPosition;
        public Vector2 m_ShoulderToWrist;
        public float m_ShoulderToWristLength;
        public float m_FoldN;
        public float m_SideValue;
        public float m_ProjectionValue;
        public float m_SpeedPerSecond;
        public float m_SideVelocityPerSecond;
        public float m_ProjectionVelocityPerSecond;
    }

    public sealed class BoneSenderTestInputSeatState
    {
        public int m_CurrentPersonId = YouDooSDKConstants.PersonIdNull;
        public int m_SkillCooldownFrameLeft;
        public int m_SkillHoldFrameCount;
        public bool m_IsSkillRecoveryPending;
        public int m_SkillRecoverClearFrameCount;
        public BoneSenderTestInputHandSide m_FrozenHandSide = BoneSenderTestInputHandSide.None;
        public int m_FreezeReleaseFrameCount;
        public long m_LastFrameTimeMs;
        public Vector3 m_LastStableFaceForward = Vector3.forward;
        public BoneSenderTestTurnState m_TurnState = BoneSenderTestTurnState.Neutral;
        public BoneSenderTestTurnState m_TurnStateCandidate = BoneSenderTestTurnState.Neutral;
        public int m_TurnStateCandidateFrameCount;
        public float m_CurrentTurnAngleDegrees;
        public float m_CurrentTurnSpeedDegrees;
        public float m_CurrentTurnStrength;
        public bool m_HasLastTurnMetrics;
        public float m_LastSignedShoulderDeltaNormalized;
        public float m_LastAbsShoulderDeltaNormalized;
        public Vector2 m_LastShoulderCenter;
        public Vector2 m_LastHipCenter;
        public float m_MaxObservedShoulderDeltaNormalized;
        internal readonly BoneSenderTestInputHandState m_LeftHandState = new BoneSenderTestInputHandState(BoneSenderTestInputHandSide.Left);
        internal readonly BoneSenderTestInputHandState m_RightHandState = new BoneSenderTestInputHandState(BoneSenderTestInputHandSide.Right);

        public void ResetForNewPerson(int personId)
        {
            m_CurrentPersonId = personId;
            m_SkillCooldownFrameLeft = 0;
            m_SkillHoldFrameCount = 0;
            m_IsSkillRecoveryPending = false;
            m_SkillRecoverClearFrameCount = 0;
            m_FrozenHandSide = BoneSenderTestInputHandSide.None;
            m_FreezeReleaseFrameCount = 0;
            m_LastFrameTimeMs = 0L;
            m_LastStableFaceForward = Vector3.forward;
            m_TurnState = BoneSenderTestTurnState.Neutral;
            m_TurnStateCandidate = BoneSenderTestTurnState.Neutral;
            m_TurnStateCandidateFrameCount = 0;
            m_CurrentTurnAngleDegrees = 0f;
            m_CurrentTurnSpeedDegrees = 0f;
            m_CurrentTurnStrength = 0f;
            m_HasLastTurnMetrics = false;
            m_LastSignedShoulderDeltaNormalized = 0f;
            m_LastAbsShoulderDeltaNormalized = 0f;
            m_LastShoulderCenter = Vector2.zero;
            m_LastHipCenter = Vector2.zero;
            m_MaxObservedShoulderDeltaNormalized = 0f;
            m_LeftHandState.Reset();
            m_RightHandState.Reset();
        }

        public void ResetForNoPerson()
        {
            ResetForNewPerson(YouDooSDKConstants.PersonIdNull);
        }
    }

    public sealed class BoneSenderTestInputResult
    {
        public int m_PersonId = YouDooSDKConstants.PersonIdNull;
        public bool m_HasPerson;
        public bool m_IsTurnAvailable;
        public string m_TurnUnavailableReason = string.Empty;
        public BoneSenderTestTurnState m_TurnState = BoneSenderTestTurnState.Neutral;
        public float m_LeftShoulderX;
        public float m_RightShoulderX;
        public float m_LeftShoulderY;
        public float m_RightShoulderY;
        public float m_SignedShoulderDeltaNormalized;
        public float m_AbsShoulderDeltaNormalized;
        public float m_ShoulderDeltaChangeNormalized;
        public float m_ShoulderYGap;
        public float m_ShoulderCenterDelta;
        public float m_HipCenterDelta;
        public float m_MaxObservedShoulderDeltaNormalized;
        public float m_TurnSpeed;
        public float m_TurnStrength;
        public float m_TurnAngleDegrees;
        public Vector3 m_FaceForward = Vector3.forward;
        public bool m_IsSkillPoseActive;
        public bool m_ShouldUseSkill;
        public string m_SkillReason = string.Empty;
        public int m_SkillCooldownFrameLeft;
        public int m_SkillHoldFrameCount;
        public bool m_ShouldNormalAttack;
        public string m_NormalAttackReason = string.Empty;
        public BoneSenderTestInputHandSide m_FrozenHandSide = BoneSenderTestInputHandSide.None;
        public bool m_HasHeadTopY;
        public float m_HeadTopY;
        public bool m_HasLeftPose;
        public bool m_HasRightPose;
        public bool m_LeftHeadRaiseActive;
        public bool m_RightHeadRaiseActive;
        public float m_LeftShoulderRaise;
        public float m_RightShoulderRaise;
        public float m_LeftHeadRaise;
        public float m_RightHeadRaise;
        public float m_LeftHandSpeedPerSecond;
        public float m_RightHandSpeedPerSecond;
        public float m_LeftFoldN;
        public float m_RightFoldN;
        public string m_LeftStateText = "无数据";
        public string m_RightStateText = "无数据";
        public string m_LeftGestureText = "无";
        public string m_RightGestureText = "无";
    }

    public sealed class BoneSenderInputTestEvaluator
    {
        private readonly BoneSenderTestInputConfig m_Config = new BoneSenderTestInputConfig();

        public BoneSenderTestInputResult Evaluate(
            BoneProtocolPerson person,
            BoneSenderTestInputSeatState seatState,
            long frameTimeMs)
        {
            var result = new BoneSenderTestInputResult();
            if (seatState == null)
            {
                result.m_TurnUnavailableReason = "状态缺失";
                return result;
            }

            if (person == null || person.m_PersonId == YouDooSDKConstants.PersonIdNull)
            {
                seatState.ResetForNoPerson();
                result.m_TurnUnavailableReason = "空位";
                return result;
            }

            if (seatState.m_CurrentPersonId != person.m_PersonId)
            {
                seatState.ResetForNewPerson(person.m_PersonId);
            }

            result.m_HasPerson = true;
            result.m_PersonId = person.m_PersonId;
            float deltaTimeSeconds = ReadDeltaTimeSeconds(frameTimeMs, seatState);
            TickSeatStateCooldowns(seatState);
            result.m_FaceForward = ResolveFaceForward(person, seatState, result, deltaTimeSeconds);
            result.m_IsSkillPoseActive = IsSkillPoseActive(person);
            result.m_ShouldUseSkill = EvaluateSkill(seatState, result.m_IsSkillPoseActive);
            if (result.m_ShouldUseSkill)
            {
                result.m_SkillReason = "双手举高持续";
            }

            UpdateSkillRecoveryState(seatState, result.m_IsSkillPoseActive, result.m_ShouldUseSkill);
            if (result.m_ShouldUseSkill)
            {
                ResetNormalAttackState(seatState);
                result.m_ShouldNormalAttack = false;
            }
            else
            {
                result.m_ShouldNormalAttack = EvaluateNormalAttack(person, seatState, result.m_IsSkillPoseActive, deltaTimeSeconds, result);
            }

            result.m_SkillCooldownFrameLeft = seatState.m_SkillCooldownFrameLeft;
            result.m_SkillHoldFrameCount = seatState.m_SkillHoldFrameCount;
            result.m_FrozenHandSide = seatState.m_FrozenHandSide;
            result.m_LeftStateText = ReadHandStateText(seatState.m_LeftHandState);
            result.m_RightStateText = ReadHandStateText(seatState.m_RightHandState);
            result.m_LeftGestureText = ReadGestureText(seatState.m_LeftHandState);
            result.m_RightGestureText = ReadGestureText(seatState.m_RightHandState);
            return result;
        }

        private float ReadDeltaTimeSeconds(long frameTimeMs, BoneSenderTestInputSeatState seatState)
        {
            const float defaultDeltaTimeSeconds = 1f / 30f;
            float deltaTimeSeconds = defaultDeltaTimeSeconds;
            if (frameTimeMs > 0L &&
                seatState.m_LastFrameTimeMs > 0L &&
                frameTimeMs > seatState.m_LastFrameTimeMs)
            {
                deltaTimeSeconds = Mathf.Clamp((frameTimeMs - seatState.m_LastFrameTimeMs) / 1000f, 1f / 120f, 0.20f);
            }

            seatState.m_LastFrameTimeMs = frameTimeMs;
            return deltaTimeSeconds;
        }

        private static void TickSeatStateCooldowns(BoneSenderTestInputSeatState seatState)
        {
            if (seatState.m_SkillCooldownFrameLeft > 0)
            {
                seatState.m_SkillCooldownFrameLeft--;
            }
        }

        private void UpdateSkillRecoveryState(
            BoneSenderTestInputSeatState seatState,
            bool isSkillPoseActive,
            bool shouldUseSkill)
        {
            if (shouldUseSkill || isSkillPoseActive)
            {
                seatState.m_IsSkillRecoveryPending = true;
                seatState.m_SkillRecoverClearFrameCount = 0;
                return;
            }

            if (!seatState.m_IsSkillRecoveryPending)
            {
                return;
            }

            seatState.m_SkillRecoverClearFrameCount++;
            if (seatState.m_SkillRecoverClearFrameCount >= m_Config.m_SkillRecoverFrameCount)
            {
                seatState.m_IsSkillRecoveryPending = false;
                seatState.m_SkillRecoverClearFrameCount = 0;
            }
        }

        private void ResetNormalAttackState(BoneSenderTestInputSeatState seatState)
        {
            seatState.m_LeftHandState.Reset();
            seatState.m_RightHandState.Reset();
            ReleaseAttackFreeze(seatState);
        }

        private struct BoneSenderTurnMetrics
        {
            public float m_LeftShoulderX;
            public float m_RightShoulderX;
            public float m_LeftShoulderY;
            public float m_RightShoulderY;
            public float m_SignedShoulderDeltaNormalized;
            public float m_AbsShoulderDeltaNormalized;
            public float m_ShoulderDeltaChangeNormalized;
            public float m_ShoulderYGap;
            public float m_ShoulderCenterDelta;
            public float m_HipCenterDelta;
            public float m_MaxObservedShoulderDeltaNormalized;
            public float m_TurnStrength;
            public float m_TurnSpeedFactor;
        }

        private Vector3 ResolveFaceForward(
            BoneProtocolPerson person,
            BoneSenderTestInputSeatState seatState,
            BoneSenderTestInputResult result,
            float deltaTimeSeconds)
        {
            BoneSenderTurnMetrics metrics;
            string unavailableReason;
            if (!TryCollectTurnMetrics(person, seatState, out metrics, out unavailableReason))
            {
                result.m_IsTurnAvailable = false;
                result.m_TurnUnavailableReason = unavailableReason;
                ClearTurnStateCandidate(seatState);
                seatState.m_CurrentTurnSpeedDegrees = 0f;
                result.m_TurnState = seatState.m_TurnState;
                result.m_TurnSpeed = 0f;
                result.m_TurnStrength = seatState.m_CurrentTurnStrength;
                result.m_TurnAngleDegrees = seatState.m_CurrentTurnAngleDegrees;
                result.m_FaceForward = seatState.m_LastStableFaceForward.sqrMagnitude > 0.0001f
                    ? seatState.m_LastStableFaceForward
                    : BuildFaceForwardByAngle(0f);
                return result.m_FaceForward;
            }

            result.m_IsTurnAvailable = true;
            result.m_TurnUnavailableReason = string.Empty;
            BoneSenderTestTurnState desiredState = ResolveDesiredTurnState(metrics, seatState);
            BoneSenderTestTurnState resolvedState = ConfirmTurnState(seatState, desiredState);
            float targetAngleDegrees = ReadTurnTargetAngleDegrees(metrics, resolvedState);
            float currentAngleDegrees = UpdateTurnAngleDegrees(seatState, resolvedState, targetAngleDegrees, deltaTimeSeconds, metrics);
            result.m_TurnState = resolvedState;
            result.m_LeftShoulderX = metrics.m_LeftShoulderX;
            result.m_RightShoulderX = metrics.m_RightShoulderX;
            result.m_LeftShoulderY = metrics.m_LeftShoulderY;
            result.m_RightShoulderY = metrics.m_RightShoulderY;
            result.m_SignedShoulderDeltaNormalized = metrics.m_SignedShoulderDeltaNormalized;
            result.m_AbsShoulderDeltaNormalized = metrics.m_AbsShoulderDeltaNormalized;
            result.m_ShoulderDeltaChangeNormalized = metrics.m_ShoulderDeltaChangeNormalized;
            result.m_ShoulderYGap = metrics.m_ShoulderYGap;
            result.m_ShoulderCenterDelta = metrics.m_ShoulderCenterDelta;
            result.m_HipCenterDelta = metrics.m_HipCenterDelta;
            result.m_MaxObservedShoulderDeltaNormalized = metrics.m_MaxObservedShoulderDeltaNormalized;
            result.m_TurnStrength = metrics.m_TurnStrength;
            result.m_TurnSpeed = seatState.m_CurrentTurnSpeedDegrees;
            result.m_TurnAngleDegrees = currentAngleDegrees;
            result.m_FaceForward = BuildFaceForwardByAngle(currentAngleDegrees);
            seatState.m_LastStableFaceForward = result.m_FaceForward;
            seatState.m_CurrentTurnStrength = metrics.m_TurnStrength;
            return result.m_FaceForward;
        }

        private bool TryCollectTurnMetrics(
            BoneProtocolPerson person,
            BoneSenderTestInputSeatState seatState,
            out BoneSenderTurnMetrics metrics,
            out string unavailableReason)
        {
            metrics = default;
            unavailableReason = null;
            Vector2 leftShoulder;
            Vector2 rightShoulder;
            Vector2 leftHip;
            Vector2 rightHip;
            if (!TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Leftshoulder, out leftShoulder) ||
                !TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Rightshoulder, out rightShoulder))
            {
                unavailableReason = "缺少双肩点";
                return false;
            }

            if (!TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Lefthip, out leftHip) ||
                !TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Righthip, out rightHip))
            {
                unavailableReason = "缺少双胯点";
                return false;
            }

            Vector2 shoulderCenter = (leftShoulder + rightShoulder) * 0.5f;
            Vector2 hipCenter = (leftHip + rightHip) * 0.5f;
            float torsoHeight = Vector2.Distance(shoulderCenter, hipCenter);
            if (torsoHeight <= 0.0001f)
            {
                unavailableReason = "肩胯中心过近";
                return false;
            }

            metrics.m_LeftShoulderX = leftShoulder.x;
            metrics.m_RightShoulderX = rightShoulder.x;
            metrics.m_LeftShoulderY = leftShoulder.y;
            metrics.m_RightShoulderY = rightShoulder.y;
            metrics.m_SignedShoulderDeltaNormalized = (rightShoulder.y - leftShoulder.y) / torsoHeight;
            metrics.m_AbsShoulderDeltaNormalized = Mathf.Abs(rightShoulder.x - leftShoulder.x) / torsoHeight;
            metrics.m_ShoulderYGap = Mathf.Abs(leftShoulder.y - rightShoulder.y) / torsoHeight;
            metrics.m_ShoulderCenterDelta = 0f;
            metrics.m_HipCenterDelta = 0f;
            metrics.m_ShoulderDeltaChangeNormalized = 0f;

            if (seatState.m_HasLastTurnMetrics)
            {
                metrics.m_ShoulderCenterDelta = Vector2.Distance(shoulderCenter, seatState.m_LastShoulderCenter);
                metrics.m_HipCenterDelta = Vector2.Distance(hipCenter, seatState.m_LastHipCenter);
                metrics.m_ShoulderDeltaChangeNormalized =
                    metrics.m_AbsShoulderDeltaNormalized - seatState.m_LastAbsShoulderDeltaNormalized;
            }

            seatState.m_MaxObservedShoulderDeltaNormalized = Mathf.Max(
                seatState.m_MaxObservedShoulderDeltaNormalized,
                metrics.m_AbsShoulderDeltaNormalized);
            metrics.m_MaxObservedShoulderDeltaNormalized = seatState.m_MaxObservedShoulderDeltaNormalized;
            seatState.m_HasLastTurnMetrics = true;
            seatState.m_LastSignedShoulderDeltaNormalized = metrics.m_SignedShoulderDeltaNormalized;
            seatState.m_LastAbsShoulderDeltaNormalized = metrics.m_AbsShoulderDeltaNormalized;
            seatState.m_LastShoulderCenter = shoulderCenter;
            seatState.m_LastHipCenter = hipCenter;

            float referenceShoulderDelta = Mathf.Max(
                metrics.m_MaxObservedShoulderDeltaNormalized,
                m_Config.m_ShoulderSideThreshold * 2f);
            float widthLoss = Mathf.Max(0f, referenceShoulderDelta - metrics.m_AbsShoulderDeltaNormalized);
            metrics.m_TurnStrength = Mathf.Clamp01(
                widthLoss / Mathf.Max(referenceShoulderDelta, 0.0001f) * Mathf.Max(0.01f, m_Config.m_StrengthAngleScale));
            metrics.m_TurnSpeedFactor = Mathf.Clamp01(
                Mathf.Abs(metrics.m_ShoulderDeltaChangeNormalized) / Mathf.Max(m_Config.m_ShoulderDeltaIgnoreThreshold, 0.0001f));
            return true;
        }

        private BoneSenderTestTurnState ResolveDesiredTurnState(
            BoneSenderTurnMetrics metrics,
            BoneSenderTestInputSeatState seatState)
        {
            bool isShoulderCenterStable = metrics.m_ShoulderCenterDelta <= m_Config.m_ShoulderCenterStableThreshold;
            bool isHipCenterStable = metrics.m_HipCenterDelta <= m_Config.m_HipCenterStableThreshold;
            bool isCenterStable = isShoulderCenterStable && isHipCenterStable;
            bool isShoulderNearlyLevel = metrics.m_ShoulderYGap <= m_Config.m_ShoulderYNeutralThreshold;
            bool isNearObservedFront =
                metrics.m_MaxObservedShoulderDeltaNormalized > 0f &&
                metrics.m_MaxObservedShoulderDeltaNormalized - metrics.m_AbsShoulderDeltaNormalized <= m_Config.m_ShoulderSideThreshold;
            int sideSign = ReadTurnDirectionSign(metrics.m_SignedShoulderDeltaNormalized);
            bool isTurningDeeper = metrics.m_ShoulderDeltaChangeNormalized <= -m_Config.m_ShoulderDeltaIgnoreThreshold;
            bool isReturning = metrics.m_ShoulderDeltaChangeNormalized >= m_Config.m_ShoulderReturnThreshold;

            if (sideSign == 0 && isNearObservedFront && isShoulderNearlyLevel)
            {
                return BoneSenderTestTurnState.Neutral;
            }

            if (!isCenterStable)
            {
                return ResolveTurnStateWhenCentersMoving(
                    seatState.m_TurnState,
                    sideSign,
                    isShoulderNearlyLevel,
                    isNearObservedFront);
            }

            if (sideSign > 0)
            {
                if (isTurningDeeper)
                {
                    return BoneSenderTestTurnState.TurningRight;
                }

                if (isReturning)
                {
                    return BoneSenderTestTurnState.ReturningFromRight;
                }

                return BoneSenderTestTurnState.StableRight;
            }

            if (sideSign < 0)
            {
                if (isTurningDeeper)
                {
                    return BoneSenderTestTurnState.TurningLeft;
                }

                if (isReturning)
                {
                    return BoneSenderTestTurnState.ReturningFromLeft;
                }

                return BoneSenderTestTurnState.StableLeft;
            }

            if (IsRightTurnState(seatState.m_TurnState))
            {
                if (isNearObservedFront && isShoulderNearlyLevel)
                {
                    return BoneSenderTestTurnState.Neutral;
                }

                return isReturning ? BoneSenderTestTurnState.ReturningFromRight : BoneSenderTestTurnState.StableRight;
            }

            if (IsLeftTurnState(seatState.m_TurnState))
            {
                if (isNearObservedFront && isShoulderNearlyLevel)
                {
                    return BoneSenderTestTurnState.Neutral;
                }

                return isReturning ? BoneSenderTestTurnState.ReturningFromLeft : BoneSenderTestTurnState.StableLeft;
            }

            return BoneSenderTestTurnState.Neutral;
        }

        private BoneSenderTestTurnState ResolveTurnStateWhenCentersMoving(
            BoneSenderTestTurnState currentState,
            int sideSign,
            bool isShoulderNearlyLevel,
            bool isNearObservedFront)
        {
            if (sideSign == 0 && isNearObservedFront && isShoulderNearlyLevel)
            {
                return BoneSenderTestTurnState.Neutral;
            }

            if (IsRightTurnState(currentState))
            {
                return sideSign < 0 ? BoneSenderTestTurnState.Neutral : BoneSenderTestTurnState.StableRight;
            }

            if (IsLeftTurnState(currentState))
            {
                return sideSign > 0 ? BoneSenderTestTurnState.Neutral : BoneSenderTestTurnState.StableLeft;
            }

            if (sideSign > 0)
            {
                return BoneSenderTestTurnState.StableRight;
            }

            if (sideSign < 0)
            {
                return BoneSenderTestTurnState.StableLeft;
            }

            return BoneSenderTestTurnState.Neutral;
        }

        private BoneSenderTestTurnState ConfirmTurnState(
            BoneSenderTestInputSeatState seatState,
            BoneSenderTestTurnState desiredState)
        {
            if (seatState.m_TurnState == desiredState)
            {
                ClearTurnStateCandidate(seatState);
                return seatState.m_TurnState;
            }

            if (IsSameTurnFamily(seatState.m_TurnState, desiredState))
            {
                seatState.m_TurnState = desiredState;
                ClearTurnStateCandidate(seatState);
                return seatState.m_TurnState;
            }

            if (seatState.m_TurnStateCandidate != desiredState)
            {
                seatState.m_TurnStateCandidate = desiredState;
                seatState.m_TurnStateCandidateFrameCount = 1;
                return seatState.m_TurnState;
            }

            seatState.m_TurnStateCandidateFrameCount++;
            if (seatState.m_TurnStateCandidateFrameCount >= Mathf.Max(1, m_Config.m_TurnStateConfirmFrameCount))
            {
                seatState.m_TurnState = desiredState;
                ClearTurnStateCandidate(seatState);
            }

            return seatState.m_TurnState;
        }

        private static void ClearTurnStateCandidate(BoneSenderTestInputSeatState seatState)
        {
            seatState.m_TurnStateCandidate = seatState.m_TurnState;
            seatState.m_TurnStateCandidateFrameCount = 0;
        }

        private int ReadTurnDirectionSign(float signedShoulderDeltaNormalized)
        {
            if (signedShoulderDeltaNormalized > m_Config.m_ShoulderSideThreshold)
            {
                return m_Config.m_InvertTurnDirection ? -1 : 1;
            }

            if (signedShoulderDeltaNormalized < -m_Config.m_ShoulderSideThreshold)
            {
                return m_Config.m_InvertTurnDirection ? 1 : -1;
            }

            return 0;
        }

        private float ReadTurnTargetAngleDegrees(
            BoneSenderTurnMetrics metrics,
            BoneSenderTestTurnState turnState)
        {
            float maxAngle = ReadClampedMaxTurnAngle();
            switch (turnState)
            {
                case BoneSenderTestTurnState.TurningRight:
                case BoneSenderTestTurnState.StableRight:
                    return metrics.m_TurnStrength * maxAngle;
                case BoneSenderTestTurnState.TurningLeft:
                case BoneSenderTestTurnState.StableLeft:
                    return -metrics.m_TurnStrength * maxAngle;
                default:
                    return 0f;
            }
        }

        private float UpdateTurnAngleDegrees(
            BoneSenderTestInputSeatState seatState,
            BoneSenderTestTurnState turnState,
            float targetAngleDegrees,
            float deltaTimeSeconds,
            BoneSenderTurnMetrics metrics)
        {
            float clampedMaxAngle = ReadClampedMaxTurnAngle();
            float safeDeltaTimeSeconds = Mathf.Max(deltaTimeSeconds, 1f / 120f);
            float previousAngleDegrees = seatState.m_CurrentTurnAngleDegrees;
            float currentAngleDegrees = previousAngleDegrees;
            float safeTargetAngleDegrees = Mathf.Clamp(targetAngleDegrees, -clampedMaxAngle, clampedMaxAngle);
            switch (turnState)
            {
                case BoneSenderTestTurnState.TurningRight:
                case BoneSenderTestTurnState.TurningLeft:
                    float turnStep = clampedMaxAngle *
                        safeDeltaTimeSeconds *
                        Mathf.Max(0.60f, 1.25f + metrics.m_TurnSpeedFactor * 1.75f) *
                        Mathf.Max(0.10f, m_Config.m_TurnSpeedScale);
                    currentAngleDegrees = Mathf.MoveTowards(previousAngleDegrees, safeTargetAngleDegrees, turnStep);
                    break;

                case BoneSenderTestTurnState.StableRight:
                case BoneSenderTestTurnState.StableLeft:
                    currentAngleDegrees = Mathf.Lerp(previousAngleDegrees, safeTargetAngleDegrees, Mathf.Clamp01(m_Config.m_StableDamping));
                    if (Mathf.Abs(currentAngleDegrees - safeTargetAngleDegrees) <= 0.05f)
                    {
                        currentAngleDegrees = safeTargetAngleDegrees;
                    }

                    break;

                case BoneSenderTestTurnState.ReturningFromRight:
                case BoneSenderTestTurnState.ReturningFromLeft:
                case BoneSenderTestTurnState.Neutral:
                default:
                    float returnStep = clampedMaxAngle *
                        safeDeltaTimeSeconds *
                        Mathf.Max(0.75f, 1.00f + metrics.m_TurnSpeedFactor * 1.50f) *
                        Mathf.Max(0.10f, m_Config.m_ReturnSpeedScale);
                    currentAngleDegrees = Mathf.MoveTowards(previousAngleDegrees, 0f, returnStep);
                    if (Mathf.Abs(currentAngleDegrees) <= 0.05f)
                    {
                        currentAngleDegrees = 0f;
                    }

                    break;
            }

            currentAngleDegrees = Mathf.Clamp(currentAngleDegrees, -clampedMaxAngle, clampedMaxAngle);
            seatState.m_CurrentTurnAngleDegrees = currentAngleDegrees;
            seatState.m_CurrentTurnSpeedDegrees = (currentAngleDegrees - previousAngleDegrees) / safeDeltaTimeSeconds;
            return currentAngleDegrees;
        }

        private float ReadClampedMaxTurnAngle()
        {
            return Mathf.Clamp(m_Config.m_MaxTurnAngle, 0f, 45f);
        }

        private static Vector3 BuildFaceForwardByAngle(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            Vector3 faceForward = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            if (faceForward.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            faceForward.Normalize();
            return faceForward;
        }

        private static bool IsRightTurnState(BoneSenderTestTurnState turnState)
        {
            return turnState == BoneSenderTestTurnState.TurningRight ||
                turnState == BoneSenderTestTurnState.StableRight ||
                turnState == BoneSenderTestTurnState.ReturningFromRight;
        }

        private static bool IsLeftTurnState(BoneSenderTestTurnState turnState)
        {
            return turnState == BoneSenderTestTurnState.TurningLeft ||
                turnState == BoneSenderTestTurnState.StableLeft ||
                turnState == BoneSenderTestTurnState.ReturningFromLeft;
        }

        private static bool IsSameTurnFamily(BoneSenderTestTurnState currentState, BoneSenderTestTurnState targetState)
        {
            if (currentState == BoneSenderTestTurnState.Neutral || targetState == BoneSenderTestTurnState.Neutral)
            {
                return currentState == targetState;
            }

            return (IsRightTurnState(currentState) && IsRightTurnState(targetState)) ||
                (IsLeftTurnState(currentState) && IsLeftTurnState(targetState));
        }

        private bool EvaluateNormalAttack(
            BoneProtocolPerson person,
            BoneSenderTestInputSeatState seatState,
            bool isSkillPoseActive,
            float deltaTimeSeconds,
            BoneSenderTestInputResult result)
        {
            float shoulderWidth;
            if (!TryReadShoulderWidth(person, out shoulderWidth))
            {
                ResetNormalAttackState(seatState);
                return false;
            }

            BoneSenderTestInputHandPoseData leftPose;
            BoneSenderTestInputHandPoseData rightPose;
            bool hasLeftPose = TryReadHandPoseData(person, seatState.m_LeftHandState, shoulderWidth, deltaTimeSeconds, out leftPose);
            bool hasRightPose = TryReadHandPoseData(person, seatState.m_RightHandState, shoulderWidth, deltaTimeSeconds, out rightPose);
            result.m_HasLeftPose = hasLeftPose;
            result.m_HasRightPose = hasRightPose;
            if (!hasLeftPose)
            {
                seatState.m_LeftHandState.ClearTransientState();
            }

            if (!hasRightPose)
            {
                seatState.m_RightHandState.ClearTransientState();
            }

            float headTopY;
            bool hasHeadTopY = TryReadHeadTopY(person, out headTopY);
            result.m_HasHeadTopY = hasHeadTopY;
            result.m_HeadTopY = headTopY;
            bool leftHeadRaiseAttack = hasLeftPose &&
                UpdateHeadRaiseAttackState(seatState.m_LeftHandState, leftPose, hasHeadTopY, headTopY, shoulderWidth);
            bool rightHeadRaiseAttack = hasRightPose &&
                UpdateHeadRaiseAttackState(seatState.m_RightHandState, rightPose, hasHeadTopY, headTopY, shoulderWidth);
            result.m_LeftHeadRaiseActive = leftHeadRaiseAttack;
            result.m_RightHeadRaiseActive = rightHeadRaiseAttack;

            if (hasLeftPose)
            {
                UpdateStableState(seatState.m_LeftHandState, leftPose, shoulderWidth);
                FillHandResult(leftPose, headTopY, hasHeadTopY, out result.m_LeftShoulderRaise, out result.m_LeftHeadRaise, out result.m_LeftHandSpeedPerSecond, out result.m_LeftFoldN);
            }

            if (hasRightPose)
            {
                UpdateStableState(seatState.m_RightHandState, rightPose, shoulderWidth);
                FillHandResult(rightPose, headTopY, hasHeadTopY, out result.m_RightShoulderRaise, out result.m_RightHeadRaise, out result.m_RightHandSpeedPerSecond, out result.m_RightFoldN);
            }

            bool shouldNormalAttack = false;
            if (!isSkillPoseActive && !seatState.m_IsSkillRecoveryPending)
            {
                if (!leftHeadRaiseAttack && rightHeadRaiseAttack)
                {
                    shouldNormalAttack = true;
                    result.m_NormalAttackReason = "右手过头持续";
                }

                if (!shouldNormalAttack && hasLeftPose && !leftHeadRaiseAttack)
                {
                    shouldNormalAttack = TryEvaluateGestureAttack(seatState.m_LeftHandState, leftPose, seatState, shoulderWidth, out result.m_NormalAttackReason);
                }

                if (!shouldNormalAttack && hasRightPose && !rightHeadRaiseAttack)
                {
                    shouldNormalAttack = TryEvaluateGestureAttack(seatState.m_RightHandState, rightPose, seatState, shoulderWidth, out result.m_NormalAttackReason);
                }
            }
            else
            {
                seatState.m_LeftHandState.ClearGestureProgress();
                seatState.m_RightHandState.ClearGestureProgress();
            }

            UpdateAttackFreezeState(seatState, hasLeftPose, hasRightPose, shoulderWidth);
            if (hasLeftPose)
            {
                CommitHandPoseData(seatState.m_LeftHandState, leftPose);
            }

            if (hasRightPose)
            {
                CommitHandPoseData(seatState.m_RightHandState, rightPose);
            }

            return shouldNormalAttack;
        }

        private static void FillHandResult(
            BoneSenderTestInputHandPoseData poseData,
            float headTopY,
            bool hasHeadTopY,
            out float shoulderRaise,
            out float headRaise,
            out float speedPerSecond,
            out float foldN)
        {
            shoulderRaise = poseData.m_ShoulderPosition.y - poseData.m_WristPosition.y;
            headRaise = hasHeadTopY ? headTopY - poseData.m_WristPosition.y : 0f;
            speedPerSecond = poseData.m_SpeedPerSecond;
            foldN = poseData.m_FoldN;
        }

        private bool EvaluateSkill(
            BoneSenderTestInputSeatState seatState,
            bool isSkillPoseActive)
        {
            if (!isSkillPoseActive)
            {
                seatState.m_SkillHoldFrameCount = 0;
                return false;
            }

            if (seatState.m_SkillCooldownFrameLeft > 0)
            {
                seatState.m_SkillHoldFrameCount = 0;
                return false;
            }

            seatState.m_SkillHoldFrameCount++;
            if (seatState.m_SkillHoldFrameCount < m_Config.m_SkillHoldFrameCount)
            {
                return false;
            }

            seatState.m_SkillHoldFrameCount = 0;
            seatState.m_SkillCooldownFrameLeft = m_Config.m_SkillCooldownFrameCount;
            return true;
        }

        private bool IsSkillPoseActive(BoneProtocolPerson person)
        {
            Vector2 leftShoulder;
            Vector2 rightShoulder;
            Vector2 leftWrist;
            Vector2 rightWrist;
            float shoulderWidth;
            if (!TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Leftshoulder, out leftShoulder) ||
                !TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Rightshoulder, out rightShoulder) ||
                !TryReadHandWristBySide(person, BoneSenderTestInputHandSide.Left, out leftWrist, out _) ||
                !TryReadHandWristBySide(person, BoneSenderTestInputHandSide.Right, out rightWrist, out _) ||
                !TryReadShoulderWidth(person, out shoulderWidth))
            {
                return false;
            }

            float raiseMargin = shoulderWidth * m_Config.m_SkillRaiseMarginRatio;
            bool leftRaised = leftWrist.y <= leftShoulder.y - raiseMargin;
            bool rightRaised = rightWrist.y <= rightShoulder.y - raiseMargin;
            if (!leftRaised || !rightRaised)
            {
                return false;
            }

            Vector2 nose;
            bool hasNose = TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Nose, out nose);
            return !hasNose ||
                (leftWrist.y <= nose.y + shoulderWidth * m_Config.m_SkillHeadAllowanceRatio &&
                 rightWrist.y <= nose.y + shoulderWidth * m_Config.m_SkillHeadAllowanceRatio);
        }

        private bool UpdateHeadRaiseAttackState(
            BoneSenderTestInputHandState handState,
            BoneSenderTestInputHandPoseData poseData,
            bool hasHeadTopY,
            float headTopY,
            float shoulderWidth)
        {
            if (!poseData.m_IsTracked || !hasHeadTopY)
            {
                handState.m_HeadRaiseHoldFrameCount = 0;
                return false;
            }

            float headRaiseThresholdY = headTopY - shoulderWidth * m_Config.m_HeadRaiseMarginRatio;
            if (poseData.m_WristPosition.y <= headRaiseThresholdY)
            {
                handState.m_HeadRaiseHoldFrameCount++;
                if (handState.m_HeadRaiseHoldFrameCount >= m_Config.m_HeadRaiseHoldFrameCount)
                {
                    handState.ClearGestureProgress();
                    return true;
                }

                return false;
            }

            handState.m_HeadRaiseHoldFrameCount = 0;
            return false;
        }

        private void UpdateStableState(
            BoneSenderTestInputHandState handState,
            BoneSenderTestInputHandPoseData poseData,
            float shoulderWidth)
        {
            float enterRadius = shoulderWidth * m_Config.m_StableZoneRadiusRatio;
            float exitRadius = enterRadius * m_Config.m_StableExitHysteresisRatio;
            float enterSpeed = shoulderWidth * m_Config.m_StableEnterSpeedRatioPerSecond;
            float exitSpeed = enterSpeed * m_Config.m_StableExitHysteresisRatio;
            if (!handState.m_HasStableReference)
            {
                if (poseData.m_SpeedPerSecond <= enterSpeed)
                {
                    handState.m_StableCandidateFrameCount++;
                }
                else
                {
                    handState.m_StableCandidateFrameCount = 0;
                }

                if (handState.m_StableCandidateFrameCount >= m_Config.m_StableEnterFrameCount)
                {
                    handState.m_HasStableReference = true;
                    handState.m_IsStable = true;
                    handState.m_StableShoulderToWrist = poseData.m_ShoulderToWrist;
                    handState.m_StableWristWorld = poseData.m_WristPosition;
                    handState.m_StableFoldN = poseData.m_HasElbow ? poseData.m_FoldN : 0f;
                }

                return;
            }

            float distanceToStable = Vector2.Distance(poseData.m_ShoulderToWrist, handState.m_StableShoulderToWrist);
            bool canEnterStable = distanceToStable <= enterRadius && poseData.m_SpeedPerSecond <= enterSpeed;
            bool canStayStable = distanceToStable <= exitRadius && poseData.m_SpeedPerSecond <= exitSpeed;
            if (canEnterStable)
            {
                handState.m_StableCandidateFrameCount++;
                if (handState.m_StableCandidateFrameCount >= m_Config.m_StableEnterFrameCount)
                {
                    handState.m_IsStable = true;
                }
            }
            else if (canStayStable)
            {
                handState.m_IsStable = true;
                handState.m_StableCandidateFrameCount = Mathf.Max(handState.m_StableCandidateFrameCount, m_Config.m_StableEnterFrameCount);
            }
            else
            {
                handState.m_IsStable = false;
                handState.m_StableCandidateFrameCount = 0;
            }

            if (!handState.m_IsStable)
            {
                return;
            }

            handState.m_StableShoulderToWrist = Vector2.Lerp(handState.m_StableShoulderToWrist, poseData.m_ShoulderToWrist, 0.25f);
            handState.m_StableWristWorld = Vector2.Lerp(handState.m_StableWristWorld, poseData.m_WristPosition, 0.25f);
            if (poseData.m_HasElbow)
            {
                handState.m_StableFoldN = Mathf.Lerp(handState.m_StableFoldN, poseData.m_FoldN, 0.25f);
            }
        }

        private bool TryEvaluateGestureAttack(
            BoneSenderTestInputHandState handState,
            BoneSenderTestInputHandPoseData poseData,
            BoneSenderTestInputSeatState seatState,
            float shoulderWidth,
            out string triggerReason)
        {
            triggerReason = string.Empty;
            if (!handState.m_HasStableReference)
            {
                return false;
            }

            if (handState.m_IsGestureConsumed)
            {
                if (!CanReleaseGestureConsumed(handState, poseData, shoulderWidth))
                {
                    return false;
                }

                handState.m_IsGestureConsumed = false;
            }

            if (IsGestureEvaluationBlocked(seatState, handState))
            {
                return false;
            }

            if (handState.m_GesturePhase != BoneSenderTestInputGesturePhase.None)
            {
                handState.m_GestureFrameCount++;
                switch (handState.m_GesturePhase)
                {
                    case BoneSenderTestInputGesturePhase.OutwardExtended:
                        if (handState.m_GestureFrameCount > m_Config.m_OutwardSwingMaxFrameCount + m_Config.m_ReturnTriggerMaxFrameCount)
                        {
                            handState.ClearGestureProgress();
                            return false;
                        }

                        if (ShouldTriggerOutwardGesture(handState, poseData, shoulderWidth))
                        {
                            handState.ClearGestureProgress();
                            handState.m_IsGestureConsumed = true;
                            triggerReason = handState.m_HandSide == BoneSenderTestInputHandSide.Left ? "左手外挥回收" : "右手外挥回收";
                            return true;
                        }

                        return false;

                    case BoneSenderTestInputGesturePhase.FoldRetracted:
                        if (handState.m_GestureFrameCount > m_Config.m_FoldPunchMaxFrameCount)
                        {
                            handState.ClearGestureProgress();
                            return false;
                        }

                        if (ShouldTriggerFoldGesture(handState, poseData, shoulderWidth))
                        {
                            handState.ClearGestureProgress();
                            handState.m_IsGestureConsumed = true;
                            triggerReason = handState.m_HandSide == BoneSenderTestInputHandSide.Left ? "左手收拳出击" : "右手收拳出击";
                            return true;
                        }

                        return false;
                }
            }

            if (CanStartOutwardGesture(handState, poseData, shoulderWidth))
            {
                handState.m_GesturePhase = BoneSenderTestInputGesturePhase.OutwardExtended;
                handState.m_GestureFrameCount = 0;
                BeginAttackFreeze(seatState, handState.m_HandSide);
                return false;
            }

            if (CanStartFoldGesture(handState, poseData))
            {
                handState.m_GesturePhase = BoneSenderTestInputGesturePhase.FoldRetracted;
                handState.m_GestureFrameCount = 0;
                BeginAttackFreeze(seatState, handState.m_HandSide);
                return false;
            }

            return false;
        }

        private bool CanStartOutwardGesture(
            BoneSenderTestInputHandState handState,
            BoneSenderTestInputHandPoseData poseData,
            float shoulderWidth)
        {
            float outwardDistance = ReadOutwardDistance(handState, poseData);
            return outwardDistance >= shoulderWidth * m_Config.m_OutwardSwingDistanceRatio &&
                poseData.m_SideVelocityPerSecond >= shoulderWidth * m_Config.m_OutwardSwingSpeedRatioPerSecond;
        }

        private bool ShouldTriggerOutwardGesture(
            BoneSenderTestInputHandState handState,
            BoneSenderTestInputHandPoseData poseData,
            float shoulderWidth)
        {
            float returnRadius = shoulderWidth * m_Config.m_ReturnTriggerRadiusRatio;
            float distanceToStable = Vector2.Distance(poseData.m_ShoulderToWrist, handState.m_StableShoulderToWrist);
            return poseData.m_SideVelocityPerSecond <= -shoulderWidth * m_Config.m_ReturnTriggerSpeedRatioPerSecond &&
                distanceToStable <= returnRadius;
        }

        private bool CanStartFoldGesture(
            BoneSenderTestInputHandState handState,
            BoneSenderTestInputHandPoseData poseData)
        {
            if (!poseData.m_HasElbow)
            {
                return false;
            }

            float deltaFold = poseData.m_FoldN - handState.m_StableFoldN;
            float baseFold = Mathf.Max(Mathf.Abs(handState.m_StableFoldN), m_Config.m_FoldBaselineFloorRatio);
            return deltaFold >= m_Config.m_FoldRetractMinDeltaRatio &&
                deltaFold >= baseFold * m_Config.m_FoldRetractDeltaScaleRatio;
        }

        private bool ShouldTriggerFoldGesture(
            BoneSenderTestInputHandState handState,
            BoneSenderTestInputHandPoseData poseData,
            float shoulderWidth)
        {
            if (!poseData.m_HasElbow)
            {
                return false;
            }

            float stableLength = handState.m_StableShoulderToWrist.magnitude;
            float lengthTolerance = shoulderWidth * m_Config.m_ReturnTriggerRadiusRatio;
            return poseData.m_FoldN <= handState.m_StableFoldN + m_Config.m_FoldPunchReleaseToleranceRatio &&
                poseData.m_ProjectionValue >= stableLength - lengthTolerance &&
                poseData.m_ProjectionVelocityPerSecond >= shoulderWidth * m_Config.m_FoldPunchSpeedRatioPerSecond;
        }

        private bool CanReleaseGestureConsumed(
            BoneSenderTestInputHandState handState,
            BoneSenderTestInputHandPoseData poseData,
            float shoulderWidth)
        {
            if (poseData.m_HasElbow &&
                poseData.m_FoldN - handState.m_StableFoldN >= m_Config.m_RearmFoldDeltaRatio)
            {
                return true;
            }

            return ReadOutwardDistance(handState, poseData) >= shoulderWidth * m_Config.m_RearmOutwardDistanceRatio;
        }

        private static float ReadOutwardDistance(BoneSenderTestInputHandState handState, BoneSenderTestInputHandPoseData poseData)
        {
            float sideSign = ReadSideSign(handState.m_HandSide);
            float stableSideValue = sideSign * handState.m_StableShoulderToWrist.x;
            return poseData.m_SideValue - stableSideValue;
        }

        private static bool IsGestureEvaluationBlocked(BoneSenderTestInputSeatState seatState, BoneSenderTestInputHandState handState)
        {
            return seatState.m_FrozenHandSide != BoneSenderTestInputHandSide.None &&
                seatState.m_FrozenHandSide != handState.m_HandSide;
        }

        private static void BeginAttackFreeze(BoneSenderTestInputSeatState seatState, BoneSenderTestInputHandSide handSide)
        {
            seatState.m_FrozenHandSide = handSide;
            seatState.m_FreezeReleaseFrameCount = 0;
        }

        private void UpdateAttackFreezeState(
            BoneSenderTestInputSeatState seatState,
            bool hasLeftPose,
            bool hasRightPose,
            float shoulderWidth)
        {
            if (seatState.m_FrozenHandSide == BoneSenderTestInputHandSide.None)
            {
                return;
            }

            BoneSenderTestInputHandState frozenHandState = seatState.m_FrozenHandSide == BoneSenderTestInputHandSide.Left
                ? seatState.m_LeftHandState
                : seatState.m_RightHandState;
            bool hasFrozenPose = seatState.m_FrozenHandSide == BoneSenderTestInputHandSide.Left ? hasLeftPose : hasRightPose;
            if (!hasFrozenPose)
            {
                ReleaseAttackFreeze(seatState);
                return;
            }

            if (frozenHandState.m_IsStable)
            {
                ReleaseAttackFreeze(seatState);
                return;
            }

            float releaseSpeed = shoulderWidth * m_Config.m_AttackFreezeReleaseSpeedRatioPerSecond;
            if (frozenHandState.m_LastSpeedPerSecond <= releaseSpeed)
            {
                seatState.m_FreezeReleaseFrameCount++;
                if (seatState.m_FreezeReleaseFrameCount >= m_Config.m_AttackFreezeReleaseFrameCount)
                {
                    ReleaseAttackFreeze(seatState);
                }
            }
            else
            {
                seatState.m_FreezeReleaseFrameCount = 0;
            }
        }

        private static void ReleaseAttackFreeze(BoneSenderTestInputSeatState seatState)
        {
            seatState.m_FrozenHandSide = BoneSenderTestInputHandSide.None;
            seatState.m_FreezeReleaseFrameCount = 0;
        }

        private static void CommitHandPoseData(BoneSenderTestInputHandState handState, BoneSenderTestInputHandPoseData poseData)
        {
            handState.m_HasLastWristPosition = true;
            handState.m_LastWristPosition = poseData.m_WristPosition;
            handState.m_HasLastSideValue = true;
            handState.m_LastSideValue = poseData.m_SideValue;
            handState.m_HasLastProjectionValue = true;
            handState.m_LastProjectionValue = poseData.m_ProjectionValue;
            handState.m_LastSpeedPerSecond = poseData.m_SpeedPerSecond;
        }

        private bool TryReadHandPoseData(
            BoneProtocolPerson person,
            BoneSenderTestInputHandState handState,
            float shoulderWidth,
            float deltaTimeSeconds,
            out BoneSenderTestInputHandPoseData poseData)
        {
            poseData = default(BoneSenderTestInputHandPoseData);
            int shoulderIndex = handState.m_HandSide == BoneSenderTestInputHandSide.Left
                ? (int)YouDooSDKConstants.KeyPointIndex.Leftshoulder
                : (int)YouDooSDKConstants.KeyPointIndex.Rightshoulder;
            int elbowIndex = handState.m_HandSide == BoneSenderTestInputHandSide.Left
                ? (int)YouDooSDKConstants.KeyPointIndex.Leftelbow
                : (int)YouDooSDKConstants.KeyPointIndex.Rightelbow;
            if (!TryReadBodyJoint(person, shoulderIndex, out poseData.m_ShoulderPosition) ||
                !TryReadHandWristBySide(person, handState.m_HandSide, out poseData.m_WristPosition, out _))
            {
                return false;
            }

            poseData.m_IsTracked = true;
            poseData.m_HasElbow = TryReadBodyJoint(person, elbowIndex, out poseData.m_ElbowPosition);
            poseData.m_ShoulderToWrist = poseData.m_WristPosition - poseData.m_ShoulderPosition;
            poseData.m_ShoulderToWristLength = poseData.m_ShoulderToWrist.magnitude;
            float safeDeltaTimeSeconds = Mathf.Max(deltaTimeSeconds, 0.0001f);
            poseData.m_SpeedPerSecond = handState.m_HasLastWristPosition
                ? Vector2.Distance(poseData.m_WristPosition, handState.m_LastWristPosition) / safeDeltaTimeSeconds
                : 0f;

            float sideSign = ReadSideSign(handState.m_HandSide);
            poseData.m_SideValue = sideSign * poseData.m_ShoulderToWrist.x;
            poseData.m_SideVelocityPerSecond = handState.m_HasLastSideValue
                ? (poseData.m_SideValue - handState.m_LastSideValue) / safeDeltaTimeSeconds
                : 0f;

            Vector2 referenceDirection = ReadReferenceDirection(handState, poseData.m_ShoulderToWrist);
            poseData.m_ProjectionValue = Vector2.Dot(poseData.m_ShoulderToWrist, referenceDirection);
            poseData.m_ProjectionVelocityPerSecond = handState.m_HasLastProjectionValue
                ? (poseData.m_ProjectionValue - handState.m_LastProjectionValue) / safeDeltaTimeSeconds
                : 0f;

            poseData.m_FoldN = poseData.m_HasElbow
                ? sideSign * (poseData.m_ElbowPosition.x - poseData.m_WristPosition.x) / Mathf.Max(shoulderWidth, 0.001f)
                : 0f;
            return true;
        }

        private static Vector2 ReadReferenceDirection(BoneSenderTestInputHandState handState, Vector2 currentShoulderToWrist)
        {
            Vector2 referenceDirection = handState.m_HasStableReference
                ? handState.m_StableShoulderToWrist
                : currentShoulderToWrist;
            if (referenceDirection.sqrMagnitude > 0.0001f)
            {
                return referenceDirection.normalized;
            }

            float sideSign = ReadSideSign(handState.m_HandSide);
            return new Vector2(sideSign, 0f);
        }

        private static float ReadSideSign(BoneSenderTestInputHandSide handSide)
        {
            return handSide == BoneSenderTestInputHandSide.Left ? -1f : 1f;
        }

        private bool TryReadHeadTopY(BoneProtocolPerson person, out float headTopY)
        {
            headTopY = 0f;
            bool hasHeadPoint = false;
            Vector2 point;
            if (TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Lefteye, out point))
            {
                headTopY = point.y;
                hasHeadPoint = true;
            }

            if (TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Righteye, out point))
            {
                headTopY = hasHeadPoint ? Mathf.Min(headTopY, point.y) : point.y;
                hasHeadPoint = true;
            }

            if (TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Nose, out point))
            {
                headTopY = hasHeadPoint ? Mathf.Min(headTopY, point.y) : point.y;
                hasHeadPoint = true;
            }

            if (TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Leftear, out point))
            {
                headTopY = hasHeadPoint ? Mathf.Min(headTopY, point.y) : point.y;
                hasHeadPoint = true;
            }

            if (TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Rightear, out point))
            {
                headTopY = hasHeadPoint ? Mathf.Min(headTopY, point.y) : point.y;
                hasHeadPoint = true;
            }

            return hasHeadPoint;
        }

        private bool TryReadShoulderWidth(BoneProtocolPerson person, out float shoulderWidth)
        {
            Vector2 leftShoulder;
            Vector2 rightShoulder;
            if (TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Leftshoulder, out leftShoulder) &&
                TryReadBodyJoint(person, (int)YouDooSDKConstants.KeyPointIndex.Rightshoulder, out rightShoulder))
            {
                shoulderWidth = Mathf.Max(0.001f, Vector2.Distance(leftShoulder, rightShoulder));
                return true;
            }

            if (person != null && person.m_Body != null && person.m_Body.m_Rect != null && person.m_Body.m_Rect.m_IsTracked)
            {
                shoulderWidth = Mathf.Max(0.001f, (person.m_Body.m_Rect.m_Right - person.m_Body.m_Rect.m_Left) * 0.35f);
                return true;
            }

            shoulderWidth = 0f;
            return false;
        }

        private bool TryReadHandWristBySide(
            BoneProtocolPerson person,
            BoneSenderTestInputHandSide handSide,
            out Vector2 wrist,
            out float wristZ)
        {
            int bodyWristIndex = handSide == BoneSenderTestInputHandSide.Left
                ? (int)YouDooSDKConstants.KeyPointIndex.Leftwrist
                : (int)YouDooSDKConstants.KeyPointIndex.Rightwrist;
            Vector2 bodyWrist;
            if (TryReadBodyJoint(person, bodyWristIndex, out bodyWrist, out wristZ))
            {
                wrist = bodyWrist;
                return true;
            }

            BoneProtocolPart handPart = handSide == BoneSenderTestInputHandSide.Left ? person.m_LeftHand : person.m_RightHand;
            if (TryReadPartJoint(handPart, (int)YouDooSDKConstants.HandLandmark21.HAND_WRIST, 0.0f, out wrist, out wristZ))
            {
                return true;
            }

            wrist = Vector2.zero;
            wristZ = 0f;
            return false;
        }

        private bool TryReadBodyJoint(BoneProtocolPerson person, int jointIndex, out Vector2 point)
        {
            float z;
            return TryReadBodyJoint(person, jointIndex, out point, out z);
        }

        private bool TryReadBodyJoint(BoneProtocolPerson person, int jointIndex, out Vector2 point, out float z)
        {
            return TryReadBodyJoint(person, jointIndex, m_Config.m_MinJointScore, out point, out z);
        }

        private static bool TryReadBodyJoint(BoneProtocolPerson person, int jointIndex, float minScore, out Vector2 point, out float z)
        {
            if (person == null)
            {
                point = Vector2.zero;
                z = 0f;
                return false;
            }

            return TryReadPartJoint(person.m_Body, jointIndex, minScore, out point, out z);
        }

        private static bool TryReadPartJoint(BoneProtocolPart part, int jointIndex, float minScore, out Vector2 point, out float z)
        {
            if (part == null || part.m_Joints == null || jointIndex < 0 || jointIndex >= part.m_Joints.Length)
            {
                point = Vector2.zero;
                z = 0f;
                return false;
            }

            BoneProtocolJoint joint = part.m_Joints[jointIndex];
            if (joint == null || !joint.m_IsTracked || joint.m_Score < minScore)
            {
                point = Vector2.zero;
                z = 0f;
                return false;
            }

            point = new Vector2(joint.m_X, joint.m_Y);
            z = joint.m_Z;
            return true;
        }

        private static string ReadHandStateText(BoneSenderTestInputHandState handState)
        {
            if (!handState.m_HasStableReference)
            {
                return "未建稳定点";
            }

            if (handState.m_IsStable)
            {
                return "稳定";
            }

            return "离开稳定区";
        }

        private static string ReadGestureText(BoneSenderTestInputHandState handState)
        {
            switch (handState.m_GesturePhase)
            {
                case BoneSenderTestInputGesturePhase.OutwardExtended:
                    return handState.m_IsGestureConsumed ? "外挥已消费" : "外挥起手";
                case BoneSenderTestInputGesturePhase.FoldRetracted:
                    return handState.m_IsGestureConsumed ? "收拳已消费" : "收拳起手";
                default:
                    return handState.m_IsGestureConsumed ? "等待重装填" : "无";
            }
        }
    }
}
