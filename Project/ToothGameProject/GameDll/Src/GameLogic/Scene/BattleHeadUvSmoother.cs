using UnityEngine;

namespace GameHot
{
    public class BattleHeadUvSmoother
    {
        private const float m_PlayerTopOffsetFromRawImageTop = 0.1f;
        private const float m_MinFrameDeltaTime = 1f / 120f;
        private const float m_MaxFrameDeltaTime = 1f / 30f;
        private const float m_MinPositionSmoothTime = 0.08f;
        private const float m_MaxPositionSmoothTime = 0.28f;
        private const float m_MinSizeSmoothTime = 0.16f;
        private const float m_MaxSizeSmoothTime = 0.45f;

        private float m_SmoothingSpeed = 0.05f;
        private Rect m_CachedResult = new Rect(0, 0, 1, 1);
        private Vector2 m_PositionSmoothVelocity;
        private Vector2 m_SizeSmoothVelocity;
        private bool m_IsFirstOriginalRegion = true;
        private bool m_PositionSmoothingEnabled = true;

        public void SetSmoothConfig(float smoothSpeed, float filterSpeed, float deadzone)
        {
            m_SmoothingSpeed = Mathf.Clamp01(smoothSpeed);
        }

        public void SetPositionSmoothingEnabled(bool enabled)
        {
            m_PositionSmoothingEnabled = enabled;
            m_PositionSmoothVelocity = Vector2.zero;
        }

        public void ResetSmoothState()
        {
            m_CachedResult = new Rect(0, 0, 1, 1);
            m_PositionSmoothVelocity = Vector2.zero;
            m_SizeSmoothVelocity = Vector2.zero;
            m_IsFirstOriginalRegion = true;
        }

        public Rect CalculateUvRect(Rect originalRegion, float rawImageAspect, int imageWidth, int imageHeight)
        {
            if (!ValidateParameters(originalRegion, rawImageAspect, imageWidth, imageHeight))
            {
                return new Rect(0, 0, 1, 1);
            }

            float deltaTime = GetClampedDeltaTime();
            float textureAspect = (float)imageWidth / imageHeight;
            float targetUVAspect = rawImageAspect / textureAspect;
            float roiAspect = originalRegion.width / originalRegion.height;

            float newWidth;
            float newHeight;
            if (roiAspect > targetUVAspect)
            {
                newWidth = originalRegion.width;
                newHeight = newWidth / targetUVAspect;
            }
            else
            {
                newHeight = originalRegion.height;
                newWidth = newHeight * targetUVAspect;
            }

            Vector2 center = originalRegion.center;
            Rect targetRect = new Rect(center.x - newWidth * 0.5f, center.y - newHeight * 0.5f, newWidth, newHeight);
            if (targetRect.width > 1f || targetRect.height > 1f)
            {
                float scale = Mathf.Min(1f / targetRect.width, 1f / targetRect.height);
                targetRect = new Rect(targetRect.center.x - targetRect.width * scale * 0.5f,
                    targetRect.center.y - targetRect.height * scale * 0.5f,
                    targetRect.width * scale,
                    targetRect.height * scale);
            }

            targetRect.x = Mathf.Clamp(targetRect.x, 0f, 1f - targetRect.width);
            targetRect.y = GetTopAnchoredY(originalRegion.y, targetRect.height);
            if (m_IsFirstOriginalRegion)
            {
                m_IsFirstOriginalRegion = false;
                m_CachedResult = targetRect;
                m_PositionSmoothVelocity = Vector2.zero;
                m_SizeSmoothVelocity = Vector2.zero;
                return targetRect;
            }

            m_CachedResult = m_PositionSmoothingEnabled
                ? SmoothTransition(m_CachedResult, targetRect, deltaTime)
                : SmoothSizeOnly(m_CachedResult, targetRect, originalRegion, deltaTime);
            return m_CachedResult;
        }

        private Rect SmoothTransition(Rect current, Rect target, float deltaTime)
        {
            Vector2 position = Vector2.SmoothDamp(new Vector2(current.x, current.y), new Vector2(target.x, target.y),
                ref m_PositionSmoothVelocity, SpeedToSmoothTime(m_SmoothingSpeed, m_MinPositionSmoothTime, m_MaxPositionSmoothTime),
                Mathf.Infinity, deltaTime);
            bool isZoomingOut = target.width > current.width || target.height > current.height;
            float scaleSpeed = isZoomingOut ? Mathf.Clamp01(m_SmoothingSpeed * 0.45f) : Mathf.Clamp01(m_SmoothingSpeed * 0.25f);
            Vector2 size = Vector2.SmoothDamp(new Vector2(current.width, current.height), new Vector2(target.width, target.height),
                ref m_SizeSmoothVelocity, SpeedToSmoothTime(scaleSpeed, m_MinSizeSmoothTime, m_MaxSizeSmoothTime),
                Mathf.Infinity, deltaTime);
            return ClampRegion(new Rect(position.x, position.y, size.x, size.y));
        }

        private Rect SmoothSizeOnly(Rect current, Rect target, Rect originalRegion, float deltaTime)
        {
            m_PositionSmoothVelocity = Vector2.zero;
            bool isZoomingOut = target.width > current.width || target.height > current.height;
            float scaleSpeed = isZoomingOut ? Mathf.Clamp01(m_SmoothingSpeed * 0.45f) : Mathf.Clamp01(m_SmoothingSpeed * 0.25f);
            Vector2 size = Vector2.SmoothDamp(new Vector2(current.width, current.height), new Vector2(target.width, target.height),
                ref m_SizeSmoothVelocity, SpeedToSmoothTime(scaleSpeed, m_MinSizeSmoothTime, m_MaxSizeSmoothTime),
                Mathf.Infinity, deltaTime);
            return ClampRegion(new Rect(originalRegion.center.x - size.x * 0.5f,
                GetTopAnchoredY(originalRegion.y, size.y), size.x, size.y));
        }

        private static bool ValidateParameters(Rect originalRegion, float rawImageAspect, int imageWidth, int imageHeight)
        {
            return originalRegion.width > 0f && originalRegion.height > 0f &&
                   rawImageAspect > 0f && !float.IsInfinity(rawImageAspect) && !float.IsNaN(rawImageAspect) &&
                   imageWidth > 0 && imageHeight > 0;
        }

        private static float SpeedToSmoothTime(float speed, float minSmoothTime, float maxSmoothTime)
        {
            return Mathf.Lerp(maxSmoothTime, minSmoothTime, Mathf.Clamp01(speed));
        }

        private static Rect ClampRegion(Rect region)
        {
            float width = Mathf.Clamp(region.width, 0.001f, 1f);
            float height = Mathf.Clamp(region.height, 0.001f, 1f);
            return new Rect(Mathf.Clamp(region.x, 0f, 1f - width), Mathf.Clamp(region.y, 0f, 1f - height), width, height);
        }

        private static float GetClampedDeltaTime()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f || float.IsInfinity(deltaTime) || float.IsNaN(deltaTime))
            {
                deltaTime = 1f / 60f;
            }

            return Mathf.Clamp(deltaTime, m_MinFrameDeltaTime, m_MaxFrameDeltaTime);
        }

        private static float GetTopAnchoredY(float playerTop, float uvHeight)
        {
            return Mathf.Clamp(playerTop - uvHeight * m_PlayerTopOffsetFromRawImageTop, 0f, 1f - uvHeight);
        }
    }
}
