using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Input = InputSystemCompat;

namespace UnityUI
{
    public class LUIPressTriggerButton : LUIButton
    {
        public Animator m_Animator;
        // 长按
        public System.Action m_LongPressStartCall;
        public System.Action m_LongPressCall;
        public System.Action m_LongPressUpCall;


        // 长按需要的变量参数
        private bool m_IsStartPress = false;
        private float m_CurPointDownTime = 0f;
        private float m_LongPressTime = 0.4f; // 0.4秒区分点击和长按
        private bool m_LongPressTrigger = false;
        private float m_LastLongPressCallTime = 0f; // 上次调用长按事件的时间
        private float m_LongPressFirstInterval = 0.2f; //  确认长按后首次触发间隔0.2秒
        private float m_LongPressInterval = 0.1f; // 长按后续触发间隔0.1秒
        private bool m_IsLongPressStartCallInvoked = false; // 是否已经调用过长按开始回调

        private bool m_IsCancelPress = false;
        private bool m_IsPointerOver = false; // 指针是否在按钮上

        private bool m_IsAnimating = false; // 是否正在播放点击动画
        private PointerEventData m_CurrentEventData; // 保存当前的 PointerEventData，用于实时检测
        private int m_PointerId = -1; // 记录按下时的 pointerId，用于追踪全局抬起事件
        private bool m_WaitingForRelease = false; // 是否等待松开

        protected override void Start()
        {
            if(m_Animator == null)
            {
                m_Animator = GetComponent<Animator>();
            }
        }

        void Update()
        {
            if (!interactable)
            {
                if (m_IsAnimating && m_Animator != null)
                {
                    m_IsAnimating = false;
                    m_Animator.Play("lrelease");
                }
                return;
            }

            // 按下期间实时检测手指是否在按钮内
            if (m_IsStartPress && m_CurrentEventData != null)
            {
                RectTransform rectTransform = transform as RectTransform;
                bool isInside = RectTransformUtility.RectangleContainsScreenPoint(
                    rectTransform,
                    m_CurrentEventData.position,
                    m_CurrentEventData.pressEventCamera);

                // 如果有其他 UI 元素挡在前面（比如一个 Image），则认为被遮挡，应中断长按
                bool isBlockedByOtherUI = false;
                if (isInside && EventSystem.current != null)
                {
                    var ped = new PointerEventData(EventSystem.current)
                    {
                        position = m_CurrentEventData.position
                    };
                    var results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(ped, results);
                    if (results != null && results.Count > 0)
                    {
                        var topGo = results[0].gameObject;
                        if (topGo != null && topGo != gameObject && !topGo.transform.IsChildOf(transform))
                        {
                            isBlockedByOtherUI = true;
                        }
                    }
                }

                m_IsPointerOver = isInside && !isBlockedByOtherUI;

                if (isBlockedByOtherUI)
                {
                    // 发现被其他 UI 遮挡，打断当前长按
                    m_IsCancelPress = true;
                    HandlePointerRelease();
                    return;
                }
            }
            // 检查松开状态
            CheckPointerRelease();

            CheckIsLongPress();

            if (m_Animator == null)
            {
                return;
            }
            if (m_IsCachePointerUp)
            {
                m_IsCachePointerUp = false;
                m_Animator.Play("lrelease");
                m_IsAnimating = true;
            }
        }
        protected override void OnDisable()
        {
            m_IsCancelPress = true;
            HandlePointerRelease();
        }
        /// <summary>
        /// 检查指针是否松开
        /// </summary>
        void CheckPointerRelease()
        {
            if (!m_IsStartPress)
            {
                return;
            }

            bool isReleased = false;
            Vector2 pointerPosition = Vector2.zero;

            // 检查鼠标左键
            if (m_PointerId == -1)
            {
                isReleased = Input.GetMouseButtonUp(0);
                pointerPosition = Input.mousePosition;
            }
            // 检查触摸
            else
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.fingerId == m_PointerId)
                    {
                        isReleased = (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled);
                        pointerPosition = touch.position;
                        break;
                    }
                }
            }

            // 如果松开了
            if (isReleased)
            {
                // 检查是否在按钮外
                RectTransform rectTransform = transform as RectTransform;
                bool isOutside = !RectTransformUtility.RectangleContainsScreenPoint(
                    rectTransform,
                    pointerPosition,
                    m_CurrentEventData?.pressEventCamera);

                if (isOutside)
                {
                    // 在按钮外松开，触发长按结束
                    HandlePointerRelease();
                }
                else if (m_WaitingForRelease)
                {
                    // 在按钮内松开，也触发长按结束
                    HandlePointerRelease();
                }
                else
                {
                    // 在按钮内拖动松开，等待 OnPointerUp 处理
                    m_WaitingForRelease = true;
                }
            }
        }

        /// <summary>
        /// 处理指针松开
        /// </summary>
        void HandlePointerRelease()
        {
            if (!m_IsStartPress)
            {
                return;
            }

            // 触发长按结束
            if (m_LongPressTrigger)
            {
                m_LongPressUpCall?.Invoke();
            }

            // 重置状态
            m_IsStartPress = false;
            m_LongPressTrigger = false;
            m_IsPointerOver = false;
            m_CurrentEventData = null;
            m_PointerId = -1;
            m_WaitingForRelease = false;

            // 播放松开动画
            if (m_Animator != null)
            {
                var info = m_Animator.GetCurrentAnimatorStateInfo(0);
                if (!info.IsName("lpressloop"))
                {
                    // 如果还在按下动画中，缓存松开操作等动画播放完
                    m_IsCachePointerUp = true;
                }
                else
                {
                    m_IsCachePointerUp = false;
                    m_Animator.Play("lrelease");
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
            // 移出按钮区域时不响应事件
            if (!m_IsPointerOver)
            {
                return;
            }
            if (m_IsStartPress)
            {
                if (m_LongPressTrigger)
                {
                    // 确认长按后首次间隔0.2秒，之后每0.1秒触发一次
                    float currentInterval = (Time.realtimeSinceStartup - m_CurPointDownTime - m_LongPressTime < m_LongPressFirstInterval)
                        ? m_LongPressFirstInterval
                        : m_LongPressInterval;

                    if (Time.realtimeSinceStartup >= m_LastLongPressCallTime + currentInterval)
                    {
                        m_LastLongPressCallTime = Time.realtimeSinceStartup;
                        m_LongPressCall?.Invoke();
                    }
                }
                else if (Time.realtimeSinceStartup > m_CurPointDownTime + m_LongPressTime)
                {
                    // 0.4秒后触发长按开始（与单击区分）
                    m_LongPressTrigger = true;
                    m_LastLongPressCallTime = Time.realtimeSinceStartup; // 记录首次触发时间
                    m_IsLongPressStartCallInvoked = true;
                    m_LongPressStartCall?.Invoke();
                    // 首次触发时也调用一次长按事件
                    m_LongPressCall?.Invoke();
                }

            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (m_IsAnimating && m_Animator != null)
            {
                m_Animator.Play("lpress", 0, 0f); // 从头播放
            }
            else if (m_Animator != null)
            {
                m_Animator.Play("lpress");
            }
            m_IsAnimating = true;
            m_IsCachePointerUp = false;

            m_CurPointDownTime = Time.realtimeSinceStartup;
            m_IsStartPress = true;
            m_LongPressTrigger = false;
            m_IsCancelPress = false;
            m_IsLongPressStartCallInvoked = false;
            m_LastLongPressCallTime = 0f;
            m_IsPointerOver = true;
            m_CurrentEventData = eventData; // 保存 eventData，用于 Update 中实时检测
            m_PointerId = eventData.pointerId; // 记录 pointerId
            m_WaitingForRelease = false; // 重置等待状态
        }
        private bool m_IsCachePointerUp = false;

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            m_IsPointerOver = true;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            // 不在这里设置 m_IsPointerOver，由 Update 实时检测
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            // 如果正在拖动，不是真正的松开，忽略
            if (eventData.dragging)
            {
                return;
            }

            // 在按钮上松开，处理松开逻辑
            HandlePointerRelease();
        }

        public void SetCancelPress(bool cancel)
        {
            m_IsCancelPress = cancel;
        }

        #endregion

        #region 单击

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (m_IsCancelPress)
            {
                m_IsCancelPress = false;
                return;
            }

            //如果时长小于0.4秒（未触发长按），响应点击事件
            if (!m_LongPressTrigger && !m_IsLongPressStartCallInvoked)
            {
                onClick?.Invoke();
            }
        }
        #endregion
    }
}
