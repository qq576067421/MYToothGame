using UnityEngine;

namespace GameDll
{
    class MoveBuff : SceneBuff
    {
        private Vector3 m_LoopEffectPosOffset = Vector3.zero;
        public override void Update(float dt)
        {
            base.Update(dt);
            if (m_Effect == null || m_TargetEnt == null)
            {
                return;
            }

            bool shouldShow = m_TargetEnt.GetStateManager().GetCurrentState().m_StateType == emEntityState.em_EntityState_Move;
            if (m_Effect.IsActive() != shouldShow)
            {
                m_Effect.SetActive(shouldShow);
            }

            var effectGo = m_Effect.GetRenderObj();
            if (effectGo != null && effectGo.transform.parent != null)
            {
                effectGo.transform.localPosition = m_LoopEffectPosOffset;
            }
            else
            {
                m_Effect.SetPosition(ReadBuffEffectPosition() + m_LoopEffectPosOffset);
            }
        }
    }

}

