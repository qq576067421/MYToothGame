/*
作者：Ting
创建时间：2025.10.07
修改时间：2026.04.15
描述：AndroidServerInfo 骨骼/帧数据服务 partial class 模块
     人脸识别和距离检测已拆分到 AndroidServerInfo.FaceRecognition.cs
*/
using System;
using UnityEngine;
using static YouDooSDKConstants;

public partial class AndroidServerInfo
{
    #region 骨骼/帧数据相关的Android接口
    /// <summary>
    /// 骨骼：
    /// 如果需要处理的是 骨骼 图片 相关，就得要绑定服务
    /// </summary>
    /// <returns></returns>
    public void BindFrameInfoGameService()
    {
        _pluginInstance.Call("bindFrameInfoGameService");
    }
    /// <summary>
    /// 骨骼：
    /// 解绑骨骼游戏服务。 
    /// </summary>
    public void UnBindFrameInfoGameService()
    {
        _pluginInstance.Call("unbindFrameInfoGameService");
    }
    /// <summary>
    /// 注册帧回调 - 开始接收数据
    /// </summary>
    public bool RegisterFrameCallback()
    {
        return _pluginInstance.Call<bool>("registerFrameInfoCallback");
    }
    /// <summary>
    /// 取消注册帧回调 - 停止接收数据
    /// </summary>
    public bool UnregisterFrameCallback()
    {
        return _pluginInstance.Call<bool>("unregisterFrameInfoCallback");
    }


    #region 模型选择相关的Android接口
    /// <summary>
    /// 获取全部的模型的详细信息 
    /// </summary>
    public string GetGameServiceConfigAll()
    {
        return _pluginInstance.Call<string>("getGameServiceConfigAll");
    }
    /// <summary>
    /// 设置我需要用到的模型。
    /// </summary>
    /// <param name="targetStr"></param>
    public bool SetCurUseMode(string targetStr)
    {
        Debug.Log("我需要用这个配置=" + targetStr);
        return _pluginInstance.Call<bool>("setGameServiceConfig", targetStr);
    }
    #endregion

    /// <summary>
    /// 设置需要跟踪的人员列表。
    /// 最多 4个人的手。  当前 facePid 没有跟踪这一说。
    /// @param lHandPid 左手对应的人员 ID 数组，可为长度 0。
    /// @param rHandPid 右手对应的人员 ID 数组，可为长度 0。
    /// @param facePid  脸部对应的人员 ID 数组，可为长度 0。
    /// @return 0 表示设置成功，其他数值表示错误码。
    /// @Since 3.0
    /// 若传入空数组表示不跟踪对应部位。
    /// 若传入非空数组则表示只跟踪这些 ID。
    /// 可动态切换跟踪ID。
    /// 示例：
    /// setTrackHandsAndFace([0,2], [1,2], [0,1,2])
    /// 表示：
    ///   左手跟踪 person 0、2
    ///   右手跟踪 person 1、2
    ///   脸部跟踪所有人（0、1、2）
    /// 示例：setTrackHandsAndFace(null，null，null)
    /// 关闭了所有的模型
    /// 示例：setTrackHandsAndFace([有值],[有值],null)
    ///  表示关闭了脸的模型。
    /// </summary>
    public int SetTrackHandsAndFace(int[] lHandPid, int[] rHandPid, int[] facePid)
    {
        int isOk = _pluginInstance.Call<int>("setTrackHandsAndFace", lHandPid, rHandPid, facePid);
        return isOk;
    }

    public void SetPlayerID(int[] playerID)
    {
        _pluginInstance.Call("setPlayerID", playerID);
    }
    #endregion



    /// <summary>
    /// 帧信息:
    /// 帧信息服务连接成功
    /// </summary>
    protected virtual void OnFrameInfoServiceConnected(string message)
    {
        isFrameInfoServerIsConnet = true;
        Debug.Log($"帧信息服务已连接: {message}");
        // 在这里处理服务连接成功后的逻辑 
    }

    /// <summary>
    /// 帧信息:
    /// 帧信息服务断开连接
    /// </summary>
    protected virtual void OnFrameInfoServiceDisconnected(string message)
    {
        isFrameInfoServerIsConnet = false;
        Debug.Log($"帧信息服务已断开: {message}");
        // 在这里处理服务断开后的逻辑
    }

    /// <summary>
    /// 帧信息:
    /// 帧信息服务正在绑定中
    /// </summary>
    protected virtual void OnFrameInfoServiceBinding(string message)
    {
        isFrameInfoServerIsConnet = false;

    }

    /// <summary>
    /// 帧信息:
    /// 帧信息服务绑定失败
    /// </summary>
    protected virtual void OnFrameInfoServiceBindFailed(string message)
    {
        isFrameInfoServerIsConnet = false;
        Debug.Log($"帧信息服务绑定失败: {message}");
        // 在这里处理服务绑定失败的逻辑
    }

    /// <summary>
    /// 获取双手光标信息。
    /// 该接口从SDK服务端获取指定用户的双手光标状态，包括左右手位置和按压状态。
    /// 对应Java层的 getHandsCursorInfo 方法。
    /// </summary>
    /// <param name="id">
    /// 用户ID。
    /// 指定要获取哪个用户的光标信息。
    /// </param>
    /// <param name="cursorSpeed">
    /// 光标移动速度倍率。
    /// 1.0 表示默认速度；
    /// <1.0 表示加快光标移动；最小 0.5
    /// >1.0 表示降低光标移动速度；最大 2.0
    /// </param>
    /// <returns>
    /// 返回 HandsCursorData 对象，包含双手光标信息。
    /// 如果调用失败或返回null，则返回null。
    /// 
    /// 成功时，可通过以下方式使用：
    /// - data.IsSuccess 判断是否成功
    /// - data.IsLeftPressed / data.IsRightPressed 判断左右手是否按下
    /// - data.LeftPosition / data.RightPosition 获取左右手位置（Vector2）
    /// 
    /// 失败时，可通过 data.code 获取错误码：
    /// 0  -> 成功
    /// -1 -> 无效参数
    /// -2 -> 找不到对应ID
    /// </returns>
    /// <example>
    /// 使用示例：
    /// <code>
    /// HandsCursorData data = GetHandsCursorInfo(0, 1.0);
    /// if (data != null && data.IsSuccess)
    /// {
    ///     if (data.IsLeftPressed)
    ///     {
    ///         Vector2 leftPos = data.LeftPosition;
    ///         Debug.Log($"左手按下，位置：{leftPos}");
    ///     }
    ///     
    ///     if (data.IsRightPressed)
    ///     {
    ///         Vector2 rightPos = data.RightPosition;
    ///         Debug.Log($"右手按下，位置：{rightPos}");
    ///     }
    /// }
    /// else if (data != null)
    /// {
    ///     Debug.LogError($"获取失败，错误码：{data.code}");
    /// }
    /// else
    /// {
    ///     Debug.LogError("获取光标信息失败，返回null");
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// 注意事项：
    /// 1. 该方法可能会被高频调用，建议复用返回的HandsCursorData对象
    /// 2. 坐标系说明：原点(0,0)在屏幕左上角，X轴向右递增，Y轴向下递增
    /// 3. 所有位置坐标范围均为0~1，表示屏幕相对位置
    /// 4. leftPress/rightPress 返回的是int类型，1表示按下，0表示未按下
    /// 5. 建议使用辅助属性 IsLeftPressed/IsRightPressed 和 LeftPosition/RightPosition
    /// </remarks>
    public HandsCursorData GetHandsCursorInfo(int id, double cursorSpeed)
    {
        string temp = _pluginInstance.Call<string>("getHandsCursorInfo", id, cursorSpeed);
        if (temp != null)
        {
            return JsonUtility.FromJson<HandsCursorData>(temp);
        }
        return null;
    }

    /// <summary>
    /// 帧信息：
    /// 处理帧信息服务通知信息
    /// </summary>
    private void HandleFrameInfoServiceNotifyInfo(string messageJson)
    {
        FrameInfoServerNotifyInfo serviceInfo = JsonUtility.FromJson<FrameInfoServerNotifyInfo>(messageJson);
        if (serviceInfo != null)
        {
            YouDooNotifyFrameInfoServiceConnectionType connectionType = (YouDooNotifyFrameInfoServiceConnectionType)serviceInfo.notifyFrameInfoServiceConnectionType;

            switch (connectionType)
            {
                case YouDooNotifyFrameInfoServiceConnectionType.SERVICE_CONNECTED:
                    Debug.Log("帧信息服务连接成功");
                    // 处理服务连接成功逻辑
                    OnFrameInfoServiceConnected(serviceInfo.message);
                    break;
                case YouDooNotifyFrameInfoServiceConnectionType.SERVICE_DISCONNECTED:
                    Debug.Log("帧信息服务断开连接");
                    // 处理服务断开逻辑
                    OnFrameInfoServiceDisconnected(serviceInfo.message);
                    break;
                case YouDooNotifyFrameInfoServiceConnectionType.SERVICE_BINDING:
                    Debug.Log("帧信息服务正在绑定");
                    // 处理服务绑定中逻辑
                    OnFrameInfoServiceBinding(serviceInfo.message);
                    break;
                case YouDooNotifyFrameInfoServiceConnectionType.SERVICE_BIND_FAILED:
                    Debug.Log("帧信息服务绑定失败");
                    // 处理服务绑定失败逻辑
                    OnFrameInfoServiceBindFailed(serviceInfo.message);
                    break;
                default:
                    Debug.LogWarning($"未知帧信息服务连接类型: {connectionType}");
                    break;
            }
        }

    }
}
