using LCL;
using MonoBean;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    internal sealed class TowerDefendAimAssistLine
    {
        private const string m_PrefabAbNameFormat = "prefab/aim/eff_player{0}_aim_line.jpg";
        private const int m_MinRuntimePointCount = 20;
        private const int m_ParabolicSegmentCount = 48;
        private const float m_FallbackWidth = 0.06f;
        private const float m_GroundOffset = 0.02f;
        private const int m_ImpactEffectId = 100;

        // 激光下压收口：向下最多若干度（BattleConst.TowerDefendLaserMaxDownPitchDegrees，默认 5 度，GMTools 可实时调），
        // 超过则压回下限。修改即生效——瞄准线、姿态俯仰、光束方向每帧都读该值。
        // 角色姿态、瞄准线、光束统一走这里（TowerDefendBattle / AimAssistLine / BulletObj 三处调用）。
        // 这是角色2激光的刻意口径：光束允许被压平（可能从远处被 Curve World 弯下去的怪物上方掠过），
        // 但命中判定必须以瞄准线的视觉接触为准——瞄准线碰到怪物时，伤害与攻击特效就在接触位置产生。
        public static Vector3 ClampLaserForwardPitch(Vector3 forward)
        {
            if (forward.y >= 0.0f)
            {
                return forward;
            }

            var horizontalSqr = forward.x * forward.x + forward.z * forward.z;
            if (horizontalSqr <= 0.0001f)
            {
                return forward;
            }

            var maxDownY = -Mathf.Tan(BattleConst.TowerDefendLaserMaxDownPitchDegrees * Mathf.Deg2Rad) * Mathf.Sqrt(horizontalSqr);
            if (forward.y >= maxDownY)
            {
                return forward;
            }

            forward.y = maxDownY;
            forward.Normalize();
            return forward;
        }

        private readonly GameObject m_LevelRoot;
        private readonly List<Vector3> m_AimPoints = new List<Vector3>(m_ParabolicSegmentCount);
        private readonly List<Vector3> m_RenderPoints = new List<Vector3>(m_ParabolicSegmentCount);

        private GameObject m_LineRoot;
        private LineRenderer m_LineRenderer;
        private ABRequest m_LineRequest;
        private int m_LoadedStyleId = -1;
        private int m_LoadingStyleId = -1;
        private RenderEff m_ImpactEff;
        private bool m_HasImpactPoint;
        private Vector3 m_ImpactPoint;
        // 激光类普攻（trajectory=3）：瞄准线按单位“视觉位置”（Curve World 弯折偏移后）做命中截断，
        // 与直线激光的瞄准/判定同口径；同时瞄准线材质关闭弯曲，保持直线渲染
        private bool m_UseVisualOffsetTargets;
        // 穿透激光（t_penetrate=0）：光束不在怪物处截断，瞄准线也必须同样穿透画到边界，否则两者后半段对不齐
        private bool m_LaserPierces;
        private MaterialPropertyBlock m_LinePropertyBlock;
        private bool m_BendOffApplied;

        public TowerDefendAimAssistLine(GameObject levelRoot)
        {
            m_LevelRoot = levelRoot;
        }

        public void Update(TowerDefendBattle battle, int seatId)
        {
            if (battle == null || m_LevelRoot == null || !TowerDefendSeatLayout.IsValidSeatId(seatId))
            {
                Hide();
                return;
            }

            var spawer = battle.ReadBattleSpawer() as TowerDefendBattleSpawer;
            if (spawer == null)
            {
                Hide();
                return;
            }

            var hero = spawer.ReadGuardHeroBySeat(seatId);
            if (!BattleManager.ReadIsEntityValide(hero))
            {
                Hide();
                return;
            }

            var skill = hero.ReadNormalSkill();
            var skillBean = skill != null ? skill.GetSkillBean() : null;
            var bulletCfg = skillBean != null ? t_bullet.GetConfig(skillBean.t_bullet_id, false) : null;
            if (skill == null || bulletCfg == null)
            {
                Hide();
                return;
            }

            // 水柱普攻（trajectory=4）喷射期间瞄准线与水柱重叠观感差：喷水时整线隐藏，
            // 停喷（含收尾散水）后恢复。激光（trajectory=3）保持原口径——瞄准线与光束常显对齐。
            if (skill is CommonBulletSkill bulletSkill && bulletSkill.ReadIsSprayStreamActive())
            {
                Hide();
                return;
            }

            m_UseVisualOffsetTargets = bulletCfg.t_trajectory == 3;
            m_LaserPierces = m_UseVisualOffsetTargets && bulletCfg.t_penetrate == 0;

            var start = hero.ReadResolvedFirePointCenter();
            var forward = battle.ReadManualAimForwardBySeat(seatId);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                Hide();
                return;
            }

            // 瞄准线样式只由固定座位决定，角色切换不会改变该位置对应的预制件。
            EnsureLine(seatId);
            if (m_LineRenderer == null)
            {
                HideImpactEffect();
                return;
            }

            ApplyBendOffState(m_UseVisualOffsetTargets);

            m_HasImpactPoint = false;
            forward.Normalize();
            if (m_UseVisualOffsetTargets)
            {
                // 激光：向下最多 5 度（手操方向先收口，与角色姿态/光束同口径）
                forward = ClampLaserForwardPitch(forward);
            }

            var hasAutoAimSolution =
                battle.TryResolveGuardHeroNormalAutoAim(hero, skill, forward, out var autoAimSolution) &&
                autoAimSolution.m_HasSnapTarget;

            if (m_UseVisualOffsetTargets)
            {
                // 激光：瞄准线必须与光束严格同源——同一起点（同一开火点约束）、同一方向（入口的瞄准朝向）。
                // 瞄准朝向就是驱动角色俯仰姿态的那份方向；开火点节点朝向会被攻击动画改写
                // （瞄准态 spine 保持住俯仰，施法态被动画关键帧带回水平），读节点画线就会
                // 和施法中的激光对不齐。光束侧（BulletObj.UpdateLaserBeam）同样优先读瞄准朝向。
                // 激光的伤害/特效不依赖光束必须穿过怪物身体——以瞄准线的视觉接触为准（见 BulletObj 命中段）。
                start = BulletFlightConstraintUtility.ResolveConstrainedPosition(bulletCfg, start);
                PopulateStraightLine(battle, hero, start, forward, hero.GetSkillCastDist(skill));
            }
            else if (hasAutoAimSolution)
            {
                // 自动吸附现在只负责修正最终发射方向。
                // 角色姿态统一由 TowerDefendBattle 内部缓存与 RefreshControlledSeatAimPose 回写，
                // 这里不再额外直接改角色俯仰，避免 Scene 和 Battle 两条链路在同一帧互相覆盖。
                // 瞄准线仍统一走通用弹道函数，避免再维护一套“目标点即终点”的专用绘线逻辑。
                if (autoAimSolution.m_UseParabolic)
                {
                    PopulateParabolicLine(battle, hero, start, autoAimSolution.m_ResolvedLaunchForward, bulletCfg);
                }
                else
                {
                    PopulateStraightLine(battle, hero, start, autoAimSolution.m_ResolvedLaunchForward, hero.GetSkillCastDist(skill));
                }
            }
            else if (TowerDefendBattle.ShouldUseParabolicAim(skill, bulletCfg))
            {
                PopulateParabolicLine(battle, hero, start, forward, bulletCfg);
            }
            else
            {
                PopulateStraightLine(battle, hero, start, forward, hero.GetSkillCastDist(skill));
            }

            SetVisible(true);
            UpdateImpactEffect();
        }

        public void Hide()
        {
            SetVisible(false);
            HideImpactEffect();
        }

        public void Destroy()
        {
            HideImpactEffect();
            UnloadLineRequest();
            DestroyLineInstance();
        }

        private void EnsureLine(int styleId)
        {
            if (m_LevelRoot == null)
            {
                return;
            }

            styleId = NormalizeStyleId(styleId);
            if (m_LineRenderer != null)
            {
                if (m_LoadedStyleId == styleId)
                {
                    return;
                }

                DestroyLineInstance();
            }

            if (m_LineRequest != null)
            {
                if (m_LoadingStyleId == styleId)
                {
                    return;
                }

                UnloadLineRequest();
            }

            string prefabPath = string.Format(m_PrefabAbNameFormat, styleId);
            m_LoadingStyleId = styleId;
            m_LineRequest = UIRes.LoadPrefabAsync(
                typeof(GameObject),
                prefabPath,
                Tool.GetAssetName(prefabPath),
                (resData, userData) => OnLineLoaded(styleId, resData));
            if (m_LineRequest == null)
            {
                m_LoadingStyleId = -1;
                CreateFallbackLine(styleId);
            }
        }

        private static int NormalizeStyleId(int styleId)
        {
            if (TowerDefendSeatLayout.IsValidSeatId(styleId))
            {
                return styleId;
            }

            return TowerDefendSeatLayout.DefaultControlledSeatId;
        }

        private void OnLineLoaded(int styleId, ResData resData)
        {
            if (m_LoadingStyleId != styleId)
            {
                return;
            }

            m_LineRequest = null;
            m_LoadingStyleId = -1;
            if (m_LevelRoot == null || resData == null || resData.m_Obj == null)
            {
                CreateFallbackLine(styleId);
                return;
            }

            var prefab = resData.m_Obj as GameObject;
            if (prefab == null)
            {
                CreateFallbackLine(styleId);
                return;
            }

            DestroyLineInstance();

            m_LineRoot = GameObject.Instantiate(prefab);
            m_LineRoot.transform.SetParent(m_LevelRoot.transform, false);
            m_LineRenderer = m_LineRoot.GetComponent<LineRenderer>();
            if (m_LineRenderer == null)
            {
                DestroyLineInstance();
                CreateFallbackLine(styleId);
                return;
            }

            ApplyLoadedLineDefaults();
            m_LoadedStyleId = styleId;
            SetVisible(false);
        }

        private void ApplyLoadedLineDefaults()
        {
            if (m_LineRenderer == null)
            {
                return;
            }

            m_LineRenderer.enabled = true;
            m_LineRenderer.useWorldSpace = true;
            m_LineRenderer.loop = false;
            if (m_LineRenderer.positionCount < 2)
            {
                m_LineRenderer.positionCount = 2;
            }
        }

        private void CreateFallbackLine(int styleId)
        {
            if (m_LevelRoot == null)
            {
                return;
            }

            DestroyLineInstance();

            m_LineRoot = new GameObject(string.Format("tower_defend_aim_assist_line_{0}", styleId));
            m_LineRoot.transform.SetParent(m_LevelRoot.transform, false);

            m_LineRenderer = m_LineRoot.AddComponent<LineRenderer>();
            m_LineRenderer.useWorldSpace = true;
            m_LineRenderer.loop = false;
            m_LineRenderer.widthMultiplier = m_FallbackWidth;
            m_LineRenderer.positionCount = 2;
            m_LineRenderer.numCapVertices = 4;
            m_LineRenderer.numCornerVertices = 4;
            m_LineRenderer.alignment = LineAlignment.View;
            m_LineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_LineRenderer.receiveShadows = false;
            m_LineRenderer.textureMode = LineTextureMode.Stretch;
            m_LineRenderer.startColor = new Color(0.35f, 0.95f, 1.0f, 0.95f);
            m_LineRenderer.endColor = new Color(1.0f, 0.95f, 0.45f, 0.25f);

            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                m_LineRenderer.material = new Material(shader);
            }

            m_LoadedStyleId = styleId;
            SetVisible(false);
        }

        private void PopulateStraightLine(TowerDefendBattle battle, PropertyEntity attacker, Vector3 start, Vector3 forward, float distance)
        {
            if (m_LineRenderer == null)
            {
                return;
            }

            var castDistance = Mathf.Max(0.5f, distance);
            var end = start + forward * castDistance;

            m_AimPoints.Clear();
            m_AimPoints.Add(start);

            if (TryResolveAimSegment(battle, attacker, start, end, out var lineEnd, out _, out var hasImpact, out var impactPoint))
            {
                end = lineEnd;
                if (hasImpact)
                {
                    m_HasImpactPoint = true;
                    m_ImpactPoint = impactPoint;
                }
            }

            m_AimPoints.Add(end);

            ApplyAimPoints();
        }

        private void PopulateParabolicLine(TowerDefendBattle battle, PropertyEntity attacker, Vector3 start, Vector3 forward, t_bullet bulletCfg)
        {
            if (m_LineRenderer == null || bulletCfg == null)
            {
                return;
            }

            var moveSpeed = bulletCfg.t_move_speed / 1000.0f;
            var maxTime = Mathf.Max(0.2f, bulletCfg.t_max_time / 1000.0f);
            var velocity = forward * moveSpeed;
            var gravity = TowerDefendBattle.ResolveRuntimeBulletGravityAcceleration(attacker, bulletCfg);

            m_AimPoints.Clear();
            m_AimPoints.Add(start);

            var previous = start;
            for (int i = 0; i < m_ParabolicSegmentCount; i++)
            {
                var t = maxTime * i / (m_ParabolicSegmentCount - 1);
                var pos = start + velocity * t;
                pos.y -= 0.5f * gravity * t * t;

                if (i == 0)
                {
                    previous = pos;
                    continue;
                }

                if (TryResolveAimSegment(battle, attacker, previous, pos, out var lineEnd, out var stopped, out var hasImpact, out var impactPoint))
                {
                    if (hasImpact)
                    {
                        m_HasImpactPoint = true;
                        m_ImpactPoint = impactPoint;
                    }

                    if (stopped)
                    {
                        m_AimPoints.Add(lineEnd);
                        break;
                    }
                }

                m_AimPoints.Add(pos);
                previous = pos;
            }

            if (m_AimPoints.Count < 2)
            {
                m_AimPoints.Add(previous);
            }

            ApplyAimPoints();
        }

        private void ApplyAimPoints()
        {
            if (m_LineRenderer == null)
            {
                return;
            }

            List<Vector3> renderPoints = m_AimPoints;
            if (m_AimPoints.Count < m_MinRuntimePointCount)
            {
                ResamplePoints(m_AimPoints, m_RenderPoints, m_MinRuntimePointCount);
                renderPoints = m_RenderPoints;
            }

            var pointCount = Mathf.Max(2, renderPoints.Count);
            m_LineRenderer.positionCount = pointCount;
            for (int i = 0; i < pointCount; i++)
            {
                m_LineRenderer.SetPosition(i, renderPoints[Mathf.Min(i, renderPoints.Count - 1)]);
            }
        }

        private static void ResamplePoints(List<Vector3> sourcePoints, List<Vector3> outputPoints, int targetPointCount)
        {
            outputPoints.Clear();
            if (sourcePoints == null || sourcePoints.Count <= 0 || targetPointCount <= 0)
            {
                return;
            }

            if (sourcePoints.Count == 1)
            {
                for (int i = 0; i < targetPointCount; i++)
                {
                    outputPoints.Add(sourcePoints[0]);
                }

                return;
            }

            float totalLength = 0f;
            for (int i = 1; i < sourcePoints.Count; i++)
            {
                totalLength += Vector3.Distance(sourcePoints[i - 1], sourcePoints[i]);
            }

            if (totalLength <= 0.0001f)
            {
                Vector3 staticPoint = sourcePoints[sourcePoints.Count - 1];
                for (int i = 0; i < targetPointCount; i++)
                {
                    outputPoints.Add(staticPoint);
                }

                return;
            }

            outputPoints.Add(sourcePoints[0]);

            int segmentIndex = 1;
            float segmentStartDistance = 0f;
            Vector3 segmentStart = sourcePoints[0];
            Vector3 segmentEnd = sourcePoints[1];
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            float stepLength = totalLength / (targetPointCount - 1);

            for (int i = 1; i < targetPointCount - 1; i++)
            {
                float targetDistance = stepLength * i;
                while (segmentIndex < sourcePoints.Count - 1 &&
                       segmentStartDistance + segmentLength < targetDistance)
                {
                    segmentStartDistance += segmentLength;
                    segmentStart = sourcePoints[segmentIndex];
                    segmentIndex++;
                    segmentEnd = sourcePoints[segmentIndex];
                    segmentLength = Vector3.Distance(segmentStart, segmentEnd);
                }

                float segmentT = segmentLength <= 0.0001f
                    ? 0f
                    : Mathf.Clamp01((targetDistance - segmentStartDistance) / segmentLength);
                outputPoints.Add(Vector3.Lerp(segmentStart, segmentEnd, segmentT));
            }

            outputPoints.Add(sourcePoints[sourcePoints.Count - 1]);
        }

        // 解析一段瞄准线：lineEnd 为最终线端，stopped 表示该段被阻挡/收束，hasImpact/impactPoint 为
        // 线端之前的第一个接触点特效。
        // 两种口径都遵循“先接触者出接触特效”：
        //  - 普通座位：怪物/NPC/棒棒糖/地面中最近者为阻挡点，线端与其重合、特效同位；
        //  - 穿透激光：怪物只算接触不算阻挡（线继续穿向地面/边界），但第一个怪物接触仍要显示特效，
        //    不能因为穿透就完全不检测怪物，否则线掠过怪物时特效会跑到更远的地面上或干脆不出现。
        // 激光座位的接触点都是完整弯折后的视觉点（特效材质 BendOff，与线端同位）；普通座位都是逻辑点。
        // 战斗关注物 Z 平面收束只在没有目标/地面阻挡时生效，且不是接触点，不出特效。
        private bool TryResolveAimSegment(
            TowerDefendBattle battle,
            PropertyEntity attacker,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out Vector3 lineEnd,
            out bool stopped,
            out bool hasImpact,
            out Vector3 impactPoint)
        {
            lineEnd = segmentEnd;
            stopped = false;
            hasImpact = false;
            impactPoint = segmentEnd;

            if (!BattleManager.ReadIsEntityValide(attacker))
            {
                return false;
            }

            var hasTargetBlock = false;
            var targetBlockT = float.MaxValue;
            var targetBlockPoint = segmentEnd;
            var lineEndT = 1f;
            var contactT = float.MaxValue;
            var contactPoint = segmentEnd;

            if (TryFindMonsterHit(attacker, segmentStart, segmentEnd, out var monsterHitT, out var monsterHitPoint, out var monsterImpactPoint))
            {
                contactT = monsterHitT;
                contactPoint = monsterImpactPoint;
                hasImpact = true;
                // 穿透激光不在怪物处截断，但怪物仍是路径上的第一个接触点，接触特效必须保留
                if (!m_LaserPierces)
                {
                    hasTargetBlock = true;
                    targetBlockT = monsterHitT;
                    targetBlockPoint = monsterHitPoint;
                }
            }

            if (TryFindNpcSpeakIntroHit(battle, segmentStart, segmentEnd, out var npcHitT, out var npcHitPoint))
            {
                if (!hasImpact || npcHitT < contactT)
                {
                    contactT = npcHitT;
                    contactPoint = npcHitPoint;
                    hasImpact = true;
                }

                if (!hasTargetBlock || npcHitT < targetBlockT)
                {
                    hasTargetBlock = true;
                    targetBlockT = npcHitT;
                    targetBlockPoint = npcHitPoint;
                }
            }

            if (TryFindUpgradeChallengeHit(battle, segmentStart, segmentEnd, out var upgradeHitT, out var upgradeHitPoint, out var upgradeImpactPoint))
            {
                if (!hasImpact || upgradeHitT < contactT)
                {
                    contactT = upgradeHitT;
                    contactPoint = upgradeImpactPoint;
                    hasImpact = true;
                }

                if (!hasTargetBlock || upgradeHitT < targetBlockT)
                {
                    hasTargetBlock = true;
                    targetBlockT = upgradeHitT;
                    targetBlockPoint = upgradeHitPoint;
                }
            }

            var hasGroundHit = TryFindGroundHit(
                attacker,
                segmentStart,
                segmentEnd,
                m_UseVisualOffsetTargets,
                out var groundHitT,
                out var groundHitPoint,
                out var groundImpactPoint);
            if (hasGroundHit && (!hasImpact || groundHitT < contactT))
            {
                contactT = groundHitT;
                contactPoint = groundImpactPoint;
                hasImpact = true;
            }

            if (hasTargetBlock)
            {
                if (hasGroundHit && groundHitT < targetBlockT)
                {
                    lineEnd = groundHitPoint;
                    lineEndT = groundHitT;
                }
                else
                {
                    lineEnd = targetBlockPoint;
                    lineEndT = targetBlockT;
                }

                stopped = true;
            }
            else if (hasGroundHit)
            {
                if (TryFindNoTargetAimClampHit(battle, segmentStart, segmentEnd, out var groundClampT, out var groundClampPoint) &&
                    groundClampT <= groundHitT)
                {
                    lineEnd = groundClampPoint;
                    lineEndT = groundClampT;
                }
                else
                {
                    lineEnd = groundHitPoint;
                    lineEndT = groundHitT;
                }

                stopped = true;
            }
            else if (TryFindNoTargetAimClampHit(battle, segmentStart, segmentEnd, out var clampHitT, out var clampPoint))
            {
                lineEnd = clampPoint;
                lineEndT = clampHitT;
                stopped = true;
            }

            // 接触点画在线端之外（例如穿透激光的怪物位于 Z 平面收束点之后）时不显示特效
            if (hasImpact && contactT > lineEndT)
            {
                hasImpact = false;
            }

            impactPoint = contactPoint;
            return stopped || hasImpact;
        }

        // 真实目标没有命中时，才允许用战斗关注物边界或地面来收束瞄准线。
        // 关注物边界不是攻击目标，因此不会显示命中特效；地面仍然保持旧的落点表现。
        private static bool TryResolveNoTargetBoundary(
            TowerDefendBattle battle,
            PropertyEntity attacker,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            bool useVisualOffset,
            out Vector3 boundaryPoint,
            out bool isGroundHit,
            out Vector3 impactPoint)
        {
            boundaryPoint = segmentEnd;
            impactPoint = segmentEnd;
            isGroundHit = false;

            var hasBoundary = false;
            var nearestHitT = float.MaxValue;
            if (TryFindNoTargetAimClampHit(battle, segmentStart, segmentEnd, out var clampHitT, out var clampPoint))
            {
                nearestHitT = clampHitT;
                boundaryPoint = clampPoint;
                impactPoint = clampPoint;
                hasBoundary = true;
            }

            if (TryFindGroundHit(attacker, segmentStart, segmentEnd, useVisualOffset, out var groundHitT, out var groundPoint, out var groundImpactPoint) &&
                (!hasBoundary || groundHitT < nearestHitT))
            {
                boundaryPoint = groundPoint;
                impactPoint = groundImpactPoint;
                isGroundHit = true;
                hasBoundary = true;
            }

            return hasBoundary;
        }

        // 供激光弹道复用的终点收束：与瞄准线同源的边界/地面截断，但不做怪物命中截断——
        // 激光按穿透弹道结算，光束要能越过头一只怪打到射程内整条线上的目标。
        // useVisualOffset：激光传 true，地面截断按“可见地面”求交，避免光束被看不见的逻辑地面提前切断。
        public static Vector3 ResolveLaserBeamEnd(
            TowerDefendBattle battle,
            PropertyEntity attacker,
            Vector3 start,
            Vector3 forward,
            float distance,
            bool useVisualOffset)
        {
            var end = start + forward * Mathf.Max(0.5f, distance);
            if (TryResolveNoTargetBoundary(battle, attacker, start, end, useVisualOffset, out var clampedEnd, out _, out _))
            {
                end = clampedEnd;
            }

            return end;
        }

        // 无真实目标时，瞄准线最远只能延伸到当前战斗关注物所在的 Z 平面前方。
        // 喊话阶段使用喊话点，棒棒糖出现后切到棒棒糖中心，棒棒糖结束后再回退。
        private static bool TryFindNoTargetAimClampHit(
            TowerDefendBattle battle,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float hitT,
            out Vector3 hitPoint)
        {
            hitT = float.MaxValue;
            hitPoint = segmentEnd;
            if (battle == null || !battle.TryReadNoTargetAimClampPointZ(out var clampPointZ))
            {
                return false;
            }

            float deltaZ = segmentEnd.z - segmentStart.z;
            if (Mathf.Abs(deltaZ) <= 0.0001f)
            {
                return false;
            }

            float t = (clampPointZ - segmentStart.z) / deltaZ;
            if (t <= 0.0f || t >= 1.0f)
            {
                return false;
            }

            // 无目标时仍然允许瞄准线沿当前方向继续延伸，但最远只画到当前战斗关注物所在的 Z 平面前方。
            // 这样可以把“场景远端的有效可视边界”固定在当前流程真正需要玩家关注的位置附近，避免没有命中体时被射程或飞行寿命拉到过远位置，
            // 让玩家误以为瞄准线突然消失，或看起来像是无限延伸到场景深处。
            hitT = t;
            hitPoint = Vector3.Lerp(segmentStart, segmentEnd, t);
            return true;
        }

        private bool TryFindMonsterHit(
            PropertyEntity attacker,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float hitT,
            out Vector3 hitPoint,
            out Vector3 impactPoint)
        {
            hitT = float.MaxValue;
            hitPoint = segmentEnd;
            impactPoint = segmentEnd;

            var objectManager = BattleManager.GetObjectManager();
            if (objectManager == null)
            {
                return false;
            }

            var hasHit = false;
            var nearestHitT = float.MaxValue;
            var nearestHitPoint = segmentEnd;
            var nearestImpactPoint = segmentEnd;
            const float hitRangeExtra = 0.2f;
            objectManager.ReadPropertyEntities((_, entity) =>
            {
                // 渲染资源还没创建好的实体（例如棒棒糖这类逻辑目标）没有画面形体、也没有可视位置：
                // 既不能参与视觉命中，也不能拿 GetPosition/GetMatrix4X4 做诊断，否则会空引用。
                var hasRender = entity.GetRender() != null;
                // 激光口径的候选与光束命中筛选（IsLaserSweepCandidate）保持一致（只看可受伤，不限小怪），
                // 否则比如 boss：光束会截断在它身上、瞄准线却穿过去，两者对不齐。
                var isCandidate = m_UseVisualOffsetTargets
                    ? entity.ReadCanBeHurt()
                    : (entity.ReadIsSmallMonster() || entity is MasterHero);

                if (!BattleManager.ReadIsEntityValide(entity) ||
                    entity == attacker ||
                    entity.ReadHurtGroup() == attacker.ReadHurtGroup() ||
                    !entity.ReadCanBeTarget() ||
                    entity.ReadIsInBorn() ||
                    !hasRender ||
                    !isCandidate)
                {
                    return true;
                }

                // 与子弹命中同口径：出生（从地面钻出）阶段的怪物既打不到也不应截断瞄准线
                // 激光口径：直线激光/瞄准线打的是 Curve World 完整弯折后的画面形体，
                // 直接对实体做“视觉口径”求交，命中点即画面上的视觉位置；
                // 不能用“逻辑求交 + 命中节点常量偏移”近似——单位移动、命中节点与碰撞体不同高/不同深时都会失准。
                bool hasSegmentHit;
                float currentHitT;
                Vector3 currentHitPoint;
                if (m_UseVisualOffsetTargets)
                {
                    hasSegmentHit = entity.TryIntersectVisualSegment(segmentStart, segmentEnd, hitRangeExtra, out currentHitT, out currentHitPoint);
                }
                else
                {
                    hasSegmentHit = entity.TryIntersectSegment(segmentStart, segmentEnd, hitRangeExtra, out currentHitT, out currentHitPoint);
                }
                if (!hasSegmentHit)
                {
                    return true;
                }

                if (currentHitT < nearestHitT)
                {
                    nearestHitT = currentHitT;
                    // 视觉求交返回的就是画面上的命中点，截线与落点特效同位：
                    // line_hit_eff 材质强制非弯折渲染（BendOff），必须放视觉坐标，画面位置才与截线点完全重合；
                    // 普通座位命中点即逻辑点，同位即正确。
                    nearestHitPoint = currentHitPoint;
                    nearestImpactPoint = currentHitPoint;
                    hasHit = true;
                }

                return true;
            });

            hitT = nearestHitT;
            hitPoint = nearestHitPoint;
            impactPoint = nearestImpactPoint;
            return hasHit;
        }

        private bool TryFindUpgradeChallengeHit(
            TowerDefendBattle battle,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float hitT,
            out Vector3 hitPoint,
            out Vector3 impactPoint)
        {
            hitT = float.MaxValue;
            hitPoint = segmentEnd;
            impactPoint = segmentEnd;

            if (battle == null ||
                (!battle.ReadIsUpgradeChallengePreActive() && !battle.ReadIsUpgradeChallengeActive()))
            {
                return false;
            }

            // 棒棒糖的自动吸附从 ChallengeEntryWait 就已经开始参与方向求解，
            // 因此瞄准线自己的命中判定也必须覆盖这段前置阶段。
            // 否则刚进入棒棒糖时会出现“方向已经朝棒棒糖修正，但瞄准线又不承认棒棒糖可命中”，
            // 最终容易提前落地，看起来像是瞄准线先消失、攻击一次后才恢复。
            var targetEntity = battle.ReadUpgradeChallengeTarget();
            if (targetEntity == null)
            {
                return false;
            }

            // 激光口径：与怪物命中共用同一套表现判定——对完整弯折后的棒棒糖形体求交，命中点即视觉点；
            // 普通座位仍是逻辑求交，命中点即逻辑点。
            Vector3 point;
            bool hasHit;
            if (m_UseVisualOffsetTargets)
            {
                hasHit = targetEntity.TryIntersectVisualSegment(segmentStart, segmentEnd, 0.0f, out hitT, out point);
            }
            else
            {
                hasHit = targetEntity.TryIntersectSegment(segmentStart, segmentEnd, 0.0f, out hitT, out point);
            }

            if (!hasHit)
            {
                hitT = float.MaxValue;
                return false;
            }

            // 落点特效 line_hit_eff 与截线点同位：激光座位特效材质强制非弯折渲染（BendOff），
            // 必须放视觉坐标；普通座位 hitPoint 即逻辑点，同位即正确。
            hitPoint = point;
            impactPoint = point;
            return true;
        }

        private bool TryFindNpcSpeakIntroHit(
            TowerDefendBattle battle,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float hitT,
            out Vector3 hitPoint)
        {
            hitT = float.MaxValue;
            hitPoint = segmentEnd;
            if (battle == null || !battle.TryReadNpcSpeakIntroAimEntity(out var entity))
            {
                return false;
            }

            // 激光口径：喊话实体同样按完整弯折后的画面形体判定，命中点即视觉点
            return m_UseVisualOffsetTargets
                ? entity.TryIntersectVisualSegment(segmentStart, segmentEnd, 0.2f, out hitT, out hitPoint)
                : entity.TryIntersectSegment(segmentStart, segmentEnd, 0.2f, out hitT, out hitPoint);
        }

        // hitPoint 用于截线：普通口径是逻辑地面交点；激光口径是“可见地面”交点（逻辑地面加该点视觉偏移，
        // 偏移只含 Y 且随距离增大，两次迭代即可收敛），否则光束会在到达远处被弯下去的目标前就被看不见的逻辑地面截断。
        // impactPoint 与截线点同位：激光座位是视觉地面交点（特效 BendOff），普通座位两者本来就相等。
        private static bool TryFindGroundHit(
            PropertyEntity attacker,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            bool useVisualOffset,
            out float hitT,
            out Vector3 hitPoint,
            out Vector3 impactPoint)
        {
            hitT = float.MaxValue;
            hitPoint = segmentEnd;
            impactPoint = segmentEnd;

            var groundY = attacker != null ? attacker.GetPosition().y : 0f;
            var delta = segmentEnd - segmentStart;
            if (Mathf.Abs(delta.y) <= 0.0001f)
            {
                return false;
            }

            var planeY = groundY;
            if (!useVisualOffset)
            {
                if ((segmentStart.y >= planeY && segmentEnd.y >= planeY) ||
                    (segmentStart.y <= planeY && segmentEnd.y <= planeY))
                {
                    return false;
                }
            }
            else
            {
                var point = segmentStart + delta * Mathf.Clamp01((planeY - segmentStart.y) / delta.y);
                for (int i = 0; i < 2; i++)
                {
                    planeY = groundY + CurveWorldVisualUtility.ReadVisualOffset(point).y;
                    if ((segmentStart.y - planeY) * (segmentEnd.y - planeY) >= 0.0f)
                    {
                        return false;
                    }

                    point = segmentStart + delta * Mathf.Clamp01((planeY - segmentStart.y) / delta.y);
                }
            }

            hitT = Mathf.Clamp01((planeY - segmentStart.y) / delta.y);
            hitPoint = segmentStart + delta * hitT;
            hitPoint.y = planeY + m_GroundOffset;
            // 落点特效与截线点同位：激光座位特效材质强制非弯折渲染（BendOff），
            // 必须放视觉地面交点（激光口径 planeY 含视觉偏移）；普通座位两者本来就相等。
            impactPoint = hitPoint;
            return true;
        }

        private void SetVisible(bool visible)
        {
            if (m_LineRoot != null && m_LineRoot.activeSelf != visible)
            {
                m_LineRoot.SetActive(visible);
            }
        }

        private void UpdateImpactEffect()
        {
            if (!m_HasImpactPoint)
            {
                HideImpactEffect();
                return;
            }

            if (m_ImpactEff == null)
            {
                m_ImpactEff = RenderEffManager.GetInstance().CreateRenderEff(m_ImpactEffectId);
                if (m_ImpactEff != null)
                {
                    m_ImpactEff.ShowEff(false, m_ImpactPoint, Vector3.zero, Vector3.one);
                    // 激光座位：特效加载完成后同样关闭 Curve World 弯折（直线渲染 + 视觉点坐标）
                    m_ImpactEff.AddLoadedCall(ApplyImpactEffectBendOff);
                }

                return;
            }

            ApplyImpactEffectBendOff();
            m_ImpactEff.SetPosition(m_ImpactPoint);
            if (!m_ImpactEff.IsActive())
            {
                m_ImpactEff.SetActive(true);
            }
        }

        // 落点特效 line_hit_eff 的材质开关统一由这里管理（特效池实例会被各座位复用）：
        // 激光座位（trajectory=3）与瞄准线/激光光束同为直线渲染，特效放在视觉点坐标，
        // 因此材质关闭 Curve World 弯折，直接渲染在世界坐标处；否则材质自身再弯折一次会把位置带偏。
        // 普通座位保持材质默认弯折（逻辑点位置），并清除池化实例可能残留的 BendOff。
        private void ApplyImpactEffectBendOff()
        {
            CurveWorldVisualUtility.SetBendOff(m_ImpactEff, m_UseVisualOffsetTargets);
        }

        private void HideImpactEffect()
        {
            if (m_ImpactEff == null)
            {
                return;
            }

            RenderEffManager.GetInstance().PoolRenderEff(m_ImpactEff);
            m_ImpactEff = null;
        }

        private void UnloadLineRequest()
        {
            if (m_LineRequest != null)
            {
                UIRes.UnloadPrefab(m_LineRequest);
                m_LineRequest = null;
            }

            m_LoadingStyleId = -1;
        }

        // 激光座位的瞄准线材质关闭 Curve World 弯曲，与直线激光同为直线渲染；
        // 其它座位保持弯曲，跟随弯折世界的表现
        private void ApplyBendOffState(bool bendOff)
        {
            if (m_LineRenderer == null || m_BendOffApplied == bendOff)
            {
                return;
            }

            m_BendOffApplied = bendOff;
            if (bendOff)
            {
                if (m_LinePropertyBlock == null)
                {
                    m_LinePropertyBlock = new MaterialPropertyBlock();
                }

                m_LineRenderer.GetPropertyBlock(m_LinePropertyBlock);
                m_LinePropertyBlock.SetFloat(CurveWorldVisualUtility.BendOffPropertyName, 1f);
                m_LineRenderer.SetPropertyBlock(m_LinePropertyBlock);
            }
            else
            {
                m_LineRenderer.SetPropertyBlock(null);
            }
        }

        private void DestroyLineInstance()
        {
            if (m_LineRoot != null)
            {
                GameObject.Destroy(m_LineRoot);
            }

            m_LineRoot = null;
            m_LineRenderer = null;
            m_LoadedStyleId = -1;
            m_BendOffApplied = false;
        }
    }
}
