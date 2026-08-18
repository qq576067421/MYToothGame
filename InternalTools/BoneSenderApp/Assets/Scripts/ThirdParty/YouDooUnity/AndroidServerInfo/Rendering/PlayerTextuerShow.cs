/*
作者：Ting
创建时间：2025.10.07
描述：在主线程中调用纹理更新 - 优化版本
*/
using System;
using UnityEngine;
using UnityEngine.UI;
using static YouDooSDKConstants;
public class PlayerTextuerShow : MonoBehaviour
{
    private const string LogTag = "[PlayerTextuerShow]";

    public Playerskeleton[] Playerskeletons;
    public GameObject rawPointPrefab;
    public GameObject rawLinePrefab;
    private RectTransform _cameraImageRectTransform;
    private bool _isRectTransformInitialized = false;

    [Header("调试设置")]
    public bool enableDebugLog = true;

    private Rect _cameraTextureViewRect;

    private Rect[] _cameraTextureViewRectPartitionView;

    [Header("区域分割线设置")]
    public GameObject regionLinePrefab; // 竖线预制体
    public float regionLineWidth = 2f; // 竖线宽度
    private GameObject[] _regionLines;
    private float[,] _regionRects; // 存储区域矩形数据

    public event Action<HandsCursorData> ShowHandsCursorDataAction;
    private bool _hasLoggedShowCameraImage;

    private void Awake()
    {
        bool shouldUseSdkRuntime = Application.platform == RuntimePlatform.Android;
        if (shouldUseSdkRuntime)
        {
            gameObject.SetActive(true);
            Debug.Log($"{LogTag} Awake platform={Application.platform} shouldUseSdkRuntime={shouldUseSdkRuntime} activeSelf={gameObject.activeSelf}");
            return;
        }

        AndroidServerInfo serverInfo = AndroidServerInfo.Instance;
        gameObject.SetActive(serverInfo != null && serverInfo.IsSDKMode);
        Debug.Log($"{LogTag} Awake platform={Application.platform} shouldUseSdkRuntime={shouldUseSdkRuntime} hasServerInfo={serverInfo != null} isSDKMode={(serverInfo != null && serverInfo.IsSDKMode)} activeSelf={gameObject.activeSelf}");
    }

    // 10种不同的颜色
    private Color[] _regionLineColors = new Color[]
    {
        Color.red,           // 红色 - 第1条线
        Color.green,         // 绿色 - 第2条线
        Color.blue,          // 蓝色 - 第3条线
        Color.yellow,        // 黄色 - 第4条线
        Color.magenta,       // 洋红色 - 第5条线
        Color.cyan,          // 青色 - 第6条线
        new Color(1f, 0.5f, 0f), // 橙色 - 第7条线
        new Color(0.5f, 0f, 1f), // 紫色 - 第8条线
        new Color(0f, 1f, 0.5f), // 青绿色 - 第9条线
        new Color(1f, 0f, 0.5f)  // 粉红色 - 第10条线
    };

    public void InitGameInfo(float[,] personPlayerReadyRectf, int playerNumber)
    {
        Debug.Log($"{LogTag} InitGameInfo playerNumber={playerNumber} skeletonCount={(Playerskeletons != null ? Playerskeletons.Length : 0)}");
        _hasLoggedShowCameraImage = false;
        _cameraTextureViewRectPartitionView ??= new Rect[4];
        for (int i = 0; i < Playerskeletons.Length; i++)
        {
            if (Playerskeletons[i] == null) continue;
            Playerskeletons[i].InitPlayerTextureItem(rawPointPrefab, rawLinePrefab, this);
        }

        _regionRects = personPlayerReadyRectf;
        InitializeRegionLines();
    }

    void Start()
    {
        if (!_isRectTransformInitialized)
        {
            _cameraImageRectTransform = GetComponent<RectTransform>();
            _isRectTransformInitialized = _cameraImageRectTransform != null;
        }
        if (enableDebugLog) Debug.Log("PlayerTextuerShow 初始化完成");
    }

    private void InitializeRegionLines()
    {
        if (regionLinePrefab == null)
        {
            Debug.LogError("区域分割线预制体未设置！请检查Inspector中的regionLinePrefab字段");
            return;
        }

        ClearRegionLines();

        if (_regionRects == null || _regionRects.Length == 0)
        {
            _regionRects = new float[,]
            {
                { 0f, 0f, 0.25f, 1f },
                { 0.25f, 0f, 0.5f, 1f },
                { 0.5f, 0f, 0.75f, 1f },
                { 0.75f, 0f, 1.0f, 1f }
            };
        }
        int regionCount = _regionRects.Length / 4;
        int lineCount = Math.Max(0, regionCount - 1);
        _regionLines = new GameObject[lineCount];

        for (int i = 0; i < lineCount; i++)
        {
            _regionLines[i] = Instantiate(regionLinePrefab, transform);
            _regionLines[i].name = $"RegionLine_{i}";
            _regionLines[i].SetActive(true);
            SetupRegionLine(_regionLines[i], i);
        }
        UpdateRegionLines();
    }

    private void SetupRegionLine(GameObject lineObject, int lineIndex)
    {
        RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = lineObject.AddComponent<RectTransform>();
        }

        Image image = lineObject.GetComponent<Image>();
        if (image == null)
        {
            image = lineObject.AddComponent<Image>();
        }

        Color lineColor = _regionLineColors[lineIndex % _regionLineColors.Length];
        image.color = lineColor;

        AddLineNumberText(lineObject, lineIndex, lineColor);
    }

    private void AddLineNumberText(GameObject lineObject, int lineIndex, Color lineColor)
    {
        GameObject textObject = new GameObject($"LineNumber_{lineIndex}");
        textObject.transform.SetParent(lineObject.transform);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        Text textComponent = textObject.AddComponent<Text>();

        textComponent.text = $"区域{lineIndex}";
        textComponent.color = lineColor;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 12;
        textComponent.alignment = TextAnchor.UpperCenter;
        textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(-50f, 5f);
        textRect.sizeDelta = new Vector2(80f, 20f);
    }

    private void UpdateRegionLines()
    {
        if (_regionLines == null) return;

        if (_cameraImageRectTransform == null)
        {
            _cameraImageRectTransform = GetComponent<RectTransform>();
            _isRectTransformInitialized = _cameraImageRectTransform != null;
            if (_cameraImageRectTransform == null) return;
        }

        if (_regionRects == null) return;

        int regionCount = _regionRects.Length / 4;
        int lineCount = _regionLines.Length;

        for (int i = 0; i < lineCount; i++)
        {
            if (i >= regionCount - 1) continue;

            float normalizedX = (float)(i + 1) / regionCount;

            GameObject lineObject = _regionLines[i];
            if (lineObject == null) continue;

            if (!lineObject.TryGetComponent<RectTransform>(out var lineRect)) continue;

            lineRect.anchorMin = new Vector2(normalizedX, 0f);
            lineRect.anchorMax = new Vector2(normalizedX, 1.0f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.offsetMin = new Vector2(-regionLineWidth * 0.5f, 0f);
            lineRect.offsetMax = new Vector2(regionLineWidth * 0.5f, 0f);
            lineObject.SetActive(true);
        }
    }

    private void ClearRegionLines()
    {
        if (_regionLines != null)
        {
            foreach (GameObject line in _regionLines)
            {
                if (line != null)
                {
                    Destroy(line);
                }
            }
            _regionLines = null;
        }
    }

    public void SetRegionLinesVisible(bool visible)
    {
        if (_regionLines != null)
        {
            foreach (GameObject line in _regionLines)
            {
                if (line != null)
                {
                    line.SetActive(visible);
                }
            }
        }
    }

    public void SetCameraTextureViewBgRect(Rect cameraTextureViewBgRect)
    {
        _cameraTextureViewRect = cameraTextureViewBgRect;
    }

    private bool IsValidRect(Rect r)
    {
        return r.width > 0.001f && r.height > 0.001f;
    }

    /// <summary>
    /// 显示相机图像（在Update中调用）
    /// </summary>
    public void ShowCameraImage()
    {
        float[,] personPlayerRectf = PlayerMatchView.Instance.PersonPlayerDrawRectf;
        if (PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
        {
            int rowCount = personPlayerRectf.GetLength(0);
            for (int i = 0; i < 4; i++) // 最多处理4个人物
            {
                if (i < rowCount)
                {
                    // 有数据，尝试显示
                    float left = personPlayerRectf[i, 0];
                    float top = personPlayerRectf[i, 1];
                    float right = personPlayerRectf[i, 2];
                    float bottom = personPlayerRectf[i, 3];
                    // 检查数据是否有效
                    if (IsValidPersonRect(left, top, right, bottom))
                    {
                        try
                        {
                            Rect targetRect = new Rect(left, top, right - left, bottom - top);
                            _cameraTextureViewRectPartitionView[i] = targetRect;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"更新人物 {i} 纹理时出错: {e.Message}");
                        }
                    }
                    else
                    {
                        _cameraTextureViewRectPartitionView[i] = new Rect(0, 0, 0, 0);
                    }
                }
                else
                {
                    // 没有数据，设置为全0或隐藏
                    _cameraTextureViewRectPartitionView[i] = new Rect(0, 0, 0, 0);
                }
            }
        }
        else
        {
            //_cameraTextureViewRectPartitionView[0] = _cameraTextureViewRect;
            _cameraTextureViewRectPartitionView[0] = IsValidRect(_cameraTextureViewRect) ? _cameraTextureViewRect : new Rect(0f, 0f, 1f, 1f);
        }

        var bridge = gameObject.GetComponent<AndroidTextureBridgeBase>();
        if (bridge != null)
        {
            if (bridge.IsTextureReady() && _cameraTextureViewRectPartitionView != null && _cameraTextureViewRectPartitionView.Length > 0)
            {
                bridge.SetCameraViewRects(_cameraTextureViewRectPartitionView);
            }
        }
    }

    public void SetSkeletsonHide()
    {
        for (int i = 0; i < Playerskeletons.Length; i++)
        {
            if (Playerskeletons[i] == null) continue;
            Playerskeletons[i].SetSkeletonActive(false);
        }
    }

    public void DrawSkeleton(int personIndex, float[,] kptPoseList, float[,] kptLHandList, float[,] kptRHandList, float left, float top, float right, float bottom, int playerId, float score)
    {
        if (Playerskeletons.Length > personIndex)
        {
            if (Playerskeletons[personIndex] == null) return;
            // 根据当前显示模式，找到与该玩家对应的 CameraTextureView，并将骨骼容器的世界坐标对齐到该图片。
            // PartitionView 模式下：cameraViews[0] 是隐藏的全屏主图，cameraViews[1..N] 分别对应玩家0..N-1。
            // 其他模式下：所有玩家共用 cameraViews[0]。
            var bridge = GetComponent<AndroidTextureBridgeBase>();
            if (bridge != null)
            {
                bool isPartitionView = PlayerMatchView.Instance != null &&
                                       PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView;
                int viewIndex;
                if (isPartitionView)
                {
                    // PartitionView 模式下：
                    // Playerskeletons 最后 4 个节点（索引 Length-4 到 Length-1）对应 cameraViews[1..4]。
                    // 根据传入的 personIndex 倒推出它是第几个分割区域（slotIndex），再加 1 得到 cameraViews 索引。
                    int offset = Mathf.Min(4, Playerskeletons.Length);
                    int baseSkeletonIndex = Playerskeletons.Length - offset;
                    int slotIndex = personIndex - baseSkeletonIndex;
                    viewIndex = slotIndex + 1;
                }
                else
                {
                    viewIndex = 0;
                }
                CameraTextureView targetView = bridge.GetCameraView(viewIndex);
                if (targetView != null)
                {
                    RectTransform targetRect = targetView.GetComponent<RectTransform>();
                    Playerskeletons[personIndex].SyncToTargetView(targetView, targetRect);
                }
            }

            Playerskeletons[personIndex].SetSkeletonActive(true);
            Playerskeletons[personIndex].UpdatePointPos(kptPoseList, playerId, score);
            Playerskeletons[personIndex].DrawRect(left, top, right, bottom);

            Playerskeletons[personIndex].UpdatePointPos(kptLHandList, DetectType.DETECT_TYPE_LEFT_HAND);
            Playerskeletons[personIndex].UpdatePointPos(kptRHandList, DetectType.DETECT_TYPE_RIGHT_HAND);
        }
    }

    public void SetPlayerState(int personIndex, string state)
    {
        if (Playerskeletons.Length > personIndex)
        {
            Playerskeletons[personIndex].SetPlayerState(state);
        }
    }

    /// <summary>
    /// 检查人物矩形数据是否有效
    /// </summary>
    private bool IsValidPersonRect(float left, float top, float right, float bottom)
    {
        if (left == 0f && top == 0f && right == 0f && bottom == 0f) return false;

        if (left < 0f || left > 1f || top < 0f || top > 1f ||
            right < 0f || right > 1f || bottom < 0f || bottom > 1f)
        {
            Debug.LogWarning($"无效的坐标范围: ({left:F3}, {top:F3}, {right:F3}, {bottom:F3})");
            return false;
        }

        if (right <= left || bottom <= top)
        {
            Debug.LogWarning($"无效的矩形: 右({right:F3})<=左({left:F3}) 或 下({bottom:F3})<=上({top:F3})");
            return false;
        }

        float width = right - left;
        float height = bottom - top;
        if (width < 0.01f || height < 0.01f)
        {
            Debug.LogWarning($"矩形尺寸过小: {width:F3}x{height:F3}");
            return false;
        }
        return true;
    }

    public string GetTextureSystemStatus()
    {
        return "CameraImagViewBetter 未设置";
    }

    private Vector3 tempVec3 = new Vector3(0, 0, 0);

    public void NormalizedToScreenPosition(float x, float y, Transform targetTransform, Rect? customUvRect = null, Vector2? customActualSize = null)
    {
        if (targetTransform == null)
        {
            return;
        }

        Rect uvRect;
        Vector2 actualSize;

        if (customUvRect.HasValue && customActualSize.HasValue)
        {
            uvRect = customUvRect.Value;
            actualSize = customActualSize.Value;
        }
        else
        {
            if (!_isRectTransformInitialized || _cameraImageRectTransform == null)
            {
                _cameraImageRectTransform = GetComponent<RectTransform>();
                _isRectTransformInitialized = _cameraImageRectTransform != null;

                if (!_isRectTransformInitialized || _cameraImageRectTransform == null)
                {
                    return;
                }
            }

            uvRect = new Rect(0, 0, 1, 1);

            if (TryGetComponent<AndroidTextureBridgeBase>(out var bridge))
            {
                CameraTextureView mainView = bridge.GetCameraView(0);
                if (mainView != null)
                {
                    uvRect = mainView.GetCurrentUVRect();
                }
            }

            actualSize = _cameraImageRectTransform.rect.size;
        }

        float localU = 0.5f;
        float localV = 0.5f;

        if (uvRect.width > 0.00001f)
        {
            localU = (x - uvRect.x) / uvRect.width;
        }
        if (uvRect.height > 0.00001f)
        {
            localV = (y - uvRect.y) / uvRect.height;
        }

        float visibleX = localU;
        float visibleY = localV;

        float localX = (visibleX - 0.5f) * actualSize.x;
        float localY = (0.5f - visibleY) * actualSize.y;

        localY *= -1f;
        tempVec3.x = localX;
        tempVec3.y = localY;
        targetTransform.localPosition = tempVec3;
    }

    ///
    /// 绘制点
    /// 
    public void ShowHandsCursorData(HandsCursorData handsCursorData)
    {
        ShowHandsCursorDataAction?.Invoke(handsCursorData);
    }

    void OnDestroy()
    {
        if (enableDebugLog) Debug.Log("PlayerTextuerShow 销毁");
        ClearRegionLines();
    }
}
