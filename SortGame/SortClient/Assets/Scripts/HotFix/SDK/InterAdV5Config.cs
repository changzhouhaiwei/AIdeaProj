using System;
using UnityEngine;

/// <summary>
/// 插屏 V5 参数。形状与以后的 Remote Config key <see cref="RemoteConfigKey"/> 相同。
/// 本版只解析包内 JSON，不读 Firebase。
/// 顶层 cd 按国家档缺省。incentive，以及 facebook / other 的某一档，写了 cd 就用自己的。
/// 冷启动门槛不在表里，按开播关减 1。
/// </summary>
public static class InterAdV5Config
{
    public const string RemoteConfigKey = "InterAdV5";

    const string LocalJson =
        "{" +
        "\"cd\":{\"T1\":[120,105,90],\"T2\":[120,105,90],\"T3\":[90,75,60]}," +
        "\"incentive\":{\"load\":1,\"play\":3,\"cd\":[0,0,0]}," +
        "\"facebook\":{" +
        "\"T1\":{\"load\":5,\"play\":8}," +
        "\"T2\":{\"load\":5,\"play\":8}," +
        "\"T3\":{\"load\":5,\"play\":8}" +
        "}," +
        "\"other\":{" +
        "\"T1\":{\"load\":7,\"play\":11}," +
        "\"T2\":{\"load\":7,\"play\":11}," +
        "\"T3\":{\"load\":5,\"play\":8}" +
        "}" +
        "}";

    const string TierT1 = "T1";
    const string TierT2 = "T2";
    const string TierT3 = "T3";

    static Root s_root;

    [Serializable]
    public class CdByTier
    {
        public int[] T1;
        public int[] T2;
        public int[] T3;
    }

    [Serializable]
    public class LevelRule
    {
        public int load;
        public int play;
        public int[] cd;
    }

    [Serializable]
    public class TierRules
    {
        public LevelRule T1;
        public LevelRule T2;
        public LevelRule T3;
    }

    [Serializable]
    public class Root
    {
        public CdByTier cd;
        public LevelRule incentive;
        public TierRules facebook;
        public TierRules other;
    }

    public static int GetLoadDisplayLevel()
    {
        LevelRule rule = GetActiveRule(out _);
        return rule != null && rule.load > 0 ? rule.load : 1;
    }

    public static int GetPlayCompletedLevel()
    {
        LevelRule rule = GetActiveRule(out _);
        return rule != null && rule.play > 0 ? rule.play : 1;
    }

    /// <summary>进入该展示关后，冷启动才重写已开始的 CD。开播关减 1，至少为 1。</summary>
    public static int GetColdStartDisplayLevel()
    {
        return Math.Max(1, GetPlayCompletedLevel() - 1);
    }

    /// <summary>安装日分段后的冷却秒数。三段都是 0 表示无冷却。</summary>
    public static int GetCdSeconds(int dayIndex)
    {
        LevelRule rule = GetActiveRule(out string tier);
        int[] cd = ResolveCd(rule, tier);
        if (!IsCd(cd))
            return 0;
        if (dayIndex >= 4)
            return Math.Max(0, cd[2]);
        if (dayIndex >= 2)
            return Math.Max(0, cd[1]);
        return Math.Max(0, cd[0]);
    }

    public static bool HasCooldown()
    {
        LevelRule rule = GetActiveRule(out string tier);
        int[] cd = ResolveCd(rule, tier);
        return IsCd(cd) && (cd[0] > 0 || cd[1] > 0 || cd[2] > 0);
    }

    /// <summary>D0–D1 秒数，给埋点看缺省档。实际等待用 <see cref="GetCdSeconds"/>。</summary>
    public static int GetBaseCdSeconds()
    {
        return GetCdSeconds(0);
    }

    /// <summary>
    /// 用 SDK 已保存的国家码分档。T1 表为 T1，T2 表为 T2，其余为 T3。
    /// 不读系统 Locale，也不用系统语言。
    /// </summary>
    public static string TierFromSdkCountry(string countryCode)
    {
        if (GPSDKMgr.Instance == null)
            return TierT3;
        if (GPSDKMgr.Instance.IsT2CountryCode(countryCode))
            return TierT2;
        if (GPSDKMgr.Instance.IsT1CountryCode(countryCode))
            return TierT1;
        return TierT3;
    }

    /// <summary>未正式分组时固定 other + T3。已分组则按存档，不再按当前渠道或国家重算。</summary>
    static LevelRule GetActiveRule(out string tier)
    {
        Root root = GetRoot();
        string locked = InterADABTestConfig.GetLockedGroupKey();
        if (string.IsNullOrEmpty(locked))
        {
            tier = TierT3;
            return Pick(root.other, TierT3);
        }

        if (locked == "incentive")
        {
            tier = TierT3;
            return root.incentive;
        }

        ParseLocked(locked, out string channel, out tier);
        TierRules rules = channel == "facebook" ? root.facebook : root.other;
        return Pick(rules, tier) ?? Pick(root.other, TierT3);
    }

    static void ParseLocked(string locked, out string channel, out string tier)
    {
        channel = "other";
        tier = TierT3;
        int split = locked.LastIndexOf('_');
        if (split <= 0 || split >= locked.Length - 1)
            return;

        string head = locked.Substring(0, split);
        string suffix = locked.Substring(split + 1).ToUpperInvariant();
        if (head == "facebook" || head == "other")
            channel = head;
        if (suffix == TierT1 || suffix == TierT2 || suffix == TierT3)
            tier = suffix;
    }

    static int[] ResolveCd(LevelRule rule, string tier)
    {
        if (IsCd(rule?.cd))
            return rule.cd;

        CdByTier defaults = GetRoot().cd;
        if (defaults == null)
            return null;

        if (tier == TierT3 && IsCd(defaults.T3))
            return defaults.T3;
        if (tier == TierT2 && IsCd(defaults.T2))
            return defaults.T2;
        return IsCd(defaults.T1) ? defaults.T1 : null;
    }

    static LevelRule Pick(TierRules rules, string tier)
    {
        if (rules == null)
            return null;
        if (tier == TierT3)
            return rules.T3;
        if (tier == TierT2)
            return rules.T2;
        return rules.T1;
    }

    static bool IsCd(int[] cd)
    {
        return cd != null && cd.Length >= 3;
    }

    static Root GetRoot()
    {
        if (s_root != null)
            return s_root;

        try
        {
            s_root = JsonUtility.FromJson<Root>(LocalJson);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[InterAdV5] local json failed: " + e.Message);
        }

        if (s_root == null || s_root.other == null || s_root.cd == null)
        {
            Debug.LogWarning("[InterAdV5] local json incomplete, use built-in fallback");
            s_root = BuildFallback();
        }

        return s_root;
    }

    static Root BuildFallback()
    {
        LevelRule fb = new LevelRule { load = 5, play = 8 };
        return new Root
        {
            cd = new CdByTier
            {
                T1 = new[] { 120, 105, 90 },
                T2 = new[] { 120, 105, 90 },
                T3 = new[] { 90, 75, 60 }
            },
            incentive = new LevelRule { load = 1, play = 3, cd = new[] { 0, 0, 0 } },
            facebook = new TierRules { T1 = fb, T2 = fb, T3 = fb },
            other = new TierRules
            {
                T1 = new LevelRule { load = 7, play = 11 },
                T2 = new LevelRule { load = 7, play = 11 },
                T3 = new LevelRule { load = 5, play = 8 }
            }
        };
    }
}
