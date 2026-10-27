using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
using GameDll;
using MonoBean;

namespace GameHot
{
    public class tower_defend_giftedness_wnd : WindowBase
    {
        private v_tower_defend_giftedness_wnd m_View;
        private int m_ItemCount = 50;
        private int m_UnlockCount = 0;
        private List<int> m_UnlockBranchList = new List<int>();
        private List<v_tower_defend_giftedness_wnd.v_Item> m_ItemsView = new();
        private List<t_talentBean> t_TalentBeans = new();
        private int m_currentRow = 0;
        private int m_currentCol = 0;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.lobby;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);

        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_giftedness_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_ButtonLeft, OnClickLeft);
            RenderAPI.AddButtonClick(m_View.m_ButtonRight, OnClickRight);
            RenderAPI.AddButtonClick(m_View.m_ButtonUp, OnClickUp);
            RenderAPI.AddButtonClick(m_View.m_ButtonDown, OnClickDown);
            RenderAPI.AddButtonClick(m_View.m_ButtonClick, OnButtonClick);
            OnInitLanguage();
        }
        private void OnClickLeft()
        {
            if (IndexIsShowBranch(m_currentRow) == null) return;
            OnUpdateSelectionVisuals(m_currentRow, Mathf.Abs((m_currentCol - 1) % 2));
        }
        private void OnClickRight()
        {
            if (IndexIsShowBranch(m_currentRow) == null) return;
            OnUpdateSelectionVisuals(m_currentRow, Mathf.Abs((m_currentCol + 1) % 2));
        }
        private void OnClickUp()
        {
            if (m_currentCol != 0) return;
            OnUpdateSelectionVisuals(Mathf.Abs((m_currentRow + 1) % m_ItemCount),m_currentCol);
        }
        private void OnClickDown()
        {
            if (m_currentCol != 0) return;
            OnUpdateSelectionVisuals(Mathf.Abs((m_currentRow - 1 + m_ItemCount) % m_ItemCount), m_currentCol);
        }
        private void OnButtonClick()
        {
            var cfg = t_TalentBeans[m_currentRow];
            if (m_currentCol==0)
            {
                if(m_currentRow==m_UnlockCount)
                {
                    OnClick(m_currentRow, cfg.t_unlock_gold, false);
                }
            }
            else
            {
                if(!m_UnlockBranchList.Contains(m_currentRow) && m_currentRow<=m_UnlockCount)
                {
                    OnClick(m_currentRow, cfg.t_special_talent_gold, true);
                }
            }
        }
        private void OnClick(int index,int reduceCoin, bool isBranch = false)
        {
            if (isBranch)
            {
                if (m_UnlockBranchList.Contains(index)) return;
                if (!LobbyPlayer.GetInstance().TryUnlockGiftednessBranch(index, reduceCoin)) return;

                m_UnlockBranchList = LobbyPlayer.GetInstance().GetGiftednessUnlockBranches();
                InitItemShow(index);
            }
            else
            {
                if (index != m_UnlockCount) return;
                if (!LobbyPlayer.GetInstance().TryUnlockGiftednessMain(index, reduceCoin)) return;
                m_UnlockCount = LobbyPlayer.GetInstance().GetGiftednessUnlockCount();
                InitItemShow(index);
                if (m_ItemsView.Count > index + 1)
                {
                    InitItemShow(index + 1);
                }
                UpdateProgressBar();
            }
        }
        protected override void OnOpen()
        {
            RenderAPI.StartCoroutine(InitGiftednessItem());
            CGameProcedure.Event.OnMoneyChanged += OnMoneyChanged;
            CGameProcedure.Event.OnMoneyChanged();
        }

        protected override void OnClose()
        {
            CGameProcedure.Event.OnMoneyChanged -= OnMoneyChanged;
        }
        private void OnMoneyChanged()
        {
            var lobbyPlayer = LobbyPlayer.GetInstance();
            lobbyPlayer.EnsureLocalPlayerInfo();
            if (lobbyPlayer.m_PlayerInfo == null)
            {
                return;
            }
            m_View.m_txt_coin.text = lobbyPlayer.m_PlayerInfo.GetMoney(MoneyId.CoinId).ToString();
        }
        private void OnInitLanguage()
        {

        }
        private IEnumerator InitGiftednessItem()
        {
            m_UnlockCount = Mathf.Max(0, LobbyPlayer.GetInstance().GetGiftednessUnlockCount());
            m_UnlockBranchList = LobbyPlayer.GetInstance().GetGiftednessUnlockBranches();
            m_ItemCount = t_talentBean.GetKeys().Count;
            for (int i=0;i< t_talentBean.GetKeys().Count; i++)
            {
                // 窗口可能在协程运行期间被关闭并销毁 m_item,这里必须防御。
                if (m_View == null || m_View.m_item == null)
                {
                    yield break;
                }
                var cfg = t_talentBean.GetConfig(i+1, false);
                if(cfg == null)
                {
                    break;
                }
                t_TalentBeans.Add(cfg);
                var item = GameObject.Instantiate(m_View.m_item, m_View.m_item.transform.parent);
                item.gameObject.SetActive(true);
                InitItemView(item.gameObject);
                InitItemShow(i);
                item.transform.SetAsFirstSibling();
            }
            yield return null;
            //InitEndMoveToUnlock();
            UpdateProgressBar();
            m_View.m_Content.transform.localPosition = new Vector2(0, m_View.m_Content.GetComponent<RectTransform>().sizeDelta.y - Screen.height / 2f);
            OnUpdateSelectionVisuals(m_UnlockCount,0);
        }
        private void IsShowChoose(LUIButton button,bool isShow)
        {
            button.transform.Find("choose").gameObject.SetActive(isShow);
        }
        //private void InitEndMoveToUnlock()
        //{
        //    RenderAPI.ResetMenu(GetUIWindow().m_ActiveButtons,Mathf.Min(m_UnlockCount,m_ItemCount-1), 0);
        //}
        private void UpdateProgressBar()
        {
            var verticalLayoutGroup = m_View.m_Content.GetComponent<VerticalLayoutGroup>();
            var itemWidth = m_View.m_item.GetComponent<RectTransform>().sizeDelta.x;
            var diRect = m_View.m_Progress_di.GetComponent<RectTransform>();
            var fillRect = m_View.m_Progress_Fill.GetComponent<RectTransform>();

            var ContentWidth = verticalLayoutGroup.spacing * (m_ItemCount-1) + itemWidth * (m_ItemCount-1);
            var unlockWidth = verticalLayoutGroup.spacing * (m_UnlockCount-1) + itemWidth * (m_UnlockCount-1);

            m_View.m_Content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, ContentWidth+ itemWidth+ verticalLayoutGroup.spacing+150);
            diRect.sizeDelta = new Vector2(ContentWidth, diRect.sizeDelta.y);
            fillRect.sizeDelta = new Vector2(unlockWidth, fillRect.sizeDelta.y);
            m_View.m_Progress.transform.SetAsFirstSibling();
        }
        private void InitItemView(GameObject obj)
        {
            var view = new v_tower_defend_giftedness_wnd.v_Item();
            view.InitComponent(obj);
            m_ItemsView.Add(view);
        }
        private void InitItemShow(int index)
        {
            var cfg = t_TalentBeans[index];
            var view = m_ItemsView[index];
            int numericalType = 0;
            int numericalCount = 0;
            if(cfg.t_atk != 0)
            {
                numericalType = 0;
                numericalCount = cfg.t_atk;
            }
            else if(cfg.t_crit != 0)
            {
                numericalType = 1;
                numericalCount = cfg.t_crit;
            }
            else if (cfg.t_crit_damage != 0)
            {
                numericalType = 2;
                numericalCount = cfg.t_crit_damage;
            }
            else if (cfg.t_amp != 0)
            {
                numericalType = 3;
                numericalCount = cfg.t_amp;
            }

            SetBranch(index);

            switch (numericalType)
            {
                case 0:
                    RenderAPI.SetTextLan(view.m_jieshao_TxtMesh, "td_giftedness_atk");
                    RenderAPI.SetText(view.m_TxtMesh, $"+{numericalCount}");
                    break;
                case 1:
                    RenderAPI.SetTextLan(view.m_jieshao_TxtMesh, "td_giftedness_crit");
                    RenderAPI.SetText(view.m_TxtMesh, $"+{numericalCount}%");
                    break;
                case 2:
                    RenderAPI.SetTextLan(view.m_jieshao_TxtMesh, "td_giftedness_cdmg");
                    RenderAPI.SetText(view.m_TxtMesh, $"+{numericalCount}%");
                    break;
                case 3:
                    RenderAPI.SetTextLan(view.m_jieshao_TxtMesh, "td_giftedness_dmg");
                    RenderAPI.SetText(view.m_TxtMesh, $"+{numericalCount}%");
                    break;
            }

            if (index < m_UnlockCount)      //解锁item处理
            {
                view.m_choose.color = new Color(1, 1, 1);
                view.m_Unlock.gameObject.SetActive(true);
                view.m_jieshao_TxtMesh.gameObject.SetActive(true);
                view.m_lockCoin_new1.gameObject.SetActive(false);
                switch (numericalType)
                {
                    case 0:
                        view.m_image1.gameObject.SetActive(true);
                        view.m_image1_new1.gameObject.SetActive(true);
                        break;
                    case 1:
                        view.m_image2.gameObject.SetActive(true);
                        view.m_image2_new1.gameObject.SetActive(true);
                        break;
                    case 2:
                        view.m_image3.gameObject.SetActive(true);
                        view.m_image3_new1.gameObject.SetActive(true);
                        break;
                    case 3:
                        view.m_image3.gameObject.SetActive(true);
                        view.m_image3_new1.gameObject.SetActive(true);
                        break;
                }
            }
            else if (index == m_UnlockCount)        //待解锁item处理
            {
                view.m_choose.color = new Color(1,1,1);
                view.m_lockImage.gameObject.SetActive(false);
                RenderAPI.SetTextNumber(view.m_lockCoin_TxtMesh, cfg.t_unlock_gold);
                view.m_Unlock.gameObject.SetActive(true);
                view.m_jieshao_TxtMesh.gameObject.SetActive(true);
                view.m_lockCoin_new1.gameObject.SetActive(true);
            }
            else
            {
                view.m_choose.color = new Color(0.5f, 0.5f, 0.5f);
                RenderAPI.SetTextNumber(view.m_lockCoin_TxtMesh, cfg.t_unlock_gold);
                view.m_jieshao_TxtMesh.gameObject.SetActive(true);
                view.m_lockCoin_new1.gameObject.SetActive(true);
            }
        }
        private void GetGiftednessTypeTxt(LUITextMesh txt, int type,int value)
        {
            switch(type)
            {
                case 1:
                    RenderAPI.SetTextLan(txt, "td_giftedness_base", value);
                    break;
                case 2:
                    RenderAPI.SetTextLan(txt, "td_giftedness_charge", value);
                    break;
                case 3:
                    RenderAPI.SetTextLan(txt, "td_giftedness_cd", value);
                    break;
                case 4:
                    RenderAPI.SetTextLan(txt, "td_giftedness_state", value);
                    break;
                case 5:
                    RenderAPI.SetTextLan(txt, "td_giftedness_endless", value);
                    break;
                default:
                    Debug.LogError("没有这个类型");
                    break;
            }
        }
        //传进索引，看这个索引是否需要显示分支
        private t_talentBean IndexIsShowBranch(int index)
        {
            var cfg = t_TalentBeans[index];
            if (cfg.t_special_talent_type != 0)
            {
                return cfg;
            }
            return null;
        }
        private void SetBranch(int index)       //设置分支天赋
        {
            var view = m_ItemsView[index];
            var cfg = IndexIsShowBranch(index);
            if (cfg != null)    //需要显示分支天赋
            {
                view.m_Branch.gameObject.SetActive(true);
                if (index < m_UnlockCount)
                {
                    view.m_lockImage_new1.gameObject.SetActive(false);
                    view.m_lockCoin.gameObject.SetActive(true);
                }
                if(index / (5+1) % 2==0)          //设置上下翻转
                {
                    view.m_Branch.transform.localEulerAngles = new Vector3(0, 0, 0);
                    view.m_branch_btn.transform.localEulerAngles = new Vector3(0, 0,0);
                    view.m_Progress_Fill.transform.localEulerAngles = new Vector3(0, 0, 0);
                }
                else
                {
                    view.m_Branch.transform.localEulerAngles = new Vector3(0, 0, 180);
                    view.m_branch_btn.transform.localEulerAngles = new Vector3(0, 0,180);
                    view.m_Progress_Fill.transform.localEulerAngles = new Vector3(0, 0, 180);
                }
                if (m_UnlockBranchList.Contains(index))
                {
                    view.m_unlock.gameObject.SetActive(true);
                    view.m_lockCoin.gameObject.SetActive(false);
                }
                RenderAPI.SetTextNumber(view.m_lockCoin_TxtMesh_new1, cfg.t_special_talent_gold);
                GetGiftednessTypeTxt(view.m_branchUnlock_Txt,cfg.t_special_talent_type, cfg.t_special_talent_value);
                GetGiftednessTypeTxt(view.m_branchChoose_Txt,cfg.t_special_talent_type, cfg.t_special_talent_value);
            }
        }
        private void OnUpdateSelectionVisuals(int row,int col)
        {
            var active = UIManager.GetCurrentActiveWindow();
            if (active == null || active != this)
            {
                return;
            }
            AudioManager.GetInstance().Play2D(4);
            //AddBranchSelected();
            SetButtonChoose(row,col);
            MoveContent();
        }
        private void SetButtonChoose(int row, int col)
        {
            IsShowChoose(m_ItemsView[m_currentRow].m_Bridge.GetComponent<LUIButton>(), false);
            IsShowChoose(m_ItemsView[m_currentRow].m_branch_btn, false);
            if (col == 0)
            {
                IsShowChoose(m_ItemsView[row].m_Bridge.GetComponent<LUIButton>(), true);
            }
            else
            {
                IsShowChoose(m_ItemsView[row].m_branch_btn, true);
            }
            m_currentRow = row;
            m_currentCol = col;
        }
        //private void AddBranchSelected()
        //{
        //    var cfg = IndexIsShowBranch(m_currentRow);
        //    if (cfg != null)       //当前item显示了天赋分支
        //    {
        //        if (m_currentCol == 0 && m_currentRow != m_UnlockCount)
        //        {
        //            RenderAPI.AddButtonClick(m_ItemsView[m_currentRow].m_branch_btn, () => { OnClick(m_currentRow, cfg.t_special_talent_gold, true); });
        //        }
        //    }
        //}
        private void MoveContent()
        {
            var view = m_ItemsView[m_currentRow];
            var dis = view.m_Bridge.transform.position.y - m_View.m_Bridge.transform.position.y;
            var limitHeight = Screen.height / 3f;

            dis = Mathf.Abs(dis) > limitHeight ? dis - Mathf.Sign(dis) * limitHeight : 0;
            m_View.m_Content.transform.DOKill();
            m_View.m_Content.transform.DOMoveY(-dis, 0.3f).SetRelative();
        }
    }
}
