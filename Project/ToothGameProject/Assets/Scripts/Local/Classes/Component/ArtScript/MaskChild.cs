using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class MaskChild : MonoBehaviour
{
    public MaskEx m_Mask;
    protected void Start()
    {
        if (m_Mask == null)
        {
            m_Mask = GetComponentInParent<MaskEx>();
        }
        if (m_Mask != null)
        {
            Mask();
        }
    }

    public void Mask()
    {
        if (m_Mask != null)
        {
            m_Mask.MaskChild(this.gameObject);
        }
    }
}