using System;
namespace GameDll
{
    public class State_Idle : StateBase
    {
        public float m_StartIdleTime = 0;
        protected override void Start(emEntityState prevState)
        {
            var render = m_Owner.GetRender();
            m_Owner.GetRender().SetAnimationSpeed(1);
            render.PlayAnimation("idle");
            m_StartIdleTime = BattleManager.ReadBattleTime();
        }

        protected override void Update(float dt)
        {


        }
        protected override void End(emEntityState nextState)
        {
        }
        public override StateLevel GetStateLevel()
        {
            return StateLevel.Normal;
        }
    }

}