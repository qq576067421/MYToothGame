/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_actionBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_actionBean:HF.BeanBase
{
private static string m_FileName = "t_actionBean.csv";
    public override string GetId_string()

    {
        return t_id;
    }

    private string _t_id;
    public string t_id{ get { return _t_id; }}
    private string _t_ac_name;
    public string t_ac_name{ get { return _t_ac_name; }}
    private int _t_ac_cast_point;
    public int t_ac_cast_point{ get { return _t_ac_cast_point; }}
    private int _t_ac_finish;
    public int t_ac_finish{ get { return _t_ac_finish; }}
    private int _t_frame_rate;
    public int t_frame_rate{ get { return _t_frame_rate; }}
    private static List<string> m_Keys = new List<string>();
    public static List<string> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_actionBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<string, t_actionBean> m_Dic = new Dictionary<string, t_actionBean>(); 
    public static t_actionBean GetConfig(string key, bool check_null = true)
    { 
        t_actionBean bean = null; var className = "t_actionBean";
        
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
    public void CopyFrom(t_actionBean source)
    {
        _t_id = source._t_id;
        _t_ac_name = source._t_ac_name;
        _t_ac_cast_point = source._t_ac_cast_point;
        _t_ac_finish = source._t_ac_finish;
        _t_frame_rate = source._t_frame_rate;
    }
    private static t_actionBean GetCSVConfigImp(string key)
    {
        t_actionBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_actionBean();
            bean._t_id = ReadString(datas[index++]);
            bean._t_ac_name = ReadString(datas[index++]);
            bean._t_ac_cast_point = ReadInt(datas[index++]);
            bean._t_ac_finish = ReadInt(datas[index++]);
            bean._t_frame_rate = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_actionBean GetConfigImp(string key)
    {
        t_actionBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_actionBean where t_id = ");
        StringBuilder.Append("\""); 
        StringBuilder.Append(key); 
        StringBuilder.Append("\""); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_actionBean();
            bean._t_id = SqliteDataManager_ReadString();
            bean._t_ac_name = SqliteDataManager_ReadString();
            bean._t_ac_cast_point = SqliteDataManager_ReadInt();
            bean._t_ac_finish = SqliteDataManager_ReadInt();
            bean._t_frame_rate = SqliteDataManager_ReadInt();
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