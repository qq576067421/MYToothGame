using MonoBean;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityUI;
using GameDll;
using PrepareMatchStep = AndroidParseDataDemo.PrepareMatchStep;

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
        }

        private readonly SeatViewState[] m_SeatStates = new SeatViewState[m_MaxPlayerCount];
        private readonly LUIRawImage m_NoPlayerTemplate;
        private readonly LUIRawImage m_SystemPlayerTemplate;
        private AndroidParseDataDemo m_ParseDataDemo;
        private bool m_IsOpen;
        private int m_PlayerCount;
        private int m_ManualSelectingSeatId = -1;
        private int m_ManualSelectingPersonId = m_InvalidPersonId;

        public tower_defend_prepare_face_controller(
            IList<LUIButton> headButtons,
            IList<LUITextMesh> headInfos,
            LUIRawImage noPlayerTemplate,
            LUIRawImage systemPlayerTemplate)
        {
            m_NoPlayerTemplate = noPlayerTemplate;
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

        public void Open(AndroidParseDataDemo parseDataDemo, int playerCount)
        {
            Close();
            m_IsOpen = true;
            m_ParseDataDemo = parseDataDemo;
            SubscribeEvents();
            Refresh(playerCount);
        }

        public void Close()
        {
            UnsubscribeEvents();
            ClearManualSelection();
            for (int seatId = 0; seatId < m_SeatStates.Length; seatId++)
            {
                ResetSeatView(seatId, false);
            }

            m_PlayerCount = 0;
            m_ParseDataDemo = null;
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
            if (!m_IsOpen || !IsActiveSeat(seatId) || m_ManualSelectingSeatId >= 0)
            {
                return;
            }

            if (Application.platform != RuntimePlatform.Android ||
                m_ParseDataDemo == null || AndroidServerInfoDemo.Instance == null)
            {
                return;
            }

            var state = m_ParseDataDemo.GetPrepareSeatState(seatId);
            if (state == null || state.m_PersonId == m_InvalidPersonId ||
                state.m_Step == PrepareMatchStep.Empty || state.m_Step == PrepareMatchStep.WaitCenter)
            {
                return;
            }

            m_ManualSelectingSeatId = seatId;
            m_ManualSelectingPersonId = state.m_PersonId;
            SetHeadInfo(seatId, m_LanFaceSelectAvatar);
            AndroidServerInfoDemo.Instance.SelectRole(m_ParseDataDemo.ReadAssignedUserIds(seatId));
        }

        private void SubscribeEvents()
        {
            if (m_ParseDataDemo == null)
            {
                return;
            }

            m_ParseDataDemo.onPlayerFaceRecognizing += OnPlayerFaceRecognizing;
            m_ParseDataDemo.onPlayerFaceRecognizeFailed += OnPlayerFaceRecognizeFailed;
            m_ParseDataDemo.onPrepareSeatStateChanged += OnPrepareSeatStateChanged;
            AndroidServerInfoDemo.OnFaceRecognizedUser += OnFaceRecognizedUser;
            AndroidServerInfoDemo.OnManualRoleSelectionFailed += OnManualRoleSelectionFinished;
            AndroidServerInfoDemo.OnManualRoleSelectionCancelled += OnManualRoleSelectionFinished;
        }

        private void UnsubscribeEvents()
        {
            if (m_ParseDataDemo != null)
            {
                m_ParseDataDemo.onPlayerFaceRecognizing -= OnPlayerFaceRecognizing;
                m_ParseDataDemo.onPlayerFaceRecognizeFailed -= OnPlayerFaceRecognizeFailed;
                m_ParseDataDemo.onPrepareSeatStateChanged -= OnPrepareSeatStateChanged;
            }

            AndroidServerInfoDemo.OnFaceRecognizedUser -= OnFaceRecognizedUser;
            AndroidServerInfoDemo.OnManualRoleSelectionFailed -= OnManualRoleSelectionFinished;
            AndroidServerInfoDemo.OnManualRoleSelectionCancelled -= OnManualRoleSelectionFinished;
        }

        private void OnPlayerFaceRecognizing(int seatId)
        {
            ApplySeatState(seatId);
        }

        private void OnPlayerFaceRecognizeFailed(int seatId)
        {
            if (!IsActiveSeat(seatId) || m_ParseDataDemo == null)
            {
                return;
            }

            var state = m_ParseDataDemo.GetPrepareSeatState(seatId);
            if (state == null || state.m_Step != PrepareMatchStep.FaceRecognizing)
            {
                return;
            }

            ShowTemplate(seatId, m_SystemPlayerTemplate);
            SetHeadInfo(seatId, m_LanFaceWaitAvatarSelect);
            // SDK 没有稳定的识别失败结果，业务超时后仍需推进到举手阶段，头像选择保持可选。
            m_ParseDataDemo.TryNotifyPlayerFaceRecognized(seatId, 0, null);
        }

        private void OnPrepareSeatStateChanged(int seatId, AndroidParseDataDemo.PrepareMatchSeatState state)
        {
            ApplySeatState(seatId);
        }

        private void OnFaceRecognizedUser(int personId, long userId, bool isManualSelection)
        {
            if (!m_IsOpen || m_ParseDataDemo == null || userId <= 0)
            {
                return;
            }

            if (isManualSelection)
            {
                ApplyManualSelection(userId);
                return;
            }

            int seatId = m_ParseDataDemo.GetPlayerIndexByPersonId(personId);
            if (!IsActiveSeat(seatId))
            {
                return;
            }

            var state = m_ParseDataDemo.GetPrepareSeatState(seatId);
            if (state == null || state.m_Step != PrepareMatchStep.FaceRecognizing || state.m_PersonId != personId)
            {
                return;
            }

            string facePhotoPath = PlayerRoleManager.Instance.GetFacePhotoPathByUserId(userId);
            m_ParseDataDemo.TryNotifyPlayerFaceRecognized(seatId, userId, facePhotoPath);
        }

        private void ApplyManualSelection(long userId)
        {
            int seatId = m_ManualSelectingSeatId;
            if (!IsActiveSeat(seatId) || m_ParseDataDemo == null)
            {
                ClearManualSelection();
                return;
            }

            var state = m_ParseDataDemo.GetPrepareSeatState(seatId);
            if (state == null || state.m_PersonId != m_ManualSelectingPersonId)
            {
                ClearManualSelection();
                return;
            }

            string facePhotoPath = PlayerRoleManager.Instance.GetFacePhotoPathByUserId(userId);
            if (state.m_Step == PrepareMatchStep.FaceRecognizing)
            {
                m_ParseDataDemo.TryNotifyPlayerFaceRecognized(seatId, userId, facePhotoPath);
            }
            else
            {
                TryReplacePrepareSeatUser(state, userId, facePhotoPath);
            }

            ClearManualSelection();
            ApplySeatState(seatId);
        }

        private bool TryReplacePrepareSeatUser(
            AndroidParseDataDemo.PrepareMatchSeatState state,
            long userId,
            string facePhotoPath)
        {
            if (state == null || state.m_PersonId != m_ManualSelectingPersonId || userId <= 0 ||
                (state.m_Step != PrepareMatchStep.WaitRaiseHand && state.m_Step != PrepareMatchStep.Ready))
            {
                return false;
            }

            var assignedUserIds = m_ParseDataDemo.ReadAssignedUserIds(state.m_SeatId);
            for (int i = 0; i < assignedUserIds.Length; i++)
            {
                if (assignedUserIds[i] == userId)
                {
                    return false;
                }
            }

            // 人工选择只替换账号头像，不改变该玩家当前的举手或已准备状态。
            state.m_UserId = userId;
            state.m_FacePhotoPath = facePhotoPath;
            return true;
        }

        private void OnManualRoleSelectionFinished()
        {
            int seatId = m_ManualSelectingSeatId;
            ClearManualSelection();
            ApplySeatState(seatId);
        }

        private void ApplySeatState(int seatId)
        {
            if (seatId < 0 || seatId >= m_SeatStates.Length)
            {
                return;
            }

            if (!IsActiveSeat(seatId) || m_ParseDataDemo == null)
            {
                ResetSeatView(seatId, true);
                return;
            }

            var state = m_ParseDataDemo.GetPrepareSeatState(seatId);
            if (state == null || state.m_Step == PrepareMatchStep.Empty ||
                state.m_Step == PrepareMatchStep.WaitCenter)
            {
                ResetSeatView(seatId, true);
                return;
            }

            var seat = m_SeatStates[seatId];
            if (seat.m_PersonId != state.m_PersonId)
            {
                ShowTemplate(seatId, m_NoPlayerTemplate);
                seat.m_PersonId = state.m_PersonId;
                seat.m_UserId = 0;
            }

            if (state.m_Step == PrepareMatchStep.FaceRecognizing)
            {
                if (seat.m_UserId != 0 || seat.m_RuntimeTexture != null)
                {
                    ShowTemplate(seatId, m_NoPlayerTemplate);
                }

                seat.m_PersonId = state.m_PersonId;
                seat.m_UserId = 0;
                SetHeadInfo(seatId, m_LanFaceRecognizing);
                return;
            }

            if (state.m_UserId <= 0)
            {
                ShowTemplate(seatId, m_SystemPlayerTemplate);
                seat.m_PersonId = state.m_PersonId;
                SetHeadInfo(seatId, m_LanFaceWaitAvatarSelect);
                return;
            }

            StartAvatarLoad(seatId, state.m_PersonId, state.m_UserId, state.m_FacePhotoPath);
        }

        private void StartAvatarLoad(int seatId, int personId, long userId, string stateFacePhotoPath)
        {
            var seat = m_SeatStates[seatId];
            if (seat.m_PersonId == personId && seat.m_UserId == userId &&
                (seat.m_RuntimeTexture != null || seat.m_LoadCoroutine != null))
            {
                SetHeadInfo(seatId, seat.m_RuntimeTexture != null ? null : m_LanFaceRecognizing);
                return;
            }

            CancelLoad(seat);
            seat.m_PersonId = personId;
            seat.m_UserId = userId;
            int loadVersion = seat.m_LoadVersion;
            string avatarUri = PlayerRoleManager.Instance.GetAvatarUriPathByUserId(userId);
            string facePhotoPath = !string.IsNullOrEmpty(stateFacePhotoPath)
                ? stateFacePhotoPath
                : PlayerRoleManager.Instance.GetFacePhotoPathByUserId(userId);
            SetHeadInfo(seatId, m_LanFaceRecognizing);
            seat.m_LoadCoroutine = RenderAPI.StartCoroutine(
                LoadAvatar(seatId, personId, userId, loadVersion, avatarUri, facePhotoPath));
        }

        private IEnumerator LoadAvatar(
            int seatId,
            int personId,
            long userId,
            int loadVersion,
            string avatarUri,
            string facePhotoPath)
        {
            var seat = m_SeatStates[seatId];
            yield return LoadTexture(seat, loadVersion, avatarUri);
            if (seat.m_LoadVersion != loadVersion)
            {
                yield break;
            }

            if (seat.m_PendingTexture == null &&
                !string.Equals(avatarUri, facePhotoPath, StringComparison.Ordinal))
            {
                yield return LoadTexture(seat, loadVersion, facePhotoPath);
            }

            if (seat.m_LoadVersion != loadVersion)
            {
                yield break;
            }

            Texture loadedTexture = TakePendingTexture(seat);
            if (!m_IsOpen || seat.m_LoadVersion != loadVersion || seat.m_PersonId != personId ||
                seat.m_UserId != userId)
            {
                DestroyRuntimeTexture(loadedTexture);
                yield break;
            }

            seat.m_LoadCoroutine = null;
            if (loadedTexture == null)
            {
                ShowTemplate(seatId, m_SystemPlayerTemplate);
                seat.m_PersonId = personId;
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

            ShowTemplate(seatId, m_NoPlayerTemplate);
            var seat = m_SeatStates[seatId];
            seat.m_PersonId = m_InvalidPersonId;
            seat.m_UserId = 0;
            SetHeadInfo(seatId, showPrompt && IsActiveSeat(seatId) ? m_LanFaceEnterArea : null);
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
            m_ManualSelectingPersonId = m_InvalidPersonId;
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
        private const float m_PrepareRoleSelectWaveSpeedRatioReference = 0.7f;
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
        private int m_LastBoundDemoPlayerCount = -1;
        private int m_LastConfiguredSdkPlayerCount = -1;
        private PlayerMatchViewMode m_LastConfiguredSdkViewMode = PlayerMatchViewMode.Length;
        private int m_LastRemoteMenuPlayerCount = -1;
        private tower_defend_prepare_face_controller m_FaceController;
        private bool m_HasSubscribedSdkEvents;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.lobby;
            __ParticipateCurrentActiveWindow = true;
            __SelectDefaultActiveButton = false;
            __SetWindowCacheTime(0);
            __CreateModel(new tower_defend_prepare_model());
        }

        public void SetRequest(BattleStartupRequest request)
        {
            // 窗口已打开后如果重新塞入请求，需要立刻按新请求刷新界面。
            var model = GetModel<tower_defend_prepare_model>();
            model.m_Request = request;
            m_IsWaitingBattleLoading = false;
            m_RoleCandidateStageId = -1;
            m_LastBoundDemoPlayerCount = -1;
            m_LastConfiguredSdkPlayerCount = -1;
            m_LastConfiguredSdkViewMode = PlayerMatchViewMode.Length;
            m_LastRemoteMenuPlayerCount = -1;
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
            UnsubscribeSdkEvents();
            base.OnDestroy();
        }

        protected override void OnOpen()
        {
            // 业务订阅和显示刷新统一放到打开阶段，不放在 OnInitComponent。
            m_IsWaitingBattleLoading = false;
            var request = GetModel<tower_defend_prepare_model>().m_Request;
            m_FaceController?.Open(
                AndroidParseDataDemo.Instance,
                request != null ? request.ReadPreparePlayerCount() : 0);
            SubscribeSdkEvents();
            m_LastBoundDemoPlayerCount = -1;
            m_LastConfiguredSdkPlayerCount = -1;
            m_LastConfiguredSdkViewMode = PlayerMatchViewMode.Length;
            m_LastRemoteMenuPlayerCount = -1;
            RefreshView();
        }

        protected override void OnClose()
        {
            m_FaceController?.Close();
            UnsubscribeSdkEvents();

            if (!m_IsWaitingBattleLoading)
            {
                var parseDataDemo = AndroidParseDataDemo.Instance;
                if (parseDataDemo != null)
                {
                    parseDataDemo.ResetPreparePhaseState();
                }
            }

            base.OnClose();
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

            RefreshSdkSeatBindings(request);
            string error;
            if (!CGameProcedure.s_ProcLobby.TrySyncPrepareRequestFromSdk(request, out error))
            {
                tip_wnd.GetInstance().OnShowTip(error);
                return;
            }

            AndroidParseDataDemo parseDataDemo = null;
            if (ShouldUseSdkPrepareSelection())
            {
                parseDataDemo = AndroidParseDataDemo.Instance;
                if (parseDataDemo == null)
                {
                    tip_wnd.GetInstance().OnShowTipLanId(m_LanTdPrepareErrorSdkDataMissing);
                    return;
                }

                int requiredPlayerCount = request.ReadPreparePlayerCount();
                if (parseDataDemo.ReadConfirmedPlayerCount() < requiredPlayerCount)
                {
                    tip_wnd.GetInstance().OnShowTipLanId(m_LanTdPrepareErrorAllReady);
                    return;
                }
            }

            if (request.GetPlayerCount() != request.ReadPreparePlayerCount())
            {
                tip_wnd.GetInstance().OnShowTipLanId(m_LanTdPrepareErrorAllReady);
                return;
            }

            // 这里不能在点击确认后立刻关闭准备界面。
            // 进入战斗时真正负责遮挡的是 loading_wnd，而它要等覆盖动画播完后才会完全盖住底下界面。
            // 因此准备界面保持到大厅分支在 loading 覆盖完成后统一关闭，避免露出下层选择界面。
            if (!CGameProcedure.s_ProcLobby.StartBattleFromRequest(request, out error))
            {
                tip_wnd.GetInstance().OnShowTip(error);
                return;
            }

            if (parseDataDemo != null)
            {
                parseDataDemo.EnterBattleWithConfirmedPlayers();
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

            var request = GetModel<tower_defend_prepare_model>().m_Request;
            if (request == null)
            {
                m_FaceController?.Refresh(0);
                RefreshRemoteMenuButtons(null);
                return;
            }

            EnsureRoleSelections(request);
            BindRuntimeDemoComponents(request);
            ConfigureSdkPrepareView(request);
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
            bool useSdkPrepareSelection = ShouldUseSdkPrepareSelection();
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
            bool useSdkPrepareSelection = ShouldUseSdkPrepareSelection();
            bool isReady = useSdkPrepareSelection ? IsSdkSeatReady(seatId) : isSelected;

            RenderAPI.SetActive(slotView.m_choose, !useSdkPrepareSelection && isSelected);

            long roleCfgId = EnsureSelectedRoleForSeat(request, seatId);
            var heroCfg = roleCfgId > 0 ? t_heroBean.GetConfig(roleCfgId, false) : null;
            var roleName = heroCfg != null && !string.IsNullOrEmpty(heroCfg.t_name)
                ? heroCfg.t_name
                : RenderAPI.GetTextByLanId(m_LanTdPrepareRoleFallback, seatId + 1);
            //RenderAPI.SetText(slotView.m_txt, roleName);
            RefreshPlayerRoleIcon(slotView, seatId);
            RefreshPlayerStateText(slotView.m_txt_prepare, slotView.m_prepare, slotView.m_Fill, isReady, !useSdkPrepareSelection);
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

        private void RefreshPlayerStateText(LUITextMesh state, LUIImage prepare, LUIImage fill, bool isReady, bool updateFillAmount)
        {
            if (state == null || prepare == null)
            {
                return;
            }

            if (updateFillAmount && fill != null)
            {
                fill.fillAmount = isReady ? 1.0f : 0.0f;
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

            var parseDataDemo = AndroidParseDataDemo.Instance;
            request.ClearSdkSlotBindings();
            if (parseDataDemo == null || parseDataDemo._PlayerList == null)
            {
                return;
            }

            int preparePlayerCount = request.ReadPreparePlayerCount();
            for (int seatId = 0; seatId < preparePlayerCount; seatId++)
            {
                int sdkSlotIndex;
                if (!TryResolveSdkSlotIndexByUiSeat(parseDataDemo, seatId, out sdkSlotIndex))
                {
                    continue;
                }

                request.SetSdkSlotIndexForSeat(seatId, sdkSlotIndex);
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

        private void ConfigureSdkPrepareView(BattleStartupRequest request)
        {
            if (!ShouldUseSdkPrepareSelection() || request == null)
            {
                return;
            }

            var parseDataDemo = AndroidParseDataDemo.Instance;
            if (parseDataDemo == null)
            {
                return;
            }

            int playerCount = request.ReadPreparePlayerCount();
            PlayerMatchViewMode viewMode = ResolveSdkPrepareViewMode(playerCount);
            if (m_LastConfiguredSdkPlayerCount == playerCount &&
                m_LastConfiguredSdkViewMode == viewMode)
            {
                return;
            }

            parseDataDemo.InitGameInfo(playerCount, viewMode);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (AndroidServerInfoDemo.Instance != null)
            {
                AndroidServerInfoDemo.Instance.SetYouDooNotifyFaceRecognitionType(
                    YouDooSDKConstants.YouDooNotifyFaceRecognitionType.FRT_USER_ONLY);
            }
#endif
            parseDataDemo.ResetPreparePhaseState();
            if (parseDataDemo.playerTextuerShow != null)
            {
                parseDataDemo.playerTextuerShow.SetRegionLinesVisible(false);
            }

            m_LastConfiguredSdkPlayerCount = playerCount;
            m_LastConfiguredSdkViewMode = viewMode;
        }

        private void BindRuntimeDemoComponents(BattleStartupRequest request)
        {
            if (request == null)
            {
                return;
            }

            int playerCount = Mathf.Clamp(request.ReadPreparePlayerCount(), 0, m_MaxPreparePlayerCount);
            if (m_LastBoundDemoPlayerCount == playerCount)
            {
                return;
            }

            var parseDataDemo = AndroidParseDataDemo.Instance;
            if (parseDataDemo == null)
            {
                return;
            }

            // 克隆完成后统一恢复 Demo 持有的槽位、准备文字、进度图、状态底图和玩家按钮引用。
            parseDataDemo.ConfigurePreparePlayerSlots(BuildPreparePlayerSlots(playerCount));
            BindRuntimeCameraViews(parseDataDemo);
            m_LastBoundDemoPlayerCount = playerCount;
        }

        private List<Transform> BuildPreparePlayerSlots(int playerCount)
        {
            var slots = new List<Transform>(playerCount);
            for (int seatId = 0; seatId < playerCount; seatId++)
            {
                var slotBridge = GetSlotBridge(seatId);
                if (slotBridge == null)
                {
                    continue;
                }

                slots.Add(slotBridge.transform);
            }

            return slots;
        }

        private PlayerMatchViewMode ResolveSdkPrepareViewMode(int playerCount)
        {
            // 准备界面始终按 Player0~3 这四个显示槽承载相机画面。
            // 即使只有 1 人，也要使用 Player0 节点，不能切到主图 cameraViews[0]。
            return PlayerMatchViewMode.PartitionView;
        }

        private void ResetPrepareRequestForNewEntry(BattleStartupRequest request)
        {
            if (request == null)
            {
                return;
            }

            request.ClearSdkSlotBindings();
            for (int seatId = 0; seatId < m_MaxPreparePlayerCount; seatId++)
            {
                request.SetSelectedRoleCfgIdForSeat(seatId, 0);
            }

            if (request.m_Players != null)
            {
                request.m_Players.Clear();
            }
        }

        // 不在这里手写反向公式，直接比对同一个界面节点拿到 SDK 内部槽位。
        private bool TryResolveSdkSlotIndexByUiSeat(AndroidParseDataDemo parseDataDemo, int seatId, out int sdkSlotIndex)
        {
            sdkSlotIndex = -1;
            var slotBridge = GetSlotBridge(seatId);
            var slotTransform = slotBridge != null ? slotBridge.transform : null;
            var playerList = parseDataDemo != null ? parseDataDemo._PlayerList : null;
            if (slotTransform == null || playerList == null)
            {
                return false;
            }

            for (int i = 0; i < playerList.Count; i++)
            {
                if (playerList[i] == slotTransform)
                {
                    sdkSlotIndex = i;
                    return true;
                }
            }

            return false;
        }

        private bool ShouldUseSdkPrepareSelection()
        {
            return Application.platform == RuntimePlatform.Android;
        }

        private bool IsSdkSeatReady(int seatId)
        {
            var parseDataDemo = AndroidParseDataDemo.Instance;
            var readySeatIds = parseDataDemo != null ? parseDataDemo.GetReadySeatIds() : null;
            return readySeatIds != null && readySeatIds.ContainsKey(seatId);
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

        private void BindRuntimeCameraViews(AndroidParseDataDemo parseDataDemo)
        {
            var playerTextureShow = parseDataDemo != null ? parseDataDemo.playerTextuerShow : null;
            var textureBridge = playerTextureShow != null
                ? playerTextureShow.GetComponent<AndroidTextureBridgeBase>()
                : null;
            if (textureBridge == null)
            {
                return;
            }

            // PartitionView 约定下标0为主画面，下标1~4依次对应准备界面的四个玩家槽位。
            var cameraViews = new CameraTextureView[m_MaxPreparePlayerCount + 1];
            cameraViews[0] = textureBridge.GetCameraView(0);
            for (int seatId = 0; seatId < m_MaxPreparePlayerCount; seatId++)
            {
                var slotBridge = GetSlotBridge(seatId);
                cameraViews[seatId + 1] = slotBridge != null
                    ? slotBridge.GetComponentInChildren<CameraTextureView>(true)
                    : null;
            }

            textureBridge.SetCameraViews(cameraViews);
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

        // 订阅 SDK 准备和挥手换角色事件，正式开战前统一要求本局选择的人数全部准备完成。
        private void SubscribeSdkEvents()
        {
            if (m_HasSubscribedSdkEvents)
            {
                return;
            }

            var parseDataDemo = AndroidParseDataDemo.Instance;
            if (parseDataDemo == null)
            {
                return;
            }

            parseDataDemo.onCanGameStart += OnClickConfirm;
            parseDataDemo.onPlayerIsReady += OnSdkPlayerReadyChanged;
            parseDataDemo.onPlayerCancelReady += OnSdkPlayerStateChanged;
            parseDataDemo.onPlayerNotInReadyArea += OnSdkPlayerStateChanged;
            parseDataDemo.onNoneIsArea += OnSdkPlayerStateChanged;
            parseDataDemo.onPlayerDisappeared += OnSdkPlayersChanged;
            parseDataDemo.onPlayerReviced += OnSdkPlayersChanged;
            parseDataDemo.onPlayerSelectRoleLeft += OnSdkPlayerSelectRoleLeft;
            parseDataDemo.onPlayerSelectRoleRight += OnSdkPlayerSelectRoleRight;
            parseDataDemo.SetPrepareRoleSelectWaveSpeedRatio(m_PrepareRoleSelectWaveSpeedRatioReference);
            m_HasSubscribedSdkEvents = true;

            if (parseDataDemo.playerTextuerShow != null)
            {
                parseDataDemo.playerTextuerShow.SetRegionLinesVisible(false);
            }
        }

        // 窗口销毁时解除真机 SDK 订阅，避免残留回调。
        private void UnsubscribeSdkEvents()
        {
            if (!m_HasSubscribedSdkEvents)
            {
                return;
            }

            m_HasSubscribedSdkEvents = false;
            var parseDataDemo = AndroidParseDataDemo.Instance;
            if (parseDataDemo == null)
            {
                return;
            }

            parseDataDemo.onCanGameStart -= OnClickConfirm;
            parseDataDemo.onPlayerIsReady -= OnSdkPlayerReadyChanged;
            parseDataDemo.onPlayerCancelReady -= OnSdkPlayerStateChanged;
            parseDataDemo.onPlayerNotInReadyArea -= OnSdkPlayerStateChanged;
            parseDataDemo.onNoneIsArea -= OnSdkPlayerStateChanged;
            parseDataDemo.onPlayerDisappeared -= OnSdkPlayersChanged;
            parseDataDemo.onPlayerReviced -= OnSdkPlayersChanged;
            parseDataDemo.onPlayerSelectRoleLeft -= OnSdkPlayerSelectRoleLeft;
            parseDataDemo.onPlayerSelectRoleRight -= OnSdkPlayerSelectRoleRight;
        }

        private void OnSdkPlayerReadyChanged(int seatId, int curFrame, int needFrame)
        {
            RefreshView();
        }

        private void OnSdkPlayerStateChanged(int seatId)
        {
            RefreshView();
        }

        private void OnSdkPlayersChanged(int[] seatIds)
        {
            RefreshView();
        }

        private void OnSdkPlayerSelectRoleLeft(int seatId)
        {
            ChangeSelectedRole(seatId, -1);
        }

        private void OnSdkPlayerSelectRoleRight(int seatId)
        {
            ChangeSelectedRole(seatId, 1);
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
