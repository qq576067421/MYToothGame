using MonoBean;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;
namespace GameDll
{


    //boss
    public class MasterHero : MoveableCreature
    {
        private int m_BackRange = 5000;
        private Vector3 m_SpawPosition;
        private t_monsterBean m_Bean;
        public override void SetBornPosition(Vector3 pos)
        {
            m_SpawPosition = pos;
        }
        public override bool ReadIsHero()
        {
            return false;
        }
        public override bool ReadIsSmallMonster()
        {
            return false;
        }
        public override bool ReadIsBoss()
        {
            return true;
        }
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
        public override float ReadRadius()
        {
            if (m_Bean.t_size == 0)
            {
                return 0.4f;
            }
            return m_Bean.t_size / 1000.0f;
        }
        public override long ReadBeanId()
        {
            return m_Bean.t_id;
        }
        public t_monsterBean GetMonsterBean()
        {
            return m_Bean;
        }
        public override void SetBean(object bean)
        {
            m_Bean = (t_monsterBean)bean;
            ResetFirePointCache();
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
            return m_Bean.t_hit_point / 1000.0f;
        }


        public override void InitInstance()
        {
            base.InitInstance();
            InitProperties(0);
        }

        protected override void InitState()
        {
            m_StateManager = new StateManager();
            m_StateManager.SetOwner(this);
            m_StateManager.AddState(emEntityState.em_EntityState_Idle, new State_Idle());

            GetStateManager().AddState(emEntityState.em_EntityState_Move, new State_Move());
            GetStateManager().AddState(emEntityState.em_EntityState_Attack, new State_Attack());
            GetStateManager().AddState(emEntityState.em_EntityState_Dead, new State_Dead());


            GetRender().PlayAnimation("idle");
            TryChangeState(emEntityState.em_EntityState_Idle);
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
            if (m_SkillManager.GetSkillCount() > 0)
            {
                Debug.LogWarning("重复注册技能");
            }
            int slot = 0;
            //添加普攻
            {
                var cfgId = ResolvePrimarySkillCfgId();
                Skill skill = SkillTemplate.createSkill(cfgId);
                if (skill != null)
                {
                    skill.InitActionData();
                    var cfg = skill.GetSkillDescBean();
                    skill.AddAction(cfg != null ? cfg.t_action : null);
                    skill.SetLevel(ReadLevel());
                    skill.SetEnableWarning(true);
                    m_SkillManager.RegisterSkill(skill, this, slot);
                    SetWarningDist(GetSkillCastDist(skill));
                    
                }
                slot++;
            }
            if (m_Bean.t_skill_id != null && m_Bean.t_skill_id.Count > 1)
            {
                var skill_strs = m_Bean.t_skill_id;
                for (int i = 1; i < skill_strs.Count; i++)
                {
                    var cfgId = skill_strs[i];
                    Skill skill = SkillTemplate.createSkill(cfgId);
                    if (skill != null)
                    {
                        skill.InitActionData();
                        var cfg = skill.GetSkillDescBean();
                        skill.AddAction(cfg != null ? cfg.t_action : null);
                        skill.SetLevel(ReadLevel());
                        skill.SetEnableWarning(true);
                        m_SkillManager.RegisterSkill(skill, this, slot);
                    }
                    slot++;

                }
            }
        }

        private long ResolvePrimarySkillCfgId()
        {
            return m_Bean != null && m_Bean.t_skill_id != null && m_Bean.t_skill_id.Count > 0
                ? m_Bean.t_skill_id[0]
                : 0;
        }


        public override bool IsReceiveExp()
        {
            return true;
        }




        public override void Update(float dt)
        {
            base.Update(dt);

        }
        public override void OnHpChanged()
        {
            var hp = ReadHP();
            var maxHp = GetMaxHP();
            GetRender().SetHpValue((float)hp / (float)maxHp, 1);
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
                        battle.GetBattleStat().OnBossDead(this.ReadId(), kill_me_id, player_id);
                    }
                    //BRenderEvent.Event.RemoveMiniMap(this.ReadId());

                    TryChangeState(emEntityState.em_EntityState_Dead);
                }
            }
        }
    }
}
