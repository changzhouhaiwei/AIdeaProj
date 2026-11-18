#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// 配置查验表用的玩法校验。坐标与局内一致（偶数层用最底层尺寸，奇数层用倒数第二层尺寸），
    /// 只检查生成器实际可能选出的落点，不检查落选的候选。
    /// </summary>
    public static class LevelSpecialPlayChecks
    {
        public const string EggCoverIssue = "eggsmash遮挡不足";
        public const string BoxSameLayerIssue = "boxcover同层遮挡";
        public const string BoxUpperIssue = "boxcover上方无遮挡";
        public const string BoxAndEggIssue = "boxcover与eggsmash同关";
        public const string ExtraEffectIssue = "非unknown棋子玩法";

        const int MaxBoxSolutions = 100000;
        const float TileSize = 1f;
        static readonly Vector2Int EggCellSize = AwardColorEggCoverResolver.CellSize;

        static HashSet<int> eggSmashLevels = new HashSet<int>();
        static bool eggsLoaded;

        public static void Reload()
        {
            eggSmashLevels = LevelLayerTileCountExporter.LoadEggSmashLevels();
            eggsLoaded = true;
        }

        public struct Finding
        {
            public string Category;
            public string Detail;

            public Finding(string category, string detail)
            {
                Category = category;
                Detail = detail;
            }
        }

        public static void Collect(LevelModel level, int levelNumber, List<Finding> issues)
        {
            if (level == null || issues == null)
            {
                return;
            }

            if (!eggsLoaded)
            {
                Reload();
            }

            bool eggSmash = levelNumber > 0 && eggSmashLevels.Contains(levelNumber);
            bool boxCover = level.HasBoxCovers;
            if (eggSmash && boxCover)
            {
                Add(issues, BoxAndEggIssue, "同一关只能有其中一种玩法");
            }

            if (eggSmash)
            {
                AppendEggIssues(level, issues);
            }

            if (boxCover)
            {
                AppendBoxIssues(level, issues);
            }

            AppendExtraEffectIssues(level, issues);
        }

        /// <summary>
        /// unknown 允许。Crate、Ice、Link 以及其他非 unknown 特效都算不允许的棋子玩法填充。
        /// </summary>
        static void AppendExtraEffectIssues(LevelModel level, List<Finding> issues)
        {
            int crate = 0;
            int ice = 0;
            int link = 0;
            int other = 0;
            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                {
                    continue;
                }

                for (int y = 0; y < layer.AmountOfRows; y++)
                {
                    LayerRow row = layer.GetRow(y);
                    if (row == null)
                    {
                        continue;
                    }

                    for (int x = 0; x < row.AmountOfCells; x++)
                    {
                        CellModel cell = row.GetCell(x);
                        if (cell == null || !cell.IsFilled)
                        {
                            continue;
                        }

                        switch (cell.Effect)
                        {
                            case TileEffectType.None:
                            case TileEffectType.Unknown:
                                break;
                            case TileEffectType.Crate:
                                crate++;
                                break;
                            case TileEffectType.Ice:
                                ice++;
                                break;
                            case TileEffectType.Link:
                                link++;
                                break;
                            default:
                                other++;
                                break;
                        }
                    }
                }
            }

            if (crate == 0 && ice == 0 && link == 0 && other == 0)
            {
                return;
            }

            List<string> parts = new List<string>();
            if (crate > 0)
            {
                parts.Add("Crate " + crate);
            }

            if (ice > 0)
            {
                parts.Add("Ice " + ice);
            }

            if (link > 0)
            {
                parts.Add("Link " + link);
            }

            if (other > 0)
            {
                parts.Add("其他 " + other);
            }

            Add(issues, ExtraEffectIssue, "含 " + string.Join("、", parts));
        }

        struct Bounds2
        {
            public float minX;
            public float minY;
            public float maxX;
            public float maxY;

            public float CenterX => (minX + maxX) * 0.5f;
            public float CenterY => (minY + maxY) * 0.5f;
            public bool Valid => minX < maxX && minY < maxY;

            public void Encapsulate(float left, float bottom, float right, float top)
            {
                if (!Valid)
                {
                    minX = left;
                    minY = bottom;
                    maxX = right;
                    maxY = top;
                    return;
                }

                if (left < minX) minX = left;
                if (bottom < minY) minY = bottom;
                if (right > maxX) maxX = right;
                if (top > maxY) maxY = top;
            }

            public Bounds2 Scaled(float scale)
            {
                float cx = CenterX;
                float cy = CenterY;
                float hx = (maxX - minX) * scale * 0.5f;
                float hy = (maxY - minY) * scale * 0.5f;
                return new Bounds2
                {
                    minX = cx - hx,
                    minY = cy - hy,
                    maxX = cx + hx,
                    maxY = cy + hy,
                };
            }
        }

        struct TileSpot
        {
            public float centerX;
            public float centerY;
            public float minX;
            public float minY;
            public float maxX;
            public float maxY;
        }

        struct EggSpot
        {
            public int layer;
            public int x;
            public int y;
            public Bounds2 footprint;
            public int coverCount;
        }

        struct BoxSpot
        {
            public int layer;
            public int x;
            public int y;
            public BoxCoverSize size;
            public Bounds2 footprint;
            public int upperTiles;
        }

        sealed class Board
        {
            public LevelModel level;
            public Vector2Int evenSize;
            public Vector2Int oddSize;
            public List<TileSpot>[] tiles;
            public Bounds2 board;
            public bool hasBoard;
        }

        static void AppendEggIssues(LevelModel level, List<Finding> issues)
        {
            Board board = BuildBoard(level);
            if (board == null)
            {
                return;
            }

            List<EggSpot> candidates = CollectEggs(board);
            if (candidates.Count < AwardColorEggCoverConfig.EggCount)
            {
                return;
            }

            float midX = BoardMidX(level);
            bool bothSides = ExistsTrio(candidates, null, true, midX);
            for (int i = 0; i < candidates.Count; i++)
            {
                EggSpot spot = candidates[i];
                if (spot.coverCount >= 2)
                {
                    continue;
                }

                bool generated = bothSides
                    ? ExistsTrio(candidates, spot, true, midX)
                    : ExistsTrio(candidates, spot, false, midX);
                if (!generated)
                {
                    continue;
                }

                Add(issues, EggCoverIssue,
                    $"第{spot.layer + 1}层 ({spot.x},{spot.y}) 的 egg 只被 {spot.coverCount} 个棋子遮挡，至少要 2 个");
            }
        }

        static void AppendBoxIssues(LevelModel level, List<Finding> issues)
        {
            Board board = BuildBoard(level);
            if (board == null)
            {
                return;
            }

            HashSet<string> seen = new HashSet<string>();
            bool truncated = false;
            int budget = MaxBoxSolutions;
            for (int layerIndex = 1; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                {
                    continue;
                }

                List<BoxCoverPlacementData> configs = new List<BoxCoverPlacementData>();
                IReadOnlyList<BoxCoverPlacementData> source = layer.BoxCovers;
                for (int i = 0; i < source.Count; i++)
                {
                    BoxCoverPlacementData config = source[i];
                    if (config != null && config.Count > 0 && config.CandidateAnchors.Count > 0)
                    {
                        configs.Add(config);
                    }
                }

                if (configs.Count == 0)
                {
                    continue;
                }

                List<BoxSpot> chosen = new List<BoxSpot>();
                HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
                WalkBoxes(board, layer, layerIndex, configs, 0, occupied, chosen, seen, issues, ref budget, ref truncated);
            }

            if (truncated)
            {
                Add(issues, BoxSameLayerIssue, "落点组合过多，同层遮挡和上方遮挡只检查了部分生成结果");
            }
        }

        static void WalkBoxes(
            Board board,
            Layer layer,
            int layerIndex,
            List<BoxCoverPlacementData> configs,
            int configIndex,
            HashSet<Vector2Int> occupied,
            List<BoxSpot> chosen,
            HashSet<string> seen,
            List<Finding> issues,
            ref int budget,
            ref bool truncated)
        {
            if (truncated || budget <= 0)
            {
                truncated = true;
                return;
            }

            if (configIndex >= configs.Count)
            {
                budget--;
                InspectBoxes(board, chosen, seen, issues);
                return;
            }

            BoxCoverPlacementData config = configs[configIndex];
            Vector2Int size = config.CellSize;
            List<Vector2Int> options = new List<Vector2Int>();
            IReadOnlyList<Vector2Int> anchors = config.CandidateAnchors;
            for (int i = 0; i < anchors.Count; i++)
            {
                Vector2Int anchor = anchors[i];
                if (!InsideLayer(layer, anchor, size) || HitsOccupied(occupied, anchor, size))
                {
                    continue;
                }

                options.Add(anchor);
            }

            if (!PickBoxes(board, layer, layerIndex, configs, configIndex, options, size, config.Size, config.Count, 0, occupied, chosen, seen, issues, ref budget, ref truncated))
            {
                WalkBoxes(board, layer, layerIndex, configs, configIndex + 1, occupied, chosen, seen, issues, ref budget, ref truncated);
            }
        }

        static bool PickBoxes(
            Board board,
            Layer layer,
            int layerIndex,
            List<BoxCoverPlacementData> configs,
            int configIndex,
            List<Vector2Int> options,
            Vector2Int size,
            BoxCoverSize boxSize,
            int need,
            int start,
            HashSet<Vector2Int> occupied,
            List<BoxSpot> chosen,
            HashSet<string> seen,
            List<Finding> issues,
            ref int budget,
            ref bool truncated)
        {
            if (truncated)
            {
                return true;
            }

            if (need == 0)
            {
                WalkBoxes(board, layer, layerIndex, configs, configIndex + 1, occupied, chosen, seen, issues, ref budget, ref truncated);
                return true;
            }

            bool any = false;
            for (int i = start; i <= options.Count - need; i++)
            {
                Vector2Int anchor = options[i];
                if (HitsOccupied(occupied, anchor, size))
                {
                    continue;
                }

                List<Vector2Int> cells = Occupy(occupied, anchor, size);
                chosen.Add(MakeBox(board, layerIndex, anchor, boxSize));
                if (PickBoxes(board, layer, layerIndex, configs, configIndex, options, size, boxSize, need - 1, i + 1, occupied, chosen, seen, issues, ref budget, ref truncated))
                {
                    any = true;
                }

                chosen.RemoveAt(chosen.Count - 1);
                for (int c = 0; c < cells.Count; c++)
                {
                    occupied.Remove(cells[c]);
                }

                if (truncated)
                {
                    break;
                }
            }

            return any;
        }

        static void InspectBoxes(Board board, List<BoxSpot> chosen, HashSet<string> seen, List<Finding> issues)
        {
            for (int i = 0; i < chosen.Count; i++)
            {
                BoxSpot box = chosen[i];
                for (int j = i + 1; j < chosen.Count; j++)
                {
                    BoxSpot other = chosen[j];
                    if (box.layer != other.layer || !Intersects(box.footprint, other.footprint))
                    {
                        continue;
                    }

                    string key = "pair:" + box.layer + ":" + box.x + "," + box.y + ":" + other.x + "," + other.y;
                    if (!seen.Add(key))
                    {
                        continue;
                    }

                    Add(issues, BoxSameLayerIssue,
                        $"第{box.layer + 1}层 {BoxCoverSizeUtility.GetDisplayName(box.size)} ({box.x},{box.y}) 与同层 {BoxCoverSizeUtility.GetDisplayName(other.size)} ({other.x},{other.y}) 发生遮挡");
                }

                if (box.upperTiles > 0)
                {
                    continue;
                }

                string bare = "bare:" + box.layer + ":" + box.x + "," + box.y + ":" + (int)box.size;
                if (!seen.Add(bare))
                {
                    continue;
                }

                Add(issues, BoxUpperIssue,
                    $"第{box.layer + 1}层 {BoxCoverSizeUtility.GetDisplayName(box.size)} ({box.x},{box.y}) 上方没有棋子遮挡，至少要 1 个");
            }
        }

        static BoxSpot MakeBox(Board board, int layerIndex, Vector2Int anchor, BoxCoverSize size)
        {
            Vector2Int cellSize = BoxCoverSizeUtility.GetCellSize(size);
            Bounds2 footprint = new Bounds2();
            for (int y = 0; y < cellSize.y; y++)
            {
                for (int x = 0; x < cellSize.x; x++)
                {
                    EncapsulateCell(board, layerIndex, anchor.x + x, anchor.y + y, ref footprint);
                }
            }

            Bounds2 blocker = footprint.Scaled(0.96f);
            int upper = 0;
            for (int layer = 0; layer < layerIndex && layer < board.tiles.Length; layer++)
            {
                List<TileSpot> tiles = board.tiles[layer];
                for (int i = 0; i < tiles.Count; i++)
                {
                    TileSpot tile = tiles[i];
                    if (Intersects(blocker, tile.minX, tile.minY, tile.maxX, tile.maxY))
                    {
                        upper++;
                    }
                }
            }

            return new BoxSpot
            {
                layer = layerIndex,
                x = anchor.x,
                y = anchor.y,
                size = size,
                footprint = footprint,
                upperTiles = upper,
            };
        }

        static List<EggSpot> CollectEggs(Board board)
        {
            List<EggSpot> result = new List<EggSpot>();
            LevelModel level = board.level;
            if (!board.hasBoard || level.AmountOfLayers < 2)
            {
                return result;
            }

            for (int layerIndex = 1; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                int footprintLayer = layerIndex - 1;
                Layer cover = level.GetLayer(footprintLayer);
                if (cover == null)
                {
                    continue;
                }

                for (int y = 0; y <= cover.AmountOfRows - EggCellSize.y; y++)
                {
                    LayerRow row = cover.GetRow(y);
                    if (row == null)
                    {
                        continue;
                    }

                    for (int x = 0; x <= row.AmountOfCells - EggCellSize.x; x++)
                    {
                        if (CountFilled(cover, x, y, EggCellSize) <= 0)
                        {
                            continue;
                        }

                        if (FootprintHitsDelta(level, x, y))
                        {
                            continue;
                        }

                        Bounds2 footprint = FootprintOf(board, footprintLayer, x, y, EggCellSize);
                        if (!footprint.Valid || !InsideBoard(footprint, board.board))
                        {
                            continue;
                        }

                        if (CountCentersInside(board, layerIndex, footprint, strict: true) < 2)
                        {
                            continue;
                        }

                        result.Add(new EggSpot
                        {
                            layer = layerIndex,
                            x = x,
                            y = y,
                            footprint = footprint,
                            coverCount = CountCentersInside(board, layerIndex, footprint, strict: false, upperOnly: true),
                        });
                    }
                }
            }

            return result;
        }

        static bool ExistsTrio(List<EggSpot> candidates, EggSpot? forced, bool requireBothSides, float midX)
        {
            List<EggSpot> chosen = new List<EggSpot>();
            if (forced.HasValue)
            {
                chosen.Add(forced.Value);
            }

            return SearchEggs(candidates, 0, chosen, requireBothSides, midX);
        }

        static bool SearchEggs(List<EggSpot> candidates, int start, List<EggSpot> chosen, bool requireBothSides, float midX)
        {
            if (chosen.Count == AwardColorEggCoverConfig.EggCount)
            {
                return !requireBothSides || !EggsOnSameSide(chosen, midX);
            }

            for (int i = start; i < candidates.Count; i++)
            {
                EggSpot candidate = candidates[i];
                if (OverlapsEgg(chosen, candidate))
                {
                    continue;
                }

                chosen.Add(candidate);
                if (SearchEggs(candidates, i + 1, chosen, requireBothSides, midX))
                {
                    chosen.RemoveAt(chosen.Count - 1);
                    return true;
                }

                chosen.RemoveAt(chosen.Count - 1);
            }

            return false;
        }

        static bool OverlapsEgg(List<EggSpot> chosen, EggSpot candidate)
        {
            const float gap = 0.02f;
            for (int i = 0; i < chosen.Count; i++)
            {
                Bounds2 other = chosen[i].footprint;
                bool separated = candidate.footprint.maxX <= other.minX + gap
                    || other.maxX <= candidate.footprint.minX + gap
                    || candidate.footprint.maxY <= other.minY + gap
                    || other.maxY <= candidate.footprint.minY + gap;
                if (!separated)
                {
                    return true;
                }
            }

            return false;
        }

        static bool EggsOnSameSide(List<EggSpot> chosen, float midX)
        {
            int left = 0;
            int right = 0;
            for (int i = 0; i < chosen.Count; i++)
            {
                float center = chosen[i].x + EggCellSize.x * 0.5f;
                if (center < midX)
                {
                    left++;
                }
                else
                {
                    right++;
                }
            }

            return left == 0 || right == 0;
        }

        static int CountCentersInside(Board board, int eggLayer, Bounds2 footprint, bool strict, bool upperOnly = false)
        {
            int count = 0;
            int layerStart = upperOnly ? 0 : eggLayer;
            int layerEnd = upperOnly ? eggLayer : eggLayer + 1;
            for (int layer = layerStart; layer < layerEnd && layer < board.tiles.Length; layer++)
            {
                List<TileSpot> tiles = board.tiles[layer];
                for (int i = 0; i < tiles.Count; i++)
                {
                    TileSpot tile = tiles[i];
                    if (strict)
                    {
                        if (tile.centerX > footprint.minX && tile.centerX < footprint.maxX
                            && tile.centerY > footprint.minY && tile.centerY < footprint.maxY)
                        {
                            count++;
                        }
                    }
                    else if (CenterCoversEgg(footprint, tile.centerX, tile.centerY))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        static bool CenterCoversEgg(Bounds2 footprint, float x, float y)
        {
            float slack = Mathf.Min(footprint.maxX - footprint.minX, footprint.maxY - footprint.minY) * 0.04f;
            return x >= footprint.minX - slack
                && x < footprint.maxX - slack
                && y >= footprint.minY - slack
                && y < footprint.maxY - slack;
        }

        static Board BuildBoard(LevelModel level)
        {
            if (level == null || level.AmountOfLayers <= 0)
            {
                return null;
            }

            Board board = new Board
            {
                level = level,
                evenSize = LayerSize(level, level.AmountOfLayers - 1),
                oddSize = level.AmountOfLayers >= 2
                    ? LayerSize(level, level.AmountOfLayers - 2)
                    : LayerSize(level, level.AmountOfLayers - 1),
                tiles = new List<TileSpot>[level.AmountOfLayers],
            };

            Bounds2 bounds = new Bounds2();
            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                List<TileSpot> tiles = new List<TileSpot>();
                board.tiles[layerIndex] = tiles;
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                {
                    continue;
                }

                for (int y = 0; y < layer.AmountOfRows; y++)
                {
                    LayerRow row = layer.GetRow(y);
                    if (row == null)
                    {
                        continue;
                    }

                    for (int x = 0; x < row.AmountOfCells; x++)
                    {
                        CellModel cell = row.GetCell(x);
                        if (cell == null || !cell.IsFilled)
                        {
                            continue;
                        }

                        Vector3 center = CellCenter(board, layerIndex, x, y);
                        float half = TileSize * 0.5f;
                        TileSpot spot = new TileSpot
                        {
                            centerX = center.x,
                            centerY = center.y,
                            minX = center.x - half,
                            minY = center.y - half,
                            maxX = center.x + half,
                            maxY = center.y + half,
                        };
                        tiles.Add(spot);
                        bounds.Encapsulate(spot.minX, spot.minY, spot.maxX, spot.maxY);
                    }
                }
            }

            board.board = bounds;
            board.hasBoard = bounds.Valid;
            return board;
        }

        static Vector3 CellCenter(Board board, int layerIndex, int x, int y)
        {
            Layer layer = board.level.GetLayer(layerIndex);
            float deltaX = layer != null ? layer.DeltaX : 0f;
            float deltaY = layer != null ? layer.DeltaY : 0f;
            CellModel cell = layer != null ? layer.GetRow(y)?.GetCell(x) : null;
            if (cell != null)
            {
                deltaX += cell.DeltaX;
                deltaY += cell.DeltaY;
            }

            return LevelFitter.GetPreviewLocalPosition(
                new ElementPosition(x, y, layerIndex),
                board.level.AmountOfLayers,
                new Vector2(TileSize, TileSize),
                board.evenSize,
                board.oddSize,
                new Vector2(deltaX, deltaY),
                new Vector2(board.level.ShowLayerDeltaX, board.level.ShowLayerDeltaY));
        }

        static void EncapsulateCell(Board board, int layerIndex, int x, int y, ref Bounds2 footprint)
        {
            Vector3 center = CellCenter(board, layerIndex, x, y);
            float half = TileSize * 0.5f;
            footprint.Encapsulate(center.x - half, center.y - half, center.x + half, center.y + half);
        }

        static Bounds2 FootprintOf(Board board, int layerIndex, int anchorX, int anchorY, Vector2Int size)
        {
            Bounds2 footprint = new Bounds2();
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    EncapsulateCell(board, layerIndex, anchorX + x, anchorY + y, ref footprint);
                }
            }

            return footprint;
        }

        static bool InsideBoard(Bounds2 footprint, Bounds2 board)
        {
            float slack = TileSize * 0.02f;
            return footprint.minX >= board.minX - slack
                && footprint.minY >= board.minY - slack
                && footprint.maxX <= board.maxX + slack
                && footprint.maxY <= board.maxY + slack;
        }

        static int CountFilled(Layer layer, int x, int y, Vector2Int size)
        {
            int filled = 0;
            for (int offsetY = 0; offsetY < size.y; offsetY++)
            {
                LayerRow row = layer.GetRow(y + offsetY);
                if (row == null)
                {
                    continue;
                }

                for (int offsetX = 0; offsetX < size.x; offsetX++)
                {
                    CellModel cell = row.GetCell(x + offsetX);
                    if (cell != null && cell.IsFilled)
                    {
                        filled++;
                    }
                }
            }

            return filled;
        }

        static bool FootprintHitsDelta(LevelModel level, int x, int y)
        {
            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                for (int offsetY = 0; offsetY < EggCellSize.y; offsetY++)
                {
                    for (int offsetX = 0; offsetX < EggCellSize.x; offsetX++)
                    {
                        CellModel cell = layer != null ? layer.GetRow(y + offsetY)?.GetCell(x + offsetX) : null;
                        if (cell == null || !cell.IsFilled)
                        {
                            continue;
                        }

                        if (Mathf.Abs(cell.DeltaX) > 0.001f || Mathf.Abs(cell.DeltaY) > 0.001f)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        static float BoardMidX(LevelModel level)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                {
                    continue;
                }

                for (int y = 0; y < layer.AmountOfRows; y++)
                {
                    LayerRow row = layer.GetRow(y);
                    if (row == null)
                    {
                        continue;
                    }

                    for (int x = 0; x < row.AmountOfCells; x++)
                    {
                        CellModel cell = row.GetCell(x);
                        if (cell == null || !cell.IsFilled)
                        {
                            continue;
                        }

                        if (x < min) min = x;
                        if (x > max) max = x;
                    }
                }
            }

            if (min == int.MaxValue)
            {
                return 0f;
            }

            return (min + max + 1) * 0.5f;
        }

        static Vector2Int LayerSize(LevelModel level, int index)
        {
            Layer layer = level.GetLayer(index);
            int rows = layer != null ? layer.AmountOfRows : 1;
            LayerRow row = layer != null && rows > 0 ? layer.GetRow(0) : null;
            int cols = row != null ? row.AmountOfCells : 1;
            return new Vector2Int(Mathf.Max(1, cols), Mathf.Max(1, rows));
        }

        static bool InsideLayer(Layer layer, Vector2Int anchor, Vector2Int size)
        {
            if (layer == null || anchor.x < 0 || anchor.y < 0 || anchor.y + size.y > layer.AmountOfRows)
            {
                return false;
            }

            for (int y = anchor.y; y < anchor.y + size.y; y++)
            {
                LayerRow row = layer.GetRow(y);
                if (row == null || anchor.x + size.x > row.AmountOfCells)
                {
                    return false;
                }
            }

            return true;
        }

        static bool HitsOccupied(HashSet<Vector2Int> occupied, Vector2Int anchor, Vector2Int size)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    if (occupied.Contains(new Vector2Int(anchor.x + x, anchor.y + y)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static List<Vector2Int> Occupy(HashSet<Vector2Int> occupied, Vector2Int anchor, Vector2Int size)
        {
            List<Vector2Int> added = new List<Vector2Int>(size.x * size.y);
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int cell = new Vector2Int(anchor.x + x, anchor.y + y);
                    if (occupied.Add(cell))
                    {
                        added.Add(cell);
                    }
                }
            }

            return added;
        }

        static bool Intersects(Bounds2 left, Bounds2 right)
        {
            return Intersects(left, right.minX, right.minY, right.maxX, right.maxY);
        }

        static bool Intersects(Bounds2 left, float minX, float minY, float maxX, float maxY)
        {
            const float epsilon = 0.001f;
            return left.minX < maxX - epsilon
                && left.maxX > minX + epsilon
                && left.minY < maxY - epsilon
                && left.maxY > minY + epsilon;
        }

        static void Add(List<Finding> issues, string category, string detail)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Category == category && issues[i].Detail == detail)
                {
                    return;
                }
            }

            issues.Add(new Finding(category, detail));
        }
    }
}
#endif
