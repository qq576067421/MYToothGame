/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_tdRoleGrowBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_tdRoleGrowBean:HF.BeanBase
{
private static string m_FileName = "t_tdRoleGrowBean.csv";
    public override long GetId_long()

    {
        return t_id;
    }

    private long _t_id;
    public long t_id{ get { return _t_id; }}
    private List<List<long>> _t_runtime_buff_ids;
    public ReadOnlyCollection<ReadOnlyCollection<long>> t_runtime_buff_ids;
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_tdRoleGrowBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_tdRoleGrowBean> m_Dic = new Dictionary<long, t_tdRoleGrowBean>(); 
    public static t_tdRoleGrowBean GetConfig(long key, bool check_null = true)
    { 
        t_tdRoleGrowBean bean = null; var className = "t_tdRoleGrowBean";
        
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
    public void CopyFrom(t_tdRoleGrowBean source)
    {
        _t_id = source._t_id;
        _t_runtime_buff_ids = source._t_runtime_buff_ids;
        t_runtime_buff_ids = source.t_runtime_buff_ids;
    }
    private static t_tdRoleGrowBean GetCSVConfigImp(long key)
    {
        t_tdRoleGrowBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_tdRoleGrowBean();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_runtime_buff_ids = ReadLongArray2(datas[index++]);
            bean.t_runtime_buff_ids = GetReadOnlyArray(bean._t_runtime_buff_ids);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_tdRoleGrowBean GetConfigImp(long key)
    {
        t_tdRoleGrowBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_tdRoleGrowBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_tdRoleGrowBean();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_runtime_buff_ids = SqliteDataManager_ReadLongArray2();
            bean.t_runtime_buff_ids = GetReadOnlyArray(bean._t_runtime_buff_ids);
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