using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCL
{
    [RequireComponent(typeof(Animation))]
    public class AnimationController : MonoBehaviour
    {
        [System.Serializable]
        public class AnimationNode
        {
            public string m_NodeName;
            public int m_LoopCount = 0;
            public AnimationClip m_Clip;
            public string m_NextNode;
            public System.Action<string> m_OnEndCall;
            public System.Action<string> m_OnStartCall;
        }

        public string m_StartNodeName;
        [SerializeField]
        public List<AnimationNode> m_Animations = new List<AnimationNode>();
        public Animation m_Player = null;
        public float m_FadeLength = 0.3f;
        public PlayState m_PlayState = PlayState.None;

        public AnimationNode m_CurNode;
        private float m_CurStartTime = 0;
        private int m_CurLoopCount = 0;

        public enum PlayState
        {
            None,
            Playing,
            Pause,
            Stop
        }

        void Start()
        {
            if (string.IsNullOrEmpty(m_StartNodeName))
            {

            }
            else
            {
                m_CurNode = FindNode(m_StartNodeName);
                if (m_CurNode != null && m_CurNode.m_Clip != null)
                {
                    m_Player.CrossFade(m_CurNode.m_Clip.name, m_FadeLength);
                    m_CurStartTime = Time.realtimeSinceStartup;
                    m_CurLoopCount = 0;
                    m_PlayState = PlayState.Playing;

                    if (m_CurNode.m_OnStartCall != null)
                    {
                        m_CurNode.m_OnStartCall(m_CurNode.m_NodeName);
                    }
                }
            }

        }
        public void PlayAnim(string anim)
        {
            if (string.IsNullOrEmpty(anim))
            {

            }
            else
            {
                m_CurNode = FindNode(anim);
                if (m_CurNode != null && m_CurNode.m_Clip != null)
                {
                    m_Player.CrossFade(m_CurNode.m_Clip.name, m_FadeLength);
                    m_CurStartTime = Time.realtimeSinceStartup;
                    m_CurLoopCount = 0;
                    m_PlayState = PlayState.Playing;

                    if (m_CurNode.m_OnStartCall != null)
                    {
                        m_CurNode.m_OnStartCall(m_CurNode.m_NodeName);
                    }
                }
            }
        }
        void Update()
        {
            if (m_CurNode != null)
            {
                bool curPlayFinish = false;
                var clip = m_CurNode.m_Clip;
                if (clip != null)
                {
                    float clipLength = clip.length;
                    if (Time.realtimeSinceStartup - m_CurStartTime >= clipLength)
                    {
                        //播放完毕
                        if (m_CurNode.m_LoopCount == 0)
                        {
                            curPlayFinish = true;
                            if (m_CurNode.m_OnEndCall != null)
                            {
                                m_CurNode.m_OnEndCall(m_CurNode.m_NodeName);
                            }
                        }
                        else
                        {
                            //需要播放多次
                            if (m_CurLoopCount >= m_CurNode.m_LoopCount)
                            {
                                curPlayFinish = true;
                                if (m_CurNode.m_OnEndCall != null)
                                {
                                    m_CurNode.m_OnEndCall(m_CurNode.m_NodeName);
                                }
                            }
                            else
                            {
                                m_CurLoopCount++;
                                m_CurStartTime = Time.realtimeSinceStartup;
                                if (!m_CurNode.m_Clip.isLooping)
                                {
                                    m_Player.CrossFade(m_CurNode.m_Clip.name, 0.3f);
                                }

                                if (m_CurNode.m_OnEndCall != null)
                                {
                                    m_CurNode.m_OnEndCall(m_CurNode.m_NodeName);
                                }
                                if (m_CurNode.m_OnStartCall != null)
                                {
                                    m_CurNode.m_OnStartCall(m_CurNode.m_NodeName);
                                }
                            }
                        }

                    }

                }

                if (curPlayFinish)
                {
                    var nextNodeName = m_CurNode.m_NextNode;
                    if (string.IsNullOrEmpty(nextNodeName))
                    {
                        m_PlayState = PlayState.Stop;
                        m_CurNode = null;
                    }
                    else
                    {
                        m_CurNode = FindNode(nextNodeName);
                        if (m_CurNode != null)
                        {
                            m_Player.CrossFade(m_CurNode.m_Clip.name, m_FadeLength);
                            m_CurStartTime = Time.realtimeSinceStartup;
                            m_CurLoopCount = 0;
                            m_PlayState = PlayState.Playing;

                            if (m_CurNode.m_OnStartCall != null)
                            {
                                m_CurNode.m_OnStartCall(m_CurNode.m_NodeName);
                            }
                        }
                        else
                        {
                            m_PlayState = PlayState.Stop;
                            m_CurNode = null;
                        }
                    }
                }
            }
        }

        private AnimationNode FindNode(string nodeName)
        {
            int count = m_Animations.Count;
            for (int i = 0; i < count; ++i)
            {
                var ani = m_Animations[i];
                if (ani != null && ani.m_Clip != null && ani.m_NodeName == nodeName)
                {
                    return ani;
                }
            }
            return null;
        }
    }
}