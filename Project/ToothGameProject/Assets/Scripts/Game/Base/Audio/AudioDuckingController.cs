using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    /// <summary>
    /// 管理由播放请求触发的声音总线压低状态，并把每条总线的最终压低系数交给 <see cref="AudioBusController"/>。
    /// </summary>
    /// <remarks>
    /// <para>每个播放请求最多对应一份压低状态。声音开始播放时激活，暂停、停止、完成或释放时进入恢复阶段。</para>
    /// <para>多个请求同时影响同一总线时采用最低的当前音量，而不是相乘，从而避免重复压低导致意外静音。</para>
    /// <para>所有时间都使用不受 <see cref="Time.timeScale"/> 影响的帧间隔推进。</para>
    /// </remarks>
    internal sealed class AudioDuckingController
    {
        /// <summary>保存单个播放请求的压低目标和当前过渡进度。</summary>
        private sealed class AudioDuckState
        {
            public long m_RequestId;
            public AudioBusMask m_TargetBuses;
            public float m_TargetVolume;
            public float m_AttackSeconds;
            public float m_ReleaseSeconds;
            public float m_CurrentVolume = 1f;
            public bool m_IsActive;
        }

        private const float m_DefaultAttackSeconds = 0.05f;
        private const float m_DefaultReleaseSeconds = 0.4f;

        private readonly List<AudioDuckState> m_States = new List<AudioDuckState>();
        private readonly float[] m_TargetBusVolumes = { 1f, 1f, 1f, 1f };
        private readonly AudioBusController m_BusController;

        private float m_DefaultAttack = m_DefaultAttackSeconds;
        private float m_DefaultRelease = m_DefaultReleaseSeconds;

        /// <summary>创建声音压低控制器。</summary>
        /// <param name="busController">接收每条总线最终压低系数的总线控制器。</param>
        public AudioDuckingController(AudioBusController busController)
        {
            m_BusController = busController;
        }

        /// <summary>
        /// 激活或更新指定播放请求的声音压低状态。
        /// </summary>
        /// <param name="requestId">播放请求的唯一编号。</param>
        /// <param name="options">需要应用的压低目标和过渡时间。</param>
        /// <remarks>参数不产生实际压低效果时，已有状态会进入恢复阶段，新状态不会创建。</remarks>
        public void Start(long requestId, AudioDuckOptions options)
        {
            var state = FindState(requestId);
            if (!options.IsEnabled())
            {
                if (state != null)
                {
                    state.m_IsActive = false;
                }
                return;
            }
            if (state == null)
            {
                state = new AudioDuckState { m_RequestId = requestId };
                m_States.Add(state);
            }
            state.m_TargetBuses = options.m_TargetBuses;
            state.m_TargetVolume = Mathf.Clamp(options.m_TargetVolume, AudioDuckOptions.m_MinimumVolume, 1f);
            state.m_AttackSeconds = options.m_AttackSeconds < 0f ? m_DefaultAttack : Mathf.Max(0f, options.m_AttackSeconds);
            state.m_ReleaseSeconds = options.m_ReleaseSeconds < 0f ? m_DefaultRelease : Mathf.Max(0f, options.m_ReleaseSeconds);
            state.m_IsActive = true;
        }

        /// <summary>
        /// 停止指定播放请求继续压低声音，并按其恢复时间回到原音量。
        /// </summary>
        /// <param name="requestId">播放请求的唯一编号。</param>
        public void Stop(long requestId)
        {
            var state = FindState(requestId);
            if (state != null)
            {
                state.m_IsActive = false;
            }
        }

        /// <summary>
        /// 立即移除全部压低状态，并将所有声音总线的压低系数恢复为 1。
        /// </summary>
        public void Clear()
        {
            m_States.Clear();
            for (int i = 0; i < m_TargetBusVolumes.Length; i++)
            {
                m_TargetBusVolumes[i] = 1f;
                m_BusController.SetDuckVolume((AudioBus)i, 1f);
            }
        }

        /// <summary>
        /// 推进全部压低和恢复过渡，并重新计算每条总线的最终压低系数。
        /// </summary>
        /// <param name="unscaledDeltaTime">不受 <see cref="Time.timeScale"/> 影响的帧间隔秒数。</param>
        public void Update(float unscaledDeltaTime)
        {
            if (m_States.Count == 0)
            {
                return;
            }
            for (int i = 0; i < m_TargetBusVolumes.Length; i++)
            {
                m_TargetBusVolumes[i] = 1f;
            }

            float deltaTime = Mathf.Max(0f, unscaledDeltaTime);
            for (int i = m_States.Count - 1; i >= 0; i--)
            {
                var state = m_States[i];
                float targetVolume = state.m_IsActive ? state.m_TargetVolume : 1f;
                float duration = state.m_IsActive ? state.m_AttackSeconds : state.m_ReleaseSeconds;
                float fullRange = 1f - state.m_TargetVolume;
                state.m_CurrentVolume = duration <= 0f ? targetVolume : Mathf.MoveTowards(state.m_CurrentVolume, targetVolume, fullRange * deltaTime / duration);
                if (!state.m_IsActive && Mathf.Approximately(state.m_CurrentVolume, 1f))
                {
                    m_States.RemoveAt(i);
                    continue;
                }

                for (int busIndex = 0; busIndex < m_TargetBusVolumes.Length; busIndex++)
                {
                    var busMask = (AudioBusMask)(1 << busIndex);
                    if ((state.m_TargetBuses & busMask) != 0)
                    {
                        // 多个播放请求同时压低同一分组时采用最低音量，避免连续相乘导致意外静音。
                        m_TargetBusVolumes[busIndex] = Mathf.Min(m_TargetBusVolumes[busIndex], state.m_CurrentVolume);
                    }
                }
            }

            for (int i = 0; i < m_TargetBusVolumes.Length; i++)
            {
                m_BusController.SetDuckVolume((AudioBus)i, m_TargetBusVolumes[i]);
            }
        }

        /// <summary>
        /// 设置压低参数未显式指定时间时使用的默认进入和恢复时间。
        /// </summary>
        /// <param name="attackSeconds">进入压低状态所用秒数；负数会按 0 处理。</param>
        /// <param name="releaseSeconds">恢复原音量所用秒数；负数会按 0 处理。</param>
        public void SetDefaultTransitionSeconds(float attackSeconds, float releaseSeconds)
        {
            m_DefaultAttack = Mathf.Max(0f, attackSeconds);
            m_DefaultRelease = Mathf.Max(0f, releaseSeconds);
        }

        /// <summary>读取默认的进入压低状态时间。</summary>
        /// <returns>非负的秒数。</returns>
        public float ReadDefaultAttackSeconds()
        {
            return m_DefaultAttack;
        }

        /// <summary>读取默认的恢复原音量时间。</summary>
        /// <returns>非负的秒数。</returns>
        public float ReadDefaultReleaseSeconds()
        {
            return m_DefaultRelease;
        }

        /// <summary>查找指定播放请求现有的压低状态。</summary>
        /// <param name="requestId">播放请求的唯一编号。</param>
        /// <returns>找到的状态；不存在时返回 <see langword="null"/>。</returns>
        private AudioDuckState FindState(long requestId)
        {
            for (int i = 0; i < m_States.Count; i++)
            {
                if (m_States[i].m_RequestId == requestId)
                {
                    return m_States[i];
                }
            }
            return null;
        }
    }
}
