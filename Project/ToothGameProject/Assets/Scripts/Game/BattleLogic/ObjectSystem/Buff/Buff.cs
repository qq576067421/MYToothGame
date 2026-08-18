using MonoBean;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameDll
{
    //buff作用目标：0单个 1周围 2全体
        public enum HurtType
    {
        None = -1,
        PhyAtk = 0,
        MagAtk = 1,
        PureAtk = 2,
        TrapAtk = 3,
    }

    public readonly struct VFactor
    {
        public static readonly VFactor one = new VFactor(1.0f);
        public readonly float Value;

        public VFactor(float value)
        {
            Value = value;
        }
    }
        public class BuffModify
    {
        public float t_buff_during = float.MaxValue;
        public float t_buff_gap = float.MaxValue;
    }
    public class BuffParamModify
    {
        public float t_buff_param_value = float.MaxValue;
        public float t_buff_param_percent = float.MaxValue;
    }
    public abstract class Buff
    {
        protected  PropertyData m_PropertyData = new PropertyData();
        protected PropertyData ReadPropertyData()
        {
            return m_PropertyData;
        }
        //这个一般用于计算概率格挡、概率倍率攻击(UI禁用)
        public virtual float CalPropertyMaxScale(int type, int slot, HurtType hurt_type)
        {
            return 0;
        }
        public float ReadProperty(int type)
        {
            return m_PropertyData.ReadProperty(type);
        }
        public float ReadPropertyV(int type)
        {
            return m_PropertyData.ReadPropertyV(type);
        }
        /// <summary>
        /// buff配置数据
        /// </summary>
        protected t_buff m_Bean;
        protected t_buffDesc m_DescBean;

        /// <summary>
        /// buff特效
        /// </summary>
        protected RenderEff m_Effect;
        protected bool m_EffectAttachedToRender;
        /// <summary>
        /// 模版Id
        /// </summary>
        protected long m_TemplateId;

        /// <summary>
        /// 实例Id
        /// </summary>
        protected int m_Id;

        /// <summary>
        /// buff添加时间
        /// </summary>
        protected int m_AddTime;

        protected float m_BuffDuringTime = 0;
        public void SetBuffDuringTime(float time)
        {
            m_BuffDuringTime = time;
            m_EndTime = m_BuffDuringTime + BattleManager.ReadBattleTime();
        }

        protected float m_BuffIntervalTime = 0;
        public void SetBuffIntervalTime(float interval)
        {
            m_BuffIntervalTime = interval;
        }

        /// <summary>
        /// 上一次生效时间
        /// </summary>
        protected int m_LastActionTime;
        protected int m_StackCount = 1;

        // 特殊技能用倍率
        public int m_BuffRate = 0;

        protected long m_BuffClassId;

        public virtual void  InitTemplate(long templateId, long buffClassId)
        {
            m_TemplateId = templateId;
            m_Bean = t_buff.GetConfig(templateId);
            m_DescBean = t_buffDesc.GetConfig(m_Bean.t_descId);
            m_BuffClassId = buffClassId;
        }
        public virtual void InitInstance(int instanceId)
        {
            m_Id = instanceId;
        }
        /// <summary>
        /// 返回模版数据
        /// </summary>
        /// <returns></returns>
        public t_buff GetBean()
        {
            return m_Bean;
        }
        public t_buffDesc GetDescBean()
        {
            return m_DescBean;
        }
        public virtual void UpdateRender()
        {

        }
        private float m_EndTime = 0;
        private float m_PassedTime = 0;
        public virtual void Update(float dt)
        {

            if (m_TargetEnt != null && !m_TargetEnt.GetStateManager().ReadIsState(emEntityState.em_EntityState_Dead))
            {
                if (m_BuffDuringTime > 0 && BattleManager.ReadBattleTime() > m_EndTime)
                {
                    m_TargetEnt.GetBuffManager().RemoveBuff(m_Id);
                }
                else
                {
                    if (m_BuffIntervalTime > 0 && m_PassedTime > m_BuffIntervalTime)
                    {
                        m_PassedTime = 0;
                        DoAction(null, m_TargetEnt);
                    }
                }
                m_PassedTime += dt;

                UpdateEffectPosition();


            }
        }

        protected virtual void UpdateEffectPosition()
        {
            if (m_Effect == null || m_EffectAttachedToRender || m_TargetEnt == null)
            {
                return;
            }

            m_Effect.SetPosition(ReadBuffEffectPosition());
        }

        /// <summary>
        /// buff移除前调用
        /// </summary>
        /// <param name="owner"></param>
        public virtual void OnRemove()
        {
            OnRemoveImp();

        }

        protected virtual void OnRemoveImp()
        {
            if (m_Effect != null)
            {
                BattleAPIBridge.__RenderEffManager_PoolRenderEff(m_Effect);
                m_Effect = null;
            }
            m_EffectAttachedToRender = false;
            m_EndTime = 0;
            m_PassedTime = 0;
            m_PropertyData.Reset();

            if(m_IsChangeProperty)
            {
                m_TargetEnt.CheckPropertyChanged();
            }
        }

        protected PropertyEntity m_TargetEnt;
        protected PropertyEntity m_SourceEnt;
        protected BuffModify m_BuffModify;
        protected Dictionary<long, BuffParamModify> m_BuffParamModifies;
        /// <summary>
        /// 添加buff时调用
        /// </summary>
        /// <param name="mAttacker"></param>
        /// <param name="defender"></param>
        public virtual int AddBuff(PropertyEntity source, PropertyEntity target,
            BuffModify modify, Dictionary<long, BuffParamModify> paramModifies)
        {
            m_SourceEnt = source;
            m_TargetEnt = target;
            m_BuffModify = modify;
            m_BuffParamModifies = paramModifies;

            m_AddTime = (int)BattleManager.ReadBattleTime();

            m_BuffDuringTime = m_Bean.t_buff_during / 1000.0f;
            m_BuffIntervalTime = m_Bean.t_buff_gap / 1000.0f;
            __ModifyBuff();

            m_EndTime = m_BuffDuringTime + BattleManager.ReadBattleTime();
            LoadEffect();


            AddSpecialAction();

            ChangeProperties();

            return 0;
        }

        private void __ModifyBuff()
        {
            if(m_BuffModify == null)
            {
                return;
            }
            if (m_BuffModify.t_buff_during != float.MaxValue)
            {
                m_BuffDuringTime = m_BuffModify.t_buff_during / 1000.0f;
            }

            if (m_BuffModify.t_buff_gap != float.MaxValue)
            {
                m_BuffIntervalTime = m_BuffModify.t_buff_gap / 1000.0f;
            }
        }

        protected virtual void ChangeProperties()
        {
            int count = m_Bean.t_buff_param_id.Count;
            for(int i = 0; i < count; ++i)
            {
                __ChangeProperty(m_Bean.t_buff_param_id[i]);
            }
            if(m_IsChangeProperty)
            {
                m_TargetEnt.CheckPropertyChanged();
            }
        }
        protected long m_Level = 0;

        public void SetLevel(long level)
        {
            m_Level = level;
        }
        private void __ChangeProperty(long buff_param_cfg_id)
        {
            var cfg = t_buffParam.GetConfig(buff_param_cfg_id);
            if (cfg == null)
            {
                return;
            }

            var data = ReadPropertyData();
            AddLinearProperty(data, (int)PropertyType.atk, cfg.t_atk, cfg.t_atk_percen / 1000f);
            AddLinearProperty(data, (int)PropertyType.hp, cfg.t_hp, cfg.t_hp_percen / 1000f);
            AddLinearProperty(data, (int)PropertyType.crit, cfg.t_crit / 1000f, 0);
            AddLinearProperty(data, (int)PropertyType.crit_damage, cfg.t_crit_damage / 1000f, 0);
            AddLinearProperty(data, (int)PropertyType.move_speed, cfg.t_move_speed / 1000f, cfg.t_move_speed_percen / 1000f);
            AddLinearProperty(data, (int)PropertyType.attack_speed, cfg.t_atk_speed / 1000f, cfg.t_atk_speed_percen / 1000f);
            AddLinearProperty(data, (int)PropertyType.amp, cfg.t_amp / 1000f, 0);
            AddLinearProperty(data, (int)PropertyType.duration, cfg.t_duration / 1000.0f, 0);
            AddLinearProperty(data, (int)PropertyType.attack_range, cfg.t_attack_range / 1000f, 0);
        }
        protected bool m_IsChangeProperty = false;


        protected virtual void AddSpecialAction()
        {
            if(m_DescBean.t_special_action == 0)
            {
                return;
            }
            if(m_TargetEnt == null)
            {
                return;
            }

            if(m_DescBean.t_special_action == 1)
            {
                m_TargetEnt.TryEnterDizzyState(m_BuffDuringTime);
            }

        }

        public PropertyEntity GetTarget()
        {
            return m_TargetEnt;
        }
        public virtual void AddBuffAgain()
        {
            var maxStack = 1;
            if (m_Bean != null && m_Bean.t_buff_param_id != null && m_Bean.t_buff_param_id.Count > 0)
            {
                var paramCfg = t_buffParam.GetConfig(m_Bean.t_buff_param_id[0], false);
                if (paramCfg != null && paramCfg.t_buff_maxnum > 0)
                {
                    maxStack = paramCfg.t_buff_maxnum;
                }
            }

            int oldStackCount = m_StackCount;
            m_StackCount = Math.Min(Math.Max(1, maxStack), m_StackCount + 1);
            m_EndTime = m_BuffDuringTime + BattleManager.ReadBattleTime();

            if (m_StackCount > oldStackCount && m_TargetEnt != null)
            {
                int stackDelta = m_StackCount - oldStackCount;
                var data = ReadPropertyData();
                int count = m_Bean.t_buff_param_id.Count;
                for (int i = 0; i < count; ++i)
                {
                    var cfg = t_buffParam.GetConfig(m_Bean.t_buff_param_id[i]);
                    if (cfg == null)
                    {
                        continue;
                    }

                    AddLinearProperty(data, (int)PropertyType.atk, cfg.t_atk * stackDelta, cfg.t_atk_percen / 1000f * stackDelta);
                    AddLinearProperty(data, (int)PropertyType.hp, cfg.t_hp * stackDelta, cfg.t_hp_percen / 1000f * stackDelta);
                    AddLinearProperty(data, (int)PropertyType.crit, cfg.t_crit / 1000f * stackDelta, 0);
                    AddLinearProperty(data, (int)PropertyType.crit_damage, cfg.t_crit_damage / 1000f * stackDelta, 0);
                    AddLinearProperty(data, (int)PropertyType.move_speed, cfg.t_move_speed / 1000f * stackDelta, cfg.t_move_speed_percen / 1000f * stackDelta);
                    AddLinearProperty(data, (int)PropertyType.attack_speed, cfg.t_atk_speed / 1000f * stackDelta, cfg.t_atk_speed_percen / 1000f * stackDelta);
                    AddLinearProperty(data, (int)PropertyType.amp, cfg.t_amp / 1000f * stackDelta, 0);
                    AddLinearProperty(data, (int)PropertyType.attack_range, cfg.t_attack_range / 1000f * stackDelta, 0);
                }

                m_TargetEnt.CheckPropertyChanged();
            }
        }
        public int ReadStackCount()
        {
            return Math.Max(1, m_StackCount);
        }
        public void SetStackCount(int stackCount)
        {
            m_StackCount = Math.Max(0, stackCount);
        }

        protected bool HasBuffEffectPath()
        {
            return IsConfigValueSet(ReadBuffEffectPath());
        }

        protected string ReadBuffEffectPath()
        {
            return m_DescBean != null ? m_DescBean.t_buff_addEffect : null;
        }

        protected string ReadBuffEffectHangPath()
        {
            return m_DescBean != null ? m_DescBean.t_specialbuff_pos : null;
        }

        protected string ReadBuffEffectSound()
        {
            return m_DescBean != null ? m_DescBean.t_specialbuff_sound : null;
        }

        protected static bool IsConfigValueSet(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value != "0";
        }

        protected Vector3 ReadBuffEffectPosition()
        {
            return m_TargetEnt != null ? m_TargetEnt.ReadMountPosition(ReadBuffEffectHangPath()) : Vector3.zero;
        }

        protected bool TryAttachEffectToTargetMount(RenderEff effect)
        {
            if (effect == null || m_TargetEnt == null)
            {
                return false;
            }

            var mountPath = ReadBuffEffectHangPath();
            var mountTransform = m_TargetEnt.ReadMountTransform(mountPath);
            if (mountTransform != null)
            {
                effect.SetParent(mountTransform);
                m_EffectAttachedToRender = true;
                return true;
            }

            var targetEnt = m_TargetEnt;
            targetEnt.AddLoadedCall(() =>
            {
                if (m_Effect != effect || effect.m_Destroy)
                {
                    return;
                }

                var deferredMount = targetEnt.ReadMountTransform(mountPath);
                if (deferredMount == null)
                {
                    return;
                }

                effect.SetParent(deferredMount);
                effect.ShowEff(true, Vector3.zero, Vector3.zero, Vector3.one);
                m_EffectAttachedToRender = true;
            });
            return false;
        }
        //加载特效
        protected virtual void LoadEffect()
        {
            m_EffectAttachedToRender = false;
            if (m_Effect!= null)
            {
                m_Effect.SetActive(true);
            }
            else if(HasBuffEffectPath())
            {
                m_Effect = BattleAPIBridge.__RenderEffManager_CreateRenderEff(ReadBuffEffectPath(), ReadBuffEffectSound());

                if (TryAttachEffectToTargetMount(m_Effect))
                {
                    m_Effect.ShowEff(true, Vector3.zero, Vector3.zero, Vector3.one);
                }
                else
                {
                    m_Effect.ShowEff(false, ReadBuffEffectPosition(), Vector3.zero, Vector3.one);
                }
            }

        }


        /// <summary>
        /// buff生效时调用
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public virtual int DoAction(PropertyEntity source, PropertyEntity target)
        {
            return 0;
        }

        /// <summary>
        /// 模版id
        /// </summary>
        /// <returns></returns>
        public long GetTemplateId()
        {
            return m_TemplateId;
        }

        public int GetId()
        {
            return m_Id;
        }

        public long GetBuffClassId()
        {
            return m_BuffClassId;
        }

        public virtual VFactor GetMoveSpeedFactor()
        {
            return VFactor.one;
        }

        public virtual void OnBeAttack(PropertyEntity attacker, float hurt)
        {

        }

        private void AddLinearProperty(PropertyData data, int propertyType, float flatValue, float percentValue)
        {
            if (data == null)
            {
                return;
            }

            if (flatValue != 0)
            {
                data.AddProperty(propertyType, flatValue);
                m_IsChangeProperty = true;
            }

            if (percentValue != 0)
            {
                data.AddProperty(propertyType, ResolvePercentPropertyBonus(propertyType, percentValue));
                m_IsChangeProperty = true;
            }
        }

        private float ResolvePercentPropertyBonus(int propertyType, float percentValue)
        {
            if (m_TargetEnt == null)
            {
                return 0;
            }

            float baseValue = 0;
            switch (propertyType)
            {
                case (int)PropertyType.atk:
                    baseValue = m_TargetEnt.GetAtk();
                    break;
                case (int)PropertyType.hp:
                    baseValue = m_TargetEnt.GetMaxHP();
                    break;
                case (int)PropertyType.move_speed:
                    baseValue = m_TargetEnt.GetConfigMoveSpeed();
                    break;
                case (int)PropertyType.attack_speed:
                    baseValue = m_TargetEnt.GetNormalAtkSpeed();
                    break;
            }

            return baseValue * percentValue;
        }



    }

}




