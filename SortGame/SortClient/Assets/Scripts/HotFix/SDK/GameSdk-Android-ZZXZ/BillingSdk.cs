using System;
using System.Collections.Generic;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class BillingSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.billing.BillingSdk";

        private static GameSdkNoneParamVoidCallback mQueryProductDetailsSuccessCallback;
        private static GameSdkIntParamVoidCallback mQueryProductDetailsFailCallback;
        private static GameSdkStringStringStringParamVoidCallback mPurchaseSuccessCallback;
        private static GameSdkStringIntParamVoidCallback mPurchaseFailCallback;
        private static GameSdkStringStringStringParamVoidCallback mRestoreSuccessCallback;
        private static GameSdkStringIntParamVoidCallback mRestoreFailCallback;
        private static GameSdkStringParamVoidCallback mConsumeSuccessCallback;
        private static GameSdkStringIntParamVoidCallback mConsumeFailCallback;

        // 此方法用于初始化计费SDK，接受一个表示产品ID的字符串作为参数
        public static void InitBillingSDK(string productIds)
        {
            // 如果当前应用运行在安卓平台
            if (Application.platform == RuntimePlatform.Android)
            {
                // 使用AndroidJavaClass创建一个代表UnityPlayer类的对象
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    // 获取当前的Activity对象，这里假设unityPlayerClassPath中有一个名为"currentActivity"的静态成员
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    // 使用AndroidJavaClass创建一个代表安卓相关类（由androidClassPath指定）的对象
                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        // 调用安卓类中的静态方法"init"，传入当前Activity对象和产品ID字符串
                        javaClass.CallStatic("init", currentActivity, productIds);
                    }
                }
            }
        }

        // 此方法用于设置产品ID，接受三个不同类型（消费型、非消费型、订阅型）的字符串集合作为参数
        public static void SetProductIds(HashSet<string> consumeIds, HashSet<string> nonConsumeIds, HashSet<string> subscribeIds)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    // 辅助操作：创建三个Java的HashSet对象，分别对应传入的三个字符串集合
                    using (var consumeSet = CreateJavaSetFromStrings(consumeIds))
                    using (var nonConsumeSet = CreateJavaSetFromStrings(nonConsumeIds))
                    using (var subscribeSet = CreateJavaSetFromStrings(subscribeIds))
                    {
                        // 创建代表安卓相关类（由androidClassPath指定）的对象
                        using (var javaClass = new AndroidJavaClass(androidClassPath))
                        {
                            // 调用安卓类中的静态方法"setProductIds"，传入创建好的三个Java集合对象
                            javaClass.CallStatic("setProductIds", consumeSet, nonConsumeSet, subscribeSet);
                        }
                    }
                }
            }
        }

        // 辅助函数：根据给定的字符串集合创建一个Java的HashSet对象并返回
        private static AndroidJavaObject CreateJavaSetFromStrings(HashSet<string> ids)
        {
            // 创建一个Java的HashSet对象
            var javaSet = new AndroidJavaObject("java.util.HashSet");
            foreach (var id in ids)
            {
                // 向Java的HashSet对象中添加元素，每个元素通过创建一个Java的String对象传入
                javaSet.Call<bool>("add", new AndroidJavaObject("java.lang.String", id));
            }
            return javaSet;
        }

        // 此方法用于检查是否准备就绪，主要针对安卓平台
        public static bool IsReady()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                // 创建代表安卓相关类（由androidClassPath指定）的对象
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    // 调用安卓类中的静态方法"isReady"并返回结果
                    return javaClass.CallStatic<bool>("isReady");
                }
            }
            return false;
        }

        // 此方法用于查询应用内产品详细信息，仅在安卓平台执行
        public static void QueryInappProductDetails(GameSdkNoneParamVoidCallback successCallback, GameSdkIntParamVoidCallback failCallback)
        {
            mQueryProductDetailsSuccessCallback = successCallback;
            mQueryProductDetailsFailCallback = failCallback;
            GameSDKCallbackListener.OnQueryProductDetailsSuccessEvent += OnQueryProductDetailsSuccess;
            GameSDKCallbackListener.OnQueryProductDetailsFailEvent += OnQueryProductDetailsFail;
            if (Application.platform == RuntimePlatform.Android)
            {
                // 创建代表安卓相关类（由androidClassPath指定）的对象
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    // 调用安卓类中的静态方法"queryInappProductDetails"
                    javaClass.CallStatic("queryInappProductDetails");
                }
            }else{
                OnQueryProductDetailsSuccess();
            }
        }

        // 此方法用于查询订阅产品详细信息，仅在安卓平台执行
        public static void QuerySubsProductDetails(GameSdkNoneParamVoidCallback successCallback, GameSdkIntParamVoidCallback failCallback)
        {
            mQueryProductDetailsSuccessCallback = successCallback;
            mQueryProductDetailsFailCallback = failCallback;
            GameSDKCallbackListener.OnQueryProductDetailsSuccessEvent += OnQueryProductDetailsSuccess;
            GameSDKCallbackListener.OnQueryProductDetailsFailEvent += OnQueryProductDetailsFail;
            if (Application.platform == RuntimePlatform.Android)
            {
                // 创建代表安卓相关类（由androidClassPath指定）的对象
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    // 调用安卓类中的静态方法"querySubsProductDetails"
                    javaClass.CallStatic("querySubsProductDetails");
                }
            }else{
                OnQueryProductDetailsSuccess();
            }
        }

        // 此方法用于查询应用内产品状态，用于恢复使用，仅在安卓平台执行
        public static void RestoreInappPurchase(GameSdkStringStringStringParamVoidCallback successCallback, GameSdkStringIntParamVoidCallback failCallback)
        {
            mRestoreSuccessCallback = successCallback;
            mRestoreFailCallback = failCallback;
            GameSDKCallbackListener.OnRestoreSuccessEvent +=  OnHandleRestoreSuccess;
            GameSDKCallbackListener.OnRestoreFailEvent += OnHandleRestoreFail;
            if (Application.platform == RuntimePlatform.Android)
            {
                // 创建代表安卓相关类（由androidClassPath指定）的对象
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    // 调用安卓类中的静态方法"queryInappStates"
                    javaClass.CallStatic("queryInappStates");
                }
            }else{
                OnHandleRestoreSuccess("", "", "");
            }
        }

        // 此方法用于查询订阅产品状态，用于恢复使用，仅在安卓平台执行
        public static void RestoreSubsPurchase(GameSdkStringStringStringParamVoidCallback successCallback, GameSdkStringIntParamVoidCallback failCallback)
        {
            mRestoreSuccessCallback = successCallback;
            mRestoreFailCallback = failCallback;
            GameSDKCallbackListener.OnRestoreSuccessEvent += OnHandleRestoreSuccess;
            GameSDKCallbackListener.OnRestoreFailEvent += OnHandleRestoreFail;
            if (Application.platform == RuntimePlatform.Android)
            {
                // 创建代表安卓相关类（由androidClassPath指定）的对象
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    // 调用安卓类中的静态方法"querySubsStates"
                    javaClass.CallStatic("querySubsStates");
                }
            }else{
                OnHandleRestoreSuccess("", "", "");
            }
        }

        // 此方法用于开始计费流程，接受一个产品ID字符串作为参数，仅在安卓平台执行
        public static void Purchase(string productId, GameSdkStringStringStringParamVoidCallback successCallback, GameSdkStringIntParamVoidCallback failCallback)
        {
            mPurchaseSuccessCallback = successCallback;
            mPurchaseFailCallback = failCallback;
            GameSDKCallbackListener.OnPurchaseSuccessEvent += OnHandlePurchaseSuccess;
            GameSDKCallbackListener.OnPurchaseFailEvent += OnHandlePurchaseFail;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        // 调用安卓类中的静态方法"startBilling"，传入当前Activity对象和产品ID对应的Java String对象
                        javaClass.CallStatic("startBilling", productId);
                    }
                }
            }else{
                OnHandlePurchaseSuccess(productId, "", "");
            }
        }

        // 此方法用于开始计费流程，并传入内购场景和自定义参数，仅在安卓平台执行
        public static void Purchase(string productId, string scene, string paramsJson, GameSdkStringStringStringParamVoidCallback successCallback, GameSdkStringIntParamVoidCallback failCallback)
        {
            mPurchaseSuccessCallback = successCallback;
            mPurchaseFailCallback = failCallback;
            GameSDKCallbackListener.OnPurchaseSuccessEvent += OnHandlePurchaseSuccess;
            GameSDKCallbackListener.OnPurchaseFailEvent += OnHandlePurchaseFail;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        javaClass.CallStatic("startBilling", productId, scene, paramsJson);
                    }
                }
            }else{
                OnHandlePurchaseSuccess(productId, "", "");
            }
        }

        /// <summary>
        /// 已废弃：请使用 FinalizePurchase 完成购买确认。保留此接口以兼容旧版本。
        /// </summary>
        public static void Consume(string productId, GameSdkStringParamVoidCallback successCallback, GameSdkStringIntParamVoidCallback failCallback)
        {
            FinalizePurchase(productId, successCallback, failCallback);
        }

        // 完成购买确认，适用于消耗品、非消耗品和订阅；请在发货并持久化后调用。
        public static void FinalizePurchase(string productId, GameSdkStringParamVoidCallback successCallback, GameSdkStringIntParamVoidCallback failCallback)
        {
            mConsumeSuccessCallback = successCallback;
            mConsumeFailCallback = failCallback;
            GameSDKCallbackListener.OnConsumeSuccessEvent += OnHandleConsumePurchaseSuccess;
            GameSDKCallbackListener.OnConsumeFailEvent += OnHandleConsumePurchaseFail;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        // 调用安卓类中的静态方法"handleConsumePurchase"，传入当前Activity对象和产品ID对应的Java String对象
                        javaClass.CallStatic("handleConsumePurchase", productId);
                    }
                }
            }else{
                OnHandleConsumePurchaseSuccess(productId);
            }
        }

        // 此方法用于获取订阅产品的过期时间（以毫秒为单位），接受一个产品ID字符串作为参数，仅在安卓平台执行
        public static long GetSubscribeExpiresTimeInMillis(string productId)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    // 调用安卓类中的静态方法"getSubscribeExpiresTimeInMillis"，传入产品ID对应的Java String对象，并返回结果
                    return javaClass.CallStatic<long>("getSubscribeExpiresTimeInMillis", productId);
                }
            }
            return 0;
        }

        // 此方法用于获取产品的描述信息，接受一个产品ID字符串作为参数，仅在安卓平台执行
        public static string GetDescription(string productId)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    // 调用安卓类中的静态方法"getDescription"，传入产品ID对应的Java String对象，并返回结果
                    return javaClass.CallStatic<string>("getDescription", productId);
                }
            }
            return "";
        }

        // 此方法用于获取产品的本地价格，接受一个产品ID字符串和一个表示是否需要货币符号的布尔值作为参数，仅在安卓平台执行
        public static string GetLocalPrice(string productId, bool needSymbol)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    // 调用安卓类中的静态方法"getLocalPrice"，传入产品ID对应的Java String对象和是否需要货币符号的布尔值，并返回结果
                    return javaClass.CallStatic<string>("getLocalPrice", productId, needSymbol);
                }
            }
            return "";
        }

        // 此方法用于获取货币代码，仅在安卓平台执行
        public static string GetCurrencyCode()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    // 调用安卓类中的静态方法"getCurrencyCode"并返回结果
                    return javaClass.CallStatic<string>("getCurrencyCode");
                }
            }
            return "";
        }

        private static void OnQueryProductDetailsSuccess()
        {
            if(mQueryProductDetailsSuccessCallback != null)
            {
                mQueryProductDetailsSuccessCallback();
            }
            ResetQueryProductDetailsCallback();

        }

        private static void OnQueryProductDetailsFail(int errorCode)
        {
            if(mQueryProductDetailsFailCallback != null)
            {
                mQueryProductDetailsFailCallback(errorCode);
            }
            ResetQueryProductDetailsCallback();
        }

        private static void ResetQueryProductDetailsCallback()
        {
            mQueryProductDetailsSuccessCallback = null;
            mQueryProductDetailsFailCallback = null;
            GameSDKCallbackListener.OnQueryProductDetailsSuccessEvent -= OnQueryProductDetailsSuccess;
            GameSDKCallbackListener.OnQueryProductDetailsFailEvent -= OnQueryProductDetailsFail;
        }

        private static void OnHandlePurchaseSuccess(string productId, string orderId, string transactionId)
        {
            if(mPurchaseSuccessCallback != null)
            {
                mPurchaseSuccessCallback(productId, orderId, transactionId);
            }
            ResetPurchaseCallback();

        }

        private static void OnHandlePurchaseFail(string productId, int errorCode)
        {
            if(mPurchaseFailCallback != null)
            {
                mPurchaseFailCallback(productId, errorCode);
            }
            ResetPurchaseCallback();
        }

        private static void ResetPurchaseCallback()
        {
            mPurchaseSuccessCallback = null;
            mPurchaseFailCallback = null;
            GameSDKCallbackListener.OnPurchaseSuccessEvent -= OnHandlePurchaseSuccess;
            GameSDKCallbackListener.OnPurchaseFailEvent -= OnHandlePurchaseFail;
        }

        private static void OnHandleRestoreSuccess(string productId, string orderId, string transactionId)
        {
            if(mRestoreSuccessCallback != null)
            {
                mRestoreSuccessCallback(productId, orderId, transactionId);
            }
            ResetRestoreCallback();
        }

        private static void OnHandleRestoreFail(string productId, int errorCode)
        {
            if(mRestoreFailCallback != null)
            {
                mRestoreFailCallback(productId, errorCode);
            }
            ResetRestoreCallback();
        }

        private static void ResetRestoreCallback()
        {
            mRestoreSuccessCallback = null;
            mRestoreFailCallback = null;
            GameSDKCallbackListener.OnRestoreSuccessEvent -= OnHandleRestoreSuccess;
            GameSDKCallbackListener.OnRestoreFailEvent -= OnHandleRestoreFail;
        }

        private static void OnHandleConsumePurchaseSuccess(string productId)
        {
            if(mConsumeSuccessCallback != null)
            {
                mConsumeSuccessCallback(productId);
            }
            ResetConsumeCallback();
        }

        private static void OnHandleConsumePurchaseFail(string productId, int errorCode)
        {
            if(mConsumeFailCallback != null)
            {
                mConsumeFailCallback(productId, errorCode);
            }
            ResetConsumeCallback();
        }

        private static void ResetConsumeCallback()
        {
            mConsumeSuccessCallback = null;
            mConsumeFailCallback = null;
            GameSDKCallbackListener.OnConsumeSuccessEvent -= OnHandleConsumePurchaseSuccess;
            GameSDKCallbackListener.OnConsumeFailEvent -= OnHandleConsumePurchaseFail;
        }

        /// <summary>
        /// 设置为远端验证模式
        /// 必须在 InitBillingSDK 之后、Purchase 之前调用
        /// </summary>
        /// <param name="baseUrl">后端服务基础URL，如 "https://api.example.com"</param>
        /// <param name="appCode">应用代码</param>
        /// <param name="appSecret">应用密钥</param>
        public static void SetRemoteMode(string baseUrl, string appCode, string appSecret)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setRemoteMode", baseUrl, appCode, appSecret);
                }
            }
        }

        /// <summary>
        /// 设置为本地模式（默认模式）
        /// </summary>
        public static void SetLocalMode()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setLocalMode");
                }
            }
        }

        /// <summary>
        /// 获取当前Billing模式
        /// </summary>
        /// <returns>0: LOCAL, 1: REMOTE</returns>
        public static int GetBillingMode()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<int>("getBillingModeValue");
                }
            }
            return 0; // 默认LOCAL
        }
    }
}
