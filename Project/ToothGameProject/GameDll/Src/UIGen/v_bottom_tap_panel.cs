//功能：bottom_tap_panel的窗口配置文件
//工具作者：lichunlin
//生成时间：2026/4/2 15:01:34
//描述：以下文件是自动生成的，任何手动修改都会被下次自动生成覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
namespace GameHot
{
     public class v_bottom_tap_panel:v_base_wnd
     {
          public object m_UserData; 
          //bottom_tap_panel/
          public ComponentBridge m_Bridge;

          //bottom_tap_panel/table_titles
          public UIArray m_table_titles;

          //bottom_tap_panel/bg
          public LUIImage m_bg;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_table_titles = m_Bridge.GetControl(0) as UIArray;
               m_bg = m_Bridge.GetControl(1) as LUIImage;
          }
          public class v_title_item:v_base_wnd
          {
               public object m_UserData; 
               //title_item/
               public ComponentBridge m_Bridge;

               //title_item/btn
               public LUIButton m_btn;

               //title_item/btn/txtTitle
               public LUIText m_txtTitle;

               //title_item/actived/txtTitleActived
               public LUIText m_txtTitleActived;

               //title_item/actived/LineFocus
               public LUIImage m_LineFocus;

               //title_item/actived
               public UITransform m_actived;

               public override void InitComponent(GameObject go)
               {
                    m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
                    m_btn = m_Bridge.GetControl(0) as LUIButton;
                    m_txtTitle = m_Bridge.GetControl(1) as LUIText;
                    m_txtTitleActived = m_Bridge.GetControl(2) as LUIText;
                    m_LineFocus = m_Bridge.GetControl(3) as LUIImage;
                    m_actived = m_Bridge.GetControl(4) as UITransform;
               }
          }
     }
}

