/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_buffDesc
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_buffDesc:MonoBean.BeanBase
{
private static string m_FileName = "t_buffDesc.csv";
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
    private string _t_battle_icon;
    public string t_battle_icon{ get { return _t_battle_icon; }}
    private int _t_effect_type;
    public int t_effect_type{ get { return _t_effect_type; }}
    private string _t_buff_addEffect;
    public string t_buff_addEffect{ get { return _t_buff_addEffect; }}
    private string _t_specialbuff_pos;
    public string t_specialbuff_pos{ get { return _t_specialbuff_pos; }}
    private string _t_specialbuff_sound;
    public string t_specialbuff_sound{ get { return _t_specialbuff_sound; }}
    private int _t_buff_replace_type;
    public int t_buff_replace_type{ get { return _t_buff_replace_type; }}
    private int _t_buff_priority;
    public int t_buff_priority{ get { return _t_buff_priority; }}
    private int _t_special_action;
    public int t_special_action{ get { return _t_special_action; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_buffDesc", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_buffDesc> m_Dic = new Dictionary<long, t_buffDesc>(); 
    public static t_buffDesc GetConfig(long key, bool check_null = true)
    { 
        t_buffDesc bean = null; var className = "t_buffDesc";
        
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
    public void CopyFrom(t_buffDesc source)
    {
        _t_id = source._t_id;
        _t_name = source._t_name;
        _t_desc = source._t_desc;
        _t_battle_icon = source._t_battle_icon;
        _t_effect_type = source._t_effect_type;
        _t_buff_addEffect = source._t_buff_addEffect;
        _t_specialbuff_pos = source._t_specialbuff_pos;
        _t_specialbuff_sound = source._t_specialbuff_sound;
        _t_buff_replace_type = source._t_buff_replace_type;
        _t_buff_priority = source._t_buff_priority;
        _t_special_action = source._t_special_action;
    }
    private static t_buffDesc GetCSVConfigImp(long key)
    {
        t_buffDesc bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_buffDesc();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_name = ReadString(datas[index++]);
            bean._t_desc = ReadString(datas[index++]);
            bean._t_battle_icon = ReadString(datas[index++]);
            bean._t_effect_type = ReadInt(datas[index++]);
            bean._t_buff_addEffect = ReadString(datas[index++]);
            bean._t_specialbuff_pos = ReadString(datas[index++]);
            bean._t_specialbuff_sound = ReadString(datas[index++]);
            bean._t_buff_replace_type = ReadInt(datas[index++]);
            bean._t_buff_priority = ReadInt(datas[index++]);
            bean._t_special_action = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_buffDesc GetConfigImp(long key)
    {
        t_buffDesc bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_buffDesc where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_buffDesc();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_name = SqliteDataManager_ReadString();
            bean._t_desc = SqliteDataManager_ReadString();
            bean._t_battle_icon = SqliteDataManager_ReadString();
            bean._t_effect_type = SqliteDataManager_ReadInt();
            bean._t_buff_addEffect = SqliteDataManager_ReadString();
            bean._t_specialbuff_pos = SqliteDataManager_ReadString();
            bean._t_specialbuff_sound = SqliteDataManager_ReadString();
            bean._t_buff_replace_type = SqliteDataManager_ReadInt();
            bean._t_buff_priority = SqliteDataManager_ReadInt();
            bean._t_special_action = SqliteDataManager_ReadInt();
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