using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScene : MonoBehaviour
{

    [SerializeField]
    public MainPanel mainPanel;

    [SerializeField]
    public DevicesPanel devicesPanel;

    [SerializeField]
    public SprayPaintPanel sprayPaintPanel;

    [SerializeField]
    public ShootingPanel shootingPanel;

    [SerializeField]
    public AudioRecordPanel audioRecordPanels;

    [SerializeField]
    public VibrationPanel vibrationPanel;

    [SerializeField]
    public KeyInputPanel KeyInputPanel;

    [SerializeField]
    public QuickDiagnosisPanel QuickDiagnosisPanel;

    [SerializeField]
    public PressureTestPanel PressureTestPanel;

    protected virtual void OnEnable()
    {
        if (RemoteControlUnitInputSystemManager.Instance != null)
        {
            RemoteControlUnitInputSystemManager.Instance.OnDownArrowPressed += OnDownArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnUpArrowPressed += OnUpArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnLeftArrowPressed += OnLeftArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnRightArrowPressed += OnRightArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnEscapePressed += OnEscapePressed;
            RemoteControlUnitInputSystemManager.Instance.OnButtonOKPressed += OnButtonOKPressed;
            RemoteControlUnitInputSystemManager.Instance.OnButtonJoystickButton1Pressed += OnButtonJoystickButton1Pressed;
        }
    }

    protected virtual void OnDisable()
    {
        if (RemoteControlUnitInputSystemManager.Instance != null)
        {
            RemoteControlUnitInputSystemManager.Instance.OnDownArrowPressed -= OnDownArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnUpArrowPressed -= OnUpArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnLeftArrowPressed -= OnLeftArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnRightArrowPressed -= OnRightArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnEscapePressed -= OnEscapePressed;
            RemoteControlUnitInputSystemManager.Instance.OnButtonOKPressed -= OnButtonOKPressed;
            RemoteControlUnitInputSystemManager.Instance.OnButtonJoystickButton1Pressed -= OnButtonJoystickButton1Pressed;
        }
    }

    protected virtual void OnButtonOKPressed() { }
    protected virtual void OnEscapePressed()
    {
        ShowMainPanel(mainPanel.gameObject);
    }
    protected virtual void OnRightArrowPressed() { }
    protected virtual void OnLeftArrowPressed() { }
    protected virtual void OnUpArrowPressed() { }
    protected virtual void OnDownArrowPressed() { }

    protected virtual void OnButtonJoystickButton1Pressed() { }

    public void ShowMainPanel(GameObject showPanel)
    {
        if (mainPanel != null) mainPanel.gameObject.SetActive(false);
        if (devicesPanel != null) devicesPanel.gameObject.SetActive(false);
        if (sprayPaintPanel != null) sprayPaintPanel.gameObject.SetActive(false);
        if (audioRecordPanels != null) audioRecordPanels.gameObject.SetActive(false);
        if (vibrationPanel != null) vibrationPanel.gameObject.SetActive(false);
        if (KeyInputPanel != null) KeyInputPanel.gameObject.SetActive(false);
        if (QuickDiagnosisPanel != null) QuickDiagnosisPanel.gameObject.SetActive(false);
        if (PressureTestPanel != null) PressureTestPanel.gameObject.SetActive(false);
        if (shootingPanel != null) shootingPanel.gameObject.SetActive(false);

        if (showPanel != null) showPanel.SetActive(true);
    }
}
