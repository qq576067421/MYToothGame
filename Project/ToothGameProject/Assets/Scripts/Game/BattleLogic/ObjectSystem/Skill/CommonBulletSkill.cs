using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameDll
{

    public class CommonBulletSkill : Skill
    {
        protected struct KeepShootTargetCandidate
        {
            public PropertyEntity m_Defender;
            public float m_Angle;
            public float m_Distance;
        }

        protected Vector3 m_StartPos;
        protected t_bullet m_BulletBean;
        protected List<RenderEff> m_KeepShootEffs = new List<RenderEff>();
        private List<Transform> m_KeepShootEffParentTransforms = new List<Transform>();
        protected float m_KeepShootStartTime = -1;
        protected HitDetectionData m_KeepShootHitData = new HitDetectionData();
        protected readonly List<KeepShootTargetCandidate> m_KeepShootCandidates = new List<KeepShootTargetCandidate>();

        public override void InitTemplate(int classId, long templateId)
        {
            base.InitTemplate(classId, templateId);
            m_BulletBean = t_bullet.GetConfig(m_SkillBean.t_bullet_id);


        }
        public override void PreLoadEffect()
        {
            LoadEffect(false);
        }

        protected override object LoadEffect(bool show)
        {
            if (m_BulletBean.t_effect_abname != null)
            {
                var objMgr = BattleManager.GetBattle().GetObjectManager();
                var pool = BattleManager.GetBattle().GetBulletObjPool();
                var bullet = pool.GetEffect(emEntityType.em_EntityType_Bullet, 
                    m_BulletBean.t_id, m_BulletBean, 
                    ResourceType.Bullet);
                bullet.SetVisiable(show);
                if(!show)
                {
                    pool.PoolEffect(m_BulletBean.t_id, bullet);
                    //表示随时可以使用
                    bullet.SetHideTime(-10);
                }
                return bullet;
            }
            else
            {
                return null;
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            SetCurAction();
            PlayAction(m_CurAction.t_ac_name);
            //Debug.Log("开始播放子弹动画时间：" + Time.realtimeSinceStartup);
            m_CastStatus =  SkillCastStatus.warming_up;
        }


        /// <summary>
        /// 获取子弹prefab名字
        /// </summary>
        /// <returns></returns>
        public virtual t_bullet GetBulletBean()
        {
            return m_BulletBean;
        }


        protected virtual void ShootBullet()
        {
            //Debug.Log("ShootBullet");
            var attacker = ReadAttacker();
            if(attacker == null)
            {
                Debug.LogWarning("attacker not find");
                return;
            }

            var cfg = GetSkillDescBean();
            PlayAttackSound(cfg != null ? cfg.t_attackSound : 0, m_Attacker.GetPosition());


            if (!BattleManager.ReadIsEntityValide(m_MainDefender))
            {
                m_MainDefender = null;
            }

            ShootBullet(m_MainDefender);


        }
        
        protected virtual void ShootBullet(PropertyEntity defender)
        {
            CreateBullet(defender);
        }

        protected virtual void CreateBullet(PropertyEntity defender)
        {
            CreateBullet(defender, false);
        }

        protected virtual void CreateBullet(PropertyEntity defender, bool useCurrentAttackerForward)
        {
            var attacker = m_Attacker;
            var cfg = GetSkillBean();

            ResetCastFirePoint();
            var firePoint = ResolveFreshFirePoint();
            m_StartPos = BulletFlightConstraintUtility.ResolveConstrainedPosition(m_BulletBean, firePoint);

            var bullet = (BulletObj)LoadEffect(true);
            var objMgr = BattleManager.GetBattle().GetObjectManager();
            bullet.SetId(objMgr.AssignClientId());
            bullet.SetAutoDel(PlayableEffectDelType.Pool);
            bullet.SetPlay(true);
            bullet.SetVisiable(true);
            bullet.SetPosition(m_StartPos);

            var launchForward = useCurrentAttackerForward ? ResolveCurrentAttackerLaunchForward() : ResolveLaunchForward();
            var forward = BulletFlightConstraintUtility.ResolveConstrainedForward(m_BulletBean, launchForward, attacker != null ? attacker.ReadForward() : Vector3.forward);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = launchForward;
            }
            bullet.SetForward(forward);
            bullet.SetAttacker(m_AttackerId, m_AttackerGroup);
            bullet.SetStartPos(m_StartPos);
            bullet.SetFlyDist(GetCastDistance());


            var desc_cfg = GetSkillDescBean();
            bullet.SetHitSound(desc_cfg != null ? desc_cfg.t_hitSound : 0);
            var hurtInfo = bullet.GetHurtInfo();
            hurtInfo.Reset();
            hurtInfo.m_Hurt = GetAtk();
            hurtInfo.m_BaseAttack = ReadLastDamageBaseAttack();
            hurtInfo.m_RawSkillDamageCfg = ReadLastRawSkillDamageCfg();
            hurtInfo.m_SkillDamagePercent = ReadLastDamageSkillPercent();
            hurtInfo.m_CritRate = ReadLastDamageCritRate();
            hurtInfo.m_CritDamageScale = ReadLastDamageCritDamageScale();
            hurtInfo.m_DamageAmpScale = ReadLastDamageAmpScale();
            hurtInfo.m_Slot = m_Slot;
            hurtInfo.m_NeedTarget = 0;
            hurtInfo.m_Group = m_AttackerGroup;
            hurtInfo.m_AttackerId = m_AttackerId;
            hurtInfo.m_Attacker = attacker;
            if (attacker != null)
            {
                hurtInfo.m_HurtGroup = attacker.ReadHurtGroup();
            }
            else
            {
                hurtInfo.m_HurtGroup = m_AttackerGroup;
            }
            hurtInfo.skillCfg = cfg;
            hurtInfo.skillDescCfg = desc_cfg;
            bullet.SetDefender(defender);
            if (defender != null)
            {
                bullet.SetDefenderPos(defender.ReadHitPoint());
            }

            var bulletBean = bullet.GetBulletBean();
            if (bulletBean != null)
            {
                long hitBuffId = 0;
                if (bulletBean.t_bullet_hittarget_buff_id != null && bulletBean.t_bullet_hittarget_buff_id.Count > 0)
                {
                    hitBuffId = bulletBean.t_bullet_hittarget_buff_id[0];
                }

                bullet.SetBulletConfig(
                    bulletBean.t_penetrate,
                    bulletBean.t_size,
                    bulletBean.t_trajectory,
                    bulletBean.t_tracking_range,
                    hitBuffId,
                    bulletBean.t_trigger_bullet_id,
                    bulletBean.t_trigger_bullet_count,
                    bulletBean.t_trigger_type
                );
            }

            int defenderId = defender != null ? defender.ReadId() : 0;
            int bulletId = bulletBean != null ? bulletBean.t_id : 0;
            //Debug.Log(
            //    $"[技能发弹] skillId={cfg.t_id}，bulletId={bulletId}，attackerId={m_AttackerId}，defenderId={defenderId}，" +
            //    $"needTarget={hurtInfo.m_NeedTarget}，startPos={m_StartPos}，forward={forward}，" +
            //    $"triggerBulletId={(bulletBean != null ? bulletBean.t_trigger_bullet_id : 0)}，" +
            //    $"triggerCount={(bulletBean != null ? bulletBean.t_trigger_bullet_count : 0)}，" +
            //    $"triggerType={(bulletBean != null ? bulletBean.t_trigger_type : 0)}");

            objMgr.AddEffObject(bullet);
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            float skillUsedTime = GetSkillUsedTime();

            switch (m_CastStatus)
            {
                case SkillCastStatus.warming_up:
                    {
                        var cast_time = BattleManager.ConvertFrame2Second(m_CurAction.t_ac_cast_point, GetAttackSpeed(), GetActionFrameRate());
                        if (skillUsedTime > cast_time)
                        {
                            m_CastStatus = SkillCastStatus.cast_point;

                        }
                        //Debug.Log("子弹预热的时间：" + Time.realtimeSinceStartup);
                        break;
                    }
                case SkillCastStatus.cast_point:
                    {
                        //Debug.Log("子弹发射的时间：" + Time.realtimeSinceStartup);
                        OnCastPoint();
                        break;
                    }
                case SkillCastStatus.cast_keep:
                    {
                        var keepTime = GetKeepShootDurationSecond();
                        var keepUsedTime = BattleManager.ReadBattleTime() - m_KeepShootStartTime;
                        if (keepUsedTime > keepTime)
                        {
                            StopKeepShoot();
                            m_CastStatus = SkillCastStatus.cast_back;
                        }
                        else
                        {
                            var hurtInterval = GetKeepShootIntervalSecond();
                            var time = BattleManager.ReadBattleTime() - m_LastCastTime;
                            if (hurtInterval > 0 && time >= hurtInterval)
                            {
                                AgainShootBullet();
                            }
                        }
                        break;
                    }
                case SkillCastStatus.cast_back:
                    {
                        var usedTime = GetSkillUsedTime();
                        var frameTime = BattleManager.ConvertFrame2Second(m_CurAction.t_ac_finish, GetAttackSpeed(), GetActionFrameRate());
                        if (usedTime >= frameTime)
                        {
                            m_CastStatus = SkillCastStatus.cast_over;
                        }
                        break;
                    }

                case SkillCastStatus.cast_over:
                    {
                        OnCastOver();
                        break;
                    }
            }
        }

        private void OnCastPoint()
        {
            ApplySkillCastBuffIfNeeded();
            FirstShootBullet();
            if (UseKeepShootMode())
            {
                StartKeepShootEffect();
                m_KeepShootStartTime = BattleManager.ReadBattleTime();

                m_CastStatus = SkillCastStatus.cast_keep;
                RenderEvent.Event.KeepSkill(this.GetCastId(), true);
            }
            else
            {
                StartShootEffect();
                m_CastStatus = SkillCastStatus.cast_back;
            }
        }

        protected float m_LastCastTime = 0;
        protected virtual void FirstShootBullet()
        {
            m_LastCastTime = BattleManager.ReadBattleTime();
            ShootBullet();
        }

        protected virtual void AgainShootBullet()
        {
            m_LastCastTime = BattleManager.ReadBattleTime();
            if (UseKeepShootMode() && m_CastStatus == SkillCastStatus.cast_keep)
            {
                ShootKeepModeBullet();
                return;
            }

            ShootBullet();
        }

        protected virtual bool UseKeepShootMode()
        {
            return m_SkillBean != null &&
                m_SkillBean.t_keep_time > 0 &&
                m_SkillBean.t_hurt_interval > 0;
        }

        protected virtual float GetKeepShootDurationSecond()
        {
            if (m_SkillBean == null)
            {
                return 0;
            }

            return Mathf.Max(0, m_SkillBean.t_keep_time / 1000.0f);
        }

        protected virtual float ResolveAttackEffectDurationSecond()
        {
            if (UseKeepShootMode())
            {
                return Mathf.Max(0.1f, GetKeepShootDurationSecond());
            }

            // 非持续子弹技能也要给技能特效可见时长，
            // 否则 t_keep_time 为 0 时会在创建后立即回收。
            return 2.0f;
        }

        protected virtual float GetKeepShootIntervalSecond()
        {
            if (m_SkillBean == null)
            {
                return 0;
            }

            return Mathf.Max(0, m_SkillBean.t_hurt_interval / 1000.0f);
        }

        protected virtual void ShootKeepModeBullet()
        {
            var defender = FindKeepShootTarget();
            if (defender != null)
            {
                m_MainDefender = defender;
                CreateBullet(defender, true);
                return;
            }

            CreateBullet(null, true);
        }

        protected virtual PropertyEntity FindKeepShootTarget()
        {
            var attacker = ReadAttacker();
            var objMgr = BattleManager.GetObjectManager();
            if (attacker == null || objMgr == null || m_SkillBean == null)
            {
                return null;
            }

            var hitType = (HitDetectionShapeType)m_SkillBean.t_hurt_param_type;
            var hitData = m_KeepShootHitData;
            hitData.hurt_group = attacker.ReadHurtGroup();
            hitData.hitType = hitType;
            hitData.hurt_range = m_SkillBean.t_hurt_param0 / 1000.0f;
            hitData.angle = m_SkillBean.t_hurt_param1 / 1000.0f;
            hitData.chang = hitData.hurt_range;
            hitData.kuan = hitData.angle;
            hitData.dir = ReadKeepShootForward();
            hitData.pos = ReadKeepShootOrigin(hitType);

            var forward = hitData.dir;
            forward.y = 0;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
            }

            var origin = hitData.pos;
            origin.y = 0;

            m_KeepShootCandidates.Clear();
            var enemies = objMgr.ReadPropertyEntities();
            int count = enemies.Count;
            for (int i = 0; i < count; i++)
            {
                var enemy = enemies[i];
                if (!BattleManager.ReadIsEntityValide(enemy))
                {
                    continue;
                }

                if (!enemy.ReadCanBeTarget() || enemy.ReadHurtGroup() == hitData.hurt_group)
                {
                    continue;
                }

                if (!BattleManager.IsHitEnemy(hitData, enemy))
                {
                    continue;
                }

                var targetPos = enemy.ReadHitPoint();
                targetPos.y = 0;
                var toTarget = targetPos - origin;
                var distance = toTarget.magnitude;
                float angle = 0;
                if (forward.sqrMagnitude > 0.0001f && toTarget.sqrMagnitude > 0.0001f)
                {
                    angle = Vector3.Angle(forward, toTarget.normalized);
                }

                m_KeepShootCandidates.Add(new KeepShootTargetCandidate
                {
                    m_Defender = enemy,
                    m_Angle = angle,
                    m_Distance = distance
                });
            }

            int candidateCount = m_KeepShootCandidates.Count;
            if (candidateCount <= 0)
            {
                return null;
            }

            m_KeepShootCandidates.Sort((left, right) =>
            {
                int angleCompare = left.m_Angle.CompareTo(right.m_Angle);
                if (angleCompare != 0)
                {
                    return angleCompare;
                }

                return left.m_Distance.CompareTo(right.m_Distance);
            });

            int randomCount = Mathf.Min(3, candidateCount);
            if (randomCount <= 1)
            {
                return m_KeepShootCandidates[0].m_Defender;
            }

            float totalWeight = 0;
            for (int i = 0; i < randomCount; i++)
            {
                totalWeight += randomCount - i;
            }

            float randomWeight = UnityEngine.Random.Range(0, totalWeight);
            for (int i = 0; i < randomCount; i++)
            {
                float weight = randomCount - i;
                if (randomWeight < weight)
                {
                    return m_KeepShootCandidates[i].m_Defender;
                }

                randomWeight -= weight;
            }

            return m_KeepShootCandidates[0].m_Defender;
        }

        protected virtual Vector3 ReadKeepShootForward()
        {
            return ResolveCurrentAttackerLaunchForward();
        }

        protected virtual Vector3 ResolveCurrentAttackerLaunchForward()
        {
            var dir = Vector3.zero;
            if (m_Attacker != null)
            {
                dir = m_Attacker.ReadResolvedFirePointForward();
            }
            if (TryResolveCurrentUpgradeChallengeLaunchForward(dir, out var challengeForward))
            {
                return challengeForward;
            }
            if (dir.sqrMagnitude <= 0.0001f)
            {
                dir = ReadSkillDir();
            }
            if (dir.sqrMagnitude <= 0.0001f && m_Attacker != null)
            {
                dir = m_Attacker.ReadForward();
            }
            if (dir.sqrMagnitude <= 0.0001f)
            {
                dir = Vector3.forward;
            }
            return dir.normalized;
        }

        private bool TryResolveCurrentUpgradeChallengeLaunchForward(Vector3 requestedForward, out Vector3 launchForward)
        {
            launchForward = Vector3.zero;
            if (m_Attacker == null)
            {
                return false;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null ||
                (!battle.ReadIsUpgradeChallengePreActive() && !battle.ReadIsUpgradeChallengeActive()) ||
                !BattleManager.ReadIsEntityValide(battle.ReadUpgradeChallengeTarget()))
            {
                return false;
            }

            // 持续发弹每颗子弹都要重新读取当前面向，但棒棒糖不是普通怪物目标。
            // 因此棒棒糖阶段还必须重新走自动吸附解算，把当前水平面向修正到 center 对应的命中高度。
            return battle.TryResolveGuardHeroNormalAutoAim(
                    m_Attacker,
                    this,
                    requestedForward,
                    out var solution) &&
                solution.m_HasSnapTarget &&
                solution.m_ResolvedLaunchForward.sqrMagnitude > 0.0001f &&
                TryAssignNormalizedForward(solution.m_ResolvedLaunchForward, out launchForward);
        }

        private static bool TryAssignNormalizedForward(Vector3 forward, out Vector3 normalizedForward)
        {
            normalizedForward = Vector3.zero;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            normalizedForward = forward.normalized;
            return true;
        }

        protected virtual Vector3 ReadKeepShootOrigin(HitDetectionShapeType hitType)
        {
            switch (hitType)
            {
                case HitDetectionShapeType.SkillPosCircle:
                    return ReadSkillPos();
                case HitDetectionShapeType.CasterCircle:
                    return m_Attacker != null ? m_Attacker.GetPosition() : ReadSkillPos();
                default:
                    return ResolveAimOrigin();
            }
        }

        protected virtual void StartKeepShootEffect()
        {
            StopKeepShootEffect();
            if (m_SkillDescBean == null || m_SkillDescBean.t_atk_eff == 0)
            {
                return;
            }

            float effectDuration = ResolveAttackEffectDurationSecond();
            string[] mountPaths = ParseMountPaths(m_SkillDescBean.t_atk_eff_pos);
            bool hasMounted = false;

            if (mountPaths.Length > 0 && m_Attacker != null)
            {
                foreach (var singlePath in mountPaths)
                {
                    var mount = m_Attacker.ReadMountTransform(singlePath);
                    if (mount == null) continue;

                    var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_eff);
                    if (eff == null) continue;

                    m_KeepShootEffParentTransforms.Add(mount);
                    if (m_SkillDescBean.t_atk_eff_parent == 0)
                    {
                        eff.SetParent(mount);
                        eff.ShowEff(true, Vector3.zero, Vector2.zero, Vector3.one);
                    }
                    else
                    {
                        eff.SetParent(GameDll.RenderEffManager.GetInstance().GetRenderEffParent());
                        var pos = mount.position;
                        var rot = mount.eulerAngles;
                        var scale = mount.localScale;
                        eff.ShowEff(false, pos, rot, scale);
                    }
                    eff.SetDuringTime(effectDuration);
                    RenderEffManager.GetInstance().SetAutoPool(eff);
                    m_KeepShootEffs.Add(eff);
                    hasMounted = true;
                }
            }

            if (!hasMounted)
            {
                var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_eff);
                if (eff == null) return;

                m_KeepShootEffParentTransforms.Add(null);
                eff.ShowEffDir(false, ResolveAimOrigin(), ReadKeepShootForward(), Vector3.one);
                eff.SetDuringTime(effectDuration);
                RenderEffManager.GetInstance().SetAutoPool(eff);
                m_KeepShootEffs.Add(eff);
            }
        }

        protected virtual void StartShootEffect()
        {
            if (m_SkillDescBean == null || m_SkillDescBean.t_atk_eff == 0)
            {
                return;
            }

            float effectDuration = ResolveAttackEffectDurationSecond();
            string[] mountPaths = ParseMountPaths(m_SkillDescBean.t_atk_eff_pos);
            bool hasMounted = false;

            if (mountPaths.Length > 0 && m_Attacker != null)
            {
                foreach (var singlePath in mountPaths)
                {
                    var mount = m_Attacker.ReadMountTransform(singlePath);
                    if (mount == null) continue;

                    var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_eff);
                    if (eff == null) continue;
                    if(m_SkillDescBean.t_atk_eff_parent == 0)
                    {
                        eff.SetParent(mount);
                        eff.ShowEff(true, Vector3.zero, Vector2.zero, Vector3.one);
                    }
                    else
                    {
                        eff.SetParent(GameDll.RenderEffManager.GetInstance().GetRenderEffParent());
                        var pos = mount.position;
                        var rot = mount.eulerAngles;
                        var scale = mount.localScale;
                        eff.ShowEff(false, pos, rot, scale);
                    }

                    eff.SetDuringTime(effectDuration);
                    RenderEffManager.GetInstance().SetAutoPool(eff);
                    hasMounted = true;
                }
            }

            if (!hasMounted)
            {
                var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_eff);
                if (eff == null) return;

                // 非持续型施法特效由 SetAutoPool 回收，不加入 m_KeepShootEffs。
                eff.ShowEffDir(false, ResolveAimOrigin(), ReadKeepShootForward(), Vector3.one);
                eff.SetDuringTime(effectDuration);
                RenderEffManager.GetInstance().SetAutoPool(eff);
            }
        }

        protected virtual void StopKeepShootEffect()
        {
            foreach (var eff in m_KeepShootEffs)
            {
                RenderEffManager.GetInstance().PoolRenderEff(eff);
            }
            m_KeepShootEffs.Clear();
            m_KeepShootEffParentTransforms.Clear();
        }

        protected virtual void StopKeepShoot()
        {
            StopKeepShootEffect();
            m_KeepShootStartTime = -1;
            RenderEvent.Event.KeepSkill(this.GetCastId(), false);
        }

        public override void Stop()
        {
            StopKeepShoot();
            base.Stop();
        }

        public override void Destroy()
        {
            StopKeepShoot();
            base.Destroy();
        }

        protected override void OnCastOver()
        {
            StopKeepShoot();
            base.OnCastOver();
        }

        public override void OnSkillUnregister()
        {
            StopKeepShoot();
        }
    }
}
