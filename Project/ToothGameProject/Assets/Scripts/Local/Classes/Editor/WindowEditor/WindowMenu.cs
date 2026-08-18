using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityUI;

namespace LCL
{
    public static class WindowMenu
    {
        private static string m_BlankString = "     "; 
        [MenuItem("GameObject/界面工具/创建新窗口", false, 9999 + CreateComponent.m_Order)]
        public static void CreateWindow()
        {
            GameObject layer = Selection.activeGameObject;
            if (layer == null || layer.transform.parent == null || layer.transform.parent.name != "GlobalCanvas")
            {
                EditorUtility.DisplayDialog("错误", "好像没有选择所属图层", "Ok");
                return;
            }


            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(WindowEditorConst.WindowPrefabPath + "template_full_wnd.prefab");
            if (obj == null)
            {
                EditorUtility.DisplayDialog("错误", "好像没有template_full_wnd预制件，请先手动创建该模板预制件", "Ok");
                return;
            }
            GameObject cloneObj = GameObject.Instantiate<GameObject>(obj);
            cloneObj.name =  FixName(layer.name) + WindowEditorConst.WindowNameEnd;
            cloneObj.transform.SetParent(layer.transform);
            cloneObj.transform.localScale = Vector3.one;
            cloneObj.transform.localEulerAngles = Vector3.zero;
            cloneObj.transform.localPosition = Vector3.zero;
            var window =  cloneObj.GetComponent<UIWindow>();
            if(window == null)
            {
                window = cloneObj.AddComponent<UIWindow>();
            }
            var bridge = cloneObj.GetComponent<ComponentBridge>();
            if(bridge == null)
            {
                bridge = cloneObj.AddComponent<ComponentBridge>();
            }
            Selection.activeGameObject = cloneObj;
        }
        //[MenuItem("GameObject/UITools/FixWindowBridge", false, 0)]
        public static void FixWindowBridgeExption()
        {
            var layer = Selection.activeGameObject;
            if (!CreateComponentCommon.IsWindow(layer))
            {
                EditorUtility.DisplayDialog("错误", "修复组件和Bridge需要选择窗口预制件", "Ok");
                return;
            }
            CreateComponentCommon.FixWindowBridgeExption(layer);
        }
        [MenuItem("Assets/界面工具/生成窗口代码 #s", false, 1 + CreateComponent.m_Order)]
        [MenuItem("GameObject/界面工具/生成窗口代码 #s", false, 1 + CreateComponent.m_Order)]
        public static void CreateWindowCode()
        {
            var layer = Selection.activeGameObject;

            if (!CreateComponentCommon.IsWindow(layer))
            {
                EditorUtility.DisplayDialog("错误", "修复组件和Bridge需要选择窗口预制件", "Ok");
                return;
            }
            CreateComponentCommon.FixWindowBridgeExption(layer.gameObject);
            CreateWindowCodeImp(layer.gameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/界面工具/生成所有窗口代码", false, 9999 + CreateComponent.m_Order)]
        [MenuItem("GameObject/界面工具/生成所有窗口代码", false, 9999 + CreateComponent.m_Order)]
        public static void CreateAllWindowCode()
        {
            if(Selection.gameObjects.Length > 0)
            {
                foreach(var win_go in Selection.gameObjects)
                {
                    var wins = win_go.GetComponentsInChildren<UIWindow>();
                    if (wins.Length > 0)
                    {
                        foreach (var layer in wins)
                        {
                            if (!CreateComponentCommon.IsWindow(layer))
                            {
                                EditorUtility.DisplayDialog("错误", "修复组件和Bridge需要选择窗口预制件", "Ok");
                                continue;
                            }
                            CreateComponentCommon.FixWindowBridgeExption(layer.gameObject);
                            CreateWindowCodeImp(layer.gameObject);
                        }
                    }
                    else
                    {
                        var s_wins = Selection.activeGameObject.GetComponentsInChildren<UISubWindow>();
                        foreach (var layer in s_wins)
                        {
                            if (!CreateComponentCommon.IsWindow(layer.gameObject))
                            {
                                EditorUtility.DisplayDialog("错误", "修复组件和Bridge需要选择窗口预制件", "Ok");
                                continue;
                            }
                            CreateComponentCommon.FixWindowBridgeExption(layer.gameObject);
                            CreateWindowCodeImp(layer.gameObject);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        private static void CreateWindowCodeImp(GameObject layer)
        {
            ComponentBridge bridge = layer.GetComponent<ComponentBridge>();
            if (bridge == null)
            {
                EditorUtility.DisplayDialog("错误", "好像没有选中带有ComponentBridge的窗口", "Ok");
                return;
            }
            //CreateComponentCommon.FixWindowBridgeExption(layer);


            string windowCode =
@"//功能：" + layer.name + @"的窗口配置文件
//工具作者：lichunlin
//生成时间：" + DateTime.Now.ToString() + @"
//描述：以下文件是自动生成的，任何手动修改都会被下次自动生成覆盖。

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityUI;
using GameDll;
namespace GameHot
{" + "\r\n";
            List<string> classNames = new List<string>();
            windowCode += CreateBridgeClass(m_BlankString, bridge, classNames);
            windowCode +=
"}\r\n";
            string file_name = layer.name;
            if(!string.IsNullOrEmpty(bridge.m_BridgeName))
            {
                file_name = bridge.m_BridgeName;
            }
            string filePath = Path.GetFullPath(Application.dataPath + WindowEditorConst.UIGenDllPath + WindowEditorConst.UIGenRelativePath+ "v_" + file_name + ".cs");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            FileStream file = new FileStream(filePath, FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(file, Encoding.UTF8);
            writer.WriteLine(windowCode);
            writer.Flush();
            writer.Close();
            writer = null;
            file.Close();
            file = null;
            AddGeneratedFileToGameDllProject(filePath);
            Debug.Log("创建成功：" + filePath);
        }
        private static void AddGeneratedFileToGameDllProject(string filePath)
        {
            string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "GameDll", "GameDll.csproj"));
            if (!File.Exists(projectPath))
            {
                Debug.LogWarning("找不到GameDll项目文件：" + projectPath);
                return;
            }

            string relativePath = GetRelativePath(Path.GetDirectoryName(projectPath), filePath).Replace("/", "\\");
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(projectPath);

            string xmlNamespace = doc.DocumentElement.NamespaceURI;
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("msb", xmlNamespace);
            string compileXPath = string.IsNullOrEmpty(xmlNamespace) ? "//Compile" : "//msb:Compile";
            XmlNodeList compileNodes = doc.SelectNodes(compileXPath, namespaceManager);
            foreach (XmlNode node in compileNodes)
            {
                XmlElement compileElement = node as XmlElement;
                if (compileElement == null)
                {
                    continue;
                }

                string includePath = compileElement.GetAttribute("Include").Replace("/", "\\");
                if (string.Equals(includePath, relativePath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

            }

            XmlElement itemGroup = null;
            if (compileNodes.Count > 0)
            {
                itemGroup = compileNodes[compileNodes.Count - 1].ParentNode as XmlElement;
            }

            if (itemGroup == null)
            {
                itemGroup = doc.CreateElement("ItemGroup", xmlNamespace);
                doc.DocumentElement.AppendChild(itemGroup);
            }

            XmlElement newCompileElement = doc.CreateElement("Compile", xmlNamespace);
            newCompileElement.SetAttribute("Include", relativePath);
            itemGroup.AppendChild(newCompileElement);
            doc.Save(projectPath);
            Debug.Log("已添加到GameDll项目：" + relativePath);
        }
        private static string GetRelativePath(string rootPath, string filePath)
        {
            Uri rootUri = new Uri(AppendDirectorySeparatorChar(rootPath));
            Uri fileUri = new Uri(filePath);
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString());
        }
        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()) && !path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }


        //layer用于区分class类名字
        private static string CreateBridgeClass(string blank,ComponentBridge bridge, List<string> classNames)
        {
            string windowCode = "";
            CreateComponentCommon.ClearBridgeNullOrRepeat(bridge);
            int count = bridge.GetAllComponents().Count;
            List<string> nameUsed = new List<string>();
            string bridgeComment = blank + m_BlankString + "//" + GetComponentPath(bridge, "") + "\r\n";
            string variableLines = bridgeComment + blank + m_BlankString + "public ComponentBridge m_Bridge;\r\n\r\n";
            string componentGetLines = "m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;\r\n";

            List<string> namePool = new List<string>();
            for (int i = 0; i < count; ++i)
            {
 
                //处理枚举
                Component com = bridge.GetControl(i);
                //处理变量
                string variablename = "m_" + ReName(namePool, FixName(com.name));
                string comment = blank + m_BlankString + "//" + GetComponentPath(com, "") + "\r\n";
                string variablestr = comment + blank + m_BlankString + "public " + com.GetType().Name + " " +variablename + ";\r\n\r\n";
                variableLines += variablestr;

                //处理变量赋值
                string componentGetstr = blank + m_BlankString + m_BlankString  + variablename + " = m_Bridge.GetControl(" + i + ") as " + com.GetType().Name + ";\r\n";
                componentGetLines += componentGetstr;
            }

            string className = "";
            if(!string.IsNullOrEmpty(bridge.m_BridgeName))
            {
                className = FixName( bridge.m_BridgeName);
            }
            else
            {
                className = FixName(bridge.name);
            }

            bool hasSame = HasSameClassName(classNames, className);
            if(hasSame)
            {
                return windowCode;
            }
            windowCode +=
      blank+ "public class v_" + className + ":v_base_wnd\r\n" +
      blank + "{\r\n" + 
      blank + m_BlankString + "public object m_UserData; \r\n" + 
      variableLines +
      blank + m_BlankString + "public override void InitComponent(GameObject go)\r\n" +
      blank + m_BlankString + "{\r\n" +
      blank + m_BlankString + m_BlankString + componentGetLines +
      blank + m_BlankString + "}\r\n";
            //处理子bridge
            windowCode += CreateChildBridgeClass(blank, bridge.transform, classNames, false);
            windowCode +=
      blank + "}\r\n";

            return windowCode;
        }
        private static bool HasSameClassName(List<string> classNames, string className)
        {
            bool has = classNames.Exists((name) => { return name == className; });
            if(!has)
            {
                classNames.Add(className);
                return false;
            }
            else
            {
                return true;
            }
        }
        private static string FixName(string name)
        {
            return name.Replace("(", "_").Replace(")", "_").Replace(" ", "");
        }
        private static string CreateChildBridgeClass(string blank,Transform  child, List<string> classNameSelf, bool IsSelf = false)
        {
            string windowCode = "";
            int count_child = child.transform.childCount;
            List<string> classNames = IsSelf ? classNameSelf : new List<string>();
            for(int i=0;i< count_child; ++i)
            {
                var childtrans = child.transform.GetChild(i);
                var child_bridge = childtrans.GetComponent<ComponentBridge>();
                var child_window = childtrans.GetComponent<UIWindow>();
                var sub_window = childtrans.GetComponent<UISubWindow>();
                if(child_bridge != null)
                {
                    if (child_window == null && sub_window == null)
                    {
                        windowCode += CreateBridgeClass(blank + m_BlankString, child_bridge, classNames);
                    }
                }
                else
                {
                    windowCode += CreateChildBridgeClass(blank, childtrans, classNames, true);
                }
            }
            return windowCode;
        }
        private static string GetComponentPath(Component com, string name)
        {
            if(com != null)
            {
                if( com.GetComponent<ComponentBridge>() != null)
                {
                    name = FixName( com.name ) + "/" + name;
                    return name;
                }
                else
                {
                    if(string.IsNullOrEmpty(name))
                    {
                        name = FixName( com.name );
                    }
                    else
                    {
                        name = FixName(com.name) + "/" + name;
                    }
                    Component parent = com.transform.parent;
                    return GetComponentPath(parent, name);
                }
            }
            else
            {
                return name;
            }
        }
        private static string ReName(List<string> namePool, string name)
        {
            if( namePool.Exists((_name) => { return _name == name; }))
            {
                if(name.Contains("_new"))
                {
                    int last = name.LastIndexOf('_');
                    string number_str = name.Remove(0, last);
                    string number = number_str.Replace("_new", "");
                    int num = 1;
                    int.TryParse(number, out num);
                    name = name.Remove(name.LastIndexOf('_'));
                    name = name + "_new" + (num + 1);
                }
                else
                {
                    name = name + "_new1";
                }
                return ReName(namePool, name);
            }
            else
            {
                namePool.Add(name);
                return name;
            }
        }
    }
}
