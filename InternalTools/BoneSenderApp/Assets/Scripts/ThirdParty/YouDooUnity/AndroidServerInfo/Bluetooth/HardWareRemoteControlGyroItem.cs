using System;
using UnityEngine;

public class HardWareRemoteControlGyroItem
{
    private YouDooSDKConstants.GyroFilterParams gyroParams;

    public YouDooSDKConstants.GyroFilterParams GyroParams
    {
        get => gyroParams;
        set => gyroParams = value;
    }

    /// <summary>
    /// 是否输出逐帧陀螺仪调试日志。正式业务建议保持 false。
    /// </summary>
    public bool EnableDebugLog { get; set; } = false;

    private long[] timestamps;
    private int[] rawGyroData;
    private float[] pixel;
    private float[] quat;
    private string deviceMac;
    private bool clearAfterGet;

    private YouDooSDKConstants.GyroData[] gyroDataCache;
    private int validDataCount = 0;
    private int lastValidIndex = 0;
    private bool dataDirty = true;
    private bool isActive;
    private bool lastNativeHasData;
    private int noDataFrameCount;
    private int reopenGyroEveryNoDataFrames = 120;
    private bool isAvailable = true;
    private string unavailableReason;

    public bool IsActive => isActive;
    public bool IsAvailable => isAvailable;
    public string UnavailableReason => unavailableReason;
    public bool LastNativeHasData => lastNativeHasData;
    public int NoDataFrameCount => noDataFrameCount;

    private readonly int GYRO_DATA_SIZE = YouDooSDKConstants.GyroDataContentSize;

    public event Action<HardWareRemoteControlGyroItem> OnUpdateGyroDataAction;

    public void InitGyroItem(string deviceMacT, int storageMaxSize, bool clearAfterGetT)
    {
        deviceMac = Bluetooth.NormalizeDeviceMac(deviceMacT);
        clearAfterGet = clearAfterGetT;

        timestamps = new long[storageMaxSize];
        rawGyroData = new int[storageMaxSize * GYRO_DATA_SIZE];
        gyroDataCache = new YouDooSDKConstants.GyroData[storageMaxSize];

        pixel = new float[storageMaxSize * 2];
        quat = new float[storageMaxSize * 4];

        // C++层默认的配置与这里的配置是一样的。不用设置一次的。
        gyroParams = YouDooSDKConstants.GyroFilterParams.CreateDefault();
        gyroParams.RealTimeComputing = 1;

        Bluetooth.RegisterGyroDeviceMac(deviceMac);

        if (EnableDebugLog)
        {
            Debug.Log($"[GyroInfo] 初始化滤波参数: RealTimeComputing={gyroParams.RealTimeComputing}, Device={deviceMac}");
        }

        SetGyroFilterParams();
    }

    /// <summary>
    /// 刷新数据。
    /// </summary>
    public void Update()
    {
        if (!isActive || !isAvailable)
        {
            return;
        }

        if (string.IsNullOrEmpty(deviceMac))
        {
            return;
        }

        try
        {
            ClearNativeBuffers();

            bool hasData = Bluetooth.GetComputationDataByMAC(deviceMac, clearAfterGet, timestamps, rawGyroData, pixel, quat);
            lastNativeHasData = hasData;

            if (hasData)
            {
                noDataFrameCount = 0;

                if (EnableDebugLog)
                {
                    LogRawComputationData();
                }
            }
            else
            {
                noDataFrameCount++;

                if (EnableDebugLog)
                {
                    Debug.Log($"[GyroInfo] 陀螺仪出库无数据 Device={deviceMac}, NoDataFrameCount={noDataFrameCount}");
                }

                if (isAvailable && noDataFrameCount % reopenGyroEveryNoDataFrames == 0)
                {
                    Debug.LogWarning($"[GyroInfo] 连续无数据，重新注册并打开陀螺仪 Device={deviceMac}, NoDataFrameCount={noDataFrameCount}");
                    Bluetooth.RegisterGyroDeviceMac(deviceMac);
                    SetGyroFilterParams();
                    AndroidServerInfo.Instance.OpenGyro(deviceMac, true);
                }
            }

            dataDirty = true;
            GetDeviceGyroData();
            OnUpdateGyroDataAction?.Invoke(this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GyroInfo] 异常: {e.Message}");
        }
    }

    private void ClearNativeBuffers()
    {
        if (timestamps != null)
        {
            Array.Clear(timestamps, 0, timestamps.Length);
        }

        if (rawGyroData != null)
        {
            Array.Clear(rawGyroData, 0, rawGyroData.Length);
        }

        if (pixel != null)
        {
            Array.Clear(pixel, 0, pixel.Length);
        }

        if (quat != null)
        {
            Array.Clear(quat, 0, quat.Length);
        }
    }

    private void LogRawComputationData()
    {
        for (int i = 0; i < timestamps.Length; i++)
        {
            if (timestamps[i] == 0)
            {
                continue;
            }

            int baseIndex = i * GYRO_DATA_SIZE;
            Debug.Log($"[GyroInfo] 陀螺仪出库 有数据 [{i}]: timestamp={timestamps[i]}, " +
                      $"Accel({rawGyroData[baseIndex]}, {rawGyroData[baseIndex + 1]}, {rawGyroData[baseIndex + 2]}), " +
                      $"Gyro({rawGyroData[baseIndex + 3]}, {rawGyroData[baseIndex + 4]}, {rawGyroData[baseIndex + 5]}), " +
                      $"Mag({rawGyroData[baseIndex + 6]}, {rawGyroData[baseIndex + 7]}, {rawGyroData[baseIndex + 8]}), " +
                      $"pixel=({pixel[i * 2]}, {pixel[i * 2 + 1]}), " +
                      $"quat=({quat[i * 4]}, {quat[i * 4 + 1]}, {quat[i * 4 + 2]}, {quat[i * 4 + 3]}), " +
                      $"Device={deviceMac}");
        }
    }

    /// <summary>
    /// 使用 Span 的高性能版本。
    /// </summary>
    public YouDooSDKConstants.GyroData[] GetDeviceGyroData()
    {
        if (!dataDirty && validDataCount > 0)
        {
            return GetValidDataSlice();
        }

        if (timestamps == null || rawGyroData == null || pixel == null || quat == null || gyroDataCache == null)
        {
            return Array.Empty<YouDooSDKConstants.GyroData>();
        }

        validDataCount = 0;

        Span<long> tsSpan = timestamps;
        Span<int> valSpan = rawGyroData;
        Span<float> pixelSpan = pixel;
        Span<float> quatSpan = quat;
        Span<YouDooSDKConstants.GyroData> dataSpan = gyroDataCache;

        for (int i = 0; i < tsSpan.Length; i++)
        {
            if (tsSpan[i] == 0)
            {
                continue;
            }

            lastValidIndex = i;
            ref YouDooSDKConstants.GyroData data = ref dataSpan[validDataCount];
            int baseIndex = i * GYRO_DATA_SIZE;

            data.timestamp = tsSpan[i];
            data.accelX = valSpan[baseIndex];
            data.accelY = valSpan[baseIndex + 1];
            data.accelZ = valSpan[baseIndex + 2];
            data.gyroX = valSpan[baseIndex + 3];
            data.gyroY = valSpan[baseIndex + 4];
            data.gyroZ = valSpan[baseIndex + 5];
            data.magX = valSpan[baseIndex + 6];
            data.magY = valSpan[baseIndex + 7];
            data.magZ = valSpan[baseIndex + 8];

            data.cursorX = pixelSpan[i * 2]; // 光标的X SDK4.2.2增加
            data.cursorY = pixelSpan[i * 2 + 1]; // 光标的Y SDK4.2.2增加

            data.quaternionX = quatSpan[i * 4]; // 四元数的X SDK4.2.2增加
            data.quaternionY = quatSpan[i * 4 + 1]; // 四元数的Y SDK4.2.2增加
            data.quaternionZ = quatSpan[i * 4 + 2]; // 四元数的Z SDK4.2.2增加
            data.quaternionW = quatSpan[i * 4 + 3]; // 四元数的W SDK4.2.2增加

            validDataCount++;

            if (EnableDebugLog)
            {
                Debug.Log($"[GyroInfo] 陀螺仪取数据: GyroData[Accel({data.accelX}, {data.accelY}, {data.accelZ}) " +
                          $"Gyro({data.gyroX}, {data.gyroY}, {data.gyroZ}) " +
                          $"Mag({data.magX}, {data.magY}, {data.magZ}) Device={deviceMac}]");
            }
        }

        dataDirty = false;
        return GetValidDataSlice();
    }

    private YouDooSDKConstants.GyroData[] GetValidDataSlice()
    {
        if (validDataCount == 0)
        {
            return Array.Empty<YouDooSDKConstants.GyroData>();
        }

        if (validDataCount == gyroDataCache.Length)
        {
            return gyroDataCache;
        }

        return gyroDataCache.AsSpan(0, validDataCount).ToArray();
    }

    /// <summary>
    /// 零分配版本：直接返回 Span（调用者需要立即使用）。
    /// </summary>
    public ReadOnlySpan<YouDooSDKConstants.GyroData> GetDeviceGyroDataSpan()
    {
        if (dataDirty)
        {
            GetDeviceGyroData(); // 确保数据最新
        }

        return new ReadOnlySpan<YouDooSDKConstants.GyroData>(gyroDataCache, 0, validDataCount);
    }

    public void SetGyroActive(bool active)
    {
        if (active && !isAvailable)
        {
            Debug.LogWarning($"[GyroInfo] 设备陀螺仪已标记不可用，忽略打开请求 Device={deviceMac}, Reason={unavailableReason}");
            return;
        }

        isActive = active;

        if (string.IsNullOrEmpty(deviceMac))
        {
            return;
        }

        if (isActive)
        {
            Bluetooth.RegisterGyroDeviceMac(deviceMac);
            SetGyroFilterParams();
        }
        else
        {
            Bluetooth.UnregisterGyroDeviceMac(deviceMac);
        }

        Debug.Log($"[GyroInfo] {(isActive ? "打开" : "关闭")}陀螺仪 Device={deviceMac}");
        AndroidServerInfo.Instance.OpenGyro(deviceMac, isActive);
    }

    public void MarkUnavailable(string reason)
    {
        isAvailable = false;
        unavailableReason = reason;
        isActive = false;
        noDataFrameCount = 0;

        if (!string.IsNullOrEmpty(deviceMac))
        {
            Bluetooth.UnregisterGyroDeviceMac(deviceMac);
        }

        Debug.LogWarning($"[GyroInfo] 标记陀螺仪不可用并停止重开 Device={deviceMac}, Reason={reason}");
    }

    public void MarkAvailable()
    {
        isAvailable = true;
        unavailableReason = null;
        noDataFrameCount = 0;
    }

    public void ResetGyroMappingState()
    {
        if (string.IsNullOrEmpty(deviceMac))
        {
            return;
        }

        Debug.Log($"[GyroInfo] 重置陀螺仪映射状态 Device={deviceMac}");
        Bluetooth.ResetGyroMappingState(deviceMac);
    }

    /// <summary>
    /// 更新陀螺仪参数到C++中。
    /// </summary>
    public void SetGyroFilterParams()
    {
        if (string.IsNullOrEmpty(deviceMac))
        {
            return;
        }

        Bluetooth.SetGyroFilterParams(deviceMac, gyroParams);
    }
}
