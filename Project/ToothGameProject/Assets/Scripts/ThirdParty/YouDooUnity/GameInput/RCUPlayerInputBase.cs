using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static YouDooSDKConstants;

namespace YouDooUnity
{
    public class Capabilities
    {
        public string deviceDescriptor;
        public int productId;
        public int vecdorId;
        bool isVirtual;
    }

    /// <summary>
    /// 这个类承接的输入事件和设备响应来自游戏业务
    /// </summary>
    public class RCUPlayerInputBase : MonoBehaviour
    {
        [SerializeField] protected PlayerInput playerInput;
        public PlayerInput PlayerInput => playerInput;
        protected InputAction upArrowAction;
        public InputAction UpArrowAction => upArrowAction;
        protected InputAction downArrowAction;
        public InputAction DownArrowAction => downArrowAction;
        protected InputAction leftArrowAction;
        public InputAction LeftArrowAction => leftArrowAction;
        protected InputAction rightArrowAction;
        public InputAction RightArrowAction => rightArrowAction;
        protected InputAction enterAction;
        public InputAction EnterAction => enterAction;
        protected InputAction escapeAction;
        public InputAction EscapeAction => escapeAction;
        protected InputAction backTriggerAction;
        public InputAction BackTriggerAction => backTriggerAction;
        protected InputAction anykeyAction;
        public InputAction AnykeyAction => anykeyAction;
        public YouDooInputDevice YouDooInputDevice;

        public string DeviceMac;
        public string DeviceDescriptor;

        // 来自HardWareRemoteControl调用的事件, 这些事件的注册来自游戏业务
        // *HardWareRemoteControl本身有可能会被删除 所以放在这里
        public event Action<BluetoothNotifyInfo<string>> OnDeviceAddSuccessAction;
        public event Action<BluetoothNotifyInfo<BatteryInfo>> OnGetDeviceBatteryLevelAction;

        public event Action<BluetoothNotifyInfo<DeviceStatusInfo>> OnNewBondDevicesAction;
        public event Action<BluetoothNotifyInfo<DeviceStatusInfo>> OnDisconnectBondedDevicesAction;
        public event Action<BluetoothNotifyInfo<DeviceStatusInfo>> OnUnbondDevicesAction;
        public event Action<BluetoothNotifyInfo<DeviceStatusInfo>> OnReconnectBondedDevicesAction;
        public event Action<BluetoothNotifyInfo<GyroStateChangedInfo>> OnGyroStateChangedAction;
        public event Action<BluetoothNotifyInfo<GyroStateError>> OnGyroErrorAction;
        public event Action<YouDooSDKConstants.InputDevice> OnVolumeUpButtonPress;
        public event Action<YouDooSDKConstants.InputDevice> OnVolumeDownButtonPress;
        public event Action<YouDooSDKConstants.InputDevice> OnHomeButtonPress;
        public event Action<YouDooSDKConstants.InputDevice> OnPowerButtonPress;
        public event Action<YouDooSDKConstants.InputDevice> OnRecordButtonPress;
        public event Action<YouDooSDKConstants.InputDevice> OnRecordButtonRelease;

        protected virtual void Awake()
        {
            InitAction();
        }
        protected virtual void InitAction()
        {
            upArrowAction = playerInput.actions["UpArrow"];
            downArrowAction = playerInput.actions["DownArrow"];
            leftArrowAction = playerInput.actions["LeftArrow"];
            rightArrowAction = playerInput.actions["RightArrow"];
            enterAction = playerInput.actions["Enter"];
            escapeAction = playerInput.actions["Escape"];
            backTriggerAction = playerInput.actions["BackTrigger"];
            anykeyAction = playerInput.actions["Anykey"];

            // 使用确认件来绑定hardware.RCUPlayerInput;
            if(AndroidServerInfo.Instance.IsSDKMode) enterAction.started += SetHardwarePlayerInput;
        }

        public virtual bool SetMacByCapabilities(AndroidServerInfo serverInfo, string capabilities)
        {
            // 解析输入设备的uid
            Capabilities capa = JsonUtility.FromJson<Capabilities>(capabilities);
            if (capa == null)
            {
                DeviceMac = "";
                return false;
            }
            DeviceMac = serverInfo.GetUniqueIdByDescriptor(capa.deviceDescriptor).ToUpper();
            // 设置mac同时设置对应hardware的playerInput
            var hardwareMap = AndroidServerInfo.Instance.Bluetooth.HardWareRemoteControlMap;
            if (!hardwareMap.Keys.Contains(DeviceMac)) return false;
            hardwareMap[DeviceMac].RCUPlayerInput = this;
            return true;
        }

        void SetHardwarePlayerInput(InputAction.CallbackContext context)
        {
            var hardwareMap = AndroidServerInfo.Instance.Bluetooth.HardWareRemoteControlMap;
            if (!hardwareMap.Keys.Contains(DeviceMac)) return;
            hardwareMap[DeviceMac].RCUPlayerInput = this;
        }

        public void HandleDeiceAddSuccessAction(BluetoothNotifyInfo<string> info)
        {
            OnDeviceAddSuccessAction?.Invoke(info);
        }

        public void HandleNewBondDevicesAction(BluetoothNotifyInfo<DeviceStatusInfo> info)
        {
            OnNewBondDevicesAction?.Invoke(info);
        }
        public void HandleGetDeviceBatteryLevelAction(BluetoothNotifyInfo<BatteryInfo> info)
        {
            OnGetDeviceBatteryLevelAction?.Invoke(info);
        }

        public void HandleDisconnectBondedDevicesAction(BluetoothNotifyInfo<DeviceStatusInfo> info)
        {
            OnDisconnectBondedDevicesAction?.Invoke(info);
        }
        public void HandleUnbondDevicesAction(BluetoothNotifyInfo<DeviceStatusInfo> info)
        {
            OnUnbondDevicesAction?.Invoke(info);
        }
        
        public void HandleReconnectBondedDevicesAction(BluetoothNotifyInfo<DeviceStatusInfo> info)
        {
            OnReconnectBondedDevicesAction?.Invoke(info);
        }
        public void HandleGyroStateChangedAction(BluetoothNotifyInfo<GyroStateChangedInfo> info)
        {
            OnGyroStateChangedAction?.Invoke(info);
        }
        public void HandleGyroErrorAction(BluetoothNotifyInfo<GyroStateError> info)
        {
            OnGyroErrorAction?.Invoke(info);
        }
        public void HandleVolumeUpButtonPress(YouDooSDKConstants.InputDevice inputDevice)
        {
            OnVolumeUpButtonPress?.Invoke(inputDevice);
        }
        public void HandleVolumeDownButtonPress(YouDooSDKConstants.InputDevice inputDevice)
        {
            OnVolumeDownButtonPress?.Invoke(inputDevice);
        }
        public void HandleHomeButtonPress(YouDooSDKConstants.InputDevice inputDevice)
        {
            OnHomeButtonPress?.Invoke(inputDevice);
        }
        public void HandlePowerButtonPress(YouDooSDKConstants.InputDevice inputDevice)
        {
            OnPowerButtonPress?.Invoke(inputDevice);
        }
        public void HandleRecordButtonPress(YouDooSDKConstants.InputDevice inputDevice)
        {
            OnRecordButtonPress?.Invoke(inputDevice);
        }
        public void HandleRecordButtonRelease(YouDooSDKConstants.InputDevice inputDevice)
        {
            OnRecordButtonRelease?.Invoke(inputDevice);
        }
    }
}