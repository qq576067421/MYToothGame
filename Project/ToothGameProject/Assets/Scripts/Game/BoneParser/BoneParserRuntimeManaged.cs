#if BoneParserLib
using System.Collections.Generic;
using ExternalBoneParserLib = CompanyInternalTools.BoneParserLib;

namespace GameDll
{
    internal sealed class BoneParserRuntimeManaged : IBoneParserRuntime
    {
        private readonly BoneParserConfig m_Config;
        private readonly ExternalBoneParserLib.BoneParserConfig m_ExternalConfig;
        private readonly ExternalBoneParserLib.BoneParser m_BoneParser;

        public string RuntimeName
        {
            get
            {
                return "BoneParserLib";
            }
        }

        public BoneParserRuntimeManaged(BoneParserConfig config)
        {
            m_Config = config ?? new BoneParserConfig();
            m_ExternalConfig = new ExternalBoneParserLib.BoneParserConfig();
            CopyConfigToExternal();
            m_BoneParser = new ExternalBoneParserLib.BoneParser(m_ExternalConfig);
        }

        public void Reset()
        {
            m_BoneParser.Reset();
        }

        public BoneParserFrameResult Update(BoneTrackedFrame frameData, IList<BoneParserSeatDefinition> seatDefinitions)
        {
            CopyConfigToExternal();
            return ConvertFrameResult(m_BoneParser.Update(
                ConvertFrame(frameData),
                ConvertSeatDefinitions(seatDefinitions)));
        }

        public bool ApplyActionConsumeResult(BoneActionConsumeResult consumeResult)
        {
            return m_BoneParser.ApplyActionConsumeResult(ConvertConsumeResult(consumeResult));
        }

        public void Shutdown()
        {
            m_BoneParser.Reset();
        }

        private void CopyConfigToExternal()
        {
            m_ExternalConfig.m_MinBodyScore = m_Config.m_MinBodyScore;
            m_ExternalConfig.m_MinJointScore = m_Config.m_MinJointScore;
            m_ExternalConfig.m_MaxMissingFrameCount = m_Config.m_MaxMissingFrameCount;
            m_ExternalConfig.m_ShoulderAngleTurnMaxOutputAngleDegrees = m_Config.m_ShoulderAngleTurnMaxOutputAngleDegrees;
            m_ExternalConfig.m_ShoulderAngleTurnInvertDirection = m_Config.m_ShoulderAngleTurnInvertDirection;
            m_ExternalConfig.m_ShoulderAngleTurnSmoothTimeSeconds = m_Config.m_ShoulderAngleTurnSmoothTimeSeconds;
            m_ExternalConfig.m_ShoulderAngleTurnFullInputDegrees = m_Config.m_ShoulderAngleTurnFullInputDegrees;
            m_ExternalConfig.m_ShoulderAngleTurnInputJitterDegrees = m_Config.m_ShoulderAngleTurnInputJitterDegrees;
            m_ExternalConfig.m_ShoulderAngleTurnMaxSpeedDegreesPerSecond = m_Config.m_ShoulderAngleTurnMaxSpeedDegreesPerSecond;
            m_ExternalConfig.m_ShoulderAngleTurnMinShoulderSpanToTorsoRatio =
                m_Config.m_ShoulderAngleTurnMinShoulderSpanToTorsoRatio;
            m_ExternalConfig.m_TurnUpdateDeltaTimeSeconds = m_Config.m_TurnUpdateDeltaTimeSeconds;
            m_ExternalConfig.m_KeypointConfidenceThreshold = m_Config.m_KeypointConfidenceThreshold;
            m_ExternalConfig.m_ShoulderWidthEpsilon = m_Config.m_ShoulderWidthEpsilon;
            m_ExternalConfig.m_AimReconnectStableFrames = m_Config.m_AimReconnectStableFrames;
            m_ExternalConfig.m_GestureKeypointMinConfidence = m_Config.m_GestureKeypointMinConfidence;
            m_ExternalConfig.m_UpDownSwingMinUpwardSpeedRatioPerSecond = m_Config.m_UpDownSwingMinUpwardSpeedRatioPerSecond;
            m_ExternalConfig.m_UpDownSwingStartMaxHeightRatio = m_Config.m_UpDownSwingStartMaxHeightRatio;
            m_ExternalConfig.m_UpDownSwingStartMinHeightRatio = m_Config.m_UpDownSwingStartMinHeightRatio;
            m_ExternalConfig.m_UpDownSwingMinStrokeDistanceRatio = m_Config.m_UpDownSwingMinStrokeDistanceRatio;
            m_ExternalConfig.m_UpDownSwingMaxLateralRatio = m_Config.m_UpDownSwingMaxLateralRatio;
            m_ExternalConfig.m_UpDownSwingPairingWindowSeconds = m_Config.m_UpDownSwingPairingWindowSeconds;
            m_ExternalConfig.m_LargeAlternatingSwingSpeedRatioPerSecond = m_Config.m_LargeAlternatingSwingSpeedRatioPerSecond;
            m_ExternalConfig.m_LargeAlternatingSwingDirectionNoiseRatio = m_Config.m_LargeAlternatingSwingDirectionNoiseRatio;
            m_ExternalConfig.m_LargeAlternatingSwingMinDirectionalFrames = m_Config.m_LargeAlternatingSwingMinDirectionalFrames;
            m_ExternalConfig.m_LargeAlternatingSwingCooldownSeconds = m_Config.m_LargeAlternatingSwingCooldownSeconds;
            m_ExternalConfig.m_LargeAlternatingSwingWindowFrames = m_Config.m_LargeAlternatingSwingWindowFrames;
            m_ExternalConfig.m_LargeAlternatingSwingMinTorsoDistanceRatio = m_Config.m_LargeAlternatingSwingMinTorsoDistanceRatio;
            m_ExternalConfig.m_HandsAboveNoseEnterMarginRatio = m_Config.m_HandsAboveNoseEnterMarginRatio;
            m_ExternalConfig.m_HandsAboveNoseExitMarginRatio = m_Config.m_HandsAboveNoseExitMarginRatio;
            m_ExternalConfig.m_CrossChestExpandReadyFrames = m_Config.m_CrossChestExpandReadyFrames;
            m_ExternalConfig.m_CrossChestExpandSpeedRatioPerSecond = m_Config.m_CrossChestExpandSpeedRatioPerSecond;
            m_ExternalConfig.m_CrossChestExpandMinDistanceRatio = m_Config.m_CrossChestExpandMinDistanceRatio;
            m_ExternalConfig.m_CrossChestExpandReleaseWindowFrames = m_Config.m_CrossChestExpandReleaseWindowFrames;
            m_ExternalConfig.m_CrossChestExpandCooldownSeconds = m_Config.m_CrossChestExpandCooldownSeconds;
            m_ExternalConfig.m_SingleHandPullDownReadyFrames = m_Config.m_SingleHandPullDownReadyFrames;
            m_ExternalConfig.m_SingleHandPullDownReleaseSpeedRatioPerSecond = m_Config.m_SingleHandPullDownReleaseSpeedRatioPerSecond;
            m_ExternalConfig.m_SingleHandPullDownMinDistanceRatio = m_Config.m_SingleHandPullDownMinDistanceRatio;
            m_ExternalConfig.m_SingleHandPullDownEndBelowShoulderRatio = m_Config.m_SingleHandPullDownEndBelowShoulderRatio;
            m_ExternalConfig.m_SingleHandPullDownReleaseWindowFrames = m_Config.m_SingleHandPullDownReleaseWindowFrames;
            m_ExternalConfig.m_SingleHandPullDownCooldownSeconds = m_Config.m_SingleHandPullDownCooldownSeconds;
            m_ExternalConfig.m_HandsOnHipRaiseReadyFrames = m_Config.m_HandsOnHipRaiseReadyFrames;
            m_ExternalConfig.m_HandsOnHipRaiseMinDistanceRatio = m_Config.m_HandsOnHipRaiseMinDistanceRatio;
            m_ExternalConfig.m_HandsOnHipRaiseEndAboveShoulderRatio = m_Config.m_HandsOnHipRaiseEndAboveShoulderRatio;
            m_ExternalConfig.m_HandsOnHipRaiseReleaseWindowFrames = m_Config.m_HandsOnHipRaiseReleaseWindowFrames;
            m_ExternalConfig.m_HandsOnHipRaiseCooldownSeconds = m_Config.m_HandsOnHipRaiseCooldownSeconds;
            m_ExternalConfig.m_CrouchStandRaiseReadyFrames = m_Config.m_CrouchStandRaiseReadyFrames;
            m_ExternalConfig.m_CrouchStandRaiseHandAboveShoulderRatio = m_Config.m_CrouchStandRaiseHandAboveShoulderRatio;
            m_ExternalConfig.m_CrouchStandRaiseReleaseWindowFrames = m_Config.m_CrouchStandRaiseReleaseWindowFrames;
            m_ExternalConfig.m_CrouchStandRaiseCooldownSeconds = m_Config.m_CrouchStandRaiseCooldownSeconds;
            m_ExternalConfig.m_ChestClosePushReadyFrames = m_Config.m_ChestClosePushReadyFrames;
            m_ExternalConfig.m_ChestClosePushCloseDistanceRatio = m_Config.m_ChestClosePushCloseDistanceRatio;
            m_ExternalConfig.m_ChestClosePushVerticalRatio = m_Config.m_ChestClosePushVerticalRatio;
            m_ExternalConfig.m_ChestClosePushMinDistanceRatio = m_Config.m_ChestClosePushMinDistanceRatio;
            m_ExternalConfig.m_ChestClosePushEndAboveShoulderRatio = m_Config.m_ChestClosePushEndAboveShoulderRatio;
            m_ExternalConfig.m_ChestClosePushSpeedRatioPerSecond = m_Config.m_ChestClosePushSpeedRatioPerSecond;
            m_ExternalConfig.m_ChestClosePushReleaseWindowFrames = m_Config.m_ChestClosePushReleaseWindowFrames;
            m_ExternalConfig.m_ChestClosePushCooldownSeconds = m_Config.m_ChestClosePushCooldownSeconds;
            m_ExternalConfig.m_HandsExpandReadyFrames = m_Config.m_HandsExpandReadyFrames;
            m_ExternalConfig.m_HandsExpandCloseDistanceRatio = m_Config.m_HandsExpandCloseDistanceRatio;
            m_ExternalConfig.m_HandsExpandCloseVerticalRatio = m_Config.m_HandsExpandCloseVerticalRatio;
            m_ExternalConfig.m_HandsExpandBeyondShoulderRatio = m_Config.m_HandsExpandBeyondShoulderRatio;
            m_ExternalConfig.m_HandsExpandVerticalToleranceRatio = m_Config.m_HandsExpandVerticalToleranceRatio;
            m_ExternalConfig.m_HandsExpandHoldFrames = m_Config.m_HandsExpandHoldFrames;
            m_ExternalConfig.m_HandsExpandReleaseWindowFrames = m_Config.m_HandsExpandReleaseWindowFrames;
            m_ExternalConfig.m_HandsExpandCooldownSeconds = m_Config.m_HandsExpandCooldownSeconds;
            m_ExternalConfig.m_LeftLeanReadyFrames = m_Config.m_LeftLeanReadyFrames;
            m_ExternalConfig.m_LeftLeanTriggerOffsetRatio = m_Config.m_LeftLeanTriggerOffsetRatio;
            m_ExternalConfig.m_LeftLeanReturnToNeutralRatio = m_Config.m_LeftLeanReturnToNeutralRatio;
            m_ExternalConfig.m_LeftLeanMinSpeedRatioPerSecond = m_Config.m_LeftLeanMinSpeedRatioPerSecond;
            m_ExternalConfig.m_LeftLeanCooldownSeconds = m_Config.m_LeftLeanCooldownSeconds;
            m_ExternalConfig.m_RightLeanReadyFrames = m_Config.m_RightLeanReadyFrames;
            m_ExternalConfig.m_RightLeanTriggerOffsetRatio = m_Config.m_RightLeanTriggerOffsetRatio;
            m_ExternalConfig.m_RightLeanReturnToNeutralRatio = m_Config.m_RightLeanReturnToNeutralRatio;
            m_ExternalConfig.m_RightLeanMinSpeedRatioPerSecond = m_Config.m_RightLeanMinSpeedRatioPerSecond;
            m_ExternalConfig.m_RightLeanCooldownSeconds = m_Config.m_RightLeanCooldownSeconds;
            m_ExternalConfig.m_JumpReadyFrames = m_Config.m_JumpReadyFrames;
            m_ExternalConfig.m_JumpReadyMaxVerticalSpeedRatioPerSecond = m_Config.m_JumpReadyMaxVerticalSpeedRatioPerSecond;
            m_ExternalConfig.m_JumpMinShoulderRiseRatio = m_Config.m_JumpMinShoulderRiseRatio;
            m_ExternalConfig.m_JumpMinHipRiseRatio = m_Config.m_JumpMinHipRiseRatio;
            m_ExternalConfig.m_JumpMinAnkleRiseRatio = m_Config.m_JumpMinAnkleRiseRatio;
            m_ExternalConfig.m_JumpMinUpwardSpeedRatioPerSecond = m_Config.m_JumpMinUpwardSpeedRatioPerSecond;
            m_ExternalConfig.m_JumpCooldownSeconds = m_Config.m_JumpCooldownSeconds;
            m_ExternalConfig.m_CrouchDownReadyFrames = m_Config.m_CrouchDownReadyFrames;
            m_ExternalConfig.m_CrouchDownReadyMaxVerticalSpeedRatioPerSecond = m_Config.m_CrouchDownReadyMaxVerticalSpeedRatioPerSecond;
            m_ExternalConfig.m_CrouchDownMinShoulderDropRatio = m_Config.m_CrouchDownMinShoulderDropRatio;
            m_ExternalConfig.m_CrouchDownMinHipDropRatio = m_Config.m_CrouchDownMinHipDropRatio;
            m_ExternalConfig.m_CrouchDownMinLegCompressionRatio = m_Config.m_CrouchDownMinLegCompressionRatio;
            m_ExternalConfig.m_CrouchDownMinDownwardSpeedRatioPerSecond = m_Config.m_CrouchDownMinDownwardSpeedRatioPerSecond;
            m_ExternalConfig.m_CrouchDownCooldownSeconds = m_Config.m_CrouchDownCooldownSeconds;
            m_ExternalConfig.m_CrouchDownConfirmHoldFrames = m_Config.m_CrouchDownConfirmHoldFrames;
            m_ExternalConfig.m_SingleHandSideSweepStartAboveHipToleranceRatio = m_Config.m_SingleHandSideSweepStartAboveHipToleranceRatio;
            m_ExternalConfig.m_SingleHandSideSweepStartBelowHipToleranceRatio = m_Config.m_SingleHandSideSweepStartBelowHipToleranceRatio;
            m_ExternalConfig.m_SingleHandSideSweepStartMovementDistanceRatio = m_Config.m_SingleHandSideSweepStartMovementDistanceRatio;
            m_ExternalConfig.m_SingleHandSideSweepElbowHeightToleranceRatio = m_Config.m_SingleHandSideSweepElbowHeightToleranceRatio;
            m_ExternalConfig.m_SingleHandSideSweepWristMissingToleranceFrames = m_Config.m_SingleHandSideSweepWristMissingToleranceFrames;
            m_ExternalConfig.m_SingleHandSideSweepMaxDurationSeconds = m_Config.m_SingleHandSideSweepMaxDurationSeconds;
            m_ExternalConfig.m_SingleHandSideSweepElbowMissingToleranceFrames = m_Config.m_SingleHandSideSweepElbowMissingToleranceFrames;
            m_ExternalConfig.m_SingleHandSideSweepMinTriggerIntervalSeconds = m_Config.m_SingleHandSideSweepMinTriggerIntervalSeconds;
            m_ExternalConfig.m_PoseRaiseMarginRatio = m_Config.m_PoseRaiseMarginRatio;
            m_ExternalConfig.m_PoseCrossChestCenterRatio = m_Config.m_PoseCrossChestCenterRatio;
            m_ExternalConfig.m_PoseCrossChestVerticalRatio = m_Config.m_PoseCrossChestVerticalRatio;
            m_ExternalConfig.m_PoseHipAttachRatio = m_Config.m_PoseHipAttachRatio;
            m_ExternalConfig.m_PoseHipVerticalRatio = m_Config.m_PoseHipVerticalRatio;
            m_ExternalConfig.m_PoseCrouchTorsoRatio = m_Config.m_PoseCrouchTorsoRatio;
            m_ExternalConfig.m_PoseStableFrames = m_Config.m_PoseStableFrames;
            m_ExternalConfig.m_LeanPredictiveLeadSeconds = m_Config.m_LeanPredictiveLeadSeconds;
            m_ExternalConfig.m_LeanPredictiveMinOffsetRatio = m_Config.m_LeanPredictiveMinOffsetRatio;
            m_ExternalConfig.m_LeanPredictiveMinSpeedRatioPerSecond = m_Config.m_LeanPredictiveMinSpeedRatioPerSecond;
            m_ExternalConfig.m_SingleHandSideSweepPredictiveLeadSeconds = m_Config.m_SingleHandSideSweepPredictiveLeadSeconds;
            m_ExternalConfig.m_SingleHandSideSweepPredictiveMinUpSpeedRatioPerSecond = m_Config.m_SingleHandSideSweepPredictiveMinUpSpeedRatioPerSecond;
            m_ExternalConfig.m_CrouchDownAirborneRiseRatio = m_Config.m_CrouchDownAirborneRiseRatio;
            m_ExternalConfig.m_CrouchDownAirborneHoldFrames = m_Config.m_CrouchDownAirborneHoldFrames;
            m_ExternalConfig.m_SingleHandSideSweepFastTriggerFrames = m_Config.m_SingleHandSideSweepFastTriggerFrames;
        }

        private static ExternalBoneParserLib.BoneTrackedFrame ConvertFrame(BoneTrackedFrame source)
        {
            ExternalBoneParserLib.BoneTrackedFrame target = new ExternalBoneParserLib.BoneTrackedFrame();
            if (source == null)
            {
                return target;
            }

            target.m_HasFrameData = source.m_HasFrameData;
            target.m_FrameSerial = source.m_FrameSerial;
            target.m_IsSimulated = source.m_IsSimulated;
            target.m_FrameTimeMs = source.m_FrameTimeMs;
            target.m_ImageWidth = source.m_ImageWidth;
            target.m_ImageHeight = source.m_ImageHeight;
            if (source.m_Persons == null)
            {
                return target;
            }

            for (int i = 0; i < source.m_Persons.Count; i++)
            {
                target.m_Persons.Add(ConvertPerson(source.m_Persons[i]));
            }

            return target;
        }

        private static ExternalBoneParserLib.BoneTrackedPerson ConvertPerson(BoneTrackedPerson source)
        {
            ExternalBoneParserLib.BoneTrackedPerson target = new ExternalBoneParserLib.BoneTrackedPerson();
            if (source == null)
            {
                return target;
            }

            target.m_PersonId = source.m_PersonId;
            CopyPartToExternal(source.m_Body, target.m_Body);
            CopyPartToExternal(source.m_LeftHand, target.m_LeftHand);
            CopyPartToExternal(source.m_RightHand, target.m_RightHand);
            CopyPartToExternal(source.m_Face, target.m_Face);
            return target;
        }

        private static void CopyPartToExternal(
            BoneTrackedPart source,
            ExternalBoneParserLib.BoneTrackedPart target)
        {
            if (source == null || target == null)
            {
                return;
            }

            CopyRectToExternal(source.m_Rect, target.m_Rect);
            target.m_Score = source.m_Score;
            target.m_Type = source.m_Type;
            int copyCount = source.m_Joints != null && target.m_Joints != null
                ? System.Math.Min(source.m_Joints.Length, target.m_Joints.Length)
                : 0;
            for (int i = 0; i < copyCount; i++)
            {
                CopyJointToExternal(source.m_Joints[i], target.m_Joints[i]);
            }
        }

        private static void CopyRectToExternal(
            BoneTrackedRect source,
            ExternalBoneParserLib.BoneTrackedRect target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.m_IsValid = source.m_IsValid;
            target.m_Left = source.m_Left;
            target.m_Top = source.m_Top;
            target.m_Right = source.m_Right;
            target.m_Bottom = source.m_Bottom;
        }

        private static void CopyJointToExternal(
            BoneTrackedJoint source,
            ExternalBoneParserLib.BoneTrackedJoint target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.m_IsTracked = source.m_IsTracked;
            target.m_X = source.m_X;
            target.m_Y = source.m_Y;
            target.m_Z = source.m_Z;
            target.m_Score = source.m_Score;
        }

        private static List<ExternalBoneParserLib.BoneParserSeatDefinition> ConvertSeatDefinitions(
            IList<BoneParserSeatDefinition> source)
        {
            List<ExternalBoneParserLib.BoneParserSeatDefinition> target =
                new List<ExternalBoneParserLib.BoneParserSeatDefinition>();
            if (source == null)
            {
                return target;
            }

            for (int i = 0; i < source.Count; i++)
            {
                target.Add(ConvertSeatDefinition(source[i]));
            }

            return target;
        }

        private static ExternalBoneParserLib.BoneParserSeatDefinition ConvertSeatDefinition(
            BoneParserSeatDefinition source)
        {
            ExternalBoneParserLib.BoneParserSeatDefinition target =
                new ExternalBoneParserLib.BoneParserSeatDefinition();
            if (source == null)
            {
                return target;
            }

            target.m_SlotIndex = source.m_SlotIndex;
            target.m_BindingId = source.m_BindingId;
            target.m_IsProcessGestureEnabled = source.m_IsProcessGestureEnabled;
            if (source.m_ActionBindings == null)
            {
                return target;
            }

            for (int i = 0; i < source.m_ActionBindings.Count; i++)
            {
                target.m_ActionBindings.Add(ConvertActionBinding(source.m_ActionBindings[i]));
            }

            return target;
        }

        private static ExternalBoneParserLib.BoneActionBinding ConvertActionBinding(BoneActionBinding source)
        {
            ExternalBoneParserLib.BoneActionBinding target = new ExternalBoneParserLib.BoneActionBinding();
            if (source == null)
            {
                return target;
            }

            target.m_ActionId = source.m_ActionId;
            target.m_GestureType = (ExternalBoneParserLib.BoneGestureType)(int)source.m_GestureType;
            target.m_PhaseMask = (ExternalBoneParserLib.BoneGesturePhaseMask)(int)source.m_PhaseMask;
            target.m_ConsumerType = (ExternalBoneParserLib.BoneActionConsumerType)(int)source.m_ConsumerType;
            target.m_ConsumerValue = source.m_ConsumerValue;
            target.m_RuntimeFlags = (ExternalBoneParserLib.BoneActionRuntimeFlags)(int)source.m_RuntimeFlags;
            target.m_RequiresConsumeResult = source.m_RequiresConsumeResult;
            return target;
        }

        private static ExternalBoneParserLib.BoneActionConsumeResult ConvertConsumeResult(
            BoneActionConsumeResult source)
        {
            ExternalBoneParserLib.BoneActionConsumeResult target =
                new ExternalBoneParserLib.BoneActionConsumeResult();
            if (source == null)
            {
                return target;
            }

            target.m_ActionEventId = source.m_ActionEventId;
            target.m_ResultType = (ExternalBoneParserLib.BoneActionConsumeResultType)(int)source.m_ResultType;
            return target;
        }

        private static BoneParserFrameResult ConvertFrameResult(
            ExternalBoneParserLib.BoneParserFrameResult source)
        {
            BoneParserFrameResult target = new BoneParserFrameResult();
            if (source == null)
            {
                return target;
            }

            target.m_FrameSerial = source.m_FrameSerial;
            if (source.m_PlayerResults == null)
            {
                return target;
            }

            for (int i = 0; i < source.m_PlayerResults.Count; i++)
            {
                target.m_PlayerResults.Add(ConvertPlayerResult(source.m_PlayerResults[i]));
            }

            return target;
        }

        private static BoneParserPlayerResult ConvertPlayerResult(
            ExternalBoneParserLib.BoneParserPlayerResult source)
        {
            BoneParserPlayerResult target = new BoneParserPlayerResult();
            if (source == null)
            {
                return target;
            }

            target.Reset(source.m_SlotIndex, source.m_BindingId);
            target.m_IsTracked = source.m_IsTracked;
            target.m_IsAimAvailable = source.m_IsAimAvailable;
            target.m_AimTrackingState = (BoneAimTrackingState)(int)source.m_AimTrackingState;
            target.m_AimConfidence = source.m_AimConfidence;
            target.m_MissingFrameCount = source.m_MissingFrameCount;
            target.m_PersonId = source.m_PersonId;
            target.m_FaceForward = ConvertVector3(source.m_FaceForward);
            target.m_TurnSpeed = source.m_TurnSpeed;
            target.m_TurnStrength = source.m_TurnStrength;
            target.m_TurnAngleDegrees = source.m_TurnAngleDegrees;
            target.m_ActivePoseHints = (BonePoseHintFlags)(int)source.m_ActivePoseHints;

            if (source.m_GestureEvents != null)
            {
                for (int i = 0; i < source.m_GestureEvents.Count; i++)
                {
                    target.m_GestureEvents.Add(ConvertGestureEvent(source.m_GestureEvents[i]));
                }
            }

            if (source.m_ActionEvents != null)
            {
                for (int i = 0; i < source.m_ActionEvents.Count; i++)
                {
                    target.m_ActionEvents.Add(ConvertActionEvent(source.m_ActionEvents[i]));
                }
            }

            return target;
        }

        private static BoneGestureEvent ConvertGestureEvent(ExternalBoneParserLib.BoneGestureEvent source)
        {
            BoneGestureEvent target = new BoneGestureEvent();
            if (source == null)
            {
                return target;
            }

            target.m_GestureType = (BoneGestureType)(int)source.m_GestureType;
            target.m_Phase = (BoneGesturePhase)(int)source.m_Phase;
            target.m_TriggerHand = (BoneGestureTriggerHand)(int)source.m_TriggerHand;
            target.m_SlotIndex = source.m_SlotIndex;
            target.m_BindingId = source.m_BindingId;
            target.m_PersonId = source.m_PersonId;
            target.m_FrameSerial = source.m_FrameSerial;
            return target;
        }

        private static BoneActionEvent ConvertActionEvent(ExternalBoneParserLib.BoneActionEvent source)
        {
            BoneActionEvent target = new BoneActionEvent();
            if (source == null)
            {
                return target;
            }

            target.m_ActionEventId = source.m_ActionEventId;
            target.m_ActionId = source.m_ActionId;
            target.m_GestureType = (BoneGestureType)(int)source.m_GestureType;
            target.m_Phase = (BoneGesturePhase)(int)source.m_Phase;
            target.m_TriggerHand = (BoneGestureTriggerHand)(int)source.m_TriggerHand;
            target.m_SlotIndex = source.m_SlotIndex;
            target.m_BindingId = source.m_BindingId;
            target.m_PersonId = source.m_PersonId;
            target.m_FrameSerial = source.m_FrameSerial;
            target.m_ConsumerType = (BoneActionConsumerType)(int)source.m_ConsumerType;
            target.m_ConsumerValue = source.m_ConsumerValue;
            target.m_RuntimeFlags = (BoneActionRuntimeFlags)(int)source.m_RuntimeFlags;
            target.m_RequiresConsumeResult = source.m_RequiresConsumeResult;
            target.m_FaceForward = ConvertVector3(source.m_FaceForward);
            target.m_MoveDirection = ConvertVector3(source.m_MoveDirection);
            return target;
        }

        private static BoneVector3 ConvertVector3(ExternalBoneParserLib.BoneVector3 source)
        {
            return new BoneVector3(source.m_X, source.m_Y, source.m_Z);
        }
    }
}
#endif
