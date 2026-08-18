using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileMover : MonoBehaviour
{
    private static readonly Vector3 HiddenPosition = new Vector3(0, 100, 0);
    private bool m_IsFollow;
    private Vector3 m_FollowTargetPosition;
    public void SetIsFollow(bool isFollow)
    {
        m_IsFollow = isFollow;
        if (isFollow)
        {
            m_TargetPositionDirty = true;
        }
    }
    public void SetFollowTargetPosition(Vector3 pos)
    {
        m_FollowTargetPosition = pos;
        m_TargetPositionDirty = true;
    }
    private bool m_TargetPositionDirty = false;

    public float m_HitOffset = 0f;
    public bool m_UseFirePointRotation;
    public GameObject m_HitEff;
    private ParticleSystem m_HitPS;
    public float m_HitShowTime = 0.1f;
    private float m_HitShow = 0;
    public GameObject m_FlashEff;
    private ParticleSystem m_FlashPS;
    public float m_FlashShowTime = 0.1f;
    private float m_FlashShow = 0;

    public bool m_MainParticleUseConfig = true;
    public ParticleSystem m_MainParticleSystem;
    public List<TrailRenderer> m_MainTrails;
    //仅仅是特效
    public ParticleSystem m_EffParticleSystem;


    public float m_DelayHideProjectileTime = 1.0f;
    private float m_DelayHideProjectileLeftTime = 0;
    private bool m_Pooled = false;
    private bool m_DeactivateGameObjectAfterStop = false;
    private ParticleSystem.Particle[] m_MainParticles = null;
    private Coroutine m_EmitCoroutine;


    public void Init(Transform parent)
    {
        SetParent(gameObject, parent);

        SetParent(m_HitEff, parent);
        if (m_HitEff != null)
        {
            m_HitPS = m_HitEff.GetComponentInChildren<ParticleSystem>();
            m_HitPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        SetParent(m_FlashEff, parent);
        if (m_FlashEff != null)
        {
            m_FlashPS = m_FlashEff.GetComponentInChildren<ParticleSystem>();
            m_FlashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        m_Pooled = true;
    }
    private void SetParent(GameObject go, Transform parent)
    {
        if(go != null)
        {
            var trans =  go.transform;
            trans.SetParent(parent);
            trans.position = HiddenPosition;
        }
    }

    private void StopEmitCoroutine()
    {
        if (m_EmitCoroutine != null)
        {
            StopCoroutine(m_EmitCoroutine);
            m_EmitCoroutine = null;
        }
    }

    private static void StopAndClearParticle(ParticleSystem ps)
    {
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void SetTrailState(bool enabled, bool emitting, bool clear)
    {
        if (m_MainTrails == null)
        {
            return;
        }

        foreach (var trail in m_MainTrails)
        {
            if (trail == null)
            {
                continue;
            }

            trail.enabled = enabled;
            trail.emitting = emitting;
            if (clear)
            {
                trail.Clear();
            }
        }
    }

    public void PrepareForEmit()
    {
        StopEmitCoroutine();
        m_Pooled = false;
        m_DelayHideProjectileLeftTime = 0f;
        m_DeactivateGameObjectAfterStop = false;
        StopAndClearParticle(m_MainParticleSystem);
        StopAndClearParticle(m_EffParticleSystem);
        SetTrailState(false, false, true);
    }

    public void StopEmit()
    {
        StopEmitCoroutine();
        m_Pooled = true;
        m_DelayHideProjectileLeftTime = 0f;
        StopAndClearParticle(m_MainParticleSystem);
        StopAndClearParticle(m_EffParticleSystem);
        SetTrailState(false, false, true);
        transform.position = HiddenPosition;
        if (m_DeactivateGameObjectAfterStop && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }

        m_DeactivateGameObjectAfterStop = false;
    }

    private void Update()
    {
        if(m_FlashShow > 0)
        {
            m_FlashShow -= Time.deltaTime;
            if(m_FlashShow <= 0)
            {
                if(m_FlashEff != null)
                {
                    m_FlashEff.transform.position = HiddenPosition;
                }
                if(m_FlashPS != null && !m_FlashPS.isStopped)
                {
                    m_FlashPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        if(m_HitShow > 0)
        {
            m_HitShow -= Time.deltaTime;
            if(m_HitShow <= 0)
            {
                if(m_HitEff != null)
                {
                    m_HitEff.transform.position = HiddenPosition;
                }
                if(m_HitPS != null && !m_HitPS.isStopped)
                {
                    m_HitPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
        bool NeedFollow = false;
        if(m_Pooled)
        {
            if(m_DelayHideProjectileLeftTime > 0)
            {
                m_DelayHideProjectileLeftTime -= Time.deltaTime;
                if(m_DelayHideProjectileLeftTime <= 0)
                {
                    StopEmit();
                }
            }

        }
        else
        {
            NeedFollow = true;
        }

        if(m_TargetPositionDirty && !m_Pooled)
        {
            m_TargetPositionDirty = false;
            Follow();
        }
        else
        {
            if(m_IsFollow && NeedFollow)
            {
                Follow();
            }
        }
    }

    private void Follow()
    {
        if(m_MainParticleSystem == null)
        {
            return;
        }
        var mainModule = m_MainParticleSystem.main;
        if (m_MainParticles == null)
        {
            m_MainParticles = new ParticleSystem.Particle[mainModule.maxParticles];
        }
        var followPos = m_FollowTargetPosition;
        int particleCount = m_MainParticleSystem.GetParticles(m_MainParticles);
        if (particleCount <= 0)
        {
            return;
        }

        float followSpeed = m_MainParticleSystem.startSpeed;
        float arrivalDistance = Mathf.Max(0.01f, followSpeed * Time.deltaTime);
        float arrivalDistanceSqr = arrivalDistance * arrivalDistance;
        for (int i = 0; i < particleCount; i++)
        {
            var particle = m_MainParticles[i];
            Vector3 toTarget = followPos - particle.position;
            float sqrDistanceToTarget = toTarget.sqrMagnitude;
            if (sqrDistanceToTarget <= arrivalDistanceSqr)
            {
                particle.position = followPos;
                particle.velocity = Vector3.zero;
            }
            else
            {
                Vector3 directionToTarget = toTarget / Mathf.Sqrt(sqrDistanceToTarget);
                particle.velocity = directionToTarget * followSpeed;
            }

            m_MainParticles[i] = particle;
        }

        m_MainParticleSystem.SetParticles(m_MainParticles, particleCount);
    }
    public static float m_SpeedFix = 1.0f;
    public void Emit(float speed)
    {
        PrepareForEmit();
        m_EmitCoroutine = StartCoroutine(EmitImp(speed));

    }

    private IEnumerator EmitImp(float speed)
    {
        yield return new WaitForEndOfFrame();

        //Debug.LogError("emit:" + m_FollowTargetPosition);
        if (m_FlashEff != null)
        {
            var trans = m_FlashEff.transform;
            trans.forward = gameObject.transform.forward;
            trans.position = transform.position;
            m_FlashShow = m_FlashShowTime;
            if (m_FlashPS != null)
            {
                m_FlashPS.Simulate(0, true, true);
                m_FlashPS.Play();
            }
        }
        m_Pooled = false;
        if (m_MainParticleSystem != null)
        {
            m_MainParticleSystem.Simulate(0.0f, true, true);
            m_MainParticleSystem.Play(true);
            //m_MainParticleSystem.startSpeed = speed * m_SpeedFix;
            //m_MainParticleSystem.startSpeed = 0;
            if (!m_MainParticleUseConfig)
            {
                m_MainParticleSystem.Emit(1);
            }
            else
            {
                m_MainParticleSystem.Emit(m_MainParticleSystem.main.maxParticles);
            }


            if (m_MainParticles == null)
            {
                m_MainParticles = new ParticleSystem.Particle[m_MainParticleSystem.main.maxParticles];
            }

            SetTrailState(true, true, true);

        }
        if (m_EffParticleSystem != null)
        {
            m_EffParticleSystem.Simulate(0.0f, true, true);
            m_EffParticleSystem.Play();
            m_EffParticleSystem.Emit(m_EffParticleSystem.main.maxParticles);
        }

        m_EmitCoroutine = null;
    }

    public void DelayHide()
    {
        DelayHide(false);
    }

    public void DelayHide(bool deactivateGameObjectAfterDelay)
    {
        m_DeactivateGameObjectAfterStop = deactivateGameObjectAfterDelay;
        if(m_Pooled)
        {
            if (m_DelayHideProjectileLeftTime <= 0f)
            {
                StopEmit();
            }
            return;
        }

        m_Pooled = true;
        m_DelayHideProjectileLeftTime = m_DelayHideProjectileTime;
    
    }

    public bool ReadCanReuseFromPool()
    {
        return m_Pooled && m_DelayHideProjectileLeftTime <= 0f && m_EmitCoroutine == null;
    }

    public void ForceStopForReuse()
    {
        m_DeactivateGameObjectAfterStop = false;
        StopEmit();
    }

    public void DestroyMover()
    {
        if(m_HitEff != null)
        {
            GameObject.Destroy(m_HitEff, m_HitShowTime);
            m_HitEff = null;
        }
        if(m_FlashEff != null)
        {
            GameObject.Destroy(m_FlashEff, m_FlashShowTime);
            m_FlashEff = null;
        }
    }

    //https ://docs.unity3d.com/ScriptReference/Rigidbody.OnCollisionEnter.html
    public void Boom(Vector3 hitPos, Vector3 normal)
    {

        m_DelayHideProjectileLeftTime = m_DelayHideProjectileTime;
        if(m_MainParticleSystem != null && m_MainParticles != null)
        {
            m_MainParticleSystem.GetParticles(m_MainParticles);
            int p_count = m_MainParticles.Length;
            for(int i =0; i < p_count; ++i)
            {
                var p = m_MainParticles[i];
                p.velocity = Vector3.zero;
                m_MainParticles[i] = p;
            }
            m_MainParticleSystem.SetParticles(m_MainParticles);
        }
        m_Pooled = true;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
        Vector3 pos = hitPos + normal * m_HitOffset;

        if (m_HitEff != null)
        {
            var trans = m_HitEff.transform;
            trans.position = pos;
            if (m_UseFirePointRotation)
            {
                trans.rotation = gameObject.transform.rotation * Quaternion.Euler(0, 180f, 0);
            }
            else
            {
                trans.LookAt(hitPos + normal);
            }
            m_HitShow = m_HitShowTime;

            if (m_HitPS != null)
            {
                m_HitPS.Simulate(0, true, true);
                m_HitPS.Play();
            }
        }
    }
}
