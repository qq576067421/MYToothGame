using System;
using UnityEngine;

namespace BoneSender
{
    public sealed class SimulatedBoneFrameGenerator
    {
        private const int m_FixedSlotCount = 4;
        private const int m_ImageWidth = 1280;
        private const int m_ImageHeight = 720;
        private const int m_BodyJointCount = (int)YouDooSDKConstants.KeyPointIndex.KEYPOINT_COUNT;
        private const int m_HandJointCount = (int)YouDooSDKConstants.HandLandmark21.HAND_LANDMARK_COUNT;
        private const int m_FaceJointCount = (int)YouDooSDKConstants.FaceLandmark5.FACE_LANDMARK_COUNT;
        private const float m_CycleDurationSeconds = 4.65f;

        private readonly BoneFrameAssembler m_FrameAssembler = new BoneFrameAssembler();

        public BoneProtocolFrame BuildFrame(string sessionId, int frameSerial, float elapsedSeconds)
        {
            var pose = EvaluatePose(elapsedSeconds, frameSerial);

            m_FrameAssembler.BeginFrame(
                sessionId,
                frameSerial,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                m_ImageWidth,
                m_ImageHeight);

            var person = m_FrameAssembler.AddPerson(1);
            person.m_Body = CreateBodyPart(pose);
            person.m_LeftHand = CreateHandPart(pose.m_LeftWrist, false);
            person.m_RightHand = CreateHandPart(pose.m_RightWrist, true);
            person.m_Face = CreateFacePart(pose);
            for (int slotIndex = 1; slotIndex < m_FixedSlotCount; slotIndex++)
            {
                m_FrameAssembler.AddPerson(YouDooSDKConstants.PersonIdNull);
            }

            var frame = m_FrameAssembler.EndFrame();
            if (frame != null)
            {
                frame.m_IsSimulated = true;
            }

            return frame;
        }

        private BoneProtocolPart CreateBodyPart(PoseData pose)
        {
            var part = CreateTrackedPart(m_BodyJointCount, (int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON);

            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Nose, pose.m_Nose);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Lefteye, pose.m_LeftEye);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Righteye, pose.m_RightEye);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Leftear, pose.m_LeftEar);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Rightear, pose.m_RightEar);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Leftshoulder, pose.m_LeftShoulder);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Rightshoulder, pose.m_RightShoulder);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Leftelbow, pose.m_LeftElbow);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Rightelbow, pose.m_RightElbow);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Leftwrist, pose.m_LeftWrist);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Rightwrist, pose.m_RightWrist);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Lefthip, pose.m_LeftHip);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Righthip, pose.m_RightHip);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Leftknee, pose.m_LeftKnee);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Rightknee, pose.m_RightKnee);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Leftankle, pose.m_LeftAnkle);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Rightankle, pose.m_RightAnkle);
            SetJoint(part, (int)YouDooSDKConstants.KeyPointIndex.Chest, pose.m_Chest);

            FinalizePartRect(part);
            return part;
        }

        private BoneProtocolPart CreateHandPart(Vector3 wrist, bool isRightHand)
        {
            var part = CreateTrackedPart(
                m_HandJointCount,
                isRightHand
                    ? (int)YouDooSDKConstants.DetectType.DETECT_TYPE_RIGHT_HAND
                    : (int)YouDooSDKConstants.DetectType.DETECT_TYPE_LEFT_HAND);

            float sideSign = isRightHand ? 1f : -1f;
            float fingerSpread = 0.014f * sideSign;
            float fingerLength = 0.022f;

            SetJoint(part, (int)YouDooSDKConstants.HandLandmark21.HAND_WRIST, wrist);
            WriteFinger(
                part,
                (int)YouDooSDKConstants.HandLandmark21.HAND_THUMB_CMC,
                wrist,
                new Vector3(0.014f * sideSign, -0.004f, 0.005f),
                new Vector3(0.010f * sideSign, -0.010f, 0.004f));
            WriteFinger(
                part,
                (int)YouDooSDKConstants.HandLandmark21.HAND_INDEX_FINGER_MCP,
                wrist,
                new Vector3(0.010f * sideSign, -0.006f, 0.002f),
                new Vector3(fingerSpread, -fingerLength, 0.003f));
            WriteFinger(
                part,
                (int)YouDooSDKConstants.HandLandmark21.HAND_MIDDLE_FINGER_MCP,
                wrist,
                new Vector3(0.002f * sideSign, -0.008f, 0.002f),
                new Vector3(0f, -fingerLength * 1.1f, 0.003f));
            WriteFinger(
                part,
                (int)YouDooSDKConstants.HandLandmark21.HAND_RING_FINGER_MCP,
                wrist,
                new Vector3(-0.006f * sideSign, -0.007f, 0.002f),
                new Vector3(-fingerSpread * 0.75f, -fingerLength, 0.003f));
            WriteFinger(
                part,
                (int)YouDooSDKConstants.HandLandmark21.HAND_PINKY_FINGER_MCP,
                wrist,
                new Vector3(-0.013f * sideSign, -0.005f, 0.002f),
                new Vector3(-fingerSpread, -fingerLength * 0.85f, 0.003f));

            FinalizePartRect(part);
            return part;
        }

        private BoneProtocolPart CreateFacePart(PoseData pose)
        {
            var part = CreateTrackedPart(m_FaceJointCount, (int)YouDooSDKConstants.DetectType.DETECT_TYPE_FACE);

            SetJoint(part, (int)YouDooSDKConstants.FaceLandmark5.LEFT_EYE, pose.m_LeftEye);
            SetJoint(part, (int)YouDooSDKConstants.FaceLandmark5.RIGHT_EYE, pose.m_RightEye);
            SetJoint(part, (int)YouDooSDKConstants.FaceLandmark5.NOSE_TIP, pose.m_Nose);
            SetJoint(part, (int)YouDooSDKConstants.FaceLandmark5.LEFT_MOUTH, pose.m_LeftMouth);
            SetJoint(part, (int)YouDooSDKConstants.FaceLandmark5.RIGHT_MOUTH, pose.m_RightMouth);

            FinalizePartRect(part);
            return part;
        }

        private BoneProtocolPart CreateTrackedPart(int jointCount, int type)
        {
            var part = m_FrameAssembler.CreatePart(jointCount);
            part.m_Score = 1f;
            part.m_Type = type;
            part.m_Rect.m_IsTracked = true;
            return part;
        }

        private static void SetJoint(BoneProtocolPart part, int jointIndex, Vector3 jointPosition)
        {
            if (part == null || part.m_Joints == null || jointIndex < 0 || jointIndex >= part.m_Joints.Length)
            {
                return;
            }

            var joint = part.m_Joints[jointIndex];
            joint.m_IsTracked = true;
            joint.m_X = jointPosition.x;
            joint.m_Y = jointPosition.y;
            joint.m_Z = jointPosition.z;
            joint.m_Score = 1f;
        }

        private static void WriteFinger(
            BoneProtocolPart part,
            int startJointIndex,
            Vector3 wrist,
            Vector3 baseOffset,
            Vector3 segmentOffset)
        {
            for (int i = 0; i < 4; i++)
            {
                SetJoint(
                    part,
                    startJointIndex + i,
                    wrist + baseOffset + segmentOffset * (i + 1));
            }
        }

        private static void FinalizePartRect(BoneProtocolPart part)
        {
            if (part == null || part.m_Joints == null)
            {
                return;
            }

            bool hasTrackedJoint = false;
            float minX = 1f;
            float minY = 1f;
            float maxX = 0f;
            float maxY = 0f;

            for (int i = 0; i < part.m_Joints.Length; i++)
            {
                var joint = part.m_Joints[i];
                if (joint == null || !joint.m_IsTracked)
                {
                    continue;
                }

                hasTrackedJoint = true;
                minX = Mathf.Min(minX, joint.m_X);
                minY = Mathf.Min(minY, joint.m_Y);
                maxX = Mathf.Max(maxX, joint.m_X);
                maxY = Mathf.Max(maxY, joint.m_Y);
            }

            if (!hasTrackedJoint)
            {
                part.m_Rect.m_IsTracked = false;
                part.m_Rect.m_Left = 0f;
                part.m_Rect.m_Top = 0f;
                part.m_Rect.m_Right = 0f;
                part.m_Rect.m_Bottom = 0f;
                part.m_Score = 0f;
                return;
            }

            const float rectPadding = 0.018f;
            part.m_Rect.m_IsTracked = true;
            part.m_Rect.m_Left = Mathf.Clamp01(minX - rectPadding);
            part.m_Rect.m_Top = Mathf.Clamp01(minY - rectPadding);
            part.m_Rect.m_Right = Mathf.Clamp01(maxX + rectPadding);
            part.m_Rect.m_Bottom = Mathf.Clamp01(maxY + rectPadding);
        }

        private static PoseData EvaluatePose(float elapsedSeconds, int frameSerial)
        {
            float phaseTime = Mathf.Repeat(elapsedSeconds, m_CycleDurationSeconds);
            float torsoBob = Mathf.Sin(elapsedSeconds * 1.37f) * 0.006f;
            float lateralDrift = Mathf.Sin(elapsedSeconds * 0.73f) * 0.008f;
            float depthWave = Mathf.Cos(frameSerial * 0.41f) * 0.006f;
            float wristWave = Mathf.Sin(frameSerial * 0.29f) * 0.010f;

            Vector3 leftShoulder = new Vector3(0.42f + lateralDrift * 0.35f, 0.40f + torsoBob * 0.25f, 0.02f + depthWave * 0.25f);
            Vector3 rightShoulder = new Vector3(0.58f + lateralDrift * 0.35f, 0.40f + torsoBob * 0.25f, 0.02f - depthWave * 0.25f);
            Vector3 leftHip = new Vector3(0.46f + lateralDrift * 0.20f, 0.62f + torsoBob * 0.20f, 0.01f);
            Vector3 rightHip = new Vector3(0.54f + lateralDrift * 0.20f, 0.62f + torsoBob * 0.20f, 0.01f);
            Vector3 idleLeftWrist = new Vector3(0.32f + lateralDrift * 0.60f, 0.62f + torsoBob * 0.45f, 0.04f - depthWave * 0.30f);
            Vector3 idleRightWrist = new Vector3(0.68f + lateralDrift * 0.60f, 0.62f + torsoBob * 0.45f, 0.04f + depthWave * 0.30f);

            Vector3 leftWrist = idleLeftWrist;
            Vector3 rightWrist = idleRightWrist;

            if (phaseTime < 1.45f)
            {
                float sway = Mathf.Sin(phaseTime * 2.4f) * 0.012f;
                leftWrist += new Vector3(-sway, sway * 0.35f, 0f);
                rightWrist += new Vector3(sway, sway * 0.35f, 0f);
            }
            else if (phaseTime < 1.75f)
            {
                float t = Mathf.InverseLerp(1.45f, 1.75f, phaseTime);
                leftWrist = Vector3.Lerp(idleLeftWrist, new Vector3(0.35f, 0.56f, 0.04f), t);
                rightWrist = Vector3.Lerp(idleRightWrist, new Vector3(0.63f, 0.49f, 0.06f), t);
            }
            else if (phaseTime < 2.05f)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.75f, 2.05f, phaseTime));
                leftWrist = Vector3.Lerp(new Vector3(0.35f, 0.56f, 0.04f), new Vector3(0.31f, 0.59f, 0.03f), t);
                rightWrist = Vector3.Lerp(new Vector3(0.63f, 0.49f, 0.06f), new Vector3(0.84f, 0.34f, 0.18f), t);
            }
            else if (phaseTime < 2.45f)
            {
                float t = Mathf.InverseLerp(2.05f, 2.45f, phaseTime);
                leftWrist = Vector3.Lerp(new Vector3(0.31f, 0.59f, 0.03f), idleLeftWrist, t);
                rightWrist = Vector3.Lerp(new Vector3(0.84f, 0.34f, 0.18f), idleRightWrist, t);
            }
            else if (phaseTime < 3.95f)
            {
                float t = Mathf.InverseLerp(2.45f, 2.80f, phaseTime);
                leftWrist = Vector3.Lerp(idleLeftWrist, new Vector3(0.42f, 0.15f, 0.05f), Mathf.Clamp01(t));
                rightWrist = Vector3.Lerp(idleRightWrist, new Vector3(0.58f, 0.15f, 0.05f), Mathf.Clamp01(t));
            }
            else
            {
                float t = Mathf.InverseLerp(3.95f, m_CycleDurationSeconds, phaseTime);
                leftWrist = Vector3.Lerp(new Vector3(0.42f, 0.15f, 0.05f), idleLeftWrist, t);
                rightWrist = Vector3.Lerp(new Vector3(0.58f, 0.15f, 0.05f), idleRightWrist, t);
            }

            leftWrist += new Vector3(-wristWave * 0.35f, torsoBob * 0.30f, -depthWave * 0.25f);
            rightWrist += new Vector3(wristWave * 0.50f, torsoBob * 0.50f, depthWave * 0.65f);

            var pose = new PoseData();
            pose.m_Nose = new Vector3(0.50f + lateralDrift * 0.20f, 0.24f + torsoBob * 0.55f, 0.02f + depthWave * 0.20f);
            pose.m_LeftEye = new Vector3(0.47f + lateralDrift * 0.18f, 0.22f + torsoBob * 0.45f, 0.02f + depthWave * 0.15f);
            pose.m_RightEye = new Vector3(0.53f + lateralDrift * 0.18f, 0.22f + torsoBob * 0.45f, 0.02f - depthWave * 0.15f);
            pose.m_LeftEar = new Vector3(0.44f + lateralDrift * 0.15f, 0.24f + torsoBob * 0.45f, 0.02f + depthWave * 0.10f);
            pose.m_RightEar = new Vector3(0.56f + lateralDrift * 0.15f, 0.24f + torsoBob * 0.45f, 0.02f - depthWave * 0.10f);
            pose.m_LeftMouth = new Vector3(0.48f + lateralDrift * 0.18f, 0.29f + torsoBob * 0.35f, 0.02f + depthWave * 0.10f);
            pose.m_RightMouth = new Vector3(0.52f + lateralDrift * 0.18f, 0.29f + torsoBob * 0.35f, 0.02f - depthWave * 0.10f);
            pose.m_Chest = new Vector3(0.50f + lateralDrift * 0.28f, 0.49f + torsoBob * 0.60f, 0.02f);
            pose.m_LeftShoulder = leftShoulder;
            pose.m_RightShoulder = rightShoulder;
            pose.m_LeftWrist = leftWrist;
            pose.m_RightWrist = rightWrist;
            pose.m_LeftElbow = ResolveElbow(leftShoulder, leftWrist, -1f);
            pose.m_RightElbow = ResolveElbow(rightShoulder, rightWrist, 1f);
            pose.m_LeftHip = leftHip;
            pose.m_RightHip = rightHip;
            pose.m_LeftKnee = new Vector3(0.47f + lateralDrift * 0.10f, 0.78f + torsoBob * 0.15f, 0.01f);
            pose.m_RightKnee = new Vector3(0.53f + lateralDrift * 0.10f, 0.78f + torsoBob * 0.15f, 0.01f);
            pose.m_LeftAnkle = new Vector3(0.48f + lateralDrift * 0.08f, 0.94f + torsoBob * 0.10f, 0.01f);
            pose.m_RightAnkle = new Vector3(0.52f + lateralDrift * 0.08f, 0.94f + torsoBob * 0.10f, 0.01f);
            return pose;
        }

        private static Vector3 ResolveElbow(Vector3 shoulder, Vector3 wrist, float sideSign)
        {
            var elbow = Vector3.Lerp(shoulder, wrist, 0.52f);
            elbow.x += 0.028f * sideSign;
            elbow.y += wrist.y < shoulder.y ? 0.028f : -0.010f;
            elbow.z = Mathf.Lerp(shoulder.z, wrist.z, 0.4f);
            return elbow;
        }

        private sealed class PoseData
        {
            public Vector3 m_Nose;
            public Vector3 m_LeftEye;
            public Vector3 m_RightEye;
            public Vector3 m_LeftEar;
            public Vector3 m_RightEar;
            public Vector3 m_LeftMouth;
            public Vector3 m_RightMouth;
            public Vector3 m_Chest;
            public Vector3 m_LeftShoulder;
            public Vector3 m_RightShoulder;
            public Vector3 m_LeftElbow;
            public Vector3 m_RightElbow;
            public Vector3 m_LeftWrist;
            public Vector3 m_RightWrist;
            public Vector3 m_LeftHip;
            public Vector3 m_RightHip;
            public Vector3 m_LeftKnee;
            public Vector3 m_RightKnee;
            public Vector3 m_LeftAnkle;
            public Vector3 m_RightAnkle;
        }
    }
}
