using MonoBean;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    /// <summary>
    /// 统一管理声音播放请求的创建、配置读取、重播策略、暂停原因、状态迁移和完成回收。
    /// </summary>
    /// <remarks>
    /// <para>本控制器是声音业务状态的唯一维护者。资源加载交给 <see cref="AudioResourceCache"/>，音量过渡交给 <see cref="AudioTransitionController"/>，总线控制交给 <see cref="AudioBusController"/>。</para>
    /// <para>相同声音的判断依据是 <c>t_sound</c> 配置编号，而不是解析后的资源路径，以保留配置人员表达的重播意图。</para>
    /// <para>请求可能同时具有多个 <see cref="AudioPauseReason"/>。只有全部暂停原因都解除后，声音才会恢复播放。</para>
    /// </remarks>
    internal sealed class AudioPlaybackController
    {
        /// <summary>保存一条等待在安全时机派发的业务回调。</summary>
        private struct CallbackInvocation
        {
            public Action<AudioPlaybackResult> m_Callback;
            public AudioPlaybackResult m_Result;
        }

        private readonly Dictionary<long, AudioPlaybackRequest> m_Requests = new Dictionary<long, AudioPlaybackRequest>();
        private readonly Dictionary<int, List<AudioPlaybackRequest>> m_RequestsByConfigId = new Dictionary<int, List<AudioPlaybackRequest>>();
        private readonly Dictionary<int, float> m_AudioLastPlayTimes = new Dictionary<int, float>();
        private readonly List<AudioSourceInstance> m_PlayingInstances = new List<AudioSourceInstance>();
        private readonly List<AudioPlaybackRequest> m_RequestBuffer = new List<AudioPlaybackRequest>();
        private readonly List<CallbackInvocation> m_CallbackInvocations = new List<CallbackInvocation>();
        private readonly List<CallbackInvocation> m_CallbackInvocationBuffer = new List<CallbackInvocation>();
        private readonly HashSet<int> m_LoadWarningConfigIds = new HashSet<int>();
        private readonly HashSet<int> m_RouteWarningConfigIds = new HashSet<int>();
        private readonly HashSet<int> m_LoopLifetimeWarningConfigIds = new HashSet<int>();
        private readonly AudioResourceCache m_ResourceCache;

        private AudioTransitionController m_TransitionController;
        private AudioBusController m_BusController;
        private AudioDuckingController m_DuckingController;
        private readonly AudioPlaybackRequest[] m_LatestCrossFadeRequests = new AudioPlaybackRequest[4];
        private int m_LifecycleVersion;
        private long m_NextRequestId = 1;
        private long m_NextSequence = 1;
        private float m_DefaultCacheSeconds = 30f;
        private bool m_IsReady;
        private bool m_IsGameAudioPaused;
        private bool m_IsSystemPaused;
        private bool m_IsFlushingCallbacks;

        /// <summary>创建播放控制器并建立资源缓存到播放状态的回调链。</summary>
        public AudioPlaybackController()
        {
            m_ResourceCache = new AudioResourceCache(IsRequestRegistered, OnCacheEntryReady, OnCacheRequestFailed, OnBeforeDestroyInstance);
        }

        /// <summary>初始化播放控制器及其资源、过渡、总线和压低依赖。</summary>
        /// <param name="soundRoot">运行时声音实例的父节点。</param>
        /// <param name="transitionController">负责停止、暂停、恢复和交叉淡入淡出的控制器。</param>
        /// <param name="busController">负责总线音量和默认过渡时间的控制器。</param>
        /// <param name="duckingController">负责声音播放期间压低其他总线的控制器。</param>
        /// <param name="lifecycleVersion">当前声音管理器生命周期版本。</param>
        /// <param name="isGameAudioPaused">初始化时是否已经处于正式游戏暂停状态。</param>
        /// <param name="isSystemPaused">初始化时是否已经处于系统暂停状态。</param>
        public void Initialize(Transform soundRoot, AudioTransitionController transitionController, AudioBusController busController, AudioDuckingController duckingController, int lifecycleVersion, bool isGameAudioPaused, bool isSystemPaused)
        {
            m_TransitionController = transitionController;
            m_BusController = busController;
            m_DuckingController = duckingController;
            m_LifecycleVersion = lifecycleVersion;
            m_IsGameAudioPaused = isGameAudioPaused;
            m_IsSystemPaused = isSystemPaused;
            m_IsReady = true;
            m_ResourceCache.Initialize(soundRoot, transitionController, lifecycleVersion);
        }

        /// <summary>释放全部播放请求和资源缓存，并清除所有运行时依赖与状态。</summary>
        public void UnInit()
        {
            m_IsReady = false;
            ClearAllAudioResources();
            FlushCallbacks();
            m_ResourceCache.UnInit();
            m_TransitionController = null;
            m_BusController = null;
            m_DuckingController = null;
            m_IsGameAudioPaused = false;
            m_IsSystemPaused = false;
            m_LifecycleVersion = 0;
        }

        /// <summary>
        /// 根据声音配置编号和完整播放策略创建或复用播放请求。
        /// </summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <param name="is3D">是否在真正播放前把实例配置为三维空间声音。</param>
        /// <param name="position">三维声音发声点的世界坐标。</param>
        /// <param name="transitionMode">新请求开始时使用的过渡方式。</param>
        /// <param name="transitionSeconds">过渡秒数；小于 0 表示使用所属总线默认值。</param>
        /// <param name="replayMode">相同配置编号已经存在请求时的处理方式。</param>
        /// <param name="lifetime">流程清理时的保留级别。</param>
        /// <param name="cacheSeconds">实例播放结束后的缓存秒数；小于 0 表示使用管理器默认值。</param>
        /// <param name="duckOptions">该请求播放期间使用的总线压低参数。</param>
        /// <param name="callbacks">根据最终结束原因选择执行的业务回调。</param>
        /// <returns>成功创建或保留请求时返回有效句柄；任何前置条件不满足时返回默认句柄。</returns>
        public AudioHandle Play(int id, bool is3D, Vector3 position, AudioTransitionMode transitionMode, float transitionSeconds, AudioReplayMode replayMode, AudioLifetime lifetime, float cacheSeconds, AudioDuckOptions duckOptions, AudioPlaybackCallbacks callbacks)
        {
            if (!m_IsReady)
            {
                NotifyRejected(id, callbacks, "声音管理器尚未完成初始化");
                return default(AudioHandle);
            }
            var config = t_soundBean.GetConfig(id, false);
            if (config == null)
            {
                WarnLoadFailure(id, string.Empty, "声音配置不存在");
                NotifyRejected(id, callbacks, "声音配置不存在");
                return default(AudioHandle);
            }
            if (string.IsNullOrWhiteSpace(config.t_res_abname))
            {
                WarnLoadFailure(id, string.Empty, "声音资源路径为空");
                NotifyRejected(id, callbacks, "声音资源路径为空");
                return default(AudioHandle);
            }

            AudioPlaybackRequest current;
            if (replayMode == AudioReplayMode.KeepCurrent && TryReadLatestRequest(id, out current))
            {
                KeepCurrentRequest(current, transitionMode, transitionSeconds, duckOptions, callbacks);
                return new AudioHandle(current.m_RequestId, current.m_LifecycleVersion);
            }
            if (!CheckMinimumPlayInterval(config))
            {
                NotifyRejected(id, callbacks, "未达到配置的最小播放间隔");
                return default(AudioHandle);
            }
            if (replayMode == AudioReplayMode.RestartCurrent)
            {
                StopAllRequestsByConfigId(id, AudioPlaybackEndReason.Replaced);
            }

            string abName = config.t_res_abname;
            var key = new AudioResourceKey(abName, Tool.GetAssetName(abName), typeof(GameObject));
            if (m_ResourceCache.IsInFailureCooldown(key))
            {
                NotifyRejected(id, callbacks, "声音资源处于加载失败冷却期");
                return default(AudioHandle);
            }

            var request = CreatePlaybackRequest(id, is3D, position, transitionMode, transitionSeconds, replayMode, lifetime, cacheSeconds, duckOptions, callbacks);
            m_ResourceCache.Request(request, key);
            var handle = new AudioHandle(request.m_RequestId, request.m_LifecycleVersion);
            return IsAudioHandleValid(handle) ? handle : default(AudioHandle);
        }

        /// <summary>停止指定播放请求，并根据参数选择立即停止或淡出停止。</summary>
        /// <param name="handle">需要停止的播放请求句柄。</param>
        /// <param name="transitionSeconds">停止淡出秒数；小于 0 表示使用所属总线默认值。</param>
        public void StopAudio(AudioHandle handle, float transitionSeconds)
        {
            AudioPlaybackRequest request;
            if (!TryGetRequest(handle, out request))
            {
                return;
            }
            if (request.m_Instance == null || request.m_State == AudioPlaybackState.WaitingForLoad || request.m_State == AudioPlaybackState.Paused)
            {
                StopRequestImmediate(request, AudioPlaybackEndReason.Stopped);
                return;
            }
            float duration = ResolveTransitionSeconds(request.m_Bus, transitionSeconds);
            if (duration <= 0f || m_TransitionController == null || !m_TransitionController.Stop(request.m_Instance, request.m_Bus, duration))
            {
                StopRequestImmediate(request, AudioPlaybackEndReason.Stopped);
                return;
            }
            SetPendingEndReason(request, AudioPlaybackEndReason.Stopped);
            request.m_IsStopping = true;
        }

        /// <summary>为仍然活动的播放请求追加结束回调。</summary>
        /// <param name="handle">需要追加回调的播放请求句柄。</param>
        /// <param name="callbacks">需要追加的回调；不会覆盖该请求已有回调。</param>
        /// <returns>句柄有效且请求尚未结束时返回 <see langword="true"/>。</returns>
        public bool RegisterCallbacks(AudioHandle handle, AudioPlaybackCallbacks callbacks)
        {
            AudioPlaybackRequest request;
            if (!callbacks.HasAny() || !TryGetRequest(handle, out request))
            {
                return false;
            }
            request.m_Callbacks.Append(callbacks);
            return true;
        }

        /// <summary>记录一个没有创建播放请求的拒绝结果，并等待主线程安全派发。</summary>
        /// <param name="configId"><c>t_sound</c> 配置编号。</param>
        /// <param name="callbacks">本次播放调用注册的回调。</param>
        /// <param name="message">请求被拒绝的原因。</param>
        public void NotifyRejected(int configId, AudioPlaybackCallbacks callbacks, string message)
        {
            QueueCallback(default(AudioHandle), configId, AudioPlaybackEndReason.Rejected, message, callbacks);
        }

        /// <summary>为指定播放请求增加手动暂停原因，并根据参数选择立即暂停或淡出暂停。</summary>
        /// <param name="handle">需要暂停的播放请求句柄。</param>
        /// <param name="transitionSeconds">暂停淡出秒数；小于 0 表示使用所属总线默认值。</param>
        public void PauseAudio(AudioHandle handle, float transitionSeconds)
        {
            AudioPlaybackRequest request;
            if (!TryGetRequest(handle, out request) || request.m_IsStopping)
            {
                return;
            }
            if (request.m_Instance == null)
            {
                request.m_PauseReasons |= AudioPauseReason.Manual;
                request.m_State = AudioPlaybackState.Paused;
                return;
            }
            if (request.m_State == AudioPlaybackState.Paused)
            {
                request.m_PauseReasons |= AudioPauseReason.Manual;
                return;
            }
            float duration = ResolveTransitionSeconds(request.m_Bus, transitionSeconds);
            request.m_PauseReasons |= AudioPauseReason.Manual;
            if (duration > 0f && m_TransitionController != null && m_TransitionController.Pause(request.m_Instance, request.m_Bus, duration))
            {
                return;
            }
            PauseRequest(request, AudioPauseReason.Manual);
        }

        /// <summary>移除指定播放请求的手动暂停原因，并在没有其他暂停原因时恢复播放。</summary>
        /// <param name="handle">需要恢复的播放请求句柄。</param>
        /// <param name="transitionSeconds">恢复淡入秒数；小于 0 表示使用所属总线默认值。</param>
        public void ResumeAudio(AudioHandle handle, float transitionSeconds)
        {
            AudioPlaybackRequest request;
            if (!TryGetRequest(handle, out request) || (request.m_PauseReasons & AudioPauseReason.Manual) == 0)
            {
                return;
            }
            request.m_PauseReasons &= ~AudioPauseReason.Manual;
            if (request.m_PauseReasons == AudioPauseReason.None)
            {
                ResumeRequest(request, transitionSeconds);
            }
        }

        /// <summary>判断播放句柄是否仍对应当前生命周期内的活动请求。</summary>
        /// <param name="handle">需要检查的播放句柄。</param>
        /// <returns>请求仍处于加载、播放或暂停状态时返回 <see langword="true"/>。</returns>
        public bool IsAudioHandleValid(AudioHandle handle)
        {
            AudioPlaybackRequest request;
            return TryGetRequest(handle, out request);
        }

        /// <summary>读取播放句柄对应请求的当前状态。</summary>
        /// <param name="handle">需要查询的播放句柄。</param>
        /// <returns>有效请求的当前状态；句柄无效时返回 <see cref="AudioPlaybackState.Released"/>。</returns>
        public AudioPlaybackState ReadAudioState(AudioHandle handle)
        {
            AudioPlaybackRequest request;
            return TryGetRequest(handle, out request) ? request.m_State : AudioPlaybackState.Released;
        }

        /// <summary>记录正式游戏暂停状态，供异步加载完成的新请求决定是否播放。</summary>
        /// <param name="paused">当前是否处于正式游戏暂停状态。</param>
        public void SetGameAudioPaused(bool paused)
        {
            m_IsGameAudioPaused = paused;
        }

        /// <summary>设置系统暂停状态，并暂停或恢复全部活动请求。</summary>
        /// <param name="paused">应用是否处于系统暂停状态。</param>
        public void SetSystemPaused(bool paused)
        {
            if (m_IsSystemPaused == paused)
            {
                return;
            }
            m_IsSystemPaused = paused;
            if (!m_IsReady)
            {
                return;
            }
            if (paused)
            {
                PauseAllForSystem();
            }
            else
            {
                ResumeAllFromSystem();
            }
        }

        /// <summary>为指定声音总线上的全部实例增加正式暂停原因并暂停播放。</summary>
        /// <param name="bus">需要暂停的声音总线。</param>
        /// <remarks>该方法由总线控制音量淡出完成后的回调调用。</remarks>
        public void PauseBusRequests(AudioBus bus)
        {
            foreach (var pair in m_Requests)
            {
                var request = pair.Value;
                if (request.m_Instance == null || request.m_Bus != bus)
                {
                    continue;
                }
                request.m_PauseReasons |= AudioPauseReason.Formal;
                if (request.m_State == AudioPlaybackState.Playing)
                {
                    PauseRequest(request, AudioPauseReason.Formal);
                }
            }
        }

        /// <summary>移除指定声音总线全部请求中的某一暂停原因，并恢复已经没有其他暂停原因的请求。</summary>
        /// <param name="bus">需要恢复的声音总线。</param>
        /// <param name="reason">需要移除的暂停原因。</param>
        public void ResumeBusRequests(AudioBus bus, AudioPauseReason reason)
        {
            m_RequestBuffer.Clear();
            foreach (var pair in m_Requests)
            {
                var request = pair.Value;
                if (request.m_Bus == bus && (request.m_PauseReasons & reason) != 0)
                {
                    m_RequestBuffer.Add(request);
                }
            }
            for (int i = 0; i < m_RequestBuffer.Count; i++)
            {
                var request = m_RequestBuffer[i];
                request.m_PauseReasons &= ~reason;
                if (request.m_PauseReasons == AudioPauseReason.None)
                {
                    ResumeRequest(request, 0f);
                }
            }
            m_RequestBuffer.Clear();
        }

        /// <summary>按指定范围停止请求或释放空闲声音实例。</summary>
        /// <param name="clearType">需要执行的清理范围。</param>
        public void Clear(AudioClearType clearType)
        {
            if (clearType == AudioClearType.All)
            {
                ClearAllAudioResources();
                return;
            }
            if (clearType == AudioClearType.Idle)
            {
                m_ResourceCache.ClearIdle();
                return;
            }
            m_RequestBuffer.Clear();
            foreach (var pair in m_Requests)
            {
                if (pair.Value.m_Lifetime == AudioLifetime.Transient)
                {
                    m_RequestBuffer.Add(pair.Value);
                }
            }
            for (int i = 0; i < m_RequestBuffer.Count; i++)
            {
                StopRequestImmediate(m_RequestBuffer[i], AudioPlaybackEndReason.Cleared);
            }
            m_RequestBuffer.Clear();
            m_ResourceCache.ReleaseEmptyEntries();
        }

        /// <summary>检查自然播放完成的实例，并推进声音资源缓存的定时释放。</summary>
        public void Update()
        {
            if (m_IsReady)
            {
                UpdatePlayingInstances();
                m_ResourceCache.Update();
            }
            FlushCallbacks();
        }

        /// <summary>设置调用方未指定缓存时间时使用的默认值。</summary>
        /// <param name="seconds">默认缓存秒数；负数会按 0 处理。</param>
        public void SetDefaultCacheSeconds(float seconds)
        {
            m_DefaultCacheSeconds = Mathf.Max(0f, seconds);
        }

        /// <summary>读取声音实例播放结束后的默认缓存时间。</summary>
        /// <returns>非负的默认缓存秒数。</returns>
        public float ReadDefaultCacheSeconds()
        {
            return m_DefaultCacheSeconds;
        }

        /// <summary>接收过渡控制器的开始停止通知，并标记请求不能再执行恢复或暂停。</summary>
        /// <param name="instance">正在停止淡出的声音实例。</param>
        internal void OnTransitionStopping(AudioSourceInstance instance)
        {
            if (instance != null && instance.m_Request != null)
            {
                if (!instance.m_Request.m_HasPendingEndReason)
                {
                    SetPendingEndReason(instance.m_Request, AudioPlaybackEndReason.Replaced);
                }
                instance.m_Request.m_IsStopping = true;
            }
        }

        /// <summary>接收过渡控制器的停止完成通知，并把请求和实例移入完成回收流程。</summary>
        /// <param name="instance">已经停止的声音实例。</param>
        internal void OnTransitionStopped(AudioSourceInstance instance)
        {
            if (instance != null && instance.m_Request != null)
            {
                var request = instance.m_Request;
                var reason = request.m_HasPendingEndReason ? request.m_PendingEndReason : AudioPlaybackEndReason.Replaced;
                CompleteRequestToIdle(request, reason);
            }
        }

        /// <summary>接收过渡控制器的暂停完成通知，并完成手动暂停状态迁移。</summary>
        /// <param name="instance">已经暂停的声音实例。</param>
        internal void OnTransitionPaused(AudioSourceInstance instance)
        {
            if (instance != null && instance.m_Request != null)
            {
                PauseRequest(instance.m_Request, AudioPauseReason.Manual);
            }
        }

        /// <summary>更新 <see cref="AudioReplayMode.KeepCurrent"/> 找到的现有请求，而不重新开始音频进度。</summary>
        /// <param name="current">需要继续保留的现有请求。</param>
        /// <param name="transitionMode">本次调用期望的过渡方式。</param>
        /// <param name="transitionSeconds">重新取得交叉过渡目标时使用的秒数。</param>
        /// <param name="duckOptions">更新后的声音压低参数。</param>
        private void KeepCurrentRequest(AudioPlaybackRequest current, AudioTransitionMode transitionMode, float transitionSeconds, AudioDuckOptions duckOptions, AudioPlaybackCallbacks callbacks)
        {
            current.m_Callbacks.Append(callbacks);
            current.m_DuckOptions = duckOptions;
            if (current.m_State == AudioPlaybackState.Playing && m_DuckingController != null)
            {
                m_DuckingController.Start(current.m_RequestId, duckOptions);
            }
            if (transitionMode != AudioTransitionMode.CrossFade)
            {
                return;
            }
            current.m_Sequence = m_NextSequence++;
            if (current.m_Instance == null)
            {
                return;
            }
            m_LatestCrossFadeRequests[(int)current.m_Bus] = current;
            if (current.m_Instance != null && current.m_HasValidRoute && m_TransitionController != null && m_TransitionController.KeepCrossFadeTarget(current.m_Instance, current.m_Bus, transitionSeconds))
            {
                current.m_IsStopping = false;
            }
        }

        /// <summary>创建、注册并按配置编号索引一个新的播放请求。</summary>
        /// <param name="id"><c>t_sound</c> 配置编号。</param>
        /// <param name="is3D">是否为三维空间声音。</param>
        /// <param name="position">声音发声点的世界坐标。</param>
        /// <param name="transitionMode">开始播放时采用的过渡方式。</param>
        /// <param name="transitionSeconds">请求指定的过渡秒数。</param>
        /// <param name="replayMode">相同配置编号的重播策略。</param>
        /// <param name="lifetime">流程清理时的保留级别。</param>
        /// <param name="cacheSeconds">调用方指定的实例缓存秒数。</param>
        /// <param name="duckOptions">该请求播放期间使用的总线压低参数。</param>
        /// <returns>已经加入活动请求集合的新请求。</returns>
        private AudioPlaybackRequest CreatePlaybackRequest(int id, bool is3D, Vector3 position, AudioTransitionMode transitionMode, float transitionSeconds, AudioReplayMode replayMode, AudioLifetime lifetime, float cacheSeconds, AudioDuckOptions duckOptions, AudioPlaybackCallbacks callbacks)
        {
            var request = new AudioPlaybackRequest
            {
                m_RequestId = m_NextRequestId++,
                m_LifecycleVersion = m_LifecycleVersion,
                m_ConfigId = id,
                m_Sequence = m_NextSequence++,
                m_State = AudioPlaybackState.WaitingForLoad,
                m_Lifetime = lifetime,
                m_ReplayMode = replayMode,
                m_TransitionMode = transitionMode,
                m_TransitionSeconds = transitionSeconds,
                m_CacheSeconds = ResolveCacheSeconds(cacheSeconds),
                m_Is3D = is3D,
                m_Position = position,
                m_DuckOptions = duckOptions,
                m_Callbacks = callbacks
            };
            m_Requests.Add(request.m_RequestId, request);
            List<AudioPlaybackRequest> requests;
            if (!m_RequestsByConfigId.TryGetValue(id, out requests))
            {
                requests = new List<AudioPlaybackRequest>();
                m_RequestsByConfigId.Add(id, requests);
            }
            requests.Add(request);
            return request;
        }

        /// <summary>处理声音资源就绪结果，取得实例并根据暂停和交叉过渡状态决定是否开始播放。</summary>
        /// <param name="request">等待该资源的播放请求。</param>
        /// <param name="entry">已经就绪的资源缓存记录。</param>
        private void OnCacheEntryReady(AudioPlaybackRequest request, AudioCacheEntry entry)
        {
            if (!IsRequestRegistered(request) || entry.m_State != AudioCacheState.Ready)
            {
                return;
            }
            request.m_Bus = entry.m_HasValidRoute ? entry.m_Bus : AudioBus.World;
            request.m_HasValidRoute = entry.m_HasValidRoute;
            if (m_IsGameAudioPaused && request.m_Bus != AudioBus.UI && request.m_Lifetime == AudioLifetime.Transient)
            {
                ReleaseRequest(request, AudioPlaybackEndReason.Rejected, "正式游戏暂停期间不启动临时声音");
                m_ResourceCache.TryReleaseEmptyEntry(entry);
                return;
            }
            if (request.m_TransitionMode == AudioTransitionMode.CrossFade)
            {
                var latest = m_LatestCrossFadeRequests[(int)request.m_Bus];
                if (latest != null && !IsRequestRegistered(latest))
                {
                    m_LatestCrossFadeRequests[(int)request.m_Bus] = null;
                    latest = null;
                }
                if (latest != null && latest != request && latest.m_Sequence > request.m_Sequence)
                {
                    ReleaseRequest(request, AudioPlaybackEndReason.Replaced, "交叉过渡请求已被更新请求替换");
                    m_ResourceCache.TryReleaseEmptyEntry(entry);
                    return;
                }
            }

            var instance = m_ResourceCache.AcquireInstance(entry);
            if (instance == null)
            {
                WarnLoadFailure(request.m_ConfigId, entry.m_Key.m_AbName, "声音实例创建失败");
                ReleaseRequest(request, AudioPlaybackEndReason.LoadFailed, "声音实例创建失败");
                m_ResourceCache.TryReleaseEmptyEntry(entry);
                return;
            }
            request.m_Instance = instance;
            instance.PrepareForPlay(request);
            if (!entry.m_HasValidRoute)
            {
                WarnInvalidRoute(request, entry);
            }
            if (instance.IsLooping() && request.m_Lifetime == AudioLifetime.Transient)
            {
                WarnTransientLoop(request);
            }
            if (m_IsSystemPaused)
            {
                request.m_PauseReasons |= AudioPauseReason.System;
            }
            if (m_IsGameAudioPaused && request.m_Bus != AudioBus.UI)
            {
                request.m_PauseReasons |= AudioPauseReason.Formal;
            }
            if (request.m_TransitionMode == AudioTransitionMode.CrossFade)
            {
                m_LatestCrossFadeRequests[(int)request.m_Bus] = request;
            }
            if (request.m_PauseReasons != AudioPauseReason.None)
            {
                request.m_State = AudioPlaybackState.Paused;
                return;
            }
            StartRequestPlayback(request);
        }

        /// <summary>处理声音资源请求失败结果并释放播放请求。</summary>
        /// <param name="request">未能取得资源的播放请求。</param>
        /// <param name="entry">关联的资源缓存记录。</param>
        /// <param name="reason">失败原因。</param>
        /// <param name="isCancellation">是否属于主动取消或生命周期失效。</param>
        private void OnCacheRequestFailed(AudioPlaybackRequest request, AudioCacheEntry entry, string reason, bool isCancellation)
        {
            if (!isCancellation)
            {
                WarnLoadFailure(request.m_ConfigId, entry.m_Key.m_AbName, reason);
            }
            ReleaseRequest(request, isCancellation ? AudioPlaybackEndReason.Cleared : AudioPlaybackEndReason.LoadFailed, reason);
        }

        /// <summary>在缓存销毁实例前移除其过渡记录和播放集合记录。</summary>
        /// <param name="instance">即将销毁的声音实例。</param>
        private void OnBeforeDestroyInstance(AudioSourceInstance instance)
        {
            if (m_TransitionController != null)
            {
                m_TransitionController.Remove(instance);
            }
            RemovePlayingInstance(instance);
        }

        /// <summary>将已经取得实例且没有暂停原因的请求切换到播放状态。</summary>
        /// <param name="request">需要开始播放的请求。</param>
        private void StartRequestPlayback(AudioPlaybackRequest request)
        {
            if (request == null || request.m_Instance == null || !IsRequestRegistered(request))
            {
                return;
            }
            if (request.m_PauseReasons != AudioPauseReason.None)
            {
                request.m_State = AudioPlaybackState.Paused;
                return;
            }
            if (request.m_TransitionMode == AudioTransitionMode.CrossFade && request != m_LatestCrossFadeRequests[(int)request.m_Bus])
            {
                CompleteRequestToIdle(request, AudioPlaybackEndReason.Replaced);
                return;
            }
            request.m_State = AudioPlaybackState.Playing;
            request.m_IsStopping = false;
            bool handled = request.m_TransitionMode == AudioTransitionMode.CrossFade && request.m_HasValidRoute && m_TransitionController != null && m_TransitionController.StartCrossFade(request.m_Instance, request.m_Bus, request.m_TransitionSeconds);
            if (!handled)
            {
                request.m_Instance.PlaySource();
            }
            AddPlayingInstance(request.m_Instance);
            if (m_DuckingController != null)
            {
                m_DuckingController.Start(request.m_RequestId, request.m_DuckOptions);
            }
        }

        /// <summary>不执行淡出，立即停止并释放指定请求。</summary>
        /// <param name="request">需要立即停止的播放请求。</param>
        private void StopRequestImmediate(AudioPlaybackRequest request, AudioPlaybackEndReason reason)
        {
            if (!IsRequestRegistered(request))
            {
                return;
            }
            if (request.m_Instance == null)
            {
                m_ResourceCache.RemovePendingRequest(request);
                var entry = request.m_CacheEntry;
                ReleaseRequest(request, reason, null);
                m_ResourceCache.TryReleaseEmptyEntry(entry);
                return;
            }
            CompleteRequestToIdle(request, reason);
        }

        /// <summary>结束播放请求，将实例移入空闲缓存并从活动请求索引释放请求。</summary>
        /// <param name="request">已经完成或被立即停止的播放请求。</param>
        private void CompleteRequestToIdle(AudioPlaybackRequest request, AudioPlaybackEndReason reason)
        {
            if (!IsRequestRegistered(request))
            {
                return;
            }
            var instance = request.m_Instance;
            var entry = instance != null ? instance.m_CacheEntry : request.m_CacheEntry;
            if (instance != null)
            {
                if (m_TransitionController != null)
                {
                    m_TransitionController.Remove(instance);
                }
                RemovePlayingInstance(instance);
                m_ResourceCache.MoveToIdle(instance, request.m_CacheSeconds);
            }
            request.m_State = AudioPlaybackState.Idle;
            ReleaseRequest(request, reason, null);
            m_ResourceCache.TrimIdleInstances(entry, instance);
            m_ResourceCache.TryReleaseEmptyEntry(entry);
        }

        /// <summary>停止声音压低并从全部活动请求索引中释放指定请求。</summary>
        /// <param name="request">需要释放的播放请求。</param>
        private void ReleaseRequest(AudioPlaybackRequest request, AudioPlaybackEndReason reason, string message)
        {
            if (!IsRequestRegistered(request))
            {
                return;
            }
            if (m_DuckingController != null)
            {
                m_DuckingController.Stop(request.m_RequestId);
            }
            request.m_State = AudioPlaybackState.Released;
            m_Requests.Remove(request.m_RequestId);
            List<AudioPlaybackRequest> configRequests;
            if (m_RequestsByConfigId.TryGetValue(request.m_ConfigId, out configRequests))
            {
                configRequests.Remove(request);
                if (configRequests.Count == 0)
                {
                    m_RequestsByConfigId.Remove(request.m_ConfigId);
                }
            }
            if (request.m_TransitionMode == AudioTransitionMode.CrossFade && m_LatestCrossFadeRequests[(int)request.m_Bus] == request)
            {
                m_LatestCrossFadeRequests[(int)request.m_Bus] = null;
            }
            request.m_Instance = null;
            QueueCallback(new AudioHandle(request.m_RequestId, request.m_LifecycleVersion), request.m_ConfigId, reason, message, request.m_Callbacks);
            request.m_Callbacks = default(AudioPlaybackCallbacks);
        }

        /// <summary>根据句柄和当前生命周期查找仍然活动的播放请求。</summary>
        /// <param name="handle">需要解析的播放句柄。</param>
        /// <param name="request">成功时返回对应的活动请求。</param>
        /// <returns>句柄属于当前生命周期且请求尚未完成时返回 <see langword="true"/>。</returns>
        private bool TryGetRequest(AudioHandle handle, out AudioPlaybackRequest request)
        {
            if (!handle.IsCreated || handle.ReadLifecycleVersion() != m_LifecycleVersion)
            {
                request = null;
                return false;
            }
            return m_Requests.TryGetValue(handle.ReadRequestId(), out request) && request.m_LifecycleVersion == handle.ReadLifecycleVersion() && request.m_State != AudioPlaybackState.Idle && request.m_State != AudioPlaybackState.Released;
        }

        /// <summary>判断播放请求引用是否仍是当前生命周期活动集合中的原对象。</summary>
        /// <param name="request">需要检查的播放请求。</param>
        /// <returns>请求仍然注册时返回 <see langword="true"/>。</returns>
        private bool IsRequestRegistered(AudioPlaybackRequest request)
        {
            AudioPlaybackRequest registered;
            return request != null && request.m_LifecycleVersion == m_LifecycleVersion && m_Requests.TryGetValue(request.m_RequestId, out registered) && registered == request;
        }

        /// <summary>读取指定声音配置编号最新创建或重新激活的请求。</summary>
        /// <param name="configId"><c>t_sound</c> 配置编号。</param>
        /// <param name="request">成功时返回顺序号最大的活动请求。</param>
        /// <returns>至少存在一个活动请求时返回 <see langword="true"/>。</returns>
        private bool TryReadLatestRequest(int configId, out AudioPlaybackRequest request)
        {
            List<AudioPlaybackRequest> requests;
            if (!m_RequestsByConfigId.TryGetValue(configId, out requests))
            {
                request = null;
                return false;
            }
            request = null;
            for (int i = 0; i < requests.Count; i++)
            {
                var current = requests[i];
                if (IsRequestRegistered(current) && (request == null || current.m_Sequence > request.m_Sequence))
                {
                    request = current;
                }
            }
            return request != null;
        }

        /// <summary>立即停止指定声音配置编号的全部活动请求。</summary>
        /// <param name="configId"><c>t_sound</c> 配置编号。</param>
        private void StopAllRequestsByConfigId(int configId, AudioPlaybackEndReason reason)
        {
            List<AudioPlaybackRequest> requests;
            if (!m_RequestsByConfigId.TryGetValue(configId, out requests))
            {
                return;
            }
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                StopRequestImmediate(requests[i], reason);
            }
        }

        /// <summary>按声音配置的最小播放间隔判断本次请求是否允许创建。</summary>
        /// <param name="config"><c>t_sound</c> 配置数据。</param>
        /// <returns>没有限制或距离上次允许播放已经达到配置毫秒数时返回 <see langword="true"/>。</returns>
        private bool CheckMinimumPlayInterval(t_soundBean config)
        {
            if (config.t_allow_multi <= 0)
            {
                return true;
            }
            float now = Time.realtimeSinceStartup;
            float lastPlayTime;
            if (m_AudioLastPlayTimes.TryGetValue(config.t_id, out lastPlayTime) && now - lastPlayTime < config.t_allow_multi / 1000f)
            {
                return false;
            }
            m_AudioLastPlayTimes[config.t_id] = now;
            return true;
        }

        /// <summary>为请求增加暂停原因，并暂停已经开始的实例。</summary>
        /// <param name="request">需要暂停的播放请求。</param>
        /// <param name="reason">本次增加的暂停原因。</param>
        private void PauseRequest(AudioPlaybackRequest request, AudioPauseReason reason)
        {
            if (!IsRequestRegistered(request))
            {
                return;
            }
            request.m_PauseReasons |= reason;
            if (m_DuckingController != null)
            {
                m_DuckingController.Stop(request.m_RequestId);
            }
            if (request.m_Instance != null && request.m_Instance.m_HasStarted)
            {
                request.m_Instance.PauseSource();
                RemovePlayingInstance(request.m_Instance);
            }
            request.m_State = AudioPlaybackState.Paused;
        }

        /// <summary>在请求没有任何暂停原因后，根据当前实例状态开始或恢复播放。</summary>
        /// <param name="request">需要恢复的播放请求。</param>
        /// <param name="transitionSeconds">恢复淡入秒数。</param>
        private void ResumeRequest(AudioPlaybackRequest request, float transitionSeconds)
        {
            if (!IsRequestRegistered(request) || request.m_PauseReasons != AudioPauseReason.None)
            {
                return;
            }
            if (request.m_TransitionMode == AudioTransitionMode.CrossFade && request.m_Instance != null && request != m_LatestCrossFadeRequests[(int)request.m_Bus])
            {
                CompleteRequestToIdle(request, AudioPlaybackEndReason.Replaced);
                return;
            }
            if (request.m_Instance == null)
            {
                request.m_State = AudioPlaybackState.WaitingForLoad;
                return;
            }
            if (!request.m_Instance.m_HasStarted)
            {
                StartRequestPlayback(request);
                return;
            }
            request.m_State = AudioPlaybackState.Playing;
            bool handled = m_TransitionController != null && m_TransitionController.Resume(request.m_Instance, request.m_Bus, transitionSeconds);
            if (!handled)
            {
                request.m_Instance.ResumeSource();
            }
            AddPlayingInstance(request.m_Instance);
            if (m_DuckingController != null)
            {
                m_DuckingController.Start(request.m_RequestId, request.m_DuckOptions);
            }
        }

        /// <summary>为全部活动请求增加系统暂停原因，并暂停正在播放的实例。</summary>
        private void PauseAllForSystem()
        {
            foreach (var pair in m_Requests)
            {
                var request = pair.Value;
                request.m_PauseReasons |= AudioPauseReason.System;
                if (request.m_Instance != null && request.m_State == AudioPlaybackState.Playing)
                {
                    if (m_DuckingController != null)
                    {
                        m_DuckingController.Stop(request.m_RequestId);
                    }
                    request.m_Instance.PauseSource();
                    RemovePlayingInstance(request.m_Instance);
                    request.m_State = AudioPlaybackState.Paused;
                }
            }
        }

        /// <summary>移除全部请求的系统暂停原因，并恢复不再受其他原因暂停的请求。</summary>
        private void ResumeAllFromSystem()
        {
            m_RequestBuffer.Clear();
            foreach (var pair in m_Requests)
            {
                if ((pair.Value.m_PauseReasons & AudioPauseReason.System) != 0)
                {
                    m_RequestBuffer.Add(pair.Value);
                }
            }
            for (int i = 0; i < m_RequestBuffer.Count; i++)
            {
                var request = m_RequestBuffer[i];
                request.m_PauseReasons &= ~AudioPauseReason.System;
                if (m_IsGameAudioPaused && request.m_Instance != null && request.m_Bus != AudioBus.UI)
                {
                    request.m_PauseReasons |= AudioPauseReason.Formal;
                }
                if (request.m_PauseReasons == AudioPauseReason.None)
                {
                    ResumeRequest(request, 0f);
                }
            }
            m_RequestBuffer.Clear();
        }

        /// <summary>将声音实例加入自然播放完成检查集合，并避免重复添加。</summary>
        /// <param name="instance">已经开始播放的声音实例。</param>
        private void AddPlayingInstance(AudioSourceInstance instance)
        {
            if (instance != null && !m_PlayingInstances.Contains(instance))
            {
                m_PlayingInstances.Add(instance);
            }
        }

        /// <summary>从自然播放完成检查集合移除声音实例。</summary>
        /// <param name="instance">已经停止、暂停、回池或即将销毁的声音实例。</param>
        private void RemovePlayingInstance(AudioSourceInstance instance)
        {
            if (instance != null)
            {
                m_PlayingInstances.Remove(instance);
            }
        }

        /// <summary>检查非循环实例是否已经自然播放完成，并将完成请求移入空闲缓存。</summary>
        private void UpdatePlayingInstances()
        {
            for (int i = m_PlayingInstances.Count - 1; i >= 0; i--)
            {
                var instance = m_PlayingInstances[i];
                var request = instance != null ? instance.m_Request : null;
                if (instance == null || instance.IsDestroyed() || request == null || request.m_State != AudioPlaybackState.Playing)
                {
                    m_PlayingInstances.RemoveAt(i);
                    continue;
                }
                if (!instance.IsLooping() && instance.m_PlayStartFrame != Time.frameCount && !instance.IsPlayingSource() && !instance.IsSourcePaused())
                {
                    CompleteRequestToIdle(request, AudioPlaybackEndReason.Completed);
                }
            }
        }

        /// <summary>停止并释放全部播放请求、过渡记录、资源缓存和播放限次状态。</summary>
        private void ClearAllAudioResources()
        {
            if (m_TransitionController != null)
            {
                m_TransitionController.Clear();
            }
            m_RequestBuffer.Clear();
            foreach (var pair in m_Requests)
            {
                m_RequestBuffer.Add(pair.Value);
            }
            for (int i = 0; i < m_RequestBuffer.Count; i++)
            {
                StopRequestImmediate(m_RequestBuffer[i], AudioPlaybackEndReason.Cleared);
            }
            m_RequestBuffer.Clear();
            m_Requests.Clear();
            m_RequestsByConfigId.Clear();
            m_PlayingInstances.Clear();
            Array.Clear(m_LatestCrossFadeRequests, 0, m_LatestCrossFadeRequests.Length);
            m_ResourceCache.ClearAll();
            m_AudioLastPlayTimes.Clear();
            m_RequestBuffer.Clear();
            m_IsGameAudioPaused = false;
        }

        /// <summary>设置请求完成停止过渡后需要上报的原因。</summary>
        private void SetPendingEndReason(AudioPlaybackRequest request, AudioPlaybackEndReason reason)
        {
            request.m_PendingEndReason = reason;
            request.m_HasPendingEndReason = true;
        }

        /// <summary>选择与结束原因对应的回调，并加入延迟派发集合。</summary>
        private void QueueCallback(AudioHandle handle, int configId, AudioPlaybackEndReason reason, string message, AudioPlaybackCallbacks callbacks)
        {
            var callback = callbacks.ReadCallback(reason);
            if (callback == null)
            {
                return;
            }
            m_CallbackInvocations.Add(new CallbackInvocation
            {
                m_Callback = callback,
                m_Result = new AudioPlaybackResult(handle, configId, reason, message)
            });
        }

        /// <summary>在内部集合操作结束后派发业务回调，单个回调异常不会中断其他回调。</summary>
        private void FlushCallbacks()
        {
            if (m_IsFlushingCallbacks || m_CallbackInvocations.Count == 0)
            {
                return;
            }
            m_IsFlushingCallbacks = true;
            try
            {
                m_CallbackInvocationBuffer.AddRange(m_CallbackInvocations);
                m_CallbackInvocations.Clear();
                for (int i = 0; i < m_CallbackInvocationBuffer.Count; i++)
                {
                    var invocation = m_CallbackInvocationBuffer[i];
                    var callbacks = invocation.m_Callback.GetInvocationList();
                    for (int callbackIndex = 0; callbackIndex < callbacks.Length; callbackIndex++)
                    {
                        try
                        {
                            ((Action<AudioPlaybackResult>)callbacks[callbackIndex])(invocation.m_Result);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError("AudioManager 播放结束回调执行异常，配置编号=" + invocation.m_Result.m_ConfigId + "，结束原因=" + invocation.m_Result.m_Reason);
                            Debug.LogException(exception);
                        }
                    }
                }
            }
            finally
            {
                m_CallbackInvocationBuffer.Clear();
                m_IsFlushingCallbacks = false;
            }
        }

        /// <summary>将调用方指定的过渡时间解析为最终有效秒数。</summary>
        /// <param name="bus">用于读取默认值的声音总线。</param>
        /// <param name="requestedSeconds">调用方指定值；小于 0 表示使用总线默认值。</param>
        /// <returns>非负的最终过渡秒数。</returns>
        private float ResolveTransitionSeconds(AudioBus bus, float requestedSeconds)
        {
            return m_BusController != null ? m_BusController.ResolveTransitionSeconds(bus, requestedSeconds) : Mathf.Max(0f, requestedSeconds);
        }

        /// <summary>将调用方指定的缓存时间解析为最终有效秒数。</summary>
        /// <param name="requestedSeconds">调用方指定值；小于 0 表示使用管理器默认值。</param>
        /// <returns>非负的最终缓存秒数。</returns>
        private float ResolveCacheSeconds(float requestedSeconds)
        {
            return requestedSeconds < 0f ? m_DefaultCacheSeconds : Mathf.Max(0f, requestedSeconds);
        }

        /// <summary>按声音配置编号限次打印资源加载或实例创建失败警告。</summary>
        /// <param name="configId"><c>t_sound</c> 配置编号。</param>
        /// <param name="resourcePath">关联的资源路径。</param>
        /// <param name="reason">失败原因。</param>
        private void WarnLoadFailure(int configId, string resourcePath, string reason)
        {
            if (!m_LoadWarningConfigIds.Add(configId))
            {
                return;
            }
            Debug.LogWarning("AudioManager 声音加载失败，配置编号=" + configId + "，资源=" + resourcePath + "，原因=" + reason);
        }

        /// <summary>按声音配置编号限次警告预制件没有连接到规范混音分组。</summary>
        /// <param name="request">正在播放的请求。</param>
        /// <param name="entry">包含实际输出分组信息的资源缓存记录。</param>
        private void WarnInvalidRoute(AudioPlaybackRequest request, AudioCacheEntry entry)
        {
            if (!m_RouteWarningConfigIds.Add(request.m_ConfigId))
            {
                return;
            }
            Debug.LogWarning("AudioManager 声音没有路由到 main_mixer 的 Normal 分组，配置编号=" + request.m_ConfigId + "，资源=" + entry.m_Key.m_AbName + "。本次继续使用预制件原输出，请把预制件设置到 MusicNormal、UINormal、WorldNormal 或 VoiceNormal。");
        }

        /// <summary>按声音配置编号限次警告循环声音使用了临时生命周期。</summary>
        /// <param name="request">配置不合理的循环播放请求。</param>
        private void WarnTransientLoop(AudioPlaybackRequest request)
        {
            if (!m_LoopLifetimeWarningConfigIds.Add(request.m_ConfigId))
            {
                return;
            }
            Debug.LogWarning("AudioManager 循环声音使用了 Transient 生命周期，配置编号=" + request.m_ConfigId + "。需要跨流程保留时请显式使用 Persistent 或 Manual。");
        }
    }
}
