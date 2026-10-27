//功能：tower_defend_lvchoose_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：10/15/2026 17:15:30
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
     public class v_tower_defend_lvchoose_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_lvchoose_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_lvchoose_wnd/WindowContent/top/txt_title
          public LUITextMesh m_txt_title;

          //tower_defend_lvchoose_wnd/WindowContent/top/luiimage_2
          public LUIImage m_luiimage_2;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level5/Content_5
          public LUIButton m_Content_5;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level4/Content_4
          public LUIButton m_Content_4;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level3/Content_3
          public LUIButton m_Content_3;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level2/Content_2
          public LUIButton m_Content_2;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level1/Content_1
          public LUIButton m_Content_1;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level1/Content_1/Topic_1/btn_toggle_choose
          public LUIButton m_btn_toggle_choose;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level2/Content_2/Topic_1/btn_toggle_choose
          public LUIButton m_btn_toggle_choose_new1;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level3/Content_3/Topic_1/btn_toggle_choose
          public LUIButton m_btn_toggle_choose_new2;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level4/Content_4/Topic_1/btn_toggle_choose
          public LUIButton m_btn_toggle_choose_new3;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level5/Content_5/Topic_1/btn_toggle_choose
          public LUIButton m_btn_toggle_choose_new4;

          //tower_defend_lvchoose_wnd/WindowContent/btnLeft
          public LUIButton m_btnLeft;

          //tower_defend_lvchoose_wnd/WindowContent/btnRight
          public LUIButton m_btnRight;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level1/Content_1/Topic_1/lock
          public LUIImage m_lock;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level2/Content_2/Topic_1/lock
          public LUIImage m_lock_new1;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level3/Content_3/Topic_1/lock
          public LUIImage m_lock_new2;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level4/Content_4/Topic_1/lock
          public LUIImage m_lock_new3;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level5/Content_5/Topic_1/lock
          public LUIImage m_lock_new4;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/huawen
          public LUIImage m_huawen;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level1/Content_1/Topic_1/stars1
          public LUIImage m_stars1;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level2/Content_2/Topic_1/stars2
          public LUIImage m_stars2;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level3/Content_3/Topic_1/stars3
          public LUIImage m_stars3;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level4/Content_4/Topic_1/stars4
          public LUIImage m_stars4;

          //tower_defend_lvchoose_wnd/WindowContent/ImagesPresentationWorldSpaceCanvas/Viewport/Images/ImagesPanel/ImagesList/Level5/Content_5/Topic_1/stars5
          public LUIImage m_stars5;

          //tower_defend_lvchoose_wnd/WindowContent/txt_desc
          public LUITextMesh m_txt_desc;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_txt_title = m_Bridge.GetControl(0) as LUITextMesh;
               m_luiimage_2 = m_Bridge.GetControl(1) as LUIImage;
               m_Content_5 = m_Bridge.GetControl(2) as LUIButton;
               m_Content_4 = m_Bridge.GetControl(3) as LUIButton;
               m_Content_3 = m_Bridge.GetControl(4) as LUIButton;
               m_Content_2 = m_Bridge.GetControl(5) as LUIButton;
               m_Content_1 = m_Bridge.GetControl(6) as LUIButton;
               m_btn_toggle_choose = m_Bridge.GetControl(7) as LUIButton;
               m_btn_toggle_choose_new1 = m_Bridge.GetControl(8) as LUIButton;
               m_btn_toggle_choose_new2 = m_Bridge.GetControl(9) as LUIButton;
               m_btn_toggle_choose_new3 = m_Bridge.GetControl(10) as LUIButton;
               m_btn_toggle_choose_new4 = m_Bridge.GetControl(11) as LUIButton;
               m_btnLeft = m_Bridge.GetControl(12) as LUIButton;
               m_btnRight = m_Bridge.GetControl(13) as LUIButton;
               m_lock = m_Bridge.GetControl(14) as LUIImage;
               m_lock_new1 = m_Bridge.GetControl(15) as LUIImage;
               m_lock_new2 = m_Bridge.GetControl(16) as LUIImage;
               m_lock_new3 = m_Bridge.GetControl(17) as LUIImage;
               m_lock_new4 = m_Bridge.GetControl(18) as LUIImage;
               m_huawen = m_Bridge.GetControl(19) as LUIImage;
               m_stars1 = m_Bridge.GetControl(20) as LUIImage;
               m_stars2 = m_Bridge.GetControl(21) as LUIImage;
               m_stars3 = m_Bridge.GetControl(22) as LUIImage;
               m_stars4 = m_Bridge.GetControl(23) as LUIImage;
               m_stars5 = m_Bridge.GetControl(24) as LUIImage;
               m_txt_desc = m_Bridge.GetControl(25) as LUITextMesh;
          }
     }
}

