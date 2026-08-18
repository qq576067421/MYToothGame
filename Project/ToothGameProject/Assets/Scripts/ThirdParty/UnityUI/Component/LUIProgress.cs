using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

namespace UnityUI
{

    public enum Direction
    {
        LeftToRight,
        RightToLeft,
        BottomToTop,
        TopToBottom
    }
    [ExecuteInEditMode]
    public class LUIProgress : MonoBehaviour
    {

        public Graphic m_Fill;
        public Graphic m_Background;
        [SerializeField]
        [Range(0,1)]
        private float m_Percent;
        [SerializeField]
        [Tooltip("建议根据实际的方向对Fill图片进行锚点的设置")]
        private Direction m_Direction = Direction.LeftToRight;
        private float m_BackgroundLength;
        void Start()
        {
            if(m_Background != null)
            {
                m_BackgroundLength = GetLength(m_Background, m_Direction);
            }
        }
        private float m_TargetPercent;
        private float m_TweenStartPercent;
        private bool m_UseTween;
        private float m_TweenStartTime = 0;
        private float m_TweenTotalTime = 1.0f;
        public void SetValue(float value)
        {
            m_UseTween = false;
            m_Percent = value;
            SetLength(m_Fill, m_Direction, m_Percent, m_BackgroundLength);
        }
        public void SetTween(float target_value, float speed)
        {
            if(speed == 0)
            {
                speed = 1.0f;
            }
            m_UseTween = true;
            m_TweenStartPercent = m_Percent;
            m_TargetPercent = target_value;
            m_TweenTotalTime = (target_value - m_Percent) / speed;
            m_TweenStartTime = Time.realtimeSinceStartup;
        }
        public float GetPercent()
        {
            return m_Percent;
        }
        private void Update()
        {
            if (m_Background != null)
            {
                m_BackgroundLength = GetLength(m_Background, m_Direction);
            }
            if (m_Fill != null)
            {
                if (m_UseTween)
                {
                    float per = (Time.realtimeSinceStartup - m_TweenStartTime) / m_TweenTotalTime;
                    m_Percent = Mathf.Lerp(m_TweenStartPercent, m_TargetPercent, per);
                }
                SetLength(m_Fill, m_Direction, m_Percent, m_BackgroundLength);
            }
        }

        private float GetLength(Graphic graphic, Direction direction)
        {
            var sizeDelta = graphic.rectTransform.sizeDelta;
            if (direction == Direction.LeftToRight || direction == Direction.RightToLeft)
            {
                return sizeDelta.x;
            }
            else if (direction == Direction.BottomToTop || direction == Direction.TopToBottom)
            {
                return sizeDelta.y;
            }
            else
            {
                return 1;
            }
        }
        private void SetLength(Graphic graphic, Direction direction, float percent, float background)
        {
            percent = Mathf.Max(0, percent);
            percent = Mathf.Min(1, percent);

            var sizeDelta = graphic.rectTransform.sizeDelta;
            if (direction == Direction.LeftToRight || direction == Direction.RightToLeft)
            {
                graphic.rectTransform.sizeDelta = new Vector2( percent * background, graphic.rectTransform.sizeDelta.y);
            }
            else if (direction == Direction.BottomToTop || direction == Direction.TopToBottom)
            {
                graphic.rectTransform.sizeDelta = new Vector2(graphic.rectTransform.sizeDelta.x, percent * background);
            }
            else
            {

            }
        }
    }

}