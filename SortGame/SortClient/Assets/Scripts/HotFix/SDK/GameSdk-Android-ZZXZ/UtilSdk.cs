using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class UtilSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.util.UtilSdk";
        public static void InitGeneralToolSdk()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        javaClass.CallStatic("init", currentActivity);
                    }
                }
            }
        }

        public static bool CheckChromeBook()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("checkChromeBook");
                }
            }
            else
            {
                return false;
            }
        }

        public static void DoRate()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("doRate");
                }
            }
        }

        public static void DoShareText(string shareText)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("doShareText", shareText);
                }
            }
        }

        public static void CopyToClipboard(string text)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("copyToClipboard", text);
                }
            }
        }

        public static void ShowToast(string message)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("showToast", message);
                }
            }
        }

        public static int GetTotalMemory()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<int>("getTotalMemory");
                }
            }
            else
            {
                return 0;
            }
        }

        public static int GetAvailableMemory()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<int>("getAvailableMemory");
                }
            }
            else
            {
                return 0;
            }
        }

        public static string GenerateRandomUUIDString(int length)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<string>("generateRandomUUIDString",length);
                }
            }
            else
            {
                return "";
            }
        }

        public static string GetUUIDString()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<string>("getUUIDString");
                }
            }
            else
            {
                return "";
            }
        }

        public static void OpenWebURL(string url)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("openWebUrl", url);
                }
            }
        }

        public static void SendEmail(string recipient, string subject, string message)
        {
            if(Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("sendEmail", recipient, subject, message);
                }
            }
        }

        public static float GetAndroidDensity()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    // 获取当前 Activity
                    using (var unityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                    using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var resources = activity.Call<AndroidJavaObject>("getResources"))
                    using (var metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
                    {
                        // density 是 density factor (如 1.0、1.5、2.0…)
                        float density = metrics.Get<float>("density");

                        return density;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[AndroidDensityHelper] 获取 density 失败: {e}");
                    return 1.0f;
                }
            }
            else
            {
                return 1.0f;
            }
        }

        public static string GetCountryCode()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                {
                    string countryCode = javaClass.CallStatic<string>("getCountryCode");
                    return string.IsNullOrEmpty(countryCode) ? "unknown" : countryCode;
                }
            }
            else
            {
                return "unknown";
            }
        }
    }
}
