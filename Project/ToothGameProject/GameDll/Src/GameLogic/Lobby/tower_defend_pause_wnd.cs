using System;
using GameDll;

namespace GameHot
{
    public class tower_defend_pause_wnd : WindowBase
    {
        private v_tower_defend_pause_wnd m_View;
        private Action m_OnResume;
        private Action m_OnRestart;
        private Action m_OnReturn;
        private TowerDefendResumeStatus m_ResumeStatus;
        private bool m_IsRestartFrameCheckPending;
        private bool IsWaitingForTracking =>
            m_IsRestartFrameCheckPending || m_ResumeStatus == TowerDefendResumeStatus.WaitingForTracking;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.lobby;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);
        }

        public void SetCallbacks(Action onResume, Action onRestart, Action onReturn)
        {
            m_OnResume = onResume;
            m_OnRestart = onRestart;
            m_OnReturn = onReturn;
        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_pause_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_luibutton_filled_1, OnClickResume);
            RenderAPI.AddButtonClick(m_View.m_luibutton_filled_2, OnClickRestart);
            RenderAPI.AddButtonClick(m_View.m_luibutton_filled_3, () => RequestTransition(m_OnReturn, "td_pause_return_failed"));
            RenderAPI.AddButtonClick(m_View.m_luibutton_filled_4, OnClickSettings);
        }

        protected override void OnOpen()
        {
            OnInitLanguage();
            RefreshResumeStatus();
            RenderEvent.Event.OnUpdateSelectionVisuals += OnUpdateSelectionVisuals;
        }

        protected override void OnClose()
        {
            RenderEvent.Event.OnUpdateSelectionVisuals -= OnUpdateSelectionVisuals;
            m_OnResume = null;
            m_OnRestart = null;
            m_OnReturn = null;
            m_ResumeStatus = TowerDefendResumeStatus.Ready;
            m_IsRestartFrameCheckPending = false;
        }

        internal void SetRestartFrameCheckPending(bool pending)
        {
            m_IsRestartFrameCheckPending = pending;
            RefreshResumeStatus();
        }

        internal void ShowRestartFailure()
        {
            if (__IsLogicOpen())
            {
                tip_wnd.GetInstance().OnShowTipLanId("td_pause_restart_failed");
            }
        }

        public void SetResumeStatus(TowerDefendResumeStatus status)
        {
            m_ResumeStatus = status;
            RefreshResumeStatus();
            if (status == TowerDefendResumeStatus.TimedOut && __IsLogicOpen())
            {
                tip_wnd.GetInstance().OnShowTipLanId("td_pause_resume_failed");
            }
        }

        private void RefreshResumeStatus()
        {
            if (m_View == null)
            {
                return;
            }

            bool waiting = IsWaitingForTracking;
            m_View.m_luibutton_filled_1.interactable = !waiting;
            m_View.m_luibutton_filled_2.interactable = !waiting;
            m_View.m_luibutton_filled_4.interactable = !m_IsRestartFrameCheckPending;
            RenderAPI.SetTextLan(m_View.m_stop_txt, waiting ? "td_pause_resume_tracking" : "td_pause_title");
        }

        public void OnClickResume()
        {
            if (!__IsLogicOpen() || loading_wnd.IsBlockingFlowInput() || IsWaitingForTracking)
            {
                return;
            }

            m_OnResume?.Invoke();
            if (__IsLogicOpen() && !IsWaitingForTracking &&
                RenderEvent.Event.OnTowerDefendBattlePauseStateQuery())
            {
                tip_wnd.GetInstance().OnShowTipLanId("td_pause_resume_failed");
            }
        }

        private void OnClickRestart()
        {
            if (!__IsLogicOpen() || loading_wnd.IsBlockingFlowInput() || IsWaitingForTracking)
            {
                return;
            }

            m_OnRestart?.Invoke();
            if (!m_IsRestartFrameCheckPending && !loading_wnd.IsBlockingFlowInput())
            {
                ShowRestartFailure();
            }
        }

        private void RequestTransition(Action request, string failureLanId)
        {
            if (!__IsLogicOpen() || loading_wnd.IsBlockingFlowInput())
            {
                return;
            }

            request?.Invoke();
            if (__IsLogicOpen() && !loading_wnd.IsBlockingFlowInput())
            {
                tip_wnd.GetInstance().OnShowTipLanId(failureLanId);
            }
        }

        private void OnClickSettings()
        {
            if (__IsLogicOpen() && !loading_wnd.IsBlockingFlowInput() && !m_IsRestartFrameCheckPending)
            {
                UIManager.OpenWindowEX<tower_defend_setting_wnd>(this);
            }
        }

        private void OnInitLanguage()
        {
            RenderAPI.SetTextLan(m_View.m_continue_txt, "td_pause_continue");
            RenderAPI.SetTextLan(m_View.m_continue_choose_txt, "td_pause_continue");
            RenderAPI.SetTextLan(m_View.m_startOver_txt, "td_pause_start_over");
            RenderAPI.SetTextLan(m_View.m_startOver_choose_txt, "td_pause_start_over");
            RenderAPI.SetTextLan(m_View.m_returnLobby_txt, "td_pause_return_lobby");
            RenderAPI.SetTextLan(m_View.m_returnLobby_choose_txt, "td_pause_return_lobby");
            RenderAPI.SetTextLan(m_View.m_gameSetting_txt, "td_pause_game_setting");
            RenderAPI.SetTextLan(m_View.m_gameSetting_choose_txt, "td_pause_game_setting");
        }

        private void OnUpdateSelectionVisuals()
        {
            if (UIManager.GetCurrentActiveWindow() == this)
            {
                AudioManager.GetInstance().Play2D(4);
            }
        }
    }
}
