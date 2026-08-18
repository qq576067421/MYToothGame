using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

namespace UnityUI
{
    //lcl 2017年1月16日19:09:05
    //桥接窗口组件和代码
    public class ComponentBridge : MonoBehaviour
    {
        [SerializeField]
        public string m_BridgeName = "";
        [SerializeField]
        private List<Component> m_ListComponent = new List<Component>();

        public Component GetControl(int index)
        {
            if (index >= 0 && index < m_ListComponent.Count)
            {
                Component tr = m_ListComponent[index];
                return tr;
            }
            else
            {
                return null;
            }
        }
        public List<Component> GetAllComponents()
        {
            return m_ListComponent;
        }

#if UNITY_EDITOR
        [ContextMenu("排序")]
        private void SortComponents()
        {
            List<Component> coms = new List<Component>();
            List<Component> other_coms = new List<Component>();
            foreach(var com in m_ListComponent)
            {
                if(com.transform.parent == transform)
                {
                    coms.Add(com);
                }
                else
                {
                    other_coms.Add(com);
                }
            }
            coms.Sort((a, b) =>
            {
                int aindex = a.transform.GetSiblingIndex();
                int bindex = b.transform.GetSiblingIndex();
                return aindex.CompareTo(bindex);
            });

            m_ListComponent.Clear();
            m_ListComponent.AddRange(coms);
            m_ListComponent.AddRange(other_coms);
        }
#endif
    }
}