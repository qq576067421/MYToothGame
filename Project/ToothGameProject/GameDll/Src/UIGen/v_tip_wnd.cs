//功能：tip_wnd的窗口配置文件
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
     public class v_tip_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tip_wnd/
          public ComponentBridge m_Bridge;

          //tip_wnd/WindowContent/msgs
          public UIArray m_msgs;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_msgs = m_Bridge.GetControl(0) as UIArray;
          }
     }
}

