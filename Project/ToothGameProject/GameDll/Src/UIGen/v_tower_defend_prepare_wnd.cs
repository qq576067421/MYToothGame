//功能：tower_defend_prepare_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：08/21/2026 10:35:17
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
     public class v_tower_defend_prepare_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_prepare_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_prepare_wnd/WindowContent/txtInfo
          public LUITextMesh m_txtInfo;

          //Player0/
          public ComponentBridge m_Player0;

          //tower_defend_prepare_wnd/WindowContent/NoPlayer
          public LUIRawImage m_NoPlayer;

          //tower_defend_prepare_wnd/WindowContent/SystemPlayer
          public LUIRawImage m_SystemPlayer;

          //tower_defend_prepare_wnd/WindowContent/NotEqualPlayer
          public LUIRawImage m_NotEqualPlayer;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_txtInfo = m_Bridge.GetControl(0) as LUITextMesh;
               m_Player0 = m_Bridge.GetControl(1) as ComponentBridge;
               m_NoPlayer = m_Bridge.GetControl(2) as LUIRawImage;
               m_SystemPlayer = m_Bridge.GetControl(3) as LUIRawImage;
               m_NotEqualPlayer = m_Bridge.GetControl(4) as LUIRawImage;
          }
          public class v_PlayerSlot:v_base_wnd
          {
               public object m_UserData; 
               //Player0/
               public ComponentBridge m_Bridge;

               //Player0/choose
               public LUIImage m_choose;

               //Player0/Character
               public LUIImage m_Character;

               //Player0/bg
               public LUIImage m_bg;

               //Player0/PlayerPre
               public LUIImage m_PlayerPre;

               //Player0/PlayerPre/Fill
               public LUIImage m_Fill;

               //Player0/prepare/txt_prepare
               public LUITextMesh m_txt_prepare;

               //Player0/chooseButton/btnLeft
               public LUIButton m_btnLeft;

               //Player0/chooseButton/btnRight
               public LUIButton m_btnRight;

               //Player0/prepare
               public LUIImage m_prepare;

               //Player0/headInfo
               public LUITextMesh m_headInfo;

               //Player0/headBg/choose
               public LUIImage m_choose_new1;

               //Player0/headBg/mask/head
               public LUIButton m_head;

               public override void InitComponent(GameObject go)
               {
                    m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
                    m_choose = m_Bridge.GetControl(0) as LUIImage;
                    m_Character = m_Bridge.GetControl(1) as LUIImage;
                    m_bg = m_Bridge.GetControl(2) as LUIImage;
                    m_PlayerPre = m_Bridge.GetControl(3) as LUIImage;
                    m_Fill = m_Bridge.GetControl(4) as LUIImage;
                    m_txt_prepare = m_Bridge.GetControl(5) as LUITextMesh;
                    m_btnLeft = m_Bridge.GetControl(6) as LUIButton;
                    m_btnRight = m_Bridge.GetControl(7) as LUIButton;
                    m_prepare = m_Bridge.GetControl(8) as LUIImage;
                    m_headInfo = m_Bridge.GetControl(9) as LUITextMesh;
                    m_choose_new1 = m_Bridge.GetControl(10) as LUIImage;
                    m_head = m_Bridge.GetControl(11) as LUIButton;
               }
          }
     }
}

