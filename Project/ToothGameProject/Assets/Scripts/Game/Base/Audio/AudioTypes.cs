using LCL;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    /// <summary>
    /// 定义声音在 <see cref="UnityEngine.Audio.AudioMixer"/> 中所属的业务总线。
    /// </summary>
    /// <remarks>
    /// 总线由声音预制件的输出分组决定。每条总线拥有独立的玩家音量、运行时控制音量、压低音量和默认过渡时间。
    /// </remarks>
    public enum AudioBus
    {
        /// <summary>背景音乐总线。</summary>
        Music,

        /// <summary>界面声音总线。</summary>
        UI,

        /// <summary>三维世界和普通战斗音效总线。</summary>
        World,

        /// <summary>角色对白、旁白和其他语音总线。</summary>
        Voice
    }

    /// <summary>
    /// 定义可以同时选中的声音总线集合，主要用于声明一次播放需要压低哪些总线。
    /// </summary>
    [Flags]
    public enum AudioBusMask
    {
        /// <summary>不选择任何声音总线。</summary>
        None = 0,

        /// <summary>选择背景音乐总线。</summary>
        Music = 1 << 0,

        /// <summary>选择界面声音总线。</summary>
        UI = 1 << 1,

        /// <summary>选择三维世界和普通战斗音效总线。</summary>
        World = 1 << 2,

        /// <summary>选择角色对白、旁白和其他语音总线。</summary>
        Voice = 1 << 3,

        /// <summary>选择全部声音总线。</summary>
        All = Music | UI | World | Voice
    }

    /// <summary>
    /// 定义一次声音播放期间对其他声音总线执行压低处理的参数。
    /// </summary>
    /// <remarks>
    /// <para>压低从该声音真正开始播放时生效，在该播放停止、完成或被释放后自动恢复。</para>
    /// <para>多个播放同时压低同一总线时采用其中最低的目标音量，不会连续相乘，因此不会因重复请求而不断变小。</para>
    /// <para>默认值表示不启用压低。</para>
    /// </remarks>
    public struct AudioDuckOptions
    {
        // 压低只用于调整听感，不承担静音职责。0.06309573 约等于 -24 dB。
        internal const float m_MinimumVolume = 0.06309573f;

        /// <summary>需要压低的声音总线集合。</summary>
        public AudioBusMask m_TargetBuses;

        /// <summary>
        /// 压低后的线性音量，取值范围为 0.06309573 到 1；1 表示保持原音量。
        /// </summary>
        public float m_TargetVolume;

        /// <summary>
        /// 从当前音量降低到目标音量所用秒数；小于 0 时使用管理器默认时间，0 表示立即完成。
        /// </summary>
        public float m_AttackSeconds;

        /// <summary>
        /// 声音结束后从目标音量恢复到原音量所用秒数；小于 0 时使用管理器默认时间，0 表示立即完成。
        /// </summary>
        public float m_ReleaseSeconds;

        /// <summary>
        /// 创建声音压低参数。
        /// </summary>
        /// <param name="targetBuses">需要压低的声音总线集合。</param>
        /// <param name="targetVolume">压低后的线性音量。系统会将该值限制在 0.06309573 到 1 之间。</param>
        /// <param name="attackSeconds">进入压低状态所用秒数；小于 0 使用管理器默认时间，0 立即完成。</param>
        /// <param name="releaseSeconds">退出压低状态所用秒数；小于 0 使用管理器默认时间，0 立即完成。</param>
        /// <example>
        /// 播放语音时，将除语音外的总线压低到 35%。
        /// <code>
        /// var options = new AudioDuckOptions(AudioBusMask.All &amp; ~AudioBusMask.Voice, 0.35f);
        /// AudioManager.GetInstance().Play2D(200, options);
        /// </code>
        /// </example>
        public AudioDuckOptions(AudioBusMask targetBuses, float targetVolume, float attackSeconds = -1f, float releaseSeconds = -1f)
        {
            m_TargetBuses = targetBuses;
            m_TargetVolume = Mathf.Clamp(targetVolume, m_MinimumVolume, 1f);
            m_AttackSeconds = attackSeconds;
            m_ReleaseSeconds = releaseSeconds;
        }

        /// <summary>
        /// 判断当前参数是否会产生实际压低效果。
        /// </summary>
        /// <returns>选择了至少一条总线且目标音量小于 1 时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        internal bool IsEnabled()
        {
            return m_TargetBuses != AudioBusMask.None && m_TargetVolume < 1f;
        }
    }

    /// <summary>
    /// 定义新声音开始播放时是否与同一总线的上一条过渡声音交叉淡入淡出。
    /// </summary>
    public enum AudioTransitionMode
    {
        /// <summary>不执行交叉过渡，声音按预制件原始音量直接播放。</summary>
        None,

        /// <summary>使用该总线的两个过渡分组进行等功率交叉淡入淡出。</summary>
        CrossFade
    }

    /// <summary>
    /// 定义再次播放相同声音配置编号时如何处理已有播放请求。
    /// </summary>
    public enum AudioReplayMode
    {
        /// <summary>始终创建新的播放请求，允许同一个配置编号同时存在多个实例。</summary>
        CreateNew,

        /// <summary>先停止该配置编号的全部已有请求，再从头创建一次播放。</summary>
        RestartCurrent,

        /// <summary>已有请求存在时继续保留它，不重新开始；不存在时才创建新的播放请求。</summary>
        KeepCurrent
    }

    /// <summary>
    /// 定义播放请求在流程清理时的保留级别。
    /// </summary>
    public enum AudioLifetime
    {
        /// <summary>临时声音，会被 <see cref="AudioClearType.Transient"/> 清理。</summary>
        Transient,

        /// <summary>跨普通流程保留的声音，只会在全部清理或显式停止时释放。</summary>
        Persistent,

        /// <summary>由业务持有 <see cref="AudioHandle"/> 并显式管理停止时机的声音。</summary>
        Manual
    }

    /// <summary>
    /// 定义 <see cref="AudioManager.Clear(AudioClearType)"/> 的清理范围。
    /// </summary>
    public enum AudioClearType
    {
        /// <summary>停止并释放所有 <see cref="AudioLifetime.Transient"/> 请求。</summary>
        Transient,

        /// <summary>仅清理已经停止且超过缓存保留时间的空闲实例。</summary>
        Idle,

        /// <summary>停止全部请求并释放全部声音资源、实例和运行时状态。</summary>
        All
    }

    /// <summary>
    /// 定义一次声音播放请求结束或未能创建的原因。
    /// </summary>
    public enum AudioPlaybackEndReason
    {
        /// <summary>非循环声音自然播放到结尾。</summary>
        Completed,

        /// <summary>业务通过 <see cref="AudioManager.StopAudio(AudioHandle, float)"/> 主动停止。</summary>
        Stopped,

        /// <summary>相同配置重新播放或同一总线交叉过渡时被新声音替换。</summary>
        Replaced,

        /// <summary>请求因 <see cref="AudioManager.Clear(AudioClearType)"/> 或 <see cref="AudioManager.UnInit"/> 被清理。</summary>
        Cleared,

        /// <summary>声音资源加载失败或运行时实例创建失败。</summary>
        LoadFailed,

        /// <summary>请求因管理器未就绪、配置无效、最小播放间隔或失败冷却等条件未能创建。</summary>
        Rejected
    }

    /// <summary>
    /// 提供声音播放结束回调需要的只读结果数据。
    /// </summary>
    /// <remarks>
    /// <see cref="m_Handle"/> 只用于识别原请求。回调执行前请求已经完成内部清理，不能再通过该句柄控制声音。
    /// </remarks>
    public struct AudioPlaybackResult
    {
        /// <summary>结束请求的句柄；请求创建前被拒绝时为默认值。</summary>
        public readonly AudioHandle m_Handle;

        /// <summary><c>t_sound</c> 配置编号。</summary>
        public readonly int m_ConfigId;

        /// <summary>播放请求结束或未能创建的原因。</summary>
        public readonly AudioPlaybackEndReason m_Reason;

        /// <summary>用于排查失败或拒绝原因的说明；正常完成时可能为空。</summary>
        public readonly string m_Message;

        /// <summary>创建一份声音播放结果。</summary>
        internal AudioPlaybackResult(AudioHandle handle, int configId, AudioPlaybackEndReason reason, string message)
        {
            m_Handle = handle;
            m_ConfigId = configId;
            m_Reason = reason;
            m_Message = message;
        }
    }

    /// <summary>
    /// 定义声音播放请求在不同结束原因下需要执行的可选回调。
    /// </summary>
    /// <remarks>
    /// <para>一次注册只会执行与最终原因对应的一个回调，未赋值的回调不会产生额外处理。</para>
    /// <para>回调在请求状态、声音压低和资源索引清理完成后由主线程派发。暂停和恢复不属于播放结束，不会触发这些回调。</para>
    /// <para>循环声音不会自然触发 <see cref="m_OnCompleted"/>，必须被停止、替换或清理后才会执行对应回调。</para>
    /// </remarks>
    public struct AudioPlaybackCallbacks
    {
        /// <summary>非循环声音自然播放到结尾时执行。</summary>
        public Action<AudioPlaybackResult> m_OnCompleted;

        /// <summary>业务主动停止声音时执行。</summary>
        public Action<AudioPlaybackResult> m_OnStopped;

        /// <summary>声音被新的播放请求替换时执行。</summary>
        public Action<AudioPlaybackResult> m_OnReplaced;

        /// <summary>声音因流程清理或管理器反初始化被释放时执行。</summary>
        public Action<AudioPlaybackResult> m_OnCleared;

        /// <summary>声音资源加载或运行时实例创建失败时执行。</summary>
        public Action<AudioPlaybackResult> m_OnLoadFailed;

        /// <summary>播放请求在创建前被管理器状态或播放规则拒绝时执行。</summary>
        public Action<AudioPlaybackResult> m_OnRejected;

        /// <summary>读取当前是否至少注册了一种结束回调。</summary>
        internal bool HasAny()
        {
            return m_OnCompleted != null || m_OnStopped != null || m_OnReplaced != null || m_OnCleared != null || m_OnLoadFailed != null || m_OnRejected != null;
        }

        /// <summary>把另一组回调追加到当前注册中，不覆盖已经存在的业务回调。</summary>
        internal void Append(AudioPlaybackCallbacks callbacks)
        {
            m_OnCompleted += callbacks.m_OnCompleted;
            m_OnStopped += callbacks.m_OnStopped;
            m_OnReplaced += callbacks.m_OnReplaced;
            m_OnCleared += callbacks.m_OnCleared;
            m_OnLoadFailed += callbacks.m_OnLoadFailed;
            m_OnRejected += callbacks.m_OnRejected;
        }

        /// <summary>读取与指定结束原因对应的回调。</summary>
        internal Action<AudioPlaybackResult> ReadCallback(AudioPlaybackEndReason reason)
        {
            switch (reason)
            {
                case AudioPlaybackEndReason.Completed:
                    return m_OnCompleted;
                case AudioPlaybackEndReason.Stopped:
                    return m_OnStopped;
                case AudioPlaybackEndReason.Replaced:
                    return m_OnReplaced;
                case AudioPlaybackEndReason.Cleared:
                    return m_OnCleared;
                case AudioPlaybackEndReason.LoadFailed:
                    return m_OnLoadFailed;
                case AudioPlaybackEndReason.Rejected:
                    return m_OnRejected;
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// 表示一个播放请求当前所处的生命周期状态。
    /// </summary>
    public enum AudioPlaybackState
    {
        /// <summary>声音资源正在异步加载，尚未取得可播放实例。</summary>
        WaitingForLoad,

        /// <summary>播放已经结束，实例处于缓存等待状态。</summary>
        Idle,

        /// <summary>声音正在播放或正在执行停止淡出。</summary>
        Playing,

        /// <summary>播放因手动、正式游戏暂停或系统暂停而暂停。</summary>
        Paused,

        /// <summary>请求已经释放，句柄不再有效。</summary>
        Released
    }

    /// <summary>
    /// 标识一次由 <see cref="AudioManager"/> 创建的声音播放请求。
    /// </summary>
    /// <remarks>
    /// 句柄同时保存请求编号和管理器生命周期版本。调用 <see cref="AudioManager.UnInit"/> 后，旧句柄不会错误操作下一次初始化创建的新请求。
    /// </remarks>
    public struct AudioHandle : IEquatable<AudioHandle>
    {
        private readonly long m_RequestId;
        private readonly int m_LifecycleVersion;

        /// <summary>
        /// 创建内部播放句柄。
        /// </summary>
        /// <param name="requestId">播放请求的唯一编号。</param>
        /// <param name="lifecycleVersion">创建请求时的管理器生命周期版本。</param>
        internal AudioHandle(long requestId, int lifecycleVersion)
        {
            m_RequestId = requestId;
            m_LifecycleVersion = lifecycleVersion;
        }

        /// <summary>读取播放请求的内部唯一编号。</summary>
        /// <returns>播放请求编号。</returns>
        internal long ReadRequestId()
        {
            return m_RequestId;
        }

        /// <summary>读取创建该句柄时的管理器生命周期版本。</summary>
        /// <returns>管理器生命周期版本。</returns>
        internal int ReadLifecycleVersion()
        {
            return m_LifecycleVersion;
        }

        /// <summary>
        /// 获取该值是否由一次有效的播放请求创建。
        /// </summary>
        /// <value>请求编号大于 0 时为 <see langword="true"/>；默认结构体为 <see langword="false"/>。</value>
        public bool IsCreated
        {
            get { return m_RequestId > 0; }
        }

        /// <summary>判断当前句柄与另一个句柄是否指向同一生命周期中的同一播放请求。</summary>
        /// <param name="other">需要比较的另一个句柄。</param>
        /// <returns>两个句柄完全相同时返回 <see langword="true"/>。</returns>
        public bool Equals(AudioHandle other)
        {
            return m_RequestId == other.m_RequestId && m_LifecycleVersion == other.m_LifecycleVersion;
        }

        /// <summary>判断当前句柄是否与指定对象表示同一播放请求。</summary>
        /// <param name="obj">需要比较的对象。</param>
        /// <returns>对象是相同的 <see cref="AudioHandle"/> 时返回 <see langword="true"/>。</returns>
        public override bool Equals(object obj)
        {
            return obj is AudioHandle && Equals((AudioHandle)obj);
        }

        /// <summary>返回由请求编号和生命周期版本组成的哈希值。</summary>
        /// <returns>当前句柄的哈希值。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (m_RequestId.GetHashCode() * 397) ^ m_LifecycleVersion;
            }
        }
    }

    /// <summary>
    /// 记录一个播放请求可能同时存在的暂停原因。
    /// </summary>
    [Flags]
    internal enum AudioPauseReason
    {
        /// <summary>没有暂停原因。</summary>
        None = 0,

        /// <summary>业务通过 <see cref="AudioManager.PauseAudio(AudioHandle, float)"/> 手动暂停。</summary>
        Manual = 1,

        /// <summary>正式游戏暂停流程暂停了非界面声音。</summary>
        Formal = 2,

        /// <summary>应用失去焦点或进入系统暂停状态。</summary>
        System = 4
    }

    /// <summary>
    /// 表示一份声音资源缓存当前所处的异步加载状态。
    /// </summary>
    internal enum AudioCacheState
    {
        /// <summary>资源正在加载。</summary>
        Loading,

        /// <summary>资源及其 <see cref="AudioSource"/> 预制件已经可用。</summary>
        Ready,

        /// <summary>资源缓存已经释放，不能再创建实例。</summary>
        Released
    }

    /// <summary>
    /// 以资源包名、资源名和资源类型共同标识一份声音资源。
    /// </summary>
    internal struct AudioResourceKey : IEquatable<AudioResourceKey>
    {
        /// <summary>声音资源所在的资源包路径。</summary>
        public string m_AbName;

        /// <summary>声音预制件的资源名。</summary>
        public string m_AssetName;

        /// <summary>异步加载时请求的资源类型。</summary>
        public Type m_AssetType;

        /// <summary>创建声音资源缓存键。</summary>
        /// <param name="abName">声音资源所在的资源包路径。</param>
        /// <param name="assetName">声音预制件的资源名。</param>
        /// <param name="assetType">异步加载时请求的资源类型。</param>
        public AudioResourceKey(string abName, string assetName, Type assetType)
        {
            m_AbName = abName;
            m_AssetName = assetName;
            m_AssetType = assetType;
        }

        /// <summary>判断两份资源键是否表示同一份声音资源。</summary>
        /// <param name="other">需要比较的另一份资源键。</param>
        /// <returns>三个资源标识字段均相同时返回 <see langword="true"/>。</returns>
        public bool Equals(AudioResourceKey other)
        {
            return m_AbName == other.m_AbName && m_AssetName == other.m_AssetName && m_AssetType == other.m_AssetType;
        }

        /// <summary>判断指定对象是否表示同一份声音资源。</summary>
        /// <param name="obj">需要比较的对象。</param>
        /// <returns>对象是相同的 <see cref="AudioResourceKey"/> 时返回 <see langword="true"/>。</returns>
        public override bool Equals(object obj)
        {
            return obj is AudioResourceKey && Equals((AudioResourceKey)obj);
        }

        /// <summary>返回由资源包名、资源名和资源类型组成的哈希值。</summary>
        /// <returns>当前资源键的哈希值。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = m_AbName != null ? m_AbName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (m_AssetName != null ? m_AssetName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_AssetType != null ? m_AssetType.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    /// <summary>
    /// 保存一次声音播放从创建、加载、播放、暂停到释放的完整运行时数据。
    /// </summary>
    internal sealed class AudioPlaybackRequest
    {
        /// <summary>当前管理器生命周期内唯一的播放请求编号。</summary>
        public long m_RequestId;
        /// <summary>创建请求时的管理器生命周期版本。</summary>
        public int m_LifecycleVersion;
        /// <summary><c>t_sound</c> 配置编号，也是相同声音重播策略的判断依据。</summary>
        public int m_ConfigId;
        /// <summary>创建或重新激活请求时的递增顺序，用于淘汰过期的异步交叉过渡请求。</summary>
        public long m_Sequence;
        /// <summary>请求当前的播放状态。</summary>
        public AudioPlaybackState m_State;
        /// <summary>请求在流程清理时的保留级别。</summary>
        public AudioLifetime m_Lifetime;
        /// <summary>相同配置编号再次播放时采用的处理方式。</summary>
        public AudioReplayMode m_ReplayMode;
        /// <summary>开始播放时采用的过渡方式。</summary>
        public AudioTransitionMode m_TransitionMode;
        /// <summary>请求指定的过渡秒数；小于 0 表示使用总线默认值。</summary>
        public float m_TransitionSeconds;
        /// <summary>实例停止后允许保留在缓存中的秒数。</summary>
        public float m_CacheSeconds;
        /// <summary>是否按三维世界声音配置 <see cref="AudioSource.spatialBlend"/>。</summary>
        public bool m_Is3D;
        /// <summary>三维声音的世界坐标；二维声音保存创建请求时的监听器位置。</summary>
        public Vector3 m_Position;
        /// <summary>资源预制件实际连接的声音总线。</summary>
        public AudioBus m_Bus;
        /// <summary>资源预制件是否连接到管理器支持的混音分组。</summary>
        public bool m_HasValidRoute;
        /// <summary>请求是否正在执行停止淡出，防止重复恢复或暂停。</summary>
        public bool m_IsStopping;
        /// <summary>停止过渡完成后需要上报的结束原因。</summary>
        public AudioPlaybackEndReason m_PendingEndReason;
        /// <summary>当前是否已经明确设置停止过渡的结束原因。</summary>
        public bool m_HasPendingEndReason;
        /// <summary>该播放请求生效期间使用的声音压低参数。</summary>
        public AudioDuckOptions m_DuckOptions;
        /// <summary>业务为本次播放请求注册的结束回调。</summary>
        public AudioPlaybackCallbacks m_Callbacks;
        /// <summary>当前所有暂停原因的组合。</summary>
        public AudioPauseReason m_PauseReasons;
        /// <summary>该请求使用的资源缓存记录。</summary>
        public AudioCacheEntry m_CacheEntry;
        /// <summary>该请求当前占用的 <see cref="AudioSource"/> 运行时实例。</summary>
        public AudioSourceInstance m_Instance;
    }

    /// <summary>
    /// 保存一份声音预制件的加载请求、待播放请求和可复用实例集合。
    /// </summary>
    internal sealed class AudioCacheEntry
    {
        /// <summary>唯一标识该资源的缓存键。</summary>
        public AudioResourceKey m_Key;
        /// <summary>当前缓存状态。</summary>
        public AudioCacheState m_State;
        /// <summary>资源系统返回的异步加载请求。</summary>
        public ABRequest m_ResourceRequest;
        /// <summary>创建缓存记录时的管理器生命周期版本。</summary>
        public int m_LifecycleVersion;
        /// <summary>加载完成的声音预制件。</summary>
        public GameObject m_SourcePrefab;
        /// <summary>声音预制件实际连接的声音总线。</summary>
        public AudioBus m_Bus;
        /// <summary>声音预制件是否连接到管理器支持的混音分组。</summary>
        public bool m_HasValidRoute;
        /// <summary>等待该资源加载完成的播放请求。</summary>
        public readonly List<AudioPlaybackRequest> m_PendingRequests = new List<AudioPlaybackRequest>();
        /// <summary>由该预制件创建且尚未销毁的所有运行时实例。</summary>
        public readonly List<AudioSourceInstance> m_Instances = new List<AudioSourceInstance>();
    }
}
