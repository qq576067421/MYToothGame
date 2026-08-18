using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityUI;
using GameDll;

namespace GameHot
{
    public class help_wnd : WindowBase
    {
        private v_help_wnd m_View;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.common;
        }

        protected override void OnInitComponent()
        {
            m_View = new v_help_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_btnButton, OnClickClose);
        }

        private void OnClickClose()
        {
            UIManager.CloseWindow(this);
        }

        protected override void OnOpen()
        {
            var str = (string)__GetUserData()[0];
            RenderAPI.SetTextLan(m_View.m_msg, str);


            //if (LobbyPlayer.GuideMgr.IsGroup(0, guide_step_ids.click_open_attr_help))
            //{
            //    LobbyPlayer.GuideMgr.AddStep(guide_step_ids.close_attr_help, (step) =>
            //    {
            //        step.m_Target0 = m_View.m_btnButton.gameObject;
            //    });
            //    LobbyPlayer.GuideMgr.StartSteps();
            //}
        }
    }
}