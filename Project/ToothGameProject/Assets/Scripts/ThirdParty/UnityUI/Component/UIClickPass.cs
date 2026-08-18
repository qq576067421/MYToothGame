using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace UnityUI
{
    [ExecuteInEditMode]
    [AddComponentMenu("UITools/Others/UIClickPass")]
    public class UIClickPass : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public System.Action OnClickCall;
        // 监听按下
        public void OnPointerDown(PointerEventData eventData)
        {
            PassEvent(eventData, ExecuteEvents.pointerDownHandler);
        }

        // 监听抬起
        public void OnPointerUp(PointerEventData eventData)
        {
            PassEvent(eventData, ExecuteEvents.pointerUpHandler);
        }

        // 监听点击
        public void OnPointerClick(PointerEventData eventData)
        {
            PassEvent(eventData, ExecuteEvents.pointerClickHandler);
            if (OnClickCall != null)
            {
                OnClickCall();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            PassEvent(eventData, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData eventData)
        {
            PassEvent(eventData, ExecuteEvents.dragHandler);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            PassEvent(eventData, ExecuteEvents.endDragHandler);
        }

        // 把事件透下去
        public void PassEvent<T>(PointerEventData data, ExecuteEvents.EventFunction<T> function)
            where T : IEventSystemHandler
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            for (int i = 0; i < results.Count; i++)
            {
                var obj = results[i].gameObject;
                if (obj == gameObject)
                {
                    continue;
                }
                // 如果是目标物体，则把事件透传下去，然后break
                ExecuteEvents.Execute(results[i].gameObject, data, function);
                //return;
            }
        }
    }
}