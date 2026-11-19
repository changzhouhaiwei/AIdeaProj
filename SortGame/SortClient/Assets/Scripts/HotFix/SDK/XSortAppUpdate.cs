using System;
using FishXSortPlay;
using Portable.AppUpdate;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// SoEasy XSort 的强更/软更宿主。逻辑来自 mahjong10adjoe 的 MahjongAppUpdate。
/// Remote Config 键 AppUpdateConfig。SDK 这次没拉到新配置时，忽略拉取间隔再向服务器要一次。
/// 要失败才沿用已激活配置。
/// </summary>
public static class XSortAppUpdate
{
    public const string RemoteConfigKey = "AppUpdateConfig";
    const string AndroidPackage = "com.sofish.soeasyXSort.gp";

    static bool s_configured;
    static bool s_forceFetchStarted;
    static AndroidJavaObject s_remoteConfig;
    static FetchTaskProxy s_proxy;

    public static void NotifyRemoteConfigLoaded(bool success)
    {
#if UNITY_IOS && !UNITY_EDITOR
        return;
#else
        EnsureConfigured();
        bool fresh = success && IsFreshRemoteConfig();
        Debug.Log("[AppUpdate] RemoteConfig success=" + success
            + " fresh=" + fresh
            + " level=" + CurrentShowLevel()
            + " version=" + Application.version);
        if (!success)
        {
            Debug.Log("[AppUpdate] Remote Config 回调失败，不更新配置");
            return;
        }

        if (fresh)
        {
            ApplyLoadedConfig("SDK已拉到新配置");
            return;
        }

        ForceFetchLatest();
#endif
    }

    /// <summary>强更返回 true，调用方中断进关。软更弹窗但返回 false。iOS 不处理，返回 false。</summary>
    public static bool TryShow(string source)
    {
#if UNITY_IOS && !UNITY_EDITOR
        return false;
#else
        EnsureConfigured();
        int level = CurrentShowLevel();
        Debug.Log("[AppUpdate] 来源=" + source + " level=" + level + " version=" + Application.version);
        bool block = AppUpdateService.TryShow(level);
        Debug.Log("[AppUpdate] 来源=" + source + " 返回 block=" + block);
        return block;
#endif
    }

    /// <summary>返回键已经被游戏检测到时调用。弹窗吃掉了这次返回则返回 true。iOS 不拦截。</summary>
    public static bool ConsumeEscape()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return false;
#else
        EnsureConfigured();
        return AppUpdateService.ConsumeEscape(true);
#endif
    }

    static void ApplyLoadedConfig(string source)
    {
        Debug.Log("[AppUpdate] 使用配置 来源=" + source);
        AppUpdateService.Refresh(ReadConfig());
        var home = UnityEngine.Object.FindObjectOfType<UIMainMenu>();
        if (home != null && home.isActiveAndEnabled)
        {
            Debug.Log("[AppUpdate] 来源=RemoteConfig.Home 主页已打开");
            AppUpdateService.TryShow(CurrentShowLevel());
            return;
        }

        Debug.Log("[AppUpdate] 来源=RemoteConfig 主页未打开，等主页或进关再弹");
    }

    static void ForceFetchLatest()
    {
        if (s_forceFetchStarted)
        {
            Debug.Log("[AppUpdate] 强制拉取已经开始，跳过重复请求");
            return;
        }

        s_forceFetchStarted = true;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var rcClass = new AndroidJavaClass("com.google.firebase.remoteconfig.FirebaseRemoteConfig"))
            {
                s_remoteConfig = rcClass.CallStatic<AndroidJavaObject>("getInstance");
            }

            Debug.Log("[AppUpdate] 步骤=强制拉取开始 interval=0 原因=SDK本次updated=false");
            var fetchTask = s_remoteConfig.Call<AndroidJavaObject>("fetch", 0L);
            s_proxy = new FetchTaskProxy("强制拉取", OnForceFetchFinished);
            fetchTask.Call<AndroidJavaObject>("addOnCompleteListener", s_proxy).Dispose();
            fetchTask.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AppUpdate] 强制拉取失败，沿用已激活配置 " + e.Message);
            ApplyLoadedConfig("已激活缓存");
        }
#else
        ApplyLoadedConfig("已激活缓存");
#endif
    }

    static void OnForceFetchFinished(bool ok, string error)
    {
        AppUpdatePromptView.Post(() =>
        {
            Debug.Log("[AppUpdate] 步骤=强制拉取结果 ok=" + ok + " error=" + error);
            if (!ok || s_remoteConfig == null)
            {
                Debug.LogWarning("[AppUpdate] 强制拉取未成功，沿用已激活配置");
                ApplyLoadedConfig("已激活缓存");
                return;
            }

            try
            {
                Debug.Log("[AppUpdate] 步骤=强制激活开始");
                var activateTask = s_remoteConfig.Call<AndroidJavaObject>("activate");
                s_proxy = new FetchTaskProxy("强制激活", OnForceActivateFinished);
                activateTask.Call<AndroidJavaObject>("addOnCompleteListener", s_proxy).Dispose();
                activateTask.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AppUpdate] 激活远端配置失败，沿用已激活配置 " + e.Message);
                ApplyLoadedConfig("已激活缓存");
            }
        });
    }

    static void OnForceActivateFinished(bool ok, string error)
    {
        AppUpdatePromptView.Post(() =>
        {
            Debug.Log("[AppUpdate] 步骤=强制激活结果 ok=" + ok + " error=" + error);
            ApplyLoadedConfig(ok ? "强制拉取" : "已激活缓存");
        });
    }

    sealed class FetchTaskProxy : AndroidJavaProxy
    {
        readonly string _step;
        readonly Action<bool, string> _done;

        public FetchTaskProxy(string step, Action<bool, string> done)
            : base("com.google.android.gms.tasks.OnCompleteListener")
        {
            _step = step;
            _done = done;
        }

        [Preserve]
        public void onComplete(AndroidJavaObject task)
        {
            bool ok = false;
            string error = "";
            try
            {
                ok = task != null && task.Call<bool>("isSuccessful");
                if (!ok && task != null)
                {
                    using (var exception = task.Call<AndroidJavaObject>("getException"))
                    {
                        error = exception == null ? "unknown" : exception.Call<string>("getMessage");
                    }
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogWarning("[AppUpdate] 步骤=" + _step + " 读取结果失败 " + e.Message);
            }

            Debug.Log("[AppUpdate] 步骤=" + _step + " 回调 ok=" + ok + " error=" + error);
            _done?.Invoke(ok, error ?? "");
        }
    }

    /// <summary>主页按钮上的关卡号。第 1 关是 1。无尽模式用累计进度，避免序号从头算把老玩家挡在门槛外。</summary>
    static int CurrentShowLevel()
    {
        if (!GPUserDataManager.HasInit() || GPUserDataManager.Instance == null)
        {
            return 0;
        }

        return GPUserDataManager.Instance.GetPassLevelCnt() + 1;
    }

    static string ReadConfig()
    {
        return EwingSoft.GameSDK.FirebaseSdk.GetStringRemoteConfig(RemoteConfigKey);
    }

    static bool IsFreshRemoteConfig()
    {
        try
        {
            return EwingSoft.GameSDK.FirebaseSdk.GetRemoteConfigUpdateStatus();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AppUpdate] 读取远端更新状态失败 " + e.Message);
            return false;
        }
    }

    static void EnsureConfigured()
    {
        if (s_configured)
        {
            return;
        }

        s_configured = true;
        AppUpdateService.Configure(new AppUpdateHostOptions
        {
            MinShowLevel = 3,
            DefaultStoreUrl = "market://details?id=" + AndroidPackage,
            DefaultFallbackUrl = "https://play.google.com/store/apps/details?id=" + AndroidPackage,
            Translate = term => I2LocalizeMgr.GetString(term),
            TitleTerm = "app_update_title",
            MessageTerm = "app_update_message",
            ConfirmTerm = "app_update_confirm",
            SkipTerm = "app_update_later",
            SoftDismissPrefsKey = "XSort_app_update_soft_dismissed_version",
        });
    }
}
