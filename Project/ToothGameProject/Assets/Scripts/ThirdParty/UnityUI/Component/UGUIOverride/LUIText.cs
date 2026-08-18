using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUI
{
    public enum TextInputType
    {
        None,
        String,
        ID
    }
    public class LUIText : Text
    {
        public string StyleName = "normal";
        [HideInInspector]
        public string LanguageId = "";
        //用于自动填充string.format
        public object[] StyleParams;
        [HideInInspector]
        public TextInputType InputType = TextInputType.None;
        [HideInInspector]
        public bool CheckAndReplaceMultiLine = false;
        [HideInInspector]
        public bool CheckBiaoDian = false;
        //这里应该用静态字段，只需要设置一个就可以了
        public static Func<string, object[], string> OnSetTextLanguageCall;

        public Color DesignerColor = Color.white;
        public Color GrayColor = Color.gray;
        public int m_DesingerSize;
        public static int m_LanguageFontSizeFactor = 100;
        private int m_CurrentFontSizeFactor = 100;
        public void SetGray(bool gray)
        {
            if(gray)
            {
                color = GrayColor;
            }
            else
            {
                color = DesignerColor;
            }
        }

        private void InitFontSize()
        {
            if(m_DesingerSize == 0)
            {
                m_DesingerSize = this.fontSize;
            }
            if(m_LanguageFontSizeFactor != m_CurrentFontSizeFactor)
            {
                var size = m_DesingerSize * m_LanguageFontSizeFactor / 100;
                if(size % 2 ==1)
                {
                    size = size + 1;
                }
                this.fontSize = size;
            }
            m_CurrentFontSizeFactor = m_LanguageFontSizeFactor;
        }
        protected override void Awake()
        {
            base.Awake();
            InitFontSize();
            if (InputType == TextInputType.None)
            {
                return;
            }
            if (InputType == TextInputType.ID)
            {
                SetTextByLanId(LanguageId);
            }
        }
        public virtual void OnChangeLanguage()
        {
            InitFontSize();
            if (InputType == TextInputType.None)
            {
                return;
            }
  
            if (InputType == TextInputType.ID)
            {
                SetTextByLanId(LanguageId);
            }
        }
        public virtual void SetTextByString(string text)
        {
            this.text = text;
            FixLineBreakFormatter();
        }
        public virtual void SetTextByLanId(string id)
        {
            if (OnSetTextLanguageCall != null)
            {
                string str = OnSetTextLanguageCall(id, StyleParams);
                //配置表等输入的时候 只需要输入\n就可以了，c#会自动添加一个\， 变成\\n，然后通过如下代码转换正确
                if (CheckAndReplaceMultiLine)
                {
                    str = str.Replace("\\n", "\n");
                }

                this.text = str == null ? "NullText" : str;
                FixLineBreakFormatter();
            }
        }
        public void FixLineBreakFormatter()
        {
            if(!CheckBiaoDian)
            {
                return;
            }
            if(this.horizontalOverflow != HorizontalWrapMode.Wrap)
            {
                return;
            }
            if(!LUITextLineBreakFormatter.m_IsOpenFormatter)
            {
                return;
            }
            var formatter = GetComponent<LUITextLineBreakFormatter>();
            if(formatter == null)
            {
                formatter = this.gameObject.AddComponent<LUITextLineBreakFormatter>();
            }
            formatter.textToReformat = this;
            formatter.FormatText();
        }
    }
}
