using System;
using UnityEngine;
namespace GameDll
{
    public class State_Move : StateBase
    {
        public static bool m_GM_DisableMove = false;
        private Vector3 m_Forward = Vector3.zero;
        private Vector3 m_CurForward = Vector3.zero;


        public void SetForward(int x, int z)
        {
            m_Forward = new Vector3(x, 0, z);
            m_Forward = m_Forward.normalized;

        }
        public void SetForward(Vector3 forward)
        {
            m_Forward = forward;
        }

        public override StateLevel GetStateLevel()
        {
            return StateLevel.Normal;
        }

        protected override void Start(emEntityState prevState)
        {
            var render = m_Owner.GetRender();
            //注意逻辑计算用3000，但是数值显示需要显示成300
            //后面的 * 2 / 3是因为我感觉移动速度实在是太快了，但是配置表有需要保持300这样的数值
            var configMoveSpeed = m_Owner.GetConfigMoveSpeed() * 10 * 2 / 3;
            var moveSpeed = m_Owner.GetMoveSpeed();

            if (moveSpeed * 100 >= configMoveSpeed * 125)
            {
                render.PlayAnimation("run");
            }
            else
            {
                render.PlayAnimation("walk");
            }

            render.SetAnimationSpeed(1.0f);
            render.SetMoveSpeed(moveSpeed);
        }

        protected override void Update(float dt)
        {
            if (m_Owner.CanMove())
            {
                if (m_GM_DisableMove && m_Owner.ReadGroup() == GroupId.PushGroupId)
                {
                    return;
                }

                m_Owner.SetForward(m_Forward);
                m_CurForward = m_Forward;
                var speed = m_Owner.GetMoveSpeed();

                var dis = speed * dt;
                Vector3 mv = m_CurForward * dis;
                m_Owner.SetPosition(m_Owner.GetPosition() + mv);

                var render = m_Owner.GetRender();
                if (render != null)
                {
                    render.SetMoveSpeed(speed);
                }
            }
            else
            {
                //Debug.Log("state_move stop");
                m_Owner.TryChangeState(emEntityState.em_EntityState_Idle);
                m_Owner.Stop();
                var render = m_Owner.GetRender();
                if (render != null)
                {
                    render.SetMoveSpeed(0);
                    render.SetAnimationSpeed(1.0f);
                }
            }
        }


        protected override void End(emEntityState nextState)
        {
            //if (nextState != emEntityState.em_EntityState_WaypointMove)
            //{
                m_Owner.GetRender().PlayAnimation("idle");
                m_Owner.Stop();

                var render = m_Owner.GetRender();
                if (render != null)
                {
                    render.SetMoveSpeed(0);
                    render.SetAnimationSpeed(1.0f);
                }
            //}



        }

    }

}