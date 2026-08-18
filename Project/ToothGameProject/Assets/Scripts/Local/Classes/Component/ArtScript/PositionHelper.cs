using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionHelper : MonoBehaviour
{
    [SerializeField]
    private string m_PointName = string.Empty;

    public string PointName => m_PointName;

    public void SetPointName(string pointName)
    {
        m_PointName = pointName;
        SyncName();
    }

    private void Awake()
    {
        SyncName();
    }

    private void OnValidate()
    {
        SyncName();
    }

    private void SyncName()
    {
        if (!string.IsNullOrEmpty(m_PointName) && name != m_PointName)
        {
            name = m_PointName;
        }
    }
}
