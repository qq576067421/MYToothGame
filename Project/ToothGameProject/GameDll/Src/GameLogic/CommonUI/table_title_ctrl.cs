using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityUI;
using GameDll;

namespace GameHot
{
    public class table_title_ctrl
    {
        private List<string> m_TabTitles;
        private UIArray m_Array;
        private List<v_tap_panel.v_title_item> m_TitleItems = new List<v_tap_panel.v_title_item>();
        public void InitTitles(UISubWindow tap_panel,List<string> titles)
        {
            m_TabTitles = titles;
            v_tap_panel panel = new v_tap_panel();
            panel.InitComponent(tap_panel.gameObject);
            m_Array = panel.m_table_titles;
            RenderAPI.UIArrayCopy(m_Array, m_TabTitles.Count);
        }
        public void OnShowTableTitle(int index, Action<int> call)
        {
            Action<int> _call = call;
            var items = m_Array.m_Items;
            int count = m_TabTitles.Count;
            for (int i = 0; i < count; ++i)
            {
                var item = items[i];
                v_tap_panel.v_title_item ti = new v_tap_panel.v_title_item();
                ti.InitComponent(item.gameObject);
                RenderAPI.SetActive(ti.m_actived, index == i);
                RenderAPI.SetActive(ti.m_btn, index != i);
                int click_idx = i;
                RenderAPI.AddButtonClick(ti.m_btn, () =>
                {
                    _call(click_idx);
                });
                //if (index == i)
                //{
                //    RenderAPI.SetText(ti.m_txtTitleNormal, "<color=#ffffff>" + m_TabTitles[i] + "</color>");
                //}
                //else
                //{
                //    RenderAPI.SetText(ti.m_txtTitleNormal, "<color=#4AACF7>" + m_TabTitles[i] + "</color>");
                //}
                RenderAPI.SetText(ti.m_txtTitle,  m_TabTitles[i]);
                RenderAPI.SetText(ti.m_txtTitleActived,  m_TabTitles[i]);
                m_TitleItems.Add(ti);
            }
        }
        public v_tap_panel.v_title_item GetTitle(int index)
        {
            if(index < 0 || index >= m_TitleItems.Count)
            {
                return null;
            }
            else
            {
                return m_TitleItems[index];
            }
        }
        public void HideAllExcept(int index)
        {
            int count = m_Array.m_Items.Count;
            for(int i = 0; i < count; ++i)
            {
                RenderAPI.SetActive(m_Array.m_Items[i].gameObject, i == index);
            }
        }
        public void HideTitle(int index)
        {
            RenderAPI.SetActive(m_Array.m_Items[index].gameObject, false);
        }
        public int GetTitleCount()
        {
            return m_TabTitles.Count;
        }
    }
}
