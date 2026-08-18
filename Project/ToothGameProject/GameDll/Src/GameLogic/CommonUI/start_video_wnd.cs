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
    public class start_video_wnd : WindowBase
    {
        private v_start_video_wnd m_View;
        private VideoPlayer videoPlayer;
        private int prepareTimeout = 3;
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
            videoPlayer = m_View.m_RawLOGO.GetComponent<VideoPlayer>();
            RenderAPI.StartCoroutine(PrepareAndPlay());
        }
        IEnumerator PrepareAndPlay()
        {
            // 开始准备
            videoPlayer.Prepare();
            float timer = 0f;

            // 循环判断是否准备完成
            while (!videoPlayer.isPrepared)
            {
                timer += Time.deltaTime;

                // 超时判断：防止无限等待导致卡死
                if (timer >= prepareTimeout)
                {
                    Debug.LogError("视频准备超时，可能文件损坏或网络异常");
                    yield break; // 退出协程，不播放
                }

                yield return null;
            }

            m_View.m_RawLOGO.enabled = true;
            videoPlayer.Play();
        }


        protected override void OnDestroy()
        {

        }
    }
}
