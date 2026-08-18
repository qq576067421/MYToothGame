//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Unity.Services.Core;
//using Unity.Services.Core.Environments;
//using UnityEngine;
//using UnityEngine.Purchasing;
//using UnityEngine.Purchasing.Extension;

//namespace UnityUI
//{
//    public class UIPurchaser : MonoBehaviour, IDetailedStoreListener
//    {
//        private IStoreController mStoreController;
//        private IExtensionProvider mExtensionProvider;


//        public Action<int, string, string, string> m_BuyResultCall;

//        //void Start()
//        //{
//        //    List<string> list = new List<string>();
//        //    for (int i = 1; i <= 6; ++i)
//        //    {
//        //        list.Add("com.com.fiveseat.buy" + i);
//        //    }

//        //    InitPurchasing(list);
//        //}

//        public void InitPurchasing(List<string> product_ids)
//        {
//            Debug.Log("开始初始化内购1");
//            if (IsInitialized())
//            {
//                return;
//            }
//            // Create a builder, first passing in a suite of Unity provided stores.
//            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

//            foreach (var key in product_ids)
//            {
//                //添加商品ID和类型 对应定义的商品ID
//                //(Consumable:要注意google play商品需要勾选“允许用户在单笔交易中购买多件此商品")
//                string productID = key.Trim();
//                Debug.Log("add product ProductType.Consumable:" + productID);
//                builder.AddProduct(productID, ProductType.Consumable);
//            }
//            UnityPurchasing.Initialize(this, builder);
//            Debug.Log("开始初始化内购2");
//        }


//        //购买
//        public void BuyProduct(string productID)
//        {
//            //ISGoogle = false;
//            if (IsInitialized())
//            {
//                Product produdt = mStoreController.products.WithID(productID);
//                if (produdt != null && produdt.availableToPurchase)
//                {
//                    mStoreController.InitiatePurchase(produdt);

//                    Debug.Log(produdt.metadata.localizedPrice);
//                }
//                else
//                {
//                    Debug.Log("fail");
//                }
//            }
//            else
//            {
//                Debug.Log("BuyProductID FAIL. Not initialized.");
//            }
//        }

//        //恢复购买
//        public void ReSotre()
//        {
//            if (!IsInitialized())
//            {
//                return;
//            }

//            if (mExtensionProvider != null && (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer))
//            {
//                var apple = mExtensionProvider.GetExtension<IAppleExtensions>();
//                apple.RestoreTransactions((result) =>
//                {
//                    // Restore purchases initiated. See ProcessPurchase for any restored transacitons.
//                    Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
//                });
//            }
//        }

//        private bool IsInitialized()
//        {
//            return mStoreController != null && mExtensionProvider != null;
//        }

//        //---------------IStoreListener的四个接口的实现-----------

//        //初始化成功
//        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
//        {
//            mStoreController = controller;
//            mExtensionProvider = extensions;
//            Debug.Log("----------------初始化内购成功---------------------");

//        }

//        //初始化失败
//        public void OnInitializeFailed(InitializationFailureReason error)
//        {

//            Debug.Log("----------------初始化内购失败-------------------- -：" + error);
//        }
//        public void OnInitializeFailed(InitializationFailureReason error, string message)
//        {
//            Debug.Log("----------------初始化内购失败-------------------- -：" + error + " message:" + message);
//        }
//        //购买失败
//        public void OnPurchaseFailed(Product e, PurchaseFailureReason p)
//        {
//            if (e.hasReceipt)
//            {
//                var wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(e.receipt);
//                var store = (string)wrapper["Store"];
//                var payload = (string)wrapper["Payload"];

//                if (m_BuyResultCall != null)
//                {
//                    m_BuyResultCall((int)p, e.definition.id, "", store);
//                }
//            }
//            else
//            {
//                if (m_BuyResultCall != null)
//                {
//                    m_BuyResultCall((int)p, e.definition.id, "", "");
//                }
//            }

//        }


//        //购买成功和恢复成功的回调，可以根据id的不同进行不同的操作
//        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
//        {
//            var wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(e.purchasedProduct.receipt);
//            var store = (string)wrapper["Store"];
//            var payload = (string)wrapper["Payload"];

//            var yanzheng = "";
//#if UNITY_ANDROID
//            var gpDetails = (Dictionary<string, object>)MiniJson.JsonDecode(payload);
//            if (gpDetails.ContainsKey("json"))
//            {
//                yanzheng = (string)gpDetails["json"];
//            }
//#elif UNITY_IPHONE || UNITY_IOS
//            yanzheng = payload;
//#endif
//            //NetworkManager.SendCustom(CLIENT_CUSTOM_MESSAGE_ENUM.CLIENT_CUSTOMMSG_GOOGLE_DELIVERY, 0, gpJson, gpSig);

//            if (m_BuyResultCall != null)
//            {
//                m_BuyResultCall(-1, e.purchasedProduct.definition.id, yanzheng, store);
//            }
//            return PurchaseProcessingResult.Complete;

//        }

//        public void OnPurchaseFailed(Product e, PurchaseFailureDescription failureDescription)
//        {
//            if (e.hasReceipt)
//            {
//                var wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(e.receipt);
//                var store = (string)wrapper["Store"];
//                var payload = (string)wrapper["Payload"];

//                if (m_BuyResultCall != null)
//                {
//                    m_BuyResultCall((int)failureDescription.reason, e.definition.id, "", store);
//                }
//            }
//            else
//            {
//                if (m_BuyResultCall != null)
//                {
//                    m_BuyResultCall((int)failureDescription.reason, e.definition.id, "", "");
//                }
//            }
//        }
//    }
//}
