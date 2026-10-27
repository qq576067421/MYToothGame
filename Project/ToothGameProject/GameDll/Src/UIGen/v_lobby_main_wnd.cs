//功能：lobby_main_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：10/14/2026 17:44:50
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

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnModeChapter/image
          public LUIImage m_image;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/right/luiimage_tap
          public LUIImage m_luiimage_tap;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/top/luiimage_1
          public LUIImage m_luiimage_1;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/right/luiimage_tap/character
          public LUIImage m_character;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/top/txt_coin
          public LUITextMesh m_txt_coin;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/title_Image
          public LUIImage m_title_Image;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnModeChapter/btnModeChapter_txt
          public LUITextMesh m_btnModeChapter_txt;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnModeEndless/btnModeEndless_txt
          public LUITextMesh m_btnModeEndless_txt;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnModeChapter/choose/btnModeChapterChoose_txt
          public LUITextMesh m_btnModeChapterChoose_txt;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnModeEndless/choose/btnModeEndlessChoose_txt
          public LUITextMesh m_btnModeEndlessChoose_txt;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnToothFactory/btnToothFactory_txt
          public LUITextMesh m_btnToothFactory_txt;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnToothFactory/choose/btnToothFactoryChoose_txt
          public LUITextMesh m_btnToothFactoryChoose_txt;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnSetting/btnSetting_txt
          public LUITextMesh m_btnSetting_txt;

          //lobby_main_wnd/WindowContent/normal_ani_ui/__ani_node/left/btnSetting/choose/btnSettingChoose_txt
          public LUITextMesh m_btnSettingChoose_txt;

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
               m_image = m_Bridge.GetControl(7) as LUIImage;
               m_luiimage_tap = m_Bridge.GetControl(8) as LUIImage;
               m_luiimage_1 = m_Bridge.GetControl(9) as LUIImage;
               m_character = m_Bridge.GetControl(10) as LUIImage;
               m_txt_coin = m_Bridge.GetControl(11) as LUITextMesh;
               m_title_Image = m_Bridge.GetControl(12) as LUIImage;
               m_btnModeChapter_txt = m_Bridge.GetControl(13) as LUITextMesh;
               m_btnModeEndless_txt = m_Bridge.GetControl(14) as LUITextMesh;
               m_btnModeChapterChoose_txt = m_Bridge.GetControl(15) as LUITextMesh;
               m_btnModeEndlessChoose_txt = m_Bridge.GetControl(16) as LUITextMesh;
               m_btnToothFactory_txt = m_Bridge.GetControl(17) as LUITextMesh;
               m_btnToothFactoryChoose_txt = m_Bridge.GetControl(18) as LUITextMesh;
               m_btnSetting_txt = m_Bridge.GetControl(19) as LUITextMesh;
               m_btnSettingChoose_txt = m_Bridge.GetControl(20) as LUITextMesh;
          }
     }
}

