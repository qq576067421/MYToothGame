/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_monsterBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_monsterBean:MonoBean.BeanBase
{
private static string m_FileName = "t_monsterBean.csv";
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
    private int _t_type;
    public int t_type{ get { return _t_type; }}
    private int _t_ui_show_rot_y;
    public int t_ui_show_rot_y{ get { return _t_ui_show_rot_y; }}
    private string _t_model;
    public string t_model{ get { return _t_model; }}
    private string _t_head;
    public string t_head{ get { return _t_head; }}
    private int _t_hit_point;
    public int t_hit_point{ get { return _t_hit_point; }}
    private List<List<int>> _t_fire_positions;
    public ReadOnlyCollection<ReadOnlyCollection<int>> t_fire_positions;
    private string _t_fire_points;
    public string t_fire_points{ get { return _t_fire_points; }}
    private int _t_size;
    public int t_size{ get { return _t_size; }}
    private int _t_MoveSpeed;
    public int t_MoveSpeed{ get { return _t_MoveSpeed; }}
    private int _t_base_damage;
    public int t_base_damage{ get { return _t_base_damage; }}
    private List<long> _t_skill_id;
    public ReadOnlyCollection<long> t_skill_id;
    private int _t_td_boss_skill_prepare_ms;
    public int t_td_boss_skill_prepare_ms{ get { return _t_td_boss_skill_prepare_ms; }}
    private int _t_die_time;
    public int t_die_time{ get { return _t_die_time; }}
    private int _t_die_effect_start_time;
    public int t_die_effect_start_time{ get { return _t_die_effect_start_time; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_monsterBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_monsterBean> m_Dic = new Dictionary<long, t_monsterBean>(); 
    public static t_monsterBean GetConfig(long key, bool check_null = true)
    { 
        t_monsterBean bean = null; var className = "t_monsterBean";
        
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
    public void CopyFrom(t_monsterBean source)
    {
        _t_id = source._t_id;
        _t_name = source._t_name;
        _t_desc = source._t_desc;
        _t_type = source._t_type;
        _t_ui_show_rot_y = source._t_ui_show_rot_y;
        _t_model = source._t_model;
        _t_head = source._t_head;
        _t_hit_point = source._t_hit_point;
        _t_fire_positions = source._t_fire_positions;
        t_fire_positions = source.t_fire_positions;
        _t_fire_points = source._t_fire_points;
        _t_size = source._t_size;
        _t_MoveSpeed = source._t_MoveSpeed;
        _t_base_damage = source._t_base_damage;
        _t_skill_id = source._t_skill_id;
        t_skill_id = source.t_skill_id;
        _t_td_boss_skill_prepare_ms = source._t_td_boss_skill_prepare_ms;
        _t_die_time = source._t_die_time;
        _t_die_effect_start_time = source._t_die_effect_start_time;
    }
    private static t_monsterBean GetCSVConfigImp(long key)
    {
        t_monsterBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_monsterBean();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_name = ReadString(datas[index++]);
            bean._t_desc = ReadString(datas[index++]);
            bean._t_type = ReadInt(datas[index++]);
            bean._t_ui_show_rot_y = ReadInt(datas[index++]);
            bean._t_model = ReadString(datas[index++]);
            bean._t_head = ReadString(datas[index++]);
            bean._t_hit_point = ReadInt(datas[index++]);
            bean._t_fire_positions = ReadIntArray2(datas[index++]);
            bean.t_fire_positions = GetReadOnlyArray(bean._t_fire_positions);
            bean._t_fire_points = ReadString(datas[index++]);
            bean._t_size = ReadInt(datas[index++]);
            bean._t_MoveSpeed = ReadInt(datas[index++]);
            bean._t_base_damage = ReadInt(datas[index++]);
            bean._t_skill_id = ReadLongArray(datas[index++]);
            bean.t_skill_id = GetReadOnlyArray(bean._t_skill_id);
            bean._t_td_boss_skill_prepare_ms = ReadInt(datas[index++]);
            bean._t_die_time = ReadInt(datas[index++]);
            bean._t_die_effect_start_time = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_monsterBean GetConfigImp(long key)
    {
        t_monsterBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_monsterBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_monsterBean();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_name = SqliteDataManager_ReadString();
            bean._t_desc = SqliteDataManager_ReadString();
            bean._t_type = SqliteDataManager_ReadInt();
            bean._t_ui_show_rot_y = SqliteDataManager_ReadInt();
            bean._t_model = SqliteDataManager_ReadString();
            bean._t_head = SqliteDataManager_ReadString();
            bean._t_hit_point = SqliteDataManager_ReadInt();
            bean._t_fire_positions = SqliteDataManager_ReadIntArray2();
            bean.t_fire_positions = GetReadOnlyArray(bean._t_fire_positions);
            bean._t_fire_points = SqliteDataManager_ReadString();
            bean._t_size = SqliteDataManager_ReadInt();
            bean._t_MoveSpeed = SqliteDataManager_ReadInt();
            bean._t_base_damage = SqliteDataManager_ReadInt();
            bean._t_skill_id = SqliteDataManager_ReadLongArray();
            bean.t_skill_id = GetReadOnlyArray(bean._t_skill_id);
            bean._t_td_boss_skill_prepare_ms = SqliteDataManager_ReadInt();
            bean._t_die_time = SqliteDataManager_ReadInt();
            bean._t_die_effect_start_time = SqliteDataManager_ReadInt();
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