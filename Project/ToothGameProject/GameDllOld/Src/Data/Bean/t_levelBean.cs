/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_levelBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_levelBean:HF.BeanBase
{
private static string m_FileName = "t_levelBean.csv";
    public override long GetId_long()

    {
        return t_id;
    }

    private long _t_id;
    public long t_id{ get { return _t_id; }}
    private int _t_1Player_exp;
    public int t_1Player_exp{ get { return _t_1Player_exp; }}
    private int _t_2Player_exp;
    public int t_2Player_exp{ get { return _t_2Player_exp; }}
    private int _t_3Player_exp;
    public int t_3Player_exp{ get { return _t_3Player_exp; }}
    private int _t_4Player_exp;
    public int t_4Player_exp{ get { return _t_4Player_exp; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_levelBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_levelBean> m_Dic = new Dictionary<long, t_levelBean>(); 
    public static t_levelBean GetConfig(long key, bool check_null = true)
    { 
        t_levelBean bean = null; var className = "t_levelBean";
        
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
    public void CopyFrom(t_levelBean source)
    {
        _t_id = source._t_id;
        _t_1Player_exp = source._t_1Player_exp;
        _t_2Player_exp = source._t_2Player_exp;
        _t_3Player_exp = source._t_3Player_exp;
        _t_4Player_exp = source._t_4Player_exp;
    }
    private static t_levelBean GetCSVConfigImp(long key)
    {
        t_levelBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_levelBean();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_1Player_exp = ReadInt(datas[index++]);
            bean._t_2Player_exp = ReadInt(datas[index++]);
            bean._t_3Player_exp = ReadInt(datas[index++]);
            bean._t_4Player_exp = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_levelBean GetConfigImp(long key)
    {
        t_levelBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_levelBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_levelBean();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_1Player_exp = SqliteDataManager_ReadInt();
            bean._t_2Player_exp = SqliteDataManager_ReadInt();
            bean._t_3Player_exp = SqliteDataManager_ReadInt();
            bean._t_4Player_exp = SqliteDataManager_ReadInt();
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