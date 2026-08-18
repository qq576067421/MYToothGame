using System;
using UnityEngine;

namespace BoneSender
{
    public sealed class BoneSenderAndroidServerInfo : AndroidServerInfo
    {
        private const string m_DefaultModelName = "yolov8n_pose_int16_w512xh288_17_20251231.adla";
        private const string m_DefaultCameraId = "0";
        private const int m_DefaultResolutionHeight = 2160;
        private const int m_DefaultResolutionWidth = 3840;

        private static BoneSenderAndroidServerInfo m_Current;

        private readonly BoneSenderBluetoothController m_BluetoothController = new BoneSenderBluetoothController();

        private AndroidModeSelect m_AndroidModeSelect;
        private bool m_HasRequestedSdkInitialization;
        private bool m_HasInitializedSdkServices;
        private bool m_HasLoadedModelConfig;
        private bool m_HasAppliedDefaultMode;
        private bool m_HasRegisteredFrameCallback;
        private bool m_HasCompletedFrameSession;
        private string m_LastFrameSessionInvalidReason = "未开始";

        public static BoneSenderAndroidServerInfo Current => m_Current;

        public bool ReadIsFrameStreamReady()
        {
            return m_HasCompletedFrameSession && m_HasRegisteredFrameCallback && isFrameInfoServerIsConnet;
        }

        public bool BeginSdkInitializationOnce()
        {
            if (!ReadShouldUseSdkRuntime())
            {
                BoneSenderAppLogger.Log("当前不是真机运行环境，继续使用本地模拟骨骼数据");
                return true;
            }

            if (m_HasRequestedSdkInitialization)
            {
                return true;
            }

            if (_pluginInstance == null)
            {
                BoneSenderAppLogger.LogError("SDK插件实例为空，无法开始初始化骨骼采集设备");
                return false;
            }

            try
            {
                m_HasRequestedSdkInitialization = true;
                BoneSenderAppLogger.Log("网络连接成功，开始执行骨骼采集基础设施初始化");

                if (!IsSDKMode)
                {
                    SetInputMode(InputMode.SDK);
                    BoneSenderAppLogger.Log("已切换到 SDK 输入模式");
                }

                if (!EnsureSdkServicesInitialized())
                {
                    BoneSenderAppLogger.LogWarning("骨骼采集基础设施初始化未完成，本次将等待下次恢复机会");
                    return false;
                }

                TryRecoverFrameStreamSession("首次联网成功");
                return true;
            }
            catch (Exception exception)
            {
                m_HasRequestedSdkInitialization = false;
                InvalidateFrameSession("基础设施初始化异常", true);
                BoneSenderAppLogger.LogError("初始化骨骼采集基础设施时出现异常: " + exception.Message);
                return false;
            }
        }

        protected override void OnAwake()
        {
            m_Current = this;
            base.OnAwake();
        }

        protected override void InitRequiredServices()
        {
            Bluetooth = m_BluetoothController;
            BoneSenderAppLogger.Log("SDK插件实例已准备，等待主工程接收端连接成功后再启动骨骼采集设备初始化");
        }

        protected override void OnApplicationPause(bool pauseStatus)
        {
            base.OnApplicationPause(pauseStatus);

            if (!m_HasRequestedSdkInitialization)
            {
                return;
            }

            Bluetooth?.OnAppPause(pauseStatus);

            if (pauseStatus)
            {
                if (m_HasRegisteredFrameCallback)
                {
                    try
                    {
                        bool unregisterResult = UnregisterFrameCallback();
                        BoneSenderAppLogger.Log("应用切到后台，注销骨骼帧回调结果=" + (unregisterResult ? "成功" : "失败"));
                    }
                    catch (Exception exception)
                    {
                        BoneSenderAppLogger.LogWarning("应用切到后台时注销骨骼帧回调出现异常: " + exception.Message);
                    }
                }

                InvalidateFrameSession("应用切到后台", true);
                return;
            }

            BoneSenderAppLogger.Log("应用回到前台，开始恢复骨骼采集会话");
            TryRecoverFrameStreamSession("应用回到前台");
        }

        protected override void CleanupRequiredServices()
        {
            BoneSenderAppLogger.Log("应用退出，开始清理骨骼采集设备相关服务");

            try
            {
                if (_pluginInstance == null)
                {
                    return;
                }

                if (m_HasRegisteredFrameCallback)
                {
                    try
                    {
                        UnregisterFrameCallback();
                    }
                    catch (Exception exception)
                    {
                        BoneSenderAppLogger.LogWarning("注销骨骼帧回调时出现异常: " + exception.Message);
                    }

                    m_HasRegisteredFrameCallback = false;
                }

                if (m_HasInitializedSdkServices)
                {
                    try
                    {
                        UnBindFrameInfoGameService();
                    }
                    catch (Exception exception)
                    {
                        BoneSenderAppLogger.LogWarning("解绑骨骼帧服务时出现异常: " + exception.Message);
                    }

                    try
                    {
                        UnBindInputDeviceService();
                    }
                    catch (Exception exception)
                    {
                        BoneSenderAppLogger.LogWarning("解绑输入设备服务时出现异常: " + exception.Message);
                    }

                    try
                    {
                        ExitHardBluetoothManager();
                    }
                    catch (Exception exception)
                    {
                        BoneSenderAppLogger.LogWarning("退出蓝牙管理器时出现异常: " + exception.Message);
                    }
                }

                _pluginInstance.Call("onDestroy");
                _pluginInstance = null;
            }
            catch (Exception exception)
            {
                BoneSenderAppLogger.LogError("清理骨骼采集设备相关服务失败: " + exception.Message);
            }
            finally
            {
                m_HasRequestedSdkInitialization = false;
                m_HasInitializedSdkServices = false;
                m_HasLoadedModelConfig = false;
                InvalidateFrameSession("应用退出清理", true);
                BoneSenderAppLogger.Log("骨骼采集设备相关服务清理完成");
            }
        }

        protected override void OnDestroy()
        {
            if (m_Current == this)
            {
                m_Current = null;
            }

            base.OnDestroy();
        }

        protected override void OnFrameInfoServiceConnected(string message)
        {
            base.OnFrameInfoServiceConnected(message);
            BoneSenderAppLogger.Log("骨骼帧服务已连接: " + (string.IsNullOrEmpty(message) ? "无附加信息" : message));
            TryRecoverFrameStreamSession("骨骼帧服务重新连接");
        }

        protected override void OnFrameInfoServiceDisconnected(string message)
        {
            base.OnFrameInfoServiceDisconnected(message);
            InvalidateFrameSession("骨骼帧服务断开", true);
            BoneSenderAppLogger.LogWarning("骨骼帧服务已断开: " + (string.IsNullOrEmpty(message) ? "无附加信息" : message));
        }

        protected override void OnFrameInfoServiceBindFailed(string message)
        {
            base.OnFrameInfoServiceBindFailed(message);
            InvalidateFrameSession("骨骼帧服务绑定失败", true);
            BoneSenderAppLogger.LogError("骨骼帧服务绑定失败: " + (string.IsNullOrEmpty(message) ? "无附加信息" : message));
        }

        protected override void OnDeviceServiceConnected(string message)
        {
            base.OnDeviceServiceConnected(message);

            YouDooSDKConstants.InputDeviceList deviceList = null;
            try
            {
                deviceList = GetAllInputDevices();
            }
            catch (Exception exception)
            {
                BoneSenderAppLogger.LogWarning("读取当前输入设备列表失败: " + exception.Message);
            }

            int deviceCount = deviceList?.devices != null ? deviceList.devices.Length : 0;
            BoneSenderAppLogger.Log("输入设备服务已连接，当前可见输入设备数量=" + deviceCount);
            m_BluetoothController.HandleOnDeviceServiceConnected(message);
        }

        protected override void OnDeviceServiceDisconnected(string message)
        {
            base.OnDeviceServiceDisconnected(message);
            BoneSenderAppLogger.LogWarning("输入设备服务已断开: " + (string.IsNullOrEmpty(message) ? "无附加信息" : message));
        }

        protected override void OnDeviceServiceBindFailed(string message)
        {
            base.OnDeviceServiceBindFailed(message);
            BoneSenderAppLogger.LogError("输入设备服务绑定失败: " + (string.IsNullOrEmpty(message) ? "无附加信息" : message));
        }

        protected override void OnBluetoothNotifyInfo(string messageJson)
        {
            try
            {
                m_BluetoothController.HandleBluetoothNotify(messageJson);
            }
            catch (Exception exception)
            {
                BoneSenderAppLogger.LogError("处理蓝牙通知失败: " + exception.Message);
            }
        }

        private bool EnsureSdkServicesInitialized()
        {
            if (m_HasInitializedSdkServices)
            {
                return true;
            }

            BindFrameInfoGameService();
            BoneSenderAppLogger.Log("骨骼采集基础设施初始化：已请求绑定骨骼帧服务");

            BindInputDeviceService();
            BoneSenderAppLogger.Log("骨骼采集基础设施初始化：已请求绑定输入设备服务");

            bool bluetoothInitResult = InitHardBluetoothManager();
            BoneSenderAppLogger.Log("骨骼采集基础设施初始化：蓝牙管理器初始化结果=" + (bluetoothInitResult ? "成功" : "失败"));

            bool hardwareConfigResult = SetHardWareRemoteControlConfig(
                true,
                true,
                false,
                false,
                true,
                (int)YouDooSDKConstants.FilterType.NONE);
            BoneSenderAppLogger.Log(
                "骨骼采集基础设施初始化：手柄能力配置结果=" + (hardwareConfigResult ? "成功" : "失败") +
                "，配置=陀螺仪开启、马达开启、音频关闭、灯光关闭、蜂鸣器开启、滤波模式为无");

            m_BluetoothController.Initialize();
            m_HasInitializedSdkServices = true;
            return true;
        }

        private void TryRecoverFrameStreamSession(string reason)
        {
            if (!m_HasRequestedSdkInitialization || !m_HasInitializedSdkServices)
            {
                return;
            }

            if (!isFrameInfoServerIsConnet)
            {
                BoneSenderAppLogger.Log(
                    "骨骼采集会话暂未恢复，骨骼帧服务尚未连接，触发原因=" + reason +
                    "，上次失效原因=" + m_LastFrameSessionInvalidReason);
                return;
            }

            if (m_HasCompletedFrameSession && m_HasRegisteredFrameCallback && m_HasAppliedDefaultMode)
            {
                return;
            }

            if (!TryLoadModelConfig())
            {
                BoneSenderAppLogger.LogWarning(
                    "骨骼采集会话恢复等待模型配置，触发原因=" + reason +
                    "，上次失效原因=" + m_LastFrameSessionInvalidReason);
                return;
            }

            m_HasAppliedDefaultMode = false;
            if (!TryApplyDefaultModeConfig())
            {
                BoneSenderAppLogger.LogWarning(
                    "骨骼采集会话恢复等待默认模式配置，触发原因=" + reason +
                    "，上次失效原因=" + m_LastFrameSessionInvalidReason);
                return;
            }

            if (m_HasRegisteredFrameCallback && m_HasCompletedFrameSession)
            {
                return;
            }

            bool registerResult = RegisterFrameCallback();
            m_HasRegisteredFrameCallback = registerResult;
            m_HasCompletedFrameSession = registerResult;
            if (registerResult)
            {
                BoneSenderAppLogger.Log(
                    "骨骼采集会话恢复成功，触发原因=" + reason +
                    "，上次失效原因=" + m_LastFrameSessionInvalidReason);
                m_LastFrameSessionInvalidReason = "当前会话有效";
                return;
            }

            BoneSenderAppLogger.LogWarning(
                "骨骼采集会话恢复失败，触发原因=" + reason +
                "，上次失效原因=" + m_LastFrameSessionInvalidReason);
        }

        private bool TryLoadModelConfig()
        {
            if (m_HasLoadedModelConfig && m_AndroidModeSelect != null)
            {
                return true;
            }

            string configList = GetGameServiceConfigAll();
            if (string.IsNullOrWhiteSpace(configList))
            {
                BoneSenderAppLogger.LogWarning("当前尚未获取到模型配置，等待下次重试");
                return false;
            }

            m_AndroidModeSelect = new AndroidModeSelect();
            m_AndroidModeSelect.SetAllModelConfig(configList);
            m_HasLoadedModelConfig = true;

            int modelCount = m_AndroidModeSelect.AllConfig?.modelList != null
                ? m_AndroidModeSelect.AllConfig.modelList.Length
                : 0;
            int cameraCount = m_AndroidModeSelect.AllConfig?.camList != null
                ? m_AndroidModeSelect.AllConfig.camList.Length
                : 0;
            BoneSenderAppLogger.Log("已读取模型配置，模型数量=" + modelCount + "，摄像头数量=" + cameraCount);
            return true;
        }

        private bool TryApplyDefaultModeConfig()
        {
            if (m_HasAppliedDefaultMode && m_AndroidModeSelect != null)
            {
                return true;
            }

            if (m_AndroidModeSelect == null)
            {
                return false;
            }

            GameServiceConfig selectedConfig = m_AndroidModeSelect.SetNeedModelConfig(
                new[] { m_DefaultModelName },
                m_DefaultCameraId,
                m_DefaultResolutionHeight,
                m_DefaultResolutionWidth);
            if (selectedConfig == null)
            {
                BoneSenderAppLogger.LogWarning("应用默认模型配置失败，等待下次重试");
                return false;
            }

            string modelName = selectedConfig.modelList != null && selectedConfig.modelList.Length > 0
                ? selectedConfig.modelList[0].name
                : "未知";
            string cameraId = selectedConfig.camList != null && selectedConfig.camList.Length > 0
                ? selectedConfig.camList[0].cameraId
                : "未知";
            string resolutionText = "未知";
            if (selectedConfig.camList != null &&
                selectedConfig.camList.Length > 0 &&
                selectedConfig.camList[0].resolutions != null &&
                selectedConfig.camList[0].resolutions.Length > 0)
            {
                Resolution resolution = selectedConfig.camList[0].resolutions[0];
                resolutionText = resolution.width + "x" + resolution.height;
            }

            m_HasAppliedDefaultMode = true;
            BoneSenderAppLogger.Log(
                "已应用默认模型配置，模型=" + modelName +
                "，摄像头=" + cameraId +
                "，分辨率=" + resolutionText);
            return true;
        }

        private static bool ReadShouldUseSdkRuntime()
        {
            return !Application.isEditor && Application.platform == RuntimePlatform.Android;
        }

        private void InvalidateFrameSession(string reason, bool resetModeState)
        {
            m_HasRegisteredFrameCallback = false;
            m_HasCompletedFrameSession = false;
            m_LastFrameSessionInvalidReason = string.IsNullOrEmpty(reason) ? "未知原因" : reason;
            if (resetModeState)
            {
                m_HasAppliedDefaultMode = false;
            }
        }
    }
}
