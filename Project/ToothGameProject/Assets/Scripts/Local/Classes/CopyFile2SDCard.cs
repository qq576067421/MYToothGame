using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace LCL
{
    /// <summary>
    /// 文件复制组件。
    /// 手动挂载到场景对象后，通过外部字段引用调用。
    /// </summary>
    /// <example>
    /// [SerializeField] private CopyFile2SDCard m_CopyFile2SDCard;
    ///
    /// private void StartCopy()
    /// {
    ///     m_CopyFile2SDCard.CopyFromStreamingAssets(
    ///         "video/demo.mp4",
    ///         "video/demo.mp4",
    ///         OnCopyFinished,
    ///         30.0f,
    ///         2);
    /// }
    ///
    /// private void OnCopyFinished(CopyFile2SDCard.CopyResult result)
    /// {
    ///     Debug.Log(result.m_Success ? "复制成功" : result.m_ErrorMessage);
    /// }
    /// </example>
    public class CopyFile2SDCard : MonoBehaviour
    {
        public enum CopyResultCode
        {
            Success = 0,
            InvalidParam = 1,
            SourceNotFound = 2,
            Timeout = 3,
            RequestFailed = 4,
            IOError = 5,
        }

        public class CopyResult
        {
            public bool m_Success;
            public CopyResultCode m_ResultCode;
            public string m_SourcePath;
            public string m_TargetPath;
            public string m_ErrorMessage;
            public int m_AttemptCount;
            public long m_FileSize;
        }

        public void Copy(
            string sourcePath,
            string targetPath,
            Action<CopyResult> callback,
            float timeoutSeconds = 30.0f,
            int retryCount = 0)
        {
            Debug.Log(string.Format(
                "CopyFile2SDCard: 收到复制请求 source={0} target={1} timeout={2:0.0}s retry={3}",
                sourcePath,
                targetPath,
                timeoutSeconds,
                retryCount));
            StartCoroutine(CopyImp(sourcePath, targetPath, callback, timeoutSeconds, retryCount));
        }

        public void CopyFromStreamingAssets(
            string sourceRelativePath,
            string targetPath,
            Action<CopyResult> callback,
            float timeoutSeconds = 30.0f,
            int retryCount = 0)
        {
            Copy(sourceRelativePath, targetPath, callback, timeoutSeconds, retryCount);
        }

        private IEnumerator CopyImp(
            string sourcePath,
            string targetPath,
            Action<CopyResult> callback,
            float timeoutSeconds,
            int retryCount)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                DispatchResult(
                    callback,
                    BuildResult(false, CopyResultCode.InvalidParam, sourcePath, targetPath, "sourcePath is empty.", 0, 0));
                yield break;
            }

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                DispatchResult(
                    callback,
                    BuildResult(false, CopyResultCode.InvalidParam, sourcePath, targetPath, "targetPath is empty.", 0, 0));
                yield break;
            }

            string resolvedSourcePath = ResolveSourcePath(sourcePath);
            string resolvedTargetPath;
            try
            {
                resolvedTargetPath = ResolveTargetPath(targetPath);
                string targetDirectory = Path.GetDirectoryName(resolvedTargetPath);
                if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
            }
            catch (Exception ex)
            {
                DispatchResult(
                    callback,
                    BuildResult(false, CopyResultCode.IOError, resolvedSourcePath, targetPath, ex.Message, 0, 0));
                yield break;
            }

            string requestUri;
            CopyResult uriError;
            if (!TryBuildRequestUri(resolvedSourcePath, resolvedTargetPath, out requestUri, out uriError))
            {
                Debug.LogError(string.Format(
                    "CopyFile2SDCard: 构建请求地址失败 source={0} resolvedSource={1} target={2} resolvedTarget={3} error={4}",
                    sourcePath,
                    resolvedSourcePath,
                    targetPath,
                    resolvedTargetPath,
                    uriError != null ? uriError.m_ErrorMessage : string.Empty));
                DispatchResult(callback, uriError);
                yield break;
            }

            Debug.Log(string.Format(
                "CopyFile2SDCard: 开始复制 resolvedSource={0} resolvedTarget={1} requestUri={2}",
                resolvedSourcePath,
                resolvedTargetPath,
                requestUri));

            int maxAttemptCount = Mathf.Max(1, retryCount + 1);
            float safeTimeoutSeconds = timeoutSeconds > 0.0f ? timeoutSeconds : 0.0f;
            for (int attempt = 1; attempt <= maxAttemptCount; attempt++)
            {
                Debug.Log(string.Format(
                    "CopyFile2SDCard: 第{0}/{1}次复制 source={2} target={3}",
                    attempt,
                    maxAttemptCount,
                    resolvedSourcePath,
                    resolvedTargetPath));
                DeleteFileIfExists(resolvedTargetPath);

                using (UnityWebRequest request = new UnityWebRequest(requestUri, UnityWebRequest.kHttpVerbGET))
                {
                    request.downloadHandler = new DownloadHandlerFile(resolvedTargetPath);
                    request.disposeDownloadHandlerOnDispose = true;
                    if (safeTimeoutSeconds > 0.0f)
                    {
                        request.timeout = Mathf.Max(1, Mathf.CeilToInt(safeTimeoutSeconds));
                    }

                    UnityWebRequestAsyncOperation asyncOp = request.SendWebRequest();
                    bool isTimeout = false;
                    float startTime = Time.realtimeSinceStartup;
                    while (!asyncOp.isDone)
                    {
                        if (safeTimeoutSeconds > 0.0f && Time.realtimeSinceStartup - startTime >= safeTimeoutSeconds)
                        {
                            isTimeout = true;
                            request.Abort();
                            break;
                        }
                        yield return null;
                    }

                    if (isTimeout)
                    {
                        Debug.LogError(string.Format(
                            "CopyFile2SDCard: 复制超时 source={0} target={1} attempt={2}/{3} timeout={4:0.0}s",
                            resolvedSourcePath,
                            resolvedTargetPath,
                            attempt,
                            maxAttemptCount,
                            safeTimeoutSeconds));
                        DeleteFileIfExists(resolvedTargetPath);
                        if (attempt < maxAttemptCount)
                        {
                            yield return null;
                            continue;
                        }

                        DispatchResult(
                            callback,
                            BuildResult(
                                false,
                                CopyResultCode.Timeout,
                                resolvedSourcePath,
                                resolvedTargetPath,
                                "Copy timeout.",
                                attempt,
                                0));
                        yield break;
                    }

                    if (request.result == UnityWebRequest.Result.Success && File.Exists(resolvedTargetPath))
                    {
                        long fileSize = 0;
                        try
                        {
                            fileSize = new FileInfo(resolvedTargetPath).Length;
                        }
                        catch
                        {
                            fileSize = 0;
                        }

                        DispatchResult(
                            callback,
                            BuildResult(
                                true,
                                CopyResultCode.Success,
                                resolvedSourcePath,
                                resolvedTargetPath,
                                string.Empty,
                                attempt,
                                fileSize));
                        Debug.Log(string.Format(
                            "CopyFile2SDCard: 复制成功 source={0} target={1} attempt={2}/{3} size={4}",
                            resolvedSourcePath,
                            resolvedTargetPath,
                            attempt,
                            maxAttemptCount,
                            fileSize));
                        yield break;
                    }

                    string errorMessage = request.error;
                    CopyResultCode resultCode = CopyResultCode.RequestFailed;
                    if (string.IsNullOrEmpty(errorMessage))
                    {
                        errorMessage = "Copy request failed.";
                    }

                    if (errorMessage.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        errorMessage.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        resultCode = CopyResultCode.SourceNotFound;
                    }

                    Debug.LogError(string.Format(
                        "CopyFile2SDCard: 复制失败 source={0} target={1} attempt={2}/{3} result={4} error={5}",
                        resolvedSourcePath,
                        resolvedTargetPath,
                        attempt,
                        maxAttemptCount,
                        resultCode,
                        errorMessage));
                    DeleteFileIfExists(resolvedTargetPath);
                    if (attempt < maxAttemptCount)
                    {
                        yield return null;
                        continue;
                    }

                    DispatchResult(
                        callback,
                        BuildResult(
                            false,
                            resultCode,
                            resolvedSourcePath,
                            resolvedTargetPath,
                            errorMessage,
                            attempt,
                            0));
                    yield break;
                }
            }
        }

        private string ResolveSourcePath(string sourcePath)
        {
            string normalizedPath = sourcePath.Trim().Replace('\\', '/');
            if (HasUriScheme(normalizedPath) || Path.IsPathRooted(normalizedPath))
            {
                return normalizedPath;
            }

            return Application.streamingAssetsPath.Replace('\\', '/').TrimEnd('/') + "/" + normalizedPath.TrimStart('/');
        }

        private string ResolveTargetPath(string targetPath)
        {
            string normalizedPath = targetPath.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(normalizedPath))
            {
                return normalizedPath;
            }

            return Path.Combine(Application.persistentDataPath, normalizedPath);
        }

        private bool TryBuildRequestUri(
            string resolvedSourcePath,
            string resolvedTargetPath,
            out string requestUri,
            out CopyResult errorResult)
        {
            requestUri = null;
            errorResult = null;
            if (string.IsNullOrWhiteSpace(resolvedSourcePath))
            {
                errorResult = BuildResult(
                    false,
                    CopyResultCode.InvalidParam,
                    resolvedSourcePath,
                    resolvedTargetPath,
                    "resolvedSourcePath is empty.",
                    0,
                    0);
                return false;
            }

            if (HasUriScheme(resolvedSourcePath))
            {
                requestUri = resolvedSourcePath.Replace(" ", "%20");
                return true;
            }

            if (!File.Exists(resolvedSourcePath))
            {
                errorResult = BuildResult(
                    false,
                    CopyResultCode.SourceNotFound,
                    resolvedSourcePath,
                    resolvedTargetPath,
                    "Source file not found.",
                    0,
                    0);
                return false;
            }

            requestUri = new Uri(resolvedSourcePath).AbsoluteUri;
            return true;
        }

        private bool HasUriScheme(string path)
        {
            return path.IndexOf("://", StringComparison.Ordinal) >= 0 || path.StartsWith("jar:file:/", StringComparison.OrdinalIgnoreCase);
        }

        private void DeleteFileIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private CopyResult BuildResult(
            bool success,
            CopyResultCode resultCode,
            string sourcePath,
            string targetPath,
            string errorMessage,
            int attemptCount,
            long fileSize)
        {
            CopyResult result = new CopyResult();
            result.m_Success = success;
            result.m_ResultCode = resultCode;
            result.m_SourcePath = sourcePath;
            result.m_TargetPath = targetPath;
            result.m_ErrorMessage = errorMessage;
            result.m_AttemptCount = attemptCount;
            result.m_FileSize = fileSize;
            return result;
        }

        private void DispatchResult(Action<CopyResult> callback, CopyResult result)
        {
            if (callback != null)
            {
                callback(result);
            }
        }
    }
}
