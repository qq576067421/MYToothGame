using GameDll;
using LCL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.InputSystem;
using YoudooGame.Core;
using YoudooGame.Framework;
using YoudooGame.MatchFlow;
using YoudooGame.MatchFlow.Runtime;

namespace GameHot
{
    public enum SceneStatus
    {
        None,
        StartBattleLogic,
        WaitingStartBattleLogic,
        LoadingBattle,
        WaitingBattleStartVisualReady,
        SceneLoop,
        GoingLogin
    }
    public class CGamePro_Scene : CGameProcedure
    {
        private enum BattleSceneLoadingTransition
        {
            None = 0,
            RestartBattle = 1,
            ReturnLobby = 2,
            BattleResultPrepare = 3,
        }

        // SDK v1.0.13 的公开 TryAddScore 只接受当前可操控的玩家。
        private static readonly FieldInfo m_SdkGameSessionField = typeof(MatchSessionCoordinator)
            .GetField("_gameSession", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly Dictionary<int, MatchSessionSlotSnapshot> m_SettlementRoster =
            new Dictionary<int, MatchSessionSlotSnapshot>();
        private long m_SettlementSessionToken = -1L;
        private string m_SettlementRoundId;
        private bool m_IsAwaitingBattleResult;

        private sealed class LeaderboardSubmission
        {
            public BattleResultData m_Result;
            public string m_Key;
            public LeaderboardScoreUpdate[] m_Entries;
            public long[] m_UserIds;
        }

        private const float m_LeaderboardReadbackTimeoutSeconds = 15f;
        private const float m_LeaderboardReadbackIntervalSeconds = 4f;
        private readonly Queue<LeaderboardSubmission> m_LeaderboardQueue = new Queue<LeaderboardSubmission>();
        private LeaderboardSubmission m_ActiveLeaderboardSubmission;
        private long m_LeaderboardNotificationSequence;
        private float m_LeaderboardReadbackDeadline;
        private float m_LeaderboardNextReadbackAt;
        private bool m_LeaderboardScoreConfirmed;
        private bool m_LeaderboardReadbackRequested;

        private const float m_BattleLoadingMinVisibleSeconds = 2.5f;
        private const float m_BattlePrewarmTimeoutSeconds = 30f;
        private const float m_RestartFrameTimeoutSeconds = 1f;
        private const float m_ResultGestureMinConfidence = 0.3f;
        private const float m_ResultGestureOverheadMarginRatio = 0.05f;
        private const int m_ResultGestureRequiredFrames = 15;
        private const float m_ResultGestureMaxFrameGap = 1f;
        private const string m_LanTdLoadingEnterBattle = "td_loading_enter_battle";
        private const string m_LanTdLoadingReturnLobby = "td_loading_return_lobby";
        private int m_BattleStartIssuedFrame = -1;
        private float m_BattlePrewarmStartedAt = -1f;
        private BattleStartupRequest m_LoadingBattleRequest;
        private BattleSceneLoadingTransition m_LoadingTransition = BattleSceneLoadingTransition.None;
        private BattleStartupRequest m_PendingRestartRequest;
        private PlayerFrame m_RestartFrameBaseline;
        private float m_RestartFrameDeadline;
        private int m_RestartApplicationStateRevision;
        private readonly int[] m_ResultGesturePersonIds = new int[TowerDefendSeatLayout.MaxSupportedPlayerCount];
        private readonly int[] m_ResultGestureFrameCounts = new int[TowerDefendSeatLayout.MaxSupportedPlayerCount];
        private readonly bool[] m_ResultGestureSawLoweredHands = new bool[TowerDefendSeatLayout.MaxSupportedPlayerCount];
        private MatchTrackingFrameSnapshot m_ResultGestureLastFrame;
        private float m_ResultGestureLastFrameAt;
        private int m_ResultGestureApplicationStateRevision;
        private float m_ResultGestureNextLogAt;
        private bool m_ResultGestureConsumed;
        protected override void Init()
        {
            m_ProType = EProcedureType.eScene;
            m_LoadingTransition = BattleSceneLoadingTransition.None;
            Event.OnEscapPressed += OnEscape;
            Event.OnInputAction += OnInput;
            RenderEvent.Event.OnTowerDefendBattleHudOpenRequest += OnTowerDefendBattleHudOpenRequest;
            RenderEvent.Event.OnTowerDefendBattleHudCloseRequest += OnTowerDefendBattleHudCloseRequest;
            RenderEvent.Event.OnTowerDefendPauseOpenRequest += OnTowerDefendPauseOpenRequest;
            RenderEvent.Event.OnTowerDefendPauseCloseRequest += OnTowerDefendPauseCloseRequest;
            RenderEvent.Event.OnTowerDefendPauseResumeStatusChanged += OnTowerDefendPauseResumeStatusChanged;
            RenderEvent.Event.OnTowerDefendRestartBattleRequest += RestartCurrentBattle;
            RenderEvent.Event.OnTowerDefendReturnLobbyRequest += ReturnToLobbyFromBattle;
            RenderEvent.Event.OnTowerDefendBattleResultFinalized += OnBattleResultFinalized;
        }

        protected override void Tick()
        {
            switch ((SceneStatus)m_Status)
            {
                case SceneStatus.None:
                    {
                        break;
                    }
                case SceneStatus.StartBattleLogic:
                    {
                        PrepareStartBattleLogic();
                        break;
                    }
                case SceneStatus.WaitingStartBattleLogic:
                    {
                        break;
                    }
                case SceneStatus.LoadingBattle:
                    {
                        loading_wnd.AddLoadingProgress(0.02f, RenderAPI.GetTextByLanId(m_LanTdLoadingEnterBattle));

                        var battle = CBattleLogic.GetInstance().GetScene();
                        if (battle == null || (battle is TowerDefendBattleScene towerScene && towerScene.HasLoadFailed()))
                        {
                            RecoverFromBattleLoadingFailure("场景加载或地图解析失败");
                            break;
                        }

                        if (!battle.IsLoaded())
                        {
                            break;
                        }

                        // 正式进入战斗前必须等场景解析和启动预热都完成，避免加载界面遮挡期间提前跑喊话或刷怪流程。
                        if (battle.ReadIsBattleStartLoadingReady())
                        {
                            if (!TryActivateLoadedBattle())
                            {
                                break;
                            }

                            battle.SetSceneStatus((int)ESceneState.StartGame);
                            m_BattleStartIssuedFrame = Time.frameCount;
                            m_Status = (int)SceneStatus.WaitingBattleStartVisualReady;
                        }
                        else if (m_BattlePrewarmStartedAt < 0f)
                        {
                            m_BattlePrewarmStartedAt = GameProductUiAdapter.ForegroundRealtime;
                        }
                        else if (GameProductUiAdapter.ForegroundRealtime - m_BattlePrewarmStartedAt >= m_BattlePrewarmTimeoutSeconds)
                        {
                            RecoverFromBattleLoadingFailure("战斗预热等待超时");
                        }

                        break;
                    }
                case SceneStatus.WaitingBattleStartVisualReady:
                    {
                        if (TryHandleBattleAccountChange())
                        {
                            break;
                        }

                        if (Time.frameCount <= m_BattleStartIssuedFrame + 1)
                        {
                            break;
                        }

                        loading_wnd.CloseLoading();
                        m_Status = (int)SceneStatus.SceneLoop;
                        break;
                    }
                case SceneStatus.SceneLoop:
                    {
                        if (TryHandleBattleAccountChange())
                        {
                            break;
                        }

                        var battle = CBattleLogic.GetInstance().GetScene();
                        if (battle != null && battle.TryConsumeResult(out var result))
                        {
                            m_IsAwaitingBattleResult = false;
                            ShowBattleResultInScene(result);
                        }

                        UpdatePendingRestart();
                        UpdateBattleResultGestureContinue();
                        break;
                    }
                case SceneStatus.GoingLogin:
                    {
                        break;
                    }
            }
        }

        protected override void UnInit()
        {
            ClearPendingRestart();
            m_LoadingTransition = BattleSceneLoadingTransition.None;
            m_IsAwaitingBattleResult = false;
            Event.OnEscapPressed -= OnEscape;
            Event.OnInputAction -= OnInput;
            RenderEvent.Event.OnTowerDefendBattleHudOpenRequest -= OnTowerDefendBattleHudOpenRequest;
            RenderEvent.Event.OnTowerDefendBattleHudCloseRequest -= OnTowerDefendBattleHudCloseRequest;
            RenderEvent.Event.OnTowerDefendPauseOpenRequest -= OnTowerDefendPauseOpenRequest;
            RenderEvent.Event.OnTowerDefendPauseCloseRequest -= OnTowerDefendPauseCloseRequest;
            RenderEvent.Event.OnTowerDefendPauseResumeStatusChanged -= OnTowerDefendPauseResumeStatusChanged;
            RenderEvent.Event.OnTowerDefendRestartBattleRequest -= RestartCurrentBattle;
            RenderEvent.Event.OnTowerDefendReturnLobbyRequest -= ReturnToLobbyFromBattle;
            RenderEvent.Event.OnTowerDefendBattleResultFinalized -= OnBattleResultFinalized;
        }

        private tower_defend_battle_hud_wnd m_HudWnd = null;
        private tower_defend_wave_notice_wnd m_NoticeWnd = null;
        private void OnTowerDefendBattleHudOpenRequest()
        {
            if(m_HudWnd != null)
            {
                UIManager.CloseWindow(m_HudWnd);
                m_HudWnd = null;
            }
            m_HudWnd = UIManager.OpenWindowEX<tower_defend_battle_hud_wnd>(null);

            if (m_NoticeWnd != null)
            {
                UIManager.CloseWindow(m_NoticeWnd);
                m_NoticeWnd = null;
            }
            m_NoticeWnd = UIManager.OpenWindowEX<tower_defend_wave_notice_wnd>(null);
        }

        private void OnTowerDefendBattleHudCloseRequest()
        {
            if (m_HudWnd != null)
            {
                UIManager.CloseWindow(m_HudWnd);
                m_HudWnd = null;
            }
            if (m_NoticeWnd != null)
            {
                UIManager.CloseWindow(m_NoticeWnd);
                m_NoticeWnd = null;
            }
        }

        private object OnTowerDefendPauseOpenRequest(Action onResume, Action onRestart, Action onReturn)
        {
            var wnd = UIManager.OpenWindowEX<tower_defend_pause_wnd>(null);
            wnd.SetCallbacks(onResume, onRestart, onReturn);
            return wnd;
        }

        private void OnTowerDefendPauseCloseRequest(object pauseWindow)
        {
            if (pauseWindow is WindowBase window)
            {
                UIManager.CloseWindow(window);
            }
        }

        private static void OnTowerDefendPauseResumeStatusChanged(object pauseWindow, TowerDefendResumeStatus status)
        {
            if (pauseWindow is tower_defend_pause_wnd window)
            {
                window.SetResumeStatus(status);
            }
        }

        private void OnStartBattle()
        {
            var inputData = CreateLevelInputData();
            if (inputData == null)
            {
                return;
            }
            CBattleLogic.GetInstance().CreateBattle(inputData);
        }

        private LevelInputData CreateLevelInputData()
        {
            if (!CGameProcedure.s_ProcLobby.TryConsumeBattleStartRequest(out var request, out var roster))
            {
                Debug.LogError("无法进入战斗：大厅没有提交正式的战斗启动请求。请先从大厅流程重新发起战斗。");
                return null;
            }

            m_LoadingBattleRequest = request;
            CaptureFrameworkSettlementRoster(request, roster);
            var inputData = new LevelInputData();
            inputData.m_BattleType = request.m_BattleType;
            inputData.m_Stage = request.m_StageId;
            inputData.m_StartRequest = request;
            inputData.m_BattleData = CreateBattleData(request);
            if (inputData.m_BattleData == null)
            {
                return null;
            }

            LogBattleEntryRequest(request, inputData.m_BattleData);
            return inputData;
        }

        private void LogBattleEntryRequest(BattleStartupRequest request, BattleData battleData)
        {
            if (request == null || battleData == null)
            {
                return;
            }

            var stageCfg = TowerDefendStageConfigResolver.Resolve(request.m_StageId, request.m_GameMode);
            Debug.Log(string.Format(
                "[塔防入战] 来源=大厅请求，模式={0}({1})，关卡={2}，配置表={3}，场景={4}，玩家数={5}",
                TowerDefendStageConfigResolver.GetModeDebugName(request.m_GameMode),
                (int)request.m_GameMode,
                request.m_StageId,
                TowerDefendStageConfigResolver.GetConfigTableName(request.m_GameMode),
                stageCfg != null ? stageCfg.ScenePath : "未找到",
                battleData.GetPlayerCount()));
        }

        private BattleData CreateBattleData(BattleStartupRequest request)
        {
            if (request == null)
            {
                Debug.LogError("无法进入战斗：战斗启动请求为空。");
                return null;
            }

            BattleData data;
            string error;
            if (!request.TryCreateBattleData(out data, out error))
            {
                Debug.LogError("无法进入战斗：" + error);
                return null;
            }

            return data;
        }

        private void OnBattleResultFinalized(BattleResultData result)
        {
            if (GameSaveService.Instance?.RequiresProfileReload == true)
            {
                return;
            }

            m_IsAwaitingBattleResult = true;
            if (m_PendingRestartRequest != null)
            {
                ClearPendingRestart();
                ClearLoadingTransition(BattleSceneLoadingTransition.RestartBattle);
            }

            var isWin = result.m_WinGroup == GroupId.GuardGroupId;
            var resultText = isWin ? "战斗胜利" : "战斗失败";
            Debug.Log(string.Format(
                "Scene battle finished. Result={0}, Reason={1}, WinGroup={2}, Stage={3}, UseTime={4}, FightId={5}",
                resultText,
                result.m_FinishReason,
                result.m_WinGroup,
                result.m_Stage,
                result.m_UseTime,
                result.m_FightId));

            ApplyBattleResultToLocalProfile(result);
            ReportFrameworkSettlement(result);
            if (result.m_GameMode == BattleGameMode.Endless)
            {
                SubmitEndlessLeaderboard(result);
            }
        }

        private void SubmitEndlessLeaderboard(BattleResultData result)
        {
            var submission = result.m_LeaderboardSubmission;
            submission.m_Score = Mathf.Max(0, result.m_BestProgressWave);
            string leaderboardKey = GameFrameworkBootstrap.EndlessLeaderboardKey;
            if (string.IsNullOrEmpty(leaderboardKey))
            {
                submission.m_State = TowerDefendLeaderboardSubmissionState.PendingSdk;
                Debug.LogWarning("[塔防排行榜] 未配置无尽排行榜 key。fight=" + result.m_FightId);
                return;
            }

            string score = submission.m_Score.ToString(CultureInfo.InvariantCulture);
            var entries = new List<LeaderboardScoreUpdate>();
            var userIds = new HashSet<long>();
            foreach (var slot in m_SettlementRoster.Values)
            {
                if (!slot.IsGuest && slot.UserId > 0 && userIds.Add(slot.UserId))
                {
                    entries.Add(new LeaderboardScoreUpdate(slot.UserId, score));
                }
            }

            if (entries.Count == 0)
            {
                return;
            }

            var users = new long[userIds.Count];
            userIds.CopyTo(users);
            m_LeaderboardQueue.Enqueue(new LeaderboardSubmission
            {
                m_Result = result,
                m_Key = leaderboardKey,
                m_Entries = entries.ToArray(),
                m_UserIds = users
            });
        }

        internal void PollLeaderboardSubmission()
        {
            if (GameProductUiAdapter.Instance?.IsApplicationForeground != true)
            {
                return;
            }

            float now = GameProductUiAdapter.ForegroundRealtime;
            if (m_ActiveLeaderboardSubmission == null)
            {
                if (m_LeaderboardQueue.Count == 0)
                {
                    return;
                }

                var next = m_LeaderboardQueue.Dequeue();
                var accepted = YoudooAPI.PlatformService.UpdateLeaderboard(next.m_Key, next.m_Entries, highScoreWins: true);
                if (!accepted.Success || !accepted.Value)
                {
                    UpdateLeaderboardStatus(next.m_Result, TowerDefendLeaderboardSubmissionState.Failed);
                    Debug.LogWarning("[塔防排行榜] 提交未受理。fight=" + next.m_Result.m_FightId + " error=" + accepted.Error);
                    return;
                }

                m_ActiveLeaderboardSubmission = next;
                m_LeaderboardNotificationSequence = YoudooAPI.PlatformService.GetNotificationsSince(long.MaxValue).NextSequence;
                m_LeaderboardReadbackDeadline = now + m_LeaderboardReadbackTimeoutSeconds;
                m_LeaderboardNextReadbackAt = now + 1f;
                m_LeaderboardScoreConfirmed = false;
                m_LeaderboardReadbackRequested = false;
                return;
            }

            var current = m_ActiveLeaderboardSubmission;
            var result = current.m_Result;
            var batch = YoudooAPI.PlatformService.GetNotificationsSince(m_LeaderboardNotificationSequence);
            m_LeaderboardNotificationSequence = batch.NextSequence;
            // 上传回执不含请求 ID。用参赛账号的云端成绩确认，并从同一响应读取排名。
            foreach (var item in batch.Items)
            {
                var notification = item.Notification;
                if (!m_LeaderboardReadbackRequested || notification.Kind != PlatformNotificationKind.UserScoresLoaded ||
                    !notification.Success ||
                    !GameFrameworkBootstrap.TryReadLeaderboardScores(notification.RawJson, current.m_UserIds,
                        result.m_LeaderboardSubmission.m_Score, out int bestRank))
                {
                    continue;
                }

                if (bestRank > 0)
                {
                    result.m_LeaderboardSubmission.m_Rank = bestRank;
                    FinishLeaderboardSubmission(true);
                    return;
                }

                if (!m_LeaderboardScoreConfirmed)
                {
                    m_LeaderboardScoreConfirmed = true;
                    UpdateLeaderboardStatus(result, TowerDefendLeaderboardSubmissionState.Submitted);
                }
            }

            if (now >= m_LeaderboardReadbackDeadline)
            {
                if (!m_LeaderboardScoreConfirmed)
                {
                    Debug.LogWarning("[塔防排行榜] 云端成绩确认超时。fight=" + result.m_FightId);
                }
                FinishLeaderboardSubmission(m_LeaderboardScoreConfirmed);
                return;
            }

            if (now >= m_LeaderboardNextReadbackAt)
            {
                m_LeaderboardNextReadbackAt = now + m_LeaderboardReadbackIntervalSeconds;
                var query = YoudooAPI.PlatformService.GetUserScores(current.m_Key, current.m_UserIds, true,
                    LeaderboardQueryMode.CurrentUsers);
                if (!query.Success || !query.Value)
                {
                    Debug.LogWarning("[塔防排行榜] 查询未受理。fight=" + result.m_FightId + " error=" + query.Error);
                }
                else
                {
                    m_LeaderboardReadbackRequested = true;
                }
            }
        }

        private void FinishLeaderboardSubmission(bool confirmed)
        {
            var result = m_ActiveLeaderboardSubmission.m_Result;
            m_ActiveLeaderboardSubmission = null;
            UpdateLeaderboardStatus(result, confirmed
                ? TowerDefendLeaderboardSubmissionState.Submitted
                : TowerDefendLeaderboardSubmissionState.Failed);
        }

        private static void UpdateLeaderboardStatus(BattleResultData result, TowerDefendLeaderboardSubmissionState state)
        {
            result.m_LeaderboardSubmission.m_State = state;
            if (UIManager.GetFirstWindow(nameof(tower_defend_endless_result_wnd)) is tower_defend_endless_result_wnd window)
            {
                window.RefreshLeaderboardStatus(result);
            }
        }

        private bool TryActivateLoadedBattle()
        {
            if (loading_wnd.GetRemainingVisibleMilliseconds() > 0)
            {
                return false;
            }

            var request = m_LoadingBattleRequest;
            if (request == null)
            {
                RecoverFromBattleLoadingFailure("缺少开战请求");
                return false;
            }

            if (GameProductUiAdapter.Instance?.IsApplicationForeground == false)
            {
                return false;
            }

            if (GameSaveService.Instance?.CheckAccountChangeBeforeBattle() == true)
            {
                ReturnToLoginForBattleAccountChange("battle-loading-account-change");
                return false;
            }

            if (request.m_FrameworkSessionToken >= 0)
            {
                if (YoudooAPI.TryGetSessionSnapshot(out var session) &&
                    (session.SessionToken != request.m_FrameworkSessionToken ||
                     session.RoundSnapshot?.RoundId != request.m_FrameworkRoundId))
                {
                    RecoverFromBattleLoadingFailure("加载期间 SDK 会话或回合已被替换");
                    return false;
                }

                Debug.Log("[塔防入战] 使用加载前确认的 SDK 回合。session=" + request.m_FrameworkSessionToken +
                          " round=" + request.m_FrameworkRoundId + " restart=" + request.m_RestartFrameworkRound);
            }

            m_LoadingBattleRequest = null;
            return true;
        }

        private bool TryHandleBattleAccountChange()
        {
            if (GameSaveService.Instance?.RequiresProfileReload != true ||
                (m_LoadingTransition != BattleSceneLoadingTransition.None && loading_wnd.IsBlockingFlowInput()))
            {
                return false;
            }

            ReturnToLoginForBattleAccountChange("battle-account-change");
            return true;
        }

        private void ReturnToLoginForBattleAccountChange(string reason)
        {
            Debug.LogWarning("[塔防流程] 平台账号已切换，退出当前战斗并返回登录。reason=" + reason +
                             " status=" + (SceneStatus)m_Status + " session=" + m_SettlementSessionToken +
                             " round=" + m_SettlementRoundId);
            m_Status = (int)SceneStatus.GoingLogin;
            m_LoadingBattleRequest = null;
            m_LoadingTransition = BattleSceneLoadingTransition.None;
            ClearPendingRestart();
            if (!TryLeaveBattleSession(reason))
            {
                Debug.LogWarning("[塔防入战] 退出旧账号回合失败，登录后重新建立会话。session=" + m_SettlementSessionToken);
            }

            CGameProcedure.s_ProcLobby.ClearBattleRequestsForAccountChange();
            GameProductUiAdapter.SuspendCamera(reason);
            CloseBattleTransitionWindowsImmediately();
            LobbyPlayer.GetInstance().SetPlayerState(PlayerState.Login);
            CBattleLogic.GetInstance().StopBattleLogic();
            loading_wnd.SetLoadingInfo(0f, RenderAPI.GetTextByLanId("td_loading_login_busy"));
            loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, () =>
            {
                if (CGameProcedure.s_ActiveProcedure == this && m_Status == (int)SceneStatus.GoingLogin)
                {
                    GameProductUiAdapter.HandoffCameraBeforeScene(reason, EnterLoginAfterBattleAccountChange);
                }
            });
        }

        private void EnterLoginAfterBattleAccountChange()
        {
            if (CGameProcedure.s_ActiveProcedure == this && m_Status == (int)SceneStatus.GoingLogin)
            {
                CGameProcedure.SetNextProc(CGameProcedure.s_ProcLogIn);
                CGameProcedure.s_ProcLogIn.SetStatus((int)LoginStatus.EnterLoginScene);
            }
        }

        private void CaptureFrameworkSettlementRoster(BattleStartupRequest request, MatchSessionSnapshot snapshot)
        {
            m_SettlementRoster.Clear();
            m_SettlementSessionToken = -1L;
            m_SettlementRoundId = null;
            if (snapshot == null)
            {
                return;
            }

            m_SettlementSessionToken = request.m_FrameworkSessionToken;
            m_SettlementRoundId = request.m_FrameworkRoundId;
            foreach (var player in request.m_Players)
            {
                if (player != null && request.TryGetSdkSlotIndexForSeat(player.m_SeatId, out int slotIndex) &&
                    snapshot.TryGetSlot(slotIndex, out var slot) && slot.IsFormalBinding)
                {
                    m_SettlementRoster[slotIndex] = slot;
                }
                else
                {
                    Debug.LogWarning("[塔防结算] 战斗座位缺少正式 SDK 绑定。seat=" + player?.m_SeatId +
                                     " session=" + snapshot.SessionToken);
                }
            }
        }

        private void ReportFrameworkSettlement(BattleResultData result)
        {
            if (m_SettlementSessionToken < 0)
            {
                return;
            }

            if (!YoudooAPI.TryGetSessionHost(out var host) || host.Snapshot == null)
            {
                Debug.LogError("[塔防结算] 无法获取 SDK 会话，游戏结算继续。");
                return;
            }

            var snapshot = host.Snapshot;
            if (m_SettlementSessionToken >= 0 && (m_SettlementSessionToken != snapshot.SessionToken ||
                (!string.IsNullOrEmpty(m_SettlementRoundId) && m_SettlementRoundId != snapshot.RoundSnapshot?.RoundId)))
            {
                Debug.LogError("[塔防结算] 正式名单所属回合已变化，避免将旧战斗提交到新回合。expected=" +
                               m_SettlementSessionToken + "/" + m_SettlementRoundId + " actual=" +
                               snapshot.SessionToken + "/" + snapshot.RoundSnapshot?.RoundId);
                return;
            }

            int score = result.m_GameMode == BattleGameMode.Endless
                ? Mathf.Max(0, result.m_BestProgressWave)
                : Mathf.Max(0, result.m_StarCount);
            var supplementalSlots = new List<MatchSessionSlotSnapshot>();
            foreach (var entry in m_SettlementRoster)
            {
                bool stillBound = TryGetSettlementSlot(snapshot, entry.Value, out var currentSlot);
                bool scoreRecorded = stillBound && (score == 0 || YoudooAPI.TryAddScore(currentSlot.SlotIndex, score));
                if (stillBound && !scoreRecorded)
                {
                    scoreRecorded = TryRecordTrackingIndependentScore(host.Coordinator, currentSlot.SlotIndex, score);
                    if (scoreRecorded)
                    {
                        Debug.Log("[塔防结算] 已为不可操控槽位补写分数。slot=" + currentSlot.SlotIndex +
                                  " score=" + score + " phase=" + currentSlot.Phase + " session=" + snapshot.SessionToken);
                    }
                }

                if (!scoreRecorded &&
                    (snapshot.Phase != GameSessionPhase.Playing ||
                     !TryReportSupplementalScore(entry.Value, score, result.m_FightId, snapshot.SessionToken)))
                {
                    supplementalSlots.Add(entry.Value);
                }
            }

            if (!YoudooAPI.CompleteGameplay())
            {
                Debug.LogError("[塔防结算] 游戏请求 SDK 结算失败。session=" + snapshot.SessionToken +
                               " phase=" + snapshot.Phase + " error=" + YoudooAPI.SessionLastError);
                return;
            }

            foreach (var slot in supplementalSlots)
            {
                if (!TryReportSupplementalScore(slot, score, result.m_FightId, snapshot.SessionToken))
                {
                    Debug.LogError("[塔防结算] 补充结算记录未被 SDK 接收。slot=" + slot.SlotIndex +
                                   " score=" + score + " session=" + snapshot.SessionToken);
                }
            }
        }

        private bool TryGetSettlementSlot(MatchSessionSnapshot snapshot, MatchSessionSlotSnapshot original,
            out MatchSessionSlotSnapshot slot)
        {
            bool hasRegisteredAccount = original.UserId > 0 && !original.IsGuest;
            foreach (var current in snapshot.Slots)
            {
                if (current.IsFormalBinding && original.MatchPlayerId >= 0 &&
                    current.MatchPlayerId == original.MatchPlayerId &&
                    (!hasRegisteredAccount || current.UserId == original.UserId))
                {
                    slot = current;
                    return true;
                }
            }

            slot = default;
            if (!snapshot.TryGetSlot(original.SlotIndex, out var rebound) || !rebound.IsFormalBinding ||
                (hasRegisteredAccount
                    ? rebound.UserId != original.UserId
                    : original.MatchPlayerId >= 0 || rebound.BoundPersonId != original.BoundPersonId))
            {
                return false;
            }

            foreach (var entry in m_SettlementRoster)
            {
                if (entry.Key != original.SlotIndex && entry.Value.MatchPlayerId >= 0 &&
                    entry.Value.MatchPlayerId == rebound.MatchPlayerId)
                {
                    return false;
                }
            }

            slot = rebound;
            Debug.Log("[塔防结算] 沿用原席位写分。slot=" + slot.SlotIndex + " session=" + snapshot.SessionToken +
                      " matchPlayer=" + original.MatchPlayerId + "->" + slot.MatchPlayerId);
            return true;
        }

        private static bool TryReportSupplementalScore(MatchSessionSlotSnapshot slot, int score, long fightId, long sessionToken)
        {
            // v1.0.13 会从内置结算名单排除已释放的槽位。会话级事件可以携带原始参赛身份。
            string playerInfo = FormattableString.Invariant(
                $"{{\"slotIndex\":{slot.SlotIndex},\"matchPlayerId\":{slot.MatchPlayerId},\"userId\":{slot.UserId},\"personId\":{slot.BoundPersonId},\"fightId\":{fightId}}}");
            bool accepted = YoudooAPI.TryReportGameplayData(-1, "tower_defend.final_score", "settlement",
                score.ToString(CultureInfo.InvariantCulture), playerInfo);
            if (accepted)
            {
                Debug.Log("[塔防结算] 补充结算记录已受理。slot=" + slot.SlotIndex +
                          " score=" + score + " session=" + sessionToken);
            }

            return accepted;
        }

        private static bool TryRecordTrackingIndependentScore(MatchSessionCoordinator coordinator, int slotIndex, int score)
        {
            if (coordinator == null || coordinator.GameSessionSnapshot.Phase != GameSessionPhase.Playing)
            {
                return false;
            }

            try
            {
                var gameSession = m_SdkGameSessionField?.GetValue(coordinator) as GameSessionService;
                return gameSession != null && gameSession.TryAddScore(slotIndex, score);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[塔防结算] SDK 兼容写分失败：" + exception.Message);
                return false;
            }
        }

        private bool TryLeaveBattleSession(string reason)
        {
            if (m_SettlementSessionToken < 0)
            {
                return true;
            }

            if (!YoudooAPI.TryGetSessionSnapshot(out var session) ||
                session.SessionToken != m_SettlementSessionToken || session.RoundSnapshot?.RoundId != m_SettlementRoundId)
            {
                Debug.LogWarning("[SDK会话] 原回合已不可用，仅退出本地战斗。reason=" + reason +
                                 " session=" + session?.SessionToken + " round=" + session?.RoundSnapshot?.RoundId);
                return true;
            }

            return GameFrameworkBootstrap.TryLeaveSession(reason, expectedToken: m_SettlementSessionToken);
        }

        private bool TryStartRestartRound(BattleStartupRequest request, PlayerFrame playerFrame,
            out MatchSessionSnapshot roster)
        {
            roster = null;
            if (request.m_FrameworkSessionToken < 0)
            {
                return true;
            }

            YoudooAPI.TryGetSessionSnapshot(out var snapshot);
            if (snapshot == null || GameProductUiAdapter.Instance?.IsApplicationForeground != true ||
                snapshot.SessionToken != request.m_FrameworkSessionToken || snapshot.SessionToken != m_SettlementSessionToken ||
                snapshot.RoundSnapshot?.RoundId != m_SettlementRoundId ||
                snapshot.LifecycleState != MatchSessionLifecycleState.Running ||
                snapshot.GameplayEndMode != GameplayEndMode.ProjectControlled ||
                (snapshot.Phase != GameSessionPhase.Playing && snapshot.Phase != GameSessionPhase.Paused &&
                 snapshot.Phase != GameSessionPhase.Settlement) ||
                (snapshot.TrackingHoldReason & ~MatchSessionTrackingHoldReason.SessionPaused) != MatchSessionTrackingHoldReason.None ||
                snapshot.AvatarSelectionRecovery?.IsBlocking == true)
            {
                Debug.LogWarning("[塔防重开] 原 SDK 回合暂时不可重开。session=" + snapshot?.SessionToken +
                                 " phase=" + snapshot?.Phase + " hold=" + snapshot?.TrackingHoldReason);
                return false;
            }

            if (!Application.isEditor && playerFrame == null)
            {
                Debug.LogWarning("[塔防重开] 缺少重开请求之后收到的设备帧。session=" + snapshot.SessionToken);
                return false;
            }

            foreach (var player in request.m_Players)
            {
                if (!request.TryGetSdkSlotIndexForSeat(player.m_SeatId, out var slotIndex) ||
                    !snapshot.TryGetSlot(slotIndex, out var slot) || !slot.IsReady || !slot.IsFormalBinding ||
                    (playerFrame != null && !playerFrame.TryGetPlayer(slot.BoundPersonId, out _)))
                {
                    Debug.LogWarning("[塔防重开] 正式席位未就绪或人员离镜。seat=" + player.m_SeatId +
                                     " session=" + snapshot.SessionToken + " frame=" + playerFrame?.FrameId);
                    return false;
                }

                request.m_SdkMatchPlayerIdsBySeat[player.m_SeatId] = slot.MatchPlayerId;
                request.SetPersonIdForSeat(player.m_SeatId, slot.BoundPersonId);
            }

            if (snapshot.Phase != GameSessionPhase.Settlement && !YoudooAPI.CompleteGameplay())
            {
                Debug.LogWarning("[塔防重开] SDK 未结束原回合。error=" + YoudooAPI.SessionLastError);
                return false;
            }

            if (!YoudooAPI.RestartGameplay() || !YoudooAPI.TryGetSessionSnapshot(out var startedSession) ||
                startedSession.SessionToken != snapshot.SessionToken ||
                string.IsNullOrEmpty(startedSession.RoundSnapshot?.RoundId) ||
                startedSession.RoundSnapshot.RoundId == m_SettlementRoundId ||
                (startedSession.Phase != GameSessionPhase.Playing && startedSession.Phase != GameSessionPhase.Paused))
            {
                Debug.LogWarning("[塔防重开] SDK 未确认新回合，保留暂停界面。error=" + YoudooAPI.SessionLastError);
                return false;
            }

            request.m_FrameworkRoundId = startedSession.RoundSnapshot.RoundId;
            request.m_RestartFrameworkRound = true;
            roster = startedSession;
            return true;
        }

        private void ShowBattleResultInScene(BattleResultData result)
        {
            if (result == null)
            {
                return;
            }

            ResetBattleResultGestureState();

            Action onReturn = () => HandleBattleResultReturn(result);

            if (result.m_GameMode == BattleGameMode.Endless)
            {
                Action onContinue = CanCreateBattleResultPrepareRequest(result)
                    ? () => HandleBattleResultPrepare(result)
                    : (Action)null;
                var wnd = UIManager.OpenWindowEX<tower_defend_endless_result_wnd>(null);
                wnd.SetResult(result, onContinue, onReturn);
                return;
            }

            Action onChapterContinue = CanCreateBattleResultPrepareRequest(result)
                ? () => HandleBattleResultPrepare(result)
                : (Action)null;
            var chapterWnd = UIManager.OpenWindowEX<tower_defend_chapter_result_wnd>(null);
            chapterWnd.SetResult(result, onChapterContinue, onReturn);
        }

        private void UpdateBattleResultGestureContinue()
        {
            if (Application.platform != RuntimePlatform.Android || m_ResultGestureConsumed ||
                m_LoadingTransition != BattleSceneLoadingTransition.None ||
                loading_wnd.IsBlockingFlowInput())
            {
                return;
            }

            var continueWindow = GetCurrentResultContinueWindow();
            if (continueWindow == null)
            {
                ResetBattleResultGestureState();
                return;
            }

            var adapter = GameProductUiAdapter.Instance;
            if (adapter?.IsApplicationForeground != true || !GameProductUiAdapter.HasLiveSession)
            {
                ClearResultGestureProgress();
                return;
            }

            if (m_ResultGestureApplicationStateRevision != adapter.ApplicationStateRevision)
            {
                m_ResultGestureApplicationStateRevision = adapter.ApplicationStateRevision;
                ClearResultGestureProgress();
            }

            YoudooAPI.TryGetSessionHost(out var host);
            var snapshot = host?.Snapshot;
            var frame = snapshot?.CurrentTrackingFrame;
            float now = Time.realtimeSinceStartup;
            if (snapshot == null || snapshot.SessionToken != m_SettlementSessionToken ||
                snapshot.RoundSnapshot?.RoundId != m_SettlementRoundId ||
                snapshot.Phase != GameSessionPhase.Settlement || frame == null ||
                frame.SessionToken != snapshot.SessionToken || frame.RoundId != m_SettlementRoundId ||
                frame.Phase != GameSessionPhase.Settlement)
            {
                ClearResultGestureProgress();
                LogResultGestureUnavailable(snapshot, now, "等待结算追踪帧");
                return;
            }

            if (m_ResultGestureLastFrame != null && m_ResultGestureLastFrame.SessionToken == frame.SessionToken &&
                m_ResultGestureLastFrame.RoundId == frame.RoundId)
            {
                bool frameGap = now - m_ResultGestureLastFrameAt >= m_ResultGestureMaxFrameGap;
                bool sameSource = frame.Provenance.SourceConnectionToken == m_ResultGestureLastFrame.Provenance.SourceConnectionToken &&
                    frame.Provenance.SourceSessionToken == m_ResultGestureLastFrame.Provenance.SourceSessionToken &&
                    frame.SourceId == m_ResultGestureLastFrame.SourceId;
                if (frameGap || !sameSource)
                {
                    ClearResultGestureProgress();
                }

                if (sameSource && m_ResultGestureLastFrame.Sequence == frame.Sequence)
                {
                    if (frameGap)
                    {
                        LogResultGestureUnavailable(snapshot, now, "结算追踪帧停滞");
                    }
                    return;
                }
            }
            else
            {
                ClearResultGestureProgress();
            }

            m_ResultGestureLastFrame = frame;
            m_ResultGestureLastFrameAt = now;
            bool hasGesturePlayer = false;
            for (int slot = 0; slot < m_ResultGestureFrameCounts.Length; slot++)
            {
                bool formal = frame.TryGetPlayer(slot, out var boundPlayer) && boundPlayer.Player != null;
                var player = formal ? boundPlayer.Player : FindReleasedResultGesturePlayer(host, snapshot, slot);
                if (!TryReadResultHandPose(player, out bool raised))
                {
                    m_ResultGestureFrameCounts[slot] = 0;
                    m_ResultGestureSawLoweredHands[slot] = false;
                    continue;
                }

                hasGesturePlayer = true;
                if (m_ResultGesturePersonIds[slot] != player.PersonId)
                {
                    m_ResultGesturePersonIds[slot] = player.PersonId;
                    m_ResultGestureFrameCounts[slot] = 0;
                    m_ResultGestureSawLoweredHands[slot] = false;
                }

                if (!raised)
                {
                    m_ResultGestureSawLoweredHands[slot] = true;
                    m_ResultGestureFrameCounts[slot] = 0;
                    continue;
                }

                if (m_ResultGestureSawLoweredHands[slot] &&
                    ++m_ResultGestureFrameCounts[slot] >= m_ResultGestureRequiredFrames)
                {
                    m_ResultGestureConsumed = continueWindow is tower_defend_chapter_result_wnd chapterWindow
                        ? chapterWindow.TryContinueByGesture()
                        : ((tower_defend_endless_result_wnd)continueWindow).TryContinueByGesture();
                    if (m_ResultGestureConsumed)
                    {
                        Debug.Log("[塔防结算手势] 举手继续已受理。slot=" + slot + " session=" + frame.SessionToken +
                                  " formal=" + formal);
                    }

                    ClearResultGestureProgress();
                    return;
                }
            }

            if (!hasGesturePlayer)
            {
                LogResultGestureUnavailable(snapshot, now, "结算席位没有可用的举手识别对象");
            }
        }

        private TrackedPlayer FindReleasedResultGesturePlayer(MatchSessionHost host, MatchSessionSnapshot snapshot,
            int slotIndex)
        {
            if (!m_SettlementRoster.ContainsKey(slotIndex) || !snapshot.TryGetSlot(slotIndex, out var slot) ||
                slot.IsFormalBinding || !snapshot.HasCurrentPlayerFrame)
            {
                return null;
            }

            var frame = snapshot.CurrentPlayerFrame;
            var config = host.Config;
            bool mirror = config.MirrorPlayerTrackingHorizontally && !frame.Metadata.MirrorAlreadyApplied;
            var projection = host.Presentation as IMatchSessionTrackingProjection;
            TrackedPlayer candidate = null;
            for (int index = 0; index < frame.Players.Count; index++)
            {
                var player = frame.Players[index];
                if (player.PersonId < 0 || !(player.Confidence >= config.MinimumConfidence))
                {
                    continue;
                }

                if (!IsUniqueResultGestureSlot(snapshot, slotIndex, player, mirror, projection))
                {
                    continue;
                }

                if (candidate != null)
                {
                    return null;
                }

                candidate = player;
            }

            return candidate;
        }

        private static bool IsUniqueResultGestureSlot(MatchSessionSnapshot snapshot, int slotIndex, TrackedPlayer player,
            bool mirror, IMatchSessionTrackingProjection projection)
        {
            int matchedSlot = -1;
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                var slot = snapshot.Slots[index];
                if (slot.IsFormalBinding && slot.BoundPersonId == player.PersonId)
                {
                    return false;
                }

                var area = slot.FlowSnapshot.MatchArea;
                var anchor = ReadResultGestureAnchor(player, slot.FlowSnapshot.AnchorPolicy, mirror, projection);
                if (area.Contains(anchor))
                {
                    if (matchedSlot >= 0)
                    {
                        return false;
                    }
                    matchedSlot = slot.SlotIndex;
                }
            }

            return matchedSlot == slotIndex;
        }

        private static Vector2 ReadResultGestureAnchor(TrackedPlayer player, MatchAnchor policy, bool mirror,
            IMatchSessionTrackingProjection projection)
        {
            if (policy == MatchAnchor.Nose &&
                TryProjectResultGestureJoint(player, PlayerJoint.Nose, mirror, projection, out var nose))
            {
                return nose;
            }

            if (TryProjectResultGestureJoint(player, PlayerJoint.Chest, mirror, projection, out var chest))
            {
                return chest;
            }

            if (TryProjectResultGestureJoint(player, PlayerJoint.LeftShoulder, mirror, projection, out var left) &&
                TryProjectResultGestureJoint(player, PlayerJoint.RightShoulder, mirror, projection, out var right))
            {
                return (left + right) * 0.5f;
            }

            return new Vector2(float.NaN, float.NaN);
        }

        private static bool TryProjectResultGestureJoint(TrackedPlayer player, PlayerJoint joint, bool mirror,
            IMatchSessionTrackingProjection projection, out Vector2 position)
        {
            position = default;
            if (!TryReadTrackedBodyJoint(player, joint, out var point))
            {
                return false;
            }

            if (projection == null || !projection.TryProjectTrackedPoint(player, joint, mirror, out position))
            {
                position = new Vector2(mirror ? 1f - point.x : point.x, point.y);
            }
            return true;
        }

        private void LogResultGestureUnavailable(MatchSessionSnapshot snapshot, float now, string reason)
        {
            if (now < m_ResultGestureNextLogAt)
            {
                return;
            }

            m_ResultGestureNextLogAt = now + 3f;
            Debug.LogWarning("[塔防结算手势] " + reason + " phase=" + snapshot?.Phase +
                             " frame=" + snapshot?.FrameState + " hold=" + snapshot?.TrackingHoldReason);
        }

        private static WindowBase GetCurrentResultContinueWindow()
        {
            var activeWindow = UIManager.GetCurrentActiveWindow();
            if ((activeWindow is tower_defend_chapter_result_wnd chapterResultWnd && chapterResultWnd.CanContinue) ||
                (activeWindow is tower_defend_endless_result_wnd endlessResultWnd && endlessResultWnd.CanContinue))
            {
                return activeWindow;
            }

            return null;
        }

        private static bool TryReturnCurrentResultWindow()
        {
            var activeWindow = UIManager.GetCurrentActiveWindow();
            if (activeWindow is tower_defend_chapter_result_wnd chapterResultWnd)
            {
                chapterResultWnd.TryReturnByRemote();
                return true;
            }

            if (activeWindow is tower_defend_endless_result_wnd endlessResultWnd)
            {
                endlessResultWnd.TryReturnByRemote();
                return true;
            }

            return false;
        }

        private static bool TryReadResultHandPose(TrackedPlayer player, out bool raised)
        {
            raised = false;
            if (!TryReadTrackedBodyJoint(player, PlayerJoint.LeftShoulder, out var leftShoulder) ||
                !TryReadTrackedBodyJoint(player, PlayerJoint.RightShoulder, out var rightShoulder))
            {
                return false;
            }

            float shoulderWidth = Mathf.Abs(leftShoulder.x - rightShoulder.x);
            float headTopY = float.PositiveInfinity;
            TryMergeHeadTopY(player, PlayerJoint.Nose, ref headTopY);
            TryMergeHeadTopY(player, PlayerJoint.LeftEye, ref headTopY);
            TryMergeHeadTopY(player, PlayerJoint.RightEye, ref headTopY);
            if (shoulderWidth <= 0f || float.IsPositiveInfinity(headTopY))
            {
                return false;
            }

            float threshold = headTopY - shoulderWidth * m_ResultGestureOverheadMarginRatio;
            bool leftTracked = TryReadTrackedBodyJoint(player, PlayerJoint.LeftWrist, out var leftWrist);
            bool rightTracked = TryReadTrackedBodyJoint(player, PlayerJoint.RightWrist, out var rightWrist);
            raised = (leftTracked && leftWrist.y <= threshold) || (rightTracked && rightWrist.y <= threshold);
            return raised || (leftTracked && rightTracked);
        }

        private static void TryMergeHeadTopY(TrackedPlayer player, PlayerJoint joint, ref float headTopY)
        {
            if (TryReadTrackedBodyJoint(player, joint, out var position))
            {
                headTopY = Mathf.Min(headTopY, position.y);
            }
        }

        private static bool TryReadTrackedBodyJoint(TrackedPlayer player, PlayerJoint joint, out Vector3 position)
        {
            if (player != null && player.TryGetJoint(joint, out var sample) &&
                sample.IsTracked && sample.Confidence >= m_ResultGestureMinConfidence &&
                !float.IsNaN(sample.Position.x) && !float.IsInfinity(sample.Position.x) &&
                !float.IsNaN(sample.Position.y) && !float.IsInfinity(sample.Position.y))
            {
                position = sample.Position;
                return true;
            }

            position = default;
            return false;
        }

        private void ClearResultGestureProgress()
        {
            Array.Clear(m_ResultGestureFrameCounts, 0, m_ResultGestureFrameCounts.Length);
            Array.Clear(m_ResultGestureSawLoweredHands, 0, m_ResultGestureSawLoweredHands.Length);
        }

        private void ResetBattleResultGestureState()
        {
            ClearResultGestureProgress();
            m_ResultGestureLastFrame = null;
            m_ResultGestureApplicationStateRevision = GameProductUiAdapter.Instance?.ApplicationStateRevision ?? 0;
            m_ResultGestureNextLogAt = Time.realtimeSinceStartup + 2f;
            m_ResultGestureConsumed = false;
        }

        private void HandleBattleResultReturn(BattleResultData result)
        {
            if (!TryBeginLoadingTransition(BattleSceneLoadingTransition.ReturnLobby))
            {
                return;
            }

            if (!TryLeaveBattleSession("battle-return"))
            {
                ClearLoadingTransition(BattleSceneLoadingTransition.ReturnLobby);
                return;
            }
            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingReturnLobby));
            loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginReturnToLobbyAfterLoading);
        }

        private void HandleBattleResultPrepare(BattleResultData result)
        {
            BattleStartupRequest request;
            string error;
            if (!TryCreateBattleResultPrepareRequest(result, out request, out error))
            {
                Debug.LogError("无法进入结算后的准备流程：" + error);
                return;
            }

            if (!TryBeginLoadingTransition(BattleSceneLoadingTransition.BattleResultPrepare))
            {
                return;
            }

            if (!CGameProcedure.s_ProcLobby.TryQueuePrepareRequest(request, out error))
            {
                ClearLoadingTransition(BattleSceneLoadingTransition.BattleResultPrepare);
                Debug.LogError("无法缓存结算后的准备请求：" + error);
                return;
            }

            if (!TryLeaveBattleSession("result-prepare"))
            {
                CGameProcedure.s_ProcLobby.TryConsumePendingPrepareRequest(out _);
                ClearLoadingTransition(BattleSceneLoadingTransition.BattleResultPrepare);
                return;
            }

            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingReturnLobby));
            loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginBattleResultPrepareAfterLoading);
        }

        private bool CanCreateBattleResultPrepareRequest(BattleResultData result)
        {
            BattleStartupRequest request;
            string error;
            return TryCreateBattleResultPrepareRequest(result, out request, out error);
        }

        private bool TryCreateBattleResultPrepareRequest(BattleResultData result, out BattleStartupRequest request, out string error)
        {
            request = null;
            error = string.Empty;
            if (result == null)
            {
                error = "结算数据为空。";
                return false;
            }

            if (result.m_GameMode != BattleGameMode.Chapter && result.m_GameMode != BattleGameMode.Endless)
            {
                error = "只有关卡模式和无尽模式结算会重新进入准备匹配。";
                return false;
            }

            var lastRequest = CGameProcedure.s_ProcLobby.GetLastBattleStartRequestClone();
            if (lastRequest == null)
            {
                error = "没有上一场战斗请求。";
                return false;
            }

            var targetMode = result.m_GameMode;
            int targetStage = ResolveBattleResultPrepareStage(result, lastRequest);
            if (!TowerDefendStageConfigResolver.Exists(targetStage, targetMode))
            {
                error = "目标关卡不存在，关卡：" + targetStage;
                return false;
            }

            request = lastRequest;
            request.m_GameMode = targetMode;
            request.m_StageId = targetStage;
            request.m_BattleType = BattleType.TowerDefend;
            request.m_IsLocal = true;
            return true;
        }

        private static int ResolveBattleResultPrepareStage(BattleResultData result, BattleStartupRequest lastRequest)
        {
            if (result.m_GameMode == BattleGameMode.Chapter)
            {
                int currentStage = result.m_Stage > 0 ? result.m_Stage : lastRequest.m_StageId;
                if (result.m_WinGroup != GroupId.GuardGroupId)
                {
                    return currentStage;
                }

                return ResolveNextChapterStage(currentStage);
            }

            // 无尽模式没有下一关概念，结算继续只是回到准备匹配后重开同一入口。
            return lastRequest.m_StageId > 0 ? lastRequest.m_StageId : result.m_Stage;
        }

        private static int ResolveNextChapterStage(int stageId)
        {
            int nextStage = stageId + 1;
            return TowerDefendStageConfigResolver.Exists(nextStage, BattleGameMode.Chapter) ? nextStage : stageId;
        }

        private void ApplyBattleResultToLocalProfile(BattleResultData result)
        {
            var profile = LobbyPlayer.GetInstance();
            profile.EnsureLocalPlayerInfo();
            if (result == null)
            {
                return;
            }

            if (result.m_TotalRewardGold > 0)
            {
                profile.AddCoin(result.m_TotalRewardGold);
            }

            if (result.m_GameMode == BattleGameMode.Chapter && result.m_WinGroup == GroupId.GuardGroupId && result.m_Stage > 0)
            {
                int nextStage = ResolveNextChapterStage(result.m_Stage);
                result.m_UnlockedStage = Mathf.Max(profile.GetUnlockedStage(), nextStage);
                profile.SetUnlockedStage(result.m_UnlockedStage);
                profile.SetStageStar(result.m_Stage, result.m_StarCount);
            }
            else
            {
                result.m_UnlockedStage = profile.GetUnlockedStage();
            }

            if (result.m_GameMode == BattleGameMode.Endless)
            {
                profile.SetBestEndlessWave(result.m_BestProgressWave);
                result.m_BestEndlessWave = profile.GetBestEndlessWave();
            }
            else
            {
                result.m_BestEndlessWave = profile.GetBestEndlessWave();
            }
        }

        public void RestartCurrentBattle()
        {
            if (m_IsAwaitingBattleResult)
            {
                return;
            }

            var lobby = CGameProcedure.s_ProcLobby;
            var request = lobby.GetLastBattleStartRequestClone();
            if (request == null)
            {
                Debug.LogWarning("[塔防重开] 缺少上一场战斗请求。");
                return;
            }

            // 直接重开沿用战斗中已经完成补位的骨骼绑定，不能退回准备阶段的旧 personId。
            var towerDefendScene = CBattleLogic.GetInstance().GetScene() as TowerDefendBattleScene;
            towerDefendScene?.WriteCurrentBonePersonIds(request);

            if (!TryBeginLoadingTransition(BattleSceneLoadingTransition.RestartBattle))
            {
                return;
            }

            if (!lobby.TryPrepareBattleStartRequest(request, out var sanitizedRequest, out var error))
            {
                ClearLoadingTransition(BattleSceneLoadingTransition.RestartBattle);
                Debug.LogWarning("[塔防重开] 战斗请求校验失败：" + error);
                return;
            }

            if (Application.isEditor || sanitizedRequest.m_FrameworkSessionToken < 0)
            {
                TryCommitRestart(sanitizedRequest, null);
                return;
            }

            var adapter = GameProductUiAdapter.Instance;
            if (adapter?.IsApplicationForeground != true || !RenderEvent.Event.OnTowerDefendBattlePauseStateQuery())
            {
                Debug.LogWarning("[塔防重开] 当前不处于前台暂停状态，无法检查设备帧。");
                ClearLoadingTransition(BattleSceneLoadingTransition.RestartBattle);
                return;
            }

            var baseline = YoudooAPI.Player.GetFrame();
            m_RestartFrameBaseline = baseline.Success ? baseline.Value : null;
            m_RestartFrameDeadline = Time.realtimeSinceStartup + m_RestartFrameTimeoutSeconds;
            m_RestartApplicationStateRevision = adapter.ApplicationStateRevision;
            m_PendingRestartRequest = sanitizedRequest;
            FindPauseWindow()?.SetRestartFrameCheckPending(true);
        }

        private void UpdatePendingRestart()
        {
            var request = m_PendingRestartRequest;
            if (request == null)
            {
                return;
            }

            var adapter = GameProductUiAdapter.Instance;
            if (!RenderEvent.Event.OnTowerDefendBattlePauseStateQuery() ||
                adapter?.IsApplicationForeground != true ||
                adapter.ApplicationStateRevision != m_RestartApplicationStateRevision ||
                GameSaveService.Instance?.RequiresProfileReload == true)
            {
                Debug.LogWarning("[塔防重开] 应用或暂停状态已变化，取消设备帧检查。session=" +
                                 request.m_FrameworkSessionToken);
                ClearPendingRestart();
                ClearLoadingTransition(BattleSceneLoadingTransition.RestartBattle);
                return;
            }

            var result = YoudooAPI.Player.GetFrame();
            if (Time.realtimeSinceStartup >= m_RestartFrameDeadline)
            {
                Debug.LogWarning("[塔防重开] 等待设备新帧超时，原回合保持暂停。session=" +
                                 request.m_FrameworkSessionToken + " frame=" + m_RestartFrameBaseline?.FrameId +
                                 " error=" + result.Error);
                ClearPendingRestart();
                ClearLoadingTransition(BattleSceneLoadingTransition.RestartBattle);
                FindPauseWindow()?.ShowRestartFailure();
                return;
            }

            if (!result.Success || result.Value == null)
            {
                return;
            }

            var frame = result.Value;
            var baseline = m_RestartFrameBaseline;
            if (baseline == null || frame.SourceId != baseline.SourceId)
            {
                m_RestartFrameBaseline = frame;
                return;
            }

            long sourceSession = frame.Provenance.SourceSessionToken;
            long sourceConnection = frame.Provenance.SourceConnectionToken;
            long baselineSession = baseline.Provenance.SourceSessionToken;
            long baselineConnection = baseline.Provenance.SourceConnectionToken;
            if (sourceSession != baselineSession || sourceConnection != baselineConnection)
            {
                if (sourceSession > baselineSession ||
                    (sourceSession == baselineSession && sourceConnection > baselineConnection))
                {
                    m_RestartFrameBaseline = frame;
                }
                return;
            }

            if (IsRestartFrameNewer(frame, baseline))
            {
                ClearPendingRestart();
                if (!TryCommitRestart(request, frame))
                {
                    FindPauseWindow()?.ShowRestartFailure();
                }
            }
        }

        private static bool IsRestartFrameNewer(PlayerFrame frame, PlayerFrame baseline)
        {
            if (frame.Provenance.HasSourceSequence && baseline.Provenance.HasSourceSequence)
            {
                return frame.Provenance.SourceSequence > baseline.Provenance.SourceSequence;
            }

            return frame.FrameId > baseline.FrameId ||
                   (frame.FrameId == baseline.FrameId && frame.TimestampSec > baseline.TimestampSec);
        }

        private bool TryCommitRestart(BattleStartupRequest request, PlayerFrame playerFrame)
        {
            if (!TryStartRestartRound(request, playerFrame, out var roster))
            {
                ClearLoadingTransition(BattleSceneLoadingTransition.RestartBattle);
                return false;
            }

            CGameProcedure.s_ProcLobby.StoreBattleStartRequest(request, roster);
            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingEnterBattle));
            loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginRestartCurrentBattleAfterLoading);
            return true;
        }

        private static tower_defend_pause_wnd FindPauseWindow()
        {
            return UIManager.GetFirstWindow(nameof(tower_defend_pause_wnd)) as tower_defend_pause_wnd;
        }

        private void ClearPendingRestart()
        {
            if (m_PendingRestartRequest != null)
            {
                m_PendingRestartRequest = null;
                m_RestartFrameBaseline = null;
                FindPauseWindow()?.SetRestartFrameCheckPending(false);
            }
        }

        public void ReturnToLobbyFromBattle()
        {
            if (m_IsAwaitingBattleResult)
            {
                return;
            }

            if (m_PendingRestartRequest != null)
            {
                ClearPendingRestart();
                ClearLoadingTransition(BattleSceneLoadingTransition.RestartBattle);
            }

            if (!TryBeginLoadingTransition(BattleSceneLoadingTransition.ReturnLobby))
            {
                return;
            }

            if (!TryLeaveBattleSession("battle-return"))
            {
                ClearLoadingTransition(BattleSceneLoadingTransition.ReturnLobby);
                return;
            }

            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingReturnLobby));
            loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginReturnToLobbyAfterLoading);
        }

        private void EnterLobbyProcedure()
        {
            GameProductUiAdapter.SuspendCamera("battle-to-lobby");
            CGameProcedure.SetNextProc(CGameProcedure.s_ProcLobby);
            CGameProcedure.s_ProcLobby.SetStatus((int)LobbyStatus.LoadLobbyScene);
        }

        private void PrepareStartBattleLogic()
        {
            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingEnterBattle));
            if (!loading_wnd.HasWnd())
            {
                m_Status = (int)SceneStatus.WaitingStartBattleLogic;
                loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginBattleSceneLoadingAfterLoading);
                return;
            }

            BeginBattleSceneLoadingAfterLoading();
        }

        private void BeginBattleSceneLoadingAfterLoading()
        {
            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingEnterBattle));
            m_BattleStartIssuedFrame = -1;
            m_BattlePrewarmStartedAt = -1f;
            m_LoadingBattleRequest = null;
            m_IsAwaitingBattleResult = false;
            OnStartBattle();
            m_Status = (int)SceneStatus.LoadingBattle;
        }

        private void RecoverFromBattleLoadingFailure(string reason)
        {
            Debug.LogError("[塔防入战] " + reason + "，返回大厅。status=" + (SceneStatus)m_Status +
                           " transition=" + m_LoadingTransition);
            m_Status = (int)SceneStatus.None;
            m_LoadingTransition = BattleSceneLoadingTransition.None;
            var failedRequest = m_LoadingBattleRequest;
            m_LoadingBattleRequest = null;
            if (failedRequest?.m_FrameworkSessionToken >= 0)
            {
                if (YoudooAPI.TryGetSessionSnapshot(out var session) &&
                    session.SessionToken == failedRequest.m_FrameworkSessionToken &&
                    session.RoundSnapshot?.RoundId == failedRequest.m_FrameworkRoundId)
                {
                    if (!GameFrameworkBootstrap.TryLeaveSession("battle-load-failed", expectedToken: session.SessionToken))
                    {
                        Debug.LogError("[塔防入战] 加载中止后无法结束 SDK 会话。session=" + session.SessionToken +
                                       " round=" + session.RoundSnapshot?.RoundId + " phase=" + session.Phase);
                    }
                }
                else
                {
                    Debug.LogWarning("[塔防入战] 加载中止的回合已失效。session=" + failedRequest.m_FrameworkSessionToken +
                                     " round=" + failedRequest.m_FrameworkRoundId);
                }
                if (!CGameProcedure.s_ProcLobby.TryQueuePrepareRequest(failedRequest, out var error))
                {
                    Debug.LogWarning("[塔防入战] 无法返回准备界面。error=" + error);
                }
            }

            CloseBattleTransitionWindowsImmediately();
            LobbyPlayer.GetInstance().SetPlayerState(PlayerState.Lobby);
            CBattleLogic.GetInstance().StopBattleLogic();
            EnterLobbyProcedure();
            loading_wnd.SetLoadingInfo(0f, RenderAPI.GetTextByLanId("td_loading_battle_failed_return"));
        }

        private void BeginRestartCurrentBattleAfterLoading()
        {
            if (!IsLoadingTransition(BattleSceneLoadingTransition.RestartBattle))
            {
                return;
            }

            ClearLoadingTransition(BattleSceneLoadingTransition.RestartBattle);
            CloseBattleTransitionWindowsImmediately();
            CBattleLogic.GetInstance().StopBattleLogic();
            m_Status = (int)SceneStatus.StartBattleLogic;
        }

        private void BeginReturnToLobbyAfterLoading()
        {
            if (!IsLoadingTransition(BattleSceneLoadingTransition.ReturnLobby))
            {
                return;
            }

            ClearLoadingTransition(BattleSceneLoadingTransition.ReturnLobby);
            CloseBattleTransitionWindowsImmediately();
            LobbyPlayer.GetInstance().SetPlayerState(PlayerState.Lobby);
            CBattleLogic.GetInstance().StopBattleLogic();
            EnterLobbyProcedure();
        }

        private void BeginBattleResultPrepareAfterLoading()
        {
            if (!IsLoadingTransition(BattleSceneLoadingTransition.BattleResultPrepare))
            {
                return;
            }

            ClearLoadingTransition(BattleSceneLoadingTransition.BattleResultPrepare);
            CloseBattleTransitionWindowsImmediately();
            LobbyPlayer.GetInstance().SetPlayerState(PlayerState.Lobby);
            CBattleLogic.GetInstance().StopBattleLogic();
            EnterLobbyProcedure();
        }

        private bool TryBeginLoadingTransition(BattleSceneLoadingTransition transition)
        {
            if (m_LoadingTransition != BattleSceneLoadingTransition.None || loading_wnd.IsBlockingFlowInput())
            {
                return false;
            }

            m_LoadingTransition = transition;
            return true;
        }

        private bool IsLoadingTransition(BattleSceneLoadingTransition transition)
        {
            return m_LoadingTransition == transition;
        }

        private void ClearLoadingTransition(BattleSceneLoadingTransition transition)
        {
            if (m_LoadingTransition == transition)
            {
                m_LoadingTransition = BattleSceneLoadingTransition.None;
            }
        }

        private void CloseBattleTransitionWindowsImmediately()
        {
            CloseWindowImmediately(nameof(tower_defend_chapter_result_wnd));
            CloseWindowImmediately(nameof(tower_defend_endless_result_wnd));
            CloseWindowImmediately(nameof(tower_defend_pause_wnd));
            CloseWindowImmediately(nameof(tower_defend_battle_hud_wnd));
            CloseWindowImmediately(nameof(tower_defend_wave_notice_wnd));
        }

        private static void CloseWindowImmediately(string windowName)
        {
            var window = UIManager.GetFirstWindow(windowName);
            if (window != null)
            {
                UIManager.CloseWindow(window, true);
            }
        }

        public override void BackLogin()
        {
            if (m_Status == (int)SceneStatus.GoingLogin || m_IsAwaitingBattleResult ||
                m_LoadingTransition != BattleSceneLoadingTransition.None || loading_wnd.IsBlockingFlowInput())
            {
                return;
            }

            var activeWindow = UIManager.GetCurrentActiveWindow();
            if (activeWindow is tower_defend_setting_wnd &&
                UIManager.GetFirstWindow(nameof(tower_defend_pause_wnd)) != null)
            {
                UIManager.CloseWindow(activeWindow);
                return;
            }

            if (activeWindow is tower_defend_pause_wnd pauseWindow)
            {
                pauseWindow.OnClickResume();
                return;
            }

            if (!TryReturnCurrentResultWindow() && !RenderEvent.Event.OnTowerDefendPauseRequest())
            {
                Debug.LogWarning("[塔防返回键] 当前状态无法打开暂停窗。sceneStatus=" + (SceneStatus)m_Status);
            }
        }

        protected override void OnEscape(InputAction.CallbackContext context)
        {
            BackLogin();
        }

        protected override void OnInput(InputAction.CallbackContext context, InputType inputType)
        {
            throw new NotImplementedException();
        }
    }
}
