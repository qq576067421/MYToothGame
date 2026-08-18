using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace LCL
{
    public class FrameUpdate : MonoBehaviour
    {
        public void NextFrameCall(Action call)
        {
            StartCoroutine(OnNextFrameCall(call));
        }
        private Action m_FrameCall;
        public void SetFrameCall(Action call)
        {
            m_FrameCall = call;
        }
        private IEnumerator OnNextFrameCall(Action call)
        {
            yield return new WaitForEndOfFrame();
            if(call != null)
            {
                call();
            }
        }

        void Update()
        {
            if(m_FrameCall != null)
            {
                m_FrameCall();
            }
        }
    }
}