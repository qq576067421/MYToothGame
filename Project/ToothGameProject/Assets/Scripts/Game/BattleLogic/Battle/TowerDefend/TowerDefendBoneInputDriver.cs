using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    public class TowerDefendBoneInputDriver
    {
        private const int m_BoneInputDebugIntervalFrames = 60;

        private readonly BoneParserConfig m_Config = new BoneParserConfig();
        private readonly IBoneParserRuntime m_BoneParserRuntime;
        private readonly BoneParserFrameAdapter m_FrameAdapter = new BoneParserFrameAdapter();
        private readonly List<BoneParserSeatDefinition> m_SeatDefinitions = new List<BoneParserSeatDefinition>();
        private readonly int[] m_LastDebugLogFrames = new int[TowerDefendSeatLayout.MaxSupportedPlayerCount];

        private TowerDefendBattleScene m_Scene;
        private IBoneFrameSource m_FrameSource;
        private bool m_HasSeatDefinitions;
        private bool m_HasLoggedRuntime;

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
            ResetDebugLogFrames();
            m_BoneParserRuntime.Reset();
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

            if (!EnsureSeatDefinitions(battle))
            {
                return;
            }

            RefreshSeatSdkSlotBindings(battle);
            RefreshSeatActionBindings(battle);
            ApplyRuntimeConfig();
            m_FrameSource.Tick();
            var parserFrame = m_FrameAdapter.Convert(m_FrameSource.ReadLatestFrameData());
            var frameResult = m_BoneParserRuntime.Update(parserFrame, m_SeatDefinitions);
            if (frameResult == null)
            {
                return;
            }

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

                RefreshHudBoneDebugInfo(seatId, seatDefinition.m_SlotIndex, playerResult);

                if (playerResult == null)
                {
                    LogMissingTrackedFrame(parserFrame, null, seatId, seatDefinition.m_SlotIndex);
                    continue;
                }

                if (!playerResult.m_IsAimAvailable)
                {
                    LogMissingTrackedFrame(parserFrame, playerResult, seatId, seatDefinition.m_SlotIndex);
                    continue;
                }

                Vector3 worldFaceForward = ResolveWorldFaceForward(ToUnityVector3(playerResult.m_FaceForward));
                Vector3 moveDirection = NormalizeHorizontalOrDefault(worldFaceForward, Vector3.forward);
                Vector3 faceForward = battle.UpdateBoneAimForwardBySeat(seatId, worldFaceForward);
                if (!playerResult.m_IsTracked)
                {
                    LogMissingTrackedFrame(parserFrame, playerResult, seatId, seatDefinition.m_SlotIndex);
                    continue;
                }

                DispatchActionEvents(playerResult, seatId, faceForward, moveDirection);
            }
        }

        public void Destroy()
        {
            m_FrameSource?.Shutdown();
            m_Scene = null;
            m_FrameSource = null;
            m_HasSeatDefinitions = false;
            m_SeatDefinitions.Clear();
            ResetDebugLogFrames();
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

        private void ApplyRuntimeConfig()
        {
            m_Config.m_MaxTurnAngleDegrees = BoneTurnTuning.ReadClampedMaxAngle();
            m_Config.m_InvertTurnDirection = BoneTurnTuning.ReadClampedInvertDirection();
            m_Config.m_RotationAmplifyFactor = BoneTurnTuning.ReadClampedRotationAmplifyFactor();
            m_Config.m_ShoulderTurnJitterDeadZone = BoneTurnTuning.ReadClampedShoulderTurnJitterDeadZone();
            BoneGestureTuning.ApplyTo(m_Config);
        }

        private bool EnsureSeatDefinitions(TowerDefendBattle battle)
        {
            int activePlayerCount = CountActivePlayers(battle);
            if (activePlayerCount <= 0)
            {
                m_HasSeatDefinitions = false;
                m_SeatDefinitions.Clear();
                m_BoneParserRuntime.Reset();
                return false;
            }

            bool shouldRebuild = !m_HasSeatDefinitions || m_SeatDefinitions.Count != TowerDefendSeatLayout.MaxSupportedPlayerCount;
            if (shouldRebuild)
            {
                RebuildSeatDefinitions();
            }

            return m_SeatDefinitions.Count > 0;
        }

        private void RefreshSeatSdkSlotBindings(TowerDefendBattle battle)
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

            ResetDebugLogFrames();
            m_BoneParserRuntime.Reset();
        }

        private void ResetDebugLogFrames()
        {
            for (int seatId = 0; seatId < TowerDefendSeatLayout.MaxSupportedPlayerCount; seatId++)
            {
                m_LastDebugLogFrames[seatId] = -m_BoneInputDebugIntervalFrames;
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
            for (int i = 0; i < eventCount; i++)
            {
                var actionEvent = playerResult.m_ActionEvents[i];
                if (actionEvent == null)
                {
                    continue;
                }

                var consumeResultType = TryConsumeActionEvent(actionEvent, seatId, faceForward, moveDirection);
                if (!actionEvent.m_RequiresConsumeResult)
                {
                    continue;
                }

                m_BoneParserRuntime.ApplyActionConsumeResult(new BoneActionConsumeResult
                {
                    m_ActionEventId = actionEvent.m_ActionEventId,
                    m_ResultType = consumeResultType,
                });
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
                    ? BoneActionConsumeResultType.拒绝可重试
                    : BoneActionConsumeResultType.忽略;
            }

            var battleLogic = CBattleLogic.GetInstance();
            if (battleLogic == null)
            {
                return actionEvent.m_RequiresConsumeResult
                    ? BoneActionConsumeResultType.拒绝阻断
                    : BoneActionConsumeResultType.忽略;
            }

            bool handled = battleLogic.TryPlayerActionBySeat(seatId, actionEvent.m_ConsumerValue, faceForward, moveDirection);
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
            ResetDebugLogFrames();
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
                RefreshSeatActionBindings(definition, battle, hero);
            }
        }

        private static void RefreshSeatActionBindings(
            BoneParserSeatDefinition definition,
            TowerDefendBattle battle,
            PropertyEntity hero)
        {
            if (definition == null)
            {
                return;
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
                if (!TryBuildSkillActionBinding(definition, battle, hero, skill))
                {
                    continue;
                }
            }

            definition.m_IsProcessGestureEnabled = HasProcessGestureBinding(definition);
        }

        private static bool TryBuildSkillActionBinding(
            BoneParserSeatDefinition definition,
            TowerDefendBattle battle,
            PropertyEntity hero,
            Skill skill)
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

            BoneActionRuntimeFlags runtimeFlags = ResolveActionRuntimeFlags(battle, hero, skill, slot);
            UpsertActionBinding(definition, gestureType, phaseMask, slot, runtimeFlags, requiresConsumeResult);
            return true;
        }

        private static BoneActionRuntimeFlags ResolveActionRuntimeFlags(
            TowerDefendBattle battle,
            PropertyEntity hero,
            Skill skill,
            int slot)
        {
            BoneActionRuntimeFlags runtimeFlags = BoneActionRuntimeFlags.可识别;
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
            if (!BattleManager.ReadIsEntityValide(hero) || skill == null || slot < 0)
            {
                return false;
            }

            if (battle != null && battle.ReadIsUpgradeChallengePreActive())
            {
                return slot == 0;
            }

            if (battle != null && battle.ReadIsUpgradeChallengeActive())
            {
                return slot == 0 &&
                    battle.CanAddUpgradeChallengeScore(hero.ReadBattlePlayerId()) &&
                    BattleManager.IsCanUseSkill_WillNextUseSkill(hero, slot, null);
            }

            if (battle != null && battle.ReadHasPendingMonsterRestore())
            {
                return false;
            }

            if (slot > 0 &&
                battle != null &&
                battle.RequiresPlayerSkillEnergy(hero, skill) &&
                !battle.CanPlayerQueueActiveSkill(hero.ReadBattlePlayerId()))
            {
                return false;
            }

            return BattleManager.IsCanUseSkill_WillNextUseSkill(hero, slot, null);
        }

        private static void UpsertActionBinding(
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

            var actionBinding = new BoneActionBinding
            {
                m_ActionId = slot,
                m_GestureType = gestureType,
                m_PhaseMask = phaseMask,
                m_ConsumerType = BoneActionConsumerType.动作槽位,
                m_ConsumerValue = slot,
                m_RuntimeFlags = runtimeFlags,
                m_RequiresConsumeResult = requiresConsumeResult,
            };

            if (existingIndex >= 0)
            {
                definition.m_ActionBindings[existingIndex] = actionBinding;
                return;
            }

            definition.m_ActionBindings.Add(actionBinding);
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
