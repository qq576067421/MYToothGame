using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityUI;
using GameDll;

namespace GameHot
{
    public class dialog_titlebar
    {
        private WindowBase m_Wnd;
        public v_dialog_titlebar m_View;
        public LUIButton GetCloseBtn()
        {
            return m_View.m_btnTitleClose;
        }
        public void OnInitComponent(WindowBase wnd, GameObject go)
        {
            m_Wnd = wnd;
            m_View = new v_dialog_titlebar();
            m_View.InitComponent(go);
            RenderAPI.SetActive(m_View.m_btnTitleClose, false);
        }

        public void SetTitleBar(Action OnCloseCall, string title)
        {
            if(OnCloseCall != null)
            {
                RenderAPI.SetActive(m_View.m_btnTitleClose, true);
                RenderAPI.AddButtonClick(m_View.m_btnTitleClose, OnCloseCall);
            }

            RenderAPI.SetText(m_View.m_txtTitle, title);
        }
        public void SetTitle(string title)
        {
            RenderAPI.SetText(m_View.m_txtTitle, title);
        }

        public static dialog_titlebar QuickCreateTitleBar(WindowBase wnd, GameObject go, Action OnCloseCall, string title)
        {
            dialog_titlebar bar = new dialog_titlebar();
            bar.OnInitComponent(wnd, go);
            bar.SetTitleBar(OnCloseCall, title);
            return bar;
        }
    }
}
