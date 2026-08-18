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
        public virtual void SetPlay(bool play)
        {
            m_PlayUsedTime = 0;
            m_IsPlay = play;
            if(play)
            {
                m_IsFinish = false;
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
