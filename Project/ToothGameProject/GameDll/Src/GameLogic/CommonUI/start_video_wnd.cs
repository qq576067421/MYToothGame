using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using GameDll;
using UnityUI;
using UnityEngine.Video;

namespace GameHot
{
    //仅仅用于播放开场Logo视频的界面，请勿随意添加代码，特别是和资源相关的。
    public class start_video_wnd : WindowBase
    {
        private v_start_video_wnd m_View;
        private VideoPlayer m_VideoPlayer;
        private Coroutine m_PrepareCoroutine;
        private int m_PrepareTimeout = 3;
        private bool m_IsClosing;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Hold;
            __CustomUIPrefabDir = UIPrefabDirs.common;
            __ParticipateCurrentActiveWindow = true;
        }
        protected override void OnInitComponent()
        {
            m_View = new v_start_video_wnd();
            m_View.InitComponent(__GetWindowObj());
            m_VideoPlayer = m_View.m_RawLOGO.GetComponent<VideoPlayer>();
            m_VideoPlayer.playOnAwake = false;
        }

        protected override void OnOpen()
        {
            m_IsClosing = false;
            m_View.m_RawLOGO.enabled = false;
            m_VideoPlayer.enabled = true;
            m_PrepareCoroutine = RenderAPI.StartCoroutine(PrepareAndPlay());
        }

        IEnumerator PrepareAndPlay()
        {
            // 开始准备
            VideoPlayer videoPlayer = m_VideoPlayer;
            videoPlayer.Prepare();
            float timer = 0f;

            // 循环判断是否准备完成
            while (!m_IsClosing && videoPlayer != null && !videoPlayer.isPrepared)
            {
                timer += Time.deltaTime;

                // 超时判断：防止无限等待导致卡死
                if (timer >= m_PrepareTimeout)
                {
                    Debug.LogError("视频准备超时，可能文件损坏或网络异常");
                    videoPlayer.Stop();
                    m_PrepareCoroutine = null;
                    yield break; // 退出协程，不播放
                }

                yield return null;
            }

            if (m_IsClosing || videoPlayer == null)
            {
                m_PrepareCoroutine = null;
                yield break;
            }

            m_View.m_RawLOGO.enabled = true;
            videoPlayer.Play();
            m_PrepareCoroutine = null;
        }

        protected override void OnClose()
        {
            StopVideo();
        }

        protected override void OnDestroy()
        {
            StopVideo();
            if (m_VideoPlayer != null)
            {
                m_VideoPlayer.targetTexture = null;
                m_VideoPlayer = null;
            }

            m_View = null;
            base.OnDestroy();
        }

        private void StopVideo()
        {
            m_IsClosing = true;
            if (m_PrepareCoroutine != null)
            {
                RenderAPI.StopCoroutine(m_PrepareCoroutine);
                m_PrepareCoroutine = null;
            }

            if (m_VideoPlayer != null)
            {
                m_VideoPlayer.Stop();
                m_VideoPlayer.enabled = false;
            }

            if (m_View != null && m_View.m_RawLOGO != null)
            {
                m_View.m_RawLOGO.enabled = false;
            }
        }
    }
}
