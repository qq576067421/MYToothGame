namespace UnityUI
{
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(Text))]
    [DisallowMultipleComponent]
    public class UIDotTextBlink : BaseMeshEffect
    {
        private const int DotCount = 3;
        private const int VerticesPerChar = 4;

        [SerializeField]
        private float m_FrameDuration = 0.35f;

        [SerializeField]
        private bool m_UseUnscaledTime = true;

        private Text m_Text;
        private string m_LastText;
        private bool m_HasTrailingDots;
        private int m_VisibleDotCount = DotCount;
        private UIVertex m_Vertex = new UIVertex();

        protected override void Awake()
        {
            base.Awake();
            m_Text = GetComponent<Text>();
            RefreshState();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshState();
            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            SetVerticesDirty();
            base.OnDisable();
        }

        private void Update()
        {
            if (!IsActive())
            {
                return;
            }

            if (m_Text == null)
            {
                m_Text = GetComponent<Text>();
            }

            if (m_Text == null)
            {
                return;
            }

            if (m_LastText != m_Text.text)
            {
                RefreshState();
                SetVerticesDirty();
            }

            if (!m_HasTrailingDots)
            {
                return;
            }

            int visibleDotCount = GetVisibleDotCount();
            if (visibleDotCount != m_VisibleDotCount)
            {
                m_VisibleDotCount = visibleDotCount;
                SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || !m_HasTrailingDots)
            {
                return;
            }

            int dotVertexCount = DotCount * VerticesPerChar;
            if (vh.currentVertCount < dotVertexCount)
            {
                return;
            }

            int startVertexIndex = vh.currentVertCount - dotVertexCount;
            for (int dotIndex = 0; dotIndex < DotCount; dotIndex++)
            {
                bool isVisible = dotIndex < m_VisibleDotCount;
                int charStartIndex = startVertexIndex + dotIndex * VerticesPerChar;
                for (int i = 0; i < VerticesPerChar; i++)
                {
                    int vertexIndex = charStartIndex + i;
                    vh.PopulateUIVertex(ref m_Vertex, vertexIndex);
                    Color32 color = m_Vertex.color;
                    if (!isVisible)
                    {
                        color.a = 0;
                    }
                    m_Vertex.color = color;
                    vh.SetUIVertex(m_Vertex, vertexIndex);
                }
            }
        }

        private void RefreshState()
        {
            m_LastText = m_Text != null ? m_Text.text : string.Empty;
            m_HasTrailingDots = EndsWithThreeVisibleDots(m_LastText);
            m_VisibleDotCount = m_HasTrailingDots ? GetVisibleDotCount() : DotCount;
        }

        private int GetVisibleDotCount()
        {
            float duration = Mathf.Max(0.05f, m_FrameDuration);
            float time = m_UseUnscaledTime ? Time.unscaledTime : Time.time;
            return Mathf.FloorToInt(time / duration) % DotCount + 1;
        }

        private void SetVerticesDirty()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }

        private static bool EndsWithThreeVisibleDots(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            int dotCount = 0;
            bool insideTag = false;

            for (int i = content.Length - 1; i >= 0; i--)
            {
                char c = content[i];
                if (insideTag)
                {
                    if (c == '<')
                    {
                        insideTag = false;
                    }
                    continue;
                }

                if (c == '>')
                {
                    insideTag = true;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                if (c == '.')
                {
                    if (dotCount >= DotCount)
                    {
                        return false;
                    }

                    dotCount++;
                    continue;
                }

                return dotCount == DotCount;
            }

            return dotCount == DotCount;
        }
    }
}
