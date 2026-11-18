using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using cfg.Game;
using FishFramework;
using UnityEngine;

/// <summary>
/// 黄金树联赛机器人涨分：段位池分配、常规/末段公式、配置查找。
/// </summary>
public static class GoldTreeRobotGrowthHelper
{
    public const int RobotCount = 49;
    const int EndGameThresholdSeconds = 3 * 3600;

    public static long GetActivityDayStartUnix()
    {
        long nowServerSeconds = GameData.GetServerTime();
        var nowUtc = DateTimeOffset.FromUnixTimeSeconds(nowServerSeconds).UtcDateTime;

        var todayUtcOpen = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 4, 0, 0, DateTimeKind.Utc);
        DateTime startUtc = nowUtc < todayUtcOpen ? todayUtcOpen.AddDays(-1) : todayUtcOpen;
        return new DateTimeOffset(startUtc).ToUnixTimeSeconds();
    }

    public static int GetTimePassedMinutes(long lastSettleUnix, long nowUnix)
    {
        if (lastSettleUnix <= 0 || nowUnix <= lastSettleUnix)
        {
            return 0;
        }

        return (int)((nowUnix - lastSettleUnix) / 60);
    }

    public static bool IsEndGamePhase()
    {
        if (GameData.GoldTreeActData == null)
        {
            return false;
        }

        long countdown = GameData.GoldTreeActData.GetEndTime() - GameData.GetServerTime();
        return countdown > 0 && countdown <= EndGameThresholdSeconds;
    }

    public static List<string> BuildRobotTierAssignments(int leagueTier, int boardId)
    {
        leagueTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(leagueTier);
        var poolCfg = ConfigHelper.Tables?.TbCSVGoldTreeLeagueRobotpoolCfg?.GetOrDefault(leagueTier);
        if (poolCfg == null || string.IsNullOrEmpty(poolCfg.Robot))
        {
            return BuildFallbackTierAssignments(boardId);
        }

        List<string> tiers = ParseRobotPool(poolCfg.Robot);
        if (tiers.Count != RobotCount)
        {
            return BuildFallbackTierAssignments(boardId);
        }

        ShuffleTiers(tiers, boardId);
        return tiers;
    }

    public static int CalcRobotGain(int playerGain, int robotRank, string robotTier, int timePassedMinutes)
    {
        if (timePassedMinutes < 0)
        {
            timePassedMinutes = 0;
        }

        if (IsEndGamePhase())
        {
            return CalcEndGameGain(playerGain, robotRank, timePassedMinutes);
        }

        return CalcFullPhaseGain(playerGain, robotTier, timePassedMinutes);
    }

    public static int CalcInitialRobotScore(int baseScore, string startJitterRaw, int seed)
    {
        if (baseScore <= 0)
        {
            return 0;
        }

        float jitter = ParseConfigFloat(startJitterRaw);
        if (jitter <= 0f)
        {
            return baseScore;
        }

        var rng = new System.Random(seed);
        float factor = (float)(rng.NextDouble() * (2f * jitter) + (1f - jitter));
        return Mathf.Max(0, Mathf.RoundToInt(baseScore * factor));
    }

    static List<string> ParseRobotPool(string robotPool)
    {
        List<string> tiers = new(RobotCount);
        string[] segments = robotPool.Split('|');
        foreach (string rawSegment in segments)
        {
            string segment = Sanitize(rawSegment);
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            int splitIndex = segment.LastIndexOf('_');
            if (splitIndex <= 0 || splitIndex >= segment.Length - 1)
            {
                continue;
            }

            string tier = segment.Substring(0, splitIndex).Trim();
            if (!int.TryParse(segment.Substring(splitIndex + 1), out int count) || count <= 0)
            {
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                tiers.Add(tier);
            }
        }

        return tiers;
    }

    static List<string> BuildFallbackTierAssignments(int boardId)
    {
        List<string> tiers = new(RobotCount);
        for (int i = 0; i < RobotCount; i++)
        {
            tiers.Add("R1");
        }

        ShuffleTiers(tiers, boardId);
        return tiers;
    }

    static void ShuffleTiers(List<string> tiers, int seed)
    {
        if (tiers == null || tiers.Count <= 1)
        {
            return;
        }

        var order = Utility.GetRandomSequence(tiers.Count, seed);
        List<string> shuffled = new(tiers.Count);
        for (int i = 0; i < order.Count; i++)
        {
            shuffled.Add(tiers[order[i] - 1]);
        }

        tiers.Clear();
        tiers.AddRange(shuffled);
    }

    static int CalcFullPhaseGain(int playerGain, string robotTier, int timePassedMinutes)
    {
        var cfg = GetFullConfigByTier(robotTier);
        if (cfg == null)
        {
            return 0;
        }

        float followRate = ParseConfigFloat(cfg.FollowRate);
        float idleRate = ParseConfigFloat(cfg.IdleRate);
        float jitter = cfg.Jitter;

        float gain = timePassedMinutes * idleRate;
        if (playerGain > 0)
        {
            float followPart = playerGain * followRate;
            float jitterPart = playerGain * jitter;
            gain += followPart + (UnityEngine.Random.value < 0.5f ? -jitterPart : jitterPart);
        }

        return Mathf.Max(0, Mathf.RoundToInt(gain));
    }

    static int CalcEndGameGain(int playerGain, int robotRank, int timePassedMinutes)
    {
        var cfg = GetEndGameConfigByRank(robotRank);
        if (cfg == null)
        {
            return 0;
        }

        float idleRate = ParseConfigFloat(cfg.IdleRate);
        float gain = timePassedMinutes * idleRate;

        if (playerGain > 0)
        {
            gain += PickEndGameFollowGain(cfg.FollowRate);
        }

        return Mathf.Max(0, Mathf.RoundToInt(gain));
    }

    static CSVGoldTreeLeagueRobotFullConfiCfg GetFullConfigByTier(string robotTier)
    {
        string tier = Sanitize(robotTier);
        var list = ConfigHelper.Tables?.TbCSVGoldTreeLeagueRobotFullConfiCfg?.DataList;
        if (list == null)
        {
            return null;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(Sanitize(list[i].Robot), tier, StringComparison.OrdinalIgnoreCase))
            {
                return list[i];
            }
        }

        return null;
    }

    static CSVGoldTreeLeagueRobotEndGameRoCfg GetEndGameConfigByRank(int robotRank)
    {
        var list = ConfigHelper.Tables?.TbCSVGoldTreeLeagueRobotEndGameRoCfg?.DataList;
        if (list == null || robotRank <= 0)
        {
            return null;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (TryParseRankRange(list[i].Ranking, robotRank, out _))
            {
                return list[i];
            }
        }

        return null;
    }

    static bool TryParseRankRange(string ranking, int rank, out int minRank)
    {
        minRank = 0;
        string text = Sanitize(ranking);
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        string[] parts = text.Split('_');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out int startRank) || !int.TryParse(parts[1], out int endRank))
        {
            return false;
        }

        minRank = startRank;
        return rank >= startRank && rank <= endRank;
    }

    static float PickEndGameFollowGain(string followRateRaw)
    {
        string text = Sanitize(followRateRaw);
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        string[] parts = text.Split(',');
        List<float> values = new();
        for (int i = 0; i < parts.Length; i++)
        {
            float value = ParseConfigFloat(parts[i]);
            values.Add(value);
        }

        if (values.Count == 0)
        {
            return 0f;
        }

        int index = UnityEngine.Random.Range(0, values.Count);
        return values[index];
    }

    static float ParseConfigFloat(string raw)
    {
        string text = Sanitize(raw);
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            return value;
        }

        return 0f;
    }

    static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        StringBuilder builder = new(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char ch = raw[i];
            if (!char.IsWhiteSpace(ch) && !char.IsControl(ch) && ch != '\u200b' && ch != '\ufeff')
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Trim();
    }
}
