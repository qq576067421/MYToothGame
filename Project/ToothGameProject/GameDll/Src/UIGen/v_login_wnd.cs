//功能：login_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：10/15/2026 15:52:59
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
     public class v_login_wnd:v_base_wnd
     {
          public object m_UserData; 
          //login_wnd/
          public ComponentBridge m_Bridge;

          //login_wnd/WindowContent/btnStartGame
          public LUIButton m_btnStartGame;

          //login_wnd/WindowContent/huawen
          public LUIImage m_huawen;

          //GMTrigger/
          public ComponentBridge m_GMTrigger;

          //login_wnd/WindowContent/txtVersion
          public LUITextMesh m_txtVersion;

          //login_wnd/WindowContent/btnYear
          public LUIButton m_btnYear;

          //login_wnd/WindowContent/btnYear/choose
          public LUIImage m_choose;

          //login_wnd/WindowContent/txtAdvice/txtAdvice_txt_1
          public LUITextMesh m_txtAdvice_txt_1;

          //login_wnd/WindowContent/WirelessEnjoyment
          public LUIImage m_WirelessEnjoyment;

          //login_wnd/WindowContent/WirelessEnjoyment/VIP
          public LUIImage m_VIP;

          //login_wnd/WindowContent/WirelessEnjoyment/Enjoyment_btn
          public LUIButton m_Enjoyment_btn;

          //login_wnd/WindowContent/WirelessEnjoyment/Enjoyment_btn/choose
          public LUIImage m_choose_new1;

          //login_wnd/WindowContent/YearInstruction
          public LUIImage m_YearInstruction;

          //login_wnd/WindowContent/YearInstruction/txtmeshline_1
          public LUITextMesh m_txtmeshline_1;

          //login_wnd/WindowContent/YearInstruction/row_1
          public LUITextMesh m_row_1;

          //login_wnd/WindowContent/YearInstruction/row_1/txtmeshline_1
          public LUITextMesh m_txtmeshline_1_new1;

          //login_wnd/WindowContent/SubscriptionSuccessful
          public LUIImage m_SubscriptionSuccessful;

          //login_wnd/WindowContent/SubscriptionSuccessful/txtmeshline_2
          public LUITextMesh m_txtmeshline_2;

          //login_wnd/WindowContent/SubscriptionSuccessful/Confirm_btn
          public LUIButton m_Confirm_btn;

          //login_wnd/WindowContent/btnStartGame/StartGame_txt
          public LUITextMesh m_StartGame_txt;

          //login_wnd/WindowContent/btnStartGame/choose/StartGameChoose_txt
          public LUITextMesh m_StartGameChoose_txt;

          //login_wnd/WindowContent/txtAdvice
          public LUITextMesh m_txtAdvice;

          //login_wnd/WindowContent/txtAdvice/txtAdvice_txt_2
          public LUITextMesh m_txtAdvice_txt_2;

          //login_wnd/WindowContent/btnYear/year_txt
          public LUITextMesh m_year_txt;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_btnStartGame = m_Bridge.GetControl(0) as LUIButton;
               m_huawen = m_Bridge.GetControl(1) as LUIImage;
               m_GMTrigger = m_Bridge.GetControl(2) as ComponentBridge;
               m_txtVersion = m_Bridge.GetControl(3) as LUITextMesh;
               m_btnYear = m_Bridge.GetControl(4) as LUIButton;
               m_choose = m_Bridge.GetControl(5) as LUIImage;
               m_txtAdvice_txt_1 = m_Bridge.GetControl(6) as LUITextMesh;
               m_WirelessEnjoyment = m_Bridge.GetControl(7) as LUIImage;
               m_VIP = m_Bridge.GetControl(8) as LUIImage;
               m_Enjoyment_btn = m_Bridge.GetControl(9) as LUIButton;
               m_choose_new1 = m_Bridge.GetControl(10) as LUIImage;
               m_YearInstruction = m_Bridge.GetControl(11) as LUIImage;
               m_txtmeshline_1 = m_Bridge.GetControl(12) as LUITextMesh;
               m_row_1 = m_Bridge.GetControl(13) as LUITextMesh;
               m_txtmeshline_1_new1 = m_Bridge.GetControl(14) as LUITextMesh;
               m_SubscriptionSuccessful = m_Bridge.GetControl(15) as LUIImage;
               m_txtmeshline_2 = m_Bridge.GetControl(16) as LUITextMesh;
               m_Confirm_btn = m_Bridge.GetControl(17) as LUIButton;
               m_StartGame_txt = m_Bridge.GetControl(18) as LUITextMesh;
               m_StartGameChoose_txt = m_Bridge.GetControl(19) as LUITextMesh;
               m_txtAdvice = m_Bridge.GetControl(20) as LUITextMesh;
               m_txtAdvice_txt_2 = m_Bridge.GetControl(21) as LUITextMesh;
               m_year_txt = m_Bridge.GetControl(22) as LUITextMesh;
          }
          public class v_GMTrigger:v_base_wnd
          {
               public object m_UserData; 
               //GMTrigger/
               public ComponentBridge m_Bridge;

               //GMTrigger/btnLeftGM
               public LUIButton m_btnLeftGM;

               //GMTrigger/btnRightGM
               public LUIButton m_btnRightGM;

               //GMTrigger/btnTopGM
               public LUIButton m_btnTopGM;

               //GMTrigger/btnBottomGM
               public LUIButton m_btnBottomGM;

               public override void InitComponent(GameObject go)
               {
                    m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
                    m_btnLeftGM = m_Bridge.GetControl(0) as LUIButton;
                    m_btnRightGM = m_Bridge.GetControl(1) as LUIButton;
                    m_btnTopGM = m_Bridge.GetControl(2) as LUIButton;
                    m_btnBottomGM = m_Bridge.GetControl(3) as LUIButton;
               }
          }
     }
}

