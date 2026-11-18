using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class BaseSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.base.BaseSdk";
        public static void InitBaseSDK()
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

        public static void InitBaseSDK(bool showDebugLog)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    using (AndroidJavaClass javaClass = new AndroidJavaClass(androidClassPath))
                    {
                        javaClass.CallStatic("init", currentActivity, showDebugLog);
                    }
                }
            }
        }
    }
}