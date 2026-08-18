using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Input = InputSystemCompat;

namespace UnityUI
{
    public class UIClickArea : MonoBehaviour
    {
        //该区域可以穿透点击(要区分拖动)并且相应关闭事件
        public RectTransform m_AreaCloseAndThrough;
        //该区域阻挡点击
        public RectTransform m_AreaBlock;
        public Action m_OnClickCloseArea;

        private Vector2 m_PointDownPosition;
        private bool m_IsMouseDown = false;
        public float m_DragLimit = 5f;
        //检测拖动，如果不检测，则和拖动没有关系
        public bool m_CheckIsDrag = false;

        private void Update()
        {
            // 鼠标左键按下
            if(m_CheckIsDrag)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    m_PointDownPosition = Input.mousePosition;
                    m_IsMouseDown = true;
                }

                // 鼠标左键抬起
                if (Input.GetMouseButtonUp(0) && m_IsMouseDown)
                {
                    m_IsMouseDown = false;
                    Vector2 currentPos = Input.mousePosition;

                    // 判断是否为点击（移动距离小于阈值）
                    float distance = Vector2.Distance(currentPos, m_PointDownPosition);
                    if (distance <= m_DragLimit)
                    {
                        HandleClick(currentPos);
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 currentPos = Input.mousePosition;
                    HandleClick(currentPos);
                }
            }


        }

        private void HandleClick(Vector2 position)
        {
            // 检查是否点击了阻挡区域
            if (m_AreaBlock != null && IsPointInRect(position, m_AreaBlock))
            {
                return;
            }

            // 检查是否点击了穿透关闭区域
            if (m_AreaCloseAndThrough != null && IsPointInRect(position, m_AreaCloseAndThrough))
            {
                m_OnClickCloseArea?.Invoke();
            }
        }

        private bool IsPointInRect(Vector2 screenPosition, RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return false;
            }
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition);
        }
    }
}
