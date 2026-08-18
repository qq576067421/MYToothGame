using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

using GameDll;
using System;

using Object = UnityEngine.Object;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

//lua
public class UIToolsWindow : EditorWindow
{
    [MenuItem("Tools/UIToolsWindow")]
    static void AddWindow()
    {
        UIToolsWindow window = (UIToolsWindow)EditorWindow.GetWindow(typeof(UIToolsWindow));
        window.m_ColorBlock.normalColor = Color.white;
        window.m_ColorBlock.highlightedColor = new Color32(245, 245, 245, 255);
        window.m_ColorBlock.pressedColor = new Color32(200, 200, 200, 255);
        window.m_ColorBlock.selectedColor = new Color32(245, 245, 245, 255);
        window.m_ColorBlock.disabledColor = new Color32(200, 200, 200, 128);
        window.Show();
    }



    private Vector2 m_ScrollPosition;
    public UnityEngine.UI.ColorBlock m_ColorBlock;
    private GameObject m_EditorGameObjectRoot;

    

    void OnGUI()
    {
        m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
        GUILayout.BeginVertical();

        //ffffff
        m_ColorBlock.normalColor = EditorGUILayout.ColorField("normalColor:", m_ColorBlock.normalColor);
        m_ColorBlock.highlightedColor = EditorGUILayout.ColorField("highlightedColor:", m_ColorBlock.highlightedColor);
        m_ColorBlock.pressedColor = EditorGUILayout.ColorField("pressedColor:", m_ColorBlock.pressedColor);
        m_ColorBlock.selectedColor = EditorGUILayout.ColorField("selectedColor:", m_ColorBlock.selectedColor);
        m_ColorBlock.disabledColor = EditorGUILayout.ColorField("disabledColor:", m_ColorBlock.disabledColor);


        m_EditorGameObjectRoot = (GameObject)EditorGUILayout.ObjectField(m_EditorGameObjectRoot, typeof(GameObject), false);
        if(GUILayout.Button("设置所有按钮过渡颜色"))
        {
            OnSetAllButtonColorBlock(m_EditorGameObjectRoot, m_ColorBlock);
        }


        
        GUILayout.EndVertical();



        GUILayout.EndScrollView();
        
    }

    private void OnSetAllButtonColorBlock(GameObject root, ColorBlock colorBlock)
    {
        var btns = root.GetComponentsInChildren<UnityEngine.UI.Button>();
        foreach(var btn in btns)
        {
            btn.colors = colorBlock;
        }

    }
}

