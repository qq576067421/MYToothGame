using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameDll
{
    public class BuffManager
    {
        private List<Buff> m_Buffs = new List<Buff>();
        private int m_BuffId = 0;
        private Entity m_Owner;

        public float ReadBuffPropertyDataV(int type)
        {
            float data = 1.0f;
            foreach (var buff in m_Buffs)
            {
                var temp = buff.ReadPropertyV(type);
                data *= temp;
            }
            return data;
        }
        public float ReadBuffPropertyData(int type)
        {
            float data = 0;
            foreach (var buff in m_Buffs)
            {
                var temp = buff.ReadProperty(type);
                data += temp;
            }
            return data;
        }
        //(UI禁用)
        public float CalBuffPropertyDataMaxScale(int type, int slot, HurtType hurt_type)
        {
            float data = 0;
            foreach (var buff in m_Buffs)
            {
                var temp = buff.CalPropertyMaxScale(type, slot, hurt_type);
                if(data < temp)
                {
                    data = temp;
                }
            }
            return data;
        }
        public  void Init()
        {
            m_Buffs.Clear();
        }
        public void SetOwner(Entity owner)
        {
            m_Owner = owner;
        }

        public int AssignId()
        {
            return ++m_BuffId;
        }

        /// <summary>
        /// 添加一个buff
        /// </summary>
        /// <param name="buff"></param>
        public void AddBuff(Buff buff, PropertyEntity source, PropertyEntity target, 
            BuffModify modify = null, Dictionary<long, BuffParamModify> paramModifies = null)
        {
            if (buff == null)
            {
                Debug.LogWarning("BuffManager.AddBuff 收到空 buff。");
                return;
            }

            int remove_id = 0;
            var beanNew = buff.GetDescBean();
            if (beanNew == null)
            {
                Debug.LogWarning("BuffManager.AddBuff 跳过缺少描述配置的 buff: " + buff.GetTemplateId());
                return;
            }
            int count = m_Buffs.Count;

            if(beanNew.t_buff_replace_type != 0)
            {
                for(int i = 0;i < count; ++i)
                {
                    var oldBuff = m_Buffs[i];
                    var beanOld = oldBuff.GetDescBean();

                    if(beanOld.t_buff_replace_type == beanNew.t_buff_replace_type)
                    {
                        if(beanOld.t_buff_priority <= beanNew.t_buff_priority)
                        {
                            remove_id = oldBuff.GetId();
                            RemoveBuffImp(oldBuff, i);
                            break;
                        }
                    }
                }
            }

            m_Buffs.Add(buff);
            buff.InitInstance(++m_BuffId);
            buff.AddBuff(source, target, modify, paramModifies);
            if (m_Owner.ReadId() == BattleAPIBridge.__BattleRenderData_GetUIUnitId(true))
            {
                BRenderEvent.Event.OnChangeBuff(buff.GetId(), buff.GetTemplateId(), remove_id);
            }


        }
        public Buff TryAddBuff(long buffCfgId, PropertyEntity source, PropertyEntity target,
            BuffModify modify = null, Dictionary<long, BuffParamModify> paramModifies = null, long level = 1)
        {
            var buff = BuffTemplate.createBuff(buffCfgId);
            if(buff != null)
            {
                buff.SetLevel(level);
                AddBuff(buff, source, target, modify, paramModifies);
                return buff.GetDescBean() != null ? buff : null;
            }
            else
            {
                return null;
            }
        }
        public Buff GetBuff(int buffId)
        {
            int count = m_Buffs.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var buff = m_Buffs[i];
                if (buff.GetId() == buffId)
                {
                    return buff;
                }
            }
            return null;
        }
        public void RemoveBuff(int buffId)
        {
            int count = m_Buffs.Count;
            for( int i = count - 1; i>=0; --i)
            {
                var buff = m_Buffs[i];
                if(buff.GetId() == buffId)
                {
                    var remove_id = buff.GetId();
                    RemoveBuffImp(buff, i);

                    if (m_Owner.ReadId() == BattleAPIBridge.__BattleRenderData_GetUIUnitId(true))
                    {
                        BRenderEvent.Event.OnChangeBuff(0, 0, remove_id);
                    }

                    break;
                }
            }
        }

        public void RemoveBuff(long buff_cfg_id)
        {
            int count = m_Buffs.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var buff = m_Buffs[i];
                if (buff.GetBean().t_id == buff_cfg_id)
                {

                    RemoveBuffImp(buff, i);
                    break;
                }
            }
        }
        public List<Buff> ReadBuffs()
        {
            return m_Buffs;
        }
        public void RemoveBuffImp(Buff buff, int idx = -1)
        {
            var remove_id = buff.GetId();

            buff.OnRemove();

            
            if (idx >= 0)
            {
                m_Buffs.RemoveAt(idx);
            }
            else
            {
                m_Buffs.Remove(buff);
            }

            if (m_Owner.ReadId() == BattleAPIBridge.__BattleRenderData_GetUIUnitId(true))
            {
                BRenderEvent.Event.OnChangeBuff(0, 0, remove_id);
            }
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// 

        private List<Buff> m_UpdateBuffs = new List<Buff>();
        public void Update(float dt)
        {
            int count = m_Buffs.Count;
            if(count > 0)
            {
                m_UpdateBuffs.Clear();
                for(int i = 0; i < count; ++i)
                {
                    m_UpdateBuffs.Add(m_Buffs[i]);
                }
            }

            for(int i = 0; i < count; ++i)
            {
                m_UpdateBuffs[i].Update(dt);
            }
        }

        public void UpdateRender()
        {
            int count = m_Buffs.Count;
            for (int i = 0; i < count; ++i)
            {
                m_Buffs[i].UpdateRender();
            }
        }

        /// <summary>
        /// 是否存在某个buff
        /// </summary>
        /// <param name="tempID"></param>
        public Buff GetExist(long templateID)
        {
            Buff buff = null;
            int count = m_Buffs.Count;
            for (int i = 0; i < count; ++i)
            {
                if (m_Buffs[i].GetBean().t_id == templateID)
                {
                    buff = m_Buffs[i];
                    return buff;
                }
            }
            return null;
            
        }
        public int ReadBuffStackCount(long templateID)
        {
            var buff = GetExist(templateID);
            return buff != null ? buff.ReadStackCount() : 0;
        }
        public int ReadBuffStackCountByParamId(long buffParamId)
        {
            int count = m_Buffs.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var buff = m_Buffs[i];
                if (buff == null || buff.GetBean() == null || buff.GetBean().t_buff_param_id == null)
                {
                    continue;
                }

                var paramIds = buff.GetBean().t_buff_param_id;
                for (int j = 0; j < paramIds.Count; j++)
                {
                    if (paramIds[j] == buffParamId)
                    {
                        return buff.ReadStackCount();
                    }
                }
            }

            return 0;
        }
        public void ClearBuffStack(long templateID)
        {
            int count = m_Buffs.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var buff = m_Buffs[i];
                if (buff == null || buff.GetBean().t_id != templateID)
                {
                    continue;
                }

                RemoveBuffImp(buff, i);
                break;
            }
        }
        public void ClearBuffStackByParamId(long buffParamId)
        {
            int count = m_Buffs.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var buff = m_Buffs[i];
                if (buff == null || buff.GetBean() == null || buff.GetBean().t_buff_param_id == null)
                {
                    continue;
                }

                var paramIds = buff.GetBean().t_buff_param_id;
                bool matched = false;
                for (int j = 0; j < paramIds.Count; j++)
                {
                    if (paramIds[j] == buffParamId)
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched)
                {
                    RemoveBuffImp(buff, i);
                    break;
                }
            }
        }

    

        public Buff GetExistClassId(long classId)
        {
            Buff buff = null;
            int count = m_Buffs.Count;
            for (int i = 0; i < count; ++i)
            {
                if (m_Buffs[i].GetBean().t_class_id == classId)
                {
                    buff = m_Buffs[i];
                    return buff;
                }
            }
            return null;

        }
        public void ClearBuffs()
        {
            for (int i = 0; i < m_Buffs.Count; ++i)
            {
                m_Buffs[i].OnRemove();
            }
            m_Buffs.Clear();
            if (m_Owner.ReadId() == BattleAPIBridge.__BattleRenderData_GetUIUnitId(true))
            {
                BRenderEvent.Event.OnClearBuff();
            }
        }
    }

    public static class BattleAPIBridge
    {
        public static RenderEff __RenderEffManager_CreateRenderEff(int cfgId)
        {
            return RenderEffManager.GetInstance().CreateRenderEff(cfgId);
        }

        public static RenderEff __RenderEffManager_CreateRenderEff(string abName, string sound = null)
        {
            return RenderEffManager.GetInstance().CreateRenderEff(abName, sound);
        }

        public static void __RenderEffManager_PoolRenderEff(RenderEff eff)
        {
            if (eff == null)
            {
                return;
            }

            RenderEffManager.GetInstance().PoolRenderEff(eff);
        }

        public static int __BattleRenderData_GetUIUnitId(bool valid)
        {
            return 0;
        }

        public static UResource __New_EntityObject(ResourceType type, emEntityType entityType)
        {
            return UResourceFactory.New_EntityObject(type, entityType);
        }
    }

    public static class BRenderEvent
    {
        public static BuffRenderEventBridge Event { get; } = new BuffRenderEventBridge();
    }

    public sealed class BuffRenderEventBridge
    {
        public void OnChangeBuff(int buffId, long templateId, int removeId)
        {
        }

        public void OnClearBuff()
        {
        }
    }

    public sealed class BattleToolCompat
    {
        public bool ReadIsEntityValide(Entity ent, int entId = 0)
        {
            return BattleManager.ReadIsEntityValide(ent, entId);
        }

        public void AddHP(PropertyEntity entity, float value, bool showNumber)
        {
            if (entity == null)
            {
                return;
            }

            var curHp = entity.ReadHP();
            var maxHp = entity.GetMaxHP();
            curHp += value;
            if (curHp > maxHp)
            {
                curHp = maxHp;
            }
            entity.SetHpRuntime(curHp);
            entity.OnHpChanged();
        }

        public void SubHP(PropertyEntity entity, float value, bool showNumber, PropertyEntity source = null)
        {
            if (entity == null)
            {
                return;
            }

            var curHp = entity.ReadHP();
            var maxHp = entity.GetMaxHP();
            curHp -= value;
            if (curHp > maxHp)
            {
                curHp = maxHp;
            }
            if (curHp < 0)
            {
                curHp = 0;
            }
            entity.SetHpRuntime(curHp);
            entity.OnHpChanged();
            if (showNumber && value > 0)
            {
                DamageCal.ShowBuffDamageNumber(source, entity, value);
            }
        }

        public void AddMagic(PropertyEntity entity, float value, bool showNumber)
        {
            if (entity == null)
            {
                return;
            }

            var curMagic = entity.ReadMagic() + value;
            var maxMagic = entity.GetMaxMagic();
            if (curMagic > maxMagic)
            {
                curMagic = maxMagic;
            }
            entity.SetMagicRuntime(curMagic);
        }

        public void SubMagic(PropertyEntity entity, float value, bool showNumber)
        {
            if (entity == null)
            {
                return;
            }

            var curMagic = entity.ReadMagic() - value;
            if (curMagic < 0)
            {
                curMagic = 0;
            }
            entity.SetMagicRuntime(curMagic);
        }

        public void DeadAttackerGold(int gold, Entity ent, Entity defender)
        {
            // 塔防当前分支未接入金币掉落链路，这里只保留兼容入口。
        }

        private enum TowerDefendBuffTargetMode
        {
            HitTarget = 0,
            Self = 1,
            AllHero = 2,
            AllMonster = 3
        }
        [Flags]
        private enum TowerDefendBuffRangeFlags
        {
            None = 0,
            ClearOnSkillCast = 1 << 8
        }

        private readonly List<PropertyEntity> m_SpreadCandidates = new List<PropertyEntity>();

        public void AddDefenderBuff(HurtInfo hurtInfo, PropertyEntity target)
        {
            if (hurtInfo == null || target == null)
            {
                return;
            }

            if (!BattleManager.ReadIsEntityValide(target))
            {
                return;
            }
        }

        public void AddSkillCastBuffs(PropertyEntity source, t_skillBean skillCfg)
        {
            if (source == null || skillCfg == null || skillCfg.t_skill_selfbuff_id == null || skillCfg.t_skill_selfbuff_id.Count <= 0)
            {
                return;
            }

            foreach (var buffId in skillCfg.t_skill_selfbuff_id)
            {
                if (buffId <= 0)
                {
                    continue;
                }

                var buffCfg = t_buff.GetConfig(buffId, false);
                if (buffCfg == null)
                {
                    continue;
                }

                if (!HasEffectiveBuffPayload(buffCfg))
                {
                    continue;
                }

                AddBuff(source, source, buffId);
            }
        }
        public bool CheckSkillPrecondition(PropertyEntity source, t_skillBean skillCfg)
        {
            if (source == null || skillCfg == null || skillCfg.t_skill_precon == null || skillCfg.t_skill_precon.Count <= 0)
            {
                return true;
            }

            var condition = ParseSkillPrecondition(skillCfg);
            if (!condition.HasValue)
            {
                return true;
            }

            var buffMgr = source.GetBuffManager();
            return buffMgr != null && buffMgr.ReadBuffStackCount(condition.Value.m_BuffId) >= condition.Value.m_NeedStackCount;
        }
        public void ConsumeSkillPreconditionIfNeeded(PropertyEntity source, t_skillBean skillCfg)
        {
            if (source == null || skillCfg == null || skillCfg.t_skill_precon == null || skillCfg.t_skill_precon.Count <= 0)
            {
                return;
            }

            var condition = ParseSkillPrecondition(skillCfg);
            if (!condition.HasValue)
            {
                return;
            }

            if (condition.Value.m_ClearFlag == 0)
            {
                var buffMgr = source.GetBuffManager();
                if (buffMgr != null)
                {
                    buffMgr.ClearBuffStack(condition.Value.m_BuffId);
                }
            }
        }

        private struct SkillPreconditionData
        {
            public long m_BuffId;
            public int m_NeedStackCount;
            public int m_ClearFlag;
        }

        private SkillPreconditionData? ParseSkillPrecondition(t_skillBean skillCfg)
        {
            if (skillCfg == null || skillCfg.t_skill_precon == null || skillCfg.t_skill_precon.Count <= 0)
            {
                return null;
            }

            if (skillCfg.t_skill_precon.Count == 1 && skillCfg.t_skill_precon[0] == 0)
            {
                return null;
            }

            if (skillCfg.t_skill_precon.Count < 3)
            {
                return null;
            }

            return new SkillPreconditionData
            {
                m_BuffId = skillCfg.t_skill_precon[0],
                m_NeedStackCount = Math.Max(1, skillCfg.t_skill_precon[1]),
                m_ClearFlag = skillCfg.t_skill_precon[2],
            };
        }

        public void RemoveBuffsOnSkillCast(PropertyEntity source)
        {
            if (source == null)
            {
                return;
            }

            var buffMgr = source.GetBuffManager();
            if (buffMgr == null)
            {
                return;
            }

            var buffs = buffMgr.ReadBuffs();
            if (buffs == null || buffs.Count <= 0)
            {
                return;
            }

            // 旧的按技能施放移除 buff 规则已废弃，是否清层由 skill_precon 的 clearFlag 控制。
        }

        public void AddBuffs(PropertyEntity source, PropertyEntity target, IReadOnlyCollection<long> buffCfgIds)
        {
            if (target == null || buffCfgIds == null || buffCfgIds.Count <= 0)
            {
                return;
            }

            foreach (var buffId in buffCfgIds)
            {
                if (buffId <= 0)
                {
                    continue;
                }

                AddBuff(source, target, buffId);
            }
        }

        public void AddBuff(PropertyEntity source, PropertyEntity target, long buffCfgId)
        {
            if (target == null || buffCfgId <= 0)
            {
                return;
            }

            var buffMgr = target.GetBuffManager();
            if (buffMgr == null)
            {
                return;
            }

            var buffCfg = t_buff.GetConfig(buffCfgId, false);
            if (buffCfg == null)
            {
                return;
            }

            if (!HasEffectiveBuffPayload(buffCfg))
            {
                return;
            }

            var old = buffMgr.GetExist(buffCfgId);
            if (old != null)
            {
                old.AddBuffAgain();
                return;
            }

            buffMgr.TryAddBuff(buffCfgId, source, target);
        }

        public void TryTriggerBuffsOnHit(PropertyEntity source, HurtInfo hurtInfo)
        {
            if (source == null || hurtInfo == null)
            {
                return;
            }

            if (!ShouldTriggerBuffsOnHit(hurtInfo))
            {
                return;
            }

            var sourceBuffMgr = source.GetBuffManager();
            if (sourceBuffMgr == null)
            {
                return;
            }

            var activeBuffs = sourceBuffMgr.ReadBuffs();
            if (activeBuffs == null || activeBuffs.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < activeBuffs.Count; i++)
            {
                var activeBuff = activeBuffs[i];
                if (activeBuff == null)
                {
                    continue;
                }

                var bean = activeBuff.GetBean();
                if (bean == null || bean.t_buff_trigger_buff == 0 || bean.t_buff_trigger_layers <= 0)
                {
                    continue;
                }

                var addOdd = bean.t_buff_add_odd > 0 ? bean.t_buff_add_odd : 1000;
                if (UnityEngine.Random.Range(0, 1000) >= addOdd)
                {
                    continue;
                }

                var triggeredBuffCfgIds = ResolveTriggeredBuffCfgIds(bean.t_buff_trigger_buff);
                if (triggeredBuffCfgIds == null || triggeredBuffCfgIds.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < triggeredBuffCfgIds.Count; j++)
                {
                    AddBuffLayers(source, source, triggeredBuffCfgIds[j], bean.t_buff_trigger_layers);
                }
            }
        }

        private bool ShouldTriggerBuffsOnHit(HurtInfo hurtInfo)
        {
            if (hurtInfo == null || hurtInfo.m_Slot != 0 || hurtInfo.skillCfg == null)
            {
                return false;
            }

            // 新表这条触发链当前只用于普攻命中叠层，避免技能命中也把条件层数刷上去。
            return hurtInfo.skillCfg.t_class_Id == 1002;
        }

        private bool HasEffectiveBuffPayload(t_buff buffCfg)
        {
            if (buffCfg == null || buffCfg.t_buff_param_id == null || buffCfg.t_buff_param_id.Count <= 0)
            {
                return false;
            }

            for (int i = 0; i < buffCfg.t_buff_param_id.Count; i++)
            {
                var paramCfg = t_buffParam.GetConfig(buffCfg.t_buff_param_id[i], false);
                if (paramCfg == null)
                {
                    continue;
                }

                if (paramCfg.t_atk != 0 ||
                    paramCfg.t_atk_percen != 0 ||
                    paramCfg.t_hp != 0 ||
                    paramCfg.t_hp_percen != 0 ||
                    paramCfg.t_crit != 0 ||
                    paramCfg.t_crit_damage != 0 ||
                    paramCfg.t_move_speed != 0 ||
                    paramCfg.t_move_speed_percen != 0 ||
                    paramCfg.t_atk_speed != 0 ||
                    paramCfg.t_atk_speed_percen != 0 ||
                    paramCfg.t_amp != 0 ||
                    paramCfg.t_duration != 0 ||
                    paramCfg.t_attack_range != 0 ||
                    paramCfg.t_buff_maxnum > 1)
                {
                    return true;
                }
            }

            return buffCfg.t_buff_trigger_buff != 0 || buffCfg.t_buff_trigger_layers > 0;
        }

        private List<long> ResolveTriggeredBuffCfgIds(int buffParamId)
        {
            var result = new List<long>();
            var keys = t_buff.GetKeys();
            if (keys == null || keys.Count <= 0)
            {
                return result;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                var buffCfg = t_buff.GetConfig(keys[i], false);
                if (buffCfg == null || buffCfg.t_buff_param_id == null)
                {
                    continue;
                }

                for (int j = 0; j < buffCfg.t_buff_param_id.Count; j++)
                {
                    if (buffCfg.t_buff_param_id[j] == buffParamId)
                    {
                        result.Add(buffCfg.t_id);
                        break;
                    }
                }
            }

            return result;
        }

        private void AddBuffLayers(PropertyEntity source, PropertyEntity target, long buffCfgId, int layers)
        {
            if (target == null || buffCfgId <= 0 || layers <= 0)
            {
                return;
            }

            var buffMgr = target.GetBuffManager();
            if (buffMgr == null)
            {
                return;
            }

            var old = buffMgr.GetExist(buffCfgId);
            if (old == null)
            {
                old = buffMgr.TryAddBuff(buffCfgId, source, target);
                layers--;
            }

            for (int i = 0; i < layers && old != null; i++)
            {
                old.AddBuffAgain();
            }
        }

        private void SpreadBuffToNearbyTargets(HurtInfo hurtInfo, PropertyEntity centerTarget, long buffCfgId, int extraCount)
        {
            if (hurtInfo == null || centerTarget == null || extraCount <= 0 || hurtInfo.m_Attacker == null)
            {
                return;
            }

            var searchDist = ResolveSpreadDistanceMeters(hurtInfo.skillCfg);
            if (!GetNearbyEnemies(centerTarget.GetPosition(), searchDist, hurtInfo.m_Attacker.ReadGroup(), m_SpreadCandidates, centerTarget))
            {
                return;
            }

            var centerPos = centerTarget.GetPosition();
            m_SpreadCandidates.Sort((a, b) =>
            {
                var da = Vector3.SqrMagnitude(a.GetPosition() - centerPos);
                var db = Vector3.SqrMagnitude(b.GetPosition() - centerPos);
                return da.CompareTo(db);
            });

            int applyCount = Math.Min(extraCount, m_SpreadCandidates.Count);
            for (int i = 0; i < applyCount; i++)
            {
                AddBuff(hurtInfo.m_Attacker, m_SpreadCandidates[i], buffCfgId);
            }
        }

        private float ResolveSpreadDistanceMeters(t_skillBean skillCfg)
        {
            if (skillCfg == null)
            {
                return 3.0f;
            }

            if (skillCfg.t_hurt_param0 > 0)
            {
                return skillCfg.t_hurt_param0 / 1000.0f;
            }

            return 3.0f;
        }

        private int ReadSpreadTargetCount(t_buff buffCfg)
        {
            if (buffCfg == null || buffCfg.t_buff_param_id == null || buffCfg.t_buff_param_id.Count <= 0)
            {
                return 0;
            }

            int count = buffCfg.t_buff_param_id.Count;
            for (int i = 0; i < count; i++)
            {
                var paramCfg = t_buffParam.GetConfig(buffCfg.t_buff_param_id[i], false);
                if (paramCfg == null)
                {
                    continue;
                }

                if (paramCfg.t_attack_range > 0)
                {
                    return Math.Max(0, paramCfg.t_attack_range);
                }
            }

            return 0;
        }

        private void AddBuffToAllGuardHeroes(PropertyEntity source, long buffCfgId)
        {
            var battle = BattleManager.GetBattle();
            var battleSpawer = battle != null ? battle.ReadBattleSpawer() : null;
            var guardHeroes = battleSpawer != null ? battleSpawer.ReadGuardHeroes() : null;
            if (guardHeroes == null || guardHeroes.Count <= 0)
            {
                return;
            }

            int count = guardHeroes.Count;
            for (int i = 0; i < count; i++)
            {
                var hero = guardHeroes[i];
                if (!BattleManager.ReadIsEntityValide(hero))
                {
                    continue;
                }

                AddBuff(source, hero, buffCfgId);
            }
        }

        private void AddBuffToAllEnemyMonsters(PropertyEntity source, long buffCfgId)
        {
            var objMgr = BattleManager.GetObjectManager();
            if (objMgr == null || source == null)
            {
                return;
            }

            var entities = objMgr.ReadPropertyEntities();
            int count = entities.Count;
            for (int i = 0; i < count; i++)
            {
                var entity = entities[i];
                if (!BattleManager.ReadIsEntityValide(entity) || entity.ReadGroup() == source.ReadGroup())
                {
                    continue;
                }

                AddBuff(source, entity, buffCfgId);
            }
        }

        private TowerDefendBuffTargetMode ReadBuffTargetMode(int mode)
        {
            switch (mode)
            {
                case (int)TowerDefendBuffTargetMode.Self:
                    return TowerDefendBuffTargetMode.Self;
                case (int)TowerDefendBuffTargetMode.AllHero:
                    return TowerDefendBuffTargetMode.AllHero;
                case (int)TowerDefendBuffTargetMode.AllMonster:
                    return TowerDefendBuffTargetMode.AllMonster;
                default:
                    return TowerDefendBuffTargetMode.HitTarget;
            }
        }

        public bool GetNearbyEnemies(Vector3 pos, float distMeters, GroupId attackerGroup, List<PropertyEntity> list, PropertyEntity exDefender = null)
        {
            if (list == null)
            {
                return false;
            }

            list.Clear();
            var objMgr = BattleManager.GetObjectManager();
            if (objMgr == null)
            {
                return false;
            }

            var entities = objMgr.ReadPropertyEntities();
            var maxDist = Mathf.Max(0, distMeters);
            var center = pos;
            center.y = 0;
            foreach (var entity in entities)
            {
                if (entity == null || entity == exDefender)
                {
                    continue;
                }
                if (!BattleManager.ReadIsEntityValide(entity))
                {
                    continue;
                }
                if (entity.ReadGroup() == attackerGroup)
                {
                    continue;
                }

                var targetPos = entity.GetPosition();
                targetPos.y = 0;
                if (Vector3.Distance(center, targetPos) <= maxDist)
                {
                    list.Add(entity);
                }
            }

            return list.Count > 0;
        }
    }

}
