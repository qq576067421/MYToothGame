using System.IO;
using UnityEditor.Android;
using UnityEngine;

public class AndroidPostBuildProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 999;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var launcherGradlePath = ResolveLauncherGradlePath(path);
        if (!File.Exists(launcherGradlePath))
        {
            Debug.LogWarning("AndroidPostBuildProcessor: launcher/build.gradle not found: " + launcherGradlePath);
            return;
        }

        var content = File.ReadAllText(launcherGradlePath);
        if (content.Contains("checkReleaseBuilds false") && content.Contains("disable 'ExpiredTargetSdkVersion'"))
        {
            return;
        }

        const string needle = "        abortOnError false";
        const string patch =
            "        abortOnError false\r\n" +
            "        checkReleaseBuilds false\r\n" +
            "        disable 'ExpiredTargetSdkVersion'";
        if (!content.Contains(needle))
        {
            Debug.LogWarning("AndroidPostBuildProcessor: lintOptions block not found in launcher/build.gradle");
            return;
        }

        content = content.Replace(needle, patch);
        File.WriteAllText(launcherGradlePath, content);
        Debug.Log("AndroidPostBuildProcessor: disabled ExpiredTargetSdkVersion for launcher release lint.");
    }

    private static string ResolveLauncherGradlePath(string path)
    {
        var currentModuleGradlePath = Path.Combine(path, "build.gradle");
        if (File.Exists(currentModuleGradlePath) &&
            string.Equals(new DirectoryInfo(path).Name, "launcher", System.StringComparison.OrdinalIgnoreCase))
        {
            return currentModuleGradlePath;
        }

        var childLauncherGradlePath = Path.Combine(path, "launcher", "build.gradle");
        if (File.Exists(childLauncherGradlePath))
        {
            return childLauncherGradlePath;
        }

        var parent = Directory.GetParent(path);
        if (parent != null)
        {
            var siblingLauncherGradlePath = Path.Combine(parent.FullName, "launcher", "build.gradle");
            if (File.Exists(siblingLauncherGradlePath))
            {
                return siblingLauncherGradlePath;
            }
        }

        return childLauncherGradlePath;
    }
}
