using UnityEngine;

namespace EwingSoft.GameSDK
{
    public static class DlightekGameEventSdk
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.dlightek.DlightekGameEventSdk";

        public const string EnvironmentRelease = "release";
        public const string EnvironmentTest = "test";

        public const string ActionAppStart = "app_start";
        public const string ActionLoadingBegin = "loading_begin";
        public const string ActionLoadingEnd = "loading_end";
        public const string ActionLoginUi = "login_ui";
        public const string ActionLoginClick = "login_click";
        /// <summary>param1: 0/1 result; param2: user ID after a successful login.</summary>
        public const string ActionLoginResult = "login_result";
        public const string ActionCreateRole = "create_role";
        public const string ActionGameStart = "game_start";
        public const string ActionTutorialBegin = "tutorial_begin";
        public const string ActionTutorialEnd = "tutorial_end";
        public const string ActionTutorialReward = "tutorial_reward";
        public const string ActionModuleCheck = "module_check";
        public const string ActionPlayMission = "play_mission";
        public const string ActionLevelBegin = "level_begin";
        /// <summary>param1: level number; param2: 0/1 result.</summary>
        public const string ActionLevelEnd = "level_end";
        public const string ActionLevelReward = "level_reward";

        public static void Init(string environment, bool enableLog)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var unityPlayer = new AndroidJavaClass(unityPlayerClassPath))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var application = currentActivity.Call<AndroidJavaObject>("getApplication"))
            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                javaClass.CallStatic("initialize", application, environment, enableLog);
            }
        }

        public static void TrackEvent(string action, string param1, string param2)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            using (var javaClass = new AndroidJavaClass(androidClassPath))
            {
                javaClass.CallStatic("trackEvent", action, param1, param2);
            }
        }
    }
}
