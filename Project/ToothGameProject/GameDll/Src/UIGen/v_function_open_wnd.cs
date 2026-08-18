//功能：function_open_wnd的窗口配置文件
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
     public class v_function_open_wnd:v_base_wnd
     {
          public object m_UserData; 
          //function_open_wnd/
          public ComponentBridge m_Bridge;

          //function_open_wnd/WindowContent/bg/desc
          public LUIText m_desc;

          //function_open_wnd/WindowContent/bg/icon
          public LUIImage m_icon;

          //function_open_wnd/WindowContent/bg/icon_raw
          public LUIRawImage m_icon_raw;

          //function_open_wnd/WindowContent/bg/lcl_btnClose
          public LUIButton m_lcl_btnClose;

          //dialog_titlebar/
          public UISubWindow m_dialog_titlebar;

          //function_open_wnd/WindowContent/bg
          public UITransform m_bg;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_desc = m_Bridge.GetControl(0) as LUIText;
               m_icon = m_Bridge.GetControl(1) as LUIImage;
               m_icon_raw = m_Bridge.GetControl(2) as LUIRawImage;
               m_lcl_btnClose = m_Bridge.GetControl(3) as LUIButton;
               m_dialog_titlebar = m_Bridge.GetControl(4) as UISubWindow;
               m_bg = m_Bridge.GetControl(5) as UITransform;
          }
     }
}

