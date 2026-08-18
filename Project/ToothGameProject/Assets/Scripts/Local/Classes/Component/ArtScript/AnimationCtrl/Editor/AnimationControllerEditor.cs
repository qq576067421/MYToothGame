using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LCL
{
    [CustomEditor(typeof(AnimationController))]
    public class AnimationControllerEditor : Editor
    {
        private string m_PlayNode = "";
        public override void OnInspectorGUI()
        {

            AnimationController c = target as AnimationController;

            if (GUILayout.Button("初始化"))
            {
                if (c.m_Player == null)
                {
                    c.m_Player = c.gameObject.GetComponent<Animation>();
                    if (c.m_Player == null)
                    {
                        Debug.LogError("没有找到animation");
                        return;
                    }
                }
                else
                {
                    if (c.m_Animations != null && c.m_Animations.Count > 0)
                    {
                        Debug.Log("已经有节点数据，如果确实需要重新设置，请清空数组");
                        return;
                    }
                    else
                    {
                        var clips = AnimationUtility.GetAnimationClips(c.gameObject);
                        c.m_Animations = new List<AnimationController.AnimationNode>();
                        AnimationController.AnimationNode lastNode = null;
                        foreach (var clip in clips)
                        {
                            var node = new AnimationController.AnimationNode();
                            node.m_NodeName = clip.name;
                            node.m_LoopCount = clip.isLooping ? 100000 : 0;
                            node.m_Clip = clip;
                            node.m_NextNode = "";

                            if (lastNode != null)
                            {
                                lastNode.m_NextNode = node.m_NodeName;
                            }
                            c.m_Animations.Add(node);

                            lastNode = node;
                        }
                    }
                }
            }

            if (GUILayout.Button("设置NextNode"))
            {
                bool result = EditorUtility.DisplayDialog("警告", "需要重新设置下一个动画节点设置？", "确认", "取消");
                if (!result)
                {
                    return;
                }
                else
                {
                    var nodes = c.m_Animations;
                    int count = nodes.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        var node0 = nodes[i];
                        if (i < count - 1)
                        {
                            var node1 = nodes[i + 1];
                            node0.m_NextNode = node1.m_NodeName;
                        }
                    }

                }
            }

            m_PlayNode = EditorGUILayout.TextField("播放节点：", m_PlayNode);
            if (GUILayout.Button("播放"))
            {
                c.PlayAnim(m_PlayNode);
            }
            base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }


    }
}