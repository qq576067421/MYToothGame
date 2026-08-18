using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCL
{
    public class ShakeableCtrl : MonoBehaviour
    {
        public GameObject m_ShakeObj;
        public Vector3 shakeRate = new Vector3(0.1f, 0.1f, 0.1f);
        public float shakeTime = 0.5f;
        public float shakeDertaTime = 0.1f;
        public bool loop = false;
        public bool playOnEnable = false;

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Shake();
            }
        }

        public void Shake()
        {
            StartCoroutine(Shake_Coroutine());
        }

        public IEnumerator Shake_Coroutine()
        {
            var oriPosition = m_ShakeObj.transform.position;
            for (float i = 0; i < shakeTime; i += shakeDertaTime)
            {
                m_ShakeObj.transform.position = oriPosition +
                    Random.Range(-shakeRate.x, shakeRate.x) * Vector3.right +
                    Random.Range(-shakeRate.y, shakeRate.y) * Vector3.up +
                    Random.Range(-shakeRate.z, shakeRate.z) * Vector3.forward;
                yield return new WaitForSeconds(shakeDertaTime);
            }
            m_ShakeObj.transform.position = oriPosition;

            if (loop)
            {
                Shake();
            }
        }
    }
}