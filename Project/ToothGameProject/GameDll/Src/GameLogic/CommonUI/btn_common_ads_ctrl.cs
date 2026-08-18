//using System;
using GameDll;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GameHot
//{
//    public class btn_common_ads_ctrl
//    {
//        private WindowBase m_Wnd;
//        private v_btn_common_ads m_View;
        
//        public void InitComponent(WindowBase win, UnityEngine.GameObject obj)
//        {
//            m_Wnd = win;
//            m_View = new v_btn_common_ads();
//            m_View.InitComponent(obj);
//        }

//        public void AddClick(string normal_title, Action on_normal, string ads_title, Action on_ads)
//        {
//            RenderAPI.SetText(m_View.m_txt, normal_title);
//            RenderAPI.SetText(m_View.m_txtads, ads_title);

//            var ncall = on_normal;
//            var acall = on_ads;
//            RenderAPI.AddButtonClick(m_View.m_btnUp, ncall);
//            RenderAPI.AddButtonClick(m_View.m_btnUpAds, acall);
//            var gameFee = LobbyMessage.GetInstance().GetGameFee();
//            if(gameFee == GameFee.Ads)
//            {
//                RenderAPI.SetActive(m_View.m_btnUpAds, true);
//            }
//            else
//            {
//                RenderAPI.SetActive(m_View.m_btnUpAds, false);
//            }

//        }
//    }
//}
