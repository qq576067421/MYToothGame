using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDll
{
    public class PlayableEffectObjList
    {
        public long m_CfgId;
        public List<PlayableEffectObj> m_List = new  List<PlayableEffectObj>();
    }

    public class PlayableEffectObjPool<T>  where T : PlayableEffectObj
    {
        private const float m_ReuseDelay = 1.5f;
        private const float m_CacheMaxTime = 10.0f;
        private const float m_CleanupInterval = 1.0f;
        private float m_LastCleanupTime;

        public void OnCreate()
        {
            m_LastCleanupTime = BattleManager.ReadBattleTime();
        }
        public void OnRelease()
        {
            foreach(var list in m_Caches)
            {
                foreach(var obj in list.m_List)
                {
                    obj.Destroy();
                }
            }
            m_CacheDict.Clear();
            m_Caches.Clear();
        }
        //cfgid,obj_list
        private List<PlayableEffectObjList> m_Caches = new List<PlayableEffectObjList>();
        private Dictionary<long, PlayableEffectObjList> m_CacheDict = new Dictionary<long, PlayableEffectObjList>();

        public T GetEffect(emEntityType type, long cfgId, object bean, ResourceType resType)
        {
            T obj = null;
            if(m_CacheDict.ContainsKey(cfgId))
            {
                var objs = m_CacheDict[cfgId];
                if (objs.m_List.Count > 0)
                {
                    var eff = objs.m_List[0];
                    var time = eff.GetHideTime();
                    var btime = BattleManager.ReadBattleTime();
                    if (btime - time >= m_ReuseDelay)
                    {
                        obj = (T)eff;
                        objs.m_List.RemoveAt(0);
                        obj.IsPooled = false;
                        return obj;
                    }
                }   
            }

            obj = (T)BattleManager.GetBattle().GetObjectManager().NewEffObject(type);
            obj.SetBean(bean);
            obj.CreateRender(null, resType);
            obj.InitInstance();
            obj.IsPooled = false;
            return obj;
        }

        public void PoolEffect(long cfgId, PlayableEffectObj obj)
        {
            if(obj == null)
            {
                return;
            }
            else if(obj.IsPooled)
            {
                return;
            }

            obj.SetHideTime(BattleManager.ReadBattleTime());
            if (m_CacheDict.ContainsKey(cfgId))
            {
                var list = m_CacheDict[cfgId];
                obj.IsPooled = true;
                list.m_List.Add(obj);
            }
            else
            {
                var list = new PlayableEffectObjList();
                list.m_CfgId = cfgId;
                list.m_List.Add(obj);

                obj.IsPooled = true; 

                m_CacheDict.Add(cfgId, list);
                m_Caches.Add(list);
            }
        }

        public void Update(float battleTime)
        {
            if (battleTime - m_LastCleanupTime < m_CleanupInterval)
            {
                return;
            }

            m_LastCleanupTime = battleTime;
            for (int cacheIndex = m_Caches.Count - 1; cacheIndex >= 0; cacheIndex--)
            {
                var cache = m_Caches[cacheIndex];
                for (int objectIndex = cache.m_List.Count - 1; objectIndex >= 0; objectIndex--)
                {
                    var obj = cache.m_List[objectIndex];
                    if (obj == null)
                    {
                        cache.m_List.RemoveAt(objectIndex);
                        continue;
                    }
                    if (battleTime - obj.GetHideTime() < m_CacheMaxTime)
                    {
                        continue;
                    }

                    // 子弹销毁时会同时销毁 ProjectileMover 拆到全局节点下的命中和枪口特效。
                    obj.Destroy();
                    cache.m_List.RemoveAt(objectIndex);
                }

                if (cache.m_List.Count == 0)
                {
                    m_CacheDict.Remove(cache.m_CfgId);
                    m_Caches.RemoveAt(cacheIndex);
                }
            }
        }
    }
}
