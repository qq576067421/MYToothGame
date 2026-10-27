//功能：tower_defend_giftedness_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：09/23/2026 18:29:59
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
     public class v_tower_defend_giftedness_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_giftedness_wnd/
          public ComponentBridge m_Bridge;

          //item/
          public LUIButton m_item;

          //tower_defend_giftedness_wnd/middle/Viewport/Content
          public LUIImage m_Content;

          //tower_defend_giftedness_wnd/middle/Viewport/Content/Progress/Progress_Fill
          public LUIImage m_Progress_Fill;

          //tower_defend_giftedness_wnd/middle/Viewport/Content/Progress/Progress_di
          public LUIImage m_Progress_di;

          //tower_defend_giftedness_wnd/top/txt_coin
          public LUITextMesh m_txt_coin;

          //tower_defend_giftedness_wnd/ButtonLeft
          public LUIButton m_ButtonLeft;

          //tower_defend_giftedness_wnd/ButtonRight
          public LUIButton m_ButtonRight;

          //tower_defend_giftedness_wnd/ButtonUp
          public LUIButton m_ButtonUp;

          //tower_defend_giftedness_wnd/ButtonDown
          public LUIButton m_ButtonDown;

          //tower_defend_giftedness_wnd/ButtonClick
          public LUIButton m_ButtonClick;

          //tower_defend_giftedness_wnd/middle/Viewport/Content/Progress
          public LUIImage m_Progress;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_item = m_Bridge.GetControl(0) as LUIButton;
               m_Content = m_Bridge.GetControl(1) as LUIImage;
               m_Progress_Fill = m_Bridge.GetControl(2) as LUIImage;
               m_Progress_di = m_Bridge.GetControl(3) as LUIImage;
               m_txt_coin = m_Bridge.GetControl(4) as LUITextMesh;
               m_ButtonLeft = m_Bridge.GetControl(5) as LUIButton;
               m_ButtonRight = m_Bridge.GetControl(6) as LUIButton;
               m_ButtonUp = m_Bridge.GetControl(7) as LUIButton;
               m_ButtonDown = m_Bridge.GetControl(8) as LUIButton;
               m_ButtonClick = m_Bridge.GetControl(9) as LUIButton;
               m_Progress = m_Bridge.GetControl(10) as LUIImage;
          }
          public class v_Item:v_base_wnd
          {
               public object m_UserData; 
               //item/
               public ComponentBridge m_Bridge;

               //item/Branch
               public LUIImage m_Branch;

               //item/Branch/branch_btn
               public LUIButton m_branch_btn;

               //item/Branch/branch_btn/unlock
               public LUIImage m_unlock;

               //item/Branch/branch_btn/lockCoin
               public LUIImage m_lockCoin;

               //item/Unlock
               public LUIImage m_Unlock;

               //item/Unlock/image1
               public LUIImage m_image1;

               //item/Unlock/image2
               public LUIImage m_image2;

               //item/Unlock/image3
               public LUIImage m_image3;

               //item/choose
               public LUIImage m_choose;

               //item/choose/image1
               public LUIImage m_image1_new1;

               //item/choose/image2
               public LUIImage m_image2_new1;

               //item/choose/image3
               public LUIImage m_image3_new1;

               //item/jieshao_TxtMesh
               public LUITextMesh m_jieshao_TxtMesh;

               //item/jieshao_TxtMesh/TxtMesh
               public LUITextMesh m_TxtMesh;

               //item/lockCoin
               public LUIImage m_lockCoin_new1;

               //item/lock/lockImage
               public LUIImage m_lockImage;

               //item/Branch/branch_btn/lock/lockImage
               public LUIImage m_lockImage_new1;

               //item/Branch/branch_btn/unlock/Progress_Fill
               public LUIImage m_Progress_Fill;

               //item/lockCoin/lockCoin_TxtMesh
               public LUITextMesh m_lockCoin_TxtMesh;

               //item/Branch/branch_btn/lockCoin/lockCoin_TxtMesh
               public LUITextMesh m_lockCoin_TxtMesh_new1;

               //item/Branch/branch_btn/unlock/branchUnlock_Txt
               public LUITextMesh m_branchUnlock_Txt;

               //item/Branch/branch_btn/choose/branchChoose_Txt
               public LUITextMesh m_branchChoose_Txt;

               public override void InitComponent(GameObject go)
               {
                    m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
                    m_Branch = m_Bridge.GetControl(0) as LUIImage;
                    m_branch_btn = m_Bridge.GetControl(1) as LUIButton;
                    m_unlock = m_Bridge.GetControl(2) as LUIImage;
                    m_lockCoin = m_Bridge.GetControl(3) as LUIImage;
                    m_Unlock = m_Bridge.GetControl(4) as LUIImage;
                    m_image1 = m_Bridge.GetControl(5) as LUIImage;
                    m_image2 = m_Bridge.GetControl(6) as LUIImage;
                    m_image3 = m_Bridge.GetControl(7) as LUIImage;
                    m_choose = m_Bridge.GetControl(8) as LUIImage;
                    m_image1_new1 = m_Bridge.GetControl(9) as LUIImage;
                    m_image2_new1 = m_Bridge.GetControl(10) as LUIImage;
                    m_image3_new1 = m_Bridge.GetControl(11) as LUIImage;
                    m_jieshao_TxtMesh = m_Bridge.GetControl(12) as LUITextMesh;
                    m_TxtMesh = m_Bridge.GetControl(13) as LUITextMesh;
                    m_lockCoin_new1 = m_Bridge.GetControl(14) as LUIImage;
                    m_lockImage = m_Bridge.GetControl(15) as LUIImage;
                    m_lockImage_new1 = m_Bridge.GetControl(16) as LUIImage;
                    m_Progress_Fill = m_Bridge.GetControl(17) as LUIImage;
                    m_lockCoin_TxtMesh = m_Bridge.GetControl(18) as LUITextMesh;
                    m_lockCoin_TxtMesh_new1 = m_Bridge.GetControl(19) as LUITextMesh;
                    m_branchUnlock_Txt = m_Bridge.GetControl(20) as LUITextMesh;
                    m_branchChoose_Txt = m_Bridge.GetControl(21) as LUITextMesh;
               }
          }
     }
}

