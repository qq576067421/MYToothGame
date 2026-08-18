using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace YouDooSDK.UI
{
public class UITip : MonoBehaviour
{
    [Header("组件引用")]
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    public Text contentText;

    [Header("动画设置")]
    public float fadeDuration = 0.2f;
    public float showDuration = 2f; // 自动隐藏时间，0表示不自动隐藏

    private Sequence showSequence;
    private bool isAutoHide = false;

    public void Initialize()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show(string content)
    {
        // 如果有之前残留的隐藏动画，需要立刻杀掉并清理Invoke
        showSequence?.Kill();
        CancelInvoke(nameof(AutoHide));

        // 重置初始状态，防止之前动画残留的错位和消失状态
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * 0.8f;
        
        // 设置内容
        contentText.text = content;
        gameObject.SetActive(true);

        // 更新位置
        UpdatePosition();

        // 播放显示动画
        PlayShowAnimation();

        // 设置自动隐藏
        if (showDuration > 0)
        {
            isAutoHide = true;
            Invoke(nameof(AutoHide), showDuration);
        }
        else
        {
            isAutoHide = false;
        }
    }

    /// <summary>
    /// 当重复刷出相同的提示时，不需要重新跑出现动画，只刷新倒计时即可
    /// </summary>
    public void RefreshShow()
    {
        // 如果正在播放隐藏动画，先打断恢复成完全显示状态
        showSequence?.Kill();
        canvasGroup.alpha = 1f;
        rectTransform.localScale = Vector3.one;

        if (showDuration > 0)
        {
            isAutoHide = true;
            CancelInvoke(nameof(AutoHide));
            Invoke(nameof(AutoHide), showDuration);
        }
    }

    void UpdatePosition()
    {
        // 将世界坐标转换为UI坐标
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, Vector3.zero);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform, screenPoint, null, out Vector2 localPoint);

        rectTransform.anchoredPosition = localPoint;

        // 确保Tips不会超出屏幕边界
        EnsureInScreenBounds();
    }

    void EnsureInScreenBounds()
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        RectTransform canvasRect = rectTransform.parent as RectTransform;
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // 简单的边界检查，可以根据需要完善
        Vector2 pos = rectTransform.anchoredPosition;

        if (pos.x + rectTransform.rect.width > canvasWidth)
            pos.x = canvasWidth - rectTransform.rect.width;
        if (pos.x < 0)
            pos.x = 0;
        if (pos.y + rectTransform.rect.height > canvasHeight)
            pos.y = canvasHeight - rectTransform.rect.height;
        if (pos.y < 0)
            pos.y = 0;

        rectTransform.anchoredPosition = pos;
    }

    void PlayShowAnimation()
    {
        /*showSequence?.Kill();

        showSequence = DOTween.Sequence();
        showSequence.Append(canvasGroup.DOFade(1f, fadeDuration));
        showSequence.Join(rectTransform.DOScale(1f, fadeDuration).From(0.8f).SetEase(Ease.OutBack));
        showSequence.OnComplete(() => canvasGroup.blocksRaycasts = true);*/
    }

    public void Hide()
    {
        /*showSequence?.Kill();
        CancelInvoke(nameof(AutoHide));

        canvasGroup.blocksRaycasts = false;

        Sequence hideSequence = DOTween.Sequence();
        hideSequence.Append(canvasGroup.DOFade(0f, fadeDuration / 2f));
        hideSequence.Join(rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + 100f, fadeDuration / 2f).SetEase(Ease.OutQuad));
        // 也可以加入轻微的缩放效果让动画更生动
        hideSequence.Join(rectTransform.DOScale(0.8f, fadeDuration / 2f));
        hideSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });*/
    }

    void AutoHide()
    {
        if (isAutoHide)
        {
            TipsManager.Instance.HideTip(this);
        }
    }
}
}
