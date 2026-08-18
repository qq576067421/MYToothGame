using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameDll;

using UnityEngine;
using System.Reflection;





public class HotFixManager_SystemDll
{
    private IGameHotFixInterface m_HotFixDll = null;

    public void Init(string dllName)
    {
#if UNITY_EDITOR
        string dllPath = GetEditorHotFixPath(dllName, ".dll.bytes");
        string mdbPath = GetEditorHotFixPath(dllName, ".mdb.bytes");
        var dllBytes = File.ReadAllBytes(dllPath);
        var mdbBytes = File.ReadAllBytes(mdbPath);
        Assembly assembly = Assembly.Load(dllBytes, mdbBytes);
        Type hotFixType = assembly.GetType("GameHot.HotFixLoop");
        m_HotFixDll = Activator.CreateInstance(hotFixType) as IGameHotFixInterface;
        m_HotFixDll.Start();
#elif MAKE_APP
        m_HotFixDll = new GameHot.HotFixLoop();
        m_HotFixDll.Start();
#endif
    }

    public void Destroy()
    {
        if (m_HotFixDll != null)
        {
            m_HotFixDll.OnDestroy();
        }
    }
    public void Update()
    {
        if (m_HotFixDll != null)
        {
            m_HotFixDll.Update();
        }
    }
    public void OnApplicationQuit()
    {
        if (m_HotFixDll != null)
        {
            m_HotFixDll.OnApplicationQuit();
        }
    }
    public object OnMono2GameDll(string func, params object[] data)
    {
        if (m_HotFixDll == null)
        {
            Debug.LogError("热更DLL未初始化，无法调用：" + func);
            return null;
        }

        return m_HotFixDll.OnMono2GameDll(func, data);
    }

    public static void InitScript(object obj)
    {

    }

#if UNITY_EDITOR
    private static string GetEditorHotFixPath(string dllName, string extension)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "GameDll", dllName + extension));
    }
#endif

}
