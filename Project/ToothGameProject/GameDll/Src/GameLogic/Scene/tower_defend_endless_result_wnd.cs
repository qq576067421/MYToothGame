using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameDll;

namespace GameHot
{
    public class tower_defend_endless_result_model : WindowModel
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

    public class tower_defend_endless_result_wnd : WindowBase
    {
        private const string m_LanTdEndlessResultTitle = "td_result_endless_title";
        private const string m_LanTdEndlessResultWave = "td_result_endless_wave";
        private const string m_LanTdEndlessResultBestWave = "td_result_endless_best_wave";
        private const string m_LanTdEndlessResultKillSummary = "td_result_kill_summary";
        private const string m_LanTdEndlessResultKillDetailLine = "td_result_endless_kill_detail_line";
        private const string m_LanTdEndlessResultKillDetailEmpty = "td_result_endless_kill_detail_empty";
        private const string m_LanTdEndlessResultKillReward = "td_result_kill_reward";
        private const string m_LanTdEndlessResultTotalReward = "td_result_total_reward";
        private const string m_LanTdEndlessResultRankScore = "td_result_endless_rank_score";
        private const string m_LanTdEndlessResultRankPosition = "td_result_endless_rank_position";
        private const string m_LanTdEndlessResultRankPending = "td_result_endless_rank_pending";
        private const string m_LanTdEndlessResultRankSubmitted = "td_result_endless_rank_submitted";
        private const string m_LanTdEndlessResultRankFailed = "td_result_endless_rank_failed";
        private const int m_ResultWinAudioId = 306;
        private const int m_ResultLoseAudioId = 307;
        private const int m_ResultBgmAudioId = 308;
        private v_tower_defend_endless_result_wnd m_View;
        private Transform m_ContinueRaiseHint;
        private bool m_IsOpen;
        private int m_ResultAudioVersion;

        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Popup;
            __CustomUIPrefabDir = UIPrefabDirs.battle_result;
            __ParticipateCurrentActiveWindow = true;
            __SetWindowCacheTime(0);
            __CreateModel(new tower_defend_endless_result_model());
        }

        public void SetResult(BattleResultData result, Action onContinue, Action onReturn)
        {
            var model = GetModel<tower_defend_endless_result_model>();
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
            m_View = new v_tower_defend_endless_result_wnd();
            m_View.InitComponent(__GetWindowObj());
            m_ContinueRaiseHint = m_View.m_btn_continue != null
                ? m_View.m_btn_continue.transform.Find("imgRaise")
                : null;
            RenderAPI.AddButtonClick(m_View.m_btn_continue, OnClickContinue);
            RenderAPI.AddButtonClick(m_View.m_btn_return, OnClickReturn);
        }

        protected override void OnOpen()
        {
            m_IsOpen = true;
            RefreshView();
            PlayResultAudio();
        }

        protected override void OnClose()
        {
            m_IsOpen = false;
            m_ResultAudioVersion++;
        }

        private void PlayResultAudio()
        {
            var model = GetModel<tower_defend_endless_result_model>();
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
            var model = GetModel<tower_defend_endless_result_model>();
            if (model == null || model.m_OnContinue == null)
            {
                return;
            }

            model.m_OnContinue.Invoke();
            CloseWithLoadingDelayIfNeeded();
        }

        public bool TryContinueByGesture()
        {
            var model = GetModel<tower_defend_endless_result_model>();
            if (model == null || model.m_OnContinue == null)
            {
                return false;
            }

            OnClickContinue();
            return true;
        }

        public bool TryReturnByRemote()
        {
            var model = GetModel<tower_defend_endless_result_model>();
            if (model == null || model.m_OnReturn == null)
            {
                return false;
            }

            OnClickReturn();
            return true;
        }

        private void OnClickReturn()
        {
            var model = GetModel<tower_defend_endless_result_model>();
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
            var model = GetModel<tower_defend_endless_result_model>();
            var result = model.m_Result;
            if (result == null)
            {
                return;
            }

            RenderAPI.SetTextLan(m_View.m_txt_title, m_LanTdEndlessResultTitle);
            RenderAPI.SetTextLan(m_View.m_txt_wave, m_LanTdEndlessResultWave, result.m_BestProgressWave);
            RenderAPI.SetTextLan(m_View.m_txt_best_wave, m_LanTdEndlessResultBestWave, result.m_BestEndlessWave);
            RenderAPI.SetTextLan(
                m_View.m_txt_kill_summary,
                m_LanTdEndlessResultKillSummary,
                result.m_NormalMonsterKillCount,
                result.m_EliteMonsterKillCount,
                result.m_BossMonsterKillCount);
            RenderAPI.SetText(m_View.m_txt_kill_detail, BuildKillDetailText(result));
            RenderAPI.SetTextLan(m_View.m_txt_reward_kill, m_LanTdEndlessResultKillReward, result.m_KillRewardGold);
            RenderAPI.SetTextLan(m_View.m_txt_reward_total, m_LanTdEndlessResultTotalReward, result.m_TotalRewardGold);
            RenderAPI.SetText(m_View.m_txt_rank_status, BuildRankStatusText(result));
            RenderAPI.SetActive(m_View.m_btn_continue.gameObject, true);
            RenderAPI.SetActive(m_ContinueRaiseHint, true);
        }

        private string BuildKillDetailText(BattleResultData result)
        {
            if (result == null || result.m_MonsterKillDetails == null || result.m_MonsterKillDetails.Count == 0)
            {
                return RenderAPI.GetTextByLanId(m_LanTdEndlessResultKillDetailEmpty);
            }

            var builder = new System.Text.StringBuilder();
            int count = result.m_MonsterKillDetails.Count;
            for (int i = 0; i < count; i++)
            {
                var detail = result.m_MonsterKillDetails[i];
                if (detail == null || detail.m_KillCount <= 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                var displayName = string.IsNullOrEmpty(detail.m_Name) ? ("怪物 " + detail.m_ConfigId) : detail.m_Name;
                builder.Append(RenderAPI.GetTextByLanId(
                    m_LanTdEndlessResultKillDetailLine,
                    displayName,
                    detail.m_KillCount));
            }

            return builder.Length > 0
                ? builder.ToString()
                : RenderAPI.GetTextByLanId(m_LanTdEndlessResultKillDetailEmpty);
        }

        private string BuildRankStatusText(BattleResultData result)
        {
            var submission = result != null ? result.m_LeaderboardSubmission : null;
            if (submission == null)
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder();
            if (submission.m_Score > 0)
            {
                builder.Append(RenderAPI.GetTextByLanId(m_LanTdEndlessResultRankScore, submission.m_Score));
            }

            if (submission.m_Rank > 0)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(RenderAPI.GetTextByLanId(m_LanTdEndlessResultRankPosition, submission.m_Rank));
            }

            string stateText;
            if (!string.IsNullOrEmpty(submission.m_StatusText))
            {
                stateText = submission.m_StatusText;
            }
            else
            {
                switch (submission.m_State)
                {
                    case TowerDefendLeaderboardSubmissionState.Submitted:
                        stateText = RenderAPI.GetTextByLanId(m_LanTdEndlessResultRankSubmitted);
                        break;
                    case TowerDefendLeaderboardSubmissionState.Failed:
                        stateText = RenderAPI.GetTextByLanId(m_LanTdEndlessResultRankFailed);
                        break;
                    case TowerDefendLeaderboardSubmissionState.PendingSdk:
                        stateText = RenderAPI.GetTextByLanId(m_LanTdEndlessResultRankPending);
                        break;
                    default:
                        stateText = string.Empty;
                        break;
                }
            }

            if (!string.IsNullOrEmpty(stateText))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(stateText);
            }

            return builder.ToString();
        }

    }
}
