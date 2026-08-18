using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityUI
{
    /// <summary>
    /// 富文本元素类型
    /// </summary>
    public enum RichElementType
    {
        Text,
        Image,
        NewLine,
    }

    /// <summary>
    /// 超链接信息
    /// </summary>
    public class HyperlinkInfo
    {
        public int startIndex;
        public int endIndex;
        public string url;
        public Color color = new Color(0.2f, 0.4f, 1f, 1f);
        public List<Rect> boxes = new List<Rect>();
    }

    /// <summary>
    /// 下划线/删除线信息
    /// </summary>
    public class LineDecorationInfo
    {
        public int startIndex;
        public int endIndex;
        public bool isStrikethrough; // true=删除线, false=下划线
        public Color color = Color.white;
    }

    /// <summary>
    /// 图片元素信息
    /// </summary>
    public class ImageElementInfo
    {
        public string abName;
        public string assetName;
        public float width = 32;
        public float height = 32;
        public bool isSpriteAtlas; // texture_set/ 开头
        public int charIndex; // 在纯文本中的字符位置
    }

    /// <summary>
    /// 富文本样式状态
    /// </summary>
    public struct RichTextStyle
    {
        public Color color;
        public int fontSize;
        public bool bold;
        public bool italic;

        public static RichTextStyle Default(Color defaultColor, int defaultSize)
        {
            return new RichTextStyle
            {
                color = defaultColor,
                fontSize = defaultSize,
                bold = false,
                italic = false,
            };
        }
    }

    /// <summary>
    /// 解析后每个字符的信息
    /// </summary>
    public class RichCharInfo
    {
        public char character;
        public RichTextStyle style;
        public bool isImagePlaceholder;
        public ImageElementInfo imageInfo;
        public int hyperlinkIndex = -1; // -1=无超链接
        public int lineDecorationIndex = -1; // -1=无下划线/删除线
    }

    /// <summary>
    /// 富文本解析器
    /// 解析 img 标签、富文本标签（color/size/b/i）、超链接(<a>)、下划线(<u>)、删除线(<s>)
    /// </summary>
    public static class RichTextParser
    {
        // img标签: <img=abName:assetName,width,height> 或 <img=abName,width,height>
        private static readonly Regex s_ImgRegex = new Regex(
            @"<img=([^,>]+?)(?:,(\d+))?(?:,(\d+))?>",
            RegexOptions.Compiled);

        // 超链接: <a=url>text</a>
        private static readonly Regex s_LinkOpenRegex = new Regex(
            @"<a=([^>]+)>",
            RegexOptions.Compiled);

        // 颜色: <color=#RRGGBB> 或 <color=#RRGGBBAA> 或 <color=colorname>
        private static readonly Regex s_ColorRegex = new Regex(
            @"<color=(#?[a-fA-F0-9]{6,8}|[a-zA-Z]+)>",
            RegexOptions.Compiled);

        // 大小: <size=N>
        private static readonly Regex s_SizeRegex = new Regex(
            @"<size=(\d+)>",
            RegexOptions.Compiled);

        /// <summary>
        /// 解析富文本字符串，返回每个字符的信息列表
        /// </summary>
        public static ParseResult Parse(string input, Color defaultColor, int defaultFontSize)
        {
            var result = new ParseResult();

            if (string.IsNullOrEmpty(input))
                return result;

            var styleStack = new Stack<RichTextStyle>();
            var currentStyle = RichTextStyle.Default(defaultColor, defaultFontSize);

            int currentHyperlinkIdx = -1;
            int currentLineDecoIdx = -1;

            int i = 0;
            while (i < input.Length)
            {
                // 换行
                if (input[i] == '\n')
                {
                    var ci = new RichCharInfo
                    {
                        character = '\n',
                        style = currentStyle,
                        isImagePlaceholder = false,
                        hyperlinkIndex = currentHyperlinkIdx,
                        lineDecorationIndex = currentLineDecoIdx,
                    };
                    result.chars.Add(ci);
                    i++;
                    continue;
                }

                // 尝试解析标签
                if (input[i] == '<')
                {
                    // img标签
                    var imgMatch = s_ImgRegex.Match(input, i);
                    if (imgMatch.Success && imgMatch.Index == i)
                    {
                        var imgInfo = ParseImageTag(imgMatch);
                        imgInfo.charIndex = result.chars.Count;
                        result.images.Add(imgInfo);

                        // 添加一个占位字符
                        var ci = new RichCharInfo
                        {
                            character = '\u00A0', // non-breaking space as placeholder
                            style = currentStyle,
                            isImagePlaceholder = true,
                            imageInfo = imgInfo,
                            hyperlinkIndex = currentHyperlinkIdx,
                            lineDecorationIndex = currentLineDecoIdx,
                        };
                        result.chars.Add(ci);
                        i += imgMatch.Length;
                        continue;
                    }

                    // 超链接开始 <a=url>
                    var linkMatch = s_LinkOpenRegex.Match(input, i);
                    if (linkMatch.Success && linkMatch.Index == i)
                    {
                        var link = new HyperlinkInfo
                        {
                            startIndex = result.chars.Count,
                            url = linkMatch.Groups[1].Value,
                            color = new Color(0.2f, 0.4f, 1f, 1f),
                        };
                        result.hyperlinks.Add(link);
                        currentHyperlinkIdx = result.hyperlinks.Count - 1;

                        // 超链接文字颜色
                        styleStack.Push(currentStyle);
                        currentStyle.color = link.color;

                        i += linkMatch.Length;
                        continue;
                    }

                    // 超链接结束 </a>
                    if (MatchTag(input, i, "</a>"))
                    {
                        if (currentHyperlinkIdx >= 0)
                        {
                            result.hyperlinks[currentHyperlinkIdx].endIndex = result.chars.Count - 1;
                            currentHyperlinkIdx = -1;

                            if (styleStack.Count > 0)
                                currentStyle = styleStack.Pop();
                        }
                        i += 4;
                        continue;
                    }

                    // 下划线 <u>
                    if (MatchTag(input, i, "<u>"))
                    {
                        var deco = new LineDecorationInfo
                        {
                            startIndex = result.chars.Count,
                            isStrikethrough = false,
                            color = currentStyle.color,
                        };
                        result.lineDecorations.Add(deco);
                        currentLineDecoIdx = result.lineDecorations.Count - 1;
                        i += 3;
                        continue;
                    }
                    if (MatchTag(input, i, "</u>"))
                    {
                        if (currentLineDecoIdx >= 0 && !result.lineDecorations[currentLineDecoIdx].isStrikethrough)
                        {
                            result.lineDecorations[currentLineDecoIdx].endIndex = result.chars.Count - 1;
                            currentLineDecoIdx = -1;
                        }
                        i += 4;
                        continue;
                    }

                    // 删除线 <s>
                    if (MatchTag(input, i, "<s>"))
                    {
                        var deco = new LineDecorationInfo
                        {
                            startIndex = result.chars.Count,
                            isStrikethrough = true,
                            color = currentStyle.color,
                        };
                        result.lineDecorations.Add(deco);
                        currentLineDecoIdx = result.lineDecorations.Count - 1;
                        i += 3;
                        continue;
                    }
                    if (MatchTag(input, i, "</s>"))
                    {
                        if (currentLineDecoIdx >= 0 && result.lineDecorations[currentLineDecoIdx].isStrikethrough)
                        {
                            result.lineDecorations[currentLineDecoIdx].endIndex = result.chars.Count - 1;
                            currentLineDecoIdx = -1;
                        }
                        i += 4;
                        continue;
                    }

                    // color标签
                    var colorMatch = s_ColorRegex.Match(input, i);
                    if (colorMatch.Success && colorMatch.Index == i)
                    {
                        styleStack.Push(currentStyle);
                        Color c;
                        if (ColorUtility.TryParseHtmlString(colorMatch.Groups[1].Value, out c))
                        {
                            currentStyle.color = c;
                        }
                        i += colorMatch.Length;
                        continue;
                    }
                    if (MatchTag(input, i, "</color>"))
                    {
                        if (styleStack.Count > 0)
                            currentStyle = styleStack.Pop();
                        i += 8;
                        continue;
                    }

                    // size标签
                    var sizeMatch = s_SizeRegex.Match(input, i);
                    if (sizeMatch.Success && sizeMatch.Index == i)
                    {
                        styleStack.Push(currentStyle);
                        int sz;
                        if (int.TryParse(sizeMatch.Groups[1].Value, out sz))
                        {
                            currentStyle.fontSize = sz;
                        }
                        i += sizeMatch.Length;
                        continue;
                    }
                    if (MatchTag(input, i, "</size>"))
                    {
                        if (styleStack.Count > 0)
                            currentStyle = styleStack.Pop();
                        i += 7;
                        continue;
                    }

                    // 粗体
                    if (MatchTag(input, i, "<b>"))
                    {
                        styleStack.Push(currentStyle);
                        currentStyle.bold = true;
                        i += 3;
                        continue;
                    }
                    if (MatchTag(input, i, "</b>"))
                    {
                        if (styleStack.Count > 0)
                            currentStyle = styleStack.Pop();
                        i += 4;
                        continue;
                    }

                    // 斜体
                    if (MatchTag(input, i, "<i>"))
                    {
                        styleStack.Push(currentStyle);
                        currentStyle.italic = true;
                        i += 3;
                        continue;
                    }
                    if (MatchTag(input, i, "</i>"))
                    {
                        if (styleStack.Count > 0)
                            currentStyle = styleStack.Pop();
                        i += 4;
                        continue;
                    }
                }

                // 普通字符
                var charInfo = new RichCharInfo
                {
                    character = input[i],
                    style = currentStyle,
                    isImagePlaceholder = false,
                    hyperlinkIndex = currentHyperlinkIdx,
                    lineDecorationIndex = currentLineDecoIdx,
                };
                result.chars.Add(charInfo);
                i++;
            }

            // 处理未关闭的标签
            if (currentHyperlinkIdx >= 0)
                result.hyperlinks[currentHyperlinkIdx].endIndex = result.chars.Count - 1;
            if (currentLineDecoIdx >= 0)
                result.lineDecorations[currentLineDecoIdx].endIndex = result.chars.Count - 1;

            return result;
        }

        private static ImageElementInfo ParseImageTag(Match match)
        {
            var info = new ImageElementInfo();
            string path = match.Groups[1].Value; // abName:assetName 或 abName

            if (path.Contains(":"))
            {
                var parts = path.Split(':');
                info.abName = parts[0];
                info.assetName = parts[1];
            }
            else
            {
                info.abName = path;
                // 从路径中提取资源名
                int lastSlash = path.LastIndexOf('/');
                info.assetName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
            }

            info.isSpriteAtlas = info.abName.StartsWith("texture_set/");

            if (match.Groups[2].Success)
                float.TryParse(match.Groups[2].Value, out info.width);
            if (match.Groups[3].Success)
                float.TryParse(match.Groups[3].Value, out info.height);

            if (info.width <= 0) info.width = 32;
            if (info.height <= 0) info.height = 32;

            return info;
        }

        private static bool MatchTag(string input, int startIndex, string tag)
        {
            if (startIndex + tag.Length > input.Length)
                return false;

            for (int j = 0; j < tag.Length; j++)
            {
                if (char.ToLower(input[startIndex + j]) != char.ToLower(tag[j]))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 解析结果
    /// </summary>
    public class ParseResult
    {
        public List<RichCharInfo> chars = new List<RichCharInfo>();
        public List<ImageElementInfo> images = new List<ImageElementInfo>();
        public List<HyperlinkInfo> hyperlinks = new List<HyperlinkInfo>();
        public List<LineDecorationInfo> lineDecorations = new List<LineDecorationInfo>();
    }
}
