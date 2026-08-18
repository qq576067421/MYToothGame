using GameDll;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace GameDll
{
    public  class PlayerHero : MoveableCreature
    {
        protected t_modelBean m_ModelBean;
        private readonly List<long> m_AppliedRoleRuntimeBuffCfgIds = new List<long>();
        public override bool ReadIsHero()
        {
            return true;
        }

        public override bool ReadShouldSyncFacingOnAttack()
        {
            return false;
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



        public override void Alive()
        {
            var maxHp = GetMaxHP();
            SetHpRuntime(maxHp);
            OnHpChanged();


            SetVisiable(true);
            TryChangeState(emEntityState.em_EntityState_Idle, true);

            //BattleManager.GetBattleEvent().OnHeroAlive(this);

            RenderEvent.Event.AddMiniMap(this.ReadId());

            base.Alive();
        }

        private float m_AliveAtTime;
        public override void OnDead()
        {

            base.OnDead();


            m_AliveAtTime = BattleManager.ReadBattleTime() + ReadAliveTotalTime();


            //BattleManager.GetBattleEvent().OnHeroDead(this);
            RenderEvent.Event.OnHeroDead(this.ReadId(), GetKillMeAttackId());
        }
        public override float GetConfigMoveSpeed()
        {
            if(m_ModelBean == null || m_ModelBean.t_move_speed == 0)
            {
                return base.GetConfigMoveSpeed();
            }
            else
            {
                return m_ModelBean.t_move_speed / 1000.0f;
            }
        }


        public override float ReadRadius()
        {
            if (m_ModelBean.t_size == 0)
            {
                return 0.4f;
            }
            return m_ModelBean.t_size / 1000.0f / 2.0f;
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
        public override void Destroy()
        {
            base.Destroy();
        }
        
        public override void InitInstance()
        {
            base.InitInstance();
        }



        public override void Update(float dt)
        {
            base.Update(dt);
        }



        protected t_heroBean m_Bean;
        public override long ReadBeanId()
        {
            return m_Bean.t_id;
        }
        public t_heroBean GetHeroBean()
        {
            return m_Bean;
        }
        public override void SetBean(object bean)
        {
            m_Bean = (t_heroBean)bean;
            m_ModelBean = t_modelBean.GetConfig(m_Bean.t_model);
            ResetFirePointCache();
        }
        public override void CreateRender(UResource obj, ResourceType resourceType)
        {
            var renderType = ResourceType.PlayerActor;
            var res = UResourceFactory.New_EntityObject(renderType, m_EntityType);
            res.SetId(ReadId());
            SetResource(res);
            res.LoadRender(m_ModelBean.t_model_res, Tool.GetAssetName(m_ModelBean.t_model_res));
            res.AddLoadedCall(() =>
            {
                var go = res.GetShowObj() as GameObject;
                if (go != null)
                {
                    var panel = go.AddComponent<HeroAttributePanel>();
                    panel.Init(this);
                }
            });
        }
        protected override ReadOnlyCollection<ReadOnlyCollection<int>> GetFirePointPositionsCfgValues()
        {
            return m_ModelBean != null ? m_ModelBean.t_fire_positions : null;
        }
        protected override string GetFirePointNamesCfgValue()
        {
            return m_ModelBean != null ? m_ModelBean.t_fire_points : null;
        }
        protected override float GetHitPointCfgValue()
        {
            return m_ModelBean.t_hit_point / 1000.0f;
        }
        public override bool IsReceiveGold()
        {
            return true;
        }

        private int m_SkillPoint = 0;
        private int m_TalentPoint = 0;
       
        public int ReadSkillPoint()
        {
            return m_SkillPoint;
        }
        public void SetSkillPoint(int point)
        {
            m_SkillPoint = point;
        }
        public void SetTalentPoint(int point)
        {
            m_TalentPoint = point;
        }
        public void InitHPPercent(int percent)
        {
            var max_hp = GetMaxHP();
            var hp = max_hp * percent / 10000;
            SetHpRuntime(hp);
            OnHpChanged();
        }
      
        public override void InitSkills()
        {
            m_SkillManager.Init();
            RegisterSkills();
        }

        public override void RegisterSkills()
        {
            if (m_SkillManager.GetSkillCount() > 0)
            {
                Debug.LogWarning("重复注册技能");
            }
            m_SkillManager.ClearSkills();
            ApplyRoleForCurrentLevel();
        }
        public override void RegisterSkill(long skillId, int level, int slot)
        {
            Skill skill = SkillTemplate.createSkill(skillId);
            if (skill != null)
            {
                var oldSkill = m_SkillManager.ReadSkillBySlot(slot);
                if (oldSkill != null)
                {
                    m_SkillManager.RemoveSkill(oldSkill.ReadSkillCfgId());
                }
                skill.InitActionData();
                var cfg = skill.GetSkillDescBean();
                skill.AddAction(cfg != null ? cfg.t_action : null);
                skill.SetLevel(level);
                m_SkillManager.RegisterSkill(skill, this, slot);
                skill.SetCooldownReady();
            }
        }
        public override void ReplaceSkill(long skillId, int level, int slot)
        {
            Skill skill = SkillTemplate.createSkill(skillId);
            if (skill != null)
            {
                var oldSkill = m_SkillManager.ReadSkillBySlot(slot);
                if (oldSkill != null)
                {
                    m_SkillManager.RemoveSkill(oldSkill.ReadSkillCfgId());
                }
                skill.InitActionData();
                var cfg = skill.GetSkillDescBean();
                skill.AddAction(cfg != null ? cfg.t_action : null);
                skill.SetLevel(level);
                m_SkillManager.RegisterSkill(skill, this, slot);
                skill.SetCooldownReady();
            }
        }
        public override bool AttackRange(PropertyEntity defender, bool is_far_move = true)
        {
            return base.AttackRange(defender, is_far_move);
        }

        public bool TryTowerDefendNormalAttack(PropertyEntity defender, bool isFarMove = true)
        {
            if (defender == null)
            {
                return false;
            }

            return AttackRange(defender, isFarMove);
        }

        public override float GetNormalAtkSpeed()
        {
            return base.GetNormalAtkSpeed();
        }


        public override void InitLevel(int level)
        {
            base.InitLevel(level);
            if (m_SkillManager != null && m_SkillManager.GetSkillCount() > 0)
            {
                ApplyRoleForCurrentLevel();
            }
        }
        protected override void OnLevelUp()
        {
            base.OnLevelUp();
            ApplyRoleForCurrentLevel();
        }

        private void SyncSkillLevelsByHeroLevel()
        {
            var skillMgr = GetSkillManager();
            if (skillMgr == null)
            {
                return;
            }

            var skills = skillMgr.ReadSkills();
            if (skills == null)
            {
                return;
            }

            var level = ReadLevel();
            int count = skills.Count;
            for (int i = 0; i < count; i++)
            {
                var skill = skills[i];
                if (skill != null)
                {
                    skill.SetLevel(level);
                }
            }
        }

        private void RefreshWarningDistance()
        {
            float warningDist = 0;

            var normalSkill = ReadNormalSkill();
            if (normalSkill != null)
            {
                warningDist = Mathf.Max(warningDist, GetSkillCastDist(normalSkill));
            }

            var autoSkill = ReadTowerDefendAutoSkill();
            if (autoSkill != null)
            {
                warningDist = Mathf.Max(warningDist, GetSkillCastDist(autoSkill));
            }

            if (warningDist > 0)
            {
                SetWarningDist(warningDist);
            }
        }

        private void ApplyRoleForCurrentLevel()
        {
            var snapshot = ResolveRoleSnapshot();
            UpsertSkillForSlot(snapshot.m_NormalSkillCfgId, 0);
            SyncActiveSkills(snapshot.m_ActiveSkillCfgIds);
            SyncAutoSkill(snapshot);
            RemoveObsoleteRoleSkills(snapshot);
            SyncSkillLevelsByHeroLevel();
            ApplyRoleRuntimeBuffs(snapshot);
            RefreshRoleBaseProperties();
            RefreshWarningDistance();
        }

        private TowerDefendRoleSnapshot ResolveRoleSnapshot()
        {
            if (m_Bean == null)
            {
                throw new InvalidOperationException($"PlayerHero 缺少角色配置，entityId={ReadId()}。");
            }

            return TowerDefendRoleResolver.Resolve(m_Bean.t_id, Mathf.Max(1, (int)ReadLevel()));
        }

        public int ReadTowerDefendAutoSkillSlot()
        {
            if (m_Bean == null)
            {
                return -1;
            }

            var snapshot = ResolveRoleSnapshot();
            if (snapshot == null || snapshot.m_AutoSkillCfgId <= 0)
            {
                return -1;
            }

            return snapshot.m_ActiveSkillCfgIds.Count + 1;
        }

        public Skill ReadTowerDefendAutoSkill()
        {
            int autoSkillSlot = ReadTowerDefendAutoSkillSlot();
            if (autoSkillSlot <= 0)
            {
                return null;
            }

            var skillMgr = GetSkillManager();
            return skillMgr != null ? skillMgr.ReadSkillBySlot(autoSkillSlot) : null;
        }

        private void SyncActiveSkills(IList<long> activeSkillCfgIds)
        {
            if (activeSkillCfgIds == null || activeSkillCfgIds.Count <= 0)
            {
                throw new InvalidOperationException($"塔防角色配置缺少有效主动技能，roleCfgId={m_Bean?.t_id ?? 0}。");
            }

            int activeSkillCount = activeSkillCfgIds.Count;
            for (int i = 0; i < activeSkillCount; i++)
            {
                UpsertSkillForSlot(activeSkillCfgIds[i], i + 1);
            }
        }

        private void SyncAutoSkill(TowerDefendRoleSnapshot snapshot)
        {
            if (snapshot == null || snapshot.m_AutoSkillCfgId <= 0)
            {
                return;
            }

            int autoSkillSlot = snapshot.m_ActiveSkillCfgIds.Count + 1;
            UpsertSkillForSlot(snapshot.m_AutoSkillCfgId, autoSkillSlot);
        }

        private void UpsertSkillForSlot(long skillCfgId, int slot)
        {
            if (skillCfgId <= 0)
            {
                throw new InvalidOperationException($"塔防角色配置缺少有效技能配置，slot={slot}，roleCfgId={m_Bean?.t_id ?? 0}。");
            }

            var currentSkill = GetSkillManager().ReadSkillBySlot(slot);
            if (currentSkill == null)
            {
                RegisterSkill(skillCfgId, (int)ReadLevel(), slot);
                return;
            }

            if (currentSkill.ReadSkillCfgId() == skillCfgId)
            {
                currentSkill.SetLevel(ReadLevel());
                return;
            }

            ReplaceSkill(skillCfgId, (int)ReadLevel(), slot);
        }

        private void RemoveObsoleteRoleSkills(TowerDefendRoleSnapshot snapshot)
        {
            var skillMgr = GetSkillManager();
            if (skillMgr == null)
            {
                return;
            }

            var skills = skillMgr.ReadSkills();
            if (skills == null || skills.Count <= 0)
            {
                return;
            }

            var validSlots = new HashSet<int> { 0 };
            int activeSkillCount = snapshot != null && snapshot.m_ActiveSkillCfgIds != null
                ? snapshot.m_ActiveSkillCfgIds.Count
                : 0;
            for (int i = 0; i < activeSkillCount; i++)
            {
                validSlots.Add(i + 1);
            }

            if (snapshot != null && snapshot.m_AutoSkillCfgId > 0)
            {
                validSlots.Add(activeSkillCount + 1);
            }

            var removeSkillCfgIds = new List<long>();
            int skillCount = skills.Count;
            for (int i = 0; i < skillCount; i++)
            {
                var skill = skills[i];
                if (skill == null)
                {
                    continue;
                }

                int slot = skill.ReadSlot();
                if (!validSlots.Contains(slot))
                {
                    removeSkillCfgIds.Add(skill.ReadSkillCfgId());
                }
            }

            int removeCount = removeSkillCfgIds.Count;
            for (int i = 0; i < removeCount; i++)
            {
                skillMgr.RemoveSkill(removeSkillCfgIds[i]);
            }
        }

        private void ApplyRoleRuntimeBuffs(TowerDefendRoleSnapshot snapshot)
        {
            var buffMgr = GetBuffManager();
            if (buffMgr == null)
            {
                return;
            }

            for (int i = 0; i < m_AppliedRoleRuntimeBuffCfgIds.Count; i++)
            {
                buffMgr.RemoveBuff(m_AppliedRoleRuntimeBuffCfgIds[i]);
            }

            m_AppliedRoleRuntimeBuffCfgIds.Clear();
            if (snapshot == null || snapshot.m_RuntimeBuffCfgIds == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.m_RuntimeBuffCfgIds.Count; i++)
            {
                var buffCfgId = snapshot.m_RuntimeBuffCfgIds[i];
                if (buffCfgId <= 0)
                {
                    continue;
                }

                var buff = buffMgr.TryAddBuff(buffCfgId, this, this, null, null, ReadLevel());
                if (buff == null)
                {
                    Debug.LogWarning(string.Format(
                        "[升级] 角色升至 {0} 级时跳过无效 buff {1}",
                        ReadLevel(),
                        buffCfgId));
                    continue;
                }

                m_AppliedRoleRuntimeBuffCfgIds.Add(buffCfgId);
                Debug.Log(string.Format("[升级] 角色升至 {0} 级，应用 buff {1}", ReadLevel(), buffCfgId));
            }
        }

        private void RefreshRoleBaseProperties()
        {
            SetBaseHppublic(1);
            SetMagicRuntime(0);
            InitHp();
        }
    }
}
