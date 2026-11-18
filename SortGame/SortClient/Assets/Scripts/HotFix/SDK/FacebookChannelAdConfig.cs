using System;
using UnityEngine;
using EwingSoft.GameSDK;
#if UNITY_IOS
using AttrSdk = EwingSoft.GameSDK.IosBridge.AttrSdk;
#endif

/// <summary>
/// Facebook / Instagram 归因渠道识别。渠道名模糊包含 facebook 或 instagram，忽略大小写（如 Facebook Ads）。
/// 加载关、开播关、冷却在 InterAdV5Config，不在这里。
/// 不改 Banner。归因未回时不走本档。
/// </summary>
public static class FacebookChannelAdConfig
{
    static readonly string[] NetworkKeys = { "facebook", "instagram" };

    /// <summary>归因已回且渠道名含 facebook 或 instagram（忽略大小写）。激励渠道不走本档。</summary>
    public static bool IsActive()
    {
        if (!GPUserDataManager.HasInit())
            return false;

        if (GPUserDataManager.Instance.GetAdSourceTypeB1())
            return false;

        if (GPSDKMgr.Instance != null && GPSDKMgr.Instance.GetBShowTotalWin())
            return false;

        string network = ReadNetwork();
        if (string.IsNullOrWhiteSpace(network)
            || string.Equals(network, "unknown", StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (var key in NetworkKeys)
        {
            if (network.Contains(key, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static string ReadNetwork()
    {
        try
        {
            if (GPSDKMgr.Instance != null)
            {
                string source = GPSDKMgr.Instance.GetMediaSource();
                if (!string.IsNullOrWhiteSpace(source))
                    return source;
            }

            return AttrSdk.GetAttrNetwork() ?? "";
        }
        catch (Exception e)
        {
            Debug.LogWarning("[FacebookAd] GetAttrNetwork failed: " + e.Message);
            return "";
        }
    }
}
