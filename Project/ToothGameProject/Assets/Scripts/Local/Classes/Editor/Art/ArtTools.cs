using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LCL;

public class ArtTools
{	
   
    [MenuItem("GameObject/美术策划工具/快速获取子对象的路径到剪切板")]
    static public void FindChildPath()
    {
        UnityEngine.Object[] selected = Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable);
        if (selected != null && selected.Length == 2)
        {
            Transform one = ((GameObject)selected[0]).transform;
            Transform two = ((GameObject)selected[1]).transform;
            for (int i = 0; i < 2; ++i)
            {
                bool find = false;
                string path = selected[i].name;
                Transform parent = ((GameObject)selected[i]).transform.parent;
                while (parent != null)
                {
                    if (parent == ((GameObject)selected[i == 0 ? 1 : 0]).transform)
                    {
                        find = true;
                        break;
                    }
                    else
                    {
                        path = parent.name + "/" + path;
                        parent = parent.parent;
                    }
                }
                if (find)
                {


                    TextEditor te = new TextEditor();
                    te.text = path;
                    te.SelectAll();
                    te.Copy();
                    Debug.Log("快速获取两个预制件的路径到剪切板》》操作成功:" + path);
                    break;
                }
            }
            

        }
        else
        {
            Debug.LogError("快速获取两个预制件的路径到剪切板》》操作失败");
        }
    }


    [MenuItem("Assets/美术策划工具/获取AB路径到剪切板 %b")]
    static public void FindFilePath()
    {
        UnityEngine.Object[] selected = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Deep);
        if (selected != null && selected.Length == 1)
        {
            UnityEngine.Object file = selected[0];
            string path = AssetDatabase.GetAssetPath(file);
            path = Path.ChangeExtension(path, MonoTool.GetAssetbundleSuffix());
            path = path.Replace(EditorTool.GetArtOutPath() + "/", "");
            TextEditor te = new TextEditor();
            te.text = path;
            te.SelectAll();
            te.Copy();
            Debug.Log("获取文件路径到剪切板》》操作成功:" + path);

        }
        else
        {
            Debug.LogError("本操作支持且只支持选择一个文件");
        }
    }
    [MenuItem("Assets/美术策划工具/删除选择字体外边框")]
    static public void DeleteAllOutline()
    {
        UnityEngine.Object[] selected = Selection.GetFiltered(typeof(Object), SelectionMode.Deep);
        if (selected != null && selected.Length > 0)
        {
            foreach(var sel in selected)
            {
                string prefabPath = AssetDatabase.GetAssetOrScenePath(sel);
                if (prefabPath.ToLower().EndsWith(".prefab") == false)
                {
                    continue;
                }
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var go_clone = (GameObject)GameObject.Instantiate(go);
                var items = go_clone.GetComponentsInChildren<UnityEngine.UI.Outline>();
                if (items.Length == 0)
                {
                    continue;
                }
                foreach (var item in items)
                {
                    GameObject.DestroyImmediate(item, true);
                }
                bool ok = false;
                PrefabUtility.SaveAsPrefabAsset(go_clone, prefabPath, out ok);

            }
            AssetDatabase.SaveAssets();

            Debug.Log("删除所有outline完成");
        }
    }




    [MenuItem("GameObject/美术策划工具/添加ButtonScale")]
    static public void AddButtonScale()
    {
        UnityEngine.Object[] selected = Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable);
        if (selected != null)
        {
            //获取lbuttonctrl资源，然后待会要放到animator
            //路径 Assets/art/out/ui_component/button/animation/lbuttonctrl.jpg
            string controllerPath = "Assets/art/out/ui_component/button/animation/lbuttonctrl.controller";
            var lbuttonctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (lbuttonctrl == null)
            {
                Debug.LogError($"找不到AnimatorController资源: {controllerPath}");
                return;
            }

            foreach (var sel in selected)
            {
                var go = sel as GameObject;
                var buttons = go.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var button in buttons)
                {
                    button.transition = UnityEngine.UI.Selectable.Transition.None;
                    var animator = button.GetComponent<Animator>();
                    if (animator == null)
                    {
                        animator = button.gameObject.AddComponent<Animator>();
                    }
                    animator.runtimeAnimatorController = lbuttonctrl;

                    var btnScale = button.gameObject.GetComponent<UnityUI.UIButtonScale>();
                    if (btnScale == null)
                    {
                        btnScale = button.gameObject.AddComponent<UnityUI.UIButtonScale>();
                    }
                    btnScale.m_Animator = animator;
                    var canvasGroup = button.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        GameObject.DestroyImmediate(canvasGroup, true);
                    }
                }
                var luibuttons = go.GetComponentsInChildren<UnityUI.LUIButton>(true);
                foreach (var button in luibuttons)
                {
                    button.transition = UnityEngine.UI.Selectable.Transition.None;
                    var animator = button.GetComponent<Animator>();
                    if (animator == null)
                    {
                        animator = button.gameObject.AddComponent<Animator>();
                    }
                    animator.runtimeAnimatorController = lbuttonctrl;

                    var btnScale = button.gameObject.GetComponent<UnityUI.UIButtonScale>();
                    if (btnScale == null)
                    {
                        btnScale = button.gameObject.AddComponent<UnityUI.UIButtonScale>();
                    }
                    btnScale.m_Animator = animator;
                    var canvasGroup = button.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        GameObject.DestroyImmediate(canvasGroup, true);
                    }
                }
                EditorUtility.SetDirty(go);
                //保存
                AssetDatabase.SaveAssetIfDirty(go);

            }
            AssetDatabase.SaveAssets();
        }
    }
}
