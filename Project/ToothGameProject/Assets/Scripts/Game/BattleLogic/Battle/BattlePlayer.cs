using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDll
{
    //带入战斗的数据，背包数据和技能点数等不带入，相关数据可以装备到装备和点击升级技能转换为技能数据
    public class BattlePlayer
    {
        public String m_Name = String.Empty;
        public long m_ID;
        public int mappedIndex;
        public long m_RoleCfgId;
        public int m_RoleLevel = 1;
        public bool m_IsAI;
        public bool m_Prepare;
        public GroupId m_Group = GroupId.GuardGroupId;
        public int m_SeatId;
        public int m_HPPercent = 10000;
        public int m_MagicPercent = 10000;
        public long m_BigWeaponCfgId;
        public int m_BigWeaponLevel;
        public List<long> m_Skills = new List<long>();
        public List<long> m_Equips = new List<long>();

        public List<int> m_Talents = new List<int>();
    }
}
