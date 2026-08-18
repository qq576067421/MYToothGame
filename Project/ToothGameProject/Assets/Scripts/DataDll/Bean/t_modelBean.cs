/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_modelBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_modelBean:MonoBean.BeanBase
{
private static string m_FileName = "t_modelBean.csv";
    public override long GetId_long()

    {
        return t_id;
    }

    private long _t_id;
    public long t_id{ get { return _t_id; }}
    private string _t_model_res;
    public string t_model_res{ get { return _t_model_res; }}
    private string _t_ui_show_pos;
    public string t_ui_show_pos{ get { return _t_ui_show_pos; }}
    private string _t_ui_show_rot;
    public string t_ui_show_rot{ get { return _t_ui_show_rot; }}
    private int _t_ui_show_scale;
    public int t_ui_show_scale{ get { return _t_ui_show_scale; }}
    private string _t_head_res;
    public string t_head_res{ get { return _t_head_res; }}
    private List<List<int>> _t_fire_positions;
    public ReadOnlyCollection<ReadOnlyCollection<int>> t_fire_positions;
    private string _t_fire_points;
    public string t_fire_points{ get { return _t_fire_points; }}
    private int _t_hit_point;
    public int t_hit_point{ get { return _t_hit_point; }}
    private int _t_size;
    public int t_size{ get { return _t_size; }}
    private int _t_move_speed;
    public int t_move_speed{ get { return _t_move_speed; }}
    private static List<long> m_Keys = new List<long>();
    public static List<long> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_modelBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<long, t_modelBean> m_Dic = new Dictionary<long, t_modelBean>(); 
    public static t_modelBean GetConfig(long key, bool check_null = true)
    { 
        t_modelBean bean = null; var className = "t_modelBean";
        
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
    public void CopyFrom(t_modelBean source)
    {
        _t_id = source._t_id;
        _t_model_res = source._t_model_res;
        _t_ui_show_pos = source._t_ui_show_pos;
        _t_ui_show_rot = source._t_ui_show_rot;
        _t_ui_show_scale = source._t_ui_show_scale;
        _t_head_res = source._t_head_res;
        _t_fire_positions = source._t_fire_positions;
        t_fire_positions = source.t_fire_positions;
        _t_fire_points = source._t_fire_points;
        _t_hit_point = source._t_hit_point;
        _t_size = source._t_size;
        _t_move_speed = source._t_move_speed;
    }
    private static t_modelBean GetCSVConfigImp(long key)
    {
        t_modelBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_modelBean();
            bean._t_id = ReadLong(datas[index++]);
            bean._t_model_res = ReadString(datas[index++]);
            bean._t_ui_show_pos = ReadString(datas[index++]);
            bean._t_ui_show_rot = ReadString(datas[index++]);
            bean._t_ui_show_scale = ReadInt(datas[index++]);
            bean._t_head_res = ReadString(datas[index++]);
            bean._t_fire_positions = ReadIntArray2(datas[index++]);
            bean.t_fire_positions = GetReadOnlyArray(bean._t_fire_positions);
            bean._t_fire_points = ReadString(datas[index++]);
            bean._t_hit_point = ReadInt(datas[index++]);
            bean._t_size = ReadInt(datas[index++]);
            bean._t_move_speed = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_modelBean GetConfigImp(long key)
    {
        t_modelBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_modelBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_modelBean();
            bean._t_id = SqliteDataManager_ReadLong();
            bean._t_model_res = SqliteDataManager_ReadString();
            bean._t_ui_show_pos = SqliteDataManager_ReadString();
            bean._t_ui_show_rot = SqliteDataManager_ReadString();
            bean._t_ui_show_scale = SqliteDataManager_ReadInt();
            bean._t_head_res = SqliteDataManager_ReadString();
            bean._t_fire_positions = SqliteDataManager_ReadIntArray2();
            bean.t_fire_positions = GetReadOnlyArray(bean._t_fire_positions);
            bean._t_fire_points = SqliteDataManager_ReadString();
            bean._t_hit_point = SqliteDataManager_ReadInt();
            bean._t_size = SqliteDataManager_ReadInt();
            bean._t_move_speed = SqliteDataManager_ReadInt();
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