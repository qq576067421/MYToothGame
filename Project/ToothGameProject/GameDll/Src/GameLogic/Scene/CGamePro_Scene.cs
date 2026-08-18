using GameDll;
using LCL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public interface ITowerDefendSdkBridge
    {
        TowerDefendLeaderboardSubmissionData SubmitEndlessLeaderboard(BattleResultData result);
    }
    public sealed class TowerDefendSdkBridgeStub : ITowerDefendSdkBridge
    {
        public TowerDefendLeaderboardSubmissionData SubmitEndlessLeaderboard(BattleResultData result)
        {
            return new TowerDefendLeaderboardSubmissionData
            {
                m_State = TowerDefendLeaderboardSubmissionState.PendingSdk,
                m_Score = result != null ? Mathf.Max(0, result.m_BestProgressWave) : 0,
            };
        }
    }
    public static class TowerDefendSdkBridge
    {
        private static ITowerDefendSdkBridge m_Current = new TowerDefendSdkBridgeStub();

        public static ITowerDefendSdkBridge Current
        {
            get
            {
                return m_Current;
            }
            set
            {
                m_Current = value ?? new TowerDefendSdkBridgeStub();
            }
        }
    }
    public class CGamePro_Scene : CGameProcedure
    {
        private enum BattleSceneLoadingTransition
        {
            None = 0,
            RestartBattle = 1,
            ReturnLobby = 2,
            BackLogin = 3,
            BattleResultPrepare = 4,
        }

        private const float m_BattleLoadingMinVisibleSeconds = 2.5f;
        private const float m_ResultGestureMinConfidence = 0.3f;
        private const float m_ResultGestureOverheadMarginRatio = 0.05f;
        private const int m_ResultGestureRequiredFrames = 15;
        private const string m_LanTdLoadingEnterBattle = "td_loading_enter_battle";
        private const string m_LanTdLoadingReturnLobby = "td_loading_return_lobby";
        private int m_BattleStartIssuedFrame = -1;
        private BattleSceneLoadingTransition m_LoadingTransition = BattleSceneLoadingTransition.None;
        private int m_ResultGestureFrameCount;
        private int m_ResultGestureLastFrameSerial = -1;
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
            RenderEvent.Event.OnTowerDefendRestartBattleRequest += RestartCurrentBattle;
            RenderEvent.Event.OnTowerDefendReturnLobbyRequest += ReturnToLobbyFromBattle;
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
                        // 正式进入战斗前必须等场景解析和启动预热都完成，避免加载界面遮挡期间提前跑喊话或刷怪流程。
                        if (battle != null && battle.IsLoaded() && battle.ReadIsBattleStartLoadingReady())
                        {
                            battle.SetSceneStatus((int)ESceneState.StartGame);
                            m_BattleStartIssuedFrame = Time.frameCount;
                            m_Status = (int)SceneStatus.WaitingBattleStartVisualReady;
                        }

                        break;
                    }
                case SceneStatus.WaitingBattleStartVisualReady:
                    {
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
                        var battle = CBattleLogic.GetInstance().GetScene();
                        if (battle != null && battle.TryConsumeResult(out var result))
                        {
                            OnFinishBattle(result);
                        }

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
            m_LoadingTransition = BattleSceneLoadingTransition.None;
            Event.OnEscapPressed -= OnEscape;
            Event.OnInputAction -= OnInput;
            RenderEvent.Event.OnTowerDefendBattleHudOpenRequest -= OnTowerDefendBattleHudOpenRequest;
            RenderEvent.Event.OnTowerDefendBattleHudCloseRequest -= OnTowerDefendBattleHudCloseRequest;
            RenderEvent.Event.OnTowerDefendPauseOpenRequest -= OnTowerDefendPauseOpenRequest;
            RenderEvent.Event.OnTowerDefendPauseCloseRequest -= OnTowerDefendPauseCloseRequest;
            RenderEvent.Event.OnTowerDefendRestartBattleRequest -= RestartCurrentBattle;
            RenderEvent.Event.OnTowerDefendReturnLobbyRequest -= ReturnToLobbyFromBattle;
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
            if (!CGameProcedure.s_ProcLobby.TryConsumeBattleStartRequest(out var request))
            {
                Debug.LogError("无法进入战斗：大厅没有提交正式的战斗启动请求。请先从大厅流程重新发起战斗。");
                return null;
            }

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

        private void OnFinishBattle(BattleResultData result)
        {
            var isWin = result.m_WinGroup == GroupId.GuardGroupId;
            var resultText = isWin ? "战斗胜利" : "战斗失败";
            if (result != null && result.m_GameMode == BattleGameMode.Endless)
            {
                result.m_LeaderboardSubmission = TowerDefendSdkBridge.Current.SubmitEndlessLeaderboard(result);
            }
            Debug.Log(string.Format(
                "Scene battle finished. Result={0}, Reason={1}, WinGroup={2}, Stage={3}, UseTime={4}, FightId={5}",
                resultText,
                result.m_FinishReason,
                result.m_WinGroup,
                result.m_Stage,
                result.m_UseTime,
                result.m_FightId));

            ApplyBattleResultToLocalProfile(result);
            ShowBattleResultInScene(result);
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
            if (m_ResultGestureConsumed ||
                m_LoadingTransition != BattleSceneLoadingTransition.None ||
                loading_wnd.IsBlockingFlowInput())
            {
                return;
            }

            if (!TryReadCurrentResultContinueTarget(out var continueAction))
            {
                ResetBattleResultGestureState();
                return;
            }

            if (!TryReadLatestBattleBoneFrameData(out var frameData))
            {
                m_ResultGestureFrameCount = 0;
                return;
            }

            if (frameData.m_FrameSerial == m_ResultGestureLastFrameSerial)
            {
                return;
            }

            m_ResultGestureLastFrameSerial = frameData.m_FrameSerial;
            if (!ReadHasResultReadyGesture(frameData))
            {
                m_ResultGestureFrameCount = 0;
                return;
            }

            m_ResultGestureFrameCount++;
            if (m_ResultGestureFrameCount < m_ResultGestureRequiredFrames)
            {
                return;
            }

            m_ResultGestureConsumed = continueAction();
            m_ResultGestureFrameCount = 0;
        }

        private static bool TryReadLatestBattleBoneFrameData(out BoneFrameData frameData)
        {
            frameData = null;
            var boneParseData = GameObject.FindFirstObjectByType<BattleBoneParseData>();
            if (boneParseData == null)
            {
                return false;
            }

            frameData = boneParseData.ReadLatestFrameData();
            return frameData != null && frameData.m_HasFrameData;
        }

        private static bool TryReadCurrentResultContinueTarget(out Func<bool> continueAction)
        {
            continueAction = null;
            var activeWindow = UIManager.GetCurrentActiveWindow();
            if (activeWindow is tower_defend_chapter_result_wnd chapterResultWnd)
            {
                continueAction = chapterResultWnd.TryContinueByGesture;
                return true;
            }

            if (activeWindow is tower_defend_endless_result_wnd endlessResultWnd)
            {
                continueAction = endlessResultWnd.TryContinueByGesture;
                return true;
            }

            return false;
        }

        private static bool TryReturnCurrentResultWindow()
        {
            var activeWindow = UIManager.GetCurrentActiveWindow();
            if (activeWindow is tower_defend_chapter_result_wnd chapterResultWnd)
            {
                return chapterResultWnd.TryReturnByRemote();
            }

            if (activeWindow is tower_defend_endless_result_wnd endlessResultWnd)
            {
                return endlessResultWnd.TryReturnByRemote();
            }

            return false;
        }

        private static bool ReadHasResultReadyGesture(BoneFrameData frameData)
        {
            if (frameData == null || !frameData.m_HasFrameData || frameData.m_Persons == null)
            {
                return false;
            }

            for (int i = 0; i < frameData.m_Persons.Count; i++)
            {
                if (ReadIsResultReadyPose(frameData.m_Persons[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ReadIsResultReadyPose(BonePersonData person)
        {
            if (!TryReadResultGestureMetrics(person, out var shoulderWidth, out var headTopY))
            {
                return false;
            }

            var threshold = headTopY - shoulderWidth * m_ResultGestureOverheadMarginRatio;
            return ReadIsTrackedJointOverY(person, YouDooSDKConstants.KeyPointIndex.Leftwrist, threshold) ||
                   ReadIsTrackedJointOverY(person, YouDooSDKConstants.KeyPointIndex.Rightwrist, threshold);
        }

        private static bool TryReadResultGestureMetrics(BonePersonData person, out float shoulderWidth, out float headTopY)
        {
            shoulderWidth = 0f;
            headTopY = 0f;
            if (!TryReadTrackedBodyJoint(person, YouDooSDKConstants.KeyPointIndex.Leftshoulder, out var leftShoulder) ||
                !TryReadTrackedBodyJoint(person, YouDooSDKConstants.KeyPointIndex.Rightshoulder, out var rightShoulder))
            {
                return false;
            }

            shoulderWidth = Mathf.Abs(leftShoulder.m_X - rightShoulder.m_X);
            if (shoulderWidth <= 0f)
            {
                return false;
            }

            var hasHeadTop = false;
            TryMergeHeadTopY(person, YouDooSDKConstants.KeyPointIndex.Nose, ref headTopY, ref hasHeadTop);
            TryMergeHeadTopY(person, YouDooSDKConstants.KeyPointIndex.Lefteye, ref headTopY, ref hasHeadTop);
            TryMergeHeadTopY(person, YouDooSDKConstants.KeyPointIndex.Righteye, ref headTopY, ref hasHeadTop);
            return hasHeadTop;
        }

        private static void TryMergeHeadTopY(
            BonePersonData person,
            YouDooSDKConstants.KeyPointIndex jointIndex,
            ref float headTopY,
            ref bool hasHeadTop)
        {
            if (!TryReadTrackedBodyJoint(person, jointIndex, out var joint))
            {
                return;
            }

            headTopY = hasHeadTop ? Mathf.Min(headTopY, joint.m_Y) : joint.m_Y;
            hasHeadTop = true;
        }

        private static bool ReadIsTrackedJointOverY(
            BonePersonData person,
            YouDooSDKConstants.KeyPointIndex jointIndex,
            float thresholdY)
        {
            return TryReadTrackedBodyJoint(person, jointIndex, out var joint) &&
                   joint.m_Y <= thresholdY;
        }

        private static bool TryReadTrackedBodyJoint(
            BonePersonData person,
            YouDooSDKConstants.KeyPointIndex jointIndex,
            out BoneJointData joint)
        {
            joint = null;
            var body = person != null ? person.m_Body : null;
            var joints = body != null ? body.m_Joints : null;
            var index = (int)jointIndex;
            if (joints == null || index < 0 || index >= joints.Length)
            {
                return false;
            }

            joint = joints[index];
            return joint != null &&
                   joint.m_IsTracked &&
                   joint.m_Score >= m_ResultGestureMinConfidence;
        }

        private void ResetBattleResultGestureState()
        {
            m_ResultGestureFrameCount = 0;
            m_ResultGestureLastFrameSerial = -1;
            m_ResultGestureConsumed = false;
        }

        private void HandleBattleResultReturn(BattleResultData result)
        {
            if (!TryBeginLoadingTransition(BattleSceneLoadingTransition.ReturnLobby))
            {
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

                int nextStage = currentStage + 1;
                return TowerDefendStageConfigResolver.Exists(nextStage, BattleGameMode.Chapter)
                    ? nextStage
                    : currentStage;
            }

            // 无尽模式没有下一关概念，结算继续只是回到准备匹配后重开同一入口。
            return lastRequest.m_StageId > 0 ? lastRequest.m_StageId : result.m_Stage;
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

            if (result.m_GameMode == BattleGameMode.Chapter &&
                result.m_WinGroup == GroupId.GuardGroupId)
            {
                result.m_UnlockedStage = Mathf.Max(profile.GetUnlockedStage(), result.m_Stage + 1);
                profile.SetUnlockedStage(result.m_UnlockedStage);
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
            var request = CGameProcedure.s_ProcLobby.GetLastBattleStartRequestClone();
            if (request == null)
            {
                return;
            }

            if (!TryBeginLoadingTransition(BattleSceneLoadingTransition.RestartBattle))
            {
                return;
            }

            string error;
            if (!CGameProcedure.s_ProcLobby.TryQueueBattleStartRequest(request, out error))
            {
                ClearLoadingTransition(BattleSceneLoadingTransition.RestartBattle);
                Debug.LogError("无法重新开始战斗：" + error);
                return;
            }

            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingEnterBattle));
            loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginRestartCurrentBattleAfterLoading);
        }

        public void ReturnToLobbyFromBattle()
        {
            if (!TryBeginLoadingTransition(BattleSceneLoadingTransition.ReturnLobby))
            {
                return;
            }

            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingReturnLobby));
            loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginReturnToLobbyAfterLoading);
        }

        private void EnterLobbyProcedure()
        {
            CGameProcedure.SetNextProc(CGameProcedure.s_ProcLobby);
            CGameProcedure.s_ProcLobby.SetStatus((int)LobbyStatus.LoadLobbyScene);
        }

        private void PrepareStartBattleLogic()
        {
            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingEnterBattle));
            if (!loading_wnd.HasWnd())
            {
                loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginBattleSceneLoadingAfterLoading);
                m_Status = (int)SceneStatus.WaitingStartBattleLogic;
                return;
            }

            BeginBattleSceneLoadingAfterLoading();
        }

        private void BeginBattleSceneLoadingAfterLoading()
        {
            loading_wnd.SetLoadingInfo(0.0f, RenderAPI.GetTextByLanId(m_LanTdLoadingEnterBattle));
            m_BattleStartIssuedFrame = -1;
            OnStartBattle();
            m_Status = (int)SceneStatus.LoadingBattle;
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
            //m_LoadingTransition = BattleSceneLoadingTransition.None;
            //UIManager.CloseWindow(UIManager.GetCurrentActiveWindow());
            //m_Status = (int)SceneStatus.GoingLogin;
            //CBattleLogic.GetInstance().StopBattleLogic();
            //BackLoginCommon();

            UIManager.OpenWindowEX<tower_defend_setting_wnd>(null);
            //CGameProcedure.SetNextProc(CGameProcedure.s_ProcLobby);
            //CGameProcedure.s_ProcLobby.SetStatus((int)LoginStatus.EnterLoginScene);
        }

        protected override void OnEscape(InputAction.CallbackContext context)
        {
            if (m_LoadingTransition != BattleSceneLoadingTransition.None || loading_wnd.IsBlockingFlowInput())
            {
                return;
            }

            // 结算界面的遥控返回必须与 btn_return 完全一致，不能打开战斗暂停界面。
            if (TryReturnCurrentResultWindow())
            {
                return;
            }

            if (!TryBeginLoadingTransition(BattleSceneLoadingTransition.BackLogin))
            {
                return;
            }

            //loading_wnd.OpenLoading(m_BattleLoadingMinVisibleSeconds, BeginBackLoginAfterLoading);
            BeginBackLoginAfterLoading();
        }

        private void BeginBackLoginAfterLoading()
        {
            if (!IsLoadingTransition(BattleSceneLoadingTransition.BackLogin))
            {
                return;
            }

            ClearLoadingTransition(BattleSceneLoadingTransition.BackLogin);
            BackLogin();
        }

        protected override void OnInput(InputAction.CallbackContext context, InputType inputType)
        {
            throw new NotImplementedException();
        }
    }
}
