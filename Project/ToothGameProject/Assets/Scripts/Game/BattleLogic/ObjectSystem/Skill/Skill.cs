using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameDll
{
    public enum SkillCastStatus
    {
        none,
        warming_up,
        cast_point,
        cast_keep,
        cast_back,
        cast_over,
        end,
    }
        public class HurtInfo
        {
            public float m_Hurt;
            public float m_BaseAttack;
            public int m_RawSkillDamageCfg;
            public float m_SkillDamagePercent;
            public float m_CritRate;
            public float m_CritDamageScale;
            public float m_DamageAmpScale;
            public int m_Slot = -1;
            public int m_NeedTarget;
            public GroupId m_Group;
        public GroupId m_HurtGroup;
        public PropertyEntity m_Attacker;
        public int m_AttackerId;
        public bool isMain;
        public t_skillBean skillCfg;
        public t_skillDescBean skillDescCfg;
        public bool m_IsCrit;

        public float m_SickHP = 0;
        public float m_AttackerFall;
        public int m_FallDashDistance;
        public Vector3 m_DamageNumberWorldPos;
        public bool m_HasDamageNumberWorldPos;

        public void Reset()
        {
            m_Hurt = 0;
            m_BaseAttack = 0;
            m_RawSkillDamageCfg = 0;
            m_SkillDamagePercent = 0;
            m_CritRate = 0;
            m_CritDamageScale = 0;
            m_DamageAmpScale = 0;
            m_Slot = -1;
            m_NeedTarget = 0;
            m_Group = 0;
            m_HurtGroup = 0;
            m_Attacker = null;
            m_AttackerId = 0;
            isMain = false;
            skillCfg = null;
            skillDescCfg = null;
            m_SickHP = 0;
            m_IsCrit = false;
            m_AttackerFall = 0;
            m_FallDashDistance = 0;
            m_DamageNumberWorldPos = Vector3.zero;
            m_HasDamageNumberWorldPos = false;
        }

        public void CopyFrom(HurtInfo that)
        {
            this.m_Hurt = that.m_Hurt;
            this.m_BaseAttack = that.m_BaseAttack;
            this.m_RawSkillDamageCfg = that.m_RawSkillDamageCfg;
            this.m_SkillDamagePercent = that.m_SkillDamagePercent;
            this.m_CritRate = that.m_CritRate;
            this.m_CritDamageScale = that.m_CritDamageScale;
            this.m_DamageAmpScale = that.m_DamageAmpScale;
            this.m_Slot = that.m_Slot;
            this.m_NeedTarget = that.m_NeedTarget;
            this.m_Group = that.m_Group;
            this.m_HurtGroup = that.m_HurtGroup;
            this.m_Attacker = that.m_Attacker;
            this.m_AttackerId = that.m_AttackerId;
            this.isMain = that.isMain;
            this.skillCfg = that.skillCfg;
            this.skillDescCfg = that.skillDescCfg;

            m_SickHP = that.m_SickHP;
            m_IsCrit = that.m_IsCrit;
            m_AttackerFall = that.m_AttackerFall;
            m_FallDashDistance = that.m_FallDashDistance;
            m_DamageNumberWorldPos = that.m_DamageNumberWorldPos;
            m_HasDamageNumberWorldPos = that.m_HasDamageNumberWorldPos;
        }

        public void SetDamageNumberWorldPos(Vector3 worldPos)
        {
            m_DamageNumberWorldPos = worldPos;
            m_HasDamageNumberWorldPos = true;
        }

        public void ClearDamageNumberWorldPos()
        {
            m_DamageNumberWorldPos = Vector3.zero;
            m_HasDamageNumberWorldPos = false;
        }


        public Skill GetSkill()
        {
            Skill skill = null;
            int skill_level = 0;
            if (this.m_Slot != -1)
            {
                var attacker = this.m_Attacker;
                if (attacker == null)
                {
                    if (this.m_AttackerId != 0)
                    {
                        attacker = BattleManager.GetObjectManager().ReadPropertyEntityById(this.m_AttackerId);
                    }
                }
                if (attacker != null)
                {
                    skill = attacker.GetSkillManager().ReadSkillBySlot(this.m_Slot);
                    if (skill != null)
                    {
                        skill_level = skill.ReadLevel();
                    }
                }
            }
            else
            {

            }
            return skill;
        }
    }

    public enum HitDetectionShapeType
    {
        SkillPosCircle,
        CasterRect,
        CasterCircle,
        CasterAngle

    }
    public class HitDetectionData
    {
        public GroupId hurt_group;
        public HitDetectionShapeType hitType;//0落点圆周围 1矩形(长+宽) 2施法者周围
        public float hurt_range;
        public float angle; //角度
        public float chang;
        public float kuan;
        public Vector3 pos;
        public Vector3 dir;
        public int care_type;



    }
    public class ActionData
    {
        public string t_ac_name;
        public int t_ac_cast_point;
        public int t_ac_finish;
        public int t_frame_rate;
        public void CopyFromBean(t_actionBean bean)
        {
            this.t_ac_name = bean.t_ac_name;
            this.t_ac_cast_point = bean.t_ac_cast_point;
            this.t_ac_finish = bean.t_ac_finish;
            this.t_frame_rate = bean.t_frame_rate;
        }
    }

    public class SkillActionStage
    {
        public List<ActionData> m_ActionComb = null;
        public ActionData m_ActionSingle = null;
    }

    public class Skill
    {
        private const string m_DefaultActionName = "attack";
        private const int m_DefaultActionCastPoint = 20;
        private const int m_DefaultActionFinish = 40;
        private const int m_DefaultActionFrameRate = 30;
        protected PropertyData m_PropertyData = new PropertyData();
        public PropertyData GetPropertyData()
        {
            return m_PropertyData;
        }

        protected t_skillBean m_SkillBean;
        protected t_skillDescBean m_SkillDescBean;

        //多套招式或者连招
        protected List<SkillActionStage> m_ActionStages = null;
        protected int m_ActionStageIndex = 0;
        protected int m_ActionCombIndex = 0;

        //单独一个招式
        protected ActionData m_CurAction = null;
        //带有连招
        protected SkillActionStage m_CurActions = null;


        protected bool m_IsInComb = false;

        protected virtual bool IsInComb()
        {
            return m_IsInComb;
        }
        protected virtual void SetCurAction()
        {
            if (m_ActionStages == null)
            {
                if (m_CurActions == null)
                {
                    m_IsInComb = false;
                    return;
                }
                else
                {
                    m_CurAction = m_CurActions.m_ActionComb[m_ActionCombIndex++];
                    if (m_ActionCombIndex >= m_CurActions.m_ActionComb.Count)
                    {
                        m_ActionCombIndex = 0;
                        m_IsInComb = false;
                    }
                    else
                    {
                        m_IsInComb = true;
                    }
                }
            }
            else
            {
                if (m_CurActions == null)
                {
                    m_CurActions = m_ActionStages[m_ActionStageIndex++];
                    if (m_ActionStageIndex >= m_ActionStages.Count)
                    {
                        m_ActionStageIndex = 0;
                    }

                    if (m_CurActions.m_ActionComb == null)
                    {
                        //当前套没有连招
                        m_CurAction = m_CurActions.m_ActionSingle;
                        m_CurActions = null;
                        m_IsInComb = false;
                    }
                    else
                    {
                        m_CurAction = m_CurActions.m_ActionComb[m_ActionCombIndex++];
                        if (m_ActionCombIndex >= m_CurActions.m_ActionComb.Count)
                        {
                            m_ActionCombIndex = 0;
                            m_CurActions = null;
                            m_IsInComb = false;
                        }
                        else
                        {
                            m_IsInComb = true;
                        }
                    }
                }
                else
                {
                    //连招的下一招式
                    m_CurAction = m_CurActions.m_ActionComb[m_ActionCombIndex++];
                    if (m_ActionCombIndex >= m_CurActions.m_ActionComb.Count)
                    {
                        m_ActionCombIndex = 0;
                        m_CurActions = null;
                        m_IsInComb = false;
                    }
                    else
                    {
                        m_IsInComb = true;
                    }
                }
            }
        }

        protected virtual void OnCastOver()
        {
            m_CastStatus = SkillCastStatus.end;
            RestoreBoneNormalAttackAnimation();
        }

        private int m_SkillLevel = 0;
        protected void _SetSkillLevel(int level)
        {
            m_SkillLevel = level;
        }
        protected int _GetSkillLevel()
        {
            return m_SkillLevel;
        }
        protected int m_ClassId;              //技能对应类名ID
        protected PropertyEntity m_Attacker;

        protected int m_AttackerId;
        protected GroupId m_AttackerGroup;
        protected int m_AnimationFrameRate = 30;
        private int m_AttackSpeed = 100;
        protected bool m_IsDirCast = false;
        private float m_LastCheckAtkChangeTime = 0;
        protected int m_CastId = 0;//该技能被实体释放的次数标识
        private bool m_SkillCastBuffApplied = false;
        private bool m_NextCastIsBoneNormalAttack;
        private bool m_IsBoneNormalAttack;
        private float m_BoneNormalAttackInputInterval;
        private float m_BoneActionFrameProgress;
        private float m_BoneActionProgressTime;
        private float m_BoneAnimationSpeedTime;
        private float m_BoneActionSpeed = 1f;
        private float m_BoneTargetActionSpeed = 1f;

        public bool ReadIsBoneNormalAttack()
        {
            return m_IsBoneNormalAttack;
        }

        public void PrepareBoneNormalAttack(float inputInterval)
        {
            m_NextCastIsBoneNormalAttack = m_Slot == 0 && this is CommonBulletSkill;
            m_BoneNormalAttackInputInterval = inputInterval;
        }

        protected float ReadBoneActionFrameProgress()
        {
            return m_BoneActionFrameProgress;
        }

        protected float ReadBoneActionSpeed()
        {
            return m_BoneActionSpeed;
        }

        protected virtual float ReadBoneNormalAttackTargetSpeed()
        {
            float finishFrame = Mathf.Max(m_CurAction.t_ac_cast_point + 1f, m_CurAction.t_ac_finish);
            float inputInterval = Mathf.Max(TowerDefendBoneAttackTuning.ReadMinAttackInterval(),
                m_BoneNormalAttackInputInterval);
            // 只在实际输入间隔短于完整动作时加速，不能把最快允许频率当作每次攻击的速度。
            return Mathf.Max(1f, finishFrame / (GetActionFrameRate() * inputInterval));
        }

        private void StartBoneNormalAttackAnimation()
        {
            float now = BattleManager.ReadBattleTime();
            float recoveryTime = Mathf.Max(0f, now - m_BoneAnimationSpeedTime);
            m_BoneActionSpeed = Mathf.Lerp(1f, m_BoneActionSpeed,
                Mathf.Exp(-recoveryTime / TowerDefendBoneAttackTuning.m_AnimationRecoverySecond));

            float frameRate = GetActionFrameRate();
            m_BoneTargetActionSpeed = ReadBoneNormalAttackTargetSpeed();
            m_BoneAnimationSpeedTime = now;
            m_Attacker.GetRender().SetAnimationSpeed(m_BoneActionSpeed * frameRate / 30f);
        }

        private void UpdateBoneNormalAttackProgress()
        {
            if (!m_IsBoneNormalAttack || m_CastStatus == SkillCastStatus.end ||
                m_Attacker == null || m_Attacker.ReadCurrentSkill() != this)
            {
                return;
            }
            if (!BattleManager.ReadIsEntityValide(m_Attacker) || m_Attacker.ReadIsBeingControlled() ||
                m_Attacker.ReadIsSuspended() || m_Attacker.ReadIsFrozen())
            {
                Stop();
                return;
            }

            float now = BattleManager.ReadBattleTime();
            float elapsed = Mathf.Max(0f, now - m_BoneActionProgressTime);
            if (elapsed <= 0f)
            {
                return;
            }

            // 技能管理器可能重复更新同一个技能，只累计尚未处理的战斗时间。
            m_BoneActionFrameProgress += elapsed * m_BoneActionSpeed * GetActionFrameRate();
            m_BoneActionProgressTime = now;
            float targetSpeed = m_Attacker.ReadBoneNormalAttackInterval() >
                TowerDefendBoneAttackTuning.m_AnimationRecoveryDelaySecond
                ? 1f : m_BoneTargetActionSpeed;
            float responseTime = targetSpeed > m_BoneActionSpeed
                ? TowerDefendBoneAttackTuning.m_AnimationAccelerationSecond
                : TowerDefendBoneAttackTuning.m_AnimationRecoverySecond;
            m_BoneActionSpeed = Mathf.Lerp(m_BoneActionSpeed, targetSpeed,
                1f - Mathf.Exp(-elapsed / responseTime));
            m_BoneAnimationSpeedTime = now;
            m_Attacker.GetRender().SetAnimationSpeed(m_BoneActionSpeed * GetActionFrameRate() / 30f);
        }

        private void RestoreBoneNormalAttackAnimation()
        {
            if (m_IsBoneNormalAttack && m_Attacker != null && m_Attacker.ReadCurrentSkill() == this)
            {
                var render = m_Attacker.GetRender();
                if (render != null)
                {
                    // 冰冻/挂起期间保持动画定格，不能把速度恢复成 1 把 Freeze/挂起的定格顶掉。
                    bool keepStill = m_Attacker.ReadIsSuspended() || m_Attacker.ReadIsFrozen();
                    render.SetAnimationSpeed(keepStill ? 0f : 1f);
                }
            }
        }

        private float m_Atk = 0;
        private float m_LastDamageBaseAttack = 0;
        private int m_LastRawSkillDamageCfg = 0;
        private float m_LastDamageSkillPercent = 0;
        private float m_LastDamageCritRate = 0;
        private float m_LastDamageCritDamageScale = 0;
        private float m_LastDamageAmpScale = 0;
        public virtual void SetAtk(PropertyEntity attacker)
        {
            SetAtk(CalculateDamageValue(attacker));
            m_LastCheckAtkChangeTime = BattleManager.ReadBattleTime();
        }
        public float GetLastCheckAtkChangeTime()
        {
            return m_LastCheckAtkChangeTime;
        }


        //该接口界面不能用
        public virtual float GetAtk()
        {
            return m_Atk;
        }
        public virtual void SetAtk(float atk)
        {
            m_Atk = atk;
        }
        public float ReadLastDamageBaseAttack()
        {
            return m_LastDamageBaseAttack;
        }
        public int ReadLastRawSkillDamageCfg()
        {
            return m_LastRawSkillDamageCfg;
        }
        public float ReadLastDamageSkillPercent()
        {
            return m_LastDamageSkillPercent;
        }
        public float ReadLastDamageCritRate()
        {
            return m_LastDamageCritRate;
        }
        public float ReadLastDamageCritDamageScale()
        {
            return m_LastDamageCritDamageScale;
        }
        public float ReadLastDamageAmpScale()
        {
            return m_LastDamageAmpScale;
        }

        protected int ResolveSkillDamageCfgPermille()
        {
            if (m_SkillBean == null)
            {
                return 1000;
            }

            var bulletCfg = t_bullet.GetConfig(m_SkillBean.t_bullet_id, false);
            if (bulletCfg == null)
            {
                return 1000;
            }

            return bulletCfg.t_skill_damage;
        }

        protected virtual float CalculateDamageValue(PropertyEntity attacker)
        {
            m_LastDamageBaseAttack = 0;
            m_LastRawSkillDamageCfg = 0;
            m_LastDamageSkillPercent = 0;
            m_LastDamageCritRate = 0;
            m_LastDamageCritDamageScale = 0;
            m_LastDamageAmpScale = 0;
            if (attacker == null)
            {
                return 0;
            }

            float baseDamage = attacker.GetAtk();
            if (baseDamage <= 0)
            {
                baseDamage = 0;
            }
            m_LastDamageBaseAttack = baseDamage;
            m_LastRawSkillDamageCfg = ResolveSkillDamageCfgPermille();
            m_LastDamageSkillPercent = Mathf.Max(0, m_LastRawSkillDamageCfg) / 1000.0f;

            var critRate = attacker.ReadCritRatePermille();
            var critDamageScale = attacker.ReadCritDamageScalePermille();
            var damageAmpScale = attacker.ReadDamageAmpPercent();
            m_LastDamageCritRate = critRate;
            m_LastDamageCritDamageScale = critDamageScale;
            m_LastDamageAmpScale = damageAmpScale;

            // 基础伤害按“攻击力 × 技能伤害倍率 × 伤害加深”计算，
            // 暴击伤害继续沿用 DamageCal 中现有的额外乘算时机。
            float damage = baseDamage * m_LastDamageSkillPercent;
            damage *= damageAmpScale;
            return Mathf.Max(0, damage);
        }
        protected bool m_NeedShowWarningEff;
        public void SetEnableWarning(bool warning)
        {
            m_NeedShowWarningEff = warning;
        }
        protected virtual void ShowStepWarningEff(Vector3 pos, Vector3 dir)
        {
            if (m_SkillDescBean == null)
            {
                return;
            }

            if (m_SkillDescBean.t_warning == 0)
            {
                return;
            }

            var hitType = (HitDetectionShapeType)m_SkillBean.t_hurt_param_type;
            var skillBean = m_SkillBean;
            //0落点圆周围 1矩形(长+宽) 2施法者周围 3施法者周围角度
            float range = 0;
            float chang = 0;
            range = skillBean.t_hurt_param0 / 1000.0f;
            chang = skillBean.t_hurt_param0 / 1000.0f;
            float kuan = 0;
            float angle = 0;
            kuan = skillBean.t_hurt_param1 / 1000.0f;
            angle = skillBean.t_hurt_param1 / 1000.0f;




            var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_warning);
            if (hitType == HitDetectionShapeType.SkillPosCircle)
            {
                range = m_SkillBean.t_hurt_param0 / 1000.0f;

                eff.ShowEff(false, pos, Vector3.zero, Vector3.one);
            }
            else if (hitType == HitDetectionShapeType.CasterRect)
            {
                chang = skillBean.t_hurt_param0 / 1000.0f;
                kuan = skillBean.t_hurt_param1 / 1000.0f;

                eff.ShowEffDir(false, pos, dir, Vector3.one);
            }
            else if (hitType == HitDetectionShapeType.CasterAngle)
            {
                range = skillBean.t_hurt_param0 / 1000.0f;
                angle = skillBean.t_hurt_param1 / 1000.0f;

                eff.ShowEffDir(false, pos, dir, Vector3.one);
            }
            else if (hitType == HitDetectionShapeType.CasterCircle)
            {
                range = skillBean.t_hurt_param0 / 1000.0f;

                eff.ShowEffDir(false, pos, dir, Vector3.one);
            }

            RenderEffManager.GetInstance().SetAutoPool(eff);
        }
        protected virtual void ShowDefaultWarningEff()
        {
            if (m_SkillDescBean == null)
            {
                return;
            }

            if (m_SkillDescBean.t_warning == 0)
            {
                return;
            }

            var hitType = (HitDetectionShapeType)m_SkillBean.t_hurt_param_type;
            var skillBean = m_SkillBean;
            //0落点圆周围 1矩形(长+宽) 2施法者周围 3施法者周围角度
            Vector3 pos = ReadSkillPos();

            float range = 0;
            float chang = 0;
            range = skillBean.t_hurt_param0 / 1000.0f;
            chang = skillBean.t_hurt_param0 / 1000.0f;
            float kuan = 0;
            float angle = 0;
            kuan = skillBean.t_hurt_param1 / 1000.0f;
            angle = skillBean.t_hurt_param1 / 1000.0f;

            var dir = this.ReadSkillDir();

            var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_warning);
            if (hitType == HitDetectionShapeType.SkillPosCircle)
            {
                pos = ReadSkillPos();
                range = m_SkillBean.t_hurt_param0 / 1000.0f;

                eff.ShowEff(false, pos, Vector3.zero, Vector3.one * range);
            }
            else if (hitType == HitDetectionShapeType.CasterRect)
            {
                pos = ReadCastFirePoint();
                chang = skillBean.t_hurt_param0 / 1000.0f;
                kuan = skillBean.t_hurt_param1 / 1000.0f;

                eff.ShowEffDir(false, pos, dir, new Vector3(kuan, 1.0f, chang));
            }
            else if (hitType == HitDetectionShapeType.CasterAngle)
            {
                pos = ReadCastFirePoint();
                range = skillBean.t_hurt_param0 / 1000.0f;
                angle = skillBean.t_hurt_param1 / 1000.0f;

                eff.ShowEffDir(false, pos, dir, Vector3.one * range);
            }
            else if (hitType == HitDetectionShapeType.CasterCircle)
            {
                pos = m_Attacker.GetPosition();
                range = skillBean.t_hurt_param0 / 1000.0f;

                eff.ShowEffDir(false, pos, dir, Vector3.one * range);
            }

            RenderEffManager.GetInstance().SetAutoPool(eff);

        }

        public PropertyEntity ReadAttacker()
        {
            return m_Attacker;
        }
        //30帧每秒的攻击速度，值越大速度也大，对应的间隔越小
        protected float GetAttackSpeed()
        {
            if (m_Slot == 0)
            {
                return m_Attacker.GetNormalAtkSpeed();
            }
            // m_AttackSpeed 以 100 为基准（100=1 倍速），与普攻的攻速属性同一量纲后参与
            // 动画速度/施法时间计算，保证普攻与技能的动作节奏走同一套公式。
            return Mathf.Max(0.01f, m_AttackSpeed / 100f);
        }

        //施法动作持续时间
        private float m_CastStartTime = 0;
        //进入冷却的开始时间戳(秒)
        protected float m_CooldownSinceTime = 0;
        protected PropertyEntity m_MainDefender = null;
        protected SkillCastStatus m_CastStatus = SkillCastStatus.none;



        protected Vector3 m_SkillDir;
        public void SetSkillDir(Vector3 dir)
        {
            m_SkillDir = dir;
        }
        public Vector3 ReadSkillDir()
        {
            return m_SkillDir;
        }
        protected Vector3 m_MoveDir = Vector3.zero;
        public void SetMoveDir(Vector3 move_dir)
        {
            m_MoveDir = move_dir;
        }
        public Vector3 ReadMoveDir()
        {
            return m_MoveDir;
        }

        public void SetIsDirCast(bool dirCast)
        {
            m_IsDirCast = dirCast;
        }
        public bool ReadIsDirCast()
        {
            return m_IsDirCast;
        }

        public virtual int ReadCastStyle()
        {
            if (m_SkillBean == null)
            {
                return 0;
            }
            return m_SkillBean.t_skill_cast_style;
        }

        // 技能施放方式：0手动施放 1自动施放 2被动施放。
        // 被动施放不打断角色当前行为，走独立于 SkillManager.m_CurrentSkill 的旁路通道：
        // 不占施法通道、不切角色状态、不改朝向、不播自身动作，只结算音效/特效/发弹/伤害。
        public const int PassiveCastStyle = 2;

        public bool ReadIsPassiveCastStyle()
        {
            return ReadCastStyle() == PassiveCastStyle;
        }

        // 技能本身的特效（t_atk_eff）：按 t_atk_eff_pos/t_atk_eff_parent 挂点跟随施法者，
        // 与施法动作同时出现。施法生效点的特效走 t_atk_cast_eff（CommonBulletSkill 的发弹点分支），
        // 两者是独立字段、独立播放入口。
        // holdUntilStop=true 用于“特效要和持续施法窗口同寿”的技能（如水柱/激光）：不进自动回收队列，
        // 由 StopSkillEffect 在施法结束时统一停发射，避免按特效表 t_time 提前或滞后消失。
        protected void PlaySkillEffect(bool holdUntilStop = false)
        {
            if (m_SkillDescBean == null || m_SkillDescBean.t_atk_eff == 0)
            {
                return;
            }

            float effectDuration = ResolveSkillEffectDurationSecond();
            string[] mountPaths = ParseMountPaths(m_SkillDescBean.t_atk_eff_pos);
            bool hasMounted = false;

            if (mountPaths.Length > 0 && m_Attacker != null)
            {
                foreach (var singlePath in mountPaths)
                {
                    var mount = m_Attacker.ReadMountTransform(singlePath);
                    if (mount == null)
                    {
                        continue;
                    }

                    var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_eff);
                    if (eff == null)
                    {
                        continue;
                    }

                    if (m_SkillDescBean.t_atk_eff_parent == 0)
                    {
                        eff.SetParent(mount);
                        eff.ShowEff(true, Vector3.zero, Vector2.zero, Vector3.one);
                    }
                    else
                    {
                        eff.SetParent(RenderEffManager.GetInstance().GetRenderEffParent());
                        eff.ShowEff(false, mount.position, mount.eulerAngles, mount.localScale);
                    }

                    ApplySkillEffectLifetime(eff, effectDuration, holdUntilStop);
                    hasMounted = true;
                }
            }

            if (!hasMounted)
            {
                // 没配挂点时落在施法开火点，朝向取施法者发射方向，不依赖角色自身朝向
                var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_eff);
                if (eff == null)
                {
                    return;
                }

                eff.ShowEffDir(false, ReadCastFirePoint(), ResolveLaunchForward(), Vector3.one);
                ApplySkillEffectLifetime(eff, effectDuration, holdUntilStop);
            }
        }

        // 普通技能特效按特效表时长自动回收；holdUntilStop 的特效改为手动持有，等施法结束再停。
        private void ApplySkillEffectLifetime(RenderEff eff, float effectDuration, bool holdUntilStop)
        {
            if (holdUntilStop)
            {
                // 顺手清掉已回池的旧实例，避免频繁施法后持有列表无限增长
                for (int i = m_SkillEffs.Count - 1; i >= 0; --i)
                {
                    if (m_SkillEffs[i] == null || m_SkillEffs[i].m_IsPooled)
                    {
                        m_SkillEffs.RemoveAt(i);
                    }
                }

                m_SkillEffs.Add(eff);
                return;
            }

            eff.SetDuringTime(effectDuration);
            RenderEffManager.GetInstance().SetAutoPool(eff);
        }

        // 停掉 PlaySkillEffect(holdUntilStop: true) 持有的特效：先停粒子发射，让已喷出的粒子自然消失再回池。
        protected void StopSkillEffect()
        {
            for (int i = m_SkillEffs.Count - 1; i >= 0; --i)
            {
                var eff = m_SkillEffs[i];
                if (eff == null || eff.m_IsPooled)
                {
                    continue;
                }

                RenderEffManager.GetInstance().StopEmissionAndAutoPool(eff);
            }

            m_SkillEffs.Clear();
        }

        // 技能特效的可见时长以特效表配置为准，未配时按施法动作长度兜底
        protected virtual float ResolveSkillEffectDurationSecond()
        {
            var effBean = MonoBean.t_effectBean.GetConfig(m_SkillDescBean.t_atk_eff, false);
            if (effBean != null && effBean.t_time > 0)
            {
                return effBean.t_time / 1000.0f;
            }

            if (m_CurAction != null)
            {
                return BattleManager.ConvertFrame2Second(m_CurAction.t_ac_finish, GetAttackSpeed(), GetActionFrameRate());
            }

            return m_DefaultSkillEffectDurationSecond;
        }

        private const float m_DefaultSkillEffectDurationSecond = 2.0f;

        // 由 PlaySkillEffect(holdUntilStop: true) 持有、需要跟施法窗口同寿的技能特效。
        protected readonly List<RenderEff> m_SkillEffs = new List<RenderEff>();

        protected Vector3 m_SkillPos;
        private Vector3 m_CastFirePoint;
        private bool m_HasCastFirePoint = false;
        private Vector3 m_ResolvedLaunchForwardOverride = Vector3.zero;
        private bool m_HasResolvedLaunchForwardOverride = false;
        public virtual void SetSkillPos(Vector3 pos)
        {
            m_SkillPos = pos;
        }

        public Vector3 ReadSkillPos()
        {
            return m_SkillPos;
        }
        protected virtual FirePointSelectMode GetFirePointSelectMode()
        {
            return m_Slot == 0 ? FirePointSelectMode.RoundRobin : FirePointSelectMode.First;
        }
        protected Vector3 ReadCastFirePoint()
        {
            if (!m_HasCastFirePoint)
            {
                m_CastFirePoint = m_Attacker != null
                    ? m_Attacker.GetFirePoint(GetFirePointSelectMode(), true)
                    : m_SkillPos;
                m_HasCastFirePoint = true;
            }

            return m_CastFirePoint;
        }

        public void ResetCastFirePoint()
        {
            m_HasCastFirePoint = false;
            m_CastFirePoint = Vector3.zero;
        }

        public Vector3 ResolveFreshFirePoint()
        {
            if (m_Attacker == null)
            {
                return m_SkillPos;
            }
            return m_Attacker.GetFirePoint(GetFirePointSelectMode(), true);
        }

        public Vector3 ResolveAimOrigin()
        {
            if (m_Attacker == null)
            {
                return m_SkillPos;
            }
            return m_Attacker.ReadResolvedFirePointCenter();
        }

        // 这个覆盖值只给塔防手操普攻的自动吸附使用。
        // 它的职责不是替代开火点朝向，而是在“保持开火点水平朝向不变、只修正竖直高度”时，
        // 把最终求出来的发射方向显式传到真正发弹的地方。
        public void SetResolvedLaunchForwardOverride(Vector3 dir)
        {
            if (dir.sqrMagnitude <= 0.0001f)
            {
                ClearResolvedLaunchForwardOverride();
                return;
            }

            m_HasResolvedLaunchForwardOverride = true;
            m_ResolvedLaunchForwardOverride = dir.normalized;
        }

        public void ClearResolvedLaunchForwardOverride()
        {
            m_HasResolvedLaunchForwardOverride = false;
            m_ResolvedLaunchForwardOverride = Vector3.zero;
        }

        // 技能发射方向默认仍优先使用开火点节点朝向。
        // 只有塔防自动吸附显式给出了覆盖值时，才允许绕过原始开火点朝向。
        // 这样可以保证角色俯仰、瞄准线和真正发弹最终看到的是同一份完整方向。
        protected Vector3 ResolveLaunchForward()
        {
            var dir = Vector3.zero;
            if (m_HasResolvedLaunchForwardOverride)
            {
                dir = m_ResolvedLaunchForwardOverride;
            }
            if (m_Attacker != null)
            {
                if (dir.sqrMagnitude <= 0.0001f)
                {
                    dir = m_Attacker.ReadResolvedFirePointForward();
                }
            }
            if (dir.sqrMagnitude <= 0.0001f)
            {
                dir = ReadSkillDir();
            }
            if (dir.sqrMagnitude <= 0.0001f && m_Attacker != null)
            {
                dir = m_Attacker.ReadForward();
            }
            if (dir.sqrMagnitude <= 0.0001f)
            {
                dir = Vector3.forward;
            }
            return dir.normalized;
        }

        public Vector3 ResolveAimHitPoint()
        {
            var aimOrigin = ResolveAimOrigin();
            var defender = m_MainDefender;
            if (BattleManager.ReadIsEntityValide(defender))
            {
                return defender.ReadHitPoint();
            }
            var dir = ResolveLaunchForward();
            return aimOrigin + dir * GetCastDistance();
        }
        public void SetDefender(PropertyEntity defender)
        {
            m_MainDefender = defender;
        }
        public void InitActionData()
        {
            m_ActionStages = null;
            m_CurAction = null;
        }
        public void SetActionFrom(Skill that)
        {
            m_ActionStages = that.m_ActionStages;
            m_CurAction = that.m_CurAction;
            m_CurActions = that.m_CurActions;
        }

        protected void AddStages(string[] actions)
        {
            m_ActionStages = new List<SkillActionStage>();

            int count = actions.Length;
            for (int i = 0; i < count; ++i)
            {

                SkillActionStage comb_stage = new SkillActionStage();
                var skill_action = actions[i];
                var comb = skill_action.Split('+');
                if (comb.Length > 1)
                {
                    comb_stage.m_ActionComb = new List<ActionData>();
                    foreach (var cb in comb)
                    {
                        t_actionBean bean = t_actionBean.GetConfig(cb);
                        var ad = new ActionData();
                        ad.CopyFromBean(bean);
                        comb_stage.m_ActionComb.Add(ad);
                    }
                }
                else
                {
                    var bean = t_actionBean.GetConfig(comb[0]);
                    var ad = new ActionData();
                    ad.CopyFromBean(bean);
                    comb_stage.m_ActionSingle = ad;
                }

                m_ActionStages.Add(comb_stage);
            }
        }
        public void AddAction(ActionData ad)
        {
            m_CurAction = ad;
        }
        public void AddSimpleAction(string ac_name, int cast, int finish, int rate)
        {
            var ad = new ActionData();
            ad.t_ac_name = ac_name;
            ad.t_ac_cast_point = cast;
            ad.t_ac_finish = finish;
            ad.t_frame_rate = rate;
            m_CurAction = ad;
        }
        public ActionData GetActionData()
        {
            return m_CurAction;
        }
        public void AddAction(string action)
        {
            if (string.IsNullOrEmpty(action))
            {
                SetDefaultAction();
                return;
            }
            var actions = action.Split('|');
            if (actions.Length > 1)
            {
                AddStages(actions);
            }
            else
            {
                var comb = action.Split('+');
                if (comb.Length > 1)
                {
                    m_CurActions = new SkillActionStage();
                    m_CurActions.m_ActionComb = new List<ActionData>();
                    foreach (var cb in comb)
                    {
                        t_actionBean bean = t_actionBean.GetConfig(cb);
                        if (bean != null)
                        {
                            var ad = new ActionData();
                            ad.CopyFromBean(bean);
                            m_CurActions.m_ActionComb.Add(ad);
                        }
                    }
                }
                else
                {
                    var bean = t_actionBean.GetConfig(action);
                    if (bean != null)
                    {
                        var ad = new ActionData();
                        ad.CopyFromBean(bean);
                        m_CurAction = ad;
                    }
                }
            }

            if (m_CurAction == null && (m_CurActions == null || m_CurActions.m_ActionComb == null || m_CurActions.m_ActionComb.Count == 0))
            {
                SetDefaultAction();
            }
        }

        private void SetDefaultAction()
        {
            var ad = new ActionData();
            ad.t_ac_name = m_DefaultActionName;
            ad.t_ac_cast_point = m_DefaultActionCastPoint;
            ad.t_ac_finish = m_DefaultActionFinish;
            ad.t_frame_rate = m_DefaultActionFrameRate;
            m_CurAction = ad;
        }

        protected int GetActionFrameRate()
        {
            if (m_CurAction != null)
            {
                if (m_CurAction.t_frame_rate <= 0)
                {
                    return 30;
                }
                else
                {
                    return m_CurAction.t_frame_rate;
                }
            }
            else
            {
                return 30;
            }
        }

        public GroupId ReadAttackerGroup()
        {
            return m_AttackerGroup;
        }
        public virtual SkillCastStatus GetCastStatus()
        {
            return m_CastStatus;
        }
        public Entity GetDefender()
        {
            return m_MainDefender;
        }

        //技能槽位
        protected int m_Slot = 0;
        public virtual int ReadSlot()
        {
            return m_Slot;
        }

        protected virtual void PlayAttackSound(int audioId, Vector3 pos)
        {
            if (m_SkillDescBean != null && m_SkillDescBean.t_attackSound != 0)
            {
                AudioManager.GetInstance().Play3D(m_SkillDescBean.t_attackSound, pos);
                return;
            }
            if (audioId == 0 || audioId == int.MinValue)
            {
                return;
            }
            AudioManager.GetInstance().Play3D(audioId, pos);
        }
        protected virtual void PlayHitSound(int audioId, Vector3 pos)
        {
            if (m_SkillDescBean != null && m_SkillDescBean.t_hitSound != 0)
            {
                AudioManager.GetInstance().Play3D(m_SkillDescBean.t_hitSound, pos);
                return;
            }
            if (audioId == 0 || audioId == int.MinValue)
            {
                return;
            }
            AudioManager.GetInstance().Play3D(audioId, pos);
        }
        protected virtual void PlayHitEff(int hitEffId, Vector3 hitPos)
        {
            //自动回收
            if (hitEffId != 0)
            {
                var eff = RenderEffManager.GetInstance().CreateRenderEff(hitEffId);
                eff.ShowEff(false, hitPos, Vector3.zero, Vector3.one);
                eff.SetDuringTime(2.0f);
                RenderEffManager.GetInstance().SetAutoPool(eff);
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="templateId">技能模板ID</param>
        public virtual void InitTemplate(int classId, long templateId)
        {
            m_ClassId = classId;
            m_SkillBean = t_skillBean.GetConfig(templateId);
            m_SkillDescBean = t_skillDescBean.GetConfig(templateId);
        }

        public virtual void PreLoadEffect()
        {

        }

        // 技能模板阶段只会预热子弹或表现对象本身。
        // 真正首发会卡住的是运行时 RenderEff 链路，所以在技能正式注册、拿到槽位后补一次。
        protected virtual void PreLoadRuntimeEffectsOnRegister()
        {
            if (m_SkillDescBean != null && m_SkillDescBean.t_atk_eff != 0)
            {
                PreLoadRenderEffOnRegister(m_SkillDescBean.t_atk_eff);
            }

            if (m_SkillDescBean != null && m_SkillDescBean.t_atk_cast_eff != 0)
            {
                PreLoadRenderEffOnRegister(m_SkillDescBean.t_atk_cast_eff);
            }

            if (m_Slot == 0 && m_SkillDescBean != null && m_SkillDescBean.t_hitEff != 0)
            {
                PreLoadRenderEffOnRegister(m_SkillDescBean.t_hitEff);
            }
        }

        // 这里只是业务侧主动触发一次 CreateRenderEff + PoolRenderEff，
        // 仍然完全复用 RenderEffManager 原本的对象池逻辑，不把“预热”语义塞进管理器。
        protected void PreLoadRenderEffOnRegister(int effectCfgId)
        {
            if (effectCfgId == 0)
            {
                return;
            }

            var renderEffMgr = RenderEffManager.GetInstance();
            if (renderEffMgr.GetPooledCountByCfgId(effectCfgId) > 0)
            {
                return;
            }

            var eff = renderEffMgr.CreateRenderEff(effectCfgId);
            renderEffMgr.PoolRenderEff(eff);
        }

        protected virtual object LoadEffect(bool show)
        {
            return null;
        }

        public virtual int ReadLevel()
        {
            return _GetSkillLevel();
        }

        public virtual void LevelUp(int curlevel)
        {
            if (_GetSkillLevel() != curlevel)
            {
                _SetSkillLevel(curlevel);
            }
        }
        public virtual void SetLevel(int level)
        {
            if (_GetSkillLevel() != level)
            {
                _SetSkillLevel(level);
            }
        }

        public Skill()
        {

        }

        public virtual void OnSkillRegister(PropertyEntity actor, int slot)
        {
            m_Slot = slot;
            m_Attacker = actor;
            m_AttackerGroup = actor.ReadGroup();
            m_AttackerId = actor.ReadId();
            PreLoadRuntimeEffectsOnRegister();

        }
        public virtual void OnSkillUnregister()
        {

        }
        protected float m_CooldownTime;
        private float m_CooldownReduceSeconds;
        public float ReadCooldownTime()
        {
            return m_CooldownTime;
        }

        public void SetCooldownReduceSeconds(float reduceSeconds)
        {
            m_CooldownReduceSeconds = Mathf.Max(0f, reduceSeconds);
        }

        protected float GetCooldownTime()
        {
            if (m_Slot == 0 &&
                m_SkillBean != null)
            {
                var intervalSecond = Mathf.Max(0.001f, m_SkillBean.t_Interval / 1000.0f);
                var attackSpeed = Mathf.Max(0.1f, GetAttackSpeed());
                var speedFactor = Mathf.Max(0.02f, attackSpeed);
                m_CooldownTime = Mathf.Max(0.001f, intervalSecond / speedFactor);
                return m_CooldownTime;
            }

            if (m_Slot == 0)
            {
                if (m_CurAction != null)
                {
                    var atk_speed = GetAttackSpeed();
                    int rate = GetActionFrameRate();
                    var action_time = BattleManager.ConvertFrame2Second(m_CurAction.t_ac_finish, atk_speed, rate);
                    var atk_idle_time = 0.1f;

                    if (atk_idle_time >= action_time)
                    {
                        //攻击间隔大于攻击时间
                        return atk_idle_time;
                    }
                    else
                    {
                        //var id = m_Attacker.GetId();
                        //action_time = ConvertFrame2MS(m_CurAction.t_ac_finish, atk_speed, rate);
                        //atk_idle_time = m_Attacker.GetNormalAtkBatTime();
                        //Debug.Log($"当前攻击时间:{action_time} 攻击间隔:{atk_idle_time} 攻击速度{atk_speed} 帧率:{rate}");
                        return action_time;
                    }
                }
            }
            // 配置表中t_cooldown按秒配置，运行时直接使用秒
            m_CooldownTime = Mathf.Max(0.001f, m_SkillBean.t_cooldown / 1000.0f - m_CooldownReduceSeconds);

            return m_CooldownTime;
        }



        protected float m_CooldownLeftTime = 0;
        public  float ReadCooldownLeftTime()
        {
            return m_CooldownLeftTime;
        }
        public bool ReadIsCooldown()
        {
            return m_CooldownLeftTime <= 0;
        }
        protected float GetCastDistance()
        {
            if (m_SkillBean == null)
            {
                return 0;
            }

            var bulletCfg = t_bullet.GetConfig(m_SkillBean.t_bullet_id, false);
            if (bulletCfg == null)
            {
                return 0;
            }

            return bulletCfg.t_move_speed / 1000.0f * bulletCfg.t_max_time / 1000.0f;
        }
        public virtual void SetCooldown(float stampTimeSeconds)
        {
            m_CooldownSinceTime = stampTimeSeconds;
        }

        public virtual void SetCooldownReady()
        {
            m_CooldownSinceTime = BattleManager.ReadBattleTime() - GetCooldownTime();
            m_CooldownLeftTime = 0f;
        }

        /// <summary>
        /// 模版Id
        /// </summary>
        /// <returns></returns>
        public virtual int GetClassId()
        {
            return m_ClassId;
        }
        public int GetCastId()
        {
            return m_CastId;
        }

        public virtual t_skillBean GetSkillBean()
        {
            return m_SkillBean;
        }
        public virtual t_skillDescBean GetSkillDescBean()
        {
            return m_SkillDescBean;
        }
        public long ReadSkillCfgId()
        {
            return m_SkillBean.t_id;
        }

        // 被动施放标记：为 true 时本技能实例跑在旁路施法通道上，与 SkillManager.m_CurrentSkill 无关。
        private bool m_IsPassiveCast;
        public bool ReadIsPassiveCast()
        {
            return m_IsPassiveCast;
        }
        public void SetPassiveCast(bool passive)
        {
            m_IsPassiveCast = passive;
        }
        // 施法状态机是否仍在推进（含被动施放）。
        public bool ReadIsCasting()
        {
            return m_CastStatus != SkillCastStatus.none && m_CastStatus != SkillCastStatus.end;
        }
        // 被动施放入口：不进 SkillManager 的当前技能通道，由 SkillManager 对全部技能的逐帧 Update 推进状态机。
        public void EnterPassive()
        {
            m_IsPassiveCast = true;
            OnEnter();
        }
        // 施法者死亡/受控/挂起/冰冻时被动施放自检终止；被动施放不走 BreakSkill，必须在这里兼顾。
        private bool ReadPassiveCastCanContinue()
        {
            if (!BattleManager.ReadIsEntityValide(m_Attacker))
            {
                return false;
            }
            return !m_Attacker.ReadIsBeingControlled() &&
                !m_Attacker.ReadIsSuspended() &&
                !m_Attacker.ReadIsFrozen();
        }
        /// <summary>
        /// 发动技能前调用
        /// </summary>
        public virtual void OnEnter()
        {
            m_CastStartTime = BattleManager.ReadBattleTime();
            m_IsBoneNormalAttack = m_NextCastIsBoneNormalAttack;
            m_NextCastIsBoneNormalAttack = false;
            m_BoneActionFrameProgress = 0f;
            m_BoneActionProgressTime = m_CastStartTime;
            m_AttackSpeed = 100;
            m_CastId++;
            m_SkillCastBuffApplied = false;
            m_HasCastFirePoint = false;

            if (m_IsPassiveCast)
            {
                // 被动施放和角色正在进行的其他技能同时存在：不能重置上半身姿态，
                // 也不走“施法时移除 buff”，否则会把当前行为的表现/buff 一起打断。
                BattleManager.GetBattleTool().ConsumeSkillPreconditionIfNeeded(m_Attacker, m_SkillBean);
                return;
            }

            // 清掉上一轮施法的上半身转动残留，新一轮按 warming_up 重新启动
            m_UpperRotLaunched = false;
            m_UpperRotRestored = false;
            RestoreUpperBodySpine();
            BattleManager.GetBattleTool().ConsumeSkillPreconditionIfNeeded(m_Attacker, m_SkillBean);

            if (m_Attacker is PlayerHero && m_Slot > 0)
            {
                BattleManager.GetBattleTool().RemoveBuffsOnSkillCast(m_Attacker);
            }


        }
        protected void ApplySkillCastBuffIfNeeded()
        {
            if (m_SkillCastBuffApplied)
            {
                return;
            }

            m_SkillCastBuffApplied = true;
            BattleManager.GetBattleTool().AddSkillCastBuffs(m_Attacker, m_SkillBean);
        }
        protected float GetSkillUsedTime()
        {
            return Mathf.Max(0, BattleManager.ReadBattleTime() - m_CastStartTime);
        }

        public float ReadSkillUsedTime()
        {
            return GetSkillUsedTime();
        }

        protected virtual void PlayAction(string ani)
        {
            if (m_Attacker == null)
            {
                return;
            }
            if (m_IsBoneNormalAttack)
            {
                StartBoneNormalAttackAnimation();
                m_Attacker.GetRender().PlayAnimation(ani, null,
                    TowerDefendBoneAttackTuning.m_AnimationBlendSecond);
                return;
            }
            var speed = GetAttackSpeed() * GetActionFrameRate() / 30;
            m_Attacker.GetRender().SetAnimationSpeed(speed);
            //Debug.Log($"当前攻击动画速度：{speed}");

            if (m_SkillDescBean == null || m_SkillDescBean.t_finish_change_idle == 0)
            {
                m_Attacker.GetRender().PlayAnimation(ani, "idle", 0);
            }
            else
            {
                m_Attacker.GetRender().PlayAnimation(ani, null, 0);
            }
        }

        public bool IsSkillCanBreakStatus(int new_slot = -1)
        {
            if (new_slot == 0)
            {
                return false;
            }
            if (m_Slot == 0)
            {
                // 连射可以持续续期，进阶攻击仍应在首发后按原有优先级打断普攻。
                if(m_CastStatus > SkillCastStatus.cast_point)
                {
                    return true;
                }
            }
            return false;
        }
        protected virtual void OnUpdateCooldown(float dt)
        {
            m_CooldownLeftTime = Mathf.Max(0, (m_CooldownSinceTime + GetCooldownTime()) - BattleManager.ReadBattleTime());
        }

        /// <summary>
        /// 发动技能中调用
        /// </summary>
        public virtual void Update(float dt)
        {
            OnUpdateCooldown(dt);

            if (m_IsPassiveCast)
            {
                // 被动施放不占施法通道，不会经 BreakSkill 收到打断，也不允许碰上半身姿态（可能正被其他技能占用）
                if (ReadIsCasting() && !ReadPassiveCastCanContinue())
                {
                    OnCastOver();
                }
                return;
            }

            UpdateBoneNormalAttackProgress();
            UpdateUpperBodyRotate(dt);
        }

        // 上半身转动表现（技能描述表 t_upper_rot_params = 角度+缓动毫秒）：
        // 预热阶段开始把 spine_01.x 转到配置角度（绕局部 X 轴），cast_back 开始以同样时间转回。
        // spine 被动画每帧驱动，逻辑层只计算目标角度，由渲染层在动画更新后按增量叠加，
        // 不覆盖动画或其它系统对骨骼的默认控制。
        private const string UpperBodySpineNodeName = "spine_01.x";
        private bool m_UpperRotLaunched;
        private bool m_UpperRotRestored;
        private bool m_UpperRotTweening;
        private float m_UpperRotCurrentAngle;
        private float m_UpperRotFromAngle;
        private float m_UpperRotToAngle;
        private float m_UpperRotElapsed;
        private float m_UpperRotDuration;

        private bool TryReadUpperRotConfig(out float targetAngle, out float durationSecond)
        {
            targetAngle = 0f;
            durationSecond = 0f;
            if (m_SkillDescBean == null || m_SkillDescBean.t_upper_rot_params == null ||
                m_SkillDescBean.t_upper_rot_params.Count < 2)
            {
                return false;
            }

            targetAngle = m_SkillDescBean.t_upper_rot_params[0];
            durationSecond = Mathf.Max(0f, m_SkillDescBean.t_upper_rot_params[1] / 1000.0f);
            return !Mathf.Approximately(targetAngle, 0f) && durationSecond > 0f;
        }

        private void UpdateUpperBodyRotate(float dt)
        {
            if (!TryReadUpperRotConfig(out var targetAngle, out var durationSecond))
            {
                return;
            }

            if (m_CastStatus == SkillCastStatus.warming_up && !m_UpperRotLaunched)
            {
                m_UpperRotLaunched = true;
                BeginUpperBodyRotate(targetAngle, durationSecond);
            }
            else if (m_CastStatus == SkillCastStatus.cast_back && m_UpperRotLaunched && !m_UpperRotRestored)
            {
                // 抬起与放下使用同样的缓动时间
                m_UpperRotRestored = true;
                BeginUpperBodyRotate(0f, durationSecond);
            }

            if (!m_UpperRotTweening)
            {
                return;
            }

            m_UpperRotElapsed += dt;
            float progress = m_UpperRotDuration <= 0f ? 1f : Mathf.Clamp01(m_UpperRotElapsed / m_UpperRotDuration);
            m_UpperRotCurrentAngle = Mathf.Lerp(m_UpperRotFromAngle, m_UpperRotToAngle, progress);
            ApplyUpperBodyRotate(m_UpperRotCurrentAngle);
            if (progress >= 1f)
            {
                m_UpperRotTweening = false;
            }
        }

        private void BeginUpperBodyRotate(float targetAngle, float durationSecond)
        {
            m_UpperRotFromAngle = m_UpperRotCurrentAngle;
            m_UpperRotToAngle = targetAngle;
            m_UpperRotElapsed = 0f;
            m_UpperRotDuration = durationSecond;
            m_UpperRotTweening = true;
        }

        private void ApplyUpperBodyRotate(float angle)
        {
            m_UpperRotCurrentAngle = angle;
            if (m_Attacker != null)
            {
                (m_Attacker.GetRender() as UActor)?.SetExtraBoneRotation(UpperBodySpineNodeName, angle);
            }
        }

        private void RestoreUpperBodySpine()
        {
            m_UpperRotTweening = false;
            m_UpperRotCurrentAngle = 0f;
            ApplyUpperBodyRotate(0f);
        }
        public virtual void UpdateRender()
        {

        }
        /// <summary>
        /// 停止技能时调用
        /// </summary>
        public virtual void Stop()
        {
            if (m_IsPassiveCast)
            {
                // 被动施放没有占用施法通道，不能在这里重置施法者的上半身姿态，只收尾自己持有的表现
                OnCastOver();
                return;
            }
            // 施法被打断时上半身姿态不能停留在转动后的状态
            RestoreUpperBodySpine();
            if (m_IsBoneNormalAttack)
            {
                m_CastStatus = SkillCastStatus.end;
                RestoreBoneNormalAttackAnimation();
                return;
            }
            if (m_SkillBean != null && m_SkillBean.t_keep_time > 0)
            {
                RenderEvent.Event.KeepSkill(this.GetCastId(), false);
            }
        }

        protected virtual void OnCalHurt(bool isFirst)
        {

        }

        public virtual void Destroy()
        {
            m_IsPassiveCast = false;
            RestoreBoneNormalAttackAnimation();
            RestoreUpperBodySpine();
            if (m_CastStatus == SkillCastStatus.warming_up ||
                m_CastStatus == SkillCastStatus.cast_point ||
                m_CastStatus == SkillCastStatus.cast_keep ||
                m_CastStatus == SkillCastStatus.cast_back)
            {
                m_CastStatus = SkillCastStatus.end;

            }
        }

        protected string[] ParseMountPaths(string mountPath)
        {
            if (string.IsNullOrEmpty(mountPath))
            {
                return Array.Empty<string>();
            }
            return mountPath.Split('+');
        }
    }
}
