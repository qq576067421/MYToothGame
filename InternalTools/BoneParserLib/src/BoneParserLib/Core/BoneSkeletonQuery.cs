namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class BoneSkeletonQuery
    {
        private readonly BoneParserConfig m_Config;

        public BoneSkeletonQuery(BoneParserConfig config)
        {
            m_Config = config;
        }

        public bool ReadIsBindablePerson(BoneTrackedPerson person)
        {
            if (person == null)
            {
                return false;
            }

            if (person.m_Body.m_Score < m_Config.m_MinBodyScore)
            {
                return false;
            }

            if (person.m_Body.m_Rect.m_IsValid)
            {
                return true;
            }

            return TryReadBodyJoint(person, BoneBodyJointType.左肩, out _) &&
                TryReadBodyJoint(person, BoneBodyJointType.右肩, out _);
        }

        public float ReadShoulderWidth(BoneTrackedPerson person)
        {
            if (!TryReadBodyJoint(person, BoneBodyJointType.左肩, out BoneVector2 leftShoulder) ||
                !TryReadBodyJoint(person, BoneBodyJointType.右肩, out BoneVector2 rightShoulder))
            {
                return 0f;
            }

            return BoneMath.Abs(rightShoulder.m_X - leftShoulder.m_X) + m_Config.m_ShoulderWidthEpsilon;
        }

        public bool TryReadBodyJoint(BoneTrackedPerson person, BoneBodyJointType jointType, out BoneVector2 point)
        {
            return TryReadBodyJoint(person, jointType, m_Config.m_MinJointScore, out point);
        }

        public static bool TryReadBodyJoint(BoneTrackedPerson person, BoneBodyJointType jointType, float minScore, out BoneVector2 point)
        {
            point = BoneVector2.m_Zero;
            if (person == null)
            {
                return false;
            }

            int jointIndex = (int)jointType;
            if (jointIndex < 0 || jointIndex >= person.m_Body.m_Joints.Length)
            {
                return false;
            }

            BoneTrackedJoint joint = person.m_Body.m_Joints[jointIndex];
            if (joint == null || !joint.m_IsTracked || joint.m_Score < minScore)
            {
                return false;
            }

            point = new BoneVector2(joint.m_X, joint.m_Y);
            return true;
        }

        public bool TryReadHeadTop(BoneTrackedPerson person, out float headTopY)
        {
            headTopY = 0f;
            bool hasHeadPoint = false;
            if (TryReadBodyJoint(person, BoneBodyJointType.鼻尖, m_Config.m_GestureKeypointMinConfidence, out BoneVector2 point))
            {
                headTopY = point.m_Y;
                hasHeadPoint = true;
            }

            if (TryReadBodyJoint(person, BoneBodyJointType.左眼, m_Config.m_GestureKeypointMinConfidence, out point))
            {
                headTopY = hasHeadPoint ? BoneMath.Min(headTopY, point.m_Y) : point.m_Y;
                hasHeadPoint = true;
            }

            if (TryReadBodyJoint(person, BoneBodyJointType.右眼, m_Config.m_GestureKeypointMinConfidence, out point))
            {
                headTopY = hasHeadPoint ? BoneMath.Min(headTopY, point.m_Y) : point.m_Y;
                hasHeadPoint = true;
            }

            return hasHeadPoint;
        }

        public bool TryReadHipCenter(BoneTrackedPerson person, out BoneVector2 hipCenter)
        {
            if (TryReadBodyJoint(person, BoneBodyJointType.左髋, out BoneVector2 leftHip) &&
                TryReadBodyJoint(person, BoneBodyJointType.右髋, out BoneVector2 rightHip))
            {
                hipCenter = (leftHip + rightHip) * 0.5f;
                return true;
            }

            hipCenter = BoneVector2.m_Zero;
            return false;
        }

        public bool TryReadShoulderCenter(BoneTrackedPerson person, out BoneVector2 shoulderCenter)
        {
            if (TryReadBodyJoint(person, BoneBodyJointType.左肩, out BoneVector2 leftShoulder) &&
                TryReadBodyJoint(person, BoneBodyJointType.右肩, out BoneVector2 rightShoulder))
            {
                shoulderCenter = (leftShoulder + rightShoulder) * 0.5f;
                return true;
            }

            shoulderCenter = BoneVector2.m_Zero;
            return false;
        }

        public bool TryReadUpperBodyCenter(BoneTrackedPerson person, out BoneVector2 upperBodyCenter)
        {
            if (!TryReadShoulderCenter(person, out BoneVector2 shoulderCenter))
            {
                upperBodyCenter = BoneVector2.m_Zero;
                return false;
            }

            if (TryReadBodyJoint(person, BoneBodyJointType.胸口, out BoneVector2 chest))
            {
                upperBodyCenter = (shoulderCenter + chest) * 0.5f;
                return true;
            }

            upperBodyCenter = shoulderCenter;
            return true;
        }
    }
}
