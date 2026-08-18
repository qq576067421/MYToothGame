
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
    // [SerializeField] BluetoothDemo blueToothDemo;
    [FormerlySerializedAs("gyroDataTestDemo")]
    [SerializeField] GyroDataReader gyroDataReader;

    [SerializeField] TMP_Dropdown targetControllerDropdown;
    [SerializeField] TextMeshProUGUI targetControllerDropdownLabel;
    [SerializeField] TextMeshProUGUI gyroXDataTMP;
    [SerializeField] TextMeshProUGUI gyroYDataTMP;
    [SerializeField] TextMeshProUGUI gyroZDataTMP;
    [SerializeField] TextMeshProUGUI gyroMagDataTMP;
    [SerializeField] TextMeshProUGUI gyroXDataDegTMP;
    [SerializeField] TextMeshProUGUI gyroYDataDegTMP;
    [SerializeField] TextMeshProUGUI gyroZDataDegTMP;
    [SerializeField] TextMeshProUGUI gyroMagDataDegTMP;
    [SerializeField] TextMeshProUGUI acceXDataTMP;
    [SerializeField] TextMeshProUGUI acceYDataTMP;
    [SerializeField] TextMeshProUGUI acceZDataTMP;
    [SerializeField] TextMeshProUGUI acceMagataTMP;
    [SerializeField] TextMeshProUGUI magnetXDataTMP;
    [SerializeField] TextMeshProUGUI magnetYDataTMP;
    [SerializeField] TextMeshProUGUI magnetZDataTMP;
    [SerializeField] TextMeshProUGUI magnetMagDataTMP;
    [SerializeField] TextMeshProUGUI timeDataTMP;
    [SerializeField] TMP_Dropdown unityFilterDropdown;
    [SerializeField] TextMeshProUGUI unityFilterDropdownLabel;
    [SerializeField] TMP_Dropdown sdkFilterDropdown;
    [SerializeField] TextMeshProUGUI sdkFilterDropdownLabel;
    [SerializeField] GameObject[] filterDebugUI;
    [SerializeField] Slider alphaThresholdSlider;
    //
    [SerializeField] Slider acceTrustMinSlider;
    [SerializeField] TextMeshProUGUI acceDeltaMinSliderData;
    [SerializeField] Slider acceDeltaMaxSlider;
    [SerializeField] TextMeshProUGUI acceDeltaMaxSliderData;
    [SerializeField] Slider gyroTrustMinSlider;
    [SerializeField] TextMeshProUGUI gyroTrustMinSliderData;
    [SerializeField] Slider gyroTrustMaxSlider;
    [SerializeField] TextMeshProUGUI gyroTrustMaxSliderData;
    [SerializeField] Slider filteredGravityAlphaMin;
    [SerializeField] TextMeshProUGUI filteredGravityAlphaMinData;
    [SerializeField] Slider filteredGravityAlphaMax;
    [SerializeField] TextMeshProUGUI filteredGravityAlphaMaxData;
    [SerializeField] Slider acceLerpSlider;
    [SerializeField] TextMeshProUGUI acceLerpData;
    [SerializeField] Slider magnetYawGainSlider;
    [SerializeField] TextMeshProUGUI magnetYawGainData;
    //
    [SerializeField] Slider ahrsBetaSlider;
    [SerializeField] TextMeshProUGUI ahrsBetaData;

    //
    [SerializeField] TextMeshProUGUI alphaDataTMP;
    [SerializeField] TextMeshProUGUI varDataTMP;
    [SerializeField] TextMeshProUGUI varAlphaDataTMP;
    [SerializeField] Transform FindListRoot;
    [SerializeField] Transform BindListRoot;
    [SerializeField] Transform UseListRoot;
    [SerializeField] TextMeshProUGUI listTextTemplateTMP;

    //
    [SerializeField] GameObject gyroMagDataHintImgGO;
    float hintTimer = 0.0f;
    const float HintActiveTime = 1.0f;
    const float GyroMagMinium = 10.0f;

    [Header("Controller Dropdown")]
    [SerializeField] private float controllerDropdownRefreshInterval = 0.5f;

    private readonly List<string> targetControllerMacs = new List<string>();
    private float nextControllerDropdownRefreshTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created 

    void Start()
    {
        RefreshTargetControllerDropdown(true);

        if (targetControllerDropdown != null)
        {
            targetControllerDropdown.Select();
        }

        OnTargetControllerChanged(targetControllerDropdown != null ? targetControllerDropdown.value : 0);
        // OnUnityFilterChanged(0);
        // OnSDKFilterChanged(0);
    }

    // Update is called once per frame
    void Update()
    {
        RefreshTargetControllerDropdown(false);

        if (gyroDataReader == null) return;

        var index = gyroDataReader.TargetIndex;
        if (index < 0) return;

        var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
        if (gyroItem == null) return;

        var gyroData = gyroDataReader.GyroData;
        var degData = gyroDataReader.DegData;
        var acceData = gyroDataReader.AcceData;
        var magnetData = gyroDataReader.MagnetData;

        if (gyroXDataTMP != null) gyroXDataTMP.text = gyroData.x.ToString("F0");
        if (gyroYDataTMP != null) gyroYDataTMP.text = gyroData.y.ToString("F0");
        if (gyroZDataTMP != null) gyroZDataTMP.text = gyroData.z.ToString("F0");
        if (gyroMagDataTMP != null) gyroMagDataTMP.text = gyroData.magnitude.ToString("F2");

        if (gyroXDataDegTMP != null) gyroXDataDegTMP.text = degData.x.ToString("F2");
        if (gyroYDataDegTMP != null) gyroYDataDegTMP.text = degData.y.ToString("F2");
        if (gyroZDataDegTMP != null) gyroZDataDegTMP.text = degData.z.ToString("F2");
        if (gyroMagDataDegTMP != null) gyroMagDataDegTMP.text = degData.magnitude.ToString("F2");

        if (acceXDataTMP != null) acceXDataTMP.text = acceData.x.ToString("F2");
        if (acceYDataTMP != null) acceYDataTMP.text = acceData.y.ToString("F2");
        if (acceZDataTMP != null) acceZDataTMP.text = acceData.z.ToString("F2");
        if (acceMagataTMP != null) acceMagataTMP.text = acceData.magnitude.ToString("F2");

        if (magnetXDataTMP != null) magnetXDataTMP.text = magnetData.x.ToString("F2");
        if (magnetYDataTMP != null) magnetYDataTMP.text = magnetData.y.ToString("F2");
        if (magnetZDataTMP != null) magnetZDataTMP.text = magnetData.z.ToString("F2");
        if (magnetMagDataTMP != null) magnetMagDataTMP.text = magnetData.magnitude.ToString("F2");

        acceDeltaMinSliderData.text = gyroItem.GyroParams.AcceDeltaMin.ToString("F2");
        acceDeltaMaxSliderData.text = gyroItem.GyroParams.AcceDeltaMax.ToString("F2");

        gyroTrustMinSliderData.text = gyroItem.GyroParams.GyroTrustMin.ToString("F2");
        gyroTrustMaxSliderData.text = gyroItem.GyroParams.GyroTrustMax.ToString("F2");
        filteredGravityAlphaMinData.text = gyroItem.GyroParams.GravityFixAlphaMin.ToString("F2");
        filteredGravityAlphaMaxData.text = gyroItem.GyroParams.GravityFixAlphaMax.ToString("F2");

        acceLerpData.text = gyroItem.GyroParams.AcceLerp.ToString("F2");
        magnetYawGainData.text = gyroItem.GyroParams.MagnetYawGain.ToString("F2");

        if (timeDataTMP != null) timeDataTMP.text = gyroDataReader.TimeCount.ToString("F2");

        UpdateHintImage(gyroData.magnitude);
    }

    void UpdateHintImage(float gyroMag)
    {
        hintTimer = Mathf.Max(hintTimer - Time.deltaTime, 0.0f);
        if (gyroMag > GyroMagMinium)
        {
            hintTimer = HintActiveTime;
        }

        gyroMagDataHintImgGO.SetActive(hintTimer > 0.0f);
    }

    public void OnTargetControllerChanged(int index)
    {
        RefreshTargetControllerDropdown(true);

        if (targetControllerMacs.Count <= 0)
        {
            if (targetControllerDropdownLabel != null)
            {
                targetControllerDropdownLabel.text = RenderAPI.GetTextByLanId("sdk_demo_no_gyro");
            }

            if (gyroDataReader != null)
            {
                gyroDataReader.SetTargetIndex(-1);
            }

            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, targetControllerMacs.Count - 1);
        if (targetControllerDropdown != null && targetControllerDropdown.value != clampedIndex)
        {
            targetControllerDropdown.SetValueWithoutNotify(clampedIndex);
        }

        UpdateTargetControllerDropdownLabel(clampedIndex);

        if (gyroDataReader != null)
        {
            gyroDataReader.SetTargetIndex(clampedIndex);
        }
    }

    private void RefreshTargetControllerDropdown(bool force)
    {
        if (targetControllerDropdown == null)
        {
            return;
        }

        if (!force && Time.time < nextControllerDropdownRefreshTime)
        {
            return;
        }

        nextControllerDropdownRefreshTime = Time.time + controllerDropdownRefreshInterval;

        List<string> currentMacs = GetCurrentControllerMacs();
        bool changed = currentMacs.Count != targetControllerMacs.Count || !currentMacs.SequenceEqual(targetControllerMacs);
        if (!changed)
        {
            return;
        }

        targetControllerMacs.Clear();
        targetControllerMacs.AddRange(currentMacs);

        targetControllerDropdown.ClearOptions();

        if (targetControllerMacs.Count <= 0)
        {
            targetControllerDropdown.AddOptions(new List<string> { RenderAPI.GetTextByLanId("sdk_demo_no_gyro") });
            targetControllerDropdown.SetValueWithoutNotify(0);

            if (targetControllerDropdownLabel != null)
            {
                targetControllerDropdownLabel.text = RenderAPI.GetTextByLanId("sdk_demo_no_gyro");
            }

            if (gyroDataReader != null)
            {
                gyroDataReader.SetTargetIndex(-1);
            }

            return;
        }

        List<string> options = new List<string>();
        for (int i = 0; i < targetControllerMacs.Count; i++)
        {
            options.Add(RenderAPI.GetTextByLanId("sdk_demo_gyro_option", i + 1, targetControllerMacs[i]));
        }

        targetControllerDropdown.AddOptions(options);

        int selectedIndex = gyroDataReader != null ? gyroDataReader.TargetIndex : targetControllerDropdown.value;
        selectedIndex = Mathf.Clamp(selectedIndex, 0, targetControllerMacs.Count - 1);

        targetControllerDropdown.SetValueWithoutNotify(selectedIndex);
        UpdateTargetControllerDropdownLabel(selectedIndex);

        if (gyroDataReader != null)
        {
            gyroDataReader.SetTargetIndex(selectedIndex);
        }
    }

    private List<string> GetCurrentControllerMacs()
    {
        if (AndroidServerInfo.Instance == null || AndroidServerInfo.Instance.Bluetooth == null)
        {
            return new List<string>();
        }

        return AndroidServerInfo.Instance.Bluetooth.HardWareRemoteControlMap.Keys
            .Select(Bluetooth.NormalizeDeviceMac)
            .Where(mac => !string.IsNullOrEmpty(mac))
            .ToList();
    }

    private void UpdateTargetControllerDropdownLabel(int index)
    {
        if (targetControllerDropdownLabel == null)
        {
            return;
        }

        if (targetControllerDropdown != null &&
            targetControllerDropdown.options != null &&
            index >= 0 &&
            index < targetControllerDropdown.options.Count)
        {
            targetControllerDropdownLabel.text = targetControllerDropdown.options[index].text;
            return;
        }

        targetControllerDropdownLabel.text = RenderAPI.GetTextByLanId("sdk_demo_no_gyro");
    }

    public void OnResetCubePressed()
    {
        var index = gyroDataReader.TargetIndex;
        if (index < 0) return;
        gyroDataReader.ResetGyroMappingState();
    }

    public void OnMiniGameButtonPress()
    {
        SceneManager.LoadScene(DemoUtil.SceneName.GyroScopeMiniGameScene.ToString());
    }

    public void OnAcceDeltaMinChanged(float value)
    {
        var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
        var newParam = gyroItem.GyroParams;
        newParam.AcceDeltaMin = value;
        gyroItem.GyroParams = newParam;
        gyroItem.SetGyroFilterParams();
    }

    public void OnAcceDeltaMaxChanged(float value)
    {
        var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
        var newParam = gyroItem.GyroParams;
        newParam.AcceDeltaMax = value;
        gyroItem.GyroParams = newParam;
        gyroItem.SetGyroFilterParams();
    }

    public void OnGyroTrustMinChanged(float value)
    {
        var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
        var newParam = gyroItem.GyroParams;
        newParam.GyroTrustMin = value;
        gyroItem.GyroParams = newParam;
        gyroItem.SetGyroFilterParams();
    }

    public void OnGyroTrustMaxChanged(float value)
    {
        var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
        var newParam = gyroItem.GyroParams;
        newParam.GyroTrustMax = value;
        gyroItem.GyroParams = newParam;
        gyroItem.SetGyroFilterParams();
    }

    public void OnAlphaMinChanged(float value)
    {
        var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
        var newParam = gyroItem.GyroParams;
        newParam.GravityFixAlphaMin = value;
        gyroItem.GyroParams = newParam;
        gyroItem.SetGyroFilterParams();
    }

    public void OnAlphaMaxChanged(float value)
    {
        var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
        var newParam = gyroItem.GyroParams;
        newParam.GravityFixAlphaMax = value;
        gyroItem.GyroParams = newParam;
        gyroItem.SetGyroFilterParams();
    }

    public void OnAcceLerpChanged(float value)
    {
        var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
        var newParam = gyroItem.GyroParams;
        newParam.AcceLerp = value;
        gyroItem.GyroParams = newParam;
        gyroItem.SetGyroFilterParams();
    }

    public void OnMagnetYawGain(float value)
    {
        var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
        var newParam = gyroItem.GyroParams;
        newParam.MagnetYawGain = value;
        gyroItem.GyroParams = newParam;
        gyroItem.SetGyroFilterParams();
    }

    public void FindAllBlueTooth()
    {

    }
    public void UpdateFindList(string[] devices)
    {

    }
    public void UpdateBindList(string[] devices)
    {
        var children = BindListRoot.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var child in children)
        {
            Destroy(child);
        }

        foreach (var device in devices)
        {
            var newGO = Instantiate(listTextTemplateTMP.gameObject, BindListRoot);
            var newTMP = newGO.GetComponent<TextMeshProUGUI>();
            newTMP.text = device;

            newGO.SetActive(true);
        }
    }

    public void UpdateUseList(string[] devices)
    {
        var children = UseListRoot.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var child in children)
        {
            Destroy(child);
        }

        foreach (var device in devices)
        {
            var newGO = Instantiate(listTextTemplateTMP.gameObject, UseListRoot);
            var newTMP = newGO.GetComponent<TextMeshProUGUI>();
            newTMP.text = device;

            newGO.SetActive(true);
        }
    }
}
