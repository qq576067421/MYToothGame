using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using UnityEngine.EventSystems;
using LCL;
using UnityEngine.UI;

namespace UnityUI
{
    public enum WindowType
    {
        [Tooltip("标准弹窗")]
        Pop,//弹窗
        [Tooltip("标准全屏")]
        FullScreen,//全屏
        [Tooltip("依附于某个全屏界面或者空屏幕的全屏大界面")]
        FullFloat, //全屏停靠
        [Tooltip("工具类小型停靠界面")]
        SideFloat, //局部停靠
    }

    [Serializable]
    public class ActiveButtons
    {
        public List<Button> buttons = new List<Button>();
    }
    [RequireComponent(typeof(ComponentBridge))]
    public class UIWindow : MonoBehaviour, IEventSystemHandler, ISelectHandler, IPointerDownHandler
    {

        public string StyleName = "normal";
        public WindowType WindowType = WindowType.Pop;
        public UIWindowAnimation m_Animation;
        public float m_CacheTime = -1;
        public int WindowLayer = 0;

        public Action OnFocusCall;

        //当前界面是否影响全局导航
        public List<ActiveButtons> m_ActiveButtons = new List<ActiveButtons>();
        public int m_DefRowIndex = 0;
        public int m_DefColIndex = 0;
        public Button m_ActiveUpButton;
        public Button m_ActiveDownButton;
        public Button m_ActiveLeftButton;
        public Button m_ActiveRightButton;

        public bool HasActiveButton()
        {
            return m_ActiveButtons != null && m_ActiveButtons.Count > 0;
        }

        private Dictionary<int, ICoroutineHandler> m_Coroutines = new Dictionary<int, ICoroutineHandler>();

        void Awake()
        {
            GameDll.RenderEvent.Event.OnAddCoroutinesGameObject += OnAddCoroutinesGameObject;
        }
        void OnDestroy()
        {
            GameDll.RenderEvent.Event.OnAddCoroutinesGameObject -= OnAddCoroutinesGameObject;
        }

        private void OnAddCoroutinesGameObject(ICoroutineHandler mono)
        {
            var id = mono.Coroutine_GetInstanceID();
            if(m_Coroutines.ContainsKey(id))
            {
                m_Coroutines[id] = mono;
            }
            else
            {
                m_Coroutines.Add(id, mono);
            }
        }

        public void OnClose()
        {
            if(m_Coroutines.Count > 0)
            {
                foreach(var kv in m_Coroutines)
                {
                    var mono = kv.Value;
                    if(mono == null || mono.Equals(null))
                    {
                        continue;
                    }
                    mono.Coroutine_StopAllCoroutines();
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(OnFocusCall != null)
            {
                OnFocusCall();
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (OnFocusCall != null)
            {
                OnFocusCall();
            }
        }
    }
}