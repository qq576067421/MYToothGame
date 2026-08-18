using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LCL;
using TMPro;
using UnityEngine.UI;

public class PrefabTool
{	
   
    [MenuItem("GameObject/美术策划工具/罗列含有中文的UI预制件")]
    [MenuItem("Assets/美术策划工具/罗列含有中文的UI预制件")]
    static public void FindCHTextPrefab()
    {
        if (UnityEditor.EditorUtility.DisplayDialog("警告", "检测预制件中含有中文", "确定", "取消"))
        {
            UnityEngine.Object[] selObj = Selection.GetFiltered(typeof(Object), SelectionMode.Unfiltered);
            if (selObj == null || selObj.Length == 0)
            {
                Debug.LogError("没有选择预制件或者文件夹");
                return;
            }

            foreach (Object item in selObj)
            {
                string objPath = AssetDatabase.GetAssetPath(item);
                if (item is DefaultAsset)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(objPath);
                    CheckChildCh(dirInfo);
                }
                else
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
                    CheckCh(go, objPath);
                }

            }
        }
        else
        {
            Debug.Log("cancel");
        }

       
    }

    static void CheckCh(GameObject go, string objPath)
    {
        if(go == null)
        {
            return;
        }
        List<string> txts = new List<string>();
        string txt_str = "";
        var texts = go.GetComponentsInChildren<Text>(true);
        if (texts != null && texts.Length > 0)
        {
            foreach (var text in texts)
            {
                if(text.GetType().FullName.Contains("LUIText"))
                {
                    continue;
                }
                else
                {
                    txt_str += " " + text.name;
                    txts.Add(text.name);
                }
            }
        }
        var tmpTexts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (tmpTexts != null && tmpTexts.Length > 0)
        {
            foreach (var text in tmpTexts)
            {
                if (text is UnityUI.LUITextMesh)
                {
                    continue;
                }
                txt_str += " " + text.name;
                txts.Add(text.name);
            }
        }
        var uitexts = go.GetComponentsInChildren<UnityUI.LUIText>(true);
        if (uitexts != null && uitexts.Length > 0)
        {
            foreach (var text in uitexts)
            {
                if(text.InputType == UnityUI.TextInputType.ID)
                {
                    if (string.IsNullOrEmpty(text.LanguageId) || text.LanguageId == "-1")
                    {
                        txt_str += " " + text.name;
                        txts.Add(text.name);
                    }
                }
                else
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(text.text, "[\u4e00-\u9fa5]"))
                    {
                        txt_str += " " + text.name;
                        txts.Add(text.name);
                    }
                }
            }
        }
        var uitextMeshes = go.GetComponentsInChildren<UnityUI.LUITextMesh>(true);
        if (uitextMeshes != null && uitextMeshes.Length > 0)
        {
            foreach (var text in uitextMeshes)
            {
                if (text.InputType == UnityUI.TextInputType.ID)
                {
                    if (string.IsNullOrEmpty(text.LanguageId) || text.LanguageId == "-1")
                    {
                        txt_str += " " + text.name;
                        txts.Add(text.name);
                    }
                }
                else
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(text.text, "[\u4e00-\u9fa5]"))
                    {
                        txt_str += " " + text.name;
                        txts.Add(text.name);
                    }
                }
            }
        }
        if(txts.Count > 0)
        {
            Debug.LogError("预制件含有中文或者Text, 预制件路径：" + objPath  + "，分别是：" + txt_str);
        }

    }


    static void CheckChildCh(DirectoryInfo dirInfo)
    {
        FileSystemInfo[] files = dirInfo.GetFileSystemInfos();
        foreach (FileSystemInfo file in files)
        {
            if(file is DirectoryInfo)
            {
                CheckChildCh(file as DirectoryInfo);
            }
            else
            {
                string objPath = file.FullName.Replace('\\', '/');
                objPath = objPath.Replace(Application.dataPath, "Assets");

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
                CheckCh(go, objPath);
            }


        }
    }
}
