using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameDll;

namespace GameHot
{
    public class tower_defend_pause_model : WindowModel
    {
        public Action m_OnResume;
        public Action m_OnRestart;
        public Action m_OnReturn;

        public override void Clear()
        {
            m_OnResume = null;
            m_OnRestart = null;
            m_OnReturn = null;
        }
    }

    public class tower_defend_pause_wnd : WindowBase
    {
        private const string m_LanTdPauseTitle = "td_pause_title";
        private const string m_LanTdPauseSummary = "td_pause_summary";
        private v_tower_defend_pause_wnd m_View;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.battle_result;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);
            __CreateModel(new tower_defend_pause_model());
        }


        public void SetCallbacks(Action onResume, Action onRestart, Action onReturn)
        {
            var model = GetModel<tower_defend_pause_model>();
            model.m_OnResume = onResume;
            model.m_OnRestart = onRestart;
            model.m_OnReturn = onReturn;

            if (IsInitializedView())
            {
                RefreshView();
            }
        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_pause_wnd();
            m_View.InitComponent(__GetWindowObj());
            RenderAPI.AddButtonClick(m_View.m_btn_resume, OnClickResume);
            RenderAPI.AddButtonClick(m_View.m_btn_restart, OnClickRestart);
            RenderAPI.AddButtonClick(m_View.m_btn_return, OnClickReturn);
        }

        protected override void OnOpen()
        {
            RefreshView();
        }

        private void OnClickResume()
        {
            var model = GetModel<tower_defend_pause_model>();
            model.m_OnResume?.Invoke();
            UIManager.CloseWindow(this);
        }

        private void OnClickRestart()
        {
            var model = GetModel<tower_defend_pause_model>();
            model.m_OnRestart?.Invoke();
            CloseWithLoadingDelayIfNeeded();
        }

        private void OnClickReturn()
        {
            var model = GetModel<tower_defend_pause_model>();
            model.m_OnReturn?.Invoke();
            CloseWithLoadingDelayIfNeeded();
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

        private void RefreshView()
        {
            if (!IsInitializedView())
            {
                return;
            }

            RenderAPI.SetTextLan(m_View.m_txt_title, m_LanTdPauseTitle);
            RenderAPI.SetTextLan(m_View.m_txt_summary, m_LanTdPauseSummary);
        }
    }
}
