using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoPlayerScene : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private RawImage rawImage;
    private RenderTexture renderTexture;
    private Text progressText;

    // 真实视频 URL
    private string videoUrl = "https://res1.youdoogo.com/video/1036/S2E1_4.mp4?auth_key=1778306719-3gd4i75cnbnczjco-0-6f05b71f01a7d22e3114149f51432902993be5656d2b477c14653920c7d8031a&response-content-disposition=attachment%3Bfilename%3D%22S2E1_4.mp4%22";

    private const int ProxyPort = 19876;
    private const float SeekSpeed = 20f; // 按住1秒拖动20秒
    private TcpListener tcpListener;
    private Thread listenerThread;
    private volatile bool proxyRunning = false;
    private Coroutine seekTimeoutCoroutine = null;
    private double pendingSeekTime = -1;

    // 进度条拖动相关
    private bool isDragging = false;
    private double dragTime = 0;
    private bool wasPlayingBeforeDrag = false;
    private Coroutine hideProgressBarCoroutine = null;

    // 进度条 UI
    private GameObject progressBarRoot;
    private Image progressBarFill;
    private Text timeText;

    void Start()
    {
        SetupUI();
        SetupProgressBar();
        StartProxy();
        StartCoroutine(DelayedPlay());
        SubscribeInputEvents();
    }

    IEnumerator DelayedPlay()
    {
        yield return null;
        SetupAndPlay("http://127.0.0.1:" + ProxyPort + "/video");
    }

    // ──────────────────────────────────────────────
    // TcpListener 代理（Android 兼容）
    // ──────────────────────────────────────────────
    void StartProxy()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Loopback, ProxyPort);
            tcpListener.Start();
            proxyRunning = true;

            listenerThread = new Thread(ProxyLoop) { IsBackground = true };
            listenerThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("[Proxy] 启动失败: " + e.Message);
        }
    }

    void ProxyLoop()
    {
        while (proxyRunning)
        {
            try
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                Thread t = new Thread(() => HandleTcpClient(client)) { IsBackground = true };
                t.Start();
            }
            catch
            {
                break;
            }
        }
    }

    void HandleTcpClient(TcpClient client)
    {
        try
        {
            using (client)
            using (NetworkStream clientStream = client.GetStream())
            {
                StringBuilder requestSb = new StringBuilder();
                byte[] buf = new byte[4096];
                string rangeHeader = null;

                while (true)
                {
                    int n = clientStream.Read(buf, 0, buf.Length);
                    if (n <= 0) break;
                    requestSb.Append(Encoding.ASCII.GetString(buf, 0, n));
                    if (requestSb.ToString().Contains("\r\n\r\n")) break;
                }

                string request = requestSb.ToString();
                foreach (string line in request.Split('\n'))
                {
                    string trimmed = line.TrimEnd('\r');
                    if (trimmed.StartsWith("Range:", StringComparison.OrdinalIgnoreCase))
                        rangeHeader = trimmed.Substring("Range:".Length).Trim();
                }

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(videoUrl);
                req.UserAgent = "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36";
                req.Referer = "https://res1.youdoogo.com/";
                req.AllowAutoRedirect = true;

                if (!string.IsNullOrEmpty(rangeHeader))
                {
                    string rangeValue = rangeHeader.Replace("bytes=", "");
                    string[] parts = rangeValue.Split('-');
                    long from = long.Parse(parts[0]);
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                        req.AddRange(from, long.Parse(parts[1]));
                    else
                        req.AddRange(from);
                }

                HttpWebResponse resp;
                try
                {
                    resp = (HttpWebResponse)req.GetResponse();
                }
                catch (WebException we)
                {
                    Debug.LogError("[Proxy] 服务器异常: " + we.Message);
                    try { clientStream.Write(Encoding.ASCII.GetBytes("HTTP/1.1 502 Bad Gateway\r\nConnection: close\r\n\r\n"), 0, 41); } catch { }
                    return;
                }

                int statusCode = (int)resp.StatusCode;
                string contentType = resp.ContentType ?? "video/mp4";
                long contentLength = resp.ContentLength;
                string contentRange = resp.Headers["Content-Range"];

                StringBuilder header = new StringBuilder();
                header.AppendFormat("HTTP/1.1 {0} {1}\r\n", statusCode, resp.StatusDescription);
                header.AppendFormat("Content-Type: {0}\r\n", contentType);
                if (contentLength >= 0)
                    header.AppendFormat("Content-Length: {0}\r\n", contentLength);
                if (!string.IsNullOrEmpty(contentRange))
                    header.AppendFormat("Content-Range: {0}\r\n", contentRange);
                header.Append("Accept-Ranges: bytes\r\n");
                header.Append("Connection: close\r\n");
                header.Append("\r\n");

                byte[] headerBytes = Encoding.ASCII.GetBytes(header.ToString());
                clientStream.Write(headerBytes, 0, headerBytes.Length);

                using (Stream src = resp.GetResponseStream())
                {
                    byte[] streamBuf = new byte[65536];
                    int read;
                    while ((read = src.Read(streamBuf, 0, streamBuf.Length)) > 0)
                    {
                        clientStream.Write(streamBuf, 0, read);
                    }
                }
                resp.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Proxy] 处理请求异常: " + e.GetType().Name + " - " + e.Message);
        }
    }

    void StopProxy()
    {
        proxyRunning = false;
        try { tcpListener?.Stop(); } catch { }
    }

    // ──────────────────────────────────────────────
    // UI
    // ──────────────────────────────────────────────
    void SetupUI()
    {
        GameObject canvasGo = new GameObject("VideoCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject rawImageGo = new GameObject("VideoDisplay");
        rawImageGo.transform.SetParent(canvasGo.transform, false);
        rawImage = rawImageGo.AddComponent<RawImage>();
        RectTransform rect = rawImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        renderTexture = new RenderTexture(1920, 1080, 0);
        rawImage.texture = renderTexture;

        GameObject textGo = new GameObject("ProgressText");
        textGo.transform.SetParent(canvasGo.transform, false);
        progressText = textGo.AddComponent<Text>();
        progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        progressText.fontSize = 36;
        progressText.color = Color.white;
        progressText.alignment = TextAnchor.MiddleCenter;
        progressText.text = "正在连接视频流...";
        RectTransform textRect = progressText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.45f);
        textRect.anchorMax = new Vector2(1f, 0.55f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    void SetupProgressBar()
    {
        // 找到 VideoCanvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // 进度条根节点
        progressBarRoot = new GameObject("ProgressBarRoot");
        progressBarRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = progressBarRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.05f, 0.08f);
        rootRect.anchorMax = new Vector2(0.95f, 0.14f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // 进度条背景
        GameObject bgGo = new GameObject("ProgressBarBg");
        bgGo.transform.SetParent(progressBarRoot.transform, false);
        Image bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 进度条填充
        GameObject fillGo = new GameObject("ProgressBarFill");
        fillGo.transform.SetParent(progressBarRoot.transform, false);
        progressBarFill = fillGo.AddComponent<Image>();
        progressBarFill.color = new Color(0.2f, 0.6f, 1f, 1f);
        RectTransform fillRect = progressBarFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // 时间文字
        GameObject timeGo = new GameObject("TimeText");
        timeGo.transform.SetParent(progressBarRoot.transform, false);
        timeText = timeGo.AddComponent<Text>();
        timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timeText.fontSize = 28;
        timeText.color = Color.white;
        timeText.alignment = TextAnchor.MiddleCenter;
        RectTransform timeRect = timeText.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0f, 1f);
        timeRect.anchorMax = new Vector2(1f, 1f);
        timeRect.pivot = new Vector2(0.5f, 0f);
        timeRect.offsetMin = new Vector2(0f, 5f);
        timeRect.offsetMax = new Vector2(0f, 40f);

        // 默认隐藏
        progressBarRoot.SetActive(false);
    }

    void ShowProgressBar()
    {
        if (hideProgressBarCoroutine != null)
        {
            StopCoroutine(hideProgressBarCoroutine);
            hideProgressBarCoroutine = null;
        }
        if (progressBarRoot != null) progressBarRoot.SetActive(true);
    }

    void HideProgressBarDelayed()
    {
        if (hideProgressBarCoroutine != null)
        {
            StopCoroutine(hideProgressBarCoroutine);
        }
        hideProgressBarCoroutine = StartCoroutine(HideProgressBarAfterDelay());
    }

    IEnumerator HideProgressBarAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (progressBarRoot != null) progressBarRoot.SetActive(false);
        hideProgressBarCoroutine = null;
    }

    void UpdateProgressBarUI(double currentTime, double duration)
    {
        if (duration <= 0) return;
        float progress = (float)(currentTime / duration);
        progress = Mathf.Clamp01(progress);

        if (progressBarFill != null)
        {
            RectTransform fillRect = progressBarFill.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(progress, 1f);
        }

        if (timeText != null)
        {
            timeText.text = FormatTime(currentTime) + " / " + FormatTime(duration);
        }
    }

    string FormatTime(double seconds)
    {
        int totalSec = (int)seconds;
        int h = totalSec / 3600;
        int m = (totalSec % 3600) / 60;
        int s = totalSec % 60;
        if (h > 0)
            return string.Format("{0}:{1:D2}:{2:D2}", h, m, s);
        else
            return string.Format("{0:D2}:{1:D2}", m, s);
    }

    // ──────────────────────────────────────────────
    // VideoPlayer
    // ──────────────────────────────────────────────
    void SetupAndPlay(string url)
    {
        GameObject videoGo = new GameObject("VideoPlayer");
        videoPlayer = videoGo.AddComponent<VideoPlayer>();

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        AudioSource audioSource = videoGo.AddComponent<AudioSource>();
        videoPlayer.SetTargetAudioSource(0, audioSource);

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.seekCompleted += OnSeekCompleted;
        videoPlayer.playOnAwake = false;
        videoPlayer.skipOnDrop = true;

        videoPlayer.Prepare();
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        if (progressText != null) progressText.text = "";
        vp.Play();
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("[Video] 播放出错: " + message);
        if (progressText != null) progressText.text = "播放出错: " + message;
    }

    // ──────────────────────────────────────────────
    // 遥控器输入控制
    // ──────────────────────────────────────────────
    void SubscribeInputEvents()
    {
        var input = RemoteControlUnitInputSystemManager.Instance;
        if (input == null) return;
        input.OnButtonOKPressed += OnTogglePause;
    }

    void UnsubscribeInputEvents()
    {
        var input = RemoteControlUnitInputSystemManager.Instance;
        if (input == null) return;
        input.OnButtonOKPressed -= OnTogglePause;
    }

    void OnTogglePause()
    {
        if (videoPlayer == null || isDragging) return;
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            if (progressText != null) progressText.text = "已暂停";
        }
        else
        {
            videoPlayer.Play();
            if (progressText != null) progressText.text = "";
        }
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared) return;

        HandleDragInput();
    }

    void HandleDragInput()
    {
        bool leftHeld = Input.GetKey(KeyCode.LeftArrow);
        bool rightHeld = Input.GetKey(KeyCode.RightArrow);

        // 开始拖动
        if (!isDragging && (leftHeld || rightHeld))
        {
            isDragging = true;
            wasPlayingBeforeDrag = videoPlayer.isPlaying;
            dragTime = videoPlayer.time;
            if (videoPlayer.isPlaying) videoPlayer.Pause();
            ShowProgressBar();
        }

        // 拖动中：持续移动预览时间
        if (isDragging)
        {
            double duration = videoPlayer.length;

            if (rightHeld)
            {
                dragTime += SeekSpeed * Time.deltaTime;
            }
            if (leftHeld)
            {
                dragTime -= SeekSpeed * Time.deltaTime;
            }

            // 限制范围
            if (dragTime < 0) dragTime = 0;
            if (dragTime > duration) dragTime = duration;

            // 更新进度条显示
            UpdateProgressBarUI(dragTime, duration);
        }

        // 松开：执行 seek 并恢复播放
        if (isDragging && !leftHeld && !rightHeld)
        {
            isDragging = false;
            DoSeek(dragTime);
            HideProgressBarDelayed();
        }
    }

    void DoSeek(double targetTime)
    {
        if (seekTimeoutCoroutine != null)
        {
            StopCoroutine(seekTimeoutCoroutine);
            seekTimeoutCoroutine = null;
        }

        pendingSeekTime = targetTime;
        videoPlayer.time = targetTime;
        videoPlayer.Play();

        seekTimeoutCoroutine = StartCoroutine(SeekTimeoutCoroutine(targetTime));
    }

    IEnumerator SeekTimeoutCoroutine(double targetTime)
    {
        yield return new WaitForSeconds(3f);
        if (videoPlayer != null && !videoPlayer.isPlaying)
        {
            // 第一次超时：再尝试 Play 一次
            Debug.LogWarning("[Video] Seek 超时，尝试重新播放");
            videoPlayer.Play();

            yield return new WaitForSeconds(2f);
            if (videoPlayer != null && !videoPlayer.isPlaying)
            {
                // 仍然无法播放，重新 Prepare 恢复连接
                Debug.LogWarning("[Video] 播放恢复失败，重新 Prepare");
                RePrepareAndSeek(targetTime);
            }
        }
        seekTimeoutCoroutine = null;
    }

    void RePrepareAndSeek(double targetTime)
    {
        pendingSeekTime = targetTime;
        videoPlayer.Stop();
        videoPlayer.url = "http://127.0.0.1:" + ProxyPort + "/video";
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnRePrepareCompleted;
        videoPlayer.Prepare();
    }

    void OnRePrepareCompleted(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnRePrepareCompleted;
        vp.prepareCompleted += OnVideoPrepared;

        if (pendingSeekTime >= 0)
        {
            vp.time = pendingSeekTime;
            pendingSeekTime = -1;
        }
        vp.Play();
        Debug.Log("[Video] 重新 Prepare 完成，恢复播放");
    }

    void OnSeekCompleted(VideoPlayer vp)
    {
        if (seekTimeoutCoroutine != null)
        {
            StopCoroutine(seekTimeoutCoroutine);
            seekTimeoutCoroutine = null;
        }
    }

    void OnDestroy()
    {
        UnsubscribeInputEvents();
        StopProxy();
        if (videoPlayer != null) videoPlayer.Stop();
        if (renderTexture != null) { renderTexture.Release(); Destroy(renderTexture); }
    }
}
