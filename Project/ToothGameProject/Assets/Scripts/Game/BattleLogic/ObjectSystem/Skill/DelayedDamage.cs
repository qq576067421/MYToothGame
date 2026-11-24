using System.Collections.Generic;

namespace GameDll
{
    // 命中延迟伤害：子弹命中当帧只播表现，伤害本身在延迟时间到期后才结算。
    // 计时挂在战斗逻辑帧上（BattleManager.LogicUpdate 驱动），战斗暂停时计时同步冻结。
    // 子弹命中后会立即回池复用；延迟队列只保留伤害快照和目标身份。
    public static class DelayedDamageManager
    {
        private sealed class PendingDamage
        {
            public HurtInfo m_HurtInfo;
            public PropertyEntity m_Defender;
            public int m_DefenderId;
            public GroupId m_DefenderGroup;
            public float m_DueTime;
        }

        private static readonly List<PendingDamage> m_Pendings = new List<PendingDamage>();
        private static readonly List<PendingDamage> m_DueDamages = new List<PendingDamage>();
        private static readonly Stack<PendingDamage> m_Cache = new Stack<PendingDamage>();

        public static void Clear()
        {
            m_Pendings.Clear();
            m_DueDamages.Clear();
            m_Cache.Clear();
        }

        // delayMilliSecond <= 0 表示命中立即结算，调用点不必再写一遍分支
        public static void Schedule(HurtInfo hurtInfo, PropertyEntity defender, int delayMilliSecond)
        {
            if (hurtInfo == null || defender == null)
            {
                return;
            }

            if (delayMilliSecond <= 0)
            {
                ApplyDamage(hurtInfo, defender);
                return;
            }

            var pending = m_Cache.Count > 0 ? m_Cache.Pop() : new PendingDamage();
            if (pending.m_HurtInfo == null)
            {
                pending.m_HurtInfo = new HurtInfo();
            }
            pending.m_HurtInfo.Reset();
            pending.m_HurtInfo.CopyFrom(hurtInfo);
            if (pending.m_HurtInfo.m_AttackerId == 0 && pending.m_HurtInfo.m_Attacker != null)
            {
                pending.m_HurtInfo.m_AttackerId = pending.m_HurtInfo.m_Attacker.ReadId();
            }

            // 延迟期间目标会移动，伤害数字改到结算那一帧按目标当前命中点弹出
            pending.m_HurtInfo.ClearDamageNumberWorldPos();
            pending.m_Defender = defender;
            pending.m_DefenderId = defender.ReadId();
            pending.m_DefenderGroup = defender.ReadGroup();
            pending.m_DueTime = BattleManager.ReadBattleTime() + delayMilliSecond * 0.001f;
            m_Pendings.Add(pending);

            // 吸血量由本次伤害派生，跟着伤害一起延后结算；调用点交出去的脏值在这里清掉
            hurtInfo.m_SickHP = 0;
        }

        public static void Update()
        {
            if (m_Pendings.Count == 0)
            {
                return;
            }

            float now = BattleManager.ReadBattleTime();
            int waitingCount = 0;
            for (int i = 0; i < m_Pendings.Count; i++)
            {
                var pending = m_Pendings[i];
                if (now >= pending.m_DueTime)
                {
                    m_DueDamages.Add(pending);
                }
                else
                {
                    m_Pendings[waitingCount++] = pending;
                }
            }

            m_Pendings.RemoveRange(waitingCount, m_Pendings.Count - waitingCount);
            for (int i = 0; i < m_DueDamages.Count; i++)
            {
                var pending = m_DueDamages[i];
                try
                {
                    var defender = ResolveDefender(pending);
                    if (defender != null)
                    {
                        ResolveAttacker(pending.m_HurtInfo);
                        ApplyDamage(pending.m_HurtInfo, defender);
                    }
                }
                catch (System.Exception exception)
                {
                    UnityEngine.Debug.LogError("[延迟伤害] 结算失败。attacker=" + pending.m_HurtInfo.m_AttackerId +
                                               " defender=" + pending.m_DefenderId);
                    UnityEngine.Debug.LogException(exception);
                }
                finally
                {
                    pending.m_Defender = null;
                    pending.m_HurtInfo.Reset();
                    m_Cache.Push(pending);
                }
            }
            m_DueDamages.Clear();
        }

        private static void ApplyDamage(HurtInfo hurtInfo, PropertyEntity defender)
        {
            DamageCal.Cal(hurtInfo, defender);
            DamageCal.CalSickHP(hurtInfo.m_Attacker, hurtInfo.m_SickHP);
            hurtInfo.m_SickHP = 0;
        }

        // 对象池会重复利用实例，结算时同时检查实体 Id、实例和阵营。
        private static PropertyEntity ResolveDefender(PendingDamage pending)
        {
            var objMgr = BattleManager.GetObjectManager();
            if (objMgr == null)
            {
                return null;
            }
            var defender = objMgr.ReadPropertyEntityById(pending.m_DefenderId);
            if (!BattleManager.ReadIsEntityValide(defender, pending.m_DefenderId) ||
                !ReferenceEquals(defender, pending.m_Defender) || defender.ReadGroup() != pending.m_DefenderGroup)
            {
                return null;
            }
            return defender;
        }

        private static void ResolveAttacker(HurtInfo hurtInfo)
        {
            if (hurtInfo.m_AttackerId != 0 &&
                BattleManager.ReadIsEntityValide(hurtInfo.m_Attacker, hurtInfo.m_AttackerId))
            {
                return;
            }

            var objMgr = BattleManager.GetObjectManager();
            var attacker = hurtInfo.m_AttackerId != 0
                ? objMgr?.ReadPropertyEntityById(hurtInfo.m_AttackerId)
                : null;
            if (hurtInfo.m_Attacker != null && !ReferenceEquals(attacker, hurtInfo.m_Attacker))
            {
                attacker = null;
            }

            hurtInfo.m_Attacker = attacker;
        }
    }
}
