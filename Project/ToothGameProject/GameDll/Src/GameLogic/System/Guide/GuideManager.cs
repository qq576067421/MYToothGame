using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using GameDll;

namespace GameHot
{
    public enum GuideStepState
    {
        _0普通步骤,
        _1保存,
        _2最后一步,
        _3保存最后一步
    }
    public enum GuideStepFinishStyle
    {
        d点击,
        s手动,
        da点击任意
    }

    // 0 2d点击 1 2d 拖动  3 3d点击 4 3d拖动
    public enum MaskClickDragStyle
    {
        _2dclick,
        _2ddrag,
        _3dclick,
        _3ddrag

    }
    public class GuideStep
    {
        //配置表id
        public t_guideBean m_StepCfg;
        //穿透点击对象
        public bool m_IsUITarget0 = true;
        public GameObject m_Target0;
        public bool m_IsUITarget1 = true;
        public GameObject m_Target1;

        public Camera m_WorldCamera;
        public MaskClickDragStyle m_Click = MaskClickDragStyle._2dclick;
        //点击事件将点击在mask上的事件传递给下一层的按钮等，默认是需要传递的
        public bool m_PassEvent = true;
        public bool m_Update = false;
        public int m_UpdateTimeInterval = 50;
             

        //当穿透对象为空时候，可以使用该方法手动判断点击情况
        public System.Func<GameObject, Vector3, bool> IsClickTargetCall;
        public System.Func<GameObject, bool> IsDragTargetCall;
        public Action OnBeforeGuideCall;

        public Action<Vector2> _OnBeginDragCall;
        public Action<Vector2> _OnDraggingCall;
        public Action<Vector2> _OnEndDragCall;

        //public bool m_FinishStepTriggerNextStep;
        //public bool m_FinishStepTriggerNextGroup;

        public Action StepFinishBeforeCall;
        public Action<guide_group, guide_step_id> StepFinishChangeNextCall;
        public bool m_BlackMask = true;
    }
    public class GuideManager : SystemBaseManager
    {
        private string m_PlayerId;
        private guide_group m_CurGroup;
        private bool m_CurGroupSaved = false;

        private List<GuideStep> m_GuideSteps = new List<GuideStep>();
        private List<guide_step_id> m_GuideIdAdded = new List<guide_step_id>();
        private GuideStep m_CurStep;
        private guide_wnd m_GuideWnd;
        private bool m_EnableGuide = true;
        public Action<guide_group, guide_step_id> OnGuideStepFinishCall;
        private static GuideManager m_Instance;
        private static GuideManager m_InstanceEvent;
        private guide_step_id m_LastGuideId = guide_step_id.None;
        private Action<guide_group> m_GuideSaveCall;
        //查询guide是否曾经完成过
        private Func<int, bool> m_ParseGuideDoCall;
        //当前引导管理器是否是按需触发（非必然触发的常规引导）
        private bool m_IsEvent = false;
        public void SetGuideSaveCall(Action<guide_group> call)
        {
            m_GuideSaveCall = call;
        }
        public void SetParseGuideDoCall(Func<int, bool> call)
        {
            m_ParseGuideDoCall = call;
        }

        public void GMAddGuide(string playerId, guide_group group)
        {
            m_PlayerId = playerId;
            m_CurGroup = group;
        }
        //初始化窗口
        public override void Init()
        {
            m_PlayerId = "";
            m_CurGroup = guide_group.None;
            m_CurGroupSaved = false;
            m_GuideSteps.Clear();
            m_GuideIdAdded.Clear();
            m_CurStep = null;
            OnGuideStepFinishCall = null;
            m_LastGuideId = guide_step_id.None;
            m_GuideSaveCall = null;
            m_ParseGuideDoCall = null;
            m_IsEvent = false;

            //暂时注释掉，这个是用来保存新手引导步骤的回调
            //LobbyPlayer.GuideMgr.SetGuideSaveCall(LoginMessage.GetInstance().Send_CSC_SaveGuideGroup);
            m_EnableGuide = true;
            if (m_GuideWnd == null || m_GuideWnd.IsLogicClosed())
            {
                m_GuideWnd = UIManager.OpenWindowEX<guide_wnd>(null);
            }

        }

        public override void UnInit()
        {
            m_PlayerId = "";
            m_CurGroup = guide_group.None;
            m_CurGroupSaved = false;
            m_GuideSteps.Clear();
            m_GuideIdAdded.Clear();
            m_CurStep = null;
            if(m_GuideWnd != null)
            {
                UIManager.CloseWindow(m_GuideWnd);
                m_GuideWnd = null;
            }
            OnGuideStepFinishCall = null;
            m_LastGuideId = guide_step_id.None;
            m_GuideSaveCall = null;
            m_ParseGuideDoCall = null;
            m_IsEvent = false;
        }

        public override void OnReceivedMainStartMessage(object msg)
        {
            //var playerId = LobbyPlayer.GetInstance().PlayerId;
            //var server_group = LobbyPlayer.GetInstance().m_PlayerInfo.guide_group;
            //m_PlayerId = playerId.ToString();
            //if (server_group == 0)
            //{
            //    m_CurGroup = guide_group.g_welcome;
            //}
            //else
            //{
            //    m_CurGroup = (guide_group)server_group;
            //}

            //if (RenderAPI.IsJumpAllGuide())
            //{
            //    LobbyPlayer.GuideMgr.SetEnable(false);
            //    LobbyPlayer.GuideInstanceMgr.SetEnable(false);
            //}
        }

        public override void OnReceivedSystemStartMessage(object msg)
        {

        }


        public void InitEvent()
        {
            m_EnableGuide = true;
            if (m_GuideWnd == null || m_GuideWnd.IsLogicClosed())
            {
                m_GuideWnd = UIManager.OpenWindowEX<guide_wnd>(null);
            }
        }
        public void Test(guide_group group)
        {
            m_EnableGuide = true;
            if (m_GuideWnd == null || m_GuideWnd.IsLogicClosed())
            {
                m_GuideWnd = UIManager.OpenWindowEX<guide_wnd>(null);
            }

            m_PlayerId = "0";
            m_CurGroup = group;
        }
        public guide_step_id GetLastGuideId()
        {
            return m_LastGuideId;
        }
        public bool IsGuideIdInGroup(guide_step_id guideId)
        {
            if ((int)m_CurGroup != (int)guideId / 100)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public bool IsGroup(guide_group group, guide_step_id checkLastId)
        {
            if (m_CurGroup != group)
            {
                return false;
            }
            else
            {
                if (checkLastId != guide_step_id.None)
                {
                    if (m_LastGuideId == checkLastId)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        public guide_step_id GetCurrentId()
        {
            if (m_CurStep != null)
            {
                return (guide_step_id)m_CurStep.m_StepCfg.t_id;
            }
            else
            {
                return guide_step_id.None;
            }
        }


        public bool IsDoEventGroup(int guideId)
        {
            if(m_ParseGuideDoCall(guideId / 100))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void BeginEventGroup(guide_step_id guideId)
        {
            m_CurGroup = (guide_group)((int)guideId / 100);
        }
        public bool AddStep(guide_step_id guideId, Action<GuideStep> addCall, bool update = false)
        {
            if (!m_EnableGuide)
            {
                return false;
            }
            if ((int)m_CurGroup != (int)guideId / 100)
            {
                return false;
            }

            int count = m_GuideSteps.Count;
            for (int i = 0; i < count; ++i)
            {
                if (m_GuideSteps[i].m_StepCfg.t_id == (int)guideId)
                {
                    //重复加
                    return false;
                }
            }

            var cfg = t_guideBean.GetConfig((int)guideId);
            if (cfg == null)
            {
                //新手引导做完了？
                UDebug.LogError("引导配置没有找到：" + guideId.ToString());
                return false;
            }

            bool findAdded = false;
            count = m_GuideIdAdded.Count;
            for (int i = 0; i < count; ++i)
            {
                if (m_GuideIdAdded[i] == guideId)
                {
                    findAdded = true;
                }
            }
            if (findAdded)
            {
                return false;
            }
            m_GuideIdAdded.Add(guideId);

            GuideStep step = new GuideStep();
            step.m_StepCfg = cfg;
            step.m_BlackMask = cfg.t_black == 0;
            step.m_Update = update;
            m_GuideSteps.Add(step);
            if (addCall != null)
            {
                addCall(step);
            }
            return true;
        }
        public void StartSteps()
        {
            if(!m_EnableGuide)
            {
                return;
            }
            if(m_GuideSteps.Count == 0)
            {
                return;
            }
            if (m_CurStep == null)
            {
                m_CurStep = m_GuideSteps[0];
                m_GuideSteps.RemoveAt(0);
                m_GuideWnd.ShowGuide(m_CurStep);
            }
        }
        public guide_group GetNextGroup(guide_group current_group)
        {
            //Id（分组 * 100 + 步骤 步骤从1开始）
            var cfg = t_guideBean.GetConfig((int)current_group * 100 + 1);
            return (guide_group)cfg.t_next_group_id;
        }
        public void GroupBreak()
        {
            if(m_CurGroupSaved)
            {
                JumpGroup();
            }
            else
            {
                //还原到这组开始
                m_CurGroupSaved = false;
                m_CurStep = null;
                m_GuideSteps.Clear();
                ClearGroupAddedStepIds(m_CurGroup);

                if (m_GuideWnd != null)
                {
                    m_GuideWnd.StepEnd();
                }

                CGameProcedure.Event.OnCheckGuideGroup();
            }
        }
        private void ClearGroupAddedStepIds(guide_group group)
        {
            bool findAdded = false;
            int count = m_GuideIdAdded.Count;
            for (int i = count -1; i >= 0; --i)
            {
                var guideId = m_GuideIdAdded[i];
                if ((int)group == (int)guideId / 100)
                {
                    m_GuideIdAdded.RemoveAt(i);
                }
            }
        }
        public void UpdateStep(GuideStep step)
        {
            m_GuideWnd.UpdateGuide(step, false);
        }
        public void JumpGroup()
        {
            m_CurGroup = GetNextGroup(m_CurGroup);
            m_CurGroupSaved = false;
            m_CurStep = null;
            m_GuideSteps.Clear();

            if (m_GuideWnd != null)
            {
                m_GuideWnd.StepEnd();
            }

            CGameProcedure.Event.OnCheckGuideGroup();
        }
        public void JumpStep()
        {
            var cfg = m_CurStep.m_StepCfg;
            m_LastGuideId = (guide_step_id)cfg.t_id;
            m_CurStep = null;
            var next_group = GetNextGroup(m_CurGroup);
            SaveGroup(next_group);
            //开始进入到下一组引导
            m_CurGroup = next_group;
            m_CurGroupSaved = false;
            m_GuideSteps.Clear();

            if (m_GuideWnd != null)
            {
                m_GuideWnd.StepEnd();
            }

            CGameProcedure.Event.OnCheckGuideGroup();
        }
        public void NextStep()
        {
            if (m_GuideWnd != null)
            {
                m_GuideWnd.StepEnd();
            }
            var group = m_CurGroup;
            var guideId = (guide_step_id)m_CurStep.m_StepCfg.t_id;
            var cfg = m_CurStep.m_StepCfg;
            var lastStep = m_CurStep;
            bool trigger_next_step = m_CurStep.m_StepCfg.t_trigger_next_step == 1;
            bool trigger_next_group = m_CurStep.m_StepCfg.t_trigger_next_group == 1;

            m_LastGuideId = (guide_step_id)cfg.t_id;

            if (lastStep.StepFinishBeforeCall != null)
            {
                lastStep.StepFinishBeforeCall();
            }

            if (cfg.t_step_state == (int)GuideStepState._0普通步骤)
            {

                m_CurStep = null;
                if (m_GuideSteps.Count > 0)
                {
                    m_CurStep = m_GuideSteps[0];
                    m_GuideSteps.RemoveAt(0);
                    m_GuideWnd.ShowGuide(m_CurStep);
                }
                else
                {
                    //等待其他地方添加该分组的其他步骤
                    if (OnGuideStepFinishCall != null)
                    {
                        var call = OnGuideStepFinishCall;
                        OnGuideStepFinishCall = null;
                        call(group, guideId);
                    }

                }
            }
            else if (cfg.t_step_state == (int)GuideStepState._1保存)
            {
                //保存步骤的地方,先保存到本地
                m_CurStep = null;
                SaveGroup(GetNextGroup(group));

                if (m_GuideSteps.Count > 0)
                {
                    m_CurStep = m_GuideSteps[0];
                    m_GuideSteps.RemoveAt(0);
                    m_GuideWnd.ShowGuide(m_CurStep);
                }
                else
                {
                    //等待其他地方添加该分组的其他步骤
                    if (OnGuideStepFinishCall != null)
                    {
                        var call = OnGuideStepFinishCall;
                        OnGuideStepFinishCall = null;
                        call(group, guideId);
                    }
                }
            }
            else if (cfg.t_step_state == (int)GuideStepState._2最后一步)
            {
                m_CurStep = null;
                //开始进入到下一组引导
                m_CurGroup = GetNextGroup(group);
                m_CurGroupSaved = false;

                if (OnGuideStepFinishCall != null)
                {
                    var call = OnGuideStepFinishCall;
                    OnGuideStepFinishCall = null;
                    call(group, guideId);
                }
            }
            else if (cfg.t_step_state == (int)GuideStepState._3保存最后一步)
            {
                m_CurStep = null;
                SaveGroup(GetNextGroup(group));
                //开始进入到下一组引导
                m_CurGroup = GetNextGroup(group);
                m_CurGroupSaved = false;

                if (OnGuideStepFinishCall != null)
                {
                    var call = OnGuideStepFinishCall;
                    OnGuideStepFinishCall = null;
                    call(group, guideId);
                }
            }

            //触发下一步或者组
            if(trigger_next_step)
            {
                CGameProcedure.Event.OnGuideStepFinish(group, guideId);
            }
            if(trigger_next_group)
            {
                CGameProcedure.Event.OnCheckGuideGroup();
            }

            if (lastStep.StepFinishChangeNextCall != null)
            {
                lastStep.StepFinishChangeNextCall(group, guideId);
            }
        }

        //外部逻辑手动结束当前新手引导
        public void StepEnd(int guideId)
        {
            if (m_CurStep != null && m_CurStep.m_StepCfg.t_id == guideId)
            {
                if (m_GuideWnd != null)
                {
                    m_GuideWnd.StepEnd();
                }

                NextStep();
            }
        }

        public void SaveGroup(guide_group nextGroupId)
        {
            m_CurGroupSaved = true;
            if (m_GuideSaveCall != null)
            {
                m_GuideSaveCall(nextGroupId);
            }
        }

        public void PrintGuide(RectTransform rect)
        {
            var obj = m_GuideWnd.__GetWindowObj();
            if (obj != null)
            {
                var parent = (RectTransform)obj.GetComponent(typeof(RectTransform));

                var scrPos = Tool.UGUIToScreenPoint(rect, Camera.main);
                var center = Tool.ScreenPointToUGUI(parent, scrPos, Camera.main);
                var width = rect.rect.width;
                var height = rect.rect.height;
                var radius = Math.Max(rect.rect.width, rect.rect.height) / 2;
                UDebug.Log("guide target info: center(" + center.ToString() + ")" + " width(" + width.ToString() + ")" +
                    " height(" + height.ToString() + ")" + " radius(" + radius.ToString() + ")");
            }
            else
            {
                UDebug.LogError("guide_wnd 没有打开");
            }

        }

        public void SetEnable(bool enable)
        {
            m_EnableGuide = enable;
        }
        public bool IsEnable()
        {
            return m_EnableGuide;
        }


    }
}
