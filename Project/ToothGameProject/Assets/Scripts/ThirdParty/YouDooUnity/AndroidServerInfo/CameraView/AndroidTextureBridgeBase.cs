using System;
using System.Collections;
using System.Diagnostics;  // 添加这个命名空间
using UnityEngine;

using Debug = UnityEngine.Debug;  // 添加这一行，指定 Debug 是 UnityEngine.Debug

public abstract class AndroidTextureBridgeBase : MonoBehaviour
{
    protected const int TargetDispatchFps = 30;
    protected const float MaxDispatchInterval30Fps = 1.0f / 30.0f;
    protected const float SizeResolveWarnIntervalSeconds = 2.0f;
    protected const float DispatchScheduleToleranceSeconds = 0.001f;

    [Header("图像显示列表")]
    [SerializeField] private CameraTextureView[] cameraViews;

    protected float dispatchMinInterval = 1.0f / 30.0f;

    protected IntPtr renderEventFunc = IntPtr.Zero;
    protected RenderTexture unityRenderTexture;

    private float _nextDispatchTime;
    private float _sizeResolveStartTime;
    private float _lastSizeResolveWarnTime;
    private float _dispatchInterval;
    private bool _isDestroying;
    private bool _isPaused;
    private bool _renderEventFuncMissingLogged;
    private bool _textureSizeResolved;
    private bool _unsupportedBackendLogged;
    private bool _nativeRuntimeInitialized;
    private Coroutine _initCoroutine;

    /// <summary>
    /// 返回当前桥接的日志标签，用于统一打印运行时信息。
    /// </summary>
    protected abstract string BridgeLogTag { get; }

    /// <summary>
    /// 判断当前 Unity 图形后端是否支持此桥接实现。
    /// </summary>
    protected abstract bool IsSupportedGraphicsBackend();

    /// <summary>
    /// 从 native 插件获取渲染事件函数指针。
    /// </summary>
    protected abstract IntPtr FetchRenderEventFunc();

    /// <summary>
    /// 读取 native 侧当前输出纹理宽度。
    /// </summary>
    protected abstract int GetNativeTextureWidth();

    /// <summary>
    /// 读取 native 侧当前输出纹理高度。
    /// </summary>
    protected abstract int GetNativeTextureHeight();

    /// <summary>
    /// 创建当前后端需要使用的 Unity RenderTexture 实例。
    /// </summary>
    protected abstract RenderTexture CreateRenderTextureInstance(int width, int height);

    /// <summary>
    /// 将 Unity RenderTexture 绑定到 native 插件。
    /// </summary>
    protected abstract void BindRenderTextureToNative(RenderTexture renderTexture);

    /// <summary>
    /// 解除 Unity RenderTexture 与 native 插件之间的绑定。
    /// </summary>
    protected abstract void UnbindRenderTextureFromNative();

    /// <summary>
    /// 清理当前后端在 native 插件侧持有的资源。
    /// </summary>
    protected abstract void CleanupNativeResources(bool finalCleanup);

    /// <summary>
    /// 在生命周期更早阶段先做一次后端过滤，避免错误后端脚本继续参与初始化。
    /// </summary>
    protected virtual void Awake()
    {
        // 诊断：输出当前图形后端
        Debug.Log($"[Diagnostic] Current Graphics Backend: {SystemInfo.graphicsDeviceType}");
        Debug.Log($"[Diagnostic] Graphics Device Name: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"[Diagnostic] Is Bridge Supported: {IsSupportedGraphicsBackend()}");
        Debug.Log($"[Diagnostic] Should Use Native Runtime: {ShouldUseNativeRuntime()}");

        if (!CanUseNativeRuntime())
        {
            DisableForUnsupportedBackend();
        }
    }

    /// <summary>
    /// 启动时检查后端并初始化桥接运行时状态。
    /// </summary>
    protected virtual void Start()
    {
        if (!CanUseNativeRuntime())
        {
            DisableForUnsupportedBackend();
            return;
        }

        InitializeRuntime();
    }

    /// <summary>
    /// 每帧按固定节奏派发渲染事件，并在尺寸变化时更新显示区域。
    /// </summary>
    protected virtual void Update()
    {
        if (_isDestroying || _isPaused)
        {
            return;
        }

        if (!CanUseNativeRuntime())
        {
            DisableForUnsupportedBackend();
            return;
        }

        if (renderEventFunc == IntPtr.Zero)
        {
            if (!_renderEventFuncMissingLogged)
            {
                _renderEventFuncMissingLogged = true;
                Debug.LogWarning($"[{BridgeLogTag}] render event function is null. Update dispatch is skipped.");
            }
            return;
        }

        float now = Time.unscaledTime;
        if (now + DispatchScheduleToleranceSeconds < _nextDispatchTime)
        {
            return;
        }

        DispatchFrame(now);

        // 微秒
        // long timeUs = Stopwatch.GetTimestamp() * 1_000_000L / Stopwatch.Frequency;
        // Debug.Log($"出库: {timeUs}μs");


    }

    /// <summary>
    /// 处理应用进入后台和回到前台时的清理与重建。
    /// </summary>
    protected virtual void OnApplicationPause(bool pauseStatus)
    {
        if (!CanUseNativeRuntime())
        {
            DisableForUnsupportedBackend();
            return;
        }

        if (pauseStatus)
        {
            _isPaused = true;
            CleanupRuntime(true, false);
            return;
        }

        _isPaused = false;
        InitializeRuntime();
    }

    /// <summary>
    /// 处理应用焦点切换，作为 pause/resume 之外的恢复兜底。
    /// </summary>
    protected virtual void OnApplicationFocus(bool hasFocus)
    {
        if (!CanUseNativeRuntime())
        {
            DisableForUnsupportedBackend();
            return;
        }
        if (!hasFocus || _isDestroying)
        {
            return;
        }

        if (_isPaused || unityRenderTexture == null || renderEventFunc == IntPtr.Zero)
        {
            _isPaused = false;
            InitializeRuntime();
        }
    }

    /// <summary>
    /// 对象销毁时做清理，确保 native 侧不再持有旧资源。
    /// 传入 false 作为 finalCleanup 避免跨场景切换时底层 Native 全局被清理导致无法恢复。
    /// </summary>
    protected virtual void OnDestroy()
    {
        Debug.Log($"[{BridgeLogTag}] OnDestroy called. Performing non-final cleanup.");
        CleanupRuntime(true, false);
        renderEventFunc = IntPtr.Zero;
    }

    /// <summary>
    /// 退出应用时执行真正的彻底清理。
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        Debug.Log($"[{BridgeLogTag}] OnApplicationQuit called. Performing final cleanup.");
        CleanupRuntime(true, true);
        renderEventFunc = IntPtr.Zero;
    }

    /// <summary>
    /// 获取指定索引的 CameraTextureView
    /// </summary>
    public CameraTextureView GetCameraView(int index)
    {
        if (cameraViews != null && index >= 0 && index < cameraViews.Length)
        {
            return cameraViews[index];
        }
        return null;
    }

    /// <summary>
    /// 获取 CameraTextureView 数组的总长度
    /// </summary>
    public int GetCameraViewCount()
    {
        return cameraViews != null ? cameraViews.Length : 0;
    }

    /// <summary>
    /// 允许业务界面把显示节点放在自己的层级里，再把节点列表交给通用桥接刷新。
    /// </summary>
    public void SetCameraViews(CameraTextureView[] views)
    {
        cameraViews = views;
        ApplyTextureToViews(unityRenderTexture);
        if (_textureSizeResolved && unityRenderTexture != null)
        {
            ApplyRegionToViews(null);
        }
        else
        {
            ClearAllViews();
        }
    }

    /// <summary>
    /// 判断纹理系统是否已经准备完毕
    /// </summary>
    public bool IsTextureReady()
    {
        return _textureSizeResolved && unityRenderTexture != null;
    }

    /// <summary>
    /// 批量设置所有小图的显示区域。
    /// </summary>
    public void SetCameraViewRects(Rect[] rects)
    {
        if (!_textureSizeResolved || unityRenderTexture == null)
        {
            // Debug.LogWarning($"[{BridgeLogTag}] SetCameraViewRects ignored: _textureSizeResolved={_textureSizeResolved}, unityRenderTexture is null={unityRenderTexture == null}");
            return;
        }
        ApplyRegionToViews(rects);
    }

    /// <summary>
    /// 启动后的前两帧内执行 native 预热，并等待 native 回传有效尺寸。
    /// </summary>
    private IEnumerator InitPluginNextFrames()
    {
        yield return null;
        yield return null;

        Debug.Log($"[诊断] renderEventFunc={renderEventFunc}");  // ⭐ 检查是否加载


        if (!CanUseNativeRuntime())
        {
            DisableForUnsupportedBackend();
            yield break;
        }

        if (renderEventFunc == IntPtr.Zero && !_renderEventFuncMissingLogged)
        {
            _renderEventFuncMissingLogged = true;
            Debug.LogWarning($"[{BridgeLogTag}] get render event function returned null. Native event dispatch is unavailable.");
        }

        if (!_isDestroying && renderEventFunc != IntPtr.Zero)
        {
            if (!CanUseNativeRuntime())
            {
                DisableForUnsupportedBackend();
                yield break;
            }

            GL.IssuePluginEvent(renderEventFunc, 0);
        }
        int loopCount = 0;

        while (!_isDestroying && !_textureSizeResolved)
        {
            yield return null;

            int resolvedWidth = GetNativeTextureWidth();
            int resolvedHeight = GetNativeTextureHeight();

            // ⭐ 关键诊断日志
            if (loopCount % 30 == 0)  // 每秒打印一次
            {
                Debug.Log($"[诊断] 循环第{loopCount}帧, width={resolvedWidth}, height={resolvedHeight}");
            }

            if (resolvedWidth <= 0 || resolvedHeight <= 0)
            {
                if (loopCount > 300)  // 5秒超时
                {
                    Debug.LogError($"[诊断] ❌ 超时：无法获取Native纹理尺寸");
                    yield break;
                }
                continue;
            }

            Debug.Log($"[诊断] ✅ Native返回纹理尺寸：{resolvedWidth}x{resolvedHeight}");

            if (resolvedWidth <= 0 || resolvedHeight <= 0)
            {
                float now = Time.unscaledTime;
                if ((now - _lastSizeResolveWarnTime) >= SizeResolveWarnIntervalSeconds)
                {
                    _lastSizeResolveWarnTime = now;
                }
                continue;
            }

            CreateOrReuseRenderTexture(resolvedWidth, resolvedHeight);
            BindRenderTextureToNative(unityRenderTexture);
            ApplyTextureToViews(unityRenderTexture);
            ApplyRegionToViews(null);
            _textureSizeResolved = true;
        }

        _initCoroutine = null;
    }

    /// <summary>
    /// 初始化运行时状态、RenderTexture 绑定和预热协程。
    /// </summary>
    private void InitializeRuntime()
    {
        if (!CanUseNativeRuntime())
        {
            DisableForUnsupportedBackend();
            return;
        }

        StopInitCoroutine();

        _isDestroying = false;
        _nativeRuntimeInitialized = false;
        renderEventFunc = FetchRenderEventFunc();
        _nativeRuntimeInitialized = renderEventFunc != IntPtr.Zero;
        _renderEventFuncMissingLogged = false;

        _dispatchInterval = ResolveDispatchInterval();
        float now = Time.unscaledTime;
        _nextDispatchTime = now;
        _sizeResolveStartTime = now;
        _lastSizeResolveWarnTime = now;

        _textureSizeResolved = false;

        _initCoroutine = StartCoroutine(InitPluginNextFrames());
    }

    /// <summary>
    /// 只有安卓真机运行时才允许调用 youdoo_plugin，编辑器和非安卓平台只做 Unity 侧清理。
    /// </summary>
    private bool ShouldUseNativeRuntime()
    {
        return Application.platform == RuntimePlatform.Android;
    }

    /// <summary>
    /// 原生桥接必须同时满足平台和图形后端，避免错误桥接进入初始化。
    /// </summary>
    private bool CanUseNativeRuntime()
    {
        return ShouldUseNativeRuntime() && IsSupportedGraphicsBackend();
    }

    /// <summary>
    /// 在当前帧触发 native 渲染事件。
    /// </summary>
    private void DispatchFrame(float now)
    {
        GL.IssuePluginEvent(renderEventFunc, 1);
        _nextDispatchTime = Mathf.Max(_nextDispatchTime + _dispatchInterval, now);
    }

    /// <summary>
    /// 把 Unity 纹理对象挂到显示控件上。
    /// </summary>
    private void ApplyTextureToViews(Texture texture)
    {
        if (cameraViews != null)
        {
            foreach (CameraTextureView cameraView in cameraViews)
            {
                if (cameraView == null)
                {
                    continue;
                }

                cameraView.SetUnityTexture(texture);
            }
        }
    }

    /// <summary>
    /// 把当前显示区域同步到所有小图或单图。
    /// </summary>
    private void ApplyRegionToViews(Rect[] rects)
    {
        bool isPartitionView = false;
        if (PlayerMatchView.Instance != null && PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
        {
            isPartitionView = true;
        }

        if (cameraViews == null || cameraViews.Length == 0)
        {
            return;
        }

        if (isPartitionView)
        {
            // 隐藏索引0的主图
            if (cameraViews[0] != null)
            {
                cameraViews[0].Clear();
                cameraViews[0].gameObject.SetActive(false);
            }

            for (int i = 1; i < cameraViews.Length; i++)
            {
                CameraTextureView view = cameraViews[i];
                if (view == null) continue;
                int playerIndex = i - 1;
                if (rects != null && playerIndex < rects.Length)
                {
                    if (rects[playerIndex].width <= 0.0001f || rects[playerIndex].height <= 0.0001f)
                    {
                        view.Clear();
                        view.gameObject.SetActive(false);
                        //Debug.Log($"[{BridgeLogTag}] PartitionView mode: cameraViews[{i}] invalid rect (cleared).");
                    }
                    else
                    {
                        view.gameObject.SetActive(true);
                        if (unityRenderTexture != null)
                        {
                            view.SetUnityTexture(unityRenderTexture);
                        }
                        view.ChangeImageRect(rects[playerIndex]);
                        //Debug.Log($"[{BridgeLogTag}] PartitionView mode: cameraViews[{i}] enabled with rect: {rects[playerIndex]}");
                    }
                }
                else
                {
                    view.Clear();
                    view.gameObject.SetActive(false);
                    //Debug.Log($"[{BridgeLogTag}] PartitionView mode: cameraViews[{i}] disabled (out of range).");
                }
            }
        }
        else
        {
            // 非 PartitionView 模式：使用 cameraViews[0] 作为全屏主图显示，隐藏其余所有图
            for (int i = 0; i < cameraViews.Length; i++)
            {
                CameraTextureView view = cameraViews[i];
                if (view == null) continue;

                if (i == 0)
                {
                    view.gameObject.SetActive(true);
                    if (unityRenderTexture != null)
                    {
                        view.SetUnityTexture(unityRenderTexture);
                    }

                    // 防越界处理
                    Rect targetRect = (rects != null && rects.Length > 0) ? rects[0] : new Rect(0f, 0f, 1f, 1f);
                    view.ChangeImageRect(targetRect);
                    //Debug.Log($"[{BridgeLogTag}] Non-PartitionView mode: cameraViews[0] enabled with rect: {targetRect}");
                }
                else
                {
                    view.Clear();
                    view.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 清空所有显示控件上的纹理，避免保留旧画面。
    /// </summary>
    private void ClearAllViews()
    {
        if (cameraViews != null)
        {
            foreach (CameraTextureView cameraView in cameraViews)
            {
                if (cameraView == null)
                {
                    continue;
                }

                cameraView.Clear();
            }
        }
    }

    /// <summary>
    /// 创建或复用 Unity RenderTexture，避免尺寸不变时重复重建。
    /// </summary>
    private void CreateOrReuseRenderTexture(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (unityRenderTexture != null && unityRenderTexture.IsCreated())
        {
            if (unityRenderTexture.width == width && unityRenderTexture.height == height)
            {
                return;
            }

            unityRenderTexture.Release();
            Destroy(unityRenderTexture);
            unityRenderTexture = null;
        }

        unityRenderTexture = CreateRenderTextureInstance(width, height);
        if (unityRenderTexture != null && !unityRenderTexture.IsCreated())
        {
            unityRenderTexture.Create();
        }
    }

    /// <summary>
    /// 清理运行时状态、解绑 native 纹理并释放 Unity 侧资源。
    /// </summary>
    private void CleanupRuntime(bool cleanupNative, bool finalCleanup)
    {
        
        StopInitCoroutine();
        _isDestroying = true;
        _textureSizeResolved = false;

        ClearAllViews();

        bool shouldCleanupNative = cleanupNative && _nativeRuntimeInitialized && ShouldUseNativeRuntime();
        if (shouldCleanupNative)
        {
            UnbindRenderTextureFromNative();
            CleanupNativeResources(finalCleanup);
            _nativeRuntimeInitialized = false;
        }
        else if (cleanupNative)
        {
            _nativeRuntimeInitialized = false;
        }

        if (unityRenderTexture != null)
        {
            unityRenderTexture.Release();
            Destroy(unityRenderTexture);
            unityRenderTexture = null;
        }
    }

    /// <summary>
    /// 停止当前正在运行的初始化协程，避免重复启动。
    /// </summary>
    private void StopInitCoroutine()
    {
        if (_initCoroutine != null)
        {
            StopCoroutine(_initCoroutine);
            _initCoroutine = null;
        }
    }

    /// <summary>
    /// 当图形后端不匹配时禁用桥接，避免误派发错误后端的插件事件。
    /// </summary>
    private void DisableForUnsupportedBackend()
    {
        if (!_unsupportedBackendLogged)
        {
            _unsupportedBackendLogged = true;
        }
        CleanupRuntime(false, false);
        enabled = false;
    }

    /// <summary>
    /// 计算本桥接允许使用的派发间隔，并限制到 30fps 目标范围内。
    /// </summary>
    private float ResolveDispatchInterval()
    {
        if (dispatchMinInterval <= 0f)
        {
            return MaxDispatchInterval30Fps;
        }

        return Mathf.Min(dispatchMinInterval, MaxDispatchInterval30Fps);
    }
}
