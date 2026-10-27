//功能：tower_defend_setting_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：10/14/2026 14:19:16
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
     public class v_tower_defend_setting_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_setting_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_setting_wnd/middle_btn/Language/language_txt
          public LUITextMesh m_language_txt;

          //tower_defend_setting_wnd/middle_btn/Language/bg/china
          public LUIButton m_china;

          //tower_defend_setting_wnd/middle_btn/Language/bg/english
          public LUIButton m_english;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_language_txt = m_Bridge.GetControl(0) as LUITextMesh;
               m_china = m_Bridge.GetControl(1) as LUIButton;
               m_english = m_Bridge.GetControl(2) as LUIButton;
          }
     }
}

