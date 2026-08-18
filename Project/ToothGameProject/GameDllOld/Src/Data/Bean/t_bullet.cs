/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_bullet
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_bullet:HF.BeanBase
{
private static string m_FileName = "t_bullet.csv";
    public override int GetId_int()

    {
        return t_id;
    }

    private int _t_id;
    public int t_id{ get { return _t_id; }}
    private int _t_class_id;
    public int t_class_id{ get { return _t_class_id; }}
    private int _t_skill_damage;
    public int t_skill_damage{ get { return _t_skill_damage; }}
    private int _t_bbt_damage;
    public int t_bbt_damage{ get { return _t_bbt_damage; }}
    private int _t_move_speed;
    public int t_move_speed{ get { return _t_move_speed; }}
    private int _t_penetrate;
    public int t_penetrate{ get { return _t_penetrate; }}
    private int _t_size;
    public int t_size{ get { return _t_size; }}
    private int _t_max_time;
    public int t_max_time{ get { return _t_max_time; }}
    private int _t_trajectory;
    public int t_trajectory{ get { return _t_trajectory; }}
    private int _t_Gravity;
    public int t_Gravity{ get { return _t_Gravity; }}
    private int _t_tracking_range;
    public int t_tracking_range{ get { return _t_tracking_range; }}
    private string _t_effect_abname;
    public string t_effect_abname{ get { return _t_effect_abname; }}
    private List<long> _t_bullet_hittarget_buff_id;
    public ReadOnlyCollection<long> t_bullet_hittarget_buff_id;
    private int _t_hit_sound;
    public int t_hit_sound{ get { return _t_hit_sound; }}
    private int _t_trigger_bullet_id;
    public int t_trigger_bullet_id{ get { return _t_trigger_bullet_id; }}
    private int _t_trigger_bullet_count;
    public int t_trigger_bullet_count{ get { return _t_trigger_bullet_count; }}
    private int _t_trigger_type;
    public int t_trigger_type{ get { return _t_trigger_type; }}
    private int _t_trigger_Y;
    public int t_trigger_Y{ get { return _t_trigger_Y; }}
    private static List<int> m_Keys = new List<int>();
    public static List<int> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_bullet", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<int, t_bullet> m_Dic = new Dictionary<int, t_bullet>(); 
    public static t_bullet GetConfig(int key, bool check_null = true)
    { 
        t_bullet bean = null; var className = "t_bullet";
        
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
    public void CopyFrom(t_bullet source)
    {
        _t_id = source._t_id;
        _t_class_id = source._t_class_id;
        _t_skill_damage = source._t_skill_damage;
        _t_bbt_damage = source._t_bbt_damage;
        _t_move_speed = source._t_move_speed;
        _t_penetrate = source._t_penetrate;
        _t_size = source._t_size;
        _t_max_time = source._t_max_time;
        _t_trajectory = source._t_trajectory;
        _t_Gravity = source._t_Gravity;
        _t_tracking_range = source._t_tracking_range;
        _t_effect_abname = source._t_effect_abname;
        _t_bullet_hittarget_buff_id = source._t_bullet_hittarget_buff_id;
        t_bullet_hittarget_buff_id = source.t_bullet_hittarget_buff_id;
        _t_hit_sound = source._t_hit_sound;
        _t_trigger_bullet_id = source._t_trigger_bullet_id;
        _t_trigger_bullet_count = source._t_trigger_bullet_count;
        _t_trigger_type = source._t_trigger_type;
        _t_trigger_Y = source._t_trigger_Y;
    }
    private static t_bullet GetCSVConfigImp(int key)
    {
        t_bullet bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_bullet();
            bean._t_id = ReadInt(datas[index++]);
            bean._t_class_id = ReadInt(datas[index++]);
            bean._t_skill_damage = ReadInt(datas[index++]);
            bean._t_bbt_damage = ReadInt(datas[index++]);
            bean._t_move_speed = ReadInt(datas[index++]);
            bean._t_penetrate = ReadInt(datas[index++]);
            bean._t_size = ReadInt(datas[index++]);
            bean._t_max_time = ReadInt(datas[index++]);
            bean._t_trajectory = ReadInt(datas[index++]);
            bean._t_Gravity = ReadInt(datas[index++]);
            bean._t_tracking_range = ReadInt(datas[index++]);
            bean._t_effect_abname = ReadString(datas[index++]);
            bean._t_bullet_hittarget_buff_id = ReadLongArray(datas[index++]);
            bean.t_bullet_hittarget_buff_id = GetReadOnlyArray(bean._t_bullet_hittarget_buff_id);
            bean._t_hit_sound = ReadInt(datas[index++]);
            bean._t_trigger_bullet_id = ReadInt(datas[index++]);
            bean._t_trigger_bullet_count = ReadInt(datas[index++]);
            bean._t_trigger_type = ReadInt(datas[index++]);
            bean._t_trigger_Y = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_bullet GetConfigImp(int key)
    {
        t_bullet bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_bullet where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_bullet();
            bean._t_id = SqliteDataManager_ReadInt();
            bean._t_class_id = SqliteDataManager_ReadInt();
            bean._t_skill_damage = SqliteDataManager_ReadInt();
            bean._t_bbt_damage = SqliteDataManager_ReadInt();
            bean._t_move_speed = SqliteDataManager_ReadInt();
            bean._t_penetrate = SqliteDataManager_ReadInt();
            bean._t_size = SqliteDataManager_ReadInt();
            bean._t_max_time = SqliteDataManager_ReadInt();
            bean._t_trajectory = SqliteDataManager_ReadInt();
            bean._t_Gravity = SqliteDataManager_ReadInt();
            bean._t_tracking_range = SqliteDataManager_ReadInt();
            bean._t_effect_abname = SqliteDataManager_ReadString();
            bean._t_bullet_hittarget_buff_id = SqliteDataManager_ReadLongArray();
            bean.t_bullet_hittarget_buff_id = GetReadOnlyArray(bean._t_bullet_hittarget_buff_id);
            bean._t_hit_sound = SqliteDataManager_ReadInt();
            bean._t_trigger_bullet_id = SqliteDataManager_ReadInt();
            bean._t_trigger_bullet_count = SqliteDataManager_ReadInt();
            bean._t_trigger_type = SqliteDataManager_ReadInt();
            bean._t_trigger_Y = SqliteDataManager_ReadInt();
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