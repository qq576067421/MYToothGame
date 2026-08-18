using System;
using GameDll;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MonoBean;
using LCL;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameHot
{

    public enum InputType
    {
        Up,
        Down,
        Left,
        Right,
        Enter,
        Escape
    }

    public enum EProcedureType
    {
        eStart,
        eLogin,
        eLobby,
        eEditHome,
        eScene,
    }
    public abstract class CGameProcedure
    {
        public EProcedureType m_ProType = EProcedureType.eStart;
        //渲染暂停
        protected static bool m_bRenderingPaused=false;
        //窗口最小化
        protected static bool m_bMinimized=false;
        //窗口处于焦点状态
        protected static bool m_bActive=true;



        //
        // 游戏运行的过程
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // 计时器



        //游戏事件管理器
        public static DispatcherHF Event;


        //public static CameraManager s_CameraManager = null;



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // 游戏运行的流程.
        //
        /// 启动游戏流程
        public static CGamePro_StartApplication s_ProcStartApp = null;
        /// 登录游戏循环
        public static CGamePro_Login s_ProcLogIn=null;
        public static CGamePro_Lobby s_ProcLobby=null;
        public static CGamePro_Scene s_ProcScene = null;

        /// 当前激活的流程.
        public static CGameProcedure s_ActiveProcedure=null;
        protected static CGameProcedure s_ProcPrev = null;
        protected static CGameProcedure s_ProcNext = null;

        //初始化静态变量
        public static void InitStaticMemeber()
        {
            Event = new DispatcherHF();

            UIManager.Init();
            RenderEvent.Event.OnRenderPrepareConfirmRequest += OnRenderPrepareConfirmRequest;
            RenderEvent.Event.OnRenderEscapePressedRequest += OnRenderEscapePressedRequest;
            RenderEvent.Event.OnRenderShouldIgnoreInputDuringLoading += OnRenderShouldIgnoreInputDuringLoading;

            //初始化所有的循环实例
            s_ProcStartApp = new CGamePro_StartApplication();//启动游戏
            s_ProcLogIn = new CGamePro_Login();			//!< 登录循环
            s_ProcLobby = new CGamePro_Lobby();
            s_ProcScene = new CGamePro_Scene();




            //可以在全局表里面设置语言
            var lanVer = 0;
            lanVer = PlayerPrefs.GetInt("LanVer", lanVer);
            RenderAPI.InitLanguage(lanVer, RenderAPI.ConvertGameLanId2GameLanCfgName(lanVer), 100);

            

            s_ProcPrev = s_ProcStartApp;
            SetNextProc(s_ProcStartApp);

            RenderEffManager.GetInstance().Init();
        }

        //将一个游戏循环激活
        public static void SetNextProc(CGameProcedure toActive)
        {
            s_ProcNext = toActive;
        }
        //进入当前游戏循环的数据逻辑函数
        public static void TickActive()
        {
            //如果要转入新的游戏循环...
            if (s_ProcNext != null && s_ActiveProcedure != s_ProcNext)
            {
                //调用旧循环的释放函数
                if (s_ActiveProcedure != null)
                {
                    s_ActiveProcedure.UnInit();
                }
                s_ProcPrev = s_ActiveProcedure;

                //调用新循环的初始化函数
                if (s_ProcNext != null)
                {
                    s_ActiveProcedure = s_ProcNext;
                    s_ActiveProcedure.Init();
                    s_ProcNext = null;
                }
            }
            //执行激活循环的数据逻辑
            if (s_ActiveProcedure != null)
            {
                s_ActiveProcedure.Tick();
            }
        }

        //释放静态变量
        public static void ReleaseStaticMember()
        {
            AudioManager.GetInstance().UnInit();
            RenderEffManager.GetInstance().Destroy();
            DataManager.Destroy();
            if (LobbyPlayer.IsInitInstance())
            {
                LobbyPlayer.GetInstance().UnInit();
            }
            //释放所有的循环实例
            if (s_ProcLogIn != null) s_ProcLogIn = null;
            if (s_ProcLobby != null) s_ProcLobby = null;
            if (s_ProcScene != null) s_ProcScene = null;

            if (s_ActiveProcedure != null)
            {
                s_ActiveProcedure.UnInit();
            }
            s_ProcPrev = s_ActiveProcedure = s_ProcNext = null;
            RenderEvent.Event.OnRenderPrepareConfirmRequest -= OnRenderPrepareConfirmRequest;
            RenderEvent.Event.OnRenderEscapePressedRequest -= OnRenderEscapePressedRequest;
            RenderEvent.Event.OnRenderShouldIgnoreInputDuringLoading -= OnRenderShouldIgnoreInputDuringLoading;
            UIManager.Destroy();
            //PrefabLoaderManager.Destroy();
            Event = null;
        }

        private static void OnRenderPrepareConfirmRequest()
        {
            if (UIManager.GetCurrentActiveWindow() is tower_defend_prepare_wnd prepare)
            {
                prepare.OnClickConfirm();
            }
        }

        private static void OnRenderEscapePressedRequest(InputAction.CallbackContext context)
        {
            Event?.OnEscapPressed(context);
        }

        private static bool OnRenderShouldIgnoreInputDuringLoading()
        {
            return loading_wnd.IsBlockingFlowInput();
        }
        //消息主循环
        public static void Update()
        {
            TickActive();


        }

        //public static void SetProcedureStatus(int state)
        //{
        //    s_ActiveProcedure.SetStatus(state);
        //}
        //public static int GetProcedureStatus()
        //{
        //    return s_ActiveProcedure.GetStatus();
        //}
        public static int m_Status;
        public  void SetStatus(int state)
        {
            m_Status = state;
        }
        public  int GetStatus()
        {
            return m_Status;
        }



        protected abstract void Init();
        protected abstract void Tick();
        protected abstract void UnInit();

        protected abstract void OnEscape(InputAction.CallbackContext context);


        protected abstract void OnInput(InputAction.CallbackContext context, InputType inputType);

        public abstract void BackLogin();

        protected virtual void BackLoginCommon()
        {

        }

    }
}
