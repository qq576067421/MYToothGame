using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DemoUtil;
using static YouDooSDKConstants;
/// <summary>
/// NFC功能的演示界面。
/// NFC 没有写的功能。
/// 
/// </summary>
public class NFCSceneDemo : RemoteInputBase
{


    [SerializeField] private Text TipsText;

    protected override void Start()
    {
        base.Start();
        SetNewButtonImageArray();
        SetButtonState(true);
        ((AndroidServerInfoDemo)AndroidServerInfoDemo.Instance).SetNFCSceneDemo(this);
    }

    protected override void EscapePressedBase()
    {

    }


    protected override void GroupButton1Change()
    {
        switch (_curSelcetIndex)
        {
            case 0:
                NFCCardInfo cardInfo = AndroidServerInfoDemo.Instance.GetNfc();
                Debug.Log($"获取NFC卡片信息  {cardInfo?.cardId}--{cardInfo?.ver}--{cardInfo?.type}--{cardInfo?.status}--{cardInfo?.action}--{cardInfo?.permissions}--{cardInfo?.flags}--{cardInfo?.customData}");

                if (cardInfo.type == NFCCardType.GAME_ITEM_CARD)
                {
                    PrintNFCCardInfo(cardInfo, Color.green);
                }
                else
                {
                    PrintNFCCardInfo(null, Color.red, RenderAPI.GetTextByLanId("sdk_demo_nfc_key_card_ignored"));
                }
                break;

            case 1:
                SceneManager.LoadScene(SceneName.TestDemo.ToString());
                break;
        }
    }

    protected override void SubscribeEvent()
    {
        base.SubscribeEvent();
    }

    protected override void UnSubscribeEvent()
    {
        base.UnSubscribeEvent();
    }

    public void HandleNFCServiceNotifyInfo(string messageJson)
    {
        try
        {
            NFCNotice nfcServerInfo = JsonUtility.FromJson<NFCNotice>(messageJson);
            if (nfcServerInfo != null)
            {
                Debug.Log($"收到NFC通知消息 160  {nfcServerInfo.notifyNFCType}--{nfcServerInfo.message}");
                YouDooNotifyNFCType notifyType = nfcServerInfo.notifyNFCType;
                switch (notifyType)
                {
                    case YouDooNotifyNFCType.NFC_INSERTED:
                        Debug.Log("收到NFC通知消息 NFC_INSERTED   NFC卡片插入（已验证通过）需要解析数据 " + nfcServerInfo.message);
                        if (!string.IsNullOrEmpty(nfcServerInfo.message) && nfcServerInfo.message != "")
                        {
                            try
                            {
                                NFCCardInfo cardInfo = JsonUtility.FromJson<NFCCardInfo>(nfcServerInfo.message);
                                if (cardInfo.type == NFCCardType.GAME_ITEM_CARD)
                                {
                                    PrintNFCCardInfo(cardInfo, Color.red);
                                }
                                else
                                {
                                    PrintNFCCardInfo(null, Color.red, RenderAPI.GetTextByLanId("sdk_demo_nfc_key_card_ignored"));
                                }

                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"getNfc 解析JSON失败: {e.Message}, 原始数据: {nfcServerInfo.message}");
                            }
                        }
                        break;
                    case YouDooNotifyNFCType.NFC_REMOVED:
                        Debug.Log("收到NFC通知消息 NFC_REMOVED   NFC卡片移除");
                        PrintNFCCardInfo(null, Color.red);
                        break;
                    default:
                        Debug.LogWarning($"未知NFC通知类型: {notifyType}");
                        break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析设备服务通知失败: {e.Message}");
        }
    }

    public void PrintNFCCardInfo(NFCCardInfo cardInfo, Color color, string prefix = null)
    {
        if (cardInfo == null)
        {
            TipsText.color = Color.white;
            if (prefix == null)
            {
                TipsText.text = RenderAPI.GetTextByLanId("sdk_demo_nfc_card_info_null");
            }
            else
            {
                TipsText.text = prefix;
            }

            Debug.LogWarning("NFCCardInfo 为 null，无法打印");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_nfc_detail_title"));
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_nfc_card_id", cardInfo.cardId));
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_nfc_ver", cardInfo.ver));
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_nfc_type", cardInfo.type));
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_nfc_status", cardInfo.status));
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_nfc_action", cardInfo.action));
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_nfc_permissions", cardInfo.permissions));
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_nfc_flags", cardInfo.flags));
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_nfc_custom_data", cardInfo.customData));
        sb.AppendLine("======================");

        string temp = sb.ToString();
        // 打印到Unity控制台
        Debug.Log(temp);

        if (TipsText != null)
        {
            TipsText.text = temp;
            TipsText.color = color;
        }
    }

}
