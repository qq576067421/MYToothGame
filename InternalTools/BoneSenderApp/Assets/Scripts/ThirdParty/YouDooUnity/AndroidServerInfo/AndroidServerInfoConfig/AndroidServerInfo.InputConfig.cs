/*
作者：Ting
创建时间：2026.05.28
描述：AndroidServerInfo 输入配置模块 - 支持编辑器中选择输入模式
  - SDK 输入模式：用于真机环境，通过Android SDK获取输入
  - 键盘输入模式：用于开发环境，通过Unity键盘API获取输入
  
模式选择说明：
  设置为 Keyboard 模式时，自动视为开发环境，所有SDK相关调用都会被跳过。
  设置为 SDK 模式时，仅执行SDK相关调用。
*/

using UnityEngine;
using YouDooSDK.Utils;

/// <summary>
/// 输入模式枚举
/// </summary>
public enum InputMode
{
    /// <summary>SDK模式 - 真机环境使用</summary>
    SDK = 0,
    /// <summary>键盘模式 - 开发环境使用</summary>
    Keyboard = 1
}

public partial class AndroidServerInfo : MonoSingleton<AndroidServerInfo>
{
    /// <summary>
    /// 当前的输入模式
    /// </summary>
    [SerializeField]
    private InputMode _inputMode = InputMode.Keyboard;

    /// <summary>
    /// 获取当前输入模式
    /// </summary>
    public InputMode CurrentInputMode => _inputMode;

    /// <summary>
    /// 获取当前是否使用SDK模式
    /// </summary>
    public bool IsSDKMode => _inputMode == InputMode.SDK;

    /// <summary>
    /// 获取当前是否使用键盘模式（即开发环境）
    /// </summary>
    public bool IsKeyboardMode => _inputMode == InputMode.Keyboard;

    /// <summary>
    /// 设置输入模式
    /// </summary>
    /// <param name="mode">要设置的输入模式</param>
    public void SetInputMode(InputMode mode)
    {
        if (_inputMode != mode)
        {
            _inputMode = mode;
            OnInputModeChanged(mode);
            Debug.Log($"输入模式已切换为: {mode} ({GetInputModeDescription(mode)})");
        }
    }

    /// <summary>
    /// 切换输入模式
    /// </summary>
    public void ToggleInputMode()
    {
        InputMode newMode = _inputMode == InputMode.SDK ? InputMode.Keyboard : InputMode.SDK;
        SetInputMode(newMode);
    }

    /// <summary>
    /// 获取输入模式的描述文本
    /// </summary>
    /// <param name="mode">输入模式</param>
    /// <returns>描述文本</returns>
    private string GetInputModeDescription(InputMode mode)
    {
        return mode switch
        {
            InputMode.SDK => "SDK模式（真机环境）",
            InputMode.Keyboard => "键盘模式（开发环境）",
            _ => "未知模式"
        };
    }

    /// <summary>
    /// 当输入模式改变时的虚方法，子类可覆写
    /// </summary>
    /// <param name="newMode">新的输入模式</param>
    protected virtual void OnInputModeChanged(InputMode newMode)
    {
        if (newMode == InputMode.SDK)
        {
            Debug.Log("[输入配置] 已切换为 SDK 模式 - 只执行SDK相关调用");
            InitSDKInputMode();
        }
        else if (newMode == InputMode.Keyboard)
        {
            Debug.Log("[输入配置] 已切换为 键盘 模式 - 开发环境，跳过所有SDK调用");
            InitKeyboardInputMode();
        }
    }

    /// <summary>
    /// 初始化SDK输入模式
    /// </summary>
    protected virtual void InitSDKInputMode()
    {
        Debug.Log("[输入配置] 初始化SDK模式");
    }

    /// <summary>
    /// 初始化键盘输入模式
    /// </summary>
    protected virtual void InitKeyboardInputMode()
    {
        Debug.Log("[输入配置] 初始化键盘模式（开发环境）");
    }

}