using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnityUI
{
    [RequireComponent(typeof(Graphic))]
    public class UIDraggableLimit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>
        /// UI和指针的位置偏移量
        /// </summary>
        Vector2 offset;
        private RectTransform m_DragUI;

        public RectTransform m_ContainerUI;


        public System.Action<float, float> OnDraggingCall;
        public System.Action<float, float> OnBeginDragCall;
        public System.Action OnBeforeBeginDragCall;
        public System.Action<float, float> OnEndDragCall;

        void Update()
        {
            DragRangeLimit();
        }

        void Start()
        {
            if (m_ContainerUI == null)
            {
                Debug.LogError("必须要有容器m_ContainerUI");
                return;
            }
            m_DragUI = GetComponent<RectTransform>();
            if (m_DragUI != null)
            {
                if (m_DragUI.parent != m_ContainerUI.parent)
                {
                    Debug.LogError("拖动对象和容器对象的父节点必须相同,否则计算结果有可能不准确");
                }
            }
        }

        /// <summary>
        /// 拖拽范围限制
        /// </summary>
        void DragRangeLimit()
        {
            //限制水平/垂直拖拽范围在最小/最大值内
            Vector2 fix = new Vector2();
            fix = m_DragUI.anchoredPosition;
            float dx = m_DragUI.anchoredPosition.x;
            float dy = m_DragUI.anchoredPosition.y;
            float cx = m_ContainerUI.anchoredPosition.x;
            float cy = m_ContainerUI.anchoredPosition.y;
            float absx = Mathf.Abs(cx - dx);
            float absy = Mathf.Abs(cy - dy);
            float halfC = (float)m_ContainerUI.rect.width / 2.0f;
            float halfD = (float)m_DragUI.rect.width / 2.0f;
            if (absx >= halfC - halfD)
            {
                if (dx < cx)
                {
                    fix.x = cx - halfC + halfD;
                }
                else
                {
                    fix.x = cx + halfC - halfD;
                }

            }
            halfC = m_ContainerUI.rect.height / 2.0f;
            halfD = m_DragUI.rect.height / 2.0f;
            if (absy >= halfC - halfD)
            {
                if (dy < cy)
                {
                    fix.y = cy - halfC + halfD;
                }
                else
                {
                    fix.y = cy + halfC - halfD;
                }
            }
            m_DragUI.anchoredPosition = fix;
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            //在列表中的拖动元素有时间需要零时设定父对象为列表外面的UI，此时需要相应该事件来设置
            if (OnBeforeBeginDragCall != null)
            {
                OnBeforeBeginDragCall();
            }
            Vector2 globalMousePos;

            //将屏幕坐标转换成世界坐标
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContainerUI, eventData.position, eventData.pressEventCamera, out globalMousePos))
            {
                //计算UI和指针之间的位置偏移量
                offset = m_DragUI.anchoredPosition - globalMousePos;
            }
            if (OnBeginDragCall != null)
            {
                OnBeginDragCall(eventData.position.x, eventData.position.y);
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
                OnDraggingCall(eventData.position.x, eventData.position.y);
            }
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
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
                m_DragUI.anchoredPosition = offset + globalMousePos;
            }
        }

    }
}