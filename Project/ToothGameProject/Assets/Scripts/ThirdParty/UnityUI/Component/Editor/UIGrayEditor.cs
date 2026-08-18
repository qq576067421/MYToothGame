using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
namespace UnityUI
{
    [CustomEditor(typeof(UIGray))]
    public class UIGrayEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            GUILayout.Space(6f);

            UIGray tw = target as UIGray;
            GUI.changed = false;

            Graphic g = (Graphic)EditorGUILayout.ObjectField("Graphics", tw.m_Graphics, typeof(Graphic));
            bool gray = EditorGUILayout.Toggle("Gray", tw.Gray);


            if (GUI.changed)
            {
                UnityEditor.Undo.RecordObject(tw, "gray change");
                tw.m_Graphics = g;
                tw.Gray = gray;
                UnityEditor.EditorUtility.SetDirty(tw);
            }

        }

    }
}