/*
作者：Ting
创建时间：2026.05.07 
描述：AndroidServerInfo 数据统计 的模块
*/
using System;
using UnityEngine;
using static YouDooSDKConstants;

public partial class AndroidServerInfo
{
     #region 数据统计相关的Android接口
     /// <summary>
     /// SDK 4.2.2
     /// 上报一个事件。不带参数的
     /// 2026.05.12验证
     /// </summary>
     public void DataStatisticsTrack(string eventName)
     {
          _pluginInstance.Call("dataStatisticsTrack", eventName);
     }

     /// <summary>
     /// SDK 4.2.2
     /// 上报一个事件。带参数的。
     /// 2026.05.12验证
     /// </summary>
     public void DataStatisticsTrackWithProperties(string eventName, string jsonProperties)
     {
          _pluginInstance.Call("dataStatisticsTrackWithProperties", eventName, jsonProperties);
     }

     /// <summary>
     /// SDK 4.2.2
     /// 上报一个事件。带参数、时间和时区
     /// </summary>
     /// <param name="eventName">事件名称</param>
     /// <param name="jsonProperties">事件属性（JSON字符串）</param>
     /// <param name="dateStr">时间字符串（格式：yyyy-MM-dd HH:mm:ss.SSS）</param>
     /// <param name="timeZone">时区（如 "Asia/Shanghai", "GMT+8"）</param>
     /// 2026.05.12验证
     public void DataStatisticsTrackWithPropertiesWithDateAndTimeZone(string eventName, string jsonProperties, string dateStr, string timeZone)
     {
          _pluginInstance.Call("dataStatisticsTrackWithPropertiesWithDateAndTimeZone", eventName, jsonProperties, dateStr, timeZone);
     }

     /// <summary>
     /// SDK 4.2.2
     /// 强制上报缓存的事件数据
     /// 2026.05.12验证
     /// </summary>
     public void DataStatisticsFlush()
     {
          _pluginInstance.Call("dataStatisticsFlush");
     }

     /// <summary>
     /// SDK 4.2.2
     /// 设置用户属性  使用概率应该非常低。
     /// 对于一般的用户属性，您可以调用 userSet 来进行设置，
     /// 使用该接口上传的属性将会覆盖原有的属性值；如果之前不存在该用户属性，则会新建该用户属性，
     /// 2026.05.12验证
     /// </summary>
     /// <param name="jsonUserInfo">用户属性（JSON字符串）</param>
     public void DataStatisticsUserSet(string jsonUserInfo)
     {
          _pluginInstance.Call("dataStatisticsUserSet", jsonUserInfo);
     }

     /// <summary>
     /// SDK 4.2.2
     /// 默认就是正式环境的。
     /// 开发期间：调用这个函数，表示数据埋点是测试的环境。
     /// 这个函数一定是在： 绑定各种服务前调用。
     /// 2026.05.12 验证
     /// </summary>
     public void DataStatisticsSetIsDebug()
     {
          _pluginInstance.Call("dataStatisticsSetIsDebug");
     }
     
     #endregion
}
