//功能：tower_defend_setting_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：2026/7/13 18:02:18
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
     public class v_tower_defend_setting_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_setting_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_setting_wnd/bottom/luibutton_filled_1
          public LUIButton m_luibutton_filled_1;

          //tower_defend_setting_wnd/bottom/luibutton_filled_2
          public LUIButton m_luibutton_filled_2;

          //tower_defend_setting_wnd/bottom/luibutton_filled_3
          public LUIButton m_luibutton_filled_3;

          //tower_defend_setting_wnd/bottom/luibutton_filled_4
          public LUIButton m_luibutton_filled_4;

          //tower_defend_setting_wnd/bottom/luibutton_filled_1/luiimage1_Select
          public LUIImage m_luiimage1_Select;

          //tower_defend_setting_wnd/bottom/luibutton_filled_4/image
          public LUIImage m_image;

          //tower_defend_setting_wnd/middle/Pause_Btn/bg/Pause_Image
          public LUIImage m_Pause_Image;

          //tower_defend_setting_wnd/middle/Pause_Btn
          public LUIButton m_Pause_Btn;

          //tower_defend_setting_wnd/middle/Pause_Btn/bg/bg
          public LUIImage m_bg;

          //tower_defend_setting_wnd/middle/Pause_Btn/bg/bg
          public LUIImage m_bg_new1;

          //tower_defend_setting_wnd/middle/Pause_Btn/bg
          public LUIImage m_bg_new2;

          //tower_defend_setting_wnd/bg/bg
          public LUIImage m_bg_new3;

          //tower_defend_setting_wnd/bottom/luibutton_filled_1/mask
          public LUIImage m_mask;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_luibutton_filled_1 = m_Bridge.GetControl(0) as LUIButton;
               m_luibutton_filled_2 = m_Bridge.GetControl(1) as LUIButton;
               m_luibutton_filled_3 = m_Bridge.GetControl(2) as LUIButton;
               m_luibutton_filled_4 = m_Bridge.GetControl(3) as LUIButton;
               m_luiimage1_Select = m_Bridge.GetControl(4) as LUIImage;
               m_image = m_Bridge.GetControl(5) as LUIImage;
               m_Pause_Image = m_Bridge.GetControl(6) as LUIImage;
               m_Pause_Btn = m_Bridge.GetControl(7) as LUIButton;
               m_bg = m_Bridge.GetControl(8) as LUIImage;
               m_bg_new1 = m_Bridge.GetControl(9) as LUIImage;
               m_bg_new2 = m_Bridge.GetControl(10) as LUIImage;
               m_bg_new3 = m_Bridge.GetControl(11) as LUIImage;
               m_mask = m_Bridge.GetControl(12) as LUIImage;
          }
     }
}

