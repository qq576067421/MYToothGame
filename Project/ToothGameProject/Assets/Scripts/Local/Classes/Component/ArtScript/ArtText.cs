using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUI
{
    public enum ArtTextInputType
    {
        None,
        String,
        ID
    }
    public class ArtText : Text
    {
        [HideInInspector]
        public string LanguageId = "";
        [HideInInspector]
        public ArtTextInputType InputType = ArtTextInputType.None;
        [HideInInspector]
        public bool CheckAndReplaceMultiLine = false;
        //这里应该用静态字段，只需要设置一个就可以了
        public static Func<string, string> OnSetTextLanguageCall;
        public static Func<Font> OnSetTextFontCall;

        public Color DesignerColor;
        public Color GrayColor;
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
        protected override void Awake()
        {
            base.Awake();
            if(OnSetTextFontCall != null)
            {
                font = OnSetTextFontCall();
            }
            if (InputType == ArtTextInputType.None)
            {
                return;
            }
            if (InputType == ArtTextInputType.ID)
            {
                SetTextByLanId(LanguageId);
            }
        }
        public virtual void OnChangeLanguage()
        {
            if (InputType == ArtTextInputType.None)
            {
                return;
            }
            if (InputType == ArtTextInputType.ID)
            {
                SetTextByLanId(LanguageId);
            }
        }
        public virtual void SetTextByString(string text)
        {
            this.text = text;

        }
        public virtual void SetTextByLanId(string id)
        {
            if (OnSetTextLanguageCall != null)
            {
                string str = OnSetTextLanguageCall(id);
                //配置表等输入的时候 只需要输入\n就可以了，c#会自动添加一个\， 变成\\n，然后通过如下代码转换正确
                if (CheckAndReplaceMultiLine)
                {
                    str = str.Replace("\\n", "\n");
                }

                this.text = str == null ? "NullText" : str;
            }
        }

    }
}
