//功能：tower_defend_battle_hud_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：08/17/2026 15:53:00
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
     public class v_tower_defend_battle_hud_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_battle_hud_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_battle_hud_wnd/WindowContent/top/txt_stage
          public LUITextMesh m_txt_stage;

          //tower_defend_battle_hud_wnd/WindowContent/top/txt_wave
          public LUITextMesh m_txt_wave;

          //tower_defend_battle_hud_wnd/WindowContent/top/txt_team_exp
          public LUITextMesh m_txt_team_exp;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg/txt_base_hp
          public LUITextMesh m_txt_base_hp;

          //tower_defend_battle_hud_wnd/WindowContent/top/txt_prepare
          public LUITextMesh m_txt_prepare;

          //tower_defend_battle_hud_wnd/WindowContent/top/bg_level_count/txt_level_count
          public LUITextMesh m_txt_level_count;

          //tower_defend_battle_hud_wnd/WindowContent/top/btn_pause
          public LUIButton m_btn_pause;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_team_exp
          public Slider m_slider_team_exp;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg/slider_base_hp
          public Slider m_slider_base_hp;

          //tower_defend_battle_hud_wnd/WindowContent/top/btn_pause/txtmeshline_1
          public LUITextMesh m_txtmeshline_1;

          //tower_defend_battle_hud_wnd/WindowContent/top/fightInfo/slider_team_exp_root
          public LUIImage m_slider_team_exp_root;

          //tower_defend_battle_hud_wnd/WindowContent/top/fightInfo/slider_team_exp_root/team_exp
          public LUIImage m_team_exp;

          //tower_defend_battle_hud_wnd/WindowContent/top/fightInfo/all_players_dps
          public UITransform m_all_players_dps;

          //PlayerSlot3/
          public ComponentBridge m_PlayerSlot3;

          //PlayerSlot2/
          public ComponentBridge m_PlayerSlot2;

          //PlayerSlot1/
          public ComponentBridge m_PlayerSlot1;

          //PlayerSlot0/
          public ComponentBridge m_PlayerSlot0;

          //tower_defend_battle_hud_wnd/WindowContent/top/fightInfo
          public UITransform m_fightInfo;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg
          public LUIImage m_slider_base_bg;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg/luiimage_3
          public LUIImage m_luiimage_3;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg/slider_base_hp/Fill_0
          public LUIImage m_Fill_0;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg/slider_base_hp/Fill_1
          public LUIImage m_Fill_1;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg/slider_base_hp/Fill_2
          public LUIImage m_Fill_2;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg/slider_base_hp/Fill_3
          public LUIImage m_Fill_3;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg/slider_base_hp/Fill_4
          public LUIImage m_Fill_4;

          //dps_player0/
          public ComponentBridge m_dps_player0;

          //dps_player1/
          public ComponentBridge m_dps_player1;

          //dps_player2/
          public ComponentBridge m_dps_player2;

          //dps_player3/
          public ComponentBridge m_dps_player3;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_base_bg/slider_base_hp/Fill_Slowly
          public Slider m_Fill_Slowly;

          //tower_defend_battle_hud_wnd/WindowContent/top
          public UITransform m_top;

          //tower_defend_battle_hud_wnd/WindowContent/top/fightInfo/slider_team_exp_root/eff_bangbang_star
          public UITransform m_eff_bangbang_star;

          //tower_defend_battle_hud_wnd/WindowContent/top/fightInfo/slider_team_exp_root/eff_xingxing
          public UITransform m_eff_xingxing;

          //tower_defend_battle_hud_wnd/WindowContent/PlayerSlots
          public UITransform m_PlayerSlots;

          //tower_defend_battle_hud_wnd/WindowContent/RewardCoinParent/showRewardCoin
          public LUIImage m_showRewardCoin;

          //tower_defend_battle_hud_wnd/WindowContent/RewardCoinParent
          public LUIImage m_RewardCoinParent;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_lollipop_bg/txt_lollipop_hp
          public LUITextMesh m_txt_lollipop_hp;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_lollipop_bg/slider_lollipop_hp/lollipopFill_Slowly
          public Slider m_lollipopFill_Slowly;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_lollipop_bg/slider_lollipop_hp
          public Slider m_slider_lollipop_hp;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_lollipop_bg/slider_lollipop_hp/lollipopFill_0
          public LUIImage m_lollipopFill_0;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_lollipop_bg/slider_lollipop_hp/lollipopFill_1
          public LUIImage m_lollipopFill_1;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_lollipop_bg/slider_lollipop_hp/lollipopFill_2
          public LUIImage m_lollipopFill_2;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_lollipop_bg/slider_lollipop_hp/lollipopFill_3
          public LUIImage m_lollipopFill_3;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_lollipop_bg/slider_lollipop_hp/lollipopFill_4
          public LUIImage m_lollipopFill_4;

          //tower_defend_battle_hud_wnd/WindowContent/top/slider_lollipop_bg
          public LUIImage m_slider_lollipop_bg;

          //tower_defend_battle_hud_wnd/WindowContent/baseBlood/baseBlood_Slider/baseBlood_Txt
          public LUITextMesh m_baseBlood_Txt;

          //tower_defend_battle_hud_wnd/WindowContent/baseBlood/baseBlood_Slider/baseBlood_Fill
          public LUIImage m_baseBlood_Fill;

          //tower_defend_battle_hud_wnd/WindowContent/top/fightInfo/xingxingParent
          public LUIImage m_xingxingParent;

          //tower_defend_battle_hud_wnd/WindowContent/baseHitEff
          public LUIImage m_baseHitEff;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_txt_stage = m_Bridge.GetControl(0) as LUITextMesh;
               m_txt_wave = m_Bridge.GetControl(1) as LUITextMesh;
               m_txt_team_exp = m_Bridge.GetControl(2) as LUITextMesh;
               m_txt_base_hp = m_Bridge.GetControl(3) as LUITextMesh;
               m_txt_prepare = m_Bridge.GetControl(4) as LUITextMesh;
               m_txt_level_count = m_Bridge.GetControl(5) as LUITextMesh;
               m_btn_pause = m_Bridge.GetControl(6) as LUIButton;
               m_slider_team_exp = m_Bridge.GetControl(7) as Slider;
               m_slider_base_hp = m_Bridge.GetControl(8) as Slider;
               m_txtmeshline_1 = m_Bridge.GetControl(9) as LUITextMesh;
               m_slider_team_exp_root = m_Bridge.GetControl(10) as LUIImage;
               m_team_exp = m_Bridge.GetControl(11) as LUIImage;
               m_all_players_dps = m_Bridge.GetControl(12) as UITransform;
               m_PlayerSlot3 = m_Bridge.GetControl(13) as ComponentBridge;
               m_PlayerSlot2 = m_Bridge.GetControl(14) as ComponentBridge;
               m_PlayerSlot1 = m_Bridge.GetControl(15) as ComponentBridge;
               m_PlayerSlot0 = m_Bridge.GetControl(16) as ComponentBridge;
               m_fightInfo = m_Bridge.GetControl(17) as UITransform;
               m_slider_base_bg = m_Bridge.GetControl(18) as LUIImage;
               m_luiimage_3 = m_Bridge.GetControl(19) as LUIImage;
               m_Fill_0 = m_Bridge.GetControl(20) as LUIImage;
               m_Fill_1 = m_Bridge.GetControl(21) as LUIImage;
               m_Fill_2 = m_Bridge.GetControl(22) as LUIImage;
               m_Fill_3 = m_Bridge.GetControl(23) as LUIImage;
               m_Fill_4 = m_Bridge.GetControl(24) as LUIImage;
               m_dps_player0 = m_Bridge.GetControl(25) as ComponentBridge;
               m_dps_player1 = m_Bridge.GetControl(26) as ComponentBridge;
               m_dps_player2 = m_Bridge.GetControl(27) as ComponentBridge;
               m_dps_player3 = m_Bridge.GetControl(28) as ComponentBridge;
               m_Fill_Slowly = m_Bridge.GetControl(29) as Slider;
               m_top = m_Bridge.GetControl(30) as UITransform;
               m_eff_bangbang_star = m_Bridge.GetControl(31) as UITransform;
               m_eff_xingxing = m_Bridge.GetControl(32) as UITransform;
               m_PlayerSlots = m_Bridge.GetControl(33) as UITransform;
               m_showRewardCoin = m_Bridge.GetControl(34) as LUIImage;
               m_RewardCoinParent = m_Bridge.GetControl(35) as LUIImage;
               m_txt_lollipop_hp = m_Bridge.GetControl(36) as LUITextMesh;
               m_lollipopFill_Slowly = m_Bridge.GetControl(37) as Slider;
               m_slider_lollipop_hp = m_Bridge.GetControl(38) as Slider;
               m_lollipopFill_0 = m_Bridge.GetControl(39) as LUIImage;
               m_lollipopFill_1 = m_Bridge.GetControl(40) as LUIImage;
               m_lollipopFill_2 = m_Bridge.GetControl(41) as LUIImage;
               m_lollipopFill_3 = m_Bridge.GetControl(42) as LUIImage;
               m_lollipopFill_4 = m_Bridge.GetControl(43) as LUIImage;
               m_slider_lollipop_bg = m_Bridge.GetControl(44) as LUIImage;
               m_baseBlood_Txt = m_Bridge.GetControl(45) as LUITextMesh;
               m_baseBlood_Fill = m_Bridge.GetControl(46) as LUIImage;
               m_xingxingParent = m_Bridge.GetControl(47) as LUIImage;
               m_baseHitEff = m_Bridge.GetControl(48) as LUIImage;
          }
          public class v_dps_player:v_base_wnd
          {
               public object m_UserData; 
               //dps_player0/
               public ComponentBridge m_Bridge;

               //dps_player0/mask
               public Image m_mask;

               //dps_player0/mask/light_0
               public LUIImage m_light_0;

               //dps_player0/luiimage2
               public LUIImage m_luiimage2;

               //dps_player0/luiimage1
               public LUIImage m_luiimage1;

               //dps_player0/txt_dps0
               public LUITextMesh m_txt_dps0;

               //dps_player0/txt_rank0
               public LUITextMesh m_txt_rank0;

               public override void InitComponent(GameObject go)
               {
                    m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
                    m_mask = m_Bridge.GetControl(0) as Image;
                    m_light_0 = m_Bridge.GetControl(1) as LUIImage;
                    m_luiimage2 = m_Bridge.GetControl(2) as LUIImage;
                    m_luiimage1 = m_Bridge.GetControl(3) as LUIImage;
                    m_txt_dps0 = m_Bridge.GetControl(4) as LUITextMesh;
                    m_txt_rank0 = m_Bridge.GetControl(5) as LUITextMesh;
               }
          }
          public class v_PlayerSlot:v_base_wnd
          {
               public object m_UserData; 
               //PlayerSlot0/
               public ComponentBridge m_Bridge;

               //PlayerSlot0/txt_level
               public LUITextMesh m_txt_level;

               //PlayerSlot0/skill_cd/txt_skill_cd
               public LUITextMesh m_txt_skill_cd;

               //PlayerSlot0/bg
               public LUIImage m_bg;

               //PlayerSlot0/skill_cd
               public LUIImage m_skill_cd;

               //PlayerSlot0/skill_cd/fill_skill_cd
               public LUIImage m_fill_skill_cd;

               //PlayerSlot0/fill_energy
               public LUIImage m_fill_energy;

               //PlayerSlot0/eff_man
               public UITransform m_eff_man;

               //PlayerSlot0/debugPanel/txtBoneConnect
               public LUITextMesh m_txtBoneConnect;

               //PlayerSlot0/debugPanel
               public LUIImage m_debugPanel;

               //PlayerSlot0/debugPanel/txtBoneState
               public LUITextMesh m_txtBoneState;

               //PlayerSlot0/headImage/headView
               public UITransform m_headView;

               //PlayerSlot0/headImage
               public LUIImage m_headImage;

               public override void InitComponent(GameObject go)
               {
                    m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
                    m_txt_level = m_Bridge.GetControl(0) as LUITextMesh;
                    m_txt_skill_cd = m_Bridge.GetControl(1) as LUITextMesh;
                    m_bg = m_Bridge.GetControl(2) as LUIImage;
                    m_skill_cd = m_Bridge.GetControl(3) as LUIImage;
                    m_fill_skill_cd = m_Bridge.GetControl(4) as LUIImage;
                    m_fill_energy = m_Bridge.GetControl(5) as LUIImage;
                    m_eff_man = m_Bridge.GetControl(6) as UITransform;
                    m_txtBoneConnect = m_Bridge.GetControl(7) as LUITextMesh;
                    m_debugPanel = m_Bridge.GetControl(8) as LUIImage;
                    m_txtBoneState = m_Bridge.GetControl(9) as LUITextMesh;
                    m_headView = m_Bridge.GetControl(10) as UITransform;
                    m_headImage = m_Bridge.GetControl(11) as LUIImage;
               }
          }
     }
}

