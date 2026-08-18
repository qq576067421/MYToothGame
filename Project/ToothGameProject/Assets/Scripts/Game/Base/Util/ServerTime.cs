using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDll
{
    public class ServerTime
    {
        private static double m_ServerTime;
        //毫秒
        public static void SetServerTime(long time_mm)
        {
            m_ServerTime = time_mm;
        }

        //毫秒
        public static long GetServerTime()
        {
            return (long)m_ServerTime;
        }


        public static void UpdateServerTime()
        {
            if(m_ServerTime == 0)
            {
                return;
            }
            m_ServerTime += UnityEngine.Time.deltaTime * 1000;
        }
    }
}
