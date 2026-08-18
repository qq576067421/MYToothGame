using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace LCL
{
    public class MonoTool
    {
        public static string GetRuntimePlatformName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.WebGLPlayer:
                    return "webgl";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                case RuntimePlatform.WindowsPlayer:
                    return "windows";
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
#if UNITY_ANDROID
                    return "android";
#elif UNITY_IOS
                    return "ios";
#elif UNITY_EDITOR_OSX
                    return "mac";
#else
                    return "windows";
#endif
                case RuntimePlatform.OSXPlayer:
                    return "mac";
                // Add more build targets for your own.
                default:
                    return "android";
            }
        }

        //public static float GetInputAxisX()
        //{
        //    return CnControls.CnInputManager.GetAxis("Horizontal");
        //}
        //public static float GetInputAxisY()
        //{
        //    return CnControls.CnInputManager.GetAxis("Vertical");
        //}
        //public static void SetMoveBase(bool canMove)
        //{
        //    CnControls.SimpleJoystick joyStick = GameObject.FindObjectOfType<CnControls.SimpleJoystick>();
        //    if (joyStick != null)
        //    {
        //        joyStick.MoveBase = canMove;
        //    }
        //}
        public static Component AddComponent(Component go, Type t)
        {
            if (go != null)
            {
                return go.gameObject.AddComponent(t);
            }
            else
            {
                return null;
            }
        }

        public static bool isLoadAssetBundleManifest { get; set; }
        //将服务器的时间戳UTC时间转换为服务器本地时间
        public static DateTime GetServerStampUTCToServerDateTime(long serverTimestampUTCMs, int serverTimeZoneOffsetHours)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime utcTime = epoch.AddMilliseconds(serverTimestampUTCMs);
            return utcTime.AddHours(serverTimeZoneOffsetHours);
        }
        //将服务器的时间戳UTC时间转化为客户端本地时间,用做显示
        public static DateTime GetServerStampUTCToClientDateTime(long serverTimestampUTCMs)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime utcTime = epoch.AddMilliseconds(serverTimestampUTCMs);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);
        }

        //已知配置表配置的是服务器的本地时间config_server_local_year_month_hour_etc，格式"yyyy-MM-dd HH:mm:ss"
        //已知服务器的时区。
        //求客户端的显示时间
        public static DateTime GetConfigTimeToClientDateTimeExact(string config_server_local_year_month_hour_etc, int serverTimeZoneOffsetHours)
        {
            // 解析配置表中的时间字符串
            DateTime serverLocalTime = DateTime.ParseExact(config_server_local_year_month_hour_etc, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            // 将服务器本地时间转换为UTC时间
            DateTime utcTime = serverLocalTime.AddHours(-serverTimeZoneOffsetHours);

            // 获取客户端本地时区
            TimeZoneInfo clientTimeZone = TimeZoneInfo.Local;

            // 将UTC时间转换为客户端本地时间
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, clientTimeZone);
        }
        //已知配置表配置的是服务器的本地时间config_server_local_year_month_hour_etc
        //已知服务器的时区。
        //求客户端的显示时间
        public static DateTime GetConfigTimeToClientDateTime(string config_server_local_year_month_hour_etc, int serverTimeZoneOffsetHours)
        {
            // 解析配置表中的时间字符串
            DateTime serverLocalTime = DateTime.Parse(config_server_local_year_month_hour_etc, CultureInfo.InvariantCulture);

            // 将服务器本地时间转换为UTC时间
            DateTime utcTime = serverLocalTime.AddHours(-serverTimeZoneOffsetHours);

            // 获取客户端本地时区
            TimeZoneInfo clientTimeZone = TimeZoneInfo.Local;

            // 将UTC时间转换为客户端本地时间
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, clientTimeZone);
        }
        /// <summary>
        /// 获取客户端本地时间的当日秒数(0-86399)
        /// </summary>
        public static long GetClientSecondTimeOfDayLocal()
        {
            // 直接获取本地时间并计算当日秒数
            return (long)DateTime.Now.TimeOfDay.TotalSeconds;
        }
        /// <summary>
        /// 根据服务器当日秒数和时区小时数计算客户端本地时间的当日秒数
        /// </summary>
        /// <param name="server_seconds_of_day">服务器当日已过秒数(0-86399)</param>
        /// <param name="server_zone_hours">服务器时区小时数（如东八区为8）</param>
        /// <returns>客户端本地时间的当日秒数</returns>
        public static long GetClientSecondTimeOfDayByServer(
            long server_seconds_of_day,
            int server_zone_hours)
        {
            // 参数校验
            if (server_seconds_of_day < 0 || server_seconds_of_day >= 86400)
                throw new ArgumentOutOfRangeException(nameof(server_seconds_of_day));

            if (server_zone_hours < -12 || server_zone_hours > 14)
                throw new ArgumentOutOfRangeException(nameof(server_zone_hours));

            // 1. 构造服务器UTC时间（当前UTC日期 + 时区偏移 + 当日秒数）
            DateTime utcNow = DateTime.UtcNow;
            DateTime serverUtcTime = utcNow.Date
                .AddHours(server_zone_hours)
                .AddSeconds(server_seconds_of_day);

            // 2. 转换为客户端本地时间（自动处理本地时区）
            DateTime clientTime = TimeZoneInfo.ConvertTimeFromUtc(serverUtcTime, TimeZoneInfo.Local);

            // 3. 计算并返回当日秒数
            return (long)clientTime.TimeOfDay.TotalSeconds;
        }
        //已知配置表配置的是服务器的本地时间config_server_local_time_ms
        //已知服务器的时区。
        //求客户端的显示时间
        public static DateTime GetConfigTimeToClientDateTime(long config_server_local_time_ms, int serverTimeZoneOffsetHours)
        {
            // 1. 将服务器本地时间转为UTC时间
            DateTime serverLocalTime = DateTimeOffset.FromUnixTimeMilliseconds(config_server_local_time_ms).DateTime;
            DateTime utcTime = serverLocalTime.AddHours(-serverTimeZoneOffsetHours);

            // 2. 将UTC时间转为客户端本地时间（自动处理夏令时）
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);
        }


        // 将指定的时间根据指定的时区转换为时间戳
        public static long DateTimeToStamp(DateTime time, int timeZoneOffsetHours)
        {
            DateTime utcEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime adjustedTime = time.AddHours(-timeZoneOffsetHours);
            return (long)(adjustedTime - utcEpoch).TotalMilliseconds;
        }
        /// <summary> 
        /// 获取时间戳()
        /// </summary> 
        /// <returns></returns> 
        public static long GetTimeStampUTCMs()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds);
        }



        public static string GetWebRequestDataPathHeader()
        {
            return "file://";
        }
        //如果代码中把GetWWWDataPathHeader 和GetDataPath结合使用的话，请直接使用下面这个接口
        private static string m_WWWDataPath;
        public static string GetWWWDataPath()
        {
            if (m_WWWDataPath != null)
            {
                return m_WWWDataPath;
            }
            string downPath = null;
            if(Application.platform == RuntimePlatform.Android)
            {
                downPath = GetDataPath();
            }
            else
            {
                downPath = "file://" + GetDataPath();
            }
            m_WWWDataPath = downPath;
            return downPath;
        }
        private static string m_DataPath = null;
        public static string GetDataPath()
        {
            if (m_DataPath != null)
            {
                return m_DataPath;
            }
            string downPath = null;
#if UNITY_EDITOR
            downPath = GetBuildStreamingAssetsPath() + GetRuntimePlatformName() + "/";
#else       
            //android平台自带了jar:file://
            downPath = Application.streamingAssetsPath + "/" + LCL.MonoTool.GetRuntimePlatformName() + "/";
#endif
            m_DataPath = downPath;
            return downPath;
        }
        private static string m_PersistentPath = null;
        public static string GetPersistentPath()
        {
            if(m_PersistentPath != null)
            {
                return m_PersistentPath;
            }
            string persistentPath = "";
            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                persistentPath = Application.persistentDataPath + "/"+ GetRuntimePlatformName() +"/";
            }
            else
            {
                persistentPath = Application.persistentDataPath + "/" + GetRuntimePlatformName() + "/";
            }
            m_PersistentPath = persistentPath;
            return persistentPath;
        }
        public static void PrintPathInfo()
        {
            Debug.Log("Application.persistentDataPath:" + Application.persistentDataPath);
            Debug.Log("Application.dataPath:" + Application.dataPath);
            Debug.Log("Application.streamingAssetsPath:" + Application.streamingAssetsPath);
            Debug.Log("Application.temporaryCachePath:" + Application.temporaryCachePath);
            Debug.Log("Application.consoleLogPath:" + Application.consoleLogPath);
        }
        public static void SetLayer(GameObject gameObjectRoot, bool withChildren, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            gameObjectRoot.layer = layer;
            if(withChildren)
            {
                var children = gameObjectRoot.GetComponentsInChildren<Transform>(true);
                int count = children.Length;
                for(int i=0;i<count;++i)
                {
                    children[i].gameObject.layer = layer;
                }
            }
        }
        private static string m_lcl_www_lcl_flag = "lcl_www_lcl";
        public static string GetBackDownloadFlag()
        {
            return m_lcl_www_lcl_flag;
        }
        private static string m_BackDownloadName = "downloadfilelist.json";
        public static string GetBackDownloadFileName()
        {
            return m_BackDownloadName;
        }

        private static string m_AssetbundleSuffix = ".jpg";
        public static string GetAssetbundleSuffix()
        {
            return m_AssetbundleSuffix;
        }
        private static string m_BuildPath = null;
        public static string GetBuildPath()
        {
            if(m_BuildPath != null)
            {
                return m_BuildPath;
            }
            m_BuildPath = Application.dataPath + "/../../build/";
            return m_BuildPath;
        }
        private static string m_BuildStreamingAssetsPath = null;
        public static string GetBuildStreamingAssetsPath()
        {
            if(m_BuildStreamingAssetsPath != null)
            {
                return m_BuildStreamingAssetsPath;
            }
            m_BuildStreamingAssetsPath = GetBuildPath() + "StreamingAssets/";
            return m_BuildStreamingAssetsPath;
        }
        private static string m_DevelopTablePath = null;
        public static string GetDevelopTablePath()
        {
            if (m_DevelopTablePath != null)
            {
                return m_DevelopTablePath;
            }
            m_DevelopTablePath = Application.dataPath + "/../../public/Tables/";
            return m_DevelopTablePath;
        }
    }
}
