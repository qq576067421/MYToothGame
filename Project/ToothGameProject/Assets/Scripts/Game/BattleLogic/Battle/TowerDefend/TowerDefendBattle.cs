using LCL;
using DG.Tweening;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Input = InputSystemCompat;

namespace GameDll
{
    public enum BattlePhase
    {
        NormalGame,
        ChallengeEntryWait,
        ChallengeCountdown,
        ChallengeActive,
        ChallengeFinish
    }

    // TowerDefendBattle: 负责塔防战斗的高层逻辑。
    // 职责包括：战斗初始化/销毁、玩家与对象管理、团队经验与升级挑战控制、
    // 场景点位注入以及对外提供运行时只读数据接口（例如当前波次、基地血量等）。
    public class TowerDefendBattle: IBattle
    {
        private const int m_TowerDefendMaxRoleLevel = 5;
        private const int m_SkillEnergyFullPermille = 10000;
        private const int m_SkillEnergyCostPermille = 5000;
        public const float m_UpgradeChallengeCountdown = 3.0f;
        private const float m_UpgradeChallengeForwardDistance = 5.0f;
        private const float m_NpcSpeakIntroLoadTimeoutSeconds = 2.0f;
        private const float m_NpcSpeakIntroGoTriggerSeconds = 7.0f;
        private const float m_NpcSpeakIntroFadeOutSeconds = 1.0f;
        private const float m_NpcSpeakIntroDuckTargetVolume = 0.35f;
        private const int m_UpgradeChallengeMaxLevelEffectId = 2;
        private const int m_RoleLevelUpEffectId = 99;
        private const int m_RoleLevelUpSoundId = 104;
        private const float m_RoleLevelUpEffectDuration = 2.0f;
        private const int m_GlobalConfigIdUpgradeChallengeBaseHealth = 100301;
        private const int m_GlobalConfigIdUpgradeChallengeHealthBoostPermille = 100302;
        private const int m_GlobalConfigIdUpgradeChallengeCrackLightThresholdPermille = 100303;
        private const int m_GlobalConfigIdUpgradeChallengeCrackMediumThresholdPermille = 100304;
        private const int m_GlobalConfigIdUpgradeChallengeCrackHeavyThresholdPermille = 100305;
        private const int m_DefaultUpgradeChallengeBaseHealth = 50;
        private const int m_DefaultUpgradeChallengeHealthBoostPermille = 8000;
        private const int m_DefaultUpgradeChallengeCrackLightThresholdPermille = 8000;
        private const int m_DefaultUpgradeChallengeCrackMediumThresholdPermille = 5000;
        private const int m_DefaultUpgradeChallengeCrackHeavyThresholdPermille = 2000;
        private static readonly int m_NpcSpeakIntroFadeColorPropertyId = Shader.PropertyToID("_BaseColor");
        // 原始策划案要求技能能量上限 50 点，每秒回复 1 点。
        // 运行时仍沿用现有 0~10000 的百分比字段，因此每秒回能换算为 200/10000。
        private const int m_SkillEnergyRechargePermillePerSecond = 200;
        public const float m_UpgradeChallengeDuration = 10.0f;
        private const float m_ManualYawSpeedDegrees = 36.0f;
        private const float m_ManualYawClampDegrees = 89.5f;
        private uint m_Seed = 0;
        private long m_FIghtId;
        private int m_Stage;
        private int m_BaseHealth;
        private int m_BaseMaxHealth;
        private long m_TeamExpCurrent;
        private long m_TeamExpMax;
        private bool m_IsUpgradeChallengeReady;
        private bool m_IsUpgradeChallengeCameraReady;
        private bool m_IsUpgradeChallengeTargetPendingDestroy;
        private float m_RuntimeUpgradeChallengeCountdown = m_UpgradeChallengeCountdown;
        private BattlePhase m_Phase = BattlePhase.NormalGame;
        private float m_PhaseTimer = 0f;
        private UpgradeChallengeTarget m_UpgradeChallengeTarget;
        private Vector3 m_UpgradeChallengeFoot;
        private readonly Dictionary<long, int> m_UpgradeChallengeScores = new Dictionary<long, int>();
        private Vector3 m_BasePoint = Vector3.zero;
        private float m_BaseReachRadius = 1.5f;
        private Transform m_NpcSpeakPoint;
        private TowerDefendNpcSpeakEntity m_NpcSpeakIntroEntity;
        private bool m_NpcSpeakIntroEntered;
        private bool m_NpcSpeakIntroTimerStarted;
        private float m_NpcSpeakIntroTimeLeft;
        private float m_NpcSpeakIntroLoadTimeoutLeft;
        private int m_NpcSpeakIntroSoundId;
        private float m_NpcSpeakIntroDurationSeconds;
        private bool m_NpcSpeakIntroGoPlayed;
        private Tween m_NpcSpeakIntroFadeTween;
        private float m_NpcSpeakIntroFadeAlpha = 1.0f;
        private readonly List<Vector3> m_MonsterSpawnPoints = new List<Vector3>();

        private readonly List<TowerDefendMonsterPathPointData> m_MonsterPathPoints = new List<TowerDefendMonsterPathPointData>();
        private readonly List<Vector3> m_GuardHeroSpawnPoints = new List<Vector3>();
        private readonly Dictionary<int, Vector2> m_ManualAimAngles = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, GuardHeroAutoAimRuntimeState> m_GuardHeroAutoAimStates = new Dictionary<int, GuardHeroAutoAimRuntimeState>();
        private readonly Dictionary<int, RenderEff> m_UpgradeChallengeMaxLevelEffects = new Dictionary<int, RenderEff>();
        private readonly List<int> m_UpgradeChallengeMaxLevelEffectRecycleSeats = new List<int>();
        private readonly HashSet<int> m_PostUpgradeChallengeAimRecoveringSeats = new HashSet<int>();
        private readonly List<int> m_PostUpgradeChallengeAimRecoveryFinishedSeats = new List<int>();
        private float m_LastRuntimeDeltaTime = 1.0f / 30.0f;
        private const long UpgradeChallengeZeroGravityRoleCfgId = 1002L;
        private const float RuntimeBulletBaseGravity = 9.8f * 0.2f;

        public struct GuardHeroNormalAutoAimSolution
        {
            public bool m_HasSnapTarget;
            public bool m_UseParabolic;
            public Vector3 m_ResolvedLaunchForward;
        }

        private struct GuardHeroNormalAutoAimTarget
        {
            public bool m_TargetIsUpgradeChallenge;
            public Vector3 m_HorizontalForward;
            public Vector3 m_HitPoint;
            public float m_HorizontalDistanceAlongRay;
        }

        private struct GuardHeroAutoAimRuntimeState
        {
            public float m_SmoothedDistanceAlongRay;
            public float m_SmoothedAimY;
            public int m_LastSmoothFrame;
            public bool m_TargetIsUpgradeChallenge;
            public bool m_IsRecoveringFromUpgradeChallenge;
        }

        private struct NpcSpeakIntroRequest
        {
            public int MonsterConfigId;
            public int SoundId;
            public float DurationSeconds;
        }

        // 返回当前无尽模式下已经推进的波次（或刷怪器报告的波次）。
        public override int GetWildWave()
        {
            return m_BattleSpawer.ReadWildWave();
        }
        // 返回当前战斗使用的场景关卡 ID。
        public override int GetStage()
        {
            return m_Stage;
        }
        // 返回本场战斗的唯一 FightId（用于日志、存档、排行榜关联）。
        public override long GetFightId()
        {
            return m_FIghtId;
        }
        private TowerDefendBattleSpawer m_BattleSpawer = null;
        private TowerDefendBattleStatistical m_BattleStat = null;
        private int m_ControlledSeatId = TowerDefendSeatLayout.DefaultControlledSeatId;
        public int ReadControlledSeatId() { return m_ControlledSeatId; }
        public Vector3 ReadManualAimForwardBySeat(int seatId)
        {
            var hero = m_BattleSpawer != null ? m_BattleSpawer.ReadGuardHeroBySeat(seatId) : null;
            var angles = EnsureManualAimAngles(seatId, hero);
            // 手操控制层的根节点水平朝向只允许由缓存的 yaw 驱动，不能把当前开火点朝向再读回来自我驱动。
            // 否则 fire_pos 本身因为骨骼姿态、动画或武器节点存在少量水平偏差时，会反过来带着根节点自己转动。
            // 这里仍然保留“基础姿态不同步，只同步俯仰变化值”的规则，因此最终方向由缓存的 yaw 加上角色自己的基础俯仰计算得到。
            return BuildForwardFromYawPitch(angles.x, ResolveManualAimPitchDegrees(hero, angles.y));
        }

        // 塔防手操普攻的自动吸附只允许修正竖直方向。
        // 求解分两步：
        // 1. 先固定水平射线。优先取当前开火点的水平朝向；如果这条射线没有可吸附目标，再回退到当前输入方向或角色水平朝向。
        // 2. 再沿这条水平射线反求“为了命中目标高度，真正应该使用的完整发射方向”。
        // 这份完整方向必须同时驱动角色俯仰表现、瞄准线显示和真正发弹，否则就会出现显示与命中不一致。
        // 如果玩家水平转动后暂时超出了吸附阈值，这里不会立刻把俯仰打回默认值，而是继续保留上一份已求出来的俯仰，
        // 直到水平射线找到了下一份可吸附高度，再用新的结果覆盖旧俯仰。
        // 追踪弹会在飞行过程中持续修正目标与方向，不适合走这套静态吸附求解，因此这里直接排除。
        // 返回 true 只表示当前普攻走这套求解链路；是否真的吸到了目标，由 solution.m_HasSnapTarget 决定。
        public bool TryResolveGuardHeroNormalAutoAim(
            PropertyEntity attacker,
            Skill skill,
            Vector3 requestedForward,
            out GuardHeroNormalAutoAimSolution solution)
        {
            solution = default;
            if (!BattleManager.ReadIsEntityValide(attacker) ||
                skill == null ||
                skill.ReadSlot() != 0 ||
                !IsGuardHeroEntity(attacker))
            {
                return false;
            }

            var skillBean = skill.GetSkillBean();
            var bulletCfg = skillBean != null ? t_bullet.GetConfig(skillBean.t_bullet_id, false) : null;
            if (bulletCfg == null)
            {
                return false;
            }
            if (!IsTowerDefendAutoAimEnabled())
            {
                return false;
            }
            if (IsTrackingTrajectoryBullet(bulletCfg))
            {
                return false;
            }

            solution.m_UseParabolic = ShouldUseParabolicAim(skill, bulletCfg);
            var aimOrigin = attacker.ReadResolvedFirePointCenter();
            var castDistance = Mathf.Max(0.5f, attacker.GetSkillCastDist(skill));
            var projectileSpeed = Mathf.Max(0.0f, bulletCfg.t_move_speed / 1000.0f);
            var gravity = ResolveRuntimeBulletGravityAcceleration(attacker, bulletCfg);

            var defaultLaunchForward = ResolveAutoAimDefaultLaunchForward(attacker, requestedForward);
            solution.m_ResolvedLaunchForward = defaultLaunchForward;

            var primaryHorizontalForward = ResolveAutoAimPrimaryHorizontalForward(attacker, requestedForward);
            if (primaryHorizontalForward.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            var fallbackHorizontalForward = ResolveAutoAimFallbackHorizontalForward(attacker, requestedForward, primaryHorizontalForward);
            if (!TryResolveGuardHeroNormalAutoAimTarget(
                    attacker,
                    aimOrigin,
                    primaryHorizontalForward,
                    fallbackHorizontalForward,
                    castDistance,
                    out var target))
            {
                // 没有真实目标时，不再伪造一份吸附目标。
                // 失焦后的俯仰保持统一只依赖 m_ManualAimAngles，避免“明明没有瞄准线目标却突然重新俯仰”。
                return true;
            }

            target = ResolvePredictedUpgradeChallengeAutoAimTarget(
                aimOrigin,
                target,
                castDistance,
                solution.m_UseParabolic,
                projectileSpeed,
                gravity);
            if (!target.m_TargetIsUpgradeChallenge &&
                TryReadGuardHeroSeatId(attacker, out var recoveringSeatId) &&
                m_PostUpgradeChallengeAimRecoveringSeats.Contains(recoveringSeatId))
            {
                // 棒棒糖结束后的常规俯仰恢复优先于普通怪吸附，避免有目标和无目标时恢复速度不一致。
                m_GuardHeroAutoAimStates.Remove(recoveringSeatId);
                solution.m_ResolvedLaunchForward = ReadManualAimForwardBySeat(recoveringSeatId);
                return true;
            }

            target = ResolveAutoAimTargetWithSmoothing(
                attacker,
                target,
                solution.m_UseParabolic,
                projectileSpeed,
                gravity);

            if (!TryResolveAutoAimLaunchForwardAndFlightTime(
                    aimOrigin,
                    target,
                    solution.m_UseParabolic,
                    projectileSpeed,
                    gravity,
                    out var resolvedLaunchForward,
                    out var resolvedFlightTime))
            {
                return true;
            }

            if (solution.m_UseParabolic)
            {
                var maxLifetime = Mathf.Max(0.05f, bulletCfg.t_max_time / 1000.0f);
                if (resolvedFlightTime > maxLifetime + 0.01f)
                {
                    return true;
                }
            }

            solution.m_ResolvedLaunchForward = resolvedLaunchForward;
            solution.m_HasSnapTarget = true;
            TryUpdateControlledSeatAutoAimPitch(attacker, solution.m_ResolvedLaunchForward);
            return true;
        }

        public static bool ShouldUseParabolicAim(Skill skill, t_bullet bulletCfg)
        {
            if (skill == null)
            {
                return bulletCfg != null && bulletCfg.t_trajectory == 1;
            }

            var skillBean = skill.GetSkillBean();
            if (skillBean != null && skillBean.t_class_Id == SkillType.CommonPaodanSkill)
            {
                return true;
            }

            return bulletCfg != null && bulletCfg.t_trajectory == 1;
        }

        // 棒棒糖阶段里，角色3的抛物线普攻需要临时按直线处理，因此这里统一给出“本次真实生效的重力值”。
        // 这样自动吸附预解算、瞄准线和真正发出去的子弹都能走同一份重力，不会出现俯仰和弹道不一致。
        public static float ResolveRuntimeBulletGravityAcceleration(PropertyEntity attacker, t_bullet bulletCfg)
        {
            return RuntimeBulletBaseGravity * ResolveRuntimeBulletGravityPermille(attacker, bulletCfg) / 1000.0f;
        }

        private static int ReadUpgradeChallengeGlobalInt(int configId, int defaultValue)
        {
            var cfg = t_globalBean.GetConfig(configId, false);
            if (cfg == null)
            {
                return defaultValue;
            }

            return cfg.t_int;
        }

        public static int ReadUpgradeChallengeBaseHealth()
        {
            return Mathf.Max(1, ReadUpgradeChallengeGlobalInt(
                m_GlobalConfigIdUpgradeChallengeBaseHealth,
                m_DefaultUpgradeChallengeBaseHealth));
        }

        public static int ReadUpgradeChallengeHealthBoostPermille()
        {
            return Mathf.Max(0, ReadUpgradeChallengeGlobalInt(
                m_GlobalConfigIdUpgradeChallengeHealthBoostPermille,
                m_DefaultUpgradeChallengeHealthBoostPermille));
        }

        public static int ReadUpgradeChallengeCrackLightThresholdPermille()
        {
            return Mathf.Clamp(
                ReadUpgradeChallengeGlobalInt(
                    m_GlobalConfigIdUpgradeChallengeCrackLightThresholdPermille,
                    m_DefaultUpgradeChallengeCrackLightThresholdPermille),
                0,
                10000);
        }

        public static int ReadUpgradeChallengeCrackMediumThresholdPermille()
        {
            return Mathf.Clamp(
                ReadUpgradeChallengeGlobalInt(
                    m_GlobalConfigIdUpgradeChallengeCrackMediumThresholdPermille,
                    m_DefaultUpgradeChallengeCrackMediumThresholdPermille),
                0,
                10000);
        }

        public static int ReadUpgradeChallengeCrackHeavyThresholdPermille()
        {
            return Mathf.Clamp(
                ReadUpgradeChallengeGlobalInt(
                    m_GlobalConfigIdUpgradeChallengeCrackHeavyThresholdPermille,
                    m_DefaultUpgradeChallengeCrackHeavyThresholdPermille),
                0,
                10000);
        }

        // 基础血量已经包含第一个可攻击角色，只有额外角色才叠加提升比例。
        // 例如基础 150、提升 80%，1 至 4 个可攻击角色分别得到 150、270、390、510。
        public static int ResolveUpgradeChallengeMaxHealth(int attackableRoleCount)
        {
            int additionalRoleCount = Mathf.Max(0, attackableRoleCount - 1);
            long scaledHealth = (long)ReadUpgradeChallengeBaseHealth() *
                (10000L + (long)additionalRoleCount * ReadUpgradeChallengeHealthBoostPermille());
            return Mathf.Max(1, Mathf.RoundToInt(scaledHealth / 10000.0f));
        }

        public static int ResolveRuntimeBulletGravityPermille(PropertyEntity attacker, t_bullet bulletCfg)
        {
            int gravityPermille = bulletCfg != null ? bulletCfg.t_Gravity : 0;
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle != null && battle.ShouldOverrideRole3BulletGravityToZero(attacker))
            {
                return 0;
            }

            return gravityPermille;
        }

        private bool ShouldOverrideRole3BulletGravityToZero(PropertyEntity attacker)
        {
            if (!BattleManager.ReadIsEntityValide(attacker) ||
                (!ReadIsUpgradeChallengePreActive() && !ReadIsUpgradeChallengeActive()) ||
                !BattleManager.ReadIsEntityValide(m_UpgradeChallengeTarget))
            {
                return false;
            }

            long playerId = attacker.ReadBattlePlayerId();
            if (playerId <= 0)
            {
                return false;
            }

            var player = GetPlayer(playerId);
            return player != null && player.m_RoleCfgId == UpgradeChallengeZeroGravityRoleCfgId;
        }

        private static bool IsTrackingTrajectoryBullet(t_bullet bulletCfg)
        {
            return bulletCfg != null && bulletCfg.t_trajectory == 2;
        }

        private static bool IsTowerDefendAutoAimEnabled()
        {
            BattleConst.ClampTowerDefendAutoAimValues();
            return BattleConst.TowerDefendAutoAimEnabled;
        }

        private bool IsGuardHeroEntity(PropertyEntity entity)
        {
            if (!BattleManager.ReadIsEntityValide(entity) || m_BattleSpawer == null)
            {
                return false;
            }

            var heroes = m_BattleSpawer.ReadGuardHeroes();
            if (heroes == null)
            {
                return false;
            }

            int heroCount = heroes.Count;
            for (int i = 0; i < heroCount; i++)
            {
                if (heroes[i] == entity)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 ResolveAutoAimDefaultLaunchForward(PropertyEntity attacker, Vector3 requestedForward)
        {
            if (BattleManager.ReadIsEntityValide(attacker))
            {
                var firePointForward = attacker.ReadResolvedFirePointForward();
                if (firePointForward.sqrMagnitude > 0.0001f)
                {
                    return firePointForward.normalized;
                }

                var roleForward = attacker.ReadForward();
                if (requestedForward.sqrMagnitude <= 0.0001f && roleForward.sqrMagnitude > 0.0001f)
                {
                    return roleForward.normalized;
                }
            }

            if (requestedForward.sqrMagnitude > 0.0001f)
            {
                return requestedForward.normalized;
            }

            return Vector3.forward;
        }

        private static Vector3 ResolveAutoAimPrimaryHorizontalForward(PropertyEntity attacker, Vector3 requestedForward)
        {
            if (BattleManager.ReadIsEntityValide(attacker))
            {
                var firePointForward = attacker.ReadResolvedFirePointForward();
                if (TryNormalizeHorizontal(firePointForward, out var firePointHorizontalForward))
                {
                    return firePointHorizontalForward;
                }
            }

            if (TryNormalizeHorizontal(requestedForward, out var requestedHorizontalForward))
            {
                return requestedHorizontalForward;
            }

            if (BattleManager.ReadIsEntityValide(attacker) &&
                TryNormalizeHorizontal(attacker.ReadForward(), out var roleHorizontalForward))
            {
                return roleHorizontalForward;
            }

            return Vector3.zero;
        }

        private static Vector3 ResolveAutoAimFallbackHorizontalForward(
            PropertyEntity attacker,
            Vector3 requestedForward,
            Vector3 primaryHorizontalForward)
        {
            if (BattleManager.ReadIsEntityValide(attacker) &&
                TryNormalizeHorizontal(attacker.ReadForward(), out var roleHorizontalForward) &&
                (roleHorizontalForward - primaryHorizontalForward).sqrMagnitude > 0.0001f)
            {
                return roleHorizontalForward;
            }

            if (TryNormalizeHorizontal(requestedForward, out var requestedHorizontalForward) &&
                (requestedHorizontalForward - primaryHorizontalForward).sqrMagnitude > 0.0001f)
            {
                return requestedHorizontalForward;
            }

            return primaryHorizontalForward;
        }

        private bool TryResolveGuardHeroNormalAutoAimTarget(
            PropertyEntity attacker,
            Vector3 aimOrigin,
            Vector3 primaryHorizontalForward,
            Vector3 fallbackHorizontalForward,
            float castDistance,
            out GuardHeroNormalAutoAimTarget target)
        {
            target = default;
            if (TryResolveUpgradeChallengeAutoAimTarget(aimOrigin, primaryHorizontalForward, castDistance, 0.0f, out target))
            {
                return true;
            }

            if (TryResolveMonsterAutoAimTarget(attacker, aimOrigin, primaryHorizontalForward, castDistance, out target))
            {
                return true;
            }

            if ((fallbackHorizontalForward - primaryHorizontalForward).sqrMagnitude > 0.0001f &&
                TryResolveMonsterAutoAimTarget(attacker, aimOrigin, fallbackHorizontalForward, castDistance, out target))
            {
                return true;
            }

            return false;
        }

        private bool TryResolveUpgradeChallengeAutoAimTarget(
            Vector3 aimOrigin,
            Vector3 horizontalForward,
            float castDistance,
            float predictionTime,
            out GuardHeroNormalAutoAimTarget target)
        {
            target = default;
            if ((!ReadIsUpgradeChallengePreActive() && !ReadIsUpgradeChallengeActive()) ||
                !BattleManager.ReadIsEntityValide(m_UpgradeChallengeTarget))
            {
                return false;
            }

            // 棒棒糖的自动吸附语义固定为：
            // 1. Y 始终跟随 center 节点本身的世界坐标；
            // 2. 前向可命中距离始终来自 center 节点上的 MeshCollider；
            // 3. 这两者都允许按预计飞行时间前推，避免棒棒糖继续运动后，预解算还停留在旧帧。
            if (!m_UpgradeChallengeTarget.TryResolveAutoAimTargetOnCurrentVerticalPlane(
                    aimOrigin,
                    horizontalForward,
                    castDistance,
                    predictionTime,
                    out var centerPosition,
                    out var meshAlongRayDistance))
            {
                return false;
            }

            target.m_TargetIsUpgradeChallenge = true;
            target.m_HorizontalForward = horizontalForward;
            target.m_HitPoint = centerPosition;
            target.m_HorizontalDistanceAlongRay = meshAlongRayDistance;
            return true;
        }

        private GuardHeroNormalAutoAimTarget ResolvePredictedUpgradeChallengeAutoAimTarget(
            Vector3 aimOrigin,
            GuardHeroNormalAutoAimTarget target,
            float castDistance,
            bool useParabolic,
            float projectileSpeed,
            float gravity)
        {
            if (!target.m_TargetIsUpgradeChallenge ||
                !BattleManager.ReadIsEntityValide(m_UpgradeChallengeTarget) ||
                projectileSpeed <= 0.0001f)
            {
                return target;
            }

            const int maxPredictIterationCount = 2;
            for (int i = 0; i < maxPredictIterationCount; i++)
            {
                if (!TryResolveAutoAimLaunchForwardAndFlightTime(
                        aimOrigin,
                        target,
                        useParabolic,
                        projectileSpeed,
                        gravity,
                        out _,
                        out var predictedFlightTime) ||
                    predictedFlightTime <= 0.0001f)
                {
                    break;
                }

                if (!TryResolveUpgradeChallengeAutoAimTarget(
                        aimOrigin,
                        target.m_HorizontalForward,
                        castDistance,
                        predictedFlightTime,
                        out var predictedTarget))
                {
                    break;
                }

                target = predictedTarget;
            }

            return target;
        }

        // 自动吸附目标只负责描述“当前这条水平射线，在多远处需要达到什么高度”。
        // 真正的完整发射方向仍然要在这里统一反解，保证角色俯仰、瞄准线和真实子弹使用同一份结果。
        private static bool TryResolveAutoAimLaunchForwardAndFlightTime(
            Vector3 aimOrigin,
            GuardHeroNormalAutoAimTarget target,
            bool useParabolic,
            float projectileSpeed,
            float gravity,
            out Vector3 launchForward,
            out float flightTime)
        {
            launchForward = Vector3.zero;
            flightTime = 0.0f;
            if (!TryBuildAutoAimHeightPointOnCurrentRay(aimOrigin, target, out var targetHeightPointOnCurrentRay))
            {
                return false;
            }

            var targetAimDelta = targetHeightPointOnCurrentRay - aimOrigin;
            if (useParabolic)
            {
                return TryResolveParabolicAutoAimLaunchForward(
                    aimOrigin,
                    targetHeightPointOnCurrentRay,
                    target.m_HorizontalForward,
                    projectileSpeed,
                    gravity,
                    out launchForward,
                    out flightTime);
            }

            launchForward = targetAimDelta.normalized;
            if (projectileSpeed > 0.0001f)
            {
                flightTime = targetAimDelta.magnitude / projectileSpeed;
            }

            return true;
        }

        private static bool TryBuildAutoAimHeightPointOnCurrentRay(
            Vector3 aimOrigin,
            GuardHeroNormalAutoAimTarget target,
            out Vector3 targetHeightPointOnCurrentRay)
        {
            targetHeightPointOnCurrentRay = aimOrigin + target.m_HorizontalForward * target.m_HorizontalDistanceAlongRay;
            targetHeightPointOnCurrentRay.y = target.m_HitPoint.y;
            return (targetHeightPointOnCurrentRay - aimOrigin).sqrMagnitude > 0.0001f;
        }

        private static bool TryResolveMonsterAutoAimTarget(
            PropertyEntity attacker,
            Vector3 aimOrigin,
            Vector3 horizontalForward,
            float castDistance,
            out GuardHeroNormalAutoAimTarget target)
        {
            target = default;
            var maxHorizontalOffsetToRay = BattleConst.TowerDefendAutoAimSnapMaxHorizontalDistance;
            if (maxHorizontalOffsetToRay <= 0.0f)
            {
                return false;
            }

            var objectManager = BattleManager.GetObjectManager();
            if (objectManager == null)
            {
                return false;
            }

            var foundTarget = false;
            var bestTargetZ = float.MinValue;
            var bestHorizontalOffsetToRay = float.MaxValue;
            var bestAlongRayDistance = float.MaxValue;
            var bestHitPoint = Vector3.zero;
            objectManager.ReadPropertyEntities((_, entity) =>
            {
                if (!BattleManager.ReadIsEntityValide(entity) ||
                    entity == attacker ||
                    entity.ReadHurtGroup() == attacker.ReadHurtGroup() ||
                    !entity.ReadCanBeTarget() ||
                    (!entity.ReadIsSmallMonster() && !entity.ReadIsBoss()))
                {
                    return true;
                }

                // 小怪和 boss 的自动吸附统一读取根节点 hit 节点，只修正自动吸附和俯仰，不改真实命中判定。
                if (!entity.TryReadAutoAimPoint(out var hitPoint))
                {
                    return true;
                }
                if (!TryProjectPointToHorizontalRay(aimOrigin, horizontalForward, hitPoint, out var alongRayDistance, out var horizontalOffsetToRay) ||
                    alongRayDistance > castDistance + 0.01f ||
                    horizontalOffsetToRay > maxHorizontalOffsetToRay + 0.0001f)
                {
                    return true;
                }

                // 在满足横向吸附阈值后，优先选择更靠近我方的目标，也就是世界坐标 z 更大的目标。
                // 这样前后两排怪都可吸附时，不会因为后排目标恰好更贴近射线，就把辅助瞄准线的 Y 来回拉跳。
                if (!foundTarget ||
                    hitPoint.z > bestTargetZ + 0.0001f ||
                    (Mathf.Abs(hitPoint.z - bestTargetZ) <= 0.0001f &&
                     (horizontalOffsetToRay < bestHorizontalOffsetToRay - 0.0001f ||
                      (Mathf.Abs(horizontalOffsetToRay - bestHorizontalOffsetToRay) <= 0.0001f &&
                       alongRayDistance < bestAlongRayDistance))))
                {
                    foundTarget = true;
                    bestTargetZ = hitPoint.z;
                    bestHorizontalOffsetToRay = horizontalOffsetToRay;
                    bestAlongRayDistance = alongRayDistance;
                    bestHitPoint = hitPoint;
                }

                return true;
            });

            if (!foundTarget)
            {
                return false;
            }

            target.m_HorizontalForward = horizontalForward;
            target.m_HitPoint = bestHitPoint;
            target.m_HorizontalDistanceAlongRay = bestAlongRayDistance;
            return true;
        }

        private static bool TryProjectPointToHorizontalRay(
            Vector3 rayOrigin,
            Vector3 horizontalForward,
            Vector3 point,
            out float alongRayDistance,
            out float horizontalOffsetToRay)
        {
            alongRayDistance = 0.0f;
            horizontalOffsetToRay = float.MaxValue;
            if (!TryNormalizeHorizontal(horizontalForward, out var normalizedHorizontalForward))
            {
                return false;
            }

            var horizontalDelta = point - rayOrigin;
            horizontalDelta.y = 0.0f;
            alongRayDistance = Vector3.Dot(horizontalDelta, normalizedHorizontalForward);
            if (alongRayDistance <= 0.0001f)
            {
                return false;
            }

            var closestPointOnRay = normalizedHorizontalForward * alongRayDistance;
            horizontalOffsetToRay = (horizontalDelta - closestPointOnRay).magnitude;
            return true;
        }

        private GuardHeroNormalAutoAimTarget ResolveAutoAimTargetWithSmoothing(
            PropertyEntity attacker,
            GuardHeroNormalAutoAimTarget target,
            bool useParabolic,
            float projectileSpeed,
            float gravity)
        {
            if (!TryReadGuardHeroSeatId(attacker, out var seatId))
            {
                return target;
            }

            BattleConst.ClampTowerDefendAutoAimValues();
            if (BattleConst.TowerDefendAutoAimSwitchSmoothSpeed <= 0.0f)
            {
                CacheGuardHeroAutoAimRuntimeState(seatId, target);
                return target;
            }

            var currentFrame = Time.frameCount;
            if (!m_GuardHeroAutoAimStates.TryGetValue(seatId, out var state))
            {
                // 首次吸附和切到棒棒糖时，也应该从“当前已经显示出来的俯仰”开始平滑过渡，
                // 而不是直接把缓存初始化到目标高度，否则玩家看到的第一下仍然会瞬间跳变。
                state = CreateGuardHeroAutoAimRuntimeState(attacker, target, useParabolic, projectileSpeed, gravity);
                state.m_LastSmoothFrame = currentFrame - 1;
            }

            if (state.m_LastSmoothFrame != currentFrame)
            {
                var isPostUpgradeChallengeAimRecovering = m_PostUpgradeChallengeAimRecoveringSeats.Contains(seatId);
                if ((state.m_TargetIsUpgradeChallenge || isPostUpgradeChallengeAimRecovering) &&
                    !target.m_TargetIsUpgradeChallenge)
                {
                    state.m_IsRecoveringFromUpgradeChallenge = true;
                }
                else if (target.m_TargetIsUpgradeChallenge)
                {
                    state.m_IsRecoveringFromUpgradeChallenge = false;
                    m_PostUpgradeChallengeAimRecoveringSeats.Remove(seatId);
                }

                var deltaTime = Mathf.Max(0.0f, m_LastRuntimeDeltaTime);
                // 棒棒糖结束后第一次重新吸回普通怪时，距离和高度都走恢复速度。
                // 这样平时怪物之间切目标仍保持原手感，但不会在恢复未完成时先按普通吸附速度快速拉回。
                var recoverSmoothSpeed = BattleConst.TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed > 0.0f
                    ? BattleConst.TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed
                    : BattleConst.TowerDefendAutoAimSwitchSmoothSpeed;
                var smoothSpeed = state.m_IsRecoveringFromUpgradeChallenge
                    ? recoverSmoothSpeed
                    : BattleConst.TowerDefendAutoAimSwitchSmoothSpeed;
                var maxStep = smoothSpeed * deltaTime;
                state.m_SmoothedDistanceAlongRay = Mathf.MoveTowards(
                    state.m_SmoothedDistanceAlongRay,
                    target.m_HorizontalDistanceAlongRay,
                    maxStep);
                state.m_SmoothedAimY = Mathf.MoveTowards(
                    state.m_SmoothedAimY,
                    target.m_HitPoint.y,
                    maxStep);
                if (!target.m_TargetIsUpgradeChallenge &&
                    Mathf.Abs(state.m_SmoothedDistanceAlongRay - target.m_HorizontalDistanceAlongRay) <= 0.0001f &&
                    Mathf.Abs(state.m_SmoothedAimY - target.m_HitPoint.y) <= 0.0001f)
                {
                    state.m_IsRecoveringFromUpgradeChallenge = false;
                }
                state.m_LastSmoothFrame = currentFrame;
                state.m_TargetIsUpgradeChallenge = target.m_TargetIsUpgradeChallenge;
                m_GuardHeroAutoAimStates[seatId] = state;
            }

            target.m_HorizontalDistanceAlongRay = state.m_SmoothedDistanceAlongRay;
            target.m_HitPoint.y = state.m_SmoothedAimY;
            return target;
        }

        private GuardHeroAutoAimRuntimeState CreateGuardHeroAutoAimRuntimeState(
            PropertyEntity attacker,
            GuardHeroNormalAutoAimTarget target,
            bool useParabolic,
            float projectileSpeed,
            float gravity)
        {
            var state = new GuardHeroAutoAimRuntimeState
            {
                m_SmoothedDistanceAlongRay = target.m_HorizontalDistanceAlongRay,
                m_SmoothedAimY = target.m_HitPoint.y,
                m_LastSmoothFrame = Time.frameCount,
                m_TargetIsUpgradeChallenge = target.m_TargetIsUpgradeChallenge,
                m_IsRecoveringFromUpgradeChallenge = false,
            };

            if (BattleManager.ReadIsEntityValide(attacker) &&
                TryResolveCurrentAutoAimReferenceForward(attacker, out var currentForward))
            {
                var aimOrigin = attacker.ReadResolvedFirePointCenter();
                bool hasCurrentAimY = useParabolic
                    ? TryResolveParabolicAimYAtHorizontalDistance(
                        aimOrigin,
                        currentForward,
                        target.m_HorizontalDistanceAlongRay,
                        projectileSpeed,
                        gravity,
                        out var currentAimY)
                    : TryResolveAimYAtHorizontalDistance(
                        aimOrigin,
                        currentForward,
                        target.m_HorizontalDistanceAlongRay,
                        out currentAimY);
                if (hasCurrentAimY)
                {
                    state.m_SmoothedAimY = currentAimY;
                }
            }

            return state;
        }

        private void CacheGuardHeroAutoAimRuntimeState(int seatId, GuardHeroNormalAutoAimTarget target)
        {
            m_GuardHeroAutoAimStates[seatId] = new GuardHeroAutoAimRuntimeState
            {
                m_SmoothedDistanceAlongRay = target.m_HorizontalDistanceAlongRay,
                m_SmoothedAimY = target.m_HitPoint.y,
                m_LastSmoothFrame = Time.frameCount,
                m_TargetIsUpgradeChallenge = target.m_TargetIsUpgradeChallenge,
                m_IsRecoveringFromUpgradeChallenge = false,
            };
        }

        private bool TryReadGuardHeroSeatId(PropertyEntity hero, out int seatId)
        {
            seatId = -1;
            if (!BattleManager.ReadIsEntityValide(hero) || m_BattleSpawer == null)
            {
                return false;
            }

            for (int i = 0; i < TowerDefendSeatLayout.MaxSupportedPlayerCount; i++)
            {
                if (m_BattleSpawer.ReadGuardHeroBySeat(i) == hero)
                {
                    seatId = i;
                    return true;
                }
            }

            return false;
        }

        private bool TryResolveCurrentAutoAimReferenceForward(PropertyEntity attacker, out Vector3 forward)
        {
            forward = Vector3.zero;
            if (!BattleManager.ReadIsEntityValide(attacker))
            {
                return false;
            }

            if (TryReadGuardHeroSeatId(attacker, out var seatId))
            {
                var manualAimForward = ReadManualAimForwardBySeat(seatId);
                if (manualAimForward.sqrMagnitude > 0.0001f)
                {
                    forward = manualAimForward.normalized;
                    return true;
                }
            }

            var firePointForward = attacker.ReadResolvedFirePointForward();
            if (firePointForward.sqrMagnitude > 0.0001f)
            {
                forward = firePointForward.normalized;
                return true;
            }

            var roleForward = attacker.ReadForward();
            if (roleForward.sqrMagnitude > 0.0001f)
            {
                forward = roleForward.normalized;
                return true;
            }

            return false;
        }

        private static bool TryResolveAimYAtHorizontalDistance(
            Vector3 aimOrigin,
            Vector3 forward,
            float horizontalDistance,
            out float aimY)
        {
            aimY = aimOrigin.y;
            if (horizontalDistance <= 0.0001f)
            {
                return true;
            }

            var normalizedForward = NormalizeForwardOrDefault(forward);
            var planarLength = new Vector2(normalizedForward.x, normalizedForward.z).magnitude;
            if (planarLength <= 0.0001f)
            {
                return false;
            }

            var travelDistance = horizontalDistance / planarLength;
            aimY = aimOrigin.y + normalizedForward.y * travelDistance;
            return true;
        }

        private static bool TryResolveParabolicAimYAtHorizontalDistance(
            Vector3 aimOrigin,
            Vector3 forward,
            float horizontalDistance,
            float projectileSpeed,
            float gravity,
            out float aimY)
        {
            aimY = aimOrigin.y;
            if (horizontalDistance <= 0.0001f)
            {
                return true;
            }

            var normalizedForward = NormalizeForwardOrDefault(forward);
            var planarLength = new Vector2(normalizedForward.x, normalizedForward.z).magnitude;
            if (planarLength <= 0.0001f || projectileSpeed <= 0.0001f)
            {
                return false;
            }

            var horizontalSpeed = projectileSpeed * planarLength;
            if (horizontalSpeed <= 0.0001f)
            {
                return false;
            }

            var travelTime = horizontalDistance / horizontalSpeed;
            aimY = aimOrigin.y + projectileSpeed * normalizedForward.y * travelTime - 0.5f * gravity * travelTime * travelTime;
            return true;
        }

        // 抛物线也必须遵守同一条水平射线，因此这里只反解俯仰角。
        // 速度和重力公式直接复用 BulletObj 的运行时规则，避免瞄准线和真正子弹出现两套轨迹。
        private static bool TryResolveParabolicAutoAimLaunchForward(
            Vector3 aimOrigin,
            Vector3 aimPoint,
            Vector3 horizontalForward,
            float projectileSpeed,
            float gravity,
            out Vector3 launchForward,
            out float flightTime)
        {
            launchForward = Vector3.zero;
            flightTime = 0.0f;
            if (projectileSpeed <= 0.0001f || !TryNormalizeHorizontal(horizontalForward, out var normalizedHorizontalForward))
            {
                return false;
            }

            var aimDelta = aimPoint - aimOrigin;
            var horizontalDistance = new Vector2(aimDelta.x, aimDelta.z).magnitude;
            if (horizontalDistance <= 0.0001f)
            {
                if (aimDelta.sqrMagnitude <= 0.0001f)
                {
                    return false;
                }

                launchForward = aimDelta.normalized;
                flightTime = aimDelta.magnitude / projectileSpeed;
                return true;
            }

            if (Mathf.Abs(gravity) <= 0.0001f)
            {
                launchForward = aimDelta.normalized;
                var horizontalSpeed = projectileSpeed * new Vector2(launchForward.x, launchForward.z).magnitude;
                if (horizontalSpeed <= 0.0001f)
                {
                    return false;
                }

                flightTime = horizontalDistance / horizontalSpeed;
                return true;
            }

            var verticalDelta = aimDelta.y;
            var speedSqr = projectileSpeed * projectileSpeed;
            var discriminant = speedSqr * speedSqr - gravity * (gravity * horizontalDistance * horizontalDistance + 2.0f * verticalDelta * speedSqr);
            if (discriminant < 0.0f)
            {
                return false;
            }

            var denominator = gravity * horizontalDistance;
            if (Mathf.Abs(denominator) <= 0.0001f)
            {
                return false;
            }

            var sqrtDiscriminant = Mathf.Sqrt(Mathf.Max(0.0f, discriminant));
            var tanTheta0 = (speedSqr - sqrtDiscriminant) / denominator;
            var tanTheta1 = (speedSqr + sqrtDiscriminant) / denominator;
            return TryPickParabolicAutoAimSolution(
                normalizedHorizontalForward,
                projectileSpeed,
                horizontalDistance,
                tanTheta0,
                tanTheta1,
                out launchForward,
                out flightTime);
        }

        private static bool TryPickParabolicAutoAimSolution(
            Vector3 horizontalForward,
            float projectileSpeed,
            float horizontalDistance,
            float tanTheta0,
            float tanTheta1,
            out Vector3 launchForward,
            out float flightTime)
        {
            launchForward = Vector3.zero;
            flightTime = 0.0f;

            var hasFirstCandidate = TryBuildParabolicAutoAimLaunchForward(
                horizontalForward,
                projectileSpeed,
                horizontalDistance,
                tanTheta0,
                out var firstLaunchForward,
                out var firstFlightTime);
            var hasSecondCandidate = TryBuildParabolicAutoAimLaunchForward(
                horizontalForward,
                projectileSpeed,
                horizontalDistance,
                tanTheta1,
                out var secondLaunchForward,
                out var secondFlightTime);

            if (!hasFirstCandidate && !hasSecondCandidate)
            {
                return false;
            }

            if (!hasSecondCandidate ||
                (hasFirstCandidate &&
                 (Mathf.Abs(firstLaunchForward.y) < Mathf.Abs(secondLaunchForward.y) - 0.0001f ||
                  (Mathf.Abs(Mathf.Abs(firstLaunchForward.y) - Mathf.Abs(secondLaunchForward.y)) <= 0.0001f &&
                   firstFlightTime <= secondFlightTime))))
            {
                launchForward = firstLaunchForward;
                flightTime = firstFlightTime;
                return true;
            }

            launchForward = secondLaunchForward;
            flightTime = secondFlightTime;
            return true;
        }

        private static bool TryBuildParabolicAutoAimLaunchForward(
            Vector3 horizontalForward,
            float projectileSpeed,
            float horizontalDistance,
            float tanTheta,
            out Vector3 launchForward,
            out float flightTime)
        {
            launchForward = Vector3.zero;
            flightTime = 0.0f;
            if (float.IsNaN(tanTheta) || float.IsInfinity(tanTheta))
            {
                return false;
            }

            var horizontalSpeedScale = 1.0f / Mathf.Sqrt(1.0f + tanTheta * tanTheta);
            if (horizontalSpeedScale <= 0.0001f)
            {
                return false;
            }

            var verticalSpeedScale = tanTheta * horizontalSpeedScale;
            launchForward = (horizontalForward * horizontalSpeedScale + Vector3.up * verticalSpeedScale).normalized;
            var horizontalSpeed = projectileSpeed * horizontalSpeedScale;
            if (horizontalSpeed <= 0.0001f)
            {
                return false;
            }

            flightTime = horizontalDistance / horizontalSpeed;
            return flightTime >= 0.0f;
        }

        private static bool TryNormalizeHorizontal(Vector3 dir, out Vector3 normalizedHorizontalDir)
        {
            normalizedHorizontalDir = new Vector3(dir.x, 0.0f, dir.z);
            if (normalizedHorizontalDir.sqrMagnitude <= 0.0001f)
            {
                normalizedHorizontalDir = Vector3.zero;
                return false;
            }

            normalizedHorizontalDir.Normalize();
            return true;
        }

        public Vector3 UpdateBoneAimForwardBySeat(int seatId, Vector3 faceForward)
        {
            var hero = m_BattleSpawer != null ? m_BattleSpawer.ReadGuardHeroBySeat(seatId) : null;
            if (!BattleManager.ReadIsEntityValide(hero))
            {
                return ReadManualAimForwardBySeat(seatId);
            }

            var horizontalForward = new Vector3(faceForward.x, 0f, faceForward.z);
            if (horizontalForward.sqrMagnitude <= 0.0001f)
            {
                return ReadManualAimForwardBySeat(seatId);
            }

            horizontalForward.Normalize();
            ExtractYawPitch(horizontalForward, out var yawDegrees, out _);
            var pitchOffsetDegrees = EnsureManualAimAngles(seatId, hero).y;
            m_ManualAimAngles[seatId] = new Vector2(yawDegrees, pitchOffsetDegrees);
            var aimForward = BuildForwardFromYawPitch(yawDegrees, ResolveManualAimPitchDegrees(hero, pitchOffsetDegrees));

            hero.SetForward(horizontalForward);
            hero.SetBaseForward(aimForward);
            return aimForward;
        }

        // 自动吸附只允许修正Y，不允许改写玩家当前的水平转向。
        // 因此这里缓存的只有俯仰变化值；当水平射线暂时失去吸附目标时，后续仍沿当前XZ继续使用这份变化值。
        private void TryUpdateControlledSeatAutoAimPitch(PropertyEntity attacker, Vector3 resolvedLaunchForward)
        {
            if (!IsTowerDefendAutoAimEnabled() ||
                !BattleManager.ReadIsEntityValide(attacker) ||
                resolvedLaunchForward.sqrMagnitude <= 0.0001f ||
                m_BattleSpawer == null)
            {
                return;
            }

            if (!TryReadGuardHeroSeatId(attacker, out var seatId))
            {
                return;
            }

            var currentAngles = EnsureManualAimAngles(seatId, attacker);
            ExtractYawPitch(resolvedLaunchForward, out _, out var pitchDegrees);
            currentAngles.y = pitchDegrees - ReadManualAimBasePitchDegrees(attacker);
            m_ManualAimAngles[seatId] = currentAngles;
            m_PostUpgradeChallengeAimRecoveringSeats.Remove(seatId);
        }

        private void RefreshControlledSeatAimPose(int seatId, PropertyEntity hero)
        {
            if (!BattleManager.ReadIsEntityValide(hero))
            {
                return;
            }

            // 这里每帧都按当前缓存的水平角与俯仰变化值回写一次表现方向。
            // 这样在“刚离开一个可吸附目标、但还没碰到下一个目标”的空窗期里，角色和开火点仍会保持上一份变化值，不会突然掉回基础姿态。
            var aimForward = ReadManualAimForwardBySeat(seatId);
            var horizontalForward = new Vector3(aimForward.x, 0f, aimForward.z);
            if (horizontalForward.sqrMagnitude > 0.0001f)
            {
                hero.SetForward(horizontalForward.normalized);
            }

            hero.SetBaseForward(aimForward);
        }

        private Vector2 EnsureManualAimAngles(int seatId, PropertyEntity hero)
        {
            const float defaultPitchOffsetDegrees = 0f;
            if (m_ManualAimAngles.TryGetValue(seatId, out var storedAngles))
            {
                if (!IsTowerDefendAutoAimEnabled() &&
                    !Mathf.Approximately(storedAngles.y, defaultPitchOffsetDegrees))
                {
                    storedAngles.y = defaultPitchOffsetDegrees;
                    m_ManualAimAngles[seatId] = storedAngles;
                }

                return storedAngles;
            }

            var initialForward = BattleManager.ReadIsEntityValide(hero)
                ? NormalizeForwardOrDefault(hero.ReadForward())
                : Vector3.forward;
            ExtractYawPitch(initialForward, out var initialYaw, out _);
            var initializedAngles = new Vector2(initialYaw, defaultPitchOffsetDegrees);
            m_ManualAimAngles[seatId] = initializedAngles;
            return initializedAngles;
        }

        // 棒棒糖结束后立即启动常规俯仰恢复。
        // 这条恢复不依赖普通怪吸附目标；如果恢复中重新吸到目标，再交给自动吸附链路。
        private void PreparePostUpgradeChallengeAimRecovery()
        {
            if (m_BattleSpawer == null)
            {
                return;
            }

            for (int seatId = 0; seatId < TowerDefendSeatLayout.MaxSupportedPlayerCount; seatId++)
            {
                var hero = m_BattleSpawer.ReadGuardHeroBySeat(seatId);
                if (!BattleManager.ReadIsEntityValide(hero))
                {
                    continue;
                }

                var angles = EnsureManualAimAngles(seatId, hero);
                m_GuardHeroAutoAimStates.Remove(seatId);
                if (Mathf.Abs(angles.y) <= 0.0001f)
                {
                    angles.y = 0.0f;
                    m_ManualAimAngles[seatId] = angles;
                    continue;
                }

                m_PostUpgradeChallengeAimRecoveringSeats.Add(seatId);
                RefreshControlledSeatAimPose(seatId, hero);
            }
        }

        private void UpdatePostUpgradeChallengeAimRecovery(float dt)
        {
            if (m_PostUpgradeChallengeAimRecoveringSeats.Count <= 0 || m_BattleSpawer == null)
            {
                return;
            }

            BattleConst.ClampTowerDefendAutoAimValues();
            var recoverSpeed = BattleConst.TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed > 0.0f
                ? BattleConst.TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed
                : BattleConst.TowerDefendAutoAimSwitchSmoothSpeed;
            var maxStep = recoverSpeed > 0.0f
                ? recoverSpeed * Mathf.Max(0.0f, dt)
                : float.MaxValue;

            m_PostUpgradeChallengeAimRecoveryFinishedSeats.Clear();
            foreach (var seatId in m_PostUpgradeChallengeAimRecoveringSeats)
            {
                var hero = m_BattleSpawer.ReadGuardHeroBySeat(seatId);
                if (!BattleManager.ReadIsEntityValide(hero))
                {
                    m_PostUpgradeChallengeAimRecoveryFinishedSeats.Add(seatId);
                    continue;
                }

                var angles = EnsureManualAimAngles(seatId, hero);
                angles.y = Mathf.MoveTowards(angles.y, 0.0f, maxStep);
                if (Mathf.Abs(angles.y) <= 0.0001f)
                {
                    angles.y = 0.0f;
                    m_PostUpgradeChallengeAimRecoveryFinishedSeats.Add(seatId);
                }

                m_ManualAimAngles[seatId] = angles;
                RefreshControlledSeatAimPose(seatId, hero);
            }

            for (int i = 0; i < m_PostUpgradeChallengeAimRecoveryFinishedSeats.Count; i++)
            {
                m_PostUpgradeChallengeAimRecoveringSeats.Remove(m_PostUpgradeChallengeAimRecoveryFinishedSeats[i]);
            }

            m_PostUpgradeChallengeAimRecoveryFinishedSeats.Clear();
        }
        public override IBattleSpawer ReadBattleSpawer()
        {
            return m_BattleSpawer;
        }
        public override IBattleStatistical GetBattleStat()
        {
            return m_BattleStat;
        }
        // OnCreate: 战斗创建时的入口，负责：
        // - 从传入的 BattleData 读取关卡与战斗参数
        // - 初始化 ObjectManager、流程与刷怪器
        // - 准备玩家数据并标准化运行时状态
        // - 初始化统计器与基础数值（基地血量、经验上限等）
        public override void OnCreate(BattleData info)
        {
            Debug.Log("OnCreate(BattleData info)");
            base.OnCreate(info);
            m_Stage = info.m_Stage;
            m_BaseMaxHealth = Mathf.Max(1, info.m_BaseMaxHealth);
            m_BaseHealth = Mathf.Clamp(info.m_BaseHealth, 0, m_BaseMaxHealth);
            m_TeamExpCurrent = 0;
            m_IsUpgradeChallengeReady = false;
            m_IsUpgradeChallengeTargetPendingDestroy = false;
            m_RuntimeUpgradeChallengeCountdown = m_UpgradeChallengeCountdown;
            m_Phase = BattlePhase.NormalGame;
            m_PhaseTimer = 0f;
            ClearUpgradeChallengeMaxLevelEffects();
            m_ManualAimAngles.Clear();
            m_GuardHeroAutoAimStates.Clear();
            m_PostUpgradeChallengeAimRecoveringSeats.Clear();
            m_PostUpgradeChallengeAimRecoveryFinishedSeats.Clear();
            m_LastRuntimeDeltaTime = 1.0f / 30.0f;
            m_NpcSpeakIntroEntered = false;
            m_NpcSpeakIntroTimerStarted = false;
            m_NpcSpeakIntroTimeLeft = 0f;
            m_NpcSpeakIntroLoadTimeoutLeft = 0f;
            m_NpcSpeakIntroSoundId = 0;
            m_NpcSpeakIntroDurationSeconds = 0f;
            m_NpcSpeakIntroGoPlayed = false;
            m_NpcSpeakIntroEntity = null;
            m_FIghtId = info.m_FightId;
            m_Seed = info.m_Seed;
            m_ObjectManager = new ObjectManager();
            m_ObjectManager.Init();

            m_Process = new TowerDefendBattleProcess ();
            m_Process.Init();

            InitPlayers(info.m_Players);
            NormalizePlayerRuntimeData();
            m_TeamExpMax = ResolveTeamExpMaxForCurrentProgress();


            m_BattleSpawer = new TowerDefendBattleSpawer();
            m_BattleSpawer.OnCreate(this);

            m_BattleStat = new TowerDefendBattleStatistical();
            m_BattleStat.Init();
            NotifyBaseHealthChanged();

            Debug.Log("OnCreate(BattleData info) finish");
        }
        // ConfigureScenePointData: 注入由场景解析得到的点位数据。
        // 点位包括怪物出生点、玩家出生点与基地位置；注入后刷怪器和战斗逻辑会使用这些坐标。
        public void ConfigureScenePointData(TowerDefendScenePointData pointData)
        {
            m_MonsterSpawnPoints.Clear();
            m_MonsterPathPoints.Clear();
            m_GuardHeroSpawnPoints.Clear();
            m_NpcSpeakPoint = pointData != null ? pointData.m_NpcSpeakPoint : null;

            if (pointData == null)
            {
                return;
            }

            m_UpgradeChallengeFoot = pointData.m_UpgradeChallengeFoot;

            if (pointData.m_MonsterSpawnPoints != null)
            {
                m_MonsterSpawnPoints.AddRange(pointData.m_MonsterSpawnPoints);
            }

            if (pointData.m_MonsterPathPoints != null)
            {
                m_MonsterPathPoints.AddRange(pointData.m_MonsterPathPoints);
            }
            else if (pointData.m_MonsterSpawnPoints != null)
            {
                int spawnPointCount = pointData.m_MonsterSpawnPoints.Count;
                for (int i = 0; i < spawnPointCount; i++)
                {
                    var pathPoint = new TowerDefendMonsterPathPointData();
                    pathPoint.m_SpawnPoint = pointData.m_MonsterSpawnPoints[i];
                    m_MonsterPathPoints.Add(pathPoint);
                }
            }

            if (pointData.m_GuardHeroSpawnPoints != null)
            {
                m_GuardHeroSpawnPoints.AddRange(pointData.m_GuardHeroSpawnPoints);
            }

            m_BasePoint = pointData.m_BasePoint;
            m_BaseReachRadius = pointData.m_BaseReachRadius > 0f ? pointData.m_BaseReachRadius : 1.5f;
        }

        public IReadOnlyList<Vector3> ReadMonsterSpawnPoints()
        {
            return m_MonsterSpawnPoints;
        }

        public IReadOnlyList<TowerDefendMonsterPathPointData> ReadMonsterPathPoints()
        {
            return m_MonsterPathPoints;
        }

        public IReadOnlyList<Vector3> ReadGuardHeroSpawnPoints()
        {
            return m_GuardHeroSpawnPoints;
        }

        public Vector3 ReadBasePoint()
        {
            return m_BasePoint;
        }

        public float ReadBaseReachRadius()
        {
            return m_BaseReachRadius;
        }

        public int ReadCurrentWave()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadWave() : 0;
        }

        public BattleGameMode ReadGameMode()
        {
            return m_BattleData != null ? m_BattleData.m_GameMode : BattleGameMode.Chapter;
        }

        private bool TryResolveNpcSpeakIntroRequest(out NpcSpeakIntroRequest request, out string reason)
        {
            request = default;

            if (m_NpcSpeakPoint == null)
            {
                reason = "boss_point missing";
                return false;
            }

            var chapterStageId = ReadGameMode() == BattleGameMode.Endless ? 1 : GetStage();
            var chapterCfg = t_chapterStageBean.GetConfig(chapterStageId, false);
            if (chapterCfg == null)
            {
                reason = "chapter stage config missing";
                return false;
            }

            if (chapterCfg.t_speak == null || chapterCfg.t_speak.Count < 2)
            {
                reason = "t_speak invalid";
                return false;
            }

            if (chapterCfg.t_first_wave_delay_ms <= 0)
            {
                reason = "t_first_wave_delay_ms invalid";
                return false;
            }

            request.MonsterConfigId = (int)chapterCfg.t_speak[0];
            request.SoundId = (int)chapterCfg.t_speak[1];
            request.DurationSeconds = chapterCfg.t_first_wave_delay_ms / 1000.0f;
            reason = null;
            return true;
        }

        public long ReadTeamExpCurrent()
        {
            return m_TeamExpCurrent;
        }

        public long ReadTeamExpMax()
        {
            return m_TeamExpMax;
        }

        public int ReadTeamExpPercent()
        {
            if (m_TeamExpMax <= 0)
            {
                return 0;
            }

            return Mathf.Clamp((int)(m_TeamExpCurrent * 10000 / m_TeamExpMax), 0, 10000);
        }
        private TowerDefendBattleProcess ReadTowerDefendBattleProcess()
        {
            return m_Process as TowerDefendBattleProcess;
        }

        public TowerDefendBattleState ReadBattleState()
        {
            var process = ReadTowerDefendBattleProcess();
            return process != null ? (TowerDefendBattleState)process.GetState() : TowerDefendBattleState.WaitingChessMan;
        }

        public bool ReadIsPreparePhase()
        {
            return false;
        }

        public bool ReadIsBattleRunning()
        {
            var state = ReadBattleState();
            return state == TowerDefendBattleState.FreeGame ||
                state == TowerDefendBattleState.UpgradeChallengeCountdown ||
                state == TowerDefendBattleState.UpgradeChallenge;
        }

        public bool ReadIsFinished()
        {
            var state = ReadBattleState();
            return state == TowerDefendBattleState.ShowWinFailed ||
                state == TowerDefendBattleState.ShowReward ||
                state == TowerDefendBattleState.GameOver;
        }
        public bool ReadIsBattleStartLoadingReady()
        {
            return m_BattleSpawer == null || m_BattleSpawer.ReadIsMonsterPreloadFinished();
        }

        public float ReadPrepareLeft()
        {
            var process = ReadTowerDefendBattleProcess();
            return process != null ? process.ReadPrepareLeft() : 0f;
        }

        public float ReadUpgradeChallengeCountdownLeft()
        {
            if (m_Phase != BattlePhase.ChallengeCountdown)
            {
                return 0f;
            }

            var process = ReadTowerDefendBattleProcess();
            return process != null ? process.ReadUpgradeChallengeCountdownLeft() : 0f;
        }

        public float ReadUpgradeChallengeLeft()
        {
            var process = ReadTowerDefendBattleProcess();
            return process != null ? process.ReadUpgradeChallengeLeft() : 0f;
        }
        public int ReadMonsterKillRewardExp()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadMonsterKillRewardExp() : 0;
        }

        public int ReadMonsterKillRewardCoin()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadMonsterKillRewardCoin() : 0;
        }

        // 标记：团队经验达到上限，且存在可参与升级挑战的玩家。
        public bool ReadIsUpgradeChallengeReady()
        {
            return m_IsUpgradeChallengeReady;
        }

        private void SetPhase(BattlePhase newPhase)
        {
            m_Phase = newPhase;
            m_PhaseTimer = 0f;
        }

        public BattlePhase ReadPhase()
        {
            return m_Phase;
        }

        // 标记：升级挑战已进入开打前流程（镜头等待或倒计时），尚未真正进入可攻击阶段。
        public bool ReadIsUpgradeChallengePreActive()
        {
            return m_Phase == BattlePhase.ChallengeEntryWait || m_Phase == BattlePhase.ChallengeCountdown;
        }

        // 标记：升级挑战数字倒计时中（3、2、1）。
        public bool ReadIsUpgradeChallengeCountdown()
        {
            return m_Phase == BattlePhase.ChallengeCountdown;
        }

        // 标记：升级挑战活动正在进行中。
        public bool ReadIsUpgradeChallengeActive()
        {
            return m_Phase == BattlePhase.ChallengeActive;
        }

        public bool TryReadNpcSpeakPointZ(out float npcSpeakPointZ)
        {
            npcSpeakPointZ = 0f;
            if (m_NpcSpeakPoint == null)
            {
                return false;
            }

            npcSpeakPointZ = m_NpcSpeakPoint.position.z;
            return true;
        }

        public bool TryReadNpcSpeakIntroAimEntity(out TowerDefendNpcSpeakEntity entity)
        {
            entity = m_NpcSpeakIntroEntity;
            return ReadBattleState() == TowerDefendBattleState.NpcSpeakIntro &&
                entity != null &&
                !entity.ReadIsDestroy() &&
                entity.ReadVisiable();
        }

        // 无目标时，瞄准线最远显示到当前战斗关注物所在的 Z 平面前方一段距离。
        // 棒棒糖进入前置阶段或可攻击阶段后，优先限制到棒棒糖中心；
        // 棒棒糖结束后立即回退到喊话点，避免瞄准线继续停留在棒棒糖后方。
        public bool TryReadNoTargetAimClampPointZ(out float clampPointZ)
        {
            clampPointZ = 0f;
            BattleConst.ClampTowerDefendAutoAimValues();
            var forwardOffset = BattleConst.TowerDefendNoTargetAimClampForwardOffset;
            if ((ReadIsUpgradeChallengePreActive() || ReadIsUpgradeChallengeActive()) &&
                BattleManager.ReadIsEntityValide(m_UpgradeChallengeTarget))
            {
                clampPointZ = m_UpgradeChallengeTarget.ReadCenterPosition().z + forwardOffset;
                return true;
            }

            if (!TryReadNpcSpeakPointZ(out clampPointZ))
            {
                return false;
            }

            clampPointZ += forwardOffset;
            return true;
        }

        public bool ReadHasPendingMonsterRestore()
        {
            return m_Phase == BattlePhase.ChallengeFinish;
        }

        // 升级挑战结束后，需要等棒棒糖的回缩或爆裂真正收尾完成，
        // 才能恢复普通战斗流程，否则会出现目标还在场上时已经开始出怪的问题。
        public bool CanCompleteUpgradeChallengeFinish()
        {
            return m_Phase == BattlePhase.ChallengeFinish &&
                !m_IsUpgradeChallengeTargetPendingDestroy;
        }

        public void GM_AddTeamExpToMax()
        {
            AddTeamExp(m_TeamExpMax);
        }

        public bool GM_TryForceUpgradeChallengeShatter()
        {
            var process = ReadTowerDefendBattleProcess();
            return process != null && process.GM_TryForceUpgradeChallengeFinish();
        }

        public bool GM_TrySpawnNextWaveNow()
        {
            return m_BattleSpawer != null && m_BattleSpawer.GM_TrySpawnNextWaveNow();
        }

        public bool GM_TrySpawnBossNow()
        {
            return m_BattleSpawer != null && m_BattleSpawer.GM_TrySpawnBossNow();
        }

        public void GM_ForceBaseHealthToZero()
        {
            if (m_BaseHealth <= 0)
            {
                return;
            }

            m_BaseHealth = 0;
            NotifyBaseHealthChanged();
        }

        public void GM_ForceFinishUpgradeChallenge()
        {
            FinishUpgradeChallenge();
        }

        public float ReadUpgradeChallengeEntryDelayLeft()
        {
            return m_Phase == BattlePhase.ChallengeEntryWait ? m_PhaseTimer : 0f;
        }

        public float ReadUpgradeChallengeCountdownDuration()
        {
            return m_RuntimeUpgradeChallengeCountdown > 0.01f
                ? m_RuntimeUpgradeChallengeCountdown
                : m_UpgradeChallengeCountdown;
        }

        public void SetUpgradeChallengeCountdownDuration(float duration)
        {
            m_RuntimeUpgradeChallengeCountdown = duration > 0.01f
                ? duration
                : m_UpgradeChallengeCountdown;
        }

        public void TryRestoreMonsters()
        {
            if (!CanCompleteUpgradeChallengeFinish())
            {
                return;
            }

            PreparePostUpgradeChallengeAimRecovery();
            SetPhase(BattlePhase.NormalGame);
            if (m_BattleSpawer != null)
            {
                m_BattleSpawer.SetMonsterPresentation(true, true, false);
            }

            var process = ReadTowerDefendBattleProcess();
            if (process != null)
            {
                process.CompleteUpgradeChallengeRestore();
            }
        }

        // 判定是否可以开始升级挑战：已就绪，且不在倒计时或进行中。
        public bool CanStartUpgradeChallenge()
        {
            return m_IsUpgradeChallengeReady && m_Phase == BattlePhase.NormalGame;
        }

        // 判断玩家是否有足够能量在队列中释放主动技能（并且不被升级挑战阻塞）。
        public bool CanPlayerQueueActiveSkill(long playerId)
        {
            if (playerId <= 0 || IsUpgradeChallengeCombatBlocked())
            {
                return false;
            }

            var player = GetPlayer(playerId);
            if (player == null)
            {
                return false;
            }

            return player.m_MagicPercent >= m_SkillEnergyCostPermille;
        }

        public bool RequiresPlayerSkillEnergy(PropertyEntity heroEntity, Skill skill)
        {
            if (heroEntity == null || skill == null || skill.ReadSlot() <= 0)
            {
                return false;
            }

            var playerHero = heroEntity as PlayerHero;
            if (playerHero == null)
            {
                return true;
            }

            return skill.ReadSlot() != playerHero.ReadTowerDefendAutoSkillSlot();
        }

        // 增加团队经验并检查是否触发升级挑战就绪条件。
        // 奖励值直接来自当前关卡配置，不再按玩家人数额外放大。
        public void AddTeamExp(long rewardExp)
        {
            if (rewardExp <= 0 || m_IsUpgradeChallengeReady || IsUpgradeChallengeCombatBlocked() || !HasUpgradeablePlayer())
            {
                return;
            }

            m_TeamExpCurrent = Math.Min(m_TeamExpCurrent + rewardExp, m_TeamExpMax);

            if (m_TeamExpCurrent >= m_TeamExpMax && HasUpgradeablePlayer())
            {
                m_IsUpgradeChallengeReady = true;
            }

            RenderEvent.Event.OnAddTeamExp();
        }

        // 启动升级挑战的倒计时流程：准备参与列表，冻结并隐藏场上怪物，同时清理控制效果。
        public void StartUpgradeChallengeCountdown()
        {
            if (m_Phase != BattlePhase.NormalGame)
            {
                return;
            }

            m_IsUpgradeChallengeReady = false;
            m_IsUpgradeChallengeCameraReady = false;
            m_RuntimeUpgradeChallengeCountdown = m_UpgradeChallengeCountdown;
            m_PostUpgradeChallengeAimRecoveringSeats.Clear();
            m_PostUpgradeChallengeAimRecoveryFinishedSeats.Clear();
            SetPhase(BattlePhase.ChallengeEntryWait);
            m_PhaseTimer = 1.0f;
            PrepareUpgradeChallengeParticipants();
            CreateUpgradeChallengeTarget();
            if (m_BattleSpawer != null)
            {
                m_BattleSpawer.SetMonsterPresentation(false, false, true);
            }
            ClearGuardHeroControlEffectsForUpgradeChallenge();
        }

        private void CreateUpgradeChallengeTarget()
        {
            var objMgr = BattleManager.GetObjectManager();
            if (objMgr == null)
            {
                return;
            }

            var target = (UpgradeChallengeTarget)objMgr.NewCreature(
                emEntityType.em_EntityType_UpgradeChallengeTarget);
            if (target == null)
            {
                return;
            }

            target.SetId(objMgr.AssignClientId());
            target.SetGroup(GroupId.NeutralGroup);
            target.SetupPlacement(m_UpgradeChallengeFoot);
            target.InitInstance();
            target.SetChallengeHealth(ResolveUpgradeChallengeMaxHealth(m_UpgradeChallengeScores.Count));
            objMgr.AddPropertyEntity(target);
            m_UpgradeChallengeTarget = target;
            m_IsUpgradeChallengeTargetPendingDestroy = false;
        }

        public void EnterUpgradeChallenge()
        {
            if (m_Phase != BattlePhase.ChallengeEntryWait && m_Phase != BattlePhase.ChallengeCountdown)
            {
                return;
            }

            SetPhase(BattlePhase.ChallengeActive);
        }

        public void NotifyUpgradeChallengeCameraReady()
        {
            if (m_Phase == BattlePhase.ChallengeEntryWait)
            {
                m_IsUpgradeChallengeCameraReady = true;
            }
        }

        public void AddUpgradeChallengeScore(long playerId, int score)
        {
            if (m_Phase != BattlePhase.ChallengeActive || score <= 0)
            {
                return;
            }

            if (!m_UpgradeChallengeScores.ContainsKey(playerId))
            {
                return;
            }

            m_UpgradeChallengeScores[playerId] += score;
        }

        public bool CanAddUpgradeChallengeScore(long playerId)
        {
            return m_Phase == BattlePhase.ChallengeActive && m_UpgradeChallengeScores.ContainsKey(playerId);
        }

        public bool CanAddUpgradeChallengeScoreBySeat(int seatId)
        {
            var playerId = ReadBattlePlayerIdBySeat(seatId);
            if (playerId <= 0)
            {
                return false;
            }

            return CanAddUpgradeChallengeScore(playerId);
        }

        public int GetUpgradeChallengeScore(long playerId)
        {
            return ReadUpgradeChallengeScore(playerId);
        }

        // 升级挑战的分数和血量都以真实命中点和真实命中子弹为准。
        // 棒棒糖自身只返回命中环值，最终显示分值和实际扣血统一使用“环值 × 子弹 t_bbt_damage”。
        public bool TryHandleUpgradeChallengeHit(PropertyEntity attacker, int bulletBbtDamage, Vector3 hitPoint, out bool targetDefeated)
        {
            targetDefeated = false;
            if (m_Phase != BattlePhase.ChallengeActive ||
                m_UpgradeChallengeTarget == null ||
                !BattleManager.ReadIsEntityValide(attacker))
            {
                return false;
            }

            long playerId = attacker.ReadBattlePlayerId();
            if (!CanAddUpgradeChallengeScore(playerId))
            {
                return false;
            }

            if (!m_UpgradeChallengeTarget.TryResolveHitRingValue(hitPoint, out var ringValue) || ringValue <= 0)
            {
                return false;
            }

            int baseDamage = Mathf.Max(0, bulletBbtDamage);
            int finalValue = ringValue * baseDamage;
            if (finalValue <= 0)
            {
                //Debug.Log(string.Format(
                //    "[升级挑战] 玩家 {0} 命中 {1} 环，但子弹 t_bbt_damage={2}，本次不结算分数和伤害",
                //    playerId,
                //    ringValue,
                //    bulletBbtDamage));
                return false;
            }

            AddUpgradeChallengeScore(playerId, finalValue);
            targetDefeated = m_UpgradeChallengeTarget.ApplyChallengeDamage(finalValue);
            //Debug.Log(string.Format(
            //    "[升级挑战] 玩家 {0} 命中 {1} 环，子弹基础伤害 {2}，本次结算 {3}，当前总 {4} 分，棒棒糖血量 {5}/{6}",
            //    playerId,
            //    ringValue,
            //    baseDamage,
            //    finalValue,
            //    m_UpgradeChallengeScores[playerId],
            //    m_UpgradeChallengeTarget.ReadCurrentHealth(),
            //    m_UpgradeChallengeTarget.ReadMaxHealth()));
            return true;
        }

        public long ReadBattlePlayerIdBySeat(int seatId)
        {
            int playerCount = m_BattlePlayers.Count;
            for (int i = 0; i < playerCount; i++)
            {
                var player = m_BattlePlayers[i];
                if (player != null && player.m_SeatId == seatId)
                {
                    return player.m_ID;
                }
            }

            return 0;
        }

        public BattlePlayer ReadPlayerBySeat(int seatId)
        {
            int playerCount = m_BattlePlayers.Count;
            for (int i = 0; i < playerCount; i++)
            {
                var player = m_BattlePlayers[i];
                if (player != null && player.m_SeatId == seatId)
                {
                    return player;
                }
            }

            return null;
        }

        public void ResolveUpgradeChallengeResult()
        {
            bool targetDefeated = m_UpgradeChallengeTarget != null && m_UpgradeChallengeTarget.ReadIsDefeated();
            int winnerScore = int.MinValue;

            foreach (var kv in m_UpgradeChallengeScores)
            {
                if (kv.Value > winnerScore)
                {
                    winnerScore = kv.Value;
                }
            }

            var winnerPlayerId = ResolveUpgradeChallengeWinnerPlayerId(winnerScore);
            if (targetDefeated && winnerPlayerId > 0)
            {
                ApplyUpgradeToPlayer(winnerPlayerId);
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("[升级挑战] 结束 — 各玩家得分：");
            foreach (var kv in m_UpgradeChallengeScores)
            {
                var player = GetPlayer(kv.Key);
                var name = player != null ? player.m_Name : "未知";
                sb.Append(string.Format("{0}({1}分) ", name, kv.Value));
            }
            if (targetDefeated && winnerPlayerId > 0)
            {
                var winner = GetPlayer(winnerPlayerId);
                var winnerName = winner != null ? winner.m_Name : "未知";
                sb.Append(string.Format("| 胜者：{0} → 等级 {1}", winnerName, winner != null ? winner.m_RoleLevel : 0));
            }
            else
            {
                sb.Append(targetDefeated ? "| 无胜者" : "| 棒棒糖未被击败，本次无人升级");
            }
            Debug.Log(sb.ToString());

            FinishUpgradeChallenge();
        }

        private void PrepareUpgradeChallengeParticipants()
        {
            m_UpgradeChallengeScores.Clear();
            int playerCount = m_BattlePlayers.Count;
            for (int i = 0; i < playerCount; i++)
            {
                var player = m_BattlePlayers[i];
                if (IsPlayerEligibleForUpgradeChallenge(player))
                {
                    m_UpgradeChallengeScores[player.m_ID] = 0;
                }
            }
        }

        public void OnPlayerSkillCast(PropertyEntity attacker, Skill skill)
        {
            if (attacker == null || skill == null)
            {
                return;
            }

            var playerId = attacker.ReadBattlePlayerId();
            if (playerId <= 0)
            {
                return;
            }

            if (RequiresPlayerSkillEnergy(attacker, skill))
            {
                ConsumePlayerSkillEnergy(playerId);
            }
        }

        private void FinishUpgradeChallenge()
        {
            m_IsUpgradeChallengeReady = false;
            m_IsUpgradeChallengeCameraReady = false;
            m_RuntimeUpgradeChallengeCountdown = m_UpgradeChallengeCountdown;
            m_TeamExpCurrent = 0;
            m_TeamExpMax = ResolveTeamExpMaxForCurrentProgress();
            m_UpgradeChallengeScores.Clear();
            m_IsUpgradeChallengeTargetPendingDestroy = false;
            ClearUpgradeChallengeMaxLevelEffects();
            if (m_UpgradeChallengeTarget != null)
            {
                if (m_UpgradeChallengeTarget.ReadIsFinalShatterTriggered())
                {
                    m_IsUpgradeChallengeTargetPendingDestroy = true;
                }
                else if (!m_UpgradeChallengeTarget.ReadIsDefeated() &&
                    m_UpgradeChallengeTarget.TryStartTimeoutRetract())
                {
                    m_IsUpgradeChallengeTargetPendingDestroy = true;
                }
                else if (m_UpgradeChallengeTarget.TryTriggerChallengeTimeoutShatter())
                {
                    m_IsUpgradeChallengeTargetPendingDestroy = true;
                }
                else
                {
                    DestroyUpgradeChallengeTarget();
                }
            }
            PreparePostUpgradeChallengeAimRecovery();
            SetPhase(BattlePhase.ChallengeFinish);
        }

        private void DestroyUpgradeChallengeTarget()
        {
            m_IsUpgradeChallengeTargetPendingDestroy = false;
            if (m_UpgradeChallengeTarget != null)
            {
                var objMgr = BattleManager.GetObjectManager();
                if (objMgr != null)
                {
                    objMgr.RemovePropertyEntity(m_UpgradeChallengeTarget, true);
                }
                m_UpgradeChallengeTarget = null;
            }
        }

        private void UpdateUpgradeChallengeTargetDestroyState()
        {
            if (!m_IsUpgradeChallengeTargetPendingDestroy || m_UpgradeChallengeTarget == null)
            {
                return;
            }

            if (!m_UpgradeChallengeTarget.ReadCanDestroyAfterFinalShatter())
            {
                return;
            }

            DestroyUpgradeChallengeTarget();
        }

        private bool IsPlayerEligibleForUpgradeChallenge(BattlePlayer player)
        {
            if (player == null)
            {
                return false;
            }

            return player.m_RoleLevel < m_TowerDefendMaxRoleLevel;
        }

        private int ReadUpgradeChallengeScore(long playerId)
        {
            int score;
            return m_UpgradeChallengeScores.TryGetValue(playerId, out score) ? score : 0;
        }

        public int ReadUpgradeChallengeScoreBySeat(int seatId)
        {
            var playerId = ReadBattlePlayerIdBySeat(seatId);
            return playerId > 0 ? ReadUpgradeChallengeScore(playerId) : 0;
        }

        private void NormalizePlayerRuntimeData()
        {
            m_BattlePlayers.Sort((left, right) =>
            {
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                if (left == null)
                {
                    return 1;
                }

                if (right == null)
                {
                    return -1;
                }

                return left.m_SeatId.CompareTo(right.m_SeatId);
            });

            int playerCount = m_BattlePlayers.Count;
            int firstAvailableSeatId = TowerDefendSeatLayout.DefaultControlledSeatId;
            bool hasAvailableSeat = false;
            for (int i = 0; i < playerCount; i++)
            {
                var player = m_BattlePlayers[i];
                if (player == null)
                {
                    continue;
                }

                player.m_MagicPercent = Mathf.Clamp(player.m_MagicPercent, 0, m_SkillEnergyFullPermille);
                player.m_RoleLevel = Mathf.Clamp(player.m_RoleLevel, 1, m_TowerDefendMaxRoleLevel);

                if (!hasAvailableSeat)
                {
                    firstAvailableSeatId = player.m_SeatId;
                    hasAvailableSeat = true;
                }
            }

            if (!hasAvailableSeat)
            {
                m_ControlledSeatId = TowerDefendSeatLayout.DefaultControlledSeatId;
                return;
            }

            if (ReadPlayerBySeat(m_ControlledSeatId) == null)
            {
                m_ControlledSeatId = firstAvailableSeatId;
            }
        }

        private void AddPlayerSkillEnergy(long playerId, int gainPermille)
        {
            if (playerId <= 0 || gainPermille <= 0 || IsUpgradeChallengeCombatBlocked())
            {
                return;
            }

            var player = GetPlayer(playerId);
            if (player == null)
            {
                return;
            }

            var wasFull = IsPlayerSkillEnergyFull(player);
            player.m_MagicPercent = Mathf.Clamp(
                player.m_MagicPercent + gainPermille,
                0,
                m_SkillEnergyFullPermille);
            NotifyPlayerSkillEnergyFullStateChangedIfNeed(player, wasFull);
        }

        private void ConsumePlayerSkillEnergy(long playerId)
        {
            if (playerId <= 0)
            {
                return;
            }

            var player = GetPlayer(playerId);
            if (player == null)
            {
                return;
            }

            var wasFull = IsPlayerSkillEnergyFull(player);
            player.m_MagicPercent = Mathf.Clamp(player.m_MagicPercent - m_SkillEnergyCostPermille, 0, m_SkillEnergyFullPermille);
            NotifyPlayerSkillEnergyFullStateChangedIfNeed(player, wasFull);
        }

        private void UpdatePlayerSkillEnergy(float dt)
        {
            if (dt <= 0 || IsUpgradeChallengeCombatBlocked())
            {
                return;
            }

            int playerCount = m_BattlePlayers.Count;
            for (int i = 0; i < playerCount; i++)
            {
                var player = m_BattlePlayers[i];
                if (player == null || player.m_MagicPercent >= m_SkillEnergyFullPermille)
                {
                    continue;
                }

                var heroEntity = FindGuardHeroEntity(player.m_ID);
                if (heroEntity == null)
                {
                    continue;
                }

                bool hasActiveSkill;
                if (!CanRechargePlayerSkillEnergy(heroEntity, out hasActiveSkill))
                {
                    if (!hasActiveSkill)
                    {
                        var wasFull = IsPlayerSkillEnergyFull(player);
                        player.m_MagicPercent = m_SkillEnergyFullPermille;
                        NotifyPlayerSkillEnergyFullStateChangedIfNeed(player, wasFull);
                    }
                    continue;
                }

                var rechargePermille = Mathf.RoundToInt(m_SkillEnergyRechargePermillePerSecond * dt);
                if (rechargePermille <= 0)
                {
                    rechargePermille = 1;
                }

                AddPlayerSkillEnergy(player.m_ID, rechargePermille);
            }
        }

        private bool IsPlayerSkillEnergyFull(BattlePlayer player)
        {
            return player != null && player.m_MagicPercent >= m_SkillEnergyFullPermille;
        }

        private void NotifyPlayerSkillEnergyFullStateChangedIfNeed(BattlePlayer player, bool wasFull)
        {
            if (player == null || wasFull == IsPlayerSkillEnergyFull(player))
            {
                return;
            }

            RenderEvent.Event.OnTowerDefendPlayerSkillEnergyFullStateChanged(player.m_SeatId);
        }

        private bool ReadIsUpgradeChallengeParticipant(long playerId)
        {
            return m_UpgradeChallengeScores.ContainsKey(playerId);
        }

        public bool ReadIsUpgradeChallengeParticipantBySeat(int seatId)
        {
            var playerId = ReadBattlePlayerIdBySeat(seatId);
            return playerId > 0 && ReadIsUpgradeChallengeParticipant(playerId);
        }

        private long ResolveUpgradeChallengeWinnerPlayerId(int winnerScore)
        {
            if (winnerScore == int.MinValue || m_UpgradeChallengeScores.Count == 0)
            {
                return 0;
            }

            long winnerPlayerId = 0;
            int bestSeatId = int.MaxValue;

            foreach (var kv in m_UpgradeChallengeScores)
            {
                if (kv.Value != winnerScore)
                {
                    continue;
                }

                var player = GetPlayer(kv.Key);
                if (!IsPlayerEligibleForUpgradeChallenge(player))
                {
                    continue;
                }

                var seatId = player != null ? player.m_SeatId : int.MaxValue;
                if (seatId < bestSeatId)
                {
                    winnerPlayerId = kv.Key;
                    bestSeatId = seatId;
                }
            }

            return winnerPlayerId;
        }

        public int ReadAliveMonsterCount()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadMonsterCount() : 0;
        }

        public float ReadWaveWait()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadWaveWait() : 0;
        }

        public int ReadMaxWave()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadMaxWave() : 0;
        }

        public bool ReadIsBossWave()
        {
            return m_BattleSpawer != null && m_BattleSpawer.ReadIsBossWave();
        }

        public bool ReadIsEliteWave()
        {
            return m_BattleSpawer != null && m_BattleSpawer.ReadIsEliteWave();
        }

        public int ReadRemainingSpecialSpawnCount()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadRemainingSpecialSpawnCount() : 0;
        }

        public int ReadCurrentMonsterPoolId()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadCurrentMonsterPoolId() : 0;
        }

        public int ReadCurrentMonsterPoolStageId()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadCurrentMonsterPoolStageId() : 0;
        }

        public float ReadMonsterPoolSwitchLeft()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadMonsterPoolSwitchLeft() : 0;
        }

        public bool ReadIsBossSkillCasting()
        {
            return m_BattleSpawer != null && m_BattleSpawer.ReadIsBossSkillCasting();
        }

        public float ReadBossSkillCastingLeft()
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadBossSkillCastingLeft() : 0;
        }

        public PropertyEntity ReadGuardHeroBySeat(int seatId)
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadGuardHeroBySeat(seatId) : null;
        }

        public int ReadPlayerLevelBySeat(int seatId)
        {
            var heroEntity = ReadGuardHeroBySeat(seatId);
            if (heroEntity != null)
            {
                return Mathf.Max(1, heroEntity.ReadLevel());
            }

            var player = ReadPlayerBySeat(seatId);
            return player != null ? Mathf.Max(1, player.m_RoleLevel) : 0;
        }

        public int ReadPlayerHpPercentBySeat(int seatId)
        {
            var heroEntity = ReadGuardHeroBySeat(seatId);
            if (heroEntity != null)
            {
                var maxHp = heroEntity.GetMaxHP();
                var hp = heroEntity.ReadHP();
                return maxHp > 0 ? Mathf.Clamp((int)(hp * 10000 / maxHp), 0, 10000) : 0;
            }

            var player = ReadPlayerBySeat(seatId);
            return player != null ? Mathf.Clamp(player.m_HPPercent, 0, 10000) : 0;
        }

        public bool ReadPlayerCanJoinUpgradeChallengeBySeat(int seatId)
        {
            var heroEntity = ReadGuardHeroBySeat(seatId);
            if (heroEntity != null)
            {
                return heroEntity.ReadLevel() < m_TowerDefendMaxRoleLevel;
            }

            var player = ReadPlayerBySeat(seatId);
            return player != null && player.m_RoleLevel < m_TowerDefendMaxRoleLevel;
        }

        public int ReadPlayerSkillEnergyPercentBySeat(int seatId)
        {
            var player = ReadPlayerBySeat(seatId);
            return player != null ? Mathf.Clamp(player.m_MagicPercent, 0, m_SkillEnergyFullPermille) : 0;
        }

        public Skill ReadPrimaryManualSkillBySeat(int seatId)
        {
            return ResolvePrimaryManualSkill(ReadGuardHeroBySeat(seatId));
        }

        public bool ReadPrimaryManualSkillReadyBySeat(int seatId)
        {
            var skill = ReadPrimaryManualSkillBySeat(seatId);
            if (skill == null)
            {
                return false;
            }

            var heroEntity = ReadGuardHeroBySeat(seatId);
            var playerId = ReadBattlePlayerIdBySeat(seatId);
            return skill.ReadIsCooldown() &&
                (!RequiresPlayerSkillEnergy(heroEntity, skill) || CanPlayerQueueActiveSkill(playerId));
        }

        private Skill ResolvePrimaryManualSkill(PropertyEntity heroEntity)
        {
            if (heroEntity == null)
            {
                return null;
            }

            var skillMgr = heroEntity.GetSkillManager();
            var skills = skillMgr != null ? skillMgr.ReadSkills() : null;
            if (skills == null || skills.Count <= 0)
            {
                return null;
            }

            int autoSkillSlot = ResolvePlayerAutoSkillSlot(heroEntity);
            int skillCount = skills.Count;
            for (int i = 0; i < skillCount; i++)
            {
                var skill = skills[i];
                if (skill != null && skill.ReadSlot() > 0 && skill.ReadSlot() != autoSkillSlot)
                {
                    return skill;
                }
            }

            for (int i = 0; i < skillCount; i++)
            {
                var skill = skills[i];
                if (skill != null && skill.ReadSlot() > 0)
                {
                    return skill;
                }
            }

            return null;
        }

        public MonsterType ReadMonsterKind(int entityId)
        {
            return m_BattleSpawer != null ? m_BattleSpawer.ReadMonsterKind(entityId) : MonsterType.Normal;
        }

        public void TrySummonMonstersFromEntity(PropertyEntity sourceEntity, int targetCount)
        {
            if (m_BattleSpawer == null)
            {
                return;
            }

            m_BattleSpawer.TrySummonMonstersFromEntity(sourceEntity, targetCount);
        }

        private bool CanRechargePlayerSkillEnergy(PropertyEntity heroEntity, out bool hasActiveSkill)
        {
            hasActiveSkill = false;
            if (heroEntity == null)
            {
                return false;
            }

            var skillMgr = heroEntity.GetSkillManager();
            var skills = skillMgr != null ? skillMgr.ReadSkills() : null;
            if (skills == null || skills.Count <= 0)
            {
                return false;
            }

            int skillCount = skills.Count;
            for (int i = 0; i < skillCount; i++)
            {
                var skill = skills[i];
                if (skill == null || !RequiresPlayerSkillEnergy(heroEntity, skill))
                {
                    continue;
                }

                hasActiveSkill = true;
                if (!skill.ReadIsCooldown())
                {
                    return false;
                }
            }

            // 共享能量仅在全部“消耗能量的主动技能”都结束 CD 后开始自然充能。
            return hasActiveSkill;
        }

        private int ResolvePlayerAutoSkillSlot(PropertyEntity heroEntity)
        {
            var playerHero = heroEntity as PlayerHero;
            return playerHero != null ? playerHero.ReadTowerDefendAutoSkillSlot() : -1;
        }

        private PropertyEntity FindGuardHeroEntity(long playerId)
        {
            var heroes = m_BattleSpawer != null ? m_BattleSpawer.ReadGuardHeroes() : null;
            if (heroes == null)
            {
                return null;
            }

            int heroCount = heroes.Count;
            for (int i = 0; i < heroCount; i++)
            {
                var hero = heroes[i];
                if (BattleManager.ReadIsEntityValide(hero) && hero.ReadBattlePlayerId() == playerId)
                {
                    return hero;
                }
            }

            return null;
        }

        private void ApplyUpgradeToPlayer(long playerId)
        {
            var player = GetPlayer(playerId);
            if (player == null)
            {
                return;
            }

            var newLevel = Mathf.Clamp(player.m_RoleLevel + 1, 1, m_TowerDefendMaxRoleLevel);
            player.m_RoleLevel = newLevel;

            var hero = FindGuardHeroEntity(playerId);
            if (hero != null)
            {
                hero.SetLevel(newLevel);
                PlayRoleLevelUpFeedback(hero);
                Debug.Log(string.Format("[升级] 玩家 {0} 升至 {1} 级，播放特效 {2} 和音效 {3}", playerId, newLevel, m_RoleLevelUpEffectId, m_RoleLevelUpSoundId));
            }
        }

        private void PlayRoleLevelUpFeedback(PropertyEntity hero)
        {
            if (hero == null)
            {
                return;
            }

            var pos = hero.GetPosition();
            var eff = RenderEffManager.GetInstance().CreateRenderEff(m_RoleLevelUpEffectId);
            if (eff != null)
            {
                eff.ShowEff(false, pos, Vector3.zero, Vector3.one);
                eff.SetDuringTime(m_RoleLevelUpEffectDuration);
                RenderEffManager.GetInstance().SetAutoPool(eff);
            }

            AudioManager.GetInstance().Play2D(m_RoleLevelUpSoundId);
        }

        // 满级脚底特效属于塔防战斗流程的一部分，只在棒棒糖阶段显示。
        // 这里按 seatId 管理实例，既能持续跟随脚底位置，也能在阶段结束时统一回收到 RenderEffManager。
        private void UpdateUpgradeChallengeMaxLevelEffects()
        {
            if (!ReadIsUpgradeChallengePreActive() && !ReadIsUpgradeChallengeActive())
            {
                ClearUpgradeChallengeMaxLevelEffects();
                return;
            }

            int playerCount = m_BattlePlayers.Count;
            for (int i = 0; i < playerCount; i++)
            {
                var player = m_BattlePlayers[i];
                if (player == null || !TowerDefendSeatLayout.IsValidSeatId(player.m_SeatId))
                {
                    continue;
                }

                int seatId = player.m_SeatId;
                var hero = ReadGuardHeroBySeat(seatId);
                if (!BattleManager.ReadIsEntityValide(hero) || hero.ReadLevel() < m_TowerDefendMaxRoleLevel)
                {
                    MarkUpgradeChallengeMaxLevelEffectForRecycle(seatId);
                    continue;
                }

                ShowOrMoveUpgradeChallengeMaxLevelEffect(seatId, hero.GetPosition());
            }

            if (m_UpgradeChallengeMaxLevelEffects.Count > 0)
            {
                foreach (var pair in m_UpgradeChallengeMaxLevelEffects)
                {
                    if (ReadPlayerBySeat(pair.Key) == null)
                    {
                        MarkUpgradeChallengeMaxLevelEffectForRecycle(pair.Key);
                    }
                }
            }

            RecycleMarkedUpgradeChallengeMaxLevelEffects();
        }

        private void ShowOrMoveUpgradeChallengeMaxLevelEffect(int seatId, Vector3 position)
        {
            if (!m_UpgradeChallengeMaxLevelEffects.TryGetValue(seatId, out var eff) ||
                eff == null ||
                eff.m_Destroy ||
                (eff.m_Eff != null && eff.m_Eff.Equals(null)))
            {
                eff = RenderEffManager.GetInstance().CreateRenderEff(m_UpgradeChallengeMaxLevelEffectId);
                if (eff == null)
                {
                    return;
                }

                m_UpgradeChallengeMaxLevelEffects[seatId] = eff;
                eff.ShowEff(false, position, Vector3.zero, Vector3.one);
                return;
            }

            eff.SetPosition(position);
            if (!eff.IsActive())
            {
                eff.ShowEff(false, position, Vector3.zero, Vector3.one);
            }
        }

        private void MarkUpgradeChallengeMaxLevelEffectForRecycle(int seatId)
        {
            if (seatId < 0 || !m_UpgradeChallengeMaxLevelEffects.ContainsKey(seatId))
            {
                return;
            }

            if (!m_UpgradeChallengeMaxLevelEffectRecycleSeats.Contains(seatId))
            {
                m_UpgradeChallengeMaxLevelEffectRecycleSeats.Add(seatId);
            }
        }

        private void RecycleMarkedUpgradeChallengeMaxLevelEffects()
        {
            int recycleCount = m_UpgradeChallengeMaxLevelEffectRecycleSeats.Count;
            if (recycleCount <= 0)
            {
                return;
            }

            for (int i = 0; i < recycleCount; i++)
            {
                int seatId = m_UpgradeChallengeMaxLevelEffectRecycleSeats[i];
                if (!m_UpgradeChallengeMaxLevelEffects.TryGetValue(seatId, out var eff))
                {
                    continue;
                }

                m_UpgradeChallengeMaxLevelEffects.Remove(seatId);
                if (eff != null)
                {
                    RenderEffManager.GetInstance().PoolRenderEff(eff);
                }
            }

            m_UpgradeChallengeMaxLevelEffectRecycleSeats.Clear();
        }

        private void ClearUpgradeChallengeMaxLevelEffects()
        {
            if (m_UpgradeChallengeMaxLevelEffects.Count <= 0)
            {
                m_UpgradeChallengeMaxLevelEffectRecycleSeats.Clear();
                return;
            }

            foreach (var pair in m_UpgradeChallengeMaxLevelEffects)
            {
                if (!m_UpgradeChallengeMaxLevelEffectRecycleSeats.Contains(pair.Key))
                {
                    m_UpgradeChallengeMaxLevelEffectRecycleSeats.Add(pair.Key);
                }
            }

            RecycleMarkedUpgradeChallengeMaxLevelEffects();
        }

        private bool HasUpgradeablePlayer()
        {
            int playerCount = m_BattlePlayers.Count;
            for (int i = 0; i < playerCount; i++)
            {
                if (m_BattlePlayers[i].m_RoleLevel < m_TowerDefendMaxRoleLevel)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ReadHasUpgradeablePlayer()
        {
            return HasUpgradeablePlayer();
        }

        private long ResolveTeamExpMaxForCurrentProgress()
        {
            if (!HasUpgradeablePlayer())
            {
                return 1;
            }

            var upgradeCount = ReadAccumulatedUpgradeCount();
            var playerCount = Mathf.Clamp(Math.Max(1, m_BattlePlayers.Count), 1, 4);
            var levelCfg = t_levelBean.GetConfig(upgradeCount, false);
            if (levelCfg == null)
            {
                Debug.LogError($"找不到团队升级经验配置，累计升级次数：{upgradeCount}，玩家人数：{playerCount}。");
                return 1;
            }

            long targetExp = ReadTeamExpTargetFromConfig(levelCfg, playerCount);
            if (targetExp <= 0)
            {
                Debug.LogError($"团队升级经验配置无效，累计升级次数：{upgradeCount}，玩家人数：{playerCount}。");
                return 1;
            }

            return targetExp;
        }

        private long ReadAccumulatedUpgradeCount()
        {
            long upgradeCount = 0;
            int playerCount = m_BattlePlayers.Count;
            for (int i = 0; i < playerCount; i++)
            {
                var player = m_BattlePlayers[i];
                if (player == null)
                {
                    continue;
                }

                upgradeCount += Math.Max(0, player.m_RoleLevel - 1);
            }

            return upgradeCount;
        }

        private static long ReadTeamExpTargetFromConfig(t_levelBean levelCfg, int playerCount)
        {
            switch (playerCount)
            {
                case 1:
                    return levelCfg.t_1Player_exp;
                case 2:
                    return levelCfg.t_2Player_exp;
                case 3:
                    return levelCfg.t_3Player_exp;
                default:
                    return levelCfg.t_4Player_exp;
            }
        }

        private void ClearGuardHeroControlEffectsForUpgradeChallenge()
        {
            // buff/控制效果逻辑已移除，挑战阶段不再做额外清理。
        }

        private bool IsUpgradeChallengeCombatBlocked()
        {
            return m_Phase != BattlePhase.NormalGame;
        }

        public int ReadBaseHealth()
        {
            return m_BaseHealth;
        }

        public int ReadBaseMaxHealth()
        {
            return m_BaseMaxHealth;
        }

        public bool ApplyMonsterReachBase(PropertyEntity monster)
        {
            if (monster == null)
            {
                return false;
            }

            var damage = ResolveMonsterBaseDamage(monster);
            var prevHealth = m_BaseHealth;
            if (damage <= 0)
            {
                m_BaseHealth = 0;
            }
            else
            {
                m_BaseHealth = Mathf.Max(0, m_BaseHealth - damage);
            }
            NotifyBaseHealthChanged();
            //Debug.Log($"[基地扣血] 怪物 {monster.ReadId()} 到达基地，扣除 {damage} 血，基地血量 {prevHealth} → {m_BaseHealth}");

            if (m_BaseHealth <= 0 && m_Process != null)
            {
                Debug.Log($"[基地扣血] 基地血量归零，触发防守失败结算！");
                m_Process.OnFinishGame(FinishReason.DefenseFailed, GroupId.PushGroupId);
            }

            return true;
        }

        private void NotifyBaseHealthChanged()
        {
            RenderEvent.Event.OnTowerDefendBaseHealthChanged(m_BaseHealth, m_BaseMaxHealth);
        }

        private int ResolveMonsterBaseDamage(PropertyEntity monster)
        {
            if (monster == null)
            {
                return 0;
            }

            var monsterCfg = t_monsterBean.GetConfig(monster.ReadBeanId(), false);
            return monsterCfg != null ? monsterCfg.t_base_damage : 0;
        }

        public override void OnLoadMap()
        {
            m_BattleSpawer.OnLoadMap(GetStage());
        }
        public override void Update(float dt)
        {
            if (m_IsBattlePause)
            {
                return;
            }

            if (m_Process != null)
            {
                m_Process.Update(dt);
                var state = m_Process.GetState();

                if (state == (int)TowerDefendBattleState.WaitingChessMan ||
                    state == (int)TowerDefendBattleState.NpcSpeakIntro ||
                    state == (int)TowerDefendBattleState.PreStart)
                {
                    UpdateOpeningPhaseRuntime(dt);
                    return;
                }

                if (state != (int)TowerDefendBattleState.FreeGame &&
                    state != (int)TowerDefendBattleState.UpgradeChallengeCountdown &&
                    state != (int)TowerDefendBattleState.UpgradeChallenge)
                {
                    return;
                }
            }
            UpdateBattleRuntime(dt, true, true, true);
        }


        private void UpdateOpeningPhaseRuntime(float dt)
        {
            // å‡†å¤‡é˜¶æ®µå…è®¸è§’è‰²è½¬å‘å’Œæ‰‹åŠ¨æ–½æ³•ï¼Œä½†ä¸åº”æå‰åˆ·æ€ªæˆ–é¢„å……å…±äº«èƒ½é‡ã€?
            UpdateBattleRuntime(dt, false, false, false);
            UpdateNpcSpeakIntro(dt);
        }

        private void UpdateNpcSpeakIntro(float dt)
        {
            if (ReadBattleState() != TowerDefendBattleState.NpcSpeakIntro)
            {
                if (!m_NpcSpeakIntroEntered && !m_NpcSpeakIntroTimerStarted)
                {
                    return;
                }

                DestroyNpcSpeakIntroEntity();
                m_NpcSpeakIntroEntered = false;
                m_NpcSpeakIntroTimerStarted = false;
                m_NpcSpeakIntroDurationSeconds = 0f;
                m_NpcSpeakIntroGoPlayed = false;
                return;
            }

            BeginNpcSpeakIntro();

            if (!m_NpcSpeakIntroEntered)
            {
                return;
            }

            if (!m_NpcSpeakIntroTimerStarted)
            {
                m_NpcSpeakIntroLoadTimeoutLeft = Mathf.Max(0f, m_NpcSpeakIntroLoadTimeoutLeft - dt);
                if (m_NpcSpeakIntroLoadTimeoutLeft <= 0f)
                {
                    CompleteNpcSpeakIntro("npc load timeout");
                }
                return;
            }

            m_NpcSpeakIntroTimeLeft = Mathf.Max(0f, m_NpcSpeakIntroTimeLeft - dt);
            TryPlayNpcSpeakIntroGoAnimation();
            if (m_NpcSpeakIntroTimeLeft <= 0f)
            {
                CompleteNpcSpeakIntro("npc intro finished");
            }
        }

        private void BeginNpcSpeakIntro()
        {
            if (m_NpcSpeakIntroEntered)
            {
                return;
            }

            m_NpcSpeakIntroEntered = true;

            if (!TryResolveNpcSpeakIntroRequest(out var request, out var reason))
            {
                CompleteNpcSpeakIntro(reason);
                return;
            }

            var monsterCfg = t_monsterBean.GetConfig(request.MonsterConfigId, false);
            if (monsterCfg == null)
            {
                CompleteNpcSpeakIntro("npc monster config missing");
                return;
            }

            DestroyNpcSpeakIntroEntity();

            m_NpcSpeakIntroSoundId = request.SoundId;
            m_NpcSpeakIntroDurationSeconds = request.DurationSeconds;
            m_NpcSpeakIntroTimeLeft = request.DurationSeconds;
            m_NpcSpeakIntroLoadTimeoutLeft = m_NpcSpeakIntroLoadTimeoutSeconds;
            m_NpcSpeakIntroGoPlayed = false;

            m_NpcSpeakIntroEntity = new TowerDefendNpcSpeakEntity();
            m_NpcSpeakIntroEntity.Initialize(
                BattleManager.GetObjectManager().AssignClientId(),
                monsterCfg,
                m_NpcSpeakPoint,
                OnNpcSpeakIntroEntityLoaded);
        }

        private void OnNpcSpeakIntroEntityLoaded(TowerDefendNpcSpeakEntity entity)
        {
            if (entity == null || entity != m_NpcSpeakIntroEntity)
            {
                return;
            }

            if (ReadBattleState() != TowerDefendBattleState.NpcSpeakIntro)
            {
                return;
            }

            var render = entity.GetRender();
            if (render == null)
            {
                CompleteNpcSpeakIntro("npc render missing");
                return;
            }

            m_NpcSpeakIntroFadeAlpha = 1.0f;
            render.SetColorAlphaProperty(m_NpcSpeakIntroFadeColorPropertyId, m_NpcSpeakIntroFadeAlpha);
            render.PlayAnimation("idle");
            var duckOptions = new AudioDuckOptions(AudioBusMask.All & ~AudioBusMask.Voice, m_NpcSpeakIntroDuckTargetVolume);
            AudioManager.GetInstance().Play3D(m_NpcSpeakIntroSoundId, entity.ReadSpeakWorldPosition(), duckOptions);
            m_NpcSpeakIntroTimerStarted = true;
            TryPlayNpcSpeakIntroGoAnimation();
        }

        private void TryPlayNpcSpeakIntroGoAnimation()
        {
            if (m_NpcSpeakIntroGoPlayed)
            {
                return;
            }

            if (!m_NpcSpeakIntroTimerStarted || m_NpcSpeakIntroDurationSeconds < m_NpcSpeakIntroGoTriggerSeconds)
            {
                return;
            }

            var entity = m_NpcSpeakIntroEntity;
            if (entity == null || entity.ReadIsDestroy())
            {
                return;
            }

            var render = entity.GetRender();
            if (render == null)
            {
                return;
            }

            // 策划要求喊话阶段进入第 7 秒时切到 go 动画。
            // 这里按“开始计时后的累计时长”判断，只触发一次，不改动原有总时长与结束时机。
            float elapsedSeconds = Mathf.Max(0f, m_NpcSpeakIntroDurationSeconds - m_NpcSpeakIntroTimeLeft);
            if (elapsedSeconds < m_NpcSpeakIntroGoTriggerSeconds)
            {
                return;
            }

            render.PlayAnimation("go");
            m_NpcSpeakIntroGoPlayed = true;
        }

        private void CompleteNpcSpeakIntro(string reason)
        {
            if (!m_NpcSpeakIntroEntered && !m_NpcSpeakIntroTimerStarted)
            {
                return;
            }

            Debug.Log("TowerDefendBattle: skip or finish npc speak intro, reason=" + reason);
            FadeOutNpcSpeakIntroEntityAndDestroy();
            m_NpcSpeakIntroEntered = false;
            m_NpcSpeakIntroTimerStarted = false;
            m_NpcSpeakIntroTimeLeft = 0f;
            m_NpcSpeakIntroLoadTimeoutLeft = 0f;
            m_NpcSpeakIntroSoundId = 0;
            m_NpcSpeakIntroDurationSeconds = 0f;
            m_NpcSpeakIntroGoPlayed = false;

            var process = ReadTowerDefendBattleProcess();
            if (process != null)
            {
                process.FinishNpcSpeakIntro();
            }
        }

        // 喊话结束属于战斗流程状态切换，淡出时序放在战斗流程里，渲染层只负责写颜色属性 Alpha。
        private void FadeOutNpcSpeakIntroEntityAndDestroy()
        {
            var entity = m_NpcSpeakIntroEntity;
            if (entity == null || entity.ReadIsDestroy())
            {
                KillNpcSpeakIntroFadeTween(false);
                m_NpcSpeakIntroEntity = null;
                m_NpcSpeakIntroFadeAlpha = 1.0f;
                return;
            }

            var render = entity.GetRender();
            if (render == null || !render.IsObjectLoaded() || !render.SupportsColorAlphaProperty(m_NpcSpeakIntroFadeColorPropertyId))
            {
                DestroyNpcSpeakIntroEntity();
                return;
            }

            KillNpcSpeakIntroFadeTween(false);
            render.SetColorAlphaProperty(m_NpcSpeakIntroFadeColorPropertyId, m_NpcSpeakIntroFadeAlpha);
            if (m_NpcSpeakIntroFadeOutSeconds <= 0f || Mathf.Abs(m_NpcSpeakIntroFadeAlpha) <= 0.0001f)
            {
                m_NpcSpeakIntroFadeAlpha = 0.0f;
                render.SetColorAlphaProperty(m_NpcSpeakIntroFadeColorPropertyId, 0.0f);
                if (!entity.ReadIsDestroy())
                {
                    entity.Destroy();
                }

                if (m_NpcSpeakIntroEntity == entity)
                {
                    m_NpcSpeakIntroEntity = null;
                }
                m_NpcSpeakIntroFadeAlpha = 1.0f;
                return;
            }

            m_NpcSpeakIntroFadeTween = DOTween
                .To(
                    () => m_NpcSpeakIntroFadeAlpha,
                    value =>
                    {
                        m_NpcSpeakIntroFadeAlpha = value;
                        render.SetColorAlphaProperty(m_NpcSpeakIntroFadeColorPropertyId, value);
                    },
                    0.0f,
                    m_NpcSpeakIntroFadeOutSeconds)
                .SetEase(Ease.Linear)
                .OnKill(() => m_NpcSpeakIntroFadeTween = null)
                .OnComplete(() =>
                {
                    if (!entity.ReadIsDestroy())
                    {
                        entity.Destroy();
                    }

                    if (m_NpcSpeakIntroEntity == entity)
                    {
                        m_NpcSpeakIntroEntity = null;
                    }
                    m_NpcSpeakIntroFadeAlpha = 1.0f;
                });
        }

        private void DestroyNpcSpeakIntroEntity()
        {
            KillNpcSpeakIntroFadeTween(false);
            if (m_NpcSpeakIntroEntity == null)
            {
                m_NpcSpeakIntroFadeAlpha = 1.0f;
                return;
            }

            m_NpcSpeakIntroEntity.Destroy();
            m_NpcSpeakIntroEntity = null;
            m_NpcSpeakIntroFadeAlpha = 1.0f;
        }

        private void KillNpcSpeakIntroFadeTween(bool complete)
        {
            if (m_NpcSpeakIntroFadeTween == null)
            {
                return;
            }

            m_NpcSpeakIntroFadeTween.Kill(complete);
            m_NpcSpeakIntroFadeTween = null;
        }

        private void UpdateBattleRuntime(float dt, bool updateBattleSpawer, bool updatePlayerSkillEnergy, bool updateBattleStat)
        {
            m_LastRuntimeDeltaTime = Mathf.Max(0.0f, dt);
            if (updateBattleSpawer && m_BattleSpawer != null)
            {
                m_BattleSpawer.Update(dt);
            }
            if (m_Phase == BattlePhase.ChallengeEntryWait)
            {
                if (m_PhaseTimer > 0f)
                {
                    m_PhaseTimer -= dt;
                    if (m_PhaseTimer < 0f)
                    {
                        m_PhaseTimer = 0f;
                    }
                }

                if (m_PhaseTimer <= 0f && m_IsUpgradeChallengeCameraReady)
                {
                    SetPhase(BattlePhase.ChallengeCountdown);
                    RenderEvent.Event.OnLollipopHealthChanged(m_UpgradeChallengeTarget.ReadMaxHealth(), m_UpgradeChallengeTarget.ReadCurrentHealth(),true);
                }
            }
            // 技能能量属于塔防战斗运行时状态，保持在现有战斗主循环里更新，避免另起一套系统。
            if (updatePlayerSkillEnergy)
            {
                UpdatePlayerSkillEnergy(dt);
            }
            if(m_ObjectManager != null)
            {
                m_ObjectManager.Update(dt);
            }
            if (updateBattleStat && m_BattleStat != null)
            {
                m_BattleStat.Update(dt);
            }

            UpdateUpgradeChallengeTargetDestroyState();
            UpdateUpgradeChallengeMaxLevelEffects();
            UpdatePostUpgradeChallengeAimRecovery(dt);
            UpdateInput(dt);
        }

        //战斗中玩家控制的英雄，目前主要是PC端模拟，真机不走这个路线
        private void UpdateInput(float dt)
        {
            if (!BoneRemoteDebugEditorConfig.ReadIsKeyboardControlEnabled())
            {
                return;
            }

            if (TryReadKeyboardManualSeatId(0, out var primarySeatId))
            {
                UpdateManualSeatInput(primarySeatId, dt, KeyCode.A, KeyCode.D, KeyCode.J, KeyCode.K);
            }

            if (TryReadKeyboardManualSeatId(1, out var secondarySeatId))
            {
                UpdateManualSeatInput(secondarySeatId, dt, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Keypad1, KeyCode.Keypad2);
            }
        }

        private bool TryReadKeyboardManualSeatId(int playerIndex, out int seatId)
        {
            seatId = TowerDefendSeatLayout.DefaultControlledSeatId;
            if (playerIndex < 0)
            {
                return false;
            }

            int foundPlayerCount = 0;
            int playerCount = m_BattlePlayers.Count;
            for (int i = 0; i < playerCount; i++)
            {
                var player = m_BattlePlayers[i];
                if (player == null || !TowerDefendSeatLayout.IsValidSeatId(player.m_SeatId))
                {
                    continue;
                }

                if (foundPlayerCount == playerIndex)
                {
                    seatId = player.m_SeatId;
                    return true;
                }

                foundPlayerCount++;
            }

            return false;
        }

        private void UpdateManualSeatInput(
            int seatId,
            float dt,
            KeyCode turnLeftKey,
            KeyCode turnRightKey,
            KeyCode normalAttackKey,
            KeyCode skillKey)
        {
            if (!TowerDefendSeatLayout.IsValidSeatId(seatId) || m_BattleSpawer == null)
            {
                return;
            }

            var hero = m_BattleSpawer.ReadGuardHeroBySeat(seatId);
            if (!BattleManager.ReadIsEntityValide(hero))
            {
                return;
            }

            RefreshControlledSeatAimPose(seatId, hero);

            float yawInput = 0f;
            if (Input.GetKey(turnLeftKey)) yawInput -= 1f;
            if (Input.GetKey(turnRightKey)) yawInput += 1f;

            if (yawInput != 0f)
            {
                var currentAngles = EnsureManualAimAngles(seatId, hero);
                var currentYaw = currentAngles.x;

                var nextYaw = Mathf.Clamp(
                    currentYaw + yawInput * m_ManualYawSpeedDegrees * dt,
                    -m_ManualYawClampDegrees,
                    m_ManualYawClampDegrees);
                var nextPitchOffset = currentAngles.y;

                var nextAimForward = BuildForwardFromYawPitch(nextYaw, ResolveManualAimPitchDegrees(hero, nextPitchOffset));
                m_ManualAimAngles[seatId] = new Vector2(nextYaw, nextPitchOffset);

                var horizontalForward = new Vector3(nextAimForward.x, 0f, nextAimForward.z);
                if (horizontalForward.sqrMagnitude > 0.0001f)
                {
                    hero.SetForward(horizontalForward.normalized);
                }
                hero.SetBaseForward(nextAimForward);
                m_BattleSpawer.MarkManualControl(hero);
            }

            if (Input.GetKeyDown(normalAttackKey))
            {
                var faceDir = ReadManualAimForwardBySeat(seatId);
                m_BattleSpawer.TryGuardHeroActionBySeat(seatId, 0, faceDir, Vector3.zero);
            }

            if (Input.GetKeyDown(skillKey))
            {
                var faceDir = ReadManualAimForwardBySeat(seatId);
                m_BattleSpawer.TryGuardHeroActionBySeat(seatId, 1, faceDir, Vector3.zero);
            }
        }

        private static void ExtractYawPitch(Vector3 forward, out float yawDegrees, out float pitchDegrees)
        {
            var normalizedForward = NormalizeForwardOrDefault(forward);
            yawDegrees = Mathf.Atan2(normalizedForward.x, normalizedForward.z) * Mathf.Rad2Deg;
            var planarLength = Mathf.Sqrt(normalizedForward.x * normalizedForward.x + normalizedForward.z * normalizedForward.z);
            pitchDegrees = Mathf.Atan2(normalizedForward.y, planarLength) * Mathf.Rad2Deg;
        }

        private static float ReadManualAimBasePitchDegrees(PropertyEntity hero)
        {
            return BattleManager.ReadIsEntityValide(hero) ? hero.ReadDefaultPitchDegrees() : 0f;
        }

        private static float ResolveManualAimPitchDegrees(PropertyEntity hero, float pitchOffsetDegrees)
        {
            return ReadManualAimBasePitchDegrees(hero) + pitchOffsetDegrees;
        }

        private static Vector3 BuildForwardFromYawPitch(float yawDegrees, float pitchDegrees)
        {
            // 显式按 yaw/pitch 三角公式重建方向，避免 Quaternion.Euler 的旋转顺序
            // 在 yaw 接近左右极限时放大出“继续抬头会朝天”的误差。
            var yawRadians = yawDegrees * Mathf.Deg2Rad;
            var pitchRadians = pitchDegrees * Mathf.Deg2Rad;
            var cosPitch = Mathf.Cos(pitchRadians);
            var forward = new Vector3(
                Mathf.Sin(yawRadians) * cosPitch,
                Mathf.Sin(pitchRadians),
                Mathf.Cos(yawRadians) * cosPitch);
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private static Vector3 NormalizeForwardOrDefault(Vector3 forward)
        {
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        public override void OnDrawGizmos()
        {
            if(m_ObjectManager != null)
            {
                m_ObjectManager.OnDrawGizmos();
            }
        }

        public override void UpdateRender(float dt)
        {
            if(m_ObjectManager != null)
            {
                m_ObjectManager.UpdateRender();
            }

            if (m_NpcSpeakIntroEntity != null && !m_NpcSpeakIntroEntity.ReadIsDestroy())
            {
                m_NpcSpeakIntroEntity.UpdateRender();
            }
        }

        public override void OnRelease()
        {
            m_ManualAimAngles.Clear();
            m_GuardHeroAutoAimStates.Clear();
            ClearUpgradeChallengeMaxLevelEffects();
            m_PostUpgradeChallengeAimRecoveringSeats.Clear();
            m_PostUpgradeChallengeAimRecoveryFinishedSeats.Clear();
            DestroyNpcSpeakIntroEntity();
            DestroyUpgradeChallengeTarget();
            if (m_BattleSpawer != null)
            {
                m_BattleSpawer.OnRelease();
                m_BattleSpawer = null;
            }

            if (m_BattleStat != null)
            {
                m_BattleStat.Destroy();
                m_BattleStat = null;
            }
            base.OnRelease();
        }

        public UpgradeChallengeTarget ReadUpgradeChallengeTarget()
        {
            return m_UpgradeChallengeTarget;
        }
        public void SetUpgradeChallengeTarget(UpgradeChallengeTarget target)
        {
            m_UpgradeChallengeTarget = target;
        }
        public Vector3 GetUpgradeChallengePosition()
        {
            return m_UpgradeChallengeTarget != null ? m_UpgradeChallengeTarget.GetFootPosition() : Vector3.zero;
        }
        public Vector3 GetUpgradeChallengeCenterPosition()
        {
            return m_UpgradeChallengeTarget != null ? m_UpgradeChallengeTarget.ReadCenterPosition() : Vector3.zero;
        }
    }

    public static class TowerDefendSeatLayout
    {
        public const int MaxSupportedPlayerCount = 4;
        public const int DefaultPlayerCount = 2;
        public const int DefaultControlledSeatId = 0;

        private static readonly int[] m_StartupSeatOrder = { 0, 2, 1, 3 };

        public static int NormalizePlayerCount(int playerCount)
        {
            return Math.Max(1, Math.Min(MaxSupportedPlayerCount, playerCount));
        }

        public static bool IsValidSeatId(int seatId)
        {
            return seatId >= 0 && seatId < MaxSupportedPlayerCount;
        }

        public static int GetStartupSeatIdByIndex(int playerIndex, int playerCount)
        {
            playerCount = NormalizePlayerCount(playerCount);
            if (playerIndex < 0 || playerIndex >= playerCount)
            {
                throw new ArgumentOutOfRangeException("playerIndex");
            }

            return m_StartupSeatOrder[playerIndex];
        }

        public static int[] ReadPrepareDefaultSeatIds()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return new[] { 0, 2 };
            }
            return new int[0];
        }
    }
}
