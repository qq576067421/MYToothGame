using GameDll;
using LCL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetBundles
{
    public struct AssetBundleDownloadCommand
    {
        public string BundleName;
        public bool FullPath;
        public Hash128 Hash;
        public uint Version;
        public Action<AssetBundle> OnComplete;
    }

    public class AssetBundleDownloader : ICommandHandler<AssetBundleDownloadCommand>
    {
        private const int MAX_RETRY_COUNT = 3;
        private const float RETRY_WAIT_PERIOD = 1;
        private const int MAX_SIMULTANEOUS_DOWNLOADS = 20;

        private static readonly Hash128 DEFAULT_HASH = default(Hash128);

        private static readonly long[] RETRY_ON_ERRORS = {
            503 // Temporary Server Error
        };

        private string InstalledUrl;
        private string SdCardUrlWWW;
        private string SdCardUrl;
        private string CodeConfig;
        private Action<IEnumerator> coroutineHandler;

        private int activeDownloads = 0;
        private Queue<IEnumerator> downloadQueue = new Queue<IEnumerator>();
        private bool cachingDisabled;

        /// <summary>
        ///     Creates a new instance of the AssetBundleDownloader.
        /// </summary>
        /// <param name="install">Uri to use as the base for all bundle requests.</param>
        public AssetBundleDownloader(string install, string sdcardWWW, string sdcard, string code_config)
        {
            this.InstalledUrl = install;
            this.SdCardUrlWWW = sdcardWWW;
            this.SdCardUrl = sdcard;
            this.CodeConfig = code_config;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                coroutineHandler = EditorCoroutine.Start;
            else
#endif
                coroutineHandler = AssetBundleDownloaderMonobehaviour.Instance.HandleCoroutine;

            if (!this.InstalledUrl.EndsWith("/"))
            {
                this.InstalledUrl += "/";
            }
            if (!this.SdCardUrlWWW.EndsWith("/"))
            {
                this.SdCardUrlWWW += "/";
            }
            if (!this.SdCardUrl.EndsWith("/"))
            {
                this.SdCardUrl += "/";
            }
            if (!string.IsNullOrEmpty(this.CodeConfig) && !this.CodeConfig.EndsWith("/"))
            {
                this.CodeConfig += "/";
            }
        }

        /// <summary>
        ///     Begin handling of a AssetBundleDownloadCommand object.
        /// </summary>
        public void Handle(AssetBundleDownloadCommand cmd)
        {
            publicHandle(Download(cmd, 0));
        }

        private void publicHandle(IEnumerator downloadCoroutine)
        {
            if (activeDownloads < MAX_SIMULTANEOUS_DOWNLOADS)
            {
                activeDownloads++;
                coroutineHandler(downloadCoroutine);
            }
            else
            {
                downloadQueue.Enqueue(downloadCoroutine);
            }
        }
        public static Dictionary<string, string> m_MakeFistPackage = new Dictionary<string, string>();
        public static bool m_OpenFirstPackageCollect = false;
        private IEnumerator Download(AssetBundleDownloadCommand cmd, int retryCount)
        {
            //先检测外面是否有该文件
            string uri = "";
            string httpDownloadBundleName = "";
            if (!cmd.FullPath)
            {
                uri = InstalledUrl + cmd.BundleName;
                var ex_uri_www = this.SdCardUrlWWW + cmd.BundleName;
                var ex_uri = this.SdCardUrl + cmd.BundleName;
                //file exists 不需要协议头
                if (System.IO.File.Exists(ex_uri))
                {
                    //下载需要协议头
                    uri = ex_uri_www;
                }
                else
                {
                    httpDownloadBundleName = cmd.BundleName;
                    if(!string.IsNullOrEmpty(this.CodeConfig))
                    {
                        httpDownloadBundleName = this.CodeConfig + cmd.BundleName;
                    }
                }
            }
            else
            {
                uri = cmd.BundleName;
            }
            if (m_OpenFirstPackageCollect)
            {
                string path = cmd.BundleName;
                if (string.IsNullOrEmpty(this.CodeConfig))
                {
                    path = cmd.BundleName;
                }
                else
                {
                    path = this.CodeConfig + cmd.BundleName;
                }
                if (!m_MakeFistPackage.ContainsKey(path))
                {
                    m_MakeFistPackage.Add(path, path);
                }
            }
            UnityWebRequest req;
            if (cachingDisabled || (cmd.Version <= 0 && cmd.Hash == DEFAULT_HASH))
            {
                if (AssetBundleManager.debugLoggingEnabled) Debug.Log(string.Format("GetAssetBundle [{0}].", uri));
#if UNITY_2018_1_OR_NEWER
                req = UnityWebRequestAssetBundle.GetAssetBundle(uri);
#else
                req = UnityWebRequest.GetAssetBundle(uri);
#endif
            }
            else if (cmd.Hash == DEFAULT_HASH)
            {
                if (AssetBundleManager.debugLoggingEnabled) Debug.Log(string.Format("GetAssetBundle [{0}] v[{1}] [{2}].", Caching.IsVersionCached(uri, new Hash128(0, 0, 0, cmd.Version)) ? "cached" : "uncached", cmd.Version, uri));
#if UNITY_2018_1_OR_NEWER
                req = UnityWebRequestAssetBundle.GetAssetBundle(uri, cmd.Version, 0);
#else
                req = UnityWebRequest.GetAssetBundle(uri, cmd.Version, 0);
#endif
            }
            else
            {
                if (AssetBundleManager.debugLoggingEnabled) Debug.Log(string.Format("GetAssetBundle [{0}] [{1}] [{2}].", Caching.IsVersionCached(uri, cmd.Hash) ? "cached" : "uncached", uri, cmd.Hash));
#if UNITY_2018_1_OR_NEWER
                req = UnityWebRequestAssetBundle.GetAssetBundle(uri, cmd.Hash, 0);
#else
                req = UnityWebRequest.GetAssetBundle(uri, cmd.Hash, 0);
#endif
            }
            float start_load_ab_time = Time.realtimeSinceStartup;
#if UNITY_2017_2_OR_NEWER
            req.SendWebRequest();
#else
            req.Send();
#endif

            while (!req.isDone)
            {
                yield return null;
            }

            byte[] bundle_data = null;
#if UNITY_2017_1_OR_NEWER
            var isNetworkError = req.isNetworkError;
            var isHttpError = req.isHttpError;
#else
            var isNetworkError = req.isError;
            var isHttpError = (req.responseCode < 200 || req.responseCode > 299) && req.responseCode != 0;  // 0 indicates the cached version may have been downloaded.  If there was an error then req.isError should have a non-0 code.
#endif
            float load_ab_use_time = Time.realtimeSinceStartup - start_load_ab_time;
            AssetBundle bundle = null;
            if (isHttpError || isNetworkError || !string.IsNullOrEmpty(req.error))
            {
                if (retryCount < MAX_RETRY_COUNT)
                {
                    Debug.LogError($"重试, 下载实际用时{load_ab_use_time}s  错误码:{req.responseCode}  错误:{req.error} 重试次数{retryCount} 地址:{uri} 等待  [{RETRY_WAIT_PERIOD}] 秒后开始重新...");
                    req.Dispose();
                    activeDownloads--;
                    yield return new WaitForSeconds(RETRY_WAIT_PERIOD);
                    publicHandle(Download(cmd, retryCount + 1));
                    yield break;
                }
                else
                {
                    Debug.LogError($"重试多次，加载或者下载AB失败, 下载实际用时{load_ab_use_time}s  错误码:{req.responseCode}  错误:{req.error} 重试次数{retryCount}  地址:{uri} ");
                }
            }
            else
            {
                try
                {
                    bundle = DownloadHandlerAssetBundle.GetContent(req);
                }
                catch(Exception e)
                {
                    bundle = null;
                    Debug.LogWarning("LCL Error Assetbundle bug:" + e.ToString());
                }

                if (bundle == null)
                {
                    if (cachingDisabled)
                    {
                        Debug.LogError($"加载或者下载AB {uri}后, 得到的Bundle是空的，cachingDisabled = true, 重试次数{retryCount}");
                    }
                    else
                    {
                        Debug.LogError($"加载或者下载AB {uri}后, 得到的Bundle是空的，cachingDisabled = false, 重试次数{retryCount}, 设置cachingDisabled = true， 再次重试");
                        cachingDisabled = true;
                        req.Dispose();
                        activeDownloads--;
                        yield return new WaitForSeconds(RETRY_WAIT_PERIOD);
                        publicHandle(Download(cmd, retryCount + 1));
                        yield break;
                    }
                }

            }
            try
            {
                cmd.OnComplete(bundle);
            }
            finally
            {
                req.Dispose();
                activeDownloads--;
                if (downloadQueue.Count > 0)
                {
                    publicHandle(downloadQueue.Dequeue());
                }
            }
        }
    }
}