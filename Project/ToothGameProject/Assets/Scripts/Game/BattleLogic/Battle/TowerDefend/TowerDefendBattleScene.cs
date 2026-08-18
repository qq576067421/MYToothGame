using GameDll;
using LCL;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{
    public enum ESceneState
    {
        Init,
        LoadScene,
        LoadingScene,
        LoadedScene,
        PrepareGame,
        StartGame,
        Loop,
        Result,
        Error
    }
    public enum BattleGameMode
    {
        None = 0,
        Chapter = 1,
        Endless = 2,
        Tutorial = 3,
    }
    public class BattleStartupPlayerData
    {
        public long m_PlayerId;
        public string m_PlayerName = string.Empty;
        public long m_RoleCfgId;
        public int m_RoleLevel;
        public bool m_IsAI;
        public GroupId m_Group;
        public int m_SeatId;
        public int m_HPPercent;
        public int m_MagicPercent;

        public BattleStartupPlayerData Clone()
        {
            return new BattleStartupPlayerData
            {
                m_PlayerId = m_PlayerId,
                m_PlayerName = m_PlayerName,
                m_RoleCfgId = m_RoleCfgId,
                m_RoleLevel = m_RoleLevel,
                m_IsAI = m_IsAI,
                m_Group = m_Group,
                m_SeatId = m_SeatId,
                m_HPPercent = m_HPPercent,
                m_MagicPercent = m_MagicPercent,
            };
        }

        public Packet_BattlePlayer ToBattlePlayer()
        {
            return new Packet_BattlePlayer
            {
                m_ID = m_PlayerId,
                m_Name = m_PlayerName,
                m_RoleCfgId = m_RoleCfgId,
                m_IsAI = m_IsAI ? 1 : 0,
                m_role_level = m_RoleLevel,
                m_Group = (int)m_Group,
                m_SeatId = m_SeatId,
                m_HPPercent = m_HPPercent,
                m_MagicPercent = m_MagicPercent,
            };
        }
    }
    public class BattleStartupRequest
    {
        private const int m_MaxSupportedBattleSeatCount = 4;
        private const string m_LanTdModeChapter = "td_mode_chapter";
        private const string m_LanTdModeEndless = "td_mode_endless";
        private const string m_LanTdModeTutorial = "td_mode_tutorial";
        private const string m_LanTdLobbyEntry = "td_lobby_entry";
        private const string m_LanTdErrorPrepareSdkBindingMissing = "td_error_prepare_sdk_binding_missing";

        public BattleType m_BattleType;
        public BattleGameMode m_GameMode;
        public int m_StageId;
        public bool m_IsLocal;
        public int m_BaseMaxHealth;
        public int m_BaseHealth;
        public List<BattleStartupPlayerData> m_Players = new List<BattleStartupPlayerData>();
        public int[] m_SdkSlotIndicesBySeat = new int[] { -1, -1, -1, -1 };
        public int m_PreparePlayerCount = TowerDefendSeatLayout.DefaultPlayerCount;
        public long[] m_SelectedRoleCfgIdsBySeat = new long[] { 0, 0, 0, 0 };

        public int GetPlayerCount()
        {
            return m_Players != null ? m_Players.Count : 0;
        }

        public int ReadPreparePlayerCount()
        {
            return TowerDefendSeatLayout.NormalizePlayerCount(m_PreparePlayerCount);
        }

        public void SetPreparePlayerCount(int playerCount)
        {
            m_PreparePlayerCount = TowerDefendSeatLayout.NormalizePlayerCount(playerCount);
            EnsureSelectedRoleStorage();
        }

        public void SetSelectedRoleCfgIdForSeat(int seatId, long roleCfgId)
        {
            if (!IsValidSeatId(seatId))
            {
                return;
            }

            EnsureSelectedRoleStorage();
            m_SelectedRoleCfgIdsBySeat[seatId] = roleCfgId;
        }

        public bool TryGetSelectedRoleCfgIdForSeat(int seatId, out long roleCfgId)
        {
            roleCfgId = 0;
            if (!IsValidSeatId(seatId))
            {
                return false;
            }

            EnsureSelectedRoleStorage();
            roleCfgId = m_SelectedRoleCfgIdsBySeat[seatId];
            return roleCfgId > 0;
        }

        // 真机准备界面会把“战斗座位 -> SDK 槽位”的对应关系写进请求，供大厅和战斗共用。
        public void ClearSdkSlotBindings()
        {
            EnsureSdkSlotBindingStorage();
            for (int i = 0; i < m_SdkSlotIndicesBySeat.Length; i++)
            {
                m_SdkSlotIndicesBySeat[i] = -1;
            }
        }

        public void SetSdkSlotIndexForSeat(int seatId, int sdkSlotIndex)
        {
            if (!IsValidSeatId(seatId))
            {
                return;
            }

            EnsureSdkSlotBindingStorage();
            m_SdkSlotIndicesBySeat[seatId] = sdkSlotIndex;
        }

        public bool TryGetSdkSlotIndexForSeat(int seatId, out int sdkSlotIndex)
        {
            sdkSlotIndex = -1;
            if (!IsValidSeatId(seatId))
            {
                return false;
            }

            EnsureSdkSlotBindingStorage();
            sdkSlotIndex = m_SdkSlotIndicesBySeat[seatId];
            return sdkSlotIndex >= 0;
        }

        // 真机绑定关系必须显式存在，不能再回退到“战斗座位等于 SDK 槽位”的旧假设。
        public bool TryValidateSdkSlotBindings(out string error)
        {
            return TryValidateSdkSlotBindings(m_MaxSupportedBattleSeatCount, out error);
        }

        public bool TryValidateSdkSlotBindings(int requiredSeatCount, out string error)
        {
            error = string.Empty;
            EnsureSdkSlotBindingStorage();
            requiredSeatCount = TowerDefendSeatLayout.NormalizePlayerCount(requiredSeatCount);

            HashSet<int> usedSdkSlotIndices = new HashSet<int>();
            for (int seatId = 0; seatId < requiredSeatCount; seatId++)
            {
                int sdkSlotIndex;
                if (!TryGetSdkSlotIndexForSeat(seatId, out sdkSlotIndex))
                {
                    error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareSdkBindingMissing);
                    return false;
                }

                if (!usedSdkSlotIndices.Add(sdkSlotIndex))
                {
                    error = RenderAPI.GetTextByLanId(m_LanTdErrorPrepareSdkBindingMissing);
                    return false;
                }
            }

            return true;
        }

        public string GetEntrySummary()
        {
            return RenderAPI.GetTextByLanId(
                m_LanTdLobbyEntry,
                GetModeDisplayName(),
                m_StageId,
                GetPlayerCount());
        }

        public string GetPreparationSummary()
        {
            var builder = new StringBuilder();
            builder.Append(GetEntrySummary());

            if (m_Players != null)
            {
                for (int i = 0; i < m_Players.Count; i++)
                {
                    var player = m_Players[i];
                    if (player == null)
                    {
                        continue;
                    }

                    var heroCfg = player.m_RoleCfgId > 0 ? t_heroBean.GetConfig(player.m_RoleCfgId, false) : null;
                    var roleName = heroCfg != null && !string.IsNullOrEmpty(heroCfg.t_name)
                        ? heroCfg.t_name
                        : "角色" + (player.m_SeatId + 1);
                    builder.AppendLine();
                    builder.AppendFormat("P{0} {1}", player.m_SeatId + 1, roleName);
                }
            }

            return builder.ToString();
        }

        public BattleStartupRequest CloneRequest()
        {
            var clone = new BattleStartupRequest();
            clone.m_BattleType = m_BattleType;
            clone.m_GameMode = m_GameMode;
            clone.m_StageId = m_StageId;
            clone.m_IsLocal = m_IsLocal;
            clone.m_BaseMaxHealth = m_BaseMaxHealth;
            clone.m_BaseHealth = m_BaseHealth;
            clone.m_PreparePlayerCount = ReadPreparePlayerCount();
            clone.ClearSdkSlotBindings();
            EnsureSdkSlotBindingStorage();
            for (int i = 0; i < Mathf.Min(m_SdkSlotIndicesBySeat.Length, clone.m_SdkSlotIndicesBySeat.Length); i++)
            {
                clone.m_SdkSlotIndicesBySeat[i] = m_SdkSlotIndicesBySeat[i];
            }
            clone.EnsureSelectedRoleStorage();
            EnsureSelectedRoleStorage();
            for (int i = 0; i < Mathf.Min(m_SelectedRoleCfgIdsBySeat.Length, clone.m_SelectedRoleCfgIdsBySeat.Length); i++)
            {
                clone.m_SelectedRoleCfgIdsBySeat[i] = m_SelectedRoleCfgIdsBySeat[i];
            }

            if (m_Players != null)
            {
                foreach (var player in m_Players)
                {
                    clone.m_Players.Add(player != null ? player.Clone() : null);
                }
            }

            return clone;
        }

        public bool TryValidate(out string error)
        {
            error = string.Empty;
            if (m_BattleType == BattleType.None)
            {
                error = "战斗启动请求缺少战斗类型。";
                return false;
            }

            if (m_GameMode == BattleGameMode.None)
            {
                error = "战斗启动请求缺少游戏模式。";
                return false;
            }

            if (m_StageId <= 0 || !TowerDefendStageConfigResolver.Exists(m_StageId, m_GameMode))
            {
                error = "战斗启动请求缺少有效的关卡配置。";
                return false;
            }

            if (m_BaseMaxHealth <= 0 || m_BaseHealth < 0 || m_BaseHealth > m_BaseMaxHealth)
            {
                error = "战斗启动请求中的基地血量配置无效。";
                return false;
            }

            if (m_Players == null || m_Players.Count <= 0 || m_Players.Count > 4)
            {
                error = "战斗启动请求缺少有效的玩家列表。";
                return false;
            }

            var usedSeatIds = new HashSet<int>();
            for (int i = 0; i < m_Players.Count; i++)
            {
                var player = m_Players[i];
                if (player == null)
                {
                    error = "战斗启动请求中存在空玩家数据。";
                    return false;
                }

                if (player.m_PlayerId <= 0)
                {
                    error = "战斗启动请求中的玩家ID无效。";
                    return false;
                }

                if (player.m_RoleCfgId <= 0 || t_heroBean.GetConfig(player.m_RoleCfgId, false) == null)
                {
                    error = "战斗启动请求中的角色配置无效。";
                    return false;
                }

                if (player.m_SeatId < 0 || player.m_SeatId >= 4 || usedSeatIds.Contains(player.m_SeatId))
                {
                    error = "战斗启动请求中的玩家站位无效或重复。";
                    return false;
                }

                if (player.m_Group != GroupId.GuardGroupId)
                {
                    error = "战斗启动请求中的玩家阵营无效。";
                    return false;
                }

                if (player.m_RoleLevel <= 0)
                {
                    error = "战斗启动请求中的玩家等级无效。";
                    return false;
                }

                usedSeatIds.Add(player.m_SeatId);
            }

            return true;
        }

        public bool TryCreateBattleData(out BattleData battleData, out string error)
        {
            battleData = null;
            if (!TryValidate(out error))
            {
                return false;
            }

            battleData = new BattleData();
            battleData.m_Stage = m_StageId;
            battleData.m_GameMode = m_GameMode;
            battleData.m_IsLocal = m_IsLocal;
            battleData.m_BaseMaxHealth = m_BaseMaxHealth;
            battleData.m_BaseHealth = m_BaseHealth;
            battleData.m_RoomSizeType = GetPlayerCount();
            battleData.m_FightId = DateTime.UtcNow.Ticks;
            battleData.m_Seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);

            if (m_Players != null)
            {
                for (int i = 0; i < m_Players.Count; i++)
                {
                    var player = m_Players[i];
                    battleData.m_Players.Add(player != null ? player.ToBattlePlayer() : null);
                }
            }

            return true;
        }

        private string GetModeDisplayName()
        {
            switch (m_GameMode)
            {
                case BattleGameMode.Endless:
                    return RenderAPI.GetTextByLanId(m_LanTdModeEndless);
                case BattleGameMode.Tutorial:
                    return RenderAPI.GetTextByLanId(m_LanTdModeTutorial);
                case BattleGameMode.Chapter:
                default:
                    return RenderAPI.GetTextByLanId(m_LanTdModeChapter);
            }
        }

        private static int Clamp(int value, int minValue, int maxValue)
        {
            if (value < minValue)
            {
                return minValue;
            }

            if (value > maxValue)
            {
                return maxValue;
            }

            return value;
        }

        private void EnsureSdkSlotBindingStorage()
        {
            if (m_SdkSlotIndicesBySeat != null && m_SdkSlotIndicesBySeat.Length == m_MaxSupportedBattleSeatCount)
            {
                return;
            }

            var oldBindings = m_SdkSlotIndicesBySeat;
            m_SdkSlotIndicesBySeat = new int[m_MaxSupportedBattleSeatCount];
            for (int i = 0; i < m_SdkSlotIndicesBySeat.Length; i++)
            {
                m_SdkSlotIndicesBySeat[i] = -1;
            }

            if (oldBindings == null)
            {
                return;
            }

            int copyCount = Mathf.Min(oldBindings.Length, m_SdkSlotIndicesBySeat.Length);
            for (int i = 0; i < copyCount; i++)
            {
                m_SdkSlotIndicesBySeat[i] = oldBindings[i];
            }
        }

        private void EnsureSelectedRoleStorage()
        {
            if (m_SelectedRoleCfgIdsBySeat != null && m_SelectedRoleCfgIdsBySeat.Length == m_MaxSupportedBattleSeatCount)
            {
                return;
            }

            var oldRoleIds = m_SelectedRoleCfgIdsBySeat;
            m_SelectedRoleCfgIdsBySeat = new long[m_MaxSupportedBattleSeatCount];
            if (oldRoleIds == null)
            {
                return;
            }

            int copyCount = Mathf.Min(oldRoleIds.Length, m_SelectedRoleCfgIdsBySeat.Length);
            for (int i = 0; i < copyCount; i++)
            {
                m_SelectedRoleCfgIdsBySeat[i] = oldRoleIds[i];
            }
        }

        private static bool IsValidSeatId(int seatId)
        {
            return seatId >= 0 && seatId < m_MaxSupportedBattleSeatCount;
        }
    }
    public class Packet_BattlePlayer
    {
        public long m_ID;
        public string m_Name;
        public long m_RoleCfgId;
        public int m_IsAI;
        public int m_role_level = 1;
        public List<long> m_Skills = new List<long>();
        public List<long> m_Equips = new List<long>();
        public long m_BigWeaponCfgId;
        public int m_BigWeaponLevel;
        public int m_Group = (int)GroupId.GuardGroupId;
        public int m_SeatId;
        public int m_HPPercent = 10000;
        public int m_MagicPercent = 10000;
    }
    public class LevelInputData
    {
        public BattleType m_BattleType;
        public int m_Stage;
        public BattleStartupRequest m_StartRequest;
        public BattleData m_BattleData;
    }
    public class BattleData
    {
        public int m_ClientVersion;
        public int m_RoomSizeType;
        public int m_Stage;
        public BattleGameMode m_GameMode = BattleGameMode.Chapter;
        public bool m_IsLocal = true;
        public int m_BaseMaxHealth = 0;
        public int m_BaseHealth = 0;
        public int m_WorldId;
        public long m_FightId;
        public uint m_Seed;
        public List<Packet_BattlePlayer> m_Players = new List<Packet_BattlePlayer>();
        public byte m_Snapshot;
        public byte m_Record;
        public byte m_SnapshotUp;
        public byte m_RecordUp;

        public BattleData GetNormalBattleData()
        {
            return this;
        }

        public int GetPlayerCount()
        {
            return m_Players != null ? m_Players.Count : 0;
        }
    }
    public class TowerDefendMonsterPathPointData
    {
        public Vector3 m_SpawnPoint = Vector3.zero;
        public Vector3 m_EndPoint = Vector3.zero;
        public bool m_HasEndPoint = false;
    }
    public class TowerDefendScenePointData
    {
        public readonly List<Vector3> m_MonsterSpawnPoints = new List<Vector3>();
        public readonly List<TowerDefendMonsterPathPointData> m_MonsterPathPoints =
            new List<TowerDefendMonsterPathPointData>();
        public readonly List<Vector3> m_GuardHeroSpawnPoints = new List<Vector3>();
        public Vector3 m_BasePoint = Vector3.zero;
        public Vector3 m_UpgradeChallengeFoot = Vector3.zero;
        public float m_BaseReachRadius = 1.5f;
        public Transform m_NpcSpeakPoint;
    }
    // 1050 策划案当前只保留两种正式结算语义：
    // 1) 防守成功：怪物全部刷完并被清空
    // 2) 防守失败：怪物到达终点导致基地血量归零
    public enum FinishReason
    {
        DefenseFailed = 0,
        DefenseSucceeded = 1,
    }
    public enum GroupId
    {
        AnyGroupId = -1,
        GuardGroupId = 1,
        PushGroupId = 0,
        WildGroupId = 99,
        NeutralGroup = 100,
        TrapGroupId = 999,
        TraitorpGroupId = -1
    }
    public class TowerDefendBattleScene : IBattleScene
    {
        private const string m_SpawnPointPrefix = "td_spawn_";
        private const string m_GuardPointPrefix = "td_guard_";
        private const string m_BasePointName = "td_base";
        private const string m_SpawnEndPointName = "td_spawn_end";
        private const string m_BoneInputRootName = "tower_defend_bone_input";
        private const string m_EnvironmentRootName = "----environment----";
        private const string m_EnvironmentRotationParamName = "IsRotation";
        private const string m_UpgradeChallengeCameraRotationStateName = "camera_rot";
        private const string m_UpgradeChallengeCameraDefaultStateName = "camera_defalt";
        private const string m_ScenePointDocHint =
            "请查看 Docs/塔防手工Prefab与场景点位清单.md -> \"3. 场景任务：塔防关卡点位命名\"，" +
            "以及 Docs/1050武装到牙齿_正式开发基线.md -> \"10. 场景点位占位约定\"。";

        protected UScene m_Scene = null;
        protected ESceneState m_SceneState = ESceneState.Init;
        protected GameObject m_LevelRoot;
        private BattleResultData m_PendingResult;
        private bool m_HasPendingResult;
        private TowerDefendScenePointData m_ScenePointData;
        private object m_PauseWindow;

        public enum ScenePhase
        {
            Idle,
            EntryDelay,
            Showing,
            Restoring
        }
        private ScenePhase m_ScenePhase = ScenePhase.Idle;
        private float m_ScenePhaseTimer = 0f;

        private Transform m_UpgradeChallengePosition;
        private bool m_WasChallengeActive = false;
        private bool m_HasReportedUpgradeChallengeCameraReady = false;
        private Animator m_EnvironmentAnimator;
        private Animator m_UpgradeChallengeCameraAnimator;
        private bool m_HasAppliedEnvironmentRotationState = false;
        private bool m_IsEnvironmentRotationEnabled = false;
        private bool m_HasAppliedUpgradeChallengeCameraRotationState = false;
        private bool m_IsUpgradeChallengeCameraRotationEnabled = false;
        private bool m_HasCachedUpgradeChallengeCameraAnimatorEnabled = false;
        private bool m_DefaultUpgradeChallengeCameraAnimatorEnabled = false;
        private readonly TowerDefendAimAssistLine[] m_AimAssistLines = new TowerDefendAimAssistLine[TowerDefendSeatLayout.MaxSupportedPlayerCount];
        private BattleBoneParseData m_BattleBoneParseData;
        private BoneFrameSourceResolver m_BoneFrameSource;
        private TowerDefendBoneInputDriver m_BoneInputDriver;
        private BoneDebugSkeletonOverlay m_BoneDebugSkeletonOverlay;
        private BattleSkeletonVisualSuppressor m_BattleSkeletonVisualSuppressor;
        private string m_LastBoneSourceName;
        private static readonly int m_EnvironmentRotationParamHash = Animator.StringToHash(m_EnvironmentRotationParamName);

        public override void Init(LevelInputData levelDataInfo)
        {
            m_LevelInputData = levelDataInfo;
            if (m_LevelInputData.m_BattleData == null)
            {
                m_LevelInputData.m_BattleData = new BattleData();
                m_LevelInputData.m_BattleData.m_Stage = levelDataInfo.m_Stage;
            }
            m_PendingResult = null;
            m_HasPendingResult = false;

            m_SceneState = ESceneState.Init;
            RenderEvent.Event.OnGameResult += OnGameResult;
            RenderEvent.Event.OnGameFightAgain += OnGameFightAgain;
            RenderEvent.Event.OnBattleResult += OnBattleResult;
            RenderEvent.Event.OnTowerDefendPauseRequest += OnPauseRequest;
            RenderEvent.Event.OnTowerDefendBattlePauseStateRequest += OnBattlePauseStateRequest;
            RenderEvent.Event.OnTowerDefendBattlePauseStateQuery += IsBattlePaused;

            m_Stage = levelDataInfo.m_Stage;
            m_SceneType = levelDataInfo.m_BattleType;
            BattleManager.Init(m_LevelInputData.m_BattleData, m_SceneType);
            LogBattleSceneEntry();

        }

        private void LogBattleSceneEntry()
        {
            var battleData = m_LevelInputData != null ? m_LevelInputData.m_BattleData : null;
            var gameMode = battleData != null ? battleData.m_GameMode : BattleGameMode.Chapter;
            var stageCfg = TowerDefendStageConfigResolver.Resolve(m_Stage, gameMode);
            Debug.Log(string.Format(
                "[塔防入战] 来源=战斗场景，模式={0}({1})，关卡={2}，配置表={3}，场景={4}，玩家数={5}",
                TowerDefendStageConfigResolver.GetModeDebugName(gameMode),
                (int)gameMode,
                m_Stage,
                TowerDefendStageConfigResolver.GetConfigTableName(gameMode),
                stageCfg != null ? stageCfg.ScenePath : "未找到",
                battleData != null ? battleData.GetPlayerCount() : 0));
        }



        public override void Update(float dt)
        {
            switch (m_SceneState)
            {
                case ESceneState.Init:
                    {
                        m_SceneState = ESceneState.LoadScene;
                        break;
                    }
                case ESceneState.LoadScene:
                    {
                        LoadScene();
                        m_SceneState = ESceneState.LoadingScene;
                        break;
                    }
                case ESceneState.LoadingScene:
                    {
                        OnLoadingScene();
                        break;
                    }
                case ESceneState.LoadedScene:
                    {
                ParseScene();

                m_SceneState = ESceneState.PrepareGame;

                //GameDll.BattleManager.GetBattle().OnLoadMap();
                OnLoadMap();

                SetPostEffect();
                RefreshGuardPlatformVisibility(BattleManager.GetBattle() as TowerDefendBattle);

                UDebug.Log("加载场景完毕，进入准备阶段");

                        break;
                    }
                case ESceneState.PrepareGame:
                    {
                        break;
                    }
                case ESceneState.StartGame:
                    {

                        m_SceneState = ESceneState.Loop;
                        break;
                    }
                case ESceneState.Loop:
                    {
                        OnGameUpdate(dt);
                        break;
                    }
                case ESceneState.Result:
                    {
                        break;
                    }
                case ESceneState.Error:
                    {
                        break;
                    }
            }
        }
        protected override void OnLoadMap()
        {
            base.OnLoadMap();
            string scenePointError;
            if (!TryResolveScenePointData(out m_ScenePointData, out scenePointError))
            {
                Debug.LogError(scenePointError);
                m_SceneState = ESceneState.Error;
                return;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle != null)
            {
                battle.ConfigureScenePointData(m_ScenePointData);
                battle.OnLoadMap();
            }

            // 当前直接使用 battle 目录下已经接通的 HUD 窗口类。
            RenderEvent.Event.OnTowerDefendBattleHudOpenRequest();
            EnsureBoneInputRuntime();
            //m_SceneState = ESceneState.StartGame;
        }

        private void RefreshGuardPlatformVisibility(TowerDefendBattle battle)
        {
            if (m_LevelRoot == null)
            {
                return;
            }

            var helpers = m_LevelRoot.GetComponentsInChildren<PositionHelper>(true);
            if (helpers == null || helpers.Length <= 0)
            {
                return;
            }

            var guardHelpers = new PositionHelper[4];
            var guardPoints = new Vector3[4];
            int resolvedCount = 0;
            for (int i = 0; i < guardHelpers.Length; i++)
            {
                guardHelpers[i] = ResolveNamedPointHelper(helpers, m_GuardPointPrefix + (i + 1));
                if (guardHelpers[i] != null)
                {
                    guardHelpers[i].gameObject.SetActive(false);
                    if (guardHelpers[i].transform != null)
                    {
                        guardPoints[i] = guardHelpers[i].transform.position;
                        resolvedCount++;
                    }
                }
            }

            if (battle == null)
            {
                return;
            }

            var spawer = battle.ReadBattleSpawer();
            var guardHeroes = spawer != null ? spawer.ReadGuardHeroes() : null;
            if (guardHeroes == null || guardHeroes.Count <= 0)
            {
                return;
            }

            // 守卫台子要与英雄站位保持同一套居中布局：按玩家数把台子整体移到几何中心附近，
            // 并只显示对应数量的台子，其余台子隐藏。这样少人局台子始终贴在角色脚下，且整体居中。
            int playerCount = guardHeroes.Count;
            int slotCount = Mathf.Max(1, resolvedCount);
            var slotPoints = new List<Vector3>(slotCount);
            var slotHelpers = new List<PositionHelper>(slotCount);
            for (int i = 0; i < guardHelpers.Length; i++)
            {
                if (guardHelpers[i] != null)
                {
                    slotPoints.Add(guardPoints[i]);
                    slotHelpers.Add(guardHelpers[i]);
                }
            }

            int effectiveCount = Mathf.Min(playerCount, slotHelpers.Count);
            for (int i = 0; i < slotHelpers.Count; i++)
            {
                var helper = slotHelpers[i];
                if (helper == null || helper.gameObject == null || helper.transform == null)
                {
                    continue;
                }

                if (i < effectiveCount)
                {
                    helper.transform.position = TowerDefendGuardLayout.ResolveCenteredPosition(slotPoints, playerCount, i);
                    helper.gameObject.SetActive(true);
                }
                else
                {
                    helper.gameObject.SetActive(false);
                }
            }
        }

        public override bool IsLoaded()
        {
            return m_SceneState == ESceneState.PrepareGame;
        }

        public override bool ReadIsBattleStartLoadingReady()
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            return battle == null || battle.ReadIsBattleStartLoadingReady();
        }


        public override void SetSceneStatus(int status)
        {
            m_SceneState = (ESceneState)status;
        }
        protected virtual void OnGameUpdate(float dt)
        {
            BattleManager.Update(dt);
            UpdateBoneInput();
            BattleManager.UpdateRender(dt);

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            SyncUpgradeChallengeTargetVisualParent(battle);
            UpdateScenePhase(battle, dt);
            UpdateUpgradeChallengeCamera(dt);
            UpdateAimAssistLines();
        }

        private void SyncUpgradeChallengeTargetVisualParent(TowerDefendBattle battle)
        {
            if (battle == null || m_LevelRoot == null)
            {
                return;
            }

            var targetEntity = battle.ReadUpgradeChallengeTarget();
            if (targetEntity != null)
            {
                targetEntity.SetVisualParent(m_LevelRoot.transform);
            }
        }

        private void UpdateScenePhase(TowerDefendBattle battle, float dt)
        {
            if (battle == null) return;

            bool isChallengeActive = battle.ReadPhase() != BattlePhase.NormalGame;
            if (!isChallengeActive)
            {
                m_WasChallengeActive = false;
                m_HasReportedUpgradeChallengeCameraReady = false;
            }
            else if (!m_WasChallengeActive)
            {
                m_WasChallengeActive = true;
                m_HasReportedUpgradeChallengeCameraReady = false;
                if (m_Camera == null)
                {
                    ResolveBattleCamera();
                }

                SetScenePhase(ScenePhase.EntryDelay);
                m_ScenePhaseTimer = 1.0f;
            }

            if (m_ScenePhase == ScenePhase.EntryDelay)
            {
                m_ScenePhaseTimer -= dt;
                if (m_ScenePhaseTimer <= 0f)
                {
                    SetScenePhase(ScenePhase.Showing);
                    m_ScenePhaseTimer = 0f;
                }
            }
            else if (m_ScenePhase == ScenePhase.Showing &&
                     battle.ReadPhase() == BattlePhase.ChallengeFinish)
            {
                SetScenePhase(ScenePhase.Restoring);
            }
        }

        private void SetScenePhase(ScenePhase nextPhase)
        {
            if (m_ScenePhase == nextPhase)
            {
                return;
            }

            m_ScenePhase = nextPhase;

            // 场景环境旋转与棒棒糖镜头共用同一套阶段切换：
            // 只有镜头真正进入棒棒糖展示阶段时才开启，进入恢复阶段后立即关闭，
            // 这样场景动画与镜头进入、回正保持同一时序，不需要由界面层重复控制。
            ApplyEnvironmentRotationState(nextPhase == ScenePhase.Showing);
            ApplyUpgradeChallengeCameraRotationState(nextPhase == ScenePhase.Showing);
        }


        protected void OnGameResult()
        {
            m_SceneState = ESceneState.Result;


        }
        protected void OnGameFightAgain()
        {
            m_SceneState = ESceneState.Loop;
        }

        private bool OnPauseRequest()
        {
            return TrySetBattlePause(true, true);
        }

        private bool OnBattlePauseStateRequest(bool pause)
        {
            return TrySetBattlePause(pause, false);
        }

        private bool IsBattlePaused()
        {
            var battle = BattleManager.GetBattle();
            return battle != null && battle.IsBattlePause();
        }

        private bool TrySetBattlePause(bool pause, bool openPauseWindow)
        {
            var battle = BattleManager.GetBattle();
            if (battle == null)
            {
                return false;
            }

            if (!pause)
            {
                ApplyBattlePauseState(battle, false);
                ClosePauseWindowOnly();
                return true;
            }

            if (m_SceneState != ESceneState.Loop)
            {
                return false;
            }

            ApplyBattlePauseState(battle, true);
            if (!openPauseWindow)
            {
                return true;
            }

            if (m_PauseWindow != null)
            {
                return true;
            }

            m_PauseWindow = RenderEvent.Event.OnTowerDefendPauseOpenRequest(
                ClosePauseWindow,
                RestartCurrentBattle,
                ReturnToLobby);
            if (m_PauseWindow != null)
            {
                return true;
            }

            ApplyBattlePauseState(battle, false);
            return false;
        }

        private static void ApplyBattlePauseState(IBattle battle, bool pause)
        {
            // 正式战斗暂停需要同时冻结战斗逻辑和声音；GM 调试冻结不经过此处。
            if (battle != null)
            {
                battle.SetBattlePause(pause);
            }

            if (pause)
            {
                AudioManager.GetInstance().PauseGameAudio();
            }
            else
            {
                AudioManager.GetInstance().ResumeGameAudio();
            }
        }

        private void OnBattleResult(BattleResultData result)
        {
            if (result == null || m_HasPendingResult)
            {
                return;
            }

            RenderEvent.Event.OnTowerDefendBattleHudCloseRequest();
            ClosePauseWindow();
            m_PendingResult = result;
            m_HasPendingResult = true;
            m_SceneState = ESceneState.Result;
            RenderEvent.Event.OnGameResult();
        }

        public override bool TryConsumeResult(out BattleResultData result)
        {
            if (!m_HasPendingResult)
            {
                result = null;
                return false;
            }

            result = m_PendingResult;
            m_PendingResult = null;
            m_HasPendingResult = false;
            return true;
        }

        public override void Destroy()
        {
            AudioManager.GetInstance().SetAudioListenerTarget(null);
            RenderEvent.Event.OnTowerDefendBattleHudCloseRequest();
            ClosePauseWindow();
            ApplyEnvironmentRotationState(false);
            ApplyUpgradeChallengeCameraRotationState(false);
            RestoreUpgradeChallengeCameraAnimatorEnabledState();
            m_EnvironmentAnimator = null;
            m_UpgradeChallengeCameraAnimator = null;
            m_HasAppliedEnvironmentRotationState = false;
            m_IsEnvironmentRotationEnabled = false;
            m_HasAppliedUpgradeChallengeCameraRotationState = false;
            m_IsUpgradeChallengeCameraRotationEnabled = false;
            m_HasCachedUpgradeChallengeCameraAnimatorEnabled = false;
            m_DefaultUpgradeChallengeCameraAnimatorEnabled = false;
            ClearBattleSkeletonVisualSuppressor();
            BoneRemoteDebugEditorConfig.SetBattleSkeletonDisplaySuppressed(false);
            ClearBoneInputRuntime();

            RenderEvent.Event.OnGameResult -= OnGameResult;
            RenderEvent.Event.OnGameFightAgain -= OnGameFightAgain;
            RenderEvent.Event.OnBattleResult -= OnBattleResult;
            RenderEvent.Event.OnTowerDefendPauseRequest -= OnPauseRequest;
            RenderEvent.Event.OnTowerDefendBattlePauseStateRequest -= OnBattlePauseStateRequest;
            RenderEvent.Event.OnTowerDefendBattlePauseStateQuery -= IsBattlePaused;

            BattleManager.Destroy();


            if (m_Scene != null)
            {
                m_Scene.Destroy();
                m_Scene = null;
            }

            DestroyAimAssistLines();

            base.Destroy();
            UDebug.Log("end Level Destroy");
        }
        protected override void ParseScene()
        {
            base.ParseScene();

            // 塔防战斗期间不允许显示骨骼可视化，避免真机和编辑器在战斗里出现调试骨架。
            BoneRemoteDebugEditorConfig.SetBattleSkeletonDisplaySuppressed(true);
            EnsureBattleSkeletonVisualSuppressor();
            m_Scene.Enter();
            ResolveBattleCamera();
            CacheEnvironmentAnimator();
            CacheUpgradeChallengeCameraAnimator(null);
            ApplyEnvironmentRotationState(m_ScenePhase == ScenePhase.Showing);
            ApplyUpgradeChallengeCameraRotationState(m_ScenePhase == ScenePhase.Showing);
            SetLightInfo();
            EnsureBoneInputRuntime();
        }

        private void CacheEnvironmentAnimator()
        {
            if (m_EnvironmentAnimator != null || m_LevelRoot == null)
            {
                return;
            }

            var animators = m_LevelRoot.GetComponentsInChildren<Animator>(true);
            if (animators == null)
            {
                return;
            }

            for (int i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null || animator.transform == null)
                {
                    continue;
                }

                if (!string.Equals(animator.transform.name, m_EnvironmentRootName, StringComparison.Ordinal))
                {
                    continue;
                }

                m_EnvironmentAnimator = animator;
                return;
            }
        }

        private void ApplyEnvironmentRotationState(bool enabled)
        {
            if (m_LevelRoot == null)
            {
                m_HasAppliedEnvironmentRotationState = false;
                m_IsEnvironmentRotationEnabled = enabled;
                return;
            }

            if (m_EnvironmentAnimator == null)
            {
                CacheEnvironmentAnimator();
            }

            if (m_EnvironmentAnimator == null)
            {
                m_HasAppliedEnvironmentRotationState = false;
                m_IsEnvironmentRotationEnabled = enabled;
                return;
            }

            if (m_HasAppliedEnvironmentRotationState &&
                m_IsEnvironmentRotationEnabled == enabled)
            {
                return;
            }

            m_EnvironmentAnimator.SetBool(m_EnvironmentRotationParamHash, enabled);
            m_HasAppliedEnvironmentRotationState = true;
            m_IsEnvironmentRotationEnabled = enabled;
        }

        private void CacheUpgradeChallengeCameraAnimator(Animator sceneCameraAnimator)
        {
            if (m_UpgradeChallengeCameraAnimator != null)
            {
                CacheUpgradeChallengeCameraAnimatorEnabledState();
                return;
            }

            if (m_Camera == null)
            {
                return;
            }

            var cameraAnimator = m_Camera.GetComponent<Animator>();
            var sceneCameraAnimatorController = sceneCameraAnimator != null
                ? sceneCameraAnimator.runtimeAnimatorController
                : null;
            bool cameraBelongsToLevel = m_LevelRoot != null &&
                m_Camera.transform != null &&
                m_Camera.transform.IsChildOf(m_LevelRoot.transform);
            if (sceneCameraAnimatorController == null && !cameraBelongsToLevel)
            {
                m_UpgradeChallengeCameraAnimator = null;
                return;
            }

            if (cameraAnimator == null &&
                sceneCameraAnimatorController != null)
            {
                cameraAnimator = m_Camera.gameObject.AddComponent<Animator>();
            }

            if (cameraAnimator != null &&
                sceneCameraAnimator != null &&
                sceneCameraAnimatorController != null)
            {
                if (cameraAnimator.runtimeAnimatorController != sceneCameraAnimatorController)
                {
                    cameraAnimator.runtimeAnimatorController = sceneCameraAnimatorController;
                }

                cameraAnimator.enabled = sceneCameraAnimator.enabled;
                cameraAnimator.cullingMode = sceneCameraAnimator.cullingMode;
                cameraAnimator.updateMode = sceneCameraAnimator.updateMode;
                cameraAnimator.applyRootMotion = sceneCameraAnimator.applyRootMotion;
            }

            m_UpgradeChallengeCameraAnimator = cameraAnimator;
            CacheUpgradeChallengeCameraAnimatorEnabledState();
        }

        private void CacheUpgradeChallengeCameraAnimatorEnabledState()
        {
            if (m_UpgradeChallengeCameraAnimator == null ||
                m_HasCachedUpgradeChallengeCameraAnimatorEnabled)
            {
                return;
            }

            m_DefaultUpgradeChallengeCameraAnimatorEnabled = m_UpgradeChallengeCameraAnimator.enabled;
            m_HasCachedUpgradeChallengeCameraAnimatorEnabled = true;
        }

        private void EnsureUpgradeChallengeCameraAnimatorEnabled()
        {
            if (m_UpgradeChallengeCameraAnimator == null)
            {
                return;
            }

            CacheUpgradeChallengeCameraAnimatorEnabledState();
            if (!m_UpgradeChallengeCameraAnimator.enabled)
            {
                m_UpgradeChallengeCameraAnimator.enabled = true;
            }
        }

        private void RestoreUpgradeChallengeCameraAnimatorEnabledState()
        {
            if (m_UpgradeChallengeCameraAnimator == null ||
                !m_HasCachedUpgradeChallengeCameraAnimatorEnabled)
            {
                return;
            }

            m_UpgradeChallengeCameraAnimator.enabled = m_DefaultUpgradeChallengeCameraAnimatorEnabled;
        }

        private void ApplyUpgradeChallengeCameraRotationState(bool enabled)
        {
            if (m_Camera == null)
            {
                m_HasAppliedUpgradeChallengeCameraRotationState = false;
                m_IsUpgradeChallengeCameraRotationEnabled = enabled;
                return;
            }

            if (m_UpgradeChallengeCameraAnimator == null)
            {
                CacheUpgradeChallengeCameraAnimator(null);
            }

            if (m_UpgradeChallengeCameraAnimator == null)
            {
                m_HasAppliedUpgradeChallengeCameraRotationState = false;
                m_IsUpgradeChallengeCameraRotationEnabled = enabled;
                return;
            }

            if (m_HasAppliedUpgradeChallengeCameraRotationState &&
                m_IsUpgradeChallengeCameraRotationEnabled == enabled)
            {
                return;
            }

            if (enabled || m_IsUpgradeChallengeCameraRotationEnabled)
            {
                EnsureUpgradeChallengeCameraAnimatorEnabled();
            }
            else
            {
                CacheUpgradeChallengeCameraAnimatorEnabledState();
                if (!m_UpgradeChallengeCameraAnimator.enabled)
                {
                    m_HasAppliedUpgradeChallengeCameraRotationState = true;
                    m_IsUpgradeChallengeCameraRotationEnabled = enabled;
                    return;
                }
            }

            m_UpgradeChallengeCameraAnimator.SetBool(m_EnvironmentRotationParamHash, enabled);
            m_HasAppliedUpgradeChallengeCameraRotationState = true;
            m_IsUpgradeChallengeCameraRotationEnabled = enabled;
        }

        private void EnsureBoneInputRuntime()
        {
            if (m_LevelRoot == null)
            {
                return;
            }

            if (m_BattleBoneParseData == null)
            {
                m_BattleBoneParseData = m_LevelRoot.GetComponentInChildren<BattleBoneParseData>(true);
            }

            if (m_BattleBoneParseData == null)
            {
                var boneInputRoot = new GameObject(m_BoneInputRootName);
                boneInputRoot.transform.SetParent(m_LevelRoot.transform, false);
                m_BattleBoneParseData = boneInputRoot.AddComponent<BattleBoneParseData>();
            }

            ConfigureBattleBoneSlotLayout();

            if (m_BoneFrameSource == null && m_BattleBoneParseData != null)
            {
                m_BoneFrameSource = new BoneFrameSourceResolver(m_BattleBoneParseData);
            }

            if (m_BoneInputDriver == null && m_BoneFrameSource != null)
            {
                m_BoneInputDriver = new TowerDefendBoneInputDriver();
                m_BoneInputDriver.Init(this, m_BoneFrameSource);
            }

            if (m_BoneDebugSkeletonOverlay == null && m_BattleBoneParseData != null)
            {
                m_BoneDebugSkeletonOverlay = m_BattleBoneParseData.GetComponent<BoneDebugSkeletonOverlay>();
                if (m_BoneDebugSkeletonOverlay == null)
                {
                    m_BoneDebugSkeletonOverlay = m_BattleBoneParseData.gameObject.AddComponent<BoneDebugSkeletonOverlay>();
                }
            }

            if (m_BoneDebugSkeletonOverlay != null)
            {
                m_BoneDebugSkeletonOverlay.Bind(m_BoneFrameSource);
            }
        }

        private void ConfigureBattleBoneSlotLayout()
        {
            if (m_BattleBoneParseData == null)
            {
                return;
            }

            // 准备界面决定本局人数，战斗输入必须用同一人数生成检测区，否则 1 人局会退回四人分区。
            int playerCount = TowerDefendSeatLayout.DefaultPlayerCount;
            var startRequest = m_LevelInputData != null ? m_LevelInputData.m_StartRequest : null;
            if (startRequest != null)
            {
                playerCount = startRequest.ReadPreparePlayerCount();
            }
            else if (m_LevelInputData != null && m_LevelInputData.m_BattleData != null)
            {
                playerCount = m_LevelInputData.m_BattleData.GetPlayerCount();
            }

            m_BattleBoneParseData.ConfigureBattleSlotLayout(
                playerCount,
                BuildInitialBattlePersonIds(playerCount, startRequest));
        }

        private static int[] BuildInitialBattlePersonIds(int playerCount, BattleStartupRequest startRequest)
        {
            var parseDataDemo = global::AndroidParseDataDemo.Instance;
            var readySeatIds = parseDataDemo != null ? parseDataDemo.GetReadySeatIds() : null;
            if (readySeatIds == null || readySeatIds.Count <= 0)
            {
                return null;
            }

            var personIds = new int[BoneSlotLayout.m_SlotCount];
            for (int i = 0; i < personIds.Length; i++)
            {
                personIds[i] = YouDooSDKConstants.PersonIdNull;
            }

            int seatCount = Mathf.Min(playerCount, personIds.Length);
            for (int seatId = 0; seatId < seatCount; seatId++)
            {
                int sdkSlotIndex = seatId;
                if (startRequest != null &&
                    startRequest.TryGetSdkSlotIndexForSeat(seatId, out int mappedSdkSlotIndex))
                {
                    sdkSlotIndex = mappedSdkSlotIndex;
                }

                if (readySeatIds.TryGetValue(sdkSlotIndex, out int personId))
                {
                    personIds[seatId] = personId;
                }
            }

            return personIds;
        }

        private void UpdateBoneInput()
        {
            if (!BoneRemoteDebugEditorConfig.ReadIsBoneControlEnabled())
            {
                if (m_BoneInputDriver != null || m_BoneFrameSource != null || m_BattleBoneParseData != null)
                {
                    ClearBoneInputRuntime();
                }

                if (m_LastBoneSourceName != "disabled")
                {
                    m_LastBoneSourceName = "disabled";
                    Debug.Log("[骨骼远程调试] 当前输入来源: 已关闭（当前 GMTools 骨骼控制开关为关闭状态）");
                }
                return;
            }

            if (m_BoneInputDriver == null || m_BoneFrameSource == null)
            {
                EnsureBoneInputRuntime();
            }

            if (m_BoneInputDriver != null)
            {
                m_BoneInputDriver.Update();
            }

            if (m_BoneFrameSource == null)
            {
                return;
            }

            string sourceName = m_BoneFrameSource.ReadSourceName();
            if (m_LastBoneSourceName == sourceName)
            {
                return;
            }

            m_LastBoneSourceName = sourceName;
            Debug.Log("[骨骼远程调试] 当前输入来源: " + ReadBoneSourceDisplayName(sourceName));
        }

        private static string ReadBoneSourceDisplayName(string sourceName)
        {
            switch (sourceName)
            {
                case "local_sdk":
                    return "本地SDK（当前主工程直接读取本机接入的骨骼SDK数据）";
                case "remote_debug":
                    return "远程调试（当前主工程通过网络读取发送端转发过来的骨骼数据）";
                case "none":
                    return "无（当前没有可用的骨骼输入数据）";
                default:
                    return string.IsNullOrEmpty(sourceName) ? "未知" : sourceName;
            }
        }

        private void ClearBoneInputRuntime()
        {
            if (m_BoneInputDriver != null)
            {
                m_BoneInputDriver.Destroy();
                m_BoneInputDriver = null;
            }

            if (m_BoneFrameSource != null)
            {
                m_BoneFrameSource.Shutdown();
                m_BoneFrameSource = null;
            }

            if (m_BoneDebugSkeletonOverlay != null)
            {
                m_BoneDebugSkeletonOverlay.Bind(null);
                m_BoneDebugSkeletonOverlay = null;
            }

            m_BattleBoneParseData = null;
            m_LastBoneSourceName = null;
        }

        private void EnsureBattleSkeletonVisualSuppressor()
        {
            if (m_LevelRoot == null)
            {
                return;
            }

            if (m_BattleSkeletonVisualSuppressor == null)
            {
                m_BattleSkeletonVisualSuppressor = m_LevelRoot.GetComponent<BattleSkeletonVisualSuppressor>();
                if (m_BattleSkeletonVisualSuppressor == null)
                {
                    m_BattleSkeletonVisualSuppressor = m_LevelRoot.AddComponent<BattleSkeletonVisualSuppressor>();
                }
            }

            m_BattleSkeletonVisualSuppressor.ForceHide();
        }

        private void ClearBattleSkeletonVisualSuppressor()
        {
            if (m_BattleSkeletonVisualSuppressor == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(m_BattleSkeletonVisualSuppressor);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(m_BattleSkeletonVisualSuppressor);
            }

            m_BattleSkeletonVisualSuppressor = null;
        }

        protected virtual bool TryResolveScenePointData(out TowerDefendScenePointData pointData, out string error)
        {
            pointData = null;
            error = string.Empty;
            var root = GetSceneRoot();
            if (root == null)
            {
                error =
                    "塔防战斗场景根节点为空，无法解析必需的 PositionHelper 点位。 " +
                    m_ScenePointDocHint;
                return false;
            }

            m_UpgradeChallengePosition = root.transform.Find("td_bangbangtang");
            var npcSpeakPoint = root.transform.Find("boss_point");

            var helpers = root.GetComponentsInChildren<PositionHelper>(true);
            var resolvedPointData = new TowerDefendScenePointData();

            resolvedPointData.m_UpgradeChallengeFoot = m_UpgradeChallengePosition.position;
            if (npcSpeakPoint != null)
            {
                resolvedPointData.m_NpcSpeakPoint = npcSpeakPoint;
            }

            resolvedPointData.m_BaseReachRadius = 1.5f;

            var missingPointNames = new List<string>();
            for (int i = 1; i <= 10; i++)
            {
                Vector3 spawnPoint;
                var pointName = m_SpawnPointPrefix + i;
                if (!TryResolveNamedPoint(helpers, pointName, out spawnPoint))
                {
                    missingPointNames.Add(pointName);
                    continue;
                }

                resolvedPointData.m_MonsterSpawnPoints.Add(spawnPoint);

                var pathPointData = new TowerDefendMonsterPathPointData();
                pathPointData.m_SpawnPoint = spawnPoint;

                Vector3 endPoint;
                if (TryResolveChildNamedPoint(helpers, pointName, m_SpawnEndPointName, out endPoint))
                {
                    pathPointData.m_EndPoint = endPoint;
                    pathPointData.m_HasEndPoint = true;
                }

                resolvedPointData.m_MonsterPathPoints.Add(pathPointData);
            }

            for (int i = 1; i <= 4; i++)
            {
                Vector3 guardPoint;
                var pointName = m_GuardPointPrefix + i;
                if (!TryResolveNamedPoint(helpers, pointName, out guardPoint))
                {
                    missingPointNames.Add(pointName);
                    continue;
                }

                resolvedPointData.m_GuardHeroSpawnPoints.Add(guardPoint);
            }

            Vector3 basePoint;
            if (!TryResolveNamedPoint(helpers, m_BasePointName, out basePoint))
            {
                missingPointNames.Add(m_BasePointName);
            }
            else
            {
                resolvedPointData.m_BasePoint = basePoint;
            }

            if (missingPointNames.Count > 0)
            {
                error =
                    "塔防战斗场景缺少必需的 PositionHelper 点位: " +
                    string.Join(", ", missingPointNames) +
                    "。请先在战斗场景中补齐并严格按名称命名这些点位，再重新进入战斗。 " +
                    m_ScenePointDocHint;
                return false;
            }

            pointData = resolvedPointData;
            return true;
        }

        private bool TryResolveNamedPoint(PositionHelper[] helpers, string pointName, out Vector3 position)
        {
            position = Vector3.zero;
            if (helpers == null)
            {
                return false;
            }

            foreach (var helper in helpers)
            {
                if (helper == null || helper.transform == null)
                {
                    continue;
                }

                if (string.Equals(helper.name, pointName, StringComparison.OrdinalIgnoreCase))
                {
                    position = helper.transform.position;
                    return true;
                }
            }

            return false;
        }

        private PositionHelper ResolveNamedPointHelper(PositionHelper[] helpers, string pointName)
        {
            if (helpers == null)
            {
                return null;
            }

            foreach (var helper in helpers)
            {
                if (helper == null || helper.transform == null)
                {
                    continue;
                }

                if (string.Equals(helper.name, pointName, StringComparison.OrdinalIgnoreCase))
                {
                    return helper;
                }
            }

            return null;
        }

        private bool TryResolveChildNamedPoint(PositionHelper[] helpers, string parentPointName, string childPointName, out Vector3 position)
        {
            position = Vector3.zero;
            if (helpers == null)
            {
                return false;
            }

            Transform parentTransform = null;
            foreach (var helper in helpers)
            {
                if (helper == null || helper.transform == null)
                {
                    continue;
                }

                if (string.Equals(helper.name, parentPointName, StringComparison.OrdinalIgnoreCase))
                {
                    parentTransform = helper.transform;
                    break;
                }
            }

            if (parentTransform == null)
            {
                return false;
            }

            foreach (var helper in helpers)
            {
                if (helper == null || helper.transform == null)
                {
                    continue;
                }

                if (!string.Equals(helper.name, childPointName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (helper.transform.parent != parentTransform)
                {
                    continue;
                }

                position = helper.transform.position;
                return true;
            }

            return false;
        }

        protected override void LoadScene()
        {
            base.LoadScene();

            m_Scene = new UScene();

            var cfg = TowerDefendStageConfigResolver.Resolve(m_Stage, m_LevelInputData.m_BattleData != null ? m_LevelInputData.m_BattleData.m_GameMode : BattleGameMode.Chapter);
            if(cfg == null)
            {
                Debug.LogError($"找不到塔防关卡配置，关卡ID：{m_Stage}。请先检查关卡表配置是否完整。");
                return;
            }
            var res = cfg.ScenePath;
            var empty_scene = new UScene();
            empty_scene.LoadEmpty((hr) =>
            {
                m_Scene.Init(res, 0, (result) =>
                {
                    m_LevelRoot = GameObject.Find(Tool.GetAssetName(res));
                    if (result)
                    {
                        LoadCache();
                        m_SceneState = ESceneState.LoadedScene;
                    }
                    else
                    {
                        m_SceneState = ESceneState.Error;
                    }

                });


            });
        }

        public override UScene GetSceneResource()
        {
            return m_Scene;
        }


        public override GameObject GetSceneRoot()
        {
            return m_LevelRoot.gameObject;
        }
        public Camera GetActiveCamera()
        {
            return m_Camera;
        }

        // 战斗内部始终按正式座位工作，只有骨骼输入需要回到 SDK 槽位读取原始人体数据。
        public bool TryGetSdkSlotIndexBySeat(int seatId, out int sdkSlotIndex)
        {
            var startRequest = m_LevelInputData != null ? m_LevelInputData.m_StartRequest : null;
            sdkSlotIndex = -1;
            return startRequest != null && startRequest.TryGetSdkSlotIndexForSeat(seatId, out sdkSlotIndex);
        }

        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            DrawUpgradeChallengeScoreGizmos();

            GameDll.BattleManager.OnDrawGizmos();
        }

        protected override void OnLoadingScene()
        {

        }

        private void ClosePauseWindow()
        {
            ApplyBattlePauseState(BattleManager.GetBattle(), false);
            ClosePauseWindowOnly();
        }

        private void ClosePauseWindowOnly()
        {
            if (m_PauseWindow != null)
            {
                RenderEvent.Event.OnTowerDefendPauseCloseRequest(m_PauseWindow);
                m_PauseWindow = null;
            }
        }

        private void RestartCurrentBattle()
        {
            RenderEvent.Event.OnTowerDefendRestartBattleRequest();
        }

        private void ReturnToLobby()
        {
            RenderEvent.Event.OnTowerDefendReturnLobbyRequest();
        }

        private void ResolveBattleCamera()
        {
            Camera sceneCamera = null;
            if (m_LevelRoot != null)
            {
                sceneCamera = m_LevelRoot.GetComponentInChildren<Camera>(true);
            }
            var sceneCameraAnimator = sceneCamera != null ? sceneCamera.GetComponent<Animator>() : null;

            var cameraFoot = CameraFoot.GetInstance();
            var cameraFootEye = cameraFoot != null ? cameraFoot.ReadCameraEye() : null;
            if (cameraFootEye != null)
            {
                if (sceneCamera != null)
                {
                    cameraFoot.ApplySceneCamera(sceneCamera);
                }

                m_Camera = cameraFootEye;
            }

            if (m_Camera == null)
            {
                m_Camera = sceneCamera;
            }

            if (m_Camera == null)
            {
                m_Camera = Camera.main;
            }

            if (m_Camera == null)
            {
                m_Camera = GameObject.FindObjectOfType<Camera>(true);
            }

            if (m_Camera == null)
            {
                return;
            }

            m_CameraRoot = m_Camera.transform;
            RestoreUpgradeChallengeCameraAnimatorEnabledState();
            m_UpgradeChallengeCameraAnimator = null;
            m_HasCachedUpgradeChallengeCameraAnimatorEnabled = false;
            m_DefaultUpgradeChallengeCameraAnimatorEnabled = false;
            CacheUpgradeChallengeCameraAnimator(sceneCameraAnimator);
            RenderAPI.SetWorldCamera(m_Camera);
            AudioManager.GetInstance().SetAudioListenerTarget(m_Camera.transform);
        }

        private void UpdateUpgradeChallengeCamera(float dt)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return;
            }

            if (m_Camera == null)
            {
                ResolveBattleCamera();
            }

            if (m_Camera == null)
            {
                return;
            }

            if (m_ScenePhase == ScenePhase.Idle)
            {
                return;
            }

            if (m_ScenePhase == ScenePhase.EntryDelay)
            {
                return;
            }

            if (m_ScenePhase == ScenePhase.Showing)
            {
                if (!m_HasReportedUpgradeChallengeCameraReady &&
                    battle.ReadPhase() == BattlePhase.ChallengeEntryWait &&
                    IsUpgradeChallengeCameraRotationReady())
                {
                    m_HasReportedUpgradeChallengeCameraReady = true;
                    battle.NotifyUpgradeChallengeCameraReady();
                }
                return;
            }

            if (m_ScenePhase == ScenePhase.Restoring)
            {
                if (IsUpgradeChallengeCameraDefaultReady() &&
                    battle.CanCompleteUpgradeChallengeFinish())
                {
                    SetScenePhase(ScenePhase.Idle);
                    RestoreUpgradeChallengeCameraAnimatorEnabledState();
                    battle.TryRestoreMonsters();
                }
            }
        }

        private bool IsUpgradeChallengeCameraRotationReady()
        {
            return IsUpgradeChallengeCameraAnimatorStateReady(m_UpgradeChallengeCameraRotationStateName, true);
        }

        private bool IsUpgradeChallengeCameraDefaultReady()
        {
            return IsUpgradeChallengeCameraAnimatorStateReady(m_UpgradeChallengeCameraDefaultStateName, false);
        }

        private bool IsUpgradeChallengeCameraAnimatorStateReady(string stateName, bool requireComplete)
        {
            if (m_UpgradeChallengeCameraAnimator == null)
            {
                CacheUpgradeChallengeCameraAnimator(null);
            }

            if (m_UpgradeChallengeCameraAnimator == null)
            {
                return true;
            }

            if (m_UpgradeChallengeCameraAnimator.IsInTransition(0))
            {
                return false;
            }

            var stateInfo = m_UpgradeChallengeCameraAnimator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(stateName))
            {
                return false;
            }

            return !requireComplete || stateInfo.normalizedTime >= 1.0f;
        }

        private void DrawUpgradeChallengeScoreGizmos()
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            var targetEntity = battle != null ? battle.ReadUpgradeChallengeTarget() : null;
            if (targetEntity == null)
            {
                return;
            }

            var centerPosition = targetEntity.ReadCenterPosition();
            if (centerPosition == Vector3.zero && targetEntity.GetFootPosition() != Vector3.zero)
            {
                centerPosition = targetEntity.GetFootPosition();
            }

            int boundaryCount = UpgradeChallengeTarget.ReadScoreBoundaryCount();
            for (int i = 0; i < boundaryCount; i++)
            {
                float radius = UpgradeChallengeTarget.ReadScoreBoundaryRadius(i);
                if (radius <= 0f)
                {
                    continue;
                }

                DrawWireCircleOnPlane(
                    centerPosition,
                    Vector3.forward,
                    radius,
                    ResolveUpgradeChallengeDebugColor(i));
            }
        }

        private static void DrawWireCircleOnPlane(Vector3 center, Vector3 normal, float radius, Color color)
        {
            const int segmentCount = 48;
            if (radius <= 0f)
            {
                return;
            }

            Vector3 planeNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.forward;
            Vector3 tangent = Vector3.Cross(planeNormal, Vector3.up);
            if (tangent.sqrMagnitude <= 0.0001f)
            {
                tangent = Vector3.Cross(planeNormal, Vector3.right);
            }
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(planeNormal, tangent).normalized;

            Color oldColor = Gizmos.color;
            Gizmos.color = color;

            Vector3 previous = center + tangent * radius;
            for (int i = 1; i <= segmentCount; i++)
            {
                float angle = i / (float)segmentCount * Mathf.PI * 2.0f;
                Vector3 next = center +
                    (Mathf.Cos(angle) * tangent + Mathf.Sin(angle) * bitangent) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.color = oldColor;
        }

        private static Color ResolveUpgradeChallengeDebugColor(int ringIndex)
        {
            switch (ringIndex)
            {
                case 0:
                    return new Color(1.0f, 0.9f, 0.25f, 0.95f);
                case 1:
                    return new Color(1.0f, 0.72f, 0.24f, 0.95f);
                case 2:
                    return new Color(1.0f, 0.55f, 0.24f, 0.95f);
                case 3:
                    return new Color(1.0f, 0.40f, 0.24f, 0.95f);
                default:
                    return new Color(0.92f, 0.22f, 0.28f, 0.95f);
            }
        }

        private void UpdateAimAssistLines()
        {
            if (m_LevelRoot == null)
            {
                return;
            }

            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            for (int seatId = 0; seatId < m_AimAssistLines.Length; seatId++)
            {
                if (m_AimAssistLines[seatId] == null)
                {
                    m_AimAssistLines[seatId] = new TowerDefendAimAssistLine(m_LevelRoot);
                }

                m_AimAssistLines[seatId].Update(battle, seatId);
            }
        }

        private void DestroyAimAssistLines()
        {
            for (int seatId = 0; seatId < m_AimAssistLines.Length; seatId++)
            {
                if (m_AimAssistLines[seatId] == null)
                {
                    continue;
                }

                m_AimAssistLines[seatId].Destroy();
                m_AimAssistLines[seatId] = null;
            }
        }

        // 战斗里不改 SDK Demo 逻辑，只在场景末尾把外部骨骼可视化统一收掉。
        private sealed class BattleSkeletonVisualSuppressor : MonoBehaviour
        {
            public void ForceHide()
            {
                HideExternalBattleSkeletons();
            }

            private void LateUpdate()
            {
                HideExternalBattleSkeletons();
            }

            private static void HideExternalBattleSkeletons()
            {
                if (BoneRemoteDebugEditorConfig.ReadShouldDrawBattleSkeleton())
                {
                    return;
                }

                var parseDataDemo = global::AndroidParseDataDemo.Instance;
                var playerTextureShow = parseDataDemo != null ? parseDataDemo.playerTextuerShow : null;
                if (playerTextureShow != null)
                {
                    playerTextureShow.SetSkeletsonHide();
                }
            }
        }
    }
}
