using GameDll;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace GameDll
{
    class SceneBuff : Buff
    {
        protected UResource mObjBuffLoop;

        public override int AddBuff(PropertyEntity source, PropertyEntity target,
            BuffModify modify, Dictionary<long, BuffParamModify> paramModifies)
        {
            base.AddBuff(source, target, modify, paramModifies);

            if (!HasBuffEffectPath())
            {
                return 0;
            }

            if (mObjBuffLoop == null)
            {
                mObjBuffLoop = BattleAPIBridge.__New_EntityObject(ResourceType.Effect, emEntityType.em_EntityType_Effect);
                mObjBuffLoop.LoadRender(ReadBuffEffectPath(), Tool.GetAssetName(ReadBuffEffectPath()));
            }

            AttachLoopEffectToTarget();
            mObjBuffLoop.SetActive(true);
            PlayLoopEffectSound();
            return 0;
        }

        private void AttachLoopEffectToTarget()
        {
            if (mObjBuffLoop == null || m_TargetEnt == null)
            {
                return;
            }

            var mountPath = ReadBuffEffectHangPath();
            var mountTransform = m_TargetEnt.ReadMountTransform(mountPath);
            if (mountTransform != null)
            {
                AttachLoopEffectToMount(mountTransform);
                return;
            }

            mObjBuffLoop.SetPosition(ReadBuffEffectPosition());

            var targetEnt = m_TargetEnt;
            targetEnt.AddLoadedCall(() =>
            {
                if (mObjBuffLoop == null)
                {
                    return;
                }

                var deferredMount = targetEnt.ReadMountTransform(mountPath);
                if (deferredMount != null)
                {
                    AttachLoopEffectToMount(deferredMount);
                }
            });
        }

        private void AttachLoopEffectToMount(Transform mountTransform)
        {
            if (mObjBuffLoop == null || mountTransform == null)
            {
                return;
            }

            void ApplyParent()
            {
                var effectGo = mObjBuffLoop.GetShowObj() as GameObject;
                if (effectGo == null)
                {
                    return;
                }

                effectGo.transform.SetParent(mountTransform, false);
                effectGo.transform.localPosition = Vector3.zero;
                effectGo.transform.localEulerAngles = Vector3.zero;
                effectGo.transform.localScale = Vector3.one;
            }

            if (mObjBuffLoop.IsObjectLoaded())
            {
                ApplyParent();
            }
            else
            {
                mObjBuffLoop.AddLoadedCall(ApplyParent);
            }
        }

        private void PlayLoopEffectSound()
        {
            var sound = ReadBuffEffectSound();
            if (!IsConfigValueSet(sound) || m_TargetEnt == null)
            {
                return;
            }

            var pos = ReadBuffEffectPosition();
            if (int.TryParse(sound, out var soundId) && soundId > 0)
            {
                AudioManager.GetInstance().Play3D(soundId, pos);
            }
            else
            {
                Debug.LogWarning("场景增益声音必须配置声音表编号，当前值：" + sound);
            }
        }

        protected override void LoadEffect()
        {

        }

        public override void OnRemove()
        {
            base.OnRemove();

            if (mObjBuffLoop != null)
            {
                mObjBuffLoop.SetActive(false);
            }
        }
    }

}

