using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityUI
{
    //这个控件的主要作用是用来标示一个预制件是程序那边需要用的，用于控制例如缩放、位置、隐藏等
    public class UIArray : MonoBehaviour
    {
        public Component m_Template;
        public List<Component> m_Items = new List<Component>();

#if UNITY_EDITOR
        [ContextMenu("排序")]
        private void SortComponents()
        {
            List<Component> coms = new List<Component>();
            List<Component> other_coms = new List<Component>();
            foreach (var com in m_Items)
            {
                if (com.transform.parent == transform)
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
            coms.AddRange(other_coms);
            m_Items = coms;
        }

        [ContextMenu("自动添加子对象")]
        private void AddChild()
        {
            int count = transform.childCount;
            List<Component> coms = new List<Component>();
            for(int i =0; i<count; ++i)
            {
                var child = transform.GetChild(i);
                coms.Add(child);
            }
            m_Items = coms;
        }
#endif
    }

}