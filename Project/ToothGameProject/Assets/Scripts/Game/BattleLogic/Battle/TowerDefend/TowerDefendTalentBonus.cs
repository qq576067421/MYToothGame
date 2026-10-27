using System.Collections.Generic;
using MonoBean;
using UnityEngine;

namespace GameDll
{
    // 天赋战斗加成汇总：把玩家已解锁的天赋按 t_talentBean 汇总成战斗内可直接使用的数值。
    // 天赋表的大型节点行同时配置普通属性和特殊天赋，但两者在大厅里是分开付费解锁的：
    // 主线列表（m_Talents，按顺序解锁的行 id）只结算攻击/暴击/暴伤/伤害加深；
    // 分支列表（m_TalentBranches，单独解锁的大型节点行 id）只结算特殊天赋，
    // 不能按主线解锁连带让特殊天赋生效。
    // 特殊天赋按类型换算成基地血量、充能速度、技能CD、状态持续时间与无尽模式巅峰天赋。
    public sealed class TowerDefendTalentBonus
    {
        public const int m_SpecialTypeBaseHealth = 1;
        public const int m_SpecialTypeChargeSpeed = 2;
        public const int m_SpecialTypeSkillCdReduce = 3;
        public const int m_SpecialTypeDuration = 4;
        public const int m_SpecialTypePeakTalent = 5;

        private const float m_PermillePerSkillEnergyPoint = 200f;

        public float m_AtkAdd;
        public float m_CritAdd;
        public float m_CritDamageAdd;
        public float m_AmpAdd;
        public float m_DurationAddSeconds;
        public int m_BaseHealthAdd;
        public int m_ChargeSpeedAddPermillePerSecond;
        public float m_SkillCdReduceSeconds;
        public int m_PeakAtkPerWave;

        public bool ReadIsEmpty()
        {
            return m_AtkAdd <= 0f &&
                m_CritAdd <= 0f &&
                m_CritDamageAdd <= 0f &&
                m_AmpAdd <= 0f &&
                m_DurationAddSeconds <= 0f &&
                m_BaseHealthAdd <= 0 &&
                m_ChargeSpeedAddPermillePerSecond <= 0 &&
                m_SkillCdReduceSeconds <= 0f &&
                m_PeakAtkPerWave <= 0;
        }

        public static TowerDefendTalentBonus Build(IList<int> mainTalents, IList<int> branchTalents)
        {
            var bonus = new TowerDefendTalentBonus();
            bool hasMainTalents = mainTalents != null && mainTalents.Count > 0;
            bool hasBranchTalents = branchTalents != null && branchTalents.Count > 0;
            if (!hasMainTalents && !hasBranchTalents)
            {
                return bonus;
            }

            if (hasMainTalents)
            {
                for (int i = 0; i < mainTalents.Count; i++)
                {
                    var cfg = t_talentBean.GetConfig(mainTalents[i], false);
                    if (cfg == null)
                    {
                        continue;
                    }

                    bonus.m_AtkAdd += cfg.t_atk;
                    bonus.m_CritAdd += cfg.t_crit / 100f;
                    bonus.m_CritDamageAdd += cfg.t_crit_damage / 100f;
                    bonus.m_AmpAdd += cfg.t_amp / 100f;
                }
            }

            if (hasBranchTalents)
            {
                for (int i = 0; i < branchTalents.Count; i++)
                {
                    var cfg = t_talentBean.GetConfig(branchTalents[i], false);
                    if (cfg == null)
                    {
                        continue;
                    }

                    ApplySpecialTalent(bonus, cfg.t_special_talent_type, cfg.t_special_talent_value);
                }
            }

            return bonus;
        }

        private static void ApplySpecialTalent(TowerDefendTalentBonus bonus, int specialType, int specialValue)
        {
            if (specialType <= 0 || specialValue <= 0)
            {
                return;
            }

            switch (specialType)
            {
                case m_SpecialTypeBaseHealth:
                    bonus.m_BaseHealthAdd += specialValue;
                    break;
                case m_SpecialTypeChargeSpeed:
                    bonus.m_ChargeSpeedAddPermillePerSecond += Mathf.RoundToInt(specialValue * m_PermillePerSkillEnergyPoint);
                    break;
                case m_SpecialTypeSkillCdReduce:
                    bonus.m_SkillCdReduceSeconds += specialValue;
                    break;
                case m_SpecialTypeDuration:
                    bonus.m_DurationAddSeconds += specialValue;
                    break;
                case m_SpecialTypePeakTalent:
                    bonus.m_PeakAtkPerWave += specialValue;
                    break;
            }
        }
    }
}
