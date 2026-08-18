using LCL;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GameDll;

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
    }
    public class LobbyPlayer
    {
        private const int m_CurrentSaveVersion = 1;
        private const string m_SaveFileName = "save.json";
        private const string m_SaveKey = "player";
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
            try
            {
                if (!ES3.FileExists(m_SaveFileName) || !ES3.KeyExists(m_SaveKey, m_SaveFileName))
                {
                    return null;
                }

                var saveData = ES3.Load<LobbyPlayerSaveData>(m_SaveKey, m_SaveFileName, CreateDefaultSaveData());
                NormalizeSaveData(saveData);
                return saveData;
            }
            catch (Exception e)
            {
                if (allowRestoreBackup)
                {
                    Debug.LogWarning("读取玩家存档失败，尝试恢复备份：" + e);
                    try
                    {
                        if (ES3.RestoreBackup(m_SaveFileName))
                        {
                            return TryLoadSaveData(false);
                        }
                    }
                    catch (Exception restoreException)
                    {
                        Debug.LogError("恢复玩家存档备份失败：" + restoreException);
                    }
                }

                Debug.LogError("读取玩家存档失败，将使用默认数据：" + e);
                return null;
            }
        }

        private void SaveSaveData()
        {
            if (m_SaveData == null)
            {
                return;
            }

            NormalizeSaveData(m_SaveData);
            try
            {
                if (ES3.FileExists(m_SaveFileName))
                {
                    ES3.CreateBackup(m_SaveFileName);
                }
                ES3.Save(m_SaveKey, m_SaveData, m_SaveFileName);
            }
            catch (Exception e)
            {
                Debug.LogError("保存玩家存档失败：" + e);
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
