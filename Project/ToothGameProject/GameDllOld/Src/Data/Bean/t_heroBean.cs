/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_heroBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_heroBean:HF.BeanBase
{
private static string m_FileName = "t_heroBean.csv";
    public override long GetId_long()

    {
        return t_id;
    }

    private long _t_id;
    public long t_id{ get { return _t_id; }}
    private string _t_name;
    public string t_name{ get { return _t_name; }}
    private string _t_desc;
    public string t_desc{ get { return _t_desc; }}
    private List<long> _t_normal_skill_id;
    public ReadOnlyCollection<long> t_normal_skill_id;
    private List<long> _t_auto_skill_id;
    public ReadOnlyCollection<long> t_auto_skill_id;
    private List<long> _t_skill_id;
    public ReadOnlyCollection<long> t_skill_id;
    private long _t_model;
    public long t_model{ get { return _t_model; }}
    private string _t_head;
    public string t_head{ get { return _t_head; }}
    private string _t_prepare_fill;
    public string t_prepare_fill{ get { return _t_prepare_fill; }}
    private string _t_prepare;
    public string t_prepare{ get { return _t_prepare; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_heroBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_heroBean> m_Dic = new Dictionary<long, t_heroBean>(); 
    public static t_heroBean GetConfig(long key, bool check_null = true)
    { 
        t_heroBean bean = null; var className = "t_heroBean";
        
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
    public void CopyFrom(t_heroBean source)
    {
        _t_id = source._t_id;
        _t_name = source._t_name;
        _t_desc = source._t_desc;
        _t_normal_skill_id = source._t_normal_skill_id;
        t_normal_skill_id = source.t_normal_skill_id;
        _t_auto_skill_id = source._t_auto_skill_id;
        t_auto_skill_id = source.t_auto_skill_id;
        _t_skill_id = source._t_skill_id;
        t_skill_id = source.t_skill_id;
        _t_model = source._t_model;
        _t_head = source._t_head;
        _t_prepare_fill = source._t_prepare_fill;
        _t_prepare = source._t_prepare;
    }
    private static t_heroBean GetCSVConfigImp(long key)
    {
        t_heroBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_heroBean();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_name = ReadString(datas[index++]);
            bean._t_desc = ReadString(datas[index++]);
            bean._t_normal_skill_id = ReadLongArray(datas[index++]);
            bean.t_normal_skill_id = GetReadOnlyArray(bean._t_normal_skill_id);
            bean._t_auto_skill_id = ReadLongArray(datas[index++]);
            bean.t_auto_skill_id = GetReadOnlyArray(bean._t_auto_skill_id);
            bean._t_skill_id = ReadLongArray(datas[index++]);
            bean.t_skill_id = GetReadOnlyArray(bean._t_skill_id);
            bean._t_model = ReadLong(datas[index++]);
            bean._t_head = ReadString(datas[index++]);
            bean._t_prepare_fill = ReadString(datas[index++]);
            bean._t_prepare = ReadString(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_heroBean GetConfigImp(long key)
    {
        t_heroBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_heroBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_heroBean();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_name = SqliteDataManager_ReadString();
            bean._t_desc = SqliteDataManager_ReadString();
            bean._t_normal_skill_id = SqliteDataManager_ReadLongArray();
            bean.t_normal_skill_id = GetReadOnlyArray(bean._t_normal_skill_id);
            bean._t_auto_skill_id = SqliteDataManager_ReadLongArray();
            bean.t_auto_skill_id = GetReadOnlyArray(bean._t_auto_skill_id);
            bean._t_skill_id = SqliteDataManager_ReadLongArray();
            bean.t_skill_id = GetReadOnlyArray(bean._t_skill_id);
            bean._t_model = SqliteDataManager_ReadLong();
            bean._t_head = SqliteDataManager_ReadString();
            bean._t_prepare_fill = SqliteDataManager_ReadString();
            bean._t_prepare = SqliteDataManager_ReadString();
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