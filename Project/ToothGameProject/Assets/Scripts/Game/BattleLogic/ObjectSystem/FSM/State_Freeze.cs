namespace GameDll
{
    // 冰冻等失去行动能力的控制状态：保持当前姿势定格，期间禁止切入攻击/移动等状态。
    public class State_Freeze : StateBase
    {
        protected override void Start(emEntityState prevState)
        {
            var render = m_Owner != null ? m_Owner.GetRender() : null;
            if (render != null)
            {
                render.SetMoveSpeed(0);
                render.SetAnimationSpeed(0);
            }
        }

        protected override void Update(float dt)
        {

        }

        protected override void End(emEntityState nextState)
        {
            var render = m_Owner != null ? m_Owner.GetRender() : null;
            if (render != null)
            {
                // 死亡状态不会自行重置动画速度，这里必须统一恢复，避免死亡动画被冻在速度0。
                render.SetAnimationSpeed(1);
            }
        }

        public override StateLevel GetStateLevel()
        {
            return StateLevel.Trapped;
        }
    }
}
