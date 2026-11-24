//功能：tower_defend_endless_result_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：11/18/2026 11:48:52
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
     public class v_tower_defend_endless_result_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_endless_result_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_endless_result_wnd/WindowContent/top/txt_title
          public LUITextMesh m_txt_title;

          //tower_defend_endless_result_wnd/WindowContent/top/txt_wave
          public LUITextMesh m_txt_wave;

          //tower_defend_endless_result_wnd/WindowContent/top/txt_best_wave
          public LUITextMesh m_txt_best_wave;

          //tower_defend_endless_result_wnd/WindowContent/center/txt_kill_summary
          public LUITextMesh m_txt_kill_summary;

          //tower_defend_endless_result_wnd/WindowContent/center/txt_kill_detail
          public LUITextMesh m_txt_kill_detail;

          //tower_defend_endless_result_wnd/WindowContent/center/txt_rank_status
          public LUITextMesh m_txt_rank_status;

          //tower_defend_endless_result_wnd/WindowContent/center/txt_reward_kill
          public LUITextMesh m_txt_reward_kill;

          //tower_defend_endless_result_wnd/WindowContent/center/txt_reward_total
          public LUITextMesh m_txt_reward_total;

          //tower_defend_endless_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_continue
          public LUIButton m_btn_continue;

          //tower_defend_endless_result_wnd/WindowContent/result_wnd/center/bottom_layout/btn_return
          public LUIButton m_btn_return;

          //tower_defend_endless_result_wnd/WindowContent/result_wnd/titlebg_win/txt_level
          public LUITextMesh m_txt_level;

          //tower_defend_endless_result_wnd/WindowContent/result_wnd/center/reward/totalBg/totalcoin/txt_reward_kill
          public LUITextMesh m_txt_reward_kill_new1;

          //tower_defend_endless_result_wnd/WindowContent/result_wnd/center/reward/killBg/killcoin/txt_reward_rankScore
          public LUITextMesh m_txt_reward_rankScore;

          //tower_defend_endless_result_wnd/WindowContent/result_wnd/left/ScrollView/Viewport/Content/itemParent/item
          public LUIImage m_item;

          //tower_defend_endless_result_wnd/WindowContent/result_wnd/left/ScrollView/Viewport/Content/itemParent
          public LUIImage m_itemParent;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_txt_title = m_Bridge.GetControl(0) as LUITextMesh;
               m_txt_wave = m_Bridge.GetControl(1) as LUITextMesh;
               m_txt_best_wave = m_Bridge.GetControl(2) as LUITextMesh;
               m_txt_kill_summary = m_Bridge.GetControl(3) as LUITextMesh;
               m_txt_kill_detail = m_Bridge.GetControl(4) as LUITextMesh;
               m_txt_rank_status = m_Bridge.GetControl(5) as LUITextMesh;
               m_txt_reward_kill = m_Bridge.GetControl(6) as LUITextMesh;
               m_txt_reward_total = m_Bridge.GetControl(7) as LUITextMesh;
               m_btn_continue = m_Bridge.GetControl(8) as LUIButton;
               m_btn_return = m_Bridge.GetControl(9) as LUIButton;
               m_txt_level = m_Bridge.GetControl(10) as LUITextMesh;
               m_txt_reward_kill_new1 = m_Bridge.GetControl(11) as LUITextMesh;
               m_txt_reward_rankScore = m_Bridge.GetControl(12) as LUITextMesh;
               m_item = m_Bridge.GetControl(13) as LUIImage;
               m_itemParent = m_Bridge.GetControl(14) as LUIImage;
          }
     }
}

