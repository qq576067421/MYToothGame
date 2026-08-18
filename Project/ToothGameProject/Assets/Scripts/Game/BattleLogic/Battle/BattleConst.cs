using System.Collections.Generic;

namespace GameDll
{
    public class BattleConst
    {
        public const float DefaultParabolicBulletY = 0.8f;
        public const float DefaultArcDurationMin = 0.18f;
        public const float DefaultArcDurationMax = 0.35f;
        public const float DefaultArcHorizontalAngle = 10.0f;
        public const float DefaultArcUpwardAngleMin = 4.0f;
        public const float DefaultArcUpwardAngleMax = 9.0f;
        public const float DefaultArcBlendSpeed = 1.0f;
        public const float DefaultTrackingTurnTime = 10.0f;
        public const float DefaultSpawnYawSpread = 5.0f;
        public const float DefaultSpawnYawJitter = 10.0f;
        public const float DefaultSpawnUpwardSpreadMin = 1.0f;
        public const float DefaultSpawnUpwardSpreadMax = 3.0f;
        public const float DefaultSpawnUpwardJitter = 5.0f;
        public const bool DefaultTriggerTrackingStartHoldTimeUseRandom = true;
        public const float DefaultTriggerTrackingStartHoldTimeMin = 0.1f;
        public const float DefaultTriggerTrackingStartHoldTimeMax = 0.1f;
        public const float DefaultDamageNumberOffset = 0.3f;
        public const float DefaultTowerDefendWaveBatchSpawnIntervalSec = 1.0f;
        public const bool DefaultTowerDefendAutoAimEnabled = true;
        public const float DefaultTowerDefendAutoAimSnapMaxHorizontalDistance = 1.0f;
        public const float DefaultTowerDefendAutoAimSwitchSmoothSpeed = 8.0f;
        public const float DefaultTowerDefendAutoAimRecoverFromUpgradeSmoothSpeed = 8.0f;
        public const float DefaultTowerDefendNoTargetAimClampForwardOffset = 6.0f;
        public const float DefaultTowerDefendUpgradeChallengeDropSpeed = 2.0f;
        public const float DefaultUpgradeChallengeCrackLightScale = 0.01f;
        public const float DefaultUpgradeChallengeCrackMediumScale = 0.03f;
        public const float DefaultUpgradeChallengeCrackFullScale = 0.07f;
        public const float DefaultUpgradeChallengeShatterExplosionForce = 12.0f;
        public const float DefaultUpgradeChallengeShatterExplosionRadius = 4.5f;
        public const float DefaultUpgradeChallengeShatterExplosionUpwardsModifier = 0.75f;
        public const float DefaultUpgradeChallengeShatterExplosionTorque = 6.0f;
        public const float DefaultUpgradeChallengeShatterDestroyDelay = 2.5f;

        public static float m_ParabolicBulletY = DefaultParabolicBulletY;
        public static float ArcDurationMin = DefaultArcDurationMin;
        public static float ArcDurationMax = DefaultArcDurationMax;
        public static float ArcHorizontalAngle = DefaultArcHorizontalAngle;
        public static float ArcUpwardAngleMin = DefaultArcUpwardAngleMin;
        public static float ArcUpwardAngleMax = DefaultArcUpwardAngleMax;
        public static float ArcBlendSpeed = DefaultArcBlendSpeed;
        public static float TrackingTurnTime = DefaultTrackingTurnTime;
        public static float SpawnYawSpread = DefaultSpawnYawSpread;
        public static float SpawnYawJitter = DefaultSpawnYawJitter;
        public static float SpawnUpwardSpreadMin = DefaultSpawnUpwardSpreadMin;
        public static float SpawnUpwardSpreadMax = DefaultSpawnUpwardSpreadMax;
        public static float SpawnUpwardJitter = DefaultSpawnUpwardJitter;
        public static bool TriggerTrackingStartHoldTimeUseRandom = DefaultTriggerTrackingStartHoldTimeUseRandom;
        public static float TriggerTrackingStartHoldTimeMin = DefaultTriggerTrackingStartHoldTimeMin;
        public static float TriggerTrackingStartHoldTimeMax = DefaultTriggerTrackingStartHoldTimeMax;
        public static float DamageNumberOffset = DefaultDamageNumberOffset;
        public static float TowerDefendWaveBatchSpawnIntervalSec = DefaultTowerDefendWaveBatchSpawnIntervalSec;
        public static bool TowerDefendAutoAimEnabled = DefaultTowerDefendAutoAimEnabled;
        public static float TowerDefendAutoAimSnapMaxHorizontalDistance = DefaultTowerDefendAutoAimSnapMaxHorizontalDistance;
        public static float TowerDefendAutoAimSwitchSmoothSpeed = DefaultTowerDefendAutoAimSwitchSmoothSpeed;
        public static float TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed = DefaultTowerDefendAutoAimRecoverFromUpgradeSmoothSpeed;
        public static float TowerDefendNoTargetAimClampForwardOffset = DefaultTowerDefendNoTargetAimClampForwardOffset;
        public static float TowerDefendUpgradeChallengeDropSpeed = DefaultTowerDefendUpgradeChallengeDropSpeed;
        public static float UpgradeChallengeCrackLightScale = DefaultUpgradeChallengeCrackLightScale;
        public static float UpgradeChallengeCrackMediumScale = DefaultUpgradeChallengeCrackMediumScale;
        public static float UpgradeChallengeCrackFullScale = DefaultUpgradeChallengeCrackFullScale;
        public static float UpgradeChallengeShatterExplosionForce = DefaultUpgradeChallengeShatterExplosionForce;
        public static float UpgradeChallengeShatterExplosionRadius = DefaultUpgradeChallengeShatterExplosionRadius;
        public static float UpgradeChallengeShatterExplosionUpwardsModifier = DefaultUpgradeChallengeShatterExplosionUpwardsModifier;
        public static float UpgradeChallengeShatterExplosionTorque = DefaultUpgradeChallengeShatterExplosionTorque;
        public static float UpgradeChallengeShatterDestroyDelay = DefaultUpgradeChallengeShatterDestroyDelay;

        public static void ResetToDefault()
        {
            m_ParabolicBulletY = DefaultParabolicBulletY;
            ArcDurationMin = DefaultArcDurationMin;
            ArcDurationMax = DefaultArcDurationMax;
            ArcHorizontalAngle = DefaultArcHorizontalAngle;
            ArcUpwardAngleMin = DefaultArcUpwardAngleMin;
            ArcUpwardAngleMax = DefaultArcUpwardAngleMax;
            ArcBlendSpeed = DefaultArcBlendSpeed;
            TrackingTurnTime = DefaultTrackingTurnTime;
            SpawnYawSpread = DefaultSpawnYawSpread;
            SpawnYawJitter = DefaultSpawnYawJitter;
            SpawnUpwardSpreadMin = DefaultSpawnUpwardSpreadMin;
            SpawnUpwardSpreadMax = DefaultSpawnUpwardSpreadMax;
            SpawnUpwardJitter = DefaultSpawnUpwardJitter;
            TriggerTrackingStartHoldTimeUseRandom = DefaultTriggerTrackingStartHoldTimeUseRandom;
            TriggerTrackingStartHoldTimeMin = DefaultTriggerTrackingStartHoldTimeMin;
            TriggerTrackingStartHoldTimeMax = DefaultTriggerTrackingStartHoldTimeMax;
            DamageNumberOffset = DefaultDamageNumberOffset;
            TowerDefendWaveBatchSpawnIntervalSec = DefaultTowerDefendWaveBatchSpawnIntervalSec;
            TowerDefendAutoAimEnabled = DefaultTowerDefendAutoAimEnabled;
            TowerDefendAutoAimSnapMaxHorizontalDistance = DefaultTowerDefendAutoAimSnapMaxHorizontalDistance;
            TowerDefendAutoAimSwitchSmoothSpeed = DefaultTowerDefendAutoAimSwitchSmoothSpeed;
            TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed = DefaultTowerDefendAutoAimRecoverFromUpgradeSmoothSpeed;
            TowerDefendNoTargetAimClampForwardOffset = DefaultTowerDefendNoTargetAimClampForwardOffset;
            TowerDefendUpgradeChallengeDropSpeed = DefaultTowerDefendUpgradeChallengeDropSpeed;
            UpgradeChallengeCrackLightScale = DefaultUpgradeChallengeCrackLightScale;
            UpgradeChallengeCrackMediumScale = DefaultUpgradeChallengeCrackMediumScale;
            UpgradeChallengeCrackFullScale = DefaultUpgradeChallengeCrackFullScale;
            UpgradeChallengeShatterExplosionForce = DefaultUpgradeChallengeShatterExplosionForce;
            UpgradeChallengeShatterExplosionRadius = DefaultUpgradeChallengeShatterExplosionRadius;
            UpgradeChallengeShatterExplosionUpwardsModifier = DefaultUpgradeChallengeShatterExplosionUpwardsModifier;
            UpgradeChallengeShatterExplosionTorque = DefaultUpgradeChallengeShatterExplosionTorque;
            UpgradeChallengeShatterDestroyDelay = DefaultUpgradeChallengeShatterDestroyDelay;
            ClampValues();
        }

        public static void ClampValues()
        {
            m_ParabolicBulletY = UnityEngine.Mathf.Max(0f, m_ParabolicBulletY);
            ArcDurationMin = UnityEngine.Mathf.Max(0f, ArcDurationMin);
            ArcDurationMax = UnityEngine.Mathf.Max(ArcDurationMin, ArcDurationMax);
            ArcHorizontalAngle = UnityEngine.Mathf.Max(0f, ArcHorizontalAngle);
            ArcUpwardAngleMin = UnityEngine.Mathf.Max(0f, ArcUpwardAngleMin);
            ArcUpwardAngleMax = UnityEngine.Mathf.Max(ArcUpwardAngleMin, ArcUpwardAngleMax);
            ArcBlendSpeed = UnityEngine.Mathf.Max(0f, ArcBlendSpeed);
            TrackingTurnTime = UnityEngine.Mathf.Max(0f, TrackingTurnTime);
            SpawnYawSpread = UnityEngine.Mathf.Max(0f, SpawnYawSpread);
            SpawnYawJitter = UnityEngine.Mathf.Max(0f, SpawnYawJitter);
            SpawnUpwardSpreadMin = UnityEngine.Mathf.Max(0f, SpawnUpwardSpreadMin);
            SpawnUpwardSpreadMax = UnityEngine.Mathf.Max(SpawnUpwardSpreadMin, SpawnUpwardSpreadMax);
            SpawnUpwardJitter = UnityEngine.Mathf.Max(0f, SpawnUpwardJitter);
            TriggerTrackingStartHoldTimeMin = UnityEngine.Mathf.Max(0f, TriggerTrackingStartHoldTimeMin);
            TriggerTrackingStartHoldTimeMax = UnityEngine.Mathf.Max(TriggerTrackingStartHoldTimeMin, TriggerTrackingStartHoldTimeMax);
            ClampDamageNumberOffsetValues();
            ClampTowerDefendSpawnValues();
            ClampTowerDefendAutoAimValues();
            ClampTowerDefendUpgradeChallengeValues();
            ClampUpgradeChallengeCrackValues();
            ClampUpgradeChallengeShatterValues();
        }

        public static void ClampDamageNumberOffsetValues()
        {
            DamageNumberOffset = UnityEngine.Mathf.Max(0f, DamageNumberOffset);
        }

        public static void ClampTowerDefendSpawnValues()
        {
            TowerDefendWaveBatchSpawnIntervalSec = UnityEngine.Mathf.Max(0f, TowerDefendWaveBatchSpawnIntervalSec);
        }

        public static void ClampTowerDefendAutoAimValues()
        {
            TowerDefendAutoAimSnapMaxHorizontalDistance = UnityEngine.Mathf.Max(
                0f,
                TowerDefendAutoAimSnapMaxHorizontalDistance);
            TowerDefendAutoAimSwitchSmoothSpeed = UnityEngine.Mathf.Max(
                0f,
                TowerDefendAutoAimSwitchSmoothSpeed);
            TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed = UnityEngine.Mathf.Max(
                0f,
                TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed);
            TowerDefendNoTargetAimClampForwardOffset = UnityEngine.Mathf.Max(
                0f,
                TowerDefendNoTargetAimClampForwardOffset);
        }

        public static void ClampTowerDefendUpgradeChallengeValues()
        {
            TowerDefendUpgradeChallengeDropSpeed = UnityEngine.Mathf.Max(0f, TowerDefendUpgradeChallengeDropSpeed);
        }

        public static void ClampUpgradeChallengeCrackValues()
        {
            UpgradeChallengeCrackLightScale = UnityEngine.Mathf.Clamp(UpgradeChallengeCrackLightScale, 0f, 0.99f);
            UpgradeChallengeCrackMediumScale = UnityEngine.Mathf.Clamp(UpgradeChallengeCrackMediumScale, UpgradeChallengeCrackLightScale, 0.99f);
            UpgradeChallengeCrackFullScale = UnityEngine.Mathf.Clamp(UpgradeChallengeCrackFullScale, UpgradeChallengeCrackMediumScale, 0.99f);
        }

        public static void ClampUpgradeChallengeShatterValues()
        {
            UpgradeChallengeShatterExplosionForce = UnityEngine.Mathf.Max(0f, UpgradeChallengeShatterExplosionForce);
            UpgradeChallengeShatterExplosionRadius = UnityEngine.Mathf.Max(0f, UpgradeChallengeShatterExplosionRadius);
            UpgradeChallengeShatterExplosionUpwardsModifier = UnityEngine.Mathf.Max(0f, UpgradeChallengeShatterExplosionUpwardsModifier);
            UpgradeChallengeShatterExplosionTorque = UnityEngine.Mathf.Max(0f, UpgradeChallengeShatterExplosionTorque);
            UpgradeChallengeShatterDestroyDelay = UnityEngine.Mathf.Max(0f, UpgradeChallengeShatterDestroyDelay);
        }
    }
}
