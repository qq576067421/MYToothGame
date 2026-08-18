using GameDll;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;

namespace LCL
{
    public class PlayerHud : MonoBehaviour
    {
        public int m_Id;
        public HudType m_Type;
        public RectTransform m_Parent;
        public Text m_txtLevel;
        public Transform m_LevelBg;
        public LUIImage m_imgExp;
        public Text m_HudName;
        public TweenSlider m_HpTweenSlider = null;
        public RectTransform m_HudRender;
        public HpBar m_HpBar;
        public Slider m_Blood;
        public Image m_BloodImage;

        public TweenSlider m_MagicTweenSlider = null;
        public Slider m_Magic;
        public Image m_MagicImage;
        public CanvasGroup m_CanvasGroup;
    }
}