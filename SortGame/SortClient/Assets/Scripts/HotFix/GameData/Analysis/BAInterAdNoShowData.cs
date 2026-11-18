using System;
using UnityEngine;

public class BAInterAdNoShowData : BaseAnaData
{
    public const string EventKey = "ad_no_show";
    public const string AdTypeInter = "INTER";
    public const string AdTypeRewarded = "REWARDED";

    public const string ReasonNoAd = "no_ad";
    public const string ReasonRvSuppress = "rv_suppress";
    public const string ReasonLevelLocked = "level_locked";
    public const string ReasonTodayFirst = "today_first";
    public const string ReasonFailStreak = "fail_streak";
    public const string ReasonWinStreak = "win_streak";
    public const string ReasonSessionReset = "session_reset";
    public const string ReasonECdWaiting = "e_cd_waiting";
    public const string ReasonNoFill = "no_fill";
    public const string ReasonAdNotLoad = "ad_not_load";

    public string ad_type;
    public string ad_place;
    public string reason;
    /// <summary>仅插屏 no_fill 上报，取 ShowFullAd 关闭回调。激励不带此字段。</summary>
    public int errorCode;

    [Serializable]
    class RewardTrackPayload
    {
        public string ad_type;
        public string ad_place;
        public string reason;
    }

    public override void ClearData()
    {
        ad_type = "";
        ad_place = "";
        reason = "";
        errorCode = 0;
    }

    /// <summary>激励点击没有关闭错误码，事件里不带 errorCode。</summary>
    public string ToTrackJson()
    {
        if (ad_type == AdTypeRewarded)
        {
            return JsonUtility.ToJson(new RewardTrackPayload
            {
                ad_type = ad_type,
                ad_place = ad_place,
                reason = reason
            });
        }

        return ToJson();
    }
}
