using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCL
{
    public class CameraFoot : MonoBehaviour
    {
        public Camera m_CameraEye;
        private static CameraFoot m_Instance;
        public static CameraFoot GetInstance()
        {
            return m_Instance;
        }

        public Camera ReadCameraEye()
        {
            return m_CameraEye;
        }

        public bool ApplySceneCamera(Camera sceneCamera, bool hideSceneCamera = true)
        {
            if (m_CameraEye == null || sceneCamera == null)
            {
                return false;
            }

            if (object.ReferenceEquals(m_CameraEye, sceneCamera))
            {
                return true;
            }

            var sceneCameraTransform = sceneCamera.transform;
            var cameraEyeTransform = m_CameraEye.transform;
            cameraEyeTransform.position = sceneCameraTransform.position;
            cameraEyeTransform.rotation = sceneCameraTransform.rotation;

            m_CameraEye.CopyFrom(sceneCamera);
            m_CameraEye.enabled = true;

            if (hideSceneCamera && sceneCamera.gameObject.activeSelf)
            {
                sceneCamera.gameObject.SetActive(false);
            }

            return true;
        }

        private void Awake()
        {
            m_Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
}
