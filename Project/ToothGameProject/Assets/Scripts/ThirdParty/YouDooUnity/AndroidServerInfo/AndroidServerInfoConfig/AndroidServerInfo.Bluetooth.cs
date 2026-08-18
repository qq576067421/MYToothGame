/*
作者：Ting
创建时间：2025.10.07
描述：AndroidServerInfo 蓝牙/陀螺仪/震动/LED/音效 partial class 模块
*/
using System;
using UnityEngine;
using static YouDooSDKConstants;

public partial class AndroidServerInfo
{
    public Bluetooth Bluetooth;
    #region 蓝牙相关的Android接口
    /// <summary>
    /// * 初始化蓝牙
    /// * 1.当游戏需要用到 手柄 的操作，则需要初始化。
    ///  * 2.初始化有异步的通知。
    ///  * @Since 2.0
    ///  *
    ///  * 1.SDK4.1开始：此方法已经优化，理论上蓝牙必然返回的是true。除非蓝牙模块是坏的 。
    /// * 2.SDK4.1开始：此方法没有异步回调。
    /// * 3.SDK4.1开始：此方法不会去刷新列表的了。
    ///  * @Since 4.1
    /// </summary>
    /// <returns>true表示成功调用蓝牙初始化</returns>
    protected bool InitHardBluetoothManager()
    {
        return _pluginInstance.Call<bool>("initHardBluetoothManager");
    }

    /// <summary>
    /// 蓝牙管理器:退出
    /// </summary>
    protected void ExitHardBluetoothManager()
    {
        _pluginInstance.Call("exitHardBluetoothManager");
    }

    /// <summary> 
    ///InitHardBluetoothManager调用之后，设置这个游戏需要使用到的手柄的能力，通常只要设置一次。 
    /// </summary>
    /// <param name="enableGyro">使用陀螺仪</param>
    /// <param name="enableMotor">使用马达</param>
    /// <param name="enableAudio">使用音频</param>
    /// <param name="enableLed">使用LED</param>
    /// <param name="enableSpeaker">使用扬声器</param>
    /// <param name="filterType">在使用陀螺仪的时候，这个手柄需要用到的滤波模式</param>
    /// <returns></returns>
    /// @SDK 4.0 及之后的版本，enableLed 的功能是被屏蔽的，原因是游戏不需要使用手柄的LED。 
    public bool SetHardWareRemoteControlConfig(bool enableGyro, bool enableMotor, bool enableAudio, bool enableLed, bool enableSpeaker, int filterType)
    {
        return _pluginInstance.Call<bool>("setHardWareRemoteControlConfig", enableGyro, enableMotor, enableAudio, enableLed, enableSpeaker, filterType);

    }

    /// <summary>
    /// 获取当前道具蓝牙的配置
    /// </summary>
    public HardWareRemoteControlConfig GetHardWareRemoteControlConfig()
    {
        string temp = _pluginInstance.Call<string>("getHardWareRemoteControlConfig");
        if (temp != null)
        {
            Debug.Log($"蓝牙 获取当前道具蓝牙的配置：{temp}");
            HardWareRemoteControlConfig hardWareRemoteControlConfig = JsonUtility.FromJson<HardWareRemoteControlConfig>(temp);
            return hardWareRemoteControlConfig;
        }
        Debug.Log($"蓝牙 这是空的配置： ");
        return new HardWareRemoteControlConfig();
    }

    /// <summary> 
    /// 获取绑定好的蓝牙的设备
    /// </summary>
    /// <returns></returns>
    public DeviceList GetBluetoothBondedDevices()
    {
        string temp = _pluginInstance.Call<string>("getBluetoothBondedDevices");
        if (temp == null)
        {
            Debug.Log($"没有蓝牙的设备");
            return null;
        }
        try
        {
            DeviceList deviceList = JsonUtility.FromJson<DeviceList>(temp);
            return deviceList;
        }
        catch (Exception e)
        {
            Debug.LogError($"蓝牙 解析设备列表异常: {e.Message}\n原始数据: {temp}");
        }
        return null;
    }

    /// <summary>
    /// 调用InitHardBluetoothManager后才能调用这个函数
    /// 作用：更新一下已经绑定蓝牙的设备。
    /// 通常这个方法：可以在初始化的时候调用一下。 也可以在隐藏并且回到主界面之后获取一下（因为蓝牙的状态可能会变化）。
    /// </summary>
    /// <returns></returns>
    public void RefreshHardBluetoothDeviceList()
    {
        _pluginInstance.Call("refreshHardBluetoothDeviceList");
    }

    /// <summary>
    /// SDK 4.0 开始使用这个接口去使用这个设备 这是一个异步的接口。！！
    /// 某些业务是需要自己去计算滤波等功能的。用这个接口。
    /// 获取数据的时候用 GetGyroDataByMAC 能获取到原始数据
    /// </summary>
    /// <param name="deviceId">需要使用这个设备/手柄 </param>
    /// <returns></returns>
    public void UseWhichDevice(string deviceId)
    {
        _pluginInstance.Call("useWhichDevice", deviceId);

    }

    /// <summary>
    ///  SDK 4.0 开始使用这个接口去 取消 使用这个设备  这是一个同步的接口。
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public bool CancelUseWhichDevice(string deviceId)
    {
        return _pluginInstance.Call<bool>("cancelUseWhichDevice", deviceId);
    }
 
    /// <summary> 
    /// 异步获得设备的电量的信息。 
    /// </summary>
    /// <param name="deviceMac"></param>
    public void GetDeviceBatteryLevelAsync(string deviceMac)
    {
        _pluginInstance.Call("getDeviceBatteryLevelAsync", deviceMac);
    }

    /// <summary> 
    /// 刷新所有的设备的电量。数据是异步回调的。
    /// 游戏可以定时调用
    /// </summary>
    public void RefreshAllBatteryLevelsAsync()
    {
        _pluginInstance.Call("refreshAllBatteryLevelsAsync");
    }
    #endregion

    #region 震动相关的Android接口
    /// <summary> 
    /// 通用的短震动 
    /// </summary>
    /// <param name="deviceMac"></param>
    public void VibrationShort(string deviceMac)
    {
        _pluginInstance.Call("vibrationShort", deviceMac);
    }
    /// <summary> 
    /// 通用的长震动
    /// </summary>
    /// <param name="deviceMac"></param>
    public void VibrationLong(string deviceMac)
    {
        _pluginInstance.Call("vibrationLong", deviceMac);
    }
    /// <summary>
    /// 详细的震动
    /// </summary>
    /// <param name="deviceMac">手柄</param>
    /// <param name="motorIntensity">强度的参数</param>
    /// <param name="duration">持续时间,毫秒</param>
    public void VibrationStart(string deviceMac, MotorIntensity motorIntensity, int duration)
    {
        _pluginInstance.Call("vibrationStart", deviceMac, (int)motorIntensity, duration);
    }
    /// <summary>
    /// 停止震动
    /// </summary>
    /// <param name="deviceMac"></param>
    public void VibrationStop(string deviceMac)
    {
        _pluginInstance.Call("vibrationStop", deviceMac);
    }

    /// <summary>
    /// 调整震动的强度
    /// </summary>
    /// <param name="deviceMac">手柄地址</param>
    /// <param name="motorIntensity">震动强度</param>
    public void ChangeVibrationIntensity(string deviceMac, MotorIntensity motorIntensity)
    {
        _pluginInstance.Call("changeVibrationIntensity", deviceMac, (int)motorIntensity);
    }

    /// <summary>
    /// 启用马达 - SDK 4.1 新增
    /// </summary>
    /// <param name="deviceMac">设备mac</param>
    public void EnableMotor(string deviceMac)
    {
        _pluginInstance.Call("enableMotor", deviceMac);
    }

    /// <summary>
    /// 禁用马达 - SDK 4.1 新增
    /// </summary>
    /// <param name="deviceMac">设备mac</param>
    public void DisableMotor(string deviceMac)
    {
        _pluginInstance.Call("disableMotor", deviceMac);
    }
    #endregion

    #region 陀螺仪相关的Android接口
    /// <summary> 
    /// 改变陀螺仪的滤波模式。目前只有None
    /// </summary>
    public void ChangeGyroscopeFilterType(string deviceMac, FilterType filterType)
    {
        _pluginInstance.Call("changeGyroscopeFilterType", deviceMac, (int)filterType);
    }

    /// <summary>
    /// 打开设备陀螺仪
    /// </summary>
    /// <param name="deviceMac">设备mac</param>
    /// <param name="openOrClose">打开或关闭陀螺仪</param>
    public void OpenGyro(string deviceMac, bool openOrClose)
    {
        _pluginInstance.Call("openGyro", deviceMac, openOrClose);
    }

    /// <summary>
    /// 启用陀螺仪 - SDK 4.1 新增
    /// </summary>
    /// <param name="deviceMac">设备mac</param>
    public void EnableGyro(string deviceMac)
    {
        _pluginInstance.Call("enableGyro", deviceMac);
    }

    /// <summary>
    /// 禁用陀螺仪 - SDK 4.1 新增
    /// </summary>
    /// <param name="deviceMac">设备mac</param>
    public void DisableGyro(string deviceMac)
    {
        _pluginInstance.Call("disableGyro", deviceMac);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="deviceMac"></param>
    /// <param name="soundEffect"></param>
    public void SetSoundEffect(string deviceMac, SoundEffect soundEffect)
    {
        _pluginInstance.Call<bool>("setSoundEffect", deviceMac, (int)soundEffect);
    }

    /// <summary>
    /// 启用蜂鸣器 - SDK 4.1 新增
    /// </summary>
    /// <param name="deviceMac">设备mac</param>
    public void EnableSpeaker(string deviceMac)
    {
        _pluginInstance.Call("enableSpeaker", deviceMac);
    }

    /// <summary>
    /// 禁用蜂鸣器 - SDK 4.1 新增
    /// </summary>
    /// <param name="deviceMac">设备mac</param>
    public void DisableSpeaker(string deviceMac)
    {
        _pluginInstance.Call("disableSpeaker", deviceMac);
    }

    /// <summary>
    /// 启用录音 - SDK 4.1 新增
    /// </summary>
    /// <param name="deviceMac">设备mac</param>
    public void EnableAudio(string deviceMac)
    {
        _pluginInstance.Call("enableAudio", deviceMac);
    }

    /// <summary>
    /// 禁用录音 - SDK 4.1 新增
    /// </summary>
    /// <param name="deviceMac">设备mac</param>
    public void DisableAudio(string deviceMac)
    {
        _pluginInstance.Call("disableAudio", deviceMac);
    }
    #endregion



    #region LED和音效相关的Android接口
    ///<summary>
    ///设置Led的等级
    ///</summary>
    /// @SDK 4.0 及之后的版本，Led 的功能是被屏蔽的，原因是游戏不需要使用手柄的LED。 所以此接口无意义了。
    public void SetLedLevel(string deviceMac, LedLevel ledLevel)
    {
        // _pluginInstance.Call<bool>("setLedLevel", deviceMac, (int)ledLevel);
    }

    /// <summary>
    /// 关闭Led
    /// </summary>
    /// @SDK 4.0 及之后的版本 Led的功能是被屏蔽的，原因是游戏不需要使用手柄的LED。 所以此接口无意义了。
    public void CloseLed(string deviceMac)
    {
        // _pluginInstance.Call<bool>("closeLed", deviceMac);
    }


    #endregion



    #region 系统设置 - 蓝牙相关
    /// <summary>
    /// 系统设置：遥控的震动是否可用
    /// </summary>
    public bool IsVibrationEnabled()
    {
        return _pluginInstance.Call<bool>("isVibrationEnabled");
    }

    /// <summary>
    /// 系统设置：遥控的蜂鸣器是否可用
    /// </summary>
    public bool IsSoundEnabled()
    {
        return _pluginInstance.Call<bool>("isSoundEnabled");
    }
    #endregion

    #region 蓝牙消息处理 
    /// <summary>
    /// 消息：蓝牙消息类型过滤
    /// 处理蓝牙通知信息
    /// </summary>
    private void HandleBluetoothNotifyInfo(string messageJson)
    {
        OnBluetoothNotifyInfo(messageJson);
    }

    #endregion

    protected virtual void OnBluetoothNotifyInfo(string messageJson)
    {

    }
}
