using UnityEngine;

namespace GameDll
{
    // 在主工程消费端直接绘制当前帧骨骼，便于 Development 包和编辑器联调时核对输入数据。
    public sealed class BoneDebugSkeletonOverlay : MonoBehaviour
    {
        private const float m_MinJointScore = 0.05f;
        private const float m_DisplayTopPaddingPixels = 24f;
        private const float m_DisplayHorizontalPaddingRatio = 0.02f;
        private const float m_DisplaySlotGapRatio = 0.008f;
        private const float m_DisplayHeightRatio = 0.42f;
        private const float m_DisplaySlotAspect = 1.85f;
        private const float m_SlotRectMinSize = 48f;
        private const float m_PointSizeRatio = 0.032f;
        private const float m_LineWidthRatio = 0.010f;
        private const float m_BorderWidthRatio = 0.006f;
        private const float m_MinPointSizePixels = 6f;
        private const float m_MaxPointSizePixels = 16f;
        private const float m_MinLineWidthPixels = 2f;
        private const float m_MaxLineWidthPixels = 6f;
        private const float m_MinBorderWidthPixels = 1f;
        private const float m_MaxBorderWidthPixels = 3f;

        private static readonly Color m_LineColor = new Color(0.30f, 1.00f, 0.30f, 0.95f);
        private static readonly Color m_PointColor = new Color(1.00f, 0.20f, 0.20f, 0.98f);
        private static readonly Color m_BorderColor = new Color(1.00f, 1.00f, 1.00f, 0.18f);

        private readonly Vector2[] m_ScreenPoints =
            new Vector2[(int)YouDooSDKConstants.KeyPointIndex.KEYPOINT_COUNT];
        private readonly bool[] m_HasScreenPoints =
            new bool[(int)YouDooSDKConstants.KeyPointIndex.KEYPOINT_COUNT];

        private IBoneFrameSource m_FrameSource;
        private Texture2D m_WhiteTexture;
        private Texture2D m_PointTexture;

        public void Bind(IBoneFrameSource frameSource)
        {
            m_FrameSource = frameSource;
        }

        private void OnGUI()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (!ReadShouldDrawOverlay() || m_FrameSource == null)
            {
                return;
            }

            BoneFrameData frameData = m_FrameSource.ReadLatestFrameData();
            if (frameData == null || !frameData.m_HasFrameData)
            {
                return;
            }

            EnsureTextures();
            Rect displayArea = BuildDisplayArea();
            for (int slotIndex = 0; slotIndex < BoneSlotLayout.m_SlotCount; slotIndex++)
            {
                Rect slotRect = BuildSlotRect(displayArea, slotIndex);
                if (slotRect.width < m_SlotRectMinSize || slotRect.height < m_SlotRectMinSize)
                {
                    continue;
                }

                DrawRectOutline(
                    slotRect,
                    Mathf.Clamp(slotRect.width * m_BorderWidthRatio, m_MinBorderWidthPixels, m_MaxBorderWidthPixels),
                    m_BorderColor);

                BonePersonData person = slotIndex < frameData.m_Persons.Count ? frameData.m_Persons[slotIndex] : null;
                if (!CollectScreenPoints(person, slotIndex, slotRect))
                {
                    continue;
                }

                DrawSkeleton(slotRect);
            }
        }

        private void OnDestroy()
        {
            DestroyTexture(ref m_WhiteTexture);
            DestroyTexture(ref m_PointTexture);
        }

        // 战斗期间先看统一运行态开关，只有未被战斗屏蔽时才继续读取 GMTools 的显示配置。
        private static bool ReadShouldDrawOverlay()
        {
            return BoneRemoteDebugEditorConfig.ReadShouldDrawBattleSkeletonOverlay();
        }

        // 水平四槽位显示，尺寸根据当前分辨率自动缩放。
        private static Rect BuildDisplayArea()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float horizontalPadding = Mathf.Max(12f, screenWidth * m_DisplayHorizontalPaddingRatio);
            float topPadding = Mathf.Max(12f, m_DisplayTopPaddingPixels);
            float slotGap = Mathf.Max(6f, screenWidth * m_DisplaySlotGapRatio);
            float totalGapWidth = slotGap * (BoneSlotLayout.m_SlotCount - 1);
            float slotWidth = (screenWidth - horizontalPadding * 2f - totalGapWidth) / BoneSlotLayout.m_SlotCount;
            slotWidth = Mathf.Max(m_SlotRectMinSize, slotWidth);
            float slotHeight = Mathf.Min(screenHeight * m_DisplayHeightRatio, slotWidth * m_DisplaySlotAspect);
            float totalWidth = slotWidth * BoneSlotLayout.m_SlotCount + totalGapWidth;
            return new Rect(horizontalPadding, topPadding, totalWidth, slotHeight);
        }

        private static Rect BuildSlotRect(Rect displayArea, int slotIndex)
        {
            float screenWidth = Screen.width;
            float slotGap = Mathf.Max(6f, screenWidth * m_DisplaySlotGapRatio);
            float slotWidth = (displayArea.width - slotGap * (BoneSlotLayout.m_SlotCount - 1)) / BoneSlotLayout.m_SlotCount;
            float x = displayArea.x + slotIndex * (slotWidth + slotGap);
            return new Rect(x, displayArea.y, slotWidth, displayArea.height);
        }

        private bool CollectScreenPoints(BonePersonData person, int slotIndex, Rect slotRect)
        {
            ClearScreenPoints();
            if (person == null)
            {
                return false;
            }

            Rect sourceUvRect = BoneSlotLayout.ReadSourceUvRect(slotIndex);
            bool hasAnyPoint = false;
            for (int jointIndex = 0; jointIndex < m_ScreenPoints.Length; jointIndex++)
            {
                Vector2 sdkPoint;
                if (!BoneFrameJointReader.TryReadBodyJoint(person, jointIndex, m_MinJointScore, out sdkPoint))
                {
                    continue;
                }

                if (!TryMapSdkPointToScreenPoint(sdkPoint, sourceUvRect, slotRect, out Vector2 screenPoint))
                {
                    continue;
                }

                m_ScreenPoints[jointIndex] = screenPoint;
                m_HasScreenPoints[jointIndex] = true;
                hasAnyPoint = true;
            }

            return hasAnyPoint;
        }

        // 当前帧数据仍然是整张采集图上的归一化坐标，这里按对应槽位源分区换算到调试面板里。
        private static bool TryMapSdkPointToScreenPoint(
            Vector2 sdkPoint,
            Rect sourceUvRect,
            Rect slotRect,
            out Vector2 screenPoint)
        {
            if (sourceUvRect.width <= 0.0001f || sourceUvRect.height <= 0.0001f)
            {
                screenPoint = Vector2.zero;
                return false;
            }

            float u = (sdkPoint.x - sourceUvRect.xMin) / sourceUvRect.width;
            float v = (sdkPoint.y - sourceUvRect.yMin) / sourceUvRect.height;
            if (u < -0.25f || u > 1.25f || v < -0.25f || v > 1.25f)
            {
                screenPoint = Vector2.zero;
                return false;
            }

            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);
            screenPoint = new Vector2(
                slotRect.xMin + slotRect.width * u,
                slotRect.yMin + slotRect.height * v);
            return true;
        }

        private void DrawSkeleton(Rect slotRect)
        {
            float pointSize = Mathf.Clamp(slotRect.width * m_PointSizeRatio, m_MinPointSizePixels, m_MaxPointSizePixels);
            float lineWidth = Mathf.Clamp(slotRect.width * m_LineWidthRatio, m_MinLineWidthPixels, m_MaxLineWidthPixels);

            for (int i = 0; i < YouDooSDKConstants.defaultSkeletonConnections.Length; i++)
            {
                var connection = YouDooSDKConstants.defaultSkeletonConnections[i];
                if (!ReadHasPoint(connection.from) || !ReadHasPoint(connection.to))
                {
                    continue;
                }

                DrawLine(m_ScreenPoints[connection.from], m_ScreenPoints[connection.to], lineWidth, m_LineColor);
            }

            for (int jointIndex = 0; jointIndex < m_ScreenPoints.Length; jointIndex++)
            {
                if (!m_HasScreenPoints[jointIndex])
                {
                    continue;
                }

                DrawPoint(m_ScreenPoints[jointIndex], pointSize, m_PointColor);
            }
        }

        private bool ReadHasPoint(int jointIndex)
        {
            return jointIndex >= 0 && jointIndex < m_HasScreenPoints.Length && m_HasScreenPoints[jointIndex];
        }

        private void ClearScreenPoints()
        {
            for (int i = 0; i < m_HasScreenPoints.Length; i++)
            {
                m_HasScreenPoints[i] = false;
            }
        }

        private void EnsureTextures()
        {
            if (m_WhiteTexture == null)
            {
                m_WhiteTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                m_WhiteTexture.hideFlags = HideFlags.HideAndDontSave;
                m_WhiteTexture.SetPixel(0, 0, Color.white);
                m_WhiteTexture.Apply();
            }

            if (m_PointTexture == null)
            {
                const int pointTextureSize = 32;
                m_PointTexture = new Texture2D(pointTextureSize, pointTextureSize, TextureFormat.ARGB32, false);
                m_PointTexture.hideFlags = HideFlags.HideAndDontSave;
                float radius = (pointTextureSize - 1) * 0.5f;
                Vector2 center = new Vector2(radius, radius);
                for (int y = 0; y < pointTextureSize; y++)
                {
                    for (int x = 0; x < pointTextureSize; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        Color pixelColor = distance <= radius ? Color.white : Color.clear;
                        m_PointTexture.SetPixel(x, y, pixelColor);
                    }
                }

                m_PointTexture.Apply();
            }
        }

        private void DrawRectOutline(Rect rect, float thickness, Color color)
        {
            DrawFilledRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            DrawFilledRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        private void DrawPoint(Vector2 center, float size, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            float halfSize = size * 0.5f;
            GUI.DrawTexture(
                new Rect(center.x - halfSize, center.y - halfSize, size, size),
                m_PointTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = oldColor;
        }

        private void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length <= 0.0001f)
            {
                return;
            }

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(
                new Rect(start.x, start.y - thickness * 0.5f, length, thickness),
                m_WhiteTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawFilledRect(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, m_WhiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }

            texture = null;
        }
    }
}
