using LCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameDll
{

    public class UAnimatorComponent
    {
        public UResource m_Resource;
        public float m_AniSpeed = 0;
        public Animator m_Animator = null;
        public string m_AniStateName = "";
        public string m_CurAniStateName = "";
        public string m_AniEndStateName;
        public bool m_UseTrigger = false;
        public float m_FadeTime = 0.3f;

        public void LoadShowObjImp(GameObject gameObject)
        {

            if (m_Animator == null)
            {
                m_Animator = gameObject.GetComponent<Animator>();
            }
            

        }

        //某个动画最长的播放时间
        private float m_AniMaxTime = 0;
        public void SetAnimationMaxTime(float time)
        {
            m_AniMaxTime = time;
        }

        public void SetAnimationSpeed(float speed)
        {
            //重置
            m_AniMaxTime = 0;


            m_AniSpeed = speed;
            if (m_Resource.IsObjectLoaded())
            {
                SetAnimationSpeedImp();
            }
            else
            {
                m_Resource.AddLoadedCall(SetAnimationSpeedImp);
            }
        }
        private void SetAnimationSpeedImp()
        {
            if (m_Animator != null)
            {
                m_Animator.speed = m_AniSpeed;
            }

        }
        private void PlayAnimationImp()
        {
            if (m_Animator != null)
            {
                //m_Animation. = m_bLoop ? WrapMode.Loop : WrapMode.Default;

                if (m_UseTrigger)
                {
                    m_Animator.SetTrigger(m_AniStateName);
                }
                else
                {
                    var state = m_Animator.GetCurrentAnimatorStateInfo(0);
                    //if (!state.IsName(m_AniStateName))
                    {
                        if (m_AniMaxTime > 0)
                        {
                            m_Animator.speed = state.length / m_AniMaxTime;
                            m_AniMaxTime = 0;
                        }
                        m_Animator.CrossFadeInFixedTime(m_AniStateName, m_FadeTime);
                    }
                }

                //UDebug.Log("play ani:" + m_AniName);
            }
        }
        public void PlayAnimation(string ani, string endAni = null, float time = 1.0f, bool useTrigger = false)
        {
            if (string.IsNullOrEmpty(ani))
            {
                return;
            }
            m_AniStateName = ani;
            m_AniEndStateName = endAni;
            m_UseTrigger = useTrigger;
            m_FadeTime = time;
            if (m_Resource.IsObjectLoaded())
            {
                PlayAnimationImp();
            }
            else
            {
                m_Resource.AddLoadedCall(PlayAnimationImp);
            }

        }

        public void ReplayCurrentAnimation(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            if (m_Resource.IsObjectLoaded())
            {
                ReplayCurrentAnimationImp(normalizedTime);
            }
            else
            {
                m_Resource.AddLoadedCall(() => ReplayCurrentAnimationImp(normalizedTime));
            }
        }

        private void ReplayCurrentAnimationImp(float normalizedTime)
        {
            if (m_Animator == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(m_AniStateName) && !m_UseTrigger)
            {
                m_Animator.Play(m_AniStateName, 0, normalizedTime);
                m_CurAniStateName = m_AniStateName;
            }
            else
            {
                var state = m_Animator.GetCurrentAnimatorStateInfo(0);
                m_Animator.Play(state.fullPathHash, 0, normalizedTime);
            }

            m_Animator.Update(0f);
            m_Animator.speed = m_AniSpeed;
        }
    }
}
