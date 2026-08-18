using System;
using UnityEngine;
using UnityEngine.Audio;

namespace GameDll
{
    /// <summary>
    /// 管理主混音器中每条声音总线的玩家音量、运行时控制音量、压低音量和全局静音。
    /// </summary>
    /// <remarks>
    /// <para>玩家音量写入各总线的 <c>XxxVolume</c> 参数并持久化。</para>
    /// <para>正式暂停等运行时流程写入各总线的 <c>XxxControlVolume</c> 参数；声音压低系数只在这一层与运行时控制音量相乘。</para>
    /// <para>全局静音只写入 <c>MasterVolume</c>，不会破坏各总线已经保存的玩家设置。</para>
    /// </remarks>
    internal sealed class AudioBusController
    {
        /// <summary>保存一条声音总线运行时控制音量的线性过渡状态。</summary>
        private sealed class AudioControlState
        {
            public AudioBus m_Bus;
            public float m_CurrentLinear = 1f;
            public float m_StartLinear = 1f;
            public float m_TargetLinear = 1f;
            public float m_Duration;
            public float m_Elapsed;
            public bool m_IsChanging;
            public bool m_PauseBusOnComplete;
        }

        private const string m_MasterVolumeParameter = "MasterVolume";
        private const string m_GlobalMutedKey = "AudioGlobalMuted";
        private const string m_VolumeSettingsVersionKey = "AudioVolumeSettingsVersion";
        private const int m_CurrentVolumeSettingsVersion = 1;
        private const float m_SilentDecibel = -80f;

        private readonly string[] m_BusVolumeParameters = { "MusicVolume", "UIVolume", "WorldVolume", "VoiceVolume" };
        private readonly string[] m_BusControlParameters = { "MusicControlVolume", "UIControlVolume", "WorldControlVolume", "VoiceControlVolume" };
        private readonly string[] m_BusVolumeKeys = { "AudioMusicVolume", "AudioUIVolume", "AudioWorldVolume", "AudioVoiceVolume" };
        private readonly float[] m_DefaultTransitionSeconds = { 1f, 0f, 0f, 0.15f };
        private readonly float[] m_BusVolumes = { 1f, 1f, 1f, 1f };
        private readonly float[] m_DuckVolumes = { 1f, 1f, 1f, 1f };
        private readonly AudioControlState[] m_ControlStates = new AudioControlState[4];
        private readonly Action<AudioBus> m_OnPauseBus;

        private AudioMixer m_Mixer;
        private bool m_IsGlobalMuted;
        private bool m_IsFrozen;

        /// <summary>创建声音总线控制器。</summary>
        /// <param name="onPauseBus">运行时控制音量淡出完成后，用于真正暂停该总线播放请求的回调。</param>
        public AudioBusController(Action<AudioBus> onPauseBus)
        {
            m_OnPauseBus = onPauseBus;
            for (int i = 0; i < m_ControlStates.Length; i++)
            {
                m_ControlStates[i] = new AudioControlState { m_Bus = (AudioBus)i };
            }
        }

        /// <summary>
        /// 绑定主混音器，验证所需暴露参数，并恢复已保存的玩家音量设置。
        /// </summary>
        /// <param name="mixer">包含主音量、四条总线音量和四条总线控制音量参数的混音器。</param>
        /// <returns>全部必要参数存在并完成初始化时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        public bool Initialize(AudioMixer mixer)
        {
            m_Mixer = mixer;
            if (!ValidateMixerParameters())
            {
                m_Mixer = null;
                return false;
            }
            InitializeVolumes();
            ResetControlStates();
            return true;
        }

        /// <summary>解除混音器引用并重置控制器的运行状态。</summary>
        public void UnInit()
        {
            m_Mixer = null;
            m_IsFrozen = false;
        }

        /// <summary>设置是否冻结运行时控制音量的过渡计时。</summary>
        /// <param name="frozen">系统暂停期间传入 <see langword="true"/>；恢复时传入 <see langword="false"/>。</param>
        public void SetFrozen(bool frozen)
        {
            m_IsFrozen = frozen;
        }

        /// <summary>推进所有声音总线的运行时控制音量过渡。</summary>
        /// <param name="unscaledDeltaTime">不受 <see cref="Time.timeScale"/> 影响的帧间隔秒数。</param>
        public void Update(float unscaledDeltaTime)
        {
            if (m_IsFrozen)
            {
                return;
            }
            float deltaTime = Mathf.Max(0f, unscaledDeltaTime);
            for (int i = 0; i < m_ControlStates.Length; i++)
            {
                var state = m_ControlStates[i];
                if (!state.m_IsChanging)
                {
                    continue;
                }
                state.m_Elapsed += deltaTime;
                float progress = state.m_Duration > 0f ? Mathf.Clamp01(state.m_Elapsed / state.m_Duration) : 1f;
                state.m_CurrentLinear = Mathf.Lerp(state.m_StartLinear, state.m_TargetLinear, progress);
                ApplyControlVolume(state);
                if (progress < 1f)
                {
                    continue;
                }
                state.m_IsChanging = false;
                if (state.m_PauseBusOnComplete)
                {
                    state.m_PauseBusOnComplete = false;
                    NotifyPauseBus(state.m_Bus);
                }
            }
        }

        /// <summary>
        /// 将指定声音总线的运行时控制音量线性过渡到目标值。
        /// </summary>
        /// <param name="bus">需要控制的声音总线。</param>
        /// <param name="targetLinear">0 到 1 的目标线性音量。</param>
        /// <param name="duration">过渡秒数；负数会按 0 处理。</param>
        /// <param name="pauseBusOnComplete">到达目标音量后是否回调播放控制器真正暂停该总线。</param>
        public void StartControlTransition(AudioBus bus, float targetLinear, float duration, bool pauseBusOnComplete)
        {
            var state = m_ControlStates[(int)bus];
            state.m_StartLinear = state.m_CurrentLinear;
            state.m_TargetLinear = Mathf.Clamp01(targetLinear);
            state.m_Duration = Mathf.Max(0f, duration);
            state.m_Elapsed = 0f;
            state.m_IsChanging = state.m_Duration > 0f;
            state.m_PauseBusOnComplete = pauseBusOnComplete;
            if (state.m_IsChanging)
            {
                return;
            }
            state.m_CurrentLinear = state.m_TargetLinear;
            ApplyControlVolume(state);
            if (pauseBusOnComplete)
            {
                NotifyPauseBus(bus);
            }
        }

        /// <summary>立即将全部声音总线的运行时控制音量恢复为 1，并取消未完成的暂停回调。</summary>
        public void ResetControlStates()
        {
            for (int i = 0; i < m_ControlStates.Length; i++)
            {
                var state = m_ControlStates[i];
                state.m_CurrentLinear = 1f;
                state.m_StartLinear = 1f;
                state.m_TargetLinear = 1f;
                state.m_Duration = 0f;
                state.m_Elapsed = 0f;
                state.m_IsChanging = false;
                state.m_PauseBusOnComplete = false;
                ApplyControlVolume(state);
            }
        }

        /// <summary>设置指定声音总线的默认过渡时间。</summary>
        /// <param name="bus">需要设置的声音总线。</param>
        /// <param name="seconds">默认秒数；负数会按 0 处理。</param>
        public void SetDefaultTransitionSeconds(AudioBus bus, float seconds)
        {
            m_DefaultTransitionSeconds[(int)bus] = Mathf.Max(0f, seconds);
        }

        /// <summary>读取指定声音总线的默认过渡时间。</summary>
        /// <param name="bus">需要查询的声音总线。</param>
        /// <returns>非负的默认过渡秒数。</returns>
        public float ReadDefaultTransitionSeconds(AudioBus bus)
        {
            return m_DefaultTransitionSeconds[(int)bus];
        }

        /// <summary>将调用方指定的过渡时间解析为最终有效秒数。</summary>
        /// <param name="bus">用于读取默认值的声音总线。</param>
        /// <param name="requestedSeconds">调用方指定值；小于 0 表示使用总线默认值。</param>
        /// <returns>非负的最终过渡秒数。</returns>
        public float ResolveTransitionSeconds(AudioBus bus, float requestedSeconds)
        {
            return requestedSeconds < 0f ? ReadDefaultTransitionSeconds(bus) : Mathf.Max(0f, requestedSeconds);
        }

        /// <summary>设置并缓存指定声音总线的玩家音量。</summary>
        /// <param name="bus">需要设置的声音总线。</param>
        /// <param name="value">0 到 1 的线性音量；超出范围时会被限制。</param>
        public void SetBusVolume(AudioBus bus, float value)
        {
            int index = (int)bus;
            m_BusVolumes[index] = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(m_BusVolumeKeys[index], m_BusVolumes[index]);
            ApplyBusVolume(bus);
        }

        /// <summary>读取指定声音总线当前的玩家音量。</summary>
        /// <param name="bus">需要查询的声音总线。</param>
        /// <returns>0 到 1 的线性音量。</returns>
        public float ReadBusVolume(AudioBus bus)
        {
            return m_BusVolumes[(int)bus];
        }

        /// <summary>设置指定声音总线当前的压低系数。</summary>
        /// <param name="bus">需要设置的声音总线。</param>
        /// <param name="value">0 到 1 的线性系数；1 表示不压低。</param>
        public void SetDuckVolume(AudioBus bus, float value)
        {
            int index = (int)bus;
            float volume = Mathf.Clamp01(value);
            if (Mathf.Approximately(m_DuckVolumes[index], volume))
            {
                return;
            }
            m_DuckVolumes[index] = volume;
            ApplyControlVolume(m_ControlStates[index]);
        }

        /// <summary>设置并缓存全部声音的全局静音状态。</summary>
        /// <param name="muted">需要静音时传入 <see langword="true"/>。</param>
        public void SetGlobalMuted(bool muted)
        {
            m_IsGlobalMuted = muted;
            PlayerPrefs.SetInt(m_GlobalMutedKey, muted ? 1 : 0);
            ApplyGlobalMuted();
        }

        /// <summary>读取当前全局静音状态。</summary>
        /// <returns>全局静音时返回 <see langword="true"/>。</returns>
        public bool ReadGlobalMuted()
        {
            return m_IsGlobalMuted;
        }

        /// <summary>将已经写入 <see cref="PlayerPrefs"/> 缓存的音量设置立即保存到设备。</summary>
        public void SaveVolumeSettings()
        {
            PlayerPrefs.Save();
        }

        /// <summary>验证主混音器是否包含管理器运行所需的全部暴露参数。</summary>
        /// <returns>所有参数均可读取时返回 <see langword="true"/>。</returns>
        private bool ValidateMixerParameters()
        {
            if (m_Mixer == null)
            {
                return false;
            }
            float value;
            if (!m_Mixer.GetFloat(m_MasterVolumeParameter, out value))
            {
                return false;
            }
            for (int i = 0; i < m_BusVolumeParameters.Length; i++)
            {
                if (!m_Mixer.GetFloat(m_BusVolumeParameters[i], out value) || !m_Mixer.GetFloat(m_BusControlParameters[i], out value))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>从 <see cref="PlayerPrefs"/> 恢复玩家音量和全局静音设置，并写入混音器。</summary>
        private void InitializeVolumes()
        {
            // 新声音总线的线性音量含义与旧数据不兼容，版本变化时直接恢复推荐默认值。
            bool resetSavedVolumes = PlayerPrefs.GetInt(m_VolumeSettingsVersionKey, 0) != m_CurrentVolumeSettingsVersion;
            for (int i = 0; i < m_BusVolumes.Length; i++)
            {
                if (!resetSavedVolumes && PlayerPrefs.HasKey(m_BusVolumeKeys[i]))
                {
                    m_BusVolumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(m_BusVolumeKeys[i]));
                }
                else
                {
                    m_BusVolumes[i] = 1f;
                    PlayerPrefs.SetFloat(m_BusVolumeKeys[i], m_BusVolumes[i]);
                }
                ApplyBusVolume((AudioBus)i);
            }
            if (resetSavedVolumes)
            {
                m_IsGlobalMuted = false;
                PlayerPrefs.SetInt(m_GlobalMutedKey, 0);
                PlayerPrefs.SetInt(m_VolumeSettingsVersionKey, m_CurrentVolumeSettingsVersion);
            }
            else
            {
                m_IsGlobalMuted = PlayerPrefs.GetInt(m_GlobalMutedKey, 0) != 0;
            }
            ApplyGlobalMuted();
        }

        /// <summary>将指定总线的玩家线性音量转换为分贝后写入混音器。</summary>
        /// <param name="bus">需要更新的声音总线。</param>
        private void ApplyBusVolume(AudioBus bus)
        {
            if (m_Mixer != null)
            {
                m_Mixer.SetFloat(m_BusVolumeParameters[(int)bus], LinearToDecibel(m_BusVolumes[(int)bus]));
            }
        }

        /// <summary>合并运行时控制音量和压低系数，并写入指定总线的控制参数。</summary>
        /// <param name="state">需要应用的总线控制状态。</param>
        private void ApplyControlVolume(AudioControlState state)
        {
            if (m_Mixer != null)
            {
                int index = (int)state.m_Bus;
                m_Mixer.SetFloat(m_BusControlParameters[index], LinearToDecibel(state.m_CurrentLinear * m_DuckVolumes[index]));
            }
        }

        /// <summary>将全局静音状态写入主混音器音量参数。</summary>
        private void ApplyGlobalMuted()
        {
            if (m_Mixer != null)
            {
                m_Mixer.SetFloat(m_MasterVolumeParameter, m_IsGlobalMuted ? m_SilentDecibel : 0f);
            }
        }

        /// <summary>通知播放控制器暂停指定总线上的播放请求。</summary>
        /// <param name="bus">需要暂停的声音总线。</param>
        private void NotifyPauseBus(AudioBus bus)
        {
            if (m_OnPauseBus != null)
            {
                m_OnPauseBus(bus);
            }
        }

        /// <summary>将 0 到 1 的线性音量转换为混音器使用的分贝值。</summary>
        /// <param name="value">线性音量；小于等于 0 时按静音处理。</param>
        /// <returns>0 对应 -80 分贝，1 对应 0 分贝，其余值按 <c>20 * Log10(value)</c> 转换。</returns>
        private float LinearToDecibel(float value)
        {
            return value <= 0f ? m_SilentDecibel : 20f * Mathf.Log10(Mathf.Clamp01(value));
        }

    }
}
