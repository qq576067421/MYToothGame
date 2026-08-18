/*
作者：Ting
创建时间：2025.10.07
描述：AndroidServerInfo NFC partial class 模块
*/
using System;
using UnityEngine;
using static YouDooSDKConstants;

public partial class AndroidServerInfo
{
    #region NFC相关的Android接口
    /// <summary>
    /// SDK 4.1 NFC重构
    /// 获取NFC卡片信息
    /// IYMS_SERVICE_CONNECTED  在这个之后才能get
    /// </summary>
    /// <returns>卡片信息，如果没有则返回null</returns> 
    public NFCCardInfo GetNfc()
    {
        string temp = _pluginInstance.Call<string>("getNfc");
        if (!string.IsNullOrEmpty(temp) && temp != "")
        {
            try
            {
                NFCCardInfo cardInfo = JsonUtility.FromJson<NFCCardInfo>(temp);
                return cardInfo;
            }
            catch (Exception e)
            {
                Debug.LogError($"getNfc 解析JSON失败: {e.Message}, 原始数据: {temp}");
            }
        }
        return null;
    }
    #endregion

    #region NFC消息处理 
    /// <summary>
    /// NFC 数据的读取
    /// </summary>
    /// <param name="messageJson"></param>
    private void HandleNFCServiceNotifyInfo(string messageJson)
    {
        OnNFCServiceNotifyInfo(messageJson);
    }

    #endregion


    protected virtual void OnNFCServiceNotifyInfo(string message)
    { 
        Debug.Log($"NFC消息处理: {message}"); 
    }
}
