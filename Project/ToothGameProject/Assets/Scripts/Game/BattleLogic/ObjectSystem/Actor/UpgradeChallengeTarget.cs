using LCL;
using MonoBean;
using RayFire;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    public class UpgradeChallengeTarget : PropertyEntity
    {
        public const string PrefabPath = "prefab/effects/bangbangtang.jpg";
        public const string CenterNodeName = "center";
        public const float HitRadius = 4.0f;
        public const float DefaultCenterHeight = 2.5f;

        private const float m_VisualDropHeight = 6.0f;
        private const float m_VisualFloatAmplitude = 0.18f;
        private const float m_VisualFloatSpeed = 2.2f;
        private const int m_AutoAimMeshPitchSampleCount = 25;
        private const int m_AutoAimMeshPitchRefineSampleCount = 9;
        private const float m_AutoAimMeshPitchExpandDegrees = 2.0f;
        private const float m_PreviewTweenDuration = 1.0f;
        private const float m_TimeoutRetractDuration = 2.0f;
        private const float m_PreviewScaleEpsilon = 0.001f;
        private const int m_CrackAudioId = 100;
        private const int m_ShatterAudioId = 101;
        private const int m_HitEffectId = 80;
        private const float m_HitEffectDuration = 2.0f;

        private static readonly float[] m_ScoreDistanceThresholds = { 0.5f, 1.5f, 2.5f, 3.5f };
        private static readonly int[] m_ScoreValues = { 5, 4, 3, 2, 1 };

        private Vector3 m_FootPosition;
        private Vector3 m_CenterPosition;
        private bool m_HasCenterPosition;

        private Transform m_VisualParent;
        private GameObject m_VisualRoot;
        private Transform m_VisualCenterTransform;
        private MeshCollider m_VisualCenterMeshCollider;
        private ABRequest m_VisualRequest;
        private RayfireShatter m_Shatter;
        private readonly List<Transform> m_FragmentRoots = new List<Transform>();
        private readonly List<Rigidbody> m_FragmentRigidbodies = new List<Rigidbody>();
        private readonly List<Renderer> m_OriginalRenderers = new List<Renderer>();
        private readonly List<RenderEff> m_ChallengeHitEffects = new List<RenderEff>();
        private float m_CurrentPreviewScale;
        private float m_PreviewTweenFromScale;
        private float m_PreviewTweenToScale;
        private float m_PreviewTweenElapsed = m_PreviewTweenDuration;
        private bool m_IsShatterPrepared;
        private bool m_FinalShatterTriggered;
        private float m_FinalShatterDestroyAt;
        private bool m_IsRetractingAfterTimeout;
        private float m_RetractDestroyAt;
        private Vector3 m_RetractStartWorldPosition;
        private bool m_IsDestroyed;
        private int m_LastPreviewAudioStage;
        private int m_MaxHealth;
        private int m_CurrentHealth;
        private static int s_LastManualPhysicsSimulateFrame = -1;
        private static float s_ManualPhysicsSimulateAccumulator;

        public override void InitInstance()
        {
            base.InitInstance();
            ReleaseChallengeHitEffects();
            SetCanBeTarget(false);
            SetCanBeHurt(false);
            m_CurrentPreviewScale = 0f;
            m_PreviewTweenFromScale = 0f;
            m_PreviewTweenToScale = 0f;
            m_PreviewTweenElapsed = m_PreviewTweenDuration;
            m_FinalShatterTriggered = false;
            m_FinalShatterDestroyAt = -1f;
            m_IsRetractingAfterTimeout = false;
            m_RetractDestroyAt = -1f;
            m_RetractStartWorldPosition = Vector3.zero;
            m_IsDestroyed = false;
            m_LastPreviewAudioStage = 0;
            m_MaxHealth = 1;
            m_CurrentHealth = 1;
            m_FragmentRoots.Clear();
            m_FragmentRigidbodies.Clear();
            m_OriginalRenderers.Clear();
        }

        public override void Destroy()
        {
            m_IsDestroyed = true;
            ReleaseChallengeHitEffects();
            ReleaseVisual();
            base.Destroy();
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            UpdateChallengeHitEffects();
            UpdateVisual(dt);
            UpdateFinalShatterPhysics(dt);
        }

        public override bool ReadIsHero()
        {
            return false;
        }

        public override bool ReadIsSmallMonster()
        {
            return false;
        }

        public override bool ReadIsBoss()
        {
            return false;
        }

        public override bool ReadIsMoveableCreature()
        {
            return false;
        }

        public override float ReadRadius()
        {
            return HitRadius;
        }

        public override Vector3 ReadHitPoint()
        {
            return ReadCenterPosition();
        }

        public override void OnHpChanged()
        {
        }

        public void SetFootPosition(Vector3 footPos)
        {
            m_FootPosition = footPos;
            SetPosition(footPos);
        }

        public void SetupPlacement(Vector3 footPos)
        {
            SetFootPosition(footPos);
            SetCenterPosition(footPos + Vector3.up * DefaultCenterHeight);
        }

        public Vector3 GetFootPosition()
        {
            return m_FootPosition;
        }

        public void SetCenterPosition(Vector3 centerPos)
        {
            m_CenterPosition = centerPos;
            m_HasCenterPosition = true;
        }

        public Vector3 ReadCenterPosition()
        {
            return m_HasCenterPosition ? m_CenterPosition : m_FootPosition + Vector3.up * DefaultCenterHeight;
        }

        // 升级挑战的自动吸附语义固定为：
        // 1. 命中的前向距离来自 center 节点上的 MeshCollider；
        // 2. 俯仰目标高度来自 center 节点本身的世界坐标；
        // 3. 两者都允许按预计飞行时间向前预测，避免棒棒糖继续运动后，发射前解算仍停留在旧位置。
        public bool TryResolveAutoAimTargetOnCurrentVerticalPlane(
            Vector3 aimOrigin,
            Vector3 horizontalForward,
            float castDistance,
            float predictionTime,
            out Vector3 centerPosition,
            out float alongRayDistance)
        {
            centerPosition = ReadPredictedCenterAnchorWorldPosition(predictionTime);
            alongRayDistance = 0.0f;
            if (!TryNormalizeHorizontal(horizontalForward, out var normalizedHorizontalForward))
            {
                return false;
            }

            return TryResolveVisualMeshAutoAimHitPoint(
                aimOrigin,
                normalizedHorizontalForward,
                Mathf.Max(0.0f, castDistance),
                centerPosition - ReadCenterPosition(),
                out _,
                out alongRayDistance);
        }

        public void SetVisualParent(Transform visualParent)
        {
            if (m_VisualParent == visualParent)
            {
                return;
            }

            m_VisualParent = visualParent;
            if (m_VisualRoot != null && m_VisualParent != null)
            {
                m_VisualRoot.transform.SetParent(m_VisualParent, false);
            }
        }

        public void SetChallengeHealth(int maxHealth)
        {
            m_MaxHealth = Mathf.Max(1, maxHealth);
            m_CurrentHealth = m_MaxHealth;
            m_LastPreviewAudioStage = 0;
        }

        public int ReadMaxHealth()
        {
            return m_MaxHealth;
        }

        public int ReadCurrentHealth()
        {
            return m_CurrentHealth;
        }

        public bool ReadIsDefeated()
        {
            return m_CurrentHealth <= 0;
        }

        public bool ReadIsFinalShatterTriggered()
        {
            return m_FinalShatterTriggered;
        }

        public bool ApplyChallengeDamage(int damage)
        {
            if (m_FinalShatterTriggered || damage <= 0)
            {
                return ReadIsDefeated();
            }

            m_CurrentHealth = Mathf.Max(0, m_CurrentHealth - damage);
            RenderEvent.Event.OnLollipopHealthChanged(m_MaxHealth,m_CurrentHealth,true);
            return ReadIsDefeated();
        }

        public void NotifyChallengeHit(Vector3 hitPoint)
        {
            if (m_FinalShatterTriggered)
            {
                return;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null || !battle.ReadIsUpgradeChallengeActive())
            {
                return;
            }

            PlayChallengeHitEffect(hitPoint);
            if (TryResolvePreviewStageByHealth(out var previewStage, out var targetPreviewScale))
            {
                if (previewStage > m_LastPreviewAudioStage)
                {
                    m_LastPreviewAudioStage = previewStage;
                    AudioManager.GetInstance().Play2D(m_CrackAudioId);
                }

                BeginPreviewTween(targetPreviewScale);
            }

            if (ReadIsDefeated())
            {
                TryTriggerFinalShatter();
            }
        }

        private void PlayChallengeHitEffect(Vector3 hitPoint)
        {
            if (m_HitEffectId == 0)
            {
                return;
            }

            var effect = RenderEffManager.GetInstance().CreateRenderEff(m_HitEffectId);
            if (effect == null)
            {
                return;
            }

            var effectParent = m_VisualRoot != null ? m_VisualRoot.transform : m_VisualParent;
            if (effectParent != null)
            {
                effect.SetParent(effectParent);
            }

            effect.ShowEff(false, hitPoint, Vector3.zero, Vector3.one);
            effect.SetDuringTime(m_HitEffectDuration);
            m_ChallengeHitEffects.Add(effect);
        }

        public bool TryTriggerChallengeTimeoutShatter()
        {
            return TryTriggerFinalShatter();
        }

        public bool ReadCanDestroyAfterFinalShatter()
        {
            if (m_FinalShatterTriggered)
            {
                return BattleManager.ReadBattleTime() >= m_FinalShatterDestroyAt;
            }

            if (m_IsRetractingAfterTimeout)
            {
                return BattleManager.ReadBattleTime() >= m_RetractDestroyAt;
            }

            return false;
        }

        public bool TryStartTimeoutRetract()
        {
            if (m_FinalShatterTriggered || m_IsRetractingAfterTimeout)
            {
                return false;
            }
            RenderEvent.Event.OnLollipopHealthChanged(m_MaxHealth , m_CurrentHealth,false);
            m_IsRetractingAfterTimeout = true;
            m_RetractDestroyAt = BattleManager.ReadBattleTime() + m_TimeoutRetractDuration;
            m_RetractStartWorldPosition = m_VisualRoot != null
                ? m_VisualRoot.transform.position
                : ReadActiveVisualRootWorldPosition(BattleManager.ReadBattleTime());
            SetCanBeTarget(false);
            SetCanBeHurt(false);
            return true;
        }

        // 这里只负责把真实命中点换算成命中环值。
        // 最终显示分值和实际扣血都在战斗层统一结算，避免这里再次混入业务伤害概念。
        public bool TryResolveHitRingValue(Vector3 hitPoint, out int ringValue)
        {
            ringValue = 0;
            if (m_FinalShatterTriggered)
            {
                return false;
            }

            var targetPoint = ReadCenterPosition();
            float offsetX = hitPoint.x - targetPoint.x;
            float offsetY = hitPoint.y - targetPoint.y;
            float distFromCenter = Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY);
            ringValue = ResolveScoreByDistance(distFromCenter);
            return true;
        }

        public static int ReadScoreBoundaryCount()
        {
            return m_ScoreDistanceThresholds.Length;
        }

        public static float ReadScoreBoundaryRadius(int index)
        {
            if (index < 0 || index >= m_ScoreDistanceThresholds.Length)
            {
                return 0f;
            }

            return m_ScoreDistanceThresholds[index];
        }

        public bool TryIntersectSegment(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float hitT,
            out Vector3 hitPoint)
        {
            return TryIntersectSegment(segmentStart, segmentEnd, 0.0f, out hitT, out hitPoint);
        }

        public override bool TryIntersectSegment(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            float extraRadius,
            out float hitT,
            out Vector3 hitPoint)
        {
            extraRadius = Mathf.Max(0.0f, extraRadius);
            if (TryIntersectVisualMesh(segmentStart, segmentEnd, out hitT, out hitPoint))
            {
                return true;
            }

            return false;
        }

        private void UpdateVisual(float dt)
        {
            if (m_IsDestroyed || m_FinalShatterTriggered)
            {
                return;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                SetVisualVisible(false);
                return;
            }

            if ((battle.ReadIsUpgradeChallengeCountdown() || battle.ReadIsUpgradeChallengeActive()) && m_VisualParent != null)
            {
                EnsureVisualLoaded();
            }

            if (m_VisualRoot == null)
            {
                return;
            }

            var phase = battle.ReadPhase();
            bool shouldShow = phase == BattlePhase.ChallengeCountdown ||
                phase == BattlePhase.ChallengeActive ||
                (phase == BattlePhase.ChallengeFinish && m_IsRetractingAfterTimeout);
            if (!shouldShow)
            {
                SetVisualVisible(false);
                return;
            }

            Vector3 displayPoint = ReadPredictedVisualRootWorldPosition(battle, 0.0f);
            m_VisualRoot.transform.localScale = Vector3.one;
            m_VisualRoot.transform.position = displayPoint;
            m_VisualRoot.transform.localRotation = Quaternion.identity;
            SetCenterPosition(ReadVisualCenterAnchorWorldPosition());
            UpdatePreviewTween(dt);
            SetVisualVisible(true);
        }

        private void UpdatePreviewTween(float dt)
        {
            if (Mathf.Abs(m_CurrentPreviewScale - m_PreviewTweenToScale) > m_PreviewScaleEpsilon)
            {
                m_PreviewTweenElapsed += dt;
                float lerp = m_PreviewTweenDuration > 0f
                    ? Mathf.Clamp01(m_PreviewTweenElapsed / m_PreviewTweenDuration)
                    : 1f;
                m_CurrentPreviewScale = Mathf.Lerp(m_PreviewTweenFromScale, m_PreviewTweenToScale, lerp);
            }
            else
            {
                m_CurrentPreviewScale = m_PreviewTweenToScale;
            }

            ApplyPreviewScale(m_CurrentPreviewScale);
        }

        private void BeginPreviewTween(float targetPreviewScale)
        {
            targetPreviewScale = Mathf.Clamp(targetPreviewScale, 0f, ReadFullCrackPreviewScale());
            if (Mathf.Abs(targetPreviewScale - m_PreviewTweenToScale) <= m_PreviewScaleEpsilon &&
                Mathf.Abs(targetPreviewScale - m_CurrentPreviewScale) <= m_PreviewScaleEpsilon)
            {
                return;
            }

            m_PreviewTweenFromScale = m_CurrentPreviewScale;
            m_PreviewTweenToScale = targetPreviewScale;
            m_PreviewTweenElapsed = 0f;
        }

        private void EnsureVisualLoaded()
        {
            if (m_VisualRoot != null || m_VisualRequest != null || m_VisualParent == null)
            {
                return;
            }

            m_VisualRequest = UIRes.LoadPrefabAsync(
                typeof(GameObject),
                PrefabPath,
                Tool.GetAssetName(PrefabPath),
                OnVisualLoaded);
        }

        private void OnVisualLoaded(ResData resData, object userData)
        {
            m_VisualRequest = null;
            if (m_IsDestroyed || resData == null || resData.m_Obj == null || m_VisualParent == null)
            {
                return;
            }

            var prefab = resData.m_Obj as GameObject;
            if (prefab == null)
            {
                return;
            }

            m_VisualRoot = GameObject.Instantiate(prefab);
            if (m_VisualRoot == null)
            {
                return;
            }

            m_VisualRoot.name = "tower_defend_upgrade_target";
            m_VisualRoot.transform.SetParent(m_VisualParent, false);
            m_VisualCenterTransform = FindChildRecursive(m_VisualRoot.transform, CenterNodeName);
            if (m_VisualCenterTransform == null)
            {
                m_VisualCenterTransform = m_VisualRoot.transform;
            }
            m_VisualCenterMeshCollider = m_VisualCenterTransform != null ? m_VisualCenterTransform.GetComponent<MeshCollider>() : null;

            m_Shatter = m_VisualRoot.GetComponentInChildren<RayfireShatter>(true);
            RebuildFragmentRootCache();
            RebuildFragmentRigidbodyCache();
            RebuildOriginalRendererCache();
            m_IsShatterPrepared = m_FragmentRoots.Count > 0;
            ResetFragmentPhysics();
            SetFragmentScale(Vector3.one);
            SetCenterPosition(ReadVisualCenterAnchorWorldPosition());
            ApplyPreviewScale(m_CurrentPreviewScale);
            SetVisualVisible(false);
        }

        private void ReleaseVisual()
        {
            if (m_VisualRequest != null)
            {
                UIRes.UnloadPrefab(m_VisualRequest);
                m_VisualRequest = null;
            }

            int fragmentRootCount = m_FragmentRoots.Count;
            for (int i = 0; i < fragmentRootCount; i++)
            {
                var fragmentRoot = m_FragmentRoots[i];
                if (fragmentRoot != null)
                {
                    GameObject.Destroy(fragmentRoot.gameObject);
                }
            }

            m_FragmentRoots.Clear();
            m_FragmentRigidbodies.Clear();
            m_OriginalRenderers.Clear();
            m_Shatter = null;
            m_IsShatterPrepared = false;
            if (m_VisualRoot != null)
            {
                GameObject.Destroy(m_VisualRoot);
                m_VisualRoot = null;
            }

            m_VisualCenterTransform = null;
            m_VisualCenterMeshCollider = null;
        }

        private void UpdateChallengeHitEffects()
        {
            for (int i = m_ChallengeHitEffects.Count - 1; i >= 0; i--)
            {
                var effect = m_ChallengeHitEffects[i];
                if (effect == null || effect.m_Destroy)
                {
                    m_ChallengeHitEffects.RemoveAt(i);
                    continue;
                }

                if (!ReferenceEquals(effect.m_Eff, null) && effect.m_Eff.Equals(null))
                {
                    effect.Destroy();
                    m_ChallengeHitEffects.RemoveAt(i);
                    continue;
                }

                if (effect.m_StartTime > 0f &&
                    effect.m_DuringTime > 0f &&
                    Time.realtimeSinceStartup - effect.m_StartTime > effect.m_DuringTime)
                {
                    effect.Destroy();
                    m_ChallengeHitEffects.RemoveAt(i);
                }
            }
        }

        private void ReleaseChallengeHitEffects()
        {
            for (int i = m_ChallengeHitEffects.Count - 1; i >= 0; i--)
            {
                var effect = m_ChallengeHitEffects[i];
                if (effect != null)
                {
                    effect.Destroy();
                }
            }

            m_ChallengeHitEffects.Clear();
        }

        private void ApplyPreviewScale(float previewScale)
        {
            if (m_Shatter == null)
            {
                return;
            }

            previewScale = Mathf.Clamp(previewScale, 0f, ReadFullCrackPreviewScale());
            if (previewScale <= m_PreviewScaleEpsilon)
            {
                if (EnsurePreviewFragments())
                {
                    ResetFragmentPhysics();
                    SetFragmentScale(Vector3.one);
                    SetFragmentRootsVisible(true);
                }

                // bangbangtang_root 保持原有碎片显示链路，bangbang 在最终炸开前都保持可见。
                SetOriginalVisualVisible(true);
                return;
            }

            if (!EnsurePreviewFragments())
            {
                return;
            }

            ResetFragmentPhysics();
            SetFragmentRootsVisible(true);
            SetFragmentScale(Vector3.one * Mathf.Lerp(1f, 0.3f, previewScale));
            SetOriginalVisualVisible(true);
        }

        private bool EnsurePreviewFragments()
        {
            if (m_Shatter == null)
            {
                return false;
            }

            if (!m_IsShatterPrepared)
            {
                RebuildFragmentRootCache();
                RebuildFragmentRigidbodyCache();
                RebuildOriginalRendererCache();
                m_IsShatterPrepared = m_FragmentRoots.Count > 0;
            }

            if (!m_IsShatterPrepared)
            {
                return false;
            }

            RebuildFragmentRootCache();
            AttachFragmentRootsToVisualRoot();
            return m_FragmentRoots.Count > 0;
        }

        private void RebuildFragmentRootCache()
        {
            m_FragmentRoots.Clear();
            if (m_Shatter != null && m_Shatter.rootChildList != null)
            {
                int rootCount = m_Shatter.rootChildList.Count;
                for (int i = 0; i < rootCount; i++)
                {
                    TryAddFragmentRoot(m_Shatter.rootChildList[i]);
                }
            }

            if (m_FragmentRoots.Count > 0 || m_Shatter == null || m_Shatter.fragmentsLast == null)
            {
                return;
            }

            int fragmentCount = m_Shatter.fragmentsLast.Count;
            for (int i = 0; i < fragmentCount; i++)
            {
                var fragment = m_Shatter.fragmentsLast[i];
                if (fragment != null)
                {
                    TryAddFragmentRoot(fragment.transform.parent);
                }
            }
        }

        private void TryAddFragmentRoot(Transform root)
        {
            if (root == null || m_FragmentRoots.Contains(root))
            {
                return;
            }

            m_FragmentRoots.Add(root);
        }

        private void RebuildFragmentRigidbodyCache()
        {
            m_FragmentRigidbodies.Clear();

            int rootCount = m_FragmentRoots.Count;
            for (int i = 0; i < rootCount; i++)
            {
                var fragmentRoot = m_FragmentRoots[i];
                if (fragmentRoot == null)
                {
                    continue;
                }

                var rigidbodies = fragmentRoot.GetComponentsInChildren<Rigidbody>(true);
                int rigidbodyCount = rigidbodies.Length;
                for (int j = 0; j < rigidbodyCount; j++)
                {
                    var rigidbody = rigidbodies[j];
                    if (rigidbody != null && !m_FragmentRigidbodies.Contains(rigidbody))
                    {
                        m_FragmentRigidbodies.Add(rigidbody);
                    }
                }
            }
        }

        private void RebuildOriginalRendererCache()
        {
            m_OriginalRenderers.Clear();
            if (m_VisualRoot == null)
            {
                return;
            }

            var renderers = m_VisualRoot.GetComponentsInChildren<Renderer>(true);
            int rendererCount = renderers.Length;
            for (int i = 0; i < rendererCount; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || IsUnderFragmentRoot(renderer.transform))
                {
                    continue;
                }

                m_OriginalRenderers.Add(renderer);
            }
        }

        private bool IsUnderFragmentRoot(Transform target)
        {
            int rootCount = m_FragmentRoots.Count;
            for (int i = 0; i < rootCount; i++)
            {
                var fragmentRoot = m_FragmentRoots[i];
                if (fragmentRoot != null &&
                    (target == fragmentRoot || target.IsChildOf(fragmentRoot)))
                {
                    return true;
                }
            }

            return false;
        }

        private void AttachFragmentRootsToVisualRoot()
        {
            if (m_VisualRoot == null)
            {
                return;
            }

            int rootCount = m_FragmentRoots.Count;
            for (int i = 0; i < rootCount; i++)
            {
                var fragmentRoot = m_FragmentRoots[i];
                if (fragmentRoot != null && fragmentRoot.parent != m_VisualRoot.transform)
                {
                    fragmentRoot.SetParent(m_VisualRoot.transform, true);
                }
            }
        }

        private void ResetFragmentPhysics()
        {
            int rigidbodyCount = m_FragmentRigidbodies.Count;
            for (int i = 0; i < rigidbodyCount; i++)
            {
                var rigidbody = m_FragmentRigidbodies[i];
                if (rigidbody == null)
                {
                    continue;
                }

                if (!rigidbody.isKinematic)
                {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }

                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }
        }

        private void TriggerFinalFragmentExplosion()
        {
            BattleConst.ClampUpgradeChallengeShatterValues();
            Vector3 explosionCenter = ReadCenterPosition();
            float explosionForce = BattleConst.UpgradeChallengeShatterExplosionForce;
            float explosionRadius = BattleConst.UpgradeChallengeShatterExplosionRadius;
            float upwardsModifier = BattleConst.UpgradeChallengeShatterExplosionUpwardsModifier;
            float explosionTorque = BattleConst.UpgradeChallengeShatterExplosionTorque;
            int rigidbodyCount = m_FragmentRigidbodies.Count;
            for (int i = 0; i < rigidbodyCount; i++)
            {
                var rigidbody = m_FragmentRigidbodies[i];
                if (rigidbody == null)
                {
                    continue;
                }

                rigidbody.isKinematic = false;
                rigidbody.useGravity = true;
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.AddExplosionForce(
                    explosionForce,
                    explosionCenter,
                    explosionRadius,
                    upwardsModifier,
                    ForceMode.Impulse);
                ApplyFinalFragmentVelocity(rigidbody, explosionCenter, explosionForce, upwardsModifier);
                rigidbody.AddTorque(
                    UnityEngine.Random.onUnitSphere * explosionTorque,
                    ForceMode.Impulse);
            }
        }

        // 额外补一段初速度，避免碎片只出现轻微抖动，看起来不像真正炸开。
        private static void ApplyFinalFragmentVelocity(
            Rigidbody rigidbody,
            Vector3 explosionCenter,
            float explosionForce,
            float upwardsModifier)
        {
            Vector3 explosionOffset = rigidbody.worldCenterOfMass - explosionCenter;
            Vector3 outwardDirection = explosionOffset.sqrMagnitude > 0.0001f
                ? explosionOffset.normalized
                : UnityEngine.Random.onUnitSphere.normalized;
            rigidbody.AddForce(
                outwardDirection * (explosionForce * 0.35f) +
                Vector3.up * upwardsModifier,
                ForceMode.VelocityChange);
        }

        // 全局自动物理保持关闭，只在棒棒糖炸开阶段按固定步长手动推进物理。
        private void UpdateFinalShatterPhysics(float dt)
        {
            if (!m_FinalShatterTriggered || m_IsDestroyed || Physics.autoSimulation || dt <= 0f)
            {
                return;
            }

            int currentFrame = Time.frameCount;
            if (s_LastManualPhysicsSimulateFrame == currentFrame)
            {
                return;
            }

            s_LastManualPhysicsSimulateFrame = currentFrame;

            float step = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
            float maxDelta = Time.maximumDeltaTime > 0f ? Time.maximumDeltaTime : step;
            int maxStepCount = Mathf.Max(1, Mathf.CeilToInt(maxDelta / step));
            s_ManualPhysicsSimulateAccumulator = Mathf.Min(
                s_ManualPhysicsSimulateAccumulator + dt,
                maxDelta);

            int simulatedStepCount = 0;
            while (s_ManualPhysicsSimulateAccumulator >= step && simulatedStepCount < maxStepCount)
            {
                Physics.Simulate(step);
                s_ManualPhysicsSimulateAccumulator -= step;
                simulatedStepCount++;
            }
        }

        private static void ResetManualShatterPhysicsState()
        {
            s_LastManualPhysicsSimulateFrame = -1;
            s_ManualPhysicsSimulateAccumulator = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
        }

        private void SetFragmentRootsVisible(bool visible)
        {
            int rootCount = m_FragmentRoots.Count;
            for (int i = 0; i < rootCount; i++)
            {
                var fragmentRoot = m_FragmentRoots[i];
                if (fragmentRoot != null && fragmentRoot.gameObject.activeSelf != visible)
                {
                    fragmentRoot.gameObject.SetActive(visible);
                }
            }
        }

        private void SetFragmentScale(Vector3 scale)
        {
            if (m_Shatter == null || m_Shatter.fragmentsLast == null)
            {
                return;
            }

            int fragmentCount = m_Shatter.fragmentsLast.Count;
            for (int i = 0; i < fragmentCount; i++)
            {
                var fragment = m_Shatter.fragmentsLast[i];
                if (fragment != null)
                {
                    fragment.transform.localScale = scale;
                }
            }
        }

        private void SetOriginalVisualVisible(bool visible)
        {
            if (m_OriginalRenderers.Count > 0)
            {
                int rendererCount = m_OriginalRenderers.Count;
                for (int i = 0; i < rendererCount; i++)
                {
                    var renderer = m_OriginalRenderers[i];
                    if (renderer != null && renderer.enabled != visible)
                    {
                        renderer.enabled = visible;
                    }
                }

                return;
            }

            if (m_Shatter == null)
            {
                return;
            }

            if (m_Shatter.meshRenderer != null)
            {
                m_Shatter.meshRenderer.enabled = visible;
            }

            if (m_Shatter.skinnedMeshRend != null)
            {
                m_Shatter.skinnedMeshRend.enabled = visible;
            }
        }

        private void SetVisualVisible(bool visible)
        {
            if (m_VisualRoot != null && m_VisualRoot.activeSelf != visible)
            {
                m_VisualRoot.SetActive(visible);
            }
        }

        private Vector3 ReadVisualCenterAnchorWorldPosition()
        {
            if (m_VisualCenterTransform != null)
            {
                return m_VisualCenterTransform.position;
            }

            if (m_VisualRoot != null)
            {
                return m_VisualRoot.transform.position + Vector3.up * DefaultCenterHeight;
            }

            return m_FootPosition + Vector3.up * DefaultCenterHeight;
        }

        private bool TryIntersectVisualMesh(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            out float hitT,
            out Vector3 hitPoint)
        {
            hitT = 0.0f;
            hitPoint = segmentEnd;

            if (m_VisualCenterMeshCollider == null || !m_VisualCenterMeshCollider.enabled)
            {
                return false;
            }

            var delta = segmentEnd - segmentStart;
            float segmentLength = delta.magnitude;
            if (segmentLength <= 0.0001f)
            {
                return false;
            }

            // 从碰撞体内部开始时，MeshCollider.Raycast 不一定会给出命中，
            // 这里先把“起点已在碰撞体内”的情况当成立即命中，避免实时命中链路出现漏判。
            var closestPoint = m_VisualCenterMeshCollider.ClosestPoint(segmentStart);
            if ((closestPoint - segmentStart).sqrMagnitude <= 0.000001f &&
                m_VisualCenterMeshCollider.bounds.Contains(segmentStart))
            {
                hitPoint = segmentStart;
                return true;
            }

            var ray = new Ray(segmentStart, delta / segmentLength);
            if (!m_VisualCenterMeshCollider.Raycast(ray, out var hit, segmentLength))
            {
                return false;
            }

            hitT = Mathf.Clamp01(hit.distance / segmentLength);
            hitPoint = hit.point;
            return true;
        }

        private bool TryResolveVisualMeshAutoAimHitPoint(
            Vector3 aimOrigin,
            Vector3 normalizedHorizontalForward,
            float castDistance,
            Vector3 meshTranslation,
            out Vector3 hitPoint,
            out float alongRayDistance)
        {
            hitPoint = Vector3.zero;
            alongRayDistance = 0.0f;
            if (m_VisualCenterMeshCollider == null || !m_VisualCenterMeshCollider.enabled)
            {
                return false;
            }

            var bounds = m_VisualCenterMeshCollider.bounds;
            if (bounds.size.sqrMagnitude <= 0.0001f)
            {
                return false;
            }
            bounds.center += meshTranslation;

            if (!TryBuildAutoAimPitchRange(
                    aimOrigin,
                    normalizedHorizontalForward,
                    bounds,
                    out var minPitchDegrees,
                    out var maxPitchDegrees))
            {
                return false;
            }

            minPitchDegrees -= m_AutoAimMeshPitchExpandDegrees;
            maxPitchDegrees += m_AutoAimMeshPitchExpandDegrees;
            float maxRayDistance = castDistance + bounds.size.magnitude + 1.0f;
            if (!TrySampleAutoAimMeshPitchRange(
                    aimOrigin,
                    normalizedHorizontalForward,
                    castDistance,
                    maxRayDistance,
                    meshTranslation,
                    minPitchDegrees,
                    maxPitchDegrees,
                    m_AutoAimMeshPitchSampleCount,
                    out hitPoint,
                    out alongRayDistance,
                    out var bestPitchDegrees,
                    out var bestPitchStepDegrees))
            {
                return false;
            }

            if (bestPitchStepDegrees > 0.0001f)
            {
                TrySampleAutoAimMeshPitchRange(
                    aimOrigin,
                    normalizedHorizontalForward,
                    castDistance,
                    maxRayDistance,
                    meshTranslation,
                    bestPitchDegrees - bestPitchStepDegrees,
                    bestPitchDegrees + bestPitchStepDegrees,
                    m_AutoAimMeshPitchRefineSampleCount,
                    out hitPoint,
                    out alongRayDistance,
                    out _,
                    out _);
            }

            return true;
        }

        private bool TrySampleAutoAimMeshPitchRange(
            Vector3 aimOrigin,
            Vector3 normalizedHorizontalForward,
            float castDistance,
            float maxRayDistance,
            Vector3 meshTranslation,
            float minPitchDegrees,
            float maxPitchDegrees,
            int sampleCount,
            out Vector3 hitPoint,
            out float alongRayDistance,
            out float bestPitchDegrees,
            out float pitchStepDegrees)
        {
            hitPoint = Vector3.zero;
            alongRayDistance = 0.0f;
            bestPitchDegrees = 0.0f;
            pitchStepDegrees = 0.0f;
            if (m_VisualCenterMeshCollider == null || !m_VisualCenterMeshCollider.enabled || sampleCount <= 0)
            {
                return false;
            }

            var translatedAimOrigin = aimOrigin - meshTranslation;

            minPitchDegrees = Mathf.Clamp(minPitchDegrees, -89.0f, 89.0f);
            maxPitchDegrees = Mathf.Clamp(maxPitchDegrees, -89.0f, 89.0f);
            if (maxPitchDegrees < minPitchDegrees)
            {
                var temp = minPitchDegrees;
                minPitchDegrees = maxPitchDegrees;
                maxPitchDegrees = temp;
            }

            pitchStepDegrees = sampleCount > 1
                ? (maxPitchDegrees - minPitchDegrees) / (sampleCount - 1)
                : 0.0f;

            bool hasHit = false;
            float bestAlongRayDistance = float.MaxValue;
            float bestRayDistance = float.MaxValue;
            for (int i = 0; i < sampleCount; i++)
            {
                float pitchDegrees = sampleCount > 1
                    ? minPitchDegrees + pitchStepDegrees * i
                    : (minPitchDegrees + maxPitchDegrees) * 0.5f;
                var rayDirection = BuildDirectionFromHorizontalPitch(normalizedHorizontalForward, pitchDegrees);
                if (!m_VisualCenterMeshCollider.Raycast(new Ray(translatedAimOrigin, rayDirection), out var hit, maxRayDistance))
                {
                    continue;
                }

                var translatedHitPoint = hit.point + meshTranslation;
                var horizontalDelta = translatedHitPoint - aimOrigin;
                horizontalDelta.y = 0.0f;
                float currentAlongRayDistance = Vector3.Dot(horizontalDelta, normalizedHorizontalForward);
                if (currentAlongRayDistance <= 0.0001f || currentAlongRayDistance > castDistance + 0.01f)
                {
                    continue;
                }

                if (!hasHit ||
                    currentAlongRayDistance < bestAlongRayDistance - 0.0001f ||
                    (Mathf.Abs(currentAlongRayDistance - bestAlongRayDistance) <= 0.0001f &&
                     hit.distance < bestRayDistance - 0.0001f))
                {
                    hasHit = true;
                    bestAlongRayDistance = currentAlongRayDistance;
                    bestRayDistance = hit.distance;
                    bestPitchDegrees = pitchDegrees;
                    hitPoint = translatedHitPoint;
                    alongRayDistance = currentAlongRayDistance;
                }
            }

            return hasHit;
        }

        private Vector3 ReadPredictedCenterAnchorWorldPosition(float predictionTime)
        {
            if (predictionTime <= 0.0001f)
            {
                return ReadCenterPosition();
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return ReadCenterPosition();
            }

            var predictedRootPosition = ReadPredictedVisualRootWorldPosition(battle, predictionTime);
            return predictedRootPosition + ReadVisualCenterLocalOffsetFromRoot();
        }

        private Vector3 ReadPredictedVisualRootWorldPosition(TowerDefendBattle battle, float predictionTime)
        {
            if (battle == null)
            {
                return m_VisualRoot != null ? m_VisualRoot.transform.position : GetFootPosition();
            }

            predictionTime = Mathf.Max(0.0f, predictionTime);
            var phase = battle.ReadPhase();
            if (phase == BattlePhase.ChallengeCountdown)
            {
                float left = battle.ReadUpgradeChallengeCountdownLeft();
                if (predictionTime < left - 0.0001f)
                {
                    float elapsed = Mathf.Max(0.0f, battle.ReadUpgradeChallengeCountdownDuration() - (left - predictionTime));
                    return ReadCountdownVisualRootWorldPosition(elapsed);
                }

                return ReadActiveVisualRootWorldPosition(BattleManager.ReadBattleTime() + predictionTime);
            }

            if (phase == BattlePhase.ChallengeActive)
            {
                return ReadActiveVisualRootWorldPosition(BattleManager.ReadBattleTime() + predictionTime);
            }

            if (phase == BattlePhase.ChallengeFinish && m_IsRetractingAfterTimeout)
            {
                return ReadTimeoutRetractVisualRootWorldPosition(BattleManager.ReadBattleTime() + predictionTime);
            }

            return m_VisualRoot != null ? m_VisualRoot.transform.position : GetFootPosition();
        }

        private Vector3 ReadCountdownVisualRootWorldPosition(float elapsed)
        {
            Vector3 displayPoint = GetFootPosition();
            float dropOffset = Mathf.Max(0f, m_VisualDropHeight - Mathf.Max(0f, elapsed) * ReadUpgradeChallengeDropSpeed());
            displayPoint += Vector3.up * dropOffset;
            return displayPoint;
        }

        private Vector3 ReadActiveVisualRootWorldPosition(float battleTime)
        {
            Vector3 displayPoint = GetFootPosition();
            displayPoint += Vector3.up * Mathf.Sin(battleTime * m_VisualFloatSpeed) * m_VisualFloatAmplitude;
            return displayPoint;
        }

        // 超时未击败时的“回缩”语义是整体向上退场，而不是缩小。
        // 起点使用当前可见位置，终点沿世界 Y 轴上移固定高度，保证视觉上是往上收回。
        private Vector3 ReadTimeoutRetractVisualRootWorldPosition(float battleTime)
        {
            float duration = Mathf.Max(0.0001f, m_TimeoutRetractDuration);
            float elapsed = Mathf.Clamp(duration - Mathf.Max(0f, m_RetractDestroyAt - battleTime), 0f, duration);
            float lerp = Mathf.Clamp01(elapsed / duration);
            return m_RetractStartWorldPosition + Vector3.up * (m_VisualDropHeight * lerp);
        }

        private Vector3 ReadVisualCenterLocalOffsetFromRoot()
        {
            if (m_VisualRoot != null && m_VisualCenterTransform != null)
            {
                return m_VisualRoot.transform.InverseTransformPoint(m_VisualCenterTransform.position);
            }

            return Vector3.up * DefaultCenterHeight;
        }

        private static bool TryBuildAutoAimPitchRange(
            Vector3 aimOrigin,
            Vector3 normalizedHorizontalForward,
            Bounds bounds,
            out float minPitchDegrees,
            out float maxPitchDegrees)
        {
            minPitchDegrees = 0.0f;
            maxPitchDegrees = 0.0f;
            bool hasRange = false;
            var extents = bounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        var corner = bounds.center + Vector3.Scale(extents, new Vector3(x, y, z));
                        if (!TryReadPitchToPointOnHorizontalRay(
                                aimOrigin,
                                normalizedHorizontalForward,
                                corner,
                                out _,
                                out var pitchDegrees))
                        {
                            continue;
                        }

                        if (!hasRange)
                        {
                            minPitchDegrees = pitchDegrees;
                            maxPitchDegrees = pitchDegrees;
                            hasRange = true;
                            continue;
                        }

                        if (pitchDegrees < minPitchDegrees)
                        {
                            minPitchDegrees = pitchDegrees;
                        }

                        if (pitchDegrees > maxPitchDegrees)
                        {
                            maxPitchDegrees = pitchDegrees;
                        }
                    }
                }
            }

            return hasRange;
        }

        private static bool TryReadPitchToPointOnHorizontalRay(
            Vector3 aimOrigin,
            Vector3 normalizedHorizontalForward,
            Vector3 point,
            out float alongRayDistance,
            out float pitchDegrees)
        {
            alongRayDistance = 0.0f;
            pitchDegrees = 0.0f;
            var horizontalDelta = point - aimOrigin;
            horizontalDelta.y = 0.0f;
            alongRayDistance = Vector3.Dot(horizontalDelta, normalizedHorizontalForward);
            if (alongRayDistance <= 0.0001f)
            {
                return false;
            }

            pitchDegrees = Mathf.Atan2(point.y - aimOrigin.y, alongRayDistance) * Mathf.Rad2Deg;
            return true;
        }

        private static bool TryNormalizeHorizontal(Vector3 forward, out Vector3 normalizedHorizontalForward)
        {
            normalizedHorizontalForward = new Vector3(forward.x, 0.0f, forward.z);
            if (normalizedHorizontalForward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            normalizedHorizontalForward.Normalize();
            return true;
        }

        private static Vector3 BuildDirectionFromHorizontalPitch(Vector3 normalizedHorizontalForward, float pitchDegrees)
        {
            float pitchRadians = pitchDegrees * Mathf.Deg2Rad;
            float cosPitch = Mathf.Cos(pitchRadians);
            var direction = normalizedHorizontalForward * cosPitch;
            direction.y = Mathf.Sin(pitchRadians);
            return direction.normalized;
        }

        private bool TryResolvePreviewStageByHealth(out int previewStage, out float targetPreviewScale)
        {
            previewStage = 0;
            targetPreviewScale = 0f;
            if (m_MaxHealth <= 0)
            {
                return false;
            }

            int remainPermille = Mathf.Clamp(
                Mathf.RoundToInt(m_CurrentHealth * 10000.0f / m_MaxHealth),
                0,
                10000);
            int lightThreshold = TowerDefendBattle.ReadUpgradeChallengeCrackLightThresholdPermille();
            int mediumThreshold = Mathf.Min(
                lightThreshold,
                TowerDefendBattle.ReadUpgradeChallengeCrackMediumThresholdPermille());
            int heavyThreshold = Mathf.Min(
                mediumThreshold,
                TowerDefendBattle.ReadUpgradeChallengeCrackHeavyThresholdPermille());
            if (remainPermille > lightThreshold)
            {
                return false;
            }

            if (remainPermille > mediumThreshold)
            {
                previewStage = 1;
                targetPreviewScale = ReadLightCrackPreviewScale();
                return true;
            }

            if (remainPermille > heavyThreshold)
            {
                previewStage = 2;
                targetPreviewScale = ReadMediumCrackPreviewScale();
                return true;
            }

            previewStage = 3;
            targetPreviewScale = ReadFullCrackPreviewScale();
            return true;
        }

        private static float ReadLightCrackPreviewScale()
        {
            BattleConst.ClampUpgradeChallengeCrackValues();
            return BattleConst.UpgradeChallengeCrackLightScale;
        }

        private static float ReadMediumCrackPreviewScale()
        {
            BattleConst.ClampUpgradeChallengeCrackValues();
            return BattleConst.UpgradeChallengeCrackMediumScale;
        }

        private static float ReadFullCrackPreviewScale()
        {
            BattleConst.ClampUpgradeChallengeCrackValues();
            return BattleConst.UpgradeChallengeCrackFullScale;
        }

        private static float ReadUpgradeChallengeDropSpeed()
        {
            BattleConst.ClampTowerDefendUpgradeChallengeValues();
            return BattleConst.TowerDefendUpgradeChallengeDropSpeed;
        }

        private static float ReadUpgradeChallengeShatterDestroyDelay()
        {
            BattleConst.ClampUpgradeChallengeShatterValues();
            return BattleConst.UpgradeChallengeShatterDestroyDelay;
        }

        private bool TryTriggerFinalShatter()
        {
            if (m_FinalShatterTriggered)
            {
                return false;
            }

            ApplyPreviewScale(ReadFullCrackPreviewScale());
            if (!EnsurePreviewFragments())
            {
                return false;
            }

            ResetFragmentPhysics();
            SetFragmentScale(Vector3.one);
            SetCenterPosition(ReadVisualCenterAnchorWorldPosition());
            SetCanBeTarget(false);
            SetCanBeHurt(false);
            m_FinalShatterTriggered = true;
            m_FinalShatterDestroyAt = BattleManager.ReadBattleTime() + ReadUpgradeChallengeShatterDestroyDelay();
            ResetManualShatterPhysicsState();
            SetFragmentRootsVisible(true);
            SetOriginalVisualVisible(false);
            AudioManager.GetInstance().Play2D(m_ShatterAudioId);
            TriggerFinalFragmentExplosion();
            return true;
        }

        private static int ResolveScoreByDistance(float distance)
        {
            for (int i = 0; i < m_ScoreDistanceThresholds.Length; i++)
            {
                if (distance < m_ScoreDistanceThresholds[i])
                {
                    return m_ScoreValues[i];
                }
            }

            return m_ScoreValues[m_ScoreValues.Length - 1];
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (string.Equals(root.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
