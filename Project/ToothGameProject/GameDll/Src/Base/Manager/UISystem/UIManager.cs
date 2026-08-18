using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using GameDll;
using LCL;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityUI;

namespace GameHot
{
    //public class UIEventTypeHF
    //{
    //    public const int onSubmit = 0;
    //    public const int onClick = 1;
    //    public const int onHover = 2;
    //    public const int onToggleChanged = 3;
    //    public const int onSliderChanged = 4;
    //    public const int onScrollbarChanged = 5;
    //    public const int onDrapDownChanged = 6;
    //    public const int onInputFieldChanged = 7;
    //    public const int onToggleGroupChanged = 8;
    //}
    public enum WindowLayer
    {
        Effect = 1,
        HUD,
        Hold,
        Float,
        Popup,
        Guide,
        Loading,
        Notice,
        Top,
        AffterEffect,
        Count
    }



    /// <summary>
    ///  说明：
    ///  1、开启窗口案例：
    ///  if(m_friend_list_wnd == null || m_friend_list_wnd.IsLogicClosed())
    ///  {
    ///     m_friend_list_wnd = UIManager.OpenWindowEX<friend_list_wnd>(this);
    ///  }
    ///  2、关闭窗口
    ///  if (m_friend_list_wnd != null)
    ///  {
    ///      UIManager.CloseWindow(m_friend_list_wnd);
    ///      m_friend_list_wnd = null;
    ///  }
    /// </summary> 
    public class UIManager
    {
        #region UI管理常量
        private const int m_DefaultDesignWidth = 1920;
        private const int m_DefaultDesignHeight = 1080;

        private const float m_GlobalUIRootPosX = 1000.0f;
        private const float m_HiddenPosXY = 10000.0f;

        private const float m_UICameraOrthographicSize = 1.0f;
        private const float m_UICameraNearClipPlane = 0.0f;
        private const float m_UICameraFarClipPlane = 1000.0f;
        private const float m_UICanvasPlaneDistance = 15.0f;
        private const int m_UICameraDepth = 2;
        private const float m_UICameraLocalPosZ = -100000.0f;
        private const int m_UICameraURPType = 1;

        private const int m_LayerSortOrderStep = 1000;
        private const int m_LayerCanvasBaseZ = 120000;
        private const int m_LayerCanvasZStep = 20000;
        private const int m_WindowRenderOrderStep = 10;
        private const int m_WindowsDistance = 1500;

        private const float m_MatchWidthMinAspect = 1280.0f / 720.0f;
        private const float m_MatchWidthMaxAspect = 1600.0f / 720.0f;

        private const int m_BasePixelDragThreshold = 10;
        private const int m_HighResolutionPixelDragThreshold = 20;
        private const int m_HighResolutionShortEdge = 1080;
        private const int m_HighResolutionLongEdge = 1920;

        private const float m_MaskSize = 2000.0f;
        private const float m_MaskAlpha = 3.0f / 255.0f;
        #endregion

        #region UI管理状态
        private static GameObject m_GlobalCanvas;
        private static GameObject m_GlobalUI;
        private static Dictionary<int, Transform> m_LayerGameObjects = new Dictionary<int, Transform>();
        //存储某个层的基础渲染顺序
        private static Dictionary<int, int> m_LayerRenderOrder = new Dictionary<int, int>();
        //存储某个层的逻辑窗口
        private static Dictionary<int, List<WindowBase>> m_LayerWindows = new Dictionary<int, List<WindowBase>>();
        public static EventSystem m_EventSystem;
        public static Camera m_UICamera;
        public static CanvasScaler m_CanvasScaler;
        public static RectTransform m_CanvasRect;

        private static GameObject m_MaskUI = null;
        private static int m_MaskUIRefCount = 0;
        #endregion

        public static void Init()
        {
            CreateUIStruct();
            RenderEvent.Event.OnClickUIHelper += OnClickUIHelper;
        }
        public static void Destroy()
        {
            RenderEvent.Event.OnClickUIHelper -= OnClickUIHelper;
            int loaded_count = m_OpenedList.Count;
            for (int i = 0; i < loaded_count; ++i)
            {
                WindowBase ui = m_OpenedList[i];
                ui.__Destroy();
            }
            m_OpenedList.Clear();
            ResetRemoteMenuBindingState();
            RenderAPI.ClearUIMenu();
            UDebug.Log("WindowManagerHF Destroy");
        }

        private static void OnClickUIHelper(string lanId)
        {
            //UIManager.OpenWindowAllowMultiEX<help_wnd>(null, lanId);
        }

        public static void CreateUIStruct()
        {
            UDebug.Log("Create UI Struct In Dll");
            m_GlobalUI = GameObject.Find("GlobalUI");
            GameObject.DontDestroyOnLoad(m_GlobalUI);
            m_GlobalUI.transform.position = new Vector3(m_GlobalUIRootPosX, 0, 0);
            //相机
            GameObject camgo = GameObject.Find("GlobalUI/GlobalCanvas/UICamera");
            camgo.tag = "UICamera";
            Transform cam = camgo.transform;

            m_UICamera = cam.gameObject.GetComponent(typeof(Camera)) as Camera;
            m_UICamera.allowHDR = false;
            m_UICamera.orthographicSize = m_UICameraOrthographicSize;
            m_UICamera.orthographic = true;
            m_UICamera.useOcclusionCulling = true;
            m_UICamera.nearClipPlane = m_UICameraNearClipPlane;
            m_UICamera.farClipPlane = m_UICameraFarClipPlane;
            m_UICamera.depth = m_UICameraDepth;
            m_UICamera.clearFlags = CameraClearFlags.Depth;
            m_UICamera.cullingMask = LayerMask.GetMask("UI", "UI3D", "UIVFX");
            m_UICamera.transform.localPosition = new Vector3(0, 0, m_UICameraLocalPosZ);
            //RenderAPI.SetCameraURPType(m_UICamera, m_UICameraURPType);
            //RenderAPI.SetUICamera(m_UICamera);
            //RenderAPI.SetCameraSplit(m_UICamera, false);
            RenderAPI.SetResolution();

            //画布
            m_GlobalCanvas = GameObject.Find("GlobalUI/GlobalCanvas");
            m_GlobalCanvas.transform.SetParent(m_GlobalUI.transform, false);

            //cam.transform.SetParent(m_GlobalCanvas.transform, false);

            m_GlobalCanvas.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = m_GlobalCanvas.GetComponent(typeof(Canvas)) as Canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            //canvas.worldCamera = m_UICamera;
            canvas.planeDistance = m_UICanvasPlaneDistance;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            canvas.vertexColorAlwaysGammaSpace = true;

            m_CanvasRect = m_GlobalCanvas.GetComponent(typeof(RectTransform)) as RectTransform;

            //画布适配器
            m_CanvasScaler = m_GlobalCanvas.GetComponent(typeof(CanvasScaler)) as CanvasScaler;
            //ResetUIScale(RenderAPI.m_RefWidth, RenderAPI.m_RefHeight);
            ResetUIScale(m_DefaultDesignWidth, m_DefaultDesignHeight);
            var raycaster = m_GlobalCanvas.GetComponent(typeof(GraphicRaycaster)) as GraphicRaycaster;
            

            //事件
            GameObject evtObj = GameObject.Find("GlobalUI/EventSystem");
            evtObj.transform.SetParent(m_GlobalUI.transform, false);
            m_EventSystem = evtObj.GetComponent(typeof(EventSystem)) as EventSystem;
            m_EventSystem.pixelDragThreshold = GetPixelDragThresholdByResolution();
            InputSystemCompat.EnsureInputSystemUIInputModule(evtObj);



            //层级
            //注意：枚举的GetValues可能无法在热更新里面用
            for (int i = 1;i < (int)WindowLayer.Count; ++i)
            {
                WindowLayer layer = (WindowLayer)i;
                string name = GetWindowLayerName(layer);
                Transform tr = m_GlobalCanvas.transform.Find(name);
                GameObject obj;
                if (tr == null)
                {
                    obj = new GameObject(name);
                    obj.transform.SetParent(m_GlobalCanvas.transform, false);
                    RectTransform rt = obj.AddComponent(typeof(RectTransform)) as RectTransform;
                    obj.layer = LayerMask.NameToLayer("UI");

                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.sizeDelta = new Vector2(0, 0);
                    tr = rt;
                }
                else
                {
                    obj = tr.gameObject;
                }
                SetWindowLayerCanvas(obj, layer);
                m_LayerGameObjects.Add((int)layer, tr);
                tr.SetAsLastSibling();
            }

            //添加mask层
            Transform top = GetLayer(WindowLayer.Top);
            m_MaskUI = new GameObject("mask");
            RenderAPI.SetParent(m_MaskUI.transform, top, false);
            RenderAPI.ResetTransform(m_MaskUI.transform, true);
            var mask_rect = (RectTransform)m_MaskUI.AddComponent(typeof(RectTransform));
            mask_rect.sizeDelta = new Vector2(m_MaskSize, m_MaskSize);
            Image img = RenderAPI.GetOrAddComponent(m_MaskUI, typeof(Image)) as Image;
            img.color = new Color(0, 0, 0, m_MaskAlpha);
            img.raycastTarget = true;
            m_MaskUIRefCount = 0;
            SetEnableMask(false);
        }
        public static int GetPixelDragThresholdByResolution()
        {
            int shortEdge = Mathf.Min(Screen.width, Screen.height);
            int longEdge = Mathf.Max(Screen.width, Screen.height);

            if (shortEdge > m_HighResolutionShortEdge || longEdge > m_HighResolutionLongEdge)
            {
                return m_HighResolutionPixelDragThreshold;
            }

            return m_BasePixelDragThreshold;
        }

        public static Vector2 GetUICanvasScalerSize()
        {
            return m_UICanvasScalerSize;
        }
        private static Vector2 m_UICanvasScalerSize;

        //offset 针对变化量进行一个设计值和实际值的缩放
        public static Vector2 ConvertDesigner2RealOffset(Vector2 offset)
        {
            return new Vector2(offset.x * m_UICanvasScalerSize.x / m_DefaultDesignWidth, offset.y * m_UICanvasScalerSize.y / m_DefaultDesignHeight);
        }
        public static void ResetUIScale(int width, int height)
        {
            m_UICanvasScalerSize = new Vector2(width, height);
            m_CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            m_CanvasScaler.referenceResolution = new Vector2(width, height);
            m_CanvasScaler.matchWidthOrHeight = SetMatchWidthOrHeight();
        }
        public static void HideLayer(WindowLayer layer,bool hide)
        {
            var trans = GetLayer(layer);
            float z = trans.localPosition.z;
            
            if(hide)
            {
                trans.localPosition = new Vector3(m_HiddenPosXY, m_HiddenPosXY, z);
            }
            else
            {
                trans.localPosition = new Vector3(0, 0, z);
            }
        }

        //0表示适配width，  1表示适配高度
        private static float SetMatchWidthOrHeight()
        {
            //当宽的比例在缩小 就应该满足宽 也就是靠近0
            float min_value = m_MatchWidthMinAspect;
            float max_value = m_MatchWidthMaxAspect;

            float per = 1.0f * Screen.width / Screen.height;
            if (per < min_value)
            {
                return 0;
            }
            else if (per >= max_value)
            {
                return 1;
            }
            else
            {
                float t = (per - min_value) / (max_value - min_value);
                return Mathf.Lerp(0, 1.0f, t);
            }
        }
        private static void SetWindowLayerCanvas(GameObject obj, WindowLayer layer)
        {
            int idx = (int)layer;
            Canvas childCanvas = obj.GetComponent(typeof(Canvas)) as Canvas;
            if(childCanvas == null)
            {
                childCanvas = obj.AddComponent(typeof(Canvas)) as Canvas;
            }
            childCanvas.overrideSorting = true;
            childCanvas.sortingLayerName = "Default";
            childCanvas.sortingOrder = idx * m_LayerSortOrderStep;
            childCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            childCanvas.vertexColorAlwaysGammaSpace = true;

            m_LayerRenderOrder.Add(idx, childCanvas.sortingOrder);

            //childCanvas.gameObject.transform.localPosition = new Vector3(0, 0, m_LayerCanvasBaseZ - idx * m_LayerCanvasZStep);
            childCanvas.gameObject.transform.localPosition = new Vector3(0, 0, 0);
            var raycaster = obj.GetComponent(typeof(GraphicRaycaster));
            if(raycaster == null)
            {
                raycaster = obj.AddComponent(typeof(GraphicRaycaster));
            }
            
        }
        private static string GetWindowLayerName(WindowLayer layer)
        {
            string name = "";
            if (layer == WindowLayer.Effect)
            {
                name = "Effect";
            }
            else if (layer == WindowLayer.Guide)
            {
                name = "Guide";
            }
            else if (layer == WindowLayer.Float)
            {
                name = "Float";
            }
            else if (layer == WindowLayer.Hold)
            {
                name = "Hold";
            }
            else if (layer == WindowLayer.HUD)
            {
                name = "HUD";
            }
            else if (layer == WindowLayer.Loading)
            {
                name = "Loading";
            }
            else if (layer == WindowLayer.Notice)
            {
                name = "Notice";
            }
            else if (layer == WindowLayer.Popup)
            {
                name = "Popup";
            }
            else if(layer == WindowLayer.Top)
            {
                name = "Top";
            }
            else if (layer == WindowLayer.AffterEffect)
            {
                name = "AffterEffect";
            }
            else
            {
                name = "NoneLayer";
            }
            return name;
        }

        public static List<WindowBase> GetWindow(string classEnum)
        {
            int count = m_OpenedList.Count;
            List<WindowBase> uis = null;
            for (int i = 0; i < count; ++i)
            {
                WindowBase ui = m_OpenedList[i];
                if (ui != null)
                {
                    if (ui.__GetWindowName() == classEnum && ui.__IsVisiable())
                    {
                        if(uis == null)
                        {
                            uis = new List<WindowBase>();
                        }
                        uis.Add(ui);
                    }
                }
            }
            return uis;
        }
        public static WindowBase GetFirstWindow(string classEnum)
        {
            int count = m_OpenedList.Count;
            for (int i = 0; i < count; ++i)
            {
                WindowBase ui = m_OpenedList[i];
                if (ui != null)
                {
                    if (ui.__GetWindowName() == classEnum && ui.__IsVisiable())
                    {
                        return ui;
                    }
                }
            }
            return null;
        }

        private static List<WindowBase> m_NeedCloseWindows = new List<WindowBase>();
        public static void CloseWindow(string classEnum, bool jumpAnimation = false)
        {
            int count = m_OpenedList.Count;
            m_NeedCloseWindows.Clear();
            for(int i =0; i<count; ++i)
            {
                m_NeedCloseWindows.Add(m_OpenedList[i]);
            }
            for (int i = 0; i < count; ++i)
            {
                WindowBase ui = m_NeedCloseWindows[i];
                if (ui != null)
                {
                    if (ui.__GetWindowName() == classEnum)
                    {
                        CloseWindow(ui, jumpAnimation);
                    }
                }
            }
            m_NeedCloseWindows.Clear();
        }
        public static void CloseWindow(WindowBase ui, bool jumpAnimation = false)
        {
            if (ui != null && ui.__IsLogicOpen())
            {
                int layer = (int)ui.__GetWindowLayer();

                if(m_LayerWindows.ContainsKey(layer))
                {
                    m_LayerWindows[layer].Remove(ui);
                }

                OnWindowClosed(ui);

                if (ui.__IsObjLoaded() == false)
                {
                    //要关闭的窗口还没有加载进来
                    ui.__SetLogicOpen(false);
                    //有个别窗口的加载不用遮罩操作
                    if (ui.__EnableLoadingMask)
                    {
                        //添加遮罩
                        SetEnableMask(false);
                    }
                    ui.__CloseWindowWithChild(jumpAnimation);
                }
                else if (ui.__GetWindowStage() == WindowStage.ReopenPending)
                {
                    // ReopenPending 可能仍然保持可见（来自 Closing 反向复开）；这里优先撤销预约，避免重复执行关闭逻辑。
                    ui.__CancelPendingReopen();
                }
                else if (ui.__IsVisiable())
                {
                    ui.__SetLogicOpen(false);
                    ui.__CloseWindowWithChild(jumpAnimation);
                }
                else
                {
                    ui.__SetLogicOpen(false);
                    UDebug.LogWarning("关闭处于隐藏态但逻辑仍为打开的窗口，按兜底逻辑执行关闭：" + ui.ToString());
                    ui.__CloseWindowWithChild(jumpAnimation);
                }

            }
            else
            {
                //关闭的窗口可能已经被自身等原因关闭了
                Debug.LogError("关闭的窗口不存在，" + ui.ToString());
            }
        }
        public static void CloseAll(List<WindowBase> filters = null, bool jumpAnimation = false)
        {
            int count = m_OpenedList.Count;

            m_NeedCloseWindows.Clear();
            for (int i = 0; i < count; ++i)
            {
                m_NeedCloseWindows.Add(m_OpenedList[i]);
            }

            for (int i = 0; i < count; ++i)
            {
                WindowBase ui = m_NeedCloseWindows[i];
                if (ui != null)
                {
                    bool needClose = true;
                    string name = ui.__GetWindowName();
                    if (name == "guide_wnd" || name == "network_delay_wnd")
                    {
                        needClose = false;
                    }
                    else if (filters != null)
                    {
                        int filter_count = filters.Count;
                        for (int n = 0; n < filter_count; ++n)
                        {
                            if (ui == filters[n])
                            {
                                needClose = false;
                                continue;
                            }
                        }
                    }
                    if (needClose)
                    {
                        CloseWindow(ui, jumpAnimation);
                    }
                }
            }
        }
        public static void WindowDestroy(WindowBase ui)
        {
            if (ui != null)
            {
                OnWindowClosed(ui);
                if (ui.__IsDestroy() == false)
                {
                    //这里的Destroy操作是针对表现层
                    ui.__Destroy();
                }
                ui.OnClassDestroyed();
                m_OpenedList.Remove(ui);
                RefreshRemoteMenuBinding();
            }
        }
        private static List<WindowBase> m_OpenedList = new List<WindowBase>();
        private static WindowBase m_LastRemoteMenuWindow = null;
        private static UIWindow m_LastRemoteMenuUI = null;
        private static bool m_HasRemoteMenuBinding = false;

        public static void OnWindowActivated(WindowBase win)
        {
            RefreshRemoteMenuBinding();
        }

        public static void OnWindowActiveButtonsChanged(WindowBase win)
        {
            if (win == null || GetCurrentActiveWindow() != win)
            {
                return;
            }

            // 菜单来源窗口没有变化时，普通刷新会被去重；业务主动重建菜单时必须使旧绑定失效。
            ResetRemoteMenuBindingState();
            RefreshRemoteMenuBinding();
        }

        private static void OnWindowClosed(WindowBase win)
        {
            RefreshRemoteMenuBinding();
        }

        private static bool IsWindowCanActivate(WindowBase win)
        {
            // 遥控菜单焦点必须跟随“当前真正可见且仍在交互层级里的窗口”，
            // 不能只看打开顺序，否则异步晚到窗口和缓存关闭窗口都会抢走菜单。
            if (win == null || !win.__IsLogicOpen() || !win.__IsObjLoaded() || !win.__IsVisiable() || !win.__IsObjActiveInHierarchy())
            {
                return false;
            }

            // 是否参与逻辑顶层窗口，由业务窗口自己显式声明，
            // 这里只看逻辑配置与显示状态，不再要求一定存在 UIWindow。
            if (!win.__CanBeCurrentActiveWindow())
            {
                return false;
            }

            return true;
        }

        public static WindowBase GetCurrentActiveWindow()
        {
            WindowBase currentWindow = null;
            int currentRenderOrder = int.MinValue;

            for (int i = 0; i < m_OpenedList.Count; ++i)
            {
                WindowBase win = m_OpenedList[i];
                if (!IsWindowCanActivate(win))
                {
                    continue;
                }

                if (currentWindow == null || win.__GetRenderOrder() >= currentRenderOrder)
                {
                    currentWindow = win;
                    currentRenderOrder = win.__GetRenderOrder();
                }
            }

            return currentWindow;
        }

        private static void ResetRemoteMenuBindingState()
        {
            m_LastRemoteMenuWindow = null;
            m_LastRemoteMenuUI = null;
            m_HasRemoteMenuBinding = false;
        }

        // 菜单焦点统一从这里刷新，避免打开、关闭、异步加载完成和缓存回收各自维护一套状态。
        public static void RefreshRemoteMenuBinding()
        {
            WindowBase currentWindow = GetCurrentActiveWindow();
            if (currentWindow == null)
            {
                if (m_HasRemoteMenuBinding || m_LastRemoteMenuWindow != null || m_LastRemoteMenuUI != null)
                {
                    RenderAPI.ClearUIMenu();
                    ResetRemoteMenuBindingState();
                }
                return;
            }

            var ui = currentWindow.GetUIWindow();
            bool hasActiveMenu = ui != null && ui.HasActiveButton();
            if (hasActiveMenu)
            {
                // 逻辑顶层窗口没变、菜单源没变时，不要因为其他窗口的开关动画再次重绑。
                // 否则 ResetMenu 会重复触发选中态刷新，业务层会误以为真的发生了菜单切换。
                if (m_LastRemoteMenuWindow == currentWindow &&
                    m_LastRemoteMenuUI == ui &&
                    m_HasRemoteMenuBinding)
                {
                    return;
                }

                int defaultRowIndex = ui.m_DefRowIndex;
                if (!currentWindow.__ShouldSelectDefaultActiveButton())
                {
                    // 使用现有默认行参数传递“暂不选择”，避免为跨程序集的 RenderAPI 增加接口依赖。
                    ui.m_DefRowIndex = -1;
                }

                RenderAPI.BindUIMenu(ui);
                ui.m_DefRowIndex = defaultRowIndex;
                m_LastRemoteMenuWindow = currentWindow;
                m_LastRemoteMenuUI = ui;
                m_HasRemoteMenuBinding = true;
            }
            else
            {
                if (m_HasRemoteMenuBinding || m_LastRemoteMenuWindow != currentWindow || m_LastRemoteMenuUI != ui)
                {
                    RenderAPI.ClearUIMenu();
                }

                m_LastRemoteMenuWindow = currentWindow;
                m_LastRemoteMenuUI = ui;
                m_HasRemoteMenuBinding = false;
            }
        }


        public static void HideAll()
        {
            m_GlobalUI.SetActive(false);
        }
        public static Vector2 GetUIScreenSize()
        {
            return m_CanvasRect.sizeDelta;
        }
        private static void ReopenWindow(WindowBase win,WindowBase parent, params object[] param)
        {
            // 复开和首次加载完成后的打开流程共用同一个入口，确保进入 Opening 链后的行为保持一致。
            StartWindowOpenFlow(win, parent, param);
        }
        private static void StartWindowOpenFlow(WindowBase win, WindowBase parent, params object[] param)
        {
            WindowStage previousStage = win.__GetWindowStage();
            if (parent != null)
            {
                parent.__AddChild(win);
            }
            win.__SetUserData(param);
            win.__ShowWindowObj();
            if (ShouldPreserveQueuedLayerOrder(previousStage))
            {
                // 异步打开的窗口在发起请求时就已经占好了层级位置，
                // 资源晚到时只刷新排序，不能重新插到末尾，否则会反过来压住后打开的页面。
                RefreshLayerWindowOrder(win.__GetWindowLayer());
            }
            else
            {
                // 缓存复开和关闭打断复开仍然要重新插回当前父子层级，保证表现顺序和当前关系一致。
                ReinsertWindowToCurrentLayer(win, parent);
            }
            if (win.__IsObjLoaded())
            {
                win.__SetWindowPos(win.__GetWindowTransform().position);
            }
            win.__BindWindowData();
        }

        private static bool ShouldPreserveQueuedLayerOrder(WindowStage previousStage)
        {
            return previousStage == WindowStage.Loading || previousStage == WindowStage.Constructed;
        }

        private static void RefreshLayerWindowOrder(WindowLayer layer)
        {
            List<WindowBase> wins = GetWindowsByLayer(layer);
            if (wins == null)
            {
                return;
            }

            SortLayerWindows(wins, layer);
        }
        private static void ReinsertWindowToCurrentLayer(WindowBase win, WindowBase parent)
        {
            // 先从所有 layer 列表移除，再按当前 WindowBase.m_Layer 和父子关系重新插回正确位置。
            foreach (var pair in m_LayerWindows)
            {
                pair.Value.Remove(win);
            }

            int layerKey = (int)win.__GetWindowLayer();
            List<WindowBase> wins = GetWindowsByLayer(win.__GetWindowLayer());
            if (wins == null)
            {
                wins = new List<WindowBase>();
                m_LayerWindows.Add(layerKey, wins);
            }

            AddWindowToParentLast(wins, win, parent);
            SortLayerWindows(wins, win.__GetWindowLayer());
        }
        // 已经显示中的窗口再次被打开时，只前置层级，不重复触发打开流程。
        private static void MoveOpenedWindowToFront(WindowBase win)
        {
            if (win == null || !win.__IsObjLoaded() || !win.__IsVisiable() || !win.__IsLogicOpen())
            {
                return;
            }

            List<WindowBase> wins = GetWindowsByLayer(win.__GetWindowLayer());
            if (wins == null || wins.Count == 0)
            {
                return;
            }

            List<WindowBase> keepWins = new List<WindowBase>(wins.Count);
            List<WindowBase> moveWins = new List<WindowBase>();
            int count = wins.Count;
            for (int i = 0; i < count; ++i)
            {
                WindowBase layerWin = wins[i];
                if (layerWin == win || layerWin.__IsChildOf(win) || layerWin.__IsGrandsonOf(win))
                {
                    moveWins.Add(layerWin);
                }
                else
                {
                    keepWins.Add(layerWin);
                }
            }

            if (moveWins.Count == 0)
            {
                return;
            }

            wins.Clear();
            wins.AddRange(keepWins);
            wins.AddRange(moveWins);
            SortLayerWindows(wins, win.__GetWindowLayer());
        }
        public static WindowBase OpenWindowAllowMulti(string classEnum, WindowBase parent, params object[] param)
        {
            var win = OpenOldWindow(classEnum, parent, true, param);
            if (win != null)
            {
                return win;
            }
            return CreateNewWindow(classEnum, parent, param);
        }
        public static WindowBase OpenWindow(string classEnum, WindowBase parent, params object[] param)
        {
            var win = OpenOldWindow(classEnum, parent, false, param);
            if (win != null)
            {
                return win;
            }
            return CreateNewWindow(classEnum, parent, param);
        }
        private static WindowBase OpenOldWindow(string classEnum, WindowBase parent, bool allowMulti, params object[] param)
        {
            int count = m_OpenedList.Count;
            for (int i = 0; i < count; ++i)
            {
                var win = m_OpenedList[i];
                if (win.__GetWindowName() == classEnum && IsWindowCanReopen(win, parent, allowMulti, param))
                {
                    return win;
                }
            }
            return null;
        }


        private static bool IsWindowCanReopen(WindowBase win, WindowBase parent, bool allowMulti, params object[] param)
        {
            switch (win.__GetWindowStage())
            {
                case WindowStage.Constructed:
                case WindowStage.Loading:
                    {
                        // Loading 阶段重复打开单例时，只更新请求参数，沿用当前加载流程。
                        if (allowMulti)
                        {
                            return false;
                        }
                        ContinueLoadingWindow(win, parent, param);
                        return true;
                    }
                case WindowStage.Opening:
                case WindowStage.Opened:
                    {
                        // Opening/Opened 阶段命中单例时，不重跑完整打开链路，只做前置和按需刷新。
                        if (allowMulti)
                        {
                            return false;
                        }
                        //这个函数的流程，后面还需要好好思考下。因为有可能会打断成对的流程。
                        RepeatOpenWindow(win, param);
                        MoveOpenedWindowToFront(win);
                        OnWindowActivated(win);
                        return true;
                    }
                case WindowStage.Closing:
                    {
                        // Closing 说明旧实例仍在执行关闭流程；单例打开时要尝试把它抢回来复用。
                        if (allowMulti)
                        {
                            return false;
                        }
                        return ReclaimWindowForReopen(win, parent, false, param);
                    }
                case WindowStage.ReopenPending:
                case WindowStage.Cached:
                    {
                        // ReopenPending/Cached 都属于“旧实例可继续复用”的路径，只是前者已经预约过复开。
                        if (allowMulti)
                        {
                            return ReclaimWindowForReopen(win, parent, true, param);
                        }
                        return ReclaimWindowForReopen(win, parent, false, param);
                    }
                case WindowStage.Destroying:
                case WindowStage.Destroyed:
                case WindowStage.LoadFailed:
                case WindowStage.None:
                default:
                    return false;
            }
        }

        private static void ContinueLoadingWindow(WindowBase win, WindowBase parent, params object[] param)
        {
            if (parent != null)
            {
                parent.__AddChild(win);
            }
            win.__SetUserData(param);
            win.__SetLogicOpen(true);
        }

        private static void RepeatOpenWindow(WindowBase win, params object[] param)
        {
            win.__HandleRepeatOpen(param);
        }

        private static bool ReclaimWindowForReopen(WindowBase win, WindowBase parent, bool allowMulti, params object[] param)
        {
            // 多开模式下，仍处于 Closing 的旧实例不参与复用，避免和新实例并发打架。
            if (allowMulti && win.__GetWindowStage() == WindowStage.Closing)
            {
                return false;
            }

            // ReopenPending 说明该旧实例已经预约过一次复开；单例场景只需要覆盖最新参数即可。
            if (win.__GetWindowStage() == WindowStage.ReopenPending)
            {
                if (allowMulti)
                {
                    return false;
                }

                if (parent != null)
                {
                    parent.__AddChild(win);
                }
                win.__SetUserData(param);
                win.__SetLogicOpen(true);
                return true;
            }

            var _win = win;
            if (parent != null)
            {
                parent.__AddChild(win);
            }
            // 在真正下一帧复开前，先把最新 parent/param 记到旧实例上，避免闭包拿到过期数据。
            win.__SetUserData(param);

            //模拟加载资源中，下一帧再真正复开窗口，表现上与首次打开一致。
            RenderAPI.NextFrameCall(() =>
            {
                // ReopenWindow 是“重新进入 Opening 链”的入口，不是“开启动画播完”的终点。
                // 因此这里下一帧只要旧实例仍处于 ReopenPending，就应立即开始复开，表现上与首次打开一致。
                if (_win.__IsObjLoaded() &&
                    _win.__GetWindowStage() == WindowStage.ReopenPending &&
                    _win.__IsLogicOpen())
                {
                    ReopenWindow(_win, _win.__GetParentWindow(), _win.__GetUserData());
                }
            });
            win.__MarkPendingReopen();
            win.__SetLogicOpen(true);
            return true;
        }

        public static T OpenWindowAllowMultiEX<T>(WindowBase parent, params object[] param) where T : WindowBase, new()
        {
            var win = OpenOldWindowEX<T>(parent, true, param);
            if (win != null)
            {
                return win as T;
            }
            return CreateNewWindow<T>(parent, param);
        }
        public static T OpenWindowEX<T>(WindowBase parent, params object[] param) where T:WindowBase,new()
        {
            var win = OpenOldWindowEX<T>(parent, false, param);
            if (win != null)
            {
                return win as T;
            }
            return CreateNewWindow<T>(parent, param);
        }
        private static T OpenOldWindowEX<T>(WindowBase parent, bool allowMulti, params object[] param) where T : WindowBase, new()
        {
            int count = m_OpenedList.Count;
            for (int i = 0; i < count; ++i)
            {
                var win = m_OpenedList[i];
                if (win is T && IsWindowCanReopen(win, parent, allowMulti, param))
                {
                    return win as T;
                }
            }
            return null;
        }
        private static T CreateNewWindow<T>(WindowBase parent, params object[] param) where T:WindowBase,new()
        {
            T win = new T();
            win.OnClassConstructed();

            string uifile = "";
            string className = typeof(T).Name;
            if (!string.IsNullOrEmpty(win.__CustomUIPrefab))
            {
                //支持使用文件夹的形式来管理UI的预制件，路径是相对于“art/out/ui”的
                uifile = win.__CustomUIPrefab;
            }
            else
            {
                uifile = typeof(T).Name;
                if (!string.IsNullOrEmpty(win.__CustomUIPrefabDir))
                {
                    uifile = win.__CustomUIPrefabDir + uifile;
                }
            }

            if (win != null)
            {
                win.__SetWindowName(className);

                CreatNewWindowImp(win, uifile, parent, param);

                return win;
            }
            else
            {
                UDebug.LogError("打开的窗口不存在，" + uifile);
            }
            return null;
        }
        private static WindowBase CreateNewWindow(string className, WindowBase parent, params object[] param)
        {
            WindowBase win = WindowCreator.GetWindowInstance(className);
            if (win != null)
            {
                win.OnClassConstructed();

                string fileName = className;
                if(!string.IsNullOrEmpty( win.__CustomUIPrefab))
                {
                    fileName = win.__CustomUIPrefab;
                }
                else
                {
                    if (!string.IsNullOrEmpty(win.__CustomUIPrefabDir))
                    {
                        fileName = win.__CustomUIPrefabDir + fileName;
                    }
                }
                win.__SetWindowName(className);

                CreatNewWindowImp(win, fileName, parent, param);

                return win;
            }
            else
            {
                UDebug.LogError("打开的窗口不存在，" + className);
                return null;
            }
        }

        private static void CreatNewWindowImp(WindowBase win, string uifile, WindowBase parent, params object[] param)
        {
            string root_path = "ui/{0}";
            StringBuilder sb = Tool.StringBuilder.AppendFormat(root_path + MonoTool.GetAssetbundleSuffix(), uifile.ToLower());
            string abName = sb.ToString();
            sb.Clear();
            win.__SetLogicOpen(true);
            win.__SetWindowStage(WindowStage.Loading);

            //有个别窗口的加载不用遮罩操作
            if (win.__EnableLoadingMask)
            {
                //添加遮罩
                SetEnableMask(true);
            }

            //win.__SetWindowName(uifile);
            win.__SetUserData(param);

            ABRequest id = null;
            id = LCL.UIRes.LoadPrefabAsync(typeof(GameObject), abName, Tool.GetAssetName(abName), (abobject, userData) =>
            {
                if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXEditor)
                {
                    OnLoadedUIPrefab(win, id, abobject);
                }
                else
                {
                    try
                    {
                        OnLoadedUIPrefab(win, id, abobject);
                    }
                    catch (Exception e)
                    {
                        UDebug.LogError(e.ToString());
                    }
                }

            }, 
            (load_index, hr) => 
            {
                if (hr == ABRequestResult.LoadError)
                {
                    OnLoadUIPrefabError(win, id);
                }
            }, null);
            win.__SetABId(id);
            if (parent != null)
            {
                parent.__AddChild(win);
            }
            m_OpenedList.Add(win);
            //添加逻辑窗口和层的对应关系
            List<WindowBase> winList = GetWindowsByLayer(win.__GetWindowLayer());
            if (winList != null)
            {
                winList = AddWindowToParentLast(winList, win, parent);
            }
            else
            {
                winList = new List<WindowBase>();
                winList = AddWindowToParentLast(winList, win, parent);
                m_LayerWindows.Add((int)win.__GetWindowLayer(), winList);
            }
        }

        private static void OnLoadUIPrefabError(WindowBase win, ABRequest id)
        {
            OnWindowClosed(win);
            if (win.__IsLogicOpen())
            {
                //有个别窗口的加载不用遮罩操作
                if (win.__EnableLoadingMask)
                {
                    //添加遮罩
                    SetEnableMask(false);
                }
            }
            UIRes.UnloadPrefab(id);
            var parent = win.__GetParentWindow();
            if (parent != null)
            {
                //parent.__RemoveChild(win);
                //这里是资源异常释放，不能走逻辑流程
                parent.__RemoveChildForUIPrefabError(win);
            }
            m_OpenedList.Remove(win);

            List<WindowBase> winList = GetWindowsByLayer(win.__GetWindowLayer());
            if(winList != null)
            {
                winList.Remove(win);
            }
            win.__SetLogicOpen(false);
            win.__SetWindowStage(WindowStage.LoadFailed);
            RefreshRemoteMenuBinding();
        }

        //异步加载UI资源回调
        private static void OnLoadedUIPrefab(WindowBase win, ABRequest id, ResData abobject)
        {
            #region 异步加载UI资源回调
            if (win.__IsLogicOpen())
            {
                //有个别窗口的加载不用遮罩操作
                if (win.__EnableLoadingMask)
                {
                    //添加遮罩
                    SetEnableMask(false);
                }
            }

            bool findWnd = false;
            int count = m_OpenedList.Count;
            for (int i = 0; i < count; ++i)
            {
                if (m_OpenedList[i] == win)
                {
                    findWnd = true;
                    break;
                }
            }
            if (findWnd == false)
            {
                UDebug.LogWarning("窗口资源加载完毕之前已经关闭了，" + win.__GetWindowName());
                UIRes.UnloadPrefab(id);
            }
            else
            {
                if (abobject.m_Obj == null)
                {
                    UDebug.LogError("UI资源加载失败， UI：" + win.__GetWindowName());
                    return;
                }
                var showObj = RenderAPI.Instantiate((GameObject)abobject.m_Obj);
                showObj.name = abobject.m_Obj.name;
                win.__SetWindowObj(showObj);
                win.__SetWindowTransform(win.__GetWindowObj().transform);
                Transform AttachObject = GetLayer(win.__GetWindowLayer());
                if(AttachObject == null)
                {
                    Debug.LogError("UI挂接的WindowLayer没有找到， layer是：" + win.__GetWindowLayer());
                    return;
                }

                //先将大小还原到他的父对象的局部坐标
                Vector3 parentScale = AttachObject.localScale;
                Vector3 now = win.__GetWindowTransform().localScale;
                Vector3 childScale = new Vector3(now.x * parentScale.x, now.y * parentScale.y, now.z * parentScale.z);
                win.__GetWindowTransform().localScale = childScale;
                //挂接到对应的位置
                RectTransform rect = win.__GetWindowObj().GetComponent(typeof(RectTransform)) as RectTransform;
                if (rect != null)
                {
                    Vector3 pos = rect.anchoredPosition3D;
                    Quaternion rotation = rect.localRotation;
                    Vector3 scale = rect.localScale;
                    Vector2 offsetMax = rect.offsetMax;
                    Vector2 offsetMin = rect.offsetMin;
                    win.__GetWindowTransform().SetParent(AttachObject);
                    rect.anchoredPosition3D = pos;
                    rect.localRotation = rotation;
                    rect.localScale = scale;
                    rect.offsetMax = offsetMax;
                    rect.offsetMin = offsetMin;
                }
                win.__SetWindowPos(win.__GetWindowTransform().position);
                // 首次加载完成后，从这里开始与复开共用同一套 Opening 流程。
                StartWindowOpenFlow(win, win.__GetParentWindow(), win.__GetUserData());
                
            }
            #endregion
        }

        //将窗口加载到紧邻父窗口的子窗口列表的最后一个位置
        private static List<WindowBase> AddWindowToParentLast(List<WindowBase> windows, WindowBase wnd, WindowBase parent)
        {
            if (parent == null)
            {
                windows.Add(wnd);
                return windows;
            }

            int count = windows.Count;
            bool findParent = false;
            for (int i = 0; i < count; ++i)
            {
                WindowBase window = windows[i];
                //先要找到父对象
                if (!findParent && window == parent)
                {
                    findParent = true;
                    continue;
                }
                if (window != null && findParent)
                {
                    if (window.__IsChildOf(parent) || window.__IsGrandsonOf(parent))
                    {
                        continue;
                    }

                    windows.Insert(i, wnd);
                    return windows;
                }
            }
            windows.Add(wnd);
            return windows;
        }
        public static List<WindowBase> GetWindowsByLayer(WindowLayer layer)
        {
            int layerInt = (int)layer;
            if (m_LayerWindows.ContainsKey(layerInt))
            {
                return m_LayerWindows[layerInt];
            }
            else
            {
                return null;
            }
        }
        public static void OnFocusWindow(WindowBase win)
        {
            var layer = win.__GetWindowLayer();
            var layer_wins = GetWindowsByLayer(layer);
            SortLayerWindows(layer_wins, layer, win);
            OnWindowActivated(win);
        }
        private static void SortLayerWindows(List<WindowBase> wins, WindowLayer layer, WindowBase last_window = null)
        {
            if(wins == null)
            {
                UDebug.LogError("UI层排序错误，该层没有任何UI" + layer.ToString());
                return;
            }
            int childCount = wins.Count;
            int baseLayerOrder = m_LayerRenderOrder[(int)layer];
            int showLayer = 0;
            for(int i= 0; i < childCount;++i)
            {
                WindowBase win = wins[i];

                if(last_window != null && last_window == win)
                {
                    continue;
                }

                showLayer = SortLayerWindow(win, baseLayerOrder, showLayer);
            }

            if(last_window != null)
            {
                showLayer = SortLayerWindow(last_window, baseLayerOrder, showLayer);
            }

        }

        private static int SortLayerWindow(WindowBase win, int baseLayerOrder, int showLayer)
        {
            if (win.__IsObjLoaded())
            {
                win.__SetAsLastSibling();
                Canvas canvas = win.__GetOrAddWindowCanvas();
                win.__AddWindowBlock();
                win.__SetRenderOrder(baseLayerOrder + showLayer * m_WindowRenderOrderStep);
                if (canvas != null)
                {
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = win.__GetRenderOrder();
                }
                showLayer++;
                win.__SetAsLastSibling();
                win.__SortRender();
                Vector3 pos = win.__GetWindowTransform().localPosition;
                //win.__GetWindowTransform().localPosition = new Vector3(pos.x, pos.y, -showLayer * m_WindowsDistance);
                win.__GetWindowTransform().localPosition = new Vector3(pos.x, pos.y, 0);
            }
            return showLayer;
        }




        public  static Transform GetLayer(WindowLayer layer)
        {
            return m_LayerGameObjects[(int)layer];
        }
        public static void SetEnableMask(bool enable)
        {
            if(enable)
            {
                m_MaskUIRefCount += 1;
                if(m_MaskUI != null && !m_MaskUI.Equals(null) && !m_MaskUI.activeSelf)
                {
                    m_MaskUI.SetActive(true);
                }
            }
            else
            {
                if (m_MaskUIRefCount > 0)
                {
                    m_MaskUIRefCount -= 1;
                    if(m_MaskUIRefCount == 0)
                    {
                        if(m_MaskUI != null && !m_MaskUI.Equals(null) && m_MaskUI.activeSelf)
                        {
                            m_MaskUI.SetActive(false);
                        }
                    }
                }
                else
                {
                    if (m_MaskUI != null && !m_MaskUI.Equals(null) && m_MaskUI.activeSelf)
                    {
                        m_MaskUI.SetActive(false);
                    }
                }
            }
        }

        public static List<WindowBase> GetWindows(WindowLayer layer)
        {
            List<WindowBase> wins = new List<WindowBase>();
            int count = m_OpenedList.Count;
            for (int i = 0; i < count; ++i)
            {
                WindowBase ui = m_OpenedList[i];
                if (ui.__GetWindowLayer() == layer && ui.__IsLogicOpen())
                {
                    wins.Add(ui);
                }
            }
            return wins;
        }
        public static int GetWindowCount(WindowLayer layer)
        {
            int num = 0;
            int count = m_OpenedList.Count;
            for (int i = 0; i < count; ++i)
            {
                WindowBase ui = m_OpenedList[i];
                if (ui.__GetWindowLayer() == layer && ui.__IsLogicOpen())
                {
                    num++;
                }
            }
            return num;
        }
        public static bool IsOpened(string name)
        {
            int count = m_OpenedList.Count;
            for (int i = 0; i < count; ++i)
            {
                WindowBase ui = m_OpenedList[i];
                if (ui != null)
                {
                    if (ui.__GetWindowName() == name && ui.__IsLogicOpen())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void ChangeGlobalCanvasRenderMode(RenderMode mode)
        {
            var worldCamera = RenderAPI.GetWorldCamera();
            if(worldCamera == null)
            {
                worldCamera = CameraFoot.GetInstance().m_CameraEye;
            }
            if(mode == RenderMode.ScreenSpaceOverlay)
            {
                RenderAPI.RemoveCameraStack(worldCamera, m_UICamera);
                m_UICamera.gameObject.SetActive(false);
                Canvas canvas = m_GlobalCanvas.GetComponent(typeof(Canvas)) as Canvas;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
            else if(mode == RenderMode.ScreenSpaceCamera)
            {
                RenderAPI.AddCameraStack(worldCamera, m_UICamera);
                m_UICamera.gameObject.SetActive(true);
                Canvas canvas = m_GlobalCanvas.GetComponent(typeof(Canvas)) as Canvas;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = m_UICamera;
            }

        }
    }
}
