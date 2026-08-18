using LCL;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameDll
{
    /// <summary>
    /// 提供声音系统的初始化、播放、过渡、暂停、缓存、音量和监听器管理入口。
    /// </summary>
    /// <remarks>
    /// <para>业务代码应通过 <see cref="GetInstance"/> 取得管理器，并在 <see cref="Init"/> 成功后调用播放接口。</para>
    /// <para>二维和三维声音均通过 <c>t_sound</c> 配置编号定位声音预制件。声音预制件的混音输出分组决定其所属的 <see cref="AudioBus"/>。</para>
    /// <para>管理器不依赖场景中的组件，初始化时会创建跨场景保留的运行时根节点，并在 <see cref="UnInit"/> 时对称释放。</para>
    /// </remarks>
    public class AudioManager
    {
        /// <summary>表示声音管理器当前的初始化生命周期状态。</summary>
        private enum AudioManagerLifecycleState
        {
            /// <summary>尚未初始化或已经完成反初始化。</summary>
            Uninitialized,
            /// <summary>混音器资源正在异步加载。</summary>
            Initializing,
            /// <summary>初始化完成，可以播放声音。</summary>
            Ready,
            /// <summary>初始化失败，管理器保持可调用但不会播放声音。</summary>
            FailedSilent,
            /// <summary>正在释放运行时资源。</summary>
            Uninitializing
        }

        private const string m_MixerAbName = "audio_mixer/main_mixer.jpg";
        private const string m_MixerAssetName = "main_mixer";

        private static AudioManager m_Instance;

        private readonly List<Action<bool>> m_InitCallbacks = new List<Action<bool>>();
        private readonly AudioPlaybackController m_PlaybackController;
        private readonly AudioBusController m_BusController;
        private readonly AudioDuckingController m_DuckingController;
        private readonly AudioListenerController m_ListenerController;

        private AudioManagerLifecycleState m_LifecycleState = AudioManagerLifecycleState.Uninitialized;
        private AudioTransitionController m_TransitionController;
        private AudioMixer m_Mixer;
        private ABRequest m_MixerRequest;
        private GameObject m_SoundManagerGo;
        private Transform m_SoundRoot;
        private long m_MixerLoadRequestId;
        private int m_LifecycleVersion;
        private int m_LastLifecycleWarningVersion = -1;
        private bool m_IsGameAudioPaused;
        private bool m_IsSystemPaused;

        /// <summary>
        /// 创建声音管理器及其内部控制器。
        /// </summary>
        private AudioManager()
        {
            m_PlaybackController = new AudioPlaybackController();
            m_BusController = new AudioBusController(m_PlaybackController.PauseBusRequests);
            m_DuckingController = new AudioDuckingController(m_BusController);
            m_ListenerController = new AudioListenerController();
        }

        /// <summary>
        /// 获取进程内唯一的声音管理器实例。
        /// </summary>
        /// <returns>可复用的 <see cref="AudioManager"/> 实例。</returns>
        public static AudioManager GetInstance()
        {
            if (m_Instance == null)
            {
                m_Instance = new AudioManager();
            }
            return m_Instance;
        }

        /// <summary>
        /// 异步加载主混音器并初始化声音系统。
        /// </summary>
        /// <param name="callback">初始化完成回调。成功时传入 <see langword="true"/>；进入静默失败状态时传入 <see langword="false"/>。允许传入 <see langword="null"/>。</param>
        /// <remarks>
        /// 重复调用不会创建多个初始化任务。初始化进行中时，回调会排队并在同一次结果产生后依次执行。
        /// </remarks>
        public void Init(Action<bool> callback)
        {
            if (m_LifecycleState == AudioManagerLifecycleState.Ready)
            {
                if (callback != null)
                {
                    callback(true);
                }
                return;
            }
            if (m_LifecycleState == AudioManagerLifecycleState.FailedSilent)
            {
                if (callback != null)
                {
                    callback(false);
                }
                return;
            }
            if (callback != null)
            {
                m_InitCallbacks.Add(callback);
            }
            if (m_LifecycleState == AudioManagerLifecycleState.Initializing)
            {
                return;
            }
            if (m_LifecycleState == AudioManagerLifecycleState.Uninitializing)
            {
                CompleteInitCallbacks(false);
                return;
            }

            m_LifecycleState = AudioManagerLifecycleState.Initializing;
            m_LifecycleVersion++;
            int lifecycleVersion = m_LifecycleVersion;
            m_MixerRequest = UIRes.LoadPrefabAsync(typeof(AudioMixer), m_MixerAbName, m_MixerAssetName, OnMixerLoaded, OnMixerLoadFailed, lifecycleVersion);
            if (m_MixerRequest == null)
            {
                FailInitialization("声音混音器加载请求创建失败");
                return;
            }
            m_MixerLoadRequestId = m_MixerRequest.LoadIndex;
        }

        /// <summary>
        /// 释放全部播放请求、声音缓存、监听器、混音器资源和运行时节点。
        /// </summary>
        /// <remarks>
        /// 该方法用于应用退出、重新登录或声音系统彻底重置。普通场景切换应使用 <see cref="Clear(AudioClearType)"/>，不要调用本方法。
        /// </remarks>
        public void UnInit()
        {
            if (m_LifecycleState == AudioManagerLifecycleState.Uninitialized || m_LifecycleState == AudioManagerLifecycleState.Uninitializing)
            {
                return;
            }
            m_LifecycleState = AudioManagerLifecycleState.Uninitializing;
            m_LifecycleVersion++;
            m_BusController.SaveVolumeSettings();
            m_PlaybackController.UnInit();
            m_DuckingController.Clear();
            m_ListenerController.UnInit();
            m_BusController.UnInit();
            if (m_MixerRequest != null)
            {
                UIRes.UnloadPrefab(m_MixerRequest);
                m_MixerRequest = null;
            }
            m_MixerLoadRequestId = 0;
            m_TransitionController = null;
            m_Mixer = null;
            DestroyRuntimeObjects();
            m_IsGameAudioPaused = false;
            m_IsSystemPaused = false;
            CompleteInitCallbacks(false);
            m_LifecycleState = AudioManagerLifecycleState.Uninitialized;
        }

        /// <summary>
        /// 使用默认策略播放一个二维声音。
        /// </summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <returns>播放请求句柄；管理器未就绪、配置无效、资源处于失败冷却或被最小播放间隔拦截时返回默认句柄。</returns>
        /// <remarks>
        /// 默认策略为：不交叉过渡、创建新实例、临时生命周期，并使用管理器默认缓存时间。
        /// </remarks>
        public AudioHandle Play2D(int id)
        {
            return Play2D(id, AudioTransitionMode.None, 0f, AudioReplayMode.CreateNew, AudioLifetime.Transient, -1f, default(AudioDuckOptions), default(AudioPlaybackCallbacks));
        }

        /// <summary>使用默认策略播放一个二维声音，并注册需要关注的结束回调。</summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <param name="callbacks">根据最终结束原因选择执行的业务回调。</param>
        /// <returns>播放请求句柄；播放请求未创建时返回默认句柄。</returns>
        public AudioHandle Play2D(int id, AudioPlaybackCallbacks callbacks)
        {
            return Play2D(id, AudioTransitionMode.None, 0f, AudioReplayMode.CreateNew, AudioLifetime.Transient, -1f, default(AudioDuckOptions), callbacks);
        }

        /// <summary>
        /// 播放一个二维声音，并在播放期间压低指定声音总线。
        /// </summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <param name="duckOptions">声音压低参数。默认结构体或目标音量为 1 时不产生压低效果。</param>
        /// <returns>播放请求句柄；播放请求未创建时返回默认句柄。</returns>
        public AudioHandle Play2D(int id, AudioDuckOptions duckOptions)
        {
            return Play2D(id, AudioTransitionMode.None, 0f, AudioReplayMode.CreateNew, AudioLifetime.Transient, -1f, duckOptions, default(AudioPlaybackCallbacks));
        }

        /// <summary>
        /// 使用完整策略播放一个二维声音。
        /// </summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <param name="transitionMode">新声音开始时采用的过渡方式。</param>
        /// <param name="transitionSeconds">过渡秒数。小于 0 使用所属总线默认时间，0 立即切换，大于 0 使用指定时间。</param>
        /// <param name="replayMode">相同配置编号已经存在播放请求时的处理方式。</param>
        /// <param name="lifetime">流程清理时的保留级别。</param>
        /// <param name="cacheSeconds">播放结束后的实例缓存秒数。小于 0 使用管理器默认时间，0 立即释放，大于 0 保留指定时间。</param>
        /// <param name="duckOptions">该声音播放期间对其他声音总线执行的压低参数。</param>
        /// <param name="callbacks">根据最终结束原因选择执行的业务回调。</param>
        /// <returns>播放请求句柄；播放请求未创建时返回默认句柄。</returns>
        /// <example>
        /// 使用默认过渡时间切换背景音乐，并保留新的背景音乐。
        /// <code>
        /// AudioHandle music = AudioManager.GetInstance().Play2D(
        ///     10,
        ///     AudioTransitionMode.CrossFade,
        ///     -1f,
        ///     AudioReplayMode.RestartCurrent,
        ///     AudioLifetime.Persistent);
        /// </code>
        /// </example>
        public AudioHandle Play2D(int id, AudioTransitionMode transitionMode, float transitionSeconds = -1f, AudioReplayMode replayMode = AudioReplayMode.CreateNew, AudioLifetime lifetime = AudioLifetime.Transient, float cacheSeconds = -1f, AudioDuckOptions duckOptions = default(AudioDuckOptions), AudioPlaybackCallbacks callbacks = default(AudioPlaybackCallbacks))
        {
            if (!CanPlay())
            {
                m_PlaybackController.NotifyRejected(id, callbacks, "声音管理器尚未完成初始化");
                return default(AudioHandle);
            }
            return m_PlaybackController.Play(id, false, m_ListenerController.ReadPosition(), transitionMode, transitionSeconds, replayMode, lifetime, cacheSeconds, duckOptions, callbacks);
        }

        /// <summary>
        /// 使用默认策略在指定世界坐标播放一个三维声音。
        /// </summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <param name="position">声音发声点的世界坐标。</param>
        /// <returns>播放请求句柄；播放请求未创建时返回默认句柄。</returns>
        public AudioHandle Play3D(int id, Vector3 position)
        {
            return Play3D(id, position, AudioTransitionMode.None, 0f, AudioReplayMode.CreateNew, AudioLifetime.Transient, -1f, default(AudioDuckOptions), default(AudioPlaybackCallbacks));
        }

        /// <summary>使用默认策略在指定世界坐标播放一个三维声音，并注册需要关注的结束回调。</summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <param name="position">声音发声点的世界坐标。</param>
        /// <param name="callbacks">根据最终结束原因选择执行的业务回调。</param>
        /// <returns>播放请求句柄；播放请求未创建时返回默认句柄。</returns>
        public AudioHandle Play3D(int id, Vector3 position, AudioPlaybackCallbacks callbacks)
        {
            return Play3D(id, position, AudioTransitionMode.None, 0f, AudioReplayMode.CreateNew, AudioLifetime.Transient, -1f, default(AudioDuckOptions), callbacks);
        }

        /// <summary>
        /// 在指定世界坐标播放一个三维声音，并在播放期间压低指定声音总线。
        /// </summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <param name="position">声音发声点的世界坐标。</param>
        /// <param name="duckOptions">声音压低参数。</param>
        /// <returns>播放请求句柄；播放请求未创建时返回默认句柄。</returns>
        public AudioHandle Play3D(int id, Vector3 position, AudioDuckOptions duckOptions)
        {
            return Play3D(id, position, AudioTransitionMode.None, 0f, AudioReplayMode.CreateNew, AudioLifetime.Transient, -1f, duckOptions, default(AudioPlaybackCallbacks));
        }

        /// <summary>
        /// 使用完整策略在指定世界坐标播放一个三维声音。
        /// </summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <param name="position">声音发声点的世界坐标。</param>
        /// <param name="transitionMode">新声音开始时采用的过渡方式。</param>
        /// <param name="transitionSeconds">过渡秒数。小于 0 使用所属总线默认时间，0 立即切换，大于 0 使用指定时间。</param>
        /// <param name="replayMode">相同配置编号已经存在播放请求时的处理方式。</param>
        /// <param name="lifetime">流程清理时的保留级别。</param>
        /// <param name="cacheSeconds">播放结束后的实例缓存秒数。小于 0 使用管理器默认时间，0 立即释放，大于 0 保留指定时间。</param>
        /// <param name="duckOptions">该声音播放期间对其他声音总线执行的压低参数。</param>
        /// <param name="callbacks">根据最终结束原因选择执行的业务回调。</param>
        /// <returns>播放请求句柄；播放请求未创建时返回默认句柄。</returns>
        public AudioHandle Play3D(int id, Vector3 position, AudioTransitionMode transitionMode, float transitionSeconds = -1f, AudioReplayMode replayMode = AudioReplayMode.CreateNew, AudioLifetime lifetime = AudioLifetime.Transient, float cacheSeconds = -1f, AudioDuckOptions duckOptions = default(AudioDuckOptions), AudioPlaybackCallbacks callbacks = default(AudioPlaybackCallbacks))
        {
            if (!CanPlay())
            {
                m_PlaybackController.NotifyRejected(id, callbacks, "声音管理器尚未完成初始化");
                return default(AudioHandle);
            }
            return m_PlaybackController.Play(id, true, position, transitionMode, transitionSeconds, replayMode, lifetime, cacheSeconds, duckOptions, callbacks);
        }

        /// <summary>为尚未结束的播放请求追加需要关注的结束回调。</summary>
        /// <param name="handle">由播放接口返回的请求句柄。</param>
        /// <param name="callbacks">需要追加的回调；不会覆盖播放时已经注册的回调。</param>
        /// <returns>句柄有效且回调已经追加时返回 <see langword="true"/>。</returns>
        public bool RegisterPlaybackCallbacks(AudioHandle handle, AudioPlaybackCallbacks callbacks)
        {
            return m_PlaybackController.RegisterCallbacks(handle, callbacks);
        }

        /// <summary>
        /// 停止指定播放请求。
        /// </summary>
        /// <param name="handle">由播放接口返回的请求句柄。</param>
        /// <param name="transitionSeconds">停止淡出秒数。小于 0 使用所属总线默认时间，0 立即停止，大于 0 淡出指定时间后停止。</param>
        /// <remarks>无效、已完成或来自旧管理器生命周期的句柄会被安全忽略。</remarks>
        public void StopAudio(AudioHandle handle, float transitionSeconds = 0f)
        {
            m_PlaybackController.StopAudio(handle, transitionSeconds);
        }

        /// <summary>
        /// 手动暂停指定播放请求。
        /// </summary>
        /// <param name="handle">由播放接口返回的请求句柄。</param>
        /// <param name="transitionSeconds">暂停淡出秒数。小于 0 使用所属总线默认时间，0 立即暂停，大于 0 淡出指定时间后暂停。</param>
        /// <remarks>手动暂停原因与正式游戏暂停、系统暂停相互独立；所有暂停原因都解除后才会真正恢复播放。</remarks>
        public void PauseAudio(AudioHandle handle, float transitionSeconds = 0f)
        {
            m_PlaybackController.PauseAudio(handle, transitionSeconds);
        }

        /// <summary>
        /// 解除指定播放请求的手动暂停原因。
        /// </summary>
        /// <param name="handle">由播放接口返回的请求句柄。</param>
        /// <param name="transitionSeconds">恢复淡入秒数。小于 0 使用所属总线默认时间，0 立即恢复，大于 0 淡入指定时间。</param>
        /// <remarks>请求仍受正式游戏暂停或系统暂停影响时，本次调用只移除手动暂停原因，不会立即恢复声音。</remarks>
        public void ResumeAudio(AudioHandle handle, float transitionSeconds = 0f)
        {
            m_PlaybackController.ResumeAudio(handle, transitionSeconds);
        }

        /// <summary>
        /// 判断播放句柄是否仍对应当前生命周期内的有效请求。
        /// </summary>
        /// <param name="handle">需要检查的播放句柄。</param>
        /// <returns>请求仍处于加载、播放或暂停状态时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        public bool IsAudioHandleValid(AudioHandle handle)
        {
            return m_PlaybackController.IsAudioHandleValid(handle);
        }

        /// <summary>
        /// 读取指定播放请求的当前状态。
        /// </summary>
        /// <param name="handle">需要查询的播放句柄。</param>
        /// <returns>有效请求的当前状态；句柄无效时返回 <see cref="AudioPlaybackState.Released"/>。</returns>
        public AudioPlaybackState ReadAudioState(AudioHandle handle)
        {
            return m_PlaybackController.ReadAudioState(handle);
        }

        /// <summary>
        /// 进入正式游戏暂停状态，并按总线默认时间淡出背景音乐、世界声音和语音。
        /// </summary>
        /// <remarks>界面声音不会被正式游戏暂停，以便暂停界面继续播放操作反馈。</remarks>
        public void PauseGameAudio()
        {
            if (m_LifecycleState != AudioManagerLifecycleState.Ready || m_IsGameAudioPaused)
            {
                return;
            }
            m_IsGameAudioPaused = true;
            m_PlaybackController.SetGameAudioPaused(true);
            StartFormalPauseTransition(AudioBus.Music);
            StartFormalPauseTransition(AudioBus.World);
            StartFormalPauseTransition(AudioBus.Voice);
        }

        /// <summary>
        /// 解除正式游戏暂停状态，并按总线默认时间恢复背景音乐、世界声音和语音。
        /// </summary>
        public void ResumeGameAudio()
        {
            if (m_LifecycleState != AudioManagerLifecycleState.Ready || !m_IsGameAudioPaused)
            {
                return;
            }
            m_IsGameAudioPaused = false;
            m_PlaybackController.SetGameAudioPaused(false);
            ResumeFormalPause(AudioBus.Music);
            ResumeFormalPause(AudioBus.World);
            ResumeFormalPause(AudioBus.Voice);
        }

        /// <summary>
        /// 设置应用级系统暂停状态。
        /// </summary>
        /// <param name="paused">应用失去焦点或被系统挂起时传入 <see langword="true"/>；恢复时传入 <see langword="false"/>。</param>
        /// <remarks>系统暂停会冻结过渡计时并暂停全部播放请求，恢复时从原进度继续。</remarks>
        public void SetSystemPaused(bool paused)
        {
            if (m_IsSystemPaused == paused)
            {
                return;
            }
            m_IsSystemPaused = paused;
            if (m_TransitionController != null)
            {
                m_TransitionController.SetFrozen(paused);
            }
            m_BusController.SetFrozen(paused);
            m_PlaybackController.SetSystemPaused(paused);
        }

        /// <summary>
        /// 按指定范围清理播放请求或空闲声音资源。
        /// </summary>
        /// <param name="clearType">需要执行的清理范围。</param>
        /// <remarks><see cref="AudioClearType.All"/> 还会清除压低状态、正式游戏暂停状态和总线运行时控制音量。</remarks>
        public void Clear(AudioClearType clearType)
        {
            m_PlaybackController.Clear(clearType);
            if (clearType == AudioClearType.All)
            {
                m_DuckingController.Clear();
                m_IsGameAudioPaused = false;
                m_PlaybackController.SetGameAudioPaused(false);
                m_BusController.ResetControlStates();
            }
        }

        /// <summary>
        /// 推进监听器同步、声音过渡、总线控制、播放状态和压低恢复。
        /// </summary>
        /// <param name="unscaledDeltaTime">不受 <see cref="Time.timeScale"/> 影响的帧间隔秒数。</param>
        /// <remarks>该方法应由现有游戏主循环每帧调用一次。</remarks>
        public void Update(float unscaledDeltaTime)
        {
            m_ListenerController.SyncTransform();
            if (m_LifecycleState != AudioManagerLifecycleState.Ready)
            {
                // 未就绪时不推进声音状态，但仍需派发播放请求被拒绝的业务回调。
                m_PlaybackController.Update();
                return;
            }
            if (m_TransitionController != null)
            {
                m_TransitionController.Update(unscaledDeltaTime);
            }
            m_BusController.Update(unscaledDeltaTime);
            m_PlaybackController.Update();
            m_DuckingController.Update(unscaledDeltaTime);
        }

        /// <summary>
        /// 设置没有临时目标时使用的默认声音监听器目标。
        /// </summary>
        /// <param name="target">需要跟随位置和旋转的目标；传入 <see langword="null"/> 表示清除默认目标。</param>
        public void SetDefaultAudioListenerTarget(Transform target)
        {
            m_ListenerController.SetDefaultTarget(target);
        }

        /// <summary>
        /// 设置当前优先使用的声音监听器目标。
        /// </summary>
        /// <param name="target">临时目标；传入 <see langword="null"/> 后恢复跟随默认目标。</param>
        public void SetAudioListenerTarget(Transform target)
        {
            m_ListenerController.SetTarget(target);
        }

        /// <summary>
        /// 设置指定声音总线的默认过渡时间。
        /// </summary>
        /// <param name="bus">需要设置的声音总线。</param>
        /// <param name="seconds">默认过渡秒数；负数会按 0 处理。</param>
        public void SetDefaultTransitionSeconds(AudioBus bus, float seconds)
        {
            m_BusController.SetDefaultTransitionSeconds(bus, seconds);
        }

        /// <summary>读取指定声音总线的默认过渡时间。</summary>
        /// <param name="bus">需要查询的声音总线。</param>
        /// <returns>非负的默认过渡秒数。</returns>
        public float ReadDefaultTransitionSeconds(AudioBus bus)
        {
            return m_BusController.ReadDefaultTransitionSeconds(bus);
        }

        /// <summary>
        /// 设置声音压低功能的默认进入和恢复时间。
        /// </summary>
        /// <param name="attackSeconds">进入压低状态所用秒数；负数会按 0 处理。</param>
        /// <param name="releaseSeconds">恢复原音量所用秒数；负数会按 0 处理。</param>
        public void SetDefaultDuckTransitionSeconds(float attackSeconds, float releaseSeconds)
        {
            m_DuckingController.SetDefaultTransitionSeconds(attackSeconds, releaseSeconds);
        }

        /// <summary>读取声音压低功能默认的进入时间。</summary>
        /// <returns>非负的进入压低状态秒数。</returns>
        public float ReadDefaultDuckAttackSeconds()
        {
            return m_DuckingController.ReadDefaultAttackSeconds();
        }

        /// <summary>读取声音压低功能默认的恢复时间。</summary>
        /// <returns>非负的恢复原音量秒数。</returns>
        public float ReadDefaultDuckReleaseSeconds()
        {
            return m_DuckingController.ReadDefaultReleaseSeconds();
        }

        /// <summary>
        /// 设置播放结束后实例的默认缓存时间。
        /// </summary>
        /// <param name="seconds">默认缓存秒数；负数会按 0 处理。</param>
        public void SetDefaultCacheSeconds(float seconds)
        {
            m_PlaybackController.SetDefaultCacheSeconds(seconds);
        }

        /// <summary>读取播放结束后实例的默认缓存时间。</summary>
        /// <returns>非负的默认缓存秒数。</returns>
        public float ReadDefaultCacheSeconds()
        {
            return m_PlaybackController.ReadDefaultCacheSeconds();
        }

        /// <summary>设置背景音乐总线的玩家音量。</summary>
        /// <param name="value">0 到 1 的线性音量；超出范围时会被限制。</param>
        public void SetMusicVolume(float value)
        {
            m_BusController.SetBusVolume(AudioBus.Music, value);
        }

        /// <summary>读取背景音乐总线的玩家音量。</summary>
        /// <returns>0 到 1 的线性音量。</returns>
        public float ReadMusicVolume()
        {
            return m_BusController.ReadBusVolume(AudioBus.Music);
        }

        /// <summary>设置界面声音总线的玩家音量。</summary>
        /// <param name="value">0 到 1 的线性音量；超出范围时会被限制。</param>
        public void SetUIVolume(float value)
        {
            m_BusController.SetBusVolume(AudioBus.UI, value);
        }

        /// <summary>读取界面声音总线的玩家音量。</summary>
        /// <returns>0 到 1 的线性音量。</returns>
        public float ReadUIVolume()
        {
            return m_BusController.ReadBusVolume(AudioBus.UI);
        }

        /// <summary>设置世界声音总线的玩家音量。</summary>
        /// <param name="value">0 到 1 的线性音量；超出范围时会被限制。</param>
        public void SetWorldVolume(float value)
        {
            m_BusController.SetBusVolume(AudioBus.World, value);
        }

        /// <summary>读取世界声音总线的玩家音量。</summary>
        /// <returns>0 到 1 的线性音量。</returns>
        public float ReadWorldVolume()
        {
            return m_BusController.ReadBusVolume(AudioBus.World);
        }

        /// <summary>设置语音总线的玩家音量。</summary>
        /// <param name="value">0 到 1 的线性音量；超出范围时会被限制。</param>
        public void SetVoiceVolume(float value)
        {
            m_BusController.SetBusVolume(AudioBus.Voice, value);
        }

        /// <summary>读取语音总线的玩家音量。</summary>
        /// <returns>0 到 1 的线性音量。</returns>
        public float ReadVoiceVolume()
        {
            return m_BusController.ReadBusVolume(AudioBus.Voice);
        }

        /// <summary>设置全部声音的全局静音状态。</summary>
        /// <param name="muted">需要静音时传入 <see langword="true"/>；需要恢复时传入 <see langword="false"/>。</param>
        public void SetGlobalMuted(bool muted)
        {
            m_BusController.SetGlobalMuted(muted);
        }

        /// <summary>读取全部声音的全局静音状态。</summary>
        /// <returns>全局静音时返回 <see langword="true"/>。</returns>
        public bool ReadGlobalMuted()
        {
            return m_BusController.ReadGlobalMuted();
        }

        /// <summary>
        /// 立即将当前玩家音量和全局静音设置写入 <see cref="PlayerPrefs"/>。
        /// </summary>
        /// <remarks>各设置接口会先更新内存和 <see cref="PlayerPrefs"/> 缓存；可在设置界面关闭或应用退出时调用本方法持久化。</remarks>
        public void SaveVolumeSettings()
        {
            m_BusController.SaveVolumeSettings();
        }

        /// <summary>处理主混音器资源加载成功结果并建立所有内部控制器连接。</summary>
        /// <param name="data">资源系统返回的混音器资源数据。</param>
        /// <param name="userData">发起加载时记录的管理器生命周期版本。</param>
        private void OnMixerLoaded(ResData data, object userData)
        {
            int lifecycleVersion = userData is int ? (int)userData : -1;
            if (m_LifecycleState != AudioManagerLifecycleState.Initializing || lifecycleVersion != m_LifecycleVersion)
            {
                return;
            }
            m_Mixer = data != null ? data.m_Obj as AudioMixer : null;
            if (m_Mixer == null)
            {
                FailInitialization("声音混音器资源为空或类型错误");
                return;
            }
            if (!m_BusController.Initialize(m_Mixer))
            {
                FailInitialization("声音混音器缺少必要的暴露参数");
                return;
            }

            var transitionController = new AudioTransitionController(m_Mixer, m_BusController.ReadDefaultTransitionSeconds, m_PlaybackController.OnTransitionStopping, m_PlaybackController.OnTransitionStopped, m_PlaybackController.OnTransitionPaused);
            if (!transitionController.IsValid())
            {
                FailInitialization("声音混音器缺少必要的播放分组");
                return;
            }

            CreateRuntimeObjects();
            m_TransitionController = transitionController;
            m_PlaybackController.Initialize(m_SoundRoot, transitionController, m_BusController, m_DuckingController, m_LifecycleVersion, m_IsGameAudioPaused, m_IsSystemPaused);
            m_LifecycleState = AudioManagerLifecycleState.Ready;
            if (m_IsSystemPaused)
            {
                m_TransitionController.SetFrozen(true);
                m_BusController.SetFrozen(true);
            }
            CompleteInitCallbacks(true);
        }

        /// <summary>处理主混音器资源加载失败或取消结果。</summary>
        /// <param name="requestId">资源系统返回的加载请求编号。</param>
        /// <param name="result">资源加载结果。</param>
        private void OnMixerLoadFailed(long requestId, ABRequestResult result)
        {
            if (m_LifecycleState != AudioManagerLifecycleState.Initializing || requestId != m_MixerLoadRequestId)
            {
                return;
            }
            FailInitialization(result == ABRequestResult.Cancel ? "声音混音器资源加载已取消" : "声音混音器资源加载失败");
        }

        /// <summary>终止初始化并进入安全的静默失败状态。</summary>
        /// <param name="reason">用于错误日志的失败原因。</param>
        private void FailInitialization(string reason)
        {
            Debug.LogError("AudioManager 初始化失败，进入静默模式：" + reason);
            if (m_MixerRequest != null)
            {
                UIRes.UnloadPrefab(m_MixerRequest);
                m_MixerRequest = null;
            }
            m_MixerLoadRequestId = 0;
            m_TransitionController = null;
            m_BusController.UnInit();
            m_ListenerController.UnInit();
            m_Mixer = null;
            DestroyRuntimeObjects();
            m_LifecycleState = AudioManagerLifecycleState.FailedSilent;
            CompleteInitCallbacks(false);
        }

        /// <summary>以统一结果依次完成并清空所有等待中的初始化回调。</summary>
        /// <param name="success">初始化是否成功。</param>
        private void CompleteInitCallbacks(bool success)
        {
            while (m_InitCallbacks.Count > 0)
            {
                var callback = m_InitCallbacks[0];
                m_InitCallbacks.RemoveAt(0);
                if (callback != null)
                {
                    callback(success);
                }
            }
        }

        /// <summary>创建跨场景保留的声音根节点和唯一监听器。</summary>
        private void CreateRuntimeObjects()
        {
            m_SoundManagerGo = new GameObject("SoundManager");
            GameObject.DontDestroyOnLoad(m_SoundManagerGo);
            m_SoundRoot = m_SoundManagerGo.transform;
            m_ListenerController.Initialize(m_SoundRoot);
        }

        /// <summary>销毁声音根节点并清除运行时对象引用。</summary>
        private void DestroyRuntimeObjects()
        {
            if (m_SoundManagerGo != null)
            {
                GameObject.Destroy(m_SoundManagerGo);
            }
            m_SoundManagerGo = null;
            m_SoundRoot = null;
        }

        /// <summary>开始指定总线的正式暂停淡出，并在淡出完成后暂停其播放请求。</summary>
        /// <param name="bus">需要暂停的声音总线。</param>
        private void StartFormalPauseTransition(AudioBus bus)
        {
            m_BusController.StartControlTransition(bus, 0f, m_BusController.ReadDefaultTransitionSeconds(bus), true);
        }

        /// <summary>解除指定总线的正式暂停原因并开始恢复音量。</summary>
        /// <param name="bus">需要恢复的声音总线。</param>
        private void ResumeFormalPause(AudioBus bus)
        {
            m_PlaybackController.ResumeBusRequests(bus, AudioPauseReason.Formal);
            m_BusController.StartControlTransition(bus, 1f, m_BusController.ReadDefaultTransitionSeconds(bus), false);
        }

        /// <summary>判断管理器当前是否允许创建新的播放请求，并对不可播放状态进行限次警告。</summary>
        /// <returns>管理器处于就绪状态时返回 <see langword="true"/>。</returns>
        private bool CanPlay()
        {
            if (m_LifecycleState == AudioManagerLifecycleState.Ready)
            {
                return true;
            }
            if (m_LastLifecycleWarningVersion != m_LifecycleVersion)
            {
                m_LastLifecycleWarningVersion = m_LifecycleVersion;
                Debug.LogWarning("AudioManager 当前不可播放声音，状态=" + m_LifecycleState);
            }
            return false;
        }
    }
}
