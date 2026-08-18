#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System;
using LCL;
using System.Collections.Generic;
using System.IO;
using Object = UnityEngine.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.Video;

public class AssetDatabaseLoader : MonoBehaviour
{
    class ObjectInfo
    {
        public Dictionary<string, UnityEngine.Object> m_Objects = null;
        public int m_ReferencedCount;

        public ObjectInfo(Dictionary<string, UnityEngine.Object> objects)
        {
            m_ReferencedCount = 0;
            m_Objects = objects;
        }
    }
    Dictionary<string, ObjectInfo> m_LoadedAssetBundles = new Dictionary<string, ObjectInfo>();
    Dictionary<string, ABReqList> m_LoadRequests = new Dictionary<string,ABReqList>();

    //异步
    private static long m_IdAsync = 1;
    private static long m_AbRefId = 1;

    public bool HasLoading()
    {
        return m_LoadRequests.Count > 0;
    }

    public ABRequest LoadPrefabAsync(Type t, string abName, string mainAssetName, Action<ResData, object> func, Action<long, ABRequestResult> errorFunc, object userData = null)
    {
        return LoadAssetAsync(t, abName, mainAssetName, func, errorFunc, userData);
    }

    ABRequest LoadAssetAsync(Type t, string abName, string mainAssetName, Action<ResData, object> action, Action<long, ABRequestResult> errorAction, object userData = null)
    {
        ABRequest request = new ABRequest();
        request.assetType = t;
        request.mainAssetName = mainAssetName;
        request.sharpFunc = action;
        request.sharpErrorFunc = errorAction;
        request.LoadIndex = m_IdAsync++;
        request.UserData = userData;
        request.abName = abName;

        ABReqList requests = null;
        if (!m_LoadRequests.TryGetValue(abName, out requests))
        {
            requests = new ABReqList();
            requests.m_ABRefId = m_AbRefId++;
            request.abRefId = requests.m_ABRefId;

            requests.m_ABReqs.Add(request);
            m_LoadRequests.Add(abName, requests);
            StartCoroutine(OnLoadAsset(t, abName));
        }
        else
        {
            request.abRefId = requests.m_ABRefId;
            requests.m_ABReqs.Add(request);
        }
        return request;
    }

    private string ConvertExtension(string abName, Type t,out bool findPath)
    {
        findPath = true;
        if (abName.Contains("/config_csv/"))
        {
            abName = Path.ChangeExtension(abName, ".csv");
        }
        else if (abName.Contains("/data_json/"))
        {
            abName = Path.ChangeExtension(abName, ".json");
        }
        else if (abName.Contains("/data_xml/"))
        {
            abName = Path.ChangeExtension(abName, ".xml");
        }
        else if (abName.Contains("/prefab/"))
        {
            abName = Path.ChangeExtension(abName, ".prefab");
        }
        else
        {
            if(t != null)
            {
                if (t == typeof(GameObject))
                {
                    abName = Path.ChangeExtension(abName, ".prefab");
                }
                else if (t == typeof(Sprite) || t == typeof(Texture) || t == typeof(Texture2D))
                {
                    abName = Path.ChangeExtension(abName, ".png");
                }
                else if (t == typeof(Material))
                {
                    abName = Path.ChangeExtension(abName, ".material");
                }
                else if (t == typeof(Font))
                {
                    abName = Path.ChangeExtension(abName, ".ttf");
                }
                else if(t == typeof(ShaderVariantCollection))
                {
                    abName = Path.ChangeExtension(abName, ".shadervariants");
                }
                else if(t == typeof(AudioMixer))
                {
                    abName = Path.ChangeExtension(abName, ".mixer");
                }
                else if(t == typeof(VideoClip))
                {
                    abName = Path.ChangeExtension(abName, ".mp4");
                }
                else
                {
                    findPath = false;
                }
            }
            else
            {

                findPath = false;
            }

        }
        return abName;
    }
    public void LoadBytesAsync(string abName, Action<byte[]> call)
    {
        StartCoroutine(LoadBytesImp(abName, call));
    }
    IEnumerator LoadBytesImp(string abName, Action<byte[]> call)
    {
        yield return null;

        bool findPath = false;
        abName = ConvertExtension(abName, null, out findPath);
        TextAsset data = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(abName);
        if (data != null)
        {
            call(data.bytes);
        }
        else
        {
            call(null);
        }
    }

    IEnumerator OnLoadAsset(Type t, string abName)
    {
        yield return null;
        ObjectInfo bundleInfo = GetLoadedAssetBundle(abName);
        if (bundleInfo == null)
        {
            bundleInfo = LoadAssetAtPath(t, abName);
            m_LoadedAssetBundles.Add(abName, bundleInfo);
            if (bundleInfo == null)
            {
                m_LoadRequests.Remove(abName);
                yield break;
            }
        }

        ABReqList list = null;
        if (!m_LoadRequests.TryGetValue(abName, out list))
        {
            m_LoadRequests.Remove(abName);
            yield break;
        }

        for (int i = 0; i < list.m_ABReqs.Count; i++)
        {
            ABRequest loadAssetRequest = list.m_ABReqs[i];
            if(loadAssetRequest == null)
            {
                continue;
            }
            string assetName = loadAssetRequest.mainAssetName;
            //List<UnityEngine.Object> result = new List<UnityEngine.Object>();
            ResData result = new ResData();
            if (bundleInfo.m_Objects.ContainsKey(assetName))
            {
                result.m_Obj = bundleInfo.m_Objects[assetName];
            }
            else
            {
                Debug.LogError("OnLoadAsset error : " + abName);
            }
            result.m_Id = loadAssetRequest.LoadIndex;
            //Debug.Log("OnLoadAsset " + abName);

            bundleInfo.m_ReferencedCount++;

            if (result.m_Obj != null)
            {
                if (loadAssetRequest.sharpFunc != null)
                {
                    loadAssetRequest.sharpFunc(result, loadAssetRequest.UserData);
                    loadAssetRequest.sharpFunc = null;
                }
            }
            else
            {
                if (loadAssetRequest.sharpErrorFunc != null)
                {
                    loadAssetRequest.sharpErrorFunc(result.m_Id, ABRequestResult.LoadError);
                    loadAssetRequest.sharpErrorFunc = null;
                }
            }


        }

        m_LoadRequests.Remove(abName);
    }


    private ObjectInfo LoadAssetAtPath(Type t, string abName)
    {
        string url = MakeFullPath(abName);
        Dictionary<string, UnityEngine.Object> objList = null;
        if (url.Contains("/texture_set/"))
        {
            url = url.Replace(".jpg", "");

            string[] GUIDs = AssetDatabase.FindAssets("t:Sprite", new string[] { url });
            Sprite[] objectList = new Sprite[GUIDs.Length];
            for (int index = 0; index < GUIDs.Length; index++)
            {
                string guid = GUIDs[index];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Sprite asset = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                objectList[index] = asset;
            }

            var assetObjs = objectList;
            if(assetObjs == null || assetObjs.Length == 0)
            {
                Debug.LogError("资源没有找到，" + url);
            }
            objList = new Dictionary<string, UnityEngine.Object>();
            foreach (var obj in assetObjs)
            {
                string name = obj.name;
                objList.Add(name, obj);
            }
        }
        else
        {
            bool findPath = false;
            url = ConvertExtension(url, t, out findPath);
            if (findPath)
            {
                var assetObj = UnityEditor.AssetDatabase.LoadAssetAtPath(url, t);
                if(assetObj == null)
                {
                    Debug.LogError("资源没有找到，" + url);
                }
                objList = new Dictionary<string, UnityEngine.Object>();
                string assetName = Path.GetFileNameWithoutExtension(url);
                objList.Add(assetName, assetObj);
            }
            else
            {
                Debug.Log("资源没有找到，" + url);
            }
        }
        if (objList == null)
        {
            return null;
        }
        return new ObjectInfo(objList);
    }
    ObjectInfo GetLoadedAssetBundle(string abName)
    {
        ObjectInfo bundle = null;
        m_LoadedAssetBundles.TryGetValue(abName, out bundle);
        return bundle;
    }
    public void UnloadPrefab(ABRequest id)
    {
        string abName = id.abName;
        UnloadAssetBundlepublic(abName);
        if (m_LoadRequests.ContainsKey(abName))
        {
            var list = m_LoadRequests[abName];
            int count = list.m_ABReqs.Count;
            for(int i = 0; i < count; ++i)
            {
                var req = list.m_ABReqs[i];
                if(req == id)
                {
                    if (req.sharpErrorFunc != null)
                    {
                        req.sharpErrorFunc(req.LoadIndex, ABRequestResult.Cancel);
                        req.sharpErrorFunc = null;
                    }
                    list.m_ABReqs[i] = null;
                    break;
                }
            }
        }
    }

    void UnloadAssetBundlepublic(string abName)
    {
        ObjectInfo bundle = GetLoadedAssetBundle(abName);
        if (bundle == null)
        {
            //Debug.LogError("需要释放的abName没有找到," + abName);
            return;
        }
        bundle.m_ReferencedCount -= 1;
    }


    public string MakeFullPath(string strFileName)
    {
        if (strFileName == null)
        {
            return null;
        }
        string strFullName = "Assets/art/out/" + strFileName;
        return strFullName;
    }

    public void LoadLevel(string abName, string assetName, int mode, Action func)
    {
        if (!m_IsInitSceneOpened)
        {
            m_IsInitSceneOpened = true;
            EditorSceneManager.sceneLoaded += EditorSceneManager_sceneLoaded;
        }
        m_LoadedSceneFunc = func;
        string path = "Assets/art/out/" + abName.Replace(".jpg", ".unity");

        UnityEngine.SceneManagement.LoadSceneParameters p = new UnityEngine.SceneManagement.LoadSceneParameters();
        p.loadSceneMode = (UnityEngine.SceneManagement.LoadSceneMode)mode;
        var result = EditorSceneManager.LoadSceneAsyncInPlayMode(path, p);

    }
    private bool m_IsInitSceneOpened = false;
    private Action m_LoadedSceneFunc;
    private void EditorSceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode arg1)
    {
        //这里不需要代码释放，unity会自动处理
        //if(m_LastScene != null)
        //{
        //    EditorSceneManager.CloseScene(m_LastScene, true);
        //}
        if (m_LoadedSceneFunc != null)
        {
            var func = m_LoadedSceneFunc;
            m_LoadedSceneFunc = null;
            func();
        }
    }
}
#endif