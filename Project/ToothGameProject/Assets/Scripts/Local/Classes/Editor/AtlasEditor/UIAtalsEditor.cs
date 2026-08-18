using UnityEngine;
using System.Collections;
using UnityEditor;
using LCL;
using System.IO;
using System.Collections.Generic;
using UnityEngine.U2D;
using UnityEditor.U2D;

public class UIAtalsEditor
{
    [MenuItem("Tools/Texture/创建当前选中文件夹(支持多选)图集(SpriteAtlas)")]
    public static void CreateSpriteAtlas()
    {
        if (Selection.objects == null || Selection.objects.Length == 0)
        {
            Debug.LogError("没有选择任何文件夹");
            return;
        }
        if (!EditorUtility.DisplayDialog("警告", "创建当前选中文件夹图集(Atlas)？", "OK", "Cancel"))
        {
            return;
        }
        foreach (var obj in Selection.objects)
        {
            if (obj is DefaultAsset)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                CreateAtlasOfAssetDir(path);
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("创建当前选中文件夹图集(SpritePacker)完成");
    }

    static SpriteAtlasPackingSettings packSetting = new SpriteAtlasPackingSettings()
    {
        blockOffset = 1,
        padding = 2,
        enableRotation = false,
        enableTightPacking = false
    };

    private static SpriteAtlasTextureSettings textureSetting = new SpriteAtlasTextureSettings()
    {
        sRGB = true,
        filterMode = FilterMode.Bilinear,
    };

    private static TextureImporterPlatformSettings importerSetting = new TextureImporterPlatformSettings()
    {
        maxTextureSize = 2048,
        compressionQuality = 50,
        format = TextureImporterFormat.ASTC_6x6,
    };
    public static void CreateAtlasOfAssetDir(string dirAssetPath)
    {
        if (string.IsNullOrEmpty(dirAssetPath) || Path.HasExtension(dirAssetPath))
        {
            Debug.LogError("当前选中对象不是文件夹，请选择对应文件夹重新创建图集");
            return;
        }

        SpriteAtlas atlas = new SpriteAtlas();
        atlas.SetPackingSettings(packSetting);
        atlas.SetTextureSettings(textureSetting);
        atlas.SetPlatformSettings(importerSetting);
        atlas.SetIncludeInBuild(true);

        var atlasPath = $"{dirAssetPath}.spriteatlas";
        TryAddSprites(atlas, dirAssetPath);
        AssetDatabase.CreateAsset(atlas, atlasPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(atlasPath);
        //设置文件夹的ab
        AssetImporter assetImporter = AssetImporter.GetAtPath(dirAssetPath);
        if (assetImporter != null)
        {
            var assetName = dirAssetPath.ToLower().Replace(EditorTool.GetArtOutPath().ToLower() + "/", "") + MonoTool.GetAssetbundleSuffix();
            assetImporter.assetBundleName = assetName;
        }
        //取消atlas文件的ab
        assetImporter = AssetImporter.GetAtPath(atlasPath);
        if (assetImporter != null)
        {
            assetImporter.assetBundleName = "";
        }
    }

    static void TryAddSprites(SpriteAtlas atlas, string dirPath)
    {
        bool add_dir = true;
        if (!add_dir)
        {
            string[] files = Directory.GetFiles(dirPath);
            if (files == null || files.Length == 0) return;

            Sprite sprite;
            List<Sprite> spriteList = new List<Sprite>();
            foreach (var file in files)
            {
                if (file.EndsWith(".meta")) continue;
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                if (sprite == null) continue;
                spriteList.Add(sprite);
            }
            if (spriteList.Count > 0) atlas.Add(spriteList.ToArray());
        }
        else
        {
            var texture = AssetDatabase.LoadAssetAtPath(dirPath, typeof(DefaultAsset));
            SpriteAtlasExtensions.Add(atlas, new Object[] { texture });
        }
    }
















    [MenuItem("Tools/Texture/清理当前选中文件夹(支持多选)图集(SpritePacker PackingTag)", false, 99)]
    public static void ClearSpritePackingTag()
    {
        if (Selection.objects == null || Selection.objects.Length == 0)
        {
            Debug.LogError("没有选择任何文件夹");
            return;
        }
        if (!EditorUtility.DisplayDialog("警告", "清理当前选中文件夹(支持多选)图集(SpritePacker PackingTag)？", "OK", "Cancel"))
        {
            return;
        }
        foreach (var obj in Selection.objects)
        {
            if (obj is DefaultAsset)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                ClearSpritePackingTag(path);
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("创建当前选中文件夹图集(SpritePacker)完成");
    }

    public static void ClearSpritePackingTag(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string[] pngFiles = Directory.GetFiles(fullPath, "*.png", SearchOption.AllDirectories);
        string atlasName = path.Replace(EditorTool.GetArtOutPath() + "/", "");
        foreach (string png in pngFiles)
        {
            string pngAssetPath = "Assets/" + png.Replace("\\", "/").Replace(Application.dataPath + "/", "");
            TextureImporter ti = TextureImporter.GetAtPath(pngAssetPath) as TextureImporter;
            if (ti != null)
            {
                if(string.IsNullOrEmpty(ti.spritePackingTag) && string.IsNullOrEmpty(ti.assetBundleName))
                {
                    continue;
                }
                ti.spritePackingTag = "";
                ti.assetBundleName = "";
                ti.SaveAndReimport();
            }
        }
    }



    [MenuItem("Tools/Texture/拆分当前(支持多选)图集", false, 99)]
    public static void SplitSpriteAtlas()
    {
        if (Selection.objects == null || Selection.objects.Length == 0)
        {
            Debug.LogError("没有选择任何文件夹");
            return;
        }
        if (!EditorUtility.DisplayDialog("警告", "拆分当前图集？", "OK", "Cancel"))
        {
            return;
        }
        foreach (var obj in Selection.objects)
        {
            Texture2D image = obj as Texture2D;
            if(image == null)
            {
                Debug.LogError("当前选择的不是图片:" + obj.name);
                continue;
            }
            string path = AssetDatabase.GetAssetPath(obj); //文件所在的路径
            TextureImporter texImp = AssetImporter.GetAtPath(path) as TextureImporter;
            string fileNameWithoutExt = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path);
            foreach (SpriteMetaData metaData in texImp.spritesheet)
            {
                Texture2D myimage = new Texture2D((int)metaData.rect.width, (int)metaData.rect.height);

                for (int y = (int)metaData.rect.y; y < metaData.rect.y + metaData.rect.height; y++)
                {
                    for (int x = (int)metaData.rect.x; x < metaData.rect.x + metaData.rect.width; x++)
                        myimage.SetPixel(x - (int)metaData.rect.x, y - (int)metaData.rect.y, image.GetPixel(x, y));
                }
                if (myimage.format != TextureFormat.ARGB32 && myimage.format != TextureFormat.RGB24)
                {
                    Texture2D newTexture = new Texture2D(myimage.width, myimage.height);
                    newTexture.SetPixels(myimage.GetPixels(0), 0);
                    myimage = newTexture;
                }
                var pngData = myimage.EncodeToPNG();
                File.WriteAllBytes(fileNameWithoutExt + "_" + metaData.name + ".PNG", pngData);
                AssetDatabase.Refresh();
            }
        }
        Debug.Log("拆分当前图集完成");
    }

    [MenuItem("Tools/Texture/修正Texture(防止AB打包Sprite资源问题)", false, 99)]
    public static void FixTexture()
    {
        string directory = "Assets/art/out/texture/";

        string[] subFolders = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);
        string[] guids = null;
        string[] assetPaths = null;
        int i = 0, iMax = 0;
        foreach (var folder in subFolders)
        {
            guids = AssetDatabase.FindAssets("t:Sprite t:Texture", new string[] { folder });
            assetPaths = new string[guids.Length];
            for (i = 0, iMax = assetPaths.Length; i < iMax; ++i)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                Debug.Log(assetPaths[i]);
                string pngAssetPath = assetPaths[i];
                TextureImporter ti = TextureImporter.GetAtPath(pngAssetPath) as TextureImporter;
                bool dirty = false;
                if (ti.textureType != TextureImporterType.Default)
                {
                    dirty = true;
                    ti.textureType = TextureImporterType.Default;
                }
                if (!string.IsNullOrEmpty(ti.spritePackingTag))
                {
                    dirty = true;
                    ti.spritePackingTag = "";
                }
                if (dirty)
                {
                    ti.SaveAndReimport();
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("修正完毕");
    }
}