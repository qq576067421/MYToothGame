/*
作者：Ting
创建时间：2025.10.07
描述：范例，显示整体的摄像机图像，显示多人的裁剪区域
*/
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class CameraImagView : MonoBehaviour
{
    [DllImport("youdoo_plugin")]
    protected static extern bool CopyHardwareBufferToMemory(long hardwareBufferPtr, IntPtr outputBuffer, int bufferSize);


    [DllImport("youdoo_plugin")]
    protected static extern bool CopyHardwareBufferToMemoryWithScale(long hardwareBufferPtr, IntPtr outputBuffer,
                                                                    int bufferSize, float scaleFactor);

    [DllImport("youdoo_plugin")]
    protected static extern void GetHardwareBufferInfo(long hardwareBufferPtr, out int width, out int height, out int format);

    [Header("显示设置")]
    public CameraTextureView cameraTextureView;

    [Header("缩放设置")]
    [Range(0.1f, 1.0f)]
    protected float baseScale = 0.3f;  // 添加缩放比例参数


    [Header("性能统计")]
    [Tooltip("是否启用性能统计")]
    public bool enablePerformanceStats = true;
    [Tooltip("统计采样次数")]
    public int statsSampleCount = 10;
    [Tooltip("显示最近几次的平均耗时")]
    public int recentStatsCount = 10;

    private Rect _cameraTextureViewBgRect;


    // 内存管理
    private IntPtr _bufferPtr = IntPtr.Zero;
    private int _bufferSizeAllocated = 0;
    protected Texture2D _externalTexture = null;
    protected Texture2D _scaleTexture = null;


    // 显示模式控制
    private DisplayMode _currentDisplayMode = DisplayMode.FullImage;

    private float _totalRenderTime = 0f;
    private int _renderCount = 0;
    private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

    // 常量
    private const int BYTES_PER_PIXEL = 4;
    private const float PERSON_MARGIN = 0.02f;
    private const float MIN_PERSON_SIZE = 0.1f;

    /// <summary>
    /// 显示模式枚举
    /// </summary>
    public enum DisplayMode
    {
        FullImage,          // 显示完整图像
        PersonCropped,      // 显示人物裁切区域
        ScaleOnly           // 仅用于缩放显示
    }

    /// <summary>
    /// 基础纹理更新，显示完整的一张图（带缩放）
    /// </summary>
    public void CreateOrUpdateTextureFromHardwareBufferPtr(long hardwareBufferPtr)
    {
        if (enablePerformanceStats)
            _stopwatch.Restart();

        try
        {
            // Debug.Log($"绘制原图 缩放比例: {baseScale}, bufferPtr: {hardwareBufferPtr}");
            _currentDisplayMode = DisplayMode.FullImage;

            GetHardwareBufferInfo(hardwareBufferPtr, out int srcWidth, out int srcHeight, out int format);
            if (srcWidth <= 0 || srcHeight <= 0) return;

            // 计算缩放后的尺寸
            int dstWidth = Mathf.Max(1, (int)(srcWidth * baseScale));
            int dstHeight = Mathf.Max(1, (int)(srcHeight * baseScale));
            int requiredBufferSize = dstWidth * dstHeight * BYTES_PER_PIXEL;

            // 缓冲区管理
            if (requiredBufferSize > _bufferSizeAllocated)
            {
                if (_bufferPtr != IntPtr.Zero) Marshal.FreeHGlobal(_bufferPtr);
                _bufferPtr = Marshal.AllocHGlobal(requiredBufferSize);
                _bufferSizeAllocated = requiredBufferSize;
                // Debug.Log($"重新分配缓冲区: {requiredBufferSize} bytes, 缩放后尺寸: {dstWidth}x{dstHeight}");
            }

            // 纹理管理 - 使用缩放后的尺寸
            if (_externalTexture == null || _externalTexture.width != dstWidth || _externalTexture.height != dstHeight)
            {
                DestroyImmediate(_externalTexture);
                DestroyImmediate(_scaleTexture);

                _externalTexture = new Texture2D(dstWidth, dstHeight, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                _scaleTexture = new Texture2D(dstWidth, dstHeight, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                Debug.Log($"创建缩放纹理: {dstWidth}x{dstHeight}");
            }

            // 数据拷贝（使用带缩放的函数）
            if (CopyHardwareBufferToMemoryWithScale(hardwareBufferPtr, _bufferPtr, requiredBufferSize, baseScale))
            {
                _externalTexture.LoadRawTextureData(_bufferPtr, requiredBufferSize);
                _externalTexture.Apply(false, false);

                // 复制纹理到缩放纹理
                Graphics.CopyTexture(_externalTexture, _scaleTexture);

                if (cameraTextureView != null)
                {
                    // 完整模式：强制重置UV为完整图像
                    cameraTextureView.ChangeImageRect(_cameraTextureViewBgRect);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"纹理更新异常: {e.Message}");
            DestroyImmediate(_externalTexture);
            DestroyImmediate(_scaleTexture);
            _externalTexture = null;
            _scaleTexture = null;
        }
        finally
        {
            if (enablePerformanceStats)
            {
                _stopwatch.Stop();
                RecordRenderTime(_stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// 带人物区域显示的纹理更新（带缩放）
    /// </summary>
    public void CreateOrUpdateTextureFromHardwareBufferPtrWithPersons(long hardwareBufferPtr)
    {
        try
        {
            _currentDisplayMode = DisplayMode.PersonCropped;

            // 先获取纹理数据（带缩放）
            GetTextureDataOnly(hardwareBufferPtr);

            // 计算并应用人物边界
            if (_cameraTextureViewBgRect != null)
            {
                if (cameraTextureView != null)
                {
                    cameraTextureView.ChangeImageRect(_cameraTextureViewBgRect);
                }
            }
            else
            {
                // 如果没有人物数据，显示完整图像
                if (cameraTextureView != null)
                {
                    cameraTextureView.ChangeImageRect(new Rect(0, 0, 1, 1));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"人物区域显示异常: {e.Message}");
        }
    }


    /// <summary>
    /// 记录渲染时间
    /// </summary>
    private void RecordRenderTime(float timeMs)
    {
        _renderCount++;
        _totalRenderTime += timeMs;
        // 调试日志：输出渲染时间队列和总时间
        Debug.Log($"调试信息 - 当前渲染时间: [{timeMs}], 总渲染时间: {_totalRenderTime:F2}ms, 总次数: {_renderCount}, 平均时间: {_totalRenderTime / _renderCount:F2}ms");
    }



    /// <summary>
    /// 仅获取纹理数据，不设置显示（供人物裁切模式使用，带缩放）
    /// </summary>
    private void GetTextureDataOnly(long hardwareBufferPtr)
    {
        try
        {
            GetHardwareBufferInfo(hardwareBufferPtr, out int srcWidth, out int srcHeight, out int format);
            if (srcWidth <= 0 || srcHeight <= 0) return;

            // 计算缩放后的尺寸
            int dstWidth = Mathf.Max(1, (int)(srcWidth * baseScale));
            int dstHeight = Mathf.Max(1, (int)(srcHeight * baseScale));
            int requiredBufferSize = dstWidth * dstHeight * BYTES_PER_PIXEL;

            // 缓冲区管理
            if (requiredBufferSize > _bufferSizeAllocated)
            {
                if (_bufferPtr != IntPtr.Zero) Marshal.FreeHGlobal(_bufferPtr);
                _bufferPtr = Marshal.AllocHGlobal(requiredBufferSize);
                _bufferSizeAllocated = requiredBufferSize;
            }

            // 纹理管理 - 使用缩放后的尺寸
            if (_externalTexture == null || _externalTexture.width != dstWidth || _externalTexture.height != dstHeight)
            {
                DestroyImmediate(_externalTexture);
                DestroyImmediate(_scaleTexture);

                _externalTexture = new Texture2D(dstWidth, dstHeight, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                _scaleTexture = new Texture2D(dstWidth, dstHeight, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            // 数据拷贝（使用带缩放的函数）
            if (CopyHardwareBufferToMemoryWithScale(hardwareBufferPtr, _bufferPtr, requiredBufferSize, baseScale))
            {
                _externalTexture.LoadRawTextureData(_bufferPtr, requiredBufferSize);
                _externalTexture.Apply(false, false);

                // 复制纹理到缩放纹理
                Graphics.CopyTexture(_externalTexture, _scaleTexture);

            }
        }
        catch (Exception e)
        {
            Debug.LogError($"获取纹理数据异常: {e.Message}");
            DestroyImmediate(_externalTexture);
            DestroyImmediate(_scaleTexture);
            _externalTexture = null;
            _scaleTexture = null;
        }
    }

    /// <summary>
    /// 设置缩放比例
    /// </summary>
    public void SetBaseScale(float scale)
    {
        baseScale = Mathf.Clamp(scale, 0.1f, 1.0f);
        Debug.Log($"设置缩放比例: {baseScale}");

        // 强制重新创建纹理
        if (_externalTexture != null)
        {
            DestroyImmediate(_externalTexture);
            DestroyImmediate(_scaleTexture);
            _externalTexture = null;
            _scaleTexture = null;
        }
    }

    /// <summary>
    /// 获取用于缩放的独立纹理
    /// </summary>
    public Texture2D GetScaleTexture()
    {
        return _scaleTexture;
    }

    /// <summary>
    /// 计算所有人物的边界框
    /// </summary>
    private Rect CalculateAllPersonBounds(float[,] personPlayerRectf)
    {
        if (personPlayerRectf == null)
            return new Rect(0, 0, 1, 1);

        float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
        bool hasValidPerson = false;

        int personCount = Math.Min(personPlayerRectf.GetLength(0), 4);
        for (int i = 0; i < personCount; i++)
        {
            float left = personPlayerRectf[i, 0];
            float top = personPlayerRectf[i, 1];
            float right = personPlayerRectf[i, 2];
            float bottom = personPlayerRectf[i, 3];

            // 跳过无效数据
            if (left == 0 && top == 0 && right == 0 && bottom == 0)
                continue;

            hasValidPerson = true;
            minX = Mathf.Min(minX, left);
            minY = Mathf.Min(minY, top);
            maxX = Mathf.Max(maxX, right);
            maxY = Mathf.Max(maxY, bottom);
        }

        if (!hasValidPerson)
            return new Rect(0, 0, 1, 1);

        // 添加边距
        minX = Mathf.Max(0f, minX - PERSON_MARGIN);
        minY = Mathf.Max(0f, minY - PERSON_MARGIN);
        maxX = Mathf.Min(1f, maxX + PERSON_MARGIN);
        maxY = Mathf.Min(1f, maxY + PERSON_MARGIN);

        // 确保最小尺寸
        float width = maxX - minX;
        float height = maxY - minY;

        if (width < MIN_PERSON_SIZE)
        {
            float centerX = (minX + maxX) * 0.5f;
            float halfWidth = MIN_PERSON_SIZE * 0.5f;
            minX = Mathf.Max(0f, centerX - halfWidth);
            maxX = Mathf.Min(1f, centerX + halfWidth);
        }

        if (height < MIN_PERSON_SIZE)
        {
            float centerY = (minY + maxY) * 0.5f;
            float halfHeight = MIN_PERSON_SIZE * 0.5f;
            minY = Mathf.Max(0f, centerY - halfHeight);
            maxY = Mathf.Min(1f, centerY + halfHeight);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// 重置平滑状态
    /// </summary>
    public void ResetSmoothing()
    {
        if (cameraTextureView != null && _externalTexture != null)
        {
            cameraTextureView.ChangeImageRect(new Rect(0, 0, 1, 1));
        }
    }

    /// <summary>
    /// 设置平滑帧数
    /// </summary>
    public void SetSmoothFrames(int frames)
    {
        // 废弃的方法，平滑逻辑现在由CameraTextureView内部处理
    }

    /// <summary>
    /// 手动切换到完整图像模式
    /// </summary>
    public void SwitchToFullImageMode()
    {
        _currentDisplayMode = DisplayMode.FullImage;
        if (cameraTextureView != null && _externalTexture != null)
        {
            cameraTextureView.ChangeImageRect(new Rect(0, 0, 1, 1));
        }
    }

    /// <summary>
    /// 手动切换到人物裁切模式
    /// </summary>
    public void SwitchToPersonCroppedMode()
    {
        _currentDisplayMode = DisplayMode.PersonCropped;
        ResetSmoothing();
    }

    /// <summary>
    /// 获取当前显示模式
    /// </summary>
    public DisplayMode GetCurrentDisplayMode()
    {
        return _currentDisplayMode;
    }

    /// <summary>
    /// 资源清理
    /// </summary>
    protected virtual void OnDestroy()
    {
        ManualCleanup();
    }

    /// <summary>
    /// 手动释放资源
    /// </summary>
    public void ManualCleanup()
    {
        DestroyImmediate(_externalTexture);
        DestroyImmediate(_scaleTexture);
        _externalTexture = null;
        _scaleTexture = null;

        if (_bufferPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_bufferPtr);
            _bufferPtr = IntPtr.Zero;
            _bufferSizeAllocated = 0;
        }
    }

    /// <summary>
    /// 设置底图的框
    /// </summary>
    public void SetCameraTextureViewBgRect(Rect cameraTextureViewBgRect)
    {
        _cameraTextureViewBgRect = cameraTextureViewBgRect;
        Debug.Log("底图的框 = ：" + _cameraTextureViewBgRect.x + " " + _cameraTextureViewBgRect.y + " " + _cameraTextureViewBgRect.width + " " + _cameraTextureViewBgRect.height);
    }
}
