using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TweenSlider : MonoBehaviour
{
    public Slider m_Slider;
    public float from;
    public float to;
    public float m_Duration = 0.2f;

    private float m_Elapsed;
    private bool m_IsPlaying;

    private void Awake()
    {
        if (m_Slider == null)
        {
            m_Slider = GetComponent<Slider>();
        }
    }

    public void PlayForward()
    {
        if (m_Slider == null)
        {
            m_Slider = GetComponent<Slider>();
        }
        if (m_Slider == null)
        {
            return;
        }

        m_Elapsed = 0f;
        m_IsPlaying = true;

        if (m_Duration <= 0f)
        {
            m_Slider.value = to;
            m_IsPlaying = false;
            return;
        }

        m_Slider.value = from;
    }

    private void Update()
    {
        if (!m_IsPlaying || m_Slider == null)
        {
            return;
        }

        if (m_Duration <= 0f)
        {
            m_Slider.value = to;
            m_IsPlaying = false;
            return;
        }

        m_Elapsed += Time.deltaTime;
        var t = Mathf.Clamp01(m_Elapsed / m_Duration);
        m_Slider.value = Mathf.Lerp(from, to, t);

        if (t >= 1f)
        {
            m_IsPlaying = false;
        }
    }
}
