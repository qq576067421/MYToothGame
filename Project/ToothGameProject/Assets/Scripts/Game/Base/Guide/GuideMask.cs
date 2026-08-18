using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LCL
{
    public enum GuideMaskShape
    {
        ROUND, ELLIPSE, DYNAMIC_ROUND
    }
    public enum GuideMaskEventType
    {
        PointerDown,
        PointerUp,
        Submit,
        PointerClick,
        BeginDrag,
        Dragging,
        EndDrag
    }
    public class GuideParam
    {
        public GameObject m_ClickObj;
        public Vector3 m_Position;
        public GuideMaskEventType m_EventType;
        public bool m_IsClick = false;
    }
    /// <summary>
    /// 新手引导动画
    /// </summary>
    public class GuideMask : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GuideMaskShape m_Shape;
        public Vector2 _Center;
        [Range(0, 2000)]
        public float _Radius = 1000;
        [Range(0, 100)]
        public float _TransitionRange = 10;
        public float _Width = 100;
        public float _Height = 100;
        public float _Ellipse = 4;
        public float _ReduceTime = 1;
        public float _TotalTime = 1;
        public float _StartTime = 0;
        public float _MaxRadius = 1500;
        public bool m_KeepShape = false;
        public bool _ClickGuide = true;
        public int _2d_click = 0; // 0 2d点击 1 2d 拖动  3 3d点击 4 3d拖动
        public bool _PassEvent = true;
        public Camera _WorldCamera;

        private Material material;
        public System.Action<GuideParam> ClickTargetCall = null;
        public System.Action<GuideParam> DragTargetCall = null;
        public System.Func<GuideParam, bool> IsClickTargetCall;
        public System.Func<GuideParam, bool> IsBeginDragTargetCall;
        public bool IsClickAnyNext = false;

        public System.Action<Vector2> _OnBeginDragCall;
        public System.Action<Vector2> _OnDraggingCall;
        public System.Action<Vector2> _OnEndDragCall;

        void Awake()
        {
            material = GetComponent<Image>().material;
        }

        public void Apply()
        {
            material.SetVector("_Center", _Center);
            material.SetFloat("_Width", _Width);
            material.SetFloat("_Height", _Height);
            material.SetFloat("_Radius", _Radius);
            material.SetFloat("_RoundMode", (float)m_Shape);
        }

        private void Update()
        {
            if (m_KeepShape)
            {
                Apply();
            }
        }


        // 监听按下
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_ClickGuide)
            {
                return;
            }
            PassEvent(eventData, ExecuteEvents.pointerDownHandler, GuideMaskEventType.PointerDown);
        }

        // 监听抬起
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_ClickGuide)
            {
                return;
            }
            PassEvent(eventData, ExecuteEvents.pointerUpHandler, GuideMaskEventType.PointerUp);
        }

        // 监听点击
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_ClickGuide)
            {
                return;
            }
            //PassEvent(eventData, ExecuteEvents.submitHandler, GuideMaskEventType.Submit);
            PassEvent(eventData, ExecuteEvents.pointerClickHandler, GuideMaskEventType.PointerClick);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_ClickGuide)
            {
                return;
            }
            PassEvent(eventData, ExecuteEvents.beginDragHandler, GuideMaskEventType.BeginDrag);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ClickGuide)
            {
                return;
            }
            PassEvent(eventData, ExecuteEvents.dragHandler, GuideMaskEventType.Dragging);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ClickGuide)
            {
                return;
            }
            PassEvent(eventData, ExecuteEvents.endDragHandler, GuideMaskEventType.EndDrag);
        }

        // 把事件透下去
        public void PassEvent<T>(PointerEventData data, ExecuteEvents.EventFunction<T> function, GuideMaskEventType evtType)
            where T : IEventSystemHandler
        {
            GuideParam param = new GuideParam();
            if (IsClickAnyNext)
            {
                if (ClickTargetCall != null)
                {
                    param.m_IsClick = true;
                    param.m_EventType = evtType;
                    param.m_Position = data.position;
                    ClickTargetCall(param);
                }
                return;
            }
            if (_2d_click == 0)
            {
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(data, results);



                for (int i = 0; i < results.Count; i++)
                {
                    var cod = results[i];
                    var obj = cod.gameObject;

                    param.m_IsClick = false;
                    param.m_EventType = evtType;
                    param.m_ClickObj = obj;
                    param.m_Position = cod.screenPosition;

                    if (IsClickTargetCall != null && IsClickTargetCall(param))
                    {
                        if (ClickTargetCall != null)
                        {
                            param.m_IsClick = true;
                            ClickTargetCall(param);
                        }

                        if (_PassEvent)
                        {
                            //GuideMask本身不参与事件，防止出现死循环
                            if(obj == gameObject)
                            {
                                continue;
                            }
                            // 如果是目标物体，则把事件透传下去，然后break
                            ExecuteEvents.Execute(results[i].gameObject, data, function);
                        }
                        return;
                    }
                }
            }
            else
            {
                if (_ClickGuide)
                {
 
                    var results = GameDll.Tool.ScreenHitWorldObjs(data.pressPosition, _WorldCamera);
                    for (int i = 0; i < results.Count; i++)
                    {
                        var cod = results[i];
                        var obj = cod.collider.gameObject;

                        param.m_IsClick = false;
                        param.m_EventType = evtType;
                        param.m_ClickObj = obj;
                        param.m_Position = cod.point;

                        if (IsClickTargetCall != null && IsClickTargetCall(param))
                        {
                            if (ClickTargetCall != null)
                            {
                                param.m_IsClick = true;
                                ClickTargetCall(param);
                            }
                            return;
                        }
                    }
                }
                else
                {
                    //需要调用对应的事件来传递
                    if (evtType == GuideMaskEventType.BeginDrag)
                    {
                        if (_2d_click == 1)
                        {
                            var results = GameDll.Tool.GetPointerOverUIGameObjects(data.pressPosition);
                            for (int i = 0; i < results.Count; i++)
                            {
                                var cod = results[i];
                                var obj = cod.gameObject;

                                param.m_IsClick = false;
                                param.m_EventType = evtType;
                                param.m_ClickObj = obj;
                                param.m_Position = cod.screenPosition;

                                if (IsBeginDragTargetCall != null && IsBeginDragTargetCall(param))
                                {
                                    if (DragTargetCall != null)
                                    {
                                        param.m_IsClick = true;
                                        DragTargetCall(param);
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            var results = GameDll.Tool.ScreenHitWorldObjs(data.pressPosition, _WorldCamera);
                            for (int i = 0; i < results.Count; i++)
                            {
                                var cod = results[i];
                                var obj = cod.collider.gameObject;

                                param.m_IsClick = false;
                                param.m_EventType = evtType;
                                param.m_ClickObj = obj;
                                param.m_Position = cod.point;

                                if (IsBeginDragTargetCall != null && IsBeginDragTargetCall(param))
                                {
                                    if (DragTargetCall != null)
                                    {
                                        param.m_IsClick = true;
                                        DragTargetCall(param);
                                    }
                                }
                            }
                        }


                        if (_OnBeginDragCall != null)
                        {
                            _OnBeginDragCall(data.position);
                        }
                    }
                    else if (evtType == GuideMaskEventType.Dragging)
                    {
                        if (_OnDraggingCall != null)
                        {
                            _OnDraggingCall(data.position);
                        }
                    }
                    else if (evtType == GuideMaskEventType.EndDrag)
                    {
                        if (_OnEndDragCall != null)
                        {
                            _OnEndDragCall(data.position);
                        }
                    }
                }
            }

            if (ClickTargetCall != null)
            {
                param.m_IsClick = false;
                param.m_EventType = evtType;
                param.m_ClickObj = null;
                param.m_Position = data.position;
                ClickTargetCall(param);
            }

        }


    }
}