using System;
using System.Collections.Generic;
using System.Text;
using LCL;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;



namespace GameDll
{

    public class UActor : UEntity
    {
        private sealed class ColorAlphaRendererOverrideState
        {
            public Renderer m_Renderer;
            public MaterialPropertyBlock[] m_PropertyBlocks;
            public Vector4[] m_Colors;
            public bool[] m_SupportedSlots;
        }

        private sealed class ColorAlphaPropertyOverrideState
        {
            public int m_PropertyId;
            public float m_Alpha = 1.0f;
            public bool m_RendererStatesInitialized;
            public bool m_HasSupport;
            public ColorAlphaRendererOverrideState[] m_RendererStates;
        }

        private UAnimatorComponent m_Animator = new UAnimatorComponent();
        // 渲染层只负责记录颜色属性的原始 RGB，并允许外部流程按属性 id 单独覆盖 Alpha。
        private readonly Dictionary<int, ColorAlphaPropertyOverrideState> m_ColorAlphaPropertyStates = new Dictionary<int, ColorAlphaPropertyOverrideState>();


        protected int m_AngularSpeed = 720;
        protected float m_fMoveSpeed = 0;



        public override void Init()
        {
            base.Init();
            m_HudCompoent.m_Resource = this;
            if (m_EntityType == emEntityType.em_EntityType_PlayerHero ||
                m_EntityType == emEntityType.em_EntityType_Hero ||
                m_EntityType == emEntityType.em_EntityType_MasterHero ||
                m_EntityType == emEntityType.em_EntityType_PlayerHero)
            {
                m_HudCompoent.SetHudType(HudType.Player);
            }
            else
            {
                m_HudCompoent.SetHudType(HudType.Monster);
            }

            m_Animator.m_Resource = this;
        }


        public override void SetMoveSpeed(float speed)
        {
            m_fMoveSpeed = speed;
        }
        public override float GetMoveSpeed()
        {
            return m_fMoveSpeed;
        }
        public override void Destroy()
        {
            ResetColorAlphaOverrides(true);
            base.Destroy();
            m_HudCompoent.Destroy();
        }
        public override void LoadShowObjFromFileAsync(Action<bool, UResource> call)
        {
            m_Destroy = false;
            m_UserLoadedCall = call;

            m_bABRes = true;
            //if (m_FullPath)
            //{
            //    m_ABId = new ABRequest();
            //    m_ABId.assetType = typeof(GameObject);
            //    m_ABId.abName = m_ABName;
            //    m_ABId.mainAssetName = m_AssetName;
            //    m_ABId.sharpFunc = OnAsyncLoaded;
            //    m_ABId.fullPath = m_FullPath;
            //    m_ABId = LCL.UIRes.LoadPrefabAsync(m_ABId);
            //}
            //else
            //{
                m_ABId = LCL.UIRes.LoadPrefabAsync(typeof(GameObject), m_ABName, m_AssetName, OnAsyncLoaded);
            //}

        }
        protected override bool LoadShowObjImp(UnityEngine.Object obj)
        {
            ResetColorAlphaOverrides(false);
            if(base.LoadShowObjImp(obj) == false)
            {
                return false;
            }
            else
            {
                m_Animator.LoadShowObjImp(m_GameObject);
                ApplyAllColorAlphaOverrides();
                return true;
            }
        }
        public override bool SupportsColorAlphaProperty(int propertyId)
        {
            if (propertyId <= 0)
            {
                return false;
            }

            var state = GetOrCreateColorAlphaPropertyState(propertyId);
            EnsureColorAlphaOverrideState(state);
            return state.m_HasSupport;
        }
        public override float ReadColorAlphaProperty(int propertyId)
        {
            if (propertyId <= 0)
            {
                return 1.0f;
            }

            return GetOrCreateColorAlphaPropertyState(propertyId).m_Alpha;
        }
        public override void SetColorAlphaProperty(int propertyId, float alpha)
        {
            if (propertyId <= 0)
            {
                return;
            }

            var state = GetOrCreateColorAlphaPropertyState(propertyId);
            state.m_Alpha = Mathf.Clamp01(alpha);
            ApplyColorAlphaOverride(state);
        }
        public override void ClearColorAlphaProperty(int propertyId)
        {
            if (propertyId <= 0)
            {
                return;
            }

            ColorAlphaPropertyOverrideState state;
            if (!m_ColorAlphaPropertyStates.TryGetValue(propertyId, out state))
            {
                return;
            }

            m_ColorAlphaPropertyStates.Remove(propertyId);
            ClearColorAlphaOverride(state);
            ApplyAllColorAlphaOverrides();
        }
        public override void SetAnimationSpeed(float speed)
        {
            m_Animator.SetAnimationSpeed(speed);
        }
        public override void ReplayCurrentAnimation(float normalizedTime)
        {
            m_Animator.ReplayCurrentAnimation(normalizedTime);
        }
        public override void SetAnimationMaxTime(float time)
        {
            m_Animator.SetAnimationMaxTime(time);
        }

        public override void PlayAnimation(string ani, string endAni = null, float time = 0.3f, bool useTrigger = false)
        {
            m_Animator.PlayAnimation(ani, endAni, time, useTrigger);
        }

        protected UHudComponent m_HudCompoent = new UHudComponent();

        public override void SetShowHud(bool show)
        {
            m_HudCompoent.SetShowHud(show);
        }
        public override void SetShowHudName(bool show)
        {
            m_HudCompoent.SetShowHudName(show);
        }
        public override void SetShowHudBlood(bool show)
        {
            if (m_HudCompoent == null)
            {
                return;
            }
            m_HudCompoent.SetShowHudBlood(show);
        }
        public override void EnableHudRender()
        {
            m_HudCompoent.EnableHudRender();
        }

        public override void SetShowExp(bool showExp)
        {
            m_HudCompoent.SetShowExp(showExp);
        }
        public override void SetShowLevel(bool showLevel)
        {
            m_HudCompoent.SetShowLevel(showLevel);
        }
        public override void SetLevel(long level)
        {
            m_HudCompoent.SetLevel(level);
        }
        public override void SetExp(float exp)
        {
            m_HudCompoent.SetExp(exp);
        }

        public override void SetCampColor(string campColor)
        {
            m_HudCompoent.SetCampColor(campColor);
        }

        public override void SetHpValue(float cur, float tween_time = 0)
        {
            m_HudCompoent.SetHpValue(cur, tween_time);
        }
        public override void SetMagicValue(float cur, float tween_time = 0)
        {
            m_HudCompoent.SetMagicValue(cur, tween_time);
        }
        public void ShowNumber(HpTextType type, string num, float size, Vector3 pos, string textColorHtml = null)
        {
            m_HudCompoent.ShowNumber(type, num, size, pos, textColorHtml);
        }
        public override void Update()
        {
            base.Update();
            m_HudCompoent.Update();
            UpdateTransform();
        }


        public override void DisableHudRender()
        {
            m_HudCompoent.DisableHudRender();
        }


       
        protected override void SetNameImp()
        {
            base.SetNameImp();
            m_HudCompoent.SetName(m_Name);
        }
        public override void SetName(string name)
        {
            base.SetName(name);
            m_HudCompoent.SetName(name);
        }
        protected float m_DashTotalTime = 0;
        protected Vector3 m_DashTargetPosition;
        protected List<Vector3> m_DashTargetPositions = new List<Vector3>();
        protected int m_DashIndex = 0;
        protected Vector3 m_DashStartPosition;
        protected float m_DashStartTime = 0;
        public override void SetDashTotalTime(float speed)
        {
            m_DashTotalTime = speed; 
        }
        private bool m_StartDash = false;
        public override void StartDash()
        {
            m_DashStartTime = Time.realtimeSinceStartup;
            m_StartDash = true;
        }

        public static float m_SpeedFix = 200f;
        private ColorAlphaPropertyOverrideState GetOrCreateColorAlphaPropertyState(int propertyId)
        {
            ColorAlphaPropertyOverrideState state;
            if (!m_ColorAlphaPropertyStates.TryGetValue(propertyId, out state))
            {
                state = new ColorAlphaPropertyOverrideState();
                state.m_PropertyId = propertyId;
                m_ColorAlphaPropertyStates.Add(propertyId, state);
            }

            return state;
        }

        private void EnsureColorAlphaOverrideState(ColorAlphaPropertyOverrideState state)
        {
            if (state == null || state.m_RendererStatesInitialized)
            {
                return;
            }

            state.m_RendererStatesInitialized = true;
            state.m_HasSupport = false;
            state.m_RendererStates = null;
            if (m_GameObject == null)
            {
                return;
            }

            var renderers = m_GameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            var states = new List<ColorAlphaRendererOverrideState>(renderers.Length);
            int rendererCount = renderers.Length;
            for (int i = 0; i < rendererCount; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var sharedMaterials = renderer.sharedMaterials;
                if (sharedMaterials == null || sharedMaterials.Length == 0)
                {
                    continue;
                }

                int materialCount = sharedMaterials.Length;
                var rendererState = new ColorAlphaRendererOverrideState
                {
                    m_Renderer = renderer,
                    m_PropertyBlocks = new MaterialPropertyBlock[materialCount],
                    m_Colors = new Vector4[materialCount],
                    m_SupportedSlots = new bool[materialCount],
                };

                bool hasSupportedSlot = false;
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    var material = sharedMaterials[materialIndex];
                    if (material == null || !material.HasProperty(state.m_PropertyId))
                    {
                        continue;
                    }

                    rendererState.m_Colors[materialIndex] = material.GetVector(state.m_PropertyId);
                    rendererState.m_SupportedSlots[materialIndex] = true;
                    hasSupportedSlot = true;
                }

                if (!hasSupportedSlot)
                {
                    continue;
                }

                state.m_HasSupport = true;
                states.Add(rendererState);
            }

            state.m_RendererStates = states.Count > 0 ? states.ToArray() : null;
        }
        private void ApplyAllColorAlphaOverrides()
        {
            foreach (var pair in m_ColorAlphaPropertyStates)
            {
                ApplyColorAlphaOverride(pair.Value);
            }
        }

        private void ClearColorAlphaOverride(ColorAlphaPropertyOverrideState state)
        {
            EnsureColorAlphaOverrideState(state);
            if (state == null || !state.m_HasSupport || state.m_RendererStates == null)
            {
                return;
            }

            int stateCount = state.m_RendererStates.Length;
            for (int stateIndex = 0; stateIndex < stateCount; stateIndex++)
            {
                var rendererState = state.m_RendererStates[stateIndex];
                var renderer = rendererState != null ? rendererState.m_Renderer : null;
                if (renderer == null)
                {
                    continue;
                }

                int materialCount = rendererState.m_SupportedSlots.Length;
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    if (!rendererState.m_SupportedSlots[materialIndex])
                    {
                        continue;
                    }

                    // 这里需要把当前材质槽位上的 PropertyBlock 整体撤掉，再由其余覆盖重新写回，
                    // 这样 `_BaseColor.a` 不会继续压住材质本体或动画里的同名属性。
                    renderer.SetPropertyBlock(null, materialIndex);
                }
            }
        }

        private void ApplyColorAlphaOverride(ColorAlphaPropertyOverrideState state)
        {
            EnsureColorAlphaOverrideState(state);
            if (state == null || !state.m_HasSupport || state.m_RendererStates == null)
            {
                return;
            }

            float alpha = Mathf.Clamp01(state.m_Alpha);
            int stateCount = state.m_RendererStates.Length;
            for (int stateIndex = 0; stateIndex < stateCount; stateIndex++)
            {
                var rendererState = state.m_RendererStates[stateIndex];
                var renderer = rendererState != null ? rendererState.m_Renderer : null;
                if (renderer == null)
                {
                    continue;
                }

                int materialCount = rendererState.m_Colors.Length;
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    if (!rendererState.m_SupportedSlots[materialIndex])
                    {
                        continue;
                    }

                    var block = rendererState.m_PropertyBlocks[materialIndex];
                    if (block == null)
                    {
                        block = new MaterialPropertyBlock();
                        rendererState.m_PropertyBlocks[materialIndex] = block;
                    }

                    renderer.GetPropertyBlock(block, materialIndex);
                    var color = rendererState.m_Colors[materialIndex];
                    color.w = alpha;
                    block.SetVector(state.m_PropertyId, color);
                    renderer.SetPropertyBlock(block, materialIndex);
                }
            }
        }
        private void ResetColorAlphaOverrides(bool resetValues)
        {
            if (resetValues)
            {
                m_ColorAlphaPropertyStates.Clear();
                return;
            }

            foreach (var pair in m_ColorAlphaPropertyStates)
            {
                var state = pair.Value;
                if (state == null)
                {
                    continue;
                }

                state.m_RendererStatesInitialized = false;
                state.m_HasSupport = false;
                state.m_RendererStates = null;
            }
        }
        protected virtual void UpdateTransform()
        {
            if(m_TransformCache  == null)
            {
                return;
            }

            //将方向转换为四元数
            Quaternion quaDir = Quaternion.LookRotation(m_Forward, Vector3.up);
            float sp = Time.deltaTime * m_AngularSpeed / 180;
            //缓慢转动到目标点
            m_TransformCache.rotation = Quaternion.Lerp(m_TransformCache.rotation, quaDir, sp);
            
            
            m_HudCompoent.SetMoveSpeed(m_fMoveSpeed);
            float tr = Time.deltaTime * m_fMoveSpeed / m_SpeedFix;
            m_TransformCache.position = Vector3.Lerp(m_TransformCache.position, m_Position, tr);


        }
    }
}
