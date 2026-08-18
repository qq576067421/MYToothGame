using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using LCL;
using MonoBean;
using System.Text;

namespace GameDll
{
    public enum emEntityBigType
    {
        PropertyEntity,
        Hero,
        StaticObj,
        Eff
    }
    public class GetNearestObjectParam
    {
        public Vector3 pos;
        public int dist;
        public Func<Entity, bool> exCondition = null;
        public Entity hrObject;
    }
    public class GetObjectNearstInObjectsParam
    {
        public Vector3 point;
        public List<Entity> objs;
        public float dist;
        public Entity hrObject;
    }

    public  class ObjectManager
    {
        protected int m_ClientIDs = 0;

        public  void Init()
        {
            Debug.LogWarning("重置实体Id");
            m_ClientIDs = 0;
        }
        public  int AssignClientId()
        {
            var id = ++m_ClientIDs;
            //Debug.LogError("分配id：" + id);
            return id;
        }

        //禁止其他地方直接调用该字段
        private List<PropertyEntity> m_LogicUpdatePropertyEntityList = new List<PropertyEntity>();
        private List<PropertyEntity> m_LogicUpdateHeroList = new List<PropertyEntity>();

        
        private List<PlayableEffectObj> m_LogicUpdateEffObjectList = new List<PlayableEffectObj>();
        private List<Entity> m_LogicUpdateStaticObjectList = new List<Entity>();

        //只用于查询的
        private Dictionary<int, Entity> m_ObjectDic = new Dictionary<int, Entity>();

        public Entity ReadEntity(int entId)
        {
            if(m_ObjectDic.ContainsKey(entId))
            {
                return m_ObjectDic[entId];
            }
            else
            {
                return null;
            }
        }
        public void ReadPropertyEntities(Func<int, Entity, bool> call)
        {
            foreach(var ent in m_LogicUpdatePropertyEntityList)
            {
                var key = ent.ReadId();
                var contiue = call(key, ent);
                if(!contiue)
                {
                    return;
                }
            }
        }
        public void ReadHeroes(Func<int, Entity, bool> call)
        {
            foreach (var ent in m_LogicUpdateHeroList)
            {
                var key = ent.ReadId();
                var contiue = call(key, ent);
                if (!contiue)
                {
                    return;
                }
            }
        }

        public void ReadStaticObjs(Func<int, Entity, bool> call)
        {
            foreach (var ent in m_LogicUpdateStaticObjectList)
            {
                var key = ent.ReadId();
                var contiue = call(key, ent);
                if (!contiue)
                {
                    return;
                }
            }
        }

        public void ReadHeroes(List<PropertyEntity> actors)
        {
            foreach (var entity in m_LogicUpdateHeroList)
            {
                if (entity == null || entity.ReadIsDestroy())
                {
                    continue;
                }
                actors.Add(entity);
            }
        }

        public List<PropertyEntity> ReadHeroes()
        {
            return m_LogicUpdateHeroList;
        }
        public List<PropertyEntity> ReadPropertyEntities()
        {
            return m_LogicUpdatePropertyEntityList;
        }

        public  PropertyEntity ReadPropertyEntityById(int id)
        {
            foreach (var entity in m_LogicUpdatePropertyEntityList)
            {
                if (entity == null || entity.ReadIsDestroy())
                {
                    continue;
                }
                if(entity.ReadId() == id)
                {
                    return entity;
                }
            }

            return null;
        }

        public List<PropertyEntity> ReadCreatureByType(emEntityType _type)
        {
            List<PropertyEntity> list = new List<PropertyEntity>();
            foreach (var entity in m_LogicUpdatePropertyEntityList)
            {
                if(entity == null || entity.ReadIsDestroy())
                {
                    continue;
                }
                if (entity.ReadObjectType() == _type)
                {
                    list.Add(entity);
                }
            }
            return list;
        }
        public Entity ReadStatic(int id)
        {
            foreach(var box in m_LogicUpdateStaticObjectList)
            {
                if(box.ReadId() == id)
                {
                    return box;
                }
            }
            return null;
        }
        public List<Entity> ReadStatics()
        {
            return m_LogicUpdateStaticObjectList;
        }
        public List<PlayableEffectObj> ReadEffObjectByType(emEntityType _type)
        {
            List<PlayableEffectObj> list = new List<PlayableEffectObj>();
            foreach (var entity in m_LogicUpdateEffObjectList)
            {
                if (entity == null || entity.ReadIsDestroy())
                {
                    continue;
                }
                if (entity.ReadObjectType() == _type)
                {
                    list.Add(entity);
                }
            }
            return list;
        }
        public  void OnDrawGizmos()
        {
            foreach(var obj in m_LogicUpdatePropertyEntityList)
            {
                if (obj == null || obj.ReadIsDestroy())
                {
                    continue;
                }
                obj.OnDrawGizmos();
            }

            foreach (var obj in m_LogicUpdateEffObjectList)
            {
                if (obj == null || obj.ReadIsDestroy())
                {
                    continue;
                }
                obj.OnDrawGizmos();
            }
        }

        public  void GetObjectNearstInObjects(GetObjectNearstInObjectsParam param)
        {
            param.dist = int.MaxValue;
            if (param.objs == null)
            {
                return;
            }
            Entity nearstObj = null;
            int count = param.objs.Count;
            for (int i = 0; i < count; ++i)
            {
                Entity obj = param.objs[i];
                if(obj != null || obj.ReadIsDestroy())
                {
                    continue;
                }
                Vector3 pos = obj.GetPosition();
                var cur = Vector3.Distance(param.point, pos);
                if (cur <= param.dist)
                {
                    nearstObj = obj;
                    param.dist = cur;
                }
            }
            param.hrObject = nearstObj;
        }

        public  void ClearAll()
        {
            Debug.Log("清理所有实体开始");
            foreach (var kv in m_LogicUpdatePropertyEntityList)
            {
                if(kv == null)
                {
                    continue;
                }
                if(kv.ReadIsBuilding())
                {
                    continue;
                }
                if(kv.ReadIsTrap())
                {
                    continue;
                }
                if(kv.ReadIsTower())
                {
                    continue;
                }
                var com = kv;
                com.Destroy();
                com = null;

            }
            m_LogicUpdatePropertyEntityList.Clear();

           

            foreach (var kv in m_LogicUpdateEffObjectList)
            {
                if (kv == null)
                {
                    continue;
                }
                var com = kv;
                com.Destroy();
                com = null;

            }
            m_LogicUpdateEffObjectList.Clear();

            foreach (var kv in m_LogicUpdateStaticObjectList)
            {
                if (kv == null)
                {
                    continue;
                }
                var com = kv;
                com.Destroy();
                com = null;

            }
            m_LogicUpdateStaticObjectList.Clear();


            m_LogicUpdateHeroList.Clear();

            m_ClientIDs = 0;
            Debug.Log("清理所有实体结束");
        }


        private List<IResource> m_Temp = new List<IResource>();
        public  void Update(float dt)
        {
            m_Temp.Clear();
            int count = m_LogicUpdatePropertyEntityList.Count;
            for(int i = 0; i < count; ++i)
            {
                var kv = m_LogicUpdatePropertyEntityList[i];
                if (kv.ReadIsBuilding())
                {
                    continue;
                }
                if (kv.ReadIsTrap())
                {
                    continue;
                }
                if (kv.ReadIsTower())
                {
                    continue;
                }
                m_Temp.Add(kv);
            }
            count = m_Temp.Count;
            for(int i =0; i < count; ++i)
            {
                var temp = m_Temp[i];
                if(temp != null && !temp.ReadIsDestroy())
                {
                    temp.Update(dt);
                }
            }

            m_Temp.Clear();
            count = m_LogicUpdateEffObjectList.Count;
            for (int i = 0; i < count; ++i)
            {
                m_Temp.Add(m_LogicUpdateEffObjectList[i]);
            }
            for (int i = 0; i < count; ++i)
            {
                var temp = m_Temp[i];
                if (temp != null && !temp.ReadIsDestroy())
                {
                    temp.Update(dt);
                }
            }

            m_Temp.Clear();
            count = m_LogicUpdateStaticObjectList.Count;
            for (int i = 0; i < count; ++i)
            {
                m_Temp.Add(m_LogicUpdateStaticObjectList[i]);
            }
            for (int i = 0; i < count; ++i)
            {
                var temp = m_Temp[i];
                if (temp != null && !temp.ReadIsDestroy())
                {
                    temp.Update(dt);
                }
            }
        }
        public  void UpdateRender()
        {
            //这步操作主要是针对列表元素移除操作
            int count = m_LogicUpdatePropertyEntityList.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var ent = m_LogicUpdatePropertyEntityList[i];
                if (ent != null)
                {
                    if (ent.ReadIsBuilding())
                    {
                        continue;
                    }
                    if (ent.ReadIsTrap())
                    {
                        continue;
                    }
                    if (ent.ReadIsTower())
                    {
                        continue;
                    }

                    ent.UpdateRender();
                }
            }

            count = m_LogicUpdateEffObjectList.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var ent = m_LogicUpdateEffObjectList[i];
                if (ent != null)
                {
                    ent.UpdateRender();
                }
            }
        }

        public PlayableEffectObj NewEffObject(emEntityType objecttype)
        {
            PlayableEffectObj eff = null;
            switch(objecttype)
            {
                case emEntityType.em_EntityType_Effect:
                    {
                        eff = new EffectObj();
                        break;
                    }
                case emEntityType.em_EntityType_Laser:
                    {
                        eff = new LaserObj();
                        break;
                    }
                case emEntityType.em_EntityType_Bullet:
                    {
                        eff = new BulletObj();
                        break;
                    }
                case emEntityType.em_EntityType_Paodan:
                    {
                        eff = new PaodanObj();
                        break;
                    }
            }
            if (eff != null)
            {
                eff.SetObjectType(objecttype);
            }
            return eff;
        }
        public  PropertyEntity NewCreature(emEntityType objecttype)
        {
            PropertyEntity role = null;
            switch (objecttype)
            {
                case emEntityType.em_EntityType_Actor:
                    {
                        role = new MoveableCreature();
                        break;
                    }
                case emEntityType.em_EntityType_MasterHero:
                    {
                        role = new MasterHero();
                        break;
                    }
                case emEntityType.em_EntityType_PlayerHero:
                    {
                        role = new PlayerHero();
                        break;
                    }

                case emEntityType.em_EntityType_SmallMonster:
                    {
                        role = new SmallMonster();
                        break;
                    }
                case emEntityType.em_EntityType_UpgradeChallengeTarget:
                    {
                        role = new UpgradeChallengeTarget();
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            if (role != null)
            {
                role.SetObjectType(objecttype);
            }
            return role;
        }

        public void AddStatic(Entity box)
        {
            m_LogicUpdateStaticObjectList.Add(box);

            int id = box.ReadId();
            
            if (m_ObjectDic.ContainsKey(id))
            {
                Debug.LogWarning("重复添加实体， id：" + id);
            }
            else
            {
                m_ObjectDic.Add(id, box);
            }

        }

        public void RemoveStatic(Entity role, bool needDestroy)
        {
            if (role != null)
            {
                int id = role.ReadId();
                
                if (!m_ObjectDic.ContainsKey(id))
                {
                    Debug.LogWarning("没有实体， id：" + id);
                }
                else
                {
                    
                    m_ObjectDic.Remove(id);
                }
                if (needDestroy)
                {
                    role.Destroy();
                }
                m_LogicUpdateStaticObjectList.Remove(role);
            }
        }
        //仅仅是从列表移除
        public void RemovePropertyEntity(PropertyEntity role, bool needDestroy)
        {
            if (role != null)
            {
                int id = role.ReadId();
                
                if (!m_ObjectDic.ContainsKey(id))
                {
                    return;
                }
                else
                {
                    m_ObjectDic.Remove(id);
                }
                if (needDestroy && !role.ReadIsDestroy())
                {
                    role.Destroy();
                }
                m_LogicUpdatePropertyEntityList.Remove(role);
                if(role.ReadIsHero())
                {
                    m_LogicUpdateHeroList.Remove(role);
                }
            }
        }

        
        public  void AddPropertyEntity(PropertyEntity obj)
        {
            m_LogicUpdatePropertyEntityList.Add(obj);
            if(obj.ReadIsHero())
            {
                m_LogicUpdateHeroList.Add(obj);
            }
            int id = obj.ReadId();
            
            if (m_ObjectDic.ContainsKey(id))
            {
                Debug.LogWarning("重复添加实体， id：" + id);
            }
            else
            {
                m_ObjectDic.Add(id, obj);
            }
        }

        public void AddEffObject(PlayableEffectObj obj)
        {
            m_LogicUpdateEffObjectList.Add(obj);
        }
        public List<PlayableEffectObj> GetEffs()
        {
            return m_LogicUpdateEffObjectList;
        }
        public void RemoveEffObject(PlayableEffectObj role, bool needDestroy)
        {
            if (role != null)
            {

                if (needDestroy)
                {
                    role.Destroy();
                }
                m_LogicUpdateEffObjectList.Remove(role);
            }
        }
        public bool GetObjectByGroup(int group, List<Entity> list)
        {
            bool find = false;
            foreach (var ent in m_LogicUpdatePropertyEntityList)
            {
                if(ent == null)
                {
                    continue;
                }
                if (ent.ReadObjectType() == emEntityType.em_EntityType_Actor)
                {
                    int obj_camp = (int)ent.ReadGroup();
                    if (group == obj_camp)
                    {
                        list.Add(ent);
                        find = true;
                    }
                }
            }
            return find;
        }

        public StringBuilder m_SnapStringBuilder = new StringBuilder();
        public void Snapshot(System.IO.StreamWriter sw)
        {
            foreach(var kv in m_LogicUpdatePropertyEntityList)
            {
                if (kv != null)
                {
                    m_SnapStringBuilder.Clear();
                    kv.Snapshot(m_SnapStringBuilder);
                    var snap = m_SnapStringBuilder.ToString();
                    if(snap == kv.m_LastSnap)
                    {
                        continue;
                    }
                    kv.m_LastSnap = snap;
                    sw.WriteLine(snap);
                }
            }
        }

        public int ReadEntityCount(emEntityBigType type)
        {
            switch(type)
            {
                case emEntityBigType.PropertyEntity:
                    {
                        return m_LogicUpdatePropertyEntityList.Count;
                    }
                case emEntityBigType.Hero:
                    {
                        return m_LogicUpdateHeroList.Count;
                    }
                case emEntityBigType.StaticObj:
                    {
                        return m_LogicUpdateStaticObjectList.Count;
                    }
                case emEntityBigType.Eff:
                    {
                        return m_LogicUpdateEffObjectList.Count;
                    }
            }
            return 0;
        }


    }
}