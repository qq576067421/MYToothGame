
using LCL;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityUI;
using GameDll;

namespace GameHot
{
    public enum LobbyStatus
    {
        None,
        LoadLobbyScene,
        LoadingLobbyScene,
        LoadedLobbyScene,
        RPGLoop,
        GoingLogin,
        GoScene,
        GoingScene,
    }
    public class CGamePro_Lobby : CGameProcedure
    {
        private const int m_GlobalConfigIdBattleBaseHealth = 100306;
        private const int m_DefaultBattleBaseHealth = 100;
        private const float m_BattleLoadingMinVisibleSeconds = 3f;
        private const string m_LanTdLoadingEnterBattle = "td_loading_enter_battle";
        private const string m_LanTdErrorBattleRequestEmpty = "td_error_battle_request_empty";
        private const string m_LanTdErrorPrepareRequestEmpty = "td_error_prepare_request_empty";
        private const string m_LanTdErrorPrepareNoGuardRoles = "td_error_prepare_no_guard_roles";
        private const string m_LanTdErrorStageGuardMapMissing = "td_error_stage_guard_map_missing";
        private const string m_LanTdErrorStageGuardMapEmpty = "td_error_stage_guard_map_empty";
        private const string m_LanTdErrorStageGuardMapInvalid = "td_error_stage_guard_map_invalid";
        private const string m_LanTdErrorPrepareInvalidSeat = "td_error_prepare_invalid_seat";
        private const string m_LanTdErrorPrepareSeatOutCount = "td_error_prepare_seat_out_count";
        private const string m_LanTdErrorPrepareSdkSeatOnly = "td_error_prepare_sdk_seat_only";
        private const string m_LanTdErrorPrepareSdkBindingMissing = "td_error_prepare_sdk_binding_missing";
        private const string m_LanTdErrorSelectMinOneSeat = "td_error_select_min_one_seat";
        private const string m_LanTdErrorBattleStageMissing = "td_error_battle_stage_missing";
        private const string m_LanTdErrorBattleStageLocked = "td_error_battle_stage_locked";
        private const string m_LanTdErrorStageGuardSlotMissing = "td_error_stage_guard_slot_missing";
        private const string m_LanTdErrorSelectedRoleMissing = "td_error_selected_role_missing";
        private const string m_LanTdErrorBuildPlayerRoleMissing = "td_error_battle_player_role_missing";
        private lobby_main_wnd m_LobbyMainWnd;
        private BattleStartupRequest m_PendingBattleRequest;
        private BattleStartupRequest m_LastBattleRequest;
        private BattleStartupRequest m_PendingPrepareRequest;
        private BattleGameMode m_SelectedGameMode = BattleGameMode.Chapter;
        private int m_SelectedStageId = 0;
        private bool m_IsBattleSelectionInitialized = false;
        private bool m_IsBattleSceneTransitionRequested = false;
        private bool m_IsBackLoginTransitionRequested = false;


        // 初始化大厅流程：恢复上次选择，并把大厅默认状态和战斗入口数据准备好。
        protected override void Init()
        {
            m_ProType = EProcedureType.eLobby;
            LobbyPlayer.GetInstance().SetPlayerState(PlayerState.Lobby);
            LoadBattleSelection();
            InitializeBattleSelection();
            m_IsBattleSceneTransitionRequested = false;
            m_IsBackLoginTransitionRequested = false;

            Event.OnInputAction += OnInput;
            Event.OnEscapPressed += OnEscape;
            RenderEvent.Event.OnGmStartBattleRequest += StartBattleFromRequest;
        }

        protected override void UnInit()
        {
            Event.OnInputAction -= OnInput;
            Event.OnEscapPressed -= OnEscape;
            RenderEvent.Event.OnGmStartBattleRequest -= StartBattleFromRequest;
        }

        // 大厅流程主循环：负责加载大厅、进入战斗场景以及切换状态机。
        protected override void Tick()
        {
            switch ((LobbyStatus)m_Status)
            {
                case LobbyStatus.None:
                    {
                        break;
                    }

                case LobbyStatus.LoadLobbyScene:
                    {
                        var ab = "scene/lobby.jpg";
                        var assetName = "lobby";
                        UIRes.LoadLevel(ab, assetName, 0, () =>
                        {
                            OnLoadMainUI();

                        });
                        m_Status = (int)LobbyStatus.LoadingLobbyScene;
                        break;
                    }
                case LobbyStatus.LoadingLobbyScene:
                    {
                        break;
                    }
                case LobbyStatus.LoadedLobbyScene:
                    {
                        loading_wnd.CloseLoading();
                        m_IsBattleSceneTransitionRequested = false;
                        m_IsBackLoginTransitionRequested = false;
                        m_Status = (int)LobbyStatus.RPGLoop;
                        break;
                    }

                case LobbyStatus.RPGLoop:
                    {
                        break;
                    }
                case LobbyStatus.GoingLogin:
                    {
                        break;
                    }
                case LobbyStatus.GoScene:
                    {
                        loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingEnterBattle));
                        loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginGoSceneAfterLoading);
                        m_Status = (int)LobbyStatus.GoingScene;
                        break;
                    }
                case LobbyStatus.GoingScene:
                    {
                        break;
                    }
            }
        }

        // 加载并打开大厅主界面窗口。
        private void OnLoadMainUI()
        {
            m_LobbyMainWnd = UIManager.OpenWindowEX<lobby_main_wnd>(null);
        }

        private void BeginGoSceneAfterLoading()
        {
            if (m_Status != (int)LobbyStatus.GoingScene || !m_IsBattleSceneTransitionRequested)
            {
                return;
            }

            m_IsBattleSceneTransitionRequested = false;
            ExitLobby(true);
            LobbyPlayer.GetInstance().SetPlayerState(PlayerState.Battle);
            SetNextProc(CGameProcedure.s_ProcScene);
            AudioManager.GetInstance().Clear(AudioClearType.Transient);
            CGameProcedure.s_ProcScene.SetStatus((int)SceneStatus.StartBattleLogic);
        }
        // 大厅主 UI 完成后，允许流程继续进入可操作状态。
        public void OnMainUILoaded()
        {
            m_Status = (int)LobbyStatus.LoadedLobbyScene;
        }
        // 读取指定模式下当前可选择的关卡列表。
        public List<int> ReadSelectableStageIds(BattleGameMode gameMode)
        {
            return GetSelectableStageIds(gameMode);
        }

        // 读取章节转盘显示用的关卡列表。
        // 这里只保留章节配置本身有效的关卡，不按当前解锁进度裁剪。
        public List<int> ReadChapterDisplayStageIds()
        {
            var stageIds = TowerDefendStageConfigResolver.GetStageIds(BattleGameMode.Chapter);
            if (stageIds == null)
            {
                return new List<int>();
            }

            stageIds.Sort();
            return stageIds;
        }

        // 判断某个模式下是否存在可进入的关卡入口。
        public bool HasSelectableStageForMode(BattleGameMode gameMode)
        {
            return GetSelectableStageIds(gameMode).Count > 0;
        }

        // 设置大厅当前选中的模式和关卡，并统一做合法性校验与持久化。
        public bool TrySetSelectedStageId(BattleGameMode gameMode, int stageId)
        {
            if (gameMode == BattleGameMode.None)
            {
                Debug.LogError("设置大厅关卡失败：游戏模式无效。");
                return false;
            }

            var selectableStageIds = GetSelectableStageIds(gameMode);
            if (!selectableStageIds.Contains(stageId))
            {
                Debug.LogError("设置大厅关卡失败：目标关卡不在当前模式可选列表中。 mode=" + gameMode + " stageId=" + stageId);
                return false;
            }

            m_SelectedGameMode = gameMode;
            m_SelectedStageId = stageId;
            SaveBattleSelection();
            return true;
        }

        // 切换大厅选择的模式，并同步修正当前关卡选择。
        public void SetSelectedGameMode(BattleGameMode gameMode)
        {
            if (gameMode == BattleGameMode.None)
            {
                Debug.LogError("设置大厅游戏模式失败：不允许使用空的游戏模式。");
                return;
            }

            if (!HasSelectableStageForMode(gameMode))
            {
                Debug.LogError("设置大厅游戏模式失败：当前模式没有可用的塔防入口配置。模式：" + gameMode);
                return;
            }

            m_SelectedGameMode = gameMode;
            ClampSelectedStageToCurrentMode();
            SaveBattleSelection();
        }
        // 消费一次战斗启动请求，供场景流程使用。
        public bool TryConsumeBattleStartRequest(out BattleStartupRequest request)
        {
            if (m_PendingBattleRequest == null)
            {
                request = null;
                return false;
            }

            request = m_PendingBattleRequest;
            m_PendingBattleRequest = null;
            return true;
        }

        // 获取上一场正式战斗请求的克隆件，便于结算后重开或调试查看。
        public BattleStartupRequest GetLastBattleStartRequestClone()
        {
            return m_LastBattleRequest != null ? m_LastBattleRequest.CloneRequest() : null;
        }

        public bool TryQueuePrepareRequest(BattleStartupRequest request, out string error)
        {
            error = string.Empty;
            if (request == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareRequestEmpty);
                return false;
            }

            m_PendingPrepareRequest = request.CloneRequest();
            int preparePlayerCount = m_PendingPrepareRequest.ReadPreparePlayerCount();
            if (preparePlayerCount <= 0)
            {
                preparePlayerCount = m_PendingPrepareRequest.GetPlayerCount();
            }

            if (!TryValidateBattleStartupRequestStage(m_PendingPrepareRequest, out error))
            {
                m_PendingPrepareRequest = null;
                return false;
            }

            m_PendingPrepareRequest.SetPreparePlayerCount(preparePlayerCount);
            m_PendingPrepareRequest.ClearSdkSlotBindings();
            if (m_PendingPrepareRequest.m_Players == null)
            {
                m_PendingPrepareRequest.m_Players = new List<BattleStartupPlayerData>();
            }
            else
            {
                m_PendingPrepareRequest.m_Players.Clear();
            }

            if (!NormalizePrepareRequestSelectedRoles(m_PendingPrepareRequest, out error))
            {
                m_PendingPrepareRequest = null;
                return false;
            }

            RefreshBattleStartupRequestBaseHealth(m_PendingPrepareRequest);
            return true;
        }

        public bool TryConsumePendingPrepareRequest(out BattleStartupRequest request)
        {
            if (m_PendingPrepareRequest == null)
            {
                request = null;
                return false;
            }

            request = m_PendingPrepareRequest;
            m_PendingPrepareRequest = null;
            return true;
        }

        public bool TrySetPreparePlayerCount(BattleStartupRequest request, int playerCount, out string error)
        {
            error = string.Empty;
            if (request == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorBattleRequestEmpty);
                return false;
            }

            request.SetPreparePlayerCount(playerCount);
            request.ClearSdkSlotBindings();
            // 重新选择人数代表重新开始本次准备，具体初始角色由准备界面按席位顺序分配。
            for (int seatId = 0; seatId < TowerDefendSeatLayout.MaxSupportedPlayerCount; seatId++)
            {
                request.SetSelectedRoleCfgIdForSeat(seatId, 0);
            }

            if (request.m_Players == null)
            {
                request.m_Players = new List<BattleStartupPlayerData>();
            }
            else
            {
                request.m_Players.Clear();
            }

            RefreshBattleStartupRequestBaseHealth(request);
            return true;
        }

        private bool NormalizePrepareRequestSelectedRoles(BattleStartupRequest request, out string error)
        {
            error = string.Empty;
            if (request == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareRequestEmpty);
                return false;
            }

            List<long> roleCfgIds;
            if (!TryGetStageGuardRoleCfgIds(request.m_StageId, out roleCfgIds, out error))
            {
                return false;
            }

            if (roleCfgIds == null || roleCfgIds.Count <= 0)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareNoGuardRoles);
                return false;
            }

            for (int seatId = 0; seatId < TowerDefendSeatLayout.MaxSupportedPlayerCount; seatId++)
            {
                long selectedRoleCfgId;
                if (request.TryGetSelectedRoleCfgIdForSeat(seatId, out selectedRoleCfgId) &&
                    roleCfgIds.Contains(selectedRoleCfgId))
                {
                    continue;
                }

                request.SetSelectedRoleCfgIdForSeat(seatId, roleCfgIds[seatId % roleCfgIds.Count]);
            }

            return true;
        }

        public bool TryGetStageGuardRoleCfgIds(int stageId, out List<long> roleCfgIds, out string error)
        {
            roleCfgIds = new List<long>();
            error = string.Empty;

            var stageRoleCfg = t_tdStageGuardRoleBean.GetConfig(stageId, false);
            if (stageRoleCfg == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorStageGuardMapMissing, stageId);
                return false;
            }

            if (stageRoleCfg.t_guard_role_ids == null || stageRoleCfg.t_guard_role_ids.Count <= 0)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorStageGuardMapEmpty, stageId);
                return false;
            }

            for (int i = 0; i < stageRoleCfg.t_guard_role_ids.Count; i++)
            {
                long roleCfgId = stageRoleCfg.t_guard_role_ids[i];
                if (roleCfgId <= 0 || t_heroBean.GetConfig(roleCfgId, false) == null)
                {
                    continue;
                }

                roleCfgIds.Add(roleCfgId);
            }

            if (roleCfgIds.Count <= 0)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorStageGuardMapInvalid, stageId);
                return false;
            }

            return true;
        }

        // 准备界面切换座位。
        // 这里只允许编辑器调试使用，真机下座位始终由 SDK 判定。
        public bool TogglePrepareSeat(BattleStartupRequest request, int seatId, out string error)
        {
            error = string.Empty;
            if (request == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorBattleRequestEmpty);
                return false;
            }

            if (!TowerDefendSeatLayout.IsValidSeatId(seatId))
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareInvalidSeat);
                return false;
            }

            if (seatId >= request.ReadPreparePlayerCount())
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareSeatOutCount);
                return false;
            }

            if (ShouldUseSdkPrepareSelection())
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareSdkSeatOnly);
                return false;
            }

            if (request.m_Players == null)
            {
                request.m_Players = new List<BattleStartupPlayerData>();
            }

            var existingPlayer = FindPlayerBySeat(request, seatId);
            if (existingPlayer != null)
            {
                request.m_Players.Remove(existingPlayer);
                SortRequestPlayersBySeat(request);
                RefreshBattleStartupRequestBaseHealth(request);
                return true;
            }

            BattleStartupPlayerData playerData;
            long selectedRoleCfgId;
            request.TryGetSelectedRoleCfgIdForSeat(seatId, out selectedRoleCfgId);
            if (!TryBuildFormalBattlePlayerData(request.m_StageId, seatId, selectedRoleCfgId, out playerData, out error))
            {
                return false;
            }

            request.m_Players.Add(playerData);
            SortRequestPlayersBySeat(request);
            RefreshBattleStartupRequestBaseHealth(request);
            return true;
        }

        // 用真机 SDK 当前已启用且已就位的槽位重建准备请求。
        // 编辑器下直接保持传入请求不变。
        public bool TrySyncPrepareRequestFromSdk(BattleStartupRequest request, out string error)
        {
            error = string.Empty;
            if (!ShouldUseSdkPrepareSelection())
            {
                return true;
            }

            if (request == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorBattleRequestEmpty);
                return false;
            }

            if (request.m_Players == null)
            {
                request.m_Players = new List<BattleStartupPlayerData>();
            }
            else
            {
                request.m_Players.Clear();
            }

            var parseDataDemo = AndroidParseDataDemo.Instance;
            if (parseDataDemo == null)
            {
                request.m_Players.Clear();
                RefreshBattleStartupRequestBaseHealth(request);
                error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareSdkBindingMissing);
                return false;
            }

            int preparePlayerCount = request.ReadPreparePlayerCount();
            if (!request.TryValidateSdkSlotBindings(preparePlayerCount, out error))
            {
                request.m_Players.Clear();
                RefreshBattleStartupRequestBaseHealth(request);
                return false;
            }

            // 真机下统一按 Demo 当前判定“这个槽位是否有人”的结果回写正式请求，
            // 避免大厅再自行推断槽位占用状态。
            var readySeatIds = parseDataDemo.GetReadySeatIds();
            if (readySeatIds == null || readySeatIds.Count == 0)
            {
                request.m_Players.Clear();
                RefreshBattleStartupRequestBaseHealth(request);
                return true;
            }

            for (int seatId = 0; seatId < preparePlayerCount; seatId++)
            {
                int sdkSlotIndex;
                if (!request.TryGetSdkSlotIndexForSeat(seatId, out sdkSlotIndex))
                {
                    request.m_Players.Clear();
                    RefreshBattleStartupRequestBaseHealth(request);
                    error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareSdkBindingMissing);
                    return false;
                }

                if (!parseDataDemo.IsSlotEnabled(sdkSlotIndex))
                {
                    continue;
                }

                if (!readySeatIds.ContainsKey(sdkSlotIndex))
                {
                    continue;
                }

                BattleStartupPlayerData playerData;
                long selectedRoleCfgId;
                request.TryGetSelectedRoleCfgIdForSeat(seatId, out selectedRoleCfgId);
                if (!TryBuildFormalBattlePlayerData(request.m_StageId, seatId, selectedRoleCfgId, out playerData, out error))
                {
                    request.m_Players.Clear();
                    RefreshBattleStartupRequestBaseHealth(request);
                    return false;
                }

                request.m_Players.Add(playerData);
            }

            if (request.m_Players.Count <= 0)
            {
                RefreshBattleStartupRequestBaseHealth(request);
                return true;
            }

            SortRequestPlayersBySeat(request);
            RefreshBattleStartupRequestBaseHealth(request);
            return true;
        }

        // 外部直接给入正式请求时，仍走统一校验后再进入战斗。
        public bool StartBattleFromRequest(BattleStartupRequest request, out string error)
        {
            if (!CanAcceptBattleStartRequest(out error))
            {
                return false;
            }

            if (!TryQueueBattleStartRequest(request, out error))
            {
                return false;
            }

            BeginBattleSceneTransition();
            return true;
        }

        // 校验并缓存战斗请求，供场景流程真正创建战斗时消费。
        public bool TryQueueBattleStartRequest(BattleStartupRequest request, out string error)
        {
            BattleStartupRequest sanitizedRequest;
            if (!TryPrepareBattleStartRequest(request, out sanitizedRequest, out error))
            {
                return false;
            }

            StoreBattleStartRequest(sanitizedRequest);
            return true;
        }

        // 请求进入战斗场景，由流程机在后续 Tick 中完成切场。
        public void GoBattleScene()
        {
            if (!CanBeginBattleSceneTransition())
            {
                return;
            }

            BeginBattleSceneTransition();
        }

        private bool CanAcceptBattleStartRequest(out string error)
        {
            error = string.Empty;
            if (m_IsBattleSceneTransitionRequested || m_Status == (int)LobbyStatus.GoScene || m_Status == (int)LobbyStatus.GoingScene)
            {
                error = "战斗正在加载中。";
                return false;
            }

            if (m_IsBackLoginTransitionRequested || m_Status == (int)LobbyStatus.GoingLogin)
            {
                error = "正在返回登录。";
                return false;
            }

            if (loading_wnd.IsBlockingFlowInput())
            {
                error = "加载界面处理中。";
                return false;
            }

            if (!CanBeginBattleSceneTransition())
            {
                error = "当前大厅状态不能进入战斗。";
                return false;
            }

            return true;
        }

        private bool CanBeginBattleSceneTransition()
        {
            return m_Status == (int)LobbyStatus.RPGLoop || m_Status == (int)LobbyStatus.LoadedLobbyScene;
        }

        private void BeginBattleSceneTransition()
        {
            m_IsBattleSceneTransitionRequested = true;
            m_Status = (int)LobbyStatus.GoScene;
        }

        private void ExitLobby(bool jumpAnimation = false)
        {
            UIManager.CloseWindow(UIManager.GetCurrentActiveWindow());
            if (m_LobbyMainWnd != null)
            {
                UIManager.CloseWindow(m_LobbyMainWnd, jumpAnimation);
                m_LobbyMainWnd = null;
            }
        }

        public override void BackLogin()
        {
            m_IsBattleSceneTransitionRequested = false;
            m_IsBackLoginTransitionRequested = false;
            ExitLobby();
            m_PendingBattleRequest = null;
            m_PendingPrepareRequest = null;
            BackLoginCommon();
            loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, () =>
            {
                SetNextProc(CGameProcedure.s_ProcLogIn);
                CGameProcedure.s_ProcLogIn.SetStatus((int)LoginStatus.EnterLoginScene);
            });
        }
        // 统一执行战斗请求的拷贝、关卡校验和基础字段校验。
        private bool TryPrepareBattleStartRequest(BattleStartupRequest request, out BattleStartupRequest sanitizedRequest, out string error)
        {
            sanitizedRequest = request != null ? request.CloneRequest() : null;
            error = string.Empty;
            if (sanitizedRequest == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorBattleRequestEmpty);
                return false;
            }

            if (!TryValidateBattleStartupRequestStage(sanitizedRequest, out error))
            {
                return false;
            }

            SortRequestPlayersBySeat(sanitizedRequest);
            RefreshBattleStartupRequestBaseHealth(sanitizedRequest);
            if (sanitizedRequest.m_Players == null || sanitizedRequest.m_Players.Count <= 0)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorSelectMinOneSeat);
                return false;
            }

            if (!sanitizedRequest.TryValidate(out error))
            {
                return false;
            }

            return true;
        }

        // 缓存待消费的战斗请求，并记录最后一次正式请求。
        private void StoreBattleStartRequest(BattleStartupRequest request)
        {
            m_PendingBattleRequest = request;
            m_LastBattleRequest = request != null ? request.CloneRequest() : null;
            if (request == null)
            {
                return;
            }

            var stageCfg = TowerDefendStageConfigResolver.Resolve(request.m_StageId, request.m_GameMode);
            Debug.Log(string.Format(
                "[大厅战斗请求] 模式={0}({1})，关卡={2}，配置表={3}，场景={4}，玩家数={5}，基地血量={6}/{7}",
                TowerDefendStageConfigResolver.GetModeDebugName(request.m_GameMode),
                (int)request.m_GameMode,
                request.m_StageId,
                TowerDefendStageConfigResolver.GetConfigTableName(request.m_GameMode),
                stageCfg != null ? stageCfg.ScenePath : "未找到",
                request.GetPlayerCount(),
                request.m_BaseHealth,
                request.m_BaseMaxHealth));
        }

        public BattleStartupRequest CreateBattleStartupRequestShell()
        {
            if (m_SelectedStageId <= 0)
            {
                return null;
            }

            var selectableStageIds = GetSelectableStageIds(m_SelectedGameMode);
            if (!selectableStageIds.Contains(m_SelectedStageId))
            {
                return null;
            }

            var request = new BattleStartupRequest();
            request.m_BattleType = BattleType.TowerDefend;
            request.m_GameMode = m_SelectedGameMode;
            request.m_StageId = m_SelectedStageId;
            request.m_IsLocal = true;
            RefreshBattleStartupRequestBaseHealth(request);
            return request;
        }

        private void RefreshBattleStartupRequestBaseHealth(BattleStartupRequest request)
        {
            if (request == null)
            {
                return;
            }

            var baseHealth = ReadBattleBaseHealthFromGlobalConfig();
            request.m_BaseMaxHealth = baseHealth;
            request.m_BaseHealth = baseHealth;
        }

        private static int ReadBattleBaseHealthFromGlobalConfig()
        {
            var cfg = t_globalBean.GetConfig(m_GlobalConfigIdBattleBaseHealth, false);
            if (cfg == null)
            {
                return m_DefaultBattleBaseHealth;
            }

            return Math.Max(1, cfg.t_int);
        }

        private static void SortRequestPlayersBySeat(BattleStartupRequest request)
        {
            if (request == null || request.m_Players == null)
            {
                return;
            }

            request.m_Players.Sort((left, right) =>
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
        }

        private static BattleStartupPlayerData FindPlayerBySeat(BattleStartupRequest request, int seatId)
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

        private static bool ShouldUseSdkPrepareSelection()
        {
            return Application.platform == RuntimePlatform.Android;
        }

        private bool TryResolveStageRoleCfgId(int stageId, int index, out long roleCfgId, out string error)
        {
            roleCfgId = 0;
            error = string.Empty;

            var stageRoleCfg = t_tdStageGuardRoleBean.GetConfig(stageId, false);
            if (stageRoleCfg == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorStageGuardMapMissing, stageId);
                return false;
            }

            if (stageRoleCfg.t_guard_role_ids == null || stageRoleCfg.t_guard_role_ids.Count <= index)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorStageGuardSlotMissing, index);
                return false;
            }

            roleCfgId = stageRoleCfg.t_guard_role_ids[index];
            return true;
        }

        private bool TryResolveSelectedRoleCfgId(long selectedRoleCfgId, out long roleCfgId, out string error)
        {
            roleCfgId = 0;
            error = string.Empty;
            if (selectedRoleCfgId <= 0)
            {
                return false;
            }

            var heroCfg = t_heroBean.GetConfig(selectedRoleCfgId, false);
            if (heroCfg == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorSelectedRoleMissing, selectedRoleCfgId);
                return false;
            }

            roleCfgId = selectedRoleCfgId;
            return true;
        }

        // 按固定座位和默认角色生成正式战斗玩家数据。
        private bool TryBuildFormalBattlePlayerData(int stageId, int index, out BattleStartupPlayerData playerData, out string error)
        {
            return TryBuildFormalBattlePlayerData(stageId, index, 0, out playerData, out error);
        }

        // 选角流程允许多个座位选择同一个角色，因此这里按座位保存的选择优先，不做唯一性限制。
        // 没有显式选角时也不再按座位序号分配不同角色，而是统一使用候选列表第一个角色。
        private bool TryBuildFormalBattlePlayerData(int stageId, int index, long selectedRoleCfgId, out BattleStartupPlayerData playerData, out string error)
        {
            playerData = null;
            error = string.Empty;

            long roleCfgId;
            if (selectedRoleCfgId > 0)
            {
                if (!TryResolveSelectedRoleCfgId(selectedRoleCfgId, out roleCfgId, out error))
                {
                    return false;
                }
            }
            else if (!TryResolveStageRoleCfgId(stageId, 0, out roleCfgId, out error))
            {
                return false;
            }

            var heroCfg = t_heroBean.GetConfig(roleCfgId, false);
            if (heroCfg == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorBuildPlayerRoleMissing, roleCfgId);
                return false;
            }

            playerData = new BattleStartupPlayerData
            {
                m_PlayerId = index + 1,
                m_PlayerName = RenderAPI.GetTextByLanId("td_hud_default_player", index + 1),
                m_RoleCfgId = roleCfgId,
                m_RoleLevel = 1,
                m_IsAI = false,
                m_Group = GroupId.GuardGroupId,
                m_SeatId = index,
                m_HPPercent = 10000,
                m_MagicPercent = 10000,
            };
            return true;
        }
        // 检查战斗请求中的关卡是否对当前模式和解锁进度有效。
        private bool TryValidateBattleStartupRequestStage(BattleStartupRequest request, out string error)
        {
            error = string.Empty;
            if (request == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorBattleRequestEmpty);
                return false;
            }

            var stageCfg = TowerDefendStageConfigResolver.Resolve(request.m_StageId, request.m_GameMode);
            if (stageCfg == null)
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorBattleStageMissing);
                return false;
            }

            var unlockedStage = LobbyPlayer.GetInstance().GetUnlockedStage();
            if (!IsSelectableStageForMode(stageCfg, unlockedStage, request.m_GameMode))
            {
                error = RenderAPI.GetTextByLanId(m_LanTdErrorBattleStageLocked);
                return false;
            }

            return true;
        }

        // 初始化大厅的模式和关卡默认值。
        private void InitializeBattleSelection()
        {
            if (m_IsBattleSelectionInitialized)
            {
                return;
            }

            if (m_SelectedGameMode == BattleGameMode.None)
            {
                m_SelectedGameMode = BattleGameMode.Chapter;
            }

            NormalizeSelectedGameMode();

            if (m_SelectedStageId <= 0)
            {
                var profile = LobbyPlayer.GetInstance();
                profile.EnsureLocalPlayerInfo();
                m_SelectedStageId = ResolveDefaultSelectableStageId();
            }

            ClampSelectedStageToCurrentMode();
            m_IsBattleSelectionInitialized = true;
        }

        // 如果当前模式不可用，则自动回退到可用模式。
        private void NormalizeSelectedGameMode()
        {
            if (HasSelectableStageForMode(m_SelectedGameMode))
            {
                return;
            }

            if (HasSelectableStageForMode(BattleGameMode.Chapter))
            {
                m_SelectedGameMode = BattleGameMode.Chapter;
                return;
            }

            if (HasSelectableStageForMode(BattleGameMode.Endless))
            {
                m_SelectedGameMode = BattleGameMode.Endless;
            }
        }

        // 选择当前模式下默认可进入的关卡。
        private int ResolveDefaultSelectableStageId()
        {
            var selectableStageIds = GetSelectableStageIds(m_SelectedGameMode);
            if (selectableStageIds.Count == 0)
            {
                return 0;
            }

            var unlockedStage = LobbyPlayer.GetInstance().GetUnlockedStage();
            int selectedStageId = selectableStageIds[0];
            for (int i = 0; i < selectableStageIds.Count; i++)
            {
                if (selectableStageIds[i] > unlockedStage)
                {
                    break;
                }

                selectedStageId = selectableStageIds[i];
            }

            return selectedStageId;
        }
        // 将当前选择的关卡限制在当前模式可用范围内。
        private void ClampSelectedStageToCurrentMode()
        {
            var selectableStageIds = GetSelectableStageIds(m_SelectedGameMode);
            if (selectableStageIds.Count == 0)
            {
                m_SelectedStageId = 0;
                return;
            }

            bool containsSelected = false;
            for (int i = 0; i < selectableStageIds.Count; i++)
            {
                if (selectableStageIds[i] == m_SelectedStageId)
                {
                    containsSelected = true;
                    break;
                }
            }

            if (!containsSelected)
            {
                m_SelectedStageId = ResolveDefaultSelectableStageId();
            }
        }

        // 按模式筛出大厅中可选择的关卡列表。
        private List<int> GetSelectableStageIds(BattleGameMode gameMode)
        {
            var stageKeys = TowerDefendStageConfigResolver.GetStageIds(gameMode);
            var selectableStageIds = new List<int>();
            if (stageKeys == null)
            {
                return selectableStageIds;
            }

            var unlockedStage = LobbyPlayer.GetInstance().GetUnlockedStage();
            for (int i = 0; i < stageKeys.Count; i++)
            {
                var stageId = stageKeys[i];
                var stageCfg = TowerDefendStageConfigResolver.Resolve(stageId, gameMode);
                if (stageCfg == null)
                {
                    continue;
                }

                if (IsSelectableStageForMode(stageCfg, unlockedStage, gameMode))
                {
                    selectableStageIds.Add(stageId);
                }
            }

            selectableStageIds.Sort();
            return selectableStageIds;
        }

        // 判断某个关卡是否符合当前模式与解锁条件。
        private bool IsSelectableStageForMode(TowerDefendStageConfigAdapter stageCfg, int unlockedStage, BattleGameMode gameMode)
        {
            if (stageCfg == null)
            {
                return false;
            }

            if (gameMode == BattleGameMode.Endless)
            {
                if (stageCfg.EndlessMonsterPool == null || stageCfg.EndlessMonsterPool.Count == 0)
                {
                    return false;
                }

                if (stageCfg.WaveIntervalMs <= 0)
                {
                    return false;
                }

                return true;
            }

            return stageCfg.StageId <= unlockedStage && IsValidChapterStage(stageCfg);
        }

        // 校验章节模式关卡是否满足当前塔防规则约束。
        private bool IsValidChapterStage(TowerDefendStageConfigAdapter stageCfg)
        {
            if (stageCfg == null)
            {
                return false;
            }

            if (stageCfg.WaveCount <= 0 ||
                stageCfg.FirstWaveDelayMs <= 0 ||
                stageCfg.WaveIntervalMs <= 0)
            {
                return false;
            }

            return stageCfg.WaveCount > 0;
        }

        // 从本地存档恢复大厅选择。
        private void LoadBattleSelection()
        {
            var lobbyPlayer = LobbyPlayer.GetInstance();
            m_SelectedGameMode = lobbyPlayer.GetSavedBattleGameMode();
            m_SelectedStageId = lobbyPlayer.GetSavedBattleStageId();
        }

        // 将当前大厅选择写入本地存档。
        private void SaveBattleSelection()
        {
            LobbyPlayer.GetInstance().SetSavedBattleSelection(m_SelectedGameMode, m_SelectedStageId);
        }

        protected override void OnEscape(InputAction.CallbackContext context)
        {
            if (m_IsBackLoginTransitionRequested || m_IsBattleSceneTransitionRequested || loading_wnd.IsBlockingFlowInput())
            {
                return;
            }

            if (TryCloseCurrentLobbyActiveWindow())
            {
                return;
            }

            m_IsBackLoginTransitionRequested = true;
            //loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginBackLoginAfterLoading);
            BeginBackLoginAfterLoading();
        }

        private bool TryCloseCurrentLobbyActiveWindow()
        {
            var currentWindow = UIManager.GetCurrentActiveWindow();
            if (currentWindow == null ||
                currentWindow == m_LobbyMainWnd ||
                currentWindow.__GetWindowName() == nameof(lobby_main_wnd))
            {
                return false;
            }

            // 遥控返回先关闭当前参与最顶层判断的业务窗口；
            // 只有退到大厅主界面后，才继续执行原来的返回登录流程。
            UIManager.CloseWindow(currentWindow);
            return true;
        }

        private void BeginBackLoginAfterLoading()
        {
            if (!m_IsBackLoginTransitionRequested)
            {
                return;
            }

            BackLogin();
        }

        protected override void OnInput(InputAction.CallbackContext context, InputType inputType)
        {
            //AudioManager.GetInstance().Play2D(3);
        }
    }
}
