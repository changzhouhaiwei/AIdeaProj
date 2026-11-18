using System.Collections.Generic;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class FirebaseSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.firebase.FirebaseSdk";

        private static GameSdkBoolParamVoidCallback mRemoteConfigCallback;
        public static void InitFirebaseSDK()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    using (var javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        javaClass.CallStatic("init", currentActivity);
                    }
                }
            }
        }

        public static void SetRemoteConfigUpdateCallback(GameSdkBoolParamVoidCallback callback = null){
            mRemoteConfigCallback = callback;
            GameSDKCallbackListener.OnRemoteConfigLoadedEvent += OnRemoteConfigLoaded;
        }

        private static void OnRemoteConfigLoaded(bool success)
        {
            if (mRemoteConfigCallback != null)
            {
                mRemoteConfigCallback(success);
                mRemoteConfigCallback = null;
            }
            GameSDKCallbackListener.OnRemoteConfigLoadedEvent -= OnRemoteConfigLoaded;
        }

        public static void SetEventAdRevenueImpression(string eventAdRevenueImpression)
        {
            if(Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setEventAdRevenueImpression", eventAdRevenueImpression);
                }
            }
        }

        public static string GetUserPseudoId()
        {
            if(Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<string>("getUserPseudoId");
                }
            }
            return "";
        }

        public static void SetUserProperty(string userPropertyKey, string userPropertyValue)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setUserProperty", userPropertyKey, userPropertyValue);
                }
            }
        }

        public static void LogEvent(string eventName)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("logEvent", eventName);
                }
            }
        }

        public static void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    if (parameters == null || parameters.Count == 0)
                    {
                        javaClass.CallStatic("logEvent", eventName);
                        return;
                    }

                    using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                    {
                        foreach (var entry in parameters)
                        {
                            string key = entry.Key;
                            object value = entry.Value;
                            if (value is string)
                            {
                                bundle.Call("putString", key, (string)value);
                            }
                            else if (value is int)
                            {
                                bundle.Call("putInt", key, (int)value);
                            }
                            else if (value is long)
                            {
                                bundle.Call("putLong", key, (long)value);
                            }
                            else if (value is float)
                            {
                                bundle.Call("putFloat", key, (float)value);
                            }
                            else if (value is double)
                            {
                                bundle.Call("putDouble", key, (double)value);
                            }
                            else if (value is bool)
                            {
                                bundle.Call("putBoolean", key, (bool)value);
                            }
                        }

                        javaClass.CallStatic("logEvent", eventName, bundle);
                    }
                }
            }
        }

        public static bool GetRemoteConfigUpdateStatus()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("getRemoteConfigUpdateStatus");
                }
            }
            else
            {
                return false;
            }
        }

        public static int GetIntRemoteConfig(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<int>("getIntRemoteConfig", key);
                }
            }
            else
            {
                return 0;
            }
        }

        public static double GetDoubleRemoteConfig(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<double>("getDoubleRemoteConfig", key);
                }
            }
            else
            {
                return 0;
            }
        }

        public static string GetStringRemoteConfig(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<string>("getStringRemoteConfig", key);
                }
            }
            else
            {
                return "";
            }
        }

        public static bool GetBoolRemoteConfig(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("getBooleanRemoteConfig", key);
                }
            }
            else
            {
                return false;
            }
        }

        public static void SetConsent()
        {
            if(Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setConsent");
                }
            }
        }
    }
}