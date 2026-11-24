using GameDll;
using UnityUI;
using UnityEngine;
using UnityEngine.UI;

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
        private Outline m_ChinaFocus;
        private Outline m_EnglishFocus;

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

            m_ChinaFocus = CreateFocusOutline(m_View.m_china);
            m_EnglishFocus = CreateFocusOutline(m_View.m_english);
            RenderAPI.AddButtonClick(m_View.m_china, () => SwitchLanguage(LanguageType.China));
            RenderAPI.AddButtonClick(m_View.m_english, () => SwitchLanguage(LanguageType.English));
            OnInitLanguage();
        }

        protected override void OnOpen()
        {
            var language = LobbyPlayer.GetInstance().GetLanguageType();
            RefreshLanguageSelection(language);
            var row = new ActiveButtons();
            row.buttons.Add(m_View.m_china);
            row.buttons.Add(m_View.m_english);
            __SetActiveButtons(new[] { row }, defaultColumnIndex: language == LanguageType.English ? 1 : 0);
            RenderEvent.Event.OnUpdateSelectionVisuals += RefreshRemoteFocus;
            RefreshRemoteFocus();
        }

        protected override void OnClose()
        {
            RenderEvent.Event.OnUpdateSelectionVisuals -= RefreshRemoteFocus;
        }

        private static Outline CreateFocusOutline(LUIButton button)
        {
            var outline = button.targetGraphic.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.enabled = false;
            return outline;
        }

        private void RefreshRemoteFocus()
        {
            m_ChinaFocus.enabled = RenderAPI.IsCurrentMenuButton(m_View.m_china);
            m_EnglishFocus.enabled = RenderAPI.IsCurrentMenuButton(m_View.m_english);
        }

        private void OnInitLanguage()
        {
            RenderAPI.SetTextLan(m_View.m_language_txt, "td_setting_language_txt");
        }
        private void SwitchLanguage(LanguageType languageType)
        {
            var player = LobbyPlayer.GetInstance();
            if (player.GetLanguageType() != languageType)
            {
                player.SetLanguageType(languageType);
                InitLanguage();
            }
            RefreshLanguageSelection(languageType);
        }

        private void RefreshLanguageSelection(LanguageType languageType)
        {
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
