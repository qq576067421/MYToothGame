using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityUI
{
    //这个控件的主要作用是用来标示一个预制件是程序那边需要用的，用于控制例如缩放、位置、隐藏等
    //该版本，用于有多个模板的情况，结构为，item_array上面挂该脚本，然后下面节点分别是layout、模板0、模板1.....
    public class UIArray2 : MonoBehaviour
    {
        public Component[] m_Templates;
        public GridLayoutGroup m_Layout;
        public List<Component> m_Items = new List<Component>();
        private void Start()
        {
            if (m_Templates != null)
            {
                foreach (var item in m_Templates)
                {
                    item.gameObject.SetActive(false);
                }
            }

        }
        public void ClearArray()
        {
            var coms = this.m_Items;
            foreach (var com in coms)
            {
                GameObject.Destroy(com.gameObject);
            }
            coms.Clear();
        }
        public Component AddItem(int type)
        {
            if (type >= m_Templates.Length)
            {
                Debug.LogError("ui array type error:" + type);
            }

            var clone = UnityEngine.Object.Instantiate(m_Templates[type], m_Layout.transform);
            clone.gameObject.SetActive(true);
            m_Items.Add(clone);
            return clone;
        }
        public void RemoveIem(int idx)
        {
            int index = 0;
            var coms = this.m_Items;
            foreach (var com in coms)
            {
                if (index == idx)
                {
                    GameObject.Destroy(com.gameObject);
                    break;
                }
            }
        }
        public void FillArray(List<int> types)
        {
            var coms = this.m_Items;
            foreach (var com in coms)
            {
                GameObject.Destroy(com.gameObject);
            }
            coms.Clear();

            foreach (var type in types)
            {
                if (type >= m_Templates.Length)
                {
                    Debug.LogError("ui array type error:" + type);
                    continue;
                }

                var clone = UnityEngine.Object.Instantiate(m_Templates[type], m_Layout.transform);
                clone.gameObject.SetActive(true);
                m_Items.Add(clone);
            }

        }
    }

}