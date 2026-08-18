using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static YouDooSDKConstants;

public class PressureTestPanel : MainScene
{
    private BluetoothOnlyUseMajorControllerDemo bluetoothDemo;

    [Header("UI References")]
    [SerializeField]
    private Image[] imageMenus; // 0: 连续震动, 1: 高频数据, 2: 综合压测

    [SerializeField]
    private Text statusText;    // 显示测试状态和结果

    [SerializeField]
    private Text gyroInfoText;  // 专门用于综合压测时显示陀螺仪数据

    private int currentSelectIndex = 0;
    private bool isTesting = false;
    private Coroutine currentTestRoutine;

    // 高频数据统计
    private Dictionary<string, int> devicePacketCounts = new Dictionary<string, int>();
    private Dictionary<string, int> deviceTotalCalls = new Dictionary<string, int>();
    private float testStartTime;

    private int oldFrameRate;
    private int oldVSync;
    private bool hasModifiedSettings = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateHighlight();
        ResetUI();
        bluetoothDemo = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopCurrentTest();
    }

    private void ResetUI()
    {
        if (statusText != null) statusText.text = "请选择测试项目按 OK 开始";
        if (gyroInfoText != null) gyroInfoText.text = "";
    }

    // --- 导航逻辑 ---

    protected override void OnUpArrowPressed()
    {
        if (isTesting) return; // 测试中锁定导航
        currentSelectIndex--;
        if (currentSelectIndex < 0) currentSelectIndex = imageMenus.Length - 1;
        UpdateHighlight();
    }

    protected override void OnDownArrowPressed()
    {
        if (isTesting) return; // 测试中锁定导航
        currentSelectIndex++;
        if (currentSelectIndex >= imageMenus.Length) currentSelectIndex = 0;
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        if (imageMenus == null) return;
        for (int i = 0; i < imageMenus.Length; i++)
        {
            if (imageMenus[i] != null)
            {
                imageMenus[i].color = (i == currentSelectIndex) ? Color.green : Color.white;
            }
        }

        // 更新描述文本
        if (statusText != null && !isTesting)
        {
            switch (currentSelectIndex)
            {
                case 0: statusText.text = "项目1: 连续震动压测 (30秒)\n不同强度随机切换"; break;
                case 1: statusText.text = "项目2: 高频数据接收压测 (60秒)\n统计丢包率与频率"; break;
                case 2: statusText.text = "项目3: 综合并发压测\n音频 + 震动 + 陀螺仪显示"; break;
            }
        }
    }

    protected override void OnButtonOKPressed()
    {
        if (isTesting)
        {
            StopCurrentTest();
        }
        else
        {
            StartTest(currentSelectIndex);
        }
    }

    protected override void OnEscapePressed()
    {
        if (isTesting)
        {
            StopCurrentTest();
        }
        else
        {
            base.OnEscapePressed();
        }
    }

    // --- 测试逻辑控制 ---

    private void StartTest(int index)
    {
        isTesting = true;
        if (statusText != null) statusText.text = "测试进行中...";

        switch (index)
        {
            case 0:
                currentTestRoutine = StartCoroutine(VibrationStressTest());
                break;
            case 1:
                currentTestRoutine = StartCoroutine(HighFreqDataStressTest());
                break;
            case 2:
                currentTestRoutine = StartCoroutine(ConcurrentStressTest());
                break;
        }
    }

    private void RestoreSettings()
    {
        if (hasModifiedSettings)
        {
            Application.targetFrameRate = oldFrameRate;
            QualitySettings.vSyncCount = oldVSync;
            hasModifiedSettings = false;
        }

    }

    private void StopCurrentTest()
    {
        if (currentTestRoutine != null)
        {
            StopCoroutine(currentTestRoutine);
            currentTestRoutine = null;
        }

        RestoreSettings();

        // 清理工作
        if (bluetoothDemo != null)
        {
            // bluetoothDemo.OnGyroDataReceived -= OnGyroDataCount;
            bluetoothDemo.OnGyroDataReceived -= OnGyroDataDisplay;

            // 停止所有设备的震动
            foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
            {
                kvp.Value.VibrationItem?.VibrationStop(kvp.Value.DeviceMac);
            }
        }

        isTesting = false;
        if (statusText != null) statusText.text += "\n测试已停止";
        if (gyroInfoText != null) gyroInfoText.text = "";
    }

    // --- 项目 1: 连续震动压测 ---
    private IEnumerator VibrationStressTest()
    {
        float duration = 30f;
        float elapsed = 0f;

        if (statusText != null) statusText.text = "震动压测开始...\n持续 30 秒";

        while (elapsed < duration)
        {
            // 每 2 秒切换一次震动模式
            MotorIntensity intensity = (MotorIntensity)Random.Range(1, 4); // 1-3 (LOW, MEDIUM, HIGH)

            if (bluetoothDemo != null && bluetoothDemo.HardWareRemoteControlMap != null)
            {
                foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
                {
                    if (kvp.Value.VibrationItem != null)
                    {
                        // 设置震动 2500ms，确保覆盖整个间隔
                        kvp.Value.VibrationItem.VibrationStart(kvp.Value.DeviceMac, intensity, 2500);
                    }
                }
            }

            if (statusText != null) statusText.text = $"震动压测中... {duration - elapsed:F1}s\n当前强度: {intensity}";

            yield return new WaitForSeconds(2f);
            elapsed += 2f;
        }

        StopCurrentTest();
        if (statusText != null) statusText.text = "震动压测完成";
    }

    // --- 项目 2: 高频数据接收压测 ---
    private IEnumerator HighFreqDataStressTest()
    {
        float duration = 60f;
        float elapsed = 0f;

        devicePacketCounts.Clear();
        deviceTotalCalls.Clear();

        // 保存并修改帧率设置以支持100Hz
        if (!hasModifiedSettings)
        {
            oldFrameRate = Application.targetFrameRate;
            oldVSync = QualitySettings.vSyncCount;
            hasModifiedSettings = true;
        }
        Application.targetFrameRate = 120; // 设置为120Hz以确保能覆盖100Hz
        QualitySettings.vSyncCount = 0;

        // 暂时禁用 BluetoothDemo 的自动 Update，由我们手动驱动
        // if (bluetoothDemo != null)
        // {
        //     bluetoothDemo.enabled = false;
        // }

        testStartTime = Time.time;
        float pollInterval = 0.01f; // 100Hz = 10ms
        float nextPollTime = Time.time + pollInterval;

        while (elapsed < duration)
        {
            // 等待直到下一个轮询时间点 (手动控制频率)
            while (Time.time < nextPollTime)
            {
                yield return null;
            }
            nextPollTime += pollInterval;

            // 执行轮询
            if (bluetoothDemo != null && bluetoothDemo.HardWareRemoteControlMap != null)
            {
                foreach (var ctrl in bluetoothDemo.HardWareRemoteControlMap.Values)
                {
                    if (ctrl == null) continue;

                    string mac = ctrl.DeviceMac;
                    if (!deviceTotalCalls.ContainsKey(mac))
                    {
                        deviceTotalCalls[mac] = 0;
                        devicePacketCounts[mac] = 0;
                    }

                    // 1. 调用接口获取数据
                    ctrl.Update(); // 内部调用 Bluetooth.GetGyroDataByMAC

                    // 2. 统计
                    deviceTotalCalls[mac]++;
                    if (ctrl.GyroItem != null)
                    {
                        int count = ctrl.GyroItem.GetDeviceGyroDataSpan().Length;
                        if (count > 0)
                        {
                            devicePacketCounts[mac]++; // 这里统计的是“成功收到数据的次数”还是“数据包总数”？
                            // 题目问“是否每次都能收到结果”，暗示统计成功的调用次数。
                            // 同时也问“丢包率”，通常指 (TotalCalls - SuccessCalls) / TotalCalls
                            // 如果一次收到多个包，说明可能之前的积累了？
                            // 假设每次调用预期收到至少1个包。
                        }
                    }
                }
            }

            elapsed = Time.time - testStartTime;

            // 更新 UI
            string info = $"100Hz 接口压测中... {duration - elapsed:F1}s\nFPS: {1.0f / Time.deltaTime:F0}\n";
            foreach (var kvp in deviceTotalCalls)
            {
                string mac = kvp.Key;
                int total = kvp.Value;
                int received = devicePacketCounts.ContainsKey(mac) ? devicePacketCounts[mac] : 0;
                float lossRate = total > 0 ? (1.0f - (float)received / total) * 100f : 0f;

                info += $"设备 {mac.Substring(mac.Length - 5)}: 收到 {received}/{total} ({lossRate:F2}% 丢包)\n";
            }
            if (statusText != null) statusText.text = info;
        }

        // 恢复设置
        RestoreSettings();

        // 显示最终结果
        string finalResult = "100Hz 压测完成:\n";
        foreach (var kvp in deviceTotalCalls)
        {
            string mac = kvp.Key;
            int total = kvp.Value;
            int received = devicePacketCounts.ContainsKey(mac) ? devicePacketCounts[mac] : 0;
            float lossRate = total > 0 ? (1.0f - (float)received / total) * 100f : 0f;

            finalResult += $"设备 {mac.Substring(mac.Length - 5)}: 总调用 {total}, 成功 {received}, 丢包率 {lossRate:F2}%\n";
        }
        if (statusText != null) statusText.text = finalResult;

        isTesting = false;
        currentTestRoutine = null;
    }

    // 移除不再使用的 OnGyroDataCount 方法，或者保留但不再注册
    private void OnGyroDataCount(HardWareRemoteControl ctrl, int index)
    {
        // 兼容旧逻辑，但在此新压测模式下不使用事件回调
    }

    // --- 项目 3: 综合并发压测 ---
    private IEnumerator ConcurrentStressTest()
    {
        if (statusText != null) statusText.text = "综合并发压测进行中...\n音频 + 震动 + 陀螺仪\n按 OK 停止";

        if (bluetoothDemo != null)
        {
            bluetoothDemo.OnGyroDataReceived += OnGyroDataDisplay;

            // 开启持续震动
            foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
            {
                if (kvp.Value.VibrationItem != null)
                    kvp.Value.VibrationItem.VibrationStart(kvp.Value.DeviceMac, MotorIntensity.MEDIUM, 60000); // 1分钟长震动
            }
        }

        float audioTimer = 0f;

        while (true)
        {
            // 每 3 秒播放一次音效
            if (Time.time - audioTimer > 3f)
            {
                audioTimer = Time.time;
                if (bluetoothDemo != null)
                {
                    foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
                    {
                        if (kvp.Value.SpeakerItem != null)
                            kvp.Value.SpeakerItem.SetSoundEffect(SoundEffect.SE_1);
                    }
                }
            }

            // 维持震动 (简单地每5秒重发一次指令以防超时停止)
            if (Time.frameCount % 300 == 0)
            {
                foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
                {
                    if (kvp.Value.VibrationItem != null)
                        kvp.Value.VibrationItem.VibrationStart(kvp.Value.DeviceMac, MotorIntensity.MEDIUM, 10000);
                }
            }

            yield return null;
        }
    }

    private void OnGyroDataDisplay(HardWareRemoteControl ctrl)
    {
        if (ctrl == null || ctrl.GyroItem == null) return;

        // 获取最新的数据跨度
        var span = ctrl.GyroItem.GetDeviceGyroDataSpan();
        if (span.Length > 0)
        {
            var data = span[span.Length - 1]; // 最新一帧
            if (gyroInfoText != null)
            {
                // 这里简单覆盖显示最后一个设备的数据，实际可能需要分屏显示
                gyroInfoText.text = $"设备: {ctrl.DeviceMac.Substring(ctrl.DeviceMac.Length - 5)}\n" +
                                    $"A: {data.accelX}, {data.accelY}, {data.accelZ}\n" +
                                    $"G: {data.gyroX}, {data.gyroY}, {data.gyroZ}\n" +
                                    $"TS: {data.timestamp}";
            }
        }
    }
}
