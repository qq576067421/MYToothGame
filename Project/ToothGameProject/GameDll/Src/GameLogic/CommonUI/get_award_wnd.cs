using System;
using System.Collections.Generic;
using MonoBean;
using GameDll;

namespace GameHot
{

    public class get_award_model : WindowModel
    {
        public Action m_CloseAwardCall;
        //public List<Packet_Item> m_Awards = null;

        public override void Clear()
        {
            m_CloseAwardCall = null;
            //m_Awards = null;
        }
    }
    public class get_award_wnd : WindowBase
    {
        private v_get_award_wnd m_View;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.common;
            __CreateModel(new get_award_model());
        }
        private void OnClickClose()
        {
            var model = GetModel<get_award_model>();
            model.m_CloseAwardCall?.Invoke();
            UIManager.CloseWindow(this);
        }

        public void SetCloseAwardCall(Action call)
        {
            var model = GetModel<get_award_model>();
            model.m_CloseAwardCall = call;
        }
        //public void SetAwards(List<Packet_Item> items)
        //{
        //    var model = GetModel<get_award_model>();
        //    model.m_Awards = items;

        //    if(IsInitializedView())
        //    {
        //        RefreshAwardsDisplay();
        //    }
        //}
        protected override void OnInitComponent()
        {
            m_View = new v_get_award_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_lcl_btnClose, OnClickClose);

            dialog_titlebar.QuickCreateTitleBar(this, m_View.m_dialog_titlebar.gameObject,
OnClickClose, RenderAPI.GetTextByLanId("reward")); 

            RenderAPI.AddButtonClick(m_View.m_lcl_btnOk, () =>
            {
                var model = GetModel<get_award_model>();
                model.m_CloseAwardCall?.Invoke();
                UIManager.CloseWindow(this);
            });
        }
        private void RefreshAwardsDisplay()
        {
            //var awards = GetModel<get_award_model>().m_Awards;
            //int count = awards.Count;

            //RenderAPI.UIArrayCopy(m_View.m_Content, count);

            //for (int i = 0; i < count; ++i)
            //{
            //    var cellCom = m_View.m_Content.m_Items[i];

            //    v_reward_item cell = new v_reward_item();
            //    cell.InitComponent(cellCom.gameObject);

            //    var item = awards[i];
            //    RenderAPI.SetText(cell.m_num, item.m_count.ToString());
            //    var item_cfg = t_itemBean.GetConfig(item.m_cfg_id);
            //    if (item_cfg != null)
            //    {
            //        __SetImage(cell.m_icon, item_cfg.t_icon);
            //        __SetImage(cell.m_frame, LogicAPI.GetQualityFrame(item_cfg.t_quality));
            //    }

            //    RenderAPI.AddButtonClick(cell.m_btn, () =>
            //    {
            //        LogicAPI.OnShowMyselfItem(item.m_cfg_id, item.m_level, ItemLookStyle.Just);
            //    });

            //}
        }

        protected override void OnOpen()
        {
            RefreshAwardsDisplay();
        }
        protected override void OnClose()
        {

        }
    }
}
