using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine;

public class AndroidTextureBridgeVulkan : AndroidTextureBridgeBase
{
    [DllImport("youdoo_plugin")]
    private static extern IntPtr GetRenderEventFuncVulkan();

    [DllImport("youdoo_plugin")]
    private static extern int GetTextureWidthVulkan();

    [DllImport("youdoo_plugin")]
    private static extern int GetTextureHeightVulkan();

    [DllImport("youdoo_plugin")]
    private static extern void SetUnityTexturePtrVulkan(long ptr);

    [DllImport("youdoo_plugin")]
    private static extern void CleanupVulkan();

    /// <summary>
    /// 返回 Vulkan 桥接的统一日志标签。
    /// </summary>
    protected override string BridgeLogTag
    {
        get { return "AndroidTextureBridgeVulkan"; }
    }

    /// <summary>
    /// 判断当前图形后端是否为 Vulkan。
    /// </summary>
    protected override bool IsSupportedGraphicsBackend()
    {
        return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan;
    }

    /// <summary>
    /// 获取 Vulkan native 插件暴露的渲染事件函数指针。
    /// </summary>
    protected override IntPtr FetchRenderEventFunc()
    {
        return GetRenderEventFuncVulkan();
    }

    /// <summary>
    /// 从 Vulkan native 插件读取当前输出纹理宽度。
    /// </summary>
    protected override int GetNativeTextureWidth()
    {
        return GetTextureWidthVulkan();
    }

    /// <summary>
    /// 从 Vulkan native 插件读取当前输出纹理高度。
    /// </summary>
    protected override int GetNativeTextureHeight()
    {
        return GetTextureHeightVulkan();
    }

    /// <summary>
    /// 创建 Vulkan 路径使用的支持随机写入的 RenderTexture。
    /// </summary>
    protected override RenderTexture CreateRenderTextureInstance(int width, int height)
    {
        return new RenderTexture(width, height, 0)
        {
            graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
            enableRandomWrite = true,
            useMipMap = false,
            autoGenerateMips = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
    }

    /// <summary>
    /// 把 Unity RenderTexture 的 native 指针同步给 Vulkan native 插件。
    /// </summary>
    protected override void BindRenderTextureToNative(RenderTexture renderTexture)
    {
        if (renderTexture == null)
        {
            return;
        }

        long texPtr = renderTexture.GetNativeTexturePtr().ToInt64();
        SetUnityTexturePtrVulkan(texPtr);
    }

    /// <summary>
    /// 通知 Vulkan native 插件当前没有可用的 Unity 输出纹理。
    /// </summary>
    protected override void UnbindRenderTextureFromNative()
    {
        SetUnityTexturePtrVulkan(0);
    }

    /// <summary>
    /// 执行 Vulkan native 插件的资源清理。
    /// </summary>
    protected override void CleanupNativeResources(bool finalCleanup)
    {
        if (finalCleanup || renderEventFunc == IntPtr.Zero)
        {
            CleanupVulkan();
            return;
        }

        GL.IssuePluginEvent(renderEventFunc, 2);
    }
}