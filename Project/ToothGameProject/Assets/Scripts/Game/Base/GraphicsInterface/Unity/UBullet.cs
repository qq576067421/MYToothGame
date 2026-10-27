using UnityEngine;
using System.Collections.Generic;
using LCL;

namespace GameDll
{
    public class UBullet:UEffect
    {
        private int m_AngularSpeed = 720;
        private float m_MoveSpeed = 1;
        private ProjectileMover m_Mover;
        private LineRenderer m_LineRenderer;

        private bool m_NeedEmit = false;
        private bool m_IsFollow;
        private Vector3 m_FollowTargetPosition;
        private static MaterialPropertyBlock s_BeamBendOffBlock;

        private void ApplyBeamBendOff()
        {
            if (s_BeamBendOffBlock == null)
            {
                s_BeamBendOffBlock = new MaterialPropertyBlock();
                s_BeamBendOffBlock.SetFloat(CurveWorldVisualUtility.BendOffPropertyName, 1f);
            }

            m_LineRenderer.SetPropertyBlock(s_BeamBendOffBlock);
        }

        protected override bool LoadShowObjImp(Object obj)
        {
            if (base.LoadShowObjImp(obj) == false)
            {
                return false;
            }
            m_Mover = m_GameObject.GetComponent<ProjectileMover>();
            m_LineRenderer = m_GameObject.GetComponentInChildren<LineRenderer>();
            if (m_LineRenderer != null)
            {
                // 带 LineRenderer 的子弹即激光束：材质关闭 Curve World 弯曲，保持直线渲染
                ApplyBeamBendOff();
                // 预制件自带的默认控制点/粒子不能出现在发弹首帧
                PrepareBeamForFire();
            }
            if (m_Mover != null)
            {
                m_Mover.Init(GameDll.RenderEffManager.GetInstance().GetRenderEffParent());


                m_Mover.SetIsFollow(m_IsFollow);

                m_Mover.SetFollowTargetPosition(m_FollowTargetPosition);
                

                if (m_NeedEmit)
                {
                    //重新设置一次位置
                    //SetPosition(m_Position);
                    SetInitPosition(m_Position);
                    m_Mover.Emit(m_MoveSpeed * 0.001f);
                }
                else
                {
                    m_Mover.StopEmit();
                }
                m_NeedEmit = false;

            }
            return true;
        }

        public override void LoadRender(string ab, string assetName)
        {
            m_ABName = ab;
            m_AssetName = assetName;

            LoadShowObjFromFileAsync(null);
        }
        public override void SetMoveSpeed(float speed)
        {
            m_MoveSpeed = speed;
        }
        public override void SetAngularSpeed(int speed)
        {
            m_AngularSpeed = speed;
        }

        public override void Update()
        {
            base.Update();
            if (m_TransformCache == null)
            {
                return;
            }
            //if (m_MoveStyle == 0)
            //{
            //    //将方向转换为四元数
            //    Quaternion quaDir = Quaternion.LookRotation(m_MoveForward, Vector3.up);
            //    float sp = Time.deltaTime * m_AngularSpeed / 180;
            //    //缓慢转动到目标点
            //    m_TransformCache.rotation = Quaternion.Lerp(m_TransformCache.rotation, quaDir, sp);
            //    float tr = Time.deltaTime * m_MoveSpeed / 0.11f;
            //    m_TransformCache.position = Vector3.Lerp(m_TransformCache.position, m_Position, tr);
            //}
            //else
            //{
                //var dir = m_FollowTargetPosition - m_TransformCache.position;
                //dir = dir.normalized;
                //m_TransformCache.forward = dir;
                //var speed = m_MoveSpeed * Time.deltaTime * 0.001f;
                //m_TransformCache.position = m_TransformCache.position + dir * speed;
                //UDebug.Log("pos:" + m_TransformCache.position);
            //}

        }

        public override void SetInitPosition(Vector3 pos)
        {
            m_Position = pos;

            if (IsObjectLoaded())
            {
                m_TransformCache.position = m_Position;
                OnUpdateHud();
            }
        }
        public override void BulletEmit()
        {
            m_NeedEmit = true;
            if (m_Mover != null)
            {
                m_NeedEmit = false;
                if (!m_Mover.ReadCanReuseFromPool())
                {
                    // 这里只处理客户端表现复用冲突，不能把表现层状态带回战斗逻辑做分支。
                    m_Mover.ForceStopForReuse();
                    base.SetActive(false);
                    base.SetActive(true);
                }
                m_Mover.Emit(m_MoveSpeed * 0.001f);
            }
        }
        public override void BulletBoom(Vector3 pos, Vector3 dir)
        {
            m_NeedEmit = false;
            if (m_Mover != null)
            {
                m_Mover.Boom(pos, dir);
            }
        }
        public override void SetActive(bool bshow)
        {
            if (!bshow && m_Mover != null)
            {
                m_Active = false;
                m_NeedEmit = false;
                m_Mover.DelayHide(true);
                return;
            }

            base.SetActive(bshow);
            if (bshow)
            {
                // 激活后再清一次：预制件上的 playOnAwake 粒子会随激活起播，必须同帧停掉，
                // 否则点普攻的第一帧会闪出预制件状态或上一发的激光粒子/命中特效。
                PrepareBeamForFire();
            }
        }

        // 激光束发弹/复用时的清理：光束控制点收成不可见退化两点、停掉粒子和命中/枪口特效。
        // 真正的光束由逻辑层（BulletObj.UpdateLaserBeam）随后的 SetBeamPoints 建立。
        // 只处理带 LineRenderer 的子弹（激光束），其它子弹保持原有激活行为。
        private void PrepareBeamForFire()
        {
            if (m_LineRenderer == null)
            {
                return;
            }

            if (m_Mover != null)
            {
                m_Mover.PrepareForEmit();
            }

            m_LineRenderer.positionCount = 2;
            m_LineRenderer.SetPosition(0, Vector3.zero);
            m_LineRenderer.SetPosition(1, Vector3.zero);
        }
        public override void Destroy()
        {
            if(m_Mover != null)
            {
                m_Mover.DestroyMover();
            }
            base.Destroy();
        }

        public  override void SetIsFollow(bool isFollow)
        {
            m_IsFollow = isFollow;
            if(m_Mover != null)
            {
                m_Mover.SetIsFollow(m_IsFollow);
            }
        }
        public override void SetTargetPosition(Vector3 pos)
        {
            m_FollowTargetPosition = pos;
            if (m_Mover != null)
            {
                m_Mover.SetFollowTargetPosition(m_FollowTargetPosition);
            }
        }

        public override void SetBeamPoints(Vector3[] points, int count)
        {
            if (m_LineRenderer == null || points == null || count < 2)
            {
                return;
            }

            // 激光类子弹：光束控制点用世界坐标直接写入 LineRenderer，与根节点位移无关。
            // 控制点数量由逻辑层决定（预弯折线），渲染前只对齐数量。
            if (m_LineRenderer.positionCount != count)
            {
                m_LineRenderer.positionCount = count;
            }
            m_LineRenderer.useWorldSpace = true;
            for (int i = 0; i < count; i++)
            {
                m_LineRenderer.SetPosition(i, points[i]);
            }
        }

    }
}
