/*
作者：Ting
创建时间：2025.10.07
描述：AndroidServerInfo 音频录制/音频播放 partial class 模块
*/
using UnityEngine;
using static YouDooSDKConstants;

public partial class AndroidServerInfo
{
    #region 音频录制相关的Android接口
    /// <summary> 
    /// 开始录音 
    /// </summary>
    /// <param name="deviceMac"></param>
    /// <param name="path">每个游戏应该有自己的保存路径</param>
    /// <returns></returns>
    public bool StartAudioRecording(string deviceMac, string path)
    {
        return _pluginInstance.Call<bool>("startAudioRecording", deviceMac, path);
    }
    /// <summary> 
    /// 停止录音
    /// </summary>
    /// <param name="deviceMac"></param>
    public void StopAudioRecording(string deviceMac)
    {
        _pluginInstance.Call("stopAudioRecording", deviceMac);
    }
    /// <summary> 
    /// 获取最近录音的文件的路径。 
    /// </summary>
    /// <param name="deviceMac"></param>
    /// <param name="path"></param>
    public void GetRecentRecordingFilePath(string deviceMac, string path)
    {
        _pluginInstance.Call("getRecentRecordingFilePath", deviceMac, path);
    }
    /// <summary> 
    /// 获取录音的的存储目录
    /// </summary>
    /// <param name="deviceMac"></param>
    /// <param name="path"></param>
    public string GetRecordingDirectory(string deviceMac, string path)
    {
        return _pluginInstance.Call<string>("getRecordingDirectory", deviceMac, path);
    }
    #endregion

    #region 音频播放相关的Android接口
    public bool PlayAudioFile(string filePath)
    {
        Debug.Log($"PlayAudioFile 播放音频文件,路径为{filePath}");
        return _pluginInstance.Call<bool>("playAudioFile", filePath);
    }

    public void StopAudioPlayback()
    {
        Debug.Log($"StopAudioPlayback 停止音频播放");
        _pluginInstance.Call("stopAudioPlayback");
    }

    public void PauseAudioPlayback()
    {
        Debug.Log($"PauseAudioPlayback 暂停音频播放");
        _pluginInstance.Call("pauseAudioPlayback");
    }


    public void ResumeAudioPlayback()
    {
        Debug.Log($"ResumeAudioPlayback 音频恢复播放");
        _pluginInstance.Call("resumeAudioPlayback");
    }

    public bool IsAudioPlaying()
    {
        Debug.Log($"isAudioPlaying 检查是否正在播放音频");
        return _pluginInstance.Call<bool>("isAudioPlaying");
    }

    public string GetCurrentPlaybackFilePath()
    {
        Debug.Log($"GetCurrentPlaybackFilePath 获取当前播放的文件路径");
        return _pluginInstance.Call<string>("getCurrentPlaybackFilePath");
    }

    public AudioPlayState GetAudioPlaybackState()
    {
        Debug.Log($"GetAudioPlaybackState 获取音频播放器的当前状态");
        return (AudioPlayState)_pluginInstance.Call<int>("getAudioPlaybackState");
    }

    public int GetCurrentPlaybackProgress()
    {
        Debug.Log($"GetCurrentPlaybackProgress 获取当前播放进度百分比");
        return _pluginInstance.Call<int>("getCurrentPlaybackProgress");
    }

    public void OnDestroyAudioPlayer()
    {
        Debug.Log($"OnDestroyAudioPlayer 释放资源");
        _pluginInstance.Call("onDestroyAudioPlayer");
    }
    #endregion
}
