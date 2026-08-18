using LCL;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace UnityUI
{
    public enum ItemCornerEnum
    {
        LeftBottom = 0,
        LeftTop,
        RightTop,
        RightBottom,
    }


    public enum ListItemArrangeType
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft,
    }

    public class ItemPool
    {
        GameObject mPrefabObj;
        string mPrefabName;
        int mInitCreateCount = 1;
        float mPadding = 0;
        float mStartPosOffset = 0;
        List<LoopListViewItem2> mTmpPooledItemList = new List<LoopListViewItem2>();
        List<LoopListViewItem2> mPooledItemList = new List<LoopListViewItem2>();
        static int mCurItemIdCount = 0;
        RectTransform mItemParent = null;
        public ItemPool()
        {
           
        }
        public void Init(GameObject prefabObj, float padding,float startPosOffset, int createCount, RectTransform parent)
        {
            mPrefabObj = prefabObj;
            mPrefabName = mPrefabObj.name;
            mInitCreateCount = createCount;
            mPadding = padding;
            mStartPosOffset = startPosOffset;
            mItemParent = parent;
            mPrefabObj.SetActive(false);
            for (int i = 0; i < mInitCreateCount; ++i)
            {
                LoopListViewItem2 tViewItem = CreateItem();
                RecycleItemReal(tViewItem);
            }
        }
        public LoopListViewItem2 GetItem()
        {
            mCurItemIdCount++;
            LoopListViewItem2 tItem = null;
            if(mTmpPooledItemList.Count > 0)
            {
                int count = mTmpPooledItemList.Count;
                tItem = mTmpPooledItemList[0];//为了确保回收和使用顺序一致，防止界面闪动
                mTmpPooledItemList.RemoveAt(0);
                tItem.gameObject.SetActive(true);
            }
            else
            {
                int count = mPooledItemList.Count;
                if (count == 0)
                {
                    tItem = CreateItem();
                }
                else
                {
                    tItem = mPooledItemList[0];//为了确保回收和使用顺序一致，防止界面闪动
                    mPooledItemList.RemoveAt(0);
                    tItem.gameObject.SetActive(true);
                }
            }
            tItem.Padding = mPadding;
            tItem.ItemId = mCurItemIdCount;
            return tItem;

        }

        public void DestroyAllItem()
        {
            ClearTmpRecycledItem();
            int count = mPooledItemList.Count;
            for (int i = 0;i<count;++i)
            {
                GameObject.Destroy(mPooledItemList[i].gameObject);
            }
            mPooledItemList.Clear();
        }
        public LoopListViewItem2 CreateItem()
        {

            GameObject go = GameObject.Instantiate<GameObject>(mPrefabObj, Vector3.zero,Quaternion.identity, mItemParent);
            go.SetActive(true);
            RectTransform rf = go.GetComponent<RectTransform>();
            rf.localScale = Vector3.one;
            rf.anchoredPosition3D = Vector3.zero;
            rf.localEulerAngles = Vector3.zero;
            LoopListViewItem2 tViewItem = go.GetComponent<LoopListViewItem2>();
            tViewItem.ItemPrefabName = mPrefabName;
            tViewItem.StartPosOffset = mStartPosOffset;
            return tViewItem;
        }
        void RecycleItemReal(LoopListViewItem2 item)
        {
            var cg = item.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0;
                if (cg.interactable)
                {
                    cg.interactable = false;
                }
                if (cg.blocksRaycasts)
                {
                    cg.blocksRaycasts = false;
                }
            }
            else
            {
                if (item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(false);
                }
            }
            mPooledItemList.Add(item);
        }
        public void RecycleItem(LoopListViewItem2 item)
        {
            //var tween = item.GetComponent<TweenCanvasAlpha>();
            //if (tween != null)
            //{
            //    tween.enabled = false;
            //}

            var cg = item.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0;
                if (cg.interactable)
                {
                    cg.interactable = false;
                }
                if(cg.blocksRaycasts)
                {
                    cg.blocksRaycasts = false;
                }
            }
            else
            {
                if (item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(false);
                }
            }



            mTmpPooledItemList.Add(item);
        }
        public void ClearTmpRecycledItem()
        {
            int count = mTmpPooledItemList.Count;
            if(count == 0)
            {
                return;
            }
            for(int i = 0;i<count;++i)
            {
                RecycleItemReal(mTmpPooledItemList[i]);
            }
            mTmpPooledItemList.Clear();
        }
    }
    [System.Serializable]
    public class ItemPrefabConfData
    {
        public GameObject mItemPrefab = null;
        public float mPadding = 0;
        public int mInitCreateCount = 0;
        public float mStartPosOffset = 0;
    }


    public class LoopListViewInitParam
    {
        // all the default values
        public float mDistanceForRecycle0 = 300; //mDistanceForRecycle0 should be larger than mDistanceForNew0
        public float mDistanceForNew0 = 200;
        public float mDistanceForRecycle1 = 300;//mDistanceForRecycle1 should be larger than mDistanceForNew1
        public float mDistanceForNew1 = 200;
        public float mSmoothDumpRate = 0.3f;
        public float mSnapFinishThreshold = 0.01f;
        public float mSnapVecThreshold = 145;
        public float mItemDefaultWithPaddingSize = 20;//item's default size (with padding)

        public static LoopListViewInitParam CopyDefaultInitParam()
        {
            return new LoopListViewInitParam();
        }
    }

    public enum SnapStatus
    {
        NoTargetSet = 0,
        TargetHasSet = 1,
        SnapMoving = 2,
        SnapMoveFinish = 3
    }


    /// <summary>
    /// 标准用法:
    /// 1、当已知具体的逻辑数据列表的时候采用如下用法
    /// 下面的例子不要把logic_items在闭包里面使用，
    /// 因为多次调用ShowEquipAttr函数的时候，闭包函数内的logic_items是之前的内容，而不是改变后的
    /// public static void ShowEquipAttr(LoopListView2 listView, List<UIEquipCell> equips)
    //{
    //    var logic_items = MakeEquipAttrs(equips);
    //    int logic_item_count = 0;
    //    if(logic_items != null)
    //    {
    //        logic_item_count = logic_items.Count;
    //    }

    //    listView.SetUserData(logic_items);
    //    if (listView.IsListViewInited())
    //    {
    //        listView.SetListItemCount(logic_item_count);
    //        listView.RefreshAllShownItem();
    //    }
    //    else
    //    {
    //        listView.InitListView(logic_item_count, (l, show_index) =>
    //        {
    //            var datas = (List<string>)l.GetUserData();
    //            var row = listView.NewListViewItem("attr_line", show_index);
    //            if (row != null)
    //            {
    //                listView.OnShowRowCells(row, show_index,
    //                (cell_go, logic_index) =>
    //                {
    //                    var v_item = new v_attr_item();
    //                    v_item.InitComponent(cell_go);
    //                    return v_item;
    //                },
    //                (int logic_index) =>
    //                {
    //                    var v_item = listView.GetCellView(logic_index) as v_attr_item;
    //                    if (datas.Count > logic_index)
    //                    {
    //                        RenderAPI.SetActive(v_item.m_Bridge, true);
    //                        var text = datas[logic_index];
    //                        RenderAPI.SetText(v_item.m_txtAttrValue, text);
    //                    }
    //                    else
    //                    {
    //                        RenderAPI.SetActive(v_item.m_Bridge, false);
    //                    }
    //                });
    //            }
    //            return row;
    //        });
    //    }
    //  }
    /// 
    /// 2、当存在空格子或者只是知道格子数量，每个格子可能需要动态获取数据的时候采用如下案例
    /// 
    //var max_size = LobbyPlayer.BagMgr.GetMaxSize();
    //m_View.m_loop_list.InitListView(max_size, OnShowBagItemByIndex, null, OnHideBagItem);
    ///private LoopListViewItem2 OnShowBagItemByIndex(LoopListView2 listView, int showIndex)
    //{
    //      var row = listView.NewListViewItem("equip_line", showIndex);
    //      if (row != null)
    //      {

    //          listView.OnShowRowCells(row, showIndex,
    //          (cell_go, logic_index) =>
    //          {
    //              var v_item = new v_bag_item();
    //              v_item.InitComponent(cell_go);
    //                      int slot = logic_index;
    //              var bagItem = LobbyPlayer.BagMgr.GetItem(slot);
    //              v_item.m_UserData = bagItem;
    //              return v_item;
    //          }, 
    //          (int logic_index) =>
    //          {
    //              int slot = logic_index;
    //              var bagItem = LobbyPlayer.BagMgr.GetItem(slot);
    //              var v_item = listView.GetCellView(logic_index) as v_bag_item;
    //              SetBagCell(v_item, bagItem);
    //          });
    //      }
    //      return row;
    //}
    /// 
    /// </summary>
    public class LoopListView2 : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, ICoroutineHandler
    {
        public enum LineStartStyle
        {
            //从这个开始的
            Start,
            //仅仅只有这个
            Just
        }
        class SnapData
        {
            public SnapStatus mSnapStatus = SnapStatus.NoTargetSet;
            public int mSnapTargetIndex = 0;
            public float mTargetSnapVal = 0;
            public float mCurSnapVal = 0;
            public bool mIsForceSnapTo = false;
            public void Clear()
            {
                mSnapStatus = SnapStatus.NoTargetSet;
                mIsForceSnapTo = false;
            }
        }

        Dictionary<string, ItemPool> mItemPoolDict = new Dictionary<string, ItemPool>();
        List<ItemPool> mItemPoolList = new List<ItemPool>();
        [SerializeField]
        List<ItemPrefabConfData> mItemPrefabDataList = new List<ItemPrefabConfData>();

        [SerializeField]
        private ListItemArrangeType mArrangeType = ListItemArrangeType.TopToBottom;
        public ListItemArrangeType ArrangeType { get { return mArrangeType; } set { mArrangeType = value; } }

        List<LoopListViewItem2> mItemList = new List<LoopListViewItem2>();
        RectTransform mContainerTrans;
        ScrollRect mScrollRect = null;
        RectTransform mScrollRectTransform = null;
        RectTransform mViewPortRectTransform = null;
        float mItemDefaultWithPaddingSize = 20;
        int mShowItemTotalCount = 0;
        bool mIsVertList = false;
        System.Func<LoopListView2, int, LoopListViewItem2> mOnShowItemCall;
        System.Action<LoopListViewItem2> mOnHideItemCall;
        //这个用在当已知数据没有增加、减少、变换顺序，只是里面的内容发生改变
        System.Action<LoopListViewItem2, int, LineStartStyle> mOnUpdateItemCall;
        Vector3[] mItemWorldCorners = new Vector3[4];
        Vector3[] mViewPortRectLocalCorners = new Vector3[4];
        int mCurReadyMinItemIndex = 0;
        int mCurReadyMaxItemIndex = 0;
        bool mNeedCheckNextMinItem = true;
        bool mNeedCheckNextMaxItem = true;
        ItemPosMgr mItemPosMgr = null;
        float mDistanceForRecycle0 = 300;
        float mDistanceForNew0 = 200;
        float mDistanceForRecycle1 = 300;
        float mDistanceForNew1 = 200;
        [SerializeField]
        bool mSupportScrollBar = true;
        bool mIsDraging = false;
        PointerEventData mPointerEventData = null;
        public System.Action mOnBeginDragAction = null;
        public System.Action mOnDragingAction = null;
        public System.Action mOnEndDragAction = null;
        int mLastItemIndex = 0;
        float mLastItemPadding = 0;
        float mSmoothDumpVel = 0;
        float mSmoothDumpRate = 0.3f;
        float mSnapFinishThreshold = 0.1f;
        float mSnapVecThreshold = 145;
        [SerializeField]
        bool mItemSnapEnable = false;
 

        Vector3 mLastFrameContainerPos = Vector3.zero;
        public System.Action<LoopListView2,LoopListViewItem2> mOnSnapItemFinished = null;
        public System.Action<LoopListView2, LoopListViewItem2> mOnSnapNearestChanged = null;
        int mCurSnapNearestItemIndex = -1;
        Vector2 mAdjustedVec;
        bool mNeedAdjustVec = false;
        int mLeftSnapUpdateExtraCount = 1;
        [SerializeField]
        Vector2 mViewPortSnapPivot = Vector2.zero;
        [SerializeField]
        Vector2 mItemSnapPivot = Vector2.zero;
        //每一行或者列有多少元素
        [Range(1,100)]
        [SerializeField]
        public int m_PerCount = 1;
        [SerializeField]
        private int m_SplitTime = 50;
        private WaitForSeconds m_SplitTimeWaitForSeconds;
        //private bool m_EnableSplitTime = true;
        public bool m_EnableAlphaEffect = true;

        private int m_ManulUpdateViewRefCount = 0;

        private int m_LogicDataTotalCount = 0;

        private enum NewItemStyle
        {
            InsertHead,
            Update,
            AddTail
        }
        private NewItemStyle m_NewItemStyle = NewItemStyle.AddTail;
        private int m_NewItemUpdateIndex = -1;

        ClickEventListener mScrollBarClickEventListener = null;
        SnapData mCurSnapData = new SnapData();
        Vector3 mLastSnapCheckPos = Vector3.zero;
        bool mListViewInited = false;
        int mListUpdateCheckFrameCount = 0;
        //public void SetEnableSplitTime(bool enable)
        //{
        //    m_EnableSplitTime = enable;
        //}
        //public bool IsEnableSplitTime()
        //{
        //    return m_EnableSplitTime;
        //}
        public void SetSplitTime(int time)
        {
            m_SplitTime = time;
            if(m_SplitTime > 0)
            {
                m_SplitTimeWaitForSeconds = new WaitForSeconds(m_SplitTime / 1000.0f);
            }
        }
        public int GetSplitTime()
        {
            return m_SplitTime;
        }

        public bool IsVertList
        {
            get
            {
                return mIsVertList;
            }
        }
        public int ShowItemTotalCount
        {
            get
            {
                return mShowItemTotalCount;
            }
        }

        public RectTransform ContainerTrans
        {
            get
            {
                return mContainerTrans;
            }
        }

        public ScrollRect ScrollRect
        {
            get
            {
                return mScrollRect;
            }
        }

        public bool IsDraging
        {
            get
            {
                return mIsDraging;
            }
        }

        public bool ItemSnapEnable
        {
            get {return mItemSnapEnable;}
            set { mItemSnapEnable = value; }
        }

        public bool SupportScrollBar
        {
            get { return mSupportScrollBar; }
            set { mSupportScrollBar = value; }
        }
        //嵌套列表，用于传递拖动
        public LoopListView2 ParentListView;

        public ItemPrefabConfData GetItemPrefabConfData(string prefabName)
        {
            foreach (ItemPrefabConfData data in mItemPrefabDataList)
            {
                if (data.mItemPrefab == null)
                {
                    Debug.LogError("A item prefab is null ");
                    continue;
                }
                if (prefabName == data.mItemPrefab.name)
                {
                    return data;
                }

            }
            return null;
        }

        public void OnItemPrefabChanged(string prefabName, bool AlphaEffect)
        {
            ItemPrefabConfData data = GetItemPrefabConfData(prefabName);
            if(data == null)
            {
                return;
            }
            ItemPool pool = null;
            if (mItemPoolDict.TryGetValue(prefabName, out pool) == false)
            {
                return;
            }
            int firstItemIndex = -1;
            Vector3 pos = Vector3.zero;
            if(mItemList.Count > 0)
            {
                firstItemIndex = mItemList[0].ItemRenderIndex;
                pos = mItemList[0].CachedRectTransform.anchoredPosition3D;
            }
            RecycleAllItem();
            ClearAllTmpRecycledItem();
            pool.DestroyAllItem();
            pool.Init(data.mItemPrefab, data.mPadding, data.mStartPosOffset,data.mInitCreateCount, mContainerTrans);
            if(firstItemIndex >= 0)
            {
                RefreshAllShownItemWithFirstIndexAndPos(firstItemIndex, pos, AlphaEffect);
            }
        }



        //建议使用绑定数据的方法，在ListView的初始化，更新等使用GetUserData提取数据，
        //最好不要用闭包的上下文来传递数据，因为闭包会把上一次的老数据留在闭包函数里面。
        //例如
        //int a = 12;
        //action call = ()=>{ a }
        // a = 13;
        // call()   函数里面的a的值实际上是12
        //通过动态绑定给list的数据
        private object m_UserData;
        public void SetUserData(object userData)
        {
            m_UserData = userData;
        }
        public object GetUserData()
        {
            return m_UserData;
        }
        public bool IsListViewInited()
        {
            return mListViewInited;
        }
        /*
        InitListView method is to initiate the LoopListView2 component. There are 3 parameters:
        logicItemTotalCount: the total item count in the listview. If this parameter is set -1, then means there are infinite items, and scrollbar would not be supported, and the ItemIndex can be from –MaxInt to +MaxInt. If this parameter is set a value >=0 , then the ItemIndex can only be from 0 to itemTotalCount -1.
        onGetItemByIndex: when a item is getting in the scrollrect viewport, and this Action will be called with the item’ index as a parameter, to let you create the item and update its content.
        */
        public void InitListView(
            System.Func<LoopListView2, int, LoopListViewItem2> onShowItemByIndex, 
            System.Action<LoopListViewItem2, int, LineStartStyle> onUpdateItem = null, 
            System.Action<LoopListViewItem2> onHideItem = null,
            LoopListViewInitParam initParam = null)
        {
            if(initParam != null)
            {
                mDistanceForRecycle0 = initParam.mDistanceForRecycle0;
                mDistanceForNew0 = initParam.mDistanceForNew0;
                mDistanceForRecycle1 = initParam.mDistanceForRecycle1;
                mDistanceForNew1 = initParam.mDistanceForNew1;
                mSmoothDumpRate = initParam.mSmoothDumpRate;
                mSnapFinishThreshold = initParam.mSnapFinishThreshold;
                mSnapVecThreshold = initParam.mSnapVecThreshold;
                mItemDefaultWithPaddingSize = initParam.mItemDefaultWithPaddingSize;
            }
            mScrollRect = gameObject.GetComponent<ScrollRect>();
            if (mScrollRect == null)
            {
                Debug.LogError("ListView Init Failed! ScrollRect component not found!");
                return;
            }
            if(mDistanceForRecycle0 <= mDistanceForNew0)
            {
                Debug.LogError("mDistanceForRecycle0 should be bigger than mDistanceForNew0");
            }
            if (mDistanceForRecycle1 <= mDistanceForNew1)
            {
                Debug.LogError("mDistanceForRecycle1 should be bigger than mDistanceForNew1");
            }
            mCurSnapData.Clear();
            mItemPosMgr = new ItemPosMgr(mItemDefaultWithPaddingSize);
            mScrollRectTransform = mScrollRect.GetComponent<RectTransform>();
            mContainerTrans = mScrollRect.content;
            mViewPortRectTransform = mScrollRect.viewport;
            if (mViewPortRectTransform == null)
            {
                mViewPortRectTransform = mScrollRectTransform;
            }
            if (mScrollRect.horizontalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport && mScrollRect.horizontalScrollbar != null)
            {
                Debug.LogError("ScrollRect.horizontalScrollbarVisibility cannot be set to AutoHideAndExpandViewport");
            }
            if (mScrollRect.verticalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport && mScrollRect.verticalScrollbar != null)
            {
                Debug.LogError("ScrollRect.verticalScrollbarVisibility cannot be set to AutoHideAndExpandViewport");
            }
            mIsVertList = (mArrangeType == ListItemArrangeType.TopToBottom || mArrangeType == ListItemArrangeType.BottomToTop);
            mScrollRect.horizontal = !mIsVertList;
            mScrollRect.vertical = mIsVertList;
            SetScrollbarListener();
            AdjustPivot(mViewPortRectTransform);
            AdjustAnchor(mContainerTrans);
            AdjustContainerPivot(mContainerTrans);
            InitItemPool();
            mOnShowItemCall = onShowItemByIndex;
            mOnHideItemCall = onHideItem;
            mOnUpdateItemCall = onUpdateItem;
            if(mListViewInited == true)
            {
                Debug.LogError("LoopListView2.InitListView method can be called only once.");
            }
            mListViewInited = true;
            m_ManulUpdateViewRefCount = 0;
            //m_EnableAlphaEffect = true;
            //m_EnableSplitTime = true;

            var id = GetInstanceID();
            GameDll.RenderEvent.Event.OnAddCoroutinesGameObject(this);

            ResetListView();
            //SetListItemCount(logicItemTotalCount, true);
        }

        void SetScrollbarListener()
        {
            mScrollBarClickEventListener = null;
            Scrollbar curScrollBar = null;
            if (mIsVertList && mScrollRect.verticalScrollbar != null)
            {
                curScrollBar = mScrollRect.verticalScrollbar;

            }
            if (!mIsVertList && mScrollRect.horizontalScrollbar != null)
            {
                curScrollBar = mScrollRect.horizontalScrollbar;
            }
            if(curScrollBar == null)
            {
                return;
            }
            ClickEventListener listener = ClickEventListener.Get(curScrollBar.gameObject);
            mScrollBarClickEventListener = listener;
            listener.SetPointerUpHandler(OnPointerUpInScrollBar);
            listener.SetPointerDownHandler(OnPointerDownInScrollBar);
        }

        void OnPointerDownInScrollBar(GameObject obj)
        {
            mCurSnapData.Clear();
        }

        void OnPointerUpInScrollBar(GameObject obj)
        {
            ForceSnapUpdateCheck();
        }

        public void ResetListView(bool resetPos = true)
        {
            mViewPortRectTransform.GetLocalCorners(mViewPortRectLocalCorners);
            if(resetPos)
            {
                mContainerTrans.anchoredPosition3D = Vector3.zero;
            }
            ForceSnapUpdateCheck();
        }
        public int GetShowIndexByLogicIndex(int logicIndex)
        {
            return Mathf.FloorToInt(1.0f * logicIndex / m_PerCount);
        }
        #region grid cell
        //获取这行有多少个逻辑数据，如果是最后一行，逻辑数据可能会比表现格子少
        public int GetThisRowLogicCount(int showIndex)
        {
            if(this.m_PerCount <= 1)
            {
                return 1;
            }
            var startIndex = this.m_PerCount * showIndex;
            var count = this.m_PerCount;
            if (showIndex == this.ShowItemTotalCount - 1)
            {
                count = m_LogicDataTotalCount - startIndex;
            }
            return count;
        }
        //获取这行某个小格子的逻辑数据序号
        public int GetThisRowLogicIndex(int showIndex, int small_index_in_show_row)
        {
            return this.m_PerCount * showIndex + small_index_in_show_row;
        }
        public Transform GetCellRender(int logicIndex)
        {
            var item = this.GetItemByLogicIndex(logicIndex);
            if (item == null)
            {
                return null;
            }
            var index = GetCellIndexInThisRow(logicIndex);
            return item.transform.GetChild(index);
        }
        public Transform GetCellRender(int showIndex, int small_index_in_show_row)
        {
            var logicIndex = this.GetThisRowLogicIndex(showIndex, small_index_in_show_row);
            var item = this.GetItemByLogicIndex(logicIndex);
            if (item == null)
            {
                return null;
            }
            return item.transform.GetChild(small_index_in_show_row);
        }
       
        //获取逻辑序号 在对应行的格子实际上的小序号
        public int GetCellIndexInThisRow(int logicIndex)
        {
            return logicIndex % this.m_PerCount;
        }
        public bool IsGridLoopList()
        {
            return m_PerCount > 1;
        }

        //获取网格的绑定的数据，此处的数据是由业务层定义的
        public object GetCellView(int logicIndex)
        {
            var show_item = GetShownItemByLogicIndex(logicIndex);
            if (show_item == null)
            {
                return null;
            }
            return GetCellView(show_item, logicIndex);
        }
        public object GetCellView(LoopListViewItem2 show_item, int logicIndex)
        {
            if (show_item == null)
            {
                return null;
            }
            if (m_PerCount > 1)
            {
                var index = GetCellIndexInThisRow(logicIndex);
                var obj = show_item.UserUIData as List<object>;
                if (obj != null && obj.Count > index)
                {
                    return obj[index];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return show_item.UserUIData;
            }
        }
        public List<object> GetAllShownViews()
        {
            List<object> all_shown_views = new List<object>();
            var shown_items = this.GetAllShownItems();
            int shown_items_count = shown_items.Count;
            for(int i = 0; i < shown_items_count; ++i)
            {
                var row = shown_items[i];
                var row_views = this.GetCellViews(row);
                foreach(var row_view in row_views)
                {
                    all_shown_views.Add(row_view);
                }
            }
            return all_shown_views;
        }
        public List<object> GetCellViews(int showIndex)
        {
            var show_item = GetShownItemByIndex(showIndex);
            if(show_item == null)
            {
                return null;
            }
            return GetCellViews(show_item);
        }
        public List<object> GetCellViews(LoopListViewItem2 show_item)
        {
            if (show_item == null)
            {
                return null;
            }
            List<object> col_datas = new List<object>();
            if (m_PerCount > 1)
            {
                for(int i = 0; i < m_PerCount; ++i)
                {
                    col_datas.Add(null);
                }

                var user_datas = show_item.UserUIData as List<object>;
                if(user_datas != null)
                {
                    for (int i = 0; i < m_PerCount; ++i)
                    {
                        if(user_datas.Count > i)
                        {
                            col_datas[i] = user_datas[i];
                        }
                    }
                }
            }
            else
            {
                var obj = show_item.UserUIData;
                col_datas.Add(obj);
            }
            return col_datas;
        }
        //在show的时候调用
        public void BindCellView(LoopListViewItem2 show_item, int logicIndex, object view)
        {
            if (show_item == null)
            {
                return;
            }
            if (m_PerCount > 1)
            {
                var index = GetCellIndexInThisRow(logicIndex);
                List<object> cells = null;
                if (show_item.UserUIData == null)
                {
                    cells = new List<object>(m_PerCount);
                    show_item.UserUIData = cells;
                    for (int i = 0; i < m_PerCount; ++i)
                    {
                        cells.Add(null);
                    }
                }
                else
                {
                    cells = show_item.UserUIData as List<object>;
                }
                if (cells != null && cells.Count > index)
                {
                    cells[index] = view;
                }
            }
            else
            {
                show_item.UserUIData = view;
            }
        }
        //在hide的时候调用
        public void UnbindCellView(int logicIndex)
        {
            var show_item = GetShownItemByLogicIndex(logicIndex);
            if (show_item == null)
            {
                return;
            }
            UnbindCellView(show_item , logicIndex);
        }
        public void UnbindCellView(LoopListViewItem2 show_item, int logicIndex)
        {
            if (show_item == null)
            {
                return;
            }
            if (m_PerCount > 1)
            {
                var index = GetCellIndexInThisRow(logicIndex);
                List<object> cells = null;
                if (show_item.UserUIData == null)
                {
                    return;
                }
                else
                {
                    cells = show_item.UserUIData as List<object>;
                }
                if (cells != null && cells.Count > index)
                {
                    cells[index] = null;
                }
            }
            else
            {
                show_item.UserUIData = null;
            }
        }
        public void UnbindCellView(LoopListViewItem2 show_item)
        {
            if (show_item == null)
            {
                return;
            }
            if (m_PerCount > 1)
            {
                List<object> cells = null;
                if (show_item.UserUIData == null)
                {
                    return;
                }
                else
                {
                    cells = show_item.UserUIData as List<object>;
                }
                if (cells != null && cells.Count > 0)
                {
                    int count = cells.Count;
                    for(int i = 0; i < count; ++i)
                    {
                        cells[i] = null;
                    }
                }
            }
            else
            {
                show_item.UserUIData = null;
            }
        }

        public void OnShowRowCells(
            LoopListViewItem2 row, 
            int showIndex,
            Func<GameObject, int, object> createViewCall, 
            Action<int> OnShowCell)
        {
            var row_arrays = (UIArray)row.GetComponent(typeof(UIArray));

            if (row_arrays.m_Items.Count < m_PerCount)
            {
                RenderAPI.UIArrayCopy(row_arrays, m_PerCount);
            }
            int subIndexStart = showIndex * m_PerCount;
            for (int i = 0; i < m_PerCount; ++i)
            {
                int logic_index = subIndexStart + i;

                var cell_go = row_arrays.m_Items[i].gameObject;
                var view = createViewCall(cell_go, logic_index);
                BindCellView(row, logic_index, view);
                //最后一行超出总个数的部分，这里没有做默认处理，交给业务层处理显示隐藏
                OnShowCell(logic_index);
            }

        }

        #endregion
        /*
        This method may use to set the item total count of the scrollview at runtime. 
        If this parameter is set -1, then means there are infinite items,
        and scrollbar would not be supported, and the ItemIndex can be from –MaxInt to +MaxInt. 
        If this parameter is set a value >=0 , then the ItemIndex can only be from 0 to itemTotalCount -1.  
        If resetPos is set false, then the scrollrect’s content position will not changed after this method finished.
        */
        /// <summary>
        /// 刷新显示，当设置了数量后需要刷新显示，这里的刷新肯定会把老的预制件回收，然后重新显示，避免了有可见的没有刷新的问题
        /// </summary>
        /// <param name="logicItemCount">设置数量</param>
        /// <param name="move_to_index">重置位置到序号</param>
        /// <param name="AlphaEffect">是否显示刷新效果</param>
        public void SetListItemCount(int logicItemCount, int move_to_index = -1, bool split = false, bool AlphaEffect = false)
        {
            Coroutine_StopAllCoroutines();
            //m_EnableAlphaEffect = AlphaEffect;
            //m_EnableSplitTime = split;

            m_LogicDataTotalCount = logicItemCount;
            int itemShowCount = 1;
            if(logicItemCount % m_PerCount == 0)
            {
                itemShowCount = logicItemCount / m_PerCount;
            }
            else
            {
                itemShowCount = logicItemCount / m_PerCount + 1;
            }
            mCurSnapData.Clear();
            mShowItemTotalCount = itemShowCount;
            if (mShowItemTotalCount < 0)
            {
                mSupportScrollBar = false;
            }
            if (mSupportScrollBar)
            {
                mItemPosMgr.SetItemMaxCount(mShowItemTotalCount);
            }
            else
            {
                mItemPosMgr.SetItemMaxCount(0);
            }
            if (mShowItemTotalCount == 0)
            {
                mCurReadyMaxItemIndex = 0;
                mCurReadyMinItemIndex = 0;
                mNeedCheckNextMaxItem = false;
                mNeedCheckNextMinItem = false;
                RecycleAllItem();
                ClearAllTmpRecycledItem();
                UpdateContentSize();
                return;
            }
            if(mCurReadyMaxItemIndex >= mShowItemTotalCount)
            {
                mCurReadyMaxItemIndex = mShowItemTotalCount - 1;
            }
            mLeftSnapUpdateExtraCount = 1;
            mNeedCheckNextMaxItem = true;
            mNeedCheckNextMinItem = true;
            if (move_to_index >= 0)
            {
                MovePanelToItemIndex(move_to_index, 0, split, AlphaEffect);
                return;
            }
            if (mItemList.Count == 0)
            {
                MovePanelToItemIndex(0, 0, false, AlphaEffect);
                return;
            }
            int maxItemIndex = mShowItemTotalCount - 1;
            int lastItemIndex = mItemList[mItemList.Count - 1].ItemRenderIndex;
            if (lastItemIndex <= maxItemIndex)
            {
                UpdateContentSize();
                UpdateAllShownItemsPos();
                RefreshAllShownItem(AlphaEffect);
                return;
            }
            MovePanelToItemIndex(maxItemIndex, 0, false, AlphaEffect);
        }

        //To get the visible item by itemIndex. If the item is not visible, then this method return null.
        public LoopListViewItem2 GetShownItemByItemIndex(int itemIndex)
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return null;
            }
            if (itemIndex < mItemList[0].ItemRenderIndex || itemIndex > mItemList[count - 1].ItemRenderIndex)
            {
                return null;
            }
            int i = itemIndex - mItemList[0].ItemRenderIndex;
            return mItemList[i];
        }

        public int ShownItemCount
        {
            get
            {
                return mItemList.Count;
            }
        }

        public float ViewPortSize
        {
            get
            {
                if (mIsVertList)
                {
                    return mViewPortRectTransform.rect.height;
                }
                else
                {
                    return mViewPortRectTransform.rect.width;
                }
            }
        }

        public float ViewPortWidth
        {
            get { return mViewPortRectTransform.rect.width; }
        }
        public float ViewPortHeight
        {
            get { return mViewPortRectTransform.rect.height; }
        }

        //获取item在整个可能的列表，如果当前logicIndex不可见，返回null
        public LoopListViewItem2 GetItemByLogicIndex(int logicIndex)
        {
            int count = mItemList.Count;
            if (logicIndex < 0)
            {
                return null;
            }
            var showIndex = GetShowIndexByLogicIndex(logicIndex);

            if (mItemList.Count > 0)
            {
                var firstShowIndex = mItemList[0].ItemRenderIndex;
                var indexInItemList = showIndex - firstShowIndex;
                if (indexInItemList >= 0 && indexInItemList < count)
                {
                    return mItemList[indexInItemList];
                }
            }
            return null;
        }
        /*
         All visible items is stored in a List<LoopListViewItem2> , which is named mItemList;
         this method is to get the visible item by the index in visible items list. The parameter index is from 0 to mItemList.Count.
        */
        public LoopListViewItem2 GetShownItemByIndex(int index)
        {
            int count = mItemList.Count;
            if(index < 0 || index >= count)
            {
                return null;
            }
            return mItemList[index];
        }
        public LoopListViewItem2 GetShownItemByLogicIndex(int logicIndex)
        {
            var index = GetShowIndexByLogicIndex(logicIndex);
            if(mItemList.Count > 0)
            {
                index = index - mItemList[0].ItemRenderIndex;
            }
            int count = mItemList.Count;
            if (index < 0 || index >= count)
            {
                return null;
            }
            return mItemList[index];
        }
        public List<LoopListViewItem2> GetAllShownItems()
        {
            return mItemList;
        }
        public LoopListViewItem2 GetShownItemByIndexWithoutCheck(int index)
        {
            return mItemList[index];
        }

        public int GetIndexInShownItemList(LoopListViewItem2 item)
        {
            if(item == null)
            {
                return -1;
            }
            int count = mItemList.Count;
            if (count == 0)
            {
                return -1;
            }
            for (int i = 0; i < count; ++i)
            {
                if (mItemList[i] == item)
                {
                    return i;
                }
            }
            return -1;
        }


        public void DoActionForEachShownItem(System.Action<LoopListViewItem2,object> action,object param)
        {
            if(action == null)
            {
                return;
            }
            int count = mItemList.Count;
            if(count == 0)
            {
                return;
            }
            for (int i = 0; i < count; ++i)
            {
                action(mItemList[i],param);
            }
        }


        public LoopListViewItem2 NewListViewItem(string itemPrefabName, int index)
        {
            ItemPool pool = null;
            if (mItemPoolDict.TryGetValue(itemPrefabName, out pool) == false)
            {
                return null;
            }
            LoopListViewItem2 item = pool.GetItem();
            RectTransform rf = item.GetComponent<RectTransform>();
            rf.SetParent(mContainerTrans);
            rf.localScale = Vector3.one;
            rf.anchoredPosition3D = Vector3.zero;
            rf.localEulerAngles = Vector3.zero;
            item.ParentListView = this;
            item.ItemRenderIndex = index;
            if (m_NewItemStyle == NewItemStyle.InsertHead)
            {
                ItemInsert(0, item);
            }
            else if(m_NewItemStyle == NewItemStyle.Update)
            {
                if(m_NewItemUpdateIndex >= 0 && m_NewItemUpdateIndex < mItemList.Count)
                {
                    SetItemValue(m_NewItemUpdateIndex, item);
                }
                else
                {
                    // 打印日志,方便排查问题
                    Debug.LogError($"[LoopListView2] m_NewItemUpdateIndex invalid: {m_NewItemUpdateIndex}, " +
                                   $"mItemList.Count={mItemList.Count}, logicIndex={index}");
                    // 保守处理: 退化为 AddItem
                    AddItem(item);
                }
            }
            else 
            {
                AddItem(item);
            }

            return item;
        }
        private void RemoveItem(int index)
        {
            mItemList.RemoveAt(index);
        }
        private void AddItem(LoopListViewItem2 item)
        {
            mItemList.Add(item);
        }
        private void ItemInsert(int index, LoopListViewItem2 item)
        {
            mItemList.Insert(index, item);
        }
        private void SetItemValue(int index, LoopListViewItem2 item)
        {
            mItemList[index] = item;
        }
        private void ClearItem()
        {
            mItemList.Clear();
        }
        /*
        For a vertical scrollrect, when a visible item’s height changed at runtime, then this method should be called to let the LoopListView2 component reposition all visible items’ position.
        For a horizontal scrollrect, when a visible item’s width changed at runtime, then this method should be called to let the LoopListView2 component reposition all visible items’ position.
        */
        public void OnItemSizeChanged(int itemIndex)
        {
            LoopListViewItem2 item = GetShownItemByItemIndex(itemIndex);
            if (item == null)
            {
                return;
            }
            if (mSupportScrollBar)
            {
                if (mIsVertList)
                {
                    SetItemSize(itemIndex, item.CachedRectTransform.rect.height, item.Padding);
                }
                else
                {
                    SetItemSize(itemIndex, item.CachedRectTransform.rect.width, item.Padding);
                }
            }
            UpdateContentSize();                
            UpdateAllShownItemsPos();
        }


        /*
        To update a item by itemIndex.if the itemIndex-th item is not visible, then this method will do nothing.
        Otherwise this method will first call onGetItemByIndex(itemIndex) to get a updated item and then reposition all visible items'position. 
        */
        public void RefreshItemByItemIndex(int itemIndex, bool AlphaEffect)
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            if (itemIndex < mItemList[0].ItemRenderIndex || itemIndex > mItemList[count - 1].ItemRenderIndex)
            {
                return;
            }
            int firstItemIndex = mItemList[0].ItemRenderIndex;
            int i = itemIndex - firstItemIndex;
            LoopListViewItem2 curItem = mItemList[i];
            Vector3 pos = curItem.CachedRectTransform.anchoredPosition3D;
            RecycleItemTmp(curItem);

            m_NewItemStyle = NewItemStyle.Update;
            m_NewItemUpdateIndex = i;
            LoopListViewItem2 newItem = GetNewItemByIndex(itemIndex, AlphaEffect);
            if (newItem == null)
            {
                RefreshAllShownItemWithFirstIndex(firstItemIndex, AlphaEffect);
                return;
            }
            //mItemList[i] = newItem;
            if(mIsVertList)
            {
                pos.x = newItem.StartPosOffset;
            }
            else
            {
                pos.y = newItem.StartPosOffset;
            }
            newItem.CachedRectTransform.anchoredPosition3D = pos;
            OnItemSizeChanged(itemIndex);
            ClearAllTmpRecycledItem();
        }

        //snap move will finish at once.
        public void FinishSnapImmediately()
        {
            UpdateSnapMove(true);
        }
        public int Coroutine_GetInstanceID()
        {
            return this.GetInstanceID();
        }
        public void Coroutine_StopAllCoroutines()
        {
            m_ManulUpdateViewRefCount = 0;
            m_EnableAlphaEffect = false;
            StopAllCoroutines();
        }
        public void MovePanelToItemIndex(int itemIndex, float offset = 0, bool split = false, bool AlphaEffect = false)
        {
            Coroutine_StopAllCoroutines();
            if (split)
            {
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(MovePanelToItemIndexCoroutine(itemIndex, offset, AlphaEffect));
                }
                return;
            }
            else
            {
                mScrollRect.StopMovement();
                mCurSnapData.Clear();
                if (itemIndex < 0 || mShowItemTotalCount == 0)
                {
                    return;
                }
                if (mShowItemTotalCount > 0 && itemIndex >= mShowItemTotalCount)
                {
                    itemIndex = mShowItemTotalCount - 1;
                }
                if (offset < 0)
                {
                    offset = 0;
                }
                Vector3 pos = Vector3.zero;
                float viewPortSize = ViewPortSize;
                if (offset > viewPortSize)
                {
                    offset = viewPortSize;
                }
                if (mArrangeType == ListItemArrangeType.TopToBottom)
                {
                    float containerPos = mContainerTrans.anchoredPosition3D.y;
                    if (containerPos < 0)
                    {
                        containerPos = 0;
                    }
                    pos.y = -containerPos - offset;
                }
                else if (mArrangeType == ListItemArrangeType.BottomToTop)
                {
                    float containerPos = mContainerTrans.anchoredPosition3D.y;
                    if (containerPos > 0)
                    {
                        containerPos = 0;
                    }
                    pos.y = -containerPos + offset;
                }
                else if (mArrangeType == ListItemArrangeType.LeftToRight)
                {
                    float containerPos = mContainerTrans.anchoredPosition3D.x;
                    if (containerPos > 0)
                    {
                        containerPos = 0;
                    }
                    pos.x = -containerPos + offset;
                }
                else if (mArrangeType == ListItemArrangeType.RightToLeft)
                {
                    float containerPos = mContainerTrans.anchoredPosition3D.x;
                    if (containerPos < 0)
                    {
                        containerPos = 0;
                    }
                    pos.x = -containerPos - offset;
                }

                RecycleAllItem();

                m_NewItemStyle = NewItemStyle.AddTail;
                LoopListViewItem2 newItem = GetNewItemByIndex(itemIndex, AlphaEffect);
                if (newItem == null)
                {
                    ClearAllTmpRecycledItem();
                    return;
                }
                if (mIsVertList)
                {
                    pos.x = newItem.StartPosOffset;
                }
                else
                {
                    pos.y = newItem.StartPosOffset;
                }
                newItem.CachedRectTransform.anchoredPosition3D = pos;
                if (mSupportScrollBar)
                {
                    if (mIsVertList)
                    {
                        SetItemSize(itemIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                    }
                    else
                    {
                        SetItemSize(itemIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                    }
                }
                //mItemList.Add(newItem);
                UpdateContentSize();
                UpdateListView(viewPortSize + 100, viewPortSize + 100, viewPortSize, viewPortSize, AlphaEffect);
                //Debug.Log("手动刷新列表完成");
                AdjustPanelPos();
                ClearAllTmpRecycledItem();
            }
        }
        /*
        This method will move the scrollrect content’s position to ( the positon of itemIndex-th item + offset ),
        and in current version the itemIndex is from 0 to MaxInt, offset is from 0 to scrollrect viewport size. 
        */
        public IEnumerator MovePanelToItemIndexCoroutine(int itemIndex, float offset, bool AlphaEffect)
        {
            mScrollRect.StopMovement();
            mCurSnapData.Clear();
            if (itemIndex < 0 || mShowItemTotalCount == 0)
            {
                yield break;
            }
            if (mShowItemTotalCount > 0 && itemIndex >= mShowItemTotalCount)
            {
                itemIndex = mShowItemTotalCount - 1;
            }
            if (offset < 0)
            {
                offset = 0;
            }
            Vector3 pos = Vector3.zero;
            float viewPortSize = ViewPortSize;
            if (offset > viewPortSize)
            {
                offset = viewPortSize;
            }
            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float containerPos = mContainerTrans.anchoredPosition3D.y;
                if (containerPos < 0)
                {
                    containerPos = 0;
                }
                pos.y = -containerPos - offset;
            }
            else if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                float containerPos = mContainerTrans.anchoredPosition3D.y;
                if (containerPos > 0)
                {
                    containerPos = 0;
                }
                pos.y = -containerPos + offset;
            }
            else if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                float containerPos = mContainerTrans.anchoredPosition3D.x;
                if (containerPos > 0)
                {
                    containerPos = 0;
                }
                pos.x = -containerPos + offset;
            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                float containerPos = mContainerTrans.anchoredPosition3D.x;
                if (containerPos < 0)
                {
                    containerPos = 0;
                }
                pos.x = -containerPos - offset;
            }

            RecycleAllItem();

            m_NewItemStyle = NewItemStyle.AddTail;
            LoopListViewItem2 newItem = GetNewItemByIndex(itemIndex, AlphaEffect);
            if (newItem == null)
            {
                ClearAllTmpRecycledItem();
                yield break;
            }
            if (mIsVertList)
            {
                pos.x = newItem.StartPosOffset;
            }
            else
            {
                pos.y = newItem.StartPosOffset;
            }
            newItem.CachedRectTransform.anchoredPosition3D = pos;
            
            if (mSupportScrollBar)
            {
                if (mIsVertList)
                {
                    SetItemSize(itemIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                }
                else
                {
                    SetItemSize(itemIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                }
            }
            //mItemList.Add(newItem);
            UpdateContentSize();

            if (m_SplitTime > 0)
            {
                m_ManulUpdateViewRefCount++;
                yield return m_SplitTimeWaitForSeconds;
                m_ManulUpdateViewRefCount--;
            }
            m_ManulUpdateViewRefCount++;
            yield return UpdateListViewCoroutne(viewPortSize + 100, viewPortSize + 100, viewPortSize, viewPortSize, AlphaEffect);
            m_ManulUpdateViewRefCount--;

            //Debug.Log("手动刷新列表完成");
            AdjustPanelPos();
            ClearAllTmpRecycledItem();
        }

        //update all visible items.
        //这个函数会清理当前显示的并且重新展示
        //mOnShowItemCall
        public void RefreshAllShownItem(bool AlphaEffect = false)
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            RefreshAllShownItemWithFirstIndex(mItemList[0].ItemRenderIndex, AlphaEffect);
        }

        //仅仅更新可见item的数据，不改变位置等数据
        //这个用在当已知数据没有增加、减少、变换顺序，只是里面的内容发生改变
        //mOnUpdateItemCall
        public void UpdateShownItem(int logic_idx = 0, LineStartStyle style = LineStartStyle.Just)
        {
            int count = mItemList.Count;
            if (count == 0 || mOnUpdateItemCall == null)
            {
                return;
            }

            var start_line = GetShowIndexByLogicIndex(logic_idx);

            for (int i = 0; i < count; ++i)
            {
                var item = mItemList[i];
                if (style == LineStartStyle.Just)
                {
                    if (item.ItemRenderIndex == start_line && mOnUpdateItemCall != null)
                    {
                        mOnUpdateItemCall(item, logic_idx, style);
                        break;
                    }
                }
                else
                {
                    if(item.ItemRenderIndex >= start_line && mOnUpdateItemCall != null)
                    {
                        mOnUpdateItemCall(item, logic_idx, style);
                    }
                }
            }
        }

        public void RefreshAllShownItemWithFirstIndex(int firstItemIndex, bool AlphaEffect = false)
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            LoopListViewItem2 firstItem = mItemList[0];
            Vector3 pos = firstItem.CachedRectTransform.anchoredPosition3D;
            RecycleAllItem();
            
            for (int i = 0; i < count; ++i)
            {
                int curIndex = firstItemIndex + i;
                m_NewItemStyle = NewItemStyle.AddTail;
                LoopListViewItem2 newItem = GetNewItemByIndex(curIndex, AlphaEffect);
                if (newItem == null)
                {
                    break;
                }
                if (mIsVertList)
                {
                    pos.x = newItem.StartPosOffset;
                }
                else
                {
                    pos.y = newItem.StartPosOffset;
                }
                newItem.CachedRectTransform.anchoredPosition3D = pos;
                if (mSupportScrollBar)
                {
                    if (mIsVertList)
                    {
                        SetItemSize(curIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                    }
                    else
                    {
                        SetItemSize(curIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                    }
                }

                //mItemList.Add(newItem);
            }
            UpdateContentSize();
            UpdateAllShownItemsPos();
            ClearAllTmpRecycledItem();
        }


        public void RefreshAllShownItemWithFirstIndexAndPos(int firstItemIndex, Vector3 pos, bool AlphaEffect)
        {
            RecycleAllItem();
            m_NewItemStyle = NewItemStyle.AddTail;
            LoopListViewItem2 newItem = GetNewItemByIndex(firstItemIndex, AlphaEffect);
            if (newItem == null)
            {
                return;
            }
            if (mIsVertList)
            {
                pos.x = newItem.StartPosOffset;
            }
            else
            {
                pos.y = newItem.StartPosOffset;
            }
            newItem.CachedRectTransform.anchoredPosition3D = pos;
            if (mSupportScrollBar)
            {
                if (mIsVertList)
                {
                    SetItemSize(firstItemIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                }
                else
                {
                    SetItemSize(firstItemIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                }
            }
            //mItemList.Add(newItem);
            UpdateContentSize();
            UpdateAllShownItemsPos();
            UpdateListView(mDistanceForRecycle0, mDistanceForRecycle1, mDistanceForNew0, mDistanceForNew1, AlphaEffect);
            ClearAllTmpRecycledItem();
        }


        void RecycleItemTmp(LoopListViewItem2 item)
        {
            if (item == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(item.ItemPrefabName))
            {
                return;
            }
            ItemPool pool = null;
            if (mItemPoolDict.TryGetValue(item.ItemPrefabName, out pool) == false)
            {
                return;
            }
            pool.RecycleItem(item);

            if(mOnHideItemCall != null)
            {
                mOnHideItemCall(item);
            }
            //业务层可以不用明确的使用Hide函数专门处理，这里会默认处理。因为如果使用OnShowRowCells的话，业务层是没有显式的处理Bind操作
            this.UnbindCellView(item);
        }


        void ClearAllTmpRecycledItem()
        {
            int count = mItemPoolList.Count;
            for(int i = 0;i<count;++i)
            {
                mItemPoolList[i].ClearTmpRecycledItem();
            }
        }


        void RecycleAllItem()
        {
            foreach (LoopListViewItem2 item in mItemList)
            {
                RecycleItemTmp(item);
            }
            ClearItem();
        }


        void AdjustContainerPivot(RectTransform rtf)
        {
            Vector2 pivot = rtf.pivot;
            if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                pivot.y = 0;
            }
            else if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                pivot.y = 1;
            }
            else if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                pivot.x = 0;
            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                pivot.x = 1;
            }
            rtf.pivot = pivot;
        }


        void AdjustPivot(RectTransform rtf)
        {
            Vector2 pivot = rtf.pivot;

            if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                pivot.y = 0;
            }
            else if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                pivot.y = 1;
            }
            else if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                pivot.x = 0;
            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                pivot.x = 1;
            }
            rtf.pivot = pivot;
        }

        void AdjustContainerAnchor(RectTransform rtf)
        {
            Vector2 anchorMin = rtf.anchorMin;
            Vector2 anchorMax = rtf.anchorMax;
            if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                anchorMin.y = 0;
                anchorMax.y = 0;
            }
            else if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                anchorMin.y = 1;
                anchorMax.y = 1;
            }
            else if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                anchorMin.x = 0;
                anchorMax.x = 0;
            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                anchorMin.x = 1;
                anchorMax.x = 1;
            }
            rtf.anchorMin = anchorMin;
            rtf.anchorMax = anchorMax;
        }


        void AdjustAnchor(RectTransform rtf)
        {
            Vector2 anchorMin = rtf.anchorMin;
            Vector2 anchorMax = rtf.anchorMax;
            if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                anchorMin.y = 0;
                anchorMax.y = 0;
            }
            else if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                anchorMin.y = 1;
                anchorMax.y = 1;
            }
            else if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                anchorMin.x = 0;
                anchorMax.x = 0;
            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                anchorMin.x = 1;
                anchorMax.x = 1;
            }
            rtf.anchorMin = anchorMin;
            rtf.anchorMax = anchorMax;
        }

        void InitItemPool()
        {
            foreach (ItemPrefabConfData data in mItemPrefabDataList)
            {
                if (data.mItemPrefab == null)
                {
                    Debug.LogError("A item prefab is null ");
                    continue;
                }
                string prefabName = data.mItemPrefab.name;
                if (mItemPoolDict.ContainsKey(prefabName))
                {
                    Debug.LogError("A item prefab with name " + prefabName + " has existed!");
                    continue;
                }
                RectTransform rtf = data.mItemPrefab.GetComponent<RectTransform>();
                if (rtf == null)
                {
                    Debug.LogError("RectTransform component is not found in the prefab " + prefabName);
                    continue;
                }
                AdjustAnchor(rtf);
                AdjustPivot(rtf);
                LoopListViewItem2 tItem = data.mItemPrefab.GetComponent<LoopListViewItem2>();
                if (tItem == null)
                {
                    data.mItemPrefab.AddComponent<LoopListViewItem2>();
                }
                ItemPool pool = new ItemPool();
                pool.Init(data.mItemPrefab, data.mPadding,data.mStartPosOffset, data.mInitCreateCount, mContainerTrans);
                mItemPoolDict.Add(prefabName, pool);
                mItemPoolList.Add(pool);
            }
        }



        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (ParentListView)
            {
                if (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
                {
                    //水平
                    if (mArrangeType == ListItemArrangeType.BottomToTop || mArrangeType == ListItemArrangeType.TopToBottom)
                    {
                        ExecuteEvents.Execute(ParentListView.gameObject, eventData, ExecuteEvents.beginDragHandler);
                        return;
                    }
                }
                else
                {
                    //垂直
                    if (mArrangeType == ListItemArrangeType.LeftToRight || mArrangeType == ListItemArrangeType.RightToLeft)
                    {
                        ExecuteEvents.Execute(ParentListView.gameObject, eventData, ExecuteEvents.beginDragHandler);
                        return;
                    }
                }
            }
            mIsDraging = true;
            Coroutine_StopAllCoroutines();

            CacheDragPointerEventData(eventData);
            mCurSnapData.Clear();
            if(mOnBeginDragAction != null)
            {
                mOnBeginDragAction();
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            if (ParentListView)
            {
                if (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
                {
                    //水平
                    if (mArrangeType == ListItemArrangeType.BottomToTop || mArrangeType == ListItemArrangeType.TopToBottom)
                    {
                        ExecuteEvents.Execute(ParentListView.gameObject, eventData, ExecuteEvents.endDragHandler);
                        return;
                    }
                }
                else
                {
                    //垂直
                    if (mArrangeType == ListItemArrangeType.LeftToRight || mArrangeType == ListItemArrangeType.RightToLeft)
                    {
                        ExecuteEvents.Execute(ParentListView.gameObject, eventData, ExecuteEvents.endDragHandler);
                        return;
                    }
                }
            }
            mIsDraging = false;
            mPointerEventData = null;
            if (mOnEndDragAction != null)
            {
                mOnEndDragAction();
            }
            ForceSnapUpdateCheck();
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            if (ParentListView)
            {
                if(Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
                {
                    //水平
                    if( mArrangeType == ListItemArrangeType.BottomToTop  || mArrangeType == ListItemArrangeType.TopToBottom)
                    {
                        ExecuteEvents.Execute(ParentListView.gameObject, eventData, ExecuteEvents.dragHandler);
                        return;
                    }
                }
                else
                {
                    //垂直
                    if (mArrangeType == ListItemArrangeType.LeftToRight || mArrangeType == ListItemArrangeType.RightToLeft)
                    {
                        ExecuteEvents.Execute(ParentListView.gameObject, eventData, ExecuteEvents.dragHandler);
                        return;
                    }
                }
            }
            CacheDragPointerEventData(eventData);
            if (mOnDragingAction != null)
            {
                mOnDragingAction();
            }
        }

        void CacheDragPointerEventData(PointerEventData eventData)
        {
            if (mPointerEventData == null)
            {
                mPointerEventData = new PointerEventData(EventSystem.current);
            }
            mPointerEventData.button = eventData.button;
            mPointerEventData.position = eventData.position;
            mPointerEventData.pointerPressRaycast = eventData.pointerPressRaycast;
            mPointerEventData.pointerCurrentRaycast = eventData.pointerCurrentRaycast;
        }

        LoopListViewItem2 GetNewItemByIndex(int index, bool AlphaEffect)
        {
            if(mSupportScrollBar && index < 0)
            {
                return null;
            }
            if(mShowItemTotalCount > 0 && index >= mShowItemTotalCount)
            {
                return null;
            }

            //建议上面那个判断重新写成下面的,这样，具体的逻辑里面就不需要判断那么多了
            if(index < 0 || index >= mShowItemTotalCount)
            {
                return null;
            }
            this.m_EnableAlphaEffect = AlphaEffect;
            LoopListViewItem2 newItem = mOnShowItemCall(this, index);
            if (newItem == null)
            {
                return null;
            }
            newItem.ItemCreatedCheckFrameCount = mListUpdateCheckFrameCount;
            return newItem;
        }


        void SetItemSize(int itemIndex, float itemSize,float padding)
        {
            mItemPosMgr.SetItemSize(itemIndex, itemSize+padding);
            if(itemIndex >= mLastItemIndex)
            {
                mLastItemIndex = itemIndex;
                mLastItemPadding = padding;
            }
        }

        void GetPlusItemIndexAndPosAtGivenPos(float pos, ref int index, ref float itemPos)
        {
            mItemPosMgr.GetItemIndexAndPosAtGivenPos(pos, ref index, ref itemPos);
        }


        float GetItemPos(int itemIndex)
        {
            return mItemPosMgr.GetItemPos(itemIndex);
        }

      
        public Vector3 GetItemCornerPosInViewPort(LoopListViewItem2 item, ItemCornerEnum corner = ItemCornerEnum.LeftBottom)
        {
            item.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
            return mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[(int)corner]);
        }
       

        void AdjustPanelPos()
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            UpdateAllShownItemsPos();
            float viewPortSize = ViewPortSize;
            float contentSize = GetContentPanelSize();
            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                if (contentSize <= viewPortSize)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.y = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(mItemList[0].StartPosOffset,0,0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 topPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                if (topPos0.y < mViewPortRectLocalCorners[1].y)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.y = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(mItemList[0].StartPosOffset, 0, 0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 downPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
                float d = downPos1.y - mViewPortRectLocalCorners[0].y;
                if (d > 0)
                {
                    Vector3 pos = mItemList[0].CachedRectTransform.anchoredPosition3D;
                    pos.y = pos.y - d;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = pos;
                    UpdateAllShownItemsPos();
                    return;
                }
            }
            else if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                if (contentSize <= viewPortSize)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.y = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(mItemList[0].StartPosOffset, 0, 0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 downPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
                if (downPos0.y > mViewPortRectLocalCorners[0].y)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.y = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(mItemList[0].StartPosOffset, 0, 0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 topPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                float d = mViewPortRectLocalCorners[1].y - topPos1.y;
                if (d > 0)
                {
                    Vector3 pos = mItemList[0].CachedRectTransform.anchoredPosition3D;
                    pos.y = pos.y + d;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = pos;
                    UpdateAllShownItemsPos();
                    return;
                }
            }
            else if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                if (contentSize <= viewPortSize)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.x = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(0,mItemList[0].StartPosOffset, 0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 leftPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                if (leftPos0.x > mViewPortRectLocalCorners[1].x)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.x = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(0, mItemList[0].StartPosOffset, 0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 rightPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[2]);
                float d = mViewPortRectLocalCorners[2].x - rightPos1.x;
                if (d > 0)
                {
                    Vector3 pos = mItemList[0].CachedRectTransform.anchoredPosition3D;
                    pos.x = pos.x + d;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = pos;
                    UpdateAllShownItemsPos();
                    return;
                }
            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                if (contentSize <= viewPortSize)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.x = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(0, mItemList[0].StartPosOffset, 0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 rightPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[2]);
                if (rightPos0.x < mViewPortRectLocalCorners[2].x)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.x = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(0, mItemList[0].StartPosOffset, 0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 leftPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                float d = leftPos1.x - mViewPortRectLocalCorners[1].x;
                if (d > 0)
                {
                    Vector3 pos = mItemList[0].CachedRectTransform.anchoredPosition3D;
                    pos.x = pos.x - d;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = pos;
                    UpdateAllShownItemsPos();
                    return;
                }
            }



        }

        void Awake()
        {
            if(m_SplitTime > 0)
            {
                m_SplitTimeWaitForSeconds = new WaitForSeconds(m_SplitTime / 1000.0f);
            }
        }
        void Update()
        {
            if (mListViewInited == false || m_ManulUpdateViewRefCount > 0)
            {
                return;
            }
            if (mNeedAdjustVec)
            {
                mNeedAdjustVec = false;
                if (mIsVertList)
                {
                    if (mScrollRect.velocity.y * mAdjustedVec.y > 0)
                    {
                        mScrollRect.velocity = mAdjustedVec;
                    }
                }
                else
                {
                    if (mScrollRect.velocity.x * mAdjustedVec.x > 0)
                    {
                        mScrollRect.velocity = mAdjustedVec;
                    }
                }

            }
            if (mSupportScrollBar)
            {
                mItemPosMgr.Update(false);
            }
            UpdateSnapMove();
            UpdateListView(mDistanceForRecycle0, mDistanceForRecycle1, mDistanceForNew0, mDistanceForNew1, false);
            ClearAllTmpRecycledItem();
            mLastFrameContainerPos = mContainerTrans.anchoredPosition3D;
        }

        //update snap move. if immediate is set true, then the snap move will finish at once.
        void UpdateSnapMove(bool immediate = false)
        {
            if (mItemSnapEnable == false)
            {
                return;
            }
            if (mIsVertList)
            {
                UpdateSnapVertical(immediate);
            }
            else
            {
                UpdateSnapHorizontal(immediate);
            }
        }



        public void UpdateAllShownItemSnapData()
        {
            if (mItemSnapEnable == false)
            {
                return;
            }
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            Vector3 pos = mContainerTrans.anchoredPosition3D;
            LoopListViewItem2 tViewItem0 = mItemList[0];
            tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
            float start = 0;
            float end = 0;
            float itemSnapCenter = 0;
            float snapCenter = 0;
            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                snapCenter = -(1 - mViewPortSnapPivot.y) * mViewPortRectTransform.rect.height;
                Vector3 topPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                start = topPos1.y;
                end = start - tViewItem0.ItemSizeWithPadding;
                itemSnapCenter = start - tViewItem0.ItemSize * (1 - mItemSnapPivot.y);
                for (int i = 0; i < count; ++i)
                {
                    mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
                    if ((i + 1) < count)
                    {
                        start = end;
                        end = end - mItemList[i + 1].ItemSizeWithPadding;
                        itemSnapCenter = start - mItemList[i + 1].ItemSize * (1 - mItemSnapPivot.y);
                    }
                }
            }
            else if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                snapCenter = mViewPortSnapPivot.y * mViewPortRectTransform.rect.height;
                Vector3 bottomPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
                start = bottomPos1.y;
                end = start + tViewItem0.ItemSizeWithPadding;
                itemSnapCenter = start + tViewItem0.ItemSize * mItemSnapPivot.y;
                for (int i = 0; i < count; ++i)
                {
                    mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
                    if ((i + 1) < count)
                    {
                        start = end;
                        end = end + mItemList[i + 1].ItemSizeWithPadding;
                        itemSnapCenter = start + mItemList[i + 1].ItemSize * mItemSnapPivot.y;
                    }
                }
            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                snapCenter = -(1 - mViewPortSnapPivot.x) * mViewPortRectTransform.rect.width;
                Vector3 rightPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[2]);
                start = rightPos1.x;
                end = start - tViewItem0.ItemSizeWithPadding;
                itemSnapCenter = start - tViewItem0.ItemSize * (1 - mItemSnapPivot.x);
                for (int i = 0; i < count; ++i)
                {
                    mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
                    if ((i + 1) < count)
                    {
                        start = end;
                        end = end - mItemList[i + 1].ItemSizeWithPadding;
                        itemSnapCenter = start - mItemList[i + 1].ItemSize * (1 - mItemSnapPivot.x);
                    }
                }
            }
            else if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                snapCenter = mViewPortSnapPivot.x * mViewPortRectTransform.rect.width;
                Vector3 leftPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                start = leftPos1.x;
                end = start + tViewItem0.ItemSizeWithPadding;
                itemSnapCenter = start + tViewItem0.ItemSize * mItemSnapPivot.x;
                for (int i = 0; i < count; ++i)
                {
                    mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
                    if ((i + 1) < count)
                    {
                        start = end;
                        end = end + mItemList[i + 1].ItemSizeWithPadding;
                        itemSnapCenter = start + mItemList[i + 1].ItemSize * mItemSnapPivot.x;
                    }
                }
            }
        }



        void UpdateSnapVertical(bool immediate = false)
        {
            if(mItemSnapEnable == false)
            {
                return;
            }
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            Vector3 pos = mContainerTrans.anchoredPosition3D;
            bool needCheck = (pos.y != mLastSnapCheckPos.y);
            mLastSnapCheckPos = pos;
            if (!needCheck)
            {
                if (mLeftSnapUpdateExtraCount > 0)
                {
                    mLeftSnapUpdateExtraCount--;
                    needCheck = true;
                }
            }
            if (needCheck)
            {
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                int curIndex = -1;
                float start = 0;
                float end = 0;
                float itemSnapCenter = 0;
                float curMinDist = float.MaxValue;
                float curDist = 0;
                float curDistAbs = 0;
                float snapCenter = 0; 
                if (mArrangeType == ListItemArrangeType.TopToBottom)
                {
                    snapCenter = -(1 - mViewPortSnapPivot.y) * mViewPortRectTransform.rect.height;
                    Vector3 topPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                    start = topPos1.y;
                    end = start - tViewItem0.ItemSizeWithPadding;
                    itemSnapCenter = start - tViewItem0.ItemSize * (1-mItemSnapPivot.y);
                    for (int i = 0; i < count; ++i)
                    {
                        curDist = snapCenter - itemSnapCenter;
                        curDistAbs = Mathf.Abs(curDist);
                        if (curDistAbs < curMinDist)
                        {
                            curMinDist = curDistAbs;
                            curIndex = i;
                        }
                        else
                        {
                            break;
                        }
                        
                        if ((i + 1) < count)
                        {
                            start = end;
                            end = end - mItemList[i + 1].ItemSizeWithPadding;
                            itemSnapCenter = start - mItemList[i + 1].ItemSize * (1 - mItemSnapPivot.y);
                        }
                    }
                }
                else if(mArrangeType == ListItemArrangeType.BottomToTop)
                {
                    snapCenter = mViewPortSnapPivot.y * mViewPortRectTransform.rect.height;
                    Vector3 bottomPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
                    start = bottomPos1.y;
                    end = start + tViewItem0.ItemSizeWithPadding;
                    itemSnapCenter = start + tViewItem0.ItemSize * mItemSnapPivot.y;
                    for (int i = 0; i < count; ++i)
                    {
                        curDist = snapCenter - itemSnapCenter;
                        curDistAbs = Mathf.Abs(curDist);
                        if (curDistAbs < curMinDist)
                        {
                            curMinDist = curDistAbs;
                            curIndex = i;
                        }
                        else
                        {
                            break;
                        }

                        if ((i + 1) < count)
                        {
                            start = end;
                            end = end + mItemList[i + 1].ItemSizeWithPadding;
                            itemSnapCenter = start + mItemList[i + 1].ItemSize *  mItemSnapPivot.y;
                        }
                    }
                }

                if (curIndex >= 0)
                {
                    int oldNearestItemIndex = mCurSnapNearestItemIndex;
                    mCurSnapNearestItemIndex = mItemList[curIndex].ItemRenderIndex;
                    if (mItemList[curIndex].ItemRenderIndex != oldNearestItemIndex)
                    {
                        if (mOnSnapNearestChanged != null)
                        {
                            mOnSnapNearestChanged(this,mItemList[curIndex]);
                        }
                    }
                }
                else
                {
                    mCurSnapNearestItemIndex = -1;
                }
            }
            if (CanSnap() == false)
            {
                ClearSnapData();
                return;
            }
            float v = Mathf.Abs(mScrollRect.velocity.y);
            UpdateCurSnapData();
            if (mCurSnapData.mSnapStatus != SnapStatus.SnapMoving)
            {
                return;
            }
            if (v > 0)
            {
                mScrollRect.StopMovement();
            }
            float old = mCurSnapData.mCurSnapVal;
            mCurSnapData.mCurSnapVal = Mathf.SmoothDamp(mCurSnapData.mCurSnapVal, mCurSnapData.mTargetSnapVal, ref mSmoothDumpVel, mSmoothDumpRate);
            float dt = mCurSnapData.mCurSnapVal - old;
                
            if (immediate || Mathf.Abs(mCurSnapData.mTargetSnapVal - mCurSnapData.mCurSnapVal) < mSnapFinishThreshold)
            {
                pos.y = pos.y + mCurSnapData.mTargetSnapVal - old;
                mCurSnapData.mSnapStatus = SnapStatus.SnapMoveFinish;
                if (mOnSnapItemFinished != null)
                {
                    LoopListViewItem2 targetItem = GetShownItemByItemIndex(mCurSnapNearestItemIndex);
                    if(targetItem != null)
                    {
                        mOnSnapItemFinished(this,targetItem);
                    }
                }
            }
            else
            {
                pos.y = pos.y + dt;
            }

            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float maxY = mViewPortRectLocalCorners[0].y + mContainerTrans.rect.height;
                pos.y = Mathf.Clamp(pos.y, 0, maxY);
                mContainerTrans.anchoredPosition3D = pos;
            }
            else if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                float minY = mViewPortRectLocalCorners[1].y - mContainerTrans.rect.height;
                pos.y = Mathf.Clamp(pos.y, minY, 0);
                mContainerTrans.anchoredPosition3D = pos;
            }

        }


        void UpdateCurSnapData()
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                mCurSnapData.Clear();
                return;
            }

            if (mCurSnapData.mSnapStatus == SnapStatus.SnapMoveFinish)
            {
                if (mCurSnapData.mSnapTargetIndex == mCurSnapNearestItemIndex)
                {
                    return;
                }
                mCurSnapData.mSnapStatus = SnapStatus.NoTargetSet;
            }
            if (mCurSnapData.mSnapStatus == SnapStatus.SnapMoving)
            {
                if ( (mCurSnapData.mSnapTargetIndex == mCurSnapNearestItemIndex) || mCurSnapData.mIsForceSnapTo)
                {
                    return;
                }
                mCurSnapData.mSnapStatus = SnapStatus.NoTargetSet;
            }
            if (mCurSnapData.mSnapStatus == SnapStatus.NoTargetSet)
            {
                LoopListViewItem2 nearestItem = GetShownItemByItemIndex(mCurSnapNearestItemIndex);
                if (nearestItem == null)
                {
                    return;
                }
                mCurSnapData.mSnapTargetIndex = mCurSnapNearestItemIndex;
                mCurSnapData.mSnapStatus = SnapStatus.TargetHasSet;
                mCurSnapData.mIsForceSnapTo = false;
            }
            if (mCurSnapData.mSnapStatus == SnapStatus.TargetHasSet)
            {
                LoopListViewItem2 targetItem = GetShownItemByItemIndex(mCurSnapData.mSnapTargetIndex);
                if (targetItem == null)
                {
                    mCurSnapData.Clear();
                    return;
                }
                UpdateAllShownItemSnapData();
                mCurSnapData.mTargetSnapVal = targetItem.DistanceWithViewPortSnapCenter;
                mCurSnapData.mCurSnapVal = 0;
                mCurSnapData.mSnapStatus = SnapStatus.SnapMoving;
            }

        }
        //Clear current snap target and then the LoopScrollView2 will auto snap to the CurSnapNearestItemIndex.
        public void ClearSnapData()
        {
            mCurSnapData.Clear();
        }

        public void SetSnapTargetItemIndex(int itemIndex)
        {
            mCurSnapData.mSnapTargetIndex = itemIndex;
            mCurSnapData.mSnapStatus = SnapStatus.TargetHasSet;
            mCurSnapData.mIsForceSnapTo = true;
        }

        //Get the nearest item index with the viewport snap point.
        public int CurSnapNearestItemIndex
        {
            get{ return mCurSnapNearestItemIndex; }
        }

        public void ForceSnapUpdateCheck()
        {
            if(mLeftSnapUpdateExtraCount <= 0)
            {
                mLeftSnapUpdateExtraCount = 1;
            }
        }

        void UpdateSnapHorizontal(bool immediate = false)
        {
            if (mItemSnapEnable == false)
            {
                return;
            }
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            Vector3 pos = mContainerTrans.anchoredPosition3D;
            bool needCheck = (pos.x != mLastSnapCheckPos.x);
            mLastSnapCheckPos = pos;
            if (!needCheck)
            {
                if(mLeftSnapUpdateExtraCount > 0)
                {
                    mLeftSnapUpdateExtraCount--;
                    needCheck = true;
                }
            }
            if (needCheck)
            {
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                int curIndex = -1;
                float start = 0;
                float end = 0;
                float itemSnapCenter = 0;
                float curMinDist = float.MaxValue;
                float curDist = 0;
                float curDistAbs = 0;
                float snapCenter = 0;
                if (mArrangeType == ListItemArrangeType.RightToLeft)
                {
                    snapCenter = -(1 - mViewPortSnapPivot.x) * mViewPortRectTransform.rect.width;
                    Vector3 rightPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[2]);
                    start = rightPos1.x;
                    end = start - tViewItem0.ItemSizeWithPadding;
                    itemSnapCenter = start - tViewItem0.ItemSize * (1 - mItemSnapPivot.x);
                    for (int i = 0; i < count; ++i)
                    {
                        curDist = snapCenter - itemSnapCenter;
                        curDistAbs = Mathf.Abs(curDist);
                        if (curDistAbs < curMinDist)
                        {
                            curMinDist = curDistAbs;
                            curIndex = i;
                        }
                        else
                        {
                            break;
                        }

                        if ((i + 1) < count)
                        {
                            start = end;
                            end = end - mItemList[i + 1].ItemSizeWithPadding;
                            itemSnapCenter = start - mItemList[i + 1].ItemSize * (1 - mItemSnapPivot.x);
                        }
                    }
                }
                else if (mArrangeType == ListItemArrangeType.LeftToRight)
                {
                    snapCenter = mViewPortSnapPivot.x * mViewPortRectTransform.rect.width;
                    Vector3 leftPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                    start = leftPos1.x;
                    end = start + tViewItem0.ItemSizeWithPadding;
                    itemSnapCenter = start + tViewItem0.ItemSize * mItemSnapPivot.x;
                    for (int i = 0; i < count; ++i)
                    {
                        curDist = snapCenter - itemSnapCenter;
                        curDistAbs = Mathf.Abs(curDist);
                        if (curDistAbs < curMinDist)
                        {
                            curMinDist = curDistAbs;
                            curIndex = i;
                        }
                        else
                        {
                            break;
                        }

                        if ((i + 1) < count)
                        {
                            start = end;
                            end = end + mItemList[i + 1].ItemSizeWithPadding;
                            itemSnapCenter = start + mItemList[i + 1].ItemSize * mItemSnapPivot.x;
                        }
                    }
                }


                if (curIndex >= 0)
                {
                    int oldNearestItemIndex = mCurSnapNearestItemIndex;
                    mCurSnapNearestItemIndex = mItemList[curIndex].ItemRenderIndex;
                    if (mItemList[curIndex].ItemRenderIndex != oldNearestItemIndex)
                    {
                        if (mOnSnapNearestChanged != null)
                        {
                            mOnSnapNearestChanged(this, mItemList[curIndex]);
                        }
                    }
                }
                else
                {
                    mCurSnapNearestItemIndex = -1;
                }
            }
            if (CanSnap() == false)
            {
                ClearSnapData();
                return;
            }
            float v = Mathf.Abs(mScrollRect.velocity.x);
            UpdateCurSnapData();
            if(mCurSnapData.mSnapStatus != SnapStatus.SnapMoving)
            {
                return;
            }
            if (v > 0)
            {
                mScrollRect.StopMovement();
            }
            float old = mCurSnapData.mCurSnapVal;
            mCurSnapData.mCurSnapVal = Mathf.SmoothDamp(mCurSnapData.mCurSnapVal, mCurSnapData.mTargetSnapVal, ref mSmoothDumpVel, mSmoothDumpRate);
            float dt = mCurSnapData.mCurSnapVal - old;

            if (immediate || Mathf.Abs(mCurSnapData.mTargetSnapVal - mCurSnapData.mCurSnapVal) < mSnapFinishThreshold)
            {
                pos.x = pos.x + mCurSnapData.mTargetSnapVal - old;
                mCurSnapData.mSnapStatus = SnapStatus.SnapMoveFinish;
                if (mOnSnapItemFinished != null)
                {
                    LoopListViewItem2 targetItem = GetShownItemByItemIndex(mCurSnapNearestItemIndex);
                    if (targetItem != null)
                    {
                        mOnSnapItemFinished(this, targetItem);
                    }
                }
            }
            else
            {
                pos.x = pos.x + dt;
            }
                
            if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                float minX = mViewPortRectLocalCorners[2].x - mContainerTrans.rect.width;
                pos.x = Mathf.Clamp(pos.x, minX, 0);
                mContainerTrans.anchoredPosition3D = pos;
            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                float maxX = mViewPortRectLocalCorners[1].x + mContainerTrans.rect.width;
                pos.x = Mathf.Clamp(pos.x, 0, maxX);
                mContainerTrans.anchoredPosition3D = pos;
            }
        }

        bool CanSnap()
        {
            if (mIsDraging)
            {
                return false;
            }
            if (mScrollBarClickEventListener != null)
            {
                if (mScrollBarClickEventListener.IsPressd)
                {
                    return false;
                }
            }

            if (mIsVertList)
            {
                if(mContainerTrans.rect.height <= ViewPortHeight)
                {
                    return false;
                }
            }
            else
            {
                if (mContainerTrans.rect.width <= ViewPortWidth)
                {
                    return false;
                }
            }

            float v = 0;
            if (mIsVertList)
            {
                v = Mathf.Abs(mScrollRect.velocity.y);
            }
            else
            {
                v = Mathf.Abs(mScrollRect.velocity.x);
            }
            if (v > mSnapVecThreshold)
            {
                return false;
            }
            if (v < 2)
            {
                return true;
            }
            float diff = 3;
            Vector3 pos = mContainerTrans.anchoredPosition3D;
            if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                float minX = mViewPortRectLocalCorners[2].x - mContainerTrans.rect.width;
                if (pos.x < (minX - diff) || pos.x > diff)
                {
                    return false;
                }
            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                float maxX = mViewPortRectLocalCorners[1].x + mContainerTrans.rect.width;
                if (pos.x > (maxX + diff) || pos.x < -diff)
                {
                    return false;
                }
            }
            else if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float maxY = mViewPortRectLocalCorners[0].y + mContainerTrans.rect.height;
                if (pos.y > (maxY + diff) || pos.y < -diff)
                {
                    return false;
                }
            }
            else if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                float minY = mViewPortRectLocalCorners[1].y - mContainerTrans.rect.height;
                if (pos.y < (minY - diff) || pos.y > diff)
                {
                    return false;
                }
            }
            return true;
        }
        public IEnumerator UpdateListViewCoroutne(float distanceForRecycle0, float distanceForRecycle1, float distanceForNew0, float distanceForNew1, bool AlphaEffect)
        {
            mListUpdateCheckFrameCount++;
            if (mIsVertList)
            {
                bool needContinueCheck = true;
                int checkCount = 0;
                int maxCount = 9999;
                while (needContinueCheck)
                {
                    checkCount++;
                    if(checkCount >= maxCount)
                    {
                        Debug.LogError("UpdateListViewCoroutne Vertical while loop " + checkCount + " times! something is wrong!");
                        break;
                    }
                    var hr = UpdateForVertList(distanceForRecycle0, distanceForRecycle1, distanceForNew0, distanceForNew1, AlphaEffect);
                    if(hr == 1 && m_SplitTime > 0)
                    {
                        yield return m_SplitTimeWaitForSeconds;
                    }
                    needContinueCheck = hr != 0;
                }
            }
            else
            {
                bool needContinueCheck = true;
                int checkCount = 0;
                int maxCount = 9999;
                while (needContinueCheck)
                {
                    checkCount++;
                    if (checkCount >= maxCount)
                    {
                        Debug.LogError("UpdateListViewCoroutne  Horizontal while loop " + checkCount + " times! something is wrong!");
                        break;
                    }
                    var hr = UpdateForHorizontalList(distanceForRecycle0, distanceForRecycle1, distanceForNew0, distanceForNew1, AlphaEffect);
                    if (hr == 1 && m_SplitTime > 0)
                    {
                        yield return m_SplitTimeWaitForSeconds;
                    }
                    needContinueCheck = hr != 0;
                }
            }

        }

        public void UpdateListView(float distanceForRecycle0, float distanceForRecycle1, float distanceForNew0, float distanceForNew1, bool AlphaEffect)
        {
            mListUpdateCheckFrameCount++;
            if (mIsVertList)
            {
                bool needContinueCheck = true;
                int checkCount = 0;
                int maxCount = 9999;
                while (needContinueCheck)
                {
                    checkCount++;
                    if (checkCount >= maxCount)
                    {
                        Debug.LogError("UpdateListViewCoroutne Vertical while loop " + checkCount + " times! something is wrong!");
                        break;
                    }
                    var hr = UpdateForVertList(distanceForRecycle0, distanceForRecycle1, distanceForNew0, distanceForNew1, AlphaEffect);
                    needContinueCheck = hr != 0;
                }
            }
            else
            {
                bool needContinueCheck = true;
                int checkCount = 0;
                int maxCount = 9999;
                while (needContinueCheck)
                {
                    checkCount++;
                    if (checkCount >= maxCount)
                    {
                        Debug.LogError("UpdateListViewCoroutne  Horizontal while loop " + checkCount + " times! something is wrong!");
                        break;
                    }
                    var hr = UpdateForHorizontalList(distanceForRecycle0, distanceForRecycle1, distanceForNew0, distanceForNew1, AlphaEffect);
                    needContinueCheck = hr != 0;
                }
            }

        }

        //0表示不需要继续 1表示需要等待的继续 2表示不需要等待的继续
        int UpdateForVertList(float distanceForRecycle0,float distanceForRecycle1,float distanceForNew0, float distanceForNew1, bool AlphaEffect)
        {
            if (mShowItemTotalCount == 0)
            {
                if(mItemList.Count > 0)
                {
                    RecycleAllItem();
                }
                return 0;
            }
            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                int itemListCount = mItemList.Count;
                if (itemListCount == 0)
                {
                    float curY = mContainerTrans.anchoredPosition3D.y;
                    if (curY < 0)
                    {
                        curY = 0;
                    }
                    int index = 0;
                    float pos = -curY;
                    if (mSupportScrollBar)
                    {
                        GetPlusItemIndexAndPosAtGivenPos(curY, ref index, ref pos);
                        pos = -pos;
                    }
                    m_NewItemStyle = NewItemStyle.AddTail;
                    LoopListViewItem2 newItem = GetNewItemByIndex(index, AlphaEffect);
                    if (newItem == null)
                    {
                        return 0;
                    }
                    if (mSupportScrollBar)
                    {
                        SetItemSize(index, newItem.CachedRectTransform.rect.height, newItem.Padding);
                    }
                    //mItemList.Add(newItem);
                    newItem.CachedRectTransform.anchoredPosition3D = new Vector3(newItem.StartPosOffset, pos, 0);
                    UpdateContentSize();
                    return 1;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 topPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 downPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);

                if (!mIsDraging && tViewItem0.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                    && downPos0.y - mViewPortRectLocalCorners[1].y > distanceForRecycle0)
                {
                    RemoveItem(0);
                    RecycleItemTmp(tViewItem0);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return 2;
                }

                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 topPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 downPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
                if (!mIsDraging && tViewItem1.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                    && mViewPortRectLocalCorners[0].y - topPos1.y > distanceForRecycle1)
                {
                    RemoveItem(mItemList.Count - 1);
                    RecycleItemTmp(tViewItem1);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return 2;
                }



                if (mViewPortRectLocalCorners[0].y - downPos1.y < distanceForNew1)
                {
                    if(tViewItem1.ItemRenderIndex > mCurReadyMaxItemIndex)
                    {
                        mCurReadyMaxItemIndex = tViewItem1.ItemRenderIndex;
                        mNeedCheckNextMaxItem = true;
                    }
                    int nIndex = tViewItem1.ItemRenderIndex + 1;
                    if (nIndex <= mCurReadyMaxItemIndex || mNeedCheckNextMaxItem)
                    {
                        m_NewItemStyle = NewItemStyle.AddTail;
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex, AlphaEffect);
                        if (newItem == null)
                        {
                            mCurReadyMaxItemIndex = tViewItem1.ItemRenderIndex;
                            mNeedCheckNextMaxItem = false;
                            CheckIfNeedUpdataItemPos();
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            //mItemList.Add(newItem);
                            float y = tViewItem1.CachedRectTransform.anchoredPosition3D.y - tViewItem1.CachedRectTransform.rect.height - tViewItem1.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(newItem.StartPosOffset, y, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();

                            if (nIndex > mCurReadyMaxItemIndex)
                            {
                                mCurReadyMaxItemIndex = nIndex;
                            }
                            return 1;
                        }
                        
                    }

                }

                if (topPos0.y - mViewPortRectLocalCorners[1].y < distanceForNew0)
                {
                    if(tViewItem0.ItemRenderIndex < mCurReadyMinItemIndex)
                    {
                        mCurReadyMinItemIndex = tViewItem0.ItemRenderIndex;
                        mNeedCheckNextMinItem = true;
                    }
                    int nIndex = tViewItem0.ItemRenderIndex - 1;
                    if (nIndex >= mCurReadyMinItemIndex || mNeedCheckNextMinItem)
                    {
                        m_NewItemStyle = NewItemStyle.InsertHead;
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex, AlphaEffect);
                        if (newItem == null)
                        {
                            mCurReadyMinItemIndex = tViewItem0.ItemRenderIndex;
                            mNeedCheckNextMinItem = false;
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            //mItemList.Insert(0, newItem);
                            float y = tViewItem0.CachedRectTransform.anchoredPosition3D.y + newItem.CachedRectTransform.rect.height + newItem.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(newItem.StartPosOffset, y, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();
                            if (nIndex < mCurReadyMinItemIndex)
                            {
                                mCurReadyMinItemIndex = nIndex;
                            }
                            return 1;
                        }
                        
                    }

                }

            }
            else
            {
                
                if (mItemList.Count == 0)
                {
                    float curY = mContainerTrans.anchoredPosition3D.y;
                    if (curY > 0)
                    {
                        curY = 0;
                    }
                    int index = 0;
                    float pos = -curY;
                    if (mSupportScrollBar)
                    {
                        GetPlusItemIndexAndPosAtGivenPos(-curY, ref index, ref pos);
                    }
                    m_NewItemStyle = NewItemStyle.AddTail;
                    LoopListViewItem2 newItem = GetNewItemByIndex(index, AlphaEffect);
                    if (newItem == null)
                    {
                        return 0;
                    }
                    if (mSupportScrollBar)
                    {
                        SetItemSize(index, newItem.CachedRectTransform.rect.height, newItem.Padding);
                    }
                    //mItemList.Add(newItem);
                    newItem.CachedRectTransform.anchoredPosition3D = new Vector3(newItem.StartPosOffset, pos, 0);
                    UpdateContentSize();
                    return 1;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 topPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 downPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);

                if (!mIsDraging && tViewItem0.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                    && mViewPortRectLocalCorners[0].y - topPos0.y > distanceForRecycle0)
                {
                    RemoveItem(0);
                    RecycleItemTmp(tViewItem0);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return 2;
                }

                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 topPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 downPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
                if (!mIsDraging && tViewItem1.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                     && downPos1.y - mViewPortRectLocalCorners[1].y > distanceForRecycle1)
                {
                    RemoveItem(mItemList.Count - 1);
                    RecycleItemTmp(tViewItem1);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return 2;
                }

                if (topPos1.y - mViewPortRectLocalCorners[1].y < distanceForNew1)
                {
                    if (tViewItem1.ItemRenderIndex > mCurReadyMaxItemIndex)
                    {
                        mCurReadyMaxItemIndex = tViewItem1.ItemRenderIndex;
                        mNeedCheckNextMaxItem = true;
                    }
                    int nIndex = tViewItem1.ItemRenderIndex + 1;
                    if (nIndex <= mCurReadyMaxItemIndex || mNeedCheckNextMaxItem)
                    {
                        m_NewItemStyle = NewItemStyle.AddTail;
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex, AlphaEffect);
                        if (newItem == null)
                        {
                            mNeedCheckNextMaxItem = false;
                            CheckIfNeedUpdataItemPos();
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            //mItemList.Add(newItem);
                            float y = tViewItem1.CachedRectTransform.anchoredPosition3D.y + tViewItem1.CachedRectTransform.rect.height + tViewItem1.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(newItem.StartPosOffset, y, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();
                            if (nIndex > mCurReadyMaxItemIndex)
                            {
                                mCurReadyMaxItemIndex = nIndex;
                            }
                            return 1;
                        }
                        
                    }

                }


                if (mViewPortRectLocalCorners[0].y - downPos0.y < distanceForNew0)
                {
                    if (tViewItem0.ItemRenderIndex < mCurReadyMinItemIndex)
                    {
                        mCurReadyMinItemIndex = tViewItem0.ItemRenderIndex;
                        mNeedCheckNextMinItem = true;
                    }
                    int nIndex = tViewItem0.ItemRenderIndex - 1;
                    if (nIndex >= mCurReadyMinItemIndex || mNeedCheckNextMinItem)
                    {
                        m_NewItemStyle = NewItemStyle.InsertHead;
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex, AlphaEffect);
                        if (newItem == null)
                        {
                            mNeedCheckNextMinItem = false;
                            return 0;
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            //mItemList.Insert(0, newItem);
                            float y = tViewItem0.CachedRectTransform.anchoredPosition3D.y - newItem.CachedRectTransform.rect.height - newItem.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(newItem.StartPosOffset, y, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();
                            if (nIndex < mCurReadyMinItemIndex)
                            {
                                mCurReadyMinItemIndex = nIndex;
                            }
                            return 1;
                        }
                        
                    }
                }


            }

            return 0;

        }
        //0表示不需要继续 1表示需要等待的继续 2表示不需要等待的继续
        int UpdateForHorizontalList(float distanceForRecycle0, float distanceForRecycle1, float distanceForNew0, float distanceForNew1, bool AlphaEffect)
        {
            if (mShowItemTotalCount == 0)
            {
                if (mItemList.Count > 0)
                {
                    RecycleAllItem();
                }
                return 0;
            }
            if (mArrangeType == ListItemArrangeType.LeftToRight)
            {

                if (mItemList.Count == 0)
                {
                    float curX = mContainerTrans.anchoredPosition3D.x;
                    if (curX > 0)
                    {
                        curX = 0;
                    }
                    int index = 0;
                    float pos = -curX;
                    if (mSupportScrollBar)
                    {
                        GetPlusItemIndexAndPosAtGivenPos(-curX, ref index, ref pos);
                    }
                    m_NewItemStyle = NewItemStyle.AddTail;
                    LoopListViewItem2 newItem = GetNewItemByIndex(index, AlphaEffect);
                    if (newItem == null)
                    {
                        return 0;
                    }
                    if (mSupportScrollBar)
                    {
                        SetItemSize(index, newItem.CachedRectTransform.rect.width, newItem.Padding);
                    }
                    //mItemList.Add(newItem);
                    newItem.CachedRectTransform.anchoredPosition3D = new Vector3(pos, newItem.StartPosOffset, 0);
                    UpdateContentSize();
                    return 1;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 leftPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 rightPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[2]);

                if (!mIsDraging && tViewItem0.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                    && mViewPortRectLocalCorners[1].x - rightPos0.x > distanceForRecycle0)
                {
                    RemoveItem(0);
                    RecycleItemTmp(tViewItem0);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return 2;
                }

                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 leftPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 rightPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[2]);
                if (!mIsDraging && tViewItem1.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                    && leftPos1.x - mViewPortRectLocalCorners[2].x> distanceForRecycle1)
                {
                    RemoveItem(mItemList.Count - 1);
                    RecycleItemTmp(tViewItem1);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return 2;
                }



                if (rightPos1.x - mViewPortRectLocalCorners[2].x < distanceForNew1)
                {
                    if (tViewItem1.ItemRenderIndex > mCurReadyMaxItemIndex)
                    {
                        mCurReadyMaxItemIndex = tViewItem1.ItemRenderIndex;
                        mNeedCheckNextMaxItem = true;
                    }
                    int nIndex = tViewItem1.ItemRenderIndex + 1;
                    if (nIndex <= mCurReadyMaxItemIndex || mNeedCheckNextMaxItem)
                    {
                        m_NewItemStyle = NewItemStyle.AddTail;
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex, AlphaEffect);
                        if (newItem == null)
                        {
                            mCurReadyMaxItemIndex = tViewItem1.ItemRenderIndex;
                            mNeedCheckNextMaxItem = false;
                            CheckIfNeedUpdataItemPos();
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                            }
                            //mItemList.Add(newItem);
                            float x = tViewItem1.CachedRectTransform.anchoredPosition3D.x + tViewItem1.CachedRectTransform.rect.width + tViewItem1.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(x, newItem.StartPosOffset, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();

                            if (nIndex > mCurReadyMaxItemIndex)
                            {
                                mCurReadyMaxItemIndex = nIndex;
                            }
                            return 1;
                        }
                    }
                }

                if ( mViewPortRectLocalCorners[1].x - leftPos0.x < distanceForNew0)
                {
                    if (tViewItem0.ItemRenderIndex < mCurReadyMinItemIndex)
                    {
                        mCurReadyMinItemIndex = tViewItem0.ItemRenderIndex;
                        mNeedCheckNextMinItem = true;
                    }
                    int nIndex = tViewItem0.ItemRenderIndex - 1;
                    if (nIndex >= mCurReadyMinItemIndex || mNeedCheckNextMinItem)
                    {
                        m_NewItemStyle = NewItemStyle.InsertHead;
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex, AlphaEffect);
                        if (newItem == null)
                        {
                            mCurReadyMinItemIndex = tViewItem0.ItemRenderIndex;
                            mNeedCheckNextMinItem = false;
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                            }
                            //mItemList.Insert(0, newItem);
                            float x = tViewItem0.CachedRectTransform.anchoredPosition3D.x - newItem.CachedRectTransform.rect.width - newItem.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(x, newItem.StartPosOffset, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();
                            if (nIndex < mCurReadyMinItemIndex)
                            {
                                mCurReadyMinItemIndex = nIndex;
                            }
                            return 1;
                        }
                    }
                }
            }
            else
            {

                if (mItemList.Count == 0)
                {
                    float curX = mContainerTrans.anchoredPosition3D.x;
                    if (curX < 0)
                    {
                        curX = 0;
                    }
                    int index = 0;
                    float pos = -curX;
                    if (mSupportScrollBar)
                    {
                        GetPlusItemIndexAndPosAtGivenPos(curX, ref index, ref pos);
                        pos = -pos;
                    }
                    m_NewItemStyle = NewItemStyle.AddTail;
                    LoopListViewItem2 newItem = GetNewItemByIndex(index, AlphaEffect);
                    if (newItem == null)
                    {
                        return 0;
                    }
                    if (mSupportScrollBar)
                    {
                        SetItemSize(index, newItem.CachedRectTransform.rect.width, newItem.Padding);
                    }
                    //mItemList.Add(newItem);
                    newItem.CachedRectTransform.anchoredPosition3D = new Vector3(pos, newItem.StartPosOffset, 0);
                    UpdateContentSize();
                    return 1;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 leftPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 rightPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[2]);

                if (!mIsDraging && tViewItem0.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                    && leftPos0.x - mViewPortRectLocalCorners[2].x > distanceForRecycle0)
                {
                    RemoveItem(0);
                    RecycleItemTmp(tViewItem0);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return 2;
                }

                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 leftPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 rightPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[2]);
                if (!mIsDraging && tViewItem1.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                    && mViewPortRectLocalCorners[1].x - rightPos1.x > distanceForRecycle1)
                {
                    RemoveItem(mItemList.Count - 1);
                    RecycleItemTmp(tViewItem1);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return 2;
                }
                if (mViewPortRectLocalCorners[1].x - leftPos1.x  < distanceForNew1)
                {
                    if (tViewItem1.ItemRenderIndex > mCurReadyMaxItemIndex)
                    {
                        mCurReadyMaxItemIndex = tViewItem1.ItemRenderIndex;
                        mNeedCheckNextMaxItem = true;
                    }
                    int nIndex = tViewItem1.ItemRenderIndex + 1;
                    if (nIndex <= mCurReadyMaxItemIndex || mNeedCheckNextMaxItem)
                    {
                        m_NewItemStyle = NewItemStyle.AddTail;
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex, AlphaEffect);
                        if (newItem == null)
                        {
                            mCurReadyMaxItemIndex = tViewItem1.ItemRenderIndex;
                            mNeedCheckNextMaxItem = false;
                            CheckIfNeedUpdataItemPos();
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                            }
                            //mItemList.Add(newItem);
                            float x = tViewItem1.CachedRectTransform.anchoredPosition3D.x - tViewItem1.CachedRectTransform.rect.width - tViewItem1.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(x, newItem.StartPosOffset, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();

                            if (nIndex > mCurReadyMaxItemIndex)
                            {
                                mCurReadyMaxItemIndex = nIndex;
                            }
                            return 1;
                        }
                    }
                }

                if (rightPos0.x - mViewPortRectLocalCorners[2].x < distanceForNew0)
                {
                    if (tViewItem0.ItemRenderIndex < mCurReadyMinItemIndex)
                    {
                        mCurReadyMinItemIndex = tViewItem0.ItemRenderIndex;
                        mNeedCheckNextMinItem = true;
                    }
                    int nIndex = tViewItem0.ItemRenderIndex - 1;
                    if (nIndex >= mCurReadyMinItemIndex || mNeedCheckNextMinItem)
                    {
                        m_NewItemStyle = NewItemStyle.InsertHead;
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex, AlphaEffect);
                        if (newItem == null)
                        {
                            mCurReadyMinItemIndex = tViewItem0.ItemRenderIndex;
                            mNeedCheckNextMinItem = false;
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                            }
                            //mItemList.Insert(0, newItem);
                            float x = tViewItem0.CachedRectTransform.anchoredPosition3D.x + newItem.CachedRectTransform.rect.width + newItem.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(x, newItem.StartPosOffset, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();
                            if (nIndex < mCurReadyMinItemIndex)
                            {
                                mCurReadyMinItemIndex = nIndex;
                            }
                            return 1;
                        }
                    }
                }
            }
            return 0;
        }
        float GetContentPanelSize()
        {
            if (mSupportScrollBar)
            {
                float tTotalSize = mItemPosMgr.mTotalSize > 0 ? (mItemPosMgr.mTotalSize - mLastItemPadding) : 0;
                if(tTotalSize < 0)
                {
                    tTotalSize = 0;
                }
                return tTotalSize;
            }
            int count = mItemList.Count;
            if (count == 0)
            {
                return 0;
            }
            if (count == 1)
            {
                return mItemList[0].ItemSize;
            }
            if (count == 2)
            {
                return mItemList[0].ItemSizeWithPadding + mItemList[1].ItemSize;
            }
            float s = 0;
            for (int i = 0; i < count - 1; ++i)
            {
                s += mItemList[i].ItemSizeWithPadding;
            }
            s += mItemList[count - 1].ItemSize;
            return s;
        }


        void CheckIfNeedUpdataItemPos()
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                LoopListViewItem2 firstItem = mItemList[0];
                LoopListViewItem2 lastItem = mItemList[mItemList.Count - 1];
                float viewMaxY = GetContentPanelSize();
                if (firstItem.TopY > 0 || (firstItem.ItemRenderIndex == mCurReadyMinItemIndex && firstItem.TopY != 0))
                {
                    UpdateAllShownItemsPos();
                    return;
                }
                if ((-lastItem.BottomY) > viewMaxY || (lastItem.ItemRenderIndex == mCurReadyMaxItemIndex && (-lastItem.BottomY) != viewMaxY))
                {
                    UpdateAllShownItemsPos();
                    return;
                }

            }
            else if (mArrangeType == ListItemArrangeType.BottomToTop)
            {
                LoopListViewItem2 firstItem = mItemList[0];
                LoopListViewItem2 lastItem = mItemList[mItemList.Count - 1];
                float viewMaxY = GetContentPanelSize();
                if (firstItem.BottomY < 0 || (firstItem.ItemRenderIndex == mCurReadyMinItemIndex && firstItem.BottomY != 0))
                {
                    UpdateAllShownItemsPos();
                    return;
                }
                if (lastItem.TopY > viewMaxY || (lastItem.ItemRenderIndex == mCurReadyMaxItemIndex && lastItem.TopY != viewMaxY))
                {
                    UpdateAllShownItemsPos();
                    return;
                }
            }
            else if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                LoopListViewItem2 firstItem = mItemList[0];
                LoopListViewItem2 lastItem = mItemList[mItemList.Count - 1];
                float viewMaxX = GetContentPanelSize();
                if (firstItem.LeftX < 0 || (firstItem.ItemRenderIndex == mCurReadyMinItemIndex && firstItem.LeftX != 0))
                {
                    UpdateAllShownItemsPos();
                    return;
                }
                if ((lastItem.RightX) > viewMaxX || (lastItem.ItemRenderIndex == mCurReadyMaxItemIndex && lastItem.RightX != viewMaxX))
                {
                    UpdateAllShownItemsPos();
                    return;
                }

            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                LoopListViewItem2 firstItem = mItemList[0];
                LoopListViewItem2 lastItem = mItemList[mItemList.Count - 1];
                float viewMaxX = GetContentPanelSize();
                if (firstItem.RightX > 0 || (firstItem.ItemRenderIndex == mCurReadyMinItemIndex && firstItem.RightX != 0))
                {
                    UpdateAllShownItemsPos();
                    return;
                }
                if ((-lastItem.LeftX) > viewMaxX || (lastItem.ItemRenderIndex == mCurReadyMaxItemIndex && (-lastItem.LeftX) != viewMaxX))
                {
                    UpdateAllShownItemsPos();
                    return;
                }

            }

        }


        void UpdateAllShownItemsPos()
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }

            mAdjustedVec = (mContainerTrans.anchoredPosition3D - mLastFrameContainerPos) / Time.deltaTime;

            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float pos = 0;
                if (mSupportScrollBar)
                {
                    pos = -GetItemPos(mItemList[0].ItemRenderIndex);
                }
                float pos1 = mItemList[0].CachedRectTransform.anchoredPosition3D.y;
                float d = pos - pos1;
                float curY = pos;
                for (int i = 0; i < count; ++i)
                {
                    LoopListViewItem2 item = mItemList[i];
                    item.CachedRectTransform.anchoredPosition3D = new Vector3(item.StartPosOffset, curY, 0);
                    curY = curY - item.CachedRectTransform.rect.height - item.Padding;
                }
                if(d != 0)
                {
                    Vector2 p = mContainerTrans.anchoredPosition3D;
                    p.y = p.y - d;
                    mContainerTrans.anchoredPosition3D = p;
                }
                
            }
            else if(mArrangeType == ListItemArrangeType.BottomToTop)
            {
                float pos = 0;
                if (mSupportScrollBar)
                {
                    pos = GetItemPos(mItemList[0].ItemRenderIndex);
                }
                float pos1 = mItemList[0].CachedRectTransform.anchoredPosition3D.y;
                float d = pos - pos1;
                float curY = pos;
                for (int i = 0; i < count; ++i)
                {
                    LoopListViewItem2 item = mItemList[i];
                    item.CachedRectTransform.anchoredPosition3D = new Vector3(item.StartPosOffset, curY, 0);
                    curY = curY + item.CachedRectTransform.rect.height + item.Padding;
                }
                if(d != 0)
                {
                    Vector3 p = mContainerTrans.anchoredPosition3D;
                    p.y = p.y - d;
                    mContainerTrans.anchoredPosition3D = p;
                }
            }
            else if (mArrangeType == ListItemArrangeType.LeftToRight)
            {
                float pos = 0;
                if (mSupportScrollBar)
                {
                    pos = GetItemPos(mItemList[0].ItemRenderIndex);
                }
                float pos1 = mItemList[0].CachedRectTransform.anchoredPosition3D.x;
                float d = pos - pos1;
                float curX = pos;
                for (int i = 0; i < count; ++i)
                {
                    LoopListViewItem2 item = mItemList[i];
                    item.CachedRectTransform.anchoredPosition3D = new Vector3(curX, item.StartPosOffset, 0);
                    curX = curX + item.CachedRectTransform.rect.width + item.Padding;
                }
                if (d != 0)
                {
                    Vector3 p = mContainerTrans.anchoredPosition3D;
                    p.x = p.x - d;
                    mContainerTrans.anchoredPosition3D = p;
                }

            }
            else if (mArrangeType == ListItemArrangeType.RightToLeft)
            {
                float pos = 0;
                if (mSupportScrollBar)
                {
                    pos = -GetItemPos(mItemList[0].ItemRenderIndex);
                }
                float pos1 = mItemList[0].CachedRectTransform.anchoredPosition3D.x;
                float d = pos - pos1;
                float curX = pos;
                for (int i = 0; i < count; ++i)
                {
                    LoopListViewItem2 item = mItemList[i];
                    item.CachedRectTransform.anchoredPosition3D = new Vector3(curX, item.StartPosOffset, 0);
                    curX = curX - item.CachedRectTransform.rect.width - item.Padding;
                }
                if (d != 0)
                {
                    Vector3 p = mContainerTrans.anchoredPosition3D;
                    p.x = p.x - d;
                    mContainerTrans.anchoredPosition3D = p;
                }

            }
            if (mIsDraging)
            {
                mScrollRect.OnBeginDrag(mPointerEventData);
                mScrollRect.Rebuild(CanvasUpdate.PostLayout);
                mScrollRect.velocity = mAdjustedVec;
                mNeedAdjustVec = true;
            }
        }
        void UpdateContentSize()
        {
            float size = GetContentPanelSize();
            if (mIsVertList)
            {
                if(mContainerTrans.rect.height != size)
                {
                    mContainerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                }
            }
            else
            {
                if(mContainerTrans.rect.width != size)
                {
                    mContainerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                }
            }
        }
    }

}
