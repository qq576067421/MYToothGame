using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LCL
{
    public class SetImageSpriteParam
    {
        public string abName;
        public string assetName;
        public Graphic img;
        public Action<SetImageSpriteParam, object> call;
    }
    /// <summary>
    /// 所有的接口都是采用回调的方式处理
    /// </summary>
    public class TextureManager
    {
        private static Dictionary<long, SetImageSpriteParam> m_Textures = new Dictionary<long, SetImageSpriteParam>();
        private static Dictionary<long, ABRequest> m_IdRequests = new Dictionary<long, ABRequest>();
        private static Sprite m_BlankSprite;
        private static bool m_UseTexturePacker = false;
        public static void SetImageDefault(Graphic g)
        {
            if (m_BlankSprite == null)
            {
                m_BlankSprite = Resources.Load<Sprite>("resource_blank");
            }
            if (g is RawImage)
            {
                RawImage img = g as RawImage;
                img.texture = m_BlankSprite.texture;
            }
            else
            {
                Image img = g as Image;
                img.sprite = m_BlankSprite;
            }
        }
        public static void SetImageDefaultIfNo(Graphic g)
        {
            bool no_texture = false;
            if (g is RawImage)
            {
                RawImage img = g as RawImage;
                if(img.texture == null || img.texture.Equals(null))
                {
                    no_texture = true;
                }
            }
            else
            {
                Image img = g as Image;
                if(img.sprite == null || img.sprite.Equals(null))
                {
                    no_texture = true;
                }
            }

            if(no_texture)
            {
                SetImageDefault(g);
            }
        }
        public static long SetImageSprite(SetImageSpriteParam param, bool useBlank = true)
        {
            if (string.IsNullOrEmpty(param.abName) || string.IsNullOrEmpty(param.assetName))
            {
                return 0;
            }
            //设置占位图片
            if (useBlank)
            {
                SetImageDefault(param.img);
            }
            Type t = null;
            Type imgType = param.img.GetType();
            if (imgType == typeof(RawImage) || imgType == typeof(UnityUI.LUIRawImage))
            {
                t = typeof(Texture);
            }
            else
            {

                if (m_UseTexturePacker)
                {
                    t = typeof(UIAtals);
                }
                else
                {
                    t = typeof(Sprite);
                }
            }



            var id = UIRes.LoadPrefabAsync(t, param.abName, param.assetName, OnLoadedAtlas, OnLoadedAtlasError);
            m_Textures.Add(id.LoadIndex, param);
            m_IdRequests.Add(id.LoadIndex, id);
            return id.LoadIndex;
        }

        private static void OnLoadedAtlasError(long id, ABRequestResult result)
        {
            SetImageSpriteParam param;
            if (m_Textures.TryGetValue(id, out param))
            {
                SetImageImp(param, null);
                param.call?.Invoke(param, null);
            }
        }

        private static void OnLoadedAtlas(ResData list, object userData)
        {
            if (list == null)
            {
                Debug.LogError("图集或者图片加载失败");
                return;
            }
            SetImageSpriteParam param;
            if (m_Textures.TryGetValue(list.m_Id, out param))
            {
                UnityEngine.Object obj = list.m_Obj;
                Type imgType = param.img.GetType();
                if (imgType == typeof(RawImage) || imgType == typeof(UnityUI.LUIRawImage))
                {
                    Texture t = null;
                    if (obj != null)
                    {
                        if (obj is Texture)
                        {
                            t = (Texture)obj;
                        }
                        else
                        {
                            t = (obj as Sprite).texture;
                        }
                    }
                    if (t != null && param.call != null)
                    {
                        SetImageImp(param, t);
                        param.call(param, t);
                    }
                }
                else
                {
                    if (m_UseTexturePacker)
                    {
                        UIAtals atlas = (UIAtals)obj;
                        if (atlas != null && param.call != null)
                        {
                            SetImageImp(param, atlas.GetSprite(param.assetName));
                            param.call(param, atlas.GetSprite(param.assetName));
                        }
                    }
                    else
                    {
                        var sprite = (Sprite)obj;
                        if (param.call != null)
                        {
                            SetImageImp(param, sprite);
                            param.call(param, sprite);
                        }
                    }
                }
                
            }
        }

        public static void ReturnImageSprite(long id)
        {
            if (id <= 0)
            {
                return;
            }
            if (m_Textures.ContainsKey(id))
            {
                m_Textures.Remove(id);
            }
            if(m_IdRequests.ContainsKey(id))
            {
                var request = m_IdRequests[id];
                UIRes.UnloadPrefab(request);
                m_IdRequests.Remove(id);
            }
        }
        //逻辑层不需要此次id的设置图片
        public static void NotSetTexture(long id)
        {
            if (id <= 0)
            {
                return;
            }
            if (m_Textures.ContainsKey(id))
            {
                m_Textures.Remove(id);
            }
        }
        private static void SetImageImp(SetImageSpriteParam param, object sp)
        {
            if (param.img is RawImage)
            {
                if (param.img != null && !param.img.Equals(null))
                {
                    RawImage img = param.img as RawImage;
                    img.texture = sp as Texture;
                }
            }
            else
            {
                if (param.img != null && !param.img.Equals(null))
                {
                    Image img = param.img as Image;
                    img.sprite = sp as Sprite;
                }
            }
        }

        public static Texture LoadTexture(string path)
        {

            //创建文件读取流
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                fileStream.Seek(0, SeekOrigin.Begin);
                //创建文件长度缓冲区
                byte[] bytes = new byte[fileStream.Length];
                //读取文件
                fileStream.Read(bytes, 0, (int)fileStream.Length);
                //释放文件读取流


                //创建Texture
                Texture2D texture = new Texture2D(0, 0);
                texture.LoadImage(bytes);
                return texture;
            }
            catch(Exception e)
            {
                Debug.LogError("加载图片失败，error：" + e.ToString());
            }
            finally
            {
                if(fileStream != null)
                {
                    fileStream.Close();
                    fileStream.Dispose();
                    fileStream = null;
                }

            }
            return null;
        }
        public static Sprite LoadSprite(string path)
        {

            //创建文件读取流
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                fileStream.Seek(0, SeekOrigin.Begin);
                //创建文件长度缓冲区
                byte[] bytes = new byte[fileStream.Length];
                //读取文件
                fileStream.Read(bytes, 0, (int)fileStream.Length);
                //释放文件读取流


                //创建Texture
                Texture2D texture = new Texture2D(0, 0);
                texture.LoadImage(bytes);
                //创建Sprite
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                return sprite;
            }
            catch (Exception e)
            {
                Debug.LogError("加载图片失败，error：" + e.ToString());
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream.Dispose();
                    fileStream = null;
                }

            }
            return null;
        }
    }

}