using MonoBean;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
using GameDll;
using System;

namespace GameHot
{
    public class tower_defend_player_count_model : WindowModel
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

    public class tower_defend_player_count_wnd : WindowBase
    {
        private v_tower_defend_player_count_wnd m_View;
        private Animator playAnimator;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.lobby;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);
            __CreateModel(new tower_defend_player_count_model());
        }

        public void SetRequest(
            BattleStartupRequest request,
            Func<WindowBase> onConfirm,
            Action onCancel)
        {
            var model = GetModel<tower_defend_player_count_model>();
            model.m_Request = request;
            model.m_OnConfirm = onConfirm;
            model.m_OnCancel = onCancel;
            if (m_View != null)
            {
                SyncSelectionVisualWithRequest();
            }
        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_player_count_wnd();
            m_View.InitComponent(__GetWindowObj());
            playAnimator = m_View.m_btnPlayer.GetComponent<Animator>();
            RenderAPI.AddButtonClick(m_View.m_btnPlayer0, OnClickPlayer0);
            RenderAPI.AddButtonClick(m_View.m_btnPlayer1, OnClickPlayer1);
            RenderAPI.AddButtonClick(m_View.m_btnPlayer2, OnClickPlayer2);
            RenderAPI.AddButtonClick(m_View.m_btnPlayer3, OnClickPlayer3);

            OnInitLanguage();
        }
        protected override void OnOpen()
        {
            RenderEvent.Event.OnUpdateSelectionVisuals += OnUpdateSelectionVisuals;
            SyncSelectionVisualWithRequest();
        }
        protected override void OnClose()
        {
            RenderEvent.Event.OnUpdateSelectionVisuals -= OnUpdateSelectionVisuals;
        }

        public void SyncSelectionVisualWithRequest()
        {
            var model = GetModel<tower_defend_player_count_model>();
            int playerCount = model.m_Request != null
                ? Mathf.Clamp(model.m_Request.ReadPreparePlayerCount(), 1, 4)
                : 1;
            int col = playerCount - 1;
            var playerButtons = new ActiveButtons();
            playerButtons.buttons.Add(m_View.m_btnPlayer0);
            playerButtons.buttons.Add(m_View.m_btnPlayer1);
            playerButtons.buttons.Add(m_View.m_btnPlayer2);
            playerButtons.buttons.Add(m_View.m_btnPlayer3);
            __SetActiveButtons(new List<ActiveButtons> { playerButtons }, 0, col);

            if (UIManager.GetCurrentActiveWindow() == this)
            {
                RenderAPI.SetCurrentCol(col);
            }

            SetBtnPlayerChoose(col);
        }

        private void OnUpdateSelectionVisuals()
        {
            var active = UIManager.GetCurrentActiveWindow();
            if (active == null || active != this)
            {
                return;
            }
            AudioManager.GetInstance().Play2D(4);
            SetBtnPlayerChoose(RenderAPI.GetCurrentCol());
        }

        private void OnInitLanguage()
        {
            RenderAPI.SetTextLan(m_View.m_one_txt, "td_lobby_player_count_one");
            RenderAPI.SetTextLan(m_View.m_one_txt_new1, "td_lobby_player_count_one");
            RenderAPI.SetTextLan(m_View.m_two_txt, "td_lobby_player_count_two");
            RenderAPI.SetTextLan(m_View.m_two_txt_new1, "td_lobby_player_count_two");
            RenderAPI.SetTextLan(m_View.m_three_txt, "td_lobby_player_count_three");
            RenderAPI.SetTextLan(m_View.m_three_txt_new1, "td_lobby_player_count_three");
            RenderAPI.SetTextLan(m_View.m_four_txt, "td_lobby_player_count_four");
            RenderAPI.SetTextLan(m_View.m_four_txt_new1, "td_lobby_player_count_four");
        }
        private void SetBtnPlayerChoose(int index)
        {
            switch(index)
            {
                case 0:
                    playAnimator.Play("selectPersonnel0",-1,0);
                    break;
                case 1:
                    playAnimator.Play("selectPersonnel1" ,- 1, 0);
                    break;
                case 2:
                    playAnimator.Play("selectPersonnel2", - 1, 0);
                    break;
                case 3:
                    playAnimator.Play("selectPersonnel3", - 1, 0);
                    break;
                default:
                    playAnimator.Play("selectPersonnel0", - 1, 0);
                    break;
            }
        }
        private void OnClickPlayer3()
        {
            OnClickPlayerCount(4);
        }

        private void OnClickPlayer2()
        {
            OnClickPlayerCount(3);
        }

        private void OnClickPlayer1()
        {
            OnClickPlayerCount(2);
        }

        private void OnClickPlayer0()
        {
            OnClickPlayerCount(1);
        }

        private void OnClickPlayerCount(int playerCount)
        {
            var model = GetModel<tower_defend_player_count_model>();
            var request = model.m_Request;
            string error;
            if (!CGameProcedure.s_ProcLobby.TrySetPreparePlayerCount(request, playerCount, out error))
            {
                tip_wnd.GetInstance().OnShowTip(error);
                return;
            }

            // 请求是人数选择的唯一数据源；界面状态从实际生效的人数回读。
            SyncSelectionVisualWithRequest();

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
    }
}
