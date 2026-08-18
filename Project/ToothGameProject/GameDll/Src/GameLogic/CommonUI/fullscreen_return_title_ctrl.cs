using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameDll;

namespace GameHot
{
    public class fullscreen_return_title_ctrl
    {
        private WindowBase m_Wnd;
        private v_fullscreen_return_title m_View;
        
        public void InitComponent(WindowBase win, UnityEngine.GameObject obj)
        {
            m_Wnd = win;
            m_View = new v_fullscreen_return_title();
            m_View.InitComponent(obj);
        }
        public UnityUI.LUIButton GetCloseButton()
        {
            return m_View.m_btnClose;
        }
        public void AddCloseClick(Action onCloseCall)
        {
            Action _call = onCloseCall;
            RenderAPI.AddButtonClick(m_View.m_btnClose, _call );
        }
        public void SetTitleLanId(string titleLan, params object[] param)
        {
            RenderAPI.SetTextLan(m_View.m_txtTitle, titleLan, param);
        }
    }
}
