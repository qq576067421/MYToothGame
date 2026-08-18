/*
作者：Ting
创建时间：2025.10.07
描述：YouDooSDKConstants 设备服务相关枚举/类 partial class 模块
*/

public static partial class YouDooSDKConstants
{
    public const string YouDooInputDeviceKeyword = "YouDoo";
    public const string KeyobardDeviceKeyword = "Keyboard";
    public const string GamepadDeviceKeyword = "AndroidGamepad";
    //设备服务。与输入相关的
    //通常在做蓝牙手柄的时候需要处理的服务。因为需要获取到手柄的信息等。
    public enum YouDooNotifyDeviceServiceConnectionType
    {
        SERVICE_CONNECTED = 300001,    // 服务已连接 这个回调是告诉游戏，设备的服务已经连接好了，能获取到主手柄等 设备信息了。
        SERVICE_DISCONNECTED = 300002, // 服务已断开  除非是自己手动断开（unBindInputDeviceService）不必处理。 因为设备服务的断开通常是系统层面的问题，游戏层面没有什么好处理的。
        SERVICE_BINDING = 300003,      // 服务正在绑定中 通常游戏没有处理的必要。开发过程中关注就可以了。
        SERVICE_BIND_FAILED = 300004,  // 服务绑定失败 这个错误通常都是系统级的错误，游戏可以选择在这个时候提示玩家。 开发过程中如果遇到这种错误，通常都是配置的关系导致的需要检查一下自己的环境。
        SERVICE_YD_INPUT = 300005,     // 输入事件的服务SDK4.0
    }

    // 设备服务通知信息
    [System.Serializable]
    public class DeviceServiceNotifyInfo
    {
        public int notifyDeviceServiceConnectionType;
        public string message;
    }

    /// <summary>
    /// SDK封装的按键数据
    /// </summary>
    [System.Serializable]
    public class InputDevice
    {
        /// <summary>
        /// 设备唯一ID
        /// </summary>
        public string uniqueId;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string name;

        /// <summary>
        /// 设备描述符
        /// </summary>
        public string descriptor;

        /// <summary>
        /// 厂商信息
        /// </summary>
        public string vendor;

        /// <summary>
        /// 产品信息
        /// </summary>
        public string product;

        /// <summary>
        /// 是否为主设备
        /// </summary>
        public bool isMajorDevice;

        /// <summary>
        /// 键码值
        /// </summary>
        public int keycode;

        /// <summary>
        /// LED等级
        /// </summary>
        public int ledlevel;
    }

    [System.Serializable]
    public class InputDeviceList
    {
        public InputDevice[] devices;
    }

    /// <summary>
    /// SDK4.0 映射过来的手势数据。
    /// 对应Java层 getHandsCursorInfo 方法返回的JSON数据格式。
    /// </summary>
    [System.Serializable]
    public class HandsCursorData
    {
        /// <summary>
        /// 状态码。
        /// 0  -> 成功
        /// -1 -> 无效参数
        /// -2 -> 找不到对应ID
        /// 其他负数 -> 其他错误
        /// </summary>
        public int code;

        /// <summary>
        /// 左手按压状态。
        /// 注意：这里是int类型，不是bool。
        /// 1 表示按下（true）
        /// 0 表示未按下（false）
        /// </summary>
        public int leftPress;

        /// <summary>
        /// 右手按压状态。
        /// 注意：这里是int类型，不是bool。
        /// 1 表示按下（true）
        /// 0 表示未按下（false）
        /// </summary>
        public int rightPress;

        /// <summary>
        /// 左手光标 X 坐标。
        /// 屏幕相对位置，取值范围 [0, 1]。
        /// 原点 (0,0) 位于屏幕左上角，X轴向右递增。
        /// </summary>
        public float leftX;

        /// <summary>
        /// 左手光标 Y 坐标。
        /// 屏幕相对位置，取值范围 [0, 1]。
        /// 原点 (0,0) 位于屏幕左上角，Y轴向下递增。
        /// </summary>
        public float leftY;

        /// <summary>
        /// 右手光标 X 坐标。
        /// 屏幕相对位置，取值范围 [0, 1]。
        /// 原点 (0,0) 位于屏幕左上角，X轴向右递增。
        /// </summary>
        public float rightX;

        /// <summary>
        /// 右手光标 Y 坐标。
        /// 屏幕相对位置，取值范围 [0, 1]。
        /// 原点 (0,0) 位于屏幕左上角，Y轴向下递增。
        /// </summary>
        public float rightY;
    }
}
