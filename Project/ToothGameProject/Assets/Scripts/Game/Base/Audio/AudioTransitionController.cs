using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameDll
{
    /// <summary>
    /// 管理单次停止、暂停、恢复淡入淡出，以及同一声音总线内的交叉淡入淡出。
    /// </summary>
    /// <remarks>
    /// <para>每条声音总线必须在主混音器中提供一个普通分组和两个过渡分组。两个过渡分组交替承载新旧声音，使它们可以同时播放并独立改变音量。</para>
    /// <para>交叉过渡采用等功率曲线，降低两个声音在中点同时衰减造成的明显音量凹陷。普通停止、暂停和恢复也复用同一过渡记录。</para>
    /// <para>本控制器只处理表现层音量和输出分组，播放请求状态由 <see cref="AudioPlaybackController"/> 通过回调维护。</para>
    /// </remarks>
    internal sealed class AudioTransitionController
    {
        /// <summary>定义一次过渡到达目标音量后需要执行的播放操作。</summary>
        private enum TransitionCompletion
        {
            /// <summary>过渡结束后继续保持播放。</summary>
            None,
            /// <summary>过渡结束后停止并回收实例。</summary>
            Stop,
            /// <summary>过渡结束后暂停实例。</summary>
            Pause
        }

        /// <summary>保存一条声音总线对应的普通分组和两个交叉过渡分组。</summary>
        private sealed class BusGroups
        {
            public AudioMixerGroup m_Normal;
            public AudioMixerGroup m_TransitionA;
            public AudioMixerGroup m_TransitionB;
        }

        /// <summary>保存单个声音实例的音量过渡状态和完成操作。</summary>
        private sealed class TransitionEntry
        {
            public AudioSourceInstance m_Instance;
            public float m_StartVolume;
            public float m_TargetVolume;
            public float m_ResumeVolume;
            public float m_Duration;
            public float m_Elapsed;
            public bool m_IsFading;
            public TransitionCompletion m_Completion;
        }

        /// <summary>保存一条声音总线当前的交叉过渡记录和下一次使用的分组。</summary>
        private sealed class CrossFadeState
        {
            public bool m_UseTransitionA;
            public readonly List<TransitionEntry> m_Entries = new List<TransitionEntry>();
        }

        private readonly BusGroups[] m_BusGroups = new BusGroups[4];
        private readonly CrossFadeState[] m_CrossFadeStates = new CrossFadeState[4];
        private readonly List<TransitionEntry> m_OperationEntries = new List<TransitionEntry>();
        private readonly Func<AudioBus, float> m_ReadDefaultTransitionSeconds;
        private readonly Action<AudioSourceInstance> m_OnStopping;
        private readonly Action<AudioSourceInstance> m_OnStopped;
        private readonly Action<AudioSourceInstance> m_OnPaused;
        private bool m_IsFrozen;

        /// <summary>
        /// 创建声音过渡控制器并解析主混音器中的全部必要分组。
        /// </summary>
        /// <param name="mixer">包含四条声音总线普通分组和交叉过渡分组的主混音器。</param>
        /// <param name="readDefaultTransitionSeconds">按声音总线读取默认过渡秒数的回调。</param>
        /// <param name="onStopping">声音开始停止淡出时的回调。</param>
        /// <param name="onStopped">声音完成停止淡出时的回调。</param>
        /// <param name="onPaused">声音完成暂停淡出时的回调。</param>
        public AudioTransitionController(AudioMixer mixer, Func<AudioBus, float> readDefaultTransitionSeconds, Action<AudioSourceInstance> onStopping, Action<AudioSourceInstance> onStopped, Action<AudioSourceInstance> onPaused)
        {
            m_ReadDefaultTransitionSeconds = readDefaultTransitionSeconds;
            m_OnStopping = onStopping;
            m_OnStopped = onStopped;
            m_OnPaused = onPaused;
            AddBusGroups(mixer, AudioBus.Music, "MusicNormal", "MusicTransitionA", "MusicTransitionB");
            AddBusGroups(mixer, AudioBus.UI, "UINormal", "UITransitionA", "UITransitionB");
            AddBusGroups(mixer, AudioBus.World, "WorldNormal", "WorldTransitionA", "WorldTransitionB");
            AddBusGroups(mixer, AudioBus.Voice, "VoiceNormal", "VoiceTransitionA", "VoiceTransitionB");
            for (int i = 0; i < m_CrossFadeStates.Length; i++)
            {
                m_CrossFadeStates[i] = new CrossFadeState();
            }
        }

        /// <summary>检查四条声音总线的必要混音分组是否全部解析成功。</summary>
        /// <returns>全部分组可用时返回 <see langword="true"/>。</returns>
        public bool IsValid()
        {
            for (int i = 0; i < m_BusGroups.Length; i++)
            {
                if (m_BusGroups[i] == null)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>根据声音预制件的输出分组判断其所属业务总线。</summary>
        /// <param name="outputGroup">声音预制件配置的混音输出分组。</param>
        /// <param name="bus">成功时返回对应的业务总线；失败时返回 <see cref="AudioBus.World"/>。</param>
        /// <returns>输出分组属于管理器支持的普通分组或过渡分组时返回 <see langword="true"/>。</returns>
        public bool TryResolveBus(AudioMixerGroup outputGroup, out AudioBus bus)
        {
            for (int i = 0; i < m_BusGroups.Length; i++)
            {
                var groups = m_BusGroups[i];
                if (groups != null && (outputGroup == groups.m_Normal || outputGroup == groups.m_TransitionA || outputGroup == groups.m_TransitionB))
                {
                    bus = (AudioBus)i;
                    return true;
                }
            }
            bus = AudioBus.World;
            return false;
        }

        /// <summary>
        /// 开始一次交叉淡入淡出，使新实例淡入并让同一总线中的已有过渡实例淡出停止。
        /// </summary>
        /// <param name="instance">需要作为新目标淡入并开始播放的声音实例。</param>
        /// <param name="bus">声音实例所属的业务总线。</param>
        /// <param name="transitionSeconds">过渡秒数；小于 0 时使用总线默认值。</param>
        /// <returns>实例和混音分组有效并成功开始交叉过渡时返回 <see langword="true"/>。</returns>
        public bool StartCrossFade(AudioSourceInstance instance, AudioBus bus, float transitionSeconds)
        {
            if (instance == null || instance.IsDestroyed())
            {
                return false;
            }
            int busIndex = (int)bus;
            var groups = m_BusGroups[busIndex];
            if (groups == null)
            {
                return false;
            }

            float duration = ResolveTransitionSeconds(bus, transitionSeconds);
            var state = m_CrossFadeStates[busIndex];
            for (int i = state.m_Entries.Count - 1; i >= 0; i--)
            {
                var outgoing = state.m_Entries[i];
                Retarget(outgoing, 0f, duration, TransitionCompletion.Stop);
                if (m_OnStopping != null)
                {
                    m_OnStopping(outgoing.m_Instance);
                }
                if (duration <= 0f || outgoing.m_Instance.IsSourcePaused())
                {
                    CompleteEntry(state.m_Entries, i, outgoing, true);
                }
            }

            state.m_UseTransitionA = !state.m_UseTransitionA;
            instance.SetRuntimeOutputGroup(state.m_UseTransitionA ? groups.m_TransitionA : groups.m_TransitionB);
            float targetVolume = instance.ReadBaseVolume();
            var entry = new TransitionEntry
            {
                m_Instance = instance,
                m_StartVolume = duration > 0f ? 0f : targetVolume,
                m_TargetVolume = targetVolume,
                m_ResumeVolume = targetVolume,
                m_Duration = duration,
                m_IsFading = duration > 0f
            };
            instance.SetRuntimeVolume(entry.m_StartVolume);
            instance.PlaySource();
            state.m_Entries.Add(entry);
            return true;
        }

        /// <summary>
        /// 继续保留已经位于交叉过渡中的目标实例，并让其他实例从当前音量反向淡出。
        /// </summary>
        /// <param name="instance">需要保留的现有声音实例。</param>
        /// <param name="bus">实例所属的业务总线。</param>
        /// <param name="transitionSeconds">重新调整过渡目标所用秒数；小于 0 时使用总线默认值。</param>
        /// <returns>找到目标实例并完成重新设定时返回 <see langword="true"/>。</returns>
        /// <remarks>该操作不会重置音频播放进度，音量从调用瞬间的实际值继续变化。</remarks>
        public bool KeepCrossFadeTarget(AudioSourceInstance instance, AudioBus bus, float transitionSeconds)
        {
            if (instance == null || instance.IsDestroyed())
            {
                return false;
            }
            var entries = m_CrossFadeStates[(int)bus].m_Entries;
            TransitionEntry target = null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].m_Instance == instance)
                {
                    target = entries[i];
                    break;
                }
            }
            if (target == null)
            {
                return false;
            }

            // 反向切换从双方当前音量继续，不能重置正在播放声音的进度。
            float duration = ResolveTransitionSeconds(bus, transitionSeconds);
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry == target)
                {
                    Retarget(entry, instance.ReadBaseVolume(), duration, TransitionCompletion.None);
                    continue;
                }
                Retarget(entry, 0f, duration, TransitionCompletion.Stop);
                if (m_OnStopping != null)
                {
                    m_OnStopping(entry.m_Instance);
                }
                if (duration <= 0f || entry.m_Instance.IsSourcePaused())
                {
                    CompleteEntry(entries, i, entry, true);
                }
            }
            return true;
        }

        /// <summary>将指定实例淡出并在过渡完成后停止。</summary>
        /// <param name="instance">需要停止的声音实例。</param>
        /// <param name="bus">实例所属的业务总线。</param>
        /// <param name="transitionSeconds">淡出秒数；小于 0 时使用总线默认值。</param>
        /// <returns>过渡记录创建或更新成功时返回 <see langword="true"/>。</returns>
        public bool Stop(AudioSourceInstance instance, AudioBus bus, float transitionSeconds)
        {
            TransitionEntry entry;
            List<TransitionEntry> owner;
            int index;
            bool isCrossFade;
            if (!TryFindEntry(instance, out entry, out owner, out index, out isCrossFade))
            {
                entry = CreateOperationEntry(instance);
                owner = m_OperationEntries;
                index = owner.Count - 1;
                isCrossFade = false;
            }
            float duration = ResolveTransitionSeconds(bus, transitionSeconds);
            Retarget(entry, 0f, duration, TransitionCompletion.Stop);
            if (m_OnStopping != null)
            {
                m_OnStopping(instance);
            }
            if (duration <= 0f)
            {
                CompleteEntry(owner, index, entry, isCrossFade);
            }
            return true;
        }

        /// <summary>将指定实例淡出并在过渡完成后暂停。</summary>
        /// <param name="instance">需要暂停的声音实例。</param>
        /// <param name="bus">实例所属的业务总线。</param>
        /// <param name="transitionSeconds">淡出秒数；小于 0 时使用总线默认值。</param>
        /// <returns>实例没有处于停止流程且过渡记录创建或更新成功时返回 <see langword="true"/>。</returns>
        public bool Pause(AudioSourceInstance instance, AudioBus bus, float transitionSeconds)
        {
            TransitionEntry entry;
            List<TransitionEntry> owner;
            int index;
            bool isCrossFade;
            if (!TryFindEntry(instance, out entry, out owner, out index, out isCrossFade))
            {
                entry = CreateOperationEntry(instance);
                owner = m_OperationEntries;
                index = owner.Count - 1;
                isCrossFade = false;
            }
            else if (entry.m_Completion == TransitionCompletion.Stop)
            {
                return false;
            }
            if (entry.m_Completion != TransitionCompletion.Pause)
            {
                entry.m_ResumeVolume = instance.ReadRuntimeVolume();
            }
            float duration = ResolveTransitionSeconds(bus, transitionSeconds);
            Retarget(entry, 0f, duration, TransitionCompletion.Pause);
            if (duration <= 0f)
            {
                CompleteEntry(owner, index, entry, isCrossFade);
            }
            return true;
        }

        /// <summary>恢复指定实例，并从当前音量淡入到暂停前音量。</summary>
        /// <param name="instance">需要恢复的声音实例。</param>
        /// <param name="bus">实例所属的业务总线。</param>
        /// <param name="transitionSeconds">淡入秒数；小于 0 时使用总线默认值。</param>
        /// <returns>实例没有处于停止流程且恢复已经开始时返回 <see langword="true"/>。</returns>
        public bool Resume(AudioSourceInstance instance, AudioBus bus, float transitionSeconds)
        {
            TransitionEntry entry;
            List<TransitionEntry> owner;
            int index;
            bool isCrossFade;
            bool hasEntry = TryFindEntry(instance, out entry, out owner, out index, out isCrossFade);
            if (hasEntry && entry.m_Completion == TransitionCompletion.Stop)
            {
                return false;
            }
            float duration = ResolveTransitionSeconds(bus, transitionSeconds);
            if (!hasEntry)
            {
                entry = CreateOperationEntry(instance);
                owner = m_OperationEntries;
                index = owner.Count - 1;
                isCrossFade = false;
            }
            if (entry.m_Completion != TransitionCompletion.Pause)
            {
                entry.m_ResumeVolume = instance.ReadBaseVolume();
                if (duration > 0f)
                {
                    instance.SetRuntimeVolume(0f);
                }
            }
            instance.ResumeSource();
            Retarget(entry, entry.m_ResumeVolume, duration, TransitionCompletion.None);
            if (duration <= 0f && !isCrossFade)
            {
                CompleteEntry(owner, index, entry, false);
            }
            return true;
        }

        /// <summary>移除指定实例的全部过渡记录，并恢复其预制件原始声音设置。</summary>
        /// <param name="instance">即将回池或销毁的声音实例。</param>
        public void Remove(AudioSourceInstance instance)
        {
            if (instance == null)
            {
                return;
            }
            for (int busIndex = 0; busIndex < m_CrossFadeStates.Length; busIndex++)
            {
                RemoveInstanceEntries(m_CrossFadeStates[busIndex].m_Entries, instance);
            }
            RemoveInstanceEntries(m_OperationEntries, instance);
            instance.RestoreOriginalAudioState();
        }

        /// <summary>设置是否冻结全部声音过渡计时。</summary>
        /// <param name="frozen">系统暂停期间传入 <see langword="true"/>。</param>
        public void SetFrozen(bool frozen)
        {
            m_IsFrozen = frozen;
        }

        /// <summary>推进全部交叉过渡和普通操作过渡。</summary>
        /// <param name="unscaledDeltaTime">不受 <see cref="Time.timeScale"/> 影响的帧间隔秒数。</param>
        public void Update(float unscaledDeltaTime)
        {
            if (m_IsFrozen)
            {
                return;
            }
            float deltaTime = Mathf.Max(0f, unscaledDeltaTime);
            for (int i = 0; i < m_CrossFadeStates.Length; i++)
            {
                UpdateEntries(m_CrossFadeStates[i].m_Entries, deltaTime, true);
            }
            UpdateEntries(m_OperationEntries, deltaTime, false);
        }

        /// <summary>取消全部过渡，恢复所有相关实例的预制件原始声音设置。</summary>
        public void Clear()
        {
            for (int i = 0; i < m_CrossFadeStates.Length; i++)
            {
                RestoreAndClear(m_CrossFadeStates[i].m_Entries);
            }
            RestoreAndClear(m_OperationEntries);
            m_IsFrozen = false;
        }

        /// <summary>为不在交叉过渡集合中的实例创建普通操作过渡记录。</summary>
        /// <param name="instance">需要执行停止、暂停或恢复过渡的实例。</param>
        /// <returns>已经加入普通操作集合的新记录。</returns>
        private TransitionEntry CreateOperationEntry(AudioSourceInstance instance)
        {
            var entry = new TransitionEntry { m_Instance = instance, m_ResumeVolume = instance.ReadRuntimeVolume() };
            m_OperationEntries.Add(entry);
            return entry;
        }

        /// <summary>推进一组过渡记录并处理到达目标音量的记录。</summary>
        /// <param name="entries">需要更新的过渡记录集合。</param>
        /// <param name="deltaTime">非负的帧间隔秒数。</param>
        /// <param name="isCrossFade">该集合是否属于交叉过渡状态。</param>
        private void UpdateEntries(List<TransitionEntry> entries, float deltaTime, bool isCrossFade)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                var instance = entry.m_Instance;
                if (instance == null || instance.IsDestroyed())
                {
                    entries.RemoveAt(i);
                    continue;
                }
                if (!entry.m_IsFading || instance.IsSourcePaused())
                {
                    continue;
                }
                entry.m_Elapsed += deltaTime;
                float progress = entry.m_Duration > 0f ? Mathf.Clamp01(entry.m_Elapsed / entry.m_Duration) : 1f;
                float curvedProgress = EvaluateEqualPowerProgress(progress, entry.m_TargetVolume >= entry.m_StartVolume);
                instance.SetRuntimeVolume(Mathf.Lerp(entry.m_StartVolume, entry.m_TargetVolume, curvedProgress));
                if (progress < 1f)
                {
                    continue;
                }
                entry.m_IsFading = false;
                instance.SetRuntimeVolume(entry.m_TargetVolume);
                CompleteEntry(entries, i, entry, isCrossFade);
            }
        }

        /// <summary>执行一条过渡记录的完成操作，并根据所属集合决定是否移除记录。</summary>
        /// <param name="owner">保存该记录的集合。</param>
        /// <param name="index">记录在集合中的索引。</param>
        /// <param name="entry">已经到达目标音量的记录。</param>
        /// <param name="isCrossFade">该记录是否属于交叉过渡集合。</param>
        private void CompleteEntry(List<TransitionEntry> owner, int index, TransitionEntry entry, bool isCrossFade)
        {
            var instance = entry.m_Instance;
            if (entry.m_Completion == TransitionCompletion.Stop)
            {
                owner.RemoveAt(index);
                instance.StopSource();
                instance.RestoreOriginalAudioState();
                if (m_OnStopped != null)
                {
                    m_OnStopped(instance);
                }
                return;
            }
            if (entry.m_Completion == TransitionCompletion.Pause)
            {
                instance.PauseSource();
                if (m_OnPaused != null)
                {
                    m_OnPaused(instance);
                }
                return;
            }
            if (!isCrossFade)
            {
                owner.RemoveAt(index);
                instance.RestoreOriginalAudioState();
            }
        }

        /// <summary>在全部交叉过渡和普通操作集合中查找指定声音实例。</summary>
        /// <param name="instance">需要查找的声音实例。</param>
        /// <param name="entry">成功时返回找到的过渡记录。</param>
        /// <param name="owner">成功时返回记录所属集合。</param>
        /// <param name="index">成功时返回记录在集合中的索引。</param>
        /// <param name="isCrossFade">成功时返回记录是否属于交叉过渡集合。</param>
        /// <returns>找到记录时返回 <see langword="true"/>。</returns>
        private bool TryFindEntry(AudioSourceInstance instance, out TransitionEntry entry, out List<TransitionEntry> owner, out int index, out bool isCrossFade)
        {
            for (int busIndex = 0; busIndex < m_CrossFadeStates.Length; busIndex++)
            {
                var entries = m_CrossFadeStates[busIndex].m_Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].m_Instance == instance)
                    {
                        entry = entries[i];
                        owner = entries;
                        index = i;
                        isCrossFade = true;
                        return true;
                    }
                }
            }
            for (int i = 0; i < m_OperationEntries.Count; i++)
            {
                if (m_OperationEntries[i].m_Instance == instance)
                {
                    entry = m_OperationEntries[i];
                    owner = m_OperationEntries;
                    index = i;
                    isCrossFade = false;
                    return true;
                }
            }
            entry = null;
            owner = null;
            index = -1;
            isCrossFade = false;
            return false;
        }

        /// <summary>解析一条声音总线所需的普通分组和两个交叉过渡分组。</summary>
        /// <param name="mixer">主混音器。</param>
        /// <param name="bus">需要建立分组映射的声音总线。</param>
        /// <param name="normalName">普通播放分组的完整名称。</param>
        /// <param name="transitionAName">第一交叉过渡分组的完整名称。</param>
        /// <param name="transitionBName">第二交叉过渡分组的完整名称。</param>
        private void AddBusGroups(AudioMixer mixer, AudioBus bus, string normalName, string transitionAName, string transitionBName)
        {
            if (mixer == null)
            {
                return;
            }
            var groups = new BusGroups
            {
                m_Normal = FindGroup(mixer, normalName),
                m_TransitionA = FindGroup(mixer, transitionAName),
                m_TransitionB = FindGroup(mixer, transitionBName)
            };
            if (groups.m_Normal != null && groups.m_TransitionA != null && groups.m_TransitionB != null)
            {
                m_BusGroups[(int)bus] = groups;
            }
        }

        /// <summary>按精确名称从混音器中查找输出分组。</summary>
        /// <param name="mixer">需要查询的主混音器。</param>
        /// <param name="groupName">目标分组的完整名称。</param>
        /// <returns>精确匹配的分组；不存在时返回 <see langword="null"/>。</returns>
        private AudioMixerGroup FindGroup(AudioMixer mixer, string groupName)
        {
            var groups = mixer.FindMatchingGroups(groupName);
            if (groups == null)
            {
                return null;
            }
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null && groups[i].name == groupName)
                {
                    return groups[i];
                }
            }
            return null;
        }

        /// <summary>将调用方指定的过渡时间解析为最终有效秒数。</summary>
        /// <param name="bus">用于读取默认值的声音总线。</param>
        /// <param name="requestedSeconds">调用方指定值；小于 0 表示使用总线默认值。</param>
        /// <returns>非负的最终过渡秒数。</returns>
        private float ResolveTransitionSeconds(AudioBus bus, float requestedSeconds)
        {
            if (requestedSeconds < 0f)
            {
                return m_ReadDefaultTransitionSeconds != null ? Mathf.Max(0f, m_ReadDefaultTransitionSeconds(bus)) : 0f;
            }
            return Mathf.Max(0f, requestedSeconds);
        }

        /// <summary>从实例当前音量重新设定一条过渡记录的目标。</summary>
        /// <param name="entry">需要重新设定的过渡记录。</param>
        /// <param name="targetVolume">目标线性音量。</param>
        /// <param name="duration">过渡秒数。</param>
        /// <param name="completion">到达目标音量后需要执行的操作。</param>
        private void Retarget(TransitionEntry entry, float targetVolume, float duration, TransitionCompletion completion)
        {
            entry.m_StartVolume = entry.m_Instance.ReadRuntimeVolume();
            entry.m_TargetVolume = targetVolume;
            entry.m_Duration = duration;
            entry.m_Elapsed = 0f;
            entry.m_IsFading = duration > 0f;
            entry.m_Completion = completion;
            if (duration <= 0f)
            {
                entry.m_Instance.SetRuntimeVolume(targetVolume);
            }
        }

        /// <summary>从指定集合移除一个声音实例的全部过渡记录。</summary>
        /// <param name="entries">需要检查的过渡记录集合。</param>
        /// <param name="instance">需要移除的声音实例。</param>
        private void RemoveInstanceEntries(List<TransitionEntry> entries, AudioSourceInstance instance)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].m_Instance == instance)
                {
                    entries.RemoveAt(i);
                }
            }
        }

        /// <summary>恢复集合内全部实例的原始声音设置并清空集合。</summary>
        /// <param name="entries">需要恢复并清空的过渡记录集合。</param>
        private void RestoreAndClear(List<TransitionEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].m_Instance != null)
                {
                    entries[i].m_Instance.RestoreOriginalAudioState();
                }
            }
            entries.Clear();
        }

        /// <summary>计算等功率淡入或淡出曲线上的归一化进度。</summary>
        /// <param name="progress">0 到 1 的线性时间进度。</param>
        /// <param name="fadeIn">淡入时传入 <see langword="true"/>；淡出时传入 <see langword="false"/>。</param>
        /// <returns>0 到 1 的等功率曲线进度。</returns>
        private float EvaluateEqualPowerProgress(float progress, bool fadeIn)
        {
            float angle = Mathf.Clamp01(progress) * Mathf.PI * 0.5f;
            return fadeIn ? Mathf.Sin(angle) : 1f - Mathf.Cos(angle);
        }
    }
}
