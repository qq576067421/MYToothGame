/*
作者：Ting
创建时间：2025.10.07
描述：AndroidServerInfo 核心基类 - 插件初始化、生命周期管理、消息路由。
通常这是一个单例，整个游戏只需一个即可。

模块化拆分说明（partial class）：
  AndroidServerInfo.cs              ← 本文件：核心（字段 + 生命周期 + 消息路由）
  AndroidServerInfo.InputConfig.cs  ← 输入配置（SDK 或 键盘）- 编辑器可选择输入模式
  AndroidServerInfo.Bluetooth.cs    ← 蓝牙 / 陀螺仪 / 震动 / LED / 音效
  AndroidServerInfo.Audio.cs        ← 音频录制 + 音频播放
  AndroidServerInfo.Device.cs       ← 设备服务（InputDevice）
  AndroidServerInfo.FrameInfo.cs    ← 骨骼帧数据
  AndroidServerInfo.FaceRecognition.cs ← 人脸识别 + 距离检测
  AndroidServerInfo.IYMS.cs         ← 桌面服务（账户 / 排行榜 / 存档 / 购买 / 上报 / 模型选择 / 系统信息 / 测试）
  AndroidServerInfo.Screen.cs       ← 截图 + 录屏
  AndroidServerInfo.NFC.cs          ← NFC
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using YouDooSDK.Utils;
using static YouDooSDKConstants;

public partial class AndroidServerInfo : MonoSingleton<AndroidServerInfo>
{
    // true表示帧数据的服务器是否已经绑定
    public bool isFrameInfoServerIsConnet = false;

    protected AndroidJavaObject _pluginInstance;


    #region 生命周期
    /// <summary>
    /// 生命周期对应的内容
    /// </summary>
    protected override void OnAwake()
    {
        Debug.Log("AndroidServerInfo 父类 OnAwake 被调用 ");
        InitUnityDataTransfer();
        base.OnAwake();
    }

    protected virtual void Update()
    {
        if(IsSDKMode) Bluetooth?.Tick();
    }

    private void InitUnityDataTransfer()
    {
        if (_pluginInstance != null)
        {
            Debug.Log("Youdoo_ApplicationActivityLifecycleCallbacks  C#的代码  已经初始化");
            return;
        }
        try
        {
            Debug.Log($"蓝牙  Youdoo_ApplicationActivityLifecycleCallbacks  C#的代码  initUnityDataTransfer 初始化开始 ");
            AndroidJavaClass dataTransferClass = new AndroidJavaClass("com.youdoo.androidLibrary.UnityDataTransfer");
            _pluginInstance = dataTransferClass.CallStatic<AndroidJavaObject>("getInstance");
            if (_pluginInstance == null)
            {
                Debug.LogError("无法获取UnityDataTransfer实例");
                return;
            }
            _pluginInstance.Call("setGameObjectName", gameObject.name); //游戏的名字。 Unity专用
            InitRequiredServices();
        }
        catch (Exception e)
        {
            Debug.LogError($"AndroidServerInfo初始化失败: {e.Message}");
            _pluginInstance = null;
        }
        Debug.Log($"蓝牙 Youdoo_ApplicationActivityLifecycleCallbacks  C#的代码  initUnityDataTransfer 初始化完成 ");
    }

    protected virtual void InitRequiredServices()
    {
        Debug.Log("AndroidServerInfo 的 InitRequiredServices 子类需要覆写 已经初始化 进行需要什么服务就要进行什么服务的绑定");
    }

    /// <summary>
    /// 生命周期对应的内容
    /// 应用状态管理。子类需要根据实际的业务处理一下数据。
    /// </summary>
    protected virtual void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log("AndroidServerInfo 的 OnApplicationPause, 处理业务时子类需要覆写");

        if (_pluginInstance == null)
        {
            Debug.LogWarning("[SDK调用] _pluginInstance为null，无法调用SDK方法");
            return;
        }

        if (pauseStatus)
        {
            _pluginInstance.Call("onPause");
        }
        else
        {
            _pluginInstance.Call("onResume");
        }
    }

    /// <summary>
    /// 生命周期对应的内容
    /// 程序退出之后需要处理的事情。
    /// </summary>
    protected override void OnApplicationQuit()
    {
        Debug.Log("AndroidServerInfo 的 OnApplicationQuit  应用程序退出，开始清理资源");
        CleanupRequiredServices();
        base.OnApplicationQuit();
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    protected virtual void CleanupRequiredServices()
    {
        Debug.Log("AndroidServerInfo 的 Cleanup 子类需要覆写 绑定了什么服务 就要解绑什么服务");
    }
    #endregion

    /////////////////////////以下 写入Speaker音效，CP不存在这些开发内容。 ///////////////////////
    public void WriteSingleAudio(string deviceMac, int audioId, string filename)
    {

        if (_pluginInstance == null)
        {
            Debug.LogError("[SDK调用] _pluginInstance为null");
            return;
        }

        // 注意：参数顺序和方法签名必须完全匹配
        bool result = _pluginInstance.Call<bool>(
            "demo_writeSpeakerAudioFileFromAssets",
            deviceMac,
            audioId,
            filename
        );

        Debug.Log($"写入单个音频结果: {result}");
    }

    /// <summary>
    /// 批量写入多个音频文件
    /// </summary>
    /// <param name="deviceMac">设备MAC地址</param>
    /// <param name="audioMap">音频映射字典，key=音频ID，value=文件名</param>
    public void WriteBatchAudios(string deviceMac, System.Collections.Generic.Dictionary<int, string> audioMap)
    {

        if (_pluginInstance == null)
        {
            Debug.LogError("[SDK调用] _pluginInstance为null");
            return;
        }

        // 将字典转换为字符串格式："1:file1.tab,2:file2.tab,3:file3.tab"
        string audioMapString = "";
        foreach (var kvp in audioMap)
        {
            if (!string.IsNullOrEmpty(audioMapString))
                audioMapString += ",";
            audioMapString += $"{kvp.Key}:{kvp.Value}";
        }

        bool result = _pluginInstance.Call<bool>(
            "demo_writeSpeakerBatchAudiosFromAssets",
            deviceMac,
            audioMapString
        );

        Debug.Log($"批量写入音频结果: {result}");
    }

    /// <summary>
    /// 获取已保存的音频ID列表
    /// </summary>
    public int[] GetAudioIds(string deviceMac)
    {

        if (_pluginInstance == null)
        {
            Debug.LogError("[SDK调用] _pluginInstance为null");
            return new int[0];
        }

        // 注意：返回的是int[]
        return _pluginInstance.Call<int[]>("demo_getSpeakerAudioIds", deviceMac);
    }

    /// <summary>
    /// 获取已保存的音频数量
    /// </summary>
    public int GetAudioCount(string deviceMac)
    {

        if (_pluginInstance == null)
        {
            Debug.LogError("[SDK调用] _pluginInstance为null");
            return 0;
        }

        return _pluginInstance.Call<int>("demo_getSpeakerAudioCount", deviceMac);
    }


    /// <summary>
    /// 完整的使用示例
    /// </summary>
    public void TestAllFeatures(string deviceMac)
    {

        // 1. 写入单个音频
        WriteSingleAudio(deviceMac, 1, "swing8_final.tab");

        // 2. 批量写入音频
        var audioMap = new Dictionary<int, string>
        {
            {1, "swing8_final.tab"},
            {2, "Vocode1_8KHz_Final.tab"},
            {3, "swing8_final.tab"},
            {4, "Vocode1_8KHz_Final.tab"}
        };

        WriteBatchAudios(deviceMac, audioMap);

        // 3. 查询已保存的音频
        int[] audioIds = GetAudioIds(deviceMac);
        if (audioIds != null)
        {
            Debug.Log("已保存的音频ID: " + string.Join(", ", audioIds));
        }

        // 4. 查询音频数量
        int count = GetAudioCount(deviceMac);
        Debug.Log($"已保存音频数量: {count}");
    }

    /////////////////////////以上 写入Speaker音效，CP不存在这些开发内容。///////////////////////
    /// 
    #region 消息路由虚方法
    /// <summary>测试录音提示回调，子类可覆写</summary>
    protected virtual void OnTestRecordTips(string message) { }
    #endregion

    #region 消息路由
    /// <summary>
    /// 消息：android主动推送
    /// 收到消息通知，根据通知类型分发到各模块的 Handle 方法
    /// </summary>
    /// <param name="jsonData"></param>
    private void OnMessageReceived(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            return;
        }
        NotificationData data = JsonUtility.FromJson<NotificationData>(jsonData);
        if (data != null)
        {
            // 直接根据通知码处理
            switch (data.notify)
            {
                case YouDooNotify.BLUETOOTH: //蓝牙信息的通知
                    HandleBluetoothNotifyInfo(data.message);
                    break;
                case YouDooNotify.FRAMEINFOSERVERCALLBACK://骨骼信息的通知
                    HandleFrameInfoServiceNotifyInfo(data.message);
                    break;
                case YouDooNotify.DEVICESERVERCALLBACK: //设备连接的属性。
                    HandleDeviceServiceNotifyInfo(data.message);
                    break;
                case YouDooNotify.TIPS://提示 。当前这个版本还不需要处理。
                    break;
                case YouDooNotify.TEST_RECORDTIPS://提示，测试期间的提示。
                    OnTestRecordTips(data.message);
                    break;
                case YouDooNotify.NFCCALLBACK://NFC的通知
                    HandleNFCServiceNotifyInfo(data.message);
                    break;
                case YouDooNotify.AUDIO_PLAYBACK:
                    break;
                case YouDooNotify.IYMSGAMESERVICECALLBACK://与桌面通讯的服务请通知。
                    HandleIYMSgameServerCallback(data.message);
                    break;
                case YouDooNotify.FACERECOGNITION://2026.01.21人脸识别
                    HandleFaceRecognitionCallback(data.message);
                    break;
                case YouDooNotify.SCREEN_CAPTURE_SHOT://2026.02.05 截图 ,  SDK 4.0  
                    HandleScreenCaptureShotCallback(data.message);
                    break;
                case YouDooNotify.SCREEN_RECORDER://2026.02.05 录屏 ,  SDK 4.0  
                    HandleScreenRecorderCallback(data.message); //录屏的回调
                    break;
                default:
                    Debug.LogWarning($"未知通知类型: {data.notify}");
                    break;
            }
        }
    }
    #endregion
}
