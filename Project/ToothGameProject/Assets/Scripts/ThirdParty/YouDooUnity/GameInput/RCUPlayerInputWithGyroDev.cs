using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YouDooUnity
{
    public class GyroInput
    {
        public enum GyroFilterType
        {
            NoFilter,
            Adaptive6AxisFilter,
            Adaptive9AxisFilter,
            BasicComplementaryFilter,
            MahonyFilter,
        }
        public float GyroLeast;
        public float GyroScale;
        // 手柄驱动设置灵敏度的lsb
        public float GyroLSB_Deg = 16.4f;
        public float AcceLSB_G = 4096.0f;
        // 自适应滤波参数，使用加速度方差
        public Vector3 ReportGyroData;
        public Vector3 ReportAcceData;
        public Vector3 ReportMagnetData;
        public Quaternion GyroRotation;
        public Vector2 ScreenPosition;
        public string DeviceMac;
        const float GyroSenPlusUnit = 0.5f;
        public Vector3 ReportGyroDrift;
        public bool IsAllowResetRotation;
        IGyroFilter gyroFilter;
        public IGyroFilter GyroFilter => gyroFilter;
        GyroAirMouse gyroAirMouse;
        public GyroAirMouse GyroAirMouse => gyroAirMouse;
        public GyroInput()
        {
            GyroLeast = 20f;
            GyroScale = 1.0f;
            ReportGyroData = Vector3.zero;
            ReportAcceData = Vector3.zero;
            ReportMagnetData = Vector3.zero;
            GyroRotation = Quaternion.identity;
            IsAllowResetRotation = true;

            // 默认使用ada9滤波
            SetFilter(GyroFilterType.Adaptive9AxisFilter);
            gyroAirMouse = new GyroAirMouse();
        }

        public void SetFilter(GyroFilterType type)
        {
            switch (type)
            {
                case GyroFilterType.NoFilter:
                    gyroFilter = new NoFilter();
                    break;
                case GyroFilterType.Adaptive6AxisFilter:
                    gyroFilter = new Adaptive6AxisFilter();
                    break;
                case GyroFilterType.Adaptive9AxisFilter:
                    gyroFilter = new Adaptive9AxisFilter();
                    break;
                case GyroFilterType.BasicComplementaryFilter:
                    gyroFilter = new BasicComplementaryFilter();
                    break;
                case GyroFilterType.MahonyFilter:
                    gyroFilter = new MahonyFilter();
                    break;
                default:
                    gyroFilter = new NoFilter();
                    break;
            }
        }

        public Vector2 UpdateGyroAirMouseNPositionAR(float deltaTime)
        {
            if (string.IsNullOrEmpty(DeviceMac)) return Vector2.negativeInfinity;

            // 获取陀螺仪数据
            // var gyroDataArray = gyroInfo.GetDeviceGyroData(index);
            // Debug.Log($"[Player] 获取设备{DeviceMac}陀螺仪数据");
            var gyroDataArray = RCUPlayerInputManager.Instance.AndroidServerInfo.Bluetooth.HardWareRemoteControlMap[DeviceMac].GyroItem.GetDeviceGyroData();
            if (gyroDataArray.Length == 0)
            {
                // Debug.Log($"[Player] === 设备{DeviceMac}获取到无效数据，返回负无穷===");
                return Vector2.negativeInfinity;
            }

            // 打印设备0的所有数据点
            // Debug.Log($"[Player] === 设备{DeviceMac}获取到 {gyroDataArray.Length} 个数据点 ===");
            for (int i = 0; i < gyroDataArray.Length; i++)
            {
                var data = gyroDataArray[i];
                // Debug.Log($"[Player] 设备{DeviceMac}[数据点{i}]: 时间戳={data.timestamp}, " +
                //          $"加速度=({data.accelX},{data.accelY},{data.accelZ}), " +
                //          $"陀螺仪=({data.gyroX},{data.gyroY},{data.gyroZ})");
            }
            // 使用最新的数据点（第一个）
            var latestGyroData = gyroDataArray[0];

            // 更严格的数据有效性检查
            if (latestGyroData.timestamp == 0)
            {
                Debug.LogWarning($"[Player] 设备{DeviceMac}最新数据无效");
                return Vector2.negativeInfinity;
            }

            // 将整数数据转换为Unity的Vector3
            // 轴对齐，并添加灵敏度修正
            ReportAcceData = new Vector3(
                latestGyroData.accelX,
                latestGyroData.accelZ,
                -latestGyroData.accelY
                );
            Vector3 fixedAcceData = ReportAcceData / AcceLSB_G;
            ReportGyroData = new Vector3(
                latestGyroData.gyroX,
                latestGyroData.gyroZ,
                -latestGyroData.gyroY
                );
            Vector3 fixedGyroData = ReportGyroData;
            ReportMagnetData = new Vector3(
                -latestGyroData.magX,
                -latestGyroData.magZ,
                latestGyroData.magY
                );
            Vector3 fixedMagnetData = ReportMagnetData;


            // 减去静置偏移
            // fixedGyroData -= ReportGyroDrift;
            // 过滤最小数值
            if (fixedGyroData.magnitude <= GyroLeast)
            {
                fixedGyroData = Vector3.zero;
            }
            // 乘lsb和默认灵敏度
            fixedGyroData = fixedGyroData / GyroLSB_Deg;

            // Debug.Log($"[Player] 使用设备{DeviceMac}最终数据 - 时间戳: {latestGyroData.timestamp}, " +
            //          $"加速度: [{ReportAcceData.x}, {ReportAcceData.y}, {ReportAcceData.z}], " +
            //          $"陀螺仪: [{ReportGyroData.x}, {ReportGyroData.y}, {ReportGyroData.z}]");

            // 使用滤波积分
            gyroFilter.QuaternionIntegrate(ref GyroRotation, fixedGyroData, fixedAcceData, fixedMagnetData, deltaTime);
            if (gyroFilter is MahonyFilter mf)
            {
                // 使用mahony滤波时, 需要修正gyro data
                fixedGyroData = mf.CorrectGyro;
            }


            if (gyroFilter is Adaptive9AxisFilter ada9)
            {
                return gyroAirMouse.GetScreenNPositionAR(ref GyroRotation, ada9);
            }

            return gyroAirMouse.GetScreenNPositionAR(GyroRotation, fixedGyroData);
        }

        public void UpdateGyroRotationByScreenPosition(Vector2 screenPosition)
        {
            Vector3 newEuler = new Vector3(
                screenPosition.y / gyroAirMouse.VSensitivity,
                screenPosition.x / gyroAirMouse.HSensitivity,
                0.0f
            );
            GyroRotation = Quaternion.Euler(newEuler);
        }

        public void AirMouseSenPlus(int value)
        {
            if (!RCUPlayerInputManager.Instance.ChangeSenAvailable) return;
            gyroAirMouse.SenPlus(value);
        }

        public void ResetRotation()
        {
            if (!IsAllowResetRotation) return;
            gyroFilter.Reset();
            gyroAirMouse.Reset();

            if (gyroFilter is Adaptive9AxisFilter ada9)
            {
                GyroRotation = ada9.LastDeviceRotation;
            }
        }
    }

    /// <summary>
    /// 带陀螺仪的RCU虚拟控制器
    /// 这个类仅用作c#下的陀螺仪姿态解算算法的开发和测试, 不要在游戏业务里使用
    /// </summary>
    public class RCUPlayerInputWithGyroDev : RCUPlayerInputBase
    {
        // 陀螺仪参数
        GyroInput gyroInput;
        public GyroInput GyroInput => gyroInput;

        protected override void InitAction()
        {
            base.InitAction();
            gyroInput = new GyroInput();
        }

        public override bool SetMacByCapabilities(AndroidServerInfo serverInfo, string capabilities)
        {
            // 解析输入设备的uid
            Capabilities capa = JsonUtility.FromJson<Capabilities>(capabilities);
            if (capa == null)
            {
                gyroInput.DeviceMac = "";
                return false;
            }
            gyroInput.DeviceMac = serverInfo.GetUniqueIdByDescriptor(capa.deviceDescriptor).ToUpper();
            return true;
        }
    }
}