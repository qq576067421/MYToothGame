using UnityEngine;
using System.Collections;
using LCL;
using GameDll;
using MonoBean;

namespace GameDll
{
    public class RecoveryBuff : Buff
    {
        private int m_PropertyType = (int)PropertyType.None;
        private float m_Value = 0;

        protected override void ChangeProperties()
        {
            var t_buff_param_id = m_Bean.t_buff_param_id[0];
            var cfg = t_buffParam.GetConfig(t_buff_param_id);
            if (cfg == null)
            {
                return;
            }

            if (cfg.t_hp != 0 || cfg.t_hp_percen != 0)
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

            DoAction(m_SourceEnt, m_TargetEnt);
            m_LastActionTime = (int)BattleManager.ReadBattleTime();
        }

        public override int DoAction(PropertyEntity source, PropertyEntity target)
        {
            if(!BattleManager.GetBattleTool().ReadIsEntityValide(target))
            {
                return 0;
            }
            if (m_PropertyType == (int)PropertyType.hp)
            {
                //回血
                if(m_Value > 0)
                {
                    BattleManager.GetBattleTool().AddHP(target, m_Value, true);
                }
                else
                {
                    BattleManager.GetBattleTool().SubHP(target, -m_Value, true, source);
                }
                return 1;
            }
            else if(m_PropertyType == (int)PropertyType.runtime_magic)
            {
                //回蓝
                if (m_Value > 0)
                {
                    BattleManager.GetBattleTool().AddMagic(target, m_Value, true);
                }
                else
                {
                    BattleManager.GetBattleTool().SubMagic(target, -m_Value, true);
                }
                return 1;
            }
            return 0;

        }

    }
}

