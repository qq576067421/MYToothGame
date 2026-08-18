using System;
using System.Collections.Generic;
using System.Text;
using LCL;
using UnityEngine;

namespace GameDll
{

    public class USimpleEff : UResource
    {
        protected GameObject m_GameObject = null;

        protected Transform m_TransformCache = null;
        protected Vector3 m_Position = Vector3.zero;
        protected Vector3 m_Forward = Vector3.forward;
        protected Vector3 m_Scale = Vector3.one;

        protected bool m_bInitVisiable = true;

        private ParticleSystem m_Particle = null;

        public override Vector3 GetEulerAngles()
        {
            if(m_TransformCache != null)
            {
                return m_TransformCache.eulerAngles;
            }
            else
            {
                Quaternion q4 = Quaternion.LookRotation(m_Forward);
                return q4.eulerAngles;
            }
        }
        public override void LoadRender(string ab, string assetName)
        {
            m_ABName = ab;
            m_AssetName = assetName;
            LoadShowObjFromFileAsync(null);
            SetParent(Main.GameMainObject, "", false);
        }


        protected override bool LoadShowObjImp(UnityEngine.Object obj)
        {
            if(base.LoadShowObjImp(obj) == false)
            {
                return false;
            }
            m_GameObject = m_UnityObject as GameObject;
            m_TransformCache = m_GameObject.transform;
            m_TransformCache.position = m_Position;
            m_Particle = m_GameObject.GetComponentInChildren<ParticleSystem>();


            return true;
        }

        public override bool IsDestroy()
        {
            return m_Destroy;
        }

        public override Vector3 GetPosition()
        {
            if(m_TransformCache != null)
            {
                return m_TransformCache.position;
            }
            return m_Position;
        }

        public override void SetPosition(Vector3 pos)
        {
            m_Position = pos;

            if(IsObjectLoaded())
            {
                m_TransformCache.position = m_Position;
            }
        }

        public virtual Vector3 GetRight()
        {
            if (m_TransformCache != null)
            {
                return m_TransformCache.right;
            }
            else
            {
                return Vector3.right;
            }
        }
        public override Vector3 GetForward()
        {
            if (m_TransformCache != null)
            {
                return m_TransformCache.forward;
            }
            else
            {
                return m_Forward;
            }
        }
        protected virtual void SetForwardImp()
        {
            m_TransformCache.forward = m_Forward;
        }



        public override void SetForward(Vector3 rot)
        {
            m_Forward = rot;
            if (IsObjectLoaded())
            {
                SetForwardImp();
            }
            else
            {
                AddLoadedCall(SetForwardImp);
            }
        }
        protected virtual void SetScaleImp()
        {
            m_TransformCache.localScale = m_Scale;
        }
        public override void SetScale(Vector3 scale)
        {
            m_Scale = scale;
            if(IsObjectLoaded())
            {
                SetScaleImp();
            }
            else
            {
                AddLoadedCall(SetScaleImp);
            }

        }

        public override Vector3 GetScale()
        {
            if(m_TransformCache != null)
            {
                return m_TransformCache.localScale;
            }
            return m_Scale;
        }

        protected virtual void SetActiveImp()
        {
            m_GameObject.SetActive(m_Active);
        }
        public override void SetActive(bool bshow)
        {
            m_Active = bshow;
            if (IsObjectLoaded())
            {
                SetActiveImp();
            }
            else
            {
                AddLoadedCall(SetActiveImp);
            }
        }
        public void Replay()
        {
            if(m_Particle != null)
            {
                m_Particle.Simulate(0, true, true);
                m_Particle.Play();
            }
        }
        public override bool GetActive()
        {
            return m_Active;
        }
        public virtual void SetOpacity(float alpha)
        {

        }
        public virtual float GetOpacity()
        {
            return 1;
        }

        public virtual void SetParent(UEntity obj, string path, bool worldPositionStays)
        {
            obj.AddLoadedCall(() => 
            {
                SetParent(obj.GetShowObj() as GameObject, path, worldPositionStays);
            });
        }

        public virtual void SetParent(GameObject parent, string path, bool worldPositionStays)
        {
            GameObject tempParent = parent;
            string tempPath = path;
            bool tempStayWorldPosition = worldPositionStays;
            AddLoadedCall(()=> 
            {
                if(IsDestroy())
                {
                    return;
                }
                if (GameObject.Equals(tempParent, null))
                {
                    m_TransformCache.SetParent(null, tempStayWorldPosition);
                }
                else
                {
                    if (string.IsNullOrEmpty(tempPath))
                    {
                        m_TransformCache.SetParent(tempParent.transform, tempStayWorldPosition);
                    }
                    else
                    {
                        Transform hang = tempParent.transform.Find(tempPath);
                        if (hang != null)
                        {
                            m_TransformCache.SetParent(hang, tempStayWorldPosition);
                        }
                    }
                }
            });
        }




        public override void Init()
        {

        }

        public override void SetAnimationSpeed(float speed)
        {
        }
        public override void SetAnimationMaxTime(float time)
        {

        }
        public override void SetCampColor(string color)
        {
        }
    }
}
