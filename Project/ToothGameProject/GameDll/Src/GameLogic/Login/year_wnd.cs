using System;
using System.Collections.Generic;
using MonoBean;
using UnityEngine.InputSystem;
using GameDll;
using System.Diagnostics.SymbolStore;
using UnityEngine.UI;
using UnityEngine;

namespace GameHot
{
    public class year_wnd : WindowBase
    {
        private v_year_wnd m_View;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __ParticipateCurrentActiveWindow = true;
            __CustomUIPrefabDir = UIPrefabDirs.login;
        }
        private void OnClickClose()
        {
            UIManager.CloseWindow(this);
        }
        protected override void OnInitComponent()
        {
            m_View = new v_year_wnd();
            m_View.InitComponent(__GetWindowObj());

            RenderAPI.AddButtonClick(m_View.m_btnReturn, OnClickClose);
            OnInitLanguage();
        }
        protected override void OnOpen()
        {
            CGameProcedure.Event.OnEscapPressed += CloseYearWindow;
            // 窗口激活后再排一次，覆盖初始化时物体未激活导致布局被跳过的情况。
            RebuildInstructionLayout();
        }
        protected override void OnClose()
        {
            CGameProcedure.Event.OnEscapPressed -= CloseYearWindow;
        }
        private void OnInitLanguage()
        {
            RenderAPI.SetTextLan(m_View.m_Instruction_title, "td_login_year_Instruction_title");
            RenderAPI.SetTextLan(m_View.m_Instruction_1, "td_login_year_Instruction_1");
            RenderAPI.SetTextLan(m_View.m_Instruction_2, "td_login_year_Instruction_2");
            RenderAPI.SetTextLan(m_View.m_Instruction_3, "td_login_year_Instruction_3");
            RenderAPI.SetTextLan(m_View.m_Instruction_4, "td_login_year_Instruction_4");
            RenderAPI.SetTextLan(m_View.m_Instruction_5, "td_login_year_Instruction_5");

            // Content高度由文本自适应，不再按语言写死（prefab里没配fitter，运行时补一个）。
            var fitter = m_View.m_Content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = m_View.m_Content.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RebuildInstructionLayout();
        }
        private void RebuildInstructionLayout()
        {
            // 布局系统默认帧末才重算；同步强制重排，保证打开当帧文本和Content高度就是最终值。
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                m_View.m_Content.GetComponent<RectTransform>());
        }
        private void CloseYearWindow(InputAction.CallbackContext context)
        {
            UIManager.CloseWindow(this);
        }
    }
}
