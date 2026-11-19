using System.Collections.Generic;
using FishFramework;
using UnityEngine;

namespace FishXSortPlay
{
    public sealed class AwardColorEggCoverController
    {
        sealed class RuntimeEntry
        {
            public AwardColorEggCoverPlacement placement;
            public AwardColorEggCoverBehavior behavior;
            public Vector2 localSize;
            public bool collected;
            public bool collecting;
        }

        readonly LevelModel level;
        readonly Transform parent;
        readonly List<RuntimeEntry> entries = new();
        readonly GameObject coverPrefab;
        Sprite collectibleSprite;
        bool disposed;

        public Sprite CollectibleSprite => collectibleSprite;

        public int TotalCount => entries.Count;
        public int CollectedCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].collected)
                        total++;
                }
                return total;
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
        public bool AreAllSettling
        {
            get
            {
                if (entries.Count == 0)
                    return false;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (!entries[i].collected && !entries[i].collecting)
                        return false;
                }
                return true;
            }
        }

        public AwardColorEggCoverController(
            LevelModel sourceLevel,
            Transform runtimeParent,
            List<AwardColorEggCoverPlacement> placements,
            List<RecoverLevelData.RLAwardColorEggCoverData> restored)
        {
            level = sourceLevel;
            parent = runtimeParent;
            coverPrefab = LoadCoverPrefab();
            collectibleSprite = ReadPrefabSprite(coverPrefab);
            Dictionary<string, RecoverLevelData.RLAwardColorEggCoverData> restoredById = new();
            if (restored != null)
            {
                for (int i = 0; i < restored.Count; i++)
                    restoredById[restored[i].placement.RuntimeId] = restored[i];
            }

            for (int i = 0; i < placements.Count; i++)
            {
                AwardColorEggCoverPlacement placement = placements[i];
                if (placement.layer <= 0 || placement.layer >= level.AmountOfLayers)
                    continue;

                RuntimeEntry entry = new RuntimeEntry { placement = placement };
                if (restoredById.TryGetValue(placement.RuntimeId, out RecoverLevelData.RLAwardColorEggCoverData saved)
                    && saved.collected)
                {
                    entry.collected = true;
                }

                CalculateLocalGeometry(placement, out Vector3 localPosition, out Vector2 localSize);
                entry.localSize = localSize;
                entries.Add(entry);
                if (!entry.collected)
                    entry.behavior = Spawn(entry, localPosition);
            }

            Physics.SyncTransforms();
        }

        public void CollectActiveBehaviors(List<AwardColorEggCoverBehavior> dest)
        {
            if (dest == null)
                return;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].behavior != null)
                    dest.Add(entries[i].behavior);
            }
        }

        public void CompleteSpawnAnimation()
        {
            for (int i = 0; i < entries.Count; i++)
                entries[i].behavior?.CompleteSpawnAnimation();
            Physics.SyncTransforms();
        }

        public void UpdateStates(bool acceptBlockersWithoutHint = false)
        {
            if (disposed)
                return;

            Physics.SyncTransforms();
            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry entry = entries[i];
                if (entry.collected || entry.collecting || entry.behavior == null)
                    continue;
                entry.behavior.RefreshBlockers(CountUpperBlockers(entry), acceptBlockersWithoutHint);
            }
        }

        public bool IsXSortBlocked(XSortActor XSort)
        {
            if (disposed || XSort == null || XSort.IsSubmitted)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry entry = entries[i];
                if (entry.collected || entry.collecting || entry.behavior == null)
                    continue;
                // 上层棋压在蛋上，用来揭开蛋，不算被蛋盖住。
                if (XSort.LayerID < entry.placement.layer)
                    continue;
                // 与常规棋子互盖一致：只要和蛋碰撞盒重叠（盖住一部分也算），整颗不可点。
                if (OverlapsEggCollider(entry, XSort))
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
                if (entry.collected || entry.collecting || entry.behavior == null)
                    continue;
                if (!entry.behavior.ImageCovers(worldPoint))
                    continue;
                // 上层棋整颗可点。已被蛋挡住的下层棋交给棋子自己抖，不在这里拦。
                if (XSort.LayerID < entry.placement.layer)
                    continue;
                if (OverlapsEggCollider(entry, XSort))
                    continue;
                return true;
            }

            return false;
        }

        static bool OverlapsEggCollider(RuntimeEntry entry, XSortActor XSort)
        {
            if (entry == null || entry.behavior == null || XSort == null)
                return false;

            BoxCollider XSortCol = XSort.BoxCollider != null
                ? XSort.BoxCollider
                : XSort.GetComponent<BoxCollider>();
            return XSortStackUtil.AreCollidersOverlappingXY(entry.behavior.BoxCollider, XSortCol);
        }

        public bool TryHandleBlockedClick(Vector3 worldPoint, XSortActor resolvedXSort = null)
        {
            if (disposed)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry entry = entries[i];
                if (entry.collected || entry.collecting || entry.behavior == null)
                    continue;
                if (!entry.behavior.ImageCovers(worldPoint))
                    continue;
                // 上层棋压在蛋上：点击交给棋子揭蛋，不让蛋抢。
                if (resolvedXSort != null
                    && !resolvedXSort.IsSubmitted
                    && resolvedXSort.LayerID < entry.placement.layer)
                    return false;
                entry.behavior.OnObjectClicked(out _);
                return true;
            }

            return false;
        }

        public void RequestCollect(AwardColorEggCoverBehavior behavior)
        {
            RuntimeEntry entry = Find(behavior);
            if (entry == null || entry.collected || entry.collecting)
                return;

            entry.collecting = true;
            Vector3 target = AwardColorEggCoverManager.GetHudTargetWorldPosition(behavior.transform.position);
            behavior.BeginCollect(target, () =>
            {
                entry.collecting = false;
                entry.collected = true;
                entry.behavior = null;
                AwardColorEggCoverManager.NotifyCollected();
                LevelDirector.LevelGenService?.UpdateClickableStates(true);
            });
            RefreshXSortClickables();
        }

        public void WriteRecoveryData(RecoverLevelData target)
        {
            if (target == null)
                return;

            target.AwardColorEggCoverStates ??= new List<RecoverLevelData.RLAwardColorEggCoverData>();
            target.AwardColorEggCoverStates.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry entry = entries[i];
                target.AwardColorEggCoverStates.Add(new RecoverLevelData.RLAwardColorEggCoverData
                {
                    placement = entry.placement,
                    collected = entry.collected,
                });
            }

            target.awardColorEggCoverCollectedCount = CollectedCount;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].behavior?.Dispose();
                entries[i].behavior = null;
            }
            entries.Clear();
        }

        AwardColorEggCoverBehavior Spawn(RuntimeEntry entry, Vector3 localPosition)
        {
            GameObject prefab = coverPrefab;
            GameObject instance = prefab != null
                ? Object.Instantiate(prefab, parent, false)
                : new GameObject("AwardColorEggCover_" + entry.placement.RuntimeId);
            if (prefab == null)
                instance.transform.SetParent(parent, false);

            AwardColorEggCoverBehavior behavior = instance.GetComponent<AwardColorEggCoverBehavior>();
            if (behavior == null)
                behavior = instance.AddComponent<AwardColorEggCoverBehavior>();
            int sorting = -(entry.placement.layer * 300) - 150
                + entry.placement.anchorX - entry.placement.anchorY;
            behavior.Initialize(entry.placement, localPosition, entry.localSize, sorting);
            return behavior;
        }

        static GameObject LoadCoverPrefab()
        {
            const string prefabPath = "Assets/CABRes/Prefabs/AwardColorEggCover/AwardColorEggCover_2x2.prefab";
            GameObject prefab = UILoadHelper.LoadAsset<GameObject>(prefabPath);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#endif
            return prefab;
        }

        static Sprite ReadPrefabSprite(GameObject prefab)
        {
            if (prefab == null)
                return null;
            Transform body = prefab.transform.Find("Visuals/Body");
            SpriteRenderer renderer = body != null
                ? body.GetComponent<SpriteRenderer>()
                : prefab.GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }

        void CalculateLocalGeometry(
            AwardColorEggCoverPlacement placement,
            out Vector3 center,
            out Vector2 size)
        {
            Vector2 XSortSize = LevelFitter.XSortSize;
            bool initialized = false;
            Bounds bounds = default;
            int footprintLayer = Mathf.Max(0, placement.layer - 1);
            for (int y = 0; y < AwardColorEggCoverResolver.CellSize.y; y++)
            {
                for (int x = 0; x < AwardColorEggCoverResolver.CellSize.x; x++)
                {
                    Vector3 cellCenter = LevelFitter.GetPosition(new ElementPosition(
                        placement.anchorX + x,
                        placement.anchorY + y,
                        footprintLayer));
                    Bounds cellBounds = new Bounds(cellCenter, new Vector3(XSortSize.x, XSortSize.y, 0f));
                    if (!initialized)
                    {
                        bounds = cellBounds;
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(cellBounds);
                    }
                }
            }

            center = bounds.center;
            center.z = placement.layer - 0.5f;
            size = new Vector2(Mathf.Max(0.01f, bounds.size.x), Mathf.Max(0.01f, bounds.size.y));
        }

        int CountUpperBlockers(RuntimeEntry entry)
        {
            // 用碰撞盒重叠计数，不用棋心进 footprint：边缘压住的棋也算遮挡，避免还剩一颗就飞走。
            List<XSortActor> XSorts = LevelDirector.LevelGenService?.ActXSorts;
            int count = 0;
            if (XSorts != null)
            {
                for (int i = 0; i < XSorts.Count; i++)
                {
                    XSortActor XSort = XSorts[i];
                    if (XSort == null || XSort.IsSubmitted || XSort.LayerID >= entry.placement.layer)
                        continue;
                    if (OverlapsEggCollider(entry, XSort))
                        count++;
                }
            }

            for (int i = 0; i < entries.Count; i++)
            {
                RuntimeEntry other = entries[i];
                if (other == entry || other.collected || other.collecting || other.behavior == null)
                    continue;
                if (other.placement.layer >= entry.placement.layer)
                    continue;
                if (entry.behavior != null
                    && other.behavior.BoxCollider != null
                    && XSortStackUtil.AreCollidersOverlappingXY(
                        entry.behavior.BoxCollider,
                        other.behavior.BoxCollider))
                    count++;
            }

            return count;
        }

        RuntimeEntry Find(AwardColorEggCoverBehavior behavior)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].behavior == behavior)
                    return entries[i];
            }
            return null;
        }

        static void RefreshXSortClickables()
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
    }
}
