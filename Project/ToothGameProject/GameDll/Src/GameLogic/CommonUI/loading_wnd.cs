using LCL;
using System;
using UnityEngine;
using GameDll;
using MonoBean;

namespace GameHot
{
    public class loading_wnd : WindowBase
    {
        private const string m_LanTdGameTitle = "td_game_title";
        private const float m_AnimationTimeoutSeconds = 5f;
        private const float m_PrefabSlowWarningSeconds = 10f;

        private enum LoadingState
        {
            WaitingForPrefab,
            Covering,
            Covered,
            Uncovering,
            PrefabFailed
        }

        private static Action m_AfterCoveredCall;
        private static loading_wnd m_Instance = null;
        private static float m_Percent = 0.0f;
        private static string m_Info = "";
        private static float m_MinVisibleUntilTime = 0.0f;
        private static int m_LoadingSessionId = 0;
        private static float m_CloseAtGameTime = float.PositiveInfinity;
        //这个是给热更新这边用
        public static void SetLoadingInfo(float percent, string info)
        {
            m_Percent = Mathf.Clamp01(percent);
            m_Info = info;
            if (m_Instance != null)
            {
                m_Instance.ShowInfo();
            }
        }
        public static void AddLoadingProgress(float pro, string info)
        {
            m_Percent += pro;
            m_Percent = Mathf.Clamp01(m_Percent);
            loading_wnd.SetLoadingInfo(m_Percent, info);
        }
        public static int GetRemainingVisibleMilliseconds()
        {
            if (m_Instance == null)
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.CeilToInt((m_MinVisibleUntilTime - Time.realtimeSinceStartup) * 1000.0f));
        }
        public static void OpenLoading(Action afterCoveredCall = null)
        {
            OpenLoadingInternal(0.0f, afterCoveredCall);
        }
        public static void OpenLoading(float minVisibleSeconds, Action afterCoveredCall = null)
        {
            OpenLoadingInternal(minVisibleSeconds, afterCoveredCall);
        }
        private static void OpenLoadingInternal(float minVisibleSeconds, Action afterCoveredCall)
        {
            if (DataManager.IsInitialized)
            {
                AudioManager.GetInstance().Play2D(2);
            }

            //UDebug.Log("-------------->>>>>:OpenLoading");
            //这个是给主工程那边用
            RenderEvent.Event.OnLoadingProChanged = OnLoadingProChanged;
            m_AfterCoveredCall = afterCoveredCall;
            m_CloseAtGameTime = float.PositiveInfinity;
            ++m_LoadingSessionId;
            minVisibleSeconds = Mathf.Max(0.0f, minVisibleSeconds);
            m_MinVisibleUntilTime = Time.realtimeSinceStartup + minVisibleSeconds;
            if (m_Instance == null || m_Instance.__GetWindowStage() == WindowStage.Cached)
            {
                m_Instance = UIManager.OpenWindowEX<loading_wnd>(null);
                m_Instance.m_PrefabWarningAt = GameProductUiAdapter.ForegroundRealtime + m_PrefabSlowWarningSeconds;
            }
            else
            {
                m_Instance.ShowInfo();
                if (m_Instance.m_State == LoadingState.Uncovering)
                {
                    m_Instance.RestoreCover();
                }
                if (m_Instance.m_State == LoadingState.Covered)
                {
                    m_Instance.AfterCoveredCall();
                }
            }
        }

        private static void OnLoadingProChanged(float arg1, string arg2)
        {
            m_Percent = arg1;
            m_Info = arg2;
            if(m_Instance != null)
            {
                m_Instance.ShowInfo();
            }
        }

        public static loading_wnd GetLoadingWnd()
        {
            return m_Instance;
        }
        public static bool HasWnd()
        {
            return m_Instance != null;
        }
        public static bool IsCovering()
        {
            return m_Instance != null && !IsCovered();
        }
        public static bool IsCovered()
        {
            return m_Instance != null &&
                (m_Instance.m_State == LoadingState.Covered || m_Instance.m_State == LoadingState.Uncovering);
        }
        public static bool IsBlockingFlowInput()
        {
            // loading 存在时流程正在切换，键盘和遥控器不能再次触发页面动作。
            return m_Instance != null;
        }
        public static void DelayClose()
        {
            if (m_Instance != null)
            {
                m_CloseAtGameTime = Mathf.Min(m_CloseAtGameTime, Time.time + 1f);
            }
        }
        public static void CloseLoading()
        {
            if (m_Instance != null)
            {
                m_CloseAtGameTime = float.NegativeInfinity;
            }
        }

        internal static void Tick()
        {
            m_Instance?.AdvanceState();
        }

        internal static void ShowSceneLoadRetry(string message, Action retry)
        {
            ++m_LoadingSessionId;
            m_AfterCoveredCall = null;
            CloseLoadingWindowNow(true);
            dialog_wnd.ShowRetryDialog(message, retry);
        }

        private static void CloseLoadingWindowNow(bool jumpAnimation = false)
        {
            if (m_Instance == null)
            {
                return;
            }

            var wnd = m_Instance;
            ResetLoadingState();
            UIManager.CloseWindow(wnd, jumpAnimation);
        }
        private static void ResetLoadingState()
        {
            m_Instance = null;
            m_Info = "";
            m_Percent = 0;
            m_MinVisibleUntilTime = 0.0f;
            m_CloseAtGameTime = float.PositiveInfinity;
            m_AfterCoveredCall = null;
        }

        private v_loading_wnd m_View = null;
        private LoadingState m_State;
        private Animator m_LoadingAnimator = null;
        private float m_PrefabWarningAt;
        private float m_AnimationDeadline;
        private int m_AnimationStartedFrame;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Loading;
            __CustomUIPrefabDir = UIPrefabDirs.common;
        }
        protected override void OnInitComponent()
        {
            m_View = new v_loading_wnd();
            m_View.InitComponent(__GetWindowObj());
        }

        private void RestoreCover()
        {
            BeginAnimation(LoadingState.Covering);
            if (m_LoadingAnimator != null)
            {
                m_LoadingAnimator.SetInteger("Step", 1);
                m_LoadingAnimator.Play("jiazai", 0, 0);
            }
        }

        private void AfterCoveredCall()
        {
            var callback = m_AfterCoveredCall;
            m_AfterCoveredCall = null;
            callback?.Invoke();
        }

        protected override void OnOpen()
        {
            m_LoadingAnimator = m_View.m_Loading != null ? m_View.m_Loading.GetComponent<Animator>() : null;
            if (m_LoadingAnimator != null)
            {
                m_LoadingAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                m_LoadingAnimator.SetInteger("Step", 1);
                m_LoadingAnimator.Play("normal", 0, 0);
            }
            BeginAnimation(LoadingState.Covering);
            ShowInfo();
        }

        private void AdvanceState()
        {
            switch (m_State)
            {
                case LoadingState.WaitingForPrefab:
                    CheckPrefabLoad();
                    break;
                case LoadingState.Covering:
                    if (CheckAnimation("jiazai", "遮挡动画未就绪，继续切换流程。", out _))
                    {
                        m_State = LoadingState.Covered;
                        AfterCoveredCall();
                    }
                    break;
                case LoadingState.Covered:
                    if (Time.time >= m_CloseAtGameTime && Time.realtimeSinceStartup >= m_MinVisibleUntilTime)
                    {
                        BeginAnimation(LoadingState.Uncovering);
                        if (m_LoadingAnimator != null)
                        {
                            m_LoadingAnimator.SetInteger("Step", 2);
                        }
                    }
                    break;
                case LoadingState.Uncovering:
                    if (CheckAnimation("normal", "退出动画未完成，关闭加载界面。", out bool forceClose))
                    {
                        CloseLoadingWindowNow(forceClose);
                    }
                    break;
            }
        }

        private void CheckPrefabLoad()
        {
            var stage = __GetWindowStage();
            if (stage == WindowStage.LoadFailed)
            {
                m_State = LoadingState.PrefabFailed;
                m_AfterCoveredCall = null;
                Debug.LogError("[加载界面] 预制件加载失败。session=" + m_LoadingSessionId +
                               " resource=" + __GetABId()?.abName);
            }
            else if (stage == WindowStage.Cached)
            {
                ResetLoadingState();
            }
            else if (GameProductUiAdapter.ForegroundRealtime >= m_PrefabWarningAt)
            {
                m_PrefabWarningAt = float.PositiveInfinity;
                Debug.LogWarning("[加载界面] 预制件加载较慢，继续等待。session=" + m_LoadingSessionId +
                                 " resource=" + __GetABId()?.abName + " stage=" + stage);
            }
        }

        private void BeginAnimation(LoadingState state)
        {
            m_State = state;
            m_AnimationStartedFrame = Time.frameCount;
            m_AnimationDeadline = GameProductUiAdapter.ForegroundRealtime + m_AnimationTimeoutSeconds;
        }

        private bool CheckAnimation(string targetState, string warning, out bool animationIncomplete)
        {
            animationIncomplete = false;
            if (Time.frameCount == m_AnimationStartedFrame)
            {
                return false;
            }

            bool available = m_LoadingAnimator != null && m_LoadingAnimator.isActiveAndEnabled &&
                m_LoadingAnimator.runtimeAnimatorController != null && m_LoadingAnimator.layerCount > 0;
            if (available && m_LoadingAnimator.GetCurrentAnimatorStateInfo(0).IsName(targetState))
            {
                return true;
            }

            if (available && GameProductUiAdapter.ForegroundRealtime < m_AnimationDeadline)
            {
                return false;
            }

            animationIncomplete = true;
            Debug.LogWarning("[加载界面] " + warning + "session=" + m_LoadingSessionId + " animator=" + available);
            return true;
        }
        private void ShowInfo()
        {
            if(IsInitializedView())
            {
                //RenderAPI.SetText(m_Wnd.m_loadtip, RenderAPI.GetTextByLanId(m_LanTdGameTitle));
                //RenderAPI.SetText(m_Wnd.m_txtinfo, m_Info);
                //m_Wnd.m_loading_fill.fillAmount = Mathf.Clamp01(m_Percent);
                //RenderAPI.SetText(m_Wnd.m_shi, string.Format("{0}%", Mathf.RoundToInt(Mathf.Clamp01(m_Percent) * 100f)));

                //var main = Main.GetInstance();
               // RenderAPI.SetActive(m_Wnd.m_imgAuthor, main != null && Main.GetInstance().m_IsChina);
            }

        }

        private void ShowTips()
        {
            var loading_tips_count = t_globalBean.GetConfig(47).t_int;
            var idx = UnityEngine.Random.Range(0, loading_tips_count);
            string key = "loading_tip_id_" + idx;
            //RenderAPI.SetText(m_Wnd.m_tips, RenderAPI.GetTextByLanId(key));
            
        }

        protected override void OnClose()
        {
            m_State = LoadingState.WaitingForPrefab;
            m_LoadingAnimator = null;
            if (m_Instance == this)
            {
                ResetLoadingState();
            }
        }

        protected override void OnDestroy()
        {
            if (m_Instance == this)
            {
                ResetLoadingState();
            }
        }
    }
}
