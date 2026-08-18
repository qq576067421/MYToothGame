using LCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;

namespace GameDll
{
    public class UHudComponent
    {
        private float m_HpTween = 0;
        private float m_MagicTween = 0;
        public UResource m_Resource;
        private PlayerHud m_Hud;
        public string m_Name = "";
        private float m_Exp;
        private long m_Level;
        private bool m_ShowLevel;
        private bool m_ShowExp;
        private HudType m_HudType;
        public void SetHudType(HudType type)
        {
            m_HudType = type;
        }
        public void SetShowExp(bool showExp)
        {
            m_ShowExp = showExp;
            if (m_Hud != null && m_Hud.m_imgExp != null)
            {

                RenderAPI.SetActiveIfNeed(m_Hud.m_imgExp.gameObject, showExp);

                if (showExp)
                {
                    SetExp(m_Exp);
                }
            }
        }
        public void SetShowLevel(bool showLevel)
        {
            m_ShowLevel = showLevel;
            if (m_Hud != null)
            {
                if (m_Hud.m_LevelBg != null)
                {
                    RenderAPI.SetActiveIfNeed(m_Hud.m_LevelBg.gameObject, showLevel);
                }
                else if(m_Hud.m_txtLevel != null)
                {
                    RenderAPI.SetActiveIfNeed(m_Hud.m_txtLevel.gameObject, showLevel);
                }
                if (showLevel)
                {
                    SetLevel(m_Level);
                }
            }
        }
        public void SetExp(float exp)
        {
            m_Exp = exp;
            if(m_Hud != null && m_Hud.m_imgExp != null && m_ShowExp)
            {
                m_Hud.m_imgExp.fillAmount = m_Exp;
            }
        }
        public void SetLevel(long level)
        {
            m_Level = level;
            if(m_Hud != null && m_Hud.m_txtLevel != null  && m_ShowLevel)
            {
                m_Hud.m_txtLevel.text = level.ToString();
            }
        }


        public bool m_HudShow = true;
        public bool m_HudShowName = false;
        private bool m_HudShowBlood = true;

        public void SetShowHudImp()
        {
            bool justOutScreen = true;
            if(justOutScreen)
            {
                var bar = m_Hud.m_HpBar;
                if(bar != null)
                {
                    bar.m_EnableUpdate = m_HudShow;
                }
                if(m_HudShow)
                {
                    //按理说可以看到的时候其他地方会设置该位置
                    m_Hud.m_HudRender.localPosition = new Vector3(0, 100000, 0);
                }
                else
                {
                    m_Hud.m_HudRender.localPosition = new Vector3(0, 100000, 0);
                    if (m_Resource != null)
                    {
                        HudManager.ResetOwnerHpTexts(m_Resource.GetId());
                    }
                }
            }
            else
            {
                RenderAPI.SetActiveIfNeed(m_Hud.m_HudRender.gameObject, m_HudShow);
            }

        }



        public void SetShowHud(bool show)
        {
            m_HudShow = show;
            if (m_Hud != null)
            {
                SetShowHudImp();
            }
        }

        public void SetShowHudNameImp()
        {
            if (m_Hud != null && m_Hud.m_HudName != null)
            {
                RenderAPI.SetActiveIfNeed(m_Hud.m_HudName.gameObject, m_HudShowName);
            }
        }
        public void SetShowHudName(bool show)
        {
            m_HudShowName = show;
            if (m_Hud != null)
            {
                SetShowHudNameImp();
            }
        }

        public void SetShowHudBloodImp()
        {
            if(m_Hud.m_Blood == null)
            {
                return;
            }
            RenderAPI.SetActiveIfNeed(m_Hud.m_Blood.gameObject, m_HudShowBlood);
            //RenderAPI.SetActiveIfNeed(m_Hud.m_LevelBg.gameObject, m_HudShowBlood);
        }
        public void SetShowHudBlood(bool show)
        {
            m_HudShowBlood = show;
            if (m_Hud != null)
            {
                SetShowHudBloodImp();
            }
        }

        public void EnableHudRender()
        {
            //这里引入了第三方需要异步加载的资源
            if (m_Hud != null)
            {
                EnableHudRenderImp();
            }
            else
            {
                m_Hud = HudManager.GetHud(m_HudType);
                if (m_Hud != null)
                {
                    EnableHudRenderImp();
                }
            }
        }


        private  void EnableHudRenderImp()
        {
            if (m_Resource.IsObjectLoaded())
            {
                EnableHudRenderImp_OnObjectLoaded();
            }
            else
            {
                m_Hud.transform.position = new Vector3(0, 1000000, 0);
                m_Resource.AddLoadedCall(EnableHudRenderImp_OnObjectLoaded);
            }

        }

        private  void EnableHudRenderImp_OnObjectLoaded()
        {
            if(m_Hud == null)
            {
                return;
            }
            var bar = m_Hud.m_HpBar;
            bar.m_WorldCamera = RenderAPI.GetWorldCamera();
            bar.m_UICamera = null;
            var render = m_Resource.GetShowObj() as GameObject;
            var collider = m_Resource.GetCollider();
            bar.target = render.transform;
            bar.target_collider = collider;
            bar.target_resource = m_Resource;
            bar.offsetPos = Vector3.zero;




            SetName(m_Name);
            SetHpValue(m_BloodValue, 0);
            SetMagicValue(m_MagicValue, 0);
            SetCampColor(m_CampColor);
            SetShowLevel(m_ShowLevel);
            SetShowExp(m_ShowExp);
            SetShowHudImp();
            SetShowHudNameImp();
            SetShowHudBloodImp();
        }


        private string m_CampColor = "";
        public void SetCampColor(string campColor)
        {
            m_CampColor = campColor;
            if(m_Hud != null)
            {
                Color color;
                if (m_Hud.m_HudName != null)
                {
                    if (ColorUtility.TryParseHtmlString(GameColor.HudName, out color))
                    {
                        m_Hud.m_HudName.color = color;
                    }
                }
                if(m_Hud.m_BloodImage != null)
                {
                    if (ColorUtility.TryParseHtmlString(campColor, out color))
                    {
                        m_Hud.m_BloodImage.color = color;
                    }
                }
                if (m_Hud.m_MagicImage != null)
                {
                    if (ColorUtility.TryParseHtmlString(GameColor.AddMagicColor, out color))
                    {
                        m_Hud.m_MagicImage.color = color;
                    }
                }
            }



        }


        private float m_BloodValue;
        private bool m_BloodDirty = false;
        public void SetHpValue(float cur, float tween = 0)
        {
            m_BloodDirty = true;
            m_BloodValue = cur;
            m_HpTween = tween;

        }
        private void SetHPValueImp()
        {
            if (m_Hud != null)
            {
                if (m_HpTween <= 0)
                {
                    if (m_Hud.m_Blood != null)
                    {
                        m_Hud.m_Blood.value = m_BloodValue;
                    }
                }
                else
                {
                    if (m_Hud.m_HpTweenSlider == null)
                    {
                        if (m_Hud.m_Blood != null)
                        {
                            m_Hud.m_Blood.value = m_BloodValue;
                        }
                    }
                    else
                    {
                        m_Hud.m_HpTweenSlider.from = m_Hud.m_Blood.value;
                        m_Hud.m_HpTweenSlider.to = m_BloodValue;
                        m_Hud.m_HpTweenSlider.m_Duration = m_HpTween;
                        m_Hud.m_HpTweenSlider.PlayForward();
                    }
                }
            }
        }



        private float m_MagicValue;

        private bool m_MagicDirty = false;
        public void SetMagicValue(float cur, float tween = 0)
        {
            m_MagicDirty = true;
            m_MagicValue = cur;
            m_MagicTween = tween;

        }
        private void SetMagicValueImp()
        {
            if (m_Hud != null)
            {
                if (m_MagicTween <= 0)
                {
                    if (m_Hud.m_Magic != null)
                    {
                        m_Hud.m_Magic.value = m_MagicValue;
                    }
                }
                else
                {
                    if (m_Hud.m_MagicTweenSlider == null)
                    {
                        if (m_Hud.m_Magic != null)
                        {
                            m_Hud.m_Magic.value = m_MagicValue;
                        }
                    }
                    else
                    {
                        m_Hud.m_MagicTweenSlider.from = m_Hud.m_Magic.value;
                        m_Hud.m_MagicTweenSlider.to = m_MagicValue;
                        m_Hud.m_MagicTweenSlider.m_Duration = m_MagicTween;
                        m_Hud.m_MagicTweenSlider.PlayForward();
                    }
                }
            }
        }

        public void SetName(string name)
        {
            m_Name = name;
            if(m_Hud != null && m_Hud.m_HudName != null)
            {
                m_Hud.m_HudName.text = name;
            }
        }

        public void ShowNumber(HpTextType type, string num, float size, Vector3 pos, string textColorHtml = null)
        {
            if (!m_HudShow)
            {
                return;
            }
            if(size == 0)
            {
#if UNITY_EDITOR
                UDebug.LogError("size == 0");
#endif
                size = 1;
            }
            HpText t = HudManager.GetHpText(type);
            if (t == null)
            {
                return;
            }
            t.m_OwnerEntityId = m_Resource != null ? m_Resource.GetId() : -1;
            if (type == HpTextType.Crit)
            {
                t.ResetTextColor();
            }
            else
            {
                ApplyHpTextColor(t, textColorHtml);
            }
            if (type == HpTextType.SkillHurt)
            {
                t.m_Text.text = num;
                t.m_TweenTimeTotal = 1.0f;
            }
            else if (type == HpTextType.Crit)
            {
                t.m_Text.text = num;
                t.m_TweenTimeTotal = 1.0f;
            }
            else if (type == HpTextType.Miss || 
                     type == HpTextType.NormalHurt)
            {
                t.m_Text.text = num;
                if (type == HpTextType.Miss)
                {
                    t.m_TweenTimeTotal = 3.0f;
                }
                else
                {
                    t.m_TweenTimeTotal = 1.0f;
                }
            }
            else if (type == HpTextType.AddGold)
            {
                //gold FFEA25 normal hurt 白色 
                t.m_Text.text = num;
                t.m_TweenTimeTotal = 2.0f;
            }
            else
            {
                t.m_Text.text = num;
                t.m_TweenTimeTotal = 1.0f;
            }
            //位置应该是3d里面的绝对位置
            t.m_StartPosition = pos;
            t.m_Size = Mathf.Abs(size);
            if(t.m_Size > 2)
            {
                t.m_Size = 2;
            }
            if(t.m_Size < 1)
            {
                t.m_Size = 1;
            }
            t.m_EndPosition = pos + new Vector3(0,1.0f,0);
            t.m_StartTime = Time.realtimeSinceStartup;

            t.m_State = HpText.State.Playing;
            t.m_WorldCamera = RenderAPI.GetWorldCamera();
            t.m_UICamera = RenderAPI.GetWorldCamera();
            t.UpdateOnce();
        }

        private static void ApplyHpTextColor(HpText hpText, string textColorHtml)
        {
            if (hpText == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(textColorHtml) &&
                ColorUtility.TryParseHtmlString(textColorHtml, out var parsedColor))
            {
                hpText.SetTextColor(parsedColor);
                return;
            }

            hpText.ResetTextColor();
        }



        public void Update()
        {
            if(m_BloodDirty)
            {
                m_BloodDirty = false;
                SetHPValueImp();
            }
            if(m_MagicDirty)
            {
                m_MagicDirty = false;
                SetMagicValueImp();
            }
        }
        public void DisableHudRender()
        {
            if (m_Hud != null)
            {
                HudManager.PoolHud(m_Hud);
                m_Hud = null;
            }
        }

        public void Destroy()
        {
            if (m_Hud != null)
            {
                HudManager.PoolHud(m_Hud);
                m_Hud = null;
            }
        }

        public void SetMoveSpeed(float speed)
        {
            if(m_Hud != null)
            {
                //m_Hud.m_HudName.text = m_Name + speed;
            }
        }
    }
}
