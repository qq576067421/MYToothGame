//功能：gm_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：2026/7/23 15:37:55
//描述：以下文件是自动生成的，任何手动修改都会被下次自动生成覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityUI;
using GameDll;
namespace GameHot
{
     public class v_gm_wnd:v_base_wnd
     {
          public object m_UserData; 
          //gm_wnd/
          public ComponentBridge m_Bridge;

          //gm_wnd/WindowContent/btnClose
          public LUIButton m_btnClose;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_btnClose = m_Bridge.GetControl(0) as LUIButton;
          }
     }
}

