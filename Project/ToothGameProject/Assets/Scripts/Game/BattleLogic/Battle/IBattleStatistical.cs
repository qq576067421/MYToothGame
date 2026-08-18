using System.Collections.Generic;

namespace GameDll
{
    public class StatisticalData
    {
        public long m_KillerPlayerId;
        public long m_OwnerPlayerId;
        public int m_Num;
    }
    public class IBattleStatistical
    {
        private int m_WellDeadCount = 0;
        //建筑被摧毁的数量
        private int m_BuildingDeadCount = 0;
        private int m_MonsterDoorDeadCount = 0;
        private int m_TowerDeadCount = 0;
        private int m_BossDeadCount = 0;
        private List<StatisticalData> m_BuyFromShopCount = new List<StatisticalData>();
        private List<StatisticalData> m_BigDragonCount = new List<StatisticalData>();
        private List<StatisticalData> m_SmallDragonCount = new List<StatisticalData>();
        protected List<StatisticalData> m_MonsterCount = new List<StatisticalData>();
        protected List<StatisticalData> m_TrapCount = new List<StatisticalData>();

        protected List<StatisticalData> m_HeroDeadCount = new List<StatisticalData>();
        protected List<StatisticalData> m_KillHeroCount = new List<StatisticalData>();

        public virtual void Init()
        {

        }

        public virtual void Update(float dt)
        {

        }

        public virtual void Destroy()
        {

        }
        
        public virtual int ReadGuardHeroDeadCount()
        {
            return 0;
        }
        
        public virtual int ReadPushHeroDeadCount()
        {
            return 0;
        }
        public virtual int GetHeroDeadCount(long owner_player_id)
        {
            int count = m_HeroDeadCount.Count;
            for (int i = 0; i < count; ++i)
            {
                var data = m_HeroDeadCount[i];
                if (data.m_OwnerPlayerId == owner_player_id)
                {
                    return data.m_Num;
                }
            }
            return 0;
        }
        public virtual int GetKillHeroCount(long killer_player_id)
        {
            int count = m_KillHeroCount.Count;
            for (int i = 0; i < count; ++i)
            {
                var data = m_KillHeroCount[i];
                if (data.m_KillerPlayerId == killer_player_id)
                {
                    return data.m_Num;
                }
            }
            return 0;
        }
        public virtual void OnHeroDead(PlayerHero hero, int attack_id, long kill_player_id)
        {
            long owner_player_id = hero.ReadBattlePlayerId();
            //被击杀的数据统计
            {
                bool find = false;
                int count = m_HeroDeadCount.Count;
                for (int i = 0; i < count; ++i)
                {
                    var buy = m_HeroDeadCount[i];
                    if (buy.m_OwnerPlayerId == owner_player_id)
                    {
                        buy.m_Num++;
                        find = true;
                        break;
                    }
                }
                if (!find)
                {
                    StatisticalData data = new StatisticalData();
                    //第一次击杀的玩家
                    data.m_KillerPlayerId = kill_player_id;
                    data.m_OwnerPlayerId = owner_player_id;
                    data.m_Num = 1;
                    m_HeroDeadCount.Add(data);
                }
            }

            //击杀他人的数据统计
            {
                bool find = false;
                int count = m_KillHeroCount.Count;
                for (int i = 0; i < count; ++i)
                {
                    var buy = m_KillHeroCount[i];
                    if (buy.m_KillerPlayerId == kill_player_id)
                    {
                        buy.m_Num++;
                        find = true;
                        break;
                    }
                }
                if (!find)
                {
                    StatisticalData data = new StatisticalData();
                    data.m_KillerPlayerId = kill_player_id;
                    data.m_OwnerPlayerId = owner_player_id;
                    data.m_Num = 1;
                    m_KillHeroCount.Add(data);
                }
            }
        }
        public virtual int ReadWellDeadCount()
        {
            return m_WellDeadCount;
        }


        public virtual void OnWellDead(int wellId, int attack_id, long player_id)
        {
            m_WellDeadCount++;
            RenderEvent.Event.OnWellDead(wellId, attack_id);
        }

        public virtual int GetMonsterDoorDeadCount()
        {
            return m_MonsterDoorDeadCount;
        }
        public virtual void OnMonsterDoorDead(int entity_id, int attack_id, long player_id)
        {
            m_MonsterDoorDeadCount++;
        }

        public virtual int GetMonsterDeadCount(long player_id)
        {
            int count = m_MonsterCount.Count;
            for (int i = 0; i < count; ++i)
            {
                var data = m_MonsterCount[i];
                if (data.m_KillerPlayerId == player_id)
                {
                    return data.m_Num;
                }
            }
            return 0;
        }

        public virtual void OnMonsterDead(int entity_id, int attack_id, long player_id)
        {
            bool find = false;
            int count = m_MonsterCount.Count;
            for (int i = 0; i < count; ++i)
            {
                var buy = m_MonsterCount[i];
                if (buy.m_KillerPlayerId == player_id)
                {
                    buy.m_Num++;
                    find = true;
                    break;
                }
            }
            if (!find)
            {
                StatisticalData data = new StatisticalData();
                data.m_KillerPlayerId = player_id;
                data.m_Num = 1;
                m_MonsterCount.Add(data);
            }
        }

        public virtual int GetTrapDeadCount(long player_id)
        {
            int count = m_TrapCount.Count;
            for (int i = 0; i < count; ++i)
            {
                var data = m_TrapCount[i];
                if (data.m_KillerPlayerId == player_id)
                {
                    return data.m_Num;
                }
            }
            return 0;
        }

        public virtual void OnTrapDead(int entity_id, int attack_id, long player_id)
        {
            bool find = false;
            int count = m_TrapCount.Count;
            for (int i = 0; i < count; ++i)
            {
                var buy = m_TrapCount[i];
                if (buy.m_KillerPlayerId == player_id)
                {
                    buy.m_Num++;
                    find = true;
                    break;
                }
            }
            if (!find)
            {
                StatisticalData data = new StatisticalData();
                data.m_KillerPlayerId = player_id;
                data.m_Num = 1;
                m_TrapCount.Add(data);
            }
        }

        public virtual int GetBossDeadCount()
        {
            return m_BossDeadCount;
        }
        public virtual void OnBossDead(int entity_id, int attack_id, long player_id)
        {
            m_BossDeadCount++;
        }

        public virtual int GetBuildingDeadCount()
        {
            return m_BuildingDeadCount;
        }
        public virtual void OnBuildingDead(int entity_id, int attack_id, long player_id)
        {
            m_BuildingDeadCount++;
        }

        public virtual int GetBuyFromShopCount(long player_id)
        {
            int count = m_BuyFromShopCount.Count;

            for (int i = 0; i < count; ++i)
            {
                var buy = m_BuyFromShopCount[i];
                if (buy.m_KillerPlayerId == player_id)
                {
                    return buy.m_Num;
                }
            }
            return 0;
        }
        //这里是统计的商店购买
        public virtual void OnBuyFromShop(int npc_cfg_id, int visitor, long player_id)
        {
            bool find = false;
            int count = m_BuyFromShopCount.Count;
            for (int i = 0; i < count; ++i)
            {
                var buy = m_BuyFromShopCount[i];
                if (buy.m_KillerPlayerId == player_id)
                {
                    buy.m_Num++;
                    find = true;
                    break;
                }
            }
            if (!find)
            {
                StatisticalData data = new StatisticalData();
                data.m_KillerPlayerId = player_id;
                data.m_Num = 1;
                m_BuyFromShopCount.Add(data);
            }
        }


        public virtual int GetDragonDeadCount(bool is_small, long player_id)
        {
            var datas = is_small ? m_SmallDragonCount : m_BigDragonCount;

            int count = datas.Count;

            for (int i = 0; i < count; ++i)
            {
                var buy = datas[i];
                if (buy.m_KillerPlayerId == player_id)
                {
                    return buy.m_Num;
                }
            }
            return 0;
        }
        public virtual void OnDragonDead(int entity_id, int attack_id, long player_id)
        {
            var dragon = BattleManager.GetObjectManager().ReadPropertyEntityById(entity_id);
            if (dragon == null)
            {
                return;
            }
            var datas = dragon.ReadIsSmallMonster() ? m_SmallDragonCount : m_BigDragonCount;
            bool find = false;
            int count = datas.Count;
            for (int i = 0; i < count; ++i)
            {
                var buy = datas[i];
                if (buy.m_KillerPlayerId == player_id)
                {
                    buy.m_Num++;
                    find = true;
                    break;
                }
            }
            if (!find)
            {
                StatisticalData data = new StatisticalData();
                data.m_KillerPlayerId = player_id;
                data.m_Num = 1;
                datas.Add(data);
            }
        }
    }
}
