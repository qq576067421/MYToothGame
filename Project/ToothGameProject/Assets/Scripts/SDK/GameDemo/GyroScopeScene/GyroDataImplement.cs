using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// GyroScopeScene 场景中的陀螺仪数据显示/驱动脚本。
///
/// 设计目标：
/// 1. 支持一个或多个手柄。
/// 2. 所有手柄地位相同，不需要“主手柄”。
/// 3. deviceMac 由 SDK 注册到 Bluetooth 后自动分配到 bindings。
/// 4. 场景中只需要配置 Cube / Mouse，运行时自动完成：手柄 MAC -> Cube。
/// </summary>
public class GyroDataImplement : MonoBehaviour
{
    [Serializable]
    public class GyroTargetBinding
    {
        [Tooltip("该手柄控制的方块。")]
        public Transform cube;

        [Tooltip("该手柄控制的鼠标/光标对象，可为空。")]
        public Transform mouse;

        [Header("Runtime - 自动赋值")]
        [Tooltip("运行时由 SDK 注册到的手柄 MAC 自动赋值，不需要手动填写。")]
        [ReadOnlyInInspector] public string deviceMac;

        [ReadOnlyInInspector] public Vector3 cursorPosition;
        [ReadOnlyInInspector] public Quaternion gyroRotation = Quaternion.identity;

        [NonSerialized] public bool isAssigned;
        [NonSerialized] public bool hasLoggedNoGyroItem;
        [NonSerialized] public bool hasLoggedNoData;
    }

    [SerializeField] private List<GyroTargetBinding> bindings = new List<GyroTargetBinding>();

    [Header("Options")]
    [SerializeField] private bool useAllBondedDevicesOnStart = false;
    [SerializeField] private bool autoAssignControllers = true;
    [SerializeField] private bool logDetail = true;
    [SerializeField] private float waitControllerLogInterval = 1.0f;
    [SerializeField] private float noDataLogInterval = 1.0f;

    private float nextWaitControllerLogTime;
    private float nextNoDataLogTime;
    private int lastControllerCount = -1;
    private int lastAssignedCount = -1;

    private void Start()
    {
        ClearRuntimeBindings();

        Debug.Log($"[GyroDataImplement] Start: useAllBondedDevicesOnStart={useAllBondedDevicesOnStart}, bindings={bindings.Count}");

        if (useAllBondedDevicesOnStart)
        {
            UseAllBondedDevices();
        }
    }

    private void Update()
    {
        if (AndroidServerInfo.Instance == null || AndroidServerInfo.Instance.Bluetooth == null)
        {
            LogThrottled(ref nextWaitControllerLogTime, waitControllerLogInterval, "[GyroDataImplement] 等待 AndroidServerInfo/Bluetooth 初始化");
            return;
        }

        if (autoAssignControllers)
        {
            AutoAssignControllerMacs();
        }

        foreach (var binding in bindings)
        {
            UpdateBinding(binding);
        }
    }

    private void ClearRuntimeBindings()
    {
        foreach (var binding in bindings)
        {
            if (binding == null)
            {
                continue;
            }

            binding.deviceMac = string.Empty;
            binding.cursorPosition = Vector3.zero;
            binding.gyroRotation = Quaternion.identity;
            binding.isAssigned = false;
            binding.hasLoggedNoGyroItem = false;
            binding.hasLoggedNoData = false;
        }
    }

    private void UseAllBondedDevices()
    {
        if (AndroidServerInfo.Instance == null || AndroidServerInfo.Instance.Bluetooth == null)
        {
            Debug.LogWarning("[GyroDataImplement] Start: AndroidServerInfo 或 Bluetooth 未初始化，暂时无法 UseAllBondedDevices");
            return;
        }

        Debug.Log($"[GyroDataImplement] Start: 请求使用所有已绑定手柄。bindings={bindings.Count}");
        AndroidServerInfo.Instance.Bluetooth.UseAllBondedDevices();
    }

    private void AutoAssignControllerMacs()
    {
        IReadOnlyDictionary<string, HardWareRemoteControl> controllers = AndroidServerInfo.Instance.Bluetooth.GetControllers();
        int controllerCount = controllers?.Count ?? 0;

        if (controllerCount <= 0)
        {
            LogThrottled(ref nextWaitControllerLogTime, waitControllerLogInterval,
                $"[GyroDataImplement] 等待手柄注册: 当前 controllers=0, bindings={bindings.Count}");
            return;
        }

        if (controllerCount != lastControllerCount)
        {
            lastControllerCount = controllerCount;
            Debug.Log($"[GyroDataImplement] 当前已注册手柄数量: {controllerCount}");
            foreach (var kvp in controllers)
            {
                Debug.Log($"[GyroDataImplement] 已注册手柄 MAC: {kvp.Key}, HasGyro={kvp.Value?.HasGyro}, IsConnected={kvp.Value?.IsConnected}");
            }
        }

        HashSet<string> alreadyAssigned = new HashSet<string>(
            bindings
                .Where(binding => binding != null && binding.isAssigned && !string.IsNullOrEmpty(binding.deviceMac))
                .Select(binding => Bluetooth.NormalizeDeviceMac(binding.deviceMac))
        );

        List<string> availableMacs = controllers.Keys
            .Select(Bluetooth.NormalizeDeviceMac)
            .Where(mac => !string.IsNullOrEmpty(mac) && !alreadyAssigned.Contains(mac))
            .ToList();

        for (int i = 0; i < bindings.Count; i++)
        {
            GyroTargetBinding binding = bindings[i];
            if (binding == null || binding.isAssigned)
            {
                continue;
            }

            if (availableMacs.Count <= 0)
            {
                break;
            }

            string mac = availableMacs[0];
            availableMacs.RemoveAt(0);

            binding.deviceMac = mac;
            binding.isAssigned = true;
            binding.hasLoggedNoGyroItem = false;
            binding.hasLoggedNoData = false;

            string cubeName = binding.cube != null ? binding.cube.name : "null";
            string mouseName = binding.mouse != null ? binding.mouse.name : "null";
            Debug.Log($"[GyroDataImplement] 自动绑定: binding[{i}] Cube={cubeName}, Mouse={mouseName} <= MAC={mac}");
        }

        int assignedCount = bindings.Count(binding => binding != null && binding.isAssigned);
        if (assignedCount != lastAssignedCount)
        {
            lastAssignedCount = assignedCount;
            Debug.Log($"[GyroDataImplement] 绑定进度: assigned={assignedCount}/{bindings.Count}, controllers={controllerCount}");
        }
    }

    private void UpdateBinding(GyroTargetBinding binding)
    {
        if (binding == null || !binding.isAssigned)
        {
            return;
        }

        string mac = Bluetooth.NormalizeDeviceMac(binding.deviceMac);
        if (string.IsNullOrEmpty(mac))
        {
            return;
        }

        if (!AndroidServerInfo.Instance.Bluetooth.TryGetController(mac, out HardWareRemoteControl controller))
        {
            binding.isAssigned = false;
            binding.deviceMac = string.Empty;
            Debug.LogWarning($"[GyroDataImplement] 已绑定手柄 {mac} 不再存在，解除该 binding，等待重新分配");
            return;
        }

        if (controller == null || controller.GyroItem == null)
        {
            if (!binding.hasLoggedNoGyroItem)
            {
                Debug.LogWarning($"[GyroDataImplement] MAC={mac} Controller 或 GyroItem 为空。ControllerNull={controller == null}");
                binding.hasLoggedNoGyroItem = true;
            }

            return;
        }

        YouDooSDKConstants.GyroData[] gyroDatas = controller.GyroItem.GetDeviceGyroData();
        if (gyroDatas == null || gyroDatas.Length <= 0)
        {
            if (!binding.hasLoggedNoData && Time.time >= nextNoDataLogTime)
            {
                Debug.LogWarning($"[GyroDataImplement] MAC={mac} 暂无陀螺仪数据。IsConnected={controller.IsConnected}, IsInitialized={controller.IsInitialized}, IsGyroActive={controller.GyroItem.IsActive}");
                binding.hasLoggedNoData = true;
                nextNoDataLogTime = Time.time + noDataLogInterval;
            }

            return;
        }

        binding.hasLoggedNoData = false;

        YouDooSDKConstants.GyroData latest = gyroDatas.Last();

        binding.cursorPosition = new Vector3(latest.cursorX, latest.cursorY, 0);
        binding.gyroRotation = new Quaternion(
            latest.quaternionX,
            latest.quaternionY,
            latest.quaternionZ,
            latest.quaternionW
        );

        if (binding.mouse != null)
        {
            binding.mouse.localPosition = binding.cursorPosition;
        }

        if (binding.cube != null)
        {
            binding.cube.rotation = binding.gyroRotation;
        }

        if (logDetail)
        {
            // 只在第一次拿到数据时打印关键状态，避免刷屏。
            logDetail = false;
            Debug.Log($"[GyroDataImplement] 首次驱动成功: MAC={mac}, Cursor={binding.cursorPosition}, Quaternion={binding.gyroRotation}");
        }
    }

    private void LogThrottled(ref float nextLogTime, float interval, string message)
    {
        if (Time.time < nextLogTime)
        {
            return;
        }

        Debug.Log(message);
        nextLogTime = Time.time + interval;
    }
}

/// <summary>
/// 只读 Inspector 显示标记。
/// 当前不提供自定义 PropertyDrawer 时，不影响运行，仅作为字段语义说明。
/// </summary>
public class ReadOnlyInInspectorAttribute : PropertyAttribute
{
}
