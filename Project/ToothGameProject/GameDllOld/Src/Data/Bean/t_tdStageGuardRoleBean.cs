/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_tdStageGuardRoleBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_tdStageGuardRoleBean:HF.BeanBase
{
private static string m_FileName = "t_tdStageGuardRoleBean.csv";
    public override int GetId_int()

    {
        return t_id;
    }

    private int _t_id;
    public int t_id{ get { return _t_id; }}
    private List<long> _t_guard_role_ids;
    public ReadOnlyCollection<long> t_guard_role_ids;
    private static List<int> m_Keys = new List<int>();
    public static List<int> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_tdStageGuardRoleBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<int, t_tdStageGuardRoleBean> m_Dic = new Dictionary<int, t_tdStageGuardRoleBean>(); 
    public static t_tdStageGuardRoleBean GetConfig(int key, bool check_null = true)
    { 
        t_tdStageGuardRoleBean bean = null; var className = "t_tdStageGuardRoleBean";
        
        if (m_Dic.TryGetValue(key, out bean))
        {
            return bean;
        }
        else
        {
            if (UseCsv())
            {
                bean = GetCSVConfigImp(key);
            }
            else
            {
                bean = GetConfigImp(key);
            }
            if (bean != null)
            {
                m_Dic.Add(key, bean);
            }
            if(check_null && bean == null)
            {
                LogWarning("not find config " + className +":" + key);
            }
            return bean;
        }
    }
    public static void ClearConfig()
    {
        m_Dic.Clear();
    }
    public void CopyFrom(t_tdStageGuardRoleBean source)
    {
        _t_id = source._t_id;
        _t_guard_role_ids = source._t_guard_role_ids;
        t_guard_role_ids = source.t_guard_role_ids;
    }
    private static t_tdStageGuardRoleBean GetCSVConfigImp(int key)
    {
        t_tdStageGuardRoleBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_tdStageGuardRoleBean();
            bean._t_id = ReadInt(datas[index++]);
            bean._t_guard_role_ids = ReadLongArray(datas[index++]);
            bean.t_guard_role_ids = GetReadOnlyArray(bean._t_guard_role_ids);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_tdStageGuardRoleBean GetConfigImp(int key)
    {
        t_tdStageGuardRoleBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_tdStageGuardRoleBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_tdStageGuardRoleBean();
            bean._t_id = SqliteDataManager_ReadInt();
            bean._t_guard_role_ids = SqliteDataManager_ReadLongArray();
            bean.t_guard_role_ids = GetReadOnlyArray(bean._t_guard_role_ids);
        }
        SqliteDataManager_EndRead();
        StringBuilder.Clear();
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

}
}