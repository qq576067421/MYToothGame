using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityUI
{
    public class LUIButton:Button,IPointerDownHandler
    {
        public string StyleName = "normal";
        [HideInInspector]
        public GameObject ChooseState;
        private bool m_IsChooseState = false;
        [HideInInspector]
        public float Cooldown = 0.1f;
        public Action OnClickDownCall = null;

        protected float m_LastClickTime = 0;

        public void SetAsChooseState(bool isChoose)
        {
            if(ChooseState != null && !ChooseState.Equals(null))
            {
                ChooseState.SetActive(isChoose);
            }
            m_IsChooseState = isChoose;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if(OnClickDownCall != null)
            {
                OnClickDownCall();
            }
        }
        public override void OnPointerClick(PointerEventData eventData)
        {
            if(Time.realtimeSinceStartup - m_LastClickTime >= Cooldown)
            {
                m_LastClickTime = Time.realtimeSinceStartup;
                base.OnPointerClick(eventData);
            }

        }

        public void StartCooldown()
        {
            m_LastClickTime = Time.realtimeSinceStartup;
        }
    }
}
