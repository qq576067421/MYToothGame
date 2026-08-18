using System;
using System.Collections.Generic;
using System.Text;
using LCL;
using UnityEngine;

namespace GameDll
{
    public class UEntity:UResource
    {
        protected GameObject m_GameObject = null;

        protected Transform m_TransformCache = null;


        protected Vector3 m_Position = Vector3.zero;
        protected Vector3 m_Forward = Vector3.forward;
        protected Vector3 m_Up = Vector3.up;
        protected Vector3 m_Scale = Vector3.one;
        protected Collider m_Collider = null;
        public override Collider GetCollider()
        {
            return m_Collider;
        }


        protected bool m_bInitVisiable = true;

        private int m_Layer = 0;
        protected virtual void SetLayerImp()
        {
            var show = GetShowObj() as GameObject;
            show.layer = m_Layer;
        }
        public override void SetLayer(int layer)
        {
            m_Layer = layer;
            if (IsObjectLoaded())
            {
                SetLayerImp();
            }
            else
            {
                AddLoadedCall(SetLayerImp);
            }
        }
        public override int GetLayer()
        {
            return m_Layer;
        }

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

        protected override void DestroyImp()
        {
            base.DestroyImp();
            m_TransformCache = null;
        }
        public override void Destroy()
        {
            base.Destroy();
            ClearRenderEffect();
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
            m_Collider = m_GameObject.GetComponentInChildren<Collider>();
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

        public virtual float GetMoveSpeed()
        {
            return 0;
        }

        public override void SetPosition(Vector3 pos)
        {
            m_Position = pos;

            if(IsObjectLoaded())
            {
                m_TransformCache.position = m_Position;
                OnUpdateHud();
            }
        }
        protected virtual void SetNameImp()
        {
            m_GameObject.name = m_GameObjectPrefabName + "_" + m_Name + "_" + m_Id;
        }
        public override void SetName(string name)
        {
            m_Name = name;
            if (IsObjectLoaded())
            {
                SetNameImp();
            }
            else
            {
                AddLoadedCall(SetNameImp);
            }
        }
        public override string GetName()
        {
            return m_Name;
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
        protected virtual void SetUpImp()
        {
            m_TransformCache.up = m_Up;
        }
        public override void Update()
        {
            base.Update();
            UpdateRenderEffect();
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
        public override void SetUp(Vector3 up)
        {
            m_Up = up;
            if (IsObjectLoaded())
            {
                SetUpImp();
            }
            else
            {
                AddLoadedCall(SetUpImp);
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

        public override Vector3 GetHeadPoint()
        {
            if (m_TransformCache == null)
            {
                return base.GetHeadPoint();
            }

            var renderers = m_GameObject != null ? m_GameObject.GetComponentsInChildren<Renderer>() : null;
            if (renderers != null && renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }

            var collider = GetCollider();
            if (collider != null)
            {
                var bounds = collider.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }

            return m_TransformCache.position;
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

        public override bool GetActive()
        {
            return m_Active;
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

        //表现层的特效
        private List<RenderEff> m_RenderEffects = new List<RenderEff>();
        public override void AddRenderEffect(int effCfgId, int duringTimeMM = 0, bool single = false)
        {
            if (single)
            {
                int has_count = m_RenderEffects.Count;
                if (has_count > 0)
                {
                    for(int i =0; i<has_count; ++i)
                    {
                        var has_eff = (RenderEff)m_RenderEffects[i];
                        if(has_eff.m_CfgId == effCfgId)
                        {
                            has_eff.ResetPlay();
                            return;
                        }
                    }
                }
            }
            var render_pos = GetPosition();
            var render_rot = GetEulerAngles();
            var ieff = RenderEffManager.GetInstance().CreateRenderEff(effCfgId);
            if (ieff != null)
            {
                var eff = (RenderEff)ieff;
                eff.ShowEff(false, render_pos, render_rot, Vector3.one);
                if (duringTimeMM > 0)
                {
                    eff.SetDuringTime(duringTimeMM / 1000.0f);
                }
                m_RenderEffects.Add(eff);
            }
        }
        private void UpdateRenderEffect()
        {
            var render_pos = GetPosition();
            int count = m_RenderEffects.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var eff = (RenderEff)m_RenderEffects[i];
                eff.SetPosition(render_pos);
                if (eff.m_DuringTime > 0)
                {
                    if (Time.realtimeSinceStartup - eff.m_StartTime > eff.m_DuringTime)
                    {
                        RenderEffManager.GetInstance().PoolRenderEff(eff);
                        m_RenderEffects.RemoveAt(i);
                        continue;
                    }
                }
            }
        }
        public void ClearRenderEffect()
        {
            int count = m_RenderEffects.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var eff = m_RenderEffects[i];
                RenderEffManager.GetInstance().PoolRenderEff(eff);
            }
            m_RenderEffects.Clear();
        }
    }
}
