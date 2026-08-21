using UnityEngine;

using UnityEditor;
using System.Collections.Generic;



// ///////////////////////////////////////////////////////////////////////////////////////////////////////// 
// 
// Batch Texture import settings modifier. 
// 
// Modifies all selected textures in the project window and applies the requested modification on the 

// textures. Idea was to have the same choices for multiple files as you would have if you open the 

// import settings of a single texture. Put this into Assets/Editor and once compiled by Unity you find 

// the new functionality in Custom -> Texture. Enjoy! :-) 

// 

// Based on the great work of benblo in this thread: 
// http://forum.unity3d.com/viewtopic.php?t=16079&start=0&postdays=0&postorder=asc&highlight=textureimporter 
// 
// Developed by Martin Schultz, Decane in August 2009 
// e-mail: ms@decane.net 
// 
// Updated for Unity 3.0 by col000r in August 2010 
// http://col000r.blogspot.com 
// 
// ///////////////////////////////////////////////////////////////////////////////////////////////////////// 

public class ChangeTextureImportSettingsUnity3 : ScriptableObject
{



    [MenuItem("Tools/Texture/Change Texture Format/Auto Compressed")]

    static void ChangeTextureFormat_AutoCompressed()
    {

        SelectedChangeTextureFormatSettings(TextureImporterFormat.AutomaticCompressed);

    }
    [MenuItem("Tools/Texture/Change Texture Format/ASTC_6x6")]

    static void ChangeTextureFormat_ASTC_6x6()
    {

        SelectedChangeTextureFormatSettings(TextureImporterFormat.ASTC_6x6);

    }
    [MenuItem("Tools/Texture/Change Texture Format/ETC2_8Bit")]

    static void ChangeTextureFormat_ETC2_8Bit()
    {

        SelectedChangeTextureFormatSettings(TextureImporterFormat.ETC2_RGBA8);

    }
    [MenuItem("Tools/Texture/Change Texture Format/PVRTC4")]
    static void ChangeTextureFormat_PVRTC4()
    {

        SelectedChangeTextureFormatSettings(TextureImporterFormat.PVRTC_RGBA4);

    }
    // —————————————————————————- 



    [MenuItem("Tools/Texture/Change Texture Size/Change Max Texture Size/32")]

    static void ChangeTextureSize_32()
    {

        SelectedChangeMaxTextureSize(32);

    }
    [MenuItem("Tools/Texture/Change Texture Size/Change Max Texture Size/64")]

    static void ChangeTextureSize_64()
    {

        SelectedChangeMaxTextureSize(64);

    }
    [MenuItem("Tools/Texture/Change Texture Size/Change Max Texture Size/128")]

    static void ChangeTextureSize_128()
    {

        SelectedChangeMaxTextureSize(128);

    }
    [MenuItem("Tools/Texture/Change Texture Size/Change Max Texture Size/256")]

    static void ChangeTextureSize_256()
    {

        SelectedChangeMaxTextureSize(256);

    }
    [MenuItem("Tools/Texture/Change Texture Size/Change Max Texture Size/512")]

    static void ChangeTextureSize_512()
    {

        SelectedChangeMaxTextureSize(512);

    }
    [MenuItem("Tools/Texture/Change Texture Size/Change Max Texture Size/1024")]

    static void ChangeTextureSize_1024()
    {

        SelectedChangeMaxTextureSize(1024);

    }
    [MenuItem("Tools/Texture/Change Texture Size/Change Max Texture Size/2048")]

    static void ChangeTextureSize_2048()
    {

        SelectedChangeMaxTextureSize(2048);

    }



    // —————————————————————————- 



    [MenuItem("Tools/Texture/Change MipMap/Enable MipMap")]

    static void ChangeMipMap_On()
    {

        SelectedChangeMimMap(true);

    }



    [MenuItem("Tools/Texture/Change MipMap/Disable MipMap")]

    static void ChangeMipMap_Off()
    {

        SelectedChangeMimMap(false);

    }



    // —————————————————————————- 





    [MenuItem("Tools/Texture/Change Non Power of 2/None")]

    static void ChangeNPOT_None()
    {

        SelectedChangeNonPowerOf2(TextureImporterNPOTScale.None);

    }



    [MenuItem("Tools/Texture/Change Non Power of 2/ToNearest")]

    static void ChangeNPOT_ToNearest()
    {

        SelectedChangeNonPowerOf2(TextureImporterNPOTScale.ToNearest);

    }



    [MenuItem("Tools/Texture/Change Non Power of 2/ToLarger")]

    static void ChangeNPOT_ToLarger()
    {

        SelectedChangeNonPowerOf2(TextureImporterNPOTScale.ToLarger);

    }



    [MenuItem("Tools/Texture/Change Non Power of 2/ToSmaller")]

    static void ChangeNPOT_ToSmaller()
    {

        SelectedChangeNonPowerOf2(TextureImporterNPOTScale.ToSmaller);

    }



    // —————————————————————————- 



    [MenuItem("Tools/Texture/Change Is Readable/Enable")]

    static void ChangeIsReadable_Yes()
    {

        SelectedChangeIsReadable(true);

    }



    [MenuItem("Tools/Texture/Change Is Readable/Disable")]

    static void ChangeIsReadable_No()
    {

        SelectedChangeIsReadable(false);

    } //Unity3D教程手册：www.unitymanual.com 



    // —————————————————————————- 



    static void SelectedChangeIsReadable(bool enabled)
    {



        Object[] textures = GetSelectedTextures();

        Selection.objects = new Object[0];

        foreach (Texture2D texture in textures)
        {

            string path = AssetDatabase.GetAssetPath(texture);

            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            textureImporter.isReadable = enabled;

            AssetDatabase.ImportAsset(path);

        }

    }





    static void SelectedChangeNonPowerOf2(TextureImporterNPOTScale npot)
    {



        Object[] textures = GetSelectedTextures();

        Selection.objects = new Object[0];

        foreach (Texture2D texture in textures)
        {

            string path = AssetDatabase.GetAssetPath(texture);

            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            textureImporter.npotScale = npot;

            AssetDatabase.ImportAsset(path);

        }

    }



    static void SelectedChangeMimMap(bool enabled)
    {



        Object[] textures = GetSelectedTextures();

        Selection.objects = new Object[0];

        foreach (Texture2D texture in textures)
        {

            string path = AssetDatabase.GetAssetPath(texture);

            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            textureImporter.mipmapEnabled = enabled;

            AssetDatabase.ImportAsset(path);

        }

    }

    //Unity3D教程手册：www.unitymanual.com 

    static void SelectedChangeMaxTextureSize(int size)
    {



        Object[] textures = GetSelectedTextures();

        Selection.objects = new Object[0];

        foreach (Texture2D texture in textures)
        {

            string path = AssetDatabase.GetAssetPath(texture);

            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            textureImporter.maxTextureSize = size;

            AssetDatabase.ImportAsset(path);

        }

    }



    static void SelectedChangeTextureFormatSettings(TextureImporterFormat newFormat)
    {
        Object[] textures = GetSelectedTextures();
        string title = "转换图片格式->" + newFormat;
        var changedPaths = new List<string>();
        var plats = new List<string>() { "Android", "iPhone" };

        AssetDatabase.StartAssetEditing(); // 批量模式，避免每次都导入
        try
        {
            for (int i = 0; i < textures.Length; ++i)
            {
                var texture = textures[i];
                if (texture == null) continue;

                string path = AssetDatabase.GetAssetPath(texture);
                var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                if (textureImporter == null) continue;

                bool dirty = false;

                foreach (var plat in plats)
                {
                    var tips = textureImporter.GetPlatformTextureSettings(plat);
                    // 确保名字正确（有些版本需要）
                    tips.name = plat;

                    if (!tips.overridden)
                    {
                        tips.overridden = true;
                        dirty = true;
                    }
                    if (tips.format != newFormat)
                    {
                        tips.format = newFormat;
                        dirty = true;
                    }
                    if (textureImporter.mipmapEnabled)
                    {
                        dirty = true;
                        textureImporter.mipmapEnabled = false;
                    }
                    if (dirty)
                    {
                        textureImporter.SetPlatformTextureSettings(tips);
                    }
                }

                if (dirty)
                {
                    // 仅写入设置，不触发导入
                    AssetDatabase.WriteImportSettingsIfDirty(path);
                    changedPaths.Add(path);
                }

                // 如需进度条，降低刷新频率：每N张刷新一次
                if (i % 50 == 0) EditorUtility.DisplayProgressBar(title, $"转换：{path}", (float)(i + 1) / textures.Length);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing(); // 结束批量，Unity 会一次性导入
                                              // EditorUtility.ClearProgressBar();
        }

        // 如需强制同步导入（大多数情况下不需要，StopAssetEditing已触发导入）
        // foreach (var p in changedPaths)
        //     AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);

        Debug.Log($"转换完毕，共修改 {changedPaths.Count} 张");
    }

    [MenuItem("Tools/Texture/转换选择的图片为NoMipmap_ASTC4X4")]
    static void AutoSelectedChangeTextureSettings()
    {
        Object[] textures = GetSelectedTextures();
        Selection.objects = new Object[0];
        int count = textures.Length;
        for (int i = 0; i < count; ++i)
        {
            var texture = textures[i];
            if (texture != null)
            {
                string path = AssetDatabase.GetAssetPath(texture);
                //Debug.Log("path: " + path); 

                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                if (textureImporter == null)
                {
                    continue;
                }
                bool isDirt = false;
                if (textureImporter.mipmapEnabled)
                {
                    isDirt = true;
                    textureImporter.mipmapEnabled = false;
                }

                List<string> plats = new List<string>() { "Android", "iPhone" };
                foreach (var plat in plats)
                {
                    TextureImporterPlatformSettings tips = textureImporter.GetPlatformTextureSettings(plat);
                    if (!tips.overridden)
                    {
                        isDirt = true;
                        tips.overridden = true;
                    }
                    if (tips.format != TextureImporterFormat.ASTC_4x4)
                    {
                        isDirt = true;
                        tips.format = TextureImporterFormat.ASTC_4x4;
                    }


                    if (isDirt)
                    {
                        textureImporter.SetPlatformTextureSettings(tips);
                    }
                }

                if (isDirt)
                {
                    AssetDatabase.ImportAsset(path);
                }
                //EditorUtility.DisplayProgressBar("转换" + i + "/" + count, "转换：" + path, (float)(i + 1) / (float)count);
            }
        }
        //EditorUtility.ClearProgressBar();
        Debug.Log("转换完毕");
    }

    static Object[] GetSelectedTextures()
    {

        return Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);

    }

}