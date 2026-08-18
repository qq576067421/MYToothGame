using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;
using YouDooSDK.Utils;

namespace YouDooUnity
{
    public class YouDooInputDevice
    {
        public InputDevice Keyboard;
        public InputDevice Gamepad;
    }
    public class RCUPlayerInputManager : MonoSingleton<RCUPlayerInputManager>
    {
        public AndroidServerInfo AndroidServerInfo;
        [SerializeField] int MaximumPlayerInputNumber;
        [SerializeField] GameObject playerInputPrefab;
        RCUPlayerInputBase dispatcher;
        public RCUPlayerInputBase Dispatcher => dispatcher;
        RCUPlayerInputBase[] rcuPlayerInputs;
        public ReadOnlyArray<RCUPlayerInputBase> RCUPlayerInputs => rcuPlayerInputs;
        // List<InputDevice> skyKeyboards = new List<InputDevice>();
        // List<InputDevice> skyGamepads = new List<InputDevice>();
        List<YouDooInputDevice> youDooInputDeviceList = new List<YouDooInputDevice>();
        public bool ChangeSenAvailable;
        public bool AutoRedispatch = true;

        // Start is called before the first frame update
        void Start()
        {
            rcuPlayerInputs = new RCUPlayerInputBase[MaximumPlayerInputNumber];
            StartCoroutine(InitPlayerDeviceCoroutine());
        }

        IEnumerator InitPlayerDeviceCoroutine()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            UpdateSkyDeviceList();
            // 添加Dispatcher
            var go = PlayerInput.Instantiate(playerInputPrefab, MaximumPlayerInputNumber, "RCU", -1);
            go.name = "Dispatcher";
            go.transform.parent = transform;
            dispatcher = go.GetComponent<RCUPlayerInputBase>();
            yield return new WaitUntil(() => { return dispatcher.PlayerInput.user.valid; });
            // 绑定所有设备给dispather
            foreach (var device in InputSystem.devices)
            {
                try
                {
                    InputUser.PerformPairingWithDevice(device, dispatcher.PlayerInput.user, InputUserPairingOptions.None);
                }
                catch (SystemException ex)
                {
                    Debug.LogError($"绑定玩家手柄分配器失败, 错误: {ex.Message}");
                }
            }

            // 添加player input
            for (int i = 0; i < MaximumPlayerInputNumber; ++i)
            {
                go = PlayerInput.Instantiate(playerInputPrefab, i, "RCU", -1);
                go.name = $"Player{i}Input";
                go.transform.parent = transform;
                rcuPlayerInputs[i] = go.GetComponent<RCUPlayerInputBase>();

                // 等待user生效
                yield return new WaitUntil(() => { return rcuPlayerInputs[i].PlayerInput.user.valid; });
            }
            SubScribleEvent();
        }

        public void SubScribleEvent()
        {
            Dispatcher.EnterAction.performed += RenderAPI.OnEnterAction;
            Dispatcher.UpArrowAction.performed += RenderAPI.OnUpArrowPressed;
            Dispatcher.DownArrowAction.performed += RenderAPI.OnDownArrowPressed;   
            Dispatcher.LeftArrowAction.performed += RenderAPI.OnLeftArrowPressed;
            Dispatcher.RightArrowAction.performed += RenderAPI.OnRightArrowPressed;
            Dispatcher.EscapeAction.performed += RenderAPI.OnEscapePressed;
        }

        private void OnDisable()
        {
            DisSubScrible();
        }

        public void DisSubScrible()
        {
            Dispatcher.EnterAction.performed -= RenderAPI.OnEnterAction;
            Dispatcher.UpArrowAction.performed -= RenderAPI.OnUpArrowPressed;
            Dispatcher.DownArrowAction.performed -= RenderAPI.OnDownArrowPressed;   
            Dispatcher.LeftArrowAction.performed -= RenderAPI.OnLeftArrowPressed;
            Dispatcher.RightArrowAction.performed -= RenderAPI.OnRightArrowPressed;
            Dispatcher.EscapeAction.performed -= RenderAPI.OnEscapePressed;
        }

        /// <summary>
        /// 将指定 MAC 的手柄绑定到指定玩家输入。
        /// 多手柄业务应优先使用该接口；SetMajorDevice 仅作为单主手柄默认策略保留。
        /// </summary>
        public bool BindPlayerToDevice(int playerIndex, string deviceMac)
        {
            if (rcuPlayerInputs == null || playerIndex < 0 || playerIndex >= rcuPlayerInputs.Length)
            {
                Debug.LogError($"绑定玩家手柄失败, playerIndex 越界: {playerIndex}");
                return false;
            }

            string mac = Bluetooth.NormalizeDeviceMac(deviceMac);
            if (string.IsNullOrEmpty(mac))
            {
                Debug.LogError("绑定玩家手柄失败, deviceMac 为空");
                return false;
            }

            var map = AndroidServerInfo.Instance.Bluetooth.HardWareRemoteControlMap;
            if (!map.TryGetValue(mac, out HardWareRemoteControl hardware))
            {
                Debug.LogWarning($"绑定玩家手柄暂未完成, HardWareRemoteControlMap中没有 {mac}，等待设备注册完成后再绑定。");
                return false;
            }

            hardware.RCUPlayerInput = rcuPlayerInputs[playerIndex];
            rcuPlayerInputs[playerIndex].DeviceMac = mac;
            return true;
        }

        /// <summary>
        /// 设置主手柄。
        /// 仅作为单手柄默认绑定策略：主手柄 -> Player0。
        /// 多手柄业务请使用 BindPlayerToDevice。
        /// </summary>
        public void SetMajorDevice()
        {
            string majorDeviceDescriptor = AndroidServerInfo.Instance.GetMajorMemInputDeviceDescriptor();
            YouDooInputDevice majorYouDooDevice = null;

            foreach (var device in youDooInputDeviceList)
            {
                Capabilities capa = JsonUtility.FromJson<Capabilities>(device.Gamepad.description.capabilities);
                if (capa.deviceDescriptor == majorDeviceDescriptor)
                {
                    majorYouDooDevice = device;
                    break;
                }
            }

            if (majorYouDooDevice == null)
            {
                Debug.LogError("在Unity中找不到主手柄的InputDevice!");
                return;
            }

            string majorDeviceMac = Bluetooth.NormalizeDeviceMac(AndroidServerInfo.Instance.GetUniqueIdByDescriptor(majorDeviceDescriptor));
            if (!BindPlayerToDevice(0, majorDeviceMac))
            {
                return;
            }

            int index = -1;
            // 决定绑定哪个sky设备给player
            for (int i = 0; i < youDooInputDeviceList.Count; ++i)
            {
                if (youDooInputDeviceList[i].Gamepad.deviceId == majorYouDooDevice.Gamepad.deviceId)
                {
                    index = i;
                }
            }

            // 获得了原厂手柄
            if (index >= 0)
            {
                // 匹配设备
                PlayerPairDevice(0, youDooInputDeviceList[index]);
            }
        }

        public void DeviceDisconnect(YouDooSDKConstants.BluetoothNotifyInfo<YouDooSDKConstants.DeviceStatusInfo> deviceInfo)
        {
            int playerIndex = -1;
            string deviceMac = deviceInfo.message.address.ToUpper();
            if (string.IsNullOrEmpty(deviceMac)) return;

            for (int i = 0; i < rcuPlayerInputs.Length; ++i)
            {
                string playerDeviceMac = Bluetooth.NormalizeDeviceMac(rcuPlayerInputs[i].DeviceMac);
                if (deviceMac == playerDeviceMac)
                {
                    playerIndex = i;
                    break;
                }
            }
            // 非游玩中手柄, 不触发重连
            if (playerIndex < 0) return;

            // 特殊处理
            // 对于keyboard类型的UnityEngine.InputDevice, 它rcu手柄在断线后会失效然后创建一个新的keyboard, 需要删除现有的
            InputSystem.RemoveDevice(rcuPlayerInputs[playerIndex].YouDooInputDevice.Keyboard);

            // 游戏中的手柄断开连接, 解除对应玩家已配对的设备
            PlayerUnpairAllDevice(playerIndex);
            // 是否自动重新匹配, 否的话需要注册dispatcher的按键事件响应去触发匹配
            if (AutoRedispatch)
            {
                StartCoroutine(DispatchBackupDeivce(playerIndex));
            }
        }

        IEnumerator DispatchBackupDeivce(int playerIndex)
        {
            // 固定间隔时间替补手柄
            float intervalTime = 1.0f;
            bool isDispatched = false;
            while (!isDispatched)
            {
                UpdateSkyDeviceList();
                // 重新绑定
                foreach (var device in youDooInputDeviceList)
                {
                    if (PlayerPairDevice(playerIndex, device))
                    {
                        isDispatched = true;
                        rcuPlayerInputs[playerIndex].SetMacByCapabilities(AndroidServerInfo, device.Gamepad.device.description.capabilities);
                        break;
                    }
                }

                yield return new WaitForSeconds(intervalTime);
            }
        }

        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    break;
                case InputDeviceChange.Disconnected:
                    break;
                case InputDeviceChange.Reconnected:
                    break;

            }
        }

        void UpdateSkyDeviceList()
        {
            // 更新设备列表
            youDooInputDeviceList.Clear();
            List<InputDevice> keyboardList = new List<InputDevice>();
            List<InputDevice> gamepadList = new List<InputDevice>();
            foreach (var device in InputSystem.devices)
            {
                // 根据名字和厂商获得键盘设备
                if (device.description.product.Contains(YouDooSDKConstants.YouDooInputDeviceKeyword))
                {
                    if (device.path.Contains(YouDooSDKConstants.KeyobardDeviceKeyword))
                    {
                        keyboardList.Add(device);
                    }

                    if (device.path.Contains(YouDooSDKConstants.GamepadDeviceKeyword))
                    {
                        gamepadList.Add(device);
                    }
                }
            }

            if (keyboardList.Count != gamepadList.Count)
            {
                Debug.LogError($"Unity识别到的YouDoo设备结构异常, Keyboard数量: {keyboardList.Count} Gamepad数量: {gamepadList.Count}");
                return;
            }

            // 添加合规的deivce
            for (int i = 0; i < keyboardList.Count; ++i)
            {
                var newYoudoo = new YouDooInputDevice
                {
                    Keyboard = keyboardList[i],
                    Gamepad = gamepadList[i]
                };
                youDooInputDeviceList.Add(newYoudoo);
            }
        }

        public void PlayerUnpairDevice(int playerIndex, YouDooInputDevice device)
        {
            rcuPlayerInputs[playerIndex].PlayerInput.user.UnpairDevice(device.Keyboard);
            rcuPlayerInputs[playerIndex].PlayerInput.user.UnpairDevice(device.Gamepad);
            rcuPlayerInputs[playerIndex].YouDooInputDevice = null;
        }

        public void PlayerUnpairAllDevice(int playerIndex)
        {
            rcuPlayerInputs[playerIndex].PlayerInput.user.UnpairDevices();
            rcuPlayerInputs[playerIndex].YouDooInputDevice = null;
        }

        public void AllPlayerUnpairAllDevice()
        {
            for (int i = 0; i < rcuPlayerInputs.Length; ++i)
            {
                PlayerUnpairAllDevice(i);
            }
        }

        public bool PlayerPairDevice(int playerIndex, YouDooInputDevice device)
        {
            if (!IsDevicePairable(device)) return false;
            for (int i = 0; i < rcuPlayerInputs[playerIndex].PlayerInput.devices.Count; ++i)
            {
                if (device.Gamepad.deviceId == rcuPlayerInputs[playerIndex].PlayerInput.devices[i].deviceId)
                {
                    // 设备已绑定给玩家, 返回失败
                    return false;
                }
            }

            try
            {
                InputUser.PerformPairingWithDevice(device.Keyboard, rcuPlayerInputs[playerIndex].PlayerInput.user, InputUserPairingOptions.None);
                InputUser.PerformPairingWithDevice(device.Gamepad, rcuPlayerInputs[playerIndex].PlayerInput.user, InputUserPairingOptions.None);
                rcuPlayerInputs[playerIndex].YouDooInputDevice = device;
            }
            catch (SystemException ex)
            {
                Debug.LogError($"绑定玩家{playerIndex}手柄失败, 错误: {ex.Message}");
                return false;
            }

            return true;
        }

        bool IsDevicePairable(YouDooInputDevice device)
        {
            foreach (var player in rcuPlayerInputs)
            {
                foreach (var d in player.PlayerInput.devices)
                {
                    if (d.deviceId == device.Gamepad.deviceId)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        int GetYouDooDeviceIndex(YouDooInputDevice device)
        {
            int skyIndex = -1;
            if (youDooInputDeviceList == null) return skyIndex;

            for (int i = 0; i < youDooInputDeviceList.Count; ++i)
            {
                if (youDooInputDeviceList[i].Gamepad.deviceId == device.Gamepad.deviceId)
                {
                    skyIndex = i;
                }
            }
            return skyIndex;
        }

        public IReadOnlyList<InputDevice> SearchDeviceByPathName(string name)
        {
            List<InputDevice> searchResultList = new List<InputDevice>();
            foreach (var device in InputSystem.devices)
            {
                // 根据名字和厂商获得键盘设备
                if (device.path.Contains(name))
                {
                    searchResultList.Add(device);
                }
            }
            return searchResultList;
        }
    }
}
