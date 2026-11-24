using System;
using DG.Tweening;
using MonoBean;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
using GameDll;

namespace GameHot
{
    public class tower_defend_lvchoose_model : WindowModel
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

    public class tower_defend_lvchoose_wnd : WindowBase
    {
        long m_CounterId;
        internal const int m_LevelsPerChapter = 5;
        private int m_FirstStageId = 1;
        private int m_StageUnlockCount;        //当前解锁到的关卡数
        private v_tower_defend_lvchoose_wnd m_View;
        private SlideShowScrollViewPro_Scroll m_Scroll;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.lobby;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);
            __CreateModel(new tower_defend_lvchoose_model());
        }

        public void SetRequest(
            BattleStartupRequest request,
            int chapterId,
            Func<WindowBase> onConfirm,
            Action onCancel)
        {
            var model = GetModel<tower_defend_lvchoose_model>();
            model.m_Request = request;
            m_FirstStageId = (chapterId - 1) * m_LevelsPerChapter + 1;
            model.m_OnConfirm = onConfirm;
            model.m_OnCancel = onCancel;

            if (IsInitializedView())
            {
                RefreshView();
            }
        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_lvchoose_wnd();
            m_View.InitComponent(__GetWindowObj());
            RenderAPI.AddButtonClick(m_View.m_Content_1, () => OnClickConfirm(m_View.m_Content_1.transform), 0);
            RenderAPI.AddButtonClick(m_View.m_Content_2, () => OnClickConfirm(m_View.m_Content_2.transform), 0);
            RenderAPI.AddButtonClick(m_View.m_Content_3, () => OnClickConfirm(m_View.m_Content_3.transform), 0);
            RenderAPI.AddButtonClick(m_View.m_Content_4, () => OnClickConfirm(m_View.m_Content_4.transform), 0);
            RenderAPI.AddButtonClick(m_View.m_Content_5, () => OnClickConfirm(m_View.m_Content_5.transform), 0);
            //RenderAPI.AddButtonClick(m_View.m_btn_cancel, OnClickCancel);
            RenderAPI.AddButtonClick(m_View.m_btnLeft, OnClickLeft, 0);
            RenderAPI.AddButtonClick(m_View.m_btnRight, OnClickRight, 0);

            m_Scroll = __GetWindowObj().GetComponentInChildren<SlideShowScrollViewPro_Scroll>();
            OnInitLanguage();
        }

        private void OnClickRight()
        {

            m_Scroll.SelectUpOrRightButton_Click();
        }

        private void OnClickLeft()
        {
            m_Scroll.SelectDownOrLeftButton_Click();
        }

        protected override void OnOpen()
        {
            RefreshView();
            //监听目前指向哪个关卡
            m_CounterId = AddCounter(50, -1, ListeningSelectedElementID);

            RenderEvent.Event.OnUpdateSelectionVisuals += OnUpdateSelectionVisuals;
        }

        private void OnUpdateSelectionVisuals()
        {
            var active = UIManager.GetCurrentActiveWindow();
            if(active == null  || active != this)
            {
                return;
            }
            AudioManager.GetInstance().Play2D(3);
            Debug.Log("播放tower_defend_lvchoose_model 3 音效");
        }

        protected override void OnClose()
        {
            RenderEvent.Event.OnUpdateSelectionVisuals -= OnUpdateSelectionVisuals;
            RemoveCounter(m_CounterId);
        }
        private void InitStar()
        {
            SetStar(1,m_View.m_Content_1.transform);
            SetStar(2,m_View.m_Content_2.transform);
            SetStar(3,m_View.m_Content_3.transform);
            SetStar(4,m_View.m_Content_4.transform);
            SetStar(5,m_View.m_Content_5.transform);
        }
        private void SetStar(int index, Transform obj)
        {
            int starCount = LobbyPlayer.GetInstance().GetStageStar(m_FirstStageId + index - 1);
            for (int star = 1; star <= 3; star++)
            {
                RenderAPI.SetActive(obj.Find($"Topic_1/stars{index}/star{star}/yellow_star"), star <= starCount);
            }
        }
        private void OnInitLanguage()
        {
            RenderAPI.SetTextLan(m_View.m_txt_desc, "td_lobby_lvchoose_desc");
        }
        private void ListeningSelectedElementID()
        {
            int selectedElementID = m_Scroll.selectedElementID;

            RenderAPI.SetTextLan(m_View.m_txt_title, "td_lobby_lvchoose_level", selectedElementID+1);

            m_View.m_btn_toggle_choose.gameObject.SetActive(selectedElementID == 0);
            m_View.m_btn_toggle_choose_new1.gameObject.SetActive(selectedElementID == 1);
            m_View.m_btn_toggle_choose_new2.gameObject.SetActive(selectedElementID == 2);
            m_View.m_btn_toggle_choose_new3.gameObject.SetActive(selectedElementID == 3);
            m_View.m_btn_toggle_choose_new4.gameObject.SetActive(selectedElementID == 4);
        }
        private void OnClickConfirm(Transform ts)
        {
            var model = GetModel<tower_defend_lvchoose_model>();
            int levelIndex = m_Scroll.selectedElementID;
            if (model.m_Request == null || levelIndex < 0 || levelIndex >= m_StageUnlockCount)
            {
                ts.parent.DOKill();
                ts.parent.DOShakePosition(0.5f, 10, 10);
                AudioManager.GetInstance().Play2D(7);
                return;
            }

            int stageId = m_FirstStageId + levelIndex;
            if (!CGameProcedure.s_ProcLobby.TrySetSelectedStageId(model.m_Request.m_GameMode, stageId))
            {
                tip_wnd.GetInstance().OnShowTipLanId("td_error_battle_stage_locked");
                return;
            }
            model.m_Request.m_StageId = stageId;

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

            AudioManager.GetInstance().Play2D(1);
        }

        private void RefreshView()
        {
            if (!IsInitializedView())
            {
                return;
            }
            var request = GetModel<tower_defend_lvchoose_model>().m_Request;
            if (request == null)
            {
                return;
            }
            InitStar();
            InitUnlockTopic();
        }
        //初始化关卡解锁到多少（GetUnlockedStage 是全局进度，换算成本章内已解锁的关卡数）
        private void InitUnlockTopic()
        {
            int unlockedStage = LobbyPlayer.GetInstance().GetUnlockedStage();
            m_StageUnlockCount = Mathf.Clamp(unlockedStage - m_FirstStageId + 1, 0, m_LevelsPerChapter);
            m_View.m_lock.gameObject.SetActive(m_StageUnlockCount<1);
            m_View.m_lock_new1.gameObject.SetActive(m_StageUnlockCount<2);
            m_View.m_lock_new2.gameObject.SetActive(m_StageUnlockCount<3);
            m_View.m_lock_new3.gameObject.SetActive(m_StageUnlockCount<4);
            m_View.m_lock_new4.gameObject.SetActive(m_StageUnlockCount<5);

            m_View.m_stars1.gameObject.SetActive(m_StageUnlockCount >= 1);
            m_View.m_stars2.gameObject.SetActive(m_StageUnlockCount >= 2);
            m_View.m_stars3.gameObject.SetActive(m_StageUnlockCount >= 3);
            m_View.m_stars4.gameObject.SetActive(m_StageUnlockCount >= 4);
            m_View.m_stars5.gameObject.SetActive(m_StageUnlockCount >= 5);

            RenderAPI.NextFrameCall(() =>
            {
                if (m_Scroll != null && m_StageUnlockCount > 0 && m_Scroll.activeButtons.Count >= m_StageUnlockCount)
                {
                    m_Scroll.SelectButtonByID_Click(m_StageUnlockCount - 1);
                }
            });
        }
    }
}
