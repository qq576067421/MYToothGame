using System.Collections.Generic;

namespace GameDll
{
    public enum TowerDefendLeaderboardSubmissionState
    {
        None = 0,
        PendingSdk = 1,
        Submitted = 2,
        Failed = 3,
    }

    public class TowerDefendLeaderboardSubmissionData
    {
        public TowerDefendLeaderboardSubmissionState m_State = TowerDefendLeaderboardSubmissionState.None;
        public int m_Score;
        public int m_Rank = -1;
        public string m_StatusText = string.Empty;
        public string m_SubmitTicket = string.Empty;
    }

    public class BattleResultMonsterDetailData
    {
        public long m_ConfigId;
        public int m_MonsterKind;
        public string m_Name = string.Empty;
        public string m_Icon = string.Empty;
        public int m_KillCount;
    }

    public class BattleResultData
    {
        public int m_WorldId;
        public long m_FightId;
        public bool m_IsLocal;
        public int m_Stage;
        public string m_StageName = string.Empty;
        public BattleGameMode m_GameMode;
        public FinishReason m_FinishReason;
        public GroupId m_WinGroup;
        public float m_UseTime;
        public long m_WinnerId = -1;
        public int m_BaseHealth;
        public int m_BaseMaxHealth;
        public int m_StarCount;
        public int m_CurrentWave;
        public int m_BestProgressWave;
        public int m_NormalMonsterKillCount;
        public int m_EliteMonsterKillCount;
        public int m_BossMonsterKillCount;
        public int m_KillRewardGold;
        public int m_ClearRewardGold;
        public int m_TotalRewardGold;
        public int m_UnlockedStage;
        public int m_BestEndlessWave;
        public List<BattleResultMonsterDetailData> m_MonsterKillDetails = new List<BattleResultMonsterDetailData>();
        public TowerDefendLeaderboardSubmissionData m_LeaderboardSubmission = new TowerDefendLeaderboardSubmissionData();

        public byte m_Snapshot;
        public byte m_Record;
        public byte m_SnapshotUp;
        public byte m_RecordUp;
    }
}
