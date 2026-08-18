using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace GameDll
{
    public class DataManager
    {
        private static Dictionary<string, Dictionary<int, List<string>>> m_IntDatas = new Dictionary<string, Dictionary<int, List<string>>>();
        private static Dictionary<string, Dictionary<long, List<string>>> m_LongDatas = new Dictionary<string, Dictionary<long, List<string>>>();
        private static Dictionary<string, Dictionary<string, List<string>>> m_StringDatas = new Dictionary<string, Dictionary<string, List<string>>>();
        private static List<string> m_Tables = new List<string>();
        public static bool Init()
        {
            if (RenderAPI.IsUseCSV())
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();
#if !UNITY_EDITOR
            try
            {
#endif
                {
                    var table = new Dictionary<int, List<string>>();
                    LoadCSV("t_tableBean", table, null, null);
                    m_IntDatas.Add("t_tableBean.csv", table);
                }
                foreach (var t in m_Tables)
                {
                    var int_table = new Dictionary<int, List<string>>();
                    var long_table = new Dictionary<long, List<string>>();
                    var string_table = new Dictionary<string, List<string>>();
                    LoadCSV(t, int_table, long_table, string_table);
                    m_IntDatas.Add(t + ".csv", int_table);
                    m_LongDatas.Add(t + ".csv", long_table);
                    m_StringDatas.Add(t + ".csv", string_table);
                }
#if !UNITY_EDITOR
            }
            catch (Exception e)
            {
                UDebug.LogError("读取数据表错误：" + e.ToString());
            }
#endif
                watch.Stop();

                long time = watch.ElapsedMilliseconds;
                UDebug.Log("加载配置表耗时：" + time + "ms");
                return true;
            }
            return false;
        }
        private static Dictionary<string, string> m_LanguageDatas = new Dictionary<string, string>();
        public static void LoadLanguage(string lan = "CN")
        {
            if (RenderAPI.IsUseCSV())
            {
                string table = "t_language" + lan + "Bean";

                m_LanguageDatas.Clear();

                var string_table = new Dictionary<string, List<string>>();

                LoadCSV(table, null, null, string_table);

                foreach (var kv in string_table)
                {
                    if(kv.Value.Count < 2)
                    {
                        UDebug.LogError("lan has error, lan:" + lan + " key:" + kv.Key);
                    }
                    m_LanguageDatas.Add(kv.Key, kv.Value[1]);
                }
            }
            else
            {
                //SqliteDataManager.LoadLanguage(lan);
            }
        }

        //public static bool IsDataError()
        //{
        //    if (RenderAPI.IsUseCSV())
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        return SqliteDataManager.IsDataError();
        //    }
        //}
        public static string GetLanguageByKey(string key)
        {
            if (RenderAPI.IsUseCSV())
            {
                if (m_LanguageDatas.ContainsKey(key))
                {
                    return m_LanguageDatas[key];
                }
                else
                {
                    return key + ".";
                }
            }
            else
            {
                return RenderAPI.ReadLanguageByKey(key);
            }
        }
        

        private static void LoadCSV(string csv, 
            Dictionary<int, List<string>> int_table, 
            Dictionary<long, List<string>> long_table,
            Dictionary<string, List<string>> string_table
            )
        {
            string out_path = "";
            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXEditor)
            {
                out_path = LCL.MonoTool.GetDevelopTablePath();
            }
            else
            {
                out_path = LCL.MonoTool.GetPersistentPath() + "codeconfig/config/";
            }

            string out_file = out_path + csv + ".csv";
            bool test_load_resource = false;
            if (File.Exists(out_file) && !test_load_resource)
            {
                using (FileStream fs = new FileStream(out_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {

                    using (StreamReader sr = new StreamReader(fs))
                    {
                        ReadCSV(sr, csv, int_table, long_table, string_table);
                    }
                }
            }
            else
            {
                string inner_file = "config/" + csv + ".csv";
                var text = Resources.Load<TextAsset>(inner_file);
                if(text == null)
                {
                    UDebug.LogError("没有找到配置表：" + csv);
                    return;
                }
                using (MemoryStream ms = new MemoryStream(text.bytes))
                {
                    using (StreamReader sr = new StreamReader(ms))
                    {
                        ReadCSV(sr,csv,int_table,long_table,string_table);
                    }
                }
            }
        }

        private static void ReadCSV(StreamReader sr,
            string csv,
            Dictionary<int, List<string>> int_table,
            Dictionary<long, List<string>> long_table,
            Dictionary<string, List<string>> string_table)
        {
            sr.ReadLine();
            sr.ReadLine();
            var types = sr.ReadLine().Split(',');
            sr.ReadLine();
            var keytype = types[0];
            if (keytype == "int32" || keytype == "int")
            {
                keytype = "int";
            }
            else if (keytype == "int64" || keytype == "long")
            {
                keytype = "long";
            }
            else
            {
                keytype = "string";
            }

            var row = sr.ReadLine();
            while (!string.IsNullOrEmpty(row))
            {
                string[] cells = GetStrings(row);
                if(cells.Length == 0)
                {
                    UDebug.LogError("配置表错误，某一行没有数据， 表：" + csv + " row:" + row);
                }
                int int_id = 0;
                long long_id = 0;
                string string_id = "";
                bool key_right = false;
                if (keytype == "int")
                {
                    key_right = int.TryParse(cells[0], out int_id);
                }
                else if (keytype == "long")
                {
                    key_right = long.TryParse(cells[0], out long_id);
                }
                else
                {
                    string_id = cells[0];
                    key_right = !string.IsNullOrEmpty(cells[0]);
                }

                if (!key_right)
                {
                    UDebug.LogWarning("表：" + csv + "读表错误,t_id字段是空字符串，检查CSV表是否有空行，行：" + row);
                }
                else
                {
                    var list_cells = new List<string>();
                    list_cells.AddRange(cells);

                    if (keytype == "int")
                    {
                        if (int_table.ContainsKey(int_id))
                        {
                            int_table[int_id] = list_cells;
                            UDebug.LogWarning("表：" + csv + "读表错误,t_id字段重复，后面的覆盖前面的数据，行：" + row);
                        }
                        else
                        {
                            int_table.Add(int_id, list_cells);
                        }
                    }
                    else if (keytype == "long")
                    {
                        if (long_table.ContainsKey(long_id))
                        {
                            long_table[long_id] = list_cells;
                            UDebug.LogWarning("表：" + csv + "读表错误,t_id字段重复，后面的覆盖前面的数据，行：" + row);
                        }
                        else
                        {
                            long_table.Add(long_id, list_cells);
                        }
                    }
                    else
                    {
                        if (string_table.ContainsKey(string_id))
                        {
                            string_table[string_id] = list_cells;
                            UDebug.LogWarning("表：" + csv + "读表错误,t_id字段重复，后面的覆盖前面的数据，行：" + row);
                        }
                        else
                        {
                            string_table.Add(string_id, list_cells);
                        }
                    }
                }
                if (sr.EndOfStream)
                {
                    break;
                }
                else
                {
                    row = sr.ReadLine();
                }
            }

            if (csv == "t_tableBean")
            {
                foreach (var r in int_table)
                {
                    string table_str = r.Value[1];
                    if (table_str == csv)
                    {
                        continue;
                    }
                    m_Tables.Add(r.Value[1]);
                }
            }
        }


        private static bool _isOddDoubleQuota(string str)
        {
            return _getDoubleQuotaCount(str) % 2 == 1;
        }

        private static int _getDoubleQuotaCount(string str)
        {
            if(str.Contains("\""))
            {
                string[] strArray = str.Split('"');
                int doubleQuotaCount = strArray.Length - 1;
                doubleQuotaCount = doubleQuotaCount < 0 ? 0 : doubleQuotaCount;
                return doubleQuotaCount;
            }
            else
            {
                return 0;
            }

        }

        public static string[] GetStrings(string instr)
        {
            bool useRegex = false;
            if (!useRegex)
            {
                instr = instr.Replace("\"\"", "\"");
                string[] lineInfoArray = instr.Split(',');
                List<string> rowItemList = new List<string>();
                string strTemp = string.Empty;
                for (int j = 0; j < lineInfoArray.Length; j++)
                {
                    strTemp += lineInfoArray[j];
                    if (_isOddDoubleQuota(strTemp))
                    {
                        if (j != lineInfoArray.Length - 1)
                        {
                            strTemp += ",";
                        }
                    }
                    else
                    {
                        if (strTemp.StartsWith("\"") && strTemp.EndsWith("\""))
                        {
                            strTemp = strTemp.Substring(1, strTemp.Length - 2);
                        }
                        rowItemList.Add(strTemp);
                        strTemp = string.Empty;
                    }
                }
                return rowItemList.ToArray();
            }
            else
            {
                List<string> hr = new List<string>();
                var mc = System.Text.RegularExpressions.Regex.Matches(instr, "(?<=^|,)[^\"]*?(?=,|$)|(?<=^|,\")(?:(\"\")?[^\"]*?)*(?=\",?|$)");
                foreach (System.Text.RegularExpressions.Match m in mc)
                {
                    hr.Add(m.Value);
                }


                return hr.ToArray();
            }
        }

        public static void Destroy()
        {
            if (RenderAPI.IsUseCSV())
            {
                m_Tables.Clear();
                m_IntDatas.Clear();
                m_LongDatas.Clear();
                m_StringDatas.Clear();
            }
            //else
            //{
            //    SqliteDataManager.Destroy();
            //}
        }
        public static List<string> BeginRead(string csv, int key)
        {
            if (m_IntDatas.ContainsKey(csv))
            {
                var datas = m_IntDatas[csv];
                if (datas.ContainsKey(key))
                {
                    return datas[key];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static List<string> BeginRead(string csv, long key)
        {
            if (m_LongDatas.ContainsKey(csv))
            {
                var datas = m_LongDatas[csv];
                if (datas.ContainsKey(key))
                {
                    return datas[key];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public static List<string> BeginRead(string csv, string key)
        {
            if (m_StringDatas.ContainsKey(csv))
            {
                var datas = m_StringDatas[csv];
                if (datas.ContainsKey(key))
                {
                    return datas[key];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static void GetKeys(string table, List<int> keys)
        {
            if (RenderAPI.IsUseCSV())
            {
                string fileName = table + ".csv";
                if (m_IntDatas.ContainsKey(fileName))
                {
                    var csv = m_IntDatas[fileName];
                    keys.AddRange(csv.Keys);
                }
            }
            //else
            //{
            //    SqliteDataManager.GetKeys(table, keys);
            //}
        }

        public static void GetKeys(string table, List<long> keys)
        {
            if (RenderAPI.IsUseCSV())
            {
                string fileName = table + ".csv";
                if (m_IntDatas.ContainsKey(fileName))
                {
                    var csv = m_LongDatas[fileName];
                    keys.AddRange(csv.Keys);
                }
            }
            //else
            //{
            //    SqliteDataManager.GetKeys(table, keys);
            //}
        }

        public static void GetKeys(string table, List<string> keys)
        {
            if (RenderAPI.IsUseCSV())
            {
                string fileName = table + ".csv";
                if (m_IntDatas.ContainsKey(fileName))
                {
                    var csv = m_StringDatas[fileName];
                    keys.AddRange(csv.Keys);
                }
            }
            //else
            //{
            //    SqliteDataManager.GetKeys(table, keys);
            //}
        }

        public static Dictionary<int, List<string>> GetAllIntRows(string csv)
        {
            if (RenderAPI.IsUseCSV())
            {
                if (m_IntDatas.ContainsKey(csv))
                {
                    return m_IntDatas[csv];
                }
                else
                {
                    return null;
                }
            }
            return null;
            //else
            //{
            //    return SqliteDataManager.GetAllIntRows(csv.Replace(".csv", ""));
            //}
        }

        public static Dictionary<long, List<string>> GetAllLongRows(string csv)
        {
            if (RenderAPI.IsUseCSV())
            {
                if (m_IntDatas.ContainsKey(csv))
                {
                    return m_LongDatas[csv];
                }
                else
                {
                    return null;
                }
            }
            return null;
            //else
            //{
            //    return SqliteDataManager.GetAllLongRows(csv.Replace(".csv", ""));
            //}
        }

        public static Dictionary<string, List<string>> GetAllStringRows(string csv)
        {
            if (RenderAPI.IsUseCSV())
            {
                if (m_IntDatas.ContainsKey(csv))
                {
                    return m_StringDatas[csv];
                }
                else
                {
                    return null;
                }
            }
            return null;
            //else
            //{
            //    return SqliteDataManager.GetAllStringRows(csv.Replace(".csv", ""));
            //}
        }

        public static List<int> ReadIntArray(string data)
        {
            List<int> list = new List<int>();
            if (string.IsNullOrEmpty(data))
            {
                return list;
            }
            Tool.ParseInts(list, data, '+');
            return list;
        }
        public static List<long> ReadLongArray(string data)
        {
            List<long> list = new List<long>();
            if (string.IsNullOrEmpty(data))
            {
                return list;
            }
            Tool.ParseLongs(list, data, '+');
            return list;
        }

        public static List<List<int>> ReadIntArray2(string data)
        {
            List<List<int>> listlist = new List<List<int>>();
            if (string.IsNullOrEmpty(data))
            {
                return listlist;
            }
            var rows = data.Split('|');
            int count = rows.Length;
            for (int i = 0; i < count; ++i)
            {
                List<int> list = new List<int>();
                Tool.ParseInts(list, rows[i], '+');
                listlist.Add(list);
            }
            return listlist;
        }
        public static List<List<long>> ReadLongArray2(string data)
        {
            List<List<long>> listlist = new List<List<long>>();
            if (string.IsNullOrEmpty(data))
            {
                return listlist;
            }
            var rows = data.Split('|');
            int count = rows.Length;
            for (int i = 0; i < count; ++i)
            {
                List<long> list = new List<long>();
                Tool.ParseLongs(list, rows[i], '+');
                listlist.Add(list);
            }
            return listlist;
        }
        public static Dictionary<int, long> ReadIntLongMap(string data)
        {
            Dictionary<int, long> dict = new Dictionary<int, long>();
            if (string.IsNullOrEmpty(data))
            {
                return dict;
            }
            var rows = data.Split('|');
            int count = rows.Length;
            for (int i = 0; i < count; ++i)
            {
                var col = rows[i].Split('+');
                int key = int.Parse(col[0]);
                long value = long.Parse(col[1]);
                dict.Add(key, value);
            }
            return dict;
        }
        //初始化主工程的数据程序集的调用函数
        public static void Bind()
        {
            MonoBean.BeanBase.LogWarning = Debug.LogWarning;
            MonoBean.BeanBase.GeyKeysByListInt = DataManager.GetKeys;
            MonoBean.BeanBase.GeyKeysByListLong = DataManager.GetKeys;
            MonoBean.BeanBase.GeyKeysByListString = DataManager.GetKeys;
            MonoBean.BeanBase.BeginReadByInt = DataManager.BeginRead;
            MonoBean.BeanBase.BeginReadByLong = DataManager.BeginRead;
            MonoBean.BeanBase.BeginReadByString = DataManager.BeginRead;
            MonoBean.BeanBase.__GetAllIntRows = DataManager.GetAllIntRows;
            MonoBean.BeanBase.__GetAllLongRows = DataManager.GetAllLongRows;
            MonoBean.BeanBase.__GetAllStringRows = DataManager.GetAllStringRows;
            MonoBean.BeanBase.IsUseCSV = RenderAPI.IsUseCSV;
            //MonoBean.BeanBase.SqliteDataManager_BeginRead = SqliteDataManager.BeginRead;
            //MonoBean.BeanBase.SqliteDataManager_EndRead = SqliteDataManager.EndRead;
            //MonoBean.BeanBase.SqliteDataManager_ReadInt = SqliteDataManager.ReadInt;
            //MonoBean.BeanBase.SqliteDataManager_ReadLong = SqliteDataManager.ReadLong;
            //MonoBean.BeanBase.SqliteDataManager_ReadString = SqliteDataManager.ReadString;
            //MonoBean.BeanBase.SqliteDataManager_ReadFloat = SqliteDataManager.ReadFloat;
            //MonoBean.BeanBase.SqliteDataManager_ReadBytes = SqliteDataManager.ReadBytes;
            //MonoBean.BeanBase.SqliteDataManager_ReadIntArray = SqliteDataManager.ReadIntArray;
            //MonoBean.BeanBase.SqliteDataManager_ReadLongArray = SqliteDataManager.ReadLongArray;
            //MonoBean.BeanBase.SqliteDataManager_ReadLongArray2 = SqliteDataManager.ReadLongArray2;
            //MonoBean.BeanBase.SqliteDataManager_ReadIntArray2 = SqliteDataManager.ReadIntArray2;
            //MonoBean.BeanBase.SqliteDataManager_ReadIntLongMap = SqliteDataManager.ReadIntLongMap; 
            MonoBean.BeanBase.ReadIntArray = DataManager.ReadIntArray; 
            MonoBean.BeanBase.ReadLongArray = DataManager.ReadLongArray; 
            MonoBean.BeanBase.ReadIntArray2 = DataManager.ReadIntArray2; 
            MonoBean.BeanBase.ReadLongArray2 = DataManager.ReadLongArray2; 
            MonoBean.BeanBase.ReadIntLongMap = DataManager.ReadIntLongMap; 
        }
    }
}