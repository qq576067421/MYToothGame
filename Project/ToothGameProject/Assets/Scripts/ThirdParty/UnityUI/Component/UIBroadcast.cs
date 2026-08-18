using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
namespace UnityUI
{
    enum CirculationType
    {
        Single,
        Circulation,
        Insertion,
    }

    class Item
    {
        public Item(string pText, int pCount)
        {
            m_pText = pText;
            Count = pCount;
        }
        public string m_pText;
        public int Count;
    }

    public class UIBroadcast : MonoBehaviour
    {

        /// <summary>
        /// 公告跑马灯
        /// </summary>
        public Text NoticeText;
        public Vector3 Speed = new Vector3(2, 0, 0);
        public float _MoveEndPoint;
        public Vector2 _MoveStartPoint;
        public float _ParentWidth;
        public float _SelfWidth;
        public RectTransform m_NoticePanel;
        public float m_NoticePanelHideDelayTime = 2.0f;
        private bool m_WillHideNoticePanel = false;


        private int spe = 3;
        string pname;

        List<string> pName;

        bool m_bCirculation;
        bool m_bInsertion;
        bool m_bRun = true;
        int m_nRun = 0;
        Queue<Item> GetVs = new Queue<Item>();

        /// <summary>
        /// 土豪数据 先进后出
        /// </summary>
        Stack<Item> GetStack = new Stack<Item>();

        public  Action m_pUpdateQueueText;

        public  Action m_pUpdateStackText;

        public void AddTextVs(string text, int play_count = 0)
        {
            m_WillHideNoticePanel = false;
            if(m_NoticePanel != null && m_NoticePanel.gameObject.activeSelf == false)
            {
                m_NoticePanel.gameObject.SetActive(true);
            }
            GetVs.Enqueue(new Item(text, play_count));
        }
        public void AddTextStack(string text, int play_count = 0)
        {
            m_WillHideNoticePanel = false;
            if (m_NoticePanel != null && m_NoticePanel.gameObject.activeSelf == false)
            {
                m_NoticePanel.gameObject.SetActive(true);
            }
            GetStack.Push(new Item(text, play_count));
        }
        private void Start()
        {
            //GetVs.Enqueue(new Item("元素1222222222222222222222222222222222222222222222222222222222222", 0));
            //GetVs.Enqueue(new Item("元素233333333333333333333333333333333333333333333333333333333333", 2));//2  3
            //GetVs.Enqueue(new Item("元素344444444444444444444444444444444444444444444444444444444444", 3));//3  4
            //GetVs.Enqueue(new Item("元素35555555555555555555555555555555555555555555555555555555555", 0));

            //GetQueueText();
            RegisterBroadcast();
            m_pUpdateQueueText += GetQueueText;

            m_pUpdateStackText += GetStackText;

            //button.onClick.AddListener(delegate ()
            //{
            //    GetStack.Push(new Item("土豪充值：百万！！！！！！！！ 这个是超级土豪！！！！！ 大家欢迎！！！！", 3));
            //});
        }

        /// <summary>
        /// 注册 公告，便于刷新
        /// </summary>
        /// <param name="text"></param>
        void RegisterBroadcast()
        {
            _ParentWidth = NoticeText.transform.parent.GetComponent<RectTransform>().rect.width;
            _SelfWidth = NoticeText.preferredWidth;
            NoticeText.transform.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);

            _MoveEndPoint = -_ParentWidth / 2 - _SelfWidth;

            _MoveStartPoint = new Vector2(_ParentWidth / 2, 0);
        }

        private void FixedUpdate()
        {
            if (GetStack.Count == 0 && GetVs.Count == 0)
            {
                if (m_bRun)
                {
                    m_nRun++;
                    m_bRun = false;
                }
            }
            else
            {

                m_bRun = true;
            }

            if (m_nRun >= 2) return;
            m_bInsertion = GetStack.Count != 0 ? true : false;

            // 公告移动
            if (NoticeText.gameObject.activeInHierarchy)
            {
                NoticeText.transform.localPosition -= Speed;
                if (NoticeText.transform.localPosition.x <= _MoveEndPoint)
                {
                    NoticeText.transform.localPosition = _MoveStartPoint;

                    if (m_nRun == 1) m_nRun++;
                    m_bCirculation = spe > 0 ? true : false;
                    if (m_bCirculation)
                        spe--;

                    if (m_bInsertion)
                    {
                        if (m_pUpdateStackText != null)
                        {
                            m_pUpdateStackText();
                            return;
                        }
                    }
                    else
                    {
                        if (m_pUpdateQueueText != null && GetVs.Count != 0 && !m_bCirculation)
                        {
                            m_pUpdateQueueText();
                            return;
                        }
                    }

                    m_WillHideNoticePanel = true;
                    StartCoroutine(DelayHideNoticePanel(m_NoticePanelHideDelayTime));
                }
            }
        }

        private IEnumerator DelayHideNoticePanel(float delay_time)
        {
            yield return new WaitForSeconds(delay_time);
            if(m_WillHideNoticePanel)
            {
                if(m_NoticePanel != null)
                {
                    m_NoticePanel.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 获取下一条
        /// </summary>
        /// <returns></returns>
        public void GetQueueText()
        {
            Item item = GetVs.Dequeue();
            NoticeText.text = item.m_pText;
            spe = item.Count - 1;

        }

        public void GetStackText()
        {
            Item item = GetStack.Pop();
            NoticeText.text = item.m_pText;
            spe = item.Count - 1;
        }
    }
}