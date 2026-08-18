using UnityEngine;

namespace GameHot
{
    public enum BattleHeadFollowMode
    {
        ContinuousSmooth,
        IndependentUICamera
    }

    /// <summary>
    /// 战斗头像的输入跟踪器。骨骼测量帧与游戏渲染帧的频率不同，
    /// 因此只在新骨骼帧到达时更新测量，再由显示层在渲染帧连续平滑追赶。
    /// </summary>
    internal sealed class BattleHeadCameraTracker
    {
        private const float m_DefaultMeasurementDeltaTime = 1f / 30f;
        private const float m_MinMeasurementDeltaTime = 1f / 120f;
        private const float m_MaxMeasurementDeltaTime = 0.2f;
        private const float m_MaxRenderDeltaTime = 1f / 20f;
        private const float m_VerticalSafeZoneRatio = 0.75f;
        private const float m_DerivativeCutoff = 1.0f;
        private const float m_OutlierDistance = 0.12f;
        private const float m_OutlierConfirmDistance = 0.06f;
        private const float m_ZoomOutThreshold = 0.04f;
        private const float m_ZoomInThreshold = 0.06f;
        private const int m_ZoomOutConfirmFrames = 3;
        private const int m_ZoomInConfirmFrames = 6;
        private const float m_ZoomOutFollowRatio = 0.35f;
        private const float m_ZoomInFollowRatio = 0.12f;
        private const float m_TargetLostTimeout = 0.6f;
        private const float m_MinRectSize = 0.01f;
        private const float m_IndependentStableDuration = 0.2f;
        private const float m_IndependentInnerHorizontalRatio = 0.08f;
        private const float m_IndependentInnerVerticalRatio = 0.06f;
        private const float m_IndependentOuterHorizontalRatio = 0.18f;
        private const float m_IndependentOuterVerticalRatio = 0.14f;
        private const float m_IndependentStabilityRangeRatio = 0.025f;
        // 数值越大跟随越及时，同时也会更明显地反映骨骼检测位置变化。
        private const float m_IndependentFollowAngularFrequency = 14.0f;

        private float m_FilterSpeed = 0.9f;
        private float m_Deadzone = 0.004f;
        private BattleHeadFollowMode m_FollowMode = BattleHeadFollowMode.IndependentUICamera;
        private int m_PersonId = int.MinValue;
        private int m_LastFrameSerial = -1;
        private long m_LastFrameTimeMs;
        private int m_LastRenderFrame = -1;
        private Vector2 m_LastRawCenter;
        private Vector2 m_FilteredMeasurementCenter;
        private Vector2 m_TrackingCenter;
        private Vector2 m_FilteredVelocity;
        private Vector2 m_TrackedSize;
        private Vector2 m_PendingOutlierCenter;
        private Vector2 m_UiCameraCenter;
        private Vector2 m_UiCameraTargetCenter;
        private Vector2 m_UiCameraVelocity;
        private Vector2 m_StabilityAnchorCenter;
        private int m_SizeChangeDirection;
        private int m_SizeChangeFrameCount;
        private float m_TimeSinceValidMeasurement;
        private float m_StableDuration;
        private bool m_HasPendingOutlier;
        private bool m_HasState;
        private bool m_UiCameraTargetFrozen;
        private bool m_HasFollowModeConfigured;

        public void SetFollowMode(BattleHeadFollowMode followMode)
        {
            if (followMode != BattleHeadFollowMode.ContinuousSmooth &&
                followMode != BattleHeadFollowMode.IndependentUICamera)
            {
                followMode = BattleHeadFollowMode.IndependentUICamera;
            }

            if (m_HasFollowModeConfigured && m_FollowMode == followMode)
            {
                return;
            }

            BattleHeadFollowMode previousFollowMode = m_FollowMode;
            m_FollowMode = followMode;
            m_HasFollowModeConfigured = true;
            LogFollowMode();
            if (!m_HasState)
            {
                return;
            }

            if (m_FollowMode == BattleHeadFollowMode.IndependentUICamera)
            {
                InitializeIndependentCamera(m_TrackingCenter);
            }
            else
            {
                if (previousFollowMode == BattleHeadFollowMode.IndependentUICamera)
                {
                    m_TrackingCenter = m_UiCameraCenter;
                }

                ClearIndependentCameraMotion();
            }
        }

        private void LogFollowMode()
        {
            if (m_FollowMode == BattleHeadFollowMode.IndependentUICamera)
            {
                Debug.Log("[战斗头像跟随] 当前模式=IndependentUICamera，速度规则=单一临界阻尼线性方程，无分段限速和减速公式切换，位置与速度连续，位置更新=每个游戏渲染帧");
                return;
            }

            Debug.Log("[战斗头像跟随] 当前模式=ContinuousSmooth，速度规则=SmoothDamp非线性平滑，目标更新=骨骼帧，位置更新=每个游戏渲染帧");
        }

        public void SetConfig(float filterSpeed, float deadzone)
        {
            m_FilterSpeed = Mathf.Clamp01(filterSpeed);
            m_Deadzone = Mathf.Clamp01(deadzone);
        }

        public void Reset()
        {
            m_PersonId = int.MinValue;
            m_LastFrameSerial = -1;
            m_LastFrameTimeMs = 0;
            m_LastRenderFrame = -1;
            m_LastRawCenter = Vector2.zero;
            m_FilteredMeasurementCenter = Vector2.zero;
            m_TrackingCenter = Vector2.zero;
            m_FilteredVelocity = Vector2.zero;
            m_TrackedSize = Vector2.zero;
            m_PendingOutlierCenter = Vector2.zero;
            m_UiCameraCenter = Vector2.zero;
            m_UiCameraTargetCenter = Vector2.zero;
            m_UiCameraVelocity = Vector2.zero;
            m_StabilityAnchorCenter = Vector2.zero;
            m_SizeChangeDirection = 0;
            m_SizeChangeFrameCount = 0;
            m_TimeSinceValidMeasurement = 0f;
            m_StableDuration = 0f;
            m_HasPendingOutlier = false;
            m_HasState = false;
            m_UiCameraTargetFrozen = false;
        }

        public void PushMeasurement(int personId, int frameSerial, long frameTimeMs, Rect measuredRect)
        {
            if (frameSerial == m_LastFrameSerial)
            {
                return;
            }

            float measurementDeltaTime = CalculateMeasurementDeltaTime(frameTimeMs);
            m_LastFrameSerial = frameSerial;

            Rect clampedRect = ClampRect(measuredRect);
            if (!IsValidRect(clampedRect))
            {
                return;
            }

            if (!m_HasState || m_PersonId != personId)
            {
                Initialize(personId, clampedRect);
                m_LastFrameTimeMs = frameTimeMs;
                return;
            }

            Vector2 rawCenter = clampedRect.center;
            if (!AcceptMeasurementCenter(rawCenter))
            {
                return;
            }

            Vector2 rawVelocity = (rawCenter - m_LastRawCenter) / measurementDeltaTime;

            float derivativeAlpha = CalculateLowPassAlpha(m_DerivativeCutoff, measurementDeltaTime);
            if (Vector2.Dot(m_FilteredVelocity, rawVelocity) < 0f)
            {
                derivativeAlpha = Mathf.Max(derivativeAlpha, 0.75f);
            }

            m_FilteredVelocity = Vector2.Lerp(m_FilteredVelocity, rawVelocity, derivativeAlpha);
            float positionCutoff = CalculatePositionCutoff(m_FilteredVelocity.magnitude);
            float positionAlpha = CalculateLowPassAlpha(positionCutoff, measurementDeltaTime);
            m_FilteredMeasurementCenter = Vector2.Lerp(m_FilteredMeasurementCenter, rawCenter, positionAlpha);
            UpdateTrackedSize(clampedRect.size);
            if (m_FollowMode == BattleHeadFollowMode.IndependentUICamera)
            {
                UpdateIndependentCameraTarget(m_FilteredMeasurementCenter, measurementDeltaTime);
            }
            else
            {
                MoveTrackingCenterOutsideSafeZone(m_FilteredMeasurementCenter);
            }

            m_LastRawCenter = rawCenter;
            m_LastFrameTimeMs = frameTimeMs;
            m_TimeSinceValidMeasurement = 0f;
        }

        public void MarkMeasurementMissing(int frameSerial, long frameTimeMs)
        {
            if (frameSerial == m_LastFrameSerial)
            {
                return;
            }

            m_LastFrameSerial = frameSerial;
            m_HasPendingOutlier = false;
        }

        public bool TryGetDisplayRect(float renderDeltaTime, int renderFrame, out Rect rect)
        {
            rect = default;
            if (!m_HasState)
            {
                return false;
            }

            if (m_LastRenderFrame != renderFrame)
            {
                m_LastRenderFrame = renderFrame;
                float clampedRenderDeltaTime = Mathf.Clamp(renderDeltaTime, 0f, m_MaxRenderDeltaTime);
                m_TimeSinceValidMeasurement += clampedRenderDeltaTime;
                if (m_FollowMode == BattleHeadFollowMode.IndependentUICamera)
                {
                    UpdateIndependentCameraPosition(clampedRenderDeltaTime);
                }
            }

            if (m_TimeSinceValidMeasurement > m_TargetLostTimeout)
            {
                return false;
            }

            Vector2 displayCenter = m_FollowMode == BattleHeadFollowMode.IndependentUICamera
                ? m_UiCameraCenter
                : m_TrackingCenter;
            Rect displayRect = ClampRect(new Rect(
                displayCenter.x - m_TrackedSize.x * 0.5f,
                displayCenter.y - m_TrackedSize.y * 0.5f,
                m_TrackedSize.x,
                m_TrackedSize.y));
            if (m_FollowMode == BattleHeadFollowMode.IndependentUICamera)
            {
                ApplyIndependentCameraBoundary(displayRect.center);
            }

            rect = displayRect;
            return IsValidRect(rect);
        }

        private void Initialize(int personId, Rect measuredRect)
        {
            m_PersonId = personId;
            m_LastRawCenter = measuredRect.center;
            m_FilteredMeasurementCenter = measuredRect.center;
            m_TrackingCenter = measuredRect.center;
            m_FilteredVelocity = Vector2.zero;
            m_TrackedSize = measuredRect.size;
            m_SizeChangeDirection = 0;
            m_SizeChangeFrameCount = 0;
            m_TimeSinceValidMeasurement = 0f;
            m_HasPendingOutlier = false;
            m_HasState = true;
            InitializeIndependentCamera(measuredRect.center);
        }

        private bool AcceptMeasurementCenter(Vector2 rawCenter)
        {
            if (Vector2.Distance(rawCenter, m_LastRawCenter) <= m_OutlierDistance)
            {
                m_HasPendingOutlier = false;
                return true;
            }

            if (m_HasPendingOutlier &&
                Vector2.Distance(rawCenter, m_PendingOutlierCenter) <= m_OutlierConfirmDistance)
            {
                m_HasPendingOutlier = false;
                return true;
            }

            m_PendingOutlierCenter = rawCenter;
            m_HasPendingOutlier = true;
            return false;
        }

        private void MoveTrackingCenterOutsideSafeZone(Vector2 targetCenter)
        {
            float horizontalSafeZone = m_Deadzone;
            float verticalSafeZone = horizontalSafeZone * m_VerticalSafeZoneRatio;
            Vector2 delta = targetCenter - m_TrackingCenter;

            if (horizontalSafeZone <= 0f)
            {
                m_TrackingCenter = targetCenter;
                return;
            }

            if (Mathf.Abs(delta.x) > horizontalSafeZone)
            {
                m_TrackingCenter.x = targetCenter.x - Mathf.Sign(delta.x) * horizontalSafeZone;
            }

            if (Mathf.Abs(delta.y) > verticalSafeZone)
            {
                m_TrackingCenter.y = targetCenter.y - Mathf.Sign(delta.y) * verticalSafeZone;
            }
        }

        private void InitializeIndependentCamera(Vector2 center)
        {
            m_UiCameraCenter = center;
            m_UiCameraTargetCenter = center;
            m_UiCameraVelocity = Vector2.zero;
            m_StabilityAnchorCenter = m_FilteredMeasurementCenter;
            m_StableDuration = 0f;
            m_UiCameraTargetFrozen = false;
        }

        private void ClearIndependentCameraMotion()
        {
            m_UiCameraTargetCenter = m_UiCameraCenter;
            m_UiCameraVelocity = Vector2.zero;
            m_StabilityAnchorCenter = m_FilteredMeasurementCenter;
            m_StableDuration = 0f;
            m_UiCameraTargetFrozen = false;
        }

        private void UpdateIndependentCameraTarget(Vector2 measuredCenter, float measurementDeltaTime)
        {
            Vector2 cameraDelta = measuredCenter - m_UiCameraCenter;
            bool outsideOuterRange = Mathf.Abs(cameraDelta.x) > m_TrackedSize.x * m_IndependentOuterHorizontalRatio ||
                                     Mathf.Abs(cameraDelta.y) > m_TrackedSize.y * m_IndependentOuterVerticalRatio;
            bool isStable = IsWithinStabilityRange(measuredCenter, m_StabilityAnchorCenter);

            if (!isStable || outsideOuterRange)
            {
                m_StabilityAnchorCenter = measuredCenter;
                m_StableDuration = 0f;
                m_UiCameraTargetFrozen = false;
                m_UiCameraTargetCenter = m_UiCameraCenter;
            }
            else
            {
                m_StableDuration += measurementDeltaTime;
            }

            if (m_UiCameraTargetFrozen)
            {
                return;
            }

            // 界面相机只追到构图安全区边缘，不强制把人物重新拉回正中间。
            float horizontalInnerRange = m_TrackedSize.x * m_IndependentInnerHorizontalRatio;
            float verticalInnerRange = m_TrackedSize.y * m_IndependentInnerVerticalRatio;
            Vector2 targetCenter = m_UiCameraTargetCenter;
            if (Mathf.Abs(cameraDelta.x) > horizontalInnerRange)
            {
                targetCenter.x = measuredCenter.x - Mathf.Sign(cameraDelta.x) * horizontalInnerRange;
            }

            if (Mathf.Abs(cameraDelta.y) > verticalInnerRange)
            {
                targetCenter.y = measuredCenter.y - Mathf.Sign(cameraDelta.y) * verticalInnerRange;
            }

            m_UiCameraTargetCenter = ClampCenter(targetCenter, m_TrackedSize);
            if (m_StableDuration >= m_IndependentStableDuration)
            {
                // 当前速度除以固定角频率就是临界阻尼系统的自然停止距离，冻结该目标后不会反向拉回。
                m_UiCameraTargetCenter = ClampCenter(
                    m_UiCameraCenter + m_UiCameraVelocity / m_IndependentFollowAngularFrequency,
                    m_TrackedSize);
                m_UiCameraTargetFrozen = true;
            }
        }

        private void UpdateIndependentCameraPosition(float renderDeltaTime)
        {
            if (renderDeltaTime <= 0f)
            {
                return;
            }

            // 临界阻尼方程使用固定系数精确推进，目标改变时速度仍连续，不存在分段阈值造成的速度跳变。
            float angularFrequency = m_IndependentFollowAngularFrequency;
            float decay = Mathf.Exp(-angularFrequency * renderDeltaTime);
            Vector2 displacement = m_UiCameraCenter - m_UiCameraTargetCenter;
            Vector2 coefficient = m_UiCameraVelocity + angularFrequency * displacement;
            Vector2 nextDisplacement = (displacement + coefficient * renderDeltaTime) * decay;
            Vector2 nextVelocity =
                (m_UiCameraVelocity - angularFrequency * coefficient * renderDeltaTime) * decay;
            m_UiCameraCenter = m_UiCameraTargetCenter + nextDisplacement;
            m_UiCameraVelocity = nextVelocity;
        }

        private bool IsWithinStabilityRange(Vector2 center, Vector2 anchor)
        {
            float horizontalRange = Mathf.Max(m_Deadzone, m_TrackedSize.x * m_IndependentStabilityRangeRatio);
            float verticalRange = Mathf.Max(
                m_Deadzone * m_VerticalSafeZoneRatio,
                m_TrackedSize.y * m_IndependentStabilityRangeRatio);
            Vector2 delta = center - anchor;
            return Mathf.Abs(delta.x) <= horizontalRange && Mathf.Abs(delta.y) <= verticalRange;
        }

        private void ApplyIndependentCameraBoundary(Vector2 clampedCenter)
        {
            if (!Mathf.Approximately(m_UiCameraCenter.x, clampedCenter.x))
            {
                m_UiCameraVelocity.x = 0f;
            }

            if (!Mathf.Approximately(m_UiCameraCenter.y, clampedCenter.y))
            {
                m_UiCameraVelocity.y = 0f;
            }

            m_UiCameraCenter = clampedCenter;
            m_UiCameraTargetCenter = ClampCenter(m_UiCameraTargetCenter, m_TrackedSize);
        }

        private static Vector2 ClampCenter(Vector2 center, Vector2 size)
        {
            float halfWidth = Mathf.Clamp(size.x, m_MinRectSize, 1f) * 0.5f;
            float halfHeight = Mathf.Clamp(size.y, m_MinRectSize, 1f) * 0.5f;
            return new Vector2(
                Mathf.Clamp(center.x, halfWidth, 1f - halfWidth),
                Mathf.Clamp(center.y, halfHeight, 1f - halfHeight));
        }

        private void UpdateTrackedSize(Vector2 measuredSize)
        {
            if (measuredSize.x < m_MinRectSize || measuredSize.y < m_MinRectSize)
            {
                return;
            }

            float widthRatio = measuredSize.x / Mathf.Max(m_TrackedSize.x, m_MinRectSize);
            float heightRatio = measuredSize.y / Mathf.Max(m_TrackedSize.y, m_MinRectSize);
            float largestRatio = Mathf.Max(widthRatio, heightRatio);
            float smallestRatio = Mathf.Min(widthRatio, heightRatio);
            int changeDirection = largestRatio > 1f + m_ZoomOutThreshold
                ? 1
                : smallestRatio < 1f - m_ZoomInThreshold
                    ? -1
                    : 0;

            if (changeDirection == 0)
            {
                m_SizeChangeDirection = 0;
                m_SizeChangeFrameCount = 0;
                return;
            }

            if (m_SizeChangeDirection == changeDirection)
            {
                m_SizeChangeFrameCount++;
            }
            else
            {
                m_SizeChangeDirection = changeDirection;
                m_SizeChangeFrameCount = 1;
            }

            int requiredFrames = changeDirection > 0 ? m_ZoomOutConfirmFrames : m_ZoomInConfirmFrames;
            if (m_SizeChangeFrameCount < requiredFrames)
            {
                return;
            }

            float followRatio = changeDirection > 0 ? m_ZoomOutFollowRatio : m_ZoomInFollowRatio;
            m_TrackedSize = Vector2.Lerp(m_TrackedSize, measuredSize, followRatio);
        }

        private float CalculatePositionCutoff(float speed)
        {
            float minCutoff = Mathf.Lerp(0.7f, 2.5f, m_FilterSpeed);
            float beta = Mathf.Lerp(2f, 8f, m_FilterSpeed);
            return minCutoff + beta * speed;
        }

        private float CalculateMeasurementDeltaTime(long frameTimeMs)
        {
            if (m_LastFrameTimeMs <= 0 || frameTimeMs <= m_LastFrameTimeMs)
            {
                return m_DefaultMeasurementDeltaTime;
            }

            float deltaTime = (frameTimeMs - m_LastFrameTimeMs) * 0.001f;
            return Mathf.Clamp(deltaTime, m_MinMeasurementDeltaTime, m_MaxMeasurementDeltaTime);
        }

        private static float CalculateLowPassAlpha(float cutoff, float deltaTime)
        {
            float timeConstant = 1f / (2f * Mathf.PI * Mathf.Max(cutoff, 0.0001f));
            return Mathf.Clamp01(deltaTime / (timeConstant + deltaTime));
        }

        private static bool IsValidRect(Rect rect)
        {
            return rect.width >= m_MinRectSize && rect.height >= m_MinRectSize;
        }

        private static Rect ClampRect(Rect rect)
        {
            float width = Mathf.Clamp(rect.width, m_MinRectSize, 1f);
            float height = Mathf.Clamp(rect.height, m_MinRectSize, 1f);
            float x = Mathf.Clamp(rect.center.x - width * 0.5f, 0f, 1f - width);
            float y = Mathf.Clamp(rect.center.y - height * 0.5f, 0f, 1f - height);
            return new Rect(x, y, width, height);
        }
    }
}
