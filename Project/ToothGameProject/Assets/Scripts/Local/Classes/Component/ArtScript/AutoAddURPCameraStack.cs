using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Rendering.Universal;
namespace LCL
{
    //将新增加的camera添加到游戏的基础相机堆栈里面
    //并非所有的相机都需要，例如剧情相机、后视镜等等功能的相机不需要。这些相机都是完全或者部分覆盖基础相机
    //一般照射的内容需要和基础相机混合的才这么做。例如高亮显示的一个Layer层的角色，例如场景里面的东西如果是分层的。
    //注意：多场景叠加那种不支持
    public class AutoAddURPCameraStack : MonoBehaviour
    {
        public Camera m_AddCamera;
        public int m_Depth = 0;
        public CameraRenderType m_CameraRenderType = CameraRenderType.Overlay;
        public static Action<Camera, int, CameraRenderType> OnAutoAddURPCameraStack;
        private void Awake()
        {
            if(m_AddCamera == null)
            {
                m_AddCamera = GetComponent<Camera>();
            }
            if(OnAutoAddURPCameraStack != null)
            {
                OnAutoAddURPCameraStack(m_AddCamera, m_Depth, m_CameraRenderType);
            }
        }
    }
}
