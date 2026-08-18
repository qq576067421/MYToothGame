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

        private bool m_NeedEmit = false;
        private bool m_IsFollow;
        private Vector3 m_FollowTargetPosition;
        protected override bool LoadShowObjImp(Object obj)
        {
            if (base.LoadShowObjImp(obj) == false)
            {
                return false;
            }
            m_Mover = m_GameObject.GetComponent<ProjectileMover>();
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
            if(m_Mover != null)
            {
                m_Mover.SetFollowTargetPosition(m_FollowTargetPosition);
            }
        }

    }
}
