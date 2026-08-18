using System;
using System.Collections.Generic;
using System.Text;
using LCL;
using UnityEngine;


public class ShaderManager
{

    private static Dictionary<string, Shader> m_ShaderList = new Dictionary<string, Shader>();
    //曾经加载过shaderlist，有可能因为shaderlist本来就为空，所以没有数据，这是正常情况
    private static bool m_bLoadedShaderList = false;
    public static void CacheShader(Action callback)
    {
        m_bLoadedShaderList = false;
        UIRes.LoadPrefabAsync(typeof(GameObject), "prefab/shaderslist.jpg", "shaderslist", (rd, ud)=> 
        {
            m_bLoadedShaderList = true;
            GameObject clone = GameObject.Instantiate(rd.m_Obj) as GameObject;
            GameObject.DontDestroyOnLoad(clone);
            ShadersList _shaderslist = clone.GetComponent<ShadersList>();
            foreach (Shader _shader in _shaderslist.list)
            {
                if (_shader != null)
                {
                    if (!m_ShaderList.ContainsKey(_shader.name))
                    {
                        m_ShaderList.Add(_shader.name, _shader);
                    }
                }
                else
                {
                    Debug.LogError("ShadersList has null shader, please check");
                }
            }
            //GameObject.Destroy(main);
            UnityEngine.Debug.Log("加载shaderslist成功，加载" + m_ShaderList.Count.ToString() + "个shader");
            callback();
        });
		
	
    }
    public static void PrewarmShader(Action callback)
    {
	     //UIRes.LoadPrefabAsync(typeof(ShaderVariantCollection), "shader/art_variants.jpg", "art_variants", (rd, ud) =>
         //   {
                
         //       var collect = rd.m_Obj as ShaderVariantCollection;

         //       //UnityEngine.Experimental.Rendering.ShaderWarmup.WarmupShaderFromCollection
         //       float prewarm_time0 = Time.realtimeSinceStartup;
         //       collect.WarmUp();
         //       float prewarm_time1 = Time.realtimeSinceStartup;
         //       var time = prewarm_time1 - prewarm_time0;
         //       Debug.Log("预热美术shader耗时：" + time + 
         //           " shaderCount:" + collect.shaderCount + 
         //           " variantCount:" + collect.variantCount);
            UIRes.LoadPrefabAsync(typeof(ShaderVariantCollection), "shader/code_variants.jpg", "code_variants", (rd, ud) =>
            {
                var collect = rd.m_Obj as ShaderVariantCollection;
                float prewarm_time0 = Time.realtimeSinceStartup;
                collect.WarmUp();
                float prewarm_time1 = Time.realtimeSinceStartup;
                var time = prewarm_time1 - prewarm_time0;
                Debug.Log("预热程序shader耗时：" + time +
                " shaderCount:" + collect.shaderCount +
                " variantCount:" + collect.variantCount);
//#if UNITY_EDITOR
//                float editorPrewarmTime0 = Time.realtimeSinceStartup;
//                Shader.WarmupAllShaders();
//                float editorPrewarmTime1 = Time.realtimeSinceStartup;
//                Debug.Log("预热编辑器已加载shader耗时：" + (editorPrewarmTime1 - editorPrewarmTime0));
//#endif
                callback();
            });


        //});

    }
    public static bool IsLoadedShader()
    {
        return m_bLoadedShaderList;
    }
    public static Shader GetShaderAllowNull(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (m_ShaderList.ContainsKey(name))
            {
                return m_ShaderList[name];
            }
        }

        return null;

    }
    public static Shader GetShader(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (m_ShaderList.ContainsKey(name))
            {
                return m_ShaderList[name];
            }
        }
        Debug.LogError("shader没有找到：" + name);
        return Shader.Find("Mobile/Diffuse");

    }
}

