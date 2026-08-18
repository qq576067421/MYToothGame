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
        private string m_MapAb;

        public UScene()
        {

        }
        public void LoadEmpty(Action<bool> onLoadCall)
        {
            m_MapAb = "scene/loading.jpg";
            if (m_Param == null)
            {
                m_Param = new LoadLevelParam();
            }
            m_Param.m_MapAb = m_MapAb;
            m_Param.m_LoadMode = 0;
            m_Param.m_OnLoadCall = onLoadCall;
            LoadLevel(m_Param);
        }
        public void Init(string  mapAb, int loadMode, Action<bool> onLoadCall)
        {
            m_MapAb = mapAb;



            //string GridABName = ABName.Replace(".jpg", "_grid.jpg");
            if(m_Param == null)
            {
                m_Param = new LoadLevelParam();
            }
            m_Param.m_MapAb = mapAb;
            m_Param.m_LoadMode = loadMode;
            m_Param.m_OnLoadCall = onLoadCall;
            LoadLevel(m_Param);
        }

        public void Destroy()
        {
            if (m_Param != null)
            {

                var assetName = Tool.GetAssetName(m_Param.m_MapAb);
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(assetName);
                if (scene != null)
                {
                    if (!scene.isLoaded)
                    {
                        return;
                    }
                    if(m_Param.m_LoadMode == (int)LoadSceneMode.Additive)
                    {
                        //UDebug.LogWarning("我们这里手动调用了卸载场景， scene：" + assetName);
                        //Unloading the last loaded scene , is not supported.Please use SceneManager.LoadScene() / EditorSceneManager.OpenScene() to switch to another scene.
                        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                    }

                }
                

                m_Param.m_OnLoadCall = null;
                m_Param = null;
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
            LoadLevelParam temp = param;
            if (temp != null)
            {
                var last_backgroupdLoadingPriority = Application.backgroundLoadingPriority;
                Application.backgroundLoadingPriority = ThreadPriority.High;
                UDebug.Log("开启到 Application.backgroundLoadingPriority = ThreadPriority.High");
                LCL.UIRes.LoadLevel(temp.m_MapAb, Tool.GetAssetName(param.m_MapAb), param.m_LoadMode, () =>
                {
                    Application.backgroundLoadingPriority = last_backgroupdLoadingPriority;
                    UDebug.Log("关闭到 Application.backgroundLoadingPriority = " + last_backgroupdLoadingPriority);
                    if (temp.m_OnLoadCall != null)
                    {
                        temp.m_OnLoadCall(true);
                    }
                });
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
