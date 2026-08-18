using System;

namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class BoneTurnEstimator
    {
        private readonly BoneParserConfig m_Config;

        public BoneTurnEstimator(BoneParserConfig config)
        {
            m_Config = config;
        }

        public void UpdateTracked(
            BoneTrackedPerson person,
            BoneParserSeatState seatState,
            BoneParserPlayerResult result,
            float deltaTimeSeconds)
        {
            if (TryReadMeasuredAngle(person, seatState, out float measuredAngleDegrees, out float confidence))
            {
                ApplyTrackedAngle(seatState, result, measuredAngleDegrees, confidence, deltaTimeSeconds);
                return;
            }

            UpdateMissing(seatState, result, deltaTimeSeconds);
        }

        public void UpdateMissing(
            BoneParserSeatState seatState,
            BoneParserPlayerResult result,
            float deltaTimeSeconds)
        {
            if (seatState == null || result == null)
            {
                return;
            }

            seatState.m_AimMissingFrameCount++;
            result.m_MissingFrameCount = seatState.m_AimMissingFrameCount;

            if (!seatState.m_HasAimOutput)
            {
                ApplyUnavailableResult(seatState, result);
                return;
            }

            int predictFrames = Math.Max(0, m_Config.m_AimPredictMissingFrames);
            int holdFrames = Math.Max(predictFrames, m_Config.m_AimHoldMissingFrames);
            if (seatState.m_AimMissingFrameCount <= predictFrames)
            {
                float maxAngle = ReadClampedMaxAngle();
                float predictedAngle = BoneMath.Clamp(
                    seatState.m_LastAimAngleDegrees + seatState.m_LastAimAngularSpeedDegrees * deltaTimeSeconds,
                    -maxAngle,
                    maxAngle);
                seatState.m_LastAimAngularSpeedDegrees *= BoneMath.Clamp01(m_Config.m_AimPredictVelocityDamping);
                ApplyAimResult(
                    seatState,
                    result,
                    BoneAimTrackingState.短暂丢失,
                    true,
                    predictedAngle,
                    BoneMath.Clamp01(1f - (float)seatState.m_AimMissingFrameCount / Math.Max(1, holdFrames)),
                    deltaTimeSeconds);
                return;
            }

            if (seatState.m_AimMissingFrameCount <= holdFrames)
            {
                seatState.m_LastAimAngularSpeedDegrees *= BoneMath.Clamp01(m_Config.m_AimPredictVelocityDamping);
                ApplyAimResult(
                    seatState,
                    result,
                    BoneAimTrackingState.短暂丢失,
                    true,
                    seatState.m_LastAimAngleDegrees,
                    BoneMath.Clamp01(1f - (float)seatState.m_AimMissingFrameCount / Math.Max(1, holdFrames)),
                    deltaTimeSeconds);
                return;
            }

            float returnSeconds = Math.Max(0.01f, m_Config.m_AimReturnToForwardSeconds);
            float blend = BoneMath.Clamp01(deltaTimeSeconds / returnSeconds);
            float returnAngle = BoneMath.Lerp(seatState.m_LastAimAngleDegrees, 0f, blend);
            bool isAimAvailable = BoneMath.Abs(returnAngle) > 0.1f;
            ApplyAimResult(
                seatState,
                result,
                isAimAvailable ? BoneAimTrackingState.平滑回正 : BoneAimTrackingState.未跟踪,
                isAimAvailable,
                returnAngle,
                0f,
                deltaTimeSeconds);

            if (!isAimAvailable)
            {
                seatState.m_AimTrackingState = BoneAimTrackingState.未跟踪;
                seatState.m_HasAimOutput = false;
                seatState.m_LastAimAngularSpeedDegrees = 0f;
                seatState.m_IsAimOutsideCenterDeadZone = false;
            }
        }

        public void NotifyPersonCandidate(BoneParserSeatState seatState, int personId)
        {
            if (seatState == null)
            {
                return;
            }

            if (seatState.m_CurrentPersonId == personId)
            {
                seatState.m_AimCandidatePersonId = BoneTrackedLayout.m_InvalidPersonId;
                seatState.m_AimCandidateStableFrameCount = 0;
                return;
            }

            if (seatState.m_AimCandidatePersonId == personId)
            {
                seatState.m_AimCandidateStableFrameCount++;
            }
            else
            {
                seatState.m_AimCandidatePersonId = personId;
                seatState.m_AimCandidateStableFrameCount = 1;
            }
        }

        public bool CanAcceptPerson(BoneParserSeatState seatState, int personId)
        {
            if (seatState == null)
            {
                return true;
            }

            if (seatState.m_CurrentPersonId == BoneTrackedLayout.m_InvalidPersonId ||
                seatState.m_CurrentPersonId == personId ||
                !seatState.m_HasAimOutput)
            {
                return true;
            }

            int stableFrames = Math.Max(1, m_Config.m_AimReconnectStableFrames);
            return seatState.m_AimCandidatePersonId == personId &&
                seatState.m_AimCandidateStableFrameCount >= stableFrames;
        }

        private void ApplyTrackedAngle(
            BoneParserSeatState seatState,
            BoneParserPlayerResult result,
            float measuredAngleDegrees,
            float confidence,
            float deltaTimeSeconds)
        {
            float previousAngle = seatState.m_HasAimOutput ? seatState.m_LastAimAngleDegrees : measuredAngleDegrees;
            float outputAngle = measuredAngleDegrees;
            if (seatState.m_HasAimOutput)
            {
                float blendSpeed = seatState.m_AimTrackingState == BoneAimTrackingState.正常跟踪
                    ? m_Config.m_RotationSmoothFactor
                    : m_Config.m_AimReconnectBlendFactor;
                outputAngle = BoneMath.Lerp(
                    previousAngle,
                    measuredAngleDegrees,
                    BoneMath.Clamp01(deltaTimeSeconds * Math.Max(0f, blendSpeed)));
            }

            ApplyAimResult(
                seatState,
                result,
                BoneAimTrackingState.正常跟踪,
                true,
                outputAngle,
                confidence,
                deltaTimeSeconds);
            seatState.m_AimMissingFrameCount = 0;
        }

        private bool TryReadMeasuredAngle(
            BoneTrackedPerson person,
            BoneParserSeatState seatState,
            out float angleDegrees,
            out float confidence)
        {
            angleDegrees = 0f;
            confidence = 0f;
            if (person == null ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左肩, m_Config.m_KeypointConfidenceThreshold, out BoneVector2 leftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右肩, m_Config.m_KeypointConfidenceThreshold, out BoneVector2 rightShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.鼻尖, m_Config.m_KeypointConfidenceThreshold, out BoneVector2 nose))
            {
                return false;
            }

            float shoulderMid = (leftShoulder.m_X + rightShoulder.m_X) * 0.5f;
            float shoulderWidth = BoneMath.Abs(rightShoulder.m_X - leftShoulder.m_X) + m_Config.m_ShoulderWidthEpsilon;
            if (BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左肩, m_Config.m_MaxShoulderWidthUpdateConfidence, out _) &&
                BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右肩, m_Config.m_MaxShoulderWidthUpdateConfidence, out _))
            {
                seatState.m_MaxObservedShoulderWidth = BoneMath.Max(seatState.m_MaxObservedShoulderWidth, shoulderWidth);
            }

            float noseOffset = (nose.m_X - shoulderMid) / shoulderWidth;
            float rawValue = BoneMath.Clamp(noseOffset * 2.5f, -1f, 1f);
            float maxShoulderWidth = seatState.m_MaxObservedShoulderWidth > m_Config.m_ShoulderWidthEpsilon
                ? seatState.m_MaxObservedShoulderWidth
                : shoulderWidth;
            float ratio = maxShoulderWidth > m_Config.m_ShoulderWidthEpsilon
                ? BoneMath.Clamp01(shoulderWidth / maxShoulderWidth)
                : 1f;
            float angleFactor = 1f - ratio;
            float shoulderValue = BoneMath.Clamp(angleFactor * 1.5f, 0f, 1f) * BoneMath.Sign(noseOffset);
            float targetOffset = BoneMath.Lerp(rawValue, shoulderValue, 0.3f);
            targetOffset = ApplyAimResponseCurve(targetOffset, seatState);
            if (m_Config.m_InvertTurnDirection)
            {
                targetOffset = -targetOffset;
            }

            angleDegrees = ConvertOffsetToAngle(
                targetOffset,
                ReadClampedMaxAngle(),
                BoneMath.Clamp(m_Config.m_RotationAmplifyFactor, 0f, 5f));
            confidence = BoneMath.Clamp01((leftShoulder.m_X != rightShoulder.m_X ? 1f : 0f) * person.m_Body.m_Score);
            return true;
        }

        private void ApplyAimResult(
            BoneParserSeatState seatState,
            BoneParserPlayerResult result,
            BoneAimTrackingState trackingState,
            bool isAimAvailable,
            float angleDegrees,
            float confidence,
            float deltaTimeSeconds)
        {
            float maxAngle = ReadClampedMaxAngle();
            float previousAngle = seatState.m_HasAimOutput ? seatState.m_LastAimAngleDegrees : angleDegrees;
            float currentAngle = BoneMath.Clamp(angleDegrees, -maxAngle, maxAngle);
            float safeDeltaTimeSeconds = BoneMath.Max(deltaTimeSeconds, 1f / 120f);

            seatState.m_AimTrackingState = trackingState;
            seatState.m_HasAimOutput = isAimAvailable;
            seatState.m_LastAimAngularSpeedDegrees = (currentAngle - previousAngle) / safeDeltaTimeSeconds;
            seatState.m_LastAimAngleDegrees = currentAngle;
            seatState.m_CachedRotationOffset = ConvertAngleToOffset(currentAngle, maxAngle);

            result.m_IsAimAvailable = isAimAvailable;
            result.m_AimTrackingState = trackingState;
            result.m_AimConfidence = confidence;
            result.m_MissingFrameCount = seatState.m_AimMissingFrameCount;
            result.m_FaceForward = BuildFaceForward(currentAngle);
            result.m_TurnAngleDegrees = currentAngle;
            result.m_TurnStrength = BoneMath.Clamp01(BoneMath.Abs(currentAngle) / Math.Max(maxAngle, m_Config.m_ShoulderWidthEpsilon));
            result.m_TurnSpeed = BoneMath.Abs(currentAngle - previousAngle) / safeDeltaTimeSeconds;
        }

        private static void ApplyUnavailableResult(BoneParserSeatState seatState, BoneParserPlayerResult result)
        {
            seatState.m_AimTrackingState = BoneAimTrackingState.未跟踪;
            seatState.m_HasAimOutput = false;
            seatState.m_LastAimAngleDegrees = 0f;
            seatState.m_LastAimAngularSpeedDegrees = 0f;
            seatState.m_CachedRotationOffset = 0f;
            seatState.m_IsAimOutsideCenterDeadZone = false;

            result.m_IsAimAvailable = false;
            result.m_AimTrackingState = BoneAimTrackingState.未跟踪;
            result.m_AimConfidence = 0f;
            result.m_FaceForward = BoneVector3.m_Forward;
            result.m_TurnAngleDegrees = 0f;
            result.m_TurnStrength = 0f;
            result.m_TurnSpeed = 0f;
        }

        private float ReadClampedMaxAngle()
        {
            return BoneMath.Clamp(m_Config.m_MaxTurnAngleDegrees, 0f, 45f);
        }

        private static BoneVector3 BuildFaceForward(float angleDegrees)
        {
            float radians = angleDegrees * ((float)Math.PI / 180f);
            return BoneMath.NormalizeOrDefault(
                new BoneVector3(BoneMath.Sin(radians), 0f, BoneMath.Cos(radians)),
                BoneVector3.m_Forward);
        }

        private static float ConvertOffsetToAngle(float normalizedOffset, float maxAngle, float rotationAmplifyFactor)
        {
            return BoneMath.Clamp(-normalizedOffset * rotationAmplifyFactor, -1f, 1f) * maxAngle;
        }

        private float ApplyAimResponseCurve(float targetOffset, BoneParserSeatState seatState)
        {
            float enterRatio = BoneMath.Clamp01(m_Config.m_AimCenterEnterRatio);
            float exitRatio = BoneMath.Clamp(m_Config.m_AimCenterExitRatio, 0f, enterRatio);
            float magnitude = BoneMath.Abs(targetOffset);
            if (seatState.m_IsAimOutsideCenterDeadZone)
            {
                if (magnitude <= exitRatio)
                {
                    seatState.m_IsAimOutsideCenterDeadZone = false;
                    return 0f;
                }
            }
            else
            {
                if (magnitude < enterRatio)
                {
                    return 0f;
                }

                seatState.m_IsAimOutsideCenterDeadZone = true;
            }

            float availableRange = BoneMath.Max(1f - exitRatio, m_Config.m_ShoulderWidthEpsilon);
            float normalizedMagnitude = BoneMath.Clamp01((magnitude - exitRatio) / availableRange);
            float exponent = BoneMath.Max(1f, m_Config.m_AimResponseCurveExponent);
            float curvedMagnitude = (float)Math.Pow(normalizedMagnitude, exponent);
            return curvedMagnitude * BoneMath.Sign(targetOffset);
        }

        private static float ConvertAngleToOffset(float angleDegrees, float maxAngle)
        {
            if (maxAngle <= 0.0001f)
            {
                return 0f;
            }

            return BoneMath.Clamp(-angleDegrees / maxAngle, -1f, 1f);
        }
    }
}
