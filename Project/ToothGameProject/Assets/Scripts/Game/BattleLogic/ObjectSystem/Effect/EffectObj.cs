using GameDll;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{

    // 不属于地表层的短期物体，例如闪电，魔法，射出的箭
    // 这类物体不受场景管理
    public class EffectObj : PlayableEffectObj 
    {


        public override void CreateRender(UResource obj, ResourceType resourceType)
        {
            var res = UResourceFactory.New_EntityObject(resourceType, m_EntityType);
            res.SetId(ReadId());
            SetResource(res);
            res.LoadRender(m_EffectBean.t_abname, Tool.GetAssetName(m_EffectBean.t_abname));
            
        }


        private t_effectBean m_EffectBean = null;

        public override void SetBean(object bean)
        {
            m_EffectBean = (t_effectBean)bean;
        }

        public t_effectBean GetEffBean()
        {
            return m_EffectBean;
        }

        public override void PoolObj()
        {
            BattleManager.GetBattle().GetEffectObjPool().PoolEffect(m_EffectBean.t_id, this);
            SetVisiable(false);
        }

        public virtual HurtInfo GetHurtInfo()
        {
            return null;
        }

        public void AttachToEntity(PropertyEntity entity)
        {
            var render = GetRender();
            render.AttachToRender(entity.GetRender());
        }
    }
}
