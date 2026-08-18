using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeLightParams : MonoBehaviour
{
    public float m_Intensity = 1.2f;
    public float m_Intensity2 = 1.35f;

    public Color m_Color = Color.white;
    
    public Light m_Light;
    public bool m_IsBake = true;

    private void Awake()
    {
        if(m_Light != null)
        {
            m_Light.intensity = m_Intensity;
            m_Light.color = m_Color;
        }
    }
}
