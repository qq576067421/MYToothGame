using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnityUI
{
    [RequireComponent(typeof(Graphic))]
    public class UIDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// UI和指针的位置偏移量
        /// </summary>
        Vector2 offset;
        public RectTransform m_DragUI;

        public RectTransform m_ContainerUI;


        public System.Action<UIDraggable, float, float> OnDraggingCall;
        public System.Action<UIDraggable, float, float> OnBeginDragCall;
        public System.Action<UIDraggable> OnBeforeBeginDragCall;
        public System.Action<UIDraggable, float, float> OnEndDragCall;
        public System.Action<UIDraggable> OnDropCall;
        public System.Action<UIDraggable> OnEnterCall;
        public System.Action<UIDraggable> OnExitCall;


        public void OnDrop(PointerEventData eventData)
        {
            if (OnDropCall != null)
            {
                OnDropCall(this);
            }
        }
        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (OnBeforeBeginDragCall != null)
            {
                OnBeforeBeginDragCall(this);
            }
            Vector2 globalMousePos;
            m_DragUI.position = transform.position;
            //将屏幕坐标转换成世界坐标
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContainerUI, eventData.position, eventData.pressEventCamera, out globalMousePos))
            {
                //计算UI和指针之间的位置偏移量
                offset = m_DragUI.anchoredPosition - globalMousePos;
            }
            if (OnBeginDragCall != null)
            {
                OnBeginDragCall(this, eventData.position.x, eventData.position.y);
            }
        }
        public void OnBeginDrag(Vector2 position, Camera uiCamera)
        {
            if (OnBeforeBeginDragCall != null)
            {
                OnBeforeBeginDragCall(this);
            }
            Vector2 globalMousePos;
            m_DragUI.position = transform.position;
            //将屏幕坐标转换成世界坐标
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContainerUI, position, uiCamera, out globalMousePos))
            {
                //计算UI和指针之间的位置偏移量
                offset = m_DragUI.anchoredPosition - globalMousePos;
            }
            if (OnBeginDragCall != null)
            {
                OnBeginDragCall(this, position.x, position.y);
            }
        }
        /// <summary>
        /// 拖拽中
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            SetDraggedPosition(eventData);
            if (OnDraggingCall != null)
            {
                OnDraggingCall(this, eventData.position.x, eventData.position.y);
            }
        }
        public void OnDrag(Vector2 position, Camera uiCamera)
        {
            SetDraggedPosition(position, uiCamera);
            if (OnDraggingCall != null)
            {
                OnDraggingCall(this, position.x, position.y);
            }
        }
        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (OnEndDragCall != null)
            {
                OnEndDragCall(this, eventData.position.x, eventData.position.y);
            }
        }
        public void OnEndDrag(Vector2 position, Camera uiCamera)
        {
            if (OnEndDragCall != null)
            {
                OnEndDragCall(this, position.x, position.y);
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
                m_DragUI.anchoredPosition = offset + globalMousePos;
            }
        }
        public void SetDraggedPosition(Vector2 mousePosition, Camera uiCamera)
        {
            Vector2 globalMousePos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContainerUI, mousePosition, uiCamera, out globalMousePos))
            {
                m_DragUI.anchoredPosition = offset + globalMousePos;
            }
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (OnEnterCall != null)
            {
                OnEnterCall(this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (OnExitCall != null)
            {
                OnExitCall(this);
            }
        }
    }
}