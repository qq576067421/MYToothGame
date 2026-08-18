using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;
using LCL;
using DG.Tweening;
using GameDll;

namespace GameHot
{
    public class guide_wnd : WindowBase
    {
        private RectTransform m_WindowContent;
        private GuideMask m_Mask;
        private LUIImage m_FakeTarget;
        private GameObject m_Arrow;
        private v_guide_wnd m_View;
        private GuideStep m_Step;

        //超过一定次数显示跳过按钮
        private int m_ClickCount = 0;
        private Action m_OnOpenCall;
        private bool m_StepStarted = false;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Guide;
            __CustomUIPrefabDir = UIPrefabDirs.common;
        }
        protected override void OnInitComponent()
        {
            var wt = (RectTransform)__GetWindowTransform();
            wt.sizeDelta = UIManager.GetUICanvasScalerSize();


            m_View = new v_guide_wnd();
            m_View.InitComponent(__GetWindowObj());

            m_Mask = (GuideMask)m_View.m_guide_mask.GetComponent(typeof(GuideMask));
            m_Mask.ClickTargetCall = OnClickTarget;
            m_Arrow = m_View.m_lcl_guide_arrow.gameObject;
            m_FakeTarget = m_View.m_fake_target;

            m_Mask.IsClickTargetCall = IsClickTarget;
            m_Mask.IsBeginDragTargetCall = IsDragTarget;


            RenderAPI.AddButtonClick(m_View.m_btnJump, OnJump);

            var obj = __GetWindowObj();
            obj.SetActive(false);
            m_WindowContent = (RectTransform)__GetWindowObj().transform.Find("WindowContent").GetComponent(typeof(RectTransform));
        }



        protected override void OnOpen()
        {
            if (m_OnOpenCall != null)
            {
                m_OnOpenCall();
            }
        }
        protected override void OnClose()
        {
        }

        private Vector3 ConvertUIPos(GameObject target)
        {
            var rect = (RectTransform)target.GetComponent(typeof(RectTransform));
            if (rect != null)
            {
                Vector3 centerPos = rect.TransformPoint(rect.rect.center);
                var scrPos = RectTransformUtility.WorldToScreenPoint(Camera.main, centerPos);
                return Tool.ScreenPointToUGUI(m_WindowContent, scrPos, Camera.main);
            }
            else
            {
                UDebug.LogError("没有RectTransform");
                return Vector3.zero;
            }
        }
        private Vector3 Convert3DPos(GameObject target, Camera worldCamera)
        {
            var from = Tool.WorldToUGUIPoint(
                m_WindowContent,
                target.transform.position,
                worldCamera, Camera.main);
            return from;
        }
        public void UpdateGuide(GuideStep step, bool first)
        {
            if (!m_StepStarted)
            {
                return;
            }
            var cfg = step.m_StepCfg;
            //描述的偏移值的参考
            Vector3 offsetRef = Vector3.zero;
            if (step.m_Click == MaskClickDragStyle._2dclick || step.m_Click == MaskClickDragStyle._3dclick)
            {
                //点击操作
                if (step.m_IsUITarget0)
                {
                    //点击2d
                    if (step.m_Target0 != null)
                    {
                        var rect = (RectTransform)step.m_Target0.GetComponent(typeof(RectTransform));
                        if (rect != null)
                        {
                            m_Mask._Center = ConvertUIPos(step.m_Target0);
                            m_Mask._Width = rect.rect.width;
                            m_Mask._Height = rect.rect.height;

                            m_Mask._Radius = Math.Max(rect.rect.width, rect.rect.height) / 2;

                        }
                        else
                        {
                            UDebug.LogError("新手引导，蒙版自动大小错误");
                        }
                    }
                    else
                    {
                        //UDebug.LogError("点击UI一定要有target");
                    }
                }
                else
                {
                    if (step.m_Target0 != null)
                    {
                        //点击3d,不设置蒙版
                        m_Mask._Center = Convert3DPos(step.m_Target0, step.m_WorldCamera);
                        if (step.m_StepCfg.t_3d_mask_h > 0)
                        {
                            var t_3d_mask = new Vector2(step.m_StepCfg.t_3d_mask_w, step.m_StepCfg.t_3d_mask_h);
                            t_3d_mask = UIManager.ConvertDesigner2RealOffset(t_3d_mask);
                            m_Mask._Width = t_3d_mask.x;
                            m_Mask._Height = t_3d_mask.y;
                            m_Mask._Radius = Math.Max(t_3d_mask.x, t_3d_mask.y) / 2;
                        }
                        else
                        {
                            m_Mask._Width = 2000;
                            m_Mask._Height = 2000;
                            m_Mask._Radius = 2000;
                        }
                    }
                    else
                    {
                        //UDebug.LogError("点击3d一定要有target");
                    }
                }
                offsetRef = m_Mask._Center;
                var arrow_rect = (RectTransform)m_View.m_lcl_guide_arrow.GetComponent(typeof(RectTransform));
                var t_arrow_offset = new Vector2(cfg.t_arrow_x, cfg.t_arrow_y);
                t_arrow_offset = UIManager.ConvertDesigner2RealOffset(t_arrow_offset);
                arrow_rect.anchoredPosition = m_Mask._Center + t_arrow_offset;
                //var tween_position = (TweenPosition)m_View.m_lcl_guide_arrow.GetComponent(typeof(TweenPosition));
                //tween_position.enabled = false;
            }
            else
            {
                if (step.m_Target0 != null && step.m_Target1 != null)
                {
                    Vector3 from = Vector3.zero;
                    Vector3 to = Vector3.zero;
                    if (step.m_IsUITarget0)
                    {
                        from = ConvertUIPos(step.m_Target0);
                    }
                    else
                    {
                        from = Convert3DPos(step.m_Target0, step.m_WorldCamera);
                    }

                    if (step.m_IsUITarget1)
                    {
                        to = ConvertUIPos(step.m_Target1);
                    }
                    else
                    {
                        to = Convert3DPos(step.m_Target1, step.m_WorldCamera);
                    }

                    var cur_pos = m_View.m_lcl_guide_arrow.transform.position;
                    //var tween_position = (TweenPosition)m_View.m_lcl_guide_arrow.GetComponent(typeof(TweenPosition));
                    //tween_position.enabled = true;
                    //tween_position.from = from;
                    //tween_position.to = to;
                    //if (first)
                    //{
                    //    tween_position.PlayForward();
                    //    tween_position.m_Tweener.SetLoops(-1, LoopType.Restart);
                    //}
                    offsetRef = from;
                }
                else
                {
                    //UDebug.LogError("原则上拖动需要两个target");
                }
            }



            //描述的位置
            var desc_rect = (RectTransform)m_View.m_desc_rect.GetComponent(typeof(RectTransform));
            if (cfg.t_desc_rel == 0)
            {
                desc_rect.anchoredPosition = offsetRef;
            }
            else
            {
                //绝对位置
                desc_rect.anchoredPosition = Vector2.zero;
            }
            RenderAPI.SetTextLan(m_View.m_txtDesc, cfg.t_desc);

            var desc = (RectTransform)m_View.m_imgDesc.GetComponent(typeof(RectTransform));
            var t_desc_pos = new Vector2(cfg.t_desc_x, cfg.t_desc_y);
            t_desc_pos = UIManager.ConvertDesigner2RealOffset(t_desc_pos);
            desc.anchoredPosition = t_desc_pos;

            var t_desc_size = new Vector2(cfg.t_desc_w, cfg.t_desc_h);
            t_desc_size = UIManager.ConvertDesigner2RealOffset(t_desc_size);
            desc.sizeDelta = t_desc_size;

            m_FakeTarget.rectTransform.anchoredPosition = m_Mask._Center;
            m_FakeTarget.rectTransform.sizeDelta = new Vector2(m_Mask._Width, m_Mask._Height);

            if (first)
            {
                RenderAPI.SetActive(m_View.m_lcl_img_arrow0, cfg.t_arrow_type == 0);
                RenderAPI.SetActive(m_View.m_lcl_img_arrow1, cfg.t_arrow_type == 1);
                RenderAPI.SetActive(m_View.m_lcl_img_arrow2, cfg.t_arrow_type == 2);
                RenderAPI.SetActive(m_View.m_lcl_img_arrow3, cfg.t_arrow_type == 3);
                var maskImg = (Image)m_Mask.GetComponent(typeof(Image));
                if (step.m_Target0 == null || !step.m_BlackMask)
                {
                    maskImg.color = new Color(0, 0, 0, 2.0f / 255.0f);
                }
                else
                {
                    if (step.m_Click == MaskClickDragStyle._2dclick || step.m_Click == MaskClickDragStyle._3dclick)
                    {
                        maskImg.color = new Color(0, 0, 0, 163.0f/255.0f);
                    }
                    else
                    {
                        maskImg.color = new Color(0, 0, 0, 2.0f / 255.0f);
                    }
                }

                m_Mask._ClickGuide = step.m_Click == MaskClickDragStyle._2dclick || step.m_Click == MaskClickDragStyle._3dclick;
                m_Mask._2d_click = (int)step.m_Click;

                m_Mask._WorldCamera = step.m_WorldCamera;
                

                m_Mask.m_Shape = (GuideMaskShape)cfg.t_shape;
                m_Mask._PassEvent = step.m_PassEvent;

                m_ClickCount = 0;
                RenderAPI.SetActive(m_View.m_btnJump, false);
            }

            m_Mask._OnBeginDragCall = step._OnBeginDragCall;
            m_Mask._OnDraggingCall = step._OnDraggingCall;
            m_Mask._OnEndDragCall = step._OnEndDragCall;
            m_Mask.IsClickAnyNext = step.m_StepCfg.t_finish_step_style == 2;

            m_Mask.Apply();



        }
        private void OnShowGuide()
        {
            m_OnOpenCall = null;
            var obj = __GetWindowObj();
            obj.SetActive(false);

            var step = m_Step;

            if (step.m_StepCfg.t_close_pop == 1)
            {
                var wins = UIManager.GetWindows(WindowLayer.Popup);
                foreach (var win in wins)
                {
                    UIManager.CloseWindow(win);
                }
            }

            if (step.OnBeforeGuideCall != null)
            {
                step.OnBeforeGuideCall();
                step.OnBeforeGuideCall = null;
            }

            if (step.m_Update)
            {
                long id = 0;
                id = CounterManager.GetInstance().AddCounter(step.m_UpdateTimeInterval, -1, () =>
                {
                    if ((int)LobbyPlayer.GuideMgr.GetCurrentId() == step.m_StepCfg.t_id)
                    {
                        LobbyPlayer.GuideMgr.UpdateStep(step);
                    }
                    else
                    {
                        CounterManager.GetInstance().RemoveCounter(id);
                    }
                });

            }
            StepStart();
            //更新一次
            UpdateGuide(step, true);

        }
        public void ShowGuide(GuideStep step)
        {
            m_Step = step;
            var obj = __GetWindowObj();
            if (obj == null)
            {
                m_OnOpenCall = OnShowGuide;
            }
            else
            {
                OnShowGuide();
            }

        }

        private void StepStart()
        {
            m_StepStarted = true;

            if(m_Step.m_StepCfg.t_story != 0)
            {
                Action story_finish = () => 
                {
                    LobbyPlayer.GuideMgr.NextStep();
                };
                //UIManager.OpenWindowEX<battle_story_wnd>(null, m_Step.m_StepCfg.t_story, story_finish);
                return;
            }
            var obj = __GetWindowObj();
            obj.SetActive(true);
        }

        private void OnJump()
        {
            LobbyPlayer.GuideMgr.JumpStep();
        }
        public void StepEnd()
        {
            HideMask();
            m_Step = null;
        }


        private void OnClickTarget(GuideParam param)
        {
            bool clickTarget = param.m_IsClick;
            GuideMaskEventType evt = param.m_EventType;
            if (evt == GuideMaskEventType.PointerClick)
            {

                if (!clickTarget)
                {
                    m_ClickCount++;
                    if (m_ClickCount >= 5 && m_Step.m_StepCfg.t_jump == 1)
                    {
                        RenderAPI.SetActive(m_View.m_btnJump, true);
                    }
                }
                else
                {
                    m_ClickCount = 0;
                    var finish_step_style = m_Step.m_StepCfg.t_finish_step_style;
                    if (finish_step_style == (int)GuideStepFinishStyle.d点击 || finish_step_style == (int)GuideStepFinishStyle.da点击任意)
                    {
                        LobbyPlayer.GuideMgr.NextStep();
                    }
                }
            }

        }

        private void HideMask()
        {
            var obj = __GetWindowObj();
            obj.SetActive(false);
        }

        private bool IsDragTarget(GuideParam param)
        {
            GameObject target = param.m_ClickObj;
            GuideMaskEventType evtType = param.m_EventType;

            if (evtType != GuideMaskEventType.BeginDrag)
            {
                return false;
            }
            if (m_Step.IsDragTargetCall != null)
            {
                return m_Step.IsDragTargetCall(target);
            }
            else if (m_Step.m_Target0 != null)
            {
                return target == m_Step.m_Target0;
            }
            else if (target == m_FakeTarget.gameObject)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsClickTarget(GuideParam param)
        {
            GameObject target = param.m_ClickObj;
            GuideMaskEventType evtType = param.m_EventType;
            Vector3 pos = param.m_Position;

            if (evtType != GuideMaskEventType.PointerClick)
            {
                return false;
            }
            if (m_Step.IsClickTargetCall != null)
            {
                return m_Step.IsClickTargetCall(target, pos);
            }
            else if (m_Step.m_Target0 != null)
            {
                return target == m_Step.m_Target0;
            }
            else if (target == m_FakeTarget.gameObject)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
