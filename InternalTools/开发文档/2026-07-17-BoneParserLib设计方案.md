# BoneParserLib 设计方案

日期：2026-07-17

## 1. 背景

当前项目中，骨骼输入相关逻辑主要分散在以下几类代码里：

- SDK 与第三方接入层  
  例如 `Assets/Scripts/ThirdParty/YouDooUnity`、`Assets/Scripts/SDK`、`AndroidParseDataDemo`、`AndroidServerInfoDemo`
- 主工程骨骼帧来源与解析层  
  例如 `Assets/Scripts/Game/BattleLogic/Battle/BoneInput`
- 塔防业务消费层  
  例如 `TowerDefendBoneInputDriver`、`TowerDefendBattleScene`

当前实现虽然已经具备骨骼数据解析、动作判定和战斗消费能力，但仍然存在以下问题：

- 骨骼数据模型、动作规则、塔防业务语义耦合在一起
- 解析逻辑仍然位于主工程业务目录中，难以复用到其他游戏
- 主工程中与骨骼解析相关的我们自研代码分布较散，后续维护成本较高
- 当前逻辑直接输出“普攻”“技能”等塔防语义，不利于抽象成通用动作库

本次设计的目标，是将“SDK 已提供骨骼点之后，我们自己的动作解析逻辑”独立为一个可复用库，并统一主工程内接入代码的目录结构。

## 2. 目标

### 2.1 目标

- 在 `InternalTools/BoneParserLib` 中建立独立的纯 C# 动作解析库
- 将“骨骼数据解析为动作”的逻辑从主工程业务目录中抽离
- 库只负责通用动作解析，不负责具体游戏语义
- 主工程中所有我们自研的骨骼解析接入代码统一集中到 `Assets/Scripts/Game/BoneParser`
- `BoneParserLib` 编译后的 `dll` 直接输出到 `Assets/Scripts/Game/BoneParser`
- 第一版覆盖以下能力：
  - 转向解析
  - 普攻相关动态动作
  - 技能相关动态动作
  - 静态姿态动作：举左手、举右手、举双手、双手交叉胸前、双手叉腰、蹲下

### 2.2 非目标

- 不修改 `Assets/Scripts/ThirdParty/YouDooUnity`
- 不修改 `Assets/Scripts/SDK`
- 不修改任何 SDK demo 代码，例如 `AndroidParseDataDemo`、`AndroidServerInfoDemo`、`ModelSelectDemo`
- 不在第一版中引入完全配置化的通用动作规则平台
- 不在第一版中把 `Assets/Scripts/Game/BoneParser` 做成 Unity 程序集

## 3. 硬约束

### 3.1 第三方与 SDK 代码不改动

以下目录和文件只作为数据来源，不参与本次改造：

- `Assets/Scripts/ThirdParty/YouDooUnity`
- `Assets/Scripts/SDK`
- SDK demo 相关脚本和预制件

原因如下：

- 这部分代码由外部公司生产，后续可能发生更新或替换
- 如果在这些目录中写入我们的业务适配，会放大后续升级冲突风险
- 动作解析库应建立在“消费现有数据”的模式上，而不是“侵入第三方接入层”的模式上

### 3.2 主工程内代码集中管理

主工程中所有我们自己的骨骼解析接入代码，统一集中到：

- `Project/GameProject/Assets/Scripts/Game/BoneParser`

不再把新的自研骨骼解析代码散落到多个业务目录中。

### 3.3 不新增 Unity 程序集

`Assets/Scripts/Game/BoneParser` 第一版不新增 `asmdef`。

原因如下：

- 当前自研游戏代码整体并未完成程序集边界整理
- 如果现在单独拆程序集，容易被 `GameDll`、塔防战斗类、旧业务引用关系卡住
- 当前阶段优先保证目录集中、边界清晰和迁移稳定

## 4. 当前链路梳理

当前骨骼输入链路可拆成三层：

### 4.1 数据来源层

- `BattleBoneParseData`
- `RemoteBoneFrameSourceProxy`
- `IBoneFrameSource`

职责：

- 从本地 SDK 或远程调试来源读取骨骼帧
- 组装成当前主工程内部使用的骨骼帧结构

### 4.2 动作解析层

- `BoneInput`
- `BoneTurnDemoRotationParse`
- `BoneCurrentNormalAttackParse`
- `BoneCurrentSkillParse`
- `IBoneInputParse`

职责：

- 维护跨帧状态
- 解析转向
- 解析普攻与技能相关动作
- 输出当前塔防直接消费的结构

问题：

- 与塔防语义耦合较深
- 依赖 `UnityEngine`
- 依赖主工程内部命名空间
- 没有真正独立成库

### 4.3 业务消费层

- `TowerDefendBoneInputDriver`
- `TowerDefendBattleScene`

职责：

- 把动作解析结果映射成塔防战斗行为
- 处理正式座位与 SDK 槽位绑定
- 结合镜头方向换算实际战斗朝向
- 判断技能是否允许释放

## 5. 目标结构

### 5.1 独立库源码位置

- `D:/lichunlin/MYToothGame/InternalTools/BoneParserLib`

建议目录结构：

```text
InternalTools/BoneParserLib/
  BoneParserLib.sln
  src/
    BoneParserLib/
      BoneParserLib.csproj
      Models/
      Runtime/
      Rules/
      Dispatch/
```

### 5.2 主工程接入目录

- `D:/lichunlin/MYToothGame/Project/GameProject/Assets/Scripts/Game/BoneParser`

建议目录结构：

```text
Assets/Scripts/Game/BoneParser/
  BoneParserLib.dll
  BoneParserLib.pdb

  Abstractions/
    IBoneFrameSource.cs
    IBoneActionConsumer.cs

  Sources/
    LocalSdkBoneFrameSource.cs
    RemoteBoneFrameSourceProxy.cs

  Adapters/
    SdkJointMapper.cs
    SdkPoseHintMapper.cs
    BoneParserSeatBinding.cs

  Consumers/
    TowerDefend/
      TowerDefendBoneParserDriver.cs
      TowerDefendBoneActionBinding.cs

  Debug/
    BoneDebugSkeletonOverlay.cs
    BoneRemoteDebugEditorConfig.cs
```

说明：

- `BoneParserLib.dll` 与我们自己的 Unity 接入代码共存于同一目录树，便于集中管理
- 第一版只要求目录集中，不要求做 Unity 程序集拆分

## 6. BoneParserLib 的职责边界

`BoneParserLib` 只做以下事情：

- 接收标准化骨骼帧
- 维护跨帧运行时状态
- 解析静态姿态动作
- 解析动态动作
- 输出通用动作事件与连续状态

`BoneParserLib` 不做以下事情：

- 不接 SDK
- 不读 `YouDooSDKConstants`
- 不处理 Unity 场景对象
- 不处理战斗正式座位
- 不判断技能是否允许释放
- 不调用塔防业务接口
- 不直接打印 Unity `Debug.Log`

## 7. 数据模型设计

### 7.1 输入模型

库内输入使用自定义纯 C# 模型，不依赖 `UnityEngine`：

- `BoneFrame`
- `BonePerson`
- `BoneJoint`
- `BoneRect`
- `BoneJointId`
- `BoneSlotBinding`
- `BonePoseHintFlags`

说明：

- `BoneJointId` 为库内统一关节点编号
- 主工程适配层负责把 SDK 的关键点编号映射成 `BoneJointId`
- `BonePoseHintFlags` 表示来源层已经提供的静态姿态提示

### 7.2 输出模型

- `BoneParseFrameResult`
- `BonePlayerSnapshot`
- `BoneActionEvent`
- `BoneGestureType`
- `BoneGesturePhase`

说明：

- `BonePlayerSnapshot` 表示连续状态，例如当前是否追踪到人、朝向角度、当前激活动作集合
- `BoneActionEvent` 表示离散事件，例如当前帧触发了某个动作

## 8. 动作模型设计

### 8.1 枚举命名规则

按确认要求执行：

- 枚举类型名使用英文
- 枚举成员名使用中文

示例：

```csharp
public enum BoneGestureType
{
    未知 = 0,
    举左手 = 1001,
    举右手 = 1002,
    举双手 = 1003,
    双手交叉胸前 = 1004,
    双手叉腰 = 1005,
    蹲下 = 1006,
    左右交替挥击 = 2001,
    双手过头蓄力 = 2002,
    双手下压释放 = 2003,
}
```

### 8.2 姿态提示层

为了兼容当前 `poseType` 的静态姿态提示，设计 `BonePoseHintFlags`：

```csharp
[Flags]
public enum BonePoseHintFlags
{
    无 = 0,
    举左手 = 1 << 0,
    举右手 = 1 << 1,
    双手交叉胸前 = 1 << 2,
    双手叉腰 = 1 << 3,
    蹲下 = 1 << 4,
}
```

说明：

- `举双手` 不在提示层单独占位
- 当 `举左手` 与 `举右手` 同时成立时，库输出层可以合成为 `举双手`

### 8.3 动作阶段

```csharp
public enum BoneGesturePhase
{
    开始 = 1,
    持续 = 2,
    结束 = 3,
    触发 = 4,
}
```

约定：

- 静态姿态类动作输出 `开始 / 持续 / 结束`
- 动态离散动作输出 `触发`
- `双手过头蓄力` 输出 `开始 / 持续 / 结束`

## 9. 规则接口设计

### 9.1 规则接口

库内部按可替换规则设计：

- `IBoneParserProfile`
- `IBoneFacingRule`
- `IBoneGestureRule`

说明：

- `IBoneFacingRule` 负责转向
- `IBoneGestureRule` 负责静态或动态动作识别
- `IBoneParserProfile` 负责组合当前项目要启用的规则集合

### 9.2 第一版默认规则

第一版默认 profile 直接对齐当前塔防行为，包含以下规则：

- 双肩鼻子转向规则
- 左右交替挥击规则
- 双手过头蓄力规则
- 双手下压释放规则
- 静态姿态转换规则：
  - 举左手
  - 举右手
  - 举双手
  - 双手交叉胸前
  - 双手叉腰
  - 蹲下

## 10. 动作注册机制

为了让不同游戏复用同一动作库，主工程不直接依赖库返回的“普攻/技能”字段，而改为注册式消费。

建议提供：

- `BoneActionRegistry`

典型调用方式：

```csharp
var parser = new BoneParser(config, profile);
var result = parser.Update(frame, slotBindings);

registry.Register(BoneGestureType.左右交替挥击, OnNormalAttack);
registry.Register(BoneGestureType.双手下压释放, OnUseSkill);
registry.Register(BoneGestureType.举双手, OnReady);
registry.Register(BoneGestureType.双手交叉胸前, OnExit);
```

这样不同游戏可以按自己的语义做映射：

- 塔防游戏：
  - `左右交替挥击 -> 普攻`
  - `双手下压释放 -> 技能`
- 准备界面：
  - `举双手 -> 准备`
  - `双手交叉胸前 -> 退出`

## 11. 主工程适配设计

### 11.1 不改第三方来源层

保持现有来源层不变：

- 本地来源继续沿用 `BattleBoneParseData`
- 远程来源继续沿用 `RemoteBoneFrameSourceProxy`

### 11.2 新增我们自己的适配层

在 `Assets/Scripts/Game/BoneParser/Adapters` 中新增适配器，将现有来源层产出的数据转成 `BoneParserLib` 输入：

- `SdkJointMapper`
  - SDK 关键点编号 -> `BoneJointId`
- `SdkPoseHintMapper`
  - `poseType` -> `BonePoseHintFlags`
- `BoneParserSeatBinding`
  - SDK 槽位、正式座位、业务绑定信息统一建模

### 11.3 塔防消费改造

`TowerDefendBoneInputDriver` 改造成新的塔防接入驱动：

- 调用 `BoneParserLib`
- 读取 `BonePlayerSnapshot`
- 消费 `BoneActionEvent`
- 通过注册映射把动作转换为塔防行为

保留以下业务逻辑在主工程：

- 正式座位与 SDK 槽位绑定
- 相机方向换算
- 战斗实际攻击调用
- 当前技能是否可释放

## 12. 编译与产物投放

### 12.1 项目形态

`BoneParserLib` 使用纯 C# 工具库形态：

- 目录：`InternalTools/BoneParserLib`
- 项目：`BoneParserLib.csproj`
- 目标框架：`netstandard2.1`

### 12.2 产物复制目标

编译后自动复制到：

- `D:/lichunlin/MYToothGame/Project/GameProject/Assets/Scripts/Game/BoneParser/BoneParserLib.dll`
- `D:/lichunlin/MYToothGame/Project/GameProject/Assets/Scripts/Game/BoneParser/BoneParserLib.pdb`

这样主工程和我们自己的 Unity 接入代码都集中在同一目录下。

## 13. 配置设计

库内提供 `BoneParserConfig`，用于保存解析阈值和规则参数。

参数建议分为三组：

### 13.1 基础阈值

- 人体整体置信度下限
- 单点置信度下限
- 丢帧释放帧数

### 13.2 静态姿态参数

- 姿态开始判定帧数
- 姿态结束判定帧数
- 姿态抖动容忍

### 13.3 动态动作参数

- 挥击速度阈值
- 普攻冷却
- 蓄力帧数
- 下压释放速度阈值
- 技能冷却

第一版默认值原则：

- 直接对齐当前塔防项目的手感和表现
- 优先保证迁移后行为一致，不主动调玩法

## 14. 错误处理与诊断

### 14.1 错误处理原则

库不依赖 Unity，也不直接打 Unity 日志。

出现以下情况时，采用安全降级：

- 当前帧无人体数据
- 关键点缺失
- 关键点置信度不足
- 静态姿态提示无效

处理原则：

- 不触发新的动态动作
- 必要时发出静态动作结束事件
- 连续状态回到安全默认值或保留最近稳定值

### 14.2 诊断输出

建议库可选输出诊断结果，例如：

- 当前帧是否缺少关键点
- 当前帧是否只有姿态提示没有有效骨骼点
- 当前动作未触发的主要原因

诊断输出只提供给宿主决定是否记录，不在库内直接打印。

## 15. 验证口径

### 15.1 编辑器远程联调

验证以下行为与现有塔防表现一致：

- 朝向解析
- `左右交替挥击 -> 普攻`
- `双手下压释放 -> 技能`

### 15.2 Android 真机

在不修改 SDK、`YouDooUnity`、demo 的前提下，验证以下内容：

- 主工程适配层可以正确消费现有骨骼来源
- 真机动作解析结果稳定

### 15.3 准备界面

验证以下静态姿态都能稳定输出 `开始 / 持续 / 结束`：

- 举左手
- 举右手
- 举双手
- 双手交叉胸前
- 双手叉腰
- 蹲下

### 15.4 回归原则

第一版目标：

- 迁移解析逻辑
- 解耦目录与职责
- 保持当前表现稳定

第一版不是玩法重做，不主动改变动作手感。

## 16. 迁移顺序

建议按以下顺序落地：

1. 创建 `InternalTools/BoneParserLib` 项目结构
2. 建立纯 C# 数据模型、枚举、结果结构、规则接口
3. 迁移当前三类规则到库中，形成默认 profile
4. 在主工程新增我们自己的适配层
5. 在主工程新增新的塔防消费驱动
6. 让准备界面和塔防战斗逐步改为消费 `BoneParserLib`
7. 验证稳定后，删除我们自己旧的骨骼解析实现

## 17. 旧文件迁移对照

### 17.1 保留不动

- `Assets/Scripts/ThirdParty/YouDooUnity/**`
- `Assets/Scripts/SDK/**`
- `AndroidParseDataDemo`
- `AndroidServerInfoDemo`
- `ModelSelectDemo`

### 17.2 保留职责但移动接入边界

- `BattleBoneParseData`
- `RemoteBoneFrameSourceProxy`
- `IBoneFrameSource`

这些继续存在，但作为来源层或适配层，不再承担核心动作解析职责。

### 17.3 迁移入库

- `BoneInput` 中的通用输入模型
- `BoneInput` 中的跨帧状态管理
- `BoneTurnDemoRotationParse`
- `BoneCurrentNormalAttackParse`
- `BoneCurrentSkillParse`
- `IBoneInputParse` 中可复用的接口思想

迁移后要去塔防化，改为输出通用动作事件和状态。

## 18. 最终目标状态

最终骨骼解析相关代码收敛为两处：

### 18.1 可复用库源码

- `InternalTools/BoneParserLib`

### 18.2 主工程接入与消费代码

- `Project/GameProject/Assets/Scripts/Game/BoneParser`

这样后续维护时：

- 查通用动作规则，只看 `BoneParserLib`
- 查当前游戏接入，只看 `Assets/Scripts/Game/BoneParser`

## 19. 实施建议

第一版以“稳定迁移”为第一优先级：

- 先保证行为与当前项目一致
- 先完成目录和职责解耦
- 先建立动作注册模式
- 后续如有其他游戏接入，再继续抽象更高层的规则平台

