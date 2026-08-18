using UnityEngine;
using UnityEngine.Audio;

namespace GameDll
{
    /// <summary>
    /// 封装一个由声音预制件实例化的 <see cref="AudioSource"/>，并保存其可复用前必须恢复的原始状态。
    /// </summary>
    /// <remarks>
    /// 播放控制器只复用没有绑定请求的实例。每次播放前会恢复预制件原始输出分组、音量、音高、空间混合、循环和静音设置，再应用本次请求的二维或三维参数。
    /// </remarks>
    internal sealed class AudioSourceInstance
    {
        private AudioSource m_AudioSource;
        private readonly AudioMixerGroup m_OriginalOutputGroup;
        private readonly float m_OriginalVolume = 1f;
        private readonly float m_OriginalPitch = 1f;
        private readonly float m_OriginalSpatialBlend;
        private readonly bool m_OriginalLoop;
        private readonly bool m_OriginalMute;
        private bool m_IsDestroyed;
        private bool m_IsSourcePaused;

        internal AudioCacheEntry m_CacheEntry;
        internal AudioPlaybackRequest m_Request;
        internal bool m_HasStarted;
        internal int m_PlayStartFrame;
        internal float m_IdleSince;
        internal float m_IdleCacheSeconds;

        /// <summary>创建运行时声音实例并记录预制件原始设置。</summary>
        /// <param name="source">由声音预制件创建的 <see cref="AudioSource"/>。</param>
        /// <param name="cacheEntry">该实例所属的资源缓存记录。</param>
        internal AudioSourceInstance(AudioSource source, AudioCacheEntry cacheEntry)
        {
            m_AudioSource = source;
            m_CacheEntry = cacheEntry;
            m_OriginalOutputGroup = source.outputAudioMixerGroup;
            m_OriginalVolume = source.volume;
            m_OriginalPitch = source.pitch;
            m_OriginalSpatialBlend = source.spatialBlend;
            m_OriginalLoop = source.loop;
            m_OriginalMute = source.mute;
            StopSource();
            RestoreOriginalAudioState();
        }

        /// <summary>将空闲实例重置并绑定到新的播放请求。</summary>
        /// <param name="request">即将使用该实例的播放请求。</param>
        internal void PrepareForPlay(AudioPlaybackRequest request)
        {
            StopSource();
            RestoreOriginalAudioState();
            m_Request = request;
            m_HasStarted = false;
            m_IsSourcePaused = false;
            m_IdleSince = 0f;
            m_IdleCacheSeconds = 0f;
            if (m_AudioSource == null)
            {
                return;
            }
            m_AudioSource.enabled = true;
            m_AudioSource.gameObject.SetActive(true);
            m_AudioSource.spatialBlend = request.m_Is3D ? 1f : 0f;
            m_AudioSource.transform.position = request.m_Position;
        }

        /// <summary>停止播放、解除请求绑定，并记录实例进入缓存的时间和保留时长。</summary>
        /// <param name="cacheSeconds">允许实例保持空闲以供复用的秒数。</param>
        internal void MoveToIdle(float cacheSeconds)
        {
            StopSource();
            RestoreOriginalAudioState();
            m_Request = null;
            m_IdleSince = Time.realtimeSinceStartup;
            m_IdleCacheSeconds = cacheSeconds;
        }

        /// <summary>从当前进度开始播放，并记录实际开始帧。</summary>
        internal void PlaySource()
        {
            if (m_AudioSource == null)
            {
                return;
            }
            m_IsSourcePaused = false;
            m_HasStarted = true;
            m_PlayStartFrame = Time.frameCount;
            m_AudioSource.Play();
        }

        /// <summary>立即停止底层 <see cref="AudioSource"/> 并清除暂停标记。</summary>
        internal void StopSource()
        {
            m_IsSourcePaused = false;
            if (m_AudioSource != null)
            {
                m_AudioSource.Stop();
            }
        }

        /// <summary>暂停已经开始的底层 <see cref="AudioSource"/>。</summary>
        internal void PauseSource()
        {
            if (m_AudioSource == null || !m_HasStarted)
            {
                return;
            }
            m_AudioSource.Pause();
            m_IsSourcePaused = true;
        }

        /// <summary>恢复暂停的底层声音；尚未开始时改为从头播放。</summary>
        internal void ResumeSource()
        {
            if (m_AudioSource == null)
            {
                return;
            }
            m_IsSourcePaused = false;
            if (m_HasStarted)
            {
                m_AudioSource.UnPause();
            }
            else
            {
                PlaySource();
            }
        }

        /// <summary>判断底层 <see cref="AudioSource"/> 当前是否正在播放。</summary>
        /// <returns>底层对象存在且正在播放时返回 <see langword="true"/>。</returns>
        internal bool IsPlayingSource()
        {
            return m_AudioSource != null && m_AudioSource.isPlaying;
        }

        /// <summary>判断该实例是否由管理器主动暂停。</summary>
        /// <returns>管理器已经调用暂停且尚未恢复时返回 <see langword="true"/>。</returns>
        internal bool IsSourcePaused()
        {
            return m_IsSourcePaused;
        }

        /// <summary>读取声音预制件是否启用了循环播放。</summary>
        /// <returns>底层 <see cref="AudioSource.loop"/> 的当前值。</returns>
        internal bool IsLooping()
        {
            return m_AudioSource != null && m_AudioSource.loop;
        }

        /// <summary>判断该实例或底层对象是否已经销毁。</summary>
        /// <returns>已经销毁时返回 <see langword="true"/>。</returns>
        internal bool IsDestroyed()
        {
            return m_IsDestroyed || m_AudioSource == null;
        }

        /// <summary>读取声音预制件原始连接的混音输出分组。</summary>
        /// <returns>创建实例时保存的原始输出分组。</returns>
        internal AudioMixerGroup ReadOriginalOutputGroup()
        {
            return m_OriginalOutputGroup;
        }

        /// <summary>读取声音预制件原始线性音量。</summary>
        /// <returns>创建实例时保存的 <see cref="AudioSource.volume"/>。</returns>
        internal float ReadBaseVolume()
        {
            return m_OriginalVolume;
        }

        /// <summary>读取过渡系统当前写入的运行时线性音量。</summary>
        /// <returns>底层对象存在时返回当前音量；否则返回预制件原始音量。</returns>
        internal float ReadRuntimeVolume()
        {
            return m_AudioSource != null ? m_AudioSource.volume : m_OriginalVolume;
        }

        /// <summary>设置过渡系统使用的运行时线性音量。</summary>
        /// <param name="volume">非负线性音量；负数会按 0 处理。</param>
        internal void SetRuntimeVolume(float volume)
        {
            if (m_AudioSource != null)
            {
                m_AudioSource.volume = Mathf.Max(0f, volume);
            }
        }

        /// <summary>临时切换到交叉过渡使用的混音输出分组。</summary>
        /// <param name="outputGroup">目标混音输出分组；传入 <see langword="null"/> 时保持当前分组。</param>
        internal void SetRuntimeOutputGroup(AudioMixerGroup outputGroup)
        {
            if (m_AudioSource != null && outputGroup != null)
            {
                m_AudioSource.outputAudioMixerGroup = outputGroup;
            }
        }

        /// <summary>恢复创建实例时保存的全部 <see cref="AudioSource"/> 设置。</summary>
        internal void RestoreOriginalAudioState()
        {
            if (m_AudioSource == null)
            {
                return;
            }
            m_AudioSource.outputAudioMixerGroup = m_OriginalOutputGroup;
            m_AudioSource.volume = m_OriginalVolume;
            m_AudioSource.pitch = m_OriginalPitch;
            m_AudioSource.spatialBlend = m_OriginalSpatialBlend;
            m_AudioSource.loop = m_OriginalLoop;
            m_AudioSource.mute = m_OriginalMute;
        }

        /// <summary>销毁底层游戏对象并清除请求和缓存引用。</summary>
        internal void Destroy()
        {
            if (m_IsDestroyed)
            {
                return;
            }
            m_IsDestroyed = true;
            if (m_AudioSource != null)
            {
                GameObject.Destroy(m_AudioSource.gameObject);
                m_AudioSource = null;
            }
            m_Request = null;
            m_CacheEntry = null;
        }
    }
}
