using System;
using LCL;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDll
{
    public class LoadLevelParam
    {
        public string m_MapAb;
        public int m_LoadMode = 0;
        public Action<bool> m_OnLoadCall;
    }
    /// <summary>
    /// 原则上场景不支持中断加载，必须加载完毕
    /// </summary>
    public class UScene
    {
        public LoadLevelParam m_Param = null;
        private bool m_IsLoading;
        private ThreadPriority m_PreviousLoadingPriority;

        public UScene()
        {

        }
        public void LoadEmpty(Action<bool> onLoadCall)
        {
            Init("scene/loading.jpg", (int)LoadSceneMode.Single, onLoadCall);
        }

        public void Init(string mapAb, int loadMode, Action<bool> onLoadCall)
        {
            LoadLevel(new LoadLevelParam
            {
                m_MapAb = mapAb,
                m_LoadMode = loadMode,
                m_OnLoadCall = onLoadCall,
            });
        }

        public void Destroy()
        {
            var request = m_Param;
            m_Param = null;
            RestoreLoadingPriority();
            if (request == null || request.m_LoadMode != (int)LoadSceneMode.Additive)
            {
                return;
            }

            var scene = SceneManager.GetSceneByName(Tool.GetAssetName(request.m_MapAb));
            if (scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        public void Enter()
        {
            if(m_Param != null)
            {
                //Res.ActiveLevel( Tool.GetAssetName( m_Param.m_MapAb));
            }

        }

        //注意这个函数仅激活已经加载成功的场景
        public bool ReActiveLoadedLevel()
        {
            var assetName = Tool.GetAssetName(m_Param.m_MapAb);
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(assetName);
            if (scene != null)
            {
                if (!scene.isLoaded)
                {
                    return false;
                }
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                return true;
            }
            else
            {
                UDebug.LogError("需要激活的场景没有找到， scene:" + assetName);
                return false;
            }
        }



        public void LoadLevel(LoadLevelParam param)
        {
            if (param == null)
            {
                return;
            }

            RestoreLoadingPriority();
            m_Param = param;
            m_PreviousLoadingPriority = Application.backgroundLoadingPriority;
            Application.backgroundLoadingPriority = ThreadPriority.High;
            m_IsLoading = true;
            UIRes.LoadLevel(param.m_MapAb, Tool.GetAssetName(param.m_MapAb), param.m_LoadMode, loaded =>
            {
                RestoreLoadingPriority();
                param.m_OnLoadCall?.Invoke(loaded);
            }, () => ReferenceEquals(m_Param, param));
        }

        private void RestoreLoadingPriority()
        {
            if (m_IsLoading)
            {
                Application.backgroundLoadingPriority = m_PreviousLoadingPriority;
                m_IsLoading = false;
            }
        }

        private static Ray m_Ray = new Ray(Vector3.zero, Vector3.down);
        public  Vector3 GetHeight(Vector3 pos)
        {
            m_Ray.origin = pos;
            RaycastHit raycastHit;
            int layer = (1 << LayerMask.NameToLayer("Floor")) | (1 << LayerMask.NameToLayer("PathObj"));
            if (Physics.Raycast(m_Ray, out raycastHit, 10000f, layer))
            {
                return raycastHit.point;
            }
            else
            {
                return pos;
            }
        }
    }
}
