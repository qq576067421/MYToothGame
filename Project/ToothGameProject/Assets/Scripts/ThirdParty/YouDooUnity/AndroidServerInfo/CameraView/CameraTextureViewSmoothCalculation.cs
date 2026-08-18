using UnityEngine;

/// <summary>
/// 平滑计算UV Rect的类
/// 实现强制匹配RawImage比例，支持反向扩展
/// </summary>
public class CameraTextureViewSmoothCalculation
{
    private float playerTopOffestFromRawImageTop = 0.1f; //顶部偏移

    // ========== 平滑配置参数 ==========
    private float smoothingSpeed = 0.05f; // 平滑过渡速度 (0-1)  

    // ========== 缓存数据 ==========
    private Rect cachedResult = new Rect(0, 0, 1, 1);

    // 输入源滤波缓存
    private Rect _filteredOriginalRegion = new Rect(0, 0, 1, 1);
    private bool _isFirstOriginalRegion = true;

    public float SmoothingSpeed
    {
        get => smoothingSpeed;
        set => smoothingSpeed = Mathf.Clamp01(value);
    }

    public virtual void SetSmoothConfig(float smoothSpeed, float filterSpeed, float deadzone)
    {
        SmoothingSpeed = smoothSpeed;
    }

    public virtual void ResetSmoothState()
    {
        cachedResult = new Rect(0, 0, 1, 1);
        _filteredOriginalRegion = new Rect(0, 0, 1, 1);
        _isFirstOriginalRegion = true;
    }

    // ========== 公有方法 ==========
    /// <summary>
    /// 计算适配RawImage宽高比的UV矩形区域
    /// 保持原始区域内容比例，避免拉伸
    /// </summary>
    /// <param name="originalRegion">归一化的原始显示区域 (0-1)</param>
    /// <param name="rawImageAspect">RawImage的宽高比 (width/height)</param>
    /// <param name="imageWidth">原始纹理宽度 (像素)</param>
    /// <param name="imageHeight">原始纹理高度 (像素)</param>
    /// <returns>调整后的归一化UV矩形区域</returns>
    public virtual Rect CalculateUvRect(Rect originalRegion, float rawImageAspect, int imageWidth, int imageHeight)
    {
        // 1. 参数验证
        if (!ValidateParameters(originalRegion, rawImageAspect, imageWidth, imageHeight))
        {
            return new Rect(0, 0, 1, 1);
        }

        // 2. 输入源预滤波（过滤AI高频跳动）
        if (_isFirstOriginalRegion)
        {
            _filteredOriginalRegion = originalRegion;
        }
        else
        {
            float diffX = Mathf.Abs(originalRegion.x - _filteredOriginalRegion.x);
            float diffY = Mathf.Abs(originalRegion.y - _filteredOriginalRegion.y);
            float diffW = Mathf.Abs(originalRegion.width - _filteredOriginalRegion.width);
            float diffH = Mathf.Abs(originalRegion.height - _filteredOriginalRegion.height);

            // 设定死区：如果目标框的改变小于 2.5%，完全忽略它，使其彻底静止不动
            float deadzone = 0.025f;
            if (diffX > deadzone || diffY > deadzone || diffW > deadzone || diffH > deadzone)
            {
                // 如果改变大于死区，我们用一个 EMA 平滑来追踪新的位置，而不是瞬间跳变
                float filterSpeed = 0.2f;
                _filteredOriginalRegion = new Rect(
                    Mathf.Lerp(_filteredOriginalRegion.x, originalRegion.x, filterSpeed),
                    Mathf.Lerp(_filteredOriginalRegion.y, originalRegion.y, filterSpeed),
                    Mathf.Lerp(_filteredOriginalRegion.width, originalRegion.width, filterSpeed),
                    Mathf.Lerp(_filteredOriginalRegion.height, originalRegion.height, filterSpeed)
                );
            }
        }

        // 计算纹理的宽高比
        float textureAspect = (float)imageWidth / imageHeight;

        // 计算目标UV的宽高比，使得显示的图像不失真
        float targetUVAspect = rawImageAspect / textureAspect;

        // 基于过滤后的 _filteredOriginalRegion 计算最小包围 UV Rect
        float roiAspect = _filteredOriginalRegion.width / _filteredOriginalRegion.height;

        float newWidth, newHeight;

        if (roiAspect > targetUVAspect)
        {
            // ROI 更宽，宽度定死，增加高度
            newWidth = _filteredOriginalRegion.width;
            newHeight = newWidth / targetUVAspect;
        }
        else
        {
            // ROI 更高，高度定死，增加宽度
            newHeight = _filteredOriginalRegion.height;
            newWidth = newHeight * targetUVAspect;
        }

        // 保持中心点一致
        Vector2 center = _filteredOriginalRegion.center;
        Rect targetRect = new Rect(
            center.x - newWidth * 0.5f,
            center.y - newHeight * 0.5f,
            newWidth,
            newHeight
        );

        // 限制在 [0,1] 范围内，同时保持宽高比

        // 1. 如果尺寸超出 1.0，则按比例缩小
        if (targetRect.width > 1.0f || targetRect.height > 1.0f)
        {
            float scaleW = 1.0f / targetRect.width;
            float scaleH = 1.0f / targetRect.height;
            float scale = Mathf.Min(scaleW, scaleH);

            float w = targetRect.width * scale;
            float h = targetRect.height * scale;
            targetRect = new Rect(
                targetRect.center.x - w * 0.5f,
                targetRect.center.y - h * 0.5f,
                w,
                h
            );
        }

        // 2. 限制位置在 [0, 1] 内 (平移以适应边界)
        // 此时 width 和 height 肯定 <= 1.0
        float clampedX = Mathf.Clamp(targetRect.x, 0f, 1f - targetRect.width);
        float clampedY = GetTopAnchoredY(_filteredOriginalRegion.y, targetRect.height);
        targetRect.x = clampedX;
        targetRect.y = clampedY;

        if (_isFirstOriginalRegion)
        {
            _isFirstOriginalRegion = false;
            cachedResult = targetRect;
            return new Rect(targetRect.x, targetRect.y, targetRect.width, targetRect.height);
        }

        // 平滑过渡
        cachedResult = SmoothTransition(cachedResult, targetRect);
        return cachedResult;
    }


    /// <summary>
    /// 验证输入参数有效性
    /// </summary>
    private bool ValidateParameters(Rect originalRegion, float rawImageAspect, int imageWidth, int imageHeight)
    {
        if (originalRegion.width <= 0 || originalRegion.height <= 0)
        {
            return false;
        }

        if (rawImageAspect <= 0 || float.IsInfinity(rawImageAspect) || float.IsNaN(rawImageAspect))
        {
            return false;
        }

        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 平滑过渡到目标区域
    /// </summary>
    private Rect SmoothTransition(Rect current, Rect target)
    {
        // 只要宽或高其中一个要变大，我们就认为是拉远(缩小画面，防止人物出框)，此时响应速度快一点
        // 如果是拉近(放大画面)，则使用较慢的基础平滑速度，让画面更稳定
        bool isZoomingOut = target.width > current.width || target.height > current.height;
        float scaleSpeed = isZoomingOut ? Mathf.Clamp01(smoothingSpeed * 3f) : smoothingSpeed;

        return new Rect(
            Mathf.Lerp(current.x, target.x, smoothingSpeed),
            Mathf.Lerp(current.y, target.y, smoothingSpeed),
            Mathf.Lerp(current.width, target.width, scaleSpeed),
            Mathf.Lerp(current.height, target.height, scaleSpeed));
    }

    private float GetTopAnchoredY(float playerTop, float uvHeight)
    {
        return Mathf.Clamp(
            playerTop - uvHeight * playerTopOffestFromRawImageTop,
            0f,
            1f - uvHeight);
    }
}
