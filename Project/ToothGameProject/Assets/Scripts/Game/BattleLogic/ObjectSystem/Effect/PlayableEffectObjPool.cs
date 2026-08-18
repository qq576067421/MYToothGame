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
        public void OnCreate()
        {

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
                    if (btime - time >= 3.0f)
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
    }
}
