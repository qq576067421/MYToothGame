/*
作者：Ting
创建时间：2025.10.07
描述：YouDooSDKConstants NFC相关枚举/类 partial class 模块
*/

public static partial class YouDooSDKConstants
{
    public enum NFCTagStatus
    {
        /// <summary>
        /// 无标签在感应区
        /// </summary>
        NoTag = 0,

        /// <summary>
        /// 标签已检测到但尚未开始处理
        /// </summary>
        Detected = 1,

        /// <summary>
        /// 正在处理标签（读取或写入中）
        /// </summary>
        Processing = 2,

        /// <summary>
        /// 标签处理完成，可以读取数据
        /// </summary>
        Processed = 3
    }

    /// <summary>
    /// NFC回调类型 - SDK4.1 重构
    /// </summary>
    public enum YouDooNotifyNFCType
    {
        NFC_INSERTED = 160014, //SDK4.1 NFC卡片插入（已验证通过）
        NFC_REMOVED = 160015,  //SDK4.1 NFC卡片移除
    }

    [System.Serializable]
    public class NFCNotice
    {
        public YouDooNotifyNFCType notifyNFCType;
        public string message; //有可能是消息有可能是其他内容
    }

    [System.Serializable]
    public class NdefRecordInfoData
    {
        public int recordIndex;
        public int tnf;
        public string tnfName;
        public string type;
        public string typeName;
        public string payload;
        public int payloadLength;
        public string payloadHex;
    }

    /// <summary>
    /// SDK4.1增加
    /// NFC卡，目前有两种类型：钥匙卡和游戏道具卡。
    /// </summary>
    public enum NFCCardType
    {
        /// <summary>
        /// 钥匙卡
        /// </summary>
        KEY_CARD = 1,

        /// <summary>
        /// 游戏道具卡
        /// </summary>
        GAME_ITEM_CARD = 2
    }

    /// <summary>
    /// NFC卡片信息数据结构
    /// 对应数据库表结构,是数据库那边配置的，也是策划要求配置，CP应该找策划对齐 extern_data；
    /// </summary>
    [System.Serializable]
    public class NFCCardInfo
    {
        /// <summary>
        /// 数据结构版本号
        /// </summary>
        public int ver;

        /// <summary>
        /// 卡片类型
        /// 1:钥匙卡, 2:游戏功能卡
        /// </summary>
        public NFCCardType type;

        /// <summary>
        /// 状态
        /// 0:未激活, 1:已激活
        /// </summary>
        public int status;

        /// <summary>
        /// 行为
        /// </summary>
        public int action;

        /// <summary>
        /// 卡片唯一标识
        /// </summary>
        public string cardId;

        /// <summary>
        /// 权限
        /// 1:允许重复激活
        /// </summary>
        public int permissions;

        /// <summary>
        /// 标记位
        /// </summary>
        public int flags;

        /// <summary>
        /// 业务数据 (JSON格式字符串),每个游戏都是不同的。
        /// </summary>
        public string customData;
    }
}
