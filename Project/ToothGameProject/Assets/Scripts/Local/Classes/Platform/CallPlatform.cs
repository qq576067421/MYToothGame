using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

namespace LCL
{
    public static class CallPlatform
    {
        public static string callFunc(string methodName, string strData)
        {
            string strResult = "";
#if UNITY_ANDROID
            if (Application.platform == RuntimePlatform.Android)
            {
                //调用AndroidJavaObject，生成一个Android端com.chunlinge.test.AndroidClass类实例
                try
                {
                    AndroidJavaObject jo = new AndroidJavaObject("com.chunlinge.PlatformBridge");
                    if (jo != null)
                    {
                        //调用该类的一个方法
                        if (strData == null)
                        {
                            strData = "";
                        }
                        strResult = jo.CallStatic<string>(methodName, strData);
                        jo = null;
                    }
                    else
                    {
                        Debug.LogWarning("没有找到平台代码");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("没有找到com.chunlinge.PlatformBridge，注意可能是android平台代码没有，但是这几乎不影响游戏，测试期间不会用到该代码" + e.StackTrace);
                }
            }
            else
            {
                strResult = LWindowsClass.CallPlatform(methodName, strData);
            }
#elif UNITY_IPHONE
            try
            {
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    strResult = LIOSClass.CallPlatform(methodName, strData);
                }
                else
                {
                    strResult = LWindowsClass.CallPlatform(methodName, strData);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message + e.StackTrace);
            }

#else
        strResult = LWindowsClass.CallPlatform(methodName, strData);
#endif


            if (strResult == null)
            {
                strResult = "";
            }
            return strResult;
        }
    }
}