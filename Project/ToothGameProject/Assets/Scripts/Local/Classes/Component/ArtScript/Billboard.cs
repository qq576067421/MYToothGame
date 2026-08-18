using UnityEngine;
using System.Collections;
namespace LCL
{
    public class Billboard : MonoBehaviour
    {
        private Camera m_LookAtCamera = null;
        private System.Action<bool> m_VisiableChangedCall = null;
        private bool m_LastVisable = true;
        private bool m_UsePosition = false;
        private float m_FarDist = 100;
        public void SetFarDist(float dist)
        {
            m_FarDist = dist;
        }
        public void SetUsePosition(bool usePosition)
        {
            m_UsePosition = usePosition;
        }
        public void SetVisiableChangedCall(System.Action<bool> call)
        {
            m_VisiableChangedCall = call;
        }
        public void SetLookAtCamera(Camera camera)
        {
            m_LookAtCamera = camera;
        }
        public void Reset()
        {
            m_LastVisable = true;
        }
        // Update is called once per frame
        void Update()
        {
            Camera cam = null;
            if(m_LookAtCamera != null)
            {
                cam = m_LookAtCamera;
            }
            else
            {
                cam = Camera.main;
            }
            if(cam == null)
            {
                return;
            }
            Vector2 player2DPosition = cam.WorldToScreenPoint(transform.position);
            bool visiable = false;
            if (player2DPosition.x > Screen.width || player2DPosition.x < 0 || player2DPosition.y > Screen.height || player2DPosition.y < 0)
            {
                visiable = false;
            }
            else
            {
                visiable = true;
            }
            if(visiable != m_LastVisable)
            {
                m_LastVisable = visiable;
                if(m_VisiableChangedCall != null)
                {
                    m_VisiableChangedCall(visiable);
                }
            }
            if(visiable)
            {
                Vector3 vDir = cam.transform.position - transform.position;
                if(Vector3.SqrMagnitude(vDir) > m_FarDist * m_FarDist)
                {
                    return;
                }
                if (m_UsePosition)
                {
                    vDir.Normalize();
                    transform.rotation = Quaternion.LookRotation(-vDir);
                }
                else
                {
                    transform.rotation = cam.transform.rotation;
                }
            }
        }
    }
}
