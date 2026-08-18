using MonoBean;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace GameDll
{
    public sealed class TowerDefendStageConfigAdapter
    {
        public int StageId;
        public string StageName = string.Empty;
        public string ScenePath = string.Empty;
        public int FirstWaveDelayMs;
        public int WaveIntervalMs;
        public int WaveCount;
        public int RewardExp;
        public int RewardCoin;
        public int MonsterHpUp1 = 1000;
        public int MonsterHpUp2 = 1000;
        public int MonsterHpUp3 = 1000;
        public int MonsterHpUp4 = 1000;
        public BattleGameMode GameMode;
        public readonly List<ReadOnlyCollection<ReadOnlyCollection<long>>> ChapterWaveMonsterPools =
            new List<ReadOnlyCollection<ReadOnlyCollection<long>>>();

        public ReadOnlyCollection<ReadOnlyCollection<long>> EndlessMonsterPool;

        public int MonsterPoolId;
        public int MonsterPoolSourceStageId;
    }

    public static class TowerDefendStageConfigResolver
    {
        public static string GetModeDebugName(BattleGameMode gameMode)
        {
            switch (gameMode)
            {
                case BattleGameMode.Endless:
                    return "无尽模式";
                case BattleGameMode.Tutorial:
                    return "教程模式";
                case BattleGameMode.Chapter:
                    return "关卡模式";
                default:
                    return "未知模式";
            }
        }

        public static string GetConfigTableName(BattleGameMode gameMode)
        {
            return gameMode == BattleGameMode.Endless ? "t_endlessStageBean" : "t_chapterStageBean";
        }

        public static bool Exists(int stageId, BattleGameMode gameMode)
        {
            return Resolve(stageId, gameMode) != null;
        }

        public static string ResolveScenePath(int stageId, BattleGameMode gameMode)
        {
            var cfg = Resolve(stageId, gameMode);
            return cfg != null ? cfg.ScenePath : string.Empty;
        }

        public static string ResolveStageName(int stageId, BattleGameMode gameMode)
        {
            var cfg = Resolve(stageId, gameMode);
            return cfg != null ? cfg.StageName : string.Empty;
        }

        public static List<int> GetStageIds(BattleGameMode gameMode)
        {
            if (gameMode == BattleGameMode.Endless)
            {
                var keys = t_endlessStageBean.GetKeys();
                return keys != null ? new List<int>(keys) : new List<int>();
            }

            var chapterKeys = t_chapterStageBean.GetKeys();
            return chapterKeys != null ? new List<int>(chapterKeys) : new List<int>();
        }

        public static TowerDefendStageConfigAdapter Resolve(int stageId, BattleGameMode gameMode)
        {
            if (stageId <= 0)
            {
                return null;
            }

            if (gameMode == BattleGameMode.Endless)
            {
                var endlessCfg = t_endlessStageBean.GetConfig(stageId, false);
                if (endlessCfg == null)
                {
                    return null;
                }

                return new TowerDefendStageConfigAdapter
                {
                    StageId = endlessCfg.t_id,
                    StageName = endlessCfg.t_name,
                    ScenePath = endlessCfg.t_scene,
                    FirstWaveDelayMs = 0,
                    WaveIntervalMs = endlessCfg.t_wave_interval_ms,
                    WaveCount = 0,
                    RewardExp = endlessCfg.t_Rewards_Exp,
                    RewardCoin = endlessCfg.t_Rewards_Coin,
                    MonsterHpUp1 = endlessCfg.t_hp_up1,
                    MonsterHpUp2 = endlessCfg.t_hp_up2,
                    MonsterHpUp3 = endlessCfg.t_hp_up3,
                    MonsterHpUp4 = endlessCfg.t_hp_up4,
                    GameMode = BattleGameMode.Endless,
                    EndlessMonsterPool = endlessCfg.t_monster_ids,
                    MonsterPoolId = 0,
                    MonsterPoolSourceStageId = endlessCfg.t_id,
                };
            }

            var chapterCfg = t_chapterStageBean.GetConfig(stageId, false);
            if (chapterCfg == null)
            {
                return null;
            }

            var adapter = new TowerDefendStageConfigAdapter
            {
                StageId = chapterCfg.t_id,
                StageName = chapterCfg.t_name,
                ScenePath = chapterCfg.t_scene,
                FirstWaveDelayMs = chapterCfg.t_first_wave_delay_ms,
                WaveIntervalMs = chapterCfg.t_wave_interval_ms,
                WaveCount = 0,
                RewardExp = chapterCfg.t_Rewards_Exp,
                RewardCoin = chapterCfg.t_Rewards_Coin,
                MonsterHpUp1 = chapterCfg.t_hp_up1,
                MonsterHpUp2 = chapterCfg.t_hp_up2,
                MonsterHpUp3 = chapterCfg.t_hp_up3,
                MonsterHpUp4 = chapterCfg.t_hp_up4,
                GameMode = BattleGameMode.Chapter,
                MonsterPoolId = Mathf.Clamp((chapterCfg.t_id - 1) / 5 + 1, 1, 5),
                MonsterPoolSourceStageId = chapterCfg.t_id,
            };

            AppendChapterWavePools(
                adapter.ChapterWaveMonsterPools,
                chapterCfg.t_monster_ids0,
                chapterCfg.t_monster_ids1,
                chapterCfg.t_monster_ids2,
                chapterCfg.t_monster_ids3,
                chapterCfg.t_monster_ids4,
                chapterCfg.t_monster_ids5);
            adapter.WaveCount = adapter.ChapterWaveMonsterPools.Count;

            return adapter;
        }

        private static void AppendChapterWavePools(
            List<ReadOnlyCollection<ReadOnlyCollection<long>>> target,
            params ReadOnlyCollection<ReadOnlyCollection<long>>[] pools)
        {
            if (target == null || pools == null)
            {
                return;
            }

            for (int i = 0; i < pools.Length; i++)
            {
                var pool = pools[i];
                if (!HasConfiguredWavePool(pool))
                {
                    break;
                }

                target.Add(pool);
            }
        }

        private static bool HasConfiguredWavePool(ReadOnlyCollection<ReadOnlyCollection<long>> pool)
        {
            return pool != null && pool.Count > 0;
        }
    }
}
