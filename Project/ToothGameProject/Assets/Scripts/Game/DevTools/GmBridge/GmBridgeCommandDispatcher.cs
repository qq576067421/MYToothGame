#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using MonoBean;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDll
{
    // GM 命令的唯一实现入口：编辑器 Play 模式与真机共用同一份，避免两边行为漂移。
    // 只做白名单委托注册，禁止按名字反射调用任意方法。
    public static class GmBridgeCommandDispatcher
    {
        public delegate GmBridgeCommandResult Handler(string argsJson);

        [Serializable]
        private sealed class BoolArgDto
        {
            public bool m_Enabled;
        }

        [Serializable]
        private sealed class PauseArgDto
        {
            public bool m_Pause;
        }

        [Serializable]
        private sealed class FrameRateArgDto
        {
            public int m_Fps;
        }

        [Serializable]
        private sealed class QualityArgDto
        {
            public int m_Level;
        }

        [Serializable]
        private sealed class WaveArgDto
        {
            public int m_Wave;
        }

        [Serializable]
        private sealed class FinishArgDto
        {
            public bool m_Success;
        }

        [Serializable]
        private sealed class StageArgDto
        {
            public int m_StageId;
        }

        [Serializable]
        private sealed class CountArgDto
        {
            public int m_Count;
        }

        private static readonly Dictionary<string, Handler> m_Handlers = CreateHandlers();

        public static bool ReadIsKnownCommand(string name)
        {
            return !string.IsNullOrEmpty(name) && m_Handlers.ContainsKey(name);
        }

        public static string ReadCommandNames()
        {
            var builder = new System.Text.StringBuilder();
            foreach (var name in m_Handlers.Keys)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(name);
            }

            return builder.ToString();
        }

        public static GmBridgeCommandResult Execute(string name, string argsJson)
        {
            if (string.IsNullOrEmpty(name))
            {
                return GmBridgeCommandResult.Fail("命令名为空");
            }

            if (!m_Handlers.TryGetValue(name, out Handler handler) || handler == null)
            {
                return GmBridgeCommandResult.Fail("未知命令：" + name);
            }

            try
            {
                return handler(argsJson) ?? GmBridgeCommandResult.Fail("命令没有返回结果：" + name);
            }
            catch (Exception exception)
            {
                Debug.LogError("[GM桥] 命令执行异常：" + name + " → " + exception.Message);
                return GmBridgeCommandResult.Fail("命令执行异常：" + exception.Message);
            }
        }

        public static string BuildStateJson()
        {
            var state = new GmBridgeStateDto
            {
                m_Scene = SceneManager.GetActiveScene().name,
                m_Battle = "None",
                m_GameMode = string.Empty,
                m_Fps = ReadFps(),
                m_Invincible = DamageCal.m_GM_Invincible,
                m_MonsterMoveEnabled = !State_Move.m_GM_DisableMove,
                m_AutoSkillEnabled = !TowerDefendBattleSpawer.m_GMDisableAutoSkill,
                m_TargetFrameRate = Application.targetFrameRate,
                m_QualityLevel = QualitySettings.GetQualityLevel(),
            };

            var battle = BattleManager.GetBattle();
            if (battle == null)
            {
                return JsonUtility.ToJson(state);
            }

            state.m_Paused = battle.GM_IsPause();
            var towerDefendBattle = battle as TowerDefendBattle;
            if (towerDefendBattle == null)
            {
                state.m_Battle = "Other";
                return JsonUtility.ToJson(state);
            }

            state.m_Battle = "TowerDefend";
            state.m_GameMode = towerDefendBattle.ReadGameMode().ToString();
            state.m_Wave = towerDefendBattle.ReadCurrentWave();
            state.m_TeamExpPercent = towerDefendBattle.ReadTeamExpPercent();
            state.m_ControlledSeatId = towerDefendBattle.ReadControlledSeatId();
            state.m_IsBattleRunning = towerDefendBattle.ReadIsBattleRunning();
            state.m_IsPreparePhase = towerDefendBattle.ReadIsPreparePhase();
            state.m_FrozenSeatCount = CountFrozenSeats(towerDefendBattle);
            return JsonUtility.ToJson(state);
        }

        private static Dictionary<string, Handler> CreateHandlers()
        {
            var handlers = new Dictionary<string, Handler>(StringComparer.Ordinal);
            handlers.Add("state.query", HandleStateQuery);
            handlers.Add("battle.pause", HandleSetPause);
            handlers.Add("cheat.invincible", HandleSetInvincible);
            handlers.Add("cheat.monster.move", HandleSetMonsterMove);
            handlers.Add("cheat.auto.skill", HandleSetAutoSkill);
            handlers.Add("cheat.freeze.all", HandleFreezeAllGuardHeroes);
            handlers.Add("app.frameRate", HandleSetFrameRate);
            handlers.Add("app.quality", HandleSetQuality);
            handlers.Add("battle.finish", HandleFinishBattle);
            handlers.Add("battle.wave.next", HandleSpawnNextWave);
            handlers.Add("battle.wave.set", HandleSetWave);
            handlers.Add("battle.boss.spawn", HandleSpawnBoss);
            handlers.Add("battle.challenge.exp.max", HandleChallengeExpMax);
            handlers.Add("battle.challenge.shatter", HandleChallengeShatter);
            handlers.Add("battle.challenge.finish", HandleChallengeFinish);
            handlers.Add("battle.base.break", HandleBaseBreak);
            handlers.Add("stage.enter", HandleEnterStage);
            handlers.Add("app.screenshot", HandleScreenshot);
            handlers.Add("log.tail", HandleLogTail);
            return handlers;
        }

        private static GmBridgeCommandResult HandleStateQuery(string argsJson)
        {
            return GmBridgeCommandResult.Ok("ok", BuildStateJson());
        }

        private static GmBridgeCommandResult HandleSetPause(string argsJson)
        {
            if (!TryParse(argsJson, out PauseArgDto args))
            {
                return GmBridgeCommandResult.Fail("参数解析失败：battle.pause 需要 {\"m_Pause\":true}");
            }

            var battle = BattleManager.GetBattle();
            if (battle == null)
            {
                return GmBridgeCommandResult.Fail("当前没有进行中的战斗");
            }

            battle.GM_SetPause(args.m_Pause);
            return GmBridgeCommandResult.Ok(args.m_Pause ? "战斗已暂停" : "战斗已恢复", BuildStateJson());
        }

        private static GmBridgeCommandResult HandleSetInvincible(string argsJson)
        {
            if (!TryParse(argsJson, out BoolArgDto args))
            {
                return GmBridgeCommandResult.Fail("参数解析失败：cheat.invincible 需要 {\"m_Enabled\":true}");
            }

            DamageCal.m_GM_Invincible = args.m_Enabled;
            return GmBridgeCommandResult.Ok(args.m_Enabled ? "怪物无敌已开启" : "怪物无敌已关闭");
        }

        private static GmBridgeCommandResult HandleSetMonsterMove(string argsJson)
        {
            if (!TryParse(argsJson, out BoolArgDto args))
            {
                return GmBridgeCommandResult.Fail("参数解析失败：cheat.monster.move 需要 {\"m_Enabled\":true}");
            }

            // 面板语义是“怪物可移动”，运行时字段是反过来的“禁止移动”。
            State_Move.m_GM_DisableMove = !args.m_Enabled;
            return GmBridgeCommandResult.Ok(args.m_Enabled ? "怪物恢复移动" : "怪物已停止移动");
        }

        private static GmBridgeCommandResult HandleSetAutoSkill(string argsJson)
        {
            if (!TryParse(argsJson, out BoolArgDto args))
            {
                return GmBridgeCommandResult.Fail("参数解析失败：cheat.auto.skill 需要 {\"m_Enabled\":true}");
            }

            // 同样取反：面板语义是“自动技能可用”。
            TowerDefendBattleSpawer.m_GMDisableAutoSkill = !args.m_Enabled;
            return GmBridgeCommandResult.Ok(args.m_Enabled ? "自动技能已开启" : "自动技能已禁用");
        }

        private static GmBridgeCommandResult HandleFreezeAllGuardHeroes(string argsJson)
        {
            var battle = BattleManager.GetBattle();
            if (battle == null)
            {
                return GmBridgeCommandResult.Fail("当前没有进行中的战斗");
            }

            var spawer = battle.ReadBattleSpawer();
            var heroes = spawer != null ? spawer.ReadHeroes() : null;
            if (heroes == null || heroes.Count <= 0)
            {
                return GmBridgeCommandResult.Fail("当前战斗没有守卫英雄");
            }

            int frozenCount = 0;
            for (int i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (!BattleManager.ReadIsEntityValide(hero))
                {
                    continue;
                }

                // 与子弹命中走同一条加 buff 链路，避免出现只有表现没有逻辑的冰冻。
                BattleManager.GetBattleTool().AddBuff(hero, hero, 923000, 0);
                frozenCount++;
            }

            return GmBridgeCommandResult.Ok("已冻结 " + frozenCount + " 名守卫英雄");
        }

        private static GmBridgeCommandResult HandleSetFrameRate(string argsJson)
        {
            if (!TryParse(argsJson, out FrameRateArgDto args) || args.m_Fps < -1)
            {
                return GmBridgeCommandResult.Fail("参数解析失败：app.frameRate 需要 {\"m_Fps\":60}，-1 表示不限制");
            }

            Application.targetFrameRate = args.m_Fps;
            return GmBridgeCommandResult.Ok("目标帧率已设置为 " + args.m_Fps);
        }

        private static GmBridgeCommandResult HandleSetQuality(string argsJson)
        {
            if (!TryParse(argsJson, out QualityArgDto args))
            {
                return GmBridgeCommandResult.Fail("参数解析失败：app.quality 需要 {\"m_Level\":0}");
            }

            int maxLevel = QualitySettings.names != null ? QualitySettings.names.Length - 1 : 0;
            int level = Mathf.Clamp(args.m_Level, 0, Mathf.Max(0, maxLevel));
            QualitySettings.SetQualityLevel(level);
            return GmBridgeCommandResult.Ok("画质档位已设置为 " + level);
        }

        private static GmBridgeCommandResult HandleFinishBattle(string argsJson)
        {
            if (!TryParse(argsJson, out FinishArgDto args))
            {
                return GmBridgeCommandResult.Fail("参数解析失败：battle.finish 需要 {\"m_Success\":true}");
            }

            var battle = BattleManager.GetBattle();
            if (battle == null)
            {
                return GmBridgeCommandResult.Fail("当前没有进行中的战斗");
            }

            var progress = battle.GetBattleProgress();
            if (progress == null)
            {
                return GmBridgeCommandResult.Fail("当前战斗缺少 BattleProgress");
            }

            if (args.m_Success)
            {
                progress.OnFinishGame(FinishReason.DefenseSucceeded, null);
                return GmBridgeCommandResult.Ok("已触发防守成功结算");
            }

            var towerDefendBattle = battle as TowerDefendBattle;
            if (towerDefendBattle != null)
            {
                towerDefendBattle.GM_ForceBaseHealthToZero();
            }

            progress.OnFinishGame(FinishReason.DefenseFailed, GroupId.PushGroupId);
            return GmBridgeCommandResult.Ok("已触发防守失败结算");
        }

        private static GmBridgeCommandResult HandleSpawnNextWave(string argsJson)
        {
            var towerDefendBattle = TryReadTowerDefendBattle(out GmBridgeCommandResult failResult);
            if (towerDefendBattle == null)
            {
                return failResult;
            }

            if (!towerDefendBattle.GM_TrySpawnNextWaveNow())
            {
                return GmBridgeCommandResult.Fail("当前没有可立即触发的下一波");
            }

            return GmBridgeCommandResult.Ok("已触发下一波", BuildStateJson());
        }

        private static GmBridgeCommandResult HandleSetWave(string argsJson)
        {
            if (!TryParse(argsJson, out WaveArgDto args) || args.m_Wave <= 0)
            {
                return GmBridgeCommandResult.Fail("参数解析失败：battle.wave.set 需要 {\"m_Wave\":3}");
            }

            var towerDefendBattle = TryReadTowerDefendBattle(out GmBridgeCommandResult failResult);
            if (towerDefendBattle == null)
            {
                return failResult;
            }

            if (towerDefendBattle.ReadGameMode() != BattleGameMode.Endless)
            {
                return GmBridgeCommandResult.Fail("当前不是无尽模式，无法直接设置波次");
            }

            if (!towerDefendBattle.GM_TrySetCurrentWave(args.m_Wave))
            {
                return GmBridgeCommandResult.Fail("设置波次失败：刷怪器处于暂停、挂起或已结束状态");
            }

            return GmBridgeCommandResult.Ok("当前波次已设置为 " + args.m_Wave, BuildStateJson());
        }

        private static GmBridgeCommandResult HandleSpawnBoss(string argsJson)
        {
            var towerDefendBattle = TryReadTowerDefendBattle(out GmBridgeCommandResult failResult);
            if (towerDefendBattle == null)
            {
                return failResult;
            }

            if (!towerDefendBattle.GM_TrySpawnBossNow())
            {
                return GmBridgeCommandResult.Fail("当前没有可直接刷出的 Boss");
            }

            return GmBridgeCommandResult.Ok("已刷出 Boss");
        }

        private static GmBridgeCommandResult HandleChallengeExpMax(string argsJson)
        {
            var towerDefendBattle = TryReadTowerDefendBattle(out GmBridgeCommandResult failResult);
            if (towerDefendBattle == null)
            {
                return failResult;
            }

            towerDefendBattle.GM_AddTeamExpToMax();
            return GmBridgeCommandResult.Ok("团队经验已补满", BuildStateJson());
        }

        private static GmBridgeCommandResult HandleChallengeShatter(string argsJson)
        {
            var towerDefendBattle = TryReadTowerDefendBattle(out GmBridgeCommandResult failResult);
            if (towerDefendBattle == null)
            {
                return failResult;
            }

            if (!towerDefendBattle.GM_TryForceUpgradeChallengeShatter())
            {
                return GmBridgeCommandResult.Fail("当前不在升级挑战倒计时或进行阶段");
            }

            return GmBridgeCommandResult.Ok("已触发棒棒糖炸开");
        }

        private static GmBridgeCommandResult HandleChallengeFinish(string argsJson)
        {
            var towerDefendBattle = TryReadTowerDefendBattle(out GmBridgeCommandResult failResult);
            if (towerDefendBattle == null)
            {
                return failResult;
            }

            towerDefendBattle.GM_ForceFinishUpgradeChallenge();
            return GmBridgeCommandResult.Ok("已强制结束升级挑战");
        }

        private static GmBridgeCommandResult HandleBaseBreak(string argsJson)
        {
            var towerDefendBattle = TryReadTowerDefendBattle(out GmBridgeCommandResult failResult);
            if (towerDefendBattle == null)
            {
                return failResult;
            }

            towerDefendBattle.GM_ForceBaseHealthToZero();
            return GmBridgeCommandResult.Ok("基地血量已置零");
        }

        private static GmBridgeCommandResult HandleEnterStage(string argsJson)
        {
            if (!TryParse(argsJson, out StageArgDto args) || args.m_StageId <= 0)
            {
                return GmBridgeCommandResult.Fail("参数解析失败：stage.enter 需要 {\"m_StageId\":1001}");
            }

            if (!TowerDefendStageConfigResolver.Exists(args.m_StageId, BattleGameMode.Chapter))
            {
                return GmBridgeCommandResult.Fail("关卡在章节模式中不存在：" + args.m_StageId);
            }

            var stageRoleCfg = t_tdStageGuardRoleBean.GetConfig(args.m_StageId, false);
            if (stageRoleCfg == null)
            {
                return GmBridgeCommandResult.Fail("关卡缺少防守角色映射配置：" + args.m_StageId);
            }

            var request = new BattleStartupRequest
            {
                m_BattleType = BattleType.TowerDefend,
                m_GameMode = BattleGameMode.Chapter,
                m_StageId = args.m_StageId,
                m_IsLocal = true,
                m_BaseMaxHealth = 10000,
                m_BaseHealth = 10000,
            };

            int playerCount = Mathf.Min(stageRoleCfg.t_guard_role_ids.Count, TowerDefendSeatLayout.DefaultPlayerCount);
            for (int i = 0; i < playerCount; i++)
            {
                int seatId = TowerDefendSeatLayout.GetStartupSeatIdByIndex(i, playerCount);
                if (seatId >= stageRoleCfg.t_guard_role_ids.Count)
                {
                    return GmBridgeCommandResult.Fail(string.Format("关卡 {0} 座位 {1} 超出角色映射范围", args.m_StageId, seatId));
                }

                long roleCfgId = stageRoleCfg.t_guard_role_ids[seatId];
                if (t_heroBean.GetConfig(roleCfgId, false) == null)
                {
                    return GmBridgeCommandResult.Fail("角色配置不存在：" + roleCfgId);
                }

                request.m_Players.Add(new BattleStartupPlayerData
                {
                    m_PlayerId = seatId + 1,
                    m_PlayerName = RenderAPI.GetTextByLanId("td_hud_default_player", seatId + 1),
                    m_RoleCfgId = roleCfgId,
                    m_RoleLevel = 1,
                    m_IsAI = false,
                    m_Group = GroupId.GuardGroupId,
                    m_SeatId = seatId,
                    m_HPPercent = 10000,
                    m_MagicPercent = 10000,
                });
            }

            if (!RenderEvent.Event.OnGmStartBattleRequest(request, out string error))
            {
                return GmBridgeCommandResult.Fail("发起进入关卡失败：" + error);
            }

            return GmBridgeCommandResult.Ok("已发起进入关卡 " + args.m_StageId + "（章节模式）");
        }

        private static GmBridgeCommandResult HandleScreenshot(string argsJson)
        {
            string fileName = "gm_shot_" + GmBridgeProtocol.ReadTimestampMs() + ".png";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            ScreenCapture.CaptureScreenshot(filePath);
            return GmBridgeCommandResult.Ok("已截图：" + filePath);
        }

        private static GmBridgeCommandResult HandleLogTail(string argsJson)
        {
            int count = 30;
            if (TryParse(argsJson, out CountArgDto args) && args.m_Count > 0)
            {
                count = Mathf.Clamp(args.m_Count, 1, 200);
            }

            return GmBridgeCommandResult.Ok(
                "ok",
                JsonUtility.ToJson(new GmBridgeLogTailDto { m_LogText = GmBridgeLogBuffer.ReadTailText(count) }));
        }

        private static TowerDefendBattle TryReadTowerDefendBattle(out GmBridgeCommandResult failResult)
        {
            failResult = null;
            var towerDefendBattle = BattleManager.GetBattle() as TowerDefendBattle;
            if (towerDefendBattle == null)
            {
                failResult = GmBridgeCommandResult.Fail("当前不是进行中的塔防战斗");
            }

            return towerDefendBattle;
        }

        private static bool TryParse<T>(string argsJson, out T value) where T : class
        {
            value = null;
            if (string.IsNullOrEmpty(argsJson))
            {
                return false;
            }

            try
            {
                value = JsonUtility.FromJson<T>(argsJson);
            }
            catch (Exception)
            {
                return false;
            }

            return value != null;
        }

        private static int CountFrozenSeats(TowerDefendBattle battle)
        {
            int frozenCount = 0;
            for (int seatId = 0; seatId < TowerDefendSeatLayout.MaxSupportedPlayerCount; seatId++)
            {
                var hero = battle.ReadGuardHeroBySeat(seatId);
                if (BattleManager.ReadIsEntityValide(hero) && hero.ReadIsFrozen())
                {
                    frozenCount++;
                }
            }

            return frozenCount;
        }

        private static int ReadFps()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0.0001f)
            {
                return 0;
            }

            return Mathf.RoundToInt(1f / deltaTime);
        }
    }
}
#endif
