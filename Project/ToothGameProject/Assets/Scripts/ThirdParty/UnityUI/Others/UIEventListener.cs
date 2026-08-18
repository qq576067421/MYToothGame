using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityUI
{
    public class UIEventListener : EventTrigger
    {
        public delegate void VoidDelegate(GameObject go);
        public delegate void BoolDelegate(GameObject go, bool isValue);
        public delegate void FloatDelegate(GameObject go, float fValue);
        public delegate void IntDelegate(GameObject go, int iIndex);
        public delegate void StringDelegate(GameObject go, string strValue);

        public VoidDelegate onSubmit;
        public VoidDelegate onClick;
        public VoidDelegate onPointerDown;
        public VoidDelegate onPointerUp;
        public BoolDelegate onHover;
        public BoolDelegate onToggleChanged;
        public FloatDelegate onSliderChanged;
        public FloatDelegate onScrollbarChanged;
        public IntDelegate onDrapDownChanged;
        public StringDelegate onInputFieldChanged;
        public Action<GameObject, Vector3> onBeginDrag;
        public Action<GameObject, Vector3> onDrag;
        public Action<GameObject, Vector3> onEndDrag;


        public override void OnSubmit(BaseEventData eventData)
        {
            if (onSubmit != null)
                onSubmit(gameObject);
        }
        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (onHover != null)
                onHover(gameObject, true);
        }
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (onClick != null)
                onClick(gameObject);
            if (onToggleChanged != null)
                onToggleChanged(gameObject, gameObject.GetComponent<Toggle>().isOn);

        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            if (onHover != null)
                onHover(gameObject, false);
        }
        public override void OnDrag(PointerEventData eventData)
        {
            if (onSliderChanged != null)
            {
                onSliderChanged(gameObject, gameObject.GetComponent<Slider>().value);
            }
            if (onScrollbarChanged != null)
            {
                onScrollbarChanged(gameObject, gameObject.GetComponent<Scrollbar>().value);
            }
            if(onDrag != null)
            {
                Vector2 position;
                Canvas canvas = FindObjectOfType<Canvas>();
                RectTransform rect = canvas.GetComponent<RectTransform>();
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, canvas.worldCamera, out position))
                {
                    onDrag(gameObject, position);
                }
            }

        }

        public override void OnSelect(BaseEventData eventData)
        {
            if (onDrapDownChanged != null)
                onDrapDownChanged(gameObject, gameObject.GetComponent<Dropdown>().value);
        }
        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (onInputFieldChanged != null)
                onInputFieldChanged(gameObject, gameObject.GetComponent<InputField>().text);
        }
        public override void OnDeselect(BaseEventData eventData)
        {
            if (onInputFieldChanged != null)
                onInputFieldChanged(gameObject, gameObject.GetComponent<InputField>().text);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (onPointerDown != null)
            {
                onPointerDown(gameObject);
            }
        }
        public override void OnPointerUp(PointerEventData eventData)
        {
            if (onPointerUp != null)
                onPointerUp(gameObject);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 position;
            Canvas canvas = FindObjectOfType<Canvas>();
            RectTransform rect = canvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, canvas.worldCamera, out position))
            {
                if (onBeginDrag != null)
                {
                    onBeginDrag(gameObject, position);
                }
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            Vector2 position;
            Canvas canvas = FindObjectOfType<Canvas>();
            RectTransform rect = canvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, canvas.worldCamera, out position))
            {
                if (onEndDrag != null)
                    onEndDrag(gameObject, position);
            }
        }












        public static UIEventListener Get(GameObject go)
        {
            UIEventListener listener = go.GetComponent<UIEventListener>();
            if (listener == null) listener = go.AddComponent<UIEventListener>();
            return listener;
        }
    }

}
