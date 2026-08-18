using System.Collections;
using UnityEngine;

namespace LCL
{
    using System.Collections.Generic;

    using UnityEngine;

    [RequireComponent(typeof(LineRenderer))]

    [ExecuteInEditMode]  //普通的类，加上ExecuteInEditMode， 就可以在编辑器模式中运行
    public class ChainLightning : MonoBehaviour
    {

        public float detail = 1;//增加后，线条数量会减少，每个线条会更长。  

        public float displacement = 15;//位移量，也就是线条数值方向偏移的最大值  

        public Vector3 EndPosition;
        public Transform EndTarget;//链接目标  

        public Transform StartTarget;
        public Vector3 StartPosition;

        public float m_UpdateDist = 0.01f;

        private Vector3 m_LastEndPosition;
        private Vector3 m_LastStartPosition;

        public float yOffset = 0;

        public LineRenderer _lineRender;

        public List<Vector3> m_PosList = new List<Vector3>();

        private Vector3 _position;



        private void Update()

        {

            //判断是否暂停，未暂停则进入分支

            if (Time.timeScale != 0)
            {



                Vector3 startPos = Vector3.zero;

                Vector3 endPos = Vector3.zero;

                if (EndTarget != null)
                {

                    endPos = EndTarget.position + Vector3.up * yOffset;

                }
                else
                {
                    endPos = EndPosition;
                }
                bool updateLineStart = true;
                bool updateLineEnd = true;
                var endDist = Vector3.Distance(endPos, m_LastEndPosition);
                if(endDist < m_UpdateDist)
                {
                    updateLineEnd = false;
                }


                if (StartTarget != null)
                {
                    startPos = StartTarget.position + Vector3.up * yOffset;
                }
                else
                {
                    startPos = StartPosition;
                }

                var startDist = Vector3.Distance(startPos, m_LastStartPosition);
                if(startDist < m_UpdateDist)
                {
                    updateLineStart = false;
                }

                if(updateLineStart || updateLineEnd)
                {
                    m_PosList.Clear();
                    m_LastEndPosition = endPos;
                    m_LastStartPosition = startPos;
                    //获得开始点与结束点之间的随机生成点
                    CollectLinPos(startPos, endPos, displacement);
                    m_PosList.Add(endPos);
                    //把点集合赋给LineRenderer
                    _lineRender.positionCount = m_PosList.Count;
                    for (int i = 0, n = m_PosList.Count; i < n; i++)
                    {
                        _lineRender.SetPosition(i, m_PosList[i]);
                    }
                }
            }

        }

        //收集顶点，中点分形法插值抖动  

        private void CollectLinPos(Vector3 startPos, Vector3 destPos, float displace)

        {

            //递归结束的条件

            if (displace < detail)

            {

                m_PosList.Add(startPos);

            }

            else

            {

                float midX = (startPos.x + destPos.x) / 2;

                float midY = (startPos.y + destPos.y) / 2;

                float midZ = (startPos.z + destPos.z) / 2;

                midX += (float)(UnityEngine.Random.value - 0.5) * displace;

                midY += (float)(UnityEngine.Random.value - 0.5) * displace;

                midZ += (float)(UnityEngine.Random.value - 0.5) * displace;

                Vector3 midPos = new Vector3(midX, midY, midZ);

                //递归获得点

                CollectLinPos(startPos, midPos, displace / 2);

                CollectLinPos(midPos, destPos, displace / 2);

            }

        }

    }

}