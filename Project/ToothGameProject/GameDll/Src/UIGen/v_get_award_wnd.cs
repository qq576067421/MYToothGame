//功能：get_award_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：2026/4/2 14:26:47
//描述：以下文件是自动生成的，任何手动修改都会被下次自动生成覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
namespace GameHot
{
     public class v_get_award_wnd:v_base_wnd
     {
          public object m_UserData; 
          //get_award_wnd/
          public ComponentBridge m_Bridge;

          //get_award_wnd/WindowContent/pop/title_tip
          public LUIText m_title_tip;

          //get_award_wnd/WindowContent/pop/ScrollView_LeftToRight/Viewport/Content
          public UIArray m_Content;

          //get_award_wnd/WindowContent/pop/close_tip
          public LUIText m_close_tip;

          //get_award_wnd/WindowContent/lcl_btnOk
          public LUIButton m_lcl_btnOk;

          //get_award_wnd/WindowContent/lcl_btnClose
          public LUIButton m_lcl_btnClose;

          //dialog_titlebar/
          public UISubWindow m_dialog_titlebar;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_title_tip = m_Bridge.GetControl(0) as LUIText;
               m_Content = m_Bridge.GetControl(1) as UIArray;
               m_close_tip = m_Bridge.GetControl(2) as LUIText;
               m_lcl_btnOk = m_Bridge.GetControl(3) as LUIButton;
               m_lcl_btnClose = m_Bridge.GetControl(4) as LUIButton;
               m_dialog_titlebar = m_Bridge.GetControl(5) as UISubWindow;
          }
     }
}

