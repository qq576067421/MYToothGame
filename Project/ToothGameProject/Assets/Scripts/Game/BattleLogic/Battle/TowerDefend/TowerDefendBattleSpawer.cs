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

        public bool TryGuardHeroActionBySeat(int seatId, int slot, Vector3 faceForward, Vector3 moveDir)
        {
            var hero = ReadGuardHeroBySeat(seatId);
            if (!BattleManager.ReadIsEntityValide(hero))
            {
                return false;
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

            var canCastNow = BattleManager.IsCanUseSkill_WillNextUseSkill(hero, slot, null);
            if (!canCastNow)
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
            // 升级挑战积分必须建立在“本次普攻可真实释放”的前提下，避免手势连点刷分。
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

            var target = SelectBossSkillPrimaryTarget(boss, skill);
            var forward = boss.ReadForward();
            if (forward.sqrMagnitude <= 0.0001f && BattleManager.ReadIsEntityValide(target))
            {
                forward = target.GetPosition() - boss.GetPosition();
            }
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();
            BattleManager.AttackDir(1, boss, forward, Vector3.zero);

            runtime.m_IsCasting = true;
            runtime.m_CastResolveTime = now + ResolveBossSkillCastPrepareSec(boss, skill);
            runtime.m_Cooldown = ResolveBossSkillCooldownSec(skillBean);
            return true;
        }

        private PropertyEntity SelectBossSkillPrimaryTarget(MasterHero boss, Skill skill)
        {
            if (boss == null || skill == null)
            {
                return null;
            }

            var aliveHeroes = CollectAliveHeroesByHpPercentAsc();
            if (aliveHeroes.Count <= 0)
            {
                return null;
            }

            var castDistance = boss.GetSkillCastDist(skill);
            var bossPosition = boss.GetPosition();
            PropertyEntity nearest = null;
            float nearestDistance = float.MaxValue;
            int heroCount = aliveHeroes.Count;
            for (int i = 0; i < heroCount; i++)
            {
                var hero = aliveHeroes[i];
                if (!BattleManager.ReadIsEntityValide(hero))
                {
                    continue;
                }

                float distance = Vector3.Distance(bossPosition, hero.GetPosition());
                if (distance > castDistance || distance >= nearestDistance)
                {
                    continue;
                }

                nearest = hero;
                nearestDistance = distance;
            }

            return nearest;
        }

        private List<PropertyEntity> CollectAliveHeroesByHpPercentAsc()
        {
            var aliveHeroes = new List<PropertyEntity>();
            int heroCount = m_GuradHeroes.Count;
            for (int i = 0; i < heroCount; i++)
            {
                var hero = m_GuradHeroes[i];
                if (BattleManager.ReadIsEntityValide(hero))
                {
                    aliveHeroes.Add(hero);
                }
            }

            aliveHeroes.Sort((a, b) =>
            {
                float hpA = ReadHpPercent(a);
                float hpB = ReadHpPercent(b);
                return hpA.CompareTo(hpB);
            });

            return aliveHeroes;
        }

        private float ReadHpPercent(PropertyEntity entity)
        {
            if (entity == null)
            {
                return 1f;
            }

            var maxHp = entity.GetMaxHP();
            if (maxHp <= 0)
            {
                return 1f;
            }

            return Mathf.Clamp01(entity.ReadHP() / (float)maxHp);
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

        private PropertyEntity SelectRandomAliveHero()
        {
            m_TargetCandidates.Clear();
            int heroCount = m_GuradHeroes.Count;
            for (int i = 0; i < heroCount; i++)
            {
                var hero = m_GuradHeroes[i];
                if (BattleManager.ReadIsEntityValide(hero))
                {
                    m_TargetCandidates.Add(hero);
                }
            }

            if (m_TargetCandidates.Count == 0)
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
