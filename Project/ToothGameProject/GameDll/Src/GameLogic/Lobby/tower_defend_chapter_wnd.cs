using System;
using System.Collections.Generic;
using LCL;
using UnityEngine;
using UnityUI;
using GameDll;
using System.Linq;

namespace GameHot
{
    public class tower_defend_chapter_model : WindowModel
    {
        public BattleStartupRequest m_Request;
        public Func<WindowBase> m_OnConfirm;
        public Action m_OnCancel;

        public override void Clear()
        {
            m_Request = null;
            m_OnConfirm = null;
            m_OnCancel = null;
        }
    }

    public class tower_defend_chapter_wnd : WindowBase
    {
        private sealed class ChapterPlateView
        {
            public LUIButton m_Button;
            public RectTransform m_Rect;
            public int m_StageId;
            public int m_BaseSlotIndex;
        }

        private sealed class ChapterWheelRuntime
        {
            public float m_SlotOffset;
            public float m_TargetSlotOffset;
            public bool m_IsWheelEnabled;
        }

        private const int m_SlotCount = 5;
        private const float m_SlotStepSpeed = 6.50f;
        private const float m_SlotEpsilon = 0.001f;
        private static readonly Vector2[] m_SlotAnchors =
        {
            new Vector2(0f, -215f),
            new Vector2(-590f, -65f),
            new Vector2(-275f, 128f),
            new Vector2(275f, 128f),
            new Vector2(590f, -65f),
        };
        private static readonly int[] m_SlotLayerRanks = { 4, 3, 1, 1, 3 };
        private static readonly Vector3[] m_SlotScales =
        {
            Vector3.one,
            new Vector3(0.72f, 0.72f, 1f),
            new Vector3(0.57f, 0.57f, 1f),
            new Vector3(0.57f, 0.57f, 1f),
            new Vector3(0.72f, 0.72f, 1f),
        };
        private readonly v_tower_defend_chapter_wnd.v_Btn_Lv[] m_Btn_Lv =
        new v_tower_defend_chapter_wnd.v_Btn_Lv[m_SlotCount];
        private v_tower_defend_chapter_wnd m_View;
        private ChapterPlateView[] m_Plates;
        private ChapterWheelRuntime m_Runtime;
        private FrameUpdate m_FrameUpdate;
        private WindowBase m_PendingCloseTargetWindow;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.lobby;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);
            __CreateModel(new tower_defend_chapter_model());
        }

        public void SetRequest(
            BattleStartupRequest request,
            Func<WindowBase> onConfirm,
            Action onCancel)
        {
            var model = GetModel<tower_defend_chapter_model>();
            model.m_Request = request;
            model.m_OnConfirm = onConfirm;
            model.m_OnCancel = onCancel;
            model.m_Request.m_StageId = 1;
            if (IsInitializedView())
            {
                RefreshView();
            }
        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_chapter_wnd();
            m_View.InitComponent(__GetWindowObj());
            Init_Btn_Lv_Views();
            InitializeWheelViews();
            m_FrameUpdate = __GetWindowObj().GetComponent<FrameUpdate>();
            if (m_FrameUpdate == null)
            {
                m_FrameUpdate = __GetWindowObj().AddComponent<FrameUpdate>();
            }

            RenderAPI.AddButtonClick(m_View.m_btnLeftRotation, OnClickLeft, 3);
            RenderAPI.AddButtonClick(m_View.m_btnRightRotation, OnClickRight, 3);
            RenderAPI.AddButtonClick(m_View.m_btnOk, OnClickConfirm);
        }

        protected override void OnOpen()
        {
            RefreshView();
        }

        protected override void OnClose()
        {
            ClearPendingCloseTargetWindow();
            StopWheelRuntime();
        }

        private void OnClickLeft()
        {
            OffsetWheelTargetByStep(-1);
        }

        private void OnClickRight()
        {
            OffsetWheelTargetByStep(1);
        }

        private void OnClickConfirm()
        {
            var model = GetModel<tower_defend_chapter_model>();
            var request = model.m_Request;
            int stageId = ResolveBottomStageId();
            if (request != null && stageId > 0)
            {
                if (!CGameProcedure.s_ProcLobby.TrySetSelectedStageId(request.m_GameMode, stageId))
                {
                    AudioManager.GetInstance().Play2D(7);
                    tip_wnd.GetInstance().OnShowTip("当前章节暂未解锁");
                    return;
                }

                request.m_StageId = stageId;
            }

            if (model.m_OnConfirm == null)
            {
                UIManager.CloseWindow(this);
                return;
            }

            var targetWindow = model.m_OnConfirm();
            if (targetWindow == null)
            {
                return;
            }

            CloseAfterTargetWindowOpened(targetWindow);
        }
        private void Init_Btn_Lv_Views()
        {
            for (int i = 0; i < m_SlotCount; i++)
            {
                var slotBridge = Get_Btn_Lv_Bridge(i);
                if (slotBridge == null)
                {
                    m_Btn_Lv[i] = null;
                    continue;
                }

                var slotView = new v_tower_defend_chapter_wnd.v_Btn_Lv();
                slotView.InitComponent(slotBridge.gameObject);
                m_Btn_Lv[i] = slotView;
            }
        }
        private ComponentBridge Get_Btn_Lv_Bridge(int index)
        {
            switch (index)
            {
                case 0:
                    return m_View.m_btn_Lv0;
                case 1:
                    return m_View.m_btn_Lv1;
                case 2:
                    return m_View.m_btn_Lv2;
                case 3:
                    return m_View.m_btn_Lv3;
                case 4:
                    return m_View.m_btn_Lv4;
                default:
                    return null;
            }
        }
        private v_tower_defend_chapter_wnd.v_Btn_Lv Get_Btn_Lv_View(int index)
        {
            if (index < 0 || index >= m_Btn_Lv.Length)
            {
                return null;
            }

            return m_Btn_Lv[index];
        }
        private void OnClickCancel()
        {
            var model = GetModel<tower_defend_chapter_model>();
            model.m_OnCancel?.Invoke();
            UIManager.CloseWindow(this);
        }

        private void CloseAfterTargetWindowOpened(WindowBase targetWindow)
        {
            if (targetWindow == null)
            {
                return;
            }

            if (targetWindow.__IsLogicOpen() &&
                targetWindow.__IsObjLoaded() &&
                targetWindow.__IsVisiable() &&
                targetWindow.__GetWindowStage() != WindowStage.Loading &&
                targetWindow.__GetWindowStage() != WindowStage.ReopenPending)
            {
                UIManager.CloseWindow(this);
                return;
            }

            ClearPendingCloseTargetWindow();
            m_PendingCloseTargetWindow = targetWindow;
            CGameProcedure.Event.OnUIOpenedEvent += OnPendingCloseTargetWindowOpened;
        }

        private void OnPendingCloseTargetWindowOpened(WindowBase openedWindow)
        {
            if (openedWindow != m_PendingCloseTargetWindow)
            {
                return;
            }

            ClearPendingCloseTargetWindow();
            if (!IsLogicClosed())
            {
                UIManager.CloseWindow(this);
            }
        }

        private void ClearPendingCloseTargetWindow()
        {
            if (m_PendingCloseTargetWindow == null)
            {
                return;
            }

            CGameProcedure.Event.OnUIOpenedEvent -= OnPendingCloseTargetWindowOpened;
            m_PendingCloseTargetWindow = null;
        }

        private void RefreshView()
        {
            if (!IsInitializedView())
            {
                return;
            }
            var request = GetModel<tower_defend_chapter_model>().m_Request;
            if (!TryBuildStageSequence(request, out var selectableStageIds, out var selectedIndex))
            {
                StopWheelRuntime();
                return;
            }
            InitUnlockCount();
            ResetWheelRuntime(selectableStageIds, selectedIndex);
            ApplyWheelVisuals();
            if (m_FrameUpdate != null)
            {
                m_FrameUpdate.SetFrameCall(OnWheelFrameUpdate);
            }
        }
        private void InitUnlockCount()
        {
            var request = GetModel<tower_defend_chapter_model>().m_Request;
            int UnlockCount = CGameProcedure.s_ProcLobby.ReadSelectableStageIds(request.m_GameMode).Count;

            for(int i=0;i< m_Btn_Lv.Count(); i++)
            {
                if (UnlockCount > i)
                {
                    Get_Btn_Lv_View(i).m_UnLock.gameObject.SetActive(true);
                    Get_Btn_Lv_View(i).m_Lock.gameObject.SetActive(false);
                    RenderAPI.SetText(Get_Btn_Lv_View(i).m_txt, (i+1).ToString());
                }
                else
                {
                    Get_Btn_Lv_View(i).m_Lock.gameObject.SetActive(true);
                    Get_Btn_Lv_View(i).m_UnLock.gameObject.SetActive(false);
                    RenderAPI.SetText(Get_Btn_Lv_View(i).m_txt, "?");
                }
            }
        }
        private void InitializeWheelViews()
        {
            m_Plates = new[]
            {
                CreatePlateView(m_View.m_btn_Lv0.GetComponent<LUIButton>(), 0),
                CreatePlateView(m_View.m_btn_Lv1.GetComponent<LUIButton>(), 1),
                CreatePlateView(m_View.m_btn_Lv2.GetComponent<LUIButton>(), 2),
                CreatePlateView(m_View.m_btn_Lv3.GetComponent<LUIButton>(), 3),
                CreatePlateView(m_View.m_btn_Lv4.GetComponent < LUIButton >(), 4),
            };

            for (int i = 0; i < m_Plates.Length; i++)
            {
                BindPlateClick(m_Plates[i]);
            }
        }

        private ChapterPlateView CreatePlateView(LUIButton button, int baseSlotIndex)
        {
            if (button == null)
            {
                return null;
            }

            return new ChapterPlateView
            {
                m_Button = button,
                m_Rect = button.transform as RectTransform,
                m_BaseSlotIndex = baseSlotIndex,
                m_StageId = 0,
            };
        }

        private void BindPlateClick(ChapterPlateView plate)
        {
            if (plate == null || plate.m_Button == null)
            {
                return;
            }

            RenderAPI.AddButtonClick(plate.m_Button, () =>
            {
                RotateStageToBottom(plate);
            });
        }

        private bool TryBuildStageSequence(BattleStartupRequest request, out List<int> selectableStageIds, out int selectedIndex)
        {
            selectableStageIds = null;
            selectedIndex = 0;
            if (request == null)
            {
                return false;
            }

            if (request.m_GameMode != BattleGameMode.Chapter)
            {
                Debug.LogError("章节转盘初始化失败：当前请求不是章节模式。 mode=" + request.m_GameMode);
                return false;
            }

            if (!HasCompletePlateViews())
            {
                Debug.LogError("章节转盘初始化失败：盘子视图引用不完整。");
                return false;
            }

            var allDisplayStageIds = CGameProcedure.s_ProcLobby.ReadChapterDisplayStageIds();
            if (allDisplayStageIds == null || allDisplayStageIds.Count < m_SlotCount)
            {
                Debug.LogError("章节转盘初始化失败：章节总表数量不足 5。");
                return false;
            }

            int globalSelectedIndex = allDisplayStageIds.IndexOf(request.m_StageId);
            if (globalSelectedIndex < 0)
            {
                Debug.LogError("章节转盘初始化失败：request.m_StageId 不在章节总表列表内，回退到第一个章节。 stageId=" + request.m_StageId);
                globalSelectedIndex = 0;
                request.m_StageId = allDisplayStageIds[0];
            }

            selectableStageIds = new List<int>(m_SlotCount);
            for (int i = 0; i < m_SlotCount; i++)
            {
                selectableStageIds.Add(allDisplayStageIds[(globalSelectedIndex + i) % allDisplayStageIds.Count]);
            }
            selectedIndex = 0;

            return true;
        }

        private bool HasCompletePlateViews()
        {
            if (m_Plates == null || m_Plates.Length != m_SlotCount)
            {
                return false;
            }

            for (int i = 0; i < m_Plates.Length; i++)
            {
                if (m_Plates[i] == null || m_Plates[i].m_Rect == null || m_Plates[i].m_Button == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void ResetWheelRuntime(List<int> selectableStageIds, int selectedIndex)
        {
            if (selectableStageIds == null || selectableStageIds.Count != m_SlotCount)
            {
                StopWheelRuntime();
                return;
            }

            if (!HasCompletePlateViews())
            {
                StopWheelRuntime();
                return;
            }

            if (m_Runtime == null)
            {
                m_Runtime = new ChapterWheelRuntime();
            }

            m_Runtime.m_SlotOffset = 0f;
            m_Runtime.m_TargetSlotOffset = 0f;
            m_Runtime.m_IsWheelEnabled = true;

            for (int i = 0; i < m_Plates.Length; i++)
            {
                m_Plates[i].m_StageId = selectableStageIds[i];
                m_Plates[i].m_BaseSlotIndex = NormalizeSlotIndex(i - selectedIndex);
                m_Plates[i].m_Rect.anchoredPosition = m_SlotAnchors[m_Plates[i].m_BaseSlotIndex];
                m_Plates[i].m_Rect.localRotation = Quaternion.identity;
                m_Plates[i].m_Rect.localScale = m_SlotScales[m_Plates[i].m_BaseSlotIndex];
            }
        }

        private void StopWheelRuntime()
        {
            if (m_FrameUpdate != null)
            {
                m_FrameUpdate.SetFrameCall(null);
            }

            if (m_Runtime == null)
            {
                return;
            }

            m_Runtime.m_IsWheelEnabled = false;
            m_Runtime.m_TargetSlotOffset = m_Runtime.m_SlotOffset;
        }

        private void OffsetWheelTargetByStep(int direction)
        {
            if (direction == 0 || m_Runtime == null || !m_Runtime.m_IsWheelEnabled)
            {
                return;
            }

            if (Mathf.Abs(m_Runtime.m_SlotOffset - m_Runtime.m_TargetSlotOffset) <= m_SlotEpsilon)
            {
                m_Runtime.m_SlotOffset = m_Runtime.m_TargetSlotOffset;
            }

            // 每次点击只改变一个槽位目标；动画途中反向点击会把目标减回上一槽位，
            // 这样会从当前状态沿原路径回退，而不是继续使用惯性滑动。
            m_Runtime.m_TargetSlotOffset += direction;
        }
        private void ShowChoose()
        {
            Get_Btn_Lv_View(0).m_choose.gameObject.SetActive(ResolveBottomStageId() == 1);
            Get_Btn_Lv_View(1).m_choose.gameObject.SetActive(ResolveBottomStageId() == 2);
            Get_Btn_Lv_View(2).m_choose.gameObject.SetActive(ResolveBottomStageId() == 3);
            Get_Btn_Lv_View(3).m_choose.gameObject.SetActive(ResolveBottomStageId() == 4);
            Get_Btn_Lv_View(4).m_choose.gameObject.SetActive(ResolveBottomStageId() == 5);
        }
        private void RotateStageToBottom(ChapterPlateView plate)
        {
            if (plate == null || m_Runtime == null || !m_Runtime.m_IsWheelEnabled)
            {
                return;
            }

            float plateSlot = ReadCurrentSlot(plate);
            if (plateSlot <= m_SlotEpsilon || plateSlot >= m_SlotCount - m_SlotEpsilon)
            {
                return;
            }

            float backwardDelta = -plateSlot;
            float forwardDelta = m_SlotCount - plateSlot;
            float bestDelta = Mathf.Abs(backwardDelta) <= Mathf.Abs(forwardDelta) ? backwardDelta : forwardDelta;

            m_Runtime.m_TargetSlotOffset = Mathf.Round(m_Runtime.m_SlotOffset + bestDelta);
        }

        private void OnWheelFrameUpdate()
        {
            UpdateWheelRuntime(Time.unscaledDeltaTime);
            ApplyWheelVisuals();
        }

        private void UpdateWheelRuntime(float dt)
        {
            if (dt <= 0f || m_Runtime == null || !m_Runtime.m_IsWheelEnabled)
            {
                return;
            }

            m_Runtime.m_SlotOffset = Mathf.MoveTowards(
                m_Runtime.m_SlotOffset,
                m_Runtime.m_TargetSlotOffset,
                m_SlotStepSpeed * dt);

            if (Mathf.Abs(m_Runtime.m_SlotOffset - m_Runtime.m_TargetSlotOffset) <= m_SlotEpsilon)
            {
                NormalizeSettledWheelOffset();
            }
            ShowChoose();
        }

        private void ApplyWheelVisuals()
        {
            if (m_Runtime == null || !HasCompletePlateViews())
            {
                return;
            }

            for (int i = 0; i < m_Plates.Length; i++)
            {
                var plate = m_Plates[i];
                float slot = ReadCurrentSlot(plate);
                int from = Mathf.FloorToInt(slot);
                float t = slot - from;
                int to = (from + 1) % m_SlotCount;

                plate.m_Rect.anchoredPosition = Vector2.Lerp(m_SlotAnchors[from], m_SlotAnchors[to], t);
                plate.m_Rect.localRotation = Quaternion.identity;
                plate.m_Rect.localScale = ResolvePlateScale(slot);
            }

            ApplySiblingOrder();
        }

        private void ApplySiblingOrder()
        {
            var orderedPlates = new List<ChapterPlateView>(m_Plates.Length);
            for (int i = 0; i < m_Plates.Length; i++)
            {
                if (m_Plates[i] != null)
                {
                    orderedPlates.Add(m_Plates[i]);
                }
            }

            orderedPlates.Sort((left, right) =>
            {
                int leftSlotIndex = ResolveNearestSlotIndex(left);
                int rightSlotIndex = ResolveNearestSlotIndex(right);
                int leftRank = m_SlotLayerRanks[leftSlotIndex];
                int rightRank = m_SlotLayerRanks[rightSlotIndex];
                if (leftRank != rightRank)
                {
                    return leftRank.CompareTo(rightRank);
                }

                return ReadCurrentSlot(left).CompareTo(ReadCurrentSlot(right));
            });

            for (int i = 0; i < orderedPlates.Count; i++)
            {
                orderedPlates[i].m_Rect.SetSiblingIndex(i);
            }
        }

        private int ResolveBottomStageId()
        {
            if (!HasCompletePlateViews())
            {
                return 0;
            }

            ChapterPlateView bestPlate = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < m_Plates.Length; i++)
            {
                float slot = ReadCurrentSlot(m_Plates[i]);
                float distance = Mathf.Min(slot, m_SlotCount - slot);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPlate = m_Plates[i];
                }
            }

            return bestPlate != null ? bestPlate.m_StageId : 0;
        }

        private float ReadCurrentSlot(ChapterPlateView plate)
        {
            if (plate == null)
            {
                return 0f;
            }

            float offset = m_Runtime != null ? m_Runtime.m_SlotOffset : 0f;
            return Mathf.Repeat(plate.m_BaseSlotIndex + offset, m_SlotCount);
        }

        private int ResolveNearestSlotIndex(ChapterPlateView plate)
        {
            int slotIndex = Mathf.RoundToInt(ReadCurrentSlot(plate));
            return NormalizeSlotIndex(slotIndex);
        }

        private static int NormalizeSlotIndex(int slotIndex)
        {
            slotIndex %= m_SlotCount;
            if (slotIndex < 0)
            {
                slotIndex += m_SlotCount;
            }

            return slotIndex;
        }

        private static float NormalizeSlotOffset(float slotOffset)
        {
            return Mathf.Repeat(slotOffset, m_SlotCount);
        }

        private void NormalizeSettledWheelOffset()
        {
            if (m_Runtime == null)
            {
                return;
            }

            float normalizedOffset = NormalizeSlotOffset(m_Runtime.m_TargetSlotOffset);
            m_Runtime.m_TargetSlotOffset = normalizedOffset;
            m_Runtime.m_SlotOffset = normalizedOffset;
        }

        private static Vector3 ResolvePlateScale(float slot)
        {
            int from = Mathf.FloorToInt(slot);
            float t = slot - from;
            int to = (from + 1) % m_SlotCount;
            return Vector3.Lerp(m_SlotScales[from], m_SlotScales[to], t);
        }

    }
}
