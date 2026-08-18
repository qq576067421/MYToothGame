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
    - t_trajectory：0 = 直线，1 = 抛物线，2 = 跟踪。
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

                TryUpdatePendingGuidance(owner);
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

                TryUpdatePendingGuidance(owner);
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

            private bool TryUpdatePendingGuidance(BulletObj owner)
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
                owner.AimAt(guidancePos);
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
        protected long m_HitTargetBuffId = 0;

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
        private IBulletTrajectory m_TrajectoryController;
        private const float m_DefaultTrackingLaunchMinYawSeparation = 15f;

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
                        OnFlying(dt);
                        break;
                    }
                case BulletState.Finish:
                    {

                        break;
                    }
                case BulletState.AfterFinish:
                    {

                        break;
                    }
            }
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
                m_TriggerBulletCount);
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
            float launchYawOffset = BuildTriggerLaunchYawOffset(triggerConfig.t_trajectory, m_TriggerBulletCount);

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

                var launchForward = BuildTriggerLaunchForward(triggerBean.t_trajectory, i, m_TriggerBulletCount, launchYawOffset);
                triggerBullet.SetDefender(null);
                var triggerSpawnPosition = ResolveTriggerSpawnPosition(triggerBean);
                triggerBullet.SetPosition(triggerSpawnPosition);
                triggerBullet.SetForward(launchForward);
                triggerBullet.SetAttacker(m_AttackerId, m_BulletGroup);
                triggerBullet.SetStartPos(triggerSpawnPosition);
                float flyDist = triggerBean.t_max_time / 1000.0f * triggerBean.t_move_speed / 1000.0f;
                triggerBullet.SetFlyDist(flyDist);

                long triggerHitBuff = 0;
                if (triggerBean.t_bullet_hittarget_buff_id != null && triggerBean.t_bullet_hittarget_buff_id.Count > 0)
                {
                    triggerHitBuff = triggerBean.t_bullet_hittarget_buff_id[0];
                }

                triggerBullet.SetTriggerChainVisitedConfigIds(childVisitedIds);
                triggerBullet.SetBulletConfig(
                    triggerBean.t_penetrate,
                    triggerBean.t_size,
                    triggerBean.t_trajectory,
                    triggerBean.t_tracking_range,
                    triggerHitBuff,
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
        }

        public void SetDashEnemy(int t_tankai_dist, float t_tan_time)
        {
            m_DashEnemyDist = t_tankai_dist;
            m_DashEnemyTime = t_tan_time;
        }

        public void SetBulletConfig(int penetrate, int size, int trajectory, int trackingRange, long hitTargetBuffId, int triggerBulletId, int triggerBulletCount, int triggerType)
        {
            m_CanPenetrate = (penetrate == 0);
            m_Size = size / 1000f;
            m_Trajectory = trajectory;
            m_TrackingRange = trackingRange / 1000f;
            m_HitTargetBuffId = hitTargetBuffId;
            m_TriggerBulletId = triggerBulletId;
            m_TriggerBulletCount = triggerBulletCount;
            m_TriggerType = triggerType;
        }



        private List<PropertyEntity> BuildTriggerTrackingTargets(int triggerTrajectory, int triggerTrackingRange, int triggerCount)
        {
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

        private Vector3 BuildTriggerLaunchForward(int triggerTrajectory, int triggerIndex, int triggerCount, float yawOffset)
        {
            var baseForward = GetForward();
            if (triggerTrajectory != 2 || triggerCount <= 1 || baseForward == Vector3.zero)
            {
                return baseForward;
            }

            BattleConst.ClampValues();
            float normalizedIndex = triggerCount > 1 ? (triggerIndex / (float)(triggerCount - 1)) * 2f - 1f : 0f;
            float yawStep = Mathf.Max(
                m_DefaultTrackingLaunchMinYawSeparation,
                BattleConst.SpawnYawSpread * 2f / (triggerCount - 1));
            float yaw = (triggerIndex - (triggerCount - 1) * 0.5f) * yawStep + yawOffset;
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

        private float BuildTriggerLaunchYawOffset(int triggerTrajectory, int triggerCount)
        {
            if (triggerTrajectory != 2 || triggerCount <= 1)
            {
                return 0f;
            }

            BattleConst.ClampValues();
            return Random.Range(-BattleConst.SpawnYawJitter, BattleConst.SpawnYawJitter);
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
                default:
                    return m_LinearTrajectory;
            }
        }

        private void ResetTrajectoryControllers()
        {
            m_LinearTrajectory.OnReset(this);
            m_ParabolicTrajectory.OnReset(this);
            m_TrackingTrajectory.OnReset(this);
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
            HandleHit(hitTarget, hitPoint);
            GetRender().BulletBoom(hitPoint, GetRender().GetForward());
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
            var actors = BattleManager.GetObjectManager().ReadPropertyEntities();
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

            if (m_Size > 0f)
            {
                bool isHit = false;
                int hitNum = 0;
                PropertyEntity firstHitTarget = null;
                Vector3 firstHitPos = Vector3.zero;
                float firstHitT = float.MaxValue;

                foreach (var kv in actors)
                {
                    var defender = kv;
                    if (!BattleManager.ReadIsEntityValide(defender))
                    {
                        continue;
                    }
                    if (defender.ReadIsInBorn())
                    {
                        continue;
                    }
                    if (!defender.ReadCanBeTarget() || !defender.ReadCanBeHurt())
                    {
                        continue;
                    }
                    if (defender.ReadHurtGroup() == m_BulletGroup)
                    {
                        continue;
                    }

                    if (!defender.TryIntersectSegment(currentPos, nextPos, m_Size, out var hitT, out var hitPoint))
                    {
                        continue;
                    }

                    isHit = true;
                    hitNum++;

                    if (firstHitTarget == null || hitT < firstHitT)
                    {
                        firstHitTarget = defender;
                        firstHitPos = hitPoint;
                        firstHitT = hitT;
                    }

                    if (m_CanPenetrate)
                    {
                        HandleHit(defender, hitPoint);
                        continue;
                    }
                }

                if (isHit)
                {
                    if (!m_CanPenetrate && firstHitTarget != null)
                    {
                        SetPosition(firstHitPos);
                        HandleHit(firstHitTarget, firstHitPos);
                        TriggerSizeDamage(firstHitPos);
                        CacheTriggerSpawnPositionOverride(firstHitTarget);
                        m_BulletState = BulletState.Finish;
                        GetRender().BulletBoom(firstHitPos, GetRender().GetForward());
                        OnFinish();
                        return;
                    }
                }
            }

            if (m_Size <= 0f)
            {
                foreach (var kv in actors)
                {
                    var defender = kv;
                    if (!BattleManager.ReadIsEntityValide(defender))
                    {
                        continue;
                    }
                    if (defender.ReadIsInBorn())
                    {
                        continue;
                    }
                    if (!defender.ReadCanBeTarget() || !defender.ReadCanBeHurt())
                    {
                        continue;
                    }
                    if (defender.ReadHurtGroup() == m_BulletGroup)
                    {
                        continue;
                    }
                    if (defender.TryIntersectSegment(currentPos, nextPos, m_DefaultSingleTargetHitRange, out var hitT, out var hitPoint))
                    {
                        if (m_CanPenetrate)
                        {
                            HandleHit(defender, hitPoint);
                        }
                        else
                        {
                            SetPosition(hitPoint);
                            HandleHit(defender, hitPoint);
                            CacheTriggerSpawnPositionOverride(defender);
                            m_BulletState = BulletState.Finish;
                            GetRender().BulletBoom(hitPoint, GetRender().GetForward());
                            OnFinish();
                            return;
                        }
                    }
                }
            }

            AdvanceNoTargetFlight(nextPos);
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

        private void HandleHit(PropertyEntity defender, Vector3 damageNumberWorldPos)
        {
            int defenderId = defender.ReadId();
            if (HasAlreadyAffectedEntity(defenderId))
            {
                return;
            }
            MarkAffectedEntity(defenderId);

            BattleManager.GetBattleTool().AddDefenderBuff(m_HurtInfo, defender);

            if (m_HitTargetBuffId > 0)
            {
                BattleManager.GetBattleTool().AddBuff(m_HurtInfo.m_Attacker, defender, m_HitTargetBuffId);
            }

            m_HurtInfo.SetDamageNumberWorldPos(damageNumberWorldPos);
            DamageCal.Cal(m_HurtInfo, defender);
            var attackedTarget = defender as MoveableCreature;
            if (attackedTarget != null)
            {
                attackedTarget.OnAttacked();
            }
            DamageCal.CalSickHP(m_HurtInfo.m_Attacker, m_HurtInfo.m_SickHP);
            m_HurtInfo.m_SickHP = 0;
            PlayHitEffect(defender.ReadHitPoint());
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

                    if (m_HitTargetBuffId > 0)
                    {
                        BattleManager.GetBattleTool().AddBuff(m_HurtInfo.m_Attacker, enemy, m_HitTargetBuffId);
                    }

                    m_HurtInfo.SetDamageNumberWorldPos(enemyHitPoint);
                    DamageCal.Cal(m_HurtInfo, enemy);
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

        public override void PoolObj()
        {
            m_DamagedEntityIds.Clear();
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
