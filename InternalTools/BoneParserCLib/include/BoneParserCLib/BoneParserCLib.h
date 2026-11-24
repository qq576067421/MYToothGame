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
    BoneParserCLibAbiVersion = 32,
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
    float m_ShoulderAngleTurnMaxOutputAngleDegrees;
    int32_t m_ShoulderAngleTurnInvertDirection;
    float m_ShoulderAngleTurnSmoothTimeSeconds;
    float m_ShoulderAngleTurnFullInputDegrees;
    float m_ShoulderAngleTurnInputJitterDegrees;
    float m_ShoulderAngleTurnMaxSpeedDegreesPerSecond;
    float m_KeypointConfidenceThreshold;
    float m_ShoulderWidthEpsilon;
    int32_t m_AimReconnectStableFrames;
    float m_GestureKeypointMinConfidence;
    float m_UpDownSwingMinUpwardSpeedRatioPerSecond;
    float m_UpDownSwingStartMaxHeightRatio;
    float m_UpDownSwingStartMinHeightRatio;
    float m_UpDownSwingMinStrokeDistanceRatio;
    float m_UpDownSwingMaxLateralRatio;
    float m_LargeAlternatingSwingSpeedRatioPerSecond;
    float m_LargeAlternatingSwingDirectionNoiseRatio;
    int32_t m_LargeAlternatingSwingMinDirectionalFrames;
    float m_LargeAlternatingSwingCooldownSeconds;
    int32_t m_LargeAlternatingSwingWindowFrames;
    float m_LargeAlternatingSwingMinTorsoDistanceRatio;
    float m_HandsAboveNoseEnterMarginRatio;
    float m_HandsAboveNoseExitMarginRatio;
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
    int32_t m_LeftLeanReadyFrames;
    float m_LeftLeanTriggerOffsetRatio;
    float m_LeftLeanReturnToNeutralRatio;
    float m_LeftLeanMinSpeedRatioPerSecond;
    float m_LeftLeanCooldownSeconds;
    int32_t m_RightLeanReadyFrames;
    float m_RightLeanTriggerOffsetRatio;
    float m_RightLeanReturnToNeutralRatio;
    float m_RightLeanMinSpeedRatioPerSecond;
    float m_RightLeanCooldownSeconds;
    int32_t m_JumpReadyFrames;
    float m_JumpReadyMaxVerticalSpeedRatioPerSecond;
    float m_JumpMinShoulderRiseRatio;
    float m_JumpMinHipRiseRatio;
    float m_JumpMinAnkleRiseRatio;
    float m_JumpMinUpwardSpeedRatioPerSecond;
    float m_JumpCooldownSeconds;
    int32_t m_CrouchDownReadyFrames;
    float m_CrouchDownReadyMaxVerticalSpeedRatioPerSecond;
    float m_CrouchDownMinShoulderDropRatio;
    float m_CrouchDownMinHipDropRatio;
    float m_CrouchDownMinLegCompressionRatio;
    float m_CrouchDownMinDownwardSpeedRatioPerSecond;
    float m_CrouchDownCooldownSeconds;
    // 蹲下确认保持帧数：下降条件首次满足后，身体需保持在髋部下降阈值以下至少
    // 这么多帧才真正触发；起跳前的反向下压会很快反弹回去，凑不满保持帧。
    // 小于等于 1 表示满足条件立即触发（旧行为）。
    int32_t m_CrouchDownConfirmHoldFrames;
    float m_SingleHandSideSweepStartAboveHipToleranceRatio;
    float m_SingleHandSideSweepStartBelowHipToleranceRatio;
    float m_SingleHandSideSweepStartMovementDistanceRatio;
    float m_SingleHandSideSweepElbowHeightToleranceRatio;
    int32_t m_SingleHandSideSweepWristMissingToleranceFrames;
    float m_SingleHandSideSweepMaxDurationSeconds;
    int32_t m_SingleHandSideSweepElbowMissingToleranceFrames;
    float m_SingleHandSideSweepMinTriggerIntervalSeconds;
    float m_PoseRaiseMarginRatio;
    float m_PoseCrossChestCenterRatio;
    float m_PoseCrossChestVerticalRatio;
    float m_PoseHipAttachRatio;
    float m_PoseHipVerticalRatio;
    float m_PoseCrouchTorsoRatio;
    int32_t m_PoseStableFrames;
    float m_UpDownSwingPairingWindowSeconds;
    float m_ShoulderAngleTurnMinShoulderSpanToTorsoRatio;
    float m_TurnUpdateDeltaTimeSeconds;
    float m_LeanRebaselineStopSpeedRatioPerSecond;
    float m_LeanRebaselineStopFrames;
    float m_LeanPredictiveLeadSeconds;
    float m_LeanPredictiveMinOffsetRatio;
    float m_LeanPredictiveMinSpeedRatioPerSecond;
    float m_SingleHandSideSweepPredictiveLeadSeconds;
    float m_SingleHandSideSweepPredictiveMinUpSpeedRatioPerSecond;
    // 蹲下去腾空联锁：最近一段时间内肩髋曾从更低位置明显上升，即视为刚起跳或仍在空中，
    // 期间不建立下蹲基准、不触发下蹲，避免把跳跃顶点当成站立基准后把下落判成下蹲。
    float m_CrouchDownAirborneRiseRatio;
    int32_t m_CrouchDownAirborneHoldFrames;
    // 单手侧挥攻击起步即发：起步后上挥速度连续达标这么多帧立即出招，不等手腕到达肘部高度。
    int32_t m_SingleHandSideSweepFastTriggerFrames;
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
    int32_t m_TriggerHand;
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
    int32_t m_TriggerHand;
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
BONE_PARSER_CLIB_API void BONE_PARSER_CLIB_CALL BoneParserCLib_ResetSeat(BoneParserCLibHandle parser, int32_t seatIndex);

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
