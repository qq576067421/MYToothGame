/*
作者：Ting
创建时间：2025.10.07
描述：标准的数据格式，Unity 与 Android  C++ 统一的数据参数。通常不要改。

模块化拆分说明（partial class）：
  YouDooSDKConstants.cs                  ← 本文件：核心常量 + 通知枚举 + 通知数据
  YouDooSDKConstants.Bluetooth.cs        ← 蓝牙相关枚举/类（陀螺仪/震动/LED/音效/设备列表/配置）
  YouDooSDKConstants.FrameInfo.cs        ← 帧信息/骨骼/关键点/手部关键点/脸部关键点
  YouDooSDKConstants.FaceRecognition.cs  ← 人脸识别相关枚举/类
  YouDooSDKConstants.IYMS.cs             ← 桌面服务相关（账户/角色/排行榜/存档/购买/上报/错误返回）
  YouDooSDKConstants.Screen.cs           ← 截屏/录屏相关枚举/类
  YouDooSDKConstants.NFC.cs              ← NFC相关枚举/类
  YouDooSDKConstants.Device.cs           ← 设备服务/InputDevice/HandsCursorData
*/

using System.Collections.Generic;

public static partial class YouDooSDKConstants
{
    public const string PLUGIN_NAME = "youdoo_plugin";

    //没有人物的ID。
    public const int PersonIdNull = -1;

    [System.Serializable]
    public class NotificationData
    {
        public YouDooNotify notify;
        public string message;
    }

    // 在 YouDooNotify 通知。
    public enum YouDooNotify
    {
        BLUETOOTH = 110000,
        TIPS = 120000,
        TEST_RECORDTIPS = 130000,       //测试期间,记录一些FrameInfo数据的通知。
        FRAMEINFOSERVERCALLBACK = 140000, // 新增帧信息服务回调
        DEVICESERVERCALLBACK = 150000,   // 设备的服务
        NFCCALLBACK = 160000,            // NFC的通知
        AUDIO_PLAYBACK = 170000,         // 音频的播放
        IYMSGAMESERVICECALLBACK = 180000, // 与桌面交互数据的callback
        FACERECOGNITION = 190000,        // 2026.01.20人脸识别
        SCREEN_CAPTURE_SHOT = 200000,    // 2026.02.04 截图 SDK 4.0 开始。
        SCREEN_RECORDER = 210000,        // 2026.02.05 录屏 SDK 4.0 开始。
    }
}
