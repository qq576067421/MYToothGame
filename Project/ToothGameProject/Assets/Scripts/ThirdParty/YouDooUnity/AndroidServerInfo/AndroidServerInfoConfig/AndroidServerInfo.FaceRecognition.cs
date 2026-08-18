/*
作者：Ting
创建时间：2026.04.15（从 AndroidServerInfo.FrameInfo.cs 拆分而来）
描述：AndroidServerInfo 人脸识别 + 距离检测 partial class 模块
*/
using System;
using UnityEngine;
using static YouDooSDKConstants;

public partial class AndroidServerInfo
{
    #region 人脸识别相关的Android接口
    /// <summary>
    /// 开始使用人脸识别 需要在服务绑定成功后才能调用。
    /// 人脸识别消耗比较大,因此并没有每一帧都识别。
    /// 当正在识别最新的这一帧的数据的时候，后面来的帧的数据都不会去做处理。
    /// 当识别完一针，就会等当前最新的这一帧的数据来到之后，去识别。
    /// 一张图片识别的时间（基于4K的分辨率） 100ms 左右。 
    /// 使用人脸识别的时候，最好使用最大的分辨率。
    /// </summary>
    public void StartFaceRecognitionCurrentFrameInfo()
    {
        _pluginInstance.Call("startFaceRecognitionCurrentFrameInfo");
    }

    /// <summary>
    /// 停止人脸识别。人脸识别消耗比较大。  
    /// 在游戏中不需要人脸识别的时候 记得关闭。
    /// </summary>
    public void StopFaceRecognitionCurrentFrameInfo()
    {
        _pluginInstance.Call("stopFaceRecognitionCurrentFrameInfo");
    }

    /// <summary>
    /// 当计算两张脸是否是同一个脸的时候，是计算相似度的结果，当 >=这个值，就认为是同一张脸
    /// </summary>
    public void SetFaceRecognitionThreshold(float faceRecognitionThreshold)
    {
        _pluginInstance.Call("setFaceRecognitionThreshold", faceRecognitionThreshold);
    }

    /// <summary>
    /// 需要回调数据的类型是哪个。
    /// 需要结合数据量的大写和业务的数据来处理。
    /// </summary>
    public void SetYouDooNotifyFaceRecognitionType(YouDooNotifyFaceRecognitionType ydnfrt)
    {
        _pluginInstance.Call("setYouDooNotifyFaceRecognitionType", (int)ydnfrt);
    }

    /// <summary>
    /// 获得人脸的相似度
    /// </summary>
    public float GetFaceInfoCosSim(float[] input1, float[] input2)
    {
        return _pluginInstance.Call<float>("getFaceInfoCosSim", input1, input2);
    }

    /// <summary>
    /// 判断两张人脸是同一个人
    /// </summary>
    public bool CheckFaceInfoCosSim(float[] input1, float[] input2)
    {
        return _pluginInstance.Call<bool>("checkFaceInfoCosSim", input1, input2);
    }



    /// <summary>
    /// 获取指定ID的人脸大小（像素单位）。
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>
    /// > 0  -> 成功，返回人脸大小（像素）
    /// -1.0 -> 无效参数
    /// -2.0 -> 找不到对应ID
    /// </returns>
    public double GetFaceSizePixel(int id)
    {
        try
        {
            double temp = _pluginInstance.Call<double>("getFaceSizePixel", id);
            return temp;
        }
        catch (Exception e)
        {
            Debug.LogError($"调用GetFaceSizePixel失败: {e.Message}");
            return -1.0;
        }
    }

    /// <summary>
    /// 切换当前角色
    /// </summary>
    /// <param name="userIds">需要排除的玩家 一般情况传入空数组</param>
    public void SelectRole(long[] userIds)
    {
        _pluginInstance.Call("selectRole", userIds);
    }
    #endregion

    #region 距离检测
    /// <summary>
    /// 距离检测：开启距离检测
    /// 默认就是开启距离检测的。
    /// </summary>
    public void NeedCheckTooClose(bool canCheck)
    {
        _pluginInstance.Call("needCheckTooClose", canCheck);
    }

    /// <summary>
    /// 距离检测：检查人脸面积是否太大的时候，可以设定一个阈值
    /// 默认是0.5
    /// </summary>
    public void SetMaximalArea(float value)
    {
        _pluginInstance.Call("setMaximalArea", value);
    }

    /// <summary>
    /// 距离检测：间隔多少帧才检测一次
    /// 默认是 30帧检测一次
    /// </summary>
    public void CheckTooCloseCounterInterval(int interval)
    {
        _pluginInstance.Call("checkTooCloseCounterInterval", interval);
    }
    #endregion

    #region 人脸识别消息处理
    /// <summary>
    /// 人脸识别相关的回调。基类负责 JSON 解析和路由，子类覆写 On 方法定制行为。
    /// </summary>
    private void HandleFaceRecognitionCallback(string messageJson)
    {
        Debug.Log($"[人脸识别] HandleFaceRecognitionCallback: {messageJson}");
        FaceRecognitionNotifyInfo notifyInfo = JsonUtility.FromJson<FaceRecognitionNotifyInfo>(messageJson);
        if (notifyInfo == null || string.IsNullOrEmpty(notifyInfo.message)) return;

        YouDooNotifyFaceRecognitionType type = notifyInfo.notifyFaceRecognitionType;
        string wrappedMessage = "{\"message\":" + notifyInfo.message + "}";

        switch (type)
        {
            case YouDooNotifyFaceRecognitionType.FRT_USER_ONLY:
                {
                    var parsed = JsonUtility.FromJson<FaceRecognitionNotifyInfoUserOnly>(wrappedMessage);
                    if (parsed?.message != null)
                    {
                        foreach (var user in parsed.message)
                        {
                            if (user.userIds != null && user.userIds.Length > 0)
                            {
                                long newestUserId = PlayerRoleManager.Instance.GetNewestFaceDataUserId(user.userIds);
                                OnFaceRecognizedUserOnly(user.personId, newestUserId, user);
                            }
                        }
                    }
                    break;
                }
            case YouDooNotifyFaceRecognitionType.FRT_MINIMALIST:
                {
                    var parsed = JsonUtility.FromJson<FaceRecognitionNotifyInfoMinimalist>(wrappedMessage);
                    if (parsed?.message != null)
                    {
                        foreach (var user in parsed.message)
                        {
                            if (user.userIds != null && user.userIds.Length > 0)
                            {
                                long newestUserId = PlayerRoleManager.Instance.GetNewestFaceDataUserId(user.userIds);
                                OnFaceRecognizedMinimalist(user.personId, newestUserId, user);
                            }
                        }
                    }
                    break;
                }
            case YouDooNotifyFaceRecognitionType.FRT_ALL:
                {
                    var parsed = JsonUtility.FromJson<FaceRecognitionNotifyInfoTypeALL>(wrappedMessage);
                    if (parsed?.message != null)
                    {
                        foreach (var user in parsed.message)
                        {
                            if (user.userIds != null && user.userIds.Length > 0)
                            {
                                long newestUserId = PlayerRoleManager.Instance.GetNewestFaceDataUserId(user.userIds);
                                OnFaceRecognizedAll(user.personId, newestUserId, user);
                            }
                        }
                    }
                    break;
                }
        }
    }
    #endregion

    #region 人脸识别 On 虚方法（子类覆写用）
    /// <summary>人脸识别回调（FRT_USER_ONLY 类型）</summary>
    protected virtual void OnFaceRecognizedUserOnly(int personId, long newestUserId, FaceRecognitionTypeUserOnly data) { }
    /// <summary>人脸识别回调（FRT_MINIMALIST 类型）</summary>
    protected virtual void OnFaceRecognizedMinimalist(int personId, long newestUserId, FaceRecognitionTypeMinimalist data) { }
    /// <summary>人脸识别回调（FRT_ALL 类型）</summary>
    protected virtual void OnFaceRecognizedAll(int personId, long newestUserId, FaceRecognitionTypeALL data) { }
    #endregion
}
