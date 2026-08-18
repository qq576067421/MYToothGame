using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameDll
{
    public class SkillType
    {
        public const long CommonSkill = 1001;
        public const long CommonBulletSkill = 1002;
        public const long CommonTriggerSkill = 1003;
        public const long DashSkill = 1004;
        public const long NormalBuff = 1005;
        public const long CommonPaodanSkill = 1006;
    }
    public class SkillTemplate
    {
        public static Skill createSkill(long skillId)
        {
            t_skillBean skillBean = t_skillBean.GetConfig(skillId);
            if (skillBean == null)
                return null;
            var classId = skillBean.t_class_Id;
            Skill skill = null;
            switch(classId)
            {
                case SkillType.CommonSkill:
                    {
                        skill = new CommonSkill();
                        break;
                    }
                case SkillType.CommonBulletSkill:
                    {
                        skill = new CommonBulletSkill();
                        break;
                    }
                case SkillType.CommonTriggerSkill:
                    {
                        skill = new CommonSkill();
                        break;
                    }
                case SkillType.DashSkill:
                    {
                        skill = new DashSkill();
                        break;
                    }
                case SkillType.CommonPaodanSkill:
                    {
                        skill = new CommonPaodanSkill();
                        break;
                    }
                case SkillType.NormalBuff:
                    {
                        skill = new CommonSkill();
                        break;
                    }
                
            }
            if (skill == null)
            {
                Debug.LogWarning("GameDll 实例化技能类失败，" + skillId);
                return null;
            }
            skill.InitTemplate((int)classId, skillId);
            skill.PreLoadEffect();
            return skill;

            
        }





    }

}