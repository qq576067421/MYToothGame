//功能：screen_affter_effect_wnd的窗口配置文件
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
     public class v_screen_affter_effect_wnd:v_base_wnd
     {
          public object m_UserData; 
          //screen_affter_effect_wnd/
          public ComponentBridge m_Bridge;

          //screen_affter_effect_wnd/WindowContent/lcl_alpha_image
          public LUIImage m_lcl_alpha_image;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_lcl_alpha_image = m_Bridge.GetControl(0) as LUIImage;
          }
     }
}

