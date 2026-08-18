using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Burst.CompilerServices;
using UnityEngine;

namespace GameDll
{
    public enum BattleType
    {
        None = 0,
        TowerDefend = 8, //塔防
    }
    public enum ServerMode
    {
        Net,
        Local_InputEvent,
    }
    public class BattleManager
    {
        private static bool m_IsBattleValid = false;
        private static float m_battleTime;
        private static int m_logicUpdateIndex;
        private static IBattle m_Battle;
        private static BattleType m_BattleType = BattleType.None;
        private static readonly BattleToolCompat m_BattleTool = new BattleToolCompat();

        public static BattleType GetBattleType()
        {
            return m_BattleType;
        }
        public static BattleToolCompat GetBattleTool()
        {
            return m_BattleTool;
        }
        public static float ReadStageTime()
        {
            var battle = BattleManager.GetBattle();
            if (battle != null)
            {
                var pro = battle.GetBattleProgress();
                if (pro != null)
                {
                    return pro.ReadStageTime();
                }
            }
            return float.MaxValue;
        }
        public static bool BulletHitEnemy(Vector3 bulletPos, GroupId bulletHurtGroup, Entity actor, float range)
        {
            return BulletHitEnemy(bulletPos, bulletPos, bulletHurtGroup, actor, range);
        }

        public static bool BulletHitEnemy(Vector3 bulletStartPos, Vector3 bulletEndPos, GroupId bulletHurtGroup, Entity actor, float range)
        {
            if (actor == null)
            {
                return false;
            }
            if (!actor.ReadCanBeTarget())
            {
                return false;
            }

            if (bulletHurtGroup != actor.ReadHurtGroup())
            {
                return actor.TryIntersectSegment(bulletStartPos, bulletEndPos, Mathf.Max(0f, range), out _, out _);
            }
            return false;

        }

        public static bool TryIntersectSegmentSphere(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            Vector3 sphereCenter,
            float sphereRadius,
            out float hitT,
            out Vector3 hitPoint)
        {
            hitT = 0.0f;
            hitPoint = segmentEnd;

            var radiusSqr = sphereRadius * sphereRadius;
            if ((segmentStart - sphereCenter).sqrMagnitude <= radiusSqr)
            {
                hitPoint = segmentStart;
                return true;
            }

            var delta = segmentEnd - segmentStart;
            var a = Vector3.Dot(delta, delta);
            if (a <= 0.000001f)
            {
                return false;
            }

            var offset = segmentStart - sphereCenter;
            var b = 2.0f * Vector3.Dot(offset, delta);
            var c = Vector3.Dot(offset, offset) - radiusSqr;
            var discriminant = b * b - 4.0f * a * c;
            if (discriminant < 0.0f)
            {
                return false;
            }

            var sqrtDiscriminant = Mathf.Sqrt(discriminant);
            var inverseDenominator = 1.0f / (2.0f * a);
            var t0 = (-b - sqrtDiscriminant) * inverseDenominator;
            var t1 = (-b + sqrtDiscriminant) * inverseDenominator;
            var clampedT = -1.0f;
            if (t0 >= 0.0f && t0 <= 1.0f)
            {
                clampedT = t0;
            }
            else if (t1 >= 0.0f && t1 <= 1.0f)
            {
                clampedT = t1;
            }

            if (clampedT < 0.0f)
            {
                return false;
            }

            hitT = clampedT;
            hitPoint = segmentStart + delta * clampedT;
            return true;
        }

        public static bool TryIntersectSegmentCapsule(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            Vector3 capsuleStart,
            Vector3 capsuleEnd,
            float capsuleRadius,
            out float hitT,
            out Vector3 hitPoint)
        {
            hitT = 0.0f;
            hitPoint = segmentEnd;

            var radius = Mathf.Max(0.0f, capsuleRadius);
            var radiusSqr = radius * radius;
            if (DistancePointSegmentSqr(segmentStart, capsuleStart, capsuleEnd) <= radiusSqr)
            {
                hitPoint = segmentStart;
                return true;
            }

            var delta = segmentEnd - segmentStart;
            if (delta.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            ClosestPtSegmentSegment(
                segmentStart,
                segmentEnd,
                capsuleStart,
                capsuleEnd,
                out var closestSegmentT,
                out _,
                out _,
                out _,
                out var closestDistSqr);
            if (closestDistSqr > radiusSqr)
            {
                return false;
            }

            var upperT = Mathf.Clamp01(closestSegmentT);
            if (upperT <= 0.0f)
            {
                return false;
            }

            var lowerT = 0.0f;
            for (int i = 0; i < 18; i++)
            {
                var midT = (lowerT + upperT) * 0.5f;
                var point = segmentStart + delta * midT;
                if (DistancePointSegmentSqr(point, capsuleStart, capsuleEnd) <= radiusSqr)
                {
                    upperT = midT;
                }
                else
                {
                    lowerT = midT;
                }
            }

            hitT = upperT;
            hitPoint = segmentStart + delta * upperT;
            return true;
        }

        private static float DistancePointSegmentSqr(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            var delta = segmentEnd - segmentStart;
            var lengthSqr = delta.sqrMagnitude;
            if (lengthSqr <= 0.000001f)
            {
                return (point - segmentStart).sqrMagnitude;
            }

            var t = Vector3.Dot(point - segmentStart, delta) / lengthSqr;
            t = Mathf.Clamp01(t);
            var closestPoint = segmentStart + delta * t;
            return (point - closestPoint).sqrMagnitude;
        }

        private static void ClosestPtSegmentSegment(
            Vector3 p1,
            Vector3 q1,
            Vector3 p2,
            Vector3 q2,
            out float s,
            out float t,
            out Vector3 c1,
            out Vector3 c2,
            out float sqrDist)
        {
            var d1 = q1 - p1;
            var d2 = q2 - p2;
            var r = p1 - p2;
            var a = Vector3.Dot(d1, d1);
            var e = Vector3.Dot(d2, d2);
            var f = Vector3.Dot(d2, r);

            if (a <= 0.000001f && e <= 0.000001f)
            {
                s = 0.0f;
                t = 0.0f;
                c1 = p1;
                c2 = p2;
                sqrDist = (c1 - c2).sqrMagnitude;
                return;
            }

            if (a <= 0.000001f)
            {
                s = 0.0f;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                var c = Vector3.Dot(d1, r);
                if (e <= 0.000001f)
                {
                    t = 0.0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    var b = Vector3.Dot(d1, d2);
                    var denom = a * e - b * b;
                    if (denom != 0.0f)
                    {
                        s = Mathf.Clamp01((b * f - c * e) / denom);
                    }
                    else
                    {
                        s = 0.0f;
                    }

                    t = (b * s + f) / e;
                    if (t < 0.0f)
                    {
                        t = 0.0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1.0f)
                    {
                        t = 1.0f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }

            c1 = p1 + d1 * s;
            c2 = p2 + d2 * t;
            sqrDist = (c1 - c2).sqrMagnitude;
        }
        public static bool ReadIsEntityValide(Entity ent, int ent_id = 0)
        {
            if (ent_id == 0)
            {
                return ent != null && !ent.ReadIsDestroy() && !ent.ReadIsDead();
            }
            else
            {
                return ent != null && ent_id == ent.ReadId() && !ent.ReadIsDestroy() && !ent.ReadIsDead();
            }
        }

        //这个是简单判断是否释放当前位置的技能，只做判断
        public static bool IsCanUseSkillDirect(PropertyEntity entity, int slot)
        {
            if (entity == null)
            {
                return false;
            }
            if (!BattleManager.ReadIsEntityValide(entity))
            {
                return false;
            }
            var skillMgr = entity.GetSkillManager();
            var curSkill = skillMgr.GetCurrentSkill();
            if (curSkill != null)
            {
                return false;
            }
            if (entity.ReadIsBeingControlled())
            {
                return false;
            }
            var use_skill = skillMgr.ReadSkillBySlot(slot);
            if (use_skill == null)
            {
                return false;
            }

            if (entity.CanAttack(use_skill) != AttackFailedReason.Success)
            {
                return false;
            }
            return true;
        }

        public static bool IsCanUseSkill_WillNextUseSkill(PropertyEntity entity, int slot, PropertyEntity defender)
        {
            if (entity == null)
            {
                return false;
            }
            var skillMgr = entity.GetSkillManager();
            skillMgr.ClearWillNextUseSkill();

            if (!BattleManager.ReadIsEntityValide(entity))
            {
                return false;
            }

            if (entity.ReadIsBeingControlled())
            {
                return false;
            }

            var use_skill = skillMgr.ReadSkillBySlot(slot);
            if (use_skill == null)
            {
                return false;
            }

            if (entity.CanAttack(use_skill) != AttackFailedReason.Success)
            {
                skillMgr.SetWillNextUseSkill(use_skill, defender);
                return false;
            }



            var curSkill = skillMgr.GetCurrentSkill();
            if (curSkill == null)
            {
                return true;
            }
            else
            {
                skillMgr.SetWillNextUseSkill(use_skill, defender);
                return false;
            }
        }


        public static float ConvertFrame2Second(int frame, float speed, int frameRate)
        {
            //攻击间隔时间秒（配置基准为100）
            //int rate = 30; //30帧每秒
            var base_attack_speed = 1.0f;
            var second = frame * base_attack_speed / speed / frameRate;
            if (second <= 0)
            {
                //防止因为攻击速度太大，导致的异常
                second = 0.001f;
            }
            return second;
        }

        public static void Attack(PropertyEntity attacker, int defender, int slot, Vector3 defender_pos)
        {
            //进入攻击状态
            attacker.TryChangeState(emEntityState.em_EntityState_Attack);

            var attacker_pos = attacker.GetPosition();
            var dir = defender_pos - attacker_pos;
            dir = dir.normalized;
            attacker.Attack(slot, dir, defender_pos, defender);
            //Debug.Log(attacker.GetId() + " 使用技能：" + Time.timeAsDouble);
        }

        public static void Attack(PropertyEntity attacker, PropertyEntity defender, Skill use_skill)
        {
            //进入攻击状态
            attacker.TryChangeState(emEntityState.em_EntityState_Attack);

            var attacker_pos = attacker.GetPosition();
            var defender_pos = defender.GetPosition();
            attacker.SetDefender(defender);
            var dir = defender_pos - attacker_pos;
            dir = dir.normalized;
            attacker.Attack(use_skill, dir, defender_pos, defender);
            //Debug.Log(attacker.GetId() + " 使用技能：" + Time.timeAsDouble);
        }

        public static void Attack(PropertyEntity attacker, PropertyEntity defender, int slot)
        {
            //进入攻击状态
            attacker.TryChangeState(emEntityState.em_EntityState_Attack);

            var attacker_pos = attacker.GetPosition();
            var defender_pos = defender.GetPosition();
            attacker.SetDefender(defender);
            var dir = defender_pos - attacker_pos;
            dir = dir.normalized;
            attacker.Attack(slot, dir, defender_pos, defender.ReadId());
            //Debug.Log(attacker.GetId() + " 使用技能：" + Time.timeAsDouble);
        }
        public static void AttackDir(int slot, PropertyEntity attacker, Vector3 face_forward, Vector3 move_dir)
        {
            attacker.SetDefender(null);
            if (attacker.ReadShouldSyncFacingOnAttack())
            {
                attacker.SetForward(face_forward.normalized);
            }
            attacker.TryChangeState(emEntityState.em_EntityState_Attack);
            attacker.AttackDir(slot, face_forward, move_dir);

            //Debug.Log(attacker.GetId() + " 使用技能：" + Time.timeAsDouble);
        }







        public static bool IsBattleValid()
        {
            return m_IsBattleValid;
        }
        public static void Init(BattleData info, BattleType battleType)
        {
            Debug.Log("初始化BattleManager");

            //初始化变量
            m_IsBattleValid = true;




            m_battleTime = 0;

            m_logicUpdateIndex = 0;
            BulletObj.ResetSharedTrackingState();

            //初始化表数据
            InitBean();

            m_BattleType = battleType;
            switch(m_BattleType)
            {
                case BattleType.TowerDefend:
                    {
                        m_Battle = new TowerDefendBattle();
                        break;
                    }
                default:
                    {
                        Debug.LogError("unsupported battle type:" + battleType);
                        m_Battle = null;
                        break;
                    }
            }
            if (m_Battle == null)
            {
                return;
            }
            m_Battle.OnCreate(info);
            Debug.Log("m_Battle.OnCreate(info);");
            RenderEvent.Event.OnCallFunction("BattleManagerInited", "");
        }

        private static void InitBean()
        {
            Debug.Log("清理配置表，防止误修改导致不同步");
            Assembly ass = Assembly.GetExecutingAssembly();
            var types = ass.GetTypes();
            foreach(var type in types)
            {
                if(type.BaseType == typeof(BeanBase))
                {
                    var method = type.GetMethod("ClearConfig");
                    method.Invoke(null, null);
                }
            }
            Debug.Log("清理配置表完毕");
        }
        public static void Update(float timeDelta)
        {
            if (!m_IsBattleValid || m_Battle == null)
            {
                return;
            }

            LogicUpdate(timeDelta);
        }
        public static void UpdateRender(float dt)
        {
            if(m_Battle != null)
            {
                m_Battle.UpdateRender(dt);
            }


        }

        public static void Destroy()
        {
            m_BattleType = BattleType.None;

            m_IsBattleValid = false;
            BulletObj.ResetSharedTrackingState();
            if (m_Battle != null)
            {
                m_Battle.OnRelease();
                m_Battle = null;
            }
        }


        public static void LogicUpdate(float logic_time)
        {
            m_Battle.Update(logic_time);

            m_battleTime += logic_time;
            m_logicUpdateIndex++;
        }
        public static float ReadBattleTime()
        {
            return m_battleTime;
        }


        public static IBattle GetBattle()
        {
            return m_Battle;
        }
        public static ObjectManager GetObjectManager()
        {
            var battle = GetBattle();
            if(battle == null)
            {
                return null;
            }
            return battle.GetObjectManager();
        }
        public static Entity ReadEntity(int entity_id)
        {
            var battle = GetBattle();
            if (battle == null)
            {
                return null;
            }
            var objMgr = battle.GetObjectManager();
            if(objMgr == null)
            {
                return null;
            }
            return objMgr.ReadEntity(entity_id);
        }

        public static void OnDrawGizmos()
        {
            if(m_Battle != null)
            {
                m_Battle.OnDrawGizmos();
            }
        }
        public static bool isInCircle(Vector3 position, float radius, Vector3 center)
        {
            return Vector3.Distance(position, center) <= radius;
        }
        public static bool isInSector(Vector3 position, Vector3 forward, float radius, float degree, Vector3 center)
        {
            if (Vector3.Distance(position, center) > radius)
            {
                return false;
            }
            var lineDir = position - center;
            forward.y = 0;
            lineDir.y = 0;
            var angle2 = Vector3.Angle(forward, lineDir);
            var isIn2 = angle2 <= degree / 2;
            return isIn2;
        }
        public static bool isInRectangle(Vector3 targetPos, Vector3 rect_forward, float change, float kuan, Vector3 rect_start_pos)
        {
            rect_forward.y = 0;
            rect_forward = rect_forward.normalized;
            targetPos.y = 0;
            rect_start_pos.y = 0;
            var center = rect_start_pos + rect_forward * change / 2;
            //目标点到中心点的向量
            var dir = targetPos - center;
            //求相对于矩形方向的x的投影值
            var angle = Vector3.Angle(rect_forward, dir);
            var dist = Vector3.Distance(targetPos, center);
            var dkuan = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
            var dchang = dist * Mathf.Cos(angle * Mathf.Deg2Rad);

            var ddkuan = dkuan + dkuan;
            var ddchang = dchang + dchang;

            if (Mathf.Abs(ddkuan) > kuan || Mathf.Abs(ddchang) > change)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static bool IsHitEnemy(HitDetectionData hit_data, PropertyEntity defender)
        {
            var defenderPos = defender.GetPosition();
            if (hit_data.hitType == HitDetectionShapeType.SkillPosCircle ||
               hit_data.hitType == HitDetectionShapeType.CasterCircle)
            {
                if (BattleManager.isInCircle(defenderPos, (hit_data.hurt_range + defender.ReadRadius()), hit_data.pos))
                {
                    return true;
                }
            }
            else if (hit_data.hitType == HitDetectionShapeType.CasterAngle)
            {
                if (BattleManager.isInSector(defenderPos, hit_data.dir, (hit_data.hurt_range + defender.ReadRadius()),
                    hit_data.angle, hit_data.pos))
                {
                    return true;
                }
            }
            else if (hit_data.hitType == HitDetectionShapeType.CasterRect)
            {
                if (BattleManager.isInRectangle(defenderPos, hit_data.dir, hit_data.chang + defender.ReadRadius(),
                    hit_data.kuan + defender.ReadRadius(), hit_data.pos))
                {
                    return true;
                }
            }
            return false;
        }
        
        //面向范围内夹角最小的
        public static PropertyEntity GetNearstDirEnemy(Vector3 skillDir, Vector3 casterPos, float dist, GroupId group, bool ignoreY)
        {
            var objMgr = GetObjectManager();
            if (objMgr == null)
            {
                return null;
            }

            var enemies = objMgr.ReadPropertyEntities();
            if (enemies == null || enemies.Count == 0)
            {
                return null;
            }

            if (ignoreY)
            {
                skillDir.y = 0;
                casterPos.y = 0;
            }

            var hasForward = skillDir.sqrMagnitude > 0.0001f;
            if (hasForward)
            {
                skillDir.Normalize();
            }

            var castDistance = Mathf.Max(0.1f, dist);
            PropertyEntity bestTarget = null;
            float bestAngle = float.MaxValue;
            float bestDistance = float.MaxValue;

            int count = enemies.Count;
            for (int i = 0; i < count; i++)
            {
                var enemy = enemies[i];
                if (!ReadIsEntityValide(enemy))
                {
                    continue;
                }

                if (!enemy.ReadCanBeTarget() || enemy.ReadHurtGroup() == group)
                {
                    continue;
                }

                var targetPos = enemy.GetPosition();
                if (ignoreY)
                {
                    targetPos.y = 0;
                }

                var toTarget = targetPos - casterPos;
                var targetDistance = toTarget.magnitude;
                if (targetDistance > castDistance)
                {
                    continue;
                }

                if (!hasForward)
                {
                    if (targetDistance < bestDistance)
                    {
                        bestTarget = enemy;
                        bestDistance = targetDistance;
                    }
                    continue;
                }

                if (toTarget.sqrMagnitude <= 0.0001f)
                {
                    return enemy;
                }

                toTarget.Normalize();
                var angle = Vector3.Angle(skillDir, toTarget);
                if (angle > 60.0f)
                {
                    continue;
                }

                if (angle < bestAngle ||
                    (Mathf.Approximately(angle, bestAngle) && targetDistance < bestDistance))
                {
                    bestTarget = enemy;
                    bestAngle = angle;
                    bestDistance = targetDistance;
                }
            }

            return bestTarget;
        }
    }
}
