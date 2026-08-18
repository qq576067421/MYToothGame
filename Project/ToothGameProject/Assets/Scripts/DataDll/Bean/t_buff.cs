/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_buff
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_buff:MonoBean.BeanBase
{
private static string m_FileName = "t_buff.csv";
    public override long GetId_long()

    {
        return t_id;
    }

    private long _t_id;
    public long t_id{ get { return _t_id; }}
    private long _t_class_id;
    public long t_class_id{ get { return _t_class_id; }}
    private int _t_buff_during;
    public int t_buff_during{ get { return _t_buff_during; }}
    private int _t_buff_gap;
    public int t_buff_gap{ get { return _t_buff_gap; }}
    private int _t_buff_trigger_buff;
    public int t_buff_trigger_buff{ get { return _t_buff_trigger_buff; }}
    private int _t_buff_trigger_layers;
    public int t_buff_trigger_layers{ get { return _t_buff_trigger_layers; }}
    private int _t_buff_add_odd;
    public int t_buff_add_odd{ get { return _t_buff_add_odd; }}
    private List<long> _t_buff_param_id;
    public ReadOnlyCollection<long> t_buff_param_id;
    private long _t_descId;
    public long t_descId{ get { return _t_descId; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_buff", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_buff> m_Dic = new Dictionary<long, t_buff>(); 
    public static t_buff GetConfig(long key, bool check_null = true)
    { 
        t_buff bean = null; var className = "t_buff";
        
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
    public void CopyFrom(t_buff source)
    {
        _t_id = source._t_id;
        _t_class_id = source._t_class_id;
        _t_buff_during = source._t_buff_during;
        _t_buff_gap = source._t_buff_gap;
        _t_buff_trigger_buff = source._t_buff_trigger_buff;
        _t_buff_trigger_layers = source._t_buff_trigger_layers;
        _t_buff_add_odd = source._t_buff_add_odd;
        _t_buff_param_id = source._t_buff_param_id;
        t_buff_param_id = source.t_buff_param_id;
        _t_descId = source._t_descId;
    }
    private static t_buff GetCSVConfigImp(long key)
    {
        t_buff bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_buff();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_class_id = ReadLong(datas[index++]);
            bean._t_buff_during = ReadInt(datas[index++]);
            bean._t_buff_gap = ReadInt(datas[index++]);
            bean._t_buff_trigger_buff = ReadInt(datas[index++]);
            bean._t_buff_trigger_layers = ReadInt(datas[index++]);
            bean._t_buff_add_odd = ReadInt(datas[index++]);
            bean._t_buff_param_id = ReadLongArray(datas[index++]);
            bean.t_buff_param_id = GetReadOnlyArray(bean._t_buff_param_id);
            bean._t_descId = ReadLong(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_buff GetConfigImp(long key)
    {
        t_buff bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_buff where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_buff();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_class_id = SqliteDataManager_ReadLong();
            bean._t_buff_during = SqliteDataManager_ReadInt();
            bean._t_buff_gap = SqliteDataManager_ReadInt();
            bean._t_buff_trigger_buff = SqliteDataManager_ReadInt();
            bean._t_buff_trigger_layers = SqliteDataManager_ReadInt();
            bean._t_buff_add_odd = SqliteDataManager_ReadInt();
            bean._t_buff_param_id = SqliteDataManager_ReadLongArray();
            bean.t_buff_param_id = GetReadOnlyArray(bean._t_buff_param_id);
            bean._t_descId = SqliteDataManager_ReadLong();
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