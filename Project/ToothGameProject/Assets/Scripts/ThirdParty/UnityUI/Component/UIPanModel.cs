using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Input = InputSystemCompat;

namespace UnityUI
{
    public class UIPanModel : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        private Vector3 lastFingerPos;
        private Vector3 nowFingerPos;
        private float xMoveDistance;
        private float yMoveDistance;
        private int backValue = 0;
        
        private bool m_MouseDown = false;
        public float pan_speed = 2;
        public Transform PanObj;
        //当点击到该图片作为开始
        public bool  check_range;
        //是否开启回弹
        public bool return_pos = true;
        public float return_speed = 1;
        public Vector3 return_def_pos = Vector3.zero;
        private bool start_return = false;
        private float start_return_time = 0;
        private Vector3 start_return_pos = Vector3.zero;
        public float return_time = 2.0f;
        public void SetPanObj(Transform obj, Vector3 def_pos)
        {
            PanObj = obj;
            return_def_pos = def_pos;
        }
        public void SetPanObj(Transform obj)
        {
            PanObj = obj;
        }
        void Update()
        {
            if (PanObj == null || PanObj.Equals(null))
            {
                return;
            }

            if (start_return)
            {
                start_return_time = start_return_time - Time.deltaTime;
                if (start_return_time < 0)
                {
                    start_return_time = 0;
                    start_return = false;
                }
                PanObj.localPosition = Vector3.Lerp(start_return_pos, return_def_pos, 1 - start_return_time / return_time);
            }

            if (check_range)
            {
                return;
            }




            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {

                if (Input.touchCount <= 0)
                {
                    return;
                }

                if (Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    //Debug.Log("======开始触摸=====");  
                    lastFingerPos = Input.GetTouch(0).position;
                    start_return = false;
                }

                nowFingerPos = Input.GetTouch(0).position;

                if ((Input.GetTouch(0).phase == TouchPhase.Stationary) || (Input.GetTouch(0).phase == TouchPhase.Ended))
                {
                    lastFingerPos = nowFingerPos;
                    if (return_pos)
                    {
                        start_return = true;
                        start_return_time = return_time;
                        start_return_pos = PanObj.localPosition;
                    }
                    return;
                }

                PanModel();
                lastFingerPos = nowFingerPos;
            }
            else
            {

                if (Input.GetMouseButtonDown(0))
                {
                    m_MouseDown = true;
                    lastFingerPos = Input.mousePosition;
                    nowFingerPos = Input.mousePosition;
                    start_return = false;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    m_MouseDown = false;
                    if (return_pos)
                    {
                        start_return = true;
                        start_return_time = return_time;
                        start_return_pos = PanObj.localPosition;
                    }
                }


                if (m_MouseDown)
                {
                    nowFingerPos = Input.mousePosition;
                    PanModel();
                    lastFingerPos = nowFingerPos;
                }
            }

        }
        private void PanModel()
        {
            if (lastFingerPos == nowFingerPos)
            {
                return;
            }
            if (nowFingerPos.x - lastFingerPos.x > 0)
            {
                //Debug.Log("=======沿着X轴负方向移动=====");  
                backValue = -1; //沿着X轴负方向移动  
            }
            else
            {
                //Debug.Log("=======沿着X轴正方向移动=====");  
                backValue = 1; //沿着X轴正方向移动  
            }

            if (backValue == -1)
            {
                PanObj.Translate(Vector3.up * -1 * Time.deltaTime * pan_speed, Space.World);
            }
            else if (backValue == 1)
            {
                PanObj.Translate(Vector3.up * Time.deltaTime * pan_speed, Space.World);
            }
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if(!check_range)
            {
                return;
            }
            lastFingerPos = eventData.position;
            nowFingerPos = eventData.position;
            start_return = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (return_pos)
            {
                start_return = true;
                start_return_time = return_time;
                start_return_pos = PanObj.localPosition;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!check_range)
            {
                return;
            }
            nowFingerPos = eventData.position;
            PanModel();
            lastFingerPos = nowFingerPos;
        }
    }
}
