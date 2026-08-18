namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class LeftHandRaisePoseRule : IBonePoseRule
    {
        public BonePoseHintFlags PoseFlag => BonePoseHintFlags.举左手;

        public BoneGestureType GestureType => BoneGestureType.举左手_姿势;

        public bool ReadIsActive(BoneGestureRuntimeContext context)
        {
            return BonePoseDetection.ReadIsLeftHandRaised(context);
        }
    }

    internal sealed class RightHandRaisePoseRule : IBonePoseRule
    {
        public BonePoseHintFlags PoseFlag => BonePoseHintFlags.举右手;

        public BoneGestureType GestureType => BoneGestureType.举右手_姿势;

        public bool ReadIsActive(BoneGestureRuntimeContext context)
        {
            return BonePoseDetection.ReadIsRightHandRaised(context);
        }
    }

    internal sealed class CrossChestPoseRule : IBonePoseRule
    {
        public BonePoseHintFlags PoseFlag => BonePoseHintFlags.双手交叉胸前;

        public BoneGestureType GestureType => BoneGestureType.双手交叉胸前_姿势;

        public bool ReadIsActive(BoneGestureRuntimeContext context)
        {
            return BonePoseDetection.ReadIsCrossChest(context);
        }
    }

    internal sealed class HandsOnHipPoseRule : IBonePoseRule
    {
        public BonePoseHintFlags PoseFlag => BonePoseHintFlags.双手叉腰;

        public BoneGestureType GestureType => BoneGestureType.双手叉腰_姿势;

        public bool ReadIsActive(BoneGestureRuntimeContext context)
        {
            return BonePoseDetection.ReadIsHandsOnHip(context);
        }
    }

    internal sealed class CrouchPoseRule : IBonePoseRule
    {
        public BonePoseHintFlags PoseFlag => BonePoseHintFlags.蹲下;

        public BoneGestureType GestureType => BoneGestureType.蹲下_姿势;

        public bool ReadIsActive(BoneGestureRuntimeContext context)
        {
            return BonePoseDetection.ReadIsCrouching(context);
        }
    }
}
