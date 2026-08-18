using GameDll;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDll
{
    public enum TowerDefendBattleState
    {
        WaitingChessMan,
        NpcSpeakIntro,
        PreStart,
        FreeGame,
        UpgradeChallengeCountdown,
        UpgradeChallenge,
        ShowWinFailed,
        ShowReward,
        GameOver
    }

    public class TowerDefendBattleProcess :IBattleProgress
    {
        private const int m_ChapterOneStarClearGold = 50;
        private const int m_ChapterTwoStarClearGold = 150;
        private const int m_ChapterThreeStarClearGold = 300;
        // 结算表现属于塔防流程本身，胜负确认后先停留 1 秒，再通知场景打开结算界面。
        private const float m_BattleResultTransitionSeconds = 1.0f;
        private float m_StageTimeUsed = 0;
        private float m_DisplayStageTimeUsed = 0;
        private float m_FreeGameTimeUsed = 0;
        private float m_DisplayFreeGameTimeUsed = 0;
        private BattleResultData m_PendingBattleResult = null;
        private bool m_HasPendingBattleResult = false;
        public override float ReadStageTime()
        {
            return m_DisplayFreeGameTimeUsed;
        }
        public float ReadUpgradeChallengeCountdownLeft()
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (m_State != TowerDefendBattleState.UpgradeChallengeCountdown ||
                battle == null ||
                battle.ReadPhase() != BattlePhase.ChallengeCountdown)
            {
                return 0;
            }

            return Math.Max(0, battle.ReadUpgradeChallengeCountdownDuration() - m_DisplayStageTimeUsed);
        }
        public float ReadUpgradeChallengeLeft()
        {
            if (m_State != TowerDefendBattleState.UpgradeChallenge)
            {
                return 0;
            }

            return Math.Max(0, TowerDefendBattle.m_UpgradeChallengeDuration - m_DisplayStageTimeUsed);
        }
        public float ReadPrepareLeft()
        {
            if (m_State != TowerDefendBattleState.PreStart)
            {
                return 0;
            }

            return Math.Max(0, 5.0f - m_DisplayStageTimeUsed);
        }

        public bool GM_TryForceUpgradeChallengeFinish()
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return false;
            }

            switch (m_State)
            {
                case TowerDefendBattleState.UpgradeChallengeCountdown:
                case TowerDefendBattleState.UpgradeChallenge:
                    battle.GM_ForceFinishUpgradeChallenge();
                    break;
                default:
                    return false;
            }

            return true;
        }

        // 战斗相位、怪物表现和流程状态必须在同一个恢复入口内完成，
        // 避免场景层先切回普通战斗后，流程层错过 ChallengeFinish 的收尾时机。
        public void CompleteUpgradeChallengeRestore()
        {
            if (m_State != TowerDefendBattleState.UpgradeChallengeCountdown &&
                m_State != TowerDefendBattleState.UpgradeChallenge)
            {
                return;
            }

            m_State = TowerDefendBattleState.FreeGame;
            m_StageTimeUsed = 0;
            m_DisplayStageTimeUsed = 0;
            m_HudAccumulator = 0;
            RenderEvent.Event.OnFinishUpgradeChallenge();
        }

        public void FinishNpcSpeakIntro()
        {
            if (m_State != TowerDefendBattleState.NpcSpeakIntro)
            {
                return;
            }

            m_State = TowerDefendBattleState.FreeGame;
            m_StageTimeUsed = 0f;
            m_DisplayStageTimeUsed = 0f;
            NotifyTowerDefendRuntimeState(0, false, true, false);
        }

        private TowerDefendBattleState m_State = TowerDefendBattleState.WaitingChessMan;
        private float m_HudAccumulator = 0;
        // 1050 策划案要求：升级挑战阶段积分展示按 0.5 秒节奏刷新。
        private const float m_HudUpdateInterval = 0.5f;

        public override int GetState()
        {
            return (int)m_State;
        }
        public override void Init()
        {
            var battle = (TowerDefendBattle)BattleManager.GetBattle();
            m_GameTotalTime = t_globalBean.GetConfig(65).t_int;

            m_State = TowerDefendBattleState.WaitingChessMan;
            m_StageTimeUsed = 0;
            m_DisplayStageTimeUsed = 0;
            m_FreeGameTimeUsed = 0;
            m_DisplayFreeGameTimeUsed = 0;
            ClearPendingBattleResult();

        }
        public override void Destroy()
        {
            ClearPendingBattleResult();
        }


        public override void Update(float dt)
        {
            var battle = BattleManager.GetBattle();
            bool freezeDisplay = battle != null && battle.GM_IsPause();

            switch (m_State)
            {
                case TowerDefendBattleState.WaitingChessMan:
                    {
                        m_StageTimeUsed += dt;
                        if (!freezeDisplay) m_DisplayStageTimeUsed += dt;
                        NotifyTowerDefendRuntimeState(0, false, false, false);
                        NotifyHUDThrottled(0, false, false, false, 0, 0, dt);
                        if(m_StageTimeUsed >= 1.0f)
                        {
                            m_State = TowerDefendBattleState.NpcSpeakIntro;
                            m_StageTimeUsed = 0;
                            m_DisplayStageTimeUsed = 0;
                            NotifyTowerDefendRuntimeState(0, false, false, false);
                            var players = battle.ReadPlayers();
                            foreach (var kv in players)
                            {
                                var man = kv;
                                if (man.m_IsAI)
                                {
                                    man.m_Prepare = true;
                                }
                            }
                        }
                        break;
                    }
                case TowerDefendBattleState.NpcSpeakIntro:
                    {
                        m_StageTimeUsed += dt;
                        if (!freezeDisplay) m_DisplayStageTimeUsed += dt;
                        NotifyTowerDefendRuntimeState(0, false, false, false);
                        NotifyHUDThrottled(0, false, false, false, 0, 0, dt);
                        break;
                    }
                case TowerDefendBattleState.PreStart:
                    {
                        m_StageTimeUsed += dt;
                        if (!freezeDisplay) m_DisplayStageTimeUsed += dt;
                        var displayPrepareLeft = Math.Max(0, 5.0f - m_DisplayStageTimeUsed);
                        NotifyTowerDefendRuntimeState(displayPrepareLeft, true, false, false);
                        NotifyHUDThrottled(displayPrepareLeft, true, false, false, 0, 0, dt);
                        if(m_StageTimeUsed > 5.0f)
                        {
                            m_State = TowerDefendBattleState.FreeGame;
                            m_StageTimeUsed = 0;
                            m_DisplayStageTimeUsed = 0;
                            RenderEvent.Event.OnPreStart();
                        }
                        else
                        {
                            RenderEvent.Event.OnBattlePrepareTimeChanged(m_DisplayStageTimeUsed);
                        }
                        break;
                    }
                case TowerDefendBattleState.FreeGame:
                    {
                        m_StageTimeUsed += dt;
                        m_FreeGameTimeUsed += dt;
                        if (!freezeDisplay)
                        {
                            m_DisplayStageTimeUsed += dt;
                            m_DisplayFreeGameTimeUsed += dt;
                        }
                        var bt = battle as TowerDefendBattle;
                        if (bt != null && bt.CanStartUpgradeChallenge())
                        {
                            bt.StartUpgradeChallengeCountdown();
                            m_State = TowerDefendBattleState.UpgradeChallengeCountdown;
                            m_StageTimeUsed = 0;
                            m_DisplayStageTimeUsed = 0;
                            NotifyTowerDefendRuntimeState(
                                0,
                                false,
                                true,
                                false,
                                0,
                                0);
                            NotifyHUDThrottled(0, false, true, false, 0, 0, dt);
                            RenderEvent.Event.OnStartUpgradeChallenge();
                            break;
                        }

                        NotifyTowerDefendRuntimeState(0, false, true, false);
                        NotifyHUDThrottled(0, false, true, false, 0, 0, dt);
                        break;
                    }
                case TowerDefendBattleState.UpgradeChallengeCountdown:
                    {
                        var bt = battle as TowerDefendBattle;
                        bool isActualCountdown = bt != null && bt.ReadPhase() == BattlePhase.ChallengeCountdown;
                        float countdownDuration = bt != null
                            ? bt.ReadUpgradeChallengeCountdownDuration()
                            : TowerDefendBattle.m_UpgradeChallengeCountdown;
                        float displayLeft = 0f;
                        if (isActualCountdown)
                        {
                            m_StageTimeUsed += dt;
                            if (!freezeDisplay)
                            {
                                m_DisplayStageTimeUsed += dt;
                            }
                            displayLeft = Math.Max(0, countdownDuration - m_DisplayStageTimeUsed);
                        }
                        NotifyTowerDefendRuntimeState(0, false, true, false, displayLeft, 0);
                        NotifyHUDThrottled(0, false, true, false, displayLeft, 0, dt);
                        if (isActualCountdown && m_StageTimeUsed >= countdownDuration)
                        {
                            if (bt != null)
                            {
                                bt.EnterUpgradeChallenge();
                            }
                            m_State = TowerDefendBattleState.UpgradeChallenge;
                            m_StageTimeUsed = 0;
                            m_DisplayStageTimeUsed = 0;
                        }
                        break;
                    }
                case TowerDefendBattleState.UpgradeChallenge:
                    {
                        var bt = battle as TowerDefendBattle;
                        if (bt != null && bt.ReadPhase() == BattlePhase.ChallengeFinish)
                        {
                            // 场景恢复与棒棒糖表现全部结束后，TryRestoreMonsters 会同步完成流程收尾。
                            break;
                        }

                        m_StageTimeUsed += dt;
                        if (!freezeDisplay) m_DisplayStageTimeUsed += dt;
                        var displayLeft = Math.Max(0, TowerDefendBattle.m_UpgradeChallengeDuration - m_DisplayStageTimeUsed);
                        NotifyTowerDefendRuntimeState(0, false, true, false, 0, displayLeft);
                        NotifyHUDThrottled(0, false, true, false, 0, displayLeft, dt);
                        if (m_StageTimeUsed >= TowerDefendBattle.m_UpgradeChallengeDuration)
                        {
                            if (bt != null && bt.ReadPhase() == BattlePhase.ChallengeActive)
                            {
                                bt.ResolveUpgradeChallengeResult();
                            }
                        }
                        break;
                    }
                case TowerDefendBattleState.ShowWinFailed:
                    {
                        m_StageTimeUsed += dt;
                        if (!freezeDisplay)
                        {
                            m_DisplayStageTimeUsed += dt;
                        }
                        NotifyTowerDefendRuntimeState(0, false, false, true);
                        NotifyHUDThrottled(0, false, false, true, 0, 0, dt);
                        TryDispatchBattleResultAfterTransition();
                        break;
                    }
                case TowerDefendBattleState.ShowReward:
                    {
                        m_StageTimeUsed += dt;
                        if (!freezeDisplay)
                        {
                            m_DisplayStageTimeUsed += dt;
                        }
                        NotifyTowerDefendRuntimeState(0, false, false, true);
                        NotifyHUDThrottled(0, false, false, true, 0, 0, dt);
                        TryDispatchBattleResultAfterTransition();
                        break;
                    }
                case TowerDefendBattleState.GameOver:
                    {
                        NotifyTowerDefendRuntimeState(0, false, false, true);
                        NotifyHUDThrottled(0, false, false, true, 0, 0, dt);
                        break;
                    }
            }
        }
        public override float ReadGameLeftTime()
        {
            return GetGameTotalTime() - m_DisplayFreeGameTimeUsed;
        }
        public override void SetState(int state)
        {
            m_State = (TowerDefendBattleState)state;
        }
        public override BattleResultData OnFinishGame(FinishReason camp, object userData)
        {
            var isWin = camp == FinishReason.DefenseSucceeded;
            m_State = isWin ? TowerDefendBattleState.ShowReward : TowerDefendBattleState.ShowWinFailed;
            m_StageTimeUsed = 0f;
            m_DisplayStageTimeUsed = 0f;
            ClearPendingBattleResult();

            var rd = base.OnFinishGame(camp, userData);
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (isWin)
            {
                rd.m_FinishReason = camp;
                rd.m_WinGroup = GroupId.GuardGroupId;
                rd.m_Stage = BattleManager.GetBattle().GetStage();
                FillTowerDefendResultData(rd, battle, camp);
            }
            else
            {
                rd.m_FinishReason = camp;
                rd.m_WinGroup = GroupId.PushGroupId;
                rd.m_Stage = BattleManager.GetBattle().GetStage();
                FillTowerDefendResultData(rd, battle, camp);
            }
            m_PendingBattleResult = rd;
            m_HasPendingBattleResult = rd != null;
            if (!m_HasPendingBattleResult)
            {
                m_State = TowerDefendBattleState.GameOver;
            }
            return rd;

        }

        private void TryDispatchBattleResultAfterTransition()
        {
            if (!m_HasPendingBattleResult || m_StageTimeUsed < m_BattleResultTransitionSeconds)
            {
                return;
            }

            var result = m_PendingBattleResult;
            ClearPendingBattleResult();
            m_State = TowerDefendBattleState.GameOver;
            RenderEvent.Event.OnBattleResult(result);
        }

        private void ClearPendingBattleResult()
        {
            m_PendingBattleResult = null;
            m_HasPendingBattleResult = false;
        }

        private void FillTowerDefendResultData(BattleResultData result, TowerDefendBattle battle, FinishReason finishReason)
        {
            if (result == null || battle == null)
            {
                return;
            }

            result.m_GameMode = battle.ReadGameMode();
            result.m_StageName = TowerDefendStageConfigResolver.ResolveStageName(result.m_Stage, result.m_GameMode);
            result.m_UseTime = BattleManager.ReadStageTime();
            var battleData = battle.GetBattleData();
            result.m_IsLocal = battleData != null && battleData.m_IsLocal;
            result.m_BaseHealth = battle.ReadBaseHealth();
            result.m_BaseMaxHealth = battle.ReadBaseMaxHealth();
            result.m_CurrentWave = battle.ReadCurrentWave();
            result.m_BestProgressWave = ResolveBestProgressWave(result, battle);

            var stat = battle.GetBattleStat() as TowerDefendBattleStatistical;
            if (stat != null)
            {
                result.m_NormalMonsterKillCount = stat.ReadNormalMonsterDeadCount();
                result.m_EliteMonsterKillCount = stat.ReadEliteMonsterDeadCount();
                result.m_BossMonsterKillCount = stat.ReadBossMonsterDeadCount();
                result.m_KillRewardGold = stat.ReadMonsterKillGold();
                result.m_MonsterKillDetails = stat.BuildMonsterKillDetailResults();
            }

            if (result.m_GameMode == BattleGameMode.Chapter)
            {
                result.m_StarCount = ResolveChapterStarCount(result.m_BaseHealth, result.m_BaseMaxHealth);
                result.m_ClearRewardGold = ResolveChapterClearRewardGold(result);
            }

            result.m_TotalRewardGold = result.m_KillRewardGold + result.m_ClearRewardGold;
        }

        private int ResolveChapterStarCount(int baseHealth, int baseMaxHealth)
        {
            if (baseMaxHealth <= 0)
            {
                return 0;
            }

            var healthPercent = baseHealth * 100.0f / baseMaxHealth;
            if (healthPercent >= 80.0f)
            {
                return 3;
            }

            if (healthPercent >= 40.0f)
            {
                return 2;
            }

            if (baseHealth > 0)
            {
                return 1;
            }

            return 0;
        }

        private int ResolveChapterClearRewardGold(BattleResultData result)
        {
            if (result == null || result.m_WinGroup != GroupId.GuardGroupId)
            {
                return 0;
            }

            switch (result.m_StarCount)
            {
                case 3:
                    return m_ChapterThreeStarClearGold;
                case 2:
                    return m_ChapterTwoStarClearGold;
                case 1:
                    return m_ChapterOneStarClearGold;
                default:
                    return 0;
            }
        }

        private int ResolveBestProgressWave(BattleResultData result, TowerDefendBattle battle)
        {
            if (result == null || battle == null)
            {
                return 0;
            }

            if (result.m_GameMode == BattleGameMode.Endless)
            {
                if (result.m_WinGroup == GroupId.GuardGroupId)
                {
                    return battle.ReadCurrentWave();
                }

                return Math.Max(0, battle.ReadCurrentWave() - 1);
            }

            return battle.ReadCurrentWave();
        }

        /// <summary>
        /// NotifyTowerDefendRuntimeState: 派发给订阅者的实时战斗状态变化通知。
        /// 参数说明：
        /// - prepareLeftMs: 准备阶段剩余毫秒数（若非准备阶段传 0）。
        /// - isPreparePhase: 是否处于准备阶段。
        /// - isBattleRunning: 是否处于战斗进行态（FreeGame/UpgradeChallenge 等会为真）。
        /// - isFinished: 战斗是否已结束（用于 HUD 显示结算状态）。
        /// - upgradeChallengeCountdownLeftMs: 升级挑战倒计时剩余毫秒（若未倒计时则 0）。
        /// - upgradeChallengeLeftMs: 升级挑战进行中剩余毫秒（若未进行则 0）。
        /// 
        /// 此函数会立即调用 OnTowerDefendWaveStateChanged（波次简要信息）和
        /// OnTowerDefendBattleStateChanged（刷新通知），供不同的订阅者使用。
        /// </summary>
        private void NotifyTowerDefendRuntimeState(
            float prepareLeft,
            bool isPreparePhase,
            bool isBattleRunning,
            bool isFinished,
            float upgradeChallengeCountdownLeft = 0,
            float upgradeChallengeLeft = 0)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return;
            }

            RenderEvent.Event.OnTowerDefendWaveStateChanged(
                battle.ReadCurrentWave(),
                battle.ReadMaxWave(),
                battle.ReadAliveMonsterCount(),
                battle.ReadWaveWait());

            RenderEvent.Event.OnTowerDefendBattleStateChanged();
        }

        /// <summary>
        /// NotifyHUDThrottled: 用于节流向 HUD 推送运行态数据的辅助方法。
        /// 参数说明与 NotifyTowerDefendRuntimeState 基本一致，外加：
        /// - dt: 本次 Update 的时间增量（毫秒），用于累积判断是否达到节流阈值。
        /// 
        /// 当内部累积时间达到 m_HudUpdateIntervalMs 时，可按需派发一次节流后的刷新通知。
        /// </summary>
        private void NotifyHUDThrottled(
            float prepareLeft,
            bool isPreparePhase,
            bool isBattleRunning,
            bool isFinished,
            float upgradeChallengeCountdownLeft,
            float upgradeChallengeLeft,
            float dt)
        {
            m_HudAccumulator += Math.Max(0, dt);
            if (m_HudAccumulator < m_HudUpdateInterval)
            {
                return;
            }

            m_HudAccumulator %= m_HudUpdateInterval;
            // 旧的 HUD 节流快照入口已废弃，当前保留节流计时以便后续按需扩展。
        }
    }
}
