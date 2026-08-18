using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static YouDooSDKConstants;

public class DevicesPanel : MainScene
{

    private BluetoothOnlyUseMajorControllerDemo bluetoothDemo;

    [FormerlySerializedAs("gyroDataTestDemo")]
    [SerializeField] GyroDataReader gyroDataReader;
    [SerializeField] GameObject gyroClub;
    [SerializeField] Camera gyroClubCamera;
    [SerializeField] RenderTexture gyroClubRT;

    [SerializeField]
    private Text gyroGyroXText;  //陀螺仪X轴数据。

    [SerializeField]
    private Text gyroGyroYText;  //陀螺仪Y轴数据。

    [SerializeField]
    private Text gyroGyroZText;  //陀螺仪Z轴数据。


    [SerializeField]
    private Text gyroAcceXText;  //加速度计X轴数据。

    [SerializeField]
    private Text gyroAcceYText;  //加速度计Y轴数据。

    [SerializeField]
    private Text gyroAcceZText;  //加速度计Z轴数据。
    [SerializeField]
    private Text magnetXText;   //磁力计X轴数据。
    [SerializeField]
    private Text magnetYText;   //磁力计Y轴数据。
    [SerializeField]
    private Text magnetZText;   //磁力计Z轴数据。

    void Start()
    {
        bluetoothDemo = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        AndroidServerInfoDemo.Instance.SetHardWareRemoteControlConfig(true, false, false, false, false, (int)FilterType.NONE);
        gyroDataReader.ResetGyroMappingState();
    }

    protected override void OnLeftArrowPressed()
    {
        base.OnLeftArrowPressed();
        gyroDataReader.ResetGyroMappingState();
    }

    void Update()
    {
        if (gyroDataReader != null)
        {
            var gyroData = gyroDataReader.GyroData;
            gyroGyroXText.text = gyroData.x.ToString();
            gyroGyroYText.text = gyroData.y.ToString();
            gyroGyroZText.text = gyroData.z.ToString();

            var acceData = gyroDataReader.AcceData;
            gyroAcceXText.text = acceData.x.ToString();
            gyroAcceYText.text = acceData.y.ToString();
            gyroAcceZText.text = acceData.z.ToString();

            var magnetData = gyroDataReader.MagnetData;
            magnetXText.text = magnetData.x.ToString();
            magnetYText.text = magnetData.y.ToString();
            magnetZText.text = magnetData.z.ToString();
        }
        UpdateGyroClubRotation();

        RenderTexture.active = gyroClubRT;
        GL.Clear(true, true, gyroClubCamera.backgroundColor);
        RenderTexture.active = null;

        gyroClubCamera.targetTexture = gyroClubRT;
        gyroClubCamera.Render();
        gyroClubCamera.targetTexture = null;
    }

    void UpdateGyroClubRotation()
    {
        if (gyroClub == null) return;
        if (gyroDataReader == null) return;
        gyroClub.transform.rotation = gyroDataReader.GyroRotation;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        AndroidServerInfoDemo.Instance.SetHardWareRemoteControlConfig(false, true, true, false, true, (int)FilterType.NONE);
    }
}
