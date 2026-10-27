using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YoudooGame.Controller;

// 新 SDK 统一输入桥：消费 ControllerInputContext（方向键/Enter/Space/Esc/
// 手柄/真机遥控器），并补齐键盘键位（WASD）。
// 项目 UI 的导航/高亮/确认体系是 RenderAPI 静态遥控菜单（ActiveButtons 网格）；
// 本桥使用新 SDK 输入直接驱动该菜单，
// 键位语义：WASD/方向键=导航，Enter/Space=确认，Esc=返回。
public sealed class GameControllerInputBridge : MonoBehaviour
{
    private const string kTag = "[新SDK整合][InputBridge]";
    private const float kStateLogInterval = 5f;
    private const float kDuplicateInputWindow = 0.15f;

    public static GameControllerInputBridge Instance { get; private set; }

    public event Action<Vector2> NavigateReceived;
    public event Action SubmitReceived;
    public event Action CancelReceived;
    public event Action PauseReceived;

    private ControllerInputContext m_Context;
    private float m_NextStateLogTime;
    private float m_LastRemoteInputTime = float.MinValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        m_Context = ControllerInput.CreateContext("GameControllerInputBridge");
        m_Context.Enable();
        GameSdkLog.Info(kTag + " created, keyboard=" + (Keyboard.current != null ? "present" : "NULL") +
                  " newHost=" + YouDooSDK.Native.AndroidServerInfo.IsInitialized);
    }

    private void Update()
    {
        if (m_Context == null)
        {
            return;
        }

        if (Time.unscaledTime >= m_NextStateLogTime)
        {
            m_NextStateLogTime = Time.unscaledTime + kStateLogInterval;
            //GameSdkLog.Info(kTag + " state: keyboard=" + (Keyboard.current != null ? "ok" : "NULL"));
        }

        while (m_Context.TryConsumeNavigate(out var direction))
        {
            if (IsDuplicateRemoteInput())
            {
                continue;
            }

            GameSdkLog.Info(kTag + " SDK navigate " + direction);
            DispatchNavigate(direction);
        }

        while (m_Context.ConsumeSubmit())
        {
            if (IsDuplicateRemoteInput())
            {
                continue;
            }

            GameSdkLog.Info(kTag + " SDK submit");
            DispatchSubmit();
        }

        while (m_Context.ConsumeCancel())
        {
            if (IsDuplicateRemoteInput())
            {
                continue;
            }

            GameSdkLog.Info(kTag + " SDK cancel");
            DispatchCancel();
        }

        while (m_Context.ConsumePause())
        {
            GameSdkLog.Info(kTag + " SDK pause");
            PauseReceived?.Invoke();
        }

        PollLegacyKeyboard();
    }

    // 键盘补充映射：A/D/W/S 与手柄方向键进入同一事件流。
    private void PollLegacyKeyboard()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.aKey.wasPressedThisFrame)
        {
            GameSdkLog.Info(kTag + " key A -> navigate left");
            DispatchNavigate(Vector2.left);
        }

        if (keyboard.dKey.wasPressedThisFrame)
        {
            GameSdkLog.Info(kTag + " key D -> navigate right");
            DispatchNavigate(Vector2.right);
        }

        if (keyboard.wKey.wasPressedThisFrame)
        {
            GameSdkLog.Info(kTag + " key W -> navigate up");
            DispatchNavigate(Vector2.up);
        }

        if (keyboard.sKey.wasPressedThisFrame)
        {
            GameSdkLog.Info(kTag + " key S -> navigate down");
            DispatchNavigate(Vector2.down);
        }
    }

    private void DispatchNavigate(Vector2 direction)
    {
        m_LastRemoteInputTime = Time.unscaledTime;
        NavigateReceived?.Invoke(direction);
        if (direction.x < -0.5f)
        {
            DispatchRemoteMenu(RenderAPI.OnLeftArrowPressed);
        }
        else if (direction.x > 0.5f)
        {
            DispatchRemoteMenu(RenderAPI.OnRightArrowPressed);
        }
        else if (direction.y > 0.5f)
        {
            DispatchRemoteMenu(RenderAPI.OnUpArrowPressed);
        }
        else if (direction.y < -0.5f)
        {
            DispatchRemoteMenu(RenderAPI.OnDownArrowPressed);
        }
    }

    private void DispatchSubmit()
    {
        m_LastRemoteInputTime = Time.unscaledTime;
        SubmitReceived?.Invoke();
        DispatchRemoteMenu(RenderAPI.OnEnterAction);
    }

    private void DispatchCancel()
    {
        m_LastRemoteInputTime = Time.unscaledTime;
        CancelReceived?.Invoke();
        DispatchRemoteMenu(RenderAPI.OnEscapePressed);
    }

    private static void DispatchRemoteMenu(
        Action<InputAction.CallbackContext> handler)
    {
        if (handler == null)
        {
            return;
        }

        handler(default(InputAction.CallbackContext));
    }

    private bool IsDuplicateRemoteInput()
    {
        return Time.unscaledTime - m_LastRemoteInputTime < kDuplicateInputWindow;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (m_Context != null)
        {
            m_Context.Dispose();
            m_Context = null;
        }
    }
}
