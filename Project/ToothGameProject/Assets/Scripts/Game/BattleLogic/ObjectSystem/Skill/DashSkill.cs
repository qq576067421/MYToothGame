using MonoBean;
using System;
using UnityEngine;

namespace GameDll
{
    public class DashSkill : Skill
    {
        public override void OnEnter()
        {
            base.OnEnter();
            SetCurAction();
            PlayAction(m_CurAction.t_ac_name);
            m_CastStatus = SkillCastStatus.warming_up;
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            float skillUsedTime = GetSkillUsedTime();

            switch (m_CastStatus)
            {
                case SkillCastStatus.warming_up:
                    {
                        float castTime = BattleManager.ConvertFrame2Second(m_CurAction.t_ac_cast_point, GetAttackSpeed(), GetActionFrameRate());
                        if (skillUsedTime > castTime)
                        {
                            m_CastStatus = SkillCastStatus.cast_point;
                        }
                        break;
                    }
                case SkillCastStatus.cast_point:
                    {
                        var attacker = ReadAttacker();
                        if (attacker != null)
                        {
                            var forward = ReadSkillDir();
                            forward.y = 0f;
                            if (forward == Vector3.zero)
                            {
                                forward = attacker.ReadForward();
                            }
                            forward.y = 0f;
                            if (forward != Vector3.zero)
                            {
                                forward.Normalize();
                                attacker.SetForward(forward);
                                var pos = attacker.GetPosition();
                                var dashDist = Mathf.Max(0.1f, GetCastDistance());
                                attacker.SetPosition(pos + forward * dashDist);
                            }
                        }
                        m_CastStatus = SkillCastStatus.cast_back;
                        break;
                    }
                case SkillCastStatus.cast_back:
                    {
                        var finishTime = BattleManager.ConvertFrame2Second(m_CurAction.t_ac_finish, GetAttackSpeed(), GetActionFrameRate());
                        if (skillUsedTime >= finishTime)
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
    }
}
