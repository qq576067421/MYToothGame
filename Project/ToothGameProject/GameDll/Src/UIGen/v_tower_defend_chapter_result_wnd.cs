//功能：tower_defend_chapter_result_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：08/13/2026 17:41:31
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
     public class v_tower_defend_chapter_result_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_chapter_result_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/titlebg_win/txt_title
          public LUITextMesh m_txt_title;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/reward/killBg/killcoin/txt_reward_kill
          public LUITextMesh m_txt_reward_kill;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/reward/totalBg/totalcoin/txt_reward_total
          public LUITextMesh m_txt_reward_total;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_continue
          public LUIButton m_btn_continue;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_return
          public LUIButton m_btn_return;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/reward/totalBg/totalcoin
          public LUIImage m_totalcoin;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/titlebg_win/txt_level
          public LUITextMesh m_txt_level;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/mask
          public LUIImage m_mask;

          //tower_defend_chapter_result_wnd/WindowContent/background
          public LUIImage m_background;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/titlebg_win
          public LUIImage m_titlebg_win;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/titlebg_fail
          public LUIImage m_titlebg_fail;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_continue/unchoose_continue
          public LUITextMesh m_unchoose_continue;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_return/unchoose_return
          public LUITextMesh m_unchoose_return;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/star/0
          public Image m_0;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/star/2
          public Image m_2;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/titlebg_fail/txt_level
          public LUITextMesh m_txt_level_new1;

          //tower_defend_chapter_result_wnd/WindowContent/background/text
          public LUITextMesh m_text;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/star/1
          public Image m_1;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/reward/totalBg
          public LUIImage m_totalBg;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_continue/choose
          public LUIImage m_choose;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_return/choose
          public LUIImage m_choose_new1;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_continue/imgRaise
          public LUIImage m_imgRaise;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/mask/raw_texture
          public RawImage m_raw_texture;

          //tower_defend_chapter_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_continue/choose/unchoose_continue
          public LUITextMesh m_unchoose_continue_new1;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_txt_title = m_Bridge.GetControl(0) as LUITextMesh;
               m_txt_reward_kill = m_Bridge.GetControl(1) as LUITextMesh;
               m_txt_reward_total = m_Bridge.GetControl(2) as LUITextMesh;
               m_btn_continue = m_Bridge.GetControl(3) as LUIButton;
               m_btn_return = m_Bridge.GetControl(4) as LUIButton;
               m_totalcoin = m_Bridge.GetControl(5) as LUIImage;
               m_txt_level = m_Bridge.GetControl(6) as LUITextMesh;
               m_mask = m_Bridge.GetControl(7) as LUIImage;
               m_background = m_Bridge.GetControl(8) as LUIImage;
               m_titlebg_win = m_Bridge.GetControl(9) as LUIImage;
               m_titlebg_fail = m_Bridge.GetControl(10) as LUIImage;
               m_unchoose_continue = m_Bridge.GetControl(11) as LUITextMesh;
               m_unchoose_return = m_Bridge.GetControl(12) as LUITextMesh;
               m_0 = m_Bridge.GetControl(13) as Image;
               m_2 = m_Bridge.GetControl(14) as Image;
               m_txt_level_new1 = m_Bridge.GetControl(15) as LUITextMesh;
               m_text = m_Bridge.GetControl(16) as LUITextMesh;
               m_1 = m_Bridge.GetControl(17) as Image;
               m_totalBg = m_Bridge.GetControl(18) as LUIImage;
               m_choose = m_Bridge.GetControl(19) as LUIImage;
               m_choose_new1 = m_Bridge.GetControl(20) as LUIImage;
               m_imgRaise = m_Bridge.GetControl(21) as LUIImage;
               m_raw_texture = m_Bridge.GetControl(22) as RawImage;
               m_unchoose_continue_new1 = m_Bridge.GetControl(23) as LUITextMesh;
          }
     }
}

