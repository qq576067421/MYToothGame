/*
作者：Ting
创建时间：2025.10.07
修改时间：2026.04.15
描述：AndroidServerInfo 截图/录屏 partial class 模块
修改说明：HandleScreenCaptureShotCallback 和 HandleScreenRecorderCallback 
         从空方法改为完整实现（JSON解析+switch路由+日志），子类不再需要覆写。
*/
using System;
using UnityEngine;
using static YouDooSDKConstants;

public partial class AndroidServerInfo
{
    #region 截屏相关的Android接口
    /// <summary>
    /// 截屏：
    /// 使用截屏功能之前需要初始化就，只需要1次就行。多次调用也无所谓。
    /// </summary>  
    public void InitScreenCaptureHandler()
    {
        _pluginInstance.Call("initScreenCaptureHandler");
    }

    /// <summary>
    /// 截屏：
    /// </summary>
    public void CaptureScreenshot()
    {
        _pluginInstance.Call("captureScreenshot");
    }

    /// <summary>
    /// 截屏：
    /// 把图片转换成视频
    /// </summary>
    public void GenerateVideoFromImages()
    {
        _pluginInstance.Call("generateVideoFromImages");
    }

    /// <summary>
    /// 截屏：
    /// 取消转换
    /// </summary>
    public void CancelVideoGeneration()
    {
        _pluginInstance.Call("cancelVideoGeneration");
    }

    /// <summary>
    /// 截屏：
    /// 清除临时文件夹
    /// </summary>
    public void CaptureClearTempFiles()
    {
        _pluginInstance.Call("captureClearTempFiles");
    }

    /// <summary>
    /// 截屏：
    /// 是否正在转换视频
    /// </summary>
    public bool IsGeneratingVideo()
    {
        return _pluginInstance.Call<bool>("isGeneratingVideo");
    }


    /// <summary>
    /// 截屏：
    /// 是否是正在截屏 
    /// </summary>
    public bool IsCapturing()
    {
        return _pluginInstance.Call<bool>("isCapturing");
    }
    /// <summary>
    /// 截屏：
    /// 取消截屏 
    /// </summary>
    public void CancelCapture()
    {
        _pluginInstance.Call("cancelCapture");
    }
    #endregion

    #region 截屏消息处理
    /// <summary>
    /// 处理屏幕截图的回调。基类完成 JSON 解析和日志输出。
    /// </summary>
    private void HandleScreenCaptureShotCallback(string messageJson)
    {
        try
        {
            Debug.Log($"[截屏]回调: {messageJson}");
            var info = JsonUtility.FromJson<YouDooNotifyScreenCaptureShotTypeInfo>(messageJson);
            if (info == null)
            {
                Debug.LogError("[截屏]解析回调JSON失败: " + messageJson);
                return;
            }

            switch (info.notifyScreenCaptureShotType)
            {
                case YouDooNotifyScreenCaptureShotType.SCT_START:
                    Debug.Log("[截屏]开始截屏");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_SUCCESS:
                    Debug.Log("[截屏]截屏成功");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_ERROR:
                    Debug.LogError($"[截屏]截屏报错: {info.message}");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_GENERATE_VIDEO_ING:
                    Debug.Log("[截屏]正在生成视频中");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_GENERATE_VIDEO_START:
                    Debug.Log("[截屏]开始生成视频");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_GENERATE_VIDEO_PROGRESS_IMAGE:
                    Debug.Log("[截屏]处理图片进度");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_GENERATE_VIDEO_PROGRESS_FRAMEENCODED:
                    Debug.Log("[截屏]处理帧编码进度");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_GENERATE_VIDEO_SUCCESS:
                    Debug.Log("[截屏]生成视频成功: " + info.message);
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_GENERATE_VIDEO_ERROR:
                    Debug.LogError($"[截屏]生成视频失败: {info.message}");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_GENERATE_VIDEO_CANCEL_SUCCESS:
                    Debug.Log("[截屏]取消生成视频成功");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_GENERATE_VIDEO_NO_IMAGE:
                    Debug.LogWarning("[截屏]没有图片可生成视频");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_GENERATE_VIDEO_NOT_INIT:
                    Debug.LogError("[截屏]截图功能未初始化");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_CLEAR_TEMP_FILES_SUCCESS:
                    Debug.Log("[截屏]清理临时文件夹成功");
                    break;
                case YouDooNotifyScreenCaptureShotType.SCT_CANCEL:
                    Debug.Log("[截屏]截图操作已取消");
                    break;
                default:
                    Debug.LogWarning($"[截屏]未知回调类型: {info.notifyScreenCaptureShotType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[截屏]处理回调异常: {ex.Message}");
        }
    }
    #endregion

    #region 录屏相关的Android接口
    /// <summary>
    /// 录屏功能：初始化
    /// </summary>  
    public void InitRecorderManager()
    {
        _pluginInstance.Call("initRecorderManager");
    }

    /// <summary>
    /// 开始录屏
    /// </summary>  
    public void StartScreenRecording()
    {
        _pluginInstance.Call("startScreenRecording");
    }

    /// <summary>
    /// 停止录屏
    /// </summary>  
    public void StopScreenRecording()
    {
        _pluginInstance.Call("stopScreenRecording");
    }

    /// <summary>
    /// 停止录屏
    /// </summary>  
    public void SaveRecording()
    {
        _pluginInstance.Call("saveRecording");
    }

    /// <summary>
    /// 删除录屏
    /// </summary>  
    public void DeleteRecording()
    {
        _pluginInstance.Call("deleteRecording");
    }

    /// <summary>
    /// 清理录屏
    /// </summary>  
    public void RecorderClearTempFiles()
    {
        _pluginInstance.Call("recorderClearTempFiles");
    }

    /// <summary>
    /// 请求悬浮窗权限
    /// </summary>  
    public void RequestOverlayPermission()
    {
        _pluginInstance.Call("requestOverlayPermission");
    }

    /// <summary>
    /// 停止前台服务
    /// </summary>  
    public void StopForegroundService()
    {
        _pluginInstance.Call("stopForegroundService");
    }

    /// <summary>
    ///是否正在录屏
    /// </summary>  
    public bool IsRecording()
    {
        return _pluginInstance.Call<bool>("isRecording");
    }

    /// <summary>
    /// 获得当前的视频的文件路径
    /// </summary>  
    public string GetCurrentTempVideoFile()
    {
        return _pluginInstance.Call<string>("getCurrentTempVideoFile");
    }
    #endregion

    #region 录屏消息处理
    /// <summary>
    /// 处理屏幕录屏的回调。基类完成 JSON 解析和日志输出。
    /// </summary>
    private void HandleScreenRecorderCallback(string messageJson)
    {
        try
        {
            var info = JsonUtility.FromJson<YouDooNotifyScreenRecorderTypeInfo>(messageJson);
            if (info == null)
            {
                Debug.LogError("[录屏]解析回调JSON失败: " + messageJson);
                return;
            }

            switch (info.notifyScreenRecorderType)
            {
                case YouDooNotifyScreenRecorderType.SRT_START:
                    Debug.Log("[录屏]开始录屏");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_RECORDING:
                    Debug.Log("[录屏]正在录制中");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_STOP:
                    Debug.Log("[录屏]停止录屏成功");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_NOT_INIT:
                    Debug.LogError("[录屏]录屏功能未初始化");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_NOT_RECORDING:
                    Debug.LogWarning("[录屏]当前不在录屏中");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_PERMISSION_DENIED:
                    Debug.LogError("[录屏]录屏权限被拒绝");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_PERMISSION_ERROR:
                    Debug.LogError("[录屏]录屏权限申请出错");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_SAVE_ERROR_NO_FILE:
                    Debug.LogError("[录屏]保存失败，找不到视频文件");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_CLEAR_TEMP_FILES_SUCCESS:
                    Debug.Log("[录屏]清理临时文件夹成功");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_SAVED:
                    Debug.Log("[录屏]视频保存成功");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_DELETED:
                    Debug.Log("[录屏]视频删除成功");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_DELETE_ERROR:
                    Debug.LogError($"[录屏]视频删除失败: {info.message}");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_SAVE_START:
                    Debug.Log("[录屏]开始保存视频");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_SAVE_PROGRESS:
                    break;
                case YouDooNotifyScreenRecorderType.SRT_SAVE_COMPLETE:
                    Debug.Log("[录屏]视频保存完成");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_ERROR:
                    Debug.LogError($"[录屏]录屏发生错误: {info.message}");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_MERGE_ERROR:
                    Debug.LogError($"[录屏]视频合并失败: {info.message}");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_MERGE_SUCCESS:
                    Debug.Log("[录屏]视频合并成功");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_MERGE_START:
                    Debug.Log("[录屏]开始合并视频");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_MERGE_PROGRESS:
                    Debug.Log($"[录屏]合并进度: {info.message}");
                    break;
                case YouDooNotifyScreenRecorderType.SRT_PROGRESS:
                    break;
                default:
                    Debug.LogWarning($"[录屏]未知回调类型: {info.notifyScreenRecorderType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[录屏]处理回调异常: {ex.Message}");
        }
    }
    #endregion
}
