/*
作者：Ting
创建时间：2025.10.07
描述：YouDooSDKConstants 蓝牙相关枚举/类 partial class 模块
*/
using System.Runtime.InteropServices;

public static partial class YouDooSDKConstants
{
    // 蓝牙通知类型枚举
    public enum YouDooNotifyBluetoothType
    {

        NEWBONDEVICES = 100001,    // 发现新的绑定设备, 通常由返回桌面添加绑定手柄, 再回到游戏后触发
        DISCONNECT_BONDEDEVICES = 100002,    // 有绑定的设备掉线了，业务：游戏通常需要做一些提示的内容。 需要过滤判断 是否是 当前使用的手柄掉线
        UNBONDEDEVICES = 100003,    // 有设备解除了绑定, 通常由返回桌面解除绑定手柄, 再回到游戏后触发
        DEVICE_BATTERY_LEVEL = 100004,       // 返回某个设备的电量
        GYROSTATE_CHANGED = 100014,  // 陀螺仪的状态被改变： message是 "id" // deviceMac gyroEnabled 。 业务：游戏通常需要根据这个状态来调整自己的陀螺仪相关的功能的开启和关闭。
        GYROSTATE_ERROR = 100015,    // 陀螺仪报错 deviceMac   errorMsg   // 业务：游戏通常需要根据这个状态来调整自己的陀螺仪相关的功能的开启和关闭。
        RECONNECT_BONDEDEVICES = 100016, // SDK 4.1 有绑定的设备又连接上了。
        USE_CANCEL_DEVICE_ADDING = 100100,      // 添加设备中   SDK 4.0 使用设备或者取消使用设备的时候有一些状态机需要返回的 。 useWhichDevice的时候返回： 正在添加 要使用这个手柄。调试的意义更加大。
        USE_CANCEL_DEVICE_ADD_SUCCESS = 100101, // 添加成功   SDK 4.0 使用设备或者取消使用设备的时候有一些状态机需要返回的 。useWhichDevice的时候返回：添加使用这个手柄成功
        USE_CANCEL_DEVICE_ADD_FAILED = 100102,  // 添加失败   SDK 4.0 使用设备或者取消使用设备的时候有一些状态机需要返回的 。useWhichDevice的时候返回：添加使用这个手柄失败
        USE_CANCEL_DEVICE_CONNECTED = 100107,   // 已连接/已使用 这个手柄，不要再连了。   SDK 4.0 使用设备或者取消使用设备的时候有一些状态机需要返回的 。 useWhichDevice的时候返回：重复使用手柄
        USE_CANCEL_DEVICE_UNKNOWN = 100109,     // 添加和取消的时候都有 ： 传递了用一个null的mac地址     SDK 4.0 使用设备或者取消使用设备的时候有一些状态机需要返回的 。  useWhichDevice  cancelUseWhichDevice 的时候Mac是null/"" 无效的。开发阶段有意义。
        USE_CANCEL_DEVICE_GYROMANAGER_OPEN_FALSE = 100110,    // 打开陀螺仪的时候失败了，相当于用不了这个陀螺仪。
        USE_CANCEL_DEVICE_MOTORMANAGER_OPEN_FALSE = 100111,   // 打开马达的时候失败了，相当于用不了这个马达
        USE_CANCEL_DEVICE_SETSOUNDEFFECT_OPEN_FALSE = 100112, // 打开音效/蜂鸣器失败了，相当于用不了这个蜂鸣器

   
    }

    // 蓝牙通知信息
    [System.Serializable]
    public class BluetoothNotifyInfo<T>
    {
        public YouDooNotifyBluetoothType notifyBluetoothType;
        public T message;
    }

    // 辅助类，只用于解析消息类型
    [System.Serializable]
    public class BluetoothNotifyTypeInfo
    {
        public YouDooNotifyBluetoothType notifyBluetoothType;
    }

    //陀螺仪的 状态发生变化。
    [System.Serializable]
    public class GyroStateChangedInfo
    {
        public string deviceMac;
        public bool gyroEnabled; //true表示能用。false表示不能用。陀螺仪经常进入省电模式。
    }

    //陀螺出错
    [System.Serializable]
    public class GyroStateError
    {
        public string deviceMac;
        public bool errorMsg;
    }

    [System.Serializable]
    public class DeviceStatusInfo
    {
        public string address;
        public string name;
        public string status;
        public long timestamp;
    }

    // 电量信息（用于 DEVICE_BATTERY_LEVEL）
    [System.Serializable]
    public class BatteryInfo
    {
        public string address;
        public string name;
        public int batteryLevel;
        public long timestamp;
    }

    [System.Serializable]
    public class DiscoveryFinishedInfo
    {
        public int unbondedDeviceCount;     // 未绑定设备数量
        public long timestamp;              // 时间戳
        public UnbondedDeviceInfo[] unbondedDevices; // 未绑定设备列表
    }

    // 未绑定设备信息
    [System.Serializable]
    public class UnbondedDeviceInfo
    {
        public string address;              // 设备MAC地址
        public string name;                 // 设备名称
    }

    /// <summary>
    /// 使用陀螺仪需要设置滤波。
    /// </summary>
    public enum FilterType
    {
        NONE = 0,
        FIRST_ORDER_LOW_PASS = 1,
        FIRST_ORDER_COMPLEMENTARY = 2,
        MOVING_AVERAGE = 3,
        KALMAN = 4,
        FIR = 5
    }

    [System.Serializable]
    public class DeviceList
    {

        
        public DeviceInfo[] devices;
        public int totalCount;
        public int usedDeviceCount;
    }

    [System.Serializable]
    public class DeviceInfo
    {
        public string address;      // 设备MAC地址
        public string name;         // 设备名称
        public bool isUsed;         // 是否正在使用

        public bool isConnected;   // SDK4.2.2 更新。true表示：在线的。  false：掉线的了。
        
    }

    public enum MotorIntensity
    {
        OFF = 0,
        LOW = 1,
        MEDIUM = 2,
        HIGH = 3
    }

    /// <summary>
    /// Led 灯的类型  2026.01.14 ：变成了颜色。
    /// </summary>
    public enum LedLevel
    {
        LEVEL0 = 0, //所有灯灭
        LEVEL1 = 1, //黄灯长亮
        LEVEL2 = 2, //紫灯长亮
        LEVEL3 = 3, //绿灯长亮
        LEVEL4 = 4, //蓝灯长亮
        LEVEL_Length = 5
    }

    public enum SoundEffect
    {
        SE_0 = 0,
        SE_1 = 1,
        SE_2 = 2,
        SE_3 = 3,
        SE_4 = 4,
        SE_Length = 5
    }

    /// <summary>
    /// 陀螺仪有效的的数据的长度
    /// 已转换为Unity坐标系
    /// </summary>
    public static int GyroDataContentSize = 9;
    public struct GyroData
    {
        public long timestamp; //时间是0表示。
        public int accelX;
        public int accelY;
        public int accelZ;
        public int gyroX;
        public int gyroY;
        public int gyroZ;
        public int magX;
        public int magY;
        public int magZ;

        public float cursorX;  //光标的X  SDK4.2.2增加
        public float cursorY;  //光标的y   SDK4.2.2增加 

        public float quaternionX;  //四元数的X  SDK4.2.2增加 
        public float quaternionY;  //四元数的Y  SDK4.2.2增加 
        public float quaternionZ;  //四元数的Z  SDK4.2.2增加 
        public float quaternionW;  //四元数的W  SDK4.2.2增加


    }

    /// <summary>
    /// 陀螺仪滤波参数结构体（与C++ GyroFilterParams 对应）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GyroFilterParams
    {
        public float AcceDeltaMin;
        public float AcceDeltaMax;
        public float GyroTrustMin;
        public float GyroTrustMax;
        public float GravityFixAlphaMin;
        public float GravityFixAlphaMax;
        public float AcceLerp;
        public float AcceLerpVibrating;
        public float MagnetYawGain;
        public float MinValidMagnetMagnitude;
        public float MaxValidMagnetMagnitude;
        public float VarianceScal;
        public float MinAlphaVarianc;
        public float MaxAlphaVarianc;
        public float GyroLsbDeg;
        public float AcceLsbG;
        public float HSensitivity;
        public float VSensitivity;
        public int ScreenWidth;
        public int ScreenHeight;
        public int ClampMode;
        public int RealTimeComputing;

        public static GyroFilterParams CreateDefault()
        {
            GyroFilterParams p = default;
            p.AcceDeltaMin = 0.04f;
            p.AcceDeltaMax = 0.07f;
            p.GyroTrustMin = 20.0f;
            p.GyroTrustMax = 200.0f;
            p.GravityFixAlphaMin = 0.00f;
            p.GravityFixAlphaMax = 0.16f;
            p.AcceLerp = 0.30f;
            p.AcceLerpVibrating = 0.03f;
            p.MagnetYawGain = 0.05f;
            p.MinValidMagnetMagnitude = 100.0f;
            p.MaxValidMagnetMagnitude = 1500.0f;
            p.VarianceScal = 0.2f;
            p.MinAlphaVarianc = 0.5f;
            p.MaxAlphaVarianc = 1.0f;
            p.GyroLsbDeg = 16.4f;
            p.AcceLsbG = 4096.0f;
            p.HSensitivity = 30.0f;
            p.VSensitivity = 30.0f;
            p.ScreenWidth = 1920;
            p.ScreenHeight = 1080;
            p.ClampMode = 1;
            p.RealTimeComputing = 0; //C++代码 默认是false。
            return p;
        }
    }

    /// <summary>
    /// 配置。
    /// </summary>
    [System.Serializable]
    public class HardWareRemoteControlConfig
    {
        /// <summary>
        /// 是否启用陀螺仪
        /// </summary>
        public bool enableGyro = false; //SDK 里面默认是开启的。

        /// <summary>
        /// 是否启用马达
        /// </summary>
        public bool enableMotor = false; //SDK 里面默认是开启的。

        /// <summary>
        /// 是否启用音频
        /// </summary>
        public bool enableAudio = false;

        /// <summary>
        /// 是否启用LED
        /// </summary>
        public bool enableLed = false;

        /// <summary>
        /// 是否启用扬声器
        /// </summary>
        public bool enableSpeaker = false; //SDK 里面默认是开启的。

        /// <summary>
        /// 滤波器类型
        /// </summary>
        public int filterType = 0;
    }

    public enum AudioPlayState
    {
        IDLE,       // 空闲
        PREPARING,  // 准备中
        PLAYING,    // 播放中
        PAUSED,     // 暂停
        STOPPED,    // 停止
        COMPLETED,  // 播放完成
        ERROR       // 错误
    }
}
