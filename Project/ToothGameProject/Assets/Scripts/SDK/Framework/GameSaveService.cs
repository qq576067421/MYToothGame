using System;
using UnityEngine;
using YoudooGame.Core;
using YoudooGame.Framework;

using System.Collections.Generic;

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
    public float[] m_AudioBusVolumes = new float[4];
    public int m_AudioMuted = 0;
    public int m_GraphicsAsync = 1;
    public int m_GraphicsAfter = 0;
}

public sealed class GameSaveService : MonoBehaviour
{
    private const string kTag = "[GameSaveService]";
    private const float kReadyTimeoutSeconds = 8f;
    // TODO 存档问题排查期间临时跳过平台存档读写，直接以空档进入游戏；修复后改回 false。
    private const bool kSkipPlatformSave = true;

    // 开始游戏硬前置：只有 SDK 返回了存档获取结果（成功通知，含平台确认无档）
    // 才视为就绪；受理失败、失败通知、超时都只重试请求，绝不自行判定无档。

    public static GameSaveService Instance { get; private set; }

    public static event Action SaveDataReady;

    public bool IsReady { get; private set; }

    public string RawSave { get; private set; }

    public GameSaveRoot Root { get; private set; } = new GameSaveRoot();

    private long m_NotificationSequence;
    private bool m_FetchRequested;
    private float m_ElapsedSeconds;
    private float m_NextFetchRetryTime;
    private float m_NextFlushRetryTime;
    private string m_PendingData;
    private bool m_FlushQueued;
    private bool m_PlatformSaveUnavailableLogged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (kSkipPlatformSave)
        {
            // 临时跳过期间不依赖启动流程是否执行到 Initialize，直接以空档就绪。
            m_FetchRequested = true;
            Debug.LogWarning(kTag + " platform save temporarily skipped, start with empty save");
            MarkReady(string.Empty);
        }
    }

    public void Initialize()
    {
        if (IsReady || m_FetchRequested)
        {
            return;
        }

        m_FetchRequested = true;
        m_ElapsedSeconds = 0f;
        var result = YoudooAPI.GetPlatformGameSave();
        if (result.Success)
        {
            GameSdkLog.Info(kTag + " fetch requested");
            return;
        }

        if (Application.platform == RuntimePlatform.Android)
        {
            // 真机受理失败：SDK 尚未给出结果，保持未就绪并按原因安排重试。
            Debug.LogWarning(kTag + " GetPlatformGameSave not accepted, will retry: " + result.Message);
            m_ElapsedSeconds = 0f;
            m_FetchRequested = false;
            // provider 尚未注册时快速重试（通常发生在框架初始化完成前）。
            float retryDelay = result.Error == ErrorCode.NotInitialized
                ? 0.5f
                : kReadyTimeoutSeconds;
            m_NextFetchRetryTime = Time.unscaledTime + retryDelay;
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
        if (IsReady || m_FetchRequested)
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
        var batch = YoudooAPI.GetPlatformNotificationsSince(m_NotificationSequence);
        m_NotificationSequence = batch.NextSequence;
        if (batch.Items == null)
        {
            return;
        }

        for (int i = 0; i < batch.Items.Length; i++)
        {
            var notification = batch.Items[i].Notification;
            if (notification.Kind == PlatformNotificationKind.GameSaveLoaded)
            {
                if (notification.Success)
                {
                    // SDK 明确返回的获取结果（含空档）是唯一就绪依据。
                    MarkReady(notification.Data as string ?? string.Empty);
                }
                else
                {
                    // SDK 返回了失败结果：不算获取完成，重新发起请求等待下一次结果。
                    Debug.LogWarning(kTag + " GameSaveLoaded failed, retrying: "
                        + notification.Message);
                    m_ElapsedSeconds = 0f;
                    m_FetchRequested = false;
                    m_NextFetchRetryTime = Time.unscaledTime + 1f;
                }

                continue;
            }

            if (notification.Success
                && notification.Kind == PlatformNotificationKind.GameSaveStored)
            {
                GameSdkLog.Info(kTag + " save stored");
            }
        }
    }

    private void PollReadyTimeout()
    {
        if (IsReady || !m_FetchRequested)
        {
            return;
        }

        if (Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        m_ElapsedSeconds += Time.unscaledDeltaTime;
        if (m_ElapsedSeconds >= kReadyTimeoutSeconds)
        {
            // 长时间没有结果只重新发起请求（请求可能未送达），不代替 SDK 做任何判定。
            Debug.LogWarning(kTag + " no result yet, re-requesting GetPlatformGameSave.");
            m_ElapsedSeconds = 0f;
            m_FetchRequested = false;
            m_NextFetchRetryTime = Time.unscaledTime;
        }
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
        m_PendingData = SerializeData(Root);
        m_FlushQueued = true;
    }

    private static GameSaveRoot ParseRoot(string rawSave)
    {
        if (!string.IsNullOrEmpty(rawSave))
        {
            var parsed = ParseData<GameSaveRoot>(rawSave, true);
            if (parsed != null)
            {
                NormalizeRoot(parsed);
                return parsed;
            }
        }

        var fresh = new GameSaveRoot();
        NormalizeRoot(fresh);
        return fresh;
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
        if (!m_FlushQueued)
        {
            return;
        }

        if (kSkipPlatformSave)
        {
            // 跳过拉档期间不向平台写档，避免空档/默认档覆盖云端存档。
            m_FlushQueued = false;
            return;
        }

        if (Time.unscaledTime < m_NextFlushRetryTime)
        {
            return;
        }

        m_FlushQueued = false;
        var result = YoudooAPI.SetPlatformGameSave(m_PendingData);
        if (result.Success)
        {
            return;
        }

        if (result.Error == ErrorCode.NotInitialized
            && Application.platform != RuntimePlatform.Android)
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

    private void MarkReady(string rawSave)
    {
        if (IsReady)
        {
            return;
        }

        IsReady = true;
        RawSave = rawSave;
        Root = ParseRoot(rawSave);
        GameSdkLog.Info(kTag + " ready, rawLength=" + (rawSave != null ? rawSave.Length : 0));
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
