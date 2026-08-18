
using LCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using GameDll;

namespace GameHot
{
    //登录状态

    public class LoginStatus
    {
        public const int None = 0;
        //加载登陆场景
        public const int EnterLoginScene = 1;
        public const int EnteringLoginScene = 2;
        public const int GoLobby = 13;
    };
    public class CGamePro_Login:CGameProcedure
    {
        private const float m_StartGameLoadingMinVisibleSeconds = 2.5f;
        private string m_AssetBundleName = "";
        private bool m_SceneLoaded = false;


        private bool m_bInitData = false;

        public float m_LoginUseTime = 0;
        public float m_PullDataUseTime = 0;
        private bool m_LoginPullData = false;
        private login_wnd login_Wnd;
        private bool m_StartGameRequested = false;

        public bool IsLoginPullingData()
        {
            return m_LoginPullData;
        }

        public CGamePro_Login()
        {
        }

        protected override void Init()
        {
            m_ProType = EProcedureType.eLogin;
            m_StartGameRequested = false;

            Event.OnInputAction += OnInput;
        }

        protected override void UnInit()
        {
            m_AssetBundleName = "";
            m_StartGameRequested = false;
            login_Wnd = null;

            Event.OnInputAction -= OnInput;
        }
        protected override void Tick()
        {
 	        switch(m_Status)
 	        {
                case (int)LoginStatus.EnterLoginScene:
                    {
                        m_StartGameRequested = false;
                        login_Wnd = null;
                        if (!m_bInitData)
                        {
                            m_bInitData = true;

                            var hr = InitDataManager();
                        }

                        var ab = "scene/login.jpg";
                        var assetName = "login";
                        UIRes.LoadLevel(ab, assetName, 0, () =>
                        {
                            login_Wnd = UIManager.OpenWindowEX<login_wnd>(null);
                        });
                        m_Status = (int)LoginStatus.EnteringLoginScene;
                        break;
                    }
                case (int)LoginStatus.EnteringLoginScene:
                    {
                        break;
                    }
                case (int)LoginStatus.GoLobby:
                    {
                        m_Status = (int)LoginStatus.None;
                        SetNextProc(s_ProcLobby);
                        Main.GetInstance().StartCoroutine(delayGoLobby());
                        break;
                    }
 	        }
        }

        System.Collections.IEnumerator delayGoLobby()
        {
            yield return new WaitForSeconds(2f);
            CGameProcedure.s_ProcLobby.SetStatus((int)LobbyStatus.LoadLobbyScene);
        }

        public void StartGame()
        {
            if (m_StartGameRequested)
            {
                return;
            }

            m_StartGameRequested = true;
            loading_wnd.OpenLoading(m_StartGameLoadingMinVisibleSeconds, BeginGoLobbyAfterLoading);
        }

        // 等加载界面真正遮住后再关闭登录界面并切流程，避免露出空场景。
        private void BeginGoLobbyAfterLoading()
        {
            if (!m_StartGameRequested)
            {
                return;
            }

            if (login_Wnd != null && login_Wnd.__IsLogicOpen())
            {
                UIManager.CloseWindow(login_Wnd);
                login_Wnd = null;
            }

            m_Status = (int)LoginStatus.None;
            SetNextProc(s_ProcLobby);
            CGameProcedure.s_ProcLobby.SetStatus((int)LobbyStatus.LoadLobbyScene);
        }


        private bool InitDataManager()
        {
            var hr = DataManager.Init();
            InitHFDataDelegate();
            if (!LobbyPlayer.IsInitInstance())
            {
                LobbyPlayer.GetInstance().Init();
            }
            LobbyPlayer.GetInstance().SetLocalGame(true);
            LobbyPlayer.GetInstance().OnLocalPlayerMainStartMessage();
            LobbyPlayer.GetInstance().SetPlayerState(PlayerState.Lobby);
            return hr;
        }

        //初始化主工程的数据程序集的调用函数
        private static void InitHFDataDelegate()
        {
            //HF.BeanBase.LogWarning = Debug.LogWarning;
            //HF.BeanBase.GeyKeysByListInt = DataManager.GetKeys;
            //HF.BeanBase.GeyKeysByListLong = DataManager.GetKeys;
            //HF.BeanBase.GeyKeysByListString = DataManager.GetKeys;
            //HF.BeanBase.BeginReadByInt = DataManager.BeginRead;
            //HF.BeanBase.BeginReadByLong = DataManager.BeginRead;
            //HF.BeanBase.BeginReadByString = DataManager.BeginRead;
            //HF.BeanBase.__GetAllIntRows = DataManager.GetAllIntRows;
            //HF.BeanBase.__GetAllLongRows = DataManager.GetAllLongRows;
            //HF.BeanBase.__GetAllStringRows = DataManager.GetAllStringRows;
            //HF.BeanBase.IsUseCSV = RenderAPI.IsUseCSV;
            //HF.BeanBase.SqliteDataManager_BeginRead = SqliteDataManager.BeginRead;
            //HF.BeanBase.SqliteDataManager_EndRead = SqliteDataManager.EndRead;
            //HF.BeanBase.SqliteDataManager_ReadInt = SqliteDataManager.ReadInt;
            //HF.BeanBase.SqliteDataManager_ReadLong = SqliteDataManager.ReadLong;
            //HF.BeanBase.SqliteDataManager_ReadString = SqliteDataManager.ReadString;
            //HF.BeanBase.SqliteDataManager_ReadFloat = SqliteDataManager.ReadFloat;
            //HF.BeanBase.SqliteDataManager_ReadBytes = SqliteDataManager.ReadBytes;
            //HF.BeanBase.SqliteDataManager_ReadIntArray = SqliteDataManager.ReadIntArray;
            //HF.BeanBase.SqliteDataManager_ReadLongArray = SqliteDataManager.ReadLongArray;
            //HF.BeanBase.SqliteDataManager_ReadLongArray2 = SqliteDataManager.ReadLongArray2;
            //HF.BeanBase.SqliteDataManager_ReadIntArray2 = SqliteDataManager.ReadIntArray2;
            //HF.BeanBase.SqliteDataManager_ReadIntLongMap = SqliteDataManager.ReadIntLongMap;
            //HF.BeanBase.ReadIntArray = DataManager.ReadIntArray;
            //HF.BeanBase.ReadLongArray = DataManager.ReadLongArray;
            //HF.BeanBase.ReadIntArray2 = DataManager.ReadIntArray2;
            //HF.BeanBase.ReadLongArray2 = DataManager.ReadLongArray2;
            //HF.BeanBase.ReadIntLongMap = DataManager.ReadIntLongMap;
        }

        public override void BackLogin()
        {
            m_LoginPullData = false;
            m_LoginUseTime = 0;
            m_PullDataUseTime = 0;

            BackLoginCommon();

            Debug.Log("登录流程中，返回登录");
        }

        protected override void OnEscape(InputAction.CallbackContext context)
        {
            
        }

        protected override void OnInput(InputAction.CallbackContext context, InputType inputType)
        {
            if(inputType == InputType.Enter)
            {
                if (login_Wnd == null || !login_Wnd.__IsLogicOpen())
                {
                    return;
                }

                login_Wnd.OnClickStartGame();
            }
        }
    }
}
