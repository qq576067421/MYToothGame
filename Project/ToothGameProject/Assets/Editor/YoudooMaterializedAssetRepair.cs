#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// 修复 SDK 发布包在团结引擎下的移植缺陷：物化资产用旧版十六进制 GUID 引用脚本，
// 包内脚本是 Base64 GUID，资产全部 Missing Script，Framework 初始化必然失败。
// 修复方式：让引擎按正确类型生成规范资产骨架（m_Script 引用必然合法），
// 再把原文件的数据行合并回去，原位覆盖以保留 .meta 的 GUID（兄弟资产引用不变）。
public static class YoudooMaterializedAssetRepair
{
    private const string m_ResourceRoot = "Assets/Resources/YoudooGame/Framework";

    private static readonly KeyValuePair<string, Type>[] m_Targets =
    {
        new KeyValuePair<string, Type>(
            m_ResourceRoot + "/Templates/SPLIT_FIXED.asset",
            typeof(YoudooGame.Framework.FrameworkTemplateSplitAsset)),
        new KeyValuePair<string, Type>(
            m_ResourceRoot + "/Profiles/MatchFlow/MatchSessionConfig_SPLIT.asset",
            typeof(YoudooGame.MatchFlow.Runtime.MatchSessionConfigAsset)),
        new KeyValuePair<string, Type>(
            m_ResourceRoot + "/Profiles/MatchFlow/MatchActionProfile.asset",
            typeof(YoudooGame.MatchFlow.Runtime.MatchActionProfileAsset)),
        new KeyValuePair<string, Type>(
            m_ResourceRoot + "/Profiles/MatchFlow/MatchSessionUiProfile.asset",
            typeof(YoudooGame.MatchFlow.Runtime.MatchSessionUiProfileAsset)),
        new KeyValuePair<string, Type>(
            m_ResourceRoot + "/Profiles/MatchFlow/MatchSessionUiVisualProfile.asset",
            typeof(YoudooGame.MatchFlow.Runtime.MatchSessionUiVisualProfileAsset)),
        new KeyValuePair<string, Type>(
            m_ResourceRoot + "/Profiles/CameraImage/CameraImageFocusProfile.asset",
            typeof(YoudooGame.CameraImage.CameraImageFocusProfileAsset)),
        new KeyValuePair<string, Type>(
            m_ResourceRoot + "/Profiles/CameraImage/CameraImageSplitFocusProfile.asset",
            typeof(YoudooGame.CameraImage.CameraImageSplitFocusProfileAsset)),
        new KeyValuePair<string, Type>(
            m_ResourceRoot + "/Profiles/CameraImage/CameraImageSplitLayoutProfile_SPLIT_FIXED.asset",
            typeof(YoudooGame.CameraImage.CameraImageSplitLayoutProfileAsset)),
    };

    private const string m_StaleLayoutProfileGuid = "9951f87445692844abf51e32a87e8c60";

    [MenuItem("Tools/YouDoo/Repair Materialized SDK Assets")]
    public static void Repair()
    {
        int repaired = 0;
        foreach (var pair in m_Targets)
        {
            if (RepairOne(pair.Key, pair.Value))
            {
                repaired++;
            }
        }

        Debug.Log("[YouDooMaterializedAssetRepair] repaired " + repaired + "/" + m_Targets.Length + " assets.");
    }

    private static bool RepairOne(string path, Type type)
    {
        var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        if (existing != null && existing.GetType() == type)
        {
            return false;
        }

        // 上次修复失败会把损坏文本留在资产文件里，原始内容在 Temp 备份中。
        string currentText = File.ReadAllText(path);
        string backupPath = Path.Combine(Path.GetFullPath("Temp"), Path.GetFileName(path) + ".broken.txt");
        string brokenText = currentText;
        if (IsCorruptYaml(currentText) && File.Exists(backupPath))
        {
            brokenText = File.ReadAllText(backupPath);
        }

        File.WriteAllText(backupPath, brokenText, new UTF8Encoding(false));

        string tmpPath = path.Replace(".asset", "_repair_tmp.asset");
        var fresh = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(fresh, tmpPath);
        string freshText = File.ReadAllText(Path.GetFullPath(tmpPath));
        AssetDatabase.DeleteAsset(tmpPath);
        if (string.IsNullOrEmpty(freshText) || !freshText.Contains("m_Script:"))
        {
            Debug.LogError("[YouDooMaterializedAssetRepair] engine wrote no template text for " + type.Name);
            return false;
        }

        string merged = BuildHeadThroughScriptLine(freshText) + BuildDataAfterScriptLine(brokenText);
        string layoutGuid = AssetDatabase.AssetPathToGUID(
            m_ResourceRoot + "/Profiles/CameraImage/CameraImageSplitLayoutProfile_SPLIT_FIXED.asset");
        if (!string.IsNullOrEmpty(layoutGuid))
        {
            merged = merged.Replace(
                "guid: " + m_StaleLayoutProfileGuid,
                "guid: " + layoutGuid);
        }

        File.WriteAllText(path, merged, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(path);

        var repairedAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        bool ok = repairedAsset != null && repairedAsset.GetType() == type;
        Debug.Log("[YouDooMaterializedAssetRepair] " + path + " -> " + (ok ? "OK" : "FAILED"));
        return ok;
    }

    private static bool IsCorruptYaml(string text)
    {
        return !text.Contains("%YAML") || !text.Contains("m_Script:");
    }

    // YAML 头到 m_Script 行（含折行续行）取自引擎新生成的骨架，保证脚本引用合法。
    private static string BuildHeadThroughScriptLine(string text)
    {
        var lines = text.Split('\n');
        var builder = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            builder.Append(lines[i]);
            builder.Append('\n');
            if (lines[i].TrimStart().StartsWith("m_Script:"))
            {
                int j = i + 1;
                while (j < lines.Length && lines[j].StartsWith("    "))
                {
                    builder.Append(lines[j]);
                    builder.Append('\n');
                    j++;
                }

                break;
            }
        }

        return builder.ToString();
    }

    // 数据字段取自原文件（m_Script 行及其折行之后到文件尾），与同类序列化顺序一致。
    private static string BuildDataAfterScriptLine(string text)
    {
        var lines = text.Split('\n');
        int index = 0;
        while (index < lines.Length && !lines[index].TrimStart().StartsWith("m_Script:"))
        {
            index++;
        }

        if (index >= lines.Length)
        {
            return string.Empty;
        }

        index++;
        while (index < lines.Length && lines[index].StartsWith("    "))
        {
            index++;
        }

        var builder = new StringBuilder();
        for (; index < lines.Length; index++)
        {
            builder.Append(lines[index]);
            if (index < lines.Length - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }
}
#endif
