using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUI
{
    public class UILotteryAnimation : MonoBehaviour
    {
        public class LotterySlot
        {
            public int m_Slot;
            public GameObject m_HightlightEffect;
        }

        [SerializeField] 
        AnimationCurve m_SpeedCurve; // 控制速度变化的曲线
        [SerializeField] 
        int m_TargetIndex = 3;      // 最终停留位置
        [SerializeField] 
        float m_TotalDuration = 5f; // 总动画时长
        [Tooltip("抽奖出结果后停留时间，用于展示结果，防止下一步太快")]
        [SerializeField]
        float m_FinishKeepTime = 1.0f;
        [SerializeField] 
        int m_MinCycles = 3;        // 最小循环次数
        [SerializeField]
        int m_HightlightAudio = 3;
        [SerializeField]
        int m_TargetAudio = 3;
        [SerializeField]
        float m_TargetAudioDelayTime = 0.1f;

        [SerializeField] 
        List<LotterySlot> m_Slots;    // 格子集合

        private Coroutine m_AnimationCoroutine;

        private Action<int> m_OnFinishAction;
        public void SetLotterSlots(List<LotterySlot> slots)
        {
            m_Slots = slots;
        }
        public void SetFinishAction(Action<int> action)
        {
            m_OnFinishAction = action;
        }
        public void SetTargetIndex(int target)
        {
            m_TargetIndex = target;
        }
        // 外部调用接口
        public void StartAnimation()
        {
            if (m_AnimationCoroutine != null)
            {
                StopCoroutine(m_AnimationCoroutine);
            }
            m_AnimationCoroutine = StartCoroutine(PlayLotteryAnimation());
        }

        private IEnumerator PlayLotteryAnimation()
        {
            // 参数校验
            if (m_Slots.Count == 0 || m_TargetIndex >= m_Slots.Count)
            {
                Debug.LogError("Invalid slots configuration");
                yield break;
            }

            float timer = 0f;
            int currentIndex = 0;
            int cycleCount = 0;

            // 第一阶段：加速
            while (timer < m_TotalDuration || cycleCount < m_MinCycles)
            {
                float normalizedTime = Mathf.Clamp01(timer / m_TotalDuration);
                float interval = m_SpeedCurve.Evaluate(normalizedTime) * 0.1f;

                UpdateHighlight(currentIndex);
                yield return new WaitForSeconds(interval);

                currentIndex = (currentIndex + 1) % m_Slots.Count;
                if (currentIndex == 0) cycleCount++;
                timer += interval;
            }

            // 第二阶段：精准定位
            while (currentIndex != m_TargetIndex)
            {
                UpdateHighlight(currentIndex);
                yield return new WaitForSeconds(0.1f);
                currentIndex = (currentIndex + 1) % m_Slots.Count;
            }

            FinalizeAnimation(m_TargetIndex);
        }

        private void UpdateHighlight(int index)
        {
            // 关闭所有高亮
            foreach (var slot in m_Slots)
            {
                if (slot.m_HightlightEffect != null && slot.m_HightlightEffect.activeSelf)
                {
                    slot.m_HightlightEffect.SetActive(false);
                }
            }

            // 开启当前高亮
            if (index < m_Slots.Count && m_Slots[index].m_HightlightEffect != null)
            {
                m_Slots[index].m_HightlightEffect.SetActive(true);

                GameDll.AudioManager.GetInstance().Play2D(m_HightlightAudio);
            }
        }

        //不想等动画了，立马结束
        public void QuickFinishAnimation()
        {
            if (m_AnimationCoroutine != null)
            {
                StopCoroutine(m_AnimationCoroutine);
            }
            FinalizeAnimation(m_TargetIndex);
        }
        private void FinalizeAnimation(int target)
        {
            UpdateHighlight(target);

            Debug.Log($"最终停留位置：{target}");

            StartCoroutine(DelayFinish(target));
        }

        private IEnumerator DelayFinish(int target)
        {
            yield return new WaitForSeconds(m_TargetAudioDelayTime);
            GameDll.AudioManager.GetInstance().Play2D(m_HightlightAudio);
            yield return new WaitForSeconds(m_FinishKeepTime);
            // 触发后续事件（如奖励发放）
            if(m_OnFinishAction != null)
            {
                m_OnFinishAction(target);
            }
        }
    }
}