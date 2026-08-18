using System;
using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    public static class BoneTrackedLayout
    {
        public const int m_InvalidPersonId = -1;
        public const int m_BodyJointCount = 18;
        public const int m_HandJointCount = 21;
        public const int m_FaceJointCount = 5;
    }

    public enum BoneBodyJointType
    {
        鼻尖 = 0,
        左眼 = 1,
        右眼 = 2,
        左耳 = 3,
        右耳 = 4,
        左肩 = 5,
        右肩 = 6,
        左肘 = 7,
        右肘 = 8,
        左手腕 = 9,
        右手腕 = 10,
        左髋 = 11,
        右髋 = 12,
        左膝 = 13,
        右膝 = 14,
        左脚踝 = 15,
        右脚踝 = 16,
        胸口 = 17,
        数量 = 18,
    }

    public enum BonePoseHintFlags
    {
        无 = 0,
        举左手 = 1 << 0,
        举右手 = 1 << 1,
        双手交叉胸前 = 1 << 2,
        双手叉腰 = 1 << 3,
        蹲下 = 1 << 4,
    }

    // 配表说明：技能描述表 t_skillDescBean 的 t_gesture 和 t_gesture_phase 用来把技能绑定到骨骼手势。
    // t_gesture 填下面 BoneGestureType 枚举值的数字，不填枚举名；不要填写 未知、姿势开始、姿势结束、流程开始、流程结束。
    // t_gesture_phase 填 BoneGesturePhase 枚举值的数字：1 表示开始，2 表示持续，3 表示结束，4 表示触发。
    // 姿势手势编号在 1001 到 1006，表示玩家保持某个姿势，可使用开始、持续、结束阶段。
    // 流程手势编号在 2001 到 2011，表示玩家完成一套动作；技能触发通常填写触发阶段。
    // 当前技能表可直接使用：普攻填 t_gesture=2001、t_gesture_phase=4；技能填流程手势编号、t_gesture_phase=4。
    // 双手过头蓄力_流程 和 双手下压释放_流程 是流程内部阶段，不直接给技能描述表使用。
    // 新增可配手势时，需要新增对应识别器并注册到 BoneGestureRecognizerRegistry，支持阶段和消费结果要求由识别器定义。
    // 如果 t_gesture 或 t_gesture_phase 填错，战斗初始化绑定时会打印错误日志，该技能不会响应骨骼手势。
    public enum BoneGestureType
    {
        未知 = 0,
        姿势开始 = 1000,
        举左手_姿势 = 1001,
        举右手_姿势 = 1002,
        举双手_姿势 = 1003,
        双手交叉胸前_姿势 = 1004,
        双手叉腰_姿势 = 1005,
        蹲下_姿势 = 1006,
        姿势结束 = 1999,
        流程开始 = 2000,
        左右交替挥击_流程 = 2001,
        双手过头蓄力_流程 = 2002,
        双手下压释放_流程 = 2003,
        双手过头下压释放_流程 = 2004,
        双手交叉抱胸快速展开_流程 = 2005,
        单手举过头下拉_流程 = 2006,
        双手叉腰后上举_流程 = 2007,
        蹲下起身举手_流程 = 2008,
        双手胸前合拢后上推_流程 = 2009,
        双手左右展开保持_流程 = 2010,
        左右交替大幅挥击_流程 = 2011,
        流程结束 = 2999,
    }

    public enum BoneGesturePhase
    {
        开始 = 1,
        持续 = 2,
        结束 = 3,
        触发 = 4,
    }

    public enum BoneGesturePhaseMask
    {
        无 = 0,
        开始 = 1 << 0,
        持续 = 1 << 1,
        结束 = 1 << 2,
        触发 = 1 << 3,
    }

    public enum BoneGestureCategory
    {
        未知 = 0,
        姿势 = 1,
        流程 = 2,
    }

    public enum BoneActionConsumerType
    {
        未知 = 0,
        动作槽位 = 1,
    }

    public enum BoneActionRuntimeFlags
    {
        无 = 0,
        可识别 = 1 << 0,
        可消费 = 1 << 1,
    }

    public enum BoneActionConsumeResultType
    {
        未知 = 0,
        接受 = 1,
        拒绝可重试 = 2,
        拒绝阻断 = 3,
        忽略 = 4,
    }

    public enum BoneAimTrackingState
    {
        未跟踪 = 0,
        正常跟踪 = 1,
        短暂丢失 = 2,
        平滑回正 = 3,
    }

    public static class BoneGestureRules
    {
        public static bool TryResolveActionBinding(
            int gestureId,
            int gesturePhaseValue,
            out BoneGestureType gestureType,
            out BoneGesturePhaseMask phaseMask,
            out bool requiresConsumeResult,
            out string error)
        {
            gestureType = BoneGestureType.未知;
            phaseMask = BoneGesturePhaseMask.无;
            requiresConsumeResult = false;
            error = string.Empty;

            if (!TryResolveGesturePhaseMask(gesturePhaseValue, out phaseMask))
            {
                error = "手势阶段无效。";
                return false;
            }

            if (!TryResolveGestureType(gestureId, out gestureType))
            {
                error = "手势编号无效。";
                return false;
            }

            if (!ReadSupportsGesturePhase(gestureType, phaseMask))
            {
                error = "手势编号和手势阶段不匹配。";
                return false;
            }

            requiresConsumeResult = ReadRequiresConsumeResult(gestureType, phaseMask);
            return true;
        }

        public static bool TryResolveGestureType(int gestureId, out BoneGestureType gestureType)
        {
            gestureType = (BoneGestureType)gestureId;
            if (gestureType == BoneGestureType.未知)
            {
                return false;
            }

            if (ReadGestureCategory(gestureType) == BoneGestureCategory.未知)
            {
                gestureType = BoneGestureType.未知;
                return false;
            }

            if (!Enum.IsDefined(typeof(BoneGestureType), gestureType))
            {
                gestureType = BoneGestureType.未知;
                return false;
            }

            if (读取支持阶段掩码(gestureType) == BoneGesturePhaseMask.无)
            {
                gestureType = BoneGestureType.未知;
                return false;
            }

            return true;
        }

        public static bool TryResolveGesturePhaseMask(int gesturePhaseValue, out BoneGesturePhaseMask phaseMask)
        {
            if (!Enum.IsDefined(typeof(BoneGesturePhase), gesturePhaseValue))
            {
                phaseMask = BoneGesturePhaseMask.无;
                return false;
            }

            phaseMask = (BoneGesturePhaseMask)(1 << (gesturePhaseValue - 1));
            return true;
        }

        public static BoneGestureCategory ReadGestureCategory(BoneGestureType gestureType)
        {
            int gestureId = (int)gestureType;
            if (gestureId > (int)BoneGestureType.姿势开始 && gestureId < (int)BoneGestureType.姿势结束)
            {
                return BoneGestureCategory.姿势;
            }

            if (gestureId > (int)BoneGestureType.流程开始 && gestureId < (int)BoneGestureType.流程结束)
            {
                return BoneGestureCategory.流程;
            }

            return BoneGestureCategory.未知;
        }

        public static bool ReadSupportsGesturePhase(BoneGestureType gestureType, BoneGesturePhaseMask phaseMask)
        {
            BoneGesturePhaseMask supportedPhaseMask = 读取支持阶段掩码(gestureType);
            if (supportedPhaseMask == BoneGesturePhaseMask.无)
            {
                return false;
            }

            return (supportedPhaseMask & phaseMask) == phaseMask;
        }

        public static bool ReadRequiresConsumeResult(BoneGestureType gestureType, BoneGesturePhaseMask phaseMask)
        {
            return BoneGestureCatalog.TryReadDefinition(gestureType, out BoneGestureDefinition definition) &&
                definition.RequiresConsumeResult(phaseMask);
        }

        public static bool ReadIsProcessGesture(BoneGestureType gestureType)
        {
            return ReadGestureCategory(gestureType) == BoneGestureCategory.流程;
        }

        private static BoneGesturePhaseMask 读取支持阶段掩码(BoneGestureType gestureType)
        {
            return BoneGestureCatalog.TryReadDefinition(gestureType, out BoneGestureDefinition definition)
                ? definition.m_SupportedPhases
                : BoneGesturePhaseMask.无;
        }

    }

    public sealed class BoneTrackedJoint
    {
        public bool m_IsTracked;
        public float m_X;
        public float m_Y;
        public float m_Z;
        public float m_Score;

        public void Reset()
        {
            m_IsTracked = false;
            m_X = 0f;
            m_Y = 0f;
            m_Z = 0f;
            m_Score = 0f;
        }

        public void Set(float x, float y, float z, float score)
        {
            m_IsTracked = true;
            m_X = x;
            m_Y = y;
            m_Z = z;
            m_Score = score;
        }
    }

    public sealed class BoneTrackedRect
    {
        public bool m_IsValid;
        public float m_Left;
        public float m_Top;
        public float m_Right;
        public float m_Bottom;

        public void Reset()
        {
            m_IsValid = false;
            m_Left = 0f;
            m_Top = 0f;
            m_Right = 0f;
            m_Bottom = 0f;
        }

        public void Set(float left, float top, float right, float bottom)
        {
            m_IsValid = right > left && bottom > top;
            m_Left = left;
            m_Top = top;
            m_Right = right;
            m_Bottom = bottom;
        }

        public float ReadHeight()
        {
            return BoneMath.Max(0f, m_Bottom - m_Top);
        }

        public float ReadWidth()
        {
            return BoneMath.Max(0f, m_Right - m_Left);
        }
    }

    public sealed class BoneTrackedPart
    {
        public readonly BoneTrackedRect m_Rect;
        public readonly BoneTrackedJoint[] m_Joints;
        public float m_Score;
        public int m_Type;

        public BoneTrackedPart(int jointCount)
        {
            m_Rect = new BoneTrackedRect();
            m_Joints = new BoneTrackedJoint[jointCount];
            for (int i = 0; i < jointCount; i++)
            {
                m_Joints[i] = new BoneTrackedJoint();
            }
        }

        public void Reset()
        {
            m_Rect.Reset();
            m_Score = 0f;
            m_Type = 0;
            for (int i = 0; i < m_Joints.Length; i++)
            {
                m_Joints[i].Reset();
            }
        }
    }

    public sealed class BoneTrackedPerson
    {
        public int m_PersonId = BoneTrackedLayout.m_InvalidPersonId;
        public readonly BoneTrackedPart m_Body;
        public readonly BoneTrackedPart m_LeftHand;
        public readonly BoneTrackedPart m_RightHand;
        public readonly BoneTrackedPart m_Face;

        public BoneTrackedPerson()
        {
            m_Body = new BoneTrackedPart(BoneTrackedLayout.m_BodyJointCount);
            m_LeftHand = new BoneTrackedPart(BoneTrackedLayout.m_HandJointCount);
            m_RightHand = new BoneTrackedPart(BoneTrackedLayout.m_HandJointCount);
            m_Face = new BoneTrackedPart(BoneTrackedLayout.m_FaceJointCount);
        }

        public void Reset()
        {
            m_PersonId = BoneTrackedLayout.m_InvalidPersonId;
            m_Body.Reset();
            m_LeftHand.Reset();
            m_RightHand.Reset();
            m_Face.Reset();
        }
    }

    public sealed class BoneTrackedFrame
    {
        public bool m_HasFrameData;
        public int m_FrameSerial;
        public bool m_IsSimulated;
        public long m_FrameTimeMs;
        public int m_ImageWidth;
        public int m_ImageHeight;
        public readonly List<BoneTrackedPerson> m_Persons = new List<BoneTrackedPerson>();

        public void Reset()
        {
            m_HasFrameData = false;
            m_FrameSerial = 0;
            m_IsSimulated = false;
            m_FrameTimeMs = 0L;
            m_ImageWidth = 0;
            m_ImageHeight = 0;
            m_Persons.Clear();
        }
    }

    public sealed class BoneParserSeatDefinition
    {
        public int m_SlotIndex;
        public int m_BindingId;
        public bool m_IsProcessGestureEnabled = true;
        public readonly List<BoneActionBinding> m_ActionBindings = new List<BoneActionBinding>();
    }

    public sealed class BoneActionBinding
    {
        public int m_ActionId;
        public BoneGestureType m_GestureType = BoneGestureType.未知;
        public BoneGesturePhaseMask m_PhaseMask = BoneGesturePhaseMask.无;
        public BoneActionConsumerType m_ConsumerType = BoneActionConsumerType.未知;
        public int m_ConsumerValue;
        public BoneActionRuntimeFlags m_RuntimeFlags = BoneActionRuntimeFlags.可识别 | BoneActionRuntimeFlags.可消费;
        public bool m_RequiresConsumeResult;
    }

    public sealed class BoneGestureEvent
    {
        public BoneGestureType m_GestureType = BoneGestureType.未知;
        public BoneGesturePhase m_Phase = BoneGesturePhase.触发;
        public int m_SlotIndex;
        public int m_BindingId;
        public int m_PersonId = BoneTrackedLayout.m_InvalidPersonId;
        public int m_FrameSerial;
    }

    public sealed class BoneActionEvent
    {
        public int m_ActionEventId;
        public int m_ActionId;
        public BoneGestureType m_GestureType = BoneGestureType.未知;
        public BoneGesturePhase m_Phase = BoneGesturePhase.触发;
        public int m_SlotIndex;
        public int m_BindingId;
        public int m_PersonId = BoneTrackedLayout.m_InvalidPersonId;
        public int m_FrameSerial;
        public BoneActionConsumerType m_ConsumerType = BoneActionConsumerType.未知;
        public int m_ConsumerValue;
        public BoneActionRuntimeFlags m_RuntimeFlags = BoneActionRuntimeFlags.无;
        public bool m_RequiresConsumeResult;
        public BoneVector3 m_FaceForward = BoneVector3.m_Forward;
        public BoneVector3 m_MoveDirection = BoneVector3.m_Zero;
    }

    public sealed class BoneActionConsumeResult
    {
        public int m_ActionEventId;
        public BoneActionConsumeResultType m_ResultType = BoneActionConsumeResultType.未知;
    }

    public sealed class BoneParserPlayerResult
    {
        public int m_SlotIndex;
        public int m_BindingId;
        public bool m_IsTracked;
        public bool m_IsAimAvailable;
        public BoneAimTrackingState m_AimTrackingState = BoneAimTrackingState.未跟踪;
        public float m_AimConfidence;
        public int m_MissingFrameCount;
        public int m_PersonId = BoneTrackedLayout.m_InvalidPersonId;
        public BoneVector3 m_FaceForward = BoneVector3.m_Forward;
        public float m_TurnSpeed;
        public float m_TurnStrength;
        public float m_TurnAngleDegrees;
        public BonePoseHintFlags m_ActivePoseHints = BonePoseHintFlags.无;
        public readonly List<BoneGestureEvent> m_GestureEvents = new List<BoneGestureEvent>();
        public readonly List<BoneActionEvent> m_ActionEvents = new List<BoneActionEvent>();

        public void Reset(int slotIndex, int bindingId)
        {
            m_SlotIndex = slotIndex;
            m_BindingId = bindingId;
            m_IsTracked = false;
            m_IsAimAvailable = false;
            m_AimTrackingState = BoneAimTrackingState.未跟踪;
            m_AimConfidence = 0f;
            m_MissingFrameCount = 0;
            m_PersonId = BoneTrackedLayout.m_InvalidPersonId;
            m_FaceForward = BoneVector3.m_Forward;
            m_TurnSpeed = 0f;
            m_TurnStrength = 0f;
            m_TurnAngleDegrees = 0f;
            m_ActivePoseHints = BonePoseHintFlags.无;
            m_GestureEvents.Clear();
            m_ActionEvents.Clear();
        }
    }

    public sealed class BoneParserFrameResult
    {
        public int m_FrameSerial;
        public readonly List<BoneParserPlayerResult> m_PlayerResults = new List<BoneParserPlayerResult>();
    }

    public sealed class BoneParserConfig
    {
        public float m_MinBodyScore = 0.20f;
        public float m_MinJointScore = 0.20f;
        public int m_MaxMissingFrameCount = 18;

        public float m_RotationSmoothFactor = 6f;
        public float m_KeypointConfidenceThreshold = 0.30f;
        public float m_ShoulderWidthEpsilon = 0.0001f;
        public float m_MaxShoulderWidthUpdateConfidence = 0.70f;
        public float m_MaxTurnAngleDegrees = 35f;
        public bool m_InvertTurnDirection;
        public float m_RotationAmplifyFactor = 1.0f;
        public float m_AimCenterEnterRatio = 0.12f;
        public float m_AimCenterExitRatio = 0.07f;
        public float m_AimResponseCurveExponent = 1.35f;
        public int m_AimPredictMissingFrames = 6;
        public int m_AimHoldMissingFrames = 18;
        public int m_AimReconnectStableFrames = 3;
        public float m_AimPredictVelocityDamping = 0.88f;
        public float m_AimReconnectBlendFactor = 10f;
        public float m_AimReturnToForwardSeconds = 0.35f;

        public float m_GestureKeypointMinConfidence = 0.40f;
        public float m_AlternatingSwingSpeedRatioPerSecond = 1.20f;
        public float m_AlternatingSwingMinVerticalDistanceRatio = 0.20f;
        public float m_AlternatingSwingDirectionNoiseRatio = 0.015f;
        public int m_AlternatingSwingMinDirectionalFrames = 2;
        public float m_AlternatingSwingCooldownSeconds = 0.45f;
        public int m_AlternatingSwingWindowFrames = 24;
        public float m_LargeAlternatingSwingMinTorsoDistanceRatio = 0.50f;
        public int m_OverheadPressReadyFrames = 5;
        public float m_OverheadPressHeadMarginRatio = 0.12f;
        public float m_OverheadPressReleaseSpeedRatio = 2.80f;
        public float m_OverheadPressMinReleaseDistanceRatio = 0.35f;
        public float m_OverheadPressReleaseEndBelowHeadRatio = 0.15f;
        public int m_OverheadPressReleaseWindowFrames = 8;
        public float m_OverheadPressCooldownSeconds = 0.70f;
        public int m_CrossChestExpandReadyFrames = 3;
        public float m_CrossChestExpandSpeedRatioPerSecond = 2.20f;
        public float m_CrossChestExpandMinDistanceRatio = 0.30f;
        public int m_CrossChestExpandReleaseWindowFrames = 10;
        public float m_CrossChestExpandCooldownSeconds = 0.50f;
        public int m_SingleHandPullDownReadyFrames = 3;
        public float m_SingleHandPullDownReleaseSpeedRatioPerSecond = 2.20f;
        public float m_SingleHandPullDownMinDistanceRatio = 0.35f;
        public float m_SingleHandPullDownEndBelowShoulderRatio = 0.05f;
        public int m_SingleHandPullDownReleaseWindowFrames = 10;
        public float m_SingleHandPullDownCooldownSeconds = 0.50f;
        public int m_HandsOnHipRaiseReadyFrames = 3;
        public float m_HandsOnHipRaiseMinDistanceRatio = 0.45f;
        public float m_HandsOnHipRaiseEndAboveShoulderRatio = 0.12f;
        public int m_HandsOnHipRaiseReleaseWindowFrames = 12;
        public float m_HandsOnHipRaiseCooldownSeconds = 0.50f;
        public int m_CrouchStandRaiseReadyFrames = 3;
        public float m_CrouchStandRaiseHandAboveShoulderRatio = 0.12f;
        public int m_CrouchStandRaiseReleaseWindowFrames = 16;
        public float m_CrouchStandRaiseCooldownSeconds = 0.60f;
        public int m_ChestClosePushReadyFrames = 3;
        public float m_ChestClosePushCloseDistanceRatio = 0.55f;
        public float m_ChestClosePushVerticalRatio = 0.65f;
        public float m_ChestClosePushMinDistanceRatio = 0.28f;
        public float m_ChestClosePushEndAboveShoulderRatio = 0.10f;
        public float m_ChestClosePushSpeedRatioPerSecond = 1.80f;
        public int m_ChestClosePushReleaseWindowFrames = 12;
        public float m_ChestClosePushCooldownSeconds = 0.50f;
        public int m_HandsExpandReadyFrames = 3;
        public float m_HandsExpandCloseDistanceRatio = 0.70f;
        public float m_HandsExpandCloseVerticalRatio = 0.70f;
        public float m_HandsExpandBeyondShoulderRatio = 0.20f;
        public float m_HandsExpandVerticalToleranceRatio = 0.85f;
        public int m_HandsExpandHoldFrames = 3;
        public int m_HandsExpandReleaseWindowFrames = 18;
        public float m_HandsExpandCooldownSeconds = 0.60f;

        public float m_PoseRaiseMarginRatio = 0.05f;
        public float m_PoseCrossChestCenterRatio = 0.70f;
        public float m_PoseCrossChestVerticalRatio = 0.65f;
        public float m_PoseHipAttachRatio = 0.70f;
        public float m_PoseHipVerticalRatio = 0.60f;
        public float m_PoseCrouchTorsoRatio = 0.24f;
        public int m_PoseStableFrames = 3;
    }
}
