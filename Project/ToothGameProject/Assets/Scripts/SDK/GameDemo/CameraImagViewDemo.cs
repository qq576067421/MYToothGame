using System;
using UnityEngine;
using UnityEngine.UI;

public class CameraImagViewDemo : CameraImagView
{
    [Header("区域分割线设置")]
    public GameObject regionLinePrefab; // 竖线预制体
    public float regionLineWidth = 2f; // 竖线宽度
    private RectTransform _rectTransform;
    private GameObject[] _regionLines;
    private float[,] _regionRects; // 存储区域矩形数据

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

    // 显示的是
    [Header("人员分割显示")]
    public CameraTextureView cameraImagViewForPerson0;
    public CameraTextureView cameraImagViewForPerson1;
    public CameraTextureView cameraImagViewForPerson2;
    public CameraTextureView cameraImagViewForPerson3;

    private int _curPlayerCount = 0;
    /// <summary>
    /// 设置区域矩形并更新分割线
    /// </summary>
    public void SetRegionRects(float[,] regionRects, int playerNumber)
    {
        _regionRects = regionRects;
        _curPlayerCount = playerNumber;
        InitializeRegionLines();
        UpdateRegionLines();
    }

    /// <summary>
    /// 初始化区域分割线
    /// </summary>
    private void InitializeRegionLines()
    {
        if (regionLinePrefab == null)
        {
            Debug.LogError("区域分割线预制体未设置！请检查Inspector中的regionLinePrefab字段");
            return;
        }

        // 清理现有的分割线
        ClearRegionLines();

        // 如果没有区域数据，使用默认的4个区域（与您的设置匹配）
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
        int lineCount = Math.Max(0, regionCount - 1); // 分割线数量 = 区域数量 - 1
        _regionLines = new GameObject[lineCount];

        for (int i = 0; i < lineCount; i++)
        {
            _regionLines[i] = Instantiate(regionLinePrefab, transform);
            _regionLines[i].name = $"RegionLine_{i}";
            _regionLines[i].SetActive(true);
            // 设置竖线样式，每条线使用不同的颜色
            SetupRegionLine(_regionLines[i], i);
        }
        // 立即更新位置
        UpdateRegionLines();
        if (PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
        {
            // cmeraImagView.SafeHandlePersonTextures(personPlayerRectf);
            cameraTextureView.gameObject.SetActive(false);
            for (int i = 0; i < 4; i++)
            {
                GetPersonDisplayByIndex(i).gameObject.SetActive(false);
            }
            for (int i = 0; i < _curPlayerCount; i++)
            {
                GetPersonDisplayByIndex(i).Clear();
                GetPersonDisplayByIndex(i).gameObject.SetActive(true);
            }
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                GetPersonDisplayByIndex(i).Clear();
                GetPersonDisplayByIndex(i).gameObject.SetActive(false);
            }
            cameraTextureView.Clear();
            cameraTextureView.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 设置单个分割线的样式
    /// </summary>
    /// <param name="lineObject">线条对象</param>
    /// <param name="lineIndex">线条索引（用于选择颜色）</param>
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

        // 根据线条索引选择颜色，如果超过10条线则循环使用颜色
        Color lineColor = _regionLineColors[lineIndex % _regionLineColors.Length];
        image.color = lineColor;

        // 添加文本显示线条编号（可选）
        AddLineNumberText(lineObject, lineIndex, lineColor);
    }

    /// <summary>
    /// 为分割线添加编号文本（可选功能）
    /// </summary>
    private void AddLineNumberText(GameObject lineObject, int lineIndex, Color lineColor)
    {
        // 创建文本对象
        GameObject textObject = new GameObject($"LineNumber_{lineIndex}");
        textObject.transform.SetParent(lineObject.transform);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        Text textComponent = textObject.AddComponent<Text>();

        // 设置文本样式
        textComponent.text = RenderAPI.GetTextByLanId("sdk_demo_region_index", lineIndex);
        textComponent.color = lineColor;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 12;
        textComponent.alignment = TextAnchor.UpperCenter;
        textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        // 设置文本位置（在线条上方）
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(-50f, 5f);
        textRect.sizeDelta = new Vector2(80f, 20f);
    }

    /// <summary>
    /// 更新区域分割线位置
    /// </summary>
    private void UpdateRegionLines()
    {
        if (_regionLines == null)
        {
            Debug.LogError("分割线数组为null");
            return;
        }

        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                Debug.LogError("RectTransform为null");
                return;
            }
        }

        if (_regionRects == null)
        {
            Debug.LogError("区域数据为null");
            return;
        }

        int regionCount = _regionRects.Length / 4;
        int lineCount = _regionLines.Length;

        for (int i = 0; i < lineCount; i++)
        {
            if (i >= regionCount - 1)
            {
                Debug.LogWarning($"分割线索引{i}超出区域范围");
                continue;
            }

            // 无论区域数据如何，强制分割线平分屏幕
            float normalizedX = (float)(i + 1) / regionCount;

            GameObject lineObject = _regionLines[i];
            if (lineObject == null)
            {
                Debug.LogError($"分割线对象 {i} 为null");
                continue;
            }

            RectTransform lineRect = lineObject.GetComponent<RectTransform>();
            if (lineRect == null)
            {
                Debug.LogError($"分割线 {i} 的RectTransform为null");
                continue;
            }

            // 使用锚点精确定位 - 这是关键！
            lineRect.anchorMin = new Vector2(normalizedX, 0f);
            lineRect.anchorMax = new Vector2(normalizedX, 1.0f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.offsetMin = new Vector2(-regionLineWidth * 0.5f, 0f); // 左边偏移
            lineRect.offsetMax = new Vector2(regionLineWidth * 0.5f, 0f);  // 右边偏移
                                                                           // 确保对象激活
            lineObject.SetActive(true);
        }
    }

    /// <summary>
    /// 清理区域分割线
    /// </summary>
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

    /// <summary>
    /// 显示/隐藏区域分割线
    /// </summary>
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

    /// <summary>
    /// 获取分割线颜色数组（用于外部访问）
    /// </summary>
    public Color[] GetRegionLineColors()
    {
        return _regionLineColors;
    }

    /// <summary>
    /// 获取指定索引的分割线颜色
    /// </summary>
    public Color GetRegionLineColor(int index)
    {
        if (index >= 0 && index < _regionLineColors.Length)
        {
            return _regionLineColors[index];
        }
        return Color.white; // 默认返回白色
    }


    /// <summary>
    /// 安全处理人员纹理显示
    /// </summary>
    public void SafeHandlePersonTextures(float[,] personPlayerRectf)
    {
        if (personPlayerRectf == null)
        {
            ClearAllPersonDisplays();
            return;
        }

        // 正确打印二维数组的维度
        int rowCount = personPlayerRectf.GetLength(0);
        int colCount = personPlayerRectf.GetLength(1);

        // 确保列数足够
        if (colCount < 4)
        {
            Debug.LogError($"人员数据列数不足: {colCount}，需要4列");
            ClearAllPersonDisplays();
            return;
        }

        // 安全地处理每个人物
        for (int i = 0; i < 4; i++) // 最多处理4个人物
        {
            CameraTextureView personDisplay = GetPersonDisplayByIndex(i);
            if (personDisplay == null)
            {
                continue;
            }

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
                        // 使用主纹理显示人物
                        personDisplay.ChangeImageRect(targetRect);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"更新人物 {i} 纹理时出错: {e.Message}");
                        personDisplay.Clear();
                    }
                }
                else
                {
                    personDisplay.Clear();
                }
            }
            else
            {
                // 没有数据，设置空纹理
                personDisplay.Clear();
            }
        }
    }

    /// <summary>
    /// 检查人物矩形数据是否有效
    /// </summary>
    private bool IsValidPersonRect(float left, float top, float right, float bottom)
    {
        // 检查是否为全0（表示无数据）
        if (left == 0f && top == 0f && right == 0f && bottom == 0f)
        {
            return false;
        }

        // 检查坐标有效性
        if (left < 0f || left > 1f || top < 0f || top > 1f ||
            right < 0f || right > 1f || bottom < 0f || bottom > 1f)
        {
            Debug.LogWarning($"无效的坐标范围: ({left:F3}, {top:F3}, {right:F3}, {bottom:F3})");
            return false;
        }

        // 检查矩形有效性
        if (right <= left || bottom <= top)
        {
            Debug.LogWarning($"无效的矩形: 右({right:F3})<=左({left:F3}) 或 下({bottom:F3})<=上({top:F3})");
            return false;
        }
        // 检查矩形是否太小
        float width = right - left;
        float height = bottom - top;
        if (width < 0.01f || height < 0.01f)
        {
            Debug.LogWarning($"矩形尺寸过小: {width:F3}x{height:F3}");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 根据索引获取人物显示组件
    /// </summary>
    private CameraTextureView GetPersonDisplayByIndex(int index)
    {
        switch (index)
        {
            case 0: return cameraImagViewForPerson0;
            case 1: return cameraImagViewForPerson1;
            case 2: return cameraImagViewForPerson2;
            case 3: return cameraImagViewForPerson3;
            default: return null;
        }
    }

    /// <summary>
    /// 清空所有人物显示
    /// </summary>
    private void ClearAllPersonDisplays()
    {
        for (int i = 0; i < 4; i++)
        {
            CameraTextureView personDisplay = GetPersonDisplayByIndex(i);
            if (personDisplay != null)
            {
                personDisplay.Clear();
            }
        }
    }

    /// <summary>
    /// 重写销毁方法，清理分割线
    /// </summary>
    protected new void OnDestroy()
    {
        base.OnDestroy();
        ClearRegionLines();
    }
}
