using MonoBean;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace GameDll
{
    public enum PropertyType
    {
        None = -1,
        atk = 0,
        atk_percent = 2,
        hp = 5,
        max_hp_percent = 7,
        crit = 10,
        crit_damage = 15,
        move_speed = 20,
        attack_speed = 25,
        move_speed_percent = 22,
        attack_speed_percent = 27,
        amp = 30,
        duration = 35,
        attack_range = 40,
        runtime_hp = 1000,
        runtime_magic = 1001,
    }
    public enum emEntityState
    {
        None,
        em_EntityState_Idle,
        em_EntityState_Move,
        em_EntityState_Attack,
        em_EntityState_Skill,
        em_EntityState_Stand,
        em_EntityState_Dead,
        em_EntityState_Born
    }
    public class PropertyEntity : Entity
    {
        protected float m_IgnoreTrapTime;
        private bool m_IsSharedByGroup = false;
        private bool m_IsSharedByAll = false;

        protected bool m_IgnoreTerrain;
        protected BuffManager m_BuffManager = new BuffManager();
        protected bool m_IsInSilence = false;
        private Buff m_CoinBuff;
        private int m_CannotUseNormalAttackNum;
        private int m_CannotUseSkillNum;
        private int m_ReboundSkillNum;
        private int m_XuWuNum;
        private UResource m_FakeBody;
        protected SkillManager m_SkillManager = new SkillManager();
        protected bool m_IsYangDaoState = false;
        protected PropertyEntity m_AttackMe;
        protected PropertyEntity m_Target;
        private Action<PropertyEntity> m_OnDeadAnimationFinished;
        //最后获得我的人头的实体id
        protected int m_KillMeAttackId;
        protected GroupId m_KillMeAttackGroup;
        //幻影等存活时间
        protected float m_WillDeadTime = -1;
        protected bool m_bWillDead = false;
        protected int m_DeadAliveTimeMap = 0;
        protected int m_DeadCount;

        private float m_HPRuntime = 0;
        private float m_MagicRuntime = 0;
        private float m_MaxHpScalePermille = 1.0f;
        public float GetHpRuntime()
        {
            return m_HPRuntime;
        }
        public float ReadMagic()
        {
            return m_MagicRuntime;
        }
        public void SetMagicRuntime(float magic)
        {
            m_MagicRuntime = Math.Max(0, magic);
        }
        public float GetMaxMagic()
        {
            return 0;
        }
        public void SetMaxHpScalePermille(float scalePermille)
        {
            m_MaxHpScalePermille = Mathf.Max(0.001f, scalePermille / 1000f);
        }
        public float ReadCritRatePermille()
        {
            var rate = 0f;
            if (m_BuffManager != null)
            {
                rate += m_BuffManager.ReadBuffPropertyData((int)PropertyType.crit);
            }
            return Mathf.Clamp(rate, 0, 1f);
        }
        public float ReadCritDamageScalePermille()
        {
            if (m_BuffManager == null)
            {
                return 1.0f;
            }

            var buffScale = m_BuffManager.ReadBuffPropertyData((int)PropertyType.crit_damage);
            if (Mathf.Approximately(buffScale, 0f))
            {
                return 1.0f;
            }

            return Math.Max(0, buffScale);
        }
        public override bool ReadIsSharedByGroup()
        {
            return m_IsSharedByGroup;
        }
        public void SetIsSharedByGroup(bool shared)
        {
            m_IsSharedByGroup = shared;
        }



        public bool ReadIsSharedByAll()
        {
            return m_IsSharedByAll;
        }
        public void SetIsSharedByAll(bool shared)
        {
            m_IsSharedByAll = shared;
        }


        public void SetIgnoreTerrain(bool ignoreTerrain)
        {
            m_IgnoreTerrain = ignoreTerrain;
        }
        public bool IsIgnoreTerrain()
        {
            return m_IgnoreTerrain;
        }
        public override bool ReadIsPropertyEntity()
        {
            return true;
        }




        public override Skill ReadNormalSkill()
        {
            return m_SkillManager.ReadSkillBySlot(0);
        }



        public override Skill ReadCurrentSkill()
        {
            return m_SkillManager.GetCurrentSkill();
        }
        public override SkillManager GetSkillManager()
        {
            return m_SkillManager;
        }
        public override void AttackFinish(AttackFinishReason reason)
        {
            if (reason == AttackFinishReason.Success)
            {
                var curSkill = m_SkillManager.GetCurrentSkill();
                m_SkillManager.LeaveSkill();
                if (!m_SkillManager.TryWillNextUseSkill())
                {
                    var stateMgr = GetStateManager();
                    if (stateMgr != null)
                    {
                        var cur_state = stateMgr.GetCurrentStateType();
                        if (cur_state == emEntityState.em_EntityState_Attack)
                        {
                            stateMgr.ChangeState(emEntityState.em_EntityState_Idle);
                        }
                    }
                }
            }
            else
            {
                
                m_SkillManager.BreakSkill();

                var stateMgr = GetStateManager();
                if (stateMgr != null)
                {
                    var cur_state = stateMgr.GetCurrentStateType();
                    if (cur_state == emEntityState.em_EntityState_Attack)
                    {
                        //stateMgr.ChangeState(emEntityState.em_EntityState_Idle);
                        this.TryChangeState(emEntityState.em_EntityState_Idle);
                    }
                }
            }

        }
        public override StateBase GetCurrentState()
        {
            var stateMgr = GetStateManager();
            if (stateMgr == null)
            {
                return null;
            }
            else
            {
                return stateMgr.GetCurrentState();
            }
        }
        public override bool TryChangeState(emEntityState em_EntityState, bool must = false)
        {
            var stateMgr = this.GetStateManager();
            if (stateMgr == null)
            {
                return false;
            }
            var curState = stateMgr.GetCurrentState();
            if (curState == null)
            {
                return stateMgr.ChangeState(em_EntityState);
            }
            if (!must)
            {
                if (curState.m_StateType == emEntityState.em_EntityState_Dead)
                {
                    return false;
                }
            }
            if (curState.GetStateType() != em_EntityState)
            {
                return stateMgr.ChangeState(em_EntityState);
            }
            else
            {
                return true;
            }
        }

        public virtual bool IsTraitor()
        {
            return false;
        }


        //能否使用技能
        public override AttackFailedReason CanAttack(Skill skill)
        {
            if (skill == null)
            {
                return AttackFailedReason.UnregisterSkill;
            }
            if (skill.ReadSlot() == 0 && m_CannotUseNormalAttackNum > 0)
            {
                return AttackFailedReason.SystemError;
            }
            if (skill.ReadSlot() != 0 && m_IsInSilence)
            {
                return AttackFailedReason.Silence;
            }
            if (!BattleManager.GetBattleTool().CheckSkillPrecondition(this, skill.GetSkillBean()))
            {
                return AttackFailedReason.SystemError;
            }
            if (skill.ReadIsCooldown() == false)
            {
                return AttackFailedReason.NotCoolDown;
            }
            return AttackFailedReason.Success;
        }
        public BuffManager GetBuffManager()
        {
            return m_BuffManager;
        }
        public void AddCannotUseNormalAttackNum(int num)
        {
            m_CannotUseNormalAttackNum += num;
            if (m_CannotUseNormalAttackNum < 0)
            {
                m_CannotUseNormalAttackNum = 0;
            }
        }
        public void AddCannotUseSkillNum(int num)
        {
            m_CannotUseSkillNum += num;
            if (m_CannotUseSkillNum < 0)
            {
                m_CannotUseSkillNum = 0;
            }
            m_IsInSilence = m_CannotUseSkillNum > 0;
        }
        public void AddReboundSkill(int num)
        {
            m_ReboundSkillNum += num;
            if (m_ReboundSkillNum < 0)
            {
                m_ReboundSkillNum = 0;
            }
        }
        public void ChangeXuWu(int num)
        {
            m_XuWuNum += num;
            if (m_XuWuNum < 0)
            {
                m_XuWuNum = 0;
            }

            var render = GetRender();
            if (render != null)
            {
                render.ChangeXuWu(num);
            }
        }
        public void TryEnterDizzyState(float duringSeconds)
        {
            // 当前项目没有旧版眩晕状态机入口，这里保留兼容函数供 Buff 调用。
        }
        public void CheckPropertyChanged()
        {
            // 当前轻量战斗属性实时计算链路没有旧版缓存刷新入口，保留兼容空实现。
        }
        public Vector3 ReadPosition()
        {
            return GetPosition();
        }
        public virtual void CreateFakeBody(t_modelBean cfg)
        {
            if (cfg == null)
            {
                return;
            }

            if (m_FakeBody == null)
            {
                m_FakeBody = UResourceFactory.New_EntityObject(ResourceType.Actor, emEntityType.em_EntityType_Actor);
            }
            if (m_FakeBody != null)
            {
                m_FakeBody.LoadRender(cfg.t_model_res, Tool.GetAssetName(cfg.t_model_res));
                m_FakeBody.SetPosition(GetPosition());
                m_FakeBody.SetForward(ReadForward());
            }
        }
        public virtual UResource GetFakeBody()
        {
            return m_FakeBody;
        }
        public virtual void ShowFakeBody(bool show)
        {
            if (m_FakeBody != null)
            {
                m_FakeBody.SetActive(show);
            }
        }
        public virtual void DestroyFakeBody()
        {
            if (m_FakeBody != null)
            {
                m_FakeBody.Destroy();
                m_FakeBody = null;
            }
        }
        public override void InitInstance()
        {
            base.InitInstance();
            m_MaxHpScalePermille = 1.0f;
            m_IsFreeze = false;
            m_BuffManager.SetOwner(this);
            m_BuffManager.Init();
        }

        public override void Destroy()
        {
            if (m_BuffManager != null)
            {
                m_BuffManager.ClearBuffs();
            }
            DestroyFakeBody();
            base.Destroy();
        }

        public void SetDeadAnimationFinishedHandler(Action<PropertyEntity> handler)
        {
            m_OnDeadAnimationFinished = handler;
        }

        public virtual bool TryHandleDeadAnimationFinished()
        {
            if (m_OnDeadAnimationFinished == null)
            {
                return false;
            }

            m_OnDeadAnimationFinished(this);
            return true;
        }

        public virtual void ResetRuntimeForReuse()
        {
            BreakSkill();
            if (m_SkillManager != null)
            {
                m_SkillManager.ClearWillNextUseSkill();
                m_SkillManager.ClearSkills();
            }
            if (m_BuffManager != null)
            {
                m_BuffManager.ClearBuffs();
            }
            if (m_XuWuNum != 0)
            {
                var render = GetRender();
                if (render != null)
                {
                    render.ChangeXuWu(-m_XuWuNum);
                }
            }

            m_CoinBuff = null;
            m_CannotUseNormalAttackNum = 0;
            m_CannotUseSkillNum = 0;
            m_ReboundSkillNum = 0;
            m_XuWuNum = 0;
            m_IsInSilence = false;
            m_AttackMe = null;
            m_Target = null;
            m_KillMeAttackId = 0;
            m_KillMeAttackGroup = GroupId.AnyGroupId;
            m_WillDeadTime = -1;
            m_bWillDead = false;
            m_DeadAliveTimeMap = 0;
            m_DeadCount = 0;
            m_IsYangDaoState = false;
            m_IgnoreTrapTime = 0;
            m_BattlePlayerId = 0;
            m_BattlePlayerName = string.Empty;
            m_NoHurtTime = 0;
            SetHpRuntime(0);
            SetMagicRuntime(0);
            SetCanBeHurt(true);
            SetCanBeTarget(true);
            SetFreeze(false);
        }

        protected virtual void InitProperties(long cfgId)
        {
            m_BaseHp = 1;
            SetMagicRuntime(0);
        }


        public override void SetYangDaoState(bool yang)
        {
            m_IsYangDaoState = yang;
        }
        public override bool ReadIsYangDaoState()
        {
            return m_IsYangDaoState;
        }

        public override void SetKillMeAttackId(int id, GroupId group)
        {
            m_KillMeAttackId = id;
            m_KillMeAttackGroup = group;
        }
        public override int GetKillMeAttackId()
        {
            return m_KillMeAttackId;
        }
        public override GroupId GetKillMeAttackGroup()
        {
            return m_KillMeAttackGroup;
        }
        public override void SetAttackMe(PropertyEntity attackMe)
        {
            m_AttackMe = attackMe;
        }
        public virtual void SetBornPosition(Vector3 pos)
        {

        }
        public virtual Vector3 ReadBornPosition()
        {
            return Vector3.zero;
        }

        public override PropertyEntity GetAttackMe()
        {
            return m_AttackMe;
        }

        public virtual void SellAllBagItemByQulity(int quality)
        {
        }

        public override PropertyEntity ReadDefender()
        {
            return m_Target;
        }
        public override void SetDefender(PropertyEntity target)
        {
            m_Target = target;
        }
        protected virtual void CheckTarget()
        {
            if (m_Target == null)
            {
                return;
            }
            if (m_Target.ReadIsDestroy())
            {
                m_Target = null;
            }
            else if (m_Target.ReadIsDead())
            {
                m_Target = null;
            }
        }
        protected virtual void CheckAttackMe()
        {
            if (m_AttackMe == null)
            {
                return;
            }
            if (m_AttackMe.ReadIsDestroy())
            {
                m_AttackMe = null;
            }
            else if (m_AttackMe.ReadIsDead())
            {
                m_AttackMe = null;
            }
        }
        public override void BreakSkill()
        {
            var skillMgr = GetSkillManager();
            if (skillMgr != null)
            {
                skillMgr.BreakSkill();
            }
        }

        public virtual void SetWillDeadTime(float time)
        {
            m_WillDeadTime = time;
            if (time > 0)
            {
                m_bWillDead = true;
            }

        }
        private void UpdateWillDead(float dt)
        {
            if (m_bWillDead)
            {
                m_WillDeadTime -= dt;
                if (m_WillDeadTime <= 0)
                {
                    OnWillDead();
                }
            }
        }
        //存活时间到了，消失
        protected virtual void OnWillDead()
        {
            SetHpRuntime(0);
            GetRender().SetHpValue(0, 1);

            var battle = BattleManager.GetBattle();
            if (battle != null)
            {
                //battle.GetBattleStat().OnHeroDead(this);
            }
            //BRenderEvent.Event.OnHeroDead(this.GetId());
            //BRenderEvent.Event.RemoveMiniMap(this.ReadId());


            var current = GetStateManager().GetCurrentState().GetStateType();
            if (current != emEntityState.em_EntityState_Dead)
            {
                TryChangeState(emEntityState.em_EntityState_Dead);
            }
        }



        public void SetAliveTimeMap(int time)
        {
            m_DeadAliveTimeMap = time;
        }

        public virtual int ReadDeadCount()
        {
            return m_DeadCount;
        }
        public override void OnDead()
        {
            if (m_BuffManager != null)
            {
                m_BuffManager.ClearBuffs();
            }
            m_DeadCount++;
            base.OnDead();
        }



        public override void UpdateRender()
        {
            base.UpdateRender();
            if (m_SkillManager != null)
            {
                m_SkillManager.UpdateRender();
            }
            if (m_BuffManager != null)
            {
                m_BuffManager.UpdateRender();
            }
        }
        public override void Update(float dt) 
        {
            base.Update(dt);
            if (m_IsFreeze)
            {
                return;
            }
            if (m_SkillManager != null)
            {
                m_SkillManager.Update(dt);
            }
            if (m_BuffManager != null)
            {
                m_BuffManager.Update(dt);
            }
            UpdateWillDead(dt);
            UpdateIgnoreTrapTime(dt);
            UpdateNormalAtk();
        }

        protected virtual void UpdateNormalAtk()
        {
            //更新普攻
            var nskill = ReadNormalSkill();
            if (nskill != null)
            {
                var atk = nskill.GetAtk();
                if (atk == 0)
                {
                    nskill.SetAtk(this);
                    atk = nskill.GetAtk();
                }
                else if (BattleManager.ReadBattleTime() - nskill.GetLastCheckAtkChangeTime() >= 2.0f)
                {
                    if (this.ReadIsHero())
                    {
                        nskill.SetAtk(this);
                        atk = nskill.GetAtk();
                    }
                }
            }
        }
        /////////////////////////属性////////////////////////////////
        #region 属性
        protected int m_Level;
        protected bool m_IsFreeze = false;
        protected void _SetLevel(int level)
        {
            m_Level = level;
            GetRender().SetLevel(m_Level);
        }

        public virtual void SetFreeze(bool v)
        {
            m_IsFreeze = v;
            var render = GetRender();
            if (render != null)
            {
                render.SetMoveSpeed(0);
                render.SetAnimationSpeed(v ? 0 : 1.0f);
            }
        }

        public virtual bool ReadIsFreeze()
        {
            return m_IsFreeze;
        }

        protected int _GetLevel()
        {
            return m_Level;
        }
        public override void InitLevel(int level)
        {
            _SetLevel(level);
        }
        
        public override void InitHp()
        {
            float curMaxHp = GetMaxHP();
            SetHpRuntime(curMaxHp);

            var hp = ReadHP();
            OnHpChanged();
        }

        public override void SetLevel(int level)
        {
            var lastMaxHp = GetMaxHP();
            var lastHp = ReadHP();

            var lastLevel = ReadLevel();
            var dtLevel = level - lastLevel;

            _SetLevel(level);

            var curMaxHp = GetMaxHP();
            var dtHp = curMaxHp - lastMaxHp;

            var hp = lastHp + dtHp;
            if (hp > curMaxHp)
            {
                hp = curMaxHp;
            }
            SetHpRuntime(hp);

            OnHpChanged();
            if (dtLevel != 0)
            {
                OnLevelUp();
            }

        }



        protected virtual void OnLevelUp()
        {

        }
        public override int ReadLevel()
        {
            return _GetLevel();
        }
        public override float ReadHP()
        {
            return m_HPRuntime;
        }
        public override void SetHpRuntime(float hp)
        {
            m_HPRuntime = Math.Max(0, hp);
        }
        public override float GetMaxHP()
        {
            var baseHp = Math.Max(1, m_BaseHp);
            var flatHp = m_BuffManager != null ? m_BuffManager.ReadBuffPropertyData((int)PropertyType.hp) : 0;
            var hpPercent = m_BuffManager != null ? m_BuffManager.ReadBuffPropertyData((int)PropertyType.max_hp_percent) : 0;
            var maxHp = baseHp + flatHp;
            maxHp = Math.Max(1, maxHp);
            maxHp = maxHp * Math.Max(0, 1f + hpPercent);
            maxHp = maxHp * m_MaxHpScalePermille;
            return Math.Max(1, maxHp);
        }
        public override float GetAtk()
        {
            var atk = 0f;
            if (m_BuffManager != null)
            {
                atk += m_BuffManager.ReadBuffPropertyData((int)PropertyType.atk);
                var atkPercent = m_BuffManager.ReadBuffPropertyData((int)PropertyType.atk_percent);
                atk = atk * Math.Max(0, 1f + atkPercent);
            }

            return Math.Max(0, atk);
        }
        public override float GetNormalAtkSpeed()
        {
            var speed = 0.0f;
            if (m_BuffManager != null)
            {
                speed = m_BuffManager.ReadBuffPropertyData((int)PropertyType.attack_speed);
            }

            return Math.Max(0.1f, speed);
        }
        public override float GetAttackRange()
        {
            return m_BuffManager != null ? Math.Max(0, m_BuffManager.ReadBuffPropertyData((int)PropertyType.attack_range)) : 0;
        }
        public override float ReadWarningDist()
        {
            var buffAttackRange = GetAttackRange();
            if (buffAttackRange > 0)
            {
                return buffAttackRange;
            }

            return base.ReadWarningDist();
        }
        public override float ReadDamageAmpPercent()
        {
            if (m_BuffManager == null)
            {
                return 1f;
            }

            var buffScale = m_BuffManager.ReadBuffPropertyData((int)PropertyType.amp);
            if (Mathf.Approximately(buffScale, 0f))
            {
                return 1f;
            }

            return Math.Max(0, buffScale);
        }
        public void ChangeGroup(GroupId group)
        {
            SetGroup(group);
            //EntityRenderTool.SetCampColor(this);
        }
        public override float GetPropertyBase(int propertyType)
        {
            switch (propertyType)
            {
                case (int)PropertyType.hp:
                    {
                        return m_BaseHp;
                    }

            }
            return 0;
        }
        public override float GetProperty(int propertyType)
        {
            float value = 0;
            switch (propertyType)
            {
                case (int)PropertyType.hp:
                    {
                        value = m_BaseHp;
                        break;
                    }
                case (int)PropertyType.runtime_hp:
                    {
                        value = ReadHP();
                        break;
                    }
                case (int)PropertyType.max_hp_percent:
                    {
                        value = GetMaxHP();
                        break;
                    }
                case (int)PropertyType.runtime_magic:
                    {
                        value = ReadMagic();
                        break;
                    }
                case (int)PropertyType.attack_range:
                    {
                        value = GetMaxMagic();
                        break;
                    }

                case (int)PropertyType.atk:
                    {
                        value = GetAtk();
                        break;
                    }
            }


            return value;
        }

        protected long m_BattlePlayerId = 0;
        public void SetBattlePlayerId(long battle_player_id)
        {
            m_BattlePlayerId = battle_player_id;
        }
        public override long ReadBattlePlayerId()
        {
            return m_BattlePlayerId;
        }
        protected string m_BattlePlayerName = "";
        public void SetBattlePlayerName(string name)
        {
            m_BattlePlayerName = name;
        }
        public string GetBattlePlayerName()
        {
            return m_BattlePlayerName;
        }
        
        

       
        #endregion

        private float m_BaseHp;
        public void SetBaseHppublic(float value)
        {
            m_BaseHp = Math.Max(1, value);
        }

        public override void AttackDir(int slot, Vector3 face_forward, Vector3 move_dir)
        {
            Skill skill = m_SkillManager.ReadSkillBySlot(slot);
            if (skill == null)
            {
                return;
            }

            var skillBean = skill.GetSkillBean();
            var bulletCfg = skillBean != null ? t_bullet.GetConfig(skillBean.t_bullet_id, false) : null;
            var iDist = bulletCfg != null
                ? bulletCfg.t_move_speed / 1000.0f * bulletCfg.t_max_time / 1000.0f
                : 1.0f;
            var resolvedForward = face_forward.sqrMagnitude > 0.0001f ? face_forward.normalized : Vector3.forward;
            var pos = GetPosition() + resolvedForward * iDist;

            // 只有塔防守卫英雄的普攻会进入这套自动吸附。
            // 这里求出来的完整方向要同时喂给三条链路：
            // 1. 技能实例的发射方向；
            // 2. 角色当前的俯仰表现；
            // 3. 技能自身按既有飞行长度推导出来的终点位置。
            // 自动吸附只负责修正最终发射方向，不再把“用于求俯仰的目标点”直接当成技能终点。
            skill.ClearResolvedLaunchForwardOverride();
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            bool useTowerDefendGuardHeroNormalLaunch = false;
            if (battle != null &&
                slot == 0 &&
                battle.TryResolveGuardHeroNormalAutoAim(this, skill, face_forward, out var autoAimSolution))
            {
                useTowerDefendGuardHeroNormalLaunch = true;
                if (autoAimSolution.m_HasSnapTarget)
                {
                    resolvedForward = autoAimSolution.m_ResolvedLaunchForward;
                }
            }

            if (useTowerDefendGuardHeroNormalLaunch)
            {
                // 就算这一发没有吸附到目标，也必须把当前最终发射方向显式回写给姿态和真正发弹。
                // 否则 BattleManager.AttackDir 前置的 SetForward 会把 UPlayerActor 暂时打回默认俯仰，
                // 在“刚离开棒棒糖、怪还没恢复出来”的空窗里就会看到角色身体突然向下俯一下。
                skill.SetResolvedLaunchForwardOverride(resolvedForward);
                SetBaseForward(resolvedForward);
            }

            pos = GetPosition() + resolvedForward * iDist;
            skill.SetSkillDir(resolvedForward);
            skill.SetMoveDir(move_dir);
            skill.SetSkillPos(pos);

            skill.SetAtk(this);

            skill.SetDefender(null);
            skill.SetIsDirCast(true);
            skill.SetCooldown(BattleManager.ReadBattleTime());

            m_SkillManager.EnterSkill(skill);
        }

        public virtual bool ReadShouldSyncFacingOnAttack()
        {
            return true;
        }

        public override void Attack(int slot, Vector3 dir, Vector3 pos, int targetId)
        {
            Skill skill = m_SkillManager.ReadSkillBySlot(slot);
            if (skill != null)
            {
                skill.ClearResolvedLaunchForwardOverride();
            }

            if (pos.x == int.MinValue || pos.z == int.MinValue)
            {

            }
            else
            {
                var forward = pos - GetPosition();
                if (ReadShouldSyncFacingOnAttack())
                {
                    SetForward(forward.normalized);
                }
            }

            if (skill == null)
            {
                return;
            }

            skill.SetSkillDir(dir);
            skill.SetMoveDir(Vector3.zero);
            skill.SetSkillPos(pos);

            skill.SetAtk(this);

            var objMgr = BattleManager.GetObjectManager();
            var defender = objMgr.ReadPropertyEntityById(targetId);

            skill.SetDefender(defender);

            skill.SetIsDirCast(false);
            skill.SetCooldown(BattleManager.ReadBattleTime());

            m_SkillManager.EnterSkill(skill);

        }
        public override void Attack(Skill skill, Vector3 dir, Vector3 pos, PropertyEntity defender)
        {
            if (skill != null)
            {
                skill.ClearResolvedLaunchForwardOverride();
            }
            else
            {
                return;
            }

            var forward = pos - GetPosition();
            if (ReadShouldSyncFacingOnAttack())
            {
                SetForward(forward.normalized);
            }
            skill.SetSkillDir(dir);
            skill.SetMoveDir(Vector3.zero);
            skill.SetSkillPos(pos);
            skill.SetDefender(defender);

            skill.SetAtk(this);

            skill.SetIsDirCast(false);
            skill.SetCooldown(BattleManager.ReadBattleTime());
            m_SkillManager.EnterSkill(skill);
        }
        private void UpdateIgnoreTrapTime(float dt)
        {
            if(m_IgnoreTrapTime > 0)
            {
                m_IgnoreTrapTime -= dt;
                if (m_IgnoreTrapTime <= 0)
                {
                    m_IgnoreTrapTime = 0;
                }
            }

        }
        public float GetIgnoreTrapTime()
        {
            return m_IgnoreTrapTime;
        }
        public void SetIgnoreTrapTime(float second)
        {
            if (m_IgnoreTrapTime < second)
            {
                m_IgnoreTrapTime = second;
            }
        }
    }
}
