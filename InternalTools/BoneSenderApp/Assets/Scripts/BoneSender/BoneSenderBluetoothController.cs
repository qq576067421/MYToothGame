using UnityEngine;

namespace BoneSender
{
    public sealed class BoneSenderBluetoothController : Bluetooth
    {
        private const string m_LogTag = "蓝牙控制桥接：";

        public void Initialize()
        {
            SetGyroStorageMaxSize(gyroStorageMaxSize);
            BoneSenderAppLogger.Log(string.Format(
                "{0}已完成初始化，陀螺仪缓存上限={1}",
                m_LogTag,
                gyroStorageMaxSize));
        }

        public override void HandleOnDeviceServiceConnected(string messageJson)
        {
            BoneSenderAppLogger.Log(m_LogTag + "收到设备服务连接回调，开始刷新并请求使用所有已绑定蓝牙设备");
            base.HandleOnDeviceServiceConnected(messageJson);
            UseAllBondedDevices();
        }

        protected override void HandleDeviceAddSuccess(string messageJson)
        {
            base.HandleDeviceAddSuccess(messageJson);
            BoneSenderAppLogger.Log(m_LogTag + "收到设备添加成功回调，" + BuildDeviceSummary(messageJson));
        }

        protected override void HandleDeviceAddFailed(string messageJson)
        {
            base.HandleDeviceAddFailed(messageJson);
            BoneSenderAppLogger.LogWarning(m_LogTag + "收到设备添加失败回调，" + BuildDeviceSummary(messageJson));
        }

        protected override void HandleDisconnectBondedDevices(string messageJson)
        {
            base.HandleDisconnectBondedDevices(messageJson);
            BoneSenderAppLogger.LogWarning(m_LogTag + "检测到已绑定设备断开连接，" + BuildDeviceSummary(messageJson));
        }

        protected override void HandleReconnectBondedDevices(string messageJson)
        {
            base.HandleReconnectBondedDevices(messageJson);
            BoneSenderAppLogger.Log(m_LogTag + "检测到已绑定设备重新连接，" + BuildDeviceSummary(messageJson));
        }

        protected override void HandleGyroOpenFailed(string messageJson)
        {
            base.HandleGyroOpenFailed(messageJson);
            BoneSenderAppLogger.LogWarning(m_LogTag + "检测到设备陀螺仪开启失败，" + BuildDeviceSummary(messageJson));
        }

        private static string BuildDeviceSummary(string messageJson)
        {
            if (string.IsNullOrWhiteSpace(messageJson))
            {
                return "无附加信息";
            }

            var deviceStatusInfo = JsonUtility.FromJson<YouDooSDKConstants.BluetoothNotifyInfo<YouDooSDKConstants.DeviceStatusInfo>>(messageJson);
            if (deviceStatusInfo != null && deviceStatusInfo.message != null)
            {
                return string.Format(
                    "名称={0}，地址={1}，状态={2}",
                    ReadValueOrUnknown(deviceStatusInfo.message.name),
                    ReadValueOrUnknown(deviceStatusInfo.message.address),
                    ReadValueOrUnknown(deviceStatusInfo.message.status));
            }

            var stringInfo = JsonUtility.FromJson<YouDooSDKConstants.BluetoothNotifyInfo<string>>(messageJson);
            if (stringInfo != null && !string.IsNullOrEmpty(stringInfo.message))
            {
                return "设备地址=" + stringInfo.message;
            }

            return "原始消息=" + messageJson;
        }

        private static string ReadValueOrUnknown(string value)
        {
            return string.IsNullOrEmpty(value) ? "未知" : value;
        }
    }
}
