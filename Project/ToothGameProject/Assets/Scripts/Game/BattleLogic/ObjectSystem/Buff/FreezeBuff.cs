namespace GameDll
{
    // 冰冻buff：禁普攻禁技能、中断进行中的施法并切入 Freeze 状态定格动画，解除时全部恢复。
    public class FreezeBuff : Buff
    {
        private bool m_BlockApplied;

        protected override void AddSpecialAction()
        {
            if (m_TargetEnt == null)
            {
                return;
            }

            // 只对英雄/怪物类实体生效，避免子弹误中基地等特殊实体时挂上禁攻计数。
            if (!m_TargetEnt.ReadIsHero() && !m_TargetEnt.ReadIsSmallMonster() && !m_TargetEnt.ReadIsBoss())
            {
                return;
            }

            // 死亡目标不再进入冰冻，避免 must 切换把死亡状态顶掉。
            if (m_TargetEnt.GetStateManager().ReadIsState(emEntityState.em_EntityState_Dead))
            {
                return;
            }

            m_TargetEnt.AddCannotUseNormalAttackNum(1);
            m_TargetEnt.AddCannotUseSkillNum(1);
            m_TargetEnt.BreakSkill();
            m_TargetEnt.TryChangeState(emEntityState.em_EntityState_Freeze, true);
            m_BlockApplied = true;
        }

        protected override void OnRemoveImp()
        {
            if (m_BlockApplied && m_TargetEnt != null)
            {
                m_TargetEnt.AddCannotUseNormalAttackNum(-1);
                m_TargetEnt.AddCannotUseSkillNum(-1);
                if (m_TargetEnt.GetStateManager().ReadIsState(emEntityState.em_EntityState_Freeze))
                {
                    m_TargetEnt.TryChangeState(emEntityState.em_EntityState_Idle, true);
                }
                m_BlockApplied = false;
            }

            base.OnRemoveImp();
        }
    }
}
