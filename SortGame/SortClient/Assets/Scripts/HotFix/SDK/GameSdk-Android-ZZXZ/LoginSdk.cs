using System;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class LoginSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.login.LoginSdk";

        private static GameSdkStringIntIntParamVoidCallback mLoginedCallback;
        public enum ThirdPartyType
        {
            Apple = 0,
            FaceBook,
            Google,
            Unknown
        }

        public enum ErrorType
        {
            NoError = 0,
            UserCanceled,
            NotSupport,
            OtherError
        }

        public static void InitLoginSdk(string googleClientId)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (var javaLoginManager = new AndroidJavaClass(androidClassPath))
                    {
                        javaLoginManager.CallStatic("init", currentActivity, googleClientId);
                    }
                }
            }
        }

        public static void Login(GameSdkStringIntIntParamVoidCallback callback, ThirdPartyType thirdPartyType)
        {
            mLoginedCallback = callback;
            GameSDKCallbackListener.OnLoginedEvent += OnLogined;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaLoginManager = new AndroidJavaClass(androidClassPath))
                {
                    javaLoginManager.CallStatic("login", (int)thirdPartyType);
                }
            }else{
                OnLogined("", 3, 3);
            }
        }

        public static void OnLogined(string openId, int loginType, int errorType)
        {
            if(mLoginedCallback != null)
            {
                mLoginedCallback(openId, loginType, errorType);
            }

            mLoginedCallback = null;
            GameSDKCallbackListener.OnLoginedEvent -= OnLogined;
        }

        public static bool CheckLoginedForAndroid(ThirdPartyType thirdPartyType)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaLoginManager = new AndroidJavaClass(androidClassPath))
                {
                    return javaLoginManager.CallStatic<bool>("checkLogined", (int)thirdPartyType);
                }
            }
            return true;
        }

        public static string GetOpenId(ThirdPartyType thirdPartyType)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaLoginManager = new AndroidJavaClass(androidClassPath))
                {
                    return javaLoginManager.CallStatic<string>("getOpenId", (int)thirdPartyType);
                }
            }
            return string.Empty;
        }

        public static void Logout(ThirdPartyType thirdPartyType)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaLoginManager = new AndroidJavaClass(androidClassPath))
                {
                    javaLoginManager.CallStatic("logout", (int)thirdPartyType);
                }
            }
        }

    }
}
