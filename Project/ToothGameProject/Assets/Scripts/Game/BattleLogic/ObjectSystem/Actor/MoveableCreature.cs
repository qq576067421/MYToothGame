namespace GameDll
{
    using GameDll;
    using LCL;
    using MonoBean;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    //删除注释的正则表达式(不包含前面两个斜杠)
    //^[ \t]*//[^\n]*\n


    /**
	    角色系列的基类，会继承出 PlayerOther,PlayerMySelf, PlayerNPC等
    */




    public enum RC_RESULT
    {
        RC_OK = 0,

        RC_ERROR,

        RC_SKIP,

        RC_WAIT, // 等待
    }

    public enum AttackFailedReason
    {
        Success = -1,

        Silence,

        UnregisterSkill,

        SystemError,

        NoEnoughMP,

        NotCoolDown,

        WrongAction,

        StateError
    }

    public enum AIState
    {
        NotInit,

        Pause,

        Playing,

        Stop,
    }

    public enum AttackFinishReason
    {
        Success = -1,
        Break = 0,
    }

    public enum ActorJob
    {
        Shooter = 0,
        Sword = 1,
        Tank = 2
    }

    public class MoveableCreature : PropertyEntity
    {
        //超出警戒范围是否追击
        protected bool m_CanWarningFollow = true;
        public void SetCanWarningFollow(bool follow)
        {
            m_CanWarningFollow = follow;
        }
        private bool m_CanPassWall = false;
        public void SetCanPassWall(bool pass)
        {
            m_CanPassWall = pass;
        }
        public bool IsCanPassWall()
        {
            return m_CanPassWall;
        }
        protected StateManager m_StateManager = null;
        private bool m_WarningDistInitialized = false;
        public override void SetForward(Vector3 forward)
        {
            base.SetForward(forward);
        }

        public override void SetPosition(Vector3 position)
        {
            base.SetPosition(position);
            //EnterGrid(position);

            StandOnFloor();
            

            position = GetPosition();
            if (position == m_LastMovePosition)
            {
                m_IsMoveSamePosition = true;
            }
            else
            {
                m_IsMoveSamePosition = false;
                m_LastMovePosition = position;
            }
        }
        public override void Destroy()
        {
            //这个地方需要特别注意防止两次离开grid
            //LeaveGrid();
            //var gridManager = BattleManager.GetGridManager();
            //if (gridManager != null)
            //{
            //    gridManager.UnRegister(this);
            //}
            if(m_StateManager != null)
            {
                m_StateManager.Destroy();
                m_StateManager = null;
            }

            base.Destroy();
        }

       
        public override StateManager GetStateManager()
        {
            return m_StateManager;
        }
        public override void InitInstance()
        {
            base.InitInstance();

            InitState();
        }
        public override bool ReadIsDead()
        {
            var hp = GetHpRuntime();
            if(hp <= 0)
            {
                return true;
            }
            var stateMgr = GetStateManager();
            if(stateMgr == null)
            {
                return false;
            }
            if(stateMgr.ReadIsState( emEntityState.em_EntityState_Dead))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual void OnAttacked()
        {
            if (ReadIsDead())
            {
                return;
            }
        }

        protected bool m_IsMoveSamePosition;
        protected Vector3 m_LastMovePosition;
        public virtual  bool IsMoveSamePosition()
        {
            return m_IsMoveSamePosition;
        }


        public override bool ReadIsBeingControlled()
        {
            return false;
        }
        private float m_LastAttackRangeTime = 0;


        public override bool AttackRange(PropertyEntity defender, bool is_far_move = true)
        {
            if(!BattleManager.ReadIsEntityValide(defender) || 
                defender.ReadGroup() == this.ReadGroup() ||
                !defender.ReadCanBeTarget())
            {
                this.SetDefender(null);
                return false;
            }

            var time = BattleManager.ReadBattleTime();
            if (m_LastAttackRangeTime == 0)
            {
                m_LastAttackRangeTime = time + UnityEngine.Random.Range(1.0f, 2.0f);
            }
            else if (time >= m_LastAttackRangeTime)
            {
                m_LastAttackRangeTime = time + UnityEngine.Random.Range(1.0f, 2.0f);
            }
            else
            {
                return TryNormalAttack(is_far_move, defender);
            }


            var attacker_pos = this.GetPosition();
            var defender_pos = defender.GetPosition();
            var dist = Vector3.Distance(attacker_pos, defender_pos);

            var warning_dist = this.ReadWarningDist();

            if (dist < warning_dist)
            {
                var skillMgr = this.GetSkillManager();
                List<Skill> cooledSkills = null;
                Skill use_skill = null;
                bool is_far = false;
                if (skillMgr != null)
                {
                    cooledSkills = new List<Skill>();

                    var _skill_count = skillMgr.GetSkillCount();
                    for (int i = 0; i < _skill_count; ++i)
                    {
                        if(ReadIsHero())
                        {
                            continue;
                        }
                        var cooledSkill = skillMgr.ReadSkillBySlot(i);
                        if (cooledSkill != null && cooledSkill.ReadIsCooldown())
                        {
                            cooledSkills.Add(cooledSkill);
                        }
                    }
                }
                if (cooledSkills != null && cooledSkills.Count > 0)
                {
                    int cooled_skill_count = cooledSkills.Count;
                    if (cooled_skill_count == 1)
                    {
                        var skill = cooledSkills[0];
                        if (GetSkillCastDist(skill) >= dist)
                        {
                            use_skill = skill;
                        }
                        else
                        {
                            is_far = true;
                        }
                    }
                    else
                    {
                        bool hasNormalSkill = false;
                        List<Skill> r_skills = new List<Skill>();
                        for (int i = 0; i < cooled_skill_count; ++i)
                        {
                            var skill = cooledSkills[i];
                            if (GetSkillCastDist(skill) >= dist)
                            {
                                if(!hasNormalSkill)
                                {
                                    if(skill.ReadSlot() == 0)
                                    {
                                        hasNormalSkill = true;
                                    }

                                }
                                r_skills.Add(skill);
                            }
                        }
                        if (r_skills.Count == 0)
                        {
                            is_far = true;
                        }
                        else
                        {
                            if (r_skills.Count == 1)
                            {
                                use_skill = r_skills[0];
                            }
                            else
                            {
                                bool useNormalSkill = false;
                                if (hasNormalSkill)
                                {
                                    var useNoramlRad = UnityEngine.Random.Range(0, 100);
                                    if(useNoramlRad <= 20)
                                    {
                                        use_skill = ReadNormalSkill();
                                        useNormalSkill = true;
                                    }
                                }

                                if (!useNormalSkill)
                                {
                                    int r_skill_count = r_skills.Count;
                                    var idx = UnityEngine.Random.Range(0, r_skill_count - 1);
                                    use_skill = r_skills[idx];
                                }
                            }
                        }
                    }
                    if (use_skill != null)
                    {
                        BattleManager.Attack(this, defender, use_skill);
                        return true;
                    }
                    else
                    {
                        if (is_far_move)
                        {
       
                        }
                        return false;
                    }
                }
                else
                {
                    var normalSkill = this.ReadNormalSkill();
                    if (is_far_move && dist> GetSkillCastDist(normalSkill))
                    {

                    }
                    else
                    {
                        var stateType = this.GetStateManager().GetCurrentStateType();
                        if (stateType == emEntityState.em_EntityState_Move)
                        {
                            this.Stop();
                            this.TryChangeState(emEntityState.em_EntityState_Idle);
                        }
                    }
                    return false;
                }
            }
            else if(dist > warning_dist * 1.4f && !m_CanWarningFollow)
            {
                SetDefender(null);
                return false;
            }
            else
            {
                if (is_far_move)
                {
  
                }
                return false;
            }
        }

        private bool TryNormalAttack(bool is_far_move, PropertyEntity defender)
        {
            var skill = ReadNormalSkill();
            if(skill == null)
            {
                return false;
            }
            if(!skill.ReadIsCooldown())
            {
                return false;
            }
            var attacker_pos = this.GetPosition();
            var defender_pos = defender.GetPosition();
            var dist = Vector3.Distance(attacker_pos, defender_pos);
            var warning_dist = this.ReadWarningDist();
            if (dist < warning_dist)
            {
                if(GetSkillCastDist(skill) >= dist)
                {
                    BattleManager.Attack(this, defender, skill);
                    return true;
                }
                else
                {
                    if (is_far_move)
                    {
                        
                    }
                    return false;
                }
            }
            else if (dist > warning_dist * 1.4f && !m_CanWarningFollow)
            {
                SetDefender(null);
                return false;
            }
            else
            {
                if (is_far_move)
                {
                    
                }
                return false;
            }
        }

        

        protected override void BeforeUpdateCheckData()
        {
            base.BeforeUpdateCheckData();
            CheckTarget();
            CheckAttackMe();
        }
        public override void Update(float dt)
        {
            if (ReadIsFreeze())
            {
                return;
            }
            BeforeUpdateCheckData();
            base.Update(dt);
            OnUpdateNextDir();



            if (m_StateManager != null)
            {
                m_StateManager.UpdateFSM(dt);
            }
        }


        public override bool CanMove()
        {
            if (ReadIsFreeze())
            {
                return false;
            }
            if (ReadIsDead())
            {
                return false;
            }
            if (ReadIsBeingControlled())
            {
                return false;
            }

            var skillMgr = GetSkillManager();
            if (skillMgr != null)
            {
                var curSkill = skillMgr.GetCurrentSkill();
                if (curSkill != null)
                {
                    if ( !curSkill.IsSkillCanBreakStatus())
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        protected virtual void InitState()
        {
            m_StateManager = new StateManager();
            m_StateManager.SetOwner(this);
            m_StateManager.AddState(emEntityState.em_EntityState_Idle, new State_Idle());

            GetRender().PlayAnimation("idle");
            TryChangeState(emEntityState.em_EntityState_Idle);

        }
        public override bool Move(int x, int z)
        {
            if (!CanMove())
            {
                return false;
            }

            var stateMgr = GetStateManager();
            if (stateMgr == null)
            {
                return false;
            }

            var move = (State_Move)stateMgr.GetState(emEntityState.em_EntityState_Move);
            if (move == null)
            {
                Debug.LogError($"{GetType().Name} 缺少 Move 状态注册，entityId={ReadId()}。请在 InitState 中注册 State_Move。");
                return false;
            }

            move.SetForward(x, z);
            var skillMgr = GetSkillManager();
            var currentSkill = skillMgr.GetCurrentSkill();
            if(currentSkill != null )
            {
                return false;
            }

            if (x == 0 && z == 0)
            {
                Stop();
                TryChangeState(emEntityState.em_EntityState_Idle);
                return true;
            }

            TryChangeState(emEntityState.em_EntityState_Move);
            return true;
        }
        public override void Stop()
        {
            base.Stop();
            //var pathMoveAgent = GetPathMoveAgent();
            //if (pathMoveAgent != null)
            //{
            //    pathMoveAgent.ClearPath();
            //}
            //var moveAgent = GetMoveAgent();
            //if (moveAgent != null)
            //{
            //    moveAgent.Stop();
            //}
        }
        public Action<MoveableCreature> OnDrawGizmosCall;
        public override void OnDrawGizmos()
        {
            if(OnDrawGizmosCall != null)
            {
                OnDrawGizmosCall(this);
            }
        }
        #region 属性
        //死亡的阶段：血量为0，进入死亡状态，死亡动作时间，广播死亡事件，从管理器移除实体
        public override void OnHpChanged()
        {
            var hp = ReadHP();
            var maxHp = GetMaxHP();
            GetRender().SetHpValue((float)hp / (float)maxHp, 1);
            if (hp <= 0)
            {
                var current = GetStateManager().GetCurrentState().GetStateType();
                if (current != emEntityState.em_EntityState_Dead)
                {
                    RenderEvent.Event.RemoveMiniMap(this.ReadId());
                    TryChangeState(emEntityState.em_EntityState_Dead);
                }
            }
        }


        protected int m_AngularSpeed = 720;
        public override int GetAngularSpeed()
        {
            return m_AngularSpeed;
        }
        
        public override void SetAngularSpeed(int speed)
        {
            m_AngularSpeed = speed;
        }




        #endregion

        public override bool ReadIsMoveableCreature()
        {
            return true;
        }

        public override float GetMoveSpeed()
        {
            var baseSpeed = GetConfigMoveSpeed();
            if (m_BuffManager != null)
            {
                baseSpeed += m_BuffManager.ReadBuffPropertyData((int)PropertyType.move_speed);
            }
            return Mathf.Max(0.01f, baseSpeed);
        }

        protected Vector3 m_NextDir;
        protected bool m_HasNextDir;
        public void SetNextDir(Vector3 forward)
        {
            m_NextDir = forward;
            m_HasNextDir = true;
        }
        protected void ApplyNextDir()
        {
            SetForward(m_NextDir);
        }
        public void ClearNextDir()
        {
            m_HasNextDir = false;
        }

        protected void OnUpdateNextDir()
        {
            if(!m_HasNextDir)
            {
                return;
            }
            if(!BattleManager.ReadIsEntityValide(this))
            {
                return;
            }

            var curSkill = this.ReadCurrentSkill();
            if (curSkill != null)
            {
                return;
            }
            else
            {
                ApplyNextDir();
                ClearNextDir();
            }

        }

        public virtual void Alive()
        {

        }

        public override void ResetRuntimeForReuse()
        {
            base.ResetRuntimeForReuse();
            Stop();
            m_LastAttackRangeTime = 0;
            m_IsMoveSamePosition = false;
            m_LastMovePosition = Vector3.zero;
            m_HasNextDir = false;
            m_NextDir = Vector3.zero;
            TryChangeState(emEntityState.em_EntityState_Idle, true);
        }
    }
}
