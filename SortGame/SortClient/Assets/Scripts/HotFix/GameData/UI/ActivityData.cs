using System.Collections.Generic;
using cfg;
using Core.Tools.MethodExtension;
using FishFramework;
using XSortGameCore;
using UnityEngine;

public class ActivityData : BaseData
{
    public const int CircleSlotCount = 11;
    public const int CircleMaxDailySpin = 5;
    public const int CircleGrandPrizeId = 1;
    public const int CircleGrandPityMinSpin = 3;
    public const int CircleGrandPityMiss = 15;
    public const int CircleSecondPityMinSpin = 4;
    public const int CircleSecondPityMiss = 220;

    public static readonly int[] CircleSecondPrizeIds = { 5, 8 };

    /// <summary>全局七日连胜是否对当前用户开放（硬开关 + GM / 通关第3关后）。</summary>
    public bool IsDailyVictoryActEnabled()
    {
        if (!ActivityBuildFlags.DailyVictoryActEnabled) return false;
        if (GameData.IsGmServerTimeOverride) return true;
        if (GPUserDataManager.Instance == null) return false;
        if (GPUserDataManager.Instance.IsGmForceDailyVictoryActEnabled()) return true;
        if (GPUserDataManager.GPUserData == null) return false;
        if (!GameData.UnlockData.GetDailyVictoryUnlock()) return false;
        return true;
    }

    /// <summary>
    /// 头像框换框入口：始终可用，不依赖七日连胜硬开关。
    /// </summary>
    public static bool IsDailyVictoryAvatarFrameEnabled()
    {
        return true;
    }

    /// <summary>关卡主题是否启用。</summary>
    public static bool IsRandomXSortBaseThemeEnabled() => ActivityBuildFlags.RandomXSortBaseThemeEnabled;

    /// <summary>经典随机底板（XSortBaseStyle）是否启用。只换棋子底板、Unknown 和破碎材质。</summary>
    public static bool IsClassicRandomXSortBaseEnabled() => ActivityBuildFlags.ClassicRandomXSortBaseEnabled;

    /// <summary>金币关卡是否启用。</summary>
    public static bool IsCoinRewardLevelEnabled() => ActivityBuildFlags.CoinRewardLevelEnabled;

    /// <summary>颜色挑战是否启用。</summary>
    public static bool IsColorChallengeEnabled() => ActivityBuildFlags.ColorChallengeEnabled;

    /// <summary>每日转盘入口是否启用。</summary>
    public bool IsCircleAwardEnabled() => ActivityBuildFlags.CircleAwardEnabled;

    public override void OnInit()
    {
    }

    public override void OnReset()
    {
    }

    public override void OnRemove()
    {

    }

    public class OneCellData
    {
        public int cfg;
    }

    public class DailyWinRewardTipData
    {
        public string CountText;
        public bool UseAtlasSprite;
        public string SpriteDirectory;
        public string SpriteName;
        public string FallbackSpriteDirectory;
        public string FallbackSpriteName;
        public bool IsAvatarFrame;
    }

    public class DailyWinRewardViewData
    {
        public cfg.Game.DailyVictoryRewardCfg RewardCfg;
        public List<DailyWinRewardTipData> TipRewardList = new();
        public bool IsChecked;
    }
    //所有转盘奖励列表
    public List<cfg.Game.ActivityCircleAwardCfg> GetActivityCircleAwardCfgs()
    {
        return ConfigHelper.Tables.TbActivityCircleAwardCfg.DataList;
    }

    public List<cfg.Game.ActivityCircleInfoCfg> GetActivityCircleInfoCfgs()
    {
        var list = new List<cfg.Game.ActivityCircleInfoCfg>(ConfigHelper.Tables.TbCSVActivityCircleInfoCfg.DataList);
        list.Sort((a, b) => a.Id.CompareTo(b.Id));
        return list;
    }

    public List<cfg.Game.DailyVictoryRewardCfg> GetDailyVictoryRewardCfgs()
    {
        var dataList = new List<cfg.Game.DailyVictoryRewardCfg>(ConfigHelper.Tables.TbDailyVictoryRewardCfg.DataList);
        dataList.Sort((a, b) => a.Day.CompareTo(b.Day));
        return dataList;
    }

    public int GetDailyVictoryStreakDay()
    {
        SyncDailyVictoryCycleDayFromTotal();
        return GPUserDataManager.GPUserData.dailyVictoryStreakDay;
    }

    public int GetDailyVictoryTotalWinDays()
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData.dailyVictoryTotalWinDays <= 0 && userData.dailyVictoryStreakDay > 0)
        {
            userData.dailyVictoryTotalWinDays = userData.dailyVictoryStreakDay;
        }

        SyncDailyVictoryCycleDayFromTotal();
        return userData.dailyVictoryTotalWinDays;
    }

    public string GetDailyVictoryStreakButtonLabel()
    {
        return $"\u00D7{GetDailyVictoryTotalWinDays()}";
    }

    public bool HasDailyVictoryWinToday()
    {
        if (!IsDailyVictoryActEnabled())
        {
            return false;
        }

        return GPUserDataManager.GPUserData.HasDailyVictoryWinToday();
    }

    public string GetDailyVictoryMainMenuFireIconSpriteName()
    {
        return HasDailyVictoryWinToday() ? "DailyWin_fire" : "DailyWin_fire_grey";
    }

    void SyncDailyVictoryCycleDayFromTotal()
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData.dailyVictoryTotalWinDays <= 0)
        {
            return;
        }

        int cycleDay = GPUserData.CalcDailyVictoryCycleDay(userData.dailyVictoryTotalWinDays);
        if (userData.dailyVictoryStreakDay != cycleDay)
        {
            userData.dailyVictoryStreakDay = cycleDay;
        }
    }

    public void TryResetDailyVictoryStreakOnLogin()
    {
        if (!IsDailyVictoryActEnabled())
        {
            return;
        }

        var userData = GPUserDataManager.GPUserData;
        if (userData == null)
        {
            return;
        }

        // 活动重开补偿：须在日切清零之前执行
        userData.TryApplyDailyVictoryReopenStreakPatch();
        userData.TryResetDailyVictoryStreakOnLogin();
        // 连胜未满 7 天：不应拥有头像框
        GPUserDataManager.Instance.SyncWinStreakAvatarFrameWithStreak();
    }

    public bool RecordDailyVictoryWin()
    {
        if (!IsDailyVictoryActEnabled())
        {
            return false;
        }

        // 未进主城直接开战时也先补丁，避免首胜按断连重计
        GPUserDataManager.GPUserData.TryApplyDailyVictoryReopenStreakPatch();
        bool recorded = GPUserDataManager.GPUserData.RecordDailyVictoryWin(out bool hadWinYesterday);
        if (recorded)
        {
            // 连胜未满 7 天（含断连胜重开）：收回连胜头像框
            GPUserDataManager.Instance.SyncWinStreakAvatarFrameWithStreak();

            GPUserDataManager.Instance.SetPendingDailyVictorySettlementDlg(true);
            GPUserDataManager.Instance.SetHadWinYesterdayBeforeTodayFirstWin(hadWinYesterday);
            GPUserDataManager.Instance.SaveData();
            GameEvent.Send((int)EventNameDef.DailyVictoryWinRecorded);
        }

        return recorded;
    }

    public bool ConsumePendingSettlementDailyWinDlg()
    {
        if (!IsDailyVictoryActEnabled())
        {
            return false;
        }

        return GPUserDataManager.Instance.ConsumePendingDailyVictorySettlementDlg();
    }

    public bool HadWinYesterdayBeforeTodayFirstWin()
    {
        return GPUserDataManager.Instance.HadWinYesterdayBeforeTodayFirstWin();
    }

    public cfg.Game.DailyVictoryRewardCfg GetRewardCfgByStreakDay(int day)
    {
        foreach (var rewardCfg in GetDailyVictoryRewardCfgs())
        {
            if (rewardCfg.Day == day)
            {
                return rewardCfg;
            }
        }

        return null;
    }

    public bool HasRewardContent(cfg.Game.DailyVictoryRewardCfg rewardCfg)
    {
        if (rewardCfg == null)
        {
            return false;
        }

        if (rewardCfg.AvatarFrameId > 0)
        {
            return true;
        }

        return rewardCfg.Award != null && rewardCfg.Award.Count > 0;
    }

    public cfg.Game.DailyVictoryRewardCfg GetCurrentStreakRewardCfg()
    {
        return GetRewardCfgByStreakDay(GetDailyVictoryStreakDay());
    }

    public bool HasClaimableStreakReward()
    {
        if (!IsDailyVictoryActEnabled())
        {
            return false;
        }

        int streakDay = GetDailyVictoryStreakDay();
        if (streakDay <= 0)
        {
            return false;
        }

        var rewardCfg = GetCurrentStreakRewardCfg();
        if (!HasRewardContent(rewardCfg))
        {
            return false;
        }

        int totalWinDays = GetDailyVictoryTotalWinDays();
        return GPUserDataManager.GPUserData.dailyVictoryLastClaimedTotalWinDays < totalWinDays;
    }

    /// <summary>
    /// 头像框（UserFrameNode 领奖展示）仅连胜周期第 7 天可领。
    /// </summary>
    public bool CanClaimDailyVictoryAvatarFrame()
    {
        if (!HasClaimableStreakReward())
        {
            return false;
        }

        var rewardCfg = GetCurrentStreakRewardCfg();
        return rewardCfg != null
            && rewardCfg.Day == 7
            && rewardCfg.AvatarFrameId > 0
            && GetDailyVictoryStreakDay() == 7;
    }

    public void ClaimCurrentStreakReward()
    {
        if (!HasClaimableStreakReward())
        {
            return;
        }

        int streakDay = GetDailyVictoryStreakDay();
        int totalWinDays = GetDailyVictoryTotalWinDays();
        var rewardCfg = GetCurrentStreakRewardCfg();

        if (rewardCfg.Award != null)
        {
            foreach (var rewardStr in rewardCfg.Award)
            {
                if (TryParseRewardString(rewardStr, out var rewardId, out var rewardCount))
                {
                    GPUserDataManager.Instance.AddCommonItemData(rewardId, rewardCount);
                }
            }
        }

        // UserFrameNode / 头像框：仅第 7 天发放，避免配置误配或错误入口提前解锁
        if (rewardCfg.Day == 7
            && streakDay == 7
            && rewardCfg.AvatarFrameId > 0)
        {
            GPUserDataManager.Instance.GrantAvatarFrame(rewardCfg.AvatarFrameId);
        }

        GPUserDataManager.GPUserData.dailyVictoryLastClaimedStreakDay = streakDay;
        GPUserDataManager.GPUserData.dailyVictoryLastClaimedTotalWinDays = totalWinDays;
        SaveController.Save();
    }

    public List<DailyWinRewardViewData> GetDailyWinRewardViewDatas()
    {
        var result = new List<DailyWinRewardViewData>();
        var rewardCfgs = GetDailyVictoryRewardCfgs();
        int streakDay = GetDailyVictoryStreakDay();

        foreach (var rewardCfg in rewardCfgs)
        {
            result.Add(new DailyWinRewardViewData
            {
                RewardCfg = rewardCfg,
                TipRewardList = BuildDailyWinRewardTipList(rewardCfg),
                IsChecked = streakDay >= rewardCfg.Day,
            });
        }

        return result;
    }

    /// <param name="includeOwnedAvatarFrame">
    /// true：领奖界面始终带头像框；false：boxTips 已拥有则不展示头像框。
    /// </param>
    public List<DailyWinRewardTipData> BuildDailyWinRewardTipList(
        cfg.Game.DailyVictoryRewardCfg rewardCfg,
        bool includeOwnedAvatarFrame = false)
    {
        var result = new List<DailyWinRewardTipData>();
        if (rewardCfg == null)
        {
            return result;
        }

        var avatarFrameCfg = GetAvatarFrameCfg(rewardCfg.AvatarFrameId);
        if (avatarFrameCfg != null
            && (includeOwnedAvatarFrame || !OwnsDailyVictoryAvatarFrame(rewardCfg.AvatarFrameId)))
        {
            result.Add(CreateAvatarFrameTipData(avatarFrameCfg));
        }

        if (rewardCfg.Award != null)
        {
            foreach (var rewardStr in rewardCfg.Award)
            {
                if (TryParseRewardString(rewardStr, out var rewardId, out var rewardCount))
                {
                    result.Add(CreateCommonRewardTipData(rewardId, rewardCount));
                }
            }
        }

        return result;
    }

    static bool OwnsDailyVictoryAvatarFrame(int avatarFrameId)
    {
        return avatarFrameId > 0
            && GPUserDataManager.HasInit()
            && GPUserDataManager.Instance.HasActiveWinStreakAvatarFrame()
            && GPUserDataManager.Instance.IsAvatarFrameUnlocked(avatarFrameId);
    }

    private static DailyWinRewardTipData CreateAvatarFrameTipData(cfg.Game.AvatarFrameCfg avatarFrameCfg)
    {
        var durationText = avatarFrameCfg.IsLimit == 1 && avatarFrameCfg.LimitDay > 0
            ? $"{avatarFrameCfg.LimitDay}D"
            : "x1";

        return new DailyWinRewardTipData
        {
            UseAtlasSprite = true,
            SpriteDirectory = "UserInfo",
            SpriteName = avatarFrameCfg.Pic,
            FallbackSpriteDirectory = "LevelAward",
            FallbackSpriteName = "item_1",
            CountText = durationText,
            IsAvatarFrame = true,
        };
    }

    public cfg.Game.AvatarFrameCfg GetAvatarFrameCfg(int avatarFrameId)
    {
        if (avatarFrameId <= 0)
        {
            return null;
        }

        return ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(avatarFrameId);
    }

    public bool TryParseRewardString(string rewardStr, out int rewardId, out int rewardCount)
    {
        rewardId = 0;
        rewardCount = 0;

        if (string.IsNullOrWhiteSpace(rewardStr))
        {
            return false;
        }

        var trimmedReward = rewardStr.Trim();
        var rewardParts = trimmedReward.Split(',');
        if (rewardParts.Length == 2)
        {
            return int.TryParse(rewardParts[0], out rewardId) && int.TryParse(rewardParts[1], out rewardCount);
        }

        // 兼容导出的脏数据，如 "4100" => 4,100
        if (rewardParts.Length == 1 && trimmedReward.Length > 1)
        {
            return int.TryParse(trimmedReward.Substring(0, 1), out rewardId)
                && int.TryParse(trimmedReward.Substring(1), out rewardCount);
        }

        return false;
    }

    private DailyWinRewardTipData CreateCommonRewardTipData(int rewardId, int rewardCount)
    {
        if (rewardId == 4)
        {
            return new DailyWinRewardTipData
            {
                UseAtlasSprite = false,
                SpriteDirectory = "ShopIcon",
                SpriteName = GameData.Shop.GetCoinSpritePathByCnt(rewardCount),
                CountText = "x" + rewardCount,
            };
        }

        return new DailyWinRewardTipData
        {
            UseAtlasSprite = false,
            SpriteDirectory = "LevelAward",
            SpriteName = "item_" + rewardId,
            CountText = "x" + rewardCount,
        };
    }

    //根据本轮奖池与保底规则抽取奖励（不改存档，发奖时再 ApplyCircleAwardDrawResult）
    public cfg.Game.ActivityCircleAwardCfg GetRandomAwardByCnt(int useCnt)
    {
        if (useCnt < 0)
        {
            return null;
        }

        var userData = GPUserDataManager.Instance?.UserData;
        userData?.TryResetCircleAwardCountsByDaily5AM();

        var list = GetActivityCircleAwardCfgs();
        if (list == null || list.Count == 0)
        {
            return null;
        }

        List<cfg.Game.ActivityCircleAwardCfg> pool = new();
        foreach (var item in list)
        {
            if (IsInRoundPool(item, useCnt, userData))
            {
                pool.Add(item);
            }
        }

        if (pool.Count == 0)
        {
            return null;
        }

        int spinNumber = useCnt + 1;
        int grandMiss = userData != null ? userData.circleGrandMissCnt : 0;
        int secondMiss = userData != null ? userData.circleSecondMissCnt : 0;

        var grandCfg = FindCfgById(pool, CircleGrandPrizeId);
        if (spinNumber >= CircleGrandPityMinSpin && grandMiss >= CircleGrandPityMiss && grandCfg != null)
        {
            return grandCfg;
        }

        List<cfg.Game.ActivityCircleAwardCfg> secondPool = new();
        for (int i = 0; i < pool.Count; i++)
        {
            if (IsCircleSecondPrize(pool[i].Id))
            {
                secondPool.Add(pool[i]);
            }
        }

        if (spinNumber >= CircleSecondPityMinSpin && secondMiss >= CircleSecondPityMiss && secondPool.Count > 0)
        {
            return PickWeighted(secondPool, useCnt);
        }

        return PickWeighted(pool, useCnt);
    }

    public void ApplyCircleAwardDrawResult(int awardId)
    {
        var userData = GPUserDataManager.Instance?.UserData;
        if (userData == null)
        {
            return;
        }

        if (IsCircleGrandPrize(awardId))
        {
            userData.ResetCircleGrandMissCnt();
            userData.AddCircleDailyRemovedId(CircleGrandPrizeId);
        }
        else
        {
            userData.AddCircleGrandMissCnt(1);
        }

        if (IsCircleSecondPrize(awardId))
        {
            userData.ResetCircleSecondMissCnt();
            for (int i = 0; i < CircleSecondPrizeIds.Length; i++)
            {
                userData.AddCircleDailyRemovedId(CircleSecondPrizeIds[i]);
            }
        }
        else
        {
            userData.AddCircleSecondMissCnt(1);
        }
    }

    public static bool IsCircleGrandPrize(int awardId)
    {
        return awardId == CircleGrandPrizeId;
    }

    public static bool IsCircleSecondPrize(int awardId)
    {
        for (int i = 0; i < CircleSecondPrizeIds.Length; i++)
        {
            if (CircleSecondPrizeIds[i] == awardId)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryParseCircleAward(string raw, out int itemId, out int count)
    {
        itemId = 0;
        count = 0;
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        var parts = raw.Split(',');
        if (parts.Length >= 2)
        {
            itemId = parts[0].ToInt();
            count = parts[1].ToInt();
            return itemId > 0 && count > 0;
        }

        var token = raw.Trim();
        if (token.Length >= 2 && token[0] >= '1' && token[0] <= '9')
        {
            itemId = token[0] - '0';
            count = token.Substring(1).ToInt();
            return count > 0;
        }

        return false;
    }

    public static List<(int itemId, int count)> ParseCircleRewardField(string reward)
    {
        var result = new List<(int itemId, int count)>();
        if (string.IsNullOrEmpty(reward))
        {
            return result;
        }

        var tokens = reward.Split('|');
        for (int i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i] != null ? tokens[i].Trim() : string.Empty;
            if (TryParseCircleAward(token, out int itemId, out int count))
            {
                result.Add((itemId, count));
            }
        }

        return result;
    }

    static bool IsInRoundPool(cfg.Game.ActivityCircleAwardCfg item, int useCnt, GPUserData userData)
    {
        if (item?.PoolArr == null || useCnt >= item.PoolArr.Count)
        {
            return false;
        }

        if (item.PoolArr[useCnt] != 1)
        {
            return false;
        }

        if (userData != null && userData.IsCircleAwardRemovedToday(item.Id))
        {
            return false;
        }

        return true;
    }

    static int GetRoundRate(cfg.Game.ActivityCircleAwardCfg item, int useCnt)
    {
        if (item == null || useCnt < 0)
        {
            return 0;
        }

        List<int> rates = item.Rate;
        if (rates == null || useCnt >= rates.Count)
        {
            return 0;
        }

        return Mathf.Max(0, rates[useCnt]);
    }

    static cfg.Game.ActivityCircleAwardCfg FindCfgById(List<cfg.Game.ActivityCircleAwardCfg> pool, int id)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Id == id)
            {
                return pool[i];
            }
        }

        return null;
    }

    static cfg.Game.ActivityCircleAwardCfg PickWeighted(List<cfg.Game.ActivityCircleAwardCfg> pool, int useCnt)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        int totalRate = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            totalRate += GetRoundRate(pool[i], useCnt);
        }

        if (totalRate <= 0)
        {
            int idx = Random.Range(0, pool.Count);
            return pool[idx];
        }

        int roll = Random.Range(0, totalRate);
        int cumulative = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += GetRoundRate(pool[i], useCnt);
            if (roll < cumulative)
            {
                return pool[i];
            }
        }

        return pool[0];
    }
}