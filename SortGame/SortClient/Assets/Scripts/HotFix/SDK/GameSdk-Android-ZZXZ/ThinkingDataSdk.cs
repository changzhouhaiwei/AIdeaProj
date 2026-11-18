using System.Collections.Generic;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    public static class ThinkingDataSdk
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.thinkingdata.ThinkingDataSdk";

        public static void Init(string appId, string serverUrl, bool isDebug)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var unityPlayer = new AndroidJavaClass(unityPlayerClassPath))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                javaClass.CallStatic("init", currentActivity, appId, serverUrl, isDebug);
            }
        }

        public static void LogEvent(string eventName)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                javaClass.CallStatic("logEvent", eventName);
            }
        }

        public static void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                if (parameters == null || parameters.Count == 0)
                {
                    javaClass.CallStatic("logEvent", eventName);
                    return;
                }

                using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                {
                    foreach (var kv in parameters)
                    {
                        string key = kv.Key;
                        object value = kv.Value;

                        if (value is string stringValue)
                        {
                            bundle.Call("putString", key, stringValue);
                        }
                        else if (value is int intValue)
                        {
                            bundle.Call("putInt", key, intValue);
                        }
                        else if (value is long longValue)
                        {
                            bundle.Call("putLong", key, longValue);
                        }
                        else if (value is float floatValue)
                        {
                            bundle.Call("putFloat", key, floatValue);
                        }
                        else if (value is double doubleValue)
                        {
                            bundle.Call("putDouble", key, doubleValue);
                        }
                        else if (value is bool boolValue)
                        {
                            bundle.Call("putBoolean", key, boolValue);
                        }
                        else if (value != null)
                        {
                            bundle.Call("putString", key, value.ToString());
                        }
                    }

                    javaClass.CallStatic("logEvent", eventName, bundle);
                }
            }
        }

        /// <summary>
        /// Log an event with parameters provided as a JSON string.
        /// The JSON string will be passed directly to Java layer for parsing.
        /// </summary>
        /// <param name="eventName">Name of the event to track</param>
        /// <param name="jsonParameters">JSON string containing event parameters</param>
        public static void LogEvent(string eventName, string jsonParameters)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                // Pass JSON string directly to Java layer - parsing handled there
                javaClass.CallStatic("logEvent", eventName, jsonParameters);
            }
        }

        public static void SetUserProperty(string propertyName, string propertyValue)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                javaClass.CallStatic("setUserProperty", propertyName, propertyValue);
            }
        }

        /// <summary>批量覆盖数数用户属性（user_set），参数为 JSON 对象字符串。</summary>
        public static void UserSet(string jsonProperties)
        {
            SetJsonProperties("userSet", jsonProperties);
        }

        /// <summary>首次写入数数用户属性（user_setOnce），参数为 JSON 对象字符串。</summary>
        public static void UserSetOnce(string jsonProperties)
        {
            SetJsonProperties("userSetOnce", jsonProperties);
        }

        /// <summary>批量设置数数公共事件属性，支持对象和数组类型。</summary>
        public static void SetPublicProperties(string jsonProperties)
        {
            SetJsonProperties("setSuperProperties", jsonProperties);
        }

        static void SetJsonProperties(string methodName, string jsonProperties)
        {
            if (Application.platform != RuntimePlatform.Android
                || string.IsNullOrWhiteSpace(jsonProperties))
            {
                return;
            }

            try
            {
                using (var properties = new AndroidJavaObject("org.json.JSONObject", jsonProperties))
                using (var javaClass = new AndroidJavaClass("cn.thinkingdata.analytics.TDAnalytics"))
                {
                    javaClass.CallStatic(methodName, properties);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ThinkingDataSdk.{methodName} failed: {e.Message}");
            }
        }

        public static void SetPublicProperty(string propertyName, string propertyValue)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                javaClass.CallStatic("setPublicProperty", propertyName, propertyValue);
            }
        }

        public static void BoundAccountId(string accountId)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                javaClass.CallStatic("boundAccountId", accountId);
            }
        }

        public static void UnboundAccountId()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                javaClass.CallStatic("unboundAccountId");
            }
        }

        public static string GetDistinctId()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return null;
            }

            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                return javaClass.CallStatic<string>("getDistinctId");
            }
        }
    }
}
