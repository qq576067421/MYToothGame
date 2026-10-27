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
        private readonly List<Animator> m_Animators = new List<Animator>();
        public string m_AniStateName = "";
        public string m_CurAniStateName = "";
        public string m_AniEndStateName;
        public bool m_UseTrigger = false;
        public float m_FadeTime = 0.3f;

        public void LoadShowObjImp(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            // 角色身上可能挂了多个 Animator（根节点 + 武器等子节点），全部收集起来一起驱动。
            m_Animators.Clear();
            var animators = gameObject.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null)
                {
                    m_Animators.Add(animators[i]);
                }
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
            int count = m_Animators.Count;
            for (int i = 0; i < count; i++)
            {
                var animator = m_Animators[i];
                if (animator != null)
                {
                    animator.speed = m_AniSpeed;
                }
            }
        }
        private void PlayAnimationImp()
        {
            int count = m_Animators.Count;
            for (int i = 0; i < count; i++)
            {
                var animator = m_Animators[i];
                if (animator == null)
                {
                    continue;
                }

                if (m_UseTrigger)
                {
                    if (HasTriggerParameter(animator, m_AniStateName))
                    {
                        animator.SetTrigger(m_AniStateName);
                    }
                    continue;
                }

                if (m_AniMaxTime > 0)
                {
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    animator.speed = state.length / m_AniMaxTime;
                }
                animator.CrossFadeInFixedTime(m_AniStateName, m_FadeTime);
            }

            if (!m_UseTrigger)
            {
                m_AniMaxTime = 0;
            }
        }

        // 子节点 Animator 的状态机未必有同名 Trigger，参数不存在时 SetTrigger 会打警告，先确认再触发。
        private static bool HasTriggerParameter(Animator animator, string paramName)
        {
            int count = animator.parameterCount;
            for (int i = 0; i < count; i++)
            {
                var parameter = animator.GetParameter(i);
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == paramName)
                {
                    return true;
                }
            }

            return false;
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
            int count = m_Animators.Count;
            for (int i = 0; i < count; i++)
            {
                var animator = m_Animators[i];
                if (animator == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(m_AniStateName) && !m_UseTrigger)
                {
                    animator.Play(m_AniStateName, 0, normalizedTime);
                    m_CurAniStateName = m_AniStateName;
                }
                else
                {
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    animator.Play(state.fullPathHash, 0, normalizedTime);
                }

                animator.Update(0f);
                animator.speed = m_AniSpeed;
            }
        }
    }
}
