using System.Collections;
using YouDooSDK.UI;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DemoUtil;
using static YouDooSDKConstants;

public class GyroScopeScnePanel : RemoteInputBase
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

    public void OnApplicationPause(bool pauseStatus)

    {
        if (((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController != null)
        {
            ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.OnAppPause(pauseStatus);
        }
    }

    protected override void EscapePressedBase()
    {
        SceneManager.LoadScene(SceneName.TestDemo.ToString());
    }
}
