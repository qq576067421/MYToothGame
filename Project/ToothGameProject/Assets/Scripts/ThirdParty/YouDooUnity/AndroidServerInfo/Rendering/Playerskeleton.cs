/*
作者：Ting
创建时间：2025.10.07
描述：范例，显示人物的骨骼
*/
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Playerskeleton : MonoBehaviour
{
    [Tooltip("点的颜色")]
    [SerializeField]
    private Color colorPoint = Color.red;

    [Tooltip("线的颜色")]
    [SerializeField]
    private Color colorLine = Color.green;

    [Tooltip("点的父节点")]
    [SerializeField]
    private Transform[] _pointTransform;

    [Tooltip("线的父节点")]
    [SerializeField]
    private Transform[] _lineTransform;

    [Tooltip("信息文本")]
    [SerializeField]
    private Text _playerInfoText;

    [Tooltip("状态文本")]
    [SerializeField]
    private Text _playerStateText;

    [Header("分帧配置")]
    [SerializeField] private int pointsPerFrame = 5;
    [SerializeField] private int linesPerFrame = 3;

    private PlayerTextuerShow _playerTextuerShow;
    private Transform[] _childrenPointTransform;
    private Image[] _childrenLineImage;

    private Transform[] _childrenLeftHandPointTransform;
    private Image[] _childrenLeftHandLineImage;

    private Transform[] _childrenRightHandPointTransform;
    private Image[] _childrenRightHandLineImage;

    private const float RAD_TO_DEG = 180f / Mathf.PI;
    private const float DEGREES_TO_ADD = 270f;
    private readonly int LINE_COUNT = 16;
    private readonly int LINE_COUNT_HAND = 25;
    private Vector2 LINE_SIZE = new Vector2(2f, 0f);

    private bool _isInitializing = false;
    private bool _isInitialized = false;

    private CameraTextureView _brotherCameraTextureView;
    private RectTransform _brotherRectTransform;

    // 复用的 WorldCorners 数组，避免每帧 GC
    private readonly Vector3[] _worldCornersCache = new Vector3[4];

    /// <summary>
    /// 将骨骼所有点/线容器的世界坐标对齐到目标图片的实际中心，并记录对应的 CameraTextureView 用于 UV 映射。
    /// 需要在每次 DrawSkeleton 之前调用，保证 PartitionView 模式下骨骼与图片坐标系一致。
    /// 直接移动 _pointTransform / _lineTransform 到目标图片世界中心，
    /// 而非移动父节点 SkeletonParentX，从而避免层级偏移累积。
    /// </summary>
    public void SyncToTargetView(CameraTextureView targetView, RectTransform targetRect)
    {
        _brotherCameraTextureView = targetView;
        _brotherRectTransform = targetRect;

        if (targetRect == null) return;

        // 用 GetWorldCorners 获取真实屏幕中心（不受 pivot/anchor 影响）
        targetRect.GetWorldCorners(_worldCornersCache);
        Vector3 viewWorldCenter = (_worldCornersCache[0] + _worldCornersCache[2]) * 0.5f;

        // 将所有 _pointTransform 和 _lineTransform 直接移动到目标图片的世界中心。
        // 这样 NormalizedToScreenPosition 计算出的 localPosition 偏移，
        // 将以目标图片中心为基准，骨骼点就能正确叠加在图片上。
        if (_pointTransform != null)
        {
            for (int i = 0; i < _pointTransform.Length; i++)
            {
                if (_pointTransform[i] != null)
                    _pointTransform[i].position = viewWorldCenter;
            }
        }

        if (_lineTransform != null)
        {
            for (int i = 0; i < _lineTransform.Length; i++)
            {
                if (_lineTransform[i] != null)
                    _lineTransform[i].position = viewWorldCenter;
            }
        }
    }

    public void InitPlayerTextureItem(GameObject objPoint, GameObject objLine, PlayerTextuerShow playerTextuerShow)
    {
        _playerTextuerShow = playerTextuerShow;

        if (_isInitializing || _isInitialized) return;
        StartCoroutine(InitializeRoutine(objPoint, objLine));
    }

    private IEnumerator InitializeRoutine(GameObject objPoint, GameObject objLine)
    {
        _isInitializing = true;

        // 1. 初始化身体关键点（分帧）
        int bodyPointCount = (int)YouDooSDKConstants.KeyPointIndex.KEYPOINT_COUNT;
        _childrenPointTransform = new Transform[bodyPointCount];
        yield return StartCoroutine(CreatePointsRoutine(
            objPoint, bodyPointCount,
            _pointTransform[(int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON],
            _childrenPointTransform, colorPoint, "BodyPoint"));

        // 2. 初始化身体连接线（分帧）
        _childrenLineImage = new Image[LINE_COUNT];
        yield return StartCoroutine(CreateLinesRoutine(
            objLine, LINE_COUNT,
            _lineTransform[(int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON],
            _childrenLineImage, colorLine, LINE_SIZE, "BodyLine"));

        // 3. 初始化左手关键点（分帧）
        int handPointCount = (int)YouDooSDKConstants.HandLandmark21.HAND_LANDMARK_COUNT;
        _childrenLeftHandPointTransform = new Transform[handPointCount];
        yield return StartCoroutine(CreatePointsRoutine(
            objPoint, handPointCount,
            _pointTransform[(int)YouDooSDKConstants.DetectType.DETECT_TYPE_LEFT_HAND],
            _childrenLeftHandPointTransform, colorPoint, "LeftHandPoint"));

        // 4. 初始化左手连接线（分帧）
        _childrenLeftHandLineImage = new Image[LINE_COUNT_HAND];
        yield return StartCoroutine(CreateLinesRoutine(
            objLine, LINE_COUNT_HAND,
            _lineTransform[(int)YouDooSDKConstants.DetectType.DETECT_TYPE_LEFT_HAND],
            _childrenLeftHandLineImage, colorLine, LINE_SIZE, "LeftHandLine"));

        // 5. 初始化右手关键点（分帧）
        _childrenRightHandPointTransform = new Transform[handPointCount];
        yield return StartCoroutine(CreatePointsRoutine(
            objPoint, handPointCount,
            _pointTransform[(int)YouDooSDKConstants.DetectType.DETECT_TYPE_RIGHT_HAND],
            _childrenRightHandPointTransform, colorPoint, "RightHandPoint"));

        // 6. 初始化右手连接线（分帧）
        _childrenRightHandLineImage = new Image[LINE_COUNT_HAND];
        yield return StartCoroutine(CreateLinesRoutine(
            objLine, LINE_COUNT_HAND,
            _lineTransform[(int)YouDooSDKConstants.DetectType.DETECT_TYPE_RIGHT_HAND],
            _childrenRightHandLineImage, colorLine, LINE_SIZE, "RightHandLine"));

        SetSkeletonActive(false);

        _isInitializing = false;
        _isInitialized = true;
        // Debug.Log("分帧初始化骨骼完成");
    }

    private IEnumerator CreatePointsRoutine(GameObject prefab, int count, Transform parent,
         Transform[] array, Color color, string baseName)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject newObj = Instantiate(prefab, parent);
            newObj.name = $"{baseName}_{i}";
            if (newObj.TryGetComponent<Image>(out var rend))
                rend.color = color;
            array[i] = newObj.transform;
            newObj.SetActive(true);
            if (i > 0 && i % pointsPerFrame == 0)
                yield return null;
        }
    }

    private IEnumerator CreateLinesRoutine(GameObject prefab, int count, Transform parent,
        Image[] array, Color color, Vector2 lineSize, string baseName)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject newObj = Instantiate(prefab, parent);
            newObj.name = $"{baseName}_{i}";
            if (newObj.TryGetComponent<Image>(out var rend))
            {
                rend.color = color;
                rend.rectTransform.sizeDelta = lineSize;
                array[i] = rend;
            }
            newObj.SetActive(true);
            if (i > 0 && i % linesPerFrame == 0)
                yield return null;
        }
    }

    public void SetSkeletonActive(bool isShowing)
    {
        float alpha = isShowing ? 1 : 0;
        if (gameObject.GetComponent<CanvasGroup>().alpha == alpha) return;
        gameObject.GetComponent<CanvasGroup>().alpha = alpha;
    }

    private void UpdateLines()
    {
        if (_childrenLineImage == null) return;

        var lineImages = _childrenLineImage;
        var pointTransforms = _childrenPointTransform;
        if (pointTransforms == null) return;

        int connectionsCount = YouDooSDKConstants.defaultSkeletonConnections.Length;
        for (int i = 0; i < connectionsCount; i++)
        {
            if (i >= lineImages.Length) break;

            var (from, to) = YouDooSDKConstants.defaultSkeletonConnections[i];
            if (from < 0 || from >= pointTransforms.Length || to < 0 || to >= pointTransforms.Length) continue;
            if (pointTransforms[from] == null || pointTransforms[to] == null) continue;
            if (lineImages[i] == null) continue;

            Vector3 targetPos = pointTransforms[to].localPosition;
            Vector3 startPos = pointTransforms[from].localPosition;
            LINE_SIZE.y = Vector3.Distance(targetPos, startPos);

            Image lineImage = lineImages[i];
            lineImage.rectTransform.sizeDelta = LINE_SIZE;
            lineImage.transform.localPosition = startPos;

            double angle = Math.Atan2(targetPos.y - startPos.y, targetPos.x - startPos.x) * RAD_TO_DEG;
            lineImage.transform.rotation = Quaternion.Euler(0, 0, (float)angle + DEGREES_TO_ADD);
        }
    }

    public void UpdatePointPos(float[,] kptList, int playerId, float score)
    {
        if (_childrenPointTransform == null) return;
        if (kptList == null)
        {
            Debug.LogWarning("传入骨骼点的数据 kptList == null 直接返回了");
            return;
        }

        Rect? customUvRect = null;
        Vector2? customActualSize = null;
        if (_brotherCameraTextureView != null && _brotherRectTransform != null)
        {
            customUvRect = _brotherCameraTextureView.GetCurrentUVRect();
            customActualSize = _brotherRectTransform.rect.size;
        }

        int count = kptList.GetLength(0);
        for (int kpIdx = 0; kpIdx < count; kpIdx++)
        {
            _playerTextuerShow.NormalizedToScreenPosition(
                kptList[kpIdx, 0], kptList[kpIdx, 1],
                _childrenPointTransform[kpIdx],
                customUvRect, customActualSize);
        }
        UpdateLines();
    }

    public void UpdatePointPos(float[,] kptList, YouDooSDKConstants.DetectType DetectType)
    {
        if (_childrenLeftHandPointTransform == null || _childrenLeftHandLineImage == null ||
           _childrenRightHandPointTransform == null || _childrenRightHandLineImage == null)
            return;

        Transform[] childrenPointTransform = null;
        Image[] childrenLineImage = null;
        switch (DetectType)
        {
            case YouDooSDKConstants.DetectType.DETECT_TYPE_LEFT_HAND:
                childrenPointTransform = _childrenLeftHandPointTransform;
                childrenLineImage = _childrenLeftHandLineImage;
                break;
            case YouDooSDKConstants.DetectType.DETECT_TYPE_RIGHT_HAND:
                childrenPointTransform = _childrenRightHandPointTransform;
                childrenLineImage = _childrenRightHandLineImage;
                break;
        }

        if (kptList == null || childrenPointTransform == null || childrenLineImage == null)
        {
            _pointTransform[(int)DetectType].gameObject.SetActive(false);
            _lineTransform[(int)DetectType].gameObject.SetActive(false);
            return;
        }

        if (!_pointTransform[(int)DetectType].gameObject.activeSelf)
            _pointTransform[(int)DetectType].gameObject.SetActive(true);
        if (!_lineTransform[(int)DetectType].gameObject.activeSelf)
            _lineTransform[(int)DetectType].gameObject.SetActive(true);

        Rect? customUvRect = null;
        Vector2? customActualSize = null;
        if (_brotherCameraTextureView != null && _brotherRectTransform != null)
        {
            customUvRect = _brotherCameraTextureView.GetCurrentUVRect();
            customActualSize = _brotherRectTransform.rect.size;
        }

        int count = kptList.GetLength(0);
        for (int kpIdx = 0; kpIdx < count; kpIdx++)
        {
            _playerTextuerShow.NormalizedToScreenPosition(
                kptList[kpIdx, 0], kptList[kpIdx, 1],
                childrenPointTransform[kpIdx],
                customUvRect, customActualSize);
        }
        UpdateHandLines(childrenPointTransform, childrenLineImage);
    }

    private void UpdateHandLines(Transform[] childrenPointTransform, Image[] childrenLineImage)
    {
        if (childrenLineImage == null || childrenPointTransform == null) return;

        var lineImages = childrenLineImage;
        var pointTransforms = childrenPointTransform;
        int connectionsCount = YouDooSDKConstants.defaultHandLandmark21Connections.Length;
        for (int i = 0; i < connectionsCount; i++)
        {
            if (i >= lineImages.Length) break;

            var (from, to) = YouDooSDKConstants.defaultHandLandmark21Connections[i];
            if (from < 0 || from >= pointTransforms.Length || to < 0 || to >= pointTransforms.Length) continue;
            if (pointTransforms[from] == null || pointTransforms[to] == null) continue;
            if (lineImages[i] == null) continue;

            Vector3 targetPos = pointTransforms[to].localPosition;
            Vector3 startPos = pointTransforms[from].localPosition;
            LINE_SIZE.y = Vector3.Distance(targetPos, startPos);

            Image lineImage = lineImages[i];
            lineImage.rectTransform.sizeDelta = LINE_SIZE;
            lineImage.transform.localPosition = startPos;

            double angle = Math.Atan2(targetPos.y - startPos.y, targetPos.x - startPos.x) * RAD_TO_DEG;
            lineImage.transform.rotation = Quaternion.Euler(0, 0, (float)angle + DEGREES_TO_ADD);
        }
    }

    public void SetPlayerState(string state)
    {
        _playerStateText.color = colorLine;
        _playerStateText.text = state;
        _playerStateText.GetComponent<Transform>().localPosition =
            _childrenPointTransform[(int)YouDooSDKConstants.KeyPointIndex.Lefthip].localPosition;
    }

    /// <summary>
    /// 绘制矩形轮廓（用前4个骨骼点）
    /// </summary>
    public void DrawRect(float left, float top, float right, float bottom)
    {
        for (int i = 0; i < 4; i++)
        {
            if (i < _childrenPointTransform.Length)
                _childrenPointTransform[i].gameObject.SetActive(true);
        }
        UpdateRectLines();
    }

    private void UpdateRectLines()
    {
        if (_childrenLineImage == null) return;

        var rectConnections = new (int from, int to)[]
        {
            (0, 1), // 上边
            (1, 2), // 右边
            (2, 3), // 下边
            (3, 0)  // 左边
        };

        for (int i = 0; i < rectConnections.Length && i < _childrenLineImage.Length; i++)
        {
            var connection = rectConnections[i];
            if (connection.from < _childrenPointTransform.Length && connection.to < _childrenPointTransform.Length)
            {
                Vector3 targetPos = _childrenPointTransform[connection.to].localPosition;
                Vector3 startPos = _childrenPointTransform[connection.from].localPosition;
                LINE_SIZE.y = Vector3.Distance(targetPos, startPos);

                Image lineImage = _childrenLineImage[i];
                lineImage.rectTransform.sizeDelta = LINE_SIZE;
                lineImage.transform.localPosition = startPos;

                double angle = Math.Atan2(targetPos.y - startPos.y, targetPos.x - startPos.x) * RAD_TO_DEG;
                lineImage.transform.rotation = Quaternion.Euler(0, 0, (float)angle + DEGREES_TO_ADD);
                lineImage.gameObject.SetActive(true);
            }
        }
    }
}
