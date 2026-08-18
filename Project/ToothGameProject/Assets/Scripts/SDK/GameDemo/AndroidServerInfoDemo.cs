using System;
using System.Collections.Generic;
using UnityEngine;
using YouDooSDK.UI;
using static YouDooSDKConstants;

public class AndroidServerInfoDemo : AndroidServerInfo
{
    private const string LogTag = "[AndroidServerInfoDemo]";

    //true表示服务已经取消。
    private bool isInitFrameInfo = false;

    [SerializeField] private ModelSelectDemo modelSelectDemo;

    /// <summary>
    /// 处理NFC用到的Demo
    /// </summary>
    [SerializeField] NFCSceneDemo nfcSceneDemo;

    //我的demo是需要用到蓝牙的,而且是 只用书手柄的demo 
    public BluetoothOnlyUseMajorControllerDemo bluetoothOnlyUseMajorController = new BluetoothOnlyUseMajorControllerDemo();


    public static event Action<string> OnRecordtips;
    public static event Action<DeviceStatusInfo> OnFindBlueTooth;
    // 人脸识别事件：传递 personId 和 userId
    public static event Action<int, long, bool> OnFaceRecognizedUser;
    public static event Action OnManualRoleSelectionFailed;
    public static event Action OnManualRoleSelectionCancelled;

    /// <summary>查询已购买数据成功事件，传递回调message</summary>
    public static event Action<string> OnQueryAppPayItemSuccess;

    /// <summary>查询已购买数据失败事件</summary>
    public static event Action<IYMSqueryAppPayItemFailureNotifyInfo> OnQueryAppPayItemFailure;

    /// <summary>查询游戏产品成功事件，传递产品列表</summary>
    public static event Action<List<GameProductInfo>> OnQueryGameProductsSuccess;
    /// <summary>查询游戏产品失败事件</summary>
    public static event Action<IYMSqueryGameProductsFailureNotifyInfo> OnQueryGameProductsFailure;
    /// <summary>购买游戏产品成功事件，传递回调message</summary>
    public static event Action<string> OnPurchaseGameProductsSuccess;
    /// <summary>购买游戏产品失败事件</summary>
    public static event Action<IYMSpurchaseGameProductsFailureNotifyInfo> OnPurchaseGameProductsFailure;

    /// <summary>测试录音提示：Demo 中触发 OnRecordtips 事件</summary>
    protected override void OnTestRecordTips(string message)
    {
        OnRecordtips?.Invoke(message);
    }

    static AndroidModeSelect _androidModeSelect;

    public static bool ShouldUseSdkRuntime()
    {
        return Application.platform == RuntimePlatform.Android;
    }

    protected override void OnAwake()
    {
        bool shouldUseSdkRuntime = ShouldUseSdkRuntime();
        Debug.Log($"{LogTag} OnAwake platform={Application.platform} shouldUseSdkRuntime={shouldUseSdkRuntime} isSDKMode={IsSDKMode}");
        if (shouldUseSdkRuntime && !IsSDKMode)
        {
            Debug.Log($"{LogTag} OnAwake 检测到 Android 运行时，切换为 SDK 输入模式");
            SetInputMode(InputMode.SDK);
        }

        base.OnAwake();
        Debug.Log($"{LogTag} OnAwake 完成 isSDKMode={IsSDKMode}");
    }

    public void SetModelSelectDemo(ModelSelectDemo modelSelectDemo)
    {
        this.modelSelectDemo = modelSelectDemo;
        Debug.Log($"{LogTag} SetModelSelectDemo modelSelectDemo={(modelSelectDemo != null ? modelSelectDemo.name : "null")}");
        TryInitializeFrameInfo();
    }

    public static AndroidModeSelect AndroidModeSelect { get => _androidModeSelect; set => _androidModeSelect = value; }

    protected override void InitRequiredServices()
    {
        Debug.Log($"{LogTag} InitRequiredServices 开始");
        //  DataStatisticsSetIsDebug(); //SDK4.2.2增加。在软件开发阶段，调用此函数，目的：在数据埋点平台能看到 debug的数据，方便调试。 正式发布之后，不要调用这个函数的。

        BindFrameInfoGameService(); //根据具体的业务需求：注册骨骼的数据,只需要绑定一次就行。   
        BindInputDeviceService();   //根据具体的业务需求：这个demo需要支持多个蓝牙设备，并区分按键输入来源的设备。因此需要在一次Input时获取输入设备的蓝牙地址，所以需要这个服务。
        InitScreenCaptureHandler(); //根据具体的业务需求：这个demo需要用到截图。因此需要初始化截图的功能。
        InitRecorderManager();//根据具体的业务需求： 这个demo需要用到录屏的功能。因此需要初始化录屏的功能。
        BindIYmsGameService();//需要上报游戏的数据就，从大厅获取到一些信息。因此要初始化。
        InitHardBluetoothManager();//根据业务的需求：这个demo需要用到蓝牙的功能。SDK 4.1开始只需要监听一次就行。
        SetHardWareRemoteControlConfig(true, true, false, false, true, (int)YouDooSDKConstants.FilterType.NONE);//根据业务的需求：这个demo需要用到蓝牙的功能。SDK 4.1开始只需要监听一次就行。
        Bluetooth = bluetoothOnlyUseMajorController;
        Debug.Log($"{LogTag} InitRequiredServices 完成");
    }

    protected override void OnApplicationPause(bool pauseStatus)
    {
        base.OnApplicationPause(pauseStatus);
        Debug.Log($"{LogTag} OnApplicationPause pauseStatus={pauseStatus} isFrameInfoServerIsConnet={isFrameInfoServerIsConnet} isInitFrameInfo={isInitFrameInfo}");
        if (pauseStatus)
        {
            Debug.Log($"{LogTag} 切到后台，重置帧信息初始化状态并注销回调");
            isInitFrameInfo = false;
            UnregisterFrameCallback();
        }
        else
        {
            Debug.Log($"{LogTag} 回到前台，尝试重新初始化帧信息");
            TryInitializeFrameInfo();
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    protected override void CleanupRequiredServices()
    {
        Debug.Log("Youdoo_ApplicationActivityLifecycleCallbacks  游戲推出的時候調用  绑定了什么服务 就要解绑什么服务 開始 ");
        if (_pluginInstance != null)
        {
            try
            {
                UnBindFrameInfoGameService();
                UnBindInputDeviceService();
                UnBindIYmsGameService();
                ExitHardBluetoothManager();
                _pluginInstance.Call("onDestroy");
                _pluginInstance = null;
                Debug.Log("AndroidServerInfo资源清理完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"资源清理失败: {e.Message}");
            }
        }
        Debug.Log("Youdoo_ApplicationActivityLifecycleCallbacks  游戲推出的時候調用  绑定了什么服务 就要解绑什么服务  結束");
    }


    /// <summary>
    /// 这个demo的业务是一打开就得有骨骼数据的显示的。
    /// </summary>
    private void InitShowFrameInfo()
    {
        Debug.Log($"{LogTag} InitShowFrameInfo enter connected={isFrameInfoServerIsConnet} initialized={isInitFrameInfo} hasModelSelectDemo={modelSelectDemo != null} hasModeConfig={AndroidModeSelect != null}");
        if (!isFrameInfoServerIsConnet)
        {
            Debug.LogWarning($"{LogTag} InitShowFrameInfo 终止，帧信息服务尚未连接");
            return;
        }

        if (isInitFrameInfo)
        {
            Debug.Log($"{LogTag} InitShowFrameInfo 跳过，帧信息已初始化");
            return;
        }

        if (!TryResolveModelSelectDemo())
        {
            Debug.LogWarning($"{LogTag} InitShowFrameInfo 终止，ModelSelectDemo 尚未准备好");
            return;
        }

        if (!TryLoadModelConfig())
        {
            Debug.LogWarning($"{LogTag} InitShowFrameInfo 终止，模型配置尚未准备好");
            return;
        }

        RegisterFrameCallback();//注册数据的回调，本质是抢占了摄像头，镜头数据的通道只流向我们这个游戏。
        modelSelectDemo.SetAllModelConfig(AndroidModeSelect);//这个是自己用的 demo
        isInitFrameInfo = true;
        int modelCount = AndroidModeSelect?.AllConfig?.modelList?.Length ?? 0;
        int cameraCount = AndroidModeSelect?.AllConfig?.camList?.Length ?? 0;
        Debug.Log($"{LogTag} InitShowFrameInfo 完成 modelSelectDemo={modelSelectDemo.name} modelCount={modelCount} cameraCount={cameraCount}");
    }

    /// <summary>
    /// 帧信息服务连接成功
    /// </summary>
    protected override void OnFrameInfoServiceConnected(string message)
    {
        Debug.Log($"{LogTag} OnFrameInfoServiceConnected message={message} hasModelSelectDemo={modelSelectDemo != null}");
        base.OnFrameInfoServiceConnected(message);
        TryLoadModelConfig();
        TryInitializeFrameInfo();
    }

    protected override void OnNFCServiceNotifyInfo(string messageJson)
    {
        if (nfcSceneDemo == null)
        {
            nfcSceneDemo = FindAnyObjectByType<NFCSceneDemo>();
        }
        // 如果需要，可以在这里立即执行一些初始化操作
        if (nfcSceneDemo != null)
        {
            nfcSceneDemo.HandleNFCServiceNotifyInfo(messageJson);
        }

    }

    /// <summary>
    /// 处理蓝牙通知信息
    /// </summary>
    protected override void OnBluetoothNotifyInfo(string messageJson)
    {
        try
        {
            bluetoothOnlyUseMajorController.HandleBluetoothNotify(messageJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"解析蓝牙通知失败: {e.Message}");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Debug.Log($"{LogTag} OnDestroy 已销毁");
    }

    private bool TryResolveModelSelectDemo()
    {
        if (modelSelectDemo != null)
        {
            Debug.Log($"{LogTag} TryResolveModelSelectDemo 已持有引用 name={modelSelectDemo.name} scene={modelSelectDemo.gameObject.scene.name}");
            return true;
        }

        Debug.Log($"{LogTag} TryResolveModelSelectDemo 开始查找场景对象");
        modelSelectDemo = FindAnyObjectByType<ModelSelectDemo>();
        if (modelSelectDemo == null)
        {
            Debug.LogWarning($"{LogTag} TryResolveModelSelectDemo 未找到 ModelSelectDemo");
            return false;
        }

        Debug.Log($"{LogTag} TryResolveModelSelectDemo 查找成功 name={modelSelectDemo.name} scene={modelSelectDemo.gameObject.scene.name}");
        return modelSelectDemo != null;
    }

    private bool TryLoadModelConfig()
    {
        if (AndroidModeSelect != null)
        {
            int modelCount = AndroidModeSelect?.AllConfig?.modelList?.Length ?? 0;
            int cameraCount = AndroidModeSelect?.AllConfig?.camList?.Length ?? 0;
            Debug.Log($"{LogTag} TryLoadModelConfig 跳过，已存在配置 modelCount={modelCount} cameraCount={cameraCount}");
            return true;
        }

        if (_pluginInstance == null)
        {
            Debug.LogWarning($"{LogTag} TryLoadModelConfig 失败，_pluginInstance 为空");
            return false;
        }

        string configList = GetGameServiceConfigAll();
        if (string.IsNullOrEmpty(configList))
        {
            Debug.LogWarning($"{LogTag} TryLoadModelConfig 失败，当前未获取到模型配置");
            return false;
        }

        AndroidModeSelect = new AndroidModeSelect();
        AndroidModeSelect.SetAllModelConfig(configList);
        int loadedModelCount = AndroidModeSelect?.AllConfig?.modelList?.Length ?? 0;
        int loadedCameraCount = AndroidModeSelect?.AllConfig?.camList?.Length ?? 0;
        Debug.Log($"{LogTag} TryLoadModelConfig 成功 configLength={configList.Length} modelCount={loadedModelCount} cameraCount={loadedCameraCount}");
        return true;
    }

    private void TryInitializeFrameInfo()
    {
        Debug.Log($"{LogTag} TryInitializeFrameInfo enter connected={isFrameInfoServerIsConnet} initialized={isInitFrameInfo} hasModelSelectDemo={modelSelectDemo != null} hasModeConfig={AndroidModeSelect != null}");
        if (!isFrameInfoServerIsConnet)
        {
            Debug.LogWarning($"{LogTag} TryInitializeFrameInfo 终止，帧信息服务尚未连接");
            return;
        }

        InitShowFrameInfo();
    }


    /// <summary>
    /// SDK4.1开始次方法作废。
    /// </summary>
    /// <param name="nfcDemo"></param>
    // protected override void OnUnbondedDeviceFound(DeviceStatusInfo deviceInfo)
    // {
    //     Debug.Log($"发现未绑定设备: {deviceInfo.name} ({deviceInfo.address})");
    //     // 在这里处理未绑定设备的逻辑
    //     // 例如：显示设备供用户选择绑定、存储设备信息等
    //     OnFindBlueTooth?.Invoke(deviceInfo);
    // }

    public void SetNFCSceneDemo(NFCSceneDemo nfcDemo)
    {
        nfcSceneDemo = nfcDemo;
    }


    #region IYMS On 方法覆写（Demo 特有逻辑）
    /// <summary>IYMS 服务连接成功：Demo 中执行测试逻辑</summary>
    protected override void OnIYMSServiceConnected(string message)
    {
        PlayerRoleManager.Instance.SetAccountInfo(GetAccountInfo());
        GetGameDataRecord();
        TestOnIYMSServiceConnected();
    }

    /// <summary>IYMS 服务断开：Demo 中显示 Tips</summary>
    protected override void OnIYMSServiceDisconnected(string message)
    {
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_stats_service_disconnected"));
    }

    /// <summary>IYMS 服务绑定失败：Demo 中显示 Tips</summary>
    protected override void OnIYMSServiceBindFailed(string message)
    {
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_stats_service_bind_failed"));
    }

    /// <summary>账户变更：Demo 中显示 Tips</summary>
    protected override void OnIYMSAccountChanged(string message)
    {
        PlayerRoleManager.Instance.SetAccountInfo(GetAccountInfo());
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_stats_account_changed"));
    }

    /// <summary>选角色成功：Demo 中触发人脸识别事件</summary>
    protected override void OnIYMSSelectRoleSuccess(string message, string rawMessageJson)
    {
        IYMSgameServerRoleNotifyInfo roleNotifyInfo = JsonUtility.FromJson<IYMSgameServerRoleNotifyInfo>(rawMessageJson);
        if (roleNotifyInfo != null && roleNotifyInfo.message != null)
        {
            Debug.Log($"选角成功。获取到 userId: {roleNotifyInfo.message.userId}");
            OnFaceRecognizedUser?.Invoke(0, roleNotifyInfo.message.userId, true);
        }
        else
        {
            Debug.LogError("选角数据解析失败。");
            OnManualRoleSelectionFailed?.Invoke();
        }
    }

    protected override void OnIYMSSelectRoleFailure(IYMSselectRoleOnFailureNotifyInfo info)
    {
        OnManualRoleSelectionFailed?.Invoke();
    }

    protected override void OnIYMSSelectRoleCancelled(string message)
    {
        OnManualRoleSelectionCancelled?.Invoke();
    }

    /// <summary>查询已购买数据成功：Demo 中显示 Tips</summary>
    protected override void OnIYMSQueryAppPayItemSuccess(string message)
    {
        OnQueryAppPayItemSuccess?.Invoke(message);
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_query_purchased_success"));
    }

    /// <summary>查询已购买数据失败：Demo 中显示 Tips</summary>
    protected override void OnIYMSQueryAppPayItemFailure(string message)
    {
        var failInfo = JsonUtility.FromJson<IYMSqueryAppPayItemFailureNotifyInfo>(message);
        OnQueryAppPayItemFailure?.Invoke(failInfo);
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_query_purchased_failed"));
    }

    /// <summary>查询游戏产品成功：Demo 中显示 Tips</summary>
    protected override void OnIYMSQueryGameProductsSuccess(List<GameProductInfo> products)
    {
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_query_product_success"));
        OnQueryGameProductsSuccess?.Invoke(products);
    }

    /// <summary>查询游戏产品失败：Demo 中显示 Tips</summary>
    protected override void OnIYMSQueryGameProductsFailure(IYMSqueryGameProductsFailureNotifyInfo info)
    {
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_query_product_failed"));
        OnQueryGameProductsFailure?.Invoke(info);
    }

    /// <summary>购买游戏产品成功：Demo 中显示 Tips</summary>
    protected override void OnIYMSPurchaseGameProductsSuccess(string message)
    {
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_purchase_product_success"));
        OnPurchaseGameProductsSuccess?.Invoke(message);
    }

    /// <summary>购买游戏产品失败：Demo 中显示 Tips</summary>
    protected override void OnIYMSPurchaseGameProductsFailure(IYMSpurchaseGameProductsFailureNotifyInfo info)
    {
        TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_purchase_product_failed"));
        OnPurchaseGameProductsFailure?.Invoke(info);
    }

    /// <summary>批量查询签名URL成功：Demo 中解析并打印签名URL</summary>
    protected override void OnIYMSQuerySignUrlSuccess(string message, string rawMessageJson)
    {
        string signUrlJsonStr = message;
        if (string.IsNullOrEmpty(signUrlJsonStr))
        {
            signUrlJsonStr = ExtractJsonObjectField(rawMessageJson, "message");
            Debug.Log($"批量查询签名URL 从原始JSON提取message对象: {signUrlJsonStr}");
        }

        Dictionary<string, string> signUrlMap = ParseJsonStringMap(signUrlJsonStr);
        if (signUrlMap != null && signUrlMap.Count > 0)
        {
            Debug.Log($"批量查询签名URL成功，共 {signUrlMap.Count} 条");
            foreach (var kvp in signUrlMap)
            {
                Debug.Log($"签名URL => storageKey: {kvp.Key}, signedUrl: {kvp.Value}");
            }
        }
        else
        {
            Debug.LogWarning($"批量查询签名URL成功，但解析结果为空，原始数据: {rawMessageJson}");
        }
    }
    #endregion

    // 子类通过覆写 OnFaceRecognizedXxx 方法来定制行为

    #region 人脸识别 On 方法覆写（Demo 特有逻辑）
    protected override void OnFaceRecognizedUserOnly(int personId, long newestUserId, FaceRecognitionTypeUserOnly data)
    {
        OnFaceRecognizedUser?.Invoke(personId, newestUserId, false);
    }

    protected override void OnFaceRecognizedMinimalist(int personId, long newestUserId, FaceRecognitionTypeMinimalist data)
    {
        OnFaceRecognizedUser?.Invoke(personId, newestUserId, false);
    }

    protected override void OnFaceRecognizedAll(int personId, long newestUserId, FaceRecognitionTypeALL data)
    {
        if (modelSelectDemo != null)
        {
            modelSelectDemo.AddFaceRecognitionAllData(data);
        }
        OnFaceRecognizedUser?.Invoke(personId, newestUserId, false);
    }
    #endregion

    /// <summary>
    /// 设备服务：
    /// 设备服务连接成功
    /// </summary>
    protected override void OnDeviceServiceConnected(string message)
    {
        Debug.Log($"设备服务已连接  OnDeviceServiceConnected : {message}");
        // 在这里处理设备服务连接成功后的逻辑
        // 例如：初始化设备管理器、开始设备扫描等
        GetAllInputDevices(); //这里的是测试用的。

        bluetoothOnlyUseMajorController.HandleOnDeviceServiceConnected(message);
    }



    /// <summary>
    /// 测试代码：IYMS服务连接成功后的测试逻辑
    /// 包含：上报测试数据、排行榜测试、语言检测、设备信息获取等
    /// </summary>
    private void TestOnIYMSServiceConnected()
    {
        //测试
        AccountInfo accountInfo = PlayerRoleManager.Instance.GetAccountInfo();
        if (accountInfo.users.Count > 0)
        {
            for (int i = 0; i < accountInfo.users.Count; i++)
            {
                RoleInfo role = accountInfo.users[i];
                Debug.Log($"测试 准备上报数据！！！: {role.userId}  {role.nickname}   ");
                SendReportEvent(REPORT_TYPE_EVENT, REPORT_BUSINESSTYPE_GAME_CALORIE, "123", "在单人模式中", role.userId);
                SendReportEvent(REPORT_TYPE_EVENT, REPORT_BUSINESSTYPE_GAME_ENERGY, "456", "在单人模式中", role.userId);
                SendReportEvent(REPORT_TYPE_EVENT, REPORT_BUSINESSTYPE_USER_GAME_DURATION, "60", "游玩时长", role.userId);//角色游戏时长上报
            }
        }

        //排行榜的测试代码，CP根据自己的业务需求去使用。
        var scoreList = new UpdateUserRankScoreList
        {
            items = new UpdateUserRankScore[]
            {
                new() { userId = 697, score = "101" },
                new() { userId = 698, score = "108" },
                new() { userId = 699, score = "399" },
                new() { userId = 701, score = "442" },
                new() { userId = 702, score = "304" }
            }
        };
        // 序列化为JSON字符串
        string jsonString = JsonUtility.ToJson(scoreList);
        UpdateUserRankScore("RankTing", jsonString, true);
        Debug.Log($"发送的排行榜1JSON: {jsonString}");
        scoreList.items[0].userId = 699;
        scoreList.items[0].score = "500";
        UpdateUserRankScore("RankTing", jsonString, false);
        Debug.Log($"发送的排行榜2JSON: {jsonString}");
        GetLeaderboardScores("RankTing", 1, 10, true);

        GetUserRankScore("RankTing", new long[] { 697, 698 }, true, GetRankHistoryScoreBusinessType.GRHSBT_MYSELF);
        Debug.Log($"调用用 GetUserRankScore");
        string currentLanguage = GetCurrentSysLanguage();
        // zh-CN
        // en-US 
        // zh-TW
        switch (currentLanguage)
        {
            case "zh-CN":
                Debug.Log("系统语言: 简体中文");
                // 处理简体中文逻辑
                break;
            case "zh-TW":
                Debug.Log("系统语言: 繁体中文（台湾）");
                // 处理繁体中文（台湾）逻辑
                break;
            default: //en-US 原理上不会有其他语言了，除非有人改了。
                Debug.Log("系统语言: 其他 = ：" + currentLanguage);
                // 处理其他语言的默认逻辑
                break;
        }

        Debug.Log($"调用用 设备的唯一信息 ： {GetSerialNumber()}");
        Debug.Log($"调用用 设备的唯一信息 ： {GetFirmwareVersion()}");

        // UpdateScore("test", "110", true);//测试代码，上报我的分数
        // GetRankScore("test", 1, 100, true);//测试代码，获取排行的信息。
        // GetRankHistoryScore("test", true, GetRankHistoryScoreBusinessType.GRHSBT_LOW_SCORE);//测试代码，获取排行的信息。
        // GetRankHistoryScore("test", true, GetRankHistoryScoreBusinessType.GRHSBT_MYSELF);//测试代码，获取排行的信息。
        // if (_accountInfo != null && _accountInfo.users.Count > 0)
        // {
        //     Debug.Log($"测试： 准备上报数据---{_accountInfo.users[0].userId}---{_accountInfo.users[0].nickname}");
        //     SendReportEvent(REPORT_TYPE_EVENT, REPORT_BUSINESSTYPE_GAME_CALORIE, "888", "在单人模式中", _accountInfo.users[0].userId);
        // }

        // Debug.Log($"测试，上报元气值： 系数，时长");
        // ReportGameEnergy reportGameEnergy = new ReportGameEnergy();
        // reportGameEnergy.f = GameEnergyType.HIGH; //高强度;
        // reportGameEnergy.v = 1000; //时间;
        // Debug.Log($"测试，上报元气值： 系数，时长" + JsonUtility.ToJson(reportGameEnergy));
        // SendReportEvent(REPORT_TYPE_EVENT, REPORT_BUSINESSTYPE_GAME_ENERGY, JsonUtility.ToJson(reportGameEnergy), "在单人模式中的高强度元气值", 0);
        // TestGameRecord testGameRecord = new TestGameRecord();
        // testGameRecord.A = "2222222";
        // testGameRecord.B = 333;
        // testGameRecord.C = new int[] { 1, 2, 3 };
        // testGameRecord.D = 200;
        // testGameRecord.E = 88.8f;
        // Debug.Log($"测试， 保存数据 " + JsonUtility.ToJson(testGameRecord));
        // SetGameDataRecord(JsonUtility.ToJson(testGameRecord));

        // Debug.Log($"755 755 755 准备上报数据！！！: {PlayerRoleManager.Instance.GetAccountInfo().users[0].userId}");

        // SendReportEvent(REPORT_TYPE_EVENT, REPORT_BUSINESSTYPE_GAME_CALORIE, "666", "在单人模式中", PlayerRoleManager.Instance.GetAccountInfo().users[0].userId);
        // SendReportEvent(REPORT_TYPE_EVENT, REPORT_BUSINESSTYPE_GAME_ENERGY, "475", "在单人模式中", PlayerRoleManager.Instance.GetAccountInfo().users[0].userId);
        // SendReportEvent(REPORT_TYPE_EVENT, REPORT_BUSINESSTYPE_USER_GAME_DURATION, "500", "游玩时长", PlayerRoleManager.Instance.GetAccountInfo().users[0].userId);//角色游戏时长上报


        //测试 数据埋点 SDK4.2.2 之后支持的。
        testMainDidan();


    }


    private void testMainDidan()
    {


        // 测试 DataStatisticsTrack - 无属性事件
        //  DataStatisticsTrack("test_simple_event"); //2026..05.12测试存在。 报名


        // 测试 DataStatisticsUserSet - 设置用户属性
        // DataStatisticsUserData userData = new DataStatisticsUserData();
        // userData.user_id = "20001";
        // userData.username = "test_user";
        // userData.user_level = 10;
        // userData.is_vip = true;
        // userData.vip_level = 99;
        // userData.update_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // string userDataJson = JsonUtility.ToJson(userData);
        // DataStatisticsUserSet(userDataJson);   //2026.05.12 测试通过。



        // 测试 enter_game 事件
        DataStatisticsEnterGameData enterGame = new DataStatisticsEnterGameData();
        enterGame.game_id = 1001;
        enterGame.game_type = "single";
        enterGame.player_number = 1;
        enterGame.level_id = "level_1";
        enterGame.difficulty = "easy";
        enterGame.song_id = "song_001";
        enterGame.course_id = "course_1";
        enterGame.track_id = "track_1";
        enterGame.prop_id = "prop_001";
        enterGame.skin_id = "skin_001";
        enterGame.role_id = "role_001";
        enterGame.player_id = "player_face_001";
        enterGame.start_timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); //2026.05.15 测试验证。


        string enterGameJson = JsonUtility.ToJson(enterGame);
        DataStatisticsTrackWithProperties("enter_game", enterGameJson); //22025.05.12 验证通过

        // 测试 DataStatisticsTrackWithPropertiesWithDateAndTimeZone - 带时间的事件
        YouDooSDKConstants.DataStatisticsBase testData = new YouDooSDKConstants.DataStatisticsBase();
        testData.game_id = 1002;
        string testDataJson = JsonUtility.ToJson(testData);
        DataStatisticsTrackWithPropertiesWithDateAndTimeZone("test_event_with_time", testDataJson, "2026-05-012 15:05:00.123", "Asia/Shanghai"); //SDK2026.05.12 测试验证


        // 测试 end_game 事件
        // DataStatisticsEndGameData endGame = new DataStatisticsEndGameData();
        // endGame.game_id = 1001;
        // endGame.game_type = "single";
        // endGame.player_number = 1;
        // endGame.level_id = "level_1";
        // endGame.difficulty = "easy";
        // endGame.song_id = "song_001";
        // endGame.course_id = "course_1";
        // endGame.track_id = "track_1";
        // endGame.prop_id = "prop_001";
        // endGame.skin_id = "skin_001";
        // endGame.role_id = "role_001";
        // endGame.player_id = "player_face_001";
        // endGame.score = 1000;
        // endGame.hit_rate = 0.95f;
        // endGame.hit_count = 95;
        // endGame.combo = 50;
        // endGame.remain_revival = 2;
        // endGame.remain_lives = 3;
        // endGame.special_mechanism_id = "";
        // endGame.result = "win";
        // endGame.grade = "S";
        // endGame.calories = 150.5f;
        // endGame.death_location = "";
        // endGame.end_timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // endGame.duration = 300;
        // endGame.fish_id = "fish_001";
        // endGame.location_id = "location_1";
        // endGame.fish_size = 10.5f;
        // endGame.bait_id = "bait_001";
        // endGame.image_list = "";

        // string endGameJson = JsonUtility.ToJson(endGame);
        // DataStatisticsTrackWithProperties("end_game", endGameJson); //2026.05.12 测试验证





        // 测试 DataStatisticsFlush - 强制上报缓存数据
        // DataStatisticsFlush();

    }

    /// <summary>
    /// 从 JSON 字符串中提取指定字段名对应的值（支持 JSON 对象、数组、字符串等类型）。
    /// 当 JsonUtility 无法将 JSON 对象映射到 string 字段时，使用此方法从原始 JSON 中手动提取。
    /// 例如：从 {"notifyType":180400,"message":{"key1":"url1"}} 中提取 "message" 字段得到 {"key1":"url1"}
    /// </summary>
    /// <param name="json">完整的 JSON 字符串</param>
    /// <param name="fieldName">要提取的字段名</param>
    /// <returns>字段对应的值字符串，未找到返回 null</returns>
    private string ExtractJsonObjectField(string json, string fieldName)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(fieldName)) return null;

        try
        {
            // 查找 "fieldName": 或 "fieldName" :
            string searchKey = "\"" + fieldName + "\"";
            int keyIdx = json.IndexOf(searchKey);
            if (keyIdx < 0) return null;

            // 找到冒号位置
            int colonIdx = json.IndexOf(':', keyIdx + searchKey.Length);
            if (colonIdx < 0) return null;

            // 跳过冒号后的空白
            int valueStart = colonIdx + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            {
                valueStart++;
            }
            if (valueStart >= json.Length) return null;

            char startChar = json[valueStart];

            // 如果值是字符串类型（以"开头），直接返回引号内的内容
            if (startChar == '"')
            {
                int strEnd = valueStart + 1;
                while (strEnd < json.Length)
                {
                    if (json[strEnd] == '\\')
                    {
                        strEnd += 2; // 跳过转义字符
                        continue;
                    }
                    if (json[strEnd] == '"')
                    {
                        return json.Substring(valueStart + 1, strEnd - valueStart - 1);
                    }
                    strEnd++;
                }
                return null;
            }

            // 如果值是对象{...}或数组[...]，通过括号匹配找到完整的值
            if (startChar == '{' || startChar == '[')
            {
                char openBracket = startChar;
                char closeBracket = startChar == '{' ? '}' : ']';
                int depth = 0;
                bool inStr = false;
                for (int i = valueStart; i < json.Length; i++)
                {
                    char c = json[i];
                    if (c == '\\' && inStr)
                    {
                        i++; // 跳过转义
                        continue;
                    }
                    if (c == '"')
                    {
                        inStr = !inStr;
                        continue;
                    }
                    if (inStr) continue;

                    if (c == openBracket) depth++;
                    else if (c == closeBracket) depth--;

                    if (depth == 0)
                    {
                        return json.Substring(valueStart, i - valueStart + 1);
                    }
                }
                return null;
            }

            // 其他类型（数字、bool、null），找到逗号或右括号为止
            int end = valueStart;
            while (end < json.Length && json[end] != ',' && json[end] != '}' && json[end] != ']')
            {
                end++;
            }
            return json.Substring(valueStart, end - valueStart).Trim();
        }
        catch (Exception e)
        {
            Debug.LogError($"ExtractJsonObjectField 提取字段 '{fieldName}' 失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解析 JSON 格式的 key-value Map 字符串为 Dictionary<string, string>
    /// 适用于 {"key1":"value1","key2":"value2"} 格式
    /// 由于 JsonUtility 不支持直接反序列化 Dictionary，这里手动解析。
    /// </summary>
    /// <param name="json">JSON 格式的 Map 字符串</param>
    /// <returns>解析后的字典，解析失败返回空字典</returns>
    private Dictionary<string, string> ParseJsonStringMap(string json)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(json)) return result;

        try
        {
            // 去掉首尾的大括号和空白
            string trimmed = json.Trim();
            if (trimmed.StartsWith("{")) trimmed = trimmed.Substring(1);
            if (trimmed.EndsWith("}")) trimmed = trimmed.Substring(0, trimmed.Length - 1);
            trimmed = trimmed.Trim();

            if (string.IsNullOrEmpty(trimmed)) return result;

            // 按逗号分隔每个键值对（需要考虑引号内可能包含逗号的情况）
            var pairs = new List<string>();
            int start = 0;
            bool inQuotes = false;
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (c == '\\' && inQuotes)
                {
                    i++; // 跳过转义字符
                    continue;
                }
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    pairs.Add(trimmed.Substring(start, i - start));
                    start = i + 1;
                }
            }
            // 添加最后一个键值对
            if (start < trimmed.Length)
            {
                pairs.Add(trimmed.Substring(start));
            }

            // 解析每个键值对
            foreach (string pair in pairs)
            {
                string p = pair.Trim();
                if (string.IsNullOrEmpty(p)) continue;

                // 找到第一个不在引号内的冒号作为分隔符
                int colonIdx = -1;
                bool inQ = false;
                for (int i = 0; i < p.Length; i++)
                {
                    char c = p[i];
                    if (c == '\\' && inQ)
                    {
                        i++;
                        continue;
                    }
                    if (c == '"')
                    {
                        inQ = !inQ;
                    }
                    else if (c == ':' && !inQ)
                    {
                        colonIdx = i;
                        break;
                    }
                }

                if (colonIdx < 0) continue;

                string key = p.Substring(0, colonIdx).Trim().Trim('"');
                string value = p.Substring(colonIdx + 1).Trim().Trim('"');

                // 处理转义字符（常见的 \" \\ \/）
                key = key.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\/", "/");
                value = value.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\/", "/");

                result[key] = value;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析签名URL Map失败: {e.Message}, 原始数据: {json}");
        }

        return result;
    }


    // // ─── 排行榜 ───
    protected override void OnIYMSGetRankScoreSuccess(IYMSRankScoreInfoList info)
    {
        if (info?.scores == null) return;
        var userIdList = new List<long>();
        // 测试1：每条遍历都单独 GetRoleInfo 测试的代码。
        foreach (var item in info.scores)
        {
            Debug.Log($"排行榜: rank={item.rank}, score={item.score}, userId={item.userId}, entryTime={item.entryTime}");
            userIdList.Add(item.userId);
        }

        // 测试2：一次性请求所有 userIds
        if (userIdList.Count > 0)
        {
            GetRoleInfo(userIdList.ToArray());
        }
    }
    protected override void OnIYMSGetRankScoreFailure(IYMSgameGetRankScoreFailureNotifyInfo info)
    {
        Debug.LogError($"排行榜  回调 失败   OnIYMSGetRankScoreFailure " + JsonUtility.ToJson(info, true));
    }


    protected override void OnIYMSGetRankHistoryScoreSuccess(IYMSgameGetRankHistoryScoreSuccessNotifyInfo info)
    {
        if (info?.scores == null) return;
        foreach (var item in info.scores)
        {
            Debug.Log($"历史排行: rank={item.rank}, score={item.score}, entryTime={item.entryTime}");
        }
    }
    protected override void OnIYMSGetRankHistoryScoreFailure(IYMSgameGetRankHistoryScoreFailureNotifyInfo info)
    {
        Debug.LogError($"排行榜 回调  历史  失败  OnIYMSGetRankScoreFailure " + JsonUtility.ToJson(info, true));
    }

    /// <summary>
    /// 获取角色信息失败回调
    /// </summary>
    /// <param name="message">错误信息</param>
    protected override void OnIYMSGetRoleInfoFailure(string message)
    {
        Debug.LogError($"[IYMS] 获取角色信息失败: {message}");
    }

    /// <summary>
    /// 获取角色信息成功回调 - Demo 中遍历打印角色信息
    /// </summary>
    /// <param name="roleInfoList">角色信息列表</param>
    protected override void OnIYMSGetRoleInfoSuccess(RoleInfoList roleInfoList)
    {
        if (roleInfoList == null)
        {
            Debug.LogWarning("[IYMS] 获取角色信息成功，但 roleInfoList 为 null");
            return;
        }

        Debug.Log($"[IYMS] Demo 层 OnIYMSGetRoleInfoSuccess: 共 {roleInfoList.items.Length} 个角色信息");

        for (int i = 0; i < roleInfoList.items.Length; i++)
        {
            RoleInfo role = roleInfoList.items[i];
            Debug.Log($"[IYMS] Demo 角色 {i + 1}: userId={role.userId}, nickname={role.nickname}, avatarId={role.avatarId}, avatarUri={role.avatarUri}, gender={role.gender}, guardian={role.guardian}, heightMm={role.heightMm}, weightG={role.weightG}");
        }

        if (roleInfoList.items.Length == 0)
        {
            Debug.LogWarning("[IYMS] 获取角色信息成功，但角色列表为空");
        }
    }



}
