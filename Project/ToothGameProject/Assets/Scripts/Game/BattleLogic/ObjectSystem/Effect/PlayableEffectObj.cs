using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{
    public enum PlayableEffectDelType
    {
        None,
        Pool,
        Del
    }
    // 不属于地表层的短期物体，例如闪电，魔法，射出的箭
    // 这类物体不受场景管理
    public class PlayableEffectObj : IResource
    {
        protected emEntityType m_EntityType = emEntityType.em_EntityType_SkillObj;
        protected int m_ID = int.MinValue;
        private bool m_Visiable = true;
        //隐藏或者结束的时间点
        private float m_HideTime = -100;
        public  float GetHideTime()
        {
            return m_HideTime;
        }
        public void SetHideTime(float time)
        {
            m_HideTime = time;
        }
        public bool ReadIsDestroy()
        {
            return m_ID == int.MinValue;
        }
        private GroupId m_Group = 0;
        public void SetGroup(GroupId group)
        {
            m_Group = group;
        }
        public GroupId GetGroup()
        {
            return m_Group;
        }

        public emEntityType ReadObjectType()
        {
            return m_EntityType;
        }
        public void SetObjectType(emEntityType type)
        {
            m_EntityType = type;
        }
        public override bool ReadVisiable()
        {
            return m_Visiable;
        }
        public override int ReadId()
        {
            return m_ID;
        }
        public void SetId(int id)
        {
            m_ID = id;
        }
        public override void SetVisiable(bool visiable)
        {
            m_Visiable = visiable;
            GetRender().SetActive(visiable);
        }
        public virtual void SetPosition(Vector3 position)
        {
            GetRender().SetPosition(position);
        }
        public virtual Vector3 GetPosition()
        {
            var render = GetRender();
            return render.GetPosition();
        }
        public virtual Vector3 GetForward()
        {
            var render = GetRender();
            return render.GetForward();
        }
        public virtual void SetForward(Vector3 forward)
        {
            if (forward == Vector3.zero)
            {
                return;
            }
            var render = GetRender();
            render.SetForward(forward.normalized);
        }

        public virtual float GetMoveSpeed()
        {
            return 0;
        }
        public bool IsPooled;
        private float m_fDuringTime = 0;
        private float m_PlayUsedTime = 0;
        private bool m_IsPlay = false;
        private PlayableEffectDelType m_IsAutoDel =  PlayableEffectDelType.None;
        private bool m_IsFinish = false;

        public void SetAutoDel(PlayableEffectDelType del)
        {
            m_IsAutoDel = del;
        }

        // 特效表 t_sound：配了音效 id 的特效在开始播放时播一次。
        // RenderEff 链路（t_atk_eff / 命中特效等）在 ShowEff 里自带同一列音效，
        // 这里补的是池化特效对象（EffectObj / LaserObj），两者共用 t_effectBean.t_sound。
        // 子弹/炮弹自己有 t_attackSound / t_hit_sound，不覆写 ReadConfiguredSoundId，不会重复播。
        private bool m_ConfiguredSoundPlayed;

        protected virtual int ReadConfiguredSoundId()
        {
            return 0;
        }

        private void PlayConfiguredSoundOnce()
        {
            m_ConfiguredSoundPlayed = true;
            int soundId = ReadConfiguredSoundId();
            if (soundId == 0)
            {
                return;
            }

            var render = GetRender();
            var position = render != null ? render.GetPosition() : Vector3.zero;
            AudioManager.GetInstance().Play3D(soundId, position);
        }

        public virtual void SetPlay(bool play)
        {
            m_PlayUsedTime = 0;
            m_IsPlay = play;
            if(play)
            {
                m_IsFinish = false;
                // 池化复用后每次重新播放都要重新触发一次音效
                m_ConfiguredSoundPlayed = false;
            }
        }
        public bool IsPlay()
        {
            return m_IsPlay;
        }

        public bool IsFinish()
        {
            return m_IsFinish;
        }

        public  void SetDuringTime(int dt)
        {
            m_fDuringTime = dt;    
        }
        public  float GetDuringTime()
        {
            return m_fDuringTime;
        }
        public void SetFinish()
        {
            if(!m_IsFinish)
            {
                FinishImp();
            }
        }

        // 表现还有多久需要继续显示（秒，如子弹命中/枪口特效）。
        // 对象回池时据此顺延可复用时间，避免复用时重新播放把上一发的表现立刻清掉。
        public virtual float ReadVisualRemainTime()
        {
            return 0f;
        }
        public virtual void SetBean(object bean)
        {

        }
        public virtual void PoolObj()
        {

        }

        protected void FinishImp()
        {
            //播放结束
            m_IsPlay = false;
            m_IsFinish = true;

            if(m_IsAutoDel == PlayableEffectDelType.Pool)
            {
                BattleManager.GetBattle().GetObjectManager().RemoveEffObject(this, false);
                PoolObj();
            }
            else if(m_IsAutoDel == PlayableEffectDelType.Del)
            {
                BattleManager.GetBattle().GetObjectManager().RemoveEffObject(this, true);
            }
            else
            {
                SetVisiable(false);
            }
        }
        public override void Update(float dt)
        {
            if(!m_IsPlay)
            {
                return;
            }
            if(!m_ConfiguredSoundPlayed)
            {
                // 首帧播放：此时位置已经由 SetPosition 写好，声源与特效同位
                PlayConfiguredSoundOnce();
            }
            if(m_IsFinish)
            {
                return;
            }
            m_PlayUsedTime += dt;
            if(m_PlayUsedTime >= m_fDuringTime)
            {
                FinishImp();
            }
        }
    }
}
