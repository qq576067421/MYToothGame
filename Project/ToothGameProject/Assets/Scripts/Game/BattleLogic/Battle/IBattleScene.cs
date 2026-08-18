using GameDll;
using GameDll;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameDll
{

    public class IBattleScene
    {
        protected BattleType m_SceneType = 0;
        protected LevelInputData m_LevelInputData;
        protected int m_Stage = 1;

        protected Transform m_CameraRoot = null;
        protected Camera m_Camera;

        public int GetStage()
        {
            return m_Stage;
        }
        public virtual bool IsLoaded()
        {
            return false;
        }
        public virtual bool ReadIsBattleStartLoadingReady()
        {
            return true;
        }
        public virtual bool IsReplay()
        {
            return false;
        }

        public BattleType GetSceneType()
        {
            return m_SceneType;
        }
        public virtual void Init(LevelInputData data)
        {

        }
        protected virtual void LoadScene()
        {

        }

        protected virtual void ParseScene()
        {

        }


        public virtual void Update(float frame_time)
        {

        }
        public virtual void LateUpdate()
        {

        }
        public virtual void Destroy()
        {

        }

        public virtual bool TryConsumeResult(out BattleResultData result)
        {
            result = null;
            return false;
        }

        public virtual UScene GetSceneResource()
        {
            return null;
        }
        public virtual GameObject GetSceneRoot()
        {
            return null;
        }

        public virtual void OnDrawGizmos()
        {

        }

        public virtual void SetSceneStatus(int status)
        {
        }

        protected RuntimeLightParams m_RuntimeLight;
        public virtual void SetLightInfo()
        {
            var sceneRoot = GetSceneRoot();
            var rlp = GameObject.FindObjectOfType<RuntimeLightParams>(true);
            m_RuntimeLight = rlp;
            if (rlp != null)
            {
                var light = rlp.m_Light;
                if (light != null)
                {
                    if(!light.gameObject.activeSelf)
                    {
                        light.gameObject.SetActive(true);
                    }

                    light.cullingMask =
                            //不再通过系统自带的影子方案了
                        //(1 << (int)GameLayer.Floor) |
                        (1 << (int)GameLayer.Building) |
                        (1 << (int)GameLayer.ClickAble) |
                        (1 << (int)GameLayer.Tower) |
                        (1 << (int)GameLayer.Char) |
                        (1 << (int)GameLayer.TowerBase);

                    //if(QualitySettings.currentLevel >= QualityLevel.Simple)
                    //{
                    //    light.cullingMask |= (1 << (int)GameLayer.Default);
                    //}

                    //光照可以照射，但是根据实际情况，不需要有影子
                    var meshes = sceneRoot.GetComponentsInChildren<MeshRenderer>();
                    foreach (var mesh in meshes)
                    {
                        if (mesh.gameObject.layer == (int)GameLayer.Default || 
                            mesh.gameObject.layer == (int)GameLayer.TowerBase ||
                            mesh.gameObject.layer == (int)GameLayer.Floor)
                        {
                            mesh.shadowCastingMode = ShadowCastingMode.Off;
                        }
                    }

                    var open_after = PlayerPrefs.GetInt("pic_after", 0) == 1;
                    if (open_after)
                    {
                        light.intensity = rlp.m_Intensity2 * m_LightFactor;
                        m_LightPicAfter = true;
                    }
                    else
                    {
                        light.intensity = rlp.m_Intensity * m_LightFactor;
                        m_LightPicAfter = false;
                    }
                }
            }




        }
        public virtual void SetPostEffect()
        {
            var position_helpers = GameObject.FindObjectsOfType<PositionHelper>();
            foreach(var positionHelper in position_helpers) 
            {
                positionHelper.gameObject.SetActive(false);
            }

        }
        public virtual void LoadCache()
        {
            HudManager.Init();
        }


        //0表示完全黑 1表示白天
        protected float m_LightFactor = 1.0f;
        protected bool m_LightPicAfter = false;
        public void SetLight(float factor)
        {
            m_LightFactor = factor;
            if(m_RuntimeLight != null)
            {
                var light = m_RuntimeLight.m_Light;
                if(light != null)
                {
                    if (m_LightPicAfter)
                    {
                        light.intensity = m_RuntimeLight.m_Intensity2 * m_LightFactor;
                    }
                    else
                    {
                        light.intensity = m_RuntimeLight.m_Intensity * m_LightFactor;
                    }
                }
            }
        }

        public virtual bool IsOpenRecord()
        {
            return false;
        }

        protected virtual void OnLoadingScene()
        {

        }


        protected virtual void OnLoadMap()
        {
            
        }
    }
}
