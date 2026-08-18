using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Animations;
namespace UnityUI
{
    public class UIWindowAnimation : MonoBehaviour
    {
        [SerializeField]
        private Animator m_Animator;
        [SerializeField]
        private List<CanvasGroup> m_CanvasGroups = new List<CanvasGroup>();
        [SerializeField]
        private float m_OpenMaxTime = 3.0f;
        [SerializeField]
        private float m_CloseMaxTime = 3.0f;

        private float m_AnimationStartTime = 0;
        private enum PlayState
        {
            None,
            Openning,
            Openned,
            Closing,
            Closed
        }
        private PlayState m_State = PlayState.None;

        private void Start()
        {
            if(m_CanvasGroups.Count == 0)
            {
                var groups = this.GetComponentsInChildren<CanvasGroup>();
                m_CanvasGroups.AddRange(groups);
            }
        }

        private System.Action m_OnFinishOpenFunc;
        public void PlayOpen(System.Action OnFinishFunc)
        {
            if (m_State == PlayState.Openned ||
                m_State == PlayState.Openning)
            {
                return;
            }
            if (m_OnFinishCloseFunc != null)
            {
                OnClosed();
            }
            m_AnimationStartTime = Time.realtimeSinceStartup;
            m_State = PlayState.Openning;
            m_OnFinishOpenFunc = OnFinishFunc;
            OnPlayAnimation("open");
        }
        private System.Action m_OnFinishCloseFunc;
        public void PlayClose(System.Action OnFinishFunc)
        {
            if(m_State == PlayState.Closed || 
               m_State == PlayState.Closing)
            {
                return;
            }
            if(m_OnFinishOpenFunc != null)
            {
                OnOpened();
            }
            m_AnimationStartTime = Time.realtimeSinceStartup;
            m_State = PlayState.Closing;
            m_OnFinishCloseFunc = OnFinishFunc;
            OnPlayAnimation("close");
        }
        public void JumpClose()
        {
            m_OnFinishCloseFunc = null;
            m_State = PlayState.Closed;
        }
        private void OnPlayAnimation(string animation)
        {
            if(m_Animator == null)
            {
                if(m_OnFinishOpenFunc != null)
                {
                    OnOpened();
                }
                if(m_OnFinishCloseFunc != null)
                {
                    OnClosed();
                }
                return;
            }
            m_Animator.enabled = true;
            SetEnableCanvasGroup(true);
            m_Animator.Play(animation, 0, 0);
            m_Animator.Update(0);
        }
        private void Update()
        {
            if(m_State != PlayState.Openning  && m_State != PlayState.Closing)
            {
                return;
            }
            float time = Time.realtimeSinceStartup;
            float max_time = m_State == PlayState.Openning ? m_OpenMaxTime : m_CloseMaxTime;
            if(time - m_AnimationStartTime >= max_time)
            {
                OnFinishAnimation();
            }
            else
            {
                var info = m_Animator.GetCurrentAnimatorStateInfo(0);
                if(info.normalizedTime >= 1.0f)
                {
                    OnFinishAnimation();
                }
            }
        }
        public void SetEnableCanvasGroup(bool enable)
        {
            if(m_CanvasGroups == null)
            {
                return;
            }
            foreach (var group in m_CanvasGroups)
            {
                group.enabled = enable;
            }
        }
        public void OnNoAnimation()
        {
            SetEnableCanvasGroup(false);
        }
        private void OnOpened()
        {
            SetEnableCanvasGroup(false);
            if(m_OnFinishOpenFunc != null)
            {
                m_OnFinishOpenFunc();
                m_OnFinishOpenFunc = null;
            }
            m_State = PlayState.Openned;
        }
        private void OnClosed()
        {
            if(m_OnFinishCloseFunc != null)
            {
                m_OnFinishCloseFunc();
                m_OnFinishCloseFunc = null;
            }
            m_State = PlayState.Closed;
        }
        private void OnFinishAnimation()
        {
            if(m_State == PlayState.Openning)
            {
                OnOpened();
            }
            if(m_State == PlayState.Closing)
            {
                OnClosed();
            }
            //if(m_Animator != null)
            //{
            //    m_Animator.enabled = false;
            //}
        }
    }
}