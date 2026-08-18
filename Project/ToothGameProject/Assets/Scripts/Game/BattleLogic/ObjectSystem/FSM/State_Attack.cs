using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    //攻击状态的标准是：先选择攻击对象，在选择适合的攻击距离和冷却的技能
    public class State_Attack : StateBase
    {


        public static int m_GMGroupNotAttack = -1;


        public override StateLevel GetStateLevel()
        {
            return StateLevel.Normal;
        }
        protected override void Start(emEntityState prevState)
        {
            var render = m_Owner != null ? m_Owner.GetRender() : null;
            if (render != null)
            {
                render.SetMoveSpeed(0);
                render.SetAnimationSpeed(1.0f);
            }
        }



        protected override void Update(float dt)
        {


        }
        protected override void End(emEntityState nextState)
        {
        }
    }

}