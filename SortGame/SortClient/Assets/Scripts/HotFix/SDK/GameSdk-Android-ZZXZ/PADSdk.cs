using System;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class PADSdk : MonoBehaviour
    {

        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.pad.PADSdk";

        private static GameSdkPADAssetStatusUpdatedCallback mAssetStatusUpdatedCallback;
        private static GameSdkStringParamVoidCallback mGetAssetStatusFailedCallback;
        private static GameSdkStringStringParamVoidCallback mGetAssetPathSuccessCallback;
        private static GameSdkStringParamVoidCallback mGetAssetPathFailedCallback;

        public static void InitPADSdk()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (var javaPADSdk = new AndroidJavaClass(androidClassPath))
                    {
                        javaPADSdk.CallStatic("init", currentActivity);
                    }
                }
            }
        }

        public static void GetAssetStatus(string assetName, GameSdkStringParamVoidCallback callback)
        {
            mGetAssetStatusFailedCallback = callback;
            GameSDKCallbackListener.OnGetAssetStatusFailedEvent += OnGetAssetStatusFailed;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaPADSdk = new AndroidJavaClass(androidClassPath))
                {
                    javaPADSdk.CallStatic("getAssetStatus", assetName);
                }
            }
            else
            {
                OnGetAssetStatusFailed(assetName);
            }
        }

        public static void FetchAssets(string assetName)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaPADSdk = new AndroidJavaClass(androidClassPath))
                {
                    javaPADSdk.CallStatic("fetchAssets", assetName);
                }
            }
        }

        public static void GetAssetPath(string assetName, int assetType, GameSdkStringStringParamVoidCallback successCallback, GameSdkStringParamVoidCallback failedCallback)
        {
            mGetAssetPathSuccessCallback = successCallback;
            mGetAssetPathFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetAssetPathSuccessEvent += OnGetAssetPathSuccess;
            GameSDKCallbackListener.OnGetAssetPathFailedEvent += OnGetAssetPathFailed;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaPADSdk = new AndroidJavaClass(androidClassPath))
                {
                    javaPADSdk.CallStatic("getAssetPath", assetName, assetType);
                }
            }
            else
            {
                OnGetAssetPathFailed(assetName);
            }
        }

        public static void CancelAssetDownload(string assetName)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaPADSdk = new AndroidJavaClass(androidClassPath))
                {
                    javaPADSdk.CallStatic("cancelAssetDownload", assetName);
                }
            }
        }

        public static void RemoveAsset(string assetName)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaPADSdk = new AndroidJavaClass(androidClassPath))
                {
                    javaPADSdk.CallStatic("removeAsset", assetName);
                }
            }
        }

        public static void RegisterListener(GameSdkPADAssetStatusUpdatedCallback callback)
        {
            mAssetStatusUpdatedCallback = callback;
            GameSDKCallbackListener.OnAssetStatusUpdatedEvent -= OnAssetStatusUpdated;
            GameSDKCallbackListener.OnAssetStatusUpdatedEvent += OnAssetStatusUpdated;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaPADSdk = new AndroidJavaClass(androidClassPath))
                {
                    javaPADSdk.CallStatic("registerListener");
                }
            }
        }

        public static void UnregisterListener()
        {
            ResetAssetStatusUpdatedCallback();
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaPADSdk = new AndroidJavaClass(androidClassPath))
                {
                    javaPADSdk.CallStatic("unregisterListener");
                }
            }
        }

        private static void OnAssetStatusUpdated(string assetName, int assetStatus, int errorCode, long totalBytes, long bytesDownloaded, int transferProgressPercentage)
        {
            if (mAssetStatusUpdatedCallback != null)
            {
                mAssetStatusUpdatedCallback(assetName, assetStatus, errorCode, totalBytes, bytesDownloaded, transferProgressPercentage);
            }
        }

        private static void ResetAssetStatusUpdatedCallback()
        {
            mAssetStatusUpdatedCallback = null;
            GameSDKCallbackListener.OnAssetStatusUpdatedEvent -= OnAssetStatusUpdated;
        }

        private static void OnGetAssetStatusFailed(string assetName)
        {
            if (mGetAssetStatusFailedCallback != null)
            {
                mGetAssetStatusFailedCallback(assetName);
            }
            mGetAssetStatusFailedCallback = null;
            GameSDKCallbackListener.OnGetAssetStatusFailedEvent -= OnGetAssetStatusFailed;
        }

        private static void OnGetAssetPathSuccess(string assetName, string assetPath)
        {
            if(mGetAssetPathSuccessCallback != null)
            {
                mGetAssetPathSuccessCallback(assetName, assetPath);
            }
            ResetGetAssetPathCallbacks();
        }

        private static void OnGetAssetPathFailed(string assetName)
        {
            if (mGetAssetPathFailedCallback != null)
            {
                mGetAssetPathFailedCallback(assetName);
            }
            ResetGetAssetPathCallbacks();
        }

        private static void ResetGetAssetPathCallbacks()
        {
            mGetAssetPathSuccessCallback = null;
            mGetAssetPathFailedCallback = null;
            GameSDKCallbackListener.OnGetAssetPathSuccessEvent -= OnGetAssetPathSuccess;
            GameSDKCallbackListener.OnGetAssetPathFailedEvent -= OnGetAssetPathFailed;
        }
    }
}
