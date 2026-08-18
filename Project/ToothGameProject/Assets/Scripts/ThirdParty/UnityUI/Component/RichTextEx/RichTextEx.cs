using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityUI
{
    /// <summary>
    /// RichTextEx - UGUI图文混排组件
    /// 支持在文本中嵌入图片、超链接、下划线、删除线、富文本样式、中文禁则排版、英文单词不断行。
    /// 
    /// 用法：
    ///   richText.SetText("获得了<img=texture_set/item:gold_icon,24,24>x100");
    /// </summary>
    [AddComponentMenu("UI/RichTextEx")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class RichTextEx : MaskableGraphic, IPointerClickHandler
    {
        // ====== 序列化字段 ======
        [SerializeField] private Font m_Font;
        [SerializeField] private int m_FontSize = 24;
        [SerializeField] private float m_LineSpacing = 1.2f;
        [SerializeField] private TextAnchor m_Alignment = TextAnchor.UpperLeft;
        [SerializeField] [TextArea(3, 10)] private string m_Text = "";
        [SerializeField] private FontStyle m_FontStyle = FontStyle.Normal;

        // ====== 事件 ======
        /// <summary>超链接点击回调: url</summary>
        public event Action<string> OnHyperlinkClick;
        /// <summary>图片点击回调: ImageElementInfo</summary>
        public event Action<ImageElementInfo> OnImageClick;

        // ====== 内部状态 ======
        private ParseResult m_ParseResult;
        private InlineImageManager m_ImageManager;
        private readonly List<UIVertex> m_Vertices = new List<UIVertex>();
        private readonly List<LayoutCharInfo> m_LayoutChars = new List<LayoutCharInfo>();
        private bool m_Dirty = true;
        private Font m_CachedFont;

        // 默认字体路径
        private const string DEFAULT_FONT_PATH = "Assets/art/out/font/notosanshans-thin.ttf";

        // 中文行首禁则字符（不允许出现在行首的字符）
        private static readonly HashSet<char> s_NoStartChars = new HashSet<char>
        {
            '，', ',', '。', '.', '！', '!', '？', '?', '：', ':', '；', ';',
            '）', ')', '】', ']', '」', '』', '》', '〉', '、', '"', '\'',
            '…', '—', '～', '·', 'ー',
        };

        // 中文行尾禁则字符（不允许出现在行尾的字符）
        private static readonly HashSet<char> s_NoEndChars = new HashSet<char>
        {
            '（', '(', '【', '[', '「', '『', '《', '〈', '"',
        };

        /// <summary>
        /// 排版后每个字符的布局信息
        /// </summary>
        private struct LayoutCharInfo
        {
            public int charIndex;
            public Rect rect; // 相对于RectTransform左上角
            public RichCharInfo richChar;
            public bool visible;
        }

        // ====== 属性 ======
        public Font Font
        {
            get { return m_Font; }
            set { m_Font = value; MarkDirty(); }
        }

        public int FontSize
        {
            get { return m_FontSize; }
            set { m_FontSize = value; MarkDirty(); }
        }

        public float LineSpacing
        {
            get { return m_LineSpacing; }
            set { m_LineSpacing = value; MarkDirty(); }
        }

        public TextAnchor Alignment
        {
            get { return m_Alignment; }
            set { m_Alignment = value; MarkDirty(); }
        }

        public string Text
        {
            get { return m_Text; }
            set
            {
                if (m_Text != value)
                {
                    m_Text = value;
                    MarkDirty();
                }
            }
        }

        public override Texture mainTexture
        {
            get
            {
                var f = GetActiveFont();
                if (f != null && f.material != null && f.material.mainTexture != null)
                    return f.material.mainTexture;
                return base.mainTexture;
            }
        }

        // ====== 公共方法 ======

        /// <summary>
        /// 设置文本内容（主入口）
        /// </summary>
        public void SetText(string text)
        {
            m_Text = text ?? "";
            MarkDirty();
        }

        /// <summary>
        /// 获取首选高度（用于ScrollView等自适应）
        /// </summary>
        public float PreferredHeight
        {
            get
            {
                EnsureLayout();
                return m_CalculatedHeight;
            }
        }

        // ====== 生命周期 ======

        protected override void Awake()
        {
            base.Awake();
            EnsureFont();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            var f = GetActiveFont();
            if (f != null)
                Font.textureRebuilt += OnFontTextureRebuilt;
            MarkDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Font.textureRebuilt -= OnFontTextureRebuilt;
            if (m_ImageManager != null)
                m_ImageManager.RecycleAll();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_ImageManager != null)
            {
                m_ImageManager.Dispose();
                m_ImageManager = null;
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            MarkDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            MarkDirty();
        }
#endif

        // ====== 字体 ======

        private void EnsureFont()
        {
            if (m_Font == null)
            {
#if UNITY_EDITOR
                m_Font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(DEFAULT_FONT_PATH);
#endif
                if (m_Font == null)
                    m_Font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        private Font GetActiveFont()
        {
            EnsureFont();
            return m_Font;
        }

        private void OnFontTextureRebuilt(Font changedFont)
        {
            if (changedFont == GetActiveFont())
            {
                SetVerticesDirty();
            }
        }

        // ====== 脏标记 ======

        private void MarkDirty()
        {
            m_Dirty = true;
            SetVerticesDirty();
            SetLayoutDirty();
        }

        // ====== 排版核心 ======

        private float m_CalculatedHeight;

        private void EnsureLayout()
        {
            if (!m_Dirty)
                return;

            m_Dirty = false;

            // 1. 解析
            m_ParseResult = RichTextParser.Parse(m_Text, color, m_FontSize);

            // 2. 排版
            DoLayout();

            // 3. 图片
            UpdateImages();
        }

        private void DoLayout()
        {
            m_LayoutChars.Clear();

            var font = GetActiveFont();
            if (font == null || m_ParseResult == null || m_ParseResult.chars.Count == 0)
            {
                m_CalculatedHeight = 0;
                return;
            }

            var rt = rectTransform;
            var containerWidth = rt.rect.width;
            if (containerWidth <= 0) containerWidth = 100;

            // 先请求所有字符到字体纹理
            RequestCharactersInFont(font);

            float x = 0;
            float y = 0;
            float lineHeight = m_FontSize * m_LineSpacing;
            float maxLineHeight = lineHeight;

            var chars = m_ParseResult.chars;

            // 逐字符排版
            int i = 0;
            while (i < chars.Count)
            {
                var ch = chars[i];

                // 换行符
                if (ch.character == '\n')
                {
                    m_LayoutChars.Add(new LayoutCharInfo
                    {
                        charIndex = i,
                        rect = new Rect(x, y, 0, maxLineHeight),
                        richChar = ch,
                        visible = false,
                    });
                    x = 0;
                    y -= maxLineHeight;
                    maxLineHeight = lineHeight;
                    i++;
                    continue;
                }

                // 计算字符/图片宽高
                float charW, charH;
                if (ch.isImagePlaceholder)
                {
                    charW = ch.imageInfo.width;
                    charH = ch.imageInfo.height;
                }
                else
                {
                    GetCharSize(font, ch, out charW, out charH);
                }

                // 英文单词不断行 - 找到整个单词
                int wordEnd = i;
                float wordWidth = charW;
                if (!ch.isImagePlaceholder && IsLatinChar(ch.character))
                {
                    int j = i + 1;
                    while (j < chars.Count && !chars[j].isImagePlaceholder &&
                           chars[j].character != '\n' && IsLatinChar(chars[j].character))
                    {
                        float cw, cch;
                        GetCharSize(font, chars[j], out cw, out cch);
                        wordWidth += cw;
                        j++;
                    }
                    wordEnd = j - 1;
                }

                // 判断是否需要换行
                bool needWrap = false;
                if (x > 0)
                {
                    if (wordEnd > i)
                    {
                        // 整个英文单词超出
                        if (x + wordWidth > containerWidth)
                            needWrap = true;
                    }
                    else
                    {
                        if (x + charW > containerWidth)
                            needWrap = true;
                    }
                }

                // 中文禁则: 当前字符不允许出现在行首
                if (needWrap && s_NoStartChars.Contains(ch.character))
                {
                    // 把前一个字符也带到下一行 (通过不换行来处理，让前一个字符和当前字符一起溢出到下一行)
                    // 这种情况下实际上需要回退
                    // 简单处理：强制不在此处断行，而在前面断行
                    // 我们在上面已经排版了前面的字符，这里简单地不做换行，让文字溢出
                    // 更好的策略：回退到上一个可断行位置
                    // 这里采用简单策略 - 允许溢出一个字符宽度
                    needWrap = false;
                }

                // 中文禁则: 当前字符不允许出现在行尾且下一个字符会导致换行
                if (!needWrap && !ch.isImagePlaceholder && s_NoEndChars.Contains(ch.character))
                {
                    // 预判下一个字符是否会超出
                    if (i + 1 < chars.Count)
                    {
                        float nextW, nextH;
                        var nextCh = chars[i + 1];
                        if (nextCh.isImagePlaceholder)
                        {
                            nextW = nextCh.imageInfo.width;
                        }
                        else
                        {
                            GetCharSize(font, nextCh, out nextW, out nextH);
                        }
                        if (x + charW + nextW > containerWidth && x > 0)
                        {
                            needWrap = true; // 当前行尾禁则字符连同移到下一行
                        }
                    }
                }

                if (needWrap)
                {
                    x = 0;
                    y -= maxLineHeight;
                    maxLineHeight = lineHeight;
                }

                // 图片字符的行高可能更大
                float elementHeight = ch.isImagePlaceholder ? charH : (ch.style.fontSize * m_LineSpacing);
                if (elementHeight > maxLineHeight)
                    maxLineHeight = elementHeight;

                var layout = new LayoutCharInfo
                {
                    charIndex = i,
                    rect = new Rect(x, y, charW, ch.isImagePlaceholder ? charH : (float)ch.style.fontSize),
                    richChar = ch,
                    visible = true,
                };
                m_LayoutChars.Add(layout);
                x += charW;

                // 如果是英文单词整体排版，后续字符接着排
                if (wordEnd > i)
                {
                    for (int k = i + 1; k <= wordEnd; k++)
                    {
                        float cw, cch;
                        GetCharSize(font, chars[k], out cw, out cch);
                        var wl = new LayoutCharInfo
                        {
                            charIndex = k,
                            rect = new Rect(x, y, cw, (float)chars[k].style.fontSize),
                            richChar = chars[k],
                            visible = true,
                        };
                        m_LayoutChars.Add(wl);
                        x += cw;
                    }
                    i = wordEnd + 1;
                }
                else
                {
                    i++;
                }
            }

            // 计算总高度
            float totalHeight = Mathf.Abs(y) + maxLineHeight;
            m_CalculatedHeight = totalHeight;

            // 应用对齐偏移
            ApplyAlignment(containerWidth, totalHeight);
        }

        private void ApplyAlignment(float containerWidth, float totalHeight)
        {
            if (m_LayoutChars.Count == 0) return;

            var rt = rectTransform;
            float pivotOffsetX = -rt.rect.width * rt.pivot.x;
            float pivotOffsetY = rt.rect.height * (1f - rt.pivot.y);

            // 按行分组
            float lastY = float.MaxValue;
            int lineStart = 0;

            for (int i = 0; i <= m_LayoutChars.Count; i++)
            {
                float curY = i < m_LayoutChars.Count ? m_LayoutChars[i].rect.y : float.MinValue;

                if (i == m_LayoutChars.Count || (curY != lastY && lastY != float.MaxValue))
                {
                    // 处理 lineStart ~ i-1 这一行
                    float lineMaxX = 0;
                    for (int j = lineStart; j < i; j++)
                    {
                        float right = m_LayoutChars[j].rect.xMax;
                        if (right > lineMaxX) lineMaxX = right;
                    }

                    float offsetX = 0;
                    if (m_Alignment == TextAnchor.UpperCenter || m_Alignment == TextAnchor.MiddleCenter || m_Alignment == TextAnchor.LowerCenter)
                        offsetX = (containerWidth - lineMaxX) * 0.5f;
                    else if (m_Alignment == TextAnchor.UpperRight || m_Alignment == TextAnchor.MiddleRight || m_Alignment == TextAnchor.LowerRight)
                        offsetX = containerWidth - lineMaxX;

                    float offsetY = 0;
                    if (m_Alignment == TextAnchor.MiddleLeft || m_Alignment == TextAnchor.MiddleCenter || m_Alignment == TextAnchor.MiddleRight)
                        offsetY = -(rt.rect.height - totalHeight) * 0.5f;
                    else if (m_Alignment == TextAnchor.LowerLeft || m_Alignment == TextAnchor.LowerCenter || m_Alignment == TextAnchor.LowerRight)
                        offsetY = -(rt.rect.height - totalHeight);

                    for (int j = lineStart; j < i; j++)
                    {
                        var lc = m_LayoutChars[j];
                        lc.rect = new Rect(
                            lc.rect.x + offsetX + pivotOffsetX,
                            lc.rect.y + offsetY + pivotOffsetY,
                            lc.rect.width,
                            lc.rect.height);
                        m_LayoutChars[j] = lc;
                    }

                    lineStart = i;
                }

                if (i < m_LayoutChars.Count)
                    lastY = m_LayoutChars[i].rect.y;
            }
        }

        private void RequestCharactersInFont(Font font)
        {
            if (m_ParseResult == null) return;
            foreach (var ch in m_ParseResult.chars)
            {
                if (!ch.isImagePlaceholder && ch.character != '\n')
                {
                    FontStyle fs = FontStyle.Normal;
                    if (ch.style.bold && ch.style.italic) fs = FontStyle.BoldAndItalic;
                    else if (ch.style.bold) fs = FontStyle.Bold;
                    else if (ch.style.italic) fs = FontStyle.Italic;

                    font.RequestCharactersInTexture(ch.character.ToString(), ch.style.fontSize, fs);
                }
            }
        }

        private void GetCharSize(Font font, RichCharInfo ch, out float width, out float height)
        {
            width = ch.style.fontSize * 0.5f;
            height = ch.style.fontSize;

            FontStyle fs = FontStyle.Normal;
            if (ch.style.bold && ch.style.italic) fs = FontStyle.BoldAndItalic;
            else if (ch.style.bold) fs = FontStyle.Bold;
            else if (ch.style.italic) fs = FontStyle.Italic;

            CharacterInfo ci;
            if (font.GetCharacterInfo(ch.character, out ci, ch.style.fontSize, fs))
            {
                width = ci.advance;
                if (width <= 0) width = ch.style.fontSize * 0.5f;
            }
        }

        private static bool IsLatinChar(char c)
        {
            // ASCII 字母、数字、连字符、撇号等不断行
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9') || c == '\'' || c == '-';
        }

        // ====== 图片更新 ======

        private void UpdateImages()
        {
            if (m_ImageManager == null)
                m_ImageManager = new InlineImageManager(rectTransform);

            m_ImageManager.RecycleAll();

            if (m_ParseResult == null) return;

            foreach (var lc in m_LayoutChars)
            {
                if (lc.richChar.isImagePlaceholder && lc.visible)
                {
                    var handle = m_ImageManager.GetImage(lc.richChar.imageInfo);
                    // 图片左下角对齐到字符位置
                    var pos = new Vector2(lc.rect.x, lc.rect.y);
                    m_ImageManager.SetImagePosition(handle, pos);
                }
            }
        }

        // ====== 顶点生成 ======

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            EnsureLayout();

            var font = GetActiveFont();
            if (font == null || m_ParseResult == null) return;

            // 再次确保字符在纹理中
            RequestCharactersInFont(font);

            // 生成文字顶点
            for (int i = 0; i < m_LayoutChars.Count; i++)
            {
                var lc = m_LayoutChars[i];
                if (!lc.visible) continue;
                if (lc.richChar.isImagePlaceholder) continue; // 图片由RawImage显示，不在这里画

                DrawCharacter(vh, font, lc);
            }

            // 生成下划线/删除线
            DrawLineDecorations(vh);
        }

        private void DrawCharacter(VertexHelper vh, Font font, LayoutCharInfo lc)
        {
            var ch = lc.richChar;

            FontStyle fs = FontStyle.Normal;
            if (ch.style.bold && ch.style.italic) fs = FontStyle.BoldAndItalic;
            else if (ch.style.bold) fs = FontStyle.Bold;
            else if (ch.style.italic) fs = FontStyle.Italic;

            CharacterInfo ci;
            if (!font.GetCharacterInfo(ch.character, out ci, ch.style.fontSize, fs))
                return;

            // 字符的四个顶点（左下角为基准，y向上）
            float baseLine = lc.rect.y; // 行基线近似
            float ascent = ch.style.fontSize * 0.8f;

            float x0 = lc.rect.x + ci.minX;
            float x1 = lc.rect.x + ci.maxX;
            float y0 = baseLine + ci.minY + ascent;
            float y1 = baseLine + ci.maxY + ascent;

            var c = ch.style.color;

            // 斜体偏移
            float italicOffset = 0;
            if (ch.style.italic)
            {
                italicOffset = ch.style.fontSize * 0.1f;
            }

            var v0 = new Vector3(x0, y0, 0); // bottom-left
            var v1 = new Vector3(x0 + italicOffset, y1, 0); // top-left
            var v2 = new Vector3(x1 + italicOffset, y1, 0); // top-right
            var v3 = new Vector3(x1, y0, 0); // bottom-right

            Vector2 uv0, uv1, uv2, uv3;
            // Unity的CharacterInfo uvBottomLeft等已经考虑了翻转
            if (ci.uvBottomLeft != Vector2.zero || ci.uvTopRight != Vector2.zero)
            {
                uv0 = ci.uvBottomLeft;
                uv1 = ci.uvTopLeft;
                uv2 = ci.uvTopRight;
                uv3 = ci.uvBottomRight;
            }
            else
            {
                uv0 = Vector2.zero;
                uv1 = Vector2.up;
                uv2 = Vector2.one;
                uv3 = Vector2.right;
            }

            int vertIdx = vh.currentVertCount;
            vh.AddVert(v0, c, uv0);
            vh.AddVert(v1, c, uv1);
            vh.AddVert(v2, c, uv2);
            vh.AddVert(v3, c, uv3);

            vh.AddTriangle(vertIdx, vertIdx + 1, vertIdx + 2);
            vh.AddTriangle(vertIdx, vertIdx + 2, vertIdx + 3);
        }

        private void DrawLineDecorations(VertexHelper vh)
        {
            if (m_ParseResult == null) return;

            foreach (var deco in m_ParseResult.lineDecorations)
            {
                DrawDecoLine(vh, deco);
            }

            // 超链接下划线
            foreach (var link in m_ParseResult.hyperlinks)
            {
                var decoAlt = new LineDecorationInfo
                {
                    startIndex = link.startIndex,
                    endIndex = link.endIndex,
                    isStrikethrough = false,
                    color = link.color,
                };
                DrawDecoLine(vh, decoAlt);
            }
        }

        private void DrawDecoLine(VertexHelper vh, LineDecorationInfo deco)
        {
            float thickness = Mathf.Max(1f, m_FontSize * 0.05f);

            // 找到对应字符的布局信息并按行画线
            float lineStartX = float.MaxValue;
            float lineEndX = float.MinValue;
            float lineY = float.MaxValue;

            for (int i = 0; i < m_LayoutChars.Count; i++)
            {
                var lc = m_LayoutChars[i];
                if (lc.charIndex < deco.startIndex || lc.charIndex > deco.endIndex)
                    continue;
                if (!lc.visible || lc.richChar.isImagePlaceholder)
                    continue;

                float charY = lc.rect.y;

                // 检测是否换行了
                if (lineY != float.MaxValue && Mathf.Abs(charY - lineY) > 1f)
                {
                    // 画前一行
                    EmitLineQuad(vh, lineStartX, lineEndX, lineY, deco, thickness);
                    lineStartX = float.MaxValue;
                    lineEndX = float.MinValue;
                }

                lineY = charY;
                if (lc.rect.x < lineStartX) lineStartX = lc.rect.x;
                if (lc.rect.xMax > lineEndX) lineEndX = lc.rect.xMax;
            }

            if (lineStartX < lineEndX)
            {
                EmitLineQuad(vh, lineStartX, lineEndX, lineY, deco, thickness);
            }
        }

        private void EmitLineQuad(VertexHelper vh, float x0, float x1, float baseY, LineDecorationInfo deco, float thickness)
        {
            float ascent = m_FontSize * 0.8f;
            float yPos;
            if (deco.isStrikethrough)
            {
                yPos = baseY + ascent * 0.5f; // 中间位置
            }
            else
            {
                yPos = baseY; // 底部
            }

            int idx = vh.currentVertCount;
            var c = deco.color;

            vh.AddVert(new Vector3(x0, yPos - thickness * 0.5f, 0), c, Vector2.zero);
            vh.AddVert(new Vector3(x0, yPos + thickness * 0.5f, 0), c, Vector2.zero);
            vh.AddVert(new Vector3(x1, yPos + thickness * 0.5f, 0), c, Vector2.zero);
            vh.AddVert(new Vector3(x1, yPos - thickness * 0.5f, 0), c, Vector2.zero);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        // ====== 点击处理 ======

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_ParseResult == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);

            // 检查超链接
            foreach (var link in m_ParseResult.hyperlinks)
            {
                for (int i = 0; i < m_LayoutChars.Count; i++)
                {
                    var lc = m_LayoutChars[i];
                    if (lc.charIndex >= link.startIndex && lc.charIndex <= link.endIndex && lc.visible)
                    {
                        if (lc.rect.Contains(localPoint))
                        {
                            OnHyperlinkClick?.Invoke(link.url);
                            return;
                        }
                    }
                }
            }

            // 检查图片点击
            for (int i = 0; i < m_LayoutChars.Count; i++)
            {
                var lc = m_LayoutChars[i];
                if (lc.richChar.isImagePlaceholder && lc.visible)
                {
                    var imgRect = new Rect(lc.rect.x, lc.rect.y, lc.richChar.imageInfo.width, lc.richChar.imageInfo.height);
                    if (imgRect.Contains(localPoint))
                    {
                        OnImageClick?.Invoke(lc.richChar.imageInfo);
                        return;
                    }
                }
            }
        }
    }
}
