using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace GameDll
{
    class MoveBuff : SceneBuff
    {
        private Vector3 m_LoopEffectPosOffset = Vector3.zero;
        public override void Update(float dt)
        {
            base.Update(dt);
            if (mObjBuffLoop == null || m_TargetEnt == null)
            {
                return;
            }

            if (m_TargetEnt.GetStateManager().GetCurrentState().m_StateType == emEntityState.em_EntityState_Move)
            {
                mObjBuffLoop.SetActive(true);
            }
            else
            {
                mObjBuffLoop.SetActive(false);
            }

            var effectGo = mObjBuffLoop.GetShowObj() as GameObject;
            if (effectGo != null && effectGo.transform.parent != null)
            {
                effectGo.transform.localPosition = m_LoopEffectPosOffset;
            }
            else
            {
                mObjBuffLoop.SetPosition(ReadBuffEffectPosition() + m_LoopEffectPosOffset);
            }
        }
    }

}

