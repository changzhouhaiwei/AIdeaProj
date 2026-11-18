using System.Collections.Generic;
using UnityEngine;

namespace FishTilePlay
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
            public int TileIndex;
            public int ClickCost;
            public int Extra;
            public int Visible;
            public List<TileActor> Tiles;
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

        public static List<TileActor> PickTargets(int clearedCount)
        {
            GetCostBand(clearedCount, out int minCost, out int maxCost);
            return PickTargets(minCost, maxCost, NeedCount);
        }

        public static List<TileActor> PickTargets(int minCost, int maxCost, int need)
        {
            ResetLastSelection();
            Physics.SyncTransforms();

            List<TileActor> currentBoard = CollectCurrentBoardSnapshot();
            List<TileActor> board = CollectConvertibleBoardTiles(currentBoard);
            if (board.Count == 0)
            {
                LogNoSelection("board empty");
                return new List<TileActor>();
            }

            List<ColorPick> colors = BuildColorPicks(board, currentBoard, need, minCost, maxCost);
            LastColorDump = FormatColorDump(colors, minCost, maxCost);

            List<TileActor> chosen = null;
            ColorPick chosenPick = default;
            bool mixed = false;
            if (colors.Count > 0)
            {
                chosenPick = ChooseColor(colors, minCost, maxCost);
                chosen = chosenPick.Tiles;
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
                        TileIndex = -1,
                        Visible = CountVisible(chosen),
                        Extra = extra,
                        ClickCost = clickCost,
                        Tiles = chosen
                    };
                }
            }

            if (!IsPlayableSet(chosen, currentBoard, need))
            {
                LogNoSelection($"no playable trio, convertible={board.Count} band=[{minCost},{maxCost}]");
                return new List<TileActor>();
            }

            RememberSelection(chosenPick, minCost, maxCost, mixed);
            return chosen;
        }

        static bool IsPlayableSet(List<TileActor> tiles, List<TileActor> currentBoard, int need)
        {
            if (tiles == null || tiles.Count != need || currentBoard == null)
                return false;

            var seen = new HashSet<TileActor>();
            for (int i = 0; i < tiles.Count; i++)
            {
                TileActor tile = tiles[i];
                if (!CanConvert(tile) || !seen.Add(tile) || !currentBoard.Contains(tile))
                    return false;
            }

            if (!PlayBotClickCost.TryGetCurrentClickCost(tiles, currentBoard, out int clickCost, out _))
                return false;
            if (clickCost >= PlayBotClickCost.UncoverImpossible)
                return false;

            return HasClickableEntry(tiles, currentBoard);
        }

        static bool HasClickableEntry(List<TileActor> tiles, List<TileActor> currentBoard)
        {
            var blockers = new HashSet<TileActor>();
            for (int i = 0; i < tiles.Count; i++)
            {
                TileActor tile = tiles[i];
                if (PlayBotClickCost.CanPlayerClick(tile))
                    return true;
                PlayBotClickCost.CollectMustRemove(tile, -1, currentBoard, blockers);
            }

            foreach (TileActor blocker in blockers)
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
        static List<TileActor> BuildPlayableFallback(
            List<TileActor> board,
            List<TileActor> currentBoard,
            int need)
        {
            var clickable = new List<TileActor>();
            var buried = new List<TileActor>();
            for (int i = 0; i < board.Count; i++)
            {
                TileActor tile = board[i];
                if (PlayBotClickCost.CanPlayerClick(tile))
                    clickable.Add(tile);
                else
                    buried.Add(tile);
            }

            if (clickable.Count >= need)
                return Take(clickable, need);

            buried.Sort(CompareCheapest);
            var selected = new List<TileActor>(clickable);
            for (int i = 0; i < buried.Count && selected.Count < need; i++)
            {
                selected.Add(buried[i]);
                if (!IsPlayableSet(selected, currentBoard, selected.Count))
                    selected.RemoveAt(selected.Count - 1);
            }

            if (selected.Count != need)
                return new List<TileActor>();
            return selected;
        }

        public static bool CanConvert(TileActor tile)
        {
            if (tile == null || tile.IsSubmitted || tile.IsFission)
                return false;
            if (tile.TileModel == null || tile.IsCoinReward)
                return false;
            if (tile.Effect != null)
                return false;
            if (BoxCoverManager.IsTileBlocked(tile))
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
                $"color={pick.TileIndex} cost={pick.ClickCost} need={NeedCount} " +
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

        static List<TileActor> CollectCurrentBoardSnapshot()
        {
            var result = new List<TileActor>();
            List<TileActor> actTiles = LevelDirector.LevelGenService?.ActTiles;
            if (actTiles == null)
                return result;

            for (int i = 0; i < actTiles.Count; i++)
            {
                TileActor tile = actTiles[i];
                if (tile == null || tile.IsSubmitted || !tile.gameObject.activeInHierarchy)
                    continue;
                result.Add(tile);
            }

            return result;
        }

        static List<TileActor> CollectConvertibleBoardTiles(List<TileActor> currentBoard)
        {
            var result = new List<TileActor>();
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
            List<TileActor> board,
            List<TileActor> currentBoard,
            int need,
            int minCost,
            int maxCost)
        {
            var byColor = new Dictionary<int, List<TileActor>>();
            for (int i = 0; i < board.Count; i++)
            {
                int id = board[i].MatchTypeId;
                if (!byColor.TryGetValue(id, out List<TileActor> list))
                {
                    list = new List<TileActor>();
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
            List<TileActor> ofColor,
            List<TileActor> currentBoard,
            int need,
            int minCost,
            int maxCost,
            out ColorPick pick)
        {
            pick = default;
            SplitByClickable(ofColor, out List<TileActor> clickable, out List<TileActor> buried);

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
            List<TileActor> ofColor,
            List<TileActor> clickable,
            List<TileActor> buried,
            List<TileActor> currentBoard,
            int need,
            out ColorPick pick)
        {
            pick = default;
            int extra = 0;
            List<TileActor> buriedChosen = null;
            int shortage = Mathf.Max(0, need - clickable.Count);
            if (shortage > 0)
            {
                if (currentBoard == null
                    || !PlayBotClickCost.TryGetUncover(
                        colorId, shortage, currentBoard,
                        out extra, out _, out _, out buriedChosen, ofColor))
                    return false;
            }

            List<TileActor> selected = SelectCheapestThree(ofColor, clickable, buriedChosen, need);
            if (selected.Count < need)
                return false;

            pick = MakePick(colorId, selected, extra);
            return true;
        }

        static bool TrySeekBandPick(
            int colorId,
            List<TileActor> clickable,
            List<TileActor> buried,
            List<TileActor> currentBoard,
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
            List<TileActor> clickable,
            List<TileActor> buried,
            List<TileActor> currentBoard,
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
            List<TileActor> clickable,
            List<TileActor> buried,
            List<TileActor> currentBoard,
            int need,
            int minCost,
            int maxCost,
            bool requireInBand,
            ColorPick fallback,
            out ColorPick pick)
        {
            pick = fallback;
            bool found = !requireInBand && fallback.Tiles != null && fallback.Tiles.Count >= need;
            int bestDist = found ? DistanceToBand(fallback.ClickCost, minCost, maxCost) : int.MaxValue;
            int bestCost = found ? fallback.ClickCost : int.MaxValue;

            List<TileActor> buriedPool = CapBuriedCandidates(buried, 10);
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

            return found && pick.Tiles != null && pick.Tiles.Count >= need;
        }

        sealed class SeekState
        {
            public int ColorId;
            public List<TileActor> Clickable;
            public List<TileActor> CurrentBoard;
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

        static void WalkBuriedCombos(List<TileActor> pool, int k, SeekState state)
        {
            if (pool == null || state == null || k <= 0 || pool.Count < k)
                return;
            WalkBuriedCombosRec(pool, k, 0, 0, new int[k], state);
        }

        static void WalkBuriedCombosRec(
            List<TileActor> pool,
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
                var combo = new List<TileActor>(k);
                for (int i = 0; i < k; i++)
                    combo.Add(pool[index[i]]);

                int extra;
                if (!PlayBotClickCost.TryGetUncover(
                    state.ColorId, state.BuriedNeed, state.CurrentBoard,
                    out extra, out _, out _, out _, combo))
                    return;

                var selected = new List<TileActor>(state.Need);
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
            if (candidate.Tiles == null || candidate.Tiles.Count < NeedCount)
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

        static ColorPick MakePick(int colorId, List<TileActor> selected, int extra)
        {
            return new ColorPick
            {
                TileIndex = colorId,
                Visible = CountVisible(selected),
                Extra = extra,
                ClickCost = NeedCount + extra,
                Tiles = selected
            };
        }

        static void SplitByClickable(
            List<TileActor> ofColor,
            out List<TileActor> clickable,
            out List<TileActor> buried)
        {
            clickable = new List<TileActor>();
            buried = new List<TileActor>();
            for (int i = 0; i < ofColor.Count; i++)
            {
                if (PlayBotClickCost.CanPlayerClick(ofColor[i]))
                    clickable.Add(ofColor[i]);
                else
                    buried.Add(ofColor[i]);
            }
        }

        static List<TileActor> CapBuriedCandidates(List<TileActor> buried, int cap)
        {
            if (buried == null || buried.Count <= cap)
                return buried ?? new List<TileActor>();

            var sorted = new List<TileActor>(buried);
            sorted.Sort(CompareCheapest);
            return sorted.GetRange(0, cap);
        }

        static List<TileActor> Take(List<TileActor> source, int count)
        {
            var result = new List<TileActor>(count);
            if (source == null)
                return result;
            for (int i = 0; i < source.Count && result.Count < count; i++)
                result.Add(source[i]);
            return result;
        }


        static List<TileActor> SelectCheapestThree(
            List<TileActor> ofColor,
            List<TileActor> clickable,
            List<TileActor> buriedChosen,
            int need)
        {
            var selected = new List<TileActor>(need);
            var used = new HashSet<TileActor>();

            for (int i = 0; i < clickable.Count && selected.Count < need; i++)
            {
                if (used.Add(clickable[i]))
                    selected.Add(clickable[i]);
            }

            if (buriedChosen != null)
            {
                for (int i = 0; i < buriedChosen.Count && selected.Count < need; i++)
                {
                    TileActor tile = buriedChosen[i];
                    if (tile == null || !CanConvert(tile) || !used.Add(tile))
                        continue;
                    selected.Add(tile);
                }
            }

            if (selected.Count < need)
            {
                var remain = new List<TileActor>();
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

        static int CompareCheapest(TileActor a, TileActor b)
        {
            int clickA = PlayBotClickCost.CanPlayerClick(a) ? 0 : 1;
            int clickB = PlayBotClickCost.CanPlayerClick(b) ? 0 : 1;
            if (clickA != clickB)
                return clickA.CompareTo(clickB);

            List<TileActor> actTiles = LevelDirector.LevelGenService?.ActTiles;
            int extraA = actTiles == null ? 0 : PlayBotClickCost.ExtraUncoverClicks(a, a.MatchTypeId, actTiles);
            int extraB = actTiles == null ? 0 : PlayBotClickCost.ExtraUncoverClicks(b, b.MatchTypeId, actTiles);
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

        static int CountVisible(List<TileActor> tiles)
        {
            int visible = 0;
            if (tiles == null)
                return 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (PlayBotClickCost.CanPlayerClick(tiles[i]))
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
                    "c" + pick.TileIndex +
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
