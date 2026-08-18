using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using static YouDooSDKConstants;

public class Bluetooth
{
    /// <summary>
    /// 陀螺仪数据存储最大长度，C++层默认为10，通常无需修改。子类可在构造函数中覆盖。
    /// </summary>
    protected int gyroStorageMaxSize = 10;
    private string TAG = "蓝牙 Bluetooth 父类";

    protected bool clearAfterGetT = true; // 默认每次获取完数据之后，清理一次

    /// <summary>
    /// 单主手柄 Demo 的自动恢复开关。
    /// 多手柄业务建议保持 false，通过 UseDevice / UseDevices / UseAllBondedDevices 明确管理设备。
    /// </summary>
    protected bool isNoRCU = false;

    /// <summary>
    /// 应用回到前台时是否刷新蓝牙绑定设备列表。
    /// </summary>
    public bool AutoRefreshBondedDevicesOnResume { get; set; } = true;

    /// <summary>
    /// 应用回到前台时是否按“主手柄”策略清理并恢复设备。
    /// 多手柄业务建议保持 false，避免回前台时错误清理其他手柄。
    /// </summary>
    public bool AutoRecoverMajorDeviceOnResume { get; set; } = false;

    /// <summary>
    /// 设备映射表：标准化后的 MAC 地址 -> HardWareRemoteControl 对象。
    /// 注意：Key 永远使用大写 MAC。
    /// </summary>
    protected Dictionary<string, HardWareRemoteControl> hardWareRemoteControlMap = new Dictionary<string, HardWareRemoteControl>();

    public Dictionary<string, HardWareRemoteControl> HardWareRemoteControlMap { get => hardWareRemoteControlMap; }

    // 来自 android notify 的事件回调
    public event Action<string> OnDeviceAddingAction;
    public event Action<BluetoothNotifyInfo<string>> OnDeviceAddSuccessAction;
    public event Action<BluetoothNotifyInfo<string>> OnDeviceAddFailedAction;
    public event Action<BluetoothNotifyInfo<string>> OnDeviceConnectedAction;
    public event Action<string> OnDeviceUnknownAction;
    public event Action<BluetoothNotifyInfo<string>> OnGyroOpenFailedAction;
    public event Action<BluetoothNotifyInfo<string>> OnMotorOpenFailedAction;
    public event Action<BluetoothNotifyInfo<string>> OnSoundEffectOpenFailedAction;

    ///////////////////////////以下陀螺仪/////////////////////////
    /// <summary>
    /// SDK4.1开始： C++层默认改成 10 。
    /// 单个设备最多存的陀螺仪的数据的长度。SDK是一个队列去存储这个数。
    /// </summary>
    #region DLL导入声明
    [DllImport(YouDooSDKConstants.PLUGIN_NAME)]
    protected static extern void SetGyroStorageMaxSize(int max_size);

    /// <summary>
    /// SDK4.1 开始，这个方法已经没有什么意义了。Android端自动处理了 陀螺仪的数据入库的问题。
    /// </summary>
    /// <param name="device_ids"></param>
    /// <param name="device_count"></param>
    [DllImport(YouDooSDKConstants.PLUGIN_NAME)]
    protected static extern void SetGyroDeviceMACs(IntPtr device_ids, int device_count);

    /// <summary>
    /// 获取单个设备的 数据，是一个数组。单个设备的最大的长度是 max_size;
    /// 当使用Android的UseWhichDevice去设置手柄的时候，应该用这个函数获取到原始的数据。
    /// 陀螺仪的数据是9轴的。 数组的长度就是9。
    /// </summary>
    /// <param name="deviceMAC">这个设备的地址</param>
    /// <param name="clear_after_get">true表示每次获取到之后，C++都会清理数据。</param>
    /// <param name="timestamps">需要保存的数据，保存到这里</param>
    /// <param name="values">需要保存的数据，保存到这里</param>
    /// <returns></returns>
    [DllImport(YouDooSDKConstants.PLUGIN_NAME)]
    public static extern bool GetGyroDataByMAC(string deviceMAC, bool clear_after_get, long[] timestamps, int[] values);

    /// <summary>
    /// 添加单个设备到陀螺仪监控列表
    /// SDK 4.1 开始提供，但是这个函数的使用意义也不大因为Android端自动处理了。即便多次调用也不会有问题。
    /// </summary>
    /// <param name="deviceMAC">设备MAC地址</param>
    [DllImport(YouDooSDKConstants.PLUGIN_NAME)]
    protected static extern void AddGyroDeviceMAC(string deviceMAC);

    /// <summary>
    /// 从陀螺仪监控列表移除单个设备
    /// SDK 4.1 开始提供，但是这个函数的使用意义也不大因为Android端自动处理了。即便多次调用也不会有问题。
    /// </summary>
    /// <param name="deviceMAC">设备MAC地址</param>
    [DllImport(YouDooSDKConstants.PLUGIN_NAME)]
    protected static extern void RemoveGyroDeviceMAC(string deviceMAC);
    #endregion

    //////SDK4.2.2之后增加//////////////////////////////////
    #region SDK4.2.2新增陀螺仪接口
    /// <summary>
    /// 重置当前设备的映射状态（如回中、清理累计量等）
    /// </summary>
    /// <param name="deviceMAC">设备MAC地址</param>
    [DllImport(YouDooSDKConstants.PLUGIN_NAME)]
    public static extern void ResetGyroMappingState(string deviceMAC);

    /// <summary>
    /// 设置指定设备的陀螺仪滤波参数（与Unity Adaptive9AxisFilter一致，每个设备独立配置）
    /// </summary>
    /// <param name="deviceMAC">设备MAC地址</param>
    /// <param name="input">滤波器参数</param>
    /// <returns>true=成功，false=失败</returns>
    [DllImport(YouDooSDKConstants.PLUGIN_NAME)]
    public static extern bool SetGyroFilterParams(string deviceMAC, YouDooSDKConstants.GyroFilterParams input);

    /// <summary>
    /// 获取指定设备的陀螺仪滤波参数  调试的意义更加大。
    /// </summary>
    /// <param name="deviceMAC">设备MAC地址</param>
    /// <param name="output">输出滤波器参数</param>
    /// <returns>true=成功，false=失败</returns>
    [DllImport(YouDooSDKConstants.PLUGIN_NAME)]
    public static extern bool GetGyroFilterParams(string deviceMAC, ref YouDooSDKConstants.GyroFilterParams output);

    /// <summary>
    /// 获取到的数据包括原始的9轴的数据 + 映射到屏幕的X Y + 旋转的数据。
    /// </summary>
    /// <param name="deviceMAC">设备MAC地址</param>
    /// <param name="clear_after_get">获取后是否清理数据</param>
    /// <param name="timestamps">输出：时间戳数组</param>
    /// <param name="gyroData">输出：9轴原始数据 (int[])</param>
    /// <param name="pixel">输出：屏幕坐标数组 [X, Y] (float[])</param>
    /// <param name="quat">输出：四元数数组 (float[])</param>
    [DllImport(YouDooSDKConstants.PLUGIN_NAME)]
    public static extern bool GetComputationDataByMAC(string deviceMAC, bool clear_after_get, long[] timestamps, int[] gyroData, float[] pixel, float[] quat);
    #endregion
    //////SDK4.2.2之后增加//////////////////////////////////
    ///////////////////////// 以上是陀螺仪//////////////////////////////////

    /// <summary>
    /// 标准化设备 MAC。SDK C# 层统一使用大写 MAC 作为字典 Key。
    /// </summary>
    public static string NormalizeDeviceMac(string deviceMac)
    {
        return string.IsNullOrEmpty(deviceMac) ? string.Empty : deviceMac.ToUpper();
    }

    /// <summary>
    /// 显式把设备加入 Native 陀螺仪监控列表。
    /// Android 侧理论上会自动处理，但多手柄场景下这里再注册一次可增强稳定性。
    /// </summary>
    public static void RegisterGyroDeviceMac(string deviceMac)
    {
        string mac = NormalizeDeviceMac(deviceMac);
        if (string.IsNullOrEmpty(mac))
        {
            return;
        }

        AddGyroDeviceMAC(mac);
    }

    /// <summary>
    /// 显式从 Native 陀螺仪监控列表移除设备。
    /// </summary>
    public static void UnregisterGyroDeviceMac(string deviceMac)
    {
        string mac = NormalizeDeviceMac(deviceMac);
        if (string.IsNullOrEmpty(mac))
        {
            return;
        }

        RemoveGyroDeviceMAC(mac);
    }

    /// <summary>
    /// 当前是否已经注册/使用指定设备。
    /// </summary>
    public bool IsUsingDevice(string deviceMac)
    {
        string mac = NormalizeDeviceMac(deviceMac);
        return !string.IsNullOrEmpty(mac) && hardWareRemoteControlMap.ContainsKey(mac);
    }

    /// <summary>
    /// 使用指定手柄。多手柄场景应对每个目标 MAC 分别调用此方法。
    /// 实际控制器对象会在 Android 异步回调 USE_CANCEL_DEVICE_ADD_SUCCESS 后创建。
    /// </summary>
    public virtual void UseDevice(string deviceMac)
    {
        string mac = NormalizeDeviceMac(deviceMac);
        if (string.IsNullOrEmpty(mac))
        {
            Debug.LogWarning($"[{TAG}] UseDevice: deviceMac 为空，忽略");
            return;
        }

        AndroidServerInfo.Instance.UseWhichDevice(mac);
    }

    /// <summary>
    /// 批量使用指定手柄。
    /// </summary>
    public virtual void UseDevices(IEnumerable<string> deviceMacs)
    {
        if (deviceMacs == null)
        {
            Debug.LogWarning($"[{TAG}] UseDevices: deviceMacs 为空，忽略");
            return;
        }

        foreach (string deviceMac in deviceMacs)
        {
            UseDevice(deviceMac);
        }
    }

    /// <summary>
    /// 使用当前已绑定/可见的所有蓝牙设备。
    /// 适合 Demo 或明确知道绑定设备全部都是业务手柄的场景。
    /// </summary>
    public virtual void UseAllBondedDevices()
    {
        YouDooSDKConstants.DeviceList deviceList = AndroidServerInfo.Instance.GetBluetoothBondedDevices();
        if (deviceList?.devices == null || deviceList.devices.Length == 0)
        {
            Debug.LogWarning($"[{TAG}] UseAllBondedDevices: 未获取到已绑定蓝牙设备");
            return;
        }

        Debug.Log($"[{TAG}] UseAllBondedDevices: totalCount={deviceList.totalCount}, usedDeviceCount={deviceList.usedDeviceCount}, devicesLength={deviceList.devices.Length}");

        if (deviceList.usedDeviceCount >= deviceList.devices.Length)
        {
            Debug.Log($"[{TAG}] UseAllBondedDevices: 设备列表已全部处于使用中，跳过重复 UseDevice 请求");
            return;
        }

        bool allRegisteredInUnity = deviceList.devices.All(deviceInfo =>
        {
            string mac = NormalizeDeviceMac(deviceInfo.address);
            return !string.IsNullOrEmpty(mac) && hardWareRemoteControlMap.ContainsKey(mac);
        });

        if (allRegisteredInUnity)
        {
            Debug.Log($"[{TAG}] UseAllBondedDevices: Unity 侧已注册全部设备，跳过重复 UseDevice 请求。当前数量={hardWareRemoteControlMap.Count}");
            return;
        }

        foreach (YouDooSDKConstants.DeviceInfo deviceInfo in deviceList.devices)
        {
            string mac = NormalizeDeviceMac(deviceInfo.address);
            Debug.Log($"[{TAG}] UseAllBondedDevices: 请求使用设备 name={deviceInfo.name}, address={mac}, isUsed={deviceInfo.isUsed}, isConnected={deviceInfo.isConnected}");

            if (!deviceInfo.isConnected)
            {
                Debug.Log($"[{TAG}] UseAllBondedDevices: 跳过离线设备 {mac}");
                continue;
            }

            if (deviceInfo.isUsed || hardWareRemoteControlMap.ContainsKey(mac))
            {
                Debug.Log($"[{TAG}] UseAllBondedDevices: 跳过已使用/已注册设备 {mac}");
                continue;
            }

            UseDevice(mac);
        }
    }

    /// <summary>
    /// 取消使用指定手柄，并从 Unity 侧注册表中移除。
    /// </summary>
    public virtual bool CancelUseDevice(string deviceMac)
    {
        string mac = NormalizeDeviceMac(deviceMac);
        if (string.IsNullOrEmpty(mac))
        {
            Debug.LogWarning($"[{TAG}] CancelUseDevice: deviceMac 为空，忽略");
            return false;
        }

        bool result = AndroidServerInfo.Instance.CancelUseWhichDevice(mac);
        if (result)
        {
            hardWareRemoteControlMap.Remove(mac);
        }

        return result;
    }

    /// <summary>
    /// 尝试获取指定手柄对象。
    /// </summary>
    public bool TryGetController(string deviceMac, out HardWareRemoteControl controller)
    {
        string mac = NormalizeDeviceMac(deviceMac);
        if (string.IsNullOrEmpty(mac))
        {
            controller = null;
            return false;
        }

        return hardWareRemoteControlMap.TryGetValue(mac, out controller);
    }

    /// <summary>
    /// 获取所有已注册手柄。
    /// </summary>
    public IReadOnlyDictionary<string, HardWareRemoteControl> GetControllers()
    {
        return hardWareRemoteControlMap;
    }

    /// <summary>
    /// 使用当前主手柄。仅作为单手柄默认选择策略，不建议作为多手柄业务入口。
    /// </summary>
    public virtual void UseMajorDevice()
    {
        string mac = TryGetMajorDeviceMac();
        if (string.IsNullOrEmpty(mac))
        {
            Debug.LogWarning($"[{TAG}] UseMajorDevice: 未找到主手柄");
            return;
        }

        UseDevice(mac);
    }

    /// <summary>
    /// 获取当前主手柄 MAC。仅作为单手柄默认选择策略。
    /// </summary>
    public virtual string TryGetMajorDeviceMac()
    {
        return CheckAndGetMajorDevice();
    }

    /// <summary>
    /// 在 AndroidServerInfo.Update 中每帧调用。
    /// </summary>
    public virtual void Tick()
    {
        TickMajorDeviceRecovery();
        TickControllers();
    }

    /// <summary>
    /// 单主手柄 Demo 的恢复逻辑。多手柄业务默认不依赖它。
    /// </summary>
    protected virtual void TickMajorDeviceRecovery()
    {
        if (!isNoRCU)
        {
            return;
        }

        if (hardWareRemoteControlMap.Count > 0)
        {
            return;
        }

        string mac = CheckAndGetMajorDevice();
        if (mac != null)
        {
            UseDevice(mac);
            isNoRCU = false;
        }
    }

    /// <summary>
    /// 更新所有已注册手柄。
    /// </summary>
    protected virtual void TickControllers()
    {
        if (hardWareRemoteControlMap.Count <= 0)
        {
            return;
        }

        var controllers = hardWareRemoteControlMap.ToList();
        foreach (var kvp in controllers)
        {
            kvp.Value?.Update();
        }
    }

    public virtual void OnAppPause(bool pauseStatus)
    {
        if (!pauseStatus) // 这个参数竟然比 Start 先到。
        {
            Debug.Log($"[{TAG}] OnApplicationPause: 应用回到前台");

            if (AutoRefreshBondedDevicesOnResume)
            {
                AndroidServerInfo.Instance.RefreshHardBluetoothDeviceList();
            }

            if (AutoRecoverMajorDeviceOnResume && hardWareRemoteControlMap.Count > 0)
            {
                CheckAndRemoveOldMajorDevice();
            }
        }
    }

    /// <summary>
    /// SDK 4.1 增加。遍历删除当前使用中的手柄。
    /// </summary>
    protected void ClearHardWareRemoteControl()
    {
        var deviceList = hardWareRemoteControlMap.ToList();
        foreach (var kvp in deviceList)
        {
            if (kvp.Value != null)
            {
                CancelUseDevice(kvp.Key);
                Debug.Log($"[{TAG}] clearHardWareRemoteControl: 已请求移除 {kvp.Key}，剩余数量 = {hardWareRemoteControlMap.Count}");
            }
        }

        Debug.Log($"[{TAG}] clearHardWareRemoteControl: 清理完成，剩余数量 = {hardWareRemoteControlMap.Count}");
    }

    /// <summary>
    /// 检查移除旧版的主手柄。
    /// 仅作为单主手柄策略使用，多手柄业务不应默认调用。
    /// </summary>
    protected void CheckAndRemoveOldMajorDevice()
    {
        string mac = CheckAndGetMajorDevice();
        if (mac != null)
        {
            mac = NormalizeDeviceMac(mac);

            // 回到前台后，原来的主手柄已经不是刚才的主手柄
            if (!hardWareRemoteControlMap.ContainsKey(mac))
            {
                Debug.Log($"[{TAG}] checkAndRemoveOldMajorDevice 回到前台: 重新设置主设备 {hardWareRemoteControlMap.Count} {mac}");
                ClearHardWareRemoteControl();
                UseDevice(mac);
            }
            else
            {
                Debug.Log($"[{TAG}] checkAndRemoveOldMajorDevice 回到前台: 还是原来的旧设备 {mac} {hardWareRemoteControlMap.Count}，不需要重新设置主设备");
            }
        }
        else
        {
            Debug.LogWarning($"[{TAG}] checkAndRemoveOldMajorDevice 回到前台: 找不到主设备");
            ClearHardWareRemoteControl();
        }
    }

    /// <summary>
    /// 该方法用于检查当前主设备是否在已配对设备列表中，并返回主设备的 MAC 地址。
    /// 1.Android 的标准蓝牙 API 检查有哪些设备是已经绑定的、在线的。
    /// 2.Device 服务拿到当前主手柄的 MAC。
    /// 3.对照一下。
    /// 4.找到对的手柄。
    /// </summary>
    /// <returns></returns>
    protected string CheckAndGetMajorDevice()
    {
        YouDooSDKConstants.DeviceList deviceList = AndroidServerInfo.Instance.GetBluetoothBondedDevices();
        if (deviceList == null)
        {
            Debug.LogWarning($"[{TAG}] checkAndGetMajorDevice 查找主手柄: 未获取到已绑定设备列表");
            return null;
        }

        YouDooSDKConstants.InputDevice inputDevice = AndroidServerInfo.Instance.GetMajorMemInputDevice();
        if (inputDevice == null)
        {
            Debug.Log($"[{TAG}] checkAndGetMajorDevice 查找主手柄: 主手柄还没拿到，大概率是服务还没好。设备列表总数 = {deviceList.totalCount}, 使用中 = {deviceList.usedDeviceCount}");
            return null;
        }

        if (deviceList.devices == null)
        {
            Debug.LogWarning($"[{TAG}] checkAndGetMajorDevice 查找主手柄: 设备列表为空");
            return null;
        }

        for (int i = 0; i < deviceList.devices.Length; i++)
        {
            YouDooSDKConstants.DeviceInfo deviceInfo = deviceList.devices[i];
            if (deviceInfo.address != null && inputDevice.uniqueId != null)
            {
                string upperAddress = NormalizeDeviceMac(deviceInfo.address);
                string upperUniqueId = NormalizeDeviceMac(inputDevice.uniqueId);
                if (upperAddress == upperUniqueId)
                {
                    Debug.Log($"[{TAG}] checkAndGetMajorDevice 查找主手柄：找到 = {upperAddress} 设备列表总数 = {deviceList.totalCount}, 使用中 = {deviceList.usedDeviceCount}");
                    return upperAddress;
                }
            }
        }

        Debug.Log($"[{TAG}] checkAndGetMajorDevice 查找主手柄：没找到，设备列表总数 = {deviceList.totalCount}, 使用中 = {deviceList.usedDeviceCount}");
        return null;
    }

    /// <summary>
    /// SDK 4.1 增加。初始化设备的功能。
    /// </summary>
    /// <param name="newController"></param>
    public void InitializeDevice(HardWareRemoteControl newController)
    {
        if (newController == null)
        {
            Debug.LogError($"[{TAG}] InitializeDevice: newController 为空，无法初始化");
            return;
        }

        newController.DeviceMac = NormalizeDeviceMac(newController.DeviceMac);
        string deviceMac = newController.DeviceMac;

        if (string.IsNullOrEmpty(deviceMac))
        {
            Debug.LogError($"[{TAG}] InitializeDevice: deviceMac 为空，无法初始化");
            return;
        }

        HardWareRemoteControlConfig config = AndroidServerInfo.Instance.GetHardWareRemoteControlConfig();
        if (config == null)
        {
            Debug.LogError($"[{TAG}] InitializeDevice: config 为空，无法获取配置");
            return;
        }

        string configJson = JsonUtility.ToJson(config);
        Debug.Log($"[{TAG}] InitializeDevice: {deviceMac}, config = {configJson}");

        if (config.enableGyro)
        {
            newController.InitGyroItem(gyroStorageMaxSize, clearAfterGetT);
        }

        if (config.enableMotor)
        {
            newController.InitVibrationItem();
        }

        if (config.enableAudio)
        {
            newController.InitAudioItem("");
        }

        // SDK4.1：SDK已经控制返回的是false，原因：游戏中设置不了LED的颜色了。但是这个接口还是保留的，以防万一后续SDK又改了。
        if (config.enableLed)
        {
            newController.InitLedItem();
        }

        if (config.enableSpeaker)
        {
            newController.InitSpeakerItem();
        }

        newController.MarkInitialized();
    }

    // Bluetooth.cs 中添加
    public virtual void HandleBluetoothNotify(string messageJson)
    {
        var typeInfo = JsonUtility.FromJson<BluetoothNotifyTypeInfo>(messageJson);
        if (typeInfo != null)
        {
            //testShowTips(typeInfo, messageJson);  //需要调试的时候可以打开。

            switch (typeInfo.notifyBluetoothType)
            {
                case YouDooNotifyBluetoothType.NEWBONDEVICES: // 有新设备绑定
                    HandleNewBondDevices(messageJson);
                    break;
                case YouDooNotifyBluetoothType.DISCONNECT_BONDEDEVICES: // 有绑定的设备掉线了
                    HandleDisconnectBondedDevices(messageJson);
                    break;
                case YouDooNotifyBluetoothType.UNBONDEDEVICES: // 有设备解除了绑定
                    HandleUnbondDevices(messageJson);
                    break;
                case YouDooNotifyBluetoothType.RECONNECT_BONDEDEVICES: // SDK 4.1 有绑定的设备又连接上了
                    HandleReconnectBondedDevices(messageJson);
                    break;
                case YouDooNotifyBluetoothType.DEVICE_BATTERY_LEVEL: // 获取到电量的回调
                    HandleDeviceBatteryLevel(messageJson);
                    break;
                case YouDooNotifyBluetoothType.GYROSTATE_CHANGED: // 陀螺仪的状态改变
                    HandleGyroStateChanged(messageJson);
                    break;
                case YouDooNotifyBluetoothType.GYROSTATE_ERROR:
                    HandleGyroError(messageJson);
                    break;
                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_ADDING:
                    HandleDeviceAdding(messageJson);
                    break;
                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_ADD_SUCCESS:
                    HandleDeviceAddSuccess(messageJson);
                    break;
                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_ADD_FAILED:
                    HandleDeviceAddFailed(messageJson);
                    break;
                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_CONNECTED: // 已连接/已使用这个手柄，不要再连了
                    HandleDeviceConnected(messageJson);
                    break;
                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_UNKNOWN:
                    HandleDeviceUnknown(messageJson);
                    break;
                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_GYROMANAGER_OPEN_FALSE:
                    HandleGyroOpenFailed(messageJson);
                    break;
                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_MOTORMANAGER_OPEN_FALSE:
                    HandleMotorOpenFailed(messageJson);
                    break;
                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_SETSOUNDEFFECT_OPEN_FALSE:
                    HandleSoundEffectOpenFailed(messageJson);
                    break;
                default:
                    Debug.LogWarning($"[{TAG}] 未知蓝牙通知类型: {typeInfo.notifyBluetoothType}");
                    break;
            }
        }
    }

    #region 蓝牙通知处理方法

    /// <summary>
    /// 绑定了新的设备
    /// </summary>
    /// <param name="messageJson"></param>
    protected virtual void HandleNewBondDevices(string messageJson)
    {
        Debug.Log($"[{TAG}] 有新的绑定设备。{messageJson}");
        var deviceInfo = JsonUtility.FromJson<BluetoothNotifyInfo<DeviceStatusInfo>>(messageJson);
        if (deviceInfo?.message == null)
        {
            return;
        }

        string mac = NormalizeDeviceMac(deviceInfo.message.address);
        deviceInfo.message.address = mac;
        Debug.Log($"[{TAG}] 新的绑定设备: {deviceInfo.message.name} ({mac}) - {deviceInfo.message.status}");

        if (string.IsNullOrEmpty(mac))
        {
            Debug.LogWarning($"[{TAG}] 绑定设备 MAC 为空，忽略本次绑定通知");
            return;
        }

        if (hardWareRemoteControlMap.TryGetValue(mac, out HardWareRemoteControl controller))
        {
            Debug.Log($"[{TAG}] 绑定设备已存在于 Unity 侧注册表，删除旧数据: {mac}");
            hardWareRemoteControlMap.Remove(mac);
            return;
        }

        Debug.Log($"[{TAG}] 绑定设备不在 Unity 侧注册表，重新请求使用设备，等待 ADD_SUCCESS 后加入: {mac}");
        UseDevice(mac);
    }

    /// <summary>
    /// SDK4.1 重构：有绑定的设备掉线了。
    /// </summary>
    protected virtual void HandleDisconnectBondedDevices(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理设备断开连接 {messageJson}");
        if (string.IsNullOrEmpty(messageJson))
        {
            return;
        }

        var deviceInfo = JsonUtility.FromJson<BluetoothNotifyInfo<DeviceStatusInfo>>(messageJson);
        if (deviceInfo?.message == null)
        {
            return;
        }

        string disconnectedMac = NormalizeDeviceMac(deviceInfo.message.address);
        deviceInfo.message.address = disconnectedMac;
        Debug.Log($"[{TAG}] 设备断开: {deviceInfo.message.name} ({disconnectedMac})");

        if (hardWareRemoteControlMap.TryGetValue(disconnectedMac, out HardWareRemoteControl controller))
        {
            Debug.Log($"[{TAG}] 断开的设备是已注册设备，准备设置断开标记: {disconnectedMac}");
            controller.HandleDisconnectBondedDevices(deviceInfo);
            hardWareRemoteControlMap.Remove(disconnectedMac);

            if (!AndroidServerInfo.Instance.CancelUseWhichDevice(disconnectedMac))
            {
                Debug.Log($"[{TAG}] 断开连接失败 : {disconnectedMac} {hardWareRemoteControlMap.Count}");
            }
        }
        else
        {
            Debug.Log($"[{TAG}] 要断开的设备不在已注册列表中: {disconnectedMac}");
        }
    }

    /// <summary>
    /// 有设备解除了绑定
    /// </summary>
    protected virtual void HandleUnbondDevices(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理设备解除绑定 {messageJson}");
        if (string.IsNullOrEmpty(messageJson))
        {
            return;
        }

        var deviceInfo = JsonUtility.FromJson<BluetoothNotifyInfo<DeviceStatusInfo>>(messageJson);
        if (deviceInfo?.message == null)
        {
            return;
        }

        string unbonddMac = NormalizeDeviceMac(deviceInfo.message.address);
        deviceInfo.message.address = unbonddMac;
        Debug.Log($"[{TAG}] 设备解绑: {deviceInfo.message.name} ({unbonddMac})");

        if (hardWareRemoteControlMap.TryGetValue(unbonddMac, out HardWareRemoteControl controller))
        {
            Debug.Log($"[{TAG}] 解绑的设备是已注册设备，准备设置断开标记: {unbonddMac}");
            controller.HandleUnbondDevices(deviceInfo);
            hardWareRemoteControlMap.Remove(unbonddMac);

            if (!AndroidServerInfo.Instance.CancelUseWhichDevice(unbonddMac))
            {
                Debug.Log($"[{TAG}] 断开连接失败 : {unbonddMac} {hardWareRemoteControlMap.Count}");
            }
        }
        else
        {
            Debug.Log($"[{TAG}] 要断开的设备不在已注册列表中: {unbonddMac}");
        }
    }

    /// <summary>
    /// SDK4.1 重构：有绑定的设备又连接上了。
    /// 业务: 游戏的过程中，有个手柄掉线了，过一会又连接上了。这个时候游戏通常需要做一些提示的。
    /// 这个回调的时机是在设备服务连接成功之后的。
    /// </summary>
    /// <param name="messageJson"></param>
    protected virtual void HandleReconnectBondedDevices(string messageJson)
    {
        Debug.Log($"[{TAG}] 有绑定的又连接上了。{messageJson}");
        var deviceInfo = JsonUtility.FromJson<BluetoothNotifyInfo<DeviceStatusInfo>>(messageJson);
        if (deviceInfo?.message == null)
        {
            return;
        }

        string mac = NormalizeDeviceMac(deviceInfo.message.address);
        deviceInfo.message.address = mac;
        Debug.Log($"[{TAG}] 有绑定的又连接上了: {deviceInfo.message.name} ({mac}) - {deviceInfo.message.status}");

        if (string.IsNullOrEmpty(mac))
        {
            Debug.LogWarning($"[{TAG}] 重连设备 MAC 为空，忽略本次重连通知");
            return;
        }

        if (hardWareRemoteControlMap.TryGetValue(mac, out HardWareRemoteControl controller))
        {
            Debug.Log($"[{TAG}] 重连设备已存在于 Unity 侧注册表，仅更新连接状态: {mac}");
            controller.HandleReconnectBondedDevices(deviceInfo);
            return;
        }

        Debug.Log($"[{TAG}] 重连设备不在 Unity 侧注册表，重新请求使用设备，等待 ADD_SUCCESS 后加入: {mac}");
        UseDevice(mac);
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理设备电量信息事件。
    /// </summary>
    protected virtual void HandleDeviceBatteryLevel(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理设备电量信息 {messageJson}");
        var batteryInfo = JsonUtility.FromJson<BluetoothNotifyInfo<BatteryInfo>>(messageJson);
        if (batteryInfo?.message == null)
        {
            return;
        }

        string mac = NormalizeDeviceMac(batteryInfo.message.address);
        batteryInfo.message.address = mac;
        Debug.Log($"设备电量: {batteryInfo.message.name} ({mac}) - {batteryInfo.message.batteryLevel}%");

        if (hardWareRemoteControlMap.TryGetValue(mac, out HardWareRemoteControl controller))
        {
            controller.GetDeviceBatteryLevelHandle(batteryInfo);
        }
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理添加设备成功事件。
    /// 当调用了 AndroidServerInfo.Instance.UseWhichDevice(mac) 之后，会回调。
    /// 代表 C++ 层 / Android 层初始化了这个设备。
    /// </summary>
    protected virtual void HandleDeviceAddSuccess(string messageJson)
    {
        var addSuccessInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
        if (addSuccessInfo == null || string.IsNullOrEmpty(addSuccessInfo.message))
        {
            OnDeviceAddSuccessAction?.Invoke(addSuccessInfo);
            return;
        }

        string mac = NormalizeDeviceMac(addSuccessInfo.message);
        addSuccessInfo.message = mac;
        Debug.Log($"[{TAG}] 添加蓝牙设备成功 游戏引擎控制器记录 A {mac}");

        if (!hardWareRemoteControlMap.TryGetValue(mac, out HardWareRemoteControl controller))
        {
            controller = new HardWareRemoteControl
            {
                DeviceMac = mac
            };
            hardWareRemoteControlMap.Add(mac, controller);
            InitializeDevice(controller);
        }
        else if (controller.GyroItem != null && !controller.GyroItem.IsActive)
        {
            Debug.LogWarning($"[{TAG}] 添加蓝牙设备成功: {mac} 已存在但陀螺仪未激活，重新打开陀螺仪");
            controller.GyroItem.SetGyroActive(true);
        }

        controller.HandleDeviceAddSuccess(addSuccessInfo);
        Debug.Log($"[{TAG}] 添加蓝牙设备成功 游戏引擎控制器记录 B {mac}, 当前数量 = {hardWareRemoteControlMap.Count}");

        OnDeviceAddSuccessAction?.Invoke(addSuccessInfo);
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理设备服务连接事件。
    /// 当设备服务管理器连接成功后，会调用这个方法。
    /// 这个服务的目的就是去获取当前主手柄是哪个。
    /// </summary>
    /// <param name="messageJson"></param>
    public virtual void HandleOnDeviceServiceConnected(string messageJson)
    {
        Debug.Log($"[{TAG}] 设备服务：连接完成 {messageJson}");
        AndroidServerInfo.Instance.RefreshHardBluetoothDeviceList();
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理陀螺仪状态变化事件，例如手柄掉线了、陀螺仪进入休眠。
    /// </summary>
    protected virtual void HandleGyroStateChanged(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理陀螺仪状态变化 {messageJson}");
        var gyroInfo = JsonUtility.FromJson<BluetoothNotifyInfo<GyroStateChangedInfo>>(messageJson);
        if (gyroInfo?.message == null)
        {
            return;
        }

        string mac = NormalizeDeviceMac(gyroInfo.message.deviceMac);
        gyroInfo.message.deviceMac = mac;
        Debug.Log($"陀螺仪状态变化: 设备 {mac} - 新状态 {gyroInfo.message.gyroEnabled}");

        if (hardWareRemoteControlMap.TryGetValue(mac, out HardWareRemoteControl controller))
        {
            controller.HandleGyroStateChanged(gyroInfo);
        }
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理陀螺仪错误事件。通常是手柄坏了。
    /// </summary>
    protected virtual void HandleGyroError(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理陀螺仪错误 {messageJson}");
        var gyroStateError = JsonUtility.FromJson<BluetoothNotifyInfo<GyroStateError>>(messageJson);
        if (gyroStateError?.message == null)
        {
            return;
        }

        string mac = NormalizeDeviceMac(gyroStateError.message.deviceMac);
        gyroStateError.message.deviceMac = mac;

        if (hardWareRemoteControlMap.TryGetValue(mac, out HardWareRemoteControl controller))
        {
            controller.HandleGyroError(gyroStateError);
        }
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理添加设备失败事件。
    /// </summary>
    protected virtual void HandleDeviceAddFailed(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理添加设备失败 {messageJson}");
        var addFailedInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
        if (addFailedInfo != null && !string.IsNullOrEmpty(addFailedInfo.message))
        {
            addFailedInfo.message = NormalizeDeviceMac(addFailedInfo.message);
        }

        OnDeviceAddFailedAction?.Invoke(addFailedInfo);
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理设备已连接事件，重复连接这个手柄。
    /// </summary>
    protected virtual void HandleDeviceConnected(string messageJson)
    {
        Debug.Log($"[{TAG}] 已连接/已使用这个手柄，不要再连了。{messageJson}");
        var connectedInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
        if (connectedInfo != null && !string.IsNullOrEmpty(connectedInfo.message))
        {
            connectedInfo.message = NormalizeDeviceMac(connectedInfo.message);
        }

        OnDeviceConnectedAction?.Invoke(connectedInfo);
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理设备未知状态事件。
    /// </summary>
    protected virtual void HandleDeviceUnknown(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理设备未知状态 {messageJson}");
        OnDeviceUnknownAction?.Invoke(messageJson);
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理打开陀螺仪失败事件。
    /// </summary>
    protected virtual void HandleGyroOpenFailed(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理打开陀螺仪失败 {messageJson}");
        var gyroFailedInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
        if (gyroFailedInfo != null && !string.IsNullOrEmpty(gyroFailedInfo.message))
        {
            gyroFailedInfo.message = NormalizeDeviceMac(gyroFailedInfo.message);

            if (hardWareRemoteControlMap.TryGetValue(gyroFailedInfo.message, out HardWareRemoteControl controller))
            {
                controller.GyroItem?.MarkUnavailable("打开陀螺仪失败");
            }
        }

        OnGyroOpenFailedAction?.Invoke(gyroFailedInfo);
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理打开马达失败事件。
    /// </summary>
    protected virtual void HandleMotorOpenFailed(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理打开马达失败 {messageJson}");
        var motorFailedInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
        if (motorFailedInfo != null && !string.IsNullOrEmpty(motorFailedInfo.message))
        {
            motorFailedInfo.message = NormalizeDeviceMac(motorFailedInfo.message);
        }

        OnMotorOpenFailedAction?.Invoke(motorFailedInfo);
    }

    /// <summary>
    /// SDK4.1 重构：子类可重写此方法处理打开声音效果失败事件。
    /// </summary>
    protected virtual void HandleSoundEffectOpenFailed(string messageJson)
    {
        Debug.Log($"[{TAG}] 处理打开声音效果失败 {messageJson}");
        var soundFailedInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
        if (soundFailedInfo != null && !string.IsNullOrEmpty(soundFailedInfo.message))
        {
            soundFailedInfo.message = NormalizeDeviceMac(soundFailedInfo.message);
        }

        OnSoundEffectOpenFailedAction?.Invoke(soundFailedInfo);
    }

    #endregion

    /// <summary>
    /// SDK4.1 重构：调用 AndroidServerInfo.Instance.UseWhichDevice(mac) 的过程中有一个回调。
    /// </summary>
    protected virtual void HandleDeviceAdding(string messageJson)
    {
        OnDeviceAddingAction?.Invoke(messageJson);
    }



    private void testShowTips(BluetoothNotifyTypeInfo typeInfo, string messageJson)
    {
        string tipMessage = "";

        try
        {
            switch (typeInfo.notifyBluetoothType)
            {
                case YouDooNotifyBluetoothType.DISCONNECT_BONDEDEVICES:
                    var disconnectInfo = JsonUtility.FromJson<BluetoothNotifyInfo<DeviceStatusInfo>>(messageJson);
                    if (disconnectInfo?.message != null)
                    {
                        tipMessage = $"设备已断开连接: {disconnectInfo.message.name} ({disconnectInfo.message.address})";
                    }
                    else
                    {
                        tipMessage = "设备已断开连接";
                    }
                    break;

                case YouDooNotifyBluetoothType.RECONNECT_BONDEDEVICES:
                    var reconnectInfo = JsonUtility.FromJson<BluetoothNotifyInfo<DeviceStatusInfo>>(messageJson);
                    if (reconnectInfo?.message != null)
                    {
                        tipMessage = $"设备已重新连接: {reconnectInfo.message.name} ({reconnectInfo.message.address})";
                    }
                    else
                    {
                        tipMessage = "设备已重新连接";
                    }
                    break;

                case YouDooNotifyBluetoothType.DEVICE_BATTERY_LEVEL:
                    var batteryInfo = JsonUtility.FromJson<BluetoothNotifyInfo<BatteryInfo>>(messageJson);
                    if (batteryInfo?.message != null)
                    {
                        tipMessage = $"{batteryInfo.message.name} 电量: {batteryInfo.message.batteryLevel}%";
                    }
                    else
                    {
                        tipMessage = "设备电量已更新";
                    }
                    break;

                case YouDooNotifyBluetoothType.GYROSTATE_CHANGED:
                    var gyroInfo = JsonUtility.FromJson<BluetoothNotifyInfo<GyroStateChangedInfo>>(messageJson);
                    if (gyroInfo?.message != null)
                    {
                        string status = gyroInfo.message.gyroEnabled ? "可用" : "不可用";
                        tipMessage = $"设备 {gyroInfo.message.deviceMac} 陀螺仪状态: {status}";
                    }
                    else
                    {
                        tipMessage = "陀螺仪状态已改变";
                    }
                    break;

                case YouDooNotifyBluetoothType.GYROSTATE_ERROR:
                    var gyroErrorInfo = JsonUtility.FromJson<BluetoothNotifyInfo<GyroStateError>>(messageJson);
                    if (gyroErrorInfo?.message != null)
                    {
                        tipMessage = $"设备 {gyroErrorInfo.message.deviceMac} 陀螺仪错误";
                    }
                    else
                    {
                        tipMessage = "陀螺仪错误";
                    }
                    break;

                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_ADDING:
                    var addingInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
                    tipMessage = $"正在添加设备... MAC: {addingInfo?.message}";
                    break;

                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_ADD_SUCCESS:
                    var addSuccessInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
                    tipMessage = $"设备添加成功: MAC {addSuccessInfo?.message}";
                    break;

                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_ADD_FAILED:
                    var addFailedInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
                    tipMessage = $"设备添加失败: MAC {addFailedInfo?.message}";
                    break;

                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_CONNECTED:
                    var connectedInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
                    tipMessage = $"设备已连接: MAC {connectedInfo?.message}";
                    break;

                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_UNKNOWN:
                    tipMessage = "未知设备";
                    break;

                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_GYROMANAGER_OPEN_FALSE:
                    var gyroFailInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
                    tipMessage = $"设备 {gyroFailInfo?.message} 陀螺仪打开失败";
                    break;

                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_MOTORMANAGER_OPEN_FALSE:
                    var motorFailInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
                    tipMessage = $"设备 {motorFailInfo?.message} 马达打开失败";
                    break;

                case YouDooNotifyBluetoothType.USE_CANCEL_DEVICE_SETSOUNDEFFECT_OPEN_FALSE:
                    var soundFailInfo = JsonUtility.FromJson<BluetoothNotifyInfo<string>>(messageJson);
                    tipMessage = $"设备 {soundFailInfo?.message} 音效打开失败";
                    break;

                default:
                    tipMessage = $"未知蓝牙通知: {typeInfo.notifyBluetoothType}";
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Bluetooth] 解析通知数据错误: {e.Message}");
            tipMessage = $"蓝牙通知: {typeInfo.notifyBluetoothType}";
        }

        Debug.Log($"[Bluetooth TestTip] {tipMessage}");

        try
        {
            // 调用 AndroidServerInfo 的 ShowToast 显示提示
            if (AndroidServerInfo.Instance != null)
            {
                AndroidServerInfo.Instance.ShowToast(tipMessage);
            }
            else
            {
                Debug.LogWarning("[Bluetooth] AndroidServerInfo.Instance is null");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Bluetooth] ShowToast error: {e.Message}");
        }
    }
}
