using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MonoBean
{
    public class BeanBase
    {
        #region 需要在战斗和热更新表都有的注册委托
        public static Action<string> LogWarning;
        public static Action<string, List<int>> GeyKeysByListInt;
        public static Action<string, List<long>> GeyKeysByListLong;
        public static Action<string, List<string>> GeyKeysByListString;
        public static Func<string, int, List<string>> BeginReadByInt;
        public static Func<string, long, List<string>> BeginReadByLong;
        public static Func<string, string, List<string>> BeginReadByString;
        public static Func<string, Dictionary<int, List<string>>> __GetAllIntRows;
        public static Func<string, Dictionary<long, List<string>>> __GetAllLongRows;
        public static Func<string, Dictionary<string, List<string>>> __GetAllStringRows;
        public static Func<bool> IsUseCSV;
        public static Func<string, bool> SqliteDataManager_BeginRead;
        public static Action SqliteDataManager_EndRead;
        public static Func<int> SqliteDataManager_ReadInt;
        public static Func<long> SqliteDataManager_ReadLong;
        public static Func<string> SqliteDataManager_ReadString;
        public static Func<float> SqliteDataManager_ReadFloat;
        public static Func<byte[]> SqliteDataManager_ReadBytes;
        public static Func<List<int>> SqliteDataManager_ReadIntArray;
        public static Func<List<long>> SqliteDataManager_ReadLongArray;
        public static Func<List<List<long>>> SqliteDataManager_ReadLongArray2;
        public static Func<List<List<int>>> SqliteDataManager_ReadIntArray2;
        public static Func<Dictionary<int, long>> SqliteDataManager_ReadIntLongMap;
        public static Func<string,List<int>> ReadIntArray;
        public static Func<string,List<long>> ReadLongArray;
        public static Func<string,List<List<int>>> ReadIntArray2;
        public static Func<string,List<List<long>>> ReadLongArray2;
        public static Func<string,Dictionary<int, long>> ReadIntLongMap;
        #endregion
        public static StringBuilder StringBuilder = new StringBuilder();
        public virtual long GetId_long()
        {
            return -1;
        }
        public virtual int GetId_int()
        {
            return -1;
        }
        public virtual string GetId_string()
        {
            return "";
        }


        protected static List<int> GetKeys(string bean, List<int> keys)
        {
            if (keys.Count == 0)
            {
                //GameDll.DataManager.GetKeys(bean, keys);
                GeyKeysByListInt(bean, keys);
                return keys;
            }
            else
            {
                return keys;
            }
        }
        protected static List<long> GetKeys(string bean, List<long> keys)
        {
            if (keys.Count == 0)
            {
                //GameDll.DataManager.GetKeys(bean, keys);
                GeyKeysByListLong(bean, keys);
                return keys;
            }
            else
            {
                return keys;
            }
        }
        protected static List<string> GetKeys(string bean, List<string> keys)
        {
            if (keys.Count == 0)
            {
                //GameDll.DataManager.GetKeys(bean, keys);
                GeyKeysByListString(bean, keys);
                return keys;
            }
            else
            {
                return keys;
            }
        }
        public static int ReadInt(string data)
        {
            int i = int.MinValue;
            int.TryParse(data, out i);
            return i;
        }
        public static long ReadLong(string data)
        {
            long i = 0;
            long.TryParse(data, out i);
            return i;
        }

        public static string ReadString(string data)
        {
            return data;
        }

        public static List<string> BeginRead(string csv, int key)
        {
            return BeginReadByInt(csv, key);
        }
        public static List<string> BeginRead(string csv, long key)
        {
            return BeginReadByLong(csv, key);
        }
        public static List<string> BeginRead(string csv, string key)
        {
            return BeginReadByString(csv, key);
        }

        public static bool UseCsv()
        {
            return IsUseCSV();
        }

        //public static ReadOnlyCollection<T> GetReadOnlyArray<T>(List<T> src)
        //{
        //    return src.AsReadOnly();
        //}
        public static ReadOnlyCollection<int> GetReadOnlyArray(List<int> src)
        {
            return src.AsReadOnly();
        }
        public static ReadOnlyCollection<long> GetReadOnlyArray(List<long> src)
        {
            return src.AsReadOnly();
        }
        public static ReadOnlyCollection<string> GetReadOnlyArray(List<string> src)
        {
            return src.AsReadOnly();
        }

        //public static List<T> CopyList<T>(ReadOnlyCollection<T> src)
        //{
        //    List<T> dest = new List<T>();
        //    foreach (var item in src)
        //    {
        //        dest.Add(item);
        //    }
        //    return dest;
        //}
        public static List<int> CopyList(ReadOnlyCollection<int> src)
        {
            List<int> dest = new List<int>();
            foreach (var item in src)
            {
                dest.Add(item);
            }
            return dest;
        }
        public static List<long> CopyList(ReadOnlyCollection<long> src)
        {
            List<long> dest = new List<long>();
            foreach (var item in src)
            {
                dest.Add(item);
            }
            return dest;
        }
        public static List<string> CopyList(ReadOnlyCollection<string> src)
        {
            List<string> dest = new List<string>();
            foreach (var item in src)
            {
                dest.Add(item);
            }
            return dest;
        }
        //public static void CopyList<T>(ReadOnlyCollection<T> src, List<T> dest)
        //{
        //    foreach (var item in src)
        //    {
        //        dest.Add(item);
        //    }
        //}
        public static void CopyList(ReadOnlyCollection<int> src, List<int> dest)
        {
            foreach (var item in src)
            {
                dest.Add(item);
            }
        }
        public static void CopyList(ReadOnlyCollection<long> src, List<long> dest)
        {
            foreach (var item in src)
            {
                dest.Add(item);
            }
        }
        public static void CopyList(ReadOnlyCollection<string> src, List<string> dest)
        {
            foreach (var item in src)
            {
                dest.Add(item);
            }
        }

        //public static ReadOnlyCollection<ReadOnlyCollection<T>> GetReadOnlyArray<T>(List<List<T>> src)
        //{
        //    List<ReadOnlyCollection<T>> sub_list = new List<ReadOnlyCollection<T>>();
        //    foreach (var sub_src in src)
        //    {
        //        ReadOnlyCollection<T> r_sub_list = sub_src.AsReadOnly();
        //        sub_list.Add(r_sub_list);
        //    }
        //    return sub_list.AsReadOnly();
        //}
        public static ReadOnlyCollection<ReadOnlyCollection<int>> GetReadOnlyArray(List<List<int>> src)
        {
            List<ReadOnlyCollection<int>> sub_list = new List<ReadOnlyCollection<int>>();
            foreach (var sub_src in src)
            {
                ReadOnlyCollection<int> r_sub_list = sub_src.AsReadOnly();
                sub_list.Add(r_sub_list);
            }
            return sub_list.AsReadOnly();
        }
        public static ReadOnlyCollection<ReadOnlyCollection<long>> GetReadOnlyArray(List<List<long>> src)
        {
            List<ReadOnlyCollection<long>> sub_list = new List<ReadOnlyCollection<long>>();
            foreach (var sub_src in src)
            {
                ReadOnlyCollection<long> r_sub_list = sub_src.AsReadOnly();
                sub_list.Add(r_sub_list);
            }
            return sub_list.AsReadOnly();
        }
        public static ReadOnlyCollection<ReadOnlyCollection<string>> GetReadOnlyArray(List<List<string>> src)
        {
            List<ReadOnlyCollection<string>> sub_list = new List<ReadOnlyCollection<string>>();
            foreach (var sub_src in src)
            {
                ReadOnlyCollection<string> r_sub_list = sub_src.AsReadOnly();
                sub_list.Add(r_sub_list);
            }
            return sub_list.AsReadOnly();
        }
        //public static ReadOnlyDictionary<K, V> GetReadOnlyArray<K, V>(Dictionary<K,V> src)
        //{
        //    return new ReadOnlyDictionary<K,V>(src);
        //}
        public static ReadOnlyDictionary<int, long> GetReadOnlyArray(Dictionary<int, long> src)
        {
            return new ReadOnlyDictionary<int, long>(src);
        }
    }
}