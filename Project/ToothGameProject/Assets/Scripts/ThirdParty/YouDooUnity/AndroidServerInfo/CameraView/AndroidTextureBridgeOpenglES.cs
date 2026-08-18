using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine;

public class AndroidTextureBridgeOpenglES : AndroidTextureBridgeBase
{
    [DllImport("youdoo_plugin")]
    private static extern System.IntPtr GetRenderEventFuncOpenglES();
    [DllImport("youdoo_plugin")]
    private static extern void SetUnityTexturePtrOpenglES(long ptr);
    [DllImport("youdoo_plugin")]
    private static extern void SetUnityTextureSizeOpenglES(int width, int height);
    [DllImport("youdoo_plugin")]
    private static extern int GetTextureWidthOpenglES();
    [DllImport("youdoo_plugin")]
    private static extern int GetTextureHeightOpenglES();

    /// <summary>
    /// 返回 OpenGL ES 桥接的统一日志标签。
    /// </summary>
    protected override string BridgeLogTag
    {
        get { return "AndroidTextureBridgeOpenglES"; }
    }

    /// <summary>
    /// 判断当前图形后端是否为 OpenGL ES。
    /// </summary>
    protected override bool IsSupportedGraphicsBackend()
    {
        GraphicsDeviceType graphicsDeviceType = SystemInfo.graphicsDeviceType;
        return graphicsDeviceType == GraphicsDeviceType.OpenGLES2 ||
               graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
    }

    /// <summary>
    /// 获取 OpenGL ES native 插件暴露的渲染事件函数指针。
    /// </summary>
    protected override IntPtr FetchRenderEventFunc()
    {
        return GetRenderEventFuncOpenglES();
    }

    /// <summary>
    /// 从 OpenGL ES native 插件读取当前输出纹理宽度。
    /// </summary>
    protected override int GetNativeTextureWidth()
    {
        return GetTextureWidthOpenglES();
    }

    /// <summary>
    /// 从 OpenGL ES native 插件读取当前输出纹理高度。
    /// </summary>
    protected override int GetNativeTextureHeight()
    {
        return GetTextureHeightOpenglES();
    }

    /// <summary>
    /// 创建 OpenGL ES 路径使用的标准 ARGB32 RenderTexture。
    /// </summary>
    protected override RenderTexture CreateRenderTextureInstance(int width, int height)
    {
        return new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            useMipMap = false,
            autoGenerateMips = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
    }

    /// <summary>
    /// 把 Unity RenderTexture 的 native 指针和尺寸同步给 OpenGL ES native 插件。
    /// </summary>
    protected override void BindRenderTextureToNative(RenderTexture renderTexture)
    {
        if (renderTexture == null)
        {
            return;
        }

        SetUnityTextureSizeOpenglES(renderTexture.width, renderTexture.height);
        SetUnityTexturePtrOpenglES(renderTexture.GetNativeTexturePtr().ToInt64());
    }

    /// <summary>
    /// 通知 OpenGL ES native 插件当前没有可用的 Unity 输出纹理。
    /// </summary>
    protected override void UnbindRenderTextureFromNative()
    {
        SetUnityTexturePtrOpenglES(0);
        SetUnityTextureSizeOpenglES(0, 0);
    }

    /// <summary>
    /// 在图形线程上触发 OpenGL ES native 资源清理事件。
    /// </summary>
    protected override void CleanupNativeResources(bool finalCleanup)
    {
        if (renderEventFunc != IntPtr.Zero)
        {
            GL.IssuePluginEvent(renderEventFunc, 2);
        }
    }

}
