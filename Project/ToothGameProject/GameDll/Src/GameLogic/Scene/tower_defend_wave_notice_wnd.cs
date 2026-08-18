using System;
using TMPro;
using UnityEngine;
using GameDll;
using UnityUI;
using DG.Tweening;

namespace GameHot
{
    public class tower_defend_wave_notice_model : WindowModel
    {
        public int m_LastWave;
        public int m_LastMonsterPoolStageId;
        public bool m_LastUpgradeReady;
        public bool m_LastUpgradeCountdown;
        public bool m_LastUpgradeActive;
        public bool m_LastBossSkillCasting;
        public bool m_UseAnimatedUpgradeCountdownNotice;
        public string m_CurrentNotice = string.Empty;
        public long m_TxtNoticeHideTimerId = -1;
        public long m_UpgradeNoticeHideTimerId = -1;
        public long m_CountdownTimerId = -1;
        public int m_NextCountdownValue = 0;

        public override void Clear()
        {
            m_LastWave = 0;
            m_LastMonsterPoolStageId = 0;
            m_LastUpgradeReady = false;
            m_LastUpgradeCountdown = false;
            m_LastUpgradeActive = false;
            m_LastBossSkillCasting = false;
            m_UseAnimatedUpgradeCountdownNotice = false;
            m_CurrentNotice = string.Empty;
            m_TxtNoticeHideTimerId = -1;
            m_UpgradeNoticeHideTimerId = -1;
            m_CountdownTimerId = -1;
            m_NextCountdownValue = 0;
        }
    }

    public class tower_defend_wave_notice_wnd : WindowBase
    {
        private const string m_LanTdNoticeLevelUp = "td_notice_level_up";
        private const string m_LanTdNoticeElite = "td_notice_elite";
        private const string m_LanTdNoticeBoss = "td_notice_boss";
        private const string m_LanTdNoticeTheme = "td_notice_theme";
        private const string m_LanTdNoticeWave = "td_notice_wave";
        private const string m_LanTdNoticeBossSkill = "td_notice_boss_skill";
        private const string m_LanTdNoticeUpgradeAttack = "td_notice_upgrade_attack";
        private const int m_UpgradeChallengeCountdownAudioId = 102;
        private v_tower_defend_wave_notice_wnd m_View;
        private Animation m_UpgradeCountdownAnimation;
        private TMP_Text m_UpgradeCountdownStartText;
        private const float ShowWaveNoticeAniTime = 0.3f;
        private const float ContinuationShowWaveNoticeAniTime = 1.5f;
        public override void OnClassConstructed()
        {
            base.OnClassConstructed();
            m_Layer = WindowLayer.Top;
            __CustomUIPrefabDir = UIPrefabDirs.battle;
            __SetWindowCacheTime(0);
            __CreateModel(new tower_defend_wave_notice_model());
        }

        protected override void OnInitComponent()
        {
            m_View = new v_tower_defend_wave_notice_wnd();
            m_View.InitComponent(__GetWindowObj());
            CacheUpgradeChallengeNoticeComponents();
            SetNoticeVisible(false);
            SetUpgradeChallengeNoticeVisible(false);
        }

        protected override void OnOpen()
        {
            RenderEvent.Event.OnTowerDefendBattleStateChanged += OnTowerDefendBattleStateChanged;
        }

        protected override void OnClose()
        {
            RenderEvent.Event.OnTowerDefendBattleStateChanged -= OnTowerDefendBattleStateChanged;
            RemoveTxtNoticeHideTimer();
            RemoveUpgradeNoticeHideTimer();
            RemoveCountdownTimer();
            SetNoticeVisible(false);
            SetUpgradeChallengeNoticeVisible(false);
        }

        private TowerDefendBattle ReadCurrentBattle()
        {
            return BattleManager.GetBattle() as TowerDefendBattle;
        }

        private void OnTowerDefendBattleStateChanged()
        {
            var battle = ReadCurrentBattle();
            if (battle == null || !battle.ReadIsBattleRunning() || battle.ReadCurrentWave() <= 0)
            {
                return;
            }

            var model = GetModel<tower_defend_wave_notice_model>();
            if (battle.ReadIsBossSkillCasting() && !model.m_LastBossSkillCasting)
            {
                model.m_LastBossSkillCasting = true;
                model.m_CurrentNotice = BuildBossSkillNotice(battle);
                ShowNotice(model.m_CurrentNotice, (int)(battle.ReadBossSkillCastingLeft() * 1000));
                return;
            }

            if (!battle.ReadIsBossSkillCasting())
            {
                model.m_LastBossSkillCasting = false;
            }

            if (battle.ReadIsUpgradeChallengeCountdown() && !model.m_LastUpgradeCountdown)
            {
                model.m_LastUpgradeCountdown = true;
                model.m_LastMonsterPoolStageId = battle.ReadCurrentMonsterPoolStageId();
                StartUpgradeChallengeCountdownNotice();
                return;
            }

            if (!battle.ReadIsUpgradeChallengeCountdown())
            {
                model.m_LastUpgradeCountdown = false;
                RemoveCountdownTimer();
                if (!battle.ReadIsUpgradeChallengeActive())
                {
                    model.m_UseAnimatedUpgradeCountdownNotice = false;
                    RemoveUpgradeNoticeHideTimer();
                    SetUpgradeChallengeNoticeVisible(false);
                }
            }

            if (battle.ReadIsUpgradeChallengeActive() && !model.m_LastUpgradeActive)
            {
                model.m_LastUpgradeActive = true;
                model.m_LastMonsterPoolStageId = battle.ReadCurrentMonsterPoolStageId();
                if (model.m_UseAnimatedUpgradeCountdownNotice)
                {
                    model.m_UseAnimatedUpgradeCountdownNotice = false;
                    RemoveUpgradeNoticeHideTimer();
                    SetUpgradeChallengeNoticeVisible(false);
                    return;
                }
                model.m_CurrentNotice = RenderAPI.GetTextByLanId(m_LanTdNoticeUpgradeAttack);
                ShowNotice(model.m_CurrentNotice);
                return;
            }

            if (!battle.ReadIsUpgradeChallengeActive())
            {
                model.m_LastUpgradeActive = false;
            }

            if (model.m_LastMonsterPoolStageId > 0 && model.m_LastMonsterPoolStageId != battle.ReadCurrentMonsterPoolStageId())
            {
                model.m_LastMonsterPoolStageId = battle.ReadCurrentMonsterPoolStageId();
                model.m_CurrentNotice = RenderAPI.GetTextByLanId(m_LanTdNoticeTheme);
                ShowNotice(model.m_CurrentNotice);
                return;
            }

            if (battle.ReadIsUpgradeChallengeReady() && !model.m_LastUpgradeReady)
            {
                model.m_LastUpgradeReady = true;
                model.m_LastMonsterPoolStageId = battle.ReadCurrentMonsterPoolStageId();
                model.m_CurrentNotice = RenderAPI.GetTextByLanId(m_LanTdNoticeLevelUp);
                ShowNotice(model.m_CurrentNotice);
                return;
            }

            if (model.m_LastWave == battle.ReadCurrentWave())
            {
                return;
            }
            bool isBossOrElite = false;
            model.m_LastWave = battle.ReadCurrentWave();
            model.m_LastMonsterPoolStageId = battle.ReadCurrentMonsterPoolStageId();
            model.m_LastUpgradeReady = battle.ReadIsUpgradeChallengeReady();
            model.m_CurrentNotice = BuildWaveNotice(battle,out isBossOrElite);
            ShowWaveNotice(model.m_CurrentNotice, isBossOrElite);
        }

        private string BuildWaveNotice(TowerDefendBattle battle,out bool isBossOrElite)
        {
            if (battle != null && battle.ReadIsBossWave())
            {
                isBossOrElite = true;
                return RenderAPI.GetTextByLanId(m_LanTdNoticeBoss);
            }

            if (battle != null && battle.ReadIsEliteWave())
            {
                isBossOrElite = true;
                return RenderAPI.GetTextByLanId(m_LanTdNoticeElite);
            }
            isBossOrElite = false;
            return battle?.ReadCurrentWave().ToString();
        }

        private string BuildBossSkillNotice(TowerDefendBattle battle)
        {
            return RenderAPI.GetTextByLanId(m_LanTdNoticeBossSkill);
        }

        private void StartUpgradeChallengeCountdownNotice()
        {
            var model = GetModel<tower_defend_wave_notice_model>();
            var battle = ReadCurrentBattle();
            float countdownDuration = ResolveUpgradeChallengeCountdownDuration(battle);
            AudioManager.GetInstance().Play2D(m_UpgradeChallengeCountdownAudioId);
            RemoveCountdownTimer();
            model.m_UseAnimatedUpgradeCountdownNotice = TryPlayUpgradeChallengeCountdownNotice(countdownDuration);
            if (model.m_UseAnimatedUpgradeCountdownNotice)
            {
                return;
            }

            int countdownCount = 3;
            int beatDurationMs = Mathf.Max(200, Mathf.RoundToInt(Mathf.Max(0.3f, countdownDuration) * 1000f / countdownCount));
            model.m_NextCountdownValue = countdownCount - 1;
            ShowNotice(countdownCount.ToString(), beatDurationMs);
            if (model.m_NextCountdownValue <= 0)
            {
                return;
            }

            model.m_CountdownTimerId = AddCounter(beatDurationMs, model.m_NextCountdownValue, () =>
            {
                if (!IsInitializedView())
                {
                    return;
                }

                var currentModel = GetModel<tower_defend_wave_notice_model>();
                if (currentModel.m_NextCountdownValue <= 0)
                {
                    return;
                }

                ShowNotice(currentModel.m_NextCountdownValue.ToString(), beatDurationMs);
                currentModel.m_NextCountdownValue--;
            }, 0, () =>
            {
                var currentModel = GetModel<tower_defend_wave_notice_model>();
                currentModel.m_CountdownTimerId = -1;
                currentModel.m_NextCountdownValue = 0;
            });
        }

        private void ShowNotice(string notice, int durationMs = 1500)
        {
            if (!IsInitializedView())
            {
                return;
            }

            RenderAPI.SetText(m_View.m_txt_notice, notice);
            SetUpgradeChallengeNoticeVisible(false);
            SetNoticeVisible(true);
            StartTxtNoticeHideTimer(durationMs);
        }
        private void ShowWaveNotice(string notice,bool isBossOrElite)
        {
            if (!IsInitializedView())
            {
                return;
            }
            if (isBossOrElite)
            {
                AudioManager.GetInstance().Play2D(8);
                RenderAPI.SetText(m_View.m_boss_txt, notice);
                StartWaveNoticeAni(m_View.m_bossNoticeBg, m_View.m_boss_txt);
            }
            else
            {
                AudioManager.GetInstance().Play2D(9);
                RenderAPI.SetText(m_View.m_wave_txt, notice);
                StartWaveNoticeAni(m_View.m_waveBg, m_View.m_wave_txt);
            }
            RenderAPI.SetActive(m_View.m_wave, !isBossOrElite);
            RenderAPI.SetActive(m_View.m_bossNotice, isBossOrElite);
            SetUpgradeChallengeNoticeVisible(false);
        }
        private void StartWaveNoticeAni(LUIImage noticeBg,LUITextMesh notice_txt)
        {
            var canvasGroup = noticeBg.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, ShowWaveNoticeAniTime);

            var recordLocalPos = notice_txt.transform.localPosition;
            notice_txt.transform.localPosition = new Vector2(notice_txt.transform.localPosition.x+Screen.width / 1.5f, notice_txt.transform.localPosition.y);
            notice_txt.transform.DOLocalMove(recordLocalPos, ShowWaveNoticeAniTime);
            AddCounter((int)(ContinuationShowWaveNoticeAniTime * 1000), 1, () =>
            {
                canvasGroup.DOFade(0, ShowWaveNoticeAniTime);
                notice_txt.transform.DOLocalMoveX(recordLocalPos.x - Screen.width / 1.5f, ShowWaveNoticeAniTime).OnComplete(() =>
                {
                    RenderAPI.SetActive(m_View.m_wave, false);
                    RenderAPI.SetActive(m_View.m_bossNotice, false);
                    notice_txt.transform.localPosition = recordLocalPos;
                });
            });
        }

        private void SetNoticeVisible(bool visible)
        {
            if (m_View != null && m_View.m_txt_notice != null)
            {
                RenderAPI.SetActive(m_View.m_txt_notice, visible);
            }
        }

        private void SetUpgradeChallengeNoticeVisible(bool visible)
        {
            if (m_View != null && m_View.m_Notice != null)
            {
                RenderAPI.SetActive(m_View.m_Notice, visible);
            }
        }

        private void CacheUpgradeChallengeNoticeComponents()
        {
            if (m_View == null || m_View.m_Notice == null)
            {
                return;
            }

            if (m_UpgradeCountdownAnimation == null)
            {
                m_UpgradeCountdownAnimation = m_View.m_Notice.GetComponent<Animation>();
            }

            if (m_UpgradeCountdownStartText == null)
            {
                var startNode = m_View.m_Notice.transform.Find("txt_start");
                if (startNode != null)
                {
                    m_UpgradeCountdownStartText = startNode.GetComponent<TMP_Text>();
                }
            }
        }

        private float ResolveUpgradeChallengeCountdownDuration(TowerDefendBattle battle)
        {
            CacheUpgradeChallengeNoticeComponents();
            float countdownDuration = TowerDefendBattle.m_UpgradeChallengeCountdown;
            if (m_UpgradeCountdownAnimation != null &&
                m_UpgradeCountdownAnimation.clip != null &&
                m_UpgradeCountdownAnimation.clip.length > 0.01f)
            {
                countdownDuration = m_UpgradeCountdownAnimation.clip.length;
            }

            if (battle != null)
            {
                battle.SetUpgradeChallengeCountdownDuration(countdownDuration);
            }

            return countdownDuration;
        }

        private bool TryPlayUpgradeChallengeCountdownNotice(float countdownDuration)
        {
            if (!IsInitializedView())
            {
                return false;
            }

            CacheUpgradeChallengeNoticeComponents();
            if (m_View == null || m_View.m_Notice == null || m_UpgradeCountdownAnimation == null)
            {
                return false;
            }

            if (m_UpgradeCountdownStartText != null)
            {
                RenderAPI.SetText(m_UpgradeCountdownStartText, RenderAPI.GetTextByLanId(m_LanTdNoticeUpgradeAttack));
            }

            SetNoticeVisible(false);
            SetUpgradeChallengeNoticeVisible(true);

            if (m_UpgradeCountdownAnimation.clip != null)
            {
                string clipName = m_UpgradeCountdownAnimation.clip.name;
                m_UpgradeCountdownAnimation.Stop();
                if (!string.IsNullOrEmpty(clipName))
                {
                    var animationState = m_UpgradeCountdownAnimation[clipName];
                    if (animationState != null)
                    {
                        animationState.time = 0f;
                    }

                    m_UpgradeCountdownAnimation.Play(clipName);
                }
                else
                {
                    m_UpgradeCountdownAnimation.Play();
                }
            }
            else
            {
                m_UpgradeCountdownAnimation.Stop();
                m_UpgradeCountdownAnimation.Play();
            }

            StartUpgradeNoticeHideTimer(Mathf.RoundToInt(Mathf.Max(0.3f, countdownDuration) * 1000f));
            return true;
        }

        private void StartTxtNoticeHideTimer(int durationMs)
        {
            RemoveTxtNoticeHideTimer();

            var model = GetModel<tower_defend_wave_notice_model>();
            durationMs = Mathf.Max(200, durationMs);
            model.m_TxtNoticeHideTimerId = AddCounter(durationMs, 1, null, 0, () =>
            {
                SetNoticeVisible(false);
                model.m_TxtNoticeHideTimerId = -1;
            });
        }

        private void StartUpgradeNoticeHideTimer(int durationMs)
        {
            RemoveUpgradeNoticeHideTimer();

            var model = GetModel<tower_defend_wave_notice_model>();
            durationMs = Mathf.Max(200, durationMs);
            model.m_UpgradeNoticeHideTimerId = AddCounter(durationMs, 1, null, 0, () =>
            {
                SetUpgradeChallengeNoticeVisible(false);
                model.m_UpgradeNoticeHideTimerId = -1;
            });
        }

        private void RemoveTxtNoticeHideTimer()
        {
            var model = GetModel<tower_defend_wave_notice_model>();
            if (model.m_TxtNoticeHideTimerId >= 0)
            {
                RemoveCounter(model.m_TxtNoticeHideTimerId);
                model.m_TxtNoticeHideTimerId = -1;
            }
        }

        private void RemoveUpgradeNoticeHideTimer()
        {
            var model = GetModel<tower_defend_wave_notice_model>();
            if (model.m_UpgradeNoticeHideTimerId >= 0)
            {
                RemoveCounter(model.m_UpgradeNoticeHideTimerId);
                model.m_UpgradeNoticeHideTimerId = -1;
            }
        }

        private void RemoveCountdownTimer()
        {
            var model = GetModel<tower_defend_wave_notice_model>();
            if (model.m_CountdownTimerId >= 0)
            {
                RemoveCounter(model.m_CountdownTimerId);
                model.m_CountdownTimerId = -1;
            }

            model.m_NextCountdownValue = 0;
        }
    }
}
