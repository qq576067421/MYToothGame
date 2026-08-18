using UnityEngine;
using System.IO;
using System.Collections.Generic;

public enum LogTags
{
    None,
    Login,
    Home,
    Battle,
    //其他标签可以在开发阶段自行添加
}
public enum LogLevel
{
    Info,
    Warning,
    Error
}
public class UDebug
{


    public static bool EnableLogging = true;
    public static LogLevel MinimumLogLevel = LogLevel.Info;
    private static HashSet<int> EnabledTags = new HashSet<int>();
    public static void EnableTag(LogTags tag)
    {
        EnabledTags.Add((int)tag);
    }

    public static void DisableTag(LogTags tag)
    {
        EnabledTags.Remove((int)tag);
    }

    public static void Log(string message, LogTags tag = LogTags.None)
    {
        var level = LogLevel.Info;

        if (!EnableLogging || level < MinimumLogLevel)
        {
            return;
        }
        if (tag != LogTags.None)
        {
            if (EnabledTags.Count > 0 && !EnabledTags.Contains((int)tag))
            {
                return;
            }
        }
        switch (level)
        {
            case LogLevel.Info:
                Debug.Log(message);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(message);
                break;
            case LogLevel.Error:
                Debug.LogError(message);
                break;
        }
    }
    public static void Log(object message, LogTags tag = LogTags.None)
    {
        if (message == null)
        {
            return;
        }
        var level = LogLevel.Info;

        if (!EnableLogging || level < MinimumLogLevel)
        {
            return;
        }
        if (tag != LogTags.None)
        {
            if (EnabledTags.Count > 0 && !EnabledTags.Contains((int)tag))
            {
                return;
            }
        }
        switch (level)
        {
            case LogLevel.Info:
                Debug.Log(message);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(message);
                break;
            case LogLevel.Error:
                Debug.LogError(message);
                break;
        }
    }
    public static void LogWarning(string message, LogTags tag = LogTags.None)
    {
        var level = LogLevel.Warning;

        if (!EnableLogging || level < MinimumLogLevel)
        {
            return;
        }
        if (tag != LogTags.None)
        {
            if (EnabledTags.Count > 0 && !EnabledTags.Contains((int)tag))
            {
                return;
            }
        }
        switch (level)
        {
            case LogLevel.Info:
                Debug.Log(message);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(message);
                break;
            case LogLevel.Error:
                Debug.LogError(message);
                break;
        }
    }
    public static void LogWarning(object message, LogTags tag = LogTags.None)
    {
        if(message == null)
        {
            return;
        }
        var level = LogLevel.Warning;

        if (!EnableLogging || level < MinimumLogLevel)
        {
            return;
        }
        if (tag != LogTags.None)
        {
            if (EnabledTags.Count > 0 && !EnabledTags.Contains((int)tag))
            {
                return;
            }
        }
        switch (level)
        {
            case LogLevel.Info:
                Debug.Log(message);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(message);
                break;
            case LogLevel.Error:
                Debug.LogError(message);
                break;
        }
    }
    public static void LogError(string message, LogTags tag = LogTags.None)
    {
        var level = LogLevel.Error;

        if (!EnableLogging || level < MinimumLogLevel)
        {
            return;
        }
        if (tag != LogTags.None)
        {
            if (EnabledTags.Count > 0 && !EnabledTags.Contains((int)tag))
            {
                return;
            }
        }
        switch (level)
        {
            case LogLevel.Info:
                Debug.Log(message);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(message);
                break;
            case LogLevel.Error:
                Debug.LogError(message);
                break;
        }
    }
    public static void LogError(object message, LogTags tag = LogTags.None)
    {
        if (message == null)
        {
            return;
        }

        var level = LogLevel.Error;

        if (!EnableLogging || level < MinimumLogLevel)
        {
            return;
        }
        if (tag != LogTags.None)
        {
            if (EnabledTags.Count > 0 && !EnabledTags.Contains((int)tag))
            {
                return;
            }
        }
        switch (level)
        {
            case LogLevel.Info:
                Debug.Log(message);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(message);
                break;
            case LogLevel.Error:
                Debug.LogError(message);
                break;
        }
    }
}
