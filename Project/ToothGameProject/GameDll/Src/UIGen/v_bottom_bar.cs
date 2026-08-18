//功能：bottom_bar的窗口配置文件
//工具作者：lichunlin
//生成时间：2026/4/2 14:54:53
//描述：以下文件是自动生成的，任何手动修改都会被下次自动生成覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
namespace GameHot
{
     public class v_bottom_bar:v_base_wnd
     {
          public object m_UserData; 
          //bottom_bar/
          public ComponentBridge m_Bridge;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
          }
     }
}

