using System;
using UnityEngine;
/// 开发者信息：
/// 创建者：Ting
/// 创建日期：2026年03月30日
/// 版本：1.0 ; 
/// 描述：SDK4.1增加,为了统一管理而增加。
/// 历史命名保留：当前已改为通用多手柄模式，所有手柄地位相同，不再只使用主手柄。
public class BluetoothOnlyUseMajorControllerDemo : Bluetooth
{
    //     /// <summary>
    // /// 陀螺仪滤波参数（默认使用无参构造函数初始化）
    // /// </summary>
    // protected YouDooSDKConstants.GyroFilterParams gyroParams = new YouDooSDKConstants.GyroFilterParams();

    private string TAG = "蓝牙 BluetoothOnlyUseMajorControllerDemo 子类 ";
    public Action<HardWareRemoteControl> OnGyroDataReceived;

    public void Initialize()
    {
        Debug.Log($"[{TAG}] Initialize: 开始初始化，设置陀螺仪存储最大长度为 {gyroStorageMaxSize}");
        SetGyroStorageMaxSize(gyroStorageMaxSize);
    }

    public override void OnAppPause(bool pauseStatus)
    {
        base.OnAppPause(pauseStatus);
    }

    /// <summary>
    /// SDK4.1 重构：处理设备服务连接事件。
    /// 当前所有手柄地位相同：设备服务连接完成后刷新列表，并请求使用所有已绑定手柄。
    /// </summary>
    /// <param name="messageJson"></param>
    public override void HandleOnDeviceServiceConnected(string messageJson)
    {
        Debug.Log($"[{TAG}] 设备服务器已经连接完成，进入通用多手柄模式：请求使用所有已绑定手柄");
        base.HandleOnDeviceServiceConnected(messageJson);
        UseAllBondedDevices();
    }
}
