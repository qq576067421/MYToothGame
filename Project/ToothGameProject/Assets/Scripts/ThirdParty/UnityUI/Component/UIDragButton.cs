using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnityUI
{
    [RequireComponent(typeof(Graphic))]
    public class UIDragButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        /// <summary>
        /// UI和指针的位置偏移量
        /// </summary>
        Vector2 offset;
        public RectTransform m_DragUI;

        public RectTransform m_ContainerUI;
        public float m_DoubleClickTime = 0.2f;

        public System.Action<float, float> OnDraggingCall;
        public System.Action<float, float> OnBeginDragCall;
        public System.Action<float, float> OnEndDragCall;
        //各种异常终止
        public System.Action OnCancelDragCall;

        public System.Action OnClickCall;
        public System.Action OnDoubleClickCall;
        private bool m_IsDragged = false;
        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 globalMousePos;

            if (OnBeginDragCall != null)
            {
                OnBeginDragCall(eventData.position.x, eventData.position.y);
            }
            if (m_DragUI != null)
            {
                m_DragUI.position = transform.position;
            }
            //将屏幕坐标转换成世界坐标
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContainerUI, eventData.position, eventData.pressEventCamera, out globalMousePos))
            {
                //计算UI和指针之间的位置偏移量
                if(m_DragUI != null)
                {
                    offset = m_DragUI.anchoredPosition - globalMousePos;
                }
            }

        }

        /// <summary>
        /// 拖拽中
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            m_IsDragged = true;
            SetDraggedPosition(eventData);
            if (OnDraggingCall != null)
            {
                OnDraggingCall(eventData.position.x, eventData.position.y);
            }
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            m_IsDragged = false;
            if (OnEndDragCall != null)
            {
                OnEndDragCall(eventData.position.x, eventData.position.y);
            }
        }

        /// <summary>
        /// 更新UI的位置
        /// </summary>
        private void SetDraggedPosition(PointerEventData eventData)
        {
            Vector2 globalMousePos;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContainerUI, eventData.position, eventData.pressEventCamera, out globalMousePos))
            {
                if (m_DragUI != null)
                {
                    m_DragUI.anchoredPosition = offset + globalMousePos;
                }
            }
        }
        public void OnHierarchyActive(bool isActive)
        {
            if(isActive == false && m_IsDragged)
            {
                m_IsDragged = false;
                if (OnCancelDragCall != null)
                {
                    OnCancelDragCall();
                }
            }
        }
        void Update()
        {
            if(m_LastPointerTime > 0)
            {
                m_LastPointerTime -= Time.deltaTime;
                if(m_LastPointerTime <= 0)
                {
                    m_LastPointerTime = 0;
                    if (OnClickCall != null)
                    {
                        OnClickCall();
                    }
                }
            }
            //这里我看过Cancel事件，貌似它是指按下Esc按钮？？？
            //如果已经隐藏，压根这段代码都不会执行
            //if(m_IsDragged && this.gameObject.activeInHierarchy == false)
            //{
            //    if(OnCancelDragCall != null)
            //    {
            //        OnCancelDragCall();
            //    }
            //}
        }

        private float m_LastPointerTime = 0;
        public void OnPointerClick(PointerEventData eventData)
        {
            if(m_IsDragged)
            {
                m_LastPointerTime = 0;
                return;
            }
            if(OnDoubleClickCall != null)
            {
                if(m_LastPointerTime <= 0)
                {
                    m_LastPointerTime = m_DoubleClickTime;
                }
                else
                {
                    m_LastPointerTime = 0;
                    if (OnDoubleClickCall != null)
                    {
                        OnDoubleClickCall();
                    }
                }
            }
            else
            {
                if (OnClickCall != null)
                {
                    OnClickCall();
                }
            }
        }
    }
}