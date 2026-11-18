using System;
using UnityEngine;

#if UNITY_ANDROID
using AndroidCallbackListener = EwingSoft.GameSDK.GameSDKCallbackListener;
#endif

namespace EwingSoft.GameSDK
{
    /// <summary>
    /// Android 回调入口。业务只订这里的事件。
    /// 当前覆盖广告与 CMP。支付、Firebase、RemoteData、登录等仍直接订 GameSDKCallbackListener。
    /// </summary>
    public static class GameSDKCallbackHub
    {
        private static bool isInitialized;

        public static event Action OnAdInitialized;
        public static event Action OnRewardAdLoadSuccess;
        public static event Action<int> OnRewardAdLoadFail;
        public static event Action OnRewardAdViewed;
        public static event Action OnRewardAdCanceled;
        public static event Action OnFullAdLoadSuccess;
        public static event Action<int> OnFullAdLoadFail;
        public static event Action<int> OnFullAdClosed;
        public static event Action OnBannerAdLoadSuccess;
        public static event Action<int> OnBannerAdLoadFail;
        public static event Action<bool, int> OnCMPFinished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

#if UNITY_ANDROID
            SubscribeAndroidListener();
#endif

            isInitialized = true;
        }

#if UNITY_ANDROID
        private static void SubscribeAndroidListener()
        {
            AndroidCallbackListener.OnAdInitializedEvent += HandleAdInitialized;
            AndroidCallbackListener.OnRewardAdLoadSuccessEvent += HandleRewardAdLoadSuccess;
            AndroidCallbackListener.OnRewardAdLoadFailEvent += HandleRewardAdLoadFail;
            AndroidCallbackListener.OnRewardAdViewedEvent += HandleRewardAdViewed;
            AndroidCallbackListener.OnRewardAdCanceledEvent += HandleRewardAdCanceled;
            AndroidCallbackListener.OnFullAdLoadSuccessEvent += HandleFullAdLoadSuccess;
            AndroidCallbackListener.OnFullAdLoadFailEvent += HandleFullAdLoadFail;
            AndroidCallbackListener.OnFullAdClosedEvent += HandleFullAdClosed;
            AndroidCallbackListener.OnBannerAdLoadSuccessEvent += HandleBannerAdLoadSuccess;
            AndroidCallbackListener.OnBannerAdLoadFailEvent += HandleBannerAdLoadFail;
            AndroidCallbackListener.OnCMPFinishedEvent += HandleCMPFinished;
        }
#endif

        private static void HandleAdInitialized()
        {
            OnAdInitialized?.Invoke();
        }

        private static void HandleRewardAdLoadSuccess()
        {
            OnRewardAdLoadSuccess?.Invoke();
        }

        private static void HandleRewardAdLoadFail(int errorCode)
        {
            OnRewardAdLoadFail?.Invoke(errorCode);
        }

        private static void HandleRewardAdViewed()
        {
            OnRewardAdViewed?.Invoke();
        }

        private static void HandleRewardAdCanceled()
        {
            OnRewardAdCanceled?.Invoke();
        }

        private static void HandleFullAdLoadSuccess()
        {
            OnFullAdLoadSuccess?.Invoke();
        }

        private static void HandleFullAdLoadFail(int errorCode)
        {
            OnFullAdLoadFail?.Invoke(errorCode);
        }

        private static void HandleFullAdClosed(int errorCode)
        {
            OnFullAdClosed?.Invoke(errorCode);
        }

        private static void HandleBannerAdLoadSuccess()
        {
            OnBannerAdLoadSuccess?.Invoke();
        }

        private static void HandleBannerAdLoadFail(int errorCode)
        {
            OnBannerAdLoadFail?.Invoke(errorCode);
        }

        private static void HandleCMPFinished(bool success, int errorCode)
        {
            OnCMPFinished?.Invoke(success, errorCode);
        }
    }
}
