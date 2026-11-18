#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TileGameCore;

namespace FishTilePlay
{
    public enum PlayBotNoticeMode
    {
        All = 0,
        Occlusion = 1
    }

    /// <summary>
    /// 选子：Need / 空格 / 棋盘分布 / 暂存分布，外加同色挤成一团的局部三连。
    /// PlayBot Skilled / Perfect 与关卡预跑共用此逻辑。
    /// </summary>
    public sealed class PlayBotPlanner
    {
        public const int MatchSize = 3;
        public const int RandomCategory = 0;
        const int UncoverImpossible = 999;
        const int OcclusionSampleAxis = 8;
        const float VisibleFloor = 0.08f;
        const int ComboSearchCap = 14;

        public struct ColorPlan
        {
            public int ColorId;
            public int Need;
            public int Slots;
            public int BoardVisible;
            public int BoardTotal;
            public int InDock;
            public int Extra;
            public int ClickCost;
            public int UncoverRaw;
            public float Occlusion;
            public int Cluster;
            public int Pile;

            /// <summary>
            /// 同色挤成一团：至少 2 个可点贴在一起，团里总共至少 3 颗，揭盖不超过 1。
            /// </summary>
            public bool IsLocalTriple => Cluster >= 2 && Pile >= MatchSize && Extra <= 2;

            /// <summary>
            /// 预跑 CSV 仍按旧 P1–P5 记账：Need 1/2/3 对应 P1/P2/P3，Need=3 且要揭盖时 P4/P5。
            /// </summary>
            public int Category
            {
                get
                {
                    if (Need <= 1)
                        return 1;
                    if (Need == 2)
                        return 2;
                    if (Extra <= 0)
                        return 3;
                    return BoardVisible >= 2 ? 4 : 5;
                }
            }
        }

        int _commitColorId = -1;
        bool _commitHadInDock;
        int _lastCategory = RandomCategory;
        int _lastNeed;
        int _lastExtra;

        public int LastCategory => _lastCategory;
        public int LastNeed => _lastNeed;
        public int LastExtra => _lastExtra;
        public int CommitColorId => _commitColorId;
        public bool LastWasNewPlan { get; private set; }
        public StepInfo LastStep;

        public struct StepInfo
        {
            public string Action;
            public string Why;
            public int TargetColor;
            public int ClickColor;
            public bool IsCover;
            public int Need;
            public int Extra;
            public int Slots;
            public int BoardVisible;
            public int BoardTotal;
            public int InDock;
            public int BestColor;
            public int BestNeed;
            public int PlanCount;
            public int NoticedCount;
            public string DockHist;
            public string BoardHist;
        }

        public void Reset()
        {
            _commitColorId = -1;
            _commitHadInDock = false;
            _lastCategory = RandomCategory;
            _lastNeed = 0;
            _lastExtra = 0;
            LastWasNewPlan = false;
            LastStep = default;
        }

        public TileActor PickNext(PlayBotNoticeMode notice = PlayBotNoticeMode.Occlusion)
        {
            LastWasNewPlan = false;
            LastStep = default;
            if (!LevelDirector.SubmitIsAllowed())
                return null;
            if (LevelDirector.LevelGenService == null || LevelDirector.LevelGenService.ActTiles == null)
                return null;

            List<TileActor> clickable = CollectClickableTiles();
            if (clickable.Count == 0)
            {
                ClearCommit();
                return null;
            }

            Dictionary<int, int> dockCounts = CollectDockColorCounts();
            List<TileActor> actTiles = LevelDirector.LevelGenService.ActTiles;
            Dictionary<int, int> boardVisible = CountByColor(clickable);
            Dictionary<int, int> boardTotal = CountBoardRemaining(actTiles);
            int slotsAvailable = DockActor.GetSlotsAvailable();
            List<ColorPlan> plans = CollectPlans(
                clickable, dockCounts, boardVisible, boardTotal, slotsAvailable, actTiles);
            List<ColorPlan> noticed = notice == PlayBotNoticeMode.All
                ? plans
                : FilterNoticedPlans(plans);

            int bestColor = PickBestColor(noticed);
            int bestNeed = 0;
            if (bestColor >= 0 && TryFindPlan(noticed, bestColor, out ColorPlan bestPlan))
                bestNeed = bestPlan.Need;

            string afterClear = "";
            if (_commitColorId >= 0)
            {
                dockCounts.TryGetValue(_commitColorId, out int inDock);
                if (inDock > 0)
                    _commitHadInDock = true;

                if (_commitHadInDock && inDock == 0)
                {
                    afterClear = $"clear-{_commitColorId}";
                    ClearCommit();
                }
                else if (!ShouldBreakCommit(_commitColorId, bestColor, noticed, dockCounts, out string breakWhy))
                {
                    TileActor committed = PickCommittedTile(_commitColorId, clickable, actTiles);
                    if (committed != null)
                    {
                        LastWasNewPlan = false;
                        RememberPlan(plans, _commitColorId);
                        return FinishStep(
                            committed.TileModel != null && committed.MatchTypeId == _commitColorId
                                ? "KEEP"
                                : "UNCOVER",
                            "lock",
                            committed,
                            _commitColorId,
                            plans,
                            bestColor,
                            bestNeed,
                            plans.Count,
                            noticed.Count,
                            dockCounts,
                            boardVisible,
                            boardTotal,
                            slotsAvailable);
                    }

                    ClearCommit();
                    afterClear = "lock-no-tile";
                }
                else
                {
                    afterClear = breakWhy;
                    ClearCommit();
                }
            }

            if (bestColor >= 0)
            {
                TileActor tile = PickCommittedTile(bestColor, clickable, actTiles);
                if (tile != null)
                {
                    LastWasNewPlan = true;
                    RememberPlan(noticed, bestColor);
                    CommitColor(bestColor, dockCounts, tile);
                    return FinishStep(
                        tile.TileModel != null && tile.MatchTypeId == bestColor ? "NEW" : "NEW-UNCOVER",
                        string.IsNullOrEmpty(afterClear) ? "best" : afterClear,
                        tile,
                        bestColor,
                        plans,
                        bestColor,
                        bestNeed,
                        plans.Count,
                        noticed.Count,
                        dockCounts,
                        boardVisible,
                        boardTotal,
                        slotsAvailable);
                }
            }

            ClearCommit();
            LastWasNewPlan = true;
            _lastCategory = RandomCategory;
            _lastNeed = 0;
            _lastExtra = 0;
            TileActor random = PickRandomClickable(clickable, dockCounts, slotsAvailable);
            int randomColor = random != null && random.TileModel != null ? random.MatchTypeId : -1;
            return FinishStep(
                "RANDOM",
                string.IsNullOrEmpty(afterClear) ? "no-plan" : afterClear,
                random,
                randomColor,
                plans,
                bestColor,
                bestNeed,
                plans.Count,
                noticed.Count,
                dockCounts,
                boardVisible,
                boardTotal,
                slotsAvailable);
        }

        public void PublishStep(StepInfo step)
        {
            LastStep = step;
        }

        public static StepInfo MakeStep(
            string action,
            string why,
            TileActor tile,
            int targetColor,
            List<ColorPlan> plans,
            int bestColor,
            int bestNeed,
            int planCount,
            int noticedCount,
            Dictionary<int, int> dockCounts,
            Dictionary<int, int> boardVisible,
            Dictionary<int, int> boardTotal,
            int slotsAvailable)
        {
            var step = new StepInfo
            {
                Action = action,
                Why = why ?? "",
                TargetColor = targetColor,
                BestColor = bestColor,
                BestNeed = bestNeed,
                PlanCount = planCount,
                NoticedCount = noticedCount,
                Slots = slotsAvailable,
                DockHist = Hist(dockCounts),
                BoardHist = Hist(boardVisible)
            };
            if (tile != null && tile.TileModel != null)
            {
                step.ClickColor = tile.MatchTypeId;
                step.IsCover = targetColor >= 0 && step.ClickColor != targetColor;
            }

            if (targetColor >= 0 && plans != null && TryFindPlan(plans, targetColor, out ColorPlan plan))
            {
                step.Need = plan.Need;
                step.Extra = plan.Extra;
                step.Slots = plan.Slots;
                step.BoardVisible = plan.BoardVisible;
                step.BoardTotal = plan.BoardTotal;
                step.InDock = plan.InDock;
            }
            else
            {
                if (dockCounts != null)
                    dockCounts.TryGetValue(targetColor, out step.InDock);
                if (boardVisible != null)
                    boardVisible.TryGetValue(targetColor, out step.BoardVisible);
                if (boardTotal != null)
                    boardTotal.TryGetValue(targetColor, out step.BoardTotal);
                step.Need = MatchSize - step.InDock;
            }

            return step;
        }

        TileActor FinishStep(
            string action,
            string why,
            TileActor tile,
            int targetColor,
            List<ColorPlan> plans,
            int bestColor,
            int bestNeed,
            int planCount,
            int noticedCount,
            Dictionary<int, int> dockCounts,
            Dictionary<int, int> boardVisible,
            Dictionary<int, int> boardTotal,
            int slotsAvailable)
        {
            LastStep = MakeStep(
                action, why, tile, targetColor, plans, bestColor, bestNeed,
                planCount, noticedCount, dockCounts, boardVisible, boardTotal, slotsAvailable);
            return tile;
        }

        public static string Hist(Dictionary<int, int> counts)
        {
            if (counts == null || counts.Count == 0)
                return "";
            var parts = new List<string>();
            foreach (var pair in counts)
            {
                if (pair.Value > 0)
                    parts.Add($"{pair.Key}:{pair.Value}");
            }

            parts.Sort((a, b) => string.CompareOrdinal(a, b));
            if (parts.Count > 8)
                parts.RemoveRange(8, parts.Count - 8);
            return string.Join(",", parts);
        }

        public static void SimClick(TileActor tile)
        {
            if (tile == null || tile.IsSubmitted)
                return;
            if (!LevelDirector.SubmitIsAllowed())
                return;
            if (tile.Effect != null && !tile.Effect.IsClickAllowed())
                return;
            if (!CanPlayerClick(tile))
                return;

            if (tile.Effect == null || tile.Effect.BeforeTileSubmitted())
                LevelDirector.SubmitElement(tile);
        }

        TileActor CommitColor(int colorId, Dictionary<int, int> dockCounts, TileActor tile)
        {
            _commitColorId = colorId;
            dockCounts.TryGetValue(colorId, out int inDock);
            _commitHadInDock = inDock > 0;
            return tile;
        }

        void ClearCommit()
        {
            _commitColorId = -1;
            _commitHadInDock = false;
        }

        void RememberPlan(List<ColorPlan> plans, int colorId)
        {
            if (TryFindPlan(plans, colorId, out ColorPlan found))
            {
                _lastCategory = found.Category;
                _lastNeed = found.Need;
                _lastExtra = found.Extra;
                return;
            }

            _lastCategory = RandomCategory;
            _lastNeed = 0;
            _lastExtra = 0;
        }

        static bool ShouldBreakCommit(
            int commitColorId,
            int bestColor,
            List<ColorPlan> noticed,
            Dictionary<int, int> dockCounts,
            out string why)
        {
            why = "";
            dockCounts.TryGetValue(commitColorId, out int inDock);
            int commitNeed = MatchSize - inDock;
            if (inDock >= 2)
                return false;
            if (bestColor < 0 || bestColor == commitColorId)
                return false;
            if (!TryFindPlan(noticed, bestColor, out ColorPlan best))
                return false;

            int commitGroup = 5;
            if (TryFindPlan(noticed, commitColorId, out ColorPlan commit))
                commitGroup = RankGroup(commit);

            int bestGroup = RankGroup(best);
            if (bestGroup < commitGroup)
            {
                why = $"break-group {bestGroup}<{commitGroup} color={bestColor}";
                return true;
            }

            if (best.Need < commitNeed)
            {
                why = $"break-need {best.Need}<{commitNeed} color={bestColor}";
                return true;
            }

            if (inDock > 0)
                return false;
            if (commitGroup >= 5)
                return false;
            if (commit.Extra > 0 && best.Need == commitNeed && best.Extra == 0)
            {
                why = $"break-visible color={bestColor}";
                return true;
            }

            return false;
        }

        public static TileActor PickCommittedTile(int colorId, List<TileActor> clickable, List<TileActor> actTiles)
        {
            var sameColor = new List<TileActor>();
            if (clickable != null)
            {
                for (int i = 0; i < clickable.Count; i++)
                {
                    if (clickable[i] != null && clickable[i].TileModel != null
                        && clickable[i].MatchTypeId == colorId)
                        sameColor.Add(clickable[i]);
                }
            }

            if (sameColor.Count > 0)
            {
                List<TileActor> cluster = LargestTouchingGroup(sameColor);
                List<TileActor> pool = cluster.Count > 0 ? cluster : sameColor;
                return pool[UnityEngine.Random.Range(0, pool.Count)];
            }

            return FindCoverToUncoverColor(colorId, clickable, actTiles);
        }

        public static int PickBestColor(List<ColorPlan> plans)
        {
            if (plans == null || plans.Count == 0)
                return -1;

            ColorPlan best = BestPlan(plans);
            var ties = new List<int>();
            for (int i = 0; i < plans.Count; i++)
            {
                ColorPlan p = plans[i];
                if (ComparePlans(p, best) == 0)
                    ties.Add(p.ColorId);
            }

            return ties[UnityEngine.Random.Range(0, ties.Count)];
        }

        public static ColorPlan BestPlan(List<ColorPlan> plans)
        {
            ColorPlan best = plans[0];
            for (int i = 1; i < plans.Count; i++)
            {
                if (ComparePlans(plans[i], best) < 0)
                    best = plans[i];
            }

            return best;
        }

        static int RankGroup(ColorPlan plan)
        {
            if (plan.Need <= 1 && plan.Extra <= 0)
                return 0;
            if (plan.Need <= 1)
                return 1;
            if (plan.IsLocalTriple || (plan.Extra <= 0 && plan.BoardVisible >= MatchSize))
                return 2;
            if (plan.Need == 2 && plan.Extra <= 0)
                return 3;
            if (plan.Need == 2)
                return 4;
            return 5;
        }

        static int ComparePlans(ColorPlan a, ColorPlan b)
        {
            int groupA = RankGroup(a);
            int groupB = RankGroup(b);
            if (groupA != groupB)
                return groupA.CompareTo(groupB);
            if (a.Extra != b.Extra)
                return a.Extra.CompareTo(b.Extra);
            if (a.Need != b.Need)
                return a.Need.CompareTo(b.Need);
            if (a.Pile != b.Pile)
                return b.Pile.CompareTo(a.Pile);
            if (a.Cluster != b.Cluster)
                return b.Cluster.CompareTo(a.Cluster);
            if (a.UncoverRaw != b.UncoverRaw)
                return a.UncoverRaw.CompareTo(b.UncoverRaw);
            if (a.BoardVisible != b.BoardVisible)
                return b.BoardVisible.CompareTo(a.BoardVisible);
            return 0;
        }

        public static bool TryFindPlan(List<ColorPlan> plans, int colorId, out ColorPlan found)
        {
            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].ColorId != colorId)
                    continue;
                found = plans[i];
                return true;
            }

            found = default;
            return false;
        }

        public static List<ColorPlan> CollectPlans(
            List<TileActor> clickable,
            Dictionary<int, int> dockCounts,
            Dictionary<int, int> boardVisible,
            int slotsAvailable,
            List<TileActor> actTiles)
        {
            return CollectPlans(
                clickable,
                dockCounts,
                boardVisible,
                CountBoardRemaining(actTiles),
                slotsAvailable,
                actTiles);
        }

        public static List<ColorPlan> CollectPlans(
            List<TileActor> clickable,
            Dictionary<int, int> dockCounts,
            Dictionary<int, int> boardVisible,
            Dictionary<int, int> boardTotal,
            int slotsAvailable,
            List<TileActor> actTiles)
        {
            var plans = new List<ColorPlan>();
            var seenColors = new HashSet<int>();

            if (clickable != null)
            {
                for (int i = 0; i < clickable.Count; i++)
                {
                    if (clickable[i] != null && clickable[i].TileModel != null)
                        seenColors.Add(clickable[i].MatchTypeId);
                }
            }

            if (dockCounts != null)
            {
                foreach (var pair in dockCounts)
                {
                    if (pair.Value > 0)
                        seenColors.Add(pair.Key);
                }
            }

            foreach (int colorId in seenColors)
            {
                if (!TryBuildPlan(colorId, dockCounts, boardVisible, boardTotal, slotsAvailable, actTiles,
                        out ColorPlan plan))
                    continue;
                plans.Add(plan);
            }

            return plans;
        }

        static List<ColorPlan> FilterNoticedPlans(List<ColorPlan> plans)
        {
            var noticed = new List<ColorPlan>();
            for (int i = 0; i < plans.Count; i++)
            {
                if (NoticePlan(plans[i]))
                    noticed.Add(plans[i]);
            }

            return noticed;
        }

        /// <summary>
        /// 可点计划总能看见。暂存区已有这色且场上有可点同色时，熟练玩家会去找第 3 颗，不因遮挡漏掉。
        /// 其余揭盖计划：遮挡越大越难认出；可见低于约 8% 当看不见。
        /// </summary>
        static bool NoticePlan(ColorPlan plan)
        {
            if (plan.InDock >= 2)
                return true;
            if (plan.InDock > 0 && plan.BoardVisible > 0)
                return true;
            if (plan.IsLocalTriple)
                return true;

            if (plan.Occlusion <= 0.02f)
                return true;

            float visible = 1f - plan.Occlusion;
            if (visible < VisibleFloor)
                return false;

            return UnityEngine.Random.value < visible;
        }

        public static bool TryBuildPlan(
            int colorId,
            Dictionary<int, int> dockCounts,
            Dictionary<int, int> boardVisible,
            int slotsAvailable,
            List<TileActor> actTiles,
            out ColorPlan plan)
        {
            return TryBuildPlan(
                colorId,
                dockCounts,
                boardVisible,
                CountBoardRemaining(actTiles),
                slotsAvailable,
                actTiles,
                out plan);
        }

        /// <summary>
        /// 一次消除 = 凑满 MatchSize。Need = MatchSize − 暂存该色。
        /// 空格必须装得下 Need 颗目标色 + Extra 颗揭盖。棋盘总量不够则不成计划。
        /// </summary>
        public static bool TryBuildPlan(
            int colorId,
            Dictionary<int, int> dockCounts,
            Dictionary<int, int> boardVisible,
            Dictionary<int, int> boardTotal,
            int slotsAvailable,
            List<TileActor> actTiles,
            out ColorPlan plan)
        {
            plan = default;
            int inDock = 0;
            int visible = 0;
            int total = 0;
            if (dockCounts != null)
                dockCounts.TryGetValue(colorId, out inDock);
            if (boardVisible != null)
                boardVisible.TryGetValue(colorId, out visible);
            if (boardTotal != null)
                boardTotal.TryGetValue(colorId, out total);

            if (inDock >= MatchSize)
                return false;

            int need = MatchSize - inDock;
            if (need <= 0)
                return false;
            if (total < need)
                return false;

            int extra = 0;
            int uncoverRaw = 0;
            float occlusion = 0f;
            int shortage = Math.Max(0, need - visible);
            if (shortage > 0)
            {
                if (!TryGetUncover(colorId, shortage, actTiles, out extra, out uncoverRaw, out occlusion))
                    return false;
            }

            if (need + extra > slotsAvailable)
                return false;

            MeasurePile(colorId, actTiles, out int cluster, out int pile);
            plan = new ColorPlan
            {
                ColorId = colorId,
                Need = need,
                Slots = slotsAvailable,
                BoardVisible = visible,
                BoardTotal = total,
                InDock = inDock,
                Extra = extra,
                ClickCost = need + extra,
                UncoverRaw = uncoverRaw,
                Occlusion = occlusion,
                Cluster = cluster,
                Pile = pile
            };
            return true;
        }

        static bool TryGetUncover(
            int colorId,
            int shortage,
            List<TileActor> actTiles,
            out int extra,
            out int raw,
            out float occlusion)
        {
            return PlayBotClickCost.TryGetUncover(colorId, shortage, actTiles, out extra, out raw, out occlusion, out _);
        }

        static bool TryPickBuriedSetGreedy(
            List<TileActor> buried,
            List<HashSet<TileActor>> removes,
            int[] raws,
            float[] occs,
            int shortage,
            out int extra,
            out int raw,
            out float occlusion)
        {
            extra = UncoverImpossible;
            raw = UncoverImpossible;
            occlusion = 1f;

            var order = new List<int>(buried.Count);
            for (int i = 0; i < buried.Count; i++)
                order.Add(i);
            order.Sort((a, b) =>
            {
                int byExtra = removes[a].Count.CompareTo(removes[b].Count);
                if (byExtra != 0)
                    return byExtra;
                int byRaw = raws[a].CompareTo(raws[b]);
                if (byRaw != 0)
                    return byRaw;
                return occs[a].CompareTo(occs[b]);
            });

            if (shortage == 1)
            {
                ScoreBuriedSet(buried, removes, raws, occs, new[] { order[0] }, out extra, out raw, out occlusion);
                return extra < UncoverImpossible;
            }

            if (order.Count < shortage)
                return false;

            var pick = new int[shortage];
            for (int i = 0; i < shortage; i++)
                pick[i] = order[i];
            ScoreBuriedSet(buried, removes, raws, occs, pick, out extra, out raw, out occlusion);
            return extra < UncoverImpossible;
        }

        static bool TryPickBuriedSetExact(
            List<TileActor> buried,
            List<HashSet<TileActor>> removes,
            int[] raws,
            float[] occs,
            int shortage,
            out int extra,
            out int raw,
            out float occlusion)
        {
            extra = UncoverImpossible;
            raw = UncoverImpossible;
            occlusion = 1f;
            var pick = new int[shortage];
            int bestExtra = UncoverImpossible;
            int bestRaw = UncoverImpossible;
            float bestOcc = 1f;
            bool found = false;
            SearchBuriedCombos(
                buried, removes, raws, occs, pick, shortage, 0, 0,
                ref found, ref bestExtra, ref bestRaw, ref bestOcc);
            extra = bestExtra;
            raw = bestRaw;
            occlusion = bestOcc;
            return found;
        }

        static void SearchBuriedCombos(
            List<TileActor> buried,
            List<HashSet<TileActor>> removes,
            int[] raws,
            float[] occs,
            int[] pick,
            int shortage,
            int start,
            int depth,
            ref bool found,
            ref int bestExtra,
            ref int bestRaw,
            ref float bestOcc)
        {
            if (depth == shortage)
            {
                ScoreBuriedSet(buried, removes, raws, occs, pick, out int e, out int r, out float o);
                if (!found || e < bestExtra || (e == bestExtra && r < bestRaw)
                    || (e == bestExtra && r == bestRaw && o < bestOcc))
                {
                    bestExtra = e;
                    bestRaw = r;
                    bestOcc = o;
                    found = true;
                }

                return;
            }

            int remain = shortage - depth;
            for (int i = start; i <= buried.Count - remain; i++)
            {
                pick[depth] = i;
                SearchBuriedCombos(
                    buried, removes, raws, occs, pick, shortage, i + 1, depth + 1,
                    ref found, ref bestExtra, ref bestRaw, ref bestOcc);
            }
        }

        static void ScoreBuriedSet(
            List<TileActor> buried,
            List<HashSet<TileActor>> removes,
            int[] raws,
            float[] occs,
            int[] pick,
            out int extra,
            out int raw,
            out float occlusion)
        {
            var union = new HashSet<TileActor>();
            var chosen = new HashSet<TileActor>();
            raw = 0;
            occlusion = 0f;
            for (int n = 0; n < pick.Length; n++)
            {
                int i = pick[n];
                chosen.Add(buried[i]);
                union.UnionWith(removes[i]);
                raw += raws[i];
                if (occs[i] > occlusion)
                    occlusion = occs[i];
            }

            foreach (TileActor tile in chosen)
                union.Remove(tile);

            extra = union.Count;
        }

        static void MeasurePile(int colorId, List<TileActor> actTiles, out int cluster, out int pile)
        {
            cluster = 0;
            pile = 0;
            List<TileActor> clickable = CollectClickableOfColor(colorId, actTiles);
            if (clickable.Count == 0)
                return;

            List<TileActor> largest = LargestTouchingGroup(clickable);
            cluster = largest.Count;
            if (cluster == 0)
                return;

            var pileSet = new HashSet<TileActor>(largest);
            bool grew = true;
            while (grew)
            {
                grew = false;
                if (actTiles == null)
                    break;
                for (int i = 0; i < actTiles.Count; i++)
                {
                    TileActor tile = actTiles[i];
                    if (tile == null || tile.IsSubmitted || tile.TileModel == null)
                        continue;
                    if (tile.MatchTypeId != colorId)
                        continue;
                    if (pileSet.Contains(tile))
                        continue;
                    if (!TouchesAny(tile, pileSet))
                        continue;
                    pileSet.Add(tile);
                    grew = true;
                }
            }

            pile = pileSet.Count;
        }

        static List<TileActor> CollectClickableOfColor(int colorId, List<TileActor> actTiles)
        {
            var result = new List<TileActor>();
            if (actTiles == null)
                return result;
            for (int i = 0; i < actTiles.Count; i++)
            {
                TileActor tile = actTiles[i];
                if (tile == null || tile.TileModel == null)
                    continue;
                if (tile.MatchTypeId != colorId)
                    continue;
                if (!CanPlayerClick(tile))
                    continue;
                result.Add(tile);
            }

            return result;
        }

        static List<TileActor> LargestTouchingGroup(List<TileActor> tiles)
        {
            var best = new List<TileActor>();
            if (tiles == null || tiles.Count == 0)
                return best;

            var visited = new bool[tiles.Count];
            for (int i = 0; i < tiles.Count; i++)
            {
                if (visited[i])
                    continue;
                var group = new List<TileActor>();
                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;
                while (queue.Count > 0)
                {
                    int idx = queue.Dequeue();
                    group.Add(tiles[idx]);
                    for (int j = 0; j < tiles.Count; j++)
                    {
                        if (visited[j])
                            continue;
                        if (!TouchesOnXY(tiles[idx], tiles[j]))
                            continue;
                        visited[j] = true;
                        queue.Enqueue(j);
                    }
                }

                if (group.Count > best.Count)
                    best = group;
            }

            return best;
        }

        static bool TouchesAny(TileActor tile, HashSet<TileActor> others)
        {
            foreach (TileActor other in others)
            {
                if (TouchesOnXY(tile, other) || CoversTile(tile, other) || CoversTile(other, tile))
                    return true;
            }

            return false;
        }

        static bool TouchesOnXY(TileActor a, TileActor b)
        {
            if (a == null || b == null || a == b)
                return false;
            if (!TryGetTileBounds(a, out Bounds ba) || !TryGetTileBounds(b, out Bounds bb))
                return false;

            float padX = Mathf.Max(0.02f, Mathf.Min(ba.size.x, bb.size.x) * 0.08f);
            float padY = Mathf.Max(0.02f, Mathf.Min(ba.size.y, bb.size.y) * 0.08f);
            return ba.min.x - padX <= bb.max.x && ba.max.x + padX >= bb.min.x
                && ba.min.y - padY <= bb.max.y && ba.max.y + padY >= bb.min.y;
        }

        static List<TileActor> CollectBuriedOfColor(int colorId, List<TileActor> actTiles)
        {
            var buried = new List<TileActor>();
            if (actTiles == null)
                return buried;

            for (int i = 0; i < actTiles.Count; i++)
            {
                TileActor tile = actTiles[i];
                if (tile == null || tile.IsSubmitted || tile.TileModel == null)
                    continue;
                if (tile.MatchTypeId != colorId)
                    continue;
                if (CanPlayerClick(tile))
                    continue;
                if (tile.Effect != null && !tile.Effect.IsClickAllowed())
                    continue;
                buried.Add(tile);
            }

            return buried;
        }

        static int CountSignificantCovers(TileActor buried, List<TileActor> actTiles)
        {
            return CollectConcealingTiles(buried, actTiles).Count;
        }

        static void CollectMustRemove(
            TileActor buried,
            int colorId,
            List<TileActor> actTiles,
            HashSet<TileActor> mustRemove)
        {
            List<TileActor> concealing = CollectConcealingTiles(buried, actTiles);
            for (int i = 0; i < concealing.Count; i++)
            {
                TileActor cover = concealing[i];
                if (cover.TileModel != null
                    && cover.MatchTypeId == colorId
                    && CanPlayerClick(cover))
                    continue;
                if (!mustRemove.Add(cover))
                    continue;
                CollectMustRemove(cover, colorId, actTiles, mustRemove);
            }
        }

        static List<TileActor> CollectConcealingTiles(TileActor buried, List<TileActor> actTiles)
        {
            var covers = new List<TileActor>();
            if (buried == null || actTiles == null || buried.LayerID <= 0)
                return covers;

            for (int i = 0; i < actTiles.Count; i++)
            {
                TileActor upper = actTiles[i];
                if (CoversTile(buried, upper))
                    covers.Add(upper);
            }

            return covers;
        }

        static bool CoversTile(TileActor buried, TileActor upper)
        {
            return TileStackUtil.IsCurrentUpperBlocker(buried, upper);
        }

        static float OcclusionRatio(TileActor buried, List<TileActor> actTiles)
        {
            if (!TryGetTileBounds(buried, out Bounds a))
                return 1f;

            List<TileActor> covers = CollectConcealingTiles(buried, actTiles);
            if (covers.Count == 0)
                return 0f;

            int covered = 0;
            int total = OcclusionSampleAxis * OcclusionSampleAxis;
            for (int ix = 0; ix < OcclusionSampleAxis; ix++)
            {
                float x = Mathf.Lerp(a.min.x, a.max.x, (ix + 0.5f) / OcclusionSampleAxis);
                for (int iy = 0; iy < OcclusionSampleAxis; iy++)
                {
                    float y = Mathf.Lerp(a.min.y, a.max.y, (iy + 0.5f) / OcclusionSampleAxis);
                    for (int c = 0; c < covers.Count; c++)
                    {
                        if (!TryGetTileBounds(covers[c], out Bounds b))
                            continue;
                        if (x >= b.min.x && x <= b.max.x && y >= b.min.y && y <= b.max.y)
                        {
                            covered++;
                            break;
                        }
                    }
                }
            }

            return (float)covered / total;
        }

        public static TileActor FindClickableOfColor(List<TileActor> clickable, int colorId)
        {
            var pool = new List<TileActor>();
            for (int i = 0; i < clickable.Count; i++)
            {
                if (clickable[i].TileModel != null && clickable[i].MatchTypeId == colorId)
                    pool.Add(clickable[i]);
            }

            if (pool.Count == 0)
                return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        public static TileActor FindCoverToUncoverColor(int colorId, List<TileActor> clickable, List<TileActor> actTiles)
        {
            if (clickable == null || actTiles == null)
                return null;

            TileActor bestCover = null;
            int bestExtra = UncoverImpossible;
            int bestDirect = UncoverImpossible;
            for (int i = 0; i < actTiles.Count; i++)
            {
                TileActor buried = actTiles[i];
                if (buried == null || buried.IsSubmitted || buried.TileModel == null)
                    continue;
                if (buried.MatchTypeId != colorId)
                    continue;
                if (CanPlayerClick(buried))
                    continue;
                if (buried.Effect != null && !buried.Effect.IsClickAllowed())
                    continue;

                TileActor clickableCover = FindClickableCover(buried, clickable, actTiles);
                if (clickableCover == null)
                    continue;

                int extra = ExtraUncoverClicks(buried, colorId, actTiles);
                int direct = CountSignificantCovers(buried, actTiles);
                if (extra < bestExtra || (extra == bestExtra && direct < bestDirect))
                {
                    bestExtra = extra;
                    bestDirect = direct;
                    bestCover = clickableCover;
                }
            }

            return bestCover;
        }

        static int ExtraUncoverClicks(TileActor buried, int colorId, List<TileActor> actTiles)
        {
            var mustRemove = new HashSet<TileActor>();
            CollectMustRemove(buried, colorId, actTiles, mustRemove);
            return mustRemove.Count;
        }

        static TileActor FindClickableCover(
            TileActor buried,
            List<TileActor> clickable,
            List<TileActor> actTiles)
        {
            return FindClickableCover(buried, clickable, actTiles, new HashSet<TileActor>());
        }

        static TileActor FindClickableCover(
            TileActor buried,
            List<TileActor> clickable,
            List<TileActor> actTiles,
            HashSet<TileActor> visiting)
        {
            if (buried == null || visiting == null || !visiting.Add(buried))
                return null;

            TileActor best = PickClickableAmong(CollectConcealingTiles(buried, actTiles), clickable, buried, out float bestOverlap);
            if (best != null)
                return best;

            var mustRemove = new HashSet<TileActor>();
            int colorId = buried.TileModel != null ? buried.MatchTypeId : -1;
            CollectMustRemove(buried, colorId, actTiles, mustRemove);
            best = PickClickableAmong(mustRemove, clickable, buried, out bestOverlap);
            if (best != null)
                return best;

            List<TileActor> concealing = CollectConcealingTiles(buried, actTiles);
            for (int i = 0; i < concealing.Count; i++)
            {
                TileActor nested = FindClickableCover(concealing[i], clickable, actTiles, visiting);
                if (nested == null)
                    continue;
                float overlap = OverlapAreaRatio(buried, nested);
                if (best == null || overlap > bestOverlap)
                {
                    bestOverlap = overlap;
                    best = nested;
                }
            }

            return best;
        }

        static TileActor PickClickableAmong(
            IEnumerable<TileActor> candidates,
            List<TileActor> clickable,
            TileActor buried,
            out float bestOverlap)
        {
            bestOverlap = -1f;
            TileActor best = null;
            if (candidates == null || clickable == null)
                return null;

            foreach (TileActor cover in candidates)
            {
                if (cover == null || !clickable.Contains(cover))
                    continue;
                float overlap = OverlapAreaRatio(buried, cover);
                if (best == null || overlap > bestOverlap)
                {
                    bestOverlap = overlap;
                    best = cover;
                }
            }

            return best;
        }

        public static TileActor PickRandomClickable(
            List<TileActor> clickable,
            Dictionary<int, int> dockCounts,
            int slotsAvailable)
        {
            var pool = new List<TileActor>();
            for (int i = 0; i < clickable.Count; i++)
            {
                TileActor tile = clickable[i];
                dockCounts.TryGetValue(tile.MatchTypeId, out int inDock);
                int need = MatchSize - inDock;
                if (need <= 1 || (need == 2 && slotsAvailable >= 2) || slotsAvailable >= MatchSize)
                    pool.Add(tile);
            }

            if (pool.Count == 0)
                pool = clickable;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        public static Dictionary<int, int> CollectDockColorCounts()
        {
            var dockCounts = new Dictionary<int, int>();
            DockActor dock = LevelDirector.Dock;
            if (dock == null)
                return dockCounts;

            List<TileActor> occupied = dock.GetOccupiedTileIndices();
            for (int i = 0; i < occupied.Count; i++)
            {
                TileActor tb = occupied[i];
                if (tb == null || tb.TileModel == null)
                    continue;
                int id = tb.MatchTypeId;
                dockCounts.TryGetValue(id, out int count);
                dockCounts[id] = count + 1;
            }

            return dockCounts;
        }

        public static Dictionary<int, int> CountByColor(List<TileActor> tiles)
        {
            var counts = new Dictionary<int, int>();
            if (tiles == null)
                return counts;

            for (int i = 0; i < tiles.Count; i++)
            {
                TileActor tile = tiles[i];
                if (tile == null || tile.TileModel == null)
                    continue;
                int id = tile.MatchTypeId;
                counts.TryGetValue(id, out int count);
                counts[id] = count + 1;
            }

            return counts;
        }

        public static Dictionary<int, int> CountBoardRemaining(List<TileActor> actTiles)
        {
            var counts = new Dictionary<int, int>();
            if (actTiles == null)
                return counts;

            for (int i = 0; i < actTiles.Count; i++)
            {
                TileActor tile = actTiles[i];
                if (tile == null || tile.IsSubmitted || tile.TileModel == null)
                    continue;
                if (!tile.gameObject.activeInHierarchy)
                    continue;
                int id = tile.MatchTypeId;
                counts.TryGetValue(id, out int count);
                counts[id] = count + 1;
            }

            return counts;
        }

        static float OverlapAreaRatio(TileActor lower, TileActor upper)
        {
            if (!TryGetTileBounds(lower, out Bounds a) || !TryGetTileBounds(upper, out Bounds b))
                return 0f;

            float overlapX = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            float overlapY = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
            if (overlapX <= 0f || overlapY <= 0f)
                return 0f;

            float area = a.size.x * a.size.y;
            if (area <= 0.0001f)
                return 0f;

            return (overlapX * overlapY) / area;
        }

        static bool TryGetTileBounds(TileActor tile, out Bounds bounds)
        {
            bounds = default;
            if (tile == null)
                return false;

            BoxCollider collider = tile.BoxCollider != null ? tile.BoxCollider : tile.GetComponent<BoxCollider>();
            if (collider == null)
                return false;

            bounds = collider.bounds;
            return true;
        }

        public static List<TileActor> CollectClickableTiles()
        {
            List<TileActor> result = new List<TileActor>();
            List<TileActor> source = LevelDirector.GetClickableTiles();
            for (int i = 0; i < source.Count; i++)
            {
                if (CanPlayerClick(source[i]))
                    result.Add(source[i]);
            }

            return result;
        }

        public static bool CanPlayerClick(TileActor tile)
        {
            return PlayBotClickCost.CanPlayerClick(tile);
        }
    }

    public static class PlayBotStepLog
    {
        public static bool Console = true;
        public static bool File = true;
        static string _path;

        public static void BeginSession(string style)
        {
            if (!File && !Console)
                return;

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LevelPreRun"));
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, $"playbot_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            string line = $"[PlayBotStep] --- session start style={style} file={_path} ---";
            Write(line);
        }

        public static void LevelStart(int level, string style)
        {
            Write($"[PlayBotStep] --- Lv{level} START {style} ---");
        }

        public static void Emit(int level, int click, string style, PlayBotPlanner.StepInfo s)
        {
            if (string.IsNullOrEmpty(s.Action))
                return;

            string cover = s.IsCover ? "(cover)" : "";
            string line =
                $"[PlayBotStep] Lv{level} #{click} {style} {s.Action} {s.Why} " +
                $"target={s.TargetColor} click={s.ClickColor}{cover} " +
                $"n={s.Need} e={s.Extra} slots={s.Slots} vis={s.BoardVisible} tot={s.BoardTotal} dock={s.InDock} " +
                $"| best={s.BestColor} n{s.BestNeed} | plans={s.PlanCount}/{s.NoticedCount} " +
                $"| dock{{{s.DockHist}}} board{{{s.BoardHist}}}";
            Write(line);
        }

        public static void Note(string message)
        {
            Write($"[PlayBotStep] {message}");
        }

        static void Write(string line)
        {
            if (Console)
                Debug.Log(line);
            if (!File || string.IsNullOrEmpty(_path))
                return;
            try
            {
                System.IO.File.AppendAllText(_path, line + "\n");
            }
            catch (Exception)
            {
            }
        }
    }
}
#endif
