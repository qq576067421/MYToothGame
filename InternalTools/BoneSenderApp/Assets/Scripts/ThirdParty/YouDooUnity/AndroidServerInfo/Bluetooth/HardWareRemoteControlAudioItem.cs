using UnityEngine;

public class HardWareRemoteControlAudioItem
{
    private string deviceMac;
    private string savePath;

    public string DeviceMac
    {
        get => deviceMac;
        set => deviceMac = value;
    }
    public string SavePath
    {
        get => savePath;
        set => savePath = value;
    }

    public void InitAudioItem(string deviceMacT, string savePathT)
    {
        deviceMac = deviceMacT;
        savePath = savePathT;

    }
    public void GetRecordingDirectory()
    {
        // Debug.Log($"48 48 48 获取存储目录AA ！！");
        string path = AndroidServerInfo.Instance.GetRecordingDirectory(deviceMac, savePath);
        // Debug.Log($"48 48 48 获取存储目录BB------------{path}---- ！！");
    }
}