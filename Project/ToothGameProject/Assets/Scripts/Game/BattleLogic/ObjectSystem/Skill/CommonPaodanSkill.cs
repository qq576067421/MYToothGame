using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameDll
{

    public class CommonPaodanSkill : Skill
    {
        protected Vector3 m_StartPos;
        protected t_bullet m_BulletBean;

        public override void InitTemplate(int classId, long templateId)
        {
            base.InitTemplate(classId, templateId);
            m_BulletBean = t_bullet.GetConfig(m_SkillBean.t_bullet_id);


        }
        public override void PreLoadEffect()
        {
            LoadEffect(false);
        }

        protected override object LoadEffect(bool show)
        {
            if (m_BulletBean.t_effect_abname != null)
            {
                var objMgr = BattleManager.GetBattle().GetObjectManager();
                var pool = BattleManager.GetBattle().GetPaodanObjPool();
                var bullet = pool.GetEffect(emEntityType.em_EntityType_Paodan, 
                    m_BulletBean.t_id, m_BulletBean, 
                    ResourceType.Paodan);
                bullet.SetVisiable(show);
                if(!show)
                {
                    pool.PoolEffect(m_BulletBean.t_id, bullet);
                    //表示随时可以使用
                    bullet.SetHideTime(-10);
                }
                return bullet;
            }
            else
            {
                return null;
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            SetCurAction();
            PlayAction(m_CurAction.t_ac_name);
            //Debug.Log("开始播放子弹动画时间：" + Time.realtimeSinceStartup);
            m_CastStatus =  SkillCastStatus.warming_up;
        }


        /// <summary>
        /// 获取子弹prefab名字
        /// </summary>
        /// <returns></returns>
        public virtual t_bullet GetBulletBean()
        {
            return m_BulletBean;
        }


        protected virtual void ShootBullet()
        {


            //Debug.Log("ShootBullet");
            var attacker = ReadAttacker();
            if(attacker == null)
            {
                Debug.LogWarning("attacker not find");
                return;
            }

            m_StartPos = BulletFlightConstraintUtility.ResolveConstrainedPosition(m_BulletBean, ReadCastFirePoint());

            var bullet = (PaodanObj)LoadEffect(true);
            var objMgr = BattleManager.GetBattle().GetObjectManager();
            bullet.SetId(objMgr.AssignClientId());
            bullet.SetAutoDel(PlayableEffectDelType.Pool);
            bullet.SetPlay(true);
            bullet.SetVisiable(true);
            bullet.SetPosition(m_StartPos);
            var forward = BulletFlightConstraintUtility.ResolveConstrainedForward(m_BulletBean, ResolveLaunchForward(), attacker.ReadForward());

            bullet.SetForward(forward);
            bullet.SetAttacker(m_AttackerId, m_AttackerGroup);




            objMgr.AddEffObject(bullet);

            var descCfg = GetSkillDescBean();
            PlayAttackSound(descCfg != null ? descCfg.t_attackSound : 0, m_Attacker.GetPosition());
            bullet.SetHitSound(descCfg != null ? descCfg.t_hitSound : 0);
            var hurtInfo = bullet.GetHurtInfo();
            hurtInfo.Reset();
            hurtInfo.m_Hurt = GetAtk();
            hurtInfo.m_BaseAttack = ReadLastDamageBaseAttack();
            hurtInfo.m_RawSkillDamageCfg = ReadLastRawSkillDamageCfg();
            hurtInfo.m_SkillDamagePercent = ReadLastDamageSkillPercent();
            hurtInfo.m_CritRate = ReadLastDamageCritRate();
            hurtInfo.m_CritDamageScale = ReadLastDamageCritDamageScale();
            hurtInfo.m_DamageAmpScale = ReadLastDamageAmpScale();
            hurtInfo.m_Slot = m_Slot;
            hurtInfo.m_NeedTarget = 0;
            hurtInfo.m_Group = m_AttackerGroup;
            hurtInfo.m_AttackerId = m_AttackerId;
            hurtInfo.m_Attacker = attacker;
            
            
            if (attacker != null)
            {
                hurtInfo.m_HurtGroup = attacker.ReadHurtGroup();
            }
            else
            {
                hurtInfo.m_HurtGroup = m_AttackerGroup;
            }
            hurtInfo.skillCfg = GetSkillBean();
            hurtInfo.skillDescCfg = descCfg;
            bullet.SetDefender(null);
        }



        public override void Update(float dt)
        {
            base.Update(dt);
            switch(m_CastStatus)
            {
                case SkillCastStatus.warming_up:
                    {
                        if (GetSkillUsedTime() > BattleManager.ConvertFrame2Second(m_CurAction.t_ac_cast_point, GetAttackSpeed(), GetActionFrameRate()))
                        {
                            m_CastStatus = SkillCastStatus.cast_point;

                        }
                        //Debug.Log("子弹预热的时间：" + Time.realtimeSinceStartup);
                        break;
                    }
                case SkillCastStatus.cast_point:
                    {
                        //Debug.Log("子弹发射的时间：" + Time.realtimeSinceStartup);
                        ApplySkillCastBuffIfNeeded();
                        ShootBullet();
                        m_CastStatus = SkillCastStatus.cast_back;
                        break;
                    }
                case SkillCastStatus.cast_back:
                    {
                        var usedTime = GetSkillUsedTime();
                        var frameTime = BattleManager.ConvertFrame2Second(m_CurAction.t_ac_finish, GetAttackSpeed(), GetActionFrameRate());
                        if (usedTime >= frameTime)
                        {
                            m_CastStatus = SkillCastStatus.cast_over;
                        }
                        break;
                    }

                case SkillCastStatus.cast_over:
                    {
                        OnCastOver();
                        break;
                    }
            }
        }

        public override void OnSkillUnregister()
        {

        }
    }
}
