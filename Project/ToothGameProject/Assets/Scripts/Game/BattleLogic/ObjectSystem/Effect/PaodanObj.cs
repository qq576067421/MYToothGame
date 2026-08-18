using GameDll;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{

    // 场景上的脱离于施法者的特效、子弹等
    public class PaodanObj : PlayableEffectObj
    {
        public enum PaodanState
        {
            Init,
            Fly,
            Finish,
            AfterFinish,
        }
        protected PaodanState m_PaodanState = PaodanState.Init;
        protected int m_AttackerId;
        protected PropertyEntity m_Defender;
        protected int m_DefenderId = 0;
        public void SetDefender(PropertyEntity defender)
        {
            if(defender != null)
            {
                m_DefenderId = defender.ReadId();
                m_Defender = defender;
                m_LastDefenderPos = m_Defender.ReadHitPoint();
            }
            else
            {
                m_DefenderId = 0;
                m_Defender = null;
            }
        }


        protected float m_FlyTime = 0;



        private HurtInfo m_HurtInfo = new HurtInfo();
        protected GroupId m_BulletGroup;

        //protected GameObject m_BulletLogic;
        protected float m_FlyPredictTime = 0;
        protected Vector3 m_LastDefenderPos;
        public void SetDefenderPos(Vector3 pos)
        {
            m_LastDefenderPos = pos;
        }
        public override void SetPlay(bool play)
        {
            base.SetPlay(play);
            m_PaodanState = PaodanState.Init;
        }
        public HurtInfo GetHurtInfo()
        {
            return m_HurtInfo;
        }

        public override void CreateRender(UResource obj, ResourceType resourceType)
        {
            var res = UResourceFactory.New_EntityObject(resourceType, m_EntityType);
            res.SetId(ReadId());
            SetResource(res);
            res.LoadRender(m_BulletBean.t_effect_abname, Tool.GetAssetName(m_BulletBean.t_effect_abname));
            
        }

        private int m_HitSound = 0;
        public void SetHitSound(int hitSound)
        {
            m_HitSound = hitSound;
        }
        private t_bullet m_BulletBean = null;
        public override void SetBean(object bean)
        {
            m_BulletBean = (t_bullet)bean;
        }

        public t_bullet GetBulletBean()
        {
            return m_BulletBean;
        }




        public override void Update(float dt)
        {
            switch (m_PaodanState)
            {
                case PaodanState.Init:
                    {
                        m_PaodanState = PaodanState.Fly;
                        OnStartFly();
                        break;
                    }
                case PaodanState.Fly:
                    {
                        OnFlying(dt);
                        break;
                    }
                case PaodanState.Finish:
                    {

                        break;
                    }
                case PaodanState.AfterFinish:
                    {

                        break;
                    }
            }
        }



        private void OnFinish()
        {
            m_DefenderId = 0;
            m_PaodanState = PaodanState.AfterFinish;
            BattleManager.GetBattle().GetObjectManager().RemoveEffObject(this, false);
            PoolObj();
        }
        public virtual void SetAttacker(int attacker, GroupId group)
        {
            m_AttackerId = attacker;
            m_BulletGroup = group;
        }
        public override void SetPosition(Vector3 position)
        {
            GetRender().SetPosition(BulletFlightConstraintUtility.ResolveConstrainedPosition(m_BulletBean, position));
            
        }
        public override void SetForward(Vector3 forward)
        {
            forward = BulletFlightConstraintUtility.ResolveConstrainedForward(
                m_BulletBean,
                forward,
                GetRender().GetForward());
            if (forward == Vector3.zero)
            {
                return;
            }
            GetRender().SetForward(forward.normalized);
            
        }

        public override float GetMoveSpeed()
        {
            return m_BulletBean.t_move_speed / 1000.0f;
        }

        private void OnStartFly()
        {
            m_FlyTime = BattleManager.ReadBattleTime();


            //render设置
            var render = GetRender();
            render.SetAngularSpeed(7200);
            var moveSpeed = GetMoveSpeed();
            render.SetMoveSpeed(moveSpeed);
            render.BulletEmit();
            render.SetInitPosition(GetPosition());

            if(m_HurtInfo.m_NeedTarget == 0)
            {
                var targetPos = GetPosition() + GetForward() * m_BulletBean.t_max_time / 1000.0f * GetMoveSpeed();
                render.SetTargetPosition(targetPos);
                render.SetIsFollow(false);
            }
            else
            {
                render.SetTargetPosition(m_LastDefenderPos);
                render.SetIsFollow(true);
            }
        }

        protected virtual void OnFlying(float dt)
        {
            if (m_HurtInfo.m_NeedTarget == 0 )
            {
                //没有目标的子弹，按照子弹轨迹运动
                //检测是否有其他人中弹
                var actors = BattleManager.GetObjectManager().ReadPropertyEntities();
                foreach (var kv in actors)
                {
                    var defender = kv;
                    if (!BattleManager.ReadIsEntityValide(defender))
                    {
                        continue;
                    }
                    if (defender.ReadIsInBorn())
                    {
                        continue;
                    }
                    if (!defender.ReadCanBeTarget() || !defender.ReadCanBeHurt())
                    {
                        continue;
                    }
                    var find = BattleManager.BulletHitEnemy(GetPosition(),
                        m_BulletGroup, defender, 0.2f);
                    if (find)
                    {
                        m_PaodanState = PaodanState.Finish;
                        BattleManager.GetBattleTool().AddDefenderBuff(m_HurtInfo, defender);
                        m_HurtInfo.SetDamageNumberWorldPos(GetPosition());
                        DamageCal.Cal(m_HurtInfo, defender);
                        var attackedTarget = defender as MoveableCreature;
                        if (attackedTarget != null)
                        {
                            attackedTarget.OnAttacked();
                        }
                        DamageCal.CalSickHP(m_HurtInfo.m_Attacker, m_HurtInfo.m_SickHP);
                        m_HurtInfo.m_SickHP = 0;

                        GetRender().BulletBoom(defender.ReadHitPoint(), GetRender().GetForward());
                        PlayHitEffect(defender.ReadHitPoint());
                        PlayHitSound();
                        OnFinish();
                        return;
                    }
                }

                    

                var dir = GetForward();
                var pos = GetPosition() + dir * dt * GetMoveSpeed();

                if (BattleManager.ReadBattleTime() - m_FlyTime >= m_BulletBean.t_max_time / 1000.0f)
                {
                    m_PaodanState = PaodanState.Finish;
                    GetRender().BulletBoom(GetPosition(), Vector3.forward);
                    OnFinish();
                }
                else
                {
                    SetPosition(pos);
                }
                
            }
            else
            {
                if(m_Defender != null)
                {
                    if (!IsCombatTargetAvailable(m_Defender, m_DefenderId))
                    {
                        m_Defender = null;
                        m_DefenderId = 0;
                    }
                }

                if(m_Defender == null)
                {
                    var dist = Vector3.Distance(m_LastDefenderPos, GetPosition());
                    if(dist < 0.16f)
                    {
                        m_PaodanState = PaodanState.Finish;
                        GetRender().BulletBoom(GetPosition(), Vector3.forward);
                        OnFinish();
                    }
                    else
                    {
                        var dir = m_LastDefenderPos - GetPosition();
                        dir = dir.normalized;
                        SetForward(dir);
                        var step = dt * GetMoveSpeed();
                        var pos = GetPosition() + dir * step;
                        if (step > dist)
                        {
                            pos = m_LastDefenderPos;
                        }

                        if (BattleManager.ReadBattleTime() - m_FlyTime >= m_BulletBean.t_max_time / 1000.0f)
                        {
                            m_PaodanState = PaodanState.Finish;
                            GetRender().BulletBoom(GetPosition(), Vector3.forward);
                            OnFinish();
                        }
                        else
                        {
                            SetPosition(pos);
                            GetRender().SetTargetPosition(m_LastDefenderPos);
                        }
                        
                    }

                }
                else
                {
                    var dist = Vector3.Distance(m_Defender.ReadHitPoint(), GetPosition());
                    if(dist < 0.160f)
                    {
                        m_PaodanState = PaodanState.Finish;
                        BattleManager.GetBattleTool().AddDefenderBuff(m_HurtInfo, m_Defender);
                        m_HurtInfo.SetDamageNumberWorldPos(GetPosition());
                        DamageCal.Cal(m_HurtInfo, m_Defender);
                        var attackedTarget = m_Defender as MoveableCreature;
                        if (attackedTarget != null)
                        {
                            attackedTarget.OnAttacked();
                        }
                        DamageCal.CalSickHP(m_HurtInfo.m_Attacker, m_HurtInfo.m_SickHP);
                        m_HurtInfo.m_SickHP = 0;

                        GetRender().BulletBoom(m_Defender.ReadHitPoint(), GetRender().GetForward());
                        PlayHitEffect(m_Defender.ReadHitPoint());
                        PlayHitSound();
                        OnFinish();
                        return;
                    }
                    else
                    {
                        SetDefenderPos(m_Defender.ReadHitPoint());
                        var render = GetRender();
                        render.SetTargetPosition(m_LastDefenderPos);
                        var dir = m_Defender.ReadHitPoint() - GetPosition();
                        dir = dir.normalized;
                        SetForward(dir);

                        var step = dt * GetMoveSpeed();
                        var pos = GetPosition() + dir * step;
                        if (step > dist)
                        {
                            pos = m_LastDefenderPos;
                        }
                        

                        if(BattleManager.ReadBattleTime() - m_FlyTime >= m_BulletBean.t_max_time / 1000.0f)
                        {
                            m_PaodanState = PaodanState.Finish;
                            GetRender().BulletBoom(GetPosition(), Vector3.forward);
                            OnFinish();
                        }
                        else
                        {
                            SetPosition(pos);
                        }
                        

                    }
                }

            }

        }

        private bool IsCombatTargetAvailable(PropertyEntity target, int targetId)
        {
            return target != null &&
                   BattleManager.ReadIsEntityValide(target, targetId) &&
                   target.ReadCanBeTarget() &&
                   target.ReadCanBeHurt();
        }

        private void PlayHitSound()
        {
            if (m_BulletBean.t_hit_sound != 0)
            {
                AudioManager.GetInstance().Play3D(m_BulletBean.t_hit_sound, GetPosition());
            }
            else if (m_HitSound != 0)
            {
                AudioManager.GetInstance().Play3D(m_HitSound, GetPosition());
            }
        }

        private void PlayHitEffect(Vector3 pos)
        {
            var skillDescCfg = m_HurtInfo != null ? m_HurtInfo.skillDescCfg : null;
            if (skillDescCfg != null && skillDescCfg.t_hitEff != 0)
            {
                var eff = RenderEffManager.GetInstance().CreateRenderEff(skillDescCfg.t_hitEff);
                eff.ShowEff(false, pos, Vector3.zero, Vector3.one);
                eff.SetDuringTime(2.0f);
                RenderEffManager.GetInstance().SetAutoPool(eff);
            }
        }

        public override void PoolObj()
        {
            BattleManager.GetBattle().GetPaodanObjPool().PoolEffect(m_BulletBean.t_id, this);
            SetVisiable(false);
        }
    }
}
