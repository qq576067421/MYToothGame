using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUI;
using DG.Tweening;

public class Logo : MonoBehaviour
{
    public LUIRawImage m_Logo;

    [SerializeField]
    private float m_FadeDuration = 1f;

    [SerializeField]
    private float m_ShowDuration = 3f;

    [SerializeField]
    private string m_NextSceneName = "startapp_dll";

    private Sequence m_ShowSequence;

    private void Start()
    {
        StartCoroutine(ShowLogo());
    }

    private IEnumerator ShowLogo()
    {
        if (m_Logo == null)
        {
            yield return SceneManager.LoadSceneAsync(m_NextSceneName);
            yield break;
        }

        Color color = m_Logo.color;
        color.a = 0f;
        m_Logo.color = color;

        m_ShowSequence?.Kill();
        m_ShowSequence = DOTween.Sequence();
        m_ShowSequence.Append(m_Logo.DOFade(1f, m_FadeDuration));
        m_ShowSequence.AppendInterval(m_ShowDuration);
        m_ShowSequence.Append(m_Logo.DOFade(0f, m_FadeDuration));

        yield return m_ShowSequence.WaitForCompletion();
        yield return SceneManager.LoadSceneAsync(m_NextSceneName);
    }

    private void OnDestroy()
    {
        m_ShowSequence?.Kill();
        m_ShowSequence = null;
    }
}
