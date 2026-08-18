using System;
using System.Collections;
using System.Data.Common;
using YouDooSDK.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ModeSelectopupData
{
    public string title;
    public string confirmText;
    public string cancelText;

    public int curSelectIndex;

    public Color bgColor;
    public Action<int> onConfirm;
    public Action onCancel;

    public ModelConfig[] modelConfigData;

    public CameraConfig[] cameraConfigData;

    public Resolution[] resolutionData;
}

public class ChangeModePopup : BasePopup, IDataPopup<ModeSelectopupData>
{
    [Header("UI组件")]
    public Text titleText;
    public Button confirmButton;
    public Button cancelButton;
    public Text confirmButtonText;
    public Text cancelButtonText;

    private ModeSelectopupData confirmData;

    [SerializeField] private GameObject gameObjectPrefab;

    [SerializeField] private Transform transformContent;

    private ChangeModeItem[] _detailInfoItems;

    private int _curColConut = 0;

    private int _curSelectIndex = 0;


    // 实现泛型 SetData 方法
    public void SetData(ModeSelectopupData data)
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

    private void SetCurPrefab<T>(T[] arrayA)
    {
        _detailInfoItems ??= new ChangeModeItem[arrayA.Length];
        ClearChildren();
        for (int i = 0; i < arrayA.Length; i++)
        {
            GameObject instance = Instantiate(gameObjectPrefab, transformContent);
            Debug.Log($"实例化对象 {i} 成功！");
            _detailInfoItems[i] = instance.GetComponent<ChangeModeItem>();
            if (_detailInfoItems[i] == null)
            {
                Debug.LogError("DetailInfoItem 脚本未挂载到预制体上！");
                continue;
            }
            instance.SetActive(true);
            if (confirmData.modelConfigData != null)
            {
                _detailInfoItems[i].InitDetailInfoItem($"{(arrayA[i] as ModelConfig).name}", i, _curSelectIndex);
            }
            else if (confirmData.cameraConfigData != null)
            {
                _detailInfoItems[i].InitDetailInfoItem((arrayA[i] as CameraConfig).cameraId, i, _curSelectIndex);
            }
            else if (confirmData.resolutionData != null)
            {
                _detailInfoItems[i].InitDetailInfoItem($"{(arrayA[i] as Resolution).height} *{(arrayA[i] as Resolution).width} ", i, _curSelectIndex);
            }
        }

    }



    private void ClearChildren()
    {
        if (transformContent == null) return;

        foreach (Transform child in transformContent)
        {
            Destroy(child.gameObject);
        }
    }

    private void UpdateUI()
    {
        if (confirmData.modelConfigData != null)
        {
            SetCurPrefab<ModelConfig>(confirmData.modelConfigData);
        }
        else if (confirmData.cameraConfigData != null)
        {
            SetCurPrefab<CameraConfig>(confirmData.cameraConfigData);
        }
        else if (confirmData.resolutionData != null)
        {
            SetCurPrefab<Resolution>(confirmData.resolutionData);
        }

        _curSelectIndex = confirmData.curSelectIndex;
        transform.GetComponent<Image>().color = confirmData.bgColor;
        titleText.text = confirmData.title;
        confirmButtonText.text = confirmData.confirmText ?? RenderAPI.GetTextByLanId("sdk_demo_confirm");
        cancelButtonText.text = confirmData.cancelText ?? RenderAPI.GetTextByLanId("sdk_demo_cancel");
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
        confirmData?.onConfirm?.Invoke(_curSelectIndex);
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

        // 清理按钮事件
        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
    }
}
