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
        // 同一份配置的可缓存实例上限：表现时长配得异常长时提供兜底，避免实例无限增长
        private const int m_MaxCachedCountPerCfg = 64;
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
                var btime = BattleManager.ReadBattleTime();
                int count = objs.m_List.Count;
                for (int i = 0; i < count; i++)
                {
                    var eff = objs.m_List[i];
                    if (eff == null || btime - eff.GetHideTime() < m_ReuseDelay)
                    {
                        continue;
                    }
                    obj = TakeReusableEffect(objs, eff);
                    if (obj != null)
                    {
                        return obj;
                    }
                }

                // 全部实例都还在表现期内时仍然要发弹：到达池容量上限后取最早入池的兜底，
                // 避免表现时长配得过长把实例数量无限拉高。
                if (count >= m_MaxCachedCountPerCfg)
                {
                    obj = TakeReusableEffect(objs, objs.m_List[0]);
                    if (obj != null)
                    {
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

        private T TakeReusableEffect(PlayableEffectObjList objs, PlayableEffectObj eff)
        {
            if (eff == null || !objs.m_List.Remove(eff))
            {
                return null;
            }
            var obj = (T)eff;
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

            // 回收起点默认是当前时间；若命中/枪口特效还没显示完，就顺延到它收尾之后，
            // 否则下次复用时重新发弹会立刻清掉这一发还没到时间的表现。
            float reuseTime = BattleManager.ReadBattleTime();
            float visualRemain = obj.ReadVisualRemainTime();
            if (visualRemain > 0f)
            {
                reuseTime += visualRemain;
            }
            obj.SetHideTime(reuseTime);
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
