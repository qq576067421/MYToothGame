using LCL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GameDll;
using System.Diagnostics.SymbolStore;

namespace GameHot
{
    public enum PlayerState
    {
        Login,
        Lobby,
        Battle
    }
    public class MoneyId
    {
        public static long CoinId;
        public static long GemId;
    }
    public class PlayerInfo
    {
        [JsonInclude]
        public long guid;
        [JsonInclude]
        public string name = "";
        [JsonInclude]
        public long cfg_id = 0;
        [JsonInclude]
        public int level;
        [JsonInclude]
        public int exp;
        [JsonInclude]
        public int coin;
        [JsonInclude]
        public int gem;
        [JsonInclude]
        public int stage = 1;
        [JsonInclude]
        public long last_get_offline_award_time;
        [JsonInclude]
        public long login_time;
        [JsonInclude]
        public int mission_stage = 1;
        [JsonInclude]
        public int vip = 0;
        [JsonInclude]
        public int master_stage = 1;
        [JsonInclude]
        public int guide_group;
        [JsonInclude]
        public byte online;

        public const string __tableName = "PlayerInfo";
        public int GetMoney(long moneyId)
        {
            if(moneyId == MoneyId.CoinId)
            {
                return coin;
            }
            else if(moneyId == MoneyId.GemId)
            {
                return gem;
            }
            else
            {
                return 0;
            }
        }
        public void SetMoney(long moneyId, int value)
        {
            if (moneyId == MoneyId.CoinId)
            {
                coin = value;
            }
            else if (moneyId == MoneyId.GemId)
            {
                gem = value;
            }

            CGameProcedure.Event.OnMoneyChanged();
        }
    }
    [Serializable]
    public class LobbyPlayerSaveData
    {
        public int m_SaveVersion = 1;
        public string m_RoleName = "";
        public int m_Coin = 0;
        public int m_Gem = 0;
        public int m_UnlockedStage = 1;
        public int m_MissionStage = 1;
        public int m_BestEndlessWave = 0;
        public int m_GiftednessUnlockCount = 0;
        public List<int> m_GiftednessUnlockBranches = new List<int>();
        public int m_SelectedGameMode = (int)BattleGameMode.Chapter;
        public int m_SelectedStageId = 0;
        [NonSerialized] public Dictionary<int, int> m_StageStars = new Dictionary<int, int>();
        public List<StageStarEntry> m_StageStarList = new List<StageStarEntry>();
        public LanguageType m_languageType;
        public int m_FirstStart = 0;
    }

    // JsonUtility 不支持字典，星级以列表镜像序列化，读写时与字典互相同步。
    [Serializable]
    public class StageStarEntry
    {
        public int m_StageId;
        public int m_Stars;
    }
    public class LobbyPlayer
    {
        private const int m_CurrentSaveVersion = 1;
        private const string m_DefaultLocalRoleName = "Player";

        public static bool IsInitInstance()
        {
            return m_Instance != null && m_Instance.m_IsInited;
        }
        private static LobbyPlayer m_Instance;
        public static LobbyPlayer GetInstance()
        {
            if (m_Instance == null)
            {
                m_Instance = new LobbyPlayer();
            }
            return m_Instance;
        }
        //是否是单机游戏
        private bool m_IsLocal = false;
        public bool IsLocalGame()
        {
            return m_IsLocal;
        }
        public void SetLocalGame(bool isLocal)
        {
            m_IsLocal = isLocal;
        }
        public long PlayerId
        {
            get
            {
                return m_PlayerInfo.guid;
            }
        }
        public int m_WorldId;
        public string PlayerName
        {
            get
            {
                return m_PlayerInfo.name;
            }
        }
        public string m_ZoneName;
        public PlayerInfo m_PlayerInfo;
        private LobbyPlayerSaveData m_SaveData;
        private bool m_IsWaitingRemoteSave;
        private bool m_PendingLanguageChanged;
        private bool m_PendingFirstStartConsumed;
        //建议关键节点保存数据，不做自动保存功能（参考很多游戏都没有做这个功能）
        public void SavePlayer()
        {
            SavePlayerInfo();
        }
        public void SavePlayerInfo()
        {
            if (!m_IsLocal)
            {
                return;
            }
            EnsureLocalPlayerInfo();
            if (m_PlayerInfo == null)
            {
                return;
            }

            SyncSaveDataFromPlayerInfo(EnsureSaveData());
            SaveSaveData();
        }
        public void SetLocalPlayerRoleName(string name)
        {
            if (!m_IsLocal)
            {
                return;
            }

            var saveData = EnsureSaveData();
            saveData.m_RoleName = NormalizeRoleName(name);
            if (m_PlayerInfo != null)
            {
                m_PlayerInfo.name = saveData.m_RoleName;
            }
            SaveSaveData();
        }
        public string GetLocalPlayerRoleName()
        {
            return EnsureSaveData().m_RoleName;
        }
        public bool HasLocalPlayerInfo()
        {
            var roleName = GetLocalPlayerRoleName();
            if(string.IsNullOrEmpty(roleName))
            {
                return false;
            }
            var md5 = RenderAPI.GetMD5Hash(roleName);
            var path = LCL.MonoTool.GetPersistentPath() + md5 + "_gicon.png";
            return File.Exists(path);
        }

        public int GetGiftednessUnlockCount()
        {
            return EnsureSaveData().m_GiftednessUnlockCount;
        }

        public List<int> GetGiftednessUnlockBranches()
        {
            return new List<int>(EnsureSaveData().m_GiftednessUnlockBranches);
        }

        // 主线天赋按顺序解锁，已解锁的行 id 为 1..解锁数。
        public List<int> GetGiftednessMainTalentIds()
        {
            var talentIds = new List<int>();
            int unlockCount = EnsureSaveData().m_GiftednessUnlockCount;
            for (int i = 1; i <= unlockCount; i++)
            {
                talentIds.Add(i);
            }
            return talentIds;
        }

        // 分支存档是 0 基行索引，天赋表 id 从 1 开始，战斗侧按 id 读表所以要 +1。
        public List<int> GetGiftednessBranchTalentIds()
        {
            var branches = EnsureSaveData().m_GiftednessUnlockBranches;
            var talentIds = new List<int>(branches.Count);
            for (int i = 0; i < branches.Count; i++)
            {
                if (branches[i] >= 0)
                {
                    talentIds.Add(branches[i] + 1);
                }
            }
            return talentIds;
        }

        public bool TryUnlockGiftednessMain(int index, int reduceCoin)
        {
            if (!m_IsLocal || index < 0)
            {
                return false;
            }

            EnsureLocalPlayerInfo();
            var saveData = EnsureSaveData();
            if (index != saveData.m_GiftednessUnlockCount)
            {
                return false;
            }

            if (!TryReduceCoinWithoutSave(reduceCoin))
            {
                return false;
            }

            saveData.m_GiftednessUnlockCount += 1;
            SavePlayerInfo();
            return true;
        }

        public bool TryUnlockGiftednessBranch(int index, int reduceCoin)
        {
            if (!m_IsLocal || index < 0)
            {
                return false;
            }

            EnsureLocalPlayerInfo();
            var saveData = EnsureSaveData();
            if (saveData.m_GiftednessUnlockBranches.Contains(index))
            {
                return false;
            }

            if (!TryReduceCoinWithoutSave(reduceCoin))
            {
                return false;
            }

            saveData.m_GiftednessUnlockBranches.Add(index);
            SavePlayerInfo();
            return true;
        }

        public BattleGameMode GetSavedBattleGameMode()
        {
            return (BattleGameMode)EnsureSaveData().m_SelectedGameMode;
        }

        public int GetSavedBattleStageId()
        {
            return EnsureSaveData().m_SelectedStageId;
        }

        public void SetSavedBattleSelection(BattleGameMode gameMode, int stageId)
        {
            if (!m_IsLocal)
            {
                return;
            }

            var saveData = EnsureSaveData();
            saveData.m_SelectedGameMode = (int)gameMode;
            saveData.m_SelectedStageId = Mathf.Max(0, stageId);
            NormalizeSaveData(saveData);
            SaveSaveData();
        }

        public void OnLocalPlayerMainStartMessage()
        {
            if(!m_IsLocal)
            {
                return;
            }
            EnsureLocalPlayerInfo();
        }

        private LobbyPlayerSaveData EnsureSaveData()
        {
            if (m_SaveData != null)
            {
                return m_SaveData;
            }

            m_SaveData = TryLoadSaveData(true);
            if (m_SaveData == null)
            {
                m_SaveData = CreateDefaultSaveData();
                SaveSaveData();
            }

            NormalizeSaveData(m_SaveData);
            return m_SaveData;
        }

        private LobbyPlayerSaveData TryLoadSaveData(bool allowRestoreBackup)
        {
            var saveService = GameSaveService.Instance;
            if (saveService == null)
            {
                return null;
            }

            if (!saveService.IsReady)
            {
                // SDK 存档尚未送达：先以默认档开局，送达后由 ApplyRemoteSave 替换。
                WaitRemoteSaveThenApply();
                return null;
            }

            return ParseRemoteSaveData(saveService.Root.m_LobbyJson);
        }

        // SDK 存档字符串（JsonUtility JSON）转换为游戏存档对象；空档返回 null。
        private LobbyPlayerSaveData ParseRemoteSaveData(string rawSave)
        {
            if (string.IsNullOrEmpty(rawSave))
            {
                return null;
            }

            try
            {
                var saveData = GameSaveService.Instance != null
                    ? GameSaveService.Instance.ParseData<LobbyPlayerSaveData>(rawSave)
                    : null;
                if (saveData == null)
                {
                    return null;
                }

                RebuildStageStarDictionary(saveData);
                NormalizeSaveData(saveData);
                return saveData;
            }
            catch (Exception e)
            {
                Debug.LogError("读取SDK玩家存档失败，将使用默认数据：" + e);
                return null;
            }
        }

        private void WaitRemoteSaveThenApply()
        {
            if (m_IsWaitingRemoteSave)
            {
                return;
            }

            m_IsWaitingRemoteSave = true;
            RenderAPI.StartCoroutine(WaitRemoteSaveCoroutine());
        }

        private IEnumerator WaitRemoteSaveCoroutine()
        {
            var saveService = GameSaveService.Instance;
            while (saveService != null && !saveService.IsReady)
            {
                yield return null;
            }

            m_IsWaitingRemoteSave = false;
            if (saveService == null)
            {
                yield break;
            }

            var remoteData = ParseRemoteSaveData(saveService.Root.m_LobbyJson);
            if (remoteData != null)
            {
                if (m_PendingLanguageChanged && m_SaveData != null)
                {
                    remoteData.m_languageType = m_SaveData.m_languageType;
                }

                if (m_PendingFirstStartConsumed)
                {
                    remoteData.m_FirstStart = 1;
                }

                m_SaveData = remoteData;
                // 进度载体 PlayerInfo 必须按新档重建，否则旧数据会在下次保存时反覆盖真档。
                m_PlayerInfo = null;
                EnsureLocalPlayerInfo();
                bool pendingFirstStart = m_PendingFirstStartConsumed;
                m_PendingLanguageChanged = false;
                m_PendingFirstStartConsumed = false;
                Debug.Log("SDK玩家存档已送达并应用。");
                if (pendingFirstStart)
                {
                    SaveSaveData();
                }
            }
        }

        private static void RebuildStageStarDictionary(LobbyPlayerSaveData saveData)
        {
            saveData.m_StageStars = new Dictionary<int, int>();
            if (saveData.m_StageStarList == null)
            {
                return;
            }

            for (int i = 0; i < saveData.m_StageStarList.Count; i++)
            {
                var entry = saveData.m_StageStarList[i];
                if (entry != null)
                {
                    saveData.m_StageStars[entry.m_StageId] = entry.m_Stars;
                }
            }
        }

        private void SaveSaveData()
        {
            if (m_SaveData == null)
            {
                return;
            }

            var pendingService = GameSaveService.Instance;
            if (pendingService != null && !pendingService.IsReady)
            {
                // SDK 存档尚未获取完成：禁止把内存数据推上云端，
                // 否则默认/半默认数据会覆盖云端真实存档。
                return;
            }

            NormalizeSaveData(m_SaveData);
            try
            {
                SyncStageStarList(m_SaveData);
                var saveService = GameSaveService.Instance;
                if (saveService != null)
                {
                    saveService.Root.m_LobbyJson = saveService.SerializeData(m_SaveData);
                    saveService.QueueSaveRoot();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("保存玩家存档到SDK失败：" + e);
            }
        }

        private static void SyncStageStarList(LobbyPlayerSaveData saveData)
        {
            if (saveData.m_StageStarList == null)
            {
                saveData.m_StageStarList = new List<StageStarEntry>();
            }
            else
            {
                saveData.m_StageStarList.Clear();
            }

            if (saveData.m_StageStars == null)
            {
                return;
            }

            foreach (var pair in saveData.m_StageStars)
            {
                saveData.m_StageStarList.Add(new StageStarEntry
                {
                    m_StageId = pair.Key,
                    m_Stars = pair.Value
                });
            }
        }

        private LobbyPlayerSaveData CreateDefaultSaveData()
        {
            return new LobbyPlayerSaveData
            {
                m_SaveVersion = m_CurrentSaveVersion,
                m_RoleName = m_DefaultLocalRoleName,
                m_UnlockedStage = 1,
                m_MissionStage = 1,
                m_SelectedGameMode = (int)BattleGameMode.Chapter,
                m_SelectedStageId = 0,
            };
        }

        private void SyncSaveDataFromPlayerInfo(LobbyPlayerSaveData saveData)
        {
            if (m_PlayerInfo == null)
            {
                return;
            }

            saveData.m_SaveVersion = m_CurrentSaveVersion;
            saveData.m_RoleName = NormalizeRoleName(m_PlayerInfo.name);
            saveData.m_Coin = m_PlayerInfo.coin;
            saveData.m_Gem = m_PlayerInfo.gem;
            saveData.m_UnlockedStage = m_PlayerInfo.stage;
            saveData.m_MissionStage = m_PlayerInfo.mission_stage;
            saveData.m_BestEndlessWave = m_PlayerInfo.master_stage;
        }

        private void ApplySaveDataToPlayerInfo(LobbyPlayerSaveData saveData)
        {
            m_PlayerInfo = new PlayerInfo();
            m_PlayerInfo.guid = 0;
            m_PlayerInfo.name = saveData.m_RoleName;
            m_PlayerInfo.cfg_id = 0;
            m_PlayerInfo.coin = saveData.m_Coin;
            m_PlayerInfo.gem = saveData.m_Gem;
            m_PlayerInfo.stage = saveData.m_UnlockedStage;
            m_PlayerInfo.mission_stage = saveData.m_MissionStage;
            m_PlayerInfo.master_stage = saveData.m_BestEndlessWave;
        }

        private void NormalizeSaveData(LobbyPlayerSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.m_SaveVersion = m_CurrentSaveVersion;
            saveData.m_RoleName = NormalizeRoleName(saveData.m_RoleName);
            saveData.m_Coin = Mathf.Max(0, saveData.m_Coin);
            saveData.m_Gem = Mathf.Max(0, saveData.m_Gem);
            saveData.m_UnlockedStage = Mathf.Max(1, saveData.m_UnlockedStage);
            saveData.m_MissionStage = Mathf.Max(saveData.m_UnlockedStage, saveData.m_MissionStage);
            saveData.m_BestEndlessWave = Mathf.Max(0, saveData.m_BestEndlessWave);
            saveData.m_GiftednessUnlockCount = Mathf.Max(0, saveData.m_GiftednessUnlockCount);
            saveData.m_GiftednessUnlockBranches = NormalizeIntList(saveData.m_GiftednessUnlockBranches);
            if (saveData.m_StageStars == null)
            {
                saveData.m_StageStars = new Dictionary<int, int>();
            }
            if (saveData.m_SelectedGameMode != (int)BattleGameMode.Chapter &&
                saveData.m_SelectedGameMode != (int)BattleGameMode.Endless &&
                saveData.m_SelectedGameMode != (int)BattleGameMode.Tutorial)
            {
                saveData.m_SelectedGameMode = (int)BattleGameMode.Chapter;
            }
            saveData.m_SelectedStageId = Mathf.Max(0, saveData.m_SelectedStageId);
        }

        private string NormalizeRoleName(string roleName)
        {
            return string.IsNullOrEmpty(roleName) ? m_DefaultLocalRoleName : roleName;
        }

        private List<int> NormalizeIntList(List<int> list)
        {
            var result = new List<int>();
            if (list == null)
            {
                return result;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];
                if (value < 0 || result.Contains(value))
                {
                    continue;
                }
                result.Add(value);
            }
            return result;
        }

        private  PlayerInput m_Input;
        public static PlayerInput Input
        {
            get
            {
                return GetInstance().m_Input;
            }
        }
        public static GuideManager GuideMgr
        {
            get
            {
                return GetInstance().m_GuideMgr;
            }
        }
        public static GuideManager GuideInstanceMgr
        {
            get
            {
                return GetInstance().m_GuideInstanceMgr;
            }
        }
        private GuideManager m_GuideMgr;
        private GuideManager m_GuideInstanceMgr;

        public int m_GMPort = 0;


        private List<SystemBaseManager> m_Systems = new List<SystemBaseManager>();

        private bool m_IsInited = false;

        public void Init()
        {
            m_Input = new PlayerInput();
            m_Systems.Add(m_Input);

            m_GuideMgr = new GuideManager();
            m_Systems.Add(m_GuideMgr);

            m_GuideInstanceMgr = new GuideManager();
            m_Systems.Add(m_GuideInstanceMgr);

            


            foreach(var mgr in m_Systems)
            {
                mgr.Init();
            }

            m_IsInited = true;

        }
        public void UnInit()
        {
            if (!m_IsInited)
            {
                return;
            }
            foreach (var mgr in m_Systems)
            {
                mgr.UnInit();
            }
            m_PlayerInfo = null;
            m_SaveData = null;


            m_Systems.Clear();  

            m_IsInited = false;
        }
        private PlayerState m_PlayerState = PlayerState.Login;
        public PlayerState GetPlayerState()
        {
            return m_PlayerState;
        }
        public void SetPlayerState(PlayerState state)
        {
            m_PlayerState = state;
        }
        public bool IsLobbyPlayerState()
        {
            return m_PlayerState == PlayerState.Lobby;
        }
        public bool IsBattlePlayerState()
        {
            return m_PlayerState == PlayerState.Battle;
        }

        public void EnsureLocalPlayerInfo()
        {
            if (!m_IsLocal)
            {
                return;
            }

            if (m_PlayerInfo != null)
            {
                return;
            }

            ApplySaveDataToPlayerInfo(EnsureSaveData());
        }

        public int GetUnlockedStage()
        {
            EnsureLocalPlayerInfo();
            return m_PlayerInfo != null ? Mathf.Max(1, m_PlayerInfo.stage) : 1;
        }

        public void SetUnlockedStage(int stage)
        {
            EnsureLocalPlayerInfo();
            if (m_PlayerInfo == null)
            {
                return;
            }

            var unlockedStage = Mathf.Max(1, stage);
            m_PlayerInfo.stage = Mathf.Max(m_PlayerInfo.stage, unlockedStage);
            m_PlayerInfo.mission_stage = Mathf.Max(m_PlayerInfo.mission_stage, m_PlayerInfo.stage);
            SavePlayerInfo();
        }
        // 首次启动标记（原 PlayerPrefs first_game_start_key，按账号存档）。
        public bool TryConsumeFirstStart()
        {
            var saveData = EnsureSaveData();
            if (saveData.m_FirstStart != 0)
            {
                return false;
            }

            saveData.m_FirstStart = 1;
            m_PendingFirstStartConsumed = true;
            SaveSaveData();
            return true;
        }

        public LanguageType GetLanguageType()
        {
            return EnsureSaveData().m_languageType;
        }
        public void SetLanguageType(LanguageType languageType)
        {
            m_PendingLanguageChanged = true;
            m_SaveData.m_languageType = languageType;
            SaveSaveData();
        }
        public int GetBestEndlessWave()
        {
            EnsureLocalPlayerInfo();
            return m_PlayerInfo != null ? Mathf.Max(0, m_PlayerInfo.master_stage) : 0;
        }

        public void SetBestEndlessWave(int wave)
        {
            EnsureLocalPlayerInfo();
            if (m_PlayerInfo == null)
            {
                return;
            }

            m_PlayerInfo.master_stage = Mathf.Max(m_PlayerInfo.master_stage, Mathf.Max(0, wave));
            SavePlayerInfo();
        }

        public int GetStageStar(int stageId)
        {
            var saveData = EnsureSaveData();
            int star;
            saveData.m_StageStars.TryGetValue(stageId, out star);
            return star;
        }

        public void SetStageStar(int stageId, int star)
        {
            if (!m_IsLocal || stageId <= 0)
            {
                return;
            }

            var saveData = EnsureSaveData();
            int oldStar;
            saveData.m_StageStars.TryGetValue(stageId, out oldStar);
            if (star <= oldStar)
            {
                return;
            }

            saveData.m_StageStars[stageId] = star;
            SaveSaveData();
        }

        public void AddCoin(int coin)
        {
            if (coin <= 0)
            {
                return;
            }

            EnsureLocalPlayerInfo();
            if (m_PlayerInfo == null)
            {
                return;
            }

            m_PlayerInfo.SetMoney(MoneyId.CoinId, m_PlayerInfo.coin + coin);
            SavePlayerInfo();
        }
        public bool ReduceCoin(int reduceCoin)
        {
            if (!TryReduceCoinWithoutSave(reduceCoin))
            {
                return false;
            }

            SavePlayerInfo();
            return true;
        }

        private bool TryReduceCoinWithoutSave(int reduceCoin)
        {
            if(reduceCoin <= 0)
            {
                return false;
            }

            EnsureLocalPlayerInfo();
            if (m_PlayerInfo == null || reduceCoin > m_PlayerInfo.coin)
            {
                return false;
            }

            m_PlayerInfo.SetMoney(MoneyId.CoinId, m_PlayerInfo.coin - reduceCoin);
            return true;
        }

    }
}
