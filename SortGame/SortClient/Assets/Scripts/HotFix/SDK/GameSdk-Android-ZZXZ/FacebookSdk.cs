using System.Collections.Generic;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class FacebookSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.facebook.FaceBookSdk";
        public static void InitFacebookSDK(bool isDebug)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    using (var javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        javaClass.CallStatic("init", currentActivity, isDebug);
                    }
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

        public static void LogEvent(string eventName, Dictionary<string, object> parameterValues)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    if (parameterValues == null || parameterValues.Count == 0)
                    {
                        javaClass.CallStatic("logEvent", eventName);
                        return;
                    }

                    using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                    {
                        foreach (var entry in parameterValues)
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
    }
}