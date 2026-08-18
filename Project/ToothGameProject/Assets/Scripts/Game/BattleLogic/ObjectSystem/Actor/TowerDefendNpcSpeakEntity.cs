namespace GameDll
{
    using MonoBean;
    using UnityEngine;

    public sealed class TowerDefendNpcSpeakEntity : Entity
    {
        private bool m_Visible = true;
        private t_monsterBean m_Bean;
        private Transform m_Parent;
        private System.Action<TowerDefendNpcSpeakEntity> m_LoadedCallback;
        private bool m_IsLoadSettled;
        private Transform m_HitRoot;
        private Transform m_HitTransform;
        private bool m_HitTransformResolved;
        private Transform m_ColliderRoot;
        private CapsuleCollider m_RootCapsuleCollider;
        private SphereCollider m_RootSphereCollider;
        private bool m_RootColliderResolved;

        public override bool ReadVisiable()
        {
            return m_Visible;
        }

        public override void SetVisiable(bool visiable)
        {
            m_Visible = visiable;
            base.SetVisiable(visiable);
        }

        public void Initialize(
            int id,
            t_monsterBean bean,
            Transform parent,
            System.Action<TowerDefendNpcSpeakEntity> loadedCallback)
        {
            InitInstance();
            SetId(id);
            SetObjectType(emEntityType.em_EntityType_Actor);
            m_Bean = bean;
            m_Parent = parent;
            m_LoadedCallback = loadedCallback;
            SetPosition(parent != null ? parent.position : Vector3.zero);
            SetScale(Vector3.one);
            CreateRender(null, ResourceType.Actor);
            if (m_IsLoadSettled || ReadIsDestroy() || GetRender() == null)
            {
                return;
            }

            AddLoadedCall(OnLoaded);
        }

        public Vector3 ReadSpeakWorldPosition()
        {
            return m_Parent != null ? m_Parent.position : GetPosition();
        }

        protected override float GetHitPointCfgValue()
        {
            return m_Bean != null ? m_Bean.t_hit_point / 1000.0f : 0f;
        }

        public override float ReadRadius()
        {
            if (m_Bean == null || m_Bean.t_size == 0)
            {
                return 0.4f;
            }

            return Mathf.Max(0.1f, m_Bean.t_size / 1000.0f * 0.5f);
        }

        public override Vector3 ReadHitPoint()
        {
            if (TryResolveHitTransform(out var hitTransform))
            {
                return hitTransform.position;
            }

            return base.ReadHitPoint();
        }

        public override bool TryIntersectSegment(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            float extraRadius,
            out float hitT,
            out Vector3 hitPoint)
        {
            extraRadius = Mathf.Max(0.0f, extraRadius);

            // 喊话展示实体不是实际战斗目标，所以运行时会把 Collider 关闭。
            // 这里仍然需要把根节点碰撞体当作纯几何数据使用，否则瞄准线只能退回到 hit 点小球，光球会明显偏难触发。
            if (TryReadRootCapsule(out var capsuleStart, out var capsuleEnd, out var capsuleRadius))
            {
                return BattleManager.TryIntersectSegmentCapsule(
                    segmentStart,
                    segmentEnd,
                    capsuleStart,
                    capsuleEnd,
                    capsuleRadius + extraRadius,
                    out hitT,
                    out hitPoint);
            }

            if (TryReadRootSphere(out var sphereCenter, out var sphereRadius))
            {
                return BattleManager.TryIntersectSegmentSphere(
                    segmentStart,
                    segmentEnd,
                    sphereCenter,
                    sphereRadius + extraRadius,
                    out hitT,
                    out hitPoint);
            }

            return base.TryIntersectSegment(segmentStart, segmentEnd, extraRadius, out hitT, out hitPoint);
        }

        public override void CreateRender(UResource obj, ResourceType resourceType)
        {
            if (m_Bean == null)
            {
                SetLoadResult(false, "创建喊话实体失败，缺少怪物配置。");
                return;
            }

            if (string.IsNullOrWhiteSpace(m_Bean.t_model))
            {
                SetLoadResult(false, string.Format("创建喊话实体失败，模型路径无效，entityId={0}。", ReadId()));
                return;
            }

            var res = UResourceFactory.New_EntityObject(ResourceType.Actor, emEntityType.em_EntityType_Actor);
            if (res == null)
            {
                SetLoadResult(false, string.Format("创建喊话实体失败，Actor 资源创建失败，entityId={0}。", ReadId()));
                return;
            }

            res.SetId(ReadId());
            SetResource(res);
            res.LoadRender(m_Bean.t_model, Tool.GetAssetName(m_Bean.t_model));
        }

        private void OnLoaded()
        {
            if (ReadIsDestroy())
            {
                return;
            }

            var go = GetShowObj() as GameObject;
            if (go == null)
            {
                SetLoadResult(false, string.Format("创建喊话实体失败，显示对象为空，entityId={0}。", ReadId()));
                return;
            }

            if (m_Parent == null)
            {
                SetLoadResult(false, string.Format("创建喊话实体失败，父节点为空，entityId={0}。", ReadId()));
                return;
            }

            var trans = go.transform;
            trans.SetParent(m_Parent, false);
            // 喊话实体要求完全贴合父节点，所以加载完成后要把相对父节点的变换强制归零。
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
            m_HitRoot = null;
            m_HitTransform = null;
            m_HitTransformResolved = false;
            m_ColliderRoot = null;
            m_RootCapsuleCollider = null;
            m_RootSphereCollider = null;
            m_RootColliderResolved = false;

            // UActor 后续仍会按照自身缓存的位置与朝向更新 Transform，
            // 这里需要把内部状态同步到父节点的世界变换，避免下一帧又把相对旋转改偏。
            SetPosition(m_Parent.position);
            SetForward(m_Parent.forward);
            GetRender()?.SetUp(m_Parent.up);
            SetScale(Vector3.one);

            var colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; ++i)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }

            SetLoadResult(true, null);
        }

        private void SetLoadResult(bool success, string errorMessage)
        {
            if (m_IsLoadSettled)
            {
                return;
            }

            m_IsLoadSettled = true;
            if (!success)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    Debug.LogError(errorMessage);
                }

                SetVisiable(false);
                if (!ReadIsDestroy())
                {
                    Destroy();
                }
            }

            var loadedCallback = m_LoadedCallback;
            m_LoadedCallback = null;
            loadedCallback?.Invoke(this);
        }

        private bool TryResolveHitTransform(out Transform hitTransform)
        {
            hitTransform = null;
            var showObj = GetShowObj() as GameObject;
            if (showObj == null)
            {
                return false;
            }

            var root = showObj.transform;
            if (!m_HitTransformResolved || m_HitRoot != root)
            {
                m_HitRoot = root;
                m_HitTransform = RenderAPI.GetTransform(root.gameObject, "hit", true);
                m_HitTransformResolved = true;
            }

            hitTransform = m_HitTransform;
            return hitTransform != null;
        }

        private bool TryReadRootCapsule(out Vector3 capsuleStart, out Vector3 capsuleEnd, out float capsuleRadius)
        {
            capsuleStart = Vector3.zero;
            capsuleEnd = Vector3.zero;
            capsuleRadius = 0.0f;
            if (!TryResolveRootColliders(out _, out var capsule, out _))
            {
                return false;
            }

            return TryBuildWorldCapsule(capsule, out capsuleStart, out capsuleEnd, out capsuleRadius);
        }

        private bool TryReadRootSphere(out Vector3 sphereCenter, out float sphereRadius)
        {
            sphereCenter = Vector3.zero;
            sphereRadius = 0.0f;
            if (!TryResolveRootColliders(out _, out _, out var sphere) || sphere == null)
            {
                return false;
            }

            var transform = sphere.transform;
            var scale = transform.lossyScale;
            var absScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            sphereCenter = transform.TransformPoint(sphere.center);
            sphereRadius = sphere.radius * Mathf.Max(absScale.x, Mathf.Max(absScale.y, absScale.z));
            return sphereRadius > 0.0001f;
        }

        private bool TryResolveRootColliders(
            out Transform root,
            out CapsuleCollider capsule,
            out SphereCollider sphere)
        {
            root = null;
            capsule = null;
            sphere = null;

            var showObj = GetShowObj() as GameObject;
            if (showObj == null)
            {
                return false;
            }

            root = showObj.transform;
            if (!m_RootColliderResolved || m_ColliderRoot != root)
            {
                m_ColliderRoot = root;
                m_RootCapsuleCollider = root.GetComponent<CapsuleCollider>();
                m_RootSphereCollider = root.GetComponent<SphereCollider>();
                m_RootColliderResolved = true;
            }

            capsule = m_RootCapsuleCollider;
            sphere = m_RootSphereCollider;
            return capsule != null || sphere != null;
        }

        private static bool TryBuildWorldCapsule(
            CapsuleCollider capsule,
            out Vector3 capsuleStart,
            out Vector3 capsuleEnd,
            out float capsuleRadius)
        {
            capsuleStart = Vector3.zero;
            capsuleEnd = Vector3.zero;
            capsuleRadius = 0.0f;
            if (capsule == null)
            {
                return false;
            }

            var transform = capsule.transform;
            var scale = transform.lossyScale;
            var absScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            var direction = capsule.direction;
            var localAxis = direction == 0 ? Vector3.right : (direction == 2 ? Vector3.forward : Vector3.up);
            var axisScale = direction == 0 ? absScale.x : (direction == 2 ? absScale.z : absScale.y);

            float radialScale;
            if (direction == 0)
            {
                radialScale = Mathf.Max(absScale.y, absScale.z);
            }
            else if (direction == 2)
            {
                radialScale = Mathf.Max(absScale.x, absScale.y);
            }
            else
            {
                radialScale = Mathf.Max(absScale.x, absScale.z);
            }

            capsuleRadius = capsule.radius * radialScale;
            var scaledHeight = capsule.height * axisScale;
            var halfStraightLength = Mathf.Max(0.0f, scaledHeight * 0.5f - capsuleRadius);
            var worldCenter = transform.TransformPoint(capsule.center);
            var worldAxis = transform.TransformDirection(localAxis).normalized;
            capsuleStart = worldCenter - worldAxis * halfStraightLength;
            capsuleEnd = worldCenter + worldAxis * halfStraightLength;
            return capsuleRadius > 0.0001f;
        }
    }
}
