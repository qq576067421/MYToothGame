namespace GameDll
{
    using MonoBean;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    public enum FirePointSelectMode
    {
        First = 0,
        RoundRobin = 1,
    }

    public class Entity : IResource
    {

        protected emEntityType m_EntityType = emEntityType.em_EntityType_None;

        protected int m_ID = int.MinValue;

        public virtual bool ReadIsMoveableCreature()
        {
            return false;
        }
        public virtual bool ReadIsPickableItem()
        {
            return false;
        }

        protected bool m_IsCanItemChangeGroup = false;
        public virtual void SetCanItemChangeGroup(bool canChangeGroup)
        {
            m_IsCanItemChangeGroup = canChangeGroup;
        }
        public virtual bool UseItemChangeGroup(GroupId group)
        {
            return false;
        }
        public virtual bool ReadIsHero()
        {
            return false;
        }

        public virtual bool ReadIsBoss()
        {
            return false;
        }
        public virtual bool ReadIsPropertyEntity()
        {
            return false;
        }
        public virtual bool ReadIsBuilding()
        {
            return false;
        }
        public virtual bool ReadIsTower()
        {
            return false;
        }
        public virtual bool ReadIsTrap()
        {
            return false;
        }
        public virtual bool ReadIsSmallMonster()
        {
            return false;
        }
        public virtual bool ReadIsMidEntity()
        {
            return false;
        }
        public virtual void SetYangDaoState(bool yang)
        {

        }
        public virtual bool ReadIsYangDaoState()
        {
            return false;
        }

        public Entity()
        {
        }

        //是否隐身状态
        protected bool m_IsTransparent = false;
        public virtual void SetTransparent(bool trans)
        {
            m_IsTransparent = trans;
            var render = GetRender(); 
            render.SetTransparent(trans ? 128.0f/255.0f : 1.0f);
            render.SetShadowTransparent(trans?0:1.0f);
        }
        public virtual bool ReadIsTransparent()
        {
            return m_IsTransparent;
        }

        private bool m_Visiable = true;
        private bool m_Attach = false;

        public virtual bool ReadIsFly()
        {
            return false;
        }

        protected bool m_IsTDMove = false;
        public virtual void SetTDMove(bool isTDMove)
        {
            m_IsTDMove = isTDMove;
        }
        public virtual bool ReadIsTDMove()
        {
            return m_IsTDMove;
        }

        public virtual bool ReadIsAttach()
        {
            return m_Attach;
        }
        public virtual void SetAttach(bool attach)
        {
            m_Attach = attach;
        }
        public virtual void SetBean(object bean)
        {

        }
        public virtual void SetDropRedArea(bool red)
        {

        }
        public virtual bool GetDropRedArea()
        {
            return false;
        }
        public virtual long ReadBeanId()
        {
            return 0;
        }
        public virtual bool ReadIsBeingControlled()
        {
            return false;
        }

        public virtual PropertyEntity ReadDefender()
        {
            return null;
        }

        public virtual void SetKillMeAttackId(int id, GroupId group)
        {

        }
        public virtual int GetKillMeAttackId()
        {
            return 0;
        }
        public virtual GroupId GetKillMeAttackGroup()
        {
            return GroupId.AnyGroupId;
        }
        public virtual void OnDead()
        {
            //LeaveGrid();
        }
        public virtual bool IsKeepDeadBody()
        {
            return false;
        }

        public virtual PropertyEntity GetAttackMe()
        {
            return null;
        }
        public virtual float GetMoveSpeed()
        {
            return 0;
        }
        //注意逻辑计算用3000，但是数值显示需要显示成300
        public virtual float GetConfigMoveSpeed()
        {
            return 3;
        }
        public virtual int GetAngularSpeed()
        {
            return int.MaxValue;
        }
        public virtual void SetAngularSpeed(int speed)
        {

        }
        public virtual bool ReadIsInBorn()
        {
            var stateMgr = GetStateManager();
            if (stateMgr != null)
            {
                return stateMgr.ReadIsState(emEntityState.em_EntityState_Born);
            }
            else
            {
                return false;
            }
        }
        public virtual AttackFailedReason CanAttack(Skill skill)
        {
            return AttackFailedReason.SystemError;
        }
        public virtual void SetDefender(PropertyEntity defender)
        {

        }
        public virtual void SetAttackMe(PropertyEntity attackMe)
        {
 
        }

        public Matrix4x4 GetMatrix4X4()
        {
            var render = GetRender();
            return render.GetMatrix4X4();
        }


        public virtual bool CanBeSelected()
        {
            return false;
        }


        public  Vector3 ReadForward()
        {
            var render = GetRender();
            return render.GetForward();
        }


        public virtual void Attack(int slot, Vector3 forward, Vector3 position, int targetId)
        {

        }
        public virtual void AttackDir(int slot, Vector3 face_forward, Vector3 move_dir)
        {

        }
        public virtual void Attack(Skill skill, Vector3 dir, Vector3 pos, PropertyEntity defender)
        {
        }
        public virtual bool AttackRange(PropertyEntity defender, bool is_far_move = true)
        {
            return false;
        }

        public override int ReadId()
        {
            return m_ID;
        }

        public emEntityType ReadObjectType()
        {
            return m_EntityType;
        }


        public int m_GridX = int.MinValue;
        public int m_GridY = int.MinValue;

        public  Vector3 GetPosition()
        {
            var render = GetRender();
            return render.GetPosition();
        }


        public Vector3 ReadScale()
        {
            var render = GetRender();
            return render.GetScale();
        }

        public override bool ReadVisiable()
        {
            return m_Visiable;
        }

        protected virtual void StandOnFloor()
        {

        }
        public virtual StateBase GetCurrentState()
        {
            return null;
        }
        public virtual bool TryChangeState(emEntityState em_EntityState, bool must = false)
        {
            return false;
        }

        public bool IsEntityType(emEntityType type)
        {
            return type == m_EntityType;
        }


        public virtual float GetNormalAtkSpeed()
        {
            return 100.0f;
        }
        protected virtual ReadOnlyCollection<ReadOnlyCollection<int>> GetFirePointPositionsCfgValues()
        {
            return null;
        }
        protected virtual string GetFirePointNamesCfgValue()
        {
            return null;
        }
        protected virtual float GetHitPointCfgValue()
        {
            return 0;
        }
        protected Vector3 m_HitPoint = new Vector3(int.MinValue, int.MinValue, int.MinValue);
        public virtual Vector3 ReadHitPoint()
        {
            var mat = GetMatrix4X4();
            var hit_point_cfg_value = GetHitPointCfgValue();
            if (m_HitPoint.x == int.MinValue)
            {
                if (hit_point_cfg_value == 0)
                {
                    m_HitPoint = new Vector3(0, 0.5f, 0);
                }
                else
                {
                    m_HitPoint = new Vector3(0, hit_point_cfg_value, 0);
                }
            }
            return mat.MultiplyPoint3x4(m_HitPoint);
        }

        public virtual bool TryReadAutoAimPoint(out Vector3 autoAimPoint)
        {
            autoAimPoint = Vector3.zero;
            if (ReadIsSmallMonster() || ReadIsBoss())
            {
                if (!TryResolveAutoAimHitTransform(out var hitTransform))
                {
                    return false;
                }

                autoAimPoint = hitTransform.position;
                return true;
            }

            autoAimPoint = ReadHitPoint();
            return true;
        }

        public virtual bool TryIntersectSegment(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            float extraRadius,
            out float hitT,
            out Vector3 hitPoint)
        {
            extraRadius = Mathf.Max(0.0f, extraRadius);
            if (TryReadMonsterRootCapsule(out var capsuleStart, out var capsuleEnd, out var capsuleRadius))
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

            return BattleManager.TryIntersectSegmentSphere(
                segmentStart,
                segmentEnd,
                ReadHitPoint(),
                ReadRadius() + extraRadius,
                out hitT,
                out hitPoint);
        }

        private bool TryGetRenderRoot(out Transform root)
        {
            root = null;
            var render = GetRender();
            if (render == null)
            {
                return false;
            }

            var showObj = render.GetShowObj() as GameObject;
            if (showObj == null)
            {
                return false;
            }

            root = showObj.transform;
            return true;
        }

        private bool TryReadMonsterRootCapsule(out Vector3 capsuleStart, out Vector3 capsuleEnd, out float capsuleRadius)
        {
            capsuleStart = Vector3.zero;
            capsuleEnd = Vector3.zero;
            capsuleRadius = 0.0f;

            if (!ReadIsSmallMonster() && !ReadIsBoss())
            {
                return false;
            }

            if (!TryGetRenderRoot(out var root))
            {
                return false;
            }

            var capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null || !capsule.enabled)
            {
                return false;
            }

            return TryBuildWorldCapsule(capsule, out capsuleStart, out capsuleEnd, out capsuleRadius);
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
            var centerWorld = transform.TransformPoint(capsule.center);
            var axisWorld = transform.TransformDirection(localAxis).normalized;
            var halfSegment = Mathf.Max(0.0f, capsule.height * axisScale * 0.5f - capsuleRadius);
            capsuleStart = centerWorld - axisWorld * halfSegment;
            capsuleEnd = centerWorld + axisWorld * halfSegment;
            return capsuleRadius > 0.0f;
        }

        private bool TryResolveAutoAimHitTransform(out Transform hitTransform)
        {
            hitTransform = null;
            if (!TryGetRenderRoot(out var root))
            {
                return false;
            }

            if (!m_AutoAimHitResolved || m_AutoAimHitRoot != root)
            {
                m_AutoAimHitRoot = root;
                m_AutoAimHitTransform = RenderAPI.GetTransform(root.gameObject, m_AutoAimHitNodeName, true);
                m_AutoAimHitResolved = true;
            }

            hitTransform = m_AutoAimHitTransform;
            return hitTransform != null;
        }

        public virtual Transform ReadMountTransform(string mountPath)
        {
            if (!TryGetRenderRoot(out var root))
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(mountPath))
            {
                return root;
            }

            var mount = RenderAPI.GetTransform(root, mountPath, true);
            return mount != null ? mount : root;
        }

        public virtual Vector3 ReadMountPosition(string mountPath)
        {
            var mount = ReadMountTransform(mountPath);
            if (mount != null)
            {
                return mount.position;
            }

            var render = GetRender();
            if (render != null)
            {
                return render.GetPosition();
            }

            return m_Position;
        }
        private Vector3 m_Forward;
        public virtual void SetForward(Vector3 forward)
        {
            if (forward == Vector3.zero)
            {
                return;
            }
            m_Forward = forward;
            var render = GetRender();
            if (render != null)
            {
                render.SetForward(forward);
            }
        }

        public virtual void SetBaseForward(Vector3 forward)
        {
            var render = GetRender();
            if (render != null)
            {
                render.SetBaseForward(forward);
            }
        }

        public virtual float ReadDefaultPitchDegrees()
        {
            var render = GetRender();
            return render != null ? render.ReadDefaultPitchDegrees() : 0f;
        }

        public virtual void SetId(int id)
        {
            m_ID = id;
            var render = GetRender();
            if (render != null)
            {
                render.SetId(id);
            }
        }

        public void SetObjectType(emEntityType ty)
        {
            m_EntityType = ty;
        }

        protected Vector3 m_Position;
        public virtual void SetPosition(Vector3 position)
        {
            m_Position = position;
            var render = GetRender();
            if (render != null)
            {
                render.SetPosition(position);
            }
        }

        protected Vector3 m_Scale;
        public void SetScale(Vector3 scale)
        {
            m_Scale = scale;
            var render = GetRender();
            if (render != null)
            {
                render.SetScale(scale);
            }
        }

        public override void SetVisiable(bool visiable)
        {
            m_Visiable = visiable;
            var render = GetRender();
            if (render != null)
            {
                render.SetActive(visiable);
            }
        }

        public virtual float ReadAliveLeftTime()
        {
            return 0;
        }
        public virtual float ReadAliveTotalTime()
        {
            return 5.0f;
        }
        public virtual void Stop()
        {
        }
        public virtual void BreakSkill()
        {
        }

        public virtual bool IsEntityId(int id)
        {
            return ReadId() == id;
        }

        protected GroupId m_Group;
        public GroupId ReadGroup()
        {
            return m_Group;
        }


        public virtual  void SetGroup(GroupId group)
        {
            m_Group = group;
            m_HurtGroup = group;
        }

        protected GroupId m_HurtGroup;
        public  GroupId ReadHurtGroup()
        {
            return m_HurtGroup;
        }
        public virtual void SetHurtGroup(GroupId hurtGroup)
        {
            m_HurtGroup = hurtGroup;
        }
        protected bool m_CanBeHurt = true;
        public virtual void SetCanBeHurt(bool can_be_hurt)
        {
            m_CanBeHurt = can_be_hurt;
        }
        public  bool ReadCanBeHurt()
        {
            return m_CanBeHurt;
        }

        public virtual void AttackFinish(AttackFinishReason attackFinishReason)
        {

        }
        public virtual StateManager GetStateManager()
        {
            return null;
        }
        public virtual SkillManager GetSkillManager()
        {
            return null;
        }
        public virtual bool Move(int x, int z)
        {
            return false;
        }

        public virtual bool CanMove()
        {
            return false;
        }

        public virtual void SetBaseFireForward(Vector3 dir)
        {

        }
        //X为轴的角度
        public virtual void SetBaseFireLocalAngle(int hudu)
        {

        }
        private const string m_AutoAimHitNodeName = "hit";
        private static readonly Vector3 m_DefaultFirePointLocal = new Vector3(0, 0.5f, 0);
        private readonly List<Vector3> m_FirePointLocalPositions = new List<Vector3>();
        private readonly List<string> m_FirePointNames = new List<string>();
        private readonly List<Vector3> m_ResolvedFirePoints = new List<Vector3>();
        private readonly List<Transform> m_ResolvedFirePointTransforms = new List<Transform>();
        private Transform m_AutoAimHitTransform = null;
        private Transform m_AutoAimHitRoot = null;
        private bool m_AutoAimHitResolved = false;
        private bool m_FirePointConfigResolved = false;
        private int m_NextRoundRobinFirePointIndex = 0;
        private int m_LastResolvedFirePointIndex = 0;

        protected void ResetFirePointCache()
        {
            m_FirePointLocalPositions.Clear();
            m_FirePointNames.Clear();
            m_ResolvedFirePoints.Clear();
            m_ResolvedFirePointTransforms.Clear();
            m_AutoAimHitTransform = null;
            m_AutoAimHitRoot = null;
            m_AutoAimHitResolved = false;
            m_FirePointConfigResolved = false;
            m_NextRoundRobinFirePointIndex = 0;
            m_LastResolvedFirePointIndex = 0;
        }

        private static Vector3 ReadFirePointLocalPosition(ReadOnlyCollection<int> positionCfg)
        {
            if (positionCfg == null || positionCfg.Count <= 0)
            {
                return m_DefaultFirePointLocal;
            }

            if (positionCfg.Count >= 3)
            {
                return new Vector3(
                    positionCfg[0] / 1000.0f,
                    positionCfg[1] / 1000.0f,
                    positionCfg[2] / 1000.0f);
            }

            if (positionCfg.Count == 2)
            {
                return new Vector3(0, positionCfg[0] / 1000.0f, positionCfg[1] / 1000.0f);
            }

            return new Vector3(0, positionCfg[0] / 1000.0f, 0);
        }

        private void EnsureFirePointConfigResolved()
        {
            if (m_FirePointConfigResolved)
            {
                return;
            }

            m_FirePointConfigResolved = true;
            m_FirePointLocalPositions.Clear();
            m_FirePointNames.Clear();

            var firePointNames = GetFirePointNamesCfgValue();
            if (!string.IsNullOrWhiteSpace(firePointNames))
            {
                var names = firePointNames.Split('+');
                for (int i = 0; i < names.Length; i++)
                {
                    var firePointName = names[i] != null ? names[i].Trim() : string.Empty;
                    if (!string.IsNullOrEmpty(firePointName))
                    {
                        m_FirePointNames.Add(firePointName);
                    }
                }
            }

            var firePointPositions = GetFirePointPositionsCfgValues();
            if (firePointPositions == null)
            {
                return;
            }

            for (int i = 0; i < firePointPositions.Count; i++)
            {
                m_FirePointLocalPositions.Add(ReadFirePointLocalPosition(firePointPositions[i]));
            }
        }

        private static bool AddResolvedFirePointTransform(List<Transform> firePoints, Transform firePoint)
        {
            if (firePoint == null)
            {
                return false;
            }

            for (int i = 0; i < firePoints.Count; i++)
            {
                if (firePoints[i] == firePoint)
                {
                    return false;
                }
            }

            firePoints.Add(firePoint);
            return true;
        }

        private int ResolveFirePointTransforms(List<Transform> firePoints)
        {
            firePoints.Clear();
            if (!TryGetRenderRoot(out var root))
            {
                return 0;
            }

            var spineRotator = root.GetComponent<LCL.SpineRotator>();
            var spineFirePoints = spineRotator != null ? spineRotator.m_FirePoints : null;
            if (spineFirePoints != null)
            {
                for (int i = 0; i < spineFirePoints.Length; i++)
                {
                    AddResolvedFirePointTransform(firePoints, spineFirePoints[i]);
                }
            }
            if (firePoints.Count > 0)
            {
                return firePoints.Count;
            }

            EnsureFirePointConfigResolved();
            for (int i = 0; i < m_FirePointNames.Count; i++)
            {
                var firePointName = m_FirePointNames[i];
                if (string.IsNullOrWhiteSpace(firePointName))
                {
                    continue;
                }

                var mount = RenderAPI.GetTransform(root.gameObject, firePointName, true);
                AddResolvedFirePointTransform(firePoints, mount);
            }
            return firePoints.Count;
        }

        private int ResolveFirePoints(List<Vector3> firePoints)
        {
            firePoints.Clear();
            int firePointCount = ResolveFirePointTransforms(m_ResolvedFirePointTransforms);
            for (int i = 0; i < firePointCount; i++)
            {
                var firePointTransform = m_ResolvedFirePointTransforms[i];
                if (firePointTransform != null)
                {
                    firePoints.Add(firePointTransform.position);
                }
            }

            if (firePoints.Count > 0)
            {
                return firePoints.Count;
            }

            EnsureFirePointConfigResolved();
            var mat = GetMatrix4X4();
            for (int i = 0; i < m_FirePointLocalPositions.Count; i++)
            {
                firePoints.Add(mat.MultiplyPoint3x4(m_FirePointLocalPositions[i]));
            }

            if (firePoints.Count <= 0)
            {
                firePoints.Add(mat.MultiplyPoint3x4(m_DefaultFirePointLocal));
            }

            return firePoints.Count;
        }

        public int ReadResolvedFirePointCount()
        {
            return ResolveFirePoints(m_ResolvedFirePoints);
        }

        public Vector3 ReadResolvedFirePointCenter()
        {
            int count = ResolveFirePoints(m_ResolvedFirePoints);
            if (count <= 0)
            {
                return GetPosition();
            }
            if (count == 1)
            {
                return m_ResolvedFirePoints[0];
            }
            var sum = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                sum += m_ResolvedFirePoints[i];
            }
            return sum / count;
        }

        // 开火方向统一取当前开火点节点朝向；如果没有有效节点则返回零向量，由上层自行兜底。
        public virtual Vector3 ReadResolvedFirePointForward()
        {
            if (ResolveFirePointTransforms(m_ResolvedFirePointTransforms) > 0)
            {
                int firePointIndex = Mathf.Clamp(m_LastResolvedFirePointIndex, 0, m_ResolvedFirePointTransforms.Count - 1);
                var firePoint = m_ResolvedFirePointTransforms[firePointIndex];
                if (firePoint != null && firePoint.forward.sqrMagnitude > 0.0001f)
                {
                    return firePoint.forward.normalized;
                }
            }

            return Vector3.zero;
        }

        public virtual Vector3 GetFirePoint(
            FirePointSelectMode selectMode = FirePointSelectMode.First,
            bool markAsUsed = false)
        {
            int firePointCount = ResolveFirePoints(m_ResolvedFirePoints);
            if (firePointCount <= 0)
            {
                return GetPosition();
            }

            if (selectMode != FirePointSelectMode.RoundRobin || firePointCount == 1)
            {
                m_LastResolvedFirePointIndex = 0;
                return m_ResolvedFirePoints[0];
            }

            int index = m_NextRoundRobinFirePointIndex;
            if (index < 0 || index >= firePointCount)
            {
                index = 0;
                m_NextRoundRobinFirePointIndex = 0;
            }

            var firePoint = m_ResolvedFirePoints[index];
            m_LastResolvedFirePointIndex = index;
            if (markAsUsed)
            {
                m_NextRoundRobinFirePointIndex = (index + 1) % firePointCount;
            }

            return firePoint;
        }
        public override void Update(float dt)
        {
            base.Update(dt);


            UpdateNoHurtTime(dt);
        }
        public virtual void UpdateNoHurtTime(float dt)
        {
            if (m_NoHurtTime > 0)
            {
                m_NoHurtTime -= dt;
                if (m_NoHurtTime <= 0)
                {
                    m_NoHurtTime = 0;
                }
            }
        }

        //不受伤害的时间
        protected float m_NoHurtTime;
        public virtual void SetNoHurtTime(float time)
        {
            m_NoHurtTime = time;
        }
        public virtual float GetNoHurtTime()
        {
            return m_NoHurtTime;
        }




        protected bool m_IsReceiveExp = false;
        public virtual void SetReceiveExp(bool receiveExp)
        {
            m_IsReceiveExp = receiveExp;
        }
        public virtual bool IsReceiveExp()
        {
            return m_IsReceiveExp;
        }
        public virtual bool IsReceiveGold()
        {
            return false;
        }
        protected long m_Exp;
        public  long ReadExp()
        {
            return m_Exp;
        }

        public virtual Skill ReadNormalSkill()
        {
            return null;
        }
        public virtual Skill ReadCurrentSkill()
        {
            return null;
        }
        public virtual void InitSkills()
        {

        }

        public virtual void RegisterSkill(long skillCfgId, int level, int slot)
        {
        }
        public virtual void RegisterOtherSkill(long skillId, int level, int slot)
        {

        }
        public virtual void RegisterSkills()
        {

        }
        public virtual void ReplaceSkill(long skillId, int level, int slot)
        {

        }

        //施法距离
        public virtual float GetSkillCastDist(Skill skill)
        {
            var skillBean = skill != null ? skill.GetSkillBean() : null;
            var bulletCfg = skillBean != null ? t_bullet.GetConfig(skillBean.t_bullet_id, false) : null;
            if (bulletCfg == null)
            {
                return 0;
            }

            return bulletCfg.t_move_speed / 1000.0f * bulletCfg.t_max_time / 1000.0f;
        }


        protected bool m_IsCanBeTarget = true;
        public virtual bool ReadCanBeTarget()
        {
            return m_IsCanBeTarget;
        }
        public virtual void SetCanBeTarget(bool attackable)
        {
            m_IsCanBeTarget = attackable;
        }
        public virtual bool IsCanAttack()
        {
            return true;
        }


        public virtual float ReadRadius()
        {
            return 0.1f;
        }
        public virtual void OnHpChanged()
        {
            var hp = ReadHP();
            var maxHp = GetMaxHP();
            GetRender().SetHpValue(hp / maxHp, 1);
            if (hp <= 0)
            {
            }
        }
        /////////////////////////属性////////////////////////////////
        #region 属性


        public virtual void InitLevel(int level)
        {

        }

        public virtual void InitHp()
        {
        }


        public virtual void SetLevel(int level)
        {

        }
        public virtual int ReadLevel()
        {
            return 0;
        }
        public virtual float GetProperty(int propertyType)
        {
            return 0;
        }
        public virtual float GetPropertyBase(int propertyType)
        {
            return 0;
        }
        public virtual float ReadHP()
        {
            return 0;
        }
        public virtual void SetHpRuntime(float hp)
        {
        }
        public virtual float GetMaxHP()
        {
            return 1;
        }
        public virtual float GetAtk()
        {
            return 0;
        }

        public virtual float GetAttackRange()
        {
            return 0;
        }
        public virtual float ReadDamageAmpPercent()
        {
            return 1;
        }


        public virtual bool ReadIsDead()
        {
            return false;
        }

 



        //警戒范围，默认为普攻的施法距离
        protected float m_WarningDist = 0;
        public virtual void SetWarningDist(float warningDist)
        {
            m_WarningDist = warningDist;
        }
        public virtual float ReadWarningDist()
        {
            return m_WarningDist;
        }
        #endregion



        public virtual long ReadBattlePlayerId()
        {
            return 0;
        }

        public virtual bool ReadIsSharedByGroup()
        {
            return false;
        }

        public virtual long ReadRenderTotalProperty(int property_type)
        {
            return 0;
        }
    }
}
