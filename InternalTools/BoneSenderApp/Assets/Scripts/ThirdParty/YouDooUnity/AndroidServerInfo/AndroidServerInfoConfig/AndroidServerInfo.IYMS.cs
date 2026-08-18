/*
作者：Ting
创建时间：2025.10.07
修改时间：2026.04.14
描述：AndroidServerInfo 桌面服务（IYMS）相关：账户、排行榜、存档、购买、上报 partial class 模块
修改说明：HandleIYMSgameServerCallback 改为 Handle+On 二层模式，基类负责 JSON 解析和路由，
         子类只需覆写关心的 On 方法。事件定义从 AndroidServerInfoDemo 上移到此处。
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using static YouDooSDKConstants;

public partial class AndroidServerInfo
{
    #region 桌面服务（IYMS）相关的Android接口
    /// <summary>
    /// 绑定与服务器（通过桌面的server）数据交互的逻辑。
    /// </summary>
    public void BindIYmsGameService()
    {
        _pluginInstance.Call("bindIYmsGameService");
    }
    /// <summary>
    /// 解除绑定与服务器（通过桌面的server）数据交互的逻辑。
    /// </summary>
    public void UnBindIYmsGameService()
    {
        _pluginInstance.Call("unBindIYmsGameService");
    }

    /// <summary>
    /// 桌面服务系统是否已经连接
    /// SDK 4.0 开始支持
    /// </summary>
    public bool IsYMSConnected()
    {
        return _pluginInstance.Call<bool>("isYMSConnected");
    }

    public AccountInfo GetAccountInfo()
    {
        string temp = _pluginInstance.Call<string>("getAccountInfo");
        if (temp != null)
        {
            AccountInfo accountInfo = JsonUtility.FromJson<AccountInfo>(temp);
            return accountInfo;
        }
        return null;
    }

    /// <summary>
    /// 刷新账户信息
    /// </summary>
    public void RefreshAccountInfo()
    {
        _pluginInstance.Call("refreshAccountInfo");
    }

    /// <summary>
    /// 刷新并且返回当前的账户信息。
    /// 在 getAccountInfo 的基础上使用的
    /// </summary>
    public string RefreshAndGetAccountInfo()
    {
        return _pluginInstance.Call<string>("refreshAndGetAccountInfo");
    }

    /// <summary>
    /// 看有多少元气豆
    /// </summary>
    /// <returns></returns>
    public int GetYuanQiDou()
    {
        return _pluginInstance.Call<int>("getYuanQiDou");
    }

    /// <summary>
    /// 判断是不是vip用户 SDK 4.2
    /// </summary>
    /// <returns></returns>
    public bool IsVip()
    {
        return _pluginInstance.Call<bool>("isVip");
    }

    /// <summary>
    /// 游戏内查询产品 SDK 4.1
    /// </summary>
    public void QueryGameProducts()
    {
        _pluginInstance.Call("queryGameProducts");
    }

    /// <summary>
    /// 游戏内购买产品 SDK 4.1
    /// </summary>
    /// <param name="productId">需要购买的产品Id</param>
    /// <param name="price">需要购买的产品价格</param>
    public void PurchaseGameProducts(int productId, double price)
    {
        _pluginInstance.Call("purchaseGameProducts", productId, price);
    }

    /// <summary>
    /// 查询已购买数据 SDK 4.1
    /// 没有网就读取本地的。
    /// 都是异步的回调。和游戏业务相关，具体数据需要和策划确认后与服务器进行定义
    /// </summary>
    public void QueryAppPayItem()
    {
        _pluginInstance.Call("queryAppPayItem");
    }

    /// <summary>
    /// 打开YMS元气豆充值页面 SDK 4.2
    /// </summary>
    /// <param name="shortfall">元气豆差额</param>
    public void OpenYmsYbPayment(long shortfall)
    {
        _pluginInstance.Call("openYmsYbPayment", shortfall);
    }

    /// <summary>
    /// 打开YMS VIP充值页面 SDK 4.2.1
    /// </summary>
    public void OpenYmsVIPPayment()
    {
        _pluginInstance.Call("openYmsVIPPayment");
    }

    /// <summary>
    /// 打开YMS CDKEY兑换页面 SDK 4.2.1
    /// </summary>
    public void OpenYmsCDKEYPayment()
    {
        _pluginInstance.Call("openYmsCDKEYPayment");
    }

    /// <summary>
    /// 桌面服务系统 获得存档记录
    ///  2026.02.09增加 获取 数据存储的入口
    /// 1.每个游戏需要 获取 存储的数据 的时候需要调用这个函数。
    /// 2.异步的形式回调 。
    /// 3.注意:不要频繁的调用。通常情况下是游戏启动的时候调用一次就够了
    /// 4.注意:存储的数据与用户的账户一一对应的。
    /// 5.数据的合并/更新，是进入游戏前,由系统 根据数据的上报时间进行判别处理。理论上进入到游戏后 拿到的数据都是 合规的最新数据。
    /// SDK 4.0  开始支持
    /// </summary>
    /// <returns></returns>
    public void GetGameDataRecord()
    {
        _pluginInstance.Call("getGameDataRecord");
    }

    /// <summary>
    /// 桌面服务系统 保存存档
    /// 2026.02.09增加 设置 数据存储的入口
    /// 1.每个游戏需要 保存 存储的数据 的时候 调用此函数。
    /// 2.异步的形式回调 成功失败 。
    /// 3.注意:不要频繁的调用。正常的做法是：游戏自身有一个数据的对象（通常是Json格式的比较方便处理），针对这个对象进数据操作，适当的时候，调用此函数保存数据。
    /// 4.注意:存储的数据是与用户的账户一一对应的。调用者 不必处理这个细节。
    /// 5.保存的数据由系统  统一管理,统一上报给服务器。调用者 不必处理这个细节。
    /// 6.系统不会对保存的数据进行分解，仅提供了保存和获取的功能。
    /// @param saveString 需要保存的数据。 通常用Json转成String
    /// @since 4.0
    /// </summary>
    public void SetGameDataRecord(string saveString)
    {
        _pluginInstance.Call("setGameDataRecord", saveString);
    }

    /// <summary>
    /// 上报 排行榜的分数
    /// @since 4.0
    /// </summary>
    /// <param name="rankkey">每个游戏自己约定的key， 可以咨询商务定义好。</param>
    /// <param name="score">分数</param>
    /// <param name="isHighScoreList">是否高分榜单</param>
    public void UpdateUserRankScore(string rankkey, string jsonString, bool isHighScoreList)
    {
        // var scoreList = new UpdateUserRankScoreList
        // {
        //     items = new UpdateUserRankScore[]
        //    {
        //         new UpdateUserRankScore { userId = userId, score = score }
        //    }
        // };
        // // 序列化为JSON字符串
        // string jsonString = JsonConvert.SerializeObject(scoreList);
        // Debug.Log($"发送的JSON: {jsonString}");
        _pluginInstance.Call("updateUserRankScore", rankkey, jsonString, isHighScoreList);
    }

    /// <summary>
    /// 获取排行的信息。
    /// @since 4.0
    /// </summary>
    /// <param name="rankkey">每个游戏自己约定的key， 可以咨询商务定义好。</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="isHighScoreList"是否高分榜单></param>
    public void GetLeaderboardScores(string rankkey, int page, int pageSize, bool isHighScoreList)
    {
        _pluginInstance.Call("getLeaderboardScores", rankkey, page, pageSize, isHighScoreList);
    }

    /// <summary>
    /// 获取排行榜的历史资料
    /// @since 4.0
    /// </summary>
    /// <param name="rankkey">每个游戏自己约定的key， 可以咨询商务定义好。</param>
    /// <param name="isHighScoreList"> 是否高分榜单 </param>
    /// <param name="businessType">   业务类型（1=最低分查询，2=查询用户自己） </param>
    public void GetUserRankScore(string rankkey, long[] userIds, bool isHighScoreList, GetRankHistoryScoreBusinessType businessType)
    {
        _pluginInstance.Call("getUserRankScore", rankkey, userIds, isHighScoreList, (int)businessType);
    }


    /// <summary>
    /// 上报信息
    /// </summary>
    /// <param name="type">当前只有Event  对应的是：REPORT_TYPE_EVENT</param>
    /// <param name="businessType">当前只有 REPORT_BUSINESSTYPE_GAME_CALORIE   REPORT_BUSINESSTYPE_USER_GAME_DURATION  REPORT_BUSINESSTYPE_GAME_ENERGY</param>
    /// <param name="value"> 当GAME_CALORIE的时候， value应该值增加的卡路里</param>
    /// <param name="source">具体业务具体分析：  找策划。 eg：游戏的战斗场景需要上报的数据  </param>
    /// <param name="userId">与用户无关的传0</param>
    public void SendReportEvent(string type, string businessType, string value, string source, long userId)
    {
        _pluginInstance.Call("sendReportEvent", type, businessType, value, source, userId);
    }

    /// <summary>
    /// 初始化游戏上报信息
    /// </summary>
    /// <param name="appName">游戏名字</param>
    /// <param name="appVersion">游戏版本号</param>
    /// <param name="appVersionCode">版本号有关</param>
    /// SDK4.2.2 已经作废。
    public void InitReportGameInfo(string appName, string appVersion, string appVersionCode)
    {
        
        Debug.Log( "SDK4.2.2 之后作废 " );
        //_pluginInstance.Call("initReportGameInfo", appName, appVersion, appVersionCode);
    }


    /// <summary>
    /// 获得系统的 语言。通常IYMS 服务绑定之后才能调用。
    /// </summary>
    public string GetCurrentSysLanguage()
    {
        return _pluginInstance.Call<string>("getCurrentSysLanguage");
    }

    /// <summary>
    /// 获取设备序列号
    ///  SDK 4.1 开始支持 盒子的唯一标识
    /// </summary>
    /// <returns>设备序列号，如果服务未绑定则返回 "" </returns>
    public string GetSerialNumber()
    {
        return _pluginInstance.Call<string>("getSerialNumber");
    }

    /// <summary>
    /// 获取固件版本
    /// </summary>
    /// <returns>固件版本字符串，如果服务未绑定则返回 ""  </returns>
    public string GetFirmwareVersion()
    {
        return _pluginInstance.Call<string>("getFirmwareVersion");
    }

    /// <summary>
    /// SDK4.1 
    /// wifi是否正常的。
    /// </summary>
    public bool IsWifiOk()
    {
        return _pluginInstance.Call<bool>("isWifiOk");
    }

    public string GetAvatarPath(string fileName)
    {
        Debug.Log($"HeadImage 获取到图片 ：  " + fileName);
        return _pluginInstance.Call<string>("getAvatarPath", fileName);
    }

    /// <summary>
    /// 批量查询签名URL SDK 4.2
    /// </summary>
    /// <param name="storageKeys">需要获取签名URL的存储Key列表</param>
    public void QuerySignUrl(string[] storageKeys)
    {
        _pluginInstance.Call("querySignUrl", storageKeys);
    }


    /// <summary>
    /// 显示通用的提示 SDK 4.2.2
    /// </summary>
    /// <param name="message">显示通用的提示</param>
    public void ShowToast(string message)
    {
        _pluginInstance.Call("showToast", message);
    }


    /// <summary>
    ///获取用户的信息，通常是排行榜拉取到其他人的userId后，要获取一下这些人的信息。
    /// @since 4.2.2
    /// </summary>
    /// <param name="userIds">获取其他用户的信息</param>
    public void GetRoleInfo(long[] userIds)
    {
        _pluginInstance.Call("getRoleInfo", userIds);
    }


    #endregion

    #region 桌面服务（IYMS）消息路由
    /// <summary>
    /// 与桌面打交道的数据统计的服务。
    /// 基类负责 JSON 解析和路由，子类通过覆写 On 方法来定制行为。
    /// </summary>
    private void HandleIYMSgameServerCallback(string messageJson)
    {
        Debug.Log($"[IYMS] HandleIYMSgameServerCallback: {messageJson}");
        IYMSgameServerNotifyInfo notifyInfo = JsonUtility.FromJson<IYMSgameServerNotifyInfo>(messageJson);
        if (notifyInfo == null) return;

        YouDooNotifyIYMSgameServerType notifyType = (YouDooNotifyIYMSgameServerType)notifyInfo.notifyIYMSgameServerType;
        switch (notifyType)
        {
            // ─── 服务生命周期 ───
            case YouDooNotifyIYMSgameServerType.IYMS_SERVICE_CONNECTED:
                Debug.Log("[IYMS] 服务已连接");
                OnIYMSServiceConnected(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_SERVICE_DISCONNECTED:
                Debug.Log("[IYMS] 服务已断开");
                OnIYMSServiceDisconnected(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_SERVICE_BINDING:
                Debug.Log("[IYMS] 服务正在绑定中");
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_SERVICE_BIND_FAILED:
                Debug.Log("[IYMS] 服务绑定失败");
                OnIYMSServiceBindFailed(notifyInfo.message);
                break;

            // ─── 账户变更 ───
            case YouDooNotifyIYMSgameServerType.IYMS_ACCOUNT_CHANGE:
                Debug.Log("[IYMS] 账户变更");
                OnIYMSAccountChanged(notifyInfo.message);
                break;
            // ─── 存档 ───
            case YouDooNotifyIYMSgameServerType.IYMS_GET_GAME_SAVE_CALLBACK_SUCCESS:
                Debug.Log("[IYMS] 获取存档信息成功");
                OnIYMSGetGameSaveSuccess(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_GET_GAME_SAVE_CALLBACK_FAILURE:
                Debug.Log("[IYMS] 获取存档信息失败");
                OnIYMSGetGameSaveFailure(JsonUtility.FromJson<IYMSgameGetSaveFailureNotifyInfo>(notifyInfo.message));
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_SET_GAME_SAVE_CALLBACK_SUCCESS:
                Debug.Log("[IYMS] 保存存档信息成功");
                OnIYMSSetGameSaveSuccess(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_SET_GAME_SAVE_CALLBACK_FAILURE:
                Debug.Log("[IYMS] 保存存档信息失败");
                OnIYMSSetGameSaveFailure(JsonUtility.FromJson<IYMSgameSetSaveFailureNotifyInfo>(notifyInfo.message));
                break;
            // ─── 排行榜 ───
            case YouDooNotifyIYMSgameServerType.IYMS_GET_RANK_SCORE_SUCCESS:
                Debug.Log("[IYMS] 排行榜 获取排行信息成功 "+notifyInfo.message );
                OnIYMSGetRankScoreSuccess(JsonUtility.FromJson<IYMSRankScoreInfoList>(notifyInfo.message));
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_GET_RANK_SCORE_FAILURE:
                Debug.Log("[IYMS] 排行榜 获取排行信息失败");
                OnIYMSGetRankScoreFailure(JsonUtility.FromJson<IYMSgameGetRankScoreFailureNotifyInfo>(notifyInfo.message));
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_UPDATA_RANK_SCORE_SUCCESS:
                Debug.Log("[IYMS]  更新排行信息成功");
                OnIYMSUpdateRankScoreSuccess(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_UPDATA_RANK_SCORE_FAILURE:
                Debug.Log("[IYMS] 更新排行信息失败");
                OnIYMSUpdateRankScoreFailure(JsonUtility.FromJson<IYMSgameUpdataRankScoreFailureNotifyInfo>(notifyInfo.message));
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_GET_RANK_HISTORY_SCORE_SUCCESS:
                Debug.Log("[IYMS] 排行榜 获取历史排行信息成功" +  notifyInfo.message );
                OnIYMSGetRankHistoryScoreSuccess(JsonUtility.FromJson<IYMSgameGetRankHistoryScoreSuccessNotifyInfo>(notifyInfo.message));
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_GET_RANK_HISTORY_SCORE_FAILURE:
                Debug.Log("[IYMS] 排行榜 获取历史排行信息失败" +  notifyInfo.message);
                OnIYMSGetRankHistoryScoreFailure(JsonUtility.FromJson<IYMSgameGetRankHistoryScoreFailureNotifyInfo>(notifyInfo.message));
                break;

            // ─── 角色选择 ───
            case YouDooNotifyIYMSgameServerType.IYMS_SELECTROLE_ONSTATECHANGED:
                OnIYMSSelectRoleStateChanged(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_SELECTROLE_ONSUCCESS:
                OnIYMSSelectRoleSuccess(notifyInfo.message, messageJson);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_SELECTROLE_ONFAILURE:
                OnIYMSSelectRoleFailure(JsonUtility.FromJson<IYMSselectRoleOnFailureNotifyInfo>(notifyInfo.message));
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_SELECTROLE_ONCANCELLED:
                Debug.Log("[IYMS] 选角色已取消");
                break;

            // ─── 购买查询 ───
            case YouDooNotifyIYMSgameServerType.IYMS_QUERY_APP_PAY_ITEM_SUCCESS:
                Debug.Log("[IYMS] 查询已购买数据成功");
                OnIYMSQueryAppPayItemSuccess(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_QUERY_APP_PAY_ITEM_FAILURE:
                Debug.Log("[IYMS] 查询已购买数据失败");
                OnIYMSQueryAppPayItemFailure(notifyInfo.message);
                break;

            // ─── 游戏产品查询/购买 ───
            case YouDooNotifyIYMSgameServerType.IYMS_QUERY_GAME_PRODUCTS_SUCCESS:
                Debug.Log($"[IYMS] 查询游戏产品成功: {notifyInfo.message}");
                {
                    var productList = new List<GameProductInfo>();
                    try
                    {
                        string wrapped = "{\"items\":" + notifyInfo.message + "}";
                        GameProductInfoList wrappedList = JsonUtility.FromJson<GameProductInfoList>(wrapped);
                        if (wrappedList?.items != null)
                        {
                            productList.AddRange(wrappedList.items);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[IYMS] 解析游戏产品列表失败: {ex.Message}");
                    }
                    OnIYMSQueryGameProductsSuccess(productList);
                }
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_QUERY_GAME_PRODUCTS_FAILURE:
                Debug.LogError($"[IYMS] 查询游戏产品失败: {notifyInfo.message}");
                {
                    var failInfo = JsonUtility.FromJson<IYMSqueryGameProductsFailureNotifyInfo>(notifyInfo.message);
                    OnIYMSQueryGameProductsFailure(failInfo);
                }
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_PURCHASE_GAME_PRODUCTS_SUCCESS:
                Debug.Log($"[IYMS] 购买游戏产品成功: {notifyInfo.message}");
                OnIYMSPurchaseGameProductsSuccess(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_PURCHASE_GAME_PRODUCTS_FAILURE:
                Debug.LogError($"[IYMS] 购买游戏产品失败: {notifyInfo.message}");
                {
                    var failInfo = JsonUtility.FromJson<IYMSpurchaseGameProductsFailureNotifyInfo>(notifyInfo.message);
                    OnIYMSPurchaseGameProductsFailure(failInfo);
                }
                break;

            // ─── 充值 ───
            case YouDooNotifyIYMSgameServerType.IYMS_PAYMENT_SUCCESS:
                Debug.Log($"[IYMS] 系统游戏充值成功: {notifyInfo.message}");
                OnIYMSPaymentSuccess(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_PAYMENT_FAILURE:
                Debug.LogError("[IYMS] 系统游戏充值失败");
                OnIYMSPaymentFailure(notifyInfo.message);
                break;

            // ─── 签名URL ───
            case YouDooNotifyIYMSgameServerType.IYMS_QUERY_SIGN_URL_SUCCESS:
                Debug.Log($"[IYMS] 批量查询签名URL成功: {notifyInfo.message}");
                OnIYMSQuerySignUrlSuccess(notifyInfo.message, messageJson);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_QUERY_SIGN_URL_FAILURE:
                Debug.Log($"[IYMS] 批量查询签名URL失败: {notifyInfo.message}");
                OnIYMSQuerySignUrlFailure(notifyInfo.message);
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_GET_ROLE_INFO_SUCCESS:
                Debug.Log($"[IYMS] 获取人物信息 :   {notifyInfo.message}");
                RoleInfoList roleInfoList = JsonUtility.FromJson<RoleInfoList>(notifyInfo.message);
                OnIYMSGetRoleInfoSuccess(   roleInfoList );
                break;
            case YouDooNotifyIYMSgameServerType.IYMS_GET_ROLE_INFO_FAILURE:
                Debug.Log($"[IYMS] 获取角色信息失败: {notifyInfo.message}");
                OnIYMSGetRoleInfoFailure(notifyInfo.message);
                break;
            default:
                Debug.LogWarning($"[IYMS] 未知通知类型: {notifyType}");
                break;
        }
    }
    #endregion

    #region IYMS On 虚方法（子类覆写用）
    // ─── 服务生命周期 ───
    protected virtual void OnIYMSServiceConnected(string message) { }
    protected virtual void OnIYMSServiceDisconnected(string message) { }
    protected virtual void OnIYMSServiceBindFailed(string message) { }

    // ─── 账户 ───
    protected virtual void OnIYMSAccountChanged(string message) { }

    // ─── 存档 ───
    protected virtual void OnIYMSGetGameSaveSuccess(string message) { }
    protected virtual void OnIYMSGetGameSaveFailure(IYMSgameGetSaveFailureNotifyInfo info) { }
    protected virtual void OnIYMSSetGameSaveSuccess(string message) { }
    protected virtual void OnIYMSSetGameSaveFailure(IYMSgameSetSaveFailureNotifyInfo info) { }

    // ─── 排行榜 ───
    protected virtual void OnIYMSGetRankScoreSuccess(IYMSRankScoreInfoList info) { }
    protected virtual void OnIYMSGetRankScoreFailure(IYMSgameGetRankScoreFailureNotifyInfo info) { }
    protected virtual void OnIYMSUpdateRankScoreSuccess(string message) { }
    protected virtual void OnIYMSUpdateRankScoreFailure(IYMSgameUpdataRankScoreFailureNotifyInfo info) { }
    protected virtual void OnIYMSGetRankHistoryScoreSuccess(IYMSgameGetRankHistoryScoreSuccessNotifyInfo info) { }
    protected virtual void OnIYMSGetRankHistoryScoreFailure(IYMSgameGetRankHistoryScoreFailureNotifyInfo info) { }

    // ─── 角色选择 ───
    /// <param name="message">notifyInfo.message 字段</param>
    protected virtual void OnIYMSSelectRoleStateChanged(string message) { }
    /// <param name="message">notifyInfo.message 字段</param>
    /// <param name="rawMessageJson">完整的原始 JSON（用于 IYMSgameServerRoleNotifyInfo 解析）</param>
    protected virtual void OnIYMSSelectRoleSuccess(string message, string rawMessageJson) { }
    protected virtual void OnIYMSSelectRoleFailure(IYMSselectRoleOnFailureNotifyInfo info) { }

    // ─── 购买查询 ───
    protected virtual void OnIYMSQueryAppPayItemSuccess(string message) { }
    protected virtual void OnIYMSQueryAppPayItemFailure(string message) { }

    // ─── 游戏产品 ───
    protected virtual void OnIYMSQueryGameProductsSuccess(List<GameProductInfo> products) { }
    protected virtual void OnIYMSQueryGameProductsFailure(IYMSqueryGameProductsFailureNotifyInfo info) { }
    protected virtual void OnIYMSPurchaseGameProductsSuccess(string message) { }
    protected virtual void OnIYMSPurchaseGameProductsFailure(IYMSpurchaseGameProductsFailureNotifyInfo info) { }

    // ─── 充值 ───
    protected virtual void OnIYMSPaymentSuccess(string message) { }
    protected virtual void OnIYMSPaymentFailure(string message) { }

    // ─── 签名URL ───
    /// <param name="message">notifyInfo.message 字段</param>
    /// <param name="rawMessageJson">完整的原始 JSON（message 字段可能为 JSON 对象需从原始 JSON 提取）</param>
    protected virtual void OnIYMSQuerySignUrlSuccess(string message, string rawMessageJson) { }
    protected virtual void OnIYMSQuerySignUrlFailure(string message) { }

    // ─── 获取角色信息 ───
    protected virtual void OnIYMSGetRoleInfoSuccess( RoleInfoList roleInfoList ) { }
    protected virtual void OnIYMSGetRoleInfoFailure(string message) { }
    #endregion


    #region 辅助开发/测试
    /// <summary>
    /// 录音
    /// </summary>
    public bool TestStartVideoRecording(string outPath, int w, int h, long durationTimeMs, bool recordAfterDraw)
    {
        return _pluginInstance.Call<bool>("testStartVideoRecording", outPath, w, h, durationTimeMs, recordAfterDraw);
    }

    public bool TestStopVideoRecording()
    {
        return _pluginInstance.Call<bool>("testStopVideoRecording"); 
    }

    public void TestShowGameServiceView()
    {
        _pluginInstance.Call("testShowGameServiceView");
    }

    public void TestStartRecorderInfo()
    {
        _pluginInstance.Call("testStartRecorderInfo");
    }

    /// <summary>
    /// 测试获取公共目录的路径
    /// </summary>
    public string TestGetExternalStoragePublicDirectory()
    {
        return _pluginInstance.Call<string>("testGetExternalStoragePublicDirectory");
    }

    /// <summary>
    /// 测试的代码。权限
    /// </summary>
    public void TestAequestAllPermissions()
    {
        _pluginInstance.Call("requestAllPermissions");
    }
    #endregion
}
