using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    public enum BoxCoverSize
    {
        Size2x2 = 0,
        Size2x3 = 1,
        Size3x3 = 2,
    }

    public enum BoxCoverType
    {
        Coke = 0,
    }

    public static class BoxCoverThemePaths
    {
        const string CokePrefabFolder = "Assets/CABRes/Prefabs/BoxCover/Coke";
        const string CokeSpriteFolder = "Assets/CABRes/Sprites/BoxCover/Coke";

        public static string GetDisplayName(BoxCoverType type)
        {
            return type switch
            {
                _ => "Coke",
            };
        }

        public static string GetPrefabPath(BoxCoverType type, BoxCoverSize size)
        {
            string sizeKey = BoxCoverSizeUtility.GetDisplayName(size);
            return type switch
            {
                _ => $"{CokePrefabFolder}/Coke_BoxCover_{sizeKey}.prefab",
            };
        }

        public static string GetClosedSpritePath(BoxCoverType type, BoxCoverSize size)
        {
            return GetSpriteBase(type, size) + "_closed.png";
        }

        public static string GetOpenSpritePath(BoxCoverType type, BoxCoverSize size)
        {
            return GetSpriteBase(type, size) + "_open.png";
        }

        public static string GetLidSpritePath(BoxCoverType type, BoxCoverSize size)
        {
            return GetSpriteBase(type, size) + "_lid.png";
        }

        public static string GetCollectibleSpritePath(BoxCoverType type)
        {
            return type switch
            {
                _ => $"{CokeSpriteFolder}/Coke.png",
            };
        }

        public static string GetContentItemSpritePath(BoxCoverType type, BoxCoverSize size, int itemIndex)
        {
            string sizedItem = $"{GetSpriteBase(type, size)}_{itemIndex + 1:00}.png";
            return sizedItem;
        }

        static string GetSpriteBase(BoxCoverType type, BoxCoverSize size)
        {
            string sizeKey = BoxCoverSizeUtility.GetDisplayName(size);
            return type switch
            {
                _ => $"{CokeSpriteFolder}/Coke_boxcover_{sizeKey}",
            };
        }
    }

    [Serializable]
    public sealed class BoxCoverPlacementData
    {
        [SerializeField] BoxCoverSize size;
        [SerializeField, Min(0)] int count;
        [SerializeField] List<Vector2Int> candidateAnchors = new();

        public BoxCoverSize Size => size;
        public int Count => Mathf.Max(0, count);
        public IReadOnlyList<Vector2Int> CandidateAnchors
        {
            get
            {
                return candidateAnchors ?? (IReadOnlyList<Vector2Int>)
                    Array.Empty<Vector2Int>();
            }
        }

        public BoxCoverPlacementData(BoxCoverSize size)
        {
            this.size = size;
            count = 0;
        }

        public Vector2Int CellSize => BoxCoverSizeUtility.GetCellSize(size);

        public bool ContainsAnchor(Vector2Int anchor)
        {
            return candidateAnchors != null && candidateAnchors.Contains(anchor);
        }

        public void SetCount(int value)
        {
            count = Mathf.Max(0, value);
        }

        public void ToggleAnchor(Vector2Int anchor)
        {
            candidateAnchors ??= new List<Vector2Int>();
            if (!candidateAnchors.Remove(anchor))
            {
                candidateAnchors.Add(anchor);
                if (count == 0)
                {
                    count = 1;
                }
            }
        }

        public void Clear()
        {
            count = 0;
            candidateAnchors?.Clear();
        }
    }

    [Serializable]
    public struct BoxCoverResolvedPlacement
    {
        public int layer;
        public int anchorX;
        public int anchorY;
        public BoxCoverSize size;

        public BoxCoverResolvedPlacement(int layer, Vector2Int anchor, BoxCoverSize size)
        {
            this.layer = layer;
            anchorX = anchor.x;
            anchorY = anchor.y;
            this.size = size;
        }

        public Vector2Int Anchor => new(anchorX, anchorY);
        public Vector2Int CellSize => BoxCoverSizeUtility.GetCellSize(size);
        public string RuntimeId => $"{layer}:{anchorX}:{anchorY}:{(int)size}";
    }

    public static class BoxCoverSizeUtility
    {
        public static Vector2Int GetCellSize(BoxCoverSize size)
        {
            return size switch
            {
                BoxCoverSize.Size2x3 => new Vector2Int(2, 3),
                BoxCoverSize.Size3x3 => new Vector2Int(3, 3),
                _ => new Vector2Int(2, 2),
            };
        }

        public static string GetDisplayName(BoxCoverSize size)
        {
            return size switch
            {
                BoxCoverSize.Size2x3 => "2x3",
                BoxCoverSize.Size3x3 => "3x3",
                _ => "2x2",
            };
        }
    }

    public static class BoxCoverLayoutResolver
    {
        public static List<BoxCoverResolvedPlacement> Resolve(
            LevelModel level,
            int reduction,
            int seed)
        {
            List<BoxCoverResolvedPlacement> result = new();
            if (level == null)
            {
                return result;
            }

            System.Random random = new(seed);
            for (int layerIndex = 1; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                {
                    continue;
                }

                HashSet<Vector2Int> occupied = new();
                IReadOnlyList<BoxCoverPlacementData> configurations = layer.BoxCovers;
                for (int configIndex = 0; configIndex < configurations.Count; configIndex++)
                {
                    BoxCoverPlacementData config = configurations[configIndex];
                    if (config == null || config.Count <= 0)
                    {
                        continue;
                    }

                    List<Vector2Int> candidates = new(config.CandidateAnchors);
                    Shuffle(candidates, random);
                    candidates.RemoveAll(anchor =>
                        !IsInsideLayer(layer, anchor, config.CellSize)
                        || Overlaps(occupied, anchor, config.CellSize));

                    List<Vector2Int> selected = new();
                    SelectNonOverlapping(
                        candidates,
                        config.CellSize,
                        config.Count,
                        0,
                        new HashSet<Vector2Int>(),
                        selected);

                    for (int selectedIndex = 0; selectedIndex < selected.Count; selectedIndex++)
                    {
                        Vector2Int anchor = selected[selectedIndex];
                        Occupy(occupied, anchor, config.CellSize);
                        result.Add(new BoxCoverResolvedPlacement(layerIndex, anchor, config.Size));
                    }
                }
            }

            int removable = Mathf.Clamp(reduction, 0, Mathf.Max(0, result.Count - 2));
            if (removable > 0)
            {
                result.Sort(CompareBottomFirst);
                result.RemoveRange(0, removable);
            }

            result.Sort(CompareTopFirst);
            return result;
        }

        public static List<string> Validate(LevelModel level)
        {
            List<string> errors = new();
            if (level == null)
            {
                errors.Add("LevelModel is null.");
                return errors;
            }

            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                {
                    continue;
                }

                IReadOnlyList<BoxCoverPlacementData> configurations = layer.BoxCovers;
                for (int configIndex = 0; configIndex < configurations.Count; configIndex++)
                {
                    BoxCoverPlacementData config = configurations[configIndex];
                    if (config == null || (config.Count == 0 && config.CandidateAnchors.Count == 0))
                    {
                        continue;
                    }

                    if (layerIndex == 0)
                    {
                        errors.Add(
                            $"Layer {layerIndex + 1} (top) can't contain box covers "
                            + $"({BoxCoverSizeUtility.GetDisplayName(config.Size)}).");
                    }

                    HashSet<Vector2Int> uniqueAnchors = new();
                    List<Vector2Int> validCandidates = new();
                    for (int anchorIndex = 0; anchorIndex < config.CandidateAnchors.Count; anchorIndex++)
                    {
                        Vector2Int anchor = config.CandidateAnchors[anchorIndex];
                        if (!uniqueAnchors.Add(anchor))
                        {
                            errors.Add(
                                $"Layer {layerIndex + 1} {BoxCoverSizeUtility.GetDisplayName(config.Size)} "
                                + $"contains duplicate anchor {anchor}.");
                            continue;
                        }

                        if (!IsInsideLayer(layer, anchor, config.CellSize))
                        {
                            errors.Add(
                                $"Layer {layerIndex + 1} {BoxCoverSizeUtility.GetDisplayName(config.Size)} "
                                + $"anchor {anchor} is outside the grid.");
                            continue;
                        }

                        validCandidates.Add(anchor);
                    }

                    if (!CanSelectCount(validCandidates, config.CellSize, config.Count))
                    {
                        errors.Add(
                            $"Layer {layerIndex + 1} {BoxCoverSizeUtility.GetDisplayName(config.Size)} "
                            + $"cannot place {config.Count} non-overlapping boxes in its candidate area.");
                    }

                    for (int otherIndex = configIndex + 1;
                         otherIndex < configurations.Count;
                         otherIndex++)
                    {
                        BoxCoverPlacementData other = configurations[otherIndex];
                        if (other == null)
                        {
                            continue;
                        }

                        for (int leftIndex = 0;
                             leftIndex < config.CandidateAnchors.Count;
                             leftIndex++)
                        {
                            Vector2Int leftAnchor = config.CandidateAnchors[leftIndex];
                            for (int rightIndex = 0;
                                 rightIndex < other.CandidateAnchors.Count;
                                 rightIndex++)
                            {
                                if (RectanglesOverlap(
                                    leftAnchor,
                                    config.CellSize,
                                    other.CandidateAnchors[rightIndex],
                                    other.CellSize))
                                {
                                    errors.Add(
                                        $"Layer {layerIndex + 1}: Overlay issue occurs with boxes "
                                        + "of different sizes on the same layer.");
                                    leftIndex = config.CandidateAnchors.Count;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return errors;
        }

        public static bool CoversCell(
            BoxCoverResolvedPlacement placement,
            int layer,
            int x,
            int y)
        {
            if (placement.layer != layer)
            {
                return false;
            }

            Vector2Int size = placement.CellSize;
            return x >= placement.anchorX
                && x < placement.anchorX + size.x
                && y >= placement.anchorY
                && y < placement.anchorY + size.y;
        }

        private static bool IsInsideLayer(Layer layer, Vector2Int anchor, Vector2Int size)
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

        private static bool Overlaps(HashSet<Vector2Int> occupied, Vector2Int anchor, Vector2Int size)
        {
            foreach (Vector2Int cell in EnumerateCells(anchor, size))
            {
                if (occupied.Contains(cell))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RectanglesOverlap(
            Vector2Int leftAnchor,
            Vector2Int leftSize,
            Vector2Int rightAnchor,
            Vector2Int rightSize)
        {
            return leftAnchor.x < rightAnchor.x + rightSize.x
                && leftAnchor.x + leftSize.x > rightAnchor.x
                && leftAnchor.y < rightAnchor.y + rightSize.y
                && leftAnchor.y + leftSize.y > rightAnchor.y;
        }

        private static void Occupy(HashSet<Vector2Int> occupied, Vector2Int anchor, Vector2Int size)
        {
            foreach (Vector2Int cell in EnumerateCells(anchor, size))
            {
                occupied.Add(cell);
            }
        }

        private static IEnumerable<Vector2Int> EnumerateCells(Vector2Int anchor, Vector2Int size)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    yield return anchor + new Vector2Int(x, y);
                }
            }
        }

        private static void Shuffle<T>(IList<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        private static bool CanSelectCount(
            List<Vector2Int> candidates,
            Vector2Int size,
            int required)
        {
            if (required <= 0)
            {
                return true;
            }

            return SelectNonOverlapping(
                candidates,
                size,
                required,
                0,
                new HashSet<Vector2Int>(),
                new List<Vector2Int>());
        }

        private static bool SelectNonOverlapping(
            List<Vector2Int> candidates,
            Vector2Int size,
            int required,
            int startIndex,
            HashSet<Vector2Int> occupied,
            List<Vector2Int> selected)
        {
            if (selected.Count >= required)
            {
                return true;
            }

            int remainingNeeded = required - selected.Count;
            for (int i = startIndex; i <= candidates.Count - remainingNeeded; i++)
            {
                Vector2Int anchor = candidates[i];
                if (Overlaps(occupied, anchor, size))
                {
                    continue;
                }

                List<Vector2Int> addedCells = new();
                foreach (Vector2Int cell in EnumerateCells(anchor, size))
                {
                    if (occupied.Add(cell))
                    {
                        addedCells.Add(cell);
                    }
                }
                selected.Add(anchor);

                if (SelectNonOverlapping(candidates, size, required, i + 1, occupied, selected))
                {
                    return true;
                }

                selected.RemoveAt(selected.Count - 1);
                for (int cellIndex = 0; cellIndex < addedCells.Count; cellIndex++)
                {
                    occupied.Remove(addedCells[cellIndex]);
                }
            }

            return false;
        }

        private static int CompareBottomFirst(
            BoxCoverResolvedPlacement left,
            BoxCoverResolvedPlacement right)
        {
            int layerCompare = right.layer.CompareTo(left.layer);
            return layerCompare != 0
                ? layerCompare
                : string.CompareOrdinal(left.RuntimeId, right.RuntimeId);
        }

        private static int CompareTopFirst(
            BoxCoverResolvedPlacement left,
            BoxCoverResolvedPlacement right)
        {
            int layerCompare = left.layer.CompareTo(right.layer);
            return layerCompare != 0
                ? layerCompare
                : string.CompareOrdinal(left.RuntimeId, right.RuntimeId);
        }
    }
}
