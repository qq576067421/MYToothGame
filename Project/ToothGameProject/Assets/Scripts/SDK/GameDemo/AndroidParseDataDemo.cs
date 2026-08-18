using GameDll;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
using YouDooSDK.UI;
using static YouDooSDKConstants;

//业务侧的代码：
public class AndroidParseDataDemo : AndroidParseData
{

    public static AndroidParseDataDemo Instance { get; private set; }

    private const string LogTag = "[AndroidParseDataDemo]";
    private const string PrepareRootPath = "prepare";
    private const string PrepareTextPath = "prepare/txt_prepare";
    private const string PrepareNoPreparePath = "prepare/noPrepare";
    private const string PrepareFillPath = "PlayerPre/Fill";


    #region 公共结构体
    /// <summary>
    /// 单个座位玩家当前帧的运行时状态快照
    /// </summary>
    public struct PlayerRuntimeState
    {
        public bool isValid;
        public int playerId;
        public int poseType;
        public int leftHandType;
        public int rightHandType;
        public float score;
        public float leftHandScore;
        public float rightHandScore;
        public float rotationOffset;
        public float normalizedX;
        public float left;
        public float top;
        public float right;
        public float bottom;
        public float[,] keyPointListPose;
    }

    public enum PrepareMatchStep
    {
        Empty,
        WaitCenter,
        FaceRecognizing,
        WaitRaiseHand,
        Ready
    }

    public sealed class PrepareMatchSeatState
    {
        public int m_SeatId;
        public int m_SdkSlotId;
        public int m_PersonId;
        public long m_UserId;
        public string m_FacePhotoPath;
        public PrepareMatchStep m_Step;
        public float m_StateStartTime;

        public void Reset(int seatId)
        {
            m_SeatId = seatId;
            m_SdkSlotId = seatId;
            m_PersonId = PersonIdNull;
            m_UserId = 0;
            m_FacePhotoPath = null;
            m_Step = PrepareMatchStep.Empty;
            m_StateStartTime = Time.time;
        }
    }
    #endregion

    #region 事件定义
    /// <summary>
    /// 每帧数据吐出去
    /// </summary>
    public event Action onFrameInfoRefresh;

    /// <summary>
    /// 玩家下面的角标
    /// </summary>
    public event Action<float[]> onFollowPlayerTransform;

    /// <summary>
    /// 玩家不在准备区域
    /// </summary>
    public event Action<int> onPlayerNotInReadyArea;

    /// <summary>
    /// 玩家取消了准备
    /// </summary>
    public event Action<int> onPlayerCancelReady;

    /// <summary>
    /// 玩家已准备
    /// </summary>
    public event Action<int, int, int> onPlayerIsReady;

    /// <summary>
    /// 指定区域没有人
    /// </summary>
    public event Action<int> onNoneIsArea;

    /// <summary>
    /// 可以开始游戏
    /// </summary>
    public event Action onCanGameStart;

    /// <summary>
    /// 玩家消失
    /// </summary>
    public event Action<int[]> onPlayerDisappeared;

    /// <summary>
    /// 玩家重新出现
    /// </summary>
    public event Action<int[]> onPlayerReviced;

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public event Action<bool, int, int> onRestartGame;

    /// <summary>
    ///这个玩家本轮不玩游戏 不要识别
    /// </summary>
    public event Action<int, int, int> onPlayerNotGame;

    /// <summary>
    /// 人脸正在识别（玩家进入槽位，等待识别结果）
    /// </summary>
    public event Action<int> onPlayerFaceRecognizing;

    /// <summary>
    /// 人脸识别成功
    /// </summary>
    public event Action<int> onPlayerFaceRecognized;

    /// <summary>
    /// 人脸识别失败
    /// </summary>
    public event Action<int> onPlayerFaceRecognizeFailed;

    /// <summary>
    /// 准备流程状态变化。准备界面只消费这个结果，不直接推断骨骼流程。
    /// </summary>
    public event Action<int, PrepareMatchSeatState> onPrepareSeatStateChanged;

    /// <summary>
    /// 每帧广播每个座位的朝向偏移 [-1,1]，0=正对相机，正值=左转，负值=右转
    /// </summary>
    public event Action<float[]> onFollowPlayerRotation;

    /// <summary>
    /// 玩家普攻（含左右手交替出拳），参数: 座位索引, 连击数(≥1)
    /// </summary>
    public event Action<int, int> onPlayerNormalAttack;

    /// <summary>
    /// 玩家技能（双手举过头做抛物动作），参数: 座位索引
    /// </summary>
    public event Action<int> onPlayerSkillAttack;

    /// <summary>
    /// 准备阶段左挥手选择上一个角色，参数：座位索引
    /// </summary>
    public event Action<int> onPlayerSelectRoleLeft;

    /// <summary>
    /// 准备阶段右挥手选择下一个角色，参数：座位索引
    /// </summary>
    public event Action<int> onPlayerSelectRoleRight;
    #endregion


    #region 事件回调处理

    public TextMeshProUGUI _messageTips;

    public List<Transform> _PlayerList;
    public List<TextMeshProUGUI> _MessageList;
    public List<Image> _FillList;
    private List<Transform> _PrepareRootList;
    private List<Transform> _NoPrepareList;
    private List<LUIButton> _PlayerButtonList;
    private bool _useTwoStatePrepareUi;

    public static bool _canStartGame;
    private bool _hasLoggedWaitingForInit;
    private bool _hasLoggedWaitingForModeSwitch;
    private bool _hasLoggedUpdateRunning;
    private bool _hasLoggedMissingPlayerTextureShow;
    private bool _hasLoggedGameLogicPath;

    private void Awake()
    {
        Instance = this;
        ModelSelectDemo.SetParseDataDemo(this);
        bool shouldUseSdkRuntime = AndroidServerInfoDemo.ShouldUseSdkRuntime();
        if (shouldUseSdkRuntime)
        {
            enabled = true;
            //EnsureDefaultInitGameInfo();
            Debug.Log($"{LogTag} Awake platform={Application.platform} shouldUseSdkRuntime={shouldUseSdkRuntime} enabled={enabled}");
            return;
        }

        AndroidServerInfo serverInfo = AndroidServerInfoDemo.Instance;
        enabled = serverInfo != null && serverInfo.IsSDKMode;
        if (enabled)
        {
            //EnsureDefaultInitGameInfo();
        }
        Debug.Log($"{LogTag} Awake platform={Application.platform} shouldUseSdkRuntime={shouldUseSdkRuntime} hasServerInfo={serverInfo != null} isSDKMode={(serverInfo != null && serverInfo.IsSDKMode)} enabled={enabled}");
    }

    private void OnEnable()
    {
        Debug.Log($"{LogTag} OnEnable");
        _canStartGame = false;
        // 订阅事件
        onPlayerNotInReadyArea += OnPlayerNotInReadyArea;
        onPlayerCancelReady += OnPlayerCancelReady;
        onPlayerIsReady += OnPlayerIsReady;
        onNoneIsArea += OnNoneIsArea;
        onCanGameStart += OnCanGameStart;
        onPlayerDisappeared += OnPlayerDisappeared;
        onPlayerReviced += OnPlayerReviced;
        onRestartGame += OnRestartGame;
        onPlayerNotGame += OnPlayerNotGame;
    }

    private void OnDisable()
    {
        onPlayerNotInReadyArea -= OnPlayerNotInReadyArea;
        onPlayerCancelReady -= OnPlayerCancelReady;
        onPlayerIsReady -= OnPlayerIsReady;
        onNoneIsArea -= OnNoneIsArea;
        onCanGameStart -= OnCanGameStart;
        onPlayerDisappeared -= OnPlayerDisappeared;
        onPlayerReviced -= OnPlayerReviced;
        onRestartGame -= OnRestartGame;
        onPlayerNotGame -= OnPlayerNotGame;
    }

    private void ShowMessage(int index, string msg)
    {
        if (_MessageList == null || index < 0 || index >= _MessageList.Count)
        {
            Debug.LogError("数组下标不正确， index：" + index + " _MessageList.Count:" + (_MessageList != null ? _MessageList.Count : 0));
            return;
        }
        if (_MessageList[index] != null)
        {
            _MessageList[index].text = msg;
        }
        
    }

    private void ShowTips(string msg)
    {
        if (_messageTips != null)
        {
            _messageTips.text = msg;
        }
    }

    private void SetPrepareFillAmount(int index, float fillAmount)
    {
        if (_FillList == null || index < 0 || index >= _FillList.Count || _FillList[index] == null)
        {
            return;
        }

        _FillList[index].fillAmount = Mathf.Clamp01(fillAmount);
    }

    private void SetPrepareChooseState(int index, bool visible)
    {
        if (_PlayerButtonList != null && index >= 0 && index < _PlayerButtonList.Count && _PlayerButtonList[index] != null)
        {
            _PlayerButtonList[index].SetAsChooseState(visible);
        }
    }

    private void SetTwoStatePrepareVisual(int index, bool isReady, float fillAmount)
    {
        if (!_useTwoStatePrepareUi)
        {
            return;
        }

        // 正式准备界面的该文本只表达准备结果，中间流程提示由对应的专用节点显示。
        ShowMessage(index, RenderAPI.GetTextByLanId(isReady ? "td_prepare_state_ready" : "td_prepare_state_no_ready"));
        SetPrepareFillAmount(index, fillAmount);

        if (_PrepareRootList != null && index >= 0 && index < _PrepareRootList.Count && _PrepareRootList[index] != null)
        {
            _PrepareRootList[index].gameObject.SetActive(true);
        }

        if (_NoPrepareList != null && index >= 0 && index < _NoPrepareList.Count && _NoPrepareList[index] != null)
        {
            _NoPrepareList[index].gameObject.SetActive(!isReady);
        }
    }

    private void RefreshPrepareSeatVisualByStep(int index)
    {
        var state = GetPrepareSeatStateInternal(index);
        if (state == null || !_useTwoStatePrepareUi)
        {
            return;
        }

        switch (state.m_Step)
        {
            case PrepareMatchStep.WaitCenter:
            case PrepareMatchStep.FaceRecognizing:
            case PrepareMatchStep.WaitRaiseHand:
                SetTwoStatePrepareVisual(index, false, 0f);
                break;
            case PrepareMatchStep.Ready:
                SetTwoStatePrepareVisual(index, true, 1f);
                break;
            default:
                SetTwoStatePrepareVisual(index, false, 0f);
                break;
        }
    }

    private void OnPlayerNotInReadyArea(int index)
    {
        var msg = RenderAPI.GetTextByLanId("sdk_demo_ready_out_area");
        ShowTips(msg);
        if (_useTwoStatePrepareUi)
        {
            RefreshPrepareSeatVisualByStep(index);
            return;
        }

        ShowMessage(index, msg);
        SetPrepareFillAmount(index, 0f);
    }

    private void OnPlayerCancelReady(int index)
    {
        var msg = RenderAPI.GetTextByLanId("sdk_demo_ready_cancel");
        ShowTips(msg);
        if (_useTwoStatePrepareUi)
        {
            RefreshPrepareSeatVisualByStep(index);
            return;
        }

        ShowMessage(index, msg);
        SetPrepareFillAmount(index, 0f);
    }

    private void OnPlayerIsReady(int index, int curFrame, int needFrame)
    {
        if (curFrame >= needFrame && needFrame > 0)
        {
            var msg = RenderAPI.GetTextByLanId("sdk_demo_ready_done");
            ShowTips(msg);
            if (_useTwoStatePrepareUi)
            {
                SetTwoStatePrepareVisual(index, true, 1f);
                return;
            }

            ShowMessage(index, msg);
            SetPrepareChooseState(index, true);
            SetPrepareFillAmount(index, 1f);
        }
        else if (curFrame > 0)
        {
            var msg = RenderAPI.GetTextByLanId("sdk_demo_ready_progress", curFrame, needFrame);
            ShowTips(msg);
            if (_useTwoStatePrepareUi)
            {
                float progress = needFrame > 0 ? (float)curFrame / needFrame : 0f;
                SetTwoStatePrepareVisual(index, false, progress);
                return;
            }

            ShowMessage(index, msg);
            SetPrepareFillAmount(index, (float)curFrame / needFrame);
        }
        else
        {
            // curFrame == 0 的情况，通常意味着准备动作中断或重置
            var msg = RenderAPI.GetTextByLanId("sdk_demo_ready_no_pose");
            ShowTips(msg);
            if (_useTwoStatePrepareUi)
            {
                RefreshPrepareSeatVisualByStep(index);
                return;
            }

            ShowMessage(index, msg);
            SetPrepareFillAmount(index, 0f);
        }
    }

    private void OnNoneIsArea(int index)
    {
        var msg = RenderAPI.GetTextByLanId("sdk_demo_ready_empty");
        ShowTips(msg);
        if (_useTwoStatePrepareUi)
        {
            SetTwoStatePrepareVisual(index, false, 0f);
            return;
        }

        ShowMessage(index, msg);
    }

    private void OnCanGameStart()
    {
        _canStartGame = true;
        ShowTips(RenderAPI.GetTextByLanId("sdk_demo_ready_all_done"));
    }

    private void OnPlayerDisappeared(int[] indices)
    {
        if (indices == null) return;
        foreach (var index in indices)
        {
            var msg = RenderAPI.GetTextByLanId("sdk_demo_player_lost");
            ShowTips(msg);
            if (_useTwoStatePrepareUi)
            {
                SetTwoStatePrepareVisual(index, false, 0f);
                continue;
            }

            ShowMessage(index, msg);
            SetPrepareFillAmount(index, 0f);
        }
    }

    private void OnPlayerReviced(int[] indices)
    {
        if (indices == null) return;
        foreach (var index in indices)
        {
            var msg = RenderAPI.GetTextByLanId("sdk_demo_player_recovered");
            ShowTips(msg);
            if (_useTwoStatePrepareUi)
            {
                SetTwoStatePrepareVisual(index, false, 0f);
                continue;
            }

            ShowMessage(index, msg);
        }
    }

    private void OnRestartGame(bool arg1, int arg2, int arg3)
    {
        ShowTips(RenderAPI.GetTextByLanId("sdk_demo_restart_game", arg1, arg2, arg3));
        //ShowMessage(index, _messageTips.text);
    }

    public void SetPrepareRoleSelectWaveSpeedRatio(float value)
    {
        _prepareRoleSelectWaveSpeedRatioPerSecond = Mathf.Max(0.1f, value);
    }

    private void OnPlayerNotGame(int index, int arg2, int arg3)
    {
        var msg = RenderAPI.GetTextByLanId("sdk_demo_player_not_game");
        ShowTips(msg);
        if (_useTwoStatePrepareUi)
        {
            SetTwoStatePrepareVisual(index, false, 0f);
            return;
        }

        ShowMessage(index, msg);
        SetPrepareFillAmount(index, 0f);
    }

    #endregion

    #region 人脸识别事件触发方法（供外部调用）

    /// <summary>
    /// 通知指定槽位人脸识别成功（供 HeadImageController 等外部调用）
    /// </summary>
    public void NotifyPlayerFaceRecognized(int slotIndex)
    {
        NotifyPlayerFaceRecognized(slotIndex, 0, null);
    }

    public void NotifyPlayerFaceRecognized(int slotIndex, long userId, string facePhotoPath)
    {
        TryNotifyPlayerFaceRecognized(slotIndex, userId, facePhotoPath);
    }

    /// <summary>
    /// 只接收当前仍处于人脸识别阶段的结果，避免玩家离位后到达的旧回调污染新一轮准备状态。
    /// </summary>
    public bool TryNotifyPlayerFaceRecognized(int slotIndex, long userId, string facePhotoPath)
    {
        if (!SetPrepareSeatFaceRecognized(slotIndex, userId, facePhotoPath))
        {
            return false;
        }

        onPlayerFaceRecognized?.Invoke(slotIndex);
        return true;
    }

    public long[] ReadAssignedUserIds(int excludeSeatIndex)
    {
        if (m_PrepareSeatStates == null || m_PrepareSeatStates.Length == 0)
        {
            return Array.Empty<long>();
        }

        var userIds = new List<long>(m_PrepareSeatStates.Length);
        for (int i = 0; i < m_PrepareSeatStates.Length; i++)
        {
            var state = m_PrepareSeatStates[i];
            if (i == excludeSeatIndex || state == null || state.m_UserId <= 0)
            {
                continue;
            }

            if (state.m_Step == PrepareMatchStep.WaitRaiseHand || state.m_Step == PrepareMatchStep.Ready)
            {
                userIds.Add(state.m_UserId);
            }
        }

        return userIds.ToArray();
    }

    /// <summary>
    /// 通知指定槽位人脸识别失败（供外部调用）
    /// </summary>
    public void NotifyPlayerFaceRecognizeFailed(int slotIndex)
    {
        var state = GetPrepareSeatStateInternal(slotIndex);
        if (state != null && state.m_Step == PrepareMatchStep.FaceRecognizing)
        {
            state.m_StateStartTime = Time.time;
            NotifyPrepareSeatStateChanged(slotIndex);
        }
        onPlayerFaceRecognizeFailed?.Invoke(slotIndex);
    }

    #endregion

    // 手动点击确认进入战斗时，只保留当前已确认的玩家参与后续战斗识别。
    public void EnterBattleWithConfirmedPlayers()
    {
        if (personPlayerIds == null || personPlayerIds.Length == 0)
        {
            return;
        }

        ApplyBattleSeatMaskFromConfirmedPlayers();
        isCheckPersonReadyIng = false;
        _canStartGame = true;
        UpdatePrepareFaceRecognitionRunning();
        if (_startGameCoroutine != null)
        {
            StopCoroutine(_startGameCoroutine);
            _startGameCoroutine = null;
        }
    }

    // 每次重新进入正式准备界面时，必须从“未准备”开始，不能沿用上一次准备或战斗阶段的识别状态。
    public void ResetPreparePhaseState()
    {
        isCheckPersonReadyIng = true;
        _canStartGame = false;
        if (_startGameCoroutine != null)
        {
            StopCoroutine(_startGameCoroutine);
            _startGameCoroutine = null;
        }

        int seatCount = Mathf.Max(_curPlayerCount, _PlayerList != null ? _PlayerList.Count : 0);
        for (int i = 0; i < seatCount; i++)
        {
            if (personPlayerIds != null && i < personPlayerIds.Length)
            {
                personPlayerIds[i] = PersonIdNull;
            }

            if (personPlayerLockIng != null && i < personPlayerLockIng.Length)
            {
                personPlayerLockIng[i] = PersonIdNull;
            }

            if (personHandsUpFrameCount != null && i < personHandsUpFrameCount.Length)
            {
                personHandsUpFrameCount[i] = 0;
            }

            if (personMissingFrameCount != null && i < personMissingFrameCount.Length)
            {
                personMissingFrameCount[i] = 0;
            }

            if (personNotGamePoseFrameCount != null && i < personNotGamePoseFrameCount.Length)
            {
                personNotGamePoseFrameCount[i] = 0;
            }

            if (personWasInReadyArea != null && i < personWasInReadyArea.Length)
            {
                personWasInReadyArea[i] = false;
            }

            if (_isReadyShow != null && i < _isReadyShow.Length)
            {
                _isReadyShow[i] = 0;
            }

            if (_detectionStates != null && i < _detectionStates.Length)
            {
                _detectionStates[i] = DetectionState.Unknown;
            }

            ResetPrepareSeatState(i);

            if (_battleSeatEnabled != null && i < _battleSeatEnabled.Length)
            {
                _battleSeatEnabled[i] = false;
            }

            ResetPrepareRoleSelectState(i);
            SetTwoStatePrepareVisual(i, false, 0f);
        }

        UpdatePrepareFaceRecognitionRunning();
    }

    private void ApplyBattleSeatMaskFromConfirmedPlayers()
    {
        if (_battleSeatEnabled == null || _battleSeatEnabled.Length != _curPlayerCount)
        {
            _battleSeatEnabled = new bool[_curPlayerCount];
        }

        for (int i = 0; i < _curPlayerCount; i++)
        {
            bool isBattleSeat = personPlayerIds != null &&
                                i < personPlayerIds.Length &&
                                personPlayerIds[i] != PersonIdNull &&
                                IsSlotEnabled(i);
            _battleSeatEnabled[i] = isBattleSeat;
            if (personPlayerLockIng != null && i < personPlayerLockIng.Length)
            {
                personPlayerLockIng[i] = PersonIdNull;
            }

            if (isBattleSeat)
            {
                continue;
            }

            if (personHandsUpFrameCount != null && i < personHandsUpFrameCount.Length)
            {
                personHandsUpFrameCount[i] = 0;
            }

            if (personMissingFrameCount != null && i < personMissingFrameCount.Length)
            {
                personMissingFrameCount[i] = 0;
            }

            if (personWasInReadyArea != null && i < personWasInReadyArea.Length)
            {
                personWasInReadyArea[i] = false;
            }

            if (_isReadyShow != null && i < _isReadyShow.Length)
            {
                _isReadyShow[i] = 0;
            }
        }
    }

    private bool IsSeatTrackedForCurrentPhase(int seatIndex)
    {
        if (seatIndex < 0 || seatIndex >= _curPlayerCount)
        {
            return false;
        }

        if (isCheckPersonReadyIng)
        {
            return IsSlotEnabled(seatIndex);
        }

        return _battleSeatEnabled != null &&
               seatIndex < _battleSeatEnabled.Length &&
               _battleSeatEnabled[seatIndex];
    }

    private Coroutine _startGameCoroutine;
    private bool[] _battleSeatEnabled;

    /// <summary>
    /// 举手准备是个阶段性动作 保存每个玩家准备的时间
    /// </summary>
    private int[] personHandsUpFrameCount;

    private bool[] personWasInReadyArea;
    /// <summary>
    /// 需要准备动作准备多久表示准备完成
    /// </summary>
    [SerializeField]
    [Tooltip("举手准备动作需要维持的帧数，默认30帧约1秒")]
    private int readyNeedFrame = 30;

    private int _curPlayerCount;

    public int CurPlayerCount { get => _curPlayerCount; set => _curPlayerCount = value; }

    /// <summary>
    ///每个玩家的矩形框有4位置组成
    /// </summary>
    public const int RECT_ELEMENTS_PER_PLAYER = 4;

    private float _lastTipTime = -10f;

    //根据业务需求来决定是否需要绘制。
    public PlayerTextuerShow playerTextuerShow;

    private HashSet<int> _currentFrameNeedHandPlayer = new HashSet<int>(); // 复用避免GC
    private int[] _tempExcludeIdArray = new int[10]; // 足够存放玩家ID，避免ToArray()产生GC

    private bool _isInit = false;

    private enum DetectionState
    {
        Unknown,
        Empty,
        OutOfArea,
        InArea,
        Ready
    }
    private DetectionState[] _detectionStates;
    private PrepareMatchSeatState[] m_PrepareSeatStates;
    private int[] _isReadyShow;
    [SerializeField]
    [Tooltip("进入准备区域后等待多少帧才开始检测举手，默认60帧约2秒")]
    private int readyShowTime = 60;
    [SerializeField]
    [Tooltip("人脸识别超时时间，超时后进入系统人工选择")]
    private float prepareFaceRecognizeTimeoutSeconds = 5f;
    private bool m_PrepareFaceRecognitionRunning;
    private float[] _cachedOffsetPlayerTransformX;

    private float[] _cachedPlayerRotationOffset;
    private float[] _maxShoulderWidth;
    private int[] _lastPoseType;
    private const float RotationSmoothFactor = 6f;
    private const float KeypointConfidenceThreshold = 0.3f;
    private const float ShoulderWidthEpsilon = 1e-4f;
    private const float MaxShoulderWidthUpdateConfidence = 0.7f;

    private const float AttackKeypointMinConfidence = 0.3f;
    private const float PunchSpeedRatioPerSecond = 1.5f;
    private const float PunchCooldownSeconds = 0.3f;
    private const int AlternatingPunchWindowFrames = 30;
    private const int ThrowWindUpFrames = 3;
    private const float ThrowMarginRatio = 0.05f;
    private const float ThrowReleaseSpeedRatio = 2.0f;
    private const float ThrowCooldownSeconds = 0.5f;
    private const float PrepareReadyOverheadMarginRatio = 0.05f;
    private const float PrepareRoleSelectWaveDistanceRatio = 0.45f;
    private const float PrepareRoleSelectResetDistanceRatio = 0.12f;
    private const float DefaultPrepareRoleSelectWaveSpeedRatioPerSecond = 0.9f;
    private const float PrepareRoleSelectWaveCooldownSeconds = 0.45f;
    private const int PunchHandNone = 0;
    private const int PunchHandLeft = 1;
    private const int PunchHandRight = 2;

    private PlayerAttackState[] _playerAttackStates;
    private bool[] _prepareRoleSelectHasLeftWrist;
    private bool[] _prepareRoleSelectHasRightWrist;
    private float[] _prepareRoleSelectAnchorLeftWristX;
    private float[] _prepareRoleSelectAnchorRightWristX;
    private float[] _prepareRoleSelectLastLeftWristX;
    private float[] _prepareRoleSelectLastRightWristX;
    private float[] _prepareRoleSelectLastLeftWristTime;
    private float[] _prepareRoleSelectLastRightWristTime;
    private float[] _prepareRoleSelectCooldownUntil;

    private int dealyFrame = 0;
    private bool playerDisappeared = false;
    private List<int> _cachedCleanPlayerList = new List<int>();
    private HashSet<int> _cachedExcludedIds = new HashSet<int>();
    private int[] _cachedExcludeArray;
    private int[] personMissingFrameCount;
    [SerializeField]
    [Tooltip("玩家丢失多少帧后才认为真正离场，默认30帧")]
    private int missingFrameThreshold = 30;

    private int[] personNotGamePoseFrameCount; 
    private float _prepareRoleSelectWaveSpeedRatioPerSecond = DefaultPrepareRoleSelectWaveSpeedRatioPerSecond;

    private float _curAreaLeft = 0;
    private float _curAreaRight = 1;
    private float _curAreaTop = 0;
    private float _curAreaBottom = 1;
    private const float READY_RECT_WIDTH = 0.15f;

    /// <summary>
    /// 人物准备动作的判定区域。 因为锁定了玩家 但是准备时 还是需要玩家站在中间
    /// </summary>
    private float[] personPlayerLockRectf;

    // 改为 class 并预分配内存以避免每帧 GC
    private class PlayerFullData
    {
        public int playerId;
        public int playerIndex;
        public float left;
        public float top;
        public float right;
        public float bottom;
        public int poseType;
        public int leftHandType;
        public int rightHandType;
        public float score;
        public float leftHandScore;
        public float rightHandScore;
        public float faceScore;
        public float[,] keyPointListPose;
        public float[,] keyPointListLeftHand;
        public float[,] keyPointListRightHand;
        public float[,] keyPointListFace;

        public PlayerFullData()
        {
            keyPointListPose = new float[(int)KeyPointIndex.KEYPOINT_COUNT, 4];
            keyPointListLeftHand = new float[(int)HandLandmark21.HAND_LANDMARK_COUNT, 4];
            keyPointListRightHand = new float[(int)HandLandmark21.HAND_LANDMARK_COUNT, 4];
            // 若需要面部也可以在此预分配
        }
    }

    private class PlayerAttackState
    {
        public bool hasLastLeftWrist;
        public Vector2 lastLeftWrist;
        public bool hasLastRightWrist;
        public Vector2 lastRightWrist;
        public float lastShoulderWidth;
        public float lastFrameTime;

        public int lastPunchHand;
        public float lastPunchTime;
        public int alternatingCombo;
        public float lastLeftPunchTime;
        public float lastRightPunchTime;
        public int frameCounter;

        public int throwWindUpCount;
        public bool throwArmed;
        public float throwCooldownUntil;

        public void Reset()
        {
            hasLastLeftWrist = false;
            lastLeftWrist = Vector2.zero;
            hasLastRightWrist = false;
            lastRightWrist = Vector2.zero;
            lastShoulderWidth = 0f;
            lastFrameTime = 0f;
            lastPunchHand = PunchHandNone;
            lastPunchTime = 0f;
            alternatingCombo = 0;
            lastLeftPunchTime = 0f;
            lastRightPunchTime = 0f;
            frameCounter = 0;
            throwWindUpCount = 0;
            throwArmed = false;
            throwCooldownUntil = 0f;
        }
    }

    /// <summary>
    /// 人物骨骼点字典
    /// </summary>
    private Dictionary<int, PlayerFullData> playerKeyPoints;
    /// <summary>
    /// 人物数据对象池，用于避免每帧实例化
    /// </summary>
    private Queue<PlayerFullData> _playerDataPool = new Queue<PlayerFullData>();

    private bool isChangeMode = false;

    public bool IsChangeMode
    {
        get => isChangeMode;
        set
        {
            if (isChangeMode == value)
            {
                return;
            }

            Debug.Log($"{LogTag} IsChangeMode {isChangeMode} -> {value} currentPlayerCount={_curPlayerCount}");
            isChangeMode = value;
            _hasLoggedWaitingForModeSwitch = false;
            _hasLoggedUpdateRunning = false;
        }
    }

    void Start()
    {
        //_MessageList = new List<TextMeshProUGUI>();
        //_FillList = new List<Image>();
        //foreach (var player in _PlayerList)
        //{
        //    _MessageList.Add(player.Find("txt_prepare").GetComponent<TextMeshProUGUI>());
        //    _FillList.Add(player.Find("PlayerPre/Fill").GetComponentInChildren<Image>());
        //}
        EnsureDefaultInitGameInfo();
    }

    public void Init(TextMeshProUGUI tips, List<TextMeshProUGUI> _messageList, PlayerTextuerShow _playerTextuerShow)
    {
        _MessageList = _messageList;
        _messageTips = tips;
        playerTextuerShow = _playerTextuerShow;
        _useTwoStatePrepareUi = false;
        _PrepareRootList = null;
        _NoPrepareList = null;
        _PlayerButtonList = null;
        if(enabled) EnsureDefaultInitGameInfo();
    }

    public void ConfigurePreparePlayerSlots(IList<Transform> playerSlots)
    {
        if (playerSlots == null || playerSlots.Count <= 0)
        {
            return;
        }

        _PlayerList = new List<Transform>(playerSlots.Count);
        _MessageList = new List<TextMeshProUGUI>(playerSlots.Count);
        _FillList = new List<Image>(playerSlots.Count);
        _PrepareRootList = new List<Transform>(playerSlots.Count);
        _NoPrepareList = new List<Transform>(playerSlots.Count);
        _PlayerButtonList = new List<LUIButton>(playerSlots.Count);
        _useTwoStatePrepareUi = true;
        for (int i = 0; i < playerSlots.Count; i++)
        {
            Transform playerSlot = playerSlots[i];
            _PlayerList.Add(playerSlot);
            _MessageList.Add(FindPrepareText(playerSlot));
            _FillList.Add(FindPrepareFill(playerSlot));
            _PrepareRootList.Add(playerSlot != null ? playerSlot.Find(PrepareRootPath) : null);
            _NoPrepareList.Add(playerSlot != null ? playerSlot.Find(PrepareNoPreparePath) : null);
            _PlayerButtonList.Add(playerSlot != null ? playerSlot.GetComponent<LUIButton>() : null);
            SetTwoStatePrepareVisual(i, false, 0f);
        }
    }

    private static TextMeshProUGUI FindPrepareText(Transform playerSlot)
    {
        if (playerSlot == null)
        {
            return null;
        }

        // 正式准备界面调整后，准备文字在 prepare 子节点下；根节点旧路径只作为 Demo 兜底。
        var text = playerSlot.Find(PrepareTextPath)?.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            return text;
        }

        return playerSlot.Find("txt_prepare")?.GetComponent<TextMeshProUGUI>();
    }

    private static Image FindPrepareFill(Transform playerSlot)
    {
        return playerSlot != null ? playerSlot.Find(PrepareFillPath)?.GetComponentInChildren<Image>() : null;
    }

    private void EnsureDefaultInitGameInfo()
    {
        if (_isInit)
        {
            return;
        }

/*#if UNITY_ANDROID
        InitGameInfo(1, PlayerMatchViewMode.FullView);
#endif*/
        InitGameInfo(4, PlayerMatchViewMode.PartitionView);
    }

    /// <summary>
    /// 初始化当前游戏需要的玩家的个数。
    /// </summary>
    public void InitGameInfo(int playerNumber, PlayerMatchViewMode playerMatchViewMode)
    {
        Debug.Log($"{LogTag} InitGameInfo enter playerNumber={playerNumber} viewMode={playerMatchViewMode} isInit={_isInit} hasPlayerTextuerShow={playerTextuerShow != null}");
        if (playerNumber <= 0)
        {
            Debug.LogWarning($"{LogTag} InitGameInfo 终止，playerNumber 非法");
            return;
        }

        _curPlayerCount = playerNumber;
        if (!_isInit)
        {
            InitFrameInfoStorage(100);
        }
        PlayerMatchView.Instance.InitPlayerMatchView(playerNumber, playerMatchViewMode);//初始化
        InitializePlayerData();
        InitializePlayerRectangles();

        if (playerTextuerShow == null)
        {
            Debug.LogWarning($"{LogTag} InitGameInfo 发现 playerTextuerShow 为空");
            _hasLoggedMissingPlayerTextureShow = true;
        }
        else
        {
            playerTextuerShow.InitGameInfo(personPlayerReadyRectf, playerNumber);
            _hasLoggedMissingPlayerTextureShow = false;
        }

        _isInit = true;
        IsChangeMode = false;
        _hasLoggedWaitingForInit = false;
        _hasLoggedUpdateRunning = false;
        _hasLoggedGameLogicPath = false;
        Debug.Log($"{LogTag} InitGameInfo 完成 curPlayerCount={_curPlayerCount} viewMode={PlayerMatchView.Instance.PlayerMatchViewMode}");
    }

    private void InitializePlayerData()
    {
        personPlayerIds = new int[_curPlayerCount];
        personPlayerLockIng = new int[_curPlayerCount];
        personHandsUpFrameCount = new int[_curPlayerCount];
        personWasInReadyArea = new bool[_curPlayerCount];
        playerKeyPoints = new Dictionary<int, PlayerFullData>();

        _detectionStates = new DetectionState[_curPlayerCount];
        m_PrepareSeatStates = new PrepareMatchSeatState[_curPlayerCount];
        _isReadyShow = new int[_curPlayerCount];
        _cachedOffsetPlayerTransformX = new float[_curPlayerCount];
        _cachedPlayerRotationOffset = new float[_curPlayerCount];
        _maxShoulderWidth = new float[_curPlayerCount];
        _lastPoseType = new int[_curPlayerCount];
        _playerAttackStates = new PlayerAttackState[_curPlayerCount];
        _prepareRoleSelectHasLeftWrist = new bool[_curPlayerCount];
        _prepareRoleSelectHasRightWrist = new bool[_curPlayerCount];
        _prepareRoleSelectAnchorLeftWristX = new float[_curPlayerCount];
        _prepareRoleSelectAnchorRightWristX = new float[_curPlayerCount];
        _prepareRoleSelectLastLeftWristX = new float[_curPlayerCount];
        _prepareRoleSelectLastRightWristX = new float[_curPlayerCount];
        _prepareRoleSelectLastLeftWristTime = new float[_curPlayerCount];
        _prepareRoleSelectLastRightWristTime = new float[_curPlayerCount];
        _prepareRoleSelectCooldownUntil = new float[_curPlayerCount];
        personMissingFrameCount = new int[_curPlayerCount];
        personNotGamePoseFrameCount = new int[_curPlayerCount];
        _cachedExcludeArray = new int[_curPlayerCount * 2];
        _battleSeatEnabled = new bool[_curPlayerCount];

        for (int i = 0; i < _curPlayerCount; i++)
        {
            personPlayerIds[i] = PersonIdNull;
            personPlayerLockIng[i] = PersonIdNull; // 初始化为空
            personHandsUpFrameCount[i] = 0;
            personWasInReadyArea[i] = false;

            _detectionStates[i] = DetectionState.Unknown;
            m_PrepareSeatStates[i] = new PrepareMatchSeatState();
            m_PrepareSeatStates[i].Reset(i);
            _isReadyShow[i] = 0;
            personMissingFrameCount[i] = 0;
            personNotGamePoseFrameCount[i] = 0;
            _cachedPlayerRotationOffset[i] = 0f;
            _maxShoulderWidth[i] = 0f;
            _lastPoseType[i] = 0;
            _playerAttackStates[i] = new PlayerAttackState();
            _playerAttackStates[i].Reset();
            ResetPrepareRoleSelectState(i);
        }
    }

    private void InitializePlayerRectangles()
    {
        int arrayLength = RECT_ELEMENTS_PER_PLAYER * _curPlayerCount;
        personPlayerLockRectf = new float[arrayLength];
        personPlayerReadyRectf = new float[_curPlayerCount, RECT_ELEMENTS_PER_PLAYER];

        // 初始赋予全屏幕坐标，后续每帧更新
        _curAreaLeft = 0;
        _curAreaRight = 1;
        _curAreaTop = 0;
        _curAreaBottom = 1;

        if (PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
        {
            PlayerMatchView.Instance.SetpersonPlayerReadyPartitionRectf(_curPlayerCount, personPlayerReadyRectf);
        }
        else
        {
            ResetJudgmentArea();
        }
    }

    /// <summary>
    /// 摄像头变化后 重新设置判定区域
    /// </summary>
    private void ResetJudgmentArea()
    {
        float oneAreaLength = (_curAreaRight - _curAreaLeft) / _curPlayerCount;
        for (int i = 0; i < _curPlayerCount; i++)
        {
            int regionIndex = _curPlayerCount - 1 - i;
            float left = _curAreaLeft + regionIndex * oneAreaLength;
            float right = _curAreaLeft + (regionIndex + 1) * oneAreaLength;
            float center = (left + right) / 2;
            float personReadyRectfLeft = center - READY_RECT_WIDTH;
            float personReadyRectfRight = center + READY_RECT_WIDTH;

            int baseIndex = i * RECT_ELEMENTS_PER_PLAYER;
            // 更新锁定区域 (这里用作全匹配的边界)
            personPlayerLockRectf[baseIndex] = left;
            personPlayerLockRectf[baseIndex + 1] = _curAreaTop;
            personPlayerLockRectf[baseIndex + 2] = right;
            personPlayerLockRectf[baseIndex + 3] = _curAreaBottom;

            // 更新举手准备区域
            personPlayerReadyRectf[i, 0] = personReadyRectfLeft;
            personPlayerReadyRectf[i, 1] = _curAreaTop;
            personPlayerReadyRectf[i, 2] = personReadyRectfRight;
            personPlayerReadyRectf[i, 3] = _curAreaBottom;
        }
    }



    /// <summary>
    /// 游戏中，根据实际需求获取数据。 
    /// 大部分游戏，每一帧都需要获取最新的数据的。 
    /// </summary>
    void Update()
    {
        if (!_isInit)
        {
            if (!_hasLoggedWaitingForInit)
            {
                Debug.LogWarning($"{LogTag} Update 等待初始化完成");
                _hasLoggedWaitingForInit = true;
            }
            return;
        }

        _hasLoggedWaitingForInit = false;
        if (isChangeMode)
        {
            if (!_hasLoggedWaitingForModeSwitch)
            {
                Debug.Log($"{LogTag} Update 暂停拉帧，等待模式切换完成");
                _hasLoggedWaitingForModeSwitch = true;
            }
            return;
        }

        _hasLoggedWaitingForModeSwitch = false;
        if (!_hasLoggedUpdateRunning)
        {
            Debug.Log($"{LogTag} Update 开始拉取人物数据 curPlayerCount={_curPlayerCount} viewMode={PlayerMatchView.Instance.PlayerMatchViewMode}");
            _hasLoggedUpdateRunning = true;
        }

        UpdateGetPersonData();
    }
    /// <summary>
    /// 遍历这一帧的人物的数据
    /// </summary>
    protected override void TraversePersion(long frameInfoPtr, int[] currentFramePersonIds, int perSonNumber)
    {

        //根据实际的业务需求去处理。
        PlayerMatchView.Instance.ResetData();//传入新的骨骼数据前必须清空

        if (playerTextuerShow != null)
        {
            playerTextuerShow.SetSkeletsonHide();
        }

        // 将上一帧用过的对象回收到池子中，避免垃圾回收(GC)
        foreach (var kv in playerKeyPoints)
        {
            _playerDataPool.Enqueue(kv.Value);
        }
        playerKeyPoints.Clear();

        _currentFrameNeedHandPlayer.Clear();

        // 遍历每个人物
        for (int i = 0; i < perSonNumber; i++)
        {
            int playerId = currentFramePersonIds[i];
            if (playerId == PersonIdNull)
            {
                continue;
            }
            _currentFrameNeedHandPlayer.Add(playerId);
            //获得这个人的参数。
            if (!GetDetectBoxInfoByType(frameInfoPtr, (int)DetectType.DETECT_TYPE_PERSON, playerId,
                out float left, out float top, out float right, out float bottom,
                out float score, out int kptCount, out int type))
            {
                continue;
            }


            if (left <= 0 || top <= 0 || right <= 0 || bottom <= 0)
            {
                continue;
            }

            // 保护：如果没有完整的骨骼点，直接跳过该人物的后续处理
            if (kptCount != (int)KeyPointIndex.KEYPOINT_COUNT)
            {
                continue;
            }

            // 从池中取对象或新建对象
            PlayerFullData playerFull = _playerDataPool.Count > 0 ? _playerDataPool.Dequeue() : new PlayerFullData();
            playerFull.playerId = playerId;
            playerFull.left = left;
            playerFull.top = top;
            playerFull.right = right;
            playerFull.bottom = bottom;
            playerFull.score = score;
            playerFull.poseType = type;
            //获取这个人的骨骼的点。这里直接使用预分配好的数组
            for (int k = 0; k < kptCount; k++)
            {
                if (GetKeyPointByType(frameInfoPtr, (int)DetectType.DETECT_TYPE_PERSON, playerId, k,
   out playerFull.keyPointListPose[k, 0], out playerFull.keyPointListPose[k, 1], out playerFull.keyPointListPose[k, 2], out playerFull.keyPointListPose[k, 3]))
                {
                }
            }
            // Debug.Log("372 372 372 看一下当前玩家的姿势---" + playerFull.poseType);

            if (Time.time - _lastTipTime >= 60f)
            {
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_face_size", playerFull.playerId, AndroidServerInfoDemo.Instance.GetFaceSizePixel(playerFull.playerId)));
                _lastTipTime = Time.time;
            }


            playerKeyPoints[playerId] = playerFull;
            _currentFrameNeedHandPlayer.Add(playerId);
            PlayerMatchView.Instance.SetPersonPlayerRectf(playerFull.keyPointListPose, playerFull.playerId, playerFull.score);//设置玩家新的数据
            if (GetDetectBoxInfoByType(frameInfoPtr, (int)DetectType.DETECT_TYPE_LEFT_HAND, playerId,
                     out float lh_left, out float lh_top, out float lh_right, out float lh_bottom,
                     out float lh_score, out int lh_kptCount, out int lh_type))
            {
                playerFull.leftHandScore = lh_score;
                playerFull.leftHandType = lh_type;
                // 复用预分配数组，直接填充数据
                for (int k = 0; k < lh_kptCount && k < playerFull.keyPointListLeftHand.GetLength(0); k++)
                {
                    if (GetKeyPointByType(frameInfoPtr, (int)DetectType.DETECT_TYPE_LEFT_HAND, playerId, k,
                        out playerFull.keyPointListLeftHand[k, 0], out playerFull.keyPointListLeftHand[k, 1], out playerFull.keyPointListLeftHand[k, 2], out playerFull.keyPointListLeftHand[k, 3]))
                    {
                    }
                }
            }
            if (GetDetectBoxInfoByType(frameInfoPtr, (int)DetectType.DETECT_TYPE_RIGHT_HAND, playerId,
                           out float rh_left, out float rh_top, out float rh_right, out float rh_bottom,
                           out float rh_score, out int rh_kptCount, out int rh_type))
            {
                playerFull.rightHandScore = rh_score;
                playerFull.rightHandType = rh_type;
                // 复用预分配数组，直接填充数据
                for (int k = 0; k < rh_kptCount && k < playerFull.keyPointListRightHand.GetLength(0); k++)
                {
                    if (GetKeyPointByType(frameInfoPtr, (int)DetectType.DETECT_TYPE_RIGHT_HAND, playerId, k,
                        out playerFull.keyPointListRightHand[k, 0], out playerFull.keyPointListRightHand[k, 1], out playerFull.keyPointListRightHand[k, 2], out playerFull.keyPointListRightHand[k, 3]))
                    {
                    }
                }
            }


            if (playerKeyPoints.ContainsKey(playerId) && playerTextuerShow != null)
            {
                if (PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
                {
                    // PartitionView 模式下：通过 personPlayerIds / personPlayerLockIng 查找该玩家真实分配的槽位，
                    // 固定使用 Playerskeletons 最后 4 个节点（对应屏幕上 4 个分割区域）。
                    // 如果玩家未被分配槽位（slotIndex == -1），说明当前帧还未识别正确，不显示骨骼。
                    int slotIndex = GetPlayerIndexByPersonId(playerId);
                    if (slotIndex != -1)
                    {
                        int offset = Mathf.Min(4, playerTextuerShow.Playerskeletons.Length);
                        int skeletonIndex = playerTextuerShow.Playerskeletons.Length - offset + slotIndex;
                        playerTextuerShow.DrawSkeleton(skeletonIndex, playerFull.keyPointListPose, null, null, left, top, right, bottom, playerId, score);
                    }
                }
                else
                {
                    // FullView / HalfView 模式下，保持原有逻辑：按帧内遍历顺序 i 分配骨骼节点
                    playerTextuerShow.DrawSkeleton(i, playerFull.keyPointListPose, null, null, left, top, right, bottom, playerId, score);
                }
            }

        }
        if (_currentFrameNeedHandPlayer.Count > 0)
        {
            AndroidServerInfoDemo.Instance.SetPlayerID(_currentFrameNeedHandPlayer.ToArray());
        }

        // JudgeReStartGame();
    }


    protected override void GameLogic(long frameInfoPtr, int[] currentFramePersonIds, int perSonNumber, long aHardwareBufferPtr)
    {
        if (!_hasLoggedGameLogicPath)
        {
            Debug.Log($"{LogTag} GameLogic 首次执行 frameInfoPtr={frameInfoPtr} personCount={perSonNumber} hasPlayerTextuerShow={playerTextuerShow != null}");
            _hasLoggedGameLogicPath = true;
        }

        if (playerTextuerShow != null)
        {
            var bridge = playerTextuerShow.GetComponent<AndroidTextureBridgeBase>();
            if (bridge != null)
            {
                bool isReady = bridge.IsTextureReady();
                //Debug.Log($"[诊断] IsTextureReady={isReady}");  // ⭐ 关键日志

                if (!isReady)
                {
                    Debug.LogError($"[诊断] ❌ 纹理未就绪");
                }
            }
        }

        if (playerTextuerShow != null)
        {
            var bridge = playerTextuerShow.GetComponent<AndroidTextureBridgeBase>();
            if (bridge != null)
            {
                var cameraTextureView = bridge.GetCameraView(0);
                if (cameraTextureView != null)
                {
                    Rect currentUvRect = cameraTextureView.GetCurrentUVRect();
                    _curAreaLeft = currentUvRect.x;
                    _curAreaRight = currentUvRect.x + currentUvRect.width;
                    _curAreaTop = currentUvRect.y;
                    _curAreaBottom = currentUvRect.y + currentUvRect.height;

                    if (PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
                    {
                        PlayerMatchView.Instance.SetpersonPlayerReadyPartitionRectf(_curPlayerCount, personPlayerReadyRectf);
                    }
                    else
                    {
                        ResetJudgmentArea();
                    }
                }
            }
        }

        if (isCheckPersonReadyIng)
        {
            CheckPersonReadyIng(0, currentFramePersonIds);
            if (isCheckPersonReadyIng)
            {
                for (int i = 0; i < _cachedOffsetPlayerTransformX.Length; i++)
                {
                    if (!IsSeatTrackedForCurrentPhase(i))
                    {
                        _cachedOffsetPlayerTransformX[i] = -1;
                        _cachedPlayerRotationOffset[i] = 0f;
                        continue;
                    }
                    int lockId = personPlayerLockIng[i];
                    int playId = personPlayerIds[i];
                    int targetId = (lockId != PersonIdNull) ? lockId : playId;

                    if (targetId == PersonIdNull)
                    {
                        _cachedOffsetPlayerTransformX[i] = -1;
                        _cachedPlayerRotationOffset[i] = 0f;
                    }
                    else
                    {
                        if (playerKeyPoints.TryGetValue(targetId, out PlayerFullData playerFullData))
                        {
                            float rawX = playerFullData.keyPointListPose[0, 0];
                            // 根据实际显示区域映射坐标
                            _cachedOffsetPlayerTransformX[i] = (rawX - _curAreaLeft) / (_curAreaRight - _curAreaLeft);
                            _cachedPlayerRotationOffset[i] = ComputeRotationOffset(i, playerFullData);
                        }
                        else
                        {
                            _cachedOffsetPlayerTransformX[i] = -1;
                        }
                    }
                }
                onFollowPlayerTransform?.Invoke(_cachedOffsetPlayerTransformX);
                onFollowPlayerRotation?.Invoke(_cachedPlayerRotationOffset);
            }
        }
        else
        {
            RecoverMissingPlayers();

            for (int i = 0; i < _curPlayerCount; i++)
            {
                if (!IsSeatTrackedForCurrentPhase(i))
                {
                    _cachedOffsetPlayerTransformX[i] = -1;
                    _cachedPlayerRotationOffset[i] = 0f;
                    continue;
                }
                int playerId = personPlayerIds[i];
                if (playerId == PersonIdNull)
                {
                    _cachedOffsetPlayerTransformX[i] = -1;
                    _cachedPlayerRotationOffset[i] = 0f;
                }
                else
                {
                    if (playerKeyPoints.TryGetValue(playerId, out PlayerFullData playerFullData))
                    {
                        float rawX = playerFullData.keyPointListPose[0, 0];
                        // 根据实际显示区域映射坐标
                        _cachedOffsetPlayerTransformX[i] = (rawX - _curAreaLeft) / (_curAreaRight - _curAreaLeft);
                        _cachedPlayerRotationOffset[i] = ComputeRotationOffset(i, playerFullData);
                        if (!isCheckPersonReadyIng)
                        {
                            EvaluateAttackActions(i, playerFullData);
                        }
                    }
                    else
                    {
                        _cachedOffsetPlayerTransformX[i] = -1;
                    }
                }
            }
            onFollowPlayerTransform?.Invoke(_cachedOffsetPlayerTransformX);
            onFollowPlayerRotation?.Invoke(_cachedPlayerRotationOffset);

            if (CleanupMissingPlayersWhenReaday(currentFramePersonIds))
            {
                dealyFrame++;
                if (dealyFrame > 5)
                {
                    _cachedCleanPlayerList.Clear();
                    for (int i = 0; i < personPlayerIds.Length; i++)
                    {
                        if (IsSeatTrackedForCurrentPhase(i) && personPlayerIds[i] == PersonIdNull)
                        {
                            _cachedCleanPlayerList.Add(i);
                        }
                    }
                    // 移除对 isCheckPersonReadyIng 的限制，使游戏中也能检测丢失
                    onPlayerDisappeared?.Invoke(_cachedCleanPlayerList.ToArray());

                    playerDisappeared = true;
                    dealyFrame = 0;
                }
            }
            else
            {
                dealyFrame = 0;
                if (playerDisappeared)
                {
                    _cachedCleanPlayerList.Clear();
                    for (int i = 0; i < personPlayerIds.Length; i++)
                    {
                        if (IsSeatTrackedForCurrentPhase(i) && personPlayerIds[i] != PersonIdNull)
                        {
                            _cachedCleanPlayerList.Add(i);
                        }
                    }

                    if (_cachedCleanPlayerList.Count > 0)
                    {
                        // 移除对 isCheckPersonReadyIng 的限制
                        onPlayerReviced?.Invoke(_cachedCleanPlayerList.ToArray());
                    }

                    if (_cachedCleanPlayerList.Count >= _curPlayerCount)
                    {
                        playerDisappeared = false;
                    }
                }
            }
        }

        if (PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
        {
            for (int i = 0; i < personPlayerLockIng.Length; i++)//PartitionView 分割区域模式时需要调用
            {
                int drawPlayerId = ResolvePlayerIdForDrawRect(i);
                if (drawPlayerId == PersonIdNull)
                {
                    PlayerMatchView.Instance.SetPersonPlayerReadyDrawPartitionRectf(null, i);
                    continue;
                }

                if (playerKeyPoints.TryGetValue(drawPlayerId, out PlayerFullData playerFullData) && playerFullData.keyPointListPose != null)
                {
                    PlayerMatchView.Instance.SetPersonPlayerReadyDrawPartitionRectf(playerFullData.keyPointListPose, i);
                }
                else
                {
                    PlayerMatchView.Instance.SetPersonPlayerReadyDrawPartitionRectf(null, i);
                }
            }

        }
        else if (!_hasLoggedMissingPlayerTextureShow)
        {
            Debug.LogWarning($"{LogTag} GameLogic 发现 playerTextuerShow 为空");
            _hasLoggedMissingPlayerTextureShow = true;
        }
        if (playerTextuerShow != null && PlayerMatchView.Instance.CheckCameraTextureViewManager() && playerTextuerShow.gameObject.activeSelf)
        {
            playerTextuerShow.SetCameraTextureViewBgRect(PlayerMatchView.Instance.CalculationResult());
        }

        if (playerTextuerShow != null && playerTextuerShow.gameObject.activeSelf)
        {
            playerTextuerShow.ShowCameraImage();
        }
        // 通知每帧数据刷新
        onFrameInfoRefresh?.Invoke();
    }

    private int ResolvePlayerIdForDrawRect(int seatIndex)
    {
        if (!IsSeatTrackedForCurrentPhase(seatIndex))
        {
            return PersonIdNull;
        }

        int lockedId = (personPlayerLockIng != null && seatIndex < personPlayerLockIng.Length)
            ? personPlayerLockIng[seatIndex]
            : PersonIdNull;
        int confirmedId = (personPlayerIds != null && seatIndex < personPlayerIds.Length)
            ? personPlayerIds[seatIndex]
            : PersonIdNull;

        // 裁切框需要跟随当前阶段实际使用的人：准备阶段优先使用锁定人，战斗阶段使用已确认人。
        // EnterBattleWithConfirmedPlayers 会清空 personPlayerLockIng，所以战斗头像不能只依赖锁定人。
        if (isCheckPersonReadyIng)
        {
            return lockedId != PersonIdNull ? lockedId : confirmedId;
        }

        return confirmedId != PersonIdNull ? confirmedId : lockedId;
    }




    private float ComputeRotationOffset(int seatIndex, PlayerFullData data)
    {
        if (data == null || data.keyPointListPose == null)
        {
            return _cachedPlayerRotationOffset[seatIndex];
        }

        float[,] kp = data.keyPointListPose;
        float noseX = kp[(int)KeyPointIndex.Nose, 0];
        float noseConf = kp[(int)KeyPointIndex.Nose, 3];
        float lShX = kp[(int)KeyPointIndex.Leftshoulder, 0];
        float lShConf = kp[(int)KeyPointIndex.Leftshoulder, 3];
        float rShX = kp[(int)KeyPointIndex.Rightshoulder, 0];
        float rShConf = kp[(int)KeyPointIndex.Rightshoulder, 3];

        if (lShConf < KeypointConfidenceThreshold || rShConf < KeypointConfidenceThreshold)
        {
            return _cachedPlayerRotationOffset[seatIndex];
        }

        float shoulderMid = (lShX + rShX) * 0.5f;
        float shoulderWidth = Mathf.Abs(lShX - rShX) + ShoulderWidthEpsilon;

        if (lShConf > MaxShoulderWidthUpdateConfidence && rShConf > MaxShoulderWidthUpdateConfidence)
        {
            if (shoulderWidth > _maxShoulderWidth[seatIndex])
            {
                _maxShoulderWidth[seatIndex] = shoulderWidth;
            }
        }

        float noseOffset = (noseX - shoulderMid) / shoulderWidth;
        float rawValue = Mathf.Clamp(noseOffset * 2.5f, -1f, 1f);

        float maxW = _maxShoulderWidth[seatIndex];
        float ratio = (maxW > ShoulderWidthEpsilon) ? Mathf.Clamp01(shoulderWidth / maxW) : 1f;
        float angleFactor = 1f - ratio;
        float shoulderValue = Mathf.Clamp(angleFactor * 1.5f, 0f, 1f) * Mathf.Sign(noseOffset);

        float target = Mathf.Lerp(rawValue, shoulderValue, 0.3f);

        if (noseConf < KeypointConfidenceThreshold)
        {
            return _cachedPlayerRotationOffset[seatIndex];
        }

        float smoothed = Mathf.Lerp(
            _cachedPlayerRotationOffset[seatIndex], target,
            Mathf.Clamp01(Time.deltaTime * RotationSmoothFactor));
        return smoothed;
    }

    private void EvaluateAttackActions(int seatIndex, PlayerFullData data)
    {
        if (data == null || data.keyPointListPose == null)
        {
            return;
        }

        if (seatIndex < 0 || seatIndex >= _playerAttackStates.Length)
        {
            return;
        }

        PlayerAttackState state = _playerAttackStates[seatIndex];
        if (state == null)
        {
            return;
        }

        state.frameCounter++;

        float[,] kp = data.keyPointListPose;
        float currentTime = Time.time;
        float deltaTime = (state.lastFrameTime > 0f) ? (currentTime - state.lastFrameTime) : 0f;
        state.lastFrameTime = currentTime;

        float lShX = kp[(int)KeyPointIndex.Leftshoulder, 0];
        float lShY = kp[(int)KeyPointIndex.Leftshoulder, 1];
        float lShConf = kp[(int)KeyPointIndex.Leftshoulder, 3];
        float rShX = kp[(int)KeyPointIndex.Rightshoulder, 0];
        float rShY = kp[(int)KeyPointIndex.Rightshoulder, 1];
        float rShConf = kp[(int)KeyPointIndex.Rightshoulder, 3];

        if (lShConf < AttackKeypointMinConfidence || rShConf < AttackKeypointMinConfidence)
        {
            state.hasLastLeftWrist = false;
            state.hasLastRightWrist = false;
            return;
        }

        float shoulderWidth = Mathf.Abs(rShX - lShX) + ShoulderWidthEpsilon;
        state.lastShoulderWidth = shoulderWidth;

        EvaluatePunch(seatIndex, state, kp, deltaTime, currentTime);
        EvaluateOverheadThrow(seatIndex, state, kp, shoulderWidth, deltaTime, currentTime);
    }

    private void EvaluatePunch(
        int seatIndex,
        PlayerAttackState state,
        float[,] kp,
        float deltaTime,
        float currentTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        float lWrX = kp[(int)KeyPointIndex.Leftwrist, 0];
        float lWrY = kp[(int)KeyPointIndex.Leftwrist, 1];
        float lWrConf = kp[(int)KeyPointIndex.Leftwrist, 3];
        float rWrX = kp[(int)KeyPointIndex.Rightwrist, 0];
        float rWrY = kp[(int)KeyPointIndex.Rightwrist, 1];
        float rWrConf = kp[(int)KeyPointIndex.Rightwrist, 3];

        float shoulderWidth = state.lastShoulderWidth;
        float speedThreshold = shoulderWidth * PunchSpeedRatioPerSecond;

        if (lWrConf >= AttackKeypointMinConfidence)
        {
            Vector2 currentLeft = new Vector2(lWrX, lWrY);
            if (state.hasLastLeftWrist)
            {
                float leftSpeed = Vector2.Distance(currentLeft, state.lastLeftWrist) / deltaTime;
                if (leftSpeed >= speedThreshold &&
                    currentTime - state.lastLeftPunchTime >= PunchCooldownSeconds)
                {
                    state.lastLeftPunchTime = currentTime;
                    int combo = UpdateAlternatingCombo(state, PunchHandLeft, currentTime);
                    onPlayerNormalAttack?.Invoke(seatIndex, combo);
                }
            }
            state.lastLeftWrist = currentLeft;
            state.hasLastLeftWrist = true;
        }
        else
        {
            state.hasLastLeftWrist = false;
        }

        if (rWrConf >= AttackKeypointMinConfidence)
        {
            Vector2 currentRight = new Vector2(rWrX, rWrY);
            if (state.hasLastRightWrist)
            {
                float rightSpeed = Vector2.Distance(currentRight, state.lastRightWrist) / deltaTime;
                if (rightSpeed >= speedThreshold &&
                    currentTime - state.lastRightPunchTime >= PunchCooldownSeconds)
                {
                    state.lastRightPunchTime = currentTime;
                    int combo = UpdateAlternatingCombo(state, PunchHandRight, currentTime);
                    onPlayerNormalAttack?.Invoke(seatIndex, combo);
                }
            }
            state.lastRightWrist = currentRight;
            state.hasLastRightWrist = true;
        }
        else
        {
            state.hasLastRightWrist = false;
        }
    }

    private int UpdateAlternatingCombo(
        PlayerAttackState state,
        int punchHand,
        float currentTime)
    {
        if (state.lastPunchHand != PunchHandNone &&
            state.lastPunchHand != punchHand &&
            state.frameCounter - (int)state.lastPunchTime <= AlternatingPunchWindowFrames)
        {
            state.alternatingCombo++;
        }
        else
        {
            state.alternatingCombo = 1;
        }

        state.lastPunchHand = punchHand;
        state.lastPunchTime = state.frameCounter;
        return state.alternatingCombo;
    }

    private void EvaluateOverheadThrow(
        int seatIndex,
        PlayerAttackState state,
        float[,] kp,
        float shoulderWidth,
        float deltaTime,
        float currentTime)
    {
        if (currentTime < state.throwCooldownUntil)
        {
            return;
        }

        float lWrX = kp[(int)KeyPointIndex.Leftwrist, 0];
        float lWrY = kp[(int)KeyPointIndex.Leftwrist, 1];
        float lWrConf = kp[(int)KeyPointIndex.Leftwrist, 3];
        float rWrX = kp[(int)KeyPointIndex.Rightwrist, 0];
        float rWrY = kp[(int)KeyPointIndex.Rightwrist, 1];
        float rWrConf = kp[(int)KeyPointIndex.Rightwrist, 3];

        if (lWrConf < AttackKeypointMinConfidence || rWrConf < AttackKeypointMinConfidence)
        {
            state.throwWindUpCount = 0;
            state.throwArmed = false;
            return;
        }

        float noseY = kp[(int)KeyPointIndex.Nose, 1];
        float noseConf = kp[(int)KeyPointIndex.Nose, 3];
        float leyeY = kp[(int)KeyPointIndex.Lefteye, 1];
        float leyeConf = kp[(int)KeyPointIndex.Lefteye, 3];
        float reyeY = kp[(int)KeyPointIndex.Righteye, 1];
        float reyeConf = kp[(int)KeyPointIndex.Righteye, 3];

        float headTopY = noseY;
        bool hasHeadTop = false;
        if (noseConf >= AttackKeypointMinConfidence)
        {
            headTopY = noseY;
            hasHeadTop = true;
        }
        if (leyeConf >= AttackKeypointMinConfidence)
        {
            headTopY = hasHeadTop ? Mathf.Min(headTopY, leyeY) : leyeY;
            hasHeadTop = true;
        }
        if (reyeConf >= AttackKeypointMinConfidence)
        {
            headTopY = hasHeadTop ? Mathf.Min(headTopY, reyeY) : reyeY;
            hasHeadTop = true;
        }

        if (!hasHeadTop)
        {
            state.throwWindUpCount = 0;
            state.throwArmed = false;
            return;
        }

        float overheadThreshold = headTopY - shoulderWidth * ThrowMarginRatio;
        bool bothOverhead = lWrY <= overheadThreshold && rWrY <= overheadThreshold;

        if (!state.throwArmed)
        {
            if (bothOverhead)
            {
                state.throwWindUpCount++;
                if (state.throwWindUpCount >= ThrowWindUpFrames)
                {
                    state.throwArmed = true;
                }
            }
            else
            {
                state.throwWindUpCount = 0;
            }
            return;
        }

        if (bothOverhead)
        {
            state.throwWindUpCount++;
            return;
        }

        if (deltaTime <= 0f)
        {
            return;
        }

        float releaseThreshold = shoulderWidth * ThrowReleaseSpeedRatio;
        float leftDownSpeed = 0f;
        float rightDownSpeed = 0f;

        if (state.hasLastLeftWrist)
        {
            leftDownSpeed = Mathf.Max(0f, (lWrY - state.lastLeftWrist.y) / deltaTime);
        }
        if (state.hasLastRightWrist)
        {
            rightDownSpeed = Mathf.Max(0f, (rWrY - state.lastRightWrist.y) / deltaTime);
        }

        if (leftDownSpeed >= releaseThreshold && rightDownSpeed >= releaseThreshold)
        {
            onPlayerSkillAttack?.Invoke(seatIndex);
            state.throwArmed = false;
            state.throwWindUpCount = 0;
            state.throwCooldownUntil = currentTime + ThrowCooldownSeconds;
        }
        else if (leftDownSpeed < releaseThreshold * 0.3f && rightDownSpeed < releaseThreshold * 0.3f)
        {
            state.throwArmed = false;
            state.throwWindUpCount = 0;
        }
    }

    private void RecoverMissingPlayers()
    {
        _cachedExcludedIds.Clear();
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (personPlayerIds[i] != PersonIdNull)
            {
                _cachedExcludedIds.Add(personPlayerIds[i]);
            }
        }

        if (_cachedExcludeArray == null || _cachedExcludeArray.Length < _cachedExcludedIds.Count)
        {
            _cachedExcludeArray = new int[Math.Max(_cachedExcludedIds.Count, _curPlayerCount * 2)];
        }
        _cachedExcludedIds.CopyTo(_cachedExcludeArray);
        int excludeCount = _cachedExcludedIds.Count;

        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (!IsSeatTrackedForCurrentPhase(i) || personPlayerIds[i] != PersonIdNull)
            {
                continue;
            }

            float left = personPlayerReadyRectf[i, 0];
            float top = personPlayerReadyRectf[i, 1];
            float right = personPlayerReadyRectf[i, 2];
            float bottom = personPlayerReadyRectf[i, 3];

            int foundPersonId = CheckPersonScoreInArea(
                0,
                left, top, right, bottom,
                _cachedExcludeArray, excludeCount
            );

            if (foundPersonId == PersonIdNull)
            {
                continue;
            }

            if (PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
            {
                int closestId = FindClosestPersonInPartition(left, top, right, bottom, foundPersonId, _cachedExcludeArray, excludeCount);
                if (closestId != PersonIdNull)
                {
                    foundPersonId = closestId;
                }
            }

            personPlayerIds[i] = foundPersonId;

            _cachedExcludedIds.Add(foundPersonId);
            if (_cachedExcludeArray.Length < _cachedExcludedIds.Count)
            {
                _cachedExcludeArray = new int[_cachedExcludedIds.Count * 2];
                _cachedExcludedIds.CopyTo(_cachedExcludeArray);
            }
            else
            {
                _cachedExcludeArray[excludeCount] = foundPersonId;
            }
            excludeCount++;

            Debug.Log($"游戏过程中玩家恢复: Slot {i}, PlayerId {foundPersonId}");
        }
    }

    protected override bool CleanupMissingPlayersWhenReaday(int[] currentFramePersonIds)
    {
        bool hasCleanup = false;
        var currentPersonSet = new HashSet<int>();

        foreach (int id in currentFramePersonIds)
        {
            if (id != PersonIdNull)
                currentPersonSet.Add(id);
        }

        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            int confirmedId = personPlayerIds[i];
            int lockedId = personPlayerLockIng[i];
            int activeId = PersonIdNull;
            bool isConfirmed = false;

            if (confirmedId != PersonIdNull)
            {
                activeId = confirmedId;
                isConfirmed = true;
            }
            else if (lockedId != PersonIdNull)
            {
                activeId = lockedId;
                isConfirmed = false;
            }

            if (activeId != PersonIdNull)
            {
                if (!currentPersonSet.Contains(activeId))
                {
                    personMissingFrameCount[i]++;
                    if (personMissingFrameCount[i] > missingFrameThreshold)
                    {
                        personHandsUpFrameCount[i] = 0;
                        personMissingFrameCount[i] = 0;

                        if (isConfirmed)
                        {
                            personPlayerIds[i] = PersonIdNull;
                        }
                        else
                        {
                            personPlayerLockIng[i] = PersonIdNull;
                        }
                        hasCleanup = true;
                    }
                }
                else
                {
                    personMissingFrameCount[i] = 0;
                }
            }
            else
            {
                personMissingFrameCount[i] = 0;
            }
        }

        return hasCleanup;
    }


    /// <summary>
    /// 锁定各个区域中的人 2025.10.09 。 
    /// </summary>
    protected override void LockPlayersInRegions(int frameIndex, int[] currentFramePersonIds)
    {
        int excludeCount = 0;
        for (int i = 0; i < _curPlayerCount; i++)//首先先把已经锁定的人拿出来 以方便排除
        {
            if (!IsSeatTrackedForCurrentPhase(i))
            {
                personPlayerIds[i] = PersonIdNull;
                personPlayerLockIng[i] = PersonIdNull;
                ResetPrepareSeatState(i);
                continue;
            }
            int playerId = personPlayerIds[i];
            if (playerId == PersonIdNull)
            {
                playerId = personPlayerLockIng[i];
            }

            // 准备阶段同样保持“骨骼不彻底丢失就不释放控制权”的规则。
            // 区域只用于初次匹配或重新接管，不再用于已锁定骨骼的即时清理。
            if (TryAppendTrackedPersonToExclude(playerId, ref excludeCount))
            {
                continue;
            }
        }

        for (int i = 0; i < _curPlayerCount; i++)
        {
            if (!IsSeatTrackedForCurrentPhase(i))
            {
                personPlayerLockIng[i] = PersonIdNull;
                ResetPrepareSeatState(i);
                continue;
            }
            if (personPlayerIds[i] != PersonIdNull)//已经存在匹配后的玩家就不用管
            {
                personPlayerLockIng[i] = PersonIdNull;
                MarkPrepareSeatReady(i, personPlayerIds[i]);
                continue;
            }
            float left = personPlayerReadyRectf[i, 0];
            float top = personPlayerReadyRectf[i, 1];
            float right = personPlayerReadyRectf[i, 2];
            float bottom = personPlayerReadyRectf[i, 3];
            int foundPersonId = CheckPersonScoreInArea(frameIndex, left, top, right, bottom, _tempExcludeIdArray, excludeCount);

            if (foundPersonId != PersonIdNull && PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
            {
                int closestId = FindClosestPersonInPartition(left, top, right, bottom, foundPersonId, _tempExcludeIdArray, excludeCount);
                if (closestId != PersonIdNull)
                {
                    foundPersonId = closestId;
                }
            }

            if (foundPersonId == PersonIdNull)//如果找出来的人和置信度最高的是一个人 或者 没有找到人 则跳过下面的流程
            {
                continue;
            }

            if (personPlayerLockIng[i] == foundPersonId)
            {
                continue;
            }

            // 如果当前正在准备阶段，检查该玩家是否双手抱胸（不玩游戏）
            bool isNotGamePose = false;
            if (isCheckPersonReadyIng && playerKeyPoints.TryGetValue(foundPersonId, out PlayerFullData foundPlayerData))
            {
                isNotGamePose = (foundPlayerData.poseType & 4) != 0;
            }

            bool isAddNewPerson = false;
            if (personPlayerLockIng[i] == PersonIdNull && !isNotGamePose)
            {
                personPlayerLockIng[i] = foundPersonId;
                isAddNewPerson = true;
                BeginPrepareSeatWaitCenter(i, foundPersonId);
            }
            if (isAddNewPerson && excludeCount < _tempExcludeIdArray.Length)
            {
                _tempExcludeIdArray[excludeCount++] = foundPersonId;
            }

        }

        for (int i = 0; i < personPlayerLockIng.Length; i++)
        {
            if (!IsSlotEnabled(i))
            {
                continue;
            }
            if (personPlayerLockIng[i] == PersonIdNull && personPlayerIds[i] == PersonIdNull)
            {
                if (_detectionStates[i] != DetectionState.Empty)
                {
                    // 移除对 isCheckPersonReadyIng 的限制
                    onNoneIsArea?.Invoke(i);

                    _detectionStates[i] = DetectionState.Empty;
                }
                ResetPrepareSeatState(i);
            }
        }
    }

    private bool TryAppendTrackedPersonToExclude(int personId, ref int excludeCount)
    {
        if (personId == PersonIdNull)
        {
            return false;
        }

        if (!playerKeyPoints.TryGetValue(personId, out PlayerFullData playerFullData) ||
            playerFullData.keyPointListPose == null)
        {
            return false;
        }

        if (excludeCount >= _tempExcludeIdArray.Length)
        {
            return false;
        }

        _tempExcludeIdArray[excludeCount++] = personId;
        return true;
    }



    /// <summary>
    /// 当同一分区内存在多个骨骼时，选取包围盒中心点距离分区中心最近的骨骼
    /// </summary>
    private int FindClosestPersonInPartition(float areaLeft, float areaTop, float areaRight, float areaBottom,
        int nativeFoundId, int[] excludeIds, int excludeCount)
    {
        float centerX = (areaLeft + areaRight) / 2f;
        float minDistance = float.MaxValue;
        int bestId = nativeFoundId;

        foreach (var kv in playerKeyPoints)
        {
            int personId = kv.Key;
            PlayerFullData data = kv.Value;

            if (personId == PersonIdNull || data.keyPointListPose == null)
                continue;

            bool excluded = false;
            for (int e = 0; e < excludeCount; e++)
            {
                if (excludeIds[e] == personId)
                {
                    excluded = true;
                    break;
                }
            }
            if (excluded)
                continue;

            float personX = data.keyPointListPose[0, 0];
            float personY = data.keyPointListPose[0, 1];
            if (personX < areaLeft || personX > areaRight || personY < areaTop || personY > areaBottom)
                continue;

            float dist = Mathf.Abs(personX - centerX);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestId = personId;
            }
        }

        return bestId;
    }

    public PrepareMatchSeatState GetPrepareSeatState(int seatIndex)
    {
        return GetPrepareSeatStateInternal(seatIndex);
    }

    public PrepareMatchStep GetPrepareSeatStep(int seatIndex)
    {
        var state = GetPrepareSeatStateInternal(seatIndex);
        return state != null ? state.m_Step : PrepareMatchStep.Empty;
    }

    private PrepareMatchSeatState GetPrepareSeatStateInternal(int seatIndex)
    {
        if (m_PrepareSeatStates == null ||
            seatIndex < 0 ||
            seatIndex >= m_PrepareSeatStates.Length)
        {
            return null;
        }

        return m_PrepareSeatStates[seatIndex];
    }

    private void ResetPrepareSeatState(int seatIndex)
    {
        var state = GetPrepareSeatStateInternal(seatIndex);
        if (state == null)
        {
            return;
        }

        var oldStep = state.m_Step;
        var oldPersonId = state.m_PersonId;
        state.Reset(seatIndex);
        if (oldStep != PrepareMatchStep.Empty || oldPersonId != PersonIdNull)
        {
            NotifyPrepareSeatStateChanged(seatIndex);
        }
    }

    private void BeginPrepareSeatWaitCenter(int seatIndex, int personId)
    {
        SetPrepareSeatState(seatIndex, personId, PrepareMatchStep.WaitCenter, true);
    }

    private void BeginPrepareSeatFaceRecognizing(int seatIndex, int personId)
    {
        if (SetPrepareSeatState(seatIndex, personId, PrepareMatchStep.FaceRecognizing, true))
        {
            onPlayerFaceRecognizing?.Invoke(seatIndex);
            UpdatePrepareFaceRecognitionRunning();
        }
    }

    private bool SetPrepareSeatFaceRecognized(int seatIndex, long userId, string facePhotoPath)
    {
        var state = GetPrepareSeatStateInternal(seatIndex);
        if (state == null || state.m_Step != PrepareMatchStep.FaceRecognizing)
        {
            return false;
        }

        if (userId > 0 && IsUserAssignedToOtherPrepareSeat(seatIndex, userId))
        {
            return false;
        }

        state.m_UserId = userId;
        state.m_FacePhotoPath = facePhotoPath;
        state.m_Step = PrepareMatchStep.WaitRaiseHand;
        state.m_StateStartTime = Time.time;
        NotifyPrepareSeatStateChanged(seatIndex);
        UpdatePrepareFaceRecognitionRunning();
        return true;
    }

    private bool IsUserAssignedToOtherPrepareSeat(int seatIndex, long userId)
    {
        if (m_PrepareSeatStates == null)
        {
            return false;
        }

        for (int i = 0; i < m_PrepareSeatStates.Length; i++)
        {
            var other = m_PrepareSeatStates[i];
            if (i == seatIndex || other == null || other.m_UserId != userId)
            {
                continue;
            }

            if (other.m_Step == PrepareMatchStep.WaitRaiseHand || other.m_Step == PrepareMatchStep.Ready)
            {
                return true;
            }
        }

        return false;
    }

    private void MarkPrepareSeatReady(int seatIndex, int personId)
    {
        SetPrepareSeatState(seatIndex, personId, PrepareMatchStep.Ready, false);
        UpdatePrepareFaceRecognitionRunning();
    }

    private bool SetPrepareSeatState(int seatIndex, int personId, PrepareMatchStep step, bool clearFaceData)
    {
        var state = GetPrepareSeatStateInternal(seatIndex);
        if (state == null)
        {
            return false;
        }

        bool changed = state.m_Step != step || state.m_PersonId != personId;
        state.m_SeatId = seatIndex;
        state.m_SdkSlotId = seatIndex;
        state.m_PersonId = personId;
        if (clearFaceData)
        {
            state.m_UserId = 0;
            state.m_FacePhotoPath = null;
        }
        if (changed)
        {
            state.m_Step = step;
            state.m_StateStartTime = Time.time;
            NotifyPrepareSeatStateChanged(seatIndex);
        }
        return changed;
    }

    private bool IsPrepareSeatFaceReady(int seatIndex, int personId)
    {
        var state = GetPrepareSeatStateInternal(seatIndex);
        return state != null &&
            state.m_PersonId == personId &&
            (state.m_Step == PrepareMatchStep.WaitRaiseHand || state.m_Step == PrepareMatchStep.Ready);
    }

    private void UpdatePrepareSeatFaceRecognizeTimeout(int seatIndex)
    {
        var state = GetPrepareSeatStateInternal(seatIndex);
        if (state == null || state.m_Step != PrepareMatchStep.FaceRecognizing)
        {
            return;
        }

        if (prepareFaceRecognizeTimeoutSeconds <= 0f ||
            Time.time - state.m_StateStartTime < prepareFaceRecognizeTimeoutSeconds)
        {
            return;
        }

        state.m_StateStartTime = Time.time;
        NotifyPrepareSeatStateChanged(seatIndex);
        onPlayerFaceRecognizeFailed?.Invoke(seatIndex);
    }

    private void NotifyPrepareSeatStateChanged(int seatIndex)
    {
        var state = GetPrepareSeatStateInternal(seatIndex);
        if (state == null)
        {
            return;
        }

        RefreshPrepareSeatVisualByStep(seatIndex);
        onPrepareSeatStateChanged?.Invoke(seatIndex, state);
    }

    private void UpdatePrepareFaceRecognitionRunning()
    {
        bool shouldRun = false;
        if (m_PrepareSeatStates != null && isCheckPersonReadyIng)
        {
            for (int i = 0; i < m_PrepareSeatStates.Length; i++)
            {
                if (m_PrepareSeatStates[i] != null && m_PrepareSeatStates[i].m_Step == PrepareMatchStep.FaceRecognizing)
                {
                    shouldRun = true;
                    break;
                }
            }
        }

        if (shouldRun == m_PrepareFaceRecognitionRunning)
        {
            return;
        }

        m_PrepareFaceRecognitionRunning = shouldRun;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (AndroidServerInfoDemo.Instance == null)
        {
            return;
        }

        if (shouldRun)
        {
            AndroidServerInfoDemo.Instance.StartFaceRecognitionCurrentFrameInfo();
        }
        else
        {
            AndroidServerInfoDemo.Instance.StopFaceRecognitionCurrentFrameInfo();
        }
#endif
    }

    private void ResetPrepareRoleSelectState(int seatIndex)
    {
        if (_prepareRoleSelectHasLeftWrist == null ||
            seatIndex < 0 ||
            seatIndex >= _prepareRoleSelectHasLeftWrist.Length)
        {
            return;
        }

        _prepareRoleSelectHasLeftWrist[seatIndex] = false;
        _prepareRoleSelectHasRightWrist[seatIndex] = false;
        _prepareRoleSelectAnchorLeftWristX[seatIndex] = 0f;
        _prepareRoleSelectAnchorRightWristX[seatIndex] = 0f;
        _prepareRoleSelectLastLeftWristX[seatIndex] = 0f;
        _prepareRoleSelectLastRightWristX[seatIndex] = 0f;
        _prepareRoleSelectLastLeftWristTime[seatIndex] = 0f;
        _prepareRoleSelectLastRightWristTime[seatIndex] = 0f;
        if (_prepareRoleSelectCooldownUntil != null && seatIndex < _prepareRoleSelectCooldownUntil.Length)
        {
            _prepareRoleSelectCooldownUntil[seatIndex] = 0f;
        }
    }

    private void EvaluatePrepareRoleSelection(int seatIndex, PlayerFullData data)
    {
        if (data == null || data.keyPointListPose == null ||
            _prepareRoleSelectCooldownUntil == null ||
            seatIndex < 0 ||
            seatIndex >= _prepareRoleSelectCooldownUntil.Length)
        {
            return;
        }

        float[,] kp = data.keyPointListPose;
        if (!TryReadPrepareGestureMetrics(kp, out float shoulderWidth, out float headTopY))
        {
            ResetPrepareRoleSelectState(seatIndex);
            return;
        }

        // 举手准备和挥手选人互斥，但准备动作要按“手过头顶”判断，不能直接信任 poseType。
        if (IsPrepareReadyPose(kp, shoulderWidth, headTopY))
        {
            ResetPrepareRoleSelectState(seatIndex);
            return;
        }

        float waveDistance = shoulderWidth * PrepareRoleSelectWaveDistanceRatio;
        float resetDistance = shoulderWidth * PrepareRoleSelectResetDistanceRatio;
        float speedThreshold = shoulderWidth * _prepareRoleSelectWaveSpeedRatioPerSecond;
        float currentTime = Time.time;
        int direction = 0;
        float maxDistance = 0f;

        EvaluatePrepareRoleSelectionWrist(
            kp[(int)KeyPointIndex.Leftwrist, 0],
            kp[(int)KeyPointIndex.Leftwrist, 3],
            waveDistance,
            resetDistance,
            speedThreshold,
            currentTime,
            ref _prepareRoleSelectHasLeftWrist[seatIndex],
            ref _prepareRoleSelectAnchorLeftWristX[seatIndex],
            ref _prepareRoleSelectLastLeftWristX[seatIndex],
            ref _prepareRoleSelectLastLeftWristTime[seatIndex],
            ref direction,
            ref maxDistance);
        EvaluatePrepareRoleSelectionWrist(
            kp[(int)KeyPointIndex.Rightwrist, 0],
            kp[(int)KeyPointIndex.Rightwrist, 3],
            waveDistance,
            resetDistance,
            speedThreshold,
            currentTime,
            ref _prepareRoleSelectHasRightWrist[seatIndex],
            ref _prepareRoleSelectAnchorRightWristX[seatIndex],
            ref _prepareRoleSelectLastRightWristX[seatIndex],
            ref _prepareRoleSelectLastRightWristTime[seatIndex],
            ref direction,
            ref maxDistance);

        if (direction == 0 || currentTime < _prepareRoleSelectCooldownUntil[seatIndex])
        {
            return;
        }

        _prepareRoleSelectCooldownUntil[seatIndex] = currentTime + PrepareRoleSelectWaveCooldownSeconds;
        if (direction < 0)
        {
            onPlayerSelectRoleLeft?.Invoke(seatIndex);
        }
        else
        {
            onPlayerSelectRoleRight?.Invoke(seatIndex);
        }
    }

    private void EvaluatePrepareRoleSelectionWrist(
        float wristX,
        float wristConfidence,
        float waveDistance,
        float resetDistance,
        float speedThreshold,
        float currentTime,
        ref bool hasLastWrist,
        ref float anchorWristX,
        ref float lastWristX,
        ref float lastWristTime,
        ref int direction,
        ref float maxDistance)
    {
        if (wristConfidence < AttackKeypointMinConfidence)
        {
            hasLastWrist = false;
            lastWristTime = 0f;
            return;
        }

        if (!hasLastWrist)
        {
            anchorWristX = wristX;
            lastWristX = wristX;
            lastWristTime = currentTime;
            hasLastWrist = true;
            return;
        }

        float deltaTime = Mathf.Max(0.0001f, currentTime - lastWristTime);
        float frameOffset = wristX - lastWristX;
        float frameSpeed = Mathf.Abs(frameOffset) / deltaTime;
        float anchorOffset = wristX - anchorWristX;
        float absAnchorOffset = Mathf.Abs(anchorOffset);
        if (absAnchorOffset <= resetDistance || frameSpeed < speedThreshold * 0.5f)
        {
            anchorWristX = wristX;
            lastWristX = wristX;
            lastWristTime = currentTime;
            return;
        }

        if (absAnchorOffset >= waveDistance &&
            frameSpeed >= speedThreshold &&
            absAnchorOffset > maxDistance)
        {
            maxDistance = absAnchorOffset;
            direction = anchorOffset < 0f ? -1 : 1;
            anchorWristX = wristX;
            lastWristX = wristX;
            lastWristTime = currentTime;
            return;
        }

        lastWristX = wristX;
        lastWristTime = currentTime;
    }

    private static bool TryReadPrepareGestureMetrics(float[,] kp, out float shoulderWidth, out float headTopY)
    {
        shoulderWidth = 0f;
        headTopY = 0f;
        if (kp == null)
        {
            return false;
        }

        float lShX = kp[(int)KeyPointIndex.Leftshoulder, 0];
        float lShConf = kp[(int)KeyPointIndex.Leftshoulder, 3];
        float rShX = kp[(int)KeyPointIndex.Rightshoulder, 0];
        float rShConf = kp[(int)KeyPointIndex.Rightshoulder, 3];
        if (lShConf < AttackKeypointMinConfidence || rShConf < AttackKeypointMinConfidence)
        {
            return false;
        }

        shoulderWidth = Mathf.Abs(rShX - lShX) + ShoulderWidthEpsilon;

        float noseY = kp[(int)KeyPointIndex.Nose, 1];
        float noseConf = kp[(int)KeyPointIndex.Nose, 3];
        float lEyeY = kp[(int)KeyPointIndex.Lefteye, 1];
        float lEyeConf = kp[(int)KeyPointIndex.Lefteye, 3];
        float rEyeY = kp[(int)KeyPointIndex.Righteye, 1];
        float rEyeConf = kp[(int)KeyPointIndex.Righteye, 3];

        bool hasHeadTop = false;
        if (noseConf >= AttackKeypointMinConfidence)
        {
            headTopY = noseY;
            hasHeadTop = true;
        }
        if (lEyeConf >= AttackKeypointMinConfidence)
        {
            headTopY = hasHeadTop ? Mathf.Min(headTopY, lEyeY) : lEyeY;
            hasHeadTop = true;
        }
        if (rEyeConf >= AttackKeypointMinConfidence)
        {
            headTopY = hasHeadTop ? Mathf.Min(headTopY, rEyeY) : rEyeY;
            hasHeadTop = true;
        }

        return hasHeadTop;
    }

    private static bool IsPrepareReadyPose(float[,] kp, float shoulderWidth, float headTopY)
    {
        float threshold = headTopY - shoulderWidth * PrepareReadyOverheadMarginRatio;
        float lWrY = kp[(int)KeyPointIndex.Leftwrist, 1];
        float lWrConf = kp[(int)KeyPointIndex.Leftwrist, 3];
        float rWrY = kp[(int)KeyPointIndex.Rightwrist, 1];
        float rWrConf = kp[(int)KeyPointIndex.Rightwrist, 3];

        bool leftOverhead = lWrConf >= AttackKeypointMinConfidence && lWrY <= threshold;
        bool rightOverhead = rWrConf >= AttackKeypointMinConfidence && rWrY <= threshold;
        return leftOverhead || rightOverhead;
    }

    private static bool IsPrepareReadyPose(float[,] kp)
    {
        return TryReadPrepareGestureMetrics(kp, out float shoulderWidth, out float headTopY) &&
            IsPrepareReadyPose(kp, shoulderWidth, headTopY);
    }

    /// <summary>
    /// 检查锁定的玩家是否举手准备
    /// 包含：已确认玩家离开准备区域的检测、锁定玩家的举手检测、双手抱胸（不玩游戏）检测
    /// </summary>
    protected override void CheckLockedPlayersReady(int frameIndex)
    {
        // 第一部分：检查已确认的玩家是否离开了准备区域
        if (isCheckPersonReadyIng)
        {
            for (int i = 0; i < personPlayerIds.Length; i++)
            {
                if (!IsSeatTrackedForCurrentPhase(i))
                {
                    continue;
                }
                int confirmedPersonId = personPlayerIds[i];
                if (confirmedPersonId == PersonIdNull || personPlayerLockIng[i] != PersonIdNull)
                {
                    continue;
                }

                if (!playerKeyPoints.TryGetValue(confirmedPersonId, out PlayerFullData playerFullData))
                {
                    continue;
                }

                float playerX = playerFullData.keyPointListPose[0, 0];
                float readyAreaLeft = personPlayerReadyRectf[i, 0];
                float readyAreaRight = personPlayerReadyRectf[i, 2];
                bool isInReadyArea = playerX >= readyAreaLeft && playerX <= readyAreaRight;

                if (!isInReadyArea)
                {
                    Debug.Log($"玩家 {confirmedPersonId} (Slot {i}) 离开了准备区域，重置匹配状态");
                    ResetPrepareSeatForRegionChange(i);
                }
            }
        }

        if (!isCheckPersonReadyIng)
        {
            return;
        }

        // 第二部分：检查锁定的玩家是否举手准备
        for (int i = 0; i < personPlayerLockIng.Length; i++)
        {
            if (!IsSeatTrackedForCurrentPhase(i))
            {
                continue;
            }
            int lockedPersonId = personPlayerLockIng[i];
            if (lockedPersonId == PersonIdNull)
            {
                continue;
            }

            if (!playerKeyPoints.TryGetValue(lockedPersonId, out PlayerFullData playerFullData))
            {
                continue;
            }

            // 检查锁定的玩家是否双手抱胸（如果是，则表示不想玩游戏，取消锁定）
            if (isCheckPersonReadyIng && playerFullData.keyPointListPose != null)
            {
                if ((playerFullData.poseType & 4) == 4)
                {
                    Debug.Log($"玩家 {lockedPersonId} (Slot {i}) 双手抱胸，不玩游戏，取消锁定");

                    // 发送不玩游戏事件
                    onPlayerNotGame?.Invoke(i, 0, readyNeedFrame);

                    personHandsUpFrameCount[i] = 0;
                    ResetPrepareRoleSelectState(i);
                    personPlayerLockIng[i] = PersonIdNull;
                    ResetPrepareSeatState(i);
                    if (personWasInReadyArea[i])
                    {
                        personWasInReadyArea[i] = false;
                        onPlayerCancelReady?.Invoke(i);
                        if (_detectionStates[i] != DetectionState.OutOfArea)
                        {
                            onPlayerNotInReadyArea?.Invoke(i);
                            _detectionStates[i] = DetectionState.OutOfArea;
                        }
                    }
                    continue;
                }
            }

            float playerX = playerFullData.keyPointListPose[0, 0];
            float readyAreaLeft = personPlayerReadyRectf[i, 0];
            float readyAreaRight = personPlayerReadyRectf[i, 2];
            bool isInReadyArea = playerX >= readyAreaLeft && playerX <= readyAreaRight;

            if (!isInReadyArea)
            {
                ResetPrepareSeatForRegionChange(i);
                continue;
            }

            if (!personWasInReadyArea[i])
            {
                personHandsUpFrameCount[i] = 0;
                ResetPrepareRoleSelectState(i);
                personWasInReadyArea[i] = true;
            }

            if (lockedPersonId != PersonIdNull && personPlayerIds[i] == PersonIdNull)
            {
                if (_isReadyShow[i] < readyShowTime)
                {
                    _isReadyShow[i]++;
                    BeginPrepareSeatWaitCenter(i, lockedPersonId);
                    if (_detectionStates[i] != DetectionState.InArea)
                    {
                        onPlayerCancelReady?.Invoke(i);
                        _detectionStates[i] = DetectionState.InArea;
                    }
                    personHandsUpFrameCount[i] = 0;
                    continue;
                }
                _isReadyShow[i] = readyShowTime;

                if (!IsPrepareSeatFaceReady(i, lockedPersonId))
                {
                    var state = GetPrepareSeatStateInternal(i);
                    if (state == null || state.m_Step != PrepareMatchStep.FaceRecognizing)
                    {
                        BeginPrepareSeatFaceRecognizing(i, lockedPersonId);
                    }
                    else
                    {
                        UpdatePrepareSeatFaceRecognizeTimeout(i);
                    }

                    personHandsUpFrameCount[i] = 0;
                    ResetPrepareRoleSelectState(i);
                    continue;
                }

                EvaluatePrepareRoleSelection(i, playerFullData);

                if (IsPrepareReadyPose(playerFullData.keyPointListPose))
                {
                    personHandsUpFrameCount[i]++;
                    if (personHandsUpFrameCount[i] >= readyNeedFrame)
                    {
                        _isReadyShow[i] = 0;
                        // 先写入确认结果，再发事件，避免外部在回调里读到旧的准备状态。
                        personPlayerIds[i] = lockedPersonId;
                        personPlayerLockIng[i] = PersonIdNull;
                        MarkPrepareSeatReady(i, lockedPersonId);
                    }
                    onPlayerIsReady?.Invoke(i, personHandsUpFrameCount[i], readyNeedFrame);
                    _detectionStates[i] = DetectionState.Ready;
                    personWasInReadyArea[i] = true;
                }
                else
                {
                    if (personHandsUpFrameCount[i] > 0)
                    {
                        personHandsUpFrameCount[i] = 0;
                        onPlayerIsReady?.Invoke(i, 0, readyNeedFrame);
                    }
                    personWasInReadyArea[i] = true;
                    if (_detectionStates[i] != DetectionState.InArea)
                    {
                        onPlayerCancelReady?.Invoke(i);
                        _detectionStates[i] = DetectionState.InArea;
                    }
                }
            }
        }

        // 第三部分：清理已确认但已不在画面中的玩家
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (!IsSeatTrackedForCurrentPhase(i))
            {
                continue;
            }
            int personPlayerId = personPlayerIds[i];
            if (personPlayerId == PersonIdNull || playerKeyPoints.ContainsKey(personPlayerId))
            {
                continue;
            }
            personPlayerIds[i] = PersonIdNull;
            personPlayerLockIng[i] = PersonIdNull;
            personHandsUpFrameCount[i] = 0;
            ResetPrepareRoleSelectState(i);
            ResetPrepareSeatState(i);
            onPlayerIsReady?.Invoke(i, 0, readyNeedFrame);
        }

        UpdatePrepareFaceRecognitionRunning();
    }

    // 准备阶段的分区决定座位归属。玩家离开当前分区后释放原座位，下一帧按所在分区重新执行步骤一。
    private void ResetPrepareSeatForRegionChange(int seatIndex)
    {
        personPlayerIds[seatIndex] = PersonIdNull;
        personPlayerLockIng[seatIndex] = PersonIdNull;
        personHandsUpFrameCount[seatIndex] = 0;
        ResetPrepareRoleSelectState(seatIndex);
        ResetPrepareSeatState(seatIndex);
        onPlayerIsReady?.Invoke(seatIndex, 0, readyNeedFrame);
        _isReadyShow[seatIndex] = 0;
        personWasInReadyArea[seatIndex] = false;

        if (_detectionStates[seatIndex] != DetectionState.OutOfArea)
        {
            onPlayerNotInReadyArea?.Invoke(seatIndex);
            _detectionStates[seatIndex] = DetectionState.OutOfArea;
        }
    }

    /// <summary>
    /// 检查是否所有玩家都已确认，如果是则结束准备阶段，触发游戏开始事件
    /// </summary>
    protected override void CheckAllPlayersConfirmed()
    {
        int readyCount = 0;
        int enabledCount = 0;
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (!IsSeatTrackedForCurrentPhase(i))
            {
                continue;
            }
            enabledCount++;
            if (personPlayerIds[i] != PersonIdNull)
            {
                readyCount++;
            }
        }
        if (enabledCount > 0 && readyCount >= enabledCount)
        {
            // 这里只通知业务尝试开战，不能提前关闭准备识别。
            // 只有业务请求校验和场景切换均成功后，窗口才会调用 EnterBattleWithConfirmedPlayers。
            if (!_canStartGame)
            {
                onCanGameStart?.Invoke();
            }
            return;
        }

        // 玩家离位后允许下一次全部准备重新触发开战请求。
        _canStartGame = false;
    }

    public int[] GetCurrentlyLostPlayers()
    {
        if (personPlayerIds == null || personPlayerIds.Length == 0)
        {
            return Array.Empty<int>();
        }

        List<int> lost = new List<int>(personPlayerIds.Length);
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (IsSeatTrackedForCurrentPhase(i) && personPlayerIds[i] == PersonIdNull)
            {
                lost.Add(i);
            }
        }
        return lost.Count == 0 ? Array.Empty<int>() : lost.ToArray();
    }

    /// <summary>
    /// 判断指定槽位是否参与本轮准备。
    /// 旧的 Disable 子节点限制已经废弃，当前只看准备界面传入了几个有效槽位。
    /// </summary>
    public bool IsSlotEnabled(int seatIndex)
    {
        if (seatIndex < 0)
        {
            return false;
        }

        if (_PlayerList == null || _PlayerList.Count <= 0)
        {
            return seatIndex < _curPlayerCount;
        }

        return seatIndex < _PlayerList.Count && _PlayerList[seatIndex] != null;
    }

    /// <summary>
    /// 获取玩家当前帧的数据
    /// </summary>
    public float[,] GetPlayerKeyPointList(int index)
    {
        if (index < 0 || index >= personPlayerIds.Length) return null;
        int playerId = personPlayerIds[index];
        if (!playerKeyPoints.TryGetValue(playerId, out PlayerFullData playerFullData)) return null;
        return playerFullData.keyPointListPose;
    }

    /// <summary>
    /// 获取指定座位玩家当前帧的完整运行时状态（动作、手部、旋转、关键点等）
    /// </summary>
    /// <param name="seatIndex">座位索引 0 ~ CurPlayerCount-1</param>
    /// <returns>PlayerRuntimeState 结构体；座位为空或越界时 isValid=false</returns>
    public PlayerRuntimeState GetPlayerState(int seatIndex)
    {
        PlayerRuntimeState state = default;
        if (seatIndex < 0 || seatIndex >= personPlayerIds.Length)
        {
            return state;
        }

        int playerId = personPlayerIds[seatIndex];
        int lockId = personPlayerLockIng[seatIndex];
        int targetId = (playerId != PersonIdNull) ? playerId : lockId;

        if (targetId == PersonIdNull)
        {
            state.isValid = false;
            state.rotationOffset = 0f;
            state.normalizedX = -1f;
            return state;
        }

        if (playerKeyPoints == null || !playerKeyPoints.TryGetValue(targetId, out PlayerFullData data))
        {
            state.isValid = false;
            state.playerId = targetId;
            state.rotationOffset = (seatIndex < _cachedPlayerRotationOffset.Length) ? _cachedPlayerRotationOffset[seatIndex] : 0f;
            state.normalizedX = -1f;
            return state;
        }

        state.isValid = true;
        state.playerId = targetId;
        state.poseType = data.poseType;
        state.leftHandType = data.leftHandType;
        state.rightHandType = data.rightHandType;
        state.score = data.score;
        state.leftHandScore = data.leftHandScore;
        state.rightHandScore = data.rightHandScore;
        state.left = data.left;
        state.top = data.top;
        state.right = data.right;
        state.bottom = data.bottom;
        state.keyPointListPose = data.keyPointListPose;
        state.rotationOffset = (seatIndex < _cachedPlayerRotationOffset.Length) ? _cachedPlayerRotationOffset[seatIndex] : 0f;
        state.normalizedX = (seatIndex < _cachedOffsetPlayerTransformX.Length) ? _cachedOffsetPlayerTransformX[seatIndex] : -1f;
        return state;
    }

    /// <summary>
    /// 根据 personId 获取玩家所在的索引位置 (0 ~ CurPlayerCount-1)
    /// </summary>
    public int GetPlayerIndexByPersonId(int personId)
    {
        if (personPlayerIds == null || personPlayerLockIng == null) return -1;
        for (int i = 0; i < _curPlayerCount; i++)
        {
            if (personPlayerIds[i] == personId || personPlayerLockIng[i] == personId)
            {
                return i;
            }
        }
        return -1;
    }

    // 读取 SDK 当前配置的准备人数。
    public int ReadConfiguredPlayerCount()
    {
        return _curPlayerCount;
    }

    // 读取 SDK 当前已经确认完成的玩家数量。
    public int ReadConfirmedPlayerCount()
    {
        if (personPlayerIds == null || personPlayerIds.Length == 0)
        {
            return 0;
        }

        int confirmedCount = 0;
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (personPlayerIds[i] != PersonIdNull)
            {
                confirmedCount++;
            }
        }

        return confirmedCount;
    }

    // 判断 SDK 当前配置的位置是否都已经确认完成。
    public bool AreAllConfiguredPlayersConfirmed()
    {
        if (personPlayerIds == null || personPlayerIds.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (!IsSlotEnabled(i))
            {
                continue;
            }
            if (personPlayerIds[i] == PersonIdNull)
            {
                return false;
            }
        }

        return true;
    }

    // 读取 SDK 当前已经确认完成的座位编号列表。
    public int[] ReadConfirmedSeatIds()
    {
        if (personPlayerIds == null || personPlayerIds.Length == 0)
        {
            return Array.Empty<int>();
        }

        List<int> seatIds = new List<int>(personPlayerIds.Length);
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (personPlayerIds[i] != PersonIdNull)
            {
                seatIds.Add(i);
            }
        }

        return seatIds.ToArray();
    }
    public Dictionary<int, int> GetReadySeatIds()
    {
        if (personPlayerIds == null || personPlayerIds.Length == 0)
        {
            return null;
        }

        Dictionary<int, int> seatIds = new Dictionary<int, int>();
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (!IsSlotEnabled(i))
            {
                continue;
            }
            var id = personPlayerIds[i];
            if(id == PersonIdNull)
            {
                continue;
            }
            seatIds[i] = personPlayerIds[i];
        }

        return seatIds;
    }


    void OnDestroy()
    {
        Debug.Log("调用清除数据的方法开始");
        playerKeyPoints?.Clear();
        playerKeyPoints = null;
        playerTextuerShow = null;
        personPlayerIds = null;
        personPlayerLockIng = null;
        personPlayerReadyRectf = null;
        personHandsUpFrameCount = null;
        _cachedOffsetPlayerTransformX = null;
        _cachedPlayerRotationOffset = null;
        _maxShoulderWidth = null;
        _lastPoseType = null;
        _playerAttackStates = null;
        Debug.Log("调用清除数据的方法完成");
    }
}
