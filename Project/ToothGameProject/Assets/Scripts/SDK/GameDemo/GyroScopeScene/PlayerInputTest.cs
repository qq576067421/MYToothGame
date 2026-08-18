using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using YouDooUnity;

public class PlayerInputTest : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(InitDelay());
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator InitDelay()
    {
        yield return new WaitUntil(() => { return RCUPlayerInputManager.Instance.Dispatcher != null; });
        RCUPlayerInputManager.Instance.SetMajorDevice();
        // RCUPlayerInputManager.Instance.Dispatcher.EnterAction.started += InitPlayer;
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].UpArrowAction.started += Up;
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].DownArrowAction.started += Down;
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].LeftArrowAction.started += Left;
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].RightArrowAction.started += Right;
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].BackTriggerAction.started += BackTrigger;
        RCUPlayerInputManager.Instance.RCUPlayerInputs[0].OnVolumeUpButtonPress += VolumeUp;
    }

    void InitPlayer(InputAction.CallbackContext context)
    {
        RCUPlayerInputManager.Instance.Dispatcher.EnterAction.started -= InitPlayer;


    }

    void Up(InputAction.CallbackContext context)
    {
    }
    void Down(InputAction.CallbackContext context)
    {
    }
    void Left(InputAction.CallbackContext context)
    {
        var result = AndroidServerInfoDemo.Instance.GetAllInputDevices();
        return;
    }
    void Right(InputAction.CallbackContext context)
    {
        var result1 = AndroidServerInfoDemo.Instance.GetMajorMemInputDeviceDescriptor();
        var result2 = AndroidServerInfoDemo.Instance.GetMajorMemInputDevice();
        return;
    }
    void BackTrigger(InputAction.CallbackContext context)
    {
        Capabilities capa = JsonUtility.FromJson<Capabilities>(context.control.device.description.capabilities);
        var result = AndroidServerInfoDemo.Instance.GetInputDeviceByDescriptor(capa.deviceDescriptor);
        return;
    }

    void VolumeUp(YouDooSDKConstants.InputDevice inputDevice)
    {
        return;
    }

}
