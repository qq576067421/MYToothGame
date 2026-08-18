using UnityEngine;
using System.Collections;
using LCL;
using System.IO;
using System;
using UnityEngine.Rendering.Universal;
using GameDll;
using UnityEditor;
using UnityUI;
using GameDll;

public class Main : MonoBehaviour 
{
    private bool m_UseAB = true;
    public bool m_EnableGuide = true;
    public int m_ChannelId = 1;
    public int m_ClientAppVersion;
    public int m_ClientResVersion;

    private HotFixManager_SystemDll m_MainHotFixManager_SystemDll;
    private static Main Instance;
    public static GameObject GameMainObject;
    public GameObject m_Update;
    public LUIText m_UpdateInfo;


    private bool m_IsStartGame = false;
    void Start () 
	{

        Application.targetFrameRate = 60;

        //设置文化环境
        System.Globalization.CultureInfo InvariantCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = InvariantCulture;

#if UNITY_EDITOR
        m_UseAB = false;
#endif
        m_IsStartGame = false;
        Instance = this;
        GameMainObject = gameObject;

        Debug.Log(Application.platform);
        DontDestroyOnLoad(gameObject);

        m_UpdateInfo.text = "";
        StartGame();
    }

    public void StartGame()
    {
        m_MainHotFixManager_SystemDll = new HotFixManager_SystemDll();
        m_MainHotFixManager_SystemDll.Init("GameDll");

        //刚启动的时候重置分辨率大小为1，等到设置了UI和屏幕分离的时候再次设置scale
        var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.renderScale = 1.0f;
        }
        m_IsStartGame = true;

        DataManager.Bind();
    }

    public void CloseUpdateWnd()
    {
        if(m_Update != null)
        {
            GameObject.Destroy(m_Update);
            m_Update = null;
            m_UpdateInfo = null;
        }
    }

    public bool IsUseAB()
    {
        return m_UseAB;
    }
    public static Main GetInstance()
    {
        return Instance;
    }

    void Update()
    {
        if (!m_IsStartGame)
        {
            return;
        }
        m_MainHotFixManager_SystemDll?.Update();

        var battleLogic = CBattleLogic.GetInstance();
        if (battleLogic != null)
        {
            battleLogic.Update();
        }

        var counter = GameDll.CounterManager.GetInstance();
        if (counter != null)
        {
            counter.Update();
        }

        GameDll.ServerTime.UpdateServerTime();

        var effMgr = GameDll.RenderEffManager.GetInstance();
        if (effMgr != null)
        {
            effMgr.Update();
        }

        var audioMgr = AudioManager.GetInstance();
        if (audioMgr != null)
        {
            audioMgr.Update(Time.unscaledDeltaTime);
        }

        var saver = GameDll.DataSaver.GetInstance();
        if (saver != null)
        { 
            saver.Update();
        }
    }
    void LateUpdate()
    {
        if (!m_IsStartGame)
        {
            return;
        }

        var battleLogic = CBattleLogic.GetInstance();
        if (battleLogic != null)
        {
            battleLogic.LateUpdate();
        }
    }
    void OnDrawGizmos()
    {
        if (!m_IsStartGame)
        {
            return;
        }

        var battleLogic = CBattleLogic.GetInstance();
        if (battleLogic != null)
        {
            battleLogic.OnDrawGizmos();
        }
    }
    public object CallGameDllFunction(string func, params object[] data)
    {
        return m_MainHotFixManager_SystemDll?.OnMono2GameDll(func, data);
    }
    public object OpenWindow(string classEnum, object parentName, params object[] param)
    {
        if(string.IsNullOrEmpty(classEnum))
        {
            Debug.LogError("classEnum is null");
            return null;
        }
        return CallGameDllFunction("GameDll_UIManager_WindowOpen", classEnum, parentName, param);
    }
    public void CloseWindow(string classEnum)
    {
        if (string.IsNullOrEmpty(classEnum))
        {
            Debug.LogError("classEnum is null");
            return;
        }
        CallGameDllFunction("GameDll_UIManager_WindowClose", classEnum);
    }
    public void CloseWindow(object winClass)
    {
        if (winClass == null)
        {
            Debug.LogError("classEnum is null");
            return;
        }
        CallGameDllFunction("GameDll_UIManager_WindowClose", winClass);
    }
    void OnDestroy()
    {
        m_MainHotFixManager_SystemDll?.Destroy();
        Debug.Log("Ondestroy");
    }
    void OnApplicationQuit()
    {
        if (!m_IsStartGame)
        {
            return;
        }

        m_MainHotFixManager_SystemDll?.OnApplicationQuit();
        
        Debug.Log("OnApplicationQuit");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!m_IsStartGame)
        {
            return;
        }
        AudioManager.GetInstance().SetSystemPaused(pauseStatus);
        if(AndroidServerInfoDemo.Instance) AndroidServerInfoDemo.Instance.OnDestroyAudioPlayer();
    }

    public void NextFrameCall(Action call)
    {
        StartCoroutine(OnNextFrameCall(call));
    }

    private IEnumerator OnNextFrameCall(Action call)
    {
        Action _func = call;
        yield return null;
        if (_func != null)
        {
            _func();
        }
    }
}
