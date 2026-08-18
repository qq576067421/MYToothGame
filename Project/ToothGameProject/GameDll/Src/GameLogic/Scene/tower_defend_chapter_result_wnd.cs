using LCL;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using GameDll;
using MonoBean;

namespace GameHot
{
    public class tower_defend_chapter_result_model : WindowModel
    {
        public BattleResultData m_Result;
        public Action m_OnContinue;
        public Action m_OnReturn;

        public override void Clear()
        {
            m_Result = null;
            m_OnContinue = null;
            m_OnReturn = null;
        }
    }

    public class tower_defend_chapter_result_wnd : WindowBase
    {
        private const string m_LanTdChapterResultWinTitle = "td_result_chapter_title";
        private const string m_LanTdChapterResultLoseTitle = "td_result_chapter_fail_title";
        private const string m_LanTdChapterResultStage = "td_result_stage";
        private const string m_LanTdChapterResultStar = "td_result_star";
        private const string m_LanTdChapterResultBase = "td_result_base";
        private const string m_LanTdChapterResultKillSummary = "td_result_kill_summary";
        private const string m_LanTdChapterResultKillReward = "td_result_kill_reward";
        private const string m_LanTdChapterResultClearReward = "td_result_clear_reward";
        private const string m_LanTdChapterResultTotalReward = "td_result_total_reward";
        private const string m_LanTdChapterResultTime = "td_result_time";
        private const string m_LanTdChapterResultUnlock = "td_result_unlock_stage";
        private const string m_result_endless_win = "td_result_endless_win";
        private const string m_result_endless_fail = "td_result_endless_fail";
        private const string m_LanTdResultContinueChallenge = "td_result_continue_challenge";
        private const string m_LanTdResultRetryChallenge = "td_result_retry_challenge";
        private const int m_ResultWinAudioId = 306;
        private const int m_ResultLoseAudioId = 307;
        private const int m_ResultBgmAudioId = 308;
        private v_tower_defend_chapter_result_wnd m_View;
        private VideoPlayer m_ResultVideoPlayer;
        private RawImage m_ResultVideoImage;
        private bool m_IsWaitingResultVideoFirstFrame;
        private bool m_IsOpen;
        private int m_ResultAudioVersion;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.battle_result;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);
            __CreateModel(new tower_defend_chapter_result_model());
        }

        public void SetResult(BattleResultData result, Action onContinue, Action onReturn)
        {
            var model = GetModel<tower_defend_chapter_result_model>();
            model.m_Result = result;
            model.m_OnContinue = onContinue;
            model.m_OnReturn = onReturn;

            if (IsInitializedView())
            {
                RefreshView();
            }
        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_chapter_result_wnd();
            m_View.InitComponent(__GetWindowObj());
            RenderAPI.AddButtonClick(m_View.m_btn_continue, OnClickContinue);
            RenderAPI.AddButtonClick(m_View.m_btn_return, OnClickReturn);
        }

        protected override void OnOpen()
        {
            m_IsOpen = true;
            RefreshView();
            PlayResultAudio();
            RenderEvent.Event.OnUpdateSelectionVisuals += OnUpdateSelectionVisuals;
        }

        protected override void OnClose()
        {
            m_IsOpen = false;
            m_ResultAudioVersion++;
            StopResultVideo();
            RenderEvent.Event.OnUpdateSelectionVisuals -= OnUpdateSelectionVisuals;
        }
        private void OnUpdateSelectionVisuals()
        {
            var active = UIManager.GetCurrentActiveWindow();
            if (active == null || active != this)
            {
                return;
            }
            AudioManager.GetInstance().Play2D(4);
            //m_View.m_unchoose_continue.gameObject.SetActive(!m_View.m_unchoose_continue.IsActive());
            //m_View.m_unchoose_return.gameObject.SetActive(!m_View.m_unchoose_return.IsActive());
        }
        private void PlayResultAudio()
        {
            var model = GetModel<tower_defend_chapter_result_model>();
            int id = model.m_Result.m_WinGroup == GroupId.GuardGroupId ? m_ResultWinAudioId : m_ResultLoseAudioId;
            int audioVersion = ++m_ResultAudioVersion;
            Action<AudioPlaybackResult> onFinished = _ => OnResultAudioFinished(audioVersion);
            var callbacks = new AudioPlaybackCallbacks { m_OnCompleted = onFinished, m_OnLoadFailed = onFinished, m_OnRejected = onFinished };
            AudioManager.GetInstance().Play2D(id, AudioTransitionMode.CrossFade, -1f, AudioReplayMode.KeepCurrent, AudioLifetime.Persistent, -1f, default(AudioDuckOptions), callbacks);
        }

        private void OnResultAudioFinished(int audioVersion)
        {
            if (!m_IsOpen || audioVersion != m_ResultAudioVersion)
            {
                return;
            }
            AudioManager.GetInstance().Play2D(m_ResultBgmAudioId, AudioTransitionMode.CrossFade, -1f, AudioReplayMode.KeepCurrent, AudioLifetime.Persistent);
        }

        private void OnClickContinue()
        {
            var model = GetModel<tower_defend_chapter_result_model>();
            if (model == null || model.m_OnContinue == null)
            {
                return;
            }

            model.m_OnContinue.Invoke();
            CloseWithLoadingDelayIfNeeded();
        }

        public bool TryContinueByGesture()
        {
            var model = GetModel<tower_defend_chapter_result_model>();
            if (model == null || model.m_OnContinue == null)
            {
                return false;
            }

            OnClickContinue();
            return true;
        }

        public bool TryReturnByRemote()
        {
            var model = GetModel<tower_defend_chapter_result_model>();
            if (model == null || model.m_OnReturn == null)
            {
                return false;
            }

            OnClickReturn();
            return true;
        }

        private void OnClickReturn()
        {
            var model = GetModel<tower_defend_chapter_result_model>();
            model.m_OnReturn?.Invoke();
            CloseWithLoadingDelayIfNeeded();
        }

        private void CloseWithLoadingDelayIfNeeded()
        {
            var delayMs = loading_wnd.GetRemainingVisibleMilliseconds();
            if (delayMs > 0)
            {
                AddCounter(delayMs, 1, null, 0, () =>
                {
                    UIManager.CloseWindow(this);
                });
                return;
            }

            UIManager.CloseWindow(this);
        }

        private void RefreshView()
        {
            if (!IsInitializedView())
            {
                return;
            }

            var result = GetModel<tower_defend_chapter_result_model>().m_Result;
            if (result == null)
            {
                return;
            }

            //RenderAPI.SetText(
            //    m_View.m_txt_title,
            //    result.m_WinGroup == GroupId.GuardGroupId
            //        ? RenderAPI.GetTextByLanId(m_LanTdChapterResultWinTitle)
            //        : RenderAPI.GetTextByLanId(m_LanTdChapterResultLoseTitle));
            m_View.m_titlebg_win.gameObject.SetActive(result.m_WinGroup == GroupId.GuardGroupId);
            m_View.m_titlebg_fail.gameObject.SetActive(result.m_WinGroup != GroupId.GuardGroupId);
            if(result.m_WinGroup == GroupId.GuardGroupId)
            {
                RenderAPI.SetTextLan(m_View.m_text, m_result_endless_win);
            }
            else
            {
                RenderAPI.SetTextLan(m_View.m_text, m_result_endless_fail);
            }

            RenderAPI.SetTextLan(m_View.m_unchoose_continue,
                result.m_WinGroup == GroupId.GuardGroupId ? m_LanTdResultContinueChallenge : m_LanTdResultRetryChallenge);
            RenderAPI.SetTextLan(m_View.m_unchoose_continue_new1,
                result.m_WinGroup == GroupId.GuardGroupId ? m_LanTdResultContinueChallenge : m_LanTdResultRetryChallenge);
            RenderAPI.SetTextNumber(m_View.m_txt_level, result.m_Stage);
            RenderAPI.SetTextNumber(m_View.m_txt_level_new1, result.m_Stage);
            RenderAPI.SetTextNumber(m_View.m_txt_reward_kill, result.m_KillRewardGold);
            RenderAPI.SetTextNumber(m_View.m_txt_reward_total, result.m_ClearRewardGold);
            SetStar(result);
            //RenderAPI.SetTextLan(m_View.m_txt_star, m_LanTdChapterResultStar, result.m_StarCount);
            //RenderAPI.SetTextLan(m_View.m_txt_base_hp, m_LanTdChapterResultBase, result.m_BaseHealth, result.m_BaseMaxHealth);
            //RenderAPI.SetTextLan(
            //    m_View.m_txt_kill_summary,
            //    m_LanTdChapterResultKillSummary,
            //    result.m_NormalMonsterKillCount,
            //    result.m_EliteMonsterKillCount,
            //    result.m_BossMonsterKillCount);
            //RenderAPI.SetTextLan(m_View.m_txt_reward_kill, m_LanTdChapterResultKillReward, result.m_KillRewardGold);
            //RenderAPI.SetTextLan(m_View.m_txt_reward_clear, m_LanTdChapterResultClearReward, result.m_ClearRewardGold);
            //RenderAPI.SetTextLan(m_View.m_txt_reward_total, m_LanTdChapterResultTotalReward, result.m_TotalRewardGold);
            //RenderAPI.SetTextLan(m_View.m_txt_time, m_LanTdChapterResultTime, FormatBattleTime(result.m_UseTime));
            //RenderAPI.SetText(m_View.m_txt_unlock, BuildUnlockText(result));

            RenderAPI.SetActive(m_View.m_btn_continue, true);
            RenderAPI.SetActive(m_View.m_imgRaise, true);
        }
        //设置结算几颗星星
        private void SetStar(BattleResultData result)
        {
            // 计算需要激活的数量（0~3）
            int activeCount = 0;
            EnsureResultVideoComponents();
            VideoPlayer videoPlayer = m_ResultVideoPlayer;
            string abName;
            if (result.m_BaseHealth >= t_globalBean.GetConfig(100307).t_int)
            {
                activeCount = 3;
                abName = "video/star3.mp4";
                AddCounter(3080, 1, () => { AudioManager.GetInstance().Play2D(106); });
                AddCounter(3240, 1, () => { AudioManager.GetInstance().Play2D(106); });
                AddCounter(3400, 1, () => { AudioManager.GetInstance().Play2D(106); });
            }
            else if (result.m_BaseHealth >= t_globalBean.GetConfig(100308).t_int)
            {
                activeCount = 2;
                abName = "video/star2.mp4";
                AddCounter(3080, 1, () => { AudioManager.GetInstance().Play2D(106); });
                AddCounter(3240, 1, () => { AudioManager.GetInstance().Play2D(106); });
            }
            else if (result.m_BaseHealth >= t_globalBean.GetConfig(100309).t_int)
            {
                activeCount = 1;
                abName = "video/star1.mp4";
                AddCounter(3080, 1, () => { AudioManager.GetInstance().Play2D(106); });
            }
            else
            {
                activeCount = 0;
                abName = "video/faill.mp4";
            }

            HideResultVideo();
            if (videoPlayer == null)
            {
                Debug.LogError("结算视频播放失败，raw_texture 上没有 VideoPlayer 组件。");
            }
            else
            {
                PlayResultVideo(videoPlayer, abName);
            }

            // 根据数量设置每个元素的激活状态
            m_View.m_0.gameObject.SetActive(activeCount >= 1);
            m_View.m_1.gameObject.SetActive(activeCount >= 2);
            m_View.m_2.gameObject.SetActive(activeCount >= 3);
        }

        private void EnsureResultVideoComponents()
        {
            if (m_View == null || m_View.m_raw_texture == null)
            {
                return;
            }

            m_ResultVideoPlayer = m_View.m_raw_texture.GetComponent<VideoPlayer>();
            m_ResultVideoImage = m_View.m_raw_texture.GetComponent<RawImage>();
        }

        private void PlayResultVideo(VideoPlayer videoPlayer, string abName)
        {
            //LCL.UIRes.LoadPrefabAsync(
            //    typeof(VideoClip),
            //    abName,
            //    Tool.GetAssetName(abName),
            //    (resData, userData) =>
            //    {
            //        Debug.LogError("加载视频成功");
            //        VideoClip clip = resData.m_Obj as VideoClip;
            //        if(clip==null)
            //        {
            //            Debug.LogError("resData.m_Obj 不是 VideoClip，实际类型：" + (resData.m_Obj?.GetType().Name ?? "null"));
            //            return;
            //        }
            //        videoPlayer.clip = clip;
            //        videoPlayer.isLooping = false;
            //        videoPlayer.GetComponent<RawImage>().enabled = true;
            //        videoPlayer.Play();
            //    },
            //    (load_index, hr) =>
            //    {
            //        Debug.LogError($"加载视频失败，错误码：{hr}");
            //    },
            //    null  // userData 可选
            //);

            videoPlayer.source = VideoSource.Url;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
            videoPlayer.skipOnDrop = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            if (videoPlayer.targetTexture == null && m_ResultVideoImage != null)
            {
                videoPlayer.targetTexture = m_ResultVideoImage.texture as RenderTexture;
            }
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            //videoPlayer.SetTargetAudioSource(0, m_AudioSource);
            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
            videoPlayer.prepareCompleted += OnPrepareCompleted;
            videoPlayer.started -= OnVideoStarted;
            videoPlayer.started += OnVideoStarted;
            videoPlayer.errorReceived -= OnVideoErrorReceived;
            videoPlayer.errorReceived += OnVideoErrorReceived;
            videoPlayer.frameReady -= OnVideoFrameReady;
            videoPlayer.frameReady += OnVideoFrameReady;
            videoPlayer.loopPointReached -= OnLoopPointReached;
            videoPlayer.loopPointReached += OnLoopPointReached;

            var path = Path.Combine(Application.persistentDataPath, abName);

            if (Application.isEditor)
            {
                path = Path.Combine(MonoTool.GetBuildStreamingAssetsPath() + "common", abName);
            }

            videoPlayer.url = path;
            m_IsWaitingResultVideoFirstFrame = true;
            videoPlayer.Prepare();
        }

        private void HideResultVideo()
        {
            if (m_ResultVideoImage == null)
            {
                return;
            }

            m_ResultVideoImage.enabled = false;
        }

        private void ShowResultVideo(VideoPlayer source)
        {
            if (source != m_ResultVideoPlayer || !m_IsWaitingResultVideoFirstFrame)
            {
                return;
            }

            m_IsWaitingResultVideoFirstFrame = false;
            if (m_ResultVideoImage != null)
            {
                m_ResultVideoImage.enabled = true;
            }
        }

        private void StopResultVideo()
        {
            if (m_ResultVideoPlayer != null)
            {
                m_ResultVideoPlayer.prepareCompleted -= OnPrepareCompleted;
                m_ResultVideoPlayer.started -= OnVideoStarted;
                m_ResultVideoPlayer.errorReceived -= OnVideoErrorReceived;
                m_ResultVideoPlayer.frameReady -= OnVideoFrameReady;
                m_ResultVideoPlayer.loopPointReached -= OnLoopPointReached;
                m_ResultVideoPlayer.Stop();
            }

            m_IsWaitingResultVideoFirstFrame = false;
            HideResultVideo();
        }

        private void OnLoopPointReached(VideoPlayer source)
        {
        }

        private void OnVideoFrameReady(VideoPlayer source, long frameIdx)
        {
            ShowResultVideo(source);
        }

        private void OnVideoErrorReceived(VideoPlayer source, string message)
        {
            if (source != m_ResultVideoPlayer)
            {
                return;
            }

            Debug.LogError("结算视频播放失败：" + message);
            m_IsWaitingResultVideoFirstFrame = false;
            HideResultVideo();
        }

        private void OnVideoStarted(VideoPlayer source)
        {
            ShowResultVideo(source);
        }

        private void OnPrepareCompleted(VideoPlayer source)
        {
            if (source != m_ResultVideoPlayer)
            {
                return;
            }

            source.Play();
        }
    }
}
