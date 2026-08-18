using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.UI;

namespace UnityUI
{
	[CanEditMultipleObjects]
    [CustomEditor(typeof(LUIRawImage))]
    public class LUIRawImageEditor : RawImageEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LUIRawImage c = target as LUIRawImage;
            c.StyleName = EditorGUILayout.TextField("样式名称：", c.StyleName);
            //LUIText c = target as LUIText;
            //c.LanguageId = EditorGUILayout.IntField("语言包ID：", c.LanguageId);
            //c.InputType = (TextInputType)EditorGUILayout.EnumFlagsField("输入类型：", c.InputType);
        }
    }
}
