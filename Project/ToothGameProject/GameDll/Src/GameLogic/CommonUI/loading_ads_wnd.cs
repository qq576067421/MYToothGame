using LCL;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using GameDll;

namespace GameHot
{
    public class loading_ads_wnd : WindowBase
    {
        private v_loading_ads_wnd m_Wnd = null;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Loading;
            __CustomUIPrefabDir = UIPrefabDirs.common;
        }
        protected override void OnInitComponent()
        {
            m_Wnd = new v_loading_ads_wnd();
            m_Wnd.InitComponent(__GetWindowObj());
        }

        private long m_TimerId = 0;
        private Action<bool> m_PlayCall;
        protected override void OnOpen()
        {
            m_PlayCall = (Action<bool>)__GetUserData()[0];
            m_Time = 0;
            RenderAPI.SetActive(m_Wnd.m_btnCancel, false);

            RenderAPI.SetTextLan(m_Wnd.m_loadtip, "load_ads_time", m_Time);
            m_TimerId = CounterManager.GetInstance().AddCounter(500, -1, OnTimer);

        }
        private float m_Time =0;
        private void OnTimer()
        {
            m_Time += 0.5f;
            int time = (int)m_Time;
            RenderAPI.SetTextLan(m_Wnd.m_loadtip, "load_ads_time", time);
            if (m_Time >= 5)
            {
                RenderAPI.SetActive(m_Wnd.m_btnCancel, true);
                RenderAPI.AddButtonClick(m_Wnd.m_btnCancel, () =>
                {
                    m_PlayCall(false);
                    UIManager.CloseWindow(this);
                });
            }

            //if(RenderAPI.IsAdsReady())
            //{
            //    m_PlayCall(true);
            //    UIManager.CloseWindow(this);
            //}
        }

        private void StopTimer()
        {
            if(m_TimerId != 0)
            {
                CounterManager.GetInstance().RemoveCounter(m_TimerId);
                m_TimerId = 0;
            }
        }

        protected override void OnClose()
        {
            StopTimer();
        }
    }
}
