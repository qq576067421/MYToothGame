using GameDll;
using System;
using LCL;

namespace GameHot
{
    public class tower_defend_pause_wnd : WindowBase
    {
        private v_tower_defend_pause_wnd m_View;
        private bool m_ShouldResumeBattleOnClose;
        private bool m_IsBattleTransitionRequested;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.lobby;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);

        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_pause_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_luibutton_filled_1, OnClickResume);
            RenderAPI.AddButtonClick(m_View.m_luibutton_filled_2, OnClickRestart);
            RenderAPI.AddButtonClick(m_View.m_luibutton_filled_3, OnClickReturnLobby);
            RenderAPI.AddButtonClick(m_View.m_luibutton_filled_4, OnClickClose);
            //RenderAPI.AddButtonClick(m_View.m_Pause_Btn, OnClickResume);
            OnInitLanguage();
        }

        private void OnClickResume()
        {
            ForceResumeBattle();
            UIManager.CloseWindow(this);
        }

        private void OnClickRestart()
        {
            RequestBattleTransition(RenderEvent.Event.OnTowerDefendRestartBattleRequest);
        }

        private void OnClickReturnLobby()
        {
            RequestBattleTransition(RenderEvent.Event.OnTowerDefendReturnLobbyRequest);
        }

        private void OnClickClose()
        {
            UIManager.CloseWindow(this);
        }

        protected override void OnOpen()
        {
            m_IsBattleTransitionRequested = false;
            m_ShouldResumeBattleOnClose = false;
            var battleLogic = CBattleLogic.GetInstance();
            // 只有本窗口把战斗从运行切到暂停时，普通关闭才负责恢复。
            if (!battleLogic.IsTowerDefendBattlePaused())
            {
                m_ShouldResumeBattleOnClose = battleLogic.SetTowerDefendBattlePause(true);
            }

            RenderEvent.Event.OnUpdateSelectionVisuals += OnUpdateSelectionVisuals;
        }

        protected override void OnClose()
        {
            RenderEvent.Event.OnUpdateSelectionVisuals -= OnUpdateSelectionVisuals;
            // 重开或返回大厅已经交给加载过渡流程处理，此时不能恢复旧战斗。
            if (!m_IsBattleTransitionRequested)
            {
                ResumeBattleIfNeeded();
            }

            m_ShouldResumeBattleOnClose = false;
            m_IsBattleTransitionRequested = false;
        }
        private void OnInitLanguage()
        {
            RenderAPI.SetTextLan(m_View.m_stop_txt, "td_pause_title");
            RenderAPI.SetTextLan(m_View.m_continue_txt, "td_pause_continue");
            RenderAPI.SetTextLan(m_View.m_continue_choose_txt, "td_pause_continue");
            RenderAPI.SetTextLan(m_View.m_startOver_txt, "td_pause_start_over");
            RenderAPI.SetTextLan(m_View.m_startOver_choose_txt, "td_pause_start_over");
            RenderAPI.SetTextLan(m_View.m_returnLobby_txt, "td_pause_return_lobby");
            RenderAPI.SetTextLan(m_View.m_returnLobby_choose_txt, "td_pause_return_lobby");
            RenderAPI.SetTextLan(m_View.m_gameSetting_txt, "td_pause_game_setting");
            RenderAPI.SetTextLan(m_View.m_gameSetting_choose_txt, "td_pause_game_setting");
        }
        private void ForceResumeBattle()
        {
            CBattleLogic.GetInstance().ResumeTowerDefendBattle();
            m_ShouldResumeBattleOnClose = false;
        }

        private void ResumeBattleIfNeeded()
        {
            if (!m_ShouldResumeBattleOnClose)
            {
                return;
            }

            CBattleLogic.GetInstance().ResumeTowerDefendBattle();
            m_ShouldResumeBattleOnClose = false;
        }

        private void RequestBattleTransition(Action request)
        {
            var hadLoading = loading_wnd.HasWnd();
            request?.Invoke();
            if (hadLoading || loading_wnd.HasWnd())
            {
                m_IsBattleTransitionRequested = true;
                CloseWithLoadingDelayIfNeeded();
                return;
            }

            UIManager.CloseWindow(this);
        }

        private void CloseWithLoadingDelayIfNeeded()
        {
            var delayMs = loading_wnd.GetRemainingVisibleMilliseconds();
            if (delayMs > 0)
            {
                AddCounter(delayMs, 1, null, 0, () =>
                {
                    UIManager.CloseWindow(this);
                });
                return;
            }

            UIManager.CloseWindow(this);
        }

        private void OnUpdateSelectionVisuals()
        {
            var active = UIManager.GetCurrentActiveWindow();
            if (active == null || active != this)
            {
                return;
            }
            AudioPlay2D();
        }
        private void AudioPlay2D()
        {
            AudioManager.GetInstance().Play2D(4);
        }
    }
}
