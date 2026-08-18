//功能：guide_wnd的窗口配置文件
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
     public class v_guide_wnd:v_base_wnd
     {
          public object m_UserData; 
          //guide_wnd/
          public ComponentBridge m_Bridge;

          //guide_wnd/WindowContent/lcl_guide_arrow/lcl_img_arrow0
          public LUIImage m_lcl_img_arrow0;

          //guide_wnd/WindowContent/fake_target
          public LUIImage m_fake_target;

          //guide_wnd/WindowContent/lcl_guide_arrow
          public UITransform m_lcl_guide_arrow;

          //guide_wnd/WindowContent/guide_mask
          public Image m_guide_mask;

          //guide_wnd/WindowContent/btnJump
          public LUIButton m_btnJump;

          //guide_wnd/WindowContent/desc_rect/imgDesc/txtDesc
          public LUIText m_txtDesc;

          //guide_wnd/WindowContent/desc_rect
          public UITransform m_desc_rect;

          //guide_wnd/WindowContent/desc_rect/imgDesc
          public LUIImage m_imgDesc;

          //guide_wnd/WindowContent/lcl_guide_arrow/lcl_img_arrow2
          public LUIImage m_lcl_img_arrow2;

          //guide_wnd/WindowContent/lcl_guide_arrow/lcl_img_arrow1
          public LUIImage m_lcl_img_arrow1;

          //guide_wnd/WindowContent/lcl_guide_arrow/lcl_img_arrow3
          public LUIImage m_lcl_img_arrow3;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_lcl_img_arrow0 = m_Bridge.GetControl(0) as LUIImage;
               m_fake_target = m_Bridge.GetControl(1) as LUIImage;
               m_lcl_guide_arrow = m_Bridge.GetControl(2) as UITransform;
               m_guide_mask = m_Bridge.GetControl(3) as Image;
               m_btnJump = m_Bridge.GetControl(4) as LUIButton;
               m_txtDesc = m_Bridge.GetControl(5) as LUIText;
               m_desc_rect = m_Bridge.GetControl(6) as UITransform;
               m_imgDesc = m_Bridge.GetControl(7) as LUIImage;
               m_lcl_img_arrow2 = m_Bridge.GetControl(8) as LUIImage;
               m_lcl_img_arrow1 = m_Bridge.GetControl(9) as LUIImage;
               m_lcl_img_arrow3 = m_Bridge.GetControl(10) as LUIImage;
          }
     }
}

