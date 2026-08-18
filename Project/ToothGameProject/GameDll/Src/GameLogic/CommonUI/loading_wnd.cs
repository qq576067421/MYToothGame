using LCL;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using GameDll;
using MonoBean;
using System.Collections;

namespace GameHot
{
    public class loading_wnd : WindowBase
    {
        private const string m_LanTdGameTitle = "td_game_title";
        private static List<Action> m_AfterCoveredCalls = new List<Action>();
        private static List<Action> m_ClosedLoadingCalls = new List<Action>();
        private static loading_wnd m_Instance = null;
        private static float m_Percent = 0.0f;
        private static string m_Info = "";
        private static float m_MinVisibleUntilTime = 0.0f;
        private static int m_LoadingSessionId = 0;
        private static bool m_IsWaitingMinVisibleClose = false;
        private static bool m_CloseRequested = false;
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
        static System.Collections.IEnumerator AutoClose()
        {
            yield return new WaitForSeconds(1f);
            CloseLoading();
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
            AudioManager.GetInstance().Play2D(2);
            //UDebug.Log("-------------->>>>>:OpenLoading");
            //这个是给主工程那边用
            RenderEvent.Event.OnLoadingProChanged = OnLoadingProChanged;
            ClearAfterCoveredCalls();
            if (afterCoveredCall != null)
            {
                m_AfterCoveredCalls.Add(afterCoveredCall);
            }
            m_IsWaitingMinVisibleClose = false;
            m_CloseRequested = false;
            ++m_LoadingSessionId;
            minVisibleSeconds = Mathf.Max(0.0f, minVisibleSeconds);
            m_MinVisibleUntilTime = Time.realtimeSinceStartup + minVisibleSeconds;
            if (m_Instance == null)
            {
                m_Instance = UIManager.OpenWindowEX<loading_wnd>(null);
            }
            else
            {
                m_Instance.ShowInfo();
                m_Instance.ResetAfterCoveredCallState();
                if (m_Instance.m_IsCovered)
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
            return m_Instance != null && !m_Instance.m_IsCovered;
        }
        public static bool IsCovered()
        {
            return m_Instance != null && m_Instance.m_IsCovered;
        }
        public static bool IsBlockingFlowInput()
        {
            // loading 存在时流程正在切换，键盘和遥控器不能再次触发页面动作。
            return m_Instance != null;
        }
        public static void DelayClose()
        {
            Main.GetInstance().StartCoroutine(DelayCloseLoading());
        }
        static System.Collections.IEnumerator DelayCloseLoading()
        {
            yield return new WaitForSeconds(1f);
            CloseLoading();
        }
        static System.Collections.IEnumerator DelayCloseLoadingByMinVisible(int sessionId)
        {
            while (m_Instance != null && sessionId == m_LoadingSessionId)
            {
                if (Time.realtimeSinceStartup >= m_MinVisibleUntilTime)
                {
                    break;
                }

                yield return null;
            }

            if (sessionId != m_LoadingSessionId)
            {
                yield break;
            }

            m_IsWaitingMinVisibleClose = false;
            CloseLoading();
        }
        private static void ClearAfterCoveredCalls()
        {
            if (m_AfterCoveredCalls == null || m_AfterCoveredCalls.Count == 0)
            {
                return;
            }

            m_AfterCoveredCalls.Clear();
        }
        public static void CloseLoading()
        {
            if (m_Instance != null)
            {
                if (Time.realtimeSinceStartup < m_MinVisibleUntilTime)
                {
                    if (!m_IsWaitingMinVisibleClose)
                    {
                        m_IsWaitingMinVisibleClose = true;
                        Main.GetInstance().StartCoroutine(DelayCloseLoadingByMinVisible(m_LoadingSessionId));
                    }
                    return;
                }

                m_CloseRequested = true;
                m_Instance.TryPlayCloseAnimation();
            }
        }
        private static void CloseLoadingWindowNow()
        {
            if (m_Instance == null)
            {
                return;
            }

            var wnd = m_Instance;
            m_Instance = null;
            m_MinVisibleUntilTime = 0.0f;
            m_IsWaitingMinVisibleClose = false;
            m_CloseRequested = false;
            UIManager.CloseWindow(wnd);
        }
        private v_loading_wnd m_View = null;
        private Coroutine m_CheckAniCoroutine = null;
        private bool m_IsCovered = false;
        private bool m_HasCalledAfterCovered = false;
        private Animator m_LoadingAnimator = null;
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

        private void ResetAfterCoveredCallState()
        {
            m_HasCalledAfterCovered = false;
        }

        private void AfterCoveredCall()
        {
            if (m_HasCalledAfterCovered)
            {
                return;
            }

            m_HasCalledAfterCovered = true;
            if (m_AfterCoveredCalls != null && m_AfterCoveredCalls.Count > 0)
            {
                int count = m_AfterCoveredCalls.Count;
                for (int i = 0; i < count; ++i)
                {
                    var call = m_AfterCoveredCalls[i];
                    if (call != null)
                    {
                        m_AfterCoveredCalls[i] = null;
                        call();
                        call = null;
                    }
                }
                m_AfterCoveredCalls.Clear();
            }
        }

        protected override void OnOpen()
        {
            m_IsCovered = false;
            m_HasCalledAfterCovered = false;
            var loading = m_View.m_Loading.GetComponent<Animator>();
            m_LoadingAnimator = loading;
            loading.SetInteger("Step", 1);
            loading.Play("normal", 0, 0);
            m_CheckAniCoroutine = RenderAPI.StartCoroutine(CheckAnimationState(loading));
            ShowInfo();
        }
        private IEnumerator CheckAnimationState(Animator animator)
        {
            while (true)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.IsName("jiazai"))
                {
                    // 进入 jiazai 状态说明遮挡动画已经完成，此时再执行切场景等业务流程。
                    m_IsCovered = true;
                    AfterCoveredCall();
                    if (m_CloseRequested)
                    {
                        TryPlayCloseAnimation();
                    }
                }
                else if(stateInfo.IsName("normal") && animator.GetInteger("Step")==2 && m_CloseRequested)
                {
                    CloseLoadingWindowNow();
                    break;
                }

                yield return null; // 每帧检查
            }
            if (m_CheckAniCoroutine != null)
            {
                RenderAPI.StopCoroutine(m_CheckAniCoroutine);
                m_CheckAniCoroutine = null;  // 置空引用
            }
        }
        private void TryPlayCloseAnimation()
        {
            if (!m_IsCovered)
            {
                return;
            }

            if (m_LoadingAnimator == null)
            {
                CloseLoadingWindowNow();
                return;
            }

            // 关闭请求由业务流程发起，覆盖动画完成前只记录请求，避免提前露出底层界面。
            m_LoadingAnimator.SetInteger("Step", 2);
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
            m_Info = "";
            m_Percent = 0;
            m_MinVisibleUntilTime = 0.0f;
            m_IsWaitingMinVisibleClose = false;
            m_CloseRequested = false;
            m_IsCovered = false;
            m_HasCalledAfterCovered = false;
            m_LoadingAnimator = null;
            if (m_CheckAniCoroutine != null)
            {
                RenderAPI.StopCoroutine(m_CheckAniCoroutine);
                m_CheckAniCoroutine = null;  
            }
            ClearAfterCoveredCalls();
            m_Instance = null;
        }
        protected override void OnDestroy()
        {
            m_MinVisibleUntilTime = 0.0f;
            m_IsWaitingMinVisibleClose = false;
            m_CloseRequested = false;
            m_IsCovered = false;
            m_HasCalledAfterCovered = false;
            m_LoadingAnimator = null;
            ClearAfterCoveredCalls();
            m_Instance = null;
        }


    }
}
