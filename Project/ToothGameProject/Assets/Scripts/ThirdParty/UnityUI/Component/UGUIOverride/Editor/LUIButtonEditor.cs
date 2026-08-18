using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.UI;

namespace UnityUI
{
	[CanEditMultipleObjects]
    [CustomEditor(typeof(LUIButton))]
    public class LUIButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();


            LUIButton c = target as LUIButton;
            c.StyleName = EditorGUILayout.TextField("样式名称：", c.StyleName);
            c.Cooldown = EditorGUILayout.FloatField("内置冷却：", c.Cooldown);
            c.ChooseState = EditorGUILayout.ObjectField("选择状态：", c.ChooseState, typeof(GameObject), true) as GameObject;

            if (GUI.changed)
            {
                UnityEditor.Undo.RecordObject(c, "LUIButton Change");
                UnityEditor.EditorUtility.SetDirty(c);
            }
        }
    }
}
