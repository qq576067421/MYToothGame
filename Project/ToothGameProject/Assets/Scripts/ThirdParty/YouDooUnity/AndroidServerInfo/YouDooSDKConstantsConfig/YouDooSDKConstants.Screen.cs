/*
作者：Ting
创建时间：2025.10.07
描述：YouDooSDKConstants 截屏/录屏相关枚举/类 partial class 模块
*/

public static partial class YouDooSDKConstants
{
    /// <summary>
    /// 屏幕截图 的错误码 SDK 4.0  开始支持
    /// 对应 YouDooNotifyScreenCaptureShotType  SCT_ERROR
    /// </summary>
    public enum CaptureScreenshotErrorCode
    {
        SCREENSHOT_IN_PROGRESS = 20000301,        // "截屏正在进行中"
        VIDEO_GENERATION_IN_PROGRESS = 20000302,  // "正在生成视频中，不能截屏"
        SCREENSHOT_EXECUTION_FAILED = 20000303,   // "截图执行失败"
        PIXEL_COPY_FAILED = 20000304,             // "PixelCopy截图失败"
        SAVE_SCREENSHOT_FAILED = 20000305,        // "保存截图失败"
    }

    /// <summary>
    /// 屏幕截图  SDK 4.0  开始支持
    /// </summary>
    public enum YouDooNotifyScreenCaptureShotType
    {
        SCT_START = 200001,                            // 开始截屏
        SCT_SUCCESS = 200002,                          // 截屏成功
        SCT_ERROR = 200003,                            // 截屏报错  对应的是：CaptureScreenshotErrorCode
        SCT_GENERATE_VIDEO_ING = 200004,               // 正在生成视频中
        SCT_GENERATE_VIDEO_START = 200005,             // 开始生成视频
        SCT_GENERATE_VIDEO_PROGRESS_IMAGE = 200006,    // 处理图片的进度
        SCT_GENERATE_VIDEO_PROGRESS_FRAMEENCODED = 200007, // 编码的进度
        SCT_GENERATE_VIDEO_SUCCESS = 200008,           // 生成视频成功
        SCT_GENERATE_VIDEO_ERROR = 200009,             // 生成视频失败
        SCT_GENERATE_VIDEO_CANCEL_SUCCESS = 200010,    // 当我们调用取消生成视频的时候，会回调
        SCT_GENERATE_VIDEO_NO_IMAGE = 200011,          // 没有图片 通常是图片生成视频的时候，如果temp文件夹内本身就没有图，那么就回调
        SCT_GENERATE_VIDEO_NOT_INIT = 200012,          // 没有初始化
        SCT_CLEAR_TEMP_FILES_SUCCESS = 200013,         // 清理临时文件夹成功 直接删除整个文件夹的。
        SCT_CANCEL = 200014,                           // 取消截图的回调
    }

    /// <summary>
    /// 录屏的需求 SDK 4.0  开始支持
    /// </summary>
    public enum YouDooNotifyScreenRecorderType
    {
        // 基础状态
        SRT_START = 210001,                    // 开始录屏
        SRT_RECORDING = 210002,                // 正在录制中（录屏已开始）
        SRT_STOP = 210003,                     // 停止录屏成功
        SRT_NOT_INIT = 210004,                 // 没有初始化，所以这个操作是错的
        SRT_NOT_RECORDING = 210005,            // 不是在录屏中，通常点击停止录屏的时候会判断并返回

        // 权限相关
        SRT_PERMISSION_DENIED = 210006,        // 录屏权限被拒绝
        SRT_PERMISSION_ERROR = 210007,         // 录屏权限申请出错

        // 文件操作相关
        SRT_SAVE_ERROR_NO_FILE = 210008,       // 保存失败,找不到视频文件，需要先录制视频
        SRT_CLEAR_TEMP_FILES_SUCCESS = 210009, // 清理临时文件夹成功
        SRT_SAVED = 210010,                    // 视频保存成功
        SRT_DELETED = 210011,                  // 视频删除成功
        SRT_DELETE_ERROR = 210012,             // 视频删除失败

        // 保存过程相关
        SRT_SAVE_START = 210013,               // 开始保存视频
        SRT_SAVE_PROGRESS = 210014,            // 保存进度更新
        SRT_SAVE_COMPLETE = 210015,            // 保存完成

        // 错误相关
        SRT_ERROR = 210016,                    // 录屏发生错误
        SRT_MERGE_ERROR = 210017,              // 视频合并失败
        SRT_MERGE_SUCCESS = 210018,            // 视频合并成功

        // 合并过程相关
        SRT_MERGE_START = 210019,              // 开始合并视频
        SRT_MERGE_PROGRESS = 210020,           // 合并进度更新

        // 录屏进度相关
        SRT_PROGRESS = 210021                  // 录屏进度更新（时长、文件大小、码率等）
    }

    /// <summary>
    /// 整个截图的入口
    /// </summary>
    [System.Serializable]
    public class YouDooNotifyScreenCaptureShotTypeInfo
    {
        public YouDooNotifyScreenCaptureShotType notifyScreenCaptureShotType;
        public string message;
    }

    /// <summary>
    /// 录屏的入口。
    /// </summary>
    [System.Serializable]
    public class YouDooNotifyScreenRecorderTypeInfo
    {
        public YouDooNotifyScreenRecorderType notifyScreenRecorderType;
        public string message;
    }

    /// <summary>
    /// image合成视频的时候，处理了多少图片的进度。
    /// </summary>
    [System.Serializable]
    public class ScreenCaptureShotProgressImage
    {
        public int total;          //总共图片的数量
        public int current;        //当前是第几张图片
        public float percent;      //进度
        public string imageName;   //图片的名字
    }

    /// <summary>
    /// image合成视频的时候，编码的进度 通常用来做整体的进度会比较准确。
    /// </summary>
    [System.Serializable]
    public class ScreenCaptureShotProgressFrameEncoded
    {
        public int totalFrames;   //总体的帧数
        public int currentFrame;  //当前的帧数
        public float percent;     //当前的进度
    }

    /// <summary>
    /// 视频合成的进度
    /// </summary>
    [System.Serializable]
    public class ScreenRecorderMergeProgress
    {
        public int current;
        public int total;
        public float percent; //当前的进度
    }
}
