using GameDll;
using MonoBean;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{

    public class State_Dead : StateBase
    {
        private float m_AniStartTime = 0;
        private float m_AniLength = 0;

        private int m_DeadState = 0;

        private float m_DieStartEffectTime = 1.0f;
        private bool m_DieEffectStarted = false;


        public override StateLevel GetStateLevel()
        {
            return StateLevel.Dead;
        }
        protected override void Start(emEntityState prevState)
        {
            var render = m_Owner.GetRender();
            render.PlayAnimation("die");
            m_AniLength = 2.0f;
            m_DieStartEffectTime = 1.0f;
            m_DieEffectStarted = false;
            if(m_Owner.ReadIsSmallMonster() || m_Owner.ReadIsBoss())
            {
                var cfg  = t_monsterBean.GetConfig(m_Owner.ReadBeanId());
                m_AniLength = cfg.t_die_time / 1000.0f;
                m_DieStartEffectTime = cfg.t_die_effect_start_time / 1000.0f;
            }
            m_AniStartTime = BattleManager.ReadBattleTime();
        }

        public override void OnChangedState()
        {
            m_Owner.OnDead();
            m_DeadState = 0;
        }

        protected override void Update(float dt)
        {
            if (m_DeadState == 0)
            {
                var time = BattleManager.ReadBattleTime();
                if (time - m_AniStartTime >= m_AniLength)
                {
                    m_DeadState = 1;

                    var render = m_Owner.GetRender();
                    render.SetShowHud(false);
                    render.DisableHudRender();
                    if (!m_Owner.TryHandleDeadAnimationFinished())
                    {
                        BattleManager.GetObjectManager().RemovePropertyEntity(m_Owner, true);
                    }
                    
                }

                if(m_DieEffectStarted == false)
                {
                    if(time - m_AniStartTime >= m_DieStartEffectTime)
                    {
                        m_DieEffectStarted  = true;
                        var eff = RenderEffManager.GetInstance().CreateRenderEff(3);
                        if (eff != null)
                        {
                            eff.ShowEff(false, this.m_Owner.GetPosition(), Vector3.zero, Vector3.one);
                            eff.SetDuringTime(5.0f);
                            RenderEffManager.GetInstance().SetAutoPool(eff);

                            var render = m_Owner.GetRender();
                            if(render.IsObjectLoaded())
                            {
                                var obj = render.GetShowObj() as GameObject;
                                var mesh = obj.GetComponentInChildren<SkinnedMeshRenderer>();
                                if(mesh != null)
                                {
                                    eff.AddLoadedCall(() => 
                                    {
                                        //有可能资源加载完毕的时候已经不是死亡状态了
                                        if(m_DieEffectStarted == false)
                                        {
                                            return;
                                        }
                                        if(this.m_Owner.ReadIsDestroy())
                                        {
                                            return;
                                        }

                                        var effObj = eff.GetRenderObj();
                                        if(effObj == null)
                                        {
                                            return;
                                        }
                                        //特殊处理，并且知道这个节点有主粒子
                                        var ps = effObj.GetComponentsInChildren<ParticleSystem>();
                                        foreach(var p in ps)
                                        {
                                            var shape = p.shape;
                                            shape.skinnedMeshRenderer = mesh;
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
            }

        }



        protected override void End(emEntityState nextState)
        {
            m_DieEffectStarted = false;
        }
    }

}
