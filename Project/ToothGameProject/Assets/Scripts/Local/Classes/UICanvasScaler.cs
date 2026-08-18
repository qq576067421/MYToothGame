using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameDll
{
    public class UICanvasScaler : MonoBehaviour
    {
        public CanvasScaler m_Scaler;

        public bool m_UpdateScaler = false;

        // Landscape UI adapts from the 1920x1080 design resolution up to 2400x1080.
        public Vector2 m_ScreenMin = new Vector2(1920.0f, 1080.0f); // 16:9 = 1.7777
        public Vector2 m_ScreenMax = new Vector2(2400.0f, 1080.0f); // 20:9 = 2.2222

        private void Start()
        {
            OnUpdateScaler();
        }

        private void Update()
        {
            if (m_UpdateScaler)
            {
                m_UpdateScaler = false;
                OnUpdateScaler();
            }
        }

        public void OnUpdateScaler()
        {
            float min_value = m_ScreenMin.x / m_ScreenMin.y;
            float max_value = m_ScreenMax.x / m_ScreenMax.y;

            float per = 1.0f * Screen.width / Screen.height;
            if (per < min_value)
            {
                m_Scaler.matchWidthOrHeight = 0f;
            }
            else if (per >= max_value)
            {
                m_Scaler.matchWidthOrHeight = 1.0f;
            }
            else
            {
                float t = (per - min_value) / (max_value - min_value);
                m_Scaler.matchWidthOrHeight = Mathf.Lerp(0, 1.0f, t);
            }
        }

        // Portrait scaling path used by vertical layouts.
        public void OnUpdateScalerV()
        {
            float match = 1.0f;

            // Portrait baseline example: 1080x2340.
            float currentFactor = 1.0f * Screen.width / Screen.height;

            // Shorter portrait screens, for example 2048x2732.
            float maxFactor = 1.0f * 2048 / 2732;
            float minMatch = 0.327f;

            // Taller portrait screens, for example 1080x2340.
            float minFactor = 1.0f * 1080 / 2340f;
            float maxMatch = 1.0f;

            if (currentFactor >= maxFactor)
            {
                match = minMatch;
            }
            // Shorter screens bias toward width matching.
            else if (currentFactor < minFactor)
            {
                match = maxMatch;
            }
            // Interpolate smoothly for aspect ratios in between.
            else
            {
                float t = (currentFactor - minFactor) / (maxFactor - minFactor);
                match = Mathf.Lerp(maxMatch, minMatch, t);
            }

            m_Scaler.matchWidthOrHeight = match;
        }
    }
}
