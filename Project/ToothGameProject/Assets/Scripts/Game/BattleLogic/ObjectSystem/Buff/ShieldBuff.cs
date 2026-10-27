using MonoBean;
using UnityEngine;

namespace GameDll
{
    public class ShieldBuff : Buff
    {
        private float m_ShieldValue;

        protected override void ChangeProperties()
        {
            var cfg = ReadShieldParam();
            var shieldValue = 0f;
            if (cfg != null)
            {
                shieldValue = cfg.t_hp;
                if (cfg.t_hp_percen != 0 && m_TargetEnt != null)
                {
                    shieldValue += m_TargetEnt.GetMaxHP() * cfg.t_hp_percen / 1000f;
                }
            }

            m_ShieldValue = Mathf.Max(0, shieldValue);
        }

        public override void AddBuffAgain()
        {
            // 护盾重复获得只刷新持续时间，不叠加护盾量。
            RefreshDuration();
        }

        public float ReadShieldValue()
        {
            return m_ShieldValue;
        }

        public float AbsorbDamage(float value)
        {
            if (value <= 0 || m_ShieldValue <= 0)
            {
                return value;
            }

            var absorbed = Mathf.Min(value, m_ShieldValue);
            m_ShieldValue -= absorbed;
            if (m_ShieldValue <= 0 && m_TargetEnt != null)
            {
                var buffMgr = m_TargetEnt.GetBuffManager();
                if (buffMgr != null)
                {
                    buffMgr.RemoveBuff(GetId());
                }
            }
            else if (m_ShieldValue > 0)
            {
                // 盾被打破时由 RemoveBuffImp 统一通知，这里只通知盾量扣减但未破的情况。
                RenderEvent.Event.OnShieldChanged(m_TargetEnt);
            }

            return value - absorbed;
        }

        private t_buffParam ReadShieldParam()
        {
            if (m_Bean == null || m_Bean.t_buff_param_id == null || m_Bean.t_buff_param_id.Count <= 0)
            {
                return null;
            }

            return t_buffParam.GetConfig(m_Bean.t_buff_param_id[0], false);
        }
    }
}
