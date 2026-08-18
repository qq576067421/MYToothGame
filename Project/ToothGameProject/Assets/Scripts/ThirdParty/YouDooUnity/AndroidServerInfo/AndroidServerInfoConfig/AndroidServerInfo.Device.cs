/*
作者：Ting
创建时间：2025.10.07
描述：AndroidServerInfo 设备服务 partial class 模块
*/
using System;
using UnityEngine;
using static YouDooSDKConstants;

public partial class AndroidServerInfo
{

    public event Action<string> OnDeviceServiceConnectedAction;
    public event Action<string> OnDeviceServiceDisconnectedAction;
    public event Action<string> OnDeviceServiceBindingAction;
    public event Action<string> OnDeviceServiceBindFailedAction;
    public event Action<InputDevice> OnDeviceInputAction;

    #region 设备相关的Android接口
    /// <summary> 
    /// 使用手柄的场景中，通常设计到N个输入的设备。
    /// 如何区分这些设备呢？需要绑定一个"绑定设备服务"
    /// </summary>
    public void BindInputDeviceService()
    {
        _pluginInstance.Call("bindInputDeviceService");
    }
    /// <summary>
    /// 蓝牙：
    /// 解绑设备服务。
    /// </summary>
    public void UnBindInputDeviceService()
    {
        _pluginInstance.Call("unBindInputDeviceService");
    }

    /// <summary>
    /// 使用我们的输入数据监听
    /// </summary>
    /// <returns></returns>
    public void RegisterEventCallback()
    {
        _pluginInstance.Call("registerEventCallback");
    }
    /// <summary>
    /// 取消我们的输入数据监听 
    /// </summary>
    /// <returns></returns>
    public void UnRegisterEventCallback()
    {
        _pluginInstance.Call("unRegisterEventCallback");
    }


    /// <summary>
    /// 蓝牙：
    /// 通过描述符，找到设备的ID
    /// </summary>
    /// <param name="descriptor"></param>
    public string GetUniqueIdByDescriptor(string descriptor)
    {
        return _pluginInstance.Call<string>("getUniqueIdByDescriptor", descriptor);
    }


    /// <summary>
    /// 根据描述获得设备
    /// </summary>
    /// <param name="descriptor"></param>
    /// <returns></returns>
    public InputDevice GetInputDeviceByDescriptor(string descriptor)
    {
        string json = _pluginInstance.Call<string>("getInputDeviceByDescriptor", descriptor);
        Debug.Log($"getInputDeviceByDescriptor 返回的字符串是：{json}");
        if (string.IsNullOrEmpty(json) || json == "null" || json == "[]")
        {
            Debug.Log("GetMajorMemInputDevice: 没有获取到主手柄设备");
            return null;
        }
        if (!string.IsNullOrEmpty(json))
        {
            return JsonUtility.FromJson<InputDevice>(json);

        }
        return null;
    }


    /// <summary>
    /// 获得主手柄 的描述 这个接口不经常用。 
    /// </summary>
    /// <returns>有null的放回</returns>
    public string GetMajorMemInputDeviceDescriptor()
    {
        return _pluginInstance.Call<string>("getMajorMemInputDeviceDescriptor");
    }

    /// <summary>
    /// 获得主手柄
    /// </summary>
    /// <returns></returns>
    public InputDevice GetMajorMemInputDevice()
    {
        string json = _pluginInstance.Call<string>("getMajorMemInputDevice");
        if (string.IsNullOrEmpty(json) || json == "null")
        {
            // Debug.Log("GetMajorMemInputDevice: 没有获取到主手柄设备");
            return null;
        }
        if (!string.IsNullOrEmpty(json))
        {
            return JsonUtility.FromJson<InputDevice>(json);
        }
        return null;
    }
    /// <summary>
    /// 设备的数组
    /// </summary>
    /// <returns></returns>
    public InputDeviceList GetAllInputDevices()
    {
        string json = _pluginInstance.Call<string>("getAllInputDevices");
        if (string.IsNullOrEmpty(json) || json == "null" || json == "[]")
        {
            // Debug.Log("GetAllInputDevices: 没有获取到任何设备");
            return null;
        }
        // 方案1：包装成对象
        string wrappedJson = "{\"devices\":" + json + "}";
        InputDeviceList list = JsonUtility.FromJson<InputDeviceList>(wrappedJson);
        if (list != null && list.devices != null)
        {
            return list;
        }
        return null;
    }
    #endregion

    #region 设备服务消息处理（virtual）
    /// <summary>
    /// 蓝牙： 
    /// 处理设备服务通知信息
    /// </summary>
    private void HandleDeviceServiceNotifyInfo(string messageJson)
    {
        try
        {
            Debug.Log(" 设备服务的数据  " + messageJson);
            DeviceServiceNotifyInfo deviceServerInfo = JsonUtility.FromJson<DeviceServiceNotifyInfo>(messageJson);
            if (deviceServerInfo != null)
            {
                YouDooNotifyDeviceServiceConnectionType notifyType = (YouDooNotifyDeviceServiceConnectionType)deviceServerInfo.notifyDeviceServiceConnectionType;

                switch (notifyType)
                {
                    case YouDooNotifyDeviceServiceConnectionType.SERVICE_CONNECTED:
                        // 处理设备服务连接成功逻辑
                        OnDeviceServiceConnected(deviceServerInfo.message);
                        break;
                    case YouDooNotifyDeviceServiceConnectionType.SERVICE_DISCONNECTED:
                        // 处理设备服务断开逻辑
                        OnDeviceServiceDisconnected(deviceServerInfo.message);
                        break;
                    case YouDooNotifyDeviceServiceConnectionType.SERVICE_BINDING:
                        // 处理设备服务绑定中逻辑
                        OnDeviceServiceBinding(deviceServerInfo.message);
                        break;
                    case YouDooNotifyDeviceServiceConnectionType.SERVICE_BIND_FAILED:
                        // 处理设备服务绑定失败逻辑
                        OnDeviceServiceBindFailed(deviceServerInfo.message);
                        break;
                    case YouDooNotifyDeviceServiceConnectionType.SERVICE_YD_INPUT:
                        OnDeviceInput(deviceServerInfo.message);
                        break;

                    default:
                        Debug.LogWarning($"未知设备服务连接类型: {notifyType}");
                        break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析设备服务通知失败: {e.Message}");
        }
    }

    /// <summary>
    /// 设备服务：
    /// 设备服务连接成功
    /// </summary>
    protected virtual void OnDeviceServiceConnected(string message)
    {
        OnDeviceServiceConnectedAction?.Invoke(message);
    }

    /// <summary>
    /// 设备服务：
    /// SDK4.1 通常都没必要处理的。
    /// 设备服务断开连接
    /// </summary>
    protected virtual void OnDeviceServiceDisconnected(string message)
    {
        Debug.Log($"设备服务已断开: {message}");
        // 在这里处理设备服务断开后的逻辑
        // 例如：停止设备扫描、清理设备资源等
        OnDeviceServiceDisconnectedAction?.Invoke(message);
    }

    /// <summary>
    /// 设备服务：
    /// SDK4.1  通常都没必要处理的。
    /// 设备服务正在绑定中  
    /// </summary>
    protected virtual void OnDeviceServiceBinding(string message)
    {
        Debug.Log($"设备服务绑定中: {message}");
        // 在这里处理设备服务绑定中的逻辑
        // 例如：显示绑定进度、更新UI状态等

        OnDeviceServiceBindingAction?.Invoke(message);

    }

    /// <summary>
    /// 设备服务：
    /// 设备服务绑定失败
    /// </summary>
    protected virtual void OnDeviceServiceBindFailed(string message)
    {
        Debug.Log($"设备服务绑定失败: {message}");
        // 在这里处理设备服务绑定失败的逻辑
        // 例如：显示错误提示、重试绑定等
        OnDeviceServiceBindFailedAction?.Invoke(message);
    }

    /// <summary>
    /// 设备服务回调数据。
    /// </summary>
    /// <param name="message"></param>
    protected virtual void OnDeviceInput(string message)
    {
        Debug.Log($"设备信息的回调: {message}");
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        InputDevice inputDevice = JsonUtility.FromJson<InputDevice>(message);
        if (inputDevice == null)
        {
            Debug.LogWarning("InputDevice 解析失败！message = " + message);
            return;
        }

        inputDevice.uniqueId = Bluetooth.NormalizeDeviceMac(inputDevice.uniqueId);

        Debug.Log($"===== InputDevice 监听到手柄输入 完整信息 =====");
        Debug.Log($"uniqueId: {inputDevice.uniqueId}");
        Debug.Log($"name: {inputDevice.name}");
        Debug.Log($"descriptor: {inputDevice.descriptor}");
        Debug.Log($"vendor: {inputDevice.vendor}");
        Debug.Log($"product: {inputDevice.product}");
        Debug.Log($"isMajorDevice: {inputDevice.isMajorDevice}");
        Debug.Log($"keycode  按键： {inputDevice.keycode} => {PrintKeycodeName(inputDevice.keycode)}");
        Debug.Log($"ledlevel: {inputDevice.ledlevel}");
        Debug.Log($"=================================");

        OnDeviceInputAction?.Invoke(inputDevice);

        if (Bluetooth == null)
        {
            Debug.LogWarning("Bluetooth 未初始化，无法分发 InputDevice 输入");
            return;
        }

        if (!Bluetooth.HardWareRemoteControlMap.TryGetValue(inputDevice.uniqueId, out HardWareRemoteControl controller))
        {
            Debug.LogWarning($"HardWareRemoteControlMap中不存在设备 {inputDevice.uniqueId}，本次输入忽略");
            return;
        }

        controller.HandleDeviceInput(inputDevice);
    }

    protected string PrintKeycodeName(int keycode)
    {
        return keycode switch
        {
            24 => "音量+",
            25 => "音量-",
            19 => "方向上",
            20 => "方向下",
            21 => "方向左",
            22 => "方向右",
            23 => "确认/中间",
            4 => "返回",
            3 => "Home",
            26 => "电源",
            97 => "背面",
            744 => "录音按键Action_Down",
            758 => "录音按键Action_Up",
            _ => "未知按键",
        };
    }
    #endregion
}
