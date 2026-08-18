using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
namespace LCL
{
    public class ResData
    {
        public UnityEngine.Object m_Obj = null;
        public long m_Id = int.MinValue;
    }

    public class UIRes
    {
        public const long IdNone = 0;
        private static AssetbundleLoader m_ResMgr;
#if UNITY_EDITOR
        private static AssetDatabaseLoader m_ResRawMgr;
#endif
        private static bool m_UseAB = false;
        public static void Initialize(System.Action<bool> initOK)
        {
            m_UseAB = Main.GetInstance().IsUseAB();
            if (m_UseAB)
            {
                GameObject main = GameObject.Find("GameMain");
                m_ResMgr = main.AddComponent<AssetbundleLoader>();
                string manifestName = "codeconfig" + MonoTool.GetAssetbundleSuffix();
                string extra = "codeconfig";
                m_ResMgr.Initialize( extra, manifestName, initOK);
            }
            else
            {
#if UNITY_EDITOR
                GameObject main = GameObject.FindGameObjectWithTag("GameMain");
                m_ResRawMgr = main.AddComponent<AssetDatabaseLoader>();
                if(initOK != null)
                {
                    initOK(true);
                }
#else
                Debug.LogError("当前是真机环境，但是UIRes居然没有使用AB，极有可能有问题");
                Debug.LogError("当前是真机环境，但是UIRes居然没有使用AB，极有可能有问题");
                Debug.LogError("当前是真机环境，但是UIRes居然没有使用AB，极有可能有问题");
#endif
            }
        }


        public static ABRequest LoadPrefabAsync(Type t, string abName, string mainAssetName, Action<ResData, object> func, Action<long, ABRequestResult> errorFunc = null, object userData = null)
        {
            ABRequest abrequest = null;
            if (m_UseAB)
            {
                abrequest = m_ResMgr.LoadPrefabAsync(t, abName, mainAssetName, func, errorFunc, userData);
            }
            else
            {
#if UNITY_EDITOR
                abrequest = m_ResRawMgr.LoadPrefabAsync(t, abName, mainAssetName, func, errorFunc, userData);
#else

#endif
            }
            return abrequest;
        }

        public static void LoadLevel(string abName, string assetName, int mode, Action func)
        {
            if (m_UseAB)
            {
                m_ResMgr.LoadLevel(abName, assetName, mode, func);
            }
            else
            {
#if UNITY_EDITOR
                m_ResRawMgr.LoadLevel(abName, assetName, mode, func);
#else

#endif
            }
        }

        public static void LoadBytes(string abName, Action<byte[]> call)
        {
            if (m_UseAB)
            {
                 m_ResMgr.LoadBytesAsync(abName, call);
            }
            else
            {
#if UNITY_EDITOR
                 m_ResRawMgr.LoadBytesAsync(abName, call);
#endif
            }
        }
        public static void UnloadPrefab(ABRequest id)
        {
            if (m_UseAB)
            {
                m_ResMgr.UnloadPrefab(id);
            }
            else
            {
#if UNITY_EDITOR
                m_ResRawMgr.UnloadPrefab(id);
#endif
            }
        }
        public static string MakeFullPath(string strFileName)
        {
            if (m_UseAB)
            {
                return m_ResMgr.MakeFullPath(strFileName);
            }
            else
            {
#if UNITY_EDITOR
                return m_ResRawMgr.MakeFullPath(strFileName);
#else
                return "";
#endif
            }
        }



        private static Dictionary<object, List<long>> m_Collectors = new Dictionary<object, List<long>>();
        public static void ResouceIdCollect(object wnd, long id)
        {
            if (wnd == null)
            {
                UnityEngine.Debug.LogError("wnd is null");
                return;
            }
            if (m_Collectors.ContainsKey(wnd))
            {
                List<long> ids = m_Collectors[wnd];
                ids.Add(id);
            }
            else
            {
                List<long> ids = new List<long>();
                ids.Add(id);
                m_Collectors.Add(wnd, ids);
            }
        }
        //当通过WindowImageIdCollect方法添加管理后，如果不需要他管理了调用
        public static void ResouceIdReturn(object wnd, long returnid, Action<long> returnCall)
        {
            if (wnd == null)
            {
                UnityEngine.Debug.LogError("wnd is null");
                return;
            }
            if (m_Collectors.ContainsKey(wnd))
            {
                List<long> ids = m_Collectors[wnd];
                int count = ids.Count;
                for (int i = 0; i < count; ++i)
                {
                    long id = ids[i];
                    if (id == returnid)
                    {
                        ids.Remove(id);
                        returnCall(id);
                        break;
                    }

                }

                if(ids.Count == 0)
                {
                    m_Collectors.Remove(wnd);
                }

            }
        }
        //这里只是清空，同时兼顾gc
        public static void ResouceIdReturn(object wnd, Action<long> returnCall)
        {
            if (wnd == null)
            {
                UnityEngine.Debug.LogError("wnd is null");
                return;
            }
            if (m_Collectors.ContainsKey(wnd))
            {
                List<long> ids = m_Collectors[wnd];
                int count = ids.Count;
                for (int i = 0; i < count; ++i)
                {
                    long id = ids[i];
                    if (returnCall != null)
                    {
                        returnCall(id);
                    }
                }
                ids.Clear();
                m_Collectors.Remove(wnd);
            }
        }
    }
}