using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using TMPro;
using TileGamePlay;

/// <summary>
/// 黄金树排行榜底部激励文案（RandomRankText + I2）。
/// </summary>
public static class GoldTreeRankTitleTextHelper
{
    public static bool TryBindRankTipTitle(Transform root, out TextMeshProUGUI titleLb)
    {
        titleLb = null;
        if (root == null)
            return false;

        Transform titleTran = root.Find("titleLb (1)");
        if (titleTran == null)
            titleTran = root.Find("titleLb");

        if (titleTran == null)
            return false;

        titleLb = titleTran.GetComponent<TextMeshProUGUI>();
        return titleLb != null;
    }

    public static void RefreshRankTipTitle(
        TextMeshProUGUI titleLb,
        List<RankData.OneUserData> rankData,
        RankData.OneUserData currentData,
        RankData.OneUserData lastData)
    {
        if (titleLb == null || currentData == null || lastData == null)
            return;

        titleLb.gameObject.SetActive(true);

        int currentRank = (int)currentData.rank;
        int lastRank = (int)lastData.rank;
        int improve = math.max(0, lastRank - currentRank);
        var queryContext = new RankChangeTitleTextData.QueryContext
        {
            CurrentRank = currentRank,
            LastRank = lastRank,
            RankImprove = improve,
            GapByTargetRank = BuildGapByTargetRank(rankData, currentData)
        };
        LocalizedTMPRefresher.SetText(
            titleLb,
            () => RankChangeTitleTextData.GetTitle(queryContext));
    }

    public static RankData.OneUserData BuildLastRankDataForTip(RankData.OneUserData currentData, int lastRankOverride = -1)
    {
        if (currentData == null)
            return null;

        int lastRank = lastRankOverride > 0
            ? lastRankOverride
            : GPUserDataManager.GPUserData.goldenTreeLastRank;
        if (lastRank <= 0)
            lastRank = (int)currentData.rank;

        var lastData = new RankData.OneUserData(currentData);
        lastData.rank = lastRank;
        return lastData;
    }

    static Dictionary<int, int> BuildGapByTargetRank(
        List<RankData.OneUserData> rankData,
        RankData.OneUserData myData)
    {
        var result = new Dictionary<int, int>();
        if (rankData == null || myData == null)
            return result;

        AddGapIfPossible(result, rankData, myData, 1);
        AddGapIfPossible(result, rankData, myData, 2);
        AddGapIfPossible(result, rankData, myData, 3);
        AddGapIfPossible(result, rankData, myData, 6);
        AddGapIfPossible(result, rankData, myData, 10);
        return result;
    }

    static void AddGapIfPossible(
        Dictionary<int, int> result,
        List<RankData.OneUserData> rankData,
        RankData.OneUserData myData,
        int targetRank)
    {
        if (targetRank <= 0 || result.ContainsKey(targetRank))
            return;

        var targetData = rankData.Find(v => v.rank == targetRank);
        if (targetData == null)
            return;

        long gap = math.abs(myData.score - targetData.score);
        result[targetRank] = (int)math.min(int.MaxValue, gap);
    }
}
