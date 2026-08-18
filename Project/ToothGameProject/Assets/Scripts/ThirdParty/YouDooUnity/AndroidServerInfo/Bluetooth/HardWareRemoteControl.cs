/// 开发者信息：
/// 创建者：Ting
/// 创建日期：2025年11月
/// 版本：1.0
using System;
using UnityEngine;
using YouDooUnity;
using static YouDooSDKConstants;

public class HardWareRemoteControl
{
    private string deviceMac; // 设备的唯一地址。

    /// <summary>
    /// 设备MAC地址。SDK C# 层统一保存为大写 MAC。
    /// </summary>
    public string DeviceMac
    {
        get => deviceMac;
        set => deviceMac = Bluetooth.NormalizeDeviceMac(value);
    }

    private HardWareRemoteControlGyroItem gyroItem;
    private bool isGyroPausedByAudio;
    private HardWareRemoteControlVibrationItem vibrationItem;
    private HardWareRemoteControlAudioItem audioItem;
    private HardWareRemoteControlLedItem ledItem;
    private HardWareRemoteControlSpeakerItem speakerItem;

    public RCUPlayerInputBase RCUPlayerInput;

    /// <summary>
    /// 是否已经完成 Unity 侧能力初始化。
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 当前设备是否处于已连接/可用状态。
    /// </summary>
    public bool IsConnected { get; private set; }

    public bool HasGyro => gyroItem != null;
    public bool HasVibration => vibrationItem != null;
    public bool HasAudio => audioItem != null;
    public bool HasLed => ledItem != null;
    public bool HasSpeaker => speakerItem != null;

    /// <summary>
    /// 陀螺仪控制项
    /// </summary>
    public HardWareRemoteControlGyroItem GyroItem
    {
        get => gyroItem;
        set => gyroItem = value;
    }

    /// <summary>
    /// 震动控制项
    /// </summary>
    public HardWareRemoteControlVibrationItem VibrationItem
    {
        get => vibrationItem;
        set => vibrationItem = value;
    }

    /// <summary>
    /// 音频控制项
    /// </summary>
    public HardWareRemoteControlAudioItem AudioItem
    {
        get => audioItem;
        set => audioItem = value;
    }

    /// <summary>
    /// Led的处理。
    /// </summary>
    public HardWareRemoteControlLedItem LedItem
    {
        get => ledItem;
        set => ledItem = value;
    }

    /// <summary>
    /// 处理的是音频
    /// </summary>
    public HardWareRemoteControlSpeakerItem SpeakerItem
    {
        get => speakerItem;
        set => speakerItem = value;
    }

    public void MarkInitialized()
    {
        IsInitialized = true;
        IsConnected = true;
    }

    public void InitGyroItem(int storageMaxSize, bool clearAfterGetT)
    {
        if (gyroItem == null)
        {
            gyroItem = new HardWareRemoteControlGyroItem();
            gyroItem.InitGyroItem(DeviceMac, storageMaxSize, clearAfterGetT);
            // 默认开启陀螺仪
            gyroItem.SetGyroActive(true);
            gyroItem.ResetGyroMappingState();
        }

        AndroidServerInfo.Instance.RegisterEventCallback();
    }

    public void InitVibrationItem()
    {
        if (vibrationItem == null)
        {
            vibrationItem = new HardWareRemoteControlVibrationItem();
            vibrationItem.InitVibrationItem(DeviceMac);
        }
    }

    public void InitAudioItem(string savePathT)
    {
        if (audioItem == null)
        {
            audioItem = new HardWareRemoteControlAudioItem();
            audioItem.InitAudioItem(DeviceMac, savePathT);
        }
    }

    public void InitLedItem()
    {
        if (ledItem == null)
        {
            ledItem = new HardWareRemoteControlLedItem();
            ledItem.InitLedItem(DeviceMac);
        }
    }

    public void InitSpeakerItem()
    {
        if (speakerItem == null)
        {
            speakerItem = new HardWareRemoteControlSpeakerItem();
            speakerItem.InitSpeakerItem(DeviceMac);
        }
    }

    public void GetDeviceBatteryLevelAsync()
    {
        AndroidServerInfo.Instance.GetDeviceBatteryLevelAsync(deviceMac);
    }

    public void GetDeviceBatteryLevelHandle(BluetoothNotifyInfo<BatteryInfo> info)
    {
        if (info?.message == null) return;
        if (deviceMac != Bluetooth.NormalizeDeviceMac(info.message.address)) return;
        if (RCUPlayerInput == null) return;

        RCUPlayerInput.HandleGetDeviceBatteryLevelAction(info);
    }

    public void HandleDeviceAddSuccess(BluetoothNotifyInfo<string> info)
    {
        IsConnected = true;
        if (RCUPlayerInput == null) return;

        RCUPlayerInput.HandleDeiceAddSuccessAction(info);
    }

    public void HandleNewBondDevices(BluetoothNotifyInfo<DeviceStatusInfo> info)
    {
        if (info?.message == null) return;
        if (deviceMac != Bluetooth.NormalizeDeviceMac(info.message.address)) return;

        IsConnected = false;

        if (RCUPlayerInput == null) return;

        RCUPlayerInput.HandleNewBondDevicesAction(info);
    }

    public void HandleDisconnectBondedDevices(BluetoothNotifyInfo<DeviceStatusInfo> info)
    {
        if (info?.message == null) return;
        if (deviceMac != Bluetooth.NormalizeDeviceMac(info.message.address)) return;

        IsConnected = false;
        gyroItem?.MarkUnavailable("设备掉线");

        if (RCUPlayerInput == null) return;

        RCUPlayerInput.HandleDisconnectBondedDevicesAction(info);
        RCUPlayerInputManager.Instance.DeviceDisconnect(info);
    }

    public void HandleUnbondDevices(BluetoothNotifyInfo<DeviceStatusInfo> info)
    {
        if (info?.message == null) return;
        if (deviceMac != Bluetooth.NormalizeDeviceMac(info.message.address)) return;

        IsConnected = false;
        gyroItem?.MarkUnavailable("设备解除绑定");

        if (RCUPlayerInput == null) return;
        RCUPlayerInput.HandleUnbondDevicesAction(info);
        RCUPlayerInputManager.Instance.DeviceDisconnect(info);
    }

    public void HandleReconnectBondedDevices(BluetoothNotifyInfo<DeviceStatusInfo> info)
    {
        if (info?.message == null) return;
        if (deviceMac != Bluetooth.NormalizeDeviceMac(info.message.address)) return;

        IsConnected = true;

        if (RCUPlayerInput == null) return;

        RCUPlayerInput.HandleReconnectBondedDevicesAction(info);
    }

    public void HandleGyroStateChanged(BluetoothNotifyInfo<GyroStateChangedInfo> info)
    {
        if (info?.message == null) return;
        if (deviceMac != Bluetooth.NormalizeDeviceMac(info.message.deviceMac)) return;

        if (gyroItem != null)
        {
            if (info.message.gyroEnabled)
            {
                gyroItem.MarkAvailable();
            }
            else
            {
                gyroItem.MarkUnavailable("陀螺仪状态变为不可用");
            }
        }

        if (RCUPlayerInput == null) return;

        RCUPlayerInput.HandleGyroStateChangedAction(info);
    }

    public void HandleGyroError(BluetoothNotifyInfo<GyroStateError> info)
    {
        if (info?.message == null) return;
        if (deviceMac != Bluetooth.NormalizeDeviceMac(info.message.deviceMac)) return;

        gyroItem?.MarkUnavailable(info.message.errorMsg.ToString());

        if (RCUPlayerInput == null) return;

        RCUPlayerInput.HandleGyroErrorAction(info);
    }

    public void StartAudioRecording()
    {
        if (audioItem == null) return;

        // 开启手柄录音功能
        // 因为带宽问题，陀螺仪开启时无法录音，需要先关闭陀螺仪功能。
        if (gyroItem != null && gyroItem.IsActive)
        {
            isGyroPausedByAudio = true;
            gyroItem.SetGyroActive(false);
        }

        AndroidServerInfo.Instance.StartAudioRecording(audioItem.DeviceMac, audioItem.SavePath);
    }

    public void StopAudioRecording()
    {
        // 关闭手柄录音功能。
        // 如果陀螺仪因为开启录音而关闭暂停，再次开启陀螺仪功能。
        if (isGyroPausedByAudio && gyroItem != null)
        {
            gyroItem.SetGyroActive(true);
            isGyroPausedByAudio = false;
        }

        AndroidServerInfo.Instance.StopAudioRecording(deviceMac);
    }

    public void Update()
    {
        if (!IsConnected) return;

        gyroItem?.Update();
    }

    public void HandleDeviceInput(InputDevice inputDevice)
    {
        if (inputDevice == null) return;

        inputDevice.uniqueId = Bluetooth.NormalizeDeviceMac(inputDevice.uniqueId);
        if (inputDevice.uniqueId != deviceMac) return;
        if (RCUPlayerInput == null) return;

        switch (inputDevice.keycode)
        {
            case 24:
                // "音量+";
                RCUPlayerInput.HandleVolumeUpButtonPress(inputDevice);
                break;
            case 25:
                // "音量-";
                RCUPlayerInput.HandleVolumeDownButtonPress(inputDevice);
                break;
            case 19:
                // "方向上";
                break;
            case 20:
                // "方向下";
                break;
            case 21:
                // "方向左";
                break;
            case 22:
                // "方向右";
                break;
            case 23:
                // "确认/中间";
                break;
            case 4:
                // "返回";
                break;
            case 3:
                // "Home";
                RCUPlayerInput.HandleHomeButtonPress(inputDevice);
                break;
            case 26:
                // "电源";
                RCUPlayerInput.HandlePowerButtonPress(inputDevice);
                break;
            case 97:
                // "背面";
                break;
            case 744:
                // "录音按键Action_Down";
                RCUPlayerInput.HandleRecordButtonPress(inputDevice);
                break;
            case 758:
                // "录音按键Action_Up";
                RCUPlayerInput.HandleRecordButtonRelease(inputDevice);
                break;
            default:
                // "未知按键";
                break;
        }
    }
}
