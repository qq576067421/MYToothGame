//功能：fullscreen_return_title的窗口配置文件
//工具作者：lichunlin
//生成时间：2026/4/2 15:02:52
//描述：以下文件是自动生成的，任何手动修改都会被下次自动生成覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
namespace GameHot
{
     public class v_fullscreen_return_title:v_base_wnd
     {
          public object m_UserData; 
          //fullscreen_return_title/
          public ComponentBridge m_Bridge;

          //fullscreen_return_title/btnClose
          public LUIButton m_btnClose;

          //fullscreen_return_title/btnClose/txtTitle
          public LUIText m_txtTitle;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_btnClose = m_Bridge.GetControl(0) as LUIButton;
               m_txtTitle = m_Bridge.GetControl(1) as LUIText;
          }
     }
}

