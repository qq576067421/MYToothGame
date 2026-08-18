using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUI
{
    public class UIHelper : MonoBehaviour
    {
        public string m_LanId;
        [SerializeField]
        private Button m_btnHelper;
        public void Awake()
        {
            if(m_btnHelper == null)
            {
                m_btnHelper = gameObject.GetComponent<Button>();
            }
            RenderAPI.AddButtonClick(m_btnHelper, OnClickHelper);
        }
        public void OnClickHelper()
        {
            if(!enabled)
            {
                return;
            }
            GameDll.RenderEvent.Event.OnClickUIHelper(m_LanId);
        }
    }
}