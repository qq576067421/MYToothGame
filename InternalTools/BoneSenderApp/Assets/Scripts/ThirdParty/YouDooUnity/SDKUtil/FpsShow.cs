using UnityEngine;
using UnityEngine.Profiling;

namespace YouDooSDK.Utils
{
public class FpsShow : MonoBehaviour
{
    [Header("显示设置")]
    [SerializeField] private bool showMemoryInfo = true;
    [SerializeField] private int frameRate = 60;
    [SerializeField] private int fpsUpdateInterval = 60;
    [SerializeField] private int memoryLogInterval = 120;

    // 计数器
    [Header("计数器")]
    [SerializeField] private long counter = 0;
    [SerializeField] private bool autoIncrement = true;

    private int frameCount;
    private float deltaTimeAccumulator;

    // GUI 样式
    private GUIStyle guiStyle;

    // void Awake()
    // {
    //     // 初始化设置
    //     Application.targetFrameRate = frameRate;
    //     QualitySettings.vSyncCount = 0;

    //     // 创建 GUI 样式
    //     guiStyle = new GUIStyle
    //     {
    //         fontSize = 14,
    //         normal = { textColor = Color.white },
    //         alignment = TextAnchor.UpperLeft
    //     };

    //     frameCount = 0;
    //     deltaTimeAccumulator = 0f;
    //     counter = 0;
    // }

    // void Update()
    // {
    //     frameCount++;
    //     deltaTimeAccumulator += Time.unscaledDeltaTime;

    //     // 自动增加计数器
    //     if (autoIncrement)
    //     {
    //         counter++;
    //     }

    //     // 更新 FPS 显示
    //     if (frameCount % fpsUpdateInterval == 0)
    //     {
    //         UpdateFpsDisplay();
    //     }

    //     // 定期打印内存信息
    //     if (frameCount % memoryLogInterval == 0)
    //     {
    //         PrintMemoryInfo();
    //     }
    // }

    // // 公开方法：手动增加计数器
    // public void IncrementCounter(int amount = 1)
    // {
    //     counter += amount;
    // }

    // // 公开方法：重置计数器
    // public void ResetCounter()
    // {
    //     counter = 0;
    // }

    // // 公开方法：设置计数器值
    // public void SetCounter(long value)
    // {
    //     counter = value;
    // }

    // // 公开属性：获取当前计数器值
    // public long Counter => counter;

    // private void UpdateFpsDisplay()
    // {
    //     float fps = fpsUpdateInterval / deltaTimeAccumulator;
    //     // 重置计数器
    //     frameCount = 0;
    //     deltaTimeAccumulator = 0f;
    // }

    // void OnGUI()
    // {
    //     if (!showMemoryInfo) return;

    //     // 计算右上角位置
    //     float screenWidth = Screen.width;
    //     float startX = screenWidth - 110;
    //     float startY = 10f;
    //     float lineHeight = 20f;

    //     // 显示 FPS（如果没有 UI Text 组件）
    //     float currentFps = 1f / Time.unscaledDeltaTime;
    //     GUI.Label(new Rect(startX, startY, 350, lineHeight), $"FPS: {Mathf.CeilToInt(currentFps)}", guiStyle);

    //     // 显示计数器
    //     float yOffset = startY + lineHeight;
    //     GUI.Label(new Rect(startX, yOffset, 350, lineHeight), $"计数器: {counter}", guiStyle);

    //     // 获取准确的内存信息
    //     long totalMemory = Profiler.GetTotalAllocatedMemoryLong() / 1048576;
    //     long monoHeapSize = Profiler.GetMonoHeapSizeLong() / 1048576;
    //     long monoUsedSize = Profiler.GetMonoUsedSizeLong() / 1048576;
    //     long totalReservedMemory = Profiler.GetTotalReservedMemoryLong() / 1048576;

    //     // 纹理内存（需要特殊处理）
    //     long textureMemory = GetTextureMemory() / 1048576;
    //     long gpuMemory = Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576;

    //     // 显示内存信息（从计数器下面开始排列）
    //     yOffset += lineHeight;
    //     GUI.Label(new Rect(startX, yOffset, 350, lineHeight), $"总分配内存: {totalMemory} MB", guiStyle);
    //     yOffset += lineHeight;
    //     GUI.Label(new Rect(startX, yOffset, 350, lineHeight), $"Mono堆大小: {monoHeapSize} MB", guiStyle);
    //     yOffset += lineHeight;
    //     GUI.Label(new Rect(startX, yOffset, 350, lineHeight), $"Mono已使用: {monoUsedSize} MB", guiStyle);
    //     yOffset += lineHeight;
    //     GUI.Label(new Rect(startX, yOffset, 350, lineHeight), $"总保留内存: {totalReservedMemory} MB", guiStyle);
    //     yOffset += lineHeight;
    //     GUI.Label(new Rect(startX, yOffset, 350, lineHeight), $"纹理内存: {textureMemory} MB", guiStyle);
    //     yOffset += lineHeight;
    //     GUI.Label(new Rect(startX, yOffset, 350, lineHeight), $"GPU内存: {gpuMemory} MB", guiStyle);
    // }

    // private long GetTextureMemory()
    // {
    //     long textureMemory = 0;
    //     Texture[] textures = Resources.FindObjectsOfTypeAll<Texture>();
    //     foreach (Texture texture in textures)
    //     {
    //         if (texture != null)
    //         {
    //             // 估算纹理内存使用
    //             textureMemory += GetTextureSizeBytes(texture);
    //         }
    //     }
    //     return textureMemory;
    // }

    // private long GetTextureSizeBytes(Texture texture)
    // {
    //     if (texture == null) return 0;

    //     int bitsPerPixel = 32; // 默认假设 RGBA32
    //     if (texture is Texture2D tex2D)
    //     {
    //         switch (tex2D.format)
    //         {
    //             case TextureFormat.RGBA32: bitsPerPixel = 32; break;
    //             case TextureFormat.RGB24: bitsPerPixel = 24; break;
    //             case TextureFormat.RGBA64: bitsPerPixel = 64; break;
    //             case TextureFormat.BC7: bitsPerPixel = 8; break; // 压缩格式
    //                                                              // 可以根据需要添加更多格式
    //         }
    //     }

    //     return (long)(texture.width * texture.height * bitsPerPixel / 8);
    // }

    // private void PrintMemoryInfo()
    // {
    //     long totalMemory = Profiler.GetTotalAllocatedMemoryLong() / 1048576;
    //     long monoHeapSize = Profiler.GetMonoHeapSizeLong() / 1048576;
    //     long monoUsedSize = Profiler.GetMonoUsedSizeLong() / 1048576;
    //     long totalReservedMemory = Profiler.GetTotalReservedMemoryLong() / 1048576;

    //     Debug.Log($"内存使用情况 == " +
    //              $"总分配: {totalMemory}MB, " +
    //              $"Mono堆: {monoHeapSize}MB, " +
    //              $"Mono已使用: {monoUsedSize}MB, " +
    //              $"总保留: {totalReservedMemory}MB, " +
    //              $"计数器: {counter}");
    // }
    //////////////////////////////////////////////小范围打印/////////////////////////////////////

    void Update()
    {
        counter++;
    }

    void OnGUI()
    {


        // 计算右上角位置
        float screenWidth = Screen.width;
        float startX = screenWidth - 310;
        float startY = 10f;
        float lineHeight = 20f;

        // 显示 FPS（如果没有 UI Text 组件）
        // float currentFps = 1f / Time.unscaledDeltaTime;
        // GUI.Label(new Rect(startX, startY, 350, lineHeight), $"FPS: {Mathf.CeilToInt(currentFps)}", guiStyle);

        // 显示计数器
        float yOffset = startY + lineHeight;
        GUI.Label(new Rect(startX, yOffset, 350, lineHeight), $"计数器: {counter}", guiStyle);

    }
    void Awake()
    {
        // 初始化 GUI 样式
        guiStyle = new GUIStyle
        {
            fontSize = 24,  // 增大字体以便更清晰
            normal = { textColor = Color.white },
            alignment = TextAnchor.UpperLeft
        };
    }

}
}
