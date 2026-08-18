//功能：dialog_titlebar的窗口配置文件
//工具作者：lichunlin
//生成时间：2026/4/2 14:55:36
//描述：以下文件是自动生成的，任何手动修改都会被下次自动生成覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
namespace GameHot
{
     public class v_dialog_titlebar:v_base_wnd
     {
          public object m_UserData; 
          //dialog_titlebar/
          public ComponentBridge m_Bridge;

          //dialog_titlebar/Top/btnTitleClose
          public LUIButton m_btnTitleClose;

          //dialog_titlebar/Top/Background/txtTitle
          public LUIText m_txtTitle;

          //dialog_titlebar/Top/btnTitleClose/Background
          public LUIImage m_Background;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_btnTitleClose = m_Bridge.GetControl(0) as LUIButton;
               m_txtTitle = m_Bridge.GetControl(1) as LUIText;
               m_Background = m_Bridge.GetControl(2) as LUIImage;
          }
     }
}

