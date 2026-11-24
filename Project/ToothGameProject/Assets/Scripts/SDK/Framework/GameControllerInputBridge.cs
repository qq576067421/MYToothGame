using GameDll;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using YoudooGame.Controller;

// SDK 在 Android 消费遥控器原生事件，在编辑器和桌面同时提供键盘与手柄绑定。
public sealed class GameControllerInputBridge : MonoBehaviour
{
    private const string kTag = "[新SDK整合][InputBridge]";
    private const float kDuplicateInputWindow = 0.15f;

    public static GameControllerInputBridge Instance { get; private set; }

    public event Func<Vector2, bool> NavigateRequested;
    public event Func<bool> CancelRequested;

    private readonly Dictionary<Vector2, float> m_LastNavigateTimes = new Dictionary<Vector2, float>(4);
    private ControllerInputContext m_Context;
    private float m_LastSubmitTime = float.MinValue;
    private float m_LastCancelTime = float.MinValue;
    private float m_LastPauseTime = float.MinValue;
    private bool m_ApplicationPaused;
    private bool m_WasInputBlocked;
    private int m_InputGeneration;

    private bool CanDispatchInput => !m_ApplicationPaused && Application.isFocused &&
        !ControllerInputSuppression.IsSuppressed && !RenderEvent.Event.OnRenderShouldIgnoreInputDuringLoading();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        m_Context = ControllerInput.CreateContext("GameControllerInputBridge");
    }

    private void OnEnable()
    {
        m_Context?.Enable();
        DiscardPendingInput();
    }

    private void OnDisable()
    {
        m_Context?.Disable();
        DiscardPendingInput();
    }

    private void OnApplicationFocus(bool focused)
    {
        ResetOnApplicationBoundary();
    }

    private void OnApplicationPause(bool paused)
    {
        m_ApplicationPaused = paused;
        ResetOnApplicationBoundary();
    }

    private void ResetOnApplicationBoundary()
    {
        m_WasInputBlocked = true;
        DiscardPendingInput();
    }

    public void DiscardPendingInput()
    {
        m_InputGeneration++;
        m_Context?.ResetPendingEvents();
    }

    private void Update()
    {
        if (m_Context == null)
        {
            return;
        }

        if (!CanDispatchInput)
        {
            m_WasInputBlocked = true;
            DiscardPendingInput();
            return;
        }

        if (m_WasInputBlocked)
        {
            m_WasInputBlocked = false;
            DiscardPendingInput();
            return;
        }

        int generation = m_InputGeneration;
        while (generation == m_InputGeneration && CanDispatchInput &&
            m_Context.TryConsumeNavigate(out var direction))
        {
            DispatchNavigate(direction);
        }

#if !UNITY_ANDROID || UNITY_EDITOR
        if (generation == m_InputGeneration && CanDispatchInput)
        {
            PollDesktopNavigation();
        }
#endif

        if (generation != m_InputGeneration || !CanDispatchInput)
        {
            return;
        }

        bool cancel = false;
        bool pause = false;
        bool submit = false;
        while (m_Context.ConsumeCancel())
        {
            cancel = true;
        }
        while (m_Context.ConsumePause())
        {
            pause = true;
        }
        while (m_Context.ConsumeSubmit())
        {
            submit = true;
        }

        // 同一帧最多处理一次菜单操作。打开暂停窗的按键不能继续确认暂停窗默认项。
        if (cancel)
        {
            if (TryAcceptInput(ref m_LastCancelTime) && CancelRequested?.Invoke() != true)
            {
                RenderAPI.OnEscapePressed(default);
            }
        }
        else if (pause)
        {
            if (TryAcceptInput(ref m_LastPauseTime))
            {
                bool handled = RenderEvent.Event.OnTowerDefendPauseRequest();
                GameSdkLog.Info(kTag + " pause handled=" + handled);
            }
        }
        else if (submit && TryAcceptInput(ref m_LastSubmitTime))
        {
            RenderAPI.OnEnterAction(default);
        }
    }

#if !UNITY_ANDROID || UNITY_EDITOR
    private void PollDesktopNavigation()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        int x = (keyboard.dKey.wasPressedThisFrame ? 1 : 0) - (keyboard.aKey.wasPressedThisFrame ? 1 : 0);
        int y = (keyboard.wKey.wasPressedThisFrame ? 1 : 0) - (keyboard.sKey.wasPressedThisFrame ? 1 : 0);
        if (x != 0 || y != 0)
        {
            DispatchNavigate(new Vector2(x, y));
        }
    }
#endif

    private void DispatchNavigate(Vector2 direction)
    {
        Vector2 menuDirection;
        Action<InputAction.CallbackContext> handler;
        if (direction.x < -0.5f)
        {
            menuDirection = Vector2.left;
            handler = RenderAPI.OnLeftArrowPressed;
        }
        else if (direction.x > 0.5f)
        {
            menuDirection = Vector2.right;
            handler = RenderAPI.OnRightArrowPressed;
        }
        else if (direction.y > 0.5f)
        {
            menuDirection = Vector2.up;
            handler = RenderAPI.OnUpArrowPressed;
        }
        else if (direction.y < -0.5f)
        {
            menuDirection = Vector2.down;
            handler = RenderAPI.OnDownArrowPressed;
        }
        else
        {
            return;
        }

        float now = Time.unscaledTime;
        if (m_LastNavigateTimes.TryGetValue(menuDirection, out float lastTime) &&
            now - lastTime < kDuplicateInputWindow)
        {
            return;
        }

        m_LastNavigateTimes[menuDirection] = now;
        if (NavigateRequested?.Invoke(direction) != true)
        {
            handler(default);
        }
    }

    private static bool TryAcceptInput(ref float lastTime)
    {
        float now = Time.unscaledTime;
        if (now - lastTime < kDuplicateInputWindow)
        {
            return false;
        }

        lastTime = now;
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        m_Context?.Dispose();
        m_Context = null;
    }
}
