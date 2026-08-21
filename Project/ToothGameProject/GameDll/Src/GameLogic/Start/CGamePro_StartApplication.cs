using System;
using System.Collections.Generic;
using System.Text;
using LCL;
using UnityEngine;
using UnityEngine.InputSystem;
using GameDll;

namespace GameHot
{
    //--------------------------------------------------------------
    //登录状态
    public enum StartApplicationStatus
    {
        None,
        FirstOpenGame,
        InitingResMgr,
        InitAudio,
        AppInitOk,
    };
    /// <summary>
    /// 游戏启动流程
    /// </summary>
    public class CGamePro_StartApplication:CGameProcedure
    {
        protected override void Init()
        {
            m_ProType = EProcedureType.eStart;
            m_Status = (int)StartApplicationStatus.FirstOpenGame;

        }
        protected override void UnInit()
        {

        }
        protected override void Tick()
        {
            switch (m_Status)
            {
                case (int)StartApplicationStatus.FirstOpenGame:
                    {
                        FirstOpenGame();
                        Main.GetInstance().StartCoroutine(DisplayLogo());
                        break;
                    }
                case (int)StartApplicationStatus.InitingResMgr:
                    {
                        OnInitingResMgr();
                        break;
                    }
                case (int)StartApplicationStatus.AppInitOk:
                    {
                        OnAppInitOk();
                        break;
                    }
            }
        }

        System.Collections.IEnumerator DisplayLogo()
        {
            yield return new WaitForSeconds(2f);
            //这里必须要等到音频引擎初始化完毕才能进入loading，不然loading的声音无法播放
            while(!m_IsInitedAudio)
            {
                yield return new WaitForSeconds(0.1f);
            }
            loading_wnd.OpenLoading();
            yield return new WaitForSeconds(2f);
            SetStatus((int)StartApplicationStatus.AppInitOk);
        }
        private bool m_IsInitedAudio = false;
        private  void InitAudio()
        {
            m_IsInitedAudio = false;
            AudioManager.GetInstance().Init((success) =>
            {
                RenderAPI.m_GlobalSoundId_ButtonClick = 1;
                RenderAPI.m_GlobalSoundId_DrapDownChanged = 2;
                RenderAPI.m_GlobalSoundId_ToggleChanged = 3;

                m_IsInitedAudio = true;

                var cameraFoot = LCL.CameraFoot.GetInstance().gameObject;
                if (cameraFoot != null)
                {
                    var cameraFootCache = cameraFoot.transform;
                    cameraFootCache.position = new Vector3(0, 0, 0);
                    AudioManager.GetInstance().SetDefaultAudioListenerTarget(cameraFootCache);

                    var cameraEyeCache = cameraFootCache.Find("CameraEye");
                    var camera = (Camera)cameraEyeCache.GetComponent(typeof(Camera));

                    camera.enabled = true;

                    //urp渲染管线设置
                    if(m_UseURP)
                    {
                        //RenderAPI.SetCameraURPType(camera, 0);
                        //RenderAPI.AddCameraStack(camera, UIManager.m_UICamera);
                    }


                }
                RenderAPI.InitTimeAndPhysics();
                //第一次启动游戏？
                var first_game_start_key = "first_game_start_key";
                if(!PlayerPrefs.HasKey(first_game_start_key))
                {
                    PlayerPrefs.SetInt(first_game_start_key, 1);

                    var sizeG = SystemInfo.systemMemorySize / 1024;
                    if(sizeG > 10)
                    {
                        //高端机
                        QualitySettings.SetQualityLevel((int)QualityLevel.Beautiful);
                    }
                    else if(sizeG > 5 && sizeG < 10)
                    {
                        //中端机
                        QualitySettings.SetQualityLevel((int)QualityLevel.Good);
                    }
                    else if(sizeG < 5)
                    {
                        //低端机
                        QualitySettings.SetQualityLevel((int)QualityLevel.Simple);
                    }
                }
                RenderAPI.SetResolution();
            });
        }
        private bool m_UseURP = true;

        //这里目前仅仅是给编译着色器用
        private float m_StartProgress = 0.6f;
        private float m_EndProgress = 0.9f;
        private float m_ProgressStep = 0.03f;
        private bool m_StartShaderCompile = false;
        private float m_StartShaderCompileTime = 0;
        private float m_LastUpdateShaderCompileProgress = 0;
        private void OnInitingResMgr()
        {
            if(m_StartShaderCompile)
            {
                if(Time.realtimeSinceStartup - m_LastUpdateShaderCompileProgress < 0.2f)
                {
                    return;
                }
                m_LastUpdateShaderCompileProgress = Time.realtimeSinceStartup;

                var total_time = (m_EndProgress - m_StartProgress) / m_ProgressStep;
                var dt = Time.realtimeSinceStartup - m_StartShaderCompileTime;
                var progress = Mathf.Lerp(m_StartProgress, m_EndProgress, dt / total_time);
            }
        }
        private void FirstOpenGame()
        {
			SetStatus((int)StartApplicationStatus.InitingResMgr);
            
            UIRes.Initialize((result0) =>
            {
                if (!result0)
                {
                    UDebug.LogError("Res初始化失败");
                    return;
                }
                UDebug.Log("init Res ok");

                //特殊处理，请勿随意更改位置
                UIManager.OpenWindowEX<start_video_wnd>(null);

                m_StartShaderCompile = true;
                m_StartShaderCompileTime = Time.realtimeSinceStartup;
                ShaderManager.CacheShader(() => 
                {
                    UDebug.Log("CacheShader ok");
                    ShaderManager.PrewarmShader(() => 
                    {
                        UDebug.Log("PrewarmShader ok");
                        UDebug.Log("开始加载默认字体");



                        string font_url = "font/700w.jpg";

                        m_StartShaderCompile = false;

                        SetStatus((int)StartApplicationStatus.InitAudio);

                        InitAudio();



                        //RenderAPI.LoadFont(font_url, () => 
                        //{
                        //    UDebug.Log("加载默认字体结束");
                        //    SetStatus((int)StartApplicationStatus.InitAudio);

                        //    InitAudio();
                        //});                               
                    });
                });

            });

        }
        private void OnAppInitOk()
        {
            //这里不能调用Load_wnd 因为还没有初始化配置表，用于提示的
            //loading_wnd.OpenLoading(() =>
            //{
            Main.GetInstance().CloseUpdateWnd();
            SetNextProc(CGameProcedure.s_ProcLogIn);
            CGameProcedure.s_ProcLogIn.SetStatus((int)LoginStatus.EnterLoginScene);
            //});

        }

        public override void BackLogin()
        {
            Debug.Log("初始化流程中，返回登录");
        }

        protected override void OnEscape(InputAction.CallbackContext context)
        {
            
        }

        protected override void OnInput(InputAction.CallbackContext context, InputType inputType)
        {
            throw new NotImplementedException();
        }
    }
}
