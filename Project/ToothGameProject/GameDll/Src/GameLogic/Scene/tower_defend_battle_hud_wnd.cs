using DG.Tweening;
using LCL;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
using GameDll;
using System.IO;

namespace GameHot
{
    public enum BattleHeadDisplayMode
    {
        FullBody,
        UpperBody,
        Head
    }

    public class tower_defend_battle_hud_model : WindowModel
    {
        public override void Clear()
        {
        }
    }

    public class tower_defend_battle_hud_wnd : WindowBase
    {
        private const string m_LanTdHudWave = "td_hud_wave";
        private const string m_LanTdHudBase = "td_hud_base";
        private const string m_LanTdHudBattleEnd = "td_hud_battle_end";
        private const string m_LanTdHudPrepare = "td_hud_prepare";
        private const string m_LanTdHudLevelUpTime = "td_hud_level_up_time";
        private const string m_LanTdHudBattleRun = "td_hud_battle_run";
        private const string m_LanTdHudLevelUpRun = "td_hud_level_up_run";
        private const string m_LanTdHudLevelUpReady = "td_hud_level_up_ready";
        private const string m_LanTdHudTeamExp = "td_hud_team_exp";
        private const int m_MaxHudPlayerCount = 4;
        private const float m_SkillEnergyFullFillValue = 10000f;
        private const float m_StarsDropDuration = 0.5f;
        private const float m_StarsFlyDuration = 2f;
        private const float m_StarsDropMinOffsetY = 55.0f;
        private const float m_StarsDropMaxOffsetY = 110.0f;
        private const float m_StarsDropMaxOffsetX = 45.0f;
        private const float m_StarsCurveMinArcHeight = 70.0f;
        private const float m_StarsCurveMaxArcHeight = 170.0f;
        private const float m_StarsCurveMaxSideOffset = 90.0f;
        private const float m_BossHealthOneBarValue = 500.0f;
        private const float m_BossHealthEpsilon = 0.0001f;
        private const float m_BossHealthSlowDelay = 0.5f;
        private const float m_BossHealthSlowDuration = 0.3f;
        private const float m_LollipopHealthSlowDelay = 0.5f;
        private const float m_LollipopHealthSlowDuration = 0.3f;
        private const float m_BaseBloodSlowDuration = 0.3f;
        private const float m_LollipopHealthOneBarValue = 50f;
        private const float m_BattleHeadReadyAreaLeftMargin = 0f;
        private const float m_BattleHeadReadyAreaRightMargin = 1f;
        private const float m_BattleHeadReadyAreaTopMargin = 0.05f;
        private const float m_BattleHeadReadyAreaBottomMargin = 0.95f;
        private const float m_BattleHeadMinShoulderWidth = 0.001f;
        private const float m_BattleHeadUpperBodyRectHeightRatio = 0.62f;
        private const float m_BattleHeadHeadRectWidthRatio = 0.5f;
        private const float m_BattleHeadHeadRectHeightRatio = 0.32f;
        private const float m_BattleHeadNoseHorizontalFollowWeight = 0.25f;
        private const float m_BattleHeadDefaultSmoothSpeed = 0.95f;
        private const float m_BattleHeadDefaultInputFilterSpeed = 0.90f;
        private const float m_BattleHeadDefaultInputDeadzone = 0.004f;
        private const float m_baseHitEffTime = 0.4f;
        private static readonly Vector3 m_BattleHeadCameraViewScale = new Vector3(-1f, -1f, 1f);
        private static readonly int m_FightInfoDpsParam = Animator.StringToHash("dps");


        private v_tower_defend_battle_hud_wnd m_View;
        private readonly v_tower_defend_battle_hud_wnd.v_PlayerSlot[] m_PlayerSlotViews =
            new v_tower_defend_battle_hud_wnd.v_PlayerSlot[m_MaxHudPlayerCount];
        private readonly v_tower_defend_battle_hud_wnd.v_dps_player[] m_dps_player =
            new v_tower_defend_battle_hud_wnd.v_dps_player[m_MaxHudPlayerCount];
        private Animator m_FightInfoAnimator;
        private ParticleSystem m_Lollipop_Particle;
        public List<RectTransform> fillDict = new();
        public List<RectTransform> LollipopFillDict = new();
        private Animator m_topAni;
        private long m_Lollipop_counter;
        private float teamExpValue;
        private Material m_TeamExpOriginMaterial;
        private Material m_TeamExpRuntimeMaterial;
        private List<RectTransform> m_StarsEffectList = new();
        private List<RectTransform> m_StarsEffectActiveList = new();
        private List<RectTransform> m_coinList = new();
        private Tween m_TeamExpFillTween;
        private Tween m_BaseBloodFillTween;
        private Slider m_Fill_Slowly;
        private CanvasGroup m_baseHitCanvasGroup;
        private const int m_BattleHeadCameraViewCount = m_MaxHudPlayerCount + 1;
        private AndroidTextureBridgeBase m_BattleHeadTextureBridge;
        private CameraTextureView[] m_BattleHeadCameraViews;
        private readonly Rect[] m_BattleHeadTextureRects = new Rect[m_MaxHudPlayerCount];
        private readonly BattleHeadCameraTracker[] m_BattleHeadCameraTrackers = new BattleHeadCameraTracker[m_MaxHudPlayerCount];
        private BattleBoneParseData m_BattleHeadBoneParseData;
        private bool m_BattleHeadTextureBridgeCreatedRuntime;
        private FrameUpdate m_BattleHeadFrameUpdate;
        private int m_BattleHeadLastBoneFrameSerial = -1;
        private BattleHeadDisplayMode m_BattleHeadDisplayMode = BattleHeadDisplayMode.Head;
        private BattleHeadFollowMode m_BattleHeadFollowMode = BattleHeadFollowMode.IndependentUICamera;
        private float m_BattleHeadSmoothSpeed = m_BattleHeadDefaultSmoothSpeed;
        private float m_BattleHeadInputFilterSpeed = m_BattleHeadDefaultInputFilterSpeed;
        private float m_BattleHeadInputDeadzone = m_BattleHeadDefaultInputDeadzone;

        private long m_DpsRefreshTimerId = 0;


        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Hold;
            __CustomUIPrefabDir = UIPrefabDirs.battle;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);
            __CreateModel(new tower_defend_battle_hud_model());
        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_battle_hud_wnd();
            m_View.InitComponent(__GetWindowObj());
            InitPlayerSlotViews();
            InitDpsPlayerViews();
            InitPlayersUI();
            fillDict.Add(m_View.m_Fill_0.GetComponent<RectTransform>());
            fillDict.Add(m_View.m_Fill_1.GetComponent<RectTransform>());
            fillDict.Add(m_View.m_Fill_2.GetComponent<RectTransform>());
            fillDict.Add(m_View.m_Fill_3.GetComponent<RectTransform>());
            fillDict.Add(m_View.m_Fill_4.GetComponent<RectTransform>());
            LollipopFillDict.Add(m_View.m_lollipopFill_0.GetComponent<RectTransform>());
            LollipopFillDict.Add(m_View.m_lollipopFill_1.GetComponent<RectTransform>());
            LollipopFillDict.Add(m_View.m_lollipopFill_2.GetComponent<RectTransform>());
            LollipopFillDict.Add(m_View.m_lollipopFill_3.GetComponent<RectTransform>());
            LollipopFillDict.Add(m_View.m_lollipopFill_4.GetComponent<RectTransform>());
            m_topAni = m_View.m_top.GetComponent<Animator>();
            m_baseHitCanvasGroup = m_View.m_baseHitEff.GetComponent<CanvasGroup>();
            m_Fill_Slowly = m_View.m_Fill_Slowly.GetComponent<Slider>();
            m_Lollipop_Particle = m_View.m_eff_bangbang_star.GetComponent<ParticleSystem>();
            m_FightInfoAnimator = m_View.m_fightInfo != null ? m_View.m_fightInfo.GetComponent<Animator>() : null;
            m_BattleHeadFrameUpdate = __GetWindowObj().GetComponent<FrameUpdate>();
            if (m_BattleHeadFrameUpdate == null)
            {
                m_BattleHeadFrameUpdate = __GetWindowObj().AddComponent<FrameUpdate>();
            }
            RenderAPI.AddButtonClick(m_View.m_btn_pause, () => RenderEvent.Event.OnTowerDefendPauseRequest());
        }

        protected override void OnOpen()
        {
            RenderEvent.Event.OnBattleResult += OnBattleResult;
            RenderEvent.Event.OnTowerDefendBaseHealthChanged += OnTowerDefendBaseHealthChanged;
            RenderEvent.Event.OnTowerDefendBaseHealthChanged(0,0);
            RenderEvent.Event.OnTowerDefendBattleStateChanged += OnTowerDefendBattleStateChanged;
            RenderEvent.Event.OnTowerDefendPlayerSkillEnergyFullStateChanged += OnTowerDefendPlayerSkillEnergyFullStateChanged;
            RenderEvent.Event.OnStartUpgradeChallenge += OnStartUpgradeChallenge;
            RenderEvent.Event.OnFinishUpgradeChallenge += OnFinishUpgradeChallenge;
            RenderEvent.Event.OnAddTeamExp += OnAddTeamExp;
            RenderEvent.Event.OnBossHealthChanged += OnBossHealthChanged;
            RenderEvent.Event.OnLollipopHealthChanged += OnLollipopHealthChanged;
            RenderEvent.Event.OnTowerDefendMonsterDeathStarsEffect += SetStarsEffect;
            RenderEvent.Event.ShowRewardCoin += ShowRewardCoin;
            InitTeamExpMaterial();
            SetTeamExpFillAmount(0f);
            RefreshFromCurrentBattle();
            EnsureBattleHeadTextureView();
            PlayMusic();
        }

        private void PlayMusic()
        {
            var battle = ReadCurrentBattle();
            int stage = battle.GetStage();
            if (stage > 5)
            {
                stage = 5;
            }
            AudioManager.GetInstance().Play2D(
                300 + stage,
                AudioTransitionMode.CrossFade,
                -1f,
                AudioReplayMode.KeepCurrent,
                AudioLifetime.Persistent);
        }

        private void EnsureBattleHeadTextureView()
        {
            if (!TryEnsureBattleHeadTextureView())
            {
                StopBattleHeadTextureRefresh();
                return;
            }

            StartBattleHeadTextureRefresh();
            TryRefreshBattleHeadCameraRegions();
        }

        public void SetBattleHeadDisplayMode(BattleHeadDisplayMode displayMode)
        {
            if (displayMode != BattleHeadDisplayMode.FullBody &&
                displayMode != BattleHeadDisplayMode.UpperBody &&
                displayMode != BattleHeadDisplayMode.Head)
            {
                displayMode = BattleHeadDisplayMode.UpperBody;
            }

            if (m_BattleHeadDisplayMode == displayMode)
            {
                return;
            }

            m_BattleHeadDisplayMode = displayMode;
            ResetBattleHeadSmoothState();
            TryRefreshBattleHeadCameraRegions();
        }

        public void SetBattleHeadFollowSmoothConfig(float smoothSpeed, float filterSpeed, float deadzone)
        {
            m_BattleHeadSmoothSpeed = Mathf.Clamp01(smoothSpeed);
            m_BattleHeadInputFilterSpeed = Mathf.Clamp01(filterSpeed);
            m_BattleHeadInputDeadzone = Mathf.Clamp01(deadzone);
            ApplyBattleHeadSmoothConfig(false);
        }

        public void SetBattleHeadFollowMode(BattleHeadFollowMode followMode)
        {
            if (followMode != BattleHeadFollowMode.ContinuousSmooth &&
                followMode != BattleHeadFollowMode.IndependentUICamera)
            {
                followMode = BattleHeadFollowMode.IndependentUICamera;
            }

            if (m_BattleHeadFollowMode == followMode)
            {
                return;
            }

            m_BattleHeadFollowMode = followMode;
            ResetBattleHeadTrackingState();
            ApplyBattleHeadSmoothConfig(true);
            TryRefreshBattleHeadCameraRegions();
        }

        private void ApplyBattleHeadSmoothConfig(bool resetSmoothState)
        {
            EnsureBattleHeadCameraTrackers();
            for (int i = 0; i < m_BattleHeadCameraTrackers.Length; i++)
            {
                m_BattleHeadCameraTrackers[i].SetFollowMode(m_BattleHeadFollowMode);
                m_BattleHeadCameraTrackers[i].SetConfig(m_BattleHeadInputFilterSpeed, m_BattleHeadInputDeadzone);
            }

            if (m_BattleHeadCameraViews == null)
            {
                return;
            }

            for (int i = 0; i < m_BattleHeadCameraViews.Length; i++)
            {
                ConfigureBattleHeadCameraView(m_BattleHeadCameraViews[i], resetSmoothState);
            }
        }

        private void ResetBattleHeadSmoothState()
        {
            ResetBattleHeadTrackingState();
            if (m_BattleHeadCameraViews == null)
            {
                return;
            }

            for (int i = 0; i < m_BattleHeadCameraViews.Length; i++)
            {
                m_BattleHeadCameraViews[i]?.ResetSmoothState();
            }
        }

        private void EnsureBattleHeadCameraTrackers()
        {
            for (int i = 0; i < m_BattleHeadCameraTrackers.Length; i++)
            {
                if (m_BattleHeadCameraTrackers[i] == null)
                {
                    m_BattleHeadCameraTrackers[i] = new BattleHeadCameraTracker();
                    m_BattleHeadCameraTrackers[i].SetFollowMode(m_BattleHeadFollowMode);
                    m_BattleHeadCameraTrackers[i].SetConfig(m_BattleHeadInputFilterSpeed, m_BattleHeadInputDeadzone);
                }
            }
        }

        private void ResetBattleHeadTrackingState()
        {
            m_BattleHeadLastBoneFrameSerial = -1;
            for (int i = 0; i < m_BattleHeadCameraTrackers.Length; i++)
            {
                m_BattleHeadCameraTrackers[i]?.Reset();
            }
        }

        private void ConfigureBattleHeadCameraView(CameraTextureView cameraView, bool resetSmoothState)
        {
            if (cameraView == null)
            {
                return;
            }

            var smoothCalculation = new BattleCameraTextureViewSmoothCalculation();
            smoothCalculation.SetPositionSmoothingEnabled(m_BattleHeadFollowMode == BattleHeadFollowMode.ContinuousSmooth);
            cameraView.SetSmoothCalculator(smoothCalculation);
            cameraView.SetSmoothConfig(m_BattleHeadSmoothSpeed, m_BattleHeadInputFilterSpeed, m_BattleHeadInputDeadzone);
            if (resetSmoothState)
            {
                cameraView.ResetSmoothState();
            }
        }

        private bool TryEnsureBattleHeadTextureView()
        {
            if (!ShouldUseBattleHeadTextureView())
            {
                ClearBattleHeadCameraViews();
                return false;
            }

            if (m_View == null || m_View.m_PlayerSlots == null)
            {
                return false;
            }

            m_BattleHeadTextureBridge = EnsureBattleHeadTextureBridge();
            if (m_BattleHeadTextureBridge == null)
            {
                return false;
            }

            if (m_BattleHeadCameraViews == null || m_BattleHeadCameraViews.Length != m_BattleHeadCameraViewCount)
            {
                DestroyBattleHeadCameraViews();
                m_BattleHeadCameraViews = CreateBattleHeadCameraViews();
                m_BattleHeadTextureBridge.SetCameraViews(m_BattleHeadCameraViews);
            }

            RefreshBattleHeadTextureViewBinding();
            return true;
        }

        private void StartBattleHeadTextureRefresh()
        {
            if (m_BattleHeadFrameUpdate == null)
            {
                return;
            }

            // 头像跟随依赖战斗实时骨骼帧，不能在首次裁切成功后停止刷新。
            m_BattleHeadFrameUpdate.SetFrameCall(OnBattleHeadTextureFrameUpdate);
        }

        private void StopBattleHeadTextureRefresh()
        {
            if (m_BattleHeadFrameUpdate != null)
            {
                m_BattleHeadFrameUpdate.SetFrameCall(null);
            }
        }

        private void OnBattleHeadTextureFrameUpdate()
        {
            if (!IsInitializedView() || !ShouldUseBattleHeadTextureView())
            {
                StopBattleHeadTextureRefresh();
                return;
            }

            TryRefreshBattleHeadCameraRegions();
        }

        private AndroidTextureBridgeBase EnsureBattleHeadTextureBridge()
        {
            if (m_View == null || m_View.m_PlayerSlots == null)
            {
                return null;
            }

            var playerSlotsObject = m_View.m_PlayerSlots.gameObject;
            var bridges = playerSlotsObject.GetComponents<AndroidTextureBridgeBase>();
            for (int i = 0; i < bridges.Length; i++)
            {
                var bridge = bridges[i];
                if (bridge != null && bridge.enabled)
                {
                    m_BattleHeadTextureBridgeCreatedRuntime = false;
                    return bridge;
                }
            }

            // 桥接挂在 PlayerSlots 上，显示节点再移动到各自 headView，避免依赖独立头像显示节点。
            switch (SystemInfo.graphicsDeviceType)
            {
                case UnityEngine.Rendering.GraphicsDeviceType.Vulkan:
                    m_BattleHeadTextureBridgeCreatedRuntime = true;
                    return playerSlotsObject.AddComponent<AndroidTextureBridgeVulkan>();
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2:
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
                    m_BattleHeadTextureBridgeCreatedRuntime = true;
                    return playerSlotsObject.AddComponent<AndroidTextureBridgeOpenglES>();
                default:
                    Debug.LogWarning("tower_defend_battle_hud_wnd: 当前图形后端不支持战斗头像显示 " + SystemInfo.graphicsDeviceType);
                    return null;
            }
        }

        private CameraTextureView[] CreateBattleHeadCameraViews()
        {
            var views = new CameraTextureView[m_BattleHeadCameraViewCount];
            if (m_View == null || m_View.m_PlayerSlots == null)
            {
                return views;
            }

            var parent = m_View.m_PlayerSlots.transform;
            int layer = m_View.m_PlayerSlots.gameObject.layer;
            for (int i = 0; i < views.Length; i++)
            {
                var go = new GameObject("battle_head_camera_view_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage),
                typeof(CameraTextureView));
                go.layer = layer;
                go.transform.SetParent(parent, false);
                ResetBattleHeadCameraViewTransform(go.transform);

                var rawImage = go.GetComponent<RawImage>();
                if (rawImage != null)
                {
                    rawImage.raycastTarget = false;
                    rawImage.color = Color.white;
                }

                go.SetActive(false);
                var cameraView = go.GetComponent<CameraTextureView>();
                ConfigureBattleHeadCameraView(cameraView, true);
                views[i] = cameraView;
            }

            return views;
        }

        private void ReleaseBattleHeadTextureView()
        {
            StopBattleHeadTextureRefresh();
            ResetBattleHeadTrackingState();
            DestroyBattleHeadCameraViews();
            if (m_BattleHeadTextureBridge != null)
            {
                m_BattleHeadTextureBridge.SetCameraViews(null);
            }

            if (m_BattleHeadTextureBridgeCreatedRuntime && m_BattleHeadTextureBridge != null)
            {
                UnityEngine.Object.Destroy(m_BattleHeadTextureBridge);
            }

            m_BattleHeadTextureBridge = null;
            m_BattleHeadBoneParseData = null;
            m_BattleHeadTextureBridgeCreatedRuntime = false;
        }

        private void ClearBattleHeadCameraViews()
        {
            if (m_BattleHeadCameraViews == null)
            {
                return;
            }

            for (int i = 0; i < m_BattleHeadCameraViews.Length; i++)
            {
                var cameraView = m_BattleHeadCameraViews[i];
                if (cameraView == null)
                {
                    continue;
                }

                cameraView.Clear();
                cameraView.gameObject.SetActive(false);
            }
        }

        private void DestroyBattleHeadCameraViews()
        {
            if (m_BattleHeadCameraViews == null)
            {
                return;
            }

            for (int i = 0; i < m_BattleHeadCameraViews.Length; i++)
            {
                var cameraView = m_BattleHeadCameraViews[i];
                if (cameraView == null)
                {
                    continue;
                }

                cameraView.Clear();
                UnityEngine.Object.Destroy(cameraView.gameObject);
            }

            m_BattleHeadCameraViews = null;
        }

        private void RefreshBattleHeadTextureViewBinding()
        {
            if (!ShouldUseBattleHeadTextureView() || m_BattleHeadTextureBridge == null)
            {
                return;
            }

            int cameraViewCount = m_BattleHeadTextureBridge.GetCameraViewCount();
            if (cameraViewCount <= 0)
            {
                return;
            }

            bool isPartitionView = PlayerMatchView.Instance != null &&
                                   PlayerMatchView.Instance.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView;
            var battle = ReadCurrentBattle();
            var boundIndices = new HashSet<int>();
            for (int seatId = 0; seatId < m_MaxHudPlayerCount; seatId++)
            {
                if (battle == null || battle.ReadPlayerBySeat(seatId) == null)
                {
                    continue;
                }

                var slotView = GetPlayerSlotView(seatId);
                if (slotView == null || slotView.m_headView == null)
                {
                    continue;
                }

                if (!isPartitionView && boundIndices.Count > 0)
                {
                    continue;
                }

                int sdkSlotIndex;
                if (!TryResolveBattleHeadSdkSlotIndex(seatId, out sdkSlotIndex))
                {
                    continue;
                }

                int cameraViewIndex = ResolveBattleHeadCameraViewIndex(sdkSlotIndex, cameraViewCount, isPartitionView);
                var cameraView = m_BattleHeadTextureBridge.GetCameraView(cameraViewIndex);
                if (cameraView == null)
                {
                    continue;
                }

                AttachBattleHeadCameraView(cameraView, slotView.m_headView.transform);
                boundIndices.Add(cameraViewIndex);
            }

            for (int i = 0; i < cameraViewCount; i++)
            {
                if (boundIndices.Contains(i))
                {
                    continue;
                }

                var cameraView = m_BattleHeadTextureBridge.GetCameraView(i);
                if (cameraView == null)
                {
                    continue;
                }

                cameraView.Clear();
                cameraView.gameObject.SetActive(false);
            }
        }

        private static void AttachBattleHeadCameraView(CameraTextureView cameraView, Transform headView)
        {
            if (cameraView == null || headView == null)
            {
                return;
            }

            cameraView.transform.SetParent(headView, false);
            ResetBattleHeadCameraViewTransform(cameraView.transform);
        }

        private static void ResetBattleHeadCameraViewTransform(Transform target)
        {
            if (target == null)
            {
                return;
            }

            var rectTransform = target as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
            }
            else
            {
                target.localPosition = Vector3.zero;
            }

            target.localRotation = Quaternion.identity;
            // 准备界面的 CameraImagViewForPersonX 使用这个缩放修正相机纹理方向，战斗头像保持同一约定。
            target.localScale = m_BattleHeadCameraViewScale;
        }

        private int ResolveBattleHeadCameraViewIndex(int sdkSlotIndex, int cameraViewCount, bool isPartitionView)
        {
            if (!isPartitionView)
            {
                return 0;
            }

            // AndroidTextureBridgeBase 在分屏模式下约定 cameraViews[0] 是主图，
            // cameraViews[1] 才对应 SDK 槽位0，因此这里必须用 SDK 槽位而不是战斗座位。
            return Mathf.Clamp(sdkSlotIndex + 1, 0, cameraViewCount - 1);
        }

        private bool TryResolveBattleHeadSdkSlotIndex(int seatId, out int sdkSlotIndex)
        {
            sdkSlotIndex = -1;
            if (seatId < 0 || seatId >= m_MaxHudPlayerCount)
            {
                return false;
            }

            var battleScene = ReadCurrentBattleScene();
            if (battleScene != null && battleScene.TryGetSdkSlotIndexBySeat(seatId, out sdkSlotIndex))
            {
                return true;
            }

            sdkSlotIndex = seatId;
            return true;
        }

        private bool TryRefreshBattleHeadCameraRegions()
        {
            if (!ShouldUseBattleHeadTextureView() || m_BattleHeadTextureBridge == null || !m_BattleHeadTextureBridge.IsTextureReady())
            {
                return false;
            }

            if (!BuildBattleHeadCameraRects(m_BattleHeadTextureRects))
            {
                return false;
            }

            m_BattleHeadTextureBridge.SetCameraViewRects(m_BattleHeadTextureRects);
            return true;
        }

        private bool BuildBattleHeadCameraRects(Rect[] rects)
        {
            if (rects == null || rects.Length <= 0)
            {
                return false;
            }

            for (int i = 0; i < rects.Length; i++)
            {
                rects[i] = new Rect(0f, 0f, 0f, 0f);
            }

            if (TryBuildBattleHeadCameraRectsFromBoneFrame(rects))
            {
                return true;
            }

            return TryBuildBattleHeadCameraRectsFromMatchView(rects);
        }

        private bool TryBuildBattleHeadCameraRectsFromBoneFrame(Rect[] rects)
        {
            var battle = ReadCurrentBattle();
            if (battle == null)
            {
                return false;
            }

            EnsureBattleHeadCameraTrackers();
            if (TryReadLatestBattleHeadBoneFrameData(out var frameData) &&
                frameData.m_FrameSerial != m_BattleHeadLastBoneFrameSerial)
            {
                m_BattleHeadLastBoneFrameSerial = frameData.m_FrameSerial;
                UpdateBattleHeadCameraTrackerMeasurements(frameData, battle, rects.Length);
            }

            int maxSlots = Mathf.Min(m_MaxHudPlayerCount, rects.Length);
            bool hasValidRect = false;
            for (int seatIndex = 0; seatIndex < maxSlots; seatIndex++)
            {
                if (battle.ReadPlayerBySeat(seatIndex) == null)
                {
                    continue;
                }

                int sdkSlotIndex;
                if (!TryResolveBattleHeadSdkSlotIndex(seatIndex, out sdkSlotIndex) ||
                    sdkSlotIndex < 0 ||
                    sdkSlotIndex >= rects.Length ||
                    sdkSlotIndex >= m_BattleHeadCameraTrackers.Length)
                {
                    continue;
                }

                if (!m_BattleHeadCameraTrackers[sdkSlotIndex].TryGetDisplayRect(
                        Time.unscaledDeltaTime,
                        Time.frameCount,
                        out var rect))
                {
                    continue;
                }

                rects[sdkSlotIndex] = rect;
                hasValidRect = true;
            }

            return hasValidRect;
        }

        private void UpdateBattleHeadCameraTrackerMeasurements(BoneFrameData frameData, TowerDefendBattle battle, int rectCount)
        {
            int maxSlots = Mathf.Min(m_MaxHudPlayerCount, rectCount);
            for (int seatIndex = 0; seatIndex < maxSlots; seatIndex++)
            {
                if (battle.ReadPlayerBySeat(seatIndex) == null ||
                    !TryResolveBattleHeadSdkSlotIndex(seatIndex, out var sdkSlotIndex) ||
                    sdkSlotIndex < 0 ||
                    sdkSlotIndex >= m_BattleHeadCameraTrackers.Length)
                {
                    continue;
                }

                var tracker = m_BattleHeadCameraTrackers[sdkSlotIndex];
                if (frameData.m_Persons == null || sdkSlotIndex >= frameData.m_Persons.Count)
                {
                    tracker.MarkMeasurementMissing(frameData.m_FrameSerial, frameData.m_FrameTimeMs);
                    continue;
                }

                var person = frameData.m_Persons[sdkSlotIndex];
                if (!TryBuildBattleHeadPersonRect(person, out var measuredRect))
                {
                    tracker.MarkMeasurementMissing(frameData.m_FrameSerial, frameData.m_FrameTimeMs);
                    continue;
                }

                tracker.PushMeasurement(
                    person.m_PersonId,
                    frameData.m_FrameSerial,
                    frameData.m_FrameTimeMs,
                    measuredRect);
            }
        }

        private bool TryReadLatestBattleHeadBoneFrameData(out BoneFrameData frameData)
        {
            frameData = null;
            if (m_BattleHeadBoneParseData == null)
            {
                var battleScene = ReadCurrentBattleScene();
                var sceneRoot = battleScene != null ? battleScene.GetSceneRoot() : null;
                if (sceneRoot != null)
                {
                    m_BattleHeadBoneParseData = sceneRoot.GetComponentInChildren<BattleBoneParseData>(true);
                }
            }

            if (m_BattleHeadBoneParseData == null)
            {
                return false;
            }

            frameData = m_BattleHeadBoneParseData.ReadLatestFrameData();
            return frameData != null && frameData.m_HasFrameData;
        }

        private bool TryBuildBattleHeadCameraRectsFromMatchView(Rect[] rects)
        {
            var matchView = PlayerMatchView.Instance;
            if (matchView == null)
            {
                rects[0] = new Rect(0f, 0f, 1f, 1f);
                return true;
            }

            if (matchView.PlayerMatchViewMode == PlayerMatchViewMode.PartitionView)
            {
                float[,] personPlayerRectf = matchView.PersonPlayerDrawRectf;
                int rowCount = personPlayerRectf != null ? personPlayerRectf.GetLength(0) : 0;
                int maxSlots = Mathf.Min(m_MaxHudPlayerCount, rects.Length);
                var battle = ReadCurrentBattle();
                bool hasValidRect = false;
                for (int seatIndex = 0; seatIndex < maxSlots; seatIndex++)
                {
                    if (battle == null || battle.ReadPlayerBySeat(seatIndex) == null)
                    {
                        continue;
                    }

                    int sdkSlotIndex;
                    if (!TryResolveBattleHeadSdkSlotIndex(seatIndex, out sdkSlotIndex) ||
                        sdkSlotIndex < 0 ||
                        sdkSlotIndex >= maxSlots ||
                        sdkSlotIndex >= rowCount)
                    {
                        continue;
                    }

                    float left = personPlayerRectf[sdkSlotIndex, 0];
                    float top = personPlayerRectf[sdkSlotIndex, 1];
                    float right = personPlayerRectf[sdkSlotIndex, 2];
                    float bottom = personPlayerRectf[sdkSlotIndex, 3];
                    if (!IsValidBattleHeadPersonRect(left, top, right, bottom))
                    {
                        continue;
                    }

                    rects[sdkSlotIndex] = new Rect(left, top, right - left, bottom - top);
                    hasValidRect = true;
                }

                return hasValidRect;
            }

            Rect fullRect = matchView.CheckCameraTextureViewManager()
                ? matchView.CalculationResult()
                : new Rect(0f, 0f, 1f, 1f);
            rects[0] = IsValidBattleHeadRect(fullRect) ? fullRect : new Rect(0f, 0f, 1f, 1f);
            return true;
        }

        private bool TryBuildBattleHeadPersonRect(BonePersonData person, out Rect rect)
        {
            rect = default;
            if (person == null || person.m_PersonId == YouDooSDKConstants.PersonIdNull || person.m_Body == null)
            {
                return false;
            }

            // 三种显示模式都使用同一份骨骼数据，只改变显示区域，避免创建额外纹理。
            var joints = person.m_Body.m_Joints;
            if (TryReadBattleHeadJoint(joints, YouDooSDKConstants.KeyPointIndex.Nose, out var nose) &&
                TryReadBattleHeadJoint(joints, YouDooSDKConstants.KeyPointIndex.Leftshoulder, out var leftShoulder) &&
                TryReadBattleHeadJoint(joints, YouDooSDKConstants.KeyPointIndex.Rightshoulder, out var rightShoulder))
            {
                float shoulderWidth = Mathf.Abs(leftShoulder.m_X - rightShoulder.m_X);
                if (shoulderWidth > m_BattleHeadMinShoulderWidth &&
                    TryBuildBattleHeadPersonRectByMode(joints, nose, leftShoulder, rightShoulder, shoulderWidth, out rect))
                {
                    return true;
                }
            }

            var bodyRect = person.m_Body.m_Rect;
            if (bodyRect == null || !bodyRect.m_IsValid)
            {
                return false;
            }

            return TryBuildBattleHeadPersonRectFromBodyRect(bodyRect, out rect);
        }

        private bool TryBuildBattleHeadPersonRectByMode(
            BoneJointData[] joints,
            BoneJointData nose,
            BoneJointData leftShoulder,
            BoneJointData rightShoulder,
            float shoulderWidth,
            out Rect rect)
        {
            float shoulderCenterX = (leftShoulder.m_X + rightShoulder.m_X) * 0.5f;
            float shoulderCenterY = (leftShoulder.m_Y + rightShoulder.m_Y) * 0.5f;
            float left = Mathf.Min(leftShoulder.m_X, rightShoulder.m_X);
            float right = Mathf.Max(leftShoulder.m_X, rightShoulder.m_X);
            float top = nose.m_Y - 2f * shoulderWidth;
            float bottom;

            switch (m_BattleHeadDisplayMode)
            {
                case BattleHeadDisplayMode.FullBody:
                    if (!TryReadBattleHeadJoint(joints, YouDooSDKConstants.KeyPointIndex.Leftankle, out var leftAnkle) ||
                        !TryReadBattleHeadJoint(joints, YouDooSDKConstants.KeyPointIndex.Rightankle, out var rightAnkle))
                    {
                        rect = default;
                        return false;
                    }

                    bottom = (leftAnkle.m_Y + rightAnkle.m_Y) * 0.5f - 0.5f * shoulderWidth;
                    break;
                case BattleHeadDisplayMode.Head:
                    float headHalfWidth = shoulderWidth * 0.55f;
                    // 水平方向以双肩中心为主、鼻子为辅，既能跟随侧身移动，也避免鼻子识别噪声直接带动画面。
                    float headCenterX = Mathf.Lerp(shoulderCenterX, nose.m_X, m_BattleHeadNoseHorizontalFollowWeight);
                    left = headCenterX - headHalfWidth;
                    right = headCenterX + headHalfWidth;
                    top = nose.m_Y - 0.65f * shoulderWidth;
                    bottom = shoulderCenterY + 0.15f * shoulderWidth;
                    break;
                case BattleHeadDisplayMode.UpperBody:
                default:
                    if (TryReadBattleHeadJoint(joints, YouDooSDKConstants.KeyPointIndex.Lefthip, out var leftHip) &&
                        TryReadBattleHeadJoint(joints, YouDooSDKConstants.KeyPointIndex.Righthip, out var rightHip))
                    {
                        bottom = (leftHip.m_Y + rightHip.m_Y) * 0.5f + 0.2f * shoulderWidth;
                    }
                    else
                    {
                        bottom = shoulderCenterY + 2f * shoulderWidth;
                    }
                    break;
            }

            return TryMakeBattleHeadPersonRect(
                left,
                top,
                right,
                bottom,
                out rect);
        }

        private bool TryBuildBattleHeadPersonRectFromBodyRect(BoneRectData bodyRect, out Rect rect)
        {
            float left = Mathf.Clamp01(bodyRect.m_Left);
            float top = Mathf.Clamp01(bodyRect.m_Top);
            float right = Mathf.Clamp01(bodyRect.m_Right);
            float bottom = Mathf.Clamp01(bodyRect.m_Bottom);
            float width = right - left;
            float height = bottom - top;

            switch (m_BattleHeadDisplayMode)
            {
                case BattleHeadDisplayMode.Head:
                    left += width * (1f - m_BattleHeadHeadRectWidthRatio) * 0.5f;
                    right -= width * (1f - m_BattleHeadHeadRectWidthRatio) * 0.5f;
                    bottom = top + height * m_BattleHeadHeadRectHeightRatio;
                    break;
                case BattleHeadDisplayMode.UpperBody:
                    bottom = top + height * m_BattleHeadUpperBodyRectHeightRatio;
                    break;
            }

            return TryMakeBattleHeadPersonRect(left, top, right, bottom, out rect);
        }

        private static bool TryReadBattleHeadJoint(BoneJointData[] joints, YouDooSDKConstants.KeyPointIndex keyPointIndex, out BoneJointData joint)
        {
            int index = (int)keyPointIndex;
            joint = null;
            if (joints == null || index < 0 || index >= joints.Length)
            {
                return false;
            }

            joint = joints[index];
            return joint != null && joint.m_IsTracked;
        }

        private static bool TryMakeBattleHeadPersonRect(float left, float top, float right, float bottom, out Rect rect)
        {
            rect = default;
            if (float.IsNaN(left) || float.IsInfinity(left) ||
                float.IsNaN(top) || float.IsInfinity(top) ||
                float.IsNaN(right) || float.IsInfinity(right) ||
                float.IsNaN(bottom) || float.IsInfinity(bottom))
            {
                return false;
            }

            float width = right - left;
            float height = bottom - top;
            if (width < 0.01f || height < 0.01f)
            {
                return false;
            }

            // 人物靠近画面边缘时只平移裁切框，不压缩裁切框，避免边缘裁切被误判成人物远近变化。
            width = Mathf.Min(width, m_BattleHeadReadyAreaRightMargin - m_BattleHeadReadyAreaLeftMargin);
            height = Mathf.Min(height, m_BattleHeadReadyAreaBottomMargin - m_BattleHeadReadyAreaTopMargin);
            float centerX = (left + right) * 0.5f;
            float centerY = (top + bottom) * 0.5f;
            float x = Mathf.Clamp(
                centerX - width * 0.5f,
                m_BattleHeadReadyAreaLeftMargin,
                m_BattleHeadReadyAreaRightMargin - width);
            float y = Mathf.Clamp(
                centerY - height * 0.5f,
                m_BattleHeadReadyAreaTopMargin,
                m_BattleHeadReadyAreaBottomMargin - height);
            rect = new Rect(x, y, width, height);
            return true;
        }

        private static bool IsValidBattleHeadRect(Rect rect)
        {
            return rect.width > 0.001f && rect.height > 0.001f;
        }

        private static bool IsValidBattleHeadPersonRect(float left, float top, float right, float bottom)
        {
            if (left == 0f && top == 0f && right == 0f && bottom == 0f)
            {
                return false;
            }

            if (left < 0f || left > 1f || top < 0f || top > 1f ||
                right < 0f || right > 1f || bottom < 0f || bottom > 1f)
            {
                return false;
            }

            if (right <= left || bottom <= top)
            {
                return false;
            }

            return right - left >= 0.01f && bottom - top >= 0.01f;
        }

        private static bool ShouldUseBattleHeadTextureView()
        {
            return Application.platform == RuntimePlatform.Android;
        }

        protected override void OnClose()
        {
            if (m_DpsRefreshTimerId != 0)
            {
                CounterManager.GetInstance().RemoveCounter(m_DpsRefreshTimerId);
                m_DpsRefreshTimerId = 0;
            }
            RenderEvent.Event.OnTowerDefendBaseHealthChanged -= OnTowerDefendBaseHealthChanged;
            RenderEvent.Event.OnTowerDefendBattleStateChanged -= OnTowerDefendBattleStateChanged;
            RenderEvent.Event.OnTowerDefendPlayerSkillEnergyFullStateChanged -= OnTowerDefendPlayerSkillEnergyFullStateChanged;
            RenderEvent.Event.OnStartUpgradeChallenge -= OnStartUpgradeChallenge;
            RenderEvent.Event.OnFinishUpgradeChallenge -= OnFinishUpgradeChallenge;
            RenderEvent.Event.OnAddTeamExp -= OnAddTeamExp;
            RenderEvent.Event.OnBossHealthChanged -= OnBossHealthChanged;
            RenderEvent.Event.OnLollipopHealthChanged -= OnLollipopHealthChanged;
            RenderEvent.Event.OnTowerDefendMonsterDeathStarsEffect -= SetStarsEffect;
            RenderEvent.Event.OnBattleResult -= OnBattleResult;
            RenderEvent.Event.ShowRewardCoin -= ShowRewardCoin;
            CleanupStarsEffectRuntime();
            ReleaseBattleHeadTextureView();
            ReleaseTeamExpMaterial();
            ClearBossHealthRuntime();
            KillBaseBloodFillTween();
        }
        private void ShowRewardCoin(PropertyEntity propertyEntity, int rewardCoin)
        {
            if (propertyEntity == null || m_View == null)
            {
                return;
            }
            var rootRect = m_View.m_RewardCoinParent.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                return;
            }
            var screenPos = Tool.WorldToScreenPoint(propertyEntity.GetPosition(), Camera.main);
            var coinAnchoredPos = GameDll.Tool.ScreenPointToUGUI(rootRect, screenPos, null);
            RectTransform coinEff = null;
            if (m_coinList.Count > 0)
            {
                coinEff = m_coinList[0];
                m_coinList.RemoveAt(0);
            }
            else
            {
                var go = UnityEngine.GameObject.Instantiate(m_View.m_showRewardCoin.gameObject, m_View.m_RewardCoinParent.transform);
                coinEff = go.GetComponent<RectTransform>();
            }
            coinEff.gameObject.SetActive(true);
            coinEff.SetParent(rootRect, false);
            coinEff.anchoredPosition = coinAnchoredPos;
            coinEff.Find("rewardCoin_txt").transform.GetComponent<LUITextMesh>().text = "+" + rewardCoin;
            coinEff.transform.DOMoveY(100, 1).SetRelative(true).OnComplete(() =>
            {
                coinEff.gameObject.SetActive(false);
                m_coinList.Add(coinEff);
            });
        }
        public void SetStarsEffect(PropertyEntity defender)
        {
            if (defender == null || m_View == null || m_View.m_slider_team_exp_root == null || m_View.m_eff_xingxing == null)
            {
                return;
            }

            var rootRect = m_View.m_slider_team_exp_root.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                return;
            }

            var screenPos = Tool.WorldToScreenPoint(defender.GetPosition(), Camera.main);
            var startAnchoredPos = GameDll.Tool.ScreenPointToUGUI(rootRect, screenPos, null);
            RectTransform starsEff = null;
            if (m_StarsEffectList.Count > 0)
            {
                starsEff = m_StarsEffectList[0];
                m_StarsEffectList.RemoveAt(0);
            }
            else
            {
                var go = UnityEngine.GameObject.Instantiate(m_View.m_eff_xingxing.gameObject, m_View.m_slider_team_exp_root.transform);
                starsEff = go.GetComponent<RectTransform>();
            }
            starsEff.GetComponent<ParticleSystem>().Play();
            m_StarsEffectActiveList.Remove(starsEff);
            starsEff.DOKill();
            starsEff.gameObject.SetActive(true);
            starsEff.SetParent(m_View.m_xingxingParent.transform, false);
            starsEff.localRotation = Quaternion.identity;
            starsEff.localScale = Vector3.one;
            starsEff.anchoredPosition = startAnchoredPos;
            m_StarsEffectActiveList.Add(starsEff);

            var distanceToTarget = startAnchoredPos.magnitude;
            var distanceLerp = Mathf.Clamp01(distanceToTarget / 900.0f);
            var dropOffsetY = Mathf.Lerp(m_StarsDropMinOffsetY, m_StarsDropMaxOffsetY, distanceLerp);
            var dropOffsetX =
                Mathf.Clamp(-startAnchoredPos.x * 0.12f, -m_StarsDropMaxOffsetX, m_StarsDropMaxOffsetX)
                + UnityEngine.Random.Range(-18.0f, 18.0f);
            var dropAnchoredPos = startAnchoredPos + new Vector2(dropOffsetX, -dropOffsetY);

            var curveArcHeight = Mathf.Lerp(m_StarsCurveMinArcHeight, m_StarsCurveMaxArcHeight, distanceLerp);
            var curveSideOffset =
                Mathf.Clamp(startAnchoredPos.x * 0.18f, -m_StarsCurveMaxSideOffset, m_StarsCurveMaxSideOffset)
                + UnityEngine.Random.Range(-20.0f, 20.0f);
            var curveControlPos = dropAnchoredPos * 0.55f + new Vector2(curveSideOffset, curveArcHeight);

            // 先做一次短促下坠，让星星先“掉出来”；再沿二次曲线吸回经验条，末段加速来补出被吸走的手感。
            var flySequence = DOTween.Sequence();
            flySequence.Append(
                DOTween.To(
                        () => starsEff.anchoredPosition,
                        value => starsEff.anchoredPosition = value,
                        dropAnchoredPos,
                        m_StarsDropDuration)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(starsEff));
            flySequence.Append(
                DOTween.To(
                        () => 0.0f,
                        value => starsEff.anchoredPosition = EvaluateQuadraticBezier(dropAnchoredPos, curveControlPos, Vector2.zero, value),
                        1.0f,
                        m_StarsFlyDuration)
                    .SetEase(Ease.InCubic)
                    .SetTarget(starsEff));
            flySequence.SetTarget(starsEff);
            flySequence.OnComplete(() =>
            {
                PunchScaleLollipop();
                RecycleStarEffectToPool(starsEff, false);
                PlayTeamExpArrivalFeedback();
            });
        }
        private void PunchScaleLollipop()
        {
            m_View.m_slider_team_exp_root.transform.DOKill();
            m_View.m_slider_team_exp_root.transform.localScale = Vector3.one;
            m_View.m_slider_team_exp_root.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.7f, 5, 0.5f);
            AudioManager.GetInstance().Play2D(105);
        }
        private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            float inverseT = 1.0f - t;
            return inverseT * inverseT * start + 2.0f * inverseT * t * control + t * t * end;
        }

        private void RecycleStarEffectToPool(RectTransform starsEff, bool killTween)
        {
            if (starsEff == null)
            {
                return;
            }

            if (killTween)
            {
                starsEff.DOKill();
            }

            //starsEff.gameObject.SetActive(false);
            starsEff.GetComponent<ParticleSystem>().Stop();
            m_StarsEffectActiveList.Remove(starsEff);
            if (!m_StarsEffectList.Contains(starsEff))
            {
                m_StarsEffectList.Add(starsEff);
            }
        }

        private void PlayTeamExpArrivalFeedback()
        {
            KillTeamExpFillTween();
            if (m_TeamExpRuntimeMaterial != null)
            {
                float currentFill = m_TeamExpRuntimeMaterial.GetFloat("_FillAmount");
                m_TeamExpFillTween = DOTween.To(() => currentFill, x =>
                {
                    currentFill = x;
                    SetTeamExpFillAmount(currentFill);
                }, teamExpValue, 0.3f)
                    .SetTarget(this)
                    .OnKill(() => m_TeamExpFillTween = null);
            }

            if (m_Lollipop_Particle == null)
            {
                return;
            }

            StopLollipopParticleRuntime();
            m_Lollipop_Particle.Play();
            m_Lollipop_counter = AddCounter(1000, 1, () =>
            {
                m_Lollipop_counter = 0;
                if (m_Lollipop_Particle != null)
                {
                    m_Lollipop_Particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
            });
        }

        private void CleanupStarsEffectRuntime()
        {
            KillTeamExpFillTween();
            StopLollipopParticleRuntime();

            for (int i = m_StarsEffectActiveList.Count - 1; i >= 0; i--)
            {
                RecycleStarEffectToPool(m_StarsEffectActiveList[i], true);
            }

            for (int i = m_StarsEffectList.Count - 1; i >= 0; i--)
            {
                var starsEff = m_StarsEffectList[i];
                if (starsEff == null)
                {
                    continue;
                }

                starsEff.DOKill();
                starsEff.gameObject.SetActive(false);
            }
        }

        private void KillTeamExpFillTween()
        {
            if (m_TeamExpFillTween == null)
            {
                return;
            }

            m_TeamExpFillTween.Kill(false);
            m_TeamExpFillTween = null;
        }

        private void StopLollipopParticleRuntime()
        {
            if (m_Lollipop_counter != 0)
            {
                CounterManager.GetInstance().RemoveCounter(m_Lollipop_counter);
                m_Lollipop_counter = 0;
            }

            if (m_Lollipop_Particle != null)
            {
                m_Lollipop_Particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void RefreshFromCurrentBattle()
        {
            RefreshView();
        }

        private TowerDefendBattle ReadCurrentBattle()
        {
            return BattleManager.GetBattle() as TowerDefendBattle;
        }

        private TowerDefendBattleScene ReadCurrentBattleScene()
        {
            var battleLogic = CBattleLogic.GetInstance();
            return battleLogic != null ? battleLogic.GetScene() as TowerDefendBattleScene : null;
        }

        private void OnBattleResult(BattleResultData result)
        {
            UIManager.CloseWindow(this);
        }

        private void OnTowerDefendBattleStateChanged()
        {
            RefreshRuntimeView();
        }

        private void OnTowerDefendPlayerSkillEnergyFullStateChanged(int seatId)
        {
            if (!IsInitializedView())
            {
                return;
            }

            var battle = ReadCurrentBattle();
            if (battle == null)
            {
                return;
            }

            var slotView = GetPlayerSlotView(seatId);
            if (slotView == null || slotView.m_Bridge == null)
            {
                return;
            }

            var player = battle.ReadPlayerBySeat(seatId);
            if (player == null)
            {
                RenderAPI.SetActiveIfNeed(slotView.m_eff_man, false);
                RenderAPI.SetActive(slotView.m_Bridge, false);
                return;
            }

            RefreshPlayerSlot(battle, seatId, slotView);
        }

        private void OnAddTeamExp()
        {
            var battle = ReadCurrentBattle();
            if (battle == null)
            {
                return;
            }

            UpdateBattleInfo(battle);
            UpdateTeamExp(battle);
        }

        private void OnTowerDefendBaseHealthChanged(int current, int max)
        {
            var battle = ReadCurrentBattle();
            if (battle == null)
            {
                return;
            }
            UpdateBaseBloodFill(battle.ReadBaseHealth(), battle.ReadBaseMaxHealth());
            RenderAPI.SetText(m_View.m_baseBlood_Txt, battle.ReadBaseHealth().ToString());

            //设置基地受击 红光闪烁效果
            if(m_baseHitCanvasGroup.alpha == 0)
            {
                m_baseHitCanvasGroup.DOKill();
                m_baseHitCanvasGroup.DOFade(1, m_baseHitEffTime).OnComplete(() =>
                {
                    m_baseHitCanvasGroup.DOFade(0, m_baseHitEffTime);
                });
            }
        }

        private void UpdateBaseBloodFill(int current, int max)
        {
            KillBaseBloodFillTween();
            float currentFill = m_View.m_baseBlood_Fill.fillAmount;
            m_BaseBloodFillTween = DOTween.To(() => currentFill, x =>
            {
                currentFill = x;
                m_View.m_baseBlood_Fill.fillAmount = x;
            }, (float)current / max, m_BaseBloodSlowDuration)
                .SetTarget(this)
                .OnKill(() => m_BaseBloodFillTween = null);
        }

        private void KillBaseBloodFillTween()
        {
            if (m_BaseBloodFillTween == null)
            {
                return;
            }
            m_BaseBloodFillTween.Kill(false);
            m_BaseBloodFillTween = null;
        }

        private void RefreshView()
        {
            if (!IsInitializedView())
            {
                return;
            }

            var battle = ReadCurrentBattle();
            if (battle == null)
            {
                return;
            }
            UpdateHudPhaseState(battle);
            UpdateBattleInfo(battle);
            UpdateTeamExp(battle);
            RefreshPlayerSlots(battle);
        }

        private void RefreshRuntimeView()
        {
            if (!IsInitializedView())
            {
                return;
            }

            var battle = ReadCurrentBattle();
            if (battle == null)
            {
                return;
            }

            UpdateHudPhaseState(battle);
            UpdateBattleInfo(battle);
            UpdateTeamExp(battle);
            RefreshPlayerSlots(battle);
        }

        private void UpdateBattleInfo(TowerDefendBattle battle)
        {
            if (battle == null)
            {
                return;
            }

            RenderAPI.SetText(m_View.m_txt_level_count, battle.GetStage().ToString());
            RenderAPI.SetText(m_View.m_txt_wave, RenderAPI.GetTextByLanId(m_LanTdHudWave, battle.ReadCurrentWave(), Math.Max(1, battle.ReadMaxWave())));
            RenderAPI.SetText(m_View.m_txt_team_exp, BuildTeamExpText(battle));
            RenderAPI.SetText(m_View.m_txt_prepare, BuildPrepareText(battle));
        }

        private void UpdateTeamExp(TowerDefendBattle battle)
        {
            if (battle == null)
            {
                SetTeamExpFillAmount(0f);
                return;
            }

            long displayCurrent = battle.ReadTeamExpCurrent();
            long displayMax = battle.ReadTeamExpMax();
            if (battle.ReadIsUpgradeChallengeActive() && displayMax > 0)
            {
                displayCurrent = (long)(displayMax * Math.Max(0f, battle.ReadUpgradeChallengeLeft()) / TowerDefendBattle.m_UpgradeChallengeDuration);
            }

            teamExpValue = displayMax > 0 ? (float)displayCurrent / displayMax : 0f;
        }
        private void UpdateAllPlayersDps()
        {
            if (!IsInitializedView())
            {
                return;
            }

            var battle = ReadCurrentBattle();
            if (battle == null)
            {
                return;
            }
            UpdatePlayerRank();
            ScoreChangeEffect(m_dps_player[0].m_light_0.GetComponent<RectTransform>(), 0);
            ScoreChangeEffect(m_dps_player[1].m_light_0.GetComponent<RectTransform>(), 1);
            ScoreChangeEffect(m_dps_player[2].m_light_0.GetComponent<RectTransform>(), 2);
            ScoreChangeEffect(m_dps_player[3].m_light_0.GetComponent<RectTransform>(), 3);
            RenderAPI.SetText(m_dps_player[0].m_txt_dps0, battle.ReadUpgradeChallengeScoreBySeat(0).ToString());
            RenderAPI.SetText(m_dps_player[1].m_txt_dps0, battle.ReadUpgradeChallengeScoreBySeat(1).ToString());
            RenderAPI.SetText(m_dps_player[2].m_txt_dps0, battle.ReadUpgradeChallengeScoreBySeat(2).ToString());
            RenderAPI.SetText(m_dps_player[3].m_txt_dps0, battle.ReadUpgradeChallengeScoreBySeat(3).ToString());
        }
        Dictionary<int, int> playersRank = new();   //seat,rank
        private void UpdatePlayerRank()
        {
            const float POSITION_OFFSET_Y = -74f;

            var battle = ReadCurrentBattle();
            Dictionary<int, int> seatScores = new();
            for (int seat = 0; seat < 4; seat++)
            {
                var player = battle != null ? battle.ReadPlayerBySeat(seat) : null;
                if (player == null) continue;
                seatScores.Add(seat, battle.ReadUpgradeChallengeScoreBySeat(seat));
            }

            var sorted = seatScores.OrderByDescending(item => item.Value).ToList();
            for (int rank = 0; rank < sorted.Count; rank++)
            {
                int seat = sorted[rank].Key;
                if (!playersRank.ContainsKey(seat))
                {
                    playersRank.Add(seat, rank);
                }
                else
                {
                    if (playersRank[seat] == rank) continue;
                    playersRank[seat] = rank;
                }
                RenderAPI.SetText(m_dps_player[seat].m_txt_rank0, (rank + 1).ToString());
                m_dps_player[seat].m_Bridge.transform.DOKill();
                m_dps_player[seat].m_Bridge.transform.DOLocalMoveY(rank * POSITION_OFFSET_Y, 0.5f);
                //dps_players[seat].transform.localPosition = new Vector2(139+ (rank * 6), dps_players[seat].transform.localPosition.y);
                m_dps_player[seat].m_Bridge.transform.DOScale(1.2f, 0.25f).OnComplete(() => m_dps_player[seat].m_Bridge.transform.DOScale(1f, 0.25f));
                if (rank == 0)
                {
                    m_dps_player[seat].m_Bridge.GetComponent<RectTransform>().sizeDelta = new Vector2(290 * 1.2f, m_dps_player[seat].m_Bridge.GetComponent<RectTransform>().sizeDelta.y);
                }
                else
                {
                    m_dps_player[seat].m_Bridge.GetComponent<RectTransform>().sizeDelta = new Vector2(290 * (1.09f - rank * 0.03f), m_dps_player[seat].m_Bridge.GetComponent<RectTransform>().sizeDelta.y);
                }
            }
        }
        private void OnStartUpgradeChallenge()
        {
            var battle = ReadCurrentBattle();
            if (battle == null)
            {
                return;
            }
            UpdateHudPhaseState(battle);
            RefreshPlayerSlots(battle);
        }

        private void OnFinishUpgradeChallenge()
        {
            var battle = ReadCurrentBattle();
            if (battle == null)
            {
                StopDpsRefreshTimer();
                SetTeamExpFillAmount(0f);
                return;
            }
            UpdateHudPhaseState(battle);
            UpdateTeamExp(battle);
            RefreshPlayerSlots(battle);
        }

        private void UpdateHudPhaseState(TowerDefendBattle battle)
        {
            if (battle == null)
            {
                StopDpsRefreshTimer();
                SetFightInfoDpsState(0);
                return;
            }

            var showUpgradeChallengeDps = battle.ReadIsUpgradeChallengeActive();
            SetFightInfoDpsState(showUpgradeChallengeDps ? 1 : 0);

            if (!showUpgradeChallengeDps)
            {
                StopDpsRefreshTimer();
                return;
            }

            UpdateDpsPlayerVisibility(battle);
            UpdateAllPlayersDps();
            EnsureDpsRefreshTimer();
        }

        private void UpdateDpsPlayerVisibility(TowerDefendBattle battle)
        {
            if (battle == null)
            {
                return;
            }

            RenderAPI.SetActive(m_View.m_dps_player0, battle.ReadPlayerBySeat(0) != null);
            RenderAPI.SetActive(m_View.m_dps_player1, battle.ReadPlayerBySeat(1) != null);
            RenderAPI.SetActive(m_View.m_dps_player2, battle.ReadPlayerBySeat(2) != null);
            RenderAPI.SetActive(m_View.m_dps_player3, battle.ReadPlayerBySeat(3) != null);
        }

        private void EnsureDpsRefreshTimer()
        {
            if (m_DpsRefreshTimerId != 0)
            {
                return;
            }

            m_DpsRefreshTimerId = CounterManager.GetInstance().AddCounter(500, 1, () =>
            {
                UpdateAllPlayersDps();
            });
        }

        private void StopDpsRefreshTimer()
        {
            if (m_DpsRefreshTimerId == 0)
            {
                return;
            }

            CounterManager.GetInstance().RemoveCounter(m_DpsRefreshTimerId);
            m_DpsRefreshTimerId = 0;
        }

        private void SetFightInfoDpsState(int value)
        {
            if (m_FightInfoAnimator == null)
            {
                return;
            }
            if (value == 1)
            {
                SetTeamExpFillAmount(0f);
            }
            m_FightInfoAnimator.SetInteger(m_FightInfoDpsParam, value);
        }

        // UI 资源在编辑器下可能直接来自工程材质资源，先克隆一份运行时材质再改参数，
        // 避免 FillAmount 的写入把预制体或材质资源本体改脏。
        private void InitTeamExpMaterial()
        {
            if (m_View == null || m_View.m_team_exp == null || m_TeamExpRuntimeMaterial != null)
            {
                return;
            }

            m_TeamExpOriginMaterial = m_View.m_team_exp.material;
            if (m_TeamExpOriginMaterial == null)
            {
                return;
            }

            m_TeamExpRuntimeMaterial = new Material(m_TeamExpOriginMaterial);
            m_TeamExpRuntimeMaterial.name = m_TeamExpOriginMaterial.name + "_Runtime";
            m_View.m_team_exp.material = m_TeamExpRuntimeMaterial;
        }

        private void ReleaseTeamExpMaterial()
        {
            if (m_View != null && m_View.m_team_exp != null && m_TeamExpOriginMaterial != null)
            {
                m_View.m_team_exp.material = m_TeamExpOriginMaterial;
            }

            if (m_TeamExpRuntimeMaterial != null)
            {
                UnityEngine.Object.Destroy(m_TeamExpRuntimeMaterial);
                m_TeamExpRuntimeMaterial = null;
            }

            m_TeamExpOriginMaterial = null;
        }

        private void SetTeamExpFillAmount(float fillAmount)
        {
            InitTeamExpMaterial();
            if (m_TeamExpRuntimeMaterial == null)
            {
                return;
            }

            m_TeamExpRuntimeMaterial.SetFloat("_FillAmount", fillAmount);
        }
        private List<int> m_playerScore = new List<int>() { 0, 0, 0, 0 };
        private void ScoreChangeEffect(RectTransform light, int index)
        {
            var battle = ReadCurrentBattle();
            int score = battle.ReadUpgradeChallengeScoreBySeat(index);
            if (m_playerScore[index] > score) m_playerScore[index] = 0;
            if (m_playerScore.Count <= index) return;
            if (m_playerScore[index] != score)       //积分变化了
            {
                m_dps_player[index].m_txt_dps0.transform.DOScale(1.4f, 0.2f).OnComplete(() => { m_dps_player[index].m_txt_dps0.transform.DOScale(1, 0.2f); });
            }
            int oldScore = m_playerScore[index];
            m_playerScore[index] = score;
            if (score / 10 <= oldScore / 10) return;    //每10积分触发下面代码
            if (DOTween.IsTweening(light)) return;      //movex未播完则不再重新触发,保持当前动画自然播放
            light.localPosition = new Vector2(-200, light.localPosition.y);
            light.DOLocalMoveX(m_dps_player[index].m_Bridge.GetComponent<RectTransform>().rect.width, 1);
        }

        private Dictionary<int, Entity> m_bossDic = new();
        float m_currHP = 0;
        float m_maxHP = 0;
        Tween Slowly_Tween;
        Tween tween;
        private void OnBossHealthChanged(Entity entity)
        {
            if (entity == null ||
                m_View == null ||
                m_topAni == null ||
                m_View.m_slider_base_hp == null ||
                m_Fill_Slowly == null ||
                fillDict == null ||
                fillDict.Count <= 0)
            {
                return;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            MonsterType kind = MonsterType.Normal;
            if (battle != null)
            {
                kind = battle.ReadMonsterKind(entity.ReadId());
            }
            if (kind != MonsterType.Elite && kind != MonsterType.Boss)
            {
                return;
            }

            bool isNewBoss = !m_bossDic.ContainsKey(entity.ReadId());
            if (isNewBoss)
            {
                m_bossDic[entity.ReadId()] = entity;
            }

            float lastCurrHP = m_currHP;
            float lastMaxHP = m_maxHP;
            float lastBaseHpValue = ResolveBossHealthBarValue(lastCurrHP);
            bool hasLastBossHealth = lastCurrHP > m_BossHealthEpsilon && lastMaxHP > m_BossHealthEpsilon;
            m_currHP = 0;
            m_maxHP = 0;
            List<int> invalidBossIds = null;
            foreach (var kv in m_bossDic)
            {
                var bossEntity = kv.Value;
                if (bossEntity == null ||
                    bossEntity.ReadIsDestroy() ||
                    bossEntity.ReadHP() <= m_BossHealthEpsilon)
                {
                    if (invalidBossIds == null)
                    {
                        invalidBossIds = new List<int>();
                    }

                    invalidBossIds.Add(kv.Key);
                    continue;
                }

                m_maxHP += bossEntity.GetMaxHP();
                m_currHP += bossEntity.ReadHP();
            }
            if (invalidBossIds != null)
            {
                for (int i = 0; i < invalidBossIds.Count; i++)
                {
                    m_bossDic.Remove(invalidBossIds[i]);
                }
            }

            // Boss/精英尚未刷完时隐藏血条
            if (battle != null && battle.ReadRemainingSpecialSpawnCount() > 1)
            {
                m_topAni.SetInteger("boss", 0);
                KillBossHealthSlowTween();
                return;
            }

            if (m_currHP <= m_BossHealthEpsilon || m_maxHP <= m_BossHealthEpsilon)
            {
                m_topAni.SetInteger("boss", 0);
                m_currHP = 0;
                m_maxHP = 0;
                KillBossHealthSlowTween();
                m_View.m_slider_base_hp.SetValueWithoutNotify(0);
                m_Fill_Slowly.SetValueWithoutNotify(0);
                return;
            }

            m_topAni.SetInteger("boss", 1);
            int bloodCurrCount = ResolveBossHealthBarCount(m_currHP);
            int bloodIndex = ResolveBossHealthFillIndex(bloodCurrCount);
            float baseHpValue = ResolveBossHealthBarValue(m_currHP);
            RenderAPI.SetText(m_View.m_txt_base_hp, "x" + bloodCurrCount);
            RefreshBossHealthFillVisible(bloodIndex, m_currHP < m_BossHealthOneBarValue);
            m_View.m_slider_base_hp.SetValueWithoutNotify(1);
            m_View.m_slider_base_hp.fillRect = fillDict[bloodIndex];
            fillDict[(bloodIndex - 1 + fillDict.Count) % fillDict.Count].SetAsLastSibling();
            m_Fill_Slowly.transform.SetAsLastSibling();
            fillDict[bloodIndex].SetAsLastSibling();
            m_View.m_slider_base_hp.SetValueWithoutNotify(baseHpValue);

            bool isHealthSetup = !hasLastBossHealth ||
                isNewBoss ||
                m_maxHP > lastMaxHP + m_BossHealthEpsilon ||
                m_currHP >= lastCurrHP - m_BossHealthEpsilon;
            if (isHealthSetup)
            {
                KillBossHealthSlowTween();
                m_Fill_Slowly.SetValueWithoutNotify(baseHpValue);
                return;
            }

            PlayBossHealthSlowTween(lastBaseHpValue, baseHpValue);
        }
        Tween m_lollipopSlowTween;
        private void OnLollipopHealthChanged(int maxHealth,int currHealth,bool isShowBlood = true)
        {
            if (!isShowBlood || currHealth <= 0)
            {
                m_View.m_txt_wave.GetComponent<CanvasGroup>().DOFade(1,0.5f);
                m_View.m_slider_lollipop_bg.transform.DOLocalMoveY(70, 0.5f).SetEase(Ease.InOutBack).SetDelay(0.5f);
                if (!isShowBlood) return;
            }
            else
            {
                m_View.m_txt_wave.GetComponent<CanvasGroup>().DOFade(0, 0.5f);
                m_View.m_slider_lollipop_bg.transform.DOLocalMoveY(-70, 0.5f).SetEase(Ease.InOutBack);
            }

            int currBloodCount = (int)(currHealth / m_LollipopHealthOneBarValue) + 1;
            float currBlood = (currHealth % m_LollipopHealthOneBarValue) / m_LollipopHealthOneBarValue;
            int bloodIndex = (Mathf.Max(1, currBloodCount) - 1) % LollipopFillDict.Count;
            if (currBloodCount<=1)
            {
                for(int i = 1;i< LollipopFillDict.Count; i++)
                {
                    LollipopFillDict[i].gameObject.SetActive(false);
                }
            }
            else if(maxHealth == currHealth)
            {
                for (int i = 1; i < LollipopFillDict.Count; i++)
                {
                    LollipopFillDict[i].gameObject.SetActive(true);
                }
            }

            RenderAPI.SetText(m_View.m_txt_lollipop_hp, "x" + currBloodCount);
            m_View.m_slider_lollipop_hp.SetValueWithoutNotify(1);
            m_View.m_slider_lollipop_hp.fillRect = LollipopFillDict[bloodIndex];
            LollipopFillDict[(bloodIndex - 1 + LollipopFillDict.Count) % LollipopFillDict.Count].SetAsLastSibling();
            m_View.m_lollipopFill_Slowly.transform.SetAsLastSibling();
            LollipopFillDict[bloodIndex].SetAsLastSibling();
            m_View.m_slider_lollipop_hp.SetValueWithoutNotify(currBlood);
            PlayLollipopSlowTween(m_View.m_lollipopFill_Slowly,currBlood);
        }
        private static int ResolveBossHealthBarCount(float hp)
        {
            if (hp <= m_BossHealthEpsilon)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.CeilToInt((hp - m_BossHealthEpsilon) / m_BossHealthOneBarValue));
        }

        private int ResolveBossHealthFillIndex(int bloodCount)
        {
            if (fillDict == null || fillDict.Count <= 0)
            {
                return 0;
            }

            return (Mathf.Max(1, bloodCount) - 1) % fillDict.Count;
        }

        private static float ResolveBossHealthBarValue(float hp)
        {
            if (hp <= m_BossHealthEpsilon)
            {
                return 0f;
            }

            float value = hp % m_BossHealthOneBarValue / m_BossHealthOneBarValue;
            if (value <= m_BossHealthEpsilon)
            {
                return 1f;
            }

            return Mathf.Clamp01(value);
        }

        private void RefreshBossHealthFillVisible(int bloodIndex, bool onlyCurrentBar)
        {
            int fillCount = fillDict != null ? fillDict.Count : 0;
            for (int i = 0; i < fillCount; i++)
            {
                var fill = fillDict[i];
                if (fill != null)
                {
                    fill.gameObject.SetActive(!onlyCurrentBar || i == bloodIndex);
                }
            }
        }

        private void PlayBossHealthSlowTween(float lastBaseHpValue, float baseHpValue)
        {
            //KillBossHealthSlowTween();

            // 扣血跨过整管边界时，当前可见血条会从低值切换到下一管高值，慢血条要从满值开始追随。
            if (baseHpValue > lastBaseHpValue)
            {
                KillBossHealthSlowTween();
                m_Fill_Slowly.SetValueWithoutNotify(1f);
            }
            else if (m_Fill_Slowly.value <= baseHpValue)
            {
                m_Fill_Slowly.SetValueWithoutNotify(Mathf.Max(lastBaseHpValue, baseHpValue));
            }

            float playbackTime = 0;
            if (tween != null)
            {
                playbackTime = tween.Elapsed();
            }
            tween = m_Fill_Slowly.DOValue(baseHpValue, m_BossHealthSlowDuration)
                .SetDelay(m_BossHealthSlowDelay - playbackTime)
                .OnKill(() =>
                {
                    if (Slowly_Tween == tween)
                    {
                        Slowly_Tween = null;
                    }
                });
            Slowly_Tween = tween;
        }
        private void PlayLollipopSlowTween(Slider slider,float currBlood)
        {
            if(slider.value < currBlood)
            {
                slider.DOKill();
                slider.SetValueWithoutNotify(1f);
            }

            float playbackTime = 0;
            if(m_lollipopSlowTween != null)
            {
                playbackTime = m_lollipopSlowTween.Elapsed();
            }
            m_lollipopSlowTween = slider.DOValue(currBlood, m_LollipopHealthSlowDuration)
            .SetDelay(m_LollipopHealthSlowDelay - playbackTime);

        }

        private void KillBossHealthSlowTween()
        {
            if (Slowly_Tween != null)
            {
                Slowly_Tween.Kill(false);
                Slowly_Tween = null;
            }

            if (m_Fill_Slowly != null)
            {
                m_Fill_Slowly.DOKill();
            }
        }

        private void ClearBossHealthRuntime()
        {
            KillBossHealthSlowTween();
            m_bossDic.Clear();
            m_currHP = 0f;
            m_maxHP = 0f;
        }



        private string BuildPrepareText(TowerDefendBattle battle)
        {
            if (battle == null)
            {
                return string.Empty;
            }

            if (battle.ReadIsFinished())
            {
                return RenderAPI.GetTextByLanId(m_LanTdHudBattleEnd);
            }

            if (battle.ReadIsPreparePhase())
            {
                var leftSeconds = Mathf.CeilToInt(battle.ReadPrepareLeft());
                return RenderAPI.GetTextByLanId(m_LanTdHudPrepare, leftSeconds);
            }

            if (battle.ReadIsUpgradeChallengeCountdown())
            {
                var leftSeconds = Mathf.CeilToInt(battle.ReadUpgradeChallengeCountdownLeft());
                return RenderAPI.GetTextByLanId(m_LanTdHudLevelUpTime, leftSeconds);
            }

            if (battle.ReadIsUpgradeChallengeActive())
            {
                var leftSeconds = Mathf.CeilToInt(battle.ReadUpgradeChallengeLeft());
                return RenderAPI.GetTextByLanId(m_LanTdHudLevelUpTime, leftSeconds);
            }

            if (battle.ReadIsBattleRunning())
            {
                return RenderAPI.GetTextByLanId(m_LanTdHudBattleRun);
            }

            return string.Empty;
        }

        private string BuildTeamExpText(TowerDefendBattle battle)
        {
            if (battle == null)
            {
                return string.Empty;
            }

            if (battle.ReadIsUpgradeChallengeCountdown())
            {
                var leftSeconds = Mathf.CeilToInt(battle.ReadUpgradeChallengeCountdownLeft());
                return RenderAPI.GetTextByLanId(m_LanTdHudLevelUpTime, leftSeconds);
            }

            if (battle.ReadIsUpgradeChallengeActive())
            {
                return RenderAPI.GetTextByLanId(m_LanTdHudLevelUpRun);
            }

            if (battle.ReadIsUpgradeChallengeReady())
            {
                return RenderAPI.GetTextByLanId(m_LanTdHudLevelUpReady);
            }

            return RenderAPI.GetTextByLanId(m_LanTdHudTeamExp, Mathf.Clamp(battle.ReadTeamExpPercent() / 100, 0, 100));
        }
        private void RefreshPlayerSlots(TowerDefendBattle battle)
        {
            for (int i = 0; i < m_MaxHudPlayerCount; i++)
            {
                var slotView = GetPlayerSlotView(i);
                if (slotView == null || slotView.m_Bridge == null)
                {
                    continue;
                }
                var player = battle != null ? battle.ReadPlayerBySeat(i) : null;
                if (player == null)
                {
                    RenderAPI.SetActiveIfNeed(slotView.m_eff_man, false);
                    ClearBattleBoneDebugView(slotView);
                    RenderAPI.SetActive(slotView.m_Bridge, false);
                    continue;
                }
                RenderAPI.SetActive(slotView.m_Bridge, true);
                if (player.mappedIndex < 2)
                {
                    slotView.m_Bridge.transform.SetParent(m_View.m_PlayerSlots.transform);
                }
                else
                {
                    slotView.m_Bridge.transform.SetParent(m_View.m_PlayerSlots.transform);
                }
                RefreshPlayerSlot(battle, i, slotView);
            }
            RefreshBattleHeadTextureViewBinding();
        }

        private void RefreshPlayerSlot(TowerDefendBattle battle, int seatId, v_tower_defend_battle_hud_wnd.v_PlayerSlot slotView)
        {
            ClearBattleBoneDebugView(slotView);
            var skill = battle.ReadPrimaryManualSkillBySeat(seatId);
            RenderAPI.SetText(slotView.m_txt_level, ReadSkillLevelText(skill));
            RefreshEnergyFill(battle, seatId, skill, slotView);
            RefreshSkillCooldown(skill, slotView);
        }

        private static void ClearBattleBoneDebugView(v_tower_defend_battle_hud_wnd.v_PlayerSlot slotView)
        {
            if (slotView == null)
            {
                return;
            }

            // 战斗界面不再显示骨骼调试信息，节点保留给预制件兼容，运行时统一隐藏。
            RenderAPI.SetActive(slotView.m_debugPanel, false);
            RenderAPI.SetText(slotView.m_txtBoneState, string.Empty);
            RenderAPI.SetText(slotView.m_txtBoneConnect, string.Empty);
        }

        private static string ReadSkillLevelText(Skill skill)
        {
            return HasValidSkill(skill) ? Mathf.Max(0, skill.ReadLevel()).ToString() : "0";
        }

        private static void RefreshEnergyFill(TowerDefendBattle battle, int seatId, Skill skill, v_tower_defend_battle_hud_wnd.v_PlayerSlot slotView)
        {
            if (slotView == null)
            {
                return;
            }

            float energyPercent = 0f;
            if (battle == null || !HasValidSkill(skill))
            {
                if (slotView.m_fill_energy != null)
                {
                    slotView.m_fill_energy.fillAmount = 0f;
                }

                RenderAPI.SetActiveIfNeed(slotView.m_eff_man, false);
                return;
            }

            energyPercent = battle.ReadPlayerSkillEnergyPercentBySeat(seatId);
            if (slotView.m_fill_energy != null)
            {
                slotView.m_fill_energy.fillAmount = Mathf.Clamp01(energyPercent / m_SkillEnergyFullFillValue);
            }

            RenderAPI.SetActiveIfNeed(slotView.m_eff_man, energyPercent >= m_SkillEnergyFullFillValue);
        }

        private static void RefreshSkillCooldown(Skill skill, v_tower_defend_battle_hud_wnd.v_PlayerSlot slotView)
        {
            if (slotView == null)
            {
                return;
            }

            float cooldownLeft = HasValidSkill(skill) ? Mathf.Max(0f, skill.ReadCooldownLeftTime()) : 0f;
            bool showCooldown = cooldownLeft > 0.0001f;
            RenderAPI.SetActive(slotView.m_skill_cd, showCooldown);
            if (slotView.m_fill_skill_cd != null)
            {
                float cooldownFill = 0f;
                if (showCooldown)
                {
                    float cooldownTotal = Mathf.Max(0.0001f, skill.ReadCooldownTime());
                    cooldownFill = Mathf.Clamp01(cooldownLeft / cooldownTotal);
                }

                slotView.m_fill_skill_cd.fillAmount = cooldownFill;
            }

            if (slotView.m_txt_skill_cd != null)
            {
                RenderAPI.SetText(slotView.m_txt_skill_cd, showCooldown ? Mathf.CeilToInt(cooldownLeft).ToString() : string.Empty);
            }
        }

        private void InitPlayerSlotViews()
        {
            for (int i = 0; i < m_MaxHudPlayerCount; i++)
            {
                var slotBridge = GetPlayerSlotBridge(i);
                if (slotBridge == null)
                {
                    m_PlayerSlotViews[i] = null;
                    continue;
                }

                var slotView = new v_tower_defend_battle_hud_wnd.v_PlayerSlot();
                slotView.InitComponent(slotBridge.gameObject);
                ClearBattleBoneDebugView(slotView);
                m_PlayerSlotViews[i] = slotView;
            }
        }
        private void InitDpsPlayerViews()
        {
            for(int i = 0;i< m_MaxHudPlayerCount; i++)
            {
                var dpsBridge = GetDpsPlayerBridge(i);
                if (dpsBridge == null)
                {
                    m_dps_player[i] = null;
                    continue;
                }
                var dpsView = new v_tower_defend_battle_hud_wnd.v_dps_player();
                dpsView.InitComponent(dpsBridge.gameObject);
                m_dps_player[i] = dpsView;
            }
        }
        private void InitPlayersUI()
        {
            var players = BattleManager.GetBattle().ReadPlayers();
            for(int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                SetPlayerUI(player);
            }
        }
        private void SetPlayerUI(BattlePlayer player)
        {
            var slotView = m_PlayerSlotViews[player.m_SeatId];

            __SetImage(slotView.m_bg, "texture_set/play.jpg", "playlevel_100" + player.m_SeatId);
            __SetImage(slotView.m_fill_energy, "texture_set/play.jpg", "playlevelfill_100" + player.m_SeatId);

            var dpsView = m_dps_player[player.m_SeatId];

            __SetImage(dpsView.m_mask, "texture_set/play.jpg", "dps_play_100" + player.m_SeatId);
            __SetImage(dpsView.m_light_0, "texture_set/play.jpg", "light_100" + player.m_SeatId);
            __SetImage(dpsView.m_luiimage2, "texture_set/play.jpg", "playrankbg_100" + player.m_SeatId);
            __SetImage(dpsView.m_luiimage1, "texture_set/play.jpg", "playrank_100" + player.m_SeatId);
        }

        private ComponentBridge GetPlayerSlotBridge(int index)
        {
            switch (index)
            {
                case 0:
                    return m_View.m_PlayerSlot0;
                case 1:
                    return m_View.m_PlayerSlot1;
                case 2:
                    return m_View.m_PlayerSlot2;
                case 3:
                    return m_View.m_PlayerSlot3;
                default:
                    return null;
            }
        }
        private ComponentBridge GetDpsPlayerBridge(int index)
        {
            switch (index)
            {
                case 0:
                    return m_View.m_dps_player0;
                case 1:
                    return m_View.m_dps_player1;
                case 2:
                    return m_View.m_dps_player2;
                case 3:
                    return m_View.m_dps_player3;
                default:
                    return null;
            }
        }
        private v_tower_defend_battle_hud_wnd.v_PlayerSlot GetPlayerSlotView(int index)
        {
            if (index < 0 || index >= m_PlayerSlotViews.Length)
            {
                return null;
            }

            return m_PlayerSlotViews[index];
        }

        private static bool HasValidSkill(Skill skill)
        {
            return skill != null && skill.ReadSkillCfgId() > 0;
        }
    }
}
