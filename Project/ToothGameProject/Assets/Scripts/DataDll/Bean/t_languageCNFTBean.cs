/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_languageCNFTBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_languageCNFTBean:MonoBean.BeanBase
{
private static string m_FileName = "t_languageCNFTBean.csv";
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
            GetKeys("t_languageCNFTBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<string, t_languageCNFTBean> m_Dic = new Dictionary<string, t_languageCNFTBean>(); 
    public static t_languageCNFTBean GetConfig(string key, bool check_null = true)
    { 
        t_languageCNFTBean bean = null; var className = "t_languageCNFTBean";
        
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
    public void CopyFrom(t_languageCNFTBean source)
    {
        _t_id = source._t_id;
        _t_content = source._t_content;
    }
    private static t_languageCNFTBean GetCSVConfigImp(string key)
    {
        t_languageCNFTBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_languageCNFTBean();
            bean._t_id = ReadString(datas[index++]);
            bean._t_content = ReadString(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_languageCNFTBean GetConfigImp(string key)
    {
        t_languageCNFTBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_languageCNFTBean where t_id = ");
        StringBuilder.Append("\""); 
        StringBuilder.Append(key); 
        StringBuilder.Append("\""); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_languageCNFTBean();
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