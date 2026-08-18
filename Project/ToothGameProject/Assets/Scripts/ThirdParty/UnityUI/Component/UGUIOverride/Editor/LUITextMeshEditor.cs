using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace UnityUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LUITextMesh))]
    public class LUITextMeshEditor : TMP_EditorPanelUI
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
            EditorGUILayout.PropertyField(StyleName, new GUIContent("Style Name"));
            EditorGUILayout.PropertyField(LanguageId, new GUIContent("Language ID"));
            EditorGUILayout.PropertyField(InputType, new GUIContent("Input Type"));
            EditorGUILayout.PropertyField(CheckAndReplaceMultiLine, new GUIContent("Check Newline"));
            EditorGUILayout.PropertyField(CheckBiaoDian, new GUIContent("Check Line-Start Punctuation"));
            EditorGUILayout.PropertyField(GrayColor, new GUIContent("Gray Color"));
            EditorGUILayout.PropertyField(DesignerColor, new GUIContent("Designer Color"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
