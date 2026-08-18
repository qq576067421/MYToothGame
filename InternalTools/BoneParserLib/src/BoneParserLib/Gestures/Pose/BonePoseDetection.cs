namespace CompanyInternalTools.BoneParserLib
{
    internal static class BonePoseDetection
    {
        public static bool ReadIsCrouching(BoneGestureRuntimeContext context)
        {
            BoneTrackedPerson person = context.m_Person;
            if (person == null || !person.m_Body.m_Rect.m_IsValid)
            {
                return false;
            }

            if (!context.m_Skeleton.TryReadShoulderCenter(person, out BoneVector2 shoulderCenter) ||
                !context.m_Skeleton.TryReadHipCenter(person, out BoneVector2 hipCenter))
            {
                return false;
            }

            float bodyHeight = person.m_Body.m_Rect.ReadHeight();
            if (bodyHeight <= 0.0001f)
            {
                return false;
            }

            float torsoHeight = BoneMath.Abs(hipCenter.m_Y - shoulderCenter.m_Y);
            return torsoHeight / bodyHeight <= context.m_Config.m_PoseCrouchTorsoRatio;
        }

        public static bool ReadIsCrossChest(BoneGestureRuntimeContext context)
        {
            BoneTrackedPerson person = context.m_Person;
            if (!context.m_Skeleton.TryReadBodyJoint(person, BoneBodyJointType.左手腕, out BoneVector2 leftWrist) ||
                !context.m_Skeleton.TryReadBodyJoint(person, BoneBodyJointType.右手腕, out BoneVector2 rightWrist) ||
                !context.m_Skeleton.TryReadShoulderCenter(person, out BoneVector2 shoulderCenter) ||
                !context.m_Skeleton.TryReadUpperBodyCenter(person, out BoneVector2 upperBodyCenter))
            {
                return false;
            }

            float shoulderWidth = context.m_Skeleton.ReadShoulderWidth(person);
            if (shoulderWidth <= context.m_Config.m_ShoulderWidthEpsilon)
            {
                return false;
            }

            bool handsSwapped = leftWrist.m_X > shoulderCenter.m_X && rightWrist.m_X < shoulderCenter.m_X;
            bool leftNearCenter = BoneMath.Abs(leftWrist.m_X - shoulderCenter.m_X) <= shoulderWidth * context.m_Config.m_PoseCrossChestCenterRatio;
            bool rightNearCenter = BoneMath.Abs(rightWrist.m_X - shoulderCenter.m_X) <= shoulderWidth * context.m_Config.m_PoseCrossChestCenterRatio;
            bool leftNearChest = BoneMath.Abs(leftWrist.m_Y - upperBodyCenter.m_Y) <= shoulderWidth * context.m_Config.m_PoseCrossChestVerticalRatio;
            bool rightNearChest = BoneMath.Abs(rightWrist.m_Y - upperBodyCenter.m_Y) <= shoulderWidth * context.m_Config.m_PoseCrossChestVerticalRatio;
            return handsSwapped && leftNearCenter && rightNearCenter && leftNearChest && rightNearChest;
        }

        public static bool ReadIsHandsOnHip(BoneGestureRuntimeContext context)
        {
            BoneTrackedPerson person = context.m_Person;
            if (!context.m_Skeleton.TryReadBodyJoint(person, BoneBodyJointType.左手腕, out BoneVector2 leftWrist) ||
                !context.m_Skeleton.TryReadBodyJoint(person, BoneBodyJointType.右手腕, out BoneVector2 rightWrist) ||
                !context.m_Skeleton.TryReadBodyJoint(person, BoneBodyJointType.左髋, out BoneVector2 leftHip) ||
                !context.m_Skeleton.TryReadBodyJoint(person, BoneBodyJointType.右髋, out BoneVector2 rightHip))
            {
                return false;
            }

            float shoulderWidth = context.m_Skeleton.ReadShoulderWidth(person);
            if (shoulderWidth <= context.m_Config.m_ShoulderWidthEpsilon)
            {
                return false;
            }

            bool leftAttached = BoneMath.Abs(leftWrist.m_X - leftHip.m_X) <= shoulderWidth * context.m_Config.m_PoseHipAttachRatio &&
                BoneMath.Abs(leftWrist.m_Y - leftHip.m_Y) <= shoulderWidth * context.m_Config.m_PoseHipVerticalRatio;
            bool rightAttached = BoneMath.Abs(rightWrist.m_X - rightHip.m_X) <= shoulderWidth * context.m_Config.m_PoseHipAttachRatio &&
                BoneMath.Abs(rightWrist.m_Y - rightHip.m_Y) <= shoulderWidth * context.m_Config.m_PoseHipVerticalRatio;
            return leftAttached && rightAttached;
        }

        public static bool ReadIsLeftHandRaised(BoneGestureRuntimeContext context)
        {
            BoneTrackedPerson person = context.m_Person;
            if (!context.m_Skeleton.TryReadBodyJoint(person, BoneBodyJointType.左手腕, out BoneVector2 leftWrist) ||
                !context.m_Skeleton.TryReadHeadTop(person, out float headTopY))
            {
                return false;
            }

            float shoulderWidth = context.m_Skeleton.ReadShoulderWidth(person);
            if (shoulderWidth <= context.m_Config.m_ShoulderWidthEpsilon)
            {
                return false;
            }

            float threshold = headTopY - shoulderWidth * context.m_Config.m_PoseRaiseMarginRatio;
            return leftWrist.m_Y <= threshold;
        }

        public static bool ReadIsRightHandRaised(BoneGestureRuntimeContext context)
        {
            BoneTrackedPerson person = context.m_Person;
            if (!context.m_Skeleton.TryReadBodyJoint(person, BoneBodyJointType.右手腕, out BoneVector2 rightWrist) ||
                !context.m_Skeleton.TryReadHeadTop(person, out float headTopY))
            {
                return false;
            }

            float shoulderWidth = context.m_Skeleton.ReadShoulderWidth(person);
            if (shoulderWidth <= context.m_Config.m_ShoulderWidthEpsilon)
            {
                return false;
            }

            float threshold = headTopY - shoulderWidth * context.m_Config.m_PoseRaiseMarginRatio;
            return rightWrist.m_Y <= threshold;
        }
    }
}
