using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using YouDooSDK.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static YouDooSDKConstants;

[System.Serializable]
public class FindBlueToothDevicesPopupData
{
    public string title;
    public string confirmText;
    public string cancelText;
    public Action<string> onConfirm;
    public Action onCancel;
}



public class FindBlueToothDevicesPopup : BasePopup, IDataPopup<FindBlueToothDevicesPopupData>
{
    [Header("UI组件")]
    public Text titleText;


    [SerializeField] private GameObject gameObjectPrefab;

    [SerializeField] private Transform transformContent;
    [SerializeField] private Transform findingUi;
    private FindDevicesItem[] _detailInfoItems;

    private List<DeviceStatusInfo> _blueToothDevices;

    private FindBlueToothDevicesPopupData confirmData;



    private int _curColConut = 0;

    private int _curSelectIndex = 0;


    // 实现泛型 SetData 方法
    public void SetData(FindBlueToothDevicesPopupData data)
    {
        confirmData = data;
    }

    public override void Initialize()
    {
        base.Initialize();

        // 设置UI内容
        if (confirmData != null)
        {
            UpdateUI();
        }
        // 添加关闭事件监听
        OnShowEvent += () => Debug.Log("确认对话框已显示");
        OnHideEvent += () => Debug.Log("确认对话框已隐藏");
        OnCloseEvent += () => Debug.Log("确认对话框已关闭");
        _curColConut = transformContent.GetComponent<GridLayoutGroup>().constraintCount;
    }

    private void SetCurPrefab()
    {
        if (_blueToothDevices == null || _blueToothDevices.Count == 0)
        {
            Debug.LogWarning("蓝牙设备列表为空");
            foreach (Transform child in transformContent)
            {
                child.gameObject.SetActive(false);
            }
            findingUi.gameObject.SetActive(true);
            return;
        }
        findingUi.gameObject.SetActive(false);
        int deviceCount = _blueToothDevices.Count;

        if (_detailInfoItems == null || _detailInfoItems.Length < deviceCount)
        {
            Array.Resize(ref _detailInfoItems, deviceCount);
        }

        EnsureChildrenCount(deviceCount);
        for (int i = 0; i < deviceCount; i++)
        {
            Transform child = transformContent.GetChild(i);

            if (!child.TryGetComponent<FindDevicesItem>(out var item))
            {
                Debug.LogError("FindDevicesItem 脚本未挂载到预制体上！");
                continue;
            }
            _detailInfoItems[i] = item;
            item.InitDetailInfoItem(_blueToothDevices[i], i, _curSelectIndex);
            child.gameObject.SetActive(true);
        }

        // 禁用多余的对象而不是销毁
        DisableExtraChildren(deviceCount);
    }

    public override void OnShow()
    {
        base.OnShow();
        AndroidServerInfoDemo.OnFindBlueTooth += OnFindBlueTooth;
    }


    private void EnsureChildrenCount(int requiredCount)
    {
        int currentCount = transformContent.childCount;

        // 只创建缺少的对象
        for (int i = currentCount; i < requiredCount; i++)
        {
            Instantiate(gameObjectPrefab, transformContent);
        }
    }

    private void DisableExtraChildren(int requiredCount)
    {
        // 禁用多余的对象，而不是销毁
        for (int i = requiredCount; i < transformContent.childCount; i++)
        {
            transformContent.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void UpdateUI()
    {

        _curSelectIndex = 0;
        titleText.text = confirmData.title;
    }

    // 重写输入事件处理方法
    protected override void OnEscapePressed()
    {
        // 按ESC键相当于点击取消按钮
        confirmData?.onCancel?.Invoke();
        Close();
    }

    protected override void OnButtonOKPressed()
    {
        // 按OK键相当于点击确认按钮
        confirmData?.onConfirm?.Invoke(_blueToothDevices[_curSelectIndex].address);
        Close();
    }

    protected override void OnDownArrowPressed()
    {
        _curSelectIndex += _curColConut;
        ResetIndex();
    }

    protected override void OnUpArrowPressed()
    {
        _curSelectIndex -= _curColConut;
        ResetIndex();
    }

    protected override void OnLeftArrowPressed()
    {
        _curSelectIndex--;
        ResetIndex();
    }

    protected override void OnRightArrowPressed()
    {
        _curSelectIndex++;
        ResetIndex();
    }

    private void ResetIndex()
    {
        if (_curSelectIndex > _detailInfoItems.Length - 1)
        {
            _curSelectIndex = 0;
        }
        else if (_curSelectIndex < 0)
        {
            _curSelectIndex = _detailInfoItems.Length - 1;
        }

        foreach (var item in _detailInfoItems)
        {
            item.SetSelectedState(_curSelectIndex);
        }
    }

    // 重写关闭方法，添加额外逻辑
    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("执行确认对话框的清理工作");
        AndroidServerInfoDemo.OnFindBlueTooth -= OnFindBlueTooth;
    }

    private void OnFindBlueTooth(DeviceStatusInfo deviceStatusInfo)
    {
        Debug.Log($"207 207 207 刷新 蓝牙列表 在UI上 主要我想看为啥来了两次 ");
        _blueToothDevices ??= new List<DeviceStatusInfo>();

        var existingDevice = _blueToothDevices.FirstOrDefault(device => device.address == deviceStatusInfo.address);

        if (existingDevice != null)
        {
            // 更新已存在设备的信息
            int index = _blueToothDevices.IndexOf(existingDevice);
            _blueToothDevices[index] = deviceStatusInfo;
            Debug.Log($"设备 {deviceStatusInfo.address} 信息已更新");
        }
        else
        {
            // 添加新设备
            _blueToothDevices.Add(deviceStatusInfo);
            Debug.Log($"添加新设备 {deviceStatusInfo.address}");
        }
        SetCurPrefab();
    }
}
