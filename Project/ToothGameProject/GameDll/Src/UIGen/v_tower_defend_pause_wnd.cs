//功能：tower_defend_pause_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：10/16/2026 11:15:29
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
     public class v_tower_defend_pause_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_pause_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_1
          public LUIButton m_luibutton_filled_1;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_2
          public LUIButton m_luibutton_filled_2;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_3
          public LUIButton m_luibutton_filled_3;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_4
          public LUIButton m_luibutton_filled_4;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_1/choose
          public LUIImage m_choose;

          //tower_defend_pause_wnd/middle/Pause_Btn/bg/Pause_Image
          public LUIImage m_Pause_Image;

          //tower_defend_pause_wnd/middle/Pause_Btn/bg/mid_oringe_di
          public LUIImage m_mid_oringe_di;

          //tower_defend_pause_wnd/middle/Pause_Btn/bg/mid_oringe
          public LUIImage m_mid_oringe;

          //tower_defend_pause_wnd/middle/Pause_Btn/bg
          public LUIImage m_bg;

          //tower_defend_pause_wnd/bg/bg1
          public LUIImage m_bg1;

          //tower_defend_pause_wnd/stop_txt
          public LUITextMesh m_stop_txt;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_1/choose/continue_choose_txt
          public LUITextMesh m_continue_choose_txt;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_1/continue_txt
          public LUITextMesh m_continue_txt;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_2/startOver_txt
          public LUITextMesh m_startOver_txt;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_2/choose/startOver_choose_txt
          public LUITextMesh m_startOver_choose_txt;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_3/choose/returnLobby_choose_txt
          public LUITextMesh m_returnLobby_choose_txt;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_3/returnLobby_txt
          public LUITextMesh m_returnLobby_txt;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_4/choose/gameSetting_choose_txt
          public LUITextMesh m_gameSetting_choose_txt;

          //tower_defend_pause_wnd/middle_btn/luibutton_filled_4/gameSetting_txt
          public LUITextMesh m_gameSetting_txt;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_luibutton_filled_1 = m_Bridge.GetControl(0) as LUIButton;
               m_luibutton_filled_2 = m_Bridge.GetControl(1) as LUIButton;
               m_luibutton_filled_3 = m_Bridge.GetControl(2) as LUIButton;
               m_luibutton_filled_4 = m_Bridge.GetControl(3) as LUIButton;
               m_choose = m_Bridge.GetControl(4) as LUIImage;
               m_Pause_Image = m_Bridge.GetControl(5) as LUIImage;
               m_mid_oringe_di = m_Bridge.GetControl(6) as LUIImage;
               m_mid_oringe = m_Bridge.GetControl(7) as LUIImage;
               m_bg = m_Bridge.GetControl(8) as LUIImage;
               m_bg1 = m_Bridge.GetControl(9) as LUIImage;
               m_stop_txt = m_Bridge.GetControl(10) as LUITextMesh;
               m_continue_choose_txt = m_Bridge.GetControl(11) as LUITextMesh;
               m_continue_txt = m_Bridge.GetControl(12) as LUITextMesh;
               m_startOver_txt = m_Bridge.GetControl(13) as LUITextMesh;
               m_startOver_choose_txt = m_Bridge.GetControl(14) as LUITextMesh;
               m_returnLobby_choose_txt = m_Bridge.GetControl(15) as LUITextMesh;
               m_returnLobby_txt = m_Bridge.GetControl(16) as LUITextMesh;
               m_gameSetting_choose_txt = m_Bridge.GetControl(17) as LUITextMesh;
               m_gameSetting_txt = m_Bridge.GetControl(18) as LUITextMesh;
          }
     }
}

