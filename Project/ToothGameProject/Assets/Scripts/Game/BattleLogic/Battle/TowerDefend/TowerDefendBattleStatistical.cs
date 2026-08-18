using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoBean;

namespace GameDll
{
    public sealed class TowerDefendMonsterKillDetailRuntimeData
    {
        public long m_ConfigId;
        public MonsterType m_MonsterKind;
        public string m_Name = string.Empty;
        public string m_Icon = string.Empty;
        public int m_KillCount;
    }
    public class TowerDefendBattleStatistical : IBattleStatistical
    {
        private int m_WellDeadCount = 0;
        private int m_GuardHeroDeadCount = 0;
        private int m_PushHeroDeadCount = 0;
        private int m_NormalMonsterDeadCount = 0;
        private int m_EliteMonsterDeadCount = 0;
        private int m_BossMonsterDeadCount = 0;
        private int m_MonsterKillGold = 0;
        private readonly Dictionary<long, TowerDefendMonsterKillDetailRuntimeData> m_MonsterKillDetails =
            new Dictionary<long, TowerDefendMonsterKillDetailRuntimeData>();

        public override void Init()
        {

        }

        public override void Update(float dt)
        {

        }

        public override void Destroy()
        {

        }

        public override int ReadGuardHeroDeadCount()
        {
            return m_GuardHeroDeadCount;
        }

        public override int ReadPushHeroDeadCount()
        {
            return m_PushHeroDeadCount;
        }

        public override int ReadWellDeadCount()
        {
            return m_WellDeadCount;
        }
        public int ReadNormalMonsterDeadCount()
        {
            return m_NormalMonsterDeadCount;
        }
        public int ReadEliteMonsterDeadCount()
        {
            return m_EliteMonsterDeadCount;
        }
        public int ReadBossMonsterDeadCount()
        {
            return m_BossMonsterDeadCount;
        }
        public int ReadMonsterKillGold()
        {
            return m_MonsterKillGold;
        }
        public List<BattleResultMonsterDetailData> BuildMonsterKillDetailResults()
        {
            var results = new List<BattleResultMonsterDetailData>();
            foreach (var kv in m_MonsterKillDetails)
            {
                var detail = kv.Value;
                if (detail == null || detail.m_KillCount <= 0)
                {
                    continue;
                }

                results.Add(new BattleResultMonsterDetailData
                {
                    m_ConfigId = detail.m_ConfigId,
                    m_MonsterKind = (int)detail.m_MonsterKind,
                    m_Name = detail.m_Name,
                    m_Icon = detail.m_Icon,
                    m_KillCount = detail.m_KillCount,
                });
            }

            results.Sort((left, right) =>
            {
                var kindCompare = right.m_MonsterKind.CompareTo(left.m_MonsterKind);
                if (kindCompare != 0)
                {
                    return kindCompare;
                }

                var countCompare = right.m_KillCount.CompareTo(left.m_KillCount);
                if (countCompare != 0)
                {
                    return countCompare;
                }

                return left.m_ConfigId.CompareTo(right.m_ConfigId);
            });
            return results;
        }
        public override void OnHeroDead(PlayerHero hero, int attack_id, long player_id)
        {
            base.OnHeroDead(hero, attack_id, player_id);

            if (hero != null && hero.ReadGroup() == GroupId.PushGroupId)
            {
                m_PushHeroDeadCount++;
            }
            else
            {
                m_GuardHeroDeadCount++;
            }

        }
        public override void OnWellDead(int wellId, int attack_id, long player_id)
        {
            m_WellDeadCount++;
            RenderEvent.Event.OnWellDead(wellId, attack_id);
        }

        public override void OnMonsterDead(int entity_id, int attack_id, long player_id)
        {
            base.OnMonsterDead(entity_id, attack_id, player_id);
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            var monsterKind = battle != null ? battle.ReadMonsterKind(entity_id) : MonsterType.Normal;
            if (monsterKind == MonsterType.Elite)
            {
                m_EliteMonsterDeadCount++;
            }
            else
            {
                m_NormalMonsterDeadCount++;
            }
            RecordMonsterKillDetail(entity_id, monsterKind);
            ApplyMonsterKillRewards(entity_id);
        }

        public override void OnBossDead(int entity_id, int attack_id, long player_id)
        {
            base.OnBossDead(entity_id, attack_id, player_id);
            m_BossMonsterDeadCount++;
            RecordMonsterKillDetail(entity_id, MonsterType.Boss);
            ApplyMonsterKillRewards(entity_id);
        }

        private void RecordMonsterKillDetail(int entityId, MonsterType monsterKind)
        {
            var entity = BattleManager.GetObjectManager().ReadPropertyEntityById(entityId);
            if (entity == null)
            {
                return;
            }

            var cfgId = entity.ReadBeanId();
            if (cfgId <= 0)
            {
                return;
            }

            TowerDefendMonsterKillDetailRuntimeData detail;
            if (!m_MonsterKillDetails.TryGetValue(cfgId, out detail))
            {
                detail = new TowerDefendMonsterKillDetailRuntimeData();
                detail.m_ConfigId = cfgId;
                detail.m_MonsterKind = monsterKind;
                FillMonsterDisplayInfo(detail);
                m_MonsterKillDetails.Add(cfgId, detail);
            }

            detail.m_MonsterKind = monsterKind;
            detail.m_KillCount++;
        }

        private void FillMonsterDisplayInfo(TowerDefendMonsterKillDetailRuntimeData detail)
        {
            if (detail == null || detail.m_ConfigId <= 0)
            {
                return;
            }

            var monsterCfg = t_monsterBean.GetConfig(detail.m_ConfigId, false);
            if (monsterCfg != null)
            {
                detail.m_Name = monsterCfg.t_name;
                detail.m_Icon = monsterCfg.t_head;
            }
        }

        private void ApplyMonsterKillRewards(int entityId)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            var monster = BattleManager.GetObjectManager().ReadPropertyEntityById(entityId);
            if (battle == null || monster == null)
            {
                return;
            }

            var rewardExp = Math.Max(0, battle.ReadMonsterKillRewardExp());
            var rewardCoin = Math.Max(0, battle.ReadMonsterKillRewardCoin());
            if (rewardCoin > 0)
            {
                m_MonsterKillGold += rewardCoin;
                RenderEvent.Event.ShowRewardCoin(monster, rewardCoin);
            }

            battle.AddTeamExp(rewardExp);
        }
    }
}
