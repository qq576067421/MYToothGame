#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoneSender
{
    public static class BoneSenderBuild
    {
        private const string m_SceneDirectory = "Assets/Scenes";
        private const string m_ScenePath = "Assets/Scenes/BoneSender.unity";
        private const string m_OutputPath = "Build/BoneSender-debug.apk";

        [MenuItem("Tools/BoneSender/Create Or Update Scene")]
        public static void CreateOrUpdateScene()
        {
            EnsureDirectory(m_SceneDirectory);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var androidServerInfoRoot = new GameObject("AndroidServerInfo");
            androidServerInfoRoot.AddComponent<BoneSenderAndroidServerInfo>();

            var senderRoot = new GameObject("BoneSenderRoot");
            var parseData = senderRoot.AddComponent<SenderBoneParseData>();
            var runtime = senderRoot.AddComponent<BoneSenderRuntime>();
            var previewDriver = senderRoot.AddComponent<BoneSenderPreviewDriver>();
            var infoPresenter = senderRoot.AddComponent<BoneSenderInfoPresenter>();
            runtime.m_ParseData = parseData;
            runtime.m_Config = new BoneSenderConfig();
            previewDriver.m_ParseData = parseData;
            infoPresenter.m_Runtime = runtime;

            EditorSceneManager.SaveScene(scene, m_ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BoneSender] 场景已更新: " + m_ScenePath);
        }

        public static void BuildAndroidDebug()
        {
            CreateOrUpdateScene();
            EnsureDirectory("Build");

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new System.Exception(
                    "当前 Unity 未安装 Android 构建支持。请在 Unity Hub 为 " +
                    Application.unityVersion +
                    " 安装 Android Build Support 后再执行 BoneSender 构建。");
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { m_ScenePath },
                target = BuildTarget.Android,
                locationPathName = m_OutputPath,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception("BoneSender Android build failed");
            }
        }

        private static void EnsureDirectory(string assetPath)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }
    }
}
#endif
