using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


public class PluginsTool
{
    private static Camera m_UICamera;
    public static Camera UICamera
    {
        get
        {
            if (m_UICamera == null)
            {
                GameObject camerago  = GameObject.FindGameObjectWithTag("UICamera");
                if (camerago != null)
                {
                    m_UICamera = camerago.GetComponent<Camera>();
                }
                return m_UICamera;
            }
            return m_UICamera;
        }
    }
}

