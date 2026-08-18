using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnityUI
{
    [RequireComponent(typeof(Graphic))]
    public class UIDropOn : MonoBehaviour
    {
        public object m_UserData;
        public Graphic m_Graphic;
        public void SetRaycastTarget(bool isEnable)
        {
            if(m_Graphic != null)
            {
                m_Graphic.raycastTarget = isEnable;
            }
        }
    }
}