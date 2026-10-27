using System;
using System.Collections.Generic;
using MonoBean;
using UnityEngine.InputSystem;
using GameDll;
using System.Diagnostics.SymbolStore;
using UnityEngine.UI;
using UnityEngine;

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
            OnInitLanguage();
        }
        protected override void OnOpen()
        {
            CGameProcedure.Event.OnEscapPressed += CloseYearWindow;
        }
        protected override void OnClose()
        {
            CGameProcedure.Event.OnEscapPressed -= CloseYearWindow;
        }
        private void OnInitLanguage()
        {
            RenderAPI.SetTextLan(m_View.m_Instruction_title, "td_login_year_Instruction_title");
            RenderAPI.SetTextLan(m_View.m_Instruction_1, "td_login_year_Instruction_1");
            RenderAPI.SetTextLan(m_View.m_Instruction_2, "td_login_year_Instruction_2");
            RenderAPI.SetTextLan(m_View.m_Instruction_3, "td_login_year_Instruction_3");
            RenderAPI.SetTextLan(m_View.m_Instruction_4, "td_login_year_Instruction_4");
            RenderAPI.SetTextLan(m_View.m_Instruction_5, "td_login_year_Instruction_5");

            switch (LobbyPlayer.GetInstance().GetLanguageType())
            {
                case LanguageType.China:
                    m_View.m_Content.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                    break;
                case LanguageType.English:
                    m_View.m_Content.GetComponent<RectTransform>().sizeDelta = new Vector2(0,1230);
                    break;
            }

            AddCounter(200, 1, () =>
            {
                m_View.m_Content.gameObject.SetActive(false);
                m_View.m_Content.gameObject.SetActive(true);
            });
        }
        private void CloseYearWindow(InputAction.CallbackContext context)
        {
            UIManager.CloseWindow(this);
        }
    }
}
