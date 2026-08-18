/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_skillBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_skillBean:MonoBean.BeanBase
{
private static string m_FileName = "t_skillBean.csv";
    public override long GetId_long()

    {
        return t_id;
    }

    private long _t_id;
    public long t_id{ get { return _t_id; }}
    private string _t_desc;
    public string t_desc{ get { return _t_desc; }}
    private long _t_class_Id;
    public long t_class_Id{ get { return _t_class_Id; }}
    private List<int> _t_skill_precon;
    public ReadOnlyCollection<int> t_skill_precon;
    private int _t_skill_cast_style;
    public int t_skill_cast_style{ get { return _t_skill_cast_style; }}
    private int _t_cooldown;
    public int t_cooldown{ get { return _t_cooldown; }}
    private int _t_Interval;
    public int t_Interval{ get { return _t_Interval; }}
    private int _t_keep_time;
    public int t_keep_time{ get { return _t_keep_time; }}
    private int _t_hurt_interval;
    public int t_hurt_interval{ get { return _t_hurt_interval; }}
    private int _t_hurt_param_type;
    public int t_hurt_param_type{ get { return _t_hurt_param_type; }}
    private int _t_hurt_param0;
    public int t_hurt_param0{ get { return _t_hurt_param0; }}
    private int _t_hurt_param1;
    public int t_hurt_param1{ get { return _t_hurt_param1; }}
    private int _t_bullet_id;
    public int t_bullet_id{ get { return _t_bullet_id; }}
    private List<long> _t_skill_selfbuff_id;
    public ReadOnlyCollection<long> t_skill_selfbuff_id;
    private long _t_split_bullet_id;
    public long t_split_bullet_id{ get { return _t_split_bullet_id; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_skillBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_skillBean> m_Dic = new Dictionary<long, t_skillBean>(); 
    public static t_skillBean GetConfig(long key, bool check_null = true)
    { 
        t_skillBean bean = null; var className = "t_skillBean";
        
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
    public void CopyFrom(t_skillBean source)
    {
        _t_id = source._t_id;
        _t_desc = source._t_desc;
        _t_class_Id = source._t_class_Id;
        _t_skill_precon = source._t_skill_precon;
        t_skill_precon = source.t_skill_precon;
        _t_skill_cast_style = source._t_skill_cast_style;
        _t_cooldown = source._t_cooldown;
        _t_Interval = source._t_Interval;
        _t_keep_time = source._t_keep_time;
        _t_hurt_interval = source._t_hurt_interval;
        _t_hurt_param_type = source._t_hurt_param_type;
        _t_hurt_param0 = source._t_hurt_param0;
        _t_hurt_param1 = source._t_hurt_param1;
        _t_bullet_id = source._t_bullet_id;
        _t_skill_selfbuff_id = source._t_skill_selfbuff_id;
        t_skill_selfbuff_id = source.t_skill_selfbuff_id;
        _t_split_bullet_id = source._t_split_bullet_id;
    }
    private static t_skillBean GetCSVConfigImp(long key)
    {
        t_skillBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_skillBean();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_desc = ReadString(datas[index++]);
            bean._t_class_Id = ReadLong(datas[index++]);
            bean._t_skill_precon = ReadIntArray(datas[index++]);
            bean.t_skill_precon = GetReadOnlyArray(bean._t_skill_precon);
            bean._t_skill_cast_style = ReadInt(datas[index++]);
            bean._t_cooldown = ReadInt(datas[index++]);
            bean._t_Interval = ReadInt(datas[index++]);
            bean._t_keep_time = ReadInt(datas[index++]);
            bean._t_hurt_interval = ReadInt(datas[index++]);
            bean._t_hurt_param_type = ReadInt(datas[index++]);
            bean._t_hurt_param0 = ReadInt(datas[index++]);
            bean._t_hurt_param1 = ReadInt(datas[index++]);
            bean._t_bullet_id = ReadInt(datas[index++]);
            bean._t_skill_selfbuff_id = ReadLongArray(datas[index++]);
            bean.t_skill_selfbuff_id = GetReadOnlyArray(bean._t_skill_selfbuff_id);
            bean._t_split_bullet_id = ReadLong(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_skillBean GetConfigImp(long key)
    {
        t_skillBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_skillBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_skillBean();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_desc = SqliteDataManager_ReadString();
            bean._t_class_Id = SqliteDataManager_ReadLong();
            bean._t_skill_precon = SqliteDataManager_ReadIntArray();
            bean.t_skill_precon = GetReadOnlyArray(bean._t_skill_precon);
            bean._t_skill_cast_style = SqliteDataManager_ReadInt();
            bean._t_cooldown = SqliteDataManager_ReadInt();
            bean._t_Interval = SqliteDataManager_ReadInt();
            bean._t_keep_time = SqliteDataManager_ReadInt();
            bean._t_hurt_interval = SqliteDataManager_ReadInt();
            bean._t_hurt_param_type = SqliteDataManager_ReadInt();
            bean._t_hurt_param0 = SqliteDataManager_ReadInt();
            bean._t_hurt_param1 = SqliteDataManager_ReadInt();
            bean._t_bullet_id = SqliteDataManager_ReadInt();
            bean._t_skill_selfbuff_id = SqliteDataManager_ReadLongArray();
            bean.t_skill_selfbuff_id = GetReadOnlyArray(bean._t_skill_selfbuff_id);
            bean._t_split_bullet_id = SqliteDataManager_ReadLong();
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