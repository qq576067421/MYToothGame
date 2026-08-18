using System;
using System.Collections.Generic;
using UnityEngine;
using YouDooSDK.Utils;

public class RemoteControlUnitInputSystemManager : MonoSingleton<RemoteControlUnitInputSystemManager>
{

    private bool _isInputEnabled = true;
    private float lastInputTime = 0f;

    private float inputCooldown = 0.2f; // 输入冷却时间，防止过快切换
    #region 
    public event Action OnDownArrowPressed;
    public event Action OnUpArrowPressed;
    public event Action OnLeftArrowPressed;
    public event Action OnRightArrowPressed;
    public event Action OnEscapePressed;
    public event Action OnButtonOKPressed;
    public event Action OnButtonJoystickButton1Pressed;
    #endregion


    private List<KeyCode> _monitoredKeys = new List<KeyCode>
    {
        KeyCode.DownArrow,
        KeyCode.UpArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.Escape,
        KeyCode.Space,
        KeyCode.JoystickButton0,
        KeyCode.JoystickButton1
    };


    /// <summary>
    /// 设置输入是否启用
    /// </summary>
    /// <param name="enabled"></param>
    public void SetRemoteControlUnitInputEnabled(bool enabled)
    {
        _isInputEnabled = enabled;
        Debug.Log($"SetRemoteControlUnitInputEnabled     启用输入: {enabled}");
    }

    /// <summary>
    /// 重置输入系统状态，用于游戏重新进入时清理状态
    /// </summary>
    public void ResetInputState()
    {
        lastInputTime = 0f;
        _isInputEnabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (_isInputEnabled)
        {
            //CheckRemoteControlUnitInput();
        }
    }

    private void CheckRemoteControlUnitInput()
    {
        if (Time.time - lastInputTime < inputCooldown) return;
        foreach (KeyCode key in _monitoredKeys)
        {
            if (Input.GetKeyDown(key))
            {
                lastInputTime = Time.time;
                switch (key)
                {
                    case KeyCode.DownArrow:
                        OnDownArrowPressed?.Invoke();
                        break;
                    case KeyCode.UpArrow:
                        OnUpArrowPressed?.Invoke();
                        break;
                    case KeyCode.LeftArrow:
                        OnLeftArrowPressed?.Invoke();
                        break;
                    case KeyCode.RightArrow:
                        OnRightArrowPressed?.Invoke();
                        break;
                    case KeyCode.Escape:
                        OnEscapePressed?.Invoke();
                        break;
                    case KeyCode.Space:
                    case KeyCode.JoystickButton0:
                        OnButtonOKPressed?.Invoke();
                        break;
                    case KeyCode.JoystickButton1:
                        OnButtonJoystickButton1Pressed?.Invoke();
                        break;
                }
            }
        }
    }
}
