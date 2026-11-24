using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

using GameDll;
using System;

using Object = UnityEngine.Object;
using MonoBean;
using AssetBundles;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;

//lua
public class GMTools : EditorWindow
{
    private const string BattleConstAssetPath = "Assets/Scripts/Game/BattleLogic/Battle/BattleConst.cs";
    private const string BoneTurnTuningAssetPath = "Assets/Scripts/Game/BattleLogic/Battle/BoneInput/BoneTurnTuning.cs";
    private const string GmBridgeSettingsAssetPath = "Assets/Scripts/Game/DevTools/GmBridge/GmBridgeSettings.cs";
    private const string BattleConstSessionKeyPrefix = "GMTools.BattleConst.";
    private const string BattleConstSessionMarkerKey = BattleConstSessionKeyPrefix + "HasOverrides";
    private const string BattleSettingsUnsavedChangesMessage = "GMTools 中还有战斗参数没有回填到代码默认值。是否现在回填保存？";
    private const string BoneTurnTuningSessionKeyPrefix = "GMTools.BoneTurnTuning.";
    private const string BoneTurnTuningSessionMarkerKey = BoneTurnTuningSessionKeyPrefix + "HasOverrides";

    private sealed class BattleConstFloatBinding
    {
        public readonly string Label;
        public readonly string Tooltip;
        public readonly string DefaultFieldName;
        public readonly Func<float> Getter;
        public readonly Action<float> Setter;

        public BattleConstFloatBinding(string label, string tooltip, string defaultFieldName, Func<float> getter, Action<float> setter)
        {
            Label = label;
            Tooltip = tooltip;
            DefaultFieldName = defaultFieldName;
            Getter = getter;
            Setter = setter;
        }
    }

    private sealed class BattleConstBoolBinding
    {
        public readonly string Label;
        public readonly string Tooltip;
        public readonly string DefaultFieldName;
        public readonly Func<bool> Getter;
        public readonly Action<bool> Setter;

        public BattleConstBoolBinding(string label, string tooltip, string defaultFieldName, Func<bool> getter, Action<bool> setter)
        {
            Label = label;
            Tooltip = tooltip;
            DefaultFieldName = defaultFieldName;
            Getter = getter;
            Setter = setter;
        }
    }

    private sealed class BoneTurnTuningFloatBinding
    {
        public readonly string FieldName;
        public readonly string DefaultFieldName;
        public readonly Func<float> Getter;
        public readonly Action<float> Setter;

        public BoneTurnTuningFloatBinding(string fieldName, string defaultFieldName, Func<float> getter, Action<float> setter)
        {
            FieldName = fieldName;
            DefaultFieldName = defaultFieldName;
            Getter = getter;
            Setter = setter;
        }
    }

    private sealed class BoneTurnTuningBoolBinding
    {
        public readonly string FieldName;
        public readonly string DefaultFieldName;
        public readonly Func<bool> Getter;
        public readonly Action<bool> Setter;

        public BoneTurnTuningBoolBinding(string fieldName, string defaultFieldName, Func<bool> getter, Action<bool> setter)
        {
            FieldName = fieldName;
            DefaultFieldName = defaultFieldName;
            Getter = getter;
            Setter = setter;
        }
    }

    private sealed class TrackingPreset
    {
        public readonly string Name;
        public readonly string Tooltip;
        public readonly float ArcDurationMin;
        public readonly float ArcDurationMax;
        public readonly float ArcHorizontalAngle;
        public readonly float ArcUpwardAngleMin;
        public readonly float ArcUpwardAngleMax;
        public readonly float ArcBlendSpeed;
        public readonly float TrackingTurnTime;
        public readonly float SpawnYawSpread;
        public readonly float SpawnYawJitter;
        public readonly float SpawnUpwardSpreadMin;
        public readonly float SpawnUpwardSpreadMax;
        public readonly float SpawnUpwardJitter;
        public readonly bool TriggerTrackingStartHoldTimeUseRandom;
        public readonly float TriggerTrackingStartHoldTimeMin;
        public readonly float TriggerTrackingStartHoldTimeMax;

        public TrackingPreset(
            string name,
            string tooltip,
            float arcDurationMin,
            float arcDurationMax,
            float arcHorizontalAngle,
            float arcUpwardAngleMin,
            float arcUpwardAngleMax,
            float arcBlendSpeed,
            float trackingTurnTime,
            float spawnYawSpread,
            float spawnYawJitter,
            float spawnUpwardSpreadMin,
            float spawnUpwardSpreadMax,
            float spawnUpwardJitter,
            bool triggerTrackingStartHoldTimeUseRandom,
            float triggerTrackingStartHoldTimeMin,
            float triggerTrackingStartHoldTimeMax)
        {
            Name = name;
            Tooltip = tooltip;
            ArcDurationMin = arcDurationMin;
            ArcDurationMax = arcDurationMax;
            ArcHorizontalAngle = arcHorizontalAngle;
            ArcUpwardAngleMin = arcUpwardAngleMin;
            ArcUpwardAngleMax = arcUpwardAngleMax;
            ArcBlendSpeed = arcBlendSpeed;
            TrackingTurnTime = trackingTurnTime;
            SpawnYawSpread = spawnYawSpread;
            SpawnYawJitter = spawnYawJitter;
            SpawnUpwardSpreadMin = spawnUpwardSpreadMin;
            SpawnUpwardSpreadMax = spawnUpwardSpreadMax;
            SpawnUpwardJitter = spawnUpwardJitter;
            TriggerTrackingStartHoldTimeUseRandom = triggerTrackingStartHoldTimeUseRandom;
            TriggerTrackingStartHoldTimeMin = triggerTrackingStartHoldTimeMin;
            TriggerTrackingStartHoldTimeMax = triggerTrackingStartHoldTimeMax;
        }
    }

    private static readonly BattleConstFloatBinding[] s_BattleConstBaseBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "抛物线子弹Y因素：",
            "保留的 BattleConst 基础参数。当前 C# 代码里没有直接读取这一项，调整后可能不会立刻看到效果。回填后会写入 BattleConst.cs 的默认值。",
            "DefaultParabolicBulletY",
            () => BattleConst.m_ParabolicBulletY,
            value => BattleConst.m_ParabolicBulletY = value),
    };

    private static readonly BattleConstFloatBinding[] s_BattleConstDamageNumberBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "伤害数字偏移：",
            "作用对象：所有伤害数字，包括命中点伤害和效果伤害。含义：在XY的随机偏移范围。值越大，数字越分散。",
            "DefaultDamageNumberOffset",
            () => BattleConst.DamageNumberOffset,
            value => BattleConst.DamageNumberOffset = value)
    };

    private static readonly BattleConstFloatBinding[] s_BattleConstTrackingArcBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "首段弧线最小时间：",
            "作用对象：追踪子弹。生效时机：子弹发射瞬间就进入首段弧线阶段，每发子弹会在最小值和最大值之间随机一个持续时间。值越大，弧线保持越久。",
            "DefaultArcDurationMin",
            () => BattleConst.ArcDurationMin,
            value => BattleConst.ArcDurationMin = value),
        new BattleConstFloatBinding(
            "首段弧线最大时间：",
            "作用对象：追踪子弹。生效时机：子弹发射瞬间就进入首段弧线阶段，每发子弹会在最小值和最大值之间随机一个持续时间。值越大，弧线保持越久。",
            "DefaultArcDurationMax",
            () => BattleConst.ArcDurationMax,
            value => BattleConst.ArcDurationMax = value),
        new BattleConstFloatBinding(
            "首段弧线左右偏转角：",
            "作用对象：追踪子弹。生效时机：发射点起始方向就生效，不是飞一段后才生效。实际会在当前瞄准方向的左右两侧随机偏转，范围是负该值到正该值。值越大，起飞侧偏越明显。",
            "DefaultArcHorizontalAngle",
            () => BattleConst.ArcHorizontalAngle,
            value => BattleConst.ArcHorizontalAngle = value),
        new BattleConstFloatBinding(
            "首段弧线上偏最小角：",
            "作用对象：追踪子弹。生效时机：发射点起始方向就生效。实际会在最小角和最大角之间随机一个上偏角，只会上偏，不会向下偏。值越大，起飞抬头更明显。",
            "DefaultArcUpwardAngleMin",
            () => BattleConst.ArcUpwardAngleMin,
            value => BattleConst.ArcUpwardAngleMin = value),
        new BattleConstFloatBinding(
            "首段弧线上偏最大角：",
            "作用对象：追踪子弹。生效时机：发射点起始方向就生效。实际会在最小角和最大角之间随机一个上偏角，只会上偏，不会向下偏。值越大，起飞抬头更明显。",
            "DefaultArcUpwardAngleMax",
            () => BattleConst.ArcUpwardAngleMax,
            value => BattleConst.ArcUpwardAngleMax = value),
        new BattleConstFloatBinding(
            "首段弧线回正速度：",
            "作用对象：追踪子弹。含义：首段弧线阶段向目标方向收回的速度。值越大，越快回到目标方向；值越小，弧线更明显、保留更久。",
            "DefaultArcBlendSpeed",
            () => BattleConst.ArcBlendSpeed,
            value => BattleConst.ArcBlendSpeed = value),
        new BattleConstFloatBinding(
            "重新追踪转向时间：",
            "作用对象：追踪子弹。含义：目标切换或重新锁定时，从当前朝向转到新目标方向需要的时间。值越大，转向越柔和；值越小，转向越直接。",
            "DefaultTrackingTurnTime",
            () => BattleConst.TrackingTurnTime,
            value => BattleConst.TrackingTurnTime = value),
    };

    private static readonly BattleConstBoolBinding[] s_BattleConstTrackingSpreadBoolBindings = new BattleConstBoolBinding[]
    {
        new BattleConstBoolBinding(
            "多发追踪起始保持时间随机模式：",
            "作用对象：生成时已经分配目标的多发追踪子弹。关闭时按距离线性取值，近距离取最小值，远距离取最大值，中间距离按比例过渡。打开时忽略距离，直接在最小值和最大值之间随机一个保持时间。",
            "DefaultTriggerTrackingStartHoldTimeUseRandom",
            () => BattleConst.TriggerTrackingStartHoldTimeUseRandom,
            value => BattleConst.TriggerTrackingStartHoldTimeUseRandom = value),
    };

    private static readonly BattleConstBoolBinding[] s_BattleConstTowerDefendManualAimBoolBindings = new BattleConstBoolBinding[]
    {
        new BattleConstBoolBinding(
            "自动吸附开关：",
            "作用对象：塔防守卫英雄的手操普攻。打开后会按当前水平射线自动吸附怪物或棒棒糖的高度；当水平射线暂时没有新的吸附目标时，会继续保留上一份俯仰，直到找到下一份可吸附高度。关闭后会完全退回默认俯仰，不再自动修正Y。",
            "DefaultTowerDefendAutoAimEnabled",
            () => BattleConst.TowerDefendAutoAimEnabled,
            value => BattleConst.TowerDefendAutoAimEnabled = value),
    };

    private static readonly BattleConstFloatBinding[] s_BattleConstTowerDefendManualAimFloatBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "自动吸附水平阈值：",
            "作用对象：塔防守卫英雄的手操普攻。含义：只有怪物或棒棒糖中心点到当前水平瞄准线的横向距离不超过这个值时，才允许吸附到它的高度。值越大，越容易吸到旁边目标。",
            "DefaultTowerDefendAutoAimSnapMaxHorizontalDistance",
            () => BattleConst.TowerDefendAutoAimSnapMaxHorizontalDistance,
            value => BattleConst.TowerDefendAutoAimSnapMaxHorizontalDistance = value),
        new BattleConstFloatBinding(
            "自动吸附平滑速度：",
            "作用对象：塔防守卫英雄的手操普攻。含义：当吸附目标的高度发生切换时，瞄准线和俯仰向新目标过渡的速度。值越大，过渡越快；值越小，变化越平缓。",
            "DefaultTowerDefendAutoAimSwitchSmoothSpeed",
            () => BattleConst.TowerDefendAutoAimSwitchSmoothSpeed,
            value => BattleConst.TowerDefendAutoAimSwitchSmoothSpeed = value),
        new BattleConstFloatBinding(
            "棒棒糖结束恢复速度：",
            "作用对象：塔防守卫英雄的手操普攻。生效时机：棒棒糖挑战结束后，角色和瞄准线恢复到常规俯仰的过程。含义：每秒允许恢复的俯仰角度。值越大，回到常规水平越快；值越小，回拉更柔和。",
            "DefaultTowerDefendAutoAimRecoverFromUpgradeSmoothSpeed",
            () => BattleConst.TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed,
            value => BattleConst.TowerDefendAutoAimRecoverFromUpgradeSmoothSpeed = value),
        new BattleConstFloatBinding(
            "无目标瞄准线前向延长距离：",
            "作用对象：塔防守卫英雄的瞄准线。生效时机：没有命中怪物、棒棒糖或喊话目标时。含义：瞄准线最远限制到当前关注物所在Z平面前方多少米，避免无目标时直接画到子弹完整寿命终点。",
            "DefaultTowerDefendNoTargetAimClampForwardOffset",
            () => BattleConst.TowerDefendNoTargetAimClampForwardOffset,
            value => BattleConst.TowerDefendNoTargetAimClampForwardOffset = value),
        new BattleConstFloatBinding(
            "激光下压最低角度：",
            "作用对象：角色2激光的瞄准线、自动吸附与光束方向。含义：方向朝下时最多允许向下俯这么多度，超过则压回该下限。修改后立即生效。值越大，越容易压到近处低目标；值越小，远处低目标越难锁定。",
            "DefaultTowerDefendLaserMaxDownPitchDegrees",
            () => BattleConst.TowerDefendLaserMaxDownPitchDegrees,
            value => BattleConst.TowerDefendLaserMaxDownPitchDegrees = value),
    };

    private static readonly BattleConstFloatBinding[] s_BattleConstTrackingSpreadBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "多发生成左右分散角：",
            "作用对象：一发主子弹触发出来的多发追踪子弹。生效时机：子弹生成瞬间，从发射点开始就按这个角度左右展开，不是飞一段后再分开。含义：单侧扇面总角，值越大整排子弹张开越宽。",
            "DefaultSpawnYawSpread",
            () => BattleConst.SpawnYawSpread,
            value => BattleConst.SpawnYawSpread = value),
        new BattleConstFloatBinding(
            "多发生成左右抖动：",
            "作用对象：多发追踪子弹。含义：在左右分散角基础上再给每发子弹叠加一层随机偏转，让每发子弹不完全整齐。值越大，散布越随机；过大会让相邻子弹叠在一起。",
            "DefaultSpawnYawJitter",
            () => BattleConst.SpawnYawJitter,
            value => BattleConst.SpawnYawJitter = value),
        new BattleConstFloatBinding(
            "多发最小左右间隔角：",
            "作用对象：多发追踪子弹。含义：相邻两发子弹之间至少张开的水平角度，用来兜底避免挤在一起。子弹数量越多时，扇面按分散角摊薄后仍然不会小于这个间隔。",
            "DefaultSpawnMinYawSeparation",
            () => BattleConst.SpawnMinYawSeparation,
            value => BattleConst.SpawnMinYawSeparation = value),
        new BattleConstFloatBinding(
            "单目标扇面放大系数：",
            "作用对象：多发追踪子弹。生效时机：发射时场上的可选目标只有 1 个或没有时。含义：此时子弹最终都会收拢到同一点，把整个扇面按这个系数放大，保证还能看出散开。值越大，单目标时张开越夸张。",
            "DefaultSpawnSingleTargetSpreadScale",
            () => BattleConst.SpawnSingleTargetSpreadScale,
            value => BattleConst.SpawnSingleTargetSpreadScale = value),
        new BattleConstFloatBinding(
            "多发跨轮错开比例：",
            "作用对象：连续快速发弹时，前后多轮触发子弹之间的错开。生效时机：每一轮生成瞬间。含义：按相邻子弹间隔角的比例给整轮一个错位，让后一轮插进前一轮的空隙，避免轮与轮压在同一条线上。0 表示不错开。",
            "DefaultSpawnVolleyYawOffsetRatio",
            () => BattleConst.SpawnVolleyYawOffsetRatio,
            value => BattleConst.SpawnVolleyYawOffsetRatio = value),
        new BattleConstFloatBinding(
            "多发生成上偏最小角：",
            "作用对象：多发追踪子弹。生效时机：子弹生成瞬间，从发射点开始就带着上偏角飞出。边缘子弹更接近最小角，中间子弹更接近最大角，只会上偏不会下偏。",
            "DefaultSpawnUpwardSpreadMin",
            () => BattleConst.SpawnUpwardSpreadMin,
            value => BattleConst.SpawnUpwardSpreadMin = value),
        new BattleConstFloatBinding(
            "多发生成上偏最大角：",
            "作用对象：多发追踪子弹。生效时机：子弹生成瞬间，从发射点开始就带着上偏角飞出。边缘子弹更接近最小角，中间子弹更接近最大角，只会上偏不会下偏。",
            "DefaultSpawnUpwardSpreadMax",
            () => BattleConst.SpawnUpwardSpreadMax,
            value => BattleConst.SpawnUpwardSpreadMax = value),
        new BattleConstFloatBinding(
            "多发生成上偏抖动：",
            "作用对象：多发追踪子弹。含义：在上偏角基础上再增加随机上偏，让每发子弹的抬头角略有差异。值越大，上偏随机性越强。",
            "DefaultSpawnUpwardJitter",
            () => BattleConst.SpawnUpwardJitter,
            value => BattleConst.SpawnUpwardJitter = value),
        new BattleConstFloatBinding(
            "多发追踪最小起始保持时间：",
            "作用对象：生成时已经分配目标的多发追踪子弹。含义：子弹会先按分散后的起射方向保持飞行一小段时间，再逐步收拢到目标。在线性模式下，距离近时取这个最小值；在随机模式下，这个值作为随机下限。",
            "DefaultTriggerTrackingStartHoldTimeMin",
            () => BattleConst.TriggerTrackingStartHoldTimeMin,
            value => BattleConst.TriggerTrackingStartHoldTimeMin = value),
        new BattleConstFloatBinding(
            "多发追踪最大起始保持时间：",
            "作用对象：生成时已经分配目标的多发追踪子弹。含义：子弹会先按分散后的起射方向保持飞行一小段时间，再逐步收拢到目标。在线性模式下，距离远时取这个最大值；在随机模式下，这个值作为随机上限。",
            "DefaultTriggerTrackingStartHoldTimeMax",
            () => BattleConst.TriggerTrackingStartHoldTimeMax,
            value => BattleConst.TriggerTrackingStartHoldTimeMax = value),
        new BattleConstFloatBinding(
            "等待分配平滑回正速度：",
            "作用对象：多发追踪子弹里暂时没分到目标、只朝目标方向引导的子弹。含义：这些子弹在等待期间朝目标收拢的平滑速度，不再是瞬间对准。值越大收拢越快、散开保留越短；值越小越晚收拢、散开越明显。",
            "DefaultSpawnPendingTurnSpeed",
            () => BattleConst.SpawnPendingTurnSpeed,
            value => BattleConst.SpawnPendingTurnSpeed = value),
    };

    private static readonly BattleConstFloatBinding[] s_BattleConstUpgradeChallengeShatterBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "棒棒糖炸开冲量：",
            "作用对象：升级挑战棒棒糖最终炸开后的所有碎片。生效时机：挑战超时触发最终炸开时。值越大，碎片飞散越明显。",
            "DefaultUpgradeChallengeShatterExplosionForce",
            () => BattleConst.UpgradeChallengeShatterExplosionForce,
            value => BattleConst.UpgradeChallengeShatterExplosionForce = value),
        new BattleConstFloatBinding(
            "棒棒糖炸开半径：",
            "作用对象：升级挑战棒棒糖最终炸开后的所有碎片。含义：爆炸力的作用半径。值越大，越远的碎片也更容易被带动。",
            "DefaultUpgradeChallengeShatterExplosionRadius",
            () => BattleConst.UpgradeChallengeShatterExplosionRadius,
            value => BattleConst.UpgradeChallengeShatterExplosionRadius = value),
        new BattleConstFloatBinding(
            "棒棒糖炸开上抬系数：",
            "作用对象：升级挑战棒棒糖最终炸开后的所有碎片。含义：在爆炸力基础上额外增加向上的抬升。值越大，碎片越容易往上弹。",
            "DefaultUpgradeChallengeShatterExplosionUpwardsModifier",
            () => BattleConst.UpgradeChallengeShatterExplosionUpwardsModifier,
            value => BattleConst.UpgradeChallengeShatterExplosionUpwardsModifier = value),
        new BattleConstFloatBinding(
            "棒棒糖炸开旋转力度：",
            "作用对象：升级挑战棒棒糖最终炸开后的所有碎片。含义：给碎片附加的随机转动力度。值越大，碎片旋转越明显。",
            "DefaultUpgradeChallengeShatterExplosionTorque",
            () => BattleConst.UpgradeChallengeShatterExplosionTorque,
            value => BattleConst.UpgradeChallengeShatterExplosionTorque = value),
        new BattleConstFloatBinding(
            "棒棒糖炸开保留时长：",
            "作用对象：整棵升级挑战棒棒糖。含义：进入最终炸开状态后，棒棒糖继续保留多久再整体销毁。值越大，炸开表现停留时间越长。",
            "DefaultUpgradeChallengeShatterDestroyDelay",
            () => BattleConst.UpgradeChallengeShatterDestroyDelay,
            value => BattleConst.UpgradeChallengeShatterDestroyDelay = value),
    };

    private static readonly BattleConstFloatBinding[] s_BattleConstUpgradeChallengeCrackBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "棒棒糖轻度破损值：",
            "作用对象：升级挑战棒棒糖第一次进入破损阶段后的预览表现。生效时机：命中达到轻度破损阶段后。值越大，碎片收缩越明显。",
            "DefaultUpgradeChallengeCrackLightScale",
            () => BattleConst.UpgradeChallengeCrackLightScale,
            value => BattleConst.UpgradeChallengeCrackLightScale = value),
        new BattleConstFloatBinding(
            "棒棒糖中度破损值：",
            "作用对象：升级挑战棒棒糖第二次进入破损阶段后的预览表现。生效时机：命中达到中度破损阶段后。值越大，碎片收缩越明显。",
            "DefaultUpgradeChallengeCrackMediumScale",
            () => BattleConst.UpgradeChallengeCrackMediumScale,
            value => BattleConst.UpgradeChallengeCrackMediumScale = value),
        new BattleConstFloatBinding(
            "棒棒糖重度破损值：",
            "作用对象：升级挑战棒棒糖最终炸开前的重度破损预览表现。生效时机：命中进入最终阶段后。值越大，炸开前的收缩越明显。",
            "DefaultUpgradeChallengeCrackFullScale",
            () => BattleConst.UpgradeChallengeCrackFullScale,
            value => BattleConst.UpgradeChallengeCrackFullScale = value),
    };

    private static readonly BattleConstFloatBinding[] s_BattleConstTowerDefendSpawnBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "同波刷怪间隔：",
            "作用对象：塔防同一波内的分批续刷。生效时机：同一波怪物没有一次性刷完，需要下一批继续补刷时。值越大，同一波内每一批怪之间的停顿越长。它不控制波与波之间的间隔。",
            "DefaultTowerDefendWaveBatchSpawnIntervalSec",
            () => BattleConst.TowerDefendWaveBatchSpawnIntervalSec,
            value => BattleConst.TowerDefendWaveBatchSpawnIntervalSec = value),
    };

    private static readonly BattleConstFloatBinding[] s_BattleConstTowerDefendChallengeEntryBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "棒棒糖下落速度：",
            "作用对象：升级挑战棒棒糖出场阶段。生效时机：棒棒糖从空中落到目标位置的过程。值越大，落下越快。",
            "DefaultTowerDefendUpgradeChallengeDropSpeed",
            () => BattleConst.TowerDefendUpgradeChallengeDropSpeed,
            value => BattleConst.TowerDefendUpgradeChallengeDropSpeed = value),
    };

    private static readonly TrackingPreset[] s_TrackingPresets = new TrackingPreset[]
    {
        new TrackingPreset(
            "平稳追踪版",
            "整体更克制。首段弧线较短，左右与上偏都较小，回正更快，多发分散也更紧。适合先确认命中感和清晰度。",
            0.12f, 0.22f, 5f, 2f, 5f, 14f, 0.22f, 6f, 1f, 0.5f, 2f, 0.3f, false, 0.02f, 0.04f),
        new TrackingPreset(
            "标准分散版",
            "接近当前默认表现。能看出起飞弧线，也能看出多发分散，但不会过于夸张。适合作为日常对比基线。",
            0.18f, 0.35f, 10f, 4f, 9f, 10f, 0.35f, 12f, 3f, 1.5f, 4f, 1f, false, 0.03f, 0.06f),
        new TrackingPreset(
            "弧线展示版",
            "重点看起飞弧线。上偏和持续时间更明显，但多发分散保持中等，适合专门观察追踪子弹的首段飞行质感。",
            0.28f, 0.45f, 14f, 7f, 13f, 6f, 0.42f, 10f, 2f, 2.5f, 6f, 0.8f, false, 0.04f, 0.08f),
        new TrackingPreset(
            "夸张分散版",
            "重点看多发展开。左右和上偏分散明显增强，适合快速观察多发子弹的层次和表演效果。",
            0.22f, 0.40f, 12f, 5f, 10f, 8f, 0.38f, 20f, 5f, 3f, 8f, 2f, false, 0.05f, 0.10f),
        new TrackingPreset(
            "演示极限版",
            "用于快速看极端表现，不建议直接上线。首段弧线和多发分散都比较夸张，方便策划迅速判断上限是否过头。",
            0.30f, 0.55f, 18f, 8f, 16f, 5f, 0.50f, 28f, 8f, 4f, 10f, 3f, false, 0.06f, 0.14f),
    };

    [InitializeOnLoadMethod]
    private static void InitializeBattleConstSessionPersistence()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        EditorApplication.delayCall -= ApplyPersistedBattleConstValues;
        EditorApplication.delayCall += ApplyPersistedBattleConstValues;
        EditorApplication.delayCall -= ApplyPersistedBoneTurnTuningValues;
        EditorApplication.delayCall += ApplyPersistedBoneTurnTuningValues;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode &&
            state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        ApplyPersistedBattleConstValues();
        ApplyPersistedBoneTurnTuningValues();
    }

    [MenuItem("Tools/GMTools")]
    static void AddWindow()
    {
        GMTools window = (GMTools)EditorWindow.GetWindow(typeof(GMTools));
        window.Show();
    }

    private int m_SelectedTab = 0;
    // 页签按职责划分：通用=编辑器本机环境与画面、战斗=全部战斗内控制、
    // 战斗参数=数值调参、真机=GM 桥连接与收发日志、引导=新手引导。
    // 战斗控制只在「战斗」页签出现一次，本机与真机通过同一个命令路由执行。
    private readonly string[] m_TabNames = { "通用", "战斗", "战斗参数", "真机", "引导" };

    private int m_GuideGroup = 0;
    private int m_GmEnterStageId = 1;
    private int m_GmEndlessWave = 0;
    private Vector2 m_ScrollPosition;

    private bool m_DebugMobile = false;
    private bool m_ShowLog = true;

    private static readonly string[] s_DeviceCommandTargetNames = { "本机", "真机", "两者" };
    private string m_DeviceCommandName = "battle.pause";
    private string m_DeviceCommandArgs = "{\"m_Pause\":true}";
    private int m_DeviceLogCount = 30;
    private string m_GmBridgeHostInput = string.Empty;
    private bool m_GmBridgeHostInitialized = false;
    private void OnEnable()
    {
        saveChangesMessage = BattleSettingsUnsavedChangesMessage;
        ApplyPersistedBattleConstValues();
        ApplyPersistedBoneTurnTuningValues();
        RefreshBattleConstSaveState();
    }

    private void OnFocus()
    {
        RefreshBattleConstSaveState();
    }

    public override void SaveChanges()
    {
        if (!TryBackfillBattleSettingsToCode(false))
        {
            return;
        }

        base.SaveChanges();
        RefreshBattleConstSaveState();
    }

    public override void DiscardChanges()
    {
        if (!TryApplyBattleConstDefaultsFromCode() || !TryApplyBoneTurnTuningDefaultsFromCode())
        {
            EditorUtility.DisplayDialog("错误", "读取战斗参数代码默认值失败，无法丢弃本次修改。", "确定");
            return;
        }

        ClearPersistedBattleConstValues();
        ClearPersistedBoneTurnTuningValues();
        base.DiscardChanges();
        RefreshOpenWindowSaveState();
    }

    private static IEnumerable<BattleConstFloatBinding> EnumerateBattleConstFloatBindings()
    {
        foreach (var binding in s_BattleConstBaseBindings) yield return binding;
        foreach (var binding in s_BattleConstDamageNumberBindings) yield return binding;
        foreach (var binding in s_BattleConstTrackingArcBindings) yield return binding;
        foreach (var binding in s_BattleConstTrackingSpreadBindings) yield return binding;
        foreach (var binding in s_BattleConstTowerDefendSpawnBindings) yield return binding;
        foreach (var binding in s_BattleConstTowerDefendManualAimFloatBindings) yield return binding;
        foreach (var binding in s_BattleConstTowerDefendChallengeEntryBindings) yield return binding;
        foreach (var binding in s_BattleConstUpgradeChallengeCrackBindings) yield return binding;
        foreach (var binding in s_BattleConstUpgradeChallengeShatterBindings) yield return binding;
    }

    private static IEnumerable<BattleConstBoolBinding> EnumerateBattleConstBoolBindings()
    {
        foreach (var binding in s_BattleConstTrackingSpreadBoolBindings) yield return binding;
        foreach (var binding in s_BattleConstTowerDefendManualAimBoolBindings) yield return binding;
    }

    private static readonly BoneTurnTuningFloatBinding[] s_BoneTurnTuningFloatBindings =
    {
    };

    private static readonly BoneTurnTuningBoolBinding[] s_BoneTurnTuningBoolBindings =
    {
        new BoneTurnTuningBoolBinding(nameof(BoneTurnTuning.m_BlockNormalAttackWhileTurning), nameof(BoneTurnTuning.m_DefaultBlockNormalAttackWhileTurning), () => BoneTurnTuning.m_BlockNormalAttackWhileTurning, value => BoneTurnTuning.m_BlockNormalAttackWhileTurning = value),
    };

    private static IEnumerable<BoneTurnTuningFloatBinding> EnumerateBoneTurnTuningFloatBindings()
    {
        foreach (var binding in s_BoneTurnTuningFloatBindings) yield return binding;
    }

    private static IEnumerable<BoneTurnTuningBoolBinding> EnumerateBoneTurnTuningBoolBindings()
    {
        foreach (var binding in s_BoneTurnTuningBoolBindings) yield return binding;
    }

    private static string BuildBattleConstFloatSessionKey(string defaultFieldName)
    {
        return BattleConstSessionKeyPrefix + "Float." + defaultFieldName;
    }

    private static string BuildBattleConstBoolSessionKey(string defaultFieldName)
    {
        return BattleConstSessionKeyPrefix + "Bool." + defaultFieldName;
    }

    private static string BuildBoneTurnTuningFloatSessionKey(string fieldName)
    {
        return BoneTurnTuningSessionKeyPrefix + "Float." + fieldName;
    }

    private static string BuildBoneTurnTuningBoolSessionKey(string fieldName)
    {
        return BoneTurnTuningSessionKeyPrefix + "Bool." + fieldName;
    }

    private static void PersistCurrentBattleConstValues()
    {
        SessionState.SetBool(BattleConstSessionMarkerKey, true);
        foreach (var binding in EnumerateBattleConstFloatBindings())
        {
            SessionState.SetFloat(BuildBattleConstFloatSessionKey(binding.DefaultFieldName), binding.Getter());
        }

        foreach (var binding in EnumerateBattleConstBoolBindings())
        {
            SessionState.SetBool(BuildBattleConstBoolSessionKey(binding.DefaultFieldName), binding.Getter());
        }
    }

    private static void ClearPersistedBattleConstValues()
    {
        SessionState.SetBool(BattleConstSessionMarkerKey, false);
    }

    private static void PersistCurrentBoneTurnTuningValues()
    {
        BoneTurnTuning.ClampValues();
        SessionState.SetBool(BoneTurnTuningSessionMarkerKey, true);

        foreach (var binding in EnumerateBoneTurnTuningFloatBindings())
        {
            SessionState.SetFloat(BuildBoneTurnTuningFloatSessionKey(binding.FieldName), binding.Getter());
        }

        foreach (var binding in EnumerateBoneTurnTuningBoolBindings())
        {
            SessionState.SetBool(BuildBoneTurnTuningBoolSessionKey(binding.FieldName), binding.Getter());
        }
    }

    private static void ClearPersistedBoneTurnTuningValues()
    {
        SessionState.SetBool(BoneTurnTuningSessionMarkerKey, false);
    }

    private static void ApplyPersistedBattleConstValues()
    {
        if (!SessionState.GetBool(BattleConstSessionMarkerKey, false))
        {
            return;
        }

        foreach (var binding in EnumerateBattleConstFloatBindings())
        {
            binding.Setter(SessionState.GetFloat(
                BuildBattleConstFloatSessionKey(binding.DefaultFieldName),
                binding.Getter()));
        }

        foreach (var binding in EnumerateBattleConstBoolBindings())
        {
            binding.Setter(SessionState.GetBool(
                BuildBattleConstBoolSessionKey(binding.DefaultFieldName),
                binding.Getter()));
        }

        BattleConst.ClampValues();
        RefreshOpenWindowSaveState();
    }

    private static void ApplyPersistedBoneTurnTuningValues()
    {
        if (!SessionState.GetBool(BoneTurnTuningSessionMarkerKey, false))
        {
            return;
        }

        foreach (var binding in EnumerateBoneTurnTuningFloatBindings())
        {
            binding.Setter(SessionState.GetFloat(
                BuildBoneTurnTuningFloatSessionKey(binding.FieldName),
                binding.Getter()));
        }

        foreach (var binding in EnumerateBoneTurnTuningBoolBindings())
        {
            binding.Setter(SessionState.GetBool(
                BuildBoneTurnTuningBoolSessionKey(binding.FieldName),
                binding.Getter()));
        }

        BoneTurnTuning.ClampValues();
        RefreshOpenWindowSaveState();
    }

    private static void ApplyTrackingPreset(TrackingPreset preset)
    {
        BattleConst.ArcDurationMin = preset.ArcDurationMin;
        BattleConst.ArcDurationMax = preset.ArcDurationMax;
        BattleConst.ArcHorizontalAngle = preset.ArcHorizontalAngle;
        BattleConst.ArcUpwardAngleMin = preset.ArcUpwardAngleMin;
        BattleConst.ArcUpwardAngleMax = preset.ArcUpwardAngleMax;
        BattleConst.ArcBlendSpeed = preset.ArcBlendSpeed;
        BattleConst.TrackingTurnTime = preset.TrackingTurnTime;
        BattleConst.SpawnYawSpread = preset.SpawnYawSpread;
        BattleConst.SpawnYawJitter = preset.SpawnYawJitter;
        BattleConst.SpawnUpwardSpreadMin = preset.SpawnUpwardSpreadMin;
        BattleConst.SpawnUpwardSpreadMax = preset.SpawnUpwardSpreadMax;
        BattleConst.SpawnUpwardJitter = preset.SpawnUpwardJitter;
        BattleConst.TriggerTrackingStartHoldTimeUseRandom = preset.TriggerTrackingStartHoldTimeUseRandom;
        BattleConst.TriggerTrackingStartHoldTimeMin = preset.TriggerTrackingStartHoldTimeMin;
        BattleConst.TriggerTrackingStartHoldTimeMax = preset.TriggerTrackingStartHoldTimeMax;
        MarkBattleConstValuesChanged();
        Debug.Log(string.Format("已套用追踪子弹模板：{0}", preset.Name));
    }

    private static void BackfillBattleConstToCode()
    {
        TryBackfillBattleConstToCode(true);
    }

    private static void BackfillBattleSettingsToCode()
    {
        TryBackfillBattleSettingsToCode(true);
    }

    private static bool TryBackfillBattleSettingsToCode(bool showConfirmation)
    {
        BattleConst.ClampValues();
        BoneTurnTuning.ClampValues();

        if (showConfirmation &&
            !EditorUtility.DisplayDialog("确认", "确认将当前战斗参数回填到代码默认值吗？", "确定", "取消"))
        {
            return false;
        }

        bool backfillBattleConst = HasUnbackfilledBattleConstChanges();
        bool backfillBoneTurnTuning = HasUnbackfilledBoneTurnTuningChanges();

        if (!backfillBattleConst && !backfillBoneTurnTuning)
        {
            Debug.Log("战斗参数默认值没有变化，无需回填。");
            return true;
        }

        if (backfillBattleConst && !TryBackfillBattleConstToCode(false))
        {
            return false;
        }

        if (backfillBoneTurnTuning && !TryBackfillBoneTurnTuningToCode(false))
        {
            return false;
        }

        return true;
    }

    private static bool TryBackfillBattleConstToCode(bool showConfirmation)
    {
        BattleConst.ClampValues();

        if (showConfirmation &&
            !EditorUtility.DisplayDialog("确认", "确认将当前 BattleConst 参数回填到代码默认值吗？", "确定", "取消"))
        {
            return false;
        }

        string fullPath = GetBattleConstFullPath();
        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("错误", "未找到 BattleConst.cs，无法回填。", "确定");
            return false;
        }

        byte[] fileBytes = File.ReadAllBytes(fullPath);
        bool hasUtf8Bom = HasUtf8Bom(fileBytes);
        string fileContent = hasUtf8Bom
            ? Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3)
            : Encoding.UTF8.GetString(fileBytes);

        string updatedContent = fileContent;
        bool replaceSuccess = true;
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BattleConstBaseBindings);
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BattleConstDamageNumberBindings);
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BattleConstTrackingArcBindings);
        replaceSuccess &= TryReplaceConstBools(ref updatedContent, s_BattleConstTrackingSpreadBoolBindings);
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BattleConstTrackingSpreadBindings);
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BattleConstTowerDefendSpawnBindings);
        replaceSuccess &= TryReplaceConstBools(ref updatedContent, s_BattleConstTowerDefendManualAimBoolBindings);
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BattleConstTowerDefendManualAimFloatBindings);
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BattleConstTowerDefendChallengeEntryBindings);
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BattleConstUpgradeChallengeCrackBindings);
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BattleConstUpgradeChallengeShatterBindings);

        if (!replaceSuccess)
        {
            EditorUtility.DisplayDialog("错误", "回填失败，BattleConst.cs 的结构与预期不一致。", "确定");
            Debug.LogError("BattleConst 参数回填失败：BattleConst.cs 的字段格式与工具预期不一致。");
            return false;
        }

        if (updatedContent != fileContent)
        {
            File.WriteAllText(fullPath, updatedContent, hasUtf8Bom ? new UTF8Encoding(true) : new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log(string.Format("已将当前 BattleConst 参数回填到代码默认值：{0}", fullPath));
        }
        else
        {
            Debug.Log("BattleConst 默认参数没有变化，无需回填。");
        }

        ClearPersistedBattleConstValues();
        RefreshOpenWindowSaveState();
        return true;
    }

    private static string GetBattleConstFullPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../", BattleConstAssetPath));
    }

    private static bool TryReadBattleConstFileContent(out string fileContent)
    {
        string fullPath = GetBattleConstFullPath();
        if (!File.Exists(fullPath))
        {
            fileContent = string.Empty;
            return false;
        }

        byte[] fileBytes = File.ReadAllBytes(fullPath);
        bool hasUtf8Bom = HasUtf8Bom(fileBytes);
        fileContent = hasUtf8Bom
            ? Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3)
            : Encoding.UTF8.GetString(fileBytes);
        return true;
    }

    private static bool TryApplyBattleConstDefaultsFromCode()
    {
        if (!TryReadBattleConstDefaultsFromCode(out var floatDefaults, out var boolDefaults))
        {
            return false;
        }

        foreach (var binding in EnumerateBattleConstFloatBindings())
        {
            if (!floatDefaults.TryGetValue(binding.DefaultFieldName, out var value))
            {
                return false;
            }

            binding.Setter(value);
        }

        foreach (var binding in EnumerateBattleConstBoolBindings())
        {
            if (!boolDefaults.TryGetValue(binding.DefaultFieldName, out var value))
            {
                return false;
            }

            binding.Setter(value);
        }

        BattleConst.ClampValues();
        return true;
    }

    private static void BackfillBoneTurnTuningToCode()
    {
        TryBackfillBoneTurnTuningToCode(true);
    }

    private static bool TryBackfillBoneTurnTuningToCode(bool showConfirmation)
    {
        BoneTurnTuning.ClampValues();

        if (showConfirmation &&
            !EditorUtility.DisplayDialog("确认", "确认将当前骨骼转向参数回填到代码默认值吗？", "确定", "取消"))
        {
            return false;
        }

        string fullPath = GetBoneTurnTuningFullPath();
        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("错误", "未找到 BoneTurnTuning.cs，无法回填。", "确定");
            return false;
        }

        byte[] fileBytes = File.ReadAllBytes(fullPath);
        bool hasUtf8Bom = HasUtf8Bom(fileBytes);
        string fileContent = hasUtf8Bom
            ? Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3)
            : Encoding.UTF8.GetString(fileBytes);

        string updatedContent = fileContent;
        bool replaceSuccess = true;
        replaceSuccess &= TryReplaceConstFloats(ref updatedContent, s_BoneTurnTuningFloatBindings);
        replaceSuccess &= TryReplaceConstBools(ref updatedContent, s_BoneTurnTuningBoolBindings);

        if (!replaceSuccess)
        {
            EditorUtility.DisplayDialog("错误", "回填失败，BoneTurnTuning.cs 的结构与预期不一致。", "确定");
            Debug.LogError("骨骼转向参数回填失败：BoneTurnTuning.cs 的字段格式与工具预期不一致。");
            return false;
        }

        if (updatedContent != fileContent)
        {
            File.WriteAllText(fullPath, updatedContent, hasUtf8Bom ? new UTF8Encoding(true) : new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log(string.Format("已将当前骨骼转向参数回填到代码默认值：{0}", fullPath));
        }
        else
        {
            Debug.Log("骨骼转向默认参数没有变化，无需回填。");
        }

        ClearPersistedBoneTurnTuningValues();
        RefreshOpenWindowSaveState();
        return true;
    }

    private static string GetBoneTurnTuningFullPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../", BoneTurnTuningAssetPath));
    }

    private static bool TryReadBoneTurnTuningFileContent(out string fileContent)
    {
        string fullPath = GetBoneTurnTuningFullPath();
        if (!File.Exists(fullPath))
        {
            fileContent = string.Empty;
            return false;
        }

        byte[] fileBytes = File.ReadAllBytes(fullPath);
        bool hasUtf8Bom = HasUtf8Bom(fileBytes);
        fileContent = hasUtf8Bom
            ? Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3)
            : Encoding.UTF8.GetString(fileBytes);
        return true;
    }

    private static bool TryApplyBoneTurnTuningDefaultsFromCode()
    {
        if (!TryReadBoneTurnTuningDefaultsFromCode(out var floatDefaults, out var boolDefaults))
        {
            return false;
        }

        foreach (var binding in EnumerateBoneTurnTuningFloatBindings())
        {
            if (!floatDefaults.TryGetValue(binding.DefaultFieldName, out var value))
            {
                return false;
            }

            binding.Setter(value);
        }

        foreach (var binding in EnumerateBoneTurnTuningBoolBindings())
        {
            if (!boolDefaults.TryGetValue(binding.DefaultFieldName, out var value))
            {
                return false;
            }

            binding.Setter(value);
        }

        BoneTurnTuning.ClampValues();
        return true;
    }

    private static bool TryReadBoneTurnTuningDefaultsFromCode(
        out Dictionary<string, float> floatDefaults,
        out Dictionary<string, bool> boolDefaults)
    {
        floatDefaults = new Dictionary<string, float>();
        boolDefaults = new Dictionary<string, bool>();
        if (!TryReadBoneTurnTuningFileContent(out var fileContent))
        {
            return false;
        }

        foreach (var binding in EnumerateBoneTurnTuningFloatBindings())
        {
            if (!TryReadConstFloat(fileContent, binding.DefaultFieldName, out var value))
            {
                return false;
            }

            floatDefaults[binding.DefaultFieldName] = value;
        }

        foreach (var binding in EnumerateBoneTurnTuningBoolBindings())
        {
            if (!TryReadConstBool(fileContent, binding.DefaultFieldName, out var value))
            {
                return false;
            }

            boolDefaults[binding.DefaultFieldName] = value;
        }

        return true;
    }

    private static bool TryReadBattleConstDefaultsFromCode(
        out Dictionary<string, float> floatDefaults,
        out Dictionary<string, bool> boolDefaults)
    {
        floatDefaults = new Dictionary<string, float>();
        boolDefaults = new Dictionary<string, bool>();
        if (!TryReadBattleConstFileContent(out var fileContent))
        {
            return false;
        }

        foreach (var binding in EnumerateBattleConstFloatBindings())
        {
            if (!TryReadConstFloat(fileContent, binding.DefaultFieldName, out var value))
            {
                return false;
            }

            floatDefaults[binding.DefaultFieldName] = value;
        }

        foreach (var binding in EnumerateBattleConstBoolBindings())
        {
            if (!TryReadConstBool(fileContent, binding.DefaultFieldName, out var value))
            {
                return false;
            }

            boolDefaults[binding.DefaultFieldName] = value;
        }

        return true;
    }

    private static bool TryReadConstFloat(string fileContent, string fieldName, out float value)
    {
        string pattern = string.Format(
            CultureInfo.InvariantCulture,
            @"public const float {0} = ([-+]?(?:\d+\.?\d*|\.\d+))f;",
            fieldName);
        Match match = Regex.Match(fileContent, pattern, RegexOptions.Multiline);
        if (!match.Success)
        {
            value = 0f;
            return false;
        }

        return float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadConstBool(string fileContent, string fieldName, out bool value)
    {
        string pattern = string.Format(
            CultureInfo.InvariantCulture,
            @"public const bool {0} = (true|false);",
            fieldName);
        Match match = Regex.Match(fileContent, pattern, RegexOptions.Multiline);
        if (!match.Success)
        {
            value = false;
            return false;
        }

        return bool.TryParse(match.Groups[1].Value, out value);
    }

    private static void MarkBattleConstValuesChanged()
    {
        BattleConst.ClampValues();
        PersistCurrentBattleConstValues();
        RefreshOpenWindowSaveState();
    }

    private static void MarkBoneTurnTuningValuesChanged()
    {
        BoneTurnTuning.ClampValues();
        PersistCurrentBoneTurnTuningValues();
        RefreshOpenWindowSaveState();
    }

    private static void RefreshOpenWindowSaveState()
    {
        var windows = Resources.FindObjectsOfTypeAll<GMTools>();
        for (int i = 0; i < windows.Length; i++)
        {
            var window = windows[i];
            if (window == null)
            {
                continue;
            }

            window.RefreshBattleConstSaveState();
            window.Repaint();
        }
    }

    private void RefreshBattleConstSaveState()
    {
        saveChangesMessage = BattleSettingsUnsavedChangesMessage;
        hasUnsavedChanges = HasUnbackfilledBattleConstChanges() || HasUnbackfilledBoneTurnTuningChanges();
    }

    private static bool HasUnbackfilledBattleConstChanges()
    {
        if (!TryReadBattleConstDefaultsFromCode(out var floatDefaults, out var boolDefaults))
        {
            return SessionState.GetBool(BattleConstSessionMarkerKey, false);
        }

        foreach (var binding in EnumerateBattleConstFloatBindings())
        {
            if (!floatDefaults.TryGetValue(binding.DefaultFieldName, out var defaultValue) ||
                !Mathf.Approximately(binding.Getter(), defaultValue))
            {
                return true;
            }
        }

        foreach (var binding in EnumerateBattleConstBoolBindings())
        {
            if (!boolDefaults.TryGetValue(binding.DefaultFieldName, out var defaultValue) ||
                binding.Getter() != defaultValue)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUnbackfilledBoneTurnTuningChanges()
    {
        if (!TryReadBoneTurnTuningDefaultsFromCode(out var floatDefaults, out var boolDefaults))
        {
            return SessionState.GetBool(BoneTurnTuningSessionMarkerKey, false);
        }

        foreach (var binding in EnumerateBoneTurnTuningFloatBindings())
        {
            if (!floatDefaults.TryGetValue(binding.DefaultFieldName, out var defaultValue) ||
                !Mathf.Approximately(binding.Getter(), defaultValue))
            {
                return true;
            }
        }

        foreach (var binding in EnumerateBoneTurnTuningBoolBindings())
        {
            if (!boolDefaults.TryGetValue(binding.DefaultFieldName, out var defaultValue) ||
                binding.Getter() != defaultValue)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReplaceConstFloats(ref string fileContent, BattleConstFloatBinding[] bindings)
    {
        bool replaceSuccess = true;
        for (int i = 0; i < bindings.Length; i++)
        {
            replaceSuccess &= TryReplaceConstFloat(ref fileContent, bindings[i].DefaultFieldName, bindings[i].Getter());
        }

        return replaceSuccess;
    }

    private static bool TryReplaceConstFloat(ref string fileContent, string fieldName, float value)
    {
        string pattern = string.Format(
            CultureInfo.InvariantCulture,
            @"(public const float {0} = )[-+]?(?:\d+\.?\d*|\.\d+)f;",
            fieldName);
        Regex regex = new Regex(pattern, RegexOptions.Multiline);
        Match match = regex.Match(fileContent);
        if (!match.Success)
        {
            return false;
        }

        string replacement = match.Groups[1].Value + FormatFloatLiteral(value) + ";";
        fileContent = regex.Replace(fileContent, replacement, 1);
        return true;
    }

    private static bool TryReplaceConstBools(ref string fileContent, BattleConstBoolBinding[] bindings)
    {
        bool replaceSuccess = true;
        for (int i = 0; i < bindings.Length; i++)
        {
            replaceSuccess &= TryReplaceConstBool(ref fileContent, bindings[i].DefaultFieldName, bindings[i].Getter());
        }

        return replaceSuccess;
    }

    private static bool TryReplaceConstBool(ref string fileContent, string fieldName, bool value)
    {
        string pattern = string.Format(
            CultureInfo.InvariantCulture,
            @"(public const bool {0} = )(true|false);",
            fieldName);
        Regex regex = new Regex(pattern, RegexOptions.Multiline);
        Match match = regex.Match(fileContent);
        if (!match.Success)
        {
            return false;
        }

        string replacement = match.Groups[1].Value + (value ? "true" : "false") + ";";
        fileContent = regex.Replace(fileContent, replacement, 1);
        return true;
    }

    private static bool TryReplaceConstFloats(ref string fileContent, BoneTurnTuningFloatBinding[] bindings)
    {
        bool replaceSuccess = true;
        for (int i = 0; i < bindings.Length; i++)
        {
            replaceSuccess &= TryReplaceConstFloat(ref fileContent, bindings[i].DefaultFieldName, bindings[i].Getter());
        }

        return replaceSuccess;
    }

    private static bool TryReplaceConstBools(ref string fileContent, BoneTurnTuningBoolBinding[] bindings)
    {
        bool replaceSuccess = true;
        for (int i = 0; i < bindings.Length; i++)
        {
            replaceSuccess &= TryReplaceConstBool(ref fileContent, bindings[i].DefaultFieldName, bindings[i].Getter());
        }

        return replaceSuccess;
    }

    private static string FormatFloatLiteral(float value)
    {
        return value.ToString("0.0######", CultureInfo.InvariantCulture) + "f";
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF;
    }

    private bool DrawBattleConstSection(string title, BattleConstFloatBinding[] bindings)
    {
        bool changed = false;
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        for (int i = 0; i < bindings.Length; i++)
        {
            float currentValue = bindings[i].Getter();
            float nextValue = EditorGUILayout.FloatField(new GUIContent(bindings[i].Label, bindings[i].Tooltip), currentValue);
            if (!Mathf.Approximately(currentValue, nextValue))
            {
                bindings[i].Setter(nextValue);
                changed = true;
            }
        }
        GUILayout.EndVertical();
        return changed;
    }

    private bool DrawBattleConstSection(string title, BattleConstBoolBinding[] boolBindings, BattleConstFloatBinding[] floatBindings)
    {
        bool changed = false;
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        for (int i = 0; i < boolBindings.Length; i++)
        {
            bool currentValue = boolBindings[i].Getter();
            bool nextValue = EditorGUILayout.Toggle(new GUIContent(boolBindings[i].Label, boolBindings[i].Tooltip), currentValue);
            if (currentValue != nextValue)
            {
                boolBindings[i].Setter(nextValue);
                changed = true;
            }
        }

        for (int i = 0; i < floatBindings.Length; i++)
        {
            float currentValue = floatBindings[i].Getter();
            float nextValue = EditorGUILayout.FloatField(new GUIContent(floatBindings[i].Label, floatBindings[i].Tooltip), currentValue);
            if (!Mathf.Approximately(currentValue, nextValue))
            {
                floatBindings[i].Setter(nextValue);
                changed = true;
            }
        }

        GUILayout.EndVertical();
        return changed;
    }

    private void DrawBattleConstTooltipBox()
    {
        string tooltip = GUI.tooltip;
        if (string.IsNullOrEmpty(tooltip))
        {
            tooltip = "将鼠标停在参数名、模板按钮或操作按钮上查看详细说明。说明会写明作用对象、生效时机，以及调大调小后的表现。";
        }

        EditorGUILayout.HelpBox(tooltip, MessageType.Info);
    }

    private void DrawTrackingPresetSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("快捷模板", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("一键覆盖追踪飞行参数、追踪分散参数和起始保持时间模式，方便策划快速对比效果。模板只改当前运行时值，不会自动回填源码。");

        const int columnCount = 2;
        for (int i = 0; i < s_TrackingPresets.Length; i += columnCount)
        {
            GUILayout.BeginHorizontal();
            for (int j = 0; j < columnCount; j++)
            {
                int presetIndex = i + j;
                if (presetIndex >= s_TrackingPresets.Length)
                {
                    GUILayout.FlexibleSpace();
                    continue;
                }

                TrackingPreset preset = s_TrackingPresets[presetIndex];
                if (GUILayout.Button(new GUIContent(preset.Name, preset.Tooltip), GUILayout.Height(34)))
                {
                    ApplyTrackingPreset(preset);
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    private void DrawBattleConstSettings()
    {
        DrawGmBridgeHostSection();

        GUILayout.Space(8);

        bool changed = false;
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("当前修改会影响对应的运行时表现，回填会修改 BattleConst.cs 或 BoneTurnTuning.cs 中的默认值。");
        DrawTrackingPresetSection();
        changed |= DrawBattleConstSection("基础参数", s_BattleConstBaseBindings);
        changed |= DrawBattleConstSection("伤害数字偏移参数", s_BattleConstDamageNumberBindings);
        changed |= DrawBattleConstSection("追踪飞行参数（发射起始弧线）", s_BattleConstTrackingArcBindings);
        changed |= DrawBattleConstSection("追踪分散参数（多发生成瞬间）", s_BattleConstTrackingSpreadBoolBindings, s_BattleConstTrackingSpreadBindings);
        changed |= DrawBattleConstSection("塔防波内刷怪参数", s_BattleConstTowerDefendSpawnBindings);
        changed |= DrawBattleConstSection("塔防自动吸附参数", s_BattleConstTowerDefendManualAimBoolBindings, s_BattleConstTowerDefendManualAimFloatBindings);
        changed |= DrawBattleConstSection("塔防棒棒糖出场参数", s_BattleConstTowerDefendChallengeEntryBindings);
        changed |= DrawBattleConstSection("升级挑战棒棒糖阶段破损值", s_BattleConstUpgradeChallengeCrackBindings);
        changed |= DrawBattleConstSection("升级挑战棒棒糖炸开参数", s_BattleConstUpgradeChallengeShatterBindings);
        if (changed)
        {
            MarkBattleConstValuesChanged();
        }
        DrawBattleConstTooltipBox();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("重置 BattleConst 参数", "将当前运行时参数恢复为 BattleConst.cs 中的默认值，不修改源码。")))
        {
            if (!TryApplyBattleConstDefaultsFromCode())
            {
                EditorUtility.DisplayDialog("错误", "读取 BattleConst.cs 默认值失败，无法重置。", "确定");
            }
            else
            {
                ClearPersistedBattleConstValues();
            }
            RefreshOpenWindowSaveState();
        }
        if (GUILayout.Button(new GUIContent("回填到 BattleConst 默认值", "将当前面板里的 BattleConst 参数写回 BattleConst.cs 的默认值常量，并刷新资源。")))
        {
            BackfillBattleConstToCode();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        DrawBoneTurnTuningSection();
    }

    // 真机连接 IP：真机侧连的是开发机的局域网 IP，写死在 GmBridgeSettings.m_DefaultHost。
    // 与 BattleConst 一致：面板先改值，再回填源码；改完需要重新出一次 Development 包。
    private void DrawGmBridgeHostSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("真机连接 IP（GM 桥）", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "真机连的是当前开发机的局域网 IP。不想重新打包时，也可以把 GmBridgeHost.txt（首行 IP，第二行可选端口）push 到真机 persistentDataPath。");

        string codeHost = GmBridgeSettings.m_DefaultHost;
        if (!m_GmBridgeHostInitialized)
        {
            m_GmBridgeHostInput = codeHost;
            m_GmBridgeHostInitialized = true;
        }

        m_GmBridgeHostInput = EditorGUILayout.TextField("局域网 IP：", m_GmBridgeHostInput);
        EditorGUILayout.LabelField("代码当前值：", codeHost);

        string localIp = GmBridgeEditorSettings.ReadLocalIpv4();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("使用本机当前 IP", "把本机识别到的局域网 IP 填到上面的输入框。")))
        {
            if (string.IsNullOrEmpty(localIp))
            {
                Debug.LogWarning("[GMTools] 未识别到本机局域网 IP，请手动填写。");
            }
            else
            {
                m_GmBridgeHostInput = localIp;
            }
        }

        if (GUILayout.Button(new GUIContent("回填到代码", "把上面的 IP 写进 GmBridgeSettings.cs 的 m_DefaultHost。")))
        {
            BackfillGmBridgeHostToCode(m_GmBridgeHostInput);
        }
        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(m_GmBridgeHostInput) && m_GmBridgeHostInput != codeHost)
        {
            EditorGUILayout.HelpBox("输入框里的 IP 与代码不一致，点「回填到代码」后才生效，并需要重新出 Development 包。", MessageType.Info);
        }

        if (!string.IsNullOrEmpty(localIp) && codeHost != localIp)
        {
            EditorGUILayout.HelpBox(
                string.Format("代码里写的是 {0}，本机当前局域网 IP 是 {1}。", codeHost, localIp),
                MessageType.Warning);
        }
        GUILayout.EndVertical();
    }

    private static void BackfillGmBridgeHostToCode(string host)
    {
        host = host != null ? host.Trim() : string.Empty;
        if (string.IsNullOrEmpty(host) || host.IndexOf('"') >= 0)
        {
            EditorUtility.DisplayDialog("错误", "局域网 IP 不能为空，也不能包含引号。", "确定");
            return;
        }

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../", GmBridgeSettingsAssetPath));
        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("错误", "未找到 GmBridgeSettings.cs，无法回填真机连接 IP。", "确定");
            return;
        }

        byte[] fileBytes = File.ReadAllBytes(fullPath);
        bool hasUtf8Bom = HasUtf8Bom(fileBytes);
        string fileContent = hasUtf8Bom
            ? Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3)
            : Encoding.UTF8.GetString(fileBytes);

        string updatedContent = fileContent;
        if (!TryReplaceConstString(ref updatedContent, "m_DefaultHost", host))
        {
            EditorUtility.DisplayDialog("错误", "回填失败，GmBridgeSettings.cs 里找不到 m_DefaultHost 常量。", "确定");
            Debug.LogError("真机连接 IP 回填失败：GmBridgeSettings.cs 缺少 m_DefaultHost 常量。");
            return;
        }

        if (updatedContent == fileContent)
        {
            Debug.Log("真机连接 IP 与代码一致，无需回填。");
            return;
        }

        File.WriteAllText(fullPath, updatedContent, hasUtf8Bom ? new UTF8Encoding(true) : new UTF8Encoding(false));
        AssetDatabase.Refresh();
        Debug.Log(string.Format("已将真机连接 IP 回填到 GmBridgeSettings.cs：{0}", host));
    }

    private static bool TryReplaceConstString(ref string fileContent, string fieldName, string value)
    {
        string pattern = string.Format(
            CultureInfo.InvariantCulture,
            "(public const string {0} = )\"[^\"]*\";",
            fieldName);
        Regex regex = new Regex(pattern, RegexOptions.Multiline);
        Match match = regex.Match(fileContent);
        if (!match.Success)
        {
            return false;
        }

        string replacement = match.Groups[1].Value + "\"" + value.Replace("$", "$$") + "\";";
        fileContent = regex.Replace(fileContent, replacement, 1);
        return true;
    }


    void OnGUI()
    {
        m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);

        m_SelectedTab = GUILayout.Toolbar(m_SelectedTab, m_TabNames);
        switch (m_SelectedTab)
        {
            case 0:
                DrawGeneralTab();
                break;
            case 1:
                DrawBattleTab();
                break;
            case 2:
                DrawBattleConstSettings();
                break;
            case 3:
                DrawDeviceTab();
                break;
            case 4:
                DrawGuideTab();
                break;
        }

        GUILayout.EndScrollView();
    }

    private void DrawGeneralTab()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("编辑器本机环境", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("以下开关只作用于编辑器进程；真机连接在「真机」页签，战斗内控制在「战斗」页签。");

        m_ShowLog = EditorGUILayout.Toggle("日志开关：", m_ShowLog);
        Debug.unityLogger.logEnabled = m_ShowLog;

        m_DebugMobile = EditorGUILayout.Toggle("真机调试(Sqlite、AB模式、原生C#)", m_DebugMobile);
        RenderAPI.m_DebugMobile = m_DebugMobile;

        if (GUILayout.Button("设置FixedTimeStamp"))
        {
            Time.fixedDeltaTime = 0.01f;
        }

        if (GUILayout.Button("清理所有数据"))
        {
            if (EditorUtility.DisplayDialog("警告", "确认需要删除所有的PlayerPrefs数据？", "Ok", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                Debug.Log("本地所有PlayerPrefs数据已被清空");
            }
            else
            {
                Debug.Log("操作取消");
            }
        }
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("画面与截图（走 GM 桥）", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("命令目标由「战斗」页签顶部的 本机/真机/两者 决定；要改真机帧率或画质时先切到真机。", EditorStyles.miniLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("帧率 60"))
        {
            GmBridgeEditorRouter.Invoke("app.frameRate", "{\"m_Fps\":60}");
        }

        if (GUILayout.Button("帧率 45"))
        {
            GmBridgeEditorRouter.Invoke("app.frameRate", "{\"m_Fps\":45}");
        }

        if (GUILayout.Button("不限帧率"))
        {
            GmBridgeEditorRouter.Invoke("app.frameRate", "{\"m_Fps\":-1}");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("低配画质", "下发 app.quality，画质档位 0")))
        {
            GmBridgeEditorRouter.Invoke("app.quality", "{\"m_Level\":0}");
        }

        if (GUILayout.Button(new GUIContent("截图", "下发 app.screenshot，图片保存到运行侧的 persistentDataPath")))
        {
            GmBridgeEditorRouter.Invoke("app.screenshot", "{}");
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.EndVertical();
    }

    // GM 桥的链路与诊断：连接参数、连接状态、设备状态快照、手动命令与收发日志。
    // 战斗相关按钮统一在「战斗」页签，本页不再重复。
    private void DrawDeviceTab()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("真机连接（GM 桥）", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("仅 Development 包生效；真机连的 IP 在「战斗参数」页签配置（写入 GmBridgeSettings.m_DefaultHost）。");
        EditorGUILayout.LabelField("战斗内控制按钮统一在「战斗」页签，与本页共用同一个命令路由。", EditorStyles.miniLabel);

        bool isEnabled = GmBridgeEditorSettings.ReadIsEnabled();
        bool nextEnabled = EditorGUILayout.Toggle("启用监听：", isEnabled);
        if (nextEnabled != isEnabled)
        {
            GmBridgeEditorSettings.SetIsEnabled(nextEnabled);
            if (nextEnabled)
            {
                GmBridgeEditorServer.Start();
            }
            else
            {
                GmBridgeEditorServer.Stop();
            }
        }

        int currentPort = GmBridgeEditorSettings.ReadPort();
        int nextPort = EditorGUILayout.IntField("监听端口：", currentPort);
        if (nextPort != currentPort)
        {
            GmBridgeEditorSettings.SetPort(nextPort);
            if (GmBridgeEditorSettings.ReadIsEnabled())
            {
                GmBridgeEditorServer.Stop();
                GmBridgeEditorServer.Start();
            }
        }

        string localIp = GmBridgeEditorSettings.ReadLocalIpv4();
        string endpoint = string.Format(
            "{0}:{1}",
            string.IsNullOrEmpty(localIp) ? "<本机局域网IP>" : localIp,
            GmBridgeEditorSettings.ReadPort());
        EditorGUILayout.LabelField("本机局域网 IP：", string.IsNullOrEmpty(localIp) ? "未识别" : localIp);
        EditorGUILayout.LabelField("真机侧连接地址：", endpoint);
        EditorGUILayout.LabelField(
            "不想重新打包换 IP：把 GmBridgeHost.txt（首行 IP）push 到真机 persistentDataPath。",
            EditorStyles.miniLabel);

        string configuredHost = GmBridgeSettings.m_DefaultHost;
        if (!string.IsNullOrEmpty(localIp) && configuredHost != localIp)
        {
            EditorGUILayout.HelpBox(
                string.Format(
                    "真机连的 IP 写死为 {0}，本机当前是 {1}。刚换了网络或电脑时，请打开连接配置改 m_DefaultHost 后再出 Development 包。",
                    configuredHost,
                    localIp),
                MessageType.Warning);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("复制地址", "复制真机要连的 IP:端口，方便填进 GmBridgeSettings 或排错。")))
        {
            EditorGUIUtility.systemCopyBuffer = endpoint;
        }

        if (GUILayout.Button(new GUIContent("打开连接配置", "打开 GmBridgeSettings.cs，换电脑或换 IP 时改 m_DefaultHost。")))
        {
            var settingsScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Game/DevTools/GmBridge/GmBridgeSettings.cs");
            if (settingsScript != null)
            {
                AssetDatabase.OpenAsset(settingsScript);
            }
        }
        GUILayout.EndHorizontal();

        bool isSelfTest = EditorPrefs.GetBool(GmBridgeSettings.m_EditorSelfTestKey, false);
        bool nextSelfTest = EditorGUILayout.Toggle(
            new GUIContent("本机 Play 模拟真机", "没有真机时验证链路：编辑器 Play 模式下的客户端连 127.0.0.1。"),
            isSelfTest);
        if (nextSelfTest != isSelfTest)
        {
            EditorPrefs.SetBool(GmBridgeSettings.m_EditorSelfTestKey, nextSelfTest);
        }

        string connectState = GmBridgeEditorServer.ReadIsListening()
            ? (GmBridgeEditorServer.ReadIsClientConnected() ? "已连接真机" : "监听中，等待真机")
            : "未启动";
        EditorGUILayout.LabelField("连接状态：", connectState);
        if (GmBridgeEditorServer.ReadIsListening() && !GmBridgeEditorServer.ReadIsClientConnected())
        {
            EditorGUILayout.HelpBox(
                "等待真机接入。连不上时依次检查：手机与电脑连同一 WiFi（不要用 4G）；真机是 Development 构建；Windows 防火墙允许 Unity Editor 访问网络；真机里写死的 IP 与本机局域网 IP 一致。",
                MessageType.Info);
        }
        else if (GmBridgeEditorServer.ReadIsClientConnected())
        {
            double secondsSinceReceive = GmBridgeEditorServer.ReadSecondsSinceLastReceive();
            if (secondsSinceReceive >= 0d && secondsSinceReceive > 3d)
            {
                EditorGUILayout.LabelField("最近心跳：", secondsSinceReceive.ToString("F1") + " 秒前");
            }
        }
        if (GmBridgeEditorServer.ReadIsClientConnected())
        {
            EditorGUILayout.LabelField("设备信息：", GmBridgeEditorServer.ReadDeviceSummary());
            DrawDeviceStateSection();
        }

        string lastError = GmBridgeEditorServer.ReadLastError();
        if (!string.IsNullOrEmpty(lastError))
        {
            EditorGUILayout.HelpBox(lastError, MessageType.Error);
        }

        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("手动命令", EditorStyles.boldLabel);
        m_DeviceCommandName = EditorGUILayout.TextField("命令名：", m_DeviceCommandName);
        m_DeviceCommandArgs = EditorGUILayout.TextField("命令参数：", m_DeviceCommandArgs);
        if (GUILayout.Button("发送命令"))
        {
            GmBridgeEditorRouter.Invoke(m_DeviceCommandName, m_DeviceCommandArgs);
        }

        EditorGUILayout.LabelField("已支持命令：", GmBridgeCommandDispatcher.ReadCommandNames());
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("收发日志", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("条数", GUILayout.Width(30f));
        m_DeviceLogCount = EditorGUILayout.IntField(m_DeviceLogCount, GUILayout.Width(40f));
        if (GUILayout.Button(new GUIContent("拉取运行侧日志", "下发 log.tail，取运行侧最近 N 条日志"), GUILayout.Width(120f)))
        {
            GmBridgeEditorRouter.Invoke("log.tail", "{\"m_Count\":" + m_DeviceLogCount + "}");
        }

        if (GUILayout.Button("清空", GUILayout.Width(60f)))
        {
            GmBridgeEditorServer.ClearLogs();
        }
        GUILayout.EndHorizontal();

        string[] logs = GmBridgeEditorServer.ReadLogsCopy();
        int logStart = Mathf.Max(0, logs.Length - 14);
        for (int i = logStart; i < logs.Length; i++)
        {
            EditorGUILayout.LabelField(logs[i], EditorStyles.miniLabel);
        }
        GUILayout.EndVertical();

        if ((GmBridgeEditorServer.ReadIsClientConnected() || GmBridgeEditorServer.ReadIsListening()) && hasFocus)
        {
            Repaint();
        }
    }

    private void DrawDeviceStateSection()
    {
        var state = GmBridgeEditorServer.ReadDeviceState();
        if (state == null)
        {
            EditorGUILayout.LabelField("真机状态：", "等待状态推送...");
            return;
        }

        EditorGUILayout.LabelField("真机状态：", string.Format(
            "场景 {0} | 战斗 {1}/{2} | 波次 {3} | 暂停 {4} | 冰冻 {5} | 团队经验 {6}% | FPS {7}/{8}",
            state.m_Scene,
            state.m_Battle,
            state.m_GameMode,
            state.m_Wave,
            state.m_Paused ? "是" : "否",
            state.m_FrozenSeatCount,
            state.m_TeamExpPercent,
            state.m_Fps,
            state.m_TargetFrameRate));
    }

    // 战斗内控制：本机与真机走同一套 GM 命令，顶部只切换命令目标；
    // 同一能力只出现一次，不再区分「本机版 / 真机版」两套按钮。
    private void DrawBattleTab()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("控制目标", EditorStyles.boldLabel);
        var target = GmBridgeEditorRouter.ReadTarget();
        var nextTarget = (GmBridgeControlTarget)GUILayout.Toolbar((int)target, s_DeviceCommandTargetNames);
        if (nextTarget != target)
        {
            GmBridgeEditorRouter.SetTarget(nextTarget);
            target = nextTarget;
        }

        EditorGUILayout.LabelField("命令通道：", BuildCommandChannelSummary(target), EditorStyles.miniLabel);
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("进入关卡", EditorStyles.boldLabel);
        m_GmEnterStageId = EditorGUILayout.IntField("关卡 ID：", m_GmEnterStageId);
        if (GUILayout.Button(new GUIContent("进入关卡", "通过关卡 ID 直接进入战斗，跳过大厅流程，按章节模式进入。")))
        {
            GmBridgeEditorRouter.Invoke("stage.enter", "{\"m_StageId\":" + m_GmEnterStageId + "}");
        }
        GUILayout.EndVertical();

        GUILayout.Space(8);

        DrawBattleFlowSection();

        GUILayout.Space(8);

        DrawBattleCheatToggleSection();

        GUILayout.Space(8);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("波次与刷怪", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("快速刷下一波", "只跳过下一波等待时间，不清理当前场上怪物，允许前后两波怪物同时在场。")))
        {
            GmBridgeEditorRouter.Invoke("battle.wave.next", "{}");
        }

        if (GUILayout.Button(new GUIContent("刷当前关卡 Boss", "优先尝试刷出当前波次配置里的 Boss；如果当前波次没有 Boss，则回退到当前关卡配置中的第一个 Boss。不会推进正式波次。")))
        {
            GmBridgeEditorRouter.Invoke("battle.boss.spawn", "{}");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        EditorGUILayout.LabelField("无尽模式", EditorStyles.boldLabel);
        DrawEndlessWaveDebugSection();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("升级挑战", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("团队经验拉满", "把当前塔防战斗的团队经验直接补到升级挑战触发所需上限。")))
        {
            GmBridgeEditorRouter.Invoke("battle.challenge.exp.max", "{}");
        }

        if (GUILayout.Button(new GUIContent("棒棒糖直接炸开", "让当前升级挑战中的棒棒糖直接进入最终炸开流程。")))
        {
            GmBridgeEditorRouter.Invoke("battle.challenge.shatter", "{}");
        }

        if (GUILayout.Button(new GUIContent("结束升级挑战", "强制结束当前升级挑战。")))
        {
            GmBridgeEditorRouter.Invoke("battle.challenge.finish", "{}");
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("冰冻调试（buff 冰冻 923000，与 GM 暂停无关）", EditorStyles.boldLabel);
        if (GUILayout.Button(new GUIContent("冰冻全部玩家", "给当前战斗的全部守卫英雄挂上冰冻buff(923000)，冻结3秒，走子弹命中同一条加buff链路。")))
        {
            GmBridgeEditorRouter.Invoke("cheat.freeze.all", "{}");
        }
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("结算", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("快速过关"))
        {
            GmBridgeEditorRouter.Invoke("battle.finish", "{\"m_Success\":true}");
        }

        if (GUILayout.Button("快速失败"))
        {
            GmBridgeEditorRouter.Invoke("battle.finish", "{\"m_Success\":false}");
        }

        if (GUILayout.Button(new GUIContent("基地血量归零", "只把基地血量置零，不触发结算。")))
        {
            GmBridgeEditorRouter.Invoke("battle.base.break", "{}");
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        DrawBoneRemoteDebugSection();

        GUILayout.EndVertical();
    }

    private static string BuildCommandChannelSummary(GmBridgeControlTarget target)
    {
        if (target == GmBridgeControlTarget.Local)
        {
            return "本机执行（命令直接在编辑器 Play 中运行）";
        }

        if (GmBridgeEditorServer.ReadIsClientConnected())
        {
            return "真机执行（" + GmBridgeEditorServer.ReadDeviceSummary() + "）";
        }

        if (target == GmBridgeControlTarget.Both)
        {
            return "真机未连接：真机侧跳过，本机照常执行";
        }

        return "真机未连接：命令退回本机执行，请到「真机」页签检查监听与 IP";
    }

    // 战斗页签控件统一数据源口径：
    // - 目标为本机，或目标为真机但真机未连接：读本机 Play（与 GmBridgeEditorRouter 的命令回退口径一致）
    // - 真机已连接：只读真机推送状态；state 还没到时标记 pending 并禁用控件，不回退本机，
    //   否则真机重连的空窗里面板会在两套数据之间来回跳。
    private struct BattleControlSource
    {
        public GmBridgeStateDto m_DeviceState;
        public IBattle m_LocalBattle;
        public bool m_IsDevicePending;
        public bool m_TargetIsDevice;
    }

    private static BattleControlSource ReadBattleControlSource()
    {
        var source = new BattleControlSource();
        source.m_TargetIsDevice = GmBridgeEditorRouter.ReadTarget() != GmBridgeControlTarget.Local;
        if (source.m_TargetIsDevice && GmBridgeEditorServer.ReadIsClientConnected())
        {
            source.m_DeviceState = GmBridgeEditorServer.ReadDeviceState();
            source.m_IsDevicePending = source.m_DeviceState == null;
            return source;
        }

        source.m_LocalBattle = BattleManager.GetBattle();
        return source;
    }

    // 战斗流程：暂停开关 + 三种“停”的只读回显（GM 暂停 / 挂起 / buff 冰冻）。
    private void DrawBattleFlowSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("战斗流程", EditorStyles.boldLabel);

        var source = ReadBattleControlSource();
        bool isPaused = source.m_DeviceState != null
            ? source.m_DeviceState.m_Paused
            : (source.m_LocalBattle != null && source.m_LocalBattle.GM_IsPause());
        EditorGUI.BeginDisabledGroup(source.m_IsDevicePending);
        bool nextPaused = EditorGUILayout.Toggle(
            new GUIContent("战斗暂停：", "战斗逻辑时间不走动，与 buff 冰冻（923000）不是一回事。"), isPaused);
        EditorGUI.EndDisabledGroup();
        if (!source.m_IsDevicePending && nextPaused != isPaused)
        {
            GmBridgeEditorRouter.Invoke("battle.pause", nextPaused ? "{\"m_Pause\":true}" : "{\"m_Pause\":false}");
        }

        DrawBattleStopStateSection();
        GUILayout.EndVertical();
    }

    // 三个作弊开关统一用正向语义（可移动 / 可用），与运行侧 state 字段方向一致，
    // 避免同一个能力在本机写“不移动”、在真机写“可移动”两套写法。
    private void DrawBattleCheatToggleSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("战斗作弊开关", EditorStyles.boldLabel);

        var source = ReadBattleControlSource();
        EditorGUI.BeginDisabledGroup(source.m_IsDevicePending);

        bool invincible = source.m_DeviceState != null ? source.m_DeviceState.m_Invincible : DamageCal.m_GM_Invincible;
        bool nextInvincible = EditorGUILayout.Toggle("怪物无敌：", invincible);
        if (nextInvincible != invincible)
        {
            GmBridgeEditorRouter.Invoke("cheat.invincible", nextInvincible ? "{\"m_Enabled\":true}" : "{\"m_Enabled\":false}");
        }

        bool monsterMove = source.m_DeviceState != null ? source.m_DeviceState.m_MonsterMoveEnabled : !State_Move.m_GM_DisableMove;
        bool nextMonsterMove = EditorGUILayout.Toggle("怪物可移动：", monsterMove);
        if (nextMonsterMove != monsterMove)
        {
            GmBridgeEditorRouter.Invoke("cheat.monster.move", nextMonsterMove ? "{\"m_Enabled\":true}" : "{\"m_Enabled\":false}");
        }

        bool autoSkill = source.m_DeviceState != null ? source.m_DeviceState.m_AutoSkillEnabled : !TowerDefendBattleSpawer.m_GMDisableAutoSkill;
        bool nextAutoSkill = EditorGUILayout.Toggle("自动技能可用：", autoSkill);
        if (nextAutoSkill != autoSkill)
        {
            GmBridgeEditorRouter.Invoke("cheat.auto.skill", nextAutoSkill ? "{\"m_Enabled\":true}" : "{\"m_Enabled\":false}");
        }

        EditorGUI.EndDisabledGroup();
        GUILayout.EndVertical();
    }

    // 三种“停”的只读回显：GM 暂停（时间不走动）/ 挂起（SetSuspended）/ buff 冰冻（923000）。
    // 调试时先看这里，避免把 GM 暂停、挂起和冰冻混成一种。
    // 固定输出 4 行：数据源在“真机推送 / 本机 Play / 无战斗”之间切时只换文案不换行数。
    // 之前按数据源分支输出 1/2/3 行，一旦真机重连（m_StateJson 被清空）就会短暂回退去读本机战斗，
    // 行数和内容一起变，整个面板高度跟着抖。
    private void DrawBattleStopStateSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("停止状态（只读）", EditorStyles.boldLabel);

        string pauseText = "—";
        string suspendText = "—";
        string frozenText = "—";
        string sourceText;

        var source = ReadBattleControlSource();
        if (source.m_DeviceState != null)
        {
            pauseText = source.m_DeviceState.m_Paused ? "是" : "否";
            frozenText = string.Format("{0} 个", source.m_DeviceState.m_FrozenSeatCount);
            sourceText = "真机状态推送";
        }
        else if (source.m_IsDevicePending)
        {
            // 真机已连接但 state 还没到：不回退去读本机战斗，否则行数和内容会跟着连接状态抖。
            sourceText = "真机已连接，等待状态…";
        }
        else if (source.m_LocalBattle != null)
        {
            var spawer = source.m_LocalBattle.ReadBattleSpawer();
            var heroes = spawer != null ? spawer.ReadHeroes() : null;
            int heroCount = heroes != null ? heroes.Count : 0;
            int suspendedCount = 0;
            int frozenCount = 0;
            for (int i = 0; i < heroCount; i++)
            {
                var hero = heroes[i];
                if (!BattleManager.ReadIsEntityValide(hero))
                {
                    continue;
                }

                if (hero.ReadIsSuspended())
                {
                    suspendedCount++;
                }
                if (hero.ReadIsFrozen())
                {
                    frozenCount++;
                }
            }

            pauseText = source.m_LocalBattle.GM_IsPause() ? "是" : "否";
            suspendText = string.Format("{0} / {1}", suspendedCount, heroCount);
            frozenText = string.Format("{0} / {1}", frozenCount, heroCount);
            sourceText = source.m_TargetIsDevice ? "本机 Play（真机未连接）" : "本机 Play";
        }
        else
        {
            sourceText = "当前没有进行中的战斗";
        }

        // 注意：EditorGUILayout.LabelField 没有 (GUIContent, string) 重载，
        // 直接传 string 会被当成 GUIStyle 做隐式转换，报 "Unable to find style '否' in skin"。
        EditorGUILayout.LabelField(
            new GUIContent("GM 暂停：", "战斗逻辑时间不走动，与 buff 冰冻（923000）不是一回事。"),
            new GUIContent(pauseText));
        EditorGUILayout.LabelField(
            new GUIContent("挂起英雄：", "SetSuspended 的守卫英雄数量：表现与逻辑停更。真机状态不下发该统计。"),
            new GUIContent(suspendText));
        EditorGUILayout.LabelField(
            new GUIContent("冰冻英雄：", "挂了 buff 923000 的守卫英雄数量：禁普攻、禁技能、禁转向。"),
            new GUIContent(frozenText));
        EditorGUILayout.LabelField(
            new GUIContent("数据来源：", "这几行数据的来源。真机目标下只读真机推送状态，未连接时不会回退去读本机战斗。"),
            new GUIContent(sourceText));
        GUILayout.EndVertical();

        // 只读值要实时反映变化；真机连接后即使编辑器不在 Play 也要重绘。
        if (Application.isPlaying || GmBridgeEditorServer.ReadIsClientConnected())
        {
            Repaint();
        }
    }

    private void DrawEndlessWaveDebugSection()
    {
        var deviceState = GmBridgeEditorRouter.ReadTarget() != GmBridgeControlTarget.Local
            ? GmBridgeEditorServer.ReadDeviceState()
            : null;

        if (deviceState != null)
        {
            if (deviceState.m_GameMode != BattleGameMode.Endless.ToString())
            {
                EditorGUILayout.LabelField(string.Format(
                    "仅在无尽模式战斗中可用（真机当前：{0}）。",
                    string.IsNullOrEmpty(deviceState.m_GameMode) ? "无战斗" : deviceState.m_GameMode));
                return;
            }

            EditorGUILayout.LabelField("当前波次（真机）：", deviceState.m_Wave.ToString());
            if (m_GmEndlessWave <= 0)
            {
                m_GmEndlessWave = Mathf.Max(1, deviceState.m_Wave);
            }
        }
        else
        {
            var towerDefendBattle = BattleManager.GetBattle() as TowerDefendBattle;
            if (towerDefendBattle == null)
            {
                EditorGUILayout.LabelField("当前没有进行中的塔防战斗。");
                return;
            }

            if (towerDefendBattle.ReadGameMode() != BattleGameMode.Endless)
            {
                EditorGUILayout.LabelField("仅在无尽模式战斗中可用。");
                return;
            }

            EditorGUILayout.LabelField("当前波次：", towerDefendBattle.ReadCurrentWave().ToString());
            if (m_GmEndlessWave <= 0)
            {
                m_GmEndlessWave = Mathf.Max(1, towerDefendBattle.ReadCurrentWave());
            }
        }

        m_GmEndlessWave = Mathf.Max(1, EditorGUILayout.IntField("目标波次：", m_GmEndlessWave));
        if (GUILayout.Button(new GUIContent("设置当前波次", "立即把无尽模式的当前波次设为上面的目标波次，并刷出该波。旧波次还没刷完的排队会被丢弃，已经在场上的怪物保留。")))
        {
            GmBridgeEditorRouter.Invoke("battle.wave.set", "{\"m_Wave\":" + m_GmEndlessWave + "}");
        }
    }

    private void DrawBoneRemoteDebugSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("战斗输入开关", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("建议联调时只开启一种控制，避免键盘输入和骨骼输入同时驱动战斗。");

        bool isKeyboardControlEnabled = BoneRemoteDebugEditorConfig.ReadIsKeyboardControlEnabled();
        bool nextKeyboardControlEnabled = EditorGUILayout.Toggle("键盘控制：", isKeyboardControlEnabled);
        if (nextKeyboardControlEnabled != isKeyboardControlEnabled)
        {
            BoneRemoteDebugEditorConfig.SetKeyboardControlEnabled(nextKeyboardControlEnabled);
            Debug.Log(string.Format(
                "[GMTools] 键盘控制已{0}",
                nextKeyboardControlEnabled ? "开启" : "关闭"));
        }

        bool isBoneControlEnabled = BoneRemoteDebugEditorConfig.ReadIsBoneControlEnabled();
        bool nextBoneControlEnabled = EditorGUILayout.Toggle("骨骼控制：", isBoneControlEnabled);
        if (nextBoneControlEnabled != isBoneControlEnabled)
        {
            BoneRemoteDebugEditorConfig.SetBoneControlEnabled(nextBoneControlEnabled);
            Debug.Log(string.Format(
                "[GMTools] 骨骼控制已{0}",
                nextBoneControlEnabled ? "开启" : "关闭"));
        }

        bool isBoneDebugSkeletonOverlayEnabled = BoneRemoteDebugEditorConfig.ReadIsBoneDebugSkeletonOverlayEnabled();
        bool nextBoneDebugSkeletonOverlayEnabled = EditorGUILayout.Toggle("骨骼调试显示：", isBoneDebugSkeletonOverlayEnabled);
        if (nextBoneDebugSkeletonOverlayEnabled != isBoneDebugSkeletonOverlayEnabled)
        {
            BoneRemoteDebugEditorConfig.SetBoneDebugSkeletonOverlayEnabled(nextBoneDebugSkeletonOverlayEnabled);
            Debug.Log(string.Format(
                "[GMTools] 骨骼调试显示已{0}",
                nextBoneDebugSkeletonOverlayEnabled ? "开启" : "关闭"));
        }

        EditorGUILayout.LabelField("说明：编辑器由这里控制显示；Development 包默认显示，正式包不显示。");

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("远程骨骼监听", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("仅用于公司内部真机骨骼远程联调。发送器连到当前编辑器后，塔防骨骼输入会自动切换到远程来源。");

        bool isRemoteEnabled = BoneRemoteDebugEditorConfig.ReadIsRemoteEnabled();
        bool nextRemoteEnabled = EditorGUILayout.Toggle("启用监听：", isRemoteEnabled);
        if (nextRemoteEnabled != isRemoteEnabled)
        {
            BoneRemoteDebugEditorConfig.SetRemoteEnabled(nextRemoteEnabled);
            Debug.Log(string.Format(
                "[GMTools] 远程骨骼监听已{0}，端口={1}",
                nextRemoteEnabled ? "开启" : "关闭",
                BoneRemoteDebugEditorConfig.ReadPort()));
        }

        int currentPort = BoneRemoteDebugEditorConfig.ReadPort();
        int nextPort = EditorGUILayout.IntField("监听端口：", currentPort);
        if (nextPort != currentPort)
        {
            BoneRemoteDebugEditorConfig.SetPort(nextPort);
            Debug.Log(string.Format(
                "[GMTools] 远程骨骼监听端口已更新为 {0}",
                BoneRemoteDebugEditorConfig.ReadPort()));
        }

        GUILayout.EndVertical();
    }

    private void DrawBoneTurnTuningSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("角色 Y 轴转向调参", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.Space(2f);
        BoneTurnTuning.m_BlockNormalAttackWhileTurning = EditorGUILayout.Toggle(new GUIContent("转身时屏蔽普攻：", "关闭时允许角色转身和普攻同时发生；开启时仅屏蔽转身期间的普通攻击，主动技能不受影响。"), BoneTurnTuning.m_BlockNormalAttackWhileTurning);
        EditorGUILayout.HelpBox("侧身角度、最大转角、方向、防抖和缓动参数统一由全局表 100313 至 100322 控制。", MessageType.Info);

        if (EditorGUI.EndChangeCheck())
        {
            MarkBoneTurnTuningValuesChanged();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("重置 BoneTurnTuning 参数", "将当前运行时参数恢复为 BoneTurnTuning.cs 中的默认值，不修改源码。")))
        {
            if (!TryApplyBoneTurnTuningDefaultsFromCode())
            {
                EditorUtility.DisplayDialog("错误", "读取 BoneTurnTuning.cs 默认值失败，无法重置。", "确定");
            }
            else
            {
                ClearPersistedBoneTurnTuningValues();
            }
            RefreshOpenWindowSaveState();
        }
        if (GUILayout.Button(new GUIContent("回填到 BoneTurnTuning 默认值", "将当前面板里的骨骼转向参数写回 BoneTurnTuning.cs 的默认值常量，并刷新资源。")))
        {
            BackfillBoneTurnTuningToCode();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void GmEnterStage()
    {
        if (m_GmEnterStageId <= 0)
        {
            Debug.LogError("[GMTools] 关卡 ID 无效。");
            return;
        }

        if (!TowerDefendStageConfigResolver.Exists(m_GmEnterStageId, BattleGameMode.Chapter))
        {
            Debug.LogError(string.Format("[GMTools] 关卡 {0} 在章节模式中不存在。", m_GmEnterStageId));
            return;
        }

        var stageRoleCfg = t_tdStageGuardRoleBean.GetConfig(m_GmEnterStageId, false);
        if (stageRoleCfg == null)
        {
            Debug.LogError(string.Format("[GMTools] 关卡 {0} 缺少防守角色映射配置。", m_GmEnterStageId));
            return;
        }

        var request = new BattleStartupRequest();
        request.m_BattleType = BattleType.TowerDefend;
        request.m_GameMode = BattleGameMode.Chapter;
        request.m_StageId = m_GmEnterStageId;
        request.m_IsLocal = true;
        request.m_BaseMaxHealth = 10000;
        request.m_BaseHealth = 10000;

        int playerCount = Mathf.Min(stageRoleCfg.t_guard_role_ids.Count, TowerDefendSeatLayout.DefaultPlayerCount);
        for (int i = 0; i < playerCount; i++)
        {
            int seatId = TowerDefendSeatLayout.GetStartupSeatIdByIndex(i, playerCount);
            if (seatId >= stageRoleCfg.t_guard_role_ids.Count)
            {
                Debug.LogError(string.Format("[GMTools] 关卡 {0} 座位 {1} 超出角色映射范围。", m_GmEnterStageId, seatId));
                return;
            }

            long roleCfgId = stageRoleCfg.t_guard_role_ids[seatId];
            var heroCfg = t_heroBean.GetConfig(roleCfgId, false);
            if (heroCfg == null)
            {
                Debug.LogError(string.Format("[GMTools] 角色配置不存在，角色 ID：{0}", roleCfgId));
                return;
            }

            request.m_Players.Add(new BattleStartupPlayerData
            {
                m_PlayerId = seatId + 1,
                m_PlayerName = RenderAPI.GetTextByLanId("td_hud_default_player", seatId + 1),
                m_RoleCfgId = roleCfgId,
                m_RoleLevel = 1,
                m_IsAI = false,
                m_Group = GroupId.GuardGroupId,
                m_SeatId = seatId,
                m_HPPercent = 10000,
                m_MagicPercent = 10000,
            });
        }

        string error;
        if (!RenderEvent.Event.OnGmStartBattleRequest(request, out error))
        {
            Debug.LogError("[GMTools] 发起进入关卡失败：" + error);
            return;
        }

        Debug.Log(string.Format("[GMTools] 发起进入关卡 {0}（章节模式）", m_GmEnterStageId));
    }

    private static void TryFinishCurrentBattle(FinishReason finishReason, string actionName, object userData)
    {
        var battle = BattleManager.GetBattle();
        if (battle == null)
        {
            Debug.LogWarning(string.Format("[GMTools] 当前没有进行中的战斗，无法{0}。", actionName));
            return;
        }

        var progress = battle.GetBattleProgress();
        if (progress == null)
        {
            Debug.LogWarning(string.Format("[GMTools] 当前战斗缺少 BattleProgress，无法{0}。", actionName));
            return;
        }

        var towerDefendBattle = battle as TowerDefendBattle;
        if (finishReason == FinishReason.DefenseFailed &&
            towerDefendBattle != null)
        {
            towerDefendBattle.GM_ForceBaseHealthToZero();
        }

        progress.OnFinishGame(finishReason, userData);
    }

    private static TowerDefendBattle TryReadCurrentTowerDefendBattle(string actionName)
    {
        var battle = BattleManager.GetBattle();
        if (battle == null)
        {
            Debug.LogWarning(string.Format("[GMTools] 当前没有进行中的战斗，无法{0}。", actionName));
            return null;
        }

        var towerDefendBattle = battle as TowerDefendBattle;
        if (towerDefendBattle == null)
        {
            Debug.LogWarning(string.Format("[GMTools] 当前战斗不是塔防战斗，无法{0}。", actionName));
            return null;
        }

        return towerDefendBattle;
    }

    private static void TryAddCurrentTowerDefendTeamExpToMax()
    {
        var towerDefendBattle = TryReadCurrentTowerDefendBattle("补满团队经验");
        if (towerDefendBattle == null)
        {
            return;
        }

        towerDefendBattle.GM_AddTeamExpToMax();
    }

    private static void TryForceCurrentTowerDefendUpgradeChallengeShatter()
    {
        var towerDefendBattle = TryReadCurrentTowerDefendBattle("触发棒棒糖炸开");
        if (towerDefendBattle == null)
        {
            return;
        }

        if (!towerDefendBattle.GM_TryForceUpgradeChallengeShatter())
        {
            Debug.LogWarning("[GMTools] 当前不在升级挑战倒计时或进行阶段，无法触发棒棒糖炸开。");
        }
    }

    private static void TrySpawnCurrentTowerDefendNextWaveNow()
    {
        var towerDefendBattle = TryReadCurrentTowerDefendBattle("快速刷下一波");
        if (towerDefendBattle == null)
        {
            return;
        }

        if (!towerDefendBattle.GM_TrySpawnNextWaveNow())
        {
            Debug.LogWarning("[GMTools] 当前没有可立即触发的下一波，或刷怪器正处于不可刷波状态。");
        }
    }

    private static void TrySpawnCurrentTowerDefendBossNow()
    {
        var towerDefendBattle = TryReadCurrentTowerDefendBattle("直接刷 Boss");
        if (towerDefendBattle == null)
        {
            return;
        }

        if (!towerDefendBattle.GM_TrySpawnBossNow())
        {
            Debug.LogWarning("[GMTools] 当前关卡配置里没有可直接刷出的 Boss，或刷怪器尚未完成可用初始化。");
        }
    }

    private static void TrySetCurrentTowerDefendWave(int wave)
    {
        var towerDefendBattle = TryReadCurrentTowerDefendBattle("设置当前波次");
        if (towerDefendBattle == null)
        {
            return;
        }

        if (towerDefendBattle.ReadGameMode() != BattleGameMode.Endless)
        {
            Debug.LogWarning("[GMTools] 当前不是无尽模式，无法直接设置波次。");
            return;
        }

        if (!towerDefendBattle.GM_TrySetCurrentWave(wave))
        {
            Debug.LogWarning("[GMTools] 设置波次失败：刷怪器当前处于暂停、挂起或已结束状态。");
            return;
        }

        Debug.Log(string.Format("[GMTools] 无尽模式当前波次已设置为 {0}。", towerDefendBattle.ReadCurrentWave()));
    }

    private void DrawGuideTab()
    {
        GUILayout.BeginVertical();

        m_GuideGroup = EditorGUILayout.IntField("新手引导组：", m_GuideGroup);
        if (GUILayout.Button("测试引导"))
        {
            Main.GetInstance().CallGameDllFunction("AddGuide", m_GuideGroup);
        }

        RenderAPI.m_JumpAllGuide = EditorGUILayout.Toggle("关闭新手引导：", RenderAPI.m_JumpAllGuide);
        RenderAPI.m_IsFuncAllOpen = EditorGUILayout.Toggle("功能全部开启：", RenderAPI.m_IsFuncAllOpen);

        GUILayout.EndVertical();
    }
}
