using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDll
{
    public class IBattleProgress
    {
        public virtual void Init()
        {
        }
        public virtual void Destroy()
        {

        }
        public virtual void Update(float dt)
        {
        }
        public virtual int GetState()
        {
            return 0;
        }
        public virtual void SetState(int state)
        {

        }
        public virtual float ReadStageTime()
        {
            return 0;
        }
        protected float m_GameTotalTime = 60 * 60;
        public virtual float GetGameTotalTime()
        {
            return m_GameTotalTime;
        }
        public virtual void SetGameTotalTime(float total_time)
        {
            m_GameTotalTime = total_time;  
        }
        public virtual float ReadGameLeftTime()
        {
            return 30 * 60;
        }
        public virtual BattleResultData OnFinishGame(FinishReason giveup, object userData)
        {
            BattleResultData rd = new BattleResultData();
            rd.m_UseTime = BattleManager.ReadBattleTime();
            var battle = BattleManager.GetBattle();
            rd.m_FightId = battle != null ? battle.GetFightId() : 0;
            var battleData = battle != null ? battle.GetBattleData() : null;
            var data = battleData != null ? battleData.GetNormalBattleData() : null;
            if (data != null)
            {
                rd.m_WorldId = data.m_WorldId;
                rd.m_Snapshot = data.m_Snapshot;
                rd.m_SnapshotUp = data.m_SnapshotUp;
                rd.m_Record = data.m_Record;
                rd.m_RecordUp = data.m_RecordUp;
            }

            return rd;
        }

        //用于广告等继续挑战本关，一般都是单人模式
        public virtual void FightAgain()
        {

        }
    }
}
