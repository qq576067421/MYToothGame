using GameDll;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{




    public abstract class IResource
    {
        private GameDll.UResource m_Resource;
        public void SetResource(GameDll.UResource res)
        {
            m_Resource = res;
        }
        public virtual void ReleaseResource()
        {
            if (m_Resource != null)
            {
                m_Resource.Destroy();
            }
        }
        private bool m_IsDestroy;
        public virtual void Destroy()
        {
            ReleaseResource();
            m_IsDestroy = true;
            //m_Resource = null;
        }
        //更新之前，检查关心的数据的改变
        protected virtual void BeforeUpdateCheckData()
        {

        }
        public virtual void Update(float dt)
        {
            BeforeUpdateCheckData();


        }
        public virtual void UpdateRender()
        {
            if (m_Resource != null)
            {
                m_Resource.Update();
            }
        }
        public abstract bool ReadVisiable();
        public abstract void SetVisiable(bool visiable);

        public abstract int ReadId();
        private bool m_IsInit = false;
        public virtual void InitInstance()
        {
            if(m_IsInit)
            {
                Debug.LogError("重复InitInstance");
            }
            m_IsInit = true;
        }

        public virtual void CreateRender(UResource obj, ResourceType resType)
        {

        }

        public  object GetShowObj()
        {
            if (m_Resource == null)
            {
                return null;
            }
            else
            {
                return m_Resource.GetShowObj();
            }
        }

        public void AddLoadedCall(Action call)
        {
            if(m_Resource != null)
            {
                m_Resource.AddLoadedCall(call);
            }
        }
        public virtual void OnDrawGizmos()
        {

        }
        public string m_LastSnap = "";
        public virtual void Snapshot(StringBuilder snap)
        {
            snap.Append("实体：");
            snap.Append("Id：" + ReadId() + " ");
            snap.Append("Name:" + m_Resource.GetName());
            snap.Append("Visiable" + ReadVisiable());
        }

        public GameDll.UResource GetRender()
        {
            return m_Resource;
        }

        public virtual bool ReadIsDestroy()
        {
            return m_IsDestroy;
        }
    }
}
