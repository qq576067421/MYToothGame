using UnityEngine;
using System.Collections;
using LCL;
using GameDll;
using System.Collections.Generic;

namespace GameDll
{
    //只有眩晕等动作
    public class ActionBuff : Buff
    {
        private bool m_BlockApplied;

        protected override void AddSpecialAction()
        {
            if (m_TargetEnt == null)
            {
                return;
            }

            m_TargetEnt.AddCannotUseNormalAttackNum(1);
            m_TargetEnt.AddCannotUseSkillNum(1);
            m_TargetEnt.TryChangeState(emEntityState.em_EntityState_Idle, true);
            m_BlockApplied = true;
        }

        protected override void OnRemoveImp()
        {
            if (m_BlockApplied && m_TargetEnt != null)
            {
                m_TargetEnt.AddCannotUseNormalAttackNum(-1);
                m_TargetEnt.AddCannotUseSkillNum(-1);
                m_BlockApplied = false;
            }

            base.OnRemoveImp();
        }
    }
}
