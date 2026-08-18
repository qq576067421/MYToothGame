/*
作者：Ting
创建时间：2025.10.07
描述：YouDooSDKConstants 帧信息/骨骼/关键点 partial class 模块
*/

public static partial class YouDooSDKConstants
{
    public enum DetectType
    {
        DETECT_TYPE_PERSON = 0,     // 人体检测
        DETECT_TYPE_LEFT_HAND = 1,  // 左手检测
        DETECT_TYPE_RIGHT_HAND = 2, // 右手检测
        DETECT_TYPE_FACE = 3        // 脸部检测
    }

    public enum PersonState
    {
        STATE_ERROR = -1,
        STATE_UNKNOWN = 0,
        STATE_LEFT_HAND_UP = 1,
        STATE_RIGHT_HAND_UP,
        STATE_LEFT_FOOT_UP,
        STATE_RIGHT_FOOT_UP,
        STATE_BOTH_HANDS_UP,          //双手同时举起来
        STATE_HANDS_CROSSED_OVERHEAD, //双手举过头交叉
        STATE_CLAPPING_HANDS,         // 拍手
        STATE_COUNT
    }

    public enum KeyPointIndex
    {
        Nose = 0,           // 0 鼻尖
        Lefteye,            // 1 左眼
        Righteye,           // 2 右眼
        Leftear,            // 3 左耳
        Rightear,           // 4 右耳
        Leftshoulder,       // 5 左肩
        Rightshoulder,      // 6 右肩
        Leftelbow,          // 7 左肘
        Rightelbow,         // 8 右肘
        Leftwrist,          // 9 左手腕
        Rightwrist,         // 10 右手腕
        Lefthip,            // 11 左髋（左腰）
        Righthip,           // 12 右髋（右腰）
        Leftknee,           // 13 左膝
        Rightknee,          // 14 右膝
        Leftankle,          // 15 左脚踝
        Rightankle,         // 16 右脚踝
        Chest,              // 17 胸口 （新加，可能没有）
        KEYPOINT_COUNT      // 关键点总数
    }

    /// <summary>
    /// 手部21个关键点枚举定义
    /// 使用传统的C风格枚举（plain enum）
    /// 用于标识手部关节
    /// </summary>
    public enum HandLandmark21
    {
        HAND_WRIST = 0,            // 0 手腕

        HAND_THUMB_CMC = 1,        // 1 拇指 - 掌指关节（近手腕）
        HAND_THUMB_MCP = 2,        // 2 拇指 - 掌指关节（近掌心）
        HAND_THUMB_IP = 3,         // 3 拇指 - 远指关节
        HAND_THUMB_TIP = 4,        // 4 拇指 - 指尖

        HAND_INDEX_FINGER_MCP = 5, // 5 食指 - 掌指关节
        HAND_INDEX_FINGER_PIP = 6, // 6 食指 - 近端指间关节
        HAND_INDEX_FINGER_DIP = 7, // 7 食指 - 远端指间关节
        HAND_INDEX_FINGER_TIP = 8, // 8 食指 - 指尖

        HAND_MIDDLE_FINGER_MCP = 9,  // 9 中指 - 掌指关节
        HAND_MIDDLE_FINGER_PIP = 10, // 10 中指 - 近端指间关节
        HAND_MIDDLE_FINGER_DIP = 11, // 11 中指 - 远端指间关节
        HAND_MIDDLE_FINGER_TIP = 12, // 12 中指 - 指尖

        HAND_RING_FINGER_MCP = 13,   // 13 无名指 - 掌指关节
        HAND_RING_FINGER_PIP = 14,   // 14 无名指 - 近端指间关节
        HAND_RING_FINGER_DIP = 15,   // 15 无名指 - 远端指间关节
        HAND_RING_FINGER_TIP = 16,   // 16 无名指 - 指尖

        HAND_PINKY_FINGER_MCP = 17,  // 17 小指 - 掌指关节
        HAND_PINKY_FINGER_PIP = 18,  // 18 小指 - 近端指间关节
        HAND_PINKY_FINGER_DIP = 19,  // 19 小指 - 远端指间关节
        HAND_PINKY_FINGER_TIP = 20,  // 20 小指 - 指尖

        HAND_LANDMARK_COUNT = 21     // 手部关键点总数
    }

    /// <summary>
    /// 脸部关键点枚举定义
    /// 使用传统的C风格枚举（plain enum）
    /// 用于标识脸部的参数
    /// </summary>
    public enum FaceLandmark5
    {
        LEFT_EYE = 0,     ///< 0 左眼 - 左眼中心位置
        RIGHT_EYE = 1,    ///< 1 右眼 - 右眼中心位置
        NOSE_TIP = 2,     ///< 2 鼻尖 - 面部中心参考点
        LEFT_MOUTH = 3,   ///< 3 左嘴角 - 左侧嘴角位置
        RIGHT_MOUTH = 4,  ///< 4 右嘴角 - 右侧嘴角位置

        FACE_LANDMARK_COUNT = 5 ///< 人脸关键点总数，用于遍历和数组初始化
    }

    public static readonly (int from, int to)[] defaultSkeletonConnections = new (int, int)[]
    {
        ((int)KeyPointIndex.Leftear, (int)KeyPointIndex.Lefteye),
        ((int)KeyPointIndex.Lefteye, (int)KeyPointIndex.Nose),
        ((int)KeyPointIndex.Nose, (int)KeyPointIndex.Righteye),
        ((int)KeyPointIndex.Righteye, (int)KeyPointIndex.Rightear),
        ((int)KeyPointIndex.Leftwrist, (int)KeyPointIndex.Leftelbow),
        ((int)KeyPointIndex.Leftelbow, (int)KeyPointIndex.Leftshoulder),
        ((int)KeyPointIndex.Leftshoulder, (int)KeyPointIndex.Rightshoulder),
        ((int)KeyPointIndex.Rightshoulder, (int)KeyPointIndex.Rightelbow),
        ((int)KeyPointIndex.Rightelbow, (int)KeyPointIndex.Rightwrist),
        ((int)KeyPointIndex.Leftshoulder, (int)KeyPointIndex.Lefthip),
        ((int)KeyPointIndex.Lefthip, (int)KeyPointIndex.Leftknee),
        ((int)KeyPointIndex.Leftknee, (int)KeyPointIndex.Leftankle),
        ((int)KeyPointIndex.Rightshoulder, (int)KeyPointIndex.Righthip),
        ((int)KeyPointIndex.Righthip, (int)KeyPointIndex.Rightknee),
        ((int)KeyPointIndex.Rightknee, (int)KeyPointIndex.Rightankle),
        ((int)KeyPointIndex.Lefthip, (int)KeyPointIndex.Righthip),
        // ((int)KeyPointIndex.Chest, (int)KeyPointIndex.Chest)
    };

    public static readonly (int from, int to)[] defaultHandLandmark21Connections = new (int, int)[]
    {
        // 手掌连接
        ((int)HandLandmark21.HAND_WRIST, (int)HandLandmark21.HAND_THUMB_CMC),
        ((int)HandLandmark21.HAND_WRIST, (int)HandLandmark21.HAND_INDEX_FINGER_MCP),
        ((int)HandLandmark21.HAND_WRIST, (int)HandLandmark21.HAND_MIDDLE_FINGER_MCP),
        ((int)HandLandmark21.HAND_WRIST, (int)HandLandmark21.HAND_RING_FINGER_MCP),
        ((int)HandLandmark21.HAND_WRIST, (int)HandLandmark21.HAND_PINKY_FINGER_MCP),

        // 拇指
        ((int)HandLandmark21.HAND_THUMB_CMC, (int)HandLandmark21.HAND_THUMB_MCP),
        ((int)HandLandmark21.HAND_THUMB_MCP, (int)HandLandmark21.HAND_THUMB_IP),
        ((int)HandLandmark21.HAND_THUMB_IP, (int)HandLandmark21.HAND_THUMB_TIP),

        // 食指
        ((int)HandLandmark21.HAND_INDEX_FINGER_MCP, (int)HandLandmark21.HAND_INDEX_FINGER_PIP),
        ((int)HandLandmark21.HAND_INDEX_FINGER_PIP, (int)HandLandmark21.HAND_INDEX_FINGER_DIP),
        ((int)HandLandmark21.HAND_INDEX_FINGER_DIP, (int)HandLandmark21.HAND_INDEX_FINGER_TIP),

        // 中指
        ((int)HandLandmark21.HAND_MIDDLE_FINGER_MCP, (int)HandLandmark21.HAND_MIDDLE_FINGER_PIP),
        ((int)HandLandmark21.HAND_MIDDLE_FINGER_PIP, (int)HandLandmark21.HAND_MIDDLE_FINGER_DIP),
        ((int)HandLandmark21.HAND_MIDDLE_FINGER_DIP, (int)HandLandmark21.HAND_MIDDLE_FINGER_TIP),

        // 无名指
        ((int)HandLandmark21.HAND_RING_FINGER_MCP, (int)HandLandmark21.HAND_RING_FINGER_PIP),
        ((int)HandLandmark21.HAND_RING_FINGER_PIP, (int)HandLandmark21.HAND_RING_FINGER_DIP),
        ((int)HandLandmark21.HAND_RING_FINGER_DIP, (int)HandLandmark21.HAND_RING_FINGER_TIP),

        // 小指
        ((int)HandLandmark21.HAND_PINKY_FINGER_MCP, (int)HandLandmark21.HAND_PINKY_FINGER_PIP),
        ((int)HandLandmark21.HAND_PINKY_FINGER_PIP, (int)HandLandmark21.HAND_PINKY_FINGER_DIP),
        ((int)HandLandmark21.HAND_PINKY_FINGER_DIP, (int)HandLandmark21.HAND_PINKY_FINGER_TIP),

        // 手掌横线
        ((int)HandLandmark21.HAND_THUMB_CMC, (int)HandLandmark21.HAND_INDEX_FINGER_MCP),
        ((int)HandLandmark21.HAND_INDEX_FINGER_MCP, (int)HandLandmark21.HAND_MIDDLE_FINGER_MCP),
        ((int)HandLandmark21.HAND_MIDDLE_FINGER_MCP, (int)HandLandmark21.HAND_RING_FINGER_MCP),
        ((int)HandLandmark21.HAND_RING_FINGER_MCP, (int)HandLandmark21.HAND_PINKY_FINGER_MCP),
    };

    //FrameInfo的服务器
    //骨骼动画相关的。
    public enum YouDooNotifyFrameInfoServiceConnectionType
    {
        SERVICE_CONNECTED = 200001,    // 服务已连接  这个回调是告诉游戏，帧信息的服务已经连接好了， 游戏可以开始玩了。
        SERVICE_DISCONNECTED = 200002, // 服务已断开  除非是自己手动断开（unBindFrameInfoGameService）不必处理。 因为帧服务的断开通常是系统层面的问题，游戏层面没有什么好处理的。
        SERVICE_BINDING = 200003,      // 服务正在绑定中 通常游戏没有处理的必要。开发过程中关注就可以了。
        SERVICE_BIND_FAILED = 200004   // 服务绑定失败 这个错误通常都是系统级的错误，游戏可以选择在这个时候提示玩家。 开发过程中如果遇到这种错误，通常都是配置的关系导致的需要检查一下自己的环境。
    }

    // 新增帧信息服务通知信息
    [System.Serializable]
    public class FrameInfoServerNotifyInfo
    {
        public int notifyFrameInfoServiceConnectionType;
        public string message;
    }

    // 服务连接详细信息（可选，用于解析message中的JSON）
    [System.Serializable]
    public class ServiceConnectionDetailInfo
    {
        public string message;
        public long timestamp;
        public string serviceName;
        public string extraInfo;
    }
}
