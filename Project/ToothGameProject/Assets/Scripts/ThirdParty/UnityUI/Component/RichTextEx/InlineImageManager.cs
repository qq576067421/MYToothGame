using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUI
{

    /// <summary>
    /// 内联图片管理器：对象池 + 异步加载 + 缓存
    /// </summary>
    public class InlineImageManager
    {
        private readonly RectTransform m_Parent;
        private readonly Queue<RawImage> m_Pool = new Queue<RawImage>();
        private readonly List<InlineImageHandle> m_ActiveHandles = new List<InlineImageHandle>();
        private readonly Dictionary<string, Texture> m_TextureCache = new Dictionary<string, Texture>();
        private readonly Dictionary<string, Sprite> m_SpriteCache = new Dictionary<string, Sprite>();

        public InlineImageManager(RectTransform parent)
        {
            m_Parent = parent;
        }

        /// <summary>
        /// 回收所有活跃的图片到对象池
        /// </summary>
        public void RecycleAll()
        {
            for (int i = 0; i < m_ActiveHandles.Count; i++)
            {
                var handle = m_ActiveHandles[i];
                if (handle.imageObj != null)
                {
                    handle.imageObj.gameObject.SetActive(false);
                    handle.imageObj.texture = null;
                    m_Pool.Enqueue(handle.imageObj);
                }
                if (handle.loadId > 0)
                {
                    LCL.TextureManager.ReturnImageSprite(handle.loadId);
                }
            }
            m_ActiveHandles.Clear();
        }

        /// <summary>
        /// 获取或创建一个RawImage用于内联显示
        /// </summary>
        public InlineImageHandle GetImage(ImageElementInfo info)
        {
            RawImage img;
            if (m_Pool.Count > 0)
            {
                img = m_Pool.Dequeue();
                img.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("InlineImg", typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(m_Parent, false);
                img = go.GetComponent<RawImage>();
                img.raycastTarget = false;
            }

            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(info.width, info.height);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);

            var handle = new InlineImageHandle
            {
                imageObj = img,
                info = info,
            };

            // 缓存检查
            string cacheKey = info.abName + ":" + info.assetName;
            if (info.isSpriteAtlas)
            {
                Sprite cachedSprite;
                if (m_SpriteCache.TryGetValue(cacheKey, out cachedSprite) && cachedSprite != null)
                {
                    img.texture = cachedSprite.texture;
                    img.uvRect = GetSpriteUVRect(cachedSprite);
                    m_ActiveHandles.Add(handle);
                    return handle;
                }
            }
            else
            {
                Texture cachedTex;
                if (m_TextureCache.TryGetValue(cacheKey, out cachedTex) && cachedTex != null)
                {
                    img.texture = cachedTex;
                    img.uvRect = new Rect(0, 0, 1, 1);
                    m_ActiveHandles.Add(handle);
                    return handle;
                }
            }

            // 异步加载
            LoadImageAsync(handle, cacheKey);
            m_ActiveHandles.Add(handle);
            return handle;
        }

        private void LoadImageAsync(InlineImageHandle handle, string cacheKey)
        {
            var param = new LCL.SetImageSpriteParam
            {
                abName = handle.info.abName,
                assetName = handle.info.assetName,
                img = handle.imageObj,
                call = (p, obj) =>
                {
                    if (handle.imageObj == null || handle.imageObj.Equals(null))
                        return;

                    if (obj is Sprite sprite)
                    {
                        m_SpriteCache[cacheKey] = sprite;
                        handle.imageObj.texture = sprite.texture;
                        handle.imageObj.uvRect = GetSpriteUVRect(sprite);
                    }
                    else if (obj is Texture tex)
                    {
                        m_TextureCache[cacheKey] = tex;
                        handle.imageObj.texture = tex;
                        handle.imageObj.uvRect = new Rect(0, 0, 1, 1);
                    }
                }
            };

            handle.loadId = LCL.TextureManager.SetImageSprite(param);
        }

        /// <summary>
        /// 设置图片的位置（相对于父RectTransform的本地坐标）
        /// </summary>
        public void SetImagePosition(InlineImageHandle handle, Vector2 localPos)
        {
            if (handle.imageObj != null && !handle.imageObj.Equals(null))
            {
                handle.imageObj.rectTransform.anchoredPosition = localPos;
            }
        }

        /// <summary>
        /// 清理所有资源
        /// </summary>
        public void Dispose()
        {
            RecycleAll();
            while (m_Pool.Count > 0)
            {
                var img = m_Pool.Dequeue();
                if (img != null && img.gameObject != null)
                {
                    UnityEngine.Object.Destroy(img.gameObject);
                }
            }
            m_TextureCache.Clear();
            m_SpriteCache.Clear();
        }

        private Rect GetSpriteUVRect(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return new Rect(0, 0, 1, 1);
            var texW = (float)sprite.texture.width;
            var texH = (float)sprite.texture.height;
            var r = sprite.textureRect;
            return new Rect(r.x / texW, r.y / texH, r.width / texW, r.height / texH);
        }
    }

    /// <summary>
    /// 内联图片句柄
    /// </summary>
    public class InlineImageHandle
    {
        public RawImage imageObj;
        public ImageElementInfo info;
        public long loadId;
        public Action<InlineImageHandle> onClick;

        /// <summary>
        /// 帧动画相关
        /// </summary>
        public Sprite[] animFrames;
        public float animFramerate = 10f;
        public bool animLoop = true;
        public int animCurrentFrame;
        public float animTimer;
    }
}
