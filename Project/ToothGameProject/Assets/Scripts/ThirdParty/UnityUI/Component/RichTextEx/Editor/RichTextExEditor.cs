using UnityEditor;
using UnityEngine;

namespace UnityUI
{
    [CustomEditor(typeof(RichTextEx))]
    [CanEditMultipleObjects]
    public class RichTextExEditor : Editor
    {
        private SerializedProperty m_Font;
        private SerializedProperty m_FontSize;
        private SerializedProperty m_LineSpacing;
        private SerializedProperty m_Alignment;
        private SerializedProperty m_Text;
        private SerializedProperty m_Color;
        private SerializedProperty m_Material;
        private SerializedProperty m_RaycastTarget;
        private SerializedProperty m_FontStyle;

        private bool m_ShowPreview = false;

        protected void OnEnable()
        {
            m_Font = serializedObject.FindProperty("m_Font");
            m_FontSize = serializedObject.FindProperty("m_FontSize");
            m_LineSpacing = serializedObject.FindProperty("m_LineSpacing");
            m_Alignment = serializedObject.FindProperty("m_Alignment");
            m_Text = serializedObject.FindProperty("m_Text");
            m_Color = serializedObject.FindProperty("m_Color");
            m_Material = serializedObject.FindProperty("m_Material");
            m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
            m_FontStyle = serializedObject.FindProperty("m_FontStyle");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Font, new GUIContent("字体 (Font)"));
            EditorGUILayout.PropertyField(m_FontSize, new GUIContent("字号 (Font Size)"));
            EditorGUILayout.PropertyField(m_FontStyle, new GUIContent("字体样式 (Font Style)"));
            EditorGUILayout.PropertyField(m_LineSpacing, new GUIContent("行距 (Line Spacing)"));
            EditorGUILayout.PropertyField(m_Alignment, new GUIContent("对齐 (Alignment)"));
            EditorGUILayout.PropertyField(m_Color, new GUIContent("颜色 (Color)"));
            EditorGUILayout.PropertyField(m_RaycastTarget, new GUIContent("射线检测 (Raycast Target)"));
            EditorGUILayout.PropertyField(m_Material, new GUIContent("材质 (Material)"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("文本内容 (Text)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Text, GUIContent.none);

            EditorGUILayout.Space();
            m_ShowPreview = EditorGUILayout.Foldout(m_ShowPreview, "标签帮助 (Tag Help)");
            if (m_ShowPreview)
            {
                EditorGUILayout.HelpBox(
                    "支持的标签:\n" +
                    "  图片:   <img=abName:assetName,width,height>\n" +
                    "  颜色:   <color=#RRGGBB>文字</color>\n" +
                    "  大小:   <size=30>文字</size>\n" +
                    "  粗体:   <b>文字</b>\n" +
                    "  斜体:   <i>文字</i>\n" +
                    "  下划线: <u>文字</u>\n" +
                    "  删除线: <s>文字</s>\n" +
                    "  超链接: <a=url>文字</a>\n\n" +
                    "图片示例:\n" +
                    "  <img=texture_set/item:gold_icon,24,24>\n" +
                    "  <img=ui/hero_icon,64,64>",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
