//功能：year_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：10/15/2026 10:58:29
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
     public class v_year_wnd:v_base_wnd
     {
          public object m_UserData; 
          //year_wnd/
          public ComponentBridge m_Bridge;

          //year_wnd/WindowContent/btnReturn
          public LUIButton m_btnReturn;

          //year_wnd/WindowContent/YearInstruction/Instruction_title
          public LUITextMesh m_Instruction_title;

          //year_wnd/WindowContent/YearInstruction/ScrollView/Viewport/Content/Instruction_1
          public LUITextMesh m_Instruction_1;

          //year_wnd/WindowContent/YearInstruction/ScrollView/Viewport/Content/Instruction_2
          public LUITextMesh m_Instruction_2;

          //year_wnd/WindowContent/YearInstruction/ScrollView/Viewport/Content/Instruction_3
          public LUITextMesh m_Instruction_3;

          //year_wnd/WindowContent/YearInstruction/ScrollView/Viewport/Content/Instruction_4
          public LUITextMesh m_Instruction_4;

          //year_wnd/WindowContent/YearInstruction/ScrollView/Viewport/Content/Instruction_5
          public LUITextMesh m_Instruction_5;

          //year_wnd/WindowContent/YearInstruction/ScrollView/Viewport/Content
          public LUIImage m_Content;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_btnReturn = m_Bridge.GetControl(0) as LUIButton;
               m_Instruction_title = m_Bridge.GetControl(1) as LUITextMesh;
               m_Instruction_1 = m_Bridge.GetControl(2) as LUITextMesh;
               m_Instruction_2 = m_Bridge.GetControl(3) as LUITextMesh;
               m_Instruction_3 = m_Bridge.GetControl(4) as LUITextMesh;
               m_Instruction_4 = m_Bridge.GetControl(5) as LUITextMesh;
               m_Instruction_5 = m_Bridge.GetControl(6) as LUITextMesh;
               m_Content = m_Bridge.GetControl(7) as LUIImage;
          }
     }
}

