using System;
using EwingSoft.GameSDK;
using UnityEngine;
#if UNITY_IOS
using AttrSdk = EwingSoft.GameSDK.IosBridge.AttrSdk;
using ThinkingDataSdk = EwingSoft.GameSDK.IosBridge.ThinkingDataSdk;
#endif

/// <summary>
/// 插屏实验 V5。加载关、开播关、冷却读 InterAdV5Config 的本地 JSON，本版不读 Firebase。
/// 渠道优先：激励，然后 Facebook/Instagram，然后其他。国家档 T1、T2、T3 分表。巴西为 T2，不在 T1/T2 表里的读 T3。
/// 国家码只认 SDK 工具类写入存档的两位码，不读系统 Locale。本机默认不管 iOS。
/// 激励渠道不看国家码即可正式分组。其他渠道要渠道已回且国家码不是 unknown 才写入，写入后不再改。
/// 还没正式分组时，广告固定走 other T3。历史存档没有 V5 分组字段的，升级后仍可按上述条件分组。
/// 顶层 cd 是缺省。激励以及 facebook/other 的某一档写了 cd 就盖掉缺省。本地激励 cd 为 0，即无冷却。
/// 通关开启关后首条必出（本关激励除外），插屏关闭后才开始 CD。
/// 数数 cfg.version：InterAdAbTestV5_incentive，或 InterAdAbTestV5_facebook_t1 / InterAdAbTestV5_other_t2。未正式分组不上报。
/// V2 / V3 本地字段只读历史，不再写入。冷却锚点仍用 V4 字段，避免升级后人人再被当成第一条。
/// </summary>
public static class InterADABTestConfig
{
    public const string ThinkingDataConfigName = "InterAdAbTestV5";
    public const string ActiveGroup = "E";
    // 以下常量是 V4 旧数，播放不再读取。加载、开播、冷却以 InterAdV5Config 为准。
    public const int IntervalOtherD0D1 = 120;
    public const int IntervalOtherD2D3 = 105;
    public const int IntervalOtherD4Plus = 90;
    public const int IntervalT3D0D1 = 90;
    public const int IntervalT3D2D3 = 75;
    public const int IntervalT3D4Plus = 60;
    /// <summary>兼容旧名：非 T3 的 D0–D1 间隔。</summary>
    public const int UnifiedIntervalSeconds = IntervalOtherD0D1;
    /// <summary>埋点：激励渠道。</summary>
    public const string AnalyticsGroupRewardChannel = "rewardChannel";
    /// <summary>非 T3 冷启动 CD 锚点门槛（展示第 10 关）。</summary>
    public const int InterstitialStartDisplayLevel = 10;
    /// <summary>T3 冷启动 CD 锚点门槛（展示第 7 关）。</summary>
    public const int InterstitialStartDisplayLevelT3 = 7;
    /// <summary>非 T3：通关第 11 关后（GetLevelNum >= 11）才允许策略插屏；该关结束首条必出。</summary>
    public const int InterstitialFirstForceShowCompletedLevel = 11;
    /// <summary>T3：通关第 8 关后（GetLevelNum >= 8）才允许策略插屏；该关结束首条必出。</summary>
    public const int InterstitialFirstForceShowCompletedLevelT3 = 8;
    /// <summary>兼容旧名：非 T3 的开启关。</summary>
    public const int AbcdInterstitialUnlockCompletedLevel = InterstitialFirstForceShowCompletedLevel;
    /// <summary>非 T3：进入第 7 关即可预加载插屏（GetLevelNum + 1 >= 7）。</summary>
    public const int AbcdInterstitialLoadCompletedLevel = 7;
    /// <summary>T3：进入第 5 关即可预加载插屏（GetLevelNum + 1 >= 5）。</summary>
    public const int AbcdInterstitialLoadCompletedLevelT3 = 5;
    /// <summary>非 T3：进入展示第 10 关后，冷启动才可能重写已开始的 CD 锚点。</summary>
    public const int RegularRecordUnlockLevel = InterstitialStartDisplayLevel;
    static bool s_hasRecordedLoadLevelThisSession;
    static bool s_rewroteStartedCooldownThisSession;
    static string s_lastReportedThinkingDataGroup;
    static bool s_remoteConfigReady;

    // 与 GPSDKMgr.mediaSourceArr 对齐，并保留本工程原先多出的 prime。
    static readonly string[] IncentiveMediaSourceArr =
    {
        "exmox",
        "inboxdollars",
        "ayetstudios",
        "freecash",
        "adjoe",
        "myappfre",
        "offertoronew",
        "mistplay",
        "helisplaynnf",
        "taurus",
        "gnacompany",
        "adaction",
        "adaction5",
        "appsamurai",
        "prime",
        "primeis"
    };

    [Serializable]
    sealed class ThinkingDataConfigEntry
    {
        public string name;
        public string version;
    }

    [Serializable]
    sealed class ThinkingDataCurrentConfigPayload
    {
        public ThinkingDataConfigEntry[] cfg;
    }

    [Serializable]
    sealed class ThinkingDataInitialConfigPayload
    {
        public ThinkingDataConfigEntry[] cfg_init;
    }

    /// <summary>V4 非激励即参与 E 组。不再读取 V3 eligible。</summary>
    public static bool IsEligible()
    {
        return GPUserDataManager.HasInit() && UsesEGroupRules();
    }

    /// <summary>非激励走 E 组规则，含归因未回时的临时 T3。</summary>
    public static bool UsesEGroupRules()
    {
        return !IsIncentiveChannel();
    }

    /// <summary>激励渠道不参与 E 组。本地激励标记优先于归因未回。</summary>
    public static bool IsIncentiveChannel()
    {
        if (!GPUserDataManager.HasInit())
            return false;

        if (GPUserDataManager.Instance.GetAdSourceTypeB1())
            return true;

        if (GPSDKMgr.Instance != null && GPSDKMgr.Instance.GetBShowTotalWin())
            return true;

        string networkStr = GetMediaSourceRaw();
        if (string.IsNullOrWhiteSpace(networkStr) || IsAttrPending(networkStr))
            return false;

        foreach (var item in IncentiveMediaSourceArr)
        {
            if (networkStr.Contains(item, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>本地已写入当前有效的 E 组。</summary>
    public static bool HasAssignedActiveGroup()
    {
        string group = GetGroup();
        if (string.IsNullOrWhiteSpace(group))
            return false;

        return string.Equals(group.Trim(), ActiveGroup, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>归因已回（非空且非 unknown）。</summary>
    public static bool IsAttributionReady()
    {
        return !string.IsNullOrWhiteSpace(GetMediaSourceForAb());
    }

    /// <summary>
    /// 当前是否允许发起插屏 LoadFullAd（仅在进关前调用）。
    /// 按 V5 当前档的 load。激励本地是 1，进关即可加载。未到加载关则跳过。
    /// </summary>
    public static bool ShouldLoadInterstitialNow()
    {
        if (!GPUserDataManager.HasInit())
            return false;

        TryAssignGroup();
        return HasReachedEGroupLoadLevel();
    }

    /// <summary>正在打或即将进入的展示关达到当前档的加载关。GetLevelNum 从 0 计。</summary>
    static bool HasReachedEGroupLoadLevel()
    {
        return GPUserDataManager.Instance.GetLevelNum() + 1 >= GetLoadDisplayLevel();
    }

    /// <summary>正在打或即将进入的展示关达到冷启动门槛。门槛为开播关减 1。</summary>
    static bool HasReachedStartDisplayLevel()
    {
        return GPUserDataManager.Instance.GetLevelNum() + 1 >= GetStartDisplayLevel();
    }

    /// <summary>已通关当前档的开启关。结算时 GetLevelNum 达到该关。</summary>
    static bool HasCompletedStartDisplayLevel()
    {
        return GPUserDataManager.Instance.GetLevelNum() >= GetUnlockCompletedLevel();
    }

    /// <summary>当前档的插屏加载展示关。</summary>
    public static int GetLoadDisplayLevel()
    {
        TryAssignGroup();
        return InterAdV5Config.GetLoadDisplayLevel();
    }

    /// <summary>当前档的插屏开启关。通关后才播。</summary>
    public static int GetUnlockCompletedLevel()
    {
        TryAssignGroup();
        return InterAdV5Config.GetPlayCompletedLevel();
    }

    /// <summary>冷启动重写 CD 的展示关门槛，等于开播关减 1。</summary>
    public static int GetStartDisplayLevel()
    {
        TryAssignGroup();
        return InterAdV5Config.GetColdStartDisplayLevel();
    }

    /// <summary>已写入的 V5 分组。空串表示还没正式分组。</summary>
    public static string GetLockedGroupKey()
    {
        if (!GPUserDataManager.HasInit() || GPUserDataManager.Instance.UserData == null)
            return "";
        return GPUserDataManager.Instance.UserData.interADGroupV5 ?? "";
    }

    /// <summary>尝试写入 V5 分组；仅当本次第一次写入时返回 true。已有分组不再改。</summary>
    public static bool TryAssignGroup()
    {
        if (!GPUserDataManager.HasInit())
            return false;

        var mgr = GPUserDataManager.Instance;
        if (mgr?.UserData == null)
            return false;

        string locked = mgr.UserData.interADGroupV5 ?? "";
        if (!string.IsNullOrEmpty(locked))
        {
            ReportThinkingDataGroup(locked);
            return false;
        }

        if (IsIncentiveChannel())
            return LockGroup(mgr, "incentive");

        if (!IsAttributionReady() || !mgr.HasResolvedSdkCountryCode())
            return false;

        string channel = FacebookChannelAdConfig.IsActive() ? "facebook" : "other";
        string tier = InterAdV5Config.TierFromSdkCountry(mgr.GetCountryCode()).ToLowerInvariant();
        return LockGroup(mgr, channel + "_" + tier);
    }

    static bool LockGroup(GPUserDataManager mgr, string key)
    {
        if (mgr?.UserData == null || string.IsNullOrEmpty(key))
            return false;
        if (!string.IsNullOrEmpty(mgr.UserData.interADGroupV5))
            return false;

        mgr.UserData.interADGroupV5 = key;
        mgr.SaveData();
        Debug.Log($"[InterADAB] lock group={key} source={GetMediaSourceRaw()} country={mgr.GetCountryCode()}");
        ReportThinkingDataGroup(key);
        return true;
    }

    public static void MarkRemoteConfigReady()
    {
        s_remoteConfigReady = true;
    }

    public static bool IsRemoteConfigReady()
    {
#if UNITY_EDITOR
        return true;
#else
        return s_remoteConfigReady;
#endif
    }

    public static string GetGroup()
    {
        if (!GPUserDataManager.HasInit())
            return "";
        return GPUserDataManager.Instance.UserData.interADTestGroupV4 ?? "";
    }

    /// <summary>
    /// 分桶 ID：数数 distinctId → UID → deviceUniqueIdentifier。
    /// </summary>
    public static string GetStableBucketId()
    {
        string distinctId = GetDistinctId();
        if (!string.IsNullOrWhiteSpace(distinctId))
            return distinctId.Trim();

        if (GPUserDataManager.HasInit())
        {
            string uid = GPUserDataManager.Instance.GetUID();
            if (!string.IsNullOrWhiteSpace(uid) && uid.Length > 2)
                return uid;
        }

        string deviceId = SystemInfo.deviceUniqueIdentifier;
        return string.IsNullOrWhiteSpace(deviceId) ? "" : deviceId;
    }

    public static string GetDistinctId()
    {
        try
        {
            return ThinkingDataSdk.GetDistinctId() ?? "";
        }
        catch (Exception e)
        {
            Debug.LogWarning("[InterADAB] GetDistinctId failed: " + e.Message);
            return "";
        }
    }

    public static int GetIntervalSeconds()
    {
        if (!GPUserDataManager.HasInit())
            return 0;

        TryAssignGroup();
        return InterAdV5Config.GetBaseCdSeconds();
    }

    /// <summary>安装日（firstDayLocalDate）起第几天，本地日历日差，首日=0。</summary>
    public static int GetDayIndex()
    {
        if (!GPUserDataManager.HasInit())
            return 0;

        int first = GPUserDataManager.Instance.UserData.firstDayLocalDate;
        if (first <= 0)
            return 0;

        if (!TryDateIntToDate(first, out DateTime firstDate) || !TryDateIntToDate(TodayLocalDateInt(), out DateTime today))
            return 0;

        return Math.Max(0, (today - firstDate).Days);
    }

    /// <summary>当前档、当前安装日的实际 CD。本档三段都是 0 时为 0。</summary>
    public static int GetEffectiveIntervalSeconds()
    {
        if (!GPUserDataManager.HasInit())
            return 0;

        TryAssignGroup();
        if (!InterAdV5Config.HasCooldown())
            return 0;

        return InterAdV5Config.GetCdSeconds(GetDayIndex());
    }

    /// <summary>埋点：当前 CD 锚点 Unix 秒；未开始计时为 0。</summary>
    public static long GetCooldownUnixForAnalytics()
    {
        if (!GPUserDataManager.HasInit() || GPUserDataManager.Instance.UserData == null)
            return 0;
        return Math.Max(0, GPUserDataManager.Instance.UserData.interADCooldownUnixV4);
    }

    /// <summary>SDK 存档国家码不在 T1/T2 表里则视为 T3。未取到国家码也视为 T3。不读系统 Locale。</summary>
    public static bool IsT3Country()
    {
        if (!GPUserDataManager.HasInit() || !GPUserDataManager.Instance.HasResolvedSdkCountryCode())
            return true;

        string code = GPUserDataManager.Instance.GetCountryCode();
        return GPSDKMgr.Instance == null || !GPSDKMgr.Instance.IsT1T2CountryCode(code);
    }

    /// <summary>
    /// 通关开启关后才播。锚点 0 为首条必出；已关播则按当前档 CD。
    /// 本档没有冷却时，过了开启关就放行。
    /// </summary>
    public static bool CanShowByInterval()
    {
        if (!GPUserDataManager.HasInit())
            return true;

        TryAssignGroup();

        if (!HasCompletedStartDisplayLevel())
            return false;

        int intervalSec = GetEffectiveIntervalSeconds();
        if (intervalSec <= 0)
            return true;

        long last = GPUserDataManager.Instance.UserData.interADCooldownUnixV4;
        if (last <= 0)
            return true;

        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        return now - last >= intervalSec;
    }

    /// <summary>E 组间隔拦住时的互斥原因。仅在 CanShowByInterval 为 false 后调用。</summary>
    public static string GetIntervalBlockReason()
    {
        if (!HasCompletedStartDisplayLevel())
            return BAInterAdNoShowData.ReasonLevelLocked;
        if (s_rewroteStartedCooldownThisSession)
            return BAInterAdNoShowData.ReasonSessionReset;
        return BAInterAdNoShowData.ReasonECdWaiting;
    }

    /// <summary>
    /// 记录点 A：冷启动后第一次进关，若 CD 已因插屏关闭开始过，则重写锚点。
    /// 通关开启关前、以及首条插屏关闭前，都不计时。
    /// </summary>
    public static void TryRecordLoadLevel()
    {
        if (!GameData.BFromStartScene)
            return;
        if (s_hasRecordedLoadLevelThisSession)
            return;
        if (!GPUserDataManager.HasInit())
            return;

        TryAssignGroup();
        if (!InterAdV5Config.HasCooldown())
            return;

        if (!HasReachedStartDisplayLevel())
            return;

        s_hasRecordedLoadLevelThisSession = true;
        var userData = GPUserDataManager.Instance.UserData;
        if (!HasCompletedStartDisplayLevel() || userData.interADCooldownUnixV4 <= 0)
        {
            if (userData.interADCooldownUnixV4 > 0)
            {
                userData.interADCooldownUnixV4 = 0;
                GPUserDataManager.Instance.SaveData();
                Debug.Log("[InterADAB] clear pre-first-show cooldown before level " + GetUnlockCompletedLevel() + " complete");
                RefreshAdStrategyAnalytics();
            }
            return;
        }

        userData.interADCooldownUnixV4 = DateTimeOffset.Now.ToUnixTimeSeconds();
        s_rewroteStartedCooldownThisSession = true;
        GPUserDataManager.Instance.SaveData();
        Debug.Log($"[InterADAB] record A loadLevel group={GetGroup()} level={GPUserDataManager.Instance.GetLevelNum()} sessionFirst=true");
        RefreshAdStrategyAnalytics();
    }

    /// <summary>记录点 B：任意插屏关闭回游戏。首条也从此刻开始 CD。</summary>
    public static void OnInterstitialClosed()
    {
        if (!GPUserDataManager.HasInit())
            return;

        TryAssignGroup();
        if (!InterAdV5Config.HasCooldown())
            return;

        GPUserDataManager.Instance.UserData.interADCooldownUnixV4 = DateTimeOffset.Now.ToUnixTimeSeconds();
        GPUserDataManager.Instance.SaveData();
        Debug.Log($"[InterADAB] record B closed group={GetGroup()}");
        RefreshAdStrategyAnalytics();
    }

    static void RefreshAdStrategyAnalytics()
    {
        GameBAData.ExcuteCommonEvent();
    }

    /// <summary>埋点用：已正式分组的 incentive / facebook_t1 / other_t3。未分组为空串。</summary>
    public static string GetTestGroupForAnalytics()
    {
        if (!GPUserDataManager.HasInit())
            return "";

        TryAssignGroup();
        return GetLockedGroupKey();
    }

    public static string GetConfigForAnalytics()
    {
        return "V5;local;cdT1=120|105|90;cdT2=120|105|90;cdT3=90|75|60;inc=load1/play3/cd0;fb=load5/play8;otherT1=load7/play11;otherT3=load5/play8";
    }

    public static string GetThinkingDataConfigVersion(string testGroup)
    {
        string locked = GetLockedGroupKey();
        if (string.IsNullOrEmpty(locked))
            return "";

        return ThinkingDataConfigName + "_" + locked;
    }

    static void ReportThinkingDataGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
            return;

        string version = GetThinkingDataConfigVersion(group);
        if (string.IsNullOrWhiteSpace(version))
            return;

        if (string.Equals(
                s_lastReportedThinkingDataGroup,
                version,
                StringComparison.OrdinalIgnoreCase))
            return;

        var entry = new ThinkingDataConfigEntry
        {
            name = ThinkingDataConfigName,
            version = version
        };
        var currentPayload = new ThinkingDataCurrentConfigPayload
        {
            cfg = new[] { entry }
        };
        var initialPayload = new ThinkingDataInitialConfigPayload
        {
            cfg_init = new[] { entry }
        };

        string currentJson = JsonUtility.ToJson(currentPayload);
        string initialJson = JsonUtility.ToJson(initialPayload);
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ThinkingDataSdk.UserSet(currentJson);
            ThinkingDataSdk.UserSetOnce(initialJson);
            ThinkingDataSdk.SetPublicProperties(currentJson);
#endif
        }
        catch (Exception e)
        {
            Debug.LogWarning("[InterADAB] ThinkingData user properties failed: " + e.Message);
        }

        s_lastReportedThinkingDataGroup = version;
        Debug.Log(
            $"[InterADAB][ThinkingData] cfg.name={entry.name}, cfg.version={entry.version}");
    }

    static bool ClearStoredGroup(GPUserDataManager mgr)
    {
        if (mgr?.UserData == null)
            return false;

        string prev = mgr.UserData.interADTestGroupV4 ?? "";
        if (string.IsNullOrWhiteSpace(prev))
            return false;

        mgr.UserData.interADTestGroupV4 = "";
        mgr.SaveData();
        Debug.Log($"[InterADAB] clear group prev={prev} source={GetMediaSourceRaw()}");
        return true;
    }

    static bool AssignActiveGroup(GPUserDataManager mgr)
    {
        if (mgr?.UserData == null)
            return false;

        string prev = mgr.UserData.interADTestGroupV4 ?? "";
        if (string.Equals(prev, ActiveGroup, StringComparison.OrdinalIgnoreCase))
        {
            ReportThinkingDataGroup(ActiveGroup);
            return false;
        }

        mgr.UserData.interADTestGroupV4 = ActiveGroup;
        mgr.SaveData();
        Debug.Log($"[InterADAB] assign group={ActiveGroup} prev={prev} source={GetMediaSourceRaw()}");
        ReportThinkingDataGroup(ActiveGroup);
        return true;
    }

    static string GetMediaSourceRaw()
    {
        if (GPSDKMgr.Instance != null)
        {
            string source = GPSDKMgr.Instance.GetMediaSource();
            if (!string.IsNullOrWhiteSpace(source))
                return source;
        }

        try
        {
            return AttrSdk.GetAttrNetwork() ?? "";
        }
        catch (Exception e)
        {
            Debug.LogWarning("[InterADAB] GetAttrNetwork failed: " + e.Message);
            return "";
        }
    }

    /// <summary>iOS Attr 桩值为 unknown，视为归因未回。</summary>
    static bool IsAttrPending(string source)
    {
        return string.IsNullOrWhiteSpace(source)
            || string.Equals(source, "unknown", StringComparison.OrdinalIgnoreCase);
    }

    static string GetMediaSourceForAb()
    {
        string source = GetMediaSourceRaw();
        return IsAttrPending(source) ? "" : source.Trim();
    }

    static int TodayLocalDateInt()
    {
        DateTime localNow = GameData.IsGmServerTimeOverride
            ? GameData.GetGameLocalDateTime()
            : DateTime.Now;
        return localNow.Year * 10000 + localNow.Month * 100 + localNow.Day;
    }

    static bool TryDateIntToDate(int dateInt, out DateTime date)
    {
        date = default;
        int y = dateInt / 10000;
        int m = dateInt % 10000 / 100;
        int d = dateInt % 100;
        if (y < 2000 || m < 1 || m > 12 || d < 1 || d > 31)
            return false;

        try
        {
            date = new DateTime(y, m, d);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
