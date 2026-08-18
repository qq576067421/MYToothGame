using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

/// <summary>
/// 头像显示组件 - 只负责显示图片
/// 对外只有一个接口：showImage(string path)
/// </summary>
public class HeadImage : MonoBehaviour
{
    [Header("图片显示组件")]
    public RawImage displayImage; // 需要拖拽赋值的RawImage

    private Texture2D defaultTexture; // 默认纹理

    void Awake()
    {
        if (displayImage == null)
        {
            displayImage = GetComponent<RawImage>();
            if (displayImage == null)
            {
                Debug.LogError("HeadImage: 请指定RawImage组件");
                return;
            }
        }

        // 保存当前纹理作为默认纹理
        defaultTexture = displayImage.texture as Texture2D;
    }

    /// <summary>
    /// 唯一对外接口 - 显示图片
    /// </summary>
    /// <param name="path">图片路径（支持http://, https://, content://, file://）</param>
    public void ShowImage(string path)
    {

        Debug.Log("HeadImage: 显示图片 A- " + path);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("HeadImage: 图片路径为空");
            return;
        }
        Debug.Log("HeadImage: 显示图片 B- " + path);
        // 停止所有正在进行的加载协程
        StopAllCoroutines();
        Debug.Log("HeadImage: 显示图片 C- " + path);
        // 根据路径类型选择加载方式
        if (path.StartsWith("http://") || path.StartsWith("https://"))
        {
            // 网络图片
            StartCoroutine(LoadFromHttp(path));
        }
        else if (path.StartsWith("content://"))
        {
            StartCoroutine(LoadFromContentUri(path));
        }
        else if (path.StartsWith("file://"))
        {
            // 文件URI
            StartCoroutine(LoadFromLocal(path.Replace("file://", "")));
        }
        else
        {
            // 本地路径
            StartCoroutine(LoadFromLocal(path));
        }
        Debug.Log("HeadImage: 显示图片 D- " + path);
    }

    /// <summary>
    /// 从HTTP加载图片
    /// </summary>
    private IEnumerator LoadFromHttp(string url)
    {
        Debug.Log($"HeadImage: 加载网络图片 - {url}");

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = 10; // 10秒超时

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    displayImage.texture = texture;
                    // 去掉 SetNativeSize 以使用原先在编辑器里设定的大小
                    // displayImage.SetNativeSize();
                    // Debug.Log($"HeadImage: 网络图片加载成功 - {texture.width}x{texture.height}");
                    // Debug.Log($"HeadImage: RawImage尺寸 = {displayImage.rectTransform.rect.width}x{displayImage.rectTransform.rect.height}");
                    // Debug.Log($"HeadImage: RawImage颜色 = {displayImage.color}");
                }
            }
            else
            {
                Debug.LogError($"HeadImage: 网络图片加载失败 - {request.error}");
                // 加载失败时恢复默认纹理
                if (defaultTexture != null)
                    displayImage.texture = defaultTexture;
            }
        }
    }

    /// <summary>
    /// 从本地加载图片
    /// </summary>
    private IEnumerator LoadFromLocal(string path)
    {
        Debug.Log($"HeadImage: 加载本地图片 - {path}");

        // 检查文件是否存在
        if (!File.Exists(path))
        {
            Debug.LogError($"HeadImage: 文件不存在 - {path}");
            if (defaultTexture != null)
                displayImage.texture = defaultTexture;
            yield break;
        }

        // 使用协程异步加载，避免卡顿
        yield return null;

        try
        {
            byte[] imageData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(imageData))
            {
                displayImage.texture = texture;
                // 去掉 SetNativeSize 以使用原先在编辑器里设定的大小
                // displayImage.SetNativeSize();
                Debug.Log($"HeadImage: 本地图片加载成功 - {texture.width}x{texture.height}");
            }
            else
            {
                Debug.LogError("HeadImage: 图片格式不支持或文件损坏");
                if (defaultTexture != null)
                    displayImage.texture = defaultTexture;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HeadImage: 本地图片加载失败 - {e.Message}");
            if (defaultTexture != null)
                displayImage.texture = defaultTexture;
        }
    }


    /// <summary>
    /// 从Android Content URI加载图片
    /// </summary>
    private IEnumerator LoadFromContentUri(string contentUri)
    {
        Debug.Log($"HeadImage: 加载Content URI - {contentUri}");

        // 使用AndroidJavaObject访问ContentResolver
        AndroidJavaObject inputStream = null;
        AndroidJavaObject bitmap = null;

        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");

            // 解析URI
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", contentUri);

            // 打开输入流
            inputStream = contentResolver.Call<AndroidJavaObject>("openInputStream", uri);

            if (inputStream == null)
            {
                Debug.LogError("HeadImage: 无法打开输入流");
                if (defaultTexture != null)
                    displayImage.texture = defaultTexture;
                yield break;
            }

            // 将InputStream转换为byte[]
            AndroidJavaClass streamHelper = new AndroidJavaClass("com.unity3d.player.StreamHelper");
            byte[] imageData = streamHelper.CallStatic<byte[]>("readInputStream", inputStream);

            if (imageData != null && imageData.Length > 0)
            {
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageData))
                {
                    displayImage.texture = texture;
                    // 去掉 SetNativeSize 以使用原先在编辑器里设定的大小
                    // displayImage.SetNativeSize();
                    Debug.Log($"HeadImage: Content URI加载成功 - {texture.width}x{texture.height}");
                }
                else
                {
                    Debug.LogError("HeadImage: 图片解码失败");
                    if (defaultTexture != null)
                        displayImage.texture = defaultTexture;
                }
            }
            else
            {
                Debug.LogError("HeadImage: 读取数据失败");
                if (defaultTexture != null)
                    displayImage.texture = defaultTexture;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HeadImage: Content URI加载失败 - {e.Message}");
            if (defaultTexture != null)
                displayImage.texture = defaultTexture;
        }
        finally
        {
            // 关闭输入流
            if (inputStream != null)
            {
                try { inputStream.Call("close"); } catch { }
            }

            // 回收Bitmap
            if (bitmap != null)
            {
                try { bitmap.Call("recycle"); } catch { }
            }
        }

        yield return null;
    }

    /// <summary>
    /// 清空图片显示
    /// </summary>
    public void ClearImage()
    {
        if (displayImage != null)
        {
            displayImage.texture = null;
        }
    }

    /// <summary>
    /// 恢复默认图片
    /// </summary>
    public void RestoreDefault()
    {
        if (displayImage != null && defaultTexture != null)
        {
            displayImage.texture = defaultTexture;
        }
    }
}
