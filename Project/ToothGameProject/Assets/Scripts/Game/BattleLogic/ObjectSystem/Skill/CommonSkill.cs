using GameDll;
using LCL;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameDll
{
    //抖动来源(0释放技能瞬间 1伤害结算点 2攻击到目标 3弹道落地)
    public enum ShakeSourceType
    {
        StartSkill,
        CastPoint,
        HurtEntity,
        FallGround
    }
    public class CommonSkill : Skill
    {
        protected float m_LastCastTime = 0;
        protected EffectObj m_WarmUpEffObj;
        protected List<EffectObj> m_KeepHurtEffObjs = null;

        public override void PreLoadEffect()
        {
            LoadEffect(false);
        }
        protected override object LoadEffect(bool show)
        {
            if (m_SkillDescBean != null && m_SkillDescBean.t_atk_eff != 0)
            {
                var objMgr = BattleManager.GetBattle().GetObjectManager();
                var pool = BattleManager.GetBattle().GetEffectObjPool();

                t_effectBean bean = t_effectBean.GetConfig(m_SkillDescBean.t_atk_eff);
                var effectObj = pool.GetEffect(emEntityType.em_EntityType_Effect, m_SkillDescBean.t_atk_eff, bean, ResourceType.Effect);
                effectObj.SetVisiable(show);
                if (!show)
                {
                    pool.PoolEffect(m_SkillDescBean.t_atk_eff, effectObj);
                }


                return effectObj;
            }
            else
            {
                return null;
            }
        }
        public override void OnEnter()
        {
            base.OnEnter();

            //if (m_IsDrag)
            //{
            //    m_MoveForward = m_SkillDir;
            //}
            //else
            //{
            //    m_MoveForward = Vector3.zero;
            //}
            OnWarmUp();
            m_CastStatus = SkillCastStatus.warming_up;
        }
        protected virtual void CastSkill()
        {
            m_LastCastTime = BattleManager.ReadBattleTime();
            OnCalHurt(false);
        }
        protected virtual void FirstCastSkill()
        {
            m_LastCastTime = BattleManager.ReadBattleTime();
            OnCalHurt(true);
            //OnSummon();

            //OnPlayOnceCameraShake(ShakeSourceType.CastPoint, m_SkillPos, null);
        }

        protected virtual float GetKeepTimeSecond()
        {
            if (m_SkillBean == null)
            {
                return 0;
            }

            return Mathf.Max(0, m_SkillBean.t_keep_time / 1000.0f);
        }

        protected virtual float GetHurtIntervalSecond()
        {
            if (m_SkillBean == null)
            {
                return 0;
            }

            return Mathf.Max(0, m_SkillBean.t_hurt_interval / 1000.0f);
        }

        public override void Destroy()
        {
            if (m_CastStatus == SkillCastStatus.warming_up ||
                m_CastStatus == SkillCastStatus.cast_point ||
                m_CastStatus == SkillCastStatus.cast_keep ||
                m_CastStatus == SkillCastStatus.cast_back)
            {
                OnCastOver();
            }
        }

        protected override void OnCastOver()
        {
            if (m_CastStatus == SkillCastStatus.cast_keep)
            {
                RenderEvent.Event.KeepSkill(this.GetCastId(), false);
            }
            base.OnCastOver();
            OnClearEffObj();
        }

        protected List<PropertyEntity> m_HitEnemies = new List<PropertyEntity>();
        protected HitDetectionData m_HitTestData = new HitDetectionData();
        protected HurtInfo m_HurtInfo = new HurtInfo();
        protected HurtInfo m_AOEHurtInfo = new HurtInfo();
        protected override void OnCalHurt(bool isFirst)
        {
            var cfg = GetSkillBean();
            m_HurtInfo.Reset();
            m_HurtInfo.m_Hurt = GetAtk();
            m_HurtInfo.m_BaseAttack = ReadLastDamageBaseAttack();
            m_HurtInfo.m_RawSkillDamageCfg = ReadLastRawSkillDamageCfg();
            m_HurtInfo.m_SkillDamagePercent = ReadLastDamageSkillPercent();
            m_HurtInfo.m_CritRate = ReadLastDamageCritRate();
            m_HurtInfo.m_CritDamageScale = ReadLastDamageCritDamageScale();
            m_HurtInfo.m_DamageAmpScale = ReadLastDamageAmpScale();
            m_HurtInfo.m_Slot = m_Slot;
            //m_HurtInfo.m_HurtType = (HurtType)GetHurtType();
            m_HurtInfo.m_AttackerId = m_AttackerId;
            m_HurtInfo.m_Attacker = m_Attacker;
            m_HurtInfo.m_Group = m_AttackerGroup;
            m_HurtInfo.m_HurtGroup = m_Attacker.ReadHurtGroup();
            
            
            m_HurtInfo.m_SickHP = 0;

            if (m_Attacker != null)
            {
                m_HurtInfo.m_HurtGroup = m_Attacker.ReadHurtGroup();
            }
            else
            {
                m_HurtInfo.m_HurtGroup = m_AttackerGroup;
            }
            float sickHP = 0;

            m_HurtInfo.skillCfg = cfg;
            var skillBean = GetSkillBean();
            var skillDescBean = GetSkillDescBean();
            bool isHit = false;

            if(BattleManager.ReadIsEntityValide(m_MainDefender))
            {
                m_HurtInfo.isMain = true;
                //判断属性的miss

                m_HitTestData.hurt_group = this.ReadAttackerGroup();
                if (m_Attacker != null)
                {
                    m_HitTestData.hurt_group = m_Attacker.ReadHurtGroup();
                }
                m_HitTestData.hitType = (HitDetectionShapeType)m_SkillBean.t_hurt_param_type;
                m_HitTestData.pos = m_Attacker.GetPosition();
                m_HitTestData.hurt_range = m_SkillBean.t_hurt_param0 / 1000.0f;
                m_HitTestData.dir = ReadSkillDir();
                if (m_HitTestData.dir.sqrMagnitude <= 0.0001f && m_Attacker != null)
                {
                    m_HitTestData.dir = m_Attacker.ReadForward();
                }
                m_HitTestData.angle = m_SkillBean.t_hurt_param1 / 1000.0f;
                //判断攻击范围和距离的miss
                isHit = BattleManager.IsHitEnemy(m_HitTestData, (PropertyEntity)m_MainDefender);
                
                if(!isHit)
                {
                    //var show_miss_render = m_HurtInfo.m_Attacker.GetRender();
                    //show_miss_render.ShowNumber(HpTextType.Miss, 0, 1);
                }
                else
                {
                    BattleManager.GetBattleTool().AddDefenderBuff(m_HurtInfo, m_MainDefender);
                    DamageCal.Cal(m_HurtInfo, m_MainDefender);
                    var attackedTarget = m_MainDefender as MoveableCreature;
                    if (attackedTarget != null)
                    {
                        attackedTarget.OnAttacked();
                    }
                    ShowHitEff(m_MainDefender);
                    OnActionSub(m_MainDefender);
                    sickHP += m_HurtInfo.m_SickHP;
                    m_HurtInfo.m_SickHP = 0;
                    DamageCal.CalSickHP(m_HurtInfo.m_Attacker, sickHP);
                }

            }
        }

        protected virtual void OnActionSub(Entity enemy)
        {

        }

        protected virtual void ShowHitEff(Entity defender)
        {
            PlayHitSound(m_SkillDescBean != null ? m_SkillDescBean.t_hitSound : 0, defender.GetPosition());
            var render = defender.GetRender();
            var targetPos = defender.ReadHitPoint();
            var attackFirePos = m_SkillPos;
            if(BattleManager.ReadIsEntityValide(m_Attacker))
            {
                attackFirePos = ReadCastFirePoint();
            }
            var hitPos = m_MainDefender.ReadHitPoint();
            PlayHitEff(m_SkillDescBean != null ? m_SkillDescBean.t_hitEff : 0, hitPos);
        }
        protected virtual void StartWarmUp()
        {
            ShowDefaultWarningEff();
        }
        protected virtual void OnWarmUp()
        {
            //Debug.Log("技能释放开始");
            if (m_Attacker != null)
            {
                SetCurAction();
                PlayAction(m_CurAction.t_ac_name);
                //PlayWeaponEff();
                PlayAttackSound(m_SkillDescBean != null ? m_SkillDescBean.t_attackSound : 0, m_Attacker.GetPosition());

                if (m_SkillDescBean != null && m_SkillDescBean.t_atk_eff != 0)
                {
                    string[] mountPaths = ParseMountPaths(m_SkillDescBean.t_atk_eff_pos);
                    bool hasMounted = false;

                    if (mountPaths.Length > 0 && m_Attacker != null)
                    {
                        foreach (var singlePath in mountPaths)
                        {
                            var mount = m_Attacker.ReadMountTransform(singlePath);
                            if (mount == null) continue;

                            var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_eff);
                            if (eff == null) continue;

                            if (m_SkillDescBean.t_atk_eff_parent == 0)
                            {
                                eff.SetParent(mount);
                                eff.ShowEff(true, Vector3.zero, Vector2.zero, Vector3.one);
                            }
                            else
                            {
                                eff.SetParent(GameDll.RenderEffManager.GetInstance().GetRenderEffParent());
                                var pos = mount.position;
                                var rot = mount.eulerAngles;
                                var scale = mount.localScale;
                                eff.ShowEff(false, pos, rot, scale);
                            }
                            eff.SetDuringTime(2.0f);
                            RenderEffManager.GetInstance().SetAutoPool(eff);
                            hasMounted = true;
                        }
                    }

                    if (!hasMounted)
                    {
                        var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_eff);
                        if (eff != null)
                        {
                            var pos = ReadCastFirePoint();
                            eff.ShowEff(false, pos, Vector3.zero, Vector3.one);
                            eff.SetDuringTime(2.0f);
                            RenderEffManager.GetInstance().SetAutoPool(eff);
                        }
                    }
                }

                //if (m_SkillDescBean.t_atk_eff_time_point == 0)
                //{
                //    m_WarmUpEffObj = PlayOnceSkillEffect();
                //    if (m_WarmUpEffObj != null)
                //    {
                //        m_WarmUpEffObj.AttachToEntity(m_Attacker);
                //    }
                //}

                //OnPlayOnceCameraShake(ShakeSourceType.StartSkill, m_SkillPos, null);

                StartWarmUp();

                m_CastStatus = SkillCastStatus.warming_up;
            }
            else
            {
                Debug.LogWarning("技能没有指定施法者");
            }
        }
        protected virtual EffectObj PlayOnceSkillEffect()
        {
            var eff = LoadEffect(true);
            if (eff != null)
            {
                var effObj = (EffectObj)eff;
                var objMgr = BattleManager.GetBattle().GetObjectManager();
                effObj.SetId(objMgr.AssignClientId());
                effObj.SetAutoDel(PlayableEffectDelType.Pool);
                effObj.SetPlay(true);
                int eff_time = Mathf.RoundToInt(BattleManager.ConvertFrame2Second(m_CurAction.t_ac_finish, GetAttackSpeed(), GetActionFrameRate()) * 1000.0f);
                var config_time = effObj.GetEffBean().t_time;
                if (eff_time < config_time)
                {
                    eff_time = config_time;
                }

                effObj.SetDuringTime(eff_time);
                effObj.SetVisiable(true);
                objMgr.AddEffObject(effObj);
                var hurtParamType = (HitDetectionShapeType)m_SkillBean.t_hurt_param_type;
                if (hurtParamType == HitDetectionShapeType.CasterRect ||
                    hurtParamType == HitDetectionShapeType.CasterAngle)
                {
                    m_SkillPos = ReadCastFirePoint();
                    effObj.SetPosition(m_SkillPos);
                }
                else if (hurtParamType == HitDetectionShapeType.CasterCircle)
                {
                    Vector3 pos = m_Attacker.GetPosition();
                    var attacker = ReadAttacker();
                    if (attacker != null)
                    {
                        pos = attacker.GetPosition();
                    }
                    m_SkillPos = pos;
                    effObj.SetPosition(pos);
                }
                else
                {
                    effObj.SetPosition(m_SkillPos);
                }

                effObj.SetForward(m_SkillDir);
                return effObj;
            }
            else
            {
                return null;
            }
        }
        protected virtual void OnCastPoint()
        {
            //Debug.Log("castskill hurt");
            ApplySkillCastBuffIfNeeded();
            FirstCastSkill();
            if (GetKeepTimeSecond() > 0)
            {
                m_CastStatus = SkillCastStatus.cast_keep;
                RenderEvent.Event.KeepSkill(this.GetCastId(), true);
                CalNextWarning();
            }
            else
            {
                m_CastStatus = SkillCastStatus.cast_back;
            }

            //if (m_SkillDescBean.t_atk_eff_time_point == -1)
            //{
            //    m_WarmUpEffObj = PlayOnceSkillEffect();
            //}
        }


        public override void Update(float dt)
        {
            base.Update(dt);
            float skillUsedTime = GetSkillUsedTime();

            switch (m_CastStatus)
            {
                case SkillCastStatus.warming_up:
                    {
                        float castPoint = BattleManager.ConvertFrame2Second(m_CurAction.t_ac_cast_point, GetAttackSpeed(), GetActionFrameRate());
                        if (skillUsedTime > castPoint)
                        {

                            m_CastStatus = SkillCastStatus.cast_point;

                        }
                        break;
                    }
                case SkillCastStatus.cast_point:
                    {
                        OnCastPoint();
                        break;
                    }
                case SkillCastStatus.cast_keep:
                    {
                        float keepTime = GetKeepTimeSecond();
                        if (skillUsedTime > keepTime)
                        {
                            m_CastStatus = SkillCastStatus.cast_back;
                            OnClearEffObj();
                            RenderEvent.Event.KeepSkill(this.GetCastId(), false);
                        }
                        else
                        {
                            float hurtInterval = GetHurtIntervalSecond();
                            float time = BattleManager.ReadBattleTime() - m_LastCastTime;
                            if (hurtInterval > 0 && time >= hurtInterval)
                            {
                                CastSkill();
                                if (keepTime - skillUsedTime >= hurtInterval)
                                {
                                    CalNextWarning();
                                }
                            }
                        }
                        OnCastKeep(dt);
                        break;
                    }
                case SkillCastStatus.cast_back:
                    {
                        var finishedTime = BattleManager.ConvertFrame2Second(m_CurAction.t_ac_finish, GetAttackSpeed(), GetActionFrameRate());
                        if (skillUsedTime >= finishedTime)
                        {
                            m_CastStatus = SkillCastStatus.cast_over;

                            //StopWeaponEff();
                            //Debug.Log("技能释放完毕" + skillUsedTime);
                        }
                        break;
                    }
                case SkillCastStatus.cast_over:
                    {


                        OnCastOver();
                        break;
                    }
                case SkillCastStatus.end:
                    {
                        break;
                    }
            }
        }

        protected virtual void OnCastKeep(float dt)
        {

        }

        protected virtual void OnClearEffObj()
        {
            if (m_WarmUpEffObj != null)
            {
                m_WarmUpEffObj.SetFinish();
                m_WarmUpEffObj = null;
            }
            if(m_KeepHurtEffObjs != null && m_KeepHurtEffObjs.Count > 0)
            {
                foreach(var obj in m_KeepHurtEffObjs)
                {
                    obj.SetFinish();
                }
                m_KeepHurtEffObjs = null;
            }
        }

        protected virtual void CalNextWarning()
        {

        }
    }
}
