using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AudioRecordPanel : MainScene
{

    [SerializeField]
    private Button[] buttonImages; // 0:开始, 1:停止, 2:加时, 3:改级


    [SerializeField]
    private AudioSource audioSource; // 用于播放录音的 AudioSource

    [Header("Visualization")]
    [SerializeField]
    private RectTransform visualizerContainer; // 用于放置声波条的容器

    [SerializeField]
    private Scrollbar audioScrollbar; // 用于播放录音的 Scrollbar

    [SerializeField]
    private Text audioScrollbarText; // 用于播放录音的 Scrollbar

    private List<RectTransform> visualizerBars = new List<RectTransform>();
    private bool isRecording = false;
    private const int BAR_COUNT = 20; // 声波条数量

    Dictionary<string, HardWareRemoteControl> hardWareRemoteControlMap;

    private int _curButtonIndex = 0;  //当前选中的按钮索引，0表示开始震动，1表示停止震动，2表示增加震动时间，3表示改变震动等级。

    private float monitorTimer = 0f;
    private float[] spectrumData = new float[64]; // 频谱数据缓存

    void Start()
    {
        hardWareRemoteControlMap = ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).bluetoothOnlyUseMajorController.HardWareRemoteControlMap;

        // 确保有 AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        Debug.Log($"[AudioInit] AudioSource Initialized. Mute: {audioSource.mute}, Volume: {audioSource.volume}, SpatialBlend: {audioSource.spatialBlend}");

        SetVolume(audioSource.volume);

        // 初始化可视化条
        InitVisualizer();
        UpdateHighlight();
    }

    void InitVisualizer()
    {
        if (visualizerContainer == null) return;

        // 清理旧的
        foreach (Transform child in visualizerContainer)
        {
            Destroy(child.gameObject);
        }
        visualizerBars.Clear();

        // 简单的水平布局
        // 假设容器有 HorizontalLayoutGroup 最好，如果没有就简单排列
        // 这里我们手动创建 Image 并添加

        // 确保容器有布局组件，或者我们手动计算
        HorizontalLayoutGroup layout = visualizerContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = visualizerContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }

        for (int i = 0; i < BAR_COUNT; i++)
        {
            GameObject barObj = new GameObject($"Bar_{i}");
            barObj.transform.SetParent(visualizerContainer, false);

            Image img = barObj.AddComponent<Image>();
            img.color = new Color(0, 1, 0, 0.8f); // 绿色半透明
            img.raycastTarget = false;

            RectTransform rt = barObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(10, 5); // 初始高度

            visualizerBars.Add(rt);
        }
    }

    protected override void OnEscapePressed()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[AudioRecordPanel] Playback stopped by Back button.");
            ResetVisualizer(); // 停止后立即复位
        }
        ShowMainPanel(mainPanel.gameObject);
    }

    void Update()
    {
        // 监控音频播放状态
        if (audioSource != null && audioSource.isPlaying)
        {
            monitorTimer += Time.deltaTime;
            if (monitorTimer >= 0.5f) // 每0.5秒打印一次
            {
                monitorTimer = 0f;
                if (audioSource.clip != null)
                {
                    Debug.Log($"[AudioMonitor] Playing... Time: {audioSource.time:F2}s / {audioSource.clip.length:F2}s | Volume: {audioSource.volume}");
                }
            }
        }

        // 更新可视化
        if (isRecording)
        {
            UpdateRecordingVisualizer();
        }
        else if (audioSource != null && audioSource.isPlaying)
        {
            UpdatePlaybackVisualizer();
        }
        else
        {
            ResetVisualizer();
        }
    }

    void UpdateRecordingVisualizer()
    {
        if (visualizerBars.Count == 0) return;

        float time = Time.time * 10f; // 动画速度
        for (int i = 0; i < visualizerBars.Count; i++)
        {
            // 使用 Perlin Noise 模拟自然的波动
            float noise = Mathf.PerlinNoise(time, i * 0.2f);
            float height = 10f + noise * 100f; // 10 ~ 110 高度

            visualizerBars[i].sizeDelta = new Vector2(10, height);
        }
    }

    void UpdatePlaybackVisualizer()
    {
        if (visualizerBars.Count == 0) return;

        // 获取频谱数据
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.Rectangular);

        int step = Mathf.FloorToInt(spectrumData.Length / visualizerBars.Count);
        // 如果 step 为 0 (bar太多)，强制为 1
        if (step < 1) step = 1;

        for (int i = 0; i < visualizerBars.Count; i++)
        {
            // 计算该频段的平均值
            float sum = 0f;
            int count = 0;
            for (int j = 0; j < step; j++)
            {
                int index = i * step + j;
                if (index < spectrumData.Length)
                {
                    sum += spectrumData[index];
                    count++;
                }
            }
            float val = (count > 0) ? sum / count : 0f;

            // 映射高度 (频谱值通常很小，需要放大)
            // 根据实际效果调整系数，这里使用 3000 作为放大倍数
            float targetHeight = 10f + val * 3000f;
            targetHeight = Mathf.Clamp(targetHeight, 10f, 200f);

            // 平滑过渡
            Vector2 currentSize = visualizerBars[i].sizeDelta;
            float newHeight = Mathf.Lerp(currentSize.y, targetHeight, Time.deltaTime * 20f);

            visualizerBars[i].sizeDelta = new Vector2(10, newHeight);
        }
    }

    void ResetVisualizer()
    {
        if (visualizerBars.Count == 0) return;

        // 只有当高度不为初始值时才重置，避免每帧都在设置
        if (visualizerBars[0].sizeDelta.y > 10.1f)
        {
            foreach (var bar in visualizerBars)
            {
                bar.sizeDelta = new Vector2(10, 5);
            }
        }
    }

    protected override void OnButtonOKPressed()
    {
        Debug.Log($"Selected Index: {_curButtonIndex}");
        switch (_curButtonIndex)
        {
            case 0:
                StartAudioRecord();
                break;
            case 1:
                StopAudioRecord();
                break;
            case 2:
                PlayAudioRecord();
                break;
            default:
                break;
        }
    }
    protected override void OnRightArrowPressed()
    {
        _curButtonIndex++;
        if (_curButtonIndex >= buttonImages.Length)
        {
            _curButtonIndex = 0; // Loop back to the first menu
        }
        UpdateHighlight();
    }

    protected override void OnLeftArrowPressed()
    {
        _curButtonIndex--;
        if (_curButtonIndex < 0)
        {
            _curButtonIndex = buttonImages.Length - 1; // Loop back to the last menu
        }
        UpdateHighlight();
    }

    protected override void OnUpArrowPressed()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            SetVolume(audioSource.volume + 0.2f);
        }
    }

    protected override void OnDownArrowPressed()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            SetVolume(audioSource.volume - 0.2f);
        }
    }

    private void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (audioSource != null) audioSource.volume = volume;
        if (audioScrollbar != null) audioScrollbar.value = volume;
        if (audioScrollbarText != null) audioScrollbarText.text = $"当前音量：{(int)(volume * 100)}";
    }



    private void UpdateHighlight()
    {
        if (buttonImages == null) return;

        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (buttonImages[i] != null)
            {
                buttonImages[i].GetComponent<Image>().color = (i == _curButtonIndex) ? Color.green : Color.white;
            }
        }
    }

    public void StartAudioRecord()
    {
        isRecording = true; // 开启可视化
        foreach (var kvp in hardWareRemoteControlMap)
        {
        //    bool isOK = kvp.Value.AudioItem.StartAudioRecording();
        //    Debug.Log($"开始录音设备：{kvp.Key}，结果：{isOK}");
        }

    }

    public void StopAudioRecord()
    {
        isRecording = false; // 关闭可视化
        foreach (var kvp in hardWareRemoteControlMap)
        {
            // kvp.Value.AudioItem.StopAudioRecording();
        }
    }

    public void PlayAudioRecord()
    {
        foreach (var kvp in hardWareRemoteControlMap)
        {
            // string path = Path.Combine(Application.persistentDataPath, kvp.Value.AudioItem.GetRecordingDirectory());
            // bool startAudio = AndroidServerInfoDemo.Instance.PlayAudioFile(path);
            // PlayExternalAudio(path);
        }
    }

    public void PlayExternalAudio(string absolutePath)
    {
        StartCoroutine(LoadAudio(absolutePath));
    }

    IEnumerator LoadAudio(string filePath)
    {
        // 1. 验证文件
        if (!File.Exists(filePath))
        {
            Debug.LogError($"文件不存在: {filePath}");
            yield break;
        }

        // 2. 创建带编码的URI（特别处理空格）
        string uri;

        // Android 上需要用 file:///
        if (Application.platform == RuntimePlatform.Android)
        {
            // 方案A：简单替换空格
            uri = "file://" + filePath.Replace(" ", "%20");

            // 方案B：完全编码
            // uri = "file://" + System.Uri.EscapeDataString(filePath);
        }
        else
        {
            uri = "file://" + filePath;
        }

        Debug.Log($"尝试加载URI: {uri}");

        // 3. 使用 UnityWebRequest
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            // 设置音频流 (本地文件建议关闭 streamAudio 以确保完全加载)
            DownloadHandlerAudioClip downloadHandler = (DownloadHandlerAudioClip)request.downloadHandler;
            downloadHandler.streamAudio = false;
            downloadHandler.compressed = false;

            // 发送请求
            yield return request.SendWebRequest();

            // 检查结果
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"加载失败: {request.error}");

                // 尝试备用方案：直接加载字节
                yield return StartCoroutine(LoadAudioAsBytes(filePath));
                yield break;
            }

            // 获取音频剪辑
            AudioClip clip = downloadHandler.audioClip;
            if (clip == null || clip.length == 0)
            {
                Debug.LogError("加载的音频剪辑为空");
                yield break;
            }

            Debug.Log($"✅ 音频加载成功: {clip.name}, 长度: {clip.length}s, Channels: {clip.channels}, Freq: {clip.frequency}");

            // 播放音频 (使用组件上的 AudioSource)
            if (audioSource != null)
            {
                audioSource.Stop(); // 播放前先停止，防止重叠
                audioSource.clip = clip;
                
                // 强制重置 AudioSource 关键参数，防止被场景中其他设置干扰
                audioSource.spatialBlend = 0f; // 2D 声音
                audioSource.bypassEffects = true;
                audioSource.bypassListenerEffects = true;
                audioSource.bypassReverbZones = true;
                audioSource.pitch = 1f;
                
                SetVolume(1f);
                audioSource.mute = false; // 确保未静音
                audioSource.Play();

                Debug.Log($"[AudioDebug] AudioSource.Play() called. \n" +
                          $"Clip: {audioSource.clip.name} \n" +
                          $"Length: {audioSource.clip.length}s \n" +
                          $"IsPlaying: {audioSource.isPlaying} \n" +
                          $"IsVirtual: {audioSource.bypassListenerEffects} (check)");
            }
            else
            {
                // Fallback
                PlayClipAtPoint(clip, Vector3.zero, 1f);
            }
        }
    }

    IEnumerator LoadAudioAsBytes(string filePath)
    {
        Debug.Log("尝试通过字节加载...");

        byte[] bytes = File.ReadAllBytes(filePath);
        if (bytes.Length < 44) // WAV文件头至少44字节
        {
            Debug.LogError("文件太小，不是有效的WAV文件");
            yield break;
        }

        // 这里需要实现WAV到AudioClip的转换
        // 可以使用第三方库或自己解析WAV格式
        Debug.Log("文件大小: " + bytes.Length + " bytes");
    }

    void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        GameObject gameObject = new GameObject("One shot audio");
        gameObject.transform.position = position;
        AudioSource audioSource = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
        audioSource.clip = clip;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
        audioSource.Play();
        Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
    }

}
