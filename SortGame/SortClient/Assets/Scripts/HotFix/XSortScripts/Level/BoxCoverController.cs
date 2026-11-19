using System;
using System.Collections.Generic;
using XSortGameCore;
using UnityEngine;
using FishFramework;

namespace FishXSortPlay
{
    /// <summary>Owns resolved box-cover placements and their runtime state.</summary>
    public sealed class BoxCoverController
    {
        sealed class RuntimeEntry
        {
            public BoxCoverResolvedPlacement placement;
            public BoxCoverBehavior behavior;
            public Vector2 localSize;
            public bool opened;
            public bool collected;
            public bool collecting;
            public int collectedItems;
        }

        public const int ItemsPerBox = 3;

        readonly LevelModel level;
        readonly Transform parent;
        readonly List<RuntimeEntry> entries = new();
        bool disposed;

        public int TotalBoxCount => entries.Count;
        public int TotalItemCount => entries.Count * ItemsPerBox;
        public int CollectedItemCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < entries.Count; i++)
                    total += Mathf.Clamp(entries[i].collectedItems, 0, ItemsPerBox);
                return total;
            }
        }
        public bool HasPendingCollection
        {
            get
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].collecting)
                        return true;
                }
                return false;
            }
        }
        public bool AllCollected
        {
            get
            {
                if (entries.Count == 0)
                    return false;

                for (int i = 0; i < entries.Count; i++)
                {
                    if (!entries[i].collected)
                        return false;
                }
                return true;
            }
        }

        public BoxCoverController(
            LevelModel sourceLevel,
            Transform runtimeParent,
            List<BoxCoverResolvedPlacement> placements,
            List<RecoverLevelData.RLBoxCoverData> restoredStates)
        {
            level = sourceLevel;
            parent = runtimeParent;

            Dictionary<string, RecoverLevelData.RLBoxCoverData> restoredById =
                BuildRestoredStateMap(restoredStates);

            for (int i = 0; i < placements.Count; i++)
            {
                BoxCoverResolvedPlacement placement = placements[i];
                if (placement.layer <= 0 || placement.layer >= level.AmountOfLayers)
                {
                    Debug.LogWarning($"[BoxCover] Ignore invalid runtime placement {placement.RuntimeId}.");
                    continue;
                }

                RuntimeEntry entry = new RuntimeEntry
                {
                    placement = placement,
                };

                if (restoredById.TryGetValue(placement.RuntimeId, out RecoverLevelData.RLBoxCoverData restored))
                {
                    entry.opened = restored.opened;
                    entry.collected = restored.collected;
                    entry.collectedItems = Mathf.Clamp(restored.collectedItems, 0, ItemsPerBox);
                    if (entry.collectedItems >= ItemsPerBox)
                        entry.collected = true;
                    if (entry.collectedItems > 0)
                        entry.opened = true;
                }

                CalculateLocalGeometry(placement, out Vector3 localPosition, out Vector2 localSize);
                entry.localSize = localSize;
                entries.Add(entry);

                if (!entry.collected)
                    entry.behavior = SpawnBehavior(entry, localPosition);
            }

            Physics.SyncTransforms();
        }

        public void CollectActiveBehaviors(List<BoxCoverBehavior> dest)
        {
            if (dest == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                BoxCoverBehavior behavior = entries[i].behavior;
                if (behavior != null)
                    dest.Add(behavior);
            }
        }

        public void CompleteSpawnAnimation()
        {
            for (int i = 0; i < entries.Count; i++)
                entries[i].behavior?.CompleteSpawnAnimation();
            Physics.SyncTransforms();
        }

        static Dictionary<string, RecoverLevelData.RLBoxCoverData> BuildRestoredStateMap(
            List<RecoverLevelData.RLBoxCoverData> restoredStates)
        {
            Dictionary<string, RecoverLevelData.RLBoxCoverData> result = new();
            if (restoredStates == null)
                return result;

            for (int i = 0; i < restoredStates.Count; i++)
            {
                RecoverLevelData.RLBoxCoverData state = restoredStates[i];
                result[state.placement.RuntimeId] = state;
            }
            return result;
        }

        BoxCoverBehavior SpawnBehavior(RuntimeEntry entry, Vector3 localPosition)
        {
            BoxCoverType type = level != null ? level.BoxCoverType : BoxCoverType.Coke;
            string sizeKey = BoxCoverSizeUtility.GetDisplayName(entry.placement.size);
            string prefabPath = BoxCoverThemePaths.GetPrefabPath(type, entry.placement.size);
            GameObject prefab = UILoadHelper.LoadAsset<GameObject>(prefabPath);
            GameObject instance;
            if (prefab != null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            }
            else
            {
                instance = new GameObject($"BoxCover_{sizeKey}_{entry.placement.RuntimeId}");
                instance.transform.SetParent(parent, false);
            }

            BoxCoverBehavior behavior = instance.GetComponent<BoxCoverBehavior>();
            if (behavior == null)
                behavior = instance.AddComponent<BoxCoverBehavior>();

            Sprite closed = BoxCoverAssetLoader.LoadSprite(
                BoxCoverThemePaths.GetClosedSpritePath(type, entry.placement.size));
            Sprite opened = BoxCoverAssetLoader.LoadSprite(
                BoxCoverThemePaths.GetOpenSpritePath(type, entry.placement.size));
            Sprite lid = BoxCoverAssetLoader.LoadSprite(
                BoxCoverThemePaths.GetLidSpritePath(type, entry.placement.size),
                logMissing: false);
            if (lid == null)
                lid = closed;

            int sortingOrder = -(entry.placement.layer * 300) - 150
                + entry.placement.anchorX - entry.placement.anchorY;
            behavior.Initialize(
                this,
                entry.placement,
                closed,
                opened,
                lid,
                localPosition,
                entry.localSize,
                sortingOrder,
                entry.opened,
                entry.collectedItems);
            return behavior;
        }

        void CalculateLocalGeometry(
            BoxCoverResolvedPlacement placement,
            out Vector3 center,
            out Vector2 size)
        {
            Vector2 XSortSize = LevelFitter.XSortSize;
            Vector2Int cellSize = placement.CellSize;
            Vector2Int opposite = placement.Anchor + cellSize - Vector2Int.one;

            // Size follows authored cell span (same as editor preview). Do NOT Encapsulate
            // GetPosition results: per-cell deltaX/deltaY compresses the AABB and turns a
            // 2x2 into a rectangle (level 67 layer cells use ±0.5 under the box anchors).
            size = new Vector2(
                Mathf.Max(0.01f, XSortSize.x * cellSize.x),
                Mathf.Max(0.01f, XSortSize.y * cellSize.y));

            Vector3 anchorPos = LevelFitter.GetPosition(new ElementPosition(
                placement.anchorX,
                placement.anchorY,
                placement.layer));
            Vector3 oppositePos = LevelFitter.GetPosition(new ElementPosition(
                opposite.x,
                opposite.y,
                placement.layer));
            StripCellDelta(ref anchorPos, placement.layer, placement.anchorX, placement.anchorY, XSortSize);
            StripCellDelta(ref oppositePos, placement.layer, opposite.x, opposite.y, XSortSize);

            center = (anchorPos + oppositePos) * 0.5f;
            // Layer indices grow away from the camera. N - 0.5 puts the box between N-1 and N.
            center.z = placement.layer - 0.5f;
        }

        static void StripCellDelta(
            ref Vector3 position,
            int layerId,
            int cellX,
            int cellY,
            Vector2 XSortSize)
        {
            Layer layer = LevelDirector.Level != null
                ? LevelDirector.Level.GetLayer(layerId)
                : null;
            CellModel cell = layer != null ? layer.GetRow(cellY)?.GetCell(cellX) : null;
            if (cell == null)
                return;

            position.x -= cell.DeltaX * XSortSize.x;
            position.y -= cell.DeltaY * XSortSize.y;
        }

        public void UpdateStates(bool acceptBlockersWithoutHint = false)
        {
            if (disposed)
                return;

            Physics.SyncTransforms();
            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry entry = entries[i];
                if (entry.collected || entry.behavior == null)
                    continue;

                if (entry.opened)
                {
                    if (!entry.collecting)
                        BeginCollection(entry);
                    continue;
                }

                int blockerCount = CountUpperBlockers(entry);
                entry.behavior.RefreshBlockers(blockerCount, acceptBlockersWithoutHint);
            }
        }

        int CountUpperBlockers(RuntimeEntry entry)
        {
            LevelGenService levelGen = LevelDirector.LevelGenService;
            List<XSortActor> activeXSorts = levelGen?.ActXSorts;
            if (activeXSorts == null)
                return 0;

            Bounds boxBounds = GetWorldBounds(entry);
            int count = 0;
            for (int i = 0; i < activeXSorts.Count; i++)
            {
                XSortActor XSort = activeXSorts[i];
                if (XSort == null
                    || XSort.IsSubmitted
                    || XSort.LayerID >= entry.placement.layer)
                    continue;
                if (IntersectsXY(boxBounds, GetLogicalXSortBounds(XSort)))
                    count++;
            }

            // Box Covers participate in the layer stack too. An overlapping upper
            // cover must be collected before a lower cover can open.
            // Opened/collecting covers no longer block — lid is off and XSorts should play.
            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry upperBox = entries[i];
                if (upperBox == entry
                    || upperBox.collected
                    || upperBox.opened
                    || upperBox.collecting
                    || upperBox.behavior == null
                    || upperBox.placement.layer >= entry.placement.layer)
                {
                    continue;
                }

                if (IntersectsXY(boxBounds, GetWorldBounds(upperBox)))
                    count++;
            }

            return count;
        }

        public bool IsXSortBlocked(XSortActor XSort)
        {
            if (disposed || XSort == null || XSort.IsSubmitted)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry entry = entries[i];
                // Only closed covers block. Opened/collecting covers fade/fly away but
                // previously still blocked XSorts → "invisible boxcover" / unclickable.
                if (entry.collected
                    || entry.opened
                    || entry.collecting
                    || entry.behavior == null
                    || XSort.LayerID < entry.placement.layer)
                {
                    continue;
                }

                if (ContainsCenter(entry, XSort.transform.position))
                    return true;
            }
            return false;
        }

        public bool IsClickBlocked(XSortActor XSort, Vector3 worldPoint)
        {
            if (disposed || XSort == null || XSort.IsSubmitted)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry entry = entries[i];
                if (entry.collected
                    || entry.opened
                    || entry.collecting
                    || entry.behavior == null)
                    continue;
                if (!entry.behavior.ImageCovers(worldPoint))
                    continue;
                if (XSort.LayerID < entry.placement.layer
                    && ContainsCenter(entry, XSort.transform.position))
                    continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 与 ColorEgg 同套路：射线优先落到下方棋子时，若落点在箱子图上，改由箱子接收点击（抖动）。
        /// 上层压箱的棋子仍交给棋子自己处理。
        /// </summary>
        public bool TryHandleBlockedClick(Vector3 worldPoint, XSortActor resolvedXSort = null)
        {
            if (disposed)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry entry = entries[i];
                if (entry.collected
                    || entry.opened
                    || entry.collecting
                    || entry.behavior == null)
                    continue;
                if (!entry.behavior.ImageCovers(worldPoint))
                    continue;
                if (resolvedXSort != null
                    && !resolvedXSort.IsSubmitted
                    && resolvedXSort.LayerID < entry.placement.layer)
                    return false;

                entry.behavior.OnObjectClicked(out _);
                return true;
            }

            return false;
        }

        static Bounds GetLogicalXSortBounds(XSortActor XSort)
        {
            Vector2 XSortSize = LevelFitter.XSortSize;
            Vector3 center = XSort != null ? XSort.transform.position : Vector3.zero;
            Transform parent = XSort != null ? XSort.transform.parent : null;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            return new Bounds(
                center,
                new Vector3(
                    Mathf.Max(0.01f, XSortSize.x * Mathf.Abs(parentScale.x)),
                    Mathf.Max(0.01f, XSortSize.y * Mathf.Abs(parentScale.y)),
                    0.18f));
        }

        Bounds GetWorldBounds(RuntimeEntry entry)
        {
            return GetFootprintBounds(entry, 0.96f);
        }

        bool ContainsCenter(RuntimeEntry entry, Vector3 worldPoint)
        {
            return ContainsPoint(GetFootprintBounds(entry, 1f), worldPoint);
        }

        static bool ContainsPoint(Bounds bounds, Vector3 worldPoint)
        {
            return worldPoint.x >= bounds.min.x
                && worldPoint.x <= bounds.max.x
                && worldPoint.y >= bounds.min.y
                && worldPoint.y <= bounds.max.y;
        }

        Bounds GetFootprintBounds(RuntimeEntry entry, float scale)
        {
            if (entry.behavior == null)
                return default;

            Transform entryTransform = entry.behavior.transform;
            Transform parent = entryTransform.parent;
            Vector3 lossyScale = parent != null ? parent.lossyScale : Vector3.one;
            return new Bounds(
                entryTransform.position,
                new Vector3(
                    Mathf.Abs(entry.localSize.x * lossyScale.x) * scale,
                    Mathf.Abs(entry.localSize.y * lossyScale.y) * scale,
                    0.18f));
        }

        static bool IntersectsXY(Bounds left, Bounds right)
        {
            const float epsilon = 0.001f;
            return left.min.x < right.max.x - epsilon
                && left.max.x > right.min.x + epsilon
                && left.min.y < right.max.y - epsilon
                && left.max.y > right.min.y + epsilon;
        }

        public void RequestOpen(BoxCoverBehavior behavior)
        {
            RuntimeEntry entry = FindEntry(behavior);
            if (entry == null || entry.opened || entry.collected || entry.collecting)
                return;

            entry.opened = true;
            BeginCollection(entry);
        }

        void BeginCollection(RuntimeEntry entry)
        {
            if (entry == null || entry.behavior == null || entry.collected || entry.collecting)
                return;

            entry.opened = true;
            entry.collecting = true;
            // Unlock XSorts under this cover immediately (do not wait for fly-collect end).
            // Only refresh XSort click flags — full UpdateClickableStates would re-enter UpdateStates.
            RefreshXSortClickablesAfterCoverOpened();
            if (IsLastBoxToCollect(entry))
                BoxCoverManager.BeginWaitForBoxDogExit();
            entry.behavior.BeginOpenAndCollect(
                () => BoxCoverManager.GetHudTargetWorldPosition(entry.behavior.transform.position),
                _ =>
                {
                    entry.opened = true;
                    SaveRecoveryState();
                },
                behavior =>
                {
                    entry.collectedItems = Mathf.Clamp(
                        behavior.CollectedItemCount,
                        0,
                        ItemsPerBox);
                    BoxCoverManager.NotifyItemCollected(CollectedItemCount, TotalItemCount);
                    SaveRecoveryState();
                },
                behavior =>
                {
                    entry.collecting = false;
                    entry.opened = true;
                    entry.collected = true;
                    entry.collectedItems = ItemsPerBox;
                    if (behavior != null)
                    {
                        behavior.Dispose();
                        entry.behavior = null;
                    }

                    SaveRecoveryState();
                    BoxCoverManager.NotifyBoxCollectionCompleted();
                    LevelDirector.LevelGenService?.UpdateClickableStates(true);
                });
        }

        static void RefreshXSortClickablesAfterCoverOpened()
        {
            LevelGenService gen = LevelDirector.LevelGenService;
            List<XSortActor> XSorts = gen?.ActXSorts;
            if (XSorts == null)
                return;

            for (int i = 0; i < XSorts.Count; i++)
            {
                XSortActor XSort = XSorts[i];
                if (XSort == null || XSort.IsSubmitted)
                    continue;
                XSort.SetIsClickable(gen.IsXSortUnconcealed(XSort), true);
            }

            gen.RefreshStackOccludedEffectVisibility();
        }

        bool IsLastBoxToCollect(RuntimeEntry entry)
        {
            if (entry == null || entries.Count == 0)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry other = entries[i];
                if (other == entry)
                    continue;
                if (!other.collected && !other.collecting)
                    return false;
            }

            return true;
        }

        RuntimeEntry FindEntry(BoxCoverBehavior behavior)
        {
            if (behavior == null)
                return null;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].behavior == behavior)
                    return entries[i];
            }
            return null;
        }

        public void HandleBoxClicked(BoxCoverBehavior behavior)
        {
            if (disposed || behavior == null || behavior.IsCollected)
                return;

            if (GameData.BFromStartScene)
            {
                RecoverLevelData.MarkUserProgress();
                RecoverLevelData.SaveCurrentLevelData();
            }
        }

        void SaveRecoveryState()
        {
            if (GameData.BFromStartScene && GameDirector.IsGameActive)
                RecoverLevelData.SaveCurrentLevelData();
        }

        public void WriteRecoveryData(RecoverLevelData target)
        {
            if (target == null)
                return;

            target.BoxCoverStates ??= new List<RecoverLevelData.RLBoxCoverData>();
            target.BoxCoverStates.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry entry = entries[i];
                target.BoxCoverStates.Add(new RecoverLevelData.RLBoxCoverData
                {
                    placement = entry.placement,
                    opened = entry.opened,
                    collected = entry.collected,
                    collectedItems = Mathf.Clamp(entry.collectedItems, 0, ItemsPerBox),
                });
            }

            target.boxCoverCollectedCount = CollectedItemCount;
            target.boxCoverTotalCount = TotalItemCount;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].behavior != null)
                    entries[i].behavior.Dispose();
                entries[i].behavior = null;
            }
            entries.Clear();
        }
    }

    internal static class BoxCoverAssetLoader
    {
        static readonly Dictionary<string, Sprite> SpriteCache = new();

        public static Sprite LoadSprite(string assetPath, bool logMissing = true)
        {
            if (SpriteCache.TryGetValue(assetPath, out Sprite cached) && cached != null)
                return cached;

            Sprite sprite = UILoadHelper.LoadAsset<Sprite>(assetPath);
            if (sprite == null)
            {
                Texture2D texture = UILoadHelper.LoadAsset<Texture2D>(assetPath);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    sprite.name = texture.name + "_RuntimeSprite";
                }
            }

            if (sprite == null)
            {
                if (logMissing)
                    Debug.LogWarning($"[BoxCover] Missing sprite: {assetPath}");
                return null;
            }

            SpriteCache[assetPath] = sprite;
            return sprite;
        }
    }
}
