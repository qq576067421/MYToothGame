using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCL
{
    [ExecuteInEditMode]
    public class DestroyableCtrl : MonoBehaviour
    {

        [Tooltip("使用 open 动画作为开启 或者 物理炸开")]
        public bool m_UseAniOpen = false;
        [Tooltip("开启前播放 shake 效果")]
        public bool m_UseShake = false;

        [Tooltip("使用脚本产生Idle和Shake效果")]
        public bool m_UseScript = false;
        public ShakeableCtrl m_IdleShake;
        public ShakeableCtrl m_ShakeShake;

        public Animation m_Animation;
        public string m_IdleAniName = "idle";
        public string m_OpenAniName = "open";
        public string m_ShakeAniName = "shake";

        public ParticleSystem m_NormalParticle;
        public ParticleSystem m_OpenParticle;

        public float m_PlayShakeTime = 1.0f;

        public GameObject m_DestroyPieceRoot;
        public GameObject m_ExplosionCenter;
        public float m_ExplosionForce = 20;
        public float m_ExplosionRadius = 3;
        public Collider m_SelfCollider = null;


        public GameObject m_NormalObject;
        public bool m_DestroyHideNormal = true;

        private List<Vector3> m_PiecesOldPosition = new List<Vector3>();



        private void Start()
        {
            PlayIdle();
        }

        public void PlayIdle()
        {
            if (m_SelfCollider != null)
            {
                m_SelfCollider.enabled = true;
            }
            if (m_OpenParticle != null && m_OpenParticle.gameObject.activeSelf)
            {
                m_OpenParticle.gameObject.SetActive(false);
            }
            if (m_DestroyPieceRoot != null && m_DestroyPieceRoot.activeSelf)
            {
                m_DestroyPieceRoot.SetActive(false);

                if (!m_UseAniOpen && m_PiecesOldPosition.Count > 0)
                {
                    var rigs = m_DestroyPieceRoot.GetComponentsInChildren<Rigidbody>();
                    int count = rigs.Length;
                    for (int i = 0; i < count; ++i)
                    {
                        var rig = rigs[i];
                        rig.transform.localPosition = m_PiecesOldPosition[i];
                        rig.velocity = Vector3.zero;
                        rig.angularVelocity = Vector3.zero;
                    }
                }
            }
            if (m_NormalObject != null && !m_NormalObject.activeSelf)
            {
                m_NormalObject.SetActive(true);
            }
            if (m_NormalParticle != null && !m_NormalParticle.gameObject.activeSelf)
            {
                m_NormalParticle.gameObject.SetActive(true);
                m_NormalParticle.Simulate(0, true);
                m_NormalParticle.Play(true);
            }
            if (m_UseScript)
            {
                if (m_IdleShake != null)
                {
                    m_IdleShake.gameObject.SetActive(true);
                }
                if (m_ShakeShake != null)
                {
                    m_ShakeShake.gameObject.SetActive(false);
                }
                if (m_Animation != null)
                {
                    m_Animation.enabled = false;
                }
            }
            else
            {
                if (m_IdleShake != null && m_IdleShake.gameObject.activeSelf)
                {
                    m_IdleShake.gameObject.SetActive(false);
                }
                if (m_ShakeShake != null && m_ShakeShake.gameObject.activeSelf)
                {
                    m_ShakeShake.gameObject.SetActive(false);
                }
                if (m_Animation != null && !string.IsNullOrEmpty(m_IdleAniName))
                {
                    m_Animation.enabled = true;
                    m_Animation.Play(m_IdleAniName);
                }
            }

        }
        public void PlayOpen()
        {
            if (m_SelfCollider != null)
            {
                m_SelfCollider.enabled = false;
            }
            if (m_UseShake)
            {
                if (m_UseScript)
                {
                    if (m_IdleShake != null)
                    {
                        m_IdleShake.gameObject.SetActive(false);
                    }
                    if (m_ShakeShake != null)
                    {
                        m_ShakeShake.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (m_Animation != null && !string.IsNullOrEmpty(m_ShakeAniName))
                    {
                        m_Animation.Play(m_ShakeAniName);
                    }
                }
                StartCoroutine(OnPlayShake());
            }
            else
            {
                OnPlayOpen();
            }
        }

        private IEnumerator OnPlayShake()
        {
            yield return new WaitForSeconds(m_PlayShakeTime);
            OnPlayOpen();
        }

        private void OnPlayOpen()
        {
            if (m_ShakeShake != null && m_ShakeShake.gameObject.activeSelf)
            {
                m_ShakeShake.gameObject.SetActive(false);
            }
            if (m_OpenParticle != null && !m_OpenParticle.gameObject.activeSelf)
            {
                m_OpenParticle.gameObject.SetActive(true);
                m_OpenParticle.Simulate(0, true);
                m_OpenParticle.Play(true);
            }
            if (m_DestroyPieceRoot != null && !m_DestroyPieceRoot.activeSelf)
            {
                m_DestroyPieceRoot.SetActive(true);

                if (m_UseAniOpen)
                {
                    m_Animation.enabled = true;
                    m_Animation.Play(m_OpenAniName);
                }
                else
                {
                    m_Animation.enabled = false;
                    var rigs = m_DestroyPieceRoot.GetComponentsInChildren<Rigidbody>();
                    bool collectOldPos = m_PiecesOldPosition.Count == 0;

                    int count = rigs.Length;
                    for (int i = 0; i < count; ++i)
                    {
                        var rig = rigs[i];
                        if (collectOldPos)
                        {
                            var pos = rig.transform.localPosition;
                            var rot = rig.transform.localEulerAngles;
                            m_PiecesOldPosition.Add(pos);
                        }
                        else
                        {
                            rig.transform.localPosition = m_PiecesOldPosition[i];
                        }
                        rig.transform.rotation = UnityEngine.Random.rotation;
                        //if (i % 5 == 0)
                        //{
                        //    rig.AddExplosionForce(m_ExplosionForce, 
                        //        m_ExplosionCenter.transform.position, m_ExplosionRadius);
                        //}
                    }
                }


            }
            if (m_NormalObject != null && m_NormalObject.activeSelf)
            {
                if (m_DestroyHideNormal)
                {
                    m_NormalObject.SetActive(false);
                }
            }

            if (m_NormalParticle != null && !m_NormalParticle.gameObject.activeSelf)
            {
                if (m_DestroyHideNormal)
                {
                    m_NormalParticle.gameObject.SetActive(false);
                }
            }
        }

        public bool m_TestIdle;

        public bool m_TestOpen;


        private void Update()
        {
            if (m_TestIdle)
            {
                PlayIdle();
                m_TestIdle = false;
            }

            if (m_TestOpen)
            {
                PlayOpen();
                m_TestOpen = false;
            }
        }

    }
}