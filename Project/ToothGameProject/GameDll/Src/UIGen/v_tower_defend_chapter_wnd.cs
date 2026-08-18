//功能：tower_defend_chapter_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：08/13/2026 11:49:26
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
     public class v_tower_defend_chapter_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_chapter_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_chapter_wnd/WindowContent/Level/luiimage_6
          public LUIImage m_luiimage_6;

          //btn_Lv0/
          public ComponentBridge m_btn_Lv0;

          //btn_Lv1/
          public ComponentBridge m_btn_Lv1;

          //btn_Lv2/
          public ComponentBridge m_btn_Lv2;

          //btn_Lv3/
          public ComponentBridge m_btn_Lv3;

          //btn_Lv4/
          public ComponentBridge m_btn_Lv4;

          //tower_defend_chapter_wnd/WindowContent/Level/btnLeftRotation
          public LUIButton m_btnLeftRotation;

          //tower_defend_chapter_wnd/WindowContent/Level/btnRightRotation
          public LUIButton m_btnRightRotation;

          //tower_defend_chapter_wnd/WindowContent/Level/btnOk
          public LUIButton m_btnOk;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_luiimage_6 = m_Bridge.GetControl(0) as LUIImage;
               m_btn_Lv0 = m_Bridge.GetControl(1) as ComponentBridge;
               m_btn_Lv1 = m_Bridge.GetControl(2) as ComponentBridge;
               m_btn_Lv2 = m_Bridge.GetControl(3) as ComponentBridge;
               m_btn_Lv3 = m_Bridge.GetControl(4) as ComponentBridge;
               m_btn_Lv4 = m_Bridge.GetControl(5) as ComponentBridge;
               m_btnLeftRotation = m_Bridge.GetControl(6) as LUIButton;
               m_btnRightRotation = m_Bridge.GetControl(7) as LUIButton;
               m_btnOk = m_Bridge.GetControl(8) as LUIButton;
          }
          public class v_Btn_Lv:v_base_wnd
          {
               public object m_UserData; 
               //btn_Lv0/
               public ComponentBridge m_Bridge;

               //btn_Lv0/UnLock
               public LUIImage m_UnLock;

               //btn_Lv0/choose
               public LUIImage m_choose;

               //btn_Lv0/Lock
               public LUIImage m_Lock;

               //btn_Lv0/txt
               public LUITextMesh m_txt;

               public override void InitComponent(GameObject go)
               {
                    m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
                    m_UnLock = m_Bridge.GetControl(0) as LUIImage;
                    m_choose = m_Bridge.GetControl(1) as LUIImage;
                    m_Lock = m_Bridge.GetControl(2) as LUIImage;
                    m_txt = m_Bridge.GetControl(3) as LUITextMesh;
               }
          }
     }
}

