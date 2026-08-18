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
    public class buff_look_wnd : WindowBase
    {
        private v_buff_look_wnd m_View;
        private long m_CfgId;
        //private t_buffDesc m_Cfg;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();

            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.common;
        }
        protected override void OnInitComponent()
        {
            m_View = new v_buff_look_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_btnClose, () =>
            {
                UIManager.CloseWindow(this);
            });
        }
        protected override void OnOpen()
        {
            m_CfgId = (long)__GetUserData()[0];
            //var cfg = t_buff.GetConfig(m_CfgId);
            //m_Cfg = t_buffDesc.GetConfig(cfg.t_descId);

            //__SetImage(m_View.m_item_icon, m_Cfg.t_battle_icon, true);
            //RenderAPI.SetTextLan(m_View.m_item_name, m_Cfg.t_name);
            //RenderAPI.SetTextLan(m_View.m_item_desc, m_Cfg.t_desc);
        }
    }
}
