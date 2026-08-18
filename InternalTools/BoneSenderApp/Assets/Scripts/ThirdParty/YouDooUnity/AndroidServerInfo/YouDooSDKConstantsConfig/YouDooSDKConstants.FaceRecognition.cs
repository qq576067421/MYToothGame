/*
作者：Ting
创建时间：2025.10.07
描述：YouDooSDKConstants 人脸识别相关枚举/类 partial class 模块
*/

public static partial class YouDooSDKConstants
{
    public enum YouDooNotifyFaceRecognitionType
    {
        FRT_USER_ONLY = 190001,  //只需要对应到 User的数据 SDK 4.0
        FRT_ALL = 190002,        //需要完整的数据。 SDK 4.0
        FRT_MINIMALIST = 190003, //简化的数据。 SDK 4.0
    }

    /// <summary>
    /// 性别枚举
    /// </summary>
    public enum Gender
    {
        UNKNOW = 0,
        MAN = 1,
        WOMAN = 2,
    }

    public enum FaceRecognitionEmoji
    {
        /// <summary>
        /// 生气/Angry
        /// </summary>
        FRE_ANGRY = 0,

        /// <summary>
        /// 厌恶/Disgust
        /// </summary>
        FRE_DISGUST = 1,

        /// <summary>
        /// 恐惧/Fear
        /// </summary>
        FRE_FEAR = 2,

        /// <summary>
        /// 开心/Happy
        /// </summary>
        FRE_HAPPY = 3,

        /// <summary>
        /// 悲伤/Sad
        /// </summary>
        FRE_SAD = 4,

        /// <summary>
        /// 惊讶/Surprise
        /// </summary>
        FRE_SURPRISE = 5,

        /// <summary>
        /// 中性/Neutral
        /// </summary>
        FRE_NEUTRAL = 6,

        /// <summary>
        /// 表情数量
        /// </summary>
        FRE_LENGTH = 7
    }

    [System.Serializable]
    public class FaceRecognitionTypeUserOnly
    {
        public long[] userIds; //null或者长度是0表示没有匹配到账户里面的人;这里是一对多的关系。
        public int personId = -1;     // 人体ID，默认-1表示未匹配
        public Gender gender;
        public float age;  //年龄
        public FaceRecognitionEmoji emoji; //表情 0:"Angry", 1:"Disgust", 2:"Fear", 3:"Happy", 4:"Sad", 5:"Surprise", 6:"Neutral"
    }

    // 人脸识别简约版数据（对应Java中的getJsonMinimalist）
    [System.Serializable]
    public class FaceRecognitionTypeMinimalist
    {
        public float left;   //这张脸的范围，归一化的数据。
        public float top;    //这张脸的范围，归一化的数据。
        public float right;  //这张脸的范围，归一化的数据。
        public float bottom; //这张脸的范围，归一化的数据。
        public long[] userIds;    //null或者长度是0表示没有匹配到账户里面的人;这里是一对多的关系。
        public int personId = -1; // 人体ID，默认-1表示未匹配
        public Gender gender;
        public float age;  //年龄
        public FaceRecognitionEmoji emoji; //表情 0:"Angry", 1:"Disgust", 2:"Fear", 3:"Happy", 4:"Sad", 5:"Surprise", 6:"Neutral"
    }

    // 人脸识别完整版数据（对应Java中的getJsonALL）
    [System.Serializable]
    public class FaceRecognitionTypeALL
    {
        public float left;   //这张脸的范围，归一化的数据。
        public float top;    //这张脸的范围，归一化的数据。
        public float right;  //这张脸的范围，归一化的数据。
        public float bottom; //这张脸的范围，归一化的数据。
        public long[] userIds;    //null或者长度是0表示没有匹配到账户里面的人;这里是一对多的关系。
        public int personId = -1; // 人体ID，默认-1表示未匹配

        // 人脸特征向量 (512维)
        public float[] faceFeatureID; //这张脸的特征值

        // 图像质量参数
        public float brightness; // 亮度值
        public float sharpness;  // 锐利度/清晰度

        // 关键点坐标数组（与Java保持一致的数组格式）
        public float[] eye_left;  // 左眼坐标数组 [x, y]
        public float[] eye_right; // 右眼坐标数组 [x, y]
        public float[] nose;      // 鼻子坐标数组 [x, y]

        // 姿态角
        public float roll;  // 翻滚角
        public float yaw;   // 偏航角
        public float pitch; // 俯仰角

        public Gender gender;
        public float age;  //年龄
        public FaceRecognitionEmoji emoji; //表情 0:"Angry", 1:"Disgust", 2:"Fear", 3:"Happy", 4:"Sad", 5:"Surprise", 6:"Neutral"
    }

    // 人脸识别通知信息入口
    [System.Serializable]
    public class FaceRecognitionNotifyInfo
    {
        public YouDooNotifyFaceRecognitionType notifyFaceRecognitionType;
        public string message;
    }

    /// <summary>
    /// 人脸识别的回调。
    /// 简单的数据
    /// </summary>
    [System.Serializable]
    public class FaceRecognitionNotifyInfoMinimalist
    {
        public YouDooNotifyFaceRecognitionType notifyFaceRecognitionType;
        public FaceRecognitionTypeMinimalist[] message;
    }

    /// <summary>
    /// 人脸识别的回调。
    /// 完整的数据
    /// </summary>
    [System.Serializable]
    public class FaceRecognitionNotifyInfoTypeALL
    {
        public YouDooNotifyFaceRecognitionType notifyFaceRecognitionType;
        public FaceRecognitionTypeALL[] message;
    }

    /// <summary>
    /// 人脸识别的回调。
    /// 需要知道 用户的ID与 骨骼的 ID对应起来。
    /// </summary>
    [System.Serializable]
    public class FaceRecognitionNotifyInfoUserOnly
    {
        public YouDooNotifyFaceRecognitionType notifyFaceRecognitionType;
        public FaceRecognitionTypeUserOnly[] message;
    }
}
