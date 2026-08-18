using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Collections;
using YouDooSDK.Utils;

namespace YouDooSDK.UI
{
    public class TipsManager : MonoSingleton<TipsManager>
    {

        private GameObject tipPrefab; // Tips预制体 
        public int maxConcurrentTips = 5;
        public float defaultShowDelay = 0.3f;

        private Queue<UITip> availableTips = new Queue<UITip>();
        private List<UITip> activeTips = new List<UITip>();
        private Transform tipsContainer;

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
            InitializePool();
        }

        void InitializePool()
        {
            availableTips?.Clear();
            activeTips?.Clear();
            GameObject existingPopupParent = GameObject.Find("TipsContainer");
            if (existingPopupParent != null)
            {
                // 如果找到了现有的 PopupParent 节点，直接使用它
                tipsContainer = existingPopupParent.transform;
                Debug.Log("使用现有的 PopupParent 节点");
                tipsContainer.localPosition = Vector3.zero;
                StartCoroutine(ShowTipsCoroutine());
            }
            else
            {
                // 如果没有找到，就不创建新的，保持 popupParent 为 null
                Debug.Log("未找到 PopupParent 节点，跳过创建");
                // 注意：这里不创建新的 Canvas，所以 popupParent 保持为 null
            }
        }


        private IEnumerator ShowTipsCoroutine()
        {
            // 隐藏当前弹窗（如果有）
            if (tipPrefab == null)
            {
                ResourceRequest request = Resources.LoadAsync<GameObject>("Tips/UITips");
                yield return request;
                if (request.asset == null)
                {
                    Debug.LogError($"弹窗预制体UITips加载失败");
                    yield break;
                }
                tipPrefab = request.asset as GameObject;
            }

            for (int i = 0; i < maxConcurrentTips; i++)
            {
                CreateNewTip();
            }
        }


        void CreateNewTip()
        {
            GameObject tipObj = Instantiate(tipPrefab, tipsContainer);
            UITip tip = tipObj.GetComponent<UITip>();
            tip.Initialize();
            availableTips.Enqueue(tip);
            tipObj.SetActive(false);
        }

        // 请求显示Tips
        public void ShowTip(string content)
        {
            // 1. 防刷屏机制：如果当前正在显示的Tip中，有相同内容的，直接刷新它的显示时间并复用
            for (int i = 0; i < activeTips.Count; i++)
            {
                if (activeTips[i].contentText.text == content && activeTips[i].gameObject.activeSelf)
                {
                    activeTips[i].RefreshShow();
                    return;
                }
            }

            if (availableTips.Count == 0)
            {
                // 回收最早的一个Tips
                RecycleTip(activeTips[0]);
            }

            UITip tip = availableTips.Dequeue();
            activeTips.Add(tip);

            // 为了防止重叠，可以将新弹出的Tip层级置顶
            tip.transform.SetAsLastSibling();
            tip.Show(content);
        }

        // 隐藏特定Tips
        public void HideTip(UITip tip)
        {
            if (activeTips.Contains(tip))
            {
                tip.Hide();
                activeTips.Remove(tip);
                availableTips.Enqueue(tip);
            }
        }

        // 回收Tips
        void RecycleTip(UITip tip)
        {
            tip.Hide();
            activeTips.Remove(tip);
            availableTips.Enqueue(tip);
        }

        // 隐藏所有Tips
        public void HideAllTips()
        {
            foreach (var tip in activeTips.ToArray())
            {
                HideTip(tip);
            }
        }


    }
}
