using System.Collections.Generic;
using cfg.Game;
using UnityEngine;

public static class RankChangeTitleTextData
{
    public struct QueryContext
    {
        public int CurrentRank;
        public int LastRank;
        public int RankImprove;
        public Dictionary<int, int> GapByTargetRank;
    }

    private const string DefaultTitle = "Keep going! Rewards are waiting for you.";

    public static string GetTitle(QueryContext context)
    {
        var tables = ConfigHelper.Tables;
        if (tables?.TbRandomRankTextCfg == null)
            return DefaultTitle;

        var rules = tables.TbRandomRankTextCfg.DataList;
        if (rules == null || rules.Count == 0)
            return DefaultTitle;

        var sortedRules = new List<RandomRankTextCfg>(rules);
        sortedRules.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        for (int i = 0; i < sortedRules.Count; i++)
        {
            RandomRankTextCfg rule = sortedRules[i];
            if (!IsRuleMatched(rule, context))
                continue;

            string termKey = PickOneText(rule.Texts);
            if (string.IsNullOrEmpty(termKey))
                continue;

            string text = I2LocalizeMgr.GetString(termKey);
            if (string.IsNullOrEmpty(text))
                continue;

            return ApplyTokens(text, rule, context);
        }

        return DefaultTitle;
    }

    private static bool IsRuleMatched(RandomRankTextCfg rule, QueryContext context)
    {
        int rankMin = rule.RankMin <= 0 ? 1 : rule.RankMin;
        int rankMax = rule.RankMax <= 0 ? int.MaxValue : rule.RankMax;
        if (context.CurrentRank < rankMin || context.CurrentRank > rankMax)
            return false;

        if (rule.NeedRankImprove == 1 && context.RankImprove <= 0)
            return false;

        if (rule.NeedRankImprove == 0 && context.RankImprove > 0)
            return false;

        int minImprove = rule.MinImprove < 0 ? int.MinValue : rule.MinImprove;
        int maxImprove = rule.MaxImprove < 0 ? int.MaxValue : rule.MaxImprove;
        if (context.RankImprove < minImprove || context.RankImprove > maxImprove)
            return false;

        if (rule.CompareTargetRank > 0)
        {
            int gap = GetGapToTarget(context, rule.CompareTargetRank);
            if (gap < 0)
                return false;

            int minGap = rule.MinGapToTarget < 0 ? int.MinValue : rule.MinGapToTarget;
            int maxGap = rule.MaxGapToTarget < 0 ? int.MaxValue : rule.MaxGapToTarget;
            if (gap < minGap || gap > maxGap)
                return false;
        }

        return true;
    }

    private static string ApplyTokens(string source, RandomRankTextCfg rule, QueryContext context)
    {
        string result = source;
        result = result.Replace("{0}", GetTokenValue(rule.Value0, rule, context));
        result = result.Replace("{1}", GetTokenValue(rule.Value1, rule, context));
        return result;
    }

    private static string GetTokenValue(string tokenType, RandomRankTextCfg rule, QueryContext context)
    {
        switch (tokenType)
        {
            case "lastRank":
                return context.LastRank.ToString();
            case "currentRank":
                return context.CurrentRank.ToString();
            case "rankImprove":
                return context.RankImprove.ToString();
            case "targetRank":
                return rule.CompareTargetRank.ToString();
            case "targetRankGap":
                return Mathf.Max(0, GetGapToTarget(context, rule.CompareTargetRank)).ToString();
            default:
                return string.Empty;
        }
    }

    private static int GetGapToTarget(QueryContext context, int targetRank)
    {
        if (targetRank <= 0 || context.GapByTargetRank == null)
            return -1;

        if (context.GapByTargetRank.TryGetValue(targetRank, out int gap))
            return gap;

        return -1;
    }

    private static string PickOneText(List<string> texts)
    {
        if (texts == null || texts.Count == 0)
            return string.Empty;

        int idx = Random.Range(0, texts.Count);
        return texts[idx];
    }
}
