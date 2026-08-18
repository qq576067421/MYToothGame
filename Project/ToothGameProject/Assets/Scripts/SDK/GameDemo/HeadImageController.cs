using System.Collections.Generic;
using UnityEngine;
using YouDooSDK.UI;

public class HeadImageController : MonoBehaviour
{
    [Tooltip("存放场景中的 1~4 个 HeadImage（人脸照片）")]
    public HeadImage[] headImagePlayers;

    [Tooltip("存放场景中的 1~4 个 HeadImage（账号头像）")]
    public HeadImage[] headImageAvatarUri;

    [Tooltip("存放场景中的 1~4 个选中框 GameObject")]
    public GameObject[] selectionFrames;

    [Tooltip("默认头像纹理（有人但未识别时显示）")]
    public Texture defaultHeadTexture;

    [Tooltip("引用 AndroidParseDataDemo 以获取索引和订阅事件")]
    public AndroidParseDataDemo parseDataDemo;

    private int _lastPlayerCount = -1;
    private bool _hasSubscribed = false;

    // 记录哪些槽位当前有玩家
    private HashSet<int> _slotHasPlayer = new HashSet<int>();

    // 记录每个槽位上次加载的头像URI，避免重复加载
    private Dictionary<int, string> _lastFaceAvatarUri = new Dictionary<int, string>();

    #region 手动选角状态

    // 当前是否处于手动选角 UI 模式
    private bool _isSelectionActive = false;

    // 当前被手动选角 UI 高亮的槽位索引
    private int _currentSelectedIndex = -1;

    // 当前是否已经向 SDK 发起选角请求，正在等待回调
    private bool _isWaitingForSelectionResult = false;

    /// <summary>
    /// 当前是否处于"已进入手动选角 UI，但还没向 SDK 发请求"的状态
    /// </summary>
    private bool IsSelectingSlot => _isSelectionActive && !_isWaitingForSelectionResult;

    /// <summary>
    /// 供外部查询当前头像选择模式是否激活
    /// </summary>
    public bool IsSelectionActive => _isSelectionActive;

    #endregion

    void Update()
    {
        if (parseDataDemo != null && parseDataDemo.CurPlayerCount != _lastPlayerCount)
        {
            _lastPlayerCount = parseDataDemo.CurPlayerCount;
            RefreshActiveImages(_lastPlayerCount);
        }

        // 订阅 parseDataDemo 的事件（延迟订阅，确保 parseDataDemo 已初始化）
        if (!_hasSubscribed && parseDataDemo != null)
        {
            SubscribeParseDataEvents();
        }
    }

    private void RefreshActiveImages(int count)
    {
        if (headImagePlayers != null)
        {
            for (int i = 0; i < headImagePlayers.Length; i++)
            {
                if (headImagePlayers[i] != null)
                {
                    headImagePlayers[i].gameObject.SetActive(i < count);
                }
            }
        }

        if (headImageAvatarUri != null)
        {
            for (int i = 0; i < headImageAvatarUri.Length; i++)
            {
                if (headImageAvatarUri[i] != null)
                {
                    headImageAvatarUri[i].gameObject.SetActive(i < count);
                }
            }
        }

        // 如果选角模式激活但当前选中槽位超出范围，重新选择
        if (_isSelectionActive)
        {
            if (_currentSelectedIndex >= count || !_slotHasPlayer.Contains(_currentSelectedIndex))
            {
                TrySelectFirstActiveSlot();
            }
            if (_currentSelectedIndex < 0)
            {
                ResetSelectionState();
            }
            UpdateSelectionVisuals();
        }
    }

    void OnEnable()
    {
        // 订阅 SDK 层的人脸识别回调（用于获取 userId 和加载头像）
        AndroidServerInfoDemo.OnFaceRecognizedUser += HandleFaceRecognized;
        _slotHasPlayer.Clear();
        _lastFaceAvatarUri.Clear();
        ResetSelectionState();
        UpdateSelectionVisuals();

        // 尝试订阅 parseDataDemo 事件
        if (parseDataDemo != null && !_hasSubscribed)
        {
            SubscribeParseDataEvents();
        }
    }

    void OnDisable()
    {
        AndroidServerInfoDemo.OnFaceRecognizedUser -= HandleFaceRecognized;
        UnsubscribeParseDataEvents();
        ResetSelectionState();
        UpdateSelectionVisuals();
    }

    #region parseDataDemo 事件订阅

    private void SubscribeParseDataEvents()
    {
        if (_hasSubscribed || parseDataDemo == null) return;

        parseDataDemo.onPlayerFaceRecognizing += HandlePlayerFaceRecognizing;
        parseDataDemo.onNoneIsArea += HandlePlayerClear;
        parseDataDemo.onPlayerNotGame += HandlePlayerNotGame;
        _hasSubscribed = true;
        Debug.Log("[HeadImageController] 已订阅 parseDataDemo 事件");
    }

    private void UnsubscribeParseDataEvents()
    {
        if (!_hasSubscribed || parseDataDemo == null) return;

        parseDataDemo.onPlayerFaceRecognizing -= HandlePlayerFaceRecognizing;
        parseDataDemo.onNoneIsArea -= HandlePlayerClear;
        parseDataDemo.onPlayerNotGame -= HandlePlayerNotGame;
        _hasSubscribed = false;
        Debug.Log("[HeadImageController] 已取消订阅 parseDataDemo 事件");
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 玩家进入槽位，正在等待人脸识别结果 → 显示默认头像
    /// </summary>
    private void HandlePlayerFaceRecognizing(int slotIndex)
    {
        if (slotIndex < 0) return;

        // 标记该槽位有玩家
        _slotHasPlayer.Add(slotIndex);

        // 显示默认头像
        ShowDefaultHead(slotIndex);
        Debug.Log($"[HeadImageController] 槽位 {slotIndex} 玩家进框，显示默认头像");
    }

    /// <summary>
    /// SDK 人脸识别成功回调 → 显示真实头像
    /// </summary>
    private void HandleFaceRecognized(int personId, long userId, bool isRecognized)
    {
        if (parseDataDemo == null) return;

        // 1. 获取这个 personId 对应的玩家索引
        int playerIndex = parseDataDemo.GetPlayerIndexByPersonId(personId);

        if (isRecognized)
        {
            // 选角成功回调，使用当前选中的槽位（如果在选角等待中）
            if (_isWaitingForSelectionResult && _currentSelectedIndex >= 0)
            {
                playerIndex = _currentSelectedIndex;
            }
            else
            {
                playerIndex = 0;
            }
        }

        // 2. 判断索引是否有效
        if (playerIndex < 0) return;

        // 标记该槽位有玩家
        _slotHasPlayer.Add(playerIndex);

        // 3. 从 PlayerRoleManager 获取头像路径
        string facePhotoPath = PlayerRoleManager.Instance.GetFacePhotoPathByUserId(userId);
        string avatarUri = PlayerRoleManager.Instance.GetAvatarUriPathByUserId(userId);

        // 加载人脸照片
        if (!string.IsNullOrEmpty(facePhotoPath) && headImagePlayers != null && playerIndex < headImagePlayers.Length)
        {
            Debug.Log($"[HeadImageController] 为槽位 {playerIndex} 加载人脸照片: {facePhotoPath}");
            headImagePlayers[playerIndex].ShowImage(facePhotoPath);
        }

        // 加载账号头像（避免重复加载相同 URI）
        if (!string.IsNullOrEmpty(avatarUri) && headImageAvatarUri != null && playerIndex < headImageAvatarUri.Length)
        {
            string lastUri = null;
            _lastFaceAvatarUri.TryGetValue(playerIndex, out lastUri);

            if (avatarUri != lastUri)
            {
                Debug.Log($"[HeadImageController] 为槽位 {playerIndex} 加载账号头像: {avatarUri}");
                headImageAvatarUri[playerIndex].ShowImage(avatarUri);
                _lastFaceAvatarUri[playerIndex] = avatarUri;
            }
        }

        // 通知 parseDataDemo 识别成功
        if (parseDataDemo != null)
        {
            parseDataDemo.NotifyPlayerFaceRecognized(playerIndex);
        }

        // 如果正在等待选角结果且是选角成功的回调
        if (isRecognized && _isWaitingForSelectionResult)
        {
            Debug.Log($"[HeadImageController] 选角成功回调，槽位: {playerIndex}");
            _isWaitingForSelectionResult = false;
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_role_select_success", playerIndex));
            ResetSelectionState();
            UpdateSelectionVisuals();
        }
    }

    /// <summary>
    /// 玩家离开槽位 → 隐藏头像
    /// </summary>
    private void HandlePlayerClear(int slotIndex)
    {
        if (slotIndex < 0) return;

        _slotHasPlayer.Remove(slotIndex);
        _lastFaceAvatarUri.Remove(slotIndex);

        // 隐藏该槽位的头像
        if (headImagePlayers != null && slotIndex < headImagePlayers.Length && headImagePlayers[slotIndex] != null)
        {
            headImagePlayers[slotIndex].ClearImage();
        }
        if (headImageAvatarUri != null && slotIndex < headImageAvatarUri.Length && headImageAvatarUri[slotIndex] != null)
        {
            headImageAvatarUri[slotIndex].ClearImage();
        }

        Debug.Log($"[HeadImageController] 槽位 {slotIndex} 玩家离开，清空头像");

        // 如果当前选中的槽位没人了，尝试切换到其他有人的槽位
        if (_isSelectionActive && _currentSelectedIndex == slotIndex)
        {
            if (!TrySelectFirstActiveSlot())
            {
                ResetSelectionState();
            }
            UpdateSelectionVisuals();
        }
    }

    /// <summary>
    /// 玩家做了"不玩游戏"姿势 → 清空该槽位
    /// </summary>
    private void HandlePlayerNotGame(int slotIndex, int lockedPersonId, int dummy)
    {
        HandlePlayerClear(slotIndex);
    }

    /// <summary>
    /// 显示指定槽位的默认头像
    /// </summary>
    private void ShowDefaultHead(int slotIndex)
    {
        if (defaultHeadTexture == null) return;

        if (headImagePlayers != null && slotIndex < headImagePlayers.Length && headImagePlayers[slotIndex] != null)
        {
            var rawImage = headImagePlayers[slotIndex].GetComponent<UnityEngine.UI.RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = defaultHeadTexture;
                rawImage.color = Color.white;
            }
        }
        if (headImageAvatarUri != null && slotIndex < headImageAvatarUri.Length && headImageAvatarUri[slotIndex] != null)
        {
            var rawImage = headImageAvatarUri[slotIndex].GetComponent<UnityEngine.UI.RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = defaultHeadTexture;
                rawImage.color = Color.white;
            }
        }
    }

    #endregion

    #region 手动选角 - 公共方法（供 ModelSelectDemo 子导航调用）

    /// <summary>
    /// 进入手动选角模式
    /// </summary>
    public void EnterSelectionMode()
    {
        if (_isSelectionActive)
        {
            Debug.LogWarning("[HeadImageController] 已经处于选角模式中");
            return;
        }

        // 开启人脸识别
        if (AndroidServerInfoDemo.Instance != null)
        {
            AndroidServerInfoDemo.Instance.StartFaceRecognitionCurrentFrameInfo();
            Debug.Log("[HeadImageController] 开启人脸识别");
        }

        // 初始化槽位玩家标记
        RefreshSlotHasPlayer();

        // 尝试选中第一个有玩家的槽位
        if (!TrySelectFirstActiveSlot())
        {
            _currentSelectedIndex = 0;
            Debug.Log("[HeadImageController] 当前没有玩家槽位激活，默认选中槽位0");
        }

        _isSelectionActive = true;
        _isWaitingForSelectionResult = false;

        UpdateSelectionVisuals();
        Debug.Log($"[HeadImageController] 进入手动选角模式，当前选中槽位: {_currentSelectedIndex}");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_role_select_enter", _currentSelectedIndex));
    }

    /// <summary>
    /// 退出手动选角模式
    /// </summary>
    public void ExitSelectionMode()
    {
        if (!_isSelectionActive)
        {
            return;
        }

        // 停止人脸识别
        if (AndroidServerInfoDemo.Instance != null)
        {
            AndroidServerInfoDemo.Instance.StopFaceRecognitionCurrentFrameInfo();
            Debug.Log("[HeadImageController] 停止人脸识别");
        }

        ResetSelectionState();
        UpdateSelectionVisuals();
        Debug.Log("[HeadImageController] 退出手动选角模式");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_role_select_exit"));
    }

    /// <summary>
    /// 左移选中槽位
    /// </summary>
    public void MoveSelectionLeft()
    {
        if (!IsSelectingSlot || headImagePlayers == null || headImagePlayers.Length == 0)
        {
            return;
        }

        int activeCount = GetActiveSlotCount();
        if (activeCount <= 0) return;

        for (int i = 0; i < activeCount; i++)
        {
            _currentSelectedIndex--;
            if (_currentSelectedIndex < 0)
            {
                _currentSelectedIndex = activeCount - 1;
            }

            if (_slotHasPlayer.Contains(_currentSelectedIndex))
            {
                break;
            }
        }

        UpdateSelectionVisuals();
        Debug.Log($"[HeadImageController] 左移，当前选中槽位: {_currentSelectedIndex}");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_role_select_slot", _currentSelectedIndex));
    }

    /// <summary>
    /// 右移选中槽位
    /// </summary>
    public void MoveSelectionRight()
    {
        if (!IsSelectingSlot || headImagePlayers == null || headImagePlayers.Length == 0)
        {
            return;
        }

        int activeCount = GetActiveSlotCount();
        if (activeCount <= 0) return;

        for (int i = 0; i < activeCount; i++)
        {
            _currentSelectedIndex++;
            if (_currentSelectedIndex >= activeCount)
            {
                _currentSelectedIndex = 0;
            }

            if (_slotHasPlayer.Contains(_currentSelectedIndex))
            {
                break;
            }
        }

        UpdateSelectionVisuals();
        Debug.Log($"[HeadImageController] 右移，当前选中槽位: {_currentSelectedIndex}");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_role_select_slot", _currentSelectedIndex));
    }

    /// <summary>
    /// 确认当前槽位，发起手动选角
    /// </summary>
    public void ConfirmSelection()
    {
        if (!IsSelectingSlot)
        {
            Debug.LogWarning("[HeadImageController] 当前不在选槽位状态，无法确认");
            return;
        }

        if (_currentSelectedIndex < 0)
        {
            Debug.LogWarning("[HeadImageController] 当前没有选中任何槽位");
            return;
        }

        if (AndroidServerInfoDemo.Instance == null)
        {
            Debug.LogWarning("[HeadImageController] AndroidServerInfoDemo.Instance 为 null，无法发起选角");
            return;
        }

        // 进入等待状态
        _isWaitingForSelectionResult = true;
        UpdateSelectionVisuals();

        // 调用 SDK 选角
        AndroidServerInfoDemo.Instance.SelectRole(new long[] { });
        Debug.Log($"[HeadImageController] 对槽位 {_currentSelectedIndex} 发起手动选角");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_role_select_wait", _currentSelectedIndex));
    }

    /// <summary>
    /// 清空所有槽位的头像显示
    /// </summary>
    public void ClearAllImages()
    {
        _slotHasPlayer.Clear();
        _lastFaceAvatarUri.Clear();

        if (headImagePlayers != null)
        {
            for (int i = 0; i < headImagePlayers.Length; i++)
            {
                if (headImagePlayers[i] != null)
                {
                    headImagePlayers[i].ClearImage();
                }
            }
        }

        if (headImageAvatarUri != null)
        {
            for (int i = 0; i < headImageAvatarUri.Length; i++)
            {
                if (headImageAvatarUri[i] != null)
                {
                    headImageAvatarUri[i].ClearImage();
                }
            }
        }

        ResetSelectionState();
        UpdateSelectionVisuals();
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 获取当前激活的槽位数量
    /// </summary>
    private int GetActiveSlotCount()
    {
        if (parseDataDemo != null)
        {
            return Mathf.Min(parseDataDemo.CurPlayerCount, headImagePlayers != null ? headImagePlayers.Length : 0);
        }
        return headImagePlayers != null ? headImagePlayers.Length : 0;
    }

    /// <summary>
    /// 刷新哪些槽位当前有玩家
    /// </summary>
    private void RefreshSlotHasPlayer()
    {
        _slotHasPlayer.Clear();
        if (parseDataDemo == null) return;

        int count = GetActiveSlotCount();
        for (int i = 0; i < count; i++)
        {
            _slotHasPlayer.Add(i);
        }
    }

    /// <summary>
    /// 查找当前第一个有玩家的槽位并设为当前高亮项
    /// </summary>
    private bool TrySelectFirstActiveSlot()
    {
        int count = GetActiveSlotCount();
        for (int i = 0; i < count; i++)
        {
            if (_slotHasPlayer.Contains(i))
            {
                _currentSelectedIndex = i;
                return true;
            }
        }

        if (count > 0)
        {
            _currentSelectedIndex = 0;
            return true;
        }

        _currentSelectedIndex = -1;
        return false;
    }

    /// <summary>
    /// 完整退出本地手动选角 UI
    /// </summary>
    private void ResetSelectionState()
    {
        _isSelectionActive = false;
        _isWaitingForSelectionResult = false;
        _currentSelectedIndex = -1;
    }

    /// <summary>
    /// 刷新每个槽位的选中框显示
    /// </summary>
    private void UpdateSelectionVisuals()
    {
        if (selectionFrames == null) return;

        for (int i = 0; i < selectionFrames.Length; i++)
        {
            if (selectionFrames[i] != null)
            {
                bool isSelected = _isSelectionActive && i == _currentSelectedIndex;
                selectionFrames[i].SetActive(isSelected);
            }
        }
    }

    #endregion
}
