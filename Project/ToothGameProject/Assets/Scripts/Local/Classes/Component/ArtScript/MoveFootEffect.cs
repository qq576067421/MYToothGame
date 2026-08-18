using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveFootEffect : MonoBehaviour
{
    public GameObject m_Effect;
    public float m_PerDistance = 0.5f;
    public Vector3 m_LastPosition = Vector3.zero;
    public float m_HideTime = 2.0f;
    public Vector3 m_PositionOffset = new Vector3(0, 0.2f, 0);

    private List<GameObject> m_ShowingEffs = new List<GameObject>();
    private List<float> m_ShowingTimes = new List<float>();

    private List<GameObject> m_HideEffs = new List<GameObject>();
    private Transform m_Transform;
    private void Start()
    {
        m_Transform = transform;
        m_Effect.SetActive(false);
    }
    private void OnDestroy()
    {
        foreach(var obj in m_ShowingEffs)
        {
            GameObject.Destroy(obj);
        }
        m_ShowingEffs.Clear();
        foreach(var obj in m_HideEffs)
        {
            GameObject.Destroy(obj);
        }
        m_HideEffs.Clear();
    }
    void Update()
    {
        var pos = m_Transform.position;
        var dist = Vector3.Distance(pos, m_LastPosition);
        if(dist >= m_PerDistance)
        {
            if(m_HideEffs.Count > 0)
            {
                var eff = m_HideEffs[0];
                eff.SetActive(true);
                eff.transform.position = pos + m_PositionOffset;
                m_HideEffs.RemoveAt(0);
                m_ShowingEffs.Add(eff);
                m_ShowingTimes.Add(Time.realtimeSinceStartup);
            }
            else
            {
                var eff = (GameObject) GameObject.Instantiate(m_Effect);
                eff.SetActive(true);
                eff.transform.position = pos + m_PositionOffset;
                m_ShowingEffs.Add(eff);
                m_ShowingTimes.Add(Time.realtimeSinceStartup);
            }
			m_LastPosition = pos;
        }

        if(m_ShowingEffs.Count > 0)
        {
            var time = m_ShowingTimes[0];
            if(Time.realtimeSinceStartup - time >= m_HideTime)
            {
                var eff = m_ShowingEffs[0];
                eff.SetActive(false);
                m_HideEffs.Add(eff);

                m_ShowingEffs.RemoveAt(0);
                m_ShowingTimes.RemoveAt(0);
            }
        }
    }
}
