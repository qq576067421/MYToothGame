using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameDll
{
    public class BuffClassId
    {
        public const long ChangePropertyBuff = 1003;
        public const long InstantDamageBuff = 1004;
        public const long RecoveryBuff = 1007;
        public const long DamageBuff = 1008;
        public const long ActionBuff = 1015;
    }
    public class BuffTemplate
    {
        public static Buff createBuff(long buffId)
        {
            var bBean = t_buff.GetConfig(buffId);
            if (bBean == null)
                return null;

            var classId = bBean.t_class_id;
            Buff buff = null;

            switch(classId)
            {
                case BuffClassId.ChangePropertyBuff:
                    {
                        buff = new ChangePropertyBuff();
                        break;
                    }
                case BuffClassId.RecoveryBuff:
                    {
                        buff = new RecoveryBuff();
                        break;
                    }
                case BuffClassId.InstantDamageBuff:
                case BuffClassId.DamageBuff:
                    {
                        buff = new DamageBuff();
                        break;
                    }
                case BuffClassId.ActionBuff:
                    {
                        buff = new ActionBuff();
                        break;
                    }
            }

            if (buff == null)
            {
                Debug.LogWarning("GameDll 实例化Buff类失败， buff id：" + buffId);
                return null;
            }
            buff.InitTemplate(buffId, classId);
            return buff;
        }
    }
}

