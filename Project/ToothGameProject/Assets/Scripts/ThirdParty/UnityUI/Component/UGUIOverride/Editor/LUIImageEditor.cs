using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.UI;

namespace UnityUI
{
	[CanEditMultipleObjects]
    [CustomEditor(typeof(LUIImage))]
    public class LUIImageEditor : ImageEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();


            LUIImage c = target as LUIImage;
            c.StyleName = EditorGUILayout.TextField("样式名称：", c.StyleName);
            //c.LanguageId = EditorGUILayout.IntField("语言包ID：", c.LanguageId);
            //c.InputType = (TextInputType)EditorGUILayout.EnumFlagsField("输入类型：", c.InputType);
        }
    }
}
