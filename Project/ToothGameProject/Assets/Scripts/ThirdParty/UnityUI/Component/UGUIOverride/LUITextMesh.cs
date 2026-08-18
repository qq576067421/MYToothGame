using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityUI
{
    public class LUITextMesh : TextMeshProUGUI
    {
        public string StyleName = "normal";
        [HideInInspector]
        public string LanguageId = "";
        // Used for string.Format arguments.
        public object[] StyleParams;
        [HideInInspector]
        public TextInputType InputType = TextInputType.None;
        [HideInInspector]
        public bool CheckAndReplaceMultiLine = false;
        [HideInInspector]
        public bool CheckBiaoDian = false;
        public static Func<string, object[], string> OnSetTextLanguageCall;

        public Color DesignerColor = Color.white;
        public Color GrayColor = Color.gray;
        public float m_DesingerSize;
        public static int m_LanguageFontSizeFactor = 100;
        private int m_CurrentFontSizeFactor = 100;
        private const int MaxFormatDepth = 100;

        public void SetGray(bool gray)
        {
            color = gray ? GrayColor : DesignerColor;
        }

        private void InitFontSize()
        {
            if (m_DesingerSize <= 0)
            {
                m_DesingerSize = fontSize;
            }

            if (m_LanguageFontSizeFactor != m_CurrentFontSizeFactor)
            {
                fontSize = m_DesingerSize * m_LanguageFontSizeFactor / 100f;
            }

            m_CurrentFontSizeFactor = m_LanguageFontSizeFactor;
        }

        protected override void Awake()
        {
            base.Awake();
            InitFontSize();
            if (InputType == TextInputType.ID)
            {
                SetTextByLanId(LanguageId);
            }
        }

        public virtual void OnChangeLanguage()
        {
            InitFontSize();
            if (InputType == TextInputType.ID)
            {
                SetTextByLanId(LanguageId);
            }
        }

        public virtual void SetTextByString(string value)
        {
            text = value;
            FixLineBreakFormatter();
        }

        public virtual void SetTextByLanId(string id)
        {
            if (OnSetTextLanguageCall != null)
            {
                string str = OnSetTextLanguageCall(id, StyleParams);
                if (CheckAndReplaceMultiLine && str != null)
                {
                    str = str.Replace("\\n", "\n");
                }

                text = str ?? "NullText";
                FixLineBreakFormatter();
            }
        }

        public void FixLineBreakFormatter()
        {
            if (!CheckBiaoDian)
            {
                return;
            }

            if (!enableWordWrapping)
            {
                return;
            }

            if (!LUITextLineBreakFormatter.m_IsOpenFormatter)
            {
                return;
            }

            var avoidAtStartOfLine = LUITextLineBreakFormatter.avoidAtStartOfLineDefault;
            if (avoidAtStartOfLine == null || avoidAtStartOfLine.Count == 0)
            {
                return;
            }

            string formatted = text;
            if (string.IsNullOrEmpty(formatted))
            {
                return;
            }

            for (int depth = 0; depth < MaxFormatDepth; depth++)
            {
                base.text = formatted;
                ForceMeshUpdate(true);

                var currentTextInfo = textInfo;
                if (currentTextInfo == null || currentTextInfo.lineCount <= 1)
                {
                    return;
                }

                bool isFixed = false;
                for (int i = 1; i < currentTextInfo.lineCount; i++)
                {
                    var lineInfo = currentTextInfo.lineInfo[i];
                    if (lineInfo.characterCount <= 0)
                    {
                        continue;
                    }

                    if (lineInfo.firstCharacterIndex < 0 || lineInfo.firstCharacterIndex >= currentTextInfo.characterCount)
                    {
                        continue;
                    }

                    var charInfo = currentTextInfo.characterInfo[lineInfo.firstCharacterIndex];
                    if (!avoidAtStartOfLine.Contains(charInfo.character))
                    {
                        continue;
                    }

                    int insertIndex = charInfo.index;
                    if (insertIndex <= 0 || insertIndex > formatted.Length)
                    {
                        continue;
                    }

                    formatted = formatted.Insert(insertIndex, "\n");
                    isFixed = true;
                    break;
                }

                if (!isFixed)
                {
                    return;
                }
            }
        }
    }
}
