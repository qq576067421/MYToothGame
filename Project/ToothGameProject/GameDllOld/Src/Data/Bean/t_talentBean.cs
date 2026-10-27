/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_talentBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_talentBean:HF.BeanBase
{
private static string m_FileName = "t_talentBean.csv";
    public override long GetId_long()

    {
        return t_id;
    }

    private long _t_id;
    public long t_id{ get { return _t_id; }}
    private int _t_atk;
    public int t_atk{ get { return _t_atk; }}
    private int _t_crit;
    public int t_crit{ get { return _t_crit; }}
    private int _t_crit_damage;
    public int t_crit_damage{ get { return _t_crit_damage; }}
    private int _t_amp;
    public int t_amp{ get { return _t_amp; }}
    private int _t_unlock_gold;
    public int t_unlock_gold{ get { return _t_unlock_gold; }}
    private int _t_special_talent_type;
    public int t_special_talent_type{ get { return _t_special_talent_type; }}
    private int _t_special_talent_value;
    public int t_special_talent_value{ get { return _t_special_talent_value; }}
    private int _t_special_talent_gold;
    public int t_special_talent_gold{ get { return _t_special_talent_gold; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_talentBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_talentBean> m_Dic = new Dictionary<long, t_talentBean>(); 
    public static t_talentBean GetConfig(long key, bool check_null = true)
    { 
        t_talentBean bean = null; var className = "t_talentBean";
        
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
    public void CopyFrom(t_talentBean source)
    {
        _t_id = source._t_id;
        _t_atk = source._t_atk;
        _t_crit = source._t_crit;
        _t_crit_damage = source._t_crit_damage;
        _t_amp = source._t_amp;
        _t_unlock_gold = source._t_unlock_gold;
        _t_special_talent_type = source._t_special_talent_type;
        _t_special_talent_value = source._t_special_talent_value;
        _t_special_talent_gold = source._t_special_talent_gold;
    }
    private static t_talentBean GetCSVConfigImp(long key)
    {
        t_talentBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_talentBean();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_atk = ReadInt(datas[index++]);
            bean._t_crit = ReadInt(datas[index++]);
            bean._t_crit_damage = ReadInt(datas[index++]);
            bean._t_amp = ReadInt(datas[index++]);
            bean._t_unlock_gold = ReadInt(datas[index++]);
            bean._t_special_talent_type = ReadInt(datas[index++]);
            bean._t_special_talent_value = ReadInt(datas[index++]);
            bean._t_special_talent_gold = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_talentBean GetConfigImp(long key)
    {
        t_talentBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_talentBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_talentBean();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_atk = SqliteDataManager_ReadInt();
            bean._t_crit = SqliteDataManager_ReadInt();
            bean._t_crit_damage = SqliteDataManager_ReadInt();
            bean._t_amp = SqliteDataManager_ReadInt();
            bean._t_unlock_gold = SqliteDataManager_ReadInt();
            bean._t_special_talent_type = SqliteDataManager_ReadInt();
            bean._t_special_talent_value = SqliteDataManager_ReadInt();
            bean._t_special_talent_gold = SqliteDataManager_ReadInt();
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