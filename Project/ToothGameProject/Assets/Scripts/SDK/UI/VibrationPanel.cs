using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static YouDooSDKConstants;

public class VibrationPanel : MainScene
{
    [SerializeField]
    private Text vibrationInfoText;  //设备的连接状态。

    [SerializeField]
    private Button[] buttonImages; // 0:开始, 1:停止, 2:加时, 3:改级

    private BluetoothOnlyUseMajorControllerDemo bluetoothDemo;  //设备的电池电量

    private int _vibrationDuration = 2000;  //默认震动持续时间，单位为毫秒。

    private MotorIntensity _vibrationLevel = MotorIntensity.LOW;  //默认震动等级。

    private int _curButtonIndex = 0;  //当前选中的按钮索引，0表示开始震动，1表示停止震动，2表示增加震动时间，3表示改变震动等级。


    private readonly int MaxVibrationDuration = 10000; //最大震动时间20秒

    private readonly int perAddVibrationDuration = 2000; //每次增加5秒

    Dictionary<string, HardWareRemoteControl> hardWareRemoteControlMap;



    // Start is called before the first frame update
    void Start()
    {
        bluetoothDemo = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController;
        hardWareRemoteControlMap = bluetoothDemo.HardWareRemoteControlMap;
        UpdateHighlight();
        ShowVibrationInfo();
    }

    protected override void OnButtonOKPressed()
    {
        Debug.Log($"Selected Index: {_curButtonIndex}");
        switch (_curButtonIndex)
        {
            case 0:
                StartVibration();
                ShowVibrationInfo();
                break;
            case 1:
                AddVibrationTime();
                ShowVibrationInfo();
                break;
            case 2:
                ChangeVibrationLevel();
                ShowVibrationInfo();
                break;
            case 3:
                StopVibration();
                CancelInfoMethod();
                break;
            default:
                break;
        }
    }

 

    protected override void OnRightArrowPressed()
    {
        _curButtonIndex++;
        if (_curButtonIndex >= buttonImages.Length)
        {
            _curButtonIndex = 0; // Loop back to the first menu
        }
        UpdateHighlight();
    }

    protected override void OnLeftArrowPressed()
    {
        _curButtonIndex--;
        if (_curButtonIndex < 0)
        {
            _curButtonIndex = buttonImages.Length - 1; // Loop back to the last menu
        }
        UpdateHighlight();
    }

    protected override void OnUpArrowPressed()
    {
        // 2->0, 3->1, 4->2
        if (_curButtonIndex == 0) _curButtonIndex = 2;
        else if (_curButtonIndex == 1) _curButtonIndex = 3;
        else if (_curButtonIndex == 2) _curButtonIndex = 0;
        else if (_curButtonIndex == 3) _curButtonIndex = 1;
        UpdateHighlight();
    }

    protected override void OnDownArrowPressed()
    {
        // 0->2, 1->3, 2->4, 3->4
        if (_curButtonIndex == 0) _curButtonIndex = 2;
        else if (_curButtonIndex == 1) _curButtonIndex = 3;
        else if (_curButtonIndex == 2) _curButtonIndex = 0;
        else if (_curButtonIndex == 3) _curButtonIndex = 1;
        UpdateHighlight();
    }



    private void UpdateHighlight()
    {
        if (buttonImages == null) return;

        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (buttonImages[i] != null)
            {
                buttonImages[i].GetComponent<Image>().color = (i == _curButtonIndex) ? Color.green : Color.white;
            }
        }
    }

    public void StartVibration()
    {
        foreach (var kvp in hardWareRemoteControlMap)
        {
            kvp.Value.VibrationItem.VibrationStart(kvp.Value.DeviceMac, _vibrationLevel, _vibrationDuration);
        }
        Invoke("CancelInfoMethod", _vibrationDuration / 1000f);
    }

    public void AddVibrationTime()
    {
        _vibrationDuration += perAddVibrationDuration;
        if (_vibrationDuration > MaxVibrationDuration)
        {
            _vibrationDuration = perAddVibrationDuration; //重置为初始值
        }
    }

    public void ChangeVibrationLevel()
    {
        _vibrationLevel = _vibrationLevel switch
        {
            MotorIntensity.OFF => MotorIntensity.LOW,
            MotorIntensity.LOW => MotorIntensity.MEDIUM,
            MotorIntensity.MEDIUM => MotorIntensity.HIGH,
            MotorIntensity.HIGH => MotorIntensity.OFF,
            _ => MotorIntensity.OFF
        };
        if (_vibrationLevel == MotorIntensity.OFF)
        {
            _vibrationLevel = MotorIntensity.LOW;
        }
    }

    public void StopVibration()
    {
        foreach (var kvp in hardWareRemoteControlMap)
        {
            kvp.Value.VibrationItem.VibrationStop(kvp.Value.DeviceMac);
        }
    }

    public void ShowVibrationInfo()
    {
        vibrationInfoText.text = $"震动等级：{(int)_vibrationLevel}级\n持续时间：{_vibrationDuration / 1000f}s";
    }

    private void CancelInfoMethod()
    {
        vibrationInfoText.text = "";
    }
}
