/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_effectBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_effectBean:HF.BeanBase
{
private static string m_FileName = "t_effectBean.csv";
    public override int GetId_int()

    {
        return t_id;
    }

    private int _t_id;
    public int t_id{ get { return _t_id; }}
    private string _t_abname;
    public string t_abname{ get { return _t_abname; }}
    private int _t_scale;
    public int t_scale{ get { return _t_scale; }}
    private int _t_time;
    public int t_time{ get { return _t_time; }}
    private int _t_sound;
    public int t_sound{ get { return _t_sound; }}
    private static List<int> m_Keys = new List<int>();
    public static List<int> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_effectBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<int, t_effectBean> m_Dic = new Dictionary<int, t_effectBean>(); 
    public static t_effectBean GetConfig(int key, bool check_null = true)
    { 
        t_effectBean bean = null; var className = "t_effectBean";
        
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
    public void CopyFrom(t_effectBean source)
    {
        _t_id = source._t_id;
        _t_abname = source._t_abname;
        _t_scale = source._t_scale;
        _t_time = source._t_time;
        _t_sound = source._t_sound;
    }
    private static t_effectBean GetCSVConfigImp(int key)
    {
        t_effectBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_effectBean();
            bean._t_id = ReadInt(datas[index++]);
            bean._t_abname = ReadString(datas[index++]);
            bean._t_scale = ReadInt(datas[index++]);
            bean._t_time = ReadInt(datas[index++]);
            bean._t_sound = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_effectBean GetConfigImp(int key)
    {
        t_effectBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_effectBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_effectBean();
            bean._t_id = SqliteDataManager_ReadInt();
            bean._t_abname = SqliteDataManager_ReadString();
            bean._t_scale = SqliteDataManager_ReadInt();
            bean._t_time = SqliteDataManager_ReadInt();
            bean._t_sound = SqliteDataManager_ReadInt();
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