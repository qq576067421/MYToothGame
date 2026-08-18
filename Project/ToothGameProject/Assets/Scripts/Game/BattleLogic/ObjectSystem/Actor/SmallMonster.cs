using GameDll;

namespace GameDll
{
    using GameDll;
    using LCL;
    using MonoBean;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using UnityEngine;

    //小怪
    public class SmallMonster : MoveableCreature
    {
        //移动到该单元格周围
        protected int m_BackRange = 5000;
        public void SetBackRange(int backRange)
        {
            m_BackRange = backRange;
        }
        public override long ReadBeanId()
        {
            return m_Bean.t_id;
        }
        public override bool ReadIsSmallMonster()
        {
            return true;
        }
        public override bool ReadIsBoss()
        {
            return false;
        }
        protected Vector3 m_BornPoint;
        public override void SetBornPosition(Vector3 pos)
        {
            m_BornPoint = pos;
        }
        public override Vector3 ReadBornPosition()
        {
            return m_BornPoint;
        }
        protected bool m_IsBackHome = false;

        //具有攻击性的
        protected bool m_Aggressive = false;
        public void SetAggressive(bool aggressive)
        {
            m_Aggressive = aggressive;
        }
        public bool GetAggressive()
        {
            return m_Aggressive;
        }
        public override void OnDead()
        {
            //BRenderEvent.Event.OnKillMonster(this.ReadId(), this.GetKillMeAttackId());


            //BattleManager.GetBattleTool().OnDeadDrop(this, m_DropBoxConfig);

            base.OnDead();
        }

        public override void Alive()
        {
            var maxHp = GetMaxHP();
            SetHpRuntime(maxHp);
            OnHpChanged();
            GetRender().SetShowHud(true);

            TryChangeState(emEntityState.em_EntityState_Idle, true);
            SetVisiable(true);

            base.Alive();
        }

        protected PropertyEntity m_Enemy;

        protected t_monsterBean m_Bean;
        public override float GetConfigMoveSpeed()
        {
            if (m_Bean == null || m_Bean.t_MoveSpeed == 0)
            {
                return base.GetConfigMoveSpeed();
            }
            else
            {
                return m_Bean.t_MoveSpeed / 1000.0f;
            }
        }
        //可以掉落到红色区域，该区域不可以行走，塔防里面用
        protected bool m_CanDropRedArea = false;
        public override void SetDropRedArea(bool red)
        {
            m_CanDropRedArea = red;
        }
        public override bool GetDropRedArea()
        {
            return m_CanDropRedArea;
        }
        private ReadOnlyCollection<long> m_DropBoxConfig = null;
        public void SetDropBoxConfig(ReadOnlyCollection<long> dropItems)
        {
            m_DropBoxConfig = dropItems;
        }

        public override float ReadRadius()
        {
            if (m_Bean == null || m_Bean.t_size == 0)
            {
                return 0.4f;
            }
            return m_Bean.t_size / 1000.0f;
        }

        public override void InitInstance()
        {
            base.InitInstance();
            InitProperties(0);

            m_IsCanItemChangeGroup = true;
        }
        public override void Update(float dt)
        {
            base.Update(dt);
        }
        public override bool UseItemChangeGroup(GroupId group)
        {
            if(m_IsCanItemChangeGroup == false)
            {
                return false;
            }
            ChangeGroup(group);
            return true;
        }

        protected override void InitState()
        {
            m_StateManager = new StateManager();
            m_StateManager.SetOwner(this);
            m_StateManager.AddState(emEntityState.em_EntityState_Idle, new State_Idle());
            m_StateManager.AddState(emEntityState.em_EntityState_Born, new State_Born());
            GetStateManager().AddState(emEntityState.em_EntityState_Move, new State_Move());
            GetStateManager().AddState(emEntityState.em_EntityState_Attack, new State_Attack());
            GetStateManager().AddState(emEntityState.em_EntityState_Dead, new State_Dead());
            GetRender().PlayAnimation("idle");
            TryChangeState(emEntityState.em_EntityState_Idle);
        }


        public override void CreateRender(UResource obj, ResourceType resourceType)
        {
            var res = UResourceFactory.New_EntityObject(resourceType, m_EntityType);
            res.SetId(ReadId());
            SetResource(res);
            res.LoadRender(m_Bean.t_model, Tool.GetAssetName(m_Bean.t_model));
            
        }
        protected override ReadOnlyCollection<ReadOnlyCollection<int>> GetFirePointPositionsCfgValues()
        {
            return m_Bean != null ? m_Bean.t_fire_positions : null;
        }
        protected override string GetFirePointNamesCfgValue()
        {
            return m_Bean != null ? m_Bean.t_fire_points : null;
        }
        protected override float GetHitPointCfgValue()
        {
            if (m_Bean != null && m_Bean.t_hit_point != 0)
            {
                return m_Bean.t_hit_point / 1000.0f;
            }
            return m_Bean.t_hit_point / 1000.0f;
        }

        public override void SetBean(object bean)
        {
            m_Bean = (t_monsterBean)bean;
            ResetFirePointCache();
        }
        public override void InitSkills()
        {
            m_SkillManager.BreakSkill();
            m_SkillManager.ClearWillNextUseSkill();
            m_SkillManager.ClearSkills();
            RegisterSkills();
        }
        public override void RegisterSkills()
        {
            long skillCfgId = ResolvePrimarySkillCfgId();
            if (skillCfgId <= 0)
            {
                return;
            }

            Skill skill = SkillTemplate.createSkill(skillCfgId);
            if (skill != null)
            {
                skill.InitActionData();
                var cfg = skill.GetSkillDescBean();
                skill.AddAction(cfg != null ? cfg.t_action : null);

                skill.SetLevel(ReadLevel());
                m_SkillManager.RegisterSkill(skill, this, 0);
                SetWarningDist(GetSkillCastDist(skill));
            }

        }

        private long ResolvePrimarySkillCfgId()
        {
            return m_Bean != null && m_Bean.t_skill_id != null && m_Bean.t_skill_id.Count > 0
                ? m_Bean.t_skill_id[0]
                : 0;
        }

        public override void RegisterOtherSkill(long skillId, int level, int slot)
        {
            Skill skill = SkillTemplate.createSkill(skillId);
            if (skill != null)
            {
                var oldSkill = m_SkillManager.ReadSkillBySlot(slot);
                if (oldSkill != null)
                {
                    m_SkillManager.RemoveSkill(oldSkill.ReadSkillCfgId());
                }
                //skill.InitActionData();
                //skill.AddAction(action);
                skill.SetLevel(level);
                m_SkillManager.RegisterSkill(skill, this, slot);
            }
        }
        //上次攻击我的目标的距离
        private PropertyEntity m_LastEnemy;
        public override void SetAttackMe(PropertyEntity attackMe)
        {
            base.SetAttackMe(attackMe);
            if (attackMe == null || attackMe.ReadIsDead())
            {
                return;
            }
            if (m_LastEnemy != null)
            {
                if (m_LastEnemy.ReadId() == attackMe.ReadId())
                {
                    return;
                }
                else
                {
                    var dist0 = Vector3.Distance(m_LastEnemy.GetPosition(), GetPosition());
                    var dist1 = Vector3.Distance(attackMe.GetPosition(), GetPosition());
                    if (dist0 > dist1)
                    {
                        m_LastEnemy = attackMe;
                    }
                }
            }

        }

        public override void OnHpChanged()
        {
            var hp = ReadHP();
            var maxHp = GetMaxHP();
            GetRender().SetHpValue((float)hp / (float)maxHp, 0.2f);
            RenderEvent.Event.OnBossHealthChanged(this);
            if (hp <= 0)
            {
                var current = GetStateManager().GetCurrentState().GetStateType();
                if (current != emEntityState.em_EntityState_Dead)
                {
                    var battle = BattleManager.GetBattle();
                    if (battle != null)
                    {
                        var kill_me_id = this.GetKillMeAttackId();
                        var kill_me_actor = BattleManager.GetObjectManager().ReadPropertyEntityById(kill_me_id);
                        long player_id = 0;
                        if (kill_me_actor != null)
                        {
                            player_id = kill_me_actor.ReadBattlePlayerId();
                        }
                        battle.GetBattleStat().OnMonsterDead(this.ReadId(), kill_me_id, player_id);
                    }
                    //BRenderEvent.Event.RemoveMiniMap(this.ReadId());
                    TryChangeState(emEntityState.em_EntityState_Dead);
                }
            }
        }

        public override void ResetRuntimeForReuse()
        {
            base.ResetRuntimeForReuse();
            m_Enemy = null;
            m_LastEnemy = null;
            m_IsBackHome = false;
        }
    }
}
