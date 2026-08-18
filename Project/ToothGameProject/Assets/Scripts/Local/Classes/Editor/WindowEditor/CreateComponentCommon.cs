using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityUI;

namespace LCL
{
    public class CreateComponentCommon
    {
        public static List<Type> m_ComponentTypeList = new List<Type>()
        {
            typeof(UIWindowAnimation),
            typeof(UISubWindow),
            typeof(UITransform),
            typeof(UIArray),
            typeof(ComponentBridge),
            typeof(LUIButton),
            typeof(Button),
            typeof(LUIPressButton),
            typeof(UIDragButton),
            typeof(UIDropOn),
            typeof(Dropdown),
            typeof(InputField),
            typeof(LoopListView2),
            typeof(UIRTRenderer),
            typeof(Scrollbar),
            typeof(Slider),
            typeof(LUIToggle),
            typeof(LUIProgress),
            typeof(LUITable),
            typeof(Toggle),
            typeof(ToggleGroup),
            typeof(LUIText),
            typeof(LUITextMesh),
            typeof(TextMeshProUGUI),
            typeof(Text),
            typeof(LUIImage),
            typeof(Image),
            typeof(LUIRawImage),
            typeof(RawImage),
            //typeof(UnityUI.ParticleImage),

        };
        public static void SetDirty()
        {
            var stage =UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                var root = stage.prefabContentsRoot;
                EditorUtility.SetDirty(root);
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            
        }
        public static void AddComponentToBridge(GameObject bridgeObj, Component component)
        {
            SetDirty();
            if(bridgeObj == null)
            {
                Debug.LogWarning("当前组件没有自动添加到ComponentBridge" + component.name +", 你可以手动处理");
            }
            var bridge = bridgeObj.GetComponent<ComponentBridge>();
            if(bridge == null)
            {
                EditorUtility.DisplayDialog("错误", "窗口预制件上面没有ComponentBridge", "Ok");
                return;
            }
            var allComs = bridge.GetAllComponents();
            ClearBridgeNullOrRepeat(bridge);
            int count = allComs.Count;
            
            for(int i=0;i<count;++i)
            {
                if(allComs[i].gameObject == component.gameObject)
                {
                    allComs[i] = component;
                    Debug.LogWarning("组件gameObject相同，替换。请确保一个GameObject一个逻辑组件，如果有多个请分在不同预制件");
                    return;
                }
            }
            allComs.Add(component);
        }


        public static void ClearBridgeNullOrRepeat(ComponentBridge bridge)
        {
            SetDirty();
            var allComs = bridge.GetAllComponents();
            int count = allComs.Count;
            List<Component> temp = new List<Component>();
            for (int i = 0; i < count; ++i)
            {
                var com = allComs[i];
                if (com != null)
                {
                    bool findSame = false;
                    foreach(var comTemp in temp)
                    {
                        if(comTemp.gameObject == com.gameObject)
                        {
                            findSame = true;
                            break;
                        }
                    }
                    if (!findSame)
                    {
                        temp.Add(allComs[i]);
                    }
                }
            }
            allComs.Clear();
            allComs.AddRange(temp);
        }
        //修复当前组件，将它放到离他最近bridge下面
        public static void FixComponentToBridge(GameObject obj)
        {
            SetDirty();
            var rightBridge = FindBridgeAboveComponent(obj.transform, true);
            //所有的bridge
            var wnd = FindComponentRootWindow(obj);
            if(wnd == null)
            {
                Debug.LogWarning("当前操作没有找到Window窗口类，可能会出现控件在其他Bridge残留问题");
            }
            else
            {
                var bridges = wnd.GetComponentsInChildren<ComponentBridge>(true).ToList();
                bridges.Remove(rightBridge);
                foreach (var bridge in bridges)
                {
                    RemoveFromBridge(bridge, obj.transform);
                }
                foreach (var bridge in bridges)
                {
                    //清理bridge上面空的或者重复控件
                    ClearBridgeNullOrRepeat(bridge);
                }            
            }
            var com = FindComponentInGameObject(obj);
            if (rightBridge != null)
            {
                AddComponentToBridge(rightBridge.gameObject, com);
                Debug.Log("已经修复组件, 名字：" + obj.name + " 类型：" + com.GetType().ToString() + "Bridge:" + rightBridge.name);
            }
            else
            {
                Debug.Log("已经清理了空组件, 名字：" + obj.name + " 类型：" + com.GetType().ToString());
            }
        }
        //得到组件所在窗口
        private static GameObject FindComponentRootWindow(GameObject com)
        {
            if(com == null)
            {
                return null;
            }
            var subwindow = com.GetComponentInParent<UISubWindow>();
            if (subwindow != null)
            {
                return subwindow.gameObject;
            }

            UIWindow window = com.GetComponentInParent<UIWindow>();
            if(window != null)
            {
                return window.gameObject;
            }
            else
            {
                return null;
            }
        }
        public static bool IsWindow(Component name)
        {
            return name.GetComponent<UIWindow>() != null;
        }
        public static bool IsWindow(GameObject name)
        {
            return name.GetComponent<UIWindow>() != null || name.GetComponent<UISubWindow>() != null;
        }
        public static void FixWindowBridgeExption(GameObject wnd)
        {
            SetDirty();
            EditorUtility.SetDirty(wnd);

            var bridges = wnd.GetComponentsInChildren<ComponentBridge>(true).ToList();
            //纠正因为移动导致的组件变成空的或者一个组件分配到两个以上的bridge里面了
            foreach(var bridge in bridges)
            {
                var components = bridge.GetAllComponents();
                for(int i=0;i<components.Count;++i)
                {
                    var component = components[i];
                    if(component == null || component.Equals(null))
                    {
                        Debug.LogWarning("组件是空的，有可能是因为预制件上面的控件被我们手动删除了, Bridge 名字：" + bridge.name + "序号：" + i);
                        continue;
                    }
                    //检查组件的预制件是否在本窗口
                    var comWnd = GetComponentWnd(component);
                    if(comWnd != wnd)
                    {
                        continue;
                    }
                    //检查下组件类型是不是在列表    
                    if(! m_ComponentTypeList.Exists((_type) => { return _type == component.GetType(); }) )
                    {
                        components[i] = null;
                        var com = FindComponentInGameObject(component.gameObject);
                        if (com != null)
                        {
                            AddComponentToBridge(bridge.gameObject, com);
                            component = com;
                        }
                        else
                        {
                            Debug.LogWarning("预制件上面没有需要的组件, Bridge 名字：" + bridge.name);
                            LogPath(component.gameObject);
                            continue;
                        }
                    }     

                    //组件的bridge可能发生改变了，这里修复下
                    var comBridge = FindBridgeAboveComponent(component, true);
                    if(comBridge != bridge)
                    {

                        components[i] = null;
                        component = FindComponentInGameObject(component.gameObject);
                        AddComponentToBridge(comBridge.gameObject, component);
                    }

                    //去除该控件在所有bridge里面可能重复添加的情况
                    List<ComponentBridge> removeFromBridges = new List<ComponentBridge>();
                    removeFromBridges.AddRange(bridges);
                    removeFromBridges.Remove(comBridge); 

                    foreach(var rb in removeFromBridges)
                    {
                        RemoveFromBridge(rb, component);
                    }

                }
            }

            foreach(var bridge in bridges)
            {
                //清理bridge上面空的或者重复控件
                ClearBridgeNullOrRepeat(bridge);
            }
        }

        private static GameObject GetComponentWnd(Component com)
        {
            UIWindow window = com.GetComponentInParent<UIWindow>();
            if (window != null)
            {
                return window.gameObject;
            }
            else
            {
                return null;
            }
        }

        public static void LogPath(GameObject obj)
        {
            if(obj == null)
            {
                return;
            }
            string log = obj.name;
            Transform parent = obj.transform.parent;
            while(parent != null)
            {
                log = parent.name + "/" + log;
                parent = parent.parent;
            }
            Debug.Log(log);
        }
        //去除该控件在所有bridge里面可能重复添加的情况
        private static void RemoveFromBridge(ComponentBridge _bridge, Component com)
        {
            SetDirty();
            var _components = _bridge.GetAllComponents();
            for (int i = 0; i < _components.Count; ++i)
            {
                if(_components[i] != null &&  _components[i].gameObject == com.gameObject)
                {
                    _components[i] = null;
                    Debug.LogWarning("去除组件被重复添加，Bridge：" + _bridge.name + " 组件：" + com.name);
                }
            }
        }
        public static void RemoveComponentFromAboveBridge(Component com)
        {
            SetDirty();
            var bridge = FindBridgeAboveComponent(com, true);
            if (bridge)
            {
                RemoveFromBridge(bridge, com);
                ClearBridgeNullOrRepeat(bridge);
            }
        }
        //找到离组件最近的bridge
        public static ComponentBridge FindBridgeAboveComponent(Component com, bool ignoreSelf = false)
        {
            ComponentBridge bridge = null;
            if(!ignoreSelf)
            {
                bridge = com.GetComponent<ComponentBridge>();
                if (bridge != null)
                {
                    return bridge;
                }
            }
            Transform parent = com.transform.parent;
            while(parent != null)
            {
                bridge = parent.GetComponent<ComponentBridge>();
                if (bridge!= null)
                {
                    return bridge;
                }
                else
                {
                    parent = parent.parent;
                }
            }
            return null;
        }

        //找到gameobject的程序需要考虑的组件
        public static Component FindComponentInGameObject(GameObject obj)
        {
            Component com = null;
            foreach(var type in m_ComponentTypeList)
            {
                com = obj.GetComponent(type);
                if(com != null)
                {
                    return com;
                }
            }
            return null;
        }
       
    
    }
}
