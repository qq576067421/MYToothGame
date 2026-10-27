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
        public static int m_levelCount = 5;
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
            Func<WindowBase> onConfirm,
            Action onCancel)
        {
            var model = GetModel<tower_defend_lvchoose_model>();
            model.m_Request = request;
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
            InitStar();
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
            int starCount = LobbyPlayer.GetInstance().GetStageStar((tower_defend_chapter_wnd.m_chapterId-1) * m_levelCount + index);

            if (starCount > 0)
            {
                obj.Find($"Topic_1/stars{index}/star1/yellow_star").gameObject.SetActive(true);
            }
            if(starCount > 1)
            {
                obj.Find($"Topic_1/stars{index}/star2/yellow_star").gameObject.SetActive(true);
            }
            if(starCount > 2)
            {
                obj.Find($"Topic_1/stars{index}/star3/yellow_star").gameObject.SetActive(true);
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
            if (m_StageUnlockCount <= m_Scroll.selectedElementID)
            {
                ts.parent.DOKill();
                ts.parent.DOShakePosition(0.5f, 10, 10);
                AudioManager.GetInstance().Play2D(7);
                return;
            }

            int stageId = m_Scroll.selectedElementID + 1;
            model.m_Request.m_StageId = stageId;
            CGameProcedure.s_ProcLobby.TrySetSelectedStageId(model.m_Request.m_GameMode, stageId);

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
            InitUnlockTopic();
        }
        //初始化关卡解锁到多少（GetUnlockedStage 是全局进度，换算成本章内已解锁的关卡数）
        private void InitUnlockTopic()
        {
            int unlockedStage = LobbyPlayer.GetInstance().GetUnlockedStage();
            m_StageUnlockCount = Mathf.Clamp(unlockedStage - (tower_defend_chapter_wnd.m_chapterId - 1) * m_levelCount, 0, m_levelCount);
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
                if (m_Scroll != null && m_Scroll.activeButtons.Count >= m_StageUnlockCount)
                {
                    m_Scroll.SelectButtonByID_Click(m_StageUnlockCount - 1);
                }
            });
        }
    }
}
