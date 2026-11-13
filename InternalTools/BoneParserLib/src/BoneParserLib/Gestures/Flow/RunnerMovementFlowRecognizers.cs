using System;

namespace CompanyInternalTools.BoneParserLib
{
    internal struct TorsoMovementInput
    {
        public float m_ShoulderWidth;
        public BoneVector2 m_LeftShoulder;
        public BoneVector2 m_RightShoulder;
        public BoneVector2 m_ShoulderCenter;
        public BoneVector2 m_LeftHip;
        public BoneVector2 m_RightHip;
        public BoneVector2 m_HipCenter;
    }

    internal struct FullBodyMovementInput
    {
        public TorsoMovementInput m_Torso;
        public BoneVector2 m_AnkleCenter;
        // 双脚踝是否至少有一侧达到置信度。脚踝是全身最不稳定的关键点，
        // 跳/蹲判定以肩髋为主，脚踝数据可用时才参与确认，不可用时不再整帧丢弃。
        public bool m_HasAnkleTracking;
    }

    internal static class RunnerMovementFlowUtility
    {
        // 跳起来/蹲下去准备阶段允许的连续竖直抖动帧数：脚踝等关键点偶发单帧尖峰
        // 不再直接重置基准收集，只有连续超过该帧数才视为真的开始运动。
        internal const int m_BodyMotionReadySpikeToleranceFrames = 2;

        // 起跳联锁的最长帧数。正常落地立刻解除，这里只是兜底：基准偏低时"回到基准"永远
        // 不成立，没有上限就会把起跳永久锁死。60 帧在 30fps 骨架下约 2 秒，远长于一次滞空。
        internal const int m_JumpAirborneGuardMaxFrames = 60;

        // 离开最高点的回落幅度达到这个肩宽倍数就认为已经落地。取小于起跳阈值
        // （m_JumpMinShoulderRiseRatio，默认 0.18）的值：触发帧已经保证身体比出手前高出
        // 这么多，所以回落 0.15 时人一定离开了最高点，基准不会再被收紧。
        internal const float m_JumpAirborneDescentRatio = 0.15f;

        // 跳已武装时，单纯下蹲蓄力（无论多深）都不重置基准：站立基准保持不变，
        // 把更低的姿势承认为新站姿会让"站起来"变成一次上升，直接误触发。

        public static bool TryReadTorsoInput(BoneGestureRuntimeContext context, out TorsoMovementInput input)
        {
            input = new TorsoMovementInput();
            BoneTrackedPerson person = context.m_Person;
            float minScore = context.m_Config.m_GestureKeypointMinConfidence;
            if (person == null ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左肩, minScore, out input.m_LeftShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右肩, minScore, out input.m_RightShoulder) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.左髋, minScore, out input.m_LeftHip) ||
                !BoneSkeletonQuery.TryReadBodyJoint(person, BoneBodyJointType.右髋, minScore, out input.m_RightHip))
            {
                return false;
            }

            input.m_ShoulderWidth = BoneMath.Abs(input.m_RightShoulder.m_X - input.m_LeftShoulder.m_X) +
                context.m_Config.m_ShoulderWidthEpsilon;
            if (input.m_ShoulderWidth <= context.m_Config.m_ShoulderWidthEpsilon)
            {
                return false;
            }

            input.m_ShoulderCenter = (input.m_LeftShoulder + input.m_RightShoulder) * 0.5f;
            input.m_HipCenter = (input.m_LeftHip + input.m_RightHip) * 0.5f;
            return true;
        }

        public static bool TryReadFullBodyInput(BoneGestureRuntimeContext context, out FullBodyMovementInput input)
        {
            input = new FullBodyMovementInput();
            float minScore = context.m_Config.m_GestureKeypointMinConfidence;
            if (!TryReadTorsoInput(context, out input.m_Torso))
            {
                return false;
            }

            bool hasLeftAnkle = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person, BoneBodyJointType.左脚踝, minScore, out BoneVector2 leftAnkle);
            bool hasRightAnkle = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person, BoneBodyJointType.右脚踝, minScore, out BoneVector2 rightAnkle);
            if (hasLeftAnkle && hasRightAnkle)
            {
                input.m_AnkleCenter = (leftAnkle + rightAnkle) * 0.5f;
                input.m_HasAnkleTracking = true;
            }
            else if (hasLeftAnkle)
            {
                input.m_AnkleCenter = leftAnkle;
                input.m_HasAnkleTracking = true;
            }
            else if (hasRightAnkle)
            {
                input.m_AnkleCenter = rightAnkle;
                input.m_HasAnkleTracking = true;
            }

            return true;
        }

        public static float ReadPlayerLeftAxisSign(TorsoMovementInput input)
        {
            return BoneMath.Sign(input.m_LeftShoulder.m_X - input.m_RightShoulder.m_X);
        }

        // 稳定性检查只看肩和髋：脚踝关键点即使人站着不动也会有大幅抖动，
        // 把脚踝速度算进稳定性会让准备阶段几乎永远无法完成。
        public static float ReadMaxTorsoVerticalSpeed(
            FullBodyMovementInput input,
            BoneVector2 lastShoulderCenter,
            BoneVector2 lastHipCenter,
            float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f)
            {
                return float.MaxValue;
            }

            float shoulderSpeed = BoneMath.Abs(input.m_Torso.m_ShoulderCenter.m_Y - lastShoulderCenter.m_Y) / deltaTimeSeconds;
            float hipSpeed = BoneMath.Abs(input.m_Torso.m_HipCenter.m_Y - lastHipCenter.m_Y) / deltaTimeSeconds;
            return BoneMath.Max(shoulderSpeed, hipSpeed);
        }

        // 蹲下去的腾空联锁：起跳过程中身体从压低位置明显上升，顶点附近竖直速度接近 0，
        // 下蹲的稳定准备会把这个空中高度记成站立基准，随后的下落相对该基准就是一次深度足够的
        // 下降，落地缓冲再撑满确认帧就误触发下蹲。这里跟踪最近的低点，一旦出现明显上升就锁定
        // 一段时间，锁定期内不重设基准、也不判定下蹲。
        public static void UpdateAirborneLock(
            BoneParserConfig config,
            StableBodyMotionFlowState state,
            FullBodyMovementInput input)
        {
            int holdFrames = config.m_CrouchDownAirborneHoldFrames;
            float riseThreshold = config.m_CrouchDownAirborneRiseRatio;
            if (holdFrames <= 0 || riseThreshold <= 0f)
            {
                state.m_AirborneLockFrames = 0;
                state.m_HasAirborneTracking = false;
                state.m_AirbornePeakRiseRatio = 0f;
                return;
            }

            float shoulderY = input.m_Torso.m_ShoulderCenter.m_Y;
            if (!state.m_HasAirborneTracking)
            {
                state.m_HasAirborneTracking = true;
                state.m_AirborneTrackShoulderY = shoulderY;
                state.m_AirborneTrackFrames = 0;
                state.m_AirbornePeakRiseRatio = 0f;
            }
            else if (shoulderY >= state.m_AirborneTrackShoulderY)
            {
                // 出现新的低点：以它作为上升起点，之前的升幅作废。
                state.m_AirborneTrackShoulderY = shoulderY;
                state.m_AirborneTrackFrames = 0;
                state.m_AirbornePeakRiseRatio = 0f;
            }
            else
            {
                float shoulderWidth = input.m_Torso.m_ShoulderWidth;
                float riseRatio = shoulderWidth > 0f
                    ? (state.m_AirborneTrackShoulderY - shoulderY) / shoulderWidth
                    : 0f;
                if (riseRatio > state.m_AirbornePeakRiseRatio)
                {
                    state.m_AirbornePeakRiseRatio = riseRatio;
                }

                // 上升走势持续太久仍未回到高位，视为站姿缓慢漂移，重新取起点。
                if (++state.m_AirborneTrackFrames > holdFrames)
                {
                    state.m_AirborneTrackShoulderY = shoulderY;
                    state.m_AirborneTrackFrames = 0;
                    state.m_AirbornePeakRiseRatio = 0f;
                }
            }

            if (state.m_AirbornePeakRiseRatio >= riseThreshold)
            {
                // 越过阈值：锁定并重置跟踪点。锁定只续期到这一段上升为止，人停在高位时
                // 不会继续续期，保证锁定必然到期，不会永久挡住下蹲。
                state.m_AirborneLockFrames = holdFrames;
                state.m_AirborneTrackShoulderY = shoulderY;
                state.m_AirborneTrackFrames = 0;
                state.m_AirbornePeakRiseRatio = 0f;
            }
            else if (state.m_AirborneLockFrames > 0)
            {
                state.m_AirborneLockFrames--;
            }
        }
    }

    internal sealed class SideLeanFlowState : TriggerOnlyFlowState
    {
        public int m_NeutralFrameCount;
        public bool m_IsArmed;
        public bool m_HasLastLeanRatio;
        public float m_LastLeanRatio;
        // 停止重定基准：触发后侧身动作停稳若干帧，就把当前姿势当作回正后的基准；
        // 之后再次快速侧身（左右皆可）视作一次新操作。基准不在 ResetFlowState 里清，
        // 因为触发成功后的状态复位要保持基准等待下一次停稳更新（完整重置由状态对象重建兜底）。
        public float m_BaselineLeanRatio;
        public int m_StoppedFrameCount;
        // 预测式提前触发的高速确认帧数：触发成功后的状态复位清零，保证每次高速都是新动作。
        public int m_FastLeanFrameCount;
        // 解除武装后的动作峰值偏移：回落超过释放量即重新武装（相对回滞，完整重置由状态对象重建兜底）。
        public float m_UnarmedPeakLeanRatio;

        public override void ResetFlowState()
        {
            m_NeutralFrameCount = 0;
            m_IsArmed = false;
            m_HasLastLeanRatio = false;
            m_LastLeanRatio = 0f;
            m_StoppedFrameCount = 0;
            m_FastLeanFrameCount = 0;
        }
    }

    internal abstract class SideLeanFlowRecognizer : TriggerOnlyFlowRecognizer<SideLeanFlowState>
    {
        protected abstract float DirectionMultiplier { get; }
        protected abstract int ReadReadyFrames(BoneParserConfig config);
        protected abstract float ReadTriggerOffsetRatio(BoneParserConfig config);
        protected abstract float ReadReturnToNeutralRatio(BoneParserConfig config);
        protected abstract float ReadMinSpeedRatioPerSecond(BoneParserConfig config);

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, SideLeanFlowState state)
        {
            if (!RunnerMovementFlowUtility.TryReadTorsoInput(context, out TorsoMovementInput input))
            {
                state.ResetFlowState();
                return false;
            }

            float playerLeftAxisSign = RunnerMovementFlowUtility.ReadPlayerLeftAxisSign(input);
            if (playerLeftAxisSign == 0f)
            {
                state.ResetFlowState();
                return false;
            }

            float playerLeftLeanRatio = (input.m_ShoulderCenter.m_X - input.m_HipCenter.m_X) *
                playerLeftAxisSign / input.m_ShoulderWidth;
            float directionalLeanRatio = playerLeftLeanRatio * DirectionMultiplier;
            float leanSpeed = 0f;
            if (state.m_HasLastLeanRatio && context.m_DeltaTimeSeconds > 0f)
            {
                leanSpeed = (directionalLeanRatio - state.m_LastLeanRatio) / context.m_DeltaTimeSeconds;
            }

            // 玩家回到真实直立（中立区）时基准归零，避免停留在旧基准上影响正常触发。
            if (BoneMath.Abs(playerLeftLeanRatio) <= ReadReturnToNeutralRatio(context.m_Config))
            {
                state.m_BaselineLeanRatio = 0f;
            }

            // 从"侧身位重定基准"再出发时行程打六折：停稳本身就是明确的动作边界，
            // 再次快速侧身不需要重新走满整个触发偏移，同方向连发不用侧得更深。
            float triggerOffsetRatio = ReadTriggerOffsetRatio(context.m_Config);
            float returnToNeutralRatio = ReadReturnToNeutralRatio(context.m_Config);
            float retriggerOffsetRatio = BoneMath.Abs(state.m_BaselineLeanRatio) > returnToNeutralRatio
                ? triggerOffsetRatio * 0.6f
                : triggerOffsetRatio;

            if (!state.m_IsArmed)
            {
                // 相对释放：从本次动作峰值回落超过释放量即重新武装（基准归零）。
                // 提前触发的发招点较浅，固定回正线会离触发点太近造成连触，改为相对峰值释放。
                const float releaseRatio = 0.08f;
                if (directionalLeanRatio > state.m_UnarmedPeakLeanRatio)
                {
                    state.m_UnarmedPeakLeanRatio = directionalLeanRatio;
                }

                if (state.m_UnarmedPeakLeanRatio - directionalLeanRatio >= releaseRatio)
                {
                    state.m_UnarmedPeakLeanRatio = directionalLeanRatio;
                    state.m_BaselineLeanRatio = 0f;
                    state.m_NeutralFrameCount = 0;
                    state.m_StoppedFrameCount = 0;
                    state.m_IsArmed = true;
                }

                // 停止重定基准：触发后不必慢慢回正——侧身动作停稳（速度低于阈值持续若干帧）就把当前
                // 姿势当作回正后的基准，之后再次快速侧身（左右皆可）视作一次新操作。
                // 停止速度小于等于 0 表示关闭，只保留回到中立区的常规重新武装。
                float stopSpeedRatioPerSecond = context.m_Config.m_LeanRebaselineStopSpeedRatioPerSecond;
                float stopFrames = context.m_Config.m_LeanRebaselineStopFrames;
                if (!state.m_IsArmed && stopSpeedRatioPerSecond > 0f && stopFrames >= 1f)
                {
                    state.m_StoppedFrameCount = BoneMath.Abs(leanSpeed) <= stopSpeedRatioPerSecond
                        ? state.m_StoppedFrameCount + 1
                        : 0;
                    if (state.m_StoppedFrameCount >= (int)stopFrames)
                    {
                        state.m_StoppedFrameCount = 0;
                        state.m_BaselineLeanRatio = directionalLeanRatio;
                        state.m_IsArmed = true;
                    }
                }

                if (!state.m_IsArmed)
                {
                    state.m_NeutralFrameCount = BoneMath.Abs(playerLeftLeanRatio) <= returnToNeutralRatio
                        ? state.m_NeutralFrameCount + 1
                        : 0;
                    state.m_IsArmed = state.m_NeutralFrameCount >= Math.Max(1, ReadReadyFrames(context.m_Config));
                    if (state.m_IsArmed)
                    {
                        state.m_BaselineLeanRatio = 0f;
                    }
                }

                state.m_LastLeanRatio = directionalLeanRatio;
                state.m_HasLastLeanRatio = true;
                return false;
            }

            state.m_LastLeanRatio = directionalLeanRatio;
            state.m_HasLastLeanRatio = true;
            if (directionalLeanRatio - state.m_BaselineLeanRatio >= retriggerOffsetRatio
                && leanSpeed >= ReadMinSpeedRatioPerSecond(context.m_Config))
            {
                state.m_UnarmedPeakLeanRatio = directionalLeanRatio;
                return true;
            }

            // 预测式提前触发：高速侧身在到达触发深度前，按当前速度推算必将越线即触发，
            // 把"动作到位才响应"提前为"动作起步就响应"。三重约束防误触：
            // 保底深度 + 高速门槛 + 连续高速帧数。提前量填 0 表示关闭。
            float predictiveLeadSeconds = context.m_Config.m_LeanPredictiveLeadSeconds;
            if (predictiveLeadSeconds > 0f)
            {
                state.m_FastLeanFrameCount = leanSpeed >= context.m_Config.m_LeanPredictiveMinSpeedRatioPerSecond
                    ? state.m_FastLeanFrameCount + 1
                    : 0;
                if (state.m_FastLeanFrameCount >= 2
                    && directionalLeanRatio - state.m_BaselineLeanRatio >= context.m_Config.m_LeanPredictiveMinOffsetRatio
                    && directionalLeanRatio - state.m_BaselineLeanRatio + leanSpeed * predictiveLeadSeconds >= retriggerOffsetRatio)
                {
                    state.m_UnarmedPeakLeanRatio = directionalLeanRatio;
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class LeftLeanFlowRecognizer : SideLeanFlowRecognizer
    {
        protected override string StateKey => "Flow.LeftLean";
        protected override BoneGestureType GestureType => BoneGestureType.向左弯腰_流程;
        protected override float DirectionMultiplier => 1f;
        protected override int ReadReadyFrames(BoneParserConfig config) => config.m_LeftLeanReadyFrames;
        protected override float ReadTriggerOffsetRatio(BoneParserConfig config) => config.m_LeftLeanTriggerOffsetRatio;
        protected override float ReadReturnToNeutralRatio(BoneParserConfig config) => config.m_LeftLeanReturnToNeutralRatio;
        protected override float ReadMinSpeedRatioPerSecond(BoneParserConfig config) => config.m_LeftLeanMinSpeedRatioPerSecond;
        protected override float ReadCooldownSeconds(BoneParserConfig config) => config.m_LeftLeanCooldownSeconds;
    }

    internal sealed class RightLeanFlowRecognizer : SideLeanFlowRecognizer
    {
        protected override string StateKey => "Flow.RightLean";
        protected override BoneGestureType GestureType => BoneGestureType.向右弯腰_流程;
        protected override float DirectionMultiplier => -1f;
        protected override int ReadReadyFrames(BoneParserConfig config) => config.m_RightLeanReadyFrames;
        protected override float ReadTriggerOffsetRatio(BoneParserConfig config) => config.m_RightLeanTriggerOffsetRatio;
        protected override float ReadReturnToNeutralRatio(BoneParserConfig config) => config.m_RightLeanReturnToNeutralRatio;
        protected override float ReadMinSpeedRatioPerSecond(BoneParserConfig config) => config.m_RightLeanMinSpeedRatioPerSecond;
        protected override float ReadCooldownSeconds(BoneParserConfig config) => config.m_RightLeanCooldownSeconds;
    }

    internal abstract class StableBodyMotionFlowState : TriggerOnlyFlowState
    {
        public int m_ReadyFrameCount;
        public bool m_IsReady;
        public bool m_HasLastInput;
        public bool m_BaselineHasAnkleTracking;
        public int m_UnstableFrameCount;
        public int m_PendingRebaselineFrames;
        public int m_CrouchDownConfirmFrames;
        // 下蹲复位联锁：触发过必须站起才能再触发
        public bool m_RearmRequired;
        // 腾空联锁：最近出现过明显上升（刚起跳/在空中），锁定期内不建立下蹲基准也不判下蹲。
        public bool m_HasAirborneTracking;
        public float m_AirborneTrackShoulderY;
        public int m_AirborneTrackFrames;
        public float m_AirbornePeakRiseRatio;
        public int m_AirborneLockFrames;
        // 起跳联锁：触发过一次起跳后，到人回到站立高度之前的每一帧都是空中姿势，
        // 不能参与基准收集。跳跃顶点竖直速度接近 0，最容易被当成"稳定站立"收紧基准，
        // 一旦基准被抬到空中，落地后相对它永远是下降，表现为跳过一次就再也跳不起来。
        public bool m_JumpAirbornePending;
        public int m_JumpAirborneFrames;
        // 联锁期间肩部到过的最高点（屏幕坐标 Y 最小）。落地判据用它做相对比较，
        // 这样基准本身偏低时也不会因为"永远回不到基准"而把起跳锁死。
        public float m_JumpAirbornePeakY;
        public BoneVector2 m_BaselineShoulderCenter;
        public BoneVector2 m_BaselineHipCenter;
        public BoneVector2 m_BaselineAnkleCenter;
        public BoneVector2 m_LastShoulderCenter;
        public BoneVector2 m_LastHipCenter;
        public BoneVector2 m_LastAnkleCenter;

        public override void ResetFlowState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_HasLastInput = false;
            m_BaselineHasAnkleTracking = false;
            m_UnstableFrameCount = 0;
            m_PendingRebaselineFrames = 0;
            m_CrouchDownConfirmFrames = 0;
            m_RearmRequired = false;
            m_HasAirborneTracking = false;
            m_AirborneTrackShoulderY = 0f;
            m_AirborneTrackFrames = 0;
            m_AirbornePeakRiseRatio = 0f;
            m_AirborneLockFrames = 0;
            m_JumpAirbornePending = false;
            m_JumpAirborneFrames = 0;
            m_JumpAirbornePeakY = 0f;
            m_BaselineShoulderCenter = BoneVector2.m_Zero;
            m_BaselineHipCenter = BoneVector2.m_Zero;
            m_BaselineAnkleCenter = BoneVector2.m_Zero;
            m_LastShoulderCenter = BoneVector2.m_Zero;
            m_LastHipCenter = BoneVector2.m_Zero;
            m_LastAnkleCenter = BoneVector2.m_Zero;
        }

        // 触发被接受/拒绝后保留站立基准与上一帧输入，只清本次触发的过程状态。
        // 基准跟着清空的话，下一帧会拿触发瞬间的姿势重建站姿：起跳那侧会把空中
        // 高度当成站立高度，落地后相对它永远够不到上升阈值，跳过一次就再也跳不起来。
        public override void ResetTriggerState()
        {
            m_ReadyFrameCount = 0;
            m_IsReady = false;
            m_UnstableFrameCount = 0;
            m_PendingRebaselineFrames = 0;
            m_CrouchDownConfirmFrames = 0;
            m_RearmRequired = false;
            m_HasAirborneTracking = false;
            m_AirborneTrackShoulderY = 0f;
            m_AirborneTrackFrames = 0;
            m_AirbornePeakRiseRatio = 0f;
            m_AirborneLockFrames = 0;
        }

        public void StoreLastInput(FullBodyMovementInput input)
        {
            m_LastShoulderCenter = input.m_Torso.m_ShoulderCenter;
            m_LastHipCenter = input.m_Torso.m_HipCenter;
            m_LastAnkleCenter = input.m_AnkleCenter;
            m_HasLastInput = true;
        }

        // adoptCurrentAsBaseline 为 false 时竖直基准只保留更高的那个（屏幕坐标 Y 越小越高）。
        // 起跳识别器用这个模式：起身过程中帧间速度必然超限、会反复重启收集，若每次都把当前
        // 姿势写成基准，蹲姿或半起身姿势就会顶替站立基准，站起来那一下相对它就是一次足够的
        // 上升，被判成起跳。需要"采纳当前姿势为新站姿"的场景（下蹲识别器等）保持默认 true。
        public void RestartReadyCollection(FullBodyMovementInput input, bool adoptCurrentAsBaseline = true)
        {
            m_ReadyFrameCount = 1;
            m_IsReady = false;
            m_UnstableFrameCount = 0;
            m_PendingRebaselineFrames = 0;
            m_CrouchDownConfirmFrames = 0;
            m_BaselineHasAnkleTracking = input.m_HasAnkleTracking;
            if (!m_HasLastInput || adoptCurrentAsBaseline)
            {
                m_BaselineShoulderCenter = input.m_Torso.m_ShoulderCenter;
                m_BaselineHipCenter = input.m_Torso.m_HipCenter;
                m_BaselineAnkleCenter = input.m_AnkleCenter;
            }
            else
            {
                // 只跟随水平漂移，竖直基准完全不动：重启发生在这人正起身或起跳的抖动里，
                // 此刻的高度既不是站姿也不是蹲姿，写进去就会伪造出一个新的"站立高度"。
                m_BaselineShoulderCenter.m_X = input.m_Torso.m_ShoulderCenter.m_X;
                m_BaselineHipCenter.m_X = input.m_Torso.m_HipCenter.m_X;
            }

            StoreLastInput(input);
        }

        public void AccumulateBaseline(FullBodyMovementInput input)
        {
            float blend = 1f / Math.Max(1, m_ReadyFrameCount);
            m_BaselineShoulderCenter.m_X = BoneMath.Lerp(m_BaselineShoulderCenter.m_X, input.m_Torso.m_ShoulderCenter.m_X, blend);
            m_BaselineHipCenter.m_X = BoneMath.Lerp(m_BaselineHipCenter.m_X, input.m_Torso.m_HipCenter.m_X, blend);
            m_BaselineAnkleCenter = Lerp(m_BaselineAnkleCenter, input.m_AnkleCenter, blend);
            // 基准高度只向站立方向收紧（屏幕坐标 Y 越小越高）：半蹲姿态停稳不会把基准钉低。
            // 否则下蹲/起跳必须达到"相对半蹲"的深度，物理上做不到，表现为蹲/跳失灵后一直失灵。
            // 这里必须直接和输入比较、不能和本轮插值结果比较：收集重启后的第一帧 blend = 1，
            // 插值结果恰好等于当前姿势，蹲姿会原样写进基准，起身时相对它就成了"肩髋同时上升"。
            m_BaselineShoulderCenter.m_Y = Math.Min(m_BaselineShoulderCenter.m_Y, input.m_Torso.m_ShoulderCenter.m_Y);
            m_BaselineHipCenter.m_Y = Math.Min(m_BaselineHipCenter.m_Y, input.m_Torso.m_HipCenter.m_Y);
            m_BaselineAnkleCenter.m_Y = Math.Min(m_BaselineAnkleCenter.m_Y, input.m_AnkleCenter.m_Y);
        }

        // 起跳联锁：触发过一跳之后，到人回到站立高度之前的每一帧都是空中姿势。
        // 跳跃顶点竖直速度接近 0，最容易被当成"稳定站立"写进基准；一旦基准被抬到空中，
        // 落地后相对它就是下降，肩髋再也达不到上升阈值，表现为跳过一次就跳不起来，
        // 只有做了会重建基准的动作（蹲下、横移等）才恢复。下蹲侧有同样的问题和解法，
        // 但那里的联锁只保护下蹲，起跳必须自己有一份。
        public void UpdateJumpAirborneGuard(FullBodyMovementInput input)
        {
            if (!m_JumpAirbornePending)
            {
                return;
            }

            float shoulderY = input.m_Torso.m_ShoulderCenter.m_Y;
            if (shoulderY < m_JumpAirbornePeakY)
            {
                m_JumpAirbornePeakY = shoulderY;
            }

            // 判据一：回到站立基准。基准可信时落地那一帧就成立。
            float shoulderBelowBaseline = shoulderY - m_BaselineShoulderCenter.m_Y;
            // 判据二：从最高点回落超过一个起跳阈值。基准偏低时判据一永远不成立，
            // 而回落的绝对幅度是自洽的——触发帧已经保证身体比出手前高出那么多。
            float descentFromPeak = shoulderY - m_JumpAirbornePeakY;
            if (shoulderBelowBaseline >= 0f ||
                descentFromPeak >= input.m_Torso.m_ShoulderWidth * RunnerMovementFlowUtility.m_JumpAirborneDescentRatio ||
                ++m_JumpAirborneFrames > RunnerMovementFlowUtility.m_JumpAirborneGuardMaxFrames)
            {
                m_JumpAirbornePending = false;
                m_JumpAirborneFrames = 0;
                m_JumpAirbornePeakY = 0f;
            }
        }

        private static BoneVector2 Lerp(BoneVector2 start, BoneVector2 end, float blend)
        {
            return new BoneVector2(
                BoneMath.Lerp(start.m_X, end.m_X, blend),
                BoneMath.Lerp(start.m_Y, end.m_Y, blend));
        }
    }

    internal sealed class JumpFlowState : StableBodyMotionFlowState
    {
    }

    internal sealed class JumpFlowRecognizer : TriggerOnlyFlowRecognizer<JumpFlowState>
    {
        protected override string StateKey => "Flow.Jump";
        protected override BoneGestureType GestureType => BoneGestureType.跳起来_流程;

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, JumpFlowState state)
        {
            if (!RunnerMovementFlowUtility.TryReadFullBodyInput(context, out FullBodyMovementInput input))
            {
                state.ResetFlowState();
                return false;
            }

            if (!state.m_HasLastInput)
            {
                state.m_RearmRequired = false;
                state.RestartReadyCollection(input);
                return false;
            }

            state.UpdateJumpAirborneGuard(input);
            if (state.m_JumpAirbornePending)
            {
                // 空中任何姿势都不是站姿：这整段不收集基准（竖直方向尤其不能），
                // 落地后从零重新收集，顺便形成"一次跳跃之后必须重新站稳"的最小间隔。
                state.m_ReadyFrameCount = 0;
                state.m_UnstableFrameCount = 0;
                state.m_IsReady = false;
                state.StoreLastInput(input);
                return false;
            }

            float maxStableSpeed = input.m_Torso.m_ShoulderWidth *
                context.m_Config.m_JumpReadyMaxVerticalSpeedRatioPerSecond;
            float measuredSpeed = RunnerMovementFlowUtility.ReadMaxTorsoVerticalSpeed(
                input, state.m_LastShoulderCenter, state.m_LastHipCenter, context.m_DeltaTimeSeconds);
            if (!state.m_IsReady)
            {
                // 起跳只能从站姿开始；已处于蹲姿时不武装，蹲下再站起不会触发跳。
                if (BonePoseDetection.ReadIsCrouching(context))
                {
                    state.m_ReadyFrameCount = 0;
                    state.m_UnstableFrameCount = 0;
                    state.StoreLastInput(input);
                    return false;
                }

                if (measuredSpeed > maxStableSpeed)
                {
                    if (++state.m_UnstableFrameCount > RunnerMovementFlowUtility.m_BodyMotionReadySpikeToleranceFrames)
                    {
                        state.m_RearmRequired = false;
                        // 起跳这一侧不能采纳当前姿势：起身抖动超限时人正处在半起身高度。
                        state.RestartReadyCollection(input, false);
                    }
                    else
                    {
                        state.StoreLastInput(input);
                    }

                    return false;
                }

                state.m_UnstableFrameCount = 0;
                state.m_ReadyFrameCount++;
                state.AccumulateBaseline(input);
                state.StoreLastInput(input);
                state.m_IsReady = state.m_ReadyFrameCount >= Math.Max(1, context.m_Config.m_JumpReadyFrames);
                return false;
            }

            float shoulderRise = state.m_BaselineShoulderCenter.m_Y - input.m_Torso.m_ShoulderCenter.m_Y;
            float hipRise = state.m_BaselineHipCenter.m_Y - input.m_Torso.m_HipCenter.m_Y;
            // 自然起跳都会先有下蹲蓄力，且深度不定：单纯下沉不再重置基准，站立基准
            // 保持不变，随后的上升仍相对站姿判定。进入蹲姿立即解除武装；低于基准的
            // 停留地也不采纳为新站姿——蹲多久都一样，站起来只是回到基准。
            if (BonePoseDetection.ReadIsCrouching(context))
            {
                state.m_RearmRequired = false;
                // 蹲着只解除武装，绝不重设基准：重开收集会把蹲姿写成站立基准，之后站起来
                // 相对蹲姿就是"肩髋同时上升并带速度"，正是"一直蹲着再站起来触发起跳"的来源。
                state.m_ReadyFrameCount = 0;
                state.m_UnstableFrameCount = 0;
                state.m_PendingRebaselineFrames = 0;
                state.m_IsReady = false;
                state.StoreLastInput(input);
                return false;
            }

            // 低位绝不采纳为新站姿：被采纳的下降量必然原样等于之后"站起来"的上升量，
            // 而能走进这个判断的下降量已经超过触发阈值，起身那一下就会正好满足上升
            // 阈值——这正是"蹲一会儿再站起来被判成起跳"。蹲多久都一样：基准始终是
            // 站立高度，站起来只是回到基准，rise 归零。
            state.m_PendingRebaselineFrames = 0;

            if (context.m_DeltaTimeSeconds <= 0f)
            {
                state.StoreLastInput(input);
                return false;
            }

            float shoulderUpSpeed = (state.m_LastShoulderCenter.m_Y - input.m_Torso.m_ShoulderCenter.m_Y) / context.m_DeltaTimeSeconds;
            float hipUpSpeed = (state.m_LastHipCenter.m_Y - input.m_Torso.m_HipCenter.m_Y) / context.m_DeltaTimeSeconds;
            state.StoreLastInput(input);
            float minUpSpeed = input.m_Torso.m_ShoulderWidth * context.m_Config.m_JumpMinUpwardSpeedRatioPerSecond;
            // 触发条件完全排除脚踝：起跳瞬间脚踝常被预测在原地附近，曾直接否决掉
            // 完全有效的起跳。肩髋同步上升并带真实上升速度已经是跳跃专属信号。
            bool triggered = shoulderRise >= input.m_Torso.m_ShoulderWidth * context.m_Config.m_JumpMinShoulderRiseRatio &&
                hipRise >= input.m_Torso.m_ShoulderWidth * context.m_Config.m_JumpMinHipRiseRatio &&
                (shoulderUpSpeed >= minUpSpeed || hipUpSpeed >= minUpSpeed);
            if (triggered)
            {
                // 从这一帧起身体已经离开站立位置，到落地前都不能再拿姿势去收紧基准。
                state.m_JumpAirbornePending = true;
                state.m_JumpAirborneFrames = 0;
                state.m_JumpAirbornePeakY = input.m_Torso.m_ShoulderCenter.m_Y;
            }

            return triggered;
        }

        protected override float ReadCooldownSeconds(BoneParserConfig config) => config.m_JumpCooldownSeconds;
    }

    internal sealed class CrouchDownFlowState : StableBodyMotionFlowState
    {
    }

    internal sealed class CrouchDownFlowRecognizer : TriggerOnlyFlowRecognizer<CrouchDownFlowState>
    {
        protected override string StateKey => "Flow.CrouchDown";
        protected override BoneGestureType GestureType => BoneGestureType.蹲下去_流程;

        protected override bool EvaluateFlow(BoneGestureRuntimeContext context, CrouchDownFlowState state)
        {
            if (!RunnerMovementFlowUtility.TryReadFullBodyInput(context, out FullBodyMovementInput input))
            {
                state.ResetFlowState();
                return false;
            }

            if (!state.m_HasLastInput)
            {
                state.m_RearmRequired = false;
                state.RestartReadyCollection(input);
                return false;
            }

            float maxStableSpeed = input.m_Torso.m_ShoulderWidth *
                context.m_Config.m_CrouchDownReadyMaxVerticalSpeedRatioPerSecond;
            float measuredSpeed = RunnerMovementFlowUtility.ReadMaxTorsoVerticalSpeed(
                input, state.m_LastShoulderCenter, state.m_LastHipCenter, context.m_DeltaTimeSeconds);
            RunnerMovementFlowUtility.UpdateAirborneLock(context.m_Config, state, input);
            if (!state.m_IsReady)
            {
                if (BonePoseDetection.ReadIsCrouching(context))
                {
                    state.m_ReadyFrameCount = 0;
                    state.m_UnstableFrameCount = 0;
                    state.StoreLastInput(input);
                    return false;
                }

                // 刚起跳或仍在空中：不收集站立基准，避免把起飞高度当成站立高度。
                if (state.m_AirborneLockFrames > 0)
                {
                    state.m_ReadyFrameCount = 0;
                    state.m_UnstableFrameCount = 0;
                    state.m_CrouchDownConfirmFrames = 0;
                    state.StoreLastInput(input);
                    return false;
                }

                if (measuredSpeed > maxStableSpeed)
                {
                    if (++state.m_UnstableFrameCount > RunnerMovementFlowUtility.m_BodyMotionReadySpikeToleranceFrames)
                    {
                        state.m_ReadyFrameCount = 0;
                        state.m_UnstableFrameCount = 0;
                    }

                    state.StoreLastInput(input);
                    return false;
                }

                state.m_UnstableFrameCount = 0;
                if (state.m_ReadyFrameCount <= 0)
                {
                    state.m_RearmRequired = false;
                    state.RestartReadyCollection(input);
                    return false;
                }

                state.m_ReadyFrameCount++;
                state.AccumulateBaseline(input);
                state.StoreLastInput(input);
                state.m_IsReady = state.m_ReadyFrameCount >= Math.Max(1, context.m_Config.m_CrouchDownReadyFrames);
                return false;
            }

            float shoulderDrop = input.m_Torso.m_ShoulderCenter.m_Y - state.m_BaselineShoulderCenter.m_Y;
            float hipDrop = input.m_Torso.m_HipCenter.m_Y - state.m_BaselineHipCenter.m_Y;
            float baselineLegHeight = state.m_BaselineAnkleCenter.m_Y - state.m_BaselineHipCenter.m_Y;
            float currentLegHeight = input.m_AnkleCenter.m_Y - input.m_Torso.m_HipCenter.m_Y;
            float legCompression = baselineLegHeight - currentLegHeight;
            // 起跳、下落与落地缓冲都不是玩家主动下蹲：锁定期内既不重设基准也不触发。
            if (state.m_AirborneLockFrames > 0)
            {
                state.m_CrouchDownConfirmFrames = 0;
                state.StoreLastInput(input);
                return false;
            }

            if (hipDrop < -input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinHipDropRatio * 0.5f)
            {
                state.m_RearmRequired = false;
                state.RestartReadyCollection(input);
                return false;
            }

            if (context.m_DeltaTimeSeconds <= 0f)
            {
                state.StoreLastInput(input);
                return false;
            }

            float shoulderDownSpeed = (input.m_Torso.m_ShoulderCenter.m_Y - state.m_LastShoulderCenter.m_Y) / context.m_DeltaTimeSeconds;
            float hipDownSpeed = (input.m_Torso.m_HipCenter.m_Y - state.m_LastHipCenter.m_Y) / context.m_DeltaTimeSeconds;
            state.StoreLastInput(input);
            // 触发后必须站起才能再次触发，否则保持蹲姿时冷却到期会连判两次
            if (state.m_RearmRequired)
            {
                return false;
            }

            float minDownSpeed = input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinDownwardSpeedRatioPerSecond;
            // 腿部压缩只有在基准帧和当前帧都有脚踝数据时才作为附加确认；
            // 脚踝不可跟踪时依靠肩髋同步下降区分下蹲与弯腰等动作。
            bool legConfirmed = !(input.m_HasAnkleTracking && state.m_BaselineHasAnkleTracking) ||
                legCompression >= input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinLegCompressionRatio;
            float hipDropThreshold = input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinHipDropRatio;
            // 起跳前的反向下压同样会让下降+速度条件短暂满足。蹲下必须保持
            // 在髋部下降阈值以下满配置的帧数才真正触发；反向下压会很快反弹
            // 回到阈值之上，从而取消待触发的蹲事件。
            int confirmHoldFrames = context.m_Config.m_CrouchDownConfirmHoldFrames;
            if (state.m_CrouchDownConfirmFrames > 0)
            {
                // 确认期间身体已经开始上冲：这是起跳前的蓄力半蹲，撤销这次下蹲。
                // 真蹲下去的人会保持或继续下沉，只有起跳前摇才会在这个窗口内反向上升。
                if (hipDownSpeed <= -minDownSpeed * 0.75f)
                {
                    state.m_CrouchDownConfirmFrames = 0;
                    return false;
                }

                if (hipDrop < hipDropThreshold)
                {
                    state.m_CrouchDownConfirmFrames = 0;
                    return false;
                }

                state.m_CrouchDownConfirmFrames++;
                if (state.m_CrouchDownConfirmFrames < confirmHoldFrames)
                {
                    return false;
                }

                state.m_CrouchDownConfirmFrames = 0;
                state.m_RearmRequired = true;
                return true;
            }

            bool fullTrigger = shoulderDrop >= input.m_Torso.m_ShoulderWidth * context.m_Config.m_CrouchDownMinShoulderDropRatio &&
                hipDrop >= hipDropThreshold &&
                legConfirmed &&
                (shoulderDownSpeed >= minDownSpeed || hipDownSpeed >= minDownSpeed);
            if (fullTrigger)
            {
                if (confirmHoldFrames <= 1)
                {
                    state.m_RearmRequired = true;
                    return true;
                }

                state.m_CrouchDownConfirmFrames = 1;
            }

            return false;
        }

        protected override float ReadCooldownSeconds(BoneParserConfig config) => config.m_CrouchDownCooldownSeconds;
    }

    internal sealed class SingleHandSideSweepHandState
    {
        public bool m_IsReady;
        public bool m_IsMoving;
        public float m_ReadyWristHipOffsetRatio;
        public bool m_HasLastWrist;
        public float m_LastWristHipOffsetRatio;
        public int m_WristMissingFrameCount;
        public bool m_HasLastElbow;
        public float m_LastElbowHipOffsetRatio;
        public int m_ElbowMissingFrameCount;
        public float m_MotionSeconds;
        // 预测式提前触发的高速确认帧数。
        public int m_FastFrameCount;
        // 本次挥手的起手事件是否已发出，保证一次挥手只预播一次动作。
        public bool m_IsStartEmitted;
        // 停手兜底的连续确认帧数。
        public int m_StopFrameCount;
        // 出招后累计的"连续向下退回"行程，用来判断这次挥手是不是已经收手。
        // 一次挥手只能出一颗子弹，判定全程只看行程，不看时间，所以挥得再快也不会被吞掉。
        public float m_PullbackRatio;
        public bool m_RequiresRearmPullback;
        // arm 那一刻冻结的身体参考系：人体纵轴方向 + 左右肩的图像距离。
        // 侧身是绕人体纵轴旋转——纵轴方向几乎不变，但左右肩在图像上的距离会随转角持续缩短，
        // 而肩宽是所有高度比值的分母。继续拿当前帧肩宽去除，手明明停在髋上方不动，比值也会
        // 越除越大：手在髋上方 0.3 个肩宽时，肩宽缩到六成就能凭空多出 0.2 个肩宽的行程，
        // 足够走完所有出口。冻结参考系之后，侧身不再产生任何行程。
        public bool m_HasReadyFrame;
        public BoneVector2 m_ReadyBodyUpAxis;
        public float m_ReadyShoulderSpan;

        // arm 那一刻“手腕相对同侧肘”的竖直差（肩宽，正数表示手腕在肘下方）。
        // 侧身、跳跃、弯腰都只是整条手臂跟着躯干走，这个差量恒定；只有手臂自己举起来才会变。
        public bool m_HasReadyForearm;
        public float m_ReadyWristElbowOffsetRatio;
        // 最近一帧的腕肘竖直差，肘关键点丢失时沿用旧值，等于自动冻结。
        public float m_LastWristElbowDropRatio;

        public void Reset()
        {
            m_IsReady = false;
            m_IsMoving = false;
            m_ReadyWristHipOffsetRatio = 0f;
            m_HasLastWrist = false;
            m_LastWristHipOffsetRatio = 0f;
            m_WristMissingFrameCount = 0;
            m_HasLastElbow = false;
            m_LastElbowHipOffsetRatio = 0f;
            m_ElbowMissingFrameCount = 0;
            m_MotionSeconds = 0f;
            m_FastFrameCount = 0;
            m_IsStartEmitted = false;
            m_StopFrameCount = 0;
            m_HasReadyFrame = false;
            m_HasReadyForearm = false;
        }
    }

    internal sealed class SingleHandSideSweepFlowState : IBoneGestureRuntimeState
    {
        public readonly SingleHandSideSweepHandState m_LeftHand = new SingleHandSideSweepHandState();
        public readonly SingleHandSideSweepHandState m_RightHand = new SingleHandSideSweepHandState();
        public int m_FirstPendingActionEventId;
        public int m_SecondPendingActionEventId;
        public int m_PendingActionEventCount;
        // 等消费回写的帧数，超时由识别器放弃这两条事件。
        public int m_PendingWaitFrames;
        public bool m_HasLastTriggerTime;
        public float m_LastTriggerTimeSeconds;
        // 肩宽投影塌缩抑制：上一帧采信过的肩宽，左右手共用同一把尺子。
        public bool m_HasLastShoulderSpan;
        public float m_LastShoulderSpan;

        public void Reset()
        {
            ResetFlowState();
            m_FirstPendingActionEventId = 0;
            m_SecondPendingActionEventId = 0;
            m_PendingActionEventCount = 0;
            m_HasLastTriggerTime = false;
            m_LastTriggerTimeSeconds = 0f;
        }

        public void ResetFlowState()
        {
            m_LeftHand.Reset();
            m_RightHand.Reset();
            m_HasLastShoulderSpan = false;
            m_LastShoulderSpan = 0f;
        }

        public void AddPendingActionEvent(int actionEventId)
        {
            if (actionEventId <= 0 || m_PendingActionEventCount >= 2)
            {
                return;
            }

            if (m_PendingActionEventCount == 0)
            {
                m_FirstPendingActionEventId = actionEventId;
            }
            else
            {
                m_SecondPendingActionEventId = actionEventId;
            }

            m_PendingActionEventCount++;
        }

        public bool RemovePendingActionEvent(int actionEventId)
        {
            if (m_PendingActionEventCount <= 0 || actionEventId <= 0)
            {
                return false;
            }

            if (m_FirstPendingActionEventId == actionEventId)
            {
                m_FirstPendingActionEventId = m_SecondPendingActionEventId;
                m_SecondPendingActionEventId = 0;
                m_PendingActionEventCount--;
                return true;
            }

            if (m_PendingActionEventCount > 1 && m_SecondPendingActionEventId == actionEventId)
            {
                m_SecondPendingActionEventId = 0;
                m_PendingActionEventCount--;
                return true;
            }

            return false;
        }
    }

    internal sealed class SingleHandSideSweepFlowRecognizer : IBoneGestureRecognizer
    {
        private const string m_StateKey = "Flow.SingleHandSideSweep";
        private const int m_PendingTimeoutFrames = 10;

        public void CollectDefinitions(System.Collections.Generic.List<BoneGestureDefinition> definitions)
        {
            definitions.Add(new BoneGestureDefinition(
                BoneGestureType.单手外侧下方向中间上方挥手_流程,
                BoneGestureCategory.流程,
                BoneGesturePhaseMask.触发,
                BoneGesturePhaseMask.触发));
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            context.m_SeatState.ReadGestureState<SingleHandSideSweepFlowState>(m_StateKey)?.Reset();
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            SingleHandSideSweepFlowState state =
                context.m_SeatState.ReadGestureState<SingleHandSideSweepFlowState>(m_StateKey);
            if (state == null)
            {
                return;
            }

            if (state.m_PendingActionEventCount > 0)
            {
                // 回执只能由驱动主动回写，那一帧派发被跳过就永远等不到。等够上限就放弃
                // 这两条事件重新识别，否则挥手出招会一直失效到重启。
                if (++state.m_PendingWaitFrames <= m_PendingTimeoutFrames)
                {
                    return;
                }

                state.m_FirstPendingActionEventId = 0;
                state.m_SecondPendingActionEventId = 0;
                state.m_PendingActionEventCount = 0;
                state.m_PendingWaitFrames = 0;
            }
            else
            {
                state.m_PendingWaitFrames = 0;
            }

            if (!context.HasRecognizableActionBinding(BoneGestureType.单手外侧下方向中间上方挥手_流程))
            {
                state.ResetFlowState();
                return;
            }

            if (!EvaluateFlow(context, state, out bool leftTriggered, out bool rightTriggered))
            {
                return;
            }

            // 左右手各判定各的：满足条件的手都会出招，两个事件分别对应两颗子弹。
            if (leftTriggered)
            {
                EmitTrigger(context, state, BoneGestureTriggerHand.左手);
            }

            if (rightTriggered)
            {
                EmitTrigger(context, state, BoneGestureTriggerHand.右手);
            }
        }

        public bool TryApplyConsumeResult(
            BoneGestureRuntimeContext context,
            BoneActionConsumeResult consumeResult)
        {
            SingleHandSideSweepFlowState state =
                context.m_SeatState.ReadGestureState<SingleHandSideSweepFlowState>(m_StateKey);
            return state != null &&
                consumeResult != null &&
                state.RemovePendingActionEvent(consumeResult.m_ActionEventId);
        }

        private static bool EvaluateFlow(
            BoneGestureRuntimeContext context,
            SingleHandSideSweepFlowState state,
            out bool leftTriggered,
            out bool rightTriggered)
        {
            leftTriggered = false;
            rightTriggered = false;
            if (!RunnerMovementFlowUtility.TryReadTorsoInput(context, out TorsoMovementInput input))
            {
                state.ResetFlowState();
                return false;
            }

            if (!TryReadBodyUpAxis(input, out BoneVector2 bodyUpAxis, out float shoulderSpan))
            {
                state.ResetFlowState();
                return false;
            }

            // 肩宽单帧限速：转身时双肩在图像上重叠会让肩宽骤降，转回来又骤增，而它是所有高度比值的
            // 分母。分母单帧突变会把完全没动的手读成大幅上挥或下落，所以每帧只放行这么多变化。
            if (state.m_HasLastShoulderSpan)
            {
                float maxSpanDelta = state.m_LastShoulderSpan * MaxSpanChangeRatio;
                float spanDelta = shoulderSpan - state.m_LastShoulderSpan;
                if (spanDelta > maxSpanDelta)
                {
                    shoulderSpan = state.m_LastShoulderSpan + maxSpanDelta;
                }
                else if (spanDelta < -maxSpanDelta)
                {
                    shoulderSpan = state.m_LastShoulderSpan - maxSpanDelta;
                }
            }

            state.m_HasLastShoulderSpan = true;
            state.m_LastShoulderSpan = shoulderSpan;

            float minScore = context.m_Config.m_GestureKeypointMinConfidence;
            bool hasLeftWrist = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.左手腕,
                minScore,
                out BoneVector2 leftWrist);
            bool hasRightWrist = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.右手腕,
                minScore,
                out BoneVector2 rightWrist);
            bool hasLeftElbow = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.左肘,
                minScore,
                out BoneVector2 leftElbow);
            bool hasRightElbow = BoneSkeletonQuery.TryReadBodyJoint(
                context.m_Person,
                BoneBodyJointType.右肘,
                minScore,
                out BoneVector2 rightElbow);
            leftTriggered = EvaluateHand(
                context,
                state.m_LeftHand,
                input,
                AdditionalSkillFlowUtility.m_LeftSide,
                hasLeftWrist,
                leftWrist,
                hasLeftElbow,
                leftElbow,
                bodyUpAxis,
                shoulderSpan);
            rightTriggered = EvaluateHand(
                context,
                state.m_RightHand,
                input,
                AdditionalSkillFlowUtility.m_RightSide,
                hasRightWrist,
                rightWrist,
                hasRightElbow,
                rightElbow,
                bodyUpAxis,
                shoulderSpan);
            return leftTriggered || rightTriggered;
        }

        private static void EmitTrigger(
            BoneGestureRuntimeContext context,
            SingleHandSideSweepFlowState state,
            BoneGestureTriggerHand triggerHand)
        {
            float minIntervalSeconds = BoneMath.Max(
                0f,
                context.m_Config.m_SingleHandSideSweepMinTriggerIntervalSeconds);
            if (minIntervalSeconds > 0f &&
                state.m_HasLastTriggerTime &&
                context.m_FrameTimeSeconds - state.m_LastTriggerTimeSeconds < minIntervalSeconds)
            {
                return;
            }

            BoneActionEvent actionEvent = context.AddGestureEvent(
                BoneGestureType.单手外侧下方向中间上方挥手_流程,
                BoneGesturePhase.触发,
                triggerHand);
            if (actionEvent == null)
            {
                return;
            }

            state.m_HasLastTriggerTime = true;
            state.m_LastTriggerTimeSeconds = context.m_FrameTimeSeconds;
            if (actionEvent.m_RequiresConsumeResult)
            {
                state.AddPendingActionEvent(actionEvent.m_ActionEventId);
            }
        }

        // 起手门槛相对出招门槛的比例，和 C++ 识别库保持同一取值。
        private const float StartEmitSpeedFactor = 0.6f;
        // 停手兜底：手腕挥出到肘部高度的这个比例，且速度回落到出招门槛的这个比例以下，
        // 就认为这一下已经挥完，必须出招。
        private const float StopConfirmDistanceRatio = 0.65f;
        private const float StopSpeedRatio = 0.15f;
        // 一次挥手结束后，手腕要连续向下退回多少个"起步位移"才算真正收手。
        // 取 3 倍（约 150‰，大致 5 厘米）就是一段明确的放手动作；
        // 挥动途中的抖动是上下交替的，一往上就清零，不会被误判成收手。
        private const float RearmPullbackScale = 3f;
        // 所有出招出口共用的最小上挥行程（肩宽）。判据已改用 arm 那一刻冻结的身体参考系，
        // 侧身和跳跃都不会再产生行程，所以这条只用来滤掉骨骼点的残余抖动。
        // 真实挥手从髋到肘约 0.9 个肩宽，取 0.12 留足余量，不会吞掉动作。
        private const float MinSweepDistanceRatio = 0.12f;
        // 手腕相对同侧肘的最小抬高量（肩宽）。前臂长约 0.35 个肩宽，从贴着髋垂到手举到上方
        // 大约变化 0.7，取 0.15 只占两成，不会吞掉动作。侧身、跳跃、弯腰都只是整条手臂当刚体
        // 随躯干移动，腕肘相对向量一个分量都不变，只有手臂自己举起来才会变，所以这条判据对
        // 躯干的一切运动免疫——它才是“手真的抬起来了”最直接的证据。
        private const float MinForearmRiseRatio = 0.15f;
        // 出招时手腕必须到达的绝对高度：不得低于"双肩中心点下方 0.80 个肩宽"。
        // 前两条判据管的都是"手动了多少"，这条管的是"手到底在不在高处"——手在髋部小幅晃动
        // 可能攒出假行程，但绝对高度永远过不了这条。
        // 肩中心是人体最稳的参考点：侧身时它就在旋转轴上，跳跃下蹲时手腕和它一起平移，
        // 作为分母的肩宽也已经冻结，所以任何身体整体运动都只会让这个比值变大（手显得更低）、
        // 不会让它变小，失效方向天然安全。
        // 取值受"到肘高"出口反推：自然站立时肘在肩下方约 0.75 个肩宽，门槛若高于这个值就会把
        // 该出口永久挡住。0.80 略低于肘高，既放行正常挥手，又能挡住手贴髋（约 1.10）、
        // 垂在身侧与放在大腿上（1.30 以上）这些根本没抬起来的姿态。
        private const float MaxWristBelowShoulderRatio = 0.80f;
        // 起手预播要求的最小上挥行程（肩宽）。
        private const float StartEmitDistanceRatio = 0.10f;
        // 起步即发（快速触发）要求的最小上挥行程（肩宽）。
        private const float FastTriggerDistanceRatio = 0.15f;
        // 单帧高度比值允许的最大跳变（肩宽）。
        private const float MaxFrameRatioJump = 0.35f;
        // 肩宽投影单帧允许的最大变化比例。侧身时双肩在图像上重叠、肩宽骤降，转回来又骤增，而肩宽是
        // 所有高度比值的分母：分母抖动会把"手没动"读成大幅上挥或下落。这里对肩宽本身做限速，
        // 每帧最多变化这个比例——人慢慢走远走近能正常跟随，快速转身的那几帧被平滑掉。
        private const float MaxSpanChangeRatio = 0.25f;

        // 确认这一手出招：清识别状态，并记录出招高度要求先回撤再重新 arm。
        // 起步即发是在手腕刚离开起点时就出招的，此时手腕仍落在"起点姿势"容差里，
        // 若立刻重新 arm，手继续上挥会被判成第二次挥手，一次挥动就出两颗子弹。
        private static bool ConfirmTrigger(
            SingleHandSideSweepHandState state,
            BoneGestureRuntimeContext context,
            float wristHipOffsetRatio)
        {
            state.Reset();
            state.m_RequiresRearmPullback = true;
            state.m_PullbackRatio = 0f;
            // Reset 会清掉上一帧手腕，这里补回出招点的真实高度，
            // 出招后的逐帧回落行程才不会从 0 起算。
            state.m_HasLastWrist = true;
            state.m_LastWristHipOffsetRatio = wristHipOffsetRatio;
            return true;
        }

        private static bool EvaluateHand(
            BoneGestureRuntimeContext context,
            SingleHandSideSweepHandState state,
            TorsoMovementInput input,
            int side,
            bool hasWrist,
            BoneVector2 wrist,
            bool hasElbow,
            BoneVector2 elbow,
            BoneVector2 bodyUpAxis,
            float shoulderSpan)
        {
            if (!hasWrist)
            {
                int missingToleranceFrames = Math.Max(
                    0,
                    context.m_Config.m_SingleHandSideSweepWristMissingToleranceFrames);
                if (state.m_HasLastWrist &&
                    state.m_WristMissingFrameCount < missingToleranceFrames)
                {
                    state.m_WristMissingFrameCount++;
                    if (state.m_IsReady)
                    {
                        state.m_MotionSeconds += BoneMath.Max(0f, context.m_DeltaTimeSeconds);
                        if (state.m_MotionSeconds > context.m_Config.m_SingleHandSideSweepMaxDurationSeconds)
                        {
                            state.Reset();
                        }
                    }

                    return false;
                }

                state.Reset();
                return false;
            }

            state.m_WristMissingFrameCount = 0;
            // 已经 arm 过就用冻结的参考系，否则用当前帧（此时只是判断"手现在贴不贴髋"）。
            BoneVector2 frameUpAxis = bodyUpAxis;
            float frameShoulderSpan = shoulderSpan;
            if (state.m_HasReadyFrame && state.m_ReadyShoulderSpan > 0.0001f)
            {
                frameUpAxis = state.m_ReadyBodyUpAxis;
                frameShoulderSpan = state.m_ReadyShoulderSpan;
            }

            if (hasElbow)
            {
                state.m_HasLastElbow = true;
                state.m_LastElbowHipOffsetRatio = ReadHipOffsetRatio(input, side, elbow, frameUpAxis, frameShoulderSpan);
                state.m_ElbowMissingFrameCount = 0;
                // 腕肘竖直差：正数表示手腕在肘下方。侧身、跳跃、弯腰都不改变它。
                if (frameShoulderSpan > 0.0001f)
                {
                    state.m_LastWristElbowDropRatio = (wrist.m_Y - elbow.m_Y) / frameShoulderSpan;
                }
            }
            else
            {
                int missingToleranceFrames = Math.Max(
                    0,
                    context.m_Config.m_SingleHandSideSweepElbowMissingToleranceFrames);
                if (state.m_HasLastElbow &&
                    state.m_ElbowMissingFrameCount < missingToleranceFrames)
                {
                    state.m_ElbowMissingFrameCount++;
                }
                else if (state.m_IsMoving)
                {
                    state.Reset();
                    return false;
                }
                else
                {
                    state.m_HasLastElbow = false;
                    state.m_ElbowMissingFrameCount = 0;
                }
            }

            float wristHipOffsetRatio = ReadHipOffsetRatio(input, side, wrist, frameUpAxis, frameShoulderSpan);
            bool hadLastWrist = state.m_HasLastWrist;
            float lastWristHipOffsetRatio = state.m_LastWristHipOffsetRatio;
            // 单帧跳变限幅：关键点重识别、躯干轴突跳都会让这一帧的差分失去意义。真实挥手每帧大约
            // 只移动 0.1 个肩宽，超过限幅的量级一定是噪声，直接丢弃，同时清掉速度与停手计数，
            // 避免把一帧突跳当成“高速上挥”再确认出招。
            if (hadLastWrist &&
                Math.Abs(wristHipOffsetRatio - lastWristHipOffsetRatio) > MaxFrameRatioJump)
            {
                state.m_FastFrameCount = 0;
                state.m_StopFrameCount = 0;
                state.m_IsMoving = false;
                state.m_HasLastWrist = true;
                state.m_LastWristHipOffsetRatio = wristHipOffsetRatio;
                return false;
            }

            state.m_HasLastWrist = true;
            state.m_LastWristHipOffsetRatio = wristHipOffsetRatio;
            // 出招之后，只有等手腕连续向下退回一段明确的行程，才认为这次挥手结束、允许重新 arm。
            // 不能只看"比出招点低多少"：一次上挥在途中本来就可能回撤几厘米，一旦当时解锁，
            // 手接着往上挥又会出一颗，这就是一次挥手两颗子弹的来源。往下退途中只要手重新往上，
            // 累计立刻清零，所以上挥途中的抖动不会被误判成收手；快速连挥时那一下下放足够长，
            // 每一下都能重新出招，不受任何时间限制。
            if (state.m_RequiresRearmPullback)
            {
                float downwardRatio = wristHipOffsetRatio - lastWristHipOffsetRatio;
                state.m_PullbackRatio = downwardRatio > 0f
                    ? state.m_PullbackRatio + downwardRatio
                    : 0f;
                if (state.m_PullbackRatio <
                    context.m_Config.m_SingleHandSideSweepStartMovementDistanceRatio * RearmPullbackScale)
                {
                    return false;
                }

                state.m_RequiresRearmPullback = false;
                state.m_PullbackRatio = 0f;
            }

            if (!state.m_IsReady)
            {
                if (ReadIsStartPose(context.m_Config, wristHipOffsetRatio))
                {
                    ArmAtHip(
                        state,
                        wristHipOffsetRatio,
                        bodyUpAxis,
                        shoulderSpan);
                    return false;
                }

                float startTopRatio = -context.m_Config.m_SingleHandSideSweepStartAboveHipToleranceRatio;
                bool crossedStartUpward = hadLastWrist &&
                    lastWristHipOffsetRatio >= startTopRatio &&
                    wristHipOffsetRatio < startTopRatio;
                if (!crossedStartUpward)
                {
                    return false;
                }

                ArmAtHip(
                    state,
                    startTopRatio,
                    bodyUpAxis,
                    shoulderSpan);
            }

            // 手腕相对同侧肘抬起了多少（肩宽）。侧身、跳跃、弯腰都是整条手臂当刚体随躯干动，
            // 腕肘相对向量一个分量都不变，只有手臂自己举起来才会变。它比“腕相对髋”干净得多：
            // 髋和腕之间隔着整条躯干，躯干一转一弯就会把两者拉开。肘关键点丢失时沿用上一帧的
            // 差值，等于自动冻结，不会因为遮挡而误判。
            float forearmRiseRatio = 0f;
            if (state.m_HasReadyForearm)
            {
                forearmRiseRatio = state.m_ReadyWristElbowOffsetRatio - state.m_LastWristElbowDropRatio;
            }

            bool forearmConfirmed =
                !state.m_HasReadyForearm || forearmRiseRatio >= MinForearmRiseRatio;

            // 手腕当前在双肩中心点下方多远（肩宽）。肩宽拿不到有效值时不阻塞判断。
            float wristBelowShoulderRatio = 0f;
            bool wristHeightConfirmed = true;
            if (frameShoulderSpan > 0.0001f)
            {
                wristBelowShoulderRatio =
                    (wrist.m_Y - input.m_ShoulderCenter.m_Y) / frameShoulderSpan;
                wristHeightConfirmed = wristBelowShoulderRatio <= MaxWristBelowShoulderRatio;
            }

            state.m_MotionSeconds += BoneMath.Max(0f, context.m_DeltaTimeSeconds);
            if (state.m_MotionSeconds > context.m_Config.m_SingleHandSideSweepMaxDurationSeconds)
            {
                // 拖得太久：只要确实挥出去了就仍然出招，避免"手挥完却没有子弹"。
                bool sweptBeforeTimeout = HasSweptEnoughDistance(
                    state,
                    state.m_ReadyWristHipOffsetRatio - wristHipOffsetRatio,
                    context.m_Config) && forearmConfirmed && wristHeightConfirmed;
                if (sweptBeforeTimeout)
                {
                    return ConfirmTrigger(state, context, wristHipOffsetRatio);
                }

                state.Reset();
                return false;
            }

            float upwardDistanceRatio = state.m_ReadyWristHipOffsetRatio - wristHipOffsetRatio;
            // 冻结参考系下的行程和腕肘相对位置必须同时成立。两者挨的是两种完全不同的失效方式，
            // 一起误判的概率极低。
            bool sweepConfirmed =
                upwardDistanceRatio >= MinSweepDistanceRatio &&
                forearmConfirmed &&
                wristHeightConfirmed;
            if (!state.m_IsMoving)
            {
                if (upwardDistanceRatio < context.m_Config.m_SingleHandSideSweepStartMovementDistanceRatio)
                {
                    return false;
                }

                state.m_IsMoving = true;
            }

            // 起步即发：手腕已经离开起点并保持上挥速度，说明这一下是来真的，立即出招，
            // 不再等手腕到达肘部高度（骨骼数据本身有内容延迟，等到位时玩家已经挥完）。
            // 确认帧数填 0 关闭；速度门槛过滤慢速抬手。
            float upwardSpeedRatio = context.m_DeltaTimeSeconds > 0f
                ? (lastWristHipOffsetRatio - wristHipOffsetRatio) / context.m_DeltaTimeSeconds
                : 0f;

            // 起手预播：手腕一出现明显的上挥速度就先发开始事件，让游戏把攻击动作提前播起来，
            // 真正确认出招仍由后面的起步即发/预测/到肘高决定。走到这里说明手腕已经离开过起点线，
            // 手垂着不动不会进来，慢速抬手也过不了这条速度门槛。
            // 起手预播只服务于显式绑定了"开始"阶段的玩法（目前只有跑酷在业务层自己绑）。
            // 没有绑定的玩法连手势事件都不写，避免给牙齿这类只关心触发阶段的调用方多出事件。
            if (!state.m_IsStartEmitted &&
                context.HasActionBinding(
                    BoneGestureType.单手外侧下方向中间上方挥手_流程,
                    BoneGesturePhase.开始) &&
                upwardDistanceRatio >= StartEmitDistanceRatio &&
                forearmConfirmed &&
                context.m_Config.m_SingleHandSideSweepPredictiveMinUpSpeedRatioPerSecond > 0f &&
                upwardSpeedRatio >= context.m_Config.m_SingleHandSideSweepPredictiveMinUpSpeedRatioPerSecond *
                    StartEmitSpeedFactor)
            {
                state.m_IsStartEmitted = true;
                context.AddGestureEvent(
                    BoneGestureType.单手外侧下方向中间上方挥手_流程,
                    BoneGesturePhase.开始,
                    side == AdditionalSkillFlowUtility.m_LeftSide
                        ? BoneGestureTriggerHand.左手
                        : BoneGestureTriggerHand.右手);
            }

            state.m_FastFrameCount = upwardSpeedRatio >= context.m_Config.m_SingleHandSideSweepPredictiveMinUpSpeedRatioPerSecond
                ? state.m_FastFrameCount + 1
                : 0;
            if (context.m_Config.m_SingleHandSideSweepPredictiveMinUpSpeedRatioPerSecond > 0f &&
                context.m_Config.m_SingleHandSideSweepFastTriggerFrames >= 1 &&
                state.m_FastFrameCount >= context.m_Config.m_SingleHandSideSweepFastTriggerFrames &&
                upwardDistanceRatio >= FastTriggerDistanceRatio &&
                sweepConfirmed)
            {
                return ConfirmTrigger(state, context, wristHipOffsetRatio);
            }

            // 出招出口只剩四个：起步即发（上）、到肘高、停手兜底、超时兜底（下）。
            // 原先的"按速度预测提前触发"要的也是连续两帧达标速度，而起步即发在同一帧更早返回，
            // 那条分支永远走不到，已删除，避免以后有人以为它还生效。
            bool hasReachedElbowHeight = state.m_HasLastElbow &&
                wristHipOffsetRatio <= state.m_LastElbowHipOffsetRatio +
                context.m_Config.m_SingleHandSideSweepElbowHeightToleranceRatio &&
                sweepConfirmed;
            bool triggered = hasReachedElbowHeight;
            if (triggered)
            {
                return ConfirmTrigger(state, context, wristHipOffsetRatio);
            }

            // 停手兜底：手腕已经挥出大半行程、速度也回落，说明这一下挥完了。
            // 速度一直没达到起步即发门槛、或者肘部数据不可用时，靠这里保证"手停下必有子弹"。
            bool slowedDown = upwardSpeedRatio <=
                context.m_Config.m_SingleHandSideSweepPredictiveMinUpSpeedRatioPerSecond * StopSpeedRatio;
            state.m_StopFrameCount =
                HasSweptEnoughDistance(state, upwardDistanceRatio, context.m_Config) && slowedDown &&
                sweepConfirmed
                    ? state.m_StopFrameCount + 1
                    : 0;
            if (state.m_StopFrameCount >= 2)
            {
                return ConfirmTrigger(state, context, wristHipOffsetRatio);
            }

            return false;
        }

        // 手腕是否已经挥出了足够完成的行程。肘部高度可用时按肘高折算，
        // 否则退回起点移动距离的十倍，保证跑动摆臂那种短行程不会命中。
        private static bool HasSweptEnoughDistance(
            SingleHandSideSweepHandState state,
            float upwardDistanceRatio,
            BoneParserConfig config)
        {
            if (state.m_HasLastElbow)
            {
                float elbowDistance = state.m_ReadyWristHipOffsetRatio - state.m_LastElbowHipOffsetRatio;
                if (elbowDistance > 0f)
                {
                    return upwardDistanceRatio >= elbowDistance * StopConfirmDistanceRatio;
                }
            }

            return upwardDistanceRatio >= config.m_SingleHandSideSweepStartMovementDistanceRatio * 10f;
        }

        private static float ReadHipOffsetRatio(
            TorsoMovementInput input,
            int side,
            BoneVector2 point,
            BoneVector2 bodyUpAxis,
            float shoulderSpan)
        {
            BoneVector2 sameSideHip = side == AdditionalSkillFlowUtility.m_LeftSide
                ? input.m_LeftHip
                : input.m_RightHip;
            BoneVector2 hipToPoint = point - sameSideHip;
            float heightAlongBodyUp = hipToPoint.m_X * bodyUpAxis.m_X +
                hipToPoint.m_Y * bodyUpAxis.m_Y;
            return -heightAlongBodyUp / shoulderSpan;
        }

        private static bool TryReadBodyUpAxis(
            TorsoMovementInput input,
            out BoneVector2 bodyUpAxis,
            out float shoulderSpan)
        {
            shoulderSpan = BoneMath.Distance(input.m_LeftShoulder, input.m_RightShoulder);
            if (shoulderSpan <= 0.0001f)
            {
                bodyUpAxis = BoneVector2.m_Zero;
                return false;
            }

            BoneVector2 torsoUp = input.m_ShoulderCenter - input.m_HipCenter;
            float lengthSquared = torsoUp.m_X * torsoUp.m_X + torsoUp.m_Y * torsoUp.m_Y;
            if (lengthSquared <= 0.0001f)
            {
                bodyUpAxis = BoneVector2.m_Zero;
                return false;
            }

            float inverseLength = 1f / (float)Math.Sqrt(lengthSquared);
            bodyUpAxis = torsoUp * inverseLength;
            return true;
        }

        private static bool ReadIsStartPose(
            BoneParserConfig config,
            float hipOffsetRatio)
        {
            return hipOffsetRatio >= -config.m_SingleHandSideSweepStartAboveHipToleranceRatio &&
                hipOffsetRatio <= config.m_SingleHandSideSweepStartBelowHipToleranceRatio;
        }

        private static void ArmAtHip(
            SingleHandSideSweepHandState state,
            float wristHipOffsetRatio,
            BoneVector2 bodyUpAxis,
            float shoulderSpan)
        {
            state.m_IsReady = true;
            state.m_IsMoving = false;
            state.m_ReadyWristHipOffsetRatio = wristHipOffsetRatio;
            state.m_MotionSeconds = 0f;
            // 冻结这一帧的身体参考系。侧身时肩宽会持续缩短，一旦拿当前帧肩宽去除，
            // 停在原处的手会被读成一路向上挥；用冻结的尺子，侧身对判据完全无影响。
            state.m_HasReadyFrame = true;
            state.m_ReadyBodyUpAxis = bodyUpAxis;
            state.m_ReadyShoulderSpan = shoulderSpan;
            // 腕肘基准只在 arm 那一刻确实拿到肘数据时才建立。拿不到就退回只靠冻结参考系判断，
            // 否则这条会永远挡住出招。
            state.m_HasReadyForearm = state.m_HasLastElbow;
            state.m_ReadyWristElbowOffsetRatio =
                state.m_HasLastElbow ? state.m_LastWristElbowDropRatio : 0f;
        }
    }
}
