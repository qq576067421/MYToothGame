/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_endlessStageBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MonoBean
{
public partial class t_endlessStageBean:MonoBean.BeanBase
{
private static string m_FileName = "t_endlessStageBean.csv";
    public override int GetId_int()

    {
        return t_id;
    }

    private int _t_id;
    public int t_id{ get { return _t_id; }}
    private string _t_name;
    public string t_name{ get { return _t_name; }}
    private string _t_scene;
    public string t_scene{ get { return _t_scene; }}
    private List<List<long>> _t_monster_ids;
    public ReadOnlyCollection<ReadOnlyCollection<long>> t_monster_ids;
    private int _t_wave_interval_ms;
    public int t_wave_interval_ms{ get { return _t_wave_interval_ms; }}
    private int _t_Rewards_Exp;
    public int t_Rewards_Exp{ get { return _t_Rewards_Exp; }}
    private int _t_Rewards_Coin;
    public int t_Rewards_Coin{ get { return _t_Rewards_Coin; }}
    private int _t_hp_up1;
    public int t_hp_up1{ get { return _t_hp_up1; }}
    private int _t_hp_up2;
    public int t_hp_up2{ get { return _t_hp_up2; }}
    private int _t_hp_up3;
    public int t_hp_up3{ get { return _t_hp_up3; }}
    private int _t_hp_up4;
    public int t_hp_up4{ get { return _t_hp_up4; }}
    private static List<int> m_Keys = new List<int>();
    public static List<int> GetKeys()
    {
        if(m_Keys.Count == 0)
        {
            GetKeys("t_endlessStageBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<int, t_endlessStageBean> m_Dic = new Dictionary<int, t_endlessStageBean>(); 
    public static t_endlessStageBean GetConfig(int key, bool check_null = true)
    { 
        t_endlessStageBean bean = null; var className = "t_endlessStageBean";
        
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
    public void CopyFrom(t_endlessStageBean source)
    {
        _t_id = source._t_id;
        _t_name = source._t_name;
        _t_scene = source._t_scene;
        _t_monster_ids = source._t_monster_ids;
        t_monster_ids = source.t_monster_ids;
        _t_wave_interval_ms = source._t_wave_interval_ms;
        _t_Rewards_Exp = source._t_Rewards_Exp;
        _t_Rewards_Coin = source._t_Rewards_Coin;
        _t_hp_up1 = source._t_hp_up1;
        _t_hp_up2 = source._t_hp_up2;
        _t_hp_up3 = source._t_hp_up3;
        _t_hp_up4 = source._t_hp_up4;
    }
    private static t_endlessStageBean GetCSVConfigImp(int key)
    {
        t_endlessStageBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_endlessStageBean();
            bean._t_id = ReadInt(datas[index++]);
            bean._t_name = ReadString(datas[index++]);
            bean._t_scene = ReadString(datas[index++]);
            bean._t_monster_ids = ReadLongArray2(datas[index++]);
            bean.t_monster_ids = GetReadOnlyArray(bean._t_monster_ids);
            bean._t_wave_interval_ms = ReadInt(datas[index++]);
            bean._t_Rewards_Exp = ReadInt(datas[index++]);
            bean._t_Rewards_Coin = ReadInt(datas[index++]);
            bean._t_hp_up1 = ReadInt(datas[index++]);
            bean._t_hp_up2 = ReadInt(datas[index++]);
            bean._t_hp_up3 = ReadInt(datas[index++]);
            bean._t_hp_up4 = ReadInt(datas[index++]);
        }
        if(bean == null)
        {
            return null;
        }
        return bean; 
    }

    private static t_endlessStageBean GetConfigImp(int key)
    {
        t_endlessStageBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_endlessStageBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_endlessStageBean();
            bean._t_id = SqliteDataManager_ReadInt();
            bean._t_name = SqliteDataManager_ReadString();
            bean._t_scene = SqliteDataManager_ReadString();
            bean._t_monster_ids = SqliteDataManager_ReadLongArray2();
            bean.t_monster_ids = GetReadOnlyArray(bean._t_monster_ids);
            bean._t_wave_interval_ms = SqliteDataManager_ReadInt();
            bean._t_Rewards_Exp = SqliteDataManager_ReadInt();
            bean._t_Rewards_Coin = SqliteDataManager_ReadInt();
            bean._t_hp_up1 = SqliteDataManager_ReadInt();
            bean._t_hp_up2 = SqliteDataManager_ReadInt();
            bean._t_hp_up3 = SqliteDataManager_ReadInt();
            bean._t_hp_up4 = SqliteDataManager_ReadInt();
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