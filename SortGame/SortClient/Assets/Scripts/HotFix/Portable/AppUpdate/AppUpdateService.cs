using System;
using UnityEngine;

namespace Portable.AppUpdate
{
    /// <summary>
    /// 版本更新弹窗。复制 Portable/AppUpdate 整个目录即可，不依赖任何游戏 SDK。
    /// 游戏侧负责：把 Remote Config 的 AppUpdateConfig 字符串交给 Refresh，在主页和进关前调用 TryShow。
    /// 返回值 true 表示强更，调用方应中断进关。软更会弹窗但返回 false。
    /// 游戏如果自己处理返回键，要先调用 ConsumeEscape，否则强更挡不住返回。
    /// </summary>
    public static class AppUpdateService
    {
        const string LogTag = "[AppUpdate]";
        const int LaunchCooldownMs = 400;

#if UNITY_ANDROID && !UNITY_EDITOR
        const int StoreFlagNewTask = 0x10000000;
        const int StoreFlagClearTop = 0x04000000;
        const int StoreFlagReorderToFront = 0x00020000;
        const int StoreFlags = StoreFlagNewTask | StoreFlagClearTop | StoreFlagReorderToFront;
#endif

        static AppUpdateHostOptions s_options = new AppUpdateHostOptions();
        static AppUpdateConfig s_config;
        static string s_version;
        static int s_escapeFrame = -1;
        static int s_lastLaunchTick;
        static volatile bool s_skipConfirmOpen;

        public static bool IsDialogOpen => AppUpdatePromptView.IsOpen;

        public static void Configure(AppUpdateHostOptions options)
        {
            s_options = options ?? new AppUpdateHostOptions();
            if (string.IsNullOrEmpty(s_options.SoftDismissPrefsKey))
            {
                s_options.SoftDismissPrefsKey = "app_update_soft_dismissed_version";
            }
        }

        public static void Refresh(string configJson, string currentVersion = null)
        {
            s_version = string.IsNullOrWhiteSpace(currentVersion) ? Application.version : currentVersion.Trim();
            try
            {
                s_config = AppUpdateRules.Parse(configJson);
            }
            catch (Exception e)
            {
                s_config = null;
                Debug.LogWarning(LogTag + " 配置解析失败 " + e.Message);
            }

            var decision = Evaluate(int.MaxValue);
            Debug.Log(LogTag + " Refresh raw=" + (string.IsNullOrWhiteSpace(configJson) ? "<empty>" : configJson));
            LogDecision("Refresh", -1, decision);

            if (!IsDialogOpen)
            {
                Debug.Log(LogTag + " Refresh 当前没有弹窗，不立刻显示");
                return;
            }

            if (!decision.ShowDialog)
            {
                Debug.Log(LogTag + " Refresh 配置不再需要弹窗，关闭");
                AppUpdatePromptView.Close();
                return;
            }

            Debug.Log(LogTag + " Refresh 弹窗仍要显示，重新打开");
            var prompt = AppUpdateRules.BuildPrompt(s_config, decision.Kind, s_options);
            AppUpdatePromptView.Show(prompt, Confirm, Skip);
        }

        /// <summary>强更返回 true。编辑器默认不弹。</summary>
        public static bool TryShow(int currentLevel)
        {
            var decision = Evaluate(currentLevel);
            LogDecision("TryShow", currentLevel, decision);
            if (!decision.ShowDialog)
            {
                return false;
            }

            if (IsDialogOpen && AppUpdatePromptView.IsForce == (decision.Kind == AppUpdateKind.Force))
            {
                Debug.Log(LogTag + " 弹窗已打开，不重复创建 kind=" + decision.Kind);
                return decision.BlockProgress;
            }

            var prompt = AppUpdateRules.BuildPrompt(s_config, decision.Kind, s_options);
            Debug.Log(LogTag
                + " 请求弹窗 kind=" + decision.Kind
                + " title=" + prompt.Title
                + " store=" + prompt.StoreUrl
                + " fallback=" + prompt.FallbackUrl);
            AppUpdatePromptView.Show(prompt, Confirm, Skip);
            if (!IsDialogOpen)
            {
                Debug.LogWarning(LogTag + " 判定要弹，但弹窗没有打开。强更仍会拦住进关 block=" + decision.BlockProgress);
            }

            return decision.BlockProgress;
        }

        /// <summary>本帧返回键已被更新弹窗吃掉时返回 true。旧输入系统用。</summary>
        public static bool HandleEscape()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return ConsumeEscape(Input.GetKeyDown(KeyCode.Escape));
#else
            return false;
#endif
        }

        /// <summary>
        /// 游戏已经检测到返回键时调用。本工程只用新输入系统，GetKeyDown 不可用。
        /// </summary>
        public static bool ConsumeEscape(bool escapePressed)
        {
            if (Time.frameCount == s_escapeFrame)
            {
                return true;
            }

            if (!IsDialogOpen || !escapePressed)
            {
                return false;
            }

            s_escapeFrame = Time.frameCount;
            Debug.Log(LogTag + " Escape 已拦截 force=" + AppUpdatePromptView.IsForce);
            if (!AppUpdatePromptView.IsForce)
            {
                Skip();
            }

            return true;
        }

        public static void Confirm()
        {
            var prompt = AppUpdatePromptView.Current;
            if (prompt == null)
            {
                return;
            }

            Debug.Log(LogTag
                + " Confirm force=" + prompt.ForceUpdate
                + " store=" + prompt.StoreUrl
                + " fallback=" + prompt.FallbackUrl
                + " already=" + s_skipConfirmOpen);
            if (s_skipConfirmOpen)
            {
                s_skipConfirmOpen = false;
            }
            else
            {
                OpenStore(prompt.StoreUrl, prompt.FallbackUrl);
            }
            if (!prompt.ForceUpdate)
            {
                Debug.Log(LogTag + " Confirm 软更，关闭弹窗");
                AppUpdatePromptView.Close();
                return;
            }

            Debug.Log(LogTag + " Confirm 强更，弹窗保持");
        }

        public static void Skip()
        {
            var prompt = AppUpdatePromptView.Current;
            if (prompt == null || prompt.ForceUpdate)
            {
                Debug.Log(LogTag + " Skip 被拒绝 force=" + (prompt != null && prompt.ForceUpdate));
                return;
            }

            if (!string.IsNullOrEmpty(prompt.LatestVersion))
            {
                PlayerPrefs.SetString(s_options.SoftDismissPrefsKey, prompt.LatestVersion.Trim());
                PlayerPrefs.Save();
                Debug.Log(LogTag + " Skip 记住 latest=" + prompt.LatestVersion.Trim() + " key=" + s_options.SoftDismissPrefsKey);
            }

            AppUpdatePromptView.Close();
        }

        /// <summary>
        /// 强更按钮在 Android UI 线程里调用。成功后 Confirm 不再开第二次，避免返回后下一帧才跳。
        /// </summary>
        internal static bool TryLaunchStoreFromUi(string storeUrl, string fallbackUrl)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            bool opened = TryOpenAndroid(storeUrl);
            if (!opened
                && !string.IsNullOrEmpty(fallbackUrl)
                && !string.Equals(fallbackUrl, storeUrl, StringComparison.Ordinal))
            {
                opened = TryOpenAndroid(fallbackUrl);
            }
#else
            bool opened = false;
#endif
            if (opened)
            {
                s_lastLaunchTick = Environment.TickCount;
                s_skipConfirmOpen = true;
            }

            return opened;
        }

        public static void OpenStore(string storeUrl, string fallbackUrl)
        {
            string store = storeUrl ?? string.Empty;
            string fallback = fallbackUrl ?? string.Empty;
            if (Application.platform == RuntimePlatform.IPhonePlayer
                && (string.IsNullOrEmpty(store) || store.StartsWith("market:", StringComparison.OrdinalIgnoreCase)))
            {
                Debug.Log(LogTag + " iOS 改用 fallback=" + fallback);
                store = fallback;
            }

            if (RecentlyLaunched())
            {
                Debug.Log(LogTag + " OpenStore 刚打开过，不重复跳转");
                return;
            }

            Debug.Log(LogTag + " OpenStore store=" + store + " fallback=" + fallback);
            if (!string.IsNullOrEmpty(store) && TryOpen(store))
            {
                s_lastLaunchTick = Environment.TickCount;
                Debug.Log(LogTag + " OpenStore 已打开 " + store);
                return;
            }

            if (!string.IsNullOrEmpty(fallback) && !string.Equals(fallback, store, StringComparison.Ordinal) && TryOpen(fallback))
            {
                s_lastLaunchTick = Environment.TickCount;
                Debug.Log(LogTag + " OpenStore 商店失败，已打开 fallback " + fallback);
                return;
            }

            Debug.LogWarning(LogTag + " 没有可打开的商店链接");
        }

        static AppUpdateDecision Evaluate(int currentLevel)
        {
            return AppUpdateRules.Evaluate(
                s_config,
                s_version,
                currentLevel,
                s_options,
                PlayerPrefs.GetString(s_options.SoftDismissPrefsKey, string.Empty),
                Application.isEditor);
        }

        static void LogDecision(string where, int level, AppUpdateDecision decision)
        {
            string levelText = level < 0 ? "忽略关卡" : level.ToString();
            Debug.Log(LogTag
                + " " + where
                + " version=" + s_version
                + " level=" + levelText
                + " kind=" + decision.Kind
                + " show=" + decision.ShowDialog
                + " block=" + decision.BlockProgress
                + " dismissed=" + PlayerPrefs.GetString(s_options.SoftDismissPrefsKey, string.Empty)
                + " editor=" + Application.isEditor
                + " reason=" + decision.Reason);
        }

        static bool RecentlyLaunched()
        {
            int delta = unchecked(Environment.TickCount - s_lastLaunchTick);
            return s_lastLaunchTick != 0 && delta >= 0 && delta < LaunchCooldownMs;
        }

        static bool TryOpen(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (TryOpenAndroid(url))
            {
                return true;
            }
#endif
            Application.OpenURL(url);
            return true;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static bool TryOpenAndroid(string url)
        {
            if (IsPlayStoreUrl(url) && TryStartView(url, true))
            {
                return true;
            }

            return TryStartView(url, false);
        }

        static bool IsPlayStoreUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            return url.StartsWith("market:", StringComparison.OrdinalIgnoreCase)
                || url.IndexOf("play.google.com", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool TryStartView(string url, bool pinPlayStore)
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", url))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW", uri))
                {
                    if (activity == null)
                    {
                        return false;
                    }

                    intent.Call<AndroidJavaObject>("addFlags", StoreFlags);
                    if (pinPlayStore)
                    {
                        intent.Call<AndroidJavaObject>("setPackage", "com.android.vending");
                    }

                    activity.Call("startActivity", intent);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogTag + " 打开链接失败 " + url + " play=" + pinPlayStore + " " + e.Message);
                return false;
            }
        }
#endif
    }
}
