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
    };

    private static readonly BattleConstFloatBinding[] s_BattleConstTrackingSpreadBindings = new BattleConstFloatBinding[]
    {
        new BattleConstFloatBinding(
            "多发生成左右分散角：",
            "作用对象：一发主子弹触发出来的多发追踪子弹。生效时机：子弹生成瞬间，从发射点开始就按这个角度左右展开，不是飞一段后再分开。值越大，左右展开越明显。",
            "DefaultSpawnYawSpread",
            () => BattleConst.SpawnYawSpread,
            value => BattleConst.SpawnYawSpread = value),
        new BattleConstFloatBinding(
            "多发生成左右抖动：",
            "作用对象：多发追踪子弹。含义：在左右分散角基础上再叠加一层随机偏转，让每发子弹不完全整齐。值越大，散布越随机。",
            "DefaultSpawnYawJitter",
            () => BattleConst.SpawnYawJitter,
            value => BattleConst.SpawnYawJitter = value),
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
    private readonly string[] m_TabNames = { "通用", "战斗", "战斗参数", "引导" };

    private int m_GuideGroup = 0;
    private bool m_LowFrameRate = false;
    private int m_GmEnterStageId = 1;
    private Vector2 m_ScrollPosition;

    private bool m_DebugMobile = false;
    private bool m_ShowLog = true;

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
        new BoneTurnTuningFloatBinding(nameof(BoneTurnTuning.m_MaxAngle), nameof(BoneTurnTuning.m_DefaultMaxAngle), () => BoneTurnTuning.m_MaxAngle, value => BoneTurnTuning.m_MaxAngle = value),
        new BoneTurnTuningFloatBinding(nameof(BoneTurnTuning.m_RotationAmplifyFactor), nameof(BoneTurnTuning.m_DefaultRotationAmplifyFactor), () => BoneTurnTuning.m_RotationAmplifyFactor, value => BoneTurnTuning.m_RotationAmplifyFactor = value),
        new BoneTurnTuningFloatBinding(nameof(BoneTurnTuning.m_ShoulderTurnJitterDeadZone), nameof(BoneTurnTuning.m_DefaultShoulderTurnJitterDeadZone), () => BoneTurnTuning.m_ShoulderTurnJitterDeadZone, value => BoneTurnTuning.m_ShoulderTurnJitterDeadZone = value),
    };

    private static readonly BoneTurnTuningBoolBinding[] s_BoneTurnTuningBoolBindings =
    {
        new BoneTurnTuningBoolBinding(nameof(BoneTurnTuning.m_InvertDirection), nameof(BoneTurnTuning.m_DefaultInvertDirection), () => BoneTurnTuning.m_InvertDirection, value => BoneTurnTuning.m_InvertDirection = value),
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
                DrawGuideTab();
                break;
        }

        GUILayout.EndScrollView();
    }

    private void DrawGeneralTab()
    {
        GUILayout.BeginVertical();

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

        if (GUILayout.Button(m_LowFrameRate ? "帧率低->高" : "帧率高->低"))
        {
            m_LowFrameRate = !m_LowFrameRate;
            Application.targetFrameRate = m_LowFrameRate ? 45 : -1;
        }

        if (GUILayout.Button("开启低配"))
        {
            QualitySettings.SetQualityLevel((int)QualityLevel.Fastest);
        }

        if (GUILayout.Button("屏幕截图"))
        {
            string fileName = Application.dataPath + "/../" + LCL.MonoTool.GetTimeStampUTCMs() + ".png";
            ScreenCapture.CaptureScreenshot(fileName);
            Debug.Log(string.Format("截取了一张图片: {0}", fileName));
        }

        GUILayout.EndVertical();
    }

    private void DrawBattleTab()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("进入关卡", EditorStyles.boldLabel);
        m_GmEnterStageId = EditorGUILayout.IntField("关卡 ID：", m_GmEnterStageId);
        if (GUILayout.Button(new GUIContent("进入关卡", "直接通过关卡 ID 进入战斗，跳过大厅流程，按章节模式进入。")))
        {
            GmEnterStage();
        }
        GUILayout.EndVertical();

        GUILayout.Space(8);

        DrawBoneRemoteDebugSection();

        GUILayout.Space(8);

        TowerDefendBattleSpawer.m_GMDisableAutoSkill = EditorGUILayout.Toggle("禁用自动释放技能：", TowerDefendBattleSpawer.m_GMDisableAutoSkill);

        DamageCal.m_GM_Invincible = EditorGUILayout.Toggle("怪物无敌：", DamageCal.m_GM_Invincible);
        State_Move.m_GM_DisableMove = EditorGUILayout.Toggle("怪物不移动：", State_Move.m_GM_DisableMove);

        {
            var battle = BattleManager.GetBattle();
            if (battle != null)
            {
                bool gmPause = EditorGUILayout.Toggle("战斗时间不走动：", battle.GM_IsPause());
                if (gmPause != battle.GM_IsPause())
                {
                    battle.GM_SetPause(gmPause);
                }
            }
        }

        if (GUILayout.Button("快速过关"))
        {
            TryFinishCurrentBattle(FinishReason.DefenseSucceeded, "快速过关", null);
        }

        if (GUILayout.Button("快速失败"))
        {
            TryFinishCurrentBattle(FinishReason.DefenseFailed, "快速失败", GroupId.PushGroupId);
        }

        GUILayout.Space(8);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("升级挑战调试", EditorStyles.boldLabel);
        if (GUILayout.Button(new GUIContent("团队经验拉满", "把当前塔防战斗的团队经验直接补到升级挑战触发所需上限。")))
        {
            TryAddCurrentTowerDefendTeamExpToMax();
        }

        if (GUILayout.Button(new GUIContent("快速刷下一波", "只跳过下一波等待时间，不清理当前场上怪物，允许前后两波怪物同时在场。")))
        {
            TrySpawnCurrentTowerDefendNextWaveNow();
        }

        if (GUILayout.Button(new GUIContent("直接刷当前关卡 Boss", "优先尝试刷出当前波次配置里的 Boss；如果当前波次没有 Boss，则回退到当前关卡配置中的第一个 Boss。不会推进正式波次。")))
        {
            TrySpawnCurrentTowerDefendBossNow();
        }

        if (GUILayout.Button(new GUIContent("棒棒糖直接炸开", "让当前升级挑战中的棒棒糖直接进入最终炸开流程。")))
        {
            TryForceCurrentTowerDefendUpgradeChallengeShatter();
        }
        GUILayout.EndVertical();

        GUILayout.EndVertical();
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
        EditorGUILayout.LabelField("骨骼转向调参", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.Space(2f);
        BoneTurnTuning.m_MaxAngle = EditorGUILayout.FloatField("最大转角：", BoneTurnTuning.m_MaxAngle);
        BoneTurnTuning.m_InvertDirection = EditorGUILayout.Toggle("左右方向反转：", BoneTurnTuning.m_InvertDirection);
        BoneTurnTuning.m_RotationAmplifyFactor = EditorGUILayout.FloatField("旋转放大量：", BoneTurnTuning.m_RotationAmplifyFactor);
        BoneTurnTuning.m_ShoulderTurnJitterDeadZone = EditorGUILayout.Slider("肩部抖动死区：", BoneTurnTuning.m_ShoulderTurnJitterDeadZone, 0f, 1f);
        EditorGUILayout.HelpBox("转向按鼻尖与双肩中线偏移计算。放大 1 表示保持当前量级。", MessageType.Info);

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
