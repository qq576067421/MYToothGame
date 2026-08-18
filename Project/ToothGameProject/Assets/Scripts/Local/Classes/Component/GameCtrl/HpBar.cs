using GameDll;
using AmazingAssets.CurvedWorld;
using UnityUI;
using UnityEngine;
using System.Collections;
namespace LCL
{
    public class HpBar : MonoBehaviour
    {
        private static CurvedWorldController m_CurvedWorldController = null;

        public RectTransform rectTrans;

        public Transform target;
        public Collider target_collider;
        public UResource target_resource;
        public Vector3 offsetPos; //头顶偏移量
        public Camera m_WorldCamera;
        public Camera m_UICamera;

        private RectTransform parentRectTrans;
        public bool m_EnableUpdate = false;
        private void Start()
        {
            if (rectTrans != null)
            {
                var screenPos = Vector3.up * 3000f;
                rectTrans.anchoredPosition = screenPos;
            }
        }
        private void Update()
        {
            if(!m_EnableUpdate)
            {
                return;
            }
            Vector3 tarPos = new Vector3(10000000, 10000000, 10000000);
            if (target_resource != null)
            {
                tarPos = target_resource.GetHeadPoint();
            }
            else if (target_collider == null)
            {
                if(target != null)
                {
                    tarPos = target.position;
                }
            }
            else
            {
                var bounds = target_collider.bounds;
                //通过Collider来获取头顶坐标
                var topAhcor = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                //加上头顶偏移量
                tarPos = topAhcor;
            }

            if (m_CurvedWorldController == null)
            {
                m_CurvedWorldController = Object.FindObjectOfType<CurvedWorldController>();
            }
            if (m_CurvedWorldController != null)
            {
                tarPos = CurvedWorldUtilities.TransformPosition(tarPos, m_CurvedWorldController);
            }
            //不在可视窗口的模型，把名字移动到视线外
            var screenPos = Vector3.up * 3000f;
            if (m_WorldCamera != null)
            {
                var viewPos = m_WorldCamera.WorldToViewportPoint(tarPos); //得到视窗坐标
                if (viewPos.z > 0f && viewPos.x > 0f && viewPos.x < 1f && viewPos.y > 0f && viewPos.y < 1f)
                {

                    //获取屏幕坐标
                    screenPos = m_WorldCamera.WorldToScreenPoint(tarPos + offsetPos); //加上头顶偏移量

                    if (parentRectTrans == null)
                    {
                        var canvas = UGUIRoot.GlobalCanvas;
                        if (canvas != null)
                        {
                            parentRectTrans = canvas.transform as RectTransform;
                        }
                        if (parentRectTrans == null)
                        {
                            parentRectTrans = rectTrans.parent as RectTransform;
                        }
                    }
                    if(parentRectTrans != null)
                    {
                        Camera uiCamera = m_UICamera;
                        var canvas = parentRectTrans.GetComponentInParent<Canvas>();
                        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            uiCamera = null;
                        }

                        var uguiPos = GameDll.Tool.ScreenPointToUGUI(parentRectTrans, screenPos, uiCamera);
                        //转化为ugui坐标
                        rectTrans.anchoredPosition = uguiPos;
                        return;
                    }

                }

            }

            {
                //放到屏幕外面
                rectTrans.anchoredPosition = screenPos;
            }

        }
    }
}
