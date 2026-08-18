using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 相机纹理显示视图（简化版）
/// 每个实例独立管理自己的纹理和显示区域
/// </summary>
[RequireComponent(typeof(RawImage))]
public class CameraTextureView : MonoBehaviour
{
    private RawImage _rawImage;
    private RectTransform _rectTransform;
    private CameraTextureViewSmoothCalculation _smoothCalculator;


    private float _aspectRatio = 1.0f;

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rectTransform = GetComponent<RectTransform>();
        if (_rawImage == null || _rectTransform == null)
        {
            enabled = false;
            return;
        }

        UpdateAspectRatio();
        _smoothCalculator = new CameraTextureViewSmoothCalculation
        {
            SmoothingSpeed = 0.05f
        };
    }

    void OnDestroy()
    {
        Clear();
    }

    public void SetUnityTexture(Texture tex)
    {
        if (_rawImage == null) return;
        if (_rawImage.texture == tex) return;
        _rawImage.texture = tex;
    }

    // ========== 核心方法 ==========
    /// <summary>
    /// 显示纹理的指定区域
    /// </summary>
    /// <param name="texture">外部传入的纹理</param>
    /// <param name="normalizedRegion">归一化显示区域(0-1)</param>
    public void ChangeImageRect(Rect normalizedRegion)
    {
        if (_rawImage == null)
        {
            return;
        }

        Texture texture = _rawImage.texture;
        if (texture == null)
        {
            return;
        }

        int imageWidth = texture.width;
        int imageHeight = texture.height;

        if (_smoothCalculator != null)
        {
            Rect calculatedUvRect = _smoothCalculator.CalculateUvRect(
                normalizedRegion,
                _aspectRatio,
                imageWidth,
                imageHeight
            );

            _rawImage.uvRect = calculatedUvRect;
        }
        else
        {
            _rawImage.uvRect = normalizedRegion;
        }
    }

    /// <summary>
    /// �����ǰ��ʾ������
    /// </summary>
    public void Clear()
    {
        if (_rawImage != null)
        {
            _rawImage.texture = null;
            _rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    /// <summary>
    /// 获取当前显示的UV矩形
    /// </summary>
    public Rect GetCurrentUVRect()
    {
        if (_rawImage != null)
        {
            return _rawImage.uvRect;
        }
        return new Rect(0, 0, 1, 1);
    }

    private void UpdateAspectRatio()
    {
        if (_rectTransform == null)
        {
            _aspectRatio = 1.0f;
            return;
        }

        Rect rect = _rectTransform.rect;
        _aspectRatio = rect.height > 0.0f ? rect.width / rect.height : 1.0f;
    }
}
