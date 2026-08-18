using UnityEngine;
using UnityEngine.UI;

public class RemoteInputBase : MonoBehaviour
{
    [SerializeField] protected Transform[] buttonGroupTransform;

    protected Image[] selectButtonImage;

    protected int _curSelcetIndex;
    protected int _groupSelectIndex;

    private bool _isSub = false;

    // 子导航模式相关字段
    protected bool _isInSubNavigation = false;
    protected int _subNavSelectIndex = 0;

    protected virtual void Start()
    {
        Debug.Log("RemoteInputBase Start called  17 ");
        SubscribeEvent();
        _curSelcetIndex = 0;
        _groupSelectIndex = 0;
    }

    protected virtual void SubscribeEvent()
    {
        Debug.Log("RemoteInputBase SubscribeEvent called  24 ");
        if (_isSub)
        {
            return;
        }
        RemoteControlUnitInputSystemManager.Instance.OnDownArrowPressed += OnDownArrowPressed;
        RemoteControlUnitInputSystemManager.Instance.OnUpArrowPressed += OnUpArrowPressed;

        RemoteControlUnitInputSystemManager.Instance.OnLeftArrowPressed += OnLeftArrowPressed;
        RemoteControlUnitInputSystemManager.Instance.OnRightArrowPressed += OnRightArrowPressed;

        RemoteControlUnitInputSystemManager.Instance.OnEscapePressed += OnEscapePressed;
        RemoteControlUnitInputSystemManager.Instance.OnButtonOKPressed += OnButtonOKPressed;
        _isSub = true;
        Debug.Log("RemoteInputBase Start called  38 ");
    }

    protected virtual void UnSubscribeEvent()
    {
        Debug.Log("RemoteInputBase UnSubscribeEvent   43 ");

        if (!_isSub)
        {
            return;
        }
        if (RemoteControlUnitInputSystemManager.Instance != null)
        {
            RemoteControlUnitInputSystemManager.Instance.OnDownArrowPressed -= OnDownArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnUpArrowPressed -= OnUpArrowPressed;

            RemoteControlUnitInputSystemManager.Instance.OnLeftArrowPressed -= OnLeftArrowPressed;
            RemoteControlUnitInputSystemManager.Instance.OnRightArrowPressed -= OnRightArrowPressed;


            RemoteControlUnitInputSystemManager.Instance.OnEscapePressed -= OnEscapePressed;
            RemoteControlUnitInputSystemManager.Instance.OnButtonOKPressed -= OnButtonOKPressed;
        }
        _isSub = false;
        Debug.Log("RemoteInputBase UnSubscribeEvent   62 ");
    }

    private void OnDownArrowPressed()
    {
        if (_isInSubNavigation)
        {
            SubNavOnDownPressed();
            return;
        }
        _curSelcetIndex++;
        if (_curSelcetIndex > selectButtonImage.Length - 1)
        {
            _curSelcetIndex = 0;
        }
        SetButtonState(true);
    }

    private void OnUpArrowPressed()
    {
        if (_isInSubNavigation)
        {
            SubNavOnUpPressed();
            return;
        }
        _curSelcetIndex--;
        if (_curSelcetIndex < 0)
        {
            _curSelcetIndex = selectButtonImage.Length - 1;
        }
        SetButtonState(true);
    }

    private void OnLeftArrowPressed()
    {
        if (_isInSubNavigation)
        {
            SubNavOnLeftPressed();
            return;
        }
        SetGroupSelectIndexBorder(false);
    }

    private void OnRightArrowPressed()
    {
        if (_isInSubNavigation)
        {
            SubNavOnRightPressed();
            return;
        }
        SetGroupSelectIndexBorder(true);
    }

    private void OnButtonOKPressed()
    {
        if (_isInSubNavigation)
        {
            SubNavOnOKPressed();
            return;
        }
        switch (_groupSelectIndex)
        {
            case 0:
                GroupButton1Change();
                break;
            case 1:
                GroupButton2Change();
                break;
            case 2:
                GroupButton3Change();
                break;
            case 3:
                GroupButton4Change();
                break;
            case 4:
                GroupButton5Change();
                break;
            case 5:
                GroupButton6Change();
                break;
            case 6:
                GroupButton7Change();
                break;

        }
    }

    protected virtual void GroupButton1Change()
    {

    }

    protected virtual void GroupButton2Change()
    {

    }

    protected virtual void GroupButton3Change()
    {

    }

    protected virtual void GroupButton4Change()
    {

    }

    protected virtual void GroupButton5Change()
    {

    }
    protected virtual void GroupButton6Change()
    {

    }

    protected virtual void GroupButton7Change()
    {

    }

    /// <summary>
    /// false表示全白
    /// </summary>
    /// <param name="isShow"></param>
    protected void SetButtonState(bool isShow)
    {
        Debug.Log("RemoteInputBase 父类 点击了确认按钮: 操作 ！！！" + isShow);
        if (selectButtonImage == null)
        {
            // Debug.LogWarning("SetButtonState: selectButtonImage is null");
            return;
        }

        if (!isShow)
        {
            foreach (var item in selectButtonImage)
            {
                if (item != null) item.color = Color.white;
            }
            return;
        }
        for (int i = 0; i < selectButtonImage.Length; i++)
        {
            Image tempImage = selectButtonImage[i];
            if (tempImage == null) continue;

            if (_curSelcetIndex == i)
            {
                tempImage.color = Color.green;
            }
            else
            {
                tempImage.color = Color.white;
            }
        }
    }


    private void SetGroupSelectIndexBorder(bool symbol)
    {
        if (buttonGroupTransform == null) return;

        int tempIndex = _groupSelectIndex + (symbol ? 1 : -1);

        if (tempIndex > buttonGroupTransform.Length - 1)
        {
            tempIndex = 0;
        }

        if (tempIndex < 0)
        {
            tempIndex = buttonGroupTransform.Length - 1;
        }
        SetButtonState(false);
        _groupSelectIndex = tempIndex;
        SetNewButtonImageArray();
        _curSelcetIndex = 0;
        SetButtonState(true);
    }

    protected void SetNewButtonImageArray()
    {
        if (buttonGroupTransform == null)
        {
            Debug.LogError("SetNewButtonImageArray: buttonGroupTransform is null!");
            return;
        }

        if (_groupSelectIndex < 0 || _groupSelectIndex >= buttonGroupTransform.Length)
        {
            Debug.LogError($"SetNewButtonImageArray: _groupSelectIndex {_groupSelectIndex} is out of range. buttonGroupTransform.Length: {buttonGroupTransform.Length}");
            return;
        }

        Transform tempTransform = buttonGroupTransform[_groupSelectIndex];

        if (tempTransform == null)
        {
            Debug.LogError($"SetNewButtonImageArray: buttonGroupTransform element at index {_groupSelectIndex} is null!");
            return;
        }

        int childCount = tempTransform.childCount;
        selectButtonImage = new Image[childCount];
        for (int i = 0; i < childCount; i++)
        {
            selectButtonImage[i] = tempTransform.GetChild(i).GetComponent<Image>();
        }
    }


    private void OnEscapePressed()
    {
        if (_isInSubNavigation)
        {
            SubNavOnEscapePressed();
            return;
        }
        EscapePressedBase();
    }

    protected virtual void EscapePressedBase()
    {

    }

    #region 子导航模式

    /// <summary>
    /// 进入子导航模式，隐藏所有按钮组
    /// </summary>
    protected virtual void EnterSubNavigation()
    {
        _isInSubNavigation = true;
        _subNavSelectIndex = 0;

        // 隐藏所有按钮组
        if (buttonGroupTransform != null)
        {
            for (int i = 0; i < buttonGroupTransform.Length; i++)
            {
                if (buttonGroupTransform[i] != null)
                {
                    buttonGroupTransform[i].gameObject.SetActive(false);
                }
            }
        }

        Debug.Log($"[SubNav] 进入子导航模式, groupIndex={_groupSelectIndex}");
    }

    /// <summary>
    /// 退出子导航模式，恢复显示所有按钮组
    /// </summary>
    protected virtual void ExitSubNavigation()
    {
        _isInSubNavigation = false;

        // 恢复显示所有按钮组
        if (buttonGroupTransform != null)
        {
            for (int i = 0; i < buttonGroupTransform.Length; i++)
            {
                if (buttonGroupTransform[i] != null)
                {
                    buttonGroupTransform[i].gameObject.SetActive(true);
                }
            }
        }

        // 恢复按钮高亮状态
        SetButtonState(true);

        Debug.Log($"[SubNav] 退出子导航模式, groupIndex={_groupSelectIndex}");
    }

    /// <summary>
    /// 子导航模式 - 上键按下
    /// </summary>
    protected virtual void SubNavOnUpPressed()
    {
        _subNavSelectIndex--;
        if (_subNavSelectIndex < 0)
        {
            _subNavSelectIndex = 0;
        }
        Debug.Log($"[SubNav] Up pressed, subNavSelectIndex={_subNavSelectIndex}");
    }

    /// <summary>
    /// 子导航模式 - 下键按下
    /// </summary>
    protected virtual void SubNavOnDownPressed()
    {
        _subNavSelectIndex++;
        Debug.Log($"[SubNav] Down pressed, subNavSelectIndex={_subNavSelectIndex}");
    }

    /// <summary>
    /// 子导航模式 - 左键按下
    /// </summary>
    protected virtual void SubNavOnLeftPressed()
    {
        Debug.Log($"[SubNav] Left pressed, subNavSelectIndex={_subNavSelectIndex}");
    }

    /// <summary>
    /// 子导航模式 - 右键按下
    /// </summary>
    protected virtual void SubNavOnRightPressed()
    {
        Debug.Log($"[SubNav] Right pressed, subNavSelectIndex={_subNavSelectIndex}");
    }

    /// <summary>
    /// 子导航模式 - 确认键按下
    /// </summary>
    protected virtual void SubNavOnOKPressed()
    {
        Debug.Log($"[SubNav] OK pressed, subNavSelectIndex={_subNavSelectIndex}");
    }

    /// <summary>
    /// 子导航模式 - 返回键按下，默认退出子导航
    /// </summary>
    protected virtual void SubNavOnEscapePressed()
    {
        ExitSubNavigation();
    }

    #endregion

    protected virtual void OnDestroy()
    {

        Debug.Log("RemoteInputBase  OnDestroy 销毁 ：230  ");
        UnSubscribeEvent();
    }

    protected virtual void Awake()
    {

        Debug.Log("RemoteInputBase  Awake 开始 ：237  ");
    }

    protected virtual void OnEnable()
    {

        Debug.Log("RemoteInputBase  OnEnable 可见 ：243  ");
    }

    protected virtual void OnDisable()
    {

        Debug.Log("RemoteInputBase  OnDisable 不可见 ：248  ");
    }

    protected virtual void Update()
    {
        if (Time.frameCount % 60 == 0) // 每60帧打印一次，避免刷屏
        {
            //Debug.Log($"RemoteInputBase  Update 更新  ：248  {Time.frameCount}");

        }

    }

    protected virtual void FixedUpdate()
    {
        
    }
}
