using LCL;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    /// <summary>
    /// 管理声音预制件的异步加载、加载请求合并、运行时实例复用和空闲资源释放。
    /// </summary>
    /// <remarks>
    /// <para>同一 <see cref="AudioResourceKey"/> 同时收到多个播放请求时只发起一次资源加载，其余请求挂在同一缓存记录等待结果。</para>
    /// <para>播放结束的实例先进入空闲状态，在缓存时间到期后销毁；同一资源再次播放时优先复用空闲实例。</para>
    /// <para>异步回调必须同时通过生命周期版本和活动缓存记录校验，防止反初始化或快速重新初始化后使用过期结果。</para>
    /// </remarks>
    internal sealed class AudioResourceCache
    {
        private const float m_LoadFailureCooldownSeconds = 5f;
        private const float m_IdleScanIntervalSeconds = 1f;

        private readonly Dictionary<AudioResourceKey, AudioCacheEntry> m_EntriesByKey = new Dictionary<AudioResourceKey, AudioCacheEntry>();
        private readonly Dictionary<long, AudioCacheEntry> m_LoadEntriesByRequestId = new Dictionary<long, AudioCacheEntry>();
        private readonly Dictionary<AudioResourceKey, float> m_FailedUntilTimes = new Dictionary<AudioResourceKey, float>();
        private readonly List<AudioCacheEntry> m_Entries = new List<AudioCacheEntry>();
        private readonly List<AudioPlaybackRequest> m_RequestBuffer = new List<AudioPlaybackRequest>();
        private readonly Func<AudioPlaybackRequest, bool> m_IsRequestRegistered;
        private readonly Action<AudioPlaybackRequest, AudioCacheEntry> m_OnEntryReady;
        private readonly Action<AudioPlaybackRequest, AudioCacheEntry, string, bool> m_OnRequestFailed;
        private readonly Action<AudioSourceInstance> m_OnBeforeDestroyInstance;

        private Transform m_SoundRoot;
        private AudioTransitionController m_TransitionController;
        private int m_LifecycleVersion;
        private float m_NextIdleScanTime;
        private bool m_IsReady;

        /// <summary>
        /// 创建声音资源缓存并绑定播放控制器回调。
        /// </summary>
        /// <param name="isRequestRegistered">判断异步结果对应的播放请求是否仍然有效的回调。</param>
        /// <param name="onEntryReady">缓存资源就绪后通知播放控制器的回调。</param>
        /// <param name="onRequestFailed">资源加载失败、取消或结果失效时通知播放控制器的回调。</param>
        /// <param name="onBeforeDestroyInstance">销毁运行时实例前清理过渡和播放集合的回调。</param>
        public AudioResourceCache(Func<AudioPlaybackRequest, bool> isRequestRegistered, Action<AudioPlaybackRequest, AudioCacheEntry> onEntryReady, Action<AudioPlaybackRequest, AudioCacheEntry, string, bool> onRequestFailed, Action<AudioSourceInstance> onBeforeDestroyInstance)
        {
            m_IsRequestRegistered = isRequestRegistered;
            m_OnEntryReady = onEntryReady;
            m_OnRequestFailed = onRequestFailed;
            m_OnBeforeDestroyInstance = onBeforeDestroyInstance;
        }

        /// <summary>初始化声音资源缓存的运行时依赖。</summary>
        /// <param name="soundRoot">所有声音实例的父节点。</param>
        /// <param name="transitionController">用于识别混音输出总线和清理实例过渡记录的控制器。</param>
        /// <param name="lifecycleVersion">当前声音管理器生命周期版本。</param>
        public void Initialize(Transform soundRoot, AudioTransitionController transitionController, int lifecycleVersion)
        {
            m_SoundRoot = soundRoot;
            m_TransitionController = transitionController;
            m_LifecycleVersion = lifecycleVersion;
            m_NextIdleScanTime = 0f;
            m_IsReady = true;
        }

        /// <summary>取消或释放全部资源、实例和等待请求，并清除运行时依赖。</summary>
        public void UnInit()
        {
            ClearAll();
            m_IsReady = false;
            m_SoundRoot = null;
            m_TransitionController = null;
            m_LifecycleVersion = 0;
        }

        /// <summary>判断指定资源是否仍处于加载失败后的短暂冷却期。</summary>
        /// <param name="key">需要检查的声音资源键。</param>
        /// <returns>冷却尚未结束时返回 <see langword="true"/>；冷却已结束或没有失败记录时返回 <see langword="false"/>。</returns>
        public bool IsInFailureCooldown(AudioResourceKey key)
        {
            float failedUntil;
            if (!m_FailedUntilTimes.TryGetValue(key, out failedUntil))
            {
                return false;
            }
            if (Time.realtimeSinceStartup < failedUntil)
            {
                return true;
            }
            m_FailedUntilTimes.Remove(key);
            return false;
        }

        /// <summary>
        /// 请求一份声音资源，并把播放请求连接到已有或新建的缓存记录。
        /// </summary>
        /// <param name="request">等待取得资源和运行时实例的播放请求。</param>
        /// <param name="key">唯一标识声音预制件的资源键。</param>
        /// <remarks>资源已经就绪时会同步通知播放控制器；正在加载时只追加等待请求。</remarks>
        public void Request(AudioPlaybackRequest request, AudioResourceKey key)
        {
            AudioCacheEntry entry;
            if (!m_EntriesByKey.TryGetValue(key, out entry))
            {
                entry = CreateEntry(key);
                AttachPendingRequest(entry, request);
                StartLoad(entry);
                return;
            }
            request.m_CacheEntry = entry;
            if (entry.m_State == AudioCacheState.Ready)
            {
                NotifyEntryReady(request, entry);
            }
            else if (entry.m_State == AudioCacheState.Loading)
            {
                entry.m_PendingRequests.Add(request);
            }
            else
            {
                NotifyRequestFailed(request, entry, "声音缓存已经释放", true);
            }
        }

        /// <summary>从指定缓存记录取得一个空闲实例，或按声音预制件创建新实例。</summary>
        /// <param name="entry">已经就绪的声音资源缓存记录。</param>
        /// <returns>可用于播放的实例；预制件、父节点或 <see cref="AudioSource"/> 无效时返回 <see langword="null"/>。</returns>
        public AudioSourceInstance AcquireInstance(AudioCacheEntry entry)
        {
            for (int i = 0; i < entry.m_Instances.Count; i++)
            {
                var instance = entry.m_Instances[i];
                if (!instance.IsDestroyed() && instance.m_Request == null)
                {
                    return instance;
                }
            }
            if (entry.m_SourcePrefab == null || m_SoundRoot == null)
            {
                return null;
            }
            var gameObject = GameObject.Instantiate(entry.m_SourcePrefab, m_SoundRoot, false);
            var audioSource = gameObject != null ? gameObject.GetComponent<AudioSource>() : null;
            if (audioSource == null)
            {
                if (gameObject != null)
                {
                    GameObject.Destroy(gameObject);
                }
                return null;
            }
            var createdInstance = new AudioSourceInstance(audioSource, entry);
            entry.m_Instances.Add(createdInstance);
            return createdInstance;
        }

        /// <summary>将播放完成的实例转为空闲缓存状态。</summary>
        /// <param name="instance">需要解除播放请求并保留的实例。</param>
        /// <param name="cacheSeconds">允许该实例保持空闲的秒数。</param>
        public void MoveToIdle(AudioSourceInstance instance, float cacheSeconds)
        {
            if (instance == null)
            {
                return;
            }
            instance.MoveToIdle(cacheSeconds);
        }

        /// <summary>从资源缓存的等待队列移除尚未取得实例的播放请求。</summary>
        /// <param name="request">已被停止或释放的播放请求。</param>
        public void RemovePendingRequest(AudioPlaybackRequest request)
        {
            var entry = request != null ? request.m_CacheEntry : null;
            if (entry != null)
            {
                entry.m_PendingRequests.Remove(request);
            }
        }

        /// <summary>扫描并释放没有等待请求、没有实例且不在加载中的缓存记录。</summary>
        public void ReleaseEmptyEntries()
        {
            for (int i = m_Entries.Count - 1; i >= 0; i--)
            {
                TryReleaseEmptyEntry(m_Entries[i]);
            }
        }

        /// <summary>在指定缓存记录已经完全空闲时释放其资源请求。</summary>
        /// <param name="entry">需要检查的缓存记录。</param>
        public void TryReleaseEmptyEntry(AudioCacheEntry entry)
        {
            if (entry == null || entry.m_State == AudioCacheState.Released)
            {
                return;
            }
            if (entry.m_PendingRequests.Count == 0 && entry.m_Instances.Count == 0 && entry.m_State != AudioCacheState.Loading)
            {
                ReleaseEntry(entry, false);
            }
        }

        /// <summary>按固定扫描间隔销毁缓存时间到期的空闲实例，并释放空缓存记录。</summary>
        public void Update()
        {
            float now = Time.realtimeSinceStartup;
            if (now < m_NextIdleScanTime)
            {
                return;
            }
            m_NextIdleScanTime = now + m_IdleScanIntervalSeconds;
            for (int entryIndex = m_Entries.Count - 1; entryIndex >= 0; entryIndex--)
            {
                var entry = m_Entries[entryIndex];
                for (int instanceIndex = entry.m_Instances.Count - 1; instanceIndex >= 0; instanceIndex--)
                {
                    var instance = entry.m_Instances[instanceIndex];
                    if (instance.m_Request != null)
                    {
                        continue;
                    }
                    if (instance.m_IdleCacheSeconds <= 0f || now - instance.m_IdleSince >= instance.m_IdleCacheSeconds)
                    {
                        DestroyInstance(entry, instance);
                    }
                }
                TryReleaseEmptyEntry(entry);
            }
        }

        /// <summary>立即销毁已经达到释放条件的空闲实例，但保留仍在缓存时间内的实例。</summary>
        public void ClearIdle()
        {
            float now = Time.realtimeSinceStartup;
            for (int entryIndex = m_Entries.Count - 1; entryIndex >= 0; entryIndex--)
            {
                var entry = m_Entries[entryIndex];
                for (int instanceIndex = entry.m_Instances.Count - 1; instanceIndex >= 0; instanceIndex--)
                {
                    var instance = entry.m_Instances[instanceIndex];
                    if (instance.m_Request == null && (instance.m_IdleCacheSeconds <= 0f || now - instance.m_IdleSince >= instance.m_IdleCacheSeconds))
                    {
                        DestroyInstance(entry, instance);
                    }
                }
                TryReleaseEmptyEntry(entry);
            }
        }

        /// <summary>取消加载并释放全部声音资源、实例、等待请求和失败冷却记录。</summary>
        public void ClearAll()
        {
            for (int i = m_Entries.Count - 1; i >= 0; i--)
            {
                ReleaseEntry(m_Entries[i], true);
            }
            m_Entries.Clear();
            m_EntriesByKey.Clear();
            m_LoadEntriesByRequestId.Clear();
            m_FailedUntilTimes.Clear();
            m_RequestBuffer.Clear();
            m_NextIdleScanTime = 0f;
        }

        /// <summary>创建并注册一份处于加载状态的新资源缓存记录。</summary>
        /// <param name="key">唯一标识声音预制件的资源键。</param>
        /// <returns>已经加入缓存索引的新记录。</returns>
        private AudioCacheEntry CreateEntry(AudioResourceKey key)
        {
            var entry = new AudioCacheEntry
            {
                m_Key = key,
                m_State = AudioCacheState.Loading,
                m_LifecycleVersion = m_LifecycleVersion
            };
            m_EntriesByKey.Add(key, entry);
            m_Entries.Add(entry);
            return entry;
        }

        /// <summary>将播放请求连接到资源缓存记录的等待队列。</summary>
        /// <param name="entry">正在加载资源的缓存记录。</param>
        /// <param name="request">等待资源的播放请求。</param>
        private void AttachPendingRequest(AudioCacheEntry entry, AudioPlaybackRequest request)
        {
            request.m_CacheEntry = entry;
            entry.m_PendingRequests.Add(request);
        }

        /// <summary>为指定缓存记录发起声音预制件异步加载。</summary>
        /// <param name="entry">需要开始加载的缓存记录。</param>
        private void StartLoad(AudioCacheEntry entry)
        {
            entry.m_ResourceRequest = UIRes.LoadPrefabAsync(entry.m_Key.m_AssetType, entry.m_Key.m_AbName, entry.m_Key.m_AssetName, OnResourceLoaded, OnResourceLoadFailed, entry);
            if (entry.m_ResourceRequest == null)
            {
                FailEntry(entry, "声音资源加载请求创建失败", false);
                return;
            }
            m_LoadEntriesByRequestId[entry.m_ResourceRequest.LoadIndex] = entry;
        }

        /// <summary>处理声音预制件加载成功结果，并唤醒仍然有效的等待请求。</summary>
        /// <param name="data">资源系统返回的声音预制件数据。</param>
        /// <param name="userData">发起加载时传入的缓存记录。</param>
        private void OnResourceLoaded(ResData data, object userData)
        {
            var entry = userData as AudioCacheEntry;
            if (entry == null)
            {
                return;
            }
            if (entry.m_ResourceRequest != null)
            {
                m_LoadEntriesByRequestId.Remove(entry.m_ResourceRequest.LoadIndex);
            }
            if (!m_IsReady || entry.m_State != AudioCacheState.Loading || entry.m_LifecycleVersion != m_LifecycleVersion || !IsActiveEntry(entry))
            {
                FailEntry(entry, "声音资源加载结果已失效", true);
                return;
            }
            var sourcePrefab = data != null ? data.m_Obj as GameObject : null;
            var sourceAudio = sourcePrefab != null ? sourcePrefab.GetComponent<AudioSource>() : null;
            if (sourcePrefab == null || sourceAudio == null)
            {
                FailEntry(entry, "声音预制件或 AudioSource 无效", false);
                return;
            }
            entry.m_SourcePrefab = sourcePrefab;
            entry.m_State = AudioCacheState.Ready;
            entry.m_HasValidRoute = m_TransitionController != null && m_TransitionController.TryResolveBus(sourceAudio.outputAudioMixerGroup, out entry.m_Bus);

            m_RequestBuffer.Clear();
            m_RequestBuffer.AddRange(entry.m_PendingRequests);
            entry.m_PendingRequests.Clear();
            for (int i = 0; i < m_RequestBuffer.Count; i++)
            {
                if (IsRequestRegistered(m_RequestBuffer[i]))
                {
                    NotifyEntryReady(m_RequestBuffer[i], entry);
                }
            }
            m_RequestBuffer.Clear();
            TryReleaseEmptyEntry(entry);
        }

        /// <summary>处理声音预制件加载失败或取消结果。</summary>
        /// <param name="requestId">资源系统返回的加载请求编号。</param>
        /// <param name="result">资源加载结果。</param>
        private void OnResourceLoadFailed(long requestId, ABRequestResult result)
        {
            AudioCacheEntry entry;
            if (!m_LoadEntriesByRequestId.TryGetValue(requestId, out entry))
            {
                return;
            }
            m_LoadEntriesByRequestId.Remove(requestId);
            if (entry.m_State != AudioCacheState.Loading || entry.m_LifecycleVersion != m_LifecycleVersion)
            {
                return;
            }
            bool isCancellation = result == ABRequestResult.Cancel;
            FailEntry(entry, isCancellation ? "声音资源加载已取消" : "声音资源加载失败", isCancellation);
        }

        /// <summary>完成缓存记录中的全部等待请求并释放失败的缓存记录。</summary>
        /// <param name="entry">加载失败或已经失效的缓存记录。</param>
        /// <param name="reason">传递给播放控制器的失败原因。</param>
        /// <param name="isCancellation">是否属于主动取消或生命周期失效；非取消失败会进入冷却期。</param>
        private void FailEntry(AudioCacheEntry entry, string reason, bool isCancellation)
        {
            if (entry == null || entry.m_State == AudioCacheState.Released)
            {
                return;
            }
            if (!isCancellation)
            {
                m_FailedUntilTimes[entry.m_Key] = Time.realtimeSinceStartup + m_LoadFailureCooldownSeconds;
            }
            while (entry.m_PendingRequests.Count > 0)
            {
                int lastIndex = entry.m_PendingRequests.Count - 1;
                var request = entry.m_PendingRequests[lastIndex];
                entry.m_PendingRequests.RemoveAt(lastIndex);
                NotifyRequestFailed(request, entry, reason, isCancellation);
            }
            ReleaseEntry(entry, true);
        }

        /// <summary>
        /// 收缩指定资源的空闲实例数量，只保留一个可复用实例或按缓存策略立即销毁。
        /// </summary>
        /// <param name="entry">需要整理实例集合的缓存记录。</param>
        /// <param name="preferredInstance">刚完成播放、优先考虑保留的实例。</param>
        public void TrimIdleInstances(AudioCacheEntry entry, AudioSourceInstance preferredInstance)
        {
            if (entry == null)
            {
                return;
            }
            if (preferredInstance != null && preferredInstance.m_IdleCacheSeconds <= 0f)
            {
                DestroyInstance(entry, preferredInstance);
                return;
            }
            bool keptOne = preferredInstance != null && preferredInstance.m_Request == null && entry.m_Instances.Contains(preferredInstance);
            for (int i = entry.m_Instances.Count - 1; i >= 0; i--)
            {
                var instance = entry.m_Instances[i];
                if (instance.m_Request != null || instance == preferredInstance)
                {
                    continue;
                }
                if (!keptOne)
                {
                    keptOne = true;
                    continue;
                }
                DestroyInstance(entry, instance);
            }
        }

        /// <summary>从缓存记录移除并销毁一个运行时声音实例。</summary>
        /// <param name="entry">实例所属的缓存记录。</param>
        /// <param name="instance">需要销毁的实例。</param>
        private void DestroyInstance(AudioCacheEntry entry, AudioSourceInstance instance)
        {
            if (instance == null)
            {
                return;
            }
            if (m_OnBeforeDestroyInstance != null)
            {
                m_OnBeforeDestroyInstance(instance);
            }
            entry.m_Instances.Remove(instance);
            instance.Destroy();
        }

        /// <summary>取消资源请求并从全部索引释放指定缓存记录。</summary>
        /// <param name="entry">需要释放的缓存记录。</param>
        /// <param name="destroyInstances">是否同时销毁记录中的全部运行时实例。</param>
        private void ReleaseEntry(AudioCacheEntry entry, bool destroyInstances)
        {
            if (entry == null || entry.m_State == AudioCacheState.Released)
            {
                return;
            }
            entry.m_State = AudioCacheState.Released;
            if (entry.m_ResourceRequest != null)
            {
                m_LoadEntriesByRequestId.Remove(entry.m_ResourceRequest.LoadIndex);
                UIRes.UnloadPrefab(entry.m_ResourceRequest);
                entry.m_ResourceRequest = null;
            }
            if (destroyInstances)
            {
                for (int i = entry.m_Instances.Count - 1; i >= 0; i--)
                {
                    DestroyInstance(entry, entry.m_Instances[i]);
                }
            }
            entry.m_PendingRequests.Clear();
            entry.m_SourcePrefab = null;
            m_EntriesByKey.Remove(entry.m_Key);
            m_Entries.Remove(entry);
        }

        /// <summary>判断缓存记录是否仍是该资源键当前注册的活动记录。</summary>
        /// <param name="entry">需要检查的缓存记录。</param>
        /// <returns>记录仍位于缓存索引中且引用一致时返回 <see langword="true"/>。</returns>
        private bool IsActiveEntry(AudioCacheEntry entry)
        {
            AudioCacheEntry activeEntry;
            return entry != null && m_EntriesByKey.TryGetValue(entry.m_Key, out activeEntry) && activeEntry == entry;
        }

        /// <summary>通过播放控制器回调判断异步等待请求是否仍然有效。</summary>
        /// <param name="request">需要检查的播放请求。</param>
        /// <returns>请求仍然注册时返回 <see langword="true"/>。</returns>
        private bool IsRequestRegistered(AudioPlaybackRequest request)
        {
            return m_IsRequestRegistered != null && m_IsRequestRegistered(request);
        }

        /// <summary>通知播放控制器资源缓存已经就绪。</summary>
        /// <param name="request">等待资源的播放请求。</param>
        /// <param name="entry">已经就绪的缓存记录。</param>
        private void NotifyEntryReady(AudioPlaybackRequest request, AudioCacheEntry entry)
        {
            if (m_OnEntryReady != null)
            {
                m_OnEntryReady(request, entry);
            }
        }

        /// <summary>通知播放控制器指定请求未能取得声音资源。</summary>
        /// <param name="request">失败的播放请求。</param>
        /// <param name="entry">关联的缓存记录。</param>
        /// <param name="reason">失败原因。</param>
        /// <param name="isCancellation">是否属于主动取消或生命周期失效。</param>
        private void NotifyRequestFailed(AudioPlaybackRequest request, AudioCacheEntry entry, string reason, bool isCancellation)
        {
            if (m_OnRequestFailed != null)
            {
                m_OnRequestFailed(request, entry, reason, isCancellation);
            }
        }
    }
}
