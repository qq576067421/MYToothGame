using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
using GameDll;

namespace GameHot
{
    public class tower_defend_giftedness_wnd : WindowBase
    {
        private v_tower_defend_giftedness_wnd m_View;
        private int m_ItemCount = 50;
        private int m_UnlockCount = 0;
        private List<int> m_UnlockBranchList = new List<int>();
        private List<v_tower_defend_giftedness_wnd.v_Item> m_ItemsView = new();
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
        }

        private void OnClick(int index,int reduceCoin, bool isBranch = false)
        {
            if (isBranch)
            {
                if (m_UnlockBranchList.Contains(index)) return;
                if (!LobbyPlayer.GetInstance().TryUnlockGiftednessBranch(index, reduceCoin)) return;

                m_UnlockBranchList = LobbyPlayer.GetInstance().GetGiftednessUnlockBranches();
                InitItemShow(index, m_ItemsView[index].m_Bridge.GetComponent<LUIButton>());
            }
            else
            {
                if (index != m_UnlockCount) return;
                if (!LobbyPlayer.GetInstance().TryUnlockGiftednessMain(index, reduceCoin)) return;
                m_UnlockCount = LobbyPlayer.GetInstance().GetGiftednessUnlockCount();
                if (IndexIsShowBranch(index))
                {
                    if (GetUIWindow().m_ActiveButtons[index].buttons.Count < 2)
                    {
                        GetUIWindow().m_ActiveButtons[index].buttons.Add(m_ItemsView[index].m_branch_btn);
                    }
                    else
                    {
                        GetUIWindow().m_ActiveButtons[index].buttons[1] = m_ItemsView[index].m_branch_btn;
                    }
                    RenderAPI.AddButtonClick(m_ItemsView[index].m_branch_btn, () => { OnClick(index, 100, true); });
                }
                InitItemShow(index, m_ItemsView[index].m_Bridge.GetComponent<LUIButton>());
                if (m_ItemsView.Count > index + 1)
                {
                    InitItemShow(index + 1, m_ItemsView[index + 1].m_Bridge.GetComponent<LUIButton>());
                }
                UpdateProgressBar();
            }
        }
        protected override void OnOpen()
        {
            RenderAPI.StartCoroutine(InitGiftednessItem());
            RenderEvent.Event.OnUpdateSelectionVisuals += OnUpdateSelectionVisuals;
            CGameProcedure.Event.OnMoneyChanged += OnMoneyChanged;
            CGameProcedure.Event.OnMoneyChanged();
        }

        protected override void OnClose()
        {
            RenderEvent.Event.OnUpdateSelectionVisuals -= OnUpdateSelectionVisuals;
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
        private IEnumerator InitGiftednessItem()
        {
            m_UnlockCount = Mathf.Max(0, LobbyPlayer.GetInstance().GetGiftednessUnlockCount());
            m_UnlockBranchList = LobbyPlayer.GetInstance().GetGiftednessUnlockBranches();
            GetUIWindow().m_ActiveButtons = new List<ActiveButtons>();
            UpdateProgressBar();
            for (int i=0;i< m_ItemCount;i++)
            {
                // 窗口可能在协程运行期间被关闭并销毁 m_item,这里必须防御。
                if (m_View == null || m_View.m_item == null)
                {
                    yield break;
                }
                var item = GameObject.Instantiate(m_View.m_item, m_View.m_item.transform.parent);
                item.gameObject.SetActive(true);
                InitItemView(item.gameObject);
                InitItemShow(i,item);
                if(i%10==0)
                {
                    yield return null;
                }
            }
            yield return null;
            InitEndMoveToUnlock();
        }
        private void InitEndMoveToUnlock()
        {
            RenderAPI.ResetMenu(GetUIWindow().m_ActiveButtons,Mathf.Min(m_UnlockCount,m_ItemCount-1), 0);
        }
        private void UpdateProgressBar()
        {
            var verticalLayoutGroup = m_View.m_Content.GetComponent<VerticalLayoutGroup>();
            var itemWidth = m_View.m_item.GetComponent<RectTransform>().sizeDelta.x;
            var diRect = m_View.m_Progress_di.GetComponent<RectTransform>();
            var fillRect = m_View.m_Progress_Fill.GetComponent<RectTransform>();

            var ContentWidth = verticalLayoutGroup.spacing * m_ItemCount + itemWidth * m_ItemCount;
            var unlockWidth = verticalLayoutGroup.spacing * m_UnlockCount + itemWidth * m_UnlockCount;

            m_View.m_Content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, ContentWidth);
            diRect.sizeDelta = new Vector2(ContentWidth, diRect.sizeDelta.y);
            fillRect.sizeDelta = new Vector2(unlockWidth, fillRect.sizeDelta.y);
        }
        private void InitItemView(GameObject obj)
        {
            var view = new v_tower_defend_giftedness_wnd.v_Item();
            view.InitComponent(obj);
            m_ItemsView.Add(view);
        }
        private void InitItemShow(int index,LUIButton item)
        {
            var view = m_ItemsView[index];
            int numericalType = index % 3;
            SetBranch(index);
            if (index < m_UnlockCount)      //解锁item处理
            {
                if(GetUIWindow().m_ActiveButtons.Count <= index)
                {
                    GetUIWindow().m_ActiveButtons.Add(new ActiveButtons { buttons = new List<Button> { item } });
                }
                view.m_Unlock.gameObject.SetActive(true);
                view.m_jieshao_TxtMesh.gameObject.SetActive(true);
                RenderAPI.SetText(view.m_TxtMesh,"+" + (index/3+1));
                view.m_lockCoin_new1.gameObject.SetActive(false);
                switch (numericalType)
                {
                    case 0:
                        RenderAPI.SetText(view.m_jieshao_TxtMesh, "攻击");
                        view.m_image1.gameObject.SetActive(true);
                        view.m_image1_new1.gameObject.SetActive(true);
                        break;
                    case 1:
                        RenderAPI.SetText(view.m_jieshao_TxtMesh, "暴击");
                        view.m_image2.gameObject.SetActive(true);
                        view.m_image2_new1.gameObject.SetActive(true);
                        break;
                    case 2:
                        RenderAPI.SetText(view.m_jieshao_TxtMesh, "攻速");
                        view.m_image3.gameObject.SetActive(true);
                        view.m_image3_new1.gameObject.SetActive(true);
                        break;
                }
            }
            else if (index == m_UnlockCount)        //待解锁item处理
            {
                GetUIWindow().m_ActiveButtons.Add(new ActiveButtons { buttons = new List<Button> { item } });
                RenderAPI.AddButtonClick(item, () => 
                {
                    OnClick(index,100);
                });
                view.m_lockImage.gameObject.SetActive(false);
                view.m_lockCoin_new1.gameObject.SetActive(true);
            }
            RenderAPI.ResetMenu(GetUIWindow().m_ActiveButtons, m_currentRow, m_currentCol);
        }
        //传进索引，看这个索引是否需要显示分支
        private bool IndexIsShowBranch(int index)
        {
            int numericalType = index % 3;
            int N = (index / 3 + 1);    //第几批天赋
            int M = 3 - ((N - 1) % 3);  //分支天赋显示在这一批第几个
            if(M == (numericalType + 1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void SetBranch(int index)       //设置分支天赋
        {
            var view = m_ItemsView[index];
            int N = (index / 3 + 1);    //第几批天赋
            if (IndexIsShowBranch(index))    //需要显示分支天赋
            {
                view.m_Branch.gameObject.SetActive(true);
                if (index < m_UnlockCount)
                {
                    view.m_lockImage_new1.gameObject.SetActive(false);
                    view.m_lockCoin.gameObject.SetActive(true);
                }
                if(N%2==0)          //设置上下翻转
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
            }
        }
        private void OnUpdateSelectionVisuals()
        {
            var active = UIManager.GetCurrentActiveWindow();
            if (active == null || active != this)
            {
                return;
            }
            AudioManager.GetInstance().Play2D(4);
            AddBranchSelected();
            MoveContent();
        }
        private void AddBranchSelected()
        {
            int currentCol = RenderAPI.GetCurrentCol();
            int currentRow = RenderAPI.GetCurrentRow();
            // RenderAPI 的行=item索引,列=item内的按钮位置(0=主,1=分支),始终同步到本窗口
            m_currentRow = currentRow;
            m_currentCol = currentCol;
            if (IndexIsShowBranch(m_currentRow))       //当前item显示了天赋分支
            {
                if (m_currentCol == 0 && m_currentRow != m_UnlockCount)
                {
                    if (GetUIWindow().m_ActiveButtons[m_currentRow].buttons.Count < 2)
                    {
                        GetUIWindow().m_ActiveButtons[m_currentRow].buttons.Add(m_ItemsView[m_currentRow].m_branch_btn);
                    }
                    else
                    {
                        GetUIWindow().m_ActiveButtons[m_currentRow].buttons[1] = m_ItemsView[m_currentRow].m_branch_btn;
                    }
                    RenderAPI.AddButtonClick(m_ItemsView[m_currentRow].m_branch_btn, () => { OnClick(m_currentRow,100,true); });
                }
                else if (m_currentCol == 1)
                {
                    RenderEvent.Event.OnUpdateSelectionVisuals -= OnUpdateSelectionVisuals;
                    RenderAPI.ResetMenu(GetUIWindow().m_ActiveButtons, m_currentRow, m_currentCol);
                    RenderEvent.Event.OnUpdateSelectionVisuals += OnUpdateSelectionVisuals;
                }
            }
            else
            {
                if (GetUIWindow().m_ActiveButtons[m_currentRow].buttons.Count > 1)
                {
                    GetUIWindow().m_ActiveButtons[m_currentRow].buttons.RemoveAt(1);
                }
            }
        }
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
