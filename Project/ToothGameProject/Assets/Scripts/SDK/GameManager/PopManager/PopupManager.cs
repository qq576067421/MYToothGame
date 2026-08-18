using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YouDooSDK.Utils;

namespace YouDooSDK.UI
{

    public interface IDataPopup<T>
    {
        void SetData(T data);
    }

    public class PopupManager : MonoSingleton<PopupManager>
    {

        [Header("弹窗设置")]
        public Transform popupParent; // 弹窗父节点
        private string popupPath = "Popups/"; // 预制体路径

        private Stack<BasePopup> popupStack = new Stack<BasePopup>(); // 弹窗栈
        private Dictionary<string, GameObject> popupPrefabs = new Dictionary<string, GameObject>(); // 预制体缓存

        public event Action<BasePopup> OnPopupShow; // 弹窗显示事件
        public event Action<BasePopup> OnPopupHide; // 弹窗隐藏事件
        public event Action<BasePopup> OnPopupClose; // 弹窗关闭事件

        // 当前活动的弹窗（接收输入的弹窗）
        private BasePopup activePopup;



        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            popupStack.Clear();
            if (popupParent == null)
            {
                // 如果没有指定父节点，创建一个Canvas
                CreatePopupCanvas();
            }
        }

        private void CreatePopupCanvas()
        {
            GameObject existingPopupParent = GameObject.Find("PopupParent");

            if (existingPopupParent != null)
            {
                // 如果找到了现有的 PopupParent 节点，直接使用它
                popupParent = existingPopupParent.transform;
                Debug.Log("使用现有的 PopupParent 节点");
            }
            else
            {
                // 如果没有找到，就不创建新的，保持 popupParent 为 null
                Debug.Log("未找到 PopupParent 节点，跳过创建");
                // 注意：这里不创建新的 Canvas，所以 popupParent 保持为 null
            }
        }


        /// <summary>
        /// 更新活动弹窗（只有栈顶弹窗能接收输入）
        /// </summary>
        private void UpdateActivePopup()
        {
            // 取消之前活动弹窗的事件订阅
            if (activePopup != null)
            {
                activePopup.UnSubscribeEvent();
            }

            // 设置新的活动弹窗
            if (popupStack.Count > 0)
            {
                activePopup = popupStack.Peek();
                activePopup.SubscribeEvent();
            }
            else
            {
                activePopup = null;
            }
        }

        /// <summary>
        /// 显示弹窗（泛型数据版本）
        /// </summary>
        public void ShowPopup<TPopup, TData>(string popupName, TData data, Action<TPopup> onCreated = null, Action<TPopup> onShow = null) where TPopup : BasePopup
        {
            StartCoroutine(ShowPopupCoroutine(popupName, data, onCreated, onShow));
        }

        /// <summary>
        /// 显示弹窗（无数据版本）
        /// </summary>
        public void ShowPopup<TPopup>(string popupName, Action<TPopup> onCreated = null, Action<TPopup> onShow = null)
            where TPopup : BasePopup
        {
            StartCoroutine(ShowPopupCoroutine<TPopup, object>(popupName, null, onCreated, onShow));
        }

        private IEnumerator ShowPopupCoroutine<TPopup, TData>(string popupName, TData data, Action<TPopup> onCreated, Action<TPopup> onShow)
         where TPopup : BasePopup
        {
            // 隐藏当前弹窗（如果有）
            if (popupStack.Count > 0)
            {
                BasePopup currentPopup = popupStack.Peek();
                currentPopup.OnHide();
            }

            // 加载弹窗预制体
            GameObject popupObj;
            if (!popupPrefabs.ContainsKey(popupName))
            {
                ResourceRequest request = Resources.LoadAsync<GameObject>(popupPath + popupName);
                yield return request;

                if (request.asset == null)
                {
                    Debug.LogError($"弹窗预制体加载失败: {popupPath + popupName}");
                    yield break;
                }

                popupPrefabs[popupName] = request.asset as GameObject;
            }

            popupObj = Instantiate(popupPrefabs[popupName], popupParent);
            TPopup popup = popupObj.GetComponent<TPopup>();

            if (popup == null)
            {
                Debug.LogError($"弹窗组件未找到: {typeof(TPopup)}");
                Destroy(popupObj);
                yield break;
            }

            // 设置弹窗数据（使用泛型版本）
            if (popup is IDataPopup<TData> dataPopup)
            {
                dataPopup.SetData(data);
            }
            else if (data != null)
            {
                Debug.LogWarning($"弹窗 {typeof(TPopup)} 没有实现 IDataPopup<{typeof(TData)}> 接口，数据将被忽略");
            }

            // 添加到栈
            popupStack.Push(popup);

            // 更新活动弹窗（新弹窗成为活动弹窗）
            UpdateActivePopup();

            // 初始化弹窗
            popup.Initialize();

            // 创建完成回调
            onCreated?.Invoke(popup);

            // 显示弹窗
            yield return StartCoroutine(popup.ShowAnimation());

            // 显示完成回调
            onShow?.Invoke(popup);

            // 触发事件
            OnPopupShow?.Invoke(popup);
        }

        /// <summary>
        /// 关闭当前弹窗
        /// </summary>
        public void CloseCurrentPopup()
        {
            if (popupStack.Count == 0)
            {
                Debug.LogWarning("没有可关闭的弹窗");
                return;
            }

            BasePopup popup = popupStack.Pop();
            StartCoroutine(ClosePopupCoroutine(popup));
        }

        /// <summary>
        /// 关闭指定弹窗
        /// </summary>
        public void ClosePopup(BasePopup popup)
        {
            if (popupStack.Contains(popup))
            {
                // 重建栈（移除指定弹窗）
                Stack<BasePopup> newStack = new Stack<BasePopup>();
                List<BasePopup> tempList = new List<BasePopup>(popupStack);

                foreach (var p in tempList)
                {
                    if (p != popup)
                    {
                        newStack.Push(p);
                    }
                }

                popupStack = newStack;
                StartCoroutine(ClosePopupCoroutine(popup));
            }
        }

        /// <summary>
        /// 关闭所有弹窗
        /// </summary>
        public void CloseAllPopups()
        {
            StopAllCoroutines();

            foreach (var popup in popupStack)
            {
                StartCoroutine(ClosePopupCoroutine(popup, false));
            }

            popupStack.Clear();
            UpdateActivePopup(); // 清空后更新活动弹窗状态
        }

        private IEnumerator ClosePopupCoroutine(BasePopup popup, bool showPrevious = true)
        {
            // 隐藏回调
            popup.OnHide();
            OnPopupHide?.Invoke(popup);

            // 关闭动画
            yield return StartCoroutine(popup.HideAnimation());

            // 关闭回调
            popup.OnClose();
            OnPopupClose?.Invoke(popup);

            // 销毁对象
            Destroy(popup.gameObject);

            // 显示上一个弹窗
            if (showPrevious && popupStack.Count > 0)
            {
                BasePopup previousPopup = popupStack.Peek();
                yield return StartCoroutine(previousPopup.ShowAnimation());
                previousPopup.OnShow();
            }
        }

        /// <summary>
        /// 获取当前弹窗
        /// </summary>
        public BasePopup GetCurrentPopup()
        {
            return popupStack.Count > 0 ? popupStack.Peek() : null;
        }

        /// <summary>
        /// 获取弹窗数量
        /// </summary>
        public int GetPopupCount()
        {
            return popupStack.Count;
        }

        /// <summary>
        /// 获取当前活动弹窗（接收输入的弹窗）
        /// </summary>
        public BasePopup GetActivePopup()
        {
            return activePopup;
        }
    }
}
