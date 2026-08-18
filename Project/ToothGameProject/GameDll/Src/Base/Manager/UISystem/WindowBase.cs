using System;
using System.Collections.Generic;
using System.Text;
using GameDll;
using LCL;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;

namespace GameHot
{
    public enum WindowStage
    {
        None,           // 无状态，未创建
        Constructed,   // 类已创建，资源请求还没完成
        Loading,       // 资源异步加载中
        ReopenPending, // 已命中二次打开，等待下一帧复用旧实例重新进入打开流程
        Opening,       // 资源已创建，正在初始化/打开/播开启动画
        Opened,        // 正常显示中
        Closing,       // 正在关闭，包含关闭动画阶段
        Cached,        // 已关闭且不可见，资源还在，可复用
        Destroying,    // 正在销毁
        Destroyed,     // 已销毁
        LoadFailed     // 加载失败
    }
    public abstract class WindowModel
    {
        public abstract void Clear();
    }
    /// <summary>
    /// 使用教程
    /// 1、UI类是同步的，但是资源加载是异步的。
    /// 2、可以在UI类里面定义各种UI紧密相关的Private数据字段，使用方法读写。不推荐把字段公开
    /// 3、UI类外部访问UI类的方法的时候，要注意，只能使用设置和更新等等方式的，不得使用获取UI元素。例如可以
    /// public void SetPlayerUIInfo(int level)
    /// {
    ///     m_PlayerLevel = level;
    ///     if(IsInitializedView)
    ///     {
    ///         m_txtLevel.text = level;
    ///     }
    /// }
    /// 上面的方法先把数据存下来，然后判断界面是否加载预制件完毕，如果完毕了，可以直接设置；如果没有加载完毕就等到OnOpen函数回来了再自行设置一次。
    /// 禁止使用获取UI元素，如
    /// public Transform GetPlayerText()
    /// {
    ///     return m_txtLevel;
    /// }
    /// 哪怕预制件加载了，也不能使用这种方式
    /// 4、UI类里面的各种字段需要在OnClose做清理操作
    /// 
    /// 生命周期图（其中OnClassConstructed和OnClassDestroyed必然会执行）
    /// OnClassConstructed->OnInitComponent->OnOpen->OnClose->OnDestroy->OnClassDestroyed
    /// </summary>
    public abstract class WindowBase
    {
        #region 窗口配置
        // 允许一个窗口类复用其他预制体，路径相对 art/out/ui。
        public string __CustomUIPrefab = "";
        // 如果使用目录形式管理预制体，必须以 "/" 结尾。
        public string __CustomUIPrefabDir = "";
        // 个别窗口打开时不需要 loading mask。
        public bool __EnableLoadingMask = true;
        // 默认不参与 GetCurrentActiveWindow。
        // 需要参与逻辑顶层窗口和遥控菜单焦点判断的业务窗口，必须显式开启。
        public bool __ParticipateCurrentActiveWindow = false;
        // 默认保持历史行为；特殊窗口可关闭打开时的首项选中，首次方向输入后再建立选中项。
        public bool __SelectDefaultActiveButton = true;
        public WindowLayer m_Layer = WindowLayer.Popup;
        protected bool __OpenFocus = false;
        #endregion

        #region 核心运行状态
        private WindowModel __Model;
        private UIWindow m_UIWindow;
        private bool m_bInitComponent = false;
        private ABRequest m_ABId = null;
        private string m_WindowName;
        private int m_RenderOrder = -1;
        private WindowStage m_Stage = WindowStage.None;

        private bool m_bDestroy = true;
        private bool m_bLogicOpen = false;
        // 标记是否已经触发过 OnUIOpenedEvent，用于防止事件顺序错误。
        private bool m_HasOpenedEvent = false;
        private object[] m_UserData;
        private int m_WindowCacheTime = 1000;
        private Action m_DestroyTimeCall = null;

        private GameObject m_WinObj;
        private Transform m_WinTransform;
        private Vector3 m_WindowPos = Vector3.zero;
        private bool __bVisiable = false;
        private long m_DestroyTimer = -1;
        private int m_IsTempHide = -1;

        private Canvas m_WindowCanvas;
        private GraphicRaycaster m_Blocker;
        #endregion

        #region 关系与辅助状态
        private readonly Dictionary<string, GameObject> m_RedPointList = new Dictionary<string, GameObject>();
        private readonly List<WindowBase> m_ChildWindows = new List<WindowBase>();
        private WindowBase m_ParentWindow = null;
        private readonly List<WindowBase> m_tempChildWnds = new List<WindowBase>();

        private readonly Dictionary<Graphic, long> m_ImageIds = new Dictionary<Graphic, long>();
        private readonly Dictionary<ABRequest, GameObject> m_ABGameObjectDic = new Dictionary<ABRequest, GameObject>();
        private readonly Dictionary<long, ABRequest> m_ABRequestDic = new Dictionary<long, ABRequest>();
        private readonly Dictionary<long, ABRequest> m_ABRequestCodeDic = new Dictionary<long, ABRequest>();
        private readonly List<long> m_TimerIds = new List<long>();
        #endregion

        #region 构造与状态访问
        public virtual void OnClassConstructed()
        {
            __SetWindowStage(WindowStage.Constructed);
        }

        public virtual void OnClassDestroyed()
        {
            __SetWindowStage(WindowStage.Destroyed);
        }

        protected void __CreateModel(WindowModel model)
        {
            this.__Model = model;
        }
        protected T GetModel<T>() where T : WindowModel, new()
        {
            return (T)this.__Model;
        }
        public WindowType GetWindowType()
        {
            if(m_UIWindow == null)
            {
                return WindowType.SideFloat;
            }
            return m_UIWindow.WindowType;
        }
        //组件初始化好，并且当前UI是可见的
        protected bool IsInitializedView()
        {
            return m_bInitComponent && __IsLogicOpen() && __IsVisiable();
        }
        public void __SetABId(ABRequest id)
        {
            m_ABId = id;
        }
        public ABRequest __GetABId()
        {
            return m_ABId;
        }
        public void __SetWindowName(string name)
        {
            m_WindowName = name;
        }
        public string __GetWindowName()
        {
            return m_WindowName;
        }
        public WindowLayer __GetWindowLayer()
        {
            return m_Layer;
        }
        public bool __CanBeCurrentActiveWindow()
        {
            return __ParticipateCurrentActiveWindow;
        }
        public bool __ShouldSelectDefaultActiveButton()
        {
            return __SelectDefaultActiveButton;
        }
        public int __GetRenderOrder()
        {
            return m_RenderOrder;
        }
        public void __SetRenderOrder(int order)
        {
            m_RenderOrder = order;
        }

        public WindowStage __GetWindowStage()
        {
            return m_Stage;
        }
        public void __SetWindowStage(WindowStage stage)
        {
            m_Stage = stage;
        }
        public void __SetWindowCacheTime(int timeMMSec)
        {
            m_WindowCacheTime = timeMMSec;
        }
        public void __SetUserData(params object[] userdata)
        {
            m_UserData = userdata;
        }
        public object[] __GetUserData()
        {
            return m_UserData;
        }
        public void __HandleRepeatOpen(params object[] userdata)
        {
            __SetUserData(userdata);
            OnRepeatOpen();
        }
        public GameObject __GetWindowObj()
        {
            return m_WinObj;
        }
        public void __SetWindowObj(GameObject obj)
        {
            m_WinObj = obj;
        }
        public Transform __GetWindowTransform()
        {
            return m_WinTransform;
        }
        public void __SetWindowTransform(Transform trans)
        {
            m_WinTransform = trans;
        }
        public void __SetWindowPos(Vector3 pos)
        {
            m_WindowPos = pos;
        }
        public Vector3 __GetWindowPos()
        {
            return m_WindowPos;
        }
        public bool __IsDestroy()
        {
            return m_bDestroy;
        }
        public void __SetAsLastSibling()
        {
            if (m_WinObj != null)
            {
                m_WinTransform.SetAsLastSibling();
            }
        }
        public void __SortRender()
        {
            if (m_WinObj != null)
            {
                Component[] depth = m_WinObj.GetComponentsInChildren(typeof(UIDepth), true);
                if (depth != null)
                {
                    int count = depth.Length;
                    for (int i = 0; i < count; ++i)
                    {
                        UIDepth d = depth[i] as UIDepth;
                        if (d != null)
                        {
                            d.SetOrder(m_RenderOrder + i + 1);
                        }
                    }
                }
            }
        }
        private void __DestroyTimeCall()
        {
            __SetWindowStage(WindowStage.Destroying);
            if (m_DestroyTimer != -1)
            {
                RenderAPI.RemoveCounter(m_DestroyTimer);
                m_DestroyTimer = -1;
            }
            UIManager.WindowDestroy(this);
        }
        public bool __IsObjLoaded()
        {
            return m_WinObj != null && !m_WinObj.Equals(null);
        }
        public bool __IsObjActiveInHierarchy()
        {
            return __IsObjLoaded() && m_WinObj.activeInHierarchy;
        }
        public Canvas __GetOrAddWindowCanvas()
        {
            if (m_WindowCanvas != null)
            {
                return m_WindowCanvas;
            }
            if (__IsObjLoaded())
            {
                m_WindowCanvas = m_WinObj.GetComponent(typeof(Canvas)) as Canvas;
                if (m_WindowCanvas == null)
                {
                    m_WindowCanvas = m_WinObj.AddComponent(typeof(Canvas)) as Canvas;
                    m_WindowCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
                }
                return m_WindowCanvas;
            }
            else
            {
                return null;
            }
        }
        //设置窗口不可被点穿
        public void __AddWindowBlock()
        {
            if (m_Blocker == null)
            {
                if (__IsObjLoaded())
                {
                    m_Blocker = m_WinObj.AddComponent(typeof(GraphicRaycaster)) as GraphicRaycaster;
                }
            }
        }
        public bool __IsVisiable()
        {
            return __bVisiable;
        }
        public UIWindow GetUIWindow()
        {
            return m_UIWindow;
        }

        // 动态界面可在 OnOpen 中按当前业务状态重建遥控菜单，不必依赖预制件中的固定配置。
        protected void __SetActiveButtons(IList<ActiveButtons> activeButtons, int defaultRowIndex = 0, int defaultColumnIndex = 0)
        {
            if (m_UIWindow == null)
            {
                return;
            }

            if (m_UIWindow.m_ActiveButtons == null)
            {
                m_UIWindow.m_ActiveButtons = new List<ActiveButtons>();
            }
            else
            {
                m_UIWindow.m_ActiveButtons.Clear();
            }

            if (activeButtons != null)
            {
                for (int rowIndex = 0; rowIndex < activeButtons.Count; rowIndex++)
                {
                    var sourceRow = activeButtons[rowIndex];
                    if (sourceRow == null || sourceRow.buttons == null || sourceRow.buttons.Count == 0)
                    {
                        continue;
                    }

                    var targetRow = new ActiveButtons();
                    for (int columnIndex = 0; columnIndex < sourceRow.buttons.Count; columnIndex++)
                    {
                        var button = sourceRow.buttons[columnIndex];
                        if (button != null)
                        {
                            targetRow.buttons.Add(button);
                        }
                    }

                    if (targetRow.buttons.Count > 0)
                    {
                        m_UIWindow.m_ActiveButtons.Add(targetRow);
                    }
                }
            }

            m_UIWindow.m_DefRowIndex = Mathf.Max(0, defaultRowIndex);
            m_UIWindow.m_DefColIndex = Mathf.Max(0, defaultColumnIndex);
            UIManager.OnWindowActiveButtonsChanged(this);
        }
        #endregion

        #region 虚生命周期钩子
        protected abstract void OnInitComponent();
        protected virtual void OnOpenAnimation(Action onFinishCall)
        {
            //处理是否需要播放动画
            //播放什么动画
            //(动画内已做)播放动画的时候打开界面的动画蒙版，防止期间误操作
            //结束播放动画
            if (m_UIWindow == null || m_UIWindow.m_Animation == null)
            {
                //不需要播放动画
                if (onFinishCall != null)
                {
                    onFinishCall();
                }
                return;
            }
            var ctrl = m_UIWindow.m_Animation;
            ctrl.PlayOpen(onFinishCall);
        }

        protected virtual void OnOpen()
        {
        }

        // 单例窗口重复打开时的可选刷新入口。
        // 默认不做任何事，避免已有 UI 被二次打开时重复注册事件或重跑完整打开流程。
        protected virtual void OnRepeatOpen()
        {
        }

        protected virtual void OnClose()
        {

        }
        protected virtual void OnEventRegister()
        {
        }

        protected virtual void OnEventUnRegister()
        {

        }
        protected virtual void OnChildClosed(WindowBase child)
        {

        }
        protected virtual void OnCloseAnimation(bool jumpAnimation, Action onFinishCall)
        {
            //处理是否需要播放动画
            //播放什么动画
            //(动画内已做)播放动画的时候打开界面的动画蒙版，防止期间误操作
            //结束播放动画
            if (m_UIWindow == null || m_UIWindow.m_Animation == null || jumpAnimation)
            {
                //不需要播放动画
                if (onFinishCall != null)
                {
                    onFinishCall();
                }
                if(m_UIWindow != null && m_UIWindow.m_Animation != null)
                {
                    m_UIWindow.m_Animation.JumpClose();
                }
                return;
            }
            m_UIWindow.m_Animation.PlayClose(onFinishCall);
        }
        protected virtual void OnDestroy()
        {
        }
        public void __Destroy()
        {
            OnDestroy();
            m_bInitComponent = false;
            WindowDestroyClearABGameObject();
            WindowDestroyClearABPrefabs();
            GameObject.Destroy(m_WinObj);
            m_WinObj = null;
            WindowDestroyClearImages();
            UIRes.UnloadPrefab(m_ABId);
            m_ABId = null;
            m_bDestroy = true;
            m_IsTempHide = -1;
            __SetWindowStage(WindowStage.Destroyed);
            //System.GC.Collect();
            //Resources.UnloadUnusedAssets();
        }
        #endregion

        #region 视图辅助
        protected Transform GetTransform(string path)
        {
            return m_WinTransform.Find(path);
        }

        public T GetControl<T>(string name = "", Component parent = null) where T : Component
        {
            if (parent == null)
            {
                parent = m_WinObj.transform;
            }
            Type type = typeof(T);
            if (name == "")
            {
                return parent.GetComponent(type) as T;
            }
            else
            {
                Transform tr = parent.transform.Find(name);
                if (tr == null)
                {
                    Tool.StringBuilder.Append(name);
                    Tool.StringBuilder.Append("not find");
                    UDebug.LogError(Tool.StringBuilder.ToString());
                    Tool.StringBuilder.Clear();
                    return null;
                }
                return tr.GetComponent(type) as T;
            }
        }
        #endregion


        //整合一个小红点的
        #region 红点辅助
        //建议在OnOpen添加
        protected void __RegisterRedPoint(string path, GameObject redPointGameObject)
        {
            //检测gameObject是否在本窗口注册
            foreach (var kv in m_RedPointList)
            {
                if (GameObject.Equals(kv.Value, redPointGameObject))
                {
                    //同一个gameobejct被重复注册，需要进行冗余处理
                    if (kv.Key == path)
                    {
                        //说明完全是重复注册
                        redPointGameObject.SetActive(RedPointManager.GetInstance().GetRedPointValue(path));
                        return;
                    }
                    else
                    {
                        //gameobject需要注册到其他路径
                        m_RedPointList.Remove(kv.Key);
                        m_RedPointList.Add(path, redPointGameObject);
                        redPointGameObject.SetActive(RedPointManager.GetInstance().GetRedPointValue(path));
                        return;
                    }
                }
            }

            if (m_RedPointList.ContainsKey(path))
            {
                m_RedPointList[path].SetActive(false);
                m_RedPointList[path] = redPointGameObject;
            }
            else
            {
                m_RedPointList.Add(path, redPointGameObject);
            }
            redPointGameObject.SetActive(RedPointManager.GetInstance().GetRedPointValue(path));
        }
        protected void __UnRegisterRedPoint(string path)
        {
            if (m_RedPointList.ContainsKey(path))
            {
                m_RedPointList.Remove(path);
            }
        }
        protected void __ClearAllRedPoint()
        {
            m_RedPointList.Clear();
        }
        protected void OnRedDotRefreshEvent(string path, bool value)
        {
            if (m_RedPointList.ContainsKey(path))
            {
                m_RedPointList[path].SetActive(value);
            }
        }
        #endregion

        #region 父子关系
        public WindowBase __GetParentWindow()
        {
            return m_ParentWindow;
        }
        public WindowBase __GetRootParentWindow()
        {
            int count = 50;
            WindowBase parent = m_ParentWindow;
            WindowBase lastParent = m_ParentWindow;
            if (lastParent == null)
            {
                return null;
            }
            for (int i = 0; i < count; ++i)
            {
                if (parent == null)
                {
                    return lastParent;
                }
                else
                {
                    lastParent = parent;
                    parent = parent.__GetParentWindow();
                }
            }
            UDebug.LogError("窗口父子关系超过上限" + count.ToString());
            return parent;
        }
        public void __AddChild(WindowBase win)
        {
            if (win != null)
            {
                int count = m_ChildWindows.Count;
                for (int i = 0; i < count; ++i)
                {
                    if (m_ChildWindows[i] == win)
                    {
                        return;
                    }
                }
                if (win.m_ParentWindow != null)
                {
                    win.m_ParentWindow.m_ChildWindows.Remove(win);
                }
                win.m_ParentWindow = this;
                m_ChildWindows.Add(win);
            }
        }
        public void __RemoveChild(WindowBase win)
        {
            if (win != null && win.m_ParentWindow == this)
            {
                win.m_ParentWindow = null;
                OnChildClosed(win);
                m_ChildWindows.Remove(win);
            }
        }
        //专门用作资源加载失败的清理
        public void __RemoveChildForUIPrefabError(WindowBase win)
        {
            if (win != null && win.m_ParentWindow == this)
            {
                win.m_ParentWindow = null;
                m_ChildWindows.Remove(win);
            }
        }
        public bool __IsChildOf(WindowBase parent)
        {
            return m_ParentWindow == parent;
        }
        public bool __IsGrandsonOf(WindowBase parent)
        {
            WindowBase wnd = m_ParentWindow;
            while (wnd != null)
            {
                bool child = wnd.__IsChildOf(parent);
                if (child)
                {
                    return true;
                }
                else
                {
                    wnd = wnd.m_ParentWindow;
                }
            }
            return false;
        }
        #endregion

        #region 打开与复开流程
        public void __MarkPendingReopen()
        {
            bool fromClosing = m_Stage == WindowStage.Closing;
            // ReopenPending 表示旧实例已被二次打开命中，等待下一帧真正复开。
            __SetWindowStage(WindowStage.ReopenPending);

            // 来自 Closing 的复开要保留当前动画画面，下一帧从当前关闭进度反向播开。
            // 只有原本已经不可见的缓存态，才继续保持隐藏。
            if (!fromClosing && __bVisiable)
            {
                Vector3 far = new Vector3(10000, 10000, 10000);
                m_WinTransform.position = far;
                __bVisiable = false;
            }
            // 在重新打开时，如果之前触发过打开事件，需要触发关闭事件
            // 这样可以让 move_joystic_wnd 等监听者正确更新计数
            if (m_HasOpenedEvent)
            {
                CGameProcedure.Event.OnUIClosedEvent(this);
                m_HasOpenedEvent = false;
            }
            if (m_DestroyTimer != -1)
            {
                RenderAPI.RemoveCounter(m_DestroyTimer);
                m_DestroyTimer = -1;
            }
        }
        public void __RemoveChildForPendingReopenCancel(WindowBase win)
        {
            if (win != null && win.m_ParentWindow == this)
            {
                // ReopenPending 只是提前预约父子关系；撤销复开时直接静默回滚，不补发 OnChildClosed。
                win.m_ParentWindow = null;
                m_ChildWindows.Remove(win);
            }
        }
        private void __StartDestroyTimerOrDestroy()
        {
            if (m_DestroyTimer != -1)
            {
                return;
            }
            __SetWindowStage(WindowStage.Cached);
            if (m_WindowCacheTime > 0)
            {
                if (m_DestroyTimeCall == null)
                {
                    m_DestroyTimeCall = __DestroyTimeCall;
                }
#pragma warning disable ANALYZER0006 // 窗口计时器规则分析器
                m_DestroyTimer = CounterManager.GetInstance().AddCounter(m_WindowCacheTime, 1, m_DestroyTimeCall);
#pragma warning restore ANALYZER0006 // 窗口计时器规则分析器
            }
            else
            {
                __DestroyTimeCall();
            }
        }
        public void __CancelPendingReopen()
        {
            // 只有处于 ReopenPending，才需要撤销这次复开预约并回到缓存/销毁流程。
            if (m_Stage != WindowStage.ReopenPending)
            {
                return;
            }

            if (m_ParentWindow != null)
            {
                m_ParentWindow.__RemoveChildForPendingReopenCancel(this);
            }
            if (__bVisiable)
            {
                // 来自 Closing 的待复开如果被撤销，直接补到关闭完成表现，避免窗口停在半关状态。
                if (m_UIWindow != null && m_UIWindow.m_Animation != null)
                {
                    m_UIWindow.m_Animation.JumpClose();
                }
                Vector3 far = new Vector3(10000, 10000, 10000);
                m_WinTransform.position = far;
                __bVisiable = false;
            }
            m_bLogicOpen = false;
            __StartDestroyTimerOrDestroy();
            UIManager.RefreshRemoteMenuBinding();
        }
        //该接口只能由WindowManager调用
        public void __ShowWindowObj()
        {
            m_bDestroy = false;
            // 无论是首次打开还是复开，真正进入表现层打开流程后都统一切到 Opening。
            __SetWindowStage(WindowStage.Opening);

            if (m_DestroyTimer != -1)
            {
                RenderAPI.RemoveCounter(m_DestroyTimer);
                m_DestroyTimer = -1;
            }

            if (m_WinObj == null)
            {
                return;
            }
            if (!m_bInitComponent)
            {
                m_UIWindow = GetControl<UIWindow>();
                if (m_UIWindow.m_CacheTime >= 0)
                {
                    float time = m_UIWindow.m_CacheTime * 1000;
                    __SetWindowCacheTime((int)time);
                }
                __OnInitUIWindow();
                OnInitComponent();

                m_bInitComponent = true;
            }

            __bVisiable = true;
            m_WinTransform.position = m_WindowPos;
            if(m_IsTempHide != -1)
            {
                m_WinObj.SetActive(m_IsTempHide == 0);
                m_IsTempHide = -1;
            }
        }
        //提供给WindowManager调用的接口
        public void __BindWindowData()
        {
            __OnShowBlur();
            __BindGuideEvent();

            //UDebug.Log("open ui:" + this.m_WindowName);
            //OnOpenAudio();
            OnEventRegister();
            CGameProcedure.Event.OnRedPointValueChange += OnRedDotRefreshEvent;
            OnOpen();
            // 检查 UI 是否仍然处于打开状态
            // 防止在 OnOpen() 中关闭所有 Popup（包括当前 UI）导致的事件顺序错误
            if (m_bLogicOpen && m_Stage == WindowStage.Opening)
            {
                UIManager.OnWindowActivated(this);
                // 只有在 UI 仍然打开时，才设置标志并触发打开事件
                m_HasOpenedEvent = true;
                CGameProcedure.Event.OnUIOpenedEvent(this);
                
                // 在触发打开事件后，再次检查 UI 状态，防止事件处理中关闭了 UI
                if (m_bLogicOpen && m_Stage == WindowStage.Opening)
                {
                    OnOpenAnimation(__OnAfterOpenAnimation);
                }
            }
        }

        private void __OnAfterOpenAnimation()
        {
            if (m_bLogicOpen && m_Stage == WindowStage.Opening)
            {
                __SetWindowStage(WindowStage.Opened);
            }

            OnAfterOpenAnimation();
        }

        protected virtual void OnAfterOpenAnimation()
        {

        }

        protected virtual void OnCheckGuideGroup()
        {

        }
        protected virtual void OnGuideStepFinish(guide_group group, guide_step_id step_id)
        {

        }
        private void __BindGuideEvent()
        {
            CGameProcedure.Event.OnCheckGuideGroup += OnCheckGuideGroup;
            CGameProcedure.Event.OnGuideStepFinish += OnGuideStepFinish;
        }
        private void __UnbindGuideEvent()
        {
            CGameProcedure.Event.OnCheckGuideGroup -= OnCheckGuideGroup;
            CGameProcedure.Event.OnGuideStepFinish -= OnGuideStepFinish;
        }


        //提供给WindowManager使用
        public void __SetLogicOpen(bool open)
        {
            m_bLogicOpen = open;
        }
        //提供给WindowManager使用
        public bool __IsLogicOpen()
        {
            return m_bLogicOpen;
        }
        public bool IsLogicClosed()
        {
            return !m_bLogicOpen;
        }
        #endregion

        #region 关闭流程
        public void __CloseWindowWithChild(bool jumpAnimation)
        {
            //先要断开和父对象的关系
            if (m_ParentWindow != null)
            {
                m_ParentWindow.__RemoveChild(this);
            }
            if (m_ChildWindows.Count > 0)
            {
                int count = m_ChildWindows.Count;
                m_tempChildWnds.Clear();
                for (int i = 0; i < count; ++i)
                {
                    m_tempChildWnds.Add(m_ChildWindows[i]);
                }
                for (int i = 0; i < count; ++i)
                {
                    WindowBase win = m_tempChildWnds[i];
                    UIManager.CloseWindow(win, jumpAnimation);
                }
            }
            if (__IsObjLoaded())
            {
                __CloseWindowObj(jumpAnimation);
            }
            else
            {
                //压根没有加载进来就直接关闭吧
                UIManager.WindowDestroy(this);
            }
        }
        protected void __CloseAllChildren(bool jumpAnimation = false)
        {
            if (m_ChildWindows.Count > 0)
            {
                int count = m_ChildWindows.Count;
                m_tempChildWnds.Clear();
                for (int i = 0; i < count; ++i)
                {
                    m_tempChildWnds.Add(m_ChildWindows[i]);
                }
                for (int i = 0; i < count; ++i)
                {
                    WindowBase win = m_tempChildWnds[i];
                    UIManager.CloseWindow(win, jumpAnimation);
                }
            }
        }
        private void __CloseWindowObj(bool jumpAnimation)
        {
            if (m_DestroyTimer != -1)
            {
                UDebug.LogError("上一次删除缓存还未结束");
                return;
            }
            __OnHideBlur();
            __UnbindGuideEvent();
            __ClearAllRedPoint();
            //OnCloseAudio();
            OnEventUnRegister();
            RemoveAllCounter();
            CGameProcedure.Event.OnRedPointValueChange -= OnRedDotRefreshEvent;
            if (m_UIWindow != null)
            {
                m_UIWindow.OnClose();
            }

            // 业务关闭逻辑先于关闭动画执行；因此 Closing/Cached 被复开时，需要重新走 OnOpen 链路恢复业务状态。
            OnClose();
            //这个必须放到这里，不能放到OnClose前面，因为OnClose里面可能会用到model数据
            __Model?.Clear();

            //记录本次关闭时候窗口的位置
            m_WindowPos = m_WinTransform.position;

            // Closing 是关闭流程仍然有效的唯一标记。
            __SetWindowStage(WindowStage.Closing);
            m_IsTempHide = -1;

            //这里有可能是异步(如果有动画的话)
            OnCloseAnimation(jumpAnimation,() =>
            {
                // 如果阶段已经离开 Closing，说明这次关闭已被复开流程打断。
                if (m_Stage != WindowStage.Closing)
                {
                    return;
                }
                //将窗口移动到很远地方
                if (__bVisiable)
                {
                    Vector3 far = new Vector3(10000, 10000, 10000);
                    m_WinTransform.position = far;
                    __bVisiable = false;
                }
                if (m_HasOpenedEvent)
                {
                    CGameProcedure.Event.OnUIClosedEvent(this);
                    m_HasOpenedEvent = false;
                }
                __StartDestroyTimerOrDestroy();
                // 菜单焦点必须等窗口真正离开可见态后再刷新，
                // 这样关闭动画期间仍然操作当前窗口，动画结束后才切到下一层。
                UIManager.RefreshRemoteMenuBinding();
            });

        }
        protected void OnCloseSelf()
        {
            OnCloseSelf(false);
        }
        protected void OnCloseSelf(bool jumpAnimation)
        {
            UIManager.CloseWindow(this, jumpAnimation);
        }
        #endregion

        #region 音频钩子
        protected virtual void OnOpenAudio()
        {
            if(m_UIWindow == null)
            {
                return;
            }
            if (m_UIWindow.WindowType == WindowType.FullScreen || m_UIWindow.WindowType == WindowType.Pop)
            {
                AudioManager.GetInstance().Play2D(5);
            }
        }
        protected virtual void OnCloseAudio()
        {
            if (m_UIWindow == null)
            {
                return;
            }
            if (m_UIWindow.WindowType == WindowType.FullScreen || m_UIWindow.WindowType == WindowType.Pop)
            {
                AudioManager.GetInstance().Play2D(6);
            }
        }
        #endregion



        #region 窗口基类帮助函数
        public void __SetImage(Graphic img, string texture, bool useBlank = false, Action onLoadCall = null)
        {
            if (img == null || img.Equals(null) || texture == null || string.IsNullOrEmpty(texture))
            {
                return;
            }
            __SetImage(img, texture, System.IO.Path.GetFileNameWithoutExtension(texture), useBlank, onLoadCall);
        }
        public void __SetImage(Graphic img, string atlas, string icon, bool useBlank = true, Action onLoadCall = null)
        {
            if(img == null || img.Equals(null) || atlas == null || string.IsNullOrEmpty(atlas))
            {
                return;
            }
            if(icon == null || string.IsNullOrEmpty(icon))
            {
                icon = System.IO.Path.GetFileNameWithoutExtension(atlas);
            }
            SetImageSpriteParam param = new SetImageSpriteParam();
            param.abName = atlas;
            param.assetName = icon;
            param.img = img;
            param.call = OnSetImageCallback;
            if(onLoadCall != null)
            {
                param.call += (SetImageSpriteParam _param, object _sprite_or_texture) => 
                {
                    onLoadCall();
                };
            }
            if (!useBlank)
            {
                TextureManager.SetImageDefaultIfNo(img);
            }
            long id = 0;
            if (m_ImageIds.ContainsKey(img))
            {
                var last_id = m_ImageIds[img];
                id = TextureManager.SetImageSprite(param, useBlank);
                m_ImageIds[img] = id;

                //作用是：防止相同控件连续加载图片闪烁白板
                //1、如果旧的图片已经显示了，在新的还没有加载进来之前不做任何变动，只有新的进来了，才会看到图片改变；
                //2、如果旧的还没有加载进来，就告诉它我们不需要它了，就算它后来加载进来了，也不再是使用了。
                //该函数不会对AB做任何变动
                TextureManager.NotSetTexture(last_id);
                //UIUIRes.ResouceIdReturn(this, last_id, TextureManager.ReturnImageSprite);
            }
            else
            {
                id = TextureManager.SetImageSprite(param, useBlank);
                m_ImageIds.Add(img, id);
            }
            UIRes.ResouceIdCollect(this, id);
        }
        /// <summary>
        /// 设置sd卡等外部原始图片
        /// </summary>
        /// <param name="img"></param>
        /// <param name="fullPath"></param>
        /// <param name="rawImage"></param>
        public void __SetImageRaw(Graphic img, string fullPath, bool rawImage = true)
        {
            if (img == null || img.Equals(null) || fullPath == null || string.IsNullOrEmpty(fullPath))
            {
                return;
            }
            if (rawImage)
            {
                var t = TextureManager.LoadTexture(fullPath);
                RawImage ri = img as RawImage;
                ri.texture = t;
            }
            else
            {
                var t = TextureManager.LoadSprite(fullPath);
                Image ri = img as Image;
                ri.sprite = t;
            }
        }
        protected virtual void OnSetImageCallback(SetImageSpriteParam param, object sprite_or_texture)
        {
            //UDebug.Log("使用图集改变按钮图片成功");
        }
        protected void ClearImage(Image img)
        {
            if (m_ImageIds.ContainsKey(img))
            {
                long id = m_ImageIds[img];
                UIRes.ResouceIdReturn(this, id, TextureManager.ReturnImageSprite);
                m_ImageIds.Remove(img);
            }
        }
        private void WindowDestroyClearImages()
        {
            UIRes.ResouceIdReturn(this, TextureManager.ReturnImageSprite);
            m_ImageIds.Clear();
        }
        //加载资源帮助函数
        /// <summary>
        /// 该接口用于简单加载资源，不用关心ab释放细节，拿到GameObject后是经过实例化的，一般不用关心GameObject的释放问题。
        /// 但是如果是在无限循环列表里面使用，可能会由于加载过多，没有释放，导致内存和性能问题。
        /// 当前界面Destroy的时候会释放。Close的时候不会释放。
        /// 新增，返回ABRequest，可以在具体逻辑里面对AB和GameObject进行释放，释放方法是UnloadGameObject
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="loadFinishCall"></param>
        /// <param name="isCode">true 程序工程的资源， false 美术工程资源</param>
        protected ABRequest LoadGameObject(string abName, Action<GameObject> loadFinishCall, Transform parent = null, bool isCode = true)
        {
            ABRequest ab = null;
            if (isCode)
            {
                ab = LCL.UIRes.LoadPrefabAsync(typeof(GameObject), abName, System.IO.Path.GetFileNameWithoutExtension(abName), (rd, ud) =>
                {
                    var prefab = rd.m_Obj as GameObject;
                    if (prefab != null)
                    {
                        var go = GameObject.Instantiate(prefab, parent) as GameObject;
                        if (m_ABGameObjectDic.ContainsKey(ab))
                        {
                            m_ABGameObjectDic[ab] = go;
                        }
                        if (loadFinishCall != null)
                        {
                            loadFinishCall(go);
                        }
                    }
                    else
                    {
                        if (loadFinishCall != null)
                        {
                            loadFinishCall(null);
                        }
                    }

                },
                (id, abr) =>
                {
                    if (id == ab.LoadIndex)
                    {
                        m_ABGameObjectDic.Remove(ab);
                        if (loadFinishCall != null)
                        {
                            loadFinishCall(null);
                        }
                    }
                }, null);
                if (ab != null)
                {
                    if (!m_ABGameObjectDic.ContainsKey(ab))
                    {
                        m_ABGameObjectDic.Add(ab, null);
                    }
                }
            }
            else
            {
                ab = LCL.UIRes.LoadPrefabAsync(typeof(GameObject), abName, System.IO.Path.GetFileNameWithoutExtension(abName), (rd, ud) =>
                {
                    var prefab = rd.m_Obj as GameObject;
                    if (prefab != null)
                    {
                        var go = GameObject.Instantiate(prefab, parent) as GameObject;
                        if (m_ABGameObjectDic.ContainsKey(ab))
                        {
                            m_ABGameObjectDic[ab] = go;
                        }
                        if (loadFinishCall != null)
                        {
                            loadFinishCall(go);
                        }
                    }
                    else
                    {
                        if (loadFinishCall != null)
                        {
                            loadFinishCall(null);
                        }
                    }

                },
                (id, abr) =>
                {
                    if (id == ab.LoadIndex)
                    {
                        m_ABGameObjectDic.Remove(ab);
                        if (loadFinishCall != null)
                        {
                            loadFinishCall(null);
                        }
                    }
                });
                if (ab != null)
                {
                    if (!m_ABGameObjectDic.ContainsKey(ab))
                    {
                        m_ABGameObjectDic.Add(ab, null);
                    }
                }
            }
            return ab;
        }
        /// <summary>
        /// 快捷释放ab和GameObject，逻辑那边调用后最好是对相应变量置空。
        /// </summary>
        /// <param name="ab"></param>
        /// <param name="go"></param>
        protected void UnloadGameObject(ABRequest ab, GameObject go)
        {
            if (go != null && !go.Equals(null))
            {
                GameObject.Destroy(go);
            }
            if (ab != null)
            {
                LCL.UIRes.UnloadPrefab(ab);
            }
            
        }
        private void WindowDestroyClearABGameObject()
        {
            foreach(var kv in m_ABGameObjectDic)
            {
                var ab = kv.Key;
                var go = kv.Value;
                if(go != null && !go.Equals(null))
                {
                    GameObject.Destroy(go);
                }
                LCL.UIRes.UnloadPrefab(ab);
            }
            m_ABGameObjectDic.Clear();
        }

        //封装一个资源加载和释放
        protected ABRequest LoadPrefabAsync(Type t, string abName, string mainAssetName, Action<ResData, object> func,
            Action<long, ABRequestResult> errorFunc = null, object userData = null)
        {
            ABRequest result = null;

            result = LCL.UIRes.LoadPrefabAsync(t, abName, mainAssetName, func, errorFunc, userData);
            m_ABRequestCodeDic.Add(result.LoadIndex, result);

            return result;
        }
        protected void UnloadPrefab(ABRequest ab)
        {
            if (ab == null)
            {
                return;
            }

            if (m_ABRequestCodeDic.ContainsKey(ab.LoadIndex))
            {
                LCL.UIRes.UnloadPrefab(ab);
                m_ABRequestCodeDic.Remove(ab.LoadIndex);
            }
            else
            {
                Debug.LogError($"资源释放存在问题，ABRequest没有在m_ABRequestCodeDic找到, abName:{ab.abName}");
            }

        }
        private void WindowDestroyClearABPrefabs()
        {
            foreach (var kv in m_ABRequestDic)
            {
                var ab = kv.Value;
                LCL.UIRes.UnloadPrefab(ab);
            }
            m_ABRequestDic.Clear();

            foreach (var kv in m_ABRequestCodeDic)
            {
                var ab = kv.Value;
                LCL.UIRes.UnloadPrefab(ab);
            }
            m_ABRequestCodeDic.Clear();
        }



        /// <summary>
        /// 快捷添加计时器，使用的时候，需要自己处理关闭。
        /// </summary>
        /// <param name="intervalMMSec"></param>
        /// <param name="count"></param>
        /// <param name="perCall"></param>
        /// <param name="delayMMSec"></param>
        /// <param name="finishCall"></param>
        /// <returns></returns>
        protected long AddCounter(int intervalMMSec, int count, System.Action perCall, int delayMMSec = 0, System.Action finishCall = null)
        {
            var timerId = CounterManager.GetInstance().AddCounter(intervalMMSec, count, perCall, delayMMSec, finishCall);
            m_TimerIds.Add(timerId);
            return timerId;
        }
        protected void RemoveCounter(long id)
        {
            m_TimerIds.Remove(id);
            CounterManager.GetInstance().RemoveCounter(id);
        }
        protected void RemoveAllCounter()
        {
            foreach (var id in m_TimerIds)
            {
                CounterManager.GetInstance().RemoveCounter(id);
            }
            m_TimerIds.Clear();
        }
        #endregion


        #region 窗口移动、前面显示
        private void __OnInitUIWindow()
        {
            if(__OpenFocus)
            {
                m_UIWindow.OnFocusCall = OnFocusWindow;
            }
        }

        protected virtual void OnFocusWindow()
        {
            UIManager.OnFocusWindow(this);
        }


        private void __OnShowBlur()
        {
            if(__IsObjLoaded() && m_Layer == WindowLayer.Popup)
            {
                var dimed = GetTransform("common_screen_dimmed");
                if (dimed == null)
                {
                    return;
                }
                dimed.gameObject.SetActive(true);
            }
        }
        private void __OnHideBlur()
        {
            if (__IsObjLoaded() && m_Layer == WindowLayer.Popup)
            {
                var dimed = GetTransform("common_screen_dimmed");
                if(dimed == null)
                {
                    return;
                }
                dimed.gameObject.SetActive(false);
            }
        }

        public void __SetImageNativeSize(Graphic img, float max_x = 0, float max_y = 0)
        {
            img.SetNativeSize();
            if(max_x == 0 && max_y == 0)
            {
                return;
            }
            var size = img.rectTransform.sizeDelta;
            var factorImg = size.x / size.y;
            var factorParent = max_x / max_y;

            var new_size = new Vector2();
            if (factorImg >= factorParent)
            {
                //图片的宽 比容器的宽 宽一些， 那么就要以容器的宽作为宽
                new_size.x = max_x;
                new_size.y = new_size.x / factorImg;
            }
            else
            {
                new_size.y = max_y;
                new_size.x = new_size.y * factorImg;
            }
            img.rectTransform.sizeDelta = new_size;
        }

        public void SetTempHide(bool hide)
        {
            m_IsTempHide = hide ? 1 : 0;
            if (__IsObjLoaded())
            {
                m_WinObj.SetActive(!hide);
                m_IsTempHide = -1;
                UIManager.RefreshRemoteMenuBinding();
            }
        }
        #endregion

    }
}
