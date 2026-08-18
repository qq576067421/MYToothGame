using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameDll
{
    public class SkillManager
    {
        private List<Skill> m_Skills = new List<Skill>();

        private Skill m_WillNextUseSkill;
        private PropertyEntity m_WillNextUseSkillDefender;
        public void SetWillNextUseSkill(Skill skill, PropertyEntity defender)
        {
            m_WillNextUseSkill = skill;
            m_WillNextUseSkillDefender = defender;
        }
        public void ClearWillNextUseSkill()
        {
            m_WillNextUseSkill = null;
            m_WillNextUseSkillDefender = null;
        }
        public void ClearWillNextUseNormalSkill()
        {
            if(m_WillNextUseSkill == null)
            {
                return;
            }
            if(m_WillNextUseSkill.ReadSlot() == 0)
            {
                m_WillNextUseSkill = null;
            }
        }

        public bool TryWillNextUseSkill()
        {
            if(m_WillNextUseSkill == null)
            {
                return false;
            }
            var owner = m_WillNextUseSkill.ReadAttacker();
            if (owner == null || !BattleManager.ReadIsEntityValide(owner) || !BattleManager.ReadIsEntityValide(m_WillNextUseSkillDefender))
            {
                m_WillNextUseSkill = null;
                m_WillNextUseSkillDefender = null;
                return false;
            }
            if(!BattleManager.IsCanUseSkillDirect(owner, m_WillNextUseSkill.ReadSlot()))
            {
                return false;
            }
            var skill = m_WillNextUseSkill;
            var defender = m_WillNextUseSkillDefender;
            m_WillNextUseSkill = null;
            m_WillNextUseSkillDefender = null;
            if (defender == null || !BattleManager.ReadIsEntityValide(defender))
            {
                return false;
            }

            var ownerPos = owner.GetPosition();
            var defenderPos = defender.GetPosition();
            var dir = defenderPos - ownerPos;
            if (dir.sqrMagnitude <= 0.0001f)
            {
                dir = owner.ReadForward();
            }
            if (dir.sqrMagnitude <= 0.0001f)
            {
                dir = Vector3.forward;
            }
            dir.Normalize();

            owner.SetDefender(defender);
            owner.TryChangeState(emEntityState.em_EntityState_Attack);
            owner.Attack(skill, dir, defenderPos, defender);
            return true;
        }


        //线性叠加，数值和千分比的数值都是，在使用的地方自行处理1000的关系
        public float ReadSkillProperty(int type)
        {
            float data = 0;

            foreach (var buff in m_Skills)
            {
                var temp = buff.GetPropertyData().ReadProperty(type);
                data += temp;
            }
            return data;
        }
        //非线性叠加
        public float ReadSkillPropertyV(int type)
        {
            float data = 1.0f;
            foreach (var buff in m_Skills)
            {
                var temp = buff.GetPropertyData().ReadPropertyV(type);
                data *= temp;
            }
            return data;
        }
        private Skill m_CurrentSkill = null;

        public void Init()
        {
            m_Skills.Clear();
        }
        public  void UpdateRender()
        {
            if (m_CurrentSkill != null)
            {
                m_CurrentSkill.UpdateRender();
            }
        }
        public void Update(float dt)
        {
            if(m_CurrentSkill!= null)
            {
                SkillCastStatus progess = m_CurrentSkill.GetCastStatus();
                if( SkillCastStatus.end == progess)
                {
                    var attacker = m_CurrentSkill.ReadAttacker();
                    if (attacker != null && attacker.ReadHP() > 0)
                    {
                        var dead_state = attacker.GetStateManager().ReadIsState(emEntityState.em_EntityState_Dead);
                        var hp = attacker.ReadHP();
                        if (hp <= 0 || dead_state)
                        {
                            m_CurrentSkill = null;
                            return;
                        }
                        //Debug.Log("技能结束，重置到Idle" + Time.realtimeSinceStartup);
                        attacker.AttackFinish(AttackFinishReason.Success);
                    }
                    else
                    {
                        m_CurrentSkill = null;
                    }

                    //Debug.Log("技能结束时间点：" + Time.realtimeSinceStartup);
                }
                else
                {
                    var attacker = m_CurrentSkill.ReadAttacker();
                    if (attacker != null)
                    {
                        m_CurrentSkill.Update(dt);
                    }
                    else
                    {
                        m_CurrentSkill = null;
                    }
                }
            }
            else
            {
                //当前没有技能
                TryWillNextUseSkill();
            }
            
            int count = m_Skills.Count;
            m_UpdateSkills.Clear();
            for (int i = 0; i < count; ++i)
            {
                m_UpdateSkills.Add(m_Skills[i]);
            }

            for (int i = 0; i < count; ++i)
            {
                var skill = m_UpdateSkills[i];
                skill.Update(dt);
            }
        }
        private List<Skill> m_UpdateSkills = new List<Skill>();

        public void BreakSkill()
        {
            ClearWillNextUseSkill();
            if (m_CurrentSkill != null)
            {
                m_CurrentSkill.Stop();
                m_CurrentSkill = null;
            }

        }
        public bool GetAllCoolDownSkill(List<Skill> skills)
        {
            if(skills == null)
            {
                return false;
            }
            skills.Clear();
            foreach(var skill in m_Skills)
            {
                if (skill != null && skill.ReadIsCooldown())
                {
                    skills.Add(skill);
                }
            }
            return skills.Count > 0;
        }
        public bool GetAllCoolDownSkillInRange(List<Skill> skills, int cast)
        {
            if (skills == null)
            {
                return false;
            }
            skills.Clear();
            foreach (var skill in m_Skills)
            {
                if(skill == null)
                {
                    continue;
                }
                var attacker = skill.ReadAttacker();
                if(attacker == null)
                {
                    continue;
                }
                var cast_dist = attacker.GetSkillCastDist(skill);
                if (skill.ReadIsCooldown() && cast <=  cast_dist)
                {
                    skills.Add(skill);
                }

            }
            return skills.Count > 0;
        }
        public void RegisterSkill(Skill skill, PropertyEntity actor, int slot)
        {
            if (skill != null)
            {
                skill.OnSkillRegister(actor, slot);
                long skillCfgId = skill.ReadSkillCfgId();
                m_Skills.Add(skill);
            }
            else
            {
                Debug.LogWarning("注册了不存在的技能!");
            }
        }

        public void AddOrChangeSkill(long skillId, PropertyEntity actor, int slot, int level)
        {
            var skill = SkillTemplate.createSkill(skillId);
            if (skill != null)
            {
                skill.OnSkillRegister(actor, slot);
                long skillCfgId = skill.ReadSkillCfgId();
                if(m_Skills.Count > slot)
                {
                    var remove_skill = m_Skills[slot];

                    skill.SetActionFrom(remove_skill);

                    remove_skill.OnSkillUnregister();
                    remove_skill.Destroy();

                    m_Skills[slot] = skill;
                }
                else
                {
                    skill.InitActionData();
                    var cfg = skill.GetSkillDescBean();
                    skill.AddAction(cfg != null ? cfg.t_action : null);

                    m_Skills.Add(skill);
                }
                skill.SetLevel(level);
            }
            else
            {
                Debug.LogWarning("注册了不存在的技能!");
            }
        }

        public void EnterSkill(Skill skill)
        {
            if (skill == null)
            {
                return;
            }

            m_CurrentSkill = skill;

            m_CurrentSkill.OnEnter();

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle != null)
            {
                battle.OnPlayerSkillCast(skill.ReadAttacker(), skill);
            }
        }
        public void LeaveSkill()
        {
            if (m_CurrentSkill == null)
            {
                return;
            }

            m_CurrentSkill.Stop();
            m_CurrentSkill = null;
        }
        public Skill GetCurrentSkill()
        {
            return m_CurrentSkill;
        }

        public int GetSkillCount()
        {
            return m_Skills.Count;
        }
        public void RemoveSkill(long skillCfgId)
        {
            
            Skill skill = null;
            int count = m_Skills.Count;
            for(int i = 0; i < count; ++i)
            {
                var temp = m_Skills[i];
                if(temp != null && temp.ReadSkillCfgId() == skillCfgId)
                {
                    skill = temp;
                    m_Skills.RemoveAt(i);
                    break;
                }
            }
            if (skill != null)
            {
                skill.OnSkillUnregister();
                skill.Destroy();
            }
        }
        public void RemoveSkillBySlot(int slot)
        {
            Skill skill = null;
            int count = m_Skills.Count;
            if(slot >= 0 && m_Skills.Count > slot)
            {
                skill = m_Skills[slot];
                m_Skills.RemoveAt(slot);
                if (skill != null)
                {
                    skill.OnSkillUnregister();
                    skill.Destroy();
                }
            }    
        }
        public Skill ReadSkillById(long skillCfgId)
        {
            Skill skill = null;
            int count = m_Skills.Count;
            for (int i = 0; i < count; ++i)
            {
                var temp = m_Skills[i];
                if (temp != null && temp.ReadSkillCfgId() == skillCfgId)
                {
                    skill = temp;
                    break;
                }
            }
            return skill;
        }

        //设计上假设按照list序号的
        public Skill ReadSkillBySlot(int slot)
        {
            int count = m_Skills.Count;
            for (int i = 0; i < count; ++i)
            {
                var temp = m_Skills[i];
                if (temp != null && temp.ReadSlot() == slot)
                {
                    return temp;
                }
            }
            return null;
        }
        public Skill GetRandomSkill()
        {
            var count = m_Skills.Count;
            if (count <= 0)
            {
                return null;
            }

            var index = UnityEngine.Random.Range(0, count);
            return m_Skills[index];
        }

        public void ClearSkills()
        {
            foreach(var skill in m_Skills)
            {
                if (skill != null)
                {
                    skill.OnSkillUnregister();
                    skill.Destroy();
                }
            }
            m_Skills.Clear();
        }

        public List<Skill> ReadSkills()
        {
            return m_Skills;
        }
    }
}
