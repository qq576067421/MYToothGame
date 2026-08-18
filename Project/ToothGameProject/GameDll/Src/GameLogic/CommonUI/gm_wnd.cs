using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameDll;
using UnityUI;

namespace GameHot
{
    public class gm_wnd : WindowBase
    {
        private v_gm_wnd m_View;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __ParticipateCurrentActiveWindow = true;
            __CustomUIPrefabDir = UIPrefabDirs.common;
        }
        protected override void OnInitComponent()
        {
            m_View = new v_gm_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_btnClose, OnClickClose);

        }

        private void OnClickClose()
        {
            UIManager.CloseWindow(this);
        }
    }
}
