using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{
    public class UEffect : UEntity
    {

        private ParticleSystem m_ParticleSystem;
        private bool m_bLoop = false;
        private bool m_bWithChildren = false;
        private bool m_bPlayParticle = false;

        public override void LoadRender(string ab, string assetName)
        {
            m_ABName = ab;
            m_AssetName = assetName;

            LoadShowObjFromFileAsync(null);
        }

        protected override bool LoadShowObjImp(UnityEngine.Object obj)
        {
            if (base.LoadShowObjImp(obj) == false)
            {
                return false;
            }
            m_ParticleSystem = m_GameObject.GetComponentInChildren<ParticleSystem>();
            return true;
        }

        public void SetLoop(bool loop)
        {
            m_bLoop = loop;
        }
        public void SetWithChildren(bool bChildren)
        {
            m_bWithChildren = bChildren;
        }
        protected virtual void PlayImp()
        {
            var main = m_ParticleSystem.main;
            main.loop = m_bLoop;
            if (m_bPlayParticle)
            {
                m_ParticleSystem.Play(m_bWithChildren);
            }
            else
            {
                m_ParticleSystem.Stop(m_bWithChildren);
            }
        }
        public void Play()
        {
            m_bPlayParticle = true;
            if (IsObjectLoaded())
            {
                PlayImp();
            }
            else
            {
                AddLoadedCall(PlayImp);
            }
        }

        protected virtual void PauseImp()
        {
            var main = m_ParticleSystem.main;
            main.loop = m_bLoop;
            if (m_bPlayParticle)
            {
                m_ParticleSystem.Play(m_bWithChildren);
            }
            else
            {
                m_ParticleSystem.Stop(m_bWithChildren);
            }
        }
        private void Pause()
        {
            m_bPlayParticle = false;
            if (IsObjectLoaded())
            {
                PauseImp();
            }
            else
            {
                AddLoadedCall(PauseImp);
            }
        }

        public override void Update()
        {
            base.Update();

            UpdateAttackToParent();
        }

        private void UpdateAttackToParent()
        {
            if(m_AttachParent != null)
            {
                var pos3d = m_AttachParent.GetPosition();
                SetPosition(pos3d);
            }
        }
    }
}
