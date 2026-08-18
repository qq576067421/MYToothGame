using GameDll;
using LCL;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityUI;

namespace GameHot
{
    public class lobby_main_wnd : WindowBase
    {
        private const string m_LanTdLobbyProfile = "td_lobby_profile";
        private const string m_LanTdModeChapter = "td_mode_chapter";
        private const string m_LanTdModeEndless = "td_mode_endless";
        private const string m_LanTdStageLabel = "td_lobby_stage_label";
        private const string m_LanTdEndlessStageLabel = "td_lobby_endless_stage_label";
        private const string m_LanTdStage = "td_lobby_stage";
        private const string m_LanTdStageInvalid = "td_lobby_stage_invalid";
        private const string m_LanTdPlayerLabel = "td_lobby_player_label";
        private const string m_LanTdPlayerCount = "td_lobby_player_count";
        private const int m_LobbyMusicAudioId = 300;
        public v_lobby_main_wnd m_Wnd = null;
        private WindowBase m_PendingMainReadyWindow;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Hold;
            __CustomUIPrefabDir = UIPrefabDirs.lobby;
            __ParticipateCurrentActiveWindow = true;
        }
        protected override void OnInitComponent()
        {
            m_Wnd = new v_lobby_main_wnd();
            m_Wnd.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_Wnd.m_btnModeChapter, () =>
            {
                CGameProcedure.s_ProcLobby.SetSelectedGameMode(BattleGameMode.Chapter);
                OnClickStartBattle();
            });

            RenderAPI.AddButtonClick(m_Wnd.m_btnModeEndless, () =>
            {
                CGameProcedure.s_ProcLobby.SetSelectedGameMode(BattleGameMode.Endless);
                OnClickStartBattle();
            });
            RenderAPI.AddButtonClick(m_Wnd.m_btnToothFactory, () =>
            {
                //OnClickOpenToothFactory();
            });
            RenderAPI.AddButtonClick(m_Wnd.m_btnSetting, () =>
            {
                //OnClickSetting();
            });
        }
        private void OnClickOpenToothFactory()
        {
            UIManager.OpenWindowEX<tower_defend_giftedness_wnd>(null);
        }
        private void OnClickSetting()
        {
            UIManager.OpenWindowEX<tower_defend_setting_wnd>(null);
        }
        private void OnClickStartBattle()
        {
            var request = CGameProcedure.s_ProcLobby.CreateBattleStartupRequestShell();
            if (request == null)
            {
                Debug.LogError("无法打开战斗准备界面：大厅当前选择没有形成有效的战斗启动请求。");
                return;
            }

            var lvChooseWnd = UIManager.OpenWindowEX<tower_defend_lvchoose_wnd>(this);
            lvChooseWnd.SetRequest(
                request,
                () =>
                {
                    if (request.m_GameMode == BattleGameMode.Chapter)
                    {
                        var chapterWnd = UIManager.OpenWindowEX<tower_defend_chapter_wnd>(this);
                        chapterWnd.SetRequest(
                            request,
                            () => OpenPlayerCountWindow(request),
                            null);
                        return chapterWnd;
                    }

                    return OpenPlayerCountWindow(request);
                },
                null);
        }

        private tower_defend_player_count_wnd OpenPlayerCountWindow(BattleStartupRequest request)
        {
            var playerCountWnd = UIManager.OpenWindowEX<tower_defend_player_count_wnd>(this);
            playerCountWnd.SetRequest(
                request,
                () => OpenPrepareWindow(request),
                null);
            return playerCountWnd;
        }

        private tower_defend_prepare_wnd OpenPrepareWindow(BattleStartupRequest request)
        {
            var prepareWnd = UIManager.OpenWindowEX<tower_defend_prepare_wnd>(this);
            prepareWnd.SetRequest(request);
            return prepareWnd;
        }

        private bool TryOpenPendingPrepareRequest()
        {
            BattleStartupRequest request;
            if (!CGameProcedure.s_ProcLobby.TryConsumePendingPrepareRequest(out request))
            {
                return false;
            }

            var prepareWnd = OpenPrepareWindow(request);
            NotifyMainUILoadedAfterWindowReady(prepareWnd);
            return true;
        }

        protected override void OnOpen()
        {
            CGameProcedure.Event.OnMoneyChanged += OnMoneyChanged;
            CGameProcedure.Event.OnMoneyChanged();
            RenderEvent.Event.OnUpdateSelectionVisuals += OnUpdateSelectionVisuals;
            PlayMusic();
            if (TryOpenPendingPrepareRequest())
            {
                return;
            }

            CGameProcedure.s_ProcLobby.OnMainUILoaded();
        }

        protected override void OnClose()
        {
            CGameProcedure.Event.OnMoneyChanged -= OnMoneyChanged;
            RenderEvent.Event.OnUpdateSelectionVisuals -= OnUpdateSelectionVisuals;
            ClearPendingMainReadyWindow();
        }

        private void NotifyMainUILoadedAfterWindowReady(WindowBase targetWindow)
        {
            if (targetWindow == null || IsWindowReady(targetWindow))
            {
                CGameProcedure.s_ProcLobby.OnMainUILoaded();
                return;
            }

            ClearPendingMainReadyWindow();
            m_PendingMainReadyWindow = targetWindow;
            CGameProcedure.Event.OnUIOpenedEvent += OnPendingMainReadyWindowOpened;
        }

        private void OnPendingMainReadyWindowOpened(WindowBase openedWindow)
        {
            if (openedWindow != m_PendingMainReadyWindow)
            {
                return;
            }

            ClearPendingMainReadyWindow();
            CGameProcedure.s_ProcLobby.OnMainUILoaded();
        }

        private void ClearPendingMainReadyWindow()
        {
            if (m_PendingMainReadyWindow == null)
            {
                return;
            }

            CGameProcedure.Event.OnUIOpenedEvent -= OnPendingMainReadyWindowOpened;
            m_PendingMainReadyWindow = null;
        }

        private static bool IsWindowReady(WindowBase window)
        {
            return window != null &&
                window.__IsLogicOpen() &&
                window.__IsObjLoaded() &&
                window.__IsVisiable() &&
                window.__GetWindowStage() != WindowStage.Loading &&
                window.__GetWindowStage() != WindowStage.ReopenPending;
        }

        private void PlayMusic()
        {
            AudioManager.GetInstance().Play2D(
                m_LobbyMusicAudioId,
                AudioTransitionMode.CrossFade,
                -1f,
                AudioReplayMode.KeepCurrent,
                AudioLifetime.Persistent);
        }

        private void OnUpdateSelectionVisuals()
        {
            var active = UIManager.GetCurrentActiveWindow();
            if (active == null || active != this)
            {
                return;
            }
            AudioManager.GetInstance().Play2D(4);
            Debug.Log("播放lobby_main_wnd 4 音效");
        }



        private void OnMoneyChanged()
        {
            m_Wnd.m_txt_coin.text = LobbyPlayer.GetInstance().m_PlayerInfo.GetMoney(MoneyId.CoinId).ToString();
        }

    }
}
