//功能：dialog_wnd的窗口配置文件
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
     public class v_dialog_wnd:v_base_wnd
     {
          public object m_UserData; 
          //dialog_wnd/
          public ComponentBridge m_Bridge;

          //dialog_wnd/WindowContent/windowed_ani_ui/__ani_node/center/slot/msg
          public LUIText m_msg;

          //dialog_wnd/WindowContent/windowed_ani_ui/__ani_node/center/slot/Menu
          public UIArray m_Menu;

          //dialog_wnd/WindowContent/windowed_ani_ui/__ani_node/center/slot/txtTime
          public LUIText m_txtTime;

          //dialog_wnd/WindowContent/windowed_ani_ui
          public UIWindowAnimation m_windowed_ani_ui;

          //dialog_titlebar/
          public UISubWindow m_dialog_titlebar;

          //dialog_wnd/WindowContent/windowed_ani_ui/__ani_node/center/slot/NotTips
          public LUIToggle m_NotTips;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_msg = m_Bridge.GetControl(0) as LUIText;
               m_Menu = m_Bridge.GetControl(1) as UIArray;
               m_txtTime = m_Bridge.GetControl(2) as LUIText;
               m_windowed_ani_ui = m_Bridge.GetControl(3) as UIWindowAnimation;
               m_dialog_titlebar = m_Bridge.GetControl(4) as UISubWindow;
               m_NotTips = m_Bridge.GetControl(5) as LUIToggle;
          }
     }
}

