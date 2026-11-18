using cfg.Game;
using TMPro;
using UnityEngine;

/// <summary>
/// 根据 game_tbcsvgoldtreeleaguerankcfg 解析排行榜晋级/保级/退级分界 flag 位置与文案，以及联赛段位结算。
/// </summary>
public static class GoldTreeLeagueRankFlagHelper
{
    public enum LeagueRankZone
    {
        Promote,
        Maintain,
        Demote,
    }

    public struct DividerRanks
    {
        /// <summary>flag：晋级区与平级区之间（promote 区间末尾）。</summary>
        public int PromoteMaintain;
        /// <summary>flag (1)：平级区与退级区之间（maintain 区间末尾）。</summary>
        public int MaintainDemote;
    }

    public static DividerRanks GetDividerRanks(int leagueCfgId = -1)
    {
        var cfg = ResolveCfg(leagueCfgId);
        if (cfg == null)
            return new DividerRanks { PromoteMaintain = 10, MaintainDemote = 40 };

        return new DividerRanks
        {
            PromoteMaintain = ParseRangeEnd(cfg.Promote, 10),
            MaintainDemote = ParseRangeEnd(cfg.Maintain, 40),
        };
    }

    static CSVGoldTreeLeagueRankCfg ResolveCfg(int leagueCfgId = -1)
    {
        var table = ConfigHelper.Tables?.TbCSVGoldTreeLeagueRankCfg;
        if (table == null || table.DataList == null || table.DataList.Count == 0)
            return null;

        if (leagueCfgId < 0)
        {
            leagueCfgId = GPUserDataManager.GPUserData.goldenTreeLeagueTier;
            if (!GoldTreeRobotRankRewardHelper.IsValidTierId(leagueCfgId))
                leagueCfgId = 1;
        }

        var cfg = table.GetOrDefault(leagueCfgId);
        if (cfg != null)
            return cfg;

        return table.DataList[0];
    }

    public static bool TryParseRange(string range, out int startRank, out int endRank)
    {
        startRank = 0;
        endRank = 0;
        if (string.IsNullOrEmpty(range))
            return false;

        var parts = range.Split('_');
        if (parts.Length < 2)
            return false;

        return int.TryParse(parts[0], out startRank) && int.TryParse(parts[1], out endRank);
    }

    static bool IsRankInRange(string range, int rank)
    {
        if (!TryParseRange(range, out int startRank, out int endRank))
            return false;

        return rank >= startRank && rank <= endRank;
    }

    public static LeagueRankZone GetZoneByRank(int rank, int cfgId = -1)
    {
        var cfg = ResolveCfg(cfgId);
        if (cfg != null)
        {
            if (IsRankInRange(cfg.Promote, rank))
                return LeagueRankZone.Promote;
            if (IsRankInRange(cfg.Demote, rank))
                return LeagueRankZone.Demote;
            if (IsRankInRange(cfg.Maintain, rank))
                return LeagueRankZone.Maintain;
        }

        if (rank >= 1 && rank <= 10)
            return LeagueRankZone.Promote;
        if (rank >= 41 && rank <= 50)
            return LeagueRankZone.Demote;

        return LeagueRankZone.Maintain;
    }

    public static int ResolveNextTier(int currentTier, int playerRank)
    {
        int minTier = GoldTreeRobotRankRewardHelper.MinTierId;
        int maxTier = GoldTreeRobotRankRewardHelper.MaxTierId;
        currentTier = Mathf.Clamp(currentTier, minTier, maxTier);

        int delta = GetZoneByRank(playerRank, currentTier) switch
        {
            LeagueRankZone.Promote => 1,
            LeagueRankZone.Demote => -1,
            _ => 0,
        };

        return Mathf.Clamp(currentTier + delta, minTier, maxTier);
    }

    public static int ResolveSettlementBaseTier(int settlementBoardId = -1)
    {
        var userData = GPUserDataManager.GPUserData;
        if (settlementBoardId >= 0
            && userData.goldenTreeLastSeasonBoardId == settlementBoardId
            && GoldTreeRobotRankRewardHelper.IsValidTierId(userData.goldenTreeLastSeasonSnapshotLeagueTier))
        {
            return NormalizeLeagueTier(userData.goldenTreeLastSeasonSnapshotLeagueTier);
        }

        return NormalizeLeagueTier(userData.goldenTreeLeagueTier);
    }

    /// <summary>
    /// 结算榜预览：写入升/保/降展示段位，不修改玩家当前 league tier，也不标记已正式结算。
    /// </summary>
    public static bool TryCacheSettlementDisplayTiers(int playerRank, int settlementBoardId = -1)
    {
        if (playerRank < 1)
            return false;

        var userData = GPUserDataManager.GPUserData;
        if (settlementBoardId < 0)
            settlementBoardId = userData.goldenTreeLastSeasonBoardId;
        if (settlementBoardId < 0)
            return false;

        int baseTier = ResolveSettlementBaseTier(settlementBoardId);
        int newTier = ResolveNextTier(baseTier, playerRank);
        int cachedOldTier = NormalizeLeagueTier(userData.goldenTreeLastSeasonLeagueTier);
        int cachedNewTier = NormalizeLeagueTier(userData.goldenTreeSettlementLeagueTier);
        if (cachedOldTier == baseTier && cachedNewTier == newTier)
            return false;

        userData.goldenTreeLastSeasonLeagueTier = baseTier;
        userData.goldenTreeSettlementLeagueTier = newTier;
        return true;
    }

    public static (int oldTier, int newTier) ApplyDailySettlement(int playerRank, int settlementBoardId)
    {
        var userData = GPUserDataManager.GPUserData;
        int baseTier = ResolveSettlementBaseTier(settlementBoardId);
        int newTier = ResolveNextTier(baseTier, playerRank);

        if (settlementBoardId >= 0
            && userData.goldenTreeSettlementTierAppliedBoardId == settlementBoardId
            && GoldTreeRobotRankRewardHelper.IsValidTierId(userData.goldenTreeLastSeasonLeagueTier)
            && GoldTreeRobotRankRewardHelper.IsValidTierId(userData.goldenTreeSettlementLeagueTier))
        {
            int cachedOldTier = NormalizeLeagueTier(userData.goldenTreeLastSeasonLeagueTier);
            int cachedNewTier = NormalizeLeagueTier(userData.goldenTreeSettlementLeagueTier);
            if (cachedOldTier == baseTier && cachedNewTier == newTier)
            {
                userData.goldenTreeLeagueTier = cachedNewTier;
                return (cachedOldTier, cachedNewTier);
            }
        }

        userData.goldenTreeLastSeasonLeagueTier = baseTier;
        userData.goldenTreeSettlementLeagueTier = newTier;
        userData.goldenTreeLeagueTier = newTier;
        if (settlementBoardId >= 0)
            userData.goldenTreeSettlementTierAppliedBoardId = settlementBoardId;

        return (baseTier, newTier);
    }

    public static int NormalizeLeagueTier(int tier)
    {
        if (GoldTreeRobotRankRewardHelper.IsValidTierId(tier))
            return tier;

        return GoldTreeRobotRankRewardHelper.MinTierId;
    }

    static int ParseRangeEnd(string range, int fallback)
    {
        if (string.IsNullOrEmpty(range))
            return fallback;

        var parts = range.Split('_');
        if (parts.Length < 2)
            return fallback;

        return int.TryParse(parts[1], out int endRank) ? endRank : fallback;
    }

    public static void ApplyFlagRangeTexts(RectTransform content, int leagueCfgId = -1)
    {
        if (content == null)
            return;

        var cfg = ResolveCfg(leagueCfgId);
        if (cfg == null)
            return;

        ApplyFlagRangeText(content, "flag", cfg.Promote);
        ApplyFlagRangeText(content, "flag (1)", cfg.Demote);
    }

    static void ApplyFlagRangeText(RectTransform content, string flagName, string rangeValue)
    {
        var flagRt = FindContentChild(content, flagName);
        if (flagRt == null)
            return;

        var rangeLb = FindFlagRangeLabel(flagRt);
        if (rangeLb == null)
            return;

        rangeLb.text = FormatRangeDisplay(rangeValue);
    }

    static TextMeshProUGUI FindFlagRangeLabel(RectTransform flagRt)
    {
        foreach (var label in flagRt.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.name == "range" || label.name == "rang")
                return label;
        }

        return null;
    }

    public static string FormatRangeDisplay(string range)
    {
        if (string.IsNullOrEmpty(range))
            return string.Empty;

        return range.Replace('_', '-');
    }

    static RectTransform FindContentChild(RectTransform content, string childName)
    {
        var direct = content.Find(childName) as RectTransform;
        if (direct != null)
            return direct;

        foreach (Transform child in content)
        {
            if (child.name == childName)
                return child as RectTransform;
        }

        return null;
    }
}
