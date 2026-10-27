#if UNITY_EDITOR
using UnityEngine;
using YoudooGame.Core;
using YoudooGame.MatchFlow.Runtime;
using YoudooGame.Platform;
using YoudooGame.PlayerAvatar;

internal sealed class GameEditorSyntheticMatchSessionRuntime :
    IMatchSessionRuntime,
    IMatchSessionAvatarRoleReservationRuntime,
    IMatchSessionGuestAvatarRuntime,
    IMatchSessionAvatarReacquireRuntime,
    IMatchSessionAvatarReacquireApprovalRuntime,
    IMatchSessionAvatarReacquireInvalidationRuntime,
    IMatchSessionAvatarMigrationRuntime,
    IMatchSessionFaceRecognitionRuntime,
    IMatchSessionFaceRecognitionReadinessRuntime,
    IMatchSessionAvatarSelectionRuntime
{
    private readonly PlatformMatchSessionRuntime m_PlatformRuntime = new PlatformMatchSessionRuntime();
    private int m_PlayerCount;
    private long m_FrameId;

    public IPlayerAvatarSnapshotProvider AvatarSnapshotProvider => m_PlatformRuntime.AvatarSnapshotProvider;

    public IPlayerAvatarManualSelectionController AvatarSelectionController =>
        m_PlatformRuntime.AvatarSelectionController;

    public bool IsFaceRecognitionReady => true;

    public GameEditorSyntheticMatchSessionRuntime(int playerCount)
    {
        SetPlayerCount(playerCount);
    }

    public void SetPlayerCount(int playerCount)
    {
        m_PlayerCount = Mathf.Clamp(playerCount, 1, 4);
    }

    public Result<PlayerFrame> GetPlayerFrame()
    {
        double timestamp = Time.realtimeSinceStartupAsDouble;
        var players = new TrackedPlayer[m_PlayerCount];
        for (int slotIndex = 0; slotIndex < players.Length; slotIndex++)
        {
            float centerX = ReadSlotCenter(m_PlayerCount, slotIndex);
            float sway = Mathf.Sin((float)timestamp * 0.7f + slotIndex) * 0.003f;
            players[slotIndex] = CreatePlayer(slotIndex, centerX + sway);
        }

        var frame = new PlayerFrame(
            ++m_FrameId,
            timestamp,
            "tooth-editor-manual-prepare",
            players,
            TrackingFrameMetadata.CanonicalTopLeft());
        return Result<PlayerFrame>.Ok(frame);
    }

    public PlatformNotificationBatch GetNotificationsSince(long sequence)
        => m_PlatformRuntime.GetNotificationsSince(sequence);

    public long GetCurrentNotificationSequence()
        => m_PlatformRuntime.GetCurrentNotificationSequence();

    public Result<bool> ConfigureAvatar(int slotCount)
        => m_PlatformRuntime.ConfigureAvatar(slotCount);

    public Result<bool> ResetAvatar()
        => m_PlatformRuntime.ResetAvatar();

    public Result<bool> SetAvatarSlotPerson(int slotIndex, int personId)
        => m_PlatformRuntime.SetAvatarSlotPerson(slotIndex, personId);

    public Result<bool> ClearAvatarSlot(int slotIndex)
        => m_PlatformRuntime.ClearAvatarSlot(slotIndex);

    public Result<bool> StartFaceRecognition()
        => Result<bool>.Ok(true);

    public long GetAvatarUserId(int slotIndex)
        => m_PlatformRuntime.GetAvatarUserId(slotIndex);

    public Result<bool> SetAvatarSlotRoleReservation(int slotIndex, long userId)
        => m_PlatformRuntime.SetAvatarSlotRoleReservation(slotIndex, userId);

    public Result<bool> SetAvatarSlotGuest(int slotIndex, int personId)
        => m_PlatformRuntime.SetAvatarSlotGuest(slotIndex, personId);

    public Result<bool> RetainAvatarSlotForReacquire(int slotIndex)
        => m_PlatformRuntime.RetainAvatarSlotForReacquire(slotIndex);

    public Result<bool> PrepareAvatarSlotReacquire(int slotIndex)
        => m_PlatformRuntime.PrepareAvatarSlotReacquire(slotIndex);

    public Result<bool> DiscardAvatarSlotReacquire(int slotIndex)
        => m_PlatformRuntime.DiscardAvatarSlotReacquire(slotIndex);

    public Result<bool> TransferAvatarSlotBinding(int sourceSlotIndex, int targetSlotIndex, int personId)
        => m_PlatformRuntime.TransferAvatarSlotBinding(sourceSlotIndex, targetSlotIndex, personId);

    public Result<bool> ApplyFaceRecognition(int slotIndex, int personId, FaceRecognitionInfo faceRecognition)
        => m_PlatformRuntime.ApplyFaceRecognition(slotIndex, personId, faceRecognition);

    private static float ReadSlotCenter(int playerCount, int slotIndex)
    {
        switch (playerCount)
        {
            case 1:
                return 0.5f;
            case 2:
                return slotIndex == 0 ? 0.3875f : 0.6125f;
            case 3:
                return slotIndex == 0 ? 0.3125f : slotIndex == 1 ? 0.5f : 0.6875f;
            default:
                switch (slotIndex)
                {
                    case 0: return 0.2625f;
                    case 1: return 0.4125f;
                    case 2: return 0.5875f;
                    default: return 0.7375f;
                }
        }
    }

    private static TrackedPlayer CreatePlayer(int slotIndex, float centerX)
    {
        var joints = new PlayerJointSample[(int)PlayerJoint.Count];
        SetJoint(joints, PlayerJoint.Nose, centerX, 0.17f);
        SetJoint(joints, PlayerJoint.LeftEye, centerX - 0.012f, 0.15f);
        SetJoint(joints, PlayerJoint.RightEye, centerX + 0.012f, 0.15f);
        SetJoint(joints, PlayerJoint.LeftEar, centerX - 0.025f, 0.17f);
        SetJoint(joints, PlayerJoint.RightEar, centerX + 0.025f, 0.17f);
        SetJoint(joints, PlayerJoint.LeftShoulder, centerX - 0.04f, 0.31f);
        SetJoint(joints, PlayerJoint.RightShoulder, centerX + 0.04f, 0.31f);
        SetJoint(joints, PlayerJoint.LeftElbow, centerX - 0.065f, 0.17f);
        SetJoint(joints, PlayerJoint.RightElbow, centerX + 0.065f, 0.17f);
        SetJoint(joints, PlayerJoint.LeftWrist, centerX - 0.07f, 0.025f);
        SetJoint(joints, PlayerJoint.RightWrist, centerX + 0.07f, 0.025f);
        SetJoint(joints, PlayerJoint.LeftHip, centerX - 0.025f, 0.57f);
        SetJoint(joints, PlayerJoint.RightHip, centerX + 0.025f, 0.57f);
        SetJoint(joints, PlayerJoint.LeftKnee, centerX - 0.03f, 0.74f);
        SetJoint(joints, PlayerJoint.RightKnee, centerX + 0.03f, 0.74f);
        SetJoint(joints, PlayerJoint.LeftAnkle, centerX - 0.035f, 0.93f);
        SetJoint(joints, PlayerJoint.RightAnkle, centerX + 0.035f, 0.93f);
        SetJoint(joints, PlayerJoint.Chest, centerX, 0.40f);

        float halfWidth = ReadPlayerHalfWidth();
        var bounds = new PlayerBounds(
            Mathf.Clamp01(centerX - halfWidth),
            0.04f,
            Mathf.Clamp01(centerX + halfWidth),
            0.97f);
        return new TrackedPlayer(1000 + slotIndex, bounds, 0.98f, joints);
    }

    private static float ReadPlayerHalfWidth()
    {
        return 0.055f;
    }

    private static void SetJoint(PlayerJointSample[] joints, PlayerJoint joint, float x, float y)
    {
        joints[(int)joint] = new PlayerJointSample(
            joint,
            new Vector3(x, y, 0f),
            0.98f,
            true);
    }
}
#endif
