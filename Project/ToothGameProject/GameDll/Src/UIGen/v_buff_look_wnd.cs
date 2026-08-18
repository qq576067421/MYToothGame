//功能：buff_look_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：2026/4/2 14:26:47
//描述：以下文件是自动生成的，任何手动修改都会被下次自动生成覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
namespace GameHot
{
     public class v_buff_look_wnd:v_base_wnd
     {
          public object m_UserData; 
          //buff_look_wnd/
          public ComponentBridge m_Bridge;

          //buff_look_wnd/WindowContent/desc_bg/item_desc
          public LUIText m_item_desc;

          //buff_look_wnd/WindowContent/item_bg
          public LUIImage m_item_bg;

          //buff_look_wnd/WindowContent/item_bg/item_icon
          public LUIRawImage m_item_icon;

          //buff_look_wnd/WindowContent/item_name
          public LUIText m_item_name;

          //buff_look_wnd/WindowContent/btnClose
          public LUIButton m_btnClose;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_item_desc = m_Bridge.GetControl(0) as LUIText;
               m_item_bg = m_Bridge.GetControl(1) as LUIImage;
               m_item_icon = m_Bridge.GetControl(2) as LUIRawImage;
               m_item_name = m_Bridge.GetControl(3) as LUIText;
               m_btnClose = m_Bridge.GetControl(4) as LUIButton;
          }
     }
}

