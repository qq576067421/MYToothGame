/*
作者：Ting
创建时间：2025.10.07
描述：YouDooSDKConstants 桌面服务（IYMS）相关枚举/类 partial class 模块
*/
using System.Collections.Generic;

public static partial class YouDooSDKConstants
{
    //与桌面打交道的数据统计的服务。
    public enum YouDooNotifyIYMSgameServerType
    {
        /// <summary>
        /// 服务已连接
        /// </summary>
        IYMS_SERVICE_CONNECTED = 180001,
        /// <summary>
        /// 服务已断开
        /// </summary>
        IYMS_SERVICE_DISCONNECTED = 180002,
        /// <summary>
        /// 服务正在绑定中
        /// </summary>
        IYMS_SERVICE_BINDING = 180003,
        /// <summary>
        /// 服务绑定失败
        /// </summary>
        IYMS_SERVICE_BIND_FAILED = 180004,
        /// <summary>
        /// 账户变更
        /// </summary>
        IYMS_ACCOUNT_CHANGE = 180005,
        IYMS_GET_GAME_SAVE_CALLBACK_SUCCESS = 180006, //获取存档信息 ,成功, 2026.02.09 SDK  4.0
        IYMS_GET_GAME_SAVE_CALLBACK_FAILURE = 180007, //获取存档信息, 失败  2026.02.09 SDK  4.0
        IYMS_SET_GAME_SAVE_CALLBACK_SUCCESS = 180008, //保存 存档信息 ,成功, 2026.02.09  SDK  4.0
        IYMS_SET_GAME_SAVE_CALLBACK_FAILURE = 180009, //保存 存档信息, 失败  2026.02.09 SDK  4.0
        IYMS_GET_RANK_SCORE_SUCCESS = 180010,         //获取排行的信息 成功 2026.02.09 SDK4.0
        IYMS_GET_RANK_SCORE_FAILURE = 180011,         //获取排行的信息 失败 2026.02.09 SDK4.0
        IYMS_UPDATA_RANK_SCORE_SUCCESS = 180012,      //更新排行的信息 成功 2026.02.09 SDK4.0
        IYMS_UPDATA_RANK_SCORE_FAILURE = 180013,      //更新排行的信息 失败 2026.02.09 SDK4.0
        IYMS_GET_RANK_HISTORY_SCORE_SUCCESS = 180014, //获取历史排行的信息 成功 2026.02.09 SDK4.0
        IYMS_GET_RANK_HISTORY_SCORE_FAILURE = 180015, //获取历史排行的信息 失败 2026.02.09 SDK4.0
        IYMS_SELECTROLE_ONSTATECHANGED = 180100,      //返回是一个int
        IYMS_SELECTROLE_ONSUCCESS = 180101,           //返回的是string 不用解析
        IYMS_SELECTROLE_ONFAILURE = 180102,           //返回的是string 不用解析
        IYMS_SELECTROLE_ONCANCELLED = 180103,         //返回的是string 不用解析
        IYMS_QUERY_APP_PAY_ITEM_SUCCESS = 180200,     //查询已购买数据成功 2026.04.07 SDK4.1
        IYMS_QUERY_APP_PAY_ITEM_FAILURE = 180201,     //查询已购买数据失败 2026.04.07 SDK4.1
        IYMS_QUERY_GAME_PRODUCTS_SUCCESS = 180202,    //查询游戏产品成功 2026.04.08 SDK4.1
        IYMS_QUERY_GAME_PRODUCTS_FAILURE = 180203,    //查询游戏产品失败 2026.04.08 SDK4.1
        IYMS_PURCHASE_GAME_PRODUCTS_SUCCESS = 180204, //购买游戏产品成功 2026.04.08 SDK4.1
        IYMS_PURCHASE_GAME_PRODUCTS_FAILURE = 180205, //购买游戏产品失败 2026.04.08 SDK4.1
        IYMS_PAYMENT_SUCCESS = 180300, //调用系统充值
        IYMS_PAYMENT_FAILURE = 180301, //调用系统充值 失败 
        IYMS_QUERY_SIGN_URL_SUCCESS = 180400, //批量查询签名URL成功 2026.04.12 SDK4.2
        IYMS_QUERY_SIGN_URL_FAILURE = 180401, //批量查询签名URL失败 2026.04.12 SDK4.2
        IYMS_GET_ROLE_INFO_SUCCESS = 180402,  //获取角色信息成功 2026.05.05 SDK4.2.2
        IYMS_GET_ROLE_INFO_FAILURE = 180403, //获取角色信息失败 2026.05.05 SDK4.2.2
    }

    public enum YouDooNotifySelectroleState
    {
        STATE_SELECTING = 1,
        STATE_CREATING_ROLE = 2,
    }

    // 桌面信息的回调。
    [System.Serializable]
    public class IYMSgameServerNotifyInfo
    {
        public int notifyIYMSgameServerType;
        public string message;
    }

    // 针对选角成功的回调，message是一个对象而不是字符串
    [System.Serializable]
    public class IYMSgameServerRoleNotifyInfo
    {
        public int notifyIYMSgameServerType;
        public RoleInfo message;
    }

    //数据统计。数据报相关的。
    public static string REPORT_TYPE_EVENT = "EVENT";
    public static string REPORT_BUSINESSTYPE_GAME_CALORIE = "GAME_CALORIE"; //卡路里，需要CP自己处理
    public static string REPORT_BUSINESSTYPE_GAME_ENERGY = "GAME_ENERGY";   // 元气值，需要CP自己处理  // value ：{"f":2,"v":100}
    public static string REPORT_BUSINESSTYPE_GAME_ON_RESUME = "GAME_ON_RESUME"; //2026.02.10 SDK 4.0 游戏来到前台 目的是统计日活; SDK在收到onResume的时候处理好了,CP不用理会
    public static string REPORT_BUSINESSTYPE_GAME_ON_PAUSE = "GAME_ON_PAUSE";   //2026.02.10 SDK 4.0 游戏去到后台 目的是统计日活; SDK在收到onPause的时候处理好了，CP不用理会

    /// <summary>
    /// SDK4.0开始。CP必须手动上报UserID 游玩的时间，
    /// 上报的是游玩的时间段。单位是秒。
    /// 如果没有UserId则不要上报
    ///  举例：UserId=2的用户玩了一局,时间是1分钟，那就上报60秒。
    ///  举例：UserId=2的用户又玩了一局,时间是1分钟，那就又上报60秒。
    /// </summary>
    public static string REPORT_BUSINESSTYPE_USER_GAME_DURATION = "USER_GAME_DURATION";

    //提交备份。 SDK 4.1
    [System.Serializable]
    public class VipInfo
    {
        public int type;       // VIP类型
        public string expire;  // 过期时间
    }

    //SDK 4.1
    [System.Serializable]
    public class CurrencyInfo
    {
        public int type;      // 虚拟币类型
        public int currency;  // 虚拟币数量
    }

    /// <summary>
    /// 角色信息列表包装类，用于 JsonUtility 解析 {"items":[...]} 格式
    /// </summary>
    [System.Serializable]
    public class RoleInfoList
    {
        public RoleInfo[] items;
    }

    [System.Serializable]
    public class RoleInfo
    {
        public long userId;            // 用户ID
        public string nickname;        // 昵称
        public string avatarId;        // 本质是一个数值
        public string avatarUri;       // 头像的地址。
        public long avatarUpdatedAt;   // 头像的更新时间
        public int guardian;           // 守护者标识
        public int heightMm;          // 身高（毫米）
        public int weightG;            // 体重（克）
        public int gender;             // 性别：0-未知，1-男，2-女
        public long createAt;          // 创建时间戳
        public string facePhotoPath;   // 人脸照片路径
        public long faceDataUpdatedAt; // 人脸数据更新时间戳
    }

    [System.Serializable]
    public class AccountInfo
    {
        public int accountId;          // 账号ID
        public string username;        // 用户名
        public string nickname;        // 昵称
        public string avatar;          // 头像URL
        public int gender;             // 性别：0-未知，1-男，2-女
        public string birthday;        // 生日
        public string birthdayReal;    // 真实生日
        public string intro;           // 个人简介
        public bool isGuest;           // true表示游客账户
        public int verified;           // 验证状态
        public string statusParents;   // 家长状态
        public string statusCurrency;  // 货币状态
        public List<VipInfo> vips;     // VIP信息列表
        public List<CurrencyInfo> currencies; // 虚拟币列表
        public List<RoleInfo> users;   // 用户角色列表
    }

    /// <summary>
    /// 游戏产品项信息 SDK 4.1
    /// </summary>
    [System.Serializable]
    public class GameProductItemInfo
    {
        public long type;      // 类型
        public long value;     // 值
        public long seq;       // 顺序
        public string name;    // 名称
        public string intro;   // 简介
        public string imageUrl; // 图片URL
        public long appId;     // 应用ID
        public long itemType;  // 项类型
    }

    /// <summary>
    /// 游戏产品信息 SDK 4.1
    /// </summary>
    [System.Serializable]
    public class GameProductInfo
    {
        public long id;                        // 产品ID
        public string name;                    // 名称
        public string subName;                 // 子名称
        public string intro;                   // 简介
        public string imageUrl;                // 图片URL
        public int amount;                     // 实际金额（数值，用于传参）
        public string showOriginal;            // 显示原价（字符串，用于展示，如"1分"）
        public string showAmount;              // 显示金额（字符串，用于展示，如"1分"）
        public List<GameProductItemInfo> items; // 产品项列表
    }

    /// <summary>
    /// 游戏产品信息列表包装类，用于 JsonUtility 解析 SDK 4.1
    /// </summary>
    [System.Serializable]
    public class GameProductInfoList
    {
        public GameProductInfo[] items;
    }

    /// <summary>
    /// 上报元气值
    /// </summary>
    [System.Serializable]
    public class ReportGameEnergy
    {
        public GameEnergyType f; //元气值。
        public long v;           //时长。单位是秒
    }

    public enum GameEnergyType
    {
        AFK = 0,    // 挂机
        LOW = 1,    // 低
        MEDIUM = 2, // 中
        HIGH = 3,   // 高
    }

    // 定义接收的包装类
    [System.Serializable]
    public class IYMSgameGetRankHistoryScoreSuccessNotifyInfo
    {
        public IYMSgameGetRankHistoryScoreSuccessNotifyInfoItem[] scores; // 分数数组
    }

    /// <summary>
    /// 获取历史排行的正确返回的信息
    /// </summary>
    [System.Serializable]
    public class IYMSgameGetRankHistoryScoreSuccessNotifyInfoItem
    {
        public string score;      // 分数
        public int rank;          // 排名
        public string entryTime; //SDK 4.2.2 增加。上榜时间 需要判断是否 ""
    }

    /// <summary>
    /// 排行榜封装的 排行信息 IYMS_GET_RANK_SCORE_SUCCESS 回调的时候要用到。
    /// </summary>
    [System.Serializable]
    public class IYMSRankScoreInfo
    {
        public long score;
        public long rank;
        // public string accountId;  // 账号ID //SDK4.2.2 删除，原因是数据结构变化了。
        public long userId;  //SDK 4.2.2 增加。
        public string entryTime; //SDK 4.2.2 增加。 //这个是秒
    }

    /// <summary>
    /// 排行榜封装的 排行信息列表 IYMS_GET_RANK_SCORE_SUCCESS 回调的时候要用到。
    /// </summary>
    [System.Serializable]
    public class IYMSRankScoreInfoList
    {
        public IYMSRankScoreInfo[] scores;
    }

    /// <summary>
    /// 调用 GetRankHistoryScore 的时候需要传入的类型
    /// businessType 业务类型（1=最低分查询，2=查询用户自己）
    /// </summary>
    public enum GetRankHistoryScoreBusinessType
    {
        GRHSBT_LOW_SCORE = 1, //最低分查询
        GRHSBT_MYSELF = 1,    //查询用户自己
    }

    /// <summary>
    /// 上报排行榜的信息。
    /// </summary>
    [System.Serializable]
    public class UpdateUserRankScoreList
    {
        public UpdateUserRankScore[] items;
    }

    /// <summary>
    /// 上报排行榜的信息。
    /// </summary>
    [System.Serializable]
    public class UpdateUserRankScore
    {
        public long userId;   //用户ID
        public string score;  //分数
    }

    /// <summary>
    /// 签名URL的单个条目，key是存储Key，url是对应的签名URL
    /// SDK 4.2
    /// </summary>
    [System.Serializable]
    public class SignUrlItem
    {
        public string key;  // 存储Key（即调用 QuerySignUrl 时传入的 storageKey）
        public string url;  // 对应的签名URL
    }

    /// <summary>
    /// 签名URL列表，用于存储解析后的签名URL结果
    /// SDK 4.2
    /// </summary>
    [System.Serializable]
    public class SignUrlList
    {
        public SignUrlItem[] items;
    }

    //////////////////////////////YMS的错误返回//////////////////////////

    /// <summary>
    /// YMS错误返回基类 。
    /// 这种错误基本上都是系统级的错误，
    /// 或者传错参之类的，
    /// 理论上都是开发阶段能处理完的。
    /// </summary>
    [System.Serializable]
    public class IYMSFailureNotifyInfo
    {
        public int code;
        public string message;
    }

    /// <summary>
    /// 获取 存档的信息失败时候 的返回。  2026.04.08  核实正确。
    /// </summary>
    [System.Serializable]
    public class IYMSgameGetSaveFailureNotifyInfo : IYMSFailureNotifyInfo { }

    /// <summary>
    /// 保存 存档的信息失败时候 的返回。 2026.04.08 核实正确
    /// </summary>
    [System.Serializable]
    public class IYMSgameSetSaveFailureNotifyInfo : IYMSFailureNotifyInfo { }

    /// <summary>
    /// 获取历史排行的失败返回的信息  2026.04.08 核实正确。
    /// </summary>
    [System.Serializable]
    public class IYMSgameGetRankHistoryScoreFailureNotifyInfo : IYMSFailureNotifyInfo { }

    /// <summary>
    /// 更新分数失败的回调。 2026.04.08 核实正确
    /// </summary>
    [System.Serializable]
    public class IYMSgameUpdataRankScoreFailureNotifyInfo : IYMSFailureNotifyInfo { }

    /// <summary>
    /// 获取排行榜信息失败的回调 2026.04.08 核实正确
    /// </summary>
    [System.Serializable]
    public class IYMSgameGetRankScoreFailureNotifyInfo : IYMSFailureNotifyInfo { }

    /// <summary>
    /// 查询我的购买记录报错  2026.04.08 核实正确。
    /// </summary>
    [System.Serializable]
    public class IYMSqueryAppPayItemFailureNotifyInfo : IYMSFailureNotifyInfo { }

    /// <summary>
    /// 查询产品可以购买列表失败。  2026.04.08 核实正确。
    /// </summary>
    [System.Serializable]
    public class IYMSqueryGameProductsFailureNotifyInfo : IYMSFailureNotifyInfo { }

    /// <summary>
    /// 游戏内购买的时候失败。  2026.04.08 核实正确。
    /// </summary>
    [System.Serializable]
    public class IYMSpurchaseGameProductsFailureNotifyInfo : IYMSFailureNotifyInfo { }

    /// <summary>
    /// 人脸识别中，排斥人脸的时候失败 2026.04.08 核实正确。
    /// </summary>
    [System.Serializable]
    public class IYMSselectRoleOnFailureNotifyInfo : IYMSFailureNotifyInfo { }
}
