using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class UserDataSdk : MonoBehaviour
    {
        private const string androidClassPath = "com.zzxz.gamesdk.core.userdata.UserDataSdk";

        public static string GetUserDataString()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<string>("getUserDataString");
                }
            }
            return string.Empty;
        }

        public static void SetUserDataString(string data)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setUserDataString", data);
                }
            }
        }

        public static void SetStringForKey(string key, string value)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setStringForKey", key, value);
                }
            }
        }

        public static string GetStringForKey(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<string>("getStringForKey", key);
                }
            }
            return string.Empty;
        }

        public static void SetIntForKey(string key, int value)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setIntForKey", key, value);
                }
            }
        }

        public static int GetIntForKey(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<int>("getIntForKey", key);
                }
            }
            return 0;
        }

        public static void SetLongForKey(string key, long value)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setLongForKey", key, value);
                }
            }
        }

        public static long GetLongForKey(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<long>("getLongForKey", key);
                }
            }
            return 0L;
        }

        public static void SetDoubleForKey(string key, double value)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setDoubleForKey", key, value);
                }
            }
        }

        public static double GetDoubleForKey(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<double>("getDoubleForKey", key);
                }
            }
            return 0.0d;
        }

        public static void SetBoolForKey(string key, bool value)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("setBoolForKey", key, value);
                }
            }
        }

        public static bool GetBoolForKey(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    return javaClass.CallStatic<bool>("getBoolForKey", key);
                }
            }
            return false;
        }

        public static void SaveUserData()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("saveUserData");
                }
            }
        }

        public static void GetUserData()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaClass = new AndroidJavaClass(androidClassPath))
                {
                    javaClass.CallStatic("getUserData");
                }
            }
        }
    }
}
