
using GameDll;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;

namespace LCL
{
    public class HpText : MonoBehaviour
    {
        public enum State
        {
            Cache,
            Playing
        }
        public int m_Id;
        public int m_OwnerEntityId = -1;
        public LUITextMesh m_Text;
        public Image m_Image;
        public RectTransform m_Trans;
        public RectTransform m_Parent;
        public Vector3 m_StartPosition;
        public Vector3 m_EndPosition;
        public float m_Size;
        public float m_StartTime;
        public float m_TweenTimeTotal = 1.0f;
        public Action<HpText> m_OnComplete;
        public HpTextType m_Type;
        public State m_State = State.Cache;
        public Camera m_WorldCamera;
        public Camera m_UICamera;
        public CanvasGroup m_CanvasGroup;
        private bool m_HasDefaultTextColor;
        private Color m_DefaultTextColor = Color.white;
        void Start()
        {

        }

        public void SetTextColor(Color color)
        {
            CacheDefaultTextColor();
            if (m_Text != null)
            {
                m_Text.color = color;
            }
        }

        public void ResetTextColor()
        {
            CacheDefaultTextColor();
            if (m_Text != null)
            {
                m_Text.color = m_DefaultTextColor;
            }
        }

        private void CacheDefaultTextColor()
        {
            if (m_HasDefaultTextColor || m_Text == null)
            {
                return;
            }

            m_DefaultTextColor = m_Text.color;
            m_HasDefaultTextColor = true;
        }

        // Update is called once per frame
        void Update()
        {
            UpdateOnce();
        }

        public void UpdateOnce()
        {
            if (m_State == State.Playing)
            {
                if (m_Trans == null)
                {
                    m_State = State.Cache;
                    //if (m_CanvasGroup != null)
                    //{
                    //    m_CanvasGroup.alpha = 0;
                    //}
                    if (this.m_Trans.gameObject.activeSelf)
                    {
                        this.m_Trans.gameObject.SetActive(false);
                    }
                    if (m_OnComplete != null)
                    {
                        m_OnComplete(this);
                        m_OnComplete = null;
                    }

                    return;
                }
                if (m_WorldCamera == null || m_UICamera == null)
                {
                    m_State = State.Cache;
                    m_Trans.localPosition = Vector3.up * 10000;
                    //if (m_CanvasGroup != null)
                    //{
                    //    m_CanvasGroup.alpha = 0;
                    //}
                    if(this.m_Trans.gameObject.activeSelf)
                    {
                        this.m_Trans.gameObject.SetActive(false);
                    }
                    if (m_OnComplete != null)
                    {
                        m_OnComplete(this);
                        m_OnComplete = null;
                    }

                    return;
                }
                float total_time = this.m_TweenTimeTotal <= 0 ? 1.0f : this.m_TweenTimeTotal;

                float useTime = Time.realtimeSinceStartup - this.m_StartTime;
                var worldPos = Vector3.Lerp(this.m_StartPosition, this.m_EndPosition, useTime / total_time);

                var screenPos = m_WorldCamera.WorldToScreenPoint(worldPos);
                var uguiPos = GameDll.Tool.ScreenPointToUGUI(this.m_Parent, screenPos, null);
                //转化为ugui坐标
                this.m_Trans.anchoredPosition = uguiPos;

                if (useTime >= total_time)
                {
                    if (m_OnComplete != null)
                    {
                        m_OnComplete(this);
                        m_OnComplete = null;
                    }
                    m_State = State.Cache;
                    this.m_Trans.localPosition = new Vector3(0, 1000000, 0);
                    this.m_Trans.gameObject.SetActive(false);
                    //if (m_CanvasGroup != null)
                    //{
                    //    m_CanvasGroup.alpha = 0;
                    //}
                }
                //float tscale = total_time / 5.0f;

                //float tscale_size = 1.2f;
                //if (useTime < tscale)
                //{
                //    this.m_Trans.localScale = Vector3.Slerp(Vector3.zero, tscale_size * Vector3.one * this.m_Size, useTime / tscale);
                //}
                //else
                //{
                //    var scale = Vector3.Slerp(tscale_size * Vector3.one * this.m_Size,
                //        Vector3.one * this.m_Size, useTime / total_time);
                //    this.m_Trans.localScale = scale;
                //}
            }
        }
    }

}
