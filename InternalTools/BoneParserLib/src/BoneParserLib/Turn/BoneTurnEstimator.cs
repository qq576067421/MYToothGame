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
            if (seatState == null || result == null)
            {
                return;
            }

            if (TryReadMeasuredAngleFromShoulderAngle(
                person, seatState, out float measuredAngleDegrees, out float confidence))
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

            // 双肩被手腕短暂遮挡时保持最后有效角度，恢复后继续从该角度平滑跟随。
            seatState.m_LastAimAngularSpeedDegrees = 0f;
            seatState.m_ShoulderAngleTurnSmoothVelocityDegrees = 0f;
            ApplyAimResult(
                seatState,
                result,
                BoneAimTrackingState.短暂丢失,
                true,
                seatState.m_LastAimAngleDegrees,
                0f,
                ReadTurnUpdateDeltaTime(deltaTimeSeconds));
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
            float outputDeltaTimeSeconds = ReadTurnUpdateDeltaTime(deltaTimeSeconds);
            float previousAngle = seatState.m_HasAimOutput ? seatState.m_LastAimAngleDegrees : measuredAngleDegrees;
            float outputAngle = measuredAngleDegrees;
            if (seatState.m_HasAimOutput)
            {
                outputAngle = SmoothDamp(
                    previousAngle,
                    measuredAngleDegrees,
                    ref seatState.m_ShoulderAngleTurnSmoothVelocityDegrees,
                    m_Config.m_ShoulderAngleTurnSmoothTimeSeconds,
                    m_Config.m_ShoulderAngleTurnMaxSpeedDegreesPerSecond,
                    outputDeltaTimeSeconds);
            }
            else
            {
                seatState.m_ShoulderAngleTurnSmoothVelocityDegrees = 0f;
            }

            ApplyAimResult(
                seatState,
                result,
                BoneAimTrackingState.正常跟踪,
                true,
                outputAngle,
                confidence,
                outputDeltaTimeSeconds);
            seatState.m_AimMissingFrameCount = 0;
        }

        private bool TryReadMeasuredAngleFromShoulderAngle(
            BoneTrackedPerson person,
            BoneParserSeatState seatState,
            out float angleDegrees,
            out float confidence)
        {
            angleDegrees = 0f;
            confidence = 0f;
            if (person == null ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左肩, m_Config.m_KeypointConfidenceThreshold, out BoneVector2 leftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右肩, m_Config.m_KeypointConfidenceThreshold, out BoneVector2 rightShoulder))
            {
                return false;
            }

            float shoulderDeltaX = rightShoulder.m_X - leftShoulder.m_X;
            float shoulderDeltaY = rightShoulder.m_Y - leftShoulder.m_Y;
            float shoulderSpan = BoneMath.Sqrt(
                shoulderDeltaX * shoulderDeltaX + shoulderDeltaY * shoulderDeltaY);
            if (shoulderSpan <= 0.0001f || float.IsNaN(shoulderSpan) || float.IsInfinity(shoulderSpan))
            {
                return false;
            }

            if (BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左髋, m_Config.m_KeypointConfidenceThreshold, out BoneVector2 leftHip) &&
                BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右髋, m_Config.m_KeypointConfidenceThreshold, out BoneVector2 rightHip))
            {
                float averageTorsoLength = (BoneMath.Distance(leftShoulder, leftHip) +
                    BoneMath.Distance(rightShoulder, rightHip)) * 0.5f;
                float minShoulderSpanRatio = BoneMath.Clamp(
                    m_Config.m_ShoulderAngleTurnMinShoulderSpanToTorsoRatio, 0f, 2f);
                if (averageTorsoLength > 0.0001f && shoulderSpan / averageTorsoLength < minShoulderSpanRatio)
                {
                    return false;
                }
            }

            const float radiansToDegrees = 180f / (float)Math.PI;
            float rawShoulderAngleDegrees = (float)Math.Atan2(
                shoulderDeltaY, BoneMath.Abs(shoulderDeltaX)) * radiansToDegrees;
            if (float.IsNaN(rawShoulderAngleDegrees) || float.IsInfinity(rawShoulderAngleDegrees))
            {
                return false;
            }
            if (m_Config.m_ShoulderAngleTurnInvertDirection)
            {
                rawShoulderAngleDegrees = -rawShoulderAngleDegrees;
            }

            float inputJitterDegrees = BoneMath.Clamp(
                m_Config.m_ShoulderAngleTurnInputJitterDegrees, 0f, 15f);
            if (!seatState.m_HasFilteredShoulderAngle)
            {
                seatState.m_HasFilteredShoulderAngle = true;
                seatState.m_FilteredShoulderAngleDegrees = rawShoulderAngleDegrees;
            }
            else if (rawShoulderAngleDegrees > seatState.m_FilteredShoulderAngleDegrees + inputJitterDegrees)
            {
                seatState.m_FilteredShoulderAngleDegrees = rawShoulderAngleDegrees - inputJitterDegrees;
            }
            else if (rawShoulderAngleDegrees < seatState.m_FilteredShoulderAngleDegrees - inputJitterDegrees)
            {
                seatState.m_FilteredShoulderAngleDegrees = rawShoulderAngleDegrees + inputJitterDegrees;
            }

            float filteredAngleDegrees = seatState.m_FilteredShoulderAngleDegrees;
            float magnitude = BoneMath.Abs(filteredAngleDegrees);
            float fullInputDegrees = BoneMath.Clamp(
                m_Config.m_ShoulderAngleTurnFullInputDegrees, 0.0001f, 90f);
            confidence = BoneMath.Clamp01(person.m_Body.m_Score);
            float normalizedMagnitude = BoneMath.Clamp01(magnitude / fullInputDegrees);
            angleDegrees = BoneMath.Sign(filteredAngleDegrees) * normalizedMagnitude * ReadClampedMaxAngle();
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
            result.m_TurnStrength = BoneMath.Clamp01(BoneMath.Abs(currentAngle) / Math.Max(maxAngle, 0.0001f));
            result.m_TurnSpeed = BoneMath.Abs(currentAngle - previousAngle) / safeDeltaTimeSeconds;
        }

        private static void ApplyUnavailableResult(BoneParserSeatState seatState, BoneParserPlayerResult result)
        {
            seatState.m_AimTrackingState = BoneAimTrackingState.未跟踪;
            seatState.m_HasAimOutput = false;
            seatState.m_LastAimAngleDegrees = 0f;
            seatState.m_LastAimAngularSpeedDegrees = 0f;
            seatState.m_CachedRotationOffset = 0f;
            seatState.ResetShoulderAngleTurnState();

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
            return BoneMath.Clamp(m_Config.m_ShoulderAngleTurnMaxOutputAngleDegrees, 0f, 45f);
        }

        private float ReadTurnUpdateDeltaTime(float fallbackDeltaTimeSeconds)
        {
            float deltaTimeSeconds = m_Config.m_TurnUpdateDeltaTimeSeconds > 0f
                ? m_Config.m_TurnUpdateDeltaTimeSeconds
                : fallbackDeltaTimeSeconds;
            return BoneMath.Clamp(deltaTimeSeconds, 1f / 240f, 0.1f);
        }

        private static float SmoothDamp(
            float current,
            float target,
            ref float currentVelocity,
            float smoothTimeSeconds,
            float maxSpeedDegreesPerSecond,
            float deltaTimeSeconds)
        {
            float smoothTime = Math.Max(0.0001f, smoothTimeSeconds);
            float deltaTime = BoneMath.Clamp(deltaTimeSeconds, 1f / 240f, 0.1f);
            float angularFrequency = 2f / smoothTime;
            float x = angularFrequency * deltaTime;
            float decay = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float originalTarget = target;
            float maxChange = Math.Max(0f, maxSpeedDegreesPerSecond) * smoothTime;
            change = BoneMath.Clamp(change, -maxChange, maxChange);
            target = current - change;
            float temporaryVelocity = (currentVelocity + angularFrequency * change) * deltaTime;
            currentVelocity = (currentVelocity - angularFrequency * temporaryVelocity) * decay;
            float output = target + (change + temporaryVelocity) * decay;
            if ((originalTarget - current > 0f) == (output > originalTarget))
            {
                output = originalTarget;
                currentVelocity = 0f;
            }

            return output;
        }

        private static BoneVector3 BuildFaceForward(float angleDegrees)
        {
            float radians = angleDegrees * ((float)Math.PI / 180f);
            return BoneMath.NormalizeOrDefault(
                new BoneVector3(BoneMath.Sin(radians), 0f, BoneMath.Cos(radians)),
                BoneVector3.m_Forward);
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
