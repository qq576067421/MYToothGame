using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GameDll
{
    internal sealed class BoneParserRuntimeNative : IBoneParserRuntime
    {
        private const int m_ExpectedAbiVersion = 9;
        private const int m_BodyJointCount = 18;
        private const int m_MinNativeArrayCapacity = 1;
        private const int m_MaxNativeEventsPerSeat = 64;

        private static readonly int m_NativeJointSize = Marshal.SizeOf(typeof(BoneParserCLibJoint));

        private readonly BoneParserConfig m_Config;
#if BoneParserLib
        private readonly BoneParserRuntimeManaged m_FallbackRuntime;
#endif
        private readonly BoneParserFrameResult m_FrameResult = new BoneParserFrameResult();

        private IntPtr m_NativeHandle;
        private bool m_HasTriedCreateNative;
        private bool m_HasLoggedFallback;
        private bool m_HasLoggedNativeFailure;

        private BoneParserCLibConfig m_NativeConfig;
        private BoneParserCLibFrameInput m_NativeFrameInput;
        private BoneParserCLibPerson[] m_NativePersons = new BoneParserCLibPerson[m_MinNativeArrayCapacity];
        private BoneParserCLibJoint[] m_NativeBodyJoints = new BoneParserCLibJoint[m_MinNativeArrayCapacity];
        private BoneParserCLibSeatDefinition[] m_NativeSeatDefinitions = new BoneParserCLibSeatDefinition[m_MinNativeArrayCapacity];
        private BoneParserCLibActionBinding[] m_NativeActionBindings = new BoneParserCLibActionBinding[m_MinNativeArrayCapacity];
        private BoneParserCLibPlayerResult[] m_NativePlayerResults = new BoneParserCLibPlayerResult[m_MinNativeArrayCapacity];
        private BoneParserCLibGestureEvent[] m_NativeGestureEvents = new BoneParserCLibGestureEvent[m_MinNativeArrayCapacity];
        private BoneParserCLibActionEvent[] m_NativeActionEvents = new BoneParserCLibActionEvent[m_MinNativeArrayCapacity];

        public string RuntimeName
        {
            get
            {
                if (m_NativeHandle != IntPtr.Zero)
                {
                    return "BoneParserCLib";
                }

#if BoneParserLib
                return "BoneParserCLib fallback BoneParserLib";
#else
                return "BoneParserCLib unavailable";
#endif
            }
        }

        public BoneParserRuntimeNative(BoneParserConfig config)
        {
            m_Config = config ?? new BoneParserConfig();
#if BoneParserLib
            m_FallbackRuntime = new BoneParserRuntimeManaged(m_Config);
#endif
        }

        public void Reset()
        {
            EnsureNativeCreated();
            if (m_NativeHandle != IntPtr.Zero)
            {
                try
                {
                    BoneParserCLib_Reset(m_NativeHandle);
                }
                catch (EntryPointNotFoundException)
                {
                    ReleaseNativeHandle();
                }
            }

            ResetFallbackRuntime();
            ResetFrameResult();
        }

        public BoneParserFrameResult Update(BoneTrackedFrame frameData, IList<BoneParserSeatDefinition> seatDefinitions)
        {
            EnsureNativeCreated();
            if (m_NativeHandle == IntPtr.Zero)
            {
                LogFallbackOnce("[塔防骨骼输入] BoneParserCLib 未加载。");
                return UpdateFallbackRuntime(frameData, seatDefinitions);
            }

            GCHandle personHandle = default(GCHandle);
            GCHandle jointHandle = default(GCHandle);
            try
            {
                CopyConfigToNative();
                int seatCount = seatDefinitions != null ? seatDefinitions.Count : 0;
                int personCount = frameData != null && frameData.m_Persons != null ? frameData.m_Persons.Count : 0;
                int actionBindingCount = CopySeatDefinitionsToNative(seatDefinitions, seatCount);
                CopyFrameToNative(frameData, personCount);
                EnsureOutputCapacity(seatCount);

                personHandle = GCHandle.Alloc(m_NativePersons, GCHandleType.Pinned);
                jointHandle = GCHandle.Alloc(m_NativeBodyJoints, GCHandleType.Pinned);
                m_NativeFrameInput.m_Persons = personHandle.AddrOfPinnedObject();
                AssignBodyJointPointers(jointHandle.AddrOfPinnedObject(), personCount);

                BoneParserCLibFrameOutput frameOutput;
                int updateResult = BoneParserCLib_Update(
                    m_NativeHandle,
                    ref m_NativeConfig,
                    ref m_NativeFrameInput,
                    m_NativeSeatDefinitions,
                    seatCount,
                    m_NativeActionBindings,
                    actionBindingCount,
                    m_NativePlayerResults,
                    m_NativePlayerResults.Length,
                    m_NativeGestureEvents,
                    m_NativeGestureEvents.Length,
                    m_NativeActionEvents,
                    m_NativeActionEvents.Length,
                    out frameOutput);

                if (updateResult == 1)
                {
                    CopyNativeResultToManaged(frameOutput);
                    return m_FrameResult;
                }

                LogNativeFailureOnce("[塔防骨骼输入] BoneParserCLib 返回无效结果。");
                ReleaseNativeHandle();
                ResetFallbackRuntime();
                return UpdateFallbackRuntime(frameData, seatDefinitions);
            }
            catch (DllNotFoundException)
            {
                ReleaseNativeHandle();
                LogFallbackOnce("[塔防骨骼输入] BoneParserCLib 未加载。");
                return UpdateFallbackRuntime(frameData, seatDefinitions);
            }
            catch (EntryPointNotFoundException)
            {
                ReleaseNativeHandle();
                LogFallbackOnce("[塔防骨骼输入] BoneParserCLib 导出函数不完整。");
                return UpdateFallbackRuntime(frameData, seatDefinitions);
            }
            catch (BadImageFormatException)
            {
                ReleaseNativeHandle();
                LogFallbackOnce("[塔防骨骼输入] BoneParserCLib 平台格式不匹配。");
                return UpdateFallbackRuntime(frameData, seatDefinitions);
            }
            finally
            {
                if (personHandle.IsAllocated)
                {
                    personHandle.Free();
                }

                if (jointHandle.IsAllocated)
                {
                    jointHandle.Free();
                }
            }
        }

        public bool ApplyActionConsumeResult(BoneActionConsumeResult consumeResult)
        {
            EnsureNativeCreated();
            if (m_NativeHandle == IntPtr.Zero)
            {
                return ApplyFallbackConsumeResult(consumeResult);
            }

            if (consumeResult == null || consumeResult.m_ActionEventId <= 0)
            {
                return false;
            }

            try
            {
                CopyConfigToNative();
                return BoneParserCLib_ApplyActionConsumeResult(
                    m_NativeHandle,
                    ref m_NativeConfig,
                    consumeResult.m_ActionEventId,
                    (int)consumeResult.m_ResultType) != 0;
            }
            catch (EntryPointNotFoundException)
            {
                ReleaseNativeHandle();
                return false;
            }
        }

        public void Shutdown()
        {
            if (m_NativeHandle != IntPtr.Zero)
            {
                ReleaseNativeHandle();
            }

            ShutdownFallbackRuntime();
            m_HasTriedCreateNative = false;
            ResetFrameResult();
        }

        private void EnsureNativeCreated()
        {
            if (m_HasTriedCreateNative)
            {
                return;
            }

            m_HasTriedCreateNative = true;
            try
            {
                int actualAbiVersion = BoneParserCLib_GetAbiVersion();
                if (actualAbiVersion != m_ExpectedAbiVersion)
                {
                    m_NativeHandle = IntPtr.Zero;
                    LogFallbackOnce(
                        "[塔防骨骼输入] BoneParserCLib ABI 版本不匹配，expected="
                        + m_ExpectedAbiVersion
                        + " actual="
                        + actualAbiVersion
                        + "。");
                    return;
                }

                m_NativeHandle = BoneParserCLib_Create();
                if (BoneParserCLib_IsValid(m_NativeHandle) == 0)
                {
                    ReleaseNativeHandle();
                }
            }
            catch (DllNotFoundException)
            {
                m_NativeHandle = IntPtr.Zero;
            }
            catch (EntryPointNotFoundException)
            {
                m_NativeHandle = IntPtr.Zero;
            }
            catch (BadImageFormatException)
            {
                m_NativeHandle = IntPtr.Zero;
            }
        }

        private void ReleaseNativeHandle()
        {
            if (m_NativeHandle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                BoneParserCLib_Destroy(m_NativeHandle);
            }
            catch (EntryPointNotFoundException)
            {
            }
            finally
            {
                m_NativeHandle = IntPtr.Zero;
            }
        }

        private void ResetFrameResult()
        {
            m_FrameResult.m_FrameSerial = 0;
            m_FrameResult.m_PlayerResults.Clear();
        }

        private BoneParserFrameResult UpdateFallbackRuntime(
            BoneTrackedFrame frameData,
            IList<BoneParserSeatDefinition> seatDefinitions)
        {
#if BoneParserLib
            return m_FallbackRuntime.Update(frameData, seatDefinitions);
#else
            return UpdateUnavailableResult(frameData, seatDefinitions);
#endif
        }

        private bool ApplyFallbackConsumeResult(BoneActionConsumeResult consumeResult)
        {
#if BoneParserLib
            return m_FallbackRuntime.ApplyActionConsumeResult(consumeResult);
#else
            return false;
#endif
        }

        private void ResetFallbackRuntime()
        {
#if BoneParserLib
            m_FallbackRuntime.Reset();
#endif
        }

        private void ShutdownFallbackRuntime()
        {
#if BoneParserLib
            m_FallbackRuntime.Shutdown();
#endif
        }

        private BoneParserFrameResult UpdateUnavailableResult(
            BoneTrackedFrame frameData,
            IList<BoneParserSeatDefinition> seatDefinitions)
        {
            m_FrameResult.m_FrameSerial = frameData != null ? frameData.m_FrameSerial : 0;
            int seatCount = seatDefinitions != null ? seatDefinitions.Count : 0;
            EnsureManagedPlayerResultCount(seatCount);
            for (int i = 0; i < seatCount; i++)
            {
                BoneParserSeatDefinition definition = seatDefinitions[i];
                int slotIndex = definition != null ? definition.m_SlotIndex : i;
                int bindingId = definition != null ? definition.m_BindingId : i;
                m_FrameResult.m_PlayerResults[i].Reset(slotIndex, bindingId);
            }

            return m_FrameResult;
        }

        private void LogFallbackOnce(string message)
        {
            if (m_HasLoggedFallback)
            {
                return;
            }

            m_HasLoggedFallback = true;
#if BoneParserLib
            Debug.LogWarning(message + "动作解析回退到 BoneParserLib。");
#else
            Debug.LogError(message + "当前未包含 BoneParserLib，无法回退。");
#endif
        }

        private void LogNativeFailureOnce(string message)
        {
            if (m_HasLoggedNativeFailure)
            {
                return;
            }

            m_HasLoggedNativeFailure = true;
#if BoneParserLib
            Debug.LogWarning(message + "动作解析回退到 BoneParserLib。");
#else
            Debug.LogError(message + "当前未包含 BoneParserLib，无法回退。");
#endif
        }

        private void CopyConfigToNative()
        {
            m_NativeConfig.m_MinBodyScore = m_Config.m_MinBodyScore;
            m_NativeConfig.m_MinJointScore = m_Config.m_MinJointScore;
            m_NativeConfig.m_MaxMissingFrameCount = m_Config.m_MaxMissingFrameCount;
            m_NativeConfig.m_RotationSmoothFactor = m_Config.m_RotationSmoothFactor;
            m_NativeConfig.m_KeypointConfidenceThreshold = m_Config.m_KeypointConfidenceThreshold;
            m_NativeConfig.m_ShoulderWidthEpsilon = m_Config.m_ShoulderWidthEpsilon;
            m_NativeConfig.m_MaxShoulderWidthUpdateConfidence = m_Config.m_MaxShoulderWidthUpdateConfidence;
            m_NativeConfig.m_MaxTurnAngleDegrees = m_Config.m_MaxTurnAngleDegrees;
            m_NativeConfig.m_InvertTurnDirection = m_Config.m_InvertTurnDirection ? 1 : 0;
            m_NativeConfig.m_RotationAmplifyFactor = m_Config.m_RotationAmplifyFactor;
            m_NativeConfig.m_AimCenterEnterRatio = m_Config.m_AimCenterEnterRatio;
            m_NativeConfig.m_AimCenterExitRatio = m_Config.m_AimCenterExitRatio;
            m_NativeConfig.m_AimResponseCurveExponent = m_Config.m_AimResponseCurveExponent;
            m_NativeConfig.m_ShoulderTurnJitterDeadZone = m_Config.m_ShoulderTurnJitterDeadZone;
            m_NativeConfig.m_AimPredictMissingFrames = m_Config.m_AimPredictMissingFrames;
            m_NativeConfig.m_AimHoldMissingFrames = m_Config.m_AimHoldMissingFrames;
            m_NativeConfig.m_AimReconnectStableFrames = m_Config.m_AimReconnectStableFrames;
            m_NativeConfig.m_AimPredictVelocityDamping = m_Config.m_AimPredictVelocityDamping;
            m_NativeConfig.m_AimReconnectBlendFactor = m_Config.m_AimReconnectBlendFactor;
            m_NativeConfig.m_AimReturnToForwardSeconds = m_Config.m_AimReturnToForwardSeconds;
            m_NativeConfig.m_GestureKeypointMinConfidence = m_Config.m_GestureKeypointMinConfidence;
            m_NativeConfig.m_AlternatingSwingSpeedRatioPerSecond = m_Config.m_AlternatingSwingSpeedRatioPerSecond;
            m_NativeConfig.m_AlternatingSwingMinVerticalDistanceRatio = m_Config.m_AlternatingSwingMinVerticalDistanceRatio;
            m_NativeConfig.m_AlternatingSwingDirectionNoiseRatio = m_Config.m_AlternatingSwingDirectionNoiseRatio;
            m_NativeConfig.m_AlternatingSwingMinDirectionalFrames = m_Config.m_AlternatingSwingMinDirectionalFrames;
            m_NativeConfig.m_AlternatingSwingCooldownSeconds = m_Config.m_AlternatingSwingCooldownSeconds;
            m_NativeConfig.m_AlternatingSwingWindowFrames = m_Config.m_AlternatingSwingWindowFrames;
            m_NativeConfig.m_LargeAlternatingSwingSpeedRatioPerSecond = m_Config.m_LargeAlternatingSwingSpeedRatioPerSecond;
            m_NativeConfig.m_LargeAlternatingSwingDirectionNoiseRatio = m_Config.m_LargeAlternatingSwingDirectionNoiseRatio;
            m_NativeConfig.m_LargeAlternatingSwingMinDirectionalFrames = m_Config.m_LargeAlternatingSwingMinDirectionalFrames;
            m_NativeConfig.m_LargeAlternatingSwingCooldownSeconds = m_Config.m_LargeAlternatingSwingCooldownSeconds;
            m_NativeConfig.m_LargeAlternatingSwingWindowFrames = m_Config.m_LargeAlternatingSwingWindowFrames;
            m_NativeConfig.m_LargeAlternatingSwingMinTorsoDistanceRatio = m_Config.m_LargeAlternatingSwingMinTorsoDistanceRatio;
            m_NativeConfig.m_OverheadPressReadyFrames = m_Config.m_OverheadPressReadyFrames;
            m_NativeConfig.m_OverheadPressHeadMarginRatio = m_Config.m_OverheadPressHeadMarginRatio;
            m_NativeConfig.m_OverheadPressReleaseSpeedRatio = m_Config.m_OverheadPressReleaseSpeedRatio;
            m_NativeConfig.m_OverheadPressMinReleaseDistanceRatio = m_Config.m_OverheadPressMinReleaseDistanceRatio;
            m_NativeConfig.m_OverheadPressReleaseEndBelowHeadRatio = m_Config.m_OverheadPressReleaseEndBelowHeadRatio;
            m_NativeConfig.m_OverheadPressReleaseWindowFrames = m_Config.m_OverheadPressReleaseWindowFrames;
            m_NativeConfig.m_OverheadPressCooldownSeconds = m_Config.m_OverheadPressCooldownSeconds;
            m_NativeConfig.m_CrossChestExpandReadyFrames = m_Config.m_CrossChestExpandReadyFrames;
            m_NativeConfig.m_CrossChestExpandSpeedRatioPerSecond = m_Config.m_CrossChestExpandSpeedRatioPerSecond;
            m_NativeConfig.m_CrossChestExpandMinDistanceRatio = m_Config.m_CrossChestExpandMinDistanceRatio;
            m_NativeConfig.m_CrossChestExpandReleaseWindowFrames = m_Config.m_CrossChestExpandReleaseWindowFrames;
            m_NativeConfig.m_CrossChestExpandCooldownSeconds = m_Config.m_CrossChestExpandCooldownSeconds;
            m_NativeConfig.m_SingleHandPullDownReadyFrames = m_Config.m_SingleHandPullDownReadyFrames;
            m_NativeConfig.m_SingleHandPullDownReleaseSpeedRatioPerSecond = m_Config.m_SingleHandPullDownReleaseSpeedRatioPerSecond;
            m_NativeConfig.m_SingleHandPullDownMinDistanceRatio = m_Config.m_SingleHandPullDownMinDistanceRatio;
            m_NativeConfig.m_SingleHandPullDownEndBelowShoulderRatio = m_Config.m_SingleHandPullDownEndBelowShoulderRatio;
            m_NativeConfig.m_SingleHandPullDownReleaseWindowFrames = m_Config.m_SingleHandPullDownReleaseWindowFrames;
            m_NativeConfig.m_SingleHandPullDownCooldownSeconds = m_Config.m_SingleHandPullDownCooldownSeconds;
            m_NativeConfig.m_HandsOnHipRaiseReadyFrames = m_Config.m_HandsOnHipRaiseReadyFrames;
            m_NativeConfig.m_HandsOnHipRaiseMinDistanceRatio = m_Config.m_HandsOnHipRaiseMinDistanceRatio;
            m_NativeConfig.m_HandsOnHipRaiseEndAboveShoulderRatio = m_Config.m_HandsOnHipRaiseEndAboveShoulderRatio;
            m_NativeConfig.m_HandsOnHipRaiseReleaseWindowFrames = m_Config.m_HandsOnHipRaiseReleaseWindowFrames;
            m_NativeConfig.m_HandsOnHipRaiseCooldownSeconds = m_Config.m_HandsOnHipRaiseCooldownSeconds;
            m_NativeConfig.m_CrouchStandRaiseReadyFrames = m_Config.m_CrouchStandRaiseReadyFrames;
            m_NativeConfig.m_CrouchStandRaiseHandAboveShoulderRatio = m_Config.m_CrouchStandRaiseHandAboveShoulderRatio;
            m_NativeConfig.m_CrouchStandRaiseReleaseWindowFrames = m_Config.m_CrouchStandRaiseReleaseWindowFrames;
            m_NativeConfig.m_CrouchStandRaiseCooldownSeconds = m_Config.m_CrouchStandRaiseCooldownSeconds;
            m_NativeConfig.m_ChestClosePushReadyFrames = m_Config.m_ChestClosePushReadyFrames;
            m_NativeConfig.m_ChestClosePushCloseDistanceRatio = m_Config.m_ChestClosePushCloseDistanceRatio;
            m_NativeConfig.m_ChestClosePushVerticalRatio = m_Config.m_ChestClosePushVerticalRatio;
            m_NativeConfig.m_ChestClosePushMinDistanceRatio = m_Config.m_ChestClosePushMinDistanceRatio;
            m_NativeConfig.m_ChestClosePushEndAboveShoulderRatio = m_Config.m_ChestClosePushEndAboveShoulderRatio;
            m_NativeConfig.m_ChestClosePushSpeedRatioPerSecond = m_Config.m_ChestClosePushSpeedRatioPerSecond;
            m_NativeConfig.m_ChestClosePushReleaseWindowFrames = m_Config.m_ChestClosePushReleaseWindowFrames;
            m_NativeConfig.m_ChestClosePushCooldownSeconds = m_Config.m_ChestClosePushCooldownSeconds;
            m_NativeConfig.m_HandsExpandReadyFrames = m_Config.m_HandsExpandReadyFrames;
            m_NativeConfig.m_HandsExpandCloseDistanceRatio = m_Config.m_HandsExpandCloseDistanceRatio;
            m_NativeConfig.m_HandsExpandCloseVerticalRatio = m_Config.m_HandsExpandCloseVerticalRatio;
            m_NativeConfig.m_HandsExpandBeyondShoulderRatio = m_Config.m_HandsExpandBeyondShoulderRatio;
            m_NativeConfig.m_HandsExpandVerticalToleranceRatio = m_Config.m_HandsExpandVerticalToleranceRatio;
            m_NativeConfig.m_HandsExpandHoldFrames = m_Config.m_HandsExpandHoldFrames;
            m_NativeConfig.m_HandsExpandReleaseWindowFrames = m_Config.m_HandsExpandReleaseWindowFrames;
            m_NativeConfig.m_HandsExpandCooldownSeconds = m_Config.m_HandsExpandCooldownSeconds;
            m_NativeConfig.m_PoseRaiseMarginRatio = m_Config.m_PoseRaiseMarginRatio;
            m_NativeConfig.m_PoseCrossChestCenterRatio = m_Config.m_PoseCrossChestCenterRatio;
            m_NativeConfig.m_PoseCrossChestVerticalRatio = m_Config.m_PoseCrossChestVerticalRatio;
            m_NativeConfig.m_PoseHipAttachRatio = m_Config.m_PoseHipAttachRatio;
            m_NativeConfig.m_PoseHipVerticalRatio = m_Config.m_PoseHipVerticalRatio;
            m_NativeConfig.m_PoseCrouchTorsoRatio = m_Config.m_PoseCrouchTorsoRatio;
            m_NativeConfig.m_PoseStableFrames = m_Config.m_PoseStableFrames;
        }

        private int CopySeatDefinitionsToNative(IList<BoneParserSeatDefinition> seatDefinitions, int seatCount)
        {
            EnsureSeatCapacity(seatCount);
            int actionBindingCount = CountActionBindings(seatDefinitions, seatCount);
            EnsureActionBindingCapacity(actionBindingCount);

            int actionBindingIndex = 0;
            for (int i = 0; i < seatCount; i++)
            {
                BoneParserSeatDefinition definition = seatDefinitions != null ? seatDefinitions[i] : null;
                m_NativeSeatDefinitions[i].m_SlotIndex = definition != null ? definition.m_SlotIndex : i;
                m_NativeSeatDefinitions[i].m_BindingId = definition != null ? definition.m_BindingId : i;
                m_NativeSeatDefinitions[i].m_IsProcessGestureEnabled = definition != null && definition.m_IsProcessGestureEnabled ? 1 : 0;
                m_NativeSeatDefinitions[i].m_ActionBindingStart = actionBindingIndex;
                m_NativeSeatDefinitions[i].m_ActionBindingCount = 0;

                if (definition == null || definition.m_ActionBindings == null)
                {
                    continue;
                }

                int bindingCount = definition.m_ActionBindings.Count;
                for (int bindingIndex = 0; bindingIndex < bindingCount; bindingIndex++)
                {
                    BoneActionBinding binding = definition.m_ActionBindings[bindingIndex];
                    if (binding == null)
                    {
                        continue;
                    }

                    m_NativeActionBindings[actionBindingIndex].m_ActionId = binding.m_ActionId;
                    m_NativeActionBindings[actionBindingIndex].m_GestureType = (int)binding.m_GestureType;
                    m_NativeActionBindings[actionBindingIndex].m_PhaseMask = (int)binding.m_PhaseMask;
                    m_NativeActionBindings[actionBindingIndex].m_ConsumerType = (int)binding.m_ConsumerType;
                    m_NativeActionBindings[actionBindingIndex].m_ConsumerValue = binding.m_ConsumerValue;
                    m_NativeActionBindings[actionBindingIndex].m_RuntimeFlags = (int)binding.m_RuntimeFlags;
                    m_NativeActionBindings[actionBindingIndex].m_RequiresConsumeResult = binding.m_RequiresConsumeResult ? 1 : 0;
                    actionBindingIndex++;
                    m_NativeSeatDefinitions[i].m_ActionBindingCount++;
                }
            }

            return actionBindingIndex;
        }

        private static int CountActionBindings(IList<BoneParserSeatDefinition> seatDefinitions, int seatCount)
        {
            int count = 0;
            for (int i = 0; i < seatCount; i++)
            {
                BoneParserSeatDefinition definition = seatDefinitions != null ? seatDefinitions[i] : null;
                if (definition == null || definition.m_ActionBindings == null)
                {
                    continue;
                }

                int bindingCount = definition.m_ActionBindings.Count;
                for (int bindingIndex = 0; bindingIndex < bindingCount; bindingIndex++)
                {
                    if (definition.m_ActionBindings[bindingIndex] != null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void CopyFrameToNative(BoneTrackedFrame frameData, int personCount)
        {
            EnsurePersonCapacity(personCount);
            m_NativeFrameInput.m_HasFrameData = frameData != null && frameData.m_HasFrameData ? 1 : 0;
            m_NativeFrameInput.m_FrameSerial = frameData != null ? frameData.m_FrameSerial : 0;
            m_NativeFrameInput.m_IsSimulated = frameData != null && frameData.m_IsSimulated ? 1 : 0;
            m_NativeFrameInput.m_FrameTimeMs = frameData != null ? frameData.m_FrameTimeMs : 0L;
            m_NativeFrameInput.m_ImageWidth = frameData != null ? frameData.m_ImageWidth : 0;
            m_NativeFrameInput.m_ImageHeight = frameData != null ? frameData.m_ImageHeight : 0;
            m_NativeFrameInput.m_PersonCount = personCount;
            m_NativeFrameInput.m_Persons = IntPtr.Zero;

            for (int i = 0; i < personCount; i++)
            {
                BoneTrackedPerson person = frameData.m_Persons[i];
                CopyPersonToNative(person, i);
            }
        }

        private void CopyPersonToNative(BoneTrackedPerson person, int personIndex)
        {
            int jointStart = personIndex * m_BodyJointCount;
            m_NativePersons[personIndex].m_PersonId = person != null ? person.m_PersonId : -1;
            m_NativePersons[personIndex].m_BodyScore = person != null && person.m_Body != null ? person.m_Body.m_Score : 0f;
            m_NativePersons[personIndex].m_BodyType = person != null && person.m_Body != null ? person.m_Body.m_Type : 0;
            CopyRectToNative(person != null && person.m_Body != null ? person.m_Body.m_Rect : null, ref m_NativePersons[personIndex].m_BodyRect);
            m_NativePersons[personIndex].m_BodyJoints = IntPtr.Zero;
            m_NativePersons[personIndex].m_BodyJointCount = m_BodyJointCount;

            for (int i = 0; i < m_BodyJointCount; i++)
            {
                BoneTrackedJoint joint = person != null &&
                    person.m_Body != null &&
                    person.m_Body.m_Joints != null &&
                    i < person.m_Body.m_Joints.Length
                        ? person.m_Body.m_Joints[i]
                        : null;
                CopyJointToNative(joint, ref m_NativeBodyJoints[jointStart + i]);
            }
        }

        private static void CopyRectToNative(BoneTrackedRect source, ref BoneParserCLibRect target)
        {
            target.m_IsValid = source != null && source.m_IsValid ? 1 : 0;
            target.m_Left = source != null ? source.m_Left : 0f;
            target.m_Top = source != null ? source.m_Top : 0f;
            target.m_Right = source != null ? source.m_Right : 0f;
            target.m_Bottom = source != null ? source.m_Bottom : 0f;
        }

        private static void CopyJointToNative(BoneTrackedJoint source, ref BoneParserCLibJoint target)
        {
            target.m_IsTracked = source != null && source.m_IsTracked ? 1 : 0;
            target.m_X = source != null ? source.m_X : 0f;
            target.m_Y = source != null ? source.m_Y : 0f;
            target.m_Z = source != null ? source.m_Z : 0f;
            target.m_Score = source != null ? source.m_Score : 0f;
        }

        private void AssignBodyJointPointers(IntPtr bodyJointBaseAddress, int personCount)
        {
            for (int i = 0; i < personCount; i++)
            {
                m_NativePersons[i].m_BodyJoints = IntPtr.Add(bodyJointBaseAddress, i * m_BodyJointCount * m_NativeJointSize);
            }
        }

        private void EnsureSeatCapacity(int count)
        {
            int capacity = Math.Max(m_MinNativeArrayCapacity, count);
            if (m_NativeSeatDefinitions.Length < capacity)
            {
                m_NativeSeatDefinitions = new BoneParserCLibSeatDefinition[capacity];
            }
        }

        private void EnsureActionBindingCapacity(int count)
        {
            int capacity = Math.Max(m_MinNativeArrayCapacity, count);
            if (m_NativeActionBindings.Length < capacity)
            {
                m_NativeActionBindings = new BoneParserCLibActionBinding[capacity];
            }
        }

        private void EnsurePersonCapacity(int personCount)
        {
            int personCapacity = Math.Max(m_MinNativeArrayCapacity, personCount);
            if (m_NativePersons.Length < personCapacity)
            {
                m_NativePersons = new BoneParserCLibPerson[personCapacity];
            }

            int jointCapacity = Math.Max(m_MinNativeArrayCapacity, personCapacity * m_BodyJointCount);
            if (m_NativeBodyJoints.Length < jointCapacity)
            {
                m_NativeBodyJoints = new BoneParserCLibJoint[jointCapacity];
            }
        }

        private void EnsureOutputCapacity(int seatCount)
        {
            int playerCapacity = Math.Max(m_MinNativeArrayCapacity, seatCount);
            if (m_NativePlayerResults.Length < playerCapacity)
            {
                m_NativePlayerResults = new BoneParserCLibPlayerResult[playerCapacity];
            }

            int eventCapacity = Math.Max(m_MinNativeArrayCapacity, seatCount * m_MaxNativeEventsPerSeat);
            if (m_NativeGestureEvents.Length < eventCapacity)
            {
                m_NativeGestureEvents = new BoneParserCLibGestureEvent[eventCapacity];
            }

            if (m_NativeActionEvents.Length < eventCapacity)
            {
                m_NativeActionEvents = new BoneParserCLibActionEvent[eventCapacity];
            }
        }

        private void CopyNativeResultToManaged(BoneParserCLibFrameOutput frameOutput)
        {
            m_FrameResult.m_FrameSerial = frameOutput.m_FrameSerial;
            EnsureManagedPlayerResultCount(frameOutput.m_PlayerResultCount);

            for (int i = 0; i < frameOutput.m_PlayerResultCount; i++)
            {
                BoneParserCLibPlayerResult nativeResult = m_NativePlayerResults[i];
                BoneParserPlayerResult managedResult = m_FrameResult.m_PlayerResults[i];
                managedResult.Reset(nativeResult.m_SlotIndex, nativeResult.m_BindingId);
                managedResult.m_IsTracked = nativeResult.m_IsTracked != 0;
                managedResult.m_IsAimAvailable = nativeResult.m_IsAimAvailable != 0;
                managedResult.m_AimTrackingState = (BoneAimTrackingState)nativeResult.m_AimTrackingState;
                managedResult.m_AimConfidence = nativeResult.m_AimConfidence;
                managedResult.m_MissingFrameCount = nativeResult.m_MissingFrameCount;
                managedResult.m_PersonId = nativeResult.m_PersonId;
                managedResult.m_FaceForward = ToBoneVector3(nativeResult.m_FaceForward);
                managedResult.m_TurnSpeed = nativeResult.m_TurnSpeed;
                managedResult.m_TurnStrength = nativeResult.m_TurnStrength;
                managedResult.m_TurnAngleDegrees = nativeResult.m_TurnAngleDegrees;
                managedResult.m_ActivePoseHints = (BonePoseHintFlags)nativeResult.m_ActivePoseHints;
                CopyGestureEventsToManaged(nativeResult, managedResult);
                CopyActionEventsToManaged(nativeResult, managedResult);
            }
        }

        private void EnsureManagedPlayerResultCount(int count)
        {
            while (m_FrameResult.m_PlayerResults.Count < count)
            {
                m_FrameResult.m_PlayerResults.Add(new BoneParserPlayerResult());
            }

            while (m_FrameResult.m_PlayerResults.Count > count)
            {
                m_FrameResult.m_PlayerResults.RemoveAt(m_FrameResult.m_PlayerResults.Count - 1);
            }
        }

        private void CopyGestureEventsToManaged(
            BoneParserCLibPlayerResult nativeResult,
            BoneParserPlayerResult managedResult)
        {
            int eventEnd = nativeResult.m_GestureEventStart + nativeResult.m_GestureEventCount;
            for (int i = nativeResult.m_GestureEventStart; i < eventEnd && i < m_NativeGestureEvents.Length; i++)
            {
                BoneParserCLibGestureEvent nativeEvent = m_NativeGestureEvents[i];
                managedResult.m_GestureEvents.Add(new BoneGestureEvent
                {
                    m_GestureType = (BoneGestureType)nativeEvent.m_GestureType,
                    m_Phase = (BoneGesturePhase)nativeEvent.m_Phase,
                    m_SlotIndex = nativeEvent.m_SlotIndex,
                    m_BindingId = nativeEvent.m_BindingId,
                    m_PersonId = nativeEvent.m_PersonId,
                    m_FrameSerial = nativeEvent.m_FrameSerial,
                });
            }
        }

        private void CopyActionEventsToManaged(
            BoneParserCLibPlayerResult nativeResult,
            BoneParserPlayerResult managedResult)
        {
            int eventEnd = nativeResult.m_ActionEventStart + nativeResult.m_ActionEventCount;
            for (int i = nativeResult.m_ActionEventStart; i < eventEnd && i < m_NativeActionEvents.Length; i++)
            {
                BoneParserCLibActionEvent nativeEvent = m_NativeActionEvents[i];
                managedResult.m_ActionEvents.Add(new BoneActionEvent
                {
                    m_ActionEventId = nativeEvent.m_ActionEventId,
                    m_ActionId = nativeEvent.m_ActionId,
                    m_GestureType = (BoneGestureType)nativeEvent.m_GestureType,
                    m_Phase = (BoneGesturePhase)nativeEvent.m_Phase,
                    m_SlotIndex = nativeEvent.m_SlotIndex,
                    m_BindingId = nativeEvent.m_BindingId,
                    m_PersonId = nativeEvent.m_PersonId,
                    m_FrameSerial = nativeEvent.m_FrameSerial,
                    m_ConsumerType = (BoneActionConsumerType)nativeEvent.m_ConsumerType,
                    m_ConsumerValue = nativeEvent.m_ConsumerValue,
                    m_RuntimeFlags = (BoneActionRuntimeFlags)nativeEvent.m_RuntimeFlags,
                    m_RequiresConsumeResult = nativeEvent.m_RequiresConsumeResult != 0,
                    m_FaceForward = ToBoneVector3(nativeEvent.m_FaceForward),
                    m_MoveDirection = ToBoneVector3(nativeEvent.m_MoveDirection),
                });
            }
        }

        private static BoneVector3 ToBoneVector3(BoneParserCLibVector3 value)
        {
            return new BoneVector3(value.m_X, value.m_Y, value.m_Z);
        }

        [DllImport("BoneParserCLib", CallingConvention = CallingConvention.Cdecl)]
        private static extern int BoneParserCLib_GetAbiVersion();

        [DllImport("BoneParserCLib", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr BoneParserCLib_Create();

        [DllImport("BoneParserCLib", CallingConvention = CallingConvention.Cdecl)]
        private static extern void BoneParserCLib_Reset(IntPtr parser);

        [DllImport("BoneParserCLib", CallingConvention = CallingConvention.Cdecl)]
        private static extern void BoneParserCLib_Destroy(IntPtr parser);

        [DllImport("BoneParserCLib", CallingConvention = CallingConvention.Cdecl)]
        private static extern int BoneParserCLib_IsValid(IntPtr parser);

        [DllImport("BoneParserCLib", CallingConvention = CallingConvention.Cdecl)]
        private static extern int BoneParserCLib_Update(
            IntPtr parser,
            ref BoneParserCLibConfig config,
            ref BoneParserCLibFrameInput frameInput,
            [In] BoneParserCLibSeatDefinition[] seatDefinitions,
            int seatDefinitionCount,
            [In] BoneParserCLibActionBinding[] actionBindings,
            int actionBindingCount,
            [Out] BoneParserCLibPlayerResult[] playerResults,
            int playerResultCapacity,
            [Out] BoneParserCLibGestureEvent[] gestureEvents,
            int gestureEventCapacity,
            [Out] BoneParserCLibActionEvent[] actionEvents,
            int actionEventCapacity,
            out BoneParserCLibFrameOutput frameOutput);

        [DllImport("BoneParserCLib", CallingConvention = CallingConvention.Cdecl)]
        private static extern int BoneParserCLib_ApplyActionConsumeResult(
            IntPtr parser,
            ref BoneParserCLibConfig config,
            int actionEventId,
            int resultType);

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibVector3
        {
            public float m_X;
            public float m_Y;
            public float m_Z;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibConfig
        {
            public float m_MinBodyScore;
            public float m_MinJointScore;
            public int m_MaxMissingFrameCount;
            public float m_RotationSmoothFactor;
            public float m_KeypointConfidenceThreshold;
            public float m_ShoulderWidthEpsilon;
            public float m_MaxShoulderWidthUpdateConfidence;
            public float m_MaxTurnAngleDegrees;
            public int m_InvertTurnDirection;
            public float m_RotationAmplifyFactor;
            public float m_AimCenterEnterRatio;
            public float m_AimCenterExitRatio;
            public float m_AimResponseCurveExponent;
            public float m_ShoulderTurnJitterDeadZone;
            public int m_AimPredictMissingFrames;
            public int m_AimHoldMissingFrames;
            public int m_AimReconnectStableFrames;
            public float m_AimPredictVelocityDamping;
            public float m_AimReconnectBlendFactor;
            public float m_AimReturnToForwardSeconds;
            public float m_GestureKeypointMinConfidence;
            public float m_AlternatingSwingSpeedRatioPerSecond;
            public float m_AlternatingSwingMinVerticalDistanceRatio;
            public float m_AlternatingSwingDirectionNoiseRatio;
            public int m_AlternatingSwingMinDirectionalFrames;
            public float m_AlternatingSwingCooldownSeconds;
            public int m_AlternatingSwingWindowFrames;
            public float m_LargeAlternatingSwingSpeedRatioPerSecond;
            public float m_LargeAlternatingSwingDirectionNoiseRatio;
            public int m_LargeAlternatingSwingMinDirectionalFrames;
            public float m_LargeAlternatingSwingCooldownSeconds;
            public int m_LargeAlternatingSwingWindowFrames;
            public float m_LargeAlternatingSwingMinTorsoDistanceRatio;
            public int m_OverheadPressReadyFrames;
            public float m_OverheadPressHeadMarginRatio;
            public float m_OverheadPressReleaseSpeedRatio;
            public float m_OverheadPressMinReleaseDistanceRatio;
            public float m_OverheadPressReleaseEndBelowHeadRatio;
            public int m_OverheadPressReleaseWindowFrames;
            public float m_OverheadPressCooldownSeconds;
            public int m_CrossChestExpandReadyFrames;
            public float m_CrossChestExpandSpeedRatioPerSecond;
            public float m_CrossChestExpandMinDistanceRatio;
            public int m_CrossChestExpandReleaseWindowFrames;
            public float m_CrossChestExpandCooldownSeconds;
            public int m_SingleHandPullDownReadyFrames;
            public float m_SingleHandPullDownReleaseSpeedRatioPerSecond;
            public float m_SingleHandPullDownMinDistanceRatio;
            public float m_SingleHandPullDownEndBelowShoulderRatio;
            public int m_SingleHandPullDownReleaseWindowFrames;
            public float m_SingleHandPullDownCooldownSeconds;
            public int m_HandsOnHipRaiseReadyFrames;
            public float m_HandsOnHipRaiseMinDistanceRatio;
            public float m_HandsOnHipRaiseEndAboveShoulderRatio;
            public int m_HandsOnHipRaiseReleaseWindowFrames;
            public float m_HandsOnHipRaiseCooldownSeconds;
            public int m_CrouchStandRaiseReadyFrames;
            public float m_CrouchStandRaiseHandAboveShoulderRatio;
            public int m_CrouchStandRaiseReleaseWindowFrames;
            public float m_CrouchStandRaiseCooldownSeconds;
            public int m_ChestClosePushReadyFrames;
            public float m_ChestClosePushCloseDistanceRatio;
            public float m_ChestClosePushVerticalRatio;
            public float m_ChestClosePushMinDistanceRatio;
            public float m_ChestClosePushEndAboveShoulderRatio;
            public float m_ChestClosePushSpeedRatioPerSecond;
            public int m_ChestClosePushReleaseWindowFrames;
            public float m_ChestClosePushCooldownSeconds;
            public int m_HandsExpandReadyFrames;
            public float m_HandsExpandCloseDistanceRatio;
            public float m_HandsExpandCloseVerticalRatio;
            public float m_HandsExpandBeyondShoulderRatio;
            public float m_HandsExpandVerticalToleranceRatio;
            public int m_HandsExpandHoldFrames;
            public int m_HandsExpandReleaseWindowFrames;
            public float m_HandsExpandCooldownSeconds;
            public float m_PoseRaiseMarginRatio;
            public float m_PoseCrossChestCenterRatio;
            public float m_PoseCrossChestVerticalRatio;
            public float m_PoseHipAttachRatio;
            public float m_PoseHipVerticalRatio;
            public float m_PoseCrouchTorsoRatio;
            public int m_PoseStableFrames;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibJoint
        {
            public int m_IsTracked;
            public float m_X;
            public float m_Y;
            public float m_Z;
            public float m_Score;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibRect
        {
            public int m_IsValid;
            public float m_Left;
            public float m_Top;
            public float m_Right;
            public float m_Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibPerson
        {
            public int m_PersonId;
            public float m_BodyScore;
            public int m_BodyType;
            public BoneParserCLibRect m_BodyRect;
            public IntPtr m_BodyJoints;
            public int m_BodyJointCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibFrameInput
        {
            public int m_HasFrameData;
            public int m_FrameSerial;
            public int m_IsSimulated;
            public long m_FrameTimeMs;
            public int m_ImageWidth;
            public int m_ImageHeight;
            public int m_PersonCount;
            public IntPtr m_Persons;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibActionBinding
        {
            public int m_ActionId;
            public int m_GestureType;
            public int m_PhaseMask;
            public int m_ConsumerType;
            public int m_ConsumerValue;
            public int m_RuntimeFlags;
            public int m_RequiresConsumeResult;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibSeatDefinition
        {
            public int m_SlotIndex;
            public int m_BindingId;
            public int m_IsProcessGestureEnabled;
            public int m_ActionBindingStart;
            public int m_ActionBindingCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibGestureEvent
        {
            public int m_GestureType;
            public int m_Phase;
            public int m_SlotIndex;
            public int m_BindingId;
            public int m_PersonId;
            public int m_FrameSerial;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibActionEvent
        {
            public int m_ActionEventId;
            public int m_ActionId;
            public int m_GestureType;
            public int m_Phase;
            public int m_SlotIndex;
            public int m_BindingId;
            public int m_PersonId;
            public int m_FrameSerial;
            public int m_ConsumerType;
            public int m_ConsumerValue;
            public int m_RuntimeFlags;
            public int m_RequiresConsumeResult;
            public BoneParserCLibVector3 m_FaceForward;
            public BoneParserCLibVector3 m_MoveDirection;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibPlayerResult
        {
            public int m_SlotIndex;
            public int m_BindingId;
            public int m_IsTracked;
            public int m_IsAimAvailable;
            public int m_AimTrackingState;
            public float m_AimConfidence;
            public int m_MissingFrameCount;
            public int m_PersonId;
            public BoneParserCLibVector3 m_FaceForward;
            public float m_TurnSpeed;
            public float m_TurnStrength;
            public float m_TurnAngleDegrees;
            public int m_ActivePoseHints;
            public int m_GestureEventStart;
            public int m_GestureEventCount;
            public int m_ActionEventStart;
            public int m_ActionEventCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BoneParserCLibFrameOutput
        {
            public int m_FrameSerial;
            public int m_PlayerResultCount;
            public int m_GestureEventCount;
            public int m_ActionEventCount;
        }
    }
}
