using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityUI
{
    public class LUIToggle:Toggle
    {
        [HideInInspector]
        public float Cooldown = 0.1f;

        protected float m_LastClickTime = 0;



        public override void OnPointerClick(PointerEventData eventData)
        {
            if(Time.realtimeSinceStartup - m_LastClickTime >= Cooldown)
            {
                m_LastClickTime = Time.realtimeSinceStartup;
                base.OnPointerClick(eventData);
            }

        }

        private static MethodInfo m_SetMethod = null;
        public static void SetIsOn(Toggle self, bool isToggle, bool sendCallback)
        {
            if(m_SetMethod == null)
            {
                Type type = typeof(Toggle);
                m_SetMethod = type.GetMethod("Set", new Type[] {typeof(bool), typeof(bool)});
            }

            m_SetMethod.Invoke(self,new object[] { isToggle, sendCallback });
        }
    }
}
