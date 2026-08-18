using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 按钮事件大小缩放
/// </summary>
namespace UnityUI
{
    public class UIButtonScale : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerClickHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Animator m_Animator;
        public void OnPointerEnter(PointerEventData eventData)
        {
        }
        public void OnPointerExit(PointerEventData eventData)
        {
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if(m_Animator == null)
            {
                return;
            }
            m_Animator.Play("lpress");
            m_IsCachePointerUp = false;
        }

        private bool m_IsCachePointerUp = false;
        public void OnPointerUp(PointerEventData eventData)
        {
            if (m_Animator == null)
            {
                return;
            }
            var info = m_Animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName("lpressloop"))
            {
                //缓存下松开操作
                m_IsCachePointerUp = true;
            }
            else
            {
                m_IsCachePointerUp = false;
                m_Animator.Play("lrelease");
            }
        }

        void Update()
        {
            if (m_Animator == null)
            {
                return;
            }
            if (m_IsCachePointerUp)
            {
                m_IsCachePointerUp = false;
                m_Animator.Play("lrelease");
            }
        }


        public void OnPointerClick(PointerEventData eventData)
        {

        }
    }
}