using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUI
{
    //这个控件的主要作用是用来标示一个预制件是程序那边需要用的，用于控制例如缩放、位置、隐藏等
    public class UIUpdateLayout : MonoBehaviour
    {
        public RectTransform m_Layout;
        public bool m_EnableUpate = false;
        public void StartUpdate()
        {
            m_EnableUpate = true;
        }
        public void StopUpdate()
        {
            m_EnableUpate = false;
        }
        private void Update()
        {
            if(m_EnableUpate)
            {
                UpdateLayout();
            }
        }
        public void UpdateLayout()
        {
            if(m_Layout != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(m_Layout);
                //LayoutRebuilder.ForceRebuildLayoutImmediate(m_Layout);
            }
        }
    }

}