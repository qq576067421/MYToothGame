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

            m_HasImpactPoint = false;
            forward.Normalize();

            var hasAutoAimSolution =
                battle.TryResolveGuardHeroNormalAutoAim(hero, skill, forward, out var autoAimSolution) &&
                autoAimSolution.m_HasSnapTarget;
            if (hasAutoAimSolution)
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
                UDebug.LogError(string.Format("瞄准线预制件缺少 LineRenderer：{0}", prefab.name));
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

            if (TryResolveBlock(battle, attacker, start, end, out var hitPoint))
            {
                m_AimPoints.Add(hitPoint);
                m_HasImpactPoint = true;
                m_ImpactPoint = hitPoint;
            }
            else
            {
                if (TryResolveNoTargetBoundary(battle, attacker, start, end, out var clampedEnd, out var isGroundHit))
                {
                    end = clampedEnd;
                    if (isGroundHit)
                    {
                        m_HasImpactPoint = true;
                        m_ImpactPoint = clampedEnd;
                    }
                }

                m_AimPoints.Add(end);
            }

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

                if (TryResolveBlock(battle, attacker, previous, pos, out var hitPoint))
                {
                    m_AimPoints.Add(hitPoint);
                    m_HasImpactPoint = true;
                    m_ImpactPoint = hitPoint;
                    break;
                }

                if (TryResolveNoTargetBoundary(battle, attacker, previous, pos, out var clampedEnd, out var isGroundHit))
                {
                    m_AimPoints.Add(clampedEnd);
                    if (isGroundHit)
                    {
                        m_HasImpactPoint = true;
                        m_ImpactPoint = clampedEnd;
                    }

                    break;
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

        private bool TryResolveBlock(TowerDefendBattle battle, PropertyEntity attacker, Vector3 segmentStart, Vector3 segmentEnd, out Vector3 hitPoint)
        {
            hitPoint = segmentEnd;

            if (!BattleManager.ReadIsEntityValide(attacker))
            {
                return false;
            }

            var hasHit = false;
            var nearestHitT = float.MaxValue;
            if (TryFindMonsterHit(attacker, segmentStart, segmentEnd, out var monsterHitT, out var monsterHitPoint))
            {
                nearestHitT = monsterHitT;
                hitPoint = monsterHitPoint;
                hasHit = true;
            }

            if (TryFindNpcSpeakIntroHit(battle, segmentStart, segmentEnd, out var npcHitT, out var npcHitPoint) &&
                (!hasHit || npcHitT < nearestHitT))
            {
                nearestHitT = npcHitT;
                hitPoint = npcHitPoint;
                hasHit = true;
            }

            if (TryFindUpgradeChallengeHit(battle, segmentStart, segmentEnd, out var upgradeHitT, out var upgradeHitPoint) &&
                (!hasHit || upgradeHitT < nearestHitT))
            {
                nearestHitT = upgradeHitT;
                hitPoint = upgradeHitPoint;
                hasHit = true;
            }

            if (hasHit &&
                TryFindGroundHit(attacker, segmentStart, segmentEnd, out var groundHitT, out var groundHitPoint) &&
                groundHitT < nearestHitT)
            {
                hitPoint = groundHitPoint;
            }

            return hasHit;
        }

        // 真实目标没有命中时，才允许用战斗关注物边界或地面来收束瞄准线。
        // 关注物边界不是攻击目标，因此不会显示命中特效；地面仍然保持旧的落点表现。
        private static bool TryResolveNoTargetBoundary(
            TowerDefendBattle battle,
            PropertyEntity attacker,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out Vector3 boundaryPoint,
            out bool isGroundHit)
        {
            boundaryPoint = segmentEnd;
            isGroundHit = false;

            var hasBoundary = false;
            var nearestHitT = float.MaxValue;
            if (TryFindNoTargetAimClampHit(battle, segmentStart, segmentEnd, out var clampHitT, out var clampPoint))
            {
                nearestHitT = clampHitT;
                boundaryPoint = clampPoint;
                hasBoundary = true;
            }

            if (TryFindGroundHit(attacker, segmentStart, segmentEnd, out var groundHitT, out var groundPoint) &&
                (!hasBoundary || groundHitT < nearestHitT))
            {
                boundaryPoint = groundPoint;
                isGroundHit = true;
                hasBoundary = true;
            }

            return hasBoundary;
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

        private static bool TryFindMonsterHit(
            PropertyEntity attacker,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float hitT,
            out Vector3 hitPoint)
        {
            hitT = float.MaxValue;
            hitPoint = segmentEnd;

            var objectManager = BattleManager.GetObjectManager();
            if (objectManager == null)
            {
                return false;
            }

            var hasHit = false;
            var nearestHitT = float.MaxValue;
            var nearestHitPoint = segmentEnd;
            const float hitRangeExtra = 0.2f;
            objectManager.ReadPropertyEntities((_, entity) =>
            {
                if (!BattleManager.ReadIsEntityValide(entity) ||
                    entity == attacker ||
                    entity.ReadHurtGroup() == attacker.ReadHurtGroup() ||
                    !entity.ReadCanBeTarget() ||
                    (!entity.ReadIsSmallMonster() && !(entity is MasterHero)))
                {
                    return true;
                }

                if (!entity.TryIntersectSegment(segmentStart, segmentEnd, hitRangeExtra, out var currentHitT, out var currentHitPoint))
                {
                    return true;
                }

                if (currentHitT < nearestHitT)
                {
                    nearestHitT = currentHitT;
                    nearestHitPoint = currentHitPoint;
                    hasHit = true;
                }

                return true;
            });

            hitT = nearestHitT;
            hitPoint = nearestHitPoint;
            return hasHit;
        }

        private static bool TryFindUpgradeChallengeHit(
            TowerDefendBattle battle,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float hitT,
            out Vector3 hitPoint)
        {
            hitT = float.MaxValue;
            hitPoint = segmentEnd;

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
            return targetEntity != null &&
                   targetEntity.TryIntersectSegment(segmentStart, segmentEnd, out hitT, out hitPoint);
        }

        private static bool TryFindNpcSpeakIntroHit(
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

            return entity.TryIntersectSegment(segmentStart, segmentEnd, 0.2f, out hitT, out hitPoint);
        }

        private static bool TryFindGroundHit(
            PropertyEntity attacker,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float hitT,
            out Vector3 hitPoint)
        {
            hitT = float.MaxValue;
            hitPoint = segmentEnd;

            var groundY = attacker != null ? attacker.GetPosition().y : 0f;
            if ((segmentStart.y >= groundY && segmentEnd.y >= groundY) ||
                (segmentStart.y <= groundY && segmentEnd.y <= groundY))
            {
                return false;
            }

            var delta = segmentEnd - segmentStart;
            if (Mathf.Abs(delta.y) <= 0.0001f)
            {
                return false;
            }

            hitT = Mathf.Clamp01((groundY - segmentStart.y) / delta.y);
            hitPoint = segmentStart + delta * hitT;
            hitPoint.y = groundY + m_GroundOffset;
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
                }

                return;
            }

            m_ImpactEff.SetPosition(m_ImpactPoint);
            if (!m_ImpactEff.IsActive())
            {
                m_ImpactEff.SetActive(true);
            }
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

        private void DestroyLineInstance()
        {
            if (m_LineRoot != null)
            {
                GameObject.Destroy(m_LineRoot);
            }

            m_LineRoot = null;
            m_LineRenderer = null;
            m_LoadedStyleId = -1;
        }
    }
}
