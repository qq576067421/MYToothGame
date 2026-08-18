//功能：year_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：08/13/2026 14:04:13
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
     public class v_year_wnd:v_base_wnd
     {
          public object m_UserData; 
          //year_wnd/
          public ComponentBridge m_Bridge;

          //year_wnd/WindowContent/btnReturn
          public LUIButton m_btnReturn;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_btnReturn = m_Bridge.GetControl(0) as LUIButton;
          }
     }
}

