using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LCL;
using MonoBean;
using UnityEngine;

namespace GameDll
{
    public enum RenderPoolStyle
    {
        Active,
        PlayStop
    }
    public class RenderEff
    {
        public int m_CfgId;
        //默认缓存60秒
        public float m_CacheMaxTime = 60.0f;
        private t_effectBean m_Cfg;
        private string m_SoundOverride;
        public Vector3 m_Position;
        public Vector3 m_Rot;
        public Vector3 m_Dir;
        public Vector3 m_Scale;
        private bool m_IsLocal;
        private bool m_UseDir;

        private bool m_Active = true;
        public virtual void SetActive(bool active)
        {
            m_Active = active;
            if(m_Eff != null)
            {
                if(m_Holder == null || m_Holder.m_RenderPoolStyle == RenderPoolStyle.Active)
                {
                    m_Eff.SetActive(active);
                }
                if(m_Holder != null)
                {
                    if (m_Active)
                    {
                        var particle = m_Holder.m_MainParticle;
                        if (particle != null)
                        {
                            particle.Simulate(0, true, true);
                            particle.Play(true);
                        }
                    }
                    else
                    {
                        var particle = m_Holder.m_MainParticle;
                        if (particle != null)
                        {
                            particle.Stop(true);
                        }
                    }
                }


            }
        }
        public virtual bool IsActive()
        {
            return m_Active;
        }
        public bool m_Destroy = false;
        public GameObject m_Eff;
        private EffectHolder m_Holder = null;
        public bool m_IsPooled = false;
        public float m_LastPoolTime; // 上次使用的时间（秒）

        private ABRequest m_ABId = null;
        public Transform m_Parent = null;
        public float m_StartTime = 0;
        public float m_DuringTime = 0;
        public object m_UserData;
        private Action OnLoadedCall;
        public virtual void AddLoadedCall(Action hasWillCall)
        {
            if(m_Destroy)
            {
                return;
            }
            if(m_Eff != null && !m_Eff.Equals(null))
            {
                hasWillCall();
            }
            else
            {
#pragma warning disable ANALYZER0002 // 委托配对分析器
                //释放方式已处理，不需要担心委托泄漏问题
                OnLoadedCall += hasWillCall;
#pragma warning restore ANALYZER0002 // 委托配对分析器
            }
        }
        public void SetParent(Transform parent)
        {
            m_Parent = parent;
            m_ParentResource = null;

            if(IsLoaded())
            {
                var trans = m_Eff.transform;
                trans.SetParent(m_Parent, false);
                if (m_IsLocal)
                {
                    trans.localPosition = m_Position;
                    trans.localEulerAngles = m_Rot;
                    trans.localScale = m_Scale;
                }
                else
                {
                    trans.position = m_Position;
                    trans.eulerAngles = m_Rot;
                    trans.localScale = m_Scale;
                }
            }
        }
        public void ResetPlay()
        {
            m_StartTime = Time.realtimeSinceStartup;
        }
        public void ShowEff(bool local, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            m_IsLocal = local;
            m_Position = pos;
            m_Rot = rot;
            m_UseDir = false;
            m_Scale = scale;
            m_Active = true;
            if(m_Eff != null)
            {
                var trans = m_Eff.transform;
                trans.SetParent(m_Parent, false);
                if(m_IsLocal)
                {
                    trans.localPosition = m_Position;
                    trans.localEulerAngles = m_Rot;
                    trans.localScale = m_Scale;
                }
                else
                {
                    trans.position = m_Position;
                    trans.eulerAngles = m_Rot;
                    trans.localScale = m_Scale;
                }

                SetActive(m_Active);

                PlaySound(trans);
            }
            m_StartTime = Time.realtimeSinceStartup;
            if(OnLoadedCall != null)
            {
                OnLoadedCall();
                OnLoadedCall = null;
            }
        }
        public void ShowEffDir(bool local, Vector3 pos, Vector3 world_dir, Vector3 scale)
        {
            m_IsLocal = local;
            m_Position = pos;
            m_Dir = world_dir;
            if(m_Dir == Vector3.zero)
            {
                m_Dir = Vector3.forward;
            }
            m_UseDir = true;
            m_Scale = scale;
            m_Active = true;
            if (m_Eff != null)
            {
                var trans = m_Eff.transform;
                trans.SetParent(m_Parent, false);
                if (m_IsLocal)
                {
                    trans.localPosition = m_Position;
                    trans.forward = m_Dir;
                    trans.localScale = m_Scale;
                }
                else
                {
                    trans.position = m_Position;
                    trans.forward = m_Dir;
                    trans.localScale = m_Scale;
                }

                SetActive(m_Active);

                PlaySound(trans);
            }
            m_StartTime = Time.realtimeSinceStartup;
            if (OnLoadedCall != null)
            {
                OnLoadedCall();
                OnLoadedCall = null;
            }
        }
        public virtual void SetDuringTime(float time)
        {
            m_DuringTime = time;
        }
        public void SetPosition(Vector3 pos)
        {
            m_Position = pos;
            if(m_Eff != null)
            {
                m_Eff.transform.position = pos;
            }
        }
        public virtual void SetPosition(float x, float y, float z)
        {
            SetPosition(new Vector3(x, y, z));
        }
        public virtual void Destroy()
        {
            OnLoadedCall = null;
            m_StartTime = 0;
            m_Delay = 0;
            m_UserData = null;
            m_Destroy = true;
            if(m_ABId != null)
            {
                LCL.UIRes.UnloadPrefab(m_ABId);
                m_ABId = null;
            }
            if(m_Eff != null)
            {
                GameObject.Destroy(m_Eff);
            }
        }
        public void LoadEff(int cfg)
        {
            m_CfgId = cfg;
            m_SoundOverride = null;
            m_LoadErrorPath = null;
            t_effectBean bean = t_effectBean.GetConfig(m_CfgId);
            if(bean == null)
            {
                UDebug.LogError("特效资源没有找到，cfg id:" + m_CfgId);
                return;
            }
            m_Cfg = bean;
            m_DuringTime = bean.t_time / 1000.0f;
            m_ABId = LCL.UIRes.LoadPrefabAsync(typeof(GameObject), bean.t_abname, 
                Tool.GetAssetName(bean.t_abname), OnLoadRes);


        }

        private string m_LoadErrorPath;
        public void LoadEff(string abName, string assetName, string sound = null)
        {
            m_Cfg = null;
            m_CfgId = 0;
            m_SoundOverride = sound;
            m_LoadErrorPath = abName;
            m_DuringTime = 2.0f;
            m_ABId = LCL.UIRes.LoadPrefabAsync(typeof(GameObject), abName, assetName, OnLoadRes);
        }

        private void PlaySound(Transform trans)
        {
            if (trans == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(m_SoundOverride) && m_SoundOverride != "0")
            {
                if (int.TryParse(m_SoundOverride, out var soundId) && soundId > 0)
                {
                    AudioManager.GetInstance().Play3D(soundId, trans.position);
                }
                else
                {
                    Debug.LogWarning("特效声音必须配置声音表编号，当前值：" + m_SoundOverride);
                }
                return;
            }

            if (m_Cfg != null && m_Cfg.t_sound != 0)
            {
                AudioManager.GetInstance().Play3D(m_Cfg.t_sound, trans.position);
            }
        }

        private void OnLoadRes(ResData uol, object data)
        {
            if(m_Destroy)
            {
                if(m_Eff != null)
                {
                    GameObject.Destroy(m_Eff);
                    m_Eff = null;
                }
                LCL.UIRes.UnloadPrefab(m_ABId);
                m_ABId = null;
                return;
            }
            if(uol.m_Obj == null || uol.m_Obj.Equals(null))
            {
                string errPath = m_Cfg != null ? m_Cfg.t_abname : m_LoadErrorPath;
                UDebug.LogError("特效资源加载失败，路径是：" + errPath);
                return;
            }
            m_Eff = (GameObject)GameObject.Instantiate(uol.m_Obj);
            if(m_Eff != null)
            {
                m_Holder = m_Eff.GetComponent<EffectHolder>();
                if(m_UseDir)
                {
                    ShowEffDir(m_IsLocal, m_Position, m_Dir, m_Scale);
                }
                else
                {
                    ShowEff(m_IsLocal, m_Position, m_Rot, m_Scale);
                }
                if(m_IsPooled)
                {
                    Pool(m_Parent);
                }
            }

        }

        public void Pool(Transform poolParent)
        {
            OnLoadedCall = null;
            m_StartTime = 0;
            m_Delay = 0;
            m_LastPoolTime = Time.realtimeSinceStartup; // 记录进入对象池的时间

            m_UserData = null;
            m_IsPooled = true;
            m_Active = false;
            m_Parent = poolParent;
            if(m_Eff != null)
            {
                SetActive(m_Active);
                m_Eff.transform.SetParent(m_Parent, true);
            }
        }
        /// <summary>
        /// 检查是否超过缓存时间
        /// </summary>
        public bool IsExpired()
        {
            if (!m_IsPooled)
                return false;
            return (Time.realtimeSinceStartup - m_LastPoolTime) > m_CacheMaxTime;
        }

        /// <summary>
        /// 获取剩余缓存时间（秒）
        /// </summary>
        public float GetRemainingCacheTime()
        {
            if (!m_IsPooled)
                return 0;

            var currentTime = Time.realtimeSinceStartup;
            var elapsedTime = currentTime - m_LastPoolTime;
            return System.Math.Max(0, m_CacheMaxTime - elapsedTime);
        }

        private float m_Delay = 0;
        public float GetDelay()
        {
            return m_Delay;
        }
        public virtual void SetDelay(float delay_show)
        {
            m_Delay = delay_show;
        }


        public virtual void SetLightningEff()
        {
            var eff = this;
            if (!eff.IsActive())
            {
                return;
            }
            if (eff.m_Eff == null)
            {
                return;
            }
            var user_data = eff.m_Eff.GetComponent<ChainLightning>();
            if (user_data != null)
            {
                eff.m_UserData = user_data;
            }
        }
        private UResource m_ParentResource;
        public virtual void SetParent(UResource render)
        {
            m_ParentResource = render;
            if (m_ParentResource == null)
            {
                SetParent((Transform)null);
                return;
            }

            if(m_ParentResource.IsObjectLoaded())
            {
                var parentGo  = m_ParentResource.GetShowObj() as GameObject;
                m_Parent = parentGo != null ? parentGo.transform : null;
                SetParent(m_Parent);
                m_ParentResource = render;
            }
            else
            {
                m_ParentResource.AddLoadedCall(() =>
                {
                    if(m_Destroy)
                    {
                        return;
                    }
                    var parentGo = m_ParentResource.GetShowObj() as GameObject;
                    m_Parent = parentGo != null ? parentGo.transform : null;
                    var pr = m_ParentResource;
                    SetParent(m_Parent);
                    m_ParentResource = pr;
                });
            }
        }

        public virtual bool IsLoaded()
        {
            return m_Eff != null;
        }
        public GameObject GetRenderObj()
        {
            return m_Eff;
        }
    }
    //只是起到表现作用的特效管理器
    public class RenderEffManager
    {
        private static RenderEffManager m_Instance;
        public static RenderEffManager GetInstance()
        {
            if(m_Instance == null)
            {
                m_Instance = new RenderEffManager();
            }
            return m_Instance;
        }

        private Dictionary<int, List<RenderEff>> m_PooledEffects = new Dictionary<int, List<RenderEff>>();
        private Transform m_RenderEffParent;

        private List<RenderEff> m_AutoPoolEffs = new List<RenderEff>();
        private float m_CleanupInterval = 5.0f; // 清理间隔（秒）
        private float m_LastCleanupTime = 0; // 上次清理时间
        public Transform GetRenderEffParent()
        {
            return m_RenderEffParent;
        }
        public void Init()
        {
            m_RenderEffParent = GameObject.Find("GameMain/RenderEff").transform;
        }
        public void Destroy()
        {
            foreach(var kv in m_PooledEffects)
            {
                foreach(var eff in kv.Value)
                {
                    eff.Destroy();
                }
            }
            m_PooledEffects.Clear();

            foreach(var eff in m_AutoPoolEffs)
            {
                if(eff != null)
                {
                    eff.Destroy();
                }
            }
            m_AutoPoolEffs.Clear();
        }
        public RenderEff CreateRenderEff(int cfgId)
        {
            if(m_PooledEffects.ContainsKey(cfgId))
            {
                var list = m_PooledEffects[cfgId];
                int count = list.Count;
                if(count > 0)
                {
                    for(int i = count - 1; i >= 0; --i)
                    {
                        var eff = (RenderEff)list[i];
                        if(eff.m_Eff != null && eff.m_Eff.Equals(null))
                        {
                            //说明资源已经挂在某个已经销毁了的父对象上面
                            eff.Destroy();
                            list.RemoveAt(i);
                            continue;
                        }

                        list.RemoveAt(i);
                        eff.m_IsPooled = false;
                        eff.SetParent(m_RenderEffParent);
                        return eff;
                    }

                }
            }

            var new_eff = new RenderEff();
            new_eff.m_IsPooled = false;
            new_eff.LoadEff(cfgId);
            new_eff.SetParent(m_RenderEffParent);
            return new_eff;
        }
        public RenderEff CreateRenderEff(string abName, string sound = null)
        {
            var new_eff = new RenderEff();
            new_eff.m_IsPooled = false;
            new_eff.LoadEff(abName, Tool.GetAssetName(abName), sound);
            new_eff.SetParent(m_RenderEffParent);
            return new_eff;
        }

        public void SetAutoPool(RenderEff eff)
        {
            if(eff == null)
            {
                return;
            }
            m_AutoPoolEffs.Add(eff);
        }
        public void PoolRenderEff(RenderEff effInterface)
        {
            var eff = (RenderEff)effInterface;
            if(eff == null)
            {
                return;
            }
            if(eff.m_IsPooled)
            {
                return;
            }
            eff.Pool(m_RenderEffParent);
            if(m_PooledEffects.ContainsKey(eff.m_CfgId))
            {
                m_PooledEffects[eff.m_CfgId].Add(eff);
            }
            else
            {
                List<RenderEff> list = new List<RenderEff>();
                list.Add(eff);
                m_PooledEffects.Add(eff.m_CfgId, list);
            }
        }

        public void Update()
        {
            // 处理自动回池的特效
            int count = m_AutoPoolEffs.Count;
            for(int i = count - 1; i >= 0; --i)
            {
                var eff = (RenderEff)m_AutoPoolEffs[i];
                if(eff != null && eff.GetDelay() > 0)
                {
                    if( Time.realtimeSinceStartup - eff.m_StartTime <= eff.GetDelay())
                    {
                        continue;
                    }
                    else
                    {
                        if(!eff.IsActive())
                        {
                            eff.SetActive(true);
                        }
                    }
                }
                if (eff.m_DuringTime <= 0)
                {
                    //一般自动释放的都必须配置持续时间，如果没有配置持续时间，默认3秒
                    eff.SetDuringTime(3.0f);
                }
                if (eff == null || Time.realtimeSinceStartup - (eff.m_StartTime + eff.GetDelay()) > eff.m_DuringTime)
                {
                    PoolRenderEff(eff);
                    m_AutoPoolEffs.RemoveAt(i);
                }
            }

            // 定期清理过期的缓存特效
            CleanupExpiredEffects();
        }

        private List<int> m_ExpireEffectCheckIds = new List<int>();
        /// <summary>
        /// 清理过期的缓存特效
        /// </summary>
        private void CleanupExpiredEffects()
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - m_LastCleanupTime < m_CleanupInterval)
            {
                return;
            }

            m_LastCleanupTime = currentTime;

            // 遍历所有缓存的特效，清理过期的
            if(m_ExpireEffectCheckIds.Count > 0)
            {
                m_ExpireEffectCheckIds.Clear();
            }
            foreach(var kv in m_PooledEffects)
            {
                m_ExpireEffectCheckIds.Add(kv.Key);
            }
            foreach (var cfgId in m_ExpireEffectCheckIds)
            {
                var list = m_PooledEffects[cfgId];
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    var eff = list[i];
                    if (eff.m_Eff != null && eff.m_Eff.Equals(null))
                    {
                        // 资源已经被销毁
                        eff.Destroy();
                        list.RemoveAt(i);
                        continue;
                    }

                    // 检查是否过期
                    if (eff.IsExpired())
                    {
                        eff.Destroy();
                        list.RemoveAt(i);
                    }
                }

                // 如果某个配置的列表为空，移除该配置
                if (list.Count == 0)
                {
                    m_PooledEffects.Remove(cfgId);
                }
            }
        }

        /// <summary>
        /// 获取缓存的特效数量
        /// </summary>
        public int GetPooledCount()
        {
            int total = 0;
            foreach (var kvp in m_PooledEffects)
            {
                total += kvp.Value.Count;
            }
            return total;
        }

        /// <summary>
        /// 获取指定配置ID的缓存数量
        /// </summary>
        public int GetPooledCountByCfgId(int cfgId)
        {
            if (m_PooledEffects.ContainsKey(cfgId))
            {
                return m_PooledEffects[cfgId].Count;
            }
            return 0;
        }

        /// <summary>
        /// 清空所有缓存的特效
        /// </summary>
        public void ClearAllPool()
        {
            foreach (var kvp in m_PooledEffects)
            {
                foreach (var eff in kvp.Value)
                {
                    eff.Destroy();
                }
            }
            m_PooledEffects.Clear();
        }

        /// <summary>
        /// 清空指定配置ID的缓存
        /// </summary>
        public void ClearPoolByCfgId(int cfgId)
        {
            if (m_PooledEffects.ContainsKey(cfgId))
            {
                var list = m_PooledEffects[cfgId];
                foreach (var eff in list)
                {
                    eff.Destroy();
                }
                m_PooledEffects.Remove(cfgId);
            }
        }
    }
}
