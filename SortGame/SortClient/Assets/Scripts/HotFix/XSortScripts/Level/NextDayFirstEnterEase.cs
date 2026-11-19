using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 跨日（次日/隔日）首次进关降难度：换皮 + 备战栏剩余匹配换到首层 + 同层洗其余。
    /// </summary>
    public static class NextDayFirstEnterEase
    {
        static bool s_pendingCrossDayEase;
        static bool s_activeThisLoad;

        public static bool IsActiveThisLoad => s_activeThisLoad;

        public static void TryMarkPendingOnLogin()
        {
            var userData = GPUserDataManager.Instance?.UserData;
            if (userData == null)
                return;

            int today = userData.GetTodayLocalDate();
            // 旧档/新号未写过：只打戳，不触发降难度
            if (userData.nextDayEaseAppliedLocalDate == 0)
            {
                userData.nextDayEaseAppliedLocalDate = today;
                return;
            }

            if (userData.nextDayEaseAppliedLocalDate != today)
                s_pendingCrossDayEase = true;
        }

        /// <summary>
        /// 进关时消耗跨日首关资格。返回 true 表示本关走降难度。
        /// </summary>
        public static bool TryConsumeForThisLoad()
        {
            s_activeThisLoad = false;
            if (!GameData.BFromStartScene)
                return false;

            var userData = GPUserDataManager.Instance?.UserData;
            if (userData == null)
                return false;

            int today = userData.GetTodayLocalDate();
            if (userData.nextDayEaseAppliedLocalDate == 0)
            {
                userData.nextDayEaseAppliedLocalDate = today;
                return false;
            }

            bool crossDay = s_pendingCrossDayEase || userData.nextDayEaseAppliedLocalDate != today;
            if (!crossDay)
                return false;

            s_pendingCrossDayEase = false;
            userData.nextDayEaseAppliedLocalDate = today;
            s_activeThisLoad = true;
            return true;
        }

        public static void ClearActiveThisLoad()
        {
            s_activeThisLoad = false;
        }

        public static void ApplyBoardEaseIfActive()
        {
            if (!s_activeThisLoad)
                return;

            RunSelfCheck();
            ApplyBoardEase();
            RecoverLevelData.SaveCurrentLevelData(ignoreGameActive: true);
        }

        static void ApplyBoardEase()
        {
            var gen = LevelDirector.LevelGenService;
            var dock = LevelDirector.Dock;
            if (gen == null || gen.ActXSorts == null)
                return;

            Physics.SyncTransforms();
            ReskinBoardAndDock(gen, dock);
            PromoteByDockThenSameLayerShuffle(gen, dock);
            gen.RelinkXSorts(false);
            gen.UpdateClickableStates();
        }

        static void ReskinBoardAndDock(LevelGenService gen, DockActor dock)
        {
            int levelNumber = LevelDirector.LoadedLevelIndex + 1;
            var available = XSortSkinCatalog.GetAvailableSkins(levelNumber);
            if (available == null || available.Count == 0)
                return;

            var groups = new Dictionary<int, List<XSortActor>>();
            void Collect(XSortActor XSort)
            {
                if (XSort == null || XSort.XSortModel == null || XSort.IsCoinReward)
                    return;
                if (string.IsNullOrEmpty(XSort.RuntimeSkinName))
                    return;

                int idx = XSort.XSortModel.XSortIndex;
                if (!groups.TryGetValue(idx, out var list))
                {
                    list = new List<XSortActor>();
                    groups[idx] = list;
                }
                list.Add(XSort);
            }

            for (int i = 0; i < gen.ActXSorts.Count; i++)
            {
                var t = gen.ActXSorts[i];
                if (t != null && !t.IsSubmitted)
                    Collect(t);
            }

            if (dock != null)
            {
                var dockXSorts = dock.GetOccupiedXSortIndices();
                if (dockXSorts != null)
                {
                    for (int i = 0; i < dockXSorts.Count; i++)
                        Collect(dockXSorts[i]);
                }
            }

            var usedNames = new HashSet<string>();
            foreach (var kv in groups)
            {
                string name = kv.Value[0].RuntimeSkinName;
                if (!string.IsNullOrEmpty(name))
                    usedNames.Add(name);
            }

            foreach (var kv in groups)
            {
                string oldName = kv.Value[0].RuntimeSkinName;
                var oldSkin = XSortSkinCatalog.GetSkin(oldName);
                if (oldSkin == null)
                    continue;

                usedNames.Remove(oldName);
                string newName = PickReplacementSkinName(oldSkin, available, usedNames);
                usedNames.Add(newName);

                if (newName == oldName)
                    continue;

                for (int i = 0; i < kv.Value.Count; i++)
                    kv.Value[i].UpdateRuntimeSkin(newName);
            }
        }

        /// <summary>
        /// 优先同色同类型另一个 Name，否则同色其他类型，再否则保持原 Name。
        /// </summary>
        public static string PickReplacementSkinName(
            XSortSkinRuntimeData oldSkin,
            IReadOnlyList<XSortSkinRuntimeData> available,
            HashSet<string> usedNames)
        {
            if (oldSkin == null || available == null || available.Count == 0)
                return oldSkin?.Name ?? "";

            usedNames ??= new HashSet<string>();

            XSortSkinRuntimeData sameColorType = null;
            XSortSkinRuntimeData sameColorOther = null;
            for (int i = 0; i < available.Count; i++)
            {
                var skin = available[i];
                if (skin == null || string.IsNullOrEmpty(skin.Name))
                    continue;
                if (skin.Name == oldSkin.Name || usedNames.Contains(skin.Name))
                    continue;
                if (skin.Color != oldSkin.Color)
                    continue;

                if (skin.Type == oldSkin.Type)
                {
                    if (sameColorType == null)
                        sameColorType = skin;
                }
                else if (sameColorOther == null)
                {
                    sameColorOther = skin;
                }
            }

            if (sameColorType != null)
                return sameColorType.Name;
            if (sameColorOther != null)
                return sameColorOther.Name;
            return oldSkin.Name;
        }

        static void PromoteByDockThenSameLayerShuffle(LevelGenService gen, DockActor dock)
        {
            var boardXSorts = new List<XSortActor>();
            for (int i = 0; i < gen.ActXSorts.Count; i++)
            {
                var t = gen.ActXSorts[i];
                if (t == null || t.IsSubmitted || t.IsCoinReward || !t.IsShuffleAllowed())
                    continue;
                boardXSorts.Add(t);
            }

            if (boardXSorts.Count <= 1)
                return;

            var dockCounts = new Dictionary<int, int>();
            var dockTypeOrder = new List<int>();
            if (dock != null)
            {
                var dockXSorts = dock.GetOccupiedXSortIndices();
                if (dockXSorts != null)
                {
                    for (int i = 0; i < dockXSorts.Count; i++)
                    {
                        var t = dockXSorts[i];
                        if (t?.XSortModel == null)
                            continue;
                        int id = t.MatchTypeId;
                        if (!dockCounts.ContainsKey(id))
                            dockTypeOrder.Add(id);
                        dockCounts.TryGetValue(id, out int c);
                        dockCounts[id] = c + 1;
                    }
                }
            }

            var easeXSorts = new List<DockEaseXSort>(boardXSorts.Count);
            for (int i = 0; i < boardXSorts.Count; i++)
                easeXSorts.Add(new DockEaseXSort(boardXSorts[i].MatchTypeId, boardXSorts[i].ElementPosition.LayerId));

            var used = new HashSet<XSortActor>();
            var swaps = PlanFirstLayerSwaps(easeXSorts, dockTypeOrder, dockCounts);
            for (int i = 0; i < swaps.Count; i++)
            {
                var (moverIndex, targetIndex) = swaps[i];
                SwapXSortsElementPosition(gen, boardXSorts[moverIndex], boardXSorts[targetIndex]);
                used.Add(boardXSorts[moverIndex]);
                used.Add(boardXSorts[targetIndex]);
            }

            Physics.SyncTransforms();

            var leftover = boardXSorts.Where(t => t != null && !used.Contains(t)).ToList();
            if (dockCounts.Count > 0)
            {
                leftover = leftover
                    .Where(t => !dockCounts.ContainsKey(t.MatchTypeId))
                    .ToList();
            }

            SameLayerShuffle(gen, leftover);
        }

        readonly struct DockEaseXSort
        {
            public readonly int TypeId;
            public readonly int LayerId;

            public DockEaseXSort(int typeId, int layerId)
            {
                TypeId = typeId;
                LayerId = layerId;
            }
        }

        /// <summary>
        /// 每种备战栏花色只把凑齐三消还差的枚数换到当前首层（棋盘最小 LayerId）。
        /// 已在首层的计入名额并锁定，不能被别的花色换走。
        /// ponytail: 只能和现有首层棋子对换；首层格子不够时，多出来的匹配留在原层。
        /// </summary>
        static List<(int mover, int target)> PlanFirstLayerSwaps(
            IReadOnlyList<DockEaseXSort> board,
            IReadOnlyList<int> dockTypeOrder,
            IReadOnlyDictionary<int, int> dockCounts)
        {
            var swaps = new List<(int mover, int target)>();
            if (board == null || board.Count <= 1 || dockTypeOrder == null || dockCounts == null || dockCounts.Count == 0)
                return swaps;

            int firstLayer = board[0].LayerId;
            for (int i = 1; i < board.Count; i++)
            {
                if (board[i].LayerId < firstLayer)
                    firstLayer = board[i].LayerId;
            }

            var blocked = new bool[board.Count];
            LockAlreadyOnFirstLayer(board, dockTypeOrder, dockCounts, firstLayer, 2, blocked);
            LockAlreadyOnFirstLayer(board, dockTypeOrder, dockCounts, firstLayer, 1, blocked);
            MoveRemainingToFirstLayer(board, dockTypeOrder, dockCounts, firstLayer, 2, blocked, swaps);
            MoveRemainingToFirstLayer(board, dockTypeOrder, dockCounts, firstLayer, 1, blocked, swaps);
            return swaps;
        }

        static void LockAlreadyOnFirstLayer(
            IReadOnlyList<DockEaseXSort> board,
            IReadOnlyList<int> dockTypeOrder,
            IReadOnlyDictionary<int, int> dockCounts,
            int firstLayer,
            int requiredDockCount,
            bool[] blocked)
        {
            for (int t = 0; t < dockTypeOrder.Count; t++)
            {
                int typeId = dockTypeOrder[t];
                if (!dockCounts.TryGetValue(typeId, out int dockCount) || dockCount != requiredDockCount)
                    continue;

                int need = 3 - dockCount;
                int kept = 0;
                for (int i = 0; i < board.Count && kept < need; i++)
                {
                    if (blocked[i] || board[i].TypeId != typeId || board[i].LayerId != firstLayer)
                        continue;
                    blocked[i] = true;
                    kept++;
                }
            }
        }

        static void MoveRemainingToFirstLayer(
            IReadOnlyList<DockEaseXSort> board,
            IReadOnlyList<int> dockTypeOrder,
            IReadOnlyDictionary<int, int> dockCounts,
            int firstLayer,
            int requiredDockCount,
            bool[] blocked,
            List<(int mover, int target)> swaps)
        {
            for (int t = 0; t < dockTypeOrder.Count; t++)
            {
                int typeId = dockTypeOrder[t];
                if (!dockCounts.TryGetValue(typeId, out int dockCount) || dockCount != requiredDockCount)
                    continue;

                int need = 3 - dockCount;
                int kept = 0;
                for (int i = 0; i < board.Count; i++)
                {
                    if (blocked[i] && board[i].TypeId == typeId && board[i].LayerId == firstLayer)
                        kept++;
                }

                int still = need - kept;
                if (still <= 0)
                    continue;

                var movers = new List<int>();
                for (int i = 0; i < board.Count; i++)
                {
                    if (blocked[i] || board[i].TypeId != typeId || board[i].LayerId == firstLayer)
                        continue;
                    movers.Add(i);
                }

                movers.Sort((a, b) =>
                {
                    int layer = board[b].LayerId.CompareTo(board[a].LayerId);
                    return layer != 0 ? layer : a.CompareTo(b);
                });

                for (int m = 0; m < movers.Count && still > 0; m++)
                {
                    int target = FindFirstLayerTarget(board, dockCounts, firstLayer, blocked);
                    if (target < 0)
                        break;

                    int mover = movers[m];
                    swaps.Add((mover, target));
                    blocked[mover] = true;
                    blocked[target] = true;
                    still--;
                }
            }
        }

        static int FindFirstLayerTarget(
            IReadOnlyList<DockEaseXSort> board,
            IReadOnlyDictionary<int, int> dockCounts,
            int firstLayer,
            bool[] blocked)
        {
            int fallback = -1;
            for (int i = 0; i < board.Count; i++)
            {
                if (blocked[i] || board[i].LayerId != firstLayer)
                    continue;
                if (!dockCounts.ContainsKey(board[i].TypeId))
                    return i;
                if (fallback < 0)
                    fallback = i;
            }

            return fallback;
        }

        static void SameLayerShuffle(LevelGenService gen, List<XSortActor> XSorts)
        {
            if (XSorts == null || XSorts.Count <= 1)
                return;

            var byLayer = XSorts.GroupBy(t => t.ElementPosition.LayerId);
            foreach (var group in byLayer)
            {
                var list = group.ToList();
                if (list.Count <= 1)
                    continue;

                var positions = list.Select(t => t.ElementPosition).ToList();
                ShuffleList(positions);
                for (int i = 0; i < list.Count; i++)
                    ApplyPosition(gen, list[i], positions[i]);
            }

            Physics.SyncTransforms();
        }

        static void ShuffleList<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        static void SwapXSortsElementPosition(LevelGenService gen, XSortActor a, XSortActor b)
        {
            if (a == null || b == null || a == b)
                return;

            var posA = a.ElementPosition;
            var posB = b.ElementPosition;
            ApplyPosition(gen, a, posB);
            ApplyPosition(gen, b, posA);
        }

        static void ApplyPosition(LevelGenService gen, XSortActor XSort, ElementPosition pos)
        {
            var layers = gen.Layers;
            XSort.SetPosition(pos);
            XSort.transform.SetParent(layers[pos.LayerId].LayerObject.transform);
            XSort.transform.localPosition = LevelFitter.GetPosition(pos);
            XSort.transform.name = $"after_l:{pos.LayerId}_x:{pos.X}_y:{pos.Y}";
        }

        public static void RunSelfCheck()
        {
            // 换皮回退链
            var old = new XSortSkinRuntimeData("red_cat_1", "red", "cat", 1);
            var available = new List<XSortSkinRuntimeData>
            {
                old,
                new XSortSkinRuntimeData("red_cat_2", "red", "cat", 2),
                new XSortSkinRuntimeData("red_dog_1", "red", "dog", 1),
                new XSortSkinRuntimeData("blue_cat_1", "blue", "cat", 1),
            };
            var used = new HashSet<string>();
            string pickSame = PickReplacementSkinName(old, available, used);
            Debug.Assert(pickSame == "red_cat_2", "[NextDayEase] same color+type should win");

            used.Add("red_cat_2");
            string pickColor = PickReplacementSkinName(old, available, used);
            Debug.Assert(pickColor == "red_dog_1", "[NextDayEase] same color other type fallback");

            used.Add("red_dog_1");
            string pickKeep = PickReplacementSkinName(old, available, used);
            Debug.Assert(pickKeep == "red_cat_1", "[NextDayEase] keep original when no same-color free");
            AssertFirstLayerPlans();
        }

        static void AssertFirstLayerPlans()
        {
            var order = new List<int> { 1, 2 };
            var dock = new Dictionary<int, int> { { 1, 2 }, { 2, 1 } };

            // 备战栏 A 差 1、B 差 2。多出来的 A 不能把首层占满，否则 B 上不来。
            var extras = new[]
            {
                new DockEaseXSort(1, 5),
                new DockEaseXSort(1, 4),
                new DockEaseXSort(1, 3),
                new DockEaseXSort(2, 5),
                new DockEaseXSort(2, 4),
                new DockEaseXSort(9, 0),
                new DockEaseXSort(9, 0),
                new DockEaseXSort(9, 0),
            };
            int[] extraLayers = LayersAfter(extras, PlanFirstLayerSwaps(extras, order, dock));
            Debug.Assert(CountOnLayer(extras, extraLayers, 1, 0) == 1, "[NextDayEase] dock 2 only brings 1 match to first layer");
            Debug.Assert(CountOnLayer(extras, extraLayers, 2, 0) == 2, "[NextDayEase] dock 1 brings both remaining matches to first layer");

            // B 已经在首层，A 只能换走非备战栏棋子。
            var parked = new[]
            {
                new DockEaseXSort(2, 0),
                new DockEaseXSort(2, 0),
                new DockEaseXSort(9, 0),
                new DockEaseXSort(1, 5),
            };
            var parkedSwaps = PlanFirstLayerSwaps(parked, order, dock);
            int[] parkedLayers = LayersAfter(parked, parkedSwaps);
            Debug.Assert(parkedLayers[0] == 0 && parkedLayers[1] == 0, "[NextDayEase] keep matches already on first layer");
            Debug.Assert(CountOnLayer(parked, parkedLayers, 1, 0) == 1, "[NextDayEase] buried dock-2 match still reaches first layer");
            Debug.Assert(parkedSwaps.Count == 1 && parkedSwaps[0].target == 2, "[NextDayEase] swap with non-dock first-layer XSort");

            // 顶层被点空后，当前最小 LayerId 就是首层。
            var clearedTop = new[]
            {
                new DockEaseXSort(1, 4),
                new DockEaseXSort(1, 3),
                new DockEaseXSort(9, 2),
                new DockEaseXSort(8, 2),
            };
            int[] clearedLayers = LayersAfter(clearedTop, PlanFirstLayerSwaps(clearedTop, new List<int> { 1 }, new Dictionary<int, int> { { 1, 2 } }));
            Debug.Assert(CountOnLayer(clearedTop, clearedLayers, 1, 2) == 1, "[NextDayEase] promote onto current front layer");

            // 首层只剩备战栏花色时，只换走出名额的那枚。
            var surplus = new[]
            {
                new DockEaseXSort(2, 0),
                new DockEaseXSort(2, 0),
                new DockEaseXSort(2, 0),
                new DockEaseXSort(1, 5),
            };
            int[] surplusLayers = LayersAfter(surplus, PlanFirstLayerSwaps(surplus, order, dock));
            Debug.Assert(CountOnLayer(surplus, surplusLayers, 2, 0) == 2, "[NextDayEase] surplus slot can be taken, quota stays");
            Debug.Assert(CountOnLayer(surplus, surplusLayers, 1, 0) == 1, "[NextDayEase] dock-2 match still reaches first layer");
        }

        static int[] LayersAfter(DockEaseXSort[] board, List<(int mover, int target)> swaps)
        {
            var seen = new HashSet<int>();
            var layers = new int[board.Length];
            for (int i = 0; i < board.Length; i++)
                layers[i] = board[i].LayerId;

            for (int i = 0; i < swaps.Count; i++)
            {
                var (mover, target) = swaps[i];
                Debug.Assert(seen.Add(mover) && seen.Add(target), "[NextDayEase] swap index reused");
                (layers[mover], layers[target]) = (layers[target], layers[mover]);
            }

            return layers;
        }

        static int CountOnLayer(DockEaseXSort[] board, int[] layers, int typeId, int layer)
        {
            int count = 0;
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i].TypeId == typeId && layers[i] == layer)
                    count++;
            }

            return count;
        }
    }
}
