using LCL;
using System;
using System.IO;
using UnityEngine;
using UnityUI;
using GameDll;
using System.Configuration;

namespace GameHot
{
    internal sealed class FilePreloadHandler
    {
        private const string m_LogTag = "login_wnd:";
        private const string m_VideoFolderName = "video";
        private const string m_VersionResourceName = "version";
        private const string m_VersionFileName = "version.bytes";
        private const float m_CopyTimeoutSeconds = 60.0f;
        private const int m_CopyRetryCount = 1;

        private readonly string[] m_PreloadFileNames =
        {
            "faill.mp4",
            "star1.mp4",
            "star2.mp4",
            "star3.mp4",
        };

        private readonly WindowBase m_Owner;
        private readonly Action m_OnFinishedCall;
        private CopyFile2SDCard m_CopyFile2SDCard;
        private TextAsset m_PackageVersionAsset;
        private string m_PackageVersionText = string.Empty;
        private int m_CopyIndex = 0;
        private bool m_IsCanceled = false;
        private bool m_IsFinished = false;

        public FilePreloadHandler(WindowBase owner, Action onFinishedCall)
        {
            m_Owner = owner;
            m_OnFinishedCall = onFinishedCall;
        }

        public void Start()
        {
            m_IsCanceled = false;
            m_IsFinished = false;
            m_CopyIndex = 0;

            Debug.Log(string.Format(
                "{0} 文件预拷贝开始 editor={1} persistentDataPath={2} streamingAssetsPath={3}",
                m_LogTag,
                Application.isEditor,
                Application.persistentDataPath,
                Application.streamingAssetsPath));

            if (Application.isEditor)
            {
                Debug.Log(m_LogTag + " 编辑器模式跳过文件预拷贝");
                Finish();
                return;
            }

            m_PackageVersionAsset = Resources.Load<TextAsset>(m_VersionResourceName);
            m_PackageVersionText = GetPackageVersionText(m_PackageVersionAsset);
            Debug.Log(string.Format(
                "{0} 当前包体版本 version={1} versionPath={2}",
                m_LogTag,
                string.IsNullOrEmpty(m_PackageVersionText) ? "<empty>" : m_PackageVersionText,
                GetPersistentFilePath(m_VersionFileName)));
            m_CopyFile2SDCard = EnsureCopyFile2SDCard();

            if (m_CopyFile2SDCard == null)
            {
                Debug.LogError(m_LogTag + " 未能添加 CopyFile2SDCard 组件");
                Finish();
                return;
            }

            if (!NeedCopyFiles())
            {
                Debug.Log(m_LogTag + " 当前版本文件已齐全，跳过视频拷贝，仅回写 version.bytes");
                WriteVersionFileToPersistent();
                Finish();
                return;
            }

            CopyNextFile();
        }

        public void Cancel()
        {
            m_IsCanceled = true;
            Debug.Log(m_LogTag + " 文件预拷贝被取消");
        }

        private CopyFile2SDCard EnsureCopyFile2SDCard()
        {
            GameObject wndObj = m_Owner.__GetWindowObj();
            if (wndObj == null)
            {
                return null;
            }

            CopyFile2SDCard copyFile2SDCard = wndObj.GetComponent<CopyFile2SDCard>();
            if (copyFile2SDCard == null)
            {
                copyFile2SDCard = wndObj.AddComponent<CopyFile2SDCard>();
            }

            return copyFile2SDCard;
        }

        private bool NeedCopyFiles()
        {
            string versionPath = GetPersistentFilePath(m_VersionFileName);
            if (!File.Exists(versionPath))
            {
                Debug.Log(m_LogTag + " 本地不存在 version.bytes，需要重新拷贝");
                return true;
            }

            string persistentVersion = ReadTextFile(versionPath);
            if (!string.Equals(persistentVersion, m_PackageVersionText, StringComparison.Ordinal))
            {
                Debug.Log(string.Format(
                    "{0} version.bytes 版本不一致，需要重新拷贝 persistentVersion={1} packageVersion={2}",
                    m_LogTag,
                    string.IsNullOrEmpty(persistentVersion) ? "<empty>" : persistentVersion,
                    string.IsNullOrEmpty(m_PackageVersionText) ? "<empty>" : m_PackageVersionText));
                return true;
            }

            int count = m_PreloadFileNames.Length;
            for (int i = 0; i < count; ++i)
            {
                string persistentFilePath = GetPersistentFilePath(m_PreloadFileNames[i]);
                if (!File.Exists(persistentFilePath))
                {
                    Debug.Log(string.Format("{0} 缺少预拷贝文件，需要重新拷贝 path={1}", m_LogTag, persistentFilePath));
                    return true;
                }
            }

            Debug.Log(m_LogTag + " 当前版本文件和视频文件都已存在，无需重新拷贝");
            return false;
        }

        private void CopyNextFile()
        {
            if (m_IsCanceled || m_IsFinished)
            {
                return;
            }

            if (m_CopyIndex >= m_PreloadFileNames.Length)
            {
                WriteVersionFileToPersistent();
                Finish();
                return;
            }

            string fileName = m_PreloadFileNames[m_CopyIndex];
            Debug.Log(string.Format(
                "{0} 开始拷贝文件 index={1}/{2} source={3} target={4}",
                m_LogTag,
                m_CopyIndex + 1,
                m_PreloadFileNames.Length,
                GetStreamingRelativeFilePath(fileName),
                GetPersistentFilePath(fileName)));
            m_CopyFile2SDCard.CopyFromStreamingAssets(
                GetStreamingRelativeFilePath(fileName),
                GetPersistentFilePath(fileName),
                OnCopyFileFinished,
                m_CopyTimeoutSeconds,
                m_CopyRetryCount);
        }

        private void OnCopyFileFinished(CopyFile2SDCard.CopyResult result)
        {
            if (m_IsCanceled || m_IsFinished)
            {
                return;
            }

            if (result == null || !result.m_Success)
            {
                string fileName = m_CopyIndex >= 0 && m_CopyIndex < m_PreloadFileNames.Length ? m_PreloadFileNames[m_CopyIndex] : string.Empty;
                string errorMessage = result != null ? result.m_ErrorMessage : "复制结果为空";
                Debug.LogError(string.Format(
                    "{0} 拷贝文件失败 file={1} source={2} target={3} attempt={4} code={5} error={6}",
                    m_LogTag,
                    fileName,
                    result != null ? result.m_SourcePath : string.Empty,
                    result != null ? result.m_TargetPath : string.Empty,
                    result != null ? result.m_AttemptCount : 0,
                    result != null ? result.m_ResultCode.ToString() : string.Empty,
                    errorMessage));
                Finish();
                return;
            }

            Debug.Log(string.Format(
                "{0} 拷贝文件成功 file={1} source={2} target={3} attempt={4} size={5}",
                m_LogTag,
                m_CopyIndex >= 0 && m_CopyIndex < m_PreloadFileNames.Length ? m_PreloadFileNames[m_CopyIndex] : string.Empty,
                result.m_SourcePath,
                result.m_TargetPath,
                result.m_AttemptCount,
                result.m_FileSize));
            ++m_CopyIndex;
            CopyNextFile();
        }

        private void WriteVersionFileToPersistent()
        {
            if (m_PackageVersionAsset == null || m_PackageVersionAsset.bytes == null || m_PackageVersionAsset.bytes.Length == 0)
            {
                Debug.LogError(m_LogTag + " 未找到包体内的 version.bytes");
                return;
            }

            string versionPath = GetPersistentFilePath(m_VersionFileName);
            try
            {
                string dir = Path.GetDirectoryName(versionPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(versionPath, m_PackageVersionAsset.bytes);
                Debug.Log(string.Format(
                    "{0} 已写入 version.bytes path={1} size={2}",
                    m_LogTag,
                    versionPath,
                    m_PackageVersionAsset.bytes.Length));
            }
            catch (Exception ex)
            {
                Debug.LogError(m_LogTag + " 写入 version.bytes 失败，错误：" + ex.Message);
            }
        }

        private string GetPackageVersionText(TextAsset versionAsset)
        {
            if (versionAsset == null)
            {
                return string.Empty;
            }

            return versionAsset.text != null ? versionAsset.text.Trim() : string.Empty;
        }

        private string GetPersistentFilePath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, m_VideoFolderName, fileName);
        }

        private string GetStreamingRelativeFilePath(string fileName)
        {
            return m_VideoFolderName + "/" + fileName;
        }

        private string ReadTextFile(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath).Trim();
            }
            catch (Exception ex)
            {
                Debug.LogError(m_LogTag + " 读取文件失败，路径：" + filePath + "，错误：" + ex.Message);
                return string.Empty;
            }
        }

        private void Finish()
        {
            if (m_IsCanceled || m_IsFinished)
            {
                return;
            }

            m_IsFinished = true;
            Debug.Log(string.Format(
                "{0} 文件预拷贝结束 canceled={1} copiedCount={2}",
                m_LogTag,
                m_IsCanceled,
                m_CopyIndex));
            if (m_OnFinishedCall != null)
            {
                m_OnFinishedCall();
            }
        }
    }

    //GM密码，当下方连续点击了5次以上，表示放弃此次密码，清空输入；每次密码需要从0开始。当没有点击方向键超过2秒，就开始确认密码
    //是否正确，确认后清空输入。
    internal sealed class GMTrigger
    {
        //0310221
        private const long m_Password = 310221;
        private const int m_ConfirmDelayMMSec = 2000;
        private const int m_CancelBottomClickCount = 5;
        private const int m_BottomNumber = 2;

        private long m_CurrentPassword = -1;
        private int m_ContinuousBottomClickCount = 0;
        private long m_ConfirmTimerId = -1;

        private v_login_wnd.v_GMTrigger m_View;
        internal void Init(WindowBase wnd, ComponentBridge bridge)
        {
            m_View = new v_login_wnd.v_GMTrigger();
            m_View.InitComponent(bridge.gameObject);


            RenderAPI.AddButtonClick(m_View.m_btnLeftGM, OnClickLeft, 0);
            RenderAPI.AddButtonClick(m_View.m_btnRightGM, OnClickRight, 0);
            RenderAPI.AddButtonClick(m_View.m_btnTopGM, OnClickTop, 0);
            RenderAPI.AddButtonClick(m_View.m_btnBottomGM, OnClickBottom, 0);
        }

        private void OnClickBottom()
        {
            OnClickNumber(2);
        }

        private void OnClickTop()
        {
            OnClickNumber(0);
        }

        private void OnClickRight()
        {
            OnClickNumber(1);
        }

        private void OnClickLeft()
        {
            OnClickNumber(3);
        }

        private void OnClickNumber(int number)
        {
            if (number == m_BottomNumber)
            {
                ++m_ContinuousBottomClickCount;
            }
            else
            {
                m_ContinuousBottomClickCount = 0;
            }

            if (m_ContinuousBottomClickCount >= m_CancelBottomClickCount)
            {
                ClearInput();
                return;
            }
            if(m_CurrentPassword == -1)
            {
                if(number != 0)
                {
                    return;
                }
                m_CurrentPassword = 0;
            }
            if(m_CurrentPassword > int.MaxValue)
            {
                ClearInput();
                return;
            }
            // m_CurrentPassword 每轮从 0 开始，首个方向数字为 0 时不会改变最终整数值。
            m_CurrentPassword = m_CurrentPassword * 10 + number;
            Debug.Log("当前密码：" + m_CurrentPassword);
            if (m_CurrentPassword == m_Password)
            {
                UIManager.OpenWindowEX<gm_wnd>(null);
                ClearInput();
                return;
            }
            RestartConfirmTimer();
        }

        private void RestartConfirmTimer()
        {
            StopConfirmTimer();
            m_ConfirmTimerId = CounterManager.GetInstance().AddCounter(m_ConfirmDelayMMSec, 1, ClearInput);
        }

        private void StopConfirmTimer()
        {
            if (m_ConfirmTimerId != -1)
            {
                CounterManager.GetInstance().RemoveCounter(m_ConfirmTimerId);
                m_ConfirmTimerId = -1;
            }
        }
        internal void ClearInput()
        {
            StopConfirmTimer();
            m_CurrentPassword = -1;
            m_ContinuousBottomClickCount = 0;
        }

        internal void Destroy()
        {
            ClearInput();
            m_View = null;
        }
    }
    public class login_wnd : WindowBase
    {
        private v_login_wnd m_Wnd = null;
        private FilePreloadHandler m_LoginFilePreloadHandler = null;
        private bool m_HasClosedLoading = false;
        private bool m_CanStartGame = false;
        private bool m_StartGameRequested = false;
        private GMTrigger m_GMTrigger = null;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Hold;
            __CustomUIPrefabDir = UIPrefabDirs.login;
            __ParticipateCurrentActiveWindow = true;
        }
        protected override void OnInitComponent()
        {
            m_Wnd = new v_login_wnd();
            m_Wnd.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_Wnd.m_btnStartGame, OnClickStartGame);
            RenderAPI.AddButtonClick(m_Wnd.m_btnYear, OnClickYear);

            m_GMTrigger = new GMTrigger();
            m_GMTrigger.Init(this, m_Wnd.m_GMTrigger);

        }

        private void OnClickYear()
        {
            UIManager.OpenWindowEX<year_wnd>(null);
        }

        protected override void OnDestroy()
        {
            m_GMTrigger.Destroy();
            m_GMTrigger = null;
            base.OnDestroy();
        }

        protected override void OnOpen()
        {
            m_HasClosedLoading = false;
            m_CanStartGame = false;
            m_StartGameRequested = false;
            SetStartButtonInteractable(false);
            m_LoginFilePreloadHandler = new FilePreloadHandler(this, OnFilePreloadFinished);
            m_LoginFilePreloadHandler.Start();

            UIManager.ChangeGlobalCanvasRenderMode(RenderMode.ScreenSpaceCamera);

            PlayMusic();

            DeleteStartVideo();

            SetVersion();
        }

        private void SetVersion()
        {
            RenderAPI.SetText(m_Wnd.m_txtVersion, Application.version);
        }

        private void DeleteStartVideo()
        {
            var go = UIManager.GetLayer(WindowLayer.Hold).Find("start_video_wnd");
            if(go!= null)
            GameObject.Destroy(go.gameObject);
        }

        private void PlayMusic()
        {
            AudioManager.GetInstance().Play2D(
                300,
                AudioTransitionMode.CrossFade,
                -1f,
                AudioReplayMode.KeepCurrent,
                AudioLifetime.Persistent);
        }

        protected override void OnClose()
        {
            UIManager.ChangeGlobalCanvasRenderMode(RenderMode.ScreenSpaceOverlay);
            if (m_GMTrigger != null)
            {
                m_GMTrigger.ClearInput();
            }
            if (m_LoginFilePreloadHandler != null)
            {
                m_LoginFilePreloadHandler.Cancel();
                m_LoginFilePreloadHandler = null;
            }
        }

        public void OnClickStartGame()
        {
            if (m_StartGameRequested)
            {
                return;
            }

            if (!m_CanStartGame)
            {
                Debug.Log("login_wnd: 登录资源尚未准备完成");
                return;
            }

            m_StartGameRequested = true;
            SetStartButtonInteractable(false);
            CGameProcedure.s_ProcLogIn.StartGame();
        }

        public void DoCloseLoading()
        {
            if (m_HasClosedLoading)
            {
                return;
            }

            m_HasClosedLoading = true;
            loading_wnd.CloseLoading();
        }

        private void OnFilePreloadFinished()
        {
            m_CanStartGame = true;
            SetStartButtonInteractable(true);
            DoCloseLoading();
        }

        private void SetStartButtonInteractable(bool interactable)
        {
            if (m_Wnd == null || m_Wnd.m_btnStartGame == null)
            {
                return;
            }

            m_Wnd.m_btnStartGame.interactable = interactable;
        }
    }
}
