using System;
using MonoBean;
using UnityEngine;

namespace GameDll
{
    public static class DamageCal
    {
        public static bool m_GM_Invincible = false;

        public static void Cal(HurtInfo hurtInfo, PropertyEntity defender)
        {
            if (hurtInfo == null || defender == null)
            {
                return;
            }

            if (!BattleManager.ReadIsEntityValide(defender) || !defender.ReadCanBeHurt())
            {
                hurtInfo.ClearDamageNumberWorldPos();
                return;
            }

            if (m_GM_Invincible && defender.ReadGroup() == GroupId.PushGroupId)
            {
                hurtInfo.ClearDamageNumberWorldPos();
                return;
            }

            var attacker = hurtInfo.m_Attacker;
            if (attacker != null)
            {
                defender.SetAttackMe(attacker);
                defender.SetKillMeAttackId(attacker.ReadId(), attacker.ReadGroup());
            }
            else if (hurtInfo.m_AttackerId != 0)
            {
                defender.SetKillMeAttackId(hurtInfo.m_AttackerId, hurtInfo.m_Group);
            }

            var worldPos = ResolveDamageNumberWorldPos(hurtInfo, defender);
            var hp = defender.ReadHP();
            hurtInfo.m_IsCrit = IsDamageCrit(hurtInfo, defender, worldPos, hp);

            var hurt = Mathf.Max(0, hurtInfo.m_Hurt);
            if (hurt > 0 && hurtInfo.m_IsCrit)
            {
                hurt *= Mathf.Max(0, hurtInfo.m_CritDamageScale);
            }
            var nextHp = Math.Max(0, hp - hurt);
            defender.SetHpRuntime(nextHp);
            defender.OnHpChanged();
            if (hurt > 0)
            {
                ShowDamageNumber(attacker, defender, hurt, worldPos, hurtInfo.m_IsCrit);
            }
            hurtInfo.ClearDamageNumberWorldPos();
            TryHandleMonsterDeathStarsEffect(defender);

            if (attacker != null && hurt > 0)
            {
                BattleManager.GetBattleTool().TryTriggerBuffsOnHit(attacker, hurtInfo);
            }
        }

        public static void ShowBuffDamageNumber(PropertyEntity attacker, PropertyEntity defender, float hurt)
        {
            if (defender == null || hurt <= 0)
            {
                return;
            }

            ShowDamageNumber(attacker, defender, hurt, defender.ReadHitPoint(), false);
        }

        public static void CalSickHP(PropertyEntity attacker, float sickHp)
        {
            if (attacker == null || sickHp <= 0)
            {
                return;
            }

            if (!BattleManager.ReadIsEntityValide(attacker))
            {
                return;
            }

            var maxHp = attacker.GetMaxHP();
            var hp = attacker.ReadHP();
            var nextHp = Mathf.Min(maxHp, hp + sickHp);
            attacker.SetHpRuntime(nextHp);
            attacker.OnHpChanged();
        }

        private static void ShowDamageNumber(PropertyEntity attacker, PropertyEntity defender, float hurt, Vector3 worldPos, bool isCrit)
        {
            var actorRender = defender.GetRender() as UActor;
            if (actorRender == null)
            {
                return;
            }

            var hpTextType = isCrit ? HpTextType.Crit : HpTextType.SkillHurt;
            actorRender.ShowNumber(
                hpTextType,
                ((int)hurt).ToString(),
                1.5f,
                ApplyDamageNumberRandomOffset(defender, worldPos),
                ResolveDamageNumberColor(attacker));
        }

        private static Vector3 ResolveDamageNumberWorldPos(HurtInfo hurtInfo, PropertyEntity defender)
        {
            if (hurtInfo != null && hurtInfo.m_HasDamageNumberWorldPos)
            {
                return hurtInfo.m_DamageNumberWorldPos;
            }

            return defender.ReadHitPoint();
        }

        private static Vector3 ApplyDamageNumberRandomOffset(PropertyEntity defender, Vector3 worldPos)
        {
            BattleConst.ClampDamageNumberOffsetValues();
            float seed = defender.ReadId() * 0.173f
                         + BattleManager.ReadBattleTime() * 3.731f
                         + defender.ReadHP() * 0.017f
                         + worldPos.x * 1.137f
                         + worldPos.y * 0.619f
                         + worldPos.z * 1.913f;
            float offsetX = Mathf.Lerp(-BattleConst.DamageNumberOffset, BattleConst.DamageNumberOffset, Hash01(seed + 17.11f));
            float offsetY = Mathf.Lerp(-BattleConst.DamageNumberOffset, BattleConst.DamageNumberOffset, Hash01(seed + 43.27f));
            return worldPos + new Vector3(offsetX, offsetY, 0);
        }

        private static bool IsDamageCrit(HurtInfo hurtInfo, PropertyEntity defender, Vector3 worldPos, float hpBefore)
        {
            if (hurtInfo == null)
            {
                return false;
            }

            float critRate = hurtInfo.m_CritRate;
            if (critRate <= 0)
            {
                return false;
            }

            if (critRate >= 1f)
            {
                return true;
            }

            float seed = hurtInfo.m_AttackerId * 0.173f
                         + defender.ReadId() * 0.619f
                         + BattleManager.ReadBattleTime() * 3.731f
                         + hpBefore * 0.017f
                         + worldPos.x * 1.137f
                         + worldPos.y * 1.913f
                         + worldPos.z * 2.417f
                         + hurtInfo.m_Slot * 0.433f;
            return Hash01(seed) < critRate;
        }

        private static float Hash01(float seed)
        {
            return Mathf.Repeat(Mathf.Sin(seed) * 43758.5453f, 1.0f);
        }

        private static string ResolveDamageNumberColor(PropertyEntity attacker)
        {
            if (attacker == null)
            {
                return null;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return null;
            }

            var battlePlayerId = attacker.ReadBattlePlayerId();
            if (battlePlayerId == 0)
            {
                return null;
            }

            for (int seatId = 0; seatId < TowerDefendSeatLayout.MaxSupportedPlayerCount; seatId++)
            {
                if (battle.ReadBattlePlayerIdBySeat(seatId) == battlePlayerId)
                {
                    return GameColor.ReadPlayerDamageColor((int)(BattleManager.GetBattle().ReadPlayers()[seatId].m_RoleCfgId-1000));
                }
            }

            return null;
        }

        private static void TryHandleMonsterDeathStarsEffect(PropertyEntity defender)
        {
            if (defender == null || !defender.ReadIsSmallMonster())
            {
                return;
            }

            if (defender.ReadIsDead())
            {
                RenderEvent.Event.OnTowerDefendMonsterDeathStarsEffect(defender);
            }
        }
    }
}
