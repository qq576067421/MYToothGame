using System;
using System.Collections.Generic;
using MonoBean;
using UnityEngine.InputSystem;
using GameDll;

namespace GameHot
{
    public class year_wnd : WindowBase
    {
        private v_year_wnd m_View;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __ParticipateCurrentActiveWindow = true;
            __CustomUIPrefabDir = UIPrefabDirs.login;
        }
        private void OnClickClose()
        {
            UIManager.CloseWindow(this);
        }
        protected override void OnInitComponent()
        {
            m_View = new v_year_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_btnReturn, OnClickClose);
        }
        protected override void OnOpen()
        {
            CGameProcedure.Event.OnEscapPressed += CloseYearWindow;
        }
        protected override void OnClose()
        {
            CGameProcedure.Event.OnEscapPressed -= CloseYearWindow;
        }
        private void CloseYearWindow(InputAction.CallbackContext context)
        {
            UIManager.CloseWindow(this);
        }
    }
}
