using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUI
{
    public class UICircleLayoutGroup : LayoutGroup
    {
        public int m_Radius = 165;
        public float m_Sapce = 45;
        public float m_StartAxis = 0;
        public bool m_RotateChild = true;
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
        }


        public override void CalculateLayoutInputVertical()
        {
            this.Calcal();
        }

        private void Calcal()
        {
            Vector3 pos = Vector3.zero;
            Vector3 starVec = Vector3.up * (float)this.m_Radius + pos;
            int childCount = base.rectChildren.Count;

            float startAxis = 0;
            if (childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.MiddleRight)
            {
                //居中
                startAxis = this.m_StartAxis - 1.0f * (childCount - 1) * this.m_Sapce/ 360.0f / 2;
            }
            else
            {
                startAxis = this.m_StartAxis;
            }
            
            for (int i = 0; i < childCount; i++)
            {
                Quaternion qua = Quaternion.AngleAxis((float)i * this.m_Sapce + startAxis * 360f, Vector3.forward);
                Vector3 point = qua * starVec;
                base.rectChildren[i].anchoredPosition = point;
                if (m_RotateChild)
                {
                    base.rectChildren[i].localEulerAngles = new Vector3(0, 0, (float)i * this.m_Sapce - startAxis * 360f);
                }
            }
            for (int j = 0; j < base.rectChildren.Count; j++)
            {
                base.rectChildren[j].anchorMin = new Vector2(0.5f, 0.5f);
                base.rectChildren[j].anchorMax = new Vector2(0.5f, 0.5f);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void Start()
        {
            base.Start();
        }

        private void Update()
        {
        }


        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
        }


    }
}