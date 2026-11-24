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
        // 持续光束普攻（t_trajectory=3 激光 / 4 水柱）的存活光束：
        // 单次作用时间内再次触发只续期这颗子弹，不重新发弹
        private BulletObj m_ActiveLaserBullet;
        // 本次发弹是否复用了存活光束（续发）；OnCastPoint 据此跳过开火特效
        private bool m_LastCastRenewedLaser;
        // 施法生效点特效的兜底可见时长：t_keep_time 为 0 时避免创建后立即回收
        private const float m_DefaultShootEffectDurationSecond = 2.0f;
        protected List<RenderEff> m_KeepShootEffs = new List<RenderEff>();
        private List<Transform> m_KeepShootEffParentTransforms = new List<Transform>();
        protected float m_KeepShootStartTime = -1;
        protected HitDetectionData m_KeepShootHitData = new HitDetectionData();
        protected readonly List<KeepShootTargetCandidate> m_KeepShootCandidates = new List<KeepShootTargetCandidate>();
        private int m_BoneShotsFired;
        private int m_BonePendingCastCount;
        private float m_BoneKeepShootEndTime;

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
            if (ReadIsBoneNormalAttack())
            {
                m_BoneShotsFired = 0;
                m_BonePendingCastCount = 0;
                m_BoneKeepShootEndTime = -1f;
            }
            // 持续光束普攻（激光/水柱）：施法入场时光束仍存活，本次施法将按续发结算——不重播攻击动画，
            // 发弹点的攻击音效与开火特效同样按续发跳过（见 ShootBullet/OnCastPoint）。
            // 常规手势普攻在输入层续发、不会进入施法，这里覆盖的是直连 AttackDir 的施法路径。
            if (!ReadHasAliveLaserBullet())
            {
                // 被动施放不播自身动作，但音效/特效/发弹照常
                if (!ReadIsPassiveCast())
                {
                    PlayAction(m_CurAction.t_ac_name);
                }
                // t_atk_eff 是技能本身的特效，随施法动作在入场播；
                // 施法生效点的特效是独立字段 t_atk_cast_eff，在 OnCastPoint 播（见 StartShootEffect）
                // 水柱/激光的 t_atk_eff 是随着喷射一直存在的特效，必须跟 keep 窗口同寿（见 HoldSkillEffectForKeep）
                PlaySkillEffect(HoldSkillEffectForKeep());
            }
            //Debug.Log("开始播放子弹动画时间：" + Time.realtimeSinceStartup);
            m_CastStatus =  SkillCastStatus.warming_up;
        }

        public bool TryRenewBoneKeepShoot(Vector3 faceForward, Vector3 moveDirection)
        {
            if (!CanRenewBoneKeepShoot())
            {
                return false;
            }

            float now = BattleManager.ReadBattleTime();
            m_Attacker.RefreshDirSkillInput(this, faceForward, moveDirection);
            var battleTool = BattleManager.GetBattleTool();
            battleTool.ConsumeSkillPreconditionIfNeeded(m_Attacker, m_SkillBean);
            if (m_CastStatus == SkillCastStatus.cast_keep)
            {
                // 只延后结束时间，不重置发弹节拍、施法标识、前摇和已经运行的特效。
                m_BoneKeepShootEndTime = now + GetKeepShootDurationSecond();
                battleTool.AddSkillCastBuffs(m_Attacker, m_SkillBean);
                // 每次续期都是一次真实出手，按输入补一声攻击音效；
                // 窗口内按 t_hurt_interval 连发的子弹不再逐颗补声（见 ShootKeepModeBullet）。
                PlayAttackSound(0, m_Attacker.GetPosition());
            }
            else
            {
                // 前摇期间收到的有效输入只累计施法次数，在首发时结算，不排队重播攻击。
                m_BonePendingCastCount++;
            }
            return true;
        }

        public bool CanRenewBoneKeepShoot()
        {
            return ReadIsBoneNormalAttack() && UseKeepShootMode() &&
                (m_CastStatus == SkillCastStatus.warming_up ||
                 m_CastStatus == SkillCastStatus.cast_point ||
                 m_CastStatus == SkillCastStatus.cast_keep);
        }

        // 水柱/激光的 t_atk_eff 不是一次性特效，而是随喷射一直挂在枪口的持续特效，必须跟 keep 窗口同寿。
        // 按特效表 t_time 自动回收会出现两种错误表现：喷水 1 秒就结束时它还在喷到 3 秒；
        // 持续喷水超过 t_time 时它又会在中途消失。
        private bool HoldSkillEffectForKeep()
        {
            return UseKeepShootMode() && m_BulletBean != null &&
                BulletObj.IsContinuousBeamTrajectory(m_BulletBean.t_trajectory);
        }

        /// <summary>
        /// 当前技能的一次释放是否会按配置间隔连续生成多颗子弹。
        /// </summary>
        public bool ReadIsContinuousBulletCast()
        {
            return UseKeepShootMode();
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

            // 攻击音效按“一次出手一声”结算：首发放弹播一次（CommonBulletSkill 直接继承 Skill，
            // 不走 CommonSkill.OnWarmUp，这里是子弹技能攻击音效的唯一入场入口）；
            // keep 窗口内按 t_hurt_interval 的续发不再逐颗播，改为按续期输入补声（TryRenewBoneKeepShoot/TryRenewKeepShoot）。
            // 持续光束续发（复用存活光束）时整次触发静默：攻击音效在此跳过，开火特效在 OnCastPoint 跳过
            if (m_BulletBean == null || !BulletObj.IsContinuousBeamTrajectory(m_BulletBean.t_trajectory) || !ReadHasAliveLaserBullet())
            {
                var cfg = GetSkillDescBean();
                PlayAttackSound(cfg != null ? cfg.t_attackSound : 0, m_Attacker.GetPosition());
            }


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
            // 激光普攻兜底：施法流程走到发弹点而上一发光束仍存活时（如升级挑战直连 AttackDir 的普攻），
            // 复用光束续发，表现连续而结算口径与重新发弹一致；常规手势普攻已在 TryAttackBoneDir 输入层续发
            if (TryRenewActiveLaserBullet(defender))
            {
                return;
            }

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


            ApplyBulletCastPayload(bullet, defender);

            var bulletBean = bullet.GetBulletBean();
            if (bulletBean != null)
            {
                bullet.SetBulletConfig(
                    bulletBean.t_penetrate,
                    bulletBean.t_size,
                    bulletBean.t_trajectory,
                    bulletBean.t_tracking_range,
                    bulletBean.t_bullet_hittarget_buff_id,
                    bulletBean.t_trigger_bullet_id,
                    bulletBean.t_trigger_bullet_count,
                    bulletBean.t_trigger_type
                );
            }

            int defenderId = defender != null ? defender.ReadId() : 0;
            int bulletId = bulletBean != null ? bulletBean.t_id : 0;
            if (m_BulletBean != null && BulletObj.IsContinuousBeamTrajectory(m_BulletBean.t_trajectory))
            {
                m_ActiveLaserBullet = bullet;
            }

            objMgr.AddEffObject(bullet);
        }

        // 返回 true 表示已复用存活光束完成本次续发；返回 false 表示需要正常发弹。
        // 持续光束（激光/水柱）本质上仍是一颗一颗的子弹：续发只复用光束表现，超时时间、怪物去重、
        // 伤害快照与命中音效全部按本次触发重建，与重新发一颗激光没有区别。
        private bool TryRenewActiveLaserBullet(PropertyEntity defender)
        {
            if (m_BulletBean == null || !BulletObj.IsContinuousBeamTrajectory(m_BulletBean.t_trajectory) || !ReadHasAliveLaserBullet())
            {
                return false;
            }

            m_LastCastRenewedLaser = true;
            m_ActiveLaserBullet.RenewLaserLifeTime();
            m_ActiveLaserBullet.SetFlyDist(GetCastDistance());
            ApplyBulletCastPayload(m_ActiveLaserBullet, defender);
            return true;
        }

        // 光束是否仍存活；已死则顺手清空引用，避免残留对象干扰后续判断
        private bool ReadHasAliveLaserBullet()
        {
            if (m_ActiveLaserBullet == null || !m_ActiveLaserBullet.ReadIsLaserBeamAlive())
            {
                m_ActiveLaserBullet = null;
                return false;
            }

            return true;
        }

        // 每次触发的结算数据（伤害快照、命中音效、目标）：首发与激光续发共用，保证口径一致
        private void ApplyBulletCastPayload(BulletObj bullet, PropertyEntity defender)
        {
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
            hurtInfo.m_Attacker = m_Attacker;
            if (m_Attacker != null)
            {
                hurtInfo.m_HurtGroup = m_Attacker.ReadHurtGroup();
            }
            else
            {
                hurtInfo.m_HurtGroup = m_AttackerGroup;
            }
            hurtInfo.skillCfg = GetSkillBean();
            hurtInfo.skillDescCfg = desc_cfg;
            bullet.SetDefender(defender);
            if (defender != null)
            {
                bullet.SetDefenderPos(defender.ReadHitPoint());
            }
        }

        // 持续施法期间的循环音效（技能描述表 t_keepSound）：施法开始播放、施法结束或被打断时停止。
        // 连点期间由输入层续射顺延窗口，声音随施法连续不断；停手后随 StopKeepShoot 立即停止。
        private AudioHandle m_KeepSoundHandle;

        private void StartKeepShootSound()
        {
            if (!UseKeepShootMode() || m_SkillDescBean == null || m_SkillDescBean.t_keepSound == 0)
            {
                return;
            }

            var attacker = ReadAttacker();
            if (attacker == null)
            {
                return;
            }

            // 调用时序由上游保证单实例：OnCastPoint 每个施法周期仅进入一次；
            // resumeFromCastBack 到达 cast_back 前必经 StopKeepShoot（旧声已停）。
            m_KeepSoundHandle = AudioManager.GetInstance().Play3DLoop(
                m_SkillDescBean.t_keepSound,
                attacker.GetPosition());
        }

        private void StopKeepShootSound()
        {
            if (!AudioManager.GetInstance().IsAudioHandleValid(m_KeepSoundHandle))
            {
                m_KeepSoundHandle = default(AudioHandle);
                return;
            }

            AudioManager.GetInstance().StopAudio(m_KeepSoundHandle);
            m_KeepSoundHandle = default(AudioHandle);
        }

        // 持续释放期间每帧/每次续射都要重新确认释放条件仍然满足。
        // 例如“自动动作技能替换普攻”依赖的前置 buff 到期后，必须立即停掉当前持续释放，
        // 让后续普攻回到原配置技能（普攻也是持续型时继续接上）。
        protected virtual bool ReadKeepShootConditionStillMet()
        {
            if (!BattleManager.ReadIsEntityValide(m_Attacker))
            {
                return false;
            }

            if (m_Attacker.ReadIsBeingControlled())
            {
                return false;
            }

            var battleTool = BattleManager.GetBattleTool();
            return battleTool == null || battleTool.CheckKeepSkillPrecondition(m_Attacker, m_SkillBean);
        }

        // 自动动作技能（t_auto_action_skill_id，cast_style == 0）作为普攻替换时，
        // 持续释放结束后如果玩家仍在连续攻击、且普攻本身也是持续型，
        // 请求在技能真正结束后自动接续普攻，不需要玩家再点一次。
        private void TryRequestAutoNormalAttackContinue()
        {
            if (!(m_Attacker is PlayerHero hero) || m_SkillBean == null)
            {
                return;
            }

            if (!hero.IsTowerDefendAutoActionSkillSlot(ReadSlot()) || ReadCastStyle() != 0)
            {
                return;
            }

            if (!hero.ReadIsNormalAttackInputActive(PlayerHero.NormalAttackInputActiveWindowSeconds))
            {
                return;
            }

            var skillManager = hero.GetSkillManager();
            var normalSkill = skillManager != null ? skillManager.ReadSkillBySlot(0) : null;
            if (normalSkill is CommonBulletSkill normalBullet && normalBullet.ReadIsContinuousBulletCast())
            {
                hero.RequestAutoNormalAttackContinue();
            }
        }

        // 任意持续型子弹技能的续射判断：cast_keep 正常顺延；cast_back（窗口刚结束、
        // 仍在宽限期内）拉回发弹状态。激光和“自动动作技能替换普攻”的持续释放都走这里。
        public bool CanRenewKeepShoot()
        {
            return UseKeepShootMode() &&
                ReadKeepShootConditionStillMet() &&
                (m_CastStatus == SkillCastStatus.cast_keep || m_CastStatus == SkillCastStatus.cast_back);
        }

        // 持续光束发弹的续射判断（保持原语义：只对 t_trajectory == 3/4 生效）
        public bool CanRenewLaserKeepShoot()
        {
            return UseKeepShootMode() && m_BulletBean != null && BulletObj.IsContinuousBeamTrajectory(m_BulletBean.t_trajectory) &&
                CanRenewKeepShoot();
        }

        // 水柱普攻（t_trajectory=4）是否正在喷射：复用的存活子弹处于 Fly 状态。
        // 停喷后的收尾散水（AfterFinish）不算，瞄准线在停喷瞬间即可恢复显示。
        public bool ReadIsSprayStreamActive()
        {
            if (m_BulletBean == null || m_BulletBean.t_trajectory != 4 || !ReadHasAliveLaserBullet())
            {
                return false;
            }

            return m_ActiveLaserBullet.ReadIsSprayStreamFlying();
        }

        // 顺延发弹窗口续射：不重开施法、不重置发弹节拍，光束由后续子弹继续续期；
        // 前置条件消耗/施法 buff/伤害快照按一次真实攻击结算，与 TryRenewBoneKeepShoot 的续射口径一致。
        public bool TryRenewKeepShoot()
        {
            if (!CanRenewKeepShoot())
            {
                return false;
            }

            bool resumeFromCastBack = m_CastStatus == SkillCastStatus.cast_back;
            var battleTool = BattleManager.GetBattleTool();
            battleTool.ConsumeSkillPreconditionIfNeeded(m_Attacker, m_SkillBean);
            battleTool.AddSkillCastBuffs(m_Attacker, m_SkillBean);
            SetAtk(m_Attacker);
            m_KeepShootStartTime = BattleManager.ReadBattleTime();
            m_CastStatus = SkillCastStatus.cast_keep;
            // 每次续期都是一次真实出手，补一声攻击音效；连续光束由 t_keepSound 循环覆盖，整轮静默。
            if (m_BulletBean == null || !BulletObj.IsContinuousBeamTrajectory(m_BulletBean.t_trajectory))
            {
                PlayAttackSound(0, m_Attacker.GetPosition());
            }
            if (resumeFromCastBack)
            {
                RenderEvent.Event.KeepSkill(this.GetCastId(), true);
                // 宽限期续射：循环音效已随窗口结束停止，重新播放
                StartKeepShootSound();
                // 喷水/光束特效已随上一轮窗口停掉，续射重新挂上，避免继续喷水但枪口没特效
                if (HoldSkillEffectForKeep() && m_SkillEffs.Count == 0)
                {
                    PlaySkillEffect(true);
                }
            }
            return true;
        }

        public bool TryRenewLaserKeepShoot()
        {
            return m_BulletBean != null && BulletObj.IsContinuousBeamTrajectory(m_BulletBean.t_trajectory) && TryRenewKeepShoot();
        }

        // 输入层持续光束普攻续发入口：上一发光束仍在单次作用时间内时，直接续发现有光束并返回 true，
        // 本次触发不再进入施法流程——不重播攻击动画、攻击音效和开火特效。
        // 续发只复用光束表现：超时时间、怪物去重、伤害快照全部按本次触发重建，与重新发一颗激光没有区别；
        // 前置条件消耗与施法 buff 也按一次真实攻击结算，与 TryRenewBoneKeepShoot 的续射口径一致。
        public bool TryRenewLaserNormalAttack(PropertyEntity attacker)
        {
            if (m_BulletBean == null || !BulletObj.IsContinuousBeamTrajectory(m_BulletBean.t_trajectory) || !ReadHasAliveLaserBullet())
            {
                return false;
            }

            SetAtk(attacker);
            var battleTool = BattleManager.GetBattleTool();
            battleTool.ConsumeSkillPreconditionIfNeeded(m_Attacker, m_SkillBean);
            battleTool.AddSkillCastBuffs(m_Attacker, m_SkillBean);
            m_ActiveLaserBullet.RenewLaserLifeTime();
            m_ActiveLaserBullet.SetFlyDist(GetCastDistance());
            ApplyBulletCastPayload(m_ActiveLaserBullet, null);
            return true;
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            if (ReadIsBoneNormalAttack())
            {
                UpdateBoneNormalAttack();
                return;
            }
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
                        break;
                    }
                case SkillCastStatus.cast_point:
                    {
                        OnCastPoint();
                        break;
                    }
                case SkillCastStatus.cast_keep:
                    {
                        // 持续释放期间前置条件不再满足（如依赖 buff 到期）时立即结束，
                        // 不再按剩余 keep 时间继续出伤害。
                        if (!ReadKeepShootConditionStillMet())
                        {
                            TryRequestAutoNormalAttackContinue();
                            OnCastOver();
                            break;
                        }

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

        private void UpdateBoneNormalAttack()
        {
            if (m_CastStatus == SkillCastStatus.end || m_Attacker == null ||
                m_Attacker.ReadCurrentSkill() != this)
            {
                return;
            }
            if (!BattleManager.ReadIsEntityValide(m_Attacker) || m_Attacker.ReadIsBeingControlled() ||
                m_Attacker.ReadIsSuspended() || m_Attacker.ReadIsFrozen())
            {
                Stop();
                return;
            }

            float progress = ReadBoneActionFrameProgress();
            int castFrame = Mathf.Max(0, m_CurAction.t_ac_cast_point);
            float finishFrame = Mathf.Max(castFrame + 1f, m_CurAction.t_ac_finish);
            if ((m_CastStatus == SkillCastStatus.warming_up || m_CastStatus == SkillCastStatus.cast_point) &&
                progress >= castFrame)
            {
                // 先标记首发，重复更新不能重新开始本轮攻击。
                m_BoneShotsFired = 1;
                m_CastStatus = SkillCastStatus.cast_back;
                OnCastPoint();
            }
            if (m_CastStatus == SkillCastStatus.cast_keep)
            {
                // 前置条件不再满足时立即结束持续释放（与普通 Update 口径一致）。
                if (!ReadKeepShootConditionStillMet())
                {
                    TryRequestAutoNormalAttackContinue();
                    OnCastOver();
                    return;
                }

                float now = BattleManager.ReadBattleTime();
                bool emissionFinished;
                if (UseKeepShootMode())
                {
                    float interval = GetKeepShootIntervalSecond();
                    float dueTime = Mathf.Min(now, m_BoneKeepShootEndTime);
                    // 发弹使用战斗时间，不受动画加速和续期影响；低帧率只补结算结束前已到期的子弹。
                    while (m_KeepShootStartTime + m_BoneShotsFired * interval <= dueTime + 0.0001f)
                    {
                        m_BoneShotsFired++;
                        AgainShootBullet();
                    }
                    emissionFinished = now >= m_BoneKeepShootEndTime;
                }
                else
                {
                    // 单发特效从实际出弹时保留一帧，避免跨帧更新时刚创建就被停止。
                    emissionFinished = now >= m_KeepShootStartTime + ResolveAttackEffectDurationSecond();
                }

                if (emissionFinished)
                {
                    StopKeepShoot();
                    m_CastStatus = SkillCastStatus.cast_back;
                }
                else
                {
                    foreach (var eff in m_KeepShootEffs)
                    {
                        eff.SetParticleSpeed(ReadBoneActionSpeed());
                    }
                }
            }
            if (m_CastStatus == SkillCastStatus.cast_back && progress >= finishFrame)
            {
                OnCastOver();
            }
        }

        protected override float ReadBoneNormalAttackTargetSpeed()
        {
            // 连射通过续期维持，不需要反复加快或重播前摇；单发仍按输入频率调整动作。
            return UseKeepShootMode() ? 1f : base.ReadBoneNormalAttackTargetSpeed();
        }

        private void OnCastPoint()
        {
            ApplySkillCastBuffIfNeeded();
            while (m_BonePendingCastCount > 0)
            {
                m_BonePendingCastCount--;
                BattleManager.GetBattleTool().AddSkillCastBuffs(m_Attacker, m_SkillBean);
            }
            m_LastCastRenewedLaser = false;
            FirstShootBullet();
            if (UseKeepShootMode() || ReadIsBoneNormalAttack())
            {
                // keep 型施法特效在续发时沿用还挂着的实例（见 StartKeepShootEffect），不会重播闪烁
                StartKeepShootEffect();
                StartKeepShootSound();
                m_KeepShootStartTime = BattleManager.ReadBattleTime();
                if (ReadIsBoneNormalAttack() && UseKeepShootMode())
                {
                    m_BoneKeepShootEndTime = m_KeepShootStartTime + GetKeepShootDurationSecond();
                }

                m_CastStatus = SkillCastStatus.cast_keep;
                if (UseKeepShootMode())
                {
                    RenderEvent.Event.KeepSkill(this.GetCastId(), true);
                }
            }
            else
            {
                if (!m_LastCastRenewedLaser)
                {
                    StartShootEffect();
                }
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
            if (ReadIsBoneNormalAttack())
            {
                return UseKeepShootMode() ? GetKeepShootDurationSecond() :
                    1f / (GetActionFrameRate() * ReadBoneActionSpeed());
            }
            if (UseKeepShootMode())
            {
                return Mathf.Max(0.1f, GetKeepShootDurationSecond());
            }

            // 非持续子弹技能也要给开火特效可见时长，
            // 否则 t_keep_time 为 0 时会在创建后立即回收。
            return m_DefaultShootEffectDurationSecond;
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
            // 攻击音效不跟发弹节拍逐颗播放（一次施法会连发多颗，逐颗补声会让一次普攻叠出好几声），
            // 改由续期输入补声（见 TryRenewBoneKeepShoot/TryRenewKeepShoot）。
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
            // 上一轮的开火特效还挂着（未被回收）时直接沿用——连点续发/轮间衔接期间炮口特效不闪断。
            // 回收只发生在 StopKeepShootEffect，因此这里看到未回池实例即表示施法连续、特效仍在展示。
            foreach (var existing in m_KeepShootEffs)
            {
                if (existing != null && !existing.m_IsPooled)
                {
                    return;
                }
            }

            StopKeepShootEffect();
            if (m_SkillDescBean == null || m_SkillDescBean.t_atk_cast_eff == 0)
            {
                return;
            }

            float effectDuration = ResolveAttackEffectDurationSecond();
            string[] mountPaths = ParseMountPaths(m_SkillDescBean.t_atk_cast_eff_pos);
            bool hasMounted = false;

            if (mountPaths.Length > 0 && m_Attacker != null)
            {
                foreach (var singlePath in mountPaths)
                {
                    var mount = m_Attacker.ReadMountTransform(singlePath);
                    if (mount == null) continue;

                    var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_cast_eff);
                    if (eff == null) continue;

                    m_KeepShootEffParentTransforms.Add(mount);
                    if (m_SkillDescBean.t_atk_cast_eff_parent == 0)
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
                    if (ReadIsBoneNormalAttack())
                    {
                        eff.SetParticleSpeed(ReadBoneActionSpeed());
                    }
                    // keep 型特效不走 SetAutoPool：自动回收时长固定为首发窗口，续射顺延后会中途消失；
                    // 统一由 StopKeepShootEffect 在施法结束时回收
                    m_KeepShootEffs.Add(eff);
                    hasMounted = true;
                }
            }

            if (!hasMounted)
            {
                var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_cast_eff);
                if (eff == null) return;

                m_KeepShootEffParentTransforms.Add(null);
                eff.ShowEffDir(false, ResolveAimOrigin(), ReadKeepShootForward(), Vector3.one);
                eff.SetDuringTime(effectDuration);
                if (ReadIsBoneNormalAttack())
                {
                    eff.SetParticleSpeed(ReadBoneActionSpeed());
                }
                m_KeepShootEffs.Add(eff);
            }
        }

        protected virtual void StartShootEffect()
        {
            if (m_SkillDescBean == null || m_SkillDescBean.t_atk_cast_eff == 0)
            {
                return;
            }

            float effectDuration = ResolveAttackEffectDurationSecond();
            string[] mountPaths = ParseMountPaths(m_SkillDescBean.t_atk_cast_eff_pos);
            bool hasMounted = false;

            if (mountPaths.Length > 0 && m_Attacker != null)
            {
                foreach (var singlePath in mountPaths)
                {
                    var mount = m_Attacker.ReadMountTransform(singlePath);
                    if (mount == null) continue;

                    var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_cast_eff);
                    if (eff == null) continue;
                    if(m_SkillDescBean.t_atk_cast_eff_parent == 0)
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
                var eff = RenderEffManager.GetInstance().CreateRenderEff(m_SkillDescBean.t_atk_cast_eff);
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
                if (ReadIsBoneNormalAttack())
                {
                    RenderEffManager.GetInstance().StopEmissionAndAutoPool(eff);
                }
                else
                {
                    RenderEffManager.GetInstance().PoolRenderEff(eff);
                }
            }
            m_KeepShootEffs.Clear();
            m_KeepShootEffParentTransforms.Clear();
        }

        protected virtual void StopKeepShoot()
        {
            // 持续光束发弹结束：激光随施法一起立即收回——发弹窗口就是光束的可见时长；
            // 水柱则先停发射，等已经喷出去的水滴自然消失（见 FinishLaserBeam）。
            // 连点期间窗口由输入层续射顺延（TryRenewLaserKeepShoot），施法不会结束、光束不断；
            // 只有真正停手（超过一个窗口没有点击）才走到这里。
            if (UseKeepShootMode() && m_BulletBean != null && BulletObj.IsContinuousBeamTrajectory(m_BulletBean.t_trajectory) &&
                m_ActiveLaserBullet != null)
            {
                m_ActiveLaserBullet.FinishLaserBeam();
                m_ActiveLaserBullet = null;
            }

            // 技能特效（t_atk_eff）与喷射同寿：停喷就停，不能等到特效表 t_time 到期才消失。
            StopSkillEffect();

            StopKeepShootSound();
            StopKeepShootEffect();
            m_KeepShootStartTime = -1;
            m_BonePendingCastCount = 0;
            m_BoneKeepShootEndTime = -1f;
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
            // 销毁后 Update 不再驱动，宽限停止计划无人执行，必须立即停
            StopKeepShootSound();
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
            // 注销后 Update 不再驱动，宽限停止计划无人执行，必须立即停
            StopKeepShootSound();
        }
    }
}
