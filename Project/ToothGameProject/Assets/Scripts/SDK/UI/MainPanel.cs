using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using YouDooUnity;
using static YouDooSDKConstants;

public class MainPanel : MainScene
{
    [SerializeField]
    private BluetoothOnlyUseMajorControllerDemo bluetoothDemo;

    [SerializeField]
    private Text deviceConnectText;  //设备的连接状态。

    [SerializeField]
    private Text deviceMacText;  //设备的唯一地址。

    [SerializeField]
    private Text deviceBatteryLevelText;  //设备的电池电量

    [SerializeField]
    private Image[] imageMenus;

    [SerializeField]
    private Image[] imageDiagnosisMenus;

    private int currentSelectIndex = 0;
    private bool isMainGroupActive = true;

    HardWareRemoteControl device;
    void Start()
    {
        ShowMainPanel(mainPanel.gameObject);
        deviceConnectText.text = "Disconnect";
        deviceMacText.text = "00:00:00:00:00:00";
        deviceBatteryLevelText.text = "0%";
        var firstKey = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.HardWareRemoteControlMap.Keys.ToArray()[0];
        device = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.HardWareRemoteControlMap[firstKey];
        HandleDeviceInfoUpdate();
        isMainGroupActive = true;
        currentSelectIndex = 0;
        UpdateSelection();
    }

    private void HandleDeviceInfoUpdate()
    {
        if (device == null)
        {
            return;
        }
        deviceConnectText.text = "Connected";
        deviceMacText.text = device.DeviceMac;
        // 电池信息是异步返回, 要先注册回调
        // 这里只返回主手柄的电池信息
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].OnGetDeviceBatteryLevelAction += GetBattleLevel;
        device.GetDeviceBatteryLevelAsync();
    }

    void GetBattleLevel(BluetoothNotifyInfo<BatteryInfo> info)
    {
        deviceBatteryLevelText.text = $"{info.message.batteryLevel}%";
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].OnGetDeviceBatteryLevelAction -= GetBattleLevel;
    }

    protected override void OnButtonOKPressed()
    {
        Debug.Log($"Selected Index: {currentSelectIndex}");
        if (isMainGroupActive)
        {
            switch (currentSelectIndex)
            {
                case 0:
                    foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap.Keys)
                    {
                        ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).TestAllFeatures(kvp);
                    }
                    break;
                case 1:
                    ShowMainPanel(devicesPanel.gameObject);
                    break;
                case 2:
                    ShowMainPanel(sprayPaintPanel.gameObject);
                    break;
                case 3:
                    ShowMainPanel(shootingPanel.gameObject);
                    break;
                case 4:
                    ShowMainPanel(audioRecordPanels.gameObject);
                    break;
                case 5:
                    ShowMainPanel(vibrationPanel.gameObject);
                    break;
                case 6:
                    ShowMainPanel(KeyInputPanel.gameObject);
                    break;
            }
        }
        else
        {
            switch (currentSelectIndex)
            {
                case 0:
                    ShowMainPanel(QuickDiagnosisPanel.gameObject);
                    break;
                case 1:
                    ShowMainPanel(PressureTestPanel.gameObject);
                    break;
            }
        }
    }

    protected override void OnEscapePressed()
    {
        // Add escape logic here if needed
    }

    protected override void OnRightArrowPressed()
    {
        SwitchGroup();
    }

    protected override void OnLeftArrowPressed()
    {
        SwitchGroup();
    }

    private void SwitchGroup()
    {
        isMainGroupActive = !isMainGroupActive;
        currentSelectIndex = 0;
        UpdateSelection();
    }

    protected override void OnUpArrowPressed()
    {
        CycleSelection(-1);
    }

    protected override void OnDownArrowPressed()
    {
        CycleSelection(1);
    }

    private void CycleSelection(int step)
    {
        Image[] activeMenus = isMainGroupActive ? imageMenus : imageDiagnosisMenus;
        if (activeMenus == null || activeMenus.Length == 0) return;

        currentSelectIndex += step;
        if (currentSelectIndex < 0) currentSelectIndex = activeMenus.Length - 1;
        if (currentSelectIndex >= activeMenus.Length) currentSelectIndex = 0;

        UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (imageMenus != null)
        {
            for (int i = 0; i < imageMenus.Length; i++)
            {
                if (imageMenus[i] != null) imageMenus[i].color = Color.white;
            }
        }

        if (imageDiagnosisMenus != null)
        {
            for (int i = 0; i < imageDiagnosisMenus.Length; i++)
            {
                if (imageDiagnosisMenus[i] != null) imageDiagnosisMenus[i].color = Color.white;
            }
        }

        Image[] activeMenus = isMainGroupActive ? imageMenus : imageDiagnosisMenus;
        if (activeMenus != null && currentSelectIndex >= 0 && currentSelectIndex < activeMenus.Length)
        {
            if (activeMenus[currentSelectIndex] != null)
                activeMenus[currentSelectIndex].color = Color.green;
        }
    }
}
