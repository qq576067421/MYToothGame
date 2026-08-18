using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using EnhancedTouchState = UnityEngine.InputSystem.EnhancedTouch.Touch;
using InputSystemTouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityTouchPhase = UnityEngine.TouchPhase;

public readonly struct InputSystemTouch
{
    public InputSystemTouch(int fingerId, Vector2 position, UnityTouchPhase phase)
    {
        this.fingerId = fingerId;
        this.position = position;
        this.phase = phase;
    }

    public int fingerId { get; }
    public Vector2 position { get; }
    public UnityTouchPhase phase { get; }
}

public static class InputSystemCompat
{
    private static InputSystemTouch[] s_Touches = Array.Empty<InputSystemTouch>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        EnsureEnhancedTouchSupport();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeAfterSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInputSystemModules();
    }

    public static int touchCount
    {
        get
        {
            EnsureEnhancedTouchSupport();
            return EnhancedTouchState.activeTouches.Count;
        }
    }

    public static InputSystemTouch[] touches
    {
        get
        {
            EnsureEnhancedTouchSupport();
            var activeTouches = EnhancedTouchState.activeTouches;
            if (s_Touches.Length != activeTouches.Count)
            {
                s_Touches = new InputSystemTouch[activeTouches.Count];
            }

            for (var i = 0; i < activeTouches.Count; ++i)
            {
                s_Touches[i] = ConvertTouch(activeTouches[i]);
            }

            return s_Touches;
        }
    }

    public static Vector3 mousePosition
    {
        get
        {
            if (Mouse.current != null)
            {
                var position = Mouse.current.position.ReadValue();
                return new Vector3(position.x, position.y, 0f);
            }

            if (touchCount > 0)
            {
                var touch = GetTouch(0);
                return new Vector3(touch.position.x, touch.position.y, 0f);
            }

            return Vector3.zero;
        }
    }

    public static InputSystemTouch GetTouch(int index)
    {
        EnsureEnhancedTouchSupport();
        return ConvertTouch(EnhancedTouchState.activeTouches[index]);
    }

    public static bool GetMouseButtonDown(int button)
    {
        return ReadMouseButton(button, control => control.wasPressedThisFrame);
    }

    public static bool GetMouseButton(int button)
    {
        return ReadMouseButton(button, control => control.isPressed);
    }

    public static bool GetMouseButtonUp(int button)
    {
        return ReadMouseButton(button, control => control.wasReleasedThisFrame);
    }

    public static bool GetKeyDown(KeyCode keyCode)
    {
        return ReadKey(keyCode, control => control.wasPressedThisFrame);
    }

    public static bool GetKey(KeyCode keyCode)
    {
        return ReadKey(keyCode, control => control.isPressed);
    }

    public static bool GetKeyUp(KeyCode keyCode)
    {
        return ReadKey(keyCode, control => control.wasReleasedThisFrame);
    }

    public static void EnsureInputSystemModules()
    {
        var eventSystems = UnityEngine.Object.FindObjectsOfType<EventSystem>(true);
        for (var i = 0; i < eventSystems.Length; ++i)
        {
            EnsureInputSystemUIInputModule(eventSystems[i].gameObject);
        }
    }

    public static void EnsureInputSystemUIInputModule(GameObject eventSystemObject)
    {
        if (eventSystemObject == null)
        {
            return;
        }

        var inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        if (inputModule.actionsAsset == null)
        {
            inputModule.AssignDefaultActions();
        }

        var standaloneInputModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
        {
            UnityEngine.Object.Destroy(standaloneInputModule);
        }

    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInputSystemModules();
    }

    private static void EnsureEnhancedTouchSupport()
    {
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
    }

    private static InputSystemTouch ConvertTouch(EnhancedTouchState touch)
    {
        return new InputSystemTouch(touch.touchId, touch.screenPosition, ConvertTouchPhase(touch.phase));
    }

    private static UnityTouchPhase ConvertTouchPhase(InputSystemTouchPhase phase)
    {
        return phase switch
        {
            InputSystemTouchPhase.Began => UnityTouchPhase.Began,
            InputSystemTouchPhase.Moved => UnityTouchPhase.Moved,
            InputSystemTouchPhase.Ended => UnityTouchPhase.Ended,
            InputSystemTouchPhase.Canceled => UnityTouchPhase.Canceled,
            _ => UnityTouchPhase.Stationary,
        };
    }

    private static bool ReadMouseButton(int button, Func<ButtonControl, bool> reader)
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            return false;
        }

        ButtonControl control = button switch
        {
            0 => mouse.leftButton,
            1 => mouse.rightButton,
            2 => mouse.middleButton,
            _ => null
        };

        return control != null && reader(control);
    }

    private static bool ReadKey(KeyCode keyCode, Func<ButtonControl, bool> reader)
    {
        if (TryReadKeyboardKey(keyCode, reader))
        {
            return true;
        }

        return TryReadGamepadKey(keyCode, reader);
    }

    private static bool TryReadKeyboardKey(KeyCode keyCode, Func<ButtonControl, bool> reader)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        ButtonControl control = keyCode switch
        {
            KeyCode.A => keyboard.aKey,
            KeyCode.D => keyboard.dKey,
            KeyCode.J => keyboard.jKey,
            KeyCode.K => keyboard.kKey,
            KeyCode.S => keyboard.sKey,
            KeyCode.W => keyboard.wKey,
            KeyCode.Alpha1 => keyboard.digit1Key,
            KeyCode.Alpha2 => keyboard.digit2Key,
            KeyCode.Alpha3 => keyboard.digit3Key,
            KeyCode.Alpha4 => keyboard.digit4Key,
            KeyCode.Keypad0 => keyboard.numpad0Key,
            KeyCode.Keypad1 => keyboard.numpad1Key,
            KeyCode.Keypad2 => keyboard.numpad2Key,
            KeyCode.Keypad3 => keyboard.numpad3Key,
            KeyCode.Keypad4 => keyboard.numpad4Key,
            KeyCode.KeypadPlus => keyboard.numpadPlusKey,
            KeyCode.UpArrow => keyboard.upArrowKey,
            KeyCode.DownArrow => keyboard.downArrowKey,
            KeyCode.LeftArrow => keyboard.leftArrowKey,
            KeyCode.RightArrow => keyboard.rightArrowKey,
            KeyCode.Escape => keyboard.escapeKey,
            KeyCode.Space => keyboard.spaceKey,
            _ => null
        };

        return control != null && reader(control);
    }

    private static bool TryReadGamepadKey(KeyCode keyCode, Func<ButtonControl, bool> reader)
    {
        var gamepads = Gamepad.all;
        for (var i = 0; i < gamepads.Count; ++i)
        {
            var gamepad = gamepads[i];
            ButtonControl control = keyCode switch
            {
                KeyCode.UpArrow => gamepad.dpad.up,
                KeyCode.DownArrow => gamepad.dpad.down,
                KeyCode.LeftArrow => gamepad.dpad.left,
                KeyCode.RightArrow => gamepad.dpad.right,
                KeyCode.Escape => gamepad.startButton,
                KeyCode.JoystickButton0 => gamepad.buttonSouth,
                KeyCode.JoystickButton1 => gamepad.buttonEast,
                _ => null
            };

            if (control != null && reader(control))
            {
                return true;
            }
        }

        return false;
    }
}
