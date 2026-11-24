using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    public static class TowerDefendBoneAttackTuning
    {
        // 同一角色双手共享的最短普攻间隔，单位为秒。
        public static float m_MinAttackIntervalSecond = 0.25f;
        // 动画速度提升的平滑时间，单位为秒。
        public const float m_AnimationAccelerationSecond = 0.1f;
        // 动画速度恢复的平滑时间，单位为秒。
        public const float m_AnimationRecoverySecond = 0.25f;
        // 无新普攻输入后开始恢复动画速度的等待时间，单位为秒。
        public const float m_AnimationRecoveryDelaySecond = 0.5f;
        // 普攻动画衔接的混合时间，单位为秒。
        public const float m_AnimationBlendSecond = 0.05f;

        public static float ReadMinAttackInterval()
        {
            return Mathf.Max(0.01f, m_MinAttackIntervalSecond);
        }
    }

    public class TowerDefendBoneInputDriver
    {
        private const int m_BoneInputDebugIntervalFrames = 60;
        // 过滤平滑回正产生的极小残值，低于最大转角的 1% 视为已经停止转身。
        private const float m_NormalAttackSuppressedTurnStrength = 0.01f;
        private readonly BoneParserConfig m_Config = new BoneParserConfig();
        private readonly IBoneParserRuntime m_BoneParserRuntime;
        private readonly BoneParserFrameAdapter m_FrameAdapter = new BoneParserFrameAdapter();
        private readonly List<BoneParserSeatDefinition> m_SeatDefinitions = new List<BoneParserSeatDefinition>();
        private readonly Stack<BoneActionBinding> m_RecycledActionBindings = new Stack<BoneActionBinding>();
        private readonly int[] m_LastDebugLogFrames = new int[TowerDefendSeatLayout.MaxSupportedPlayerCount];
        private readonly bool[] m_IsTurningBySeat = new bool[TowerDefendSeatLayout.MaxSupportedPlayerCount];
        private readonly int[] m_LastOperablePersonIds = new int[TowerDefendSeatLayout.MaxSupportedPlayerCount];
        private readonly bool[] m_WasOperableBySeat = new bool[TowerDefendSeatLayout.MaxSupportedPlayerCount];
        private readonly bool[] m_HasLoggedAcceptedActionBySeat = new bool[TowerDefendSeatLayout.MaxSupportedPlayerCount];

        private TowerDefendBattleScene m_Scene;
        private IBoneFrameSource m_FrameSource;
        private bool m_WasUsingAuthoritativeSlots;
        private bool m_HasSeatDefinitions;
        private bool m_HasLoggedRuntime;
        private BoneParserFrameResult m_CachedFrameResult;
        private int? m_LastFrameworkFrameSerial;
        private bool m_HadFrameworkFrame;

        public TowerDefendBoneInputDriver()
        {
            m_BoneParserRuntime = BoneParserRuntimeFactory.Create(m_Config);
        }

        public void Init(TowerDefendBattleScene scene, IBoneFrameSource frameSource)
        {
            m_Scene = scene;
            m_FrameSource = frameSource;
            m_HasSeatDefinitions = false;
            m_SeatDefinitions.Clear();
            ResetSeatTrackingState();
            ResetTurningStates();
            ResetParser();
            LogRuntimeOnce();
        }

        public void Update()
        {
            if (m_Scene == null || m_FrameSource == null)
            {
                return;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return;
            }

            if (battle.IsBattlePause())
            {
                if (m_CachedFrameResult != null || m_HadFrameworkFrame)
                {
                    ResetTurningStates();
                    ResetParser();
                }
                return;
            }

            if (!EnsureSeatDefinitions(battle))
            {
                return;
            }

            RefreshSeatSdkSlotBindings();
            m_FrameSource.Tick();
            var sourceFrame = m_FrameSource.ReadLatestFrameData();
            bool frameworkFrame = sourceFrame != null && sourceFrame.m_HasAuthoritativeSlotBindings;
            bool isNewFrame = !frameworkFrame || m_LastFrameworkFrameSerial != sourceFrame.m_FrameSerial;
            BoneTrackedFrame parserFrame = null;
            if (isNewFrame)
            {
                if (frameworkFrame && m_HadFrameworkFrame && !sourceFrame.m_HasFrameData)
                {
                    ResetParser();
                }

                SynchronizeParserSeats(sourceFrame);
                ApplyRuntimeConfig();
                RefreshSeatActionBindings(battle);
                parserFrame = m_FrameAdapter.Convert(sourceFrame);
                m_CachedFrameResult = m_BoneParserRuntime.Update(parserFrame, m_SeatDefinitions);
                m_LastFrameworkFrameSerial = frameworkFrame ? (int?)sourceFrame.m_FrameSerial : null;
                m_HadFrameworkFrame = frameworkFrame && sourceFrame.m_HasFrameData;
            }

            var frameResult = m_CachedFrameResult;
            if (frameResult == null)
            {
                ResetTurningStates();
                return;
            }

            ResetTurningStates();
            int resultCount = frameResult.m_PlayerResults.Count;
            for (int i = 0; i < resultCount && i < m_SeatDefinitions.Count; i++)
            {
                var playerResult = frameResult.m_PlayerResults[i];
                var seatDefinition = m_SeatDefinitions[i];
                if (seatDefinition == null)
                {
                    continue;
                }

                int seatId = seatDefinition.m_BindingId;
                if (battle.ReadPlayerBySeat(seatId) == null)
                {
                    RefreshHudBoneDebugInfo(seatId, seatDefinition.m_SlotIndex, null);
                    continue;
                }

                if (isNewFrame)
                {
                    RefreshHudBoneDebugInfo(seatId, seatDefinition.m_SlotIndex, playerResult);
                    LogParserOperability(seatId, seatDefinition.m_SlotIndex, playerResult);
                }

                if (playerResult == null || !playerResult.m_IsAimAvailable)
                {
                    if (isNewFrame)
                    {
                        LogMissingTrackedFrame(parserFrame, playerResult, seatId, seatDefinition.m_SlotIndex);
                    }

                    continue;
                }

                Vector3 worldFaceForward = ResolveWorldFaceForward(ToUnityVector3(playerResult.m_FaceForward));
                Vector3 moveDirection = NormalizeHorizontalOrDefault(worldFaceForward, Vector3.forward);
                Vector3 faceForward = battle.UpdateBoneAimForwardBySeat(seatId, worldFaceForward);
                if (!playerResult.m_IsTracked)
                {
                    if (isNewFrame)
                    {
                        LogMissingTrackedFrame(parserFrame, playerResult, seatId, seatDefinition.m_SlotIndex);
                    }

                    continue;
                }

                m_IsTurningBySeat[seatId] = Mathf.Abs(playerResult.m_TurnStrength) > m_NormalAttackSuppressedTurnStrength;
                if (isNewFrame)
                {
                    DispatchActionEvents(playerResult, seatId, faceForward, moveDirection);
                }
            }
        }

        public void Destroy()
        {
            m_FrameSource?.Shutdown();
            m_Scene = null;
            m_FrameSource = null;
            m_HasSeatDefinitions = false;
            m_SeatDefinitions.Clear();
            ResetSeatTrackingState();
            ResetTurningStates();
            m_CachedFrameResult = null;
            m_BoneParserRuntime.Shutdown();
            RenderEvent.Event.OnTowerDefendBoneDebugInfosCleared();
        }

        private void LogRuntimeOnce()
        {
            if (m_HasLoggedRuntime)
            {
                return;
            }

            m_HasLoggedRuntime = true;
            Debug.Log("[塔防骨骼输入] 骨骼解析运行时：" + m_BoneParserRuntime.RuntimeName);
        }

        private void ResetParser()
        {
            m_BoneParserRuntime.Reset();
            m_CachedFrameResult = null;
            m_LastFrameworkFrameSerial = null;
            m_HadFrameworkFrame = false;
        }

        private void ApplyRuntimeConfig()
        {
            BoneTurnTuning.ApplyTo(m_Config);
            BoneGestureTuning.ApplyTo(m_Config);
        }

        private bool EnsureSeatDefinitions(TowerDefendBattle battle)
        {
            int activePlayerCount = CountActivePlayers(battle);
            if (activePlayerCount <= 0)
            {
                m_HasSeatDefinitions = false;
                m_SeatDefinitions.Clear();
                ResetTurningStates();
                ResetParser();
                return false;
            }

            bool shouldRebuild = !m_HasSeatDefinitions || m_SeatDefinitions.Count != TowerDefendSeatLayout.MaxSupportedPlayerCount;
            if (shouldRebuild)
            {
                RebuildSeatDefinitions();
            }

            return m_SeatDefinitions.Count > 0;
        }

        private void RefreshSeatSdkSlotBindings()
        {
            bool hasChanged = false;
            for (int i = 0; i < m_SeatDefinitions.Count; i++)
            {
                var definition = m_SeatDefinitions[i];
                if (definition == null)
                {
                    continue;
                }

                int seatId = definition.m_BindingId;
                int sdkSlotIndex = -1;
                if (m_Scene == null || !m_Scene.TryGetSdkSlotIndexBySeat(seatId, out sdkSlotIndex))
                {
                    sdkSlotIndex = -1;
                }

                if (definition.m_SlotIndex == sdkSlotIndex)
                {
                    continue;
                }

                Debug.Log(
                    "[塔防骨骼输入] SDK 槽位绑定变化。 seat=" + seatId +
                    " oldSdkSlot=" + definition.m_SlotIndex +
                    " newSdkSlot=" + sdkSlotIndex);
                definition.m_SlotIndex = sdkSlotIndex;
                hasChanged = true;
            }

            if (!hasChanged)
            {
                return;
            }

            ResetSeatTrackingState();
            ResetTurningStates();
            ResetParser();
        }

        private void SynchronizeParserSeats(BoneFrameData frame)
        {
            if (frame == null)
            {
                return;
            }

            if (frame.m_HasAuthoritativeSlotBindings != m_WasUsingAuthoritativeSlots)
            {
                m_WasUsingAuthoritativeSlots = frame.m_HasAuthoritativeSlotBindings;
                ResetSeatTrackingState();
                ResetTurningStates();
                m_BoneParserRuntime.Reset();
            }

            if (!frame.m_HasAuthoritativeSlotBindings)
            {
                return;
            }

            for (int index = 0; index < m_SeatDefinitions.Count; index++)
            {
                var definition = m_SeatDefinitions[index];
                int slot = definition.m_SlotIndex;
                if (slot < 0 || slot >= frame.m_Persons.Count)
                {
                    continue;
                }

                int personId = frame.m_Persons[slot].m_PersonId;
                if (personId < 0)
                {
                    continue;
                }

                int seat = definition.m_BindingId;
                int previousPersonId = m_LastOperablePersonIds[seat];
                if (personId == previousPersonId)
                {
                    continue;
                }

                m_LastOperablePersonIds[seat] = personId;
                if (previousPersonId >= 0)
                {
                    m_BoneParserRuntime.ResetSeat(index);
                    m_IsTurningBySeat[seat] = false;
                    m_WasOperableBySeat[seat] = false;
                    m_HasLoggedAcceptedActionBySeat[seat] = false;
                    Debug.Log("[塔防骨骼输入] 正式换绑 seat=" + seat + " sdkSlot=" + slot +
                              " oldPerson=" + previousPersonId + " newPerson=" + personId +
                              " frame=" + frame.m_FrameSerial + " time=" + Time.unscaledTime.ToString("F1"));
                }
            }
        }

        private void LogParserOperability(int seatId, int sdkSlot, BoneParserPlayerResult result)
        {
            bool operable = result != null && result.m_IsTracked && result.m_IsAimAvailable;
            if (m_WasOperableBySeat[seatId] == operable)
            {
                return;
            }

            m_WasOperableBySeat[seatId] = operable;
            if (operable)
            {
                Debug.Log("[塔防骨骼输入] 解析已可用 seat=" + seatId + " sdkSlot=" + sdkSlot +
                          " person=" + result.m_PersonId + " time=" + Time.unscaledTime.ToString("F1"));
            }
            else
            {
                m_HasLoggedAcceptedActionBySeat[seatId] = false;
                m_LastDebugLogFrames[seatId] = Time.frameCount - m_BoneInputDebugIntervalFrames;
            }
        }

        private void ResetSeatTrackingState()
        {
            for (int seatId = 0; seatId < TowerDefendSeatLayout.MaxSupportedPlayerCount; seatId++)
            {
                m_LastDebugLogFrames[seatId] = -m_BoneInputDebugIntervalFrames;
                m_LastOperablePersonIds[seatId] = BoneTrackedLayout.m_InvalidPersonId;
                m_WasOperableBySeat[seatId] = false;
                m_HasLoggedAcceptedActionBySeat[seatId] = false;
            }
        }

        private void ResetTurningStates()
        {
            for (int seatId = 0; seatId < m_IsTurningBySeat.Length; seatId++)
            {
                m_IsTurningBySeat[seatId] = false;
            }
        }

        private static void RefreshHudBoneDebugInfo(
            int seatId,
            int sdkSlotIndex,
            BoneParserPlayerResult playerResult)
        {
            RenderEvent.Event.OnTowerDefendBoneDebugInfoChanged(seatId, playerResult, sdkSlotIndex);
        }

        private void LogMissingTrackedFrame(
            BoneTrackedFrame parserFrame,
            BoneParserPlayerResult playerResult,
            int seatId,
            int sdkSlotIndex)
        {
            if (!TowerDefendSeatLayout.IsValidSeatId(seatId))
            {
                return;
            }

            int frameCount = Time.frameCount;
            if (frameCount - m_LastDebugLogFrames[seatId] < m_BoneInputDebugIntervalFrames)
            {
                return;
            }

            m_LastDebugLogFrames[seatId] = frameCount;
            Debug.Log(
                "[塔防骨骼输入] 当前帧未跟踪到有效玩家。 seat=" + seatId +
                " sdkSlot=" + sdkSlotIndex +
                " aimAvailable=" + (playerResult != null && playerResult.m_IsAimAvailable) +
                " aimState=" + (playerResult != null ? playerResult.m_AimTrackingState.ToString() : "none") +
                " aimConfidence=" + (playerResult != null ? playerResult.m_AimConfidence.ToString("0.00") : "0.00") +
                " missingFrames=" + (playerResult != null ? playerResult.m_MissingFrameCount : 0) +
                " resultTracked=" + (playerResult != null && playerResult.m_IsTracked) +
                " frame=" + (parserFrame != null ? parserFrame.m_FrameSerial : 0) +
                " time=" + Time.unscaledTime.ToString("F1") +
                " " + BuildTrackedPersonDebugText(parserFrame, sdkSlotIndex, m_Config.m_KeypointConfidenceThreshold));
        }

        private static string BuildTrackedPersonDebugText(
            BoneTrackedFrame parserFrame,
            int sdkSlotIndex,
            float keypointConfidenceThreshold)
        {
            if (parserFrame == null)
            {
                return "frame=null";
            }

            if (!parserFrame.m_HasFrameData)
            {
                return "frameData=false";
            }

            if (parserFrame.m_Persons == null || sdkSlotIndex < 0 || sdkSlotIndex >= parserFrame.m_Persons.Count)
            {
                int personCount = parserFrame.m_Persons != null ? parserFrame.m_Persons.Count : 0;
                return "person=none personCount=" + personCount;
            }

            BoneTrackedPerson person = parserFrame.m_Persons[sdkSlotIndex];
            if (person == null)
            {
                return "person=null";
            }

            return "personId=" + person.m_PersonId +
                " bodyScore=" + person.m_Body.m_Score.ToString("0.00") +
                " leftShoulder=" + BuildJointDebugText(person, BoneBodyJointType.左肩, keypointConfidenceThreshold) +
                " rightShoulder=" + BuildJointDebugText(person, BoneBodyJointType.右肩, keypointConfidenceThreshold) +
                " nose=" + BuildJointDebugText(person, BoneBodyJointType.鼻尖, keypointConfidenceThreshold);
        }

        private static string BuildJointDebugText(
            BoneTrackedPerson person,
            BoneBodyJointType jointType,
            float keypointConfidenceThreshold)
        {
            if (person == null || person.m_Body == null || person.m_Body.m_Joints == null)
            {
                return "无";
            }

            int jointIndex = (int)jointType;
            if (jointIndex < 0 || jointIndex >= person.m_Body.m_Joints.Length)
            {
                return "越界";
            }

            BoneTrackedJoint joint = person.m_Body.m_Joints[jointIndex];
            if (joint == null || !joint.m_IsTracked)
            {
                return "未跟踪";
            }

            return joint.m_Score >= keypointConfidenceThreshold
                ? "有效(" + joint.m_Score.ToString("0.00") + ")"
                : "低分(" + joint.m_Score.ToString("0.00") + ")";
        }

        private void DispatchActionEvents(
            BoneParserPlayerResult playerResult,
            int seatId,
            Vector3 faceForward,
            Vector3 moveDirection)
        {
            if (playerResult == null)
            {
                return;
            }

            int eventCount = playerResult.m_ActionEvents.Count;
            if (eventCount > 0)
            {
                // 解冻转向收敛期间一旦出现开火手势，立即对齐模型朝向，避免背身出弹。
                var actionBattle = BattleManager.GetBattle() as TowerDefendBattle;
                if (actionBattle != null)
                {
                    actionBattle.SnapAimTurnRecoverBySeat(seatId);
                }
            }

            bool hasActiveSkillEvent = false;
            for (int i = 0; i < eventCount; i++)
            {
                var actionEvent = playerResult.m_ActionEvents[i];
                if (actionEvent != null && actionEvent.m_ConsumerType == BoneActionConsumerType.动作槽位 &&
                    actionEvent.m_ConsumerValue > 0)
                {
                    hasActiveSkillEvent = true;
                    break;
                }
            }

            // 先处理技能。即使能量不足，该帧普攻也不能先出手再为技能补足条件。
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < eventCount; i++)
                {
                    var actionEvent = playerResult.m_ActionEvents[i];
                    if (actionEvent == null)
                    {
                        continue;
                    }
                    bool isActiveSkillEvent = actionEvent.m_ConsumerType == BoneActionConsumerType.动作槽位 &&
                        actionEvent.m_ConsumerValue > 0;
                    if (isActiveSkillEvent != (pass == 0))
                    {
                        continue;
                    }

                    bool blockNormalAttack = hasActiveSkillEvent &&
                        actionEvent.m_ConsumerType == BoneActionConsumerType.动作槽位 &&
                        actionEvent.m_ConsumerValue == 0;
                    var consumeResultType = blockNormalAttack
                        ? BoneActionConsumeResultType.拒绝阻断
                        : TryConsumeActionEvent(actionEvent, seatId, faceForward, moveDirection);
                    if (consumeResultType == BoneActionConsumeResultType.接受 && !m_HasLoggedAcceptedActionBySeat[seatId])
                    {
                        m_HasLoggedAcceptedActionBySeat[seatId] = true;
                        Debug.Log("[塔防骨骼输入] 动作已消费 seat=" + seatId + " person=" + playerResult.m_PersonId +
                                  " skillSlot=" + actionEvent.m_ConsumerValue +
                                  " time=" + Time.unscaledTime.ToString("F1"));
                    }

                    if (actionEvent.m_RequiresConsumeResult)
                    {
                        m_BoneParserRuntime.ApplyActionConsumeResult(new BoneActionConsumeResult
                        {
                            m_ActionEventId = actionEvent.m_ActionEventId,
                            m_ResultType = consumeResultType,
                        });
                    }
                }
            }
        }

        private static BoneActionConsumeResultType TryConsumeActionEvent(
            BoneActionEvent actionEvent,
            int seatId,
            Vector3 faceForward,
            Vector3 moveDirection)
        {
            if (actionEvent == null)
            {
                return BoneActionConsumeResultType.忽略;
            }

            if (actionEvent.m_ConsumerType != BoneActionConsumerType.动作槽位 || actionEvent.m_ConsumerValue < 0)
            {
                return BoneActionConsumeResultType.忽略;
            }

            if ((actionEvent.m_RuntimeFlags & BoneActionRuntimeFlags.可消费) == 0)
            {
                return actionEvent.m_RequiresConsumeResult
                    ? BoneActionConsumeResultType.拒绝阻断
                    : BoneActionConsumeResultType.忽略;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            var spawer = battle != null ? battle.ReadBattleSpawer() as TowerDefendBattleSpawer : null;
            if (spawer == null)
            {
                return actionEvent.m_RequiresConsumeResult
                    ? BoneActionConsumeResultType.拒绝阻断
                    : BoneActionConsumeResultType.忽略;
            }

            bool boneNormalAttack = actionEvent.m_ConsumerValue == 0;
            bool handled = spawer.TryGuardHeroBoneActionBySeat(seatId, actionEvent.m_ConsumerValue,
                faceForward, moveDirection, boneNormalAttack);
            if (handled)
            {
                return BoneActionConsumeResultType.接受;
            }

            return actionEvent.m_RequiresConsumeResult
                ? BoneActionConsumeResultType.拒绝阻断
                : BoneActionConsumeResultType.忽略;
        }

        private void RebuildSeatDefinitions()
        {
            m_BoneParserRuntime.Reset();
            ResetSeatTrackingState();
            m_SeatDefinitions.Clear();
            for (int seatId = 0; seatId < TowerDefendSeatLayout.MaxSupportedPlayerCount; seatId++)
            {
                int sdkSlotIndex = -1;
                if (m_Scene != null && !m_Scene.TryGetSdkSlotIndexBySeat(seatId, out sdkSlotIndex))
                {
                    var battle = BattleManager.GetBattle() as TowerDefendBattle;
                    if (battle != null && battle.ReadPlayerBySeat(seatId) != null)
                    {
                        Debug.LogError("[塔防骨骼输入] 战斗座位 " + seatId + " 缺少 SDK 槽位绑定，当前不会接收骨骼输入。");
                    }
                }

                m_SeatDefinitions.Add(new BoneParserSeatDefinition
                {
                    m_SlotIndex = sdkSlotIndex,
                    m_BindingId = seatId,
                    m_IsProcessGestureEnabled = true,
                });
            }

            m_HasSeatDefinitions = m_SeatDefinitions.Count > 0;
        }

        private void RefreshSeatActionBindings(TowerDefendBattle battle)
        {
            for (int i = 0; i < m_SeatDefinitions.Count; i++)
            {
                var definition = m_SeatDefinitions[i];
                var hero = battle.ReadGuardHeroBySeat(definition.m_BindingId);
                bool suppressNormalAttack = ReadIsNormalAttackSuppressedByTurn(definition.m_BindingId);
                RefreshSeatActionBindings(definition, battle, hero, suppressNormalAttack);
            }
        }

        private bool ReadIsNormalAttackSuppressedByTurn(int seatId)
        {
            if (!BoneTurnTuning.ReadBlockNormalAttackWhileTurning() || !TowerDefendSeatLayout.IsValidSeatId(seatId))
            {
                return false;
            }

            // 动作绑定必须在本帧骨骼解析前提供，因此这里使用上一帧已经确认的转向状态。
            return m_IsTurningBySeat[seatId];
        }

        private void RefreshSeatActionBindings(
            BoneParserSeatDefinition definition,
            TowerDefendBattle battle,
            PropertyEntity hero,
            bool suppressNormalAttack)
        {
            if (definition == null)
            {
                return;
            }

            foreach (var binding in definition.m_ActionBindings)
            {
                m_RecycledActionBindings.Push(binding);
            }

            definition.m_ActionBindings.Clear();
            definition.m_IsProcessGestureEnabled = false;

            if (!BattleManager.ReadIsEntityValide(hero))
            {
                return;
            }

            var skillManager = hero.GetSkillManager();
            var skills = skillManager != null ? skillManager.ReadSkills() : null;
            if (skills == null || skills.Count <= 0)
            {
                return;
            }

            int skillCount = skills.Count;
            for (int i = 0; i < skillCount; i++)
            {
                var skill = skills[i];
                if (!TryBuildSkillActionBinding(definition, battle, hero, skill, suppressNormalAttack))
                {
                    continue;
                }
            }

            definition.m_IsProcessGestureEnabled = HasProcessGestureBinding(definition);
        }

        private bool TryBuildSkillActionBinding(
            BoneParserSeatDefinition definition,
            TowerDefendBattle battle,
            PropertyEntity hero,
            Skill skill,
            bool suppressNormalAttack)
        {
            if (definition == null || skill == null)
            {
                return false;
            }

            int slot = skill.ReadSlot();
            if (slot < 0)
            {
                return false;
            }

            var skillDesc = skill.GetSkillDescBean();
            if (skillDesc == null)
            {
                LogInvalidSkillActionBinding(definition != null ? definition.m_BindingId : -1, hero, skill, 0, 0, "技能描述表为空。");
                return false;
            }

            if (!BoneGestureRules.TryResolveActionBinding(
                    skillDesc.t_gesture,
                    skillDesc.t_gesture_phase,
                    out BoneGestureType gestureType,
                    out BoneGesturePhaseMask phaseMask,
                    out bool requiresConsumeResult,
                    out string error))
            {
                LogInvalidSkillActionBinding(definition != null ? definition.m_BindingId : -1, hero, skill, skillDesc.t_gesture, skillDesc.t_gesture_phase, error);
                return false;
            }

            BoneActionRuntimeFlags runtimeFlags = ResolveActionRuntimeFlags(battle, hero, skill, slot, suppressNormalAttack);
            UpsertActionBinding(definition, gestureType, phaseMask, slot, runtimeFlags, requiresConsumeResult);
            return true;
        }

        private static BoneActionRuntimeFlags ResolveActionRuntimeFlags(
            TowerDefendBattle battle,
            PropertyEntity hero,
            Skill skill,
            int slot,
            bool suppressNormalAttack)
        {
            BoneActionRuntimeFlags runtimeFlags = BoneActionRuntimeFlags.无;
            if (slot != 0 || !suppressNormalAttack)
            {
                runtimeFlags |= BoneActionRuntimeFlags.可识别;
            }

            if (ReadCanConsumeSkillAction(battle, hero, skill, slot))
            {
                runtimeFlags |= BoneActionRuntimeFlags.可消费;
            }

            return runtimeFlags;
        }

        private static bool ReadCanConsumeSkillAction(
            TowerDefendBattle battle,
            PropertyEntity hero,
            Skill skill,
            int slot)
        {
            var spawer = battle != null ? battle.ReadBattleSpawer() as TowerDefendBattleSpawer : null;
            bool boneNormalAttack = slot == 0;
            return spawer != null && slot >= 0 &&
                spawer.CanConsumeGuardHeroBoneAction(hero, skill, boneNormalAttack);
        }

        private void UpsertActionBinding(
            BoneParserSeatDefinition definition,
            BoneGestureType gestureType,
            BoneGesturePhaseMask phaseMask,
            int slot,
            BoneActionRuntimeFlags runtimeFlags,
            bool requiresConsumeResult)
        {
            if (definition == null)
            {
                return;
            }

            int existingIndex = -1;
            for (int i = 0; i < definition.m_ActionBindings.Count; i++)
            {
                var existing = definition.m_ActionBindings[i];
                if (existing != null &&
                    existing.m_GestureType == gestureType &&
                    existing.m_PhaseMask == phaseMask)
                {
                    existingIndex = i;
                    if (existing.m_ConsumerValue <= slot)
                    {
                        Debug.LogWarning(
                            "[塔防骨骼输入] 检测到重复手势绑定，已保留较小技能槽位。 seat=" + definition.m_BindingId +
                            " gesture=" + gestureType +
                            " phase=" + phaseMask +
                            " keepSlot=" + existing.m_ConsumerValue +
                            " dropSlot=" + slot);
                        return;
                    }

                    Debug.LogWarning(
                        "[塔防骨骼输入] 检测到重复手势绑定，已切换为较小技能槽位。 seat=" + definition.m_BindingId +
                        " gesture=" + gestureType +
                        " phase=" + phaseMask +
                        " keepSlot=" + slot +
                        " dropSlot=" + existing.m_ConsumerValue);
                    break;
                }
            }

            BoneActionBinding actionBinding;
            if (existingIndex >= 0)
            {
                actionBinding = definition.m_ActionBindings[existingIndex];
            }
            else
            {
                actionBinding = m_RecycledActionBindings.Count > 0
                    ? m_RecycledActionBindings.Pop()
                    : new BoneActionBinding();
                definition.m_ActionBindings.Add(actionBinding);
            }

            actionBinding.m_ActionId = slot;
            actionBinding.m_GestureType = gestureType;
            actionBinding.m_PhaseMask = phaseMask;
            actionBinding.m_ConsumerType = BoneActionConsumerType.动作槽位;
            actionBinding.m_ConsumerValue = slot;
            actionBinding.m_RuntimeFlags = runtimeFlags;
            actionBinding.m_RequiresConsumeResult = requiresConsumeResult;
        }

        private static bool HasProcessGestureBinding(BoneParserSeatDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            int bindingCount = definition.m_ActionBindings.Count;
            for (int i = 0; i < bindingCount; i++)
            {
                var actionBinding = definition.m_ActionBindings[i];
                if (actionBinding != null && BoneGestureRules.ReadIsProcessGesture(actionBinding.m_GestureType))
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogInvalidSkillActionBinding(
            int seatId,
            PropertyEntity hero,
            Skill skill,
            int gestureId,
            int gesturePhaseValue,
            string reason)
        {
            long battlePlayerId = hero != null ? hero.ReadBattlePlayerId() : 0;
            int slot = skill != null ? skill.ReadSlot() : -1;
            long skillCfgId = skill != null ? skill.ReadSkillCfgId() : 0;
            Debug.LogError(
                "[塔防骨骼输入] 技能动作绑定无效。 seat=" + seatId +
                " playerId=" + battlePlayerId +
                " skillCfg=" + skillCfgId +
                " slot=" + slot +
                " gesture=" + gestureId +
                " phase=" + gesturePhaseValue +
                " reason=" + reason);
        }

        private Vector3 ResolveWorldFaceForward(Vector3 localFaceForward)
        {
            var camera = m_Scene != null ? m_Scene.GetActiveCamera() : null;
            if (camera == null)
            {
                return NormalizeHorizontalOrDefault(localFaceForward, Vector3.forward);
            }

            var cameraForward = NormalizeHorizontalOrDefault(camera.transform.forward, Vector3.forward);
            var cameraRight = NormalizeHorizontalOrDefault(camera.transform.right, Vector3.right);
            var worldForward = cameraForward * Mathf.Max(0.1f, localFaceForward.z) + cameraRight * localFaceForward.x;
            return NormalizeHorizontalOrDefault(worldForward, cameraForward);
        }

        private static Vector3 ToUnityVector3(BoneVector3 value)
        {
            return new Vector3(value.m_X, value.m_Y, value.m_Z);
        }

        private static int CountActivePlayers(TowerDefendBattle battle)
        {
            var players = battle != null ? battle.ReadPlayers() : null;
            if (players == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static Vector3 NormalizeOrDefault(Vector3 value, Vector3 fallback)
        {
            if (value.sqrMagnitude <= 0.0001f)
            {
                return fallback;
            }

            return value.normalized;
        }

        private static Vector3 NormalizeHorizontalOrDefault(Vector3 value, Vector3 fallback)
        {
            value.y = 0f;
            fallback.y = 0f;
            return NormalizeOrDefault(value, fallback);
        }
    }
}
