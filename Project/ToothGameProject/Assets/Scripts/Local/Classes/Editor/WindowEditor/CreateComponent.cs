using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;

namespace LCL
{
    public class CreateComponent
    {
        public const int m_Order = -10000;
        [MenuItem("GameObject/界面工具/Button", false, 10 + m_Order)]
        public static void CreateButton()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "button/luibutton_filled.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<Button>());
            });
        }

        [MenuItem("GameObject/界面工具/Dropdown", false, 99 + m_Order)]
        public static void CreateDropdown()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "dropdown/dropdown_basic.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<Dropdown>());
            });
        }

        [MenuItem("GameObject/界面工具/Image", false, 11 + m_Order)]
        public static void CreateImage()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "icon/luiimage.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<Image>());
            });
        }
        [MenuItem("GameObject/界面工具/RawImage", false, 11 + m_Order)]
        public static void CreateRawImage()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "icon/luirawimage.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<RawImage>());
            });
        }
        [MenuItem("GameObject/界面工具/ProgressBar", false, 99 + m_Order)]
        public static void CreateProgressBar()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "progressbar/luiprogress.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<LUIProgress>());
            });
        }
        [MenuItem("GameObject/界面工具/Inputfield", false, 99 + m_Order)]
        public static void CreateInputfield()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "inputfield/input_single.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<InputField>());
            });
        }

        [MenuItem("GameObject/界面工具/列表/ScrollViewH", false, 20 + m_Order)]
        public static void CreateScrollViewH()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "scrollview/scrollview_lefttoright.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<UnityUI.LoopListView2>());
            });
        }
        [MenuItem("GameObject/界面工具/列表/ScrollViewV", false, 20 + m_Order)]
        public static void CreateScrollViewV()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "scrollview/scrollview_topbottom.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<UnityUI.LoopListView2>());
            });
        }

        [MenuItem("GameObject/界面工具/UIRTRenderer", false, 99 + m_Order)]
        public static void CreateUIRTRenderer()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "rtrenderer.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<UIRTRenderer>());
            });
        }

        [MenuItem("GameObject/界面工具/Scrollbar", false, 99 + m_Order)]
        public static void CreateScrollbar()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "scrollbar/scrollbar.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<Scrollbar>());
            });
        }

        [MenuItem("GameObject/界面工具/Slider", false, 99 + m_Order)]
        public static void CreateSlider()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "slider/slider.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<Slider>());
            });
        }

        [MenuItem("GameObject/界面工具/Text/单行", false, 12 + m_Order)]
        public static void CreateTextLine()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "text/txtLine.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<Text>());
            });
        }
        [MenuItem("GameObject/界面工具/Text/多行内容", false, 12 + m_Order)]
        public static void CreateTextContent()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "text/txtContent.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<Text>());
            });
        }
        [MenuItem("GameObject/界面工具/TextMesh/单行", false, 12 + m_Order)]
        public static void CreateTextMeshLine()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "text/txtmeshline.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<LUITextMesh>());
            });
        }
        [MenuItem("GameObject/界面工具/TextMesh/多行内容", false, 12 + m_Order)]
        public static void CreateTextMeshContent()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "text/txtmeshcontent.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<LUITextMesh>());
            });
        }
        [MenuItem("GameObject/界面工具/Toggle", false, 99 + m_Order)]
        public static void CreateToggle()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "checkbox/luitoggle_label.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<Toggle>());
            });
        }

        [MenuItem("GameObject/界面工具/ToggleGroup", false, 99 + m_Order)]
        public static void CreateToggleGroup()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "toggle/togglegroup.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<ToggleGroup>());
            });
        }
        [MenuItem("GameObject/界面工具/UITransform", false, 15 + m_Order)]
        public static void CreateUITransform()
        {
            GameObject bridge = null;
            CreateComponentPrefab(WindowEditorConst.WindowPrefabPath + "uitransform.prefab", out bridge, (componentObj) =>
            {
                CreateComponentCommon.AddComponentToBridge(bridge, componentObj.GetComponent<UITransform>());
            });
        }

        [MenuItem("GameObject/界面工具/添加到Bridge或清理 #a", false, 0 + m_Order)]
        public static void FixOrAddComponentToBridge()
        {
            GameObject dirty = null;
            foreach (var obj in Selection.gameObjects)
            {
                CreateComponentCommon.FixComponentToBridge(obj as GameObject);
                dirty = obj;
            }
#if UNITY_2018
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(dirty).scene);
#endif

        }
        [MenuItem("GameObject/界面工具/从Bridge移除", false, 9999 + m_Order)]
        public static void RemoveComponentFromBridge()
        {
            foreach (var obj in Selection.gameObjects)
            {
                Transform com = obj.transform;
                CreateComponentCommon.RemoveComponentFromAboveBridge(com);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            
        }
        private static bool CreateComponentPrefab(string componentPrefabPath, out GameObject bridge, Action<GameObject> CreatedCall)
        {
            bridge = null;
            GameObject sel = Selection.activeGameObject;
            if(sel!= null)
            {
                var componentBridge = CreateComponentCommon.FindBridgeAboveComponent(sel.transform, true);
                if (componentBridge != null)
                {
                    bridge = componentBridge.gameObject;
                }
                else
                {
                    Debug.LogWarning("当前模式的预制件没有找到ComponentBridge, 可能相关组件需要手动添加到ComponentBridge");
                }
                if(!LoadComponentPrefab(Selection.activeGameObject, componentPrefabPath, CreatedCall))
                {
                    ShowNoPrefabNotice();
                    return false;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "没有选择需要添加预制件的父对象，预制件不知道放在什么地方", "Ok");
                return false;
            }

            return true;
        }
        private static void ShowNotInBridge()
        {
            EditorUtility.DisplayDialog("错误", "没有选中窗口或者含有Bridge的预制件，无法创建组件", "Ok");
        }

        private static bool LoadComponentPrefab(GameObject parent, string componentPrefabPath, Action<GameObject> CreatedCall)
        {
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(componentPrefabPath);
            if(obj == null)
            {
                return false;
            }
            GameObject cloneObj = GameObject.Instantiate<GameObject>(obj);
            cloneObj.transform.SetParent(parent.transform);
            cloneObj.name = obj.name;
            cloneObj.name = cloneObj.name + "_" + parent.transform.childCount;
            cloneObj.transform.localScale = Vector3.one;
            cloneObj.transform.localRotation = Quaternion.identity;
            cloneObj.transform.localPosition = Vector3.zero;

            //以前的版本
            //PrefabUtility.ConnectGameObjectToPrefab(cloneObj, obj);
            
            
            //现在的版本 2024年6月17日20:33:35这里不推荐把一些基础的控件设置Prefab的关联，防止过于零碎
            //一般比较大点的模块化预制件才使用关联  这个一般都是手动拖的
            //cloneObj = PrefabUtility.ConnectGameObjectToPrefab(cloneObj, obj);

            CreatedCall(cloneObj);
            Selection.activeGameObject = cloneObj;
            return true;
        }

        private static void ShowNoPrefabNotice()
        {
            EditorUtility.DisplayDialog("错误", "没有找到对应组件的模板Prefab", "Ok");
        }
    }
}
