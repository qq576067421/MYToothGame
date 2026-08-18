using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameDll;
using UnityUI;

namespace GameHot
{
    public class tip_model : WindowModel
    {
        public List<string> m_CacheTips = new List<string>();

        public override void Clear()
        {
            m_CacheTips.Clear();
        }
    }
    public class tip_wnd : WindowBase
    {
        private static tip_wnd m_Instance;
        public static tip_wnd GetInstance()
        {
            if(m_Instance == null)
            {
                m_Instance = UIManager.OpenWindowEX<tip_wnd>(null);
            }
            return m_Instance;
        }
        public static bool HasWnd()
        {
            return m_Instance != null;
        }
        private v_tip_wnd m_View = null;
        //TipCtrl m_Msgs = null;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Top;
            __CustomUIPrefabDir = UIPrefabDirs.common;
            __CreateModel(new tip_model());
        }
        protected override void OnInitComponent()
        {
            m_View = new v_tip_wnd();
            m_View.InitComponent(__GetWindowObj());
            //m_Msgs = (TipCtrl)m_View.m_msgs.GetComponent(typeof(TipCtrl));
        }

        public void OnShowTip(string tip)
        {
            var model = GetModel<tip_model>();
            model.m_CacheTips.Add(tip);

            if(IsInitializedView())
            {
                TipDisplay();
            }

        }

        private void TipDisplay()
        {
            var model = GetModel<tip_model>();
            var tips = model.m_CacheTips;
            if (tips.Count > 0)
            {
                int count = tips.Count;
                for (int i = 0; i < count; ++i)
                {
                    //m_Msgs.AddTips(tips[i]);
                }
                tips.Clear();
            }
        }

        public void OnShowTipLanId(string lanId, params object[] param)
        {
            var tip = RenderAPI.GetTextByLanId(lanId, param);
            var model = GetModel<tip_model>();
            model.m_CacheTips.Add(tip);

            if (IsInitializedView())
            {
                TipDisplay();
            }
        }
        public void OnShowTipLanId(string lanId)
        {
            var tip = RenderAPI.GetTextByLanId(lanId);
            var model = GetModel<tip_model>();
            model.m_CacheTips.Add(tip);

            if (IsInitializedView())
            {
                TipDisplay();
            }
        }

        protected override void OnOpen()
        {
            TipDisplay();
            //BRenderEvent.Event.OnShowTip += OnShowTip;
            //BRenderEvent.Event.OnShowTipLanId += OnShowTipLanId;
            //BRenderEvent.Event.OnShowBattleTipById += OnShowBattleChatTipById;
            //BRenderEvent.Event.OnShowBattleTip += OnShowBattleChatTip;
        }
        protected override void OnClose()
        {
            m_Instance = null;
            //BRenderEvent.Event.OnShowBattleTipById -= OnShowBattleChatTipById;
            //BRenderEvent.Event.OnShowTip -= OnShowTip;
            //BRenderEvent.Event.OnShowTipLanId -= OnShowTipLanId;
            //BRenderEvent.Event.OnShowBattleTip -= OnShowBattleChatTip;
        }

        public void OnShowBattleChatTip(string info)
        {
            //if (LobbyPlayer.GetInstance().IsLobbyPlayerState())
            //{
            //    LobbyPlayer.ChatMgr.AddSystemChat(info);
            //}
            //else
            //{
            //    CGameProcedure.Event.OnFightSystemChat(info);
            //}
        }

        public void OnShowBattleChatTipById(string obj)
        {
            //var info = RenderAPI.GetTextByLanId(obj);
            //if (LobbyPlayer.GetInstance().IsLobbyPlayerState())
            //{
            //    LobbyPlayer.ChatMgr.AddSystemChat(info);
            //}
            //else
            //{
            //    CGameProcedure.Event.OnFightSystemChat(info);
            //}
        }

        protected override void OnDestroy()
        {
            //m_Msgs = null;
            m_Instance = null;
        }
    }
}
