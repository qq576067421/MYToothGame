using GameDll;
using LCL;
using MonoBean;
using System; 
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace GameDll
{
    public sealed class TowerDefendBossSkillRuntimeData
    {
        public int m_BossEntityId;
        public float m_NextCastTime;
        public float m_CastResolveTime;
        public float m_Cooldown;
        public bool m_IsCasting;
    }

    public class TowerDefendBattleSpawer : IBattleSpawer
    {
        public static bool m_GMDisableAutoSkill = false;
        private const float m_ManualControlAutoAttackBlockSec = 0.6f;

        private List<PropertyEntity> m_GuradHeroes = new List<PropertyEntity>();
        private readonly List<PropertyEntity> m_TargetCandidates = new List<PropertyEntity>();
        private readonly List<PropertyEntity> m_BossCandidates = new List<PropertyEntity>();
        private readonly Dictionary<int, float> m_ManualControlExpireTimes = new Dictionary<int, float>();
        private readonly Dictionary<int, TowerDefendBossSkillRuntimeData> m_BossSkillRuntimes = new Dictionary<int, TowerDefendBossSkillRuntimeData>();
        private bool m_IsMonsterSuspended = false;
        private float m_MonsterSuspendStartBattleTime = -1f;

        private TowerDefendMonsterSpawer m_MonsterSpawer = new TowerDefendMonsterSpawer();
        public override int ReadWave()
        {
            return m_MonsterSpawer.ReadWaveIndex();
        }
        public override int ReadWildWave()
        {
            return m_MonsterSpawer.ReadWaveIndex();
        }
        public float ReadWaveWait()
        {
            return m_MonsterSpawer.ReadWaitSpawWaveTime();
        }
        public int ReadMaxWave()
        {
            return m_MonsterSpawer.ReadMaxWave();
        }
        public bool ReadIsBossWave()
        {
            return m_MonsterSpawer.ReadIsBossWave();
        }
        public bool ReadIsEliteWave()
        {
            return m_MonsterSpawer.ReadIsEliteWave();
        }
        public int ReadRemainingSpecialSpawnCount()
        {
            return m_MonsterSpawer.CountRemainingQueuedSpecialSpawnCount();
        }
        public int ReadCurrentMonsterPoolId()
        {
            return m_MonsterSpawer.ReadCurrentMonsterPoolId();
        }
        public int ReadCurrentMonsterPoolStageId()
        {
            return m_MonsterSpawer.ReadCurrentMonsterPoolStageId();
        }
        public int ReadMonsterKillRewardExp()
        {
            return m_MonsterSpawer.ReadMonsterKillRewardExp();
        }
        public int ReadMonsterKillRewardCoin()
        {
            return m_MonsterSpawer.ReadMonsterKillRewardCoin();
        }
        public float ReadMonsterPoolSwitchLeft()
        {
            return m_MonsterSpawer.ReadMonsterPoolSwitchLeft();
        }
        public MonsterType ReadMonsterKind(int entityId)
        {
            return m_MonsterSpawer.ReadMonsterKind(entityId);
        }
        public bool ReadIsBossSkillCasting()
        {
            if (m_IsMonsterSuspended)
            {
                return false;
            }

            foreach (var kv in m_BossSkillRuntimes)
            {
                var runtime = kv.Value;
                if (runtime != null && runtime.m_IsCasting)
                {
                    return true;
                }
            }

            return false;
        }
        public float ReadBossSkillCastingLeft()
        {
            if (m_IsMonsterSuspended)
            {
                return 0;
            }

            float now = BattleManager.ReadBattleTime();
            foreach (var kv in m_BossSkillRuntimes)
            {
                var runtime = kv.Value;
                if (runtime != null && runtime.m_IsCasting)
                {
                    return Mathf.Max(0, runtime.m_CastResolveTime - now);
                }
            }

            return 0;
        }
        public override int ReadMonsterCount()
        {
            return m_MonsterSpawer.ReadMonsterCount();
        }
        public bool ReadIsMonsterPreloadFinished()
        {
            return m_MonsterSpawer == null || m_MonsterSpawer.ReadIsMonsterPreloadFinished();
        }
        public TowerDefendMonsterSpawer GetMonsterSpawer()
        {
            return m_MonsterSpawer;
        }
        public bool GM_TrySpawnNextWaveNow()
        {
            return m_MonsterSpawer != null && m_MonsterSpawer.GM_TrySpawnNextWaveNow();
        }

        public bool GM_TrySpawnBossNow()
        {
            return m_MonsterSpawer != null && m_MonsterSpawer.GM_TrySpawnBossNow();
        }

        public bool GM_TrySetCurrentWave(int wave)
        {
            return m_MonsterSpawer != null && m_MonsterSpawer.GM_TrySetCurrentWave(wave);
        }

        public void TrySummonMonstersFromEntity(PropertyEntity sourceEntity, int targetCount)
        {
            if (m_MonsterSpawer == null || sourceEntity == null || targetCount <= 0)
            {
                return;
            }

            var spawnData = m_MonsterSpawer.BuildSummonedMonsterSpawnData(sourceEntity.ReadId(), targetCount);
            if (spawnData == null || spawnData.Count <= 0)
            {
                return;
            }

            m_MonsterSpawer.CreateMonsters(spawnData);
        }


        private TowerDefendBattle m_Battle;

        public override void OnCreate(IBattle battle)
        {
            m_Battle = (TowerDefendBattle)battle;
        }



        public override PropertyEntity ReadHero(int id)
        {
            foreach (var hero in m_GuradHeroes)
            {
                if (hero.ReadId() == id)
                {
                    return hero;
                }
            }
            return null;
        }

        public override List<PropertyEntity> ReadHeroes()
        {
            return m_GuradHeroes;
        }


        public override void OnLoadMap(int stage)
        {
            var monsterPathPoints = new List<TowerDefendMonsterPathPointData>(m_Battle.ReadMonsterPathPoints());
            m_MonsterSpawer.ConfigureScenePoints(monsterPathPoints, m_Battle.ReadBasePoint(), m_Battle.ReadBaseReachRadius());
            m_MonsterSpawer.Init(stage);

            //m_MonsterSpawer.SetMaxMonsterCount(max_monster_count);

            LoadGuardHeroes();
        }

        private void LoadGuardHeroes()
        {
            var battle = BattleManager.GetBattle();
            var players = battle.ReadPlayers();
            var heroSpawnPoints = m_Battle.ReadGuardHeroSpawnPoints();
            var objMgr = BattleManager.GetObjectManager();
            m_GuradHeroes.Clear();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var heroCfg = t_heroBean.GetConfig(player.m_RoleCfgId, false);
                if (heroCfg == null)
                {
                    Debug.LogWarning("塔防战斗缺少角色配置，角色配置ID：" + player.m_RoleCfgId + "。请先检查角色表配置。");
                    continue;
                }

                var bornPosition = ResolveGuardHeroSpawnPoint(heroSpawnPoints, i, players);
                var ent = (PlayerHero)objMgr.NewCreature(emEntityType.em_EntityType_PlayerHero);
                ent.SetBean(heroCfg);
                ent.SetGroup(player.m_Group);
                ent.SetId(objMgr.AssignClientId());
                ent.SetBattlePlayerId(player.m_ID);
                ent.SetBattlePlayerName(player.m_Name);
                ent.SetBornPosition(bornPosition);
                ent.CreateRender(null, ResourceType.PlayerActor);
                ent.InitInstance();
                ent.SetForward(Vector3.forward);
                ent.SetPosition(bornPosition);
                ent.InitLevel(player.m_RoleLevel);
                ent.InitSkills();
                ent.SetCanWarningFollow(false);
                ent.InitHp();
                ent.InitHPPercent(player.m_HPPercent);
                ent.ApplyTalentBonus(m_Battle.ReadPlayerTalentBonus(player.m_ID));
                ent.SetAngularSpeed(720);
                ent.SetVisiable(true);
                objMgr.AddPropertyEntity(ent);
                m_GuradHeroes.Add(ent);

                var normalSkill = ent.ReadNormalSkill();
                if (normalSkill != null)
                {
                    ent.SetWarningDist(ent.GetSkillCastDist(normalSkill));
                }
            }
        }

        private Vector3 ResolveGuardHeroSpawnPoint(IReadOnlyList<Vector3> heroSpawnPoints, int index, List<BattlePlayer> players)
        {
            if (heroSpawnPoints == null || heroSpawnPoints.Count <= 0)
            {
                throw new InvalidOperationException("塔防场景缺少守塔英雄站位点，无法创建玩家角色。");
            }

            var mappedIndex = ResolveGuardHeroDisplayIndex(index, players.Count, heroSpawnPoints.Count);
            players[index].mappedIndex = mappedIndex;
            if (mappedIndex < 0 || mappedIndex >= heroSpawnPoints.Count)
            {
                throw new InvalidOperationException(
                    $"塔防场景守塔英雄站位数量不足，无法为玩家索引 {index} 映射有效站位。当前点位数：{heroSpawnPoints.Count}。");
            }

            return TowerDefendGuardLayout.ResolveCenteredPosition(heroSpawnPoints, players.Count, index);
        }

        private int ResolveGuardHeroDisplayIndex(int playerIndex, int playerCount, int slotCount)
        {
            if (slotCount <= 0)
            {
                return 0;
            }

            if (playerCount <= 0)
            {
                return Mathf.Clamp(playerIndex, 0, slotCount - 1);
            }

            // 原始策划案要求少人局时角色站位保持居中。
            var normalizedPlayerCount = Mathf.Min(playerCount, slotCount);
            var startIndex = Mathf.Max(0, (slotCount - normalizedPlayerCount) / 2);
            return Mathf.Clamp(startIndex + playerIndex, 0, slotCount - 1);
        }

        public override void Update(float dt)
        {
            if (m_MonsterSpawer != null)
            {
                m_MonsterSpawer.Update(dt);
            }

            UpdateBossSkills();
            UpdateGuardHeroAutoAttack();
        }

        public override void OnRelease()
        {
            m_GuradHeroes.Clear();
            m_ManualControlExpireTimes.Clear();
            m_BossSkillRuntimes.Clear();
            if (m_MonsterSpawer != null)
            {
                m_MonsterSpawer.OnRelease();
            }
            m_IsMonsterSuspended = false;
            m_MonsterSuspendStartBattleTime = -1f;
        }

        public override List<PropertyEntity> ReadGuardHeroes()
        {
            return m_GuradHeroes;
        }

        public void SetMonsterPresentation(bool visible, bool canBeTarget, bool suspend)
        {
            if (suspend != m_IsMonsterSuspended)
            {
                if (suspend)
                {
                    m_MonsterSuspendStartBattleTime = BattleManager.ReadBattleTime();
                }
                else if (m_MonsterSuspendStartBattleTime >= 0)
                {
                    ShiftBossSkillRuntimeTime(Mathf.Max(0, BattleManager.ReadBattleTime() - m_MonsterSuspendStartBattleTime));
                    m_MonsterSuspendStartBattleTime = -1f;
                }

                m_IsMonsterSuspended = suspend;
            }

            if (m_MonsterSpawer != null)
            {
                m_MonsterSpawer.SetMonsterPresentation(visible, canBeTarget, suspend);
            }
        }

        private void UpdateGuardHeroAutoAttack()
        {
            int heroCount = m_GuradHeroes.Count;
            for (int i = 0; i < heroCount; i++)
            {
                var hero = m_GuradHeroes[i];
                if (!BattleManager.ReadIsEntityValide(hero))
                {
                    continue;
                }

                if (m_Battle != null && m_Battle.ReadPhase() != BattlePhase.NormalGame)
                {
                    continue;
                }

                // 自动动作持续技能因条件失效结束后，若玩家还在连续攻击且普攻也是持续型，
                // 在当前技能真正结束的这一帧自动释放普攻，不等玩家再点一次。
                if (hero is PlayerHero playerHero && TryAutoContinueNormalAttack(playerHero))
                {
                    continue;
                }

                var skillMgr = hero.GetSkillManager();
                int skillCount = skillMgr != null ? skillMgr.GetSkillCount() : 0;
                bool handled = false;
                for (int slot = 1; slot < skillCount; slot++)
                {
                    if (TryAutoCastGuardHeroSkill(hero, slot))
                    {
                        handled = true;
                        break;
                    }
                }

                if (handled)
                {
                    continue;
                }

                TryAutoCastGuardHeroSkill(hero, 0);
            }
        }

        // 自动动作持续技能因条件失效结束后：若当前技能已真正结束、玩家仍在连续攻击，
        // 且普攻本身是持续型（如激光），自动释放普攻，接上持续释放。
        private bool TryAutoContinueNormalAttack(PlayerHero hero)
        {
            if (hero == null ||
                !hero.TryPeekAutoNormalAttackContinue(
                    PlayerHero.AutoNormalAttackContinueMaxAgeSeconds, out _))
            {
                return false;
            }

            var skillManager = hero.GetSkillManager();
            if (skillManager == null || skillManager.GetCurrentSkill() != null)
            {
                // 触发条件失效的技能还没真正结束，等它结束后的下一帧再接。
                return false;
            }

            if (!hero.ReadIsNormalAttackInputActive(PlayerHero.NormalAttackInputActiveWindowSeconds))
            {
                hero.ClearAutoNormalAttackContinue();
                return false;
            }

            var normalSkill = skillManager.ReadSkillBySlot(0);
            if (!(normalSkill is CommonBulletSkill normalBullet) || !normalBullet.ReadIsContinuousBulletCast())
            {
                hero.ClearAutoNormalAttackContinue();
                return false;
            }

            if (!BattleManager.IsCanUseSkillDirect(hero, 0))
            {
                return false;
            }

            var forward = hero.ReadForward();
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            hero.ClearAutoNormalAttackContinue();
            MarkManualControl(hero);
            BattleManager.AttackDir(0, hero, forward, Vector3.zero);
            return true;
        }

        private bool TryAutoCastGuardHeroSkill(PropertyEntity hero, int slot)
        {
            if (!BattleManager.ReadIsEntityValide(hero) || slot < 0)
            {
                return false;
            }

            if (slot > 0 && m_GMDisableAutoSkill)
            {
                return false;
            }

            var skill = hero.GetSkillManager().ReadSkillBySlot(slot);
            if (skill == null || !skill.ReadIsCooldown() || skill.ReadCastStyle() == 0)
            {
                return false;
            }

            if (slot > 0 &&
                m_Battle != null &&
                m_Battle.RequiresPlayerSkillEnergy(hero, skill) &&
                !m_Battle.CanPlayerQueueActiveSkill(hero.ReadBattlePlayerId()))
            {
                return false;
            }

            // 自动选技需要继续向后尝试可释放技能，
            // 因此前置、沉默、普攻禁用等“当前技能本身不可释放”的情况不能截断后续槽位。
            if (hero.CanAttack(skill) != AttackFailedReason.Success)
            {
                return false;
            }

            var skillMgr = hero.GetSkillManager();
            var currentSkill = skillMgr != null ? skillMgr.GetCurrentSkill() : null;
            if (slot > 0 &&
                currentSkill != null &&
                currentSkill.ReadSlot() == 0 &&
                currentSkill.IsSkillCanBreakStatus(slot))
            {
                // 自动技能满足条件后应优先于后续普攻触发，
                // 这里仅允许其打断已经越过施法点的普攻，避免被连续普攻长期挤占。
                hero.BreakSkill();
            }

            // 自动施放不在释放时索敌，只要技能自身条件满足就按当前面向直接施放。
            if (!BattleManager.IsCanUseSkillDirect(hero, slot))
            {
                return false;
            }

            var faceForward = hero.ReadForward();
            if (faceForward.sqrMagnitude <= 0.0001f)
            {
                faceForward = Vector3.forward;
            }
            BattleManager.AttackDir(slot, hero, faceForward, Vector3.zero);

            return true;
        }

        // 程序触发的动作技能（t_auto_action_skill_id）以"强化普攻"方式生效：
        // 自身条件满足后，玩家的下一次普攻替换为该技能的完整施法（带动作，与手动施放同链路）。
        private bool TryCastAutoActionSkillInsteadOfNormalAttack(PropertyEntity hero, Vector3 faceForward, Vector3 moveDir)
        {
            var playerHero = hero as PlayerHero;
            if (playerHero == null || m_GMDisableAutoSkill)
            {
                return false;
            }

            if (!playerHero.TryResolveAutoActionSkillForNormalAttack(out int autoActionSlot))
            {
                return false;
            }

            BattleManager.AttackDir(autoActionSlot, hero, faceForward, moveDir);
            return true;
        }

        public bool CanConsumeGuardHeroBoneAction(PropertyEntity hero, Skill skill, bool boneNormalAttack)
        {
            if (m_Battle == null || m_Battle.GM_IsPause() ||
                !BattleManager.ReadIsEntityValide(hero) || hero.ReadIsBeingControlled() ||
                hero.ReadIsFreeze() || skill == null)
            {
                return false;
            }

            int slot = skill.ReadSlot();
            if (m_Battle.ReadIsUpgradeChallengePreActive())
            {
                return slot == 0;
            }
            if (m_Battle.ReadIsUpgradeChallengeActive())
            {
                return slot == 0 && m_Battle.CanAddUpgradeChallengeScore(hero.ReadBattlePlayerId()) &&
                    BattleManager.IsCanUseBoneSkillDirect(hero, slot, boneNormalAttack);
            }
            if (m_Battle.ReadHasPendingMonsterRestore())
            {
                return false;
            }
            if (slot > 0 && m_Battle.RequiresPlayerSkillEnergy(hero, skill) &&
                !m_Battle.CanPlayerQueueActiveSkill(hero.ReadBattlePlayerId()))
            {
                return false;
            }
            return BattleManager.IsCanUseBoneSkillDirect(hero, slot, boneNormalAttack);
        }

        public bool TryGuardHeroBoneActionBySeat(int seatId, int slot, Vector3 faceForward,
            Vector3 moveDir, bool boneNormalAttack)
        {
            var hero = ReadGuardHeroBySeat(seatId);
            var skillManager = hero != null ? hero.GetSkillManager() : null;
            var skill = skillManager != null ? skillManager.ReadSkillBySlot(slot) : null;

            if (slot == 0 && hero is PlayerHero attackInputHero)
            {
                attackInputHero.MarkNormalAttackInput();
            }

            // 自动动作技能替换普攻后的持续释放：先顺延技能自己的发弹窗口。
            // 这里必须在 CanConsumeHeroBoneAction 之前处理，否则当前技能槽位不是 0 时会被拦掉，
            // 连点普攻只能等窗口结束再重新施放，表现就会断开。
            if (slot == 0 && hero is PlayerHero boneAutoActionHero)
            {
                var boneCurrentSkill = skillManager != null ? skillManager.GetCurrentSkill() : null;
                if (boneAutoActionHero.IsAutoActionNormalAttackSkill(boneCurrentSkill) &&
                    boneCurrentSkill is CommonBulletSkill boneAutoActionSkill &&
                    boneAutoActionSkill.TryRenewKeepShoot())
                {
                    MarkManualControl(hero);
                    return true;
                }
            }

            if (!CanConsumeGuardHeroBoneAction(hero, skill, boneNormalAttack))
            {
                return false;
            }
            if (m_Battle.ReadIsUpgradeChallengePreActive())
            {
                return TryHandleUpgradeChallengePreActiveAction(hero, faceForward);
            }

            if (faceForward.sqrMagnitude <= 0.0001f)
            {
                faceForward = hero.ReadForward();
            }
            if (faceForward.sqrMagnitude <= 0.0001f)
            {
                faceForward = Vector3.forward;
            }
            faceForward.Normalize();
            moveDir.y = 0f;

            if (!BattleManager.TryAttackBoneDir(slot, hero, faceForward, moveDir, boneNormalAttack))
            {
                return false;
            }
            MarkManualControl(hero);
            return true;
        }

        public bool TryGuardHeroActionBySeat(int seatId, int slot, Vector3 faceForward, Vector3 moveDir)
        {
            var hero = ReadGuardHeroBySeat(seatId);
            if (!BattleManager.ReadIsEntityValide(hero))
            {
                return false;
            }

            if (slot == 0 && hero is PlayerHero seatAttackInputHero)
            {
                seatAttackInputHero.MarkNormalAttackInput();
            }

            var rawFaceForward = faceForward;
            var rawMoveDir = moveDir;

            if (m_Battle != null && m_Battle.ReadIsUpgradeChallengePreActive())
            {
                return TryHandleUpgradeChallengePreActiveAction(hero, rawFaceForward);
            }

            if (m_Battle != null && m_Battle.ReadIsUpgradeChallengeActive())
            {
                return TryHandleUpgradeChallengeAction(hero, rawFaceForward, rawMoveDir);
            }

            if (m_Battle != null && m_Battle.ReadHasPendingMonsterRestore())
            {
                return false;
            }

            var playerId = hero.ReadBattlePlayerId();
            var skill = hero.GetSkillManager().ReadSkillBySlot(slot);
            if (skill == null)
            {
                return false;
            }

            // 仅真正消耗共享能量的主动技能才受能量门槛限制。
            if (slot > 0 &&
                m_Battle != null &&
                m_Battle.RequiresPlayerSkillEnergy(hero, skill) &&
                !m_Battle.CanPlayerQueueActiveSkill(playerId))
            {
                return false;
            }

            if (faceForward.sqrMagnitude <= 0.0001f)
            {
                faceForward = hero.ReadForward();
            }
            if (faceForward.sqrMagnitude <= 0.0001f)
            {
                faceForward = Vector3.forward;
            }
            faceForward.Normalize();
            moveDir.y = 0f;

            MarkManualControl(hero);

            // 激光持续发弹期间的普攻点击：直接顺延发弹窗口续射，不走 WillNext 排队。
            // 键盘等直连入口的 WillNext 记录因 defender 为空永远不会被消费（TryWillNextUseSkill 会丢弃），
            // 施法期间的点击会被吞掉，只能等窗口结束重开施法，导致连点时光束在宽限外断开。
            var currentSkill = hero.GetSkillManager().GetCurrentSkill();
            if (slot == 0 && currentSkill == skill && skill is CommonBulletSkill laserKeepSkill &&
                laserKeepSkill.TryRenewLaserKeepShoot())
            {
                return true;
            }

            // 自动动作技能替换普攻后同样要满足“持续释放”：连点/长按普攻时
            // 顺延该技能自己的发弹窗口，而不是重新施放。
            if (slot == 0 && hero is PlayerHero autoActionHero &&
                autoActionHero.IsAutoActionNormalAttackSkill(currentSkill) &&
                currentSkill is CommonBulletSkill autoActionSkill &&
                autoActionSkill.TryRenewKeepShoot())
            {
                return true;
            }

            var canCastNow = BattleManager.IsCanUseSkill_WillNextUseSkill(hero, slot, null);
            if (!canCastNow)
            {
                return true;
            }

            // 普攻确认可发起后，若动作技能条件满足则本次普攻替换为动作技能的施放
            if (TryCastAutoActionSkillInsteadOfNormalAttack(hero, faceForward, moveDir))
            {
                return true;
            }

            BattleManager.AttackDir(slot, hero, faceForward, moveDir);

            return true;
        }

        private bool TryHandleUpgradeChallengeAction(PropertyEntity hero, Vector3 faceForward, Vector3 moveDir)
        {
            if (hero == null || m_Battle == null)
            {
                return false;
            }

            if (hero is PlayerHero upgradeAttackInputHero)
            {
                upgradeAttackInputHero.MarkNormalAttackInput();
            }

            var playerId = hero.ReadBattlePlayerId();
            if (!m_Battle.CanAddUpgradeChallengeScore(playerId))
            {
                return false;
            }

            var castFaceForward = faceForward;
            if (castFaceForward.sqrMagnitude <= 0.0001f)
            {
                castFaceForward = hero.ReadForward();
            }
            if (castFaceForward.sqrMagnitude <= 0.0001f)
            {
                castFaceForward = Vector3.forward;
            }

            castFaceForward.Normalize();
            moveDir.y = 0f;

            MarkManualControl(hero);

            // 激光持续发弹期间的普攻点击同样直接续射（与常规路径一致），
            // 避免键盘入口 WillNext（defender 为空不被消费）吞点击导致光束断开。
            var skillManager = hero.GetSkillManager();
            var normalSkill = skillManager != null ? skillManager.ReadSkillBySlot(0) : null;
            if (normalSkill != null && skillManager.GetCurrentSkill() == normalSkill &&
                normalSkill is CommonBulletSkill laserKeepSkill &&
                laserKeepSkill.TryRenewLaserKeepShoot())
            {
                return true;
            }

            // 自动动作技能替换普攻后的持续释放：与常规普攻入口一致。
            var upgradeCurrentSkill = skillManager.GetCurrentSkill();
            if (hero is PlayerHero upgradeAutoActionHero &&
                upgradeAutoActionHero.IsAutoActionNormalAttackSkill(upgradeCurrentSkill) &&
                upgradeCurrentSkill is CommonBulletSkill upgradeAutoActionSkill &&
                upgradeAutoActionSkill.TryRenewKeepShoot())
            {
                return true;
            }

            // 升级挑战积分必须建立在”本次普攻可真实释放”的前提下，避免手势连点刷分。
            if (!BattleManager.IsCanUseSkill_WillNextUseSkill(hero, 0, null))
            {
                if (hero != null && BattleManager.ReadIsEntityValide(hero))
                {
                    if (hero.ReadIsBeingControlled())
                    {
                        Debug.Log("【无法攻击调试】英雄被控制，无法攻击");
                    }
                    else
                    {
                        var skill = hero.GetSkillManager()?.ReadSkillBySlot(0);
                        if (skill == null)
                        {
                            Debug.Log("【无法攻击调试】普攻技能为null");
                        }
                        else
                        {
                            var reason = hero.CanAttack(skill);
                            if (reason == AttackFailedReason.SystemError)
                            {
                                Debug.Log("【无法攻击调试】普攻被禁用(m_CannotUseNormalAttackNum>0或前置条件不满足)");
                            }
                            else if (reason == AttackFailedReason.Silence)
                            {
                                Debug.Log("【无法攻击调试】英雄被沉默");
                            }
                            else if (reason == AttackFailedReason.UnregisterSkill)
                            {
                                Debug.Log("【无法攻击调试】普攻技能未注册");
                            }
                        }
                    }
                }
                return true;
            }

            // 动作技能条件满足时，本次普攻输入替换为动作技能的施放（升级挑战阶段同样生效）
            if (TryCastAutoActionSkillInsteadOfNormalAttack(hero, castFaceForward, moveDir))
            {
                return true;
            }

            BattleManager.AttackDir(0, hero, castFaceForward, moveDir);
            return true;
        }

        private bool TryHandleUpgradeChallengePreActiveAction(PropertyEntity hero, Vector3 faceForward)
        {
            if (hero == null)
            {
                return false;
            }

            if (faceForward.sqrMagnitude <= 0.0001f)
            {
                faceForward = hero.ReadForward();
            }
            if (faceForward.sqrMagnitude <= 0.0001f)
            {
                faceForward = Vector3.forward;
            }

            var horizontalForward = new Vector3(faceForward.x, 0f, faceForward.z);
            if (horizontalForward.sqrMagnitude > 0.0001f)
            {
                hero.SetForward(horizontalForward.normalized);
            }
            hero.SetBaseForward(faceForward);

            MarkManualControl(hero);
            return true;
        }

        public PropertyEntity ReadGuardHeroBySeat(int seatId)
        {
            int heroCount = m_GuradHeroes.Count;
            for (int i = 0; i < heroCount; i++)
            {
                var hero = m_GuradHeroes[i];
                if (!BattleManager.ReadIsEntityValide(hero))
                {
                    continue;
                }

                if (hero.ReadBattlePlayerId() <= 0)
                {
                    continue;
                }

                var battlePlayer = m_Battle.GetPlayer(hero.ReadBattlePlayerId());
                if (battlePlayer != null && battlePlayer.m_SeatId == seatId)
                {
                    return hero;
                }
            }

            return null;
        }

        private void UpdateBossSkills()
        {
            if (m_MonsterSpawer == null)
            {
                return;
            }
            if (m_IsMonsterSuspended)
            {
                return;
            }

            // Boss 技能放在塔防刷怪器的运行时里统一更新，
            // 这样可以直接复用现有的怪物列表、主题信息和英雄列表，不额外拆一层系统。
            SyncBossSkillRuntimes();

            float now = BattleManager.ReadBattleTime();
            foreach (var kv in m_BossSkillRuntimes)
            {
                var runtime = kv.Value;
                var boss = BattleManager.GetObjectManager().ReadPropertyEntityById(runtime.m_BossEntityId) as MasterHero;
                if (!BattleManager.ReadIsEntityValide(boss))
                {
                    continue;
                }

                if (runtime.m_IsCasting)
                {
                    if (now >= runtime.m_CastResolveTime)
                    {
                        runtime.m_IsCasting = false;
                        runtime.m_NextCastTime = runtime.m_CastResolveTime + runtime.m_Cooldown;
                        TrySummonBossClonesOnCastResolved(boss);
                    }

                    continue;
                }

                if (now >= runtime.m_NextCastTime)
                {
                    if (!TryStartBossSkillCast(boss, runtime, now))
                    {
                        // 目标不合法或单位状态不允许时短暂重试，避免整轮卡死。
                        runtime.m_NextCastTime = now + 0.2f;
                    }
                }
            }
        }

        private void ShiftBossSkillRuntimeTime(float pausedDuration)
        {
            if (pausedDuration <= 0)
            {
                return;
            }

            foreach (var kv in m_BossSkillRuntimes)
            {
                var runtime = kv.Value;
                if (runtime == null)
                {
                    continue;
                }

                runtime.m_NextCastTime += pausedDuration;
                if (runtime.m_IsCasting)
                {
                    runtime.m_CastResolveTime += pausedDuration;
                }
            }
        }

        private void SyncBossSkillRuntimes()
        {
            m_BossCandidates.Clear();
            m_MonsterSpawer.ReadAliveMonsters(m_BossCandidates);

            var invalidIds = new List<int>();
            foreach (var kv in m_BossSkillRuntimes)
            {
                var boss = BattleManager.GetObjectManager().ReadPropertyEntityById(kv.Key) as MasterHero;
                if (!BattleManager.ReadIsEntityValide(boss) ||
                    m_MonsterSpawer.ReadMonsterKind(kv.Key) != MonsterType.Boss ||
                    !TryResolveBossActiveSkill(boss, out _, out _))
                {
                    invalidIds.Add(kv.Key);
                }
            }

            int invalidCount = invalidIds.Count;
            for (int i = 0; i < invalidCount; i++)
            {
                m_BossSkillRuntimes.Remove(invalidIds[i]);
            }

            int candidateCount = m_BossCandidates.Count;
            for (int i = 0; i < candidateCount; i++)
            {
                var boss = m_BossCandidates[i] as MasterHero;
                if (!BattleManager.ReadIsEntityValide(boss) || m_MonsterSpawer.ReadMonsterKind(boss.ReadId()) != MonsterType.Boss)
                {
                    continue;
                }

                if (m_BossSkillRuntimes.ContainsKey(boss.ReadId()))
                {
                    continue;
                }

                if (!TryResolveBossActiveSkill(boss, out _, out var skillBean))
                {
                    // 塔防里的 Boss 允许只有普攻，没有 slot 1 主动技能。
                    // 这种情况下不创建 Boss 技能运行时，避免把合法配置误判成致命错误。
                    continue;
                }

                var runtime = new TowerDefendBossSkillRuntimeData();
                runtime.m_BossEntityId = boss.ReadId();
                runtime.m_Cooldown = ResolveBossSkillCooldownSec(skillBean);
                runtime.m_NextCastTime = BattleManager.ReadBattleTime() + runtime.m_Cooldown;
                m_BossSkillRuntimes.Add(runtime.m_BossEntityId, runtime);
            }
        }

        private bool TryResolveBossActiveSkill(MasterHero boss, out Skill skill, out t_skillBean skillBean)
        {
            skill = null;
            skillBean = null;
            if (boss == null)
            {
                return false;
            }

            skill = boss.GetSkillManager().ReadSkillBySlot(1);
            if (skill == null)
            {
                return false;
            }

            skillBean = skill.GetSkillBean();
            return skillBean != null;
        }

        private float ResolveBossSkillCooldownSec(t_skillBean skillBean)
        {
            if (skillBean == null)
            {
                throw new InvalidOperationException("塔防 Boss 技能冷却配置缺失：技能配置为空。");
            }

            return Mathf.Max(1.0f, skillBean.t_cooldown / 1000.0f);
        }

        private void TrySummonBossClonesOnCastResolved(MasterHero boss)
        {
            if (boss == null || m_MonsterSpawer == null)
            {
                return;
            }

            // 分身不再分身，只有本体施放技能时才触发召唤。
            if (m_MonsterSpawer.ReadIsSummonClone(boss.ReadId()))
            {
                return;
            }

            if (!TryResolveBossActiveSkill(boss, out _, out var skillBean) || skillBean == null)
            {
                return;
            }

            var summonCount = (int)skillBean.t_summon;
            if (summonCount <= 0)
            {
                return;
            }

            m_MonsterSpawer.SpawnSummonedBossClones(boss, summonCount);
        }

        private bool TryStartBossSkillCast(MasterHero boss, TowerDefendBossSkillRuntimeData runtime, float now)
        {
            if (boss == null || runtime == null || !BattleManager.IsCanUseSkillDirect(boss, 1))
            {
                return false;
            }

            if (!TryResolveBossActiveSkill(boss, out var skill, out var skillBean))
            {
                return false;
            }

            // 施放前先随机锁定一个施法距离内的守卫英雄，本次施放围绕它来摆朝向和落点
            var target = SelectRandomAliveHeroInDist(boss.GetPosition(), boss.GetSkillCastDist(skill));
            var forward = ResolveBossSkillLaunchForward(boss, target);

            BattleManager.AttackDir(1, boss, forward, Vector3.zero);
            // 朝向只决定出膛方向，真正把目标带进发弹的是技能上的出生目标；
            // 不写回的话子弹会退回“飞行中按负载最低/最近自己再挑一个”的逻辑，随机挑选就白做了。
            skill.SetDefender(target);

            runtime.m_IsCasting = true;
            runtime.m_CastResolveTime = now + ResolveBossSkillCastPrepareSec(boss, skill);
            runtime.m_Cooldown = ResolveBossSkillCooldownSec(skillBean);
            return true;
        }

        private Vector3 ResolveBossSkillLaunchForward(MasterHero boss, PropertyEntity target)
        {
            var forward = Vector3.zero;
            if (BattleManager.ReadIsEntityValide(target))
            {
                forward = target.GetPosition() - boss.GetPosition();
            }
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = boss.ReadForward();
            }

            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();
            return forward;
        }

        private float ResolveBossSkillCastPrepareSec(MasterHero boss, Skill skill)
        {
            if (boss != null)
            {
                var bean = boss.GetMonsterBean();
                if (bean != null && bean.t_td_boss_skill_prepare_ms > 0)
                {
                    return Mathf.Max(0.1f, bean.t_td_boss_skill_prepare_ms / 1000.0f);
                }
            }

            return ResolveSkillCastPointSec(skill);
        }

        private float ResolveSkillCastPointSec(Skill skill)
        {
            if (skill == null)
            {
                return 0.1f;
            }

            var action = skill.GetActionData();
            if (action == null)
            {
                return 0.1f;
            }

            int frameRate = action.t_frame_rate > 0 ? action.t_frame_rate : 30;
            int castPointFrame = Mathf.Max(0, action.t_ac_cast_point);
            var castPointSec = BattleManager.ConvertFrame2Second(castPointFrame, 100.0f, frameRate);
            return Mathf.Max(0.1f, castPointSec);
        }

        // 在存活守卫英雄里随机挑一个目标；maxDistance 非正数时视为不限距离。
        private PropertyEntity SelectRandomAliveHeroInDist(Vector3 origin, float maxDistance)
        {
            m_TargetCandidates.Clear();
            int heroCount = m_GuradHeroes.Count;
            for (int i = 0; i < heroCount; i++)
            {
                var hero = m_GuradHeroes[i];
                if (!BattleManager.ReadIsEntityValide(hero))
                {
                    continue;
                }

                if (maxDistance > 0f && Vector3.Distance(origin, hero.GetPosition()) > maxDistance)
                {
                    continue;
                }

                m_TargetCandidates.Add(hero);
            }

            if (m_TargetCandidates.Count <= 0)
            {
                return null;
            }

            return m_TargetCandidates[UnityEngine.Random.Range(0, m_TargetCandidates.Count)];
        }

        public void MarkManualControl(PropertyEntity hero)
        {
            if (hero == null)
            {
                return;
            }

            m_ManualControlExpireTimes[hero.ReadId()] = BattleManager.ReadBattleTime() + m_ManualControlAutoAttackBlockSec;
        }

        private bool IsManualControlBlockingAutoAttack(PropertyEntity hero)
        {
            if (hero == null)
            {
                return false;
            }

            float expireTime;
            if (!m_ManualControlExpireTimes.TryGetValue(hero.ReadId(), out expireTime))
            {
                return false;
            }

            if (expireTime <= BattleManager.ReadBattleTime())
            {
                m_ManualControlExpireTimes.Remove(hero.ReadId());
                return false;
            }

            return true;
        }

    }
}
