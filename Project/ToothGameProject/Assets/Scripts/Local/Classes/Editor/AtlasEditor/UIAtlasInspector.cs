using UnityEngine;
using System.Collections;
using UnityEditor;
using LCL;

[CustomEditor(typeof(UIAtals))]
public class UIAtlasInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        UIAtals atlas = target as UIAtals;

        EditorGUI.BeginChangeCheck();

        atlas.mainTexture = EditorGUILayout.ObjectField("MainTexture", atlas.mainTexture, typeof(Texture2D), true) as Texture2D;

        if (GUILayout.Button("刷新数据"))
        {
            if (atlas.mainTexture == null)
            {
                string path = EditorUtility.OpenFilePanel("选着一张图集", Application.dataPath, "png");
                if (!string.IsNullOrEmpty(path))
                {
                    atlas.mainTexture = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
                }
            }
            if (atlas.mainTexture != null)
            {
                string path = AssetDatabase.GetAssetPath(atlas.mainTexture);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    Object[] objs = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(atlas.mainTexture));
                    atlas.spriteLists.Clear();
                    foreach (Object o in objs)
                    {
                        if (o.GetType() == typeof(Texture2D))
                        {
                            atlas.mainTexture = o as Texture2D;
                        }
                        else if (o.GetType() == typeof(Sprite))
                        {
                            Sprite sprite = o as Sprite;
                            atlas.spriteLists.Add(sprite);
                        }
                    }
                }
                else
                {
                    atlas.mainTexture = null;
                }
            }

        }
        int count = atlas.spriteLists.Count;
        for (int i = 0; i < count; ++i)
        {
            Sprite s = atlas.spriteLists[i];
            EditorGUILayout.ObjectField(s.name, s, typeof(Sprite), true);
        }
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}