using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityUI;
using GameDll;

namespace GameHot
{
    public class dialog_model : WindowModel
    {
        public string m_Msg = "";
        public Dictionary<int,string> m_ButtonNames = new Dictionary<int, string>();
        public Action<int> OnChoose;
        public Action<int, bool> OnChooseWithNotTips;
        public bool m_AutoClose = true;
        public bool m_ShowNotTips;
        public string m_Title;
        public bool m_HasShowTime = false;
        public float m_StartTime = 0;
        public float m_ShowTime = 0;
        public long m_ShowTimer = 0;

        public override void Clear()
        {
            m_Msg = "";
            m_ButtonNames.Clear();
            OnChoose = null;
            OnChooseWithNotTips = null;
            m_AutoClose = true;
            m_ShowNotTips = false;
            m_Title = null;
            m_HasShowTime = false;
            m_StartTime = 0;
            m_ShowTime = 0;
            m_ShowTimer = 0;
        }
    }
    public class dialog_wnd : WindowBase
    {

        private v_dialog_wnd m_View;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Loading;
            __CustomUIPrefabDir = UIPrefabDirs.common;
            __CreateModel(new dialog_model());
        }

        protected override void OnInitComponent()
        {
            m_View = new v_dialog_wnd();
            m_View.InitComponent(__GetWindowObj());
        }
        protected override void OnOpen()
        {
            DialogDisplay();
        }

        private void DialogDisplay()
        {
            var model = GetModel<dialog_model>();
            
            if (model.m_ButtonNames == null)
            {
                model.m_ButtonNames = new Dictionary<int, string>();
                model.m_ButtonNames.Add(0, RenderAPI.GetTextByLanId("ok"));
            }
            RenderAPI.UIArrayCopy(m_View.m_Menu, model.m_ButtonNames.Count);
            foreach (var kv in model.m_ButtonNames)
            {
                AddButton(kv.Key, kv.Value);
            }
            RenderAPI.SetText(m_View.m_msg, model.m_Msg);
            //RenderAPI.SetText(m_View.m_title, m_Title);
            dialog_titlebar.QuickCreateTitleBar(this, m_View.m_dialog_titlebar.gameObject,
                null, model.m_Title);

            RenderAPI.SetActive(m_View.m_Menu, !model.m_HasShowTime);

            RenderAPI.SetActive(m_View.m_NotTips, model.m_ShowNotTips);

            OnInitShowTime();
        }


        private void OnInitShowTime()
        {
            var model = GetModel<dialog_model>();
            RenderAPI.SetText(m_View.m_txtTime, "");
            int sec = (int)model.m_ShowTime;
            RenderAPI.SetText(m_View.m_txtTime, "");
            if(model.m_ShowTime > 0)
            {
                model.m_ShowTimer = CounterManager.GetInstance().AddCounter(200, -1, () =>
                {
                    var left_time = model.m_ShowTime - (UnityEngine.Time.realtimeSinceStartup - model.m_StartTime);
                    if (left_time < 0)
                    {
                        CounterManager.GetInstance().RemoveCounter(model.m_ShowTimer);
                        model.m_ShowTimer = 0;
                        RenderAPI.SetText(m_View.m_txtTime, "");
                        RenderAPI.SetActive(m_View.m_Menu, true);
                        return;
                    }

                    sec = (int)left_time;
                    if (sec <= 0)
                    {
                        sec = 0;
                    }
                    RenderAPI.SetTextLan(m_View.m_txtTime, "dialog_show_time_tips", sec);

                });
            }

        }

        protected override void OnClose()
        {
            var model = GetModel<dialog_model>();
            if (model.m_ShowTimer != 0)
            {
                CounterManager.GetInstance().RemoveCounter(model.m_ShowTimer);
                model.m_ShowTimer = 0;
            }
        }
        private void AddButton(int i, string buttonName)
        {
            var model = GetModel<dialog_model>();
            var com = m_View.m_Menu.m_Items[i];
            RenderAPI.SetActive(com, true);
            string name = buttonName;
            int idx = i;
            RenderAPI.SetText((LUIText)RenderAPI.GetComponent(com, typeof(LUIText), "txt"), name);
            var btn = (LUIButton)com.GetComponent(typeof(LUIButton));
            RenderAPI.AddButtonClick(btn, () =>
            {
                if(model.m_ShowTimer != 0)
                {
                    var left_time = model.m_ShowTime - (UnityEngine.Time.realtimeSinceStartup - model.m_StartTime);
                     var sec = (int)left_time;
                    if (sec <= 0)
                    {
                        sec = 0;
                    }
                    string str = RenderAPI.GetTextByLanId("dialog_show_time_tips", sec);
                    tip_wnd.GetInstance().OnShowTip(str);
                    model.OnChoose = null;
                    model.OnChooseWithNotTips = null;

                    return;
                }
                if (model.OnChoose != null)
                {
                    model.OnChoose(idx);
                    model.OnChoose = null;
                }
                if(model.OnChooseWithNotTips != null)
                {
                    model.OnChooseWithNotTips(idx, m_View.m_NotTips.isOn);
                    model.OnChooseWithNotTips = null;
                }
                if (model.m_AutoClose)
                {
                    UIManager.CloseWindow(this);
                }
                else
                {
                    model.m_AutoClose = true;
                }
            });
        }
        public void ShowDialog(string msg)
        {
            var model = GetModel<dialog_model>();
            model.m_ShowNotTips = false;
            model.OnChoose = null;
            model.OnChooseWithNotTips = null;

            ShowDialogImp(msg);
        }

        public static void ShowSimpleDialog(string info, Action call = null, string lan_ok = "ok")
        {
            var dialog = UIManager.OpenWindowAllowMultiEX<dialog_wnd>(null);
            Dictionary<int, string> chooseNames = new Dictionary<int, string>();
            chooseNames.Add(0, RenderAPI.GetTextByLanId(lan_ok));
            string error_msg = info;
            dialog.ShowDialog(error_msg, chooseNames, (chooseId) =>
            {
                if(call != null)
                {
                    call();
                }
            });
        }
        public static void ShowSimpleDialogOKCancel(string info, Action call = null, string lan_ok = "ok", string lan_cancel = "cancel")
        {
            var dialog = UIManager.OpenWindowAllowMultiEX<dialog_wnd>(null);
            Dictionary<int, string> chooseNames = new Dictionary<int, string>();
            chooseNames.Add(0, RenderAPI.GetTextByLanId(lan_ok));
            chooseNames.Add(1, RenderAPI.GetTextByLanId(lan_cancel));
            string error_msg = info;
            dialog.ShowDialog(error_msg, chooseNames, (chooseId) =>
            {
                if (chooseId == 0)
                {
                    if (call != null)
                    {
                        call();
                    }
                }
            });
        }
        public static void ShowSimpleDialogOKCancel(string info, Action<int> call = null, string lan_ok = "ok", string lan_cancel = "cancel")
        {
            var dialog = UIManager.OpenWindowAllowMultiEX<dialog_wnd>(null);
            Dictionary<int, string> chooseNames = new Dictionary<int, string>();
            chooseNames.Add(0, RenderAPI.GetTextByLanId(lan_ok));
            chooseNames.Add(1, RenderAPI.GetTextByLanId(lan_cancel));
            string error_msg = info;
            dialog.ShowDialog(error_msg, chooseNames, (chooseId) =>
            {
                if (call != null)
                {
                    call(chooseId);
                }
            });
        }
        public void ShowDialog(string msg, Dictionary<int, string> chooseNames = null, Action<int> onChooseCall = null,
            bool autoClose = true, string title = "", float showTime = 0)
        {
            var model = GetModel<dialog_model>();

            model.m_ShowNotTips = false;
            model.OnChoose = onChooseCall;
            model.OnChooseWithNotTips = null;

            ShowDialogImp(msg, chooseNames, autoClose, title, showTime);
        }

        private void ShowDialogImp(string msg, Dictionary<int, string> chooseNames = null,
            bool autoClose = true, string title = "", float showTime = 0)
        {
            var model = GetModel<dialog_model>();
            model.m_Msg = msg;
            model.m_Title = title;
            model.m_ButtonNames = chooseNames;
            model.m_AutoClose = autoClose;
            if (showTime > 0)
            {
                model.m_HasShowTime = true;
                model.m_ShowTime = showTime;
                model.m_StartTime = UnityEngine.Time.realtimeSinceStartup;
            }
            else
            {
                model.m_HasShowTime = false;
            }

            if (IsInitializedView())
            {
                DialogDisplay();
            }
        }

        public void ShowDialog(string msg, Dictionary<int, string> chooseNames = null, Action<int, bool> onChooseCall = null,
    bool autoClose = true, string title = "", float showTime = 0, bool showNotTips = false)
        {
            var model = GetModel<dialog_model>();
            model.m_ShowNotTips = showNotTips;
            model.OnChooseWithNotTips = onChooseCall;
            model.OnChoose = null;

            ShowDialogImp(msg, chooseNames, autoClose, title, showTime);
        }
    }
}