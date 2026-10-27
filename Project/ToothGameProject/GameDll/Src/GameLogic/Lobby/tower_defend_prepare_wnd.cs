using MonoBean;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityUI;
using GameDll;
using YoudooGame.CameraImage;
using YoudooGame.Core;
using YoudooGame.Framework;
using YoudooGame.MatchFlow.Runtime;
using YoudooGame.PlayerAvatar;

namespace GameHot
{
    internal sealed class tower_defend_prepare_face_controller
    {
        private const int m_MaxPlayerCount = 4;
        private const int m_InvalidPersonId = -1;
        private const string m_LanFaceEnterArea = "td_prepare_face_enter_area";
        private const string m_LanFaceRecognizing = "td_prepare_face_recognizing";
        private const string m_LanFaceWaitAvatarSelect = "td_prepare_face_wait_avatar_select";
        private const string m_LanFaceSelectAvatar = "td_prepare_face_select_avatar";

        private sealed class SeatViewState
        {
            public LUIRawImage m_HeadImage;
            public LUITextMesh m_HeadInfo;
            public Coroutine m_LoadCoroutine;
            public UnityWebRequest m_LoadRequest;
            public Texture m_PendingTexture;
            public Texture m_RuntimeTexture;
            public int m_LoadVersion;
            public int m_PersonId = m_InvalidPersonId;
            public long m_UserId;
            public string m_PendingImagePath;
            public string m_LastLogKey;
        }

        private readonly SeatViewState[] m_SeatStates = new SeatViewState[m_MaxPlayerCount];
        private readonly LUIRawImage m_NoPlayerTemplate;
        private readonly LUIRawImage m_NotEqualPlayerTemplate;
        private readonly LUIRawImage m_SystemPlayerTemplate;
        private GameProductUiAdapter m_FrameworkAdapter;
        private bool m_UseFramework;
        private bool m_IsOpen;
        private int m_PlayerCount;
        private int m_ManualSelectingSeatId = -1;

        public tower_defend_prepare_face_controller(
            IList<LUIButton> headButtons,
            IList<LUITextMesh> headInfos,
            LUIRawImage noPlayerTemplate,
            LUIRawImage notEqualPlayerTemplate,
            LUIRawImage systemPlayerTemplate)
        {
            m_NoPlayerTemplate = noPlayerTemplate;
            m_NotEqualPlayerTemplate = notEqualPlayerTemplate;
            m_SystemPlayerTemplate = systemPlayerTemplate;
            for (int seatId = 0; seatId < m_SeatStates.Length; seatId++)
            {
                var headButton = headButtons != null && seatId < headButtons.Count
                    ? headButtons[seatId]
                    : null;
                var headImage = headButton != null ? headButton.targetGraphic as LUIRawImage : null;
                if (headImage == null && headButton != null)
                {
                    headImage = headButton.GetComponent<LUIRawImage>();
                }
                if (headImage != null)
                {
                    headImage.raycastTarget = true;
                }

                m_SeatStates[seatId] = new SeatViewState
                {
                    m_HeadImage = headImage,
                    m_HeadInfo = headInfos != null && seatId < headInfos.Count ? headInfos[seatId] : null
                };
            }
        }

        // 框架会话模式：数据源换成 GameProductUiAdapter 的 UI 快照与 PlayerAvatar。
        public void Open(GameProductUiAdapter adapter, int playerCount)
        {
            Close();
            m_IsOpen = true;
            m_UseFramework = true;
            m_FrameworkAdapter = adapter;
            Refresh(playerCount);
        }

        public void Close()
        {
            CancelFrameworkAvatarSelection();
            m_UseFramework = false;
            m_FrameworkAdapter = null;
            ClearManualSelection();
            for (int seatId = 0; seatId < m_SeatStates.Length; seatId++)
            {
                ResetSeatView(seatId, false);
            }

            m_PlayerCount = 0;
            m_IsOpen = false;
        }

        public void Refresh(int playerCount)
        {
            m_PlayerCount = Mathf.Clamp(playerCount, 0, m_MaxPlayerCount);
            for (int seatId = 0; seatId < m_SeatStates.Length; seatId++)
            {
                ApplySeatState(seatId);
            }
        }

        public void OnClickHead(int seatId)
        {
            if (!m_IsOpen || !IsActiveSeat(seatId))
            {
                return;
            }

            OnClickHeadFramework(seatId);
        }

        // 框架会话模式：头像选角完全跟随 MatchFlow 的状态机，界面不保存自己的选角标记。
        // 未选角时点击=进入该座位的槽位选择；已在选角时点击同一座位=确认并打开平台选角界面，
        // 点击别的座位=把焦点移到那个座位；平台选角界面打开后由平台持有输入，界面不再重复触发。
        private void OnClickHeadFramework(int seatId)
        {
            if (m_FrameworkAdapter == null)
            {
                return;
            }

            var slot = TryGetFrameworkSlot(seatId);
            if (!slot.HasValue)
            {
                LogFrameworkAvatarSelectionSkipped(seatId, "no session slot");
                return;
            }

            var selection = m_FrameworkAdapter.AvatarSelectionSnapshot;
            if (selection == null)
            {
                LogFrameworkAvatarSelectionSkipped(seatId, "selection snapshot unavailable");
                return;
            }

            if (selection.ManualState != PlayerAvatarManualState.Inactive &&
                selection.ManualState != PlayerAvatarManualState.SelectingSlot)
            {
                GameSdkLog.Info("[新SDK整合] prepare avatar click ignored seat=" + seatId +
                          " platform owns input, state=" + selection.ManualState);
                return;
            }

            // 能不能选角只由 MatchFlow 判定（只有已占用槽位可选），座位显示状态不参与：
            // TemporarilyLost/Reacquiring 恰恰是需要重新选角的场景。
            if (!selection.IsSelectable(slot.Value.SlotIndex))
            {
                LogFrameworkAvatarSelectionSkipped(seatId,
                    "slot " + slot.Value.SlotIndex + " is not occupied, state=" + slot.Value.State);
                return;
            }

            if (selection.ManualState == PlayerAvatarManualState.Inactive)
            {
                LogFrameworkAvatarSelectionResult(
                    "enter seat=" + seatId + " slot=" + slot.Value.SlotIndex,
                    m_FrameworkAdapter.BeginAvatarSlotSelection(slot.Value.SlotIndex));
            }
            else if (selection.SelectedSlotIndex == slot.Value.SlotIndex)
            {
                LogFrameworkAvatarSelectionResult(
                    "confirm slot=" + selection.SelectedSlotIndex,
                    m_FrameworkAdapter.HandleAvatarSelectionCommand(
                        MatchAvatarSelectionCommand.Confirm));
            }
            else
            {
                MoveFrameworkAvatarSelectionTo(slot.Value.SlotIndex);
            }

            Refresh(m_PlayerCount);
        }

        public void HandleFrameworkSubmit()
        {
            var selection = m_FrameworkAdapter != null ? m_FrameworkAdapter.AvatarSelectionSnapshot : null;
            if (selection == null || selection.ManualState != PlayerAvatarManualState.SelectingSlot)
            {
                return;
            }

            LogFrameworkAvatarSelectionResult(
                "confirm by submit",
                m_FrameworkAdapter.HandleAvatarSelectionCommand(MatchAvatarSelectionCommand.Confirm));
            Refresh(m_PlayerCount);
        }

        public void HandleFrameworkCancel()
        {
            var selection = m_FrameworkAdapter != null ? m_FrameworkAdapter.AvatarSelectionSnapshot : null;
            if (selection == null || selection.ManualState != PlayerAvatarManualState.SelectingSlot)
            {
                return;
            }

            LogFrameworkAvatarSelectionResult(
                "cancel",
                m_FrameworkAdapter.HandleAvatarSelectionCommand(MatchAvatarSelectionCommand.Back));
            Refresh(m_PlayerCount);
        }

        // 框架会话模式：选角期间方向键交给 MatchFlow（左右切槽位、下取消、上进入），
        // 未在选角时不消费，其余遥控器导航仍交回现有 UI 框架，避免双输入。
        public bool HandleFrameworkNavigate(Vector2 direction)
        {
            var selection = m_FrameworkAdapter != null ? m_FrameworkAdapter.AvatarSelectionSnapshot : null;
            if (!m_UseFramework || selection == null || !selection.IsActive)
            {
                return false;
            }

            LogFrameworkAvatarSelectionResult(
                "navigate " + direction,
                m_FrameworkAdapter.HandleAvatarSelectionNavigation(direction));
            Refresh(m_PlayerCount);
            return true;
        }

        public bool IsFrameworkManualSelecting
        {
            get
            {
                if (!m_UseFramework || m_FrameworkAdapter == null)
                {
                    return false;
                }

                var selection = m_FrameworkAdapter.AvatarSelectionSnapshot;
                return selection != null && selection.IsActive;
            }
        }

        private void CancelFrameworkAvatarSelection()
        {
            var selection = m_FrameworkAdapter != null ? m_FrameworkAdapter.AvatarSelectionSnapshot : null;
            if (selection == null || selection.ManualState != PlayerAvatarManualState.SelectingSlot)
            {
                return;
            }

            m_FrameworkAdapter.HandleAvatarSelectionCommand(MatchAvatarSelectionCommand.Back);
        }

        // 把选角焦点槽位换算成界面座位号；没在选角或槽位不属于本局座位时返回 -1。
        private int ResolveFrameworkSelectingSeatId()
        {
            if (!m_UseFramework || m_FrameworkAdapter == null)
            {
                return m_InvalidPersonId;
            }

            var selection = m_FrameworkAdapter.AvatarSelectionSnapshot;
            if (selection == null || !selection.IsActive || selection.SelectedSlotIndex < 0)
            {
                return m_InvalidPersonId;
            }

            var snapshot = m_FrameworkAdapter.UiSnapshot;
            if (snapshot == null)
            {
                return m_InvalidPersonId;
            }

            for (int i = 0; i < snapshot.Slots.Count; i++)
            {
                if (snapshot.Slots[i].SlotIndex == selection.SelectedSlotIndex)
                {
                    return snapshot.Slots[i].VisualSlotIndex;
                }
            }

            return m_InvalidPersonId;
        }

        private void MoveFrameworkAvatarSelectionTo(int targetSlotIndex)
        {
            var selection = m_FrameworkAdapter != null ? m_FrameworkAdapter.AvatarSelectionSnapshot : null;
            if (selection == null || selection.SelectableSlotCount <= 1)
            {
                return;
            }

            int currentOrdinal = selection.SelectedSlotOrdinal;
            int targetOrdinal = ResolveSelectableOrdinal(selection, targetSlotIndex);
            if (currentOrdinal < 0 || targetOrdinal < 0)
            {
                LogFrameworkAvatarSelectionSkipped(-1, "target slot " + targetSlotIndex + " is not selectable");
                return;
            }

            MatchAvatarSelectionCommand command = targetOrdinal > currentOrdinal
                ? MatchAvatarSelectionCommand.Right
                : MatchAvatarSelectionCommand.Left;
            for (int step = 0; step < selection.SelectableSlotCount; step++)
            {
                selection = m_FrameworkAdapter.AvatarSelectionSnapshot;
                if (selection == null || selection.SelectedSlotIndex == targetSlotIndex)
                {
                    return;
                }

                var result = m_FrameworkAdapter.HandleAvatarSelectionCommand(command);
                if (!result.Success || !result.Value)
                {
                    LogFrameworkAvatarSelectionResult("focus move to slot " + targetSlotIndex, result);
                    return;
                }
            }
        }

        private static int ResolveSelectableOrdinal(MatchAvatarSelectionSnapshot selection, int slotIndex)
        {
            var selectable = selection.SelectableSlotIndices;
            for (int i = 0; i < selectable.Count; i++)
            {
                if (selectable[i] == slotIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private void LogFrameworkAvatarSelectionResult(string action, Result<bool> result)
        {
            if (!result.Success)
            {
                Debug.LogWarning("[新SDK整合] prepare avatar " + action + " failed: " +
                          result.Error + " " + result.Message);
                return;
            }

            GameSdkLog.Info("[新SDK整合] prepare avatar " + action +
                      (result.Value ? " ok" : " rejected by flow"));
        }

        private void LogFrameworkAvatarSelectionSkipped(int seatId, string reason)
        {
            Debug.LogWarning("[新SDK整合] prepare avatar click ignored seat=" + seatId + " reason=" + reason);
        }

        private MatchSessionUiSlotSnapshot? TryGetFrameworkSlot(int seatId)
        {
            var snapshot = m_FrameworkAdapter != null ? m_FrameworkAdapter.UiSnapshot : null;
            if (snapshot == null)
            {
                return null;
            }

            MatchSessionUiSlotSnapshot slot;
            return snapshot.TryGetSlotForVisualIndex(seatId, out slot)
                ? slot
                : (MatchSessionUiSlotSnapshot?)null;
        }


        private void ApplySeatState(int seatId)
        {
            if (seatId < 0 || seatId >= m_SeatStates.Length)
            {
                return;
            }

            ApplyFrameworkSeatState(seatId);
        }

        // 框架会话模式的座位头像刷新：状态来自 MatchSessionUiSnapshot，
        // 头像地址来自 PlayerAvatar，纹理加载复用既有协程管线。
        private void ApplyFrameworkSeatState(int seatId)
        {
            if (!IsActiveSeat(seatId) || m_FrameworkAdapter == null)
            {
                ResetSeatView(seatId, true);
                return;
            }

            var slot = TryGetFrameworkSlot(seatId);
            if (!slot.HasValue)
            {
                ResetSeatView(seatId, true);
                return;
            }

            // 选角焦点所在座位：保留当前头像，只把提示切到"选择头像中"，
            // 结束与否由 MatchFlow 的状态机决定，界面不自己判定。
            if (seatId == ResolveFrameworkSelectingSeatId())
            {
                SetHeadInfo(seatId, m_LanFaceSelectAvatar);
                return;
            }

            switch (slot.Value.State)
            {
                case MatchSessionUiSlotState.Searching:
                    ResetSeatView(seatId, true);
                    return;
                case MatchSessionUiSlotState.FocusedCandidate:
                case MatchSessionUiSlotState.ConfirmingIdentity:
                case MatchSessionUiSlotState.Reacquiring:
                    ShowTemplate(seatId, m_NotEqualPlayerTemplate);
                    SetHeadInfo(seatId, m_LanFaceRecognizing);
                    return;
            }

            // WaitingForReadyAction / Bound / TemporarilyLost：头像同步成功就加载；
            // 未同步或游客时先显示系统占位并提示等待选头像。
            string imagePath = ResolveFrameworkSeatImagePath(slot.Value);
            var seat = m_SeatStates[seatId];
            string logKey = slot.Value.State + "|" + slot.Value.AvatarSyncState + "|" +
                            slot.Value.IsReady + "|" + imagePath;
            if (seat.m_LastLogKey != logKey)
            {
                seat.m_LastLogKey = logKey;
                GameSdkLog.Info("[新SDK整合] prepare seat=" + seatId +
                          " state=" + slot.Value.State +
                          " ready=" + slot.Value.IsReady +
                          " obs=" + slot.Value.HasObservation +
                          " person=" + slot.Value.PersonId +
                          " avatarSync=" + slot.Value.AvatarSyncState +
                          " image=" + (string.IsNullOrEmpty(imagePath) ? "none" : imagePath));
            }

            if (string.IsNullOrEmpty(imagePath))
            {
                ShowTemplate(seatId, m_SystemPlayerTemplate);
                SetHeadInfo(seatId, m_LanFaceWaitAvatarSelect);
                return;
            }

            StartFrameworkAvatarLoad(seatId, slot.Value.PersonId, imagePath);
        }

        private string ResolveFrameworkSeatImagePath(MatchSessionUiSlotSnapshot slot)
        {
            if (!slot.IsAvatarSynchronized)
            {
                return null;
            }

            var result = YoudooAPI.GetSessionPlayerAvatarSlot(slot.SlotIndex);
            if (!result.Success || result.Value == null)
            {
                return null;
            }

            var avatar = result.Value;
            if (!string.IsNullOrEmpty(avatar.PreferredImageUri))
            {
                return avatar.PreferredImageUri;
            }

            if (!string.IsNullOrEmpty(avatar.AvatarUri))
            {
                return avatar.AvatarUri;
            }

            return avatar.FacePhotoPath;
        }

        private void StartFrameworkAvatarLoad(int seatId, int personId, string imagePath)
        {
            var seat = m_SeatStates[seatId];
            if (seat.m_PersonId == personId && seat.m_PendingImagePath == imagePath &&
                (seat.m_RuntimeTexture != null || seat.m_LoadCoroutine != null))
            {
                SetHeadInfo(seatId, seat.m_RuntimeTexture != null ? null : m_LanFaceRecognizing);
                return;
            }

            CancelLoad(seat);
            seat.m_PersonId = personId;
            seat.m_UserId = 0;
            seat.m_PendingImagePath = imagePath;
            SetHeadInfo(seatId, m_LanFaceRecognizing);
            int loadVersion = seat.m_LoadVersion;
            seat.m_LoadCoroutine = RenderAPI.StartCoroutine(
                LoadFrameworkAvatar(seatId, personId, imagePath, loadVersion));
        }

        private IEnumerator LoadFrameworkAvatar(
            int seatId,
            int personId,
            string imagePath,
            int loadVersion)
        {
            var seat = m_SeatStates[seatId];
            yield return LoadTexture(seat, loadVersion, imagePath);
            if (seat.m_LoadVersion != loadVersion)
            {
                yield break;
            }

            Texture loadedTexture = TakePendingTexture(seat);
            if (!m_IsOpen || seat.m_LoadVersion != loadVersion || seat.m_PersonId != personId)
            {
                DestroyRuntimeTexture(loadedTexture);
                yield break;
            }

            seat.m_LoadCoroutine = null;
            if (loadedTexture == null)
            {
                ShowTemplate(seatId, m_SystemPlayerTemplate);
                SetHeadInfo(seatId, m_LanFaceWaitAvatarSelect);
                yield break;
            }

            SetRuntimeTexture(seat, loadedTexture);
            SetHeadInfo(seatId, null);
        }

        private IEnumerator LoadTexture(SeatViewState seat, int loadVersion, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                yield break;
            }

            byte[] imageBytes = null;
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
            {
                var request = UnityWebRequest.Get(path);
                if (seat.m_LoadVersion != loadVersion)
                {
                    request.Dispose();
                    yield break;
                }

                seat.m_LoadRequest = request;
                request.timeout = 10;
                yield return request.SendWebRequest();

                bool isCurrentLoad = seat.m_LoadVersion == loadVersion && seat.m_LoadRequest == request;
                if (seat.m_LoadRequest == request)
                {
                    seat.m_LoadRequest = null;
                }

                if (isCurrentLoad && request.result == UnityWebRequest.Result.Success)
                {
                    imageBytes = request.downloadHandler.data;
                }

                request.Dispose();
                if (!isCurrentLoad)
                {
                    yield break;
                }
            }
            else
            {
                string localPath = NormalizeLocalPath(path);
                yield return null;
                if (seat.m_LoadVersion != loadVersion)
                {
                    yield break;
                }

                SetPendingTexture(
                    seat,
                    loadVersion,
                    File.Exists(localPath) ? LCL.TextureManager.LoadTexture(localPath) : null);
                yield break;
            }

            SetPendingTexture(seat, loadVersion, LoadTextureFromBytes(imageBytes));
        }

        private void SetPendingTexture(SeatViewState seat, int loadVersion, Texture texture)
        {
            if (texture == null)
            {
                return;
            }

            if (seat.m_LoadVersion != loadVersion)
            {
                DestroyRuntimeTexture(texture);
                return;
            }

            ReleasePendingTexture(seat);
            seat.m_PendingTexture = texture;
        }

        private Texture TakePendingTexture(SeatViewState seat)
        {
            Texture texture = seat.m_PendingTexture;
            seat.m_PendingTexture = null;
            return texture;
        }

        private string NormalizeLocalPath(string path)
        {
            if (!path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            try
            {
                return new Uri(path).LocalPath;
            }
            catch
            {
                return Uri.UnescapeDataString(path.Substring("file://".Length));
            }
        }

        private Texture LoadTextureFromBytes(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return null;
            }

            string tempPath = Path.Combine(
                Application.temporaryCachePath,
                "prepare_face_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllBytes(tempPath, imageBytes);
                return LCL.TextureManager.LoadTexture(tempPath);
            }
            catch
            {
                return null;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                }
            }
        }

        private void ResetSeatView(int seatId, bool showPrompt)
        {
            if (seatId < 0 || seatId >= m_SeatStates.Length)
            {
                return;
            }

            if (m_ManualSelectingSeatId == seatId)
            {
                ClearManualSelection();
            }

            bool isWaitingForPlayer = showPrompt && IsActiveSeat(seatId);
            ShowTemplate(seatId, isWaitingForPlayer ? m_NotEqualPlayerTemplate : m_NoPlayerTemplate);
            var seat = m_SeatStates[seatId];
            seat.m_PersonId = m_InvalidPersonId;
            seat.m_UserId = 0;
            seat.m_LastLogKey = null;
            SetHeadInfo(seatId, isWaitingForPlayer ? m_LanFaceEnterArea : null);
        }

        private void ShowTemplate(int seatId, LUIRawImage template)
        {
            var seat = m_SeatStates[seatId];
            CancelLoad(seat);
            ReleaseRuntimeTexture(seat);
            if (seat.m_HeadImage == null)
            {
                return;
            }

            seat.m_HeadImage.texture = template != null ? template.texture : null;
            if (template != null)
            {
                seat.m_HeadImage.uvRect = template.uvRect;
                seat.m_HeadImage.color = template.color;
                seat.m_HeadImage.SetNativeSize();
            }
        }

        private void SetRuntimeTexture(SeatViewState seat, Texture texture)
        {
            ReleaseRuntimeTexture(seat);
            seat.m_RuntimeTexture = texture;
            if (seat.m_HeadImage != null)
            {
                seat.m_HeadImage.texture = texture;
                seat.m_HeadImage.uvRect = new Rect(0f, 0f, 1f, 1f);
                seat.m_HeadImage.color = Color.white;
            }
        }

        private void ReleaseRuntimeTexture(SeatViewState seat)
        {
            DestroyRuntimeTexture(seat.m_RuntimeTexture);
            seat.m_RuntimeTexture = null;
        }

        private void ReleasePendingTexture(SeatViewState seat)
        {
            DestroyRuntimeTexture(seat.m_PendingTexture);
            seat.m_PendingTexture = null;
        }

        private void DestroyRuntimeTexture(Texture texture)
        {
            if (texture != null && !texture.Equals(null))
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        private void CancelLoad(SeatViewState seat)
        {
            seat.m_LoadVersion++;
            if (seat.m_LoadRequest != null)
            {
                seat.m_LoadRequest.Abort();
                seat.m_LoadRequest.Dispose();
                seat.m_LoadRequest = null;
            }

            if (seat.m_LoadCoroutine != null)
            {
                RenderAPI.StopCoroutine(seat.m_LoadCoroutine);
                seat.m_LoadCoroutine = null;
            }

            // 协程可能已经创建纹理但尚未交给界面，关闭或切换时也必须回收这一阶段的资源。
            ReleasePendingTexture(seat);
        }

        private void SetHeadInfo(int seatId, string lanId)
        {
            var headInfo = m_SeatStates[seatId].m_HeadInfo;
            if (headInfo == null)
            {
                return;
            }

            if (seatId == m_ManualSelectingSeatId)
            {
                lanId = m_LanFaceSelectAvatar;
            }

            bool visible = !string.IsNullOrEmpty(lanId);
            RenderAPI.SetActive(headInfo, visible);
            if (visible)
            {
                RenderAPI.SetTextLan(headInfo, lanId);
            }
        }

        private void ClearManualSelection()
        {
            m_ManualSelectingSeatId = -1;
        }

        private bool IsActiveSeat(int seatId)
        {
            return seatId >= 0 && seatId < m_PlayerCount;
        }
    }

    public class tower_defend_prepare_model : WindowModel
    {
        public BattleStartupRequest m_Request;

        public override void Clear()
        {
            m_Request = null;
        }
    }

    public class tower_defend_prepare_wnd : WindowBase
    {
        private const string m_LanTdPrepareTitle = "td_prepare_title";
        private const string m_LanTdModeChapter = "td_mode_chapter";
        private const string m_LanTdModeEndless = "td_mode_endless";
        private const string m_LanTdPrepareStage = "td_prepare_stage";
        private const string m_LanTdPreparePlayerCount = "td_prepare_player_count";
        private const string m_LanTdPrepareBase = "td_prepare_base";
        private const string m_LanTdPreparePlayer = "td_prepare_player";
        private const string m_LanTdPrepareRole = "td_prepare_role";
        private const string m_LanTdPrepareStateReady = "td_prepare_state_ready";
        private const string m_LanTdPrepareStateNoReady = "td_prepare_state_no_ready";
        private const string m_LanTdPrepareRoleFallback = "td_prepare_role_fallback";
        private const string m_LanTdErrorBattleRequestEmpty = "td_error_battle_request_empty";
        private const string m_LanTdPrepareErrorSdkDataMissing = "td_prepare_error_sdk_data_missing";
        private const string m_LanTdPrepareErrorAllReady = "td_prepare_error_all_ready";
        private const string m_PrepareRoleIconAtlas = "texture_set/character.jpg";
        private const int m_MaxPreparePlayerCount = 4;
        private bool m_IsWaitingBattleLoading;
        private v_tower_defend_prepare_wnd m_View;
        private readonly ComponentBridge[] m_SlotBridges = new ComponentBridge[m_MaxPreparePlayerCount];
        private readonly v_tower_defend_prepare_wnd.v_PlayerSlot[] m_SlotViews =
            new v_tower_defend_prepare_wnd.v_PlayerSlot[m_MaxPreparePlayerCount];
        //可参与防守的角色
        private readonly List<long> m_RoleCandidates = new List<long>();
        private readonly long[] m_SelectedRoleCfgIdBySeat = new long[m_MaxPreparePlayerCount];
        private readonly string[] m_LastRoleVisualKeysBySeat = new string[m_MaxPreparePlayerCount];
        private int m_RoleCandidateStageId = -1;
        private int m_LastRemoteMenuPlayerCount = -1;
        private tower_defend_prepare_face_controller m_FaceController;
        private bool m_UseFrameworkSession;
        private bool m_HasSubscribedFrameworkUi;
        private int m_LastBoundFrameworkPlayerCount = -1;
        private string m_LastFrameworkSnapshotLogKey = string.Empty;
        private PrepareRoleWaveDetector m_WaveDetector;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.lobby;
            __ParticipateCurrentActiveWindow = true;
            __SelectDefaultActiveButton = false;
            __SetWindowCacheTime(GameProductUiAdapter.HasLiveSession ? 3600 : 0);
            __CreateModel(new tower_defend_prepare_model());
        }

        public void SetRequest(BattleStartupRequest request)
        {
            // 窗口已打开后如果重新塞入请求，需要立刻按新请求刷新界面。
            var model = GetModel<tower_defend_prepare_model>();
            model.m_Request = request;
            m_IsWaitingBattleLoading = false;
            m_RoleCandidateStageId = -1;
            m_LastRemoteMenuPlayerCount = -1;
            m_LastBoundFrameworkPlayerCount = -1;
            ResetPrepareRequestForNewEntry(request);
            EnsureRoleSelections(request);

            var windowStage = __GetWindowStage();
            if (IsInitializedView() &&
                (windowStage == WindowStage.Opening || windowStage == WindowStage.Opened))
            {
                // 快速返回时旧窗口会先进入 ReopenPending，下一帧再统一执行 OnOpen。
                // 该阶段只保存新请求，避免 SDK 和界面状态在复开前后重复初始化。
                RefreshView();
            }
        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_prepare_wnd();
            m_View.InitComponent(__GetWindowObj());
            InitSlotViews();
            InitFaceController();

            for (int i = 0; i < m_MaxPreparePlayerCount; i++)
            {
                int seatId = i;
                RenderAPI.AddButtonClick(GetSlotToggleButton(seatId), () => OnClickToggleSeat(seatId));
                RenderAPI.AddButtonClick(GetSlotRoleLeftButton(seatId), () => OnClickChangeSelectedRole(seatId, -1));
                RenderAPI.AddButtonClick(GetSlotRoleRightButton(seatId), () => OnClickChangeSelectedRole(seatId, 1));
                RenderAPI.AddButtonClick(GetSlotHeadButton(seatId), () => m_FaceController.OnClickHead(seatId));
            }
        }
        protected override void OnDestroy()
        {
            m_FaceController?.Close();
            UnsubscribeFrameworkUiEvents();
            base.OnDestroy();
        }

        protected override void OnOpen()
        {
            // 业务订阅和显示刷新统一放到打开阶段，不放在 OnInitComponent。
            m_IsWaitingBattleLoading = false;
            var request = GetModel<tower_defend_prepare_model>().m_Request;
            m_UseFrameworkSession = GameProductUiAdapter.HasLiveSession;
            if (!m_UseFrameworkSession)
            {
                Debug.LogError("[新SDK整合] prepare window opened without a live Framework session.");
            }

            // 相机在大厅入口统一恢复；窗口打开时只绑定界面视图。
            m_FaceController?.Open(
                GameProductUiAdapter.Instance,
                request != null ? request.ReadPreparePlayerCount() : 0);
            SubscribeFrameworkUiEvents();

            m_LastRemoteMenuPlayerCount = -1;
            m_LastBoundFrameworkPlayerCount = -1;
            m_LastFrameworkSnapshotLogKey = string.Empty;
            RefreshView();
            var openSnapshot = GameProductUiAdapter.Instance != null
                ? GameProductUiAdapter.Instance.UiSnapshot
                : null;
            // 人数一致性自检：请求人数（界面表现）与框架会话槽位数（实际数据）必须一致。
            Debug.Log("[新SDK整合] prepare open frameworkSession=" + m_UseFrameworkSession +
                      " requestPlayerCount=" + (request != null ? request.ReadPreparePlayerCount() : 0) +
                      " sessionSlots=" + (openSnapshot != null ? openSnapshot.Slots.Count : 0) +
                      " platform=" + Application.platform);
            LogFrameworkSnapshot("open");
        }

        protected override void OnClose()
        {
            Debug.Log("[返回] prepare OnClose begin frameworkSession=" + m_UseFrameworkSession +
                      " waitingBattle=" + m_IsWaitingBattleLoading);
            UnsubscribeFrameworkUiEvents();
            m_FaceController?.Close();

            base.OnClose();
            Debug.Log("[返回] prepare OnClose end");
        }

        // 准备界面自己完成确认链：先写回当前 UI 绑定，再同步真机已就位结果，
        // 点击确认时要求本次选择的人数都完成准备，最后走大厅正式开战入口。
        public void OnClickConfirm()
        {
            if (m_IsWaitingBattleLoading)
            {
                return;
            }

            var request = GetModel<tower_defend_prepare_model>().m_Request;
            if (request == null)
            {
                tip_wnd.GetInstance().OnShowTipLanId(m_LanTdErrorBattleRequestEmpty);
                return;
            }

            bool editorManualSeatSelection = Application.isEditor && m_UseFrameworkSession;
            if (!ShouldUseSdkManagedSeats() && !editorManualSeatSelection)
            {
                tip_wnd.GetInstance().OnShowTipLanId(m_LanTdPrepareErrorSdkDataMissing);
                return;
            }

            if (editorManualSeatSelection && request.GetPlayerCount() != request.ReadPreparePlayerCount())
            {
                tip_wnd.GetInstance().OnShowTipLanId(m_LanTdPrepareErrorAllReady);
                return;
            }

            OnClickConfirmFramework(request);
        }

        // 框架会话模式确认链：全部视觉座位 Bound 后，按快照回写正式请求，
        // 再走大厅开战入口，并通知新 SDK 进入 Gameplay 阶段。
        private void OnClickConfirmFramework(BattleStartupRequest request)
        {
            string error;
            int requiredPlayerCount = request.ReadPreparePlayerCount();
            var readiness = new StringBuilder(requiredPlayerCount);
            bool allReady = true;
            for (int seatId = 0; seatId < requiredPlayerCount; seatId++)
            {
                bool ready = IsSdkSeatReady(seatId);
                readiness.Append(ready ? '1' : '0');
                allReady &= ready;
            }

            GameSdkLog.Info("[新SDK整合] prepare confirm required=" + requiredPlayerCount +
                      " seatReady=" + readiness + " allReady=" + allReady);
            if (!allReady)
            {
                LogFrameworkSnapshot("confirm-blocked");
                tip_wnd.GetInstance().OnShowTipLanId(m_LanTdPrepareErrorAllReady);
                return;
            }

            RefreshSdkSeatBindings(request);
            if (!CGameProcedure.s_ProcLobby.TrySyncPrepareRequestFromFramework(request, out error))
            {
                Debug.LogWarning("[新SDK整合] prepare confirm sync failed: " + error);
                tip_wnd.GetInstance().OnShowTip(error);
                return;
            }

            if (request.GetPlayerCount() != requiredPlayerCount)
            {
                Debug.LogWarning("[新SDK整合] prepare confirm playerCount mismatch, request=" +
                                 request.GetPlayerCount() + " required=" + requiredPlayerCount);
                tip_wnd.GetInstance().OnShowTipLanId(m_LanTdPrepareErrorAllReady);
                return;
            }

            // 与旧链一致：不能在点击确认后立刻关闭准备界面，
            // 等 loading 完全覆盖后由大厅统一关闭。
            if (!CGameProcedure.s_ProcLobby.StartBattleFromRequest(request, out error))
            {
                Debug.LogWarning("[新SDK整合] prepare confirm StartBattleFromRequest failed: " + error);
                tip_wnd.GetInstance().OnShowTip(error);
                return;
            }

            // 注意：这里不能暂停框架相机。CameraImage.Pause 会清空当前帧，
            // 准备界面的实时画面会立刻变透明；等 loading 完全覆盖后再在
            // CGamePro_Lobby.BeginGoSceneAfterLoading 里暂停/停止。
            GameSdkLog.Info("[新SDK整合] prepare confirm passed, notify framework gameplay. players=" +
                      request.GetPlayerCount());
            if (!GameProductUiAdapter.Instance.StartGameplay())
            {
                Debug.LogError("[新SDK整合] Framework StartGameplay failed.");
            }

            m_IsWaitingBattleLoading = true;
        }

        // 仅编辑器允许手动切换座位；真机会在大厅逻辑里直接拦截。
        private void OnClickToggleSeat(int seatId)
        {
            if (m_IsWaitingBattleLoading)
            {
                return;
            }

            var model = GetModel<tower_defend_prepare_model>();
            var request = model.m_Request;
            if (request == null || seatId < 0)
            {
                return;
            }

            if (seatId >= request.ReadPreparePlayerCount())
            {
                return;
            }

            string error;
            if (!CGameProcedure.s_ProcLobby.TogglePrepareSeat(request, seatId, out error))
            {
                tip_wnd.GetInstance().OnShowTip(error);
                return;
            }

            RefreshView();
        }

        // 准备界面的统一刷新入口。
        // 这里只在打开窗口、手动点击和 SDK 事件时触发，不走 Update 轮询。
        // 真机准备阶段只刷新界面显示，不在这里反复重建正式战斗请求。
        private void RefreshView()
        {
            if (!IsInitializedView())
            {
                return;
            }

            m_UseFrameworkSession = GameProductUiAdapter.HasLiveSession;
            var request = GetModel<tower_defend_prepare_model>().m_Request;
            if (request == null)
            {
                m_FaceController?.Refresh(0);
                RefreshRemoteMenuButtons(null);
                return;
            }

            if (!m_UseFrameworkSession)
            {
                Debug.LogError("[新SDK整合] prepare refresh blocked because the Framework session is unavailable.");
                return;
            }

            EnsureRoleSelections(request);
            BindFrameworkCameraViews(request);

            m_FaceController?.Refresh(request.ReadPreparePlayerCount());
            RefreshSdkSeatBindings(request);
            RefreshPlayerSlots(request);
            RefreshRemoteMenuButtons(request);
        }

        private void RefreshRemoteMenuButtons(BattleStartupRequest request)
        {
            int playerCount = request != null
                ? Mathf.Clamp(request.ReadPreparePlayerCount(), 0, m_MaxPreparePlayerCount)
                : 0;
            if (m_LastRemoteMenuPlayerCount == playerCount)
            {
                return;
            }

            var activeButtonRows = new List<ActiveButtons>();
            var playerHeadButtons = new ActiveButtons();
            for (int seatId = 0; seatId < playerCount; seatId++)
            {
                var slotView = GetSlotView(seatId);
                if (slotView != null && slotView.m_head != null)
                {
                    playerHeadButtons.buttons.Add(slotView.m_head);
                }
            }

            if (playerHeadButtons.buttons.Count > 0)
            {
                activeButtonRows.Add(playerHeadButtons);
            }

            __SetActiveButtons(activeButtonRows);
            m_LastRemoteMenuPlayerCount = playerCount;
        }

        // 当前界面只按本局选择的人数显示座位；角色开放限制已经废弃。
        // 真机下座位是否有人由 SDK 准备结果决定，编辑器下保留手点座位调试。
        private void RefreshPlayerSlots(BattleStartupRequest request)
        {
            bool useSdkPrepareSelection = ShouldUseSdkManagedSeats();
            for (int i = 0; i < m_MaxPreparePlayerCount; i++)
            {
                var slotView = GetSlotView(i);
                if (slotView == null || slotView.m_Bridge == null)
                {
                    continue;
                }

                bool isVisible = ShouldShowSeat(request, i, useSdkPrepareSelection);
                RenderAPI.SetActive(slotView.m_Bridge, isVisible);

                var slotButton = GetSlotToggleButton(i);
                if (slotButton != null)
                {
                    slotButton.interactable = !useSdkPrepareSelection;
                }

                if (!isVisible)
                {
                    continue;
                }

                RefreshPlayerSlot(request, i, FindPlayerBySeat(request, i));
            }
        }

        // 刷新单个座位的名称、选中态和准备文案。
        private void RefreshPlayerSlot(BattleStartupRequest request, int seatId, BattleStartupPlayerData player)
        {
            var slotView = GetSlotView(seatId);
            if (slotView == null)
            {
                return;
            }

            bool isSelected = player != null;
            bool useSdkPrepareSelection = ShouldUseSdkManagedSeats();
            bool isReady = useSdkPrepareSelection ? IsSdkSeatReady(seatId) : isSelected;
            float prepareProgress = useSdkPrepareSelection
                ? GetFrameworkSeatPrepareProgress(seatId)
                : (isReady ? 1f : 0f);

            RenderAPI.SetActive(slotView.m_choose, !useSdkPrepareSelection && isSelected);

            long roleCfgId = EnsureSelectedRoleForSeat(request, seatId);
            var heroCfg = roleCfgId > 0 ? t_heroBean.GetConfig(roleCfgId, false) : null;
            var roleName = heroCfg != null && !string.IsNullOrEmpty(heroCfg.t_name)
                ? heroCfg.t_name
                : RenderAPI.GetTextByLanId(m_LanTdPrepareRoleFallback, seatId + 1);
            //RenderAPI.SetText(slotView.m_txt, roleName);
            RefreshPlayerRoleIcon(slotView, seatId);
            RefreshPlayerStateText(slotView.m_txt_prepare, slotView.m_prepare, slotView.m_Fill, isReady, prepareProgress);
        }

        private void RefreshPlayerRoleIcon(v_tower_defend_prepare_wnd.v_PlayerSlot slotView, int seatId)
        {
            if (slotView == null || slotView.m_Character == null || seatId < 0 || seatId >= m_SelectedRoleCfgIdBySeat.Length)
            {
                return;
            }

            var cfgId = m_SelectedRoleCfgIdBySeat[seatId];
            var cfg = t_heroBean.GetConfig(cfgId, false);
            if (cfg == null)
            {
                return;
            }

            // 三张图片均由角色配置决定。槽位使用同一模板后，同角色自然显示同一套表现。
            string iconName = cfg.t_head;
            string fillName = cfg.t_prepare_fill;
            string prepareName = cfg.t_prepare;
            string visualKey = string.Concat(iconName, "|", fillName, "|", prepareName);
            if (m_LastRoleVisualKeysBySeat[seatId] == visualKey &&
                slotView.m_Character.sprite != null &&
                (slotView.m_Fill == null || slotView.m_Fill.sprite != null) &&
                (slotView.m_prepare == null || slotView.m_prepare.sprite != null))
            {
                return;
            }

            m_LastRoleVisualKeysBySeat[seatId] = visualKey;
            __SetImage(slotView.m_Character, m_PrepareRoleIconAtlas, iconName, false, () =>
            {
                slotView.m_Character.SetNativeSize();
            });
            __SetImage(slotView.m_Fill, m_PrepareRoleIconAtlas, fillName.Substring(0, fillName.Length - 1) + seatId, false);
            __SetImage(slotView.m_prepare, m_PrepareRoleIconAtlas, prepareName.Substring(0, prepareName.Length - 1) + seatId, false);
        }

        private void RefreshPlayerStateText(LUITextMesh state, LUIImage prepare, LUIImage fill, bool isReady, float prepareProgress)
        {
            if (state == null || prepare == null)
            {
                return;
            }

            if (fill != null)
            {
                fill.fillAmount = isReady ? 1f : Mathf.Clamp01(prepareProgress);
            }

            var notPrepare = prepare.transform.Find("noPrepare");
            RenderAPI.SetActive(prepare, true);
            RenderAPI.SetActive(notPrepare, !isReady);
            RenderAPI.SetTextLan(state, isReady ? m_LanTdPrepareStateReady : m_LanTdPrepareStateNoReady);
        }

        // 准备界面自己掌握了节点绑定关系，这里把“战斗座位 -> SDK 槽位”写回请求。
        private void RefreshSdkSeatBindings(BattleStartupRequest request)
        {
            if (request == null)
            {
                return;
            }

            request.ClearSdkSlotBindings();
            var frameworkSnapshot = GameProductUiAdapter.Instance != null
                ? GameProductUiAdapter.Instance.UiSnapshot
                : null;
            if (frameworkSnapshot != null)
            {
                int frameworkPlayerCount = request.ReadPreparePlayerCount();
                for (int seatId = 0; seatId < frameworkPlayerCount; seatId++)
                {
                    MatchSessionUiSlotSnapshot slot;
                    if (frameworkSnapshot.TryGetSlotForVisualIndex(seatId, out slot))
                    {
                        request.SetSdkSlotIndexForSeat(seatId, slot.SlotIndex);
                    }
                }

            }
        }

        // 人数选择后，准备界面只显示本局真实参与匹配的槽位。
        private bool ShouldShowSeat(BattleStartupRequest request, int seatId, bool useSdkPrepareSelection)
        {
            return request != null && seatId >= 0 && seatId < request.ReadPreparePlayerCount();
        }

        private void EnsureRoleSelections(BattleStartupRequest request)
        {
            if (request == null)
            {
                return;
            }

            if (m_RoleCandidateStageId != request.m_StageId)
            {
                m_RoleCandidates.Clear();
                string error;
                List<long> roleCfgIds;
                CGameProcedure.s_ProcLobby.TryGetStageGuardRoleCfgIds(request.m_StageId, out roleCfgIds, out error);
                if (roleCfgIds != null)
                {
                    m_RoleCandidates.AddRange(roleCfgIds);
                }

                m_RoleCandidateStageId = request.m_StageId;
            }

            for (int seatId = 0; seatId < m_MaxPreparePlayerCount; seatId++)
            {
                long selectedRoleCfgId;
                if (request.TryGetSelectedRoleCfgIdForSeat(seatId, out selectedRoleCfgId))
                {
                    m_SelectedRoleCfgIdBySeat[seatId] = selectedRoleCfgId;
                    continue;
                }

                if (m_RoleCandidates.Count <= 0)
                {
                    request.SetSelectedRoleCfgIdForSeat(seatId, 0);
                    m_SelectedRoleCfgIdBySeat[seatId] = 0;
                    continue;
                }

                // 每次重新进入准备界面都按配置顺序给位置分配初始角色；候选不足时循环使用。
                // 当前界面内人员离开只刷新座位状态，不会重新执行这里，因此保留该位置已经选择的角色。
                var defaultCfgId = m_RoleCandidates[seatId % m_RoleCandidates.Count];
                m_SelectedRoleCfgIdBySeat[seatId] = defaultCfgId;
                request.SetSelectedRoleCfgIdForSeat(seatId, m_SelectedRoleCfgIdBySeat[seatId]);
            }
        }

        private long EnsureSelectedRoleForSeat(BattleStartupRequest request, int seatId)
        {
            EnsureRoleSelections(request);
            if (request == null)
            {
                return 0;
            }

            long selectedRoleCfgId;
            return request.TryGetSelectedRoleCfgIdForSeat(seatId, out selectedRoleCfgId) ? selectedRoleCfgId : 0;
        }
        private void ChangeSelectedRole(int seatId, int delta)
        {
            var request = GetModel<tower_defend_prepare_model>().m_Request;
            if (request == null || seatId < 0 || seatId >= request.ReadPreparePlayerCount())
            {
                return;
            }

            EnsureRoleSelections(request);
            if (m_RoleCandidates.Count <= 0)
            {
                return;
            }
            var curRoleCfgId = m_SelectedRoleCfgIdBySeat[seatId];
            var roleIndex = m_RoleCandidates.IndexOf(curRoleCfgId) + delta;
            if (roleIndex < 0)
            {
                roleIndex = m_RoleCandidates.Count - 1;
            }
            else if (roleIndex >= m_RoleCandidates.Count)
            {
                roleIndex = 0;
            }

            long roleCfgId = m_RoleCandidates[roleIndex];
            m_SelectedRoleCfgIdBySeat[seatId] = roleCfgId;
            request.SetSelectedRoleCfgIdForSeat(seatId, roleCfgId);

            var player = FindPlayerBySeat(request, seatId);
            if (player != null)
            {
                player.m_RoleCfgId = roleCfgId;
            }

            RefreshView();
        }

        private void OnClickChangeSelectedRole(int seatId, int delta)
        {
            if (m_IsWaitingBattleLoading)
            {
                return;
            }

            ChangeSelectedRole(seatId, delta);
        }

        private void ResetPrepareRequestForNewEntry(BattleStartupRequest request)
        {
            if (request == null)
            {
                return;
            }

            request.ClearSdkSlotBindings();
            request.ClearPersonIdBindings();
            for (int seatId = 0; seatId < m_MaxPreparePlayerCount; seatId++)
            {
                request.SetSelectedRoleCfgIdForSeat(seatId, 0);
            }

            if (request.m_Players != null)
            {
                request.m_Players.Clear();
            }
        }

        private bool ShouldUseSdkManagedSeats()
        {
            return m_UseFrameworkSession && !Application.isEditor;
        }

        private bool IsSdkSeatReady(int seatId)
        {
            var snapshot = GameProductUiAdapter.Instance != null
                ? GameProductUiAdapter.Instance.UiSnapshot
                : null;
            MatchSessionUiSlotSnapshot slot;
            return snapshot != null
                && snapshot.TryGetSlotForVisualIndex(seatId, out slot)
                && slot.IsReady;
        }

        private float GetFrameworkSeatPrepareProgress(int seatId)
        {
            var snapshot = GameProductUiAdapter.Instance != null
                ? GameProductUiAdapter.Instance.UiSnapshot
                : null;
            MatchSessionUiSlotSnapshot slot;
            if (snapshot == null || !snapshot.TryGetSlotForVisualIndex(seatId, out slot))
            {
                return 0f;
            }

            if (slot.ReadyFramesRequired > 0)
            {
                return Mathf.Clamp01((float)slot.ReadyFrameCount / slot.ReadyFramesRequired);
            }

            return 0f;
        }


        private BattleStartupPlayerData FindPlayerBySeat(BattleStartupRequest request, int seatId)
        {
            if (request == null || request.m_Players == null)
            {
                return null;
            }

            for (int i = 0; i < request.m_Players.Count; i++)
            {
                var player = request.m_Players[i];
                if (player != null && player.m_SeatId == seatId)
                {
                    return player;
                }
            }

            return null;
        }

        private ComponentBridge GetSlotBridge(int index)
        {
            if (index < 0 || index >= m_SlotBridges.Length)
            {
                return null;
            }

            return m_SlotBridges[index];
        }

        private void InitSlotViews()
        {
            var templateBridge = m_View.m_Player0;
            if (templateBridge == null)
            {
                return;
            }

            var parent = templateBridge.transform.parent;
            int templateSiblingIndex = templateBridge.transform.GetSiblingIndex();
            for (int i = 0; i < m_MaxPreparePlayerCount; i++)
            {
                ComponentBridge slotBridge;
                if (i == 0)
                {
                    slotBridge = templateBridge;
                }
                else
                {
                    // 只维护一份完整模板，其他座位复制同一套组件绑定，避免预制件长期维护后出现槽位差异。
                    var slotObject = GameObject.Instantiate(templateBridge.gameObject, parent, false);
                    slotObject.name = $"Player{i}";
                    slotObject.transform.SetSiblingIndex(templateSiblingIndex + i);
                    slotBridge = slotObject.GetComponent<ComponentBridge>();
                }

                m_SlotBridges[i] = slotBridge;
                if (slotBridge == null)
                {
                    m_SlotViews[i] = null;
                    continue;
                }

                var slotView = new v_tower_defend_prepare_wnd.v_PlayerSlot();
                slotView.InitComponent(slotBridge.gameObject);
                m_SlotViews[i] = slotView;
            }

        }

        private void InitFaceController()
        {
            var headButtons = new LUIButton[m_MaxPreparePlayerCount];
            var headInfos = new LUITextMesh[m_MaxPreparePlayerCount];
            for (int seatId = 0; seatId < m_MaxPreparePlayerCount; seatId++)
            {
                var slotView = GetSlotView(seatId);
                if (slotView == null)
                {
                    continue;
                }

                headButtons[seatId] = slotView.m_head;
                headInfos[seatId] = slotView.m_headInfo;
            }

            m_FaceController = new tower_defend_prepare_face_controller(
                headButtons,
                headInfos,
                m_View.m_NoPlayer,
                m_View.m_NotEqualPlayer,
                m_View.m_SystemPlayer);
        }

        private v_tower_defend_prepare_wnd.v_PlayerSlot GetSlotView(int index)
        {
            if (index < 0 || index >= m_SlotViews.Length)
            {
                return null;
            }

            return m_SlotViews[index];
        }

        // 把新 SDK 的 CameraImageView 归位到界面座位节点下（必须在窗口 Canvas 内才能随界面渲染）。
        private void BindFrameworkCameraViews(BattleStartupRequest request)
        {
            int playerCount = Mathf.Clamp(request.ReadPreparePlayerCount(), 0, m_MaxPreparePlayerCount);
            if (m_LastBoundFrameworkPlayerCount == playerCount)
            {
                return;
            }

            var splitViews = GameProductUiAdapter.SplitCameraViews;
            if (splitViews == null)
            {
                Debug.LogWarning("[新SDK整合] prepare bind camera views skipped: SplitCameraViews is null.");
                return;
            }

            if (GameProductUiAdapter.SplitCameraLayout != null)
            {
                // 视图交给界面座位承载后，模板布局不再负责排布，避免两套布局互相覆盖。
                GameProductUiAdapter.SplitCameraLayout.enabled = false;
            }

            // 相机由大厅逻辑层在打开准备窗前统一进入；这里只负责把视图挂到座位节点上。

            for (int seatId = 0; seatId < playerCount && seatId < splitViews.Length; seatId++)
            {
                var view = splitViews[seatId];
                var slotBridge = GetSlotBridge(seatId);
                if (view == null || slotBridge == null)
                {
                    Debug.LogWarning("[新SDK整合] prepare bind camera view skipped, seat=" + seatId +
                                     " view=" + (view != null) + " slot=" + (slotBridge != null));
                    continue;
                }

                // 老版相机画面挂在座位下的 mask 节点内（Mask 裁剪溢出），新视图保持同位。
                var mount = slotBridge.transform.Find("mask");
                if (mount == null)
                {
                    mount = slotBridge.transform;
                }

                if (!slotBridge.gameObject.activeSelf)
                {
                    slotBridge.gameObject.SetActive(true);
                }

                if (!mount.gameObject.activeSelf)
                {
                    mount.gameObject.SetActive(true);
                }

                var viewRect = view.transform as RectTransform;
                if (viewRect != null)
                {
                    viewRect.SetParent(mount, false);
                    viewRect.anchorMin = Vector2.zero;
                    viewRect.anchorMax = Vector2.one;
                    viewRect.offsetMin = Vector2.zero;
                    viewRect.offsetMax = Vector2.zero;
                    viewRect.localScale = Vector3.one;
                }

                var rawImage = view.GetComponent<RawImage>();
                if (rawImage != null)
                {
                    rawImage.color = Color.white;
                }

                view.gameObject.SetActive(true);
                GameSdkLog.Info("[新SDK整合] prepare bind camera view seat=" + seatId +
                          " view=" + view.name + " mount=" + mount.name +
                          " active=" + view.gameObject.activeInHierarchy);
            }

            // 人数不足 4 时剩余视图无人布局管理，隐藏防止漏出游离画面。
            for (int seatId = playerCount; seatId < splitViews.Length; seatId++)
            {
                if (splitViews[seatId] != null)
                {
                    splitViews[seatId].gameObject.SetActive(false);
                }
            }

            // 原生纹理绑定由 GameProductUiAdapter.EnterPrepareCamera 统一处理。

            var cameraStatus = YoudooGame.CameraImage.CameraImage.Status;
            GameSdkLog.Info("[新SDK整合] prepare camera views bound playerCount=" + playerCount +
                      " camera=" + cameraStatus.State + "/" + cameraStatus.GraphicsApi +
                      " error=" + cameraStatus.Error + " msg=" + cameraStatus.Message +
                      " views=" + BuildFrameworkViewSummary(splitViews, playerCount));
            m_LastBoundFrameworkPlayerCount = playerCount;
        }

        // 座位视图被重新挂到界面节点下，这里回读框架算出的分区 UV 与实际父节点，
        // 用于判断画面是否还在框架布局的槽位几何里，而不必连真机猜。
        private static string BuildFrameworkViewSummary(CameraImageView[] views, int playerCount)
        {
            if (views == null)
            {
                return "null";
            }

            var builder = new StringBuilder();
            for (int i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null)
                {
                    builder.Append(i).Append(":null ");
                    continue;
                }

                builder.Append(i)
                    .Append(":uv=").Append(view.CurrentUvRegion.x).Append(',').Append(view.CurrentUvRegion.y)
                    .Append(',').Append(view.CurrentUvRegion.width).Append(',').Append(view.CurrentUvRegion.height)
                    .Append(" partition=").Append(view.HasPartitionUvBounds)
                    .Append(" tex=").Append(view.HasPresentation)
                    .Append(" parent=").Append(view.transform.parent != null ? view.transform.parent.name : "none")
                    .Append(" active=").Append(view.gameObject.activeInHierarchy)
                    .Append(i < playerCount ? " (seat)" : " (idle)")
                    .Append(' ');
            }

            return builder.ToString();
        }

        // 框架会话模式：快照推送驱动刷新，不再订阅旧 Demo 事件。
        private void SubscribeFrameworkUiEvents()
        {
            if (m_HasSubscribedFrameworkUi)
            {
                return;
            }

            var adapter = GameProductUiAdapter.Instance;
            if (adapter == null)
            {
                Debug.LogWarning("[新SDK整合] prepare subscribe skipped: framework adapter is null.");
                return;
            }

            adapter.UiSnapshotChanged += OnFrameworkUiSnapshotChanged;
            adapter.AvatarSelectionChanged += OnFrameworkAvatarSelectionChanged;
            if (GameControllerInputBridge.Instance != null)
            {
                GameControllerInputBridge.Instance.NavigateReceived += OnFrameworkNavigate;
                GameControllerInputBridge.Instance.SubmitReceived += OnFrameworkSubmit;
                GameControllerInputBridge.Instance.CancelReceived += OnFrameworkCancel;
            }
            else
            {
                Debug.LogWarning("[新SDK整合] prepare navigate subscribe skipped: input bridge is null.");
            }

            m_HasSubscribedFrameworkUi = true;
            m_WaveDetector = PrepareRoleWaveDetector.Ensure();
            m_WaveDetector.RoleSwitchRequested += OnFrameworkWaveRoleSwitch;
            m_WaveDetector.Active = true;
            GameSdkLog.Info("[新SDK整合] prepare framework UI subscribed, inputBridge=" +
                      (GameControllerInputBridge.Instance != null) +
                      " waveDetector=" + (m_WaveDetector != null));
        }

        private void OnFrameworkNavigate(Vector2 direction)
        {
            if (m_FaceController != null)
            {
                m_FaceController.HandleFrameworkNavigate(direction);
            }
        }

        private void OnFrameworkSubmit()
        {
            m_FaceController?.HandleFrameworkSubmit();
        }

        private void OnFrameworkCancel()
        {
            m_FaceController?.HandleFrameworkCancel();
        }

        // 选角状态机一旦变化就重绘座位提示，界面不缓存自己的选角标记。
        private void OnFrameworkAvatarSelectionChanged(MatchAvatarSelectionSnapshot selection)
        {
            GameSdkLog.Info("[新SDK整合] prepare avatar selection state=" +
                      (selection != null ? selection.ManualState.ToString() : "null") +
                      " selectedSlot=" + (selection != null ? selection.SelectedSlotIndex : -1) +
                      " selectable=" + (selection != null ? selection.SelectableSlotCount : 0));
            RefreshView();
        }

        // 框架模式下左右挥手切换本座位的出战角色，等价旧 SDK 的
        // onPlayerSelectRoleLeft/Right -> ChangeSelectedRole。
        private void OnFrameworkWaveRoleSwitch(int seatId, int delta)
        {
            if (!m_UseFrameworkSession || m_IsWaitingBattleLoading)
            {
                return;
            }

            if (m_FaceController != null && m_FaceController.IsFrameworkManualSelecting)
            {
                return;
            }

            ChangeSelectedRole(seatId, delta);
        }

        private void UnsubscribeFrameworkUiEvents()
        {
            if (!m_HasSubscribedFrameworkUi)
            {
                return;
            }

            m_HasSubscribedFrameworkUi = false;
            var adapter = GameProductUiAdapter.Instance;
            if (adapter != null)
            {
                adapter.UiSnapshotChanged -= OnFrameworkUiSnapshotChanged;
                adapter.AvatarSelectionChanged -= OnFrameworkAvatarSelectionChanged;
            }

            if (GameControllerInputBridge.Instance != null)
            {
                GameControllerInputBridge.Instance.NavigateReceived -= OnFrameworkNavigate;
                GameControllerInputBridge.Instance.SubmitReceived -= OnFrameworkSubmit;
                GameControllerInputBridge.Instance.CancelReceived -= OnFrameworkCancel;
            }

            if (m_WaveDetector != null)
            {
                m_WaveDetector.RoleSwitchRequested -= OnFrameworkWaveRoleSwitch;
                m_WaveDetector.Active = false;
                m_WaveDetector = null;
            }
        }

        private void OnFrameworkUiSnapshotChanged()
        {
            LogFrameworkSnapshot("change");
            RefreshView();
        }

        private void LogFrameworkSnapshot(string stage)
        {
            var snapshot = GameProductUiAdapter.Instance != null
                ? GameProductUiAdapter.Instance.UiSnapshot
                : null;
            if (snapshot == null)
            {
                if (m_LastFrameworkSnapshotLogKey != "none")
                {
                    m_LastFrameworkSnapshotLogKey = "none";
                    GameSdkLog.Info("[新SDK整合] prepare " + stage + " snapshot=none");
                }

                return;
            }

            var key = new StringBuilder(96);
            var detail = new StringBuilder(320);
            key.Append(snapshot.SessionPhase);
            for (int i = 0; i < snapshot.Slots.Count; i++)
            {
                var slot = snapshot.Slots[i];
                key.Append('[').Append(slot.VisualSlotIndex)
                    .Append(':').Append(slot.State)
                    .Append(':').Append(slot.IsReady ? '1' : '0')
                    .Append(':').Append(slot.HasObservation ? '1' : '0')
                    .Append(':').Append(slot.AvatarSyncState)
                    .Append(']');
                detail.Append("[v").Append(slot.VisualSlotIndex)
                    .Append(" s").Append(slot.SlotIndex)
                    .Append(" p").Append(slot.PersonId)
                    .Append(' ').Append(slot.State)
                    .Append(" obs=").Append(slot.HasObservation ? '1' : '0')
                    .Append(" ready=").Append(slot.IsReady ? '1' : '0')
                    .Append(" prog=").Append(slot.Progress01.ToString("F2"))
                    .Append(" stable=").Append(slot.StableFrameCount).Append('/').Append(slot.StableFramesRequired)
                    .Append(" readyF=").Append(slot.ReadyFrameCount).Append('/').Append(slot.ReadyFramesRequired)
                    .Append(" missing=").Append(slot.MissingFrameCount)
                    .Append("] ");
            }

            string keyText = key.ToString();
            if (keyText == m_LastFrameworkSnapshotLogKey)
            {
                return;
            }

            m_LastFrameworkSnapshotLogKey = keyText;
            GameSdkLog.Info("[新SDK整合] prepare " + stage + " phase=" + snapshot.SessionPhase +
                      " slots=" + detail);
        }

        private LUIButton GetSlotToggleButton(int index)
        {
            var slotView = GetSlotView(index);
            return slotView != null && slotView.m_Bridge != null
                ? slotView.m_Bridge.GetComponent<LUIButton>()
                : null;
        }

        private LUIButton GetSlotRoleLeftButton(int index)
        {
            var slotView = GetSlotView(index);
            return slotView != null ? slotView.m_btnLeft : null;
        }

        private LUIButton GetSlotRoleRightButton(int index)
        {
            var slotView = GetSlotView(index);
            return slotView != null ? slotView.m_btnRight : null;
        }

        private LUIButton GetSlotHeadButton(int index)
        {
            var slotView = GetSlotView(index);
            return slotView != null ? slotView.m_head : null;
        }

    }
}
