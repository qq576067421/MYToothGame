using System;
using System.Collections.Generic;
using System.Text;
using AmazingAssets.CurvedWorld;
using LCL;
using UnityEngine;

namespace GameDll
{
    public class UEntity:UResource
    {
        protected GameObject m_GameObject = null;

        protected Transform m_TransformCache = null;


        protected Vector3 m_Position = Vector3.zero;
        protected Vector3 m_Forward = Vector3.forward;
        protected Vector3 m_Up = Vector3.up;
        protected Vector3 m_Scale = Vector3.one;
        protected Collider m_Collider = null;
        public override Collider GetCollider()
        {
            return m_Collider;
        }


        protected bool m_bInitVisiable = true;

        private int m_Layer = 0;
        protected virtual void SetLayerImp()
        {
            var show = GetShowObj() as GameObject;
            show.layer = m_Layer;
        }
        public override void SetLayer(int layer)
        {
            m_Layer = layer;
            if (IsObjectLoaded())
            {
                SetLayerImp();
            }
            else
            {
                AddLoadedCall(SetLayerImp);
            }
        }
        public override int GetLayer()
        {
            return m_Layer;
        }

        public override Vector3 GetEulerAngles()
        {
            if(m_TransformCache != null)
            {
                return m_TransformCache.eulerAngles;
            }
            else
            {
                Quaternion q4 = Quaternion.LookRotation(m_Forward);
                return q4.eulerAngles;
            }
        }
        public override void LoadRender(string ab, string assetName)
        {
            m_ABName = ab;
            m_AssetName = assetName;

            LoadShowObjFromFileAsync(null);
            SetParent(Main.GameMainObject, "", false);
        }

        protected override void DestroyImp()
        {
            base.DestroyImp();
            m_TransformCache = null;
            m_HitStickBonesByRenderer = null;
            m_HitStickRenderers = null;
        }
        public override void Destroy()
        {
            base.Destroy();
            ClearHitStickEffects();
            ClearRenderEffect();
        }
        protected override bool LoadShowObjImp(UnityEngine.Object obj)
        {
            if(base.LoadShowObjImp(obj) == false)
            {
                return false;
            }
            m_GameObject = m_UnityObject as GameObject;
            m_TransformCache = m_GameObject.transform;
            m_TransformCache.position = m_Position;
            m_Collider = m_GameObject.GetComponentInChildren<Collider>();
            m_HitStickBonesByRenderer = null;
            m_HitStickRenderers = null;
            return true;
        }

        public override bool IsDestroy()
        {
            return m_Destroy;
        }

        public override Vector3 GetPosition()
        {
            if(m_TransformCache != null)
            {
                return m_TransformCache.position;
            }
            return m_Position;
        }

        public virtual float GetMoveSpeed()
        {
            return 0;
        }

        public override void SetPosition(Vector3 pos)
        {
            m_Position = pos;

            if(IsObjectLoaded())
            {
                m_TransformCache.position = m_Position;
                OnUpdateHud();
            }
        }
        protected virtual void SetNameImp()
        {
            m_GameObject.name = m_GameObjectPrefabName + "_" + m_Name + "_" + m_Id;
        }
        public override void SetName(string name)
        {
            m_Name = name;
            if (IsObjectLoaded())
            {
                SetNameImp();
            }
            else
            {
                AddLoadedCall(SetNameImp);
            }
        }
        public override string GetName()
        {
            return m_Name;
        }
        public virtual Vector3 GetRight()
        {
            if (m_TransformCache != null)
            {
                return m_TransformCache.right;
            }
            else
            {
                return Vector3.right;
            }
        }
        public override Vector3 GetForward()
        {
            if (m_TransformCache != null)
            {
                return m_TransformCache.forward;
            }
            else
            {
                return m_Forward;
            }
        }
        protected virtual void SetForwardImp()
        {
            m_TransformCache.forward = m_Forward;
        }
        protected virtual void SetUpImp()
        {
            m_TransformCache.up = m_Up;
        }
        public override void Update()
        {
            base.Update();
            UpdateRenderEffect();
        }

        public override void SetForward(Vector3 rot)
        {
            m_Forward = rot;
            if (IsObjectLoaded())
            {
                SetForwardImp();
            }
            else
            {
                AddLoadedCall(SetForwardImp);
            }
        }
        public override void SetUp(Vector3 up)
        {
            m_Up = up;
            if (IsObjectLoaded())
            {
                SetUpImp();
            }
            else
            {
                AddLoadedCall(SetUpImp);
            }
        }
        protected virtual void SetScaleImp()
        {
            m_TransformCache.localScale = m_Scale;
        }
        public override void SetScale(Vector3 scale)
        {
            m_Scale = scale;
            if(IsObjectLoaded())
            {
                SetScaleImp();
            }
            else
            {
                AddLoadedCall(SetScaleImp);
            }

        }

        public override Vector3 GetScale()
        {
            if(m_TransformCache != null)
            {
                return m_TransformCache.localScale;
            }
            return m_Scale;
        }

        public override Vector3 GetHeadPoint()
        {
            if (m_TransformCache == null)
            {
                return base.GetHeadPoint();
            }

            var renderers = m_GameObject != null ? m_GameObject.GetComponentsInChildren<Renderer>() : null;
            if (renderers != null && renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }

            var collider = GetCollider();
            if (collider != null)
            {
                var bounds = collider.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }

            return m_TransformCache.position;
        }

        protected virtual void SetActiveImp()
        {
            m_GameObject.SetActive(m_Active);
        }
        public override void SetActive(bool bshow)
        {
            m_Active = bshow;
            if (IsObjectLoaded())
            {
                SetActiveImp();
            }
            else
            {
                AddLoadedCall(SetActiveImp);
            }
        }

        public override bool GetActive()
        {
            return m_Active;
        }


        public virtual void SetParent(UEntity obj, string path, bool worldPositionStays)
        {
            obj.AddLoadedCall(() => 
            {
                SetParent(obj.GetShowObj() as GameObject, path, worldPositionStays);
            });
        }

        public virtual void SetParent(GameObject parent, string path, bool worldPositionStays)
        {
            GameObject tempParent = parent;
            string tempPath = path;
            bool tempStayWorldPosition = worldPositionStays;
            AddLoadedCall(()=> 
            {
                if(IsDestroy())
                {
                    return;
                }
                if (GameObject.Equals(tempParent, null))
                {
                    m_TransformCache.SetParent(null, tempStayWorldPosition);
                }
                else
                {
                    if (string.IsNullOrEmpty(tempPath))
                    {
                        m_TransformCache.SetParent(tempParent.transform, tempStayWorldPosition);
                    }
                    else
                    {
                        Transform hang = tempParent.transform.Find(tempPath);
                        if (hang != null)
                        {
                            m_TransformCache.SetParent(hang, tempStayWorldPosition);
                        }
                    }
                }
            });
        }




        public override void Init()
        {

        }

        public override void SetAnimationSpeed(float speed)
        {
        }
        public override void SetAnimationMaxTime(float time)
        {

        }
        public override void SetCampColor(string color)
        {
        }

        //表现层的特效
        private List<RenderEff> m_RenderEffects = new List<RenderEff>();
        public override void AddRenderEffect(int effCfgId, int duringTimeMM = 0, bool single = false)
        {
            if (single)
            {
                int has_count = m_RenderEffects.Count;
                if (has_count > 0)
                {
                    for(int i =0; i<has_count; ++i)
                    {
                        var has_eff = (RenderEff)m_RenderEffects[i];
                        if(has_eff.m_CfgId == effCfgId)
                        {
                            has_eff.ResetPlay();
                            return;
                        }
                    }
                }
            }
            var render_pos = GetPosition();
            var render_rot = GetEulerAngles();
            var ieff = RenderEffManager.GetInstance().CreateRenderEff(effCfgId);
            if (ieff != null)
            {
                var eff = (RenderEff)ieff;
                eff.ShowEff(false, render_pos, render_rot, Vector3.one);
                if (duringTimeMM > 0)
                {
                    eff.SetDuringTime(duringTimeMM / 1000.0f);
                }
                m_RenderEffects.Add(eff);
            }
        }
        private void UpdateRenderEffect()
        {
            var render_pos = GetPosition();
            int count = m_RenderEffects.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var eff = (RenderEff)m_RenderEffects[i];
                eff.SetPosition(render_pos);
                if (eff.m_DuringTime > 0)
                {
                    if (Time.realtimeSinceStartup - eff.m_StartTime > eff.m_DuringTime)
                    {
                        RenderEffManager.GetInstance().PoolRenderEff(eff);
                        m_RenderEffects.RemoveAt(i);
                        continue;
                    }
                }
            }
        }
        public void ClearRenderEffect()
        {
            int count = m_RenderEffects.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var eff = m_RenderEffects[i];
                RenderEffManager.GetInstance().PoolRenderEff(eff);
            }
            m_RenderEffects.Clear();
        }

        //命中残留特效（例如插在身上的箭）：绝不能用 SetParent 挂到目标身上，否则目标销毁渲染对象时
        //会把特效一起带走，也会连累对象池。这里只记录相对锚点的局部位姿，每帧反算世界位姿跟随。
        //锚点取命中点附近的骨骼：动画只驱动骨骼，渲染根的位置和旋转是不动的，
        //只盯渲染根会导致头部中箭时倒地动画里箭停在原地。
        private sealed class HitStickEffect
        {
            public RenderEff m_Eff = null;
            public Transform m_Anchor = null;
            public Vector3 m_LocalPos = Vector3.zero;
            public Quaternion m_LocalRot = Quaternion.identity;
            public bool m_UseCurvedWorld;
            public Vector3 m_LocalVisualOffset;
            public float m_SpawnTime = 0f;
        }
        private struct HitStickSurface
        {
            public int m_RendererIndex;
            public Vector3 m_SurfacePosition;
            public Vector3 m_VisualPosition;
            public bool m_UseCurvedWorld;
        }
        //同一配置的残留特效数量上限，避免持续命中后目标身上无限堆积
        private const int m_MaxHitStickEffectPerCfg = 3;
        private const float m_HitStickDepth = 0.04f;
        private const float m_HitStickBacktrack = 0.25f;
        private static readonly List<UEntity> m_HitStickTargets = new List<UEntity>();
        private static readonly List<Vector3> m_HitStickVertices = new List<Vector3>();
        private static readonly List<Vector3> m_HitStickVisualVertices = new List<Vector3>();
        private static readonly List<int> m_HitStickTriangles = new List<int>();
        private static readonly List<Material> m_HitStickMaterials = new List<Material>();
        private static Mesh m_HitStickMesh;
        private readonly List<HitStickEffect> m_HitStickEffects = new List<HitStickEffect>();
        private Transform[][] m_HitStickBonesByRenderer;
        private SkinnedMeshRenderer[] m_HitStickRenderers;
        public override bool TryAddHitStickEffect(int effCfgId, Vector3 worldPos, Quaternion worldRotation, out Vector3 visualHitPosition)
        {
            visualHitPosition = worldPos;
            if (effCfgId <= 0)
            {
                return false;
            }
            var bean = MonoBean.t_effectBean.GetConfig(effCfgId, false);
            if (bean == null)
            {
                UDebug.LogError("命中残留特效没有找到，cfg id:" + effCfgId);
                return false;
            }

            EnsureHitStickRenderers();
            if (!TryResolveHitStickSurface(worldPos, worldRotation * Vector3.forward, out var surface))
            {
                return false;
            }

            var anchor = ResolveHitStickAnchor(surface);
            if (anchor == null)
            {
                return false;
            }

            var eff = RenderEffManager.GetInstance().CreateRenderEff(effCfgId);
            if (eff == null)
            {
                return false;
            }

            eff.SetDuringTime(bean.t_time / 1000.0f);
            eff.ShowEff(false, surface.m_VisualPosition, worldRotation.eulerAngles, Vector3.one);

            if (m_HitStickEffects.Count == 0)
            {
                m_HitStickTargets.Add(this);
            }

            var item = AllocHitStickEffect(effCfgId);
            item.m_Eff = eff;
            item.m_Anchor = anchor;
            var inverseRotation = Quaternion.Inverse(anchor.rotation);
            var bentSurface = surface.m_UseCurvedWorld
                ? CurveWorldVisualUtility.ApplyVisualOffset(surface.m_SurfacePosition)
                : surface.m_SurfacePosition;
            item.m_LocalPos = anchor.InverseTransformPoint(surface.m_SurfacePosition);
            item.m_LocalRot = inverseRotation * worldRotation;
            item.m_UseCurvedWorld = surface.m_UseCurvedWorld;
            item.m_LocalVisualOffset = inverseRotation * (surface.m_VisualPosition - bentSurface);
            item.m_SpawnTime = Time.realtimeSinceStartup;
            visualHitPosition = surface.m_VisualPosition;
            return true;
        }
        private Transform ResolveHitStickAnchor(HitStickSurface surface)
        {
            var renderer = m_HitStickRenderers[surface.m_RendererIndex];
            var bones = m_HitStickBonesByRenderer[surface.m_RendererIndex];
            Transform nearest = renderer.transform;
            float nearestSqr = float.MaxValue;
            foreach (var bone in bones)
            {
                if (bone == null)
                {
                    continue;
                }
                float sqr = (bone.position - surface.m_SurfacePosition).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = bone;
                }
            }
            return nearest;
        }
        private void EnsureHitStickRenderers()
        {
            if (m_HitStickRenderers != null || m_GameObject == null)
            {
                return;
            }
            m_HitStickRenderers = m_GameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            m_HitStickBonesByRenderer = new Transform[m_HitStickRenderers.Length][];
            for (int i = 0; i < m_HitStickRenderers.Length; ++i)
            {
                m_HitStickBonesByRenderer[i] = m_HitStickRenderers[i].bones ?? Array.Empty<Transform>();
            }
        }

        private bool TryResolveHitStickSurface(
            Vector3 hitPosition, Vector3 direction, out HitStickSurface surface)
        {
            surface = default;
            if (m_HitStickRenderers == null || m_HitStickRenderers.Length == 0)
            {
                return false;
            }

            if (m_HitStickMesh == null)
            {
                m_HitStickMesh = new Mesh { name = "Hit stick surface", hideFlags = HideFlags.HideAndDontSave };
            }

            var controller = CurveWorldVisualUtility.ReadController();
            var ray = new Ray(hitPosition - direction * m_HitStickBacktrack, direction);
            float firstContact = float.PositiveInfinity;
            float nearestDistanceSqr = float.PositiveInfinity;
            HitStickSurface nearestSurface = default;
            for (int rendererIndex = 0; rendererIndex < m_HitStickRenderers.Length; ++rendererIndex)
            {
                var renderer = m_HitStickRenderers[rendererIndex];
                if (renderer == null || !renderer.enabled || renderer.forceRenderingOff ||
                    !renderer.gameObject.activeInHierarchy || renderer.sharedMesh == null)
                {
                    continue;
                }

                renderer.GetSharedMaterials(m_HitStickMaterials);
                bool hasCurvedMaterial = controller != null && m_HitStickMaterials.Exists(UsesHitStickCurve);
                var bentHitPosition = hasCurvedMaterial
                    ? CurvedWorldUtilities.TransformPosition(hitPosition, controller)
                    : hitPosition;
                float maxRayDistance = m_HitStickBacktrack + renderer.bounds.size.magnitude;
                // BakeMesh 可以读取关闭 Read/Write 的 FBX，并带上当前动画姿态。
                renderer.BakeMesh(m_HitStickMesh, false);
                m_HitStickMesh.GetVertices(m_HitStickVertices);
                m_HitStickVisualVertices.Clear();
                var localToWorld = renderer.localToWorldMatrix;
                for (int j = 0; j < m_HitStickVertices.Count; ++j)
                {
                    var vertex = localToWorld.MultiplyPoint3x4(m_HitStickVertices[j]);
                    m_HitStickVertices[j] = vertex;
                    if (hasCurvedMaterial)
                    {
                        m_HitStickVisualVertices.Add(CurvedWorldUtilities.TransformPosition(vertex, controller));
                    }
                }

                for (int subMesh = 0; subMesh < m_HitStickMesh.subMeshCount; ++subMesh)
                {
                    if (m_HitStickMesh.GetTopology(subMesh) != MeshTopology.Triangles)
                    {
                        continue;
                    }

                    bool bent = hasCurvedMaterial && subMesh < m_HitStickMaterials.Count &&
                        UsesHitStickCurve(m_HitStickMaterials[subMesh]);
                    var visualVertices = bent ? m_HitStickVisualVertices : m_HitStickVertices;
                    var referencePosition = bent ? bentHitPosition : hitPosition;
                    m_HitStickMesh.GetTriangles(m_HitStickTriangles, subMesh);
                    for (int j = 0; j < m_HitStickTriangles.Count; j += 3)
                    {
                        int a = m_HitStickTriangles[j];
                        int b = m_HitStickTriangles[j + 1];
                        int c = m_HitStickTriangles[j + 2];
                        var visualA = visualVertices[a];
                        var visualB = visualVertices[b];
                        var visualC = visualVertices[c];
                        if (TryIntersectHitStickTriangle(ray, visualA, visualB, visualC, out float distance, out var uv) &&
                            distance <= maxRayDistance && distance < firstContact)
                        {
                            firstContact = distance;
                            surface = CreateHitStickSurface(rendererIndex, a, b, c, uv,
                                ray.GetPoint(distance + m_HitStickDepth), bent);
                            continue;
                        }

                        // 逻辑胶囊命中后，飞行线仍可能擦过当前动画的可见网格。
                        if (float.IsPositiveInfinity(firstContact))
                        {
                            var ab = visualB - visualA;
                            var ac = visualC - visualA;
                            var normal = Vector3.Cross(ab, ac);
                            float normalLengthSqr = normal.sqrMagnitude;
                            if (normalLengthSqr <= 1e-12f)
                            {
                                continue;
                            }

                            uv = ClosestHitStickTriangleCoordinates(referencePosition - visualA, ab, ac);
                            var nearestPoint = visualA + ab * uv.x + ac * uv.y;
                            float distanceSqr = (nearestPoint - referencePosition).sqrMagnitude;
                            if (distanceSqr < nearestDistanceSqr)
                            {
                                nearestDistanceSqr = distanceSqr;
                                nearestSurface = CreateHitStickSurface(rendererIndex, a, b, c, uv,
                                    nearestPoint - normal * (m_HitStickDepth / Mathf.Sqrt(normalLengthSqr)), bent);
                            }
                        }
                    }
                }
            }

            m_HitStickMaterials.Clear();
            if (!float.IsPositiveInfinity(firstContact))
            {
                return true;
            }

            surface = nearestSurface;
            return !float.IsPositiveInfinity(nearestDistanceSqr);
        }

        private static HitStickSurface CreateHitStickSurface(
            int rendererIndex, int a, int b, int c, Vector2 uv, Vector3 visualPosition, bool bent)
        {
            var vertex = m_HitStickVertices[a];
            return new HitStickSurface
            {
                m_RendererIndex = rendererIndex,
                m_SurfacePosition = vertex + (m_HitStickVertices[b] - vertex) * uv.x +
                    (m_HitStickVertices[c] - vertex) * uv.y,
                m_VisualPosition = visualPosition,
                m_UseCurvedWorld = bent
            };
        }

        private static Vector2 ClosestHitStickTriangleCoordinates(Vector3 fromA, Vector3 ab, Vector3 ac)
        {
            float abA = Vector3.Dot(ab, fromA);
            float acA = Vector3.Dot(ac, fromA);
            if (abA <= 0f && acA <= 0f)
                return Vector2.zero;

            var fromB = fromA - ab;
            float abB = Vector3.Dot(ab, fromB);
            float acB = Vector3.Dot(ac, fromB);
            if (abB >= 0f && acB <= abB)
                return Vector2.right;

            float areaC = abA * acB - abB * acA;
            if (areaC <= 0f && abA >= 0f && abB <= 0f)
                return new Vector2(abA / (abA - abB), 0f);

            var fromC = fromA - ac;
            float abC = Vector3.Dot(ab, fromC);
            float acC = Vector3.Dot(ac, fromC);
            if (acC >= 0f && abC <= acC)
                return Vector2.up;

            float areaB = abC * acA - abA * acC;
            if (areaB <= 0f && acA >= 0f && acC <= 0f)
                return new Vector2(0f, acA / (acA - acC));

            float areaA = abB * acC - abC * acB;
            if (areaA <= 0f && acB >= abB && abC >= acC)
            {
                float alongBC = (acB - abB) / (acB - abB + abC - acC);
                return new Vector2(1f - alongBC, alongBC);
            }

            float inverseArea = 1f / (areaA + areaB + areaC);
            return new Vector2(areaB * inverseArea, areaC * inverseArea);
        }

        private static bool UsesHitStickCurve(Material material)
        {
            return material != null && material.IsKeywordEnabled("CURVEDWORLD_BEND_TYPE_CYLINDRICALROLLOFF_Z");
        }

        private static bool TryIntersectHitStickTriangle(
            Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance, out Vector2 uv)
        {
            distance = 0f;
            uv = default;
            var ab = b - a;
            var ac = c - a;
            var perpendicular = Vector3.Cross(ray.direction, ac);
            float determinant = Vector3.Dot(ab, perpendicular);
            if (determinant <= 1e-7f)
            {
                return false;
            }

            var fromVertex = ray.origin - a;
            float inverseDeterminant = 1f / determinant;
            float u = Vector3.Dot(fromVertex, perpendicular) * inverseDeterminant;
            var cross = Vector3.Cross(fromVertex, ab);
            float v = Vector3.Dot(ray.direction, cross) * inverseDeterminant;
            distance = Vector3.Dot(ac, cross) * inverseDeterminant;
            uv = new Vector2(u, v);
            return distance >= 0f && u >= 0f && v >= 0f && u + v <= 1f;
        }

        private HitStickEffect AllocHitStickEffect(int effCfgId)
        {
            HitStickEffect oldest = null;
            int sameCfgCount = 0;
            int count = m_HitStickEffects.Count;
            for (int i = 0; i < count; ++i)
            {
                var cur = m_HitStickEffects[i];
                if (cur.m_Eff == null || cur.m_Eff.m_CfgId != effCfgId)
                {
                    continue;
                }
                ++sameCfgCount;
                if (oldest == null)
                {
                    oldest = cur;
                    continue;
                }
                if (cur.m_SpawnTime < oldest.m_SpawnTime)
                {
                    oldest = cur;
                }
            }

            //数量到上限时顶掉最早一支，复用它的记录槽位
            if (oldest != null && sameCfgCount >= m_MaxHitStickEffectPerCfg)
            {
                RenderEffManager.GetInstance().PoolRenderEff(oldest.m_Eff);
                oldest.m_Eff = null;
                return oldest;
            }

            var item = new HitStickEffect();
            m_HitStickEffects.Add(item);
            return item;
        }
        internal static void ReleaseHitStickSurfaceMesh()
        {
            if (m_HitStickMesh != null)
            {
                UnityEngine.Object.Destroy(m_HitStickMesh);
                m_HitStickMesh = null;
            }
        }

        internal static void UpdateHitStickTargets()
        {
            for (int i = m_HitStickTargets.Count - 1; i >= 0; --i)
            {
                var target = m_HitStickTargets[i];
                target.UpdateHitStickEffects();
                if (target.m_HitStickEffects.Count == 0)
                {
                    m_HitStickTargets.RemoveAt(i);
                }
            }
        }

        private void UpdateHitStickEffects()
        {
            int count = m_HitStickEffects.Count;
            if (count <= 0)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            for (int i = count - 1; i >= 0; --i)
            {
                var item = m_HitStickEffects[i];
                var eff = item.m_Eff;
                if (eff == null || eff.m_IsPooled)
                {
                    m_HitStickEffects.RemoveAt(i);
                    continue;
                }
                //到期按特效表配置的展示时间回收，不能交给目标的销毁流程
                if (eff.m_DuringTime > 0 && now - eff.m_StartTime > eff.m_DuringTime)
                {
                    RenderEffManager.GetInstance().PoolRenderEff(eff);
                    item.m_Eff = null;
                    m_HitStickEffects.RemoveAt(i);
                    continue;
                }

                var anchor = item.m_Anchor;
                if (anchor == null)
                {
                    //锚点跟渲染对象一起销毁后箭不再有意义，直接回池避免悬在空中
                    RenderEffManager.GetInstance().PoolRenderEff(eff);
                    item.m_Eff = null;
                    m_HitStickEffects.RemoveAt(i);
                    continue;
                }
                var worldPos = anchor.TransformPoint(item.m_LocalPos);
                if (item.m_UseCurvedWorld)
                {
                    worldPos = CurveWorldVisualUtility.ApplyVisualOffset(worldPos);
                }

                eff.SetPose(worldPos + anchor.rotation * item.m_LocalVisualOffset,
                    (anchor.rotation * item.m_LocalRot).eulerAngles);
            }
        }
        public override void ClearHitStickEffects()
        {
            int count = m_HitStickEffects.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                var eff = m_HitStickEffects[i].m_Eff;
                if (eff != null)
                {
                    RenderEffManager.GetInstance().PoolRenderEff(eff);
                }
            }
            m_HitStickEffects.Clear();
            m_HitStickTargets.Remove(this);
        }
    }
}
