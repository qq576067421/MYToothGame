/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_skillDescBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_skillDescBean:HF.BeanBase
{
private static string m_FileName = "t_skillDescBean.csv";
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
    private int _t_atk_eff;
    public int t_atk_eff{ get { return _t_atk_eff; }}
    private string _t_atk_eff_pos;
    public string t_atk_eff_pos{ get { return _t_atk_eff_pos; }}
    private int _t_atk_eff_parent;
    public int t_atk_eff_parent{ get { return _t_atk_eff_parent; }}
    private int _t_hitEff;
    public int t_hitEff{ get { return _t_hitEff; }}
    private int _t_attackSound;
    public int t_attackSound{ get { return _t_attackSound; }}
    private int _t_hitSound;
    public int t_hitSound{ get { return _t_hitSound; }}
    private int _t_skill_op_type;
    public int t_skill_op_type{ get { return _t_skill_op_type; }}
    private int _t_skill_op_radius;
    public int t_skill_op_radius{ get { return _t_skill_op_radius; }}
    private int _t_skill_op_deg;
    public int t_skill_op_deg{ get { return _t_skill_op_deg; }}
    private int _t_change_attack;
    public int t_change_attack{ get { return _t_change_attack; }}
    private int _t_finish_change_idle;
    public int t_finish_change_idle{ get { return _t_finish_change_idle; }}
    private string _t_action;
    public string t_action{ get { return _t_action; }}
    private int _t_warning;
    public int t_warning{ get { return _t_warning; }}
    private string _t_icon;
    public string t_icon{ get { return _t_icon; }}
    private int _t_gesture;
    public int t_gesture{ get { return _t_gesture; }}
    private int _t_gesture_phase;
    public int t_gesture_phase{ get { return _t_gesture_phase; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_skillDescBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_skillDescBean> m_Dic = new Dictionary<long, t_skillDescBean>(); 
    public static t_skillDescBean GetConfig(long key, bool check_null = true)
    { 
        t_skillDescBean bean = null; var className = "t_skillDescBean";
        
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
    public void CopyFrom(t_skillDescBean source)
    {
        _t_id = source._t_id;
        _t_name = source._t_name;
        _t_desc = source._t_desc;
        _t_atk_eff = source._t_atk_eff;
        _t_atk_eff_pos = source._t_atk_eff_pos;
        _t_atk_eff_parent = source._t_atk_eff_parent;
        _t_hitEff = source._t_hitEff;
        _t_attackSound = source._t_attackSound;
        _t_hitSound = source._t_hitSound;
        _t_skill_op_type = source._t_skill_op_type;
        _t_skill_op_radius = source._t_skill_op_radius;
        _t_skill_op_deg = source._t_skill_op_deg;
        _t_change_attack = source._t_change_attack;
        _t_finish_change_idle = source._t_finish_change_idle;
        _t_action = source._t_action;
        _t_warning = source._t_warning;
        _t_icon = source._t_icon;
        _t_gesture = source._t_gesture;
        _t_gesture_phase = source._t_gesture_phase;
    }
    private static t_skillDescBean GetCSVConfigImp(long key)
    {
        t_skillDescBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_skillDescBean();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_name = ReadString(datas[index++]);
            bean._t_desc = ReadString(datas[index++]);
            bean._t_atk_eff = ReadInt(datas[index++]);
            bean._t_atk_eff_pos = ReadString(datas[index++]);
            bean._t_atk_eff_parent = ReadInt(datas[index++]);
            bean._t_hitEff = ReadInt(datas[index++]);
            bean._t_attackSound = ReadInt(datas[index++]);
            bean._t_hitSound = ReadInt(datas[index++]);
            bean._t_skill_op_type = ReadInt(datas[index++]);
            bean._t_skill_op_radius = ReadInt(datas[index++]);
            bean._t_skill_op_deg = ReadInt(datas[index++]);
            bean._t_change_attack = ReadInt(datas[index++]);
            bean._t_finish_change_idle = ReadInt(datas[index++]);
            bean._t_action = ReadString(datas[index++]);
            bean._t_warning = ReadInt(datas[index++]);
            bean._t_icon = ReadString(datas[index++]);
            bean._t_gesture = ReadInt(datas[index++]);
            bean._t_gesture_phase = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_skillDescBean GetConfigImp(long key)
    {
        t_skillDescBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_skillDescBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_skillDescBean();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_name = SqliteDataManager_ReadString();
            bean._t_desc = SqliteDataManager_ReadString();
            bean._t_atk_eff = SqliteDataManager_ReadInt();
            bean._t_atk_eff_pos = SqliteDataManager_ReadString();
            bean._t_atk_eff_parent = SqliteDataManager_ReadInt();
            bean._t_hitEff = SqliteDataManager_ReadInt();
            bean._t_attackSound = SqliteDataManager_ReadInt();
            bean._t_hitSound = SqliteDataManager_ReadInt();
            bean._t_skill_op_type = SqliteDataManager_ReadInt();
            bean._t_skill_op_radius = SqliteDataManager_ReadInt();
            bean._t_skill_op_deg = SqliteDataManager_ReadInt();
            bean._t_change_attack = SqliteDataManager_ReadInt();
            bean._t_finish_change_idle = SqliteDataManager_ReadInt();
            bean._t_action = SqliteDataManager_ReadString();
            bean._t_warning = SqliteDataManager_ReadInt();
            bean._t_icon = SqliteDataManager_ReadString();
            bean._t_gesture = SqliteDataManager_ReadInt();
            bean._t_gesture_phase = SqliteDataManager_ReadInt();
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