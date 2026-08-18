using Microsoft.International.Converters.PinYinConverter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class EditorTool
{
    private static string m_ArtOutPath = "Assets/art/out";
    public static string GetArtOutPath()
    {
        return m_ArtOutPath;
    }
    public static string GetAssetsRelativePath(string fullPath)
    {
        string path = fullPath.Replace("\\", "/").Replace(Application.dataPath, "");
        path = "Assets" + path;
        return path;
    }

    public static string GetBuildTargetName(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return "android";
            case BuildTarget.iOS:
                return "ios";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "windows";
            case BuildTarget.StandaloneOSXIntel64:
            case BuildTarget.StandaloneOSXIntel:
                return "mac";
            // Add more build targets for your own.
            default:
                return "android";
        }
    }
    /// <summary>  
    /// 复制文件夹  
    /// </summary>  
    /// <param name="sourceFolder">待复制的文件夹</param>  
    /// <param name="destFolder">复制到的文件夹</param>  
    /// //ext_filter=".manifest|.manifest";
    public static void CopyFolder(string sourceFolder, string destFolder, string ext_filter )
    {
        if (!Directory.Exists(destFolder))
        {
            Directory.CreateDirectory(destFolder);
        }
        string[] files = Directory.GetFiles(sourceFolder);
        foreach (string file in files)
        {
            string name = Path.GetFileName(file);

            string dest = Path.Combine(destFolder, name);

            string ext = Path.GetExtension(file);

            if(!string.IsNullOrEmpty(ext_filter))
            {
                if(ext_filter.Contains(ext))
                {
                    continue;
                }
            }

            if(file.Length >= 260)
            {
                Debug.LogWarning("字符串路径太长，超出260的上限，src:" + file);
            }
            else if(dest.Length >= 260)
            {
                Debug.LogWarning("字符串路径太长，超出260的上限， dest:" + dest);
            }
            File.Copy(file, dest, true);
        }
        string[] folders = Directory.GetDirectories(sourceFolder);
        foreach (string folder in folders)
        {
            string name = Path.GetFileName(folder);

            string dest = Path.Combine(destFolder, name);

            CopyFolder(folder, dest, ext_filter);
        }
    }

    /// <summary>     
    /// C# 删除文件夹        
    /// </summary>     
    /// <param name="dir">删除的文件夹，全路径格式</param>     
    public static void DeleteFolder(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }
        // 循环文件夹里面的内容     
        foreach (string f in Directory.GetFileSystemEntries(dir))
        {
            // 如果是文件存在     
            if (File.Exists(f))
            {
                FileInfo fi = new FileInfo(f);
                if (fi.Attributes.ToString().IndexOf("Readonly") != 1)
                {
                    fi.Attributes = FileAttributes.Normal;
                }
                // 直接删除其中的文件     
                File.Delete(f);
            }
            else
            {
                // 如果是文件夹存在     
                // 递归删除子文件夹     
                DeleteFolder(f);
            }
        }
        // 删除已空文件夹     
        Directory.Delete(dir);
    }

    public static System.Diagnostics.Process CreateShellExProcess(string cmd, string args, string workingDir = "")
    {
        var pStartInfo = new System.Diagnostics.ProcessStartInfo(cmd);
        pStartInfo.Arguments = args;
        pStartInfo.CreateNoWindow = false;
        pStartInfo.UseShellExecute = true;
        pStartInfo.RedirectStandardError = false;
        pStartInfo.RedirectStandardInput = false;
        pStartInfo.RedirectStandardOutput = false;
        if (!string.IsNullOrEmpty(workingDir))
            pStartInfo.WorkingDirectory = workingDir;
        return System.Diagnostics.Process.Start(pStartInfo);
    }

    public static void RunBat(string batfile, string args, string workingDir = "", bool wait_for_end = true)
    {
        var p = CreateShellExProcess(batfile, args, workingDir);
        if(wait_for_end)
        {
            p.WaitForExit();
        }
        p.Close();
    }

    public static string m_ABCWords = "[A-Z]";
    public static string m_PinYinWords = "[\u4e00-\u9fa5]";
    public static string m_SpecialURLWords = "#|%|\\+";
    public static string m_ChinesePunctuation = "[\u3000-\u303f\uff00-\uffef]";
    public static string m_NotEnglishWords = "[^a-z0-9_./@ ()\\-]";
}

