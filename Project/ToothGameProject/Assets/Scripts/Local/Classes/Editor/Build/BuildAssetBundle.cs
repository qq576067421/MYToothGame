using GameDll;
using Microsoft.International.Converters.PinYinConverter;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class BuildAssetBundle
{
    private enum DependencyValidationStatus
    {
        Safe,
        Unsafe,
    }

    [MenuItem("Build/AssetBundles/Clear Cache")]
    static public void CleanLocalCache()
    {
        Caching.ClearCache();
    }


    [MenuItem("Build/AssetBundles/Android打包代码(自动命名)")]
    static public void BuildCodeConfigWithAutoNameAndroid()
    {
        BuildCodeConfig(BuildTarget.Android);
    }
    [MenuItem("Build/AssetBundles/IOS打包代码(自动命名)")]
    static public void BuildCodeConfigWithAutoNameIOS()
    {
        BuildCodeConfig(BuildTarget.iOS);
    }
    [MenuItem("Build/AssetBundles/Windows64打包代码(自动命名)")]
    static public void BuildCodeConfigWithAutoNameWindows()
    {
        BuildCodeConfig(BuildTarget.StandaloneWindows64);
    }
    [MenuItem("Build/AssetBundles/Mac打包代码(自动命名)")]
    static public void BuildCodeConfigWithAutoNameMac()
    {
        BuildCodeConfig(BuildTarget.StandaloneOSX);
    }


    public static void BuildCodeConfig(BuildTarget target)
    {

        string platformName = "android";
        if (BuildTarget.Android == target)
        {
            platformName = "android";
        }
        else if (BuildTarget.iOS == target)
        {
            platformName = "ios";
        }
        else if (BuildTarget.StandaloneWindows == target || BuildTarget.StandaloneWindows64 == target)
        {
            platformName = "windows";
        }
        else if (BuildTarget.StandaloneOSX == target)
        {
            platformName = "mac";
        }
        string outputPath = LCL.MonoTool.GetBuildStreamingAssetsPath() + platformName;
        outputPath += "/codeconfig";


        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        AssetBundleManifest manifest;
#if ZipCodeConfig
        Debug.Log("资源没有使用Unity自带的压缩");
        manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.UncompressedAssetBundle, target);
#else
        Debug.Log("资源使用Unity自带的ChunkBasedCompression压缩");
        manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.ChunkBasedCompression, target);
        
#endif
        ValidateAssetBundleDependencies(manifest, platformName, outputPath);
        //根文件复制为jpg
        File.Copy(outputPath + "/codeconfig", outputPath + "/codeconfig" + LCL.MonoTool.GetAssetbundleSuffix(), true);
        //AssetDatabase.Refresh();
        Debug.Log("AssetBundle built over, path is " + outputPath + " time:" + System.DateTime.Now.ToString());
    }

    private static void ValidateAssetBundleDependencies(AssetBundleManifest manifest, string platformName, string outputPath)
    {
        if (manifest == null)
        {
            Debug.LogError("AssetBundle dependency validation skipped because manifest is null. platform:" + platformName + " path:" + outputPath);
            return;
        }

        var states = new Dictionary<string, DependencyValidationStatus>(System.StringComparer.OrdinalIgnoreCase);
        var cycles = new Dictionary<string, string[]>(System.StringComparer.OrdinalIgnoreCase);
        var reportedCycles = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        var allBundles = manifest.GetAllAssetBundles();

        for (int i = 0; i < allBundles.Length; i++)
        {
            string[] cyclePath;
            if (!TryGetDependencyCycle(manifest, allBundles[i], states, cycles, out cyclePath))
            {
                continue;
            }

            var cycleKey = BuildNormalizedCycleKey(cyclePath);
            if (reportedCycles.Add(cycleKey))
            {
                Debug.LogError("AssetBundle dependency cycle detected after build. platform:" + platformName + " path:" + outputPath + " cycle:" + FormatDependencyPathForLog(cyclePath));
            }
        }

        if (reportedCycles.Count == 0)
        {
            Debug.Log("AssetBundle dependency validation completed. No dependency cycle found. platform:" + platformName + " path:" + outputPath + " bundles:" + allBundles.Length);
        }
    }

    private static bool TryGetDependencyCycle(
        AssetBundleManifest manifest,
        string bundleName,
        Dictionary<string, DependencyValidationStatus> states,
        Dictionary<string, string[]> cycles,
        out string[] cyclePath)
    {
        cyclePath = null;
        if (manifest == null || string.IsNullOrEmpty(bundleName))
        {
            return false;
        }

        DependencyValidationStatus cachedStatus;
        if (states.TryGetValue(bundleName, out cachedStatus))
        {
            if (cachedStatus == DependencyValidationStatus.Unsafe)
            {
                cyclePath = cycles[bundleName];
                return true;
            }

            return false;
        }

        var visitingPath = new List<string>(8);
        var stackIndices = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        return TryValidateBundleDependencies(manifest, bundleName, visitingPath, stackIndices, states, cycles, out cyclePath);
    }

    private static bool TryValidateBundleDependencies(
        AssetBundleManifest manifest,
        string bundleName,
        List<string> visitingPath,
        Dictionary<string, int> stackIndices,
        Dictionary<string, DependencyValidationStatus> states,
        Dictionary<string, string[]> cycles,
        out string[] cyclePath)
    {
        cyclePath = null;

        DependencyValidationStatus cachedStatus;
        if (states.TryGetValue(bundleName, out cachedStatus))
        {
            if (cachedStatus == DependencyValidationStatus.Unsafe)
            {
                cyclePath = cycles[bundleName];
                return true;
            }

            return false;
        }

        int cycleStartIndex;
        if (stackIndices.TryGetValue(bundleName, out cycleStartIndex))
        {
            cyclePath = BuildCyclePath(visitingPath, cycleStartIndex, bundleName);
            return true;
        }

        stackIndices.Add(bundleName, visitingPath.Count);
        visitingPath.Add(bundleName);

        var dependencies = manifest.GetDirectDependencies(bundleName);
        for (int i = 0; i < dependencies.Length; i++)
        {
            string[] dependencyCyclePath;
            if (!TryValidateBundleDependencies(manifest, dependencies[i], visitingPath, stackIndices, states, cycles, out dependencyCyclePath))
            {
                continue;
            }

            states[bundleName] = DependencyValidationStatus.Unsafe;
            cycles[bundleName] = dependencyCyclePath;
            visitingPath.RemoveAt(visitingPath.Count - 1);
            stackIndices.Remove(bundleName);
            cyclePath = dependencyCyclePath;
            return true;
        }

        visitingPath.RemoveAt(visitingPath.Count - 1);
        stackIndices.Remove(bundleName);
        states[bundleName] = DependencyValidationStatus.Safe;
        return false;
    }

    private static string[] BuildCyclePath(List<string> visitingPath, int cycleStartIndex, string repeatedBundleName)
    {
        var cycleLength = visitingPath.Count - cycleStartIndex;
        var cyclePath = new string[cycleLength + 1];
        for (int i = 0; i < cycleLength; i++)
        {
            cyclePath[i] = visitingPath[cycleStartIndex + i];
        }

        cyclePath[cycleLength] = repeatedBundleName;
        return cyclePath;
    }

    private static string BuildNormalizedCycleKey(string[] cyclePath)
    {
        if (cyclePath == null || cyclePath.Length == 0)
        {
            return string.Empty;
        }

        var nodeCount = cyclePath.Length - 1;
        if (nodeCount <= 0)
        {
            return cyclePath[0];
        }

        var bestStart = 0;
        for (int candidateStart = 1; candidateStart < nodeCount; candidateStart++)
        {
            for (int offset = 0; offset < nodeCount; offset++)
            {
                var compare = System.StringComparer.OrdinalIgnoreCase.Compare(
                    cyclePath[(candidateStart + offset) % nodeCount],
                    cyclePath[(bestStart + offset) % nodeCount]);

                if (compare < 0)
                {
                    bestStart = candidateStart;
                    break;
                }

                if (compare > 0)
                {
                    break;
                }
            }
        }

        var orderedPath = new List<string>(nodeCount + 1);
        for (int offset = 0; offset < nodeCount; offset++)
        {
            orderedPath.Add(cyclePath[(bestStart + offset) % nodeCount]);
        }

        orderedPath.Add(cyclePath[bestStart]);
        return string.Join(" -> ", orderedPath.ToArray());
    }

    private static string FormatDependencyPathForLog(string[] cyclePath)
    {
        if (cyclePath == null || cyclePath.Length == 0)
        {
            return "<unknown>";
        }

        return string.Join(" -> ", cyclePath);
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    [MenuItem("Build/AssetBundles/打包所有资源 %#e")]
    static public void BuildAllResources()
    {
        if (EditorUtility.DisplayDialog("警告", "需要打包“全部”资源？", "OK", "Cancel"))
        {
            SmallNameOutFile();
            RemoveAllAssetBundleNames();
            NameAssetBundle(EditorTool.GetArtOutPath(), EditorTool.GetArtOutPath() + "/");
            //UIAtalsEditor.CreateSpriteAtlasRoot();
            BuildAllResourcesImp();
        }
    }

    static public void BuildAllResourcesForPlayer()
    {
        SmallNameOutFile();
        RemoveAllAssetBundleNames();
        NameAssetBundle(EditorTool.GetArtOutPath(), EditorTool.GetArtOutPath() + "/");
        NameTextureSets();
        //UIAtalsEditor.CreateSpriteAtlasRoot();
        BuildAllResourcesImp();
    }

    [MenuItem("Build/AssetBundles/打包时自动重新生成图集命名")]
    private static void NameTextureSets()
    {
        var ts = EditorTool.GetArtOutPath() + "/texture_set";
        string art_out = EditorTool.GetArtOutPath();
        string art_out_slash = art_out + "/";

        var dirs = Directory.GetDirectories(ts, "*", SearchOption.TopDirectoryOnly);
        foreach (var dir in dirs)
        {
            var fullPath = Path.GetFullPath(dir);
            string path = EditorTool.GetAssetsRelativePath(fullPath);
            UIAtalsEditor.CreateAtlasOfAssetDir(path);
        }
    }

    [MenuItem("Build/AssetBundles/打包部分资源 %#w")]
    static public void BuildResourceForDevelop()
    {
        if (EditorUtility.DisplayDialog("警告", "需要打包部分资源？", "OK", "Cancel"))
        {
            BuildAllResourcesImp();
        }
    }
    static private void RemoveAllAssetBundleNames()
    {
        var names = AssetDatabase.GetAllAssetBundleNames();
        foreach (var name in names)
        {
            AssetDatabase.RemoveAssetBundleName(name, true);
        }
    }
    static private void SmallNameOutFile()
    {
        var root_dir = EditorTool.GetArtOutPath();
        RenameDirAndFilesImp(root_dir);
    }

    private static void BuildAllResourcesImp()
    {
#if UNITY_IPHONE
        BuildCodeConfigWithAutoNameIOS();
#elif UNITY_ANDROID
        BuildCodeConfigWithAutoNameAndroid();
#elif UNITY_STANDALONE_OSX
        BuildCodeConfigWithAutoNameMac();
#elif UNITY_STANDALONE_WIN
        BuildCodeConfigWithAutoNameWindows();
#endif
    }
    [MenuItem("Assets/选择资源删除AssetBundleName")]
    static public void ClearSelectionResourcesAssetBundleName()
    {
        if (EditorUtility.DisplayDialog("警告", "是否需要清理选择资源的AssetBundleName", "OK", "Cancel"))
        {
            UnityEngine.Object[] selObj = Selection.GetFiltered(typeof(Object), SelectionMode.Unfiltered);
            foreach (Object item in selObj)
            {
                string objPath = AssetDatabase.GetAssetPath(item);
                if (item is DefaultAsset)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(objPath);
                    ClearAbNameImp(dirInfo);
                }
                else
                {
                    AssetImporter ai = AssetImporter.GetAtPath(objPath);
                    if (ai != null)
                    {
                        ai.assetBundleName = "";
                        if (!string.IsNullOrEmpty(ai.assetBundleVariant))
                        {
                            ai.assetBundleVariant = "";
                        }
                    }
                }

            }
            AssetDatabase.Refresh();
            Debug.Log("******批量删除ABName成功******");
        }
    }
    static void ClearAbNameImp(DirectoryInfo dirInfo)
    {
        //判断文件夹是否有abname
        {
            string filePath = dirInfo.FullName.Replace('\\', '/');
            filePath = filePath.Replace(Application.dataPath, "Assets");

            AssetImporter ai = AssetImporter.GetAtPath(filePath);
            if (ai != null)
            {
                ai.assetBundleName = "";
                if(!string.IsNullOrEmpty(ai.assetBundleVariant))
                {
                    ai.assetBundleVariant = "";
                }

            }
        }
        FileSystemInfo[] files = dirInfo.GetFileSystemInfos();
        foreach (FileSystemInfo file in files)
        {
            string filePath = file.FullName.Replace('\\', '/');
            filePath = filePath.Replace(Application.dataPath, "Assets");
            AssetImporter ai = AssetImporter.GetAtPath(filePath);
            if (ai != null)
            {
                ai.assetBundleName = "";
                if (!string.IsNullOrEmpty(ai.assetBundleVariant))
                {
                    ai.assetBundleVariant = "";
                }
            }
            if (file is DirectoryInfo)
            {
                ClearAbNameImp(file as DirectoryInfo);
            }
        }
    }

    //%f代表ctrl+f快捷键 &代表alt  #代表shift
    [MenuItem("Assets/选择资源快速打包 &z")]
    static public void BuildSelectionResourcesQuick()
    {
        int selValideCount = NameSelectionResourcesQuick();
        if (selValideCount > 0)
        {
            BuildAllResourcesImp();
        }
        else
        {
            if (EditorUtility.DisplayDialog("警告", "没有选择需要打包的资源,请问仍然需要打包吗？", "OK", "Cancel"))
            {
                BuildAllResourcesImp();
            }
            else
            {
                Debug.LogWarning("没有选择需要打包的资源");
            }
        }
    }
    [MenuItem("Assets/美术策划工具/小写英文命名(去特殊字符)")]
    static public void RenameDirAndFiles()
    {
        Object[] sels = Selection.objects;
        string art_out = "Assets/art/out";
        foreach (var obj in sels)
        {
            if (obj is DefaultAsset)
            {
                string _source = AssetDatabase.GetAssetPath(obj);
                RenameDirAndFilesImp(_source);
            }
            else
            {
                string _source = AssetDatabase.GetAssetPath(obj);
                Rename(_source);
            }

        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("小写英文命名(去特殊字符) ok");
    }
    static private void RenameDirAndFilesImp(string dir)
    {
        var subDirs = AssetDatabase.GetSubFolders(dir);
        if (subDirs != null || subDirs.Length > 0)
        {
            foreach (var subDir in subDirs)
            {
                RenameDirAndFilesImp(subDir);
            }
        }
        //当该路径已经没有文件夹的时候，就开始重命名文件
        DirectoryInfo direction = new DirectoryInfo(dir);
        FileInfo[] files = direction.GetFiles("*");
        for (int i = 0; i < files.Length; i++)
        {
            if (IsFilterExt(files[i].Name))
            {
                continue;
            }
            else
            {
                string path = files[i].FullName.Replace("\\", "/").Replace(Application.dataPath, "");
                path = "Assets" + path;
                Rename(path);
            }
        }
        Rename(dir);
    }
    private static List<string> m_NotRenameFiles = new List<string>() {
        "LightingData",
        "Lightmap",
        "NavMesh",
        "ReflectionProbe"

    };
    private static void Rename(string path)
    {
        string filename = Path.GetFileNameWithoutExtension(path);

        foreach (var name in m_NotRenameFiles)
        {
            if (filename.ToLower().Contains(name.ToLower()))
            {
                return;
            }
        }
        bool rename = false;
        if (Regex.IsMatch(filename, EditorTool.m_PinYinWords))
        {
            filename = ConvertPinYin(filename);
            rename = true;
        }
        if (System.Text.RegularExpressions.Regex.IsMatch(filename, EditorTool.m_ABCWords))
        {
            filename = filename.ToLower();
            rename = true;
        }
        if (System.Text.RegularExpressions.Regex.IsMatch(filename, EditorTool.m_SpecialURLWords))
        {
            Regex reg = new Regex(EditorTool.m_SpecialURLWords);
            filename = reg.Replace(filename, "_");
            rename = true;
        }
        if (System.Text.RegularExpressions.Regex.IsMatch(filename, EditorTool.m_ChinesePunctuation))
        {
            Regex reg = new Regex(EditorTool.m_ChinesePunctuation);
            filename = reg.Replace(filename, "_");
            rename = true;
        }
        //最后检查，是否只包含小写英文、数字、下划线等等，如果不是，那就把它换成下划线
        if (Regex.IsMatch(filename, EditorTool.m_NotEnglishWords))
        {
            filename = Regex.Replace(filename, EditorTool.m_NotEnglishWords, "_");
            rename = true;
        }
        if (rename)
        {
            AssetDatabase.RenameAsset(path, filename);
        }

    }
    private static string ConvertPinYin(string ch)
    {
        string r = string.Empty;
        foreach (char obj in ch)
        {
            try
            {
                ChineseChar chineseChar = new ChineseChar(obj);
                string t = chineseChar.Pinyins[0].ToString();
                r += t.Substring(0, t.Length - 1);
            }
            catch
            {
                r += obj.ToString();
            }
        }
        r = r.ToLower();
        return r;
    }
    [MenuItem("Assets/选择资源快速名ABName &n")]
    static public int NameSelectionResourcesQuick()
    {
        //快速命名零散小包
        Object[] sels = Selection.objects;
        string art_out = EditorTool.GetArtOutPath();
        string art_out_slash = art_out + "/";
        int selValideCount = 0;
        foreach (var obj in sels)
        {
            string _source = AssetDatabase.GetAssetPath(obj);
            if (!_source.Contains(art_out))
            {
                Debug.LogWarning("为了方便管理，请最好将预制件放到" + art_out + "目录下面。");
                continue;
            }
            if (obj is DefaultAsset)
            {
                selValideCount = NameAssetBundle(_source, art_out_slash);
            }
            else
            {
                string assetName = _source.Replace(art_out_slash, "");
                if(IsSpecialPath(assetName))
                {
                    continue;
                }
                if (IsFilterExt(assetName))
                {
                    continue;
                }
                else if (HasSpecialWord(assetName))
                {
                    continue;
                }
                selValideCount += NameSelectionImp(assetName, _source);
            }


        }
        Debug.Log("选择资源快速名ABName ok");
        return selValideCount;
    }
    [MenuItem("Assets/选择文件夹设置ABName")]
    static public int NameSelectionFloders()
    {
        if (!EditorUtility.DisplayDialog("警告", "你确定要对当前选择的【文件夹】进行AB命名吗？", "OK", "Cancel"))
        {
            Debug.Log("文件夹命名AB取消");
            return 0;
        }
        //快速命名零散小包
        Object[] sels = Selection.objects;
        string art_out = EditorTool.GetArtOutPath();
        string art_out_slash = art_out + "/";
        int selValideCount = 0;
        foreach (var obj in sels)
        {
            string _source = AssetDatabase.GetAssetPath(obj);
            if (!_source.Contains(art_out))
            {
                Debug.LogWarning("为了方便管理，请最好将预制件放到" + art_out + "目录下面。");
                continue;
            }
            if (obj is DefaultAsset)
            {
                if (_source.ToLower().Contains("_lcl_noab_"))
                {
                    continue;
                }

                string assetName = _source.Replace(art_out_slash, "");
                AssetImporter assetImporter = AssetImporter.GetAtPath(_source);
                assetImporter.assetBundleName = assetName + LCL.MonoTool.GetAssetbundleSuffix();
                selValideCount++;
            }
        }
        Debug.Log("选择文件夹设置ABName ok");
        return selValideCount;
    }
    static bool IsSpecialPath(string path)
    {
        var full_name = path.ToLower();
        if (full_name.Contains("_lcl_noab_") || full_name.Contains("~"))
        {
            return true;
        }
        return false;
    }
    //从文件夹开始
    static private int NameAssetBundle(string _source, string art_out_slash)
    {
        var full_art_out_slash = Path.GetFullPath(art_out_slash);
        int selValideCount = 0;
        DirectoryInfo direction = new DirectoryInfo(_source);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i].FullName.Replace(full_art_out_slash, "").Replace("\\", "/");
            if (IsSpecialPath(file))
            {
                continue;
            }
            if (IsFilterExt(files[i].Name))
            {
                continue;
            }
            else if (HasSpecialWord(files[i].Name))
            {
                continue;
            }
            else
            { 
                string path = EditorTool.GetAssetsRelativePath(files[i].FullName);
                string assetName = path.Replace(art_out_slash, "");
                selValideCount += NameSelectionImp(assetName, path);
            }
        }
        return selValideCount;
    }
    static private bool HasSpecialWord(string filename)
    {
        var ext = Path.GetExtension(filename);
        if (!string.IsNullOrEmpty(ext))
        {
            filename = filename.Replace(ext, "");
        }
        if (Regex.IsMatch(filename, EditorTool.m_PinYinWords))
        {
            Debug.LogError("含有中文字符串， 字符串是：" + filename);
            return true;
        }
        if (System.Text.RegularExpressions.Regex.IsMatch(filename, EditorTool.m_ABCWords))
        {
            Debug.LogError("含有大写字母， 字符串是：" + filename);
            return true;
        }
        if (System.Text.RegularExpressions.Regex.IsMatch(filename, EditorTool.m_SpecialURLWords))
        {
            Debug.LogError("含有特殊字符串， 字符串是：" + filename);
            return true;
        }
        if (System.Text.RegularExpressions.Regex.IsMatch(filename, EditorTool.m_ChinesePunctuation))
        {
            Debug.LogError("含有特殊字符串， 字符串是：" + filename);
            return true;
        }
        if (Regex.IsMatch(filename, EditorTool.m_NotEnglishWords))
        {
            //Debug.LogError("含有特殊字符串， 字符串是：" + filename);
            var invalidChars = Regex.Matches(filename, EditorTool.m_NotEnglishWords)
                       .Cast<Match>()
                       .Select(m => m.Value)
                       .Distinct()
                       .ToArray();
            Debug.LogError($"含有特殊字符串，字符串是：{filename}，特殊字符为：{string.Join(", ", invalidChars)}");
            return true;
        }
        return false;
    }
    static private bool IsFilterExt(string filename)
    {
        filename = filename.ToLower();
        if (filename.EndsWith(".meta") ||
            filename.EndsWith(".fbx") ||
            filename.EndsWith(".mdb") ||
            filename.EndsWith(".pdb") ||
            filename.EndsWith(".obj") ||
            filename.EndsWith(".bin") ||
            filename.EndsWith(".cs") ||
            filename.EndsWith(".js") ||
            filename.EndsWith(".csv") ||
            //fileName.EndsWith(".shader") ||
            filename.EndsWith(".cginc") ||
            filename.EndsWith(".exr") ||
            filename.EndsWith(".dll.bytes") ||
            filename.EndsWith(".glslinc"))
        {
            return true;
        }

        foreach (var partName in m_NotRenameFiles)
        {
            if (filename.Contains(partName.ToLower()))
            {
                return true;
            }
        }
        string name = Path.GetFileNameWithoutExtension(filename);
        string dir = Path.GetDirectoryName(filename);
        if (name.Contains(".") || dir.Contains("."))
        {
            return true;
        }

        return false;
    }
    static public int NameSelectionImp(string assetName, string _source)
    {
        string _assetPath = _source;

        //Debug.Log (_assetPath);  

        //在代码中给资源设置AssetBundleName  
        AssetImporter ai = AssetImporter.GetAtPath(_assetPath);
        if(ai == null)
        {
            Debug.LogError("命名AB错误，路径不存在文件：" + _assetPath);
            return 0;
        }
        if (ai is TextureImporter)
        {

            if (_assetPath.Contains("texture_set/"))
            {
                return 0;
            }

            string ext = Path.GetExtension(assetName);
            if (string.IsNullOrEmpty(ext))
            {
                Debug.LogError("文件没有后缀，原则上不支持打ab");
                return 0;
            }
            assetName = assetName.Replace(ext, "");

            if (_assetPath.ToLower().Contains("out/texture") ||
                _assetPath.ToLower().Contains("out/texture_set"))
            {

            }
            else
            {
                assetName = assetName + "_tex";
            }

            ai.assetBundleName = assetName + LCL.MonoTool.GetAssetbundleSuffix();
            if (!string.IsNullOrEmpty(ai.assetBundleVariant))
            {
                ai.assetBundleVariant = "";
            }
            return 1;
        }
        else
        {
            string ext = Path.GetExtension(assetName);
            if (string.IsNullOrEmpty(ext))
            {
                Debug.LogError("文件没有后缀，原则上不支持打ab");
                return 0;
            }
            if(ext.ToLower() == ".spriteatlas")
            {
                return 0;
            }
            assetName = assetName.Replace(ext, "");
            if (ext.ToLower() == ".mat")
            {
                assetName = assetName + "_mat";
            }
            else if (ext.ToLower() == ".anim")
            {
                assetName = assetName + "_anim";
            }
            else if (ext.ToLower() == ".controller")
            {
                assetName = assetName + "_controller";
            }

            ai.assetBundleName = assetName + LCL.MonoTool.GetAssetbundleSuffix();
            if (!string.IsNullOrEmpty(ai.assetBundleVariant))
            {
                ai.assetBundleVariant = "";
            }
            return 1;
        }

    }


}
