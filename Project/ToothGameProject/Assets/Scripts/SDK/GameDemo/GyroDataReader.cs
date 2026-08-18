using System.Linq;
using UnityEngine;

/// <summary>
/// 单目标陀螺仪数据读取器。
///
/// 说明：
/// 1. 该脚本替代旧的 GyroDataTestDemo 命名。
/// 2. 不再依赖“主手柄”概念，按 AndroidServerInfo.Instance.Bluetooth 中的 controller 顺序读取指定 targetIndex。
/// 3. 主要用于旧 UI 面板、调试 UI、小游戏面板读取当前选中手柄的陀螺仪数据。
/// 4. 多手柄多目标正式驱动请使用 GyroDataImplement。
/// </summary>
public class GyroDataReader : MonoBehaviour
{
    private int targetIndex = 0;

    public int TargetIndex => targetIndex;

    [HideInInspector] public float TimeCount = 0.0f;

    [HideInInspector] public Vector3 GyroData;
    [HideInInspector] public Vector3 DegData;
    [HideInInspector] public Vector3 AcceData;
    [HideInInspector] public Vector3 MagnetData;
    [HideInInspector] public Vector3 CursorPosition;
    [HideInInspector] public Quaternion GyroRotation = Quaternion.identity;

    private void Update()
    {
        TimeCount += Time.deltaTime;
        UpdateGyroData();
    }

    private void UpdateGyroData()
    {
        HardWareRemoteControlGyroItem gyroItem = GetTargetHardWareGyroItem();
        if (gyroItem == null || !gyroItem.IsActive || !gyroItem.IsAvailable)
        {
            return;
        }

        YouDooSDKConstants.GyroData[] gyroDatas = gyroItem.GetDeviceGyroData();
        if (gyroDatas == null || gyroDatas.Length <= 0)
        {
            return;
        }

        YouDooSDKConstants.GyroData latest = gyroDatas.Last();
        GyroData = new Vector3(latest.gyroX, latest.gyroY, latest.gyroZ);
        DegData = GyroData / gyroItem.GyroParams.GyroLsbDeg;
        AcceData = new Vector3(latest.accelX, latest.accelY, latest.accelZ);
        MagnetData = new Vector3(latest.magX, latest.magY, latest.magZ);
        CursorPosition = new Vector3(latest.cursorX, latest.cursorY, 0);
        GyroRotation = new Quaternion(latest.quaternionX, latest.quaternionY, latest.quaternionZ, latest.quaternionW);
    }

    public void SetTargetIndex(int value)
    {
        targetIndex = value;
    }

    public HardWareRemoteControlGyroItem GetTargetHardWareGyroItem()
    {
        if (AndroidServerInfo.Instance == null || AndroidServerInfo.Instance.Bluetooth == null)
        {
            return null;
        }

        HardWareRemoteControl[] controllers = AndroidServerInfo.Instance.Bluetooth.HardWareRemoteControlMap.Values.ToArray();
        if (targetIndex < 0 || targetIndex >= controllers.Length)
        {
            return null;
        }

        return controllers[targetIndex]?.GyroItem;
    }

    public void ResetGyroMappingState()
    {
        if (AndroidServerInfo.Instance == null || AndroidServerInfo.Instance.Bluetooth == null)
        {
            Debug.LogWarning("[GyroDataReader] ResetGyroMappingState: AndroidServerInfo 或 Bluetooth 为空");
            return;
        }

        HardWareRemoteControl[] controllers = AndroidServerInfo.Instance.Bluetooth.HardWareRemoteControlMap.Values.ToArray();
        if (targetIndex < 0 || targetIndex >= controllers.Length)
        {
            Debug.LogWarning($"[GyroDataReader] ResetGyroMappingState: targetIndex={targetIndex} 越界，当前手柄数量={controllers.Length}");
            return;
        }

        controllers[targetIndex]?.GyroItem?.ResetGyroMappingState();
    }
}
