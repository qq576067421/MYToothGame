using System;
using GameDll;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
using MonoBean;

namespace GameHot
{
    public class account_level_up_wnd : WindowBase
    {
        private v_account_level_up_wnd m_View;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.common;
        }
        protected override void OnInitComponent()
        {
            m_View = new v_account_level_up_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_lcl_btnClose, OnClickClose);

//            dialog_titlebar.QuickCreateTitleBar(this, m_View.m_dialog_titlebar.gameObject,
//OnClickClose, RenderAPI.GetTextByLanId("player_account")); 

//            RenderAPI.AddButtonClick(m_View.m_btnOk, () =>
//            {
//                UIManager.CloseWindow(this);
//            });
        }

        private void OnClickClose()
        {
            UIManager.CloseWindow(this);
        }

        protected override void OnOpen()
        {
            var level = LobbyPlayer.GetInstance().m_PlayerInfo.level;
            RenderAPI.SetText(m_View.m_level, level.ToString());
        }




        protected override void OnClose()
        {

        }
    }
}
