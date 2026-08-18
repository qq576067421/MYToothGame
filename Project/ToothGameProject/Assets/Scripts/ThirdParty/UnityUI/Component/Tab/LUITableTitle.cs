using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

namespace UnityUI
{


    public class LUITableTitle : MonoBehaviour
    {
        public int m_Index;
        public Button m_Button;
        public GameObject m_UnChecked;
        public GameObject m_Checked;
        public LUITable m_Table;

        // Use this for initialization
        void Start()
        {
            if(m_Button != null)
            {
                m_Button.onClick.RemoveAllListeners();
                m_Button.onClick.AddListener(OnTitleClick);
            }
        }

        private void OnTitleClick()
        {
            if(m_Table != null)
            {
                m_Table.SetClickedTitle(this.m_Index);
            }
        }

        public void OnTitleChecked(bool check)
        {
            if(m_UnChecked != null)
            {
                m_UnChecked.SetActive(check == false);
            }
            if(m_Checked != null)
            {
                m_Checked.SetActive(check == true);
            }
        }
    }

}