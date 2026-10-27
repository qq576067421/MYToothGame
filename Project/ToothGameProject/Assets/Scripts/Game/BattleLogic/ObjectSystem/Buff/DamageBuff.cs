using UnityEngine;
using System.Collections;
using LCL;
using GameDll;
using MonoBean;

namespace GameDll
{
    //每隔t_buff_gap毫秒损失血蓝
    public class DamageBuff : Buff
    {
        private int m_PropertyType = (int)PropertyType.None;
        private float m_Value = 0;

        protected override void ChangeProperties()
        {
            //只取样数值，不立即结算：第一跳在 t_buff_gap 后由 Update 触发，
            //避免与子弹直伤同帧出两个伤害数字
            ReadTickValue();
            m_LastActionTime = (int)BattleManager.ReadBattleTime();
        }

        public override void AddBuffAgain()
        {
            base.AddBuffAgain();
            //刷新时只按施法者当前属性重取样每跳数值，不立即额外结算一跳
            ReadTickValue();
        }

        private void ReadTickValue()
        {
            var t_buff_param_id = m_Bean.t_buff_param_id[0];
            var cfg = t_buffParam.GetConfig(t_buff_param_id);
            if (cfg == null)
            {
                return;
            }

            //t_buff_damage=1 的伤害 buff 每跳取来源子弹的技能伤害；=0 的无伤害 buff 不读子弹表
            var skillDamageCfg = cfg.t_buff_damage > 0 ? m_SourceSkillDamageCfg : 0;
            if (skillDamageCfg > 0)
            {
                m_PropertyType = (int)PropertyType.hp;
                m_Value = 0;
                if (m_SourceEnt != null)
                {
                    m_Value = m_SourceEnt.GetAtk() * skillDamageCfg / 1000f * m_SourceEnt.ReadDamageAmpPercent();
                }
            }
            else if (cfg.t_hp != 0 || cfg.t_hp_percen != 0)
            {
                m_PropertyType = (int)PropertyType.hp;
                m_Value = cfg.t_hp;
                if (cfg.t_hp_percen != 0 && m_TargetEnt != null)
                {
                    m_Value += m_TargetEnt.GetMaxHP() * cfg.t_hp_percen / 1000f;
                }
            }
            else
            {
                m_PropertyType = (int)PropertyType.runtime_magic;
                m_Value = 0;
            }
        }

        public override int DoAction(PropertyEntity source, PropertyEntity target)
        {
            if(!BattleManager.GetBattleTool().ReadIsEntityValide(target))
            {
                return 0;
            }
            if (m_PropertyType == (int)PropertyType.hp)
            {
                //扣血
                var hurt = m_Value * target.ReadDamageTakenAmpPercent();
                BattleManager.GetBattleTool().SubHP(target, hurt, true, source);
                return 1;
            }
            else if(m_PropertyType == (int)PropertyType.runtime_magic)
            {
                //扣蓝
                BattleManager.GetBattleTool().SubMagic(target, m_Value, true);
                return 1;
            }
            return 0;

        }

    }
}
