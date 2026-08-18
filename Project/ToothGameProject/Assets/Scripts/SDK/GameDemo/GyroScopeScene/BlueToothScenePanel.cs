using System.Collections;
using YouDooSDK.UI;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DemoUtil;
using static YouDooSDKConstants;

public class BlueToothScenePanel : RemoteInputBase
{
    // [SerializeField] private BluetoothDemo bluetoothDemo;

    [SerializeField] private Image[] imageMotorIntensityState;

    protected override void Start()
    {
        base.Start();
        SetNewButtonImageArray();
        SetButtonState(true);

        if (((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController != null)
        {
            ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.Initialize();
        }

    }

    // protected override void Update()
    // {
    //     if (((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController != null)
    //     {
    //         ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.Tick(); //刷新数据。

    //         var deviceMap = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.HardWareRemoteControlMap;
    //         if (deviceMap != null && deviceMap.Count > 0)
    //         {
    //             int index = 0;
    //             foreach (var kvp in deviceMap)
    //             {
    //                 UpdateGyro(kvp.Value, index);
    //                 index++;
    //             }
    //         }
    //     }
    // }

    public void OnApplicationPause(bool pauseStatus)

    {
        if (((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController != null)
        {
            ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.OnAppPause(pauseStatus);
        }
    }

    protected override void EscapePressedBase()
    {


    }

    protected override void GroupButton1Change()
    {
        // Debug.Log($"24 24 24 点击了 第 {_curSelcetIndex}个 按钮--{bluetoothDemo == null}");
        switch (_curSelcetIndex)
        {
            case 0:
                SetVibrationItemVibrationState(VibrationType.Start);
                break;
            case 1:
                SetVibrationItemVibrationState(VibrationType.Stop);
                break;
            case 2:
                SetVibrationItemVibrationState(VibrationType.Short);
                break;
            case 3:
                SetVibrationItemVibrationState(VibrationType.Long);
                break;
        }
    }

    protected override void GroupButton2Change()
    {

        System.Collections.Generic.Dictionary<string, HardWareRemoteControl> hardWareRemoteControlMap = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.HardWareRemoteControlMap;
        switch (_curSelcetIndex)
        {
            case 0:
                SceneManager.LoadScene(SceneName.TestDemo.ToString());
                break;
            case 1:
                Debug.Log($"81 81 81 获取某个蓝牙设备的电量AAA");
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    AndroidServerInfoDemo.Instance.GetDeviceBatteryLevelAsync(kvp.Value.DeviceMac);
                }
                break;
            case 2:
                Debug.Log($"81 81 81 获取所有蓝牙设备的电量BBB");
                AndroidServerInfoDemo.Instance.RefreshAllBatteryLevelsAsync();
                break;
            case 3:
                bool isVibrationEnabled = AndroidServerInfoDemo.Instance.IsVibrationEnabled();
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_remote_vibration_enabled", isVibrationEnabled));
                break;
            case 4:
                bool isSoundEnabled = AndroidServerInfoDemo.Instance.IsSoundEnabled();
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_remote_speaker_enabled", isSoundEnabled));
                break;
        }
    }

    protected override void GroupButton3Change()
    {
        switch (_curSelcetIndex)
        {
            case 0:
                SetAudioRecording(AudioRecordingType.Start);
                break;
            case 1:
                SetAudioRecording(AudioRecordingType.Stop);
                break;
            case 2:
                SetAudioRecording(AudioRecordingType.GetDirectory);
                break;
            case 3:
                SetAudioRecording(AudioRecordingType.GetFilePath);
                break;
        }
    }

    private int _ledLevel = 1;
    private int _soundEffect = 0;

    private int soundeLength = (int)SoundEffect.SE_Length;

    protected override void GroupButton4Change()
    {
        System.Collections.Generic.Dictionary<string, HardWareRemoteControl> hardWareRemoteControlMap = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.HardWareRemoteControlMap;
        switch (_curSelcetIndex)
        {
            case 0:
                LedLevel ledLevel = (LedLevel)_ledLevel;
                _ledLevel++;
                if (_ledLevel >= (int)LedLevel.LEVEL_Length)
                {
                    _ledLevel = 1;
                }
                Debug.Log($"117 117 117  显示LED灯");
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_led_opened", ledLevel));
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    kvp.Value.LedItem.SetLedLevel(ledLevel);
                }
                break;
            case 1:
                Debug.Log($"117 117 117  关闭LED灯");
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_led_closed"));
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    kvp.Value.LedItem.CloseLed();
                }
                break;
            case 2:
                SoundEffect soundEffect = (SoundEffect)_soundEffect;
                _soundEffect++;
                if (_soundEffect >= soundeLength)
                {
                    _soundEffect = 0;
                }
                Debug.Log($"124 124 124 蜂鸣器");
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_speaker_opened", soundEffect));
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    kvp.Value.SpeakerItem.SetSoundEffect(soundEffect);
                }
                break;
        }
    }

    /// <summary>
    /// 音频的
    /// </summary>
    protected override void GroupButton5Change()
    {
        System.Collections.Generic.Dictionary<string, HardWareRemoteControl> hardWareRemoteControlMap = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.HardWareRemoteControlMap;
        switch (_curSelcetIndex)
        {
            case 0:
                Debug.Log($"169 169 169 文件单个写入！！！");
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).WriteSingleAudio(kvp.Value.DeviceMac, 8, "swing8_final.tab");
                }
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_single_file_written"));
                break;
            case 1:
                Debug.Log($"169 169 169 文件多个写入！！！");
                var audioMap = new System.Collections.Generic.Dictionary<int, string>();
                int writeCount = 0;
                for (int i = 0; i < 50; i++)
                {
                    switch (writeCount)
                    {
                        case 0:
                            audioMap.Add(i, "swing8_final.tab");
                            break;
                        case 1:
                            audioMap.Add(i, "Vocode1_8KHz_Final.tab");
                            break;
                        case 2:
                            audioMap.Add(i, "ppq1.tab");
                            break;
                        case 3:
                            audioMap.Add(i, "ppq2.tab");
                            break;
                    }
                    writeCount++;
                    if (writeCount > 3)
                    {
                        writeCount = 0;
                    }
                }
                Debug.Log($"文件Map构造完成！！！{audioMap.Count}");
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).WriteBatchAudios(kvp.Value.DeviceMac, audioMap);
                }
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_batch_file_written"));
                break;
            case 2:
                Debug.Log($"169 169 169 获取文件列表！！！");
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    int[] res = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).GetAudioIds(kvp.Value.DeviceMac);
                    TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_file_list_count", res.Length));
                }
                break;
            case 3:
                Debug.Log($"169 169 169 获取文件总数量！！！");
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    int count = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).GetAudioCount(kvp.Value.DeviceMac);
                    TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_file_count", count));
                    if (count > _soundEffect)
                    {
                        _soundEffect = count;
                    }
                }
                break;
            case 4:
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).TestAllFeatures(kvp.Value.DeviceMac);
                }
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_check_log"));
                break;
        }
    }

    #region 震动相关测试代码
    private int _motorIntensityTypes = 0;
    private void SetVibrationItemVibrationState(VibrationType vibrationType)
    {
        System.Collections.Generic.Dictionary<string, HardWareRemoteControl> hardWareRemoteControlMap = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.HardWareRemoteControlMap;
        Debug.Log($"221 221 221 点击开始震动了AAAAAAAAA   {hardWareRemoteControlMap.Count}");
        switch (vibrationType)
        {
            case VibrationType.Start:
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    Debug.Log($"221 221 221 点击开始震动了--{kvp}---   {kvp.Value.DeviceMac}");
                    switch (_motorIntensityTypes)
                    {
                        case 0:
                            kvp.Value.VibrationItem.VibrationStart(kvp.Value.DeviceMac, MotorIntensity.LOW, 10000);
                            break;
                        case 1:
                            kvp.Value.VibrationItem.VibrationStart(kvp.Value.DeviceMac, MotorIntensity.MEDIUM, 10000);
                            break;
                        case 2:
                            kvp.Value.VibrationItem.VibrationStart(kvp.Value.DeviceMac, MotorIntensity.HIGH, 10000);
                            break;
                    }
                }
                for (int i = 0; i < imageMotorIntensityState.Length; i++)
                {
                    if (_motorIntensityTypes == i)
                    {
                        imageMotorIntensityState[i].color = Color.green;
                    }
                    else
                    {
                        imageMotorIntensityState[i].color = Color.red;
                    }
                }

                _motorIntensityTypes++;
                if (_motorIntensityTypes > 2)
                {
                    _motorIntensityTypes = 0;
                }
                break;
            case VibrationType.Stop:
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    kvp.Value.VibrationItem.VibrationStop(kvp.Value.DeviceMac);
                }
                break;
            case VibrationType.Short:
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    kvp.Value.VibrationItem.VibrationShort(kvp.Value.DeviceMac);
                }
                break;
            case VibrationType.Long:
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    kvp.Value.VibrationItem.VibrationLong(kvp.Value.DeviceMac);
                }
                break;
        }
    }
    #endregion 

    private void SetAudioRecording(AudioRecordingType audioRecordingType)
    {
        System.Collections.Generic.Dictionary<string, HardWareRemoteControl> hardWareRemoteControlMap = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.HardWareRemoteControlMap;
        Debug.Log($"221 221 221 点击开始录音AAAAAAAAA   {hardWareRemoteControlMap.Count}");
        switch (audioRecordingType)
        {
            case AudioRecordingType.Start:
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    Debug.Log($"221 221 221 点击开始震动了--{kvp}---   {kvp.Value.DeviceMac}");
                    kvp.Value.StartAudioRecording();
                }
                break;
            case AudioRecordingType.Stop:
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    kvp.Value.StopAudioRecording();
                }
                break;
            case AudioRecordingType.GetDirectory:
                foreach (var kvp in hardWareRemoteControlMap)
                {
                    kvp.Value.AudioItem.GetRecordingDirectory();
                }
                break;
            case AudioRecordingType.GetFilePath:
                // foreach (var kvp in hardWareRemoteControlMap)
                // {
                //     kvp.Value.AudioItem.VibrationLong(kvp.Value.DeviceMac);
                // }
                break;
        }
    }
}
