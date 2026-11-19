using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 策划案 5.2–5.4：对每种 ≥3 的颜色算 ClickCost = Need(3) + Extra，
    /// Extra 只为 shortage 颗埋棋揭盖（同色可点盖不算 Extra）。
    /// 取该色最便宜的 3 枚；再按本轮区间选色。E1 换色，凑不齐时散取。
    /// 5.4 的区间跟连胜连败档位走，分档阈值与 LevelGenService.GetWinFailLevelCfg 相同。
    /// 区间只影响优先选哪一组。场上成本对不上、或选中的一组点不掉时，改选任意可点的 3 枚。
    /// 凑不满 3 枚就返回空，不能把 1、2 枚转成脑花，否则脑花互消永远凑不齐。
    /// </summary>
    public static class IQChallengeSelector
    {
        public const int NeedCount = 3;

        public static int LastClickCost { get; private set; } = PlayBotClickCost.UncoverImpossible;
        public static int LastExtra { get; private set; } = PlayBotClickCost.UncoverImpossible;
        public static int LastVisible { get; private set; }
        public static bool LastWasInBand { get; private set; }
        public static bool LastUsedMixedColorFallback { get; private set; }
        public static string LastSelectionSummary { get; private set; } = string.Empty;
        public static string LastColorDump { get; private set; } = string.Empty;

        public struct ColorPick
        {
            public int XSortIndex;
            public int ClickCost;
            public int Extra;
            public int Visible;
            public List<XSortActor> XSorts;
        }

        enum CostBandTier
        {
            Fail2OrBelow,
            Win1OrFail1,
            Win2OrAbove,
        }

        public static void GetCostBand(int clearedCount, out int minCost, out int maxCost)
        {
            if (clearedCount <= 0)
            {
                minCost = 0;
                maxCost = 4;
                return;
            }

            switch (ResolveCostBandTier(out _, out _))
            {
                case CostBandTier.Fail2OrBelow:
                    if (clearedCount == 1)
                    {
                        minCost = 5;
                        maxCost = 6;
                        return;
                    }

                    minCost = 7;
                    maxCost = 8;
                    return;
                case CostBandTier.Win2OrAbove:
                    if (clearedCount == 1)
                    {
                        minCost = 5;
                        maxCost = 7;
                        return;
                    }

                    minCost = 12;
                    maxCost = 12;
                    return;
                default:
                    if (clearedCount == 1)
                    {
                        minCost = 5;
                        maxCost = 7;
                        return;
                    }

                    minCost = 8;
                    maxCost = 10;
                    return;
            }
        }

        /// <summary>
        /// fail2 及更深连败用低成本；win2 及以上用高成本。
        /// 无连胜连败、连胜 1、跨日首关、win1、fail1 用中间区间。
        /// </summary>
        static CostBandTier ResolveCostBandTier(out int winCnt, out int failCnt)
        {
            winCnt = 0;
            failCnt = 0;
#if UNITY_EDITOR
            if (GameDirector.AutoRunLevelInEditor)
                return CostBandTier.Win1OrFail1;
#endif
            if (!GameData.BFromStartScene || !GPUserDataManager.HasInit())
                return CostBandTier.Win1OrFail1;

            var userData = GPUserDataManager.Instance.UserData;
            if (userData == null)
                return CostBandTier.Win1OrFail1;

            winCnt = userData.continusWinCnt;
            failCnt = userData.continusFailCnt;
            if (NextDayFirstEnterEase.IsActiveThisLoad && winCnt > 1 && failCnt == 0)
                return CostBandTier.Win1OrFail1;
            if (winCnt >= 3)
                return CostBandTier.Win2OrAbove;
            if (winCnt > 0)
                return CostBandTier.Win1OrFail1;
            if (failCnt >= 3)
                return CostBandTier.Fail2OrBelow;
            return CostBandTier.Win1OrFail1;
        }

        static string DescribeCostBandTier()
        {
            CostBandTier tier = ResolveCostBandTier(out int winCnt, out int failCnt);
            string name;
            if (tier == CostBandTier.Fail2OrBelow)
                name = "fail2-";
            else if (tier == CostBandTier.Win2OrAbove)
                name = "win2+";
            else
                name = "win1/fail1";
            return name + " win=" + winCnt + " fail=" + failCnt;
        }

        public static List<XSortActor> PickTargets(int clearedCount)
        {
            GetCostBand(clearedCount, out int minCost, out int maxCost);
            return PickTargets(minCost, maxCost, NeedCount);
        }

        public static List<XSortActor> PickTargets(int minCost, int maxCost, int need)
        {
            ResetLastSelection();
            Physics.SyncTransforms();

            List<XSortActor> currentBoard = CollectCurrentBoardSnapshot();
            List<XSortActor> board = CollectConvertibleBoardXSorts(currentBoard);
            if (board.Count == 0)
            {
                LogNoSelection("board empty");
                return new List<XSortActor>();
            }

            List<ColorPick> colors = BuildColorPicks(board, currentBoard, need, minCost, maxCost);
            LastColorDump = FormatColorDump(colors, minCost, maxCost);

            List<XSortActor> chosen = null;
            ColorPick chosenPick = default;
            bool mixed = false;
            if (colors.Count > 0)
            {
                chosenPick = ChooseColor(colors, minCost, maxCost);
                chosen = chosenPick.XSorts;
            }

            if (!IsPlayableSet(chosen, currentBoard, need))
            {
                chosen = BuildPlayableFallback(board, currentBoard, need);
                mixed = true;
                if (IsPlayableSet(chosen, currentBoard, need)
                    && PlayBotClickCost.TryGetCurrentClickCost(
                        chosen, currentBoard, out int clickCost, out int extra))
                {
                    chosenPick = new ColorPick
                    {
                        XSortIndex = -1,
                        Visible = CountVisible(chosen),
                        Extra = extra,
                        ClickCost = clickCost,
                        XSorts = chosen
                    };
                }
            }

            if (!IsPlayableSet(chosen, currentBoard, need))
            {
                LogNoSelection($"no playable trio, convertible={board.Count} band=[{minCost},{maxCost}]");
                return new List<XSortActor>();
            }

            RememberSelection(chosenPick, minCost, maxCost, mixed);
            return chosen;
        }

        static bool IsPlayableSet(List<XSortActor> XSorts, List<XSortActor> currentBoard, int need)
        {
            if (XSorts == null || XSorts.Count != need || currentBoard == null)
                return false;

            var seen = new HashSet<XSortActor>();
            for (int i = 0; i < XSorts.Count; i++)
            {
                XSortActor XSort = XSorts[i];
                if (!CanConvert(XSort) || !seen.Add(XSort) || !currentBoard.Contains(XSort))
                    return false;
            }

            if (!PlayBotClickCost.TryGetCurrentClickCost(XSorts, currentBoard, out int clickCost, out _))
                return false;
            if (clickCost >= PlayBotClickCost.UncoverImpossible)
                return false;

            return HasClickableEntry(XSorts, currentBoard);
        }

        static bool HasClickableEntry(List<XSortActor> XSorts, List<XSortActor> currentBoard)
        {
            var blockers = new HashSet<XSortActor>();
            for (int i = 0; i < XSorts.Count; i++)
            {
                XSortActor XSort = XSorts[i];
                if (PlayBotClickCost.CanPlayerClick(XSort))
                    return true;
                PlayBotClickCost.CollectMustRemove(XSort, -1, currentBoard, blockers);
            }

            foreach (XSortActor blocker in blockers)
            {
                if (PlayBotClickCost.CanPlayerClick(blocker))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 区间里没有可完成的三连时，优先拿当前就能点的 3 枚。
        /// 脑花互消，这 3 枚转完就能直接凑成一组，不依赖原花色和目标成本。
        /// </summary>
        static List<XSortActor> BuildPlayableFallback(
            List<XSortActor> board,
            List<XSortActor> currentBoard,
            int need)
        {
            var clickable = new List<XSortActor>();
            var buried = new List<XSortActor>();
            for (int i = 0; i < board.Count; i++)
            {
                XSortActor XSort = board[i];
                if (PlayBotClickCost.CanPlayerClick(XSort))
                    clickable.Add(XSort);
                else
                    buried.Add(XSort);
            }

            if (clickable.Count >= need)
                return Take(clickable, need);

            buried.Sort(CompareCheapest);
            var selected = new List<XSortActor>(clickable);
            for (int i = 0; i < buried.Count && selected.Count < need; i++)
            {
                selected.Add(buried[i]);
                if (!IsPlayableSet(selected, currentBoard, selected.Count))
                    selected.RemoveAt(selected.Count - 1);
            }

            if (selected.Count != need)
                return new List<XSortActor>();
            return selected;
        }

        public static bool CanConvert(XSortActor XSort)
        {
            if (XSort == null || XSort.IsSubmitted || XSort.IsFission)
                return false;
            if (XSort.XSortModel == null || XSort.IsCoinReward)
                return false;
            if (XSort.Effect != null)
                return false;
            if (BoxCoverManager.IsXSortBlocked(XSort))
                return false;
            return true;
        }

        static void ResetLastSelection()
        {
            LastClickCost = PlayBotClickCost.UncoverImpossible;
            LastExtra = PlayBotClickCost.UncoverImpossible;
            LastVisible = 0;
            LastWasInBand = false;
            LastUsedMixedColorFallback = false;
            LastSelectionSummary = string.Empty;
            LastColorDump = string.Empty;
        }

        static void RememberSelection(ColorPick pick, int minCost, int maxCost, bool mixed)
        {
            LastClickCost = pick.ClickCost;
            LastExtra = pick.Extra;
            LastVisible = pick.Visible;
            LastWasInBand = pick.ClickCost >= minCost && pick.ClickCost <= maxCost;
            LastUsedMixedColorFallback = mixed;

            LastSelectionSummary =
                $"color={pick.XSortIndex} cost={pick.ClickCost} need={NeedCount} " +
                $"vis={pick.Visible} extra={pick.Extra} band=[{minCost},{maxCost}] " +
                $"tier={DescribeCostBandTier()} " +
                $"inBand={LastWasInBand} mode={(mixed ? "playable-fallback" : "same-color")}";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[IQCost] {LastSelectionSummary}");
#endif
        }

        static void LogNoSelection(string reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[IQCost] skip: {reason}");
#endif
        }

        static List<XSortActor> CollectCurrentBoardSnapshot()
        {
            var result = new List<XSortActor>();
            List<XSortActor> actXSorts = LevelDirector.LevelGenService?.ActXSorts;
            if (actXSorts == null)
                return result;

            for (int i = 0; i < actXSorts.Count; i++)
            {
                XSortActor XSort = actXSorts[i];
                if (XSort == null || XSort.IsSubmitted || !XSort.gameObject.activeInHierarchy)
                    continue;
                result.Add(XSort);
            }

            return result;
        }

        static List<XSortActor> CollectConvertibleBoardXSorts(List<XSortActor> currentBoard)
        {
            var result = new List<XSortActor>();
            if (currentBoard == null)
                return result;

            for (int i = 0; i < currentBoard.Count; i++)
            {
                if (CanConvert(currentBoard[i]))
                    result.Add(currentBoard[i]);
            }

            return result;
        }

        static List<ColorPick> BuildColorPicks(
            List<XSortActor> board,
            List<XSortActor> currentBoard,
            int need,
            int minCost,
            int maxCost)
        {
            var byColor = new Dictionary<int, List<XSortActor>>();
            for (int i = 0; i < board.Count; i++)
            {
                int id = board[i].MatchTypeId;
                if (!byColor.TryGetValue(id, out List<XSortActor> list))
                {
                    list = new List<XSortActor>();
                    byColor[id] = list;
                }

                list.Add(board[i]);
            }

            var picks = new List<ColorPick>();
            foreach (var kv in byColor)
            {
                if (kv.Value.Count < need)
                    continue;

                if (!TryBuildColorPick(
                    kv.Key, kv.Value, currentBoard, need, minCost, maxCost, out ColorPick pick))
                    continue;

                picks.Add(pick);
            }

            return picks;
        }

        static bool TryBuildColorPick(
            int colorId,
            List<XSortActor> ofColor,
            List<XSortActor> currentBoard,
            int need,
            int minCost,
            int maxCost,
            out ColorPick pick)
        {
            pick = default;
            SplitByClickable(ofColor, out List<XSortActor> clickable, out List<XSortActor> buried);

            if (!TryBuildCheapestPick(
                colorId, ofColor, clickable, buried, currentBoard, need, out ColorPick cheapest))
                return false;

            // 5.3：该色代表成本是最便宜的一次三消。已在区间内就用它。
            if (DistanceToBand(cheapest.ClickCost, minCost, maxCost) == 0)
            {
                pick = cheapest;
                return true;
            }

            // 5.4：最便宜偏浅时，在同色里找一组 Need+Extra 落在本轮区间的三连。
            if (cheapest.ClickCost < minCost
                && TrySeekBandPick(
                    colorId, clickable, buried, currentBoard, need, minCost, maxCost, out ColorPick inBand))
            {
                pick = inBand;
                return true;
            }

            if (TrySeekNearestPick(
                colorId, clickable, buried, currentBoard, need, minCost, maxCost, cheapest, out ColorPick nearer)
                && DistanceToBand(nearer.ClickCost, minCost, maxCost)
                    < DistanceToBand(cheapest.ClickCost, minCost, maxCost))
            {
                pick = nearer;
                return true;
            }

            pick = cheapest;
            return true;
        }

        static bool TryBuildCheapestPick(
            int colorId,
            List<XSortActor> ofColor,
            List<XSortActor> clickable,
            List<XSortActor> buried,
            List<XSortActor> currentBoard,
            int need,
            out ColorPick pick)
        {
            pick = default;
            int extra = 0;
            List<XSortActor> buriedChosen = null;
            int shortage = Mathf.Max(0, need - clickable.Count);
            if (shortage > 0)
            {
                if (currentBoard == null
                    || !PlayBotClickCost.TryGetUncover(
                        colorId, shortage, currentBoard,
                        out extra, out _, out _, out buriedChosen, ofColor))
                    return false;
            }

            List<XSortActor> selected = SelectCheapestThree(ofColor, clickable, buriedChosen, need);
            if (selected.Count < need)
                return false;

            pick = MakePick(colorId, selected, extra);
            return true;
        }

        static bool TrySeekBandPick(
            int colorId,
            List<XSortActor> clickable,
            List<XSortActor> buried,
            List<XSortActor> currentBoard,
            int need,
            int minCost,
            int maxCost,
            out ColorPick pick)
        {
            return TrySeekPick(
                colorId, clickable, buried, currentBoard, need, minCost, maxCost,
                requireInBand: true, fallback: default, out pick);
        }

        static bool TrySeekNearestPick(
            int colorId,
            List<XSortActor> clickable,
            List<XSortActor> buried,
            List<XSortActor> currentBoard,
            int need,
            int minCost,
            int maxCost,
            ColorPick fallback,
            out ColorPick pick)
        {
            return TrySeekPick(
                colorId, clickable, buried, currentBoard, need, minCost, maxCost,
                requireInBand: false, fallback, out pick);
        }

        static bool TrySeekPick(
            int colorId,
            List<XSortActor> clickable,
            List<XSortActor> buried,
            List<XSortActor> currentBoard,
            int need,
            int minCost,
            int maxCost,
            bool requireInBand,
            ColorPick fallback,
            out ColorPick pick)
        {
            pick = fallback;
            bool found = !requireInBand && fallback.XSorts != null && fallback.XSorts.Count >= need;
            int bestDist = found ? DistanceToBand(fallback.ClickCost, minCost, maxCost) : int.MaxValue;
            int bestCost = found ? fallback.ClickCost : int.MaxValue;

            List<XSortActor> buriedPool = CapBuriedCandidates(buried, 10);
            int extraMin = Mathf.Max(0, minCost - need);
            int extraMax = Mathf.Max(0, maxCost - need);

            // 从“更埋”扫到“更浅”，覆盖 5.3 的 0/1/2 可见典型档。
            for (int buriedNeed = Mathf.Min(need, buriedPool.Count); buriedNeed >= 0; buriedNeed--)
            {
                int visibleNeed = need - buriedNeed;
                if (clickable.Count < visibleNeed)
                    continue;

                if (buriedNeed == 0)
                {
                    if (extraMin > 0)
                        continue;
                    Consider(
                        MakePick(colorId, Take(clickable, visibleNeed), 0),
                        minCost, maxCost, requireInBand, extraMin, extraMax,
                        ref found, ref bestDist, ref bestCost, ref pick);
                    continue;
                }

                var seek = new SeekState
                {
                    ColorId = colorId,
                    Clickable = clickable,
                    CurrentBoard = currentBoard,
                    VisibleNeed = visibleNeed,
                    BuriedNeed = buriedNeed,
                    Need = need,
                    MinCost = minCost,
                    MaxCost = maxCost,
                    RequireInBand = requireInBand,
                    ExtraMin = extraMin,
                    ExtraMax = extraMax,
                    Found = found,
                    BestDist = bestDist,
                    BestCost = bestCost,
                    Pick = pick,
                    StepsLeft = SeekStepBudget
                };
                WalkBuriedCombos(buriedPool, buriedNeed, seek);
                found = seek.Found;
                bestDist = seek.BestDist;
                bestCost = seek.BestCost;
                pick = seek.Pick;
                if (requireInBand && found)
                    break;
                if (seek.StepsLeft <= 0)
                    break;
            }

            return found && pick.XSorts != null && pick.XSorts.Count >= need;
        }

        sealed class SeekState
        {
            public int ColorId;
            public List<XSortActor> Clickable;
            public List<XSortActor> CurrentBoard;
            public int VisibleNeed;
            public int BuriedNeed;
            public int Need;
            public int MinCost;
            public int MaxCost;
            public bool RequireInBand;
            public int ExtraMin;
            public int ExtraMax;
            public bool Found;
            public int BestDist;
            public int BestCost;
            public ColorPick Pick;
            public int StepsLeft;
        }

        const int SeekStepBudget = 256;

        static void WalkBuriedCombos(List<XSortActor> pool, int k, SeekState state)
        {
            if (pool == null || state == null || k <= 0 || pool.Count < k)
                return;
            WalkBuriedCombosRec(pool, k, 0, 0, new int[k], state);
        }

        static void WalkBuriedCombosRec(
            List<XSortActor> pool,
            int k,
            int start,
            int depth,
            int[] index,
            SeekState state)
        {
            if (state.StepsLeft <= 0)
                return;
            if (state.RequireInBand && state.Found)
                return;

            state.StepsLeft--;
            if (depth == k)
            {
                var combo = new List<XSortActor>(k);
                for (int i = 0; i < k; i++)
                    combo.Add(pool[index[i]]);

                int extra;
                if (!PlayBotClickCost.TryGetUncover(
                    state.ColorId, state.BuriedNeed, state.CurrentBoard,
                    out extra, out _, out _, out _, combo))
                    return;

                var selected = new List<XSortActor>(state.Need);
                selected.AddRange(Take(state.Clickable, state.VisibleNeed));
                selected.AddRange(combo);
                Consider(
                    MakePick(state.ColorId, selected, extra),
                    state.MinCost, state.MaxCost, state.RequireInBand,
                    state.ExtraMin, state.ExtraMax,
                    ref state.Found, ref state.BestDist, ref state.BestCost, ref state.Pick);
                return;
            }

            int remain = k - depth;
            for (int i = start; i <= pool.Count - remain; i++)
            {
                if (state.StepsLeft <= 0 || (state.RequireInBand && state.Found))
                    return;

                index[depth] = i;
                WalkBuriedCombosRec(pool, k, i + 1, depth + 1, index, state);
            }
        }

        static void Consider(
            ColorPick candidate,
            int minCost,
            int maxCost,
            bool requireInBand,
            int extraMin,
            int extraMax,
            ref bool found,
            ref int bestDist,
            ref int bestCost,
            ref ColorPick pick)
        {
            if (candidate.XSorts == null || candidate.XSorts.Count < NeedCount)
                return;
            if (candidate.Extra < extraMin || candidate.Extra > extraMax + (requireInBand ? 0 : 8))
                return;

            int dist = DistanceToBand(candidate.ClickCost, minCost, maxCost);
            if (requireInBand && dist != 0)
                return;

            if (!found
                || dist < bestDist
                || (dist == bestDist && candidate.ClickCost < bestCost)
                || (dist == bestDist && candidate.ClickCost == bestCost && candidate.Extra < pick.Extra))
            {
                found = true;
                bestDist = dist;
                bestCost = candidate.ClickCost;
                pick = candidate;
            }
        }

        static ColorPick MakePick(int colorId, List<XSortActor> selected, int extra)
        {
            return new ColorPick
            {
                XSortIndex = colorId,
                Visible = CountVisible(selected),
                Extra = extra,
                ClickCost = NeedCount + extra,
                XSorts = selected
            };
        }

        static void SplitByClickable(
            List<XSortActor> ofColor,
            out List<XSortActor> clickable,
            out List<XSortActor> buried)
        {
            clickable = new List<XSortActor>();
            buried = new List<XSortActor>();
            for (int i = 0; i < ofColor.Count; i++)
            {
                if (PlayBotClickCost.CanPlayerClick(ofColor[i]))
                    clickable.Add(ofColor[i]);
                else
                    buried.Add(ofColor[i]);
            }
        }

        static List<XSortActor> CapBuriedCandidates(List<XSortActor> buried, int cap)
        {
            if (buried == null || buried.Count <= cap)
                return buried ?? new List<XSortActor>();

            var sorted = new List<XSortActor>(buried);
            sorted.Sort(CompareCheapest);
            return sorted.GetRange(0, cap);
        }

        static List<XSortActor> Take(List<XSortActor> source, int count)
        {
            var result = new List<XSortActor>(count);
            if (source == null)
                return result;
            for (int i = 0; i < source.Count && result.Count < count; i++)
                result.Add(source[i]);
            return result;
        }


        static List<XSortActor> SelectCheapestThree(
            List<XSortActor> ofColor,
            List<XSortActor> clickable,
            List<XSortActor> buriedChosen,
            int need)
        {
            var selected = new List<XSortActor>(need);
            var used = new HashSet<XSortActor>();

            for (int i = 0; i < clickable.Count && selected.Count < need; i++)
            {
                if (used.Add(clickable[i]))
                    selected.Add(clickable[i]);
            }

            if (buriedChosen != null)
            {
                for (int i = 0; i < buriedChosen.Count && selected.Count < need; i++)
                {
                    XSortActor XSort = buriedChosen[i];
                    if (XSort == null || !CanConvert(XSort) || !used.Add(XSort))
                        continue;
                    selected.Add(XSort);
                }
            }

            if (selected.Count < need)
            {
                var remain = new List<XSortActor>();
                for (int i = 0; i < ofColor.Count; i++)
                {
                    if (!used.Contains(ofColor[i]))
                        remain.Add(ofColor[i]);
                }

                remain.Sort(CompareCheapest);
                for (int i = 0; i < remain.Count && selected.Count < need; i++)
                    selected.Add(remain[i]);
            }

            return selected;
        }

        static int CompareCheapest(XSortActor a, XSortActor b)
        {
            int clickA = PlayBotClickCost.CanPlayerClick(a) ? 0 : 1;
            int clickB = PlayBotClickCost.CanPlayerClick(b) ? 0 : 1;
            if (clickA != clickB)
                return clickA.CompareTo(clickB);

            List<XSortActor> actXSorts = LevelDirector.LevelGenService?.ActXSorts;
            int extraA = actXSorts == null ? 0 : PlayBotClickCost.ExtraUncoverClicks(a, a.MatchTypeId, actXSorts);
            int extraB = actXSorts == null ? 0 : PlayBotClickCost.ExtraUncoverClicks(b, b.MatchTypeId, actXSorts);
            if (extraA != extraB)
                return extraA.CompareTo(extraB);
            return a.LayerID.CompareTo(b.LayerID);
        }

        static ColorPick ChooseColor(List<ColorPick> colors, int minCost, int maxCost)
        {
            ColorPick bestInBand = default;
            bool hasInBand = false;
            ColorPick nearest = colors[0];
            int nearestDist = DistanceToBand(colors[0].ClickCost, minCost, maxCost);

            for (int i = 0; i < colors.Count; i++)
            {
                ColorPick pick = colors[i];
                int dist = DistanceToBand(pick.ClickCost, minCost, maxCost);
                if (dist == 0)
                {
                    if (!hasInBand
                        || pick.ClickCost < bestInBand.ClickCost
                        || (pick.ClickCost == bestInBand.ClickCost && pick.Extra < bestInBand.Extra))
                    {
                        bestInBand = pick;
                        hasInBand = true;
                    }
                }

                if (dist < nearestDist
                    || (dist == nearestDist && pick.ClickCost < nearest.ClickCost)
                    || (dist == nearestDist
                        && pick.ClickCost == nearest.ClickCost
                        && pick.Extra < nearest.Extra))
                {
                    nearest = pick;
                    nearestDist = dist;
                }
            }

            return hasInBand ? bestInBand : nearest;
        }

        static int CountVisible(List<XSortActor> XSorts)
        {
            int visible = 0;
            if (XSorts == null)
                return 0;
            for (int i = 0; i < XSorts.Count; i++)
            {
                if (PlayBotClickCost.CanPlayerClick(XSorts[i]))
                    visible++;
            }

            return visible;
        }

        static string FormatColorDump(List<ColorPick> colors, int minCost, int maxCost)
        {
            if (colors == null || colors.Count == 0)
                return "colors=0";

            var parts = new List<string>(colors.Count);
            for (int i = 0; i < colors.Count; i++)
            {
                ColorPick pick = colors[i];
                parts.Add(
                    "c" + pick.XSortIndex +
                    ":cost" + pick.ClickCost +
                    "/e" + pick.Extra +
                    "/v" + pick.Visible +
                    (DistanceToBand(pick.ClickCost, minCost, maxCost) == 0 ? "*" : ""));
            }

            return string.Join(" ", parts);
        }

        static int DistanceToBand(int cost, int minCost, int maxCost)
        {
            if (cost < minCost)
                return minCost - cost;
            if (cost > maxCost)
                return cost - maxCost;
            return 0;
        }
    }
}
