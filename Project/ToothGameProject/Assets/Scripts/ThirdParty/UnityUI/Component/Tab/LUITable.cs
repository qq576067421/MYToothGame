using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityUI
{


    public class LUITable : MonoBehaviour
    {
        [SerializeField]
        private List<LUITableTitle> m_Titles = new List<LUITableTitle>();
        public System.Action<LUITableTitle> OnClickedTitleCall = null;

        private LUITableTitle m_LastTitle = null;

        private void Start()
        {
            InitTitles();
        }
        public List<LUITableTitle> GetTitles()
        {
            return m_Titles;
        }
        public void SetTitles(List<LUITableTitle> titles)
        {
            m_Titles = titles;
            InitTitles();
        }
        private void InitTitles()
        {
            int count = m_Titles.Count;
            for(int i=0;i<count;++i)
            {
                var title = m_Titles[i];
                if(title != null)
                {
                    title.m_Index = i;
                }
            }
        }
        public void SetClickedTitle(int index)
        {
            if(m_Titles == null || m_Titles.Count <= index || index < 0)
            {
                return;
            }
            var title = m_Titles[index];
            if(m_LastTitle != null)
            {
                if(m_LastTitle != title)
                {
                    m_LastTitle.OnTitleChecked(false);
                }
                if(title != null)
                {
                    title.OnTitleChecked(true);
                }
                m_LastTitle = title;
            }
            if(OnClickedTitleCall != null)
            {
                OnClickedTitleCall(title);
            }

        }
    }

}