/*
作者：Ting
创建时间：2025.10.20
描述：震动
*/
public class HardWareRemoteControlVibrationItem
{
    private string deviceMac;
    public string DeviceMac
    {
        get => deviceMac;
        set => deviceMac = value;
    }

    public void InitVibrationItem(string deviceMacT)
    {
        deviceMac = deviceMacT;
    }

    public virtual void VibrationStop(string deviceMac)
    {
        AndroidServerInfo.Instance.VibrationStop(deviceMac);
    }

    public virtual void VibrationStart(string deviceMac, YouDooSDKConstants.MotorIntensity motorIntensity, int duration)
    {
        AndroidServerInfo.Instance.VibrationStart(deviceMac, motorIntensity, duration);
    }

    public virtual void VibrationShort(string deviceMac)
    {
        AndroidServerInfo.Instance.VibrationShort(deviceMac);
    }

    public virtual void VibrationLong(string deviceMac)
    {
        AndroidServerInfo.Instance.VibrationLong(deviceMac);
    }
 
}