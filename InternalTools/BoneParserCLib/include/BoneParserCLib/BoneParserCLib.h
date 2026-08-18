#pragma once

#include <stdint.h>

#if defined(_WIN32)
#if defined(BONE_PARSER_CLIB_BUILD)
#define BONE_PARSER_CLIB_API __declspec(dllexport)
#else
#define BONE_PARSER_CLIB_API __declspec(dllimport)
#endif
#define BONE_PARSER_CLIB_CALL __cdecl
#else
#define BONE_PARSER_CLIB_API __attribute__((visibility("default")))
#define BONE_PARSER_CLIB_CALL
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef void* BoneParserCLibHandle;

enum
{
    BoneParserCLibAbiVersion = 4,
    BoneParserCLibBodyJointCount = 18
};

typedef struct BoneParserCLibVector3
{
    float m_X;
    float m_Y;
    float m_Z;
} BoneParserCLibVector3;

typedef struct BoneParserCLibConfig
{
    float m_MinBodyScore;
    float m_MinJointScore;
    int32_t m_MaxMissingFrameCount;
    float m_RotationSmoothFactor;
    float m_KeypointConfidenceThreshold;
    float m_ShoulderWidthEpsilon;
    float m_MaxShoulderWidthUpdateConfidence;
    float m_MaxTurnAngleDegrees;
    int32_t m_InvertTurnDirection;
    float m_RotationAmplifyFactor;
    float m_AimCenterEnterRatio;
    float m_AimCenterExitRatio;
    float m_AimResponseCurveExponent;
    int32_t m_AimPredictMissingFrames;
    int32_t m_AimHoldMissingFrames;
    int32_t m_AimReconnectStableFrames;
    float m_AimPredictVelocityDamping;
    float m_AimReconnectBlendFactor;
    float m_AimReturnToForwardSeconds;
    float m_GestureKeypointMinConfidence;
    float m_AlternatingSwingSpeedRatioPerSecond;
    float m_AlternatingSwingMinVerticalDistanceRatio;
    float m_AlternatingSwingDirectionNoiseRatio;
    int32_t m_AlternatingSwingMinDirectionalFrames;
    float m_AlternatingSwingCooldownSeconds;
    int32_t m_AlternatingSwingWindowFrames;
    float m_LargeAlternatingSwingMinTorsoDistanceRatio;
    int32_t m_OverheadPressReadyFrames;
    float m_OverheadPressHeadMarginRatio;
    float m_OverheadPressReleaseSpeedRatio;
    float m_OverheadPressMinReleaseDistanceRatio;
    float m_OverheadPressReleaseEndBelowHeadRatio;
    int32_t m_OverheadPressReleaseWindowFrames;
    float m_OverheadPressCooldownSeconds;
    int32_t m_CrossChestExpandReadyFrames;
    float m_CrossChestExpandSpeedRatioPerSecond;
    float m_CrossChestExpandMinDistanceRatio;
    int32_t m_CrossChestExpandReleaseWindowFrames;
    float m_CrossChestExpandCooldownSeconds;
    int32_t m_SingleHandPullDownReadyFrames;
    float m_SingleHandPullDownReleaseSpeedRatioPerSecond;
    float m_SingleHandPullDownMinDistanceRatio;
    float m_SingleHandPullDownEndBelowShoulderRatio;
    int32_t m_SingleHandPullDownReleaseWindowFrames;
    float m_SingleHandPullDownCooldownSeconds;
    int32_t m_HandsOnHipRaiseReadyFrames;
    float m_HandsOnHipRaiseMinDistanceRatio;
    float m_HandsOnHipRaiseEndAboveShoulderRatio;
    int32_t m_HandsOnHipRaiseReleaseWindowFrames;
    float m_HandsOnHipRaiseCooldownSeconds;
    int32_t m_CrouchStandRaiseReadyFrames;
    float m_CrouchStandRaiseHandAboveShoulderRatio;
    int32_t m_CrouchStandRaiseReleaseWindowFrames;
    float m_CrouchStandRaiseCooldownSeconds;
    int32_t m_ChestClosePushReadyFrames;
    float m_ChestClosePushCloseDistanceRatio;
    float m_ChestClosePushVerticalRatio;
    float m_ChestClosePushMinDistanceRatio;
    float m_ChestClosePushEndAboveShoulderRatio;
    float m_ChestClosePushSpeedRatioPerSecond;
    int32_t m_ChestClosePushReleaseWindowFrames;
    float m_ChestClosePushCooldownSeconds;
    int32_t m_HandsExpandReadyFrames;
    float m_HandsExpandCloseDistanceRatio;
    float m_HandsExpandCloseVerticalRatio;
    float m_HandsExpandBeyondShoulderRatio;
    float m_HandsExpandVerticalToleranceRatio;
    int32_t m_HandsExpandHoldFrames;
    int32_t m_HandsExpandReleaseWindowFrames;
    float m_HandsExpandCooldownSeconds;
    float m_PoseRaiseMarginRatio;
    float m_PoseCrossChestCenterRatio;
    float m_PoseCrossChestVerticalRatio;
    float m_PoseHipAttachRatio;
    float m_PoseHipVerticalRatio;
    float m_PoseCrouchTorsoRatio;
    int32_t m_PoseStableFrames;
} BoneParserCLibConfig;

typedef struct BoneParserCLibJoint
{
    int32_t m_IsTracked;
    float m_X;
    float m_Y;
    float m_Z;
    float m_Score;
} BoneParserCLibJoint;

typedef struct BoneParserCLibRect
{
    int32_t m_IsValid;
    float m_Left;
    float m_Top;
    float m_Right;
    float m_Bottom;
} BoneParserCLibRect;

typedef struct BoneParserCLibPerson
{
    int32_t m_PersonId;
    float m_BodyScore;
    int32_t m_BodyType;
    BoneParserCLibRect m_BodyRect;
    const BoneParserCLibJoint* m_BodyJoints;
    int32_t m_BodyJointCount;
} BoneParserCLibPerson;

typedef struct BoneParserCLibFrameInput
{
    int32_t m_HasFrameData;
    int32_t m_FrameSerial;
    int32_t m_IsSimulated;
    int64_t m_FrameTimeMs;
    int32_t m_ImageWidth;
    int32_t m_ImageHeight;
    int32_t m_PersonCount;
    const BoneParserCLibPerson* m_Persons;
} BoneParserCLibFrameInput;

typedef struct BoneParserCLibActionBinding
{
    int32_t m_ActionId;
    int32_t m_GestureType;
    int32_t m_PhaseMask;
    int32_t m_ConsumerType;
    int32_t m_ConsumerValue;
    int32_t m_RuntimeFlags;
    int32_t m_RequiresConsumeResult;
} BoneParserCLibActionBinding;

typedef struct BoneParserCLibSeatDefinition
{
    int32_t m_SlotIndex;
    int32_t m_BindingId;
    int32_t m_IsProcessGestureEnabled;
    int32_t m_ActionBindingStart;
    int32_t m_ActionBindingCount;
} BoneParserCLibSeatDefinition;

typedef struct BoneParserCLibGestureEventOutput
{
    int32_t m_GestureType;
    int32_t m_Phase;
    int32_t m_SlotIndex;
    int32_t m_BindingId;
    int32_t m_PersonId;
    int32_t m_FrameSerial;
} BoneParserCLibGestureEventOutput;

typedef struct BoneParserCLibActionEventOutput
{
    int32_t m_ActionEventId;
    int32_t m_ActionId;
    int32_t m_GestureType;
    int32_t m_Phase;
    int32_t m_SlotIndex;
    int32_t m_BindingId;
    int32_t m_PersonId;
    int32_t m_FrameSerial;
    int32_t m_ConsumerType;
    int32_t m_ConsumerValue;
    int32_t m_RuntimeFlags;
    int32_t m_RequiresConsumeResult;
    BoneParserCLibVector3 m_FaceForward;
    BoneParserCLibVector3 m_MoveDirection;
} BoneParserCLibActionEventOutput;

typedef struct BoneParserCLibPlayerResultOutput
{
    int32_t m_SlotIndex;
    int32_t m_BindingId;
    int32_t m_IsTracked;
    int32_t m_IsAimAvailable;
    int32_t m_AimTrackingState;
    float m_AimConfidence;
    int32_t m_MissingFrameCount;
    int32_t m_PersonId;
    BoneParserCLibVector3 m_FaceForward;
    float m_TurnSpeed;
    float m_TurnStrength;
    float m_TurnAngleDegrees;
    int32_t m_ActivePoseHints;
    int32_t m_GestureEventStart;
    int32_t m_GestureEventCount;
    int32_t m_ActionEventStart;
    int32_t m_ActionEventCount;
} BoneParserCLibPlayerResultOutput;

typedef struct BoneParserCLibFrameOutput
{
    int32_t m_FrameSerial;
    int32_t m_PlayerResultCount;
    int32_t m_GestureEventCount;
    int32_t m_ActionEventCount;
} BoneParserCLibFrameOutput;

BONE_PARSER_CLIB_API int32_t BONE_PARSER_CLIB_CALL BoneParserCLib_GetAbiVersion(void);

BONE_PARSER_CLIB_API BoneParserCLibHandle BONE_PARSER_CLIB_CALL BoneParserCLib_Create(void);

BONE_PARSER_CLIB_API void BONE_PARSER_CLIB_CALL BoneParserCLib_Reset(BoneParserCLibHandle parser);

BONE_PARSER_CLIB_API void BONE_PARSER_CLIB_CALL BoneParserCLib_Destroy(BoneParserCLibHandle parser);

BONE_PARSER_CLIB_API int32_t BONE_PARSER_CLIB_CALL BoneParserCLib_IsValid(BoneParserCLibHandle parser);

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
    BoneParserCLibFrameOutput* frameOutput);

BONE_PARSER_CLIB_API int32_t BONE_PARSER_CLIB_CALL BoneParserCLib_ApplyActionConsumeResult(
    BoneParserCLibHandle parser,
    const BoneParserCLibConfig* config,
    int32_t actionEventId,
    int32_t resultType);

#ifdef __cplusplus
}
#endif
