using GameDll;
using LCL;
using MonoBean;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameDll
{
    public sealed  class TrackingProjectileAlgorithm
    {
        private enum State
        {
            Idle,
            Wobbling,
            Turning,
            Targeting
        }

        private const float DirectionEpsilon = 0.0001f;
        private const float CloseTurnAssistLeadTime = 0.2f;
        private const float CloseTurnAssistMinRange = 2.0f;
        private const float CloseTurnAssistMinTurnTime = 0.35f;
        private const float CloseTurnAssistMaxTurnTime = 1.2f;
        private const float CloseTurnAssistStartAngle = 15.0f;
        private const float CloseTurnAssistFullAngle = 75.0f;

        private State m_State = State.Idle;
        private float m_CurrentWobbleTime;
        private float m_WobbleDuration;
        private float m_CurrentTurnTime;
        private Vector3 m_TargetWobbleVector = Vector3.zero;
        private Vector3 m_LastTargetPosition = Vector3.zero;
        private bool m_HasLastTargetPosition;

        public void Reset(Vector3 targetPosition, bool startWithWobble)
        {
            BattleConst.ClampValues();
            m_CurrentWobbleTime = 0f;
            m_WobbleDuration = Random.Range(BattleConst.ArcDurationMin, BattleConst.ArcDurationMax);
            m_CurrentTurnTime = 0f;
            m_LastTargetPosition = targetPosition;
            m_HasLastTargetPosition = true;
            SelectNewWobbleVector();
            m_State = startWithWobble ? State.Wobbling : State.Targeting;
        }

        public void RestartTracking(Vector3 targetPosition)
        {
            m_CurrentTurnTime = 0f;
            m_LastTargetPosition = targetPosition;
            m_HasLastTargetPosition = true;
            m_State = State.Turning;
        }

        public void Stop()
        {
            m_State = State.Idle;
            m_CurrentWobbleTime = 0f;
            m_WobbleDuration = 0f;
            m_CurrentTurnTime = 0f;
            m_TargetWobbleVector = Vector3.zero;
            m_LastTargetPosition = Vector3.zero;
            m_HasLastTargetPosition = false;
        }

        private Vector3 EstimateTargetVelocity(Vector3 targetPosition, float deltaTime)
        {
            if (!m_HasLastTargetPosition || deltaTime <= DirectionEpsilon)
            {
                return Vector3.zero;
            }

            return (targetPosition - m_LastTargetPosition) / deltaTime;
        }

        private void SelectNewWobbleVector()
        {
            m_TargetWobbleVector = new Vector3(
                Random.Range(BattleConst.ArcUpwardAngleMin, BattleConst.ArcUpwardAngleMax),
                Random.Range(-BattleConst.ArcHorizontalAngle, BattleConst.ArcHorizontalAngle),
                0f);
        }

        private static Vector3 ApplyTrackingOffset(Vector3 baseForward, float yawAngle, float upwardPitchAngle)
        {
            var safeForward = NormalizeOrFallback(baseForward, Vector3.forward);
            var yawForward = (Quaternion.Euler(0f, yawAngle, 0f) * safeForward).normalized;
            var pitchAxis = Vector3.Cross(yawForward, Vector3.up);
            if (pitchAxis.sqrMagnitude <= DirectionEpsilon)
            {
                pitchAxis = Vector3.left;
            }

            var arcForward = Quaternion.AngleAxis(upwardPitchAngle, pitchAxis.normalized) * yawForward;
            return NormalizeOrFallback(arcForward, safeForward);
        }

        private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
        {
            if (value.sqrMagnitude > DirectionEpsilon)
            {
                return value.normalized;
            }

            if (fallback.sqrMagnitude > DirectionEpsilon)
            {
                return fallback.normalized;
            }

            return Vector3.forward;
        }

        public static Vector3 CalculateInterceptVelocity(
            Vector3 startPoint,
            Vector3 targetPoint,
            Vector3 targetVelocity,
            float projectileSpeed)
        {
            if (projectileSpeed <= DirectionEpsilon)
            {
                return Vector3.zero;
            }

            Vector3 delta = targetPoint - startPoint;
            float a = targetVelocity.sqrMagnitude - projectileSpeed * projectileSpeed;
            float b = 2f * Vector3.Dot(delta, targetVelocity);
            float c = delta.sqrMagnitude;

            if (Mathf.Abs(a) <= DirectionEpsilon)
            {
                if (Mathf.Abs(b) <= DirectionEpsilon)
                {
                    return Vector3.zero;
                }

                float linearTime = -c / b;
                if (linearTime <= 0f)
                {
                    return Vector3.zero;
                }

                return (delta + targetVelocity * linearTime) / linearTime;
            }

            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f)
            {
                return Vector3.zero;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float t1 = (-b + sqrtDiscriminant) / (2f * a);
            float t2 = (-b - sqrtDiscriminant) / (2f * a);
            float time = float.MaxValue;

            if (t1 > 0f)
            {
                time = t1;
            }

            if (t2 > 0f && t2 < time)
            {
                time = t2;
            }

            if (time == float.MaxValue)
            {
                return Vector3.zero;
            }

            return (delta + targetVelocity * time) / time;
        }

        public Vector3 UpdateDirection(
            Vector3 currentPosition,
            Vector3 currentForward,
            Vector3 targetPosition,
            float projectileSpeed,
            float deltaTime)
        {
            Vector3 safeForward = NormalizeOrFallback(currentForward, Vector3.forward);
            Vector3 targetVelocity = EstimateTargetVelocity(targetPosition, deltaTime);
            Vector3 desiredHeading = GetHeading(
                currentPosition,
                targetPosition,
                targetVelocity,
                projectileSpeed,
                safeForward);

            Vector3 nextForward = desiredHeading;
            switch (m_State)
            {
                case State.Wobbling:
                    nextForward = UpdateWobbling(safeForward, desiredHeading, deltaTime);
                    break;
                case State.Turning:
                    nextForward = UpdateTurning(
                        currentPosition,
                        targetPosition,
                        safeForward,
                        desiredHeading,
                        projectileSpeed,
                        deltaTime);
                    break;
                case State.Targeting:
                case State.Idle:
                default:
                    nextForward = desiredHeading;
                    break;
            }

            m_LastTargetPosition = targetPosition;
            m_HasLastTargetPosition = true;
            return NormalizeOrFallback(nextForward, desiredHeading);
        }

        public Vector3 BuildLaunchDirection(
            Vector3 currentPosition,
            Vector3 currentForward,
            Vector3 targetPosition,
            float projectileSpeed)
        {
            Vector3 safeForward = NormalizeOrFallback(currentForward, Vector3.forward);
            Vector3 desiredHeading = GetHeading(
                currentPosition,
                targetPosition,
                Vector3.zero,
                projectileSpeed,
                safeForward);

            if (m_State != State.Wobbling)
            {
                return desiredHeading;
            }

            return NormalizeOrFallback(
                ApplyTrackingOffset(desiredHeading, m_TargetWobbleVector.y, m_TargetWobbleVector.x),
                desiredHeading);
        }

        private Vector3 UpdateWobbling(Vector3 currentForward, Vector3 desiredHeading, float deltaTime)
        {
            m_CurrentWobbleTime += deltaTime;
            float wobbleProgress = m_WobbleDuration <= DirectionEpsilon
                ? 1f
                : Mathf.Clamp01(m_CurrentWobbleTime / m_WobbleDuration);
            float yawAngle = Mathf.Lerp(m_TargetWobbleVector.y, 0f, wobbleProgress);
            float upwardPitchAngle = Mathf.Lerp(m_TargetWobbleVector.x, 0f, wobbleProgress);
            Vector3 wobbleForward = ApplyTrackingOffset(desiredHeading, yawAngle, upwardPitchAngle);
            Vector3 nextForward = Vector3.Slerp(
                currentForward,
                wobbleForward,
                Mathf.Clamp01(deltaTime * BattleConst.ArcBlendSpeed));
            if (wobbleProgress >= 1f)
            {
                m_State = State.Targeting;
            }

            return NormalizeOrFallback(nextForward, desiredHeading);
        }

        private static float ResolveTurningDuration(
            Vector3 currentPosition,
            Vector3 targetPosition,
            Vector3 currentForward,
            Vector3 desiredHeading,
            float projectileSpeed)
        {
            float baseTurnTime = BattleConst.TrackingTurnTime;
            if (baseTurnTime <= DirectionEpsilon)
            {
                return 0f;
            }

            float closeRange = Mathf.Max(CloseTurnAssistMinRange, projectileSpeed * CloseTurnAssistLeadTime);
            float distanceToTarget = Vector3.Distance(currentPosition, targetPosition);
            if (distanceToTarget >= closeRange)
            {
                return baseTurnTime;
            }

            float headingAngle = Vector3.Angle(currentForward, desiredHeading);
            float angleFactor = Mathf.InverseLerp(CloseTurnAssistStartAngle, CloseTurnAssistFullAngle, headingAngle);
            if (angleFactor <= 0f)
            {
                return baseTurnTime;
            }

            // 近距离如果仍沿用远距离的慢回正，会在目标附近绕圈。
            // 这里保留 Turning 阶段本身，只在离目标较近且偏角较大时压缩回正时间。
            float distanceFactor = Mathf.Clamp01(distanceToTarget / closeRange);
            float closeTurnTime = Mathf.Lerp(CloseTurnAssistMinTurnTime, CloseTurnAssistMaxTurnTime, distanceFactor);
            closeTurnTime = Mathf.Min(baseTurnTime, closeTurnTime);
            return Mathf.Lerp(baseTurnTime, closeTurnTime, angleFactor);
        }

        private Vector3 UpdateTurning(
            Vector3 currentPosition,
            Vector3 targetPosition,
            Vector3 currentForward,
            Vector3 desiredHeading,
            float projectileSpeed,
            float deltaTime)
        {
            float effectiveTurnTime = ResolveTurningDuration(
                currentPosition,
                targetPosition,
                currentForward,
                desiredHeading,
                projectileSpeed);
            if (effectiveTurnTime <= DirectionEpsilon)
            {
                m_State = State.Targeting;
                return desiredHeading;
            }

            m_CurrentTurnTime += deltaTime;
            float turnProgress = Mathf.Clamp01(m_CurrentTurnTime / effectiveTurnTime);
            Vector3 turnedForward = Vector3.Slerp(currentForward, desiredHeading, turnProgress);
            if (turnProgress >= 1f)
            {
                m_State = State.Targeting;
            }

            return NormalizeOrFallback(turnedForward, desiredHeading);
        }

        private static Vector3 GetHeading(
            Vector3 currentPosition,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            float projectileSpeed,
            Vector3 fallbackForward)
        {
            Vector3 interceptVelocity = CalculateInterceptVelocity(
                currentPosition,
                targetPosition,
                targetVelocity,
                projectileSpeed);
            if (interceptVelocity.sqrMagnitude > DirectionEpsilon)
            {
                return interceptVelocity.normalized;
            }

            return NormalizeOrFallback(targetPosition - currentPosition, fallbackForward);
        }
    }
    // 场景上的脱离于施法者的特效、子弹等
    /*
    子弹配置说明（给策划）

    一、这类子弹当前实际使用的关键配置
    - t_move_speed：飞行速度，单位毫米/秒，代码里会除以 1000，当作米/秒使用。
    - t_max_time：最大飞行时间，单位毫秒。
    - t_penetrate：0 = 穿透，1 = 命中即停。这个值和直觉相反，最容易配错。
    - t_size：命中半径，单位毫米。0 = 单体；> 0 = 范围命中。范围判定只看 XZ 平面距离，不看高度差。
    - t_trajectory：0 = 直线，1 = 抛物线，2 = 跟踪，3 = 激光（直线持续光束），4 = 水柱（随世界弯曲的持续喷射）。
    - t_tracking_range：跟踪搜索半径，单位毫米。只对 t_trajectory = 2 生效。搜索圆心是子弹当前位置。
    - t_trigger_bullet_id / t_trigger_bullet_count / t_trigger_type：触发子弹配置。当前 trigger_type = 0 时，会在子弹结束时触发。
    - t_bullet_hittarget_buff_id：命中目标时附加 Buff。没有就不填。

    二、这类子弹还会受到发射方式影响
    - HurtInfo.m_NeedTarget = 0：无锁定目标。子弹按当前 forward 飞。
    - HurtInfo.m_NeedTarget != 0：有锁定目标。子弹走“有目标”流程。
    - 这个值不是 t_bullet 自身字段，而是技能发射时传进来的。

    三、常用配置组合
    - 直线单体子弹：
      t_trajectory = 0，t_penetrate = 1，t_size = 0
      说明：有目标时会持续朝目标飞；无目标时沿当前 forward 直飞。

    - 直线穿透子弹：
      t_trajectory = 0，t_penetrate = 0，t_size = 0
      说明：命中后不停，可以连续穿过多个目标。

    - 直线范围子弹：
      t_trajectory = 0，t_penetrate = 1，t_size > 0
      说明：飞行过程中用半径做范围命中，命中后停止。

    - 直线范围穿透子弹：
      t_trajectory = 0，t_penetrate = 0，t_size > 0
      说明：飞行过程中持续按范围判定，可多次命中多个敌人。

    - 抛物线子弹：
      t_trajectory = 1
      常见搭配：t_penetrate = 1，t_size = 0 或 > 0
      说明：发射初速度来自当前发射朝向和 t_move_speed。
      说明：t_Gravity 用来控制竖直方向的重力加速度倍率，1000 表示使用当前默认重力，0 表示不受重力影响，负数表示反重力。
      说明：飞行轨迹始终按当前发射朝向和重力公式推进。

    - 跟踪子弹（出生就有目标）：
      t_trajectory = 2，HurtInfo.m_NeedTarget != 0
      建议搭配：t_tracking_range > 0
      说明：如果当前目标没有超过单怪建议负载，会直接锁定并追踪。
      说明：如果当前场上目标过少，超出建议负载的子弹会先短暂观察，再决定是否强行锁定。

    - 跟踪子弹（出生无目标，飞行中找目标）：
      t_trajectory = 2，HurtInfo.m_NeedTarget = 0
      建议搭配：t_tracking_range > 0
      说明：开局先直线飞；飞行中会优先找负载较低的目标，尽量把跟踪弹分散开。
      说明：如果当前所有候选目标都已达到建议负载，子弹会先等一小段时间，再锁当前最想追的目标。

    - 跟踪穿透子弹：
      t_trajectory = 2，t_penetrate = 0
      说明：命中已锁定目标后会继续飞，但不会再改锁别的目标。

    - 看起来像直飞的“伪跟踪弹”：
      t_trajectory = 2，t_tracking_range = 0，且出生时不传目标
      说明：因为永远找不到目标，所以表现上接近普通直线飞行。一般不建议这样配。

    - 激光持续光束：
      t_trajectory = 3
      说明：直线光束，命中按“完整弯折后的画面形体”判定；t_penetrate 决定贯穿还是止于最近目标。
      说明：材质不走 Curve World 弯折，瞄准线与命中特效同样按直线口径处理。

    - 水柱持续喷射：
      t_trajectory = 4
      说明：特效固定在开火点、跟随瞄准方向；命中按逻辑空间求交，水柱与怪物一起随世界弯曲。
      说明：靠特效自身的粒子碰撞表现喷溅，不再额外播一次性受击/爆炸特效。

    四、当前代码里的重要行为约束
    - 跟踪目标在真正锁定前，会优先避开已经被过多跟踪弹占用的怪物。
    - 当前默认建议负载是单怪最多 2 个跟踪弹；这是软限制，不是硬禁止。
    - 如果当前没有负载更低的目标，子弹会按“预计命中耗时的一半”做一次短等待，超时后再锁当前最想追的目标。
    - 跟踪目标一旦真正锁定，就不会切换到别的目标。
    - 跟踪目标如果中途死亡，子弹不会立刻消失，而是继续飞向目标最后记录的位置，飞到后结束。
    - 跟踪弹如果出生时没有目标，不会原地重置，而是继续飞行；等待期间如果有可参考的已占用目标，会先朝那个方向飞。
    - t_tracking_range 只影响“能不能找到目标”，不会影响转向速度。
    - 抛物线的竖直参数目前不是策划可配项；如果后面要配，需要单独加表字段。
    */
    public class BulletObj : PlayableEffectObj
    {
        private const float m_DefaultSingleTargetHitRange = 0.2f;
        private const float m_DefaultTrackedPointArrivalRange = 0.2f;
        private const float m_DefaultLostTargetArrivalRange = 0.5f;
        private const int m_DefaultTrackingSoftTargetLimit = 2;
        private const float m_DefaultTrackingDelayedLockRatio = 0.5f;
        private static readonly Dictionary<int, int> s_TrackingTargetAssignmentByBulletId = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> s_TrackingTargetLoadCounts = new Dictionary<int, int>();
        // 触发多发追踪弹的轮次序号，用于让前后多次发弹的扇面互相错开而不是重叠
        private static int s_TrackingTriggerVolleyIndex;
        private const float TrackingTriggerVolleyGoldenRatioConjugate = 0.6180339887f;

        private struct TrackingTargetCandidate
        {
            public PropertyEntity m_Target;
            public int m_TargetId;
            public Vector3 m_TargetPos;
            public float m_SqrDistance;
            public int m_AssignedBulletCount;
        }

        public enum BulletState
        {
            Init,
            Fly,
            Finish,
            AfterFinish,
        }

        private interface IBulletTrajectory
        {
            void OnStart(BulletObj owner);
            void OnReset(BulletObj owner);
            void OnTargetInvalid(BulletObj owner);
            bool HandleNoTarget(BulletObj owner, float dt);
            bool HandleMissingTarget(BulletObj owner, float dt);
            void UpdateTargetDirection(BulletObj owner, float dt);
            Vector3 CalculateNextPosition(BulletObj owner, float dt);
        }

        private abstract class BulletTrajectoryBase : IBulletTrajectory
        {
            public virtual void OnStart(BulletObj owner)
            {
            }

            public virtual void OnReset(BulletObj owner)
            {
            }

            public virtual void OnTargetInvalid(BulletObj owner)
            {
                owner.m_MainDefender = null;
                owner.m_DefenderId = 0;
            }

            public virtual bool HandleNoTarget(BulletObj owner, float dt)
            {
                return false;
            }

            public virtual bool HandleMissingTarget(BulletObj owner, float dt)
            {
                return false;
            }

            public virtual void UpdateTargetDirection(BulletObj owner, float dt)
            {
                owner.AimAt(owner.m_LastDefenderPos);
            }

            public virtual Vector3 CalculateNextPosition(BulletObj owner, float dt)
            {
                var pos = owner.GetPosition();
                float moveSpeed = owner.GetMoveSpeed();
                if (moveSpeed <= 0f)
                {
                    return pos;
                }

                return pos + owner.GetForward() * dt * moveSpeed;
            }
        }

        private sealed class LinearBulletTrajectory : BulletTrajectoryBase
        {
        }

        // 持续光束弹道（t_trajectory=3 激光 / 4 水柱）：真正的位置/命中/表现全部由 UpdateLaserBeam /
        // UpdateSprayStream 每帧驱动（跟随施法者朝向的持续光束），这里只作为工厂占位，不做弹道运动。
        private sealed class LaserBulletTrajectory : BulletTrajectoryBase
        {
        }


        private sealed class ParabolicBulletTrajectory : BulletTrajectoryBase
        {
            private Vector3 m_Velocity;
            private float m_Gravity;

            public override void OnStart(BulletObj owner)
            {
                var launchForward = owner.GetForward();
                var bulletBean = owner.GetBulletBean();
                if (launchForward.sqrMagnitude <= 0.0001f)
                {
                    launchForward = Vector3.forward;
                }

                m_Velocity = launchForward.normalized * owner.GetMoveSpeed();
                if (bulletBean != null)
                {
                    m_Gravity = TowerDefendBattle.ResolveRuntimeBulletGravityAcceleration(
                        owner.GetHurtInfo() != null ? owner.GetHurtInfo().m_Attacker : null,
                        bulletBean);
                }
                else
                {
                    m_Gravity = 0f;
                }
            }

            public override void OnReset(BulletObj owner)
            {
                m_Velocity = Vector3.zero;
                m_Gravity = 0f;
            }

            public override Vector3 CalculateNextPosition(BulletObj owner, float dt)
            {
                var pos = owner.GetPosition();
                float moveSpeed = owner.GetMoveSpeed();
                if (moveSpeed <= 0f)
                {
                    return pos;
                }

                m_Velocity.y -= m_Gravity * dt;
                pos += m_Velocity * dt;
                if (m_Velocity.sqrMagnitude > 0.0001f)
                {
                    owner.SetForward(m_Velocity.normalized);
                }
                return pos;
            }
        }

        private sealed class TrackingBulletTrajectory : BulletTrajectoryBase
        {
            private readonly TrackingProjectileAlgorithm m_Algorithm = new TrackingProjectileAlgorithm();
            private bool m_AlgorithmEnabled;
            private int m_TrackedTargetId;
            private bool m_TargetLocked;
            private bool m_TargetLost;
            private float m_StartupHoldRemaining;
            private bool m_SkipWobbleOnFirstActivation;
            private bool m_HasLaunchSpreadWindow;
            private int m_PendingTargetId;
            private int m_PendingGuidanceTargetId;
            private float m_PendingLockRemaining;

            public override void OnStart(BulletObj owner)
            {
                Resetpublic();

                if (owner.m_MainDefender == null)
                {
                    PrepareStartupHoldWhileSearching(owner);
                    return;
                }

                owner.m_LastDefenderPos = owner.m_MainDefender.ReadHitPoint();
                LockTarget(owner, owner.m_MainDefender, true);
                if (m_StartupHoldRemaining > 0f)
                {
                    return;
                }

                m_Algorithm.Reset(owner.m_LastDefenderPos, true);
                owner.SetForward(m_Algorithm.BuildLaunchDirection(
                    owner.GetPosition(),
                    owner.GetForward(),
                    owner.m_LastDefenderPos,
                    owner.GetMoveSpeed()));
                m_AlgorithmEnabled = true;
                m_TrackedTargetId = owner.m_MainDefender.ReadId();
            }

            public override void OnReset(BulletObj owner)
            {
                ReleaseTrackingTarget(owner);
                Resetpublic();
            }

            public override void OnTargetInvalid(BulletObj owner)
            {
                ReleaseTrackingTarget(owner);
                ClearPendingLock();
                MarkTargetLost(owner);
            }

            public override bool HandleNoTarget(BulletObj owner, float dt)
            {
                if (owner.m_MainDefender != null && !owner.IsCombatTargetAvailable(owner.m_MainDefender, owner.m_DefenderId))
                {
                    OnTargetInvalid(owner);
                }

                if (owner.m_MainDefender == null && !m_TargetLocked)
                {
                    TryAcquireTarget(owner, dt);
                }

                if (owner.m_MainDefender != null)
                {
                    owner.m_LastDefenderPos = owner.m_MainDefender.ReadHitPoint();
                    owner.SetDefenderPos(owner.m_LastDefenderPos);
                    UpdateTrackingDirection(owner, owner.m_LastDefenderPos, dt, owner.m_DefenderId, false);
                    return false;
                }

                if (m_TargetLost)
                {
                    return MoveToLastTrackedPosition(owner, dt);
                }

                if (UpdateSearchingForward(owner, dt))
                {
                    return false;
                }

                TryUpdatePendingGuidance(owner, dt);
                return false;
            }

            public override bool HandleMissingTarget(BulletObj owner, float dt)
            {
                if (!m_TargetLocked)
                {
                    TryAcquireTarget(owner, dt);
                }

                if (owner.m_MainDefender != null)
                {
                    return false;
                }

                if (m_TargetLost)
                {
                    return MoveToLastTrackedPosition(owner, dt);
                }

                if (UpdateSearchingForward(owner, dt))
                {
                    return false;
                }

                TryUpdatePendingGuidance(owner, dt);
                return false;
            }

            public override void UpdateTargetDirection(BulletObj owner, float dt)
            {
                UpdateTrackingDirection(owner, owner.m_LastDefenderPos, dt, owner.m_DefenderId, true);
            }

            private void Resetpublic()
            {
                m_Algorithm.Stop();
                m_AlgorithmEnabled = false;
                m_TrackedTargetId = 0;
                m_TargetLocked = false;
                m_TargetLost = false;
                m_StartupHoldRemaining = 0f;
                m_SkipWobbleOnFirstActivation = false;
                m_HasLaunchSpreadWindow = false;
                ClearPendingLock();
            }

            private void TryAcquireTarget(BulletObj owner, float dt)
            {
                if (owner.m_TrackingRange <= 0f)
                {
                    ClearPendingLock();
                    return;
                }

                PropertyEntity preferredTarget;
                PropertyEntity guidanceTarget;
                var immediateTarget = owner.FindTrackingTarget(out preferredTarget, out guidanceTarget);
                if (immediateTarget != null)
                {
                    bool useStartupHold = m_PendingTargetId == 0;
                    LockTarget(owner, immediateTarget, useStartupHold);
                    return;
                }

                if (preferredTarget == null)
                {
                    ClearPendingLock();
                    return;
                }

                int preferredTargetId = preferredTarget.ReadId();
                if (m_PendingTargetId != preferredTargetId)
                {
                    m_PendingTargetId = preferredTargetId;
                    m_PendingLockRemaining = BuildDeferredLockTime(owner, preferredTarget);
                }

                m_PendingGuidanceTargetId = guidanceTarget != null ? guidanceTarget.ReadId() : 0;
                if (m_PendingLockRemaining > 0f)
                {
                    m_PendingLockRemaining -= dt;
                }

                if (m_PendingLockRemaining <= 0f)
                {
                    LockTarget(owner, preferredTarget, false);
                }
            }

            private void LockTarget(BulletObj owner, PropertyEntity target, bool useStartupHold)
            {
                if (target == null)
                {
                    return;
                }

                ReleaseTrackingTarget(owner);

                owner.m_MainDefender = target;
                owner.m_DefenderId = target.ReadId();
                owner.m_LastDefenderPos = target.ReadHitPoint();
                owner.RegisterTrackingTargetAssignment(owner.m_DefenderId);
                m_TargetLocked = true;
                m_TargetLost = false;
                ClearPendingLock();

                if (useStartupHold)
                {
                    PrepareStartupHoldForFirstTarget(owner, owner.m_DefenderId);
                    return;
                }

                if (m_HasLaunchSpreadWindow && m_StartupHoldRemaining > 0f)
                {
                    m_SkipWobbleOnFirstActivation = true;
                    return;
                }

                m_StartupHoldRemaining = 0f;
                m_SkipWobbleOnFirstActivation = m_HasLaunchSpreadWindow && !m_AlgorithmEnabled;
            }

            private bool UpdateSearchingForward(BulletObj owner, float dt)
            {
                if (m_TargetLocked || owner.GetTrackingStartupHoldTime() <= 0f)
                {
                    return false;
                }

                if (m_StartupHoldRemaining > 0f)
                {
                    m_StartupHoldRemaining = Mathf.Max(0f, m_StartupHoldRemaining - dt);
                    owner.GetRender().SetTargetPosition(owner.GetPosition() + owner.GetForward() * 10f);
                    if (m_StartupHoldRemaining > 0f)
                    {
                        return true;
                    }
                }

                // 批量跟踪弹在等待重新分配目标期间，继续保持当前发射方向。
                // 这里不能再强制切到固定世界方向，否则在敌人数量不足时会出现明显朝后乱飞。
                return false;
            }

            private void ReleaseTrackingTarget(BulletObj owner)
            {
                owner.ReleaseTrackingTargetAssignment();
            }

            private void MarkTargetLost(BulletObj owner)
            {
                owner.m_MainDefender = null;
                owner.m_DefenderId = 0;
                m_TargetLost = true;
            }

            private void ClearPendingLock()
            {
                m_PendingTargetId = 0;
                m_PendingGuidanceTargetId = 0;
                m_PendingLockRemaining = 0f;
            }

            private bool TryUpdatePendingGuidance(BulletObj owner, float dt)
            {
                int guidanceTargetId = m_PendingGuidanceTargetId != 0 ? m_PendingGuidanceTargetId : m_PendingTargetId;
                if (guidanceTargetId == 0)
                {
                    return false;
                }

                var guidanceTarget = BattleManager.ReadEntity(guidanceTargetId) as PropertyEntity;
                if (!owner.IsCombatTargetAvailable(guidanceTarget, guidanceTargetId))
                {
                    if (guidanceTargetId == m_PendingGuidanceTargetId)
                    {
                        m_PendingGuidanceTargetId = 0;
                    }

                    return false;
                }

                var guidancePos = guidanceTarget.ReadHitPoint();
                owner.SetDefenderPos(guidancePos);
                // 等待分配期间只做平滑转向，不能瞬间对准目标：
                // 单目标时瞬间对准会让整批子弹的散开当面塌缩成一条线。
                var toTarget = guidancePos - owner.GetPosition();
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    var desiredForward = toTarget.normalized;
                    float blend = Mathf.Clamp01(dt * BattleConst.SpawnPendingTurnSpeed);
                    var nextForward = Vector3.Slerp(owner.GetForward(), desiredForward, blend);
                    if (nextForward.sqrMagnitude > 0.0001f)
                    {
                        owner.SetForward(nextForward.normalized);
                    }
                }

                return true;
            }

            private float BuildDeferredLockTime(BulletObj owner, PropertyEntity preferredTarget)
            {
                if (preferredTarget == null)
                {
                    return 0f;
                }

                float moveSpeed = owner.GetMoveSpeed();
                if (moveSpeed <= Mathf.Epsilon)
                {
                    return 0f;
                }

                float estimatedHitTime = Vector3.Distance(owner.GetPosition(), preferredTarget.ReadHitPoint()) / moveSpeed;
                float remainingLife = Mathf.Max(
                    0f,
                    owner.m_BulletBean.t_max_time / 1000.0f - (BattleManager.ReadBattleTime() - owner.m_FlyTime));
                return Mathf.Min(estimatedHitTime * m_DefaultTrackingDelayedLockRatio, remainingLife);
            }

            private bool MoveToLastTrackedPosition(BulletObj owner, float dt)
            {
                var currentPos = owner.GetPosition();
                if (owner.TryResolveTrackedPointArrival(
                        currentPos,
                        currentPos,
                        owner.m_LastDefenderPos,
                        m_DefaultLostTargetArrivalRange,
                        out var arrivalPos))
                {
                    if (owner.m_Size > 0f)
                    {
                        owner.TriggerSizeDamage(arrivalPos);
                    }

                    owner.SetPosition(arrivalPos);
                    owner.GetRender().BulletBoom(arrivalPos, owner.GetForward());
                    owner.PlayHitSound();
                    owner.OnFinish();
                    return true;
                }

                UpdateTrackingDirection(owner, owner.m_LastDefenderPos, dt, m_TrackedTargetId, false);
                var step = dt * owner.GetMoveSpeed();
                var pos = currentPos + owner.GetForward() * step;
                if (owner.TryResolveTrackedPointArrival(
                        currentPos,
                        pos,
                        owner.m_LastDefenderPos,
                        m_DefaultLostTargetArrivalRange,
                        out arrivalPos))
                {
                    if (owner.m_Size > 0f)
                    {
                        owner.TriggerSizeDamage(arrivalPos);
                    }

                    owner.SetPosition(arrivalPos);
                    owner.GetRender().BulletBoom(arrivalPos, owner.GetForward());
                    owner.PlayHitSound();
                    owner.OnFinish();
                    return true;
                }

                if (BattleManager.ReadBattleTime() - owner.m_FlyTime >= owner.m_BulletBean.t_max_time / 1000.0f)
                {
                    if (owner.m_Size > 0f)
                    {
                        owner.TriggerSizeDamage(owner.GetPosition());
                    }

                    owner.GetRender().BulletBoom(owner.GetPosition(), owner.GetForward());
                    owner.PlayHitSound();
                    owner.OnFinish();
                    return true;
                }

                owner.SetPosition(pos);
                owner.GetRender().SetTargetPosition(owner.m_LastDefenderPos);
                return true;
            }

            private void UpdateTrackingDirection(BulletObj owner, Vector3 targetPos, float dt, int targetId, bool startWithWobbleWhenActivating)
            {
                if (ConsumeStartupHold(dt))
                {
                    return;
                }

                if (!m_AlgorithmEnabled)
                {
                    if (m_SkipWobbleOnFirstActivation)
                    {
                        m_Algorithm.Reset(targetPos, false);
                        m_Algorithm.RestartTracking(targetPos);
                    }
                    else
                    {
                        m_Algorithm.Reset(targetPos, startWithWobbleWhenActivating);
                    }
                    m_AlgorithmEnabled = true;
                    m_TrackedTargetId = targetId;
                    m_SkipWobbleOnFirstActivation = false;
                }
                else if (m_TrackedTargetId != targetId)
                {
                    m_Algorithm.RestartTracking(targetPos);
                    m_TrackedTargetId = targetId;
                }

                owner.SetForward(m_Algorithm.UpdateDirection(
                    owner.GetPosition(),
                    owner.GetForward(),
                    targetPos,
                    owner.GetMoveSpeed(),
                    dt));
            }

            private bool ConsumeStartupHold(float dt)
            {
                if (m_StartupHoldRemaining <= 0f)
                {
                    return false;
                }

                m_StartupHoldRemaining -= dt;
                return m_StartupHoldRemaining > 0f;
            }

            private float BuildStartupHoldTime(BulletObj owner)
            {
                if (owner.GetTrackingStartupHoldTime() <= 0f)
                {
                    return 0f;
                }

                BattleConst.ClampValues();

                float minHoldTime = BattleConst.TriggerTrackingStartHoldTimeMin;
                float maxHoldTime = BattleConst.TriggerTrackingStartHoldTimeMax;
                if (maxHoldTime <= minHoldTime)
                {
                    return minHoldTime;
                }

                if (BattleConst.TriggerTrackingStartHoldTimeUseRandom)
                {
                    return Random.Range(minHoldTime, maxHoldTime);
                }

                float moveSpeed = owner.GetMoveSpeed();
                if (moveSpeed <= Mathf.Epsilon)
                {
                    return minHoldTime;
                }

                float targetDistance = Vector3.Distance(owner.GetPosition(), owner.m_LastDefenderPos);
                float nearDistance = moveSpeed * minHoldTime;
                float farDistance = moveSpeed * maxHoldTime;
                if (farDistance <= nearDistance + Mathf.Epsilon)
                {
                    return minHoldTime;
                }

                float distanceRatio = Mathf.InverseLerp(nearDistance, farDistance, targetDistance);
                return Mathf.Lerp(minHoldTime, maxHoldTime, distanceRatio);
            }

            private void PrepareStartupHoldWhileSearching(BulletObj owner)
            {
                if (m_HasLaunchSpreadWindow || owner.GetTrackingStartupHoldTime() <= 0f)
                {
                    return;
                }

                m_StartupHoldRemaining = BuildStartupHoldTime(owner);
                if (m_StartupHoldRemaining <= 0f)
                {
                    return;
                }

                m_SkipWobbleOnFirstActivation = true;
                m_HasLaunchSpreadWindow = true;
            }

            private void PrepareStartupHoldForFirstTarget(BulletObj owner, int targetId)
            {
                if (m_AlgorithmEnabled || m_TrackedTargetId != 0 || m_StartupHoldRemaining > 0f)
                {
                    return;
                }

                if (m_HasLaunchSpreadWindow)
                {
                    m_SkipWobbleOnFirstActivation = true;
                    return;
                }

                m_StartupHoldRemaining = BuildStartupHoldTime(owner);
                if (m_StartupHoldRemaining <= 0f)
                {
                    return;
                }

                m_SkipWobbleOnFirstActivation = true;
                m_TrackedTargetId = targetId;
            }
        }

        protected BulletState m_BulletState = BulletState.Init;
        protected int m_AttackerId;
        protected PropertyEntity m_MainDefender;
        protected int m_DefenderId = 0;
        protected Vector3 m_StartPos;
        protected float m_FlyDist = 0;
        protected float m_DashEnemyDist = 0;
        protected float m_DashEnemyTime = 0;

        // 轨迹类型（0=直线，1=抛物线，2=追踪）
        protected int m_Trajectory = 0;
        // 追踪范围，以发射点为圆心（米）
        protected float m_TrackingRange = 0f;
        // 穿透配置（0=穿透，1=击中即停）
        protected bool m_CanPenetrate = false;
        // 伤害范围（0=单个目标，>0=范围，单位米）
        protected float m_Size = 0f;

        // 击中时为目标添加的Buff
        protected IReadOnlyCollection<long> m_HitTargetBuffIds = null;

        // 触发子弹配置
        protected int m_TriggerBulletId = 0;
        protected int m_TriggerBulletCount = 0;
        protected int m_TriggerType = 0;
        protected float m_TrackingStartupHoldTime = 0f;

        // 触发链已访问的子弹配置 ID 集合，用于检测循环触发（A→B→C→A）
        protected HashSet<int> m_TriggerChainVisitedConfigIds = null;

        public void SetStartPos(Vector3 pos)
        {
            var constrainedPos = BulletFlightConstraintUtility.ResolveConstrainedPosition(m_BulletBean, pos);
            m_StartPos = constrainedPos;
            m_LastDefenderPos = constrainedPos;
        }
        public void SetFlyDist(float dist)
        {
            m_FlyDist = dist;
        }
        public void SetDefender(PropertyEntity defender)
        {
            if(defender != null)
            {
                m_DefenderId = defender.ReadId();
                m_MainDefender = defender;
                m_LastDefenderPos = m_MainDefender.ReadHitPoint();
            }
            else
            {
                m_DefenderId = 0;
                m_MainDefender = null;
            }
        }

        public static void ResetSharedTrackingState()
        {
            s_TrackingTargetAssignmentByBulletId.Clear();
            s_TrackingTargetLoadCounts.Clear();
            s_TrackingTriggerVolleyIndex = 0;
        }

        private static int ReadTrackingTargetAssignedCount(int targetId)
        {
            if (targetId <= 0)
            {
                return 0;
            }

            return s_TrackingTargetLoadCounts.TryGetValue(targetId, out var count) ? count : 0;
        }

        private static int ReadTrackingTargetAssignedCount(Dictionary<int, int> extraAssignedCounts, int targetId)
        {
            if (extraAssignedCounts == null || targetId <= 0)
            {
                return 0;
            }

            return extraAssignedCounts.TryGetValue(targetId, out var count) ? count : 0;
        }

        private static void IncrementTrackingTargetAssignedCount(Dictionary<int, int> assignedCounts, int targetId)
        {
            if (assignedCounts == null || targetId <= 0)
            {
                return;
            }

            assignedCounts.TryGetValue(targetId, out var count);
            assignedCounts[targetId] = count + 1;
        }

        private static void DecrementTrackingTargetAssignedCount(Dictionary<int, int> assignedCounts, int targetId)
        {
            if (assignedCounts == null || targetId <= 0)
            {
                return;
            }

            if (!assignedCounts.TryGetValue(targetId, out var count))
            {
                return;
            }

            count--;
            if (count <= 0)
            {
                assignedCounts.Remove(targetId);
                return;
            }

            assignedCounts[targetId] = count;
        }

        private void RegisterTrackingTargetAssignment(int targetId)
        {
            int bulletId = ReadId();
            if (bulletId <= 0 || targetId <= 0)
            {
                return;
            }

            if (s_TrackingTargetAssignmentByBulletId.TryGetValue(bulletId, out var oldTargetId))
            {
                if (oldTargetId == targetId)
                {
                    return;
                }

                DecrementTrackingTargetAssignedCount(s_TrackingTargetLoadCounts, oldTargetId);
            }

            s_TrackingTargetAssignmentByBulletId[bulletId] = targetId;
            IncrementTrackingTargetAssignedCount(s_TrackingTargetLoadCounts, targetId);
        }

        private void ReleaseTrackingTargetAssignment()
        {
            int bulletId = ReadId();
            if (bulletId <= 0)
            {
                return;
            }

            if (!s_TrackingTargetAssignmentByBulletId.TryGetValue(bulletId, out var targetId))
            {
                return;
            }

            s_TrackingTargetAssignmentByBulletId.Remove(bulletId);
            DecrementTrackingTargetAssignedCount(s_TrackingTargetLoadCounts, targetId);
        }


        protected float m_FlyTime = 0;



        private HurtInfo m_HurtInfo = new HurtInfo();
        protected GroupId m_BulletGroup;
        private HashSet<int> m_DamagedEntityIds = new HashSet<int>();
        private bool m_HasTriggerSpawnPositionOverride;
        private Vector3 m_TriggerSpawnPositionOverride;
        private readonly LinearBulletTrajectory m_LinearTrajectory = new LinearBulletTrajectory();
        private readonly ParabolicBulletTrajectory m_ParabolicTrajectory = new ParabolicBulletTrajectory();
        private readonly TrackingBulletTrajectory m_TrackingTrajectory = new TrackingBulletTrajectory();
        private readonly LaserBulletTrajectory m_LaserTrajectory = new LaserBulletTrajectory();
        private IBulletTrajectory m_TrajectoryController;

        //protected GameObject m_BulletLogic;
        protected float m_FlyPredictTime = 0;
        protected Vector3 m_LastDefenderPos;
        public void SetDefenderPos(Vector3 pos)
        {
            m_LastDefenderPos = pos;
        }
        public override void SetPlay(bool play)
        {
            base.SetPlay(play);
            m_DamagedEntityIds.Clear();
            ClearTriggerSpawnPositionOverride();
            m_TrackingStartupHoldTime = 0f;
            ResetTrajectoryControllers();
            m_BulletState = BulletState.Init;
        }
        public HurtInfo GetHurtInfo()
        {
            return m_HurtInfo;
        }

        public override void CreateRender(UResource obj, ResourceType resourceType)
        {
            var res = UResourceFactory.New_EntityObject(resourceType, m_EntityType);
            res.SetId(ReadId());
            SetResource(res);
            res.LoadRender(m_BulletBean.t_effect_abname, Tool.GetAssetName(m_BulletBean.t_effect_abname));


            //m_BulletLogic = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //m_BulletLogic.transform.localScale = new Vector3(0.05f, 1, 0.05f);
        }

        private int m_HitSound = 0;
        public void SetHitSound(int hitSound)
        {
            m_HitSound = hitSound;
        }
        private t_bullet m_BulletBean = null;


        public override void SetBean(object bean)
        {
            m_BulletBean = (t_bullet)bean;
        }

        public t_bullet GetBulletBean()
        {
            return m_BulletBean;
        }

        public override void Update(float dt)
        {
            switch (m_BulletState)
            {
                case BulletState.Init:
                    {
                        m_BulletState = BulletState.Fly;
                        OnStartFly();
                        break;
                    }
                case BulletState.Fly:
                    {
                        if (m_Trajectory == 3)
                        {
                            UpdateLaserBeam();
                        }
                        else if (m_Trajectory == 4)
                        {
                            UpdateSprayStream();
                        }
                        else
                        {
                            OnFlying(dt);
                        }
                        break;
                    }
                case BulletState.Finish:
                    {

                        break;
                    }
                case BulletState.AfterFinish:
                    {
                        if (m_SprayTailActive)
                        {
                            UpdateSprayTail(dt);
                        }
                        break;
                    }
            }
        }

        // 激光（t_trajectory=3）：持续光束——每帧跟随施法者当前位置与朝向重新收束终点，
        // 整条光束段持续扫射，同一目标由已命中去重保证整条光束只结算一次。
        // 穿透口径走 t_penetrate：穿透=贯穿怪物打满整条线；非穿透=光束止于最近的命中物（怪物或棒棒糖）。
        // 棒棒糖（升级挑战目标）：命中即结算伤害（只一次），之后光束持续架在棒棒糖上，直到超时或被击破。
        // Curve World 口径：激光是直线（材质不走弯曲）；瞄准与命中按单位“完整弯折后的画面形体”计算——
        // 对实体做视觉口径求交（TryIntersectVisualSegment：把逻辑碰撞体端点完整弯折后再与直线射线求交，
        // 与瞄准线/落点特效同一套判定），命中点直接就是画面上的视觉位置，t 即直线射线上的位置。
        // 不能用“逻辑求交 + 命中节点常量偏移”近似——怪物移动、命中节点与碰撞体不同高/不同深时都会失准。
        private const int LaserBeamPointCount = 2;
        private readonly Vector3[] m_LaserStraightPoints = new Vector3[LaserBeamPointCount];
        private readonly Vector3[] m_LaserBentPoints = new Vector3[LaserBeamPointCount];
        private int m_LaserRenderCount = LaserBeamPointCount;

        // 激光（持续光束）的受击特效跟随：普通子弹的受击特效是一次性的（打中一处后留在原地），
        // 但光束会持续接触移动中的怪物，受击点必须一直贴在“最新的激光-怪物接触点”上并跟着移动。
        // 因此激光不走 PlayHitEffect 的一次性播放：每个接触中的目标各持有一个受击特效实例，
        // 每帧同步到本帧算出的接触点；目标离开光束/死亡/光束回收时立即回池，避免残留旧位置的击中点。
        private sealed class LaserContactEffect
        {
            public RenderEff m_Eff;
            public Vector3 m_Position;
            public int m_LastTouchFrame = -1;
        }

        private readonly Dictionary<int, LaserContactEffect> m_LaserContactEffects = new Dictionary<int, LaserContactEffect>();
        private readonly List<int> m_LaserContactStaleIds = new List<int>();

        // 水柱（t_trajectory=4）收尾：停手后先停粒子发射，等已经喷出去的粒子自然消失再回池；
        // 立即回收会把整条水柱截断。
        private bool m_SprayTailActive;
        private float m_SprayTailRemaining;

        private void UpdateLaserBeam()
        {
            var attacker = m_HurtInfo != null ? m_HurtInfo.m_Attacker : null;
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null || !BattleManager.ReadIsEntityValide(attacker))
            {
                // 施法者已无效时光束停在原地，等超时回收
                SyncLaserBeamRender();
                ReleaseAllLaserContactEffects();
                if (HasTimedOut())
                {
                    FinishWithoutHit();
                }
                return;
            }

            var start = BulletFlightConstraintUtility.ResolveConstrainedPosition(
                m_BulletBean,
                attacker.ReadResolvedFirePointCenter());
            // 方向与瞄准线同源：优先取战斗侧缓存的瞄准朝向（驱动角色俯仰姿态的同一份方向，
            // 含默认俯仰与吸附偏移）。开火点节点朝向会被攻击动画改写——瞄准态 spine 保持住
            // 俯仰、施法态被动画关键帧带回水平，直接读节点就表现为瞄准线上仰而激光打平，
            // 因此节点朝向只作兜底；起点仍取开火点，保证光束贴着武器。
            var forward = Vector3.zero;
            battle.TryResolveCurrentAutoAimReferenceForward(attacker, out forward);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = attacker.ReadForward();
            }
            if (forward.sqrMagnitude <= 0.0001f)
            {
                SyncLaserBeamRender();
                ReleaseAllLaserContactEffects();
                if (HasTimedOut())
                {
                    FinishWithoutHit();
                }
                return;
            }

            // 激光：向下最多 5 度（与角色姿态/瞄准线同口径，统一由 ClampLaserForwardPitch 收口）
            forward = TowerDefendAimAssistLine.ClampLaserForwardPitch(forward);
            var direction = forward.normalized;

            // 命中/结算段：与瞄准线的命中判定完全同源——同起点、同方向、同一整段施法距离，
            // 不做地面/关注物边界收束。角色2激光的刻意口径：瞄准线视觉上碰到谁，
            // 伤害和攻击特效就必须在接触位置产生（即使光束因为 5 度收口从怪物上方掠过）。
            var hitEnd = start + direction * Mathf.Max(0.5f, m_FlyDist);
            // 渲染用末端：仍按地面/关注物边界收束，保持原有观感
            var end = TowerDefendAimAssistLine.ResolveLaserBeamEnd(battle, attacker, start, direction, m_FlyDist, true);
            int pointCount = LaserBeamPointCount;
            for (int i = 0; i < pointCount; i++)
            {
                m_LaserStraightPoints[i] = Vector3.Lerp(start, end, i / (float)(pointCount - 1));
                m_LaserBentPoints[i] = m_LaserStraightPoints[i];
            }
            m_StartPos = m_LaserBentPoints[0];

            // 命中判定统一使用与瞄准线同源的整段施法距离（hitEnd），
            // 渲染末端 end（地面/关注物边界收束）只用于光束观感。
            bool hasChallengeHit = TryResolveLaserChallengeHit(
                battle,
                start,
                hitEnd,
                out var challengeT,
                out var challengeVisualPoint,
                out var challengeAnchorPoint);
            bool beamEndedByChallenge = false;
            bool showEndHitEffect = false;
            var clampT = 1f;
            var clampVisualPoint = end;

            if (m_CanPenetrate)
            {
                SweepLaserBeamTargets(start, hitEnd);
                beamEndedByChallenge = hasChallengeHit;
                // 穿透光束的末端在射线远端而非怪物身上，末端同样保留受击特效
                showEndHitEffect = true;
            }
            else
            {
                bool hasMonsterHit = FindLaserFirstMonsterHit(
                    start,
                    hitEnd,
                    out var monsterT,
                    out var monsterAnchor,
                    out var monsterVisualPoint,
                    out var monsterTarget);
                bool clampToChallenge = hasChallengeHit && (!hasMonsterHit || challengeT <= monsterT);
                if (clampToChallenge)
                {
                    clampT = challengeT;
                    clampVisualPoint = challengeVisualPoint;
                    beamEndedByChallenge = true;
                    // 光束架在棒棒糖上：末端保留受击特效
                    showEndHitEffect = true;
                }
                else if (hasMonsterHit)
                {
                    clampT = monsterT;
                    clampVisualPoint = monsterVisualPoint;
                    // 伤害数字与特效同位：都用视觉接触点，画面上正好落在线与模型的接触处
                    HandleHit(monsterTarget, monsterVisualPoint, monsterVisualPoint);
                    // 末端就是怪物接触点，接触特效已经贴在该位置，不再叠加末端特效
                    showEndHitEffect = false;
                }
                else
                {
                    // 没打中目标：末端（地面/边界收束点）保持显示受击特效
                    showEndHitEffect = true;
                }
            }

            // 接触点受击特效：本帧命中分支跑完后统一同步，贴着最新的激光-怪物接触点
            SyncLaserContactEffects();

            // 末端受击特效：光束末端没有贴在怪物接触点上时（没打中/穿透远端/架在棒棒糖上），在末端保持显示
            SyncLaserEndHitEffect(showEndHitEffect, clampVisualPoint);

            pointCount = ClampLaserBeamPoints(clampT, clampVisualPoint);
            SetPosition(m_LaserBentPoints[pointCount - 1]);
            m_LaserRenderCount = pointCount;
            SyncLaserBeamRender();

            if (beamEndedByChallenge)
            {
                var challengeEntity = battle.ReadUpgradeChallengeTarget();
                if (challengeEntity != null)
                {
                    // 光束末端用完整弯折变换后的视觉点（与 line_hit_eff 落点特效在画面上重合）；
                    // 结算仍用逻辑锚点，避免用“视觉点-常量偏移”近似回逻辑点时再度引入 Y 误差。
                    HandleLaserChallengeHit(battle, challengeEntity, challengeAnchorPoint, challengeVisualPoint);
                    if (m_BulletState != BulletState.Fly)
                    {
                        return;
                    }
                }
            }

            if (HasTimedOut())
            {
                FinishWithoutHit();
            }
        }

        // 光束被截断在视觉射线的 clampT（0~1）处。
        // 命中时整条光束的渲染点统一起始于“光束起点”、终止于“视觉命中点”重新插值，
        // 与瞄准线（起点 → 视觉命中点的一条直线）同源；否则中间点仍沿原来的逻辑远端终点方向，
        // 只有末梢一个点被拉到命中点，视觉上光束主轴与瞄准线/落点特效出现偏折。
        private int ClampLaserBeamPoints(float clampT, Vector3 visualPoint)
        {
            int lastIndex = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Clamp01(clampT) * (LaserBeamPointCount - 1)),
                1,
                LaserBeamPointCount - 1);
            var startPoint = m_LaserStraightPoints[0];
            for (int i = 0; i <= lastIndex; i++)
            {
                m_LaserBentPoints[i] = Vector3.Lerp(startPoint, visualPoint, i / (float)lastIndex);
            }
            return lastIndex + 1;
        }

        private bool TryResolveLaserChallengeHit(
            TowerDefendBattle battle,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float rayT,
            out Vector3 visualPoint,
            out Vector3 anchorPoint)
        {
            rayT = float.MaxValue;
            visualPoint = Vector3.zero;
            anchorPoint = Vector3.zero;
            if (battle == null ||
                (!battle.ReadIsUpgradeChallengeCountdown() && !battle.ReadIsUpgradeChallengeActive()))
            {
                return false;
            }

            var targetEntity = battle.ReadUpgradeChallengeTarget();
            if (targetEntity == null)
            {
                return false;
            }

            // 与瞄准线同口径：使用同一整段施法距离求交（迭代收敛），同时取逻辑锚点用于结算/算环值。
            // 不能用“中心点常量偏移”近似——近似交点与真实接触点在 Y 上不一致，会导致特效/光束末端比瞄准线偏高。
            if (!targetEntity.TryIntersectVisualSegmentWithAnchor(segmentStart, segmentEnd, out var t, out var visual, out var anchor))
            {
                return false;
            }

            rayT = Mathf.Clamp01(t);
            visualPoint = visual;
            anchorPoint = anchor;
            return true;
        }

        // 棒棒糖：持续光束命中后不能像普通子弹那样立刻回收，否则一帧就看不到激光。
        // 伤害与普通子弹同口径只结算一次，之后光束持续架在棒棒糖上，直到超时或被击破。
        // hitPoint 为逻辑锚点（结算/受击反馈），visualPoint 为击中点特效位置（完整弯折变换后的视觉点）。
        private void HandleLaserChallengeHit(TowerDefendBattle battle, UpgradeChallengeTarget challengeEntity, Vector3 hitPoint, Vector3 visualPoint)
        {
            var challengeId = challengeEntity.ReadId();
            if (HasAlreadyAffectedEntity(challengeId))
            {
                return;
            }

            MarkAffectedEntity(challengeId);
            int bulletBbtDamage = m_BulletBean != null ? m_BulletBean.t_bbt_damage : 0;
            bool targetDefeated = battle.TryHandleUpgradeChallengeHit(
                m_HurtInfo != null ? m_HurtInfo.m_Attacker : null,
                bulletBbtDamage,
                hitPoint,
                out var shouldResolveChallengeResult) && shouldResolveChallengeResult;
            // 结算/环值仍用逻辑锚点；棒棒糖的受击特效与瞄准线/激光同口径：
            // 放在视觉接触点并关闭 Curve World 弯折（否则二次弯折、Y 方向偏低）
            challengeEntity.NotifyChallengeHit(hitPoint, visualPoint);
            GetRender().BulletBoom(visualPoint, GetRender().GetForward());
            PlayHitSound();
            if (targetDefeated)
            {
                battle.ResolveUpgradeChallengeResult();
                m_BulletState = BulletState.Finish;
                OnFinish();
            }
        }

        private bool FindLaserFirstMonsterHit(
            Vector3 start,
            Vector3 end,
            out float rayT,
            out Vector3 straightAnchor,
            out Vector3 visualPoint,
            out PropertyEntity hitTarget)
        {
            rayT = float.MaxValue;
            straightAnchor = Vector3.zero;
            visualPoint = Vector3.zero;
            hitTarget = null;

            var actors = BattleManager.GetObjectManager().ReadPropertyEntities();
            foreach (var kv in actors)
            {
                var defender = kv;
                if (!IsBeamSweepCandidate(defender))
                {
                    continue;
                }

                // 与瞄准线同口径：直接对完整弯折后的画面形体求交，命中点即视觉位置；
                // 不能用“逻辑求交 + 命中节点常量偏移”近似——怪物移动、命中节点与碰撞体不同高/不同深时都会失准。
                var hasLaserCandidateHit = defender.TryIntersectVisualSegment(start, end, m_DefaultSingleTargetHitRange, out var t, out var point);
                if (!hasLaserCandidateHit)
                {
                    continue;
                }

                var clampedT = Mathf.Clamp01(t);
                if (clampedT < rayT)
                {
                    rayT = clampedT;
                    hitTarget = defender;
                    // 视觉求交返回的就是画面上的命中点：光束末端与落点特效/飘字同位
                    straightAnchor = point;
                    visualPoint = point;
                }
            }

            return hitTarget != null;
        }

        private bool IsBeamSweepCandidate(PropertyEntity defender)
        {
            if (!BattleManager.ReadIsEntityValide(defender))
            {
                return false;
            }
            if (m_HurtInfo != null && defender == m_HurtInfo.m_Attacker)
            {
                return false;
            }
            if (defender.ReadIsInBorn())
            {
                return false;
            }
            if (!defender.ReadCanBeTarget() || !defender.ReadCanBeHurt())
            {
                return false;
            }
            if (defender.ReadHurtGroup() == m_BulletGroup)
            {
                return false;
            }
            return true;
        }

        private void SweepLaserBeamTargets(Vector3 start, Vector3 end)
        {
            var actors = BattleManager.GetObjectManager().ReadPropertyEntities();
            foreach (var kv in actors)
            {
                var defender = kv;
                if (!IsBeamSweepCandidate(defender))
                {
                    continue;
                }

                // 与瞄准线同口径：对完整弯折后的画面形体求交，命中点即视觉位置
                if (!defender.TryIntersectVisualSegment(start, end, m_DefaultSingleTargetHitRange, out var t, out var point))
                {
                    continue;
                }

                // 伤害数字与特效同位：都用视觉接触点，画面上正好落在线与模型的接触处
                var visualPoint = point;
                HandleHit(defender, visualPoint, visualPoint);
            }
        }

        private void SyncLaserBeamRender()
        {
            var render = GetRender();
            if (render != null)
            {
                render.SetBeamPoints(m_LaserBentPoints, Mathf.Max(2, m_LaserRenderCount));
            }
        }

        private void RecordLaserContact(int defenderId, Vector3 contactPoint)
        {
            if (!m_LaserContactEffects.TryGetValue(defenderId, out var contact))
            {
                contact = new LaserContactEffect();
                m_LaserContactEffects.Add(defenderId, contact);
            }

            contact.m_Position = contactPoint;
            contact.m_LastTouchFrame = Time.frameCount;
        }

        // 每帧在激光命中分支之后调用：本帧仍接触的目标把受击特效贴到最新接触点，
        // 已经脱离光束的目标（不再被登记）立即回池，保证击中点永远只出现在当前接触位置。
        private void SyncLaserContactEffects()
        {
            if (m_LaserContactEffects.Count <= 0)
            {
                return;
            }

            int hitEffectId = m_HurtInfo != null && m_HurtInfo.skillDescCfg != null
                ? m_HurtInfo.skillDescCfg.t_hitEff
                : 0;
            var effectManager = RenderEffManager.GetInstance();
            m_LaserContactStaleIds.Clear();
            foreach (var kv in m_LaserContactEffects)
            {
                var contact = kv.Value;
                if (contact.m_LastTouchFrame != Time.frameCount || hitEffectId == 0)
                {
                    m_LaserContactStaleIds.Add(kv.Key);
                    continue;
                }

                if (contact.m_Eff == null)
                {
                    contact.m_Eff = effectManager.CreateRenderEff(hitEffectId);
                    if (contact.m_Eff == null)
                    {
                        continue;
                    }

                    contact.m_Eff.ShowEff(false, contact.m_Position, Vector3.zero, Vector3.one);
                }

                // 角色2 激光受击特效与瞄准线/接触点特效同为表现（直线）口径：
                // 特效放在视觉接触点，材质必须关闭 Curve World 弯折，
                // 否则会被世界再弯一次、Y 方向偏低，和瞄准线对不上。
                CurveWorldVisualUtility.SetBendOff(contact.m_Eff, true);
                contact.m_Eff.SetPosition(contact.m_Position);
                if (!contact.m_Eff.IsActive())
                {
                    contact.m_Eff.SetActive(true);
                }
            }

            for (int i = 0; i < m_LaserContactStaleIds.Count; i++)
            {
                ReleaseLaserContactEffect(m_LaserContactStaleIds[i]);
            }
        }

        private void ReleaseLaserContactEffect(int defenderId)
        {
            if (!m_LaserContactEffects.TryGetValue(defenderId, out var contact))
            {
                return;
            }

            m_LaserContactEffects.Remove(defenderId);
            if (contact.m_Eff != null)
            {
                // 回池前恢复默认弯折，避免直线口径残留在池化实例上
                CurveWorldVisualUtility.SetBendOff(contact.m_Eff, false);
                RenderEffManager.GetInstance().PoolRenderEff(contact.m_Eff);
            }
        }

        private void ReleaseAllLaserContactEffects()
        {
            if (m_LaserContactEffects.Count <= 0)
            {
                return;
            }

            var effectManager = RenderEffManager.GetInstance();
            foreach (var kv in m_LaserContactEffects)
            {
                if (kv.Value.m_Eff != null)
                {
                    CurveWorldVisualUtility.SetBendOff(kv.Value.m_Eff, false);
                    effectManager.PoolRenderEff(kv.Value.m_Eff);
                }
            }

            m_LaserContactEffects.Clear();
            m_LaserContactStaleIds.Clear();
        }

        // 光束末端的受击特效：末端没有贴在怪物接触点上时（没打中/穿透远端/架在棒棒糖上）保持在末端显示，
        // 命中怪物时释放（怪物自身的接触特效已经在末端位置）
        private RenderEff m_LaserEndHitEffect;

        private void SyncLaserEndHitEffect(bool show, Vector3 position)
        {
            int hitEffectId = m_HurtInfo != null && m_HurtInfo.skillDescCfg != null
                ? m_HurtInfo.skillDescCfg.t_hitEff
                : 0;
            if (!show || hitEffectId == 0)
            {
                ReleaseLaserEndHitEffect();
                return;
            }

            var effectManager = RenderEffManager.GetInstance();
            if (m_LaserEndHitEffect == null)
            {
                m_LaserEndHitEffect = effectManager.CreateRenderEff(hitEffectId);
                if (m_LaserEndHitEffect == null)
                {
                    return;
                }

                m_LaserEndHitEffect.ShowEff(false, position, Vector3.zero, Vector3.one);
            }

            // 与接触特效同口径：特效放在直线空间的末端位置，材质必须关闭 Curve World 弯折
            CurveWorldVisualUtility.SetBendOff(m_LaserEndHitEffect, true);
            m_LaserEndHitEffect.SetPosition(position);
            if (!m_LaserEndHitEffect.IsActive())
            {
                m_LaserEndHitEffect.SetActive(true);
            }
        }

        private void ReleaseLaserEndHitEffect()
        {
            if (m_LaserEndHitEffect == null)
            {
                return;
            }

            // 回池前恢复默认弯折，避免直线口径残留在池化实例上
            CurveWorldVisualUtility.SetBendOff(m_LaserEndHitEffect, false);
            RenderEffManager.GetInstance().PoolRenderEff(m_LaserEndHitEffect);
            m_LaserEndHitEffect = null;
        }

        // 水柱（t_trajectory=4）：持续喷射——特效固定在开火点，每帧跟随施法者的瞄准方向；
        // 命中与瞄准线同源，按逻辑空间（未弯折）沿瞄准射线求交，水柱、命中飘字与怪物都在同一套弯曲世界里。
        // 视觉弯曲交给特效自身处理：这里不写 LineRenderer，也不对特效材质关闭 Curve World。
        private void UpdateSprayStream()
        {
            var attacker = m_HurtInfo != null ? m_HurtInfo.m_Attacker : null;
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null || !BattleManager.ReadIsEntityValide(attacker))
            {
                BeginSprayTail();
                return;
            }

            var start = BulletFlightConstraintUtility.ResolveConstrainedPosition(
                m_BulletBean,
                attacker.ReadResolvedFirePointCenter());
            // 方向与瞄准线同源：开火点节点朝向会被攻击动画改写，节点朝向只作兜底
            var forward = Vector3.zero;
            battle.TryResolveCurrentAutoAimReferenceForward(attacker, out forward);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = attacker.ReadForward();
            }
            if (forward.sqrMagnitude <= 0.0001f)
            {
                BeginSprayTail();
                return;
            }

            // 水柱喷射期间瞄准线整线隐藏（见 TowerDefendAimAssistLine.Update），
            // 而手操普攻的上下吸附原本是靠瞄准线每帧求解的；这里补一次同源求解，
            // 否则水柱一开喷就锁死在开喷瞬间的俯仰上，无法像其它角色那样随目标高度吸附。
            // 求解结果会同时写回手操俯仰缓存，角色姿态与水柱方向保持同一口径。
            var sprayNormalSkill = m_HurtInfo != null ? m_HurtInfo.GetSkill() : null;
            if (sprayNormalSkill != null &&
                battle.TryResolveGuardHeroNormalAutoAim(attacker, sprayNormalSkill, forward, out var sprayAutoAimSolution) &&
                sprayAutoAimSolution.m_HasSnapTarget)
            {
                forward = sprayAutoAimSolution.m_ResolvedLaunchForward;
            }

            var direction = forward.normalized;
            // 水柱跟着枪口走：只更新特效根节点的位置和朝向，不把根节点搬到射线末端
            SetPosition(start);
            SetForward(direction);
            m_StartPos = start;

            var hitEnd = start + direction * Mathf.Max(0.5f, m_FlyDist);
            bool hasChallengeHit = TryResolveSprayChallengeHit(
                battle,
                start,
                hitEnd,
                out var challengeT,
                out var challengePoint);

            if (m_CanPenetrate)
            {
                SweepSprayTargets(start, hitEnd);
                if (hasChallengeHit)
                {
                    HandleSprayChallengeHit(battle, challengePoint);
                    if (m_BulletState != BulletState.Fly)
                    {
                        return;
                    }
                }
            }
            else
            {
                bool hasMonsterHit = FindSprayFirstMonsterHit(
                    start,
                    hitEnd,
                    out var monsterT,
                    out var monsterPoint,
                    out var monsterTarget);
                bool clampToChallenge = hasChallengeHit && (!hasMonsterHit || challengeT <= monsterT);
                if (clampToChallenge)
                {
                    HandleSprayChallengeHit(battle, challengePoint);
                    if (m_BulletState != BulletState.Fly)
                    {
                        return;
                    }
                }
                else if (hasMonsterHit)
                {
                    // 非穿透只结算最近的目标；水柱本身不停，持续喷溅由特效碰撞负责
                    HandleHit(monsterTarget, monsterPoint, monsterPoint);
                }
            }

            if (HasTimedOut())
            {
                FinishWithoutHit();
            }
        }

        private bool FindSprayFirstMonsterHit(
            Vector3 start,
            Vector3 end,
            out float rayT,
            out Vector3 hitPoint,
            out PropertyEntity hitTarget)
        {
            rayT = float.MaxValue;
            hitPoint = end;
            hitTarget = null;

            var actors = BattleManager.GetObjectManager().ReadPropertyEntities();
            foreach (var kv in actors)
            {
                var defender = kv;
                if (!IsBeamSweepCandidate(defender))
                {
                    continue;
                }

                if (!defender.TryIntersectSegment(start, end, m_DefaultSingleTargetHitRange, out var t, out var point))
                {
                    continue;
                }

                var clampedT = Mathf.Clamp01(t);
                if (clampedT < rayT)
                {
                    rayT = clampedT;
                    hitTarget = defender;
                    hitPoint = point;
                }
            }

            return hitTarget != null;
        }

        private void SweepSprayTargets(Vector3 start, Vector3 end)
        {
            var actors = BattleManager.GetObjectManager().ReadPropertyEntities();
            foreach (var kv in actors)
            {
                var defender = kv;
                if (!IsBeamSweepCandidate(defender))
                {
                    continue;
                }

                if (!defender.TryIntersectSegment(start, end, m_DefaultSingleTargetHitRange, out var t, out var point))
                {
                    continue;
                }

                HandleHit(defender, point, point);
            }
        }

        private bool TryResolveSprayChallengeHit(
            TowerDefendBattle battle,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float rayT,
            out Vector3 hitPoint)
        {
            rayT = float.MaxValue;
            hitPoint = segmentEnd;
            if (battle == null ||
                (!battle.ReadIsUpgradeChallengeCountdown() && !battle.ReadIsUpgradeChallengeActive()))
            {
                return false;
            }

            var targetEntity = battle.ReadUpgradeChallengeTarget();
            if (targetEntity == null)
            {
                return false;
            }

            if (!targetEntity.TryIntersectSegment(segmentStart, segmentEnd, m_DefaultSingleTargetHitRange, out var t, out var point))
            {
                return false;
            }

            rayT = Mathf.Clamp01(t);
            hitPoint = point;
            return true;
        }

        // 棒棒糖：水柱与棒棒糖都随世界弯曲，命中直接用逻辑接触点，
        // 不播子弹爆炸、不关受击特效的 Curve World（这些是直线激光的表现口径）。
        private void HandleSprayChallengeHit(TowerDefendBattle battle, Vector3 hitPoint)
        {
            if (battle == null)
            {
                return;
            }

            var challengeEntity = battle.ReadUpgradeChallengeTarget();
            if (challengeEntity == null)
            {
                return;
            }

            int challengeId = challengeEntity.ReadId();
            if (HasAlreadyAffectedEntity(challengeId))
            {
                return;
            }

            MarkAffectedEntity(challengeId);
            int bulletBbtDamage = m_BulletBean != null ? m_BulletBean.t_bbt_damage : 0;
            bool targetDefeated = battle.TryHandleUpgradeChallengeHit(
                m_HurtInfo != null ? m_HurtInfo.m_Attacker : null,
                bulletBbtDamage,
                hitPoint,
                out var shouldResolveChallengeResult) && shouldResolveChallengeResult;
            challengeEntity.NotifyChallengeHit(hitPoint);
            PlayHitSound();
            if (targetDefeated)
            {
                battle.ResolveUpgradeChallengeResult();
                BeginSprayTail();
            }
        }

        // 停手/超时：先停粒子发射，保留逻辑对象让已喷出的水滴自然消失，再回池；
        // 立即回收会把 32 米长的水柱整条截断。
        private void BeginSprayTail()
        {
            if (m_BulletState == BulletState.AfterFinish)
            {
                return;
            }

            m_DefenderId = 0;
            m_BulletState = BulletState.AfterFinish;
            float particleLife = 0f;
            var render = GetRender();
            if (render != null)
            {
                particleLife = render.StopParticleEmission();
            }
            m_SprayTailRemaining = Mathf.Max(0.1f, particleLife + 0.05f);
            m_SprayTailActive = true;
        }

        private void UpdateSprayTail(float dt)
        {
            m_SprayTailRemaining -= dt;
            if (m_SprayTailRemaining > 0f)
            {
                return;
            }

            m_SprayTailActive = false;
            BattleManager.GetBattle().GetObjectManager().RemoveEffObject(this, false);
            PoolObj();
        }

        private void OnFinish()
        {
            bool shouldTrigger = m_TriggerBulletId > 0 && m_TriggerBulletCount > 0 && m_TriggerType == 0;
            bool isBlockedByVisited = m_TriggerChainVisitedConfigIds != null && m_TriggerChainVisitedConfigIds.Contains(m_TriggerBulletId);
            if (shouldTrigger)
            {
                if (!isBlockedByVisited)
                {
                    CreateTriggerBullet();
                }
            }

            m_DefenderId = 0;
            m_BulletState = BulletState.AfterFinish;
            BattleManager.GetBattle().GetObjectManager().RemoveEffObject(this, false);
            PoolObj();
        }

        private void CreateTriggerBullet()
        {
            var pool = BattleManager.GetBattle().GetBulletObjPool();
            var objMgr = BattleManager.GetBattle().GetObjectManager();
            // 策划要求父子弹和子子弹的排除列表互相独立。
            // 因此这里不能把父子弹的 m_DamagedEntityIds 传给触发子弹。
            var triggerConfig = t_bullet.GetConfig(m_TriggerBulletId, false);
            if (triggerConfig == null)
            {
                return;
            }

            var assignedTrackingTargets = BuildTriggerTrackingTargets(
                triggerConfig.t_trajectory,
                triggerConfig.t_tracking_range,
                m_TriggerBulletCount,
                out int distinctTargetCount);
            var childVisitedIds = new HashSet<int>();
            if (m_TriggerChainVisitedConfigIds != null)
            {
                childVisitedIds.UnionWith(m_TriggerChainVisitedConfigIds);
            }
            int currentConfigId = m_BulletBean != null ? m_BulletBean.t_id : 0;
            if (currentConfigId > 0)
            {
                childVisitedIds.Add(currentConfigId);
            }
            childVisitedIds.Add(m_TriggerBulletId);
            float yawStep = ResolveTriggerLaunchYawStep(m_TriggerBulletCount, distinctTargetCount);
            float launchYawOffset = BuildTriggerLaunchVolleyYawOffset(triggerConfig.t_trajectory, yawStep);

            for (int i = 0; i < m_TriggerBulletCount; i++)
            {
                var triggerBean = t_bullet.GetConfig(m_TriggerBulletId, false);
                if (triggerBean == null)
                {
                    continue;
                }

                var triggerBullet = (BulletObj)pool.GetEffect(
                    emEntityType.em_EntityType_Bullet,
                    m_TriggerBulletId,
                    triggerBean,
                    ResourceType.Bullet);

                triggerBullet.SetId(objMgr.AssignClientId());
                triggerBullet.SetAutoDel(PlayableEffectDelType.Pool);
                triggerBullet.SetPlay(true);
                triggerBullet.SetVisiable(true);

                var launchForward = BuildTriggerLaunchForward(triggerConfig.t_trajectory, i, m_TriggerBulletCount, yawStep, launchYawOffset);
                triggerBullet.SetDefender(null);
                var triggerSpawnPosition = ResolveTriggerSpawnPosition(triggerBean);
                triggerBullet.SetPosition(triggerSpawnPosition);
                triggerBullet.SetForward(launchForward);
                triggerBullet.SetAttacker(m_AttackerId, m_BulletGroup);
                triggerBullet.SetStartPos(triggerSpawnPosition);
                float flyDist = triggerBean.t_max_time / 1000.0f * triggerBean.t_move_speed / 1000.0f;
                triggerBullet.SetFlyDist(flyDist);

                triggerBullet.SetTriggerChainVisitedConfigIds(childVisitedIds);
                triggerBullet.SetBulletConfig(
                    triggerBean.t_penetrate,
                    triggerBean.t_size,
                    triggerBean.t_trajectory,
                    triggerBean.t_tracking_range,
                    triggerBean.t_bullet_hittarget_buff_id,
                    triggerBean.t_trigger_bullet_id,
                    triggerBean.t_trigger_bullet_count,
                    triggerBean.t_trigger_type
                );

                var hurtInfo = triggerBullet.GetHurtInfo();
                hurtInfo.Reset();
                hurtInfo.CopyFrom(m_HurtInfo);
                ApplyTriggerBulletDamage(triggerBean, hurtInfo);
                hurtInfo.m_NeedTarget = 0;
                PropertyEntity assignedTarget = null;
                if (assignedTrackingTargets != null && i < assignedTrackingTargets.Count)
                {
                    assignedTarget = assignedTrackingTargets[i];
                    if (assignedTarget != null)
                    {
                        triggerBullet.SetDefender(assignedTarget);
                        hurtInfo.m_NeedTarget = 1;
                    }
                }
                if (triggerBean.t_trajectory == 2 && m_TriggerBulletCount > 1)
                {
                    triggerBullet.SetTrackingStartupHoldTime(1f);
                }

                BattleManager.GetBattle().GetObjectManager().AddEffObject(triggerBullet);
            }
        }

        private void ApplyTriggerBulletDamage(t_bullet triggerBean, HurtInfo hurtInfo)
        {
            if (triggerBean == null || hurtInfo == null)
            {
                return;
            }

            hurtInfo.m_RawSkillDamageCfg = triggerBean.t_skill_damage;
            hurtInfo.m_SkillDamagePercent = Mathf.Max(0, hurtInfo.m_RawSkillDamageCfg) / 1000.0f;
            hurtInfo.m_Hurt = Mathf.Max(0, hurtInfo.m_BaseAttack * hurtInfo.m_SkillDamagePercent * hurtInfo.m_DamageAmpScale);
        }

        public virtual void SetAttacker(int attacker, GroupId group)
        {
            m_AttackerId = attacker;
            m_BulletGroup = group;
        }

        public void SetTriggerChainVisitedConfigIds(HashSet<int> visitedIds)
        {
            m_TriggerChainVisitedConfigIds = visitedIds;
        }

        public void SetTrackingStartupHoldTime(float holdTime)
        {
            m_TrackingStartupHoldTime = Mathf.Max(0f, holdTime);
        }

        public float GetTrackingStartupHoldTime()
        {
            return m_TrackingStartupHoldTime;
        }

        public override void SetPosition(Vector3 position)
        {
            GetRender().SetPosition(BulletFlightConstraintUtility.ResolveConstrainedPosition(m_BulletBean, position));

        }
        public override void SetForward(Vector3 forward)
        {
            forward = BulletFlightConstraintUtility.ResolveConstrainedForward(
                m_BulletBean,
                forward,
                GetRender().GetForward());
            if (forward == Vector3.zero)
            {
                return;
            }
            GetRender().SetForward(forward.normalized);

        }

        public override float GetMoveSpeed()
        {
            return m_BulletBean.t_move_speed / 1000.0f;
        }

        private void OnStartFly()
        {
            m_FlyTime = BattleManager.ReadBattleTime();
            m_TrajectoryController = ResolveTrajectoryController();
            m_TrajectoryController.OnStart(this);

            var render = GetRender();
            render.SetAngularSpeed(7200);
            var moveSpeed = GetMoveSpeed();
            render.SetMoveSpeed(moveSpeed);
            render.BulletEmit();
            render.SetInitPosition(GetPosition());

            if(m_HurtInfo.m_NeedTarget == 0)
            {
                var targetPos = GetPosition() + GetForward() * m_BulletBean.t_max_time / 1000.0f * GetMoveSpeed();
                render.SetTargetPosition(targetPos);
                render.SetIsFollow(false);
            }
            else
            {
                render.SetTargetPosition(m_LastDefenderPos);
                render.SetIsFollow(true);
            }

            if (m_Trajectory == 3)
            {
                // 激光：发弹当帧就按当前姿态建立光束（否则要等下一帧 UpdateLaserBeam，
                // 首帧会露出预制件默认控制点或上一发残留的光束）
                UpdateLaserBeam();
            }
            else if (m_Trajectory == 4)
            {
                // 水柱：发弹当帧就把特效摆到开火点并对准瞄准方向，避免首帧在预制件原始位置闪一下
                UpdateSprayStream();
            }
        }

        public void SetDashEnemy(int t_tankai_dist, float t_tan_time)
        {
            m_DashEnemyDist = t_tankai_dist;
            m_DashEnemyTime = t_tan_time;
        }

        public void SetBulletConfig(int penetrate, int size, int trajectory, int trackingRange, IReadOnlyCollection<long> hitTargetBuffIds, int triggerBulletId, int triggerBulletCount, int triggerType)
        {
            m_CanPenetrate = (penetrate == 0);
            m_Size = size / 1000f;
            m_Trajectory = trajectory;
            m_TrackingRange = trackingRange / 1000f;
            m_HitTargetBuffIds = hitTargetBuffIds;
            m_TriggerBulletId = triggerBulletId;
            m_TriggerBulletCount = triggerBulletCount;
            m_TriggerType = triggerType;

            if (m_Trajectory == 3)
            {
                ResetLaserBeamRenderState();
            }
        }

        // 持续光束（激光/水柱）弹道：共用续期、收尾与不播受击特效的口径
        public static bool IsContinuousBeamTrajectory(int trajectory)
        {
            return trajectory == 3 || trajectory == 4;
        }

        // 持续光束普攻续期判断：光束未回池、未销毁且处于待飞/飞行状态，才算仍在作用期内的存活光束。
        // 只看 m_BulletState 不够——外部走 SetFinish 静默回池时 state 会停留在 Fly，需叠加对象池标记。
        public bool ReadIsLaserBeamAlive()
        {
            return IsContinuousBeamTrajectory(m_Trajectory) && !IsPooled && !ReadIsDestroy() &&
                (m_BulletState == BulletState.Init || m_BulletState == BulletState.Fly);
        }

        // 水柱（t_trajectory=4）是否正在喷射（Fly 状态）；
        // 停喷后的收尾散水（AfterFinish）不算，表现层据此恢复瞄准线显示。
        public bool ReadIsSprayStreamFlying()
        {
            return m_Trajectory == 4 && m_BulletState == BulletState.Fly;
        }

        // 单次作用时间内再次触发普攻：不重走发弹流程，只把超时起点推到当前战斗时间并重置已命中去重，
        // 光束表现连续，结算口径与重新发一颗激光完全一致（每次触发对线上目标各结算一次）。
        public void RenewLaserLifeTime()
        {
            m_FlyTime = BattleManager.ReadBattleTime();
            m_DamagedEntityIds.Clear();
        }

        // 持续发弹结束：技能在收尾时主动收束光束，不播受击爆炸表现，直接走回收流程。
        // 发弹窗口就是光束的可见时长，避免出现"光束还在但不再结算伤害"的空窗。
        // 水柱不能直接回收：先停发射，等已经喷出去的水滴自然消失再回池。
        public void FinishLaserBeam()
        {
            if (!ReadIsLaserBeamAlive())
            {
                return;
            }

            if (m_Trajectory == 4)
            {
                BeginSprayTail();
                return;
            }

            m_BulletState = BulletState.Finish;
            OnFinish();
        }

        // 复用/首次发弹时清干净光束渲染状态：LineRenderer 会保留预制件默认控制点或上一发的控制点，
        // 在 UpdateLaserBeam 建立新光束之前就会渲染出来，表现为点普攻时闪一下旧光束/预制件状态。
        private void ResetLaserBeamRenderState()
        {
            m_LaserRenderCount = LaserBeamPointCount;
            for (int i = 0; i < LaserBeamPointCount; i++)
            {
                m_LaserStraightPoints[i] = Vector3.zero;
                m_LaserBentPoints[i] = Vector3.zero;
            }

            var render = GetRender();
            if (render != null)
            {
                // 两点同点 = 零长度直线，不可见；等 UpdateLaserBeam 立即重建
                render.SetBeamPoints(m_LaserBentPoints, 2);
            }
        }



        private List<PropertyEntity> BuildTriggerTrackingTargets(int triggerTrajectory, int triggerTrackingRange, int triggerCount, out int distinctTargetCount)
        {
            distinctTargetCount = 0;
            if (triggerTrajectory != 2 || triggerCount <= 0)
            {
                return null;
            }

            float trackingRange = triggerTrackingRange / 1000f;
            if (trackingRange <= 0f)
            {
                return null;
            }

            var candidates = BuildTrackingTargetCandidates(GetPosition(), trackingRange);
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            distinctTargetCount = candidates.Count;

            var assignedTargets = new List<PropertyEntity>(triggerCount);
            var extraAssignedCounts = new Dictionary<int, int>();
            for (int i = 0; i < triggerCount; i++)
            {
                if (TryPickTriggerTrackingTarget(candidates, extraAssignedCounts, out var assignedTarget))
                {
                    assignedTargets.Add(assignedTarget);
                    IncrementTrackingTargetAssignedCount(extraAssignedCounts, assignedTarget.ReadId());
                    continue;
                }

                assignedTargets.Add(null);
            }

            return assignedTargets;
        }

        private float ResolveTriggerLaunchYawStep(int triggerCount, int distinctTargetCount)
        {
            if (triggerCount <= 1)
            {
                return 0f;
            }

            BattleConst.ClampValues();
            // SpawnYawSpread 是单侧总扇面角，换算成相邻两颗的间隔角，再用最小间隔兜底。
            float yawStep = BattleConst.SpawnYawSpread * 2f / (triggerCount - 1);
            yawStep = Mathf.Max(BattleConst.SpawnMinYawSeparation, yawStep);
            // 目标越少，子弹最终都收拢到同一个点，需要把扇面进一步放大才看得出散开。
            if (distinctTargetCount <= 1)
            {
                yawStep *= BattleConst.SpawnSingleTargetSpreadScale;
            }

            return yawStep;
        }

        private float BuildTriggerLaunchVolleyYawOffset(int triggerTrajectory, float yawStep)
        {
            if (triggerTrajectory != 2 || yawStep <= 0f)
            {
                return 0f;
            }

            BattleConst.ClampValues();
            // 用低差异序列给每一轮一个小于一个间隔角的错位，
            // 让后一轮的子弹插进前一轮的空隙，而不是压在同一条线上。
            float phase = Mathf.Repeat(s_TrackingTriggerVolleyIndex * TrackingTriggerVolleyGoldenRatioConjugate, 1f);
            s_TrackingTriggerVolleyIndex++;
            return (phase - 0.5f) * yawStep * BattleConst.SpawnVolleyYawOffsetRatio;
        }

        private Vector3 BuildTriggerLaunchForward(int triggerTrajectory, int triggerIndex, int triggerCount, float yawStep, float yawOffset)
        {
            var baseForward = GetForward();
            if (triggerTrajectory != 2 || triggerCount <= 1 || baseForward == Vector3.zero)
            {
                return baseForward;
            }

            BattleConst.ClampValues();
            float normalizedIndex = triggerCount > 1 ? (triggerIndex / (float)(triggerCount - 1)) * 2f - 1f : 0f;
            float yaw = (triggerIndex - (triggerCount - 1) * 0.5f) * yawStep
                + yawOffset
                + Random.Range(-BattleConst.SpawnYawJitter, BattleConst.SpawnYawJitter);
            float pitch = Mathf.Lerp(
                    BattleConst.SpawnUpwardSpreadMax,
                    BattleConst.SpawnUpwardSpreadMin,
                    Mathf.Abs(normalizedIndex))
                + Random.Range(0f, BattleConst.SpawnUpwardJitter);
            Vector3 yawForward = (Quaternion.Euler(0f, yaw, 0f) * baseForward).normalized;
            Vector3 pitchAxis = Vector3.Cross(yawForward, Vector3.up);
            if (pitchAxis.sqrMagnitude <= 0.0001f)
            {
                pitchAxis = Vector3.left;
            }

            return (Quaternion.AngleAxis(pitch, pitchAxis.normalized) * yawForward).normalized;
        }

        private bool TryPickTriggerTrackingTarget(
            List<TrackingTargetCandidate> candidates,
            Dictionary<int, int> extraAssignedCounts,
            out PropertyEntity assignedTarget)
        {
            assignedTarget = null;
            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            TrackingTargetCandidate bestCandidate = default;
            bool hasBestCandidate = false;
            int candidateCount = candidates.Count;
            for (int i = 0; i < candidateCount; i++)
            {
                var candidate = candidates[i];
                int assignedCount = candidate.m_AssignedBulletCount + ReadTrackingTargetAssignedCount(extraAssignedCounts, candidate.m_TargetId);
                if (assignedCount >= m_DefaultTrackingSoftTargetLimit)
                {
                    continue;
                }

                if (!hasBestCandidate ||
                    assignedCount < bestCandidate.m_AssignedBulletCount ||
                    (assignedCount == bestCandidate.m_AssignedBulletCount && candidate.m_SqrDistance < bestCandidate.m_SqrDistance))
                {
                    candidate.m_AssignedBulletCount = assignedCount;
                    bestCandidate = candidate;
                    hasBestCandidate = true;
                }
            }

            if (!hasBestCandidate)
            {
                return false;
            }

            assignedTarget = bestCandidate.m_Target;
            return assignedTarget != null;
        }

        private List<TrackingTargetCandidate> BuildTrackingTargetCandidates(
            Vector3 searchCenter,
            float trackingRange)
        {
            var objMgr = BattleManager.GetObjectManager();
            if (objMgr == null)
            {
                return null;
            }

            var actors = objMgr.ReadPropertyEntities();
            if (actors == null)
            {
                return null;
            }

            float maxSqrDist = trackingRange * trackingRange;
            var candidates = new List<TrackingTargetCandidate>();
            int actorCount = actors.Count;
            for (int i = 0; i < actorCount; i++)
            {
                var enemy = actors[i];
                if (!TryBuildTrackingTargetCandidate(enemy, searchCenter, maxSqrDist, out var candidate))
                {
                    continue;
                }

                candidates.Add(candidate);
            }

            return candidates;
        }

        private bool TryBuildTrackingTargetCandidate(
            PropertyEntity enemy,
            Vector3 searchCenter,
            float maxSqrDist,
            out TrackingTargetCandidate candidate)
        {
            candidate = default;
            if (!BattleManager.ReadIsEntityValide(enemy))
            {
                return false;
            }
            if (enemy == m_HurtInfo.m_Attacker)
            {
                return false;
            }
            if (enemy.ReadHurtGroup() == m_BulletGroup)
            {
                return false;
            }
            if (enemy.ReadIsInBorn())
            {
                return false;
            }
            if (!enemy.ReadCanBeTarget() || !enemy.ReadCanBeHurt())
            {
                return false;
            }

            int enemyId = enemy.ReadId();
            if (HasAlreadyAffectedEntity(enemyId))
            {
                return false;
            }

            Vector3 targetPos = enemy.ReadHitPoint();
            float sqrDist = (targetPos - searchCenter).sqrMagnitude;
            if (sqrDist > maxSqrDist)
            {
                return false;
            }

            candidate.m_Target = enemy;
            candidate.m_TargetId = enemyId;
            candidate.m_TargetPos = targetPos;
            candidate.m_SqrDistance = sqrDist;
            candidate.m_AssignedBulletCount = ReadTrackingTargetAssignedCount(enemyId);
            return true;
        }

        private IBulletTrajectory ResolveTrajectoryController()
        {
            switch (m_Trajectory)
            {
                case 1:
                    return m_ParabolicTrajectory;
                case 2:
                    return m_TrackingTrajectory;
                case 3:
                case 4:
                    return m_LaserTrajectory;
                default:
                    return m_LinearTrajectory;
            }
        }

        private void ResetTrajectoryControllers()
        {
            m_LinearTrajectory.OnReset(this);
            m_ParabolicTrajectory.OnReset(this);
            m_TrackingTrajectory.OnReset(this);
            m_LaserTrajectory.OnReset(this);
            m_TrajectoryController = null;
        }

        private bool HasTimedOut()
        {
            return BattleManager.ReadBattleTime() - m_FlyTime >= m_BulletBean.t_max_time / 1000.0f;
        }

        // 子弹靠近目标点时，不能只看本帧结束后的剩余距离。
        // 这里按“当前点到下一帧点”的整段位移判断，避免子弹从目标点旁边掠过后再回头绕圈。
        private bool TryResolveTrackedPointArrival(
            Vector3 currentPos,
            Vector3 nextPos,
            Vector3 targetPos,
            float arrivalRange,
            out Vector3 arrivalPos)
        {
            arrivalPos = nextPos;
            float clampedRange = Mathf.Max(0.0f, arrivalRange);
            float arrivalRangeSqr = clampedRange * clampedRange;
            if ((targetPos - currentPos).sqrMagnitude <= arrivalRangeSqr ||
                (targetPos - nextPos).sqrMagnitude <= arrivalRangeSqr ||
                DistancePointSegmentSqr(targetPos, currentPos, nextPos) <= arrivalRangeSqr)
            {
                arrivalPos = targetPos;
                return true;
            }

            return false;
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

        private void FinishWithoutHit()
        {
            // 水柱没有一次性爆炸表现，只停发射并等残余粒子消失
            if (m_Trajectory == 4)
            {
                BeginSprayTail();
                return;
            }

            m_BulletState = BulletState.Finish;
            GetRender().BulletBoom(GetPosition(), Vector3.forward);
            OnFinish();
        }

        private void AimAt(Vector3 targetPos)
        {
            var direction = targetPos - GetPosition();
            if (direction.sqrMagnitude > 0.0001f)
            {
                SetForward(direction.normalized);
            }
        }

        private void AdvanceNoTargetFlight(Vector3 pos)
        {
            var dist = Vector3.Distance(pos, m_StartPos);
            var elapsed = BattleManager.ReadBattleTime() - m_FlyTime;
            var maxLifetime = m_BulletBean.t_max_time / 1000.0f;
            bool isOutOfFlyDist = dist > m_FlyDist;
            bool isTimedOut = elapsed >= maxLifetime;
            if (isOutOfFlyDist || isTimedOut)
            {
                FinishWithoutHit();
                return;
            }

            SetPosition(pos);
        }

        private void AdvanceSearchingTargetFlight(Vector3 pos)
        {
            var elapsed = BattleManager.ReadBattleTime() - m_FlyTime;
            var maxLifetime = m_BulletBean.t_max_time / 1000.0f;
            if (elapsed >= maxLifetime)
            {
                FinishWithoutHit();
                return;
            }

            SetPosition(pos);
            GetRender().SetTargetPosition(pos + GetForward() * 10f);
        }

        private void AdvanceTargetFlight(Vector3 pos)
        {
            if (m_Size > 0f)
            {
                TriggerSizeDamage(pos);
            }

            var elapsed = BattleManager.ReadBattleTime() - m_FlyTime;
            var maxLifetime = m_BulletBean.t_max_time / 1000.0f;
            if (elapsed >= maxLifetime)
            {
                FinishWithoutHit();
                return;
            }

            SetPosition(pos);
            GetRender().SetTargetPosition(pos + GetForward() * 10f);
        }

        private bool TryHandleCurrentTargetHit(Vector3 nextPos)
        {
            if (m_MainDefender == null)
            {
                return false;
            }

            if (!IsCombatTargetAvailable(m_MainDefender, m_DefenderId))
            {
                m_TrajectoryController.OnTargetInvalid(this);
                return false;
            }

            var hitTarget = m_MainDefender;
            var currentPos = GetPosition();
            Vector3 hitPoint;
            if (hitTarget.TryIntersectSegment(currentPos, nextPos, m_DefaultSingleTargetHitRange, out var hitT, out hitPoint))
            {
                return FinishCurrentTargetHit(hitTarget, hitPoint);
            }

            if (TryResolveTrackedPointArrival(
                    currentPos,
                    nextPos,
                    m_LastDefenderPos,
                    m_DefaultTrackedPointArrivalRange,
                    out hitPoint))
            {
                return FinishCurrentTargetHit(hitTarget, hitPoint);
            }

            return false;
        }

        private bool FinishCurrentTargetHit(PropertyEntity hitTarget, Vector3 hitPoint)
        {
            SetPosition(hitPoint);
            var visualHitPosition = HandleHit(hitTarget, hitPoint);
            if (!m_CanPenetrate || m_Trajectory != 0)
            {
                GetRender().BulletBoom(visualHitPosition, GetRender().GetForward());
            }
            if (m_Size > 0f)
            {
                TriggerSizeDamage(hitPoint);
            }

            if (!m_CanPenetrate)
            {
                CacheTriggerSpawnPositionOverride(hitTarget);
                m_BulletState = BulletState.Finish;
                OnFinish();
                return true;
            }

            m_MainDefender = null;
            m_DefenderId = 0;
            return true;
        }

        private bool IsCombatTargetAvailable(PropertyEntity target, int targetId)
        {
            return target != null &&
                   BattleManager.ReadIsEntityValide(target, targetId) &&
                   target.ReadCanBeTarget() &&
                   target.ReadCanBeHurt();
        }

        private bool HasAlreadyAffectedEntity(int entityId)
        {
            if (m_DamagedEntityIds.Contains(entityId))
            {
                return true;
            }
            return false;
        }

        private void MarkAffectedEntity(int entityId)
        {
            m_DamagedEntityIds.Add(entityId);
        }

        private void ClearTriggerSpawnPositionOverride()
        {
            m_HasTriggerSpawnPositionOverride = false;
            m_TriggerSpawnPositionOverride = Vector3.zero;
        }

        private void CacheTriggerSpawnPositionOverride(PropertyEntity hitTarget)
        {
            if (hitTarget == null || m_TriggerBulletId <= 0)
            {
                ClearTriggerSpawnPositionOverride();
                return;
            }

            var triggerBean = t_bullet.GetConfig(m_TriggerBulletId, false);
            if (!ShouldUseHitPointAsTriggerSpawn(triggerBean))
            {
                ClearTriggerSpawnPositionOverride();
                return;
            }

            // 原地范围子弹没有自己的飞行过程，范围中心应该落在被命中目标的受击点。
            // 否则父子弹提前碰到怪物碰撞体前沿时，范围中心会偏到目标前方，导致只擦到一个怪。
            m_TriggerSpawnPositionOverride = hitTarget.ReadHitPoint();
            m_HasTriggerSpawnPositionOverride = true;
        }

        private Vector3 ResolveTriggerSpawnPosition(t_bullet triggerBean)
        {
            if (!m_HasTriggerSpawnPositionOverride || !ShouldUseHitPointAsTriggerSpawn(triggerBean))
            {
                return GetPosition();
            }

            return m_TriggerSpawnPositionOverride;
        }

        private static bool ShouldUseHitPointAsTriggerSpawn(t_bullet triggerBean)
        {
            return triggerBean != null &&
                   triggerBean.t_move_speed == 0 &&
                   triggerBean.t_size > 0;
        }

        protected virtual void OnFlyingNoTarget(float dt)
        {
            var currentPos = GetPosition();
            if (m_TrajectoryController.HandleNoTarget(this, dt))
            {
                return;
            }

            var nextPos = m_TrajectoryController.CalculateNextPosition(this, dt);
            if (TryHandleStationaryAreaDamage(nextPos))
            {
                return;
            }

            bool hasAreaDamage = m_Size > 0f;
            float hitRange = hasAreaDamage ? m_Size : m_DefaultSingleTargetHitRange;
            PropertyEntity firstHitTarget = null;
            Vector3 firstHitPosition = Vector3.zero;
            float firstHitT = float.PositiveInfinity;
            var actors = BattleManager.GetObjectManager().ReadPropertyEntities();
            foreach (var defender in actors)
            {
                if (!BattleManager.ReadIsEntityValide(defender) || defender.ReadIsInBorn() ||
                    !defender.ReadCanBeTarget() || !defender.ReadCanBeHurt() ||
                    defender.ReadHurtGroup() == m_BulletGroup ||
                    !defender.TryIntersectSegment(currentPos, nextPos, hitRange, out float hitT, out var hitPoint))
                {
                    continue;
                }

                if (m_CanPenetrate)
                {
                    HandleHit(defender, hitPoint);
                    continue;
                }

                if (hitT < firstHitT)
                {
                    firstHitTarget = defender;
                    firstHitPosition = hitPoint;
                    firstHitT = hitT;
                    if (firstHitT <= 0f)
                    {
                        break;
                    }
                }
            }

            if (firstHitTarget == null)
            {
                AdvanceNoTargetFlight(nextPos);
                return;
            }

            SetPosition(firstHitPosition);
            var visualHitPosition = HandleHit(firstHitTarget, firstHitPosition);
            if (hasAreaDamage)
            {
                TriggerSizeDamage(firstHitPosition);
            }

            CacheTriggerSpawnPositionOverride(firstHitTarget);
            m_BulletState = BulletState.Finish;
            GetRender().BulletBoom(visualHitPosition, GetRender().GetForward());
            OnFinish();
        }

        private bool TryHandleStationaryAreaDamage(Vector3 nextPos)
        {
            if (m_Size <= 0f || GetMoveSpeed() > 0f)
            {
                return false;
            }

            // t_move_speed 为 0 的 t_size 子弹没有扫掠路径，必须按当前位置做范围结算。
            // 否则线段退化为一个点后会受高度和单个碰撞体影响，无法稳定覆盖范围内全部单位。
            var damageCenter = GetPosition();
            var hitCount = TriggerSizeDamage(damageCenter);
            if (hitCount <= 0 || m_CanPenetrate)
            {
                AdvanceNoTargetFlight(nextPos);
                return true;
            }

            SetPosition(damageCenter);
            m_BulletState = BulletState.Finish;
            GetRender().BulletBoom(damageCenter, GetRender().GetForward());
            OnFinish();
            return true;
        }

        private Vector3 HandleHit(PropertyEntity defender, Vector3 damageNumberWorldPos)
        {
            return HandleHit(defender, damageNumberWorldPos, defender.ReadHitPoint());
        }

        // hitEffectPos：击中点特效的世界位置。普通子弹默认取怪物命中节点；
        // 激光传视觉接触点（逻辑交点做完整弯折变换），否则特效会浮在弯折后单位的上方
        private Vector3 HandleHit(PropertyEntity defender, Vector3 damageNumberWorldPos, Vector3 hitEffectPos)
        {
            int defenderId = defender.ReadId();
            // 激光（持续光束）：受击特效必须逐帧跟着最新的接触点，这里只登记本帧接触，
            // 由 SyncLaserContactEffects 统一创建/移动/回收；普通子弹仍走下面的一次性受击特效。
            if (m_Trajectory == 3)
            {
                RecordLaserContact(defenderId, hitEffectPos);
            }

            if (HasAlreadyAffectedEntity(defenderId))
            {
                return damageNumberWorldPos;
            }
            MarkAffectedEntity(defenderId);

            BattleManager.GetBattleTool().AddDefenderBuff(m_HurtInfo, defender);

            if (m_HitTargetBuffIds != null && m_HitTargetBuffIds.Count > 0)
            {
                BattleManager.GetBattleTool().AddBuffs(m_HurtInfo.m_Attacker, defender, m_HitTargetBuffIds, m_HurtInfo.m_RawSkillDamageCfg);
            }

            m_HurtInfo.SetDamageNumberWorldPos(damageNumberWorldPos);
            if (m_BulletBean != null && m_BulletBean.t_delay_damage > 0)
            {
                // 策划需求：命中后延迟 t_delay_damage 毫秒再结算伤害。子弹当帧就会回池复用，
                // 伤害参数整份快照交给延迟队列，受击表现仍然当场播。
                DelayedDamageManager.Schedule(m_HurtInfo, defender, m_BulletBean.t_delay_damage);
            }
            else
            {
                DamageCal.Cal(m_HurtInfo, defender);
            }
            var attackedTarget = defender as MoveableCreature;
            if (attackedTarget != null)
            {
                attackedTarget.OnAttacked();
            }
            DamageCal.CalSickHP(m_HurtInfo.m_Attacker, m_HurtInfo.m_SickHP);
            m_HurtInfo.m_SickHP = 0;
            var visualHitPosition = damageNumberWorldPos;
            // 持续光束（激光/水柱）的命中表现由光束或喷溅特效自己承担，不再额外播一次性受击特效
            if (!IsContinuousBeamTrajectory(m_Trajectory))
            {
                if (TrySpawnHitStickEffect(defender, damageNumberWorldPos, out visualHitPosition))
                {
                    hitEffectPos = visualHitPosition;
                }
                bool isStraightPiercing = m_CanPenetrate && m_Trajectory == 0;
                PlayHitEffect(isStraightPiercing ? visualHitPosition : hitEffectPos);
                if (isStraightPiercing && GetRender() is UBullet bulletRender)
                {
                    bulletRender.PlayImpact(visualHitPosition, GetForward());
                }
            }
            PlayHitSound();

            if (m_Trajectory == 2 && defender == m_MainDefender)
            {
                ReleaseTrackingTargetAssignment();
            }

            if (m_Trajectory == 2 && m_CanPenetrate && defender == m_MainDefender)
            {
                m_MainDefender = null;
                m_DefenderId = 0;
            }

            return visualHitPosition;
        }

        protected virtual void OnFlyTarget(float dt)
        {
            if (m_MainDefender != null && !IsCombatTargetAvailable(m_MainDefender, m_DefenderId))
            {
                m_TrajectoryController.OnTargetInvalid(this);
            }

            if (m_MainDefender == null)
            {
                if (m_TrajectoryController.HandleMissingTarget(this, dt))
                {
                    return;
                }

                AdvanceSearchingTargetFlight(m_TrajectoryController.CalculateNextPosition(this, dt));
                return;
            }

            m_LastDefenderPos = m_MainDefender.ReadHitPoint();
            SetDefenderPos(m_LastDefenderPos);
            GetRender().SetTargetPosition(m_LastDefenderPos);
            m_TrajectoryController.UpdateTargetDirection(this, dt);
            var nextPos = m_TrajectoryController.CalculateNextPosition(this, dt);
            if (TryHandleCurrentTargetHit(nextPos))
            {
                return;
            }
            AdvanceTargetFlight(nextPos);
        }

        private int TriggerSizeDamage(Vector3 center)
        {
            var actors = BattleManager.GetObjectManager().ReadPropertyEntities();
            int hitCount = 0;
            foreach (var kv in actors)
            {
                var enemy = kv;
                if (!BattleManager.ReadIsEntityValide(enemy))
                {
                    continue;
                }

                int enemyId = enemy.ReadId();
                var enemyHitPoint = enemy.ReadHitPoint();
                var enemyRadius = enemy.ReadRadius();
                float dist;
                {
                    var centerFlat = new Vector3(center.x, 0, center.z);
                    var targetPos = enemyHitPoint;
                    targetPos.y = 0;
                    dist = Vector3.Distance(centerFlat, targetPos);
                }

                float hitThreshold = m_Size + enemyRadius;
                if (enemy == m_HurtInfo.m_Attacker)
                {
                    continue;
                }
                if (enemy.ReadHurtGroup() == m_BulletGroup)
                {
                    continue;
                }
                if (enemy.ReadIsInBorn())
                {
                    continue;
                }
                if (!enemy.ReadCanBeTarget() || !enemy.ReadCanBeHurt())
                {
                    continue;
                }

                if (dist <= hitThreshold)
                {
                    if (HasAlreadyAffectedEntity(enemyId))
                    {
                        continue;
                    }
                    MarkAffectedEntity(enemyId);

                    BattleManager.GetBattleTool().AddDefenderBuff(m_HurtInfo, enemy);

                    if (m_HitTargetBuffIds != null && m_HitTargetBuffIds.Count > 0)
                    {
                        BattleManager.GetBattleTool().AddBuffs(m_HurtInfo.m_Attacker, enemy, m_HitTargetBuffIds, m_HurtInfo.m_RawSkillDamageCfg);
                    }

                    m_HurtInfo.SetDamageNumberWorldPos(enemyHitPoint);
                    if (m_BulletBean != null && m_BulletBean.t_delay_damage > 0)
                    {
                        // 同一颗子弹的范围伤害也遵循命中延迟结算
                        DelayedDamageManager.Schedule(m_HurtInfo, enemy, m_BulletBean.t_delay_damage);
                    }
                    else
                    {
                        DamageCal.Cal(m_HurtInfo, enemy);
                    }
                    var target = enemy as MoveableCreature;
                    if (target != null)
                    {
                        target.OnAttacked();
                    }
                    DamageCal.CalSickHP(m_HurtInfo.m_Attacker, m_HurtInfo.m_SickHP);
                    m_HurtInfo.m_SickHP = 0;
                    hitCount++;
                }
            }

            return hitCount;
        }

        protected virtual void OnFlying(float dt)
        {
            var prePos = GetPosition();
            if (m_HurtInfo.m_NeedTarget == 0 )
            {
                OnFlyingNoTarget(dt);
            }
            else
            {
                OnFlyTarget(dt);
            }

            TryHandleUpgradeChallengeHit(prePos, GetPosition());
        }

        private void TryHandleUpgradeChallengeHit(Vector3 prePos, Vector3 postPos)
        {
            if (m_BulletState == BulletState.Init ||
                m_BulletState == BulletState.Finish ||
                m_BulletState == BulletState.AfterFinish)
            {
                return;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null ||
                (!battle.ReadIsUpgradeChallengeCountdown() && !battle.ReadIsUpgradeChallengeActive()))
            {
                return;
            }

            var targetEntity = battle.ReadUpgradeChallengeTarget();
            if (targetEntity == null ||
                !targetEntity.TryIntersectSegment(prePos, postPos, out var hitT, out var hitPoint))
            {
                return;
            }

            int bulletBbtDamage = m_BulletBean != null ? m_BulletBean.t_bbt_damage : 0;
            bool targetDefeated = battle.TryHandleUpgradeChallengeHit(
                m_HurtInfo != null ? m_HurtInfo.m_Attacker : null,
                bulletBbtDamage,
                hitPoint,
                out var shouldResolveChallengeResult) && shouldResolveChallengeResult;
            targetEntity.NotifyChallengeHit(hitPoint);
            if (targetDefeated)
            {
                battle.ResolveUpgradeChallengeResult();
            }
            m_BulletState = BulletState.Finish;
            SetPosition(hitPoint);
            GetRender().BulletBoom(hitPoint, GetRender().GetForward());
            PlayHitSound();
            OnFinish();
        }

        private PropertyEntity FindTrackingTarget(out PropertyEntity preferredTarget, out PropertyEntity guidanceTarget)
        {
            preferredTarget = null;
            guidanceTarget = null;
            if (m_TrackingRange <= 0f)
            {
                return null;
            }

            var candidates = BuildTrackingTargetCandidates(GetPosition(), m_TrackingRange);
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            TrackingTargetCandidate bestImmediateCandidate = default;
            bool hasBestImmediateCandidate = false;
            float preferredSqrDist = float.MaxValue;
            float guidanceSqrDist = float.MaxValue;
            int candidateCount = candidates.Count;
            for (int i = 0; i < candidateCount; i++)
            {
                var candidate = candidates[i];
                if (candidate.m_SqrDistance < preferredSqrDist)
                {
                    preferredSqrDist = candidate.m_SqrDistance;
                    preferredTarget = candidate.m_Target;
                }

                if (candidate.m_AssignedBulletCount > 0 && candidate.m_SqrDistance < guidanceSqrDist)
                {
                    guidanceSqrDist = candidate.m_SqrDistance;
                    guidanceTarget = candidate.m_Target;
                }

                if (candidate.m_AssignedBulletCount >= m_DefaultTrackingSoftTargetLimit)
                {
                    continue;
                }

                if (!hasBestImmediateCandidate ||
                    candidate.m_AssignedBulletCount < bestImmediateCandidate.m_AssignedBulletCount ||
                    (candidate.m_AssignedBulletCount == bestImmediateCandidate.m_AssignedBulletCount &&
                     candidate.m_SqrDistance < bestImmediateCandidate.m_SqrDistance))
                {
                    bestImmediateCandidate = candidate;
                    hasBestImmediateCandidate = true;
                }
            }

            if (guidanceTarget == null)
            {
                guidanceTarget = preferredTarget;
            }

            if (!hasBestImmediateCandidate)
            {
                return null;
            }

            return bestImmediateCandidate.m_Target;
        }

        private void PlayHitSound()
        {
            if (m_BulletBean.t_hit_sound != 0)
            {
                AudioManager.GetInstance().Play3D(m_BulletBean.t_hit_sound, GetPosition());
            }
            else if (m_HitSound != 0)
            {
                AudioManager.GetInstance().Play3D(m_HitSound, GetPosition());
            }
        }

        private void PlayHitEffect(Vector3 pos)
        {
            var skillDescCfg = m_HurtInfo != null ? m_HurtInfo.skillDescCfg : null;
            if (skillDescCfg != null && skillDescCfg.t_hitEff != 0)
            {
                var eff = RenderEffManager.GetInstance().CreateRenderEff(skillDescCfg.t_hitEff);
                eff.ShowEff(false, pos, Vector3.zero, Vector3.one);
                eff.SetDuringTime(2.0f);
                RenderEffManager.GetInstance().SetAutoPool(eff);
            }
        }

        private bool TrySpawnHitStickEffect(PropertyEntity defender, Vector3 impactPosition, out Vector3 visualHitPosition)
        {
            visualHitPosition = impactPosition;
            if (m_BulletBean == null || m_BulletBean.t_hit_stick_eff == 0)
            {
                return false;
            }

            var targetRender = defender.GetRender();
            var projectileRender = GetRender();
            if (targetRender == null || !targetRender.TryAddHitStickEffect(m_BulletBean.t_hit_stick_eff, impactPosition,
                    Quaternion.Euler(projectileRender.GetEulerAngles()), out visualHitPosition))
            {
                return false;
            }

            if (!m_CanPenetrate && projectileRender is UBullet bulletRender)
            {
                bulletRender.HideProjectileForHitStick();
            }

            return true;
        }

        // 子弹命中后逻辑上已回池，但命中特效还要按 m_HitShowTime 继续显示。
        // 这里上报剩余显示时间，由对象池把它换算成延后的可复用时间。
        public override float ReadVisualRemainTime()
        {
            var render = GetRender();
            return render != null ? render.ReadVisualRemainTime() : 0f;
        }

        public override void PoolObj()
        {
            m_DamagedEntityIds.Clear();
            m_SprayTailActive = false;
            m_SprayTailRemaining = 0f;
            ReleaseAllLaserContactEffects();
            ReleaseLaserEndHitEffect();
            ClearTriggerSpawnPositionOverride();
            m_TriggerChainVisitedConfigIds = null;
            ResetTrajectoryControllers();
            SetVisiable(false);
            BattleManager.GetBattle().GetBulletObjPool().PoolEffect(m_BulletBean.t_id, this);
        }

        private void DrawSizeGizmo()
        {
            if (m_Size <= 0f)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetPosition(), m_Size);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(GetPosition(), GetPosition() + GetForward() * m_Size);
        }

        public override void OnDrawGizmos()
        {
            DrawSizeGizmo();
        }
    }
    internal static class BulletFlightConstraintUtility
    {
        // t_trigger_Y 不为 0 时，子弹全程锁定在同一个世界高度。
        // 配置仍按表的毫米单位读取，1000 表示 1 米。
        public static Vector3 ResolveConstrainedPosition(t_bullet bulletBean, Vector3 position)
        {
            if (!HasConstrainedWorldY(bulletBean))
            {
                return position;
            }

            position.y = bulletBean.t_trigger_Y / 1000.0f;
            return position;
        }

        // t_trigger_Y 生效时，子弹只允许绕世界 Y 轴转向。
        // 这样可以保留水平朝向，但不会再被俯仰或翻滚带歪。
        public static Vector3 ResolveConstrainedForward(t_bullet bulletBean, Vector3 forward, Vector3 fallbackForward)
        {
            var resolvedForward = forward.sqrMagnitude > 0.0001f ? forward : fallbackForward;
            if (resolvedForward.sqrMagnitude <= 0.0001f)
            {
                resolvedForward = Vector3.forward;
            }

            if (!HasConstrainedWorldY(bulletBean))
            {
                return resolvedForward.normalized;
            }

            if (TryNormalizeHorizontal(resolvedForward, out var horizontalForward))
            {
                return horizontalForward;
            }

            if (TryNormalizeHorizontal(fallbackForward, out horizontalForward))
            {
                return horizontalForward;
            }

            return Vector3.forward;
        }

        private static bool HasConstrainedWorldY(t_bullet bulletBean)
        {
            return bulletBean != null && bulletBean.t_trigger_Y != 0;
        }

        private static bool TryNormalizeHorizontal(Vector3 source, out Vector3 normalizedHorizontalForward)
        {
            source.y = 0.0f;
            if (source.sqrMagnitude <= 0.0001f)
            {
                normalizedHorizontalForward = Vector3.zero;
                return false;
            }

            normalizedHorizontalForward = source.normalized;
            return true;
        }
    }
}
