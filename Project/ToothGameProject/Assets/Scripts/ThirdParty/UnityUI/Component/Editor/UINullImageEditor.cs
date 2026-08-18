using System.Collections;
using UnityEngine;

namespace UnityUI
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.UI;

    [CanEditMultipleObjects, CustomEditor(typeof(UINullImage), false)]
    public class UINullImageEditor : GraphicEditor
    {
        public override void OnInspectorGUI()
        {
            base.serializedObject.Update();
            EditorGUILayout.PropertyField(base.m_Script, new GUILayoutOption[0]);
            base.RaycastControlsGUI();
            base.serializedObject.ApplyModifiedProperties();
        }
    }
}