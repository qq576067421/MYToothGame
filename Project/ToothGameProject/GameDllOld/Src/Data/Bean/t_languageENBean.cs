/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_languageENBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_languageENBean:HF.BeanBase
{
private static string m_FileName = "t_languageENBean.csv";
    public override string GetId_string()

    {
        return t_id;
    }

    private string _t_id;
    public string t_id{ get { return _t_id; }}
    private string _t_content;
    public string t_content{ get { return _t_content; }}
    private static List<string> m_Keys = new List<string>();
    public static List<string> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_languageENBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<string, t_languageENBean> m_Dic = new Dictionary<string, t_languageENBean>(); 
    public static t_languageENBean GetConfig(string key, bool check_null = true)
    { 
        t_languageENBean bean = null; var className = "t_languageENBean";
        
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
    public void CopyFrom(t_languageENBean source)
    {
        _t_id = source._t_id;
        _t_content = source._t_content;
    }
    private static t_languageENBean GetCSVConfigImp(string key)
    {
        t_languageENBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_languageENBean();
            bean._t_id = ReadString(datas[index++]);
            bean._t_content = ReadString(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_languageENBean GetConfigImp(string key)
    {
        t_languageENBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_languageENBean where t_id = ");
        StringBuilder.Append("\""); 
        StringBuilder.Append(key); 
        StringBuilder.Append("\""); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_languageENBean();
            bean._t_id = SqliteDataManager_ReadString();
            bean._t_content = SqliteDataManager_ReadString();
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