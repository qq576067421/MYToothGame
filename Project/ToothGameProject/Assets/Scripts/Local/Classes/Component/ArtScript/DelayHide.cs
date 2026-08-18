using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCL
{
    public class DelayHide : MonoBehaviour
    {
        public float DelayTime = 1.0f;
        public GameObject HideObj = null;
        private float _DelayTime = 0;
        private bool _IsStart = false;
        public void Play(bool mustShow = false)
        {
            _DelayTime = DelayTime;
            _IsStart = true;
            if(mustShow)
            {
                HideObj.SetActive(true);
            }
        }
        public void Play(float time, bool mustShow = false)
        {
            _DelayTime = time;
            _IsStart = true;
            if (mustShow)
            {
                HideObj.SetActive(true);
            }
        }
        // Update is called once per frame
        void Update()
        {
            if (_IsStart)
            {
                _DelayTime -= Time.deltaTime;
                if (_DelayTime <= 0)
                {
                    _IsStart = false;
                    if (HideObj != null)
                    {
                        HideObj.SetActive(false);
                    }
                }
            }

        }
    }
}