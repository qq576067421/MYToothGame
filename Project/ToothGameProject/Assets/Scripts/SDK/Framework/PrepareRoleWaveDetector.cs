using System;
using UnityEngine;
using YoudooGame.Core;
using YoudooGame.MatchFlow.Runtime;
using YoudooGame.Player;

// 框架会话模式下的准备界面挥手换角色。
// 判定：以人体中线（Chest / 双肩中点 / Nose 的 X）为界，手腕从一侧越到另一侧，
// 并且这次跨越必须是“快速摆动”——用该侧最外侧点到越线点的平均速度判定。
// 慢慢把手移回另一侧速度不够，不会触发。
// 其它约束：手腕在头部以下、髋部以上；举手姿势期间不判定，放手后再锁 0.45s；
// 判定后进入 0.45s 冷却。
public sealed class PrepareRoleWaveDetector : MonoBehaviour
{
    private const string kTag = "[新SDK整合][WaveRole]";
    private const int kMaxSeats = 4;
    private const int kHandCount = 2;
    private const float kMinConfidence = 0.3f;
    private const float kShoulderWidthEpsilon = 1e-4f;
    private const float kOverheadMarginRatio = 0.05f;
    private const float kMidlineMarginRatio = 0.12f;
    private const float kMinSwingSpeedRatioPerSecond = 0.8f;
    private const float kMaxSwingSeconds = 0.6f;
    private const float kWaveCooldownSeconds = 0.45f;

    public static PrepareRoleWaveDetector Instance { get; private set; }

    public event Action<int, int> RoleSwitchRequested;

    public bool Active { get; set; }

    // 每个座位每只手：当前所在侧（-1 中线左，1 中线右，0 未知），
    // 以及该侧最外侧点的 X 与时间，用于计算本次跨越的平均速度。
    private readonly int[] m_Side = new int[kMaxSeats * kHandCount];
    private readonly float[] m_ExtremeX = new float[kMaxSeats * kHandCount];
    private readonly float[] m_ExtremeTime = new float[kMaxSeats * kHandCount];
    private readonly float[] m_CooldownUntil = new float[kMaxSeats];
    private readonly float[] m_ReadyReleaseLockUntil = new float[kMaxSeats];

    public static PrepareRoleWaveDetector Ensure()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var gameObject = new GameObject("PrepareRoleWaveDetector");
        DontDestroyOnLoad(gameObject);
        return gameObject.AddComponent<PrepareRoleWaveDetector>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!Active)
        {
            return;
        }

        var adapter = GameProductUiAdapter.Instance;
        var snapshot = adapter != null ? adapter.UiSnapshot : null;
        if (snapshot == null)
        {
            return;
        }

        var slots = snapshot.Slots;
        for (int index = 0; index < slots.Count; index++)
        {
            var slot = slots[index];
            int seatId = slot.VisualSlotIndex;
            if (seatId < 0 || seatId >= kMaxSeats)
            {
                continue;
            }

            if (!slot.HasObservation || slot.PersonId < 0)
            {
                ResetSeat(seatId);
                continue;
            }

            var result = Player.Get(slot.PersonId);
            if (!result.Success || result.Value == null)
            {
                ResetSeat(seatId);
                continue;
            }

            Evaluate(seatId, result.Value);
        }
    }

    private void Evaluate(int seatId, TrackedPlayer player)
    {
        if (!TryReadMetrics(player, out float shoulderWidth, out float headTopY, out float hipY))
        {
            ResetSeat(seatId);
            return;
        }

        // 举手准备姿势期间不做换角色判定；结束后再锁 0.45s，
        // 避免“放下手”时手臂斜着落下越过中线被当成挥手。
        if (IsReadyPose(player, shoulderWidth, headTopY))
        {
            m_ReadyReleaseLockUntil[seatId] = Time.unscaledTime + kWaveCooldownSeconds;
            ResetSeat(seatId);
            return;
        }

        if (!TryReadMidlineX(player, out float midlineX))
        {
            ResetSeat(seatId);
            return;
        }

        float margin = shoulderWidth * kMidlineMarginRatio;
        float minSwingSpeed = shoulderWidth * kMinSwingSpeedRatioPerSecond;
        EvaluateHand(seatId, 0, player, PlayerJoint.LeftWrist,
            midlineX, margin, minSwingSpeed, shoulderWidth, headTopY, hipY);
        EvaluateHand(seatId, 1, player, PlayerJoint.RightWrist,
            midlineX, margin, minSwingSpeed, shoulderWidth, headTopY, hipY);
    }

    private void EvaluateHand(
        int seatId,
        int hand,
        TrackedPlayer player,
        PlayerJoint joint,
        float midlineX,
        float margin,
        float minSwingSpeed,
        float shoulderWidth,
        float headTopY,
        float hipY)
    {
        int index = seatId * kHandCount + hand;
        if (!TryReadWrist(player, joint, out float wristX, out float wristY, out float confidence))
        {
            ResetHand(index);
            return;
        }

        // 手腕必须位于头部以下、髋部以上，抬手/放手过程不参与判定。
        if (wristY < headTopY - shoulderWidth * kOverheadMarginRatio || wristY > hipY)
        {
            ResetHand(index);
            return;
        }

        float now = Time.unscaledTime;
        int currentSide = m_Side[index];
        int newSide = currentSide;
        if (wristX < midlineX - margin)
        {
            newSide = -1;
        }
        else if (wristX > midlineX + margin)
        {
            newSide = 1;
        }

        // 首次识别所在侧，或仍停留在中线死区内：只更新状态。
        if (currentSide == 0 || newSide == 0)
        {
            if (currentSide == 0 && newSide != 0)
            {
                m_Side[index] = newSide;
                m_ExtremeX[index] = wristX;
                m_ExtremeTime[index] = now;
            }

            return;
        }

        // 仍在同一侧：记录这一侧最外侧的位置（最快摆动的起点）。
        if (newSide == currentSide)
        {
            if (Mathf.Abs(wristX - midlineX) > Mathf.Abs(m_ExtremeX[index] - midlineX))
            {
                m_ExtremeX[index] = wristX;
                m_ExtremeTime[index] = now;
            }

            return;
        }

        // 越过中线：用“最外侧点 -> 越线点”的平均速度判断是否为快速挥手。
        float swingTime = Mathf.Max(0.0001f, now - m_ExtremeTime[index]);
        float swingDistance = Mathf.Abs(wristX - m_ExtremeX[index]);
        float swingSpeed = swingDistance / swingTime;
        bool fastSwing = swingTime <= kMaxSwingSeconds && swingSpeed >= minSwingSpeed;
        bool cooldownReady = now >= m_CooldownUntil[seatId];
        bool notRecentlyReady = now >= m_ReadyReleaseLockUntil[seatId];
        if (fastSwing && cooldownReady && notRecentlyReady)
        {
            m_CooldownUntil[seatId] = now + kWaveCooldownSeconds;
            GameSdkLog.Info(kTag + " seat=" + seatId + " person=" + player.PersonId +
                            " direction=" + (newSide < 0 ? "left" : "right") +
                            " swingSpeed=" + (swingSpeed / Mathf.Max(shoulderWidth, kShoulderWidthEpsilon)).ToString("F2") +
                            " swingTime=" + swingTime.ToString("F2"));
            RoleSwitchRequested?.Invoke(seatId, newSide);
        }

        m_Side[index] = newSide;
        m_ExtremeX[index] = wristX;
        m_ExtremeTime[index] = now;
    }

    private static bool TryReadMidlineX(TrackedPlayer player, out float midlineX)
    {
        if (TryReadJoint(player, PlayerJoint.Chest, out var chest))
        {
            midlineX = chest.Position.x;
            return true;
        }

        if (TryReadJoint(player, PlayerJoint.LeftShoulder, out var leftShoulder) &&
            TryReadJoint(player, PlayerJoint.RightShoulder, out var rightShoulder))
        {
            midlineX = (leftShoulder.Position.x + rightShoulder.Position.x) * 0.5f;
            return true;
        }

        if (TryReadJoint(player, PlayerJoint.Nose, out var nose))
        {
            midlineX = nose.Position.x;
            return true;
        }

        midlineX = 0f;
        return false;
    }

    private static bool TryReadMetrics(
        TrackedPlayer player,
        out float shoulderWidth,
        out float headTopY,
        out float hipY)
    {
        shoulderWidth = 0f;
        headTopY = 0f;
        hipY = 0f;
        if (!TryReadJoint(player, PlayerJoint.LeftShoulder, out var leftShoulder) ||
            !TryReadJoint(player, PlayerJoint.RightShoulder, out var rightShoulder))
        {
            return false;
        }

        shoulderWidth = Mathf.Abs(rightShoulder.Position.x - leftShoulder.Position.x) + kShoulderWidthEpsilon;

        bool hasHeadTop = false;
        if (TryReadJoint(player, PlayerJoint.Nose, out var nose))
        {
            headTopY = nose.Position.y;
            hasHeadTop = true;
        }

        if (TryReadJoint(player, PlayerJoint.LeftEye, out var leftEye))
        {
            headTopY = hasHeadTop ? Mathf.Min(headTopY, leftEye.Position.y) : leftEye.Position.y;
            hasHeadTop = true;
        }

        if (TryReadJoint(player, PlayerJoint.RightEye, out var rightEye))
        {
            headTopY = hasHeadTop ? Mathf.Min(headTopY, rightEye.Position.y) : rightEye.Position.y;
            hasHeadTop = true;
        }

        // 髋部可用时用真实位置，否则按肩宽估算躯干下界。
        hipY = headTopY + shoulderWidth * 1.6f;
        if (TryReadJoint(player, PlayerJoint.LeftHip, out var leftHip) &&
            TryReadJoint(player, PlayerJoint.RightHip, out var rightHip))
        {
            hipY = (leftHip.Position.y + rightHip.Position.y) * 0.5f;
        }

        return hasHeadTop;
    }

    private static bool IsReadyPose(TrackedPlayer player, float shoulderWidth, float headTopY)
    {
        float threshold = headTopY - shoulderWidth * kOverheadMarginRatio;
        bool leftOverhead = TryReadJoint(player, PlayerJoint.LeftWrist, out var leftWrist) &&
                            leftWrist.Position.y <= threshold;
        bool rightOverhead = TryReadJoint(player, PlayerJoint.RightWrist, out var rightWrist) &&
                             rightWrist.Position.y <= threshold;
        return leftOverhead || rightOverhead;
    }

    private static bool TryReadWrist(
        TrackedPlayer player,
        PlayerJoint joint,
        out float x,
        out float y,
        out float confidence)
    {
        if (player.TryGetJoint(joint, out var sample) && sample.Confidence >= kMinConfidence)
        {
            x = sample.Position.x;
            y = sample.Position.y;
            confidence = sample.Confidence;
            return true;
        }

        x = 0f;
        y = 0f;
        confidence = 0f;
        return false;
    }

    private static bool TryReadJoint(TrackedPlayer player, PlayerJoint joint, out PlayerJointSample sample)
    {
        if (player.TryGetJoint(joint, out sample) && sample.Confidence >= kMinConfidence)
        {
            return true;
        }

        sample = default;
        return false;
    }

    private void ResetSeat(int seatId)
    {
        if (seatId < 0 || seatId >= kMaxSeats)
        {
            return;
        }

        ResetHand(seatId * kHandCount);
        ResetHand(seatId * kHandCount + 1);
        m_CooldownUntil[seatId] = 0f;
    }

    private void ResetHand(int index)
    {
        if (index < 0 || index >= m_Side.Length)
        {
            return;
        }

        m_Side[index] = 0;
        m_ExtremeX[index] = 0f;
        m_ExtremeTime[index] = 0f;
    }
}
