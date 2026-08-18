using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using LCL;
using AssetBundles;
using UnityEngine.SceneManagement;

public class ABReqList
{
    public List<ABRequest> m_ABReqs = new List<ABRequest>();
    public long m_ABRefId;
}
public enum ABRequestResult
{
    Normal,
    LoadError,
    Cancel,
}
public class ABRequest
{
    public Type assetType;
    public string mainAssetName;
    public Action<ResData, object> sharpFunc;
    public Action<long, ABRequestResult> sharpErrorFunc;
    public System.Action levelSharpFunc;
    public long LoadIndex = int.MinValue;
    public object UserData;
    public string abName = "";
    public bool fullPath;
    //ab加载批次id
    public long abRefId;
    public bool isRelease;
}
//使用说明，这里是针对ab的资源引用计数，如果通过ab复制出来的GameObject后，然后不通过本系统直接复制GameObject的话，
//需要在对应的逻辑层自行维护这些GameObjects，只有当这些GameObjects都不需要的时候才能使用本系统的引用计数处理资源本身，
//否则，如果直接操作本引用计数，会导致后来复制的GameObject出现资源丢失的情况
public class AssetBundleInfo
{
    public AssetBundle m_AssetBundle;
    public int m_ReferencedCount;

    public AssetBundleInfo(AssetBundle bundle)
    {
        m_AssetBundle = bundle;
        m_ReferencedCount = 0;
    }
}

public class AssetbundleLoader : MonoBehaviour
{

    //解决UI的资源在美术资源内部的问题
    private string m_ExtraInnerPath = "";
    Dictionary<string, ABReqList> m_LoadRequests = new Dictionary<string, ABReqList>();
    private Dictionary<string, Dictionary<long, int>> m_AssetRefs = new Dictionary<string, Dictionary<long, int>>();
    private AssetBundleManager abm;


    //异步
    private static long m_IdAsync = 1;
    private static long m_AbRefId = 1;
    public void OnDestroy()
    {
        if (abm != null)
        {
            abm.Dispose();
        }
    }
    public void Initialize(string extra, string manifestName, System.Action<bool> initOK)
    {
        Caching.ClearCache();
        m_ExtraInnerPath = extra;
        abm = new AssetBundleManager();

        //这步是abm一个关于版本的设置，这里为了我们实际需要，默认设置。
        if (!PlayerPrefs.HasKey("__abm_manifest_version__"))
        {
            PlayerPrefs.SetInt("__abm_manifest_version__", 1);
        }
        string sdcard_www = MonoTool.GetWebRequestDataPathHeader() + Path.Combine(MonoTool.GetPersistentPath(), m_ExtraInnerPath);
        string install = Path.Combine(MonoTool.GetWWWDataPath(), m_ExtraInnerPath);
        string sdcard = Path.Combine(MonoTool.GetPersistentPath(), m_ExtraInnerPath);
        abm.SetBaseUri(new string[] { install, sdcard_www, sdcard});
        abm.SetCodeConfig(m_ExtraInnerPath);
        abm.Initialize(manifestName, true, (result) =>
        {
            if (initOK != null)
            {
                initOK(result);
            }
        });
        abm.DisableDebugLogging(true);

    }
    public ABRequest LoadPrefabAsync(Type t, string abName, string mainAssetName, Action<ResData, object> func, Action<long, ABRequestResult> errorFunc, object userData = null)
    {
        return LoadAssetAsync(t, abName, mainAssetName, func, errorFunc, userData);
    }
    public ABRequest LoadPrefabAsync(ABRequest request)
    {
        return LoadAssetAsync(request);
    }
    public void LoadLevel(string abName, string assetName, int mode, System.Action func)
    {
        LoadLevelAsset(abName, assetName, mode, func);
    }
    void LoadLevelAsset(string abName, string assetName, int mode, System.Action func)
    {
        ABRequest request = new ABRequest();
        request.assetType = typeof(UnityEngine.SceneManagement.Scene);
        request.mainAssetName = assetName;
        request.levelSharpFunc = func;
        request.LoadIndex = m_IdAsync++;
        request.abName = abName;
        StartCoroutine(OnLoadLevelAssetAsync(abName, mode, func));
    }
    public void UnloadLevel(string abName)
    {
        abm.UnloadBundle(abName, true, true);
    }

    /// <summary>
    /// 载入素材
    /// </summary>
    ABRequest LoadAssetAsync(Type t, string abName, string mainAssetName, Action<ResData, object> action, Action<long, ABRequestResult> errorAction, object userData = null)
    {
        if (string.IsNullOrEmpty(abName) )
        {
            Debug.LogError($"assetbundle abName \"{abName}\" is empty or null");
            return null;
        }
        if (string.IsNullOrEmpty(mainAssetName))
        {
            Debug.LogError($"assetbundle mainAssetName \"{mainAssetName}\" is empty or null");
            return null;
        }
        ABRequest request = new ABRequest();
        request.assetType = t;
        request.mainAssetName = mainAssetName;
        request.sharpFunc = action;
        request.sharpErrorFunc = errorAction;
        request.LoadIndex = m_IdAsync++;
        request.abName = abName;
        request.UserData = userData;


        ABReqList requests = null;
        //Debug.Log("加载：" + abName);
        if (!m_LoadRequests.TryGetValue(abName, out requests))
        {
            //Debug.Log("新加载：" + abName);
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
    ABRequest LoadAssetAsync(ABRequest request)
    {
        request.LoadIndex = m_IdAsync++;

        ABReqList requests = null;
        //Debug.Log("加载：" + abName);
        if (!m_LoadRequests.TryGetValue(request.abName, out requests))
        {
            //Debug.Log("新加载：" + abName);
            requests = new ABReqList();
            requests.m_ABRefId = m_AbRefId++;
            request.abRefId = requests.m_ABRefId;

            requests.m_ABReqs.Add(request);
            m_LoadRequests.Add(request.abName, requests);
            StartCoroutine(OnLoadAsset(request.assetType, request.abName, request.fullPath));
        }
        else
        {
            request.abRefId = requests.m_ABRefId;
            requests.m_ABReqs.Add(request);
        }
        return request;
    }


    IEnumerator OnLoadLevelAssetAsync(string abName, int mode, Action func)
    {
        //为了确保异步，强制添加一个返回
        yield return null;
        string assetName = System.IO.Path.GetFileNameWithoutExtension(abName);
        var bundleRequest = abm.GetBundleAsync(abName, false);
        yield return bundleRequest;
        if (bundleRequest.Failed || bundleRequest.AssetBundle == null)
        {
            Debug.LogError("Load scene assetbundle failed, bundle:" + abName + " scene:" + assetName);
            yield break;
        }
        var bundle = SceneManager.LoadSceneAsync(assetName, (LoadSceneMode) mode);
        while (!bundle.isDone)
        {
            yield return new WaitForEndOfFrame();
        }
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(assetName);
        if (scene != null)
        {
            while (!scene.isLoaded)
            {
                yield return new WaitForEndOfFrame();
            }
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        }
        m_LoadRequests.Remove(abName);
        abm.UnloadBundle(abName, false, false);
        func();
    }

    private Dictionary<string, int> temp = new Dictionary<string, int>();

    public static Action OnLoadYieldReturnNullInsert;
    public static Action OnABloadInsert;
    public static Action OnAssetLoadInsert;
    public static Action OnLoadFinishInsert;

    IEnumerator OnLoadAsset(Type t, string abName, bool fullPath = false)
    {
        //为了确保异步，强制添加一个返回
        yield return null;
        if(OnLoadYieldReturnNullInsert != null)
        {
            Debug.Log("OnLoadYieldReturnNull");
            OnLoadYieldReturnNullInsert();
        }
        ABReqList list = null;
        if (!m_LoadRequests.TryGetValue(abName, out list))
        {
            //Debug.Log("提前释放ab" + abName);
            yield break;
        }

        var bundle_request = abm.GetBundleAsync(abName, fullPath);
        yield return bundle_request;

        if(OnABloadInsert!= null)
        {
            Debug.Log("OnABload");
            OnABloadInsert();
        }

        if (!m_LoadRequests.TryGetValue(abName, out list))
        {
            //Debug.Log("提前释放ab" + abName);
            abm.UnloadBundle(abName, true, false);
            yield break;
        }



        AssetBundle ab = bundle_request.AssetBundle;
        if (ab == null)
        {
            Debug.LogError("Load assetbundle failed, bundle:" + abName);
        }
        for (int i = 0; i < list.m_ABReqs.Count; i++)
        {
            ABRequest loadAssetRequest = list.m_ABReqs[i];
            if(list.m_ABReqs[i] == null)
            {
                continue;
            }
            ResData result = new ResData();
            UnityEngine.Object obj = null;
            if (ab != null)
            {
                AssetBundleRequest request = ab.LoadAssetAsync(loadAssetRequest.mainAssetName, loadAssetRequest.assetType);
                yield return request;
                obj = request.asset;
                if (list.m_ABReqs[i] == null)
                {
                    continue;
                }
            }

            long refId = list.m_ABRefId;
            if (m_AssetRefs.ContainsKey(abName))
            {
                var assetRefs = m_AssetRefs[abName];
                if (assetRefs.ContainsKey(refId))
                {
                    var count = assetRefs[refId];
                    assetRefs[refId] = count + 1;
                }
                else
                {
                    assetRefs.Add(refId, 1);
                }
            }
            else
            {
                Dictionary<long, int> assetRefs = new Dictionary<long, int>();
                assetRefs.Add(refId, 1);
                m_AssetRefs.Add(abName, assetRefs);
            }

            if (obj != null)
            {
                result.m_Obj = obj;
                result.m_Id = loadAssetRequest.LoadIndex;
                if (loadAssetRequest.sharpFunc != null)
                {
                    try
                    {
                        loadAssetRequest.sharpFunc(result, loadAssetRequest.UserData);
                        loadAssetRequest.sharpFunc = null;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("AB：" + loadAssetRequest.abName + " 资源名：" + loadAssetRequest.mainAssetName + "加载后执行sharpFunc函数错误，请检查是否回调函数存在逻辑错误，UI关闭了但是搞忘了释放AB等错误,错误信息：" + e.Message + "  堆栈：" + e.StackTrace);
                    }

                }
            }
            else
            {
                if (loadAssetRequest.sharpErrorFunc != null)
                {
                    try
                    {
                        loadAssetRequest.sharpErrorFunc(loadAssetRequest.LoadIndex, ABRequestResult.LoadError);
                        loadAssetRequest.sharpErrorFunc = null;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("AB：" + loadAssetRequest.abName + " 资源名：" + loadAssetRequest.mainAssetName + "加载后执行sharpErrorFunc函数错误，请检查是否回调函数存在逻辑错误，UI关闭了但是搞忘了释放AB等错误,错误信息：" + e.Message + "  堆栈：" + e.StackTrace);
                    }

                }
                if (!loadAssetRequest.isRelease)
                {
                    UnloadPrefab(loadAssetRequest);
                }
            }

        }

        if (OnAssetLoadInsert != null)
        {
            Debug.Log("OnAssetLoad");
            OnAssetLoadInsert();
        }

        if (!m_LoadRequests.TryGetValue(abName, out list))
        {
            //Debug.Log("提前释放ab" + abName);
            abm.UnloadBundle(abName, true, false);
            yield break;
        }
        else
        {
            var reqs = list.m_ABReqs;
            var count = reqs.Count;
            bool isAllPreUnload = true;
            for(int i = 0; i < count; ++i)
            {
                if (reqs[i] != null)
                {
                    isAllPreUnload = false;
                    break;
                }
            }
            if(isAllPreUnload)
            {
                //Debug.Log("提前释放ab" + abName);
                abm.UnloadBundle(abName, true, false);
            }
        }

        //Debug.Log("加载完成：" + abName);
        m_LoadRequests.Remove(abName);

        if (OnLoadFinishInsert != null)
        {
            Debug.Log("OnLoadFinish");
            OnLoadFinishInsert();
        }
    }

    //编辑器下恢复shader打包和编辑器运行运行时平台不一致导致的shader丢失问题
    public static void RecoveryShader(GameObject go)
    {
        if (go == null)
        {
            return;
        }
        else
        {
            Renderer[] renders =go.GetComponentsInChildren<Renderer>(true);
            if(renders!=null)
            {
                int render_count = renders.Length;
                for (int i = 0; i < render_count; ++i)
                {
                    Renderer render = renders[i];
                    if (render != null)
                    {
                        Material[] mats = render.sharedMaterials;
                        int mat_count = mats.Length;
                        for (int j = 0; j < mat_count; ++j)
                        {
                            Material mat = mats[j];
                            if (mat != null)
                            {
                                var shader = Shader.Find(mat.shader.name);
                                if (shader != null)
                                {
                                    mat.shader = shader;
                                }
                            }
                        }
                    }
                }
            }
        }
    }


    public void UnloadPrefab(ABRequest id)
    {
        if(id == null)
        {
            Debug.LogWarning("id == null " + (id == null));
            return;
        }
        if (id.isRelease)
        {
            return;
        }
        id.isRelease = true;
        if (m_LoadRequests.ContainsKey(id.abName))
        {
            var list = m_LoadRequests[id.abName];
            int count = list.m_ABReqs.Count;
            bool findReq = false;
            for (int i = 0; i < count; ++i)
            {
                var req = list.m_ABReqs[i];
                if (req == null)
                {
                    //Debug.LogWarning("req == null" + id.abName);
                }
                else
                {
                    if (req.LoadIndex == id.LoadIndex)
                    {
                        if(req.sharpErrorFunc != null)
                        {
                            req.sharpErrorFunc(req.LoadIndex, ABRequestResult.Cancel);
                            req.sharpErrorFunc = null;
                        }
                        list.m_ABReqs[i] = null;
                        findReq = true;
                        break;
                    }
                }

            }
            if (!findReq)
            {
                UnloadAssetRef(id);
            }
            //for (int i = 0; i < list.m_ABReqs.Count; ++i)
            //{
            //    if (list.m_ABReqs[i] != null)
            //    {
            //        return;
            //    }
            //}
            //m_LoadRequests.Remove(id.abName);
        }
        else
        {
            UnloadAssetRef(id);
        }
    }

    public bool HasLoading()
    {
        return m_LoadRequests.Count > 0;
    }

    private void UnloadAssetRef(ABRequest id)
    {
        if (m_AssetRefs.ContainsKey(id.abName))
        {
            var refCount = m_AssetRefs[id.abName];
            if (refCount.ContainsKey(id.abRefId))
            {
                int count = refCount[id.abRefId];
                refCount[id.abRefId] = count - 1;
                if (refCount[id.abRefId] <= 0)
                {
                    abm.UnloadBundle(id.abName, true, false);
                    refCount.Remove(id.abRefId);
                }
            }
            else
            {
                //Debug.LogWarning("释放错误，没有该引用：" + id.abName + " " + id.abRefId);
            }
        }
        else
        {
            //Debug.LogWarning("释放错误，没有该引用：" + id.abName + " " + id.abRefId);
        }
    }



    //应该交由业务层在合适的时间调用，例如切换场景前
    public void UnloadUnusedAssets()
    {
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    //异步的，因为资源有可能在网络
    public ABRequest LoadBytesAsync(string resName, Action<byte[]> call)
    {
        resName = MakeFullPath(resName);
        Debug.Log("LoadBytes:" + resName);

        ABRequest ab = null;
        ab = LoadPrefabAsync(typeof(TextAsset), resName, System.IO.Path.GetFileNameWithoutExtension(resName), (rd, ud) =>
        {
            var ta = rd.m_Obj as TextAsset;
            byte[] data = ta.bytes;
            UnloadPrefab(ab);
            call(data);
        }, null);
        return ab;
    }

    public  string MakeFullPath(string strFileName)
    {
        string strFullName = "";
        if (strFileName == null)
        {
            return null;
        }
        strFileName = Path.Combine(m_ExtraInnerPath, strFileName);
        strFullName = Path.Combine(MonoTool.GetPersistentPath(), strFileName);
        
        //对该路径进行检测，如果没有找到，就为他尽量指定一个默认路径
        if (File.Exists(strFullName))
        {
            Debug.Log("file find at manu folder,path is" + strFullName);
            return strFullName;
        }
        else
        {
            Debug.Log("file not find at the path : " + strFullName + " ,using File.Exists for test");
            strFullName = Path.Combine( MonoTool.GetWWWDataPath() , strFileName);
            Debug.Log("change path to system path :" + strFullName);
        }

        Debug.LogWarning("MakeFullPath:" + strFullName);
        return strFullName;
    }
    void OnApplicationQuit()
    {
        Debug.Log("ResourceManagerMono OnApplicationQuit");
    }
    

}
