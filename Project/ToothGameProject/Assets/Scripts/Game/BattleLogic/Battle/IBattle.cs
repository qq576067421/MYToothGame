using MonoBean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDll
{
    public class IBattle
    {
        protected ObjectManager m_ObjectManager;
        protected PlayableEffectObjPool<EffectObj> m_EffectObjPool = null;
        protected PlayableEffectObjPool<BulletObj> m_BulletObjPool = null;
        protected PlayableEffectObjPool<PaodanObj> m_PaodanObjPool = null;
        protected IBattleProgress m_Process = null;
        protected List<BattlePlayer> m_BattlePlayers = new List<BattlePlayer>();

        public BattlePlayer GetPlayer(long playerId)
        {
            foreach(var player in m_BattlePlayers)
            {
                if(player.m_ID == playerId)
                {
                    return player;
                }
            }
            return null;
        }

        public virtual IBattleSpawer ReadBattleSpawer()
        {
            return null;
        }

        public List<BattlePlayer> ReadPlayers()
        {
            return m_BattlePlayers;
        }

        public virtual void InitPlayers(List<Packet_BattlePlayer> m_Players)
        {
            foreach (var pp in m_Players)
            {
                var battlePlayer = new BattlePlayer();

                battlePlayer.m_ID = pp.m_ID;
                battlePlayer.m_Name = pp.m_Name;
                battlePlayer.m_RoleCfgId = pp.m_RoleCfgId;
                battlePlayer.m_IsAI = pp.m_IsAI == 1;
                battlePlayer.m_RoleLevel = pp.m_role_level;
                battlePlayer.m_Skills.AddRange(pp.m_Skills);
                battlePlayer.m_Equips.AddRange(pp.m_Equips);
                battlePlayer.m_BigWeaponCfgId = pp.m_BigWeaponCfgId;
                battlePlayer.m_BigWeaponLevel = pp.m_BigWeaponLevel;
                battlePlayer.m_Group = (GroupId)pp.m_Group;
                battlePlayer.m_SeatId = pp.m_SeatId;
                battlePlayer.m_HPPercent = pp.m_HPPercent;
                battlePlayer.m_MagicPercent = pp.m_MagicPercent;

                AddPlayer(battlePlayer);
            }
        }
        public void AddPlayer(BattlePlayer player)
        {
            m_BattlePlayers.Add(player);
        }
        public IBattleProgress GetBattleProgress()
        {
            return m_Process;
        }
        public virtual IBattleStatistical GetBattleStat()
        {
            return null;
        }

        public PlayableEffectObjPool<EffectObj> GetEffectObjPool()
        {
            return m_EffectObjPool;
        }
        public PlayableEffectObjPool<BulletObj> GetBulletObjPool()
        {
            return m_BulletObjPool;
        }
        public PlayableEffectObjPool<PaodanObj> GetPaodanObjPool()
        {
            return m_PaodanObjPool;
        }

        public virtual int GetStage()
        {
            return 0;
        }

        public ObjectManager GetObjectManager()
        {
            return m_ObjectManager;
        }
        protected BattleData m_BattleData;
        public BattleData GetBattleData()
        {
            return m_BattleData;
        }
        public bool IsOpenRecord()
        {
            var data = m_BattleData.GetNormalBattleData();
            return data.m_Record == 1;
        }
        public bool IsOpenRecordUp()
        {
            var data = m_BattleData.GetNormalBattleData();
            return data.m_RecordUp == 1;
        }
        public bool IsOpenSnapshot()
        {
            var data = m_BattleData.GetNormalBattleData();
            return data.m_Snapshot == 1;
        }
        public bool IsOpenSnapshotUp()
        {
            var data = m_BattleData.GetNormalBattleData();
            return data.m_SnapshotUp == 1;
        }
        public virtual void OnCreate(BattleData info)
        {
            m_BattleData = info;
            m_EffectObjPool = new PlayableEffectObjPool<EffectObj>();
            m_EffectObjPool.OnCreate();

            m_BulletObjPool = new PlayableEffectObjPool<BulletObj>();
            m_BulletObjPool.OnCreate();

            m_PaodanObjPool = new PlayableEffectObjPool<PaodanObj>();
            m_PaodanObjPool.OnCreate();


        }

        public virtual void ResetBattleEvent()
        {

        }

        public virtual void OnLoadMap()
        {

        }
        public virtual void Update(float dt)
        {

        }

        public virtual void OnDrawGizmos()
        {
            if(m_ObjectManager != null)
            {
                m_ObjectManager.OnDrawGizmos();
            }
        }

        public virtual void UpdateRender(float dt)
        {
            if(m_ObjectManager != null)
            {
                m_ObjectManager.UpdateRender();
            }
        }



        public virtual void OnRelease()
        {
            if (m_EffectObjPool != null)
            {
                m_EffectObjPool.OnRelease();
                m_EffectObjPool = null;
            }
            if(m_BulletObjPool != null)
            {
                m_BulletObjPool.OnRelease();
                m_BulletObjPool = null;
            }

            if (m_PaodanObjPool != null)
            {
                m_PaodanObjPool.OnRelease();
                m_PaodanObjPool = null;
            }

            if(m_ObjectManager != null)
            {
                m_ObjectManager.ClearAll();
                m_ObjectManager = null;
            }
        }

        public virtual long GetFightId()
        {
            return 0;
        }

        public virtual int GetWildWave()
        {
            return 0;
        }

        public virtual void SetIsPlayingAds(bool play_ads)
        {

        }

        protected bool m_IsBattlePause = false;
        public bool IsBattlePause()
        {
            return m_IsBattlePause;
        }
        public void SetBattlePause(bool pause)
        {
            m_IsBattlePause = pause;
        }

        // GM 调试冻结只服务调试流程，不等同于正式战斗暂停。
        protected bool m_GM_IsPause = false;
        public bool GM_IsPause()
        {
            return m_GM_IsPause;
        }
        public void GM_SetPause(bool pause)
        {
            m_GM_IsPause = pause;
        }
    }
}
