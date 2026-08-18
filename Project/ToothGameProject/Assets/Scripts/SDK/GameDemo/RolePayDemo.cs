using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YouDooSDK.UI;
using static YouDooSDKConstants;

public class RolePayDemo : RemoteInputBase
{
    [SerializeField] private Text VipInfoText;

    [SerializeField] private Text CurrencyInfoText;

    /// <summary>
    /// 显示查询到的商品列表及当前选中商品的详情
    /// </summary>
    [SerializeField] private Text ProductInfoText;

    /// <summary>
    /// 显示各类操作的回调结果
    /// </summary>
    [SerializeField] private Text CallbackResultText;

    // 查询到的商品列表
    private List<GameProductInfo> _queryProductList = new List<GameProductInfo>();
    // 当前选中的商品索引
    private int _currentProductIndex = 0;
    // 是否正在等待购买记录查询以决定是否继续购买
    private bool _isPendingPurchaseCheck = false;

    // Start is called before the first frame update
    protected override void Start()
    {
        PlayerRoleManager.Instance.UserInfoUpdateCallback += UpdateAccountInfoUI;
        base.Start();
        SetNewButtonImageArray();
        SetButtonState(true);
        UpdateAccountInfoUI();
        UpdateProductInfoUI();
    }

    protected override void OnDestroy()
    {
        PlayerRoleManager.Instance.UserInfoUpdateCallback -= UpdateAccountInfoUI;
        base.OnDestroy();
    }

    private void UpdateAccountInfoUI()
    {
        AccountInfo accountInfo = PlayerRoleManager.Instance.GetAccountInfo();
        if (accountInfo != null)
        {
            // 打印并显示 VIP 信息
            if (accountInfo.vips != null && accountInfo.vips.Count > 0)
            {
                string vipStr = RenderAPI.GetTextByLanId("sdk_demo_vip_info_header") + "\n";
                foreach (var vip in accountInfo.vips)
                {
                    vipStr += RenderAPI.GetTextByLanId("sdk_demo_vip_info_line", vip.type, vip.expire) + "\n";
                }
                if (VipInfoText != null) VipInfoText.text = vipStr;
                Debug.Log("39 39 39 39 UpdateAccountInfoUI " + vipStr);
            }
            else
            {
                if (VipInfoText != null) VipInfoText.text = RenderAPI.GetTextByLanId("sdk_demo_vip_info_empty");
                Debug.Log("39 39 39 39 UpdateAccountInfoUI VIP信息: 无");
            }

            // 打印并显示 Currency 信息
            if (accountInfo.currencies != null && accountInfo.currencies.Count > 0)
            {
                string currencyStr = RenderAPI.GetTextByLanId("sdk_demo_currency_info_header") + "\n";
                foreach (var currency in accountInfo.currencies)
                {
                    currencyStr += RenderAPI.GetTextByLanId("sdk_demo_currency_info_line", currency.type, currency.currency) + "\n";
                }
                if (CurrencyInfoText != null) CurrencyInfoText.text = currencyStr;
                Debug.Log(currencyStr);
            }
            else
            {
                if (CurrencyInfoText != null) CurrencyInfoText.text = RenderAPI.GetTextByLanId("sdk_demo_currency_info_empty");
                Debug.Log("39 39 39 39 UpdateAccountInfoUI 虚拟币信息: 无");
            }
        }
        else
        {
            if (VipInfoText != null) VipInfoText.text = RenderAPI.GetTextByLanId("sdk_demo_account_info_missing");
            if (CurrencyInfoText != null) CurrencyInfoText.text = RenderAPI.GetTextByLanId("sdk_demo_account_info_missing");
            Debug.LogWarning("39 39 39 39 UpdateAccountInfoUI 未能获取到账号信息 AccountInfo 为 null");
        }
    }

    /// <summary>
    /// 刷新商品信息显示
    /// </summary>
    private void UpdateProductInfoUI()
    {
        if (ProductInfoText == null) return;

        if (_queryProductList == null || _queryProductList.Count == 0)
        {
            ProductInfoText.text = RenderAPI.GetTextByLanId("sdk_demo_product_list_empty_hint");
            return;
        }

        string info = RenderAPI.GetTextByLanId("sdk_demo_product_list_header", _currentProductIndex + 1, _queryProductList.Count) + "\n";

        // 列出所有商品名称，并高亮当前选中的
        for (int i = 0; i < _queryProductList.Count; i++)
        {
            var p = _queryProductList[i];
            if (i == _currentProductIndex)
                info += RenderAPI.GetTextByLanId("sdk_demo_product_list_selected", i + 1, p.name) + "\n";
            else
                info += RenderAPI.GetTextByLanId("sdk_demo_product_list_item", i + 1, p.name) + "\n";
        }

        // 显示当前选中商品详情
        GameProductInfo current = _queryProductList[_currentProductIndex];
        info += "\n" + RenderAPI.GetTextByLanId("sdk_demo_product_detail_title") + "\n";
        info += RenderAPI.GetTextByLanId("sdk_demo_product_detail_id", current.id) + "\n";
        info += RenderAPI.GetTextByLanId("sdk_demo_product_detail_name", current.name) + "\n";
        if (!string.IsNullOrEmpty(current.subName))
            info += RenderAPI.GetTextByLanId("sdk_demo_product_detail_sub_name", current.subName) + "\n";
        if (!string.IsNullOrEmpty(current.intro))
            info += RenderAPI.GetTextByLanId("sdk_demo_product_detail_intro", current.intro) + "\n";
        // showOriginal / showAmount 是字符串（如"1分"），直接显示
        info += RenderAPI.GetTextByLanId("sdk_demo_product_detail_original", string.IsNullOrEmpty(current.showOriginal) ? "-" : current.showOriginal) + "\n";
        info += RenderAPI.GetTextByLanId("sdk_demo_product_detail_price", string.IsNullOrEmpty(current.showAmount) ? RenderAPI.GetTextByLanId("sdk_demo_free") : current.showAmount) + "\n";

        if (current.items != null && current.items.Count > 0)
        {
            info += RenderAPI.GetTextByLanId("sdk_demo_product_items_header", current.items.Count) + "\n";
            foreach (var item in current.items)
            {
                info += RenderAPI.GetTextByLanId("sdk_demo_product_item_line", item.name, item.value) + "\n";
            }
        }

        ProductInfoText.text = info;
    }

    /// <summary>
    /// 检查 WiFi 是否正常，不正常则显示提示并返回 false
    /// </summary>
    private bool CheckWifi()
    {
        if (!AndroidServerInfoDemo.Instance.IsWifiOk())
        {
            Debug.LogWarning("[RolePayDemo] 网络不可用，操作已取消");
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_wifi_error_tip"));
            if (CallbackResultText != null)
                CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_wifi_error_detail");
            return false;
        }
        return true;
    }

    protected override void GroupButton1Change()
    {
        switch (_curSelcetIndex)
        {
            case 0:
                // 先检查网络
                if (!CheckWifi()) break;
                // 购买前先查询购买记录，防止重复购买
                if (_queryProductList != null && _queryProductList.Count > 0)
                {
                    GameProductInfo product = _queryProductList[_currentProductIndex];
                    Debug.Log($"[RolePayDemo] 准备购买商品 ID={product.id}, 名称={product.name}，先查询购买记录...");
                    _isPendingPurchaseCheck = true;
                    AndroidServerInfoDemo.Instance.QueryAppPayItem();
                    if (CallbackResultText != null)
                        CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_purchase_prepare", product.name);
                }
                else
                {
                    Debug.LogWarning("[RolePayDemo] 尚未查询到商品，请先执行查询游戏商品(按钮3)");
                    if (CallbackResultText != null)
                        CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_product_query_first_buy");
                }
                break;
            case 1:
                // 先检查网络
                if (!CheckWifi()) break;
                AndroidServerInfoDemo.Instance.QueryAppPayItem();
                if (CallbackResultText != null)
                    CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_query_purchased_wait");
                break;
            case 2:
                // 先检查网络
                if (!CheckWifi()) break;
                AndroidServerInfoDemo.Instance.QueryGameProducts();
                if (CallbackResultText != null)
                    CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_query_product_wait");
                break;
            case 3:
                SceneManager.LoadScene(DemoUtil.SceneName.TestDemo.ToString());
                break;
        }
    }

    /// <summary>
    /// Group2 用于切换当前选中的商品：
    /// 按钮0 = 上一个商品，按钮1 = 下一个商品
    /// </summary>
    protected override void GroupButton2Change()
    {

        switch (_curSelcetIndex)
        {
            case 0:
                if (_queryProductList == null || _queryProductList.Count == 0)
                {
                    if (CallbackResultText != null)
                        CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_product_query_first_switch");
                    return;
                }
                // 上一个商品
                _currentProductIndex--;
                if (_currentProductIndex < 0)
                    _currentProductIndex = _queryProductList.Count - 1;
                break;
            case 1:
                if (_queryProductList == null || _queryProductList.Count == 0)
                {
                    if (CallbackResultText != null)
                        CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_product_query_first_switch");
                    return;
                }
                // 下一个商品
                _currentProductIndex++;
                if (_currentProductIndex >= _queryProductList.Count)
                    _currentProductIndex = 0;
                break;

            case 2:
                Debug.Log("[RolePayDemo] 调用系统的充值的接口 ！！！");
                AndroidServerInfoDemo.Instance.OpenYmsYbPayment(9L);
                break;
            case 3:
                Debug.Log("[RolePayDemo] 打开YMS VIP充值页面 ！！！");
                AndroidServerInfoDemo.Instance.OpenYmsVIPPayment();
                break;
            case 4:
                Debug.Log("[RolePayDemo] 打开YMS CDKEY兑换页面 ！！！");
                AndroidServerInfoDemo.Instance.OpenYmsCDKEYPayment();
                break;
        }
        Debug.Log($"[RolePayDemo] 切换商品索引: {_currentProductIndex}, 商品: {_queryProductList[_currentProductIndex].name}");
        UpdateProductInfoUI();
    }

    protected override void GroupButton3Change()
    {
        switch (_curSelcetIndex)
        {
            case 0:
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_current_vip", AndroidServerInfoDemo.Instance.IsVip()));
                break;
            case 1:
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_current_yuanqidou", AndroidServerInfoDemo.Instance.GetYuanQiDou()));
                break;
        }
    }

    protected override void SubscribeEvent()
    {
        base.SubscribeEvent();
        AndroidServerInfoDemo.OnQueryGameProductsSuccess += HandleQueryGameProductsSuccess;
        AndroidServerInfoDemo.OnQueryGameProductsFailure += HandleQueryGameProductsFailure;
        AndroidServerInfoDemo.OnPurchaseGameProductsSuccess += HandlePurchaseGameProductsSuccess;
        AndroidServerInfoDemo.OnPurchaseGameProductsFailure += HandlePurchaseGameProductsFailure;
        AndroidServerInfoDemo.OnQueryAppPayItemSuccess += HandleQueryAppPayItemSuccess;
        AndroidServerInfoDemo.OnQueryAppPayItemFailure += HandleQueryAppPayItemFailure;
    }

    protected override void UnSubscribeEvent()
    {
        base.UnSubscribeEvent();
        AndroidServerInfoDemo.OnQueryGameProductsSuccess -= HandleQueryGameProductsSuccess;
        AndroidServerInfoDemo.OnQueryGameProductsFailure -= HandleQueryGameProductsFailure;
        AndroidServerInfoDemo.OnPurchaseGameProductsSuccess -= HandlePurchaseGameProductsSuccess;
        AndroidServerInfoDemo.OnPurchaseGameProductsFailure -= HandlePurchaseGameProductsFailure;
        AndroidServerInfoDemo.OnQueryAppPayItemSuccess -= HandleQueryAppPayItemSuccess;
        AndroidServerInfoDemo.OnQueryAppPayItemFailure -= HandleQueryAppPayItemFailure;
    }

    // ==================== 回调处理 ====================

    /// <summary>
    /// 查询游戏商品成功
    /// </summary>
    private void HandleQueryGameProductsSuccess(List<GameProductInfo> products)
    {
        _queryProductList = products ?? new List<GameProductInfo>();
        _currentProductIndex = 0;

        Debug.Log($"[RolePayDemo] 查询商品成功，共 {_queryProductList.Count} 个商品");

        if (CallbackResultText != null)
        {
            if (_queryProductList.Count > 0)
                CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_query_product_success_count", _queryProductList.Count);
            else
                CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_query_product_success_empty");
        }

        UpdateProductInfoUI();
    }

    /// <summary>
    /// 查询游戏商品失败
    /// </summary>
    private void HandleQueryGameProductsFailure(IYMSqueryGameProductsFailureNotifyInfo failureInfo)
    {
        Debug.LogError($"[RolePayDemo] 查询商品失败 code={failureInfo?.code}, msg={failureInfo?.message}");
        if (CallbackResultText != null)
            CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_query_product_failed_detail", failureInfo?.code, failureInfo?.message);
    }

    /// <summary>
    /// 购买游戏商品成功
    /// </summary>
    private void HandlePurchaseGameProductsSuccess(string message)
    {
        Debug.Log($"[RolePayDemo] 购买成功: {message}");
        if (CallbackResultText != null)
            CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_purchase_success_detail", message);
    }

    /// <summary>
    /// 购买游戏商品失败
    /// </summary>
    private void HandlePurchaseGameProductsFailure(IYMSpurchaseGameProductsFailureNotifyInfo failureInfo)
    {
        Debug.LogError($"[RolePayDemo] 购买失败 code={failureInfo?.code}, msg={failureInfo?.message}");
        if (CallbackResultText != null)
            CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_purchase_failed_detail", failureInfo?.code, failureInfo?.message);
    }

    /// <summary>
    /// 查询已购买记录成功
    /// </summary>
    private void HandleQueryAppPayItemSuccess(string message)
    {
        Debug.Log($"[RolePayDemo] 查询已购买记录成功: {message}");

        // 如果是在购买流程中触发的查询（_isPendingPurchaseCheck == true）
        if (_isPendingPurchaseCheck)
        {
            _isPendingPurchaseCheck = false;

            if (_queryProductList == null || _queryProductList.Count == 0)
            {
                if (CallbackResultText != null)
                    CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_query_purchased_success_product_empty");
                return;
            }

            GameProductInfo product = _queryProductList[_currentProductIndex];
            // 通过检查 message 中是否包含商品 ID 来判断是否已购买
            bool alreadyPurchased = !string.IsNullOrEmpty(message) &&
                                    message.Contains(product.id.ToString());

            if (alreadyPurchased)
            {
                Debug.LogWarning($"[RolePayDemo] 商品 {product.name}(ID={product.id}) 已购买，不能再次购买");
                TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_product_already_purchased_tip"));
                if (CallbackResultText != null)
                    CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_product_already_purchased_detail", product.name, product.id);
            }
            else
            {
                // 使用 amount 字段（数值）作为价格传参，showAmount 仅用于显示
                Debug.Log($"[RolePayDemo] 验证通过，执行购买 ID={product.id}, amount={product.amount}, 名称={product.name}");
                AndroidServerInfoDemo.Instance.PurchaseGameProducts((int)product.id, product.amount);
                if (CallbackResultText != null)
                    CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_purchasing_detail", product.name, product.id, product.showAmount);
            }
        }
        else
        {
            // 手动点击「查询已购记录」按钮时，直接显示原始数据
            if (CallbackResultText != null)
                CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_query_purchased_success_detail", message);
        }
    }

    /// <summary>
    /// 查询已购买记录失败
    /// </summary>
    private void HandleQueryAppPayItemFailure(IYMSqueryAppPayItemFailureNotifyInfo failureInfo)
    {
        Debug.LogError($"[RolePayDemo] 查询已购买记录失败 code={failureInfo?.code}, msg={failureInfo?.message}");

        if (_isPendingPurchaseCheck)
        {
            // 购买流程中查询失败，取消购买
            _isPendingPurchaseCheck = false;
            Debug.LogWarning("[RolePayDemo] 购买记录查询失败，购买操作已取消");
            TipsManager.Instance.ShowTip(RenderAPI.GetTextByLanId("sdk_demo_purchase_record_failed_tip"));
            if (CallbackResultText != null)
                CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_purchase_record_failed_detail", failureInfo?.code);
        }
        else
        {
            if (CallbackResultText != null)
                CallbackResultText.text = RenderAPI.GetTextByLanId("sdk_demo_query_purchased_failed_detail", failureInfo?.code, failureInfo?.message);
        }
    }
}
