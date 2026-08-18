using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Input = InputSystemCompat;

namespace UnityUI
{
    public class UIRotateModel : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        private Vector3 lastFingerPos;
        private Vector3 nowFingerPos;
        private float xMoveDistance;
        private float yMoveDistance;
        private int backValue = 0;
        
        private bool m_MouseDown = false;
        public float speed = 120;
        public Transform RotationObj;
        public bool RotationWorld = false;
        //当点击到该图片作为开始
        public bool  check_range;
        //是否开启回弹
        public bool return_rot = true;
        public Vector3 return_euler = new Vector3(0,0,0);
        private bool start_return = false;
        private float start_return_time = 0;
        private Vector3 start_return_euler = Vector3.zero;
        public float return_time = 2.0f;
        public void SetRotationObj(Transform obj, Vector3 def_rot)
        {
            RotationObj = obj;
            return_euler = def_rot;
        }
        public void SetRotationObj(Transform obj)
        {
            RotationObj = obj;
        }
        void Update()
        {
            if (RotationObj == null || RotationObj.Equals(null))
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
                if(RotationWorld)
                {
                    RotationObj.eulerAngles = Vector3.Lerp(start_return_euler, return_euler, 1 - start_return_time / return_time);
                }
                else
                {
                    RotationObj.localEulerAngles = Vector3.Lerp(start_return_euler, return_euler, 1 - start_return_time / return_time);
                }
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
                    if (return_rot)
                    {
                        start_return = true;
                        start_return_time = return_time;
                        start_return_euler = RotationObj.localEulerAngles;
                    }
                    return;
                }

                RotModel();
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
                    if (return_rot)
                    {
                        start_return = true;
                        start_return_time = return_time;
                        start_return_euler = RotationWorld ? RotationObj.eulerAngles : RotationObj.localEulerAngles;
                    }
                }


                if (m_MouseDown)
                {
                    nowFingerPos = Input.mousePosition;
                    RotModel();
                    lastFingerPos = nowFingerPos;
                }
            }

        }
        private void RotModel()
        {
            if(RotationObj == null || RotationObj.Equals(null))
            {
                return;
            }
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
                RotationObj.Rotate(Vector3.up * -1 * Time.deltaTime * speed, RotationWorld ? Space.World : Space.Self);
            }
            else if (backValue == 1)
            {
                RotationObj.Rotate(Vector3.up * Time.deltaTime * speed, RotationWorld ? Space.World : Space.Self);
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
            if (return_rot)
            {
                start_return = true;
                start_return_time = return_time;
                start_return_euler = RotationWorld? RotationObj.eulerAngles : RotationObj.localEulerAngles;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!check_range)
            {
                return;
            }
            nowFingerPos = eventData.position;
            RotModel();
            lastFingerPos = nowFingerPos;
        }
    }
}
