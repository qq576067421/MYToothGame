using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUI
{
    //该组件需要用到ScrollRect 并且需要包含ScrollBar 水平挥动条或者垂直滑动条，如果不想看到滑动条可以把滑动条大小改为0
    public class UIScrollPageTween : MonoBehaviour
    {
        public ScrollRect m_Rect;
        public int m_PageCount;
        public int m_Index = -1;
        private float m_TargetValue;
        public float m_MoveSpeed = 1.0f;
        private bool m_NeedMove = false;
        public float m_SmoothTime = 0.2F;
        private System.Action<int> m_OnStartCall;
        private System.Action<int> m_OnFinishCall;
        public void SetCallback(System.Action<int> OnStartCall, System.Action<int> OnFinishCall)
        {
            m_OnStartCall = OnStartCall;
            m_OnFinishCall = OnFinishCall;
        }
        public void MoveTo(int index)
        {
            var per = 1.0f / m_PageCount;
            m_TargetValue = per * index;
            if (m_Index != index)
            {
                m_Index = index;
                m_NeedMove = true;
                if (m_OnStartCall != null)
                {
                    m_OnStartCall(m_Index);
                }

            }

        }
        void Start()
        {

        }
        void Update()
        {
            if (m_NeedMove)
            {
                if (Mathf.Abs(m_Rect.horizontalScrollbar.value - m_TargetValue) < 0.01f)
                {
                    m_Rect.horizontalScrollbar.value = m_TargetValue;
                    m_NeedMove = false;
                    if (m_OnFinishCall != null)
                    {
                        m_OnFinishCall(m_Index);
                    }
                    return;
                }
                m_Rect.horizontalScrollbar.value = Mathf.SmoothDamp(m_Rect.horizontalScrollbar.value, m_TargetValue, ref m_MoveSpeed, m_SmoothTime);
            }
        }
    }
}