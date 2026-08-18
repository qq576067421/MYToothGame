using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityUI
{
    public class ClickEventListener : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public static ClickEventListener Get(GameObject obj)
        {
            ClickEventListener listener = obj.GetComponent<ClickEventListener>();
            if (listener == null)
            {
                listener = obj.AddComponent<ClickEventListener>();
            }
            return listener;
        }
        public PointerEventData mEventData { get; set; }

        System.Action<GameObject> mClickedHandler = null;
        System.Action<GameObject> mDoubleClickedHandler = null;
        System.Action<GameObject> mOnPointerDownHandler = null;
        System.Action<GameObject> mOnPointerUpHandler = null;
        bool mIsPressed = false;

        public bool IsPressd
        {
            get { return mIsPressed; }
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            mEventData = eventData;
            if (eventData.clickCount == 2)
            {
                if (mDoubleClickedHandler != null)
                {
                    mDoubleClickedHandler(gameObject);
                }
            }
            else
            {
                if (mClickedHandler != null)
                {
                    mClickedHandler(gameObject);
                }
            }

        }
        public void SetClickEventHandler(System.Action<GameObject> handler)
        {
            mClickedHandler = handler;
        }

        public void SetDoubleClickEventHandler(System.Action<GameObject> handler)
        {
            mDoubleClickedHandler = handler;
        }

        public void SetPointerDownHandler(System.Action<GameObject> handler)
        {
            mOnPointerDownHandler = handler;
        }

        public void SetPointerUpHandler(System.Action<GameObject> handler)
        {
            mOnPointerUpHandler = handler;
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            mEventData = eventData;
            mIsPressed = true;
            if (mOnPointerDownHandler != null)
            {
                mOnPointerDownHandler(gameObject);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            mEventData = eventData;
            mIsPressed = false;
            if (mOnPointerUpHandler != null)
            {
                mOnPointerUpHandler(gameObject);
            }
        }

    }

}
