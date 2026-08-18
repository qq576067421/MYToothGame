using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;

namespace GameHot
{
    public class v_tower_defend_pause_wnd : v_base_wnd
    {
        public object m_UserData;
        public ComponentBridge m_Bridge;

        public TMP_Text m_txt_title;
        public TMP_Text m_txt_summary;
        public Button m_btn_resume;
        public Button m_btn_restart;
        public Button m_btn_return;

        public override void InitComponent(GameObject go)
        {
            m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
            m_txt_title = m_Bridge.GetControl(0) as TMP_Text;
            m_txt_summary = m_Bridge.GetControl(1) as TMP_Text;
            m_btn_resume = m_Bridge.GetControl(2) as Button;
            m_btn_restart = m_Bridge.GetControl(3) as Button;
            m_btn_return = m_Bridge.GetControl(4) as Button;
        }
    }
}
