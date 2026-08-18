using LCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CIBuild
{
    [MenuItem("Build/自动构建Android", false, 501)]
    public static void BuildAPK()
    {
        BuildPlayer.BuildAppAndroid();
    }
}

public class BuildPlayer
{
    public class BuildParams
    {
        public BuildTarget target;
        public BuildTargetGroup targetGroup;
        public bool isDebug = false;
        public bool isAutoRun = false;
        public bool isMono = false;
        public bool aab = false;
        public bool isProject = false;
        public bool third_build = false;
        public bool isJust64 = false;
        public int isIncremental = 0; //0表示未设置过 -1表示不使用 1表示使用
    }
    public class BuildPrepareParams
    {
        public BuildTarget target;
        public bool is_china = true;
    }

    [MenuItem("Build/发布Android", false, 501)]
    public static void BuildAppAndroid()
    {
        BuildTarget target = BuildTarget.Android;
        BuildTargetGroup targetGroup = BuildTargetGroup.Android;

        BuildPrepareParams bpp = new BuildPrepareParams();
        bpp.target = target;
        BuildAppPrepare(bpp);

        BuildParams bp = new BuildParams();
        bp.target = target;
        bp.targetGroup = targetGroup;
        BuildPlayerImp(bp);
    }
    [MenuItem("Build/发布Android(真机直接运行)", false, 501)]
    public static void BuildAppAndroidAutoRun()
    {
        BuildTarget target = BuildTarget.Android;
        BuildTargetGroup targetGroup = BuildTargetGroup.Android;

        BuildPrepareParams bpp = new BuildPrepareParams();
        bpp.target = target;
        BuildAppPrepare(bpp);

        BuildParams bp = new BuildParams();
        bp.target = target;
        bp.targetGroup = targetGroup;
        bp.isDebug = false;
        bp.isAutoRun = true;
        BuildPlayerImp(bp);
    }
    [MenuItem("Build/发布Android(真机直接运行 开发版)", false, 501)]
    public static void BuildAppAndroidAutoRunDevelopment()
    {
        BuildTarget target = BuildTarget.Android;
        BuildTargetGroup targetGroup = BuildTargetGroup.Android;

        BuildPrepareParams bpp = new BuildPrepareParams();
        bpp.target = target;
        BuildAppPrepare(bpp);

        BuildParams bp = new BuildParams();
        bp.target = target;
        bp.targetGroup = targetGroup;
        bp.isDebug = true;
        bp.isAutoRun = true;
        BuildPlayerImp(bp);
    }
    [MenuItem("Build/发布Android调试版", false, 501)]
    public static void BuildAppAndroidDev()
    {
        BuildTarget target = BuildTarget.Android;
        BuildTargetGroup targetGroup = BuildTargetGroup.Android;

        BuildPrepareParams bpp = new BuildPrepareParams();
        bpp.target = target;
        BuildAppPrepare(bpp);

        BuildParams bp = new BuildParams();
        bp.target = target;
        bp.targetGroup = targetGroup;
        bp.isDebug = true;
        BuildPlayerImp(bp);
    }

    [MenuItem("Build/清理/LibraryBee工程文件", false, 501)]
    public static void ClearLibraryBeePro()
    {
        if (UnityEditor.EditorUtility.DisplayDialog("警告", "你需要清理打包工程临时文件（Android IOS）？", "确定", "取消"))
        {
            ClearLibraryBeeProImp();
        }
    }

    private static void ClearLibraryBeeProImp()
    {
        string path = Application.dataPath + "/../Library/Bee/Android";
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            Debug.Log("清理临时打包文件夹：" + path);
        }
        path = Application.dataPath + "/../Library/Bee/IOS";
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            Debug.Log("清理临时打包文件夹：" + path);
        }
    }

    [MenuItem("Build/清理/LibraryBeeIL2CPP文件", false, 501)]
    public static void ClearLibraryBeeIL2CPP()
    {
        if (UnityEditor.EditorUtility.DisplayDialog("警告", "你需要清理打包IL2CPP文件（删除后重新打包耗时较长）？", "确定", "取消"))
        {
            string path = Application.dataPath + "/../Library/Bee/artifacts";
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Debug.Log("清理临时打包文件夹：" + path);
            }
        }
    }

    static void BuildPlayerImp(BuildParams param)
    {
        int gameId = 169;
        var oldScripting = PlayerSettings.GetScriptingBackend(param.targetGroup);
        PlayerSettings.SetScriptingBackend(param.targetGroup, param.isMono ? ScriptingImplementation.Mono2x : ScriptingImplementation.IL2CPP);
        string scriptBackend = PlayerSettings.GetScriptingBackend(param.targetGroup).ToString();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);

        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        EditorUserBuildSettings.buildAppBundle = param.aab;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = param.isProject;
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.ForceInternal;
        PlayerSettings.Android.forceSDCardPermission = false;


        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel30;
        PlayerSettings.productName = "yd" + gameId;
        PlayerSettings.applicationIdentifier = "com.youdoo.yd" + gameId;
        int lastBundleVersionCode = PlayerSettings.Android.bundleVersionCode;

        PlayerSettings.Android.bundleVersionCode += 1;
        WriteVersionFile();

        string locationCopyPathName = LCL.MonoTool.GetBuildPath() + "release/";
        //路径不能使用含有相对路径的风格，必须转成全路径
        locationCopyPathName = Path.GetFullPath(locationCopyPathName);
        if (!Directory.Exists(locationCopyPathName))
        {
            Directory.CreateDirectory(locationCopyPathName);
        }
        locationCopyPathName = Path.Combine(locationCopyPathName, "yd_" + gameId +"_"+Application.version+ ".apk");

        PlayerSettings.SetManagedStrippingLevel( param.targetGroup, ManagedStrippingLevel.Disabled);
        PlayerSettings.stripEngineCode = false;
        //默认开启增量打包
        PlayerSettings.SetIncrementalIl2CppBuild(param.targetGroup, true);
        buildPlayerOptions.locationPathName = locationCopyPathName;
        buildPlayerOptions.target = param.target;


        if (param.isDebug)
        {
            //CompressWithLz4打包速度快些
            buildPlayerOptions.options = BuildOptions.CompressWithLz4;
            buildPlayerOptions.options |= (BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AutoRunPlayer | BuildOptions.AllowDebugging);
        }
        else
        {
            //包体大约小20M
            buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;
            if(param.isAutoRun)
            {
                buildPlayerOptions.options |= (BuildOptions.AutoRunPlayer);
            }
        }
        try
        {
            var result = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("发布成功：" + locationCopyPathName);
            }
            else
            {
                Debug.Log("发布失败，：" + result.summary.result.ToString());
            }
        }
        finally
        {
            ChangeGameDll(false);
        }

    }
    static void BuildAppPrepare(BuildPrepareParams param)
    {
        //清理上次打包的工程文件，防止每次打包包体都会变大一些
        ClearLibraryBeeProImp();
        BuildAssetBundle.BuildAllResourcesForPlayer();
        //拷贝配置表（或者将csv转换为sqlite）
        CopyCSV();

        Debug.LogWarning("代码和配置资源已经复制到build目录");
        Debug.LogWarning("代码工程只会处理代码和配置表资源到build目录，如果美术资源不是最新的，请打开美术工程处理");
        string localpath = Application.dataPath + "/StreamingAssets";
        EditorTool.DeleteFolder(localpath);
        localpath = localpath + "/" + EditorTool.GetBuildTargetName(param.target);
        string remotepath = LCL.MonoTool.GetBuildStreamingAssetsPath() + EditorTool.GetBuildTargetName(param.target);
        EditorTool.CopyFolder(LCL.MonoTool.GetBuildStreamingAssetsPath() + "android", Application.dataPath + "/StreamingAssets/android", "");
        EditorTool.CopyFolder(LCL.MonoTool.GetBuildStreamingAssetsPath() + "common", Application.dataPath + "/StreamingAssets", "");

        string root_path = "Assets/StreamingAssets/" + EditorTool.GetBuildTargetName(param.target) + "/codeconfig/config";
        //删除首包的csv文件，有可能做热更新的时候把这个文件放到里面
        DeleCSV(root_path);
        ChangeGameDll(true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    private static void ChangeGameDll(bool toDll)
    {
        string gameDllDir = Path.Combine(Application.dataPath, "GameDll");
        string dllBytesPath = Path.Combine(gameDllDir, "GameDll.dll.bytes");
        string dllPath = Path.Combine(gameDllDir, "GameDll.dll");
        string dllMetaPath = dllPath + ".meta";

        if (toDll)
        {
            if (!File.Exists(dllBytesPath))
            {
                throw new FileNotFoundException("GameDll字节文件不存在，无法生成临时DLL。", dllBytesPath);
            }

            Directory.CreateDirectory(gameDllDir);
            File.Copy(dllBytesPath, dllPath, true);
            Debug.Log("生成临时GameDll：" + dllPath);
        }
        else
        {
            if (File.Exists(dllPath))
            {
                File.Delete(dllPath);
                Debug.Log("删除临时GameDll：" + dllPath);
            }

            if (File.Exists(dllMetaPath))
            {
                File.Delete(dllMetaPath);
            }
        }
        SetMakeApp(toDll);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    public static void SetMakeApp(bool is_make)
    {
        var definesList = GetDefineSymbols();
        if (is_make)
        {
            if (!definesList.Contains("MAKE_APP"))
            {
                definesList.Add("MAKE_APP");
            }
        }
        else
        {
            if (definesList.Contains("MAKE_APP"))
            {
                definesList.Remove("MAKE_APP");
            }
        }

        ChangeDefineSymbol(definesList);
    }
    public static List<string> GetDefineSymbols()
    {
#if UNITY_IPHONE
        string symbolsDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
#elif UNITY_ANDROID
        string symbolsDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
#else
        string symbolsDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#endif
        return symbolsDefines.Split(';').ToList();
    }
    private static void ChangeDefineSymbol(List<string> definesList)
    {
        string defineSymbols = string.Join(";", definesList.ToArray());
#if UNITY_IPHONE
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, defineSymbols);
#elif UNITY_ANDROID
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defineSymbols);
#else
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);
#endif
    }



    private static void DeleCSV(string config_path)
    {
        if(!Directory.Exists(config_path))
        {
            return;
        }
        var out_files = Directory.GetFiles(config_path, "*.csv", SearchOption.AllDirectories);
       
        foreach (var out_file in out_files)
        {
            File.Delete(out_file);
        }
    }

    private static void CopyCSV()
    {
        //这里如果我们使用sqlite就不需要拷贝csv
        bool use_csv = true;
        if (use_csv)
        {
            var out_path = MonoTool.GetDevelopTablePath();
            var out_files = Directory.GetFiles(out_path, "*.csv");
            var inner_path = Application.dataPath + "/Resources/config";
            if (Directory.Exists(inner_path))
            {
                Directory.Delete(inner_path, true);
            }
            Directory.CreateDirectory(inner_path);
            foreach (var out_file in out_files)
            {
                string fileName = Path.GetFileName(out_file);
                File.Copy(out_file, inner_path + "/" + fileName + ".bytes");
            }
        }
        else
        {
            var out_path = MonoTool.GetDevelopTablePath();
            var bat = out_path + "导入表到客户端.bat";
            var dir = Path.GetDirectoryName(out_path);
            EditorTool.RunBat(bat, null,  dir);
            Debug.Log("处理sqlite数据完毕");
        }
    }

    private static void WriteVersionFile()
    {
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
        }

        string assetPath = "Assets/Resources/version.bytes";
        string fullPath = Path.Combine(resourcesDir, "version.bytes");
        string versionText = PlayerSettings.Android.bundleVersionCode.ToString();

        File.WriteAllText(fullPath, versionText);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("写入版本号文件：" + assetPath + " -> " + versionText);
    }

    [MenuItem("Build/Test/清理可读写文件夹")]
    public static void ClearPersistentDirection()
    {
        string dir = MonoTool.GetPersistentPath();
        if (Directory.Exists(dir))
        {
            if (EditorUtility.DisplayDialog("警告", "是否需要清理" + dir, "需要", "取消"))
            {
                Directory.Delete(dir, true);
                Debug.Log("清理完成:" + dir);
            }
            else
            {
                Debug.Log("已取消清理：" + dir);
            }
        }
        else
        {
            Debug.Log("可读写文件夹不存在，无需清理");
        }
    }
    [MenuItem("Build/Test/打开可读写文件夹")]
    public static void OpenPersistentDirection()
    {
        string dir = Application.persistentDataPath;
        if (Directory.Exists(dir))
        {
            EditorUtility.RevealInFinder(dir);
        }
    }
}
