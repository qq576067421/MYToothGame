using System;
using System.Collections.Generic;
using System.Text;
using LCL;
using UnityEngine;
using UnityEngine.AI;

namespace GameDll
{
    public enum emEntityType
    {
        em_EntityType_None,
        em_EntityType_PlayerHero,
        em_EntityType_Hero,
        em_EntityType_MasterHero,
        em_EntityType_SmallMonster,
        em_EntityType_SkillObj,
        em_EntityType_Actor,
        em_EntityType_Effect,
        em_EntityType_Laser,
        em_EntityType_Bullet,
        em_EntityType_Paodan,
        em_EntityType_UpgradeChallengeTarget,
    }
    public  class UResource
    {
        //这个是设置本渲染对象需要附着的父对象，这里只同步位置
        protected UResource m_AttachParent;
        protected string m_ABName;
        protected string m_AssetName;
        protected bool m_FullPath;
        protected ABRequest m_ABId;
        protected bool m_bABRes = false;

        protected bool m_Destroy = false;

        //显示在Hud的名字
        protected string m_Name;
        protected emEntityType m_EntityType;

        //预制件本来的名字
        protected string m_GameObjectPrefabName = "UObject";
        protected List<System.Action> m_LoadedCalls = new List<Action>(5);

        protected Action<bool, UResource> m_UserLoadedCall = null;

        public virtual float GetRunMoveFactor()
        {
            return 1.25f;
        }

        protected bool m_Active = true;
        protected UnityEngine.Object m_UnityObject = null;


        //对应的实体id
        protected int m_Id;
        public virtual void SetId(int id)
        {
            m_Id = id;
            if(id == 0)
            {
                UDebug.LogError("id == 0");
            }
        }
        //这个地方不能用重载，也就是不能用到基类，防止被战斗误用
        public int GetId()
        {
            return m_Id;
        }
        public virtual void SetEntityType(emEntityType type)
        {
            m_EntityType = type;
        }
        //这个地方不能用重载，也就是不能用到基类，防止被战斗误用
        public emEntityType GetEntityType()
        {
            return m_EntityType;
        }
        public virtual void Init()
        {

        }

        public virtual void LoadRender(string ab, string assetName)
        {
            m_ABName = ab;
            m_AssetName = assetName;
            m_FullPath = false;            
        }

        public virtual bool IsSameRender(string abName, string assetName)
        {
            return m_ABName == abName && m_AssetName == assetName;
        }

        public  virtual void SetTransparent(float alpha)
        {

        }
        public virtual float GetTransparent()
        {
            return 1.0f;
        }
        public virtual bool SupportsColorAlphaProperty(int propertyId)
        {
            return false;
        }
        public virtual float ReadColorAlphaProperty(int propertyId)
        {
            return 1.0f;
        }
        public virtual void SetColorAlphaProperty(int propertyId, float alpha)
        {
        }
        public virtual void ClearColorAlphaProperty(int propertyId)
        {
        }
        public virtual void PlayAnimation(string ani, string endAni = null, 
            float time = 0.3f, bool useTrigger = false)
        {

        }
        public virtual void Destroy()
        {
            DisableHudRender();

            if (m_ABId != null)
            {
                LCL.UIRes.UnloadPrefab(m_ABId);
                m_ABId = null;
            }
            if (IsObjectLoaded())
            {
                //UDebug.Log("Real Destroy:" + m_ABName);
                DestroyImp();
            }
            m_Destroy = true;
        }

        protected virtual void DestroyImp()
        {
            m_Destroy = true;
            if (m_bABRes)
            {
                //只有当以AB形式加载的才自行进行资源是否，否则是需要是否引用就可以了
                GameObject.Destroy(m_UnityObject);
            }
            m_bABRes = false;
            m_UnityObject = null;

        }

        public virtual void AddLoadedCall(System.Action call)
        {
            if (m_Destroy == false && call != null)
            {
                m_LoadedCalls.Add(call);
            }
        }

        public virtual void SetLevel(long level)
        {
        }
        public virtual void SetShowLevel(bool showLevel)
        {

        }
        public virtual void SetExp(float exp)
        {

        }
        public virtual void SetShowExp(bool showExp)
        {

        }

        //直接设置显示对象
        public virtual void LoadShowObjFromMemory(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }
            m_Destroy = false;
            m_bABRes = false;
            if (m_UnityObject == null)
            {
                LoadShowObjImp(obj);
            }
        }

        public virtual void SetForward(Vector3 value)
        {
        }
        public virtual void SetUp(Vector3 up)
        {

        }
        public virtual void SetBaseForward(Vector3 value)
        {
        }

        public virtual float ReadDefaultPitchDegrees()
        {
            return 0f;
        }

 
        public virtual string GetName()
        {
            return "";
        }




        //从ab中加载显示对象
        public virtual void LoadShowObjFromFileAsync(Action<bool, UResource> call)
        {
            m_Destroy = false;
            m_UserLoadedCall = call;

            m_bABRes = true;
            //异步
            m_ABId = LCL.UIRes.LoadPrefabAsync(typeof(GameObject), m_ABName, m_AssetName, OnAsyncLoaded);
        }

        public virtual Vector3 GetForward()
        {
            return Vector3.forward;
        }

        public virtual Vector3 GetPosition()
        {
            return Vector3.zero;
        }


        public virtual void SetScale(Vector3 value)
        {
        }


        public virtual Vector3 GetScale()
        {
            return Vector3.one;
        }

        protected Vector3 m_InitScale = Vector3.one;
        public virtual void SetInitScale(Vector3 initScale)
        {
            m_InitScale = initScale;
        }
        public virtual Vector3 GetInitScale()
        {
            return m_InitScale;
        }


        public virtual void SetPosition(Vector3 value)
        {
        }
        protected virtual void OnAfterLoadedCall()
        {
            int count = m_LoadedCalls.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; ++i)
                {
                    m_LoadedCalls[i]();
                }
                m_LoadedCalls.Clear();
            }
        }

 
        public virtual Collider GetCollider()
        {
            return null;
        }

        protected virtual void OnAsyncLoaded(ResData uol, object userData)
        {
            if (uol == null || m_ABId == null)
            {
                return;
            }
            if (m_Destroy)
            {
                if(m_ABId != null)
                {
                    LCL.UIRes.UnloadPrefab(m_ABId);
                    m_ABId = null;
                }

                return;
            }
            GameObject prefab = (GameObject)GameObject.Instantiate(uol.m_Obj);
            prefab.name = uol.m_Obj.name;
            LoadShowObjImp(prefab);
            OnAfterLoadedCall();


            if (m_UserLoadedCall != null)
            {
                m_UserLoadedCall(uol != null, this);
            }
        }



        public virtual void BulletEmit()
        {

        }
        public virtual void BulletBoom(Vector3 pos, Vector3 forward)
        {

        }

        protected virtual bool LoadShowObjImp(UnityEngine.Object obj)
        {
            m_GameObjectPrefabName = obj.name;
            m_UnityObject = obj;
            if (m_Destroy)
            {
                m_LoadedCalls = null;
                DestroyImp();
                return false;
            }
            return true;
        }

        public virtual object GetShowObj()
        {
            return m_UnityObject;
        }

        public virtual void SetDashTotalTime(float speed)
        {

        }
        public virtual void StartDash()
        {

        }
        public virtual bool IsDestroy()
        {
            return m_Destroy;
        }
        public virtual void SetName(string name)
        {
            m_Name = name;
        }

        public virtual void SetActive(bool bshow)
        {

        }
        public virtual bool GetActive()
        {
            return m_Active;
        }
        public virtual bool IsObjectLoaded()
        {
            return m_UnityObject != null && !m_UnityObject.Equals(null);
        }

        public virtual void SetShowHud(bool show)
        {

        }
        public virtual void SetShowHudBlood(bool show)
        {

        }
        public virtual void SetShowHudName(bool show)
        {

        }
        public virtual void Update()
        {

        }

        public virtual void OnUpdateHud()
        {

        }
        public virtual void SetLayer(int layer)
        {

        }
        public virtual int GetLayer()
        {
            return LayerMask.NameToLayer("Default");
        }


        public virtual void SetHpValue(float cur, float tween_time = 0)
        {

        }
        public virtual void SetMagicValue(float cur, float tween_time = 0)
        {

        }

        public virtual void EnableHudRender()
        {

        }

        public virtual void DisableHudRender()
        {

        }

        public virtual void SetAnimationSpeed(float speed)
        {

        }
        public virtual void ReplayCurrentAnimation(float normalizedTime)
        {

        }
        public virtual void SetAnimationMaxTime(float time)
        {

        }
        public virtual void SetCampColor(string color)
        {

        }

        public virtual Vector3 GetEulerAngles()
        {
            return Vector3.zero;
        }

        public virtual void ShowWeaponEff(bool show)
        {

        }

        public virtual void SetAngularSpeed(int speed)
        {

        }
        public virtual void SetMoveSpeed(float speed)
        {

        }

        public virtual Vector3 GetHitPoint()
        {
            return Vector3.zero;
        }


        public virtual Vector3 GetHeadPoint()
        {
            return GetPosition();
        }



        public virtual void SetPaoTongForward(Vector3 forward)
        {

        }
        public virtual void SetTargetPosition(Vector3 pos)
        {
        }
 
        public virtual void SetIsFollow(bool isFollow)
        {

        }
        public virtual void SetInitPosition(Vector3 pos)
        {

        }

        public virtual void PlayOnceWeaponEff(string animation, float speed  = 1.0f)
        {

        }

        public virtual void StartGhostingRender(int snap_type, float time_interval_or_distance, int init_count, int max_count, float life_time)
        {

        }
        public virtual void PauseGhostingRender()
        {

        }
        public virtual void DestroyGhostingRender()
        {

        }

        public virtual void ChangeXuWu(int num)
        {

        }

        public virtual void SetShadowTransparent(float trans)
        {

        }

        public virtual void SetQuality(int t_quality)
        {
            UDebug.LogWarning("SetQuality NotImplementedException");
        }


        

        public virtual void SetItemName(string t_name, long item_exp)
        {
        }

        public virtual void PlaySpear(float time, float speed)
        {
        }

        public virtual void PlayDownNeedle(float time)
        {
        }

        public virtual void PlayUpNeedle(float time)
        {
        }

        public virtual void PlayStone(float v1, float v2)
        {
        }

        public virtual void StayStone()
        {
        }


        //设置我需要附着的对象
        public virtual void AttachToRender(UResource parent)
        {
            m_AttachParent = parent;
        }
        //添加特效到我身上
        public virtual void AddRenderEffect(int effCfgId, int duringTimeMM = 0, bool single = false)
        {

        }

        public virtual Matrix4x4 GetMatrix4X4()
        {
            if(m_UnityObject == null)
            {
                return Matrix4x4.identity;
            }
            else
            {
                var obj = m_UnityObject as GameObject;
                if(obj == null)
                {
                    return Matrix4x4.identity;
                }
                else
                {
                    return obj.transform.transform.localToWorldMatrix;
                }
            }
        }
    }
}
