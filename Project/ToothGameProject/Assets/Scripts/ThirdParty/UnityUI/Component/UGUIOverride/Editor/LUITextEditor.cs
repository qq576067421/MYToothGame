using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace UnityUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LUIText))]
    public class LUITextEditor : UnityEditor.UI.TextEditor
    {
        SerializedProperty StyleName;
        SerializedProperty LanguageId;
        SerializedProperty InputType;
        SerializedProperty CheckAndReplaceMultiLine;
        SerializedProperty CheckBiaoDian;
        SerializedProperty GrayColor;
        SerializedProperty DesignerColor;
        protected override void OnEnable()
        {
            base.OnEnable();
            StyleName = serializedObject.FindProperty("StyleName");
            LanguageId = serializedObject.FindProperty("LanguageId");
            InputType = serializedObject.FindProperty("InputType");
            CheckAndReplaceMultiLine = serializedObject.FindProperty("CheckAndReplaceMultiLine");
            CheckBiaoDian = serializedObject.FindProperty("CheckBiaoDian");
            GrayColor = serializedObject.FindProperty("GrayColor");
            DesignerColor = serializedObject.FindProperty("DesignerColor");

        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.PropertyField(StyleName, new GUIContent("样式名称："));
            EditorGUILayout.PropertyField(LanguageId, new GUIContent("语言包ID："));
            EditorGUILayout.PropertyField(InputType, new GUIContent("输入类型："));
            EditorGUILayout.PropertyField(CheckAndReplaceMultiLine, new GUIContent("换行符检测"));
            EditorGUILayout.PropertyField(CheckBiaoDian, new GUIContent("行首标点符号检测"));
            EditorGUILayout.PropertyField(GrayColor, new GUIContent("灰色"));
            EditorGUILayout.PropertyField(DesignerColor, new GUIContent("设计颜色"));




            serializedObject.ApplyModifiedProperties();

            //LUIText c = target as LUIText;
            //c.LanguageId = EditorGUILayout.TextField("语言包ID：", c.LanguageId);
            //c.InputType = (TextInputType)EditorGUILayout.EnumPopup("输入类型：", c.InputType);
            //c.CheckAndReplaceMultiLine = EditorGUILayout.Toggle("换行符检测", c.CheckAndReplaceMultiLine);
            //c.GrayColor = EditorGUILayout.ColorField("灰色", c.GrayColor);
            //c.DesignerColor = EditorGUILayout.ColorField("设计颜色", c.DesignerColor);
            //if (GUI.changed)
            //{
            //    UnityUIEditorTools.RegisterUndo("LUIText Change", c);
            //    UnityEditor.EditorUtility.SetDirty(c);
            //}
        }
    }
}
