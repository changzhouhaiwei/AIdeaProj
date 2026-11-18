using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class CMPSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.cmp.CMPSdk";

        private static GameSdkBoolIntParamVoidCallback mCMPFinishedCallback;

        public static void InitCMPSdk(bool isDebug)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath);
                    string adMediationName = "";

                    javaClass.CallStatic("init", currentActivity, adMediationName, isDebug);
                }
            }
        }

        public static void SetURL(string privacyPolicyURL, string termsOfServiceURL)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setURL", privacyPolicyURL, termsOfServiceURL);
                }
            }
        }

        public static bool IsGDPRCountry()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("isGDPRCountry");
                }
            }
            else
            {
                return false;
            }
        }

        public static void StartCMP(GameSdkBoolIntParamVoidCallback startCMPFinishedCallback = null)
        {
            mCMPFinishedCallback = startCMPFinishedCallback;
            GameSDKCallbackListener.OnCMPFinishedEvent += OnCMPFinished;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("startGDPR");
                }
            }
            else
            {
                OnCMPFinished(true, 0);
            }
        }

        public static void UpdateCMP(GameSdkBoolIntParamVoidCallback updateCMPFinishedCallback = null)
        {
            mCMPFinishedCallback = updateCMPFinishedCallback;
            GameSDKCallbackListener.OnCMPFinishedEvent += OnCMPFinished;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("updateGDPR");
                }
            }
            else
            {
                OnCMPFinished(true, 0);
            }
        }

        public static int GetAdLevels()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<int>("getAdLevels");
                }
            }
            else
            {
                return -1;
            }
        }

        private static void OnCMPFinished(bool canSet, int errorCode)
        {
            if (mCMPFinishedCallback != null)
            {
                mCMPFinishedCallback(canSet, errorCode);
            }
            ResetCMPFinishedCallbacks();
        }

        private static void ResetCMPFinishedCallbacks()
        {
            mCMPFinishedCallback = null;
            GameSDKCallbackListener.OnCMPFinishedEvent -= OnCMPFinished;
        }
    }
}
