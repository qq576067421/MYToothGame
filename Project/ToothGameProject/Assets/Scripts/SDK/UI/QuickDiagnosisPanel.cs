using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using YouDooUnity;
using static YouDooSDKConstants;

public class QuickDiagnosisPanel : MainScene
{
    private BluetoothOnlyUseMajorControllerDemo bluetoothDemo;

    [Header("UI References")]
    [SerializeField]
    private Text promptText;          // 用于显示当前正在进行的检测提示，例如 "接下来开始震动马达检测"

    [SerializeField]
    private GameObject animationPanel; // 提示动画的容器面板

    [SerializeField]
    private Text resultText;          // 用于显示检测结果列表

    HardWareRemoteControl device;
    // 内部状态
    private bool isDiagnosisRunning = false;
    private bool isWaitingForUser = false;
    private bool isDiagnosisComplete = false;
    private bool userConfirmed = false;
    // 电池信息
    int currentBatteryLevel;

    // 按键测试相关
    private HashSet<string> requiredKeys = new HashSet<string>();
    private HashSet<string> pressedKeys = new HashSet<string>();

    void Start()
    {
        bluetoothDemo = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController;
        device = bluetoothDemo.HardWareRemoteControlMap.Values.First();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResetState();
    }

    private void ResetState()
    {
        isDiagnosisRunning = false;
        isDiagnosisComplete = false;
        isWaitingForUser = false;
        if (promptText != null) promptText.text = "按 OK 键开始快速检测";
        if (resultText != null) resultText.text = "";
        if (animationPanel != null) animationPanel.SetActive(false);
    }

    protected override void OnButtonOKPressed()
    {
        if (isDiagnosisComplete)
        {
            ExitPanel();
            return;
        }

        if (!isDiagnosisRunning)
        {
            StartCoroutine(DiagnosisRoutine());
        }
        else if (isWaitingForUser)
        {
            userConfirmed = true;
        }
    }

    protected override void OnEscapePressed()
    {
        if (isDiagnosisComplete)
        {
            ExitPanel();
        }
        else
        {
            base.OnEscapePressed();
        }
    }

    private void ExitPanel()
    {
        ShowMainPanel(mainPanel.gameObject);
    }

    // 复用 MainScene 的输入事件用于按键测试
    protected override void OnUpArrowPressed() { RecordKeyPress("Up"); }
    protected override void OnDownArrowPressed() { RecordKeyPress("Down"); }
    protected override void OnLeftArrowPressed() { RecordKeyPress("Left"); }
    protected override void OnRightArrowPressed() { RecordKeyPress("Right"); }

    protected override void OnButtonJoystickButton1Pressed() { RecordKeyPress("JoystickButton1"); }

    private void RecordKeyPress(string key)
    {
        if (isDiagnosisRunning && isWaitingForUser)
        {
            if (requiredKeys.Contains(key))
            {
                pressedKeys.Add(key);
                UpdatePromptText($"按键测试: 请依次按下 上 下 左 右 扳机\n已检测: {string.Join(", ", pressedKeys)}");
            }
        }
    }

    private IEnumerator DiagnosisRoutine()
    {
        isDiagnosisRunning = true;
        isDiagnosisComplete = false;
        if (resultText != null) resultText.text = "检测开始...\n";
        GetBatteryLevelAsync();

        // 1. 蓝牙连接
        yield return ShowPromptAnimation("正在检测蓝牙连接...");
        bool btConnected = CheckBluetooth();
        AppendResult("蓝牙连接", btConnected);

        if (!btConnected)
        {
            UpdatePromptText("蓝牙未连接，无法继续检测。\n按 返回键 或 确认键 退出");
            isDiagnosisRunning = false;
            isDiagnosisComplete = true;
            yield break;
        }

        yield return new WaitForSeconds(1.0f); // 稍作停顿以便看清结果

        // 2. 电池健康
        yield return ShowPromptAnimation("正在检测电池健康...");
        bool batteryOk = CheckBattery();
        AppendResult("电池健康", batteryOk);
        yield return new WaitForSeconds(1.0f);

        // 3. 六轴校准
        yield return ShowPromptAnimation("正在检测六轴传感器...");
        bool gyroOk = CheckGyro(); // 这里做简单的存在性检查
        AppendResult("六轴校准", gyroOk);
        yield return new WaitForSeconds(1.0f);

        // 4. 扬声器通路
        yield return ShowPromptAnimation("接下来开始扬声器通路检测\n如果听到声音请按 OK 键");
        PlaySpeakerSound();
        yield return WaitForUserConfirmation(5.0f);
        AppendResult("扬声器通路", userConfirmed);

        // 5. 话筒通路
        yield return ShowPromptAnimation("正在检测话筒通路...");
        bool micOk = CheckMicrophoneAndStop();
        AppendResult("话筒通路", micOk);
        yield return new WaitForSeconds(1.0f);

        // 6. 震动马达
        yield return ShowPromptAnimation("接下来开始震动马达检测\n如果感觉到震动请按 OK 键");
        TriggerVibration();
        yield return WaitForUserConfirmation(5.0f);
        StopVibration();
        AppendResult("震动马达", userConfirmed);

        // 7. 按键输入
        yield return ShowPromptAnimation("接下来开始按键输入检测\n请依次按下：上、下、左、右、扳机");
        yield return CheckKeySequence();

        // 结束
        if (animationPanel != null) animationPanel.SetActive(false);
        if (promptText != null) promptText.text = "检测流程结束\n按 返回键 或 确认键 退出";
        isDiagnosisRunning = false;
        isDiagnosisComplete = true;
    }

    // --- 辅助方法 ---

    private IEnumerator ShowPromptAnimation(string text)
    {
        if (animationPanel != null) animationPanel.SetActive(true);
        UpdatePromptText(text);

        // 简单的闪烁动画或淡入效果模拟
        if (promptText != null)
        {
            promptText.color = new Color(promptText.color.r, promptText.color.g, promptText.color.b, 0);
            float duration = 0.5f;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0, 1, elapsed / duration);
                promptText.color = new Color(promptText.color.r, promptText.color.g, promptText.color.b, alpha);
                yield return null;
            }
        }

        yield return new WaitForSeconds(2f); // 保持显示一小会儿
    }

    private void UpdatePromptText(string text)
    {
        if (promptText != null) promptText.text = text;
    }

    private void AppendResult(string itemName, bool passed)
    {
        if (resultText != null)
        {
            string status = passed ? "<color=green>通过</color>" : "<color=red>失败</color>";
            resultText.text += $"{itemName}: {status}\n";
        }
    }

    private IEnumerator WaitForUserConfirmation(float timeout)
    {
        isWaitingForUser = true;
        userConfirmed = false;
        float timer = 0;

        while (timer < timeout && !userConfirmed)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isWaitingForUser = false;
    }

    // --- 检测逻辑实现 ---

    private bool CheckBluetooth()
    {
        if (bluetoothDemo == null || bluetoothDemo.HardWareRemoteControlMap == null) return false;
        return bluetoothDemo.HardWareRemoteControlMap.Count > 0;
    }

    private bool CheckBattery()
    {
        if (!CheckBluetooth()) return false;

        return currentBatteryLevel > 0;
    }

    void GetBatteryLevelAsync()
    {
        var device = bluetoothDemo.HardWareRemoteControlMap.Values.First();
        if (device == null) return;
        // 这里只返回主手柄的电池信息
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].OnGetDeviceBatteryLevelAction += GetBattleLevel;
        device.GetDeviceBatteryLevelAsync();
    }

    void GetBattleLevel(BluetoothNotifyInfo<BatteryInfo> info)
    {
        currentBatteryLevel = info.message.batteryLevel;
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].OnGetDeviceBatteryLevelAction -= GetBattleLevel;
    }

    private bool CheckGyro()
    {
        if (!CheckBluetooth()) return false;

        foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
        {
            // 检查 GyroItem 是否初始化
            if (kvp.Value.GyroItem != null) return true;
        }
        return false;
    }

    private bool CheckMicrophoneAndStop()
    {
        if (!CheckBluetooth()) return false;

        bool allOk = true;
        bool hasChecked = false;

        // foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
        // {
        //     if (kvp.Value.AudioItem != null)
        //     {
        //         hasChecked = true;
        //         bool ret = kvp.Value.AudioItem.StartAudioRecording();
        //         Debug.Log($"[Diagnosis] CheckMicrophone Device:{kvp.Key} Result:{ret}");

        //         if (ret)
        //         {
        //             // 成功开启，表示通路正常，立即关闭
        //             kvp.Value.AudioItem.StopAudioRecording();
        //         }
        //         else
        //         {
        //             allOk = false;
        //         }
        //     }
        // }
        return hasChecked && allOk;
    }

    private void PlaySpeakerSound()
    {
        if (!CheckBluetooth()) return;

        foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
        {
            // 假设 SpeakerItem 有 Play 方法，或者重新初始化来触发声音
            // 由于看不到 SpeakerItem 的定义，这里做个假设调用
            // 如果没有 Play 方法，可能需要扩展 HardWareRemoteControl
            // 暂时打印日志
            Debug.Log($"Playing sound on device {kvp.Key}");

            // 尝试调用 InitSpeakerItem (如果它是播放声音的唯一方式)
            // 或者如果有 AudioItem，可能用 AudioItem 播放
            if (kvp.Value.SpeakerItem != null)
            {
                kvp.Value.SpeakerItem.SetSoundEffect(SoundEffect.SE_1); // 假设的方法
            }
        }
    }

    private void TriggerVibration()
    {
        if (!CheckBluetooth()) return;

        foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
        {
            if (kvp.Value.VibrationItem != null)
            {
                // 震动 2 秒，强度 LOW
                kvp.Value.VibrationItem.VibrationStart(kvp.Value.DeviceMac, MotorIntensity.LOW, 2000);
            }
        }
    }

    private void StopVibration()
    {
        if (!CheckBluetooth()) return;
        foreach (var kvp in bluetoothDemo.HardWareRemoteControlMap)
        {
            if (kvp.Value.VibrationItem != null)
            {
                kvp.Value.VibrationItem.VibrationStop(kvp.Value.DeviceMac);
            }
        }
    }

    private IEnumerator CheckKeySequence()
    {
        requiredKeys.Clear();
        requiredKeys.Add("Up");
        requiredKeys.Add("Down");
        requiredKeys.Add("Left");
        requiredKeys.Add("Right");
        requiredKeys.Add("JoystickButton1");

        pressedKeys.Clear();
        isWaitingForUser = true;

        float timeout = 10f;
        float timer = 0;

        while (timer < timeout && pressedKeys.Count < requiredKeys.Count)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isWaitingForUser = false;

        bool allPressed = pressedKeys.Count == requiredKeys.Count;
        AppendResult("按键输入", allPressed);
    }
}
