using System;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class AdSdk : MonoBehaviour
    {
        public const int FullAdFormatUnspecified = 0;
        public const int FullAdFormatInterstitial = 1;
        public const int FullAdFormatNative = 2;
        public const int RewardedAdFormatUnspecified = 0;
        public const int RewardedAdFormatInterstitial = 1;
        public const int RewardedAdFormatNative = 2;
        public const int RewardedAdFormatRewarded = 3;

        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.ad.AdSdk";

        private static GameSdkNoneParamVoidCallback mAdInitializedCallback;
        private static GameSdkAdPaidCallback        mAdPaidCallback;
        private static GameSdkNoneParamVoidCallback mRewardedAdLoadSuccessCallback;
        private static GameSdkIntParamVoidCallback  mRewardedAdLoadFailCallback;
        private static GameSdkNoneParamVoidCallback mRewardedAdViewedCallback;
        private static GameSdkNoneParamVoidCallback mRewardedAdCanceledCallback;
        private static GameSdkStringParamVoidCallback mRewardedAdClickedCallback;
        private static GameSdkStringParamVoidCallback mRewardedAdImpressionCallback;
        private static GameSdkNoneParamVoidCallback mFullAdLoadSuccessCallback;
        private static GameSdkIntParamVoidCallback  mFullAdLoadFailCallback;
        private static GameSdkIntParamVoidCallback  mFullAdClosedCallback;
        private static GameSdkStringParamVoidCallback mFullAdClickedCallback;
        private static GameSdkStringParamVoidCallback mFullAdImpressionCallback;
        private static GameSdkNoneParamVoidCallback mOpenAdLoadSuccessCallback;
        private static GameSdkIntParamVoidCallback  mOpenAdLoadFailCallback;
        private static GameSdkNoneParamVoidCallback mOpenAdClosedCallback;
        private static GameSdkNoneParamVoidCallback mBannerAdLoadSuccessCallback;
        private static GameSdkIntParamVoidCallback  mBannerAdLoadFailCallback;
        private static GameSdkNoneParamVoidCallback mBannerAdShownCallback;
        private static GameSdkStringParamVoidCallback mBannerAdClickedCallback;
        private static GameSdkStringParamVoidCallback mBannerAdImpressionCallback;

        #region Init Ad SDK
        public static void InitAdSDK(string rewardedAdId, string fullAdId, string bannerAdId, string openAdId, bool isDebug, string[] testSignatureSHA1List)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        if (testSignatureSHA1List == null)
                        {
                            testSignatureSHA1List = new string[0];
                        }

                        javaClass.CallStatic("init",
                            currentActivity,
                            rewardedAdId,
                            fullAdId,
                            bannerAdId,
                            openAdId,
                            isDebug,
                            testSignatureSHA1List
                        );
                    }
                }
            }
        }

        public static void InitAdSDK(string rewardedAdId, string fullAdId, string bannerAdId, string openAdId, bool isDebug, string applovinSdkKey)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        javaClass.CallStatic("init",
                            currentActivity,
                            rewardedAdId,
                            fullAdId,
                            bannerAdId,
                            openAdId,
                            isDebug,
                            applovinSdkKey
                        );
                    }
                }
            }
        }

        public static void InitAdSDK(string rewardedAdGuaranteedIdHigh, string rewardedAdGuaranteedIdLow, string fullAdGuaranteedIdHigh, string fullAdGuaranteedIdLow, string bannerAdId, string[] testSignatureSHA1List)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        // 处理空数组情况
                        if (testSignatureSHA1List == null)
                        {
                            testSignatureSHA1List = new string[0];
                        }

                        // 直接将C#字符串数组传递给Java方法
                        // Unity会自动将C#字符串数组转换为Java字符串数组
                        javaClass.CallStatic("init",
                            currentActivity,
                            rewardedAdGuaranteedIdHigh,
                            rewardedAdGuaranteedIdLow,
                            fullAdGuaranteedIdHigh,
                            fullAdGuaranteedIdLow,
                            bannerAdId,
                            testSignatureSHA1List
                        );
                    }
                }
            }
        }

        public static string GetSignatureSHA1()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<string>("getSignatureSHA1");
                }
            }
            return "";
        }

        public static void InitAds(GameSdkNoneParamVoidCallback adInitializedCallback = null)
        {
            mAdInitializedCallback = adInitializedCallback;
            GameSDKCallbackListener.OnAdInitializedEvent += OnAdInitialized;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("initAds");
                }
            }else{
                OnAdInitialized();
            }
        }

        public static bool HasInitedAds()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("hasInitedAds");
                }
            }
            return false;
            
        }

        public static void SetAdMuted(bool muted)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setAdMuted", muted);
                }
            }
        }

        public static void SetRemoveAdMode(bool needRemoveAd)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setRemoveAdMode", needRemoveAd);
                }
            }
        }

        public static bool GetRemoveAdMode()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("getRemoveAdMode");
                }
            }
                return false;
        }

        private static void OnAdInitialized()
        {
            if (mAdInitializedCallback != null)
            {
                mAdInitializedCallback();
                mAdInitializedCallback = null;
            }
            GameSDKCallbackListener.OnAdInitializedEvent -= OnAdInitialized;
        }

        public static void SetAdPaidCallback(GameSdkAdPaidCallback adPaidCallback)
        {
            mAdPaidCallback = adPaidCallback;
            GameSDKCallbackListener.OnAdPaidEvent += onPaidEvent;
        }

        private static void onPaidEvent(string adPlatform, string adId, string adType, string adTypeDisplayed, string adNetwork, double adMoney, string currency)
        {
            if (mAdPaidCallback != null)
            {
                mAdPaidCallback(adPlatform, adId, adType, adTypeDisplayed, adNetwork, adMoney, currency);
            }
        }

        #endregion

        #region RewardAd Functions
        /// <summary>
        /// 激励只能加载一次。SDK 的加载成功/失败回调没有作用。
        /// </summary>
        public static void LoadRewardAd(GameSdkNoneParamVoidCallback rewardedAdLoadCallback = null, GameSdkIntParamVoidCallback rewardedAdLoadFailCallback = null)
        {
            mRewardedAdLoadSuccessCallback = rewardedAdLoadCallback;
            mRewardedAdLoadFailCallback = rewardedAdLoadFailCallback;
            GameSDKCallbackListener.OnRewardAdLoadSuccessEvent += OnRewardAdLoaded;
            GameSDKCallbackListener.OnRewardAdLoadFailEvent += OnRewardAdLoadFailed;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("loadRewardAd");
                }
            }else{
                OnRewardAdLoaded();
            }
        }

        public static bool IsRewardAdLoaded()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("isRewardAdLoaded");
                }
            }
            else
            {
                return false;
            }
        }

        public static bool IsRewardAdLoaded(int adFormat)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("isRewardAdLoaded", adFormat);
                }
            }
            else
            {
                return false;
            }
        }

        public static void ShowRewardAd(GameSdkNoneParamVoidCallback rewardedAdViewedCallback = null, GameSdkNoneParamVoidCallback rewardedAdCanceledCallback = null, GameSdkStringParamVoidCallback rewardedAdClickedCallback = null, GameSdkStringParamVoidCallback rewardedAdImpressionCallback = null)
        {
            mRewardedAdViewedCallback = rewardedAdViewedCallback;
            mRewardedAdCanceledCallback = rewardedAdCanceledCallback;
            mRewardedAdClickedCallback = rewardedAdClickedCallback;
            mRewardedAdImpressionCallback = rewardedAdImpressionCallback;
            GameSDKCallbackListener.OnRewardAdViewedEvent += OnRewardAdViewed;
            GameSDKCallbackListener.OnRewardAdCanceledEvent += OnRewardAdCanceled;
            GameSDKCallbackListener.OnRewardAdClickedEvent += OnRewardAdClicked;
            GameSDKCallbackListener.OnRewardAdImpressionEvent += OnRewardAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showRewardAd");
                }
            }else{
                OnRewardAdViewed();
            }
        }

        public static void ShowRewardAd(string scene, string action, string paramsJson, GameSdkNoneParamVoidCallback rewardedAdViewedCallback = null, GameSdkNoneParamVoidCallback rewardedAdCanceledCallback = null, GameSdkStringParamVoidCallback rewardedAdClickedCallback = null, GameSdkStringParamVoidCallback rewardedAdImpressionCallback = null)
        {
            mRewardedAdViewedCallback = rewardedAdViewedCallback;
            mRewardedAdCanceledCallback = rewardedAdCanceledCallback;
            mRewardedAdClickedCallback = rewardedAdClickedCallback;
            mRewardedAdImpressionCallback = rewardedAdImpressionCallback;
            GameSDKCallbackListener.OnRewardAdViewedEvent += OnRewardAdViewed;
            GameSDKCallbackListener.OnRewardAdCanceledEvent += OnRewardAdCanceled;
            GameSDKCallbackListener.OnRewardAdClickedEvent += OnRewardAdClicked;
            GameSDKCallbackListener.OnRewardAdImpressionEvent += OnRewardAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showRewardAd", scene, action, paramsJson);
                }
            }else{
                OnRewardAdViewed();
            }
        }

        public static void ShowRewardAd(string scene, string action, string paramsJson, int primaryAdFormat, bool forcePrimaryAdFormat, GameSdkNoneParamVoidCallback rewardedAdViewedCallback = null, GameSdkNoneParamVoidCallback rewardedAdCanceledCallback = null, GameSdkStringParamVoidCallback rewardedAdClickedCallback = null, GameSdkStringParamVoidCallback rewardedAdImpressionCallback = null)
        {
            mRewardedAdViewedCallback = rewardedAdViewedCallback;
            mRewardedAdCanceledCallback = rewardedAdCanceledCallback;
            mRewardedAdClickedCallback = rewardedAdClickedCallback;
            mRewardedAdImpressionCallback = rewardedAdImpressionCallback;
            GameSDKCallbackListener.OnRewardAdViewedEvent += OnRewardAdViewed;
            GameSDKCallbackListener.OnRewardAdCanceledEvent += OnRewardAdCanceled;
            GameSDKCallbackListener.OnRewardAdClickedEvent += OnRewardAdClicked;
            GameSDKCallbackListener.OnRewardAdImpressionEvent += OnRewardAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showRewardAd", scene, action, paramsJson, primaryAdFormat, forcePrimaryAdFormat);
                }
            }else{
                OnRewardAdViewed();
            }
        }

        public static void ShowRewardAd(string scene, string action, int level, string paramsJson, GameSdkNoneParamVoidCallback rewardedAdViewedCallback = null, GameSdkNoneParamVoidCallback rewardedAdCanceledCallback = null, GameSdkStringParamVoidCallback rewardedAdClickedCallback = null, GameSdkStringParamVoidCallback rewardedAdImpressionCallback = null)
        {
            mRewardedAdViewedCallback = rewardedAdViewedCallback;
            mRewardedAdCanceledCallback = rewardedAdCanceledCallback;
            mRewardedAdClickedCallback = rewardedAdClickedCallback;
            mRewardedAdImpressionCallback = rewardedAdImpressionCallback;
            GameSDKCallbackListener.OnRewardAdViewedEvent += OnRewardAdViewed;
            GameSDKCallbackListener.OnRewardAdCanceledEvent += OnRewardAdCanceled;
            GameSDKCallbackListener.OnRewardAdClickedEvent += OnRewardAdClicked;
            GameSDKCallbackListener.OnRewardAdImpressionEvent += OnRewardAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showRewardAd", scene, action, level, paramsJson);
                }
            }else{
                OnRewardAdViewed();
            }
        }

        public static void ShowRewardAd(string scene, string action, int level, string rewardType, int rewardAmount, string paramsJson, GameSdkNoneParamVoidCallback rewardedAdViewedCallback = null, GameSdkNoneParamVoidCallback rewardedAdCanceledCallback = null, GameSdkStringParamVoidCallback rewardedAdClickedCallback = null, GameSdkStringParamVoidCallback rewardedAdImpressionCallback = null)
        {
            mRewardedAdViewedCallback = rewardedAdViewedCallback;
            mRewardedAdCanceledCallback = rewardedAdCanceledCallback;
            mRewardedAdClickedCallback = rewardedAdClickedCallback;
            mRewardedAdImpressionCallback = rewardedAdImpressionCallback;
            GameSDKCallbackListener.OnRewardAdViewedEvent += OnRewardAdViewed;
            GameSDKCallbackListener.OnRewardAdCanceledEvent += OnRewardAdCanceled;
            GameSDKCallbackListener.OnRewardAdClickedEvent += OnRewardAdClicked;
            GameSDKCallbackListener.OnRewardAdImpressionEvent += OnRewardAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showRewardAd", scene, action, level, rewardType, rewardAmount, paramsJson);
                }
            }else{
                OnRewardAdViewed();
            }
        }

        public static void ShowRewardAd(string scene, string action, string rewardType, int rewardAmount, string paramsJson, int primaryAdFormat, bool forcePrimaryAdFormat, GameSdkNoneParamVoidCallback rewardedAdViewedCallback = null, GameSdkNoneParamVoidCallback rewardedAdCanceledCallback = null, GameSdkStringParamVoidCallback rewardedAdClickedCallback = null, GameSdkStringParamVoidCallback rewardedAdImpressionCallback = null)
        {
            ShowRewardAd(scene, action, 0, rewardType, rewardAmount, paramsJson, primaryAdFormat, forcePrimaryAdFormat, rewardedAdViewedCallback, rewardedAdCanceledCallback, rewardedAdClickedCallback, rewardedAdImpressionCallback);
        }

        public static void ShowRewardAd(string scene, string action, int level, string rewardType, int rewardAmount, string paramsJson, int primaryAdFormat, bool forcePrimaryAdFormat, GameSdkNoneParamVoidCallback rewardedAdViewedCallback = null, GameSdkNoneParamVoidCallback rewardedAdCanceledCallback = null, GameSdkStringParamVoidCallback rewardedAdClickedCallback = null, GameSdkStringParamVoidCallback rewardedAdImpressionCallback = null)
        {
            mRewardedAdViewedCallback = rewardedAdViewedCallback;
            mRewardedAdCanceledCallback = rewardedAdCanceledCallback;
            mRewardedAdClickedCallback = rewardedAdClickedCallback;
            mRewardedAdImpressionCallback = rewardedAdImpressionCallback;
            GameSDKCallbackListener.OnRewardAdViewedEvent += OnRewardAdViewed;
            GameSDKCallbackListener.OnRewardAdCanceledEvent += OnRewardAdCanceled;
            GameSDKCallbackListener.OnRewardAdClickedEvent += OnRewardAdClicked;
            GameSDKCallbackListener.OnRewardAdImpressionEvent += OnRewardAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showRewardAd", scene, action, level, rewardType, rewardAmount, paramsJson, primaryAdFormat, forcePrimaryAdFormat);
                }
            }else{
                OnRewardAdViewed();
            }
        }

        private static void OnRewardAdLoaded()
        {
            if(mRewardedAdLoadSuccessCallback != null)
            {
                mRewardedAdLoadSuccessCallback();
            }
            ResetRewardedAdLoadCallbacks();
        }

        private static void OnRewardAdLoadFailed(int errorCode)
        {
            if (mRewardedAdLoadFailCallback != null)
            {
                mRewardedAdLoadFailCallback(errorCode);
            }
            ResetRewardedAdLoadCallbacks();
        }

        private static void ResetRewardedAdLoadCallbacks()
        {
            mRewardedAdLoadSuccessCallback = null;
            mRewardedAdLoadFailCallback = null;
            GameSDKCallbackListener.OnRewardAdLoadSuccessEvent -= OnRewardAdLoaded;
            GameSDKCallbackListener.OnRewardAdLoadFailEvent -= OnRewardAdLoadFailed;
        }

        private static void OnRewardAdViewed()
        {
            if (mRewardedAdViewedCallback != null)
            {
                mRewardedAdViewedCallback();
            }
            ResetRewardedAdShowCallbacks();
        }

        private static void OnRewardAdCanceled()
        {
            if (mRewardedAdCanceledCallback != null)
            {
                mRewardedAdCanceledCallback();
            }
            ResetRewardedAdShowCallbacks();
        }

        private static void OnRewardAdClicked(string adSource)
        {
            if (mRewardedAdClickedCallback != null)
            {
                mRewardedAdClickedCallback(adSource);
                mRewardedAdClickedCallback = null;
            }
            GameSDKCallbackListener.OnRewardAdClickedEvent -= OnRewardAdClicked;
        }

        private static void OnRewardAdImpression(string adSource)
        {
            if (mRewardedAdImpressionCallback != null)
            {
                mRewardedAdImpressionCallback(adSource);
                mRewardedAdImpressionCallback = null;
            }
            GameSDKCallbackListener.OnRewardAdImpressionEvent -= OnRewardAdImpression;
        }


        private static void ResetRewardedAdShowCallbacks()
        {
            mRewardedAdViewedCallback = null;
            mRewardedAdCanceledCallback = null;
            GameSDKCallbackListener.OnRewardAdViewedEvent -= OnRewardAdViewed;
            GameSDKCallbackListener.OnRewardAdCanceledEvent -= OnRewardAdCanceled;
        }
        #endregion

        #region FullAd Functions

        /// <summary>
        /// 插屏只能加载一次。SDK 的加载成功/失败回调没有作用。
        /// </summary>
        public static void LoadFullAd(GameSdkNoneParamVoidCallback fullAdLoadSuccessCallback = null, GameSdkIntParamVoidCallback fullAdLoadFailCallback = null)
        {
            mFullAdLoadSuccessCallback = fullAdLoadSuccessCallback;
            mFullAdLoadFailCallback = fullAdLoadFailCallback;
            GameSDKCallbackListener.OnFullAdLoadSuccessEvent += OnFullAdLoaded;
            GameSDKCallbackListener.OnFullAdLoadFailEvent += OnFullAdLoadFailed;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {         
                    javaClass.CallStatic("loadFullAd");
                }
            }else{
                OnFullAdLoaded();
            }
        }

        public static bool IsFullAdLoaded()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("isFullAdLoaded");
                }
            }
                return false;
        }

        public static bool IsFullAdLoaded(int adFormat)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("isFullAdLoaded", adFormat);
                }
            }
            return false;
        }

        public static void ShowFullAd(GameSdkIntParamVoidCallback fullAdClosedCallback = null, GameSdkStringParamVoidCallback fullAdClickedCallback = null, GameSdkStringParamVoidCallback fullAdImpressionCallback = null)
        {
            mFullAdClosedCallback = fullAdClosedCallback;
            mFullAdClickedCallback = fullAdClickedCallback;
            mFullAdImpressionCallback = fullAdImpressionCallback;
            GameSDKCallbackListener.OnFullAdClosedEvent += OnFullAdClosed;
            GameSDKCallbackListener.OnFullAdClickedEvent += OnFullAdClicked;
            GameSDKCallbackListener.OnFullAdImpressionEvent += OnFullAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showFullAd");
                }
            }else{
                OnFullAdClosed(0);
            }
        }

        public static void ShowFullAd(string scene, string action, string paramsJson, GameSdkIntParamVoidCallback fullAdClosedCallback = null, GameSdkStringParamVoidCallback fullAdClickedCallback = null, GameSdkStringParamVoidCallback fullAdImpressionCallback = null)
        {
            mFullAdClosedCallback = fullAdClosedCallback;
            mFullAdClickedCallback = fullAdClickedCallback;
            mFullAdImpressionCallback = fullAdImpressionCallback;
            GameSDKCallbackListener.OnFullAdClosedEvent += OnFullAdClosed;
            GameSDKCallbackListener.OnFullAdClickedEvent += OnFullAdClicked;
            GameSDKCallbackListener.OnFullAdImpressionEvent += OnFullAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showFullAd", scene, action, paramsJson);
                }
            }else{
                OnFullAdClosed(0);
            }
        }

        public static void ShowFullAd(string scene, string action, string paramsJson, int primaryAdFormat, bool forcePrimaryAdFormat, GameSdkIntParamVoidCallback fullAdClosedCallback = null, GameSdkStringParamVoidCallback fullAdClickedCallback = null, GameSdkStringParamVoidCallback fullAdImpressionCallback = null)
        {
            mFullAdClosedCallback = fullAdClosedCallback;
            mFullAdClickedCallback = fullAdClickedCallback;
            mFullAdImpressionCallback = fullAdImpressionCallback;
            GameSDKCallbackListener.OnFullAdClosedEvent += OnFullAdClosed;
            GameSDKCallbackListener.OnFullAdClickedEvent += OnFullAdClicked;
            GameSDKCallbackListener.OnFullAdImpressionEvent += OnFullAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showFullAd", scene, action, paramsJson, primaryAdFormat, forcePrimaryAdFormat);
                }
            }else{
                OnFullAdClosed(0);
            }
        }

        public static void ShowFullAd(string scene, string action, int level, string paramsJson, GameSdkIntParamVoidCallback fullAdClosedCallback = null, GameSdkStringParamVoidCallback fullAdClickedCallback = null, GameSdkStringParamVoidCallback fullAdImpressionCallback = null)
        {
            mFullAdClosedCallback = fullAdClosedCallback;
            mFullAdClickedCallback = fullAdClickedCallback;
            mFullAdImpressionCallback = fullAdImpressionCallback;
            GameSDKCallbackListener.OnFullAdClosedEvent += OnFullAdClosed;
            GameSDKCallbackListener.OnFullAdClickedEvent += OnFullAdClicked;
            GameSDKCallbackListener.OnFullAdImpressionEvent += OnFullAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showFullAd", scene, action, level, paramsJson);
                }
            }else{
                OnFullAdClosed(0);
            }
        }

        private static void OnFullAdLoaded()
        {
            if(mFullAdLoadSuccessCallback != null)
            {
                mFullAdLoadSuccessCallback();
            }
            ResetFullAdLoadCallbacks();
        }

        private static void OnFullAdLoadFailed(int errorCode)
        {
            if(mFullAdLoadFailCallback != null)
            {
                mFullAdLoadFailCallback(errorCode);
            }
            ResetFullAdLoadCallbacks();
        }

        private static void ResetFullAdLoadCallbacks()
        {
            mFullAdLoadSuccessCallback = null;
            mFullAdLoadFailCallback = null;
            GameSDKCallbackListener.OnFullAdLoadSuccessEvent -= OnFullAdLoaded;
            GameSDKCallbackListener.OnFullAdLoadFailEvent -= OnFullAdLoadFailed;
        }

        private static void OnFullAdClosed(int errorCode)
        {
            if(mFullAdClosedCallback != null)
            {
                mFullAdClosedCallback(errorCode);
                mFullAdClosedCallback = null;
            }
            GameSDKCallbackListener.OnFullAdClosedEvent -= OnFullAdClosed;

        }

        private static void OnFullAdClicked(string adSource)
        {
            if (mFullAdClickedCallback != null)
            {
                mFullAdClickedCallback(adSource);
                mFullAdClickedCallback = null;
            }
            GameSDKCallbackListener.OnFullAdClickedEvent -= OnFullAdClicked;
        }

        private static void OnFullAdImpression(string adSource)
        {
            if (mFullAdImpressionCallback != null)
            {
                mFullAdImpressionCallback(adSource);
                mFullAdImpressionCallback = null;
            }
            GameSDKCallbackListener.OnFullAdImpressionEvent -= OnFullAdImpression;
        }
        #endregion

        #region BannerAd Functions
        /// <summary>
        /// Banner 只能加载一次，且已由 InitAds 内部加载。不要再调用。SDK 的加载成功/失败回调没有作用。
        /// </summary>
        public static void LoadBannerAd(GameSdkNoneParamVoidCallback bannerAdLoadSuccessCallback = null, GameSdkIntParamVoidCallback bannerAdLoadFailCallback = null)
        {
            mBannerAdLoadSuccessCallback = bannerAdLoadSuccessCallback;
            mBannerAdLoadFailCallback = bannerAdLoadFailCallback;
            GameSDKCallbackListener.OnBannerAdLoadSuccessEvent += OnBannerAdLoaded;
            GameSDKCallbackListener.OnBannerAdLoadFailEvent += OnBannerAdLoadFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("loadBannerAd");
                }
            }else{
                OnBannerAdLoaded();
            }
        }

        public static bool IsBannerLoaded()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("isBannerLoaded");
                }
            }
            else
            {
                return false;
            }
        }

        public static void ShowBannerAd(bool isVisible, GameSdkStringParamVoidCallback bannerAdClickedCallback = null, GameSdkNoneParamVoidCallback bannerAdShownCallback = null, GameSdkStringParamVoidCallback bannerAdImpressionCallback = null)
        {
            mBannerAdClickedCallback = bannerAdClickedCallback;
            mBannerAdShownCallback = bannerAdShownCallback;
            mBannerAdImpressionCallback = bannerAdImpressionCallback;
            GameSDKCallbackListener.OnBannerAdClickedEvent += OnBannerAdClicked;
            GameSDKCallbackListener.OnBannerAdShownEvent += OnBannerAdShown;
            GameSDKCallbackListener.OnBannerAdImpressionEvent += OnBannerAdImpression;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showBannerAd", isVisible);
                }
            }else{
                OnBannerAdShown();
            }
        }

        public static bool IsBannerAdVisible()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("isBannerAdVisible");
                }
            }
            else
            {
                return false;
            }
        }

        public static void RemoveBannerAd()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("removeBannerAd");
                }
            }
        }

        public static float GetBannerAdHeight()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<float>("getBannerAdHeight");
                }
            }
            else
            {
                return 0;
            }
        }

        public static void SetWinWidth(int width)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setWinWidth", width);
                }
            }
        }

        public static float GetBannerAdHeightWithScale()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<float>("getBannerAdHeightWithScale");
                }
            }
            else
            {
                return 0.0f;
            }
        }

        private static void OnBannerAdLoaded()
        {
            if(mBannerAdLoadSuccessCallback != null)
            {
                mBannerAdLoadSuccessCallback();
            }
        }

        private static void OnBannerAdLoadFailed(int errorCode)
        {
            if(mBannerAdLoadFailCallback != null)
            {
                mBannerAdLoadFailCallback(errorCode);
            }
        }

        private static void OnBannerAdClicked(string adSource)
        {
            if(mBannerAdClickedCallback != null)
            {
                mBannerAdClickedCallback(adSource);
                mBannerAdClickedCallback = null;
            }
            GameSDKCallbackListener.OnBannerAdClickedEvent -= OnBannerAdClicked;
        }

        private static void OnBannerAdShown()
        {
            if(mBannerAdShownCallback != null)
            {
                mBannerAdShownCallback();
            }
        }

        private static void OnBannerAdImpression(string adSource)
        {
            if(mBannerAdImpressionCallback != null)
            {
                mBannerAdImpressionCallback(adSource);
                mBannerAdImpressionCallback = null;
            }
            GameSDKCallbackListener.OnBannerAdImpressionEvent -= OnBannerAdImpression;
        }

        #endregion

        #region OpenAd Functions
        public static void LoadOpenAd(GameSdkNoneParamVoidCallback openAdLoadCallback = null, GameSdkIntParamVoidCallback openAdLoadFailCallback = null)
        {
            mOpenAdLoadSuccessCallback = openAdLoadCallback;
            mOpenAdLoadFailCallback = openAdLoadFailCallback;
            GameSDKCallbackListener.OnOpenAdLoadSuccessEvent += OnOpenAdLoaded;
            GameSDKCallbackListener.OnOpenAdLoadFailEvent += OnOpenAdLoadFailed;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("loadOpenAd");
                }
            }else{
                OnOpenAdLoaded();
            }
        }

        public static bool IsOpenAdLoaded()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("isOpenAdLoaded");
                }
            }
            return false;
        }

        public static void ShowOpenAd(GameSdkNoneParamVoidCallback openAdClosedCallback = null)
        {
            mOpenAdClosedCallback = openAdClosedCallback;
            GameSDKCallbackListener.OnOpenAdClosedEvent += OnOpenAdClosed;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showOpenAd");
                }
            }else{
                OnOpenAdClosed();
            }
        }

        private static void OnOpenAdLoaded()
        {
            if (mOpenAdLoadSuccessCallback != null)
            {
                mOpenAdLoadSuccessCallback();
            }
            ResetOpenAdLoadCallbacks();
        }


        //
        private static void OnOpenAdLoadFailed(int errorCode)
        {
            if (mOpenAdLoadFailCallback != null)
            {
                mOpenAdLoadFailCallback(errorCode);
            }
            ResetOpenAdLoadCallbacks();
        }

        private static void ResetOpenAdLoadCallbacks()
        {
            mOpenAdLoadSuccessCallback = null;
            mOpenAdLoadFailCallback = null;
            GameSDKCallbackListener.OnOpenAdLoadSuccessEvent -= OnOpenAdLoaded;
            GameSDKCallbackListener.OnOpenAdLoadFailEvent -= OnOpenAdLoadFailed;
        }

        private static void OnOpenAdClosed()
        {
            if (mOpenAdClosedCallback != null)
            {
                mOpenAdClosedCallback();
            }
            mOpenAdClosedCallback = null;
            GameSDKCallbackListener.OnOpenAdClosedEvent -= OnOpenAdClosed;
        }
        #endregion
    }
}
