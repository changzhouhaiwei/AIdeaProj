using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishTilePlay
{
    [Serializable]
    public struct AwardColorEggCoverPlacement
    {
        public int layer;
        public int anchorX;
        public int anchorY;
        public int color;

        public AwardColorEggCoverPlacement(int layer, int anchorX, int anchorY, int color)
        {
            this.layer = layer;
            this.anchorX = anchorX;
            this.anchorY = anchorY;
            this.color = color;
        }

        public Vector2Int Anchor => new(anchorX, anchorY);
        public string RuntimeId => layer + ":" + anchorX + ":" + anchorY;
    }

    public static class AwardColorEggCoverResolver
    {
        public static readonly Vector2Int CellSize = new(2, 3);

        struct Candidate
        {
            public int layer;
            public int x;
            public int y;
            public float minX;
            public float minY;
            public float maxX;
            public float maxY;
        }

        public static List<AwardColorEggCoverPlacement> Resolve(LevelModel level, int seed)
        {
            return Pick(level, Collect(level), seed, null);
        }

        public static List<AwardColorEggCoverPlacement> RerollOnLayers(
            LevelModel level,
            int seed,
            IReadOnlyList<AwardColorEggCoverPlacement> previous)
        {
            int[] layers = null;
            if (previous != null && previous.Count > 0)
            {
                layers = new int[previous.Count];
                for (int i = 0; i < previous.Count; i++)
                    layers[i] = previous[i].layer;
            }

            List<AwardColorEggCoverPlacement> picked = Pick(level, Collect(level), seed, layers);
            return picked.Count > 0 ? picked : Resolve(level, seed);
        }

        static List<Candidate> Collect(LevelModel level)
        {
            List<Candidate> result = new();
            if (level == null || level.AmountOfLayers < 2)
                return result;
            if (!TryGetBoardBounds(level, out float boardMinX, out float boardMinY, out float boardMaxX, out float boardMaxY))
                return result;

            for (int layerIndex = 1; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                int footprintLayer = layerIndex - 1;
                Layer cover = level.GetLayer(footprintLayer);
                if (cover == null)
                    continue;

                for (int y = 0; y <= cover.AmountOfRows - CellSize.y; y++)
                {
                    LayerRow row = cover.GetRow(y);
                    if (row == null)
                        continue;

                    for (int x = 0; x <= row.AmountOfCells - CellSize.x; x++)
                    {
                        if (CountFilled(cover, x, y) <= 0)
                            continue;
                        if (FootprintHitsOverlapDesign(level, x, y))
                            continue;
                        if (!TryGetFootprint(footprintLayer, x, y, out float minX, out float minY, out float maxX, out float maxY))
                            continue;
                        if (!InsideBoard(minX, minY, maxX, maxY, boardMinX, boardMinY, boardMaxX, boardMaxY))
                            continue;
                        if (CountSupport(level, layerIndex, minX, minY, maxX, maxY) < 2)
                            continue;

                        result.Add(new Candidate
                        {
                            layer = layerIndex,
                            x = x,
                            y = y,
                            minX = minX,
                            minY = minY,
                            maxX = maxX,
                            maxY = maxY,
                        });
                    }
                }
            }

            return result;
        }

        static List<AwardColorEggCoverPlacement> Pick(
            LevelModel level,
            List<Candidate> candidates,
            int seed,
            int[] requiredLayers)
        {
            List<AwardColorEggCoverPlacement> result = new();
            if (level == null || candidates == null || candidates.Count == 0)
                return result;

            Shuffle(candidates, new System.Random(seed));
            List<Candidate> chosen = new();
            float midX = BoardMidX(level);
            if (!Search(candidates, 0, chosen, requiredLayers, true, midX)
                && !Search(candidates, 0, chosen, requiredLayers, false, midX))
            {
                if (requiredLayers != null)
                    return Pick(level, candidates, seed + 17, null);
                return result;
            }

            for (int i = 0; i < chosen.Count; i++)
            {
                Candidate candidate = chosen[i];
                result.Add(new AwardColorEggCoverPlacement(
                    candidate.layer,
                    candidate.x,
                    candidate.y,
                    i % AwardColorEggCoverConfig.EggCount));
            }

            return result;
        }

        static bool Search(
            List<Candidate> candidates,
            int start,
            List<Candidate> chosen,
            int[] requiredLayers,
            bool requireBothSides,
            float midX)
        {
            if (chosen.Count == AwardColorEggCoverConfig.EggCount)
                return !requireBothSides || !AllOnSameSide(chosen, midX);

            for (int i = start; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                if (OverlapsChosen(chosen, candidate))
                    continue;
                if (!LayerAllowed(chosen, candidate.layer, requiredLayers))
                    continue;

                chosen.Add(candidate);
                if (Search(candidates, i + 1, chosen, requiredLayers, requireBothSides, midX))
                    return true;
                chosen.RemoveAt(chosen.Count - 1);
            }

            return false;
        }

        static bool LayerAllowed(List<Candidate> chosen, int layer, int[] requiredLayers)
        {
            if (requiredLayers == null)
                return true;

            int needed = 0;
            for (int i = 0; i < requiredLayers.Length; i++)
            {
                if (requiredLayers[i] == layer)
                    needed++;
            }

            if (needed == 0)
                return false;

            int used = 0;
            for (int i = 0; i < chosen.Count; i++)
            {
                if (chosen[i].layer == layer)
                    used++;
            }

            return used < needed;
        }

        static bool OverlapsChosen(List<Candidate> chosen, Candidate candidate)
        {
            const float gap = 0.02f;
            for (int i = 0; i < chosen.Count; i++)
            {
                Candidate other = chosen[i];
                bool separated = candidate.maxX <= other.minX + gap
                    || other.maxX <= candidate.minX + gap
                    || candidate.maxY <= other.minY + gap
                    || other.maxY <= candidate.minY + gap;
                if (!separated)
                    return true;
            }

            return false;
        }

        static bool TryGetBoardBounds(
            LevelModel level,
            out float minX,
            out float minY,
            out float maxX,
            out float maxY)
        {
            minX = float.MaxValue;
            minY = float.MaxValue;
            maxX = float.MinValue;
            maxY = float.MinValue;
            bool any = false;
            Vector2 tileSize = LevelFitter.TileSize;
            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                    continue;

                for (int y = 0; y < layer.AmountOfRows; y++)
                {
                    LayerRow row = layer.GetRow(y);
                    if (row == null)
                        continue;

                    for (int x = 0; x < row.AmountOfCells; x++)
                    {
                        CellModel cell = row.GetCell(x);
                        if (cell == null || !cell.IsFilled)
                            continue;

                        Vector3 center = LevelFitter.GetPosition(new ElementPosition(x, y, layerIndex));
                        float left = center.x - tileSize.x * 0.5f;
                        float right = center.x + tileSize.x * 0.5f;
                        float bottom = center.y - tileSize.y * 0.5f;
                        float top = center.y + tileSize.y * 0.5f;
                        if (left < minX)
                            minX = left;
                        if (right > maxX)
                            maxX = right;
                        if (bottom < minY)
                            minY = bottom;
                        if (top > maxY)
                            maxY = top;
                        any = true;
                    }
                }
            }

            return any && minX < maxX && minY < maxY;
        }

        static bool TryGetFootprint(
            int layer,
            int anchorX,
            int anchorY,
            out float minX,
            out float minY,
            out float maxX,
            out float maxY)
        {
            minX = float.MaxValue;
            minY = float.MaxValue;
            maxX = float.MinValue;
            maxY = float.MinValue;
            Vector2 tileSize = LevelFitter.TileSize;
            for (int y = 0; y < CellSize.y; y++)
            {
                for (int x = 0; x < CellSize.x; x++)
                {
                    Vector3 center = LevelFitter.GetPosition(new ElementPosition(anchorX + x, anchorY + y, layer));
                    float left = center.x - tileSize.x * 0.5f;
                    float right = center.x + tileSize.x * 0.5f;
                    float bottom = center.y - tileSize.y * 0.5f;
                    float top = center.y + tileSize.y * 0.5f;
                    if (left < minX)
                        minX = left;
                    if (right > maxX)
                        maxX = right;
                    if (bottom < minY)
                        minY = bottom;
                    if (top > maxY)
                        maxY = top;
                }
            }

            return minX < maxX && minY < maxY;
        }

        static bool InsideBoard(
            float minX,
            float minY,
            float maxX,
            float maxY,
            float boardMinX,
            float boardMinY,
            float boardMaxX,
            float boardMaxY)
        {
            float slack = Mathf.Min(LevelFitter.TileSize.x, LevelFitter.TileSize.y) * 0.02f;
            return minX >= boardMinX - slack
                && minY >= boardMinY - slack
                && maxX <= boardMaxX + slack
                && maxY <= boardMaxY + slack;
        }

        // 与运行时 CountUpperBlockers 一致：数上层（LayerId 更小）且与蛋 footprint AABB 重叠的棋。
        // 用重叠而不是棋心，避免只被边缘压住的棋在生成时漏算、运行时却仍挡住。
        static int CountSupport(
            LevelModel level,
            int eggLayer,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            // 碰撞盒是 footprint 的 0.95，生成侧同步缩小，两边门槛一致。
            float width = maxX - minX;
            float height = maxY - minY;
            float insetX = width * 0.025f;
            float insetY = height * 0.025f;
            float eggMinX = minX + insetX;
            float eggMaxX = maxX - insetX;
            float eggMinY = minY + insetY;
            float eggMaxY = maxY - insetY;

            Vector2 tileSize = LevelFitter.TileSize;
            float halfW = tileSize.x * 0.5f;
            float halfH = tileSize.y * 0.5f;
            int count = 0;
            for (int layerIndex = 0; layerIndex < eggLayer; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                    continue;

                for (int y = 0; y < layer.AmountOfRows; y++)
                {
                    LayerRow row = layer.GetRow(y);
                    if (row == null)
                        continue;

                    for (int x = 0; x < row.AmountOfCells; x++)
                    {
                        CellModel cell = row.GetCell(x);
                        if (cell == null || !cell.IsFilled)
                            continue;

                        Vector3 center = LevelFitter.GetPosition(new ElementPosition(x, y, layerIndex));
                        float tileMinX = center.x - halfW;
                        float tileMaxX = center.x + halfW;
                        float tileMinY = center.y - halfH;
                        float tileMaxY = center.y + halfH;
                        if (tileMaxX > eggMinX
                            && tileMinX < eggMaxX
                            && tileMaxY > eggMinY
                            && tileMinY < eggMaxY)
                            count++;
                    }
                }
            }

            return count;
        }

        static bool AllOnSameSide(List<Candidate> chosen, float midX)
        {
            int left = 0;
            int right = 0;
            for (int i = 0; i < chosen.Count; i++)
            {
                float center = chosen[i].x + CellSize.x * 0.5f;
                if (center < midX)
                    left++;
                else
                    right++;
            }

            return left == 0 || right == 0;
        }

        static float BoardMidX(LevelModel level)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                    continue;

                for (int y = 0; y < layer.AmountOfRows; y++)
                {
                    LayerRow row = layer.GetRow(y);
                    if (row == null)
                        continue;

                    for (int x = 0; x < row.AmountOfCells; x++)
                    {
                        CellModel cell = row.GetCell(x);
                        if (cell == null || !cell.IsFilled)
                            continue;
                        if (x < min)
                            min = x;
                        if (x > max)
                            max = x;
                    }
                }
            }

            if (min == int.MaxValue)
                return 0f;
            return (min + max + 1) * 0.5f;
        }

        static int CountFilled(Layer layer, int x, int y)
        {
            int filled = 0;
            for (int offsetY = 0; offsetY < CellSize.y; offsetY++)
            {
                for (int offsetX = 0; offsetX < CellSize.x; offsetX++)
                {
                    if (IsFilled(layer, x + offsetX, y + offsetY))
                        filled++;
                }
            }

            return filled;
        }

        static bool FootprintHitsOverlapDesign(LevelModel level, int x, int y)
        {
            for (int offsetY = 0; offsetY < CellSize.y; offsetY++)
            {
                for (int offsetX = 0; offsetX < CellSize.x; offsetX++)
                {
                    if (IsOverlapDesign(level, x + offsetX, y + offsetY))
                        return true;
                }
            }

            return false;
        }

        static bool IsOverlapDesign(LevelModel level, int x, int y)
        {
            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                CellModel cell = GetCell(layer, x, y);
                if (cell == null || !cell.IsFilled)
                    continue;
                if (Mathf.Abs(cell.DeltaX) > 0.001f || Mathf.Abs(cell.DeltaY) > 0.001f)
                    return true;
            }

            return false;
        }

        static bool IsFilled(Layer layer, int x, int y)
        {
            CellModel cell = GetCell(layer, x, y);
            return cell != null && cell.IsFilled;
        }

        static CellModel GetCell(Layer layer, int x, int y)
        {
            if (layer == null || y < 0 || y >= layer.AmountOfRows)
                return null;
            LayerRow row = layer.GetRow(y);
            if (row == null || x < 0 || x >= row.AmountOfCells)
                return null;
            return row.GetCell(x);
        }

        static void Shuffle(List<Candidate> candidates, System.Random random)
        {
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                Candidate temp = candidates[i];
                candidates[i] = candidates[swap];
                candidates[swap] = temp;
            }
        }
    }
}
