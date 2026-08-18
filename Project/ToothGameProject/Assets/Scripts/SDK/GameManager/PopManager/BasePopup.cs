using System;
using System.Collections;
using UnityEngine;

namespace YouDooSDK.UI
{

    public abstract class BasePopup : MonoBehaviour
    {
        [Header("弹窗组件")]
        public CanvasGroup canvasGroup;
        public GameObject content;

        [Header("动画设置")]
        public float showDuration = 0.3f;
        public float hideDuration = 0.2f;

        // 弹窗事件
        public event Action OnShowEvent;
        public event Action OnHideEvent;
        public event Action OnCloseEvent;

        // 输入事件订阅状态
        private bool isSubscribed = false;

        /// <summary>
        /// 初始化弹窗
        /// </summary>
        public virtual void Initialize()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (content == null) content = gameObject;

            // 初始状态
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            content.SetActive(false);
        }

        /// <summary>
        /// 显示动画
        /// </summary>
        public virtual IEnumerator ShowAnimation()
        {
            content.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                float elapsed = 0f;
                while (elapsed < showDuration)
                {
                    canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / showDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            OnShow();
        }

        /// <summary>
        /// 隐藏动画
        /// </summary>
        public virtual IEnumerator HideAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                float elapsed = 0f;
                float startAlpha = canvasGroup.alpha;

                while (elapsed < hideDuration)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, elapsed / hideDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                canvasGroup.alpha = 0;
            }

            content.SetActive(false);
        }

        /// <summary>
        /// 弹窗显示时调用
        /// </summary>
        public virtual void OnShow()
        {
            OnShowEvent?.Invoke();
        }

        /// <summary>
        /// 弹窗隐藏时调用
        /// </summary>
        public virtual void OnHide()
        {
            OnHideEvent?.Invoke();
        }

        /// <summary>
        /// 弹窗关闭时调用
        /// </summary>
        public virtual void OnClose()
        {
            UnSubscribeEvent();
            OnCloseEvent?.Invoke();
        }

        /// <summary>
        /// 关闭弹窗
        /// </summary>
        public void Close()
        {
            PopupManager.Instance.ClosePopup(this);
        }

        public void SubscribeEvent()
        {
            if (!isSubscribed)
            {
                RemoteControlUnitInputSystemManager.Instance.OnDownArrowPressed += OnDownArrowPressed;
                RemoteControlUnitInputSystemManager.Instance.OnUpArrowPressed += OnUpArrowPressed;
                RemoteControlUnitInputSystemManager.Instance.OnLeftArrowPressed += OnLeftArrowPressed;
                RemoteControlUnitInputSystemManager.Instance.OnRightArrowPressed += OnRightArrowPressed;
                RemoteControlUnitInputSystemManager.Instance.OnEscapePressed += OnEscapePressed;
                RemoteControlUnitInputSystemManager.Instance.OnButtonOKPressed += OnButtonOKPressed;
                isSubscribed = true;
                Debug.Log($"{GetType().Name} 订阅了输入事件");
            }
        }

        public void UnSubscribeEvent()
        {
            if (isSubscribed)
            {
                RemoteControlUnitInputSystemManager.Instance.OnDownArrowPressed -= OnDownArrowPressed;
                RemoteControlUnitInputSystemManager.Instance.OnUpArrowPressed -= OnUpArrowPressed;
                RemoteControlUnitInputSystemManager.Instance.OnLeftArrowPressed -= OnLeftArrowPressed;
                RemoteControlUnitInputSystemManager.Instance.OnRightArrowPressed -= OnRightArrowPressed;
                RemoteControlUnitInputSystemManager.Instance.OnEscapePressed -= OnEscapePressed;
                RemoteControlUnitInputSystemManager.Instance.OnButtonOKPressed -= OnButtonOKPressed;
                isSubscribed = false;

                Debug.Log($"{GetType().Name} 取消了输入事件订阅");
            }
        }

        protected virtual void OnDownArrowPressed() { }
        protected virtual void OnUpArrowPressed() { }
        protected virtual void OnLeftArrowPressed() { }
        protected virtual void OnRightArrowPressed() { }
        protected virtual void OnEscapePressed()
        {
            Close();
        }
        protected virtual void OnButtonOKPressed() { }
    }
}
