using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Use this script to fade out/in your canvas group
/// </summary>
[DisallowMultipleComponent]
public class FadeCanvas: MonoBehaviour {

    public CanvasGroup canvasGroup;

    public bool DeactivateGameObject = true;

    public float FadeTime = 0.2f;

    private SlideShowScrollViewPro_Scroll m_Scroll;

    private void Awake()
    {
        if (m_Scroll == null)
        {
            m_Scroll = this.GetComponentInParent<SlideShowScrollViewPro_Scroll>();
        }
    }
    // Fix rotation
    void Start()
    {
        if (m_Scroll == null)
        {
            m_Scroll = this.GetComponentInParent<SlideShowScrollViewPro_Scroll>();
        }
    }
    /// <summary>
    /// Fade Out
    /// </summary>
    public void FadeOut()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutNow());
    }

    /// <summary>
    /// Fade In
    /// </summary>
    public void FadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(FadeInNow());
    }

    IEnumerator FadeOutNow()
    {

        canvasGroup.alpha = 1;

        float t = 0;

        while (t <= FadeTime) {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / FadeTime);
            t += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }

        canvasGroup.alpha = 0;

        if (DeactivateGameObject) {
            canvasGroup.gameObject.SetActive(false);
        }

    }

    IEnumerator FadeInNow()
    {
        m_Scroll.ShowVisibleActiveButtons();

        canvasGroup.alpha = 0;
        canvasGroup.gameObject.SetActive(true);

        float t = 0;

        while (t <= FadeTime) {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / FadeTime);
            t += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }

        canvasGroup.alpha = 1;

        m_Scroll.ShowVisibleActiveButtons();
    }
}
