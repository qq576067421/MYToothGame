//功能：lobby_main_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：2026/7/13 19:27:36
//描述：以下文件是自动生成的，任何手动修改都会被下次自动生成覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityUI;
namespace GameHot
{
     public class v_lobby_main_wnd:v_base_wnd
     {
          public object m_UserData; 
          //lobby_main_wnd/
          public ComponentBridge m_Bridge;

          //lobby_main_wnd/WindowContent/guide_mask
          public LUIImage m_guide_mask;

          //lobby_main_wnd/WindowContent/normal_ani_ui
          public UIWindowAnimation m_normal_ani_ui;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnModeChapter
          public LUIButton m_btnModeChapter;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnModeEndless
          public LUIButton m_btnModeEndless;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnToothFactory
          public LUIButton m_btnToothFactory;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnSetting
          public LUIButton m_btnSetting;

          //lobby_main_wnd/WindowContent/luiimage_3
          public LUIImage m_luiimage_3;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnModeChapter/luiimage_3
          public LUIImage m_luiimage_3_new1;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/right/luiimage_tap
          public LUIImage m_luiimage_tap;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/top/luiimage_1
          public LUIImage m_luiimage_1;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/right/luiimage_tap/character
          public LUIImage m_character;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/top/txt_coin
          public LUITextMesh m_txt_coin;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_guide_mask = m_Bridge.GetControl(0) as LUIImage;
               m_normal_ani_ui = m_Bridge.GetControl(1) as UIWindowAnimation;
               m_btnModeChapter = m_Bridge.GetControl(2) as LUIButton;
               m_btnModeEndless = m_Bridge.GetControl(3) as LUIButton;
               m_btnToothFactory = m_Bridge.GetControl(4) as LUIButton;
               m_btnSetting = m_Bridge.GetControl(5) as LUIButton;
               m_luiimage_3 = m_Bridge.GetControl(6) as LUIImage;
               m_luiimage_3_new1 = m_Bridge.GetControl(7) as LUIImage;
               m_luiimage_tap = m_Bridge.GetControl(8) as LUIImage;
               m_luiimage_1 = m_Bridge.GetControl(9) as LUIImage;
               m_character = m_Bridge.GetControl(10) as LUIImage;
               m_txt_coin = m_Bridge.GetControl(11) as LUITextMesh;
          }
     }
}

