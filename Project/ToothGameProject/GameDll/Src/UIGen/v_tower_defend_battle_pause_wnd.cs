//功能：tower_defend_battle_pause_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：10/14/2026 10:50:13
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
     public class v_tower_defend_battle_pause_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_battle_pause_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_battle_pause_wnd/WindowContent/top/txt_title
          public LUITextMesh m_txt_title;

          //tower_defend_battle_pause_wnd/WindowContent/center/txt_summary
          public LUITextMesh m_txt_summary;

          //tower_defend_battle_pause_wnd/WindowContent/bottom/btn_resume
          public LUIButton m_btn_resume;

          //tower_defend_battle_pause_wnd/WindowContent/bottom/btn_restart
          public LUIButton m_btn_restart;

          //tower_defend_battle_pause_wnd/WindowContent/bottom/btn_return
          public LUIButton m_btn_return;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_txt_title = m_Bridge.GetControl(0) as LUITextMesh;
               m_txt_summary = m_Bridge.GetControl(1) as LUITextMesh;
               m_btn_resume = m_Bridge.GetControl(2) as LUIButton;
               m_btn_restart = m_Bridge.GetControl(3) as LUIButton;
               m_btn_return = m_Bridge.GetControl(4) as LUIButton;
          }
     }
}

