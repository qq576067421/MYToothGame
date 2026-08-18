using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    public class State_Born : StateBase
    {
        private float m_BornTime = 2.0f;
        private float m_StartTime = 0;

        public override StateLevel GetStateLevel()
        {
            return StateLevel.Dead;
        }
        protected override void Start(emEntityState prevState)
        {
            m_StartTime = BattleManager.ReadBattleTime();
        }
        protected override void Update(float dt)
        {
            if(BattleManager.ReadBattleTime() - m_StartTime >= m_BornTime)
            {
                m_Owner.TryChangeState(emEntityState.em_EntityState_Idle);
            }

        }
        protected override void End(emEntityState nextState)
        {


        }
    }

}