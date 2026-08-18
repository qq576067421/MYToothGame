using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LCL
{
    public class CooldownCtrl : MonoBehaviour
    {
        public enum CDState
        {
            None,
            InCooling,
            Cooleddown,
        }
        public Text m_NumberTxt;
        public Image m_CDImage;

        //毫秒
        public int m_TotalCDMs = 0;
        public System.Func<int> OnUpdateCDCall;
        public System.Action OnCDFinishCall;
        private int m_LastCDNumber = int.MinValue;
        public CDState m_State = CDState.None;
        public void StartPlay()
        {
            m_State = CDState.InCooling;
        }
        void Update()
        {
            switch(m_State)
            {
                case CDState.None:
                    {
                        break;
                    }
                case CDState.InCooling:
                    {
                        if (OnUpdateCDCall != null)
                        {
                            int cd = OnUpdateCDCall();
                            if(cd <= 0)
                            {
                                m_NumberTxt.text = "";
                                m_CDImage.fillAmount = 0;
                                m_LastCDNumber = int.MinValue;
                                m_State = CDState.Cooleddown;
                            }
                            else
                            {
                                if(m_LastCDNumber != cd)
                                {
                                    m_NumberTxt.text = (cd / 1000).ToString();
                                    m_LastCDNumber = cd;
                                    if(m_TotalCDMs != 0)
                                    {
                                        m_CDImage.fillAmount = (float)cd / (float)m_TotalCDMs;
                                    }

                                }
                            }
                        }
                        break;
                    }
                case CDState.Cooleddown:
                    {
                        if(OnCDFinishCall != null)
                        {
                            OnCDFinishCall();
                        }
                        m_State = CDState.None;
                        break;
                    }
            }

        }
    }

}