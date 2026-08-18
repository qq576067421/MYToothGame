using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityUI
{
    public class LUIPressButton : LUIButton
    {

        // 长按
        public System.Action m_LongPressStartCall;
        public System.Action m_LongPressCall;
        public System.Action m_LongPressUpCall;

        // 双击
        public System.Action m_DoubleClickCall;


        // 长按需要的变量参数
        private bool m_IsStartPress = false;
        private float m_CurPointDownTime = 0f;
        private float m_LongPressTime = 0.6f;
        private bool m_LongPressTrigger = false;

        private bool m_IsCancelPress = false;

        void Update()
        {
            CheckIsLongPress();
            if (m_LastPointerTime > 0)
            {
                m_LastPointerTime -= Time.deltaTime;
                if (m_LastPointerTime <= 0)
                {
                    m_LastPointerTime = 0;
                    onClick.Invoke();
                }
            }
        }

        #region 长按

        /// <summary>
        /// 处理长按
        /// </summary>
        void CheckIsLongPress()
        {
            if (m_IsCancelPress)
            {
                return;
            }
            if (m_IsStartPress)
            {
                if (m_LongPressTrigger)
                {
                    if (m_LongPressCall != null)
                    {
                        m_LongPressCall();
                    }
                }
                else if (Time.time > m_CurPointDownTime + m_LongPressTime)
                {
                    m_LongPressTrigger = true;
                    if (m_LongPressStartCall != null)
                    {
                        m_LongPressStartCall();
                    }
                }

            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            m_CurPointDownTime = Time.time;
            m_IsStartPress = true;
            m_LongPressTrigger = false;
            m_IsCancelPress = false;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            if(m_LongPressTrigger)
            {
                if(m_LongPressUpCall != null)
                {
                    m_LongPressUpCall();
                }
            }

            m_IsStartPress = false;
            m_LongPressTrigger = false;


        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (m_LongPressTrigger)
            {
                if (m_LongPressUpCall != null)
                {
                    m_LongPressUpCall();
                }
            }
            m_IsStartPress = false;
            m_LongPressTrigger = false;

        }

        public void SetCancelPress(bool cancel)
        {
            m_IsCancelPress = cancel;
        }

        #endregion

        #region 双击（单击）

        public float m_DoubleClickTime = 0.2f;
        private float m_LastPointerTime = 0;
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (m_IsCancelPress)
            {
                m_IsCancelPress = false;
                return;
            }
            if (!m_LongPressTrigger)
            {
                if (m_DoubleClickCall != null)
                {
                    if (m_LastPointerTime <= 0)
                    {
                        m_LastPointerTime = m_DoubleClickTime;
                    }
                    else
                    {
                        m_LastPointerTime = 0;
                        if (m_DoubleClickCall != null)
                        {
                            m_DoubleClickCall();
                        }
                    }
                }
                else
                {
                    onClick.Invoke();
                }
            }
        }
        #endregion
    }
}