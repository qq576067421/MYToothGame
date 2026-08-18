using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace UnityUI
{
    public class UISliderEx: MonoBehaviour
    {
        public Action<float> OnValueChanged;

        public LUIButton m_AddButton;
        public LUIButton m_SubButton;
        public InputField m_Input;
        public Slider m_Slider;

        public int m_MaxValue;
        public int m_MinValue;
        private int m_CurValue;

        public void OnSetComponentValue(int count)
        {
            m_CurValue = count;
            if(m_Slider != null)
            {
                m_Slider.SetValueWithoutNotify((float)count /(m_MaxValue - m_MinValue) );
            }
            if(m_Input != null)
            {
                m_Input.SetTextWithoutNotify(count.ToString());
            }
        }
        public void Init()
        {
            RenderAPI.AddButtonClick(m_SubButton, () =>
            {

                int count = m_CurValue - 1;
                count = Math.Max(m_MinValue, count);
                count = Math.Min(m_MaxValue, count);
                OnSetComponentValue(count);
                if (OnValueChanged != null)
                {
                    OnValueChanged(count);
                }

            });

            RenderAPI.AddButtonClick(m_AddButton, () =>
            {

                int count = m_CurValue + 1;
                count = Math.Max(m_MinValue, count);
                count = Math.Min(m_MaxValue, count);
                OnSetComponentValue(count);
                if (OnValueChanged != null)
                {
                    OnValueChanged(count);
                }

            });

            RenderAPI.AddSliderChanged(m_Slider, (value) =>
            {

                int count = (int)Mathf.Lerp(m_MinValue, m_MaxValue, value);
                OnSetComponentValue(count);
                if (OnValueChanged != null)
                {
                    OnValueChanged(count);
                }

            });

            RenderAPI.AddInputFieldChanged(m_Input, (value) =>
            {
                int num = 0;
                if(int.TryParse(value, out num))
                {
                    int count = (int)Mathf.Lerp(m_MinValue, m_MaxValue, num);
                    OnSetComponentValue(count);
                    if (OnValueChanged != null)
                    {
                        OnValueChanged(count);
                    }
                }

            });
        }

    }
}
