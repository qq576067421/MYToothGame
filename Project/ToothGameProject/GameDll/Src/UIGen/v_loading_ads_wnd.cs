//功能：loading_ads_wnd的窗口配置文件
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
     public class v_loading_ads_wnd:v_base_wnd
     {
          public object m_UserData; 
          //loading_ads_wnd/
          public ComponentBridge m_Bridge;

          //loading_ads_wnd/WindowContent/loadtip
          public LUIText m_loadtip;

          //loading_ads_wnd/WindowContent/btnCancel
          public LUIButton m_btnCancel;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_loadtip = m_Bridge.GetControl(0) as LUIText;
               m_btnCancel = m_Bridge.GetControl(1) as LUIButton;
          }
     }
}

