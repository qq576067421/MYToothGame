/// 开发者信息：
/// 创建者：Ting
/// 创建日期：2025年11月
/// 版本：1.0
using System;
using UnityEngine;
using static YouDooSDKConstants;

public class HardWareRemoteControlLedItem
{
    private string deviceMac;
    public string DeviceMac
    {
        get => deviceMac;
        set => deviceMac = value;
    }

    public void InitLedItem(string deviceMacT)
    {
        deviceMac = deviceMacT;
    }


    /// <summary>
    /// 设置LED的灯的
    /// </summary>
    /// <param name="deviceMac"></param>
    /// <param name="ledlevelT"></param>
    public virtual void SetLedLevel(LedLevel ledLevel)
    {
        AndroidServerInfo.Instance.SetLedLevel(deviceMac, ledLevel);
    }

    /// <summary>
    /// 关闭LED的灯。
    /// </summary>
    /// <param name="ledLevel"></param>
    public virtual void CloseLed()
    {
        AndroidServerInfo.Instance.CloseLed(deviceMac);
    }
}