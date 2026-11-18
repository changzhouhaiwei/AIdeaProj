using System.Collections.Generic;
using UnityEngine;

namespace FishTilePlay
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
            List<TileActor> actTiles,
            out int extra)
        {
            return TryGetUncover(colorId, shortage, actTiles, out extra, out _, out _, out _, null);
        }

        public static bool TryGetUncover(
            int colorId,
            int shortage,
            List<TileActor> actTiles,
            out int extra,
            out int raw,
            out float occlusion,
            out List<TileActor> chosenBuried)
        {
            return TryGetUncover(colorId, shortage, actTiles, out extra, out raw, out occlusion, out chosenBuried, null);
        }

        /// <param name="buriedPool">
        /// 非空则只在这批埋棋里凑 shortage（裂变棋：仅可转化同色）。
        /// 空则按 PlayBot：场上该色全部未可点棋。
        /// </param>
        public static bool TryGetUncover(
            int colorId,
            int shortage,
            List<TileActor> actTiles,
            out int extra,
            out int raw,
            out float occlusion,
            out List<TileActor> chosenBuried,
            List<TileActor> buriedPool)
        {
            extra = UncoverImpossible;
            raw = UncoverImpossible;
            occlusion = 1f;
            chosenBuried = new List<TileActor>();
            if (shortage <= 0)
            {
                extra = 0;
                raw = 0;
                occlusion = 0f;
                return true;
            }

            List<TileActor> buried = buriedPool != null
                ? CollectBuriedFromPool(colorId, buriedPool)
                : CollectBuriedOfColor(colorId, actTiles);
            if (buried.Count < shortage)
                return false;

            var removes = new List<HashSet<TileActor>>(buried.Count);
            var raws = new int[buried.Count];
            var occs = new float[buried.Count];
            for (int i = 0; i < buried.Count; i++)
            {
                var mustRemove = new HashSet<TileActor>();
                CollectMustRemove(buried[i], colorId, actTiles, mustRemove);
                removes.Add(mustRemove);
                raws[i] = CountSignificantCovers(buried[i], actTiles);
                occs[i] = OcclusionRatio(buried[i], actTiles);
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

        public static int ExtraUncoverClicks(TileActor buried, int colorId, List<TileActor> actTiles)
        {
            var mustRemove = new HashSet<TileActor>();
            CollectMustRemove(buried, colorId, actTiles, mustRemove);
            return mustRemove.Count;
        }

        /// <summary>
        /// 用当前棋盘快照计算目标组的真实点击成本：
        /// 目标自身点击数 + 为使目标可点而必须先点掉的棋（递归、去重）。
        /// 目标之间互相遮挡时，目标自身只在前半项计一次，不重复进入 Extra。
        /// </summary>
        public static bool TryGetCurrentClickCost(
            IList<TileActor> targets,
            List<TileActor> actTiles,
            out int clickCost,
            out int extra)
        {
            clickCost = UncoverImpossible;
            extra = UncoverImpossible;
            if (targets == null || targets.Count == 0 || actTiles == null)
                return false;

            var targetSet = new HashSet<TileActor>();
            var blockers = new HashSet<TileActor>();
            for (int i = 0; i < targets.Count; i++)
            {
                TileActor tile = targets[i];
                if (tile == null
                    || tile.IsSubmitted
                    || tile.TileModel == null
                    || !tile.gameObject.activeInHierarchy
                    || !actTiles.Contains(tile)
                    || !targetSet.Add(tile))
                    return false;

                // colorId = -1：这里算当前目标组的真实成本，不能像 PlayBot 计划那样
                // 把同色可点盖免费跳过；未选中的同色盖也需要一次真实点击。
                CollectMustRemove(tile, -1, actTiles, blockers);
            }

            blockers.ExceptWith(targetSet);
            extra = blockers.Count;
            clickCost = targetSet.Count + extra;
            return true;
        }

        public static int ExtraForTargets(IList<TileActor> targets, List<TileActor> actTiles)
        {
            return TryGetCurrentClickCost(targets, actTiles, out _, out int extra)
                ? extra
                : UncoverImpossible;
        }

        /// <summary>
        /// 为一批目标建立当前遮挡闭包缓存。遮挡关系按 LayerID 严格向上，是 DAG；
        /// 同一条深层链只递归一次，供大量三枚组合复用。
        /// </summary>
        public static Dictionary<TileActor, HashSet<TileActor>> BuildCurrentBlockerCache(
            IList<TileActor> targets,
            List<TileActor> actTiles)
        {
            var memo = new Dictionary<TileActor, HashSet<TileActor>>();
            if (targets == null || actTiles == null)
                return memo;

            for (int i = 0; i < targets.Count; i++)
            {
                TileActor tile = targets[i];
                if (tile != null)
                    GetCurrentBlockersMemoized(tile, actTiles, memo);
            }

            return memo;
        }

        static HashSet<TileActor> GetCurrentBlockersMemoized(
            TileActor tile,
            List<TileActor> actTiles,
            Dictionary<TileActor, HashSet<TileActor>> memo)
        {
            if (memo.TryGetValue(tile, out HashSet<TileActor> cached))
                return cached;

            var blockers = new HashSet<TileActor>();
            memo[tile] = blockers;

            List<TileActor> directCovers = CollectConcealingTiles(tile, actTiles);
            for (int i = 0; i < directCovers.Count; i++)
            {
                TileActor cover = directCovers[i];
                blockers.Add(cover);
                blockers.UnionWith(GetCurrentBlockersMemoized(cover, actTiles, memo));
            }

            return blockers;
        }

        public static bool CanPlayerClick(TileActor tile)
        {
            if (tile == null || tile.IsSubmitted || !tile.IsClickable)
                return false;
            if (tile.TileModel == null)
                return false;
            if (!tile.gameObject.activeInHierarchy)
                return false;

            LevelGenService gen = LevelDirector.LevelGenService;
            if (gen != null && !gen.IsTileUnconcealed(tile))
                return false;
            if (tile.Effect != null && !tile.Effect.IsClickAllowed())
                return false;

            return true;
        }

        public static void CollectMustRemove(
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

        public static List<TileActor> CollectConcealingTiles(TileActor buried, List<TileActor> actTiles)
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

        public static int CountSignificantCovers(TileActor buried, List<TileActor> actTiles)
        {
            return CollectConcealingTiles(buried, actTiles).Count;
        }

        public static bool TryGetTileBounds(TileActor tile, out Bounds bounds)
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

        static List<TileActor> CollectBuriedOfColor(int colorId, List<TileActor> actTiles)
        {
            return CollectBuriedFromPool(colorId, actTiles);
        }

        static List<TileActor> CollectBuriedFromPool(int colorId, List<TileActor> pool)
        {
            var buried = new List<TileActor>();
            if (pool == null)
                return buried;

            for (int i = 0; i < pool.Count; i++)
            {
                TileActor tile = pool[i];
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

        static bool TryPickBuriedSetGreedy(
            List<TileActor> buried,
            List<HashSet<TileActor>> removes,
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
            List<TileActor> buried,
            List<HashSet<TileActor>> removes,
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
            List<TileActor> buried,
            List<HashSet<TileActor>> removes,
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
    }
}
