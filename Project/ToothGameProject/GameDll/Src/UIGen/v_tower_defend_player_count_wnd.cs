//功能：tower_defend_player_count_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：10/15/2026 17:33:45
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
     public class v_tower_defend_player_count_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_player_count_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer0
          public LUIButton m_btnPlayer0;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer1
          public LUIButton m_btnPlayer1;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer2
          public LUIButton m_btnPlayer2;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer3
          public LUIButton m_btnPlayer3;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer3/icon
          public LUIImage m_icon;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer3/playIcon
          public LUIImage m_playIcon;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/choose3/playIcon
          public LUIImage m_playIcon_new1;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/choose3/icon
          public LUIImage m_icon_new1;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/choose3/four_txt
          public LUITextMesh m_four_txt;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer
          public LUIImage m_btnPlayer;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer0/one_txt
          public LUITextMesh m_one_txt;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/choose0/one_txt
          public LUITextMesh m_one_txt_new1;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer1/two_txt
          public LUITextMesh m_two_txt;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/choose1/two_txt
          public LUITextMesh m_two_txt_new1;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer2/three_txt
          public LUITextMesh m_three_txt;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/choose2/three_txt
          public LUITextMesh m_three_txt_new1;

          //tower_defend_player_count_wnd/WindowContent/btnPlayer/btnPlayer3/four_txt
          public LUITextMesh m_four_txt_new1;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_btnPlayer0 = m_Bridge.GetControl(0) as LUIButton;
               m_btnPlayer1 = m_Bridge.GetControl(1) as LUIButton;
               m_btnPlayer2 = m_Bridge.GetControl(2) as LUIButton;
               m_btnPlayer3 = m_Bridge.GetControl(3) as LUIButton;
               m_icon = m_Bridge.GetControl(4) as LUIImage;
               m_playIcon = m_Bridge.GetControl(5) as LUIImage;
               m_playIcon_new1 = m_Bridge.GetControl(6) as LUIImage;
               m_icon_new1 = m_Bridge.GetControl(7) as LUIImage;
               m_four_txt = m_Bridge.GetControl(8) as LUITextMesh;
               m_btnPlayer = m_Bridge.GetControl(9) as LUIImage;
               m_one_txt = m_Bridge.GetControl(10) as LUITextMesh;
               m_one_txt_new1 = m_Bridge.GetControl(11) as LUITextMesh;
               m_two_txt = m_Bridge.GetControl(12) as LUITextMesh;
               m_two_txt_new1 = m_Bridge.GetControl(13) as LUITextMesh;
               m_three_txt = m_Bridge.GetControl(14) as LUITextMesh;
               m_three_txt_new1 = m_Bridge.GetControl(15) as LUITextMesh;
               m_four_txt_new1 = m_Bridge.GetControl(16) as LUITextMesh;
          }
     }
}

