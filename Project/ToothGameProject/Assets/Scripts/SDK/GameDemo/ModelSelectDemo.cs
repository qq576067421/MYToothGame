using System.IO;
using System.Collections.Generic;
using YouDooSDK.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModelSelectDemo : RemoteInputBase
{
  private const string LogTag = "[ModelSelectDemo]";

  [SerializeField] private Text TitleText;
  [SerializeField] private Text CameraIndexText;
  [SerializeField] private Text ResolutionHeightText;
  [SerializeField] private Text ResolutionWidthText;

  [SerializeField] private Text InputHeightText;
  [SerializeField] private Text InputWidthText;
  [SerializeField] private Text KptNumText;
  [SerializeField] private Text TypeText;
  [SerializeField] private Text QuantizationText;

  [SerializeField] private Text TipsText;

  [SerializeField] private static AndroidParseDataDemo androidParseDataDemo;
  [SerializeField] private HeadImageController headImageController;
  private int _modeChangeIndex = 0;
  private int _cameraChangeIndex = 0;
  private int _resolutionChangeIndex = 0;

  AndroidModeSelect _androidModeSelect;

  // 保存最新的人脸识别数据的队列，最大长度为 5
  public Queue<YouDooSDKConstants.FaceRecognitionTypeALL> FaceRecognitionAllQueue = new Queue<YouDooSDKConstants.FaceRecognitionTypeALL>();
  private bool _hasLoggedMissingParseDataDemo;
  private bool _hasLoggedMissingHeadImageController;


    public static void SetParseDataDemo(AndroidParseDataDemo demo)
    {
        androidParseDataDemo = demo;
    }

    private void Awake()
    {
        Debug.Log($"{LogTag} Awake scene={gameObject.scene.name} hasAndroidParseDataDemo={androidParseDataDemo != null} hasHeadImageController={headImageController != null}");
        TryBindRuntimeReferences();
        if (AndroidServerInfoDemo.Instance is AndroidServerInfoDemo demoInstance)
        {
            demoInstance.SetModelSelectDemo(this);
            Debug.Log($"{LogTag} Awake 已注册到 AndroidServerInfoDemo");
        }
        else
        {
            Debug.LogWarning($"{LogTag} Awake 未获取到 AndroidServerInfoDemo.Instance");
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Debug.Log($"{LogTag} OnEnable");
        TryBindRuntimeReferences();
    }

    /// <summary>
    /// 添加人脸识别数据到队列，保持最多 5 个元素
    /// </summary>
    public void AddFaceRecognitionAllData(YouDooSDKConstants.FaceRecognitionTypeALL data)
  {
    if (FaceRecognitionAllQueue.Count >= 5)
    {
      FaceRecognitionAllQueue.Dequeue();
    }
    FaceRecognitionAllQueue.Enqueue(data);
  }

  protected override void Start()
  {
    base.Start();
    Debug.Log($"{LogTag} Start hasAndroidModeSelect={AndroidServerInfoDemo.AndroidModeSelect != null}");
    TryBindRuntimeReferences();
#if UNITY_EDITOR
    TextAsset jsonAsset = Resources.Load<TextAsset>("MockData/ModelConfig");
    if (jsonAsset != null)
    {
      AndroidModeSelect androidModeSelect = new AndroidModeSelect();
      androidModeSelect.SetAllModelConfig(jsonAsset.text);
      SetAllModelConfig(androidModeSelect);
    }
    else
    {
      Debug.LogError("MockData/ModelConfig.json 未找到，请检查 Resources 目录");
    }
#endif
    if (AndroidServerInfoDemo.Instance != null)
    {
      // Try to cast to AndroidServerInfoDemo to access the specific method
      var demoInstance = AndroidServerInfoDemo.Instance as AndroidServerInfoDemo;
      if (AndroidServerInfoDemo.AndroidModeSelect != null)
      {
        SetAllModelConfig(AndroidServerInfoDemo.AndroidModeSelect);
      }
    }
    Debug.Log($"{LogTag} Start 完成");
    // RemoteControlUnitInputSystemManager.Instance.SetRemoteControlUnitInputEnabled(true);
    // SetNewButtonImageArray();
    // SetButtonState(true);
  }

  protected override void SubscribeEvent()
  {
    base.SubscribeEvent();
    AndroidServerInfoDemo.OnRecordtips += ShowTipsString;
  }

  protected override void UnSubscribeEvent()
  {
    base.UnSubscribeEvent();
    AndroidServerInfoDemo.OnRecordtips -= ShowTipsString;
  }


  protected override void GroupButton1Change()
  {
    Debug.Log($"{LogTag} GroupButton1Change selectIndex={_curSelcetIndex}");
    if (_androidModeSelect == null || _androidModeSelect.AllConfig == null)
    {
      Debug.LogError("GroupButton1Change: _androidModeSelect or AllConfig is null");
      return;
    }

    ModeSelectopupData popupModelData = new ModeSelectopupData
    {
      confirmText = RenderAPI.GetTextByLanId("sdk_demo_confirm"),
      cancelText = RenderAPI.GetTextByLanId("sdk_demo_cancel"),
      onConfirm = OnConfirmCallback,
      onCancel = OnCancelCallback,
    };
    switch (_curSelcetIndex)
    {
      case 0:
        if (_androidModeSelect.AllConfig.modelList != null)
        {
          popupModelData.title = RenderAPI.GetTextByLanId("sdk_demo_change_model");
          popupModelData.modelConfigData = _androidModeSelect.AllConfig.modelList;
          popupModelData.bgColor = new Color(0, 219, 255);
        }
        else
        {
          Debug.LogError("GroupButton1Change: modelList is null");
          return;
        }
        break;
      case 1:
        if (_androidModeSelect.AllConfig.camList != null)
        {
          popupModelData.title = RenderAPI.GetTextByLanId("sdk_demo_change_camera");
          popupModelData.cameraConfigData = _androidModeSelect.AllConfig.camList;
          popupModelData.bgColor = new Color(0, 255, 98);
        }
        else
        {
          Debug.LogError("GroupButton1Change: camList is null");
          return;
        }
        break;
      case 2:
        if (_androidModeSelect.AllConfig.camList != null &&
            _cameraChangeIndex >= 0 &&
            _cameraChangeIndex < _androidModeSelect.AllConfig.camList.Length &&
            _androidModeSelect.AllConfig.camList[_cameraChangeIndex].resolutions != null)
        {
          popupModelData.title = RenderAPI.GetTextByLanId("sdk_demo_change_resolution");
          popupModelData.resolutionData = _androidModeSelect.AllConfig.camList[_cameraChangeIndex].resolutions;
          popupModelData.bgColor = new Color(255, 112, 0);
        }
        else
        {
          Debug.LogError($"GroupButton1Change: Invalid camera index {_cameraChangeIndex} or resolutions is null");
          return;
        }
        break;
      case 3:
        if (!TryBindAndroidParseDataDemo())
        {
          return;
        }
        androidParseDataDemo.IsChangeMode = true;
        int curPlayerCount = androidParseDataDemo.CurPlayerCount;
        curPlayerCount++;
        if (curPlayerCount > 4)
        {
          curPlayerCount = 1;
        }
        Debug.Log($"{LogTag} GroupButton1Change 切换玩家数量 old={androidParseDataDemo.CurPlayerCount} new={curPlayerCount} viewMode={PlayerMatchView.Instance.PlayerMatchViewMode}");
        androidParseDataDemo.InitGameInfo(curPlayerCount, PlayerMatchView.Instance.PlayerMatchViewMode);
        break;
      case 4:
        if (!TryBindAndroidParseDataDemo())
        {
          return;
        }
        androidParseDataDemo.IsChangeMode = true;
        PlayerMatchViewMode playerMatchViewMode = PlayerMatchView.Instance.PlayerMatchViewMode;
        playerMatchViewMode = (PlayerMatchViewMode)((int)playerMatchViewMode + 1);
        if ((int)playerMatchViewMode >= (int)PlayerMatchViewMode.Length)
        {
          playerMatchViewMode = PlayerMatchViewMode.FullView;
        }
        Debug.Log($"{LogTag} GroupButton1Change 切换显示模式 playerCount={androidParseDataDemo.CurPlayerCount} newViewMode={playerMatchViewMode}");
        androidParseDataDemo.InitGameInfo(androidParseDataDemo.CurPlayerCount, playerMatchViewMode);
        break;
      case 5:
        Debug.Log("161 161 161 点击确定按钮 切换模型参数了");
        SetDefaultModelConfig(false);
        break;
    }
    if (_curSelcetIndex < 3)
    {
      UnSubscribeEvent();
      CreatNeedPopup(popupModelData);
    }
  }


  protected override void GroupButton2Change()
  {

    switch (_curSelcetIndex)
    {
      case 0:
        SceneManager.LoadScene(DemoUtil.SceneName.BluetoothScene.ToString());
        break;
      case 1:
        SceneManager.LoadScene(DemoUtil.SceneName.NFCScene.ToString());
        break;
      case 2:
        SceneManager.LoadScene(DemoUtil.SceneName.RolePayScene.ToString());
        break;
      case 3:
        SceneManager.LoadScene(DemoUtil.SceneName.GyroScopeScene.ToString());
        break;
    }
  }

  protected override void GroupButton3Change()
  {
    switch (_curSelcetIndex)
    {
      case 0:
        AndroidServerInfoDemo.Instance.TestStartRecorderInfo();
        break;
      case 1:
        Debug.Log($"[ModelSelectDemo] GroupButton3Change: 触发查询签名URL");
        AndroidServerInfoDemo.Instance.QuerySignUrl(new string[] {
                                        "/video/1032/bk0001_01.mp4",
                                        "/video/1032/bk0001_02.mp4",
                                        "/video/1032/bk0001_03.mp4",
                                        "/video/1032/bk0001_04.mp4",
                                        "/video/1032/bk0001_05.mp4",
                                        "/video/1032/bk0001_06.mp4",
                                        "/video/1032/bk0001_07.mp4",
                                        "/video/1032/bk0001_08.mp4",
                                        "/video/1032/bk0001_09.mp4",
                                        "/video/1032/bk0001_10.mp4",
                                        "/video/1032/bk0001_11.mp4"  });
        break;
      case 2:
        AndroidServerInfoDemo.Instance.ShowToast(RenderAPI.GetTextByLanId("sdk_demo_test_toast"));
        break;
      case 3:
        YouDooSDKConstants.AccountInfo accountInfo = PlayerRoleManager.Instance.GetAccountInfo();
        long[] userIds = new long[accountInfo.users.Count];
        for (int i = 0; i < accountInfo.users.Count; i++)
        {
          userIds[i] = accountInfo.users[i].userId;
        }
        Debug.Log($"[ModelSelectDemo] GroupButton3Change: 触发获取角色信息，userIds={string.Join(", ", userIds)}");
        AndroidServerInfoDemo.Instance.GetRoleInfo(userIds);
        break;
    }
  }

  protected override void GroupButton4Change()
  {
    switch (_curSelcetIndex)
    {
      case 0:
        string path = Path.Combine(Application.streamingAssetsPath, "/storage/emulated/0/Android/data/com.UnityTechnologies.Mobile3DTemplate/files/abcdefg/recording_Skyworth Remote_20251211_142927.wav");
        bool startAudio = AndroidServerInfoDemo.Instance.PlayAudioFile(path);
        Debug.Log($"236 236 236 开始播放音频！！！{startAudio}--{path}");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_audio_play_start", startAudio));
        break;
      case 1:
        AndroidServerInfoDemo.Instance.StopAudioPlayback();
        Debug.Log($"236 236 236 停止播放音频！！！");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_audio_play_stop"));
        break;
      case 2:
        AndroidServerInfoDemo.Instance.PauseAudioPlayback();
        Debug.Log($"236 236 236 暂停播放音频！！！");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_audio_play_pause"));
        break;
      case 3:
        AndroidServerInfoDemo.Instance.ResumeAudioPlayback();
        Debug.Log($"236 236 236 恢复播放音频！！！");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_audio_play_resume"));
        break;
      case 4:
        bool isPlaying = AndroidServerInfoDemo.Instance.IsAudioPlaying();
        Debug.Log($"236 236 236 音频是否正在播放！！！{isPlaying}");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_audio_is_playing", isPlaying));
        break;
      case 5:
        string audioPath = AndroidServerInfoDemo.Instance.GetCurrentPlaybackFilePath();
        Debug.Log($"236 236 236 音频路径为{audioPath}");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_audio_path"));
        break;
      case 6:
        YouDooSDKConstants.AudioPlayState audioPlayState = AndroidServerInfoDemo.Instance.GetAudioPlaybackState();
        Debug.Log($"236 236 236 获取音频播放器的当前状态 {audioPlayState}");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_audio_state"));
        break;
      case 7:
        int progess = AndroidServerInfoDemo.Instance.GetCurrentPlaybackProgress();
        Debug.Log($"236 236 236 获取当前播放进度百分比 {progess}");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_audio_progress"));
        break;
      case 8:
        AndroidServerInfoDemo.Instance.OnDestroyAudioPlayer();
        Debug.Log($"236 236 236 释放资源");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_audio_release"));
        break;
    }
  }

  protected override void GroupButton5Change()
  {
    Debug.Log("[截屏] 236 236 236 截屏的触发！！！！！！！！！！！！");
    switch (_curSelcetIndex)
    {
      case 0:

        AndroidServerInfoDemo.Instance.CaptureScreenshot();
        break;
      case 1:
        AndroidServerInfoDemo.Instance.GenerateVideoFromImages();
        break;
      case 2:
        AndroidServerInfoDemo.Instance.CancelVideoGeneration();
        break;
      case 3:
        AndroidServerInfoDemo.Instance.CaptureClearTempFiles();
        break;
      case 4:
        bool isGenerating = AndroidServerInfoDemo.Instance.IsGeneratingVideo();
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_video_is_generating", isGenerating));
        break;
      case 5:
        bool isCapturing = AndroidServerInfoDemo.Instance.IsCapturing();
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_screenshot_is_capturing", isCapturing));
        break;
      case 6:
        AndroidServerInfoDemo.Instance.CancelCapture();
        break;
    }
  }

  protected override void GroupButton6Change()
  {
    Debug.Log($"[屏幕录制] 236 236 236 屏幕录制的触发！！！！！！！！！！！！");
    switch (_curSelcetIndex)
    {
      case 0:
        Debug.Log($"[屏幕录制] 开始录制屏幕！！");
        AndroidServerInfoDemo.Instance.StartScreenRecording();
        break;
      case 1:
        Debug.Log($"[屏幕录制] 停止录制屏幕！！");
        AndroidServerInfoDemo.Instance.StopScreenRecording();
        break;
      case 2:
        Debug.Log($"[屏幕录制] 保存录制的屏幕！！");
        AndroidServerInfoDemo.Instance.SaveRecording();
        break;
      case 3:
        Debug.Log($"[屏幕录制] 删除录制的屏幕！！");
        AndroidServerInfoDemo.Instance.DeleteRecording();
        break;
      case 4:
        Debug.Log($"[屏幕录制] 清理存储的临时文件！！");
        AndroidServerInfoDemo.Instance.RecorderClearTempFiles();
        break;
      case 5:
        Debug.Log($"[屏幕录制] 请求悬浮窗权限！！");
        AndroidServerInfoDemo.Instance.RequestOverlayPermission();
        break;
      case 6:
        Debug.Log($"[屏幕录制] 停止前台的服务！！");
        AndroidServerInfoDemo.Instance.StopForegroundService();
        break;
      case 7:
        bool issRecording = AndroidServerInfoDemo.Instance.IsRecording();
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_recording_is_recording", issRecording));
        break;
      case 8:
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_method_deprecated"));
        break;
      case 9:
        Debug.Log($"[屏幕录制] 获得当前的视频的文件路径！！{AndroidServerInfoDemo.Instance.GetCurrentTempVideoFile()}");
        break;
    }
  }

  private int _faceRecognitionType = 0;

  private int _checkTooCloseCounterInterval = 0;
  private int _setMaximalArea = 0;
  private int _setFaceRecognitionThreshold = 0;

  private bool _isOpenCheckTooClose = true;
  protected override void GroupButton7Change()
  {
    Debug.Log($"[人脸识别] 401 401 401 人脸识别的按钮！！！！！！！！！！！！");
    switch (_curSelcetIndex)
    {
      case 0:
        Debug.Log($"[人脸识别] 开启人脸识别！！");
        AndroidServerInfoDemo.Instance.StartFaceRecognitionCurrentFrameInfo();
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_recognition_start"));
        break;
      case 1:
        Debug.Log($"[人脸识别] 关闭人脸识别！！");
        AndroidServerInfoDemo.Instance.StopFaceRecognitionCurrentFrameInfo();//本demo需要停止人脸识别 
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_recognition_stop"));
        break;
      case 2:
        Debug.Log($"[人脸识别] 切换人脸识别输出数据！！");
        switch (_faceRecognitionType)
        {
          case 0:
            AndroidServerInfoDemo.Instance.SetYouDooNotifyFaceRecognitionType(YouDooSDKConstants.YouDooNotifyFaceRecognitionType.FRT_USER_ONLY);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_output_user"));
            break;
          case 1:
            AndroidServerInfoDemo.Instance.SetYouDooNotifyFaceRecognitionType(YouDooSDKConstants.YouDooNotifyFaceRecognitionType.FRT_ALL);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_output_all"));
            break;
          case 2:
            AndroidServerInfoDemo.Instance.SetYouDooNotifyFaceRecognitionType(YouDooSDKConstants.YouDooNotifyFaceRecognitionType.FRT_MINIMALIST);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_output_minimal"));
            break;
        }
        _faceRecognitionType++;
        if (_faceRecognitionType > 2)
        {
          _faceRecognitionType = 0;
        }
        break;
      case 3:
        switch (_setFaceRecognitionThreshold)
        {
          case 0:
            AndroidServerInfoDemo.Instance.SetFaceRecognitionThreshold(0.5f);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_threshold", 0.5f));
            break;
          case 1:
            AndroidServerInfoDemo.Instance.SetFaceRecognitionThreshold(0.8f);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_threshold", 0.8f));
            break;
          case 2:
            AndroidServerInfoDemo.Instance.SetFaceRecognitionThreshold(0.9f);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_threshold", 0.9f));
            break;
        }
        _setFaceRecognitionThreshold++;
        if (_setFaceRecognitionThreshold > 2)
        {
          _setFaceRecognitionThreshold = 0;
        }
        break;
      case 4:
        if (FaceRecognitionAllQueue != null && FaceRecognitionAllQueue.Count > 0)
        {
          var firstItem = FaceRecognitionAllQueue.Peek();
          var lastItem = new YouDooSDKConstants.FaceRecognitionTypeALL();
          foreach (var item in FaceRecognitionAllQueue)
          {
            lastItem = item;
          }
          if (firstItem.faceFeatureID != null && lastItem.faceFeatureID != null)
          {
            float sim = AndroidServerInfoDemo.Instance.GetFaceInfoCosSim(firstItem.faceFeatureID, lastItem.faceFeatureID);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_similarity_a", sim));
          }
          else
          {
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_feature_not_enough"));
          }
        }
        else
        {
          TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_queue_empty"));
        }
        break;
      case 5:
        if (FaceRecognitionAllQueue != null && FaceRecognitionAllQueue.Count > 0)
        {
          var firstItem = FaceRecognitionAllQueue.Peek();
          var lastItem = new YouDooSDKConstants.FaceRecognitionTypeALL();
          foreach (var item in FaceRecognitionAllQueue)
          {
            lastItem = item;
          }
          if (firstItem.faceFeatureID != null && lastItem.faceFeatureID != null)
          {
            bool simBool = AndroidServerInfoDemo.Instance.CheckFaceInfoCosSim(firstItem.faceFeatureID, lastItem.faceFeatureID);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_similarity_b", simBool));
          }
          else
          {
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_feature_not_enough"));
          }
        }
        else
        {
          TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_queue_empty"));
        }
        break;
      case 6:
        if (_isOpenCheckTooClose)
        {
          TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_distance_check_close"));
          _isOpenCheckTooClose = false;
        }
        else
        {
          TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_distance_check_open"));
          _isOpenCheckTooClose = true;
        }
        AndroidServerInfoDemo.Instance.NeedCheckTooClose(_isOpenCheckTooClose);
        break;
      case 7:
        switch (_setMaximalArea)
        {
          case 0:
            AndroidServerInfoDemo.Instance.SetMaximalArea(0.7f);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_area_threshold", 0.7f));
            break;
          case 1:
            AndroidServerInfoDemo.Instance.SetMaximalArea(0.5f);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_area_threshold", 0.5f));
            break;
          case 2:
            AndroidServerInfoDemo.Instance.SetMaximalArea(0.4f);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_area_threshold", 0.4f));
            break;
        }
        _setMaximalArea++;
        if (_setMaximalArea > 2)
        {
          _setMaximalArea = 0;
        }

        break;
      case 8:

        switch (_checkTooCloseCounterInterval)
        {
          case 0:
            AndroidServerInfoDemo.Instance.CheckTooCloseCounterInterval(30);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_check_frame_interval", 30));
            break;
          case 1:
            AndroidServerInfoDemo.Instance.CheckTooCloseCounterInterval(50);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_check_frame_interval", 50));
            break;
          case 2:
            AndroidServerInfoDemo.Instance.CheckTooCloseCounterInterval(60);
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_check_frame_interval", 60));
            break;
        }
        _checkTooCloseCounterInterval++;
        if (_checkTooCloseCounterInterval > 2)
        {
          _checkTooCloseCounterInterval = 0;
        }

        break;
      case 9:
        Debug.Log($"[人脸识别] 进入子导航模式");
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_enter_sub_navigation"));
        EnterSubNavigation();
        OnEnterFaceSubNav();
        break;
    }
  }

  #region 人脸识别子导航

  /// <summary>
  /// 进入人脸识别子导航时的初始化逻辑
  /// 激活 HeadImageController 的选角模式
  /// </summary>
  private void OnEnterFaceSubNav()
  {
    TryBindRuntimeReferences();
    Debug.Log($"{LogTag} OnEnterFaceSubNav hasHeadImageController={headImageController != null} hasAndroidParseDataDemo={androidParseDataDemo != null}");
    if (headImageController != null)
    {
      headImageController.gameObject.SetActive(true);
      headImageController.EnterSelectionMode();
    }
    else
    {
      Debug.LogWarning("[SubNav-人脸识别] headImageController 未赋值，无法进入选角模式");
      TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_head_image_missing"));
    }
  }

  protected override void SubNavOnUpPressed()
  {
    Debug.Log($"[SubNav-人脸识别] Up pressed");
    // 上键预留，暂无操作
  }

  protected override void SubNavOnDownPressed()
  {
    Debug.Log($"[SubNav-人脸识别] Down pressed");
    // 下键预留，暂无操作
  }

  protected override void SubNavOnLeftPressed()
  {
    Debug.Log($"[SubNav-人脸识别] Left pressed - 左移槽位");
    if (headImageController != null)
    {
      headImageController.MoveSelectionLeft();
    }
  }

  protected override void SubNavOnRightPressed()
  {
    Debug.Log($"[SubNav-人脸识别] Right pressed - 右移槽位");
    if (headImageController != null)
    {
      headImageController.MoveSelectionRight();
    }
  }

  protected override void SubNavOnOKPressed()
  {
    Debug.Log($"[SubNav-人脸识别] OK pressed - 确认选角");
    if (headImageController != null)
    {
      headImageController.ConfirmSelection();
    }
  }

  protected override void SubNavOnEscapePressed()
  {
    Debug.Log("[SubNav-人脸识别] 退出子导航，返回主导航");
    if (headImageController != null)
    {
      headImageController.ExitSelectionMode();
      headImageController.gameObject.SetActive(false);
    }
    base.SubNavOnEscapePressed(); // 调用 ExitSubNavigation()
  }

  #endregion

  private void CreatNeedPopup<T>(T needData)
  {
    PopupManager.Instance.ShowPopup<ChangeModePopup, T>(
    "ChangeModePopup", // 预制体名称
    needData,
    onCreated: (popup) =>
    {
      Debug.Log("确认对话框创建完成");
    },
    onShow: (popup) =>
    {
      Debug.Log("确认对话框显示完成");
    }
    );
  }

  protected override void EscapePressedBase()
  {
    // _modeChangeIndex = 0;
    // _cameraChangeIndex = 0;
    // _resolutionChangeIndex = 0;
    // SetDefaultModelConfig(); 
  }


  private void OnConfirmCallback(int index)
  {
    SubscribeEvent();
    switch (_curSelcetIndex)
    {
      case 0:
        _modeChangeIndex = index;
        break;
      case 1:
        _cameraChangeIndex = index;
        break;
      case 2:
        _resolutionChangeIndex = index;
        break;
    }
  }

  private void OnCancelCallback()
  {
    SubscribeEvent();
  }

  private void TryBindRuntimeReferences()
  {
    bool hasParseDataDemo = TryBindAndroidParseDataDemo();
    bool hasHeadImageController = TryBindHeadImageController();
    Debug.Log($"{LogTag} TryBindRuntimeReferences hasAndroidParseDataDemo={hasParseDataDemo} hasHeadImageController={hasHeadImageController}");
  }

  private bool TryBindAndroidParseDataDemo()
  {
    if (androidParseDataDemo != null)
    {
      _hasLoggedMissingParseDataDemo = false;
      return true;
    }

    androidParseDataDemo = FindSceneObject<AndroidParseDataDemo>();
    if (androidParseDataDemo == null)
    {
      if (!_hasLoggedMissingParseDataDemo)
      {
        Debug.LogWarning($"{LogTag} 场景中未找到 AndroidParseDataDemo，相关功能暂不可用");
        _hasLoggedMissingParseDataDemo = true;
      }
      return false;
    }

    _hasLoggedMissingParseDataDemo = false;
    Debug.Log($"{LogTag} TryBindAndroidParseDataDemo 成功 name={androidParseDataDemo.name} scene={androidParseDataDemo.gameObject.scene.name}");
    return true;
  }

  private bool TryBindHeadImageController()
  {
    if (headImageController == null)
    {
      headImageController = FindSceneObject<HeadImageController>();
    }

    if (headImageController == null)
    {
      if (!_hasLoggedMissingHeadImageController)
      {
        Debug.LogWarning($"{LogTag} 场景中未找到 HeadImageController");
        _hasLoggedMissingHeadImageController = true;
      }
      return false;
    }

    _hasLoggedMissingHeadImageController = false;
    if (headImageController.parseDataDemo == null && androidParseDataDemo != null)
    {
      headImageController.parseDataDemo = androidParseDataDemo;
      Debug.Log($"{LogTag} TryBindHeadImageController 已回填 parseDataDemo");
    }

    Debug.Log($"{LogTag} TryBindHeadImageController 成功 name={headImageController.name} scene={headImageController.gameObject.scene.name} hasParseDataDemo={headImageController.parseDataDemo != null}");
    return true;
  }

  private T FindSceneObject<T>() where T : Component
  {
    T[] objects = Resources.FindObjectsOfTypeAll<T>();
    for (int i = 0; i < objects.Length; i++)
    {
      if (objects[i] != null && objects[i].gameObject.scene.IsValid())
      {
        return objects[i];
      }
    }

    return null;
  }

  public void SetAllModelConfig(AndroidModeSelect androidModeSelect)
  {
    _androidModeSelect = androidModeSelect;
    Debug.Log($"{LogTag} SetAllModelConfig 完成 modelCount={_androidModeSelect.AllConfig.modelList.Length} cameraCount={_androidModeSelect.AllConfig.camList.Length}");
    SetDefaultModelConfig(true);
    RemoteControlUnitInputSystemManager.Instance.SetRemoteControlUnitInputEnabled(true);
    _groupSelectIndex = 0;
    _curSelcetIndex = 0;
    SetNewButtonImageArray();
    SetButtonState(true);
  }


  private void ShowTipsString(string tips)
  {
    TipsText.text = tips;
  }

  private void SetDefaultModelConfig(bool isInit)
  {
    if (_androidModeSelect.AllConfig == null)
    {
      Debug.LogError("模型配置未初始化，请先调用 SetAllModelConfig 方法。");
      return;
    }
    if (_modeChangeIndex >= _androidModeSelect.AllConfig.modelList.Length)
    {
      _modeChangeIndex = 0;
    }
    if (_cameraChangeIndex >= _androidModeSelect.AllConfig.camList.Length)
    {
      _cameraChangeIndex = 0;
    }

    if (_resolutionChangeIndex >= _androidModeSelect.AllConfig.camList[_cameraChangeIndex].resolutions.Length)
    {
      _resolutionChangeIndex = 0;
    }
    _modeChangeIndex = 1;

    Debug.Log($"看一下现在的三个参数: {_modeChangeIndex}------{_cameraChangeIndex}------{_resolutionChangeIndex}");
    GameServiceConfig selectModel;
    if (isInit)
    {
      selectModel = _androidModeSelect.SetNeedModelConfig(new string[] { "yolov8n_pose_int16_w512xh288_17_20251231.adla" }, "0", 2160, 3840);
    }
    else
    {
      selectModel = _androidModeSelect.SetNeedModelConfig(_modeChangeIndex, _cameraChangeIndex, _resolutionChangeIndex);
    }

    if (selectModel != null)
    {
      InitTitle(_androidModeSelect.AllConfig.modelList.Length, _androidModeSelect.AllConfig.camList.Length, _androidModeSelect.AllConfig.camList[_cameraChangeIndex].resolutions.Length,
  _cameraChangeIndex, selectModel.camList[0].resolutions[0].height, selectModel.camList[0].resolutions[0].width,
  selectModel.modelList[0].inputHeight, selectModel.modelList[0].inputWidth, selectModel.modelList[0].kptNum,
  selectModel.modelList[0].type, selectModel.modelList[0].name);
    }
    else
    {
      Debug.LogError("SetDefaultModelConfig: selectModel is null");
    }
  }

  private void InitTitle(int modelCount, int camListCount, int resolutionCount,
  int cameraIdx, int resolutionHeight, int resolutionWidth,
  int inputHeight, int inputWidth, int kptNum, string type, string quantization)
  {
    TitleText.text = RenderAPI.GetTextByLanId("sdk_demo_model_title", modelCount, camListCount, resolutionCount);
    CameraIndexText.text = RenderAPI.GetTextByLanId("sdk_demo_camera_index", cameraIdx);
    ResolutionHeightText.text = RenderAPI.GetTextByLanId("sdk_demo_resolution_height", resolutionHeight);
    ResolutionWidthText.text = RenderAPI.GetTextByLanId("sdk_demo_resolution_width", resolutionWidth);

    InputHeightText.text = RenderAPI.GetTextByLanId("sdk_demo_input_height", inputHeight);
    InputWidthText.text = RenderAPI.GetTextByLanId("sdk_demo_input_width", inputWidth);
    KptNumText.text = RenderAPI.GetTextByLanId("sdk_demo_kpt_num", kptNum);
    TypeText.text = RenderAPI.GetTextByLanId("sdk_demo_type", type);
    QuantizationText.text = RenderAPI.GetTextByLanId("sdk_demo_name", quantization);

  }
}
