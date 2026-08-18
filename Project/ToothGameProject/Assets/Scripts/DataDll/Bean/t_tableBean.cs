/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_tableBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_tableBean:MonoBean.BeanBase
{
private static string m_FileName = "t_tableBean.csv";
    public override int GetId_int()

    {
        return t_id;
    }

    private int _t_id;
    public int t_id{ get { return _t_id; }}
    private string _t_csv;
    public string t_csv{ get { return _t_csv; }}
    private static List<int> m_Keys = new List<int>();
    public static List<int> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_tableBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<int, t_tableBean> m_Dic = new Dictionary<int, t_tableBean>(); 
    public static t_tableBean GetConfig(int key, bool check_null = true)
    { 
        t_tableBean bean = null; var className = "t_tableBean";
        
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
    public void CopyFrom(t_tableBean source)
    {
        _t_id = source._t_id;
        _t_csv = source._t_csv;
    }
    private static t_tableBean GetCSVConfigImp(int key)
    {
        t_tableBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_tableBean();
            bean._t_id = ReadInt(datas[index++]);
            bean._t_csv = ReadString(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_tableBean GetConfigImp(int key)
    {
        t_tableBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_tableBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_tableBean();
            bean._t_id = SqliteDataManager_ReadInt();
            bean._t_csv = SqliteDataManager_ReadString();
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