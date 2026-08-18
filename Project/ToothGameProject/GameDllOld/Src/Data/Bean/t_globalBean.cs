/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_globalBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_globalBean:HF.BeanBase
{
private static string m_FileName = "t_globalBean.csv";
    public override int GetId_int()

    {
        return t_id;
    }

    private int _t_id;
    public int t_id{ get { return _t_id; }}
    private int _t_int;
    public int t_int{ get { return _t_int; }}
    private string _t_string;
    public string t_string{ get { return _t_string; }}
    private static List<int> m_Keys = new List<int>();
    public static List<int> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_globalBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<int, t_globalBean> m_Dic = new Dictionary<int, t_globalBean>(); 
    public static t_globalBean GetConfig(int key, bool check_null = true)
    { 
        t_globalBean bean = null; var className = "t_globalBean";
        
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
    public void CopyFrom(t_globalBean source)
    {
        _t_id = source._t_id;
        _t_int = source._t_int;
        _t_string = source._t_string;
    }
    private static t_globalBean GetCSVConfigImp(int key)
    {
        t_globalBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_globalBean();
            bean._t_id = ReadInt(datas[index++]);
            bean._t_int = ReadInt(datas[index++]);
            bean._t_string = ReadString(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_globalBean GetConfigImp(int key)
    {
        t_globalBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_globalBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_globalBean();
            bean._t_id = SqliteDataManager_ReadInt();
            bean._t_int = SqliteDataManager_ReadInt();
            bean._t_string = SqliteDataManager_ReadString();
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