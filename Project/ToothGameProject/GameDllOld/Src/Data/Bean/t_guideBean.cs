/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_guideBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_guideBean:HF.BeanBase
{
private static string m_FileName = "t_guideBean.csv";
    public override int GetId_int()

    {
        return t_id;
    }

    private int _t_id;
    public int t_id{ get { return _t_id; }}
    private string _t_guide_group;
    public string t_guide_group{ get { return _t_guide_group; }}
    private string _t_guide_step;
    public string t_guide_step{ get { return _t_guide_step; }}
    private int _t_shape;
    public int t_shape{ get { return _t_shape; }}
    private int _t_arrow_x;
    public int t_arrow_x{ get { return _t_arrow_x; }}
    private int _t_arrow_y;
    public int t_arrow_y{ get { return _t_arrow_y; }}
    private int _t_arrow_type;
    public int t_arrow_type{ get { return _t_arrow_type; }}
    private int _t_desc_x;
    public int t_desc_x{ get { return _t_desc_x; }}
    private int _t_desc_y;
    public int t_desc_y{ get { return _t_desc_y; }}
    private int _t_desc_w;
    public int t_desc_w{ get { return _t_desc_w; }}
    private int _t_desc_h;
    public int t_desc_h{ get { return _t_desc_h; }}
    private int _t_desc_rel;
    public int t_desc_rel{ get { return _t_desc_rel; }}
    private string _t_desc;
    public string t_desc{ get { return _t_desc; }}
    private int _t_finish_step_style;
    public int t_finish_step_style{ get { return _t_finish_step_style; }}
    private int _t_trigger_next_step;
    public int t_trigger_next_step{ get { return _t_trigger_next_step; }}
    private int _t_trigger_next_group;
    public int t_trigger_next_group{ get { return _t_trigger_next_group; }}
    private int _t_step_state;
    public int t_step_state{ get { return _t_step_state; }}
    private int _t_jump;
    public int t_jump{ get { return _t_jump; }}
    private int _t_next_group_id;
    public int t_next_group_id{ get { return _t_next_group_id; }}
    private int _t_close_pop;
    public int t_close_pop{ get { return _t_close_pop; }}
    private int _t_black;
    public int t_black{ get { return _t_black; }}
    private int _t_need;
    public int t_need{ get { return _t_need; }}
    private int _t_3d_mask_w;
    public int t_3d_mask_w{ get { return _t_3d_mask_w; }}
    private int _t_3d_mask_h;
    public int t_3d_mask_h{ get { return _t_3d_mask_h; }}
    private long _t_story;
    public long t_story{ get { return _t_story; }}
    private static List<int> m_Keys = new List<int>();
    public static List<int> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_guideBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<int, t_guideBean> m_Dic = new Dictionary<int, t_guideBean>(); 
    public static t_guideBean GetConfig(int key, bool check_null = true)
    { 
        t_guideBean bean = null; var className = "t_guideBean";
        
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
    public void CopyFrom(t_guideBean source)
    {
        _t_id = source._t_id;
        _t_guide_group = source._t_guide_group;
        _t_guide_step = source._t_guide_step;
        _t_shape = source._t_shape;
        _t_arrow_x = source._t_arrow_x;
        _t_arrow_y = source._t_arrow_y;
        _t_arrow_type = source._t_arrow_type;
        _t_desc_x = source._t_desc_x;
        _t_desc_y = source._t_desc_y;
        _t_desc_w = source._t_desc_w;
        _t_desc_h = source._t_desc_h;
        _t_desc_rel = source._t_desc_rel;
        _t_desc = source._t_desc;
        _t_finish_step_style = source._t_finish_step_style;
        _t_trigger_next_step = source._t_trigger_next_step;
        _t_trigger_next_group = source._t_trigger_next_group;
        _t_step_state = source._t_step_state;
        _t_jump = source._t_jump;
        _t_next_group_id = source._t_next_group_id;
        _t_close_pop = source._t_close_pop;
        _t_black = source._t_black;
        _t_need = source._t_need;
        _t_3d_mask_w = source._t_3d_mask_w;
        _t_3d_mask_h = source._t_3d_mask_h;
        _t_story = source._t_story;
    }
    private static t_guideBean GetCSVConfigImp(int key)
    {
        t_guideBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_guideBean();
            bean._t_id = ReadInt(datas[index++]);
            bean._t_guide_group = ReadString(datas[index++]);
            bean._t_guide_step = ReadString(datas[index++]);
            bean._t_shape = ReadInt(datas[index++]);
            bean._t_arrow_x = ReadInt(datas[index++]);
            bean._t_arrow_y = ReadInt(datas[index++]);
            bean._t_arrow_type = ReadInt(datas[index++]);
            bean._t_desc_x = ReadInt(datas[index++]);
            bean._t_desc_y = ReadInt(datas[index++]);
            bean._t_desc_w = ReadInt(datas[index++]);
            bean._t_desc_h = ReadInt(datas[index++]);
            bean._t_desc_rel = ReadInt(datas[index++]);
            bean._t_desc = ReadString(datas[index++]);
            bean._t_finish_step_style = ReadInt(datas[index++]);
            bean._t_trigger_next_step = ReadInt(datas[index++]);
            bean._t_trigger_next_group = ReadInt(datas[index++]);
            bean._t_step_state = ReadInt(datas[index++]);
            bean._t_jump = ReadInt(datas[index++]);
            bean._t_next_group_id = ReadInt(datas[index++]);
            bean._t_close_pop = ReadInt(datas[index++]);
            bean._t_black = ReadInt(datas[index++]);
            bean._t_need = ReadInt(datas[index++]);
            bean._t_3d_mask_w = ReadInt(datas[index++]);
            bean._t_3d_mask_h = ReadInt(datas[index++]);
            bean._t_story = ReadLong(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_guideBean GetConfigImp(int key)
    {
        t_guideBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_guideBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_guideBean();
            bean._t_id = SqliteDataManager_ReadInt();
            bean._t_guide_group = SqliteDataManager_ReadString();
            bean._t_guide_step = SqliteDataManager_ReadString();
            bean._t_shape = SqliteDataManager_ReadInt();
            bean._t_arrow_x = SqliteDataManager_ReadInt();
            bean._t_arrow_y = SqliteDataManager_ReadInt();
            bean._t_arrow_type = SqliteDataManager_ReadInt();
            bean._t_desc_x = SqliteDataManager_ReadInt();
            bean._t_desc_y = SqliteDataManager_ReadInt();
            bean._t_desc_w = SqliteDataManager_ReadInt();
            bean._t_desc_h = SqliteDataManager_ReadInt();
            bean._t_desc_rel = SqliteDataManager_ReadInt();
            bean._t_desc = SqliteDataManager_ReadString();
            bean._t_finish_step_style = SqliteDataManager_ReadInt();
            bean._t_trigger_next_step = SqliteDataManager_ReadInt();
            bean._t_trigger_next_group = SqliteDataManager_ReadInt();
            bean._t_step_state = SqliteDataManager_ReadInt();
            bean._t_jump = SqliteDataManager_ReadInt();
            bean._t_next_group_id = SqliteDataManager_ReadInt();
            bean._t_close_pop = SqliteDataManager_ReadInt();
            bean._t_black = SqliteDataManager_ReadInt();
            bean._t_need = SqliteDataManager_ReadInt();
            bean._t_3d_mask_w = SqliteDataManager_ReadInt();
            bean._t_3d_mask_h = SqliteDataManager_ReadInt();
            bean._t_story = SqliteDataManager_ReadLong();
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