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
        private int m_MonsterPoolId;           //当前解锁到的主题ID
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
        private void ListeningSelectedElementID()
        {
            int selectedElementID = m_Scroll.selectedElementID;
            switch (selectedElementID)
            {
                case 0:
                    m_View.m_txt_title.text = "冰淇淋";
                    break;
                case 1:
                    m_View.m_txt_title.text = "蛋糕";
                    break;
                case 2:
                    m_View.m_txt_title.text = "糖果";
                    break;
                case 3:
                    m_View.m_txt_title.text = "果冻";
                    break;
                case 4:
                    m_View.m_txt_title.text = "甜甜圈";
                    break;
                default:
                    m_View.m_txt_title.text = "选择关卡";
                    break;
            }
            m_View.m_btn_toggle_choose.gameObject.SetActive(selectedElementID == 0);
            m_View.m_btn_toggle_choose_new1.gameObject.SetActive(selectedElementID == 1);
            m_View.m_btn_toggle_choose_new2.gameObject.SetActive(selectedElementID == 2);
            m_View.m_btn_toggle_choose_new3.gameObject.SetActive(selectedElementID == 3);
            m_View.m_btn_toggle_choose_new4.gameObject.SetActive(selectedElementID == 4);
        }
        private void OnClickConfirm(Transform ts)
        {
            var model = GetModel<tower_defend_lvchoose_model>();
            if (m_MonsterPoolId <= m_Scroll.selectedElementID)
            {
                ts.parent.DOKill();
                ts.parent.DOShakePosition(0.5f, 10, 10);
                AudioManager.GetInstance().Play2D(7);
                return;
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
        //初始化主题解锁到多少
        private void InitUnlockTopic()
        {
            var model = GetModel<tower_defend_lvchoose_model>();
            var adapter = TowerDefendStageConfigResolver.Resolve(model.m_Request.m_StageId, model.m_Request.m_GameMode);
            m_MonsterPoolId = adapter.MonsterPoolId;
            m_View.m_lock.gameObject.SetActive(m_MonsterPoolId<1);
            m_View.m_lock_new1.gameObject.SetActive(m_MonsterPoolId<2);
            m_View.m_lock_new2.gameObject.SetActive(m_MonsterPoolId<3);
            m_View.m_lock_new3.gameObject.SetActive(m_MonsterPoolId<4);
            m_View.m_lock_new4.gameObject.SetActive(m_MonsterPoolId<5);

            RenderAPI.NextFrameCall(() =>
            {
                if (m_Scroll != null && m_Scroll.buttons.Count > m_MonsterPoolId)
                {
                    m_Scroll.SelectButtonByID_Click(m_MonsterPoolId - 1);
                }
            });
        }
    }
}
