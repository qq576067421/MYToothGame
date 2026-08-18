using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCL
{
    public class FlyCoinCtrl : MonoBehaviour
    {
        public RectTransform m_Template;
        private List<RectTransform> m_CacheCoins = new List<RectTransform>();
        public float m_Speed;
        public float m_DelayFly = 1.0f;
        public float m_DelayDisappear = 1.0f;

        //uiParent 是转换过去的UI节点的父节点，也就是参考系
        public void Play(RectTransform uiParent, Vector3 worldPos, Camera worldCamera, Camera uiCamera, Vector2 targetPos)
        {
            var pos = GameDll.Tool.WorldToUGUIPoint(uiParent,  worldPos,  worldCamera,  uiCamera);
            RectTransform rect = null;
            if (m_CacheCoins.Count > 0)
            {
                rect = m_CacheCoins[0];
                m_CacheCoins.RemoveAt(0);
            }
            else
            {
                var go = GameObject.Instantiate(m_Template.gameObject);
                go.transform.SetParent(m_Template.transform.parent, false);
                go.transform.localScale = Vector3.one;
                rect = go.GetComponent<RectTransform>();
            }

            rect.anchoredPosition = pos;
            rect.gameObject.SetActive(false);
            float time = Vector2.Distance(pos, targetPos) / m_Speed;
            StartCoroutine(OnPlay(rect, pos, targetPos, time));
        }

        private IEnumerator OnPlay(RectTransform rect, Vector2 start, Vector2 targetPos, float time)
        {
            rect.gameObject.SetActive(true);
            yield return new WaitForSeconds(m_DelayFly);

            float startTime = Time.timeSinceLevelLoad;
            while(Time.timeSinceLevelLoad - startTime <= time)
            {
                var newPos = Vector2.Lerp(start, targetPos, (Time.timeSinceLevelLoad - startTime) / time);
                rect.anchoredPosition = newPos;
                yield return null;
            }
            rect.anchoredPosition = targetPos;
            yield return new WaitForSeconds(m_DelayDisappear);
            m_CacheCoins.Add(rect);
            rect.gameObject.SetActive(false);

        }
    }

}