/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_buffParam
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_buffParam:MonoBean.BeanBase
{
private static string m_FileName = "t_buffParam.csv";
    public override long GetId_long()

    {
        return t_id;
    }

    private long _t_id;
    public long t_id{ get { return _t_id; }}
    private int _t_buff_maxnum;
    public int t_buff_maxnum{ get { return _t_buff_maxnum; }}
    private int _t_atk;
    public int t_atk{ get { return _t_atk; }}
    private int _t_atk_percen;
    public int t_atk_percen{ get { return _t_atk_percen; }}
    private int _t_hp;
    public int t_hp{ get { return _t_hp; }}
    private int _t_hp_percen;
    public int t_hp_percen{ get { return _t_hp_percen; }}
    private int _t_crit;
    public int t_crit{ get { return _t_crit; }}
    private int _t_crit_damage;
    public int t_crit_damage{ get { return _t_crit_damage; }}
    private int _t_move_speed;
    public int t_move_speed{ get { return _t_move_speed; }}
    private int _t_move_speed_percen;
    public int t_move_speed_percen{ get { return _t_move_speed_percen; }}
    private int _t_atk_speed;
    public int t_atk_speed{ get { return _t_atk_speed; }}
    private int _t_atk_speed_percen;
    public int t_atk_speed_percen{ get { return _t_atk_speed_percen; }}
    private int _t_amp;
    public int t_amp{ get { return _t_amp; }}
    private int _t_duration;
    public int t_duration{ get { return _t_duration; }}
    private int _t_attack_range;
    public int t_attack_range{ get { return _t_attack_range; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_buffParam", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_buffParam> m_Dic = new Dictionary<long, t_buffParam>(); 
    public static t_buffParam GetConfig(long key, bool check_null = true)
    { 
        t_buffParam bean = null; var className = "t_buffParam";
        
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
    public void CopyFrom(t_buffParam source)
    {
        _t_id = source._t_id;
        _t_buff_maxnum = source._t_buff_maxnum;
        _t_atk = source._t_atk;
        _t_atk_percen = source._t_atk_percen;
        _t_hp = source._t_hp;
        _t_hp_percen = source._t_hp_percen;
        _t_crit = source._t_crit;
        _t_crit_damage = source._t_crit_damage;
        _t_move_speed = source._t_move_speed;
        _t_move_speed_percen = source._t_move_speed_percen;
        _t_atk_speed = source._t_atk_speed;
        _t_atk_speed_percen = source._t_atk_speed_percen;
        _t_amp = source._t_amp;
        _t_duration = source._t_duration;
        _t_attack_range = source._t_attack_range;
    }
    private static t_buffParam GetCSVConfigImp(long key)
    {
        t_buffParam bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_buffParam();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_buff_maxnum = ReadInt(datas[index++]);
            bean._t_atk = ReadInt(datas[index++]);
            bean._t_atk_percen = ReadInt(datas[index++]);
            bean._t_hp = ReadInt(datas[index++]);
            bean._t_hp_percen = ReadInt(datas[index++]);
            bean._t_crit = ReadInt(datas[index++]);
            bean._t_crit_damage = ReadInt(datas[index++]);
            bean._t_move_speed = ReadInt(datas[index++]);
            bean._t_move_speed_percen = ReadInt(datas[index++]);
            bean._t_atk_speed = ReadInt(datas[index++]);
            bean._t_atk_speed_percen = ReadInt(datas[index++]);
            bean._t_amp = ReadInt(datas[index++]);
            bean._t_duration = ReadInt(datas[index++]);
            bean._t_attack_range = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_buffParam GetConfigImp(long key)
    {
        t_buffParam bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_buffParam where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_buffParam();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_buff_maxnum = SqliteDataManager_ReadInt();
            bean._t_atk = SqliteDataManager_ReadInt();
            bean._t_atk_percen = SqliteDataManager_ReadInt();
            bean._t_hp = SqliteDataManager_ReadInt();
            bean._t_hp_percen = SqliteDataManager_ReadInt();
            bean._t_crit = SqliteDataManager_ReadInt();
            bean._t_crit_damage = SqliteDataManager_ReadInt();
            bean._t_move_speed = SqliteDataManager_ReadInt();
            bean._t_move_speed_percen = SqliteDataManager_ReadInt();
            bean._t_atk_speed = SqliteDataManager_ReadInt();
            bean._t_atk_speed_percen = SqliteDataManager_ReadInt();
            bean._t_amp = SqliteDataManager_ReadInt();
            bean._t_duration = SqliteDataManager_ReadInt();
            bean._t_attack_range = SqliteDataManager_ReadInt();
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