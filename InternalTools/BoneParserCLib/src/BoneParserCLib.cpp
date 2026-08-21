#include "BoneParserCLib/BoneParserCLib.h"

#include <algorithm>
#include <cmath>
#include <cstdint>
#include <new>
#include <vector>

namespace
{
    constexpr uint32_t kContextMagic = 0x4250434Cu;
    constexpr int32_t kInvalidPersonId = -1;
    constexpr int32_t kGestureLeftHandRaisePose = 1001;
    constexpr int32_t kGestureRightHandRaisePose = 1002;
    constexpr int32_t kGestureBothHandsRaisePose = 1003;
    constexpr int32_t kGestureCrossChestPose = 1004;
    constexpr int32_t kGestureHandsOnHipPose = 1005;
    constexpr int32_t kGestureCrouchPose = 1006;
    constexpr int32_t kGestureAlternatingSwingFlow = 2001;
    constexpr int32_t kGestureOverheadPressReleaseFlow = 2004;
    constexpr int32_t kGestureCrossChestExpandFlow = 2005;
    constexpr int32_t kGestureSingleHandPullDownFlow = 2006;
    constexpr int32_t kGestureHandsOnHipRaiseFlow = 2007;
    constexpr int32_t kGestureCrouchStandRaiseFlow = 2008;
    constexpr int32_t kGestureChestClosePushFlow = 2009;
    constexpr int32_t kGestureHandsExpandHoldFlow = 2010;
    constexpr int32_t kGestureLargeAlternatingSwingFlow = 2011;
    constexpr int32_t kPhaseStart = 1;
    constexpr int32_t kPhaseContinue = 2;
    constexpr int32_t kPhaseEnd = 3;
    constexpr int32_t kPhaseTrigger = 4;
    constexpr int32_t kPhaseMaskStart = 1 << 0;
    constexpr int32_t kPhaseMaskContinue = 1 << 1;
    constexpr int32_t kPhaseMaskEnd = 1 << 2;
    constexpr int32_t kPhaseMaskTrigger = 1 << 3;
    constexpr int32_t kRuntimeFlagRecognizable = 1 << 0;
    constexpr int32_t kConsumeResultAccept = 1;
    constexpr int32_t kConsumeResultRetry = 2;
    constexpr int32_t kAimStateNotTracked = 0;
    constexpr int32_t kAimStateTracked = 1;
    constexpr int32_t kAimStateMissing = 2;
    constexpr int32_t kAimStateReturning = 3;
    constexpr int32_t kPoseFlagLeftHandRaise = 1 << 0;
    constexpr int32_t kPoseFlagRightHandRaise = 1 << 1;
    constexpr int32_t kPoseFlagCrossChest = 1 << 2;
    constexpr int32_t kPoseFlagHandsOnHip = 1 << 3;
    constexpr int32_t kPoseFlagCrouch = 1 << 4;
    constexpr int32_t kLeftSide = 1;
    constexpr int32_t kRightSide = 2;

    enum BodyJoint
    {
        BodyJointNose = 0,
        BodyJointLeftEye = 1,
        BodyJointRightEye = 2,
        BodyJointLeftShoulder = 5,
        BodyJointRightShoulder = 6,
        BodyJointLeftWrist = 9,
        BodyJointRightWrist = 10,
        BodyJointLeftHip = 11,
        BodyJointRightHip = 12,
        BodyJointChest = 17,
    };

    struct Vec2
    {
        float m_X = 0.0f;
        float m_Y = 0.0f;
    };

    struct PoseState
    {
        int32_t m_LastPoseHints = 0;
        int32_t m_RawPoseHints = 0;
        int32_t m_RawPoseStableFrameCount = 0;

        void Reset()
        {
            m_LastPoseHints = 0;
            m_RawPoseHints = 0;
            m_RawPoseStableFrameCount = 0;
        }
    };

    struct AlternatingSwingHandState
    {
        bool m_HasLastRelativeWristY = false;
        float m_LastRelativeWristY = 0.0f;
        float m_StrokeStartRelativeWristY = 0.0f;
        float m_StrokeDurationSeconds = 0.0f;
        int32_t m_StrokeDirection = 0;
        int32_t m_DirectionalFrameCount = 0;
        int32_t m_IdleFrameCount = 0;

        void ResetStroke(float relativeWristY)
        {
            m_StrokeStartRelativeWristY = relativeWristY;
            m_StrokeDurationSeconds = 0.0f;
            m_StrokeDirection = 0;
            m_DirectionalFrameCount = 0;
            m_IdleFrameCount = 0;
        }

        void Reset()
        {
            m_HasLastRelativeWristY = false;
            m_LastRelativeWristY = 0.0f;
            ResetStroke(0.0f);
        }
    };

    struct AlternatingSwingState
    {
        AlternatingSwingHandState m_LeftHand;
        AlternatingSwingHandState m_RightHand;
        int32_t m_LastAlternatingSideMarker = 0;
        int32_t m_LastAlternatingFrameSerial = 0;
        float m_LastLeftAlternatingTimeSeconds = 0.0f;
        float m_LastRightAlternatingTimeSeconds = 0.0f;
        bool m_IsPending = false;
        int32_t m_ActionEventId = 0;
        bool m_LeftSwingDetected = false;
        bool m_RightSwingDetected = false;
        float m_FrameTimeSeconds = 0.0f;
        int32_t m_FrameSerial = 0;

        void ResetPending()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_LeftSwingDetected = false;
            m_RightSwingDetected = false;
            m_FrameTimeSeconds = 0.0f;
            m_FrameSerial = 0;
        }

        void Reset()
        {
            m_LeftHand.Reset();
            m_RightHand.Reset();
            m_LastAlternatingSideMarker = 0;
            m_LastAlternatingFrameSerial = 0;
            m_LastLeftAlternatingTimeSeconds = 0.0f;
            m_LastRightAlternatingTimeSeconds = 0.0f;
            ResetPending();
        }
    };

    struct OverheadPressReleaseState
    {
        bool m_HasLastLeftWrist = false;
        Vec2 m_LastLeftWrist;
        bool m_HasLastRightWrist = false;
        Vec2 m_LastRightWrist;
        int32_t m_ReadyFrameCount = 0;
        bool m_IsReady = false;
        Vec2 m_ReadyLeftWrist;
        Vec2 m_ReadyRightWrist;
        bool m_IsReleaseCollecting = false;
        int32_t m_ReleaseFrameCount = 0;
        bool m_HasLeftRelease = false;
        bool m_HasRightRelease = false;
        float m_CooldownUntilSeconds = 0.0f;
        bool m_LastActive = false;
        bool m_IsPending = false;
        int32_t m_ActionEventId = 0;
        float m_FrameTimeSeconds = 0.0f;

        void ResetReleaseWindow()
        {
            m_IsReleaseCollecting = false;
            m_ReleaseFrameCount = 0;
            m_HasLeftRelease = false;
            m_HasRightRelease = false;
        }

        void ResetReadyState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReadyLeftWrist = {};
            m_ReadyRightWrist = {};
            ResetReleaseWindow();
        }

        void StartReleaseWindow()
        {
            if (m_IsReleaseCollecting)
            {
                return;
            }

            m_IsReleaseCollecting = true;
            m_ReleaseFrameCount = 0;
            m_HasLeftRelease = false;
            m_HasRightRelease = false;
        }

        void ResetPending()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_FrameTimeSeconds = 0.0f;
        }

        void Reset()
        {
            m_HasLastLeftWrist = false;
            m_LastLeftWrist = {};
            m_HasLastRightWrist = false;
            m_LastRightWrist = {};
            ResetReadyState();
            m_CooldownUntilSeconds = 0.0f;
            m_LastActive = false;
            ResetPending();
        }
    };

    struct CrossChestExpandState
    {
        int32_t m_ReadyFrameCount = 0;
        bool m_IsReady = false;
        float m_ReadyWristDistance = 0.0f;
        bool m_IsReleaseCollecting = false;
        int32_t m_ReleaseFrameCount = 0;
        bool m_HasLastWristDistance = false;
        float m_LastWristDistance = 0.0f;
        float m_CooldownUntilSeconds = 0.0f;
        bool m_IsPending = false;
        int32_t m_ActionEventId = 0;
        float m_FrameTimeSeconds = 0.0f;

        void ResetReleaseWindow()
        {
            m_IsReleaseCollecting = false;
            m_ReleaseFrameCount = 0;
        }

        void ResetReadyState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReadyWristDistance = 0.0f;
            m_HasLastWristDistance = false;
            m_LastWristDistance = 0.0f;
            ResetReleaseWindow();
        }

        void StartReleaseWindow()
        {
            if (m_IsReleaseCollecting)
            {
                return;
            }

            m_IsReleaseCollecting = true;
            m_ReleaseFrameCount = 0;
        }

        void ResetPending()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_FrameTimeSeconds = 0.0f;
        }

        void Reset()
        {
            ResetReadyState();
            m_CooldownUntilSeconds = 0.0f;
            ResetPending();
        }
    };

    struct TriggerOnlyState
    {
        bool m_IsPending = false;
        int32_t m_ActionEventId = 0;
        float m_FrameTimeSeconds = 0.0f;
        float m_CooldownUntilSeconds = 0.0f;

        void ResetBase()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_FrameTimeSeconds = 0.0f;
            m_CooldownUntilSeconds = 0.0f;
        }

        void ResetPending()
        {
            m_IsPending = false;
            m_ActionEventId = 0;
            m_FrameTimeSeconds = 0.0f;
        }
    };

    struct SingleHandPullDownState : TriggerOnlyState
    {
        int32_t m_CandidateSide = 0;
        int32_t m_CandidateFrameCount = 0;
        int32_t m_ReadySide = 0;
        Vec2 m_ReadyWrist;
        Vec2 m_LastWrist;
        bool m_HasLastWrist = false;
        int32_t m_ReleaseFrameCount = 0;

        void ResetFlowState()
        {
            m_CandidateSide = 0;
            m_CandidateFrameCount = 0;
            m_ReadySide = 0;
            m_ReadyWrist = {};
            m_LastWrist = {};
            m_HasLastWrist = false;
            m_ReleaseFrameCount = 0;
        }

        void Reset()
        {
            ResetFlowState();
            ResetBase();
        }
    };

    struct HandsOnHipRaiseState : TriggerOnlyState
    {
        int32_t m_ReadyFrameCount = 0;
        bool m_IsReady = false;
        Vec2 m_ReadyLeftWrist;
        Vec2 m_ReadyRightWrist;
        int32_t m_ReleaseFrameCount = 0;

        void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReadyLeftWrist = {};
            m_ReadyRightWrist = {};
            m_ReleaseFrameCount = 0;
        }

        void Reset()
        {
            ResetFlowState();
            ResetBase();
        }
    };

    struct CrouchStandRaiseState : TriggerOnlyState
    {
        int32_t m_ReadyFrameCount = 0;
        bool m_IsReady = false;
        int32_t m_ReleaseFrameCount = 0;

        void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReleaseFrameCount = 0;
        }

        void Reset()
        {
            ResetFlowState();
            ResetBase();
        }
    };

    struct ChestClosePushState : TriggerOnlyState
    {
        int32_t m_ReadyFrameCount = 0;
        bool m_IsReady = false;
        Vec2 m_ReadyLeftWrist;
        Vec2 m_ReadyRightWrist;
        Vec2 m_LastLeftWrist;
        Vec2 m_LastRightWrist;
        bool m_HasLastWrist = false;
        int32_t m_ReleaseFrameCount = 0;

        void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReadyLeftWrist = {};
            m_ReadyRightWrist = {};
            m_LastLeftWrist = {};
            m_LastRightWrist = {};
            m_HasLastWrist = false;
            m_ReleaseFrameCount = 0;
        }

        void Reset()
        {
            ResetFlowState();
            ResetBase();
        }
    };

    struct HandsExpandHoldState : TriggerOnlyState
    {
        int32_t m_ReadyFrameCount = 0;
        bool m_IsReady = false;
        int32_t m_ReleaseFrameCount = 0;
        int32_t m_HoldFrameCount = 0;

        void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_ReleaseFrameCount = 0;
            m_HoldFrameCount = 0;
        }

        void Reset()
        {
            ResetFlowState();
            ResetBase();
        }
    };

    struct SeatState
    {
        int32_t m_CurrentPersonId = kInvalidPersonId;
        int32_t m_MissingFrameCount = 0;
        int64_t m_LastFrameTimeMs = 0;
        float m_CachedRotationOffset = 0.0f;
        float m_MaxObservedShoulderWidth = 0.0f;
        int32_t m_AimTrackingState = kAimStateNotTracked;
        bool m_HasAimOutput = false;
        float m_LastAimAngleDegrees = 0.0f;
        float m_LastAimAngularSpeedDegrees = 0.0f;
        int32_t m_AimMissingFrameCount = 0;
        int32_t m_AimCandidatePersonId = kInvalidPersonId;
        int32_t m_AimCandidateStableFrameCount = 0;
        bool m_IsAimOutsideCenterDeadZone = false;
        PoseState m_Pose;
        AlternatingSwingState m_AlternatingSwing;
        AlternatingSwingState m_LargeAlternatingSwing;
        OverheadPressReleaseState m_OverheadPressRelease;
        CrossChestExpandState m_CrossChestExpand;
        SingleHandPullDownState m_SingleHandPullDown;
        HandsOnHipRaiseState m_HandsOnHipRaise;
        CrouchStandRaiseState m_CrouchStandRaise;
        ChestClosePushState m_ChestClosePush;
        HandsExpandHoldState m_HandsExpandHold;

        void ResetAimState()
        {
            m_AimTrackingState = kAimStateNotTracked;
            m_HasAimOutput = false;
            m_LastAimAngleDegrees = 0.0f;
            m_LastAimAngularSpeedDegrees = 0.0f;
            m_AimMissingFrameCount = 0;
            m_AimCandidatePersonId = kInvalidPersonId;
            m_AimCandidateStableFrameCount = 0;
            m_IsAimOutsideCenterDeadZone = false;
        }

        void ResetGestureStates()
        {
            m_Pose.Reset();
            m_AlternatingSwing.Reset();
            m_LargeAlternatingSwing.Reset();
            m_OverheadPressRelease.Reset();
            m_CrossChestExpand.Reset();
            m_SingleHandPullDown.Reset();
            m_HandsOnHipRaise.Reset();
            m_CrouchStandRaise.Reset();
            m_ChestClosePush.Reset();
            m_HandsExpandHold.Reset();
        }

        void ResetForNewPerson(int32_t personId)
        {
            m_CurrentPersonId = personId;
            m_MissingFrameCount = 0;
            m_LastFrameTimeMs = 0;
            m_CachedRotationOffset = 0.0f;
            m_MaxObservedShoulderWidth = 0.0f;
            ResetAimState();
            ResetGestureStates();
        }

        void ResetForRelease()
        {
            m_CurrentPersonId = kInvalidPersonId;
            m_MissingFrameCount = 0;
            m_LastFrameTimeMs = 0;
            m_CachedRotationOffset = 0.0f;
            m_MaxObservedShoulderWidth = 0.0f;
            ResetAimState();
            ResetGestureStates();
        }
    };

    struct BoneParserCLibContext
    {
        uint32_t m_Magic = kContextMagic;
        uint64_t m_ResetCount = 0;
        int32_t m_FrameSerial = 0;
        int32_t m_NextActionEventId = 1;
        std::vector<SeatState> m_SeatStates;
    };

    struct OutputWriter
    {
        BoneParserCLibContext* m_Parser = nullptr;
        const BoneParserCLibSeatDefinition* m_SeatDefinitions = nullptr;
        int32_t m_SeatDefinitionCount = 0;
        const BoneParserCLibActionBinding* m_ActionBindings = nullptr;
        int32_t m_ActionBindingCount = 0;
        BoneParserCLibPlayerResultOutput* m_PlayerResults = nullptr;
        int32_t m_PlayerResultCapacity = 0;
        BoneParserCLibGestureEventOutput* m_GestureEvents = nullptr;
        int32_t m_GestureEventCapacity = 0;
        BoneParserCLibActionEventOutput* m_ActionEvents = nullptr;
        int32_t m_ActionEventCapacity = 0;
        BoneParserCLibFrameOutput* m_FrameOutput = nullptr;
        BoneParserCLibPlayerResultOutput* m_CurrentPlayer = nullptr;
        int32_t m_GestureEventCount = 0;
        int32_t m_ActionEventCount = 0;
        bool m_Overflow = false;
    };

    struct RuntimeContext
    {
        BoneParserCLibContext* m_Parser = nullptr;
        const BoneParserCLibConfig* m_Config = nullptr;
        OutputWriter* m_Writer = nullptr;
        const BoneParserCLibPerson* m_Person = nullptr;
        SeatState* m_SeatState = nullptr;
        const BoneParserCLibSeatDefinition* m_SeatDefinition = nullptr;
        BoneParserCLibPlayerResultOutput* m_Result = nullptr;
        const BoneParserCLibFrameInput* m_FrameData = nullptr;
        const BoneParserCLibActionBinding* m_ActionBindings = nullptr;
        int32_t m_ActionBindingCount = 0;
        float m_DeltaTimeSeconds = 0.0f;
        float m_FrameTimeSeconds = 0.0f;
    };

    struct UpperBodyFlowInput
    {
        float m_ShoulderWidth = 0.0f;
        float m_HeadTopY = 0.0f;
        bool m_HasHeadTop = false;
        Vec2 m_LeftShoulder;
        Vec2 m_RightShoulder;
        Vec2 m_ShoulderCenter;
        Vec2 m_UpperBodyCenter;
        Vec2 m_LeftWrist;
        Vec2 m_RightWrist;
    };

    float Abs(float value)
    {
        return std::fabs(value);
    }

    float Clamp(float value, float minValue, float maxValue)
    {
        return std::max(minValue, std::min(maxValue, value));
    }

    float Clamp01(float value)
    {
        return Clamp(value, 0.0f, 1.0f);
    }

    float Lerp(float start, float end, float t)
    {
        return start + (end - start) * Clamp01(t);
    }

    float Distance(Vec2 left, Vec2 right)
    {
        float deltaX = left.m_X - right.m_X;
        float deltaY = left.m_Y - right.m_Y;
        return std::sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    float Sign(float value)
    {
        if (value > 0.0f)
        {
            return 1.0f;
        }

        if (value < 0.0f)
        {
            return -1.0f;
        }

        return 0.0f;
    }

    Vec2 Add(Vec2 left, Vec2 right)
    {
        return { left.m_X + right.m_X, left.m_Y + right.m_Y };
    }

    Vec2 Scale(Vec2 value, float scale)
    {
        return { value.m_X * scale, value.m_Y * scale };
    }

    BoneParserCLibVector3 Forward()
    {
        return { 0.0f, 0.0f, 1.0f };
    }

    BoneParserCLibVector3 NormalizeOrDefault(BoneParserCLibVector3 value, BoneParserCLibVector3 fallback)
    {
        float lengthSquared = value.m_X * value.m_X + value.m_Y * value.m_Y + value.m_Z * value.m_Z;
        if (lengthSquared <= 0.0001f)
        {
            return fallback;
        }

        float invLength = 1.0f / std::sqrt(lengthSquared);
        return { value.m_X * invLength, value.m_Y * invLength, value.m_Z * invLength };
    }

    const BoneParserCLibConfig& ReadConfig(const BoneParserCLibConfig* config)
    {
        static const BoneParserCLibConfig kDefaultConfig = {
            0.20f, 0.20f, 18, 6.0f, 0.30f, 0.0001f, 0.70f, 35.0f, 0, 1.0f,
            0.12f, 0.07f, 1.35f, 0.0f, 6, 18, 3, 0.88f, 10.0f, 0.35f, 0.40f,
            1.20f, 0.20f, 0.015f, 2, 0.45f, 24,
            1.20f, 0.015f, 2, 0.45f, 24, 0.50f,
            5, 0.12f, 2.80f, 0.35f, 0.15f, 8, 0.70f, 3, 2.20f, 0.30f,
            10, 0.50f, 3, 2.20f, 0.35f, 0.05f, 10, 0.50f, 3, 0.45f,
            0.12f, 12, 0.50f, 3, 0.12f, 16, 0.60f, 3, 0.55f, 0.65f,
            0.28f, 0.10f, 1.80f, 12, 0.50f, 3, 0.70f, 0.70f, 0.20f,
            0.85f, 3, 18, 0.60f, 0.05f, 0.70f, 0.65f, 0.70f, 0.60f,
            0.24f, 3
        };
        return config != nullptr ? *config : kDefaultConfig;
    }

    BoneParserCLibContext* ReadContext(BoneParserCLibHandle parser)
    {
        auto* context = static_cast<BoneParserCLibContext*>(parser);
        if (context == nullptr || context->m_Magic != kContextMagic)
        {
            return nullptr;
        }

        return context;
    }

    void ResetPlayerResult(BoneParserCLibPlayerResultOutput& result, int32_t slotIndex, int32_t bindingId, int32_t gestureStart, int32_t actionStart)
    {
        result = {};
        result.m_SlotIndex = slotIndex;
        result.m_BindingId = bindingId;
        result.m_AimTrackingState = kAimStateNotTracked;
        result.m_PersonId = kInvalidPersonId;
        result.m_FaceForward = Forward();
        result.m_GestureEventStart = gestureStart;
        result.m_ActionEventStart = actionStart;
    }

    const BoneParserCLibSeatDefinition* ReadSeatDefinition(
        const BoneParserCLibSeatDefinition* seatDefinitions,
        int32_t seatDefinitionCount,
        int32_t definitionIndex)
    {
        if (seatDefinitions == nullptr || definitionIndex < 0 || definitionIndex >= seatDefinitionCount)
        {
            return nullptr;
        }

        return &seatDefinitions[definitionIndex];
    }

    int32_t ReadFrameSlotIndex(
        const BoneParserCLibSeatDefinition* seatDefinitions,
        int32_t seatDefinitionCount,
        int32_t definitionIndex)
    {
        const BoneParserCLibSeatDefinition* definition = ReadSeatDefinition(seatDefinitions, seatDefinitionCount, definitionIndex);
        return definition != nullptr ? definition->m_SlotIndex : definitionIndex;
    }

    int32_t ReadFrameBindingId(
        const BoneParserCLibSeatDefinition* seatDefinitions,
        int32_t seatDefinitionCount,
        int32_t definitionIndex)
    {
        const BoneParserCLibSeatDefinition* definition = ReadSeatDefinition(seatDefinitions, seatDefinitionCount, definitionIndex);
        return definition != nullptr ? definition->m_BindingId : definitionIndex;
    }

    bool TryReadSlotPerson(const BoneParserCLibFrameInput* frameData, int32_t slotIndex, const BoneParserCLibPerson*& person)
    {
        person = nullptr;
        if (frameData == nullptr || frameData->m_Persons == nullptr || slotIndex < 0 || slotIndex >= frameData->m_PersonCount)
        {
            return false;
        }

        person = &frameData->m_Persons[slotIndex];
        return true;
    }

    bool TryReadBodyJoint(const BoneParserCLibPerson* person, int32_t jointType, float minScore, Vec2& point)
    {
        point = {};
        if (person == nullptr ||
            person->m_BodyJoints == nullptr ||
            jointType < 0 ||
            jointType >= person->m_BodyJointCount)
        {
            return false;
        }

        const BoneParserCLibJoint& joint = person->m_BodyJoints[jointType];
        if (joint.m_IsTracked == 0 || joint.m_Score < minScore)
        {
            return false;
        }

        point = { joint.m_X, joint.m_Y };
        return true;
    }

    bool TryReadBodyJoint(const BoneParserCLibConfig& config, const BoneParserCLibPerson* person, int32_t jointType, Vec2& point)
    {
        return TryReadBodyJoint(person, jointType, config.m_MinJointScore, point);
    }

    bool ReadIsBindablePerson(const BoneParserCLibConfig& config, const BoneParserCLibPerson* person)
    {
        if (person == nullptr || person->m_BodyScore < config.m_MinBodyScore)
        {
            return false;
        }

        if (person->m_BodyRect.m_IsValid != 0)
        {
            return true;
        }

        Vec2 ignored;
        return TryReadBodyJoint(config, person, BodyJointLeftShoulder, ignored) &&
            TryReadBodyJoint(config, person, BodyJointRightShoulder, ignored);
    }

    float ReadShoulderWidth(const BoneParserCLibConfig& config, const BoneParserCLibPerson* person)
    {
        Vec2 leftShoulder;
        Vec2 rightShoulder;
        if (!TryReadBodyJoint(config, person, BodyJointLeftShoulder, leftShoulder) ||
            !TryReadBodyJoint(config, person, BodyJointRightShoulder, rightShoulder))
        {
            return 0.0f;
        }

        return Abs(rightShoulder.m_X - leftShoulder.m_X) + config.m_ShoulderWidthEpsilon;
    }

    bool TryReadHeadTop(const BoneParserCLibConfig& config, const BoneParserCLibPerson* person, float& headTopY)
    {
        headTopY = 0.0f;
        bool hasHeadPoint = false;
        Vec2 point;
        if (TryReadBodyJoint(person, BodyJointNose, config.m_GestureKeypointMinConfidence, point))
        {
            headTopY = point.m_Y;
            hasHeadPoint = true;
        }

        if (TryReadBodyJoint(person, BodyJointLeftEye, config.m_GestureKeypointMinConfidence, point))
        {
            headTopY = hasHeadPoint ? std::min(headTopY, point.m_Y) : point.m_Y;
            hasHeadPoint = true;
        }

        if (TryReadBodyJoint(person, BodyJointRightEye, config.m_GestureKeypointMinConfidence, point))
        {
            headTopY = hasHeadPoint ? std::min(headTopY, point.m_Y) : point.m_Y;
            hasHeadPoint = true;
        }

        return hasHeadPoint;
    }

    bool TryReadHipCenter(const BoneParserCLibConfig& config, const BoneParserCLibPerson* person, Vec2& hipCenter)
    {
        Vec2 leftHip;
        Vec2 rightHip;
        if (TryReadBodyJoint(config, person, BodyJointLeftHip, leftHip) &&
            TryReadBodyJoint(config, person, BodyJointRightHip, rightHip))
        {
            hipCenter = Scale(Add(leftHip, rightHip), 0.5f);
            return true;
        }

        hipCenter = {};
        return false;
    }

    bool TryReadShoulderCenter(const BoneParserCLibConfig& config, const BoneParserCLibPerson* person, Vec2& shoulderCenter)
    {
        Vec2 leftShoulder;
        Vec2 rightShoulder;
        if (TryReadBodyJoint(config, person, BodyJointLeftShoulder, leftShoulder) &&
            TryReadBodyJoint(config, person, BodyJointRightShoulder, rightShoulder))
        {
            shoulderCenter = Scale(Add(leftShoulder, rightShoulder), 0.5f);
            return true;
        }

        shoulderCenter = {};
        return false;
    }

    bool TryReadUpperBodyCenter(const BoneParserCLibConfig& config, const BoneParserCLibPerson* person, Vec2& upperBodyCenter)
    {
        Vec2 shoulderCenter;
        if (!TryReadShoulderCenter(config, person, shoulderCenter))
        {
            upperBodyCenter = {};
            return false;
        }

        Vec2 chest;
        if (TryReadBodyJoint(config, person, BodyJointChest, chest))
        {
            upperBodyCenter = Scale(Add(shoulderCenter, chest), 0.5f);
            return true;
        }

        upperBodyCenter = shoulderCenter;
        return true;
    }

    float ReadRectHeight(const BoneParserCLibRect& rect)
    {
        return std::max(0.0f, rect.m_Bottom - rect.m_Top);
    }

    bool ReadIsCrouching(const RuntimeContext& context)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        const BoneParserCLibPerson* person = context.m_Person;
        if (person == nullptr || person->m_BodyRect.m_IsValid == 0)
        {
            return false;
        }

        Vec2 shoulderCenter;
        Vec2 hipCenter;
        if (!TryReadShoulderCenter(config, person, shoulderCenter) ||
            !TryReadHipCenter(config, person, hipCenter))
        {
            return false;
        }

        float bodyHeight = ReadRectHeight(person->m_BodyRect);
        if (bodyHeight <= 0.0001f)
        {
            return false;
        }

        float torsoHeight = Abs(hipCenter.m_Y - shoulderCenter.m_Y);
        return torsoHeight / bodyHeight <= config.m_PoseCrouchTorsoRatio;
    }

    bool ReadIsCrossChest(const RuntimeContext& context)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        const BoneParserCLibPerson* person = context.m_Person;
        Vec2 leftWrist;
        Vec2 rightWrist;
        Vec2 shoulderCenter;
        Vec2 upperBodyCenter;
        if (!TryReadBodyJoint(config, person, BodyJointLeftWrist, leftWrist) ||
            !TryReadBodyJoint(config, person, BodyJointRightWrist, rightWrist) ||
            !TryReadShoulderCenter(config, person, shoulderCenter) ||
            !TryReadUpperBodyCenter(config, person, upperBodyCenter))
        {
            return false;
        }

        float shoulderWidth = ReadShoulderWidth(config, person);
        if (shoulderWidth <= config.m_ShoulderWidthEpsilon)
        {
            return false;
        }

        bool handsSwapped = leftWrist.m_X > shoulderCenter.m_X && rightWrist.m_X < shoulderCenter.m_X;
        bool leftNearCenter = Abs(leftWrist.m_X - shoulderCenter.m_X) <= shoulderWidth * config.m_PoseCrossChestCenterRatio;
        bool rightNearCenter = Abs(rightWrist.m_X - shoulderCenter.m_X) <= shoulderWidth * config.m_PoseCrossChestCenterRatio;
        bool leftNearChest = Abs(leftWrist.m_Y - upperBodyCenter.m_Y) <= shoulderWidth * config.m_PoseCrossChestVerticalRatio;
        bool rightNearChest = Abs(rightWrist.m_Y - upperBodyCenter.m_Y) <= shoulderWidth * config.m_PoseCrossChestVerticalRatio;
        return handsSwapped && leftNearCenter && rightNearCenter && leftNearChest && rightNearChest;
    }

    bool ReadIsHandsOnHip(const RuntimeContext& context)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        const BoneParserCLibPerson* person = context.m_Person;
        Vec2 leftWrist;
        Vec2 rightWrist;
        Vec2 leftHip;
        Vec2 rightHip;
        if (!TryReadBodyJoint(config, person, BodyJointLeftWrist, leftWrist) ||
            !TryReadBodyJoint(config, person, BodyJointRightWrist, rightWrist) ||
            !TryReadBodyJoint(config, person, BodyJointLeftHip, leftHip) ||
            !TryReadBodyJoint(config, person, BodyJointRightHip, rightHip))
        {
            return false;
        }

        float shoulderWidth = ReadShoulderWidth(config, person);
        if (shoulderWidth <= config.m_ShoulderWidthEpsilon)
        {
            return false;
        }

        bool leftAttached = Abs(leftWrist.m_X - leftHip.m_X) <= shoulderWidth * config.m_PoseHipAttachRatio &&
            Abs(leftWrist.m_Y - leftHip.m_Y) <= shoulderWidth * config.m_PoseHipVerticalRatio;
        bool rightAttached = Abs(rightWrist.m_X - rightHip.m_X) <= shoulderWidth * config.m_PoseHipAttachRatio &&
            Abs(rightWrist.m_Y - rightHip.m_Y) <= shoulderWidth * config.m_PoseHipVerticalRatio;
        return leftAttached && rightAttached;
    }

    bool ReadIsLeftHandRaised(const RuntimeContext& context)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        Vec2 leftWrist;
        float headTopY = 0.0f;
        if (!TryReadBodyJoint(config, context.m_Person, BodyJointLeftWrist, leftWrist) ||
            !TryReadHeadTop(config, context.m_Person, headTopY))
        {
            return false;
        }

        float shoulderWidth = ReadShoulderWidth(config, context.m_Person);
        if (shoulderWidth <= config.m_ShoulderWidthEpsilon)
        {
            return false;
        }

        float threshold = headTopY - shoulderWidth * config.m_PoseRaiseMarginRatio;
        return leftWrist.m_Y <= threshold;
    }

    bool ReadIsRightHandRaised(const RuntimeContext& context)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        Vec2 rightWrist;
        float headTopY = 0.0f;
        if (!TryReadBodyJoint(config, context.m_Person, BodyJointRightWrist, rightWrist) ||
            !TryReadHeadTop(config, context.m_Person, headTopY))
        {
            return false;
        }

        float shoulderWidth = ReadShoulderWidth(config, context.m_Person);
        if (shoulderWidth <= config.m_ShoulderWidthEpsilon)
        {
            return false;
        }

        float threshold = headTopY - shoulderWidth * config.m_PoseRaiseMarginRatio;
        return rightWrist.m_Y <= threshold;
    }

    int32_t ConvertPhaseMask(int32_t phase)
    {
        switch (phase)
        {
            case kPhaseStart:
                return kPhaseMaskStart;
            case kPhaseContinue:
                return kPhaseMaskContinue;
            case kPhaseEnd:
                return kPhaseMaskEnd;
            case kPhaseTrigger:
                return kPhaseMaskTrigger;
            default:
                return 0;
        }
    }

    int32_t ReadPersonId(const RuntimeContext& context)
    {
        return context.m_Person != nullptr ? context.m_Person->m_PersonId : kInvalidPersonId;
    }

    int32_t ReadFrameSerial(const RuntimeContext& context)
    {
        if (context.m_FrameData != nullptr)
        {
            return context.m_FrameData->m_FrameSerial;
        }

        return context.m_Result != nullptr ? context.m_Result->m_SlotIndex : 0;
    }

    bool HasRecognizableActionBinding(const RuntimeContext& context, int32_t gestureType)
    {
        if (context.m_SeatDefinition == nullptr || context.m_ActionBindings == nullptr)
        {
            return false;
        }

        int32_t start = context.m_SeatDefinition->m_ActionBindingStart;
        int32_t count = context.m_SeatDefinition->m_ActionBindingCount;
        for (int32_t i = 0; i < count; i++)
        {
            int32_t index = start + i;
            if (index < 0 || index >= context.m_ActionBindingCount)
            {
                continue;
            }

            const BoneParserCLibActionBinding& binding = context.m_ActionBindings[index];
            if (binding.m_GestureType == gestureType &&
                (binding.m_RuntimeFlags & kRuntimeFlagRecognizable) != 0)
            {
                return true;
            }
        }

        return false;
    }

    const BoneParserCLibActionBinding* FindFirstActionBinding(const RuntimeContext& context, int32_t gestureType, int32_t phase)
    {
        if (context.m_SeatDefinition == nullptr || context.m_ActionBindings == nullptr)
        {
            return nullptr;
        }

        int32_t phaseMask = ConvertPhaseMask(phase);
        int32_t start = context.m_SeatDefinition->m_ActionBindingStart;
        int32_t count = context.m_SeatDefinition->m_ActionBindingCount;
        for (int32_t i = 0; i < count; i++)
        {
            int32_t index = start + i;
            if (index < 0 || index >= context.m_ActionBindingCount)
            {
                continue;
            }

            const BoneParserCLibActionBinding& binding = context.m_ActionBindings[index];
            if (binding.m_GestureType != gestureType ||
                (binding.m_PhaseMask & phaseMask) == 0 ||
                (binding.m_RuntimeFlags & kRuntimeFlagRecognizable) == 0)
            {
                continue;
            }

            return &binding;
        }

        return nullptr;
    }

    BoneParserCLibActionEventOutput* AddActionEvent(
        RuntimeContext& context,
        int32_t gestureType,
        int32_t phase,
        int32_t personId,
        int32_t frameSerial)
    {
        OutputWriter* writer = context.m_Writer;
        if (writer == nullptr || context.m_Result == nullptr)
        {
            return nullptr;
        }

        const BoneParserCLibActionBinding* binding = FindFirstActionBinding(context, gestureType, phase);
        if (binding == nullptr)
        {
            return nullptr;
        }

        if (writer->m_ActionEventCount >= writer->m_ActionEventCapacity || writer->m_ActionEvents == nullptr)
        {
            writer->m_Overflow = true;
            return nullptr;
        }

        int32_t actionIndex = writer->m_ActionEventCount++;
        BoneParserCLibActionEventOutput& actionEvent = writer->m_ActionEvents[actionIndex];
        actionEvent = {};
        actionEvent.m_ActionEventId = writer->m_Parser->m_NextActionEventId++;
        actionEvent.m_ActionId = binding->m_ActionId;
        actionEvent.m_GestureType = gestureType;
        actionEvent.m_Phase = phase;
        actionEvent.m_SlotIndex = context.m_Result->m_SlotIndex;
        actionEvent.m_BindingId = context.m_Result->m_BindingId;
        actionEvent.m_PersonId = personId;
        actionEvent.m_FrameSerial = frameSerial;
        actionEvent.m_ConsumerType = binding->m_ConsumerType;
        actionEvent.m_ConsumerValue = binding->m_ConsumerValue;
        actionEvent.m_RuntimeFlags = binding->m_RuntimeFlags;
        actionEvent.m_RequiresConsumeResult = binding->m_RequiresConsumeResult;
        actionEvent.m_FaceForward = context.m_Result->m_FaceForward;
        actionEvent.m_MoveDirection = context.m_Result->m_FaceForward;
        context.m_Result->m_ActionEventCount++;
        return &actionEvent;
    }

    BoneParserCLibActionEventOutput* AddGestureEvent(RuntimeContext& context, int32_t gestureType, int32_t phase)
    {
        OutputWriter* writer = context.m_Writer;
        if (writer == nullptr || context.m_Result == nullptr)
        {
            return nullptr;
        }

        int32_t personId = ReadPersonId(context);
        int32_t frameSerial = ReadFrameSerial(context);
        if (writer->m_GestureEventCount >= writer->m_GestureEventCapacity || writer->m_GestureEvents == nullptr)
        {
            writer->m_Overflow = true;
            return nullptr;
        }

        int32_t gestureIndex = writer->m_GestureEventCount++;
        BoneParserCLibGestureEventOutput& gestureEvent = writer->m_GestureEvents[gestureIndex];
        gestureEvent = {};
        gestureEvent.m_GestureType = gestureType;
        gestureEvent.m_Phase = phase;
        gestureEvent.m_SlotIndex = context.m_Result->m_SlotIndex;
        gestureEvent.m_BindingId = context.m_Result->m_BindingId;
        gestureEvent.m_PersonId = personId;
        gestureEvent.m_FrameSerial = frameSerial;
        context.m_Result->m_GestureEventCount++;
        return AddActionEvent(context, gestureType, phase, personId, frameSerial);
    }

    void AddBooleanGestureEvent(RuntimeContext& context, bool previousActive, bool currentActive, int32_t gestureType)
    {
        if (!previousActive && currentActive)
        {
            AddGestureEvent(context, gestureType, kPhaseStart);
            return;
        }

        if (previousActive && currentActive)
        {
            AddGestureEvent(context, gestureType, kPhaseContinue);
            return;
        }

        if (previousActive && !currentActive)
        {
            AddGestureEvent(context, gestureType, kPhaseEnd);
        }
    }

    float ReadClampedMaxAngle(const BoneParserCLibConfig& config)
    {
        return Clamp(config.m_MaxTurnAngleDegrees, 0.0f, 45.0f);
    }

    float ReadTurnDirectionMultiplier(const BoneParserCLibConfig& config)
    {
        return config.m_InvertTurnDirection != 0 ? -1.0f : 1.0f;
    }

    float ReadTurnAmplifyFactor(const BoneParserCLibConfig& config)
    {
        return Clamp(config.m_RotationAmplifyFactor, 0.0f, 5.0f);
    }

    float ConvertOffsetToAngle(float normalizedOffset, float maxAngle, float rotationAmplifyFactor)
    {
        return Clamp(-normalizedOffset * rotationAmplifyFactor, -1.0f, 1.0f) * maxAngle;
    }

    float ApplyAimResponseCurve(
        float targetOffset,
        bool& isOutsideDeadZone,
        const BoneParserCLibConfig& config)
    {
        float enterRatio = Clamp01(config.m_AimCenterEnterRatio);
        float exitRatio = Clamp(config.m_AimCenterExitRatio, 0.0f, enterRatio);
        float magnitude = Abs(targetOffset);
        if (isOutsideDeadZone)
        {
            if (magnitude <= exitRatio)
            {
                isOutsideDeadZone = false;
                return 0.0f;
            }
        }
        else
        {
            if (magnitude < enterRatio)
            {
                return 0.0f;
            }

            isOutsideDeadZone = true;
        }

        float availableRange = std::max(1.0f - exitRatio, config.m_ShoulderWidthEpsilon);
        float normalizedMagnitude = Clamp01((magnitude - exitRatio) / availableRange);
        float exponent = std::max(1.0f, config.m_AimResponseCurveExponent);
        return std::pow(normalizedMagnitude, exponent) * Sign(targetOffset);
    }

    float ApplyShoulderTurnJitterDeadZone(float value, float jitterDeadZone)
    {
        float normalizedDeadZone = Clamp01(jitterDeadZone);
        if (normalizedDeadZone <= 0.0f)
        {
            return value;
        }

        if (normalizedDeadZone >= 1.0f)
        {
            return 0.0f;
        }

        float magnitude = Abs(value);
        if (magnitude <= normalizedDeadZone)
        {
            return 0.0f;
        }

        float normalizedMagnitude = (magnitude - normalizedDeadZone) /
            std::max(1.0f - normalizedDeadZone, 0.0001f);
        return Sign(value) * Clamp01(normalizedMagnitude);
    }

    float ConvertAngleToOffset(float angleDegrees, float maxAngle)
    {
        if (maxAngle <= 0.0001f)
        {
            return 0.0f;
        }

        return Clamp(-angleDegrees / maxAngle, -1.0f, 1.0f);
    }

    BoneParserCLibVector3 BuildFaceForward(float angleDegrees)
    {
        constexpr float kPi = 3.14159265358979323846f;
        float radians = angleDegrees * (kPi / 180.0f);
        return NormalizeOrDefault({ std::sin(radians), 0.0f, std::cos(radians) }, Forward());
    }

    bool TryReadMeasuredAngleFromNose(
        const BoneParserCLibConfig& config,
        const BoneParserCLibPerson* person,
        SeatState& seatState,
        float& angleDegrees,
        float& confidence)
    {
        Vec2 leftShoulder;
        Vec2 rightShoulder;
        Vec2 nose;
        angleDegrees = 0.0f;
        confidence = 0.0f;
        if (person == nullptr ||
            !TryReadBodyJoint(person, BodyJointLeftShoulder, config.m_KeypointConfidenceThreshold, leftShoulder) ||
            !TryReadBodyJoint(person, BodyJointRightShoulder, config.m_KeypointConfidenceThreshold, rightShoulder) ||
            !TryReadBodyJoint(person, BodyJointNose, config.m_KeypointConfidenceThreshold, nose))
        {
            return false;
        }

        float shoulderWidth = Abs(rightShoulder.m_X - leftShoulder.m_X) + config.m_ShoulderWidthEpsilon;
        Vec2 ignored;
        if (TryReadBodyJoint(person, BodyJointLeftShoulder, config.m_MaxShoulderWidthUpdateConfidence, ignored) &&
            TryReadBodyJoint(person, BodyJointRightShoulder, config.m_MaxShoulderWidthUpdateConfidence, ignored))
        {
            seatState.m_MaxObservedShoulderWidth = std::max(seatState.m_MaxObservedShoulderWidth, shoulderWidth);
        }

        float shoulderMid = (leftShoulder.m_X + rightShoulder.m_X) * 0.5f;
        float noseOffset = (nose.m_X - shoulderMid) / shoulderWidth;
        float rawValue = ApplyShoulderTurnJitterDeadZone(
            Clamp(noseOffset * 2.5f, -1.0f, 1.0f),
            config.m_ShoulderTurnJitterDeadZone);
        float maxShoulderWidth = seatState.m_MaxObservedShoulderWidth > config.m_ShoulderWidthEpsilon
            ? seatState.m_MaxObservedShoulderWidth
            : shoulderWidth;
        float ratio = maxShoulderWidth > config.m_ShoulderWidthEpsilon
            ? Clamp01(shoulderWidth / maxShoulderWidth)
            : 1.0f;
        float angleFactor = 1.0f - ratio;
        float shoulderValue = Clamp(angleFactor * 1.5f, 0.0f, 1.0f) * Sign(noseOffset);
        float targetOffset = Lerp(rawValue, shoulderValue, 0.3f);
        targetOffset = ApplyAimResponseCurve(targetOffset, seatState.m_IsAimOutsideCenterDeadZone, config);
        if (ReadTurnDirectionMultiplier(config) < 0.0f)
        {
            targetOffset = -targetOffset;
        }

        angleDegrees = ConvertOffsetToAngle(
            targetOffset,
            ReadClampedMaxAngle(config),
            ReadTurnAmplifyFactor(config));
        confidence = Clamp01((leftShoulder.m_X != rightShoulder.m_X ? 1.0f : 0.0f) * person->m_BodyScore);
        return true;
    }

    void ApplyUnavailableResult(SeatState& seatState, BoneParserCLibPlayerResultOutput& result)
    {
        seatState.m_AimTrackingState = kAimStateNotTracked;
        seatState.m_HasAimOutput = false;
        seatState.m_LastAimAngleDegrees = 0.0f;
        seatState.m_LastAimAngularSpeedDegrees = 0.0f;
        seatState.m_CachedRotationOffset = 0.0f;
        seatState.m_IsAimOutsideCenterDeadZone = false;

        result.m_IsAimAvailable = 0;
        result.m_AimTrackingState = kAimStateNotTracked;
        result.m_AimConfidence = 0.0f;
        result.m_FaceForward = Forward();
        result.m_TurnAngleDegrees = 0.0f;
        result.m_TurnStrength = 0.0f;
        result.m_TurnSpeed = 0.0f;
    }

    void ApplyAimResult(
        const BoneParserCLibConfig& config,
        SeatState& seatState,
        BoneParserCLibPlayerResultOutput& result,
        int32_t trackingState,
        bool isAimAvailable,
        float angleDegrees,
        float confidence,
        float deltaTimeSeconds)
    {
        float maxAngle = ReadClampedMaxAngle(config);
        float previousAngle = seatState.m_HasAimOutput ? seatState.m_LastAimAngleDegrees : angleDegrees;
        float currentAngle = Clamp(angleDegrees, -maxAngle, maxAngle);
        float safeDeltaTimeSeconds = std::max(deltaTimeSeconds, 1.0f / 120.0f);

        seatState.m_AimTrackingState = trackingState;
        seatState.m_HasAimOutput = isAimAvailable;
        seatState.m_LastAimAngularSpeedDegrees = (currentAngle - previousAngle) / safeDeltaTimeSeconds;
        seatState.m_LastAimAngleDegrees = currentAngle;
        seatState.m_CachedRotationOffset = ConvertAngleToOffset(currentAngle, maxAngle);

        result.m_IsAimAvailable = isAimAvailable ? 1 : 0;
        result.m_AimTrackingState = trackingState;
        result.m_AimConfidence = confidence;
        result.m_MissingFrameCount = seatState.m_AimMissingFrameCount;
        result.m_FaceForward = BuildFaceForward(currentAngle);
        result.m_TurnAngleDegrees = currentAngle;
        result.m_TurnStrength = Clamp01(Abs(currentAngle) / std::max(maxAngle, config.m_ShoulderWidthEpsilon));
        result.m_TurnSpeed = Abs(currentAngle - previousAngle) / safeDeltaTimeSeconds;
    }

    void UpdateMissingAim(
        const BoneParserCLibConfig& config,
        SeatState& seatState,
        BoneParserCLibPlayerResultOutput* result,
        float deltaTimeSeconds)
    {
        if (result == nullptr)
        {
            return;
        }

        seatState.m_AimMissingFrameCount++;
        result->m_MissingFrameCount = seatState.m_AimMissingFrameCount;

        if (!seatState.m_HasAimOutput)
        {
            ApplyUnavailableResult(seatState, *result);
            return;
        }

        int32_t predictFrames = std::max(0, config.m_AimPredictMissingFrames);
        int32_t holdFrames = std::max(predictFrames, config.m_AimHoldMissingFrames);
        if (seatState.m_AimMissingFrameCount <= predictFrames)
        {
            float maxAngle = ReadClampedMaxAngle(config);
            float predictedAngle = Clamp(
                seatState.m_LastAimAngleDegrees + seatState.m_LastAimAngularSpeedDegrees * deltaTimeSeconds,
                -maxAngle,
                maxAngle);
            seatState.m_LastAimAngularSpeedDegrees *= Clamp01(config.m_AimPredictVelocityDamping);
            ApplyAimResult(
                config,
                seatState,
                *result,
                kAimStateMissing,
                true,
                predictedAngle,
                Clamp01(1.0f - static_cast<float>(seatState.m_AimMissingFrameCount) / static_cast<float>(std::max(1, holdFrames))),
                deltaTimeSeconds);
            return;
        }

        if (seatState.m_AimMissingFrameCount <= holdFrames)
        {
            seatState.m_LastAimAngularSpeedDegrees *= Clamp01(config.m_AimPredictVelocityDamping);
            ApplyAimResult(
                config,
                seatState,
                *result,
                kAimStateMissing,
                true,
                seatState.m_LastAimAngleDegrees,
                Clamp01(1.0f - static_cast<float>(seatState.m_AimMissingFrameCount) / static_cast<float>(std::max(1, holdFrames))),
                deltaTimeSeconds);
            return;
        }

        float returnSeconds = std::max(0.01f, config.m_AimReturnToForwardSeconds);
        float blend = Clamp01(deltaTimeSeconds / returnSeconds);
        float returnAngle = Lerp(seatState.m_LastAimAngleDegrees, 0.0f, blend);
        bool isAimAvailable = Abs(returnAngle) > 0.1f;
        ApplyAimResult(
            config,
            seatState,
            *result,
            isAimAvailable ? kAimStateReturning : kAimStateNotTracked,
            isAimAvailable,
            returnAngle,
            0.0f,
            deltaTimeSeconds);

        if (!isAimAvailable)
        {
            seatState.m_AimTrackingState = kAimStateNotTracked;
            seatState.m_HasAimOutput = false;
            seatState.m_LastAimAngularSpeedDegrees = 0.0f;
            seatState.m_IsAimOutsideCenterDeadZone = false;
        }
    }

    void ApplyTrackedAngle(
        const BoneParserCLibConfig& config,
        SeatState& seatState,
        BoneParserCLibPlayerResultOutput& result,
        float measuredAngleDegrees,
        float confidence,
        float deltaTimeSeconds)
    {
        float previousAngle = seatState.m_HasAimOutput ? seatState.m_LastAimAngleDegrees : measuredAngleDegrees;
        float outputAngle = measuredAngleDegrees;
        if (seatState.m_HasAimOutput)
        {
            float blendSpeed = seatState.m_AimTrackingState == kAimStateTracked
                ? config.m_RotationSmoothFactor
                : config.m_AimReconnectBlendFactor;
            outputAngle = Lerp(previousAngle, measuredAngleDegrees, Clamp01(deltaTimeSeconds * std::max(0.0f, blendSpeed)));
        }

        ApplyAimResult(config, seatState, result, kAimStateTracked, true, outputAngle, confidence, deltaTimeSeconds);
        seatState.m_AimMissingFrameCount = 0;
    }

    void UpdateTrackedAim(
        const BoneParserCLibConfig& config,
        const BoneParserCLibPerson* person,
        SeatState& seatState,
        BoneParserCLibPlayerResultOutput& result,
        float deltaTimeSeconds)
    {
        float measuredAngleDegrees = 0.0f;
        float confidence = 0.0f;
        if (TryReadMeasuredAngleFromNose(config, person, seatState, measuredAngleDegrees, confidence))
        {
            ApplyTrackedAngle(config, seatState, result, measuredAngleDegrees, confidence, deltaTimeSeconds);
            return;
        }

        UpdateMissingAim(config, seatState, &result, deltaTimeSeconds);
    }

    void NotifyPersonCandidate(SeatState& seatState, int32_t personId)
    {
        if (seatState.m_CurrentPersonId == personId)
        {
            seatState.m_AimCandidatePersonId = kInvalidPersonId;
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

    bool CanAcceptPerson(const BoneParserCLibConfig& config, const SeatState& seatState, int32_t personId)
    {
        if (seatState.m_CurrentPersonId == kInvalidPersonId ||
            seatState.m_CurrentPersonId == personId ||
            !seatState.m_HasAimOutput)
        {
            return true;
        }

        int32_t stableFrames = std::max(1, config.m_AimReconnectStableFrames);
        return seatState.m_AimCandidatePersonId == personId &&
            seatState.m_AimCandidateStableFrameCount >= stableFrames;
    }

    float ReadFrameTimeSeconds(const BoneParserCLibFrameInput* frameData)
    {
        if (frameData == nullptr || frameData->m_FrameTimeMs <= 0)
        {
            return 0.0f;
        }

        return static_cast<float>(frameData->m_FrameTimeMs) / 1000.0f;
    }

    float ReadDeltaTimeSeconds(const BoneParserCLibFrameInput* frameData, SeatState& seatState)
    {
        constexpr float kDefaultDeltaTimeSeconds = 1.0f / 30.0f;
        int64_t currentFrameTimeMs = frameData != nullptr ? frameData->m_FrameTimeMs : 0;
        float deltaTimeSeconds = kDefaultDeltaTimeSeconds;
        if (currentFrameTimeMs > 0 &&
            seatState.m_LastFrameTimeMs > 0 &&
            currentFrameTimeMs > seatState.m_LastFrameTimeMs)
        {
            deltaTimeSeconds = Clamp(static_cast<float>(currentFrameTimeMs - seatState.m_LastFrameTimeMs) / 1000.0f, 1.0f / 120.0f, 0.20f);
        }

        seatState.m_LastFrameTimeMs = currentFrameTimeMs;
        return deltaTimeSeconds;
    }

    int32_t CalculateRawPoseHints(RuntimeContext& context)
    {
        int32_t flags = 0;
        if (context.m_Person == nullptr)
        {
            return flags;
        }

        if (ReadIsLeftHandRaised(context))
        {
            flags |= kPoseFlagLeftHandRaise;
        }

        if (ReadIsRightHandRaised(context))
        {
            flags |= kPoseFlagRightHandRaise;
        }

        if (ReadIsCrossChest(context))
        {
            flags |= kPoseFlagCrossChest;
        }

        if (ReadIsHandsOnHip(context))
        {
            flags |= kPoseFlagHandsOnHip;
        }

        if (ReadIsCrouching(context))
        {
            flags |= kPoseFlagCrouch;
        }

        return flags;
    }

    int32_t ReadStablePoseHints(const BoneParserCLibConfig& config, PoseState& state, int32_t rawFlags)
    {
        if (state.m_RawPoseHints == rawFlags)
        {
            state.m_RawPoseStableFrameCount++;
        }
        else
        {
            state.m_RawPoseHints = rawFlags;
            state.m_RawPoseStableFrameCount = 1;
        }

        int32_t stableFrames = std::max(1, config.m_PoseStableFrames);
        return state.m_RawPoseStableFrameCount >= stableFrames
            ? rawFlags
            : state.m_LastPoseHints;
    }

    void UpdatePoseRecognizer(RuntimeContext& context)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        PoseState& state = context.m_SeatState->m_Pose;
        int32_t previousFlags = state.m_LastPoseHints;
        int32_t rawFlags = CalculateRawPoseHints(context);
        int32_t currentFlags = ReadStablePoseHints(config, state, rawFlags);
        context.m_Result->m_ActivePoseHints = currentFlags;

        AddBooleanGestureEvent(
            context,
            (previousFlags & kPoseFlagLeftHandRaise) == kPoseFlagLeftHandRaise,
            (currentFlags & kPoseFlagLeftHandRaise) == kPoseFlagLeftHandRaise,
            kGestureLeftHandRaisePose);
        AddBooleanGestureEvent(
            context,
            (previousFlags & kPoseFlagRightHandRaise) == kPoseFlagRightHandRaise,
            (currentFlags & kPoseFlagRightHandRaise) == kPoseFlagRightHandRaise,
            kGestureRightHandRaisePose);
        AddBooleanGestureEvent(
            context,
            (previousFlags & kPoseFlagCrossChest) == kPoseFlagCrossChest,
            (currentFlags & kPoseFlagCrossChest) == kPoseFlagCrossChest,
            kGestureCrossChestPose);
        AddBooleanGestureEvent(
            context,
            (previousFlags & kPoseFlagHandsOnHip) == kPoseFlagHandsOnHip,
            (currentFlags & kPoseFlagHandsOnHip) == kPoseFlagHandsOnHip,
            kGestureHandsOnHipPose);
        AddBooleanGestureEvent(
            context,
            (previousFlags & kPoseFlagCrouch) == kPoseFlagCrouch,
            (currentFlags & kPoseFlagCrouch) == kPoseFlagCrouch,
            kGestureCrouchPose);

        bool previousBothHands = (previousFlags & kPoseFlagLeftHandRaise) == kPoseFlagLeftHandRaise &&
            (previousFlags & kPoseFlagRightHandRaise) == kPoseFlagRightHandRaise;
        bool currentBothHands = (currentFlags & kPoseFlagLeftHandRaise) == kPoseFlagLeftHandRaise &&
            (currentFlags & kPoseFlagRightHandRaise) == kPoseFlagRightHandRaise;
        AddBooleanGestureEvent(context, previousBothHands, currentBothHands, kGestureBothHandsRaisePose);
        state.m_LastPoseHints = currentFlags;
    }

    bool TryReadAlternatingSwingFlowInput(
        RuntimeContext& context,
        float& shoulderWidth,
        bool& hasLeftWrist,
        float& leftRelativeWristY,
        bool& hasRightWrist,
        float& rightRelativeWristY)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        shoulderWidth = 0.0f;
        hasLeftWrist = false;
        leftRelativeWristY = 0.0f;
        hasRightWrist = false;
        rightRelativeWristY = 0.0f;

        Vec2 leftShoulder;
        Vec2 rightShoulder;
        if (context.m_Person == nullptr ||
            !TryReadBodyJoint(context.m_Person, BodyJointLeftShoulder, config.m_GestureKeypointMinConfidence, leftShoulder) ||
            !TryReadBodyJoint(context.m_Person, BodyJointRightShoulder, config.m_GestureKeypointMinConfidence, rightShoulder))
        {
            return false;
        }

        shoulderWidth = Abs(rightShoulder.m_X - leftShoulder.m_X) + config.m_ShoulderWidthEpsilon;
        Vec2 leftWrist;
        Vec2 rightWrist;
        hasLeftWrist = TryReadBodyJoint(context.m_Person, BodyJointLeftWrist, config.m_GestureKeypointMinConfidence, leftWrist);
        hasRightWrist = TryReadBodyJoint(context.m_Person, BodyJointRightWrist, config.m_GestureKeypointMinConfidence, rightWrist);
        if (hasLeftWrist)
        {
            leftRelativeWristY = leftWrist.m_Y - leftShoulder.m_Y;
        }

        if (hasRightWrist)
        {
            rightRelativeWristY = rightWrist.m_Y - rightShoulder.m_Y;
        }

        return true;
    }

    bool TryReadSwingFlowInput(
        RuntimeContext& context,
        float& shoulderWidth,
        bool& hasLeftWrist,
        Vec2& leftWrist,
        bool& hasRightWrist,
        Vec2& rightWrist)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        shoulderWidth = 0.0f;
        hasLeftWrist = false;
        leftWrist = {};
        hasRightWrist = false;
        rightWrist = {};

        Vec2 leftShoulder;
        Vec2 rightShoulder;
        if (context.m_Person == nullptr ||
            !TryReadBodyJoint(context.m_Person, BodyJointLeftShoulder, config.m_GestureKeypointMinConfidence, leftShoulder) ||
            !TryReadBodyJoint(context.m_Person, BodyJointRightShoulder, config.m_GestureKeypointMinConfidence, rightShoulder))
        {
            return false;
        }

        shoulderWidth = Abs(rightShoulder.m_X - leftShoulder.m_X) + config.m_ShoulderWidthEpsilon;
        hasLeftWrist = TryReadBodyJoint(context.m_Person, BodyJointLeftWrist, config.m_GestureKeypointMinConfidence, leftWrist);
        hasRightWrist = TryReadBodyJoint(context.m_Person, BodyJointRightWrist, config.m_GestureKeypointMinConfidence, rightWrist);
        return true;
    }

    void RecordSwing(AlternatingSwingState& state, int32_t side, int32_t frameSerial)
    {
        state.m_LastAlternatingSideMarker = side;
        state.m_LastAlternatingFrameSerial = frameSerial;
    }

    void SubmitSwing(AlternatingSwingState& state, bool leftSwingDetected, bool rightSwingDetected, float frameTimeSeconds, int32_t frameSerial)
    {
        if (leftSwingDetected)
        {
            state.m_LastLeftAlternatingTimeSeconds = frameTimeSeconds;
            RecordSwing(state, kLeftSide, frameSerial);
        }

        if (rightSwingDetected)
        {
            state.m_LastRightAlternatingTimeSeconds = frameTimeSeconds;
            RecordSwing(state, kRightSide, frameSerial);
        }
    }

    bool EvaluateAlternatingHandSwing(
        AlternatingSwingHandState& handState,
        float relativeWristY,
        float shoulderWidth,
        float deltaTimeSeconds,
        const BoneParserCLibConfig& config)
    {
        if (!handState.m_HasLastRelativeWristY)
        {
            handState.m_HasLastRelativeWristY = true;
            handState.m_LastRelativeWristY = relativeWristY;
            handState.ResetStroke(relativeWristY);
            return false;
        }

        float previousRelativeWristY = handState.m_LastRelativeWristY;
        float frameDelta = relativeWristY - previousRelativeWristY;
        handState.m_LastRelativeWristY = relativeWristY;

        float directionNoise = shoulderWidth * std::max(0.0f, config.m_AlternatingSwingDirectionNoiseRatio);
        int32_t minDirectionalFrames = std::max(2, config.m_AlternatingSwingMinDirectionalFrames);
        if (Abs(frameDelta) < directionNoise)
        {
            if (handState.m_StrokeDirection != 0)
            {
                handState.m_IdleFrameCount++;
                handState.m_StrokeDurationSeconds += deltaTimeSeconds;
                if (handState.m_DirectionalFrameCount < minDirectionalFrames || handState.m_IdleFrameCount > 1)
                {
                    handState.ResetStroke(relativeWristY);
                }
            }

            return false;
        }

        int32_t currentDirection = frameDelta > 0.0f ? 1 : -1;
        if (handState.m_StrokeDirection != currentDirection)
        {
            handState.m_StrokeStartRelativeWristY = previousRelativeWristY;
            handState.m_StrokeDurationSeconds = deltaTimeSeconds;
            handState.m_StrokeDirection = currentDirection;
            handState.m_DirectionalFrameCount = 1;
            handState.m_IdleFrameCount = 0;
        }
        else
        {
            handState.m_StrokeDurationSeconds += deltaTimeSeconds;
            handState.m_DirectionalFrameCount++;
            handState.m_IdleFrameCount = 0;
        }

        float verticalDistance = Abs(relativeWristY - handState.m_StrokeStartRelativeWristY);
        float verticalSpeed = verticalDistance / std::max(handState.m_StrokeDurationSeconds, 1.0f / 120.0f);
        bool isCompleted =
            handState.m_DirectionalFrameCount >= minDirectionalFrames &&
            verticalDistance >= shoulderWidth * config.m_AlternatingSwingMinVerticalDistanceRatio &&
            verticalSpeed >= shoulderWidth * config.m_AlternatingSwingSpeedRatioPerSecond;
        if (isCompleted)
        {
            handState.ResetStroke(relativeWristY);
        }

        return isCompleted;
    }

    bool EvaluateAlternatingSwing(
        RuntimeContext& context,
        AlternatingSwingState& state,
        float shoulderWidth,
        bool hasLeftWrist,
        float leftRelativeWristY,
        bool hasRightWrist,
        float rightRelativeWristY,
        bool& leftSwingDetected,
        bool& rightSwingDetected)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        leftSwingDetected = false;
        rightSwingDetected = false;
        if (context.m_DeltaTimeSeconds <= 0.0f)
        {
            return false;
        }

        if (hasLeftWrist)
        {
            bool leftMotionCompleted = EvaluateAlternatingHandSwing(
                state.m_LeftHand,
                leftRelativeWristY,
                shoulderWidth,
                context.m_DeltaTimeSeconds,
                config);
            leftSwingDetected = leftMotionCompleted &&
                context.m_FrameTimeSeconds - state.m_LastLeftAlternatingTimeSeconds >= config.m_AlternatingSwingCooldownSeconds;
        }

        if (hasRightWrist)
        {
            bool rightMotionCompleted = EvaluateAlternatingHandSwing(
                state.m_RightHand,
                rightRelativeWristY,
                shoulderWidth,
                context.m_DeltaTimeSeconds,
                config);
            rightSwingDetected = rightMotionCompleted &&
                context.m_FrameTimeSeconds - state.m_LastRightAlternatingTimeSeconds >= config.m_AlternatingSwingCooldownSeconds;
        }

        if (leftSwingDetected == rightSwingDetected)
        {
            return false;
        }

        int32_t currentSideMarker = leftSwingDetected ? kLeftSide : kRightSide;
        bool hasPreviousSide = state.m_LastAlternatingSideMarker != 0;
        bool isOppositeSide = hasPreviousSide && state.m_LastAlternatingSideMarker != currentSideMarker;
        int32_t maxWindowFrames = std::max(1, config.m_AlternatingSwingWindowFrames);
        int32_t frameSerial = ReadFrameSerial(context);
        bool isInsideWindow =
            frameSerial <= 0 ||
            state.m_LastAlternatingFrameSerial <= 0 ||
            frameSerial - state.m_LastAlternatingFrameSerial <= maxWindowFrames;
        if (!isOppositeSide || !isInsideWindow)
        {
            SubmitSwing(state, leftSwingDetected, rightSwingDetected, context.m_FrameTimeSeconds, frameSerial);
            return false;
        }

        return true;
    }

    void UpdateAlternatingHandTracking(AlternatingSwingHandState& handState, bool hasWrist)
    {
        if (!hasWrist)
        {
            handState.Reset();
        }
    }

    void SynchronizeAlternatingHandSample(
        AlternatingSwingHandState& handState,
        bool hasWrist,
        float relativeWristY)
    {
        if (!hasWrist)
        {
            handState.Reset();
            return;
        }

        handState.m_HasLastRelativeWristY = true;
        handState.m_LastRelativeWristY = relativeWristY;
        handState.ResetStroke(relativeWristY);
    }

    void UpdateAlternatingSwingRecognizer(RuntimeContext& context)
    {
        AlternatingSwingState& state = context.m_SeatState->m_AlternatingSwing;
        float shoulderWidth = 0.0f;
        bool hasLeftWrist = false;
        float leftRelativeWristY = 0.0f;
        bool hasRightWrist = false;
        float rightRelativeWristY = 0.0f;
        if (!TryReadAlternatingSwingFlowInput(
                context,
                shoulderWidth,
                hasLeftWrist,
                leftRelativeWristY,
                hasRightWrist,
                rightRelativeWristY))
        {
            state.Reset();
            return;
        }

        bool leftSwingDetected = false;
        bool rightSwingDetected = false;
        bool shouldEmitTrigger = false;
        if (!state.m_IsPending)
        {
            shouldEmitTrigger = EvaluateAlternatingSwing(
                context,
                state,
                shoulderWidth,
                hasLeftWrist,
                leftRelativeWristY,
                hasRightWrist,
                rightRelativeWristY,
                leftSwingDetected,
                rightSwingDetected);
        }
        else
        {
            SynchronizeAlternatingHandSample(state.m_LeftHand, hasLeftWrist, leftRelativeWristY);
            SynchronizeAlternatingHandSample(state.m_RightHand, hasRightWrist, rightRelativeWristY);
        }

        if (shouldEmitTrigger)
        {
            BoneParserCLibActionEventOutput* actionEvent = AddGestureEvent(context, kGestureAlternatingSwingFlow, kPhaseTrigger);
            if (actionEvent != nullptr && actionEvent->m_RequiresConsumeResult != 0)
            {
                state.m_IsPending = true;
                state.m_ActionEventId = actionEvent->m_ActionEventId;
                state.m_LeftSwingDetected = leftSwingDetected;
                state.m_RightSwingDetected = rightSwingDetected;
                state.m_FrameTimeSeconds = context.m_FrameTimeSeconds;
                state.m_FrameSerial = ReadFrameSerial(context);
            }
            else if (actionEvent != nullptr)
            {
                SubmitSwing(state, leftSwingDetected, rightSwingDetected, context.m_FrameTimeSeconds, ReadFrameSerial(context));
            }
        }

        UpdateAlternatingHandTracking(state.m_LeftHand, hasLeftWrist);
        UpdateAlternatingHandTracking(state.m_RightHand, hasRightWrist);
    }

    bool ApplyAlternatingSwingConsumeResult(SeatState& seatState, int32_t actionEventId, int32_t resultType)
    {
        AlternatingSwingState& state = seatState.m_AlternatingSwing;
        if (!state.m_IsPending || state.m_ActionEventId != actionEventId)
        {
            return false;
        }

        if (resultType == kConsumeResultAccept)
        {
            SubmitSwing(
                state,
                state.m_LeftSwingDetected,
                state.m_RightSwingDetected,
                state.m_FrameTimeSeconds,
                state.m_FrameSerial);
        }

        state.ResetPending();
        return true;
    }

    bool TryReadLargeAlternatingSwingFlowInput(
        RuntimeContext& context,
        float& shoulderWidth,
        float& leftTorsoHeight,
        float& leftWristHeight,
        float& rightTorsoHeight,
        float& rightWristHeight)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        shoulderWidth = 0.0f;
        leftTorsoHeight = 0.0f;
        leftWristHeight = 0.0f;
        rightTorsoHeight = 0.0f;
        rightWristHeight = 0.0f;

        Vec2 leftShoulder;
        Vec2 rightShoulder;
        Vec2 leftHip;
        Vec2 rightHip;
        Vec2 leftWrist;
        Vec2 rightWrist;
        if (context.m_Person == nullptr ||
            !TryReadBodyJoint(context.m_Person, BodyJointLeftShoulder, config.m_GestureKeypointMinConfidence, leftShoulder) ||
            !TryReadBodyJoint(context.m_Person, BodyJointRightShoulder, config.m_GestureKeypointMinConfidence, rightShoulder) ||
            !TryReadBodyJoint(context.m_Person, BodyJointLeftHip, config.m_GestureKeypointMinConfidence, leftHip) ||
            !TryReadBodyJoint(context.m_Person, BodyJointRightHip, config.m_GestureKeypointMinConfidence, rightHip) ||
            !TryReadBodyJoint(context.m_Person, BodyJointLeftWrist, config.m_GestureKeypointMinConfidence, leftWrist) ||
            !TryReadBodyJoint(context.m_Person, BodyJointRightWrist, config.m_GestureKeypointMinConfidence, rightWrist))
        {
            return false;
        }

        shoulderWidth = Abs(rightShoulder.m_X - leftShoulder.m_X) + config.m_ShoulderWidthEpsilon;
        leftTorsoHeight = Abs(leftHip.m_Y - leftShoulder.m_Y);
        rightTorsoHeight = Abs(rightHip.m_Y - rightShoulder.m_Y);
        if (leftTorsoHeight <= config.m_ShoulderWidthEpsilon || rightTorsoHeight <= config.m_ShoulderWidthEpsilon)
        {
            return false;
        }

        leftWristHeight = leftHip.m_Y - leftWrist.m_Y;
        rightWristHeight = rightHip.m_Y - rightWrist.m_Y;
        return true;
    }

    bool EvaluateLargeAlternatingHandSwing(
        AlternatingSwingHandState& handState,
        float wristHeight,
        float torsoHeight,
        float shoulderWidth,
        float deltaTimeSeconds,
        const BoneParserCLibConfig& config)
    {
        if (!handState.m_HasLastRelativeWristY)
        {
            SynchronizeAlternatingHandSample(handState, true, wristHeight);
            return false;
        }

        float previousWristHeight = handState.m_LastRelativeWristY;
        float frameDelta = wristHeight - previousWristHeight;
        handState.m_LastRelativeWristY = wristHeight;

        float directionNoise = shoulderWidth * std::max(0.0f, config.m_LargeAlternatingSwingDirectionNoiseRatio);
        int32_t minDirectionalFrames = std::max(2, config.m_LargeAlternatingSwingMinDirectionalFrames);
        if (frameDelta <= directionNoise)
        {
            if (frameDelta < -directionNoise)
            {
                handState.ResetStroke(wristHeight);
                return false;
            }

            if (handState.m_DirectionalFrameCount > 0)
            {
                handState.m_IdleFrameCount++;
                handState.m_StrokeDurationSeconds += deltaTimeSeconds;
                if (handState.m_DirectionalFrameCount < minDirectionalFrames || handState.m_IdleFrameCount > 1)
                {
                    handState.ResetStroke(wristHeight);
                }
            }
            else
            {
                handState.ResetStroke(wristHeight);
            }

            return false;
        }

        if (handState.m_StrokeDirection != 1)
        {
            handState.m_StrokeStartRelativeWristY = previousWristHeight;
            handState.m_StrokeDurationSeconds = deltaTimeSeconds;
            handState.m_StrokeDirection = 1;
            handState.m_DirectionalFrameCount = 1;
            handState.m_IdleFrameCount = 0;
        }
        else
        {
            handState.m_StrokeDurationSeconds += deltaTimeSeconds;
            handState.m_DirectionalFrameCount++;
            handState.m_IdleFrameCount = 0;
        }

        float verticalDistance = wristHeight - handState.m_StrokeStartRelativeWristY;
        float verticalSpeed = verticalDistance / std::max(handState.m_StrokeDurationSeconds, 1.0f / 120.0f);
        bool isCompleted =
            handState.m_DirectionalFrameCount >= minDirectionalFrames &&
            verticalDistance >= torsoHeight * std::max(0.0f, config.m_LargeAlternatingSwingMinTorsoDistanceRatio) &&
            verticalSpeed >= shoulderWidth * config.m_LargeAlternatingSwingSpeedRatioPerSecond;
        if (isCompleted)
        {
            handState.ResetStroke(wristHeight);
        }

        return isCompleted;
    }

    bool EvaluateLargeAlternatingSwing(
        RuntimeContext& context,
        AlternatingSwingState& state,
        float shoulderWidth,
        float leftTorsoHeight,
        float leftWristHeight,
        float rightTorsoHeight,
        float rightWristHeight,
        bool& leftSwingDetected,
        bool& rightSwingDetected)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        leftSwingDetected = false;
        rightSwingDetected = false;
        if (context.m_DeltaTimeSeconds <= 0.0f)
        {
            return false;
        }

        bool leftMotionCompleted = EvaluateLargeAlternatingHandSwing(
            state.m_LeftHand,
            leftWristHeight,
            leftTorsoHeight,
            shoulderWidth,
            context.m_DeltaTimeSeconds,
            config);
        leftSwingDetected = leftMotionCompleted &&
            context.m_FrameTimeSeconds - state.m_LastLeftAlternatingTimeSeconds >= config.m_LargeAlternatingSwingCooldownSeconds;

        bool rightMotionCompleted = EvaluateLargeAlternatingHandSwing(
            state.m_RightHand,
            rightWristHeight,
            rightTorsoHeight,
            shoulderWidth,
            context.m_DeltaTimeSeconds,
            config);
        rightSwingDetected = rightMotionCompleted &&
            context.m_FrameTimeSeconds - state.m_LastRightAlternatingTimeSeconds >= config.m_LargeAlternatingSwingCooldownSeconds;

        if (leftSwingDetected == rightSwingDetected)
        {
            return false;
        }

        int32_t currentSideMarker = leftSwingDetected ? kLeftSide : kRightSide;
        bool hasPreviousSide = state.m_LastAlternatingSideMarker != 0;
        bool isOppositeSide = hasPreviousSide && state.m_LastAlternatingSideMarker != currentSideMarker;
        int32_t maxWindowFrames = std::max(1, config.m_LargeAlternatingSwingWindowFrames);
        int32_t frameSerial = ReadFrameSerial(context);
        bool isInsideWindow =
            frameSerial <= 0 ||
            state.m_LastAlternatingFrameSerial <= 0 ||
            frameSerial - state.m_LastAlternatingFrameSerial <= maxWindowFrames;
        if (!isOppositeSide || !isInsideWindow)
        {
            SubmitSwing(state, leftSwingDetected, rightSwingDetected, context.m_FrameTimeSeconds, frameSerial);
            return false;
        }

        return true;
    }

    void UpdateLargeAlternatingSwingRecognizer(RuntimeContext& context)
    {
        AlternatingSwingState& state = context.m_SeatState->m_LargeAlternatingSwing;
        float shoulderWidth = 0.0f;
        float leftTorsoHeight = 0.0f;
        float leftWristHeight = 0.0f;
        float rightTorsoHeight = 0.0f;
        float rightWristHeight = 0.0f;
        if (!TryReadLargeAlternatingSwingFlowInput(
                context,
                shoulderWidth,
                leftTorsoHeight,
                leftWristHeight,
                rightTorsoHeight,
                rightWristHeight))
        {
            state.Reset();
            return;
        }

        bool leftSwingDetected = false;
        bool rightSwingDetected = false;
        bool shouldEmitTrigger = false;
        if (!state.m_IsPending)
        {
            shouldEmitTrigger = EvaluateLargeAlternatingSwing(
                context,
                state,
                shoulderWidth,
                leftTorsoHeight,
                leftWristHeight,
                rightTorsoHeight,
                rightWristHeight,
                leftSwingDetected,
                rightSwingDetected);
        }
        else
        {
            SynchronizeAlternatingHandSample(state.m_LeftHand, true, leftWristHeight);
            SynchronizeAlternatingHandSample(state.m_RightHand, true, rightWristHeight);
        }

        if (!shouldEmitTrigger)
        {
            return;
        }

        BoneParserCLibActionEventOutput* actionEvent =
            AddGestureEvent(context, kGestureLargeAlternatingSwingFlow, kPhaseTrigger);
        if (actionEvent != nullptr && actionEvent->m_RequiresConsumeResult != 0)
        {
            state.m_IsPending = true;
            state.m_ActionEventId = actionEvent->m_ActionEventId;
            state.m_LeftSwingDetected = leftSwingDetected;
            state.m_RightSwingDetected = rightSwingDetected;
            state.m_FrameTimeSeconds = context.m_FrameTimeSeconds;
            state.m_FrameSerial = ReadFrameSerial(context);
        }
        else if (actionEvent != nullptr)
        {
            SubmitSwing(state, leftSwingDetected, rightSwingDetected, context.m_FrameTimeSeconds, ReadFrameSerial(context));
        }
    }

    bool ApplyLargeAlternatingSwingConsumeResult(SeatState& seatState, int32_t actionEventId, int32_t resultType)
    {
        AlternatingSwingState& state = seatState.m_LargeAlternatingSwing;
        if (!state.m_IsPending || state.m_ActionEventId != actionEventId)
        {
            return false;
        }

        if (resultType == kConsumeResultAccept)
        {
            SubmitSwing(
                state,
                state.m_LeftSwingDetected,
                state.m_RightSwingDetected,
                state.m_FrameTimeSeconds,
                state.m_FrameSerial);
        }

        state.ResetPending();
        return true;
    }

    bool ReadSingleHandOverheadReleaseCompleted(
        bool hasLastWrist,
        Vec2 lastWrist,
        Vec2 wrist,
        Vec2 readyWrist,
        float deltaTimeSeconds,
        float releaseThreshold,
        float releaseDistanceThreshold,
        float releaseEndY)
    {
        if (!hasLastWrist || deltaTimeSeconds <= 0.0f)
        {
            return false;
        }

        float downSpeed = std::max(0.0f, (wrist.m_Y - lastWrist.m_Y) / deltaTimeSeconds);
        float downDistance = std::max(0.0f, wrist.m_Y - readyWrist.m_Y);
        return downSpeed >= releaseThreshold &&
            downDistance >= releaseDistanceThreshold &&
            wrist.m_Y >= releaseEndY;
    }

    bool ReadOverheadReleaseWindowTimeout(const BoneParserCLibConfig& config, const OverheadPressReleaseState& state)
    {
        return state.m_IsReleaseCollecting &&
            state.m_ReleaseFrameCount > std::max(1, config.m_OverheadPressReleaseWindowFrames);
    }

    void EvaluateOverheadFlow(
        RuntimeContext& context,
        OverheadPressReleaseState& state,
        float shoulderWidth,
        bool hasLeftWrist,
        Vec2 leftWrist,
        bool hasRightWrist,
        Vec2 rightWrist,
        bool& isActive,
        bool& shouldEmitTrigger)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        isActive = false;
        shouldEmitTrigger = false;
        bool hasProcessGestureBinding = HasRecognizableActionBinding(context, kGestureOverheadPressReleaseFlow);
        if ((context.m_SeatDefinition == nullptr || context.m_SeatDefinition->m_IsProcessGestureEnabled == 0) && !hasProcessGestureBinding)
        {
            state.ResetReadyState();
            return;
        }

        if (context.m_FrameTimeSeconds > 0.0f && context.m_FrameTimeSeconds < state.m_CooldownUntilSeconds)
        {
            return;
        }

        float headTopY = 0.0f;
        if (!TryReadHeadTop(config, context.m_Person, headTopY))
        {
            state.ResetReadyState();
            return;
        }

        bool hasBothWrist = hasLeftWrist && hasRightWrist;
        if (!hasBothWrist)
        {
            if (!state.m_IsReady)
            {
                state.ResetReadyState();
                return;
            }

            state.StartReleaseWindow();
            state.m_ReleaseFrameCount++;
            if (ReadOverheadReleaseWindowTimeout(config, state))
            {
                state.ResetReadyState();
            }
            else
            {
                isActive = true;
            }

            return;
        }

        float overheadThreshold = headTopY - shoulderWidth * config.m_OverheadPressHeadMarginRatio;
        bool bothOverhead = leftWrist.m_Y <= overheadThreshold && rightWrist.m_Y <= overheadThreshold;
        isActive = bothOverhead || state.m_IsReady;
        if (!state.m_IsReady)
        {
            if (bothOverhead)
            {
                state.m_ReadyFrameCount++;
                if (state.m_ReadyFrameCount >= config.m_OverheadPressReadyFrames)
                {
                    state.m_IsReady = true;
                    state.m_ReadyLeftWrist = leftWrist;
                    state.m_ReadyRightWrist = rightWrist;
                    isActive = true;
                }
            }
            else
            {
                state.m_ReadyFrameCount = 0;
            }

            return;
        }

        if (bothOverhead)
        {
            state.m_ReadyFrameCount++;
            state.ResetReleaseWindow();
            isActive = true;
            return;
        }

        state.StartReleaseWindow();
        state.m_ReleaseFrameCount++;
        if (context.m_DeltaTimeSeconds <= 0.0f)
        {
            if (ReadOverheadReleaseWindowTimeout(config, state))
            {
                state.ResetReadyState();
                isActive = false;
            }
            else
            {
                isActive = true;
            }

            return;
        }

        float releaseThreshold = shoulderWidth * config.m_OverheadPressReleaseSpeedRatio;
        float releaseDistanceThreshold = shoulderWidth * config.m_OverheadPressMinReleaseDistanceRatio;
        float releaseEndY = headTopY + shoulderWidth * config.m_OverheadPressReleaseEndBelowHeadRatio;
        if (!state.m_HasLeftRelease &&
            ReadSingleHandOverheadReleaseCompleted(
                state.m_HasLastLeftWrist,
                state.m_LastLeftWrist,
                leftWrist,
                state.m_ReadyLeftWrist,
                context.m_DeltaTimeSeconds,
                releaseThreshold,
                releaseDistanceThreshold,
                releaseEndY))
        {
            state.m_HasLeftRelease = true;
        }

        if (!state.m_HasRightRelease &&
            ReadSingleHandOverheadReleaseCompleted(
                state.m_HasLastRightWrist,
                state.m_LastRightWrist,
                rightWrist,
                state.m_ReadyRightWrist,
                context.m_DeltaTimeSeconds,
                releaseThreshold,
                releaseDistanceThreshold,
                releaseEndY))
        {
            state.m_HasRightRelease = true;
        }

        if (state.m_HasLeftRelease && state.m_HasRightRelease)
        {
            shouldEmitTrigger = true;
            isActive = false;
        }
        else if (ReadOverheadReleaseWindowTimeout(config, state))
        {
            state.ResetReadyState();
            isActive = false;
        }
        else
        {
            isActive = true;
        }
    }

    void SubmitOverheadAccepted(const BoneParserCLibConfig& config, OverheadPressReleaseState& state, float frameTimeSeconds)
    {
        state.ResetReadyState();
        state.m_CooldownUntilSeconds = frameTimeSeconds + config.m_OverheadPressCooldownSeconds;
        state.m_LastActive = false;
    }

    void SubmitOverheadRetry(OverheadPressReleaseState& state)
    {
        state.m_IsReady = true;
        state.ResetReleaseWindow();
        state.m_LastActive = false;
    }

    void SubmitOverheadBlocked(OverheadPressReleaseState& state)
    {
        state.ResetReadyState();
        state.m_LastActive = false;
    }

    void UpdateOverheadPressReleaseRecognizer(RuntimeContext& context)
    {
        OverheadPressReleaseState& state = context.m_SeatState->m_OverheadPressRelease;
        bool previousActive = state.m_LastActive;
        float shoulderWidth = 0.0f;
        bool hasLeftWrist = false;
        Vec2 leftWrist;
        bool hasRightWrist = false;
        Vec2 rightWrist;
        if (!TryReadSwingFlowInput(context, shoulderWidth, hasLeftWrist, leftWrist, hasRightWrist, rightWrist))
        {
            if (previousActive)
            {
                AddGestureEvent(context, kGestureOverheadPressReleaseFlow, kPhaseEnd);
                state.m_LastActive = false;
            }
            state.Reset();
            return;
        }

        bool currentActive = state.m_LastActive;
        bool shouldEmitTrigger = false;
        if (!state.m_IsPending)
        {
            EvaluateOverheadFlow(
                context,
                state,
                shoulderWidth,
                hasLeftWrist,
                leftWrist,
                hasRightWrist,
                rightWrist,
                currentActive,
                shouldEmitTrigger);
        }

        AddBooleanGestureEvent(context, previousActive, currentActive, kGestureOverheadPressReleaseFlow);
        state.m_LastActive = currentActive;

        if (shouldEmitTrigger)
        {
            BoneParserCLibActionEventOutput* actionEvent = AddGestureEvent(context, kGestureOverheadPressReleaseFlow, kPhaseTrigger);
            if (actionEvent == nullptr)
            {
                SubmitOverheadBlocked(state);
            }
            else if (actionEvent->m_RequiresConsumeResult != 0)
            {
                state.m_IsPending = true;
                state.m_ActionEventId = actionEvent->m_ActionEventId;
                state.m_FrameTimeSeconds = context.m_FrameTimeSeconds;
            }
            else
            {
                SubmitOverheadAccepted(ReadConfig(context.m_Config), state, context.m_FrameTimeSeconds);
            }
        }

        if (hasLeftWrist)
        {
            state.m_LastLeftWrist = leftWrist;
            state.m_HasLastLeftWrist = true;
        }
        else
        {
            state.m_HasLastLeftWrist = false;
        }

        if (hasRightWrist)
        {
            state.m_LastRightWrist = rightWrist;
            state.m_HasLastRightWrist = true;
        }
        else
        {
            state.m_HasLastRightWrist = false;
        }
    }

    bool ApplyOverheadConsumeResult(const BoneParserCLibConfig& config, SeatState& seatState, int32_t actionEventId, int32_t resultType)
    {
        OverheadPressReleaseState& state = seatState.m_OverheadPressRelease;
        if (!state.m_IsPending || state.m_ActionEventId != actionEventId)
        {
            return false;
        }

        if (resultType == kConsumeResultAccept)
        {
            SubmitOverheadAccepted(config, state, state.m_FrameTimeSeconds);
        }
        else if (resultType == kConsumeResultRetry)
        {
            SubmitOverheadRetry(state);
        }
        else
        {
            SubmitOverheadBlocked(state);
        }

        state.ResetPending();
        return true;
    }

    bool TryReadCrossChestExpandInput(RuntimeContext& context, float& shoulderWidth, Vec2& leftWrist, Vec2& rightWrist)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        shoulderWidth = 0.0f;
        leftWrist = {};
        rightWrist = {};
        Vec2 leftShoulder;
        Vec2 rightShoulder;
        if (context.m_Person == nullptr ||
            !TryReadBodyJoint(context.m_Person, BodyJointLeftShoulder, config.m_GestureKeypointMinConfidence, leftShoulder) ||
            !TryReadBodyJoint(context.m_Person, BodyJointRightShoulder, config.m_GestureKeypointMinConfidence, rightShoulder) ||
            !TryReadBodyJoint(context.m_Person, BodyJointLeftWrist, config.m_GestureKeypointMinConfidence, leftWrist) ||
            !TryReadBodyJoint(context.m_Person, BodyJointRightWrist, config.m_GestureKeypointMinConfidence, rightWrist))
        {
            return false;
        }

        shoulderWidth = Abs(rightShoulder.m_X - leftShoulder.m_X) + config.m_ShoulderWidthEpsilon;
        return true;
    }

    bool EvaluateCrossChestExpand(RuntimeContext& context, CrossChestExpandState& state, float shoulderWidth, Vec2 leftWrist, Vec2 rightWrist)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        if (!HasRecognizableActionBinding(context, kGestureCrossChestExpandFlow))
        {
            state.ResetReadyState();
            return false;
        }

        if (context.m_FrameTimeSeconds > 0.0f && context.m_FrameTimeSeconds < state.m_CooldownUntilSeconds)
        {
            return false;
        }

        bool isCrossChest = ReadIsCrossChest(context);
        float wristDistance = Abs(leftWrist.m_X - rightWrist.m_X);
        if (!state.m_IsReady)
        {
            if (isCrossChest)
            {
                state.m_ReadyFrameCount++;
                if (state.m_ReadyFrameCount >= std::max(1, config.m_CrossChestExpandReadyFrames))
                {
                    state.m_IsReady = true;
                    state.m_ReadyWristDistance = wristDistance;
                    state.m_LastWristDistance = wristDistance;
                    state.m_HasLastWristDistance = true;
                    state.ResetReleaseWindow();
                }
            }
            else
            {
                state.m_ReadyFrameCount = 0;
            }

            return false;
        }

        if (isCrossChest)
        {
            state.ResetReleaseWindow();
            state.m_LastWristDistance = wristDistance;
            state.m_HasLastWristDistance = true;
            return false;
        }

        state.StartReleaseWindow();
        state.m_ReleaseFrameCount++;

        if (!state.m_HasLastWristDistance || context.m_DeltaTimeSeconds <= 0.0f)
        {
            state.m_LastWristDistance = wristDistance;
            state.m_HasLastWristDistance = true;
            if (state.m_IsReleaseCollecting &&
                state.m_ReleaseFrameCount > std::max(1, config.m_CrossChestExpandReleaseWindowFrames))
            {
                state.ResetReadyState();
            }
            return false;
        }

        float expandSpeedThreshold = shoulderWidth * config.m_CrossChestExpandSpeedRatioPerSecond;
        float expandDistanceThreshold = shoulderWidth * config.m_CrossChestExpandMinDistanceRatio;
        float expandSpeed = std::max(0.0f, (wristDistance - state.m_LastWristDistance) / context.m_DeltaTimeSeconds);
        float expandDistance = std::max(0.0f, wristDistance - state.m_ReadyWristDistance);
        state.m_LastWristDistance = wristDistance;

        if (expandSpeed >= expandSpeedThreshold && expandDistance >= expandDistanceThreshold)
        {
            return true;
        }

        if (state.m_IsReleaseCollecting &&
            state.m_ReleaseFrameCount > std::max(1, config.m_CrossChestExpandReleaseWindowFrames))
        {
            state.ResetReadyState();
        }

        return false;
    }

    void SubmitCrossChestExpandAccepted(const BoneParserCLibConfig& config, CrossChestExpandState& state, float frameTimeSeconds)
    {
        state.ResetReadyState();
        state.m_CooldownUntilSeconds = frameTimeSeconds + config.m_CrossChestExpandCooldownSeconds;
    }

    void SubmitCrossChestExpandRetry(CrossChestExpandState& state)
    {
        state.m_IsReady = true;
        state.ResetReleaseWindow();
    }

    void SubmitCrossChestExpandBlocked(CrossChestExpandState& state)
    {
        state.ResetReadyState();
    }

    void UpdateCrossChestExpandRecognizer(RuntimeContext& context)
    {
        CrossChestExpandState& state = context.m_SeatState->m_CrossChestExpand;
        if (state.m_IsPending)
        {
            return;
        }

        float shoulderWidth = 0.0f;
        Vec2 leftWrist;
        Vec2 rightWrist;
        if (!TryReadCrossChestExpandInput(context, shoulderWidth, leftWrist, rightWrist))
        {
            state.Reset();
            return;
        }

        if (!EvaluateCrossChestExpand(context, state, shoulderWidth, leftWrist, rightWrist))
        {
            return;
        }

        BoneParserCLibActionEventOutput* actionEvent = AddGestureEvent(context, kGestureCrossChestExpandFlow, kPhaseTrigger);
        if (actionEvent == nullptr)
        {
            SubmitCrossChestExpandBlocked(state);
        }
        else if (actionEvent->m_RequiresConsumeResult != 0)
        {
            state.m_IsPending = true;
            state.m_ActionEventId = actionEvent->m_ActionEventId;
            state.m_FrameTimeSeconds = context.m_FrameTimeSeconds;
        }
        else
        {
            SubmitCrossChestExpandAccepted(ReadConfig(context.m_Config), state, context.m_FrameTimeSeconds);
        }
    }

    bool ApplyCrossChestExpandConsumeResult(const BoneParserCLibConfig& config, SeatState& seatState, int32_t actionEventId, int32_t resultType)
    {
        CrossChestExpandState& state = seatState.m_CrossChestExpand;
        if (!state.m_IsPending || state.m_ActionEventId != actionEventId)
        {
            return false;
        }

        if (resultType == kConsumeResultAccept)
        {
            SubmitCrossChestExpandAccepted(config, state, state.m_FrameTimeSeconds);
        }
        else if (resultType == kConsumeResultRetry)
        {
            SubmitCrossChestExpandRetry(state);
        }
        else
        {
            SubmitCrossChestExpandBlocked(state);
        }

        state.ResetPending();
        return true;
    }

    bool TryReadUpperBodyInput(RuntimeContext& context, UpperBodyFlowInput& input)
    {
        input = {};
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        const BoneParserCLibPerson* person = context.m_Person;
        if (person == nullptr ||
            !TryReadBodyJoint(person, BodyJointLeftShoulder, config.m_GestureKeypointMinConfidence, input.m_LeftShoulder) ||
            !TryReadBodyJoint(person, BodyJointRightShoulder, config.m_GestureKeypointMinConfidence, input.m_RightShoulder) ||
            !TryReadBodyJoint(person, BodyJointLeftWrist, config.m_GestureKeypointMinConfidence, input.m_LeftWrist) ||
            !TryReadBodyJoint(person, BodyJointRightWrist, config.m_GestureKeypointMinConfidence, input.m_RightWrist) ||
            !TryReadUpperBodyCenter(config, person, input.m_UpperBodyCenter))
        {
            return false;
        }

        input.m_ShoulderWidth = Abs(input.m_RightShoulder.m_X - input.m_LeftShoulder.m_X) + config.m_ShoulderWidthEpsilon;
        if (input.m_ShoulderWidth <= config.m_ShoulderWidthEpsilon)
        {
            return false;
        }

        input.m_ShoulderCenter = Scale(Add(input.m_LeftShoulder, input.m_RightShoulder), 0.5f);
        input.m_HasHeadTop = TryReadHeadTop(config, person, input.m_HeadTopY);
        return true;
    }

    int32_t ReadSingleOverheadSide(const BoneParserCLibConfig& config, const UpperBodyFlowInput& input)
    {
        if (!input.m_HasHeadTop)
        {
            return 0;
        }

        float threshold = input.m_HeadTopY - input.m_ShoulderWidth * config.m_PoseRaiseMarginRatio;
        bool leftOverhead = input.m_LeftWrist.m_Y <= threshold;
        bool rightOverhead = input.m_RightWrist.m_Y <= threshold;
        if (leftOverhead == rightOverhead)
        {
            return 0;
        }

        return leftOverhead ? kLeftSide : kRightSide;
    }

    Vec2 ReadWristBySide(const UpperBodyFlowInput& input, int32_t side)
    {
        return side == kLeftSide ? input.m_LeftWrist : input.m_RightWrist;
    }

    Vec2 ReadShoulderBySide(const UpperBodyFlowInput& input, int32_t side)
    {
        return side == kLeftSide ? input.m_LeftShoulder : input.m_RightShoulder;
    }

    bool ReadBothHandsAboveShoulder(const UpperBodyFlowInput& input, float aboveShoulderRatio)
    {
        float threshold = input.m_ShoulderCenter.m_Y - input.m_ShoulderWidth * aboveShoulderRatio;
        return input.m_LeftWrist.m_Y <= threshold && input.m_RightWrist.m_Y <= threshold;
    }

    bool ReadAnyHandAboveShoulder(const UpperBodyFlowInput& input, float aboveShoulderRatio)
    {
        float threshold = input.m_ShoulderCenter.m_Y - input.m_ShoulderWidth * aboveShoulderRatio;
        return input.m_LeftWrist.m_Y <= threshold || input.m_RightWrist.m_Y <= threshold;
    }

    bool ReadChestClose(const UpperBodyFlowInput& input, float closeDistanceRatio, float verticalRatio)
    {
        float wristDistance = Distance(input.m_LeftWrist, input.m_RightWrist);
        bool isClose = wristDistance <= input.m_ShoulderWidth * closeDistanceRatio;
        bool handsSwapped = input.m_LeftWrist.m_X > input.m_ShoulderCenter.m_X && input.m_RightWrist.m_X < input.m_ShoulderCenter.m_X;
        bool leftNearChest = Abs(input.m_LeftWrist.m_Y - input.m_UpperBodyCenter.m_Y) <= input.m_ShoulderWidth * verticalRatio;
        bool rightNearChest = Abs(input.m_RightWrist.m_Y - input.m_UpperBodyCenter.m_Y) <= input.m_ShoulderWidth * verticalRatio;
        return isClose && !handsSwapped && leftNearChest && rightNearChest;
    }

    bool ReadHandsExpanded(const UpperBodyFlowInput& input, float beyondShoulderRatio, float verticalToleranceRatio)
    {
        bool leftExpanded = input.m_LeftWrist.m_X <= input.m_LeftShoulder.m_X - input.m_ShoulderWidth * beyondShoulderRatio;
        bool rightExpanded = input.m_RightWrist.m_X >= input.m_RightShoulder.m_X + input.m_ShoulderWidth * beyondShoulderRatio;
        bool leftHeightValid = Abs(input.m_LeftWrist.m_Y - input.m_UpperBodyCenter.m_Y) <= input.m_ShoulderWidth * verticalToleranceRatio;
        bool rightHeightValid = Abs(input.m_RightWrist.m_Y - input.m_UpperBodyCenter.m_Y) <= input.m_ShoulderWidth * verticalToleranceRatio;
        return leftExpanded && rightExpanded && leftHeightValid && rightHeightValid;
    }

    bool ReadIsTimeout(int32_t frameCount, int32_t maxFrameCount)
    {
        return frameCount > std::max(1, maxFrameCount);
    }

    void UpdateSingleHandPullDownReadyState(
        const BoneParserCLibConfig& config,
        SingleHandPullDownState& state,
        const UpperBodyFlowInput& input,
        int32_t overheadSide)
    {
        if (overheadSide == 0)
        {
            state.m_CandidateSide = 0;
            state.m_CandidateFrameCount = 0;
            return;
        }

        if (state.m_CandidateSide == overheadSide)
        {
            state.m_CandidateFrameCount++;
        }
        else
        {
            state.m_CandidateSide = overheadSide;
            state.m_CandidateFrameCount = 1;
        }

        if (state.m_CandidateFrameCount < std::max(1, config.m_SingleHandPullDownReadyFrames))
        {
            return;
        }

        state.m_ReadySide = overheadSide;
        state.m_ReadyWrist = ReadWristBySide(input, overheadSide);
        state.m_LastWrist = state.m_ReadyWrist;
        state.m_HasLastWrist = true;
        state.m_ReleaseFrameCount = 0;
    }

    bool EvaluateSingleHandPullDown(RuntimeContext& context, SingleHandPullDownState& state)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        UpperBodyFlowInput input;
        if (!TryReadUpperBodyInput(context, input))
        {
            state.ResetFlowState();
            return false;
        }

        int32_t overheadSide = ReadSingleOverheadSide(config, input);
        if (state.m_ReadySide == 0)
        {
            UpdateSingleHandPullDownReadyState(config, state, input, overheadSide);
            return false;
        }

        if (overheadSide == state.m_ReadySide)
        {
            state.m_ReleaseFrameCount = 0;
            state.m_LastWrist = ReadWristBySide(input, state.m_ReadySide);
            state.m_HasLastWrist = true;
            return false;
        }

        if (overheadSide != 0 && overheadSide != state.m_ReadySide)
        {
            state.ResetFlowState();
            return false;
        }

        state.m_ReleaseFrameCount++;
        if (ReadIsTimeout(state.m_ReleaseFrameCount, config.m_SingleHandPullDownReleaseWindowFrames))
        {
            state.ResetFlowState();
            return false;
        }

        Vec2 currentWrist = ReadWristBySide(input, state.m_ReadySide);
        if (!state.m_HasLastWrist || context.m_DeltaTimeSeconds <= 0.0f)
        {
            state.m_LastWrist = currentWrist;
            state.m_HasLastWrist = true;
            return false;
        }

        Vec2 shoulder = ReadShoulderBySide(input, state.m_ReadySide);
        float downDistance = currentWrist.m_Y - state.m_ReadyWrist.m_Y;
        float downSpeed = (currentWrist.m_Y - state.m_LastWrist.m_Y) / context.m_DeltaTimeSeconds;
        state.m_LastWrist = currentWrist;
        return downDistance >= input.m_ShoulderWidth * config.m_SingleHandPullDownMinDistanceRatio &&
            downSpeed >= input.m_ShoulderWidth * config.m_SingleHandPullDownReleaseSpeedRatioPerSecond &&
            currentWrist.m_Y >= shoulder.m_Y + input.m_ShoulderWidth * config.m_SingleHandPullDownEndBelowShoulderRatio;
    }

    bool EvaluateHandsOnHipRaise(RuntimeContext& context, HandsOnHipRaiseState& state)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        UpperBodyFlowInput input;
        if (!TryReadUpperBodyInput(context, input))
        {
            state.ResetFlowState();
            return false;
        }

        bool handsOnHip = ReadIsHandsOnHip(context);
        if (!state.m_IsReady)
        {
            if (!handsOnHip)
            {
                state.m_ReadyFrameCount = 0;
                return false;
            }

            state.m_ReadyFrameCount++;
            if (state.m_ReadyFrameCount >= std::max(1, config.m_HandsOnHipRaiseReadyFrames))
            {
                state.m_IsReady = true;
                state.m_ReadyLeftWrist = input.m_LeftWrist;
                state.m_ReadyRightWrist = input.m_RightWrist;
            }

            return false;
        }

        if (handsOnHip)
        {
            state.m_ReleaseFrameCount = 0;
            return false;
        }

        state.m_ReleaseFrameCount++;
        if (ReadIsTimeout(state.m_ReleaseFrameCount, config.m_HandsOnHipRaiseReleaseWindowFrames))
        {
            state.ResetFlowState();
            return false;
        }

        float leftRaiseDistance = state.m_ReadyLeftWrist.m_Y - input.m_LeftWrist.m_Y;
        float rightRaiseDistance = state.m_ReadyRightWrist.m_Y - input.m_RightWrist.m_Y;
        return ReadBothHandsAboveShoulder(input, config.m_HandsOnHipRaiseEndAboveShoulderRatio) &&
            leftRaiseDistance >= input.m_ShoulderWidth * config.m_HandsOnHipRaiseMinDistanceRatio &&
            rightRaiseDistance >= input.m_ShoulderWidth * config.m_HandsOnHipRaiseMinDistanceRatio;
    }

    bool EvaluateCrouchStandRaise(RuntimeContext& context, CrouchStandRaiseState& state)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        UpperBodyFlowInput input;
        if (!TryReadUpperBodyInput(context, input))
        {
            state.ResetFlowState();
            return false;
        }

        bool crouching = ReadIsCrouching(context);
        if (!state.m_IsReady)
        {
            if (!crouching)
            {
                state.m_ReadyFrameCount = 0;
                return false;
            }

            state.m_ReadyFrameCount++;
            if (state.m_ReadyFrameCount >= std::max(1, config.m_CrouchStandRaiseReadyFrames))
            {
                state.m_IsReady = true;
            }

            return false;
        }

        if (crouching)
        {
            state.m_ReleaseFrameCount = 0;
            return false;
        }

        state.m_ReleaseFrameCount++;
        if (ReadIsTimeout(state.m_ReleaseFrameCount, config.m_CrouchStandRaiseReleaseWindowFrames))
        {
            state.ResetFlowState();
            return false;
        }

        return ReadAnyHandAboveShoulder(input, config.m_CrouchStandRaiseHandAboveShoulderRatio);
    }

    bool EvaluateChestClosePush(RuntimeContext& context, ChestClosePushState& state)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        UpperBodyFlowInput input;
        if (!TryReadUpperBodyInput(context, input))
        {
            state.ResetFlowState();
            return false;
        }

        bool chestClose = ReadChestClose(input, config.m_ChestClosePushCloseDistanceRatio, config.m_ChestClosePushVerticalRatio);
        if (!state.m_IsReady)
        {
            if (!chestClose)
            {
                state.m_ReadyFrameCount = 0;
                return false;
            }

            state.m_ReadyFrameCount++;
            if (state.m_ReadyFrameCount >= std::max(1, config.m_ChestClosePushReadyFrames))
            {
                state.m_IsReady = true;
                state.m_ReadyLeftWrist = input.m_LeftWrist;
                state.m_ReadyRightWrist = input.m_RightWrist;
                state.m_LastLeftWrist = input.m_LeftWrist;
                state.m_LastRightWrist = input.m_RightWrist;
                state.m_HasLastWrist = true;
            }

            return false;
        }

        if (chestClose)
        {
            state.m_ReleaseFrameCount = 0;
            state.m_LastLeftWrist = input.m_LeftWrist;
            state.m_LastRightWrist = input.m_RightWrist;
            state.m_HasLastWrist = true;
            return false;
        }

        state.m_ReleaseFrameCount++;
        if (ReadIsTimeout(state.m_ReleaseFrameCount, config.m_ChestClosePushReleaseWindowFrames))
        {
            state.ResetFlowState();
            return false;
        }

        if (!state.m_HasLastWrist || context.m_DeltaTimeSeconds <= 0.0f)
        {
            state.m_LastLeftWrist = input.m_LeftWrist;
            state.m_LastRightWrist = input.m_RightWrist;
            state.m_HasLastWrist = true;
            return false;
        }

        float leftPushDistance = state.m_ReadyLeftWrist.m_Y - input.m_LeftWrist.m_Y;
        float rightPushDistance = state.m_ReadyRightWrist.m_Y - input.m_RightWrist.m_Y;
        float leftPushSpeed = (state.m_LastLeftWrist.m_Y - input.m_LeftWrist.m_Y) / context.m_DeltaTimeSeconds;
        float rightPushSpeed = (state.m_LastRightWrist.m_Y - input.m_RightWrist.m_Y) / context.m_DeltaTimeSeconds;
        state.m_LastLeftWrist = input.m_LeftWrist;
        state.m_LastRightWrist = input.m_RightWrist;
        return ReadBothHandsAboveShoulder(input, config.m_ChestClosePushEndAboveShoulderRatio) &&
            leftPushDistance >= input.m_ShoulderWidth * config.m_ChestClosePushMinDistanceRatio &&
            rightPushDistance >= input.m_ShoulderWidth * config.m_ChestClosePushMinDistanceRatio &&
            leftPushSpeed >= input.m_ShoulderWidth * config.m_ChestClosePushSpeedRatioPerSecond &&
            rightPushSpeed >= input.m_ShoulderWidth * config.m_ChestClosePushSpeedRatioPerSecond;
    }

    bool EvaluateHandsExpandHold(RuntimeContext& context, HandsExpandHoldState& state)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        UpperBodyFlowInput input;
        if (!TryReadUpperBodyInput(context, input))
        {
            state.ResetFlowState();
            return false;
        }

        bool chestClose = ReadChestClose(input, config.m_HandsExpandCloseDistanceRatio, config.m_HandsExpandCloseVerticalRatio);
        if (!state.m_IsReady)
        {
            if (!chestClose)
            {
                state.m_ReadyFrameCount = 0;
                return false;
            }

            state.m_ReadyFrameCount++;
            if (state.m_ReadyFrameCount >= std::max(1, config.m_HandsExpandReadyFrames))
            {
                state.m_IsReady = true;
            }

            return false;
        }

        if (chestClose)
        {
            state.m_ReleaseFrameCount = 0;
            state.m_HoldFrameCount = 0;
            return false;
        }

        state.m_ReleaseFrameCount++;
        if (ReadIsTimeout(state.m_ReleaseFrameCount, config.m_HandsExpandReleaseWindowFrames))
        {
            state.ResetFlowState();
            return false;
        }

        if (ReadHandsExpanded(input, config.m_HandsExpandBeyondShoulderRatio, config.m_HandsExpandVerticalToleranceRatio))
        {
            state.m_HoldFrameCount++;
        }
        else
        {
            state.m_HoldFrameCount = 0;
        }

        return state.m_HoldFrameCount >= std::max(1, config.m_HandsExpandHoldFrames);
    }

    template <typename TState>
    void MarkTriggerPending(TState& state, const BoneParserCLibActionEventOutput& actionEvent, float frameTimeSeconds)
    {
        state.m_IsPending = true;
        state.m_ActionEventId = actionEvent.m_ActionEventId;
        state.m_FrameTimeSeconds = frameTimeSeconds;
    }

    template <typename TState>
    void SubmitTriggerAccepted(TState& state, float cooldownSeconds, float frameTimeSeconds)
    {
        state.ResetFlowState();
        state.m_CooldownUntilSeconds = frameTimeSeconds + cooldownSeconds;
    }

    template <typename TState>
    void UpdateTriggerOnlyRecognizer(
        RuntimeContext& context,
        TState& state,
        int32_t gestureType,
        float cooldownSeconds,
        bool (*evaluate)(RuntimeContext&, TState&))
    {
        if (state.m_IsPending)
        {
            return;
        }

        if (!HasRecognizableActionBinding(context, gestureType))
        {
            state.ResetFlowState();
            return;
        }

        if (context.m_FrameTimeSeconds > 0.0f && context.m_FrameTimeSeconds < state.m_CooldownUntilSeconds)
        {
            return;
        }

        if (!evaluate(context, state))
        {
            return;
        }

        BoneParserCLibActionEventOutput* actionEvent = AddGestureEvent(context, gestureType, kPhaseTrigger);
        if (actionEvent == nullptr)
        {
            state.ResetFlowState();
            return;
        }

        if (actionEvent->m_RequiresConsumeResult != 0)
        {
            MarkTriggerPending(state, *actionEvent, context.m_FrameTimeSeconds);
            return;
        }

        SubmitTriggerAccepted(state, cooldownSeconds, context.m_FrameTimeSeconds);
    }

    template <typename TState>
    bool ApplyTriggerOnlyConsumeResult(
        TState& state,
        int32_t actionEventId,
        int32_t resultType,
        float cooldownSeconds)
    {
        if (!state.m_IsPending || state.m_ActionEventId != actionEventId)
        {
            return false;
        }

        if (resultType == kConsumeResultAccept)
        {
            SubmitTriggerAccepted(state, cooldownSeconds, state.m_FrameTimeSeconds);
        }
        else
        {
            state.ResetFlowState();
        }

        state.ResetPending();
        return true;
    }

    void UpdateAllGestureRecognizers(RuntimeContext& context)
    {
        const BoneParserCLibConfig& config = ReadConfig(context.m_Config);
        UpdatePoseRecognizer(context);
        UpdateAlternatingSwingRecognizer(context);
        UpdateLargeAlternatingSwingRecognizer(context);
        UpdateOverheadPressReleaseRecognizer(context);
        UpdateCrossChestExpandRecognizer(context);
        UpdateTriggerOnlyRecognizer(context, context.m_SeatState->m_SingleHandPullDown, kGestureSingleHandPullDownFlow, config.m_SingleHandPullDownCooldownSeconds, EvaluateSingleHandPullDown);
        UpdateTriggerOnlyRecognizer(context, context.m_SeatState->m_HandsOnHipRaise, kGestureHandsOnHipRaiseFlow, config.m_HandsOnHipRaiseCooldownSeconds, EvaluateHandsOnHipRaise);
        UpdateTriggerOnlyRecognizer(context, context.m_SeatState->m_CrouchStandRaise, kGestureCrouchStandRaiseFlow, config.m_CrouchStandRaiseCooldownSeconds, EvaluateCrouchStandRaise);
        UpdateTriggerOnlyRecognizer(context, context.m_SeatState->m_ChestClosePush, kGestureChestClosePushFlow, config.m_ChestClosePushCooldownSeconds, EvaluateChestClosePush);
        UpdateTriggerOnlyRecognizer(context, context.m_SeatState->m_HandsExpandHold, kGestureHandsExpandHoldFlow, config.m_HandsExpandCooldownSeconds, EvaluateHandsExpandHold);
    }

    void HandleSeatMissing(
        const BoneParserCLibConfig& config,
        const BoneParserCLibFrameInput* frameData,
        SeatState& seatState,
        BoneParserCLibPlayerResultOutput* result,
        bool preservePersonCandidate)
    {
        if (!preservePersonCandidate)
        {
            seatState.m_AimCandidatePersonId = kInvalidPersonId;
            seatState.m_AimCandidateStableFrameCount = 0;
        }

        seatState.m_MissingFrameCount++;
        float deltaTimeSeconds = ReadDeltaTimeSeconds(frameData, seatState);
        UpdateMissingAim(config, seatState, result, deltaTimeSeconds);
        if (seatState.m_MissingFrameCount > std::max(config.m_MaxMissingFrameCount, config.m_AimHoldMissingFrames) &&
            !seatState.m_HasAimOutput)
        {
            seatState.ResetForRelease();
        }
    }

    void EnsureSeatRuntime(BoneParserCLibContext& context, int32_t slotCount)
    {
        if (slotCount < 0)
        {
            slotCount = 0;
        }

        context.m_SeatStates.resize(static_cast<size_t>(slotCount));
    }

    void PrepareFrameOutput(
        BoneParserCLibContext& parser,
        OutputWriter& writer,
        const BoneParserCLibFrameInput* frameData,
        const BoneParserCLibSeatDefinition* seatDefinitions,
        int32_t seatDefinitionCount)
    {
        int32_t frameSerial = frameData != nullptr ? frameData->m_FrameSerial : parser.m_FrameSerial + 1;
        writer.m_FrameOutput->m_FrameSerial = frameSerial;
        writer.m_FrameOutput->m_PlayerResultCount = seatDefinitionCount;
        writer.m_FrameOutput->m_GestureEventCount = 0;
        writer.m_FrameOutput->m_ActionEventCount = 0;
        writer.m_GestureEventCount = 0;
        writer.m_ActionEventCount = 0;
        writer.m_Overflow = false;

        for (int32_t i = 0; i < seatDefinitionCount; i++)
        {
            ResetPlayerResult(
                writer.m_PlayerResults[i],
                ReadFrameSlotIndex(seatDefinitions, seatDefinitionCount, i),
                ReadFrameBindingId(seatDefinitions, seatDefinitionCount, i),
                writer.m_GestureEventCount,
                writer.m_ActionEventCount);
        }
    }

    void UpdateFrameSeats(
        BoneParserCLibContext& parser,
        OutputWriter& writer,
        const BoneParserCLibConfig& config,
        const BoneParserCLibFrameInput* frameData,
        const BoneParserCLibSeatDefinition* seatDefinitions,
        int32_t seatDefinitionCount,
        const BoneParserCLibActionBinding* actionBindings,
        int32_t actionBindingCount)
    {
        float frameTimeSeconds = ReadFrameTimeSeconds(frameData);
        for (int32_t definitionIndex = 0; definitionIndex < seatDefinitionCount; definitionIndex++)
        {
            SeatState& seatState = parser.m_SeatStates[static_cast<size_t>(definitionIndex)];
            BoneParserCLibPlayerResultOutput& result = writer.m_PlayerResults[definitionIndex];
            const BoneParserCLibSeatDefinition* seatDefinition = ReadSeatDefinition(seatDefinitions, seatDefinitionCount, definitionIndex);
            int32_t frameSlotIndex = ReadFrameSlotIndex(seatDefinitions, seatDefinitionCount, definitionIndex);
            writer.m_CurrentPlayer = &result;
            result.m_GestureEventStart = writer.m_GestureEventCount;
            result.m_ActionEventStart = writer.m_ActionEventCount;

            const BoneParserCLibPerson* person = nullptr;
            if (!TryReadSlotPerson(frameData, frameSlotIndex, person) ||
                !ReadIsBindablePerson(config, person))
            {
                HandleSeatMissing(config, frameData, seatState, &result, false);
                continue;
            }

            NotifyPersonCandidate(seatState, person->m_PersonId);
            if (!CanAcceptPerson(config, seatState, person->m_PersonId))
            {
                HandleSeatMissing(config, frameData, seatState, &result, true);
                continue;
            }

            if (seatState.m_CurrentPersonId != person->m_PersonId)
            {
                seatState.ResetForNewPerson(person->m_PersonId);
            }
            else
            {
                seatState.m_MissingFrameCount = 0;
            }

            result.m_IsTracked = 1;
            result.m_PersonId = person->m_PersonId;
            float deltaTimeSeconds = ReadDeltaTimeSeconds(frameData, seatState);
            UpdateTrackedAim(config, person, seatState, result, deltaTimeSeconds);

            RuntimeContext runtimeContext;
            runtimeContext.m_Parser = &parser;
            runtimeContext.m_Config = &config;
            runtimeContext.m_Writer = &writer;
            runtimeContext.m_Person = person;
            runtimeContext.m_SeatState = &seatState;
            runtimeContext.m_SeatDefinition = seatDefinition;
            runtimeContext.m_Result = &result;
            runtimeContext.m_FrameData = frameData;
            runtimeContext.m_ActionBindings = actionBindings;
            runtimeContext.m_ActionBindingCount = actionBindingCount;
            runtimeContext.m_DeltaTimeSeconds = deltaTimeSeconds;
            runtimeContext.m_FrameTimeSeconds = frameTimeSeconds;
            UpdateAllGestureRecognizers(runtimeContext);
        }

        writer.m_CurrentPlayer = nullptr;
    }

    bool ApplyConsumeResultToSeat(const BoneParserCLibConfig& config, SeatState& seatState, int32_t actionEventId, int32_t resultType)
    {
        if (ApplyAlternatingSwingConsumeResult(seatState, actionEventId, resultType))
        {
            return true;
        }

        if (ApplyLargeAlternatingSwingConsumeResult(seatState, actionEventId, resultType))
        {
            return true;
        }

        if (ApplyOverheadConsumeResult(config, seatState, actionEventId, resultType))
        {
            return true;
        }

        if (ApplyCrossChestExpandConsumeResult(config, seatState, actionEventId, resultType))
        {
            return true;
        }

        if (ApplyTriggerOnlyConsumeResult(seatState.m_SingleHandPullDown, actionEventId, resultType, config.m_SingleHandPullDownCooldownSeconds))
        {
            return true;
        }

        if (ApplyTriggerOnlyConsumeResult(seatState.m_HandsOnHipRaise, actionEventId, resultType, config.m_HandsOnHipRaiseCooldownSeconds))
        {
            return true;
        }

        if (ApplyTriggerOnlyConsumeResult(seatState.m_CrouchStandRaise, actionEventId, resultType, config.m_CrouchStandRaiseCooldownSeconds))
        {
            return true;
        }

        if (ApplyTriggerOnlyConsumeResult(seatState.m_ChestClosePush, actionEventId, resultType, config.m_ChestClosePushCooldownSeconds))
        {
            return true;
        }

        if (ApplyTriggerOnlyConsumeResult(seatState.m_HandsExpandHold, actionEventId, resultType, config.m_HandsExpandCooldownSeconds))
        {
            return true;
        }

        return false;
    }
}

extern "C"
{
    BONE_PARSER_CLIB_API int32_t BONE_PARSER_CLIB_CALL BoneParserCLib_GetAbiVersion(void)
    {
        return BoneParserCLibAbiVersion;
    }

    BONE_PARSER_CLIB_API BoneParserCLibHandle BONE_PARSER_CLIB_CALL BoneParserCLib_Create(void)
    {
        return new (std::nothrow) BoneParserCLibContext();
    }

    BONE_PARSER_CLIB_API void BONE_PARSER_CLIB_CALL BoneParserCLib_Reset(BoneParserCLibHandle parser)
    {
        BoneParserCLibContext* context = ReadContext(parser);
        if (context == nullptr)
        {
            return;
        }

        context->m_ResetCount++;
        context->m_SeatStates.clear();
        context->m_FrameSerial = 0;
        context->m_NextActionEventId = 1;
    }

    BONE_PARSER_CLIB_API void BONE_PARSER_CLIB_CALL BoneParserCLib_Destroy(BoneParserCLibHandle parser)
    {
        BoneParserCLibContext* context = ReadContext(parser);
        if (context == nullptr)
        {
            return;
        }

        context->m_Magic = 0;
        delete context;
    }

    BONE_PARSER_CLIB_API int32_t BONE_PARSER_CLIB_CALL BoneParserCLib_IsValid(BoneParserCLibHandle parser)
    {
        return ReadContext(parser) != nullptr ? 1 : 0;
    }

    BONE_PARSER_CLIB_API int32_t BONE_PARSER_CLIB_CALL BoneParserCLib_Update(
        BoneParserCLibHandle parser,
        const BoneParserCLibConfig* config,
        const BoneParserCLibFrameInput* frameInput,
        const BoneParserCLibSeatDefinition* seatDefinitions,
        int32_t seatDefinitionCount,
        const BoneParserCLibActionBinding* actionBindings,
        int32_t actionBindingCount,
        BoneParserCLibPlayerResultOutput* playerResults,
        int32_t playerResultCapacity,
        BoneParserCLibGestureEventOutput* gestureEvents,
        int32_t gestureEventCapacity,
        BoneParserCLibActionEventOutput* actionEvents,
        int32_t actionEventCapacity,
        BoneParserCLibFrameOutput* frameOutput)
    {
        BoneParserCLibContext* context = ReadContext(parser);
        if (context == nullptr || frameOutput == nullptr || playerResults == nullptr || seatDefinitionCount < 0)
        {
            return 0;
        }

        if (playerResultCapacity < seatDefinitionCount)
        {
            return -2;
        }

        const BoneParserCLibConfig& resolvedConfig = ReadConfig(config);
        EnsureSeatRuntime(*context, seatDefinitionCount);

        OutputWriter writer;
        writer.m_Parser = context;
        writer.m_SeatDefinitions = seatDefinitions;
        writer.m_SeatDefinitionCount = seatDefinitionCount;
        writer.m_ActionBindings = actionBindings;
        writer.m_ActionBindingCount = actionBindingCount;
        writer.m_PlayerResults = playerResults;
        writer.m_PlayerResultCapacity = playerResultCapacity;
        writer.m_GestureEvents = gestureEvents;
        writer.m_GestureEventCapacity = gestureEventCapacity;
        writer.m_ActionEvents = actionEvents;
        writer.m_ActionEventCapacity = actionEventCapacity;
        writer.m_FrameOutput = frameOutput;

        PrepareFrameOutput(*context, writer, frameInput, seatDefinitions, seatDefinitionCount);
        if (seatDefinitionCount <= 0)
        {
            context->m_FrameSerial = frameOutput->m_FrameSerial;
            return 1;
        }

        if (frameInput == nullptr || frameInput->m_HasFrameData == 0)
        {
            for (int32_t i = 0; i < seatDefinitionCount; i++)
            {
                writer.m_CurrentPlayer = &playerResults[i];
                HandleSeatMissing(resolvedConfig, frameInput, context->m_SeatStates[static_cast<size_t>(i)], &playerResults[i], false);
            }
        }
        else
        {
            UpdateFrameSeats(
                *context,
                writer,
                resolvedConfig,
                frameInput,
                seatDefinitions,
                seatDefinitionCount,
                actionBindings,
                actionBindingCount);
        }

        frameOutput->m_GestureEventCount = writer.m_GestureEventCount;
        frameOutput->m_ActionEventCount = writer.m_ActionEventCount;
        context->m_FrameSerial = frameOutput->m_FrameSerial;
        return writer.m_Overflow ? -2 : 1;
    }

    BONE_PARSER_CLIB_API int32_t BONE_PARSER_CLIB_CALL BoneParserCLib_ApplyActionConsumeResult(
        BoneParserCLibHandle parser,
        const BoneParserCLibConfig* config,
        int32_t actionEventId,
        int32_t resultType)
    {
        BoneParserCLibContext* context = ReadContext(parser);
        if (context == nullptr || actionEventId <= 0)
        {
            return 0;
        }

        const BoneParserCLibConfig& resolvedConfig = ReadConfig(config);
        for (size_t i = 0; i < context->m_SeatStates.size(); i++)
        {
            if (ApplyConsumeResultToSeat(resolvedConfig, context->m_SeatStates[i], actionEventId, resultType))
            {
                return 1;
            }
        }

        return 0;
    }
}
