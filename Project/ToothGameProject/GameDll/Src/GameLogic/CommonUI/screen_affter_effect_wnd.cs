using DG.Tweening;
using GameDll;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;

namespace GameHot
{
    public class screen_affter_effect_wnd : WindowBase
    {
        private static screen_affter_effect_wnd m_Instance;
        public static screen_affter_effect_wnd OpenWindow()
        {
            if (m_Instance != null)
            {
                return m_Instance;
            }
            else
            {
                m_Instance = UIManager.OpenWindowEX<screen_affter_effect_wnd>(null);
                return m_Instance;
            }
        }
        public static void CloseWindow()
        {
            if (m_Instance != null)
            {
                UIManager.CloseWindow(m_Instance);
                m_Instance = null;
            }
        }
        private Tweener m_Tween;
        private v_screen_affter_effect_wnd m_View;

        private float m_ToValue;
        private float m_FromValue;
        private float m_Duration;
        private Action m_FinishCall;
        private bool m_Block = true;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();

            m_Layer = WindowLayer.AffterEffect;
            __CustomUIPrefabDir = UIPrefabDirs.common;
        }
        protected override void OnInitComponent()
        {
            m_View = new v_screen_affter_effect_wnd();
            m_View.InitComponent(__GetWindowObj());
        }
        protected override void OnOpen()
        {
            OnSetTweenValue();

        }
        public void SetTweenValue(float from, float to, float time, Action call)
        {
            m_FromValue = from;
            m_ToValue = to;
            m_Duration = time;
            m_FinishCall = call;

            OnSetTweenValue();
            SetBlock(m_Block);
        }

        private void OnSetTweenValue()
        {
            //m_Tween = m_BlockImage.DOFade(m_ToValue, m_Duration).ChangeStartValue(m_FromValue);
            //m_Tween.OnComplete(() =>
            //{
            //    m_Tween = null;
            //    if (m_FinishCall != null)
            //    {
            //        m_FinishCall();
            //    }
            //});
        }

        public void SetBlock(bool block)
        {
            m_Block = block;
            m_View.m_lcl_alpha_image.raycastTarget = block;
        }

        protected override void OnClose()
        {
            if (m_Tween != null)
            {
                m_Tween.Kill();
                m_Tween = null;
            }
        }
        protected override void OnDestroy()
        {


        }
    }
}
