using System.Collections.Generic;
using cfg.Game;
using FishFramework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// game_tbgoldtreerobotrankrewardcfg 段位与名次奖励解析。
/// </summary>
public static class GoldTreeRobotRankRewardHelper
{
  const string MainMenuRankTreeSpriteDir = "Assets/CABRes/SpriteAtlas/GoldTree/UI_NewRank";
  const string LegacyMainMenuRankTreeSpriteDir = "Assets/Arts/NewEff/UI_Imasgs/UI_NewRank";
  const string FlatMainMenuRankTreeSpriteDir = "Assets/CABRes/SpriteAtlas/GoldTree";
  public enum RankRewardSlot
  {
    Rank1 = 1,
    Rank2 = 2,
    Rank3 = 3,
    Rank4To6 = 4,
    Rank7To10 = 5,
  }

  public readonly struct RewardItem
  {
    public readonly int ItemId;
    public readonly int Count;

    public RewardItem(int itemId, int count)
    {
      ItemId = itemId;
      Count = count;
    }
  }

  public readonly struct TierInfo
  {
    public readonly GoldTreeLeagueTier Tier;
    public readonly int Id;
    public readonly string Name;

    public TierInfo(GoldTreeLeagueTier tier, int id, string name)
    {
      Tier = tier;
      Id = id;
      Name = name;
    }
  }

  static readonly TierInfo[] AllTiers =
  {
    new(GoldTreeLeagueTier.Bronze, 1, "Bronze"),
    new(GoldTreeLeagueTier.Silver, 2, "Silver"),
    new(GoldTreeLeagueTier.Gold, 3, "Gold"),
    new(GoldTreeLeagueTier.Platinum, 4, "Platinum"),
    new(GoldTreeLeagueTier.Diamond, 5, "Diamond"),
    new(GoldTreeLeagueTier.Epic, 6, "Epic"),
    new(GoldTreeLeagueTier.Master, 7, "Master"),
    new(GoldTreeLeagueTier.Glory, 8, "Glory"),
  };

  public static IReadOnlyList<TierInfo> GetAllTiers() => AllTiers;

  public static int MinTierId => (int)GoldTreeLeagueTier.Bronze;
  public static int MaxTierId => (int)GoldTreeLeagueTier.Glory;

  public static bool IsValidTierId(int tierId)
  {
    return tierId >= MinTierId && tierId <= MaxTierId;
  }

  public static string GetMainMenuRankTreeSpritePath(int leagueTier = -1)
  {
    if (leagueTier < 0)
      leagueTier = GPUserDataManager.GPUserData.goldenTreeLeagueTier;

    leagueTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(leagueTier);
    return $"{MainMenuRankTreeSpriteDir}/lv{leagueTier}.png";
  }

  public static string GetLegacyMainMenuRankTreeSpritePath(int leagueTier = -1)
  {
    if (leagueTier < 0)
      leagueTier = GPUserDataManager.GPUserData.goldenTreeLeagueTier;

    leagueTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(leagueTier);
    return $"{LegacyMainMenuRankTreeSpriteDir}/lv{leagueTier}.png";
  }

  public static string GetFlatMainMenuRankTreeSpritePath(int leagueTier = -1)
  {
    if (leagueTier < 0)
      leagueTier = GPUserDataManager.GPUserData.goldenTreeLeagueTier;

    leagueTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(leagueTier);
    return $"{FlatMainMenuRankTreeSpriteDir}/lv{leagueTier}.png";
  }

  public static Sprite LoadMainMenuRankTreeSprite(int leagueTier = -1)
  {
    if (leagueTier < 0)
      leagueTier = GPUserDataManager.GPUserData.goldenTreeLeagueTier;

    leagueTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(leagueTier);

    string bundledPath = GetMainMenuRankTreeSpritePath(leagueTier);
    var sprite = UILoadHelper.LoadAsset<Sprite>(bundledPath);
    if (sprite != null)
      return sprite;

    string flatPath = GetFlatMainMenuRankTreeSpritePath(leagueTier);
    sprite = UILoadHelper.LoadAsset<Sprite>(flatPath);
    if (sprite != null)
      return sprite;

    string legacyPath = GetLegacyMainMenuRankTreeSpritePath(leagueTier);
    sprite = UILoadHelper.LoadAsset<Sprite>(legacyPath);
    if (sprite != null)
      return sprite;

#if UNITY_EDITOR
    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(bundledPath);
    if (sprite != null)
      return sprite;

    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(flatPath);
    if (sprite != null)
      return sprite;

    return AssetDatabase.LoadAssetAtPath<Sprite>(legacyPath);
#else
    return null;
#endif
  }

  public static bool TryParseTier(int tierId, out GoldTreeLeagueTier tier)
  {
    if (IsValidTierId(tierId))
    {
      tier = (GoldTreeLeagueTier)tierId;
      return true;
    }

    tier = GoldTreeLeagueTier.None;
    return false;
  }

  public static bool TryParseTier(string ranking, out GoldTreeLeagueTier tier)
  {
    tier = GoldTreeLeagueTier.None;
    if (string.IsNullOrEmpty(ranking))
      return false;

    foreach (var info in AllTiers)
    {
      if (info.Name == ranking)
      {
        tier = info.Tier;
        return true;
      }
    }

    return false;
  }

  public static TierInfo GetTierInfo(GoldTreeLeagueTier tier)
  {
    int index = (int)tier - 1;
    if (index >= 0 && index < AllTiers.Length)
      return AllTiers[index];

    return default;
  }

  public static TierInfo GetTierInfo(int tierId)
  {
    return TryParseTier(tierId, out var tier) ? GetTierInfo(tier) : default;
  }

  public static string GetTierLocalizeKey(int tierId)
  {
    if (!IsValidTierId(tierId))
      return string.Empty;

    var cfg = GetCfg(tierId);
    if (cfg == null || string.IsNullOrEmpty(cfg.Ranking))
      return string.Empty;

    // Excel 粘贴常带 U+200B 零宽空格，不去掉会拼出 I2 找不到的 key，界面就会显示 UI_RankingEvent_Emerald。
    string ranking = cfg.Ranking.Trim().Trim('\u200b', '\u200c', '\ufeff');
    if (string.IsNullOrEmpty(ranking))
      return string.Empty;

    return $"UI_RankingEvent_{ranking}";
  }

  public static string GetTierDisplayName(int tierId)
  {
    var key = GetTierLocalizeKey(tierId);
    if (string.IsNullOrEmpty(key))
      return string.Empty;

    return I2LocalizeMgr.GetString(key);
  }

  public static string GetLeagueOpenTitle(int tierId)
  {
    string tierName = GetTierDisplayName(tierId);
    if (string.IsNullOrEmpty(tierName))
      return string.Empty;

    string format = I2LocalizeMgr.GetString("UI_RankingEvent_OpenTitle");
    return string.IsNullOrEmpty(format) ? tierName : string.Format(format, tierName);
  }

  public static string GetPlayerLeagueOpenTitle()
  {
    int tierId = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(
      GPUserDataManager.GPUserData.goldenTreeLeagueTier);
    return GetLeagueOpenTitle(tierId);
  }

  public static string GetLeagueOpenText(int tierId)
  {
    string tierName = GetTierDisplayName(tierId);
    if (string.IsNullOrEmpty(tierName))
      return string.Empty;

    string format = I2LocalizeMgr.GetString("UI_RankingEvent_OpenText");
    return string.IsNullOrEmpty(format) ? tierName : string.Format(format, tierName);
  }

  public static string GetPlayerLeagueOpenText()
  {
    int tierId = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(
      GPUserDataManager.GPUserData.goldenTreeLeagueTier);
    return GetLeagueOpenText(tierId);
  }

  public static string GetLeagueRankEndTitle(int tierId)
  {
    string tierName = GetTierDisplayName(tierId);
    if (string.IsNullOrEmpty(tierName))
      return string.Empty;

    string format = I2LocalizeMgr.GetString("UI_RankingEvent_RankEndTitle");
    return string.IsNullOrEmpty(format) ? tierName : string.Format(format, tierName);
  }

  public static int ResolveLastSeasonLeagueTier()
  {
    return ResolveLeagueEndTiers().oldTier;
  }

  public static int ResolveSettlementLeagueTier()
  {
    return ResolveLeagueEndTiers().newTier;
  }

  public static (int oldTier, int newTier) ResolveLeagueEndTiers()
  {
    var userData = GPUserDataManager.GPUserData;
    if (IsValidTierId(userData.goldenTreeLastSeasonLeagueTier)
        && IsValidTierId(userData.goldenTreeSettlementLeagueTier))
    {
      return (
        GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(userData.goldenTreeLastSeasonLeagueTier),
        GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(userData.goldenTreeSettlementLeagueTier));
    }

    int fallbackTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(userData.goldenTreeLeagueTier);
    return (fallbackTier, fallbackTier);
  }

  /// <summary>
  /// 结算榜展示用：middle 为本次结算后段位（升/保/降结果）。
  /// 与 UIGoldTreeLeagueEnd 一样优先读 lastSeason/settlement 缓存，
  /// 不能用当前 goldenTreeLeagueTier 再算一次（升段会变成下下段，降段会停在旧段）。
  /// </summary>
  public static (int oldTier, int newTier) ResolveSettlementBoardDisplayTiers()
  {
    var userData = GPUserDataManager.GPUserData;
    if (IsValidTierId(userData.goldenTreeLastSeasonLeagueTier)
        && IsValidTierId(userData.goldenTreeSettlementLeagueTier))
    {
      return (
        GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(userData.goldenTreeLastSeasonLeagueTier),
        GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(userData.goldenTreeSettlementLeagueTier));
    }

    int boardId = userData.goldenTreeLastSeasonBoardId;
    int baseTier = GoldTreeLeagueRankFlagHelper.ResolveSettlementBaseTier(boardId);

    int playerRank = 0;
    if (GameData.Rank != null && GameData.Rank.TryGetPendingLastSeasonMyRank(out var settlementMyData))
      playerRank = (int)settlementMyData.rank;
    else
    {
      var myData = GameData.Rank?.GetMyRankData();
      playerRank = myData != null ? (int)myData.rank : 0;
    }

    if (playerRank < 1)
      return (baseTier, baseTier);

    int newTier = GoldTreeLeagueRankFlagHelper.ResolveNextTier(baseTier, playerRank);
    return (baseTier, newTier);
  }

  public static string GetPlayerLastSeasonLeagueRankEndTitle()
  {
    return GetLeagueRankEndTitle(ResolveLastSeasonLeagueTier());
  }

  public static string GetLeagueRankEndDetailsText(int oldTier, int newTier)
  {
    oldTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(oldTier);
    newTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(newTier);

    string oldTierName = GetTierDisplayName(oldTier);
    string newTierName = GetTierDisplayName(newTier);
    if (string.IsNullOrEmpty(oldTierName) || string.IsNullOrEmpty(newTierName))
      return string.Empty;

    string termKey = newTier > oldTier
      ? "UI_RankingEvent_RankEndPromote"
      : newTier < oldTier
        ? "UI_RankingEvent_RankEndDemote"
        : "UI_RankingEvent_RankEndMaintain";

    string format = I2LocalizeMgr.GetString(termKey);
    if (string.IsNullOrEmpty(format))
      return string.Empty;

    return format
      .Replace("{Rank}", newTierName)
      .Replace("{0}", oldTierName);
  }

  public static string GetPlayerLeagueRankEndDetailsText()
  {
    return GetLeagueRankEndDetailsText(
      ResolveLastSeasonLeagueTier(),
      ResolveSettlementLeagueTier());
  }

  public static GoldTreeRobotRankRewardCfg GetCfg(GoldTreeLeagueTier tier)
  {
    return GetCfg((int)tier);
  }

  public static GoldTreeRobotRankRewardCfg GetCfg(int tierId)
  {
    return ConfigHelper.Tables?.TbGoldTreeRobotRankRewardCfg?.GetOrDefault(tierId);
  }

  public static GoldTreeRobotRankRewardCfg GetCfgByLeagueTier(int leagueTier = -1)
  {
    if (leagueTier < 0)
      leagueTier = GPUserDataManager.GPUserData.goldenTreeLeagueTier;

    if (!IsValidTierId(leagueTier))
      leagueTier = MinTierId;

    return GetCfg(leagueTier);
  }

  public static RankRewardSlot GetRewardSlotByRank(int rank)
  {
    return rank switch
    {
      1 => RankRewardSlot.Rank1,
      2 => RankRewardSlot.Rank2,
      3 => RankRewardSlot.Rank3,
      >= 4 and <= 6 => RankRewardSlot.Rank4To6,
      >= 7 and <= 10 => RankRewardSlot.Rank7To10,
      _ => default,
    };
  }

  public static string GetRewardString(GoldTreeRobotRankRewardCfg cfg, int rank)
  {
    if (cfg == null)
      return null;

    return GetRewardSlotByRank(rank) switch
    {
      RankRewardSlot.Rank1 => cfg.Reweard1,
      RankRewardSlot.Rank2 => cfg.Reweard2,
      RankRewardSlot.Rank3 => cfg.Reweard3,
      RankRewardSlot.Rank4To6 => cfg.Reweard4,
      RankRewardSlot.Rank7To10 => cfg.Rewards,
      _ => null,
    };
  }

  public static string GetRewardString(GoldTreeLeagueTier tier, int rank)
  {
    return GetRewardString(GetCfg(tier), rank);
  }

  public static string GetRewardStringByLeagueTier(int rank, int leagueTier = -1)
  {
    return GetRewardString(GetCfgByLeagueTier(leagueTier), rank);
  }

  public static List<RewardItem> ParseRewardList(string rewardStr)
  {
    var result = new List<RewardItem>();
    if (string.IsNullOrWhiteSpace(rewardStr))
      return result;

    foreach (var part in rewardStr.Split('|'))
    {
      if (GameData.Activity.TryParseRewardString(part, out int itemId, out int count))
        result.Add(new RewardItem(itemId, count));
    }

    return result;
  }

  public static List<RewardItem> GetRewardList(GoldTreeLeagueTier tier, int rank)
  {
    return ParseRewardList(GetRewardString(tier, rank));
  }

  public static List<RewardItem> GetRewardListByLeagueTier(int rank, int leagueTier = -1)
  {
    return ParseRewardList(GetRewardStringByLeagueTier(rank, leagueTier));
  }
}
