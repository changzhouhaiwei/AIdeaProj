using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// PlayBot 揭盖代价：Extra = 为凑够 shortage 颗埋棋必须先点掉的盖（递归、去重）。
    /// 运行时与编辑器 PlayBot 共用，避免裂变棋 ClickCost 只数一层可点盖。
    /// </summary>
    public static class PlayBotClickCost
    {
        public const int UncoverImpossible = 999;
        const int ComboSearchCap = 14;
        const int OcclusionSampleAxis = 8;

        public static bool TryGetUncover(
            int colorId,
            int shortage,
            List<XSortActor> actXSorts,
            out int extra)
        {
            return TryGetUncover(colorId, shortage, actXSorts, out extra, out _, out _, out _, null);
        }

        public static bool TryGetUncover(
            int colorId,
            int shortage,
            List<XSortActor> actXSorts,
            out int extra,
            out int raw,
            out float occlusion,
            out List<XSortActor> chosenBuried)
        {
            return TryGetUncover(colorId, shortage, actXSorts, out extra, out raw, out occlusion, out chosenBuried, null);
        }

        /// <param name="buriedPool">
        /// 非空则只在这批埋棋里凑 shortage（裂变棋：仅可转化同色）。
        /// 空则按 PlayBot：场上该色全部未可点棋。
        /// </param>
        public static bool TryGetUncover(
            int colorId,
            int shortage,
            List<XSortActor> actXSorts,
            out int extra,
            out int raw,
            out float occlusion,
            out List<XSortActor> chosenBuried,
            List<XSortActor> buriedPool)
        {
            extra = UncoverImpossible;
            raw = UncoverImpossible;
            occlusion = 1f;
            chosenBuried = new List<XSortActor>();
            if (shortage <= 0)
            {
                extra = 0;
                raw = 0;
                occlusion = 0f;
                return true;
            }

            List<XSortActor> buried = buriedPool != null
                ? CollectBuriedFromPool(colorId, buriedPool)
                : CollectBuriedOfColor(colorId, actXSorts);
            if (buried.Count < shortage)
                return false;

            var removes = new List<HashSet<XSortActor>>(buried.Count);
            var raws = new int[buried.Count];
            var occs = new float[buried.Count];
            for (int i = 0; i < buried.Count; i++)
            {
                var mustRemove = new HashSet<XSortActor>();
                CollectMustRemove(buried[i], colorId, actXSorts, mustRemove);
                removes.Add(mustRemove);
                raws[i] = CountSignificantCovers(buried[i], actXSorts);
                occs[i] = OcclusionRatio(buried[i], actXSorts);
            }

            int[] pick;
            bool ok = shortage == 1 || buried.Count > ComboSearchCap
                ? TryPickBuriedSetGreedy(buried, removes, raws, occs, shortage, out extra, out raw, out occlusion, out pick)
                : TryPickBuriedSetExact(buried, removes, raws, occs, shortage, out extra, out raw, out occlusion, out pick);
            if (!ok || pick == null)
                return false;

            for (int n = 0; n < pick.Length; n++)
                chosenBuried.Add(buried[pick[n]]);
            return true;
        }

        public static int ExtraUncoverClicks(XSortActor buried, int colorId, List<XSortActor> actXSorts)
        {
            var mustRemove = new HashSet<XSortActor>();
            CollectMustRemove(buried, colorId, actXSorts, mustRemove);
            return mustRemove.Count;
        }

        /// <summary>
        /// 用当前棋盘快照计算目标组的真实点击成本：
        /// 目标自身点击数 + 为使目标可点而必须先点掉的棋（递归、去重）。
        /// 目标之间互相遮挡时，目标自身只在前半项计一次，不重复进入 Extra。
        /// </summary>
        public static bool TryGetCurrentClickCost(
            IList<XSortActor> targets,
            List<XSortActor> actXSorts,
            out int clickCost,
            out int extra)
        {
            clickCost = UncoverImpossible;
            extra = UncoverImpossible;
            if (targets == null || targets.Count == 0 || actXSorts == null)
                return false;

            var targetSet = new HashSet<XSortActor>();
            var blockers = new HashSet<XSortActor>();
            for (int i = 0; i < targets.Count; i++)
            {
                XSortActor XSort = targets[i];
                if (XSort == null
                    || XSort.IsSubmitted
                    || XSort.XSortModel == null
                    || !XSort.gameObject.activeInHierarchy
                    || !actXSorts.Contains(XSort)
                    || !targetSet.Add(XSort))
                    return false;

                // colorId = -1：这里算当前目标组的真实成本，不能像 PlayBot 计划那样
                // 把同色可点盖免费跳过；未选中的同色盖也需要一次真实点击。
                CollectMustRemove(XSort, -1, actXSorts, blockers);
            }

            blockers.ExceptWith(targetSet);
            extra = blockers.Count;
            clickCost = targetSet.Count + extra;
            return true;
        }

        public static int ExtraForTargets(IList<XSortActor> targets, List<XSortActor> actXSorts)
        {
            return TryGetCurrentClickCost(targets, actXSorts, out _, out int extra)
                ? extra
                : UncoverImpossible;
        }

        /// <summary>
        /// 为一批目标建立当前遮挡闭包缓存。遮挡关系按 LayerID 严格向上，是 DAG；
        /// 同一条深层链只递归一次，供大量三枚组合复用。
        /// </summary>
        public static Dictionary<XSortActor, HashSet<XSortActor>> BuildCurrentBlockerCache(
            IList<XSortActor> targets,
            List<XSortActor> actXSorts)
        {
            var memo = new Dictionary<XSortActor, HashSet<XSortActor>>();
            if (targets == null || actXSorts == null)
                return memo;

            for (int i = 0; i < targets.Count; i++)
            {
                XSortActor XSort = targets[i];
                if (XSort != null)
                    GetCurrentBlockersMemoized(XSort, actXSorts, memo);
            }

            return memo;
        }

        static HashSet<XSortActor> GetCurrentBlockersMemoized(
            XSortActor XSort,
            List<XSortActor> actXSorts,
            Dictionary<XSortActor, HashSet<XSortActor>> memo)
        {
            if (memo.TryGetValue(XSort, out HashSet<XSortActor> cached))
                return cached;

            var blockers = new HashSet<XSortActor>();
            memo[XSort] = blockers;

            List<XSortActor> directCovers = CollectConcealingXSorts(XSort, actXSorts);
            for (int i = 0; i < directCovers.Count; i++)
            {
                XSortActor cover = directCovers[i];
                blockers.Add(cover);
                blockers.UnionWith(GetCurrentBlockersMemoized(cover, actXSorts, memo));
            }

            return blockers;
        }

        public static bool CanPlayerClick(XSortActor XSort)
        {
            if (XSort == null || XSort.IsSubmitted || !XSort.IsClickable)
                return false;
            if (XSort.XSortModel == null)
                return false;
            if (!XSort.gameObject.activeInHierarchy)
                return false;

            LevelGenService gen = LevelDirector.LevelGenService;
            if (gen != null && !gen.IsXSortUnconcealed(XSort))
                return false;
            if (XSort.Effect != null && !XSort.Effect.IsClickAllowed())
                return false;

            return true;
        }

        public static void CollectMustRemove(
            XSortActor buried,
            int colorId,
            List<XSortActor> actXSorts,
            HashSet<XSortActor> mustRemove)
        {
            List<XSortActor> concealing = CollectConcealingXSorts(buried, actXSorts);
            for (int i = 0; i < concealing.Count; i++)
            {
                XSortActor cover = concealing[i];
                if (cover.XSortModel != null
                    && cover.MatchTypeId == colorId
                    && CanPlayerClick(cover))
                    continue;
                if (!mustRemove.Add(cover))
                    continue;
                CollectMustRemove(cover, colorId, actXSorts, mustRemove);
            }
        }

        public static List<XSortActor> CollectConcealingXSorts(XSortActor buried, List<XSortActor> actXSorts)
        {
            var covers = new List<XSortActor>();
            if (buried == null || actXSorts == null || buried.LayerID <= 0)
                return covers;

            for (int i = 0; i < actXSorts.Count; i++)
            {
                XSortActor upper = actXSorts[i];
                if (CoversXSort(buried, upper))
                    covers.Add(upper);
            }

            return covers;
        }

        public static int CountSignificantCovers(XSortActor buried, List<XSortActor> actXSorts)
        {
            return CollectConcealingXSorts(buried, actXSorts).Count;
        }

        public static bool TryGetXSortBounds(XSortActor XSort, out Bounds bounds)
        {
            bounds = default;
            if (XSort == null)
                return false;

            BoxCollider collider = XSort.BoxCollider != null ? XSort.BoxCollider : XSort.GetComponent<BoxCollider>();
            if (collider == null)
                return false;

            bounds = collider.bounds;
            return true;
        }

        static List<XSortActor> CollectBuriedOfColor(int colorId, List<XSortActor> actXSorts)
        {
            return CollectBuriedFromPool(colorId, actXSorts);
        }

        static List<XSortActor> CollectBuriedFromPool(int colorId, List<XSortActor> pool)
        {
            var buried = new List<XSortActor>();
            if (pool == null)
                return buried;

            for (int i = 0; i < pool.Count; i++)
            {
                XSortActor XSort = pool[i];
                if (XSort == null || XSort.IsSubmitted || XSort.XSortModel == null)
                    continue;
                if (XSort.MatchTypeId != colorId)
                    continue;
                if (CanPlayerClick(XSort))
                    continue;
                if (XSort.Effect != null && !XSort.Effect.IsClickAllowed())
                    continue;
                buried.Add(XSort);
            }

            return buried;
        }

        static bool TryPickBuriedSetGreedy(
            List<XSortActor> buried,
            List<HashSet<XSortActor>> removes,
            int[] raws,
            float[] occs,
            int shortage,
            out int extra,
            out int raw,
            out float occlusion,
            out int[] pick)
        {
            extra = UncoverImpossible;
            raw = UncoverImpossible;
            occlusion = 1f;
            pick = null;

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
                pick = new[] { order[0] };
                ScoreBuriedSet(buried, removes, raws, occs, pick, out extra, out raw, out occlusion);
                return extra < UncoverImpossible;
            }

            if (order.Count < shortage)
                return false;

            pick = new int[shortage];
            for (int i = 0; i < shortage; i++)
                pick[i] = order[i];
            ScoreBuriedSet(buried, removes, raws, occs, pick, out extra, out raw, out occlusion);
            return extra < UncoverImpossible;
        }

        static bool TryPickBuriedSetExact(
            List<XSortActor> buried,
            List<HashSet<XSortActor>> removes,
            int[] raws,
            float[] occs,
            int shortage,
            out int extra,
            out int raw,
            out float occlusion,
            out int[] pick)
        {
            extra = UncoverImpossible;
            raw = UncoverImpossible;
            occlusion = 1f;
            pick = null;
            var cursor = new int[shortage];
            int[] bestPick = new int[shortage];
            int bestExtra = UncoverImpossible;
            int bestRaw = UncoverImpossible;
            float bestOcc = 1f;
            bool found = false;
            SearchBuriedCombos(
                buried, removes, raws, occs, cursor, bestPick, shortage, 0, 0,
                ref found, ref bestExtra, ref bestRaw, ref bestOcc);
            extra = bestExtra;
            raw = bestRaw;
            occlusion = bestOcc;
            if (!found)
                return false;
            pick = bestPick;
            return true;
        }

        static void SearchBuriedCombos(
            List<XSortActor> buried,
            List<HashSet<XSortActor>> removes,
            int[] raws,
            float[] occs,
            int[] pick,
            int[] bestPick,
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
                    for (int i = 0; i < pick.Length; i++)
                        bestPick[i] = pick[i];
                }

                return;
            }

            int remain = shortage - depth;
            for (int i = start; i <= buried.Count - remain; i++)
            {
                pick[depth] = i;
                SearchBuriedCombos(
                    buried, removes, raws, occs, pick, bestPick, shortage, i + 1, depth + 1,
                    ref found, ref bestExtra, ref bestRaw, ref bestOcc);
            }
        }

        static void ScoreBuriedSet(
            List<XSortActor> buried,
            List<HashSet<XSortActor>> removes,
            int[] raws,
            float[] occs,
            int[] pick,
            out int extra,
            out int raw,
            out float occlusion)
        {
            var union = new HashSet<XSortActor>();
            var chosen = new HashSet<XSortActor>();
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

            foreach (XSortActor XSort in chosen)
                union.Remove(XSort);

            extra = union.Count;
        }

        static bool CoversXSort(XSortActor buried, XSortActor upper)
        {
            return XSortStackUtil.IsCurrentUpperBlocker(buried, upper);
        }

        static float OcclusionRatio(XSortActor buried, List<XSortActor> actXSorts)
        {
            if (!TryGetXSortBounds(buried, out Bounds a))
                return 1f;

            List<XSortActor> covers = CollectConcealingXSorts(buried, actXSorts);
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
                        if (!TryGetXSortBounds(covers[c], out Bounds b))
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
    }
}
