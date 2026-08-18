using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
using MonoBean;
using GameDll;

namespace GameHot
{
    /// <summary>
    /// 1	开启单位养成
    //  2	开启征讨战
    //  3	开启装备合成
    //  4	开启神器
    //  5	开启达尔文
    /// </summary>
    public enum function_open_type
    {
        unit = 1,
        master = 2,
        compose = 3,
        magic_weapon = 4,
        daerwen = 5,
        pvp = 6,
        td = 7,
        catmouse = 8,
        cardpvp = 9
    }
    public class function_open_model : WindowModel
    {
        public List<int> m_OpenFuncIds = new List<int>();
        public long m_TimerId = 0;
        public int m_CurId = 0;
        public Action m_FinishCallback;

        public override void Clear()
        {
            m_OpenFuncIds.Clear();
            m_TimerId = 0;
            m_CurId = 0;
            m_FinishCallback = null;
        }
    }
    public class function_open_wnd : WindowBase
    {
        private v_function_open_wnd m_View;

        public void AddOpenFuncIds(Dictionary<int, bool> ids)
        {
            var model = GetModel<function_open_model>();
            model.m_OpenFuncIds.Clear();
            foreach(var kv in ids)
            {
                model.m_OpenFuncIds.Add(kv.Key);
            }
        }
        public void SetFinishCallback(Action call)
        {
            var model = GetModel<function_open_model>();
            model.m_FinishCallback = call;
        }

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.common;
            __CreateModel(new function_open_model());
        }

        private dialog_titlebar m_TitleBar;
        protected override void OnInitComponent()
        {
            m_View = new v_function_open_wnd();
            m_View.InitComponent(__GetWindowObj());
            m_TitleBar = dialog_titlebar.QuickCreateTitleBar(this, m_View.m_dialog_titlebar.gameObject,
                OnClickClose, "");
            RenderAPI.AddButtonClick(m_View.m_lcl_btnClose, OnClickClose);
        }
        private void OnClickClose()
        {
            var model = GetModel<function_open_model>();
            RenderAPI.SetActive(m_View.m_bg, false);
            if (!FunctionOpenDisplay(1000))
            {
                if (model.m_FinishCallback != null)
                {
                    model.m_FinishCallback();
                    model.m_FinishCallback = null;
                }
                UIManager.CloseWindow(this);
            }
        }
        private bool FunctionOpenDisplay(int delay_time)
        {
            var model = GetModel<function_open_model>();
            if (model.m_OpenFuncIds.Count  == 0)
            {
                return false;
            }
            model.m_CurId = model.m_OpenFuncIds[0];
            model.m_OpenFuncIds.RemoveAt(0);

            if (model.m_TimerId != 0)
            {
                CounterManager.GetInstance().RemoveCounter(model.m_TimerId);
                model.m_TimerId = 0;
            }
            if (delay_time > 0)
            {
                model.m_TimerId = CounterManager.GetInstance().AddCounter(delay_time, 1, ShowOneFunction);
            }
            else
            {
                ShowOneFunction();
            }


            return true;
        }

        private void ShowOneFunction()
        {
            //var model = GetModel<function_open_model>();
            //var funcMgr = LobbyPlayer.FunctionMgr;
            //var title = RenderAPI.GetTextByLanId("open_function_title", funcMgr.GetOpenFunctionName(model.m_CurId));
            //m_TitleBar.SetTitle(title);

            //RenderAPI.SetText(m_View.m_desc, funcMgr.GetOpenFunctionDesc(model.m_CurId));

            //var cfg = t_openFuncBean.GetConfig(model.m_CurId);
            //if(string.IsNullOrEmpty(cfg.t_icon))
            //{
            //    RenderAPI.SetActive(m_View.m_icon_raw, true);
            //    RenderAPI.SetActive(m_View.m_icon, false);

            //    __SetImage(m_View.m_icon_raw, cfg.t_atlas);
            //}
            //else
            //{
            //    RenderAPI.SetActive(m_View.m_icon_raw, false);
            //    RenderAPI.SetActive(m_View.m_icon, true);

            //    __SetImage(m_View.m_icon, cfg.t_atlas, cfg.t_icon);
            //}
        }



        protected override void OnOpen()
        {
            RenderAPI.SetActive(m_View.m_bg, true);
            FunctionOpenDisplay(0);
        }



        protected override void OnClose()
        {
            var model = GetModel<function_open_model>();
            if(model.m_TimerId != 0)
            {
                CounterManager.GetInstance().RemoveCounter(model.m_TimerId);
                model.m_TimerId = 0;
            }
        }
    }
}
