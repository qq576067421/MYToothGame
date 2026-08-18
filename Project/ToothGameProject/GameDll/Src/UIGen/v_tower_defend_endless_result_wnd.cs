using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;

namespace GameHot
{
    public class v_tower_defend_endless_result_wnd : v_base_wnd
    {
        public object m_UserData;
        public ComponentBridge m_Bridge;

        public TMP_Text m_txt_title;
        public TMP_Text m_txt_wave;
        public TMP_Text m_txt_best_wave;
        public TMP_Text m_txt_kill_summary;
        public TMP_Text m_txt_kill_detail;
        public TMP_Text m_txt_rank_status;
        public TMP_Text m_txt_reward_kill;
        public TMP_Text m_txt_reward_total;
        public Button m_btn_continue;
        public Button m_btn_return;

        public override void InitComponent(GameObject go)
        {
            m_Bridge = go.GetComponent(typeof(ComponentBridge)) as ComponentBridge;
            m_txt_title = m_Bridge.GetControl(0) as TMP_Text;
            m_txt_wave = m_Bridge.GetControl(1) as TMP_Text;
            m_txt_best_wave = m_Bridge.GetControl(2) as TMP_Text;
            m_txt_kill_summary = m_Bridge.GetControl(3) as TMP_Text;
            m_txt_kill_detail = m_Bridge.GetControl(4) as TMP_Text;
            m_txt_rank_status = m_Bridge.GetControl(5) as TMP_Text;
            m_txt_reward_kill = m_Bridge.GetControl(6) as TMP_Text;
            m_txt_reward_total = m_Bridge.GetControl(7) as TMP_Text;
            m_btn_continue = m_Bridge.GetControl(8) as Button;
            m_btn_return = m_Bridge.GetControl(9) as Button;
        }
    }
}
