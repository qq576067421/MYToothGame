using GameDll;
using System;
using LCL;
using UnityUI;
using UnityEngine;

namespace GameHot
{
    public enum LanguageType
    {
        China,
        English
    }
    public class tower_defend_setting_wnd : WindowBase
    {
        private v_tower_defend_setting_wnd m_View;

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
            m_View = new v_tower_defend_setting_wnd();
            m_View.InitComponent(__GetWindowObj());

            SwitchLanguage(LobbyPlayer.GetInstance().GetLanguageType());
            RenderAPI.AddButtonClick(m_View.m_china,()=> SwitchLanguage(LanguageType.China));
            RenderAPI.AddButtonClick(m_View.m_english,()=> SwitchLanguage(LanguageType.English));
            OnInitLanguage();
        }

        protected override void OnOpen()
        {

        }

        protected override void OnClose()
        {

        }
        private void OnInitLanguage()
        {
            RenderAPI.SetTextLan(m_View.m_language_txt, "td_setting_language_txt");
        }
        private void SwitchLanguage(LanguageType languageType)
        {
            LobbyPlayer.GetInstance().SetLanguageType(languageType);

            switch (languageType)
            {
                case LanguageType.China:
                    m_View.m_china.GetComponent<LUIImage>().color = Color.red;
                    m_View.m_english.GetComponent<LUIImage>().color = Color.black;
                    break;
                case LanguageType.English:
                    m_View.m_china.GetComponent<LUIImage>().color = Color.black;
                    m_View.m_english.GetComponent<LUIImage>().color = Color.red;
                    break;
            }
            InitLanguage();
        }
        public static void InitLanguage()
        {
            switch (LobbyPlayer.GetInstance().GetLanguageType())
            {
                case LanguageType.China:
                    RenderAPI.InitLanguage(0, "CN");
                    break;
                case LanguageType.English:
                    RenderAPI.InitLanguage(1, "EN");
                    break;
            }

            var texts = UIManager.m_CanvasScaler.GetComponentsInChildren<LUITextMesh>(true);
            foreach (var text in texts)
            {
                text.OnChangeLanguage();
            }
        }
    }
}
