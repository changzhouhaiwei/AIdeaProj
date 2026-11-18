using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class UserProfileSdk : MonoBehaviour
    {
        private const string androidClassPath = "com.zzxz.gamesdk.core.userprofile.UserProfileSdk";
        private static GameSdkUserProfileScoreParamVoidCallback queryUserProfileScoreSuccessCallback;
        private static GameSdkIntParamVoidCallback queryUserProfileScoreFailedCallback;

        public static void ReportTutorialComplete()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportTutorialComplete");
                }
            }
        }

        public static void ReportLevelStart(int level)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportLevelStart", level);
                }
            }
        }

        public static void ReportLevelComplete(int level, int levelRetryCnt, bool usedAdRevive)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportLevelComplete", level, levelRetryCnt, usedAdRevive);
                }
            }
        }

        public static void ReportLevelFail(int level, int levelFailCnt, int consecutiveFail, int remainingTargets)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportLevelFail", level, levelFailCnt, consecutiveFail, remainingTargets);
                }
            }
        }

        public static void ReportIapPurchase(string productId, double payAmount, string currency, string productType, bool isFirstPurchase)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportIapPurchase", productId, payAmount, currency, productType, isFirstPurchase);
                }
            }
        }

        public static void ReportItemUse(string itemType, int itemCount, int level, string result)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportItemUse", itemType, itemCount, level, result);
                }
            }
        }

        public static void ReportItemAcquire(string itemType, int itemCount, int level, string acquireMethod)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportItemAcquire", itemType, itemCount, level, acquireMethod);
                }
            }
        }

        public static void ReportShopClick()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportShopClick");
                }
            }
        }

        public static void ReportShopLeave(int dwellTimeSec)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportShopLeave", dwellTimeSec);
                }
            }
        }

        public static void ReportPopupAction(string popupId, string action)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("reportPopupAction", popupId, action);
                }
            }
        }

        public static void QueryUserProfileScore(GameSdkUserProfileScoreParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback)
        {
            if (queryUserProfileScoreSuccessCallback != null || queryUserProfileScoreFailedCallback != null)
            {
                failedCallback?.Invoke(-1);
                return;
            }

            queryUserProfileScoreSuccessCallback = successCallback;
            queryUserProfileScoreFailedCallback = failedCallback;
            GameSDKCallbackListener.OnQueryUserProfileScoreSuccessEvent += OnQueryUserProfileScoreSuccess;
            GameSDKCallbackListener.OnQueryUserProfileScoreFailEvent += OnQueryUserProfileScoreFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("queryUserProfileScore");
                }
            }
            else
            {
                OnQueryUserProfileScoreFailed(-1);
            }
        }

        public static double GetAdScore()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<double>("getAdScore");
                }
            }
            return 0.0;
        }

        public static double GetPayScore()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<double>("getPayScore");
                }
            }
            return 0.0;
        }

        public static string GetWindow()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<string>("getWindow");
                }
            }
            return string.Empty;
        }

        private static void OnQueryUserProfileScoreSuccess(double adScore, double payScore, string window)
        {
            var callback = queryUserProfileScoreSuccessCallback;
            ClearQueryUserProfileScoreCallbacks();
            callback?.Invoke(adScore, payScore, window);
        }

        private static void OnQueryUserProfileScoreFailed(int responseCode)
        {
            var callback = queryUserProfileScoreFailedCallback;
            ClearQueryUserProfileScoreCallbacks();
            callback?.Invoke(responseCode);
        }

        private static void ClearQueryUserProfileScoreCallbacks()
        {
            GameSDKCallbackListener.OnQueryUserProfileScoreSuccessEvent -= OnQueryUserProfileScoreSuccess;
            GameSDKCallbackListener.OnQueryUserProfileScoreFailEvent -= OnQueryUserProfileScoreFailed;
            queryUserProfileScoreSuccessCallback = null;
            queryUserProfileScoreFailedCallback = null;
        }
    }
}
