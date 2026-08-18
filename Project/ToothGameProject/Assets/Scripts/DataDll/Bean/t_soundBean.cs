/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_soundBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_soundBean:MonoBean.BeanBase
{
private static string m_FileName = "t_soundBean.csv";
    public override int GetId_int()

    {
        return t_id;
    }

    private int _t_id;
    public int t_id{ get { return _t_id; }}
    private int _t_allow_multi;
    public int t_allow_multi{ get { return _t_allow_multi; }}
    private string _t_res_abname;
    public string t_res_abname{ get { return _t_res_abname; }}
    private static List<int> m_Keys = new List<int>();
    public static List<int> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_soundBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<int, t_soundBean> m_Dic = new Dictionary<int, t_soundBean>(); 
    public static t_soundBean GetConfig(int key, bool check_null = true)
    { 
        t_soundBean bean = null; var className = "t_soundBean";
        
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
    public void CopyFrom(t_soundBean source)
    {
        _t_id = source._t_id;
        _t_allow_multi = source._t_allow_multi;
        _t_res_abname = source._t_res_abname;
    }
    private static t_soundBean GetCSVConfigImp(int key)
    {
        t_soundBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_soundBean();
            bean._t_id = ReadInt(datas[index++]);
            bean._t_allow_multi = ReadInt(datas[index++]);
            bean._t_res_abname = ReadString(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_soundBean GetConfigImp(int key)
    {
        t_soundBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_soundBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_soundBean();
            bean._t_id = SqliteDataManager_ReadInt();
            bean._t_allow_multi = SqliteDataManager_ReadInt();
            bean._t_res_abname = SqliteDataManager_ReadString();
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