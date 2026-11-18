using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 黄金树排行榜滚动列表槽位布局。默认第 4–50 名占 RankyCellNode (1–47)；
/// includeTop3InScroll 时第 1–50 名占 RankyCellNode (1–50)。
/// </summary>
public static class GoldTreeRankScrollLayout
{
    public const int MaxNormalSlots = 47;
    public const int MaxFullRankSlots = 50;
    const float RowSpacing = 141f;
    const float BottomPadding = 44f;
    const float FlagItemGap = 5f;
    const float DefaultFlagHeight = 58f;

    public static void ApplyNormalCellPosition(RectTransform cellRt, int slotIndex, RectTransform firstRowTemplate)
    {
        if (cellRt == null || firstRowTemplate == null)
            return;

        cellRt.anchorMin = firstRowTemplate.anchorMin;
        cellRt.anchorMax = firstRowTemplate.anchorMax;
        cellRt.pivot = firstRowTemplate.pivot;
        cellRt.sizeDelta = firstRowTemplate.sizeDelta;

        var pos = firstRowTemplate.anchoredPosition;
        pos.y -= slotIndex * RowSpacing;
        cellRt.anchoredPosition = pos;
    }

    public static void ApplyFullRankSlotPositions(
        Func<string, Transform> findRankCellNode,
        int slotCount,
        RectTransform content = null,
        GoldTreeLeagueRankFlagHelper.DividerRanks? dividers = null)
    {
        if (findRankCellNode == null || slotCount <= 0)
            return;

        var firstRow = findRankCellNode("RankyCellNode (1)") as RectTransform;
        if (firstRow == null)
            return;

        float promoteFlagBlock = 0f;
        float maintainFlagBlock = 0f;
        if (dividers.HasValue && content != null)
        {
            promoteFlagBlock = GetFlagBlockHeight(content, "flag");
            maintainFlagBlock = GetFlagBlockHeight(content, "flag (1)");
        }

        for (int i = 1; i <= slotCount; i++)
        {
            var node = findRankCellNode($"RankyCellNode ({i})");
            if (node is not RectTransform cellRt)
                continue;

            float yOffset = (i - 1) * RowSpacing;
            if (dividers.HasValue)
            {
                var divider = dividers.Value;
                if (slotCount > divider.PromoteMaintain && i > divider.PromoteMaintain)
                    yOffset += promoteFlagBlock;
                if (slotCount > divider.MaintainDemote && i > divider.MaintainDemote)
                    yOffset += maintainFlagBlock;
            }

            cellRt.anchorMin = firstRow.anchorMin;
            cellRt.anchorMax = firstRow.anchorMax;
            cellRt.pivot = firstRow.pivot;
            cellRt.sizeDelta = firstRow.sizeDelta;

            var pos = firstRow.anchoredPosition;
            pos.y -= yOffset;
            cellRt.anchoredPosition = pos;
        }
    }

    static float GetFlagBlockHeight(RectTransform content, string flagName)
    {
        var flagRt = FindContentChild(content, flagName);
        float flagHeight = flagRt != null ? flagRt.sizeDelta.y : DefaultFlagHeight;
        return flagHeight + FlagItemGap * 2f;
    }

    public static void FitScrollContentHeight(
        RectTransform content,
        Func<string, Transform> findRankCellNode,
        int normalCount,
        RectTransform oriNormalCellRt,
        ScrollRect scrollView,
        bool includeTop3InScroll = false,
        bool toggleSlotVisibility = true)
    {
        if (findRankCellNode == null || oriNormalCellRt == null)
            return;

        if (scrollView != null && scrollView.content != null)
            content = scrollView.content;

        if (content == null)
            return;

        var top3InScroll = content.Find("top3Node");
        if (top3InScroll != null)
            top3InScroll.gameObject.SetActive(true);

        if (includeTop3InScroll && normalCount > 0)
        {
            var dividers = GoldTreeLeagueRankFlagHelper.GetDividerRanks();
            ApplyFullRankSlotPositions(findRankCellNode, normalCount, content, dividers);
        }

        if (includeTop3InScroll)
        {
            ApplyRankDividerFlags(content, findRankCellNode, normalCount);
            GoldTreeLeagueRankFlagHelper.ApplyFlagRangeTexts(content);
        }

        int maxSlots = includeTop3InScroll ? MaxFullRankSlots : MaxNormalSlots;
        if (toggleSlotVisibility)
        {
            for (int i = 1; i <= maxSlots; i++)
            {
                var node = findRankCellNode($"RankyCellNode ({i})");
                if (node != null)
                    node.gameObject.SetActive(i <= normalCount);
            }
        }
        else
        {
            for (int i = 1; i <= normalCount; i++)
            {
                var node = findRankCellNode($"RankyCellNode ({i})");
                if (node != null)
                    node.gameObject.SetActive(true);
            }
        }

        float contentHeight = 0f;
        if (normalCount > 0)
        {
            var lastNode = findRankCellNode($"RankyCellNode ({normalCount})");
            if (lastNode is RectTransform lastRt)
            {
                contentHeight = -lastRt.anchoredPosition.y + lastRt.rect.height * 0.5f + BottomPadding;
            }
            else
            {
                float cellH = oriNormalCellRt.sizeDelta.y;
                contentHeight = normalCount * cellH + BottomPadding;
            }
        }

        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);

        if (scrollView != null)
        {
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            Canvas.ForceUpdateCanvases();
            scrollView.verticalNormalizedPosition = Mathf.Clamp01(scrollView.verticalNormalizedPosition);
        }
    }

    public static RectTransform EnsureNormalCell(
        int rank,
        Transform scrollParent,
        GameObject oriNormalCellObj,
        Func<string, Transform> findRankCellNode,
        bool includeTop3InScroll = false,
        int totalRankCount = 0)
    {
        int nodeIndex = includeTop3InScroll ? rank : rank - 3;
        var cellName = $"RankyCellNode ({nodeIndex})";
        var objTran = findRankCellNode(cellName);
        if (objTran == null)
        {
            var clone = UnityEngine.Object.Instantiate(oriNormalCellObj, scrollParent);
            clone.name = cellName;
            objTran = clone.transform;
        }

        int slotIndex = includeTop3InScroll ? rank - 1 : rank - 4;
        var firstRow = findRankCellNode("RankyCellNode (1)") as RectTransform;
        if (objTran is RectTransform cellRt && firstRow != null)
        {
            if (includeTop3InScroll && totalRankCount > 0)
            {
                var dividers = GoldTreeLeagueRankFlagHelper.GetDividerRanks();
                ApplyFullRankSlotPositions(
                    findRankCellNode,
                    totalRankCount,
                    scrollParent as RectTransform,
                    dividers);
            }
            else if (!includeTop3InScroll)
            {
                ApplyNormalCellPosition(cellRt, slotIndex, firstRow);
            }
        }

        return objTran as RectTransform;
    }

    /// <summary>
    /// 在 content 下放置名次分隔 flag，位置由 game_tbcsvgoldtreeleaguerankcfg 决定。
    /// </summary>
    public static void ApplyRankDividerFlags(
        RectTransform content,
        Func<string, Transform> findRankCellNode,
        int rankCount,
        int leagueCfgId = -1)
    {
        if (content == null || findRankCellNode == null)
            return;

        var dividers = GoldTreeLeagueRankFlagHelper.GetDividerRanks(leagueCfgId);
        ApplyDividerFlag(content, findRankCellNode, "flag", dividers.PromoteMaintain, rankCount);
        ApplyDividerFlag(content, findRankCellNode, "flag (1)", dividers.MaintainDemote, rankCount);
    }

    static void ApplyDividerFlag(
        RectTransform content,
        Func<string, Transform> findRankCellNode,
        string flagName,
        int afterRank,
        int rankCount)
    {
        var flagRt = FindContentChild(content, flagName);
        if (flagRt == null)
            return;

        if (flagRt.parent != content)
            flagRt.SetParent(content, false);

        bool visible = rankCount > afterRank;
        flagRt.gameObject.SetActive(visible);
        if (!visible)
            return;

        var upper = findRankCellNode($"RankyCellNode ({afterRank})") as RectTransform;
        var lower = findRankCellNode($"RankyCellNode ({afterRank + 1})") as RectTransform;
        if (upper == null || lower == null)
            return;

        if (upper.parent != content)
            return;

        flagRt.anchorMin = upper.anchorMin;
        flagRt.anchorMax = upper.anchorMax;
        flagRt.pivot = new Vector2(0.5f, 0.5f);

        float upperBottom = upper.anchoredPosition.y - upper.rect.height * upper.pivot.y;
        float lowerTop = lower.anchoredPosition.y + lower.rect.height * (1f - lower.pivot.y);
        float flagHalfH = flagRt.rect.height * 0.5f;

        var pos = flagRt.anchoredPosition;
        pos.x = upper.anchoredPosition.x;
        pos.y = (upperBottom - FlagItemGap - flagHalfH + lowerTop + FlagItemGap + flagHalfH) * 0.5f;
        flagRt.anchoredPosition = pos;

        // 插在下方名次格子之前，保证与列表同层并随 content 滚动。
        int insertIndex = lower.GetSiblingIndex();
        if (flagRt.GetSiblingIndex() != insertIndex)
            flagRt.SetSiblingIndex(insertIndex);
    }

    static RectTransform FindContentChild(RectTransform content, string childName)
    {
        if (content == null)
            return null;

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

    public static float GetRectCenterOffsetFromTop(RectTransform child, RectTransform topAnchoredParent)
    {
        if (child == null || topAnchoredParent == null)
            return 0f;

        Vector3 centerInParent = topAnchoredParent.InverseTransformPoint(
            child.TransformPoint(child.rect.center));
        return -centerInParent.y;
    }

    public static float GetViewportCenterOffsetFromTop(RectTransform viewport)
    {
        if (viewport == null)
            return 0f;

        return viewport.rect.height * 0.5f;
    }

    public static Vector2 GetContentPosToAlignChildCenter(
        RectTransform content,
        RectTransform child,
        RectTransform viewport)
    {
        if (content == null || child == null || viewport == null)
            return Vector2.zero;

        float viewportHeight = viewport.rect.height;
        float cellCenterFromTop = GetRectCenterOffsetFromTop(child, content);
        float viewportCenterFromTop = GetViewportCenterOffsetFromTop(viewport);
        float contentY = cellCenterFromTop - viewportCenterFromTop;
        float maxY = Mathf.Max(0f, content.rect.height - viewportHeight);
        contentY = Mathf.Clamp(contentY, 0f, maxY);
        return new Vector2(content.anchoredPosition.x, contentY);
    }
}
