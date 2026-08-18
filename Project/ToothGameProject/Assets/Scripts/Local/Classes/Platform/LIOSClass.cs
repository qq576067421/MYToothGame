using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LCL
{

    public class LIOSClass
    {
#if UNITY_IPHONE
        [DllImport("__public")]
        private static extern string LIOSClass_CallPlatform(string strData);
#endif
        public static string CallPlatform(string methodName, string strData)
        {
            if (methodName == "DiskSize")
            {
                return DiskSize();
            }
            else if (methodName == "UnityCallPlatform")
            {
#if UNITY_IPHONE
                return LIOSClass_CallPlatform(strData);
#endif
            }
            return "";

        }

        private static string DiskSize()
        {
            //剩余200M
            long size = 1024 * 1024 * 500;
            return size.ToString();
        }
    }

    }