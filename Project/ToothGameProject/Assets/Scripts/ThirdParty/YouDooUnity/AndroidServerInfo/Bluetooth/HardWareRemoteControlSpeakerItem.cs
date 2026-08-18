/// 开发者信息：
/// 创建者：Ting
/// 创建日期：2025年11月
/// 版本：1.0
using System;
using UnityEngine;
using static YouDooSDKConstants;

public class HardWareRemoteControlSpeakerItem
{
    private string deviceMac;
    public string DeviceMac
    {
        get => deviceMac;
        set => deviceMac = value;
    }
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="deviceMacT"></param>
    public void InitSpeakerItem(string deviceMacT)
    {
        deviceMac = deviceMacT;
    }

    public void SetSoundEffect(SoundEffect soundEffect)
    {
        AndroidServerInfo.Instance.SetSoundEffect(deviceMac, soundEffect);
    }


}