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
        // 回池后最多保留10秒，避免战斗高峰产生的特效对象长时间占用内存。
        public float m_CacheMaxTime = 10.0f;
        private t_effectBean m_Cfg;
        public Vector3 m_Position;
        public Vector3 m_Rot;
        public Vector3 m_Dir;
        public Vector3 m_Scale;
        private bool m_IsLocal;
        private bool m_UseDir;
        private ParticleSystem[] m_ParticleSystems;
        private float[] m_ParticleBaseSpeeds;
        private float m_ParticleSpeed = 1f;
        internal bool m_WaitForParticleFinish;
        internal float m_ParticleFinishWaitStartTime;
        private bool m_WaitForPoolDeactivate;

        public void SetParticleSpeed(float speed)
        {
            speed = Mathf.Max(0.001f, speed);
            bool changed = !Mathf.Approximately(m_ParticleSpeed, speed);
            m_ParticleSpeed = speed;
            if (m_Eff == null)
            {
                return;
            }
            if (m_ParticleSystems == null)
            {
                m_ParticleSystems = m_Eff.GetComponentsInChildren<ParticleSystem>(true);
                m_ParticleBaseSpeeds = new float[m_ParticleSystems.Length];
                for (int i = 0; i < m_ParticleSystems.Length; i++)
                {
                    m_ParticleBaseSpeeds[i] = m_ParticleSystems[i].main.simulationSpeed;
                }
                changed = true;
            }
            if (!changed)
            {
                return;
            }
            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                if (m_ParticleSystems[i] == null) continue;
                var main = m_ParticleSystems[i].main;
                main.simulationSpeed = m_ParticleBaseSpeeds[i] * speed;
            }
        }

        internal bool StopParticleEmission(Transform tailParent)
        {
            if (m_Eff == null)
            {
                return false;
            }
            SetParticleSpeed(m_ParticleSpeed);
            if (m_ParticleSystems.Length == 0)
            {
                return false;
            }
            foreach (var particle in m_ParticleSystems)
            {
                if (particle != null)
                {
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            // 收尾期间不再挂在角色上，也不能提前入池复用；已发出的粒子自然消失。
            m_ParentResource = null;
            m_Parent = tailParent;
            m_Eff.transform.SetParent(tailParent, true);
            m_WaitForParticleFinish = true;
            m_ParticleFinishWaitStartTime = Time.realtimeSinceStartup;
            return true;
        }

        internal bool HasLivingParticles()
        {
            if (m_Eff == null || m_ParticleSystems == null)
            {
                return false;
            }
            foreach (var particle in m_ParticleSystems)
            {
                if (particle != null && particle.gameObject.activeInHierarchy && particle.IsAlive(false))
                {
                    return true;
                }
            }
            return false;
        }

        private bool m_Active = true;
        public virtual void SetActive(bool active)
        {
            m_Active = active;
            if(m_Eff != null)
            {
                bool usePlayStop = m_Holder != null && m_Holder.m_RenderPoolStyle == RenderPoolStyle.PlayStop;
                if (active)
                {
                    m_WaitForPoolDeactivate = false;
                    m_Eff.SetActive(true);
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
                if (!active && !usePlayStop)
                {
                    m_Eff.SetActive(false);
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
        //只刷新已显示特效的世界位姿：不重置播放时间、不重播音效、不重放粒子。
        //给需要跟随目标的残留特效（例如插在身上的箭）逐帧摆位用。
        public void SetPose(Vector3 worldPos, Vector3 worldEuler)
        {
            m_IsLocal = false;
            m_UseDir = false;
            m_Position = worldPos;
            m_Rot = worldEuler;

            //资源还没加载完时只记录位姿，OnLoadRes 会用最新的 m_Position/m_Rot 显示
            if (m_Eff == null)
            {
                return;
            }
            var trans = m_Eff.transform;
            trans.position = m_Position;
            trans.eulerAngles = m_Rot;
        }
        public virtual void Destroy()
        {
            OnLoadedCall = null;
            m_StartTime = 0;
            m_Delay = 0;
            m_UserData = null;
            m_WaitForPoolDeactivate = false;
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
        private void PlaySound(Transform trans)
        {
            if (trans == null)
            {
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
                UDebug.LogError("特效资源加载失败，路径是：" + m_Cfg.t_abname);
                return;
            }
            m_Eff = (GameObject)GameObject.Instantiate(uol.m_Obj);
            if(m_Eff != null)
            {
                m_Holder = m_Eff.GetComponent<EffectHolder>();
                // 加载完成前本轮攻击可能已经结束，不能先播放一次再隐藏。
                if (m_IsPooled)
                {
                    Pool(m_Parent);
                    RenderEffManager.GetInstance().TrackDelayedDeactivate(this);
                    return;
                }
                if (!Mathf.Approximately(m_ParticleSpeed, 1f))
                {
                    SetParticleSpeed(m_ParticleSpeed);
                }
                if(m_UseDir)
                {
                    ShowEffDir(m_IsLocal, m_Position, m_Dir, m_Scale);
                }
                else
                {
                    ShowEff(m_IsLocal, m_Position, m_Rot, m_Scale);
                }
            }

        }

        public void Pool(Transform poolParent)
        {
            OnLoadedCall = null;
            m_WaitForParticleFinish = false;
            m_ParticleFinishWaitStartTime = 0f;
            m_WaitForPoolDeactivate = false;
            if (!Mathf.Approximately(m_ParticleSpeed, 1f))
            {
                SetParticleSpeed(1f);
            }
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
                m_WaitForPoolDeactivate = m_Holder != null &&
                                          m_Holder.m_RenderPoolStyle == RenderPoolStyle.PlayStop &&
                                          m_Eff.activeSelf;
            }
        }

        internal void CancelPoolDeactivate()
        {
            m_WaitForPoolDeactivate = false;
        }

        internal bool IsWaitingForPoolDeactivate()
        {
            return m_WaitForPoolDeactivate;
        }

        internal bool TryDeactivatePooledObject(float currentTime, float delay)
        {
            if (!m_WaitForPoolDeactivate || !m_IsPooled || m_Destroy || m_Eff == null)
            {
                m_WaitForPoolDeactivate = false;
                return true;
            }
            if (currentTime - m_LastPoolTime < delay)
            {
                return false;
            }

            m_Eff.SetActive(false);
            m_WaitForPoolDeactivate = false;
            return true;
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
        // PlayStop允许短时间内直接重新播放，连续闲置达到该时间后仍会强制隐藏根对象。
        private const float m_PlayStopDeactivateDelay = 5.0f;
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
        private List<RenderEff> m_DelayedDeactivateEffs = new List<RenderEff>();
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
            m_DelayedDeactivateEffs.Clear();
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
                        eff.CancelPoolDeactivate();
                        m_DelayedDeactivateEffs.Remove(eff);
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
        public void SetAutoPool(RenderEff eff)
        {
            if(eff == null || eff.m_IsPooled || m_AutoPoolEffs.Contains(eff))
            {
                return;
            }
            m_AutoPoolEffs.Add(eff);
        }

        public void StopEmissionAndAutoPool(RenderEff eff)
        {
            if (eff == null || eff.m_IsPooled)
            {
                return;
            }
            if (eff.StopParticleEmission(m_RenderEffParent))
            {
                SetAutoPool(eff);
            }
            else
            {
                PoolRenderEff(eff);
            }
        }

        public void PoolRenderEff(RenderEff effInterface)
        {
            var eff = (RenderEff)effInterface;
            if(eff == null)
            {
                return;
            }
            // 手动结束必须同时取消旧的自动回收，避免对象复用后被上一轮回收。
            m_AutoPoolEffs.Remove(eff);
            if(eff.m_IsPooled)
            {
                return;
            }
            eff.Pool(m_RenderEffParent);
            TrackDelayedDeactivate(eff);
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

        internal void TrackDelayedDeactivate(RenderEff eff)
        {
            if (eff != null && eff.IsWaitingForPoolDeactivate() && !m_DelayedDeactivateEffs.Contains(eff))
            {
                m_DelayedDeactivateEffs.Add(eff);
            }
        }

        public void Update()
        {
            float currentTime = Time.realtimeSinceStartup;
            for (int i = m_DelayedDeactivateEffs.Count - 1; i >= 0; --i)
            {
                var eff = m_DelayedDeactivateEffs[i];
                if (eff == null || eff.TryDeactivatePooledObject(currentTime, m_PlayStopDeactivateDelay))
                {
                    m_DelayedDeactivateEffs.RemoveAt(i);
                }
            }

            // 处理自动回池的特效
            int count = m_AutoPoolEffs.Count;
            for(int i = count - 1; i >= 0; --i)
            {
                var eff = (RenderEff)m_AutoPoolEffs[i];
                if (eff == null || eff.m_IsPooled)
                {
                    m_AutoPoolEffs.RemoveAt(i);
                    continue;
                }
                if (eff.m_WaitForParticleFinish)
                {
                    // 异常粒子不能无限阻塞回池；五秒与 PlayStop 的最终隐藏保底保持一致。
                    if (!eff.HasLivingParticles() || currentTime - eff.m_ParticleFinishWaitStartTime >= m_PlayStopDeactivateDelay)
                    {
                        PoolRenderEff(eff);
                    }
                    continue;
                }
                if(eff.GetDelay() > 0)
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
                if (Time.realtimeSinceStartup - (eff.m_StartTime + eff.GetDelay()) > eff.m_DuringTime)
                {
                    PoolRenderEff(eff);
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
            m_DelayedDeactivateEffs.Clear();
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
                    m_DelayedDeactivateEffs.Remove(eff);
                }
                m_PooledEffects.Remove(cfgId);
            }
        }
    }
}
