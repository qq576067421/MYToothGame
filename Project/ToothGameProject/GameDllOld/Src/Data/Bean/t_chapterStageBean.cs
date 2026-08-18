/**
 * Auto generated, do not edit it
 *Author lichunlin
 * t_chapterStageBean
 */
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace HF
{
public partial class t_chapterStageBean:HF.BeanBase
{
private static string m_FileName = "t_chapterStageBean.csv";
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
    private List<List<long>> _t_monster_ids0;
    public ReadOnlyCollection<ReadOnlyCollection<long>> t_monster_ids0;
    private List<List<long>> _t_monster_ids1;
    public ReadOnlyCollection<ReadOnlyCollection<long>> t_monster_ids1;
    private List<List<long>> _t_monster_ids2;
    public ReadOnlyCollection<ReadOnlyCollection<long>> t_monster_ids2;
    private List<List<long>> _t_monster_ids3;
    public ReadOnlyCollection<ReadOnlyCollection<long>> t_monster_ids3;
    private List<List<long>> _t_monster_ids4;
    public ReadOnlyCollection<ReadOnlyCollection<long>> t_monster_ids4;
    private List<List<long>> _t_monster_ids5;
    public ReadOnlyCollection<ReadOnlyCollection<long>> t_monster_ids5;
    private int _t_first_wave_delay_ms;
    public int t_first_wave_delay_ms{ get { return _t_first_wave_delay_ms; }}
    private int _t_wave_interval_ms;
    public int t_wave_interval_ms{ get { return _t_wave_interval_ms; }}
    private int _t_Rewards_Exp;
    public int t_Rewards_Exp{ get { return _t_Rewards_Exp; }}
    private int _t_Rewards_Coin;
    public int t_Rewards_Coin{ get { return _t_Rewards_Coin; }}
    private List<long> _t_speak;
    public ReadOnlyCollection<long> t_speak;
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
            GetKeys("t_chapterStageBean", m_Keys);
            return m_Keys;
        }
        else
        {
            return m_Keys;
        }
    }
    private static Dictionary<int, t_chapterStageBean> m_Dic = new Dictionary<int, t_chapterStageBean>(); 
    public static t_chapterStageBean GetConfig(int key, bool check_null = true)
    { 
        t_chapterStageBean bean = null; var className = "t_chapterStageBean";
        
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
    public void CopyFrom(t_chapterStageBean source)
    {
        _t_id = source._t_id;
        _t_name = source._t_name;
        _t_scene = source._t_scene;
        _t_monster_ids0 = source._t_monster_ids0;
        t_monster_ids0 = source.t_monster_ids0;
        _t_monster_ids1 = source._t_monster_ids1;
        t_monster_ids1 = source.t_monster_ids1;
        _t_monster_ids2 = source._t_monster_ids2;
        t_monster_ids2 = source.t_monster_ids2;
        _t_monster_ids3 = source._t_monster_ids3;
        t_monster_ids3 = source.t_monster_ids3;
        _t_monster_ids4 = source._t_monster_ids4;
        t_monster_ids4 = source.t_monster_ids4;
        _t_monster_ids5 = source._t_monster_ids5;
        t_monster_ids5 = source.t_monster_ids5;
        _t_first_wave_delay_ms = source._t_first_wave_delay_ms;
        _t_wave_interval_ms = source._t_wave_interval_ms;
        _t_Rewards_Exp = source._t_Rewards_Exp;
        _t_Rewards_Coin = source._t_Rewards_Coin;
        _t_speak = source._t_speak;
        t_speak = source.t_speak;
        _t_hp_up1 = source._t_hp_up1;
        _t_hp_up2 = source._t_hp_up2;
        _t_hp_up3 = source._t_hp_up3;
        _t_hp_up4 = source._t_hp_up4;
    }
    private static t_chapterStageBean GetCSVConfigImp(int key)
    {
        t_chapterStageBean bean = null;
        var datas =BeginRead(m_FileName, key); 
        if (datas != null)
        {
            int index = 0;
            bean = new t_chapterStageBean();
            bean._t_id = ReadInt(datas[index++]);
            bean._t_name = ReadString(datas[index++]);
            bean._t_scene = ReadString(datas[index++]);
            bean._t_monster_ids0 = ReadLongArray2(datas[index++]);
            bean.t_monster_ids0 = GetReadOnlyArray(bean._t_monster_ids0);
            bean._t_monster_ids1 = ReadLongArray2(datas[index++]);
            bean.t_monster_ids1 = GetReadOnlyArray(bean._t_monster_ids1);
            bean._t_monster_ids2 = ReadLongArray2(datas[index++]);
            bean.t_monster_ids2 = GetReadOnlyArray(bean._t_monster_ids2);
            bean._t_monster_ids3 = ReadLongArray2(datas[index++]);
            bean.t_monster_ids3 = GetReadOnlyArray(bean._t_monster_ids3);
            bean._t_monster_ids4 = ReadLongArray2(datas[index++]);
            bean.t_monster_ids4 = GetReadOnlyArray(bean._t_monster_ids4);
            bean._t_monster_ids5 = ReadLongArray2(datas[index++]);
            bean.t_monster_ids5 = GetReadOnlyArray(bean._t_monster_ids5);
            bean._t_first_wave_delay_ms = ReadInt(datas[index++]);
            bean._t_wave_interval_ms = ReadInt(datas[index++]);
            bean._t_Rewards_Exp = ReadInt(datas[index++]);
            bean._t_Rewards_Coin = ReadInt(datas[index++]);
            bean._t_speak = ReadLongArray(datas[index++]);
            bean.t_speak = GetReadOnlyArray(bean._t_speak);
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

    private static t_chapterStageBean GetConfigImp(int key)
    {
        t_chapterStageBean bean = null;
        StringBuilder.Clear();
        StringBuilder.Append("select * from t_chapterStageBean where t_id = ");
        StringBuilder.Append(key); 
        if(SqliteDataManager_BeginRead(StringBuilder.ToString()))
        {
            bean = new t_chapterStageBean();
            bean._t_id = SqliteDataManager_ReadInt();
            bean._t_name = SqliteDataManager_ReadString();
            bean._t_scene = SqliteDataManager_ReadString();
            bean._t_monster_ids0 = SqliteDataManager_ReadLongArray2();
            bean.t_monster_ids0 = GetReadOnlyArray(bean._t_monster_ids0);
            bean._t_monster_ids1 = SqliteDataManager_ReadLongArray2();
            bean.t_monster_ids1 = GetReadOnlyArray(bean._t_monster_ids1);
            bean._t_monster_ids2 = SqliteDataManager_ReadLongArray2();
            bean.t_monster_ids2 = GetReadOnlyArray(bean._t_monster_ids2);
            bean._t_monster_ids3 = SqliteDataManager_ReadLongArray2();
            bean.t_monster_ids3 = GetReadOnlyArray(bean._t_monster_ids3);
            bean._t_monster_ids4 = SqliteDataManager_ReadLongArray2();
            bean.t_monster_ids4 = GetReadOnlyArray(bean._t_monster_ids4);
            bean._t_monster_ids5 = SqliteDataManager_ReadLongArray2();
            bean.t_monster_ids5 = GetReadOnlyArray(bean._t_monster_ids5);
            bean._t_first_wave_delay_ms = SqliteDataManager_ReadInt();
            bean._t_wave_interval_ms = SqliteDataManager_ReadInt();
            bean._t_Rewards_Exp = SqliteDataManager_ReadInt();
            bean._t_Rewards_Coin = SqliteDataManager_ReadInt();
            bean._t_speak = SqliteDataManager_ReadLongArray();
            bean.t_speak = GetReadOnlyArray(bean._t_speak);
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