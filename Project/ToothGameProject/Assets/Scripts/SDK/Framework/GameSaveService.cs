using System;
using UnityEngine;
using YoudooGame.Core;
using YoudooGame.Framework;

// SDK 存档服务：启动时经 YoudooAPI 拉取平台存档（结果由平台通知送达），
// 保存采用下一帧合并 flush——同一帧内多次修改只触发一次 SDK 写入。
// 运行时游戏数据全部驻留内存，本地不再写任何存档文件（含 PlayerPrefs）。
[Serializable]
public class GameSaveRoot
{
    public int m_SaveVersion = 1;

    // 各模块数据段：GameDll 侧类型经 JSON 字符串解耦，主工程类型直接平铺。
    public string m_LobbyJson = "";
    public string m_GuideJson = "";
    public float[] m_AudioBusVolumes = { -1f, -1f, -1f, -1f };
    public int m_AudioMuted = 0;
    public int m_GraphicsAsync = 1;
    public int m_GraphicsAfter = 0;
}

public sealed class GameSaveService : MonoBehaviour
{
    private const string kTag = "[GameSaveService]";
    private const float kReadyTimeoutSeconds = 8f;
    private const float m_SaveAcknowledgmentTimeoutSeconds = 15f;
    private const float m_CloudReadbackIntervalSeconds = 5f;

    private const int kMaxFetchFailures = 3;

    public static GameSaveService Instance { get; private set; }

    public static event Action SaveDataReady;

    public bool IsReady { get; private set; }
    public bool RequiresProfileReload { get; private set; }
    public bool IsAccountReadBlocked { get; private set; }

    public string RawSave { get; private set; }

    public GameSaveRoot Root { get; private set; } = new GameSaveRoot();

    [Serializable]
    private sealed class SaveSectionVersion
    {
        public int m_SaveVersion;
    }

    private enum AccountConfirmation
    {
        None,
        SnapshotRequired,
        AwaitingChangeDetails,
        AwaitingMatchingSnapshot
    }

    private enum SaveRead
    {
        None,
        Profile,
        WriteVerification,
        PreviousAccount
    }

    private string m_AccountKey;
    private AccountConfirmation m_AccountConfirmation;
    private float m_NextAccountRefreshAt;
    private SaveRead m_PendingRead;
    private float m_DrainDeadline;
    private long m_NotificationSequence;
    private float m_ElapsedSeconds;
    private float m_FetchStartedAt = -1f;
    private float m_NextFetchRetryTime;
    private float m_NextFlushRetryTime;
    private string m_InFlightData;
    private string m_ConfirmedCloudSave;
    private float m_InFlightRetryAt;
    private float m_CloudReadbackDeadline;
    private float m_NextCloudReadbackAt;
    private bool m_RequireWriteReadback;
    private bool m_CloudConflictLogged;
    private bool m_FlushQueued;
    private bool m_PlatformSaveUnavailableLogged;
    private int m_FetchFailureStreak;
    private bool m_UploadSuppressed;
    private bool m_UploadSuppressLogged;
    private bool m_PreReadyFlushDroppedLogged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Initialize()
    {
        if (IsReady || RequiresProfileReload)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (m_PendingRead != SaveRead.None)
        {
            if (m_PendingRead == SaveRead.PreviousAccount && now >= m_DrainDeadline && !IsAccountReadBlocked)
            {
                IsAccountReadBlocked = true;
                Debug.LogWarning(kTag + " 上一账号的读档回包尚未返回。");
            }
            return;
        }
        IsAccountReadBlocked = false;
        if (m_FetchStartedAt < 0f)
        {
            m_FetchStartedAt = now;
        }

        if (!ConfirmSaveAccount())
        {
            if (RequiresProfileReload)
            {
                return;
            }

            if (now - m_FetchStartedAt >= kReadyTimeoutSeconds)
            {
                Debug.LogError(kTag + " 无法确认平台存档所属账号，当前档案仅在本次运行中使用。");
                ReleaseReadOnly();
            }
            else
            {
                m_NextFetchRetryTime = now + 0.5f;
            }
            return;
        }

        m_ElapsedSeconds = 0f;
        var result = YoudooAPI.PlatformService.GetGameSave();
        if (result.Success && result.Value)
        {
            m_PendingRead = SaveRead.Profile;
            GameSdkLog.Info(kTag + " fetch requested");
            return;
        }

        if (Application.platform == RuntimePlatform.Android)
        {
            bool waitingForProvider = result.Error == ErrorCode.NotInitialized;
            if (waitingForProvider && now - m_FetchStartedAt < kReadyTimeoutSeconds)
            {
                m_NextFetchRetryTime = now + 0.5f;
                return;
            }

            HandleFetchFailure("GetGameSave 同步拒绝 " + result.Error + " " + result.Message,
                waitingForProvider ? 0.5f : kReadyTimeoutSeconds);
            return;
        }

        // 非真机环境的同步受理失败本身就是 SDK 适配层的明确结果：无平台存档能力。
        GameSdkLog.Info(kTag + " GetPlatformGameSave not accepted on editor: " + result.Message);
        MarkReady(string.Empty);
    }

    private void Update()
    {
        PollNotifications();
        RetryFetchIfNeeded();
        PollReadyTimeout();
        FlushIfNeeded();
    }

    private void RetryFetchIfNeeded()
    {
        if (IsReady || RequiresProfileReload || m_PendingRead == SaveRead.Profile)
        {
            return;
        }

        if (Time.unscaledTime < m_NextFetchRetryTime)
        {
            return;
        }

        Initialize();
    }

    private void PollNotifications()
    {
        var batch = YoudooAPI.PlatformService.GetNotificationsSince(m_NotificationSequence);
        m_NotificationSequence = batch.NextSequence;
        // 先处理账号事件。同批旧回包不得为新账号初始化档案或重发写入。
        if (Application.platform == RuntimePlatform.Android)
        {
            if (batch.HistoryLost)
            {
                Debug.LogWarning(kTag + " SDK 通知历史缺失，正在重新核对平台账号。pendingRead=" + m_PendingRead);
                if (m_AccountConfirmation == AccountConfirmation.None)
                {
                    m_AccountConfirmation = AccountConfirmation.SnapshotRequired;
                }

                if (m_InFlightData != null)
                {
                    m_RequireWriteReadback = true;
                }
            }

            if (batch.Items != null)
            {
                foreach (var item in batch.Items)
                {
                    var notification = item.Notification;
                    if (notification.Kind != PlatformNotificationKind.AccountChanged || !notification.Success)
                    {
                        continue;
                    }

                    string key = GetAccountKey(notification.Data as AccountInfo);
                    if (key == null)
                    {
                        m_AccountConfirmation = AccountConfirmation.AwaitingChangeDetails;
                        continue;
                    }

                    ObserveAccount(key);
                    // 原生查询可能在收到换号事件后的几帧内仍返回旧账号。
                    m_AccountConfirmation = AccountConfirmation.AwaitingMatchingSnapshot;
                }
            }

            if (batch.HistoryLost || (m_AccountConfirmation != AccountConfirmation.None &&
                                      Time.unscaledTime >= m_NextAccountRefreshAt))
            {
                ConfirmSaveAccount();
            }
        }

        if (batch.Items == null)
        {
            return;
        }

        foreach (var item in batch.Items)
        {
            var notification = item.Notification;
            if (notification.Kind == PlatformNotificationKind.GameSaveLoaded)
            {
                HandleSaveLoaded(notification);
                continue;
            }

            if (notification.Kind != PlatformNotificationKind.GameSaveStored ||
                m_InFlightData == null || m_RequireWriteReadback)
            {
                continue;
            }

            if (notification.Success)
            {
                if (ConfirmSaveAccount())
                {
                    ConfirmInFlightSave();
                }
            }
            else
            {
                Debug.LogWarning(kTag + " GameSaveStored failed, will retry: " + notification.Message);
                m_InFlightData = null;
                m_FlushQueued = true;
                m_NextFlushRetryTime = Time.unscaledTime + 1f;
            }
        }
    }

    private void ObserveAccount(string key)
    {
        m_AccountConfirmation = AccountConfirmation.None;
        if (!string.Equals(key, m_AccountKey, StringComparison.Ordinal) && (m_AccountKey != null || IsReady))
        {
            IsReady = false;
            RequiresProfileReload = true;
            if (m_PendingRead != SaveRead.None)
            {
                m_PendingRead = SaveRead.PreviousAccount;
            }
            m_InFlightData = null;
            m_ConfirmedCloudSave = null;
            m_CloudReadbackDeadline = 0f;
            m_FlushQueued = false;
            m_RequireWriteReadback = true;
            Debug.LogWarning(kTag + " 平台账号已切换，已隔离原账号读写，等待登录流程重新读档。");
        }
        m_AccountKey = key;
    }

    public bool CheckAccountChangeBeforeBattle()
    {
        if (!RequiresProfileReload && Application.platform == RuntimePlatform.Android && m_AccountKey != null)
        {
            ConfirmSaveAccount();
        }

        return RequiresProfileReload;
    }

    public void BeginProfileReload()
    {
        if (!RequiresProfileReload)
        {
            return;
        }

        RequiresProfileReload = false;
        IsAccountReadBlocked = false;
        RawSave = null;
        Root = new GameSaveRoot();
        m_FetchFailureStreak = 0;
        m_FetchStartedAt = -1f;
        m_NextFetchRetryTime = 0f;
        m_NextFlushRetryTime = 0f;
        m_UploadSuppressed = false;
        m_UploadSuppressLogged = false;
        m_CloudConflictLogged = false;
        m_DrainDeadline = Time.unscaledTime + kReadyTimeoutSeconds;
    }

    public void RetryBlockedAccountRead()
    {
        if (!IsAccountReadBlocked || RequiresProfileReload)
        {
            return;
        }

        PollNotifications();
        if (RequiresProfileReload)
        {
            return;
        }
        if (m_PendingRead != SaveRead.None)
        {
            Debug.LogWarning(kTag + " 旧读档请求尚未结束，当前账号读档仍隔离。pendingRead=" + m_PendingRead);
            return;
        }

        Initialize();
    }

    public void ContinueWithReadOnlyProfile()
    {
        if (IsAccountReadBlocked && !RequiresProfileReload)
        {
            IsAccountReadBlocked = false;
            ReleaseReadOnly();
        }
    }

    private static string GetAccountKey(AccountInfo account)
    {
        if (account == null)
        {
            return null;
        }
        if (account.AccountId > 0)
        {
            return "id:" + account.AccountId;
        }
        if (!string.IsNullOrWhiteSpace(account.Username))
        {
            return "name:" + account.Username.Trim();
        }
        return account.IsGuest ? "guest" : null;
    }

    private bool ConfirmSaveAccount()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            return true;
        }

        var account = YoudooAPI.PlatformService.GetAccountInfo();
        string key = account.Success ? GetAccountKey(account.Value) : null;
        bool matchesAccount = string.Equals(key, m_AccountKey, StringComparison.Ordinal);
        bool anonymousGuest = key == "guest" && m_AccountKey != null && !matchesAccount;
        if (key == null || anonymousGuest || m_AccountConfirmation == AccountConfirmation.AwaitingChangeDetails ||
            (m_AccountConfirmation == AccountConfirmation.AwaitingMatchingSnapshot && !matchesAccount))
        {
            if (anonymousGuest && m_AccountConfirmation == AccountConfirmation.None)
            {
                m_AccountConfirmation = AccountConfirmation.SnapshotRequired;
            }
            RequestAccountRefresh();
            return false;
        }

        ObserveAccount(key);
        return !RequiresProfileReload;
    }

    private void RequestAccountRefresh()
    {
        float now = Time.unscaledTime;
        if (now >= m_NextAccountRefreshAt)
        {
            m_NextAccountRefreshAt = now + 1f;
            YoudooAPI.PlatformService.RefreshAccountInfo();
        }
    }

    private void HandleSaveLoaded(PlatformNotification notification)
    {
        var pendingRead = m_PendingRead;
        if (pendingRead == SaveRead.None)
        {
            return;
        }

        m_PendingRead = SaveRead.None;
        if (pendingRead == SaveRead.WriteVerification)
        {
            m_CloudReadbackDeadline = 0f;
        }
        if (RequiresProfileReload || pendingRead == SaveRead.PreviousAccount ||
            (pendingRead == SaveRead.Profile && IsReady))
        {
            return;
        }
        if (!ConfirmSaveAccount())
        {
            return;
        }

        if (pendingRead == SaveRead.WriteVerification)
        {
            HandleCloudReadback(notification);
        }
        else if (notification.Success && notification.Data is string rawSave)
        {
            MarkReady(rawSave);
        }
        else
        {
            HandleFetchFailure("GameSaveLoaded 结果无效 " + notification.Message, 1f);
        }
    }

    private void ConfirmInFlightSave()
    {
        m_ConfirmedCloudSave = m_InFlightData;
        m_InFlightData = null;
        m_CloudReadbackDeadline = 0f;
        m_CloudConflictLogged = false;
        GameSdkLog.Info(kTag + " save confirmed");
    }

    private void HandleCloudReadback(PlatformNotification notification)
    {
        if (!m_RequireWriteReadback || m_InFlightData == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        m_NextCloudReadbackAt = now + m_CloudReadbackIntervalSeconds;
        if (!notification.Success || !(notification.Data is string cloudData))
        {
            return;
        }
        if (string.Equals(cloudData, m_InFlightData, StringComparison.Ordinal))
        {
            ConfirmInFlightSave();
            return;
        }

        if (!string.Equals(cloudData, m_ConfirmedCloudSave, StringComparison.Ordinal))
        {
            if (!m_CloudConflictLogged)
            {
                m_CloudConflictLogged = true;
                Debug.LogWarning(kTag + " 云存档与已确认数据不一致，保留待保存进度，等待复核。");
            }
            return;
        }

        if (now >= m_InFlightRetryAt)
        {
            // 云端仍是本服务最后确认的存档，先重试同一笔，再写入后来积累的进度。
            var result = YoudooAPI.PlatformService.SetGameSave(m_InFlightData);
            bool accepted = result.Success && result.Value;
            m_InFlightRetryAt = now + (accepted
                ? m_SaveAcknowledgmentTimeoutSeconds
                : m_CloudReadbackIntervalSeconds);
            if (!accepted)
            {
                Debug.LogWarning(kTag + " 云存档重试未受理。error=" + result.Error);
            }
        }
    }

    private void CheckInFlightSave()
    {
        float now = Time.unscaledTime;
        if (!m_RequireWriteReadback)
        {
            if (now < m_InFlightRetryAt)
            {
                return;
            }

            // SDK 写回执没有请求 ID。第一个超时后只用云端内容确认写入。
            m_RequireWriteReadback = true;
            Debug.LogWarning(kTag + " 云存档写入回执超时，开始读回校验。");
        }

        if (m_PendingRead != SaveRead.None)
        {
            if (m_PendingRead == SaveRead.WriteVerification && m_CloudReadbackDeadline > 0f &&
                now >= m_CloudReadbackDeadline)
            {
                m_CloudReadbackDeadline = 0f;
                Debug.LogWarning(kTag + " 云存档校验回包尚未返回，继续等待并保留待保存进度。");
            }
            return;
        }

        if (now < m_NextCloudReadbackAt)
        {
            return;
        }

        m_NextCloudReadbackAt = now + m_CloudReadbackIntervalSeconds;
        if (!ConfirmSaveAccount())
        {
            return;
        }

        var result = YoudooAPI.PlatformService.GetGameSave();
        if (result.Success && result.Value)
        {
            m_PendingRead = SaveRead.WriteVerification;
            m_CloudReadbackDeadline = now + kReadyTimeoutSeconds;
        }
    }

    private void PollReadyTimeout()
    {
        if (IsReady || m_PendingRead != SaveRead.Profile)
        {
            return;
        }

        if (Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        float previousElapsed = m_ElapsedSeconds;
        m_ElapsedSeconds += Time.unscaledDeltaTime;
        float waitLimit = kReadyTimeoutSeconds * (kMaxFetchFailures - m_FetchFailureStreak);
        if (m_ElapsedSeconds >= waitLimit)
        {
            Debug.LogError(kTag + " 等待云存档回包超时，当前档案仅在本次运行中使用。");
            ReleaseReadOnly();
        }
        else if (previousElapsed < kReadyTimeoutSeconds && m_ElapsedSeconds >= kReadyTimeoutSeconds)
        {
            Debug.LogWarning(kTag + " 云存档响应较慢，等待当前读取完成。");
        }
    }

    private void HandleFetchFailure(string reason, float retryDelay)
    {
        m_ElapsedSeconds = 0f;
        m_FetchFailureStreak++;
        Debug.LogWarning(kTag + " 读取存档失败 attempt=" + m_FetchFailureStreak + "/" + kMaxFetchFailures +
                         " reason=" + reason);
        if (m_FetchFailureStreak >= kMaxFetchFailures)
        {
            ReleaseAsSuspectedNewAccount();
            return;
        }

        m_NextFetchRetryTime = Time.unscaledTime + retryDelay;
    }

    // JSON 转换统一在主工程完成（热更程序集的 UnityEngine 引用集不含 JsonUtility）。
    public T ParseData<T>(string raw) where T : class
    {
        return ParseData<T>(raw, true);
    }

    private static T ParseData<T>(string raw, bool warn) where T : class
    {
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<T>(raw);
        }
        catch (Exception e)
        {
            if (warn)
            {
                Debug.LogWarning("[GameSaveService] parse save data failed: " + e.Message);
            }

            return null;
        }
    }

    public string SerializeData<T>(T data)
    {
        return data != null ? JsonUtility.ToJson(data) : string.Empty;
    }

    // 下一帧合并保存：本帧内重复修改只保留最终 Root，flush 在下一帧执行一次。
    public void QueueSaveRoot()
    {
        m_FlushQueued = true;
    }

    private static bool TryParseRoot(string rawSave, out GameSaveRoot root)
    {
        root = new GameSaveRoot();
        if (rawSave.Length == 0)
        {
            return true;
        }

        try
        {
            root.m_SaveVersion = 0;
            JsonUtility.FromJsonOverwrite(rawSave, root);
            return root.m_SaveVersion == 1 && IsValidSaveSection(root.m_LobbyJson) && IsValidSaveSection(root.m_GuideJson);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsValidSaveSection(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return true;
        }

        var section = new SaveSectionVersion();
        JsonUtility.FromJsonOverwrite(json, section);
        return section.m_SaveVersion == 1;
    }

    private static void NormalizeRoot(GameSaveRoot root)
    {
        if (root.m_AudioBusVolumes == null || root.m_AudioBusVolumes.Length != 4)
        {
            root.m_AudioBusVolumes = new float[4];
            for (int i = 0; i < root.m_AudioBusVolumes.Length; i++)
            {
                root.m_AudioBusVolumes[i] = -1f;
            }
        }

        if (root.m_LobbyJson == null)
        {
            root.m_LobbyJson = string.Empty;
        }

        if (root.m_GuideJson == null)
        {
            root.m_GuideJson = string.Empty;
        }
    }

    private void FlushIfNeeded()
    {
        if (m_InFlightData != null)
        {
            CheckInFlightSave();
            return;
        }

        if (!m_FlushQueued)
        {
            return;
        }

        // 服务级上传闸门：就绪前的 queued 数据是默认/半默认档，绝不发往平台（不依赖调用方自查）。
        if (!IsReady)
        {
            m_FlushQueued = false;
            if (!m_PreReadyFlushDroppedLogged)
            {
                m_PreReadyFlushDroppedLogged = true;
                Debug.LogWarning(kTag + " dropped pre-ready flush; callers must gate writes on IsReady.");
            }

            return;
        }

        // 疑似新号会话：云端是否有档未知，上传即赌博。本次会话所有自动写入一律丢弃。
        if (m_UploadSuppressed)
        {
            m_FlushQueued = false;
            if (!m_UploadSuppressLogged)
            {
                m_UploadSuppressLogged = true;
                Debug.LogWarning(kTag + " 当前档案未获云端写入授权，已忽略自动保存请求。");
            }

            return;
        }

        if (Time.unscaledTime < m_NextFlushRetryTime)
        {
            return;
        }

        if (!ConfirmSaveAccount())
        {
            m_NextFlushRetryTime = Time.unscaledTime + 1f;
            return;
        }

        m_FlushQueued = false;
        var data = SerializeData(Root);
        var result = YoudooAPI.PlatformService.SetGameSave(data);
        if (result.Success && result.Value)
        {
            m_InFlightData = data;
            m_InFlightRetryAt = Time.unscaledTime + m_SaveAcknowledgmentTimeoutSeconds;
            m_NextCloudReadbackAt = Time.unscaledTime + m_CloudReadbackIntervalSeconds;
            return;
        }

        if (Application.platform != RuntimePlatform.Android)
        {
            // 编辑器没有平台存档能力：只提示一次，不重试、不刷屏。
            if (!m_PlatformSaveUnavailableLogged)
            {
                m_PlatformSaveUnavailableLogged = true;
                GameSdkLog.Info(kTag + " platform save unavailable on editor: " + result.Message);
            }

            return;
        }

        Debug.LogWarning(kTag + " SetPlatformGameSave failed, will retry: " + result.Message);
        m_FlushQueued = true;
        m_NextFlushRetryTime = Time.unscaledTime + 1f;
    }

    // 连续拉取失败达到上限：按新账号放行（疑似空档），同时抑制本次会话上传。
    private void ReleaseAsSuspectedNewAccount()
    {
        Debug.LogError(kTag + " fetch failed/no-result " + m_FetchFailureStreak
            + " times in a row; cloud save presence unknown. Releasing as new account to unblock login,"
            + " auto-upload suppressed for this session.");
        ReleaseReadOnly();
    }

    private void ReleaseReadOnly()
    {
        m_UploadSuppressed = true;
        m_UploadSuppressLogged = false;
        MarkReady(string.Empty);
    }

    private void MarkReady(string rawSave)
    {
        if (IsReady)
        {
            return;
        }

        if (!TryParseRoot(rawSave, out var root))
        {
            Debug.LogError(kTag + " 云存档内容无效或版本不支持，已保护原档案。length=" + rawSave.Length);
            m_UploadSuppressed = true;
            root = new GameSaveRoot();
        }

        NormalizeRoot(root);
        Root = root;
        RawSave = rawSave;
        m_ConfirmedCloudSave = rawSave;
        IsReady = true;
        m_FetchFailureStreak = 0;
        // 用 LogWarning：GameSdkLog 默认关闭、正式包过滤 Debug.Log，就绪回读是排查丢档的关键线索，必须默认可见。
        Debug.LogWarning(kTag + " ready, rawLength=" + (rawSave != null ? rawSave.Length : 0)
            + ", lobbyJson=" + (Root.m_LobbyJson != null ? Root.m_LobbyJson.Length : 0)
            + ", guideJson=" + (Root.m_GuideJson != null ? Root.m_GuideJson.Length : 0));
        SaveDataReady?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
