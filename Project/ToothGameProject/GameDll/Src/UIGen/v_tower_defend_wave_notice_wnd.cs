//功能：tower_defend_wave_notice_wnd的窗口配置文件
//工具作者：lichunlin
//生成时间：08/20/2026 11:17:51
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
     public class v_tower_defend_wave_notice_wnd:v_base_wnd
     {
          public object m_UserData; 
          //tower_defend_wave_notice_wnd/
          public ComponentBridge m_Bridge;

          //tower_defend_wave_notice_wnd/WindowContent/center/waveNotice/wave/wave_txt
          public LUITextMesh m_wave_txt;

          //tower_defend_wave_notice_wnd/WindowContent/center/Notice
          public UITransform m_Notice;

          //tower_defend_wave_notice_wnd/WindowContent/center/Notice/bg1
          public LUIImage m_bg1;

          //tower_defend_wave_notice_wnd/WindowContent/center/waveNotice
          public LUIImage m_waveNotice;

          //tower_defend_wave_notice_wnd/WindowContent/center/waveNotice/wave
          public LUIImage m_wave;

          //tower_defend_wave_notice_wnd/WindowContent/center/waveNotice/bossNotice
          public LUIImage m_bossNotice;

          //tower_defend_wave_notice_wnd/WindowContent/center/waveNotice/bossNotice/boss_txt
          public LUITextMesh m_boss_txt;

          //tower_defend_wave_notice_wnd/WindowContent/center/txt_notice
          public LUITextMesh m_txt_notice;

          //tower_defend_wave_notice_wnd/WindowContent/center/waveNotice/wave/waveBg
          public LUIImage m_waveBg;

          //tower_defend_wave_notice_wnd/WindowContent/center/waveNotice/bossNotice/bossNoticeBg
          public LUIImage m_bossNoticeBg;

          //tower_defend_wave_notice_wnd/WindowContent/center/waveNotice/bossNotice/bossNoticeBg/bossArrow
          public LUIImage m_bossArrow;

          public override void InitComponent(GameObject go)
          {
               m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
               m_wave_txt = m_Bridge.GetControl(0) as LUITextMesh;
               m_Notice = m_Bridge.GetControl(1) as UITransform;
               m_bg1 = m_Bridge.GetControl(2) as LUIImage;
               m_waveNotice = m_Bridge.GetControl(3) as LUIImage;
               m_wave = m_Bridge.GetControl(4) as LUIImage;
               m_bossNotice = m_Bridge.GetControl(5) as LUIImage;
               m_boss_txt = m_Bridge.GetControl(6) as LUITextMesh;
               m_txt_notice = m_Bridge.GetControl(7) as LUITextMesh;
               m_waveBg = m_Bridge.GetControl(8) as LUIImage;
               m_bossNoticeBg = m_Bridge.GetControl(9) as LUIImage;
               m_bossArrow = m_Bridge.GetControl(10) as LUIImage;
          }
     }
}

