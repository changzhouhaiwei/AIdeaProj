using System;
using System.Collections.Generic;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class AttrSdk : MonoBehaviour
    {

        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.attribution.AttrSdk";

        /**
        * 初始化AttrSDK。此方法仅适用于Singular。
        * 
        * 该函数用于在Android平台上初始化AttrSDK，通过传入的sdkKey和sdkSecret进行配置。
        * 
        * 参数:
        * - sdkKey: string类型，表示SDK的唯一标识符，用于验证SDK的合法性。
        * - sdkSecret: string类型，表示SDK的密钥，用于确保SDK的安全性。
        * 
        * 返回值:
        * - 无返回值。
        */
        public static void InitAttrSDK(string sdkKey, string sdkSecret)
        {
            InitAttrSDK(sdkKey, sdkSecret, false);
        }

        /// <summary>
        /// 初始化AttrSDK，支持传入调试开关，仅适用于Singular。
        /// </summary>
        /// <param name="sdkKey">SDK Key</param>
        /// <param name="sdkSecret">SDK Secret</param>
        /// <param name="isDebug">是否使用调试模式</param>
        public static void InitAttrSDK(string sdkKey, string sdkSecret, bool isDebug)
        {
            // 检查当前运行平台是否为Android
            if (Application.platform == RuntimePlatform.Android)
            {
                // 使用Unity的AndroidJavaClass获取UnityPlayer类
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    // 获取当前Activity对象，用于与Android原生代码交互
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    // 使用指定的Android类路径加载目标类
                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        // 调用目标类的静态方法init，传递当前Activity、sdkKey、sdkSecret和调试标志参数
                        javaClass.CallStatic("initialize",
                            currentActivity,
                            sdkKey,
                            sdkSecret,
                            isDebug
                        );
                    }
                }
            }
        }


        /// <summary>
        /// 跟踪事件，特定于Android平台。
        /// </summary>
        /// <param name="eventToken">事件标识符。</param>
        public static void TrackEvent(string eventToken)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("trackEvent", eventToken);
                }
            }
        }
        
        /// <summary>
        /// 跟踪带有参数的事件，特定于Android平台。
        /// </summary>
        /// <param name="eventName">事件名称。</param>
        /// <param name="parameterValues">事件参数和值的字典。</param>
        public static void TrackEvent(string eventName, Dictionary<string, object> parameterValues)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    using (var javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        // 创建 HashMap
                        using (var hashMap = new AndroidJavaObject("java.util.HashMap"))
                        {
                            var putMethod = AndroidJNIHelper.GetMethodID(
                                hashMap.GetRawClass(),
                                "put",
                                "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;"
                            );

                            foreach (var entry in parameterValues)
                            {
                                // 处理不同类型的值，转换为 Java 对象
                                AndroidJavaObject valueObject = null;
                                if (entry.Value is string valueString)
                                {
                                    valueObject = new AndroidJavaObject("java.lang.String", valueString);
                                }
                                else if (entry.Value is int valueInt)
                                {
                                    valueObject = new AndroidJavaObject("java.lang.Integer", valueInt);
                                }
                                else if (entry.Value is bool valueBool)
                                {
                                    valueObject = new AndroidJavaObject("java.lang.Boolean", valueBool);
                                }
                                else if (entry.Value is double valueDouble)
                                {
                                    valueObject = new AndroidJavaObject("java.lang.Double", valueDouble);
                                }
                                else if (entry.Value is float valueFloat)
                                {
                                    valueObject = new AndroidJavaObject("java.lang.Float", valueFloat);
                                }
                                else if (entry.Value is long valueLong)
                                {
                                    valueObject = new AndroidJavaObject("java.lang.Long", valueLong);
                                }
                                else
                                {
                                    // 尝试转换为字符串
                                    valueObject = new AndroidJavaObject("java.lang.String", entry.Value.ToString());
                                }

                                // 显式按 put(Object, Object) 调用，匹配 Java 层 trackEvent(String, HashMap<String, Object>)
                                using (var keyObject = new AndroidJavaObject("java.lang.String", entry.Key))
                                {
                                    object[] putArgs = { keyObject, valueObject };
                                    var jniArgs = AndroidJNIHelper.CreateJNIArgArray(putArgs);
                                    AndroidJNI.CallObjectMethod(hashMap.GetRawObject(), putMethod, jniArgs);
                                    AndroidJNIHelper.DeleteJNIArgArray(putArgs, jniArgs);
                                }

                                // 释放 valueObject
                                if (valueObject != null)
                                {
                                    valueObject.Dispose();
                                }
                            }
        
                            // 调用 Java 方法，传递 eventName 和 hashMap
                            javaClass.CallStatic("trackEvent", eventName, hashMap);
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    // 处理不支持的类型异常
                    Debug.LogError($"Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // 处理其他可能的异常
                    Debug.LogError($"Unexpected error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取归因渠道信息。
        /// </summary>
        /// <returns>归因渠道名称，如果未获取到则返回 "unknown"。</returns>
        public static string GetAttrNetwork()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    using (var javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        return javaClass.CallStatic<string>("getAttrNetwork");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error calling getAttrNetwork: {ex.Message}");
                }
            }
            return "unknown";
        }

        /// <summary>
        /// 获取归因广告系列信息。
        /// </summary>
        /// <returns>归因广告系列名称，如果未获取到则返回 "unknown"。</returns>
        public static string GetAttrCampaign()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    using (var javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        return javaClass.CallStatic<string>("getAttrCampaign");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error calling getAttrCampaign: {ex.Message}");
                }
            }
            return "unknown";
        }

    }
}
