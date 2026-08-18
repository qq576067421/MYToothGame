using System.Collections.Generic;

namespace GameDll
{
    public sealed class TowerDefendRoleSnapshot
    {
        public long m_NormalSkillCfgId;
        public long m_AutoSkillCfgId;
        public List<long> m_ActiveSkillCfgIds = new List<long>();
        public List<long> m_RuntimeBuffCfgIds = new List<long>();
    }
}
