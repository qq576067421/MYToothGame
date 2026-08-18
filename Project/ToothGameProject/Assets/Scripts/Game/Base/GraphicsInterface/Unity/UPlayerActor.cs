using System;
using System.Collections.Generic;
using LCL;
using UnityEngine;
using UnityEngine.UI;

namespace GameDll
{
    public class FashionMesh
    {
        public enum FashionType
        {
            head = 0,
            chest,
            hand,
            feet
        }
        public FashionType m_Slot;
        public string m_ABName;
        public string m_AssetName;
    }

    public class UPlayerActor : UActor
    {
        private bool m_bIsChangeGo = false;
        private bool m_bCombine = false;
        private bool m_bClearCombineAB = true;
        private List<FashionMesh> m_Fashions = null;
        private Dictionary<int, GameObject> m_FashionObjs = new Dictionary<int, GameObject>();
        private Dictionary<ABRequest, int> m_FashionABNames = new Dictionary<ABRequest, int>();
        private int m_LoadFashionCount = 0;
        private int m_LoadedFashionCount = 0;
        private Vector3 m_BaseForward = Vector3.forward;
        private SpineRotator m_SpineRotator;
        private float m_DefaultPitchDegrees = 0f;
        private float m_DefaultAimPitchDegrees = 0f;
        private float m_DefaultSpineLocalPitchDegrees = 0f;
        private Vector3 m_DefaultFireRotationPointLocalEuler = Vector3.zero;
        private bool m_HasPitchCalibration = false;
        private bool m_UseDefaultPitch = true;

        protected override bool LoadShowObjImp(UnityEngine.Object obj)
        {
            if (!base.LoadShowObjImp(obj))
            {
                return false;
            }

            CacheSpineRotator();
            // 先缓存俯仰标定，再回写当前朝向，避免角色加载完成时沿用未标定的中立脊椎姿态。
            CachePitchCalibration();
            ApplyCurrentBaseForward();
            return true;
        }

        public override void SetForward(Vector3 rot)
        {
            base.SetForward(ResolveHorizontalForward(rot));
            m_UseDefaultPitch = true;
            ApplyCurrentBaseForward();
        }

        public override void SetBaseForward(Vector3 value)
        {
            m_UseDefaultPitch = false;
            if (value.sqrMagnitude <= 0.0001f)
            {
                m_BaseForward = Vector3.forward;
            }
            else
            {
                m_BaseForward = value.normalized;
            }

            if (IsObjectLoaded())
            {
                ApplyBaseForward();
            }
            else
            {
                AddLoadedCall(ApplyBaseForward);
            }
        }

        public override float ReadDefaultPitchDegrees()
        {
            return m_HasPitchCalibration ? m_DefaultAimPitchDegrees : m_DefaultPitchDegrees;
        }

        public override Vector3 GetForward()
        {
            if (m_BaseForward.sqrMagnitude > 0.0001f)
            {
                return m_BaseForward.normalized;
            }

            return base.GetForward();
        }

        private void CacheSpineRotator()
        {
            m_SpineRotator = m_GameObject != null ? m_GameObject.GetComponent<SpineRotator>() : null;
            m_DefaultPitchDegrees = m_SpineRotator != null ? m_SpineRotator.m_DefaultRotation.x : 0f;
            m_DefaultAimPitchDegrees = m_DefaultPitchDegrees;
            m_DefaultSpineLocalPitchDegrees = 0f;
            m_DefaultFireRotationPointLocalEuler = Vector3.zero;
            m_HasPitchCalibration = false;
        }

        private void ApplyCurrentBaseForward()
        {
            if (m_UseDefaultPitch)
            {
                var horizontalForward = ResolveHorizontalForward(m_Forward);
                var yawDegrees = Mathf.Atan2(horizontalForward.x, horizontalForward.z) * Mathf.Rad2Deg;
                var defaultAimPitchDegrees = m_HasPitchCalibration ? m_DefaultAimPitchDegrees : m_DefaultPitchDegrees;
                m_BaseForward = BuildForwardFromYawPitch(yawDegrees, defaultAimPitchDegrees);
            }

            if (IsObjectLoaded())
            {
                ApplyBaseForward();
            }
            else
            {
                AddLoadedCall(ApplyBaseForward);
            }
        }

        private void ApplyBaseForward()
        {
            if (m_SpineRotator == null || m_SpineRotator.m_Spine == null || m_TransformCache == null)
            {
                return;
            }

            var localForward = m_TransformCache.InverseTransformDirection(m_BaseForward);
            var defaultRotation = m_SpineRotator.m_DefaultRotation;
            var targetPitchDegrees = ExtractPitchDegrees(localForward);
            var spineLocalPitchDegrees = -targetPitchDegrees;
            if (m_HasPitchCalibration)
            {
                var pitchOffsetDegrees = targetPitchDegrees - m_DefaultAimPitchDegrees;
                spineLocalPitchDegrees = m_DefaultSpineLocalPitchDegrees - pitchOffsetDegrees;
            }

            m_SpineRotator.m_Spine.localRotation = Quaternion.Euler(
                spineLocalPitchDegrees,
                defaultRotation.y,
                defaultRotation.z);
            ApplyFireRotationPointPitch(spineLocalPitchDegrees);
        }

        private void CachePitchCalibration()
        {
            if (m_SpineRotator == null || m_SpineRotator.m_Spine == null || m_TransformCache == null)
            {
                return;
            }

            // 用角色当前运行时的中立开火方向做一次标定。
            // 后续所有俯仰只在这份中立姿态上叠加增量，避免不同角色资源的默认骨架角度被当成同一套绝对角度处理。
            var defaultAimForward = ResolveDefaultAimForward();
            var localDefaultAimForward = m_TransformCache.InverseTransformDirection(defaultAimForward);
            m_DefaultAimPitchDegrees = ExtractPitchDegrees(localDefaultAimForward);
            m_DefaultSpineLocalPitchDegrees = NormalizeSignedAngle(m_SpineRotator.m_Spine.localEulerAngles.x);
            CacheFireRotationPointCalibration();
            m_HasPitchCalibration = true;
            ApplyFireRotationPointPitch(m_DefaultSpineLocalPitchDegrees);
        }

        private void CacheFireRotationPointCalibration()
        {
            if (m_SpineRotator == null || m_SpineRotator.m_FireRotationPoint == null)
            {
                m_DefaultFireRotationPointLocalEuler = Vector3.zero;
                return;
            }

            var localEulerAngles = m_SpineRotator.m_FireRotationPoint.localEulerAngles;
            m_DefaultFireRotationPointLocalEuler = new Vector3(
                NormalizeSignedAngle(localEulerAngles.x),
                localEulerAngles.y,
                localEulerAngles.z);
        }

        private void ApplyFireRotationPointPitch(float spineLocalPitchDegrees)
        {
            if (!m_HasPitchCalibration || m_SpineRotator == null || m_SpineRotator.m_FireRotationPoint == null)
            {
                return;
            }

            // 这里同步的是俯仰增量，不是把 FireRotationPoint 的 x 绝对值直接改成 Spine 的 x。
            // 这样可以保留两个节点各自的中立姿态，只让它们在运行时共享同一份俯仰变化量。
            var spinePitchDeltaDegrees = NormalizeSignedAngle(spineLocalPitchDegrees - m_DefaultSpineLocalPitchDegrees);
            m_SpineRotator.m_FireRotationPoint.localRotation = Quaternion.Euler(
                m_DefaultFireRotationPointLocalEuler.x + spinePitchDeltaDegrees,
                m_DefaultFireRotationPointLocalEuler.y,
                m_DefaultFireRotationPointLocalEuler.z);
        }

        private static Vector3 BuildForwardFromYawPitch(float yawDegrees, float pitchDegrees)
        {
            return (Quaternion.Euler(pitchDegrees, yawDegrees, 0f) * Vector3.forward).normalized;
        }

        private Vector3 ResolveDefaultAimForward()
        {
            if (m_SpineRotator != null && m_SpineRotator.m_FirePoints != null)
            {
                int firePointCount = m_SpineRotator.m_FirePoints.Length;
                for (int i = 0; i < firePointCount; i++)
                {
                    var firePoint = m_SpineRotator.m_FirePoints[i];
                    if (firePoint == null)
                    {
                        continue;
                    }

                    var forward = firePoint.forward;
                    if (forward.sqrMagnitude > 0.0001f)
                    {
                        return forward.normalized;
                    }
                }
            }

            if (m_BaseForward.sqrMagnitude > 0.0001f)
            {
                return m_BaseForward.normalized;
            }

            return m_TransformCache != null ? m_TransformCache.forward : Vector3.forward;
        }

        private static float ExtractPitchDegrees(Vector3 forward)
        {
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            forward.Normalize();
            var planarLength = Mathf.Sqrt(forward.x * forward.x + forward.z * forward.z);
            return Mathf.Atan2(forward.y, planarLength) * Mathf.Rad2Deg;
        }

        private static float NormalizeSignedAngle(float angleDegrees)
        {
            return Mathf.Repeat(angleDegrees + 180f, 360f) - 180f;
        }

        private static Vector3 ResolveHorizontalForward(Vector3 forward)
        {
            var horizontalForward = new Vector3(forward.x, 0f, forward.z);
            if (horizontalForward.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            return horizontalForward.normalized;
        }




        private bool m_bIsParts = false;
        //改变渲染对象的组成，用于换装
        //一般进入换装模式后要求是全部换
        //如果角色之前有全套或者整体的模型，应该提前卸载，防止资源泄漏
        public void CreateFashions(List<FashionMesh> parts)
        {
            m_bIsParts = true;

            SetSubMeshABNames(parts);
            SetCombine(false, true);
            LoadFashion();
        }

        public List<FashionMesh> GetSubMeshes()
        {
            return m_Fashions;
        }
        //骨架的GameObject
        //private GameObject m_GameObject;

        public void SetSubMeshABNames(List<FashionMesh> parts)
        {
            m_Fashions = parts;

        }

        public void SetCombine(bool combine, bool clearab)
        {
            m_bCombine = combine;
            m_bClearCombineAB = clearab;
        }

        public void LoadFashion()
        {
            AddLoadedCall(() => 
            {
                    m_LoadFashionCount = m_Fashions.Count;
                    m_LoadedFashionCount = 0;

                    for(int i =0;i< m_LoadFashionCount; ++i)
                    {
                        FashionMesh mp = m_Fashions[i];
                        int slot = (int)mp.m_Slot;
                        var id = LCL.UIRes.LoadPrefabAsync(typeof(GameObject), mp.m_ABName, mp.m_AssetName, (objs,userData) =>
                        {
                            if (objs != null && objs.m_Obj != null)
                            {
                                m_LoadedFashionCount++;
                                m_FashionObjs.Add(slot,  (GameObject)objs.m_Obj);

                                if (m_LoadedFashionCount == m_LoadFashionCount)
                                {
                                    GameObject skeleton = GetShowObj() as GameObject;
                                    CombineObject(skeleton, m_FashionObjs, true);
                                    DestroyFashionAB();
                                    CounterManager.GetInstance().AddCounter(3000, 1, Destroy);
                                }

                            }
                            else
                            {
                                UDebug.LogError("资源加载失败");
                            }
                        });

                        m_FashionABNames.Add(id, slot);
                    }
            });
        }

        private void DestroyFashionAB()
        {
            if (m_FashionABNames.Count > 0)
            {
                Dictionary<ABRequest, int>.Enumerator iter = m_FashionABNames.GetEnumerator();
                while (iter.MoveNext())
                {
                    ABRequest id = iter.Current.Key;
                    LCL.UIRes.UnloadPrefab(id);
                    int slot = iter.Current.Value;
                    //GameObject.DestroyImmediate(m_SubMeshes[slot], true);
                }
                m_FashionObjs.Clear();
            }
        }
        protected override void DestroyImp()
        {
            UDebug.LogError("删除换装模型");
            DestroyFashionAB();
            base.DestroyImp();
        }



        //合并材质用的
        private const int COMBINE_TEXTURE_MAX = 512;
        private const string COMBINE_DIFFUSE_TEXTURE = "_MainTex";

        private void CombineObject(GameObject skeleton, Dictionary<int, GameObject> meshes, bool combine = false)
        {

            // Fetch all bones of the skeleton
            List<Transform> transforms = new List<Transform>();
            transforms.AddRange(skeleton.GetComponentsInChildren<Transform>(true));

            List<Material> materials = new List<Material>();//the list of materials
            List<CombineInstance> combineInstances = new List<CombineInstance>();//the list of meshes
            List<Transform> bones = new List<Transform>();//the list of bones

            // Below informations only are used for merge materilas(bool combine = true)
            List<Vector2[]> oldUV = null;
            Material newMaterial = null;
            Texture2D newDiffuseTex = null;


            Dictionary<int, GameObject>.Enumerator iter = meshes.GetEnumerator();
            while (iter.MoveNext())
            {
                SkinnedMeshRenderer smr = iter.Current.Value.GetComponentInChildren<SkinnedMeshRenderer>();
                materials.AddRange(smr.sharedMaterials); // Collect materials
                                                         // Collect meshes
                for (int sub = 0; sub < smr.sharedMesh.subMeshCount; sub++)
                {
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = smr.sharedMesh;
                    ci.subMeshIndex = sub;
                    combineInstances.Add(ci);
                }
                // Collect bones
                for (int j = 0; j < smr.bones.Length; j++)
                {
                    int tBase = 0;
                    for (tBase = 0; tBase < transforms.Count; tBase++)
                    {
                        if (smr.bones[j].name.Equals(transforms[tBase].name))
                        {
                            bones.Add(transforms[tBase]);
                            break;
                        }
                    }
                }
            }

            // merge materials
            if (combine)
            {
                newMaterial = new Material(ShaderManager.GetShader("Mobile/Diffuse"));
                oldUV = new List<Vector2[]>();
                // merge the texture
                List<Texture2D> Textures = new List<Texture2D>();
                for (int i = 0; i < materials.Count; i++)
                {
                    Textures.Add(materials[i].GetTexture(COMBINE_DIFFUSE_TEXTURE) as Texture2D);
                }

                newDiffuseTex = new Texture2D(COMBINE_TEXTURE_MAX, COMBINE_TEXTURE_MAX, TextureFormat.RGBA32, true);
                Rect[] uvs = newDiffuseTex.PackTextures(Textures.ToArray(), 0);
                newMaterial.mainTexture = newDiffuseTex;
                newDiffuseTex.name = m_GameObjectPrefabName + "_combine_tex";

                // reset uv
                Vector2[] uva, uvb;
                for (int j = 0; j < combineInstances.Count; j++)
                {
                    uva = (Vector2[])(combineInstances[j].mesh.uv);
                    uvb = new Vector2[uva.Length];
                    for (int k = 0; k < uva.Length; k++)
                    {
                        uvb[k] = new Vector2((uva[k].x * uvs[j].width) + uvs[j].x, (uva[k].y * uvs[j].height) + uvs[j].y);
                    }
                    oldUV.Add(combineInstances[j].mesh.uv);
                    combineInstances[j].mesh.uv = uvb;
                }
            }

            // Create a new SkinnedMeshRenderer
            SkinnedMeshRenderer oldSKinned = (SkinnedMeshRenderer)skeleton.GetComponent(typeof(SkinnedMeshRenderer));
            if (oldSKinned != null)
            {

                GameObject.DestroyImmediate(oldSKinned);
            }
            SkinnedMeshRenderer r = (SkinnedMeshRenderer)skeleton.AddComponent(typeof(SkinnedMeshRenderer));
            r.sharedMesh = new Mesh();
            r.sharedMesh.CombineMeshes(combineInstances.ToArray(), combine, false);// Combine meshes
            r.bones = bones.ToArray();// Use new bones
            if (combine)
            {
                r.material = newMaterial;
                for (int i = 0; i < combineInstances.Count; i++)
                {
                    combineInstances[i].mesh.uv = oldUV[i];
                }
            }
            else
            {
                r.materials = materials.ToArray();
            }

        }



    }
}
